using System;
using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Whether the alias name is a lexer fault or a parser fault.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the lexer emits three tokens for <c>[]=</c></strong>:
/// <c>Delimiter [ | Delimiter ] | Operator =</c> -- <strong>and
/// Ruby's own lexer emits one, <c>tFID</c>.</strong>
/// </para>
/// <para>
/// <strong>And the question this test answers is which of the two has
/// to change</strong>, -- <strong>because a reader that joins the three
/// tokens in the parser needs no new token kind, and one that makes a
/// new token kind has to change the lexer's caller as well.</strong>
/// </para>
/// <para>
/// <strong>And <c>def []=(x)</c> and <c>attr_accessor</c> already
/// work in this repository</strong>, -- <strong>and that is the
/// evidence that the three-token form is understood
/// somewhere.</strong>
/// </para>
/// </remarks>
public partial class TestRubyAliasZusammen : TestBase
{
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
    /// And the forms that already work beside the one that does not.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the working ones are printed first</strong>, --
    /// <strong>because if <c>def []=</c> works then the three tokens
    /// are already joined somewhere, and the alias is the only place
    /// where they are not.</strong>
    /// </para>
    /// </remarks>
    public void Test_WoDerNameEntsteht()
    {
        var formen = new[]
        {
            "1: def [](x); end",
            "2: def []=(x); end",
            "3: def self.name; end",
            "4: alias a b",
            "5: alias a []",
            "6: alias a []=",
            "7: alias :sym b",
            "8: undef []=",
            "9: undef foo",
        };

        foreach (var f in formen)
        {
            Console.WriteLine(f.Substring(0, 2) + ": " + P(f.Substring(3)));
        }

        AssertTrue(true,
            "**and the nine forms are printed above** -- and whether"
                + " `def []=` works while `alias a []=` does not is"
                + " what tells the lexer apart from the parser, and"
                + " that is the measurement");
    }
}
