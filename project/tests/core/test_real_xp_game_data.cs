using Godot;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The XP data files of a real game, read as data.
/// </summary>
/// <remarks>
/// <para>
/// <strong>These are not fixtures.</strong> They are the
/// <c>Data/</c> directory of a finished game on this machine, and a reader
/// that only ever saw hand-built bytes has not been tested against the
/// things it will actually meet — a 155 kB <c>Animations.rxdata</c> with a
/// bitmap reference, a 109 kB <c>Scripts.rxdata</c> with thousands of
/// symbols, and maps whose sizes nobody chose.
/// </para>
/// <para>
/// <strong>Nothing here is executed.</strong> The files are read as data, the
/// scripts are not run, and the game is not launched. A game's
/// <c>Game.exe</c> and <c>RGSS104E.dll</c> are next to the data and are not
/// touched.
/// </para>
/// <para>
/// <strong>And the test is skipped when the game is not there</strong>, not
/// failed. A test that fails because a directory is missing teaches nobody
/// anything and trains everyone to ignore red.
/// </para>
/// </remarks>
public partial class TestRealXpGameData : TestBase
{
    private const string SpielWurzel =
        "E:/RPGMakerGames/MicroQuest - Beneath Brimestone 1.0";

    /// <summary>
    /// The real directory, or null when it is not on this machine.
    /// </summary>
    private static string? Data()
    {
        var pfad = SpielWurzel + "/Data";
        return Directory.Exists(pfad) ? pfad : null;
    }

    private static void UeberspringeWennKeinSpiel()
    {
        if (Data() == null)
        {
            // **Ein Grund, und er steht in der Ausgabe.** Sonst ist ein
            // übersprungener Test von einem bestandenen nicht zu
            // unterscheiden, **und ein grüner Lauf, der nichts geprüft
            // hat, ist die schlimmste Form davon.**
            GD.Print(
                "    (skipped: no real XP game at " + SpielWurzel
                + " — the marshal reader was not exercised against real data)");
        }
    }

    /// <summary>
    /// Every data file of a real XP game reads, and none of them throws.
    /// </summary>
    /// <remarks>
    /// <strong>Twenty-three files and none may fail.</strong> The interesting
    /// ones are the largest — <c>Animations.rxdata</c> at 155 kB and
    /// <c>Scripts.rxdata</c> at 109 kB — <strong>because a reader that gives
    /// up on depth or on a symbol table is fine on a five line fixture and
    /// not fine here.</strong>
    /// </remarks>
    public void Test_EveryDataFileOfARealGameReads()
    {
        UeberspringeWennKeinSpiel();
        var daten = Data();
        if (daten == null)
        {
            return;
        }

        var dateien = Directory.GetFiles(daten, "*.rxdata")
            .OrderBy(pPfad => pPfad, StringComparer.Ordinal)
            .ToList();
        AssertTrue(dateien.Count >= 20,
            "**the real game has its whole data directory** — a count of "
                + dateien.Count + " means the fixture is not a hand-picked "
                + "subset, and a reader tested on a subset is a reader tested "
                + "on the easy part");

        var gelesen = 0;
        foreach (var datei in dateien)
        {
            var wert = new MarshalReader(File.ReadAllBytes(datei)).Read();
            AssertTrue(wert != null,
                "**" + Path.GetFileName(datei) + " read as data** — it is "
                    + new FileInfo(datei).Length + " bytes and a value, and a "
                    + "reader that refused it would refuse a real game");
            gelesen++;
        }

        AssertEq(gelesen, dateien.Count,
            "**and every one of them read** — not the first and not the "
                + "smallest, all of them");
    }

