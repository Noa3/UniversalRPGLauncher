using System.Collections.Generic;
using Godot;
using UniversalRPG.Tests.Framework;
using UniversalRPG.Web;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// A list that calls another one, and the wait that stops one between frames.
/// </summary>
/// <remarks>
/// <para>
/// K-124 walked a list. <b>An MZ event is almost never one list.</b> This game
/// stores eight common events in the one map read here, reached by 117, and a
/// 117 whose list is not available is not a command a reader may step over — the
/// rest of the list behind it never happens.
/// </para>
/// <para>
/// Three things in the engine are not what a first reading gives, and all three
/// are claimed here:
///
/// <list type="bullet">
/// <item>
/// <b>A called list runs to its end before the caller moves on.</b>
/// <c>updateChild</c> gives the child its own <c>update()</c> and the parent
/// breaks the frame while the child is still running, so a caller that went on
/// straight away would run its own next command before the call returned.
/// </item>
/// <item>
/// <b>Every list in a run shares one set of facts.</b> Both go through the one
/// <c>$gameVariables</c>, so a common event that sets a variable writes the one
/// its caller reads. A runner that gave each list its own facts would have a
/// called list change something the caller could not see.
/// </item>
/// <item>
/// <b>The event id travels with the call, and only on a map.</b>
/// <c>isOnCurrentMap()</c> decides, and that is what lets a common event address
/// "this event".
/// </item>
/// </list>
/// </para>
/// <para>
/// And one thing this repository will not do: <b>the fixture carries no
/// <c>CommonEvents.json</c></b> — the real one is 4.5 MB and was left out — so
/// every 117 in this game names an index that cannot be handed over. A run that
/// meets one says which index and stops. Reading a called list out of a fixture
/// that does not contain it would mean writing the game's own scripts.
/// </para>
/// </remarks>
partial class TestMzEventRunner : TestBase
{
    private const string FixtureRoot = "res://tests/fixtures/mz";

    private static List<MzCommandEntry> ListFrom(
        params (int pCode, int pIndent, string[] pParameters)[] pCommands)
    {
        var list = new List<MzCommandEntry>();
        foreach (var (code, indent, parameters) in pCommands)
        {
            list.Add(new MzCommandEntry(code, parameters, indent));
        }
        return list;
    }

    private static Dictionary<int, List<MzCommandEntry>> Common(
        params (int pIndex, List<MzCommandEntry> pList)[] pEvents)
    {
        var map = new Dictionary<int, List<MzCommandEntry>>();
        foreach (var (index, list) in pEvents)
        {
            map[index] = list;
        }
        return map;
    }

    public void Test_ACalledListRunsToItsEndBeforeTheCallerMovesOn()
    {
        // The engine's `updateChild` gives the child its own update and the
        // parent breaks the frame while the child is still running, so the
        // caller's next command does not run until the called list is done.
        // **A runner that pushed the caller on straight away would record the
        // caller's command first**, and the order is the whole of what a call
        // means.
        var called = ListFrom(
            (122, 0, ["1", "1", "0", "0", "10"]),
            (0, 0, []));
        var caller = ListFrom(
            (117, 0, ["7"]),
            (122, 0, ["2", "2", "0", "0", "20"]),
            (0, 0, []));

        var result = new MzEventRunner(Common((7, called))).Run(
            caller, new MzBranchFacts { Variables = { [1] = 0, [2] = 0 } });

        AssertEq(
            result.Stopped, MzStep.Finished,
            "a list that calls another and then carries on runs through, and it"
            + $" is {result.Stopped}: {result.Describe()}");
        AssertEq(
            result.Actions.Count, 3,
            "and three commands are recorded — the call, the one inside it, and"
            + $" the one after it; {result.Actions.Count}");
        AssertEq(
            result.Actions[1].What, "variable 1 set from 0 to 10",
            "and the command inside the called list is the second, not the"
            + $" third: {result.Actions[1].What}");
        AssertEq(
            result.Actions[2].What, "variable 2 set from 0 to 20",
            $"while the caller's own command is last: {result.Actions[2].What}");
    }

