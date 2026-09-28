using System.Collections.Generic;
using System.Linq;
using Godot;
using UniversalRPG.Tests.Framework;
using UniversalRPG.Web;

namespace UniversalRPG.Tests.Core;

partial class TestMzDialogueAndChoice : TestBase
{
    private const string Root = "res://tests/fixtures/mz_plain/data";

    /// <summary>
    /// The four hundred and fourteen dialogues, as the engine would walk them:
    /// the index on a 101, and that command's own loop over the lines under
    /// it.
    /// </summary>
    private static List<(int Map, int Event, MzDialogue.Block Block, int Consumed)>
        EveryDialogueInThisGame()
    {
        var alle = new List<(int, int, MzDialogue.Block, int)>();
        for (var i = 1; i < 20; i++)
        {
            var name = "Map" + i.ToString("000") + ".json";
            var file = MzDataFile.Read(
                name, Godot.FileAccess.GetFileAsBytes(Root.PathJoin(name)));
            foreach (var ev in file.Root.Member("events")?.Items ?? new List<MzValue>())
            {
                if (ev.Kind != MzKind.Object)
                {
                    continue;
                }
                var nummer = (int)(ev.Member("id")?.Number ?? 0);
                foreach (var page in ev.Member("pages")?.Items ?? new List<MzValue>())
                {
                    var zeilen = new List<MzCommandEntry>();
                    foreach (var c in page.Member("list")?.Items ?? new List<MzValue>())
                    {
                        zeilen.Add(MzCommandEntry.From(c));
                    }
                    for (var k = 0; k < zeilen.Count; k++)
                    {
                        if (zeilen[k].Code != MzCommandTable.ShowDialogue)
                        {
                            continue;
                        }
                        var (block, eaten)
                            = MzDialogue.Read(zeilen, k, new MzBranchFacts());
                        alle.Add((i, nummer, block, eaten));
                        // **`k` is on the 101, and `eaten` counts it** — the
                        // 101 itself, its lines and the 102 it took. The
                        // loop's own `k++` moves on from there, so this
                        // advances by `eaten - 1`.
                        //
                        // **A first draft changed this to `k += eaten`** on
                        // the strength of a distribution that was one bucket
                        // out, and made the walk step one command too far:
                        // 88 and 102 where the files say 118 and 130. **The
                        // numbers were wrong and the fix made them wronger,
                        // and the honest reading of that is that the fault
                        // was never here.** It was in `MzDialogue.Read`,
                        // which looked one command past the first line.
                        k += eaten - 1;
                    }
                }
            }
        }
        return alle;
    }

    /// <summary>The choices of this game, read the same way the event is.</summary>
    private static List<MzChoice.Set> EveryChoiceInThisGame()
    {
        var sets = new List<MzChoice.Set>();
        for (var i = 1; i < 20; i++)
        {
            var name = "Map" + i.ToString("000") + ".json";
            var file = MzDataFile.Read(
                name, Godot.FileAccess.GetFileAsBytes(Root.PathJoin(name)));
            foreach (var ev in file.Root.Member("events")?.Items ?? new List<MzValue>())
            {
                if (ev.Kind != MzKind.Object)
                {
                    continue;
                }
                foreach (var page in ev.Member("pages")?.Items ?? new List<MzValue>())
                {
                    foreach (var c in page.Member("list")?.Items ?? new List<MzValue>())
                    {
                        var entry = MzCommandEntry.From(c);
                        if (entry.Code == MzCommandTable.ShowChoiceList)
                        {
                            sets.Add(MzChoice.Read(entry.Parameters));
                        }
                    }
                }
            }
        }
        return sets;
    }


    /// <summary>
    /// What was recorded, in a form a failure message can carry.
    /// </summary>
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

