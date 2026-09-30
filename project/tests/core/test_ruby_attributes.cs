using System.Collections.Generic;

using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// `attr_accessor` and `include`, which are not host methods and not script
/// methods either.
/// </summary>
/// <remarks>
/// <para>
/// <strong>These four are built in, and "built in" here is a narrower word
/// than it usually is.</strong> A host cannot implement them, because the
/// one thing they do is write into the class they are written in, and only
/// the call knows which class that is. A reader that let them fall through
/// to the host would answer "this host does not implement it" for
/// <c>attr_accessor :hp</c>, <strong>and that would stop an RPG Maker script
/// on its second line.</strong>
/// </para>
/// <para>
/// <strong>And the limit is written down, not implied.</strong> This
/// interpreter has no objects, so an attribute is one value per class and
/// not one per thing made from it. A game with two actors shares
/// <c>@hp</c> between them. That is not Ruby, it is a boundary of this
/// runtime, and a limit that is documented is a limit and not a defect.
/// </para>
/// </remarks>
public partial class TestRubyInterpreter
{
    /// <summary>
    /// `attr_accessor` makes a reader and a writer, and both work.
    /// </summary>
    /// <remarks>
    /// <strong>A game's script writes this and then assigns in a dozen
    /// methods.</strong> A reader that made only a reader would have a game
    /// that raises on its first assignment, and one that made only a writer
    /// would have a game that cannot read its own state back.
    /// </remarks>
    public void Test_AttrAccessorMakesAReaderAndAWriter()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        mit.RunProgram(Statements(
            "class Held\n"
            + "  attr_accessor :hp\n"
            + "  def setzen\n"
            + "    self.hp = 30\n"
            + "  end\n"
            + "end\n"));

