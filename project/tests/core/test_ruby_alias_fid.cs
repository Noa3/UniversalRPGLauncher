using System;
using System.Collections.Generic;
using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The name an alias may have, measured with its tokens.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is the last of Random Dungeon's failing
/// scripts.</strong> <c>マップ軽量化</c> line 103 writes
/// <c>alias indexer_equal_KGC_MapLightening []=</c> --
/// <strong>and the alias names an index assignment.</strong>
/// </para>
/// <para>
/// <strong>And Ruby's own <c>parse.y</c> has a token class for it:</strong>
/// <c>fname : tIDENTIFIER | tCONSTANT | tFID</c>, and
/// <c>tFID</c> is <c>operation2 tIDENTIFIER</c> with
/// <c>operation2 : '[' ']' '='</c>. -- <strong>And so <c>[]=</c> is one
/// token, and it is not an identifier and not a symbol.</strong>
/// </para>
/// </remarks>
public partial class TestRubyAliasFid : TestBase
{
    private static string T(string pQuelle)
    {
        var stuecke = new List<string>();
        foreach (var t in new RubyLexer(pQuelle).Tokenize())
        {
            stuecke.Add(t.Kind + " " + t.Text);
        }

        return string.Join(" | ", stuecke);
    }

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
    /// And the tokens of the names an alias may have.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the tokens are printed first</strong>, --
    /// <strong>because whether this is a parser gap or a lexer gap is
    /// decided by what the lexer emits, and not by what the parser
    /// rejects.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieNamen()
    {
        var namen = new[]
        {
            "alias a b",
            "alias a []",
            "alias a []=",
            "alias a []=(x)",
            "alias a b []=",
            "alias []= b",
            "alias :sym b",
        };

        foreach (var n in namen)
        {
            Console.WriteLine(n);
            Console.WriteLine("  -> " + T(n));
            Console.WriteLine("  -> " + P(n));
        }

        AssertTrue(true,
            "**and the tokens and the results are printed above** --"
                + " and whether `[]=` arrives as one token or as three"
                + " is what decides where the fix belongs, and that is"
                + " the measurement");
    }
}
