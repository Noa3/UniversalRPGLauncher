using System;
using System.Collections.Generic;
using System.Linq;
using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>A name ends in one `!` or one `?`, and a symbol does too.</summary>
public partial class TestRubyInterpreter
{
    /// <summary>
    /// A method name may end in one `!` or one `?`.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the reference.</strong> Ruby 1.8.1 reads the
    /// name, and then takes the next character when it is <c>!</c> or
    /// <c>?</c>, the name is not empty, and the character after it is not
    /// <c>=</c> — <c>parse.y</c> line 4314. <strong>The tail is not a name
    /// character</strong>, **and that is the reason**: <c>x != 1</c> is the
    /// comparison, and <c>x? = 1</c> is an assignment to a method called
    /// <c>x?</c>.
    /// </para>
    /// <para>
    /// <strong>And every predicate in every script ends in <c>?</c>.</strong>
    /// <c>reich?</c>, <c>empty?</c>, <c>ungleich!</c> — **and a reader
    /// that stops at the <c>?</c> parses <c>send(:reich?)</c> as a colon,
    /// an expression and a closing bracket**, which is measured.
    /// </para>
    /// </remarks>
    public void Test_AMethodNameEndsInABangOrAQuestionMark()
    {
        // **Und `!` am Ende, gerufen ohne Klammern.** `a.ungleich! 5`
        // ist der Satz, den jedes Skript schreibt, das jemanden
        // "ungleich" macht -- **und das ist die Form ohne Klammern, und
        // die Form mit ist die andere.**
        var mitBang = new RubyInterpreter(new RubyNullHost());
        var fuenf = mitBang.RunProgram(Statements(
            "class A\n"
            + "  def ungleich!(v)\n"
            + "    @v = v\n"
            + "  end\n"
            + "end\n"
            + "a = A.new\n"
            + "a.ungleich! 5\n"));

        AssertEq(fuenf.Integer, 5,
            "**`a.ungleich! 5` is five** -- the call without parentheses, "
                + "and the return value is the assignment inside");
        AssertEq(mitBang.Diagnostics.Count, 0,
            "**and nothing was said** -- and the diagnostics were: "
                + string.Join(" | ", mitBang.Diagnostics));

        // **Und `?` am Ende, gerufen mit und ohne Klammern.**
        var mitFrage = new RubyInterpreter(new RubyNullHost());
        var sieben = mitFrage.RunProgram(Statements(
            "def r?(v)\n"
            + "  v\n"
            + "end\n"
            + "class A\n"
            + "end\n"
            + "a = A.new\n"
            + "a.r? 7\n"));
        AssertEq(sieben.Integer, 7,
            "**`a.r? 7` is seven** -- a top-level `def` is an `Object` "
                + "method, and a name may end in `?`");

        var mitKlammern = new RubyInterpreter(new RubyNullHost());
        var auchSieben = mitKlammern.RunProgram(Statements(
            "def r?(v)\n"
            + "  v\n"
            + "end\n"
            + "class A\n"
            + "end\n"
            + "A.new.r?(7)\n"));
        AssertEq(auchSieben.Integer, 7,
            "**and with brackets is the same seven** -- the two spellings "
                + "are one sentence");
    }

