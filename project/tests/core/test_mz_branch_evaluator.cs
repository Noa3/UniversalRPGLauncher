using System.Collections.Generic;
using Godot;
using UniversalRPG.Tests.Framework;
using UniversalRPG.Web;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// A conditional branch of a real game's event, decided the way the engine does.
/// </summary>
/// <remarks>
/// <para>
/// The numbers were read out of the engine's own <c>command111</c>: which
/// parameter holds the kind of thing being tested, which holds the way of
/// testing it, which hold the operands. A branch is now answered rather than
/// merely named.
/// </para>
/// <para>
/// The three branches this test uses as their subjects are the ones this game
/// actually stores, read out of its own map: a switch, and two variable
/// comparisons against other variables. Every other kind is exercised with the
/// parameters the engine reads, and the one that is not decided here is decided
/// on purpose.
/// </para>
/// </remarks>
partial class TestMzBranchEvaluator : TestBase
{
    private const string FixtureRoot = "res://tests/fixtures/mz";

    public void Test_ASwitchBranchComesToWhatTheSwitchIs()
    {
        // This game's own branch, read out of its map: switch thirteen, asked
        // about as "not on". The engine asks whether it is on when the second
        // parameter is zero and whether it is off when it is anything else.
        var branch = MzBranch.FromParameters(["0", "13", "1"]);
        AssertEq(branch.Kind, MzBranchKind.Switch, "it is a branch on a switch");

        var on = new MzBranchFacts { Switches = { [13] = true } };
        var off = new MzBranchFacts { Switches = { [13] = false } };

        AssertEq(
            MzBranchEvaluator.Evaluate(branch, on).Outcome, MzBranchOutcome.False,
            "and a switch that is on makes the branch asking for off come to false");
        AssertEq(
            MzBranchEvaluator.Evaluate(branch, off).Outcome, MzBranchOutcome.True,
            "while a switch that is off comes to true");
    }

    public void Test_AVariableBranchComparesTwoVariables()
    {
        // This game's own branch, read out of its map and out of the engine
        // beside it: variable seventy seven, the right side is a variable rather
        // than a number, that variable is seventy eight, and the way of testing
        // is 1.
        //
        // Two things a first draft of this test got wrong, and both were found
        // by the game rather than by reasoning. **The way of testing is "at
        // least", not "equal to"** — 1 is the second of six and not the first.
        // And **the right hand variable is the fourth parameter, not the
        // third**: the engine reads `params[2] === 0` only to ask whether the
        // right side is a number, and the variable's own number is `params[3]`.
        // A reader that read the third would ask about variable one where the
        // game asked about seventy eight, and would refuse a branch it could
        // have answered.
        var branch = MzBranch.FromParameters(["1", "77", "1", "78", "1"]);
        AssertEq(branch.Kind, MzBranchKind.Variable, "it is a branch on a variable");

        var holds = new MzBranchFacts
        {
            Variables = { [77] = 5, [78] = 3 },
        };
        var result = MzBranchEvaluator.Evaluate(branch, holds);
        AssertEq(
            result.Outcome, MzBranchOutcome.True,
            "a variable holding five is at least one holding three");
        AssertEq(
            result.Comparison, "at least",
            "and the answer says how it compared them, which is not the same"
            + " thing as the kind of branch");

        var alsoHolds = new MzBranchFacts
        {
            Variables = { [77] = 3, [78] = 3 },
        };
        AssertEq(
            MzBranchEvaluator.Evaluate(branch, alsoHolds).Outcome,
            MzBranchOutcome.True,
            "and three is at least three, which an equality test would also have"
            + " said and cannot be told from by the answer alone");

        var doesNot = new MzBranchFacts
        {
            Variables = { [77] = 2, [78] = 3 },
        };
        AssertEq(
            MzBranchEvaluator.Evaluate(branch, doesNot).Outcome,
            MzBranchOutcome.False,
            "while two is not at least three");
    }

