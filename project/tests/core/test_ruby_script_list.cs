using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// A project's scripts run, in the order the project lists them.
/// </summary>
public partial class TestRubyInterpreter
{
    /// <summary>
    /// A host that has files, and refuses everything else.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this host answers nothing but <c>ReadScript</c>.</strong>
    /// The default for <c>CallMethod</c> is null, and null is the refusal,
    /// <strong>and a host that does not implement a method does not have
    /// to say so</strong> — **a interface that demands every new ability
    /// of every host grows faster than the hosts that need it**, **and
    /// then every host writes an empty method nobody reads.**
    /// </para>
    /// <para>
    /// <strong>And the bytes are written the way a game of that time
    /// writes them: CP932, and no encoding magic.</strong>
    /// </para>
    /// </remarks>
    private sealed class SkriptOrdner : IRubyHost
    {
        private readonly Dictionary<string, byte[]> _dateien;

        public SkriptOrdner(Dictionary<string, string> pDateien)
        {
            _dateien = pDateien.ToDictionary(
                kvp => kvp.Key,
                kvp => Encoding.GetEncoding(932).GetBytes(kvp.Value),
                StringComparer.Ordinal);
        }

        public string Name => "the script folder";

        public byte[]? ReadScript(string pName, bool pEinmal)
            => _dateien.TryGetValue(pName, out var b) ? b : null;

        // **Und die vier Methoden, die der Vertrag nicht mit einer
        // Vorgabe versieht, sind hier nicht implementiert** --
        // **und sie sind hier auch nicht noetig, denn keine der beiden
        // Skripte in diesem Test ruft den Host.**
        // **Und sie muessen trotzdem dastehen**,
        // **denn der Vertrag hat keine Vorgabe fuer sie.**

        public RubyValue? CallMethod(
            RubyValue pReceiver,
            string pMethod,
            IReadOnlyList<RubyValue> pArguments)
        {
            _ = pReceiver;
            _ = pMethod;
            _ = pArguments;
            return null;
        }

        public RubyValue? CallMethodWithBlock(
            RubyValue pReceiver,
            string pMethod,
            IReadOnlyList<RubyValue> pArguments,
            Func<IReadOnlyList<RubyValue>, RubyValue> pYield)
        {
            _ = pReceiver;
            _ = pMethod;
            _ = pArguments;
            _ = pYield;
            return null;
        }

        public RubyValue? LookupConstant(string pName)
        {
            _ = pName;
            return null;
        }

        public IReadOnlyList<string> KnownMethods
            => Array.Empty<string>();
    }

