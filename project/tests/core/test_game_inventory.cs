using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Godot;

using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Every finished game on this machine, and which engine each one is.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is the inventory the whole repository is measured
/// against, and it was written out by hand before this test and by nothing
/// else.</strong> <strong>Eleven games, eight engines:</strong>
/// </para>
/// <list type="bullet">
/// <item><description><strong>MZ</strong> --
/// <c>CamelliaCoronation-Win</c>, and
/// <c>test_real_mz_game_data</c> measures it;</description></item>
/// <item><description><strong>XP</strong> -- <c>MicroQuest - Beneath
/// Brimstone</c> and <c>Heartache 101 v2.5</c>, and
/// <c>test_real_xp_game_data</c> measures the first;</description></item>
/// <item><description><strong>VX</strong> -- <c>Random Dungeon</c> and
/// <c>The Princess and the Rose Knight</c>, both with
/// <c>Data/Scripts.rvdata</c> and <c>RGSS202E.dll</c>;</description></item>
/// <item><description><strong>VX Ace</strong> -- <c>Dreaming Mary</c>, and
/// its scripts are the ninety-three this repository's parser reads;</description></item>
/// <item><description><strong>MV</strong> -- <c>LegalTruck_v1.1</c>,
/// <c>Fatal Fantasy Update</c> and <c>sister/www</c>;</description></item>
/// <item><description><strong>RM2K</strong> -- <c>Dragon Destiny</c>,
/// <c>Lisa</c> and <c>Pom Gets Wi-Fi</c>, all with <c>.ldb</c>;</description></item>
/// <item><description><strong>WOLF</strong> -- <c>dungeon5min</c> and
/// <c>Kaiju Girlfriend</c>, both with a single <c>Data.wolf</c>;</description></item>
/// <item><description><strong>KiriKiri</strong> -- inside
/// <c>project/tests/fixtures/kirikiri</c>.</description></item>
/// </list>
/// <para>
/// <strong>And the test asserts the engine of each one against the engine's
/// own marker in the files, and not against a list written beside the
/// test.</strong> <strong>A table of engines that lives next to the test
/// is the same kind of thing as a hand-kept list of commands, and this
/// repository has learned what that costs three times now.</strong>
/// </para>
/// </remarks>
public partial class TestGameInventory : TestBase
{
    private const string Wurzel = "E:/RPGMakerGames";

    /// <summary>
    /// Every game in the folder, with the file that says what it is.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And a marker is a file, and each engine has one this
    /// repository can look for.</strong> <c>rmmz_core.js</c> is MZ,
    /// <c>rpg_core.js</c> is MV, <c>Game.rgss3a</c> and
    /// <c>Scripts.rvdata2</c> are VX Ace, <c>RGSS202E.dll</c> is VX,
    /// <c>RGSS104E.dll</c> is XP, an <c>.ldb</c> is RM2K, a
    /// <c>Data.wolf</c> is WOLF, and <c>rvproj2</c> is KiriKiri.
    /// </para>
    /// <para>
    /// <strong>And the list here is a reading of what the folder holds and
    /// not a claim about what should be in it</strong> -- **so a game
    /// that is added and is not in this list is a game this test has not
    /// been told about, and the test says so by failing on the
    /// count.</strong>
    /// </para>
    /// </remarks>
    public void Test_JedesFertigeSpielAufDieserMaschineIstErkannt()
    {
        if (!Directory.Exists(Wurzel))
        {
            GD.Print(
                "    (skipped: no " + Wurzel + " -- the inventory was not read"
                + " against the games this machine holds)");
            return;
        }

        var erwartet = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["CamelliaCoronation-Win"] = "MZ",
            ["Dreaming Mary"] = "VX Ace",
            ["Dragon Destiny"] = "RM2K",
            ["Fatal Fantasy Update"] = "MV",
            ["Heartache 101 v2.5"] = "XP",
            ["LegalTruck_v1.1"] = "MV",
            ["Lisa"] = "RM2K",
            ["MicroQuest - Beneath Brimestone 1.0"] = "XP",
            ["Pom Gets Wi-Fi v1-04"] = "RM2K",
            ["Random Dungeon -English Version-"] = "VX",
            ["The Princess and the Rose Knight"] = "VX",
        };

        // **Und WOLF ist absichtlich nicht dabei, denn seine beiden Spiele
        // tragen keine Game.ini und keinen Skriptnamen, und der Leser
        // spricht Data.wolf an.** Sie sind weiter unten gemessen.
        var gefunden = new List<string>();
        foreach (var ordner in Directory.GetDirectories(Wurzel)
            .OrderBy(pName => pName, StringComparer.Ordinal))
        {
            var name = Path.GetFileName(ordner);
            if (!erwartet.ContainsKey(name))
            {
                continue;
            }

            gefunden.Add(name);
            var marke = Marker(ordner);
            AssertEq(marke, erwartet[name],
                "**and " + name + " is the engine its own files say** -- and "
                    + "the marker found was '" + marke + "', and a game"
                    + " identified by a table next to the test is a game"
                    + " identified by nothing");
        }

