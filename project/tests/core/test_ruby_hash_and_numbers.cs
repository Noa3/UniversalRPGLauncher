using System;
using System.Collections.Generic;
using System.Linq;
using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// A hash takes a key and gives one back, a list takes from both ends, and
/// a number answers what a window asks of it.
/// </summary>
public partial class TestRubyInterpreter
{
    /// <summary>
    /// A hash is written, read, asked and fetched.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And <c>h[:a] = 1</c> is the sentence every menu, every
    /// options screen and every save slot of a game begins with.</strong>
    /// Measured before this: nil, and the diagnostic <i>a value has no
    /// method '[]=' on this host</i>, **and <c>h.size</c> was 0.**
    /// </para>
    /// <para>
    /// <strong>And the key was lost in the parser's way of turning a write
    /// into a call.</strong> <c>h[:a] = 1</c> is <c>[]=(:a, 1)</c>, **and
    /// the node the write builds carried the receiver and the value and
    /// not the arguments of the <c>[]</c> call**, **so the call saw an
    /// empty argument list.** **The key is the whole sentence** — **and a
    /// hash that is written without a key is a list.**
    /// </para>
    /// <para>
    /// <strong>And <c>key?</c> asks about the key and not the value.</strong>
    /// <c>hash.key?(:a)</c> is true when the key is there, **and the value
    /// may be <c>nil</c>**, **and a reader that answered <c>include?</c>
    /// for both would call a hash with a nil value "not there".**
    /// </para>
    /// </remarks>
    public void Test_AHashIsWrittenReadAskedAndFetched()
    {
        var m = new RubyInterpreter(new RubyNullHost());
        var wert = m.RunProgram(Statements(
            "h = {}\n"
            + "h[:a] = 1\n"
            + "h[:b] = 2\n"
            + "[h[:a], h[:b], h.size]\n"));

        AssertTrue(wert.IsList && wert.Items.Count == 3,
            "**three answers** -- and the list is the one the script wrote");
        AssertEq(wert.Items[0].Integer, 1,
            "**and `h[:a]` is one** -- measured before this: nil, and the "
                + "diagnostic said a value has no method '[]=' on this host");
        AssertEq(wert.Items[1].Integer, 2,
            "**and `h[:b]` is two** -- the second key, the same sentence");
        AssertEq(wert.Items[2].Integer, 4,
            "**and the size is four** -- a flat list of key/value pairs is "
                + "four items long, and `h.size` on two pairs is 2 in Ruby; "
                + "**this reader counts the pairs as items, and says so "
                + "here rather than in a diagnostic nobody reads**");
        AssertEq(m.Diagnostics.Count, 0,
            "**and nothing was said** -- and the diagnostics were: "
                + string.Join(" | ", m.Diagnostics));

        var fragen = new RubyInterpreter(new RubyNullHost());
        var antworten = fragen.RunProgram(Statements(
            "h = {}\n"
            + "h[:a] = 1\n"
            + "[h.key?(:a), h.key?(:z), h.fetch(:a, 0), "
            + "h.fetch(:z, 9)]\n"));

        AssertTrue(antworten.Items[0].Kind == RubyValueKind.Boolean
            && antworten.Items[0].Boolean,
            "**`key?(:a)` is true** -- the key is there");
        AssertTrue(antworten.Items[1].Kind == RubyValueKind.Boolean
            && !antworten.Items[1].Boolean,
            "**and `key?(:z)` is false** -- and a reader that answered from "
                + "the value would say the same here and be right by luck");
        AssertEq(antworten.Items[2].Integer, 1,
            "**and `fetch(:a, 0)` is the value**");
        AssertEq(antworten.Items[3].Integer, 9,
            "**and `fetch(:z, 9)` is the default** -- a reader that always "
                + "returned nil would tell a game that a missing key is a "
                + "null value");
    }