    /// <summary>
    /// A project's script list runs in its order, and the order decides
    /// what a method means.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the door that was missing.</strong> Measured
    /// before this: 340 <c>new RubyInterpreter(...)</c> in the tests,
    /// <strong>and not one of them in <c>src/</c></strong>, **and every
    /// one of them handed the reader nodes it had parsed itself** —
    /// <strong>and the other half already existed, privately</strong>
    /// (<c>SkriptLaden</c>: read the bytes, decode CP932, parse, run, keep
    /// the chain), <strong>so the language could load a file and could not
    /// be asked to load a project's worth of them.</strong>
    /// </para>
    /// <para>
    /// <strong>And the order is the project's, and not the folder's.</strong>
    /// A VX project lists its scripts in <c>Scripts.list</c>, last-added
    /// first, <strong>and a reader that sorted names would run the game
    /// and get a superclass that did not exist yet</strong> — because
    /// <c>Window_Command</c> sorts before <c>Window_Base</c>.
    /// </para>
    /// </remarks>
    public void Test_DieSkriptlisteEinesProjektsLaeuftInIhrerReihenfolge()
    {
        var m = new RubyInterpreter(new SkriptOrdner(new()
        {
            ["Window_Base.rb"] = "class Window_Base\n"
                + "  def erhoehe(n)\n"
                + "    n + 1\n"
                + "  end\n"
                + "end\n",
            ["Window_Command.rb"] = "class Window_Command < Window_Base\n"
                + "  def erhoehe(n)\n"
                + "    super(n) * 2\n"
                + "  end\n"
                + "end\n",
        }));

        // **Und in der Reihenfolge des Projekts:**
        var geladen = m.RunScripts(
            ["Window_Base.rb", "Window_Command.rb"]);

        AssertEq(geladen, 2,
            "**and both scripts ran** -- and a reader that ran one of them "
                + "twice because it was in the list twice would say three");

        AssertEq(m.Diagnostics.Count, 0,
            "**and nothing was said** -- and the diagnostics were: "
                + string.Join(" | ", m.Diagnostics));

        AssertTrue(m.FindMethod("Window_Command", "erhoehe") != null,
            "**and the class that inherits is there** -- and it inherits "
                + "from a name that was defined one line list earlier, "
                + "which is the whole reason a list is ordered");

        // **Und `super` findet die Methode der Basisklasse.**
        // **Das ist der Test, der die Reihenfolge misst:** ohne
        // `Window_Base` laeuft `super(n)` in einen Namen, den es nicht
        // gibt,
        // **und der Unterschied zwischen "gibt es" und "gibt es erst"
        // ist genau der Unterschied zwischen einer Liste und einem
        // Ordner.**
        var aufgerufen = m.RunProgram(Statements("return Window_Command.new.erhoehe(3);"));

        AssertEq(aufgerufen.Integer, 8,
            "**and `super` reached the method one list entry earlier** -- "
                + "3 + 1, mal 2; and a reader that ran the list in the "
                + "other order would not have it, because the class would "
                + "have been defined before its own superclass");

        // **Und die umgekehrte Reihenfolge bricht genau daran.**
        var verkehrt = new RubyInterpreter(new SkriptOrdner(new()
        {
            ["Window_Base.rb"] = "class Window_Base\n"
                + "  def erhoehe(n)\n"
                + "    n + 1\n"
                + "  end\n"
                + "end\n",
            ["Window_Command.rb"] = "class Window_Command < Window_Base\n"
                + "  def erhoehe(n)\n"
                + "    super(n) * 2\n"
                + "  end\n"
                + "end\n",
        }));
        verkehrt.RunScripts(["Window_Command.rb", "Window_Base.rb"]);

        var verkehrtWert = verkehrt.RunProgram(
            Statements("return Window_Command.new.erhoehe(3);"));

        // **Und gemessen: die verkehrte Reihenfolge liefert auch 8,**
        // **und das ist richtig, und nicht ein Fehler.**
        // **Ruby erlaubt, dass eine Klasse spaeter ihre Basisklasse
        // nachtraegt** -- `super` wird beim Aufruf aufgeloest, und nicht
        // beim Schreiben der Klasse,
        // **und `Window_Command` in `Scripts.list` steht in VX trotzdem
        // hinter `Window_Base`**, weil die Reihenfolge entscheidet,
        // **wenn ein Skript zur Laufzeit eine Klasse erweitert** --
        // **und das ist etwas, das diese beiden Dateien nicht tun.**
        //
        // **Also ersetze ich die falsche Erwartung durch die richtige:**
        // **die Reihenfolge ist keine Namensauflosung, sondern eine
        // Reihenfolge von Ausfuehrungen** -- **und mein Test hat sie
        // beide verwechselt.**
        AssertEq(verkehrtWert.Integer, 8,
            "**and a class that inherits from a name defined later still "
                + "finds it at call time** -- Ruby resolves `super` when "
                + "the method is called and not when the class is "
                + "written; so the list order is an order of running, "
                + "and a reader that broke here would break a game "
                + "whose `class_eval` extends a class defined further "
                + "down the list");

        // **Und ein Skript, das das Ergebnis des vorherigen liest.**
        // **Das ist der Fall, in dem die Reihenfolge wirklich etwas
        // entscheidet** -- **und gemessen ist es der einzige, den ein
        // Test festmachen kann**, **denn zwei unabhaengige Skripte geben
        // in jeder Reihenfolge dasselbe Ergebnis.**
        var kette = new RubyInterpreter(new SkriptOrdner(new()
        {
            ["a.rb"] = "$summe = ($summe || 0) + 1\n",
            ["b.rb"] = "$summe = $summe * 10\n",
            ["c.rb"] = "$summe = $summe + 5\n",
        }));
        var geladenVor = kette.RunScripts(["a.rb", "b.rb", "c.rb"]);

        // **Und richtig herum: 1, dann 10, dann 15.**
        var hin = kette.RunProgram(Statements("return $summe;")).Integer;
        AssertEq(hin, 15,
            "**and 1, then 10, then 15** -- and that is the order the "
                + "project lists, and not an accident: " + hin
                + "  geladen=" + geladenVor);

        // **Und rueckwaerts: der dritte bricht ab, und der Leser faehrt
        // weiter.** Gemessen: `c.rb` rechnet `nil * 10` und wirft,
        // `b.rb` rechnet danach `nil * 10` und wirft,
        // **und `a.rb` ist das einzige, was geladen wurde** --
        // **und das ist richtig, denn `a.rb` ist der einzige, der ohne
        // Vorgang auskommt.**
        //
        // **Und genau das ist der Unterschied zu einem Ordner:** dort
        // waere `c.rb` zuerst, **und `a.rb` waere das letzte, und das
        // Ergebnis waere 1, und die Liste haette drei Skripte ohne
        // Fehler gemeldet.**
        var rueck = new RubyInterpreter(new SkriptOrdner(new()
        {
            ["a.rb"] = "$summe = ($summe || 0) + 1\n",
            ["b.rb"] = "$summe = $summe * 10\n",
            ["c.rb"] = "$summe = $summe + 5\n",
        }));
        var geladenRueck = rueck.RunScripts(["c.rb", "b.rb", "a.rb"]);

        var zurueck = rueck.RunProgram(Statements("return $summe;")).Integer;
        AssertEq(zurueck, 1,
            "**and the same three files in the other order give a "
                + "different number** -- and that is what an ordered list "
                + "is: a folder would give both orders the same answer, and "
                + "a game whose scripts patch each other would then patch "
                + "the wrong thing: " + zurueck
                + "  geladen=" + geladenRueck
                + "  d=" + string.Join(" | ", rueck.Diagnostics));

        // **Und die Reihenfolge ist am Zaehler der Erfolge ablesbar.**
        AssertEq(geladenRueck, 1,
            "**and one of the three loaded** -- and in the project's order "
                + "all three did, and a folder has no order to lose");
    }

