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
    /// An inherited method is callable on the subclass.
    /// </summary>
    /// <remarks>
    /// <strong>The chain has to reach the call, and not only the
    /// lookup.</strong> A reader that recorded the superclass in the tree but
    /// not in the table would have found the method in a diagnostic and not in
    /// an answer.
    /// </remarks>
    public void Test_AnInheritedMethodIsCallableOnTheSubclass()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class Basis\n"
            + "  def antwort\n"
            + "    42\n"
            + "  end\n"
            + "end\n"
            + "class Erbe < Basis\n"
            + "end\n"
            + "Erbe.antwort\n"));

        AssertEq(AsInteger(wert), 42,
            "**the subclass called the base's method and got forty-two** — the "
                + "chain reaches the call, and a reader that kept the "
                + "superclass in the tree but not in the table would have "
                + "found it in a diagnostic and not in an answer");
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
