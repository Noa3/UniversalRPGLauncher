using System.Collections.Generic;

namespace UniversalRPG.Web;

/// <summary>
/// Why the interpreter stopped, which is the one thing a caller must never
/// guess at.
/// </summary>
public enum MzStep
{
    /// <summary>It ran out of commands. The event is over.</summary>
    Finished,

    /// <summary>It did one command and can be asked for the next.</summary>
    Stepped,

    /// <summary>
    /// A command said it is not done. The engine leaves the index where it was,
    /// so the same command runs again next time, and a reader that moved on
    /// would drop whatever the command was waiting for.
    /// </summary>
    Waiting,

    /// <summary>
    /// A command asked for something that is not known, and the index was left
    /// alone. The event is stuck at this command and says why.
    /// </summary>
    Refused,

    /// <summary>The list ends in the middle of a structure. See
    /// <see cref="MzInterpreter.Malformed"/>.</summary>
    Truncated,

    /// <summary>
    /// It ran out of the steps the engine would have had frames for. A repeat
    /// above that never meets a changed number sends the index back for ever, and
    /// the engine freezes the game on it; this reader says so instead.
    /// </summary>
    Frozen,
}

/// <summary>
/// Walks the commands of an event, one at a time, the way the engine walks them.
/// </summary>
/// <remarks>
/// <para>
/// This is the piece that makes a decision mean something. Without an index into
/// a list, a branch decides something and nothing acts on it; here the answer to
/// a branch moves the index over the commands that are inside the other arm, and
/// a loop moves it back.
/// </para>
/// <para>
/// Every rule here was read out of <c>Game_Interpreter</c> in the engine, and
/// three of them are not what a first reading suggests:
/// </para>
/// <list type="bullet">
/// <item>
/// <b>A command the engine has no method for is stepped over, not refused.</b>
/// <c>executeCommand</c> asks <c>typeof this[methodName] === "function"</c> and,
/// if it is not, still does <c>this._index++</c>. A reader that reported such a
/// command as unknown would stop a game over a command the game itself ran past.
/// </item>
/// <item>
/// <b>Skipping a branch reads the next command without a bound check.</b>
/// <c>skipBranch</c> is <c>while (this._list[this._index + 1].indent &gt;
/// this._indent)</c> with no test for the end of the list. A list whose last
/// command is the last line of a branch therefore asks for a command that is not
/// there. This reader returns <see cref="MzStep.Truncated"/> instead of reading
/// past the end, and says so, because the alternative is a crash on a file that
/// is merely odd rather than broken.
/// </item>
/// <item>
/// <b>Dividing by zero sets a variable to zero rather than failing.</b>
/// The engine wraps the operation in a try and, on any failure, writes zero.
/// A reader that let the exception out would stop a game the engine runs.
/// </item>
/// </list>
/// </remarks>
public sealed class MzInterpreter
{
    private readonly List<MzCommandEntry> _commands;
    private readonly Dictionary<int, bool?> _branch = new();

    /// <summary>The list being walked.</summary>
    public IReadOnlyList<MzCommandEntry> Commands => _commands;

    /// <summary>Where the next command will be read from.</summary>
    public int Index { get; internal set; }

    /// <summary>Why the interpreter stopped, if it did.</summary>
    public MzStep Stopped { get; private set; } = MzStep.Stepped;

    /// <summary>
    /// The engine stops a run that has gone on too long, counting frames: a
    /// hundred thousand commands in one frame, or a repeat above that never
    /// meets a changed number, and the game freezes until the author finds it.
    /// This reader has no frames, so it counts commands against the same number
    /// and reports <see cref="MzStep.Frozen"/> rather than running for ever on a
    /// list that never ends.
    /// </summary>
    public int CommandLimit { get; init; } = 100_000;

    private int _taken;

    /// <summary>What is missing, when it stopped because something was.</summary>
    public string Reason { get; private set; } = "";

    /// <summary>
    /// Set when a list ends in the middle of a branch, a loop or a choice, where
    /// the engine would read past the end of its list. This is a fact about the
    /// file, not a failure of the game, and it is reported rather than thrown.
    /// </summary>
    public string Malformed { get; private set; } = "";

    /// <summary>
    /// Where the numbers a random command draws come from. It belongs to the run
    /// rather than to the class, so two event lists read at once do not shift
    /// one another's numbers, and a caller can replay a run it has seen.
    /// </summary>
    public MzRandom Random { get; init; } = new();

    /// <summary>How deeply the interpreter has been nested. The engine's limit
    /// is 100, and it throws past that.</summary>
    public int Depth { get; init; }

    /// <summary>The map the event belongs to, or zero for a common event.</summary>
    public int MapId { get; private set; }

    /// <summary>The event, or zero for a common event.</summary>
    public int EventId { get; private set; }