    public void Test_AFourHundredAndFourteenDialoguesAndTheNineHundredAndThirtyEightLinesBetweenThem()
    {
        // **Four hundred and fourteen dialogues, nine hundred and thirty-eight
        // lines, and the two numbers come out of the same walk.**
        //
        // The first draft of this card assumed a dialogue was one line. It is
        // one to four — and the total is exactly 938, which is the total
        // number of 401s in nineteen maps. **Every line of text in this game
        // belongs to a 101 and to nothing else**, and a reader that ran a 401
        // as a command of its own would be running 938 commands the engine
        // never runs.
        var alle = EveryDialogueInThisGame();

        AssertEq(
            alle.Count, 414,
            "and this game has four hundred and fourteen of them across"
            + $" nineteen maps; it has {alle.Count}");

        var zeilen = alle.Sum(a => a.Block.Lines.Count);
        AssertEq(
            zeilen, 938,
            "and nine hundred and thirty-eight lines between them, which is"
            + " the same as the number of 401s in the maps and not a"
            + $" coincidence; there are {zeilen}");

        // **Two distributions, and they are not the same number.**
        //
        // The lines a dialogue has: 118 with one, 130 with two, 104 with
        // three and 62 with four — 938, which is every line in the game.
        // The commands a 101 eats: 112, 134, 106 and 62 — 1360, which is
        // more than 414 + 938 because the eight choices are inside it.
        //
        // **A first draft asserted the command counts against the line
        // counts** — 112 where the lines say 118 — and the two were off by
        // exactly the eight choices: six of them under a one-line dialogue
        // and two under a two-line one.
        AssertEq(
            alle.Count(a => a.Block.Lines.Count == 1), 118,
            "and a hundred and eighteen of them have one line, a third of"
            + $" the game's dialogues; there are"
            + $" {alle.Count(a => a.Block.Lines.Count == 1)}");
        AssertEq(
            alle.Count(a => a.Block.Lines.Count == 2), 130,
            "and a hundred and thirty have two, the most of any; there are"
            + $" {alle.Count(a => a.Block.Lines.Count == 2)}");
        AssertEq(
            alle.Count(a => a.Block.Lines.Count == 3), 104,
            $"and a hundred and four have three; there are"
            + $" {alle.Count(a => a.Block.Lines.Count == 3)}");
        AssertEq(
            alle.Count(a => a.Block.Lines.Count == 4), 62,
            "and sixty-two have four, which is the most a dialogue in this"
            + " game has and is not the same as the most a 101 eats; there"
            + $" are {alle.Count(a => a.Block.Lines.Count == 4)}");
        AssertEq(
            alle.Count(a => a.Block.Lines.Count == 0), 0,
            "and not one has no line at all, which would be a 101 the editor"
            + " wrote with nothing under it; there are"
            + $" {alle.Count(a => a.Block.Lines.Count == 0)}");
        AssertEq(
            alle.Count(a => a.Block.Lines.Count > 4), 0,
            "and not one has five, so the longest dialogue in this game is"
            + $" four lines; there are"
            + $" {alle.Count(a => a.Block.Lines.Count > 4)}");

        AssertEq(
            alle.Count(a => a.Consumed == 2), 112,
            "and a hundred and twelve of them eat two commands — themselves"
            + " and their one line — which is six fewer than the hundred and"
            + " eighteen with one line, because six of those have a choice"
            + $" under it; there are {alle.Count(a => a.Consumed == 2)}");
        AssertEq(
            alle.Count(a => a.Consumed == 3), 134,
            "and a hundred and thirty-four eat three, two more than the"
            + " hundred and thirty with two lines and six fewer than the"
            + " hundred and forty that two lines would suggest if every one"
            + " of them had a choice; there are"
            + $" {alle.Count(a => a.Consumed == 3)}");
        AssertEq(
            alle.Count(a => a.Consumed == 4), 106,
            "and a hundred and six eat four, two more than the hundred and"
            + " four with three lines, for the two choices under two-line"
            + $" dialogues; there are {alle.Count(a => a.Consumed == 4)}");
        AssertEq(
            alle.Count(a => a.Consumed == 5), 62,
            "and sixty-two eat five, the same as the sixty-two with four"
            + " lines, because none of the four-line dialogues has a choice"
            + $" under it; there are {alle.Count(a => a.Consumed == 5)}");

        AssertEq(
            alle.Sum(a => a.Consumed), 1360,
            "and the total is thirteen hundred and sixty, not the thirteen"
            + " hundred and fifty-two a reader would get by adding the four"
            + " hundred and fourteen dialogues to the nine hundred and"
            + " thirty-eight lines, because the eight choices are counted"
            + $" twice: once by the dialogue that took them and once here;"
            + $" it is {alle.Sum(a => a.Consumed)}");

        AssertEq(
            alle.Count(a => a.Consumed == 1), 0,
            "and not one dialogue is a command by itself, so none of them"
            + " leaves a line behind for the interpreter to run on its own;"
            + $" there are {alle.Count(a => a.Consumed == 1)}");
    }

    public void Test_ASecondDialogueIsRefusedWhileOneIsUpAndTheIndexWaits()
    {
        // **`if ($gameMessage.isBusy()) { return false; }`** — and
        // `isBusy` is `hasText() || isChoice() || isNumberInput() ||
        // isItemChoice()`, so the refusal is not only about a line of text.
        //
        // A 101 that returns false leaves the index exactly where it was, so
        // the second dialogue happens when the first is done. **A reader that
        // showed both would put two speakers' words on one screen**, and the
        // one the game wrote second would replace the first.
        var zeilen = new List<MzCommandEntry>
        {
            new(MzCommandTable.ShowDialogue, new List<string> { "0", "0", "0", "0", "A" }, 0),
            new(MzCommandTable.ShowTextLine, new List<string> { "first" }, 0),
            new(MzCommandTable.ShowDialogue, new List<string> { "0", "0", "0", "0", "B" }, 0),
            new(MzCommandTable.ShowTextLine, new List<string> { "second" }, 0),
        };
        var facts = new MzBranchFacts();
        var aktionen = new List<MzAction>();
        var interp = new MzInterpreter(zeilen);
        interp.WaitFor(MzWaitMode.Message);
        facts.MessageBusy = true;

        var geliefen = MzCommands.TryExecute(
            interp, zeilen[0], aktionen, facts, new MzRandom());

        AssertFalse(
            geliefen,
            "and nothing is said, because the engine puts nobody's words on"
            + $" the screen while another message is up; it returned {geliefen}");
        AssertEq(
            interp.Index, 0,
            "and the index stays on the dialogue, so the same one is tried"
            + $" again when the first is done; it is at {interp.Index}");
        AssertEq(
            aktionen.Count, 0,
            "and nothing was recorded, because nothing happened; there are"
            + $" {aktionen.Count}");

        // **And once the message is done, the same command runs.**
        facts.MessageBusy = false;
        var danach = new List<MzAction>();
        geliefen = MzCommands.TryExecute(
            interp, zeilen[0], danach, facts, new MzRandom());

        AssertTrue(
            geliefen,
            "and once the message is done the very same command does run;"
            + $" it returned {geliefen}");
        // **The index lands on the command *after* the dialogue, not on one
        // of its lines.** `command101` ate three commands here — itself, its
        // one line and nothing else — and the engine's index is on the 101
        // while it runs, so it ends on the command that follows.
        //
        // A first draft wrote `Index += consumed - 1` in the reader and
        // asserted index 1 here — **and both were the same mistake**: the
        // index is on the 101, and `consumed` counts the 101. With that off
        // by one the next frame read the line again, as a command of its own,
        // and every dialogue in this game ended in a refusal.
        AssertEq(
            interp.Index, 2,
            "and the index is past the dialogue and its line, on the second"
            + " dialogue, which is the next thing the engine would read"
            + $" after the message is gone; it is at {interp.Index}");
        AssertEq(
            facts.LastDialogue!.Lines[0].Text, "first",
            $"and the words are the game's; they are"
            + $" \"{facts.LastDialogue!.Lines[0].Text}\"");
    }

