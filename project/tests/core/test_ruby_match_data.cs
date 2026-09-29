using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Reading a match back, and reading a global at all.
/// </summary>
/// <remarks>
/// <para>
/// <strong>A global could be written and not read.</strong> <c>$x = 1</c>
/// worked and <c>$x</c> did not, because the assignment went to a table and
/// the reading did not go anywhere —
/// <strong>and <c>$game_party</c> is the first line of every RPG Maker
/// script.</strong>
/// </para>
/// <para>
/// <strong>And a match was a number and nothing else.</strong>
/// <c>s =~ /(\d+)/</c> answered where,
/// <strong>and a script that then read <c>$1</c> to get the number had
/// nothing to read</strong> — which is the form every plugin uses to pull
/// a number out of an event name.
/// </para>
/// </remarks>
public partial class TestRubyInterpreter
{
    /// <summary>
    /// A global that was set can be read.
    /// </summary>
    /// <remarks>
    /// <strong>And this was the gap, not a missing method.</strong> The
    /// writing went to a table and the reading did not go anywhere,
    /// <strong>so a script could set what it could not read</strong> — and
    /// <c>$game_party</c> is the first line of every RPG Maker script.
    /// </remarks>
    public void Test_AGlobalThatWasSetCanBeRead()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements("$flag = 3\n$flag\n"));

        AssertEq(AsInteger(wert), 3,
            "**the global came back** — the writing went to a table and the "
                + "reading did not go anywhere, so a script could set what it "
                + "could not read, and `$game_party` is the first line of "
                + "every RPG Maker script");
    }

    /// <summary>
    /// `$1` is the first group, and it is a number a game can use.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the sentence with which a plugin pulls a number
    /// out of an event name.</strong> <c>$1</c> after
    /// <c>s =~ /(\d+)/</c> is the digits,
    /// <strong>and a reader that left `$1` empty would have given every
    /// plugin a name where a number belongs.</strong>
    /// </para>
    /// <para>
    /// <strong>And a group that did not take part is nil, not an empty
    /// text.</strong> <c>"abc" =~ /(a)(z)?/</c> has a second group that
    /// matched nothing,
    /// <strong>and a script that reads <c>$2</c> wants to know whether there
    /// was a second group</strong> — and an empty text says there was one
    /// and it was empty.
    /// </para>
    /// </remarks>
    public void Test_TheFirstGroupIsReadableAsANumber()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class A\n"
            + "  def m\n"
            + "    \"ab12\" =~ /(\\d+)/\n"
            + "    $1\n"
            + "  end\n"
            + "end\n"
            + "A.new.m\n"));

        AssertEq(System.Text.Encoding.UTF8.GetString(wert.Bytes), "12",
            "**the digits came back** — and this is the sentence with which a "
                + "plugin pulls a number out of an event name; a reader that "
                + "left `$1` empty would have given every plugin a name where "
                + "a number belongs");

        var mit2 = new RubyInterpreter(new RubyNullHost());
        var ohneGruppe = mit2.RunProgram(Statements(
            "\"abc\" =~ /(a)(z)?/\n$2\n"));
        AssertTrue(ohneGruppe.Kind == RubyValueKind.Nil,
            "**and a group that did not take part is nil, not an empty text** "
                + "— a script that reads `$2` wants to know whether there was "
                + "a second group, and an empty text says there was one and "
                + "it was empty");
    }

    /// <summary>
    /// `$&` is the whole match, and the text around it has names too.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And <c>$&amp;</c> is not a number.</strong> It is the text the
    /// pattern found,
    /// <strong>and a reader that gave a place there would have had a plugin
    /// that renames a file rename it to a number.</strong>
    /// </para>
    /// <para>
    /// <strong>And the text before and after it have names.</strong>
    /// <c>$`</c> and <c>$'</c>,
    /// <strong>und <c>pre_match</c> und <c>post_match</c> sagen dasselbe**
    /// — **and a script that takes the name from before the colon uses
    /// one of the two, and a reader that knew neither would have given it
    /// half an argument.**
    /// </para>
    /// </remarks>
    public void Test_TheWholeMatchAndTheTextAroundItHaveNames()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class A\n"
            + "  def m\n"
            + "    \"Held:12\" =~ /\\d+/\n"
            + "    [$&, $`, $', $~.pre_match, $~.post_match]\n"
            + "  end\n"
            + "end\n"
            + "A.new.m\n"));

        AssertEq(System.Text.Encoding.UTF8.GetString(wert.Items[0].Bytes), "12",
            "**the whole match is the text and not a number** — a reader that "
                + "gave a place there would have had a plugin that renames a "
                + "file rename it to a number");
        AssertEq(System.Text.Encoding.UTF8.GetString(wert.Items[1].Bytes), "Held:",
            "**and the text before it came back**");
        AssertEq(System.Text.Encoding.UTF8.GetString(wert.Items[2].Bytes), "",
            "**and the text after it** — and it is empty here because the "
                + "match ends the text, which is a different answer from nil");
        AssertEq(System.Text.Encoding.UTF8.GetString(wert.Items[3].Bytes), "Held:",
            "**and `pre_match` says the same as the backtick name** — a "
                + "script that takes the name from before the colon uses one "
                + "of the two");
        AssertEq(wert.Items[4].Kind, RubyValueKind.String,
            "**and `post_match` answered with a value and not nil**");
    }

    /// <summary>
    /// `Regexp.last_match` is the same match under the name plugins write.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And it said the host was at fault for something the reader
    /// lacked.</strong> <c>Regexp.last_match[1]</c> is in almost every
    /// plugin,
    /// <strong>and the answer was <c>the constant Regexp is not defined by
    /// this host</c> — a message about the host for a class the reader did
    /// not have.</strong>
    /// </para>
    /// <para>
    /// <strong>And it is the same match and not a copy.</strong> A game that
    /// reads <c>$~</c> and then <c>Regexp.last_match</c> gets one answer,
    /// <strong>because six tables would be six chances for them to
    /// disagree.</strong>
    /// </para>
    /// </remarks>
    public void Test_LastMatchIsTheSameMatchUnderTheNamePluginsWrite()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class A\n"
            + "  def m\n"
            + "    \"ab12\" =~ /(\\d+)/\n"
            + "    [Regexp.last_match[1], $~[1]]\n"
            + "  end\n"
            + "end\n"
            + "A.new.m\n"));

        AssertEq(System.Text.Encoding.UTF8.GetString(wert.Items[0].Bytes), "12",
            "**the group came back under the name plugins write** — and the "
                + "answer used to be a message about the host for a class the "
                + "reader did not have");
        AssertEq(System.Text.Encoding.UTF8.GetString(wert.Items[1].Bytes), "12",
            "**and it is the same match and not a copy** — a game that reads "
                + "`$~` and then `Regexp.last_match` gets one answer, because "
                + "six tables would be six chances for them to disagree");
    }

    /// <summary>
    /// A run that found nothing clears the match.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And that is the difference between "there was never one" and
    /// "this one found nothing".</strong> Ruby sets the match on a hit and
    /// nil on a miss,
    /// <strong>and a reader that left the old one standing would have a
    /// script that matched nothing and read the previous line's number</strong>
    /// — which is a save file with one more field than the game expects.
    /// </para>
    /// <para>
    /// <strong>And before any run at all it is nil too.</strong> A script
    /// that reads <c>$1</c> before it has matched anything gets nothing,
    /// <strong>and that is different from an empty text.</strong>
    /// </para>
    /// </remarks>
    public void Test_ARunThatFoundNothingClearsTheMatch()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class A\n"
            + "  def m\n"
            + "    \"ab12\" =~ /(\\d+)/\n"
            + "    \"nichts\" =~ /(\\d+)/\n"
            + "    $1\n"
            + "  end\n"
            + "  def n\n"
            + "    $1\n"
            + "  end\n"
            + "end\n"
            + "[A.new.m, A.new.n]\n"));

        AssertTrue(wert.Items[0].Kind == RubyValueKind.Nil,
            "**the run that found nothing cleared it** — a reader that left "
                + "the old match standing would have a script that matched "
                + "nothing and read the previous line's number, which is a "
                + "save file with one more field than the game expects");
        AssertTrue(wert.Items[1].Kind == RubyValueKind.Nil,
            "**and before any run at all it is nil too** — different from an "
                + "empty text, and that is the difference a script sees");
    }    /// <summary>
    /// `$~` is the match, and its own numbers answer.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the mutation that changed <c>$~</c> to nil survived
    /// until this test existed.</strong> The other tests read
    /// <c>$~.pre_match</c> and <c>$~[1]</c>, and both of those work
    /// <strong>without the <c>$~</c> branch at all</strong>,
    /// <strong>because they are answered by the methods on the value and not
    /// by the value itself.</strong> Measured: <c>$~[0]</c>,
    /// <c>$~[1]</c>, <c>$~.size</c> and <c>$~.begin</c> all answer —
    /// **which is exactly the shape of a test that cannot see the
    /// difference.</strong>
    /// </para>
    /// <para>
    /// <strong>And <c>$~</c> on its own is a list of the whole match and the
    /// groups.</strong> That is what <c>$~[0]</c> reads,
    /// <strong>and a reader that made it a copy would have a game that
    /// changes the value and keeps the old one.</strong>
    /// </para>
    /// </remarks>
    public void Test_TheMatchItselfIsAValueWithItsOwnNumbers()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "\"ab12cd\" =~ /(\\d+)/\n"
            + "[$~[0], $~[1], $~.size, $~.begin, $~[9]]\n"));

        AssertEq(System.Text.Encoding.UTF8.GetString(wert.Items[0].Bytes), "12",
            "**the whole match is the first thing in it** — and the other "
                + "tests read `$~.pre_match` and `$~[1]`, which are answered "
                + "by the methods on the value and not by the value itself, "
                + "so a reader that made `$~` nil would have passed all of "
                + "them");
        AssertEq(System.Text.Encoding.UTF8.GetString(wert.Items[1].Bytes), "12",
            "**and the first group is the second thing**");
        AssertEq(AsInteger(wert.Items[2]), 2,
            "**and `size` counts the whole match plus the groups**");
        AssertEq(AsInteger(wert.Items[3]), 2,
            "**and `begin` is where it started** — and a reader that gave 0 "
                + "there would have had a plugin that cuts a string at the "
                + "wrong place");
        AssertTrue(wert.Items[4].Kind == RubyValueKind.Nil,
            "**and a group that is not there is nil**");
    }
}