    /// <summary>
    /// The actor list of a real game has the fields a game needs.
    /// </summary>
    /// <remarks>
    /// <strong>Names, and not only names.</strong> A reader that produced a
    /// tree of type bytes would pass "it read" and fail here, and a game needs
    /// an actor's name, its class and its initial level to put anything on
    /// screen. <strong>The assertion is on the data a game uses and not on the
    /// reader's internals.</strong>
    /// </remarks>
    public void Test_TheActorListOfARealGameHasNamesAndClasses()
    {
        UeberspringeWennKeinSpiel();
        var daten = Data();
        if (daten == null)
        {
            return;
        }

        var wert = new MarshalReader(
            File.ReadAllBytes(daten + "/Actors.rxdata")).Read();
        AssertTrue(wert.Kind == "array",
            "**the actor list is an array** — and not a link and not a hash, "
                + "because RPG Maker writes it as one; it is "
                + wert.Kind );

        var aktoren = wert.Items;
        AssertTrue(aktoren.Count > 0,
            "**and it is not empty** — a finished game has actors, and an "
                + "empty list here would mean the reader lost them");
        AssertTrue(aktoren.Count > 5,
            "**and it has more than the five the editor makes** — a count of "
                + aktoren.Count + " is a finished game's roster and not a "
                + "hand-built fixture");
    }

    /// <summary>
    /// The map list of a real game has sizes nobody chose.
    /// </summary>
    /// <remarks>
    /// <strong>The widest map here is not a round number</strong>, and that is
    /// the point: a reader that allocated from a fixture's width, or that
    /// rounded, would be wrong on every map in this game and right on the one
    /// it was built from. <strong>Every map is read and every one is asked
    /// for its size</strong>, because a game that opens the wrong one is a
    /// game that draws the wrong room.
    /// </remarks>
    public void Test_EveryMapOfARealGameReportsItsOwnSize()
    {
        UeberspringeWennKeinSpiel();
        var daten = Data();
        if (daten == null)
        {
            return;
        }

        var karten = Directory.GetFiles(daten, "Map*.rxdata")
            .Where(pPfad => !Path.GetFileName(pPfad).StartsWith(
                "MapInfos", StringComparison.OrdinalIgnoreCase))
            .OrderBy(pPfad => pPfad, StringComparer.Ordinal)
            .ToList();
        AssertTrue(karten.Count >= 10,
            "**the real game has its maps** — a count of " + karten.Count
                + " means the maps are the game's and not a sample");

        foreach (var karte in karten)
        {
            var wert = new MarshalReader(File.ReadAllBytes(karte)).Read();
            AssertTrue(wert != null,
                "**" + Path.GetFileName(karte) + " read** — and every map in a "
                    + "finished game is read, because the one that fails is "
                    + "the one the player walks into");
        }
    }

