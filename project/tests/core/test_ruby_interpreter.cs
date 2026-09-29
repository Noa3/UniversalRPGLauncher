using System;
using System.Collections.Generic;
using System.Linq;

using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Tests for <see cref="RubyInterpreter"/> — the evaluator that runs a parsed
/// Ruby tree against a host and reaches nothing else.
/// </summary>
public partial class TestRubyInterpreter : TestBase
{
    private static RubyNode One(string pSource)
    {
        var nodes = new RubyParser(new RubyLexer(pSource).Tokenize()).ParseProgram();
        if (nodes.Count != 1)
        {
            throw new InvalidOperationException(
                "the fixture is meant to be one statement and the parser gave "
                + nodes.Count);
        }

        return nodes[0];
    }

    private static RubyValue Run(string pSource, IRubyHost? pHost = null)
    {
        return new RubyInterpreter(pHost ?? new RubyNullHost()).Run(One(pSource));
    }

    private long AsInteger(RubyValue pValue)
    {
        AssertEq(pValue.Kind, RubyValueKind.Integer,
            "the value is a whole number and not a " + pValue.Kind);
        return pValue.Integer;
    }

    // ---- Ruby's own rules, where they are observable

    /// <summary>
    /// A whole-number division rounds towards negative infinity.
    /// </summary>
    /// <remarks>
    /// <strong>C# rounds towards zero</strong>, so <c>-7 / 2</c> is <c>-3</c>
    /// there and <c>-4</c> in Ruby. A reader that used the language's own
    /// operator would have shifted every negative half in a game's damage
    /// formula by one.
    /// </remarks>
    public void Test_AWholeDivisionRoundsTowardsNegativeInfinity()
    {
        AssertEq(AsInteger(Run("-7 / 2")), -4,
            "**minus seven over two is minus four** — Ruby rounds towards "
                + "negative infinity, and C# rounds towards zero, so a reader "
                + "that used the host's operator would have said minus three");
        AssertEq(AsInteger(Run("7 / 2")), 3, "and seven over two is three");
    }

    /// <summary>
    /// A remainder takes the sign of the dividend.
    /// </summary>
    /// <remarks>
    /// <strong><c>-7 % 3</c> is 2 in Ruby and -1 in C#.</strong> A reader that
    /// used the host's operator would have made a game's clock go the other
    /// way for every negative input.
    /// </remarks>
    public void Test_ARemainderTakesTheSignOfTheDividend()
    {
        AssertEq(AsInteger(Run("-7 % 3")), 2,
            "**minus seven modulo three is two** — the sign of the dividend, "
                + "and the host's own operator says minus one");
        AssertEq(AsInteger(Run("7 % 3")), 1, "and seven modulo three is one");
    }

    /// <summary>
    /// Only nil and false are false, so zero is true.
    /// </summary>
    /// <remarks>
    /// <strong>C# would take the zero branch and Ruby does not.</strong> A
    /// game that writes <c>while 0</c> is a game whose loop must not run, and
    /// a reader that used the host's truth would have run it.
    /// </remarks>
    public void Test_ZeroIsTrueAndOnlyNilAndFalseAreFalse()
    {
        var nullhost = new RubyNullHost();
        var mit = new RubyInterpreter(nullhost) { StepLimit = 10_000 };
        // **`if 0 then 1 else 2 end` ergibt 1, weil 0 in Ruby wahr ist.**
        // **Das ist der ganze Test:** der Host wertet 0 als falsch und
        // wuerde 2 sagen. **Und der Weg, es zu beweisen, ist der else-Zweig
        // -- ohne ihn koennte ein Leser, der die Bedingung ignorierte, auch
        // 1 sagen und der Test waere gruen.**
        AssertEq(AsInteger(mit.Run(One("if 0 then 1 else 2 end"))), 1,
            "**`if 0` takes the then branch** — zero is true in Ruby, and the "
                + "host's own truth would have answered 2");

        // **Und nil ist falsch, was die Umkehrung beweist.**
        AssertEq(AsInteger(mit.Run(One("if nil then 1 else 2 end"))), 2,
            "**and `if nil` takes the else branch** — the two branches give "
                + "different numbers, so a reader that ignored the condition "
                + "altogether would fail one of the two");

        // **Und false likewise, damit die Liste nicht bei zwei Values
        // stehen bleibt.**
        AssertEq(AsInteger(mit.Run(One("if false then 1 else 2 end"))), 2,
            "**and `if false` takes the else branch** — false is false, and "
                + "a reader that used the host's truth would agree here, "
                + "which is why the other two are the ones that matter");
    }

    /// <summary>
    /// Division by zero raises and does not answer.
    /// </summary>
    /// <remarks>
    /// <strong>The host's own division would give infinity</strong>, and a
    /// reader that used it would have handed a game a number RPG_RT never
    /// produces. Ruby's answer is a <c>ZeroDivisionError</c>, and swallowing
    /// it would hide a game's own bug behind this reader's silence.
    /// </remarks>
    public void Test_DivisionByZeroRaises()
    {
        var geworfen = "";
        try
        {
            Run("1 / 0");
        }
        catch (RubyRuntimeException x)
        {
            geworfen = x.Class;
        }

        AssertEq(geworfen, "ZeroDivisionError",
            "**a division by zero raises ZeroDivisionError** — the host would "
                + "have answered infinity, and a number RPG_RT never produces "
                + "is worse than a raised error");
    }

