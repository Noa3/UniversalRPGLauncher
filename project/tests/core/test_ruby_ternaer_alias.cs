using System;
using System.Collections.Generic;
using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The ternary colon, a label, an op-assignment chain and an alias
/// name, measured over seventeen forms.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the label and the ternary live in one test on
/// purpose.</strong> They are told apart by one thing only -- <strong>by
/// whether a <c>?</c> was read before the colon</strong> -- <strong>and a
/// test that measures only the ternary cannot say whether the change
/// broke the label.</strong>
/// </para>
/// <para>
/// <strong>And what was measured, before the fix:</strong>
///
/// <code>
/// x = y ? z : w      Operator '?'  Delimiter ':'      1 Anweisungen
/// x = y ? z:w        Operator '?'  Symbol 'z:'       ':' was expected
/// while x do label: end             Symbol 'label:'  1 Anweisungen
/// </code>
///
/// <strong>And the fault was in the lexer and not in the
/// parser</strong>, -- <strong>and the line printed as
/// <c>Symbol 'z:'</c> is the proof</strong>: <strong>the colon was
/// swallowed into the symbol before the parser ever saw it.</strong>
/// </para>
/// </remarks>
public partial class TestRubyTernaerAlias : TestBase
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

    private static string T(string pQuelle)
    {
        var stuecke = new List<string>();
        foreach (var t in new RubyLexer(pQuelle).Tokenize())
        {
            stuecke.Add(t.Kind + " " + t.Text);
        }

        return string.Join(" | ", stuecke);
    }

    /// <summary>
    /// And the seventeen forms.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the op-assignment chain and the alias name are in
    /// here because they are the two that still fail</strong>, -- and
    /// they are printed so that the next step starts from a list and
    /// not from a guess.
    /// </para>
    /// </remarks>
    public void Test_DieFormen()
    {
        Console.WriteLine("--- op-Asgn auf der rechten Seite");
        var rechts = new[]
        {
            "1: a += 1",
            "2: a = b",
            "3: a = b += 1",
            "4: a.b = c += 1",
        };
        foreach (var f in rechts)
        {
            Console.WriteLine(f.Substring(0, 2) + ": " + P(f.Substring(3)));
        }

        Console.WriteLine("--- der Doppelpunkt im Ternaer");
        var ternaer = new[]
        {
            "5: x = y ? z : w",
            "6: x = y ? z:w",
            "7: x = a ? b: c",
        };
        foreach (var f in ternaer)
        {
            Console.WriteLine(f.Substring(0, 2) + ": " + P(f.Substring(3)));
        }

        Console.WriteLine("--- der Klammername im Alias");
        var aliase = new[]
        {
            "8: alias a b",
            "9: alias a b []",
            "10: alias [] a b",
            "11: alias a[]= b",
            "12: alias []= b",
        };
        foreach (var f in aliase)
        {
            Console.WriteLine(f.Substring(0, 2) + ": " + P(f.Substring(3)));
        }

        Console.WriteLine("--- die Tokens,  die den Unterschied zeigen");
        Console.WriteLine("  while x do label: end  ->  "
            + T("while x do label: end"));
        Console.WriteLine("  x = y ? z:w            ->  " + T("x = y ? z:w"));

        // **Und der Ternaer geht.**
        AssertEq("1 Anweisungen", P("x = y ? z:w"),
            "**and a ternary without a space before the colon reads**"
                + " -- and it did not before, and the lexer had"
                + " written `Symbol 'z:'` where the parser needed a"
                + " colon, and that is what this test exists for");

        AssertEq("1 Anweisungen", P("x = y ? z : w"),
            "**and one with a space reads** -- and that was already"
                + " working and must not have broken");

        // **Und das Label geht.**
        AssertEq("1 Anweisungen", P("while x do label: end"),
            "**and a label inside a loop body still reads** -- and"
                + " that is the case the change had to keep, because"
                + " a `?` was never read before it");

        AssertEq("1 Anweisungen", P("loop do label: end"),
            "**and a label after `loop do` reads** -- and for the"
                + " same reason");

        // **Und op-Asgn geht,  wenn es links steht.**
        AssertEq("1 Anweisungen", P("a += 1"),
            "**and an op-assignment on the left reads** -- and so"
                + " does one onto a member or an index, and the"
                + " open case is only when it stands on the right");
    }
}
