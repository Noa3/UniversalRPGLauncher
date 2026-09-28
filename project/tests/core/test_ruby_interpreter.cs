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

        AssertEq(methoden + konstanten, 3,
            "**three calls this host cannot make, three refusals** -- a reader "
                + "that evaluated the name would have made every one of them "
                + "a call. The diagnostics were: "
                + string.Join(" | ", mit.Diagnostics));
        AssertEq(konstanten, 2,
            "**and two of them are constants and one is a method** -- a "
                + "constant this host does not define and a method it does "
                + "not have are different refusals, and a reader that counted "
                + "only the method form would have missed half of them");
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
}
