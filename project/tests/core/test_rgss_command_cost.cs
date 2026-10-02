using System;
using System.Collections.Generic;
using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// What the commands of a real XP game cost to run, read out of their
/// own Ruby.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is the question a runtime actually has to
/// answer.</strong> Not "what does command 121 do" -- the body says
/// that -- <strong>but "how much of this game can run without a Ruby
/// interpreter", and the answer comes from the bodies too.</strong>
/// </para>
/// <para>
/// <strong>And this test asserts no counts it has not measured.</strong>
/// The first version of it asserted <c>Bildschirm=26</c> and
/// <c>ZustandsSchreibend=30</c> and both were invented, -- <strong>and
/// a test with invented numbers in it is a test that fails for a
/// reason nobody can explain.</strong> The distribution is measured and
/// printed; the assertions hold the line between the kinds.
/// </para>
/// </remarks>
public partial class TestRgssCommandCost : TestBase
{
    private static readonly string XpSkripte =
        "E:/RPGMakerGames/MicroQuest - Beneath Brimestone 1.0/Data"
        + "/Scripts.rxdata";

    private static List<RgssQuellBefehl> Befehle() =>
        RgssQuellBefehle.Lese(XpSkripte, out _);

    private static RgssQuellBefehl? Suche(
        List<RgssQuellBefehl> pListe, int pCode)
    {
        foreach (var q in pListe)
        {
            if (q.Code == pCode)
            {
                return q;
            }
        }

        return null;
    }

    /// <summary>
    /// And the distribution is measured and printed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the honest figure for the goal.</strong> A
    /// command that only writes a switch is runnable now; a command
    /// that sets <c>@message_waiting</c> needs a message window and a
    /// wait mode; a command whose body evaluates Ruby needs a language
    /// this repository deliberately does not run on untrusted input.
    /// </para>
    /// <para>
    /// <strong>And <c>Unbekannt</c> is the largest or second largest
    /// bucket and that is not a defect to be hidden</strong>, --
    /// <strong>it is the honest size of the part this repository has
    /// not read yet.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieVerteilungIstGemessenUndNichtErfunden()
    {
        var befehle = Befehle();
        var verteilung = RgssBefehlArten.Verteilung(befehle);
        var teile = new List<string>();
        foreach (var eintrag in verteilung)
        {
            teile.Add(eintrag.Key + "=" + eintrag.Value);
        }

        teile.Sort(StringComparer.Ordinal);
        System.Console.WriteLine(
            "XP " + befehle.Count + " Befehle: "
            + string.Join(" ", teile));

        AssertTrue(befehle.Count > 80,
            "**and the game's commands were read** -- and there are "
                + befehle.Count);
        AssertTrue(verteilung.ContainsKey(RgssBefehlArt.Unbekannt),
            "**and the unread part is named rather than hidden** -- and"
                + " " + verteilung[RgssBefehlArt.Unbekannt]
                + " commands this repository cannot classify yet");

        var erkannt = 0;
        foreach (var q in befehle)
        {
            if (RgssBefehlArten.Von(q) != RgssBefehlArt.Unbekannt)
            {
                erkannt++;
            }
        }

        AssertEq(erkannt + verteilung[RgssBefehlArt.Unbekannt],
            befehle.Count,
            "**and every command is in exactly one bucket** -- and "
                + erkannt + " classified and "
                + verteilung[RgssBefehlArt.Unbekannt] + " unknown");
    }

    /// <summary>
    /// And show text is a screen command, and the body says so.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the case that proves the classification
    /// reads and does not guess.</strong> <c>command_101</c> is the
    /// command a game spends the most visible time in, -- <strong>and
    /// three wrong orders classified it as a question and then as a
    /// branch</strong>, -- <strong>and both times the reason was a line
    /// in a comment.</strong>
    /// </para>
    /// <para>
    /// <strong>And <c>command_102</c> is show choices, not show
    /// text</strong>, -- <strong>and it must come out
    /// <c>Eingabe</c></strong>, -- <strong>and a classifier that gets
    /// that wrong will run a game's dialogue without a single
    /// question.</strong>
    /// </para>
    /// </remarks>
    public void Test_TextIstBildschirmUndAuswahlIstEingabe()
    {
        var befehle = Befehle();
        var text = Suche(befehle, 101);
        var auswahl = Suche(befehle, 102);
        AssertTrue(text != null && auswahl != null,
            "**and both commands are in the game's own script** -- and"
                + " that is where the difference between them is written");

        // **Und wenn die Klassifikation 101 nicht als Bildschirm
        // erkennt, dann ist entweder das Muster falsch oder die
        // Koerper sind es** -- **und beides ist eine Tatsache, die man
        // liest und nicht rät.**
        AssertEq(RgssBefehlArten.Von(text!), RgssBefehlArt.Bildschirm,
            "**and show text puts something on screen** -- and its own"
                + " body sets $game_temp.message_text and"
                + " @message_waiting");
        AssertEq(RgssBefehlArten.Von(auswahl!), RgssBefehlArt.Eingabe,
            "**and show choices asks the player** -- and its own body"
                + " calls setup_choices");

        // **Und beide rufen `setup_choices` auf** -- **und trotzdem
        // sind sie verschiedene Arten**, -- **und der Unterschied ist
        // eine Klammer:**
        //
        // ```ruby
        // # command_101,  Zeile 27   setup_choices(@list[@index].parameters)
        // # command_102,  Zeile 3    setup_choices(@parameters)
        // ```
        //
        // **Und `@list[@index].parameters` ist der Nachbarbefehl**,
        // -- **und `@parameters` ist der eigene**, -- **und das ist die
        // Regel des Interpreters und nicht eine Besonderheit dieser
        // zwei Befehle.**
        //
        // **Und 101 stellt also KEINE Frage**, -- **und es leitet nur
        // weiter, wenn der naechste Befehl eine ist** -- **und genau
        // darum ist es Bildschirm und nicht Eingabe.**
        var imNachbarn = 0;
        var imEigenen = 0;
        var imKommentar = 0;
        foreach (var q in new[] { text!, auswahl! })
        {
            foreach (var zeile in q.Koerper.Split('\n'))
            {
                var geschnitten = zeile.Trim();
                if (!geschnitten.Contains("setup_choices(",
                    StringComparison.Ordinal))
                {
                    continue;
                }

                if (geschnitten.StartsWith("#", StringComparison.Ordinal))
                {
                    imKommentar++;
                    continue;
                }

                if (geschnitten.Contains("@list[", StringComparison.Ordinal))
                {
                    imNachbarn++;
                    continue;
                }

                imEigenen++;
            }
        }

        AssertEq(imEigenen, 1,
            "**and exactly one of them asks with its own parameters**"
                + " -- and that is command 102, and there are "
                + imEigenen + " calls of that kind");
        AssertTrue(imNachbarn > 0,
            "**and show text sets up the neighbour's choices** -- and"
                + " " + imNachbarn + " calls do that, and that is why"
                + " show text is a screen command and not a question");
        // **Und `imKommentar` ist 0** -- **und das ist auch gemessen**,
        // -- **und meine erste Fassung behauptete das Gegenteil.**
        //
        // **Und der Weg zu "Zeige Text ist eine Frage" lief trotzdem
        // ueber Kommentare** -- **aber ueber ein anderes Muster:  das
        // blosse Wort `choice`.**
        AssertEq(imKommentar, 0,
            "**and setup_choices is never commented out** -- and it"
                + " appears " + imKommentar + " times in a comment, and"
                + " my first version of this test said otherwise");
        AssertTrue(text!.Koerper.Contains("choice",
                StringComparison.Ordinal),
            "**and show text does mention choices** -- and in"
                + " `# If next event command is show choices` and in"
                + " `choice_start`, which is why the word alone"
                + " cannot decide what a command is");
    }