    public MzInterpreter(IReadOnlyList<MzCommandEntry> pCommands) =>
        _commands = new List<MzCommandEntry>(pCommands);

    /// <summary>Is there anything left to run, as the engine asks it.</summary>
    public bool IsRunning => Index < _commands.Count;

    public void Setup(int pMapId, int pEventId)
    {
        MapId = pMapId;
        EventId = pEventId;
        Index = 0;
        Stopped = MzStep.Stepped;
        Reason = "";
        _branch.Clear();
    }

    /// <summary>
    /// Runs commands until the interpreter stops, which is what the engine's own
    /// update loop does each frame.
    /// </summary>
    /// <param name="pCommands">
    /// The things the game asked for, and what this interpreter did about them.
    /// </param>
    /// <param name="pBranchFacts">What the game knows, to decide a branch.</param>
    public void Run(
        List<MzAction> pCommands, MzBranchFacts pBranchFacts)
    {
        while (IsRunning)
        {
            if (_taken >= CommandLimit)
            {
                Stopped = MzStep.Frozen;
                Reason =
                    $"the event ran {CommandLimit} commands without finishing,"
                    + " which is where the engine gives up on a run that goes"
                    + " on too long in one frame";
                return;
            }
            if (!ExecuteOne(pCommands, pBranchFacts))
            {
                // A command that is not done leaves the index where it was. It
                // is either waiting — the same 230 will be read again the
                // frame after — or it stopped with a reason of its own, and
                // both end the run here. **This is not the same as finishing**,
                // and a caller that read `Stopped` as "the list ended" would
                // take a game's dialogue for a completed event.
                return;
            }
            _taken++;
        }
        Stopped = MzStep.Finished;
    }

    /// <summary>
    /// Runs one command. It returns whether the interpreter may run the next one;
    /// when it returns false the interpreter has already said why it stopped.
    /// </summary>
    public bool ExecuteOne(
        List<MzAction> pCommands, MzBranchFacts pBranchFacts)
    {
        var command = _commands[Index];

        // Three answers, read apart: a command that is not about where the index
        // goes, one that ran, and one that stopped the interpreter. A first
        // draft of this returned one bool for the first two, so "not mine" was
        // read as "stopped" and the index never moved again.
        switch (MzControlFlow.TryExecute(this, command, pCommands, pBranchFacts))
        {
            case MzControlFlow.MzControlOutcome.NotControlFlow:
                break;

            case MzControlFlow.MzControlOutcome.Stopped:
                return false;

            case MzControlFlow.MzControlOutcome.Ran:
                // The engine's own rule, with no exception: every command that
                // returns true is followed by this._index++. A first draft of
                // this reader added a flag for "the command moved the index
                // itself" and did not step over a command that had, which made
                // an else step onto the false arm it had just skipped over.
                //
                // A repeat above is not the exception it looks like either: it
                // walks back to the first command at its own indent, and the
                // step then moves off that one, so a loop written 112, body,
                // 413 goes round properly.
                Index++;
                return true;
        }

        // A command the engine has no method for is stepped over. The engine
        // asks whether the method exists and, when it does not, still advances.
        if (!MzCommands.HasEffect(command.Code))
        {
            Index++;
            return true;
        }

        if (!MzCommands.TryExecute(
            this, command, pCommands, pBranchFacts, Random))
        {
            return false;
        }

        // **The index steps over a command that ran, and a wait does not hold
        // it.** The engine's own rule is that every command which returns true
        // is followed by `this._index++`, and the wait a command set lives in
        // `_waitCount` where the next command cannot reach it. A first draft
        // made the index wait for the wait, so a 232 that asked to wait held
        // its own index on the command that asked — the next frame read the
        // same command again, set the same frames again, and the picture never
        // arrived.
        //
        // So the index moves and the run stops in two separate steps, which is
        // what the engine's frame does: the command is done, the frame is not.
        Index++;
        if (Stopped != MzStep.Stepped)
        {
            return false;
        }
        return true;
    }

    /// <summary>
    /// The branch result for the indent a command sits at, and the first time a
    /// command at that indent is met there is none.
    /// </summary>
    public bool? BranchAt(int pIndent) =>
        _branch.TryGetValue(pIndent, out var value) ? value : null;

    public void SetBranch(int pIndent, bool? pValue) => _branch[pIndent] = pValue;

    public void ClearBranch(int pIndent) => _branch.Remove(pIndent);

    /// <summary>
    /// Steps over every command that sits inside the branch this one opens, the
    /// way the engine's <c>skipBranch</c> does, and says so when the list ends
    /// before the branch does.
    /// </summary>
    public bool SkipBranch()
    {
        var indent = _commands[Index].Indent;
        while (Index + 1 < _commands.Count)
        {
            if (_commands[Index + 1].Indent <= indent)
            {
                return true;
            }
            Index++;
        }
        // The engine writes `while (this._list[this._index + 1].indent >
        // this._indent)` and has no test for the end of the list, so a list
        // whose last command is the last line of a branch asks for a command
        // that is not there. Reported, not read past.
        Malformed =
            $"a branch opened at index {Index} and never closes before the list"
            + " ends, and the engine would read past the end here";
        Stopped = MzStep.Truncated;
        return false;
    }