    /// <summary>
    /// A symbol may end in one `!` or one `?`.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And a symbol is a name.</strong> <c>parse.y</c> line 4314
    /// speaks of every name, **and a symbol is one** — <c>:reich?</c> and
    /// <c>:"ungleich!"</c>.
    /// </para>
    /// <para>
    /// <strong>And <c>send(:reich?)</c> is the sentence every plugin
    /// writes</strong> when it calls a predicate whose name it holds in a
    /// variable. Measured before this: <c>")" at offset 57 does not begin
    /// an expression</c>, because the lexer read <c>:reich</c> and then
    /// the <c>?</c> as something else.
    /// </para>
    /// </remarks>
    public void Test_ASymbolEndsInABangOrAQuestionMark()
    {
        var m = new RubyInterpreter(new RubyNullHost());
        var wert = m.RunProgram(Statements(
            "def reich?\n"
            + "  true\n"
            + "end\n"
            + "class A\n"
            + "end\n"
            + "A.new.send(:reich?)\n"));

        AssertTrue(wert.Kind == RubyValueKind.Boolean && wert.Boolean,
            "**`send(:reich?)` is true** -- measured before this: *\")\" at "
                + "offset 57 does not begin an expression*, because the "
                + "lexer stopped the name at the `?`");
        AssertEq(m.Diagnostics.Count, 0,
            "**and nothing was said** -- and the diagnostics were: "
                + string.Join(" | ", m.Diagnostics));
    }

    /// <summary>
    /// A `?` or `!` before a quote is a symbol, and not a separator.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the lexer gave <c>Delimiter :</c> and then
    /// <c>String "r?"</c>.</strong> Measured, token by token:
    /// <c>Symbol text=:"r?" value=r? bytes=2</c> is what it gives now.
    /// Before that it gave <c>Delimiter text=:</c> followed by
    /// <c>String text="r?" value=r?</c> — **and the parser said
    /// <c>":" at offset 46 does not begin an expression</c>, and the
    /// message spoke of a colon the reader had recognised and not
    /// treated as a symbol.**
    /// </para>
    /// <para>
    /// <strong>And <c>IsSymbolStart</c> did not know a quote</strong> —
    /// <strong>and <c>ReadSymbol</c> did handle one</strong>, so the branch
    /// below the check was written and could not be reached. **A rule in
    /// one place and its condition in another is a rule that does not
    /// run.**
    /// </para>
    /// </remarks>
    public void Test_ABackslashColonBeforeAQuoteIsOneSymbol()
    {
        var m = new RubyInterpreter(new RubyNullHost());
        var wert = m.RunProgram(Statements(
            "def reich?\n"
            + "  true\n"
            + "end\n"
            + "class A\n"
            + "end\n"
            + "A.new.send(:\"reich?\")\n"));

        AssertTrue(wert.Kind == RubyValueKind.Boolean && wert.Boolean,
            "**`send(:\"reich?\")` is true** -- a name with a space or a "
                + "question mark travels between files as a symbol, and "
                + "measured before this the lexer split it into a colon "
                + "and a string");

        // **Und der Token selbst, denn der Satz besteht aus einem Token.**
        var token = new RubyLexer("A.send(:\"reich?\")").Tokenize();
        var einziges = token.FirstOrDefault(t => t.Kind == RubyTokenKind.Symbol);
        AssertTrue(einziges != null,
            "**and the lexer gave one symbol token** -- measured before "
                + "this: `Delimiter text=:` followed by `String "
                + "text=\"r?\"`, and the parser said a colon does not "
                + "begin an expression");
        AssertEq(System.Text.Encoding.GetEncoding(932)
            .GetString(einziges!.Bytes), "reich?",
            "**and it carries the name without the quotes and the colon** "
                + "-- measured: `value=r?`");
    }

