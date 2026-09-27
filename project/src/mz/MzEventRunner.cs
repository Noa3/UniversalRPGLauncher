using System.Collections.Generic;

namespace UniversalRPG.Web;

/// <summary>
/// Runs an MZ event list, and the lists it calls, the way the engine runs them.
/// </summary>
/// <remarks>
/// <para>
/// K-124 gave the interpreter an index and could walk one list. <b>An MZ event
/// is almost never one list.</b> This game stores eight common events in the
/// one map read here, reached by 117, and a 117 whose common event is not
/// available is not a command a reader may step over: the rest of the list
/// behind it never happens, and the game stops there.
/// </para>
/// <para>
/// The engine runs a called list with a <b>child interpreter</b>:
///
/// <code>
/// Game_Interpreter.prototype.command117 = function(params) {
///     const commonEvent = $dataCommonEvents[params[0]];
///     if (commonEvent) {
///         const eventId = this.isOnCurrentMap() ? this._eventId : 0;
///         this.setupChild(commonEvent.list, eventId);
///     }
///     return true;
/// };
///
/// Game_Interpreter.prototype.updateChild = function() {
///     if (this._childInterpreter) {
///         this._childInterpreter.update();
///         if (this._childInterpreter.isRunning()) { return true; }
///         else { this._childInterpreter = null; }
///     }
///     return false;
/// };</code>
///
/// <b>A child that is still running makes the parent wait for that frame</b>, and
/// the parent carries on from where it was when the child finishes. Two things
/// follow from that and both are easy to get wrong:
///
/// <list type="bullet">
/// <item>
/// <b>Every list in a run shares one set of facts.</b> A common event reads and
/// writes the same variables the list that called it, because both go through
/// the one <c>$gameVariables</c>. A runner that gave each list its own facts
/// would have a common event set a variable its caller cannot see.
/// </item>
/// <item>
/// <b>The event id travels with the call.</b> <c>isOnCurrentMap()</c> decides
/// whether the child knows which event it is part of, and that is what lets a
/// common event address "this event". A runner that always passed zero would
/// lose it.
/// </item>
/// </list>
/// </para>
/// <para>
/// A 230 wait is <b>not</b> modelled here. The engine's <c>updateWaitCount</c>
/// counts frames, and this reader has no frames; giving a caller a way to
/// advance a frame count is a different card from making a called list run. A
/// run that meets a 230 reports that it is waiting and names the count, which is
/// the truth rather than a guess.
/// </para>
/// <para>
/// What it does not do is invent data. <b>The bounded fixture read here carries
/// no <c>CommonEvents.json</c></b> — the real one is 4.5 MB and was left out on
/// purpose — so every 117 in this game names an index this repository cannot
/// hand over. A run that meets one says which index and that the list is not
/// here, and stops. Reading a common event out of a fixture that does not carry
/// it would mean writing the game's own scripts.
/// </para>
/// </remarks>
public sealed class MzEventRunner
{
    /// <summary>
    /// The common event lists a game stored, by the index its 117 names.
    /// </summary>
    /// <remarks>
    /// Keyed the way <c>CommonEvents.json</c> is: the array's own position, with
    /// index 0 unused, because that is how the engine reads it,
    /// <c>$dataCommonEvents[params[0]]</c>.
    /// </remarks>
    private readonly Dictionary<int, List<MzCommandEntry>> _commonEvents;

    /// <summary>
    /// The engine's own nesting limit. <c>setupChild</c> makes
    /// <c>new Game_Interpreter(this._depth + 1)</c>, and past a hundred the
    /// engine stops, so a common event that calls itself stops the game rather
    /// than running for ever. This runner says so instead of recursing until
    /// the stack gives out.
    ///
    /// <b>A field and not only a constant</b>, so a test can ask what happens
    /// three deep without building a hundred lists. The engine's hundred is the
    /// default; nothing here raises it.
    /// </summary>
    public int MaxDepth { get; init; } = 100;

    /// <summary>
    /// The engine's own freeze: a hundred thousand commands in one frame is what
    /// <c>checkFreeze</c> stops on, and it counts the whole run, not one list.
    /// </summary>
    public int CommandLimit { get; init; } = 100_000;

    public MzEventRunner(
        Dictionary<int, List<MzCommandEntry>> pCommonEvents = null) =>
        _commonEvents = pCommonEvents ?? new Dictionary<int, List<MzCommandEntry>>();

    /// <summary>What a run did, in a form a caller can act on.</summary>
    public sealed class Result
    {
        /// <summary>How the run ended.</summary>
        public MzStep Stopped { get; init; }

        /// <summary>
        /// The index of the common event a run stopped for, or -1 when it
        /// stopped for something else. **This is a field and not something a
        /// caller has to read out of the prose**: a test that took the number out
        /// of the message text lost its first digit, because the message grew a
        /// character and the offset did not.
        /// </summary>
        public int MissingCommonEvent { get; init; } = -1;