    public void Test_TheThirdParameterOnlySaysWhetherTheRightSideIsAVariable()
    {
        // The detail above, on its own, because it is the one a reader gets
        // wrong and cannot see. Both of these branches look the same to a reader
        // that has not read the engine: the third parameter is 1 in one and 0 in
        // the other, and in one the right side is variable 78 and in the other
        // it is the number 500.
        var againstVariable = MzBranch.FromParameters(["1", "77", "1", "78", "1"]);
        var againstNumber = MzBranch.FromParameters(["1", "77", "0", "500", "1"]);

        var facts = new MzBranchFacts
        {
            Variables = { [77] = 600, [1] = 0, [78] = 700 },
        };

        AssertEq(
            MzBranchEvaluator.Evaluate(againstVariable, facts).Outcome,
            MzBranchOutcome.False,
            "six hundred is not at least variable seventy eight, which holds seven"
            + " hundred");
        AssertEq(
            MzBranchEvaluator.Evaluate(againstNumber, facts).Outcome,
            MzBranchOutcome.True,
            "and six hundred is at least the number five hundred");

        // The one that shows the difference cannot be papered over: variable one
        // holds zero, so a reader that took the third parameter as the variable
        // would compare six hundred with zero and say true for the wrong reason.
        var papered = new MzBranchFacts { Variables = { [77] = 1, [78] = 900 } };
        AssertEq(
            MzBranchEvaluator.Evaluate(againstVariable, papered).Outcome,
            MzBranchOutcome.False,
            "and a variable holding one is still not at least a variable holding"
            + " nine hundred, whatever the third parameter says");
    }

    public void Test_EachWayOfComparingIsItsOwnRuleAndNotTheOneBefore()
    {
        // Six ways, and the boundary between them is where a reader that wrote
        // one rule for all six would be wrong without anything looking wrong.
        var cases = new (int pCode, int pLeft, int pRight, MzBranchOutcome pWanted)[]
        {
            (0, 5, 5, MzBranchOutcome.True),    // equal
            (0, 5, 6, MzBranchOutcome.False),
            (1, 5, 5, MzBranchOutcome.True),    // at least
            (1, 6, 5, MzBranchOutcome.True),
            (1, 4, 5, MzBranchOutcome.False),
            (2, 5, 5, MzBranchOutcome.True),    // at most
            (2, 4, 5, MzBranchOutcome.True),
            (2, 6, 5, MzBranchOutcome.False),
            (3, 6, 5, MzBranchOutcome.True),    // greater than: 6 is not 6
            (3, 5, 5, MzBranchOutcome.False),
            (4, 4, 5, MzBranchOutcome.True),    // less than
            (4, 5, 5, MzBranchOutcome.False),
            (5, 5, 6, MzBranchOutcome.True),    // not equal to
            (5, 5, 5, MzBranchOutcome.False),
        };

        // The third parameter is 1, so the right side is a variable, and the
        // fourth says which. A first draft passed 0 there, which asks the
        // opposite question: the reader compared variable 0 with the number 3
        // and the harness said it had compared two variables.
        foreach (var (code, left, right, wanted) in cases)
        {
            var branch = MzBranch.FromParameters(
                ["1", "0", "1", "3", code.ToString()]);
            var facts = new MzBranchFacts { Variables = { [0] = left, [3] = right } };
            AssertEq(
                MzBranchEvaluator.Evaluate(branch, facts).Outcome, wanted,
                $"comparing {left} with {right} by way {code} comes to {wanted}");
        }
    }

