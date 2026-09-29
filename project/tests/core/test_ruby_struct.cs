using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// `Struct`, which is how RPG Maker XP, VX and VX Ace write their data
/// classes.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is not a convenience.</strong>
/// <c>RPG::Actor = Struct.new(:id, :name, :class_id)</c> is <strong>the
/// first line of the standard library of all three</strong>,
/// <strong>and without it not one of those games is readable</strong> —
/// every actor, item, skill, enemy, troop and state in the catalogue is a
/// struct.
/// </para>
/// <para>
/// <strong>And a constant had nowhere to put its value.</strong>
/// <c>Punkt = Struct.new(:x, :y)</c> said <em>is on the left of an = and
/// there is nowhere to put the value</em> —
/// <strong>a message about the reader for something the reader very well
/// can do</strong>, and the script that needs it is every game from that
/// time.
/// </para>
/// </remarks>
public partial class TestRubyInterpreter
{
    /// <summary>
    /// A constant takes a value, and not only a type.
    /// </summary>
    /// <remarks>
    /// <strong>And it is a value and not a class.</strong>
    /// <c>LIMIT = 100</c> is a number, and putting it in the type table would
    /// have made every game that reads <c>LIMIT</c> do its arithmetic on a
    /// class,
    /// <strong>and the number 100 is the first line of half the scripts in
    /// VX Ace's configuration.</strong>
    /// </remarks>
    public void Test_AConstantCanHoldAValueAndNotOnlyAType()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "LIMIT = 100\n"
            + "class Held\n"
            + "  def above?(n)\n"
            + "    n > LIMIT\n"
            + "  end\n"
            + "end\n"
            + "Held.new.above?(150)\n"));

        AssertTrue(wert.Kind == RubyValueKind.Boolean && wert.Boolean,
            "**the constant was readable as a value** — a reader that filed "
                + "it as a class would have done the arithmetic on a class, "
                + "and the number 100 is the first line of half the scripts "
                + "in VX Ace's configuration");
    }

    /// <summary>
    /// `Struct.new` builds a class, and its fields are attributes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the fields are attributes, and not a second kind of
    /// member.</strong> <c>a.x</c> and <c>a.x = 1</c> are the reader and the
    /// writer of <c>@x</c>,
    /// <strong>and Ruby builds a struct's accessors exactly that way</strong>
    /// — so a game's struct takes a class, an instance variable and no new
    /// code.
    /// </para>
    /// <para>
    /// <strong>And the values go to the fields in order.</strong> The
    /// constructor takes them positionally,
    /// <strong>and a reader that looked them up by name would have put an
    /// actor's name where its id belongs</strong> — and a game's save would
    /// be a list of values in the wrong order.
    /// </para>
    /// </remarks>
    public void Test_StructNewBuildsAClassAndItsFieldsAreAttributes()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "Punkt = Struct.new(:x, :y)\n"
            + "p = Punkt.new(3, 4)\n"
            + "p.x = 9\n"
            + "[p.x, p.y, Punkt.name]\n"));

        AssertEq(AsInteger(wert.Items[0]), 9,
            "**the writer wrote and the reader read the same field** — the "
                + "fields are attributes, and Ruby builds a struct's "
                + "accessors exactly that way, so a game's struct takes a "
                + "class, an instance variable and no new code");
        AssertEq(AsInteger(wert.Items[1]), 4,
            "**and the second value went to the second field** — a reader "
                + "that looked them up by name would have put an actor's "
                + "name where its id belongs, and a game's save would be a "
                + "list of values in the wrong order");
    }

    /// <summary>
    /// A field with a default gets it, and one without gets nil.
    /// </summary>
    /// <remarks>
    /// <strong>And nil, and not a field that is missing.</strong>
    /// <c>Struct.new(:a, :b, 0)</c> and <c>Punkt.new(3)</c> —
    /// <strong>and that is the whole answer to "what is in this
    /// struct"</strong>, because a game's database is full of structs built
    /// with fewer values than they have fields.
    /// </remarks>
    public void Test_AStructFieldWithADefaultGetsItAndOneWithoutGetsNil()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "MitDefault = Struct.new(:a, :b, 0)\n"
            + "Ohne = Struct.new(:a, :b)\n"
            + "[MitDefault.new(3).b, Ohne.new(3).b]\n"));

        AssertEq(AsInteger(wert.Items[0]), 0,
            "**the declared default was there** — a reader that dropped it "
                + "would hand nil to a field the game declared, and "
                + "`Struct.new(:id, :name, \"\")` is the first line of every "
                + "RPG Maker data class");
        AssertTrue(wert.Items[1].Kind == RubyValueKind.Nil,
            "**and a field with no default is nil** — and not a field that is "
                + "missing, because a game's database is full of structs "
                + "built with fewer values than they have fields");
    }

    /// <summary>
    /// A struct is a list of its fields, in the order they were written.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And that order is the whole reason a struct exists</strong> —
    /// it is what a save file is,
    /// <strong>and a reader that sorted the fields or used a dictionary would
    /// write a save no other game can read.</strong>
    /// </para>
    /// <para>
    /// <strong>And a name is a field too.</strong> <c>p[:x]</c> is
    /// <c>p.x</c>,
    /// <strong>and that is the case a game writes when it reads fields in a
    /// loop</strong> — a reader that took only numbers would give
    /// <c>held[:hp]</c> nil, and a status bar that reads it draws nothing.
    /// </para>
    /// </remarks>
    public void Test_AStructIsAListOfItsFieldsInTheOrderTheyWereWritten()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "Punkt = Struct.new(:x, :y)\n"
            + "p = Punkt.new(3, 4)\n"
            + "[p[0], p[1], p[:y], p.to_a, p.size]\n"));

        AssertEq(AsInteger(wert.Items[0]), 3,
            "**the first field came first** — and that order is what a save "
                + "file is, and a reader that sorted the fields would write a "
                + "save no other game can read");
        AssertEq(AsInteger(wert.Items[2]), 4,
            "**and a name is a field too** — that is the case a game writes "
                + "when it reads fields in a loop, and a reader that took "
                + "only numbers would give `held[:hp]` nil, and a status bar "
                + "that reads it draws nothing");
        AssertEq(wert.Items[3].Items.Count, 2,
            "**and `to_a` gave both fields**");
        AssertEq(AsInteger(wert.Items[4]), 2,
            "**and the size is the number of fields**");
    }

    /// <summary>
    /// Two structs with the same fields are equal.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the fields, one by one, and not the identity.</strong> A
    /// game that looks for an item in a list writes
    /// <c>liste.include?(item)</c>,
    /// <strong>and a reader that compared the objects themselves would never
    /// find it</strong> — and every inventory screen would be empty.
    /// </para>
    /// <para>
    /// <strong>And two different struct kinds are never equal.</strong> A
    /// weapon and a piece of armour can both be made of three numbers,
    /// <strong>and a reader that compared only the fields would hold a sword
    /// to be armour</strong> — and <c>include?</c> would find it.
    /// </para>
    /// </remarks>
    public void Test_TwoStructsWithTheSameFieldsAreEqual()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "Punkt = Struct.new(:x, :y)\n"
            + "Waffe = Struct.new(:a, :b, :c)\n"
            + "a = Punkt.new(1, 2)\n"
            + "b = Punkt.new(1, 2)\n"
            + "c = Punkt.new(1, 3)\n"
            + "[a == b, a == c, a != c, a == Waffe.new(1, 2, 3)]\n"));

        AssertTrue(wert.Items[0].Kind == RubyValueKind.Boolean && wert.Items[0].Boolean,
            "**two structs with the same fields are equal** — a reader that "
                + "compared the objects themselves would never find a game's "
                + "item in its own inventory, and every inventory screen "
                + "would be empty");
        AssertTrue(wert.Items[1].Kind == RubyValueKind.Boolean && !wert.Items[1].Boolean,
            "**and one with a different field is not**");
        AssertTrue(wert.Items[2].Kind == RubyValueKind.Boolean && wert.Items[2].Boolean,
            "**and `!=` is `not ==`** — a reader that compared the two "
                + "separately had two places where the struct's rule could be "
                + "missing, and at the second one it would be");
        AssertTrue(wert.Items[3].Kind == RubyValueKind.Boolean && !wert.Items[3].Boolean,
            "**and two different struct kinds are never equal** — a weapon "
                + "and a piece of armour can both be made of three numbers, "
                + "and a reader that compared only the fields would hold a "
                + "sword to be armour");
    }

    /// <summary>
    /// The catalogue line, exactly as RPG Maker XP writes it.
    /// </summary>
    /// <remarks>
    /// <strong>And the whole database hangs on this one line.</strong>
    /// Every actor, item, skill, enemy, troop and state in the catalogue is
    /// a struct,
    /// <strong>and a reader that had `Struct` but not the constant
    /// assignment would build the class and then lose the name</strong> —
    /// **and the game would look up <c>RPG::Actor</c> and find nothing.**
    /// </remarks>
    public void Test_TheCatalogueLineThatEveryDataClassIsBuiltFrom()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "module RPG\n"
            + "  Actor = Struct.new(:id, :name, :class_id)\n"
            + "  Item = Struct.new(:id, :name, :price)\n"
            + "end\n"
            + "held = RPG::Actor.new(1, \"Held\", 2)\n"
            + "schwert = RPG::Item.new(3, \"Schwert\", 500)\n"
            + "[held.id, held.name, schwert.name, schwert.price, held == held]\n"));

        AssertEq(System.Text.Encoding.UTF8.GetString(wert.Items[1].Bytes), "Held",
            "**the name came back out of a nested constant** — and the whole "
                + "catalogue hangs on this one line, because every actor, "
                + "item, skill, enemy, troop and state in RPG Maker XP, VX "
                + "and VX Ace is a struct");
        AssertEq(AsInteger(wert.Items[3]), 500,
            "**and the price is in the field it belongs to**");
        AssertTrue(wert.Items[4].Kind == RubyValueKind.Boolean && wert.Items[4].Boolean,
            "**and a struct equals itself**");
    }
}