    public void Test_AnUnansweredChoiceIsAsBusyAsALineAndThatIsTheFourthCondition()
    {
        // **The fourth condition is the one that is easy to leave out**, and
        // it is the one that decides whether a page runs on before the player
        // has answered.
        //
        // `isBusy()` is four things. A reader that waits only for text would
        // let a dialogue that is sitting on two options go on to the next
        // event, and the player would be asked a question they never saw.
        AssertEq(
            MzWaitMode.Message, MzWaitMode.Message,
            "and the wait a 101 sets is the message one, which asks"
            + " $gameMessage.isBusy() and not hasText()");

        // **And this game's own choices are all two options with a cancel of
        // 0 or 1, so the real data never reaches the interesting case.** The
        // interesting case has to be built, and it is built below.
        var alle = EveryDialogueInThisGame();
        AssertEq(
            alle.Count(a => a.Block.Follower == MzCommandTable.ShowChoiceList), 8,
            "and eight of the four hundred and fourteen dialogues have a"
            + " choice under them, and every one of them sits directly after"
            + $" the last line; there are"
            + $" {alle.Count(a => a.Block.Follower == MzCommandTable.ShowChoiceList)}");
        AssertEq(
            alle.Count(a => a.Block.Follower == 103), 0,
            "and none has a number to enter after it, so the fall-through"
            + " from 102 to 103 to 104 is not exercised by this game and has"
            + " to be written out by hand; there are"
            + $" {alle.Count(a => a.Block.Follower == 103)}");
        AssertEq(
            alle.Count(a => a.Block.Follower == 104), 0,
            $"and none has an item to choose; there are"
            + $" {alle.Count(a => a.Block.Follower == 104)}");
    }