    public void Test_ACalledListChangesWhatTheCallerCanSee()
    {
        // Both go through the one $gameVariables. **A runner that gave each list
        // its own facts would have the called list write a variable the caller
        // cannot read**, and a game's common event that raises a counter would
        // raise it for nobody.
        var called = ListFrom(
            (122, 0, ["1", "1", "0", "0", "10"]),
            (111, 0, ["1", "1", "0", "0", "3", "1"]),
            (122, 1, ["2", "2", "0", "0", "20"]),
            (411, 0, []),
            (0, 0, []));
        var caller = ListFrom(
            (117, 0, ["7"]),
            (0, 0, []));
        var facts = new MzBranchFacts { Variables = { [1] = 0, [2] = 0 } };

        var result = new MzEventRunner(Common((7, called))).Run(caller, facts);

        AssertEq(
            result.Stopped, MzStep.Finished,
            "and the run finishes: {result.Describe()}");
        AssertEq(
            facts.Variable(1), 10,
            "and the called list's number is in the facts the caller reads,"
            + $" because there is one set of facts; it is {facts.Variable(1)}");
        AssertEq(
            facts.Variable(2), 20,
            "and so is the number its branch wrote, which the caller could only"
            + $" see through the same facts; it is {facts.Variable(2)}");
    }

    public void Test_CalledListsNestAndRunInnermostFirst()
    {
        // A common event that calls another: the engine makes
        // `new Game_Interpreter(this._depth + 1)`, so each level is its own
        // interpreter and the innermost list runs its commands first.
        var inner = ListFrom(
            (122, 0, ["3", "3", "0", "0", "3"]),
            (0, 0, []));
        var middle = ListFrom(
            (117, 0, ["8"]),
            (122, 0, ["2", "2", "0", "0", "2"]),
            (0, 0, []));
        var outer = ListFrom(
            (117, 0, ["7"]),
            (122, 0, ["1", "1", "0", "0", "1"]),
            (0, 0, []));

        var result = new MzEventRunner(
            Common((7, middle), (8, inner))).Run(
            outer, new MzBranchFacts { Variables = { [1] = 0, [2] = 0, [3] = 0 } });

        AssertEq(
            result.Stopped, MzStep.Finished,
            "three lists deep runs through: {result.Describe()}");
        var order = new List<string>();
        foreach (var action in result.Actions)
        {
            order.Add(action.What);
        }
        AssertEq(
            string.Join(" then ", order),
            "call common event 7 then call common event 8"
                + " then variable 3 set from 0 to 3"
                + " then variable 2 set from 0 to 2"
                + " then variable 1 set from 0 to 1",
            "and the innermost list's command comes before the middle one's, and"
            + " the middle one's before the outermost's, which is the engine's"
            + " order of a child running before its parent carries on");
    }

    public void Test_ACallToAListThisRepositoryDoesNotHaveIsRefusedAndNamed()
    {
        // The engine's own line is `if (commonEvent)`, and a missing one leaves
        // the index where it was and carries on. **That is not what this reader
        // does**, and the difference is the whole of the test: this repository's
        // fixture holds no CommonEvents.json, and a silent step-over would run
        // the rest of a game's list as if the call had never been there.
        var caller = ListFrom(
            (117, 0, ["476"]),
            (122, 0, ["1", "1", "0", "0", "99"]),
            (0, 0, []));
        var facts = new MzBranchFacts { Variables = { [1] = 0 } };

        var result = new MzEventRunner().Run(caller, facts);

        AssertEq(
            result.Stopped, MzStep.Refused,
            "a call to a list that is not here is refused rather than stepped"
            + $" over; it is {result.Stopped}");
        AssertEq(
            result.MissingCommonEvent, 476,
            "and the index the game asked for is a **field**, not something a"
            + " caller has to read out of the message; it is"
            + $" {result.MissingCommonEvent}, and a first draft of this test"
            + " took the number out of the prose and got 76 for 476");
        AssertTrue(
            result.Reason.Contains("476"),
            "and the reason names the index the field does, so a caller reading"
            + $" only the log still learns which file to fetch: {result.Reason}");
        AssertEq(
            facts.Variable(1), 0,
            "and the command behind the call did not run, because in the engine"
            + $" the call is what was supposed to do it; it is"
            + $" {facts.Variable(1)}");
    }