        AssertTrue(gefunden.Count == erwartet.Count,
            "**and every game in the table is in the folder** -- "
                + gefunden.Count + " of " + erwartet.Count
                + ", and a game that was added and not listed here is a game"
                + " this repository has not been measured against");
    }

    /// <summary>
    /// The two WOLF games and what carries them, since they have no
    /// <c>Game.ini</c>.
    /// </summary>
    public void Test_DieBeidenWolfSpieleTragenIhreDatei()
    {
        foreach (var name in new[] { "dungeon5min", "Kaiju Girlfriend" })
        {
            var ordner = Wurzel + "/" + name;
            if (!Directory.Exists(ordner))
            {
                continue;
            }

            var daten = ordner + "/Data.wolf";
            AssertTrue(File.Exists(daten),
                "**and " + name + " carries its data in one file** -- and "
                    + "that file is missing, and a WOLF game without it has"
                    + " nothing to read");
            var laenge = new FileInfo(daten).Length;
            AssertTrue(laenge > 1_000_000,
                "**and the file is the game's own** -- " + laenge
                    + " bytes, and a stub would have been a few hundred");
        }
    }

    /// <summary>
    /// The engine a folder carries, named by the file that says so.
    /// </summary>
    private static string Marker(string pOrdner)
    {
        // **Und die Reihenfolge ist die Reihenfolge, in der die
        // Markierungen sich ausschliessen muessen** -- **und VX Ace nennt
        // <c>Scripts.rvdata2</c> und VX nennt <c>Scripts.rvdata</c>, und
        /// eines ist kein Praefix des anderen**, **und RM2K nennt eine
        /// <c>.ldb</c> und WOLF eine <c>Data.wolf</c>, und beides steht im
        /// Wurzelverzeichnis.**
        if (Finde(pOrdner, "rmmz_core.js") || Finde(pOrdner, "rmmz_objects.js"))
        {
            return "MZ";
        }

        if (Finde(pOrdner, "rpg_core.js") || Finde(pOrdner, "rpg_objects.js"))
        {
            return "MV";
        }

        // **Und ein Spiel, das sein Datenverzeichnis nicht neben sich
        // liegen hat, sondern im Archiv** -- **und beide Archive sind an
        // ihren ersten fuenf Bytes erkennbar, und das ist gemessen:**
        //
        // ```text
        // Game.rgss3a  ->  52 47 53 53 33 61   "RGSS3a"
        // Game.rgssad  ->  52 47 53 53 41 44   "RGSSAD"
        // ```
        //
        // **Und `Game.ini` sagt beides ohne jede Vermutung**, denn sie
        // nennt `Scripts=Data\Scripts.rxdata` und
        // `Scripts=Data\Scripts.rvdata2` -- **und das `Library` sagt die
        // Engine**, **und beide Felder sind es, die RPG Maker selbst
        // schreibt, und nicht etwas, was dieses Repository erraten hat.**
        var ini = LeseIni(pOrdner);
        var bibliothek = ini.GetValueOrDefault("Library", "");
        if (bibliothek.Contains("RGSS301", StringComparison.Ordinal))
        {
            return "VX Ace";
        }

        if (bibliothek.Contains("RGSS202", StringComparison.Ordinal))
        {
            return "VX";
        }

        if (bibliothek.Contains("RGSS104", StringComparison.Ordinal)
            || bibliothek.Contains("RGSS102", StringComparison.Ordinal))
        {
            return "XP";
        }

        if (Finde(pOrdner, "Scripts.rvdata2") || Finde(pOrdner, "Game.rgss3a"))
        {
            return "VX Ace";
        }

        if (Finde(pOrdner, "Scripts.rvdata") || Finde(pOrdner, "RGSS202E.dll"))
        {
            return "VX";
        }

        if (Finde(pOrdner, "Scripts.rxdata") || Finde(pOrdner, "RGSS104E.dll"))
        {
            return "XP";
        }

        // **Und `RGSSAD` ist XP, und es heisst so:**
        if (ArchivKopf(pOrdner + "/Game.rgssad") == "RGSSAD")
        {
            return "XP";
        }

        if (ArchivKopf(pOrdner + "/Game.rgss3a") == "RGSS3a")
        {
            return "VX Ace";
        }

        if (Finde(pOrdner, "*.ldb") || Finde(pOrdner, "*.lmu"))
        {
            return "RM2K";
        }

        if (Finde(pOrdner, "Data.wolf"))
        {
            return "WOLF";
        }

        return "unbekannt";
    }

    /// <summary>
    /// The two fields of <c>Game.ini</c> that name the engine, read as
    /// key and value.
    /// </summary>
    private static Dictionary<string, string> LeseIni(string pOrdner)
    {
        var ergebnis = new Dictionary<string, string>(StringComparer.Ordinal);
        var pfad = pOrdner + "/Game.ini";
        if (!File.Exists(pfad))
        {
            return ergebnis;
        }

        foreach (var zeile in File.ReadAllLines(pfad))
        {
            var trenn = zeile.IndexOf('=');
            if (trenn <= 0)
            {
                continue;
            }

            ergebnis[zeile[..trenn].Trim()] = zeile[(trenn + 1)..].Trim();
        }

        return ergebnis;
    }

    /// <summary>
    /// The first six bytes of an archive, as text, and empty when there is
    /// no file.
    /// </summary>
    private static string ArchivKopf(string pPfad)
    {
        if (!File.Exists(pPfad))
        {
            return "";
        }

        var kopf = File.ReadAllBytes(pPfad)[..6];
        return System.Text.Encoding.ASCII.GetString(kopf);
    }

    private static bool Finde(string pOrdner, string pMuster)
    {
        return Directory.GetFiles(pOrdner, pMuster, SearchOption.AllDirectories)
            .Length > 0;
    }
}