    /// <summary>A switch nobody supplied is off, because the engine says so.</summary>
    /// <remarks>
    /// <para>
    /// <strong>And this test used to say the opposite, and in words:</strong>
    /// "a switch nobody has supplied is not called off", "this reader
    /// treated what it does not know as off, and a game content would be
    /// skipped with nothing to show for it".
    /// </para>
    /// <para>
    /// <strong>And that reasoning was wrong, and the engine settles it.</strong>
    /// Measured at <c>Game_Switches.prototype.value</c>: <c>return
    /// !!this._data[switchId];</c> — <strong>and <c>setValue</c> writes
    /// only when <c>switchId &gt; 0 &amp;&amp; switchId &lt;
    /// $dataSystem.switches.length</c></strong> — <strong>and so every
    /// switch outside that range is off for the whole game, and no
    /// caller can supply it.</strong>
    /// </para>
    /// <para>
    /// <strong>And <c>command111</c> compares <c>value(params[1]) ===
    /// (params[2] === 0)</c></strong> — <strong>and the brackets stand
    /// around the second condition</strong> — <strong>and
    /// <c>false === false</c> is true.</strong> <strong>And this is not
    /// theory: measured at Map011 event 6, the game carries</strong>
    /// <strong><c>[0, 10, 0]</c>, and without this rule one of its four
    /// branches could not be answered at all.</strong>
    /// </para>
    /// </remarks>
    public void Test_ASwitchThatIsNotKnownIsOffBecauseTheEngineSaysSo()
    {
        var branch = MzBranch.FromParameters(["0", "99", "0"]);
        //
        // **Und ich habe zuerst das Gegenteil behauptet** -- **und der
        // Zweig des Lesers war die ganze Zeit richtig.** **Ein Kommentar,
        // der die Frage verkehrt herum benennt, faehrt einen Test mit,
        // der die Antwort verkehrt herum prueft.**
AssertEq(
            MzBranchEvaluator.Evaluate(
                branch, new MzBranchFacts()).Outcome,
            MzBranchOutcome.False,
            "**and a switch nobody supplied is off** -- because the"
                + " engine value() is `!!this._data[switchId]`, and a"
                + " slot that was never written is falsy, and"
                + " `false === true` is false");

        var fragtNachAn = MzBranch.FromParameters(["0", "99", "0"]);

        AssertEq(
            MzBranchEvaluator.Evaluate(
                fragtNachAn, new MzBranchFacts()).Outcome,
            MzBranchOutcome.False,
            "**and asking whether that same switch is on comes to false"
                + "** -- because `command111` says `value(params[1]) ==="
                + " (params[2] === 0)`, and the brackets stand around the"
                + " ANSWER and not around the question, and"
                + " `params[2] === 0` is exactly when it asks whether the"
                + " switch is on");

        var fragtNachAus = MzBranch.FromParameters(["0", "99", "1"]);

        AssertEq(
            MzBranchEvaluator.Evaluate(
                fragtNachAus, new MzBranchFacts()).Outcome,
            MzBranchOutcome.True,
            "**and asking whether it is off comes to true** -- and that"
                + " is the other side of the same comparison, and a"
                + " reader that read the third parameter the other way"
                + " round would take the wrong branch of this game's own"
                + " pages");

        // **Und derselbe Fall mit einer gesetzten Nummer, denn es ist
        // derselbe Pfad** -- **und gemessen ist `[8, 2]` und `[1, 1,
        // 0, 10, 1]` in diesem Spiel, und die beiden brauchen echten
        // Zustand** -- **und der Schalter nicht, denn es gibt keinen.**
        // **Und derselbe Zweig mit gesetztem Schalter, denn sonst
        // waere der ganze Test eine Konstante.**
        var gesetzt = new MzBranchFacts { Switches = { [99] = true } };
        AssertEq(
            MzBranchEvaluator.Evaluate(branch, gesetzt).Outcome,
            MzBranchOutcome.True,
            "**and the very same branch comes to true once the switch"
                + " is on** -- and the two together are the whole rule:"
                + " a switch nobody supplied is off, and the same"
                + " switch set to on flips the branch");
    }
    public void Test_ABranchThatAsksForTheAuthorsOwnScriptIsReportedAndNotRun()
    {
        // The engine writes `result = !!eval(params[1])` for this kind. This
        // repository does not evaluate a game's JavaScript and the one thing it
        // may not do is answer as though it had. The branch is reported, the
        // author's text is kept, and nothing is evaluated.
        const string authorsLine = "1 + 1 === 2";
        var branch = MzBranch.FromParameters(["12", authorsLine]);

        AssertEq(branch.Kind, MzBranchKind.Script, "it is a branch on a script");
        AssertEq(
            branch.ScriptText, authorsLine,
            "and the author's own line is kept as it was written");

        var result = MzBranchEvaluator.Evaluate(branch, new MzBranchFacts());
        AssertEq(
            result.Outcome, MzBranchOutcome.ScriptNotRun,
            "and the answer is that it was not run, and not whether the line is"
            + " true");
        AssertTrue(
            result.Outcome != MzBranchOutcome.True
            && result.Outcome != MzBranchOutcome.False,
            "which is the point: it says neither true nor false, because saying"
            + " either would be a claim about code this repository did not run");
    }

