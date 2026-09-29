using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// `break` and `next`, and a copy that does not share what can be written.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And `break` with a value is the sentence a search is written
/// in.</strong> <c>[1,2,3].each { |x| break x if x &gt; 1 }</c>,
/// <strong>and before this the reader answered nil</strong> — a game that
/// looks for a thing in a list got nothing and no message.
/// </para>
/// <para>
/// <strong>And <c>dup</c> made a second name for the same thing.</strong>
/// Measured: <c>a.dup.n = 2</c> left <c>a.n</c> at two as well,
/// <strong>and a game that makes two actors from one template would have had
/// one actor twice, with every change on one visible on the other</strong>.
/// </para>
/// </remarks>
public partial class TestRubyInterpreter
{
    /// <summary>
    /// `break` ends a loop and carries a value out of it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the value is the loop's answer.</strong> That is the whole
    /// reason <c>break</c> takes one, **and a reader that answered nil would
    /// make a game's search return nothing at all.**
    /// </para>
    /// <para>
    /// <strong>And a <c>while</c> with a <c>break</c> in it comes back.</strong>
    /// Measured before: <c>while true; break 7; end</c> ran to the step limit
    /// and threw <em>this script ran 2000000 steps without finishing</em> —
    /// **and a game that waits for a condition would hang for two million
    /// steps and then stop, and the stop is the only thing the player sees.**
    /// </para>
    /// </remarks>
    public void Test_BreakEndsALoopAndCarriesAValue()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "r = while true\n"
            + "  break 7\n"
            + "end\n"
            + "r\n"));

        AssertEq(wert.Integer, 7,
            "**the loop came back with seven** — measured before: 2000000 "
                + "steps and *this script ran 2000000 steps without "
                + "finishing*, and a game that waits for a condition would "
                + "hang and then stop, and the stop is the only thing the "
                + "player sees");
        AssertEq(mit.Diagnostics.Count, 0,
            "**and nothing was said about it** — the first version answered "
                + "*this interpreter does not evaluate a Break node*, once "
                + "per run, and a game's own `break` would have filled the "
                + "diagnostics with a message about the reader's source");
    }

    /// <summary>
    /// `next` skips the rest of one turn and goes on.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And `next` is not a `break` with another spelling.</strong>
    /// **A reader with one flag would have ended the loop on `next`** — and
    /// every `next` in every enumerator would have been a `break`.
    /// </para>
    /// </remarks>
    public void Test_NextSkipsTheRestOfOneTurnAndGoesOn()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "r = 0\n"
            + "[1, 2, 3].each do |x|\n"
            + "  next if x == 2\n"
            + "  r += x\n"
            + "end\n"
            + "r\n"));

        AssertEq(wert.Integer, 4,
            "**one and three were added and two was skipped** — a reader "
                + "with one flag for both would have ended the loop at the "
                + "first `next`, and every `next` in every enumerator would "
                + "have been a `break`");
    }

    /// <summary>
    /// `dup` makes a second thing and not a second name for the first.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the class comes along.</strong> Measured: after the copy
    /// was built as a plain list, <c>b.n = 2</c> said <em>a value has no
    /// method 'n=' on this host</em> — **and the message named the host for a
    /// missing method, when the real problem was that the copy had lost its
    /// class.**
    /// </para>
    /// <para>
    /// <strong>And a list is a new list.</strong>
    /// <c>l = [1,2]; d = l.dup; d.push(3)</c> leaves <c>l</c> at two entries,
    /// **and a reader that handed back the same list would have three in
    /// both** — and a game's party list would grow when a plugin took a copy
    /// of it.
    /// </para>
    /// </remarks>
    public void Test_DupMakesASecondThingAndNotASecondName()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class Held\n"
            + "  def n=(v)\n"
            + "    @n = v\n"
            + "  end\n"
            + "  def n\n"
            + "    @n\n"
            + "  end\n"
            + "end\n"
            + "a = Held.new\n"
            + "a.n = 1\n"
            + "b = a.dup\n"
            + "b.n = 2\n"
            + "[a.n, b.n, a.object_id == b.object_id]\n"));

        AssertEq(wert.Items[0].Integer, 1,
            "**the first still has one** — measured before: two, and a game "
                + "that makes two actors from one template would have had one "
                + "actor twice, with every change on one visible on the other");
        AssertEq(wert.Items[1].Integer, 2,
            "**and the second has two** — the copy carries the class, and a "
                + "reader that built it as a plain list said *a value has no "
                + "method 'n=' on this host*, naming the host for a missing "
                + "method when the real problem was that the copy had lost "
                + "its class");
        AssertTrue(wert.Items[2].Kind == RubyValueKind.Boolean
            && !wert.Items[2].Boolean,
            "**and they are two objects**");

        var mit2 = new RubyInterpreter(new RubyNullHost());
        var liste = mit2.RunProgram(Statements(
            "l = [1, 2]\n"
            + "d = l.dup\n"
            + "d.push(3)\n"
            + "[l.length, d.length, l.include?(3)]\n"));
        AssertEq(liste.Items[0].Integer, 2,
            "**the original list still has two** — a reader that handed back "
                + "the same list would have three in both, and a game's party "
                + "list would grow when a plugin took a copy of it");
        AssertEq(liste.Items[1].Integer, 3,
            "**and the copy has three**");
        AssertTrue(liste.Items[2].Kind == RubyValueKind.Boolean
            && !liste.Items[2].Boolean,
            "**and the original does not have the new entry**");
    }

    /// <summary>
    /// `next` skips the rest of the turn, `break` ends the run, and
    /// `break 7` is seven.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the sentence after the control word is skipped, and
    /// that is the whole point.</strong>
    /// <c>each { |x| n += 1; next if x == 2; n += 10 }</c> is 23 and not
    /// 33,
    /// **and a reader that ran the rest of the body would have added the
    /// ten a second time** — and a game's filter would let through the
    /// very entries it is filtering out.
    /// </para>
    /// <para>
    /// <strong>And <c>break</c> ends the run, and not only the turn.</strong>
    /// <c>each { |x| n += 1; break if x == 2 }</c> is 2,
    /// **and a reader that made <c>break</c> a <c>next</c> would have gone
    /// through all three and the counter would say 3** — and a search that
    /// gives up would have read every entry.
    /// </para>
    /// <para>
    /// <strong>And the value goes with it.</strong> <c>l.each { |x| break 7
    /// if x == 2 }</c> is 7,
    /// **and a reader that dropped the value would answer the list and a
    /// game would store the whole list in a field it expected to hold one
    /// number.**
    /// </para>
    /// <para>
    /// <strong>And these were three surviving mutations</strong>, **because
    /// no test held any of the three sentences** — a test that holds
    /// <c>break 7</c> out of a <c>while</c> and not out of an <c>each</c>
    /// has not held the sentence, it has held a spelling of it.
    /// </para>
    /// </remarks>
    public void Test_ControlWordsEndWhatTheyName()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "n = 0\n"
            + "[1,2,3].each { |x| n += 1; next if x == 2; n += 10 }\n"
            + "m = 0\n"
            + "[1,2,3].each { |x| m += 1; break if x == 2 }\n"
            + "[n, m, [1,2,3].each { |x| break 7 if x == 2 }]\n"));

        AssertEq(wert.Items[0].Integer, 23,
            "**`next` skipped the ten** — one plus one plus one plus the "
                + "two tens is 23 and not 33, and a reader that ran the rest "
                + "of the body would have let through the very entries the "
                + "filter is filtering out");
        AssertEq(wert.Items[1].Integer, 2,
            "**`break` ended the run after two** — and a reader that made"
                + " `break` a `next` would have gone through all three and the"
                + " counter would say 3, and a search that gives up would"
                + " have read every entry");
        AssertEq(wert.Items[2].Integer, 7,
            "**and `break 7` is seven** — and a reader that dropped the"
                + " value would answer the list, and a game would store the"
                + " whole list in a field it expected to hold one number");
        AssertEq(mit.Diagnostics.Count, 0,
            "**and nothing was said** — measured before: *this interpreter"
                + " does not evaluate a Break node*, once per run, and a"
                + " game's own `break` filled the diagnostics with a"
                + " sentence about the reader's source");
    }

    /// <summary>
    /// A setter written by hand parses, and `==` is not one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the <c>=</c> is part of the name.</strong>
    /// <c>attr_writer</c> builds exactly such a method, **and a reader that
    /// stopped at the identifier would have read <c>n</c> and then found an
    /// <c>=</c> where a parameter list belongs** — and every game that writes
    /// a setter by hand would be a syntax error while <c>attr_writer</c> in
    /// the same file worked.
    /// </para>
    /// <para>
    /// <strong>And the failure named the wrong thing.</strong> The message was
    /// <em>A member name was expected at offset 44, but 'end' is there</em> —
    /// **and the name was right: the second read had eaten it.**
    /// </para>
    /// <para>
    /// <strong>And <c>def ==(other)</c> compares.</strong> A reader that took
    /// the first <c>=</c> would have named the method <c>==</c> and then
    /// complained about the second one.
    /// </para>
    /// </remarks>
    public void Test_ASetterWrittenByHandParses()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class A\n"
            + "  def n=(v)\n"
            + "    @n = v * 2\n"
            + "  end\n"
            + "  def n\n"
            + "    @n\n"
            + "  end\n"
            + "  def ==(other)\n"
            + "    n == other.n\n"
            + "  end\n"
            + "end\n"
            + "a = A.new\n"
            + "a.n = 5\n"
            + "b = A.new\n"
            + "b.n = 5\n"
            + "[a.n, a == b]\n"));

        AssertEq(wert.Items[0].Integer, 10,
            "**the setter ran** — measured before: *A member name was "
                + "expected at offset 44, but 'end' is there*, and a game "
                + "that writes a setter by hand would be a syntax error while "
                + "`attr_writer` in the same file worked");
        AssertTrue(wert.Items[1].Kind == RubyValueKind.Boolean
            && wert.Items[1].Boolean,
            "**and `==` is still a comparison** — a reader that took the "
                + "first `=` would have named the method `==` and then "
                + "complained about the second one");
        AssertEq(mit.Diagnostics.Count, 0,
            "**and nothing was said**");
    }
}
