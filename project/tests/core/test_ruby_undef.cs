using System.Collections.Generic;

using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// `undef`, which says "this class does not do that".
/// </summary>
/// <remarks>
/// <para>
/// <strong>It is a keyword and the lexer's own grammar list already had
/// it</strong> — the count in <c>Test_TheKeywordListIsTheOneFromTheGrammar</c>
/// said forty one and the list had forty-one names, and this work nearly
/// added a forty-second by mistake. <strong>A test that counts something is a
/// test that says the size, not the contents</strong>, and the size was right
/// while my edit would have been wrong.
/// </para>
/// <para>
/// <strong>What `undef` is for is inheritance.</strong> A game's base class
/// gains a method later, and a subclass that must not answer to it says
/// <c>undef</c> — <strong>and that is the case a table without a mark cannot
/// express</strong>, because a missing entry says nothing about whether the
/// base's method may come through.
/// </para>
/// </remarks>
public partial class TestRubyInterpreter
{
    /// <summary>
    /// `undef` takes a method out of the class it stands in.
    /// </summary>
    /// <remarks>
    /// <strong>The method is gone and the name comes back.</strong> The value
    /// of the statement is the name, which is Ruby's own shape and the only
    /// thing a reader could put there — <strong>and a reader that answered
    /// the method it removed would have run the thing it was removing.</strong>
    /// </remarks>
    public void Test_UndefTakesAMethodOut()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        mit.RunProgram(Statements(
            "class A\n"
            + "  def m\n"
            + "    1\n"
            + "  end\n"
            + "  undef m\n"
            + "end\n"));

