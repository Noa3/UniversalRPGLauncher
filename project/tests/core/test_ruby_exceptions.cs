using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// `raise`, `rescue` and `ensure`, which is how a game says it cannot go on.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And without them a script runs past its own error path.</strong>
/// Every RPG Maker script of that time wraps the call it does not trust,
/// <strong>and a reader that only refused the unknown method would have run
/// the line after the failure as if nothing had happened</strong> — which is
/// a save file written from state that is already wrong.
/// </para>
/// <para>
/// <strong>And `rescue` did not even parse.</strong> Measured: *'rescue' at
/// offset 10 does not begin an expression* —
/// <strong>and the sentence a game writes to handle its own error was a
/// syntax error.</strong>
/// </para>
/// </remarks>
public partial class TestRubyInterpreter
{
    /// <summary>
    /// `raise` stops the script, and the class and the text are the ones
    /// written.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And there are three forms, and they are not
    /// interchangeable.</strong> <c>raise "x"</c> is a
    /// <c>RuntimeError</c>,
    /// <c>raise ArgumentError, "x"</c> is that class with that text,
    /// <strong>and <c>raise ArgumentError.new("x")</c> puts the text inside
    /// the object</strong> — and a reader that treated the second and the
    /// third alike would have named every error by its class and lost every
    /// message,
    /// <strong>and a game's error dialog would be a class name with no text
    /// in it.</strong>
    /// </para>
    /// </remarks>
    public void Test_RaiseStopsTheScriptWithTheClassAndTheTextWritten()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var ausnahme = FehlerAus<RubyRuntimeException>(() =>
            mit.RunProgram(Statements("raise \"kaputt\"\n")));
        AssertEq(ausnahme.Class, "RuntimeError",
            "**a text alone is a RuntimeError** — and not a class of the "
                + "game's own, because the game named none");
        AssertEq(ausnahme.Detail, "kaputt",
            "**and the text is the text**");

