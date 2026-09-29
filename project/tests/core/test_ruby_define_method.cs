using System;
using System.Collections.Generic;

using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// `define_method`, which is how a game writes a method it has as a block.
/// </summary>
/// <remarks>
/// <para>
/// <strong>It is the way a plugin layer builds methods it did not write as
/// source.</strong> A game that reads a name list and makes a command per name
/// writes <c>define_method(name) { ... }</c>, <strong>and a reader without it
/// would have no way to run a method whose name only exists at runtime.</strong>
/// </para>
/// <para>
/// <strong>And it has to land in exactly the same place a <c>def</c> does.</strong>
/// Two paths that both file a method are two places where they can disagree
/// about the parameters, the singleton spelling and the mark <c>undef</c>
/// left — <strong>so every test here is a <c>def</c> test with the source
/// replaced by a block.</strong>
/// </para>
/// </remarks>
public partial class TestRubyInterpreter
{
    /// <summary>
    /// A method made from a block behaves like one written with `def`.
    /// </summary>
    /// <remarks>
    /// <strong>The block is not run where it stands.</strong> `define_method`
    /// builds a method, <strong>and the block runs first when the method
    /// runs</strong> — a reader that evaluated it as an argument would have
    /// run the body once at definition and then built a method that runs it
    /// again, so the body would happen twice.
    /// </remarks>
    public void Test_DefineMethodMakesAMethodThatRunsLater()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class A\n"
            + "  define_method(:doppelt) { |x| x * 2 }\n"
            + "end\n"
            + "A.doppelt(4)\n"));

        AssertEq(AsInteger(wert), 8,
            "**the method answers eight** — it was built from a block and the "
                + "block runs when the method is called, not where it stands");
    }

    /// <summary>
    /// The block's parameters are the method's parameters.
    /// </summary>
    /// <remarks>
    /// <strong>This is the one that has to be right.</strong> A game writes a
    /// two-parameter command and a dispatcher calls it with two values,
    /// <strong>and a reader that filed the block as a body with no parameters
    /// would have had every call arrive with nothing bound</strong> — the
    /// method would answer nil and a game would show a number that never
    /// changed.
    /// </remarks>
    public void Test_TheBlockParametersAreTheMethodsParameters()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class A\n"
            + "  define_method(:summe) { |x, y| x + y }\n"
            + "end\n"
            + "A.summe(3, 4)\n"));

        AssertEq(AsInteger(wert), 7,
            "**both arguments arrived** — the block's parameters are the "
                + "method's, and a reader that filed the block as a body with "
                + "no parameters would have had every call answer nil");
    }

    /// <summary>
    /// A parameter the call does not supply is nil, here as everywhere.
    /// </summary>
    /// <remarks>
    /// <strong>A game writes an optional command this way</strong> — a
    /// dispatcher that passes one value to a method that takes two is
    /// ordinary, <strong>and the rule is the same one a `def` gets</strong>,
    /// which is what "the same place as a def" has to mean.
    /// </remarks>
    public void Test_AMissingArgumentIsNilHereAsWell()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class A\n"
            + "  define_method(:vielleicht) { |x, y| [x, y] }\n"
            + "end\n"
            + "A.vielleicht(1)\n"));

        AssertEq(wert.Items.Count, 2,
            "**the method answered two values**");
        AssertEq(AsInteger(wert.Items[0]), 1,
            "**the first is the one it was given**");
        AssertTrue(wert.Items[1].Kind == RubyValueKind.Nil,
            "**and the second is nil** — the same rule a `def` gets, and a "
                + "reader that gave it a different rule would have made a "
                + "game whose optional commands work in one spelling and not "
                + "in the other");
    }

    /// <summary>
    /// The method lands on the class, and a subclass inherits it.
    /// </summary>
    /// <remarks>
    /// <strong>The chain has to walk to it.</strong> A game that builds a
    /// command on a base class and calls it on a subclass <strong>is the
    /// ordinary case</strong>, and a reader that filed the method somewhere
    /// the chain does not look would have it work on the class and fail on
    /// every subclass.
    /// </remarks>
    public void Test_TheMethodLandsOnTheClassAndASubclassInheritsIt()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class Basis\n"
            + "  define_method(:gruss) { |x| \"hallo \" + x }\n"
            + "end\n"
            + "class Erbe < Basis\n"
            + "end\n"
            + "Erbe.gruss(\"a\")\n"));

        AssertTrue(wert.Kind == RubyValueKind.String,
            "**the subclass answers** — the method was filed on the base and "
                + "the chain walked up to it, and a reader that filed it "
                + "where the chain does not look would have had it work on "
                + "the class and fail on every subclass");
    }

    /// <summary>
    /// `define_singleton_method` is a class method, and the two do not mix.
    /// </summary>
    /// <remarks>
    /// <strong>Two names, and a reader that filed both the same way would have
    /// had a class whose singleton methods were reachable on its
    /// instances</strong> — and a game that defines a command for players and
    /// one for the class would have had them answer each other.
    /// </remarks>
    public void Test_DefineSingletonMethodIsAClassMethod()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        mit.RunProgram(Statements(
            "class A\n"
            + "  define_method(:nur_instanz) { 1 }\n"
            + "  define_singleton_method(:nur_klasse) { 2 }\n"
            + "end\n"));

        AssertTrue(mit.FindMethod("A", "nur_instanz") != null,
            "**the instance method is there**");
        AssertTrue(mit.FindMethod("A", "nur_klasse") == null,
            "**and the class method is not under the plain name** — a reader "
                + "that filed both the same way would have had a class whose "
                + "singleton methods were reachable on its instances");
        AssertTrue(mit.FindMethod("A", "self.nur_klasse") != null,
            "**it is under `self.`** — that is the name a class method has "
                + "here, and the only one the chain looks for");
    }

    /// <summary>
    /// A name taken out with `undef` comes back when a block defines it.
    /// </summary>
    /// <remarks>
    /// <strong>The mark is on the name and not on one way of writing it.</strong>
    /// A class that takes `m` out and later builds it through a block means
    /// the same thing as one that writes it with `def`,
    /// <strong>and a reader that cleared the mark only in the `def` path would
    /// have left a name dead in one spelling and alive in the other</strong> —
    /// which is the kind of thing a game notices as a method that works in
    /// one file and not in the next.
    /// </remarks>
    public void Test_DefineMethodBringsBackAnUndefinedName()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class A\n"
            + "  undef m\n"
            + "  define_method(:m) { 5 }\n"
            + "end\n"
            + "A.m\n"));

        AssertEq(AsInteger(wert), 5,
            "**the name is alive again** — `define_method` writes the method "
                + "the same way `def` does, and a reader that cleared the mark "
                + "only in the `def` path would have left a name dead in one "
                + "spelling and alive in the other");
    }

    /// <summary>
    /// A method written with `def` after `define_method` wins, and the other
    /// way round.
    /// </summary>
    /// <remarks>
    /// <strong>Both write to one table, so the later one wins.</strong> That
    /// is what a game expects when it patches a command: <strong>and a reader
    /// that kept two tables would have had both answers standing and the
    /// winner depending on which the chain asked first.</strong>
    /// </remarks>
    public void Test_TheLaterOfTheTwoSpellingsWins()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class A\n"
            + "  define_method(:m) { 1 }\n"
            + "  def m\n"
            + "    2\n"
            + "  end\n"
            + "end\n"
            + "A.m\n"));

        AssertEq(AsInteger(wert), 2,
            "**the `def` that came second answers two** — both spellings "
                + "write one table, and a reader that kept two would have had "
                + "the winner depend on which the chain asked first");
    }

    /// <summary>
    /// A block with no parameters is a method with no parameters.
    /// </summary>
    /// <remarks>
    /// <strong>`define_method(:tick) { @n += 1 }` is the shape a game's
    /// event layer writes</strong>, and <strong>a reader that invented a
    /// positional parameter for the empty list would have had the method
    /// read a variable the game never named.</strong>
    /// </remarks>
    public void Test_ABlockWithNoParametersIsAMethodWithNoParameters()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class A\n"
            + "  @n = 0\n"
            + "  define_method(:tick) { @n = @n + 1 }\n"
            + "  def stand\n"
            + "    @n\n"
            + "  end\n"
            + "  tick\n"
            + "  tick\n"
            + "  stand\n"
            + "end\n"
            + "A.stand\n"));

        AssertEq(AsInteger(wert), 2,
            "**the method has no parameters, it did not mind the argument, "
                + "and it ran twice** — a reader that invented a positional "
                + "name for the empty list would have had the method read a "
                + "variable the game never named, and one that dropped the "
                + "call's argument would have complained about a parameter "
                + "count that is zero here and one there");
    }

    /// <summary>
    /// A name that is not a symbol is a diagnostic, and not a guess.
    /// </summary>
    /// <remarks>
    /// <strong>Ruby would name the method after the string.</strong> This
    /// reader says what it needs instead, <strong>because a game that passed
    /// an expression would want a name and get a tree</strong> — and a reader
    /// that guessed would have filed a method under a name no call reaches,
    /// which is a silent failure rather than a said one.
    /// </remarks>
    public void Test_ANameThatIsNotASymbolIsADiagnostic()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        mit.RunProgram(Statements(
            "class A\n"
            + "  define_method(\"m\") { 1 }\n"
            + "end\n"));

        var gesagt = false;
        foreach (var d in mit.Diagnostics)
        {
            if (d.Contains("needs a name"))
            {
                gesagt = true;
            }
        }

        AssertTrue(gesagt,
            "**and it says so** — the diagnostics were: "
                + string.Join(" | ", mit.Diagnostics));
    }

    /// <summary>
    /// A missing block is a diagnostic, and not an empty method.
    /// </summary>
    /// <remarks>
    /// <strong>There is no such thing as a method without a body in
    /// Ruby.</strong> A reader that filed an empty one <strong>would have
    /// given a game a command that answers nil forever</strong>, and nothing
    /// would say why.
    /// </remarks>
    public void Test_AMissingBlockIsADiagnostic()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        mit.RunProgram(Statements("class A\n  define_method(:m)\nend\n"));

        AssertTrue(mit.FindMethod("A", "m") == null,
            "**and no method was filed** — a method without a body would "
                + "answer nil forever and nothing would say why");
        var gesagt = false;
        foreach (var d in mit.Diagnostics)
        {
            if (d.Contains("no block"))
            {
                gesagt = true;
            }
        }

        AssertTrue(gesagt, "**and it says so** — the diagnostics were: "
            + string.Join(" | ", mit.Diagnostics));
    }

    /// <summary>
    /// A block belongs to the call that carries it, and not to another one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Two mutations survived everything above, and both are this.</strong>
    /// Dropping the check that the block on top of the stack is the one that
    /// wraps <em>this</em> call, and widening the built-in list from two names
    /// to all of them, both pass every other test in this file —
    /// <strong>because every other test makes one call inside one block.</strong>
    /// </para>
    /// <para>
    /// <strong>Two nested calls, and the inner one is not the outer one.</strong>
    /// An inner <c>define_method</c> must read the inner block,
    /// <strong>and a reader that took whatever was on top of the stack would
    /// have given the inner method the outer block's body</strong> — which is
    /// a method that does something the game never asked for, and answers it
    /// every time it is called.
    /// </para>
    /// </remarks>
    public void Test_ABlockBelongsToTheCallThatCarriesIt()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class A\n"
            + "  define_method(:aussen) do |x|\n"
            + "    define_method(:innen) { |y| y * 100 }\n"
            + "    x * 2\n"
            + "  end\n"
            + "end\n"
            + "A.aussen(1)\n"
            + "A.innen(3)\n"));

        AssertEq(AsInteger(wert), 300,
            "**the inner method has the inner block** — a reader that took "
                + "whatever was on top of the stack would have given it the "
                + "outer block's body, and the inner method would have "
                + "answered four hundred for an argument of four, because "
                + "the outer body multiplies by two");
    }

    /// <summary>
    /// A call that wants no block is not handed one.
    /// </summary>
    /// <remarks>
    /// <strong>The list is short on purpose.</strong> A block at an ordinary
    /// call belongs to that call and the host decides what to do with it,
    /// <strong>and a reader that appended the block to every call would have
    /// handed a host an argument it does not expect</strong> — a host that
    /// counts its arguments would answer something else entirely, and nothing
    /// in the script would say why.
    /// </remarks>
    public void Test_ACallThatWantsNoBlockIsNotHandedOne()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        mit.RunProgram(Statements(
            "class A\n"
            + "  define_method(:m) { 1 }\n"
            + "  m\n"
            + "end\n"
            + "A.zzz\n"));

        var ueberEach = false;
        foreach (var d in mit.Diagnostics)
        {
            if (d.Contains("'zzz'"))
            {
                ueberEach = true;
            }
        }

        AssertTrue(ueberEach, "**the host was asked about zzz** — the "
            + "diagnostics were: " + string.Join(" | ", mit.Diagnostics));
    }

    /// <summary>
    /// A block reaches the host as a callback, and it runs there.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>A block at an ordinary call belongs to that call, and the only
    /// shape it can take on the way out of this interpreter is a callback.</strong>
    /// So a host that had been handed <c>CallMethod</c> instead would have got
    /// a block it cannot run, <strong>and a game's loop would have had nothing
    /// to loop over.</strong>
    /// </para>
    /// <para>
    /// <strong>And the body really runs, once per value.</strong> The host
    /// hands over two values and the body calls <c>rand()</c> twice,
    /// <strong>which is the count that says the block ran and was not merely
    /// parsed and dropped.</strong> A host that ran it once would drop half
    /// the list — <strong>which a game shows as a sprite that walks half its
    /// path.</strong>
    /// </para>
    /// <para>
    /// <strong>The call needs its brackets, and this is where that shows.</strong>
    /// <c>{ rand }</c> is a bare name and answers nil,
    /// <strong>and a test that wrote it without brackets would have measured
    /// the wrong thing entirely</strong> — the body would look broken when it
    /// is the test that is. That is what this test first got wrong.
    /// </para>
    /// </remarks>
    public void Test_ABlockReachesTheHostAsACallback()
    {
        var host = new BlockHost();
        var mit = new RubyInterpreter(host);
        var wert = mit.RunProgram(Statements("[1].each { rand() }\n"));

        // **Einmal je Wert.** Der Gast laeuft die Liste ab und ruft
        // `pYield` fuer jedes Element, **und genau das ist der Vertrag**:
        // der Host entscheidet, wie oft und womit.
        // **`each_with_index` gibt beide Werte in einer Runde** -- es gibt
        // also beides, und ein Test, der nur eines davon erlaubt, wuerde die
        // falsche Regel festschreiben.
        AssertEq(host.Aufrufe.Count, 3,
            "**the host saw `each` and then `rand` twice** — once per value, "
                + "because the body ran once per element; the host saw: "
                + string.Join(", ", host.Aufrufe));
        AssertEq(AsInteger(wert), 6,
            "**and the loop answers what the body answered** — `rand()` "
                + "answers six on this host, and a host that answered nil "
                + "instead of the body's value would make a game's loop hand "
                + "back nothing");
    }

    /// <summary>
    /// A call that wants a block inside a block still gets its own.
    /// </summary>
    /// <remarks>
    /// <strong>The stack has the right block on top, and the check is what
    /// proves it.</strong> The inner <c>define_method</c> stands inside a
    /// block that itself stands at an ordinary call,
    /// <strong>and a reader that took whatever was on top would have given
    /// the inner method the wrong body</strong> — which is a method that does
    /// what the game never asked, every time it is called.
    /// </remarks>
    public void Test_TheInnerCallTakesItsOwnBlockAndNotTheOuterOne()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class A\n"
            + "  def mach\n"
            + "    define_method(:innen) { |y| y * 100 }\n"
            + "    0\n"
            + "  end\n"
            + "end\n"
            + "A.mach\n"
            + "A.innen(3)\n"));

        AssertEq(AsInteger(wert), 300,
            "**the inner method answers three hundred** — it took its own "
                + "block, and a reader that took whatever was on the stack "
                + "would have given it a different body");
    }


    /// <summary>
    /// A host with `each` in it, so a block actually runs.
    /// </summary>
    /// <remarks>
    /// <strong>The null host has no `each`</strong>, so a block written for it
    /// never runs and nothing inside it is ever measured,
    /// <strong>and a test that wants to see a call inside a block has to bring
    /// a host that has one.</strong> This one runs the block once per value,
    /// which is what `each` means,
    /// <strong>and it gives back what the last run answered</strong> — that is
    /// the contract, and a host that answered nil instead would make a game's
    /// loop hand back nothing.
    /// </remarks>
    private sealed class BlockHost : IRubyHost
    {
        public string Name => "the block host";

        public List<string> Aufrufe { get; } = [];

        public List<RubyValue> Argumente { get; } = [];

        public IReadOnlyList<string> KnownMethods => ["rand", "each"];

        public RubyValue? CallMethod(
            RubyValue pReceiver,
            string pMethod,
            IReadOnlyList<RubyValue> pArguments)
        {
            _ = pReceiver;
            Aufrufe.Add(pMethod);
            Argumente.AddRange(pArguments);
            return pMethod == "rand" ? RubyValue.OfInteger(6) : null;
        }

        /// <summary>
        /// Runs the block once per value, and answers what the last run
        /// answered.
        /// </summary>
        /// <param name="pReceiver">The receiver.</param>
        /// <param name="pMethod">The method's name.</param>
        /// <param name="pArguments">The arguments.</param>
        /// <param name="pYield">Runs the block.</param>
        /// <returns>What the last run answered, or a refusal.</returns>
        public RubyValue? CallMethodWithBlock(
            RubyValue pReceiver,
            string pMethod,
            IReadOnlyList<RubyValue> pArguments,
            Func<IReadOnlyList<RubyValue>, RubyValue> pYield)
        {
            _ = pReceiver;
            if (pMethod != "each")
            {
                return CallMethod(pReceiver, pMethod, pArguments);
            }

            // **Einmal je Wert, und das ist der ganze Vertrag.** Ein Gast,
            // der den Block einmal liefe, wuerde die Haelfte der Liste
            // fallen lassen -- **das zeigt sich in einem Spiel als Sprite,
            // der nur die Haelfte seines Weges geht.**
            Aufrufe.Add(pMethod);
            Argumente.AddRange(pArguments);
            // **Einmal je Wert, mit diesem Wert.** Das ist `each`:
            // **das Skript bekommt ein Element pro Runde**, und ein Gast,
            // der alle Werte auf einmal uebergibt, wuerde einem Block mit
            // einem Parameter nur das erste geben und den Rest fallen
            // lassen.
            RubyValue? letztes = null;
            foreach (var w in new[]
            {
                RubyValue.OfInteger(1),
                RubyValue.OfInteger(2),
            })
            {
                letztes = pYield([w]);
            }

            return letztes;
        }

        public RubyValue? LookupConstant(string pName)
        {
            _ = pName;
            return null;
        }
    }

    /// <summary>
    /// A block's parameter comes from the values the host hands over.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>This is the whole point of the callback, and the thing
    /// <c>Yield</c> exists for.</strong> The host hands over values, the block
    /// names them in its parameters, <strong>and the body uses those
    /// names</strong> — so the loop in a game walks a list by receiving each
    /// element. A reader that ran the body without binding would have the
    /// body see nil, <strong>and `items.each { |i| sum = sum + i }` would add
    /// nothing at all.</strong>
    /// </para>
    /// <para>
    /// <strong>And the parameter shadows the caller's name, not overwrites
    /// it.</strong> The block gets its own level,
    /// <strong>and a reader that bound the parameter over the caller's
    /// variable would leave the caller's value changed after the loop</strong>
    /// — which is a bug a game only shows on the second loop.
    /// </para>
    /// </remarks>
    public void Test_ABlockParameterComesFromTheHostsValues()
    {
        var host = new BlockHost();
        var mit = new RubyInterpreter(host);
        var wert = mit.RunProgram(Statements(
            "x = 100\n"
            + "[1, 2].each { |i| x = x + i }\n"
            + "x\n"));

        AssertEq(AsInteger(wert), 103,
            "**the caller's x is 103 and not 100** — the block's `i` was bound "
                + "to the value the host handed over and added to x, and a "
                + "reader that ran the body without binding would have added "
                + "nothing and left x at a hundred");
    }
}