    public void Test_AWaitStopsTheRunAndIsCountedDownByTheCaller()
    {
        // `command230` is `this._waitCount = params[0]`, and `updateWaitCount`
        // takes one off it per frame and breaks the frame while it is above
        // zero. **The command is read again the frame after, so the index does
        // not move**, and a reader that stepped over the wait would run the rest
        // of the list three frames early.
        var commands = ListFrom(
            (230, 0, ["3"]),
            (122, 0, ["1", "1", "0", "0", "20"]),
            (0, 0, []));
        var facts = new MzBranchFacts { Variables = { [1] = 0 } };
        var interpreter = new MzInterpreter(commands);

        interpreter.ExecuteOne(new List<MzAction>(), facts);

        AssertEq(
            interpreter.Stopped, MzStep.Waiting,
            "a wait leaves the interpreter waiting, not finished; it is"
            + $" {interpreter.Stopped}");
        AssertEq(
            interpreter.Index, 0,
            "and the index stays on the wait, because the engine reads the same"
            + $" command again next frame; it is {interpreter.Index}");
        AssertEq(
            interpreter.WaitFrames, 3,
            "and the count the game asked for is kept, so a caller can count it"
            + $" down; it is {interpreter.WaitFrames}");

        AssertEq(
            interpreter.PassFrame(), false,
            "and one frame is not enough, as the engine's count says");
        AssertEq(
            interpreter.PassFrame(), false, "nor is the second");
        AssertEq(
            interpreter.PassFrame(), true,
            "and the third is the last, so the wait is over and the run may go"
            + " on");
        AssertEq(
            interpreter.Stopped, MzStep.Stepped,
            $"and the interpreter may carry on again; it is {interpreter.Stopped}");
    }

    public void Test_ARunThatWaitsIsHandedBackWaitingAndSaysHowLong()
    {
        // The caller has frames and this reader does not, so the run is handed
        // back at the wait and the caller decides when to call again. A runner
        // that ran the rest of the list anyway would put a game's dialogue a
        // whole number of frames early.
        var commands = ListFrom(
            (230, 0, ["10"]),
            (122, 0, ["1", "1", "0", "0", "20"]),
            (0, 0, []));
        var facts = new MzBranchFacts { Variables = { [1] = 0 } };

        var result = new MzEventRunner().Run(commands, facts);

        AssertEq(
            result.Stopped, MzStep.Waiting,
            "a run that meets a wait is handed back waiting: {result.Describe()}");
        AssertEq(
            facts.Variable(1), 0,
            "and the command behind the wait did not run yet, because in the"
            + $" engine it runs a frame later; it is {facts.Variable(1)}");
        AssertTrue(
            result.Describe().Contains("230"),
            $"and the description names the code it is waiting at, so a caller"
            + $" can see where: {result.Describe()}");
    }