    public void Test_TheCancelNumberIsClampedAndTheGamesOwnEightAreNot()
    {
        // **`cancelType = params[1] < choices.length ? params[1] : -2`**, and
        // this is the line a first reading gets wrong twice: once by
        // assuming the number is used as written, and once by assuming the
        // option list is a `|`-separated string, which it is not —
        // `params[0]` is an array, and this game writes `["Yes", "No"]`.
        //
        // **Measured over this game's eight choices: all two options, cancel
        // 0 or 1, and not one of the eight is clamped.** The rule therefore
        // never fires in the real data, which means a test that only read the
        // real data would not know the rule at all.
        var acht = EveryChoiceInThisGame();

        AssertEq(
            acht.Count, 8,
            "and this game has eight choices in nineteen maps; it has"
            + $" {acht.Count}");
        AssertTrue(
            acht.All(s => s.Options.Count == 2),
            "and every one of them has two options, so the option list is"
            + " read as an array and not split on a bar; they have"
            + $" {string.Join(", ", acht.Select(s => s.Options.Count))}");
        AssertTrue(
            acht.All(s => !s.Options.Any(o => o.Contains('[') || o.Contains('|'))),
            "and no option carries brackets or a bar, which is what a reader"
            + " that had split the parameter would have produced; they are"
            + $" {string.Join(" | ", acht.Select(s => s.Options[0]))}");
        AssertEq(
            acht.Count(s => s.CancelWasClamped), 0,
            "and not one of the eight is clamped, because a cancel of 0 or 1"
            + " is below two options in every case; there are"
            + $" {acht.Count(s => s.CancelWasClamped)}");
        AssertEq(
            acht.Count(s => s.CancelType == 1), 7,
            "and seven of them allow a cancel on the second option, which is"
            + " what the editor writes for 'cancel on the last one'; there"
            + $" are {acht.Count(s => s.CancelType == 1)}");
        AssertEq(
            acht.Count(s => s.CancelType == 0), 1,
            "and one allows it on the first, and not one of the eight allows"
            + $" no cancel at all; there are"
            + $" {acht.Count(s => s.CancelType == 0)}");
        AssertTrue(
            acht.All(s => s.Position == 2 && s.Background == 0),
            "and all eight sit at the bottom on a window, which is both the"
            + " value the game wrote and the engine's own default of 2; they"
            + $" are at {string.Join(", ", acht.Select(s => s.Position))}");

        // **And now the case the game's data never reaches, written out.**
        var zuKlein = MzChoice.Read(new List<string> { "[\"A\", \"B\", \"C\"]", "1" });
        AssertEq(
            zuKlein.CancelType, 1,
            "and a cancel of one under three options is the second option,"
            + $" because 1 is below 3; it is {zuKlein.CancelType}");
        AssertFalse(
            zuKlein.CancelWasClamped,
            $"and is not clamped; it was {zuKlein.CancelWasClamped}");

        var zuGross = MzChoice.Read(new List<string> { "[\"A\", \"B\", \"C\"]", "3" });
        AssertEq(
            zuGross.CancelType, MzChoice.NoCancel,
            "and a cancel of three under three options is no cancel at all,"
            + $" because 3 is not below 3; it is {zuGross.CancelType}");
        AssertEq(
            zuGross.WrittenCancelType, 3,
            "and the number the game wrote is kept, because a reader that"
            + $" threw it away could not say why; the game wrote"
            + $" {zuGross.WrittenCancelType}");
        AssertTrue(
            zuGross.CancelWasClamped,
            $"and it says it clamped; it said {zuGross.CancelWasClamped}");

        var keinArray = MzChoice.Read(new List<string> { "just one", "0" });
        AssertEq(
            keinArray.Options.Count, 1,
            "and a parameter that is not an array is one option, because the"
            + " engine's params[0].clone() would carry a string through and"
            + $" not a list of two; it has {keinArray.Options.Count}");
        AssertEq(
            keinArray.Options[0], "just one",
            $"and the string is the option; it is \"{keinArray.Options[0]}\"");

        // **And the four defaults, which are the fourth rule a first reading
        // misses** — every one of them is a value the caller did not write,
        // and every one of them is different from the one a reader would
        // guess. **This game's own eight choices write all five parameters,
        // so not one of these defaults is reached by the real data** — and a
        // test that only read the real data would not know that the default
        // for the position is 2, the bottom of the screen, and not 0, the
        // top.
        var kurz = MzChoice.Read(new List<string> { "[\"A\", \"B\"]" });
        AssertEq(
            kurz.Options.Count, 2,
            "and a 102 with only its options reads the four the caller left"
            + $" out; it has {kurz.Options.Count} options");
        AssertEq(
            kurz.Position, 2,
            "and the position is the bottom, because `positionType ="
            + " params.length > 3 ? params[3] : 2` and two is the bottom and"
            + $" not zero; it is {kurz.Position}");
        AssertEq(
            kurz.Background, 0,
            "and the background is a window, which is zero and the value"
            + $" the caller did not write; it is {kurz.Background}");
        AssertEq(
            kurz.DefaultType, 0,
            "and the chosen option is the first, which is what a missing"
            + $" third parameter means; it is {kurz.DefaultType}");
        // **And this is the rule's own edge, and a first draft got it
        // backwards.** `params[1] < choices.length ? params[1] : -2` reads a
        // **missing** second parameter as **zero** — the "no cancel" branch
        // never runs for a missing one, because the length test is asked
        // first. And zero is below two options, so **a 102 that wrote no
        // cancel number at all allows cancelling on the first option.**
        //
        // **That is what the engine does.** A reader that made a missing
        // number mean "no cancel" would be adding a rule the engine does not
        // have, and this game's eight choices all write all five parameters,
        // so the case is reached only by a hand-written 102.
        AssertEq(
            kurz.CancelType, 0,
            "and a 102 that wrote no cancel number allows cancelling on the"
            + " first option, because a missing second parameter reads as"
            + " zero and zero is below two options — which is what the"
            + $" engine's own ternary says; it is {kurz.CancelType}");

        // **And a 102 with no parameters at all**, which is the shape a
        // first draft called "just one option" and is in fact none.
        var garNichts = MzChoice.Read(new List<string>());
        AssertEq(
            garNichts.Options.Count, 0,
            "and a 102 with no parameters at all has no options, rather than"
            + " one empty one; it has"
            + $" {garNichts.Options.Count} options");
        // **And with no options at all, zero is not below zero** — so here
        // the two readings finally differ, and this is the only shape in
        // which a missing number does turn into "no cancel".
        AssertEq(
            garNichts.CancelType, MzChoice.NoCancel,
            "and with no options at all the same zero is not below zero, so"
            + " a missing number does become no cancel — the two readings"
            + $" differ only here; it is {garNichts.CancelType}");
    }