        /// <summary>
        /// The frames a 230 asked for, or zero. The engine counts these down one
        /// per frame; a caller with frames counts them itself.
        /// </summary>
        public int WaitingFrames { get; init; }

        /// <summary>
        /// The innermost list the run was on when it stopped, or null when it
        /// stopped before it made one. A caller reads a child's map, event and
        /// depth off this, which is how a common event's "this event" is
        /// checked rather than taken on trust.
        /// </summary>
        public MzInterpreter Child { get; init; }

        /// <summary>What the interpreter said, when it said something.</summary>
        public string Reason { get; init; } = "";

        /// <summary>
        /// What is wrong with a list itself, when something is. Empty for a list
        /// that is merely odd, which is not an error.
        /// </summary>
        public string Malformed { get; init; } = "";

        /// <summary>Every command the whole run recorded, in order.</summary>
        public List<MzAction> Actions { get; init; } = new();

        /// <summary>One line, for a caller writing a log.</summary>
        public string Describe() => Stopped switch
        {
            MzStep.Finished =>
                $"ran to its end, {Actions.Count} commands",
            MzStep.Waiting =>
                $"waiting {WaitingFrames} frames at code"
                + $" {WaitingCode}, {Actions.Count} commands so far",
            MzStep.Frozen => $"froze: {Reason}",
            MzStep.Truncated => $"a list that does not close: {Malformed}",
            _ => $"stopped: {Reason}",
        };

        /// <summary>The code the run is waiting at, for a caller writing a log.</summary>
        public int WaitingCode { get; init; }
    }

    /// <summary>
    /// Runs a list, and every list it calls, to the end of all of them.
    /// </summary>
    /// <param name="pCommands">The list, in the shape the editor writes it.</param>
    /// <param name="pBranchFacts">
    /// What the game knows. Every list in the run shares it, because the
    /// engine's <c>$gameVariables</c> is one object and a common event sees the
    /// same variables the list that called it.
    /// </param>
    /// <param name="pMapId">The map, or zero for a common event.</param>
    /// <param name="pEventId">The event, or zero for a common event.</param>
    /// <param name="pRandom">
    /// One generator for the whole run, so a common event and the list that
    /// called it draw from one stream and a run can be replayed.
    /// </param>
    public Result Run(
        IReadOnlyList<MzCommandEntry> pCommands,
        MzBranchFacts pBranchFacts,
        int pMapId = 0,
        int pEventId = 0,
        MzRandom pRandom = null)
    {
        var actions = new List<MzAction>();
        var random = pRandom ?? new MzRandom();
        var frames = new Stack<Frame>();

        var top = new MzInterpreter(pCommands) { Random = random, Depth = 0 };
        top.Setup(pMapId, pEventId);
        frames.Push(new Frame(top));

        var taken = 0;
        // The innermost list the run was on. A caller reads a child's map,
        // event and depth off this, which is how a common event's "this event"
        // is checked rather than taken on trust.
        var deepest = top;
        while (frames.Count > 0)
        {
            var frame = frames.Peek();
            if (frame.Interpreter.Depth >= deepest.Depth)
            {
                deepest = frame.Interpreter;
            }

            if (!frame.Interpreter.IsRunning)
            {
                // The engine drops a finished child and carries on with the
                // parent, and a list with no parent left is the whole run.
                frames.Pop();
                if (frames.Count == 0)
                {
                    return new Result
                    {
                        Stopped = MzStep.Finished,
                        Child = deepest.Depth > 0 ? deepest : null,
                        Actions = actions,
                    };
                }
                continue;
            }

            if (taken >= CommandLimit)
            {
                return new Result
                {
                    Stopped = MzStep.Frozen,
                    Child = deepest.Depth > 0 ? deepest : null,
                    Reason =
                        $"the run took {CommandLimit} commands without finishing,"
                        + " which is where the engine freezes a run that goes"
                        + " on too long in one frame",
                    Actions = actions,
                };
            }

            var at = frame.Interpreter.Index;
            var command = frame.Interpreter.Commands[at];
            var before = frame.Interpreter.Index;

            if (command.Code == MzCommandTable.CommonEvent)
            {
                var called = Called(
                    command, frame, frames, actions, pMapId, random);
                if (called != null)
                {
                    return called;
                }
                taken++;
                continue;
            }

            frame.Interpreter.ExecuteOne(actions, pBranchFacts);
            taken++;

            switch (frame.Interpreter.Stopped)
            {
                case MzStep.Frozen:
                case MzStep.Truncated:
                case MzStep.Refused:
                    return new Result
                    {
                        Stopped = frame.Interpreter.Stopped,
                        Child = deepest.Depth > 0 ? deepest : null,
                        Reason = frame.Interpreter.Reason,
                        Malformed = frame.Interpreter.Malformed,
                        Actions = actions,
                    };

                case MzStep.Waiting:
                    // A command that is not done leaves the index alone, and
                    // the engine waits a frame before reading it again. There
                    // are no frames here, so the run is handed back waiting and
                    // the caller decides when the next frame is.
                    return new Result
                    {
                        Stopped = MzStep.Waiting,
                        Child = deepest.Depth > 0 ? deepest : null,
                        WaitingCode = frame.Interpreter.Commands[before].Code,
                        WaitingFrames = frame.Interpreter.WaitFrames,
                        Reason = frame.Interpreter.Reason,
                        Actions = actions,
                    };
            }
        }

        return new Result
        {
            Stopped = MzStep.Finished,
            Child = deepest.Depth > 0 ? deepest : null,
            Actions = actions,
        };
    }