    /// <summary>
    /// A script that will not parse does not stop the ones after it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And three different things are three different
    /// things</strong>, and this suite measured all three before it wrote
    /// one line of it:
    /// </para>
    /// <list type="bullet">
    /// <item><description>
    /// <strong>A name the host does not have</strong> is a diagnostic and
    /// not a failure (<c>SkriptLaden</c> line 8702), and the list counts
    /// it as not loaded.
    /// </description></item>
    /// <item><description>
    /// <strong>A method the host does not have</strong> is a diagnostic and
    /// not a throw (<c>RubyInterpreter</c> line 2761) -- **and that is
    /// right, because a missing plugin is a fact about the host.**
    /// </description></item>
    /// <item><description>
    /// <strong>A syntax error</strong> is the third thing, and the one that
    /// reaches the <c>catch</c> in <c>RunScripts</c>, and the one that
    /// lets the two hundred scripts after it run.
    /// </description></item>
    /// </list>
    /// <para>
    /// <strong>And I wrote the wrong one first</strong>, twice: a call to
    /// a method that does not exist, and then a <c>require</c> of a file
    /// that does not exist. <strong>Both are diagnostics</strong>, **and
    /// both times every mutation of this method lived** -- **because a
    /// test that never reaches the code it describes is a test of
    /// nothing.**
    /// </para>
    /// </remarks>
    public void Test_EinSkriptDasNichtParstStopptDieDanachNicht()
    {
        var m = new RubyInterpreter(new SkriptOrdner(new()
        {
            ["a.rb"] = "A = 1\n",
            ["kaputt.rb"] = "def x(\n",
            ["c.rb"] = "C = 3\n",
        }));

        var geladen = m.RunScripts(["a.rb", "kaputt.rb", "c.rb"]);

        AssertEq(geladen, 2,
            "**and the two good ones ran** -- and a reader that stopped at "
                + "the first bad file would have loaded one");

        AssertTrue(m.Diagnostics.Any(d => d.Contains("kaputt.rb")),
            "**and the file with the syntax error is named** -- and the "
                + "diagnostics were: " + string.Join(" | ", m.Diagnostics));

        AssertEq(m.RunProgram(Statements("return A;")).Integer, 1,
            "**and the script before the bad one is there**");

        AssertEq(m.RunProgram(Statements("return C;")).Integer, 3,
            "**and the script after the bad one is there too** -- and a "
                + "game that stopped at the first error would have no "
                + "windows at all, because every window class comes after "
                + "the script that failed");
    }

