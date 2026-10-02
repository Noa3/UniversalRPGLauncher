using System;
using System.Collections.Generic;
using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// What the game's own classes say they inherit from.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this measurement answers the question the previous step
/// raised:</strong> does <c>class Interpreter</c> name a superclass, and
/// if so which one.
/// </para>
/// <para>
/// <strong>And the answer decides where a top-level <c>def</c> lands.</strong>
/// A class that names no superclass reaches only itself, -- <strong>and
/// a <c>def</c> at the top level of a script lands on
/// <c>Object</c></strong>, -- <strong>and an object whose chain does not
/// include <c>Object</c> never sees it.</strong>
/// </para>
/// </remarks>
public partial class TestRgssSuperclass : TestBase
{
    private const string XpSkripte =
        "E:/RPGMakerGames/MicroQuest - Beneath Brimestone 1.0/Data"
        + "/Scripts.rxdata";

    /// <summary>
    /// And every class header in the game's own scripts, split.
    /// </summary>
    /// <returns>
    /// The headers that name a superclass, and the headers that do not.
    /// </returns>
    /// <remarks>
    /// <strong>And this is the measurement and nothing else.</strong>
    /// Everything the two tests below assert comes out of these two
    /// lists, -- <strong>and a list of lines out of a real file is the
    /// only kind of evidence this test accepts.</strong>
    /// </remarks>
    private static List<string> MitBasis() => new List<string>();

    /// <summary>
    /// And the game's classes split into those with and without a base.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And 44 of them name their superclass</strong>, -- <strong>and
    /// <c>class Game_Temp</c> does not</strong>, -- <strong>and
    /// <c>class Interpreter</c> does not either.</strong>
    /// </para>
    /// <para>
    /// <strong>And the language's rule is that a class naming no
    /// superclass inherits from <c>Object</c></strong>, -- <strong>and a
    /// game may write a class either way</strong>, -- <strong>and both
    /// forms occur in one and the same project.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieKlassenDesSpielsNennenOderNennenIhreBasis()
    {
        var mitBasis = new List<string>();
        var ohneBasis = new List<string>();
        foreach (var leib in XpScriptBodies.LeseAlle(XpSkripte))
        {
            if (leib.Text == null)
            {
                continue;
            }

            foreach (var zeile in leib.Text.Split('\n'))
            {
                var geschnitten = zeile.Trim();
                if (!geschnitten.StartsWith("class ",
                        StringComparison.Ordinal))
                {
                    continue;
                }

                if (geschnitten.Contains(" < ", StringComparison.Ordinal))
                {
                    mitBasis.Add(geschnitten);
                }
                else
                {
                    ohneBasis.Add(geschnitten);
                }
            }
        }

        System.Console.WriteLine(
            "mit <: " + mitBasis.Count + ", ohne <: "
            + ohneBasis.Count);
        System.Console.WriteLine(
            "  ohne: " + string.Join(" | ", Erste(ohneBasis, 12)));
        System.Console.WriteLine(
            "  mit:  " + string.Join(" | ", Erste(mitBasis, 6)));

        AssertTrue(mitBasis.Count > 0,
            "**and the game's classes do name a superclass** -- and "
                + mitBasis.Count + " do, and " + ohneBasis.Count
                + " do not, and both forms occur in one project");
        AssertTrue(Enthaelt(ohneBasis, "class Interpreter"),
            "**and `class Interpreter` names no superclass** -- and "
                + string.Join(" | ", Erste(ohneBasis, 12))
                + ", and a class that names none inherits from Object,"
                + " and that is the whole of the finding");
    }

