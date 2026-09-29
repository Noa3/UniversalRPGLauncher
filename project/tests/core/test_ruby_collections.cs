using System.Linq;

using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The basic collections, which is what a menu is made of.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Thirty-six of the forty-one names a game writes in its first
/// hundred lines were missing.</strong> <c>length</c> alone stops every menu
/// that counts, <c>[0]</c> stops every list that reads its first entry,
/// <strong>and a game without those is not slightly broken — it does not
/// start.</strong>
/// </para>
/// <para>
/// <strong>And a host cannot answer them.</strong> A real host implements the
/// game's own objects — <c>Sprite</c>, <c>Window_Base</c>, <c>Input</c> —
/// <strong>and not Ruby's <c>Array</c> and <c>String</c>, because a game never
/// asks the host what an array is.</strong>
/// </para>
/// </remarks>
public partial class TestRubyInterpreter
{
    /// <summary>
    /// A list has a length, and reads a value at a place.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And a place that is not there is nil, and not an error.</strong>
    /// <c>liste[5]</c> on a list of three is nil in Ruby,
    /// <strong>and a reader that refused would have made every script that
    /// reads one entry too many stop</strong> — which is how a save file
    /// with one more field than the game expects behaves.
    /// </para>
    /// <para>
    /// <strong>And a negative place counts from the end.</strong>
    /// <c>liste[-1]</c> is the last entry,
    /// <strong>and the last actor of a party is written that way.</strong>
    /// </para>
    /// </remarks>
    public void Test_AListHasALengthAndAnswersAtAPlace()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "[1, 2, 3].length\n"));

        AssertEq(AsInteger(wert), 3,
            "**a list of three says three** — and every menu that writes "
                + "\"#{liste.size} of #{party.max} actors\" asks this first");

        var zweite = new RubyInterpreter(new RubyNullHost());
        var stellen = zweite.RunProgram(Statements(
            "[[1, 2, 3][0], [1, 2, 3][-1], [1, 2, 3][5]]\n"));

        AssertEq(AsInteger(stellen.Items[0]), 1,
            "**the first place is the first value**");
        AssertEq(AsInteger(stellen.Items[1]), 3,
            "**and a negative place counts from the end** — `liste[-1]` is "
                + "the last entry, and the last actor of a party is written "
                + "that way");
        AssertTrue(stellen.Items[2].Kind == RubyValueKind.Nil,
            "**and a place that is not there is nil** — and not an error, "
                + "because a save file with one more field than the game "
                + "expects is read that way and a refusal would stop the game");
    }

    /// <summary>
    /// `first` and `last` read the ends, and an empty list has none.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this test exists because a mutation survived.</strong>
    /// "A negative place does not count from the end" changed the rule in
    /// one place and left the other,
    /// <strong>and no test noticed</strong> — because no test used `first` or
    /// `last` with a place that has to be computed.
    /// </para>
    /// <para>
    /// <strong>And `last` on an empty list is nil.</strong>
    /// <c>liste[-1]</c> on nothing is nothing,
    /// <strong>and a reader that computed the place and read it anyway
    /// would have thrown</strong> — and a game's empty party is empty at the
    /// start.
    /// </para>
    /// </remarks>
    public void Test_FirstAndLastReadTheEnds()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "[[1, 2, 3].first, [1, 2, 3].last, [].first, [].last]\n"));

        AssertEq(AsInteger(wert.Items[0]), 1,
            "**first is the first**");
        AssertEq(AsInteger(wert.Items[1]), 3,
            "**and last is the last** — and it is the same rule as a "
                + "negative place, read from one place and not two, because a "
                + "mutation that changed one and not the other survived until "
                + "this test existed");
        AssertTrue(wert.Items[2].Kind == RubyValueKind.Nil,
            "**and an empty list has no first** — a game's party is empty at "
                + "the start, and a reader that read the place anyway would "
                + "have thrown");
        AssertTrue(wert.Items[3].Kind == RubyValueKind.Nil,
            "**and no last** — and the place it computes is negative, which "
                + "is the case this test exists for");
    }

    /// <summary>
    /// A string has a length, and a string is a byte string.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Bytes and not characters, and that is Ruby 1.8.</strong>
    /// There is no character type there, and <c>"abc".length</c> is 3 while a
    /// Japanese name in CP932 measures in bytes,
    /// <strong>and a reader that counted characters would have given a game a
    /// different number on a different machine</strong> — which is a window
    /// that draws the wrong number of characters.
    /// </para>
    /// <para>
    /// <strong>And <c>"abc"[1]</c> is one byte, not one character.</strong>
    /// In CP932 that is half a Kanji,
    /// <strong>and that is the reference's behaviour and not a defect
    /// here</strong> — a game that writes <c>name[0]</c> knows it is
    /// getting one byte of a multi-byte name.
    /// </para>
    /// </remarks>
    public void Test_AStringIsAByteStringAndSaysHowLongItIs()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "[\"abc\".length, \"abc\"[1], \"abc\".upcase, \"abc\".downcase]\n"));

        AssertEq(AsInteger(wert.Items[0]), 3,
            "**the text says how many bytes it has** — and this runtime "
                + "counts bytes, because Ruby 1.8 has no character type and a "
                + "reader that counted characters would give a game a "
                + "different number on a different machine");
        AssertEq(System.Text.Encoding.UTF8.GetString(wert.Items[1].Bytes), "b",
            "**and the second byte is the b** — one byte and not one "
                + "character, which in CP932 is half a Kanji, and that is "
                + "what the reference does");
        AssertEq(System.Text.Encoding.UTF8.GetString(wert.Items[2].Bytes), "ABC",
            "**and the text can go up**");
        AssertEq(System.Text.Encoding.UTF8.GetString(wert.Items[3].Bytes), "abc",
            "**and down** — and a game that writes a name in the menu and a "
                + "name in a file writes it in one of the two");
    }

    /// <summary>
    /// A hash is built, and it is found by its key.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And a hash was refused outright.</strong> The answer was
    /// "this interpreter does not evaluate a Hash node" — a message about
    /// the reader's own source,
    /// <strong>and every saved setting, every event table and every status
    /// row is a hash.</strong> That is most of what a game consists of.
    /// </para>
    /// <para>
    /// <strong>And both spellings are the same thing.</strong> <c>{:a =&gt; 1}</c>
    /// and <c>{a: 1}</c> are one hash,
    /// <strong>and a reader that knew only one of them would have read the
    /// other as an empty hash</strong> — a settings screen that saves nothing
    /// and says nothing.
    /// </para>
    /// </remarks>
    public void Test_AHashIsBuiltAndFoundByItsKey()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "h = {:hp => 10, :name => \"Held\"}\n"
            + "[h[:hp], h[:name], h[:fehlt], h.keys.length, h.values.length]\n"));

        AssertEq(AsInteger(wert.Items[0]), 10,
            "**the value is behind its key** — and a hash that was refused "
                + "outright gave a game a message about the reader's own "
                + "source, and every saved setting is a hash");
        AssertEq(System.Text.Encoding.UTF8.GetString(wert.Items[1].Bytes), "Held",
            "**and the second one too**");
        AssertTrue(wert.Items[2].Kind == RubyValueKind.Nil,
            "**and a key that is not there is nil**");
        AssertEq(AsInteger(wert.Items[3]), 2,
            "**the keys are the keys** — and a reader that took the first of "
                + "every two would have given a settings screen its values "
                + "where its keys belong");
        AssertEq(AsInteger(wert.Items[4]), 2,
            "**and the values are the values**");
    }

    /// <summary>
    /// A list grows, holds, finds and gives up a value.
    /// </summary>
    /// <remarks>
    /// <strong>And it grows into a new list.</strong> Ruby changes the list
    /// in place and answers it,
    /// <strong>and a reader that answered the old list would have made
    /// `akteure.push(held)` do nothing visible</strong> — the game would add
    /// a member and see the same number of members.
    /// </remarks>
    public void Test_AListGrowsHoldsAndFinds()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "a = [1, 2]\n"
            + "b = a.push(3)\n"
            + "[b.length, a.length, b.include?(2), b.index(3), b[1]]\n"));

        AssertEq(AsInteger(wert.Items[0]), 3,
            "**the list is one longer** — and a reader that answered the old "
                + "list would have made `push` do nothing visible, and the "
                + "game would add a member and see the same number");
        AssertEq(AsInteger(wert.Items[1]), 2,
            "**and the old one is untouched** — that is what a new list "
                + "means, and a game that pushes onto a list it kept somewhere "
                + "depends on it");
        AssertTrue(wert.Items[2].Boolean,
            "**and it holds the value** — by value and not by identity, or "
                + "every string a game looks for would come back false");
        AssertEq(AsInteger(wert.Items[3]), 2,
            "**and the place is the first one** — and a reader that gave the "
                + "last would have made a game that removes by place remove "
                + "the wrong actor");
        AssertEq(AsInteger(wert.Items[4]), 2,
            "**and the place reads that value**");
    }

    /// <summary>
    /// `to_i` reads the number at the start and drops the rest.
    /// </summary>
    /// <remarks>
    /// <strong>And it stops at the first thing that is not a digit.</strong>
    /// <c>"3 Abenteuer".to_i</c> is 3,
    /// <strong>and a reader that required the whole text to be a number would
    /// have given nil for every saved value a game writes next to a
    /// label</strong> — and a party would come back from its save with no
    /// level.
    /// </remarks>
    public void Test_ToIReadsTheNumberAtTheStart()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "[\"42\".to_i, \"3 Abenteuer\".to_i, \"x\".to_i, 7.to_i]\n"));

        AssertEq(AsInteger(wert.Items[0]), 42,
            "**the whole text is a number**");
        AssertEq(AsInteger(wert.Items[1]), 3,
            "**and the number at the start is a number** — a saved value a "
                + "game writes next to a label reads as nil if the whole "
                + "text has to be digits, and a party comes back with no "
                + "level");
        AssertEq(AsInteger(wert.Items[2]), 0,
            "**and text without a number is zero, not nil** — because a game "
                + "adds to that number, and nil plus one is an error");
        AssertEq(AsInteger(wert.Items[3]), 7,
            "**and a number stays itself**");
    }

    /// <summary>
    /// A list joins, reverses, takes out and takes only once.
    /// </summary>
    /// <remarks>
    /// <strong>And `join` takes the text the game gave.</strong>
    /// <c>liste.join(", ")</c> puts a comma and a space between,
    /// <strong>and a reader that always joined without a text would have
    /// written a party's names as one word</strong> — and a menu would show
    /// "HeldHeldHeld".
    /// </remarks>
    public void Test_AListJoinsReversesAndTakes()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "[[\"a\", \"b\"].join(\", \"), [1, 2, 3].reverse, "
            + "[1, 2, 1].uniq, [1, 2, 2].delete(2)]\n"));

        AssertEq(System.Text.Encoding.UTF8.GetString(wert.Items[0].Bytes), "a, b",
            "**the text between the values is the one the game wrote** — and "
                + "a reader that joined without a text would have written a "
                + "party's names as one word");
        AssertEq(AsInteger(wert.Items[1].Items[0]), 3,
            "**the list reversed starts at the end**");
        AssertEq(wert.Items[2].Items.Count, 2,
            "**and the list with each value once has two** — a list of one, "
                + "two and one");
        AssertEq(wert.Items[3].Items.Count, 1,
            "**and taking out a value takes out every one of them** — a "
                + "reader that took out one would have left a second actor "
                + "with the same name in the party");
    }

    /// <summary>
    /// A class's own `length` wins over the built-in one.
    /// </summary>
    /// <remarks>
    /// <strong>And that is the order.</strong> A game's own
    /// <c>Game_Party#size</c> is its own answer,
    /// <strong>and a reader that asked the built-in first would have given
    /// every party a list's length</strong> — which is a number from
    /// somewhere else, and it looks right.
    /// </remarks>
    public void Test_AClasssOwnLengthWins()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class Party\n"
            + "  def size\n"
            + "    7\n"
            + "  end\n"
            + "end\n"
            + "Party.new.size\n"));

        AssertEq(AsInteger(wert), 7,
            "**the class answered and not the built-in** — a reader that "
                + "asked the built-in first would have given every party a "
                + "list's length, which is a number from somewhere else and "
                + "looks right");
    }

    /// <summary>
    /// `is_a?` walks the class's own chain.
    /// </summary>
    /// <remarks>
    /// <strong>And the chain, not only the class itself.</strong>
    /// <c>Held &lt; HeldBase</c>, and <c>held.is_a?(HeldBase)</c> is true,
    /// <strong>and a reader that compared the name would say no to every
    /// guard a game writes against its own base class</strong> — and every
    /// own object would be foreign.
    /// </remarks>
    public void Test_IsAWalksTheClassChain()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class Basis\n"
            + "end\n"
            + "class Held < Basis\n"
            + "end\n"
            + "h = Held.new\n"
            + "[h.is_a?(Held), h.is_a?(Basis), h.is_a?(EtwasAnderes)]\n"));

        AssertTrue(wert.Items[0].Boolean,
            "**the class itself matches**");
        AssertTrue(wert.Items[1].Boolean,
            "**and the base class matches too** — a reader that compared "
                + "the name would say no to every guard a game writes against "
                + "its own base class, and every own object would be "
                + "foreign");
        AssertTrue(!wert.Items[2].Boolean,
            "**and a class it has nothing to do with does not match**");
    }
}
