using System.Collections.Generic;
using System.Linq;
using Godot;
using UniversalRPG.Tests.Framework;
using UniversalRPG.Web;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// 401, the line of text — and the escape codes in it.
/// </summary>
/// <remarks>
/// <para>
/// This is the biggest thing left in this fixture by a long way: <b>938 lines
/// of text</b> across nineteen maps, every one of them carrying exactly one
/// parameter. Nothing about it is a rendering detail, though, and that is the
/// finding of this card: <b>a line of dialogue is mostly not words</b>.
/// </para>
///
/// <para>
/// <b>Two passes, two rule sets, and a reader that does them in one shows a
/// different line than the game does.</b> Pass one,
/// <c>convertEscapeCharacters</c>, rewrites in three steps: every backslash
/// becomes the escape character; <b>two escape characters put one backslash
/// back</b>; and the variable, actor, party and currency codes are filled in,
/// the variable one in a loop. Pass two, the drawing loop, treats
/// <b>every character below 0x20 as a control character</b> and never puts it
/// in the output.
/// </para>
///
/// <para>
/// <b>And the guess that was wrong by a factor of fifty.</b> A scan of this
/// game's lines found the letters <c>C</c> 53 times, <c>N</c> 38, <c>V</c> 22
/// and <c>P</c> 11, and a first reading took them for escape codes. They are
/// ordinary letters in ordinary words: "SEND <b>C</b>OUT!!", "*<b>N</b>om*",
/// "Valuable <b>V</b>egetables", "nutrients". <b>A code is a backslash first
/// and a letter second</b>, and a reader that scans for letters finds English.
/// The real codes in this game: <c>\|</c> once and <c>\!</c> once.
/// </para>
/// </remarks>
partial class TestMzMessage : TestBase
{
    private const string Root = "res://tests/fixtures/mz_plain/data";

