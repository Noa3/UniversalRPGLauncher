using System;
using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The one-line <c>for</c> form, measured over ten variants.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this was a fault test and is now a
/// regression test.</strong> Every form of <c>for</c> carrying a
/// <c>do</c> on the same line as the collection failed: the
/// <c>do</c> was read as a block opener, and the rest of the
/// <c>for</c> was read as that block's body, and the <c>end</c> of
/// the <c>for</c> closed the block. <c>while</c>, <c>each</c> and
/// <c>times</c> with <c>do</c> parsed throughout, which is what made
/// the fault one token wide rather than general.
/// </para>
/// <para>
/// <strong>And the place was <c>RubyParser</c>,
/// <c>ParsePostfix</c>, at <c>IsKeyword("do") &amp;&amp;
/// !AfterACondition</c></strong>, -- <strong>because a
/// <c>for</c> reads a collection and not a condition, and so it was
/// not on the list of the constructs that set that flag.</strong>
/// Ruby 1.8.1 keeps the two apart in the grammar, where
/// <c>expr_value</c> is an <c>arg</c> and an <c>arg</c> carries no
/// block.
/// </para>
/// <para>
/// <strong>And the consequence was named:</strong> Random Dungeon's own
/// <c>Game_Interpreter</c> writes
/// <c>for actor in $game_party.members do yield actor end</c>, and that
/// line is why its script was one of the thirteen that failed.
/// </para>
/// </remarks>
public partial class TestRubyForGemessen : TestBase
{
    private const string NL = "\n";

    private static string Lese(string pQuelle)
    {
        try
        {
            return new RubyParser(new RubyLexer(pQuelle).Tokenize())
                .ParseProgram().Count + " Anweisungen";
        }
        catch (RubyParseException ausnahme)
        {
            return ausnahme.Message;
        }
    }

    /// <summary>
    /// And the ten variants, each differing in one thing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the results are printed</strong>, --
    /// <strong>because which variant works is the measurement</strong>,
    /// -- <strong>and then asserted as the fault</strong>, --
    /// <strong>so that a change which fixes it fails this
    /// test</strong>, -- <strong>and that is what a fault test is
    /// for.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieZehnVarianten()
    {
        var varianten = new[]
        {
            "A: for a in $game_party.members do yield a end",
            "B: for a in $game_party.members do" + NL
                + "    yield a" + NL + "  end",
            "C: for a in $game_party.members" + NL
                + "  yield a" + NL + "end",
            "D: for a in [1,2] do break end",
            "E: for a in [1,2] do" + NL + "    break" + NL + "  end",
            "F: for a in [1,2]" + NL + "  break" + NL + "end",
            "G: [1,2].each do |a| yield a end",
            "H: while true do" + NL + "    break" + NL + "  end",
            "I: 1.times do |a| yield a end",
            "J: for a in [1,2] do break end",
            "K: for a in [1,2] do for b in [3,4] do break end end",
            "L: for a in [1,2] do for b in [3,4] do break end\nend",
        };

        foreach (var v in varianten)
        {
            Console.WriteLine(v.Substring(0, 2) + ": "
                + Lese(v.Substring(3)));
        }

        // **Und jetzt geht die Form, die vorher fehlschlug.**
        AssertEq("1 Anweisungen",
            Lese("for a in [1,2] do break end"),
            "**and `for` with `do` on the same line parses** -- and it"
                + " did not before, and the place was"
                + " `RubyParser`, `ParsePostfix`, where"
                + " `IsKeyword(\"do\") && !AfterACondition` hung a block"
                + " onto the collection and read the rest of the"
                + " `for` as that block's body");

        // **Und die andere Haelfte geht, und das macht den Fehler
        // eindeutig.**
        AssertEq("1 Anweisungen",
            Lese("for a in [1,2]" + NL + "  break" + NL + "end"),
            "**and the same `for` without `do` on the same line"
                + " parses** -- and that is the half that works");

        AssertEq("1 Anweisungen",
            Lese("[1,2].each do |a| yield a end"),
            "**and `each do` on one line parses** -- and so a one-line"
                + " block is not what fails");

        AssertEq("1 Anweisungen",
            Lese("while true do" + NL + "    break" + NL + "  end"),
            "**and `while do` parses** -- and so the block keyword"
                + " itself is not what fails");

        // **Und die Zeile aus Random Dungeons `Game_Interpreter`
        // ist lesbar, und die ist der Grund fuer die ganze
        // Messung.**
        AssertEq("1 Anweisungen",
            Lese("for a in $game_party.members do yield a end"),
            "**and the line out of Random Dungeon's own"
                + " `Game_Interpreter` is readable now** -- and that is"
                + " why its script was one of the thirteen that"
                + " failed, and that is the whole reason this"
                + " measurement existed");

        // **Und ein Block, der wirklich einer ist, geht auch.**
        AssertEq("1 Anweisungen",
            Lese("[1,2].each do |a| yield a end"),
            "**and a `do` that really opens a block still opens"
                + " one** -- and that is the half that could have"
                + " broken while fixing the other half");

    }
}