    public void Test_GoldIsComparedByItsOwnNumberingAndNotTheVariables()
    {
        // The engine numbers gold on its own: `switch (params[2])` with case 0 at
        // least, case 1 at most, case 2 less. A variable's numbering is 0 equal,
        // 1 at least, 2 at most, 3 greater, 4 less, 5 not equal. **The first two
        // are the same words in a different order**, and a reader that read the
        // gold number through the variable's table tested a hundred gold for
        // equality and said it was not enough.
        var facts = new MzBranchFacts { Gold = 100 };

        AssertEq(
            MzBranchEvaluator.Evaluate(
                MzBranch.FromParameters(["7", "100", "0"]), facts).Outcome,
            MzBranchOutcome.True, "a hundred gold is at least a hundred");
        AssertEq(
            MzBranchEvaluator.Evaluate(
                MzBranch.FromParameters(["7", "100", "1"]), facts).Outcome,
            MzBranchOutcome.True, "and is at most a hundred");
        AssertEq(
            MzBranchEvaluator.Evaluate(
                MzBranch.FromParameters(["7", "100", "2"]), facts).Outcome,
            MzBranchOutcome.False, "but is not less than a hundred");

        // The one that would have been wrong. Read through the variable's
        // numbering, case 1 is "at least" and case 2 is "at most", and a party
        // holding ninety would pass the second and a party holding a thousand
        // would pass the first. Under gold's own numbering the answers are the
        // other way round, and a game that gates a purchase on a hundred gold
        // opens at ninety and shuts at a hundred and ten.
        var poor = new MzBranchFacts { Gold = 90 };
        var rich = new MzBranchFacts { Gold = 110 };
        AssertEq(
            MzBranchEvaluator.Evaluate(
                MzBranch.FromParameters(["7", "100", "1"]), poor).Outcome,
            MzBranchOutcome.True, "so ninety gold is at most a hundred");
        AssertEq(
            MzBranchEvaluator.Evaluate(
                MzBranch.FromParameters(["7", "100", "1"]), rich).Outcome,
            MzBranchOutcome.False, "and a hundred and ten is not");
        AssertEq(
            MzBranchEvaluator.Evaluate(
                MzBranch.FromParameters(["7", "100", "4"]), facts).Outcome,
            MzBranchOutcome.Unknown,
            "and a way of testing gold the engine does not have is refused"
            + " rather than guessed at");
    }

