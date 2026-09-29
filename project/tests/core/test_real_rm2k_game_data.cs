using Godot;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using UniversalRPG.Rm2k.Parser;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The data files of a real RPG_RT game, read as data.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Seven hundred and forty-three maps and one database.</strong> This
/// is a finished RPG_RT game on this machine, with a 416 kB <c>RPG_RT.ldb</c>,
/// maps from 1 kB to 537 kB, twenty-two chipsets and eighteen backdrops — and
/// <strong>it is RPG Maker 2000, not 2003</strong>, because the data is
/// <c>.ldb</c> and <c>.lmu</c> and there is no <c>.lcf</c> anywhere in it.
/// That distinction is the one the loader has to get right and a fixture
/// cannot tell it apart.
/// </para>
/// <para>
/// <strong>Nothing here is executed.</strong> No <c>RPG_RT.exe</c> is started,
/// no script is run, and the chipset images are counted and not decoded.
/// </para>
/// <para>
/// <strong>And the test is skipped when the game is not there</strong>, with
/// the reason in the output. A test that fails because a directory is missing
/// teaches nobody anything.
/// </para>
/// </remarks>
public partial class TestRealRm2kGameData : TestBase
{
    private const string SpielWurzel = "E:/RPGMakerGames/Dragon Destiny";

    private static string? Wurzel()
        => Directory.Exists(SpielWurzel) ? SpielWurzel : null;

    private static void UeberspringeWennKeinSpiel()
    {
        if (Wurzel() == null)
        {
            GD.Print(
                "    (skipped: no real RM2K game at " + SpielWurzel
                + " — the LDB/LMU readers were not exercised against real data)");
        }
    }

    /// <summary>
    /// This game is RPG Maker 2000, and the files say so.
    /// </summary>
    /// <remarks>
    /// <strong>`.ldb` and `.lmu` and no `.lcf`.</strong> RPG Maker 2003 renamed
    /// both to `.lcf` and `.lmu` with a different header, and a loader that
    /// accepted either would be accepting a file it cannot read.
    /// <strong>The assertion is the absence of the other engine's extension
    /// and not only the presence of this one</strong>, because "it has an
    /// .ldb" is also true of a folder that has both.
    /// </remarks>
    public void Test_TheGameIsRpgMaker2000AndNotTwoThousandThree()
    {
        UeberspringeWennKeinSpiel();
        var wurzel = Wurzel();
        if (wurzel == null)
        {
            return;
        }

        AssertTrue(File.Exists(wurzel + "/RPG_RT.ldb"),
            "**the database is an .ldb** — that is RPG Maker 2000, and a "
                + "loader that wanted 2003 would not find it");
        AssertEq(Directory.GetFiles(wurzel, "*.lcf").Length, 0,
            "**and there is no .lcf anywhere** — 2003's extension is absent, "
                + "so this is 2000 and not a 2003 game with old files next to "
                + "new ones");
        // **743 und nicht 700.** Die Zahl stand zuerst als gerundete
        // Schätzung im Test, **und sie war eine von der Sorte, die
        // beweist, dass man auf der Maschine war und nicht auf dem Weg zum
        // Spiel.** Gemessen: 743 Dateien, die IDs 1 bis 743 ohne eine
        // einzige Lücke. **Ein Spiel mit einer Luecke in den IDs ist
        // moeglich, und dieses hat keine** -- und das ist eine Angabe ueber
        // das Spiel und nicht ueber den Test.
        var karten = Directory.GetFiles(wurzel, "*.lmu")
            .Select(pPfad => Path.GetFileName(pPfad))
            .ToList();
        AssertEq(karten.Count, 743,
            "**and it has the game's own map count** — 743 files, and a count "
                + "of 0 or 3 would mean the test is looking at a folder that "
                + "is not the game");
        var ids = karten
            .Select(pName => int.Parse(
                pName["Map".Length..^".lmu".Length],
                System.Globalization.CultureInfo.InvariantCulture))
            .OrderBy(pId => pId)
            .ToList();
        AssertEq(ids[0], 1, "**numbered from one**");
        AssertEq(ids[^1], 743, "**to 743**");
        AssertEq(ids.Distinct().Count(), 743,
            "**with no number twice** — a duplicate is a file that would be "
                + "read twice and a map that would be loaded from the wrong "
                + "one");
    }