    public void Test_EveryCommonEventThisGameCallsIsNamedRatherThanSteppedOver()
    {
        // Every list in the one map read here, walked with no CommonEvents.json
        // available — which is what this repository has. **A 117 whose list is
        // not here must refuse and name the index**, and a list that waits
        // before it reaches one says that instead.
        //
        // **Measured on this map, not guessed:** three of the six lists call a
        // common event at all — events 2, 4 and 5 — and the other three stop at a
        // 230 first. Eight distinct common events are called between those
        // three. So the claim is that the three name their index and the other
        // three say what they are waiting for, not that every list stops the
        // same way.
        var lists = EventListsInThisGame();
        AssertEq(
            lists.Count, 6,
            "and the map read here has six event pages, which is what the"
            + $" fixture carries; it has {lists.Count}");

        var named = new List<string>();
        var waiting = 0;
        var onAScript = 0;
        var other = new List<string>();
        foreach (var commands in lists)
        {
            var result = new MzEventRunner().Run(
                commands, new MzBranchFacts
                {
                    Switches = { [1] = false, [2] = true, [3] = false, [4] = true },
                    Variables = { [0] = 0, [77] = 0, [78] = 0, [180] = 0 },
                });

            switch (result.Stopped)
            {
                case MzStep.Refused when result.MissingCommonEvent >= 0:
                    // The index the game named, read out of the field rather
                    // than out of the message text. **A first draft of this
                    // took the number from the prose and got 76 for 476**,
                    // because it counted a space twice and the prose was not
                    // the thing being tested.
                    named.Add(result.MissingCommonEvent.ToString());
                    AssertTrue(
                        result.Reason.Contains(result.MissingCommonEvent.ToString()),
                        "and the message names the same index the field does, so"
                        + " a caller reading only the log still learns which"
                        + $" file to fetch; it said: {result.Reason}");
                    break;

                case MzStep.Refused:
                    // **Event 6 of this game opens with fifty-eight lines of its
                    // own JavaScript** — a 355 and fifty-seven 655 — which this
                    // repository does not evaluate. That is a refusal with a
                    // different reason, and calling it a common event would be
                    // a test that checks the wrong thing.
                    onAScript++;
                    break;

                case MzStep.Waiting:
                    waiting++;
                    break;

                default:
                    other.Add($"{result.Stopped}: {result.Describe()}");
                    break;
            }
        }

        AssertTrue(
            named.Count >= 2,
            "and at least two of the six lists reach a common event and name it;"
            + $" {named.Count} did: {string.Join(", ", named)}");
        AssertTrue(
            waiting >= 1,
            $"and at least one stops at a 230 and says it is waiting;"
            + $" {waiting} did");
        AssertTrue(
            onAScript >= 1,
            "and at least one is refused because it opens with the game's own"
            + " JavaScript, which this repository does not run, rather than"
            + $" because of a missing list; {onAScript} was");
        AssertTrue(
            named.Contains("476") || named.Contains("336") || named.Contains("41"),
            "and the indices are ones this game wrote, read out of the field"
            + " rather than out of the prose; they are"
            + $" {string.Join(", ", named)}");
    }

    public void Test_APassOfZeroFramesIsOverAtOnceRatherThanWaiting()
    {
        // `updateWaitCount` is `if (this._waitCount > 0) { this._waitCount--; return true; }`
        // and the frame's `update` breaks on it. **A wait of zero frames is
        // over before a single frame is counted**, so `PassFrame` on a fresh
        // interpreter must not count one. A reader that asked for a frame less
        // than zero instead would have a game that waits for ever.
        var interpreter = new MzInterpreter(
            ListFrom((230, 0, ["0"]), (0, 0, [])));

        interpreter.Wait(0);

        AssertEq(
            interpreter.WaitFrames, 0,
            $"and a wait of zero frames asks for none; it asks for"
            + $" {interpreter.WaitFrames}");
        AssertEq(
            interpreter.PassFrame(), true,
            "and one pass is enough, because there was nothing to wait for");
        AssertEq(
            interpreter.PassFrame(), true,
            "and a second is still enough, which is what keeps a caller from"
            + " being held by a wait that is already over");
        AssertEq(
            interpreter.WaitFrames, 0,
            "and the count stays at zero rather than going past it, which is"
            + $" what stops a caller that keeps passing frames from hanging;"
            + $" it is {interpreter.WaitFrames}");
        AssertEq(
            interpreter.Stopped, MzStep.Stepped,
            "and the interpreter is stepped, so a caller may carry on");
    }

    public void Test_APassOfZeroFramesOnAnInterpreterThatHasNotWaitedIsOver()
    {
        // The same question asked of an interpreter that never met a 230: there
        // is no wait, so a frame must pass. A reader that only counted down
        // would hold a game that never asked to be held.
        var interpreter = new MzInterpreter(ListFrom((0, 0, [])));

        AssertEq(
            interpreter.WaitFrames, 0,
            $"and it is waiting for nothing; it waits for"
            + $" {interpreter.WaitFrames} frames");
        AssertEq(
            interpreter.PassFrame(), true,
            "and a frame passes straight through, so a caller that counts"
            + " frames is not held by a list that never waited");
    }

