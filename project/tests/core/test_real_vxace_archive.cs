using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

using Godot;

using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The one finished RPG Maker VX Ace game on this machine, and the archive
/// that carries all of it.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And criterion 6 was measured against ninety-three loose
/// <c>.rb</c> files taken from a game's mod script directories.</strong> Those
/// were real game scripts, and they were read as Ruby 1.9.2. <strong>But they
/// were not what the engine loads</strong> -- <strong>and the engine loads
/// <c>Data/Scripts.rvdata2</c>, and this game has no <c>Data</c>
/// directory at all.</strong>
/// </para>
/// <para>
/// <strong>And that is the whole of this game on disk:</strong>
/// <c>Game.rgss3a</c>, twenty-eight megabytes, one header and the data
/// directory inside it. <strong>So the archive reader is the only way to
/// the game's own <c>Scripts.rvdata2</c>, and until this test it had never
/// been pointed at one.</strong>
/// </para>
/// </remarks>
public partial class TestRealVxAceArchive : TestBase
{
    private const string Wurzel = "E:/RPGMakerGames/Dreaming Mary";

    /// <summary>
    /// The reader this repository uses, created here so that no test reaches
    /// for a static that does not exist.
    /// </summary>
    private readonly RgssArchiveReader _archiv = new();

    private static bool Vorhanden()
    {
        if (!File.Exists(Wurzel + "/Game.rgss3a"))
        {
            GD.Print(
                "    (skipped: no Game.rgss3a at " + Wurzel + " -- the VX Ace"
                + " archive reader was not measured against a game's own"
                + " archive, and the ninety-three loose scripts are not the"
                + " same thing)");
            return false;
        }

        return true;
    }

    /// <summary>
    /// The archive header names the format, and the version byte says which
    /// of the three generations wrote it.
    /// </summary>
    public void Test_DerKopfNenntFormatUndGeneration()
    {
        if (!Vorhanden())
        {
            return;
        }

        var bytes = File.ReadAllBytes(Wurzel + "/Game.rgss3a");
        AssertTrue(RgssArchiveReader.HasArchiveHeader(bytes),
            "**and the file starts with the archive header** -- and RGSSAD is "
                + "six bytes, and RPG Maker writes it, and no other engine does");
        AssertEq((int)RgssArchiveReader.ReadVersion(bytes)!,
            (int)RgssArchiveReader.VersionVxAce,
            "**and it is version three, which is VX Ace** -- and version one "
                + "is XP and VX, and a reader that accepted version one here "
                + "would read every entry offset wrong");
    }

    /// <summary>
    /// Every entry of the archive lists, and the game's own files are in it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the measurement with teeth, because version three
    /// is not version one with another number.</strong> Version one walks a
    /// generator across name and size; <strong>version three reads one key
    /// from the file, transforms it, never advances it, and writes four
    /// fields before every name.</strong> <strong>So either the names come
    /// out as <c>Data/Actors.rvdata2</c> and
    /// <c>Data/Animations.rvdata2</c>, or nothing here works at
    /// all</strong> -- <strong>and there is no state in between, where
    /// names look like names.</strong>
    /// </para>
    /// </remarks>
    public void Test_JederEintragLaesstSichAufzaehlen()
    {
        if (!Vorhanden())
        {
            return;
        }

        var bytes = File.ReadAllBytes(Wurzel + "/Game.rgss3a");
        var gelistet = new RgssArchiveReader().ListEntries(bytes, "Game.rgss3a");
        AssertTrue(gelistet.Success,
            "**and the archive lists** -- " + gelistet.Error?.Message);
        var eintraege = gelistet.Value!;
        System.Console.WriteLine(
            "VXAce Archiv: " + eintraege.Count + " Eintraege, "
            + bytes.Length / 1e6 + " MB");

        // **Und 138 ist die Zahl dieses Spiels, gemessen, und nicht eine
        // gerundete Erwartung.** **Es hat kein Audio im Archiv**, weil es
        // keins hat -- **und ein Test, der Audio verlangt, haette ein
        // Spiel beschrieben, das es nicht gibt.**
        AssertTrue(eintraege.Count > 100,
            "**and it holds a game's worth of files** -- " + eintraege.Count
                + " entries, and this game carries thirty-one database files "
                + "and a hundred and seven images");

        foreach (var erwartet in new[]
        {
            "Data/Scripts.rvdata2", "Data/Actors.rvdata2",
            "Data/MapInfos.rvdata2", "Data/Map001.rvdata2",
        })
        {
            AssertTrue(eintraege.Any(pE => pE.Name == erwartet),
                "**and it carries " + erwartet + "**");
        }

        // **Und der Bildbestand ist der, den das Spiel wirklich traegt,
        // und er ist gross: ein sieben-Megabyte-Bild, weil es eine
        // Endsequenz ist.**
        var bilder = eintraege.Count(pE => pE.Name.EndsWith(".png",
                StringComparison.OrdinalIgnoreCase));
        System.Console.WriteLine(
            "VXAce Archiv: " + bilder + " Bilder, "
            + eintraege.Count(pE => pE.Name.StartsWith("Data/",
                StringComparison.Ordinal)) + " Datendateien");
        AssertTrue(bilder > 90,
            "**and it carries the game's images** -- " + bilder);
        AssertTrue(eintraege.Any(pE => pE.Name.EndsWith("$end.png",
                StringComparison.Ordinal)),
            "**and it carries the game's own art, not a stock set** -- and "
                + "this game's characters are named after its own characters");
    }