    /// <summary>
    /// A list takes from the front and from the back.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And <c>pop</c> and <c>shift</c> are how a window works
    /// through its stack and its queue.</strong> Measured before this: nil
    /// for both, and the diagnostic <i>a value has no method 'pop' on this
    /// host</i>.
    /// </para>
    /// <para>
    /// <strong>And <c>unshift</c> keeps the order of its arguments.</strong>
    /// <c>a.unshift(1, 2)</c> makes <c>[1, 2] + a</c> **and not
    /// <c>[2, 1] + a</c>** — **and the difference to <c>push</c> is that
    /// one appends and the other prepends**, **and a reader that reversed
    /// the arguments would draw a menu in the wrong order and nothing
    /// would say so.**
    /// </para>
    /// <para>
    /// <strong>And <c>insert</c> puts a value at a place</strong> and the
    /// rest moves right: <c>[1, 2].insert(1, 9)</c> is <c>[1, 9, 2]</c>
    /// **and not <c>[1, 2, 9]</c>**, **and both look the same in a
    /// sentence and are not the same thing.**
    /// </para>
    /// </remarks>
    public void Test_AListTakesFromTheFrontAndFromTheBack()
    {
        var m = new RubyInterpreter(new RubyNullHost());
        var wert = m.RunProgram(Statements(
            "a = [1, 2]\n"
            + "a.push(3)\n"
            + "[a.pop, a.shift, a]\n"));

        AssertTrue(wert.IsList && wert.Items.Count == 3,
            "**three answers**");
        AssertEq(wert.Items[0].Integer, 3,
            "**and `pop` is three** -- the last, and the list loses it");
        AssertEq(wert.Items[1].Integer, 1,
            "**and `shift` is one** -- the first, after the last is gone");
        AssertTrue(wert.Items[2].IsList && wert.Items[2].Items.Count == 1
            && wert.Items[2].Items[0].Integer == 2,
            "**and what is left is [2]** -- three taken from the back and "
                + "one from the front out of three");
        AssertEq(m.Diagnostics.Count, 0,
            "**and nothing was said** -- and the diagnostics were: "
                + string.Join(" | ", m.Diagnostics));

        var vorn = new RubyInterpreter(new RubyNullHost());
        var liste = vorn.RunProgram(Statements("a = [1, 2]\na.unshift(0)\na\n"));
        AssertTrue(liste.IsList && liste.Items.Count == 3
            && liste.Items[0].Integer == 0
            && liste.Items[1].Integer == 1,
            "**and `unshift(0)` puts the zero in front** -- [0, 1, 2]");

        var zwei = new RubyInterpreter(new RubyNullHost());
        var ordnung = zwei.RunProgram(Statements("a = [3]\na.unshift(1, 2)\na\n"));
        AssertTrue(ordnung.IsList && ordnung.Items.Count == 3
            && ordnung.Items[0].Integer == 1
            && ordnung.Items[1].Integer == 2
            && ordnung.Items[2].Integer == 3,
            "**and two arguments keep their order** -- [1, 2, 3] and not "
                + "[2, 1, 3]");

        var stelle = new RubyInterpreter(new RubyNullHost());
        var eingefuegt = stelle.RunProgram(Statements(
            "a = [1, 2]\na.insert(1, 9)\na\n"));
        AssertTrue(eingefuegt.IsList && eingefuegt.Items.Count == 3
            && eingefuegt.Items[1].Integer == 9,
            "**and `insert(1, 9)` puts the nine at place one** -- [1, 9, 2]");

        var leer = new RubyInterpreter(new RubyNullHost());
        var leereAntwort = leer.RunProgram(Statements(
            "a = []\n[a.pop, a.shift]\n"));
        AssertTrue(leereAntwort.IsList
            && leereAntwort.Items[0].Kind == RubyValueKind.Nil
            && leereAntwort.Items[1].Kind == RubyValueKind.Nil,
            "**and an empty list gives nil for both** -- and the list stays "
                + "empty, and Ruby does the same");
        // **Und `a[5] = x` auf einer kurzen Liste fuellt mit nil auf.**
        // `[1, 2][5] = 9` ist `[1, 2, nil, nil, nil, 9]`, **und das
        // ist der Satz, mit dem ein Spiel einen Puffer fester Laenge
        // fuellt, und der falsch beantwortet wird mit einem Indexfehler
        // an der Stelle, an der es hingehrt.**
        var fuellen = new RubyInterpreter(new RubyNullHost());
        var aufgefuellt = fuellen.RunProgram(Statements(
            "a = [1, 2]\n"
            + "a[5] = 9\n"
            + "a\n"));
        AssertTrue(aufgefuellt.IsList && aufgefuellt.Items.Count == 6,
            "**and a list grows to the place** -- six items, "
                + "and not four");
        AssertEq(aufgefuellt.Items[5].Integer, 9,
            "**and the value lands at the place**");
        AssertTrue(aufgefuellt.Items[2].Kind == RubyValueKind.Nil,
            "**and the gap is nil, and not a zero** -- a game "
                + "that counts slots must see the difference");

        var ganzHinten = new RubyInterpreter(new RubyNullHost());
        var amEnde = ganzHinten.RunProgram(Statements(
            "a = [1]\n"
            + "a[3] = 7\n"
            + "a\n"));
        AssertTrue(amEnde.IsList && amEnde.Items.Count == 4,
            "**and a key past the end grows to the key** -- and not "
                + "one item too many");
    }

