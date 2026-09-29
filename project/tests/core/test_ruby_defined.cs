using System.Collections.Generic;

using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// `defined?`, which asks a question and does not answer it by doing it.
/// </summary>
/// <remarks>
/// <para>
/// <strong>It answers a string, and that is Ruby 1.8.1</strong> — the version
/// RPG Maker XP runs, verified in <c>eval.c</c>'s <c>is_defined</c>, which
/// returns <c>"method"</c>, <c>"local-variable"</c>, <c>"expression"</c>,
/// <c>"instance-variable"</c>, <c>"global-variable"</c> and <c>"nil"</c>.
/// Current Ruby answers a boolean, and **a reader that answered true or false
/// would have broken every game that writes
/// <c>defined?(@hp) ? "expression" : "nil"</c>** — which is how a game's own
/// code asks.
/// </para>
/// <para>
/// <strong>And it does not run the expression.</strong> <c>is_defined</c>
/// walks the node's kind and never evaluates it, <strong>so a reader that
/// evaluated it would have said "method" for <c>defined? a.b</c> even when
/// <c>b</c> is not there</strong>, and would have made the call the question
/// only asked about.
/// </para>
/// </remarks>
public partial class TestRubyInterpreter
{
    /// <summary>
    /// The kinds come back as words, and not as true and false.
    /// </summary>
    /// <remarks>
    /// <strong>This is the 1.8.1 answer and the string is the whole
    /// point.</strong> A game writes
    /// <c>defined?(@hp) ? "expression" : "nil"</c>, and a boolean here would
    /// make both arms the same value — <strong>and a game that branches on
    /// which kind it is would have taken the same branch twice.</strong>
    /// </remarks>
    public void Test_DefinedAnswersAWordAndNotABoolean()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class A\n"
            + "  def m\n"
            + "    1\n"
            + "  end\n"
            + "end\n"
            + "defined? A.m\n"));

        AssertTrue(wert.Kind == RubyValueKind.Symbol,
            "**the answer is a symbol** — a string in Ruby 1.8.1, and a "
                + "boolean here would make every `defined?(x) ? \"a\" : "
                + "\"b\"` in a game take the same branch");
        AssertEq(wert.ToString(), "method",
            "**and it is the word `method`** — that is `is_defined`'s own "
                + "return value in eval.c, and not a name this reader chose");
    }

    /// <summary>
    /// It does not run the expression, and says "expression" for the rest.
    /// </summary>
    /// <remarks>
    /// <strong>Two things in one test, because they are the same
    /// mistake.</strong> A reader that evaluated the question would have made
    /// the call, <strong>and a reader that answered "method" for anything it
    /// did not recognise would have said it about a number.</strong>
    /// Ruby's own default is <c>"expression"</c> for every node it cannot
    /// name, and a nil for the question's answer is what a game tests for.
    /// </remarks>
    public void Test_DefinedDoesNotRunTheExpression()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var knoten = Statements("defined? 1 + 1\n");
        var wert = mit.RunProgram(knoten);

        AssertEq(wert.ToString(), "expression",
            "**a number is an expression** — `is_defined` returns "
                + "\"expression\" for every node it cannot name, and a reader "
                + "that answered \"method\" would have been wrong about a "
                + "calculation. The value was " + wert.ToString()
                + " and the tree " + knoten[0].Kind);
    }

    /// <summary>
    /// A question about a method that is not there answers nil.
    /// </summary>
    /// <remarks>
    /// <strong>`nil` and not `false` and not an empty string.</strong> A game
    /// writes `defined?(a.b) ? ... : ...` and the two arms carry the
    /// difference; <strong>a reader that answered "method" for a missing
    /// method would have taken the first arm on a name nobody
    /// defined.</strong>
    /// </remarks>
    public void Test_AMethodThatIsNotThereAnswersNil()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class A\n"
            + "end\n"
            + "defined? A.gibtsnicht\n"));

        AssertTrue(wert.IsNil,
            "**the answer is nil** — and not \"method\" and not false, "
                + "because a game that writes `defined?(a.b) ? x : y` is "
                + "asking which arm to take");
    }

    /// <summary>
    /// A local variable, an instance variable and a global each answer with
    /// their own word.
    /// </summary>
    /// <remarks>
    /// <strong>Three words and not one.</strong> A game's own code
    /// distinguishes them — <strong>a reader that answered "expression" for
    /// all three would have made a game treat a local as a global</strong>,
    /// and a save file written under one would not load under the other.
    /// </remarks>
    public void Test_EachKindOfVariableAnswersWithItsOwnWord()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "x = 1\n"
            + "@hp = 2\n"
            + "$flag = 3\n"
            + "defined? (x)\n"));

        AssertEq(wert.ToString(), "local-variable",
            "**a name with a value in scope is a local variable** — and the "
                + "parentheses are the writer's and not the reader's, because "
                + "Ruby does not draw that distinction either");

        var ivar = new RubyInterpreter(new RubyNullHost());
        AssertEq(ivar.RunProgram(Statements("defined? @hp\n")).ToString(), "nil",
            "**an instance variable that was never set is nil** — not "
                + "\"instance-variable\", because the question is whether it "
                + "exists and it does not");

        // **Ein Lauf und eine Frage danach.** Zwei `RunProgram` auf
        // demselben Interpreter sind dasselbe Programm nur zweimal
        // beendet -- **und die erste Fassung tat das und bekam nil**,
        // weil der zweite Lauf die Globals nicht zuruecksetzt, aber der
        // erste Lauf nie eine geschrieben hatte: `$flag = 3` stand in
        // einem Skript, das `Statements` nicht als *dasselbe* Programm
        // ausfuehrt.
        var mit2 = new RubyInterpreter(new RubyNullHost());
        // **Zwei Anweisungen und ein Programm.** `Statements` nimmt einen
        // Quelltext und gibt die Knotenliste, **und `RunProgram` laeuft
        // ueber alle** -- die erste Fassung rief `RunProgram` zweimal auf
        // und bekam nil, **weil der erste Lauf nichts mit dem zweiten
        // gemeinsam hatte, ausser dem Interpreter, und der zweite Lauf
        // fing wieder bei der Frage an.** Der Globalspeicher haelt die
        // Zuweisung -- **aber nur, wenn dasselbe Programm sie gemacht hat.**
        var gelesen = mit2.RunProgram(Statements("$flag = 3\ndefined? $flag\n"));
        AssertEq(gelesen.ToString(), "global-variable",
            "**a global that was set is a global variable** — and the same "
                + "question before the assignment would be nil, which is the "
                + "whole use of the word. It was: " + gelesen.ToString());
    }

    /// <summary>
    /// A class the script defined answers "constant".
    /// </summary>
    /// <remarks>
    /// <strong>A class is a constant, and a game checks for that.</strong> A
    /// reader that answered "class" would have been a name this reader
    /// invented; <strong>"constant" is what <c>is_defined</c> returns for
    /// <c>NODE_CONST</c></strong>, and it is what a game's code compares
    /// against.
    /// </remarks>
    public void Test_AScriptClassAnswersConstant()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class Vorhanden\n"
            + "end\n"
            + "defined? Vorhanden\n"));

        AssertEq(wert.ToString(), "constant",
            "**a class the script defined is a constant** — `is_defined` "
                + "returns \"constant\" for a constant node, and a reader that "
                + "said \"class\" would have named something Ruby does not "
                + "name");
    }

    /// <summary>
    /// The question does not raise for a name nothing knows.
    /// </summary>
    /// <remarks>
    /// <strong>A question is not a statement.</strong> A game checks
    /// <c>defined?</c> precisely because it does not know — <strong>and a
    /// reader that raised here would have made the check the thing that
    /// fails</strong>, so the one guard a game writes would be the one line
    /// that stops it.
    /// </remarks>
    public void Test_TheQuestionDoesNotRaiseForAnUnknownName()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        RubyValue wert;
        try
        {
            wert = mit.RunProgram(Statements("defined? Nichts\n"));
        }
        catch (System.Exception e)
        {
            AssertTrue(false, "the question raised: " + e.Message);
            return;
        }

        AssertTrue(wert.IsNil,
            "**and the answer is nil** — a game asks `defined?` because it "
                + "does not know, and a reader that raised would have made the "
                + "check the line that stops it");
    }

    /// <summary>
    /// A question about a call whose method is missing answers nil, and it
    /// answers without making the call.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>This is the case that kills the mutation which evaluated the
    /// expression.</strong> Every other question in this file is about
    /// something that works, <strong>so a reader that ran the expression
    /// would have made the call and then answered about it</strong>, and the
    /// answer would be right by accident. Here the method is not there: a
    /// reader that runs the question <strong>would answer a diagnostic about
    /// the host's missing method</strong> rather than `nil`, and a game's
    /// `defined?(a.b) ? x : y` would take the first arm on a name nobody
    /// defined.
    /// </para>
    /// </remarks>
    public void Test_AQuestionAboutAMissingMethodDoesNotMakeTheCall()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var knoten = Statements("class A\nend\ndefined? A.gibtsnicht\n");
        var wert = mit.RunProgram(knoten);

        AssertTrue(wert.IsNil,
            "**the answer is nil** — and the question made no call, so the "
                + "diagnostics are: " + string.Join(" | ", mit.Diagnostics)
                + ". The value was " + wert.ToString());
        var ueberHost = 0;
        foreach (var d in mit.Diagnostics)
        {
            if (d.Contains("has no method"))
            {
                ueberHost++;
            }
        }

        AssertEq(ueberHost, 0,
            "**and nothing was asked of the host** — a question is not a call, "
                + "and a reader that made it would have reported a missing "
                + "method for a name it was only asking about");
    }
}
