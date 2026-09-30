using System.Collections.Generic;

using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The superclass chain, which the parser used to throw away.
/// </summary>
public partial class TestRubyInterpreter
{
    /// <summary>
    /// A method is found through the superclass, and the chain is walked at
    /// the lookup.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>The subclass is written first and the base second</strong> —
    /// that is the order a script with several files runs in, and it is why
    /// the chain is walked when a method is asked for and not copied when a
    /// class is defined. <strong>A reader that copied the methods at
    /// definition would have had an incomplete subclass</strong> that never
    /// learned about the base.
    /// </para>
    /// <para>
    /// <strong>This test used to say the opposite.</strong> The parser read
    /// <c>class X</c> and <em>discarded</em> the <c>&lt; Basis</c>, so the
    /// chain existed in the evaluator and was never fed — <strong>and a test
    /// that says "inheritance does not work" reads as a measurement of the
    /// evaluator when it is a measurement of the parser.</strong> The parser
    /// keeps the name now, and this asserts that the chain works.
    /// </para>
    /// </remarks>
    public void Test_AMethodIsFoundThroughTheSuperclassChain()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        mit.RunProgram(Statements(
            "class Erbe < Basis\n"
            + "  def eigen\n"
            + "    1\n"
            + "  end\n"
            + "end\n"
            + "class Basis\n"
            + "  def geerbt\n"
            + "    2\n"
            + "  end\n"
            + "end\n"));

        AssertTrue(mit.FindMethod("Erbe", "eigen") != null,
            "**the subclass has its own method**");
        AssertTrue(mit.FindMethod("Erbe", "geerbt") != null,
            "**and finds the base's method through the chain** — the base was "
                + "written second, and a reader that copied the methods at "
                + "definition would have had an incomplete subclass");
        AssertTrue(mit.FindMethod("Basis", "geerbt") != null,
            "**and the base still has its own** — a reader that moved the "
                + "method up the chain instead of finding it would have taken "
                + "it away from the base");
    }

    /// <summary>
    /// An inherited method is callable on an instance of the subclass, and
    /// the inherited class method on the class name.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>The chain has to reach the call, and not only the
    /// lookup.</strong> A reader that recorded the superclass in the tree but
    /// not in the table would have found the method in a diagnostic and not in
    /// an answer.
    /// </para>
    /// <para>
    /// <strong>And <em>an instance</em>, and not the class name.</strong>
    /// This test said <c>Erbe.antwort</c> and expected 42, **and that is
    /// a <c>NoMethodError</c> in Ruby 1.8.1** —
    /// <c>rb_singleton_class(Erbe)</c> is the metaclass,
    /// <c>rb_make_metaclass</c> gives it
    /// <c>RBASIC(super)->klass</c> as its super
    /// (**class.c line 158**), **and the chain of a call on a class name is
    /// <c>Singleton(Erbe) -&gt; Singleton(Basis) -&gt; Class -&gt; Module
    /// -&gt; Object</c>** — **and <c>Basis.m_tbl</c> is in none of those.**
    /// </para>
    /// <para>
    /// **And a test that holds a wrong number is worse than no test**,
    /// because it is checked on every run and reads as a proof.
    /// </para>
    /// </remarks>
    public void Test_AnInheritedMethodIsCallableOnAnInstance()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class Basis\n"
            + "  def antwort\n"
            + "    42\n"
            + "  end\n"
            + "  def self.selbst_antwort\n"
            + "    43\n"
            + "  end\n"
            + "end\n"
            + "class Erbe < Basis\n"
            + "end\n"
            + "[Erbe.new.antwort, Erbe.selbst_antwort]\n"));

        AssertEq(wert.Items[0].Integer, 42,
            "**an instance of the subclass calls the base's method** — the "
                + "chain reaches the call, and a reader that kept the "
                + "superclass in the tree but not in the table would have "
                + "found it in a diagnostic and not in an answer");
        AssertEq(wert.Items[1].Integer, 43,
            "**and the class name calls the base's class method** — that is "
                + "the call on a class name, and it walks the singleton "
                + "chain, and the instance method beside it is invisible to "
                + "it, **and a reader that answered both from one table "
                + "would have given 42 here and called it right**");
    }

    /// <summary>
    /// A module has no superclass, and a reader must not give it one.
    /// </summary>
    /// <remarks>
    /// <strong>The parser refuses to put one in the node</strong>, so a
    /// <c>module M &lt; N</c> is written as a module with no superclass
    /// rather than as an error — <strong>and the evaluator's chain walk then
    /// has nothing to walk, which is the safe direction</strong>: a module
    /// that could inherit would take methods it never asked for.
    /// </remarks>
    public void Test_AModuleHasNoSuperclass()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        mit.RunProgram(Statements(
            "class Basis\n"
            + "  def geerbt\n"
            + "    2\n"
            + "  end\n"
            + "end\n"
            + "module M < Basis\n"
            + "  def eigen\n"
            + "    1\n"
            + "  end\n"
            + "end\n"));

        AssertTrue(mit.FindMethod("M", "geerbt") == null,
            "**the module does not inherit** — only a class has a superclass, "
                + "and a module that could would take methods the script never "
                + "asked for");
        AssertTrue(mit.FindMethod("M", "eigen") != null,
            "**and its own method is there** — the module is a type like any "
                + "other and holds what the script wrote in it");
    }
}
