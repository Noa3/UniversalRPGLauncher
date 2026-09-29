using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Making an object, which is what every line of an RPG Maker script does
/// before it draws anything.
/// </summary>
/// <remarks>
/// <para>
/// <strong>There was no <c>new</c> at all.</strong> <c>Game_Party.new</c>,
/// <c>Game_Actor.new(1)</c> and <c>Sprite.new</c> are the first lines of most
/// of an RPG Maker's script,
/// <strong>and without it a game has no actors, no party and no map</strong> —
/// nothing at all runs.
/// </para>
/// <para>
/// <strong>And the fields were one store for the whole program.</strong>
/// <c>@hp</c> was kept in a table that every object shared,
/// <strong>so every actor would have held the last one's level</strong> — a
/// game where every character walks with the same number, and where nothing
/// in the script says why.
/// </para>
/// </remarks>
public partial class TestRubyInterpreter
{
    /// <summary>
    /// `new` makes an object, and `initialize` runs on it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the line a game cannot start without.</strong>
    /// <c>Game_Party.new</c> builds the party, and
    /// <strong>a reader that made an object without running the constructor
    /// would have given a game an empty party with no error</strong> — and
    /// the first thing a game does is put actors into it.
    /// </para>
    /// <para>
    /// <strong>And the constructor's arguments reach it.</strong>
    /// <c>def initialize(n); @hp = n; end</c> is a constructor that takes
    /// something, <strong>and a reader that called it without arguments would
    /// have given every actor a nil level</strong>.
    /// </para>
    /// </remarks>
    public void Test_NewMakesAnObjectAndTheConstructorRunsOnIt()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class Held\n"
            + "  def initialize(n)\n"
            + "    @hp = n\n"
            + "  end\n"
            + "  def hp\n"
            + "    @hp\n"
            + "  end\n"
            + "end\n"
            + "Held.new(42).hp\n"));

        AssertEq(wert.Kind, RubyValueKind.Integer,
            "**a number came back** — so the object was made and its method "
                + "ran; a reader that made an empty object would have "
                + "answered nil here, and a game's actor would have no level");
        AssertEq(AsInteger(wert), 42,
            "**and it is the forty-two** — the constructor's argument reached "
                + "it, and a reader that called it without arguments would "
                + "have given every actor a nil level");
    }

    /// <summary>
    /// Two objects hold their own fields.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the whole reason the fields moved.</strong> The
    /// store was one table for the program, so the second actor's level would
    /// overwrite the first's,
    /// <strong>and a party of four would have four actors at the last one's
    /// level</strong> — a game where every character walks with the same
    /// number, and where nothing in the script says why.
    /// </para>
    /// <para>
    /// <strong>What the test would look like with one store.</strong> Both
    /// reads would answer the same number, and the list
    /// <c>[a.hp, b.hp]</c> would be <c>[20, 20]</c> instead of
    /// <c>[10, 20]</c> — <strong>and the two objects would be
    /// indistinguishable by anything the script wrote.</strong>
    /// </para>
    /// </remarks>
    public void Test_TwoObjectsHoldTheirOwnFields()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class Held\n"
            + "  def initialize(n)\n"
            + "    @hp = n\n"
            + "  end\n"
            + "  def hp\n"
            + "    @hp\n"
            + "  end\n"
            + "end\n"
            + "a = Held.new(10)\n"
            + "b = Held.new(20)\n"
            + "[a.hp, b.hp]\n"));

        AssertEq(wert.Items.Count, 2,
            "**two values came back**");
        AssertEq(AsInteger(wert.Items[0]), 10,
            "**the first has ten** — and with one table for the program the "
                + "second constructor would have overwritten it, so both reads "
                + "would answer twenty; a party of four would be four actors "
                + "at the last one's level, and nothing in the script would "
                + "say why");
        AssertEq(AsInteger(wert.Items[1]), 20,
            "**and the second has twenty** — the two objects are different "
                + "objects, and a reader that made a copy would have given a "
                + "game two of the same one");
    }

    /// <summary>
    /// A field the script wrote is read back through the accessor.
    /// </summary>
    /// <remarks>
    /// <strong>`@hp = 1` and `hp` are two doors to one room.</strong> A game
    /// writes the field and reads the accessor,
    /// <strong>and a reader that kept two tables would have given a game an
    /// actor whose field says one and whose accessor says nil</strong> — which
    /// is a game that sets a stat and never sees it.
    /// </remarks>
    public void Test_AFieldAndItsAccessorAreTheSameThing()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class Held\n"
            + "  attr_accessor :hp\n"
            + "  def roh\n"
            + "    @hp\n"
            + "  end\n"
            + "end\n"
            + "a = Held.new\n"
            + "a.hp = 7\n"
            + "[a.hp, a.roh]\n"));

        AssertEq(AsInteger(wert.Items[0]), 7,
            "**the accessor reads what was written** — so a game that sets a "
                + "stat and reads it back gets what it set, and not nil");
        AssertEq(AsInteger(wert.Items[1]), 7,
            "**and the field itself says the same** — a reader that kept two "
                + "tables would have given a game an actor whose accessor says "
                + "seven and whose field says nil, which is a game that sets "
                + "a stat and never sees it");
    }

    /// <summary>
    /// A class with no constructor still makes an object.
    /// </summary>
    /// <remarks>
    /// <strong>Ruby inherits <c>Object#initialize</c>, which does
    /// nothing.</strong> A game writes a class of plain data and expects an
    /// object, <strong>and a reader that refused the call would have made
    /// every such class unusable</strong> — and the refusal would look like a
    /// missing method rather than a missing language rule.
    /// </remarks>
    public void Test_AClassWithoutAConstructorStillMakesAnObject()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class Marke\n"
            + "  def text\n"
            + "    \"rot\"\n"
            + "  end\n"
            + "end\n"
            + "Marke.new.text\n"));

        AssertEq(System.Text.Encoding.UTF8.GetString(wert.Bytes), "rot",
            "**the object answers** — Ruby inherits a constructor that does "
                + "nothing, and a reader that refused the call would have "
                + "made every class of plain data unusable");
    }

    /// <summary>
    /// A name that is not a class makes no object, and says so.
    /// </summary>
    /// <remarks>
    /// <strong>And it falls through to the host, which refuses.</strong>
    /// <c>GibtEsNicht.new</c> is a typo in a class name,
    /// <strong>and an object that answered nil would have gone on into the
    /// game as an object with nothing in it</strong> — the party would be
    /// there and empty, and nothing would say why.
    /// </remarks>
    public void Test_ANameThatIsNotAClassMakesNoObject()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        mit.RunProgram(Statements("GibtEsNicht.new\n"));

        var gesagt = false;
        foreach (var d in mit.Diagnostics)
        {
            if (d.Contains("new"))
            {
                gesagt = true;
            }
        }

        AssertTrue(gesagt,
            "**and the call was refused out loud** — a name that is not a "
                + "class is a typo in the script, and a reader that answered "
                + "nil would have put an empty object into the game with "
                + "nothing said; the diagnostics were: "
                + string.Join(" | ", mit.Diagnostics));
    }

    /// <summary>
    /// A method on one object does not change the other.
    /// </summary>
    /// <remarks>
    /// <strong>Two objects, two sets of fields, and one running class.</strong>
    /// The method is the same code for both,
    /// <strong>and a reader that wrote the field into the class's own store
    /// would have changed the other actor as well</strong> — which is a game
    /// where healing one party member heals the party.
    /// </remarks>
    public void Test_AMethodOnOneObjectLeavesTheOtherAlone()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class Held\n"
            + "  def initialize\n"
            + "    @hp = 1\n"
            + "  end\n"
            + "  def heilen\n"
            + "    @hp += 1\n"
            + "  end\n"
            + "  def hp\n"
            + "    @hp\n"
            + "  end\n"
            + "end\n"
            + "a = Held.new\n"
            + "b = Held.new\n"
            + "a.heilen\n"
            + "[a.hp, b.hp]\n"));

        AssertEq(AsInteger(wert.Items[0]), 2,
            "**the first is two** — a method that writes into the class's own "
                + "store would have changed both, and healing one actor would "
                + "have healed the party");
        AssertEq(AsInteger(wert.Items[1]), 1,
            "**and the second is still one** — the two objects are two sets "
                + "of fields, and the same code ran on both");
    }

    /// <summary>
    /// `super` sees the same object, and not another one.
    /// </summary>
    /// <remarks>
    /// <strong>`super` is the same call one level up, not a call on
    /// something else.</strong> <c>Held#hp</c> written on top of
    /// <c>Waffe#hp</c> is looking at the same actor,
    /// <strong>and a reader that let <c>super</c> lose the receiver would have
    /// read the base's field from nowhere</strong> — and the actor's own value
    /// would have been the base class's, which is nil.
    /// </remarks>
    public void Test_SuperSeesTheSameObject()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class Waffe\n"
            + "  def initialize\n"
            + "    @staerke = 3\n"
            + "  end\n"
            + "  def staerke\n"
            + "    @staerke\n"
            + "  end\n"
            + "end\n"
            + "class Held < Waffe\n"
            + "  def staerke\n"
            + "    super + 10\n"
            + "  end\n"
            + "end\n"
            + "Held.new.staerke\n"));

        AssertEq(AsInteger(wert), 13,
            "**and it is thirteen** — three from the base and ten from the "
                + "class above it; a reader that let `super` lose the "
                + "receiver would have read the base's field from nowhere and "
                + "answered ten, which looks like a number and is not the "
                + "actor's strength");
    }

    /// <summary>
    /// A field written in a class body belongs to the class, and an accessor
    /// reads the receiver's.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Verified in Ruby 1.8.1's own source.</strong>
    /// <c>rb_attr</c> builds the reader as <c>NEW_IVAR(attriv)</c>,
    /// <strong>which reads <c>@name</c> off whatever the call is made
    /// on</strong> — and a class body is not an instance.
    /// <c>class A; @x = 1; attr_reader :x; end</c> therefore reads nil on
    /// <c>A.new</c>, <strong>and a reader that made the reader read the
    /// class's own field would have answered 1</strong> — a game where every
    /// actor starts with the class body's value.
    /// </para>
    /// <para>
    /// <strong>And the class body keeps its own field.</strong>
    /// <c>class A; @x = 1; end</c> writes the class's field,
    /// <strong>and two classes each keep their own</strong> — a reader with
    /// one store for the program would have given a party the map's tile
    /// count.
    /// </para>
    /// </remarks>
    public void Test_AClassBodyFieldIsTheClasssOwnAndAnAccessorIsNot()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class Die\n"
            + "  @zahl = 0\n"
            + "  attr_reader :zahl\n"
            + "end\n"
            + "Die.new.zahl\n"));

        AssertTrue(wert.Kind == RubyValueKind.Nil,
            "**an accessor reads the receiver and not the class** — Ruby 1.8.1 "
                + "builds the reader as `NEW_IVAR(@name)`, which reads off "
                + "whatever the call is made on, and a class body is not an "
                + "instance; a reader that made it read the class's own "
                + "field would have answered 0, which is a game where every "
                + "actor starts with the class body's value");
    }
    /// <summary>
    /// A class variable is shared by every object of its class.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is what a class variable is for.</strong>
    /// <c>@@zaehler</c> is how a class hands out ids and counts its objects,
    /// <strong>and a field per object would count nothing</strong> — every new
    /// object would start at zero, and a game handing out ids from a class
    /// variable would hand out the same id twice.
    /// </para>
    /// <para>
    /// <strong>And the class body starts it, and the body is where a game
    /// writes it.</strong> <c>class D; @@anzahl = 0; def setze; @@anzahl =
    /// @@anzahl + 1; end; end</c> is the ordinary form,
    /// <strong>and measured before the fix it answered
    /// <c>NoMethodError: undefined operator '+' for a Nil and a Integer</c>**
    /// — the class body's value was not readable, so the first increment
    /// failed.
    /// </para>
    /// </remarks>
    public void Test_AClassVariableIsSharedByEveryObjectOfItsClass()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class D\n"
            + "  @@anzahl = 0\n"
            + "  def setze\n"
            + "    @@anzahl = @@anzahl + 1\n"
            + "  end\n"
            + "  def anzahl\n"
            + "    @@anzahl\n"
            + "  end\n"
            + "end\n"
            + "a = D.new\n"
            + "b = D.new\n"
            + "a.setze\n"
            + "[a.anzahl, b.anzahl]\n"));

        AssertEq(AsInteger(wert.Items[0]), 1,
            "**the first object sees the count** — a field per object would "
                + "have left this at zero, and a game handing out ids from a "
                + "class variable would hand out the same id twice");
        AssertEq(AsInteger(wert.Items[1]), 1,
            "**and so does the second** — that is what shared means, and a "
                + "reader that gave each object its own would have made the "
                + "second read zero");
    }

    /// <summary>
    /// A class variable belongs to its class, and not to the program.
    /// </summary>
    /// <remarks>
    /// <strong>Two classes, two tables.</strong> <c>@@anzahl</c> in one class
    /// and <c>@@anzahl</c> in another are two counters,
    /// <strong>and one table for the program would have given a game the
    /// map's object count inside an actor's id</strong> — which is a number
    /// that looks right and belongs to something else.
    /// </remarks>
    public void Test_AClassVariableBelongsToItsClass()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class A\n"
            + "  @@n = 0\n"
            + "  def plus\n"
            + "    @@n = @@n + 1\n"
            + "  end\n"
            + "  def n\n"
            + "    @@n\n"
            + "  end\n"
            + "end\n"
            + "class B\n"
            + "  @@n = 100\n"
            + "  def n\n"
            + "    @@n\n"
            + "  end\n"
            + "end\n"
            + "a = A.new\n"
            + "a.plus\n"
            + "a.plus\n"
            + "[a.n, B.new.n]\n"));

        AssertEq(AsInteger(wert.Items[0]), 2,
            "**the first class counted to two** — and a reader that ran every "
                + "class in one table would have started at the other class's "
                + "hundred");
        AssertEq(AsInteger(wert.Items[1]), 100,
            "**and the second class still has its own hundred** — one table "
                + "for the program would have put the first class's count "
                + "inside the second, which is a number that looks right and "
                + "belongs to something else");
    }

    /// <summary>
    /// `defined?` says "class variable" and not "instance variable".
    /// </summary>
    /// <remarks>
    /// <strong>Ruby's own word.</strong> <c>defined?(@@x)</c> answers
    /// <c>"class variable"</c>,
    /// <strong>and a reader that said "instance-variable" would have given a
    /// script that compares the answer against a name the wrong one</strong> —
    /// and that is a form a game uses to check whether a class has been set
    /// up yet.
    /// </remarks>
    public void Test_DefinedSaysClassVariableAndNotInstanceVariable()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class D\n"
            + "  @@n = 0\n"
            + "  def namen\n"
            + "    [defined?(@@n), defined?(@fehlt)]\n"
            + "  end\n"
            + "end\n"
            + "D.new.namen\n"));

        // **`defined?` antwortet mit einem Symbol, und das ist eine
        // bestehende, gemessene Entscheidung dieses Lesers** -- die
        // anderen Antworten sind es auch, und `ToString()` gibt den Namen.
        AssertEq(wert.Items[0].ToString(), "class variable",
            "**the class variable says its own name, with a space** — "
                + "verified in `eval.c` from Ruby 1.8.1, where `defined?` "
                + "answers `\"class variable\"` and next to it "
                + "`\"local-variable\"` with a dash: the two spellings "
                + "are not the same in the reference, and a reader that "
                + "made them the same would have given a script comparing "
                + "the answer against a name the wrong word");
        AssertTrue(wert.Items[1].Kind == RubyValueKind.Nil,
            "**and an unset one is nil** — so `defined?` can tell a class "
                + "that has been set up from one that has not");
    }
}