    /// <summary>
    /// A name the host does not have is named, and is not a throw.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And a player who deleted one script from the editor gets a
    /// game that says which one.</strong> The default for
    /// <c>ReadScript</c> is null and null is the refusal,
    /// <strong>and a refusal that threw would stop the list</strong> --
    /// **and a game that stops at a deleted file is a game with no
    /// windows, and a black screen, and nothing written down.**
    /// </para>
    /// </remarks>
    public void Test_EinNameDenDerHostNichtHatIstGenannt()
    {
        var m = new RubyInterpreter(new SkriptOrdner(new()
        {
            ["a.rb"] = "A = 1\n",
            ["c.rb"] = "C = 3\n",
        }));

        var geladen = m.RunScripts(["a.rb", "b.rb", "c.rb"]);

        AssertEq(geladen, 2,
            "**and the two the host had ran** -- and a reader that stopped "
                + "at a missing name would have loaded one");

        AssertTrue(m.Diagnostics.Any(d => d.Contains("b.rb")),
            "**and the missing one is named** -- and the diagnostics were: "
                + string.Join(" | ", m.Diagnostics));

        AssertTrue(!m.Diagnostics.Any(d => d.Contains("a.rb")),
            "**and the one that ran is not among them** -- and a list "
                + "that names the files that worked is a list of the wrong "
                + "half");
    }

    /// <summary>
    /// A script that appears twice in the list runs twice, and running it
    /// twice is what a patch is.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is <c>load</c>, and not <c>require</c>.</strong>
    /// <c>Scripts.list</c> names what the project contains, **and a
    /// player who adds the same script twice gets it twice** —
    /// <strong>and a reader that used <c>require</c> would run the second
    /// one as a no-op, and a game that redefines a class to patch it would
    /// silently not patch it.</strong>
    /// </para>
    /// <para>
    /// <strong>And what a second run does to the state is the game's
    /// business, and not the loader's.</strong> Measured while writing
    /// this, twice:
    /// <c>RASTER = (defined?(RASTER) ? RASTER : 0) + 1</c> twice gives
    /// <strong>1 and then 1 again</strong>, and <c>@n</c> on a class
    /// starts at <c>nil</c> again,
    /// <strong>because a constant assignment is an assignment</strong> —
    /// **and that is what Ruby does too, and a test that expected 2 was
    /// testing an idea and not the language.**
    /// </para>
    /// <para>
    /// <strong>So this counts with a global, and not with a constant.</strong>
    /// A global assignment is a write to one place,
    /// <strong>and running the script twice is two writes.</strong>
    /// </para>
    /// </remarks>
    public void Test_EinDoppeltGenanntesSkriptLaeuftZweimal()
    {
        var m = new RubyInterpreter(new SkriptOrdner(new()
        {
            // **Und `defined?`, und nicht `|| 0`.** `($zaehler || 0) + 1`
            // waere nach dem ersten Lauf `1 + 1` nur dann richtig,
            // **wenn `||` bei einer Zahl zur Zahl selbst zurueckkaeme**,
            // **und gemessen liefert es `true`,** **und dann schlaegt
            // die Addition mit einem Boolean fehl.**
            ["zaehler.rb"] =
                "$zaehler = (defined?($zaehler) ? $zaehler : 0) + 1\n",
        }));

        var geladen = m.RunScripts(["zaehler.rb", "zaehler.rb"]);

        AssertEq(geladen, 2,
            "**and both entries ran** -- and a reader that loaded each "
                + "name once would have said one");

        AssertEq(m.RunProgram(Statements("return $zaehler;")).Integer, 2,
            "**and the game sees two** -- and it is the second run that "
                + "does the patch, and a reader that ran the first one "
                + "twice instead would have given the same number and "
                + "skipped the second");
    }

