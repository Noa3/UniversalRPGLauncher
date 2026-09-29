using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The methods every value has, which the host does not have to provide.
/// </summary>
/// <remarks>
/// <para>
/// <strong>They were on the host, and the host cannot answer them.</strong>
/// Every value in Ruby has <c>to_s</c>, <c>nil?</c>, <c>class</c> and the
/// rest, and a real host answers <c>rand</c> and refuses the rest —
/// <strong>so a game writing <c>"Level #{level}"</c> depended on something no
/// host in this repository implements.</strong>
/// </para>
/// <para>
/// <strong>And the null host made it untestable.</strong> Every test that
/// says a number has a text would have needed a host that agrees,
/// <strong>which is how the interpreter came to look like it needs one.</strong>
/// </para>
/// </remarks>
public partial class TestRubyInterpreter
{
    /// <summary>
    /// A number has a text, and the null host is never asked.
    /// </summary>
    /// <remarks>
    /// <strong>The most ordinary line in an RPG Maker script.</strong>
    /// <c>"Level #{level}"</c> is a number turned into text, and
    /// <strong>a reader that left it to the host would have made every
    /// game's name for a room depend on something no host in this repository
    /// implements.</strong>
    /// </remarks>
    public void Test_ANumberHasATextAndTheNullHostIsNotAsked()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements("5.to_s\n"));

        AssertEq(wert.Kind, RubyValueKind.String,
            "**a number has a text** — and the null host was never asked, "
                + "because a host that answers only `rand` cannot answer this");
        AssertEq(
            System.Text.Encoding.UTF8.GetString(wert.Bytes),
            "5",
            "**and it is the five** — a reader that wrote it any other way "
                + "would have put a number into a save file no other game can "
                + "read");
    }

    /// <summary>
    /// `nil` says itself as `nil` and not as a C# word.
    /// </summary>
    /// <remarks>
    /// <strong>Ruby's spelling is the contract.</strong> A game writes the
    /// name of an absent thing into a message and into a save file,
    /// <strong>and a reader that wrote C#'s <c>Null</c> would have put a
    /// word in a save file that no other game reads</strong> — and one that
    /// wrote <c>true</c> instead of <c>True</c> has the same problem.
    /// </remarks>
    public void Test_NilAndTheBooleansSayThemselvesAsRubyWritesThem()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements("[nil.to_s, true.to_s, false.to_s]\n"));

        AssertEq(wert.Items.Count, 3,
            "**three texts came back**");
        AssertEq(System.Text.Encoding.UTF8.GetString(wert.Items[0].Bytes), "nil",
            "**nil is nil and not Null**");
        AssertEq(System.Text.Encoding.UTF8.GetString(wert.Items[1].Bytes), "true",
            "**and true is true and not True**");
        AssertEq(System.Text.Encoding.UTF8.GetString(wert.Items[2].Bytes), "false",
            "**and false is false** — a reader that used C#'s spelling would "
                + "have put a word in a save file no other game reads");
    }

    /// <summary>
    /// `nil?` and `class` answer, and they answer about the value.
    /// </summary>
    /// <remarks>
    /// <strong>Two questions every game asks.</strong> <c>return nil if x.nil?</c>
    /// is the first line of most guard clauses,
    /// <strong>and a reader that left both to the host would have made a
    /// game's own guard clauses fail</strong> — and silently, because a nil
    /// from a refused call looks the same as a nil from a method.
    /// </remarks>
    public void Test_NilQuestionAndClassAnswerAboutTheValue()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "[nil.nil?, 5.nil?, nil.class.to_s]\n"));

        AssertEq(wert.Items.Count, 3,
            "**three answers came back**");
        AssertTrue(wert.Items[0].Boolean,
            "**nil is nil and says so**");
        AssertTrue(!wert.Items[1].Boolean,
            "**and five is not** — a reader that answered the same for both "
                + "would have made every guard clause in a game pass");
        AssertEq(System.Text.Encoding.UTF8.GetString(wert.Items[2].Bytes), "NilClass",
            "**and nil's class is NilClass** — the name a game writes in a "
                + "diagnostic and a reader that wrote C#'s type name would "
                + "have written something no Ruby writes");
    }

    /// <summary>
    /// A class's own `to_s` wins over the value's.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>The script comes first, and that is the order.</strong> A
    /// game's <c>Scene_Title#to_s</c> is its own text,
    /// <strong>and a reader that asked the value's methods first would have
    /// taken every class's own <c>to_s</c> away</strong> — which is a game
    /// that shows its own name everywhere except where it wrote it.
    /// </para>
    /// <para>
    /// <strong>The order is the script, then the value, then the host.</strong>
    /// A game that defines it wins; a value that has one wins over a host
    /// that would refuse.
    /// </para>
    /// </remarks>
    public void Test_AClasssOwnToSWins()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class A\n"
            + "  def to_s\n"
            + "    \"von A\"\n"
            + "  end\n"
            + "end\n"
            + "A.new.to_s\n"));

        AssertEq(System.Text.Encoding.UTF8.GetString(wert.Bytes), "von A",
            "**the instance's own `to_s` won** — a reader that asked the "
                + "value's methods first would have taken every class's own "
                + "`to_s` away, and a game would show its own name everywhere "
                + "except where it wrote it");

        // **Und `A.to_s` ist eine andere Frage.** Ein Typ ist kein Objekt,
        // **und es hat kein `to_s` aus dem Rumpf** -- **es ist sein Name**,
        // **und die eigene `to_s` des Rumpfs gilt nur fuer die
        // Instanz.**
        var mit2 = new RubyInterpreter(new RubyNullHost());
        var wert2 = mit2.RunProgram(Statements(
            "class A\n"
            + "  def to_s\n"
            + "    \"von A\"\n"
            + "  end\n"
            + "end\n"
            + "A.to_s\n"));
        AssertEq(System.Text.Encoding.UTF8.GetString(wert2.Bytes), "A",
            "**and the type answers with its own name** — measured, and not "
                + "with the body's text: a type is not an object, and Ruby "
                + "names it");
    }

    /// <summary>
    /// `inspect` shows a string in quotes and a number does not.
    /// </summary>
    /// <remarks>
    /// <strong>The two are not the same and a game's `p` depends on
    /// it.</strong> <c>inspect</c> is what a debug line prints,
    /// <strong>and a reader that made it identical to <c>to_s</c> would have
    /// printed a string without its quotes</strong> — so a log of a name and a
    /// log of a number would be told apart by nothing.
    /// </remarks>
    public void Test_InspectShowsAStringInQuotesAndANumberNot()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements("[\"hi\".inspect, 5.inspect]\n"));

        AssertEq(System.Text.Encoding.UTF8.GetString(wert.Items[0].Bytes), "\"hi\"",
            "**a string is in quotes** — a log of a name and a log of a "
                + "number have to be told apart, and this is what tells them "
                + "apart");
        AssertEq(System.Text.Encoding.UTF8.GetString(wert.Items[1].Bytes), "5",
            "**and a number is not** — a reader that quoted everything would "
                + "have made every number look like a name");
    }

    /// <summary>
    /// `freeze` answers the value, because nothing here can be frozen.
    /// </summary>
    /// <remarks>
    /// <strong>A copy would be a different thing from what the game
    /// asked for.</strong> <c>Sprite.new(...).freeze</c> is the sprite the
    /// game made, <strong>and a reader that made a copy would have given a
    /// game two sprites where it made one</strong> — and the game's picture
    /// would be the copy, which nothing else holds.
    /// </remarks>
    public void Test_FreezeAnswersTheValueAndNotACopy()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements("[5.freeze, 5.frozen?]\n"));

        AssertTrue(wert.Items[0].Kind == RubyValueKind.Integer,
            "**freeze answers a number** — a reader that made a copy would "
                + "have given a game two things where it made one");
        AssertTrue(wert.Items[1].Boolean,
            "**and frozen? says yes** — this runtime has no changeable value "
                + "objects, and a reader that said no would make a game's "
                + "guard clause `unless sprite.frozen?` run when it should not");
    }

    /// <summary>
    /// The value's methods answer, and the null host still refuses the rest.
    /// </summary>
    /// <remarks>
    /// <strong>The line between the two layers.</strong> A number has a text
    /// and a host that has no such number does not care,
    /// <strong>and a method that is neither stays a refusal</strong> — a
    /// reader that answered everything would have made a typo look like a
    /// method.
    /// </remarks>
    public void Test_TheHostStillRefusesWhatIsNeitherTheScriptsNorTheValues()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        mit.RunProgram(Statements("5.gibtsnicht\n"));

        var gesagt = false;
        foreach (var d in mit.Diagnostics)
        {
            if (d.Contains("'gibtsnicht'"))
            {
                gesagt = true;
            }
        }

        AssertTrue(gesagt,
            "**and it says so** — a reader that answered everything would have "
                + "made a typo look like a method; the diagnostics were: "
                + string.Join(" | ", mit.Diagnostics));
    }
}