    public void Test_ATimerIsNotComparedWhenItIsNotRunning()
    {
        // The engine compares a timer only when it is running, and a timer that
        // is not running is the editor's own -1. A reader that compared it
        // anyway would say a stopped timer has run for no time, which is true,
        // and would then answer a branch that means "has this been running for
        // five seconds yet" with a yes before the timer started.
        var running = new MzBranchFacts { TimerSeconds = 5 };
        var stopped = new MzBranchFacts { TimerSeconds = -1 };

        AssertEq(
            MzBranchEvaluator.Evaluate(
                MzBranch.FromParameters(["3", "5", "0"]), running).Outcome,
            MzBranchOutcome.True, "a timer at five has run for at least five");
        AssertEq(
            MzBranchEvaluator.Evaluate(
                MzBranch.FromParameters(["3", "10", "0"]), running).Outcome,
            MzBranchOutcome.False, "and one at five has not run for ten");
        AssertEq(
            MzBranchEvaluator.Evaluate(
                MzBranch.FromParameters(["3", "5", "0"]), stopped).Outcome,
            MzBranchOutcome.Unknown,
            "and a timer that is not running is not compared at all");
        AssertTrue(
            MzBranchEvaluator.Evaluate(
                MzBranch.FromParameters(["3", "5", "0"]), stopped).Missing
                .Contains("not running"),
            "and the refusal says so, so a caller can tell it apart from a"
            + " branch about something else it does not have");
    }

    public void Test_AnItemIsHeldWhenTheBagHoldsAtLeastAsManyAsAsked()
    {
        // The engine asks whether the party has the item and, for a weapon or an
        // armour, at least how many. Zero is the number a single item is, so a
        // branch written before the count existed and a branch written after it
        // mean the same thing and both must be read.
        var bag = new MzBranchFacts
        {
            Items = { [3] = 2 },
            Weapons = { [1] = 1 },
        };

        AssertEq(
            MzBranchEvaluator.Evaluate(
                MzBranch.FromParameters(["8", "3"]), bag).Outcome,
            MzBranchOutcome.True, "an item in the bag is held");
        AssertEq(
            MzBranchEvaluator.Evaluate(
                MzBranch.FromParameters(["8", "4"]), bag).Outcome,
            MzBranchOutcome.Unknown, "and an item nobody has is refused");
        AssertEq(
            MzBranchEvaluator.Evaluate(
                MzBranch.FromParameters(["9", "1", "1"]), bag).Outcome,
            MzBranchOutcome.True, "a weapon held once is held at least once");
        AssertEq(
            MzBranchEvaluator.Evaluate(
                MzBranch.FromParameters(["9", "1", "2"]), bag).Outcome,
            MzBranchOutcome.False, "but is not held twice");
    }

    public void Test_EveryKindOfBranchIsTheOneTheEngineNamesAndNoOther()
    {
        // The mutation that got through the first suite folded a kind the engine
        // has no name for into the nearest kind it does have. That is the shape
        // of a whole family of mistakes here: a number from a newer editor, a
        // number from a corrupt file, a number from a game of another generation
        // in the same file, and each would be decided as though the game had
        // meant the nearest thing this reader knows.
        //
        // Fourteen kinds and the engine's own list of them, checked in both
        // directions: a number that is a kind is that kind, and a number that is
        // not is not one of them however near it is.
        var engineKinds = new[]
        {
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13,
        };
        foreach (var kind in engineKinds)
        {
            var branch = MzBranch.FromParameters([kind.ToString(), "0"]);
            AssertEq(
                (int)branch.Kind, kind,
                $"{kind} is a kind of branch this reader has a name for");
        }

        foreach (var kind in new[] { 14, 15, 20, 50, 99, 100, 111, 355, 400 })
        {
            var branch = MzBranch.FromParameters([kind.ToString(), "0"]);
            var result = MzBranchEvaluator.Evaluate(branch, new MzBranchFacts());
            AssertEq(
                result.Outcome, MzBranchOutcome.Unknown,
                $"and {kind} is not folded into the kind below it");
            AssertTrue(
                result.Missing.Contains(kind.ToString()),
                $"with the number in the refusal, so a caller can see what the"
                + $" game asked: {result.Missing}");
        }

        // 411 and 413 are commands and 412 is not a kind of branch at all, so
        // none of the three may be decided as one. A reader that treated the
        // numbers near 111 as kinds would read a branch's else as a test.
        foreach (var notAKind in new[] { 0, 401, 411, 412, 413, 501, 605, 655, 657 })
        {
            if (notAKind < 14)
            {
                continue;
            }
            var branch = MzBranch.FromParameters([notAKind.ToString(), "0"]);
            AssertEq(
                MzBranchEvaluator.Evaluate(branch, new MzBranchFacts()).Outcome,
                MzBranchOutcome.Unknown,
                $"{notAKind} is a number in a list, not a kind of branch, and is"
                + " not decided as one");
        }
    }

