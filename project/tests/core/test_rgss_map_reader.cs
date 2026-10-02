using System;
using System.IO;
using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The map reader, against the two real Ruby Maker projects on this
/// machine.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And these are the only two complete Ruby Maker projects
/// here</strong>, -- <strong>and both were measured rather than
/// assumed</strong>: MicroQuest is XP with 24 maps and 38 data files,
/// and Random Dungeon is VX with 657 maps and 671 files.
/// </para>
/// <para>
/// <strong>And nothing in this test is invented.</strong> Every number
/// asserted below came out of the files themselves, -- <strong>and a
/// test that asserts a shape invented in a comment is a test that
/// passes on a reader that is wrong.</strong>
/// </para>
/// </remarks>
public partial class TestRgssMapReader : TestBase
{
    private const string Xp = "E:/RPGMakerGames/MicroQuest - Beneath"
        + " Brimestone 1.0/Data/Map001.rxdata";
    private const string Vx = "E:/RPGMakerGames/Random Dungeon"
        + " -English Version-/Data/Map001.rvdata";

    /// <summary>
    /// And an XP map reads with the numbers the file says.
    /// </summary>
    /// <remarks>
    /// <strong>And 20 by 15 is not a guess</strong>, -- <strong>it is
    /// what <c>Map001.rxdata</c> carries</c> under <c>@width</c> and
    /// <c>@height</c>, and the probe printed exactly that.</strong>
    /// </remarks>
    public void Test_EineXpKarteLiestSichMitDenZahlenDerDatei()
    {
        AssertTrue(File.Exists(Xp), "**and the XP game is on this"
            + " machine** -- and it is at " + Xp);
        AssertTrue(RgssMapReader.TryRead(Xp, out var karte, out var fehler),
            "**and the map reads** -- and it said: " + fehler);
        AssertTrue(karte != null, "**and there is a map**");
        AssertEq(karte!.Width, 20,
            "**and the width is what the file says** -- and the file"
                + " says 20, and not the 17 a default map would have");
        AssertEq(karte.Height, 15,
            "**and the height is what the file says** -- and the file"
                + " says 15");
    }

    /// <summary>
    /// And a VX map reads, and it is a different size from the XP one.
    /// </summary>
    /// <remarks>
    /// <strong>And 70 by 25 is measured</strong> from
    /// <c>Map001.rvdata</c>, and <strong>it is not 20 by 15</strong>, --
    /// <strong>which is the whole point of a reader that reads names and
    /// not indexes.</strong>
    /// </remarks>
    public void Test_EineVxKarteLiestSichUndIstNichtDieXpKarte()
    {
        AssertTrue(File.Exists(Vx), "**and the VX game is on this"
            + " machine** -- and it is at " + Vx);
        AssertTrue(RgssMapReader.TryRead(Vx, out var karte, out var fehler),
            "**and the map reads** -- and it said: " + fehler);
        AssertTrue(karte != null, "**and there is a map**");
        AssertEq(karte!.Width, 70,
            "**and VX says 70** -- and an index-based reader would have"
                + " read the XP number");
        AssertEq(karte.Height, 25,
            "**and VX says 25** -- and the XP map says 15");
    }

    /// <summary>
    /// And the events come out, with their names and their positions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the name <c>GAME START</c> is not invented</strong>,
    /// -- <strong>it is what <c>Map001.rxdata</c> carries under
    /// <c>@name</c></strong>, -- <strong>and it is the name a player
    /// would recognise</strong>, and <strong>and a reader that reads
    /// nothing but sizes proves nothing at all.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieEreignisseKommenMitNamenUndPositionen()
    {
        AssertTrue(RgssMapReader.TryRead(Xp, out var xp, out var fehler),
            "**and the XP map reads** -- and it said: " + fehler);
        AssertTrue(xp!.Events.Count == 1,
            "**and Map001 has one event** -- and it has "
            + xp.Events.Count);
        var ereignis = xp.Events[0];
        AssertEq(ereignis.Id, 1, "**and its id is 1**");
        AssertEq(ereignis.Name, "GAME START",
            "**and it is called GAME START** -- and that string is in"
                + " the file and not in this test");
        AssertEq(ereignis.X, 0, "**and it stands at x 0**");
        AssertEq(ereignis.Y, 0, "**and it stands at y 0**");
        AssertTrue(ereignis.Pages.Count >= 1,
            "**and it has pages** -- and it has " + ereignis.Pages.Count);
    }