    private static List<string> EveryTextLineInThisGame()
    {
        var found = new List<string>();
        for (var i = 1; i < 20; i++)
        {
            var name = "Map" + i.ToString("000") + ".json";
            var file = MzDataFile.Read(
                name, Godot.FileAccess.GetFileAsBytes(Root.PathJoin(name)));
            foreach (var item in file.Root.Member("events")?.Items ?? new List<MzValue>())
            {
                if (item.Kind != MzKind.Object)
                {
                    continue;
                }
                foreach (var page in item.Member("pages")?.Items ?? new List<MzValue>())
                {
                    foreach (var command in page.Member("list")?.Items ?? new List<MzValue>())
                    {
                        var entry = MzCommandEntry.From(command);
                        if (entry.Code == MzCommandTable.ShowTextLine
                            && entry.Parameters.Count > 0)
                        {
                            found.Add(entry.Parameters[0]);
                        }
                    }
                }
            }
        }
        return found;
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

    public void Test_EveryLineOfThisGamesTextIsOneParameterAndWhatCodesItActuallyUses()
    {
        // **938 lines, one parameter each — and fourteen of them are empty.**
        // An empty line is a blank in the middle of a conversation, not a
        // fault, and a reader that called it one would be wrong about the
        // game's own data.
        var zeilen = EveryTextLineInThisGame();

        AssertEq(
            zeilen.Count, 938,
            "and the nineteen maps carry nine hundred and thirty-eight lines of"
            + $" text, which is this game's own number; they carry {zeilen.Count}");

        AssertEq(
            zeilen.Count(z => z.Length == 0), 14,
            "and fourteen of them are empty, which is a blank line in a"
            + $" conversation and not a fault; there are"
            + $" {zeilen.Count(z => z.Length == 0)}");

        // **The five codes this game actually writes, counted over the
        // files.** A first draft counted the *letters* C, N, V and P and
        // found scores of them, and took them for escape codes — they are
        // ordinary letters in ordinary words. A code is a backslash first and
        // a letter second, and a reader that scans for a bare capital is
        // reading English.
        AssertEq(
            zeilen.Count(z => z.Contains("\\C[")), 19,
            "and nineteen lines ask for a colour, which this game does use"
            + $" and which a first draft said it did not; they are"
            + $" {zeilen.Count(z => z.Contains("\\C["))}");
        AssertEq(
            zeilen.Count(z => z.Contains("\\I[177]")), 19,
            "and nineteen ask for icon 177, which is the same nineteen lines"
            + $" and is a monster picture; they are"
            + $" {zeilen.Count(z => z.Contains("\\I[177]"))}");
        AssertEq(
            zeilen.Count(z => z.Contains("\\|")), 1,
            "and one line waits for the player, three times over; they are"
            + $" {zeilen.Count(z => z.Contains("\\|"))}");
        AssertEq(
            zeilen.Count(z => z.Contains("\\!")), 3,
            "and three ask the player to decide; they are"
            + $" {zeilen.Count(z => z.Contains("\\!"))}");

        // **And the letters, counted the way the first draft counted them, to
        // show how wrong that was.**
        AssertTrue(
            zeilen.Count(z => z.Contains("C")) > 20,
            "and the bare letter C is in scores of lines — SCOUT, OUT —"
            + " which is why a reader that looks for a capital rather than a"
            + $" backslash reads English; it is in {zeilen.Count(z => z.Contains("C"))}");

        // **The one line that waits, and it waits three times.**
        var gewartete = zeilen.First(z => z.Contains("\\|"));
        var gelesen = MzMessage.Read(gewartete);
        AssertEq(
            gelesen.Waits, 3,
            "and the one line that waits does so three times, because a"
            + $" `\\|.|\\|.|\\|.` is three decisions and not one; it waits"
            + $" {gelesen.Waits}");
        AssertTrue(
            !gelesen.Text.Contains("|"),
            "and none of the three appears in the text, because a control"
            + $" character never reaches the output; it is"
            + $" \"{gelesen.Text}\"");
    }

    public void Test_ABackslashBecomesAControlCharacterAndTwoPutOneBack()
    {
        // **Step one, and it is the step a reader skips.** `text.replace(/\\/g,
        // "\x1b")` — every backslash becomes the escape character. A line with
        // `\!` does not carry a backslash and a bang, it carries a control
        // code, and one that is never shown.
        //
        // **Step two, and it is the step that undoes step one for a literal.**
        // `text.replace(/\x1b\x1b/g, "\\")` — two escape characters put one
        // backslash back, because a game writes a real backslash as `\\`.
        var gewoehnlich = MzMessage.Read("Hallo \\!");
        AssertEq(
            gewoehnlich.Text, "Hallo ",
            "and a line with a backslash and a bang shows neither, because the"
            + $" backslash became a control code; it shows \"{gewoehnlich.Text}\"");

        var doppelt = MzMessage.Read("C:\\\\Users");
        AssertEq(
            doppelt.Text, "C:\\Users",
            "and a doubled backslash shows as one, because the game writes a"
            + $" real backslash as two; it shows \"{doppelt.Text}\"");

        // **And a code that is a single mark has nothing to show either.**
        var warten = MzMessage.Read("Hallo \\|");
        AssertEq(
            warten.Text, "Hallo ",
            "and a line that waits shows the words and not the wait, because"
            + $" the wait is a control code; it shows \"{warten.Text}\"");
        AssertEq(
            warten.Waits, 1,
            $"and it waits once, which is what the line asked for; it waits"
            + $" {warten.Waits}");
    }

    public void Test_AVariableIsFilledInEveryTimeItIsNamedAndAnUnsetOneIsNotGuessed()
    {
        // **`while (text.match(/\x1bV\[(\d+)\]/gi))` is a loop, not a test.**
        // A line that names the same variable twice has both filled in, and a
        // reader that replaced one occurrence would leave the second as
        // `\V[3]` in the middle of a sentence.
        var zweimal = MzMessage.Read(
            "\\V[3] von \\V[3] und \\V[5]", id => id == 3 ? "42" : "7");
        AssertEq(
            zweimal.Text, "42 von 42 und 7",
            "and a line that names a variable three times has all three"
            + $" filled in, because the engine loops; it reads"
            + $" \"{zweimal.Text}\"");

        // **A variable this reader has no value for is not invented.** The
        // engine's `$gameVariables.value(n)` answers 0 for one that was never
        // set, and a reader with no game state can say the same — but it must
        // say it, not drop the code.
        var ohne = MzMessage.Read("Du hast \\V[9] Stueck", id => null);
        AssertEq(
            ohne.Text, "Du hast 0 Stueck",
            "and a variable with no value reads as zero, which is what"
            + $" `$gameVariables.value` answers for one that was never set;"
            + $" it reads \"{ohne.Text}\"");

        // **Actor and party names come from a database this reader has not
        // opened**, so with no source they are empty — and the line says it
        // substituted one rather than pretending the name was there.
        // **With a source that has no actors**, the names come out empty —
        // which is what the engine's own `actorName` does for an actor that
        // is not there.
        var namen = MzMessage.Read(
            "\\N[1] sagt: \\P[2].", null, new MzMessage.ThreeNames());
        AssertEq(
            namen.Text, " sagt: .",
            "and an actor and a party member with no database come out empty,"
            + $" not as invented names; it reads \"{namen.Text}\"");
        AssertEq(
            namen.Substituted.Count, 2,
            $"and both are recorded, so the gap is visible; there are"
            + $" {namen.Substituted.Count}");

        // **With a source, the real name.** The engine's
        // `actorName(n)` returns "" for an actor that is not there, and this
        // source is the honest one.
        var mitQuelle = MzMessage.Read(
            "\\N[1]: \\P[1]!",
            null,
            new MzMessage.ThreeNames
            {
                Actors = new Dictionary<int, string> { { 1, "Ria" } },
                Members = new Dictionary<int, string> { { 1, "Ria" } },
                CurrencyUnit = "G",
            });
        AssertEq(
            mitQuelle.Text, "Ria: Ria!",
            "and with a source that knows the names the line reads as the game"
            + $" wrote it; it reads \"{mitQuelle.Text}\"");

        var geld = MzMessage.Read(
            "\\G 500", null, new MzMessage.ThreeNames { CurrencyUnit = "Gold" });
        AssertEq(
            geld.Text, "Gold 500",
            "and `\\G` is the currency unit, which the game takes from"
            + $" TextManager; it reads \"{geld.Text}\"");
    }

    public void Test_WhatDrawsNothingIsNamedAndWhatShowsIsTheWords()
    {
        // **Three classes, and only one of them is text.**
        //
        // The pen codes — `>`, `<`, `{`, `}` — and the parameter codes — `C`,
        // `I`, `PX`, `PY`, `FS` — **change nothing a reader without a renderer
        // can put in its output, and dropping them in silence would make a
        // game look like it had fewer words than it has.** So they are named.
        var farbe = MzMessage.Read("\\C[2]Rot\\C[0]");
        AssertEq(
            farbe.Text, "Rot",
            "and a line that asks for a colour reads as the words alone, because"
            + $" a colour is not a character; it reads \"{farbe.Text}\"");
        AssertEq(
            farbe.Undrawable.Count, 2,
            $"and both colour changes are named, so nothing went missing"
            + $" quietly; there are {farbe.Undrawable.Count}");
        AssertTrue(
            farbe.Undrawable[0].Contains("2"),
            $"and the first one names the colour the game asked for, which is"
            + $" the number in its brackets: {farbe.Undrawable[0]}");

        var icon = MzMessage.Read("Vor \\I[5]");
        AssertEq(
            icon.Text, "Vor ",
            "and an icon is not a character either, so the words are what is"
            + $" left; it reads \"{icon.Text}\"");
        AssertTrue(
            icon.Undrawable.Any(u => u.Contains("5")),
            $"and the icon's number is kept: {string.Join(", ", icon.Undrawable)}");

        // **Pages and early end are the same kind of thing.**
        var seiten = MzMessage.Read("Zehn\\|Zehn\\>Zwanzig");
        AssertEq(
            seiten.Waits, 1,
            $"and a line with a wait and a page change waits once; it waits"
            + $" {seiten.Waits}");
        AssertTrue(
            seiten.Undrawable.Any(u => u.Contains("page")),
            $"and the page is named as a page: {string.Join(", ", seiten.Undrawable)}");

        var frueh = MzMessage.Read("Ende\\$");
        AssertEq(
            frueh.Text, "Ende",
            "and `\\$` ends the line early and shows nothing, so the words are"
            + $" the whole of it; it reads \"{frueh.Text}\"");
        AssertEq(
            frueh.EndsEarly, true,
            $"and says so, because a line that ends early is not a line that"
            + $" simply stopped; it is {frueh.EndsEarly}");
    }

    public void Test_EveryLineOfThisGameReadsToWordsAndNothingIsLostQuietly()
    {
        // **All 938, run.** Every one of them produces text, none of them
        // leaves an unconsumed control character behind, and the ones that ask
        // for something undrawable say so.
        var zeilen = EveryTextLineInThisGame();
        var mitWartet = 0;
        var mitNamen = 0;
        var undrawable = 0;
        var unvollstaendig = 0;

        foreach (var zeile in zeilen)
        {
            var gelesen = MzMessage.Read(
                zeile, id => id.ToString(System.Globalization.CultureInfo.InvariantCulture));

            if (gelesen.Waits > 0)
            {
                mitWartet++;
            }
            if (gelesen.Substituted.Count > 0)
            {
                mitNamen++;
            }
            undrawable += gelesen.Undrawable.Count;

            // **An unconsumed control character means a code the reader
            // looked at and did not handle.** Nothing in this game should
            // produce one, and a code that did would be a line that lost part
            // of itself without saying.
            if (gelesen.Text.Contains(MzMessage.Escape))
            {
                unvollstaendig++;
            }
        }

        AssertEq(
            zeilen.Count, 938,
            "and all of them run, which is the same nine hundred and"
            + $" thirty-eight; there are {zeilen.Count}");

        AssertEq(
            unvollstaendig, 0,
            "and not one of them leaves a control character in the text, so"
            + " every line was read to its end and no code was looked at and"
            + $" ignored; {unvollstaendig} did");

        AssertEq(
            mitWartet, 1,
            "and exactly one of them asks the player to wait, which is the"
            + $" line this card began with; {mitWartet} do");

        // **Fifty-seven, and every one of them is named.** Nineteen lines
        // each ask for a colour twice and an icon once — `\I[177]\C[3]…\C[0]`,
        // a monster picture in a colour and then back to normal — so 19 × 3.
        //
        // **A first draft asserted zero here**, on the strength of a scan that
        // had mistaken the letters for the codes, and the real data said
        // fifty-seven. **A claim about a number is worth exactly as much as
        // the test that notices when the number changes** — and the one that
        // notices was this one, because it read the same files and did not
        // guess.
        AssertEq(
            undrawable, 57,
            "and fifty-seven of them ask for something this reader cannot"
            + $" draw — nineteen lines times a colour twice and an icon once —"
            + $" and every one of them is named rather than dropped;"
            + $" there are {undrawable}");

        // **And the words come out with no backslashes left in them**, which
        // is the whole of what this card is about.
        AssertEq(
            zeilen.All(z => !MzMessage.Read(z).Text.Contains('\\')), true,
            "and every one of them reads to words with no backslash left in"
            + " them, which is what a player would see");
    }

    public void Test_AFourOhOneIsNotADispatchedCommandAndTheReaderSaysSoOnce()
    {
        // **There is no `command401` in the engine.** The 114 `commandNNN`
        // methods do not include it: a 401 is a *position* inside a 101's
        // block, read while the engine walks that block, and the 402 that
        // follows it is read the same way.
        //
        // **So a reader that dispatched it as a command of its own would be
        // running something the engine never runs** — and this test is the
        // record of that, with the count behind it.
        var zeilen = EveryTextLineInThisGame();
        AssertEq(
            zeilen.Count > 900, true,
            "and this game has enough of them for the question to be worth"
            + $" asking, which it is nine hundred times over; it has"
            + $" {zeilen.Count}");

        // **What the reader does instead, and says.**
        //
        // **A first draft ran a 401 through the interpreter on its own and
        // checked `facts.Message`.** That was true while 101 was unread, and
        // it broke the moment K-133 gave 101 to the 101 — because then the
        // 401 is **refused**, which is the honest answer: there is no
        // `command401`, and a line with no dialogue over it is a line
        // nothing would have read.
        //
        // **So a 401 is read the way the engine reads it — under a 101 —
        // and this test says both answers.** One dialogue holding one line is
        // what the engine produces; a reader that dispatched the line would
        // have produced a command of its own, and there is no such thing.
        var facts = new MzBranchFacts();
        var actions = new List<MzAction>();
        var interpreter = new MzInterpreter(new List<MzCommandEntry>
        {
            new(MzCommandTable.ShowDialogue,
                new List<string> { "0", "0", "0", "0", "Rin" }, 0),
            new(MzCommandTable.ShowTextLine, new List<string> { "Hallo" }, 0),
            new(0, new List<string>(), 0),
        });
        interpreter.Run(actions, facts);

        AssertEq(
            facts.LastDialogue!.Lines.Count, 1,
            "and a 401 under a 101 lands in that dialogue's block, because"
            + " the 101 read it and nobody dispatched it; there is"
            + $" {facts.LastDialogue!.Lines.Count}");
        AssertEq(
            facts.LastDialogue.Lines[0].Text, "Hallo",
            "with the words the game wrote; they are"
            + $" \"{facts.LastDialogue.Lines[0].Text}\"");
        AssertEq(
            facts.LastDialogue.SpeakerName, "Rin",
            "and the name the 101 carried, which is why the 101 needs five"
            + $" parameters; it is \"{facts.LastDialogue.SpeakerName}\"");
        AssertEq(
            actions.Count, 1,
            "and it is recorded once, for the dialogue and not for the line,"
            + $" because the line is not a command; it recorded"
            + $" {Describe(actions)}");
        AssertTrue(
            actions[0].What.Contains("Hallo"),
            "and what was recorded names the words, so a caller can see"
            + $" what was read; it says \"{actions[0].What}\"");
    }
}
