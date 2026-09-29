using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// `sub` and `gsub`, with a text, a pattern, and a block.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the block form is how a game rewrites a text.</strong>
/// <c>gsub(/(\w+)/) { |w| w.upcase }</c>,
/// <strong>and without it a game's item renamer, its tag filter and its
/// dialogue formatter all fail at the same line</strong>.
/// </para>
/// <para>
/// <strong>And `sub` replaces the first one and `gsub` all of them.</strong>
/// Measured before: <c>"aXbXc".sub("X", "-")</c> answered <c>'a-b-c'</c> —
/// <strong>and a reader that used one `Replace` for both turns a game that
/// removes one mark from a name into one that removes all</strong> — and the
/// name the game writes into its list would not be the one it looked for.
/// </para>
/// </remarks>
public partial class TestRubyInterpreter
{
    /// <summary>
    /// `sub` replaces the first one, and `gsub` all of them.
    /// </summary>
    /// <remarks>
    /// <strong>And an empty replacement really removes.</strong>
    /// <c>Replace</c> with an empty string does nothing in newer runtimes,
    /// <strong>and a game that strips a mark out of a name would have kept
    /// the name unchanged</strong> — and the name it writes into the list
    /// would be a different one from the one it looked for.
    /// </remarks>
    public void Test_SubReplacesTheFirstOneAndGsubAllOfThem()
    {
        // **Und vier Saetze, und nicht eine Liste** --
        // **weil `["a", "b"]` hier eine Sammlung pruefen wuerde und
        // nicht vier Umbenennungen.**
        var erstens = new RubyInterpreter(new RubyNullHost());
        var eins = erstens.RunProgram(Statements(
            "\"aXbXc\".sub(\"X\", \"-\")\n"));
        var zweitens = new RubyInterpreter(new RubyNullHost());
        var zwei = zweitens.RunProgram(Statements(
            "\"aXbXc\".gsub(\"X\", \"-\")\n"));
        var drittens = new RubyInterpreter(new RubyNullHost());
        var drei = drittens.RunProgram(Statements(
            "\"aXbXc\".gsub(\"X\", \"\")\n"));
        var viertens = new RubyInterpreter(new RubyNullHost());
        var vier = viertens.RunProgram(Statements(
            "\"aXbXc\".sub(\"X\", \"\")\n"));

        AssertEq(System.Text.Encoding.UTF8.GetString(eins.Bytes), "a-bXc",
"**`sub` replaced the first one** — and a reader that used one "
                + "`Replace` for both would turn a game that removes one mark "
                + "from a name into one that removes all, and the name it "
                + "writes into its list would not be the one it looked for");
        AssertEq(System.Text.Encoding.UTF8.GetString(zwei.Bytes), "a-b-c",
            "**and `gsub` replaced all of them**");
        AssertEq(System.Text.Encoding.UTF8.GetString(drei.Bytes), "abc",
            "**and an empty replacement really removes** — `Replace` with an "
                + "empty string does nothing in newer runtimes, and a game "
                + "that strips a mark out of a name would have kept it");
        AssertEq(System.Text.Encoding.UTF8.GetString(vier.Bytes), "abXc",
            "**and `sub` with an empty replacement removes the first one**");
    }

    /// <summary>
    /// A block answers each match, and it gets the whole match.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And it runs once per match.</strong>
    /// <c>"aXbXc".gsub("X") { |x| x * 2 }</c> is <c>'aXXbXXc'</c>,
    /// <strong>and a reader that ran the block once for the whole text would
    /// have doubled only the first mark</strong> — and the name would come
    /// out half rewritten.
    /// </para>
    /// <para>
    /// <strong>And a block with a text is a block and not a list of
    /// words.</strong> <c>gsub("X", "Y")</c> and <c>gsub("X") { "Y" }</c> are
    /// the same sentence,
    /// <strong>and the reader has to tell them apart without guessing at the
    /// form</strong> — **and guessing here means: the block as text, and the
    /// player sees `X` and `Y` and an empty line between them.**
    /// </para>
    /// </remarks>
    public void Test_ABlockAnswersEachMatchAndItGetsTheWholeMatch()
    {
        var m0 = new RubyInterpreter(new RubyNullHost());
        var w0 = m0.RunProgram(Statements(
            "\"aXbXc\".gsub(\"X\") { |x| x * 2 }"+ "\n"));
        var m1 = new RubyInterpreter(new RubyNullHost());
        var w1 = m1.RunProgram(Statements(
            "\"aXbXc\".sub(\"X\") { |x| x + \"!\" }"+ "\n"));
        var m2 = new RubyInterpreter(new RubyNullHost());
        var w2 = m2.RunProgram(Statements(
            "\"aXbXc\".gsub(\"X\") { \"-\" }"+ "\n"));
        AssertEq(System.Text.Encoding.UTF8.GetString(w0.Bytes),
            "aXXbXXc",
            "**the block ran once per match** — and a reader that ran it once for the whole text would have doubled only the first mark, and the name would come out half rewritten");

        AssertEq(System.Text.Encoding.UTF8.GetString(w1.Bytes),
            "aX!bXc",
            "**and sub ran it once**");

        AssertEq(System.Text.Encoding.UTF8.GetString(w2.Bytes),
            "a-b-c",
            "**and a block that answers a text puts the text there** — the block is a value and not a list of words, and a reader that guessed would print X and Y and an empty line");

    }

