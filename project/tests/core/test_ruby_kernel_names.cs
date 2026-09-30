using System;
using System.Collections.Generic;
using System.Linq;
using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// A throw walks outwards to the catch with its own name, a block can hang
/// on a value without changing it, and a script can write a line.
/// </summary>
public partial class TestRubyInterpreter
{
    /// <summary>
    /// A `catch` answers with the value its block gave or the one that was
    /// thrown.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is how every VX state machine is written.</strong>
    /// Measured before this: nil, and the diagnostic <i>self has no method
    /// 'catch' on this host</i> — **which names the host for a sentence
    /// the host never owed**, because <c>catch</c> is a <c>Kernel</c>
    /// method and the reader builds <c>Kernel</c> itself.
    /// </para>
    /// <para>
    /// <strong>And a block that was never left answers with what it
    /// answered.</strong> <c>catch(:a) { 1 + 1 }</c> is 2 — **and a
    /// reader that always answered nil would tell a script that every
    /// state machine had failed.**
    /// </para>
    /// <para>
    /// <strong>And a throw walks outwards until it finds its own name.</strong>
    /// <c>catch(:a) { catch(:b) { throw :a, 7 } }</c> is 7 — **and a
    /// reader with one slot would let the inner catch swallow a throw
    /// meant for the outer one**, **and every nested window uses that.**
    /// </para>
    /// <para>
    /// <strong>And the block came from the wrong place, twice.</strong> It
    /// hangs on the call, and <c>EvaluateBlock</c> puts it on
    /// <c>_blockKette</c> — **and it is the whole block node, not its
    /// body**, **because <c>BlockAufrufen</c> reads <c>Children[1]</c> for
    /// the parameters and <c>Children[2]</c> for the body itself.**
    /// </para>
    /// </remarks>
    public void Test_ACatchAnswersWithWhatWasThrownOrWhatTheBlockGave()
    {
        var geworfen = new RubyInterpreter(new RubyNullHost());
        var wert = geworfen.RunProgram(Statements(
            "r = catch(:f) do\n"
            + "  throw :f, 42\n"
            + "end\n"
            + "r\n"));

        AssertEq(wert.Integer, 42,
            "**`throw :f, 42` answers 42** -- measured before this: nil, "
                + "and the diagnostic said self has no method 'catch' on "
                + "this host, which names the host for a Kernel method the "
                + "reader itself has to build");
        AssertEq(geworfen.Diagnostics.Count, 0,
            "**and nothing was said** -- and the diagnostics were: "
                + string.Join(" | ", geworfen.Diagnostics));

        var nichtGeworfen = new RubyInterpreter(new RubyNullHost());
        var zwei = nichtGeworfen.RunProgram(Statements(
            "r = catch(:f) do\n"
            + "  1 + 1\n"
            + "end\n"
            + "r\n"));
        AssertEq(zwei.Integer, 2,
            "**and a block that was never left answers what it answered** -- "
                + "2, and not nil");

        // **Und ein Wurf verlaesst jeden Block dazwischen.**
        var nach = new RubyInterpreter(new RubyNullHost());
        var eins = nach.RunProgram(Statements(
            "r = catch(:f) do\n"
            + "  x = 1\n"
            + "  throw :f, x\n"
            + "  2\n"
            + "end\n"
            + "r\n"));
        AssertEq(eins.Integer, 1,
            "**and the rest of the block does not run** -- the value the "
                + "throw carries, and not the 2 behind it");

        // **Und verschachtelt, mit einem Wurf nach aussen.**
        var verschachtelt = new RubyInterpreter(new RubyNullHost());
        var sieben = verschachtelt.RunProgram(Statements(
            "r = catch(:a) do\n"
            + "  catch(:b) do\n"
            + "    throw :a, 7\n"
            + "  end\n"
            + "end\n"
            + "r\n"));
        AssertEq(sieben.Kind == RubyValueKind.Integer, true,
            "**and a nested throw walks outwards to its own name** -- 7, "
                + "and not a silent nil; a reader with one catch slot "
                + "would let the inner one swallow a throw meant for the "
                + "outer one, and every VX window nests its catches");
        AssertEq(sieben.Integer, 7, "**and it carries the value**");

        // **Und der innere faengt seinen eigenen Wurf.**
        var innen = new RubyInterpreter(new RubyNullHost());
        var innenWert = innen.RunProgram(Statements(
            "r = catch(:a) do\n"
            + "  x = catch(:b) do\n"
            + "    throw :b, 1\n"
            + "    2\n"
            + "  end\n"
            + "  x + 10\n"
            + "end\n"
            + "r\n"));
        AssertEq(innenWert.Integer, 11,
            "**and the inner catch takes its own throw** -- 1 and not 7,"
                + " and then the outer block adds 10");

        // **Und ein Wurf, der den inneren catch verlaesst, laesst
        // den aeusseren weiterlaufen.** Das ist der Satz, an dem ein
        // Leser scheitert, der immer den innersten nimmt:
        // **`catch(:a) { catch(:b) { throw :a, 1 }; 20 }` ist 21**
        // **und nicht 1** -- **und der Unterschied ist, ob der
        // aeussere Block fertig laeuft.**
        var laeuftWeiter = new RubyInterpreter(new RubyNullHost());
        var weiter = laeuftWeiter.RunProgram(Statements(
            "r = catch(:a) do\n"
            + "  catch(:b) do\n"
            + "    throw :a, 1\n"
            + "  end\n"
            + "  20\n"
            + "end\n"
            + "r\n"));
        // **Und ein Wurf verlaesst JEDEN Block bis zum catch, und
        // nicht nur den inneren.** Das ist der ganze Unterschied
        // zwischen `throw` und `break`, **und Ruby macht es so**:
        // `catch(:a) { catch(:b) { throw :a, 1 }; 20 }` ist 1 und
        // nicht 21. **Ich hatte hier 21 erwartet und der Leser hat
        // 1 gemessen, und der Leser hatte recht** — **und ein
        // Leser, der nur den inneren Block verlassen wuerde, wuerde
        // 21 geben und ein Skript einen Weg weiterlaufen lassen, den
        // Ruby nicht kennt.**
        var verlaesstAlles = new RubyInterpreter(new RubyNullHost());
        var nurEins = verlaesstAlles.RunProgram(Statements(
            "r = catch(:a) do\n"
            + "  catch(:b) do\n"
            + "    throw :a, 1\n"
            + "  end\n"
            + "  20\n"
            + "end\n"
            + "r\n"));
        AssertEq(nurEins.Integer, 1,
            "**and a throw leaves every block up to the catch** -- "
                + "1, not 21; measured before I believed it, and the "
                + "reader was right and my expectation was wrong; a "
                + "reader that left only the inner block would answer "
                + "21 and let a script run on a way Ruby does not "
                + "have");

        // **Und ein Block, der nicht geworfen hat, laeuft weiter.**
        var ohneWurf = new RubyInterpreter(new RubyNullHost());
        var zwanzig = ohneWurf.RunProgram(Statements(
            "r = catch(:a) do\n"
            + "  catch(:b) do\n"
            + "    9\n"
            + "  end\n"
            + "  20\n"
            + "end\n"
            + "r\n"));
        AssertEq(zwanzig.Integer, 20,
            "**and a block that did not throw runs on** -- 20, and that "
                + "is the case where the rest of the outer block belongs");
    }

