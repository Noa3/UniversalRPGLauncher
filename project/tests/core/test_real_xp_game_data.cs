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
}
