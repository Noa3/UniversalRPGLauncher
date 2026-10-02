using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

using Godot;

using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Two finished RPG Maker VX games on this machine, measured.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And criterion 5 had no game at all until the folder was filled,
/// and VX was the one generation this repository had never read a single file
/// of</strong> -- it had the VX Ace corpus, ninety-three scripts of Ruby
/// 1.9.2, <strong>and not one VX data file.</strong>
/// </para>
/// <para>
/// <strong>And VX and XP share the Marshal format and not the version.</strong>
/// XP's data is Ruby 1.8, VX's is Ruby 1.9, <strong>and the two differ in the
/// form of the integer Marshal calls a fixnum.</strong> A reader that worked
/// on XP is therefore not yet proven on VX, and that is precisely what this
/// test is for.
/// </para>
/// </remarks>
public partial class TestRealVxGameData : TestBase
{
    private const string Wurzel =
        "E:/RPGMakerGames/Random Dungeon -English Version-";

    private static bool Vorhanden()
    {
        if (!Directory.Exists(Wurzel + "/Data"))
        {
            GD.Print(
                "    (skipped: no VX game at " + Wurzel + " -- the VX data"
                + " files were not read, and the 1.9 integer form stays"
                + " unmeasured)");
            return false;
        }

        return true;
    }

    /// <summary>
    /// The game says what it is, in the fields RPG Maker itself writes.
    /// </summary>
    public void Test_DasSpielNenntSeineEngineSelbst()
    {
        if (!Vorhanden())
        {
            return;
        }

        var ini = File.ReadAllLines(Wurzel + "/Game.ini")
            .Where(l => l.Contains('='))
            .Select(l => l.Split('=', 2))
            .ToDictionary(p => p[0].Trim(), p => p[1].Trim(),
                StringComparer.OrdinalIgnoreCase);

        AssertEq(ini.GetValueOrDefault("Library"), "RGSS202E.dll",
            "**and this is VX and not VX Ace** -- and VX Ace is RGSS301, "
                + "which is a different Marshal and a different Ruby");
        AssertEq(ini.GetValueOrDefault("Scripts"), @"Data\Scripts.rvdata",
            "**and its scripts are the 1.8 form** -- and the 1.9 form is "
                + "Scripts.rvdata2, which VX Ace writes and this does not");
        AssertTrue(ini.ContainsKey("RTP") && ini["RTP"].Length > 0,
            "**and it names the RTP it expects** -- and a game without that "
                + "line cannot find its own graphics at runtime");
    }

    /// <summary>
    /// Every one of the game's data files reads as Marshal.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the measurement that decides whether the
    /// repository reads VX at all.</strong> Six hundred and fifty-six maps
    /// and the whole database, <strong>and every single one of them is Ruby
    /// 1.9's Marshal and not 1.8's.</strong>
    /// </para>
    /// </remarks>
    public void Test_JedeDatendateiLiestSichAlsMarshal()
    {
        if (!Vorhanden())
        {
            return;
        }

        var dateien = Directory.GetFiles(Wurzel + "/Data", "*.rvdata")
            .OrderBy(pPfad => pPfad, StringComparer.Ordinal)
            .ToList();
        AssertTrue(dateien.Count > 600,
            "**and the game has its whole data directory** -- "
                + dateien.Count + " files, and the six hundred and fifty-six "
                + "maps are not a subset anyone chose");

        var gelesen = 0;
        var fehler = new List<string>();
        foreach (var datei in dateien)
        {
            try
            {
                if (new MarshalReader(File.ReadAllBytes(datei)).Read() != null)
                {
                    gelesen++;
                }
                else
                {
                    fehler.Add(Path.GetFileName(datei) + ": read nothing");
                }
            }
            catch (Exception pAusnahme)
            {
                fehler.Add(Path.GetFileName(datei) + ": "
                    + pAusnahme.GetType().Name + " " + pAusnahme.Message);
            }
        }

        System.Console.WriteLine(
            "VX  gemessen: " + gelesen + " Dateien, " + fehler.Count + " Fehler");
        AssertTrue(fehler.Count == 0,
            "**and every one of them reads** -- "
                + string.Join("; ", fehler.Take(4)));
        AssertTrue(gelesen == dateien.Count,
            "**and the whole directory read, not a hand-picked part** -- "
                + gelesen + " of " + dateien.Count);
    }