    /// <summary>
    /// A block on a value runs without changing the value, or with.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And <c>tap</c> gives the receiver back and <c>then</c> gives
    /// the block's answer.</strong> That is the whole difference — **and a
    /// reader that gave the answer for both would turn
    /// <c>x.tap { setup }</c> into nil**, **and that is the line every
    /// initialization is written as.**
    /// </para>
    /// <para>
    /// <strong>And the receiver is not the first argument.</strong>
    /// <c>5.tap { |v| v }</c> has one argument, **and it is the block** —
    /// **and a reader that took the first argument as the receiver would
    /// pass the block to itself and the value nowhere.** Measured before
    /// this: nil for both.
    /// </para>
    /// </remarks>
    public void Test_ABlockOnAValueRunsWithoutChangingIt()
    {
        var m = new RubyInterpreter(new RubyNullHost());
        var wert = m.RunProgram(Statements(
            "[5.tap { |v| v }, 5.then { |v| v + 1 }]\n"));

        AssertTrue(wert.IsList && wert.Items.Count == 2,
            "**two answers**");
        AssertEq(wert.Items[0].Integer, 5,
            "**and `tap` is the receiver** -- 5 and not the block's "
                + "answer, because `x.tap { setup }` is the line every "
                + "initialization is written as and a nil there breaks "
                + "the chain");
        AssertEq(wert.Items[1].Integer, 6,
            "**and `then` is the block's answer** -- 5 + 1, and not the "
                + "receiver");

        var nurTap = new RubyInterpreter(new RubyNullHost());
        var fuenf = nurTap.RunProgram(Statements("x = 5\nx.tap { |v| v }\n"));
        AssertEq(fuenf.Integer, 5,
            "**and `tap` alone is the value** -- a reader that gave the "
                + "block's answer would make every chain end in whatever "
                + "the block happened to return");
        // **Und ein Block, der etwas anderes gibt, aendert tap nicht.**
        var anderer = new RubyInterpreter(new RubyNullHost());
        var immerNochFuenf = anderer.RunProgram(Statements(
            "x = 5\n"
            + "x.tap { |v| v * 99 }\n"));
        AssertEq(immerNochFuenf.Integer, 5,
            "**and `x.tap { |v| v * 99 }` is still five** -- and not 495, "
                + "and that is the whole difference to `then`, and a "
                + "reader that gave the block's answer would turn every "
                + "initialization into whatever the last line computed");

        AssertEq(m.Diagnostics.Count, 0,
            "**and nothing was said** -- and the diagnostics were: "
                + string.Join(" | ", m.Diagnostics));
    }