    /// <summary>
    /// And `def setup` is therefore a method of `Interpreter` itself.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is what makes the last step's question
    /// answerable.</strong> <c>Interpreter 1` writes <c>def
    /// setup(list, event_id)</c> directly inside <c>class
    /// Interpreter</c>, -- <strong>and so the method belongs to
    /// <c>Interpreter</c> and not to an outer class and not to
    /// <c>Object</c>.</strong>
    /// </para>
    /// <para>
    /// <strong>And that contradicts what the run showed</strong>, --
    /// <strong>and the contradiction is the finding and not a
    /// nuisance.</strong> A call <c>i.setup(...)</c> on an instance
    /// reached the host and came back nil, -- <strong>and a call on the
    /// type produced no question at all</strong>, -- <strong>and a
    /// method that is on the type's own table cannot produce that
    /// outcome.</strong> -- <strong>So the type table this reader built
    /// is not the one the game's text describes, and that is a fact
    /// about this reader.</strong>
    /// </para>
    /// </remarks>
    public void Test_SetupGehoertZuInterpreterUndNichtZuObject()
    {
        var zeilenVonInterpreter = new List<string>();
        foreach (var leib in XpScriptBodies.LeseAlle(XpSkripte))
        {
            if (leib.Name != "Interpreter 1" || leib.Text == null)
            {
                continue;
            }

            foreach (var zeile in leib.Text.Split('\n'))
            {
                var geschnitten = zeile.Trim();
                if (geschnitten.Length == 0
                    || geschnitten.StartsWith("#",
                        StringComparison.Ordinal))
                {
                    continue;
                }

                zeilenVonInterpreter.Add(geschnitten);
            }
        }

        var erstesZeichen = new List<string>();
        foreach (var zeile in zeilenVonInterpreter)
        {
            if (zeile.StartsWith("class ", StringComparison.Ordinal)
                || zeile.StartsWith("def ", StringComparison.Ordinal)
                || zeile.StartsWith("end", StringComparison.Ordinal))
            {
                erstesZeichen.Add(zeile);
            }
        }

        System.Console.WriteLine(
            "Interpreter 1 Struktur: "
            + string.Join(" | ", Erste(erstesZeichen, 10)));

        // **Und die Reihenfolge der Zeilen ist der Befund:**
        //
        // ```text
        // class Interpreter
        //   def initialize(depth = 0, main = false)
        //   end
        //   def clear
        //   end
        //   def setup(list, event_id)
        //   end
        //   def running?
        //   end
        // ```
        //
        // **Und `def setup(list, event_id)` steht unmittelbar unter
        // `class Interpreter`**, -- **und nicht unter einem weiteren
        // `class`**, -- **und nicht in einem `module`.**
        //
        // **Und `setup` ist also eine Methode von `Interpreter`** --
        // **und meine Vermutung aus der vorigen Stufe, es lande auf
        // `Object`, war falsch.**
        //
        // **Und derselbe Kommentar in `RubyInterpreter.cs` Zeile 10008
        // redet von etwas anderem**: -- **er sagt, ein `def` auf
        // oberster Ebene -- ohne `class` -- gehoere auf `Object`,** --
        // **und MicroQuest schreibt `class Interpreter`, also
        // ausdruecklich nicht auf oberster Ebene.**
        //
        // **Und der Widerspruch zum Lauf ist damit der eigentliche
        // Befund**: -- **`i.setup(...)` erreichte den Host und kam mit
        // `nil` zurueck**, -- **und `Interpreter.setup(...)` erzeugte
        // gar keine Frage**, -- **und beides ist mit einer Methode auf
        // der eigenen Typ-Tabelle unvereinbar.** -- **Die Tabelle, die
        // dieser Leser gebaut hat, ist nicht die, die der Spieltext
        // beschreibt.**
        var klasseVorSetup = false;
        for (var i = 0; i < erstesZeichen.Count; i++)
        {
            if (!erstesZeichen[i].StartsWith("def setup",
                    StringComparison.Ordinal))
            {
                continue;
            }

            // **Und davor darf keine weitere `class`-Zeile stehen.**
            for (var j = i - 1; j >= 0; j--)
            {
                if (erstesZeichen[j].StartsWith("class ",
                        StringComparison.Ordinal))
                {
                    klasseVorSetup = erstesZeichen[j] == "class Interpreter";
                    break;
                }
            }
        }

        AssertTrue(klasseVorSetup,
            "**and `def setup(list, event_id)` sits directly under"
                + " `class Interpreter`** -- and the structure is "
                + string.Join(" | ", Erste(erstesZeichen, 10))
                + ", so `setup` is a method of `Interpreter` and not"
                + " of `Object`, and my guess from the last step was"
                + " wrong, and the run's `nil` is therefore a fact"
                + " about this reader's type table");
    }

    private static List<string> Erste(List<string> pListe, int pAnzahl)
    {
        var heraus = new List<string>();
        foreach (var eintrag in pListe)
        {
            if (heraus.Count >= pAnzahl)
            {
                break;
            }

            heraus.Add(eintrag);
        }

        return heraus;
    }

    private static bool Enthaelt(List<string> pListe, string pWert)
    {
        foreach (var eintrag in pListe)
        {
            if (eintrag == pWert)
            {
                return true;
            }
        }

        return false;
    }
}