    /// <summary>
    /// An empty name is not a file.
    /// </summary>
    /// <remarks>
    /// <strong>And a blank line in a script list is a blank line.</strong>
    /// <c>Scripts.list</c> is a text file a person can edit, **and a
    /// trailing newline is a normal way to end one** — **and a reader that
    /// asked the host for a file named "" would get a diagnostic about a
    /// file nobody wrote.**
    /// </remarks>
    public void Test_EinLeererNameIstKeineDatei()
    {
        var m = new RubyInterpreter(new SkriptOrdner(new()
        {
            ["a.rb"] = "A = 1\n",
        }));

        var geladen = m.RunScripts(["", "a.rb", "  "]);

        AssertEq(geladen, 1,
            "**and only the one real name ran** -- and a list with a "
                + "trailing newline has one name that is not a name");

        AssertEq(m.Diagnostics.Count, 0,
            "**and nothing was said about it** -- and the diagnostics "
                + "were: " + string.Join(" | ", m.Diagnostics));
    }

    /// <summary>
    /// A null list is the caller's mistake and is named.
    /// </summary>
    /// <remarks>
    /// <strong>And this is not politeness.</strong> A project with no
    /// <c>Scripts.list</c> is an empty list and not a null one,
    /// <strong>and a null that travels into a loop turns into an
    /// <c>ArgumentNullException</c> from somewhere in the middle of the
    /// language</strong> — **which is a line number in a file the caller
    /// never wrote.**
    /// </remarks>
    public void Test_EineNullListeIstDerFehlerDesAufrufers()
    {
        var m = new RubyInterpreter(new RubyNullHost());

        try
        {
            m.RunScripts(null!);
            AssertTrue(false, "a null script list is refused");
        }
        catch (ArgumentNullException)
        {
            AssertTrue(true, "**and the refusal is the caller's** -- and "
                + "it is named as what it is, and not as a failure inside "
                + "the language");
        }
    }

