using System;
using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Which target a multiple assignment accepts, measured over twelve
/// steps.
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
/// <strong>And so the comma is not the fault and the lookahead is.</strong>
/// <c>CommaBelongsToTheTarget</c> asked whether the token after the
/// comma began a value and whether the token after <em>that</em> was
/// <c>=</c> -- <strong>and for <c>a, A.b = 1, 2</c> the token after
/// <c>A</c> is <c>.</c>, so the lookahead answered no and the comma
/// was left to the expression reader.</strong>
/// </para>
/// <para>
/// <strong>And the fix reads the whole target first</strong>,
/// -- <strong>because <c>parse.y</c> has
/// <c>mlhs_node : variable | primary_value '[' aref_args ']' |
/// primary_value '.' tIDENTIFIER</c></strong>, --
/// <strong>so a target is a name, then any number of members, then any
/// number of indexes.</strong>
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

        // **Und die Haelfte, die vorher nicht ging, und jetzt schon.**
        AssertEq("1 Anweisungen", P("a, a.b = 1, 2"),
            "**and a dotted expression on the left parses** -- and it"
                + " did not before, and the place was"
                + " `RubyParser`, `CommaBelongsToTheTarget`, whose"
                + " lookahead read one token and asked for `=` behind"
                + " it, -- and Ruby 1.8.1's own `parse.y` has"
                + " `mlhs_node : variable | primary_value '[' aref_args"
                + " ']' | primary_value '.' tIDENTIFIER`");

        AssertEq("1 Anweisungen", P("a, a[i] = 1, 2"),
            "**and an indexed expression on the left parses too**"
                + " -- and that is the same fix, because both are"
                + " `primary_value`");

        AssertEq("1 Anweisungen",
            P("@actors[i], @actors[j] = @actors[j], @actors[i]"),
            "**and the line out of `多人数パーティ` parses** -- and that"
                + " is one of the four VX scripts that failed on a"
                + " comma, and it stands at line 576 of that game's"
                + " own source");

        // **Und eine Kette und zwei Indizes gehen auch.**
        AssertEq("1 Anweisungen", P("a, b.c.d = 1"),
            "**and a chain of members on the left parses** -- and the"
                + " lookahead reads all of it before it asks for the"
                + " `=`");

        AssertEq("1 Anweisungen", P("a, b[i][j] = 1"),
            "**and two indexes in a row parse** -- and that is the"
                + " case where a lookahead that counts one `[` would"
                + " stop in the middle");
    }
}
