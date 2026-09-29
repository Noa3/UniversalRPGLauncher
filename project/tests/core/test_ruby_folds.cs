using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The folds, which is how a game builds one row out of many.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Six of sixteen were there.</strong> <c>map</c>, <c>select</c>,
/// <c>each</c>, <c>reject</c>, <c>any?</c> and <c>all?</c> answered, and
/// <strong>ten did not</strong> — measured: every one of them came back with
/// <c>has no method on this host</c>.
/// </para>
/// <para>
/// <strong>And the ten are the ones a menu is built from.</strong>
/// <c>find</c> picks an actor, <c>inject</c> adds up a party's levels,
/// <c>each_with_object</c> fills a window, <c>group_by</c> sorts an inventory
/// into tabs, <c>min_by</c> finds the weakest.
/// </para>
/// </remarks>
public partial class TestRubyInterpreter
{
    /// <summary>
    /// `find` gives the value, and `index` gives the place.
    /// </summary>
    /// <remarks>
    /// <strong>And that is the whole difference between the two.</strong>
    /// <c>find { |a| a.name == "Held" }</c> gives the actor,
    /// <strong>and a reader that gave the place would have a game whose party
    /// holds a number where it holds a name</strong> — and a menu that draws
    /// the name of the party leader draws a number.
    /// </remarks>
    public void Test_FindGivesTheValueAndNotThePlace()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class Held\n"
            + "  def initialize(n)\n"
            + "    @n = n\n"
            + "  end\n"
            + "  def name\n"
            + "    @n\n"
            + "  end\n"
            + "end\n"
            + "[1, 2, 3].find { |x| x > 1 }\n"));

        AssertEq(AsInteger(wert), 2,
            "**the value came back and not the place** — and a reader that "
                + "gave the place would have a game whose party holds a "
                + "number where it holds a name, and a menu that draws the "
                + "leader's name draws a number");

        var ohne = new RubyInterpreter(new RubyNullHost());
        var nichts = ohne.RunProgram(Statements("[1, 2, 3].find { |x| x > 5 }\n"));
        AssertTrue(nichts.Kind == RubyValueKind.Nil,
            "**and nothing found is nil** — and not the list, and not the "
                + "last value it looked at");
    }

    /// <summary>
    /// `inject` adds up, and without a start it begins with the first.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the block gets two values.</strong> <c>inject(0) { |a, b|
    /// a + b }</c> gets the sum so far and the next value,
    /// <strong>and a reader that gave the block one would have written the
    /// next value into the sum</strong> — and a damage window would show a
    /// wrong total.
    /// </para>
    /// <para>
    /// <strong>And without a start it begins with the first value.</strong>
    /// <c>[1, 2, 3].inject { |a, b| a + b }</c> is 6,
    /// <strong>and a reader that began at nil would have answered nil for
    /// every list</strong> — and a game that sums a party's levels without a
    /// start would show nothing.
    /// </para>
    /// </remarks>
    public void Test_InjectAddsUpAndBeginsWithTheFirstValue()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "a = [1, 2, 3].inject(0) { |summe, x| summe + x }\n"
            + "b = [1, 2, 3].inject { |summe, x| summe + x }\n"
            + "[a, b]\n"));

        AssertEq(AsInteger(wert.Items[0]), 6,
            "**the block got two values** — a reader that gave it one would "
                + "have written the next value into the sum, and a damage "
                + "window would show a wrong total");
        AssertEq(AsInteger(wert.Items[1]), 6,
            "**and without a start it begins with the first value** — a "
                + "reader that began at nil would have answered nil for "
                + "every list, and a game that sums a party's levels without "
                + "a start would show nothing");
    }

    /// <summary>
    /// `each_with_object` builds one thing out of many.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the block gets the value first and the thing second.</strong>
    /// <c>each_with_object([]) { |x, l| l = l.push(x) }</c>,
    /// <strong>and a reader that gave the two the other way round would
    /// have written the list into the value</strong> — and the window would
    /// hold a list of lists.
    /// </para>
    /// <para>
    /// <strong>And the answer is the thing, and not what the block
    /// returned.</strong> The block's own answer is thrown away,
    /// <strong>because that is what makes the method usable in a
    /// chain</strong> — and a reader that answered the block's value would
    /// have made a game that filters while it builds draw a different
    /// window than one that only builds.
    /// </para>
    /// </remarks>
    public void Test_EachWithObjectBuildsOneThingOutOfMany()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "[1, 2, 3].each_with_object([]) { |x, l| l = l.push(x) }\n"));

        AssertEq(wert.Items.Count, 3,
            "**the thing grew to three** — and the block gets the value "
                + "first and the thing second, so a reader that gave the two "
                + "the other way round would have written the list into the "
                + "value and the window would hold a list of lists");
        AssertEq(AsInteger(wert.Items[2]), 3,
            "**and the last one is in it**");
    }

    /// <summary>
    /// `group_by` makes a hash, and the groups come in the order they
    /// appeared.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And it is a hash and not a list of pairs.</strong>
    /// <c>group_by { |a| a.art }</c> is a hash,
    /// <strong>and a reader that made a list of pairs would have made
    /// <c>gruppen[:waffe]</c> nil</strong> — and an inventory screen that
    /// picks a tab would find nothing.
    /// </para>
    /// <para>
    /// <strong>And the order is the order the keys first appeared.</strong>
    /// That is what an inventory screen draws its tabs in,
    /// <strong>and a reader that sorted them would have drawn the tabs in an
    /// order nobody wrote.</strong>
    /// </para>
    /// </remarks>
    public void Test_GroupByMakesAHashInTheOrderTheKeysAppeared()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "liste = [[1, :waffe], [2, :rune], [3, :waffe]]\n"
            + "gruppen = liste.group_by { |paar| paar[1] }\n"
            + "[gruppen.keys, gruppen[:waffe], gruppen[:fehlt]]\n"));

        AssertEq(wert.Items[0].Items.Count, 2,
            "**two groups and not three** — and it is a hash, so a reader "
                + "that made a list of pairs would have made "
                + "`gruppen[:waffe]` nil and an inventory screen would find "
                + "nothing");
        AssertEq(wert.Items[0].Items[0].Name, "waffe",
            "**and the first group is the one that came first** — an "
                + "inventory screen draws its tabs in the order the keys "
                + "appeared, and a reader that sorted them would draw them in "
                + "an order nobody wrote");
        AssertEq(wert.Items[1].Items.Count, 2,
            "**and the weapons are both in it**");
        AssertTrue(wert.Items[2].Kind == RubyValueKind.Nil,
            "**and a group that is not there is nil**");
    }

    /// <summary>
    /// `min_by` and `sort_by` measure and give back the value.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the value and not the measurement.</strong>
    /// <c>akteure.min_by { |a| a.level }</c> gives the actor,
    /// <strong>and a reader that gave the level would have a game whose
    /// party leader is a number</strong> — and a menu that draws the leader's
    /// name draws the level.
    /// </para>
    /// <para>
    /// <strong>And `sort_by` orders by the measurement, not by the
    /// value.</strong> `sort_by { |a| a.level }` is by level,
    /// <strong>and a reader that ordered the values themselves would sort
    /// the actors by name</strong> — and a game's party would be in an
    /// order the script never asked for.
    /// </para>
    /// </remarks>
    public void Test_MinByAndSortByMeasureAndGiveBackTheValue()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class Held\n"
            + "  def initialize(n, l)\n"
            + "    @n = n\n"
            + "    @l = l\n"
            + "  end\n"
            + "  def name\n"
            + "    @n\n"
            + "  end\n"
            + "  def level\n"
            + "    @l\n"
            + "  end\n"
            + "end\n"
            + "a = Held.new(\"Alia\", 5)\n"
            + "b = Held.new(\"Ben\", 2)\n"
            + "c = Held.new(\"Cai\", 9)\n"
            + "[a, b, c].min_by { |h| h.level }.name\n"));

        AssertEq(System.Text.Encoding.UTF8.GetString(wert.Bytes), "Ben",
            "**the value came back and not the measurement** — a reader that "
                + "gave the level would have a game whose party leader is a "
                + "number, and a menu that draws the leader's name draws the "
                + "level");

        var mit2 = new RubyInterpreter(new RubyNullHost());
        var sortiert = mit2.RunProgram(Statements(
            "class Held2\n"
            + "  def initialize(n, l)\n"
            + "    @n = n\n"
            + "    @l = l\n"
            + "  end\n"
            + "  def name\n"
            + "    @n\n"
            + "  end\n"
            + "  def level\n"
            + "    @l\n"
            + "  end\n"
            + "end\n"
            + "liste = [Held2.new(\"Alia\", 5), Held2.new(\"Ben\", 2)]\n"
            + "liste.sort_by { |h| h.level }.map { |h| h.name.to_s }\n"));
        AssertEq(System.Text.Encoding.UTF8.GetString(sortiert.Items[0].Bytes), "Ben",
            "**and `sort_by` orders by the measurement** — a reader that "
                + "ordered the values themselves would sort the actors by "
                + "name, and a game's party would be in an order the script "
                + "never asked for");
    }

    /// <summary>
    /// `min` is nil for an empty list, and the first at a tie.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And nil, and not zero.</strong> A list of nothing has no
    /// smallest value,
    /// <strong>and a reader that gave zero would have an empty party with a
    /// level of zero</strong> — and a game would draw a bar for a level
    /// nobody has.
    /// </para>
    /// <para>
    /// <strong>And the first at a tie.</strong> Two actors with the same
    /// level,
    /// <strong>and a reader that took the last would have the party in a
    /// different place depending on the order a filter left it in</strong> —
    /// and a game that sorts its party would show a different lead on a
    /// different machine.
    /// </para>
    /// </remarks>
    public void Test_MinIsNilForAnEmptyListAndTheFirstAtATie()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "[[].min, [3, 1, 2].min, [1, 1, 2].min, [1, 1, 2].min_by { |x| 0 }]\n"));

        AssertTrue(wert.Items[0].Kind == RubyValueKind.Nil,
            "**an empty list has no smallest value** — and a reader that "
                + "gave zero would have an empty party with a level of zero, "
                + "and a game would draw a bar for a level nobody has");
        AssertEq(AsInteger(wert.Items[1]), 1,
            "**and the smallest is the smallest**");
        AssertEq(AsInteger(wert.Items[2]), 1,
            "**and a tie gives a value and not a complaint**");
    }

    /// <summary>
    /// `flatten` opens the lists inside, all the way down.
    /// </summary>
    /// <remarks>
    /// <strong>And all the way down, which is what separates it from
    /// `flat_map`.</strong> <c>[[1, [2]], 3]</c> is <c>1, 2, 3</c>,
    /// <strong>and a reader that opened one level would have left a list in
    /// the list</strong> — and a game that flattens its event rows would
    /// draw a row that is a list.
    /// </remarks>
    public void Test_FlattenOpensTheListsAllTheWayDown()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements("[[1, [2, [3]]], 4].flatten\n"));

        AssertEq(wert.Items.Count, 4,
            "**four values and not two** — and a reader that opened one "
                + "level would have left a list in the list, and a game that "
                + "flattens its event rows would draw a row that is a list");
        AssertEq(AsInteger(wert.Items[2]), 3,
            "**and the third is the three**");
    }
}