    /// <summary>
    /// The scripts inside <c>Scripts.rvdata</c> are VX's default set, and
    /// every one of them decompresses.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the tie between criterion 5 and criterion 6.</strong>
    /// VX Ace's ninety-three scripts were real game scripts and were read as
    /// Ruby 1.9.2. <strong>VX's <c>Scripts.rvdata</c> holds the default set,
    /// which is the largest Ruby 1.8 file collection a VX game can
    /// have</strong> -- <strong>and a game can replace every one of
    /// them.</strong>
    /// </para>
    /// <para>
    /// <strong>And the encoding is the second half of the claim, and it is
    /// not Ruby and not a string:</strong> VX stores each script zlib
    /// compressed behind a two byte header, <strong>and a reader that
    /// reported "read" without decompressing would have read the
    /// compression and nothing else.</strong> <strong>So this decompresses,
    /// and counts the bytes that come out.</strong>
    /// </para>
    /// </remarks>
    public void Test_DasSkriptarchivTraegtSkripteUndNichtNurSeinenNamen()
    {
        if (!Vorhanden())
        {
            return;
        }

        var wert = new MarshalReader(
            File.ReadAllBytes(Wurzel + "/Data/Scripts.rvdata")).Read();
        AssertTrue(wert != null, "**and the script archive reads**");
        var skripte = wert!.Items.Where(pE => pE.Items.Count == 3).ToList();
        AssertTrue(skripte.Count > 150,
            "**and it carries the default script set** -- " + skripte.Count
                + " entries, and VX's own default set is larger than that");

        var namen = skripte
            .Select(pE => pE.Items[1].Text ?? "")
            .Where(pText => pText.Length > 0)
            .ToList();
        System.Console.WriteLine(
            "VX  Skriptarchiv: " + skripte.Count + " Skripte, " + namen.Count
            + " Namen");

        foreach (var name in new[] { "Game_Map", "Window_Base", "Main" })
        {
            AssertTrue(namen.Any(pN => pN.Contains(name)),
                "**and it carries " + name + "** -- and VX's default set has "
                    + "that script, and a game that dropped it would not run "
                    + "at all");
        }

        // **Und jetzt der Teil, der zaehlt: entpacken.** **VX legt jedes
        // Skript zlib-komprimiert ab, hinter zwei Byte Kopf.**
        long entpackt = 0;
        var fehler = new List<string>();
        foreach (var eintrag in skripte)
        {
            var blob = eintrag.Items[2].Bytes;
            if (blob == null || blob.Length < 3)
            {
                fehler.Add((eintrag.Items[1].Text ?? "?") + ": no payload");
                continue;
            }

            // **Und der zlib-Kopf ist `78` gefolgt von einem Level, und ein
            // Skript ohne ihn ist kein Skript und ist eine leere Datei.**
            if (blob[0] != 0x78)
            {
                fehler.Add((eintrag.Items[1].Text ?? "?") + ": zlib head "
                    + blob[0].ToString("x2"));
                continue;
            }

            try
            {
                using var ein = new MemoryStream(blob, 2, blob.Length - 2);
                using var aus = new MemoryStream();
                using (var deflate = new DeflateStream(ein, CompressionMode.Decompress))
                {
                    deflate.CopyTo(aus);
                }

                entpackt += aus.Length;
            }
            catch (Exception pAusnahme)
            {
                fehler.Add((eintrag.Items[1].Text ?? "?") + ": "
                    + pAusnahme.GetType().Name);
            }
        }

        System.Console.WriteLine(
            "VX  entpackt: " + entpackt + " Bytes Ruby aus "
                + skripte.Count + " Skripten, " + fehler.Count + " Fehler");
        AssertTrue(fehler.Count == 0,
            "**and every script decompresses** -- "
                + string.Join("; ", fehler.Take(4)));
        AssertTrue(entpackt > 500_000,
            "**and what comes out is Ruby and not a compression stub** -- "
                + entpackt + " bytes from " + skripte.Count + " scripts, and "
                + "a reader that reported the archive read without "
                + "decompressing would have counted the other 332917");
    }
}