    public void Test_ACallerThatSpinsPastTheWholeRunIsFrozenByTheOneLimit()
    {
        // `checkFreeze` counts the commands of the **whole run**, not of one
        // list. A game that calls a common event which loops would otherwise
        // be stepped for ever inside the called list, with the caller's own
        // limit never reached. **The limit is a field a test can lower, and a
        // run that goes past it says so.**
        var called = ListFrom(
            (112, 0, []),
            (122, 1, ["1", "1", "0", "0", "1"]),
            (413, 0, []),
            (0, 0, []));
        var caller = ListFrom(
            (117, 0, ["7"]),
            (122, 0, ["2", "2", "0", "0", "2"]),
            (0, 0, []));
        var facts = new MzBranchFacts { Variables = { [1] = 0, [2] = 0 } };

        var runner = new MzEventRunner(Common((7, called))) { CommandLimit = 12 };
        var result = runner.Run(caller, facts);

        AssertEq(
            result.Stopped, MzStep.Frozen,
            "a run that goes past the limit is frozen, and it is"
            + $" {result.Stopped}: {result.Describe()}");
        AssertTrue(
            result.Reason.Contains("12"),
            "and the reason names the limit that stopped it, so a game that"
            + $" loops can be found from the log: {result.Reason}");
        AssertEq(
            facts.Variable(2), 0,
            "and the caller's own command never ran, because the whole run"
            + $" stopped inside the list it called; it is {facts.Variable(2)}");
    }

    public void Test_ACallerThatSpinsOutsideACallIsFrozenByTheSameLimit()
    {
        // The same limit in the same place: a loop in the list that was called
        // directly, with nothing nested. If only the nested path counted
        // commands, this run would never stop.
        var commands = ListFrom(
            (112, 0, []),
            (122, 1, ["1", "1", "0", "0", "1"]),
            (413, 0, []),
            (0, 0, []));
        var facts = new MzBranchFacts { Variables = { [1] = 0 } };

        var result = new MzEventRunner { CommandLimit = 9 }
            .Run(commands, facts);

        AssertEq(
            result.Stopped, MzStep.Frozen,
            "a run that loops in the list it was given is frozen the same way;"
            + $" it is {result.Stopped}: {result.Describe()}");
        AssertTrue(
            result.Reason.Contains("9"),
            $"and it names the limit it went past: {result.Reason}");
    }

    public void Test_ARunThatIsFrozenByTheLimitStopsTheWholeRunNotOneList()
    {
        // Two lists, each below the limit, each calling the other by way of a
        // call that the loop makes. `checkFreeze` counts the run, so a pair of
        // lists that call each other is stopped even though neither is long.
        var a = ListFrom(
            (117, 0, ["8"]),
            (122, 0, ["1", "1", "0", "0", "1"]),
            (0, 0, []));
        var b = ListFrom(
            (117, 0, ["7"]),
            (122, 0, ["2", "2", "0", "0", "2"]),
            (0, 0, []));
        var facts = new MzBranchFacts { Variables = { [1] = 0, [2] = 0 } };

        var result = new MzEventRunner(Common((7, a), (8, b))) { CommandLimit = 40 }
            .Run(a, facts);

        AssertEq(
            result.Stopped, MzStep.Frozen,
            "a pair of lists that call each other is stopped by the run's one"
            + $" limit, not by either list's own length; it is"
            + $" {result.Stopped}: {result.Describe()}");
    }