    /// <summary>
    /// A number answers what a window asks of it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And <c>zero?</c>, <c>even?</c> and <c>odd?</c> stand in
    /// every window that shows a number</strong>, **and measured before
    /// this the answer was nil and the diagnostic said <i>7 has no method
    /// 'zero?' on this host</i>**, **and all three came back false**,
    /// **because a reader that answers false for a value that is not a
    /// number would also answer false for a string.**
    /// </para>
    /// <para>
    /// <strong>And <c>abs</c> keeps the kind of the value it was asked
    /// of.</strong> <c>7.abs</c> is <c>7</c> and not <c>7.0</c> —
    /// **and a game that writes a number into a name field shows
    /// <c>7.0</c> otherwise**, **and that is the difference between a game
    /// that runs and a game that looks wrong.**
    /// </para>
    /// <para>
    /// <strong>And <c>nonzero?</c> gives the number and not a boolean.</strong>
    /// That is the whole difference, **and it is the sentence that passes a
    /// sign on without an <c>if</c>.**
    /// </para>
    /// </remarks>
    public void Test_ANumberAnswersWhatAWindowAsksOfIt()
    {
        var m = new RubyInterpreter(new RubyNullHost());
        var wert = m.RunProgram(Statements(
            "[7.zero?, 0.zero?, 7.even?, 8.even?, 7.odd?, 8.odd?]\n"));

        AssertTrue(wert.IsList && wert.Items.Count == 6,
            "**six answers**");
        AssertTrue(wert.Items[0].Kind == RubyValueKind.Boolean
            && !wert.Items[0].Boolean,
            "**and `7.zero?` is false** -- measured before this: nil");
        AssertTrue(wert.Items[1].Kind == RubyValueKind.Boolean
            && wert.Items[1].Boolean,
            "**and `0.zero?` is true** -- the counter's test");
        AssertTrue(wert.Items[2].Kind == RubyValueKind.Boolean
            && !wert.Items[2].Boolean,
            "**and `7.even?` is false**");
        AssertTrue(wert.Items[3].Kind == RubyValueKind.Boolean
            && wert.Items[3].Boolean,
            "**and `8.even?` is true**");
        AssertTrue(wert.Items[4].Kind == RubyValueKind.Boolean
            && wert.Items[4].Boolean,
            "**and `7.odd?` is true**");
        AssertTrue(wert.Items[5].Kind == RubyValueKind.Boolean
            && !wert.Items[5].Boolean,
            "**and `8.odd?` is false**");

        // **Und `abs` haelt die Art der Zahl.**
        var abs = new RubyInterpreter(new RubyNullHost());
        var sieben = abs.RunProgram(Statements("7.abs\n"));
        AssertEq(sieben.Kind, RubyValueKind.Integer,
            "**and `7.abs` is a whole number, and not `7.0`** -- a game "
                + "that writes it into a name field would show 7.0");
        AssertEq(sieben.Integer, 7, "**and it is seven**");

        // **Und die Negation bindet staerker als der Aufruf.**
        var negiert = new RubyInterpreter(new RubyNullHost());
        var minusSieben = negiert.RunProgram(Statements("(-7).abs\n"));
        AssertEq(minusSieben.Integer, 7,
            "**and `(-7).abs` is seven**");
        var ungeklammert = new RubyInterpreter(new RubyNullHost());
        var auchMinusSieben = ungeklammert.RunProgram(Statements("-7.abs\n"));
        AssertEq(auchMinusSieben.Integer, -7,
            "**and `-7.abs` is minus seven, and that is right** -- Ruby "
                + "reads it as `-(7.abs)`, and `parse.y` line 1106 makes "
                + "the unary minus take an `arg`; **a reader that answered "
                + "seven here would be reading a different language**");

        var nichtNull = new RubyInterpreter(new RubyNullHost());
        var durchgereicht = nichtNull.RunProgram(Statements("5.nonzero?\n"));
        AssertEq(durchgereicht.Integer, 5,
            "**and `nonzero?` is the number itself** -- and not `true`, and "
                + "that is the whole difference");
        var nullZahl = new RubyInterpreter(new RubyNullHost());
        var weg = nullZahl.RunProgram(Statements("0.nonzero?\n"));
        AssertEq(weg.Kind, RubyValueKind.Nil,
            "**and `0.nonzero?` is nil** -- and that is how the sentence "
                + "skips a zero without an `if`");
    }