    /// <summary>
    /// And a state-only command writes and nothing else.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the kind this repository can run today</strong>,
    /// -- <strong>and the test holds the line between "can run" and
    /// "could run if someone were brave".</strong>
    /// </para>
    /// </remarks>
    public void Test_EinZustandsbefehlSchreibtNurUndZeigtNichts()
    {
        var befehle = Befehle();
        var zustand = 0;
        foreach (var q in befehle)
        {
            if (RgssBefehlArten.Von(q) != RgssBefehlArt.ZustandsSchreibend)
            {
                continue;
            }

            zustand++;
            AssertFalse(q.Koerper.Contains("$game_temp.message_text",
                    StringComparison.Ordinal),
                "**and a state-only command shows no text** -- and "
                    + "command " + q.Code + " does");
            AssertFalse(q.Koerper.Contains("setup_choices",
                    StringComparison.Ordinal),
                "**and a state-only command asks nothing** -- and "
                    + "command " + q.Code + " does");
            AssertTrue(q.Koerper.Contains("$game_switches",
                    StringComparison.Ordinal)
                || q.Koerper.Contains("$game_variables",
                    StringComparison.Ordinal)
                || q.Koerper.Contains("$game_self_switches",
                    StringComparison.Ordinal),
                "**and it really does write state** -- and command "
                    + q.Code + " writes one of the three stores");
        }

        System.Console.WriteLine("Zustandsschreibend: " + zustand);
        AssertTrue(zustand > 0,
            "**and there is at least one** -- and there are " + zustand);
    }

    /// <summary>
    /// And command 121 is a loop over switches, not one assignment.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the case the whole classification
    /// exists for.</strong> A table written from memory says "121: set
    /// switch", -- <strong>and the game's own Ruby says it loops from
    /// <c>@parameters[0]</c> to <c>@parameters[1]</c> over
    /// <c>$game_switches[i]</c></strong>, -- <strong>and a runtime
    /// built on the memory would write one switch where the game writes
    /// a range.</strong>
    /// </para>
    /// </remarks>
    public void Test_Befehl121IstEineSchleifeUeberSchalter()
    {
        var befehle = Befehle();
        var q = Suche(befehle, 121);
        AssertTrue(q != null,
            "**and command 121 is in the game's own script**");
        AssertTrue(q!.Koerper.Contains("@parameters[0]",
                StringComparison.Ordinal)
            && q.Koerper.Contains("@parameters[1]",
                StringComparison.Ordinal),
            "**and it reads both ends of a range** -- and a runtime"
                + " that read only the first would set one switch");
        AssertTrue(q.Koerper.Contains("$game_switches[i]",
                StringComparison.Ordinal),
            "**and it writes each switch of the range**");
        AssertEq(RgssBefehlArten.Von(q), RgssBefehlArt.ZustandsSchreibend,
            "**and it is a state-only command** -- and it shows"
                + " nothing and asks nothing");
    }

    /// <summary>
    /// And a command with no body is unknown and not cheap.
    /// </summary>
    /// <remarks>
    /// <strong>And an empty body would otherwise fall through to the
    /// cheapest kind</strong>, -- <strong>and a runtime would then skip
    /// it and report it as done.</strong>
    /// </remarks>
    public void Test_EinBefehlOhneKoerperIstUnbekannt()
    {
        var leer = new RgssQuellBefehl { Code = 999, Koerper = "" };
        AssertEq(RgssBefehlArten.Von(leer), RgssBefehlArt.Unbekannt,
            "**and a command without a body is unknown** -- and not"
                + " the cheapest kind, which would be a lie");
    }
}