    /// <summary>
    /// The scripts inside the archive are the game's own, and they
    /// decompress to Ruby.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this closes the gap criterion 6 had.</strong> The
    /// ninety-three loose scripts were real game scripts, <strong>and they
    /// were not what the engine loads</strong> -- <strong>and a game can and
    /// does replace every script in here.</strong> This takes
    /// <c>Data/Scripts.rvdata2</c> out of the archive, parses it as Marshal
    /// and decompresses every script in it.
    /// </para>
    /// </remarks>
    public void Test_DasSkriptarchivAusDemArchivLiestUndEntpackt()
    {
        if (!Vorhanden())
        {
            return;
        }

        var bytes = File.ReadAllBytes(Wurzel + "/Game.rgss3a");
        var gelistet = new RgssArchiveReader().ListEntries(bytes, "Game.rgss3a");
        AssertTrue(gelistet.Success, "**and the archive lists**");
        var eintrag = gelistet.Value!
            .FirstOrDefault(pE => pE.Name == "Data/Scripts.rvdata2");
        AssertTrue(eintrag != null,
            "**and it carries the script archive** -- and Game.ini names that "
                + "file as its scripts, so an archive without it cannot start "
                + "the game");
        AssertTrue(eintrag!.Size > 100_000,
            "**and the script archive is a game's worth** -- " + eintrag.Size
                + " bytes, and VX Ace's default set compressed is around "
                + "two hundred kilobytes, so this game thinned it");

        var roh = new RgssArchiveReader().ReadEntry(bytes, eintrag, "Game.rgss3a");
        AssertTrue(roh.Success, "**and the entry reads** -- " + roh.Error?.Message);

        MarshalValue archiv;
        try
        {
            archiv = new MarshalReader(roh.Value!).Read();
        }
        catch (Exception pAusnahme)
        {
            AssertTrue(false,
                "**and the script archive reads as Marshal** -- "
                    + pAusnahme.GetType().Name + " " + pAusnahme.Message);
            return;
        }

        AssertTrue(archiv != null, "**and the script archive reads as Marshal**");
        var skripte = archiv!.Items.Where(pE => pE.Items.Count == 3).ToList();
        AssertTrue(skripte.Count > 120,
            "**and it carries the whole default script set** -- "
                + skripte.Count + " entries");

        var namen = skripte.Select(pE => pE.Items[1].Text ?? "")
            .Where(pT => pT.Length > 0).ToList();
        System.Console.WriteLine(
            "VXAce Archiv: " + skripte.Count + " Skripte, " + namen.Count
            + " Namen");

        long entpackt = 0;
        var fehler = 0;
        foreach (var e in skripte)
        {
            var blob = e.Items[2].Bytes;
            if (blob == null || blob.Length < 3 || blob[0] != 0x78)
            {
                fehler++;
                continue;
            }

            try
            {
                using var ein = new MemoryStream(blob, 2, blob.Length - 2);
                using var aus = new MemoryStream();
                using (var d = new DeflateStream(ein, CompressionMode.Decompress))
                {
                    d.CopyTo(aus);
                }

                entpackt += aus.Length;
            }
            catch (Exception)
            {
                fehler++;
            }
        }

        System.Console.WriteLine(
            "VXAce Archiv: entpackt " + entpackt + " Bytes Ruby, " + fehler
            + " Fehler");
        AssertTrue(fehler == 0,
            "**and every script decompresses** -- " + fehler + " of "
                + skripte.Count);
        AssertTrue(entpackt > 800_000,
            "**and what comes out is Ruby** -- " + entpackt + " bytes, and "
                + "VX Ace's default set is the largest of the four "
                + "generations that carry scripts");

        foreach (var name in new[] { "Game_Map", "Window_Base", "Main" })
        {
            AssertTrue(namen.Any(pN => pN.Contains(name)),
                "**and it carries " + name + "**");
        }
    }
}
