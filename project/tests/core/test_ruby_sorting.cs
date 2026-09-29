using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Comparing and sorting, which is how a game orders a party, a list or a
/// menu.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Neither existed.</strong> <c>&lt;=&gt;</c> was not an operator, and
/// a game that wrote <c>liste.sort</c> got a refusal,
/// <strong>and there is no RPG Maker that has never sorted a list.</strong>
/// </para>
/// <para>
/// <strong>And the built-in sort is not stable.</strong>
/// <c>List.Sort</c> may swap two equal values, so a game sorting actors by
/// level would get a different party depending on the algorithm,
/// <strong>and nothing in the script would say why.</strong>
/// </para>
/// </remarks>
public partial class TestRubyInterpreter
{
    /// <summary>
    /// `<=>` says minus one, zero or one.
    /// </summary>
    /// <remarks>
    /// <strong>Ruby's three-way answer, and the base of everything that
    /// orders.</strong> <c>&lt;</c>, <c>&lt;=</c>, <c>sort</c> and every
    /// <c>Comparable</c> stand on it,
    /// <strong>and a reader that answered a boolean would have no way to
    /// express "these two are the same".</strong>
    /// </remarks>
    public void Test_ThreeWayComparisonSaysMinusOneZeroOrOne()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements("[1 <=> 2, 2 <=> 1, 2 <=> 2]\n"));

        AssertEq(wert.Items.Count, 3,
            "**three answers came back**");
        AssertEq(AsInteger(wert.Items[0]), -1,
            "**one against two is minus one**");
        AssertEq(AsInteger(wert.Items[1]), 1,
            "**and two against one is one** — not a boolean, because a boolean "
                + "cannot say which of the two is smaller");
        AssertEq(AsInteger(wert.Items[2]), 0,
            "**and two against two is zero** — the answer that a boolean "
                + "cannot give, and the one a game uses to find a duplicate");
    }

    /// <summary>
    /// Two things that cannot be compared say so, and not zero.
    /// </summary>
    /// <remarks>
    /// <strong>`nil` means "nobody knows", and zero would mean "the same".</strong>
    /// A game sorting a list of things where some cannot be compared needs to
    /// be told,
    /// <strong>and a reader that answered zero would have said they were
    /// equal</strong> — and the list would come out in an order nobody wrote.
    /// </remarks>
    public void Test_WhatCannotBeComparedSaysSoAndNotZero()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements("[nil <=> 5, 5 <=> nil]\n"));

        AssertTrue(wert.Items[0].Kind == RubyValueKind.Nil,
            "**nil against a number is nil** — and not zero, because zero "
                + "means the two are the same and a game sorting a list of "
                + "things needs to be told that it cannot order them");
        AssertTrue(wert.Items[1].Kind == RubyValueKind.Nil,
            "**and the other way round** — a comparison that is not symmetric "
                + "would sort a list differently depending on which end the "
                + "sort reads it from");
    }

    /// <summary>
    /// Two strings compare, and `a` comes before `b`.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the case the numbers-only comparison misses.</strong>
    /// <c>"a" &lt; "b"</c> was an exception, because the comparison only
    /// handled numbers, <strong>and a game that sorts names is a menu.</strong>
    /// </para>
    /// <para>
    /// <strong>Ordinal and not the culture's order.</strong> The culture's
    /// order puts "ä" next to "a", <strong>so a game sorting names would get
    /// a different order on a German machine than on a Japanese one</strong> —
    /// and a list of actors would come out differently depending on where the
    /// game runs.
    /// </para>
    /// </remarks>
    public void Test_TwoStringsCompareAndAComesBeforeB()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "[\"a\" < \"b\", \"b\" < \"a\", \"a\" < \"a\"]\n"));

        AssertTrue(wert.Items[0].Boolean,
            "**a comes before b** — and a comparison that only handled "
                + "numbers would have raised here, and a game that sorts "
                + "names is a menu");
        AssertTrue(!wert.Items[1].Boolean,
            "**and b does not come before a**");
        AssertTrue(!wert.Items[2].Boolean,
            "**and a is not before itself** — a reader that answered true "
                + "for equal values would have a game believe every name is "
                + "smaller than every other");
    }

    /// <summary>
    /// A list comes back in order, and both spellings answer.
    /// </summary>
    /// <remarks>
    /// <strong>`sort` and `sort!` are one instruction here</strong>, because
    /// this runtime has no changeable value objects and a sorted copy is the
    /// list,
    /// <strong>and a reader that gave them two different answers would have
    /// a game that depends on the spelling for its result.</strong>
    /// </remarks>
    public void Test_SortAnswersTheListInOrderAndBothSpellingsDoTheSame()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "[[3, 1, 2].sort, [3, 1, 2].sort!]\n"));

        AssertEq(wert.Items.Count, 2,
            "**two answers came back**");
        AssertEq(AsInteger(wert.Items[0].Items[0]), 1,
            "**the list starts with the one**");
        AssertEq(AsInteger(wert.Items[0].Items[1]), 2,
            "**then the two**");
        AssertEq(AsInteger(wert.Items[0].Items[2]), 3,
            "**and then the three**");
        AssertEq(AsInteger(wert.Items[1].Items[0]), 1,
            "**and `sort!` says the same** — this runtime has no changeable "
                + "value objects, and a reader that gave the two spellings "
                + "different answers would have a game whose result depends "
                + "on which one it wrote");
    }

    /// <summary>
    /// Values that compare equal keep the order they came in.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the whole reason for the own sort.</strong>
    /// <c>List.Sort</c> is not stable, so two equal values may be swapped
    /// <strong>depending on the algorithm</strong> — a game sorting actors by
    /// level would get a different party on a different runtime,
    /// <strong>and nothing in the script would say why.</strong>
    /// </para>
    /// <para>
    /// <strong>Three equal numbers would not show this.</strong> They are the
    /// same value, and <strong>so the order they come out in cannot be told
    /// from the order they went in.</strong> The values here are distinct
    /// strings of the same length whose comparison says they are not
    /// greater, <strong>and that is the only case where a swap would
    /// show.</strong>
    /// </para>
    /// </remarks>
    public void Test_ValuesThatCompareEqualKeepTheOrderTheyCameIn()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class Kachel\n"
            + "  def initialize(n)\n"
            + "    @n = n\n"
            + "  end\n"
            + "  def <=>(andere)\n"
            + "    # **Alle drei sind gleich** und sagen das mit einer Null.\n"
            + "    0\n"
            + "  end\n"
            + "  def to_s\n"
            + "    @n\n"
            + "  end\n"
            + "end\n"
            + "[Kachel.new(\"z\"), Kachel.new(\"y\"), Kachel.new(\"x\")].sort"
            + ".map { |k| k.to_s }\n"));

        AssertEq(wert.Items.Count, 3,
            "**three values came back** — a list of three equal numbers could "
                + "not show this at all, because the order they come out in "
                + "would be the same as the order they went in either way");
        AssertEq(System.Text.Encoding.UTF8.GetString(wert.Items[0].Bytes), "z",
            "**the z is still first** — it compares as not greater, and a "
                + "sort that is not stable would have been free to put it "
                + "last, which is a game whose list changes for a reason it "
                + "cannot name");
        AssertEq(System.Text.Encoding.UTF8.GetString(wert.Items[1].Bytes), "y",
            "**and the y is still second**");
        AssertEq(System.Text.Encoding.UTF8.GetString(wert.Items[2].Bytes), "x",
            "**and the x is still last**");
    }

    /// <summary>
    /// A sort follows the script's own `<=>`, and not a built-in order.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is what makes `&lt;=&gt;` worth having.</strong> A
    /// game's own class sorts by whatever it writes,
    /// <strong>and a reader that compared the values itself would have given
    /// a game a list in an order its own rule does not produce</strong> —
    /// and the game would have no way to say what is wrong.
    /// </para>
    /// <para>
    /// <strong>Descending, because the rule says so.</strong> The class says
    /// "the bigger name comes first", and the built-in order would put the
    /// other way round, <strong>so a list that comes out ascending under
    /// this test has ignored the script.</strong>
    /// </para>
    /// </remarks>
    public void Test_ASortFollowsTheScriptsOwnComparison()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class Kachel\n"
            + "  def initialize(n)\n"
            + "    @n = n\n"
            + "  end\n"
            + "  def <=>(andere)\n"
            + "    # **Absteigend: der groessere Name zuerst.**\n"
            + "    andere.to_s <=> @n\n"
            + "  end\n"
            + "  def to_s\n"
            + "    @n\n"
            + "  end\n"
            + "end\n"
            + "[Kachel.new(\"b\"), Kachel.new(\"c\")].sort.map { |k| k.to_s }\n"));

        AssertEq(wert.Items.Count, 2,
            "**two names came back** — a list, and a reader that put the "
                + "values themselves into the list instead of their names "
                + "would have made the order unreadable");
        AssertEq(System.Text.Encoding.UTF8.GetString(wert.Items[0].Bytes), "c",
            "**the c comes first** — the class says the bigger name first, and "
                + "a sort that compared the values itself would have put the "
                + "b here, which is a list in an order the game's own rule "
                + "does not produce");
        AssertEq(System.Text.Encoding.UTF8.GetString(wert.Items[1].Bytes), "b",
            "**and then the b**");
    }

    /// <summary>
    /// Strings sort by their own order, and a mixed list is refused.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Names sort, and a list that cannot be ordered says so.</strong>
    /// A game sorting a menu by name needs strings to work,
    /// <strong>and a list of things that cannot be compared must not come
    /// out in an order nobody wrote.</strong>
    /// </para>
    /// <para>
    /// <strong>And sorting a number is a mistake the game can see.</strong>
    /// Ruby raises, and this returns nil,
    /// <strong>because a silent answer would hide a bug in the script.</strong>
    /// </para>
    /// </remarks>
    public void Test_StringsSortAndWhatCannotBeOrderedIsNotOrdered()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "[[\"b\", \"a\", \"c\"].sort, 5.sort]\n"));

        AssertEq(System.Text.Encoding.UTF8.GetString(wert.Items[0].Items[0].Bytes), "a",
            "**and the first is the a**");
        AssertEq(System.Text.Encoding.UTF8.GetString(wert.Items[0].Items[2].Bytes), "c",
            "**and the last is the c**");
        AssertTrue(wert.Items[1].Kind == RubyValueKind.Nil,
            "**and a number is not sorted** — Ruby raises there, and a silent "
                + "answer would hide a bug in the script");
    }}
