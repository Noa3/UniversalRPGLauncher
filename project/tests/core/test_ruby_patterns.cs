using System.Text;

using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The pattern engine, which is how a script looks a name up in a text.
/// </summary>
/// <remarks>
/// <para>
/// <strong>It did not exist.</strong> <c>name =~ /Held/</c> is how a plugin
/// finds a file, recognises an event name and looks a key up in a save
/// file, <strong>and without it a game has no way to test text at
/// all.</strong>
/// </para>
/// <para>
/// <strong>And it has a bound, and that is the reason it exists in this
/// form.</strong> A pattern comes from the game's own data, and a pattern
/// that runs long is a way to stop a game from inside its own script —
/// a nested quantifier over a long text does it in a few hundred
/// milliseconds. <strong>Every run here is bounded by the text's
/// length</strong>, not by a clock, because a clock is not testable and a
/// pattern that came back late is a game that is already hanging.
/// </para>
/// </remarks>
public partial class TestRubyInterpreter
{
    /// <summary>
    /// `=~` says where, and `-1` says nowhere.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And it says the place and not true or false.</strong> Ruby
    /// returns the index,
    /// <strong>and a reader that answered a boolean would have taken the
    /// index away from a game that writes
    /// <c>if s =~ /x/ then s.slice($~...) </c></strong> — and a game that
    /// reads what it just matched would read nothing.
    /// </para>
    /// <para>
    /// <strong>And the pattern may stand on either side.</strong>
    /// <c>/held/ =~ name</c> is the same test,
    /// <strong>and a reader that only knew the text-first form refused the
    /// other</strong> — and a script writes both, depending on what it
    /// already holds.
    /// </para>
    /// </remarks>
    public void Test_MatchSaysWhereAndMinusOneSaysNowhere()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "[\"Held Drache\" =~ /Drache/, \"Held Drache\" =~ /nichtda/]\n"));

        AssertEq(AsInteger(wert.Items[0]), 5,
            "**the pattern says where it matched** — and a reader that "
                + "answered true or false would have taken the index away "
                + "from a game that writes `if s =~ /x/ then s.slice(...)`, "
                + "and a game that reads what it just matched would read "
                + "nothing");
        AssertEq(AsInteger(wert.Items[1]), -1,
            "**and minus one says nowhere** — and not nil, because a script "
                + "that writes `if s !~ /x/` needs to be able to tell the two "
                + "apart");

        var mit2 = new RubyInterpreter(new RubyNullHost());
        var andersrum = mit2.RunProgram(Statements("/Drache/ =~ \"Ein Held Drache\"\n"));
        AssertEq(AsInteger(andersrum), 9,
            "**and the pattern may stand on the left** — a reader that only "
                + "knew the text-first form refused the other, and a script "
                + "writes both depending on what it holds");
    }

    /// <summary>
    /// `i` makes the pattern ignore the case, and it was thrown away.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the letters behind the second slash were read by the lexer
    /// and thrown away.</strong> <c>/held/i</c> and <c>/held/</c> were the
    /// same pattern, <strong>so a script that looks a name up without caring
    /// about the spelling did not find it, and nothing said so.</strong>
    /// </para>
    /// <para>
    /// <strong>And the order is <c>m</c>, <c>i</c>, <c>x</c>, and that is
    /// not this reader's choice.</strong> Verified in <c>re.c</c> from Ruby
    /// 1.8.1, where <c>rb_reg_to_s</c> appends them in exactly that order.
    /// </para>
    /// </remarks>
    public void Test_TheIgnoreCaseOptionIsNotThrownAway()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "[\"HELD\" =~ /held/, \"HELD\" =~ /held/i]\n"));

        AssertEq(AsInteger(wert.Items[0]), -1,
            "**without the option the case matters** — that is what a "
                + "pattern without `i` means");
        AssertEq(AsInteger(wert.Items[1]), 0,
            "**and with it the case does not** — the letters behind the "
                + "second slash were read and thrown away, so this was a "
                + "case-insensitive compare that had become a "
                + "case-sensitive one, and nothing said so");
    }

    /// <summary>
    /// `!~` is the negative, and it is a question.
    /// </summary>
    /// <remarks>
    /// <strong>And it is how a script rules a name out.</strong>
    /// <c>return if name !~ /\\A[A-Z]/</c> is a guard clause,
    /// <strong>and a reader that gave a number would have made
    /// `if !~` false for every name</strong> — and every name would be
    /// accepted.
    /// </remarks>
    public void Test_NotMatchIsAQuestion()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "[\"held\" !~ /Drache/, \"Ein Drache\" !~ /Drache/]\n"));

        AssertTrue(wert.Items[0].Boolean,
            "**a name without the pattern is not it** — and this is the "
                + "guard clause a script writes to rule a name out");
        AssertTrue(!wert.Items[1].Boolean,
            "**and one with it is** — and a reader that gave a number would "
                + "have made `!~` false for every name, and every name would "
                + "be accepted");
    }

    /// <summary>
    /// `scan` finds every one, and a block asks for the groups.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And a block changes what comes out.</strong>
    /// <c>text.scan(/(\\d+)/)</c> gives the number runs,
    /// <strong>and with a block it gives the groups instead of the whole
    /// match</strong> — and that is the sentence with which a game pulls the
    /// number out of an event name.
    /// </para>
    /// <para>
    /// <strong>And every match, not the first.</strong> A save file has
    /// many keys in it,
    /// <strong>and a reader that stopped at the first would have found one
    /// name where a game wrote three</strong>.
    /// </para>
    /// </remarks>
    public void Test_ScanFindsEveryOneAndABlockAsksForTheGroups()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var alle = mit.RunProgram(Statements("\"a1b2c3\".scan(/\\d+/)\n"));

        AssertEq(alle.Items.Count, 3,
            "**every match and not the first** — a save file has many keys "
                + "in it, and a reader that stopped at the first would have "
                + "found one name where a game wrote three");
        AssertEq(System.Text.Encoding.UTF8.GetString(
                alle.Items[0].Items[0].Bytes),
            "1",
            "**and each match is a list of its groups** — `scan` gives the "
                + "groups back, and a match without a group is a list of "
                + "one, which is what a game that scans for a number and "
                + "reads the first thing gets");

        var mit2 = new RubyInterpreter(new RubyNullHost());
        var gruppen = mit2.RunProgram(Statements(
            "\"Ab\".scan(/(\\w)(\\w)/) { |a, b| [a, b] }\n"));
        AssertEq(gruppen.Items.Count, 1,
            "**and a block is run for every match** — and that is the "
                + "sentence with which a game pulls the number out of an "
                + "event name");
        AssertEq(System.Text.Encoding.UTF8.GetString(gruppen.Items[0].Items[0].Bytes), "A",
            "**and the answer is the groups and not what the block "
                + "returned** — `scan` gives its own list back, and a "
                + "reader that gave nil or the block's answer would have "
                + "made a script that scans and reads the list read "
                + "nothing");
    }

    /// <summary>
    /// A text that is too long for one run says which pattern it was.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the bound is on the text's length and not on a
    /// clock.</strong> A clock is not testable,
    /// <strong>and a pattern that came back after a long time is a game that
    /// is already hanging</strong> — so what is counted is bytes, which is
    /// the same for a given pattern and a given text every time.
    /// </para>
    /// <para>
    /// <strong>And the message names the pattern.</strong> A warning about
    /// nothing sends the reader looking,
    /// <strong>and a script author who sees <c>/a/</c> in the message can
    /// find the line that hangs.</strong>
    /// </para>
    /// <para>
    /// <strong>And a real game's own text stays under the bound.</strong>
    /// 4096 bytes is a paragraph,
    /// <strong>and a name check on a line needs a hundred comparisons or so,
    /// so a pattern that goes over that is one that has no business in a
    /// game.</strong>
    /// </para>
    /// </remarks>
    public void Test_TextTooLongForOneRunSaysWhichPatternItWas()
    {
        var lang = new string('a', 5000);
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "t = \"" + lang + "\"\nt =~ /a/\n"));

        AssertTrue(wert.Kind == RubyValueKind.Nil,
            "**nothing came back** — and not a wrong number, because a "
                + "pattern that runs long is a way to stop a game, and a "
                + "reader that answered anyway would have given that away");

        var genannt = false;
        foreach (var d in mit.Diagnostics)
        {
            if (d.Contains("/a/") && d.Contains("4096"))
            {
                genannt = true;
            }
        }

        AssertTrue(genannt,
            "**and the message names the pattern and the bound** — a warning "
                + "about nothing sends the reader looking; the diagnostics "
                + "were: " + string.Join(" | ", mit.Diagnostics));
    }

    /// <summary>
    /// A pattern that cannot be built says so, and does not answer.
    /// </summary>
    /// <remarks>
    /// <strong>And a silent answer looks like a text with no match.</strong>
    /// A game that writes <c>/[/</c> has a broken pattern,
    /// <strong>and a reader that returned nil for it would have made the
    /// game believe the name is not in the text</strong> — and it would
    /// have looked for a different reason.
    /// </remarks>
    public void Test_APatternThatCannotBeBuiltSaysSo()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        mit.RunProgram(Statements("\"abc\" =~ /(a/\n"));

        var gesagt = false;
        foreach (var d in mit.Diagnostics)
        {
            if (d.Contains("could not be put together"))
            {
                gesagt = true;
            }
        }

        AssertTrue(gesagt,
            "**the message says the pattern could not be built** — a silent "
                + "nil would look exactly like a text with no match in it, "
                + "and the game would look for a different reason; the "
                + "diagnostics were: " + string.Join(" | ", mit.Diagnostics));
    }

    /// <summary>
    /// `=~` without a pattern says what it got, and answers nothing.
    /// </summary>
    /// <remarks>
    /// <strong>And it is not the same test.</strong> <c>s =~ "x"</c> is not
    /// <c>s == "x"</c>,
    /// <strong>and a reader that compared the two texts instead would have
    /// made a script that meant to use a pattern use an equality</strong> —
    /// and a game that looks a name up would find it by whole-string
    /// equality, which is a different question.
    /// </remarks>
    public void Test_MatchWithoutAPatternSaysWhatItGot()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements("\"abc\" =~ \"b\"\n"));

        AssertTrue(wert.Kind == RubyValueKind.Nil,
            "**nothing came back** — and it is not the same test as "
                + "equality, so a reader that compared the two texts would "
                + "have made a script that meant to use a pattern use an "
                + "equality");

        var genannt = false;
        foreach (var d in mit.Diagnostics)
        {
            if (d.Contains("needs a pattern"))
            {
                genannt = true;
            }
        }

        AssertTrue(genannt,
            "**and the message says what it got instead** — the diagnostics "
                + "were: " + string.Join(" | ", mit.Diagnostics));
    }

    /// <summary>
    /// A bracket that never closes ends the pattern and does not break the
    /// file.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And it broke the whole file.</strong> The lexer read
    /// <c>/a[/</c>, set its "inside a character class" mark, and looked for
    /// a <c>]</c> that never came,
    /// <strong>so it ran to the end of the script and said
    /// <c>A regular expression opened at offset 9 is never closed</c></strong>
    /// — **a message about a pattern that ends there, and a game that writes
    /// one loses the rest of its file.**
    /// </para>
    /// <para>
    /// <strong>And the pattern is still incomplete, and the machine says so.</strong>
    /// <c>a[</c> is an unclosed character set in every engine including
    /// Ruby's,
    /// <strong>so "Unterminated [] set" is the right answer</strong> — **and
    /// the claim here is not that the pattern is valid, it is that the
    /// reader got as far as asking.</strong>
    /// </para>
    /// <para>
    /// <strong>And a closing bracket with no opening one is a
    /// character.</strong> That form was already right, and it is the other
    /// way round that went wrong: a mark that is set and never cleared.
    /// </para>
    /// </remarks>
    public void Test_ABracketThatNeverClosesEndsThePattern()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements("\"a[b\" =~ /a[/\n"));

        AssertTrue(wert.Kind == RubyValueKind.Nil,
            "**nothing came back, and the file is still whole** — and the "
                + "assertion is not that the pattern is valid, because `a[` "
                + "is an unclosed set in every engine; it is that the reader "
                + "got as far as asking, instead of running to the end of "
                + "the script and calling a valid pattern never closed");

        var genannt = false;
        foreach (var d in mit.Diagnostics)
        {
            if (d.Contains("could not be put together"))
            {
                genannt = true;
            }
        }

        AssertTrue(genannt,
            "**and the machine's own answer came through** — a warning about "
                + "nothing sends the reader looking; the diagnostics were: "
                + string.Join(" | ", mit.Diagnostics));

        // **`a]b` steht in dem Text, und ohne die Regel wuerde der Lexer
        // beim ersten `/` schliessen.**
        var mit2 = new RubyInterpreter(new RubyNullHost());
        var mit2Wert = mit2.RunProgram(Statements("\"xa]by\" =~ /a]b/\n"));
        AssertEq(AsInteger(mit2Wert), 1,
            "**and a closing bracket with no opening one is a character "
                + "too** — that form was already right, and it is the other "
                + "way round that went wrong: a mark that is set and never "
                + "cleared");
    }
}