    /// <summary>
    /// Walks back to the first command at the same indent, the way the engine's
    /// repeat-above does, and says so when there is none.
    /// </summary>
    public bool JumpToRepeat()
    {
        var indent = _commands[Index].Indent;
        do
        {
            Index--;
            if (Index < 0)
            {
                Malformed =
                    "a repeat above sits at the start of the list with nothing"
                    + " above it to go back to";
                Stopped = MzStep.Truncated;
                return false;
            }
        }
        while (_commands[Index].Indent != indent);
        return true;
    }

    /// <summary>
    /// Steps over everything up to and including the repeat that closes the loop
    /// this one is in, the way the engine's break-loop does, and says so when
    /// there is none.
    /// </summary>
    public bool JumpToEndOfLoop()
    {
        var depth = 0;
        while (Index < _commands.Count - 1)
        {
            Index++;
            var code = _commands[Index].Code;
            if (code == MzCommandTable.Loop)
            {
                depth++;
            }
            if (code == MzCommandTable.RepeatAbove)
            {
                if (depth > 0)
                {
                    depth--;
                }
                else
                {
                    return true;
                }
            }
        }
        // The engine's own loop has the same shape and the same end: it stops
        // when the list does, whatever the depth was.
        return true;
    }

    /// <summary>
    /// Moves to a label, clearing the branch results for every indent stepped
    /// over, which is what the engine's <c>jumpTo</c> does.
    /// </summary>
    public bool JumpToLabel(string pName)
    {
        for (var i = 0; i < _commands.Count; i++)
        {
            var command = _commands[i];
            if (command.Code == MzCommandTable.Label
                && command.Parameters.Count > 0
                && command.Parameters[0] == pName)
            {
                JumpTo(i);
                return true;
            }
        }
        // The engine looks through the whole list and, finding no such label,
        // leaves the index where it was and carries on. That is worth keeping:
        // a jump to a label that is not there is not a reason to stop a game.
        return false;
    }

    /// <summary>
    /// The engine clears the branch result of every indent it steps over while
    /// jumping, so a branch met again after a jump is decided afresh.
    /// </summary>
    public void JumpTo(int pIndex)
    {
        var last = Index;
        var start = pIndex < last ? pIndex : last;
        var end = pIndex < last ? last : pIndex;
        var indent = CurrentIndent();
        for (var i = start; i <= end; i++)
        {
            var stepped = _commands[i].Indent;
            if (stepped != indent)
            {
                _branch[indent] = null;
                indent = stepped;
            }
        }
        Index = pIndex;
    }

    /// <summary>The indent of the command about to run, which is also the
    /// indent of the command running when the engine is asked.</summary>
    public int CurrentIndent() =>
        Index < _commands.Count ? _commands[Index].Indent : 0;

    /// <summary>Stops, saying why, without moving the index.</summary>
    public void Stop(MzStep pStep, string pReason)
    {
        Stopped = pStep;
        Reason = pReason;
    }

    /// <summary>The frames a wait asked for, which a caller counts down itself.</summary>
    public int WaitFrames { get; private set; }

    /// <summary>
    /// Records that a command is waiting, and leaves the index where it was.
    /// The engine's <c>updateWaitCount</c> takes one off the count per frame and
    /// breaks the frame while it is above zero, and the command is read again
    /// the frame after — so a reader that moved the index on would run the rest
    /// of a list before the wait was over.
    /// </summary>
    public void Wait(int pFrames)
    {
        WaitFrames = pFrames;
        Stopped = MzStep.Waiting;
        Reason = pFrames > 0
            ? $"waiting {pFrames} frames at index {Index}"
            : $"waiting at index {Index} for something outside this reader";
    }

    /// <summary>
    /// Counts one frame off a wait, which is what the engine does before each
    /// frame, and says whether the wait is over.
    /// </summary>
    public bool PassFrame()
    {
        // The engine's `updateWaitCount` is
        // `if (this._waitCount > 0) { this._waitCount--; return true; }`, so a
        // count of zero is over and only a count above zero is counted. **A
        // reader that asked for a frame less than zero would let a count of
        // zero hold the caller for ever**, and there is a state where that
        // matters: a caller that passes frames past the end of the wait.
        if (WaitFrames <= 0)
        {
            WaitFrames = 0;
            Stopped = MzStep.Stepped;
            return true;
        }
        WaitFrames--;
        if (WaitFrames > 0)
        {
            return false;
        }
        WaitFrames = 0;
        Stopped = MzStep.Stepped;
        return true;
    }
}