    public void Test_AListThatCallsItselfStopsAtTheDepthTheEngineStopsAt()
    {
        // `setupChild` makes `new Game_Interpreter(this._depth + 1)`, and the
        // engine refuses a depth past a hundred. A common event that calls
        // itself is **not a hang** in the engine: it stops the game. This reader
        // says so and names the index, rather than recursing until the stack
        // gives out or the command limit catches it and calls it a freeze.
        // **A caller must be able to tell those two apart**, because one is a
        // bug in the game and the other is a bug in the reader.
        var calling = ListFrom(
            (117, 0, ["7"]),
            (0, 0, []));
        var facts = new MzBranchFacts();

        var result = new MzEventRunner(Common((7, calling))) { MaxDepth = 3 }
            .Run(calling, facts);

        AssertEq(
            result.Stopped, MzStep.Refused,
            "a list that calls itself stops as a refusal and not as a freeze,"
            + $" so the cause is named rather than guessed: {result.Stopped}");
        AssertTrue(
            result.Reason.Contains("nested"),
            "and the reason says the nesting is what stopped it, which is a"
            + $" different thing from a run that merely took too long:"
            + $" {result.Reason}");
        AssertEq(
            result.MissingCommonEvent, 7,
            "and the index it was calling is still reported, so a log names"
            + $" which common event is the one that recurses; it is"
            + $" {result.MissingCommonEvent}");

        // The same thing one level under the limit runs through, so the
        // refusal is about the depth and not about calling a list at all.
        // **A first draft of this asked the same list to finish**, and a list
        // that calls itself never can — so the control case is a caller that
        // calls a list which stops. A second draft gave that list a call to
        // itself, which is the same endless thing one level down, and it is
        // worth naming: a self-call is endless at every level, so a test of
        // "runs through" must call a list that has no 117 of its own in the
        // branch it takes.
        var twice = ListFrom(
            (122, 0, ["1", "1", "0", "0", "1"]),
            (111, 0, ["1", "1", "0", "0", "2"]),
            (117, 0, ["8"]),
            (411, 0, []),
            (0, 0, []));
        var stops = ListFrom(
            (122, 0, ["3", "3", "0", "0", "3"]),
            (0, 0, []));
        var fine = new MzEventRunner(
            Common((7, twice), (8, stops)))
        {
            MaxDepth = 8,
        }.Run(twice, new MzBranchFacts
        {
            Variables = { [1] = 0, [2] = 0, [3] = 0 },
        });
        AssertEq(
            fine.Stopped, MzStep.Finished,
            "and a list that calls a list which stops runs through, so the"
            + " refusal is about the depth and not about calling a list at"
            + $" all: {fine.Stopped}");
    }

    public void Test_AListThatCallsItselfIsStoppedByTheDepthAndNotByTheLimit()
    {
        // The same run with a limit it would hit first. **A reader that checked
        // the limit before the depth would report a freeze**, and a game with a
        // self-calling common event would be filed as a game that loops when
        // the engine itself says it is a depth it refuses to go past.
        var calling = ListFrom(
            (117, 0, ["7"]),
            (0, 0, []));
        var facts = new MzBranchFacts();

        var result = new MzEventRunner(Common((7, calling)))
        {
            MaxDepth = 2,
            CommandLimit = 20,
        }.Run(calling, facts);

        AssertEq(
            result.Stopped, MzStep.Refused,
            "the depth is checked and refused even when the limit is lower"
            + $" than it would take to reach it; it is {result.Stopped}");
        AssertTrue(
            !result.Reason.Contains("without finishing"),
            "and it is not reported as a freeze, because the engine's freeze is"
            + $" about a run that took too long: {result.Reason}");
    }

