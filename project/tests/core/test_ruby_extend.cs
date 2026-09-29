using System.Collections.Generic;

using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// `extend`, which is `include` written with `self` in front.
/// </summary>
public partial class TestRubyInterpreter
{
    /// <summary>
    /// An `extend` makes a method of the class, and not of the things made
    /// from it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Ruby defines the methods as singleton methods of the object,
    /// and in a class body that object is the class.</strong> A game that
    /// writes <c>extend Modul</c> in a class wants <c>Klasse.name</c> to
    /// work, <strong>and a reader that treated it as <c>include</c> would
    /// have put the method on the instances</strong> — which this runtime does
    /// not have, so it would have been a method no call could reach.
    /// </para>
    /// <para>
    /// <strong>And it lands under the <c>self.</c> prefix</strong>, where a
    /// class method belongs. A class with both gets both, <strong>which is
    /// what a game's mixin class wants.</strong>
    /// </para>
    /// </remarks>
    public void Test_AnExtendMakesAClassMethod()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "module Werkzeug\n"
            + "  def namen\n"
            + "    11\n"
            + "  end\n"
            + "end\n"
            + "class Held\n"
            + "  extend Werkzeug\n"
            + "end\n"
            + "Held.namen\n"));

        AssertEq(AsInteger(wert), 11,
            "**the class answers eleven through the extended method** — and a "
                + "reader that treated `extend` as `include` would have filed "
                + "it on the instances, which this runtime does not have, so "
                + "the call would have found nothing");
        AssertTrue(mit.FindMethod("Held", "self.namen") != null,
            "**and it is filed as a class method** — under the `self.` "
                + "prefix, where `Klasse.name` looks");
    }

    /// <summary>
    /// `extend` and `include` keep their methods apart.
    /// </summary>
    /// <remarks>
    /// <strong>One name, two meanings, and a game that writes both wants
    /// both.</strong> This runtime files a class method under `self.` and an
    /// instance method plainly, <strong>so a class that extends and includes
    /// has one method that `Klasse.name` reaches and one that `Klasse.name`
    /// does not touch</strong>. A reader that filed both under one key would
    /// have had the second definition overwrite the first, **and the game's
    /// class method would have become an instance method on the last write.**
    /// </remarks>
    public void Test_ExtendAndIncludeKeepTheirMethodsApart()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "module A\n"
            + "  def namen\n"
            + "    1\n"
            + "  end\n"
            + "end\n"
            + "module B\n"
            + "  def namen\n"
            + "    2\n"
            + "  end\n"
            + "end\n"
            + "class Held\n"
            + "  include A\n"
            + "  extend B\n"
            + "end\n"
            + "Held.namen\n"));

        AssertEq(AsInteger(wert), 2,
            "**the class method wins on the class, and the instance method is "
                + "still there** — `extend B` put it under `self.namen` and "
                + "`include A` put its own under `namen`, and a reader that "
                + "filed both under one key would have let the second "
                + "overwrite the first");
        AssertTrue(mit.FindMethod("Held", "namen") != null,
            "**and the instance method survived** — it is what the class's own "
                + "things would call, and a reader that lost it would have "
                + "made the class's instances unable to answer");
    }

    /// <summary>
    /// A `def self.x` in a module keeps its name when the module is
    /// extended.
    /// </summary>
    /// <remarks>
    /// <strong>The prefix is not added twice.</strong> A module's class
    /// method is already filed as <c>self.x</c>, and a reader that
    /// prepended the prefix again would have made <c>self.self.x</c> —
    /// <strong>a name the game never wrote and no call can reach</strong>,
    /// while the plain <c>self.x</c> was still there but unreachable from
    /// the module's own key.
    /// </remarks>
    public void Test_AClassMethodInAModuleKeepsItsNameWhenExtended()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "module Werkzeug\n"
            + "  def self.bauen\n"
            + "    3\n"
            + "  end\n"
            + "end\n"
            + "class Held\n"
            + "  extend Werkzeug\n"
            + "end\n"
            + "Held.bauen\n"));

        AssertEq(AsInteger(wert), 3,
            "**the class method answers three** — the module's `def self.bauen` "
                + "is already under `self.bauen`, and a reader that prepended "
                + "the prefix again would have made `self.self.bauen` and left "
                + "the real name unreachable from the module's own key");
        AssertTrue(mit.FindMethod("Held", "self.self.bauen") == null,
            "**and there is no doubled name** — a key the game never wrote is "
                + "a method no call can reach");
    }

    /// <summary>
    /// An attribute is not extended either.
    /// </summary>
    /// <remarks>
    /// <strong>The same rule as `include`, and for the same reason.</strong>
    /// An attribute is a pair of methods standing for a field, and the field
    /// belongs to the class that made it — <strong>and a class method that
    /// reads a field would read a field that class does not own.</strong>
    /// </remarks>
    public void Test_AnAttributeIsNotExtended()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        mit.RunProgram(Statements(
            "module Mit\n"
            + "  attr_accessor :hp\n"
            + "  def eigenes\n"
            + "    1\n"
            + "  end\n"
            + "end\n"
            + "class Nimmt\n"
            + "  extend Mit\n"
            + "end\n"));

        AssertTrue(mit.FindMethod("Nimmt", "self.eigenes") != null,
            "**the module's own method is there as a class method** — a plain "
                + "method is what `extend` copies, and the fixture needs one "
                + "beside the attribute to say that");
        AssertTrue(mit.FindMethod("Nimmt", "self.hp") == null,
            "**and the attribute is not** — it stands for a field the class "
                + "does not own, and a class method reading it would read "
                + "somewhere nobody wrote");
        AssertTrue(mit.FindMethod("Mit", "hp") != null,
            "**while the module still has its own** — a reader that moved it "
                + "instead of skipping it would have taken it away");
    }
}