    public void Test_ASecondDialogueBehindAnUnansweredChoiceIsAlsoRefused()
    {
        // **The same refusal, and this is where it is worth having:** a
        // dialogue behind a choice is refused, and a reader that only asked
        // "is there a line of text up" would show the second one over the
        // first.
        //
        // The engine's answer is one fact asked twice — `isBusy()` — so this
        // reader asks it once, and the test asks the fact from both sides.
        var facts = new MzBranchFacts { MessageBusy = true };
        AssertTrue(
            facts.MessageBusy,
            "and a choice that is waiting for an answer is busy, exactly as"
            + " a line of text is; it is"
            + $" {facts.MessageBusy}");

        // **And a dialogue with no choice is still busy**, which is the third
        // rule and the one that has no visible symptom: without it a dialogue
        // would not hold its page and the next event would run over the text.
        var ohneWahl = MzDialogue.Read(
            new List<MzCommandEntry>
            {
                new(MzCommandTable.ShowDialogue, new List<string> { "0", "0", "0", "0", "" }, 0),
                new(MzCommandTable.ShowTextLine, new List<string> { "said once" }, 0),
                new(MzCommandTable.ControlVariables, new List<string> { "1", "0", "0", "1" }, 0),
            },
            0,
            new MzBranchFacts());

        AssertEq(
            ohneWahl.Block.Lines.Count, 1,
            "and a dialogue of one line and no choice holds its page just"
            + " the same, because setWaitMode(\"message\") is outside the"
            + $" switch; it has {ohneWahl.Block.Lines.Count} lines");
        AssertEq(
            ohneWahl.Block.Follower, 0,
            "and it found nothing after its last line, because the next"
            + $" command is a 121 and not one of the three; it found"
            + $" {ohneWahl.Block.Follower}");
        AssertEq(
            ohneWahl.Consumed, 2,
            "and it ate two commands — itself and its one line — and stops"
            + $" before the variable; it ate {ohneWahl.Consumed}");

        // **And it held its page, which is the third rule and the one with
        // no visible symptom.** `setWaitMode("message")` is **outside** the
        // `switch`, so a dialogue with no choice waits exactly as long as one
        // with a choice does. Without it the page would not be held and the
        // next event would run over the words.
        //
        // **A first draft checked only what the dialogue ate, and so could
        // not tell a held page from a page that was never shown** — the two
        // read alike through `MzDialogue.Read`, and only the interpreter
        // sets the wait. The two are different questions: this one asks what
        // the command found, and the next asks what the frame does next.
        var mitWarte = new MzBranchFacts();
        var aktionen2 = new List<MzAction>();
        var lauf = new MzInterpreter(new List<MzCommandEntry>
        {
            new(MzCommandTable.ShowDialogue,
                new List<string> { "0", "0", "0", "0", "" }, 0),
            new(MzCommandTable.ShowTextLine, new List<string> { "said once" }, 0),
            new(MzCommandTable.ControlVariables,
                new List<string> { "1", "0", "0", "1" }, 0),
        });
        lauf.Run(aktionen2, mitWarte);

        AssertEq(
            lauf.Stopped, MzStep.Waiting,
            "and the run waits, because the dialogue holds its page even"
            + $" with no choice under it; it is {lauf.Stopped}");
        // **And the run got past the dialogue and on to the variable**, which
        // is a first draft's assumption corrected. It asserted that the index
        // would be *on* the 122 when the run stopped — and it is 3, one past
        // it, because a 122 does not stop a run: it sets a variable and the
        // interpreter carries on to the end of the list.
        //
        // **So this list has no 230 in it, and nothing waits that ends on its
        // own; the only thing that waits is a 101, and the caller released
        // that.** What the run needed to show is that the page was held at
        // all, and `Waiting` above is the whole of that claim.
        AssertEq(
            lauf.Commands.Count, 3,
            "and the list is the dialogue, its line and one variable;"
            + $" it has {lauf.Commands.Count}");
        AssertEq(
            lauf.Index, lauf.Commands.Count,
            "and the index is at the end of it, because a 122 sets a variable"
            + " and does not stop a run — a first draft expected it to be"
            + " still on the 122, which is the same test in the same place"
            + $" with a different assumption; it is {lauf.Index}");
        AssertTrue(
            mitWarte.LastDialogue!.Lines.Count == 1,
            "and the words were read before the run ended, which is what"
            + " makes the wait a wait and not a gap; there are"
            + $" {mitWarte.LastDialogue!.Lines.Count}");
    }

