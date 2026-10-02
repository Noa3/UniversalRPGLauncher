using System;
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

    /// <summary>
    /// The number of the command at this index, or zero when the index is
    /// outside the list.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this exists because <c>command108</c> looks ahead.</strong>
    /// The engine's comment reader is
    /// <c>while (this.nextEventCode() === 408)</c>, **and that is a question
    /// about the command after this one without moving the index.**
    /// </para>
    /// <para>
    /// <strong>And a reader that moved the index to ask would skip a
    /// comment's own lines</strong>, **and a reader that copied the list
    /// would be a second copy of a list that can be a hundred and twenty
    /// entries long.**
    /// </para>
    /// </remarks>
    public int PeekCode(IReadOnlyList<MzCommandEntry> pCommands, int pIndex)
    {
        return pIndex >= 0 && pIndex < pCommands.Count
            ? pCommands[pIndex].Code
            : 0;
    }

    /// <summary>
    /// Steps the index over a comment's own lines and says how many there
    /// were.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the engine does this in the command, not after it:</strong>
    /// <c>while (this.nextEventCode() === 408) { this._index++;
    /// this._comments.push(this.currentCommand().parameters[0]); }</c>.
    /// <strong>And a reader that does not do it runs every line as a
    /// command of its own</strong> -- **and measured at
    /// <c>D:/Itch/sister/www</c> that is 1908 commands of a game whose
    /// writer wrote none of them.**
    /// </para>
    /// <para>
    /// <strong>And the index is moved before the command itself, so the
    /// comment's lines are not counted again as commands.</strong>
    /// </para>
    /// </remarks>
    public int SkipCommentLines()
    {
        var zeilen = 0;
        while (Index + 1 < Commands.Count
            && Commands[Index + 1].Code == MzCommandTable.CommentLine)
        {
            Index++;
            zeilen++;
        }

        return zeilen;
    }

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
        // **A run that starts past the end of the list says so.**
        //
        // **`IsRunning` is `Index < _commands.Count`**, so a `while` built on
        // it is never entered when the index is already beyond the list — and
        // a caller that was told nothing would be left with `Stopped ==
        // Stepped`, which is the answer for "I have not run yet".
        //
        // **A first draft had no check here**, and a truncated fixture — a
        // file cut off in the middle of a command, which is what a
        // half-written event file looks like — left the caller with an
        // interpreter that had neither run nor stopped. **A guard that is
        // never asked is not a guard**, and the index guard in `ExecuteOne`
        // was exactly that until this one existed beside it.
        if (!IsRunning)
        {
            Stopped = MzStep.Finished;
            Reason =
                $"the event's list has {Commands.Count} commands and the"
                + $" index is at {Index}, so it ran off the end of its list"
                + " before it started";
            return;
        }

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
        // **The index is inside the list, and that is not a claim — it is
        // `Run`'s job to keep it so.**
        //
        // **A first draft put a guard here**, on the reasoning that a 101
        // moves the index by however many commands it swallowed and could
        // therefore land past the end. It can, and it does — and
        // **`IsRunning` is `Index < _commands.Count`**, so `Run`'s `while` is
        // never entered with the index beyond the list, and the guard was
        // **never asked.**
        //
        // **A mutation that switched it off passed every test in the file**,
        // which is what a guard with no test looks like from the outside. The
        // honest repair is not a test for the guard but **its removal**: the
        // case it was written for is handled one level up, where it is
        // reachable, and a second check that can never fire is a claim a
        // reader will believe and nobody can prove.
        //
        // **What a truncated file does get** is said in `Run`, which checks
        // before it enters its loop and says where the index was.

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
        // **Und ein `355` wird nicht ausgefuehrt, und der Block
        // dahinter wird in einem Zug ueberlesen.**
        //
        // **Und das ist gemessen an `command355`: `let script =
        // this.currentCommand().parameters[0] + "\n"; while
        // (this.nextEventCode() === 655) { this._index++; script +=
        // this.currentCommand().parameters[0] + "\n"; } eval(script);`**
        // -- **und die `while` ist die des Motors, und sie setzt den
        // Index einmal je Zeile** -- **und `eval` fuehrt dieser Leser
        // nicht aus, und das ist eine Grenze dieses Repositorys, keine
        // Luecke in ihm.**
        //
        // **Und gemessen ist, was ohne diese Regel passiert:** **die
        // Fixture Map002 Event 6 hat 83 Befehle, davon 62 `655`, und der
        // Leser ging an dem 355 vorbei, treating each 655 as a command
        // with no effect, one step at a time, until the run froze at
        // 100000 commands.**
        if (command.Code == MzCommandTable.Script)
        {
            var zeilen = new List<string>();
            if (command.Parameters.Count > 0)
            {
                zeilen.Add(command.Parameters[0]);
            }

            while (Index + 1 < _commands.Count
                && _commands[Index + 1].Code == MzCommandTable.ScriptLine)
            {
                Index++;
                zeilen.Add(_commands[Index].Parameters.Count > 0
                    ? _commands[Index].Parameters[0]
                    : "");
            }

            // **Und es ist ein Hinweis, und keine Verweigerung.**
            //
            // **Und das ist gemessen an `executeCommand`:** `if (typeof
            // this[methodName] === "function") { if (!this[methodName]
            // (command.parameters)) return false; } this._index++;`
            // -- **und `command355` gibt `true` zurueck**, -- **und
            // `update()` laeuft weiter.**
            //
            // **Und `355` hat eine Seite ohne `655`: dann ist der Block
            // leer, und `this._index++` fuehrt auf den Index nach dem
            // Block, und genau das macht diese Regel.**
            Hinweise.Add(
                "the page runs the author's own JavaScript, and this"
                + " repository does not run it; the block is"
                + $" {zeilen.Count} line(s) long and starts with"
                + $" \"{(zeilen.Count > 0 ? zeilen[0] : "(empty)")}"
                + "\", and the"
                + " index now stands on the command after the block");

            // **Und der Index geht hinter den Block, und immer.**
            //
            // **Und gemessen an `executeCommand`: `this._index++;` steht
            // nach dem Aufruf, und ohne Bedingung.** **Und
            // `IsRunning` ist `Index < _commands.Count`** -- **und
            // also beendet das Hochzaehlen ueber das Ende die Liste
            // genauso, wie es der Motor tut.**
            //
            // **Und die Bedingung, die ich zuerst schrieb, verhinderte
            // genau den Fall, den ein Test prueft** -- **eine Liste aus
            // einem einzigen `355`, ohne `0` am Ende** -- **und liess
            // den Lauf bei Index 0 stehen, immer.**
            Index++;
            return true;
        }

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
        // **Und der Index wird nur dann weitergesetzt, wenn der
        // Befehl ihn nicht selbst weitergesetzt hat.**
        //
        // **Der Motor: `command101` liest seine Zeilen mit
        // `while (this.nextEventCode() === 401) { this._index++; ... }`,
        // und `_index` steht hinter der letzten Zeile, wenn der Befehl
        // zurueckkommt** -- **und `Interpreter.update` zaehlt danach
        // nicht noch einmal.** **Dasselbe gilt fuer `102`, das seine
        // Optionen isst, und fuer jeden Befehl, der einen zweiten
        // liest.**
        //
        // **Und diese Datei tat beides, und der Fehler fiel erst an
        // einer Seite mit 211 Befehlen auf.** **Gemessen dort:**
        // **`101` bei Index 1 mit vier Zeilen setzt den Index auf 6,
        // **und `Index++` machte daraus 7** -- **und damit lief der
        // `205` bei Index 6 nie, und der `101` bei 12 wurde
        // uebersprungen, und der `401` bei 16 kam ohne Dialog an den
        // Dispatcher**, **der zurueckweist, weil eine Zeile ohne `101`
        // darueber keine Zeile ist.**
        //
        // **Und ein Test, der nur zwei Befehle gemacht haette, das nie
        // gesehen** -- **denn nach dem zweiten Befehl ist der Fehler
        // noch nicht erreicht, und der Dialog steht.**
        if (!IndexWeitergesetzt)
        {
            Index++;
        }

        IndexWeitergesetzt = false;
        if (Stopped != MzStep.Stepped)
        {
            return false;
        }

        return true;
    }

    /// <summary>Whether the command that just ran moved the index.</summary>
    /// <remarks>
    /// <strong>And this is measured, and not guessed.</strong> A command
    /// that reads its own lines sets <c>_index</c> itself, <strong>and
    /// the engine's loop does not step it again.</strong> <strong>A
    /// reader that stepped it twice skipped one command in every five</strong>
    /// <strong>and read a dialogue line as a line of its own</strong> ---
    /// <strong>which is the second half of that mistake, and the half
    /// that is visible.</strong>
    /// </remarks>
    public bool IndexWeitergesetzt { get; set; }

    /// <summary>
    /// The branch result for the indent a command sits at, and the first time a
    /// command at that indent is met there is none.
    /// </summary>
    public bool? BranchAt(int pIndent) =>
        _branch.TryGetValue(pIndent, out var value) ? value : null;

    /// <summary>
    /// Whether the slot holds nothing at all, and how <c>403</c> reads
    /// that.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this exists because JavaScript's <c>ToNumber</c> makes
    /// two different things out of the same <c>null</c>.</strong>
    /// </para>
    /// <para>
    /// <strong><c>undefined &gt;= 0</c> is false</strong> -- <strong>because
    /// <c>undefined</c> becomes <c>NaN</c> and every comparison with
    /// <c>NaN</c> is false</strong> -- <strong>and <c>null &gt;= 0</c> is
    /// true</strong>, <strong>because <c>null</c> becomes <c>+0</c>.</strong>
    /// <strong>So <c>command403</c> skips after a <c>jumpTo</c> crossed
    /// the indent and does not skip on an indent no branch ever
    /// wrote.</strong>
    /// </para>
    /// <para>
    /// <strong>And this reader's branch slot is <c>bool?</c>, where
    /// <c>null</c> means both of those.</strong> <strong>A guard of
    /// <c>!= false</c> therefore skips in both, and that is the wrong
    /// answer for the first of them</strong> -- <strong>and it is
    /// measurable: a <c>403</c> at an indent no branch decided stepped
    /// over the command after it.</strong>
    /// </para>
    /// <para>
    /// <strong>So the slot carries three states and not two</strong>:
    /// <c>MzBranch.Undecided</c> for the slot nothing wrote,
    /// <c>MzBranch.Crossed</c> for the one a <c>jumpTo</c> left,
    /// <strong>and <c>true</c> or <c>false</c> for the one a
    /// <c>111</c> decided.</strong>
    /// </para>
    /// </remarks>
    public MzBranchState StateAt(int pIndent) =>
        _branch.TryGetValue(pIndent, out var wert)
            ? wert.HasValue
                ? MzBranchState.Decided
                : MzBranchState.Crossed
            : MzBranchState.Undecided;

    public void SetBranch(int pIndent, bool? pValue) => _branch[pIndent] = pValue;

    /// <summary>
    /// Steps over every command that sits inside the branch this one opens, the
    /// way the engine's <c>skipBranch</c> does, and says so when the list ends
    /// before the branch does.
    /// </summary>
    /// <summary>
    /// The options a choice is made of, read off the <c>402</c>s that
    /// follow a <c>102</c>, and where the choice ends.
    /// </summary>
    /// <param name="pOptions">The option texts, in the game's order.</param>
    /// <param name="pEnd">The index of the first command after the
    /// choice.</param>
    /// <remarks>
    /// <para>
    /// <strong>And the options are not in the <c>102</c> at all.</strong>
    /// Measured: <c>102 [["Yes", "No"], 1, 0, 2, 0]</c> is followed by
    /// <c>402 [0, "Yes"]</c> and <c>402 [1, "No"]</c> — <strong>and the
    /// second parameter of the <c>102</c> is the *cancel branch*, not
    /// the number of options</strong>, **so a reader that read it as a
    /// count opened a choice of one.**
    /// </para>
    /// <para>
    /// <strong>And a choice ends where its indent ends.</strong> The
    /// <c>102</c> and its <c>402</c>s are at indent 0,
    /// <strong>their branches at 1</strong>, **and a reader that looked
    /// for a <c>412</c> to close it found none**, **because the
    /// branches close themselves and the <c>412</c> belongs to an
    /// ordinary <c>411</c> branch.**
    /// </para>
    /// </remarks>
    /// <summary>
    /// Remembers the open choice: what the player was asked and where
    /// each answer leads.
    /// </summary>
    /// <param name="pChoice">The choice the game wrote.</param>
    /// <param name="pBranches">Each option's branch, by its own index.</param>
    /// <remarks>
    /// <para>
    /// <strong>And the branches are a map, and not a list.</strong>
    /// Measured: <c>402 [0, "Yes"]</c>, <c>402 [1, "No"]</c> —
    /// <strong>the index is in the first parameter, and it counts from
    /// zero.</strong> A list indexed by it would be off by one on every
    /// branch, <strong>and a choice of two would jump into the third
    /// thing in the list.</strong>
    /// </para>
    /// </remarks>
    public void SetChoice(
        MzChoice.Set pChoice, IReadOnlyDictionary<int, int> pBranches)
    {
        _choice = pChoice;
        _choiceBranches = pBranches;
    }

    /// <summary>The options of the open choice, and none without one.</summary>
    public IReadOnlyList<string> ChoiceOptions => _choice.Options;

    /// <summary>Whether a choice is open on this interpreter.</summary>
    public bool HasChoice => _choice.Options.Count > 0;

    /// <summary>
    /// Where the answer <c>pBranch</c> leads, or -1.
    /// </summary>
    /// <param name="pBranch">Which answer, counting from one.</param>
    public int ChoiceBranch(int pBranch)
    {
        return _choiceBranches.TryGetValue(pBranch - 1, out var ziel)
            ? ziel
            : -1;
    }

    /// <summary>
    /// Steps over an option line that was reached on its own.
    /// </summary>
    /// <remarks>
    /// <strong>And an option is never its own command.</strong> It is a
    /// line the <c>102</c> reads, <strong>and a reader that executed one
    /// on its own ran a command the engine has no method for.</strong>
    /// </remarks>
    public void SkipChoiceOption() => Index++;

    private MzChoice.Set _choice = MzChoice.Read(System.Array.Empty<string>());

    private IReadOnlyDictionary<int, int> _choiceBranches =
        new Dictionary<int, int>();


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
    /// Holds the run up until a <b>condition</b> is met, which is the engine's
    /// <c>setWaitMode</c> and not its <c>wait</c>.
    /// </summary>
    /// <remarks>
    /// <b>These are two different waits and conflating them is a real
    /// failure.</b> A 230 sets a count, and a caller passes frames until it is
    /// zero. A 201 sets a mode, and the engine asks a question every frame:
    /// <c>waiting = $gamePlayer.isTransferring()</c>. A condition wait has no
    /// length, so a caller passing frames cannot end it and must carry out the
    /// transfer — and until it does, the run stays exactly where it is.
    ///
    /// **The index does not move**, which is what makes it a wait at all: the
    /// 201 has run, the page is held after it, and the commands that follow do
    /// not start until the map has actually changed.
    /// </remarks>
    public void WaitFor(MzWaitMode pMode)
    {
        WaitMode = pMode;
        Stopped = MzStep.Waiting;
        Reason = pMode == MzWaitMode.Transfer
            ? "waiting for the reserved transfer to be carried out, at index"
                + $" {Index}"
            : $"waiting for {pMode} at index {Index}";
    }

    /// <summary>
    /// Stops the run without a frame to wait for, and says why.
    /// </summary>
    /// <remarks>
    /// **Not the same as a wait and not the same as a stop.** A 201 in a battle
    /// or with a message on the screen returns false from the engine with
    /// nothing set — the index stays, the list is not over, and the frame in
    /// which the message closes is the frame in which the transfer happens.
    /// That is neither "the event finished" nor "the event is waiting for N
    /// frames", so it gets its own answer rather than being dressed up as one
    /// of the two.
    /// </remarks>
    public void Refuse(string pReason)
    {
        Stopped = MzStep.Refused;
        Reason = pReason;
    }

    /// <summary>Why the run is being held up, when the answer is a condition
    /// and not a count.</summary>
    public MzWaitMode WaitMode { get; private set; } = MzWaitMode.None;

    /// <summary>What was reported and not run, in the order it happened.</summary>
    /// <remarks>
    /// <strong>And this is here because a 355 is reported and not
    /// executed</strong>, <strong>and a refusal would stop the page where
    /// the game carries on</strong> — <strong>and measured at
    /// <c>executeCommand</c>: <c>command355</c> returns <c>true</c>, and
    /// the run goes on.</strong> <para>
    /// <strong>And this is the same list <c>MzBranchFacts.Notices</c>
    /// carries</strong>, <strong>and a caller reading one of them learns
    /// the same thing.</strong>
    /// </para>
    /// </remarks>
    public List<string> Hinweise { get; } = new();

    /// <summary>
    /// Counts one frame off a wait, which is what the engine does before each
    /// frame, and says whether the wait is over.
    /// </summary>
    public bool PassFrame(Func<MzWaitMode, bool>? pCheck = null)
    {
        // **A condition wait is asked about, not counted down.** The engine's
        // `updateWaitMode` is a question every frame, and a first draft of
        // this reader counted frames here for a transfer as well — which made
        // a page wait for a number of frames and then carry on with the player
        // still on the old map. `pCheck` is the caller's answer to
        // `isTransferring()`.
        if (WaitMode != MzWaitMode.None)
        {
            if (pCheck != null && pCheck(WaitMode))
            {
                WaitMode = MzWaitMode.None;
                Stopped = MzStep.Stepped;
                return true;
            }
            return false;
        }

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
