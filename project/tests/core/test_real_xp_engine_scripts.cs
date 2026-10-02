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
/// The XP engine's own default script set, and the second finished game that
/// carries its data in an archive.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is the same question the VX corpus answered, asked of
/// the generation that shares its Marshal version with it.</strong> XP is
/// Ruby 1.8 like VX, <strong>and the two default sets are not the same
/// program.</strong>
/// </para>
/// <para>
/// <strong>And the second game here is the first XP archive this repository
/// has ever been pointed at:</strong> <c>Heartache 101</c> ships no
/// <c>Data</c> directory at all, and carries seventy-two megabytes in
/// <c>Game.rgssad</c> -- <strong>which is version one, and the version the
/// archive reader already handled before VX Ace taught it that version three
/// is a different format entirely.</strong>
/// </para>
/// </remarks>
public partial class TestRealXpEngineScripts : TestBase
{
    private const string Klartext =
        "E:/RPGMakerGames/MicroQuest - Beneath Brimestone 1.0";
    private const string Archiv = "E:/RPGMakerGames/Heartache 101 v2.5";

    // ---------------------------------------------------------------------
    // The clear-text game: the engine's default script set.
    // ---------------------------------------------------------------------

    private static bool KlartextVorhanden()
    {
        if (!File.Exists(Klartext + "/Data/Scripts.rxdata"))
        {
            GD.Print(
                "    (skipped: no XP Scripts.rxdata at " + Klartext + " -- the "
                + "XP engine's own default script set stays unread by the "
                + "parser)");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Every script of the XP engine's default set, counted.
    /// </summary>
    public void Test_DerXpStandardsatzIstVollstaendig()
    {
        if (!KlartextVorhanden())
        {
            return;
        }

        var skripte = LeseSkripte(Klartext + "/Data/Scripts.rxdata");
        AssertTrue(skripte.Count > 80,
            "**and the XP default set is whole** -- " + skripte.Count
                + " entries, and XP ships about a hundred");

        var mitCode = skripte.Where(pE => pE.Value.Length > 0).ToList();
        var bytes = mitCode.Sum(pE => (long)pE.Value.Length);
        System.Console.WriteLine(
            "XP Standardsatz: " + skripte.Count + " Skripte, " + mitCode.Count
            + " mit Code, " + bytes + " Bytes Ruby");

        AssertTrue(bytes > 400_000,
            "**and that is the engine's own program** -- " + bytes
                + " bytes, and the XP default set is over four hundred "
                + "kilobytes, which is about half of VX's");
    }

    /// <summary>
    /// The XP engine's own scripts, through this repository's Ruby 1.8
    /// parser.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the assertion that matters, and it is on every
    /// file.</strong> XP's default set and VX's default set are both Ruby
    /// 1.8, <strong>and one of them parsing does not mean the other
    /// does</strong> -- **XP predates VX by three years and its runtime has
    /// scripts VX does not and lacks ones VX has.**
    /// </para>
    /// </remarks>
    public void Test_DerXpStandardsatzGehtDurchDenRubyParser()
    {
        if (!KlartextVorhanden())
        {
            return;
        }

        var fehler = Parse(LeseSkripte(Klartext + "/Data/Scripts.rxdata"));
        AssertTrue(fehler.Count == 0,
            "**and every one of the XP engine's own scripts parses** -- "
                + string.Join(" | ", fehler.Take(6)));
    }

    // ---------------------------------------------------------------------
    // The archived game: 72 megabytes of version one.
    // ---------------------------------------------------------------------

    private static bool ArchivVorhanden()
    {
        if (!File.Exists(Archiv + "/Game.rgssad"))
        {
            GD.Print(
                "    (skipped: no Game.rgssad at " + Archiv + " -- the XP "
                + "archive path stays unmeasured against a finished game)");
            return false;
        }

        return true;
    }

    /// <summary>
    /// The archived game's data directory, out of the archive.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is a version one archive, which is the version the
    /// reader had before VX Ace taught it a second one.</strong> So this is
    /// not a re-run of a test -- <strong>it is the first time the version
    /// one path is pointed at a real seventy-two megabyte archive</strong>,
    /// <strong>and the writer's body encryption, which was fixed two commits
    /// ago, is what makes the bodies readable at all.</strong>
    /// </para>
    /// </remarks>
    public void Test_DasXpArchivListetUndSeineRumpfeEntschluesselt()
    {
        if (!ArchivVorhanden())
        {
            return;
        }

        var bytes = File.ReadAllBytes(Archiv + "/Game.rgssad");
        AssertEq((int)RgssArchiveReader.ReadVersion(bytes)!, 1,
            "**and it is version one, which is XP and VX**");

        var gelistet = new RgssArchiveReader().ListEntries(bytes, "Game.rgssad");
        AssertTrue(gelistet.Success,
            "**and the archive lists** -- " + gelistet.Error?.Message);
        var eintraege = gelistet.Value!;
        System.Console.WriteLine(
            "XP Archiv: " + eintraege.Count + " Eintraege, "
            + bytes.Length / 1e6 + " MB");

        AssertTrue(eintraege.Count > 50,
            "**and it holds a game's worth of files** -- " + eintraege.Count
                + " entries");

        var skript = eintraege.FirstOrDefault(
            pE => pE.Name == "Data/Scripts.rxdata");
        AssertTrue(skript != null,
            "**and it carries the script archive** -- and Game.ini names that "
                + "file as its scripts, and an archive without it cannot "
                + "start the game");
        AssertTrue(skript!.Size > 50_000,
            "**and the script archive is a game's worth** -- " + skript.Size
                + " bytes");

        // **Und der Rumpf ist verschluesselt, und genau das war der stille
        // Fehler, den der Rundlauftest vor zwei Commits fand.**
        var roh = new RgssArchiveReader().ReadEntry(bytes, skript, "Game.rgssad");
        AssertTrue(roh.Success, "**and the entry reads** -- " + roh.Error?.Message);
        AssertTrue(roh.Value!.Length == skript.Size,
            "**and it reads back at the length the list gave it** -- "
                + roh.Value!.Length + " against " + skript.Size);

        // **Und der entschluesselte Rumpf ist ein Marshal-Strom, und dessen
        // erster Byte ist der Major.** **Das ist die Pruefung, die zaehlt:**
        // ein unentschluesselter Rumpf faengt mit Zufall an.
        AssertEq((char)roh.Value![0], (char)4,
            "**and what comes out is a marshal stream and not ciphertext** -- "
                + "major version " + roh.Value![0]);
    }

    // ---------------------------------------------------------------------

    /// <summary>
    /// Every script of an XP or VX script archive, decompressed.
    /// </summary>
    private static List<(string Name, byte[] Value)> LeseSkripte(string pPfad)
    {
        var archiv = new MarshalReader(File.ReadAllBytes(pPfad)).Read();
        var ergebnis = new List<(string, byte[])>();
        foreach (var eintrag in archiv!.Items.Where(pE => pE.Items.Count == 3))
        {
            var name = eintrag.Items[1].Text ?? "";
            var blob = eintrag.Items[2].Bytes;
            if (blob == null || blob.Length < 3 || blob[0] != 0x78)
            {
                ergebnis.Add((name, Array.Empty<byte>()));
                continue;
            }

            using var ein = new MemoryStream(blob, 2, blob.Length - 2);
            using var aus = new MemoryStream();
            using (var d = new DeflateStream(ein, CompressionMode.Decompress))
            {
                d.CopyTo(aus);
            }

            ergebnis.Add((name, aus.ToArray()));
        }

        return ergebnis;
    }

    /// <summary>
    /// Runs every script through the parser and names those that fail.
    /// </summary>
    private static List<string> Parse(List<(string Name, byte[] Value)> pSkripte)
    {
        var fehler = new List<string>();
        var gelesen = 0;
        foreach (var (name, code) in pSkripte.Where(pE => pE.Value.Length > 0))
        {
            try
            {
                var quelle = "require 'rgss'\n" + MarshalReader.AlsText(code);
                _ = new RubyParser(new RubyLexer(quelle).Tokenize());
                gelesen++;
            }
            catch (Exception pAusnahme)
            {
                fehler.Add(name + ": " + pAusnahme.Message);
            }
        }

        System.Console.WriteLine(
            "XP Standardsatz geparst: " + gelesen + "/" + pSkripte.Count);
        return fehler;
    }
}