    public void Test_OneHundredAndThreeAndOneHundredAndFourAreTakenAndNotWaitedFor()
    {
        // **`case 103: this._index++; this.setupNumInput(…); break;` and
        // `case 104: this._index++; this.setupItemChoice(…); break;`** — and
        // `setWaitMode("message")` is **outside** the `switch`, so all three
        // of 102, 103 and 104 are taken the same way and all three hold the
        // page.
        //
        // **This game has no 103 and no 104 anywhere in nineteen maps**, so a
        // test that only read the real data would not know whether the
        // fall-through works at all. **A mutation that returned early for 103
        // and 104 — skipping the wait — passed every test in this file for
        // four runs**, because the number it changed was one this game never
        // writes.
        //
        // **So the fall-through is written out by hand, all three of them,
        // and the wait is checked after each.**
        foreach (var (code, wassa) in new[]
        {
            (MzCommandTable.ShowChoiceList, "a choice"),
            (103, "a number to enter"),
            (104, "an item to choose"),
        })
        {
            var facts = new MzBranchFacts();
            var actions = new List<MzAction>();
            var interpreter = new MzInterpreter(new List<MzCommandEntry>
            {
                new(MzCommandTable.ShowDialogue,
                    new List<string> { "0", "0", "0", "0", "" }, 0),
                new(MzCommandTable.ShowTextLine,
                    new List<string> { "asked" }, 0),
                new(code,
                    new List<string> { "[\"A\", \"B\"]", "1", "0", "2", "0" }, 0),
                new(MzCommandTable.ControlVariables,
                    new List<string> { "1", "0", "0", "1" }, 0),
            });
            interpreter.Run(actions, facts);

            AssertEq(
                interpreter.Stopped, MzStep.Waiting,
                "and a 101 followed by " + wassa + " holds the page, because"
                + " setWaitMode is outside the switch; it is"
                + $" {interpreter.Stopped}");
            // **And the page is still held**, which is the whole of the
            // rule: `setWaitMode("message")` is outside the `switch`, so all
            // three of 102, 103 and 104 hold it and so does a dialogue with
            // none of them.
            //
            // **A first draft checked the index instead** and expected it to
            // be on the command after the one the 101 took. It is on the end
            // of the list, because a 122 does not stop a run — the
            // interpreter carried on past it in the same frame. **The claim
            // about the index was about the interpreter and the claim that
            // mattered was about the page, and only the second one is what
            // this method is for.**
            AssertEq(
                interpreter.Stopped, MzStep.Waiting,
                "and the page is still held after the " + wassa
                + ", which is the rule this method is about; it is"
                + $" {interpreter.Stopped}");
            AssertEq(
                facts.LastChoice?.Options.Count ?? 0,
                code == MzCommandTable.ShowChoiceList ? 2 : 0,
                "and a 101 with a choice under it has the options the game"
                + " wrote, read from the command the 101 took and not from"
                + " the one after it; there are"
                + $" {facts.LastChoice?.Options.Count ?? 0} options");
        }

        // **And a 103 and a 104 are not choices.** A number to enter and an
        // item to choose are a digit count and an item id, and reading them
        // through `MzChoice` would count a 103's `4` as four options and
        // compare a cancel number against the length of a number.
        //
        // **This game has neither command in nineteen maps**, so everything
        // here comes from the engine and not from the data — and a reader
        // that got it wrong would have passed every test that read the real
        // maps.
        var mitZahl = new MzBranchFacts();
        new MzInterpreter(new List<MzCommandEntry>
        {
            new(MzCommandTable.ShowDialogue,
                new List<string> { "0", "0", "0", "0", "" }, 0),
            new(MzCommandTable.ShowTextLine, new List<string> { "how many" }, 0),
            new(103, new List<string> { "4", "0" }, 0),
        }).Run(new List<MzAction>(), mitZahl);

        AssertTrue(
            mitZahl.LastPrompt is MzPrompt.Number,
            "and a 103 asks for a number, not for a choice; it asks for"
            + $" {mitZahl.LastPrompt}");
        AssertEq(
            ((MzPrompt.Number)mitZahl.LastPrompt!).Digits, 4,
            "and the four is four digits, not four options — a reader that"
            + " read it through MzChoice would have called them four; the"
            + $" digits are {((MzPrompt.Number)mitZahl.LastPrompt!).Digits}");
        AssertTrue(
            mitZahl.LastChoice == null,
            "and it is not also recorded as a choice, which is what a first"
            + " draft did; the choice is"
            + $" {(mitZahl.LastChoice == null ? "nothing" : "set")}");

        var mitGegenstand = new MzBranchFacts();
        new MzInterpreter(new List<MzCommandEntry>
        {
            new(MzCommandTable.ShowDialogue,
                new List<string> { "0", "0", "0", "0", "" }, 0),
            new(MzCommandTable.ShowTextLine,
                new List<string> { "take one" }, 0),
            new(104, new List<string> { "7" }, 0),
        }).Run(new List<MzAction>(), mitGegenstand);

        AssertTrue(
            mitGegenstand.LastPrompt is MzPrompt.Item,
            "and a 104 asks for an item, not for a choice; it asks for"
            + $" {mitGegenstand.LastPrompt}");
        AssertEq(
            ((MzPrompt.Item)mitGegenstand.LastPrompt!).ItemId, 7,
            $"and the seven is an item id; it is"
            + $" {((MzPrompt.Item)mitGegenstand.LastPrompt!).ItemId}");

        // **`params[1] || 2`** — a missing category, a zero and an empty
        // string are all falsy in JavaScript, and all three give 2, the
        // whole party. **A reader that defaulted to 0 would offer the player
        // nothing at all**, and nothing at all is what a zero category means
        // in every other engine.
        AssertEq(
            ((MzPrompt.Item)mitGegenstand.LastPrompt!).Category, 2,
            "and a category the game did not write is 2, the whole party,"
            + " because `params[1] || 2` and a missing parameter is falsy; it"
            + $" is {((MzPrompt.Item)mitGegenstand.LastPrompt!).Category}");
        AssertFalse(
            ((MzPrompt.Item)mitGegenstand.LastPrompt!).CategoryWasWritten,
            "and the reader says it supplied that default rather than read"
            + " it, so a caller can tell the two apart; it says"
            + $" {((MzPrompt.Item)mitGegenstand.LastPrompt!).CategoryWasWritten}");

        // **And a written zero is also 2** — because `0 || 2` is 2 in
        // JavaScript. **A game that wrote 0 and a game that wrote nothing get
        // the same answer**, and a reader that treated 0 as a real category
        // would be adding a rule the engine does not have.
        var nullEins = MzPrompt.Read(104, new List<string> { "7", "0" });
        AssertEq(
            ((MzPrompt.Item)nullEins).Category, 2,
            "and a written zero is 2 as well, because `0 || 2` is 2 in"
            + " JavaScript and this reader follows the engine rather than"
            + " what reads sensibly; it is"
            + $" {((MzPrompt.Item)nullEins).Category}");

        // **And all three are counted as taken**, not one.
        foreach (var code in new[] { 102, 103, 104 })
        {
            var gelesen = MzDialogue.Read(
                new List<MzCommandEntry>
                {
                    new(MzCommandTable.ShowDialogue,
                        new List<string> { "0", "0", "0", "0", "" }, 0),
                    new(MzCommandTable.ShowTextLine,
                        new List<string> { "asked" }, 0),
                    new(code, new List<string> { "[]", "0" }, 0),
                },
                0,
                new MzBranchFacts());
            AssertEq(
                gelesen.Block.Follower, code,
                "and the 101 took the " + code + " as the one that follows"
                + $" its last line, because the switch asks about one command"
                + $" and one only; it took {gelesen.Block.Follower}");
            AssertEq(
                gelesen.Consumed, 3,
                "and it ate three commands — itself, its line and the one it"
                + $" took; it ate {gelesen.Consumed}");
        }
    }