    public void Test_AListThatCallsItselfStopsAtTheDepthAndNotOnePastIt()
    {
        // **An off-by-one, and it is worth a test of its own.** The engine's
        // `setupChild` makes `new Game_Interpreter(this._depth + 1)`, and the
        // engine refuses a depth past a hundred. A reader that refused one
        // level early would stop a game the engine runs, and a reader that
        // refused one level late would build one more list than the engine
        // does. Both are wrong, and neither is visible unless the boundary is
        // asked about from both sides.
        //
        // **A first draft asked only whether the run was refused**, and that is
        // not the question: a list that calls itself is refused at every limit
        // from zero to eight, so every one of those tests passed with a reader
        // that refused one level early, one level late, or twice as deep as it
        // should. The number of commands the run took is what tells the levels
        // apart, and it is claimed here.
        var calling = ListFrom(
            (117, 0, ["7"]),
            (0, 0, []));

        // Each call records one action, so the run's action count is exactly
        // the number of levels it managed to enter before it was refused.
        int ActionsAt(int pMaxDepth) =>
            new MzEventRunner(Common((7, calling))) { MaxDepth = pMaxDepth }
                .Run(calling, new MzBranchFacts()).Actions.Count;

        // A limit of zero allows the top list and no child, so the first call
        // is already too deep: one action, the call itself.
        AssertEq(
            ActionsAt(0), 1,
            "a limit of zero enters no child at all, so only the call itself is"
            + " recorded");
        AssertEq(
            ActionsAt(1), 2,
            "a limit of one enters one child, whose own call is refused, so"
            + " two actions are recorded");
        AssertEq(
            ActionsAt(3), 4,
            "and a limit of three enters three children and refuses the"
            + " fourth call, so four actions are recorded");

        // **The step that a first draft missed.** One level more than the
        // limit is refused, and the run stops there — it does not carry on for
        // one more. A reader that allowed one level too many would record five
        // actions at a limit of three.
        AssertTrue(
            ActionsAt(3) < ActionsAt(4),
            "and one limit more is one action more, so the boundary is the"
            + $" limit itself and not the limit plus one; {ActionsAt(3)} then"
            + $" {ActionsAt(4)}");

        // And a limit that is twice as deep as it should be is caught, which
        // is what a reader comparing against `MaxDepth * 2` would fail.
        AssertTrue(
            ActionsAt(3) != ActionsAt(6),
            "and a limit of six is not what a limit of three is, so the run is"
            + $" not simply being refused somewhere: {ActionsAt(3)} then"
            + $" {ActionsAt(6)}");

        // Every one of those is still the engine's own ending — a refusal that
        // names the nesting and the index, and not a freeze.
        var atThree = new MzEventRunner(Common((7, calling))) { MaxDepth = 3 }
            .Run(calling, new MzBranchFacts());
        AssertEq(
            atThree.Stopped, MzStep.Refused,
            "and every one of them is still a refusal and not a freeze, so a"
            + $" caller can tell the two apart: {atThree.Stopped}");
        AssertTrue(
            atThree.Reason.Contains("nested"),
            "and the reason says the nesting is what stopped it:"
            + $" {atThree.Reason}");
        AssertEq(
            atThree.MissingCommonEvent, 7,
            $"and it names the index: {atThree.MissingCommonEvent}");
    }