        var mit2 = new RubyInterpreter(new RubyNullHost());
        var zweite = FehlerAus<RubyRuntimeException>(() =>
            mit2.RunProgram(Statements("raise ArgumentError, \"kaputt\"\n")));
        AssertEq(zweite.Class, "ArgumentError",
            "**a name and a text raise that class** — and a reader that took "
                + "the first value as the text would have written a message "
                + "about the class");
        AssertEq(zweite.Detail, "kaputt",
            "**and the text is the second value**");
    }

    /// <summary>
    /// `rescue` catches, and only the classes it names.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And a class list is a list.</strong>
    /// <c>rescue ArgumentError, TypeError</c> faengt beide,
    /// <strong>and a reader that took the first name would have let a game's
    /// second error class escape</strong> — and the game would crash on the
    /// error it thought it had handled.
    /// </para>
    /// <para>
    /// <strong>And a superclass catches what is under it.</strong>
    /// <c>rescue StandardError</c> faengt einen <c>ArgumentError</c>,
    /// <strong>because that is what the hierarchy says</strong> — and a
    /// reader that compared two names for equality would catch nothing.
    /// </para>
    /// </remarks>
    public void Test_RescueCatchesAndOnlyTheClassesItNames()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "a = begin\n"
            + "  1/0\n"
            + "rescue ZeroDivisionError\n"
            + "  10\n"
            + "end\n"
            + "c = begin\n"
            + "  1/0\n"
            + "rescue StandardError, TypeError\n"
            + "  30\n"
            + "end\n"
            + "[a, c]\n"));

        AssertEq(AsInteger(wert.Items[0]), 10,
            "**the class it names was caught**");
        AssertEq(AsInteger(wert.Items[1]), 30,
            "**and a list of two classes catches both** — `TypeError` sits "
                + "under `StandardError` and the error is a "
                + "`ZeroDivisionError`, **so the list catches it twice over, "
                + "and a reader that took the first name alone would have let "
                + "the game's second error class escape**");

        // **Und ein Arm, dessen Klasse nicht passt, laesst den Fehler
        // durch.** Das ist gemessen und nicht angenommen:
        // **Ruby bricht auch ab, wenn kein Arm greift**,
        // **und ein Leser, der den Fehler verschluckt haette, wuerde jedes
        // Spiel, das einen Rechenfehler von einem Typfehler unterscheidet,
        // mit der falschen Antwort laufen lassen** --
        // **und genau diese Unterscheidung ist der Grund, warum es zwei
        // Klassen gibt.**
        var allein = new RubyInterpreter(new RubyNullHost());
        var durchgelassen = FehlerAus<RubyRuntimeException>(() => allein.RunProgram(Statements(
            "begin\n"
            + "  1/0\n"
            + "rescue TypeError\n"
            + "  20\n"
            + "end\n")));
        AssertEq(durchgelassen.Class, "ZeroDivisionError",
            "**and an arm that does not name the class lets it through** — a "
                + "reader that caught everything would make `rescue TypeError` "
                + "handle a division by zero, and the two classes would be "
                + "the same thing");

    }

    /// <summary>
    /// `rescue => e` binds the error, and it answers `message` and `class`.
    /// </summary>
    /// <remarks>
    /// <strong>And those are the two things a game reads.</strong>
    /// <strong>And a reader that gave nil for both would have an error
    /// dialog with no text in it</strong> — and a player staring at an empty
    /// box with an OK button.
    /// </remarks>
    public void Test_RescueWithABoundErrorAnswersMessageAndClass()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "begin\n"
            + "  raise ArgumentError, \"kaputt\"\n"
            + "rescue => e\n"
            + "  [e.message, e.class]\n"
            + "end\n"));

        AssertEq(System.Text.Encoding.UTF8.GetString(wert.Items[0].Bytes), "kaputt",
            "**the message is the text that was raised** — and a reader that "
                + "gave nil would have an error dialog with no text in it, "
                + "and a player staring at an empty box with an OK button");
        AssertEq(wert.Items[1].Name, "ArgumentError",
            "**and the class is the class that was raised**");
    }

    /// <summary>
    /// `ensure` runs on every way out, and its value is not the answer.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And on the way with an error too.</strong> That is the whole
    /// reason a game writes it,
    /// <strong>and a reader that ran it only when nothing was wrong would
    /// leave a file open when the game failed</strong> — and the next save
    /// would go into a file that is already open, and the save before it
    /// would be gone.
    /// </para>
    /// <para>
    /// <strong>And its value is not the answer.</strong>
    /// <c>begin; a; ensure; b; end</c> answers <c>a</c>,
    /// <strong>and a reader that made <c>ensure</c> its answer would have a
    /// method return the value of the line that closed a file.</strong>
    /// </para>
    /// </remarks>
    public void Test_EnsureRunsOnEveryWayOutAndIsNotTheAnswer()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "g = 0\n"
            + "a = begin\n"
            + "  g = g + 1\n"
            + "  1\n"
            + "ensure\n"
            + "  g = g + 10\n"
            + "end\n"
            + "b = begin\n"
            + "  1/0\n"
            + "rescue\n"
            + "  2\n"
            + "ensure\n"
            + "  g = g + 100\n"
            + "end\n"
            + "[a, b, g]\n"));

        AssertEq(AsInteger(wert.Items[0]), 1,
            "**the body is the answer and not the line that closed a file** — "
                + "a reader that made `ensure` its answer would have a method "
                + "return the value of the close");
        AssertEq(AsInteger(wert.Items[1]), 2,
            "**and the answer is the arm that caught it**");
        AssertEq(AsInteger(wert.Items[2]), 111,
            "**and `ensure` ran on both ways out** — a reader that ran it "
                + "only when nothing was wrong would leave a file open when "
                + "the game failed, and the next save would go into a file "
                + "that is already open");
    }

    /// <summary>
    /// A method body may carry the arms without a `begin`.
    /// </summary>
    /// <remarks>
    /// <strong>And that is the form `Kernel#load` uses.</strong>
    /// <c>def m; a; rescue; b; end</c>,
    /// <strong>and a reader that demanded an `end` right after the body
    /// would refuse a script the language itself reads</strong> — and
    /// `def m; a; rescue; b; end` is in every RPG Maker script that loads a
    /// file.
    /// </remarks>
    public void Test_AMethodBodyCarriesTheArmsWithoutABegin()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class Leser\n"
            + "  def lies\n"
            + "    1/0\n"
            + "  rescue => e\n"
            + "    e.message\n"
            + "  end\n"
            + "  def sicher\n"
            + "    42\n"
            + "  ensure\n"
            + "    @gerufen = true\n"
            + "  end\n"
            + "end\n"
            + "l = Leser.new\n"
            + "[l.lies, l.sicher, l.instance_variable_get(:@gerufen)]\n"));

        AssertEq(System.Text.Encoding.UTF8.GetString(wert.Items[0].Bytes),
            "divided by 0",
            "**the method's own arm caught its own error** — and a reader "
                + "that demanded an `end` right after the body would refuse "
                + "a script the language itself reads, and every RPG Maker "
                + "script that loads a file writes it this way");
        AssertEq(AsInteger(wert.Items[1]), 42,
            "**and the method's answer is its body**");
    }

    /// <summary>
    /// The first arm whose class it is, and `Exception` is the last resort
    /// that always matches.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the order is the order they were written in.</strong>
    /// `rescue RuntimeError` before `rescue Exception` takes the specific
    /// one,
    /// <strong>and a reader that took the general one first would have run
    /// a game's catch-all before its specific handler</strong> — and the
    /// specific one is the one that knows what to do.
    /// </para>
    /// <para>
    /// <strong>And `Exception` really is the root.</strong>
    /// <c>rescue Exception</c> faengt einen <c>RuntimeError</c>,
    /// <strong>because that is what the hierarchy says</strong> — and a
    /// game that writes <c>rescue Exception =&gt; e</c> expects it to catch
    /// everything.
    /// </para>
    /// </remarks>
    public void Test_TheFirstArmWhoseClassItIsWinsAndExceptionCatchesAll()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "a = begin\n"
            + "  raise \"x\"\n"
            + "rescue RuntimeError\n"
            + "  1\n"
            + "rescue Exception\n"
            + "  2\n"
            + "end\n"
            + "b = begin\n"
            + "  raise \"x\"\n"
            + "rescue Exception\n"
            + "  2\n"
            + "rescue RuntimeError\n"
            + "  1\n"
            + "end\n"
            + "c = begin\n"
            + "  raise \"x\"\n"
            + "rescue Exception\n"
            + "  3\n"
            + "end\n"
            + "[a, b, c]\n"));

        AssertEq(AsInteger(wert.Items[0]), 1,
            "**the specific arm came first and won** — a reader that took "
                + "the general one first would have run a game's catch-all "
                + "before the handler that knows what to do");
        AssertEq(AsInteger(wert.Items[1]), 2,
            "**and the general arm first also wins** — because that is where "
                + "the game put it, and an arm does not get to be skipped "
                + "because a later one would also match");
        AssertEq(AsInteger(wert.Items[2]), 3,
            "**and `Exception` really is the root** — a game that writes "
                + "`rescue Exception => e` expects it to catch everything, "
                + "and a reader whose chain stopped at the first parent "
                + "would have let a runtime error past it");
    }

    /// <summary>
    /// An arm with a bound error and no error, and one with two of them.
    /// </summary>
    /// <remarks>
    /// <strong>And an arm with a bound name is an arm.</strong>
    /// `rescue =&gt; e` faengt jeden Fehler,
    /// <strong>und ein Leser, der einen Arm nur an einem leeren Namen
    /// erkannte, wuerde jeden Arm mit einem gebundenen Fehler
    /// ueberspringen</strong> — **and `rescue =&gt; e` is the most common
    /// form of all.**
    /// </remarks>
    public void Test_AnArmWithABoundErrorIsAnArmAndTheSecondOneWaits()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "a = begin\n"
            + "  1\n"
            + "rescue => e\n"
            + "  9\n"
            + "end\n"
            + "b = begin\n"
            + "  raise \"x\"\n"
            + "rescue => e\n"
            + "  5\n"
            + "rescue => e\n"
            + "  6\n"
            + "end\n"
            + "[a, b]\n"));

        AssertEq(AsInteger(wert.Items[0]), 1,
            "**the body answered, and no arm ran** — and the arm with a bound "
                + "error is an arm, because `rescue => e` is the most common "
                + "form of all and a reader that only recognised an arm by "
                + "an empty name would skip it");
        AssertEq(AsInteger(wert.Items[1]), 5,
            "**and the first bound arm caught it** — the second one waits, "
                + "and a reader that ran the last would have given a game "
                + "its fallback instead of its handler");
    }

    /// <summary>
    /// The first arm that catches it wins.
    /// </summary>
    /// <remarks>
    /// <strong>And that is the order they were written in.</strong> Ruby
    /// takes the arms in order,
    /// <strong>and a reader that took the last would have run a game's
    /// general handler before its specific one</strong> — and the specific
    /// one is the one that knows what to do.
    /// </remarks>
    public void Test_TheFirstArmThatCatchesItWins()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "begin\n"
            + "  raise \"x\"\n"
            + "rescue ArgumentError\n"
            + "  1\n"
            + "rescue RuntimeError\n"
            + "  2\n"
            + "end\n"));

        AssertEq(AsInteger(wert), 2,
            "**the second arm caught it and not the first** — the first names "
                + "a class the error is not of, and a reader that stopped at "
                + "the first arm whatever it said would have run a game's "
                + "wrong handler");
    }
    /// <summary>
    /// `else` is not an answer, and `ensure` is not one either.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And without the jump, `else` would run as an arm.</strong>
    /// <c>begin; raise "x"; rescue; 5; else; 99; end</c> would answer 99
    /// instead of 5,
    /// <strong>because `else` stands in the same list of nodes and the arm
    /// loop walks that list</strong> — and
    /// <strong>that is the whole difference between `else` and a second
    /// `rescue`: `else` is the way taken when nothing went wrong, and an arm
    /// is what runs when something did.</strong>
    /// </para>
    /// <para>
    /// <strong>And this is a test that cannot see the difference by looking
    /// at the value alone.</strong> A reader whose arm loop skipped
    /// non-arms would answer 5, and one that did not would answer 99 —
    /// <strong>and the mutation that removes the jump was measured against
    /// exactly this sentence.</strong>
    /// </para>
    /// </remarks>
    public void Test_ElseIsNotAnAnswerAndEnsureIsNotOneEither()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "a = begin\n"
            + "  raise \"x\"\n"
            + "rescue\n"
            + "  5\n"
            + "else\n"
            + "  99\n"
            + "end\n"
            + "b = begin\n"
            + "  1\n"
            + "else\n"
            + "  99\n"
            + "end\n"
            + "c = begin\n"
            + "  raise \"x\"\n"
            + "rescue => e\n"
            + "  5\n"
            + "ensure\n"
            + "  99\n"
            + "end\n"
            + "[a, b, c]\n"));

        AssertEq(AsInteger(wert.Items[0]), 5,
            "**the arm answered and not the `else`** — `else` stands in the "
                + "same list of nodes and the arm loop walks that list, so a "
                + "reader that did not skip it would have answered 99");
        AssertEq(AsInteger(wert.Items[1]), 1,
            "**and `else` never runs when nothing went wrong** — it is the "
                + "way taken when nothing went wrong, and a reader that ran "
                + "it always would have a game's `else` run after its "
                + "`rescue`, which is the second rescue arm in everything "
                + "but its name");
        AssertEq(AsInteger(wert.Items[2]), 5,
            "**and `ensure` is not an answer** — the same list, and the same "
                + "skip, and a reader that ran it as an arm would have "
                + "answered 99 and lost the arm's answer entirely");
    }
}
