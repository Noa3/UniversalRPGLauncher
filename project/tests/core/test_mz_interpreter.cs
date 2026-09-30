using System.Collections.Generic;
using Godot;
using UniversalRPG.Tests.Framework;
using UniversalRPG.Web;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The interpreter of an MZ event, walked over lists in the shape this game
/// writes them.
/// </summary>
/// <remarks>
/// <para>
/// K-123 decided a branch and nothing acted on it, because there was no index
/// into the list to move. This is that index, and every rule in it was read out
/// of <c>Game_Interpreter</c> in the engine rather than reasoned about.
/// </para>
/// <para>
/// <b>Every list here is the shape the editor writes, and most of them were got
/// wrong first.</b> That is worth saying plainly, because the shape is the
/// whole of what makes an event list mean anything:
/// </para>
/// <list type="bullet">
/// <item>
/// A conditional branch is <c>111</c>, its arms one indent in, and an <c>411</c>
/// at the branch's own indent between them. A list with two branches in a row
/// and no else is not a shape the editor writes.
/// </item>
/// <item>
/// A loop is <c>112</c>, the body one indent in, and a <c>413</c> at the loop's
/// own indent. <b>The body and the branch are beside each other, not one under
/// the other</b>, and the counter goes up <i>before</i> the branch that tests
/// it. This game writes exactly that at event 5, page 1, and every loop test
/// here is that shape.
/// </item>
/// <item>
/// A loop in a game ends with a <c>113</c>, not with its branch going false. A
/// counter that rises past its limit makes the branch false for ever, and a
/// repeat above after it would send the index back for ever. The engine's own
/// answer to that is <c>checkFreeze</c>, and this reader's is the same limit.
/// </item>
/// <item>
/// A variable command names a <b>range</b>: the engine's loop is
/// <c>for (let i = params[0]; i &lt;= params[1]; i++)</c>, so a command whose
/// first parameter is above its second does nothing at all.
/// </item>
/// <item>
/// A branch's <b>first parameter is the kind</b>, and 1 is a variable. A first
/// draft wrote 0 there meaning "variable zero", the engine read it as a switch,
/// and the reader refused a switch nobody had — correctly, four times over,
/// while the test was wrong every time.
/// </item>
/// </list>
/// <para>
/// What is asserted is not that the game looks right on screen — that needs a
/// renderer, and there is none — but that the interpreter's <i>choices</i> are
/// the engine's: which arm of a branch it takes, where a loop goes, what it
/// refuses, and what it does with a list that goes on for ever.
/// </para>
/// </remarks>
partial class TestMzInterpreter : TestBase
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

    private static MzInterpreter Run(
        List<MzCommandEntry> pCommands, out List<MzAction> pActions,
        MzBranchFacts pFacts = null)
    {
        var interpreter = new MzInterpreter(pCommands);
        pActions = new List<MzAction>();
        try
        {
            interpreter.Run(pActions, pFacts ?? new MzBranchFacts());
        }
        catch (System.Exception e)
        {
            // A fault in the reader shows up as an exception the runner reports
            // without a usable stack, so the test says which command was running
            // rather than leaving it to be guessed at.
            var at = interpreter.Index;
            var command = at >= 0 && at < pCommands.Count
                ? $"code {pCommands[at].Code} at indent {pCommands[at].Indent}"
                : "no command, the index is outside the list";
            throw new System.InvalidOperationException(
                $"the interpreter threw at index {at} of {pCommands.Count}"
                + $" ({command}), stopped {interpreter.Stopped}: {e.Message}",
                e);
        }
        return interpreter;
    }

    /// <summary>What the interpreter recorded, in one line.</summary>
    private static string Describe(List<MzAction> pActions)
    {
        if (pActions.Count == 0)
        {
            return "nothing at all";
        }
        var text = new System.Text.StringBuilder();
        for (var i = 0; i < pActions.Count; i++)
        {
            if (i > 0)
            {
                text.Append("; ");
            }
            text.Append(pActions[i].Code).Append('@')
                .Append(pActions[i].Indent).Append('=').Append(pActions[i].What);
        }
        return text.ToString();
    }

    /// <summary>
    /// What a recorded action says, or a description of the gap when there is
    /// none. A test that reads actions[1] when only one was recorded took the
    /// whole suite down, and the runner reports an exception without a stack
    /// worth having, so nothing here reads past the end.
    /// </summary>
    private static string Action(List<MzAction> pActions, int pIndex) =>
        pIndex < pActions.Count
            ? pActions[pIndex].What
            : $"no action {pIndex}, there are {pActions.Count}";

    public void Test_ABranchThatIsTrueRunsItsOwnArmAndStepsOverTheOther()
    {
        // 111 asks whether variable 0 is at least the number 3: the kind from
        // the first parameter and 1 is a variable, the variable from the second,
        // whether the right side is a number from the third, that number from
        // the fourth and the way from the fifth.
        //
        // The two arms write different variables, so which one holds a number
        // says which ran. A test watching one number could not tell an arm that
        // ran from one that wrote the same value.
        var commands = ListFrom(
            (111, 0, ["1", "0", "0", "3", "1"]),
            (122, 1, ["1", "1", "0", "0", "10"]),
            (411, 0, []),
            (122, 1, ["2", "2", "0", "0", "20"]),
            (0, 0, []));
        var facts = new MzBranchFacts
        {
            Variables = { [0] = 5, [1] = 0, [2] = 0 },
        };

        var interpreter = Run(commands, out var actions, facts);

        AssertEq(
            interpreter.Stopped, MzStep.Finished,
            $"the event runs to its end, and it is {interpreter.Stopped}");
        AssertEq(
            actions.Count, 2,
            "and the true arm is taken and the false arm is not; there are"
            + $" {actions.Count}: {Describe(actions)}");
        AssertEq(
            Action(actions, 0), "branch True by at least",
            $"the branch is true, and it is: {Action(actions, 0)}");
        AssertEq(
            facts.Variable(1), 10,
            "so the command in the true arm ran");
        AssertEq(
            facts.Variable(2), 0,
            "and the false arm's number was not written, because the else steps"
            + " over it when the branch before it was true");
    }

    public void Test_ABranchThatIsFalseSkipsToTheElseAndTakesTheOtherArm()
    {
        // The engine's skipBranch steps while the next command sits deeper than
        // the branch, so it steps over the true arm and lands on the 411. The
        // 411 sees false already at that indent and does not skip, and the false
        // arm runs. Both cases give a branch and one arm, never three.
        var commands = ListFrom(
            (111, 0, ["1", "0", "0", "3", "1"]),
            (122, 1, ["1", "1", "0", "0", "10"]),
            (411, 0, []),
            (122, 1, ["2", "2", "0", "0", "20"]),
            (0, 0, []));
        var facts = new MzBranchFacts
        {
            Variables = { [0] = 0, [1] = 0, [2] = 0 },
        };

        var interpreter = Run(commands, out var actions, facts);

        AssertEq(
            interpreter.Stopped, MzStep.Finished,
            "and it still finishes rather than stopping at the branch, and it"
            + $" is {interpreter.Stopped}: {interpreter.Reason}");
        AssertEq(
            actions.Count, 2,
            "and the false arm is taken, with nothing but the branch and the"
            + $" command inside it; there are {actions.Count}: {Describe(actions)}");
        AssertEq(
            Action(actions, 0), "branch False by at least",
            $"which is false, and it is: {Action(actions, 0)}");
        AssertEq(
            facts.Variable(1), 0,
            "and the true arm left its number alone");
        AssertEq(
            facts.Variable(2), 20,
            "while the false arm's own number was written");
    }

    public void Test_AnElseRunsOnlyWhenTheBranchBeforeItWasFalse()
    {
        // `command411` is `if (this._branch[this._indent] !== false)
        // this.skipBranch()`. It skips when the indent does not already hold
        // false, so a branch decided true skips the else, and a branch not
        // decided at all also skips it. A reader that read the condition the
        // other way round would run the else after a branch that was true, and
        // both arms of every branch in a game would run.
        //
        // The branch asks about variable 0, so what the two rounds differ in is
        // variable 0. A first draft varied variable 3, which is the limit's own
        // number in the parameters and not something the branch reads.
        foreach (var (tested, expected) in new[] { (5, "if"), (0, "else") })
        {
            var commands = ListFrom(
                (111, 0, ["1", "0", "0", "3", "1"]),
                (122, 1, ["1", "1", "0", "0", "10"]),
                (411, 0, []),
                (122, 1, ["2", "2", "0", "0", "20"]),
                (0, 0, []));
            var facts = new MzBranchFacts
            {
                Variables = { [0] = tested, [1] = 0, [2] = 0 },
            };

            Run(commands, out _, facts);

            AssertEq(
                facts.Variable(1), expected == "if" ? 10 : 0,
                "variable one, which only the true arm writes, holds what the"
                + $" {expected} arm set when the branch tested {tested}");
            AssertEq(
                facts.Variable(2), expected == "else" ? 20 : 0,
                "and variable two, which only the false arm writes, holds what"
                + $" the {expected} arm set");
        }
    }

    public void Test_ALoopInAGameEndsWithABreakAndNotWithItsBranchGoingFalse()
    {
        // The loop this game writes at event 5, page 1, and the shape the editor
        // produces: the counter goes up, the branch tests it, the arm runs and
        // breaks, the branch ends, the loop repeats.
        //
        // A first draft of this expected the loop to end by itself when the
        // counter reached a number, and it never did: the counter keeps rising
        // and `counter < limit` is false for ever after, so the branch skips
        // its arm and the repeat above sends the index back anyway. That is
        // what the engine does with that list too. A game's loop ends with a
        // break, and this says so and counts the round the break left on.
        var commands = ListFrom(
            (112, 0, []),
            (122, 1, ["0", "0", "1", "0", "1"]),
            (111, 1, ["1", "0", "0", "3", "4"]),
            (122, 2, ["1", "1", "0", "0", "1"]),
            (122, 2, ["0", "0", "0", "0", "0"]),
            (113, 2, []),
            (0, 2, []),
            (412, 1, []),
            (0, 1, []),
            (413, 0, []),
            (0, 0, []));
        var facts = new MzBranchFacts
        {
            Variables = { [0] = 0, [1] = 0, [2] = 0, [3] = 3 },
        };

        var interpreter = new MzInterpreter(commands);
        var actions = new List<MzAction>();
        interpreter.Run(actions, facts);

        AssertEq(
            interpreter.Stopped, MzStep.Finished,
            "a loop that breaks on its own condition ends rather than running"
            + $" on, and it is {interpreter.Stopped}: {interpreter.Reason}");
        AssertEq(
            facts.Variable(0), 0,
            "and the counter is back where it started, because the arm put it"
            + $" back before the break; it is {facts.Variable(0)}");
        AssertEq(
            facts.Variable(1), 1,
            "and the arm ran once, because the break inside it left the loop on"
            + $" the first round; it is {facts.Variable(1)}");
    }

    public void Test_TheDefaultStepLimitIsTheEnginesOwnAndIsReportedWithoutBeingAsked()
    {
        // A caller who sets no limit gets the engine's own number, and a list
        // that never ends is reported rather than left to hang. **This is claimed
        // with the default in place and no limit set on the interpreter**,
        // because a test that sets `CommandLimit` itself proves only that the
        // property works, not that a reader with no limit would ever stop.
        var commands = ListFrom(
            (112, 0, []),
            (122, 1, ["0", "0", "1", "0", "1"]),
            (413, 0, []),
            (0, 0, []));

        AssertEq(
            new MzInterpreter(commands).CommandLimit, 100_000,
            "and the default is the engine's own checkFreeze, which is a hundred"
            + " thousand commands in one frame");

        // Run it with the default. This costs a hundred thousand steps, which
        // the reader does without a frame of its own.
        var interpreter = new MzInterpreter(commands);
        var actions = new List<MzAction>();
        interpreter.Run(actions, new MzBranchFacts { Variables = { [0] = 0 } });

        AssertEq(
            interpreter.Stopped, MzStep.Frozen,
            "and a list that goes on for ever is reported with no limit set");
        AssertTrue(
            interpreter.Reason.Contains("100000")
                && interpreter.Reason.Contains("without finishing"),
            $"and the reason gives the engine's own number: {interpreter.Reason}");
    }

    public void Test_ARunThatGoesOnTooLongIsReportedAndNotLeftToHang()
    {
        // The engine's own answer: checkFreeze counts frames and update() breaks
        // out of its loop when a hundred thousand have passed in one frame, so
        // a repeat above that never meets a changed number freezes the game.
        // This reader has no frames, so it counts commands against the same
        // number and says so, which is the difference between a reader that
        // hangs and one that reports a list nobody could finish.
        var commands = ListFrom(
            (112, 0, []),
            (122, 1, ["0", "0", "1", "0", "1"]),
            (413, 0, []),
            (0, 0, []));
        var facts = new MzBranchFacts
        {
            Variables = { [0] = 0 },
        };

        var interpreter = new MzInterpreter(commands) { CommandLimit = 50 };
        var actions = new List<MzAction>();
        interpreter.Run(actions, facts);

        AssertEq(
            interpreter.Stopped, MzStep.Frozen,
            "a list whose repeat above never ends is reported");
        AssertTrue(
            interpreter.Reason.Contains("without finishing"),
            $"and the reason says what happened: {interpreter.Reason}");
    }

    public void Test_ABreakLeavesItsOwnLoopAndNotTheOneAroundIt()
    {
        // The engine's break walks forward counting loops and stops on the first
        // repeat above that closes one. The list nests two loops and breaks out
        // of the inner one, and the outer one must carry on.
        var commands = ListFrom(
            (112, 0, []),
            (122, 1, ["0", "0", "1", "0", "1"]),
            (111, 1, ["1", "0", "0", "9", "4"]),
            (112, 2, []),
            (113, 3, []),
            (0, 3, []),
            (413, 2, []),
            (122, 2, ["1", "1", "0", "0", "1"]),
            (412, 1, []),
            (413, 0, []),
            (0, 0, []));
        var facts = new MzBranchFacts
        {
            Variables = { [0] = 0, [1] = 0, [2] = 0, [9] = 9 },
        };

        // The outer loop's counter rises past its limit and nothing ends it, so
        // the run is stopped by the reader's limit, as the engine's checkFreeze
        // stops one. The claim is the break, not the ending.
        var interpreter = new MzInterpreter(commands) { CommandLimit = 200 };
        var actions = new List<MzAction>();
        interpreter.Run(actions, facts);

        var insideInner = 0;
        var outerRounds = 0;
        foreach (var action in actions)
        {
            if (action.Indent == 3)
            {
                insideInner++;
            }
            if (action.Indent == 1 && action.Code == 122)
            {
                outerRounds++;
            }
        }
        AssertEq(
            insideInner, 0,
            "and nothing inside the loop the break left ever ran");
        AssertTrue(
            outerRounds >= 2,
            $"while the outer body ran more than once ({outerRounds}), so the"
            + " break ended the inner loop and not the outer one");
        AssertEq(
            interpreter.Stopped, MzStep.Frozen,
            "and the run is reported as gone on too long rather than hanging,"
            + " which is where the engine freezes a game");
    }

    public void Test_ACommandTheEngineHasNoMethodForIsSteppedOverRatherThanRefused()
    {
        // executeCommand asks `typeof this[methodName] === "function"` and, when
        // it is not, still does `this._index++`. A reader that reported such a
        // command as unknown would stop a game over a command the game itself
        // ran past, and games are full of them: a 401 line of text under a 101
        // has no method and must be stepped over. This game stores exactly
        // that, at event 1: a 101 with a 401 under it.
        // **`params[4]` is the speaker's name** — `setSpeakerName(params[4])`
        // — and the first draft of this test gave the 101 a single empty
        // parameter. That was harmless while 101 was unread, and it stopped
        // being harmless the moment K-133 gave 101 to the 101: the command
        // asks for five and was handed one, and the reader read past the end
        // of a list that was perfectly good.
        var commands = ListFrom(
            (101, 0, ["0", "0", "0", "0", ""]),
            (401, 1, ["a line of text"]),
            (0, 0, []));
        var facts = new MzBranchFacts();

        var interpreter = Run(commands, out var actions, facts);

        // **It waits, and that is the answer.**
        //
        // **A first draft asserted `Finished`**, because before K-133 a 101
        // was an unknown command the interpreter ran past in one frame. Now a
        // 101 takes its line and **holds the page** — `setWaitMode("message")`,
        // ended by `$gameMessage.isBusy()` — so a walk that does not press on
        // stops on the first dialogue of every game, which is not a claim
        // about the engine and not one about this test's list.
        //
        // **So the run is finished once the caller releases the page**, and
        // the first press is the one that proves the line was read: the words
        // are already in the dialogue's block before anybody has seen them.
        AssertEq(
            interpreter.Stopped, MzStep.Waiting,
            "and a dialogue holds its page rather than ending the list, and"
            + $" a walk that does not press on stops there; it is"
            + $" {interpreter.Stopped}");

        facts.MessageBusy = false;
        var danach = new List<MzAction>();
        interpreter.Run(danach, facts);
        AssertEq(
            interpreter.Stopped, MzStep.Finished,
            "and once the page is released the list runs to its end, which"
            + $" is what a player pressing on does; it is"
            + $" {interpreter.Stopped}");
        // **The engine part of this claim still holds, and it is the part
        // that matters:** `typeof this["command401"] === "function"` is false,
        // and the engine steps over it. A reader that reported 401 as unknown
        // would refuse a command the game runs past every time a line of text
        // appears.
        AssertTrue(
            !MzCommandSet.HasMethod(401),
            "and the engine really has no method for it, which is what the"
            + $" engine asks; it has {MzCommandSet.HasMethod(401)}");

        // **The check is two terms, because 401 is not in the list of a
        // hundred and fourteen at all.** A first draft read it as one term
        // and wrote a test that passed for the wrong reason: taking 401 out
        // of `NoMethodCodes` changed nothing, because the hundred and
        // fourteen never held it.
        //
        // **And a first draft of that list claimed 601, 602 and 603 have no
        // method. They do** — `command601`, `command602` and `command603` are
        // three of the hundred and fourteen. **This test says so, because a
        // claim about a list is worth exactly as much as the test that
        // notices when the list is wrong.**
        AssertTrue(
            MzCommandSet.HasMethod(601),
            "and 601, which a first draft listed as having no method, does"
            + $" have one; it has {MzCommandSet.HasMethod(601)}");
        AssertTrue(
            MzCommandSet.HasMethod(602),
            $"and so does 602; it has {MzCommandSet.HasMethod(602)}");
        AssertTrue(
            MzCommandSet.HasMethod(603),
            $"and so does 603; it has {MzCommandSet.HasMethod(603)}");
        AssertEq(
            MzCommandSet.NoMethodCodes.Count, 9,
            "and the numbers without a method are nine, not the twelve a"
            + $" first draft wrote, and all nine lie outside the hundred and"
            + $" fourteen; there are {MzCommandSet.NoMethodCodes.Count}");

        // **And where the line ends up.** The engine reads a 401 by position
        // inside a 101's block: `command101` runs
        // `while (this.nextEventCode() === 401) { this._index++;
        // $gameMessage.add(…); }` and steps the index over each line itself.
        // **The line is never dispatched**, so what the reader keeps is the
        // dialogue's block and not a list of lines.
        //
        // A first draft of this test read the 401 as a command of its own and
        // kept it in `facts.Message`. That was true of the data and false of
        // the engine, and it broke as soon as K-133 gave 101 to the 101 — the
        // line then belonged to a dialogue, and `facts.Message` was empty.
        // **A test that keeps passing by reading the data a different way
        // from the engine is a test of the reader's own invention.**
        AssertEq(
            facts.LastDialogue!.Lines.Count, 1,
            "and the line is in the dialogue's block, because the 101 read"
            + " it and the reader did not run it as a command of its own;"
            + $" there are {facts.LastDialogue!.Lines.Count}");
        AssertEq(
            facts.LastDialogue.Lines[0].Text, "a line of text",
            "with the words the game wrote; they are"
            + $" \"{facts.LastDialogue.Lines[0].Text}\"");
        AssertEq(
            actions.Count, 1,
            "and it says so once — for the dialogue, not for the line,"
            + $" because the line is not a command; it recorded"
            + $" {Describe(actions)}");

        // **And a 401 with no 101 over it is refused**, which is what the
        // engine's own list says: `nextEventCode()` returns 0 at the end of
        // a list, and a line the dialogue never reached is not one the
        // engine shows.
        var ohneDialog = Run(
            ListFrom(
                (401, 0, ["a line nobody put under a dialogue"]),
                (0, 0, [])),
            out var ohneAktionen,
            new MzBranchFacts());

        AssertEq(
            ohneDialog.Stopped, MzStep.Refused,
            "and a line on its own is refused, which is the honest answer"
            + " rather than a stop: the engine has no command401, nothing"
            + " would have read it, and a game that ships one has an event"
            + $" list the editor would not write; it is {ohneDialog.Stopped}");
        AssertEq(
            ohneAktionen.Count, 0,
            $"and nothing was shown; it recorded {Describe(ohneAktionen)}");
    }

    public void Test_AListThatEndsInsideABranchIsReportedAndNotReadPast()
    {
        // skipBranch is `while (this._list[this._index + 1].indent >
        // this._indent` with no test for the end of the list. A list whose last
        // command is the last line of a branch asks for a command that is not
        // there. The engine would read past the end and find undefined; this
        // reader says the list is malformed, because the alternative is a crash
        // on a file that is merely odd rather than broken.
        //
        // The branch has to come to false for its arm to be skipped, and the
        // arm has to be the last thing in the list for the skip to run off the
        // end of it. Variable 0 against the number 3, and variable 0 is zero.
        var commands = ListFrom(
            (111, 0, ["1", "0", "0", "3", "1"]),
            (122, 1, ["0", "0", "1", "0", "1"]));
        var facts = new MzBranchFacts
        {
            Variables = { [0] = 0 },
        };

        var interpreter = Run(commands, out _, facts);

        AssertEq(
            interpreter.Stopped, MzStep.Truncated,
            "a branch that never closes is said rather than read past");
        AssertTrue(
            interpreter.Malformed.Contains("never closes"),
            "and the reason names what is wrong with the list:"
            + $" {interpreter.Malformed}");
    }

    public void Test_ADivisionByZeroWritesZeroRatherThanStoppingTheGame()
    {
        // The engine wraps each operation in a try and, on any failure, writes
        // zero. A reader that let the failure out would stop a game the engine
        // plays on, and a game that divides by a variable it has not set yet
        // would stop on the first frame.
        var commands = ListFrom(
            (122, 0, ["0", "0", "4", "0", "0"]),
            (0, 0, []));
        var facts = new MzBranchFacts
        {
            Variables = { [0] = 10 },
        };

        var interpreter = Run(commands, out var actions, facts);

        AssertEq(
            interpreter.Stopped, MzStep.Finished,
            "dividing by zero does not stop the event");
        AssertEq(
            facts.Variable(0), 0,
            "and the variable is zero, which is what the engine writes;"
            + $" it is {facts.Variable(0)}");
        AssertEq(actions.Count, 1, "and the command is recorded as having run");
    }

    public void Test_ARangeOfSwitchesSwitchesEveryOneOfThem()
    {
        // Both commands loop `for (let i = params[0]; i <= params[1]; i++)`,
        // both ends in, so a command naming a range is a command about all of
        // it. A reader that switched only the first would leave a game's flags
        // half set and its branches would go somewhere the editor did not say.
        var commands = ListFrom(
            (121, 0, ["10", "13", "0"]),
            (0, 0, []));
        var facts = new MzBranchFacts();

        Run(commands, out var actions, facts);

        AssertEq(actions.Count, 4, "a range of four is four commands");
        for (var id = 10; id <= 13; id++)
        {
            AssertTrue(
                facts.Switches.ContainsKey(id) && facts.Switches[id],
                $"and switch {id} is on, both ends of the range included");
        }
    }

    public void Test_ARangeThatStartsAboveWhereItEndsDoesNothingAtAll()
    {
        // The engine's own loop is `for (let i = params[0]; i <= params[1];
        // i++)`, so a command whose first parameter is above its second writes
        // nothing. A reader that walked from the first down to the second would
        // write a game's variables the editor never asked it to.
        var commands = ListFrom(
            (122, 0, ["5", "2", "0", "0", "7"]),
            (0, 0, []));
        var facts = new MzBranchFacts
        {
            Variables = { [2] = 0, [3] = 0, [4] = 0, [5] = 0 },
        };

        Run(commands, out var actions, facts);

        AssertEq(
            actions.Count, 0,
            "a range from five down to two writes nothing, because the engine's"
            + " loop runs the other way");
        for (var id = 2; id <= 5; id++)
        {
            AssertEq(
                facts.Variable(id), 0,
                $"and variable {id} is untouched, as the editor wrote it");
        }
    }

    public void Test_RandomIsDrawnForEveryVariableOfARangeAndASeedGivesTheSameRun()
    {
        // The engine draws once per variable, not once for the range:
        //
        //     for (let i = startId; i <= endId; i++) {
        //         if (typeof value === "number") {
        //             const realValue = value + Math.randomInt(randomMax);
        //             this.operateVariable(i, operationType, realValue);
        //         }
        //     }
        //
        // **A first draft of this claimed one draw for the whole range, and the
        // engine's source says otherwise**: the `Math.randomInt` call is inside
        // the loop, so three variables in one range get three numbers. A reader
        // that drew once would give a game the same roll everywhere in a range,
        // and a roll of "how many of these five did I get" would come out the
        // same number five times over.
        //
        // The other half of the claim is the seed. A game that rolls dice has to
        // be replayable, or a save cannot be loaded and produce what it produced
        // last time, and that is the whole of what a deterministic runtime is
        // for.
        var commands = ListFrom(
            (122, 0, ["0", "2", "1", "2", "1", "6"]),
            (0, 0, []));
        var facts = new MzBranchFacts();

        var first = new MzInterpreter(commands) { Random = new MzRandom() };
        first.Random.Seed(7);
        first.Run(new List<MzAction>(), facts);

        for (var id = 0; id <= 2; id++)
        {
            AssertTrue(
                facts.Variable(id) >= 1 && facts.Variable(id) <= 6,
                $"and variable {id} holds its own roll, inside the range the"
                + $" engine computes from parameters four and five, which is"
                + $" six minus one plus one, so one to six; it is"
                + $" {facts.Variable(id)}");
        }

        // The same seed gives the same run, so a save can be replayed.
        var again = new MzBranchFacts();
        var second = new MzInterpreter(commands) { Random = new MzRandom() };
        second.Random.Seed(7);
        second.Run(new List<MzAction>(), again);
        for (var id = 0; id <= 2; id++)
        {
            AssertEq(
                again.Variable(id), facts.Variable(id),
                $"and the same seed gives the same number for variable {id}, so"
                + " a run can be replayed");
        }

        // A different seed must be able to give a different run, or the
        // generator is not one and the repeatability above means nothing. Three
        // draws from six values make a collision likely, so this is checked
        // over several seeds rather than on one.
        var differentFrom = 0;
        for (var seed = 1; seed <= 12; seed++)
        {
            var other = new MzBranchFacts();
            var run = new MzInterpreter(commands) { Random = new MzRandom() };
            run.Random.Seed(seed);
            run.Run(new List<MzAction>(), other);
            if (other.Variable(0) != facts.Variable(0)
                || other.Variable(1) != facts.Variable(1)
                || other.Variable(2) != facts.Variable(2))
            {
                differentFrom++;
            }
        }
        AssertTrue(
            differentFrom >= 10,
            $"and a different seed gives a different run, or the generator is"
            + $" not one; {differentFrom} of twelve seeds differed from seed 7");
    }

    public void Test_AJumpToALabelThatIsNotThereLeavesTheIndexWhereItWas()
    {
        // The engine looks through the list, finds no such label and carries on.
        // That is kept, because a jump to a label that is not there is not a
        // reason to stop a game, and a reader that stopped there would strand
        // every event whose author deleted a label.
        var commands = ListFrom(
            (119, 0, ["nowhere"]),
            (122, 0, ["0", "0", "0", "0", "7"]),
            (0, 0, []));
        var facts = new MzBranchFacts
        {
            Variables = { [0] = 0 },
        };

        var interpreter = Run(commands, out var actions, facts);

        AssertEq(
            interpreter.Stopped, MzStep.Finished,
            "a jump to a label that is not there is not a stop");
        AssertEq(
            facts.Variable(0), 7,
            "and the command after it runs, as the engine would have it;"
            + $" it is {facts.Variable(0)}");
    }

    public void Test_AJumpClearsTheBranchResultsOfEveryIndentItStepsOver()
    {
        // The engine's jumpTo walks from the old index to the new one and
        // clears the result of every indent whose depth changes:
        //
        //     let indent = this._indent;
        //     for (let i = startIndex; i <= endIndex; i++) {
        //         const newIndent = this._list[i].indent;
        //         if (newIndent !== indent) {
        //             this._branch[indent] = null;
        //             indent = newIndent;
        //         }
        //     }
        //     this._index = index;
        //
        // **A repeat above is not one of those jumps, and a reader that treated
        // it as one would clear branch results the engine keeps.** command413
        // is `do { this._index--; } while (currentCommand().indent !==
        // this._indent); return true;` — it writes the index and goes, and never
        // calls jumpTo. So a loop's second pass answers a branch with the
        // first pass's result where the engine would too, but a jump to a label
        // does clear. Both are claimed here so that neither is a guess.
        //
        // The list is the shape this game writes: a loop, a counter that rises,
        // a branch that tests it, and a repeat above. Once the counter has
        // reached the limit the branch is false for ever and the repeat above
        // sends the index back for ever, so the list does not end and the
        // engine's own checkFreeze is what stops it. This reader's limit is the
        // same answer, claimed here rather than papered over.
        var commands = ListFrom(
            (112, 0, []),
            (122, 1, ["0", "0", "1", "0", "1"]),
            (111, 1, ["1", "0", "0", "1", "4"]),
            (122, 2, ["1", "1", "0", "0", "1"]),
            (412, 1, []),
            (0, 1, []),
            (413, 0, []),
            (0, 0, []));
        var facts = new MzBranchFacts
        {
            Variables = { [0] = 0, [1] = 0, [2] = 0 },
        };
        var interpreter = new MzInterpreter(commands) { CommandLimit = 40 };
        var actions = new List<MzAction>();
        interpreter.Run(actions, facts);

        // The counter rises to 1 on the first round, and 1 is not less than 1,
        // so from the second round on the branch is false and its arm is
        // skipped. A reader that kept the first round's true would put the
        // arm's number up once per round and the two would not agree.
        AssertEq(
            facts.Variable(1), 0,
            "and the arm ran only on the round the branch allowed, because the"
            + $" branch was asked again on the next and said no; it is"
            + $" {facts.Variable(1)}");
        AssertEq(
            interpreter.Stopped, MzStep.Frozen,
            "and the run is reported as gone on too long, where the engine"
            + " freezes a game rather than leaving it hanging");

        // The clearing, stated directly: the branch is asked on every round, and
        // asked afresh, rather than once and remembered.
        var decisions = 0;
        foreach (var action in actions)
        {
            if (action.Code == MzCommandTable.ShowText)
            {
                decisions++;
            }
        }
        AssertTrue(
            decisions >= 4,
            "and the branch was asked on every round rather than once and"
            + $" remembered ({decisions} decisions in forty commands)");
    }

    public void Test_AJumpClearsTheBranchResultOfEveryIndentItWalksOver()
    {
        // The engine's jumpTo clears the result of an indent **whose depth
        // changes while the index walks over it**:
        //
        //     let indent = this._indent;
        //     for (let i = startIndex; i <= endIndex; i++) {
        //         const newIndent = this._list[i].indent;
        //         if (newIndent !== indent) {
        //             this._branch[indent] = null;
        //             indent = newIndent;
        //         }
        //     }
        //     this._index = index;
        //
        // **A jump that stays on one indent clears nothing**, and every earlier
        // test jumped from indent 0 to indent 0, so nothing had ever gone
        // through that loop. A reader that dropped the clearing entirely passed
        // all of them.
        //
        // One branch at indent 1, and the index walks the whole list, so the
        // result stored at indent 1 is the one the branch decided.
        var commands = ListFrom(
            (111, 1, ["1", "0", "0", "3", "1"]),
            (122, 2, ["1", "1", "0", "0", "10"]),
            (412, 1, []),
            (0, 1, []),
            (0, 0, []));

        // The branch comes to false, so its arm does not run and the result for
        // indent 1 is false.
        var onFalse = new MzInterpreter(commands);
        var falseFacts = new MzBranchFacts { Variables = { [0] = 0 } };
        var falseActions = new List<MzAction>();
        onFalse.Run(falseActions, falseFacts);
        AssertEq(
            onFalse.BranchAt(1), false,
            "a branch that came to false is remembered at its own indent, and"
            + $" that is {onFalse.BranchAt(1)}");
        AssertEq(
            falseFacts.Variable(1), 0,
            "and the arm of a false branch left its number alone, because the"
            + $" engine steps over it; it is {falseFacts.Variable(1)}");

        // The same list with facts that make the branch true: the arm runs and
        // the result at indent 1 is true.
        var onTrue = new MzInterpreter(commands);
        var trueFacts = new MzBranchFacts { Variables = { [0] = 5 } };
        var trueActions = new List<MzAction>();
        onTrue.Run(trueActions, trueFacts);
        AssertEq(
            onTrue.BranchAt(1), true,
            "and the same branch with facts that make it true is remembered as"
            + $" true; that is {onTrue.BranchAt(1)}");
        AssertEq(
            trueFacts.Variable(1), 10,
            "and its arm ran, as the engine would have it; variable one is"
            + $" {trueFacts.Variable(1)}");

        // The movement itself. The engine's walk clears the result of the indent
        // it leaves, which is what a jump does and a repeat above does not.
        var movement = new MzInterpreter(commands);
        movement.SetBranch(1, true);
        movement.Index = 3;
        movement.JumpTo(4);
        AssertEq(
            movement.BranchAt(1), null,
            "and a jump that walks over an indent clears the result stored at"
            + $" that indent, so a branch met again is decided afresh;"
            + $" it is {movement.BranchAt(1)?.ToString() ?? "nothing there"}");
        AssertEq(
            movement.Index, 4,
            "and the index is the one the jump named");

        // A jump that does not change indent clears nothing, which is the other
        // half of the rule and the one every other test here happened to hit.
        var level = new MzInterpreter(commands);
        level.SetBranch(0, true);
        level.Index = 1;
        level.JumpTo(2);
        AssertEq(
            level.BranchAt(0), true,
            "while a jump that stays on one indent keeps the result, because"
            + " the engine only clears an indent it leaves; it is"
            + $" {level.BranchAt(0)}");
    }

    public void Test_AJumpToALabelLandsOnItAndTheStepAfterRunsTheCommandBeyond()
    {
        // A jump to a label calls jumpTo, which sets the index to the label
        // itself; executeCommand then does `this._index++`, so **the command
        // after the label runs, not the label.** That is the whole of it, and
        // it is what a label is for: a game writes the commands a jump wants to
        // skip over, puts a label after them, and jumps to it.
        //
        // **A jump that points backwards at a label with nothing between is a
        // loop, and the engine loops on it too**: the jump sets the index back
        // to the label, the step moves on, and the jump is met again until
        // checkFreeze stops it. A first draft of this pointed the jump at the
        // label in front of it and hung the whole suite for a hundred thousand
        // steps. That is the engine's own answer to that list, and **this game
        // stores no label and no jump at all** — there is not one 118 or 119 in
        // the map read here — so the only shape asserted here is one that ends.
        var landing = ListFrom(
            (111, 0, ["1", "0", "0", "3", "1"]),
            (122, 1, ["1", "1", "0", "0", "10"]),
            (412, 0, []),
            (0, 0, []),
            (119, 0, ["ahead"]),
            (118, 0, ["ahead"]),
            (122, 0, ["2", "2", "0", "0", "20"]),
            (0, 0, []));
        var facts = new MzBranchFacts
        {
            Variables = { [0] = 0, [1] = 0, [2] = 0 },
        };

        var landed = Run(landing, out var actions, facts);

        AssertEq(
            landed.Stopped, MzStep.Finished,
            "a jump to a label that is there runs through, and it is"
            + $" {landed.Stopped}: {landed.Reason}");
        AssertEq(
            facts.Variable(1), 0,
            "and the branch before the jump was false, so its arm did not run;"
            + $" it is {facts.Variable(1)}");
        AssertEq(
            facts.Variable(2), 20,
            "and the command after the label ran, so the jump landed there and"
            + $" the step moved on past the label; it is {facts.Variable(2)}");
        AssertEq(
            actions.Count, 2,
            "and the only things recorded are the branch and the command after"
            + $" the label, because a label and a jump do nothing themselves;"
            + $" {actions.Count} actions");

        // A jump to a label that is not there leaves the index where it was,
        // which is the engine's answer and is not a reason to stop a game.
        var missing = ListFrom(
            (122, 0, ["0", "0", "0", "0", "7"]),
            (119, 0, ["gone"]),
            (122, 0, ["1", "1", "0", "0", "20"]),
            (0, 0, []));
        var missingFacts = new MzBranchFacts
        {
            Variables = { [0] = 0, [1] = 0 },
        };

        var missed = Run(missing, out var missActions, missingFacts);
        AssertEq(
            missed.Stopped, MzStep.Finished,
            "and a jump to a label that is not there is not a stop either");
        AssertEq(
            missingFacts.Variable(1), 20,
            "and the command after it runs, as the engine would have it;"
            + $" it is {missingFacts.Variable(1)}");
    }

    public void Test_ThisGamesOwnEventListsAreWalkedAndEveryOneSaysWhatItDid()
    {
        // Every event list in the one map read here, walked with the numbers a
        // caller would have.
        //
        // **The list at event 4 has a loop with no way out of it.** It is a 112
        // at the top with seven dialogues and a 413, and nothing between them
        // that tests anything or breaks. The engine plays that list until
        // `checkFreeze` stops it, which is what `isFreeze` is for; a list like
        // that is a bug in a game and freezes it. So this reader's step limit is
        // not a safety net invented here, it is the same answer to the same
        // list, and the test says so rather than treating a freeze as a failure.
        //
        // **And K-133 changed what that freeze means.** Before 101 was
        // dispatched, event 4's seven 101s were seven unknown commands the
        // interpreter ran past. Now each one **takes a line and holds the
        // page** — `setWaitMode("message")`, ended by
        // `$gameMessage.isBusy()`, which is true until the player presses on.
        // **A test that walks the list and never presses on is now waiting
        // where it used to be looping**, and the two look the same from
        // outside: the list does not reach its end.
        //
        // **So the walk presses on**, once per dialogue, which is what a
        // player does and what the engine's own `updateWaitMode` waits for.
        // A reader that did not allow it would report a game that hangs on
        // its second line of dialogue, and every game has dialogues.
        var lists = EventListsInThisGame();
        AssertTrue(
            lists.Count >= 6,
            $"the game has {lists.Count} event pages in the one map read here");

        var finished = 0;
        var refused = 0;
        var frozen = 0;
        var waiting = 0;
        foreach (var commands in lists)
        {
            var interpreter = new MzInterpreter(commands);
            var actions = new List<MzAction>();
            var facts = new MzBranchFacts
            {
                Switches = { [1] = false, [2] = true, [3] = false, [4] = true },
                Variables = { [0] = 0, [77] = 0, [78] = 0, [180] = 0 },
            };

            // **Press on until the list ends — as a player would, and as the
            // engine's own `updateWaitMode` waits for.** A 230 waits a fixed
            // number of frames and stops by itself, so `PassFrame` answers
            // it; a 101 waits for a key and does not.
            //
            // **The cap is not a safety net and is not a claim about the
            // game**: it is the number of things that can wait, and a list
            // that waits more often than that has a loop in it — which is
            // event 4's, and `Frozen` is the right answer for it.
            for (var frame = 0; frame < 400; frame++)
            {
                interpreter.Run(actions, facts);
                if (interpreter.Stopped == MzStep.Finished
                    || interpreter.Stopped == MzStep.Frozen
                    || interpreter.Stopped == MzStep.Refused)
                {
                    break;
                }

                // **A 101's page is released the way the engine releases it:**
                // `$gameMessage.clearMessage()` between two lines, which is
                // what `Window_Selectable`'s input handler ends with. The
                // reader has no window, so the caller does it — and a caller
                // that did not would be a caller that stopped at the first
                // line of every game.
                if (facts.MessageBusy)
                {
                    facts.MessageBusy = false;
                }
                else
                {
                    interpreter.PassFrame();
                }
            }

            switch (interpreter.Stopped)
            {
                case MzStep.Finished:
                    finished++;
                    break;
                case MzStep.Refused:
                    refused++;
                    AssertTrue(
                        interpreter.Reason != "",
                        "and a refusal says what it needed");
                    break;
                case MzStep.Frozen:
                    frozen++;
                    AssertTrue(
                        interpreter.Reason.Contains("without finishing"),
                        "and a freeze says how far it got, so the list that"
                        + " could not end can be found in the game");
                    break;
                case MzStep.Waiting:
                    // A 230 leaves the index where it is, and the engine counts
                    // the frames down before reading it again. A single reader
                    // has no frames, so a list that waits stops here and says
                    // so. **This is not a failure**: a caller with frames calls
                    // `PassFrame` and comes back.
                    waiting++;
                    AssertTrue(
                        interpreter.Reason.Contains("waiting"),
                        "and a wait says it is waiting, and how long for, so the"
                        + $" caller knows when to come back: {interpreter.Reason}");
                    // **The index is on the command that waits, and the
                    // engine reads that same command again next frame.**
                    // After K-133 that is a 230 and not a 101, because a
                    // 101's page is released by the caller and the loop
                    // above does it — so what is left waiting here is a
                    // command that stops on its own.
                    AssertEq(
                        interpreter.Commands[interpreter.Index].Code,
                        MzCommandTable.Wait,
                        "and the index stayed on the wait, because the engine"
                        + " reads the same command again next frame; it is"
                        + $" on {interpreter.Commands[interpreter.Index].Code}");
                    break;
                default:
                    AssertTrue(
                        false,
                        $"an event list of this game stopped as"
                        + $" {interpreter.Stopped}: {interpreter.Malformed}"
                        + $" {interpreter.Reason}");
                    break;
            }
        }

        AssertTrue(
            finished > 0,
            $"and {finished} of {lists.Count} ran through without stopping, with"
            + $" {refused} refused by name, {frozen} frozen by the limit and"
            + $" {waiting} waiting for frames");
        // **And whether anything froze is now a question with two answers,
        // and the honest one is measured rather than asserted.**
        //
        // **K-133 removed the freeze this test used to demand.** Before 101
        // was dispatched, event 4's seven 101s were seven unknown commands
        // the interpreter ran past in one frame, the 413 came round again at
        // once, and the step limit caught it. **Now each 101 takes a line and
        // holds the page** — one dialogue per frame — so the same list takes
        // seven frames to get to the 413 and the freeze never arrives.
        //
        // **That is the engine's answer, and a test that demanded the freeze
        // was demanding a reader that did not wait for the player.** The
        // numbers below say what actually happened; the reader's limit is
        // still there and still says the same thing when a list really does
        // go on, which event 6's 655s and this game's 112 do when nothing
        // releases them.
        AssertEq(
            finished + refused + frozen + waiting, lists.Count,
            "and every list answered with one of the four, so none of them"
            + $" ran off without saying; there are {lists.Count} lists and"
            + $" {finished + refused + frozen + waiting} answers"
            + $" ({finished} through, {refused} refused, {frozen} frozen,"
            + $" {waiting} waiting)");
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
    /// <summary>
    /// A command the engine has no method for is stepped over.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And that is implemented, and not missing.</strong> Four of
    /// the 114 names this table carries are named and treated nowhere:
    /// <c>355 Script</c>, <c>402 ContinueText</c>, <c>405 ShowChoices</c>
    /// and <c>412 EndBranch</c>. Measured end to end, **a list that
    /// consists of one of them ends with <c>Finished</c> and zero
    /// actions.**
    /// </para>
    /// <para>
    /// <strong>And that is the engine rule</strong> — it asks whether a
    /// method exists for the number and advances anyway when it does not
    /// (<c>MzInterpreter</c> line 265). **And a reader that treated a
    /// skipped command as a refusal would stop every list that contains
    /// one**, **and a list with an <c>EndBranch</c> is every
    /// conditional in a game.**
    /// </para>
    /// <para>
    /// <strong>And I reported these as missing first</strong>, because I
    /// looked in the two dispatch switches and not in the path that catches
    /// them. **And a command the engine steps over is implemented** — **and
    /// the difference between "not treated" and "not executed" is the
    /// difference between a command with no effect and a command that does
    /// not exist.**
    /// </para>
    /// </remarks>
    public void Test_ACommandWithNoMethodIsSteppedOver()
    {
        var namen = new[]
        {
            ("EndBranch", MzCommandTable.EndBranch,
                System.Array.Empty<string>()),
            ("ContinueText", MzCommandTable.ContinueText,
                System.Array.Empty<string>()),
            ("ShowChoices", MzCommandTable.ShowChoices, new[] { "1", "1" }),
            ("Script", MzCommandTable.Script, new[] { "1", "0", "0", "x" }),
        };

        foreach (var (name, code, parameter) in namen)
        {
            var liste = ListFrom((code, 0, parameter));
            var lauf = Run(liste, out var aktionen);

            AssertEq(lauf.Stopped, MzStep.Finished,
                $"**a list of only {name} runs to its end** -- and a reader"
                + " that treated a skipped command as a refusal would stop"
                + " every list that contains one");
            AssertEq(aktionen.Count, 0,
                $"**and it records nothing for {name}** -- there is no"
                + " effect to record, and a log that said otherwise would"
                + " be a log of the reader rather than of the game");
        }
    }
}