        AssertTrue(mit.FindMethod("A", "m") == null,
            "**the method is out of the table** — a reader that marked it "
                + "instead of removing it would have to answer two questions "
                + "where Ruby answers one");
        AssertTrue(mit.DefinedTypes.Count == 1,
            "**and the class is still there** — `undef` takes a method out and "
                + "not the class with it, and a reader that removed the type "
                + "would have taken the game's class with it");
    }

    /// <summary>
    /// An undefined method does not fall through to the base class.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>This is the whole point of `undef`.</strong> Ruby makes the
    /// name undefined, so a call raises <c>NoMethodError</c> rather than
    /// finding the base's method — <strong>and a reader that only deleted the
    /// class's own entry would have carried on up the chain and found
    /// it</strong>, which is exactly what the game is saying it does not
    /// want.
    /// </para>
    /// <para>
    /// <strong>The subclass never had a method of that name of its own</strong>,
    /// and that is the case a table without a mark cannot express at all.
    /// </para>
    /// </remarks>
    public void Test_AnUndefinedMethodDoesNotFallThroughToTheBase()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        mit.RunProgram(Statements(
            "class Basis\n"
            + "  def m\n"
            + "    1\n"
            + "  end\n"
            + "end\n"
            + "class Erbe < Basis\n"
            + "  undef m\n"
            + "end\n"));

        AssertTrue(mit.FindMethod("Erbe", "m") == null,
            "**the subclass does not answer to m** — and a reader that only "
                + "removed an entry of its own would have found the base's "
                + "method and answered one, which is the opposite of what "
                + "`undef` is for");
        AssertTrue(mit.FindMethod("Basis", "m") != null,
            "**and the base still has it** — `undef` is this class's sentence "
                + "and not the base's, and a reader that took the method out of "
                + "the base would have changed a game it was not asked to "
                + "change");
    }

    /// <summary>
    /// A method defined after `undef` is not undefined.
    /// </summary>
    /// <remarks>
    /// <strong>The mark is about a name and not a moment in time.</strong> In
    /// Ruby, a <c>def</c> after an <c>undef</c> brings the name back — and
    /// <strong>a reader that put the mark on the method object rather than on
    /// the name would have kept the name undefined forever</strong>, so a
    /// class that defines the method afterwards would never answer to it.
    /// </remarks>
    public void Test_AMethodDefinedAfterUndefIsThere()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class A\n"
            + "  undef m\n"
            + "  def m\n"
            + "    3\n"
            + "  end\n"
            + "end\n"
            + "A.new.m\n"));

        AssertEq(AsInteger(wert), 3,
            "**the method defined after the undef answers three** — the mark "
                + "is on the name and not on the method, and a reader that "
                + "marked the object would have left the name undefined for "
                + "good, so a class that brings it back would never answer");
    }

    /// <summary>
    /// The symbol spelling names the same method as the bare word.
    /// </summary>
    /// <remarks>
    /// <strong>`undef m` and `undef :m` are one instruction</strong> — and a
    /// reader that only read the bare word would have let the symbol form
    /// through as an expression. <strong>Both spellings are what a game
    /// writes</strong>, the second one when the name comes from somewhere
    /// else.
    /// </remarks>
    public void Test_TheSymbolSpellingNamesTheSameMethod()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        mit.RunProgram(Statements(
            "class A\n"
            + "  def m\n"
            + "    1\n"
            + "  end\n"
            + "  undef :m\n"
            + "end\n"));

        AssertTrue(mit.FindMethod("A", "m") == null,
            "**the symbol spelling takes the same method out** — the two forms "
                + "are one instruction, and a reader that only read the bare "
                + "word would have read the symbol form as an expression");
    }

    /// <summary>
    /// A class method can be undefined too, and it goes under its own name.
    /// </summary>
    /// <remarks>
    /// <strong>Both spellings go, and only the right one.</strong> A class
    /// method is filed as <c>self.m</c> and an instance method as
    /// <c>m</c>, and <strong>a reader that removed only the plain name would
    /// have left the class method callable</strong> — while one that removed
    /// both unconditionally would have taken an instance method the class
    /// never wrote.
    /// </remarks>
    public void Test_ABothMethodsAreUndefinedByOneUndef()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        mit.RunProgram(Statements(
            "class A\n"
            + "  def m\n"
            + "    1\n"
            + "  end\n"
            + "  def self.m\n"
            + "    2\n"
            + "  end\n"
            + "  undef m\n"
            + "end\n"));

        AssertTrue(mit.FindMethod("A", "m") == null,
            "**the instance method is out**");
        AssertTrue(mit.FindMethod("A", "self.m") == null,
            "**and so is the class method** — a reader that removed only the "
                + "plain name would have left `A.new.m` answering two, and a "
                + "reader that removed both under either name would have made "
                + "`undef` take a method the class never wrote");
    }

    /// <summary>
    /// `undef` outside a class is a diagnostic that says where it stands.
    /// </summary>
    /// <remarks>
    /// <strong>Ruby would put it on <c>Object</c></strong> and this
    /// interpreter files methods under a class, so the message says which
    /// thing would have to provide that. **A reader that invented a root would
    /// have put a game's top-level `undef` in a place no game asks for**, and
    /// a game that has one would have had a method silently vanish from
    /// every class.
    /// </remarks>
    public void Test_UndefOutsideAClassIsADiagnostic()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        mit.RunProgram(Statements("def m\n 1\nend\nundef m\n"));

        var gesagt = false;
        foreach (var d in mit.Diagnostics)
        {
            if (d.Contains("outside a class"))
            {
                gesagt = true;
            }
        }

        AssertTrue(gesagt, "**and it says so** — the diagnostics were: "
            + string.Join(" | ", mit.Diagnostics));
    }

    /// <summary>
    /// A method that was never there is not a diagnostic.
    /// </summary>
    /// <remarks>
    /// <strong>Ruby allows it</strong> — <c>undef</c> on a name the class
    /// does not define is legal and simply marks it. <strong>A reader that
    /// complained would have stopped a game that takes a method out before
    /// the base defines it</strong>, which is a real thing a game's plugin
    /// layer does.
    /// </remarks>
    public void Test_UndefOfAMethodThatWasNeverThereIsFine()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        mit.RunProgram(Statements(
            "class A\n"
            + "  undef gibtsnicht\n"
            + "end\n"));

        var gemeldet = 0;
        foreach (var d in mit.Diagnostics)
        {
            if (d.Contains("gibtsnicht"))
            {
                gemeldet++;
            }
        }

        AssertEq(gemeldet, 0,
            "**and nothing was said** — Ruby allows undefining a name the "
                + "class does not define, and a reader that complained would "
                + "have stopped a game that takes a method out before the "
                + "base has it; the diagnostics were: "
                + string.Join(" | ", mit.Diagnostics));
    }

    /// <summary>
    /// The parenthesised spelling names the same method.
    /// </summary>
    /// <remarks>
    /// <strong>`undef(m)` and `undef m` are one instruction</strong> — and the
    /// bracket is the form a game writes when the name comes from
    /// somewhere else and is spliced into the line. <strong>It is also the
    /// form that distinguishes a reader that reads one name from one that
    /// reads the whole parenthesised expression</strong>, **and that is why
    /// it is here: the mutation that made `undef` read only its first token
    /// survived every other case in this file.**
    /// </remarks>
    public void Test_TheParenthesisedSpellingNamesTheSameMethod()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        mit.RunProgram(Statements(
            "class A\n"
            + "  def m\n"
            + "    1\n"
            + "  end\n"
            + "  undef(m)\n"
            + "end\n"));

        AssertTrue(mit.FindMethod("A", "m") == null,
            "**the bracket form takes the method out** — a reader that read "
                + "only the first token would have read `m` and stopped, which "
                + "happens to work here and would not work for a name the "
                + "game builds itself");
    }

    /// <summary>
    /// `undef a, b` takes both, and that is a list in the grammar.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Verified in <c>v1_8_1</c>'s <c>parse.y</c>:
    /// <c>undef_list: fitem | undef_list ',' fitem</c>.</strong> The first
    /// version of this reader took exactly one name, so <c>undef a, b</c>
    /// removed <c>a</c> and left <c>b</c> to be read as a statement of its
    /// own — <strong>a name sitting in a script that nothing calls.</strong>
    /// </para>
    /// <para>
    /// <strong>This is the case that kills the "reads only the first token"
    /// mutation.</strong> With a single name both readings produce the same
    /// node, so every other test in this file passed with the list support
    /// deleted. **A test that cannot fail for the wrong reason is not
    /// measuring the thing it names**, and the only way out is a case where
    /// the two readings differ — which is a list.
    /// </para>
    /// </remarks>
    public void Test_UndefTakesEveryNameOfAList()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        mit.RunProgram(Statements(
            "class A\n"
            + "  def a\n"
            + "    1\n"
            + "  end\n"
            + "  def b\n"
            + "    2\n"
            + "  end\n"
            + "  undef a, b\n"
            + "end\n"));

        AssertTrue(mit.FindMethod("A", "a") == null,
            "**the first name is out**");
        AssertTrue(mit.FindMethod("A", "b") == null,
            "**and the second is out** — a reader that read one name would "
                + "have removed the first and left the second sitting in the "
                + "script as a statement nothing calls");
    }

    /// <summary>
    /// Every name of a list is marked, and not only the ones that were there.
    /// </summary>
    /// <remarks>
    /// <strong>A game that takes a method out before the base has it is
    /// legal</strong>, and that is the case a table of removals cannot
    /// express — **a name removed from a table nobody had is a name nobody
    /// stops inheriting.** The list form makes it visible: all three names are
    /// marked, and two of them were never defined here.
    /// </remarks>
    public void Test_EveryNameOfAListIsMarkedAndNotOnlyTheDefinedOnes()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        mit.RunProgram(Statements(
            "class Basis\n"
            + "  def geerbt\n"
            + "    1\n"
            + "  end\n"
            + "end\n"
            + "class Erbe < Basis\n"
            + "  undef geerbt, nie_dagewesen, auch_nicht\n"
            + "end\n"));

        AssertTrue(mit.FindMethod("Erbe", "geerbt") == null,
            "**the inherited method is stopped** — that is the one the base "
                + "has, and it is the one `undef` is for");
        AssertTrue(mit.FindMethod("Erbe", "nie_dagewesen") == null,
            "**and the two the class never had are stopped too** — they are "
                + "names in the list, and a reader that marked only the ones "
                + "in the table would have marked one of three");
        AssertTrue(mit.FindMethod("Erbe", "auch_nicht") == null,
            "**all three, not two** — the last one is the one a reader that "
                + "stopped after the first would leave");
    }
}
