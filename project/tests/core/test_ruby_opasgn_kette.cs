using System;
using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// An op-assignment on the right of an assignment, measured.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is the third construct of the three that kept
/// Random Dungeon's <c>Scene_Battle</c> from running.</strong>
/// <c>@status_window.index = @actor_index += 1</c> at line 225 --
/// <strong>and the fault was that the chain in
/// <c>ParseAssignment</c> knew only <c>=</c> and <c>=&gt;</c>.</strong>
/// </para>
/// <para>
/// <strong>And Ruby 1.8.1 gives <c>=</c> and every <c>tOP_ASGN</c> the
/// loosest binding of all</strong>:
///
/// <code>
/// %right '=' tOP_ASGN
/// arg      : lhs '=' arg_rhs
/// arg_rhs  : arg | tSTAR arg_rhs
/// </code>
///
/// <strong>And so they belong in one loop</strong>, --
/// <strong>and a reader with a loop for one and a different reader for
/// the other would let them drift apart.</strong>
/// </para>
/// </remarks>
public partial class TestRubyOpAsgnKette : TestBase
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
    /// And the forms, one change each.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the deeper chains are in here</strong>, --
    /// <strong>because a fix that only knows <c>+=</c> would pass the
    /// first row and fail the game's own line.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieKetten()
    {
        var formen = new[]
        {
            "1: a = b",
            "2: a += 1",
            "3: a = b += 1",
            "4: a = b = c",
            "5: a = b = c += 1",
            "6: a = b -= 1",
            "7: a = b ||= 1",
            "8: a = b &&= 1",
            "9: a = b <<= 1",
            "10: a = b += 1" + NL + "c = 2",
            "11: a.b = c.d += 1",
            "12: @status_window.index = @actor_index += 1",
            "13: a == b",
            "14: a != b",
            "15: a >= b",
        };

        foreach (var f in formen)
        {
            Console.WriteLine(f.Substring(0, 2) + ": " + P(f.Substring(3)));
        }

        // **Und die Kette geht.**
        AssertEq("1 Anweisungen", P("a = b += 1"),
            "**and an op-assignment on the right reads** -- and it"
                + " did not before, and the chain in `ParseAssignment`"
                + " knew only `=` and `=>`");

        AssertEq("1 Anweisungen", P("a = b = c += 1"),
            "**and a chain of three reads** -- and that is the case"
                + " that needs the op-assignment inside the chain"
                + " and not only after it");

        AssertEq("1 Anweisungen", P("@status_window.index = @actor_index += 1"),
            "**and the line out of `Scene_Battle` reads** -- and that"
                + " is line 225 of that game's own source, and it was"
                + " one of the two scripts that still failed");

        AssertEq("2 Anweisungen", P("a = b += 1" + NL + "c = 2"),
            "**and the chain does not swallow the next statement**"
                + " -- and that is what a chain loop that forgets to"
                + " stop would do");

        // **Und die Vergleiche sind keine Zuweisungen.**
        AssertEq("1 Anweisungen", P("a == b"),
            "**and `==` is a comparison and not an assignment** -- and"
                + " the op-assignment test has to leave it out or this"
                + " fails");
    }
}