    /// <summary>
    /// `$1` is the group of the match being replaced, and not of the last
    /// one.
    /// </summary>
    /// <remarks>
    /// <strong>And that is the whole reason the block exists.</strong>
    /// <c>gsub(/(\w+) (\w+)/) { "#{$2} #{$1}" }</c> swaps the halves of every
    /// name in a list,
    /// <strong>and a reader that ran the block after collecting all the
    /// matches would have every replacement see the last match's
    /// groups</strong> — and every name in the list would come out the same.
    /// </remarks>
    public void Test_TheGroupIsTheOneOfTheMatchBeingReplaced()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        // **Und der Block liest die Gruppen mit `$1`, und nicht mit einer
        // Einsetzung im Text** -- `"#{$1}"` ist eine andere Ruby-Sache,
        // **und der Leser hat sie nicht.**
        var wert = mit.RunProgram(Statements(
            "\"anna bob\".gsub(/(\\w+) (\\w+)/) { $2 + \" \" + $1 }\n"));
        AssertEq(System.Text.Encoding.UTF8.GetString(wert.Bytes), "bob anna",
            "**each replacement saw its own groups** — and a reader that ran "
                + "the block after collecting all the matches would have "
                + "every replacement see the last one, and every name in the "
                + "list would come out the same");

        // **Und `\\1` im Ersatztext ist derselbe Satz ohne einen Block.**
        var mit2 = new RubyInterpreter(new RubyNullHost());
        // **Und die Gruppe im Ersatzerzeugnis braucht zwei Backslashes
        // in Ruby.** `"\1"` ist der Oktalwert 1 -- **gemessen: genau ein
        // Byte `01`** -- **und der Gruppenrueckverweis steht in
        // `"\\1"`.**
        var ohne = mit2.RunProgram(Statements(
            "\"anna bob\".gsub(/(\\w+) (\\w+)/, \"\\\\2 \\\\1\")\n"));
        AssertEq(System.Text.Encoding.UTF8.GetString(ohne.Bytes), "bob anna",
            "**and `\\1` in the replacement is the same sentence without a "
                + "block** — a reader that only knew `\\1` would leave `\\0` "
                + "standing in every replacement, and a game that swaps the "
                + "halves of a name with `\\0` would write two characters "
                + "into it");
    }

    /// <summary>
    /// `sub` with a pattern stops after the first one, like `sub` with a
    /// text.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the two are separate sentences, and both need
    /// holding.</strong> `sub` with a text walks the text with
    /// <c>IndexOf</c>, `sub` with a pattern walks the matches,
    /// **and a reader that fixed one and not the other would have a
    /// `gsub` in a game's name field</strong> — and the name it looked for
    /// would come back with every later mark already replaced.
    /// </para>
    /// <para>
    /// <strong>And this was a surviving mutation, not a guess.</strong>
    /// Two rules survived because no test had this sentence at all,
    /// **and the sentence is the commonest one in a game's name
    /// tidy** — `name.sub!(/(\d+)/, '')`.
    /// </para>
    /// </remarks>
    public void Test_SubWithAPatternStopsAfterTheFirstOne()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "\"a1b2c3\".sub(/[0-9]/, \"X\")\n"));
        AssertEq(System.Text.Encoding.UTF8.GetString(wert.Bytes), "aXb2c3",
            "**`sub` with a pattern replaced the first one** — and a reader "
                + "that fixed this and not the text form would have a `gsub` "
                + "in a game's name field, and the name it looked for would "
                + "come back with every later mark already replaced");

        // **Und ohne Muster derselbe Satz, damit beide Wege getestet sind.**
        var mit2 = new RubyInterpreter(new RubyNullHost());
        var ohne = mit2.RunProgram(Statements(
            "\"a1b2c3\".sub(\"1\", \"X\")\n"));
        AssertEq(System.Text.Encoding.UTF8.GetString(ohne.Bytes), "aXb2c3",
            "**and `sub` with a text did the same** — the two are separate "
                + "sentences, and each needs its own holding");
    }

    /// <summary>
    /// A text repeated by a number, because that is how a game draws a
    /// divider.
    /// </summary>
    /// <remarks>
    /// <strong>And it is the sentence with which a window draws a
    /// separator.</strong> <c>"-" * 30</c>,
    /// <strong>and a reader that only took numbers would say *undefined
    /// operator '*' for a String and a Integer*** — **and the message would
    /// be about an operator that exists.**
    /// </remarks>
    public void Test_ATextTimesANumberRepeatsIt()
    {
        var strich = new RubyInterpreter(new RubyNullHost());
        var eins = strich.RunProgram(Statements("\"-\" * 5\n"));
        var paar = new RubyInterpreter(new RubyNullHost());
        var zwei = paar.RunProgram(Statements("\"ab\" * 2\n"));

        AssertEq(System.Text.Encoding.UTF8.GetString(eins.Bytes), "-----",
            "**the text repeated** — and `\"-\" * 30` is the divider "
                + "between two windows, and a reader that only took numbers "
                + "would say *undefined operator '*' for a String and a "
                + "Integer*, and the message would be about an operator "
                + "that exists");
        AssertEq(System.Text.Encoding.UTF8.GetString(zwei.Bytes), "abab",
            "**and it is the whole text and not the first character**");
    }
}