    /// <summary>
    /// A setter keeps its `=` when the name already carries one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this was never broken, and the measurement says so.</strong>
    /// <c>def x=(v)</c>, <c>def x!=(v)</c> and <c>def ==(o)</c> answered five,
    /// five and true on the tree before this work, **and removing every
    /// change of this batch left <c>All 2078 tests passed</c> with only
    /// the two symbol tests failing.**
    /// </para>
    /// <para>
    /// <strong>And a test that was green before it is still worth
    /// having</strong> -- it holds the sentence in place against the next
    /// reader that touches <c>ReadWord</c> or <c>LeseSchreiberName</c>.
    /// **What it is not is a find, and this comment says which of the two
    /// it is, so the next session does not go looking for a bug that
    /// was not there.**
    /// </para>
    /// </remarks>
    public void Test_ASetterKeepsTheEqualsTheLexerGaveIt()
    {
        var m = new RubyInterpreter(new RubyNullHost());
        var wert = m.RunProgram(Statements(
            "class A\n"
            + "  def x=(v)\n"
            + "    @v = v\n"
            + "  end\n"
            + "  def x\n"
            + "    @v\n"
            + "  end\n"
            + "end\n"
            + "a = A.new\n"
            + "a.x = 5\n"
            + "a.x\n"));

        AssertEq(wert.Integer, 5,
            "**`a.x = 5; a.x` is five** -- `LeseSchreiberName` "
                + "returns null and the caller reads the name itself, "
                + "and the lexer gave it `x=` as one token");
        AssertEq(m.Diagnostics.Count, 0,
            "**and nothing was said** -- and the diagnostics were: "
                + string.Join(" | ", m.Diagnostics));

        // **Und `x!` ist dasselbe, und `def ==` bleibt ein Vergleich.**
        var bang = new RubyInterpreter(new RubyNullHost());
        var auchFuenf = bang.RunProgram(Statements(
            "class A\n"
            + "  def x!=(v)\n"
            + "    @v = v\n"
            + "  end\n"
            + "  def x!\n"
            + "    @v\n"
            + "  end\n"
            + "end\n"
            + "a = A.new\n"
            + "a.x! = 5\n"
            + "a.x!\n"));
        AssertEq(auchFuenf.Integer, 5,
            "**and `a.x! = 5; a.x!` is five** -- the tail does not "
                + "cost the setter its equals");

        var gleich = new RubyInterpreter(new RubyNullHost());
        var wahr = gleich.RunProgram(Statements(
            "class A\n"
            + "  def ==(o)\n"
            + "    true\n"
            + "  end\n"
            + "end\n"
            + "A.new == 1\n"));
        AssertTrue(wahr.Kind == RubyValueKind.Boolean && wahr.Boolean,
            "**and `def ==(o)` is still a comparison** -- `==` is "
                + "not a setter name, and a reader that took it "
                + "for one would make every equality a write");
    }

    /// <summary>
    /// The `?` and `!` do not swallow the comparison.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And <c>parse.y</c> line 4314 says
    /// <c>!peek('=')</c>.</strong> A reader that put <c>!</c> into
    /// <c>is_identchar</c> would read <c>x != 1</c> as the call
    /// <c>x!</c> with the argument <c>= 1</c> — **and the game would
    /// compare nothing.**
    /// </para>
    /// <para>
    /// <strong>And a bare <c>!</c> is a logical negation</strong>, **and a
    /// reader that took it for a name tail would answer
    /// <c>undefined operator '!'</c>** — measured.
    /// </para>
    /// </remarks>
    public void Test_TheTailDoesNotSwallowTheComparison()
    {
        var m = new RubyInterpreter(new RubyNullHost());
        var wert = m.RunProgram(Statements("[1 != 2, 1 == 1, 1 < 2]\n"));

        AssertTrue(wert.IsList && wert.Items.Count == 3,
            "**three answers** -- and the list is the one the script wrote");
        AssertTrue(wert.Items[0].Kind == RubyValueKind.Boolean
            && wert.Items[0].Boolean,
            "**and `1 != 2` is true** -- the comparison is a comparison, "
                + "and not the call `1!` with the argument `= 2`");

        var x = new RubyInterpreter(new RubyNullHost());
        var auch = x.RunProgram(Statements("x = 1\nx != 2\n"));
        AssertTrue(auch.Kind == RubyValueKind.Boolean && auch.Boolean,
            "**and `x != 2` is true** -- the same sentence with a name on "
                + "the left, and the name may end in a tail");
        AssertEq(x.Diagnostics.Count, 0,
            "**and nothing was said** -- and the diagnostics were: "
                + string.Join(" | ", x.Diagnostics));
    }
}