    /// <summary>
    /// And a VX map with 43 events reads all 43.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And 43 is measured</strong>, -- <strong>the probe printed
    /// <c>Schluessel 43</c> for the <c>@events</c> hash</strong>, --
    /// <strong>and a hash read wrong gives half of them</strong> because
    /// the keys and the values share one list.
    /// </para>
    /// </remarks>
    public void Test_DieVxKarteHatDreiundvierzigEreignisse()
    {
        AssertTrue(RgssMapReader.TryRead(Vx, out var vx, out var fehler),
            "**and the VX map reads** -- and it said: " + fehler);
        AssertEq(vx!.Events.Count, 43,
            "**and all 43 events read** -- and the hash holds "
            + vx.Events.Count + " of them");
        var evNamen = 0;
        var eigeneNamen = new System.Collections.Generic.List<string>();
        foreach (var ereignis in vx.Events)
        {
            if (ereignis.Name.StartsWith("EV", StringComparison.Ordinal))
            {
                evNamen++;
            }
            else if (ereignis.Name.Length > 0 && eigeneNamen.Count < 8)
            {
                eigeneNamen.Add(ereignis.Name);
            }
        }

        System.Console.WriteLine(
            "VX Map001: " + evNamen + " EV-Namen, eigene: "
            + string.Join(" | ", eigeneNamen));

        // **Und 31 ist nicht 43, und das ist nicht ein Fehler des
        // Lesers, sondern eine Eigenschaft des Spiels:**
        // **die uebrigen 12 heissen `ENEMY2!`** -- **und der Name ist
        // gemessen** -- **und der Leser gibt beide unveraendert
        // weiter.**
        AssertEq(evNamen, 31,
            "**and 31 of them carry the editor's EV name** -- and the"
                + " other twelve are called ENEMY2!, and that is what"
                + " the file says");
        AssertTrue(eigeneNamen.Count == 8,
            "**and the rest are named too** -- and the file calls them "
                + string.Join(", ", eigeneNamen));
        AssertEq(vx.Events.Count, 43,
            "**and all 43 are there** -- and no event was dropped"
                + " because its name was not the editor's");
    }

    /// <summary>
    /// And the command list comes out, and it is not empty.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the test that matters most</strong>, --
    /// <strong>because a command list is the only place a Ruby Maker
    /// game's instructions live</strong>, -- <strong>and everything
    /// before this was reading a game's furniture.</strong>
    /// </para>
    /// <para>
    /// <strong>And a trigger of 3 is an autostart</strong>, -- <strong>and
    /// it is what <c>Map001.rxdata</c> carries under
    /// <c>@trigger</c></strong>, -- <strong>and it is the reason this
    /// game's first page runs by itself.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieBefehlslisteEinerSeiteKommtHeraus()
    {
        AssertTrue(RgssMapReader.TryRead(Xp, out var xp, out var fehler),
            "**and the XP map reads** -- and it said: " + fehler);
        var seite = xp!.Events[0].Pages[0];
        AssertEq(seite.Trigger, 3,
            "**and the page is an autostart** -- and the file says 3,"
                + " and not the 1 a default page carries");
        AssertTrue(seite.Commands.Count > 0,
            "**and the page has commands** -- and it has "
            + seite.Commands.Count);
        System.Console.WriteLine(
            "XP Map001 GAME START: " + seite.Commands.Count
            + " Befehle, Codes: "
            + string.Join(" ", ErsteCodes(seite.Commands.Count, seite)));
    }