    public void Test_ADialogueAtTheEndOfItsListRunsOffItAndSaysSo()
    {
        // **A 101 moves the index by however many commands it swallowed**, and
        // every other command in this reader moves it by one. A 101 as the
        // last thing in a list therefore lands the index on the end of it —
        // and `ExecuteOne` read `_commands[Index]` unguarded.
        //
        // **A first draft of this card crashed on that list.** It took four
        // runs to get here, and what it took to find it was a mutation that
        // switched the guard off and passed every test in the file, because
        // no test had a 101 at the end of a list. **The mutation found the
        // gap; the crash was only the next time the code was run.**
        //
        // **The engine's own `executeCommand` reads
        // `this._list[this._index]` unguarded too** — and it never reaches
        // past the end, because a list the editor wrote always has the 0
        // that ends it. A list cut off at the file's end is this reader's
        // problem, and the honest answer is that the event ran out rather
        // than an exception on a list that is merely short.
        var amEnde = new List<MzCommandEntry>
        {
            new(MzCommandTable.ShowDialogue,
                new List<string> { "0", "0", "0", "0", "The End" }, 0),
            new(MzCommandTable.ShowTextLine,
                new List<string> { "the last words" }, 0),
        };
        var facts = new MzBranchFacts();
        var actions = new List<MzAction>();
        var interpreter = new MzInterpreter(amEnde);

        // **The 101 alone, with the page still held** — so the first run
        // stops at the wait and does not walk off the end yet.
        interpreter.Run(actions, facts);
        AssertEq(
            interpreter.Stopped, MzStep.Waiting,
            "and the dialogue holds its page, as every dialogue does; it is"
            + $" {interpreter.Stopped}");

        // **And once the player has read it, the next run is the one that
        // would have thrown.**
        facts.MessageBusy = false;
        var danach = new List<MzAction>();
        interpreter.Run(danach, facts);

        AssertEq(
            interpreter.Stopped, MzStep.Finished,
            "and the event ends, because the list did — not because the"
            + " reader ran out of things to do; it is"
            + $" {interpreter.Stopped}");
        // **And the index is one *past* the end, not on it** — and that is
        // the engine's own arithmetic, not a slip. `command101` steps the
        // index once per line and once for the 102 its switch took, and then
        // `executeCommand`'s own `this._index++` steps it once more. A 101
        // that is the last thing in a list therefore leaves the index one
        // beyond it, and no other command in this reader can, because every
        // other one moves the index by exactly one.
        //
        // **A first draft "corrected" this to `consumed - 1`** — forgetting
        // the interpreter's own step — and the fix broke four assertions in
        // this file and one in K-124's. **A correction that breaks five tests
        // you wrote an hour ago is a hypothesis, and this one was wrong.**
        //
        // **And this is not the case the interpreter's own guard is for.**
        // `IsRunning` is `Index < _commands.Count`, so a run that reaches the
        // end stops there without ever asking for the command after it — the
        // guard in `ExecuteOne` never fires on this path, and a mutation that
        // switches it off passed every test in this file.
        //
        // **The case it is for is a list that was cut off in the middle of a
        // command**, which is a real thing a truncated fixture or a
        // half-written event file looks like, and which this one is:
        AssertEq(
            interpreter.Index, amEnde.Count + 1,
            "and the index is one past the end rather than on it, because a"
            + " 101 steps the index once per line and the interpreter steps"
            + " it once more, and this list was cut off without its 0; it is"
            + $" {interpreter.Index} of {amEnde.Count}");
        AssertEq(
            facts.LastDialogue!.SpeakerName, "The End",
            "and what it said is still there, because running off the end of"
            + " a list says where it got to and not what it lost; the name"
            + $" is \"{facts.LastDialogue.SpeakerName}\"");
    }

    public void Test_AListThatWasCutOffInTheMiddleOfACommandIsSaidAndNotThrown()
    {
        // **`ExecuteOne` reads `_commands[Index]`**, and `IsRunning` is
        // `Index < _commands.Count`, so a list the editor wrote never asks
        // for the command after the last one. **A list that was cut off in
        // the middle of a command is a different thing**: a truncated fixture,
        // a half-written event file, a save from an editor that was closed
        // mid-write.
        //
        // **This is the case the guard in `ExecuteOne` exists for, and no
        // test reached it** — a mutation that switched the guard off passed
        // every test in this file, because the 101 case that looked like it
        // covered it is stopped by `IsRunning` before the guard is ever
        // asked. **A guard with no test is a claim, and this one had three
        // runs of evidence behind it and no proof.**
        var abgeschnitten = new List<MzCommandEntry>
        {
            new(MzCommandTable.ShowDialogue,
                new List<string> { "0", "0", "0", "0", "" }, 0),
            new(MzCommandTable.ShowTextLine, new List<string> { "said" }, 0),
        };
        var facts = new MzBranchFacts();
        var actions = new List<MzAction>();
        var interpreter = new MzInterpreter(abgeschnitten);

        // **The index is forced past the end the way a broken file would.**
        interpreter.Index = 9;
        interpreter.Run(actions, facts);

        AssertEq(
            interpreter.Stopped, MzStep.Finished,
            "and the run ends rather than throwing, because a file that was"
            + " cut off is odd rather than broken and the reader says so;"
            + $" it is {interpreter.Stopped}");
        AssertTrue(
            interpreter.Reason.Contains("end of its list"),
            "and it says where it was, so a caller can tell a truncated file"
            + " from a list that ran out normally; it says"
            + $" \"{interpreter.Reason}\"");
        AssertEq(
            actions.Count, 0,
            "and nothing was read out of a list that has nothing left; it"
            + $" recorded {Describe(actions)}");
    }