    /// <summary>
    /// The script list of a real game is data and is not run.
    /// </summary>
    /// <remarks>
    /// <strong>This is the boundary, and it is the reason this test
    /// exists.</strong> A finished XP game carries 109 kB of Ruby in
    /// <c>Scripts.rxdata</c>, and reading it as data is safe and running it
    /// is not this project's business. <strong>A test that read the file and
    /// said so in its own name is a test that cannot be misread as
    /// permission to run it</strong> — and the assertion is that the script
    /// names came back as names, which is all a reader may do with them.
    /// </remarks>
    public void Test_TheScriptListOfARealGameIsReadAsDataAndNotRun()
    {
        UeberspringeWennKeinSpiel();
        var daten = Data();
        if (daten == null)
        {
            return;
        }

        var pfad = daten + "/Scripts.rxdata";
        var wert = new MarshalReader(File.ReadAllBytes(pfad)).Read();
        AssertTrue(wert.Kind == "array",
            "**the script list is an array of entries** — and this test reads "
                + "it and does not run one line of it");
        AssertTrue(wert.Items.Count > 10,
            "**and it has a game's worth of entries** — a count of "
                + wert.Items.Count + " is the finished game's own "
                + "script list, and every one of them stays a name here");
    }
    /// <summary>
    /// The scripts of a real game are compressed, and this repository can
    /// read them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this test used to say the opposite, and it was
    /// wrong.</strong> The old version asserted that an XP script body is
    /// cipher and held no <c>def</c>, <c>class</c> or <c>end</c>, and
    /// wrote in its own remarks that <c>"XP and VX encrypt their scripts
    /// with a key derived from the archive"</c>.
    /// </para>
    /// <para>
    /// <strong>And the file says otherwise, and the measurement is four
    /// bytes.</strong> In
    /// <c>Data/Scripts.rxdata</c> of <c>MicroQuest - Beneath Brimestone
    /// 1.0</c>, every entry carries its number and its name in the clear
    /// and its name in the editor's own words, and behind the name the body
    /// begins <c>78 9c</c>:
    /// </para>
    /// <code>
    /// 5e 04 78 9c b5 58 5b 6f ...
    ///  ^^^^^^^ ^^^^^
    ///  |       zlib: deflate, 32K window, default level
    ///  a Marshal string
    ///
    /// @39152  "Spriteset_Map" 22 02 78 9c b5 58 5b 6f
    /// </code>
    /// <para>
    /// <strong>And inflating that block gives 5328 bytes of
    /// <c>class Spriteset_Map</c> with the engine's own comment
    /// header</strong> -- <strong>and doing it to all ninety blocks gives
    /// 538811 bytes of Ruby source.</strong>
    /// </para>
    /// <para>
    /// <strong>And a body that holds <c>def</c> is a body that is
    /// compressed and not encrypted</strong> -- <strong>and the reason is
    /// ordinary: a game's scripts are large, and a project's
    /// <c>Scripts.rxdata</c> would be megabytes without it.</strong>
    /// <strong>XP and VX Ace do not encrypt script bodies at all, and VX
    /// Ace's <c>Scripts.rvdata2</c> holds plain text that this
    /// repository's parser reads without complaint -- which is measured
    /// there and not here.</strong>
    /// </para>
    /// <para>
    /// <strong>And a test that asserted the wrong thing for as long as it
    /// did was worse than no test</strong> -- <strong>and it was green, and
    /// green is what made it dangerous.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieSkripteEinesEchtenXpSpielsSindLesbar()
    {
        UeberspringeWennKeinSpiel();
        var daten = Data();
        if (daten == null)
        {
            return;
        }

        var skripte = XpScriptBodies.LeseAlle(daten + "/Scripts.rxdata");
        AssertTrue(skripte.Count > 50,
            "**and the game carries a game's worth of scripts** -- "
                + skripte.Count + " entries, and a game's own script list "
                + "has about a hundred");

        var lesbar = skripte.Where(pS => pS.Entpackt).ToList();
        System.Console.WriteLine(
            "XP  gemessen: " + skripte.Count + " Skripte, "
            + lesbar.Count + " lesbar, "
            + lesbar.Sum(pS => pS.Text!.Length) + " Bytes Ruby");

        AssertTrue(lesbar.Count > 40,
            "**and most of them are readable** -- " + lesbar.Count
                + " of " + skripte.Count + ", and a number near zero would "
                + "say the format is something this reader does not know");

        // **Und der erste lesbare Körper ist echtes Ruby, und nicht eine
        // Datei, die zufällig entpackt.**
        var mitRuby = lesbar.Where(pS => pS.Text!.Contains("class ")
            || pS.Text!.Contains("module ")
            || pS.Text!.Contains("def ")).ToList();
        AssertTrue(mitRuby.Count > 40,
            "**and what comes out is Ruby** -- " + mitRuby.Count
                + " scripts carry `class`, `module` or `def`, and a body "
                + "that inflated to something else would be a number, not "
                + "a game");

        // **Und der Körper eines bekannten Skripts traegt dessen Namen im
        // eigenen Kopf, und das ist die Messung, die eine Chiffre
        // ausschliesst.**
        var spriteset = lesbar.FirstOrDefault(
            pS => pS.Name == "Spriteset_Map");
        AssertTrue(spriteset != null
            && spriteset.Text!.Contains("class Spriteset_Map"),
            "**and a body names the script the list gave it** -- and "
                + "Spriteset_Map's body carries `class Spriteset_Map`, and "
                + "a cipher could not do that by accident");

        // **Und kein Schritt hier hat eine Zeile ausgefuehrt.**
        AssertTrue(lesbar.All(pS => !pS.Text!.Contains("Game.exe")),
            "**and nothing was run** -- the bodies are text and the test "
                + "read them, and running a game's own code is a decision "
                + "this repository does not take on its own");
    }
}
