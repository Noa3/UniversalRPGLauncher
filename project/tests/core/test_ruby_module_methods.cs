using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// What a class or a module answers when it is asked about itself.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is the first line of most VX plugins.</strong>
/// <c>unless MyPatch.instance_methods.include?(:draw)</c>,
/// <strong>and a reader that had no answer for it would stop at the
/// plugin's own guard</strong> — and the plugin would then define a method
/// the game already had.
/// </para>
/// <para>
/// <strong>Measured before: all ten questions were refused.</strong>
/// <c>M.instance_methods</c> said *M has no method 'instance_methods' on
/// this host*, <strong>and <c>Object.ancestors</c> said first that the
/// constant <c>Object</c> is not defined and then that nil has no
/// ancestors</strong> — and both halves of that message were wrong: the
/// reader is the one that has no <c>ancestors</c>, and <c>Object</c> is
/// part of the language and not of a host.
/// </para>
/// </remarks>
public partial class TestRubyInterpreter
{
    /// <summary>
    /// The names of the methods a class has, its own and the whole chain.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And `false` means "only this class".</strong>
    /// <c>A.instance_methods(false)</c> is what a plugin asks before it
    /// overrides anything, <strong>and a reader that ignored the argument
    /// would say the base class's method is already there</strong> — and
    /// the plugin would leave its own override out.
    /// </para>
    /// <para>
    /// <strong>And the answer is symbols.</strong>
    /// <c>M.instance_methods.include?(:x)</c>,
    /// <strong>and a reader that gave the names as texts would answer
    /// <c>false</c> for every name the game wrote</strong>, because
    /// <c>include?</c> looks for a symbol.
    /// </para>
    /// </remarks>
    public void Test_ATypeIsAskedWhatMethodsItHas()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var eigen = mit.RunProgram(Statements(
            "class Basis\n"
            + "  def geerbt\n"
            + "  end\n"
            + "end\n"
            + "class A < Basis\n"
            + "  def eigenes\n"
            + "  end\n"
            + "end\n"
            + "[A.instance_methods(false).length,"
            + " A.instance_methods(false).include?(:eigenes),"
            + " A.instance_methods(false).include?(:geerbt),"
            + " A.instance_methods.length,"
            + " A.instance_methods.include?(:geerbt)]\n"));

