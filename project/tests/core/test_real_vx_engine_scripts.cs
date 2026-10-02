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
/// The VX engine's own default script set, read out of a finished game and
/// given to this repository's Ruby parser.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is a different question from the one the VX Ace corpus
/// answered.</strong> Those ninety-three scripts were a mod directory --
/// real game scripts, but written by whoever wrote the mod.
/// <strong>This is the engine's own default set, unmodified, which is the
/// largest Ruby 1.8 program a VX game can contain.</strong>
/// </para>
/// <para>
/// <strong>And it matters because it is the common denominator.</strong> A
/// player with a VX game and no mod has exactly these files. <strong>So a
/// parser that reads them reads what almost every VX player will
/// actually run</strong> -- <strong>and one that does not will fail on the
/// first unmodified game in the wild.</strong>
/// </para>
/// </remarks>
public partial class TestRealVxEngineScripts : TestBase
{
    private const string Wurzel =
        "E:/RPGMakerGames/Random Dungeon -English Version-";

    private static bool Vorhanden()
    {
        if (!File.Exists(Wurzel + "/Data/Scripts.rvdata"))
        {
            GD.Print(
                "    (skipped: no VX Scripts.rvdata at " + Wurzel + " -- the "
                + "engine's own default script set stays unread by the "
                + "parser, and a mod directory is not a substitute)");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Every script of the set, decompressed, counted.
    /// </summary>
    public void Test_DerStandardsatzIstVollstaendigLesbar()
    {
        if (!Vorhanden())
        {
            return;
        }

        var skripte = LeseSkripte();
        AssertTrue(skripte.Count > 150,
            "**and the engine's default set is whole** -- " + skripte.Count
                + " entries, and VX ships about one hundred and eighty");

        var mitCode = skripte.Where(pE => pE.Value.Length > 0).ToList();
        var bytes = mitCode.Sum(pE => (long)pE.Value.Length);
        System.Console.WriteLine(
            "VX Standardsatz: " + skripte.Count + " Skripte, " + mitCode.Count
            + " mit Code, " + bytes + " Bytes Ruby");

        AssertTrue(mitCode.Count > 150,
            "**and nearly all of them carry code** -- " + mitCode.Count
                + " of " + skripte.Count);
        AssertTrue(bytes > 1_500_000,
            "**and that is the engine's own program** -- " + bytes
                + " bytes, and a VX default set is over a million and a half");
    }

    /// <summary>
    /// The engine's own scripts, through this repository's Ruby 1.8 parser.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the measurement, and it is the same shape as the
    /// VX Ace one that took nine rounds.</strong> The engine's default set is
    /// plain Ruby with no metaprogramming tricks, <strong>and it is the
    /// widest thing a Ruby 1.8 parser meets in this generation.</strong>
    /// </para>
    /// <para>
    /// <strong>And the assertion is on every file, not on a sample.</strong>
    /// A parser that reads 180 of 186 scripts is a parser with six holes, and
    /// a VX player hits one of them on the first game.
    /// </para>
    /// </remarks>
    public void Test_DerStandardsatzGehtDurchDenRubyParser()
    {
        if (!Vorhanden())
        {
            return;
        }

        var skripte = LeseSkripte()
            .Where(pE => pE.Value.Length > 0)
            .ToList();

        var gelesen = 0;
        var fehler = new List<string>();
        foreach (var (name, code) in skripte)
        {
            try
            {
                // **Und MarshalReader.AlsText, und nicht eigene
                // Kodierung** -- **und der Grund ist in dessen eigenem
                // Kommentar nachzulesen: "das ist der einzige Weg, auf dem
                // ein Skriptrumpf in diesem Repository zu Text wird".**
                var text = MarshalReader.AlsText(code);
                var quelle = "require 'rgss'\n" + text;
                var tokens = new RubyLexer(quelle).Tokenize();
                _ = new RubyParser(tokens);
                gelesen++;
            }
            catch (Exception pAusnahme)
            {
                fehler.Add(name + ": " + pAusnahme.Message);
            }
        }

        System.Console.WriteLine(
            "VX Standardsatz geparst: " + gelesen + "/" + skripte.Count);
        AssertTrue(fehler.Count == 0,
            "**and every one of the engine's own scripts parses** -- "
                + string.Join(" | ", fehler.Take(6)));
        AssertTrue(gelesen == skripte.Count,
            "**and all of them, not a sample** -- " + gelesen + " of "
                + skripte.Count);
    }

    /// <summary>
    /// The scripts, decompressed, paired with their names.
    /// </summary>
    private static List<(string Name, byte[] Value)> LeseSkripte()
    {
        var archiv = new MarshalReader(
            File.ReadAllBytes(Wurzel + "/Data/Scripts.rvdata")).Read();
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
}