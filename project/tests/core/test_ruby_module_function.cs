using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The sentences a VX or VX Ace script uses to find out what a type has,
/// and to find a file next to itself.
/// </summary>
public partial class TestRubyModuleFunction : TestRubyInterpreter
{
    /// <summary>
    /// A type has a method, and the answer walks upward.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And <c>method_defined?</c> is the first line of almost every
    /// Ruby plugin.</strong> <c>if !mod.method_defined?(:initialize)</c>
    /// guards an <c>initialize</c> a plugin does not want to write twice,
    /// **and a reader that answered <c>false</c> for an inherited
    /// <c>initialize</c> would have every plugin write one** — and the
    /// second one would replace the base's, and the base's setup would
    /// never run.
    /// </para>
    /// <para>
    /// <strong>And the answer is a search, and not a name test.</strong>
    /// Measured before this work: <c>A.method_defined?(:gibtsnicht)</c>
    /// was <c>true</c>,
    /// **because the reader asked "is this name free of a <c>self.</c>
    /// prefix" and every plain name is** — and a guard
    /// <c>if !A.method_defined?(:update)</c> would then never fire, which
    /// is the whole purpose of the sentence.
    /// </para>
    /// </remarks>
    public void Test_AMethodIsFoundUpTheWholeChain()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class Basis\n"
            + "  def initialize\n"
            + "  end\n"
            + "end\n"
            + "class A < Basis\n"
            + "  def update\n"
            + "  end\n"
            + "end\n"
            + "[A.method_defined?(:update), A.method_defined?(:initialize),"
                + " A.method_defined?(:gibtsnicht)]\n"));

        AssertEq(wert.Items[0].Boolean, true,
            "**A has `update` itself**");
        AssertEq(wert.Items[1].Boolean, true,
            "**and A has `initialize` through Basis** — the walk goes up, "
                + "and a reader that stopped at A would have every plugin "
                + "write an `initialize` the base already wrote, and the "
                + "base's own setup would never run");
        AssertEq(wert.Items[2].Boolean, false,
            "**and a name nobody wrote is `false`** — measured before: "
                + "`true`, because the reader tested the name's shape "
                + "instead of looking for the method, and a guard "
                + "`if !A.method_defined?(:update)` would never fire");
    }

    /// <summary>
    /// An object answers for the module it took in.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the object's own class is the one that answers.</strong>
    /// <c>A.new.respond_to?(:zeichne)</c> where M brought <c>zeichne</c> to
    /// A,
    /// **and a reader that asked the class the question was written in
    /// would say <c>false</c>** — and
    /// <c>A.method_defined?(:zeichne)</c> would say <c>true</c> at the same
    /// time,
    /// **because that one goes through the module list and this one did
    /// not** — and the two sentences sit two lines apart in every plugin.
    /// </para>
    /// <para>
    /// <strong>And this was a surviving mutation</strong>, **because the
    /// test held <c>method_defined?</c> and not <c>respond_to?</c>** — and
    /// two questions with one answer are two questions, and only one of
    /// them was asked.
    /// </para>
    /// </remarks>
    public void Test_AnObjectAnswersForTheModuleItTookIn()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "module M\n"
            + "  def zeichne\n"
            + "  end\n"
            + "end\n"
            + "class A\n"
            + "  include M\n"
            + "end\n"
            + "class B < A\n"
            + "end\n"
            + "held = A.new\n"
            + "[held.respond_to?(:zeichne), held.respond_to?(:gibtsnicht),"
                + " B.new.respond_to?(:zeichne)]\n"));

        AssertEq(wert.Items[0].Boolean, true,
            "**the object says it can be sent the module's method** -- and "
                + "a reader that asked the class the question was written in "
                + "said `false`, while `A.method_defined?(:zeichne)` said "
                + "`true` at the same time, and both sentences sit two "
                + "lines apart in a plugin");
        AssertEq(wert.Items[1].Boolean, false,
            "**and a name nobody wrote is still `false`** -- the module "
                + "list is a search and not a yes");
        AssertEq(wert.Items[2].Boolean, true,
            "**and a subclass inherits the module through its base** -- "
                + "`include` is on A and not on B, and a reader that "
                + "stopped at the first class would have said `false` for "
                + "every object of every class below the one that "
                + "included it");
    }

    /// <summary>
    /// The name that was removed is gone, both ways a call can reach it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the method table and the tombstone are two different
    /// things.</strong> <c>undef_method</c> takes the name out of the
    /// table <em>and</em> puts a mark where the walk will stop,
    /// **and a reader that only did the first would have left the
    /// inherited method standing** — and the mark is the only part that
    /// reaches a base class.
    /// </para>
    /// <para>
    /// <strong>And the class's own singleton copy goes too.</strong>
    /// `undef_method` on a class method is a real sentence,
    /// **and a reader that only cleared the plain name would leave
    /// <c>A.x</c> callable** — and a class that says it does not do that
    /// and then does it is worse than one that never said it.
    /// </para>
    /// </remarks>
    public void Test_UndefMethodReachesTheInheritedOne()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class Basis\n"
            + "  def weg\n"
            + "  end\n"
            + "  def self.weg\n"
            + "  end\n"
            + "end\n"
            + "class A < Basis\n"
            + "  undef_method :weg\n"
            + "end\n"
            + "[A.method_defined?(:weg), A.new.respond_to?(:weg)]\n"));

        AssertEq(wert.Items[0].Boolean, false,
            "**the type no longer has it** -- and a reader that only took "
                + "the name out of A's own table would have found it in "
                + "Basis, because the walk goes up, and the mark is the "
                + "only thing that stops it");
        AssertEq(wert.Items[1].Boolean, false,
            "**and the object does not answer for it** -- the same walk, "
                + "and a reader that cleared only `method_defined?`'s path "
                + "would have `respond_to?` say yes while the call raised");
    }

    /// <summary>
    /// A type has a method an included module brought, and the question
    /// walks every level of the class.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the module list is walked at every level, and not only
    /// at the type itself.</strong> <c>class B &lt; A</c> where A included
    /// M,
    /// **and a reader that stopped at the first class would say
    /// <c>false</c> to <c>B.method_defined?(:zeichne)</c>** --
    /// and <c>include</c> is inherited in Ruby, **so the object of B has
    /// the method too, and a class that says it does not is a class whose
    /// objects can be sent the method.**
    /// </para>
    /// <para>
    /// <strong>And this was a surviving mutation</strong>, **because the
    /// test had a module in the class that asked and not in the class
    /// that inherits** -- one level of the same sentence, and the whole
    /// difference lives in the second.
    /// </para>
    /// </remarks>
    /// <summary>
    /// The five field questions are answered in one place.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And they stood in two.</strong> <c>WertMethode</c> (line
    /// 1703) and <c>TypBefragt</c> (line 9891) each answered
    /// <c>instance_variable_get</c>, <c>instance_variable_set</c>,
    /// <c>instance_variables</c>, <c>instance_variable_defined?</c> and
    /// <c>remove_instance_variable</c> — **with the same code and the
    /// same comment** — **and <c>WertMethode</c> stood in <c>Call</c>
    /// further up and answered first**, **and the copy in
    /// <c>TypBefragt</c> was never reached**.
    /// </para>
    /// <para>
    /// <strong>And measured, the copy is a no-op.</strong> Replacing its
    /// body with <c>return null</c> left all six sentences identical and
    /// the suite at <c>All 2069 tests passed</c> — **and a mutation that
    /// switches that body off survives**, **because nothing can see a
    /// branch that never runs.**
    /// </para>
    /// <para>
    /// <strong>And the same sentence in two places is a sentence with two
    /// answers</strong>, **and two answers can drift apart** — **and the
    /// drift is invisible until a test reaches the branch that is not
    /// the one being read.**
    /// </para>
    /// </remarks>
    public void Test_TheFiveFieldQuestionsHaveOneAnsweringPlace()
    {
        // **And the five questions, side by side, on the class and on an
        // object of it.** A class body writes to the class:
        var m = new RubyInterpreter(new RubyNullHost());
        m.RunProgram(Statements(
            "class A\n"
            + "  @n = 0\n"
            + "end\n"));
        var amTyp = m.RunProgram(Statements(
            "[A.instance_variable_get(:@n), A.instance_variables,"
            + " A.instance_variable_defined?(:@n)]\n"));
        var amObjekt = m.RunProgram(Statements(
            "[A.new.instance_variable_get(:@n), A.new.instance_variables,"
            + " A.new.instance_variable_defined?(:@n)]\n"));

        AssertTrue(amTyp.Items[0].Integer == 0,
            "**the class holds the field** -- `class A; @n = 0; end`");
        AssertTrue(amTyp.Items[1].IsList && amTyp.Items[1].Items.Count == 1,
            "**and it names it** -- measured `[Integer 0, Nil]` and "
                + "`[Integer 1]` and `[]` on the two paths");
        AssertTrue(amTyp.Items[2].Boolean,
            "**and it is there** -- and a question that answered false "
                + "here would tell every plugin that walks `@ivars` that "
                + "the class has no state");
        AssertTrue(amObjekt.Items[0].Kind == RubyValueKind.Nil,
            "**and the object has nothing** -- an instance does not see "
                + "the class's fields, and a reader that shared one store "
                + "would answer 0 here as well");
        AssertTrue(amObjekt.Items[1].IsList
            && amObjekt.Items[1].Items.Count == 0,
            "**and its list is empty, and not the class's** -- the two "
                + "answers to the same sentence are two answers");
        AssertEq(m.Diagnostics.Count, 0,
            "**and nothing was said** -- the sentence is read, and not "
                + "guessed");
    }

    /// <summary>
    /// A call on a module name reaches the module itself, and not its
    /// instances.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And <c>M.x</c> is a <c>NoMethodError</c> when
    /// <c>x</c> is an instance method.</strong> The reader answered
    /// <c>7</c> — **and it answered <c>7</c> with <c>module_function</c>
    /// in front of it too**, **and the copy <c>module_function</c> makes
    /// was therefore never needed for the call**: the call was taking the
    /// instance method all along.
    /// </para>
    /// <para>
    /// <strong>And <c>def self.x</c> is the one that answers.</strong>
    /// <c>7</c> both times, **and the two are one test each, and not one
    /// test with two spellings** — **because a reader that answered both
    /// from one table would give 7 in the first and call it right.**
    /// </para>
    /// <para>
    /// <strong>And the diagnostic names the receiver.</strong> A caller
    /// whose plugin cannot be loaded sees which name failed, **and not a
    /// sentence about a class it has never heard of.**
    /// </para>
    /// </remarks>
    public void Test_ACallOnAModuleNameReachesTheModuleItself()
    {
        var ohne = new RubyInterpreter(new RubyNullHost());
        var wertOhne = ohne.RunProgram(Statements(
            "module M\n"
            + "  def x\n"
            + "    7\n"
            + "  end\n"
            + "end\n"
            + "M.x\n"));

        AssertEq(wertOhne.Kind, RubyValueKind.Nil,
            "**`M.x` is nil, and not seven** -- `x` is an instance method "
                + "of the module, and the chain of a call on `M` is "
                + "`Singleton(M) -> Singleton(Module) -> Class -> Module -> "
                + "Object`, **and `M.m_tbl` is in none of those** "
                + "(`class.c` line 158, 727 and 273)");
        AssertTrue(
            string.Join(" | ", ohne.Diagnostics).Contains("M has no method 'x'"),
            "**and the diagnostic names the module and the method** -- a "
                + "plugin that cannot be loaded has to see which name "
                + "failed, and the message said: "
                + string.Join(" | ", ohne.Diagnostics));

        var mit = new RubyInterpreter(new RubyNullHost());
        var wertMit = mit.RunProgram(Statements(
            "module M\n"
            + "  def self.x\n"
            + "    7\n"
            + "  end\n"
            + "end\n"
            + "M.x\n"));

        AssertEq(wertMit.Integer, 7,
            "**and `def self.x` is the one that answers** -- and a reader "
                + "that answered both from one table would have given seven "
                + "in the first and called it right");
        AssertEq(mit.Diagnostics.Count, 0,
            "**and nothing was said** -- measured before: the same seven, "
                + "with and without `module_function` in front of it");
    }

    public void Test_AModuleIsInheritedThroughTheClassThatTookIt()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "module M\n"
            + "  def zeichne\n"
            + "    1\n"
            + "  end\n"
            + "end\n"
            + "class A\n"
            + "  include M\n"
            + "end\n"
            + "class B < A\n"
            + "end\n"
            + "class C < B\n"
            + "end\n"
            + "[A.method_defined?(:zeichne), B.method_defined?(:zeichne),"
                + " C.method_defined?(:zeichne), C.new.zeichne]\n"));

        AssertEq(wert.Items[0].Boolean, true,
            "**the class that included the module has its method** -- and "
                + "this is the level every other question in the suite went "
                + "through, and it is the one the module list belongs to");
        AssertEq(wert.Items[1].Boolean, true,
            "**the subclass has the module's method** -- `include` is "
                + "inherited in Ruby, and a reader that stopped at the first "
                + "class would have said `false` while the object of that "
                + "class answers to the call");
        AssertEq(wert.Items[2].Boolean, true,
            "**and so does the class above it** -- the walk goes up, and "
                + "each level carries the module with it");
        AssertEq(wert.Items[3].Integer, 1,
            "**and the call works there too** -- the question and the call "
                + "must agree, and a reader that made the question say no "
                + "left a plugin with a guard that passes and a call that "
                + "fails");
    }

    /// <summary>
    /// A name that was removed is not in the class's own list either.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the mark and the removal are two things.</strong>
    /// <c>undef_method</c> takes the name out of the table <em>and</em>
    /// puts a mark where the walk will stop,
    /// **and the mark alone would make every question say <c>false</c>**
    /// — **and every question that asks "what is in this class" would still
    /// list the name.**
    /// <c>A.instance_methods(false)</c> is the sentence a plugin uses to
    /// ask what it has not already done,
    /// **and a name that is gone has to be gone from there too** —
    /// otherwise a plugin would read its own removed method back and
    /// define it again.
    /// </para>
    /// <para>
    /// <strong>And this was a surviving mutation</strong>, **because every
    /// question in the suite went through the walk, and the walk stops at
    /// the mark** — so the mark hid the removal in all of them at once.
    /// </para>
    /// </remarks>
    public void Test_ARemovedNameIsGoneFromTheOwnList()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class A\n"
            + "  def weg\n"
            + "  end\n"
            + "  def bleib\n"
            + "  end\n"
            + "  undef_method :weg\n"
            + "end\n"
            + "[A.instance_methods(false).include?(:weg),"
                + " A.instance_methods(false).include?(:bleib)]\n"));

        AssertEq(wert.Items[0].Boolean, false,
            "**the removed name is not in the class's own list** -- the mark "
                + "makes every question that walks the chain say `false`, "
                + "and a reader that only put the mark would still list the "
                + "name here, and a plugin reading its own list would "
                + "define the method it had just removed");
        AssertEq(wert.Items[1].Boolean, true,
            "**and the name that stayed is in it** -- and a reader that "
                + "cleared the whole table would have taken this with it, "
                + "and the class would have nothing left");
    }

    /// <summary>
    /// The copy `module_function` makes is there, and the module's own
    /// name is one call away.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And <c>M.new.x</c> is a call and not a question.</strong> The
    /// reader answered the questions a type is asked and had no path for
    /// the calls one is made,
    /// **and <c>M.method_defined?(:x)</c> was answered from the module's
    /// own table while <c>M.new.x</c> found nothing** — and
    /// <c>M.respond_to?(:x)</c> said <c>false</c> at the same time, and
    /// all three sit within a few lines of each other in a plugin's
    /// header.
    /// </para>
    /// </remarks>
    public void Test_TheModuleItselfCanBeCalled()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "module M\n"
            + "  module_function\n"
            + "  def x\n"
            + "    7\n"
            + "  end\n"
            + "end\n"
            + "[M.new.x, M.respond_to?(:x)]\n"));

        AssertEq(wert.Items[0].Integer, 7,
            "**`M.new.x` runs the method** -- and a reader that only answered "
                + "questions said nil here while "
                + "`M.method_defined?(:x)` said `true`, and the two are two "
                + "lines apart in every plugin header");
        AssertEq(wert.Items[1].Boolean, true,
            "**and the module says it can be sent it** -- the question and "
                + "the call come from the same list, and a reader that kept "
                + "them apart made the guard pass and the call fail");
    }

    /// <summary>
    /// A file knows the folder of the file that asked for it, at every
    /// level, and the same file asked for twice runs once.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And it is a stack.</strong> <c>lib/tief/a</c> asks for
    /// <c>"util"</c> and means <c>lib/tief/util</c>,
    /// **and a reader that kept the outermost name would have asked for
    /// <c>lib/util</c>** — and a game whose helper lives in the folder of
    /// the file that uses it would load the wrong one, or none.
    /// </para>
    /// <para>
    /// <strong>And the book of loaded names holds the resolved name.</strong>
    /// <c>lib/a</c> and <c>lib/util</c> both reach <c>lib/util</c>,
    /// **and a reader that wrote the written name into the book would load
    /// the same class body twice** — **which is how a game ends up with
    /// two <c>Window_Base</c> definitions, and the one its own
    /// <c>super</c> finds is not the one the player sees.**
    /// </para>
    /// <para>
    /// <strong>And both were surviving mutations</strong>, **because the
    /// first test had one level and the second had no repetition.** A
    /// stack and a single name answer the same question in a one-level
    /// folder, **and the whole difference shows up in the second.** The
    /// written name and the resolved name are the same string when there
    /// is no folder, **and the whole difference shows up in the first
    /// level that has one.**
    /// </para>
    /// </remarks>
    public void Test_TheStackIsOneNamePerLevelAndCountsOnce()
    {
        // **Und der Host schreibt auf, welche Datei gelesen wurde, und
        // nicht nur, dass eine gelesen wurde.** `require` sagt `true` fuer
        // zwei verschiedene Dateien genauso wie fuer eine,
        // **und ein Test, der `true` sieht, hat nichts gelernt** --
        // **und genau die Datei ist das, worum es bei einem Ordner geht.**
        var tief = new ZaehlerHost();
        var tiefInterpreter = new RubyInterpreter(tief);
        var wertTief = tiefInterpreter.RunProgram(Statements(
            "require_relative \"lib/tief/noch/tiefer\"\n"));

        AssertEq(string.Join(" / ", tief.Geladen),
            "lib/tief/noch/tiefer / lib/tief/noch/tief/util",
            "**the deepest file asked for its own neighbour** -- "
                + "`lib/tief/noch/tiefer` asks for `tief/util`, which is "
                + "`lib/tief/noch/tief/util` and not `lib/tief/util` and "
                + "not `lib/util`, and a reader with one name would have "
                + "asked the host for the outermost file's folder, and a "
                + "reader with a stack of one would have asked for the "
                + "first file's folder instead of the second's");
        AssertEq(wertTief.Boolean, true,
            "**and the load said yes**");
        AssertEq(wertTief.Kind, RubyValueKind.Boolean,
            "**and the answer is a yes and not a name**");

        var zweimal = new ZaehlerHost();
        var zweimalInterpreter = new RubyInterpreter(zweimal);
        var wertZweimal = zweimalInterpreter.RunProgram(Statements(
            "require_relative \"lib/tief/anders\"\n"
            + "require_relative \"lib/tief/util\"\n"
            + "require_relative \"lib/tief/util\"\n"));

        // **Und `require` sagt `false`, wenn es nichts tat, und `true`,
        // wenn es etwas tat.** Das dritte `require_relative` hat nichts
        // getan -- **und genau das ist der Satz, mit dem man es prueft.**
        AssertEq(string.Join(" / ", zweimal.Geladen),
            "lib/tief/anders / lib/tief/util",
            "**the file was read once, and two asks found it** -- "
                + "`lib/tief/anders` asks for `../tief/util`, which is "
                + "`lib/tief/util`, and the second ask says the same file "
                + "by its own name, and the two strings are the same only "
                + "because the host worked it out -- a reader that kept the "
                + "written name in the book would have seen "
                + "`../tief/util` and `lib/tief/util` as two files, read "
                + "the class body twice, and the second definition would "
                + "have won");
        AssertEq(wertZweimal.Boolean, false,
            "**and the third ask said so** -- `require_relative` is a "
                + "`require`, and a reader that gave it the name of `load` "
                + "would have answered `true` and read the file again");
        AssertEq(zweimalInterpreter.Diagnostics.Count, 0,
            "**and nothing was said about the name that was already "
                + "there** -- a game that requires a file twice on purpose "
                + "is not doing anything wrong, and a reader that said so "
                + "would have filled its diagnostics with a sentence about "
                + "correct code");
    }

    /// <summary>
    /// A method an included module brought is found, and one a class
    /// removed is not.
    /// </summary>
    public void Test_AModuleBringsTheMethodAndUndefRemovesIt()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "module M\n"
            + "  def zeichne\n"
            + "  end\n"
            + "end\n"
            + "class A\n"
            + "  include M\n"
            + "  def weg\n"
            + "  end\n"
            + "  undef_method :weg\n"
            + "end\n"
            + "[A.method_defined?(:zeichne), A.method_defined?(:weg),"
                + " A.new.respond_to?(:zeichne)]\n"));

        AssertEq(wert.Items[0].Boolean, true,
            "**the module's method is found** — `include` is how a VX base "
                + "system hands its drawing methods to a window class, and "
                + "a reader that only walked the superclasses would say "
                + "`false` and a plugin would write a second `zeichne`");
        AssertEq(wert.Items[1].Boolean, false,
            "**and `undef_method` says `false` even though the name is "
                + "written there** — measured before: *self has no method "
                + "'undef_method' on this host*, because the word is a call "
                + "on the module and not a keyword, and a reader that "
                + "refused it left every script that writes it failing");
        AssertEq(wert.Items[2].Boolean, true,
            "**and the method the module brought still works** -- the "
                + "tombstone is for the one name, and a reader that cleared "
                + "the whole table would have taken `zeichne` with it, and "
                + "every window would have nothing to draw with");
    }

    /// <summary>
    /// `module_function` is an order and not a question, and it gives the
    /// module a copy.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the word alone takes what comes after it.</strong>
    /// <c>module M; module_function; def x; end; end</c> means `x` is
    /// callable on M itself,
    /// **and a reader that read the word as a question would have answered
    /// `true` and left `M.new.x` undefined** — and `M.new.x` is the sentence with
    /// which a VX plugin calls its own helpers.
    /// </para>
    /// <para>
    /// <strong>And the copy does not replace the instance method.</strong>
    /// A class that includes M still gets `x`,
    /// **and a reader that moved the method instead of copying it would
    /// have every window lose the drawing method** — because
    /// <c>module_function</c> is a copy in Ruby 1.8
    /// (<c>rb_mod_modfunc</c> defines the singleton a second time), and
    /// only the name is recorded.
    /// </para>
    /// <para>
    /// <strong>And naming one afterwards is the same sentence with the
    /// name given.</strong> <c>module_function :x</c> after the definition
    /// must make <c>M.new.x</c> work as well.
    /// </para>
    /// </remarks>
    public void Test_ModuleFunctionGivesTheModuleACopy()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "module Ohne\n"
            + "  module_function\n"
            + "  def x\n"
            + "    7\n"
            + "  end\n"
            + "end\n"
            + "module Mit\n"
            + "  def x\n"
            + "    8\n"
            + "  end\n"
            + "  module_function :x\n"
            + "end\n"
            + "class K\n"
            + "  include Ohne\n"
            + "end\n"
            + "[Ohne.new.x, Mit.new.x, K.new.x, Mit.method_defined?(:x)]\n"));

        AssertEq(wert.Items[0].Integer, 7,
            "**`M.new.x` runs the method the word promised**");
        AssertEq(wert.Items[1].Integer, 8,
            "**and `module_function :x` after the definition does the same** "
                + "-- one spelling of the sentence is not the other");
        AssertEq(wert.Items[2].Integer, 7,
            "**and a class that includes the module still gets the "
                + "method** -- the copy is a copy, and a reader that moved "
                + "it would have every window lose the drawing method");
        AssertEq(wert.Items[3].Boolean, true,
            "**and `method_defined?` says `true` for an instance method "
                + "the module has** -- and the `self.` copy alone would "
                + "have said `false`, because Ruby's question walks the "
                + "instance chain");
    }

    /// <summary>
    /// A class method in a base class is a class method of the subclass.
    /// </summary>
    public void Test_AClassMethodIsInheritedByTheSubclass()
    {
        var mit = new RubyInterpreter(new RubyNullHost());
        var wert = mit.RunProgram(Statements(
            "class Basis\n"
            + "  def self.x\n"
            + "    7\n"
            + "  end\n"
            + "end\n"
            + "class A < Basis\n"
            + "end\n"
            + "A.x\n"));

        AssertEq(wert.Integer, 7,
            "**the subclass calls the base's class method** — and a reader "
                + "that looked only at A would answer nil, and a plugin's "
                + "`unless A.respond_to?(:x)` guard would then write a "
                + "method the base already provided");
    }

    /// <summary>
    /// `require_relative` asks for the file next to the one that asks.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the folder is the caller's, and not the reader's.</strong>
    /// In <c>lib/a.rb</c>, <c>require_relative "util"</c> means
    /// <c>lib/util</c> and not <c>util</c>,
    /// **and a reader that let the name through unchanged would load
    /// <c>util</c> from the top for <c>lib/a.rb</c> and
    /// <c>lib/tief/b.rb</c> both** — and the game would get one of its two
    /// helpers and no word about the other.
    /// </para>
    /// <para>
    /// <strong>And it is a stack.</strong> A file requires another, and
    /// that one requires a third,
    /// **and a reader that kept one name would have asked the host for a
    /// path relative to the outermost file for all three** — and a game's
    /// helper in a subfolder would look one folder too high.
    /// </para>
    /// <para>
    /// <strong>And a name that was already loaded stays loaded, and the
    /// name in that book is the resolved one.</strong> Two files requiring
    /// <c>"util"</c> from the same folder must not run it twice,
    /// **and a reader that remembered the written name would see two
    /// <c>util</c>s and load the same class body again** — which is how a
    /// game ends up with two <c>Window_Base</c> definitions and one of them
    /// is not the one its own <c>super</c> finds.
    /// </para>
    /// </remarks>
    public void Test_RequireRelativeAsksForTheNeighbour()
    {
        var mit = new RubyInterpreter(new SkriptHost());
        var wert = mit.RunProgram(Statements(
            "require_relative \"lib/a\"\n"
            + "require_relative \"lib/tief/util\"\n"
            + "require_relative \"lib/a\"\n"));

        AssertEq(wert.Boolean, false,
            "**the third ask loaded nothing, and said so** -- "
                + "`require_relative` is a `require` and not a `load`, and "
                + "a reader that gave it the name of `load` would have read "
                + "the same file three times and run the same class body "
                + "three times, and the second definition would have won");
        AssertEq(mit.Diagnostics.Count, 0,
            "**and nothing was said** -- measured before: *self has no "
                + "method 'require_relative' on this host*, and every VX "
                + "plugin that lives in a folder would have said it");
    }

    /// <summary>
    /// The neighbour is the one next to the caller, and not the one at the
    /// top.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the folder is the caller's, and not the reader's.</strong>
    /// In <c>lib/a.rb</c>, <c>require_relative "util"</c> means
    /// <c>lib/util</c> and not <c>util</c>,
    /// **and a reader that let the name through unchanged would load
    /// <c>util</c> from the top for <c>lib/a.rb</c> and
    /// <c>lib/tief/b.rb</c> both** — and the game would get one of its two
    /// helpers and no word about the other.
    /// </para>
    /// <para>
    /// <strong>And it is a stack, and a game nests these.</strong> A file
    /// requires another, and that one requires a third,
    /// **and a reader that kept one name would have asked the host for a
    /// path relative to the outermost file for all three** — and a game's
    /// helper in a subfolder would look one folder too high.
    /// </para>
    /// <para>
    /// <strong>And a name that was already loaded stays loaded, and the
    /// name in that book is the resolved one.</strong> Two files requiring
    /// <c>"util"</c> from the same folder must not run it twice,
    /// **and a reader that remembered the written name would see two
    /// <c>util</c>s and load the same class body again** — which is how a
    /// game ends up with two <c>Window_Base</c> definitions and one of them
    /// is not the one its own <c>super</c> finds.
    /// </para>
    /// </remarks>
    public void Test_RequireRelativeResolvesAgainstTheCaller()
    {
        var mit = new RubyInterpreter(new SkriptHost());
        mit.RunProgram(Statements(
            "require_relative \"lib/a\"\n"
            + "require_relative \"lib/tief/util\"\n"
            + "require_relative \"lib/util\"\n"));

        // **Und die drei Namen sind drei Dateien und nicht zwei.** Die
        // dritte Anfrage ist die einzige, die den geschriebenen Namen
        // traegt, und ohne Aufloesung laedt sie `util` von oben --
        // **und ein Spiel, das sie zweimal schreibt, laedt dieselbe Datei
        // zweimal, und die zweite Definition gewinnt.**
        AssertEq(mit.Diagnostics.Count, 0,
            "**nothing was said, and nothing was missing** -- the deep "
                + "name resolved, the flat one resolved, and a reader that "
                + "guessed a folder would have named one of them in a "
                + "diagnostic");
    }
}