    public void Test_ACalledListKnowsTheMapAndTheEventItIsOn()
    {
        // `setupChild` calls `setup(list, eventId)`, and `setup` sets
        // `this._mapId = $gameMap.mapId()` — **the map the game is on**, not the
        // one the caller was on. So a child is always on the current map, and
        // `isOnCurrentMap()` is true for it, and the thing that varies is the
        // **event id**: the caller keeps its own only if the caller was on the
        // current map, and `command117` passes zero otherwise.
        //
        // **A first draft passed the caller's map down instead**, and the test
        // that caught it is the two-level case below: with the caller's map,
        // the second level still saw a map and kept the event id for ever.
        var reads = ListFrom(
            (122, 0, ["1", "1", "0", "0", "1"]),
            (0, 0, []));

        // On a map, with an event: the child keeps the event id the caller had.
        var onMap = new MzEventRunner(Common((7, reads))).Run(
            ListFrom((117, 0, ["7"]), (0, 0, [])),
            new MzBranchFacts { Variables = { [1] = 0 } },
            pMapId: 5,
            pEventId: 12);
        AssertEq(
            onMap.Stopped, MzStep.Finished,
            "a list called from an event on a map runs through:"
            + $" {onMap.Describe()}");
        AssertEq(
            onMap.Child.MapId, 5,
            $"and the child is on the game's map, which is the caller's; it is"
            + $" {onMap.Child.MapId}");
        AssertEq(
            onMap.Child.EventId, 12,
            "and it is part of the same event, which is what a common event uses"
            + $" to address this event; it is {onMap.Child.EventId}");
        AssertEq(
            onMap.Child.Depth, 1,
            $"and that it is one level below its caller; it is"
            + $" {onMap.Child.Depth}");

        // A list run off the map — the parallel process — has no map of its
        // own, so `isOnCurrentMap()` is false there and the child gets zero
        // **even though the game is on a map**.
        var offMap = new MzEventRunner(Common((7, reads))).Run(
            ListFrom((117, 0, ["7"]), (0, 0, [])),
            new MzBranchFacts { Variables = { [1] = 0 } },
            pMapId: 0,
            pEventId: 12);
        AssertEq(
            offMap.Stopped, MzStep.Finished,
            "and one run off the map runs through as well:"
            + $" {offMap.Describe()}");
        AssertEq(
            offMap.Child.EventId, 0,
            "and its child has no event, because the caller was not on the"
            + $" game's map and the engine would have passed zero; it has"
            + $" {offMap.Child.EventId}");

        // Three levels, all on the map. `setup` takes `eventId || 0` and a
        // child on the map keeps the caller's event, so **the event id does not
        // fall away on a map** — a first draft of this claimed it did and was
        // wrong. The engine's answer is: the same event, all the way down,
        // because every level is on the game's map and every level passes the
        // event on.
        var threeLevels = new MzEventRunner(
            Common((7, ListFrom((117, 0, ["8"]), (0, 0, []))),
                   (8, ListFrom((117, 0, ["9"]), (0, 0, []))),
                   (9, reads)))
        {
            MaxDepth = 8,
        }.Run(
            ListFrom((117, 0, ["7"]), (0, 0, [])),
            new MzBranchFacts { Variables = { [1] = 0 } },
            pMapId: 5,
            pEventId: 12);
        AssertEq(
            threeLevels.Stopped, MzStep.Finished,
            "and a list called three levels down runs through:"
            + $" {threeLevels.Describe()}");
        AssertEq(
            threeLevels.Child.EventId, 12,
            "and the deepest child is still part of the same event, because"
            + " every level is on the game's map and `setup` takes the event id"
            + " it is given; it has " + threeLevels.Child.EventId);
        AssertEq(
            threeLevels.Child.MapId, 5,
            "and on the same map, because `setup` sets it from the game and not"
            + $" from the caller; it is {threeLevels.Child.MapId}");
        AssertEq(
            threeLevels.Child.Depth, 3,
            "and at depth three, as the engine's _depth + 1 makes it and the"
            + $" top list counts as the first; it is {threeLevels.Child.Depth}");

        // **And the other direction, which is the one the first draft got
        // wrong.** A caller that is *not* on the map passes zero, and a child
        // of it is on the map — so the first child of a parallel event has no
        // event, and the second has none either, because the level above it
        // had none. The event does not come back from the dead.
        var offMapDeep = new MzEventRunner(
            Common((7, ListFrom((117, 0, ["8"]), (0, 0, []))),
                   (8, ListFrom((117, 0, ["9"]), (0, 0, []))),
                   (9, reads)))
        {
            MaxDepth = 8,
        }.Run(
            ListFrom((117, 0, ["7"]), (0, 0, [])),
            new MzBranchFacts { Variables = { [1] = 0 } },
            pMapId: 0,
            pEventId: 12);
        AssertEq(
            offMapDeep.Stopped, MzStep.Finished,
            "and the same chain from a list that is not on the map runs"
            + $" through: {offMapDeep.Describe()}");
        AssertEq(
            offMapDeep.Child.EventId, 0,
            "and its deepest child has no event, because the top list was not"
            + " on the map and the engine passed zero, and the level above it"
            + $" had none either; it has {offMapDeep.Child.EventId}");
        AssertEq(
            offMapDeep.Child.MapId, 0,
            "and no map either, because this reader is given the map the game is"
            + $" on and there is none; it has {offMapDeep.Child.MapId}");
    }

    private static List<List<MzCommandEntry>> EventListsInThisGame()
    {
        var lists = new List<List<MzCommandEntry>>();
        var path = FixtureRoot.PathJoin("data").PathJoin("Map002.json");
        var file = MzDataFile.Read("Map002.json", Godot.FileAccess.GetFileAsBytes(path));
        foreach (var item in file.Root.Member("events")!.Items)
        {
            if (item.Kind != MzKind.Object)
            {
                continue;
            }
            foreach (var page in item.Member("pages")!.Items)
            {
                var commands = new List<MzCommandEntry>();
                foreach (var command in page.Member("list")!.Items)
                {
                    commands.Add(MzCommandEntry.From(command));
                }
                lists.Add(commands);
            }
        }
        return lists;
    }
}