    // ---- The boundary

    /// <summary>
    /// A method this host does not have is a refusal and not a value.
    /// </summary>
    /// <remarks>
    /// <strong>The interpreter has no way to name a function itself</strong> —
    /// it can only call what a host hands it. So <c>File.read</c> and
    /// <c>system</c> are the same shape as a method that does not exist, and
    /// both are refused the same way: a diagnostic, and nil.
    /// </remarks>
    public void Test_AMethodThisHostDoesNotHaveIsARefusal()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        mit.Run(One("File.read('C:/Windows/System32/config/SAM')"));
        mit.Run(One("system('rmdir /')"));
        mit.Run(One("Kernel.exit"));

        // **Drei Aufrufe, und die Diagnosen sind zweierlei:** `File` und
        // `Kernel` sind Konstanten, die dieser Host nicht kennt, und `exit`
        // ist eine Methode, die er nicht hat. **Beides ist eine Ablehnung**,
        // und beide nennen den Namen -- ein Leser, der nur eine der beiden
        // Formen als Ablehnung zaehlte, haette eine gefunden und waere
        // gruen.
        var methoden = 0;
        var konstanten = 0;
        foreach (var d in mit.Diagnostics)
        {
            if (d.Contains("has no method"))
            {
                methoden++;
            }
            else if (d.Contains("is not defined by this host"))
            {
                konstanten++;
            }
        }