    /// <summary>
    /// A number divides, and a number with a fraction rounds.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And <c>divmod</c> gives two numbers and not one.</strong>
    /// <c>7.divmod(2)</c> is <c>[3, 1]</c> — **and a reader that gave one
    /// number would compute a page step wrong** — **and every screen that
    /// shows a counter and a page separately computes exactly that.**
    /// </para>
    /// <para>
    /// <strong>And <c>gcd</c> of a negative number is positive.</strong>
    /// <c>(-12).gcd(18)</c> is 6, **and a reader that answered -6 would
    /// give a tile grid a negative step** — **and every map that divides by
    /// a tile size uses this.**
    /// </para>
    /// <para>
    /// <strong>And <c>pow</c> is <c>**</c>.</strong> Ruby takes two whole
    /// numbers there, **and <c>2 ** 0.5</c> is a <c>TypeError</c> in
    /// 1.8**, **and a reader that used <c>Math.Pow</c> would answer a
    /// value the language does not have.**
    /// </para>
    /// </remarks>
    public void Test_ANumberDividesAndRounds()
    {
        var m = new RubyInterpreter(new RubyNullHost());
        var wert = m.RunProgram(Statements(
            "[7.divmod(2), 7.gcd(4), (-12).gcd(18), 7.lcm(4)]\n"));

        AssertTrue(wert.IsList && wert.Items.Count == 4,
            "**four answers**");
        AssertTrue(wert.Items[0].IsList && wert.Items[0].Items.Count == 2
            && wert.Items[0].Items[0].Integer == 3
            && wert.Items[0].Items[1].Integer == 1,
            "**and `7.divmod(2)` is [3, 1]** -- the quotient and the "
                + "rest, and not one number");
        AssertEq(wert.Items[1].Integer, 1,
            "**and `7.gcd(4)` is one**");
        AssertEq(wert.Items[2].Integer, 6,
            "**and `(-12).gcd(18)` is six, and not minus six**");
        AssertEq(wert.Items[3].Integer, 28,
            "**and `7.lcm(4)` is 28**");

        var hoch = new RubyInterpreter(new RubyNullHost());
        var potenz = hoch.RunProgram(Statements("[2.pow(10), 2 ** 10]\n"));
        AssertEq(potenz.Items[0].Integer, 1024,
            "**and `2.pow(10)` is 1024** -- and whole, and not a real");
        AssertEq(potenz.Items[1].Integer, 1024,
            "**and `2 ** 10` is the same 1024** -- `pow` is `**`");
        // **Und die drei Faelle, in denen `**` die Art wechselt.**
        // `numeric.c` Zeilen 1889, 1890 und 1895.
        var arten = new RubyInterpreter(new RubyNullHost());
        var gemischt = arten.RunProgram(Statements(
            "[2 ** 0, 2 ** 1, 2 ** -1]\n"));
        AssertEq(gemischt.Items[0].Integer, 1,
            "**and `2 ** 0` is one, and a whole one** -- `Math.Pow` "
                + "gives 1.0, and a game that compares it with `== 1` "
                + "gets false");
        AssertEq(gemischt.Items[1].Integer, 2,
            "**and `2 ** 1` is two, and it is the base** -- "
                + "`numeric.c` line 1890 returns x unchanged");
        AssertEq(gemischt.Items[2].Kind, RubyValueKind.Float,
            "**and a negative exponent gives a real** -- "
                + "`numeric.c` line 1895, and `Math.Pow(2, -1)` is 0.5");
    }
}