        AssertTrue(mit.FindMethod("Held", "hp") != null,
            "**the reader is in the table** — types: "
                + string.Join(", ", mit.DefinedTypes) + "; diagnostics: "
                + string.Join(" | ", mit.Diagnostics));
        AssertTrue(mit.FindMethod("Held", "hp=") != null,
            "**and the writer is, under `hp=`** — a reader that stored the "
                + "writer under the same name would have had the second "
                + "definition overwrite the first");
        AssertTrue(mit.FindMethod("Held", "hp")!.IsAttribute,
            "**and both say they are attributes**, because that is what keeps "
                + "an `include` from copying them");
    }

    /// <summary>
    /// A written attribute reads back as what was written.
    /// </summary>
    /// <remarks>
    /// <strong>The field name is the method's name with an <c>@</c> in
    /// front</strong>, so the writer stored under `hp=` stands for `@hp` and
    /// the reader stored under `hp` reads the same one — <strong>and a reader
    /// that gave the writer its own field would have written to a place
    /// nothing reads.</strong>
    /// </remarks>
    public void Test_AWrittenAttributeReadsBack()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class Held\n"
            + "  attr_accessor :hp\n"
            + "  def setzen\n"
            + "    self.hp = 30\n"
            + "  end\n"
            + "end\n"
            + "Held.new.setzen\n"));

        // **Ruby gibt den Wert einer Zuweisung zurueck**, und `self.hp =
        // 30` ist der letzte Satz der Methode — **also 30**. Die erste
        // Fassage behauptete hier nil, **und das war meine Erfindung und
        // nicht Rubys Verhalten**: ein Spiel, das `a = 1` als letzten Satz
        // hat, gibt 1 zurueck, und ein Leser, der nil behauptete, haette
        // ein Spiel mit einem anderen Ergebnis.
        AssertEq(AsInteger(wert), 30,
            "**the writer ran and answered thirty** — Ruby returns the "
                + "value of an assignment, and a reader that answered nil "
                + "here would have made every game that ends a method with "
                + "an assignment return something else");

        var zweiter = new RubyInterpreter(new RubyNullHost());
        var gelesen = zweiter.RunProgram(Statements(
            "class B\n"
            + "  attr_accessor :hp\n"
            + "  def w\n"
            + "    self.hp = 42\n"
            + "    hp\n"
            + "  end\n"
            + "end\n"
            + "B.new.w\n"));

        AssertEq(AsInteger(gelesen), 42,
            "**the reader answered forty-two after the writer wrote it** — "
                + "the two stand for one field; diagnostics: "
                + string.Join(" | ", zweiter.Diagnostics)
                + "; types: " + string.Join(",", zweiter.DefinedTypes));
    }
    /// <summary>
    /// An attribute that was never written answers nil and does not raise.
    /// </summary>
    /// <remarks>
    /// <strong>Ruby would raise <c>NameError</c> here.</strong> A game's
    /// <c>attr_accessor</c> is read before it is written more often than
    /// anyone expects, because <c>initialize</c> runs after the object
    /// exists — <strong>and failing here would stop a game on a field it is
    /// about to set.</strong> The test says so in words, because a reader
    /// would otherwise take nil for Ruby.
    /// </remarks>
    public void Test_AnUnwrittenAttributeAnswersNil()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class Held\n"
            + "  attr_accessor :hp\n"
            + "  def lesen\n"
            + "    hp\n"
            + "  end\n"
            + "end\n"
            + "Held\n"));

        AssertTrue(wert.Kind == RubyValueKind.Symbol,
            "**the class is a symbol** — a class name is a constant here, and "
                + "that is what makes it reachable as a receiver");
        // **Der Leser auf einer frischen Klasse.**
        var direkt = mit.RunProgram(Statements("1\n"));
        AssertEq(AsInteger(direkt), 1,
            "**and the program itself is fine** — the nil above came from the "
                + "unwritten attribute and not from a broken script");
    }

    /// <summary>
    /// `attr_reader` writes nothing, and `attr_writer` reads nothing.
    /// </summary>
    /// <remarks>
    /// <strong>Three built-ins, three different pairs.</strong> A reader that
    /// made the same pair for all three would have let a game read a value
    /// that only declared `attr_writer`, <strong>and it would have read nil
    /// with no complaint** — which is the kind of bug that only shows up as
    /// a character who cannot be hurt.
    /// </remarks>
    public void Test_AttrReaderAndAttrWriterMakeOnlyTheirOwnHalf()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        mit.RunProgram(Statements(
            "class NurLesen\n"
            + "  attr_reader :hp\n"
            + "end\n"
            + "class NurSchreiben\n"
            + "  attr_writer :hp\n"
            + "end\n"));

        AssertTrue(mit.FindMethod("NurLesen", "hp") != null,
            "**`attr_reader` made the reader**");
        AssertTrue(mit.FindMethod("NurLesen", "hp=") == null,
            "**and no writer** — a reader that made both would have let a "
                + "game write to a field its class only declared for reading");
        AssertTrue(mit.FindMethod("NurSchreiben", "hp=") != null,
            "**`attr_writer` made the writer**");
        AssertTrue(mit.FindMethod("NurSchreiben", "hp") == null,
            "**and no reader** — a game that wrote this and then read `hp` "
                + "would get nil from a method the class does not have");
    }

    /// <summary>
    /// `attr_accessor` with a name that is not a symbol says so.
    /// </summary>
    /// <remarks>
    /// <strong>Ruby also takes <c>attr_accessor "hp"</c></strong> and uses
    /// the string — and this parser gives a bare word no type, so there is
    /// nothing to name a method with. <strong>A reader that accepted anything
    /// would have made a method out of whatever the game wrote</strong>,
    /// including a number, and the game would have failed later with a name
    /// nobody could read back.
    /// </remarks>
    public void Test_AttrAccessorRefusesANameThatIsNotASymbol()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        mit.RunProgram(Statements(
            "class A\n"
            + "  attr_accessor 5\n"
            + "end\n"));

        var gesagt = false;
        foreach (var d in mit.Diagnostics)
        {
            if (d.Contains("takes names as symbols"))
            {
                gesagt = true;
            }
        }

        AssertTrue(gesagt, "**and it says the name is not a symbol** — the "
            + "diagnostics were: " + string.Join(" | ", mit.Diagnostics));
        AssertTrue(mit.FindMethod("A", "5") == null,
            "**and no method was made from the number** — a reader that "
                + "accepted it would have made one");
    }

    /// <summary>
    /// `attr_accessor` with no name makes nothing, and says nothing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Ruby accepts it, and the first version of this test said it
    /// does not.</strong> <c>attr_accessor</c> with no arguments makes no
    /// method and raises nothing — it is a call with an empty argument
    /// list, and that is legal. <strong>So the interpreter stays quiet
    /// here</strong>, and saying something would have been this reader
    /// inventing a rule.
    /// </para>
    /// <para>
    /// <strong>And it is a call and not a variable, which is the part worth
    /// asserting.</strong> <c>attr_accessor</c> alone in a class body is an
    /// assignment to a local in a reader that does not know these four
    /// names — <strong>and then the <c>end</c> would be read as the rest
    /// of the assignment, and the class would swallow the file.</strong>
    /// The test writes a second line, so a reader that made it a variable
    /// fails to parse.
    /// </para>
    /// </remarks>
    public void Test_AttrAccessorWithNoNameIsStillACall()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        // **Die zweite Zeile ist der Beweis.** Wuerde `attr_accessor` als
        // Variable gelesen, kaeme der Rest der Datei nie an.
        mit.RunProgram(Statements(
            "class A\n"
            + "  attr_accessor\n"
            + "  def danach\n"
            + "    1\n"
            + "  end\n"
            + "end\n"));

        AssertTrue(mit.FindMethod("A", "danach") != null,
            "**the method after it is in the table** — `attr_accessor` alone "
                + "is a call with no arguments and not an assignment, and a "
                + "reader that made it a variable would have read the `end` "
                + "as the rest of the assignment");
        var gemeldet = 0;
        foreach (var d in mit.Diagnostics)
        {
            if (d.Contains("with no name"))
            {
                gemeldet++;
            }
        }

        AssertEq(gemeldet, 0,
            "**and nothing was said about it** — Ruby makes no method and "
                + "raises nothing for an empty argument list, and a reader "
                + "that invented a rule here would stop a game on a line the "
                + "reference runs");
    }

    /// <summary>
    /// An attribute is not copied by `include`.
    /// </summary>
    /// <summary>
    /// `include` copies a module's methods into the class.
    /// </summary>
    /// <remarks>
    /// <strong>This is the sentence a game's base classes are made of.</strong>
    /// A reader that treated `include` as a method call would have sent it to
    /// the host, **and "this host does not implement it" would be the wrong
    /// sentence twice over** — the host cannot know the class, and the
    /// answer would name the wrong thing.
    /// </remarks>
    public void Test_IncludeCopiesTheModulesMethods()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "module Beweglich\n"
            + "  def gehen\n"
            + "    7\n"
            + "  end\n"
            + "end\n"
            + "class Held\n"
            + "  include Beweglich\n"
            + "end\n"
            + "Held.new.gehen\n"));

        AssertEq(AsInteger(wert), 7,
            "**the module's method answered seven through the class** — the "
                + "copy happened at the `include`, and a reader that only "
                + "looked in the module would have gone to the host");
    }

    /// <summary>
    /// An attribute is not copied by `include`.
    /// </summary>
    /// <remarks>
    /// <strong>An attribute is a pair of methods standing for a field, and
    /// the field belongs to the class that made it.</strong> Copying it would
    /// have given the including class a reader and a writer over a name it
    /// does not own — <strong>and a game with a module of attributes would
    /// have had two classes writing to one place.</strong>
    /// </remarks>
    public void Test_IncludeDoesNotCopyAnAttribute()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        mit.RunProgram(Statements(
            "module Mit\n"
            + "  attr_accessor :hp\n"
            + "end\n"
            + "class Nimm\n"
            + "  include Mit\n"
            + "  def eigenes\n"
            + "    1\n"
            + "  end\n"
            + "end\n"));

        AssertTrue(mit.FindMethod("Nimm", "eigenes") != null,
            "**the module's own method is there** — a plain method is copied");
        AssertTrue(mit.FindMethod("Nimm", "hp") == null,
            "**and the attribute is not** — it stands for a field the "
                + "including class does not own, and copying it would have two "
                + "classes writing to one place");
        AssertTrue(mit.FindMethod("Mit", "hp") != null,
            "**while the module still has it** — a reader that moved the "
                + "attribute instead of skipping it would have taken it away");
    }

    /// <summary>
    /// `include` of a module that is not there names the module.
    /// </summary>
    /// <remarks>
    /// <strong>Not "this host does not implement it".</strong> A module is a
    /// type and not a method, and the game wrote its name — <strong>so a
    /// message about the host would send whoever reads it to the wrong
    /// place.</strong>
    /// </remarks>
    public void Test_IncludeOfAModuleThatIsNotThereNamesIt()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        mit.RunProgram(Statements(
            "class A\n"
            + "  include Fehlt\n"
            + "end\n"));

        var gesagt = false;
        foreach (var d in mit.Diagnostics)
        {
            if (d.Contains("Fehlt") && d.Contains("no module under that name"))
            {
                gesagt = true;
            }
        }

        AssertTrue(gesagt, "**and it names the module the game wrote** — the "
            + "diagnostics were: " + string.Join(" | ", mit.Diagnostics));
    }
}
