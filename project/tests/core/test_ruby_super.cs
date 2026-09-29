using System.Collections.Generic;

using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// `super`, which needs the call stack and not just the current class.
/// </summary>
public partial class TestRubyInterpreter
{
    /// <summary>
    /// A bracketless `super` hands the caller's arguments on.
    /// </summary>
    /// <remarks>
    /// <strong>That is the whole difference between the two forms.</strong>
    /// `super` without brackets passes what it got, `super(x)` passes what was
    /// written — <strong>and a reader that treated both alike would have
    /// called the base with the subclass's arguments</strong>, so an override
    /// that changes what the base receives would not change anything.
    /// </remarks>
    public void Test_ABracketlessSuperHandsTheArgumentsOn()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class Basis\n"
            + "  def anzeige(a, b)\n"
            + "    a * 10 + b\n"
            + "  end\n"
            + "end\n"
            + "class Erbe < Basis\n"
            + "  def anzeige(a, b)\n"
            + "    super\n"
            + "  end\n"
            + "end\n"
            + "Erbe.anzeige(4, 2)\n"));

        AssertEq(AsInteger(wert), 42,
            "**the base got four and two** — a reader that passed an empty "
                + "argument list would have made both parameters nil and the "
                + "answer something else entirely");
    }

    /// <summary>
    /// A `super` with brackets hands the written arguments on.
    /// </summary>
    /// <remarks>
    /// <strong>This is the form an override is written in</strong> — a game's
    /// subclass changes what the base receives and writes the new value. A
    /// reader that kept the caller's arguments here would have made the
    /// override a no-op with extra steps.
    /// </remarks>
    public void Test_ASuperWithBracketsHandsTheWrittenArgumentsOn()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class Basis\n"
            + "  def anzeige(a)\n"
            + "    a\n"
            + "  end\n"
            + "end\n"
            + "class Erbe < Basis\n"
            + "  def anzeige(a)\n"
            + "    super(a * 2)\n"
            + "  end\n"
            + "end\n"
            + "Erbe.anzeige(21)\n"));

        AssertEq(AsInteger(wert), 42,
            "**the base got forty-two and not twenty-one** — the written "
                + "argument replaced the caller's, and a reader that kept the "
                + "caller's would have made the override do nothing");
    }

    /// <summary>
    /// `super` walks the chain, and it does not stop at the class it is in.
    /// </summary>
    /// <remarks>
    /// <strong>The chain is the call stack and not the class we are
    /// in.</strong> A `super` inside a method the middle class called finds the
    /// top class's method — <strong>and a reader that remembered only "the
    /// class I am in" would have called the middle class's own method
    /// again</strong>, which is a game going for ever on one line.
    /// </remarks>
    public void Test_ASuperWalksTheWholeChain()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class Oben\n"
            + "  def anzahl\n"
            + "    1\n"
            + "  end\n"
            + "end\n"
            + "class Mitte < Oben\n"
            + "  def anzahl\n"
            + "    super + 10\n"
            + "  end\n"
            + "end\n"
            + "class Unten < Mitte\n"
            + "  def anzahl\n"
            + "    super + 100\n"
            + "  end\n"
            + "end\n"
            + "Unten.anzahl\n"));

        AssertEq(AsInteger(wert), 111,
            "**one hundred and eleven** — the call went Unten, then Mitte, then "
                + "Oben, and a reader that stopped at the class it was in would "
                + "have gone for ever on one line");
    }

    /// <summary>
    /// A `super` in a class with no base says so and answers nil.
    /// </summary>
    /// <remarks>
    /// <strong>Ruby would raise here</strong>, and a game whose override has
    /// no base is broken. The message says which class and which method, so the
    /// answer is a place to start looking — <strong>and a reader that answered
    /// a value would have hidden the break behind a plausible number.</strong>
    /// </remarks>
    public void Test_ASuperWithNoBaseIsADiagnostic()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            // **Der Name beginnt gross, weil Rubys Klassen so beginnen.**
            // `class allein` waere eine kleinegeschriebene Konstante, **und
            // ein kleingeschriebenes `allein` waere eine Variable** -- der
            // Aufruf ginge dann an den Host, **und die Diagnose wuerde den
            // Empfangernamen nicht nennen**, obwohl die Frage "woher"
            // genau die ist, die man stellt.
            "class Allein\n"
            + "  def anzahl\n"
            + "    super\n"
            + "  end\n"
            + "end\n"
            + "Allein.anzahl\n"));

        AssertTrue(wert.IsNil,
            "**nothing came back** — the reference would raise, and a reader "
                + "that answered a value would have hidden the break behind a "
                + "plausible number");
        // **Und die Meldung sucht jetzt in `Object`, und nicht in "keiner
        // Basis".** `class Allein` hat seit heute `Object` als Basis,
        // **und `Object` hat kein `anzahl`** -- **und das ist der Grund, den
        // man beim Lesen braucht:** die Klasse hat eine Basis, **und die
        // Basis hat die Methode nicht.**
        // ***Ein Test, der auf eine Fehlermeldung hoert, die der Leser
        // abgeschafft hat, prueft eine Form und nicht ein Verhalten.***
        var gesagt = false;
        foreach (var d in mit.Diagnostics)
        {
            if (d.Contains("Object does not have anzahl"))
            {
                gesagt = true;
            }
        }

        AssertTrue(gesagt, "**and it says where the search ended and which "
            + "method it did not find** — `class Allein` sits under `Object` "
                + "since a class with no written base gets one, and `Object` "
                + "has no `anzahl`; the old wording said *no superclass*, and "
                + "that sentence is no longer true. The diagnostics were: "
                + string.Join(" | ", mit.Diagnostics));
    }

    /// <summary>
    /// A base that does not have the method says so, naming the method.
    /// </summary>
    /// <remarks>
    /// <strong>The two failures are different and the message has to
    /// tell them apart.</strong> A class with no base and a base without the
    /// method both mean "no `super` to call", and <strong>a reader that said
    /// only that would have sent a reader of the game to the wrong line.</strong>
    /// </remarks>
    public void Test_ASuperNamesTheMethodTheBaseDoesNotHave()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        mit.RunProgram(Statements(
            "class Basis\n"
            + "  def anders\n"
            + "    1\n"
            + "  end\n"
            + "end\n"
            + "class Erbe < Basis\n"
            + "  def fehlt\n"
            + "    super\n"
            + "  end\n"
            + "end\n"
            + "Erbe.fehlt\n"));

        var gesagt = false;
        foreach (var d in mit.Diagnostics)
        {
            if (d.Contains("does not have fehlt"))
            {
                gesagt = true;
            }
        }

        AssertTrue(gesagt, "**the message names the method** — a reader that "
            + "said only \"no super\" would have sent someone to the wrong "
            + "line; the diagnostics were: "
            + string.Join(" | ", mit.Diagnostics));
    }

    /// <summary>
    /// A class method and an instance method of one name stay apart.
    /// </summary>
    /// <remarks>
    /// <strong>A game's script has both under the same name more than
    /// once</strong>, and the flag is what keeps them apart — <strong>a reader
    /// that filed them together would have answered a `self.` call with the
    /// instance body</strong>, and a game would have had two methods where it
    /// wrote one.
    /// </remarks>
    public void Test_AClassMethodAndAnInstanceMethodStayApart()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class Zaehler\n"
            + "  def self.stand\n"
            + "    1\n"
            + "  end\n"
            + "  def stand\n"
            + "    2\n"
            + "  end\n"
            + "end\n"
            + "Zaehler.stand\n"));

        // **Die Reihenfolge ist umgedreht, und das ist der Punkt.** Die
        // Klassenmethode steht ZUERST, die Instanzmethode danach -- **und
        // ohne das `self.`-Praefix wuerde die zweite die erste ueberschreiben**
        // und es stuende dort die Instanzmethode. **Ein Leser, der beide
        // unter einem Schluessel ablegte, wuerde also *dieselbe* Antwort
        // geben, wenn er die Reihenfolge nicht mitdreht** -- und genau das
        // macht die Assertion unten zur Messung und nicht zur Absicht.
        AssertEq(AsInteger(wert), 1,
            "**the class method answered one** — the class method is written "
                + "first and the instance method second, and a reader that filed "
                + "both under one key would have let the second overwrite the "
                + "first and answered two");
    }

    /// <summary>
    /// A `super` out of a class method finds the base's plain method.
    /// </summary>
    /// <remarks>
    /// <strong>The name a class method is filed under carries a
    /// <c>self.</c> prefix and the base's does not.</strong> Without stripping
    /// it, the lookup would have asked the base for <c>self.antwort</c> — and
    /// a base that defined it as a plain method would have been reported as not
    /// having it.
    /// </remarks>
    public void Test_ASuperOutOfAClassMethodFindsTheBasesPlainMethod()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class Basis\n"
            + "  def baue\n"
            + "    10\n"
            + "  end\n"
            + "end\n"
            + "class Erbe < Basis\n"
            + "  def self.baue\n"
            + "    super + 1\n"
            + "  end\n"
            + "end\n"
            + "Erbe.baue\n"));

        AssertEq(AsInteger(wert), 11,
            "**the base's plain method answered ten and the super added one** "
                + "— a reader that kept the self. prefix would have asked the "
                + "base for a method it does not have");
    }
}
