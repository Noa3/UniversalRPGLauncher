using System.Text;

using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The text operations, which is how a game cuts a name apart before it
/// draws it.
/// </summary>
/// <remarks>
/// <para>
/// <strong>All of it was refused.</strong> A game's text window had nothing
/// to show: <c>split</c> is how a save-game key becomes two,
/// <c>gsub</c> is how a script strips a marker off a name,
/// <c>ljust</c> is how a menu puts a number in a column,
/// <strong>and <c>3.times</c> is how every status window draws its
/// rows.</strong>
/// </para>
/// <para>
/// <strong>And a pattern from a script is not run.</strong> This reader does
/// not execute a pattern a game brought with it,
/// <strong>because a pattern that runs long is a way to stop a game and a
/// reader without a time limit gives that away</strong> — and it says so out
/// loud, <strong>because a silent nil looks like a text that had no
/// separator in it.</strong>
/// </para>
/// </remarks>
public partial class TestRubyInterpreter
{
    /// <summary>
    /// A text is cut at a separator.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And a save-game key is the case.</strong>
    /// <c>$game_party[1]</c> is <c>"Game_Party:1"</c> and a script splits it
    /// into a name and a number,
    /// <strong>and a reader that could not would make every save slot
    /// unreachable.</strong>
    /// </para>
    /// <para>
    /// <strong>And the empty parts at the end are gone.</strong>
    /// <c>"a,b,,".split(",")</c> is <c>["a", "b"]</c>,
    /// <strong>and a reader that kept them would hang blank rows on a
    /// menu</strong> — and the script never wrote them.
    /// </para>
    /// </remarks>
    public void Test_ATextIsCutAtASeparator()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "[[\"a,b,c\".split(\",\"), \"a,b,,\".split(\",\")].map { |x| x.length }]\n"));

        AssertEq(AsInteger(wert.Items[0].Items[0]), 3,
            "**three parts** — and a save-game key is split this way, so a "
                + "reader that could not would make every save slot "
                + "unreachable");
        AssertEq(AsInteger(wert.Items[0].Items[1]), 2,
            "**and the empty parts at the end are gone** — a reader that "
                + "kept them would hang blank rows on a menu that the script "
                + "never wrote");

        var mit2 = new RubyInterpreter(new RubyNullHost());
        var namen = mit2.RunProgram(Statements(
            "\"Held Alaric\".split(\" \").map { |x| x.to_s }\n"));
        AssertEq(System.Text.Encoding.UTF8.GetString(namen.Items[0].Bytes), "Held",
            "**and the first word is the first name**");
    }

    /// <summary>
    /// `sub` replaces the first and `gsub` all of them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And that is the whole difference between them.</strong> A
    /// script strips one marker off a name,
    /// <strong>and a reader that made both the same would turn a game that
    /// removes one into one that removes all</strong> — and the save key it
    /// then writes is a different key than the one it reads.
    /// </para>
    /// <para>
    /// <strong>And an empty replacement still removes.</strong> Modern
    /// runtimes' <c>Replace</c> with an empty text does nothing,
    /// <strong>and a game that cuts a name apart would have kept the name
    /// whole</strong> — and the name it stores and the name it looks for
    /// would be two different names.
    /// </para>
    /// </remarks>
    public void Test_SubReplacesTheFirstAndGsubAll()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "[\"Hallo Welt\".sub(\"o\", \"0\"), \"Hallo Welt\".gsub(\"o\", \"0\"), "
            + "\"aXbXc\".sub(\"X\", \"\")]\n"));

        AssertEq(System.Text.Encoding.UTF8.GetString(wert.Items[0].Bytes), "Hall0 Welt",
            "**sub changed the first one** — and a reader that made both the "
                + "same would turn a script that removes one marker into one "
                + "that removes all of them, and the save key it writes would "
                + "be a different key than the one it reads");
        AssertEq(System.Text.Encoding.UTF8.GetString(wert.Items[1].Bytes), "Hall0 Welt",
            "**and gsub changed the second as well**");
        AssertEq(System.Text.Encoding.UTF8.GetString(wert.Items[2].Bytes), "abXc",
            "**and an empty replacement removes the first one and only the "
                + "first** — which is what `sub` means, and the claim here is "
                + "not that both are gone: modern runtimes' `Replace` with an "
                + "empty text does nothing at all, so a game that cuts one "
                + "marker out of a name would have kept it whole");
    }

    /// <summary>
    /// A pattern from a script runs, inside a bound, and a text over the
    /// bound is refused by name.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the earlier version of this test said the opposite, and
    /// was right at the time.</strong> It was written before
    /// <c>MusterBauen</c> and <c>LaufZaehlt</c> existed,
    /// **and the first version of <c>gsub</c> refused every pattern instead
    /// of refusing a long one** -- **a refusal with no bound, which is a
    /// refusal for safety's sake and not for any bound at all.**
    /// </para>
    /// <para>
    /// <strong>And the bound is still there, and it is the one
    /// <c>=~</c> uses.</strong> A text over 4096 bytes is refused,
    /// <strong>and the message names the pattern</strong> -- because a
    /// message that does not name it sends the reader looking, and a
    /// silent nil
    /// <strong>would look exactly like a text that had no digits in
    /// it</strong> — and the game would have drawn the name it read, with
    /// the marker still in it, and nothing would say why.
    /// </para>
    /// </remarks>
    public void Test_APatternFromAScriptRunsAndALongOneIsRefusedByName()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements("\"a1b2\".gsub(/[0-9]/, \"X\")\n"));
        AssertEq(System.Text.Encoding.UTF8.GetString(wert.Bytes), "aXbX",
            "**the pattern ran** — and it refused every pattern before, "
                + "which was a refusal for safety's sake and not for any "
                + "bound at all; the bound is here and this text is inside "
                + "it");

        // **Und die Schranke ist echt.** 4097 Bytes sind drueber,
        // **und der Leser laesst das Muster nicht laufen.**
        var lang = new RubyInterpreter(new RubyNullHost());
        var ziffern = new string('a', 4097);
        var abgelehnt = lang.RunProgram(Statements(
            "\"" + ziffern + "\".gsub(/[0-9]/, \"X\")\n"));
        AssertTrue(abgelehnt.Kind == RubyValueKind.Nil,
            "**a text over the bound is refused** — and the answer is nil and "
                + "not the text unchanged, because a reader that returned the "
                + "text would make the game draw the name with the marker "
                + "still in it");

        var genannt = false;
        foreach (var d in lang.Diagnostics)
        {
            if (d.Contains("[0-9]"))
            {
                genannt = true;
            }
        }

        AssertTrue(genannt,
            "**and the message names the pattern** — a message that does "
                + "not name it sends the reader looking; the diagnostics "
                + "were: " + string.Join(" | ", lang.Diagnostics));
    }

    /// <summary>
    /// A text says whether it starts and ends with something.
    /// </summary>
    /// <remarks>
    /// <strong>And this is a tag a game writes on every command.</strong>
    /// <c>"/i".start_with?("/")</c> is how a window's option list finds the
    /// italic one,
    /// <strong>and a reader that could not would give every option the same
    /// look</strong> — and the player would see no italics at all.
    /// </remarks>
    public void Test_ATextSaysWhetherItStartsAndEndsWithSomething()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "[\"abc\".start_with?(\"ab\"), \"abc\".end_with?(\"c\"), "
            + "\"abc\".start_with?(\"bc\")]\n"));

        AssertTrue(wert.Items[0].Boolean,
            "**the text starts with it** — and a window's option list finds "
                + "its italic entry this way, so a reader that could not "
                + "would give every option the same look");
        AssertTrue(wert.Items[1].Boolean,
            "**and ends with it**");
        AssertTrue(!wert.Items[2].Boolean,
            "**and a text that does not start with it says no**");
    }

    /// <summary>
    /// A text is trimmed, chomped and padded.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And `chomp` takes the carriage return with it.</strong> Ruby
    /// 1.8 knows <c>"\r\n"</c> on every platform,
    /// <strong>and a reader that only cut <c>"\n"</c> would leave a
    /// <c>\r</c> standing</strong> — and that carriage return ends up in a
    /// saved setting when the game reads one from a file.
    /// </para>
    /// <para>
    /// <strong>And padding never shortens.</strong> <c>ljust</c> never makes
    /// a text shorter,
    /// <strong>and a reader that cut to the width would take the name out of
    /// a menu's column</strong> — and the column is exactly what the padding
    /// is for.
    /// </para>
    /// </remarks>
    public void Test_ATextIsTrimmedChompedAndPadded()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "[\"  a  \".strip, \"a\\r\\n\".chomp, \"x\".ljust(3, \".\"), "
            + "\"long\".ljust(2, \".\")]\n"));

        AssertEq(System.Text.Encoding.UTF8.GetString(wert.Items[0].Bytes), "a",
            "**the spaces are gone** — a name a game read from a file has them, "
                + "and a menu would draw it indented");
        AssertEq(System.Text.Encoding.UTF8.GetString(wert.Items[1].Bytes), "a",
            "**and the line ending is gone, carriage return and all** — Ruby "
                + "1.8 knows \"\\r\\n\" on every platform, and a reader that "
                + "only cut \"\\n\" would leave a \\r standing in a saved "
                + "setting");
        AssertEq(System.Text.Encoding.UTF8.GetString(wert.Items[2].Bytes), "x..",
            "**and the padding is what the game wrote** — a menu puts a number "
                + "in a column this way");
        AssertEq(System.Text.Encoding.UTF8.GetString(wert.Items[3].Bytes), "long",
            "**and a text that is longer stays longer** — a reader that cut to "
                + "the width would take the name out of the column, and the "
                + "column is what the padding is for");
    }

    /// <summary>
    /// A number runs a block as often as it says.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is how every status window draws its rows.</strong>
    /// <c>4.times { |i| draw_row(i) }</c>,
    /// <strong>and a reader that had no <c>times</c> had no way to draw a
    /// row at all</strong> — the window would exist and be empty.
    /// </para>
    /// <para>
    /// <strong>And the answer is the number, not a list.</strong> Ruby
    /// returns the number,
    /// <strong>and a reader that returned a list would have made a game that
    /// checks the answer see a length where it wanted the count</strong>.
    /// </para>
    /// <para>
    /// <strong>And the block does not see the method's variables.</strong>
    /// That is a deliberate departure from Ruby — <c>parse.y</c> links the
    /// new level with <c>local->prev = lvtbl</c> and does not cut the chain,
    /// and four tests here say the same,
    /// <strong>so a block gathers through the object it was written
    /// in</strong> — which is what a game's window does anyway, because its
    /// row list is an <c>@ivar</c>. <strong>So this test counts through a
    /// field</strong>, and that is the form a game writes.
    /// </para>
    /// </remarks>
    public void Test_ANumberRunsABlockAsOftenAsItSays()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class Fenster\n"
            + "  def initialize\n"
            + "    @zeilen = []\n"
            + "  end\n"
            + "  def baue(n)\n"
            + "    n.times { |i| @zeilen = @zeilen.push(i) }\n"
            + "    @zeilen.length\n"
            + "  end\n"
            + "end\n"
            + "[Fenster.new.baue(3), 3]\n"));

        AssertEq(AsInteger(wert.Items[0]), 3,
            "**the block ran three times and the list grew** — a status "
                + "window builds its rows this way, and a reader that had no "
                + "`times` had no way to draw a row at all; the list is an "
                + "`@ivar` because a block here does not see the method's "
                + "local variables, and that is the form every window uses");
        AssertEq(AsInteger(wert.Items[1]), 3,
            "**and the answer is the number** — Ruby returns the count, and a "
                + "reader that returned a list would have made a game that "
                + "checks the answer see a length where it wanted the count");
    }

    /// <summary>
    /// `tr` translates, and a range counts as one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is written after <c>tr_trans</c> and <c>trnext</c> in
    /// <c>string.c</c> from Ruby 1.8.1.</strong> One table over all 256
    /// bytes, one character read at a time from each side,
    /// <strong>and that is why a range fills every character it covers
    /// instead of being translated three times.</strong>
    /// </para>
    /// <para>
    /// <strong>And the replacement is read per character, and then
    /// reused.</strong> <c>tr("abc", "xy")</c> makes <c>a</c> into
    /// <c>x</c>, <c>b</c> into <c>y</c> and <c>c</c> into <c>y</c> again,
    /// <strong>because the replacement is spent and the last one
    /// stays</strong> — that is the <c>if (r == -1) r = trrepl.now;</c> in
    /// the source, <strong>and without it <c>c</c> would stay
    /// untranslated</strong>.
    /// </para>
    /// <para>
    /// <strong>And an empty replacement removes.</strong> That is the
    /// sentence with which a game strips the characters a name may not
    /// have, <strong>and a reader that left the name alone would let a
    /// player type a name the game cannot store.</strong>
    /// </para>
    /// </remarks>
    public void Test_TrTranslatesAndARangeCountsAsOne()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "[\"Held\".tr(\"a-z\", \"x\"), \"abc\".tr(\"abc\", \"xy\"), "
            + "\"a1b2\".tr(\"0-9\", \"xy\"), \"abc\".tr(\"^b\", \"x\"), "
            + "\"Held\".tr(\"a-z\", \"\")]\n"));

        AssertEq(System.Text.Encoding.UTF8.GetString(wert.Items[0].Bytes), "Hxxx",
            "**the range became one x for every lower-case letter** — a "
                + "reader that did not know ranges would translate each "
                + "letter on its own, which is a different name than the one "
                + "the game stores");
        AssertEq(System.Text.Encoding.UTF8.GetString(wert.Items[1].Bytes), "xyy",
            "**and the spent replacement is reused** — that is "
                + "`if (r == -1) r = trrepl.now;` in `string.c`, and a "
                + "reader that left `c` alone would translate two of three");
        AssertEq(System.Text.Encoding.UTF8.GetString(wert.Items[2].Bytes), "ayby",
            "**and a range in the digits counts from the front again** — "
                + "`0` becomes `x` and `1` becomes `y`, and then it starts "
                + "over");
        AssertEq(System.Text.Encoding.UTF8.GetString(wert.Items[3].Bytes), "xbx",
            "**and a leading caret means everything else** — that is the "
                + "sentence with which a game locks down the keys a name may "
                + "use, and a reader without it would keep every letter the "
                + "game meant to lock");
        AssertEq(System.Text.Encoding.UTF8.GetString(wert.Items[4].Bytes), "H",
            "**and an empty replacement removes the range** — a game that "
                + "strips the characters a name may not have would otherwise "
                + "let a player type a name the game cannot store");
    }
}
