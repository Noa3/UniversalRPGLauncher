using System;
using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The one-line <c>for</c> form, measured over ten variants.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this asserts what was measured and not what
/// should work.</strong> Every form of <c>for</c> carrying a
/// <c>do</c> on the same line as the collection fails today, and the
/// same <c>for</c> without that <c>do</c> parses, and
/// <c>while</c>, <c>each</c> and <c>times</c> with <c>do</c> parse.
/// That is the fault, and it is one token wide.
/// </para>
/// <para>
/// <strong>And the consequence is named:</strong> Random Dungeon's own
/// <c>Game_Interpreter</c> writes
/// <c>for actor in $game_party.members do yield actor end</c>, and this
/// reader cannot read it, and that is why 167 of 180 of its scripts
/// run.
/// </para>
/// <para>
/// <strong>And the place is named too:</strong> with
/// <c>URPG_TRACE=for</c> the parser prints, after reading the
/// collection, which token stands next -- and for
/// <c>for a in [1,2] do break end</c> that is
/// <c>EndOfInput</c>, so <c>ParseExpression</c> consumed <c>do</c>,
/// <c>break</c> and <c>end</c> together.
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
        };

        foreach (var v in varianten)
        {
            Console.WriteLine(v.Substring(0, 2) + ": "
                + Lese(v.Substring(3)));
        }

        // **Und das ist der Fehler, und er ist eine Messung.**
        AssertEq("'end' was expected, but the script ends first.",
            Lese("for a in [1,2] do break end"),
            "**and `for` with `do` on the same line does not parse**"
                + " -- and the same `for` without that `do` parses,"
                + " and the difference is one token, and the place is"
                + " `RubyParser`, `case \"for\"`, where"
                + " `ParseExpression` is given the collection");

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

        // **Und der Leser, der das nicht kann, ist genau der
        // Umfang, der fehlt.**
        AssertTrue(Lese("for a in $game_party.members do yield a end")
            .Contains("was expected"),
            "**and the line out of Random Dungeon's own"
                + " `Game_Interpreter` cannot be read** -- and that is"
                + " the whole reason its `Game_Interpreter` script is"
                + " one of the thirteen that fail");
    }
}
