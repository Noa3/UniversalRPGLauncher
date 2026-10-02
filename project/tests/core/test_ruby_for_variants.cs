using System;
using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Which form of VX's line the reader takes, measured one at a time.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And each variant differs from VX's line in exactly one
/// thing</strong>, -- <strong>and a test that changes four things at
/// once cannot say which of them was the problem.</strong>
/// </para>
/// </remarks>
public partial class TestRubyForVariants : TestBase
{
    /// <summary>And a line break, spelled where it is used.</summary>
    private const string NL = "\n";

    private static string P(string pQuelle)
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
    /// And the variants, each with one thing changed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the results are printed and asserted nowhere</strong>,
    /// -- <strong>because which variant works is the
    /// measurement</strong>.
    /// </para>
    /// </remarks>
    public void Test_DieVariantenDerZeile()
    {
        var varianten = new[]
        {
            "A: for a in $game_party.members do yield a end",
            "B: for a in $game_party.members do" + NL
                + "    yield a" + NL + "  end",
            "C: for a in $game_party.members" + NL
                + "  yield a" + NL + "end",
            "D: for a in [1,2] do yield a end",
            "E: for a in [1,2] do" + NL
                + "    yield a" + NL + "  end",
            "F: for a in [1,2]" + NL + "  yield a" + NL + "end",
            "G: [1,2].each do |a| yield a end",
            "H: while true do" + NL + "    break" + NL + "  end",
            "I: 1.times do |a| yield a end",
            "J: for a in [1,2] do break end",
        };

        foreach (var v in varianten)
        {
            System.Console.WriteLine(
                v.Substring(0, 2) + ": " + P(v.Substring(3)));
        }

        // **Und das Gemessene, und es ist ein Fehler in diesem
        // Leser:**
        //
        // - **C, F, G, H, I gehen**:  `for` ohne `do`,
        //   `each`, `while`, `times`.
        // - **A, B, D, E, J gehen nicht**:  **jede Form von `for` mit
        //   `do`**.
        //
        // **Und der Fehler ist also nicht der Einzeiler, sondern der
        // `for`-Zweig mit `do`** -- **und damit ist die Zeile in
        // `RubyParser`, `case "for"`, der Ort.**
        //
        // **Und die Spur an dieser Stelle hat nie ausgeloest**, --
        // **und das heisst:  der Zweig wird nie erreicht, und der
        // Fehler liegt davor.**
        AssertTrue(P("for a in [1,2] do break end").Contains("was expected"),
            "**and `for` with `do` does not parse** -- and the"
                + " measurement is: without `do` it parses, with `do`"
                + " it does not, and `while do`, `each do` and `times"
                + " do` all parse, and that is measured over ten"
                + " variants in this test");
        AssertTrue(!P("for a in [1,2]" + NL + "  break" + NL + "end")
            .Contains("was expected"),
            "**and the same `for` without `do` parses** -- and that is"
                + " the half that works, and the difference between the"
                + " two is one token");
    }
}