    /// <summary>
    /// A script can write a line, and the host owns where it goes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And <c>print</c> joins and <c>puts</c> separates.</strong>
    /// <c>print 1, 2</c> writes <c>12</c> and <c>puts 1, 2</c> writes
    /// <c>1</c> and then <c>2</c> — **and a reader that gave both the same
    /// treatment would make one of the two names wrong**, **and every
    /// script writes both.**
    /// </para>
    /// <para>
    /// <strong>And <c>p</c> gives its argument back.</strong> That is the
    /// whole difference to <c>puts</c>, **and <c>p</c> stands in every
    /// script that wants to see a value without losing it.**
    /// </para>
    /// <para>
    /// <strong>And the host owns the writing.</strong>
    /// <c>IRubyHost.Write</c> and <c>WriteLine</c> are default members, so
    /// no host has to implement them — **and a reader that wrote to the
    /// console itself would take that away from the game.**
    /// </para>
    /// <para>
    /// <strong>And <c>print</c> was refused before this</c>, **and
    /// <c>puts</c> with a list was silently nil** — **which is worse than
    /// a refusal, because nothing said so.**
    /// </para>
    /// </remarks>
    public void Test_AScriptCanWriteALine()
    {
        // **Und der Host sieht, was geschrieben wurde.**
        var sammler = new SchreibHost();
        var m = new RubyInterpreter(sammler);
        var wert = m.RunProgram(Statements(
            "print 1, 2\n"
            + "puts 3\n"
            + "p 4\n"));

        AssertEq(wert.Integer, 4,
            "**and `p` gives its argument back** -- and not nil, and that "
                + "is the whole difference to `puts`");
        AssertEq(m.Diagnostics.Count, 0,
            "**and nothing was said** -- and the diagnostics were: "
                + string.Join(" | ", m.Diagnostics));

        AssertEq(sammler.Geschrieben, "123" + System.Environment.NewLine
                + "4",
            "**and `print 1, 2` wrote 12, and `puts 3` wrote 3 on a line, "
                + "and `p 4` wrote 4** -- and a reader that gave both the "
                + "same treatment would make one of the two names wrong, "
                + "and every script writes both");

        // **Und `raise` ohne Klammern geht auch durch.**
        var wirft = new RubyInterpreter(new RubyNullHost());
        var gefangen = wirft.RunProgram(Statements(
            "begin\n"
            + "  raise ArgumentError, \"nein\"\n"
            + "rescue => e\n"
            + "  e.class\n"
            + "end\n"));
        AssertEq(gefangen.Name is "ArgumentError", true,
            "**and `raise ArgumentError, \"ne\"` is caught** -- a name "
                + "without brackets has to be found before it is called, "
                + "and measured before this `puts 3` and `p 4` never "
                + "reached the call at all because `Name()` only looked "
                + "for a script method");

        // **Und `puts` auf eine Liste schreibt die Werte einzeln.**
        var mitListe = new SchreibHost();
        var l = new RubyInterpreter(mitListe);
        l.RunProgram(Statements("puts [1, 2]\n"));
        System.Console.WriteLine(
            "OH  liste=[" + mitListe.Geschrieben + "]");
        AssertEq(mitListe.Geschrieben,
            "1" + System.Environment.NewLine + "2"
                + System.Environment.NewLine,
            "**and `puts [1, 2]` wrote 1 and 2, and not `[1, 2]`** -- and "
                + "that is what makes a debug line of a window readable");

        // **Und ein Host, der nicht schreibt, ist kein Fehler.**
        var still = new RubyInterpreter(new RubyNullHost());
        var nichts = still.RunProgram(Statements("print \"a\"\nnil\n"));
        AssertEq(nichts.Kind, RubyValueKind.Nil,
            "**and a host that does not write still gets nil** -- the "
                + "sentence is answered and the text goes nowhere, and "
                + "that is the honest answer for a debug line");
    }
    /// <summary>A host that keeps what a script wrote.</summary>
    /// <remarks>
    /// <strong>And it answers nothing else.</strong> The writing is what is
    /// under test, **and a host that answered a method would make the
    /// reader path a second one and the sentence ambiguous.**
    /// </remarks>
    private sealed class SchreibHost : IRubyHost
    {
        public string Geschrieben { get; private set; } = string.Empty;

        public string Name => "the writing host";

        public IReadOnlyList<string> KnownMethods => [];

        public void Write(string pText) => Geschrieben += pText;

        public void WriteLine(string pText) =>
            Geschrieben += pText + System.Environment.NewLine;

        public RubyValue? CallMethod(
            RubyValue pReceiver,
            string pMethod,
            IReadOnlyList<RubyValue> pArguments)
        {
            _ = pReceiver;
            _ = pMethod;
            _ = pArguments;
            return null;
        }

        public RubyValue? CallMethodWithBlock(
            RubyValue pReceiver,
            string pMethod,
            IReadOnlyList<RubyValue> pArguments,
            Func<IReadOnlyList<RubyValue>, RubyValue> pYield)
        {
            _ = pReceiver;
            _ = pMethod;
            _ = pArguments;
            _ = pYield;
            return null;
        }

        public RubyValue? LookupConstant(string pName)
        {
            _ = pName;
            return null;
        }
    }
}