    public void Test_ABranchKeepsTheNumberTheGameGaveItWhateverBecomesOfIt()
    {
        // The check that should have caught a folded kind and did not. The
        // evaluator names what it needs in its own words, so a branch on 99
        // that was folded into 0 reports "switch 0" — and "switch 0" does not
        // contain "99". The refusal was missing the number, the check passed,
        // and the mutation was not seen.
        //
        // The number is in the branch, not in the complaint about it, so the
        // branch is what is checked. A fold that changes what the branch is
        // cannot hide behind a message that is itself already wrong.
        foreach (var kind in new[] { 14, 15, 20, 50, 99, 100, 111, 355, 401, 657 })
        {
            var branch = MzBranch.FromParameters([kind.ToString(), "0"]);
            AssertEq(
                (int)branch.Kind, kind,
                $"{kind} is still {kind} after the branch has been read,"
                + " whatever the evaluator then makes of it");
        }

        // And the two that sit right below a real kind, where a fold would
        // change the most: 13 is the last kind the engine has and 14 is the
        // first number that is not one.
        AssertEq(
            (int)MzBranch.FromParameters(["13", "0"]).Kind, 13,
            "thirteen is the last kind there is");
        AssertEq(
            (int)MzBranch.FromParameters(["14", "0"]).Kind, 14,
            "and fourteen is the first number that is not one");
    }

    public void Test_ThisGamesOwnBranchesAreAllDecidedAndNoneIsRefused()
    {
        // The whole of what this game stores, read out of its own map, with
        // facts that make each one answerable. Three branches, and every one of
        // them is decided rather than refused — which is the difference between
        // a branch table that exists and one that is consulted.
        var branches = BranchesInThisGame();
        AssertTrue(
            branches.Count >= 3,
            $"the game stores {branches.Count} branches in the one map read here");

        // The three branches this game stores are a switch on thirteen, a
        // comparison of variable seventy seven with variable seventy eight, and
        // a comparison of variable one hundred and eighty with the number zero,
        // tested for "not equal to". The first draft of these facts had the
        // first two and not the third, so a branch the reader could have
        // answered came back refused — which is the failure this test is for.
        var facts = new MzBranchFacts
        {
            Switches = { [13] = true },
            Variables = { [77] = 3, [78] = 3, [180] = 7 },
        };

        var refused = new List<string>();
        var undecided = 0;
        foreach (var parameters in branches)
        {
            var result = MzBranchEvaluator.Evaluate(
                MzBranch.FromParameters(parameters), facts);
            if (result.Outcome == MzBranchOutcome.Unknown)
            {
                refused.Add($"{result.Missing} in [{string.Join(",", parameters)}]");
            }
            if (result.Outcome == MzBranchOutcome.ScriptNotRun)
            {
                undecided++;
            }
        }

        AssertTrue(
            refused.Count == 0,
            "and every one of them is decided from what the game asked;"
            + " refused: " + string.Join("; ", refused));
        AssertEq(
            undecided, 0,
            "and none of them is a branch on the author's own script, which"
            + " would be reported and not run");
    }

    private static List<List<string>> BranchesInThisGame()
    {
        var found = new List<List<string>>();
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
                foreach (var command in page.Member("list")!.Items)
                {
                    if (command.Member("code")?.IntOr(0) != 111)
                    {
                        continue;
                    }
                    var parameters = command.Member("parameters")!;
                    var values = new List<string>();
                    foreach (var value in parameters.Items)
                    {
                        values.Add(value.Kind == MzKind.Number
                            ? System.Math.Round(value.Number).ToString()
                            : value.Text);
                    }
                    found.Add(values);
                }
            }
        }
        return found;
    }
}
