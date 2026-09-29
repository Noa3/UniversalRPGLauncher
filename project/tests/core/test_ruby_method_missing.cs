using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// `method_missing` and `respond_to?`, which is how a game answers a hundred
/// command names it did not write.
/// </summary>
/// <remarks>
/// <para>
/// <strong>A plugin layer builds its dispatch out of this.</strong> A game
/// that reads a command table and makes a method per name writes
/// <c>def self.method_missing(name, *args)</c>,
/// <strong>and without it every one of those calls is a refusal</strong> — the
/// host does not know the name, the script has no such method, and a game
/// with two hundred commands would stop at the first.
/// </para>
/// <para>
/// <strong>And the two are not the same question.</strong>
/// <c>respond_to?</c> asks whether a name resolves;
/// <c>method_missing</c> answers names that do not.
/// <strong>A class that answered both the same way would make the question
/// worthless</strong>, and that is the mistake this file is built around.
/// </para>
/// </remarks>
public partial class TestRubyInterpreter
{
    /// <summary>
    /// A class that answers an unknown name through `method_missing`.
    /// </summary>
    /// <remarks>
    /// <strong>The name arrives as the first argument, and that is the whole
    /// point.</strong> `method_missing(name, *args)` takes the name the caller
    /// wrote, <strong>and a reader that passed the call's arguments unchanged
    /// would have handed the handler the first argument under the name
    /// `name`</strong> — so a plugin answering by name would have answered
    /// for the wrong command, silently, on every single one.
    /// </remarks>
    public void Test_MethodMissingAnswersAnUnknownName()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class A\n"
            + "  def self.method_missing(name, x)\n"
            + "    [name, x]\n"
            + "  end\n"
            + "end\n"
            + "A.hurenkommando(7)\n"));

        AssertEq(wert.Items.Count, 2,
            "**the handler answered two things**");
        AssertEq(wert.Items[0].Name, "hurenkommando",
            "**the first is the name that was written** — the one piece of "
                + "information the handler exists to receive, and a reader "
                + "that passed the arguments through unchanged would have "
                + "answered seven here");
        AssertEq(AsInteger(wert.Items[1]), 7,
            "**and the second is the argument the caller gave**");
    }

    /// <summary>
    /// A name that does resolve is not answered by `method_missing`.
    /// </summary>
    /// <remarks>
    /// <strong>It is looked up after the whole chain, and that is what makes
    /// the feature usable.</strong> A class that defines a method *and* a
    /// handler must use its own method,
    /// <strong>and a reader that consulted the handler first would have had
    /// every real method answered by the catch-all</strong> — which is a class
    /// where `draw` works but nothing it wrote ever runs.
    /// </remarks>
    public void Test_AMethodThatExistsIsNotAnsweredByMethodMissing()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class A\n"
            + "  def zeichne\n"
            + "    \"echt\"\n"
            + "  end\n"
            + "  def self.method_missing(name, x)\n"
            + "    \"handler\"\n"
            + "  end\n"
            + "end\n"
            + "A.zeichne\n"));

        AssertTrue(wert.Kind == RubyValueKind.String,
            "**the real method answered** — the handler is for names the "
                + "chain does not have, and a reader that consulted it first "
                + "would have had every real method answered by the "
                + "catch-all");
    }

    /// <summary>
    /// A subclass's handler answers for a name the base does not have.
    /// </summary>
    /// <remarks>
    /// <strong>The chain is walked for the handler too, and the subclass's
    /// own comes first.</strong> A plugin that extends a game's class and
    /// answers its own commands is the ordinary case,
    /// <strong>and a reader that took the first handler walking down would
    /// have had the base answer for a subclass that answered
    /// differently.</strong>
    /// </remarks>
    public void Test_ASubclasssHandlerAnswersAndTheBasesOwnIsFoundToo()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class Basis\n"
            + "  def self.method_missing(name, x)\n"
            + "    \"basis\"\n"
            + "  end\n"
            + "end\n"
            + "class Erbe < Basis\n"
            + "  def self.method_missing(name, x)\n"
            + "    \"erbe\"\n"
            + "  end\n"
            + "end\n"
            + "Erbe.befehl(1)\n"));

        AssertTrue(wert.Kind == RubyValueKind.String,
            "**something answered**");
        AssertEq(wert.Items.Count, 0, "**and it was not a list** — the "
            + "handler answered a plain string, and a reader that had mixed "
            + "the two would have had a list here");
    }

    /// <summary>
    /// A subclass without its own handler gets the base's.
    /// </summary>
    /// <remarks>
    /// <strong>Inheritance has to work here too, or a plugin could only patch
    /// a class it had replaced entirely.</strong>
    /// </remarks>
    public void Test_ASubclassWithoutItsOwnHandlerGetsTheBases()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class Basis\n"
            + "  def self.method_missing(name, x)\n"
            + "    [name, x]\n"
            + "  end\n"
            + "end\n"
            + "class Erbe < Basis\n"
            + "end\n"
            + "Erbe.befehl(3)\n"));

        AssertEq(wert.Items[0].Name, "befehl",
            "**the base's handler answered for the subclass** — the chain is "
                + "walked for the handler as well, and a reader that only "
                + "looked at the class itself would have had a plugin that "
                + "could only patch a class it had replaced");
    }

    /// <summary>
    /// `respond_to?` says yes for a method that exists and no for one that
    /// does not.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And a `method_missing` is not an answer.</strong> Ruby takes a
    /// second argument that says whether to count it, <strong>and the default
    /// is no</strong> — because the point of the question is to know whether
    /// a call will work without raising.
    /// </para>
    /// <para>
    /// <strong>And a handler on the class does not make every name
    /// yes.</strong> A class that answers everything through
    /// <c>method_missing</c> would say yes to everything,
    /// <strong>and the question would be worthless</strong> — so the search
    /// for a real method has to stop before the handler, and a reader that
    /// called `FindMethod` would have walked straight into it.
    /// </para>
    /// </remarks>
    public void Test_RespondToIsNoForAHandlerAndNoForAnUnknownName()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class A\n"
            + "  def zeichne\n"
            + "    1\n"
            + "  end\n"
            + "  def self.method_missing(name, x)\n"
            + "    2\n"
            + "  end\n"
            + "end\n"
            + "[A.respond_to?(:zeichne), A.respond_to?(:gibtsnicht)]\n"));

        AssertEq(wert.Items.Count, 2,
            "**two answers**");
        AssertTrue(wert.Items[0].Kind == RubyValueKind.Boolean,
            "**the first is a boolean and not nil** — `respond_to?` answers a "
                + "question, and a reader that let it fall through to the host "
                + "would have given the null host's nil");
        AssertTrue(wert.Items[0].Boolean,
            "**and it is true for a method that exists**");
        AssertTrue(!wert.Items[1].Boolean,
            "**and false for one that does not, even though the class has a "
                + "handler** — a reader that consulted the handler would have "
                + "said true, and the question would be worthless");
    }

    /// <summary>
    /// `respond_to?` sees a class method and an instance method both.
    /// </summary>
    /// <remarks>
    /// <strong>Both spellings, and a game asks about both.</strong> A plugin
    /// asks whether a class answers a command,
    /// <strong>and a reader that looked only in the instance table would have
    /// said no to every class method.</strong>
    /// </remarks>
    public void Test_RespondToSeesBothMethodSpellings()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class A\n"
            + "  def instanz\n"
            + "    1\n"
            + "  end\n"
            + "  def self.klassen\n"
            + "    1\n"
            + "  end\n"
            + "end\n"
            + "[A.respond_to?(:instanz), A.respond_to?(:klassen)]\n"));

        AssertEq(wert.Items.Count, 2,
            "**two answers**");
        AssertTrue(wert.Items[0].Boolean,
            "**the instance method is there**");
        AssertTrue(wert.Items[1].Boolean,
            "**and the class method too** — a reader that looked only in the "
                + "instance table would have said no to every class method, "
                + "and a plugin asks about those");
    }

    /// <summary>
    /// `respond_to?` sees what the host knows.
    /// </summary>
    /// <remarks>
    /// <strong>The host is asked too, over its own list.</strong> A game asks
    /// <c>respond_to?(:draw)</c> about a host method,
    /// <strong>and a reader that looked only at the script would have said no
    /// and a game would skip a feature the host does have.</strong>
    /// </remarks>
    public void Test_RespondToSeesTheHostsMethods()
    {
        var mit = new RubyInterpreter(new ZaehlHost());
        var wert = mit.RunProgram(Statements("A.respond_to?(:rand)\n"));

        AssertTrue(wert.Boolean,
            "**the host's own method counts** — the host has a list of what "
                + "it knows, and a reader that looked only at the script would "
                + "have said no and a game would skip a feature the host does "
                + "have");
    }

    /// <summary>
    /// A `method_missing` on a class does not answer a plain name outside a
    /// class.
    /// </summary>
    /// <remarks>
    /// <strong>There is no root here.</strong> Ruby would put
    /// <c>method_missing</c> on <c>Object</c> and answer for every object;
    /// this runtime files methods under a class,
    /// <strong>and a reader that invented a root would have had a method
    /// appear out of nowhere for every call a game makes</strong> — including
    /// the ones that are genuine typos.
    /// </remarks>
    public void Test_AnUnknownNameOutsideAClassIsStillARefusal()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        mit.RunProgram(Statements("gibtsnicht(1)\n"));

        var gesagt = false;
        foreach (var d in mit.Diagnostics)
        {
            if (d.Contains("'gibtsnicht'"))
            {
                gesagt = true;
            }
        }

        AssertTrue(gesagt,
            "**and it says so** — the host was asked and refused, because "
                + "there is no root class here to hold a handler; the "
                + "diagnostics were: " + string.Join(" | ", mit.Diagnostics));
    }

    /// <summary>
    /// `undef method_missing` takes the handler away, and the class says no
    /// again.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>The same sentence a class says about anything else, and it has
    /// to work here too.</strong> A game that takes a plugin's handler away
    /// writes <c>undef method_missing</c>,
    /// <strong>and a reader that only removed the table entry would have let
    /// the base class's handler come through</strong> — which is the opposite
    /// of what the sentence says.
    /// </para>
    /// <para>
    /// <strong>And it has to be the base class's handler, not this class's
    /// own.</strong> A subclass writing <c>undef method_missing</c> when it
    /// has one of its own is a table deletion,
    /// <strong>and a table deletion a reader already got right.</strong> The
    /// case that needs the mark is a subclass that has none and must stop the
    /// base's — **and that is the only case the mark exists for.**
    /// </para>
    /// <para>
    /// <strong>And the refusal is the point.</strong> After the `undef` the
    /// host is asked and refuses by name,
    /// <strong>and a reader that only marked the name without reporting would
    /// have left the game with a nil and no reason.</strong>
    /// </para>
    /// </remarks>
    public void Test_UndefMethodMissingTakesTheHandlerAway()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        mit.RunProgram(Statements(
            "class Basis\n"
            + "  def self.method_missing(name, x)\n"
            + "    1\n"
            + "  end\n"
            + "end\n"
            + "class A < Basis\n"
            + "  undef method_missing\n"
            + "end\n"
            + "A.befehl(1)\n"));

        var gesagt = false;
        foreach (var d in mit.Diagnostics)
        {
            if (d.Contains("'befehl'"))
            {
                gesagt = true;
            }
        }

        AssertTrue(gesagt,
            "**and the name is refused by name** — the handler is out, the "
                + "host does not know the name, and a reader that only removed "
                + "the entry would have let a base handler through and "
                + "answered silently instead; the diagnostics were: "
                + string.Join(" | ", mit.Diagnostics));
    }
}
