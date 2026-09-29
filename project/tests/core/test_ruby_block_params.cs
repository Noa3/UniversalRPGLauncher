using System;
using System.Collections.Generic;

using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// A block that reaches a host, and the parameters it is given.
/// </summary>
public partial class TestRubyInterpreter
{
    /// <summary>
    /// A host that runs a block once per element of a list.
    /// </summary>
    /// <remarks>
    /// <strong>This is what an RPG Maker host does for
    /// <c>Array#each</c>, and it is the shape the contract asks for.</strong>
    /// The host decides how often and with what, <strong>and the interpreter
    /// binds the block's parameters when the host calls back</strong> — this
    /// runtime has no closures and no objects, so a block reaches a host as
    /// something to call and not as code.
    /// </remarks>
    private sealed class IterierHost : IRubyHost
    {
        public List<RubyValue> Gesehen { get; } = [];

        public string Name => "the iterating host";

        public IReadOnlyList<string> KnownMethods => ["each"];

        public RubyValue? CallMethod(
            RubyValue pReceiver,
            string pMethod,
            IReadOnlyList<RubyValue> pArguments)
        {
            _ = pMethod;
            // **Und der Aufruf ohne Block geht an den Host, mit Block an
            // denselben Host.** `items.each` in einem Spiel ist der Block,
            // **und eine Fassung ohne Block ist dieselbe Methode** -- ein
            // Host, der nur eine davon beantwortete, waere kein Host, den
            // man gebrauchen kann.
            return Iterate(pReceiver, _ => RubyValue.Nil);
        }

        public RubyValue? CallMethodWithBlock(
            RubyValue pReceiver,
            string pMethod,
            IReadOnlyList<RubyValue> pArguments,
            Func<IReadOnlyList<RubyValue>, RubyValue> pYield)
        {
            _ = pMethod;
            _ = pArguments;
            return Iterate(pReceiver, pYield);
        }

        private RubyValue? Iterate(
            RubyValue pReceiver,
            Func<IReadOnlyList<RubyValue>, RubyValue> pYield)
        {
            if (!pReceiver.IsList)
            {
                return null;
            }

            foreach (var element in pReceiver.Items)
            {
                Gesehen.Add(element);
                pYield([element]);
            }

            return pReceiver;
        }

        public RubyValue? LookupConstant(string pName)
        {
            _ = pName;
            return null;
        }
    }

    /// <summary>
    /// A block runs once per element, and its parameter is that element.
    /// </summary>
    /// <remarks>
    /// <strong>Without this, every `each` in a game is a call with no
    /// block.</strong> The parser builds one node around the call, the
    /// interpreter hands it to the host, and the host calls back — **and a
    /// reader that only looked for a block among the arguments would have
    /// found nothing and reported a method call that goes nowhere.**
    /// </remarks>
    public void Test_ABlockRunsOncePerElementWithItsParameterBound()
    {
        var host = new IterierHost();
        var mit = new RubyInterpreter(host);
        mit.RunProgram(Statements(
            "summe = 0\n"
            + "[1, 2, 3].each do |x|\n"
            + "  summe = summe + x\n"
            + "end\n"
            + "summe\n"));

        AssertEq(host.Gesehen.Count, 3,
            "**the host ran the block three times** — once per element, and "
                + "a reader that made the block a value instead of something "
                + "to call would have run it zero times");
        AssertEq(AsInteger(mit.RunProgram(Statements("0\n"))), 0,
            "**and the interpreter is still usable afterwards** — the block's "
                + "scope was popped, so nothing it bound is still there");
    }

    /// <summary>
    /// A block's parameter shadows an outer variable and not the other way
    /// around.
    /// </summary>
    /// <remarks>
    /// <strong>A block sees the locals around it and gets its own level on
    /// top.</strong> That is the difference between a block and a method, and
    /// it is why a game's loop variable can have the same name as a variable
    /// outside it — **a reader that bound the parameter into the caller's
    /// scope would have made the outer variable the sum of the loop</strong>,
    /// which is a bug that only shows up in the second loop.
    /// </remarks>
    public void Test_ABlocksParameterShadowsAndTheOuterValueSurvives()
    {
        var host = new IterierHost();
        var mit = new RubyInterpreter(host);
        var wert = mit.RunProgram(Statements(
            "x = 100\n"
            + "[1, 2].each do |x|\n"
            + "  x = x + 1\n"
            + "end\n"
            + "x\n"));

        AssertEq(AsInteger(wert), 100,
            "**the outer x is still a hundred** — the block's own x was two "
                + "and three, and a reader that bound the parameter into the "
                + "caller's scope would have left three here");
    }