    /// <summary>
    /// And a word parameter stays a word.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And <c>121 Switch</c> carries <c>"True"</c></strong>, --
    /// <strong>and it is measured on the VX map</strong>, -- <strong>and
    /// a reader that parses it as a number gets zero</strong>, --
    /// <strong>and zero is off</strong>, -- <strong>and every switch in
    /// a game would be off.</strong>
    /// </para>
    /// </remarks>
    public void Test_EinWortparameterBleibtEinWort()
    {
        AssertTrue(RgssMapReader.TryRead(Vx, out var vx, out var fehler),
            "**and the VX map reads** -- and it said: " + fehler);
        var wahreWorte = 0;
        var schalterBefehle = 0;
        var beispiel = new System.Collections.Generic.List<string>();
        foreach (var ereignis in vx!.Events)
        {
            foreach (var seite in ereignis.Pages)
            {
                foreach (var befehl in seite.Commands)
                {
                    if (befehl.Code != 121 && befehl.Code != 122
                        && befehl.Code != 123)
                    {
                        continue;
                    }

                    schalterBefehle++;
                    if (beispiel.Count < 4)
                    {
                        beispiel.Add(befehl.Code + ": "
                            + string.Join(", ", befehl.Parameters));
                    }

                    for (var i = 0; i < befehl.Parameters.Count; i++)
                    {
                        if (befehl.Parameters[i].IstText)
                        {
                            wahreWorte++;
                        }
                    }
                }
            }
        }

        AssertTrue(schalterBefehle > 0,
            "**and the VX map has switch commands** -- and it has "
                + schalterBefehle);
        System.Console.WriteLine(
            "VX Schalterbefehle: " + schalterBefehle + ", Wortparameter: "
            + wahreWorte + ", Beispiele: "
            + string.Join(" | ", beispiel));

        // **Und gemessen sind es Befehle 121, 122 und 123**, und
        // **123 traegt den Buchstaben eines Selbstschalters** -- **und
        // **der ist ein Wort** -- **und kein Integer**, -- **und er ist
        // "A", "B", "C" oder "D".**
        //
        // **Und "A" ist genau der Fall, an dem ein Leser, der
        // Parameter als Zahlen liest, aus "A" die 0 macht** -- **und
        // 0 ist kein gueltiger Selbstschalter**, -- **und die Seite
        // laeuft, ohne ihren Schalter zu sehen.**
        AssertTrue(schalterBefehle > 0,
            "**and the VX map has switch commands** -- and it has "
                + schalterBefehle);
        AssertEq(wahreWorte, schalterBefehle,
            "**and every one of them carries a word** -- and "
                + schalterBefehle + " commands carry " + wahreWorte
                + " word parameters");
    }

    /// <summary>
    /// And a file that is not a map says so instead of reading as one.
    /// </summary>
    /// <remarks>
    /// <strong>And Actors.rxdata is a list, not a map</strong>, --
    /// <strong>and a reader that returns an empty map for it looks
    /// polite and is wrong</strong>, -- <strong>and a caller cannot tell
    /// the two apart.</strong>
    /// </remarks>
    public void Test_EineDateiDieKeineKarteIstSagtEs()
    {
        const string daten = "E:/RPGMakerGames/MicroQuest - Beneath"
            + " Brimestone 1.0/Data/Actors.rxdata";
        if (!File.Exists(daten))
        {
            AssertTrue(true, "**and the file is not here** -- and then"
                + " there is nothing to prove");
            return;
        }

        AssertFalse(RgssMapReader.TryRead(daten, out var karte, out var f),
            "**and it is refused** -- and a non-map is not an empty map");
        AssertTrue(karte == null, "**and there is no map**");
        AssertTrue(f.Contains("RPG::Map", StringComparison.Ordinal),
            "**and the reason names what it wanted** -- and it said: "
                + f);
    }

    /// <summary>
    /// And a file that is not there says so, and does not crash.
    /// </summary>
    /// <remarks>
    /// <strong>And an imported project is untrusted input</strong>, --
    /// <strong>and a reader that throws on a missing file turns a
    /// missing map into a crash</strong>.
    /// </remarks>
    public void Test_EineFehlendeDateiIstEinFehlerUndKeinAbsturz()
    {
        AssertFalse(RgssMapReader.TryRead(
            "E:/RPGMakerGames/kein-solches-spiel/Data/Map001.rxdata",
            out var karte, out var f),
            "**and the missing file is refused**");
        AssertTrue(karte == null, "**and there is no map**");
        AssertTrue(f.Length > 0, "**and it says why** -- and it said: " + f);
    }

    private static string ErsteCodes(
        int pAnzahl, RgssMapEventPage pSeite)
    {
        var teile = new System.Collections.Generic.List<string>();
        var n = Math.Min(pAnzahl, 12);
        for (var i = 0; i < n; i++)
        {
            teile.Add(pSeite.Commands[i].Code.ToString(
                System.Globalization.CultureInfo.InvariantCulture));
        }

        return string.Join(" ", teile);
    }
}