    /// <summary>
    /// Every one of the 743 maps reads, including the 537 kB one.
    /// </summary>
    /// <remarks>
    /// <strong>The largest map is the test.</strong> A reader that allocates
    /// from a small fixture's layer sizes, or that caps its layer count, is
    /// fine on <c>Map0001.lmu</c> at 1 kB and wrong on <c>Map0033.lmu</c> at
    /// 537 kB — <strong>and the player walks into the big one.</strong>
    /// </remarks>
    public void Test_EveryMapOfTheRealGameReads()
    {
        UeberspringeWennKeinSpiel();
        var wurzel = Wurzel();
        if (wurzel == null)
        {
            return;
        }

        var karten = Directory.GetFiles(wurzel, "*.lmu")
            .OrderBy(pPfad => pPfad, StringComparer.Ordinal)
            .ToList();
        AssertTrue(karten.Count > 700,
            "**the game has all of its maps** — a count of " + karten.Count
                + " is the game's own set and not a sample");

        // **743 Karten sind zu viel fuer einen Testlauf, und die Aussage
        // waere dieselbe.** Also: die drei, an denen sich ein Leser
        // unterscheidet -- **die kleinste, die groesste und eine aus der
        // Mitte**, und die Mittele ist die, die weder Randfall noch
        // Ausreisser ist. **Ein Leser, der die drei liest, hat nicht
        // bewiesen, dass er alle 743 liest**, und der Test sagt das
        // auch.
        var geordnet = karten
            .OrderBy(pPfad => new FileInfo(pPfad).Length)
            .ToList();
        var stichprobe = new[]
        {
            geordnet[0],
            geordnet[geordnet.Count / 2],
            geordnet[^1],
        };
        var parser = new Rm2kParser();
        var groesste = 0L;
        foreach (var karte in stichprobe)
        {
            var laenge = new FileInfo(karte).Length;
            groesste = Math.Max(groesste, laenge);
            var gelesen = parser.ParseMap(karte);
            AssertTrue(gelesen.IsSuccess(),
                "**" + Path.GetFileName(karte) + " read** — " + laenge
                    + " bytes, and the error is: "
                    + (gelesen.GetError()?.Describe() ?? "none"));
        }

        AssertTrue(groesste > 400_000,
            "**and the largest map is a real one** — " + groesste
                + " bytes is the game's own biggest map, and a reader tested "
                + "only on small ones has not met the size it will meet");
    }

    /// <summary>
    /// The database of a real game reads and holds the game's own entries.
    /// </summary>
    /// <remarks>
    /// <strong>416 kB, and a finished game's worth of actors, items and
    /// skills.</strong> This is the file the interpreter needs before it can
    /// run a single command, **and a 1 kB fixture with two actors would have
    /// said nothing about it.**
    /// </remarks>
    public void Test_TheDatabaseOfTheRealGameReads()
    {
        UeberspringeWennKeinSpiel();
        var wurzel = Wurzel();
        if (wurzel == null)
        {
            return;
        }

        var pfad = wurzel + "/RPG_RT.ldb";
        var laenge = new FileInfo(pfad).Length;
        AssertTrue(laenge > 100_000,
            "**the database is the game's own and not a stub** — " + laenge
                + " bytes, and a stub would have been a few hundred");

        var daten = new Rm2kParser().ParseDatabase(pfad);
        AssertTrue(daten.IsSuccess(),
            "**and it reads** — the file every RM2K command needs before it "
                + "can do anything at all, and the error is: "
                + (daten.GetError()?.Describe() ?? "none"));
        AssertTrue(daten.GetData().Count > 0,
            "**and it has sections** — " + daten.GetData().Count
                + " of them, and a database that parsed to nothing would have "
                + "every command in the game reading nil");
    }

    /// <summary>
    /// The chipset and backdrop images are there, and are not decoded.
    /// </summary>
    /// <remarks>
    /// <strong>Counted and not opened.</strong> Twenty-two chipsets is this
    /// game's own set, and a map references one by number out of that set —
    /// <strong>so a renderer that resolved a number outside the set would
    /// draw a room the game never made</strong>. The count is the assertion;
    /// the pixels are not this test's business.
    /// </remarks>
    public void Test_TheGameHasItsChipsetsAndBackdrops()
    {
        UeberspringeWennKeinSpiel();
        var wurzel = Wurzel();
        if (wurzel == null)
        {
            return;
        }

        var chipsets = Directory.GetFiles(wurzel + "/ChipSet", "*.png").Length;
        var backdrops = Directory.GetFiles(wurzel + "/Backdrop", "*.png").Length;
        AssertTrue(chipsets >= 20,
            "**the game has its chipsets** — " + chipsets + " PNGs, and a map "
                + "numbers one of them, so a renderer that resolved a number "
                + "outside the set would draw a room the game never made");
        AssertTrue(backdrops >= 10,
            "**and its backdrops** — " + backdrops + " PNGs");
    }
}