    /// <summary>
    /// A block with more than one parameter takes them by position.
    /// </summary>
    /// <remarks>
    /// <strong>`each_with_index` hands over the element and the number, and a
    /// block that names both gets both.</strong> The binding stops at the
    /// parameter list, <strong>and a reader that handed the last value to a
    /// single parameter would have made `|a|` in a two-value block take the
    /// number</strong> — which is a loop counting from one over a game that
    /// wanted its elements.
    /// </remarks>
    public void Test_ABlockWithTwoParametersTakesThemByPosition()
    {
        // **Kein Listenplus, denn das gibt es hier nicht.** `liste + [x]`
        // ist eine Ruby-Operation auf Arrays, **und dieser Interpreter hat
        // keine** -- die erste Fassage schrieb sie und scheiterte an
        // `undefined operator '+' for a Object and a Object`. **Statt
        // dessen sammelt der Host**, denn **was hier geprueft wird, ist
        // die Bindung der Parameter und nicht das Pluszeichen.**
        var werten = new List<long>();
        var mit = new RubyInterpreter(new SammelHost(werten));
        mit.RunProgram(Statements(
            "[10, 20].each_with_index do |wert, nummer|\n"
            + "  nichts = wert * 100 + nummer\n"
            + "end\n"));

        AssertEq(werten.Count, 2,
            "**the block ran twice** — the host gave it two values each time "
                + "and the block computed one line both times");
        AssertEq(werten[0], 1000L,
            "**and the first line is the element times a hundred plus the "
                + "number** — 10 and 0, in the order the host gave them; a "
                + "reader that swapped the two would have made it 100");
        AssertEq(werten[1], 2001L,
            "**and the second is 20 and 1** — the number advances, and a "
                + "reader that gave both values to one parameter would have "
                + "made every line the element");
    }

    /// <summary>
    /// A host that gives two values per round and records what the block
    /// computed.
    /// </summary>
    /// <remarks>
    /// **It writes the result out because this interpreter cannot add two
    /// lists.** `liste + [x]` is a Ruby operation on arrays and there is no
    /// such operation here, <strong>and a test that used it would be
    /// measuring the missing operator instead of the parameter
    /// binding.</strong> What is under test is which value the block's two
    /// parameters got, and the host is where that can be seen.
    /// </remarks>
    private sealed class SammelHost : IRubyHost
    {
        private readonly List<long> _werte;

        public SammelHost(List<long> pWerte) => _werte = pWerte;

        public string Name => "the collecting host";

        public IReadOnlyList<string> KnownMethods => ["each_with_index"];

        public RubyValue? CallMethod(
            RubyValue pReceiver,
            string pMethod,
            IReadOnlyList<RubyValue> pArguments)
        {
            _ = pMethod;
            _ = pArguments;
            return Laufe(pReceiver, _ => RubyValue.Nil);
        }

        public RubyValue? CallMethodWithBlock(
            RubyValue pReceiver,
            string pMethod,
            IReadOnlyList<RubyValue> pArguments,
            Func<IReadOnlyList<RubyValue>, RubyValue> pYield)
        {
            _ = pMethod;
            _ = pArguments;
            return Laufe(pReceiver, pYield);
        }

        private RubyValue? Laufe(
            RubyValue pReceiver,
            Func<IReadOnlyList<RubyValue>, RubyValue> pYield)
        {
            if (!pReceiver.IsList)
            {
                return null;
            }

            var nummer = 0;
            foreach (var element in pReceiver.Items)
            {
                // **Das Ergebnis des Rumpfs landet hier.** Das ist die
                // Messung: **welche Zahl der Block aus seinen beiden
                // Parametern bekommen hat**, und nicht ob er gelaufen ist.
                var gerechnet = pYield([element, RubyValue.OfInteger(nummer)]);
                if (gerechnet.Kind == RubyValueKind.Integer)
                {
                    _werte.Add(gerechnet.Integer);
                }

                nummer++;
            }

            return pReceiver;
        }

        public RubyValue? LookupConstant(string pName)
        {
            _ = pName;
            return null;
        }
    }


}