    /// <summary>
    /// Runs the common event a 117 names, or says why it cannot.
    /// </summary>
    /// <returns>
    /// A result to hand back when the run cannot go on, and null when the child
    /// is set up and the caller should carry on — which is the engine's
    /// <c>setupChild</c> followed by <c>updateChild</c> finding it still running.
    /// </returns>
    private Result Called(
        MzCommandEntry pCommand, Frame pFrame, Stack<Frame> pFrames,
        List<MzAction> pActions, int pMapId, MzRandom pRandom)
    {
        var index = At(pCommand, 0);
        pActions.Add(MzAction.CommonEvent(pCommand, index));

        if (pFrames.Count > MaxDepth)
        {
            return new Result
            {
                Stopped = MzStep.Refused,
                Child = pFrame.Interpreter.Depth > 0 ? pFrame.Interpreter : null,
                MissingCommonEvent = index,
                Reason =
                    $"common event {index} is nested {pFrames.Count} deep, and the"
                    + $" engine stops at {MaxDepth}, so a common event that calls"
                    + " itself ends the game rather than running for ever",
                Actions = pActions,
            };
        }

        if (!_commonEvents.TryGetValue(index, out var list))
        {
            // The engine's own line is `if (commonEvent)`, and a missing one
            // leaves the index where it was and carries on. **That is not what
            // this reader does**, because this repository's fixture holds no
            // CommonEvents.json and a silent step-over would run the rest of a
            // game's list as if the call had never been there. It says which
            // index is missing and stops.
            return new Result
            {
                Stopped = MzStep.Refused,
                Child = pFrame.Interpreter.Depth > 0 ? pFrame.Interpreter : null,
                MissingCommonEvent = index,
                Reason =
                    $"common event {index} is called at index"
                    + $" {pFrame.Interpreter.Index} and this repository has no"
                    + " list for it, so the rest of this list cannot run",
                Actions = pActions,
            };
        }

        // The engine asks isOnCurrentMap() and keeps the event id only then.
        // **The question is asked of the calling interpreter's own state**, and
        // that is what a first draft got wrong twice over: it read the map and
        // the event off the frame instead of off the interpreter, so a child
        // that had been given zero carried the caller's event id into the next
        // level and the event came back from the dead.
        var onMap = pFrame.Interpreter.MapId != 0;
        var eventId = onMap ? pFrame.Interpreter.EventId : 0;
        var child = new MzInterpreter(list)
        {
            Random = pRandom,
            Depth = pFrame.Interpreter.Depth + 1,
        };
        // `setupChild` calls `setup(list, eventId)`, and `setup` sets
        // `this._mapId = $gameMap.mapId()` — **the map the game is on, not the
        // one the caller was on**. A child is therefore always on the current
        // map, which is what makes `isOnCurrentMap()` true for it, and what
        // makes a second level of common event keep asking the question and
        // getting a different answer: its own `_eventId` is already 0.
        //
        // A first draft passed the caller's map down. **That is wrong for a
        // parallel-event list**, which has no map of its own, and it made a
        // second level of common event keep the caller's event id for ever.
        child.Setup(pMapId, eventId);
        pFrames.Push(new Frame(child));

        // The engine's executeCommand still steps the index over a 117 once the
        // child is set up, so the rest of the list waits at the next command
        // until the child is finished.
        pFrame.Interpreter.Index++;
        return null;
    }

    private static int At(MzCommandEntry pCommand, int pIndex) =>
        pIndex < pCommand.Parameters.Count
        && int.TryParse(
            pCommand.Parameters[pIndex],
            System.Globalization.NumberStyles.Integer,
            System.Globalization.CultureInfo.InvariantCulture, out var value)
            ? value
            : 0;

    /// <summary>
    /// One list in a run. It carries **only the interpreter**, and that is a
    /// decision and not an oversight: a first draft kept the map and the event
    /// here as well, and a mutation run then found that two of the three ways of
    /// reading them are the same in every state the reader can reach — the
    /// frame's copy always matched its interpreter's. Two copies of one truth
    /// is how a common event's event id came back from the dead in a first
    /// draft, and it is cheaper to have one.
    /// </summary>
    private sealed class Frame
    {
        public Frame(MzInterpreter pInterpreter) => Interpreter = pInterpreter;

        public MzInterpreter Interpreter { get; }
    }
}