        AssertEq(eigen.Items[0].Integer, 1,
            "**`instance_methods(false)` is only this class** — and a reader "
                + "that ignored the argument would say the base class's "
                + "method is already there, and the plugin would leave its "
                + "own override out");
        AssertTrue(eigen.Items[1].Kind == RubyValueKind.Boolean
            && eigen.Items[1].Boolean,
            "**and it found the class's own name**");
        AssertTrue(eigen.Items[2].Kind == RubyValueKind.Boolean
            && !eigen.Items[2].Boolean,
            "**and not the base class's** — one name, and not two");
        AssertEq(eigen.Items[3].Integer, 2,
            "**and without the argument it walks the whole chain** — the "
                + "base class's method is there");
        AssertTrue(eigen.Items[4].Kind == RubyValueKind.Boolean
            && eigen.Items[4].Boolean,
            "**and the answer is symbols** — a reader that gave the names as "
                + "texts would answer false for every name the game wrote, "
                + "because `include?` looks for a symbol");
        AssertEq(mit.Diagnostics.Count, 0,
            "**and none of it was a refusal** — measured before: *M has no "
                + "method 'instance_methods' on this host*");
    }

    /// <summary>
    /// A class's own `to_s` and `name` do not answer for the class, and
    /// `superclass` walks up.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And a type is not an object.</strong> <c>A.to_s</c> is
    /// <c>"A"</c> and not the body's own text,
    /// <strong>and a reader that ran the body's <c>to_s</c> against the
    /// class would make every class show a name it wrote for its
    /// instances</strong> — and a scene class would draw its own name in
    /// the field that expects its characters.
    /// </para>
    /// <para>
    /// <strong>And <c>BasicObject.superclass</c> is nil.</strong>
    /// <strong>A reader that made a class its own base would walk
    /// for ever</strong>, and that is the sentence
    /// <c>Object.ancestors</c> ends on.
    /// </para>
    /// </remarks>
    public void Test_ATypeIsNotAnObjectAndItsChainEndsInNil()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class A\n"
            + "  def to_s\n"
            + "    'von A'\n"
            + "  end\n"
            + "end\n"
            + "[A.to_s, A.name, A.superclass, Object.superclass,"
            + " BasicObject.superclass, Object.ancestors.length,"
            + " Object.ancestors.include?(Object)]\n"));

        AssertEq(System.Text.Encoding.UTF8.GetString(wert.Items[0].Bytes), "A",
            "**the type answered with its own name** — a reader that ran the "
                + "body's `to_s` against the class would make every class "
                + "show a name it wrote for its instances");
        AssertTrue(wert.Items[1].Kind == RubyValueKind.Symbol,
            "**and `name` is a symbol**");
        // **Und gemessen: `class A` ohne `<` hat `Object` als Basis.**
        // Ruby gibt einer Klasse ohne geschriebene Basis die Basis `Object`,
        // **und ein Leser, der `null` las, hatte eine Klasse ohne Eltern** --
        // **und `A.is_a?(Object)` waere falsch und `A.ancestors` waere
        // `[A]`**, **und damit wuerde jede Wache in einem Skript gegen die
        // eigene Basisklasse `nein` sagen.**
        AssertTrue(wert.Items[2].Kind == RubyValueKind.Symbol
            && wert.Items[2].Name == "Object",
            "**a class with no written base sits under `Object`** — measured, "
                + "and not nil; a reader that read null had a class with no "
                + "parents, and `A.is_a?(Object)` would be false and "
                + "`A.ancestors` would be `[A]`, and every guard in a script "
                + "would say no to its own base class");
        // **Und gemessen: `Object.superclass` ist `BasicObject`, und erst
        // dessen ist nil.** Meine erste Erwartung war das Gegenteil,
        // **und ein Test, der `nil` behauptet, um die Kette zu beenden,
        // schneidet sie ab dem zweiten Glied ab** -- **und `Object.ancestors`
        // waere dann `[Object]` und nicht `[Object, BasicObject]`.**
        AssertTrue(wert.Items[3].Kind == RubyValueKind.Symbol
            && wert.Items[3].Name == "BasicObject",
            "**`Object`'s superclass is `BasicObject`** — measured, and not "
                + "nil; my first expectation was the other way round, and a "
                + "test that asserts nil to end the chain cuts it at the "
                + "second link, and `Object.ancestors` would be `[Object]` "
                + "and not `[Object, BasicObject]`");
        AssertTrue(wert.Items[4].Kind == RubyValueKind.Nil,
            "**and `BasicObject`'s is nil** — the chain ends, and that is the "
                + "sentence `ancestors` ends on");
        AssertTrue(wert.Items[5].Kind == RubyValueKind.Integer
            && wert.Items[5].Integer >= 2,
            "**and `Object.ancestors` is a real list** — measured before: "
                + "*the constant Object is not defined by this host*, and "
                + "then *nil has no method 'ancestors'*, and both halves "
                + "were wrong");
        AssertTrue(wert.Items[6].Kind == RubyValueKind.Boolean
            && wert.Items[6].Boolean,
            "**and the list contains the type itself**");
    }

    /// <summary>
    /// `include?` asks about a module, and the module is remembered, not
    /// only copied.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And a copy is not enough.</strong> <c>include M</c> copies
    /// M's methods into the class,
    /// <strong>and a reader that kept only the copy could not answer
    /// <c>include?</c> at all</strong> — and that question is what stops a
    /// plugin from loading twice.
    /// </para>
    /// <para>
    /// <strong>And a name counts in either spelling.</strong>
    /// <c>A.include?("Comparable")</c> and <c>A.include?(:Comparable)</c>
    /// are the same question,
    /// <strong>and a reader that took only symbols would say <c>false</c>
    /// for the one the game wrote as a text</strong> — and the plugin would
    /// include itself twice.
    /// </para>
    /// </remarks>
    public void Test_IncludeRemembersTheModuleAndAnswersInclude()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "module M\n"
            + "  def x\n"
            + "  end\n"
            + "end\n"
            + "class A\n"
            + "  include M\n"
            + "end\n"
            + "[A.include?(M), A.include?(\"M\"), A.include?(String),"
            + " String.include?(Comparable), A.included_modules.include?(M),"
            + " M.is_a?(Module), A.is_a?(Module), M.is_a?(Class),"
            + " A.is_a?(Class)]\n"));

        AssertTrue(wert.Items[0].Kind == RubyValueKind.Boolean
            && wert.Items[0].Boolean,
            "**the class includes the module** — and a reader that kept only "
                + "the copy could not answer this at all, and this is what "
                + "stops a plugin from loading twice");
        AssertTrue(wert.Items[1].Kind == RubyValueKind.Boolean
            && wert.Items[1].Boolean,
            "**and a text names it too** — a reader that took only symbols "
                + "would say false for the spelling a game writes");
        AssertTrue(wert.Items[2].Kind == RubyValueKind.Boolean
            && !wert.Items[2].Boolean,
            "**and a module that is not in it says no**");
        AssertTrue(wert.Items[3].Kind == RubyValueKind.Boolean
            && wert.Items[3].Boolean,
            "**`String` includes `Comparable`** — measured before: *the "
                + "constant Comparable is not defined by this host*, and the "
                + "host was being asked about the language");
        AssertTrue(wert.Items[4].Kind == RubyValueKind.Boolean
            && wert.Items[4].Boolean,
            "**and `included_modules` lists it**");
        AssertTrue(wert.Items[5].Kind == RubyValueKind.Boolean
            && wert.Items[5].Boolean,
            "**and a module is a `Module`** — that is what separates "
                + "`module` from `class`");
        AssertTrue(wert.Items[6].Kind == RubyValueKind.Boolean
            && wert.Items[6].Boolean,
            "**and a class is one too**");

        // **Und gemessen: Index 6 ist `A.is_a?(Module)` und Index 7 ist
        // `M.is_a?(Class)`.** Ich hatte die beiden umgekehrt gelesen,
        // **und ein Test, der die Reihenfolge eines Skripts erraet statt
        // zu messen, ist ein Test, der die eigene Aenderung als Fehler
        // meldet.**
        AssertTrue(wert.Items[6].Kind == RubyValueKind.Boolean
            && wert.Items[6].Boolean,
            "**a class is a `Module`** — measured before: *is_a? needs a "
                + "name, and a symbol is what a game writes*, which said "
                + "nothing about the type at all");
        AssertTrue(wert.Items[7].Kind == RubyValueKind.Boolean
            && !wert.Items[7].Boolean,
            "**and a module is not a `Class`** — that is the difference "
                + "between `module` and `class`, and a game that builds an "
                + "instance list from a constant has to be able to tell "
                + "them apart");

        // **Und `A.include?(Object)` ist wahr, weil `class A` unter
        // `Object` sitzt.** Das ist der Satz, den die erste Fassung der
        // membership-Funktion verleugnet hat:
        // **sie pruefte `pTyp.Superclass != "Object"`, und `class A` hat
        // genau das** -- **und damit war `false` fuer jede Klasse ohne
        // geschriebene Basis.** *Ein `include?`, das die Basis
        // ausschliesst statt sie zu suchen, fragt nach einem Feld und nicht
        // nach der Kette.*
        var mit2 = new RubyInterpreter(new RubyNullHost());
        var basis = mit2.RunProgram(Statements(
            "class A\nend\n[A.include?(Object), Object.include?(Object)]\n"));
        AssertTrue(basis.Items[0].Kind == RubyValueKind.Boolean
            && basis.Items[0].Boolean,
            "**`A.include?(Object)` is true** — and it was false before, "
                + "because the membership test excluded the base field "
                + "instead of searching the chain, and `class A` has "
                + "`Superclass == \"Object\"`");
        AssertTrue(basis.Items[1].Kind == RubyValueKind.Boolean
            && !basis.Items[1].Boolean,
            "**and `Object.include?(Object)` is false** — a type is not a "
                + "module of itself, and a reader that said true would let a "
                + "plugin check twice and add itself again");
    }
}
