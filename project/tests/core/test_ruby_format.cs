using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// `String#%`, `sprintf` and `printf`, which is how a game puts a number
/// into a sentence.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the grammar is the one of `sprintf.c` from Ruby 1.8.1.</strong>
/// Flags, width, precision, conversion,
/// <strong>and not a table I wrote</strong> — `0` is a flag and `5` the width
/// in `%05.2f`, **and a reader that read `0` as the width would make every
/// clock in a game print `3.14 ` where it should print ` 3.14`.**
/// </para>
/// <para>
/// <strong>And `"%05.2f" % wert` is in every status line VX draws.</strong>
/// The reader said *undefined operator '%' for a String and a Float* —
/// <strong>a message about an operator that exists</strong>, because only
/// the arithmetic meaning of `%` was there.
/// </para>
/// </remarks>
public partial class TestRubyInterpreter
{
    /// <summary>
    /// The conversions, and what each of them writes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And `%c` takes a number as a character, and `%s` uses
    /// `to_s`.</strong> A name on a menu is the name,
    /// <strong>and a reader that used `inspect` would draw quotes around
    /// every name</strong> — and a menu item would be <c>"Held"</c> with the
    /// quotes visible.
    /// </para>
    /// <para>
    /// <strong>And `%%` is a percent sign and not a conversion.</strong> It is
    /// in every caption that writes "100%".
    /// </para>
    /// </remarks>
    public void Test_TheConversionsAndWhatEachWrites()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            // **Und jedes `%` hat Klammern.** Ohne sie ist
            // `[\"%d\", 5]` eine Liste aus einem Text und einer Zahl,
            // **und der Leser wertet den Text nicht als Ausdruck** --
            // **gemessen: `["%d" % 5]` gibt eine Liste mit einem Element,
            // und `[("%d" % 5)]` gibt dieselbe mit dem richtigen Inhalt.**
            "["
            + "(\"%d\" % 5) + \"|\""
            + ", (\"%x\" % 255) + \"|\""
            + ", (\"%o\" % 8) + \"|\""
            + ", (\"%b\" % 5) + \"|\""
            + ", (\"%c\" % 65) + \"|\""
            + ", (\"%s\" % nil) + \"|\""
            + ", (\"100%%\" % 5)"
            + "]\n"));
        AssertEq(System.Text.Encoding.UTF8.GetString(wert.Items[0].Bytes), "5|",
            "**`%d` writes the number**");
        AssertEq(System.Text.Encoding.UTF8.GetString(wert.Items[1].Bytes), "ff|",
            "**and `%x` writes it in sixteen** — a game's colour code writes "
                + "every channel this way, and `%x` of 255 is `ff`");
        AssertEq(System.Text.Encoding.UTF8.GetString(wert.Items[2].Bytes), "10|",
            "**and `%o` in eight**");
        AssertEq(System.Text.Encoding.UTF8.GetString(wert.Items[3].Bytes), "101|",
            "**and `%b` in two** — and a game that shows a bit mask writes it "
                + "this way");
        AssertEq(System.Text.Encoding.UTF8.GetString(wert.Items[4].Bytes), "A|",
            "**and `%c` takes a number as a character**");
        AssertEq(System.Text.Encoding.UTF8.GetString(wert.Items[5].Bytes), "nil|",
            "**and `%s` uses `to_s` and not `inspect`** — a reader that used "
                + "`inspect` would draw quotes around every name, and a menu "
                + "item would be `\"Held\"` with the quotes visible");
        AssertEq(System.Text.Encoding.UTF8.GetString(wert.Items[6].Bytes), "100%",
            "**and `%%` is a percent sign and not a conversion** — it is in "
                + "every caption that writes \"100%\", and the value the "
                + "format asks for is not used by it");
    }

    /// <summary>
    /// `sprintf` returns the text, and `printf` returns nothing.
    /// </summary>
    /// <remarks>
    /// <strong>And they are the same function with two different answers.</strong>
    /// `printf` is `sprintf` that writes,
    /// **and Ruby gives it nil back** —
    /// **and a reader that gave it the text would have `printf` in a
    /// chain**, **and a `puts` and a `printf` after each other would be an
    /// output in a variable.**
    /// <strong>And a test that only asked `printf` for its kind could not
    /// see the difference</strong>, **because the refusal also answers
    /// nil** — measured: `printf("%d", 5)` answers `Nil` and
    /// `sprintf("%d", 5)` answers `'5'`, **and a test that stopped at
    /// "nil" would pass on a reader that had no `printf` at all.**
    /// </remarks>
    public void Test_SprintfReturnsTheTextAndPrintfReturnsNothing()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var geformt = mit.RunProgram(Statements("sprintf(\"%d\", 5)\n"));
        AssertEq(System.Text.Encoding.UTF8.GetString(geformt.Bytes), "5",
            "**`sprintf` returns the text**");
        AssertEq(geformt.Kind, RubyValueKind.String,
            "**and the kind is a text and not nil** — the refusal answers "
                + "nil too, and a test that stopped at the kind could not "
                + "see the difference");

        var schreibt = new RubyInterpreter(new RubyNullHost());
        var gedruckt = schreibt.RunProgram(Statements("printf(\"%d\", 5)\n"));
        AssertTrue(gedruckt.Kind == RubyValueKind.Nil,
            "**`printf` returns nil** — and a reader that gave it the text "
                + "would have `printf` in a chain, and a `puts` and a "
                + "`printf` after each other would be an output in a "
                + "variable");
        AssertEq(schreibt.Diagnostics.Count, 0,
            "**and it did it without complaining** — the null host refuses "
                + "everything else, and a `printf` that answered a refusal "
                + "here would be the one name in `Kernel` that worked");
    }

    /// <summary>
    /// Width, and the two fillings.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the gap is zeros for a number and spaces for a text.</strong>
    /// `%05d` and `%5s` use the same width and different filling,
    /// <strong>and a reader that made both the same would turn `%5s` into a
    /// number with zeros</strong> — **and `%5s` is the form a game aligns a
    /// name column with.**
    /// </para>
    /// <para>
    /// <strong>And the zeros go behind the sign.</strong> `-42` with `%06d` is
    /// `-00042`,
    /// <strong>and a reader that put the zeros in front would write
    /// `000-42`</strong> — and that is a number no person can read and no
    /// game should draw.
    /// </para>
    /// </remarks>
    public void Test_WidthAndTheTwoFillings()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "["
            + "(\"%3d\" % 5) + \"|\""
            + ", (\"%-5s|\" % \"hi\")"
            + ", (\"%5s|\" % \"hi\")"
            + ", (\"%010d\" % -42) + \"|\""
            + ", \"%3.2s|\" % \"held\""
            + "]\n"));
        AssertEq(System.Text.Encoding.UTF8.GetString(wert.Items[0].Bytes), "  5|",
            "**the width stands and the gap is spaces**");
        AssertEq(System.Text.Encoding.UTF8.GetString(wert.Items[1].Bytes), "hi   |",
            "**and a left-justified text stands left**");
        AssertEq(System.Text.Encoding.UTF8.GetString(wert.Items[2].Bytes), "   hi|",
            "**and a right-justified text is filled with spaces and not with "
                + "zeros** — a reader that made both the same would turn "
                + "`%5s` into a number with zeros, and `%5s` is the form a "
                + "game aligns a name column with");
        AssertEq(System.Text.Encoding.UTF8.GetString(wert.Items[3].Bytes),
            "-000000042|",
            "**and the zeros go behind the sign** — a reader that put them in "
                + "front would write `000-42`, and that is a number no person "
                + "can read and no game should draw");
        AssertEq(System.Text.Encoding.UTF8.GetString(wert.Items[4].Bytes), " he|",
            "**and a precision cuts a text and does not round it**");
    }

    /// <summary>
    /// An array on the right is the list of values.
    /// </summary>
    /// <remarks>
    /// <strong>And `nil` where a value is missing is not an error, and a
    /// missing value is.</strong> `"%d %d" % [1]` raises
    /// <c>too few argument</c>,
    /// <strong>because a status line with a hole where a number belongs is
    /// worse than one that stops</strong> — and the reference stops.
    /// </remarks>
    public void Test_AnArrayOnTheRightIsTheListOfValues()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "\"%s und %s\" % [\"a\", \"b\"]\n"));
        AssertEq(System.Text.Encoding.UTF8.GetString(wert.Bytes), "a und b",
            "**the array filled both holes** — and a reader that formatted the "
                + "array as one value would have written `[\"a\", \"b\"]` into "
                + "the first hole, and that is the sentence every game builds "
                + "a message with");

        var zuWenig = new RubyInterpreter(new RubyNullHost());
        var ausnahme = FehlerAus<RubyRuntimeException>(() =>
            zuWenig.RunProgram(Statements("\"%d %d\" % [1]\n")));
        AssertEq(ausnahme.Class, "ArgumentError",
            "**and one value too few is an error** — a status line with a "
                + "hole where a number belongs is worse than one that stops, "
                + "and the reference stops");

        var stern = new RubyInterpreter(new RubyNullHost());
        var breite = stern.RunProgram(Statements("\"%*d\" % [5, 42]\n"));
        AssertEq(System.Text.Encoding.UTF8.GetString(breite.Bytes), "   42",
            "**and `*` takes the width from a value, and the value stays a "
                + "value** — a reader that took it for both would have made "
                + "one value out of two");
    }

    /// <summary>
    /// `0` is a flag and the digit after it is the width.
    /// </summary>
    /// <remarks>
    /// <strong>And this is the whole reason the grammar came from the
    /// source.</strong> In <c>sprintf.c</c> the <c>0</c> is in the flag
    /// list, and the digits after it are the width,
    /// <strong>so <c>%05.2f</c> is width five and precision two</strong> —
    /// **and a reader that read <c>0</c> as the width would give
    /// <c>%5.2f</c> and drop the zero**, **and the leading space of a clock
    /// would be gone.**
    /// </remarks>
    public void Test_ZeroIsAFlagAndTheDigitAfterItIsTheWidth()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements("[(\"%05.2f\" % 3.14159)]\n"));
        AssertEq(System.Text.Encoding.UTF8.GetString(wert.Items[0].Bytes), " 3.14",
            "**`%05.2f` is width five and precision two** — the `0` is a flag "
                + "in `sprintf.c`, and a reader that read it as the width "
                + "would give `%5.2f` and drop the zero");

        var spalten = new RubyInterpreter(new RubyNullHost());
        var b = spalten.RunProgram(Statements("\"%08.3f|\" % 2.5\n"));
        AssertEq(System.Text.Encoding.UTF8.GetString(b.Bytes), "   2.500|",
            "**and eight columns with three decimals fill with spaces, not "
                + "zeros** — a float column in a status line is filled with "
                + "spaces, and the reader that used zeros would put the number "
                + "at the wrong end of its column");
    }
}