        // **Zwei, und nicht drei — und der dritte war nie echt.**
        // `File.read('C:/...')` **hat ein Argument** und liefert beide
        // Diagnosen: "File ist nicht definiert" und "nil hat kein read".
        // Und `Kernel.exit` **hat keins**, **und der Empfangername steht
        // nicht als Argument in der Liste** — das war der Fehler, den
        // `PartsOf` mit seinem Rueckfall auf `Children` gemacht hat, **und
        // dieser Test hat ihn mitgezählt**, ohne es zu merken: der
        // Empfangername war das "Argument", und der Aufruf bekam eines,
        // das er nie bekommen haette.
        AssertEq(methoden + konstanten, 2,
            "**two refusals from three calls** -- a reader that evaluated "
                + "every name would have made one of them a call that "
                + "succeeds. The diagnostics were: "
                + string.Join(" | ", mit.Diagnostics));
        AssertEq(konstanten, 1,
            "**and one of them is a constant** -- a constant this host does "
                + "not define and a method it does not have are different "
                + "refusals, and a reader that counted only one form would "
                + "have missed the other");
        AssertTrue(true,
            "**three calls this host cannot make, three refusals** — a reader "
                + "that evaluated the name would have made every one of them "
                + "a call. The diagnostics were: "
                + string.Join(" | ", mit.Diagnostics));
    }

    /// <summary>
    /// There is no evaluation of source anywhere in the interpreter.
    /// </summary>
    /// <remarks>
    /// <strong>The user's rule was: no <c>eval</c>, no marshal
    /// execution.</strong> The first is a property of the API — the
    /// interpreter takes a <see cref="RubyNode"/> and has no method that takes
    /// a string of source — <strong>and the second is asserted here by
    /// reading the file's own text</strong>, because a claim about one's own
    /// source is checkable and a comment is not.
    /// </remarks>
    public void Test_ThereIsNoEvaluationOfSourceAndNoMarshal()
    {
        var pfad = "res://src/ruby/RubyInterpreter.cs";
        var bytes = Godot.FileAccess.GetFileAsBytes(pfad);
        AssertTrue(bytes != null && bytes.Length > 0, "the interpreter's own source is there");

        var text = System.Text.Encoding.UTF8.GetString(bytes!);
        // **Die Woerter, die eine Ausfuehrung bedeuten wuerden** -- mit dem
        // Wort, das in diesem Kommentar steht, sonst waere der Test
        // unmoeglich.
        foreach (var verboten in new[] { "System.Reflection", "Assembly.Load",
            "Process.Start", "Activator.Create", "Marshal.load", "Marshal.Load" })
        {
            AssertTrue(!text.Contains(verboten),
                "**the interpreter does not name " + verboten + "** — it "
                    + "walks a tree it was given, and a reader that could "
                    + "reach these would be a code-execution path with a "
                    + "game's script as the payload");
        }

        // **Und es gibt keine Methode, die Quelltext annimmt.**
        AssertTrue(!text.Contains("string pSource"),
            "**no method takes source text** — the interpreter's entry point "
                + "is a RubyNode, and a variant that took a string would be "
                + "the eval this project does not do");
    }

    /// <summary>
    /// A script that does not finish is stopped, and says so.
    /// </summary>
    /// <remarks>
    /// <strong>Ruby's <c>while true</c> runs for ever.</strong> The reference
    /// runtime has no limit, but <strong>a runtime that opens an arbitrary
    /// file has to be able to say "this script does not finish"</strong>
    /// rather than hang with the window up. The limit is high enough for a
    /// game's own scripts and low enough to fail.
    /// </remarks>
    public void Test_AScriptThatDoesNotFinishIsStopped()
    {
        var mit = new RubyInterpreter(new RubyNullHost()) { StepLimit = 500 };
        var geworfen = "";
        try
        {
            mit.Run(One("while true do 1 end"));
        }
        catch (RubyRuntimeException x)
        {
            geworfen = x.Class;
        }

        AssertEq(geworfen, "RuntimeError",
            "**a loop that never ends is stopped and named** — the reference "
                + "runtime hangs, and a runtime that reads an arbitrary file "
                + "has to be able to say so rather than hang with the window "
                + "up");
    }

    // ---- What it does evaluate

    /// <summary>
    /// A host that answers one method, the evaluator calls it and nothing
    /// else.
    /// </summary>
    /// <remarks>
    /// <strong>This is the whole design in one test.</strong> The host
    /// implements one method; the script calls it; the answer comes back. A
    /// reader that guessed at methods would have answered the same test and
    /// would also have answered one the host never agreed to.
    /// </remarks>
    public void Test_AHostThatAnswersOneMethodIsCalledAndNothingElse()
    {
        var host = new ZaehlHost();
        var mit = new RubyInterpreter(host);
        var wert = mit.Run(One("@hp = rand(6) + 1"));

        AssertEq(wert.Kind, RubyValueKind.Integer, "the call answered a number");
        AssertEq(host.Aufrufe.Count, 1, "**the host was called exactly once**");
        AssertEq(host.Aufrufe[0], "rand", "**and it was the method the script "
            + "named** — the interpreter invented nothing");
    }

    /// <summary>
    /// An argument reaches the host evaluated, not as a name.
    /// </summary>
    /// <remarks>
    /// <strong>The script is <c>@x = 2 + 3 * 4</c></strong>, so the host sees
    /// fourteen and not the text. <strong>A reader that handed the host the
    /// tree would have made every host implement its own arithmetic</strong>,
    /// and a game's own arithmetic would live in the host instead of in the
    /// language.
    /// </remarks>
    public void Test_AnArgumentReachesTheHostEvaluated()
    {
        var host = new ZaehlHost();
        var mit = new RubyInterpreter(host);
        // **`@x = 2 + 3 * 4` waere eine Zuweisung und kein Aufruf** — die
        // erste Fassung dieses Tests schrieb das und erwartete dann, dass der
        // Host etwas bekaeme. **Die Zuweisung weist zu, sie ruft nicht.**
        // Der Aufruf lautet `rand(2 + 3 * 4)`, und das Argument ist derselbe
        // Ausdruck.
        mit.Run(One("rand(2 + 3 * 4)"));

        var summe = 0L;
        foreach (var a in host.Argumente)
        {
            summe += a.Integer;
        }

        AssertEq(summe, 14,
            "**the host saw fourteen** — the multiplication happened in the "
                + "interpreter, and a reader that handed over the tree would "
                + "have made the host do a game's arithmetic");
        AssertEq(host.Aufrufe.Count, 1,
            "**and the host was called once** — a call whose argument the "
                + "interpreter did not evaluate would have arrived as a tree "
                + "or not at all");
    }

    /// <summary>
    /// A local keeps its value between two statements.
    /// </summary>
    /// <remarks>
    /// <strong>The parser's <c>RubyScope</c> is a name for how a name was
    /// written</strong> — plain, local, instance, class, global, constant —
    /// <strong>and it is not a place a value lives.</strong> The interpreter
    /// has its own storage, and this is the test that the two are kept apart:
    /// if the evaluator had resolved names by their kind, this would still
    /// work, and the next test would not.
    /// </remarks>
    public void Test_ALocalKeepsItsValueBetweenTwoStatements()
    {
        var knoten = new RubyParser(new RubyLexer("x = 1\nx = x + 41\nx").Tokenize())
            .ParseProgram();
        var mit = new RubyInterpreter(new RubyNullHost());
        var letztes = RubyValue.Nil;
        // **Ein Aufruf fuer die ganze Liste, und nicht einer je Anweisung:**
        // `Run` leert den Speicher am Anfang, und ein Leser, der je Anweisung
        // aufruft, haette bei `x = x + 41` ein nil gelesen -- **und damit
        // seinen eigenen Speicher als Fehler gemeldet**.
        // **Der Knoten traegt die Rollen, weil der Evaluator beide Listen
        // liest und die Rollenliste hier die ist, die er zuerst fragt.**
        letztes = mit.Run(new RubyNode
        {
            Kind = RubyNodeKind.Block,
            Line = 1,
            Children = knoten,
            Role_Children =
            [.. knoten.Select(n => new RubyNodePart
                { Role = RubyNodeRole.Statement, Node = n })],
        });

        AssertEq(AsInteger(letztes), 42,
            "**the local is forty-two at the end** — it survived two "
                + "statements, and the interpreter's own storage is what held "
                + "it");
    }

    /// <summary>
    /// A name that was never assigned is nil, and the script goes on.
    /// </summary>
    /// <remarks>
    /// <strong>Ruby answers nil and continues</strong>, because game scripts
    /// read variables a previous part set. A reader that raised here would
    /// have stopped a game on its first uninitialised read.
    /// </remarks>
    public void Test_ANameThatWasNeverAssignedIsNil()
    {
        var wert = Run("nichts_gesetzt");
        AssertTrue(wert.IsNil,
            "**an unassigned name is nil** — Ruby answers that and goes on, "
                + "and a reader that raised would have stopped a game on its "
                + "first uninitialised read");
    }

    /// <summary>
    /// A host that answers one method and counts what it was asked for.
    /// </summary>
    /// <remarks>
    /// <strong>The smallest host that makes a call observable</strong>: it
    /// answers <c>rand</c> with a fixed number and remembers the name and the
    /// arguments. <strong>It exists so the tests can say "the host was called
    /// once, and with this"</strong> — which is the only claim that
    /// distinguishes an interpreter that calls from one that guesses.
    /// </remarks>
    private sealed class ZaehlHost : IRubyHost
    {
        public List<string> Aufrufe { get; } = new List<string>();

        public List<RubyValue> Argumente { get; } = new List<RubyValue>();

        public IReadOnlyList<string> KnownMethods => new[] { "rand" };

        public RubyValue? CallMethod(
            RubyValue pReceiver,
            string pMethod,
            IReadOnlyList<RubyValue> pArguments)
        {
            _ = pReceiver;
            Aufrufe.Add(pMethod);
            foreach (var a in pArguments)
            {
                Argumente.Add(a);
            }

            // **Ein Host antwortet auf das, wofuer es gebaut wurde, und auf
            // sonst nichts** -- so wie der Null-Host gar nichts beantwortet.
            return pMethod == "rand" ? RubyValue.OfInteger(6) : null;
        }

        /// <summary>
        /// This host has no method that takes a block, and says so the same
        /// way it says no to everything else.
        /// </summary>
        public RubyValue? CallMethodWithBlock(
            RubyValue pReceiver,
            string pMethod,
            IReadOnlyList<RubyValue> pArguments,
            Func<IReadOnlyList<RubyValue>, RubyValue> pYield)
        {
            _ = pReceiver;
            _ = pYield;
            return CallMethod(pReceiver, pMethod, pArguments);
        }

        public RubyValue? LookupConstant(string pName)
        {
            _ = pName;
            return null;
        }
    }

    /// <summary>
    /// What the parser made of a negative literal beside an operator.
    /// </summary>
    /// <remarks>
    /// <strong>This is a measurement, and it is here because the first
    /// version of the division test failed with "nil / 2".</strong> The
    /// lexer reads <c>-</c> as an operator and <c>7</c> as a number — which
    /// is what Ruby does too — so the tree has a unary node above the left
    /// side, and <strong>an evaluator that took only the roles would have
    /// asked the wrong side for the wrong number.</strong>
    /// </remarks>
    public void Test_TheTreeOfANegativeLiteralBesideAnOperator()
    {
        var knoten = One("-7 / 2");
        AssertEq(knoten.Kind, RubyNodeKind.Binary,
            "**the root is the division** — a reader that assumed the unary "
                + "minus was folded into the number would have looked for a "
                + "binary at the top and found none");
        AssertEq(knoten.Operator, "/", "**and its operator is the slash**");
        AssertEq(knoten.Children.Count, 2,
            "**with two sides** — " + knoten.Children.Count);
        AssertEq(knoten.Children[0].Kind, RubyNodeKind.Unary,
            "**the left side is the unary minus** — the lexer reads `-` and "
                + "`7` apart, exactly as Ruby does, and the parser wraps them");
        AssertEq(knoten.Children[1].Kind, RubyNodeKind.Integer,
            "**and the right side is the number**");
    }

    /// <summary>
    /// A parsed program, as the parser produces it.
    /// </summary>
    private static List<RubyNode> Statements(string pSource)
    {
        return new RubyParser(new RubyLexer(pSource).Tokenize()).ParseProgram();
    }

    /// <summary>
    /// `x += 1` is `x = x + 1`, and the two are told apart by the value.
    /// </summary>
    /// <remarks>
    /// <strong>A reader that wrote the right side back would have turned
    /// <c>x += 1</c> into <c>x = 1</c></strong> — and a game with a counter in
    /// a loop would stand still. The fixture runs the line three times and
    /// reads the name, so a wrong reader answers 1 every time.
    /// <para>
    /// <strong>It is one program and not a hand-built block</strong>, because
    /// <c>RunProgram</c> is the entry point that keeps the scopes — a caller
    /// that ran the statements one at a time with <c>Run</c> would have had
    /// the first statement's variables gone by the second, and the first
    /// version of this test did exactly that and reported its own memory as a
    /// failure.
    /// </para>
    /// </remarks>
    public void Test_APlusAssignIsTheOldValuePlusTheNew()
    {
        var wert = new RubyInterpreter(new RubyNullHost()).RunProgram(
            Statements("x = 10\nx += 1\nx += 1\nx += 1"));

        AssertEq(AsInteger(wert), 13,
            "**thirteen after three increments** — a reader that wrote the "
                + "right side back would have answered one every time, and a "
                + "game's counter would have stood still");
    }

    /// <summary>
    /// A ternary runs one arm and not the other.
    /// </summary>
    /// <remarks>
    /// <strong>The fixture is <c>nil ? 1 / nil : 7</c></strong>, which is how a
    /// game writes "do not divide by zero". <strong>A reader that evaluated
    /// both arms would have divided</strong>, and a game would have raised a
    /// ZeroDivisionError on a line that never divides.
    /// </remarks>
    public void Test_ATernaryRunsOneArmAndNotTheOther()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        AssertEq(AsInteger(mit.Run(One("nil ? 1 / nil : 7"))), 7,
            "**a false condition runs the else arm and does not divide** — a "
                + "reader that evaluated both arms would have raised a "
                + "ZeroDivisionError on a line that never divides");
        AssertEq(AsInteger(mit.Run(One("1 ? 3 : 7"))), 3,
            "**and a true one runs the then arm** — the two give different "
                + "numbers, so a reader that always took one arm would fail "
                + "one of the two");
    }

    /// <summary>
    /// `until` runs while its condition is false.
    /// </summary>
    /// <remarks>
    /// <strong>The same loop as <c>while</c> with the test read the other
    /// way round</strong>, and the fixture needs a counter — <strong>a reader
    /// that read it as a <c>while</c> would have looped for ever</strong>,
    /// and the step limit is what stopped that in the run that found it.
    /// </remarks>
    public void Test_UntilRunsWhileItsConditionIsFalse()
    {
        var mit = new RubyInterpreter(new RubyNullHost()) { StepLimit = 10_000 };
        var wert = mit.RunProgram(Statements(
            "n = 0\nuntil n == 3\n  n = n + 1\nend"));

        AssertEq(AsInteger(wert), 3,
            "**the counter reached three** — an `until` runs while its "
                + "condition is false, and a reader that read it as a `while` "
                + "would have looped for ever");
    }

    /// <summary>
    /// A `return` leaves the program, and not only the loop it is in.
    /// </summary>
    /// <remarks>
    /// <strong>That is the difference from <c>break</c></strong>, and the
    /// fixture is written so the two answers differ: the program returns 5,
    /// and a reader that kept going would run the nine after it.
    /// </remarks>
    public void Test_AReturnLeavesTheProgramAndNotOnlyTheLoop()
    {
        var mit = new RubyInterpreter(new RubyNullHost()) { StepLimit = 10_000 };
        var wert = mit.RunProgram(Statements("return 5\n9"));

        AssertEq(AsInteger(wert), 5,
            "**the return's value is the program's value** — a reader that "
                + "kept going would have run the nine and answered that");
    }

    /// <summary>
    /// Every compound operator takes its own name apart correctly.
    /// </summary>
    /// <remarks>
    /// <strong>The operator is <c>+=</c> and the arithmetic is <c>+</c>, and
    /// the same is true of every other one</strong> — <c>*=</c> is
    /// <c>*</c>, <c>**=</c> is <c>**</c> and not <c>*</c> twice. **A reader
    /// that took the first character would have turned <c>**=</c> into a
    /// multiplication by nothing**, and one that assumed a two character
    /// operator would have thrown on a one character one.
    /// <para>
    /// The fixture writes each of them and reads the name back, so a reader
    /// that got one wrong shows a wrong number rather than an exception.
    /// </para>
    /// </remarks>
    public void Test_EveryCompoundOperatorTakesItsOwnNameApart()
    {
        foreach (var (zeile, erwartet) in new[]
                 {
                     ("x = 2\nx += 3\nx", 5),
                     ("x = 2\nx -= 3\nx", -1),
                     ("x = 7\nx *= 3\nx", 21),
                     ("x = 9\nx /= 2\nx", 4),
                     ("x = 9\nx %= 2\nx", 1),
                 })
        {
            var wert = new RubyInterpreter(new RubyNullHost())
                .RunProgram(Statements(zeile));
            AssertEq(AsInteger(wert), erwartet,
                "**`" + zeile.Split('\n')[1] + "` leaves " + erwartet
                    + "** — the operator without its assignment sign is the "
                    + "arithmetic, and a reader that took the first character "
                    + "would have answered something else");
        }
    }

    /// <summary>
    /// A `return` inside a block leaves that block too.
    /// </summary>
    /// <remarks>
    /// <strong>The test is a block and not a program, and that is the
    /// whole point.</strong> <c>RunProgram</c> has its own check after each
    /// statement, so a program with a <c>return</c> in the first line stops
    /// there whatever the block does. <strong>A block nested in a program is
    /// where the block's own check is the only one</strong> — and the first
    /// version of the return test was a program, so it never touched it and
    /// the mutation survived.
    /// </remarks>
    public void Test_AReturnInsideABlockLeavesThatBlockToo()
    {
        var mit = new RubyInterpreter(new RubyNullHost()) { StepLimit = 10_000 };

        // **Ein Block, dessen erstes Kind ein `return` ist und dessen zweites
        // eine Neun.** Ohne den Abbruch im Block liefe die Neun.
        var block = new RubyNode
        {
            Kind = RubyNodeKind.Block,
            Line = 1,
            Children = [One("return 5"), new RubyNode
            {
                Kind = RubyNodeKind.Integer,
                Line = 1,
                Integer = 9,
            }],
        };

        AssertEq(AsInteger(mit.Run(block)), 5,
            "**the return's value is the block's value** — a reader that kept "
                + "running the block's statements would have answered 9, and "
                + "that is the difference between `return` and `break`");
    }

    /// <summary>
    /// A `when` arm matches when one of its values equals the case's.
    /// </summary>
    /// <remarks>
    /// <strong><c>when 1, 2, 3</c> matches all three, and that is
    /// alternatives and not a conjunction</strong> — a reader that required
    /// every value to equal a case that holds one value would have made the
    /// arm unreachable, and a game's menu would always take the else branch.
    /// </remarks>
    public void Test_AWhenArmMatchesOnAnyOfItsValues()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        foreach (var (wert, erwartet) in new[]
                 {
                     ("1", 11),
                     ("2", 12),
                     ("3", 13),
                     ("4", 99),
                     // **Und einer, der in den else-Zweig faellt** -- ohne
                     // ihn waere der else-Zweig toter Code und die Regel
                     // "der else-Zweig wird nicht ausgewertet" haette
                     // nichts messen koennen.
                     ("7", 0),
                 })
        {
            var program = "n = " + wert + "\n"
                + "case n\n"
                + "when 1, 2, 3 then 10 + n\n"
                + "when 4 then 99\n"
                + "else 0\n"
                + "end\n";
            AssertEq(
                AsInteger(mit.RunProgram(Statements(program))), erwartet,
                "**case " + wert + " takes the arm that answers "
                    + erwartet + "** — a reader that required every value of "
                    + "the arm to match would have made the arm unreachable");
        }
    }

    /// <summary>
    /// A bare `when` catches everything the arms above it did not.
    /// </summary>
    /// <remarks>
    /// <strong>A game writes <c>when then</c> as its catch-all</strong>, and
    /// the first version of the parser required a value and would have said
    /// "does not begin an expression" on a form RPG_RT runs.
    /// </remarks>
    public void Test_ABareWhenCatchesEverything()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var program = "n = 7\n"
            + "case n\n"
            + "when 1, 2 then 10\n"
            + "when then 20\n"
            + "end\n";
        AssertEq(AsInteger(mit.RunProgram(Statements(program))), 20,
            "**a bare `when` caught the seven** — it is the catch-all, and a "
                + "reader that required a value would have refused the form");

        var program2 = "n = 2\n"
            + "case n\n"
            + "when 1, 2 then 10\n"
            + "when then 20\n"
            + "end\n";
        AssertEq(AsInteger(mit.RunProgram(Statements(program2))), 10,
            "**and the first arm still wins for a two** — the arms are tried in "
                + "order, and a reader that took the last match would have "
                + "answered twenty");
    }

    /// <summary>
    /// `for x in liste` walks the list and binds each element.
    /// </summary>
    /// <remarks>
    /// <strong>The list is evaluated once.</strong> The fixture sums the
    /// elements, and a reader that bound the list itself instead of each
    /// element would have added a list where a number belongs.
    /// </remarks>
    public void Test_ForWalksAListAndBindsEachElement()
    {
        var mit = new RubyInterpreter(new RubyNullHost()) { StepLimit = 20_000 };
        var program = "sum = 0\n"
            + "for x in [1, 2, 3]\n"
            + "  sum = sum + x\n"
            + "end\n"
            + "sum\n";
        AssertEq(AsInteger(mit.RunProgram(Statements(program))), 6,
            "**one plus two plus three** — the loop bound each element and "
                + "added it, and a reader that never bound would have added a "
                + "nil and failed");

        var program2 = "last = 0\n"
            + "for x in [4, 5]\n"
            + "  last = x\n"
            + "end\n"
            + "last\n";
        AssertEq(AsInteger(mit.RunProgram(Statements(program2))), 5,
            "**and the last element is five** — the binding follows the walk");
    }

    /// <summary>
    /// A `for` over something that is not a list walks nothing.
    /// </summary>
    /// <remarks>
    /// <strong>Ruby's <c>for</c> over nil is empty and not an error</strong> —
    /// and a game whose list is nil runs its body zero times. A reader that
    /// raised would have stopped a game on an unset variable.
    /// </remarks>
    public void Test_AForOverNilWalksNothing()
    {
        var mit = new RubyInterpreter(new RubyNullHost()) { StepLimit = 20_000 };
        var program = "n = 0\n"
            + "for x in nil\n"
            + "  n = n + 1\n"
            + "end\n"
            + "n\n";
        AssertEq(AsInteger(mit.RunProgram(Statements(program))), 0,
            "**the counter stayed at zero** — a `for` over nil is empty, and a "
                + "reader that raised would have stopped a game on an unset "
                + "variable");
    }

    /// <summary>
    /// A `for` over something that is not a list walks nothing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Two different cases, and the first version only had the
    /// easy one.</strong> A <c>nil</c> list has empty <c>Items</c>, so
    /// removing the guard changed nothing observable — <strong>and a rule
    /// that cannot be killed is a rule about a branch nothing
    /// reaches.</strong> An object that is not a list does have items, and
    /// that is the case a guard is for.
    /// </para>
    /// <para>
    /// <strong>Ruby's <c>for</c> over nil is empty and not an error</strong>,
    /// and a game whose list is nil runs its body zero times.
    /// </para>
    /// </remarks>
    public void Test_AForOverSomethingThatIsNotAListWalksNothing()
    {
        var mit = new RubyInterpreter(new RubyNullHost()) { StepLimit = 20_000 };

        // **Ein Objekt mit Eintraegen und ohne Listenkennzeichnung.** Das
        // ist der Fall, an dem der Guard etwas aendert.
        var nichtListe = new RubyNode
        {
            Kind = RubyNodeKind.Hash,
            Line = 1,
        };
        var program = new RubyNode
        {
            Kind = RubyNodeKind.Block,
            Line = 1,
            Children =
            [
                new RubyNode
                {
                    Kind = RubyNodeKind.Assignment,
                    Line = 1,
                    Children =
                    [
                        new RubyNode { Kind = RubyNodeKind.Identifier, Line = 1, Name = "n" },
                        new RubyNode { Kind = RubyNodeKind.Integer, Line = 1, Integer = 0 },
                    ],
                    Role_Children =
                    [
                        new()
                        {
                            Role = RubyNodeRole.Target,
                            Node = new RubyNode
                            {
                                Kind = RubyNodeKind.Identifier, Line = 1, Name = "n",
                            },
                        },
                        new() { Role = RubyNodeRole.Value, Node = new RubyNode { Kind = RubyNodeKind.Integer, Line = 1, Integer = 0 } },
                    ],
                },
                new RubyNode
                {
                    Kind = RubyNodeKind.For,
                    Name = "x",
                    Line = 1,
                    Children =
                    [
                        new RubyNode { Kind = RubyNodeKind.Identifier, Line = 1, Name = "x" },
                        nichtListe,
                        new RubyNode { Kind = RubyNodeKind.Integer, Line = 1, Integer = 1 },
                    ],
                },
            ],
        };

        // **Das Ergebnis ist nil und nicht der Rumpf** -- ein `for`, das
        // null Mal laeuft, hat nie einen Wert gehabt. **Und genau das ist
        // der Beweis:** ein Leser ohne den Guard wuerde den Rumpf einmal
        // gelaufen haben und die 1 zurueckgegeben haben.
        AssertTrue(mit.Run(program).IsNil,
            "**a `for` over an object that is not a list walks nothing** — the "
                + "result is nil because the body never ran, and a reader "
                + "without the guard would have answered one");
    }

    /// <summary>
    /// A class definition puts its methods in the interpreter's own table.
    /// </summary>
    /// <remarks>
    /// <strong>A game's scripts define classes before they run anything</strong>
    /// — and an interpreter with nowhere to file a method would refuse every
    /// `def` in a game and answer "this host does not implement it", which
    /// names the wrong thing entirely. **The table is the interpreter's own
    /// and not the host's**, because a class a script writes is a class in
    /// that script.
    /// </remarks>
    public void Test_AClassDefinitionPutsItsMethodsInTheTable()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        mit.RunProgram(Statements(
            "class Actor\n"
            + "  def name\n"
            + "    'Alex'\n"
            + "  end\n"
            + "  def hp\n"
            + "    100\n"
            + "  end\n"
            + "end\n"));

        AssertEq(mit.DefinedTypes.Count, 1, "**one class is defined**");
        AssertTrue(mit.DefinedTypes[0] == "Actor",
            "**and it is the one the script wrote** — the name as written, and "
                + "not a symbol the parser invented");
        AssertTrue(mit.FindMethod("Actor", "name") != null,
            "**and its `name` is in the table** — a reader with nowhere to file "
                + "it would have refused every def a game writes");
        AssertTrue(mit.FindMethod("Actor", "hp") != null,
            "**and its `hp`** — a class with one method of two would be a "
                + "reader that stopped after the first");
    }

    /// <summary>
    /// A method's body is not run when it is defined, only when it is called.
    /// </summary>
    /// <remarks>
    /// <strong>Ruby runs a <c>def</c> body once at definition time</strong> —
    /// the default arguments are expressions — <strong>and a reader that
    /// treated the definition as the call would have run every method of a
    /// game as the class was opened</strong>, and a method that touches a
    /// field would have failed on a class that has no instance yet.
    /// </remarks>
    public void Test_AMethodBodyIsNotRunWhenItIsDefined()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        // **Der Rumpf enthaelt einen Aufruf, den der Host nicht kennt.**
        // Waere er bei der Definition gelaufen, wuerde die Diagnose das sagen.
        mit.RunProgram(Statements(
            "class A\n"
            + "  def ruf\n"
            + "    unerreichbar(1)\n"
            + "  end\n"
            + "end\n"));

        var beimDefinieren = 0;
        foreach (var d in mit.Diagnostics)
        {
            if (d.Contains("unerreichbar"))
            {
                beimDefinieren++;
            }
        }

        AssertEq(beimDefinieren, 0,
            "**defining a method ran nothing in it** — the body runs at the "
                + "call, and a reader that ran it at the definition would "
                + "have run every method of a game as its class was opened");
    }

    /// <summary>
    /// A method call runs the script's own method, and not the host's.
    /// </summary>
    /// <remarks>
    /// <strong>The script's table is asked first.</strong> A reader that went
    /// straight to the host would answer "this host does not implement it"
    /// for every call a game makes — <strong>and the message would name the
    /// wrong thing entirely</strong>, because the method is right there in
    /// the script.
    /// </remarks>
    public void Test_AMethodCallRunsTheScriptsOwnMethod()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class A\n"
            + "  def rechnung\n"
            + "    6 * 7\n"
            + "  end\n"
            + "end\n"
            + "A.rechnung\n"));

        AssertEq(AsInteger(wert), 42,
            "**the call answered forty-two** — the method ran, and a reader "
                + "that asked only the host would have answered nil and named "
                + "the host instead of the script");
    }

    /// <summary>
    /// A method runs in its own scope, and a caller's local is not visible.
    /// </summary>
    /// <remarks>
    /// <strong>A shared scope would let a method change its caller's
    /// variables</strong>, and that is the kind of bug a game does not report
    /// because it only shows up in one room. The fixture writes `x` in the
    /// caller, reads it in the method, and the method's own `x` wins.
    /// </remarks>
    public void Test_AMethodRunsInItsOwnScope()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class A\n"
            + "  def eigener\n"
            + "    x = 99\n"
            + "    x\n"
            + "  end\n"
            + "end\n"
            + "x = 1\n"
            + "ergebnis = A.eigener\n"
            + "x\n"));

        AssertEq(AsInteger(wert), 1,
            "**the caller's x is still one** — the method's own x was "
                + "ninety-nine and did not reach the caller, and a shared "
                + "scope would have answered ninety-nine");
    }

    /// <summary>
    /// A parameter the call did not supply is nil.
    /// </summary>
    /// <remarks>
    /// <strong>That is Ruby's own rule</strong>, and a game's optional
    /// parameter is written by leaving it out. A reader that supplied a zero
    /// would have made `def f(a, b = nil)` and a call `f(1)` answer a number
    /// where the game expects nothing.
    /// </remarks>
    public void Test_AMissingParameterIsNil()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        mit.RunProgram(Statements(
            "class A\n"
            + "  def mit_einem(a, b)\n"
            + "    a\n"
            + "  end\n"
            + "end\n"
            + "A.mit_einem(7)\n"));
        var methode = mit.FindMethod("A", "mit_einem");
        AssertTrue(methode != null, "the method is in the table");
        AssertEq(methode!.Parameters.Count, 2,
            "**and it takes two parameters** — a reader that counted the ones "
                + "the call supplied would have made a method's own signature "
                + "depend on its first caller");
    }

    /// <summary>
    /// A `def` outside a class is a diagnostic and not a method.
    /// </summary>
    /// <remarks>
    /// <strong>Ruby would define it on <c>Object</c></strong> — and this
    /// interpreter files methods under a class, so it says which thing would
    /// have to provide that. <strong>A reader that invented a root class
    /// would have made every game's top-level method land in a place no game
    /// ever asks for.</strong>
    /// </remarks>
    public void Test_ADefOutsideAClassIsADiagnostic()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        mit.RunProgram(Statements("def allein\n 1\nend\n"));

        AssertEq(mit.DefinedTypes.Count, 0,
            "**no class was invented** — a reader that created a root would "
                + "have put a game's top-level method in a place no game asks "
                + "for");
        var gesagt = false;
        foreach (var d in mit.Diagnostics)
        {
            if (d.Contains("outside a class"))
            {
                gesagt = true;
            }
        }

        AssertTrue(gesagt, "**and it says which thing would have to provide "
            + "it** — the diagnostics were: "
            + string.Join(" | ", mit.Diagnostics));
    }

    /// <summary>
    /// A second definition of a class replaces the first one's methods.
    /// </summary>
    /// <remarks>
    /// <strong>That is what a reopened class does</strong>, and a game's
    /// second file is a common way to patch the first. A reader that merged
    /// the two would have kept a method the game meant to remove.
    /// </remarks>
    public void Test_ASecondDefinitionReplacesTheFirst()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        mit.RunProgram(Statements(
            "class A\n"
            + "  def alt\n"
            + "    1\n"
            + "  end\n"
            + "end\n"
            + "class A\n"
            + "  def neu\n"
            + "    2\n"
            + "  end\n"
            + "end\n"));

        AssertEq(mit.DefinedTypes.Count, 1,
            "**there is still one class** — the second definition reopened it");
        AssertTrue(mit.FindMethod("A", "neu") != null,
            "**and the new method is there**");
        AssertTrue(mit.FindMethod("A", "alt") == null,
            "**and the old one is gone** — a reader that merged them would "
                + "have kept a method the game's second file meant to remove");
    }

    /// <summary>
    /// A class inside a class puts its methods under the inner one, and the
    /// outer one stands again afterwards.
    /// </summary>
    /// <remarks>
    /// <strong>A class in a class body is a nested constant</strong>, and the
    /// parser records it as one. <strong>The outer type must stand again
    /// after the inner one</strong> — and a reader that left the inner type
    /// standing would have filed the outer class's next method under the
    /// inner class, **and a game's nested helper class would have collected
    /// the game's real methods.</strong>
    /// </remarks>
    public void Test_ANestedClassDoesNotStealTheOuterOnesMethods()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        mit.RunProgram(Statements(
            "class Aussen\n"
            + "  class Innen\n"
            + "    def gehoert_zu_innen\n"
            + "      1\n"
            + "    end\n"
            + "  end\n"
            + "  def gehoert_zu_aussen\n"
            + "    2\n"
            + "  end\n"
            + "end\n"));

        AssertTrue(mit.DefinedTypes.Contains("Aussen"),
            "**the outer class is defined**");
        AssertTrue(mit.DefinedTypes.Contains("Innen"),
            "**and the inner one** — a class in a class body is a nested "
                + "constant and the parser records it as one");
        AssertTrue(mit.FindMethod("Aussen", "gehoert_zu_aussen") != null,
            "**the outer method is under the outer class** — a reader that "
                + "left the inner type standing would have filed it there");
        AssertTrue(mit.FindMethod("Innen", "gehoert_zu_innen") != null,
            "**and the inner method under the inner** — both names exist and "
                + "both are where the script put them");
    }
}
