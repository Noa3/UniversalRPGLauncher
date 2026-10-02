using System;
using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Which target a multiple assignment accepts, measured.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And four of VX's eight failing scripts fail on this
/// construct</strong>, and all four were measured in
/// <c>TestRgssVxKomma</c>:
///
/// <code>
/// @actors[index1], @actors[index2] = @actors[index2], @actors[index1]
/// self.ox , self.oy = bitmap.width / 2 , bitmap.height / 2
/// c[e], c[f] = c[f], c[e]
/// s.x  , s.y  = x , y
/// </code>
///
/// <strong>And this is the bisection that locates it.</strong> A plain
/// name on the left parses, -- <strong>and a dotted or indexed
/// expression does not</strong>:
///
/// <code>
/// a, b = 1, 2          -> 1 Anweisungen
/// a, @b = 1, 2        -> 1 Anweisungen
/// a, $b = 1, 2        -> 1 Anweisungen
/// a, A.b = 1, 2       -> ',' at offset 1 does not begin an expression
/// a, a[i] = 1, 2      -> ',' at offset 1 does not begin an expression
/// a, a.b = 1, 2       -> ',' at offset 1 does not begin an expression
/// </code>
///
/// <strong>And so the comma is not the fault and the target list
/// is</strong>, -- <strong>and <c>ParseAssignmentTarget</c> reads one
/// name at a time</strong>.
/// </para>
/// </remarks>
public partial class TestRubyMlhsZiele : TestBase
{
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
    /// And the twelve steps, one change each.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the results are printed and asserted as the
    /// fault</strong>, -- <strong>so that a change which teaches the
    /// target list a dotted expression fails this test and has to
    /// say why.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieZwölfSchritte()
    {
        var schritte = new[]
        {
            "a, b = 1, 2",
            "a, b = b, a",
            "a, @b = 1, 2",
            "a, $b = 1, 2",
            "a, A.b = 1, 2",
            "a, a[i] = 1, 2",
            "a, a.b = 1, 2",
            "a, a.b.c = 1, 2",
            "a, b = 1, 2" + NL + "a, b = b, a",
            "a[i], a[j] = 1, 2",
            "a.b, a.c = 1, 2",
            "@actors[i], @actors[j] = @actors[j], @actors[i]",
        };

        foreach (var s in schritte)
        {
            Console.WriteLine("  " + s);
            Console.WriteLine("    -> " + P(s));
        }

        // **Und die Haelfte, die geht.**
        AssertEq("1 Anweisungen", P("a, b = 1, 2"),
            "**and two plain names parse** -- and that is the half that"
                + " works, and `a, *rest = x` was fixed earlier the same"
                + " way");

        // **Und die Haelfte, die nicht geht, und das ist der Fehler.**
        AssertTrue(P("a, a.b = 1, 2").Contains("does not begin"),
            "**and a dotted expression on the left does not parse**"
                + " -- and `RubyParser`, `ParseAssignmentTarget`, reads"
                + " one plain name per comma, and Ruby 1.8.1's own"
                + " `parse.y` has `mlhs_item : mlhs_basic | tSTAR"
                + " mlhs_node | primary_value '[' aref_args ']'`, and a"
                + " `primary_value` includes a member reference");

        AssertTrue(P("a, a[i] = 1, 2").Contains("does not begin"),
            "**and an indexed expression on the left does not parse"
                + " either** -- and that is the same fault, because"
                + " both are `primary_value`");

        AssertTrue(P("@actors[i], @actors[j] = @actors[j], @actors[i]")
            .Contains("does not begin"),
            "**and the line out of `多人数パーティ` does not parse**"
                + " -- and that is one of the four VX scripts, and it"
                + " is at line 576 of that game's own source");
    }
}
