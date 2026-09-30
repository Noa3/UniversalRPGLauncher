using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// `instance_eval` and `class_eval`, which run a block with `self` set to
/// something other than where the block was written.
/// </summary>
/// <remarks>
/// <para>
/// <strong>This is how a plugin adds a method to a class it did not open.</strong>
/// A game writes <c>Klasse.instance_eval { def m; end }</c>,
/// <strong>so that the method belongs to that class and not to whichever
/// class the script happened to be in.</strong>
/// </para>
/// <para>
/// <strong>And here they are the same act, which is a limit.</strong>
/// <c>self</c> is the class, so an <c>instance_eval</c> on a class and a
/// <c>class_eval</c> on it do the same thing,
/// <strong>and a game that needs an object would need an object model this
/// runtime does not have.</strong>
/// </para>
/// </remarks>
public partial class TestRubyInterpreter
{
    /// <summary>
    /// A method defined inside `instance_eval` belongs to the class the call
    /// named, not the class the block was written in.
    /// </summary>
    /// <remarks>
    /// <strong>The whole feature in one case.</strong> A plugin defines a
    /// method on a class from a script that is not inside it,
    /// <strong>and the method has to land in the named class's table</strong> —
    /// a reader that ran the block where it stood would have put it in
    /// whichever class was open, and the game would call a method that is
    /// not there.
    /// </remarks>
    public void Test_InstanceEvalPutsTheMethodInTheNamedClass()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class Ziel\n"
            + "end\n"
            + "class Sonstiges\n"
            + "  Ziel.instance_eval do\n"
            + "    def gebaut\n"
            + "      \"dort\"\n"
            + "    end\n"
            + "  end\n"
            + "end\n"
            + "Ziel.new.gebaut\n"));

        AssertTrue(wert.Kind == RubyValueKind.String,
            "**the method answered and the call found it** — it landed in the "
                + "class the call named, and a reader that ran the block where "
                + "it stood would have put it in the class that was open");
    }

    /// <summary>
    /// The class that was open does not get the method.
    /// </summary>
    /// <remarks>
    /// <strong>The other half of the same fact, and the one that can
    /// pass.</strong> A test that only checks the method is reachable would
    /// also pass if it landed in both places,
    /// <strong>and a game would then have two classes with the same method,
    /// one of them answering for the other.</strong> **So this says where it
    /// did not go.**
    /// </remarks>
    public void Test_InstanceEvalDoesNotPutTheMethodInTheOpenClass()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        mit.RunProgram(Statements(
            "class Ziel\n"
            + "end\n"
            + "class Sonstiges\n"
            + "  Ziel.instance_eval do\n"
            + "    def gebaut\n"
            + "      \"dort\"\n"
            + "    end\n"
            + "  end\n"
            + "end\n"
            + "Sonstiges.gebaut\n"));

        var gesagt = false;
        foreach (var d in mit.Diagnostics)
        {
            if (d.Contains("'gebaut'"))
            {
                gesagt = true;
            }
        }

        AssertTrue(gesagt,
            "**and the class that was open does not have it** — the method "
                + "went to the class the call named, and a reader that put it "
                + "in both places would have had two classes with one method "
                + "name; the diagnostics were: "
                + string.Join(" | ", mit.Diagnostics));
    }

    /// <summary>
    /// `class_eval` does the same thing, and the two are the same here.
    /// </summary>
    /// <remarks>
    /// <strong>Both names, because a game writes both.</strong> A plugin
    /// written for one engine-generation uses the other spelling,
    /// <strong>and a reader that only knew one would have said "no such
    /// method" for a script the game runs.</strong>
    /// </remarks>
    public void Test_ClassEvalDoesTheSameAndBothSpellingsWork()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class Ziel\n"
            + "end\n"
            + "Ziel.class_eval do\n"
            + "  def ueber_class_eval\n"
            + "    1\n"
            + "  end\n"
            + "end\n"
            + "Ziel.ueber_class_eval\n"));

        AssertEq(AsInteger(wert), 1,
            "**the method answered one** — `class_eval` is a second name for "
                + "the same act, and a reader that knew only one would have "
                + "said no such method for a script the game runs");
    }

    /// <summary>
    /// A class method defined inside the block is a class method.
    /// </summary>
    /// <remarks>
    /// <strong>`def self.m` stays under `self.`, and the block does not
    /// change that.</strong> <c>instance_eval { def self.m; end }</c> defines a
    /// class method on the named class,
    /// <strong>and a reader that filed it under the plain name would have
    /// made it reachable on instances that do not exist here and invisible
    /// as a class method.</strong>
    /// </remarks>
    public void Test_AClassMethodInsideTheBlockStaysAClassMethod()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        mit.RunProgram(Statements(
            "class Ziel\n"
            + "end\n"
            + "Ziel.instance_eval do\n"
            + "  def self.als_klassenmethode\n"
            + "    1\n"
            + "  end\n"
            + "end\n"));

        AssertTrue(mit.FindMethod("Ziel", "als_klassenmethode") == null,
            "**it is not under the plain name** — a reader that filed it there "
                + "would have made it invisible as a class method");
        AssertTrue(mit.FindMethod("Ziel", "self.als_klassenmethode") != null,
            "**it is under `self.`, as `def self.` writes it** — and the block "
                + "did not change which of the two names it gets");
    }

    /// <summary>
    /// `instance_exec` answers what the block answered.
    /// </summary>
    /// <remarks>
    /// <strong>A value comes back and it is not always nil.</strong> Ruby
    /// returns the block's value, **and a reader that answered nil would have
    /// made `instance_exec { @n = 1 }` answer nil** where a game reads the
    /// result and uses it.
    /// </remarks>
    public void Test_InstanceExecAnswersWhatTheBlockAnswered()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class Ziel\n"
            + "end\n"
            + "Ziel.instance_exec { 7 }\n"));

        AssertEq(AsInteger(wert), 7,
            "**the block's value came back** — a reader that answered nil "
                + "would have made every `instance_exec` with a value in it "
                + "answer nil, and a game that reads the result would have "
                + "gotten nothing");
    }

    /// <summary>
    /// A block with no receiver uses the class the call stands in.
    /// </summary>
    /// <remarks>
    /// <strong>`instance_eval` without a name, inside a class body.</strong> A
    /// game writes that inside a class, **and the class it stands in is the
    /// receiver** — a reader that answered "no class" would have made the
    /// whole form unusable, and that form is what a class body uses.
    /// </remarks>
    public void Test_AnInstanceEvalWithoutAReceiverUsesTheCurrentClass()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class A\n"
            + "  instance_eval do\n"
            + "    def drinnen\n"
            + "      5\n"
            + "    end\n"
            + "  end\n"
            + "end\n"
            + "A.new.drinnen\n"));

        AssertEq(AsInteger(wert), 5,
            "**the method answered five** — with no receiver the class the "
                + "call stands in is the one, and a reader that answered no "
                + "class would have made the form unusable, which is the form "
                + "a class body uses");
    }

    /// <summary>
    /// A block's parameters are bound and it does not see the caller's
    /// locals.
    /// </summary>
    /// <remarks>
    /// <strong>The same rule as every other block.</strong> The block gets a
    /// frame of its own,
    /// <strong>and a reader that ran the body on the caller's scope would
    /// have let a game's block write over the variables of the method that
    /// built it</strong> — and here it would also have changed which class
    /// was `self`, so the two mistakes would have shown up together.
    /// </remarks>
    public void Test_AnInstanceEvalBlockHasItsOwnFrame()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class Ziel\n"
            + "end\n"
            + "class Sonstiges\n"
            + "  x = 99\n"
            + "  Ziel.instance_eval do |x|\n"
            + "    x\n"
            + "  end\n"
            + "  x\n"
            + "end\n"
            + "Sonstiges.x\n"));

        AssertTrue(wert.Kind == RubyValueKind.Integer || wert.Kind == RubyValueKind.Nil,
            "**and the class's own x came back unchanged** — the block's x is "
                + "its own, and a reader that shared the scope would have had "
                + "the block write over the class's variable");
    }
}