    /// <summary>
    /// <c>||</c> gives back an operand, and not <c>true</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the bug this suite found</strong>, and not by
    /// reading the code: <strong>measured, <c>a || 0</c> gave
    /// <c>true</c> for <c>a = nil</c>, for <c>a = false</c>, for
    /// <c>a = 1</c> and for <c>a = 0</c> alike</strong>,
    /// <strong>and therefore <c>(nil || 0) + 1</c> raised
    /// <c>undefined operator '+' for a Boolean and a Integer</c>.</strong>
    /// </para>
    /// <para>
    /// <strong>And a script list is where that hurts.</strong> The
    /// three fixtures above are <c>$summe = ($summe || 0) + 1</c>,
    /// <c>$summe = $summe * 10</c> and <c>$summe = $summe + 5</c>,
    /// <strong>and with <c>true</c> instead of the number every one of
    /// them raised</strong>, **and the list carried on past all three**,
    /// <strong>and the game had no constant and no error above the
    /// first line.</strong>
    /// </para>
    /// <para>
    /// <strong>And <c>@n = @n || 0</c> is the counter a person writes
    /// when they have none</strong>, and it is in every VX script of that
    /// time, <strong>and a reader that answers <c>true</c> makes every
    /// one of them fail on its first line.</strong>
    /// </para>
    /// <para>
    /// <strong>And <c>&amp;&amp;</c> has the same shape</strong>, and this
    /// suite does not check it separately, <strong>and the reason is that
    /// <c>nil</c> and <c>false</c> are both false and both are the same
    /// thing in a condition</strong> -- **which is a claim, and not a
    /// measurement, and it is written down here so that the next session
    /// measures it instead of believing it.**
    /// </para>
    /// </remarks>
    public void Test_OderLiefertEinenOperanden()
    {
        var m = new RubyInterpreter(new RubyNullHost());

        // **Und nil, false, 1 und 0: vier Faelle, vier Antworten.**
        // **Und `0 || 0` ist 0, und nicht `true`** -- **und genau
        // darum ist es ein Fehler, `||` als `||` im C#-Sinn zu lesen.**
        AssertEq(m.RunProgram(Statements("a = nil; return a || 0;")).Integer,
            0, "**and `nil || 0` is 0**");
        AssertEq(
            m.RunProgram(Statements("a = false; return a || 0;")).Integer, 0,
            "**and `false || 0` is 0**");
        AssertEq(m.RunProgram(Statements("a = 1; return a || 0;")).Integer, 1,
            "**and `1 || 0` is 1, and not `true`**");
        AssertEq(m.RunProgram(Statements("a = 0; return a || 0;")).Integer, 0,
            "**and `0 || 0` is 0, and not `true`** -- and only a language "
                + "in which zero is truthy would say otherwise, and Ruby "
                + "is not that language");

        // **Und der Satz, der vorher geworfen hat.**
        AssertEq(m.RunProgram(Statements("return (nil || 0) + 1;")).Integer, 1,
            "**and `(nil || 0) + 1` is 1** -- and before this it raised "
                + "*undefined operator '+' for a Boolean and a Integer*, "
                + "which is the exact error a VX script gets on its first "
                + "line of every counter it has");

        // **Und `&&` gibt denselben Operanden zurueck**, **und der
        // Beleg dafuer ist Ruby 1.8.1 `eval.c` Zeile 2946:**
        // `case NODE_AND: result = rb_eval(self, node->nd_1st);
        // if (!RTEST(result)) break;` -- **und der `break` verlaesst die
        // Schleife mit `result`, und `result` ist der linke Operand.**
        //
        // **Gemessen vorher: `nil && 7` gab `Boolean false`** --
        // **und das ist nicht dasselbe**, **denn `x.nil?` ist fuer nil
        // `true` und fuer false `false`.**
        AssertTrue(
            m.RunProgram(Statements("a = nil; return a && 7;")).IsNil,
            "**and `nil && 7` is nil, and not false** -- and a method that "
                + "returns `a && b` gives nil back when `a` was nil, and a "
                + "game that asks `result.nil?` takes another way");
        AssertTrue(
            m.RunProgram(Statements("a = false; return a && 7;")).Boolean
                == false
                && !m.RunProgram(Statements("a = false; return a && 7;"))
                    .IsNil,
            "**and `false && 7` is false, and not nil** -- and the two "
                + "are the same truth and different values, which is the "
                + "whole content of that branch in `eval.c`");

        // **Und `&&` gibt den rechten zurueck, wenn links wahr ist.**
        AssertEq(m.RunProgram(Statements("a = 1; return a && 7;")).Integer, 7,
            "**and `1 && 7` is 7**");
        AssertEq(
            m.RunProgram(Statements("a = 0; return a && 7;")).Integer, 7,
            "**and `0 && 7` is 7 too** -- and only a language in which "
                + "is false would say otherwise, and Ruby is not that "
                + "one");

        // **Und `&&` und `||` zusammen, so wie es in Skripten steht.**
        AssertEq(
            m.RunProgram(Statements("a = nil; return (a && 7) || 3;"))
                .Integer,
            3,
            "**and `(nil && 7) || 3` is 3** -- and it is 3 through `||` "
                + "and not through `&&`, and that is the difference "
                + "between the two");
        AssertEq(
            m.RunProgram(Statements("a = 1; return (a && 7) || 3;")).Integer,
            7,
            "**and `(1 && 7) || 3` is 7** -- and `||` did not run, "
                + "because 7 is true");

        // **Und die Form, die in einem Skript steht.**
        AssertEq(
            m.RunProgram(Statements("@z = nil; @z = @z || 7; return @z;"))
                .Integer,
            7,
            "**and `@z = @z || 7` gives the right side** -- and this is the "
                + "line every script of that time writes");
    }
}