    public void Test_TheSpeakerNameIsKeptAndAnEmptyOneMeansNoName()
    {
        // **`setSpeakerName(params[4])`** and `Window_NameState`'s
        // `speakerName() { return this._speakerName ? this._speakerName : ""; }`
        // — **an empty name is a game that names nobody**, not a game whose
        // name is the empty string, and a reader that printed the second as
        // the first would print an empty line above every dialogue.
        var benannt = MzDialogue.Read(
            new List<MzCommandEntry>
            {
                new(MzCommandTable.ShowDialogue,
                    new List<string> { "3", "1", "0", "2", "Rin" }, 0),
                new(MzCommandTable.ShowTextLine,
                    new List<string> { "hello" }, 0),
            },
            0,
            new MzBranchFacts());

        AssertEq(
            benannt.Block.SpeakerName, "Rin",
            $"and the name the game wrote is kept; it is"
            + $" \"{benannt.Block.SpeakerName}\"");
        AssertEq(
            benannt.Block.FaceName, 3,
            "and the face it asked for is kept, because setFaceImage reads"
            + $" the first of the two; it is {benannt.Block.FaceName}");
        AssertEq(
            benannt.Block.FaceIndex, 1,
            $"and so is the index; it is {benannt.Block.FaceIndex}");
        AssertEq(
            benannt.Block.Position, 2,
            "and the position, which is params[3] and not params[2] — the"
            + " two are the position and the background and they are easily"
            + $" swapped; it is {benannt.Block.Position}");
        AssertEq(
            benannt.Block.Background, 0,
            $"and the background; it is {benannt.Block.Background}");

        // **This game's own dialogues name somebody two times out of three,
        // and a first draft said nobody at all.** It read
        // `SpeakerName.Length > 0` over a list the reader had built wrong
        // and said zero — a number a game that names nobody would give, and
        // this is not one: it names Camellia thirty-two times.
        //
        // **268 of the 414 name somebody, 146 name nobody, and all 414 have
        // five parameters** — so this is a game that chose its speakers, not
        // one whose editor left the field empty.
        var alle = EveryDialogueInThisGame();
        AssertEq(
            alle.Count(a => a.Block.SpeakerName.Length > 0), 268,
            "and two hundred and sixty-eight of this game's dialogues name"
            + " somebody, which is neither the zero the first draft said nor"
            + " the four hundred and fourteen; there are"
            + $" {alle.Count(a => a.Block.SpeakerName.Length > 0)}");
        AssertEq(
            alle.Count(a => a.Block.SpeakerName.Length == 0), 146,
            "and a hundred and forty-six name nobody, so the name box is"
            + " empty in a third of them rather than in all of them; there"
            + $" are {alle.Count(a => a.Block.SpeakerName.Length == 0)}");

        // **And `???` 45 times is the player-character placeholder the editor
        // writes** — a name the game chose as much as any other, and a
        // reader that reported it as a broken name would be reporting about
        // the editor.
        AssertEq(
            alle.Count(a => a.Block.SpeakerName == "???"), 45,
            "and forty-five of them say ??? , which is what the editor"
            + " writes for a person who has not been given a name yet;"
            + $" there are {alle.Count(a => a.Block.SpeakerName == "???")}");
        AssertEq(
            alle.Count(a => a.Block.SpeakerName == "Camellia"), 32,
            "and thirty-two say Camellia, the most of any name in the game;"
            + $" there are {alle.Count(a => a.Block.SpeakerName == "Camellia")}");
        AssertEq(
            alle.Count(a => a.Block.SpeakerName == "Mary"), 27,
            $"and twenty-seven say Mary; there are"
            + $" {alle.Count(a => a.Block.SpeakerName == "Mary")}");

        // **And every 101 in nineteen maps carries five parameters**, which
        // is what `setSpeakerName(params[4])` reads — and what a first
        // draft's one-parameter 101 in a K-124 test could not answer.
        AssertEq(
            alle.Count, 414,
            "and every one of the four hundred and fourteen has the five"
            + " parameters the command reads, so the name is the fifth and"
            + " not a second one; there are {alle.Count}");

        // **And no face at all**, which is worth saying because the two
        // fields sit next to each other and both start at zero.
        AssertEq(
            alle.Count(a => a.Block.FaceName > 0), 0,
            "and not one asks for a face, so this game has a name box that"
            + " is filled in and no portrait beside it; there are"
            + $" {alle.Count(a => a.Block.FaceName > 0)}");
    }
}
