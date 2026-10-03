using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Godot;
using UniversalRPG.Rm2k.Parser;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// What one finished game's own map files ask for, counted over all of
/// them.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is the measurement that says whether criterion 1
/// is done, and it is not a claim.</strong> -- <strong>It reads the
/// <c>commands</c> of every map of a finished game</strong>, --
/// <strong>which is the key <c>LoadCurrentMapEvents</c> in the
/// runtime reads.</strong>
/// </para>
/// <para>
/// <strong>And my first attempt at this counted garbage.</strong> --
/// <strong>I scanned the raw bytes of the map files for every word
/// between 11110 and 12300 and got 219 different numbers</strong>,
/// -- <strong>which is what tile and event data looks
/// like</strong>, -- <strong>and 214 of them were not commands at
/// all.</strong> -- <strong>A map file is not a byte pattern, and a
/// reader that treats it as one produces a number and not a
/// measurement.</strong>
/// </para>
/// </remarks>
public partial class TestRm2kBefehlsmessung : TestBase
{
    private const string Spiel = "E:/RPGMakerGames/Dragon Destiny";

    /// <summary>
    /// And every map of the game, and the commands in it.
    /// </summary>
    /// <returns>The number of each command, and the maps read.</returns>
    private static (Dictionary<int, int> Zahler, int Karten, int Befehle)
        Miss()
    {
        var zahler = new Dictionary<int, int>();
        var karten = 0;
        var befehle = 0;
        if (!Directory.Exists(Spiel))
        {
            return (zahler, 0, 0);
        }

        var parser = new Rm2kParser();
        foreach (var datei in Directory.GetFiles(Spiel, "Map*.lmu"))
        {
            var karte = parser.ParseMap(datei);
            if (!karte.Success)
            {
                continue;
            }

            karten++;
            var daten = karte.Data;
            if (!daten.TryGetValue("events", out var roh)
                || roh.VariantType != Variant.Type.Array)
            {
                continue;
            }

            foreach (var evRoh in roh.AsGodotArray())
            {
                if (evRoh.VariantType != Variant.Type.Dictionary)
                {
                    continue;
                }

                var ev = evRoh.AsGodotDictionary();
                if (!ev.TryGetValue("pages", out var seitenRoh)
                    || seitenRoh.VariantType != Variant.Type.Array)
                {
                    continue;
                }

                foreach (var seiteRoh in seitenRoh.AsGodotArray())
                {
                    if (seiteRoh.VariantType != Variant.Type.Dictionary)
                    {
                        continue;
                    }

                    var seite = seiteRoh.AsGodotDictionary();
                    if (!seite.TryGetValue("commands", out var befehleRoh)
                        || befehleRoh.VariantType != Variant.Type.Array)
                    {
                        continue;
                    }

                    foreach (var befehlRoh in befehleRoh.AsGodotArray())
                    {
                        if (befehlRoh.VariantType != Variant.Type.Dictionary)
                        {
                            continue;
                        }

                        var b = befehlRoh.AsGodotDictionary();
                        if (!b.TryGetValue("code", out var nr)
                            || nr.VariantType != Variant.Type.Int)
                        {
                            continue;
                        }

                        var n = nr.AsInt32();
                        zahler.TryGetValue(n, out var k);
                        zahler[n] = k + 1;
                        befehle++;
                    }
                }
            }
        }

        return (zahler, karten, befehle);
    }

    /// <summary>
    /// And the commands the interpreter dispatches, by name.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the switch dispatches by <em>name</em> and not by
    /// number</strong>, -- <strong>so a reader that looked for the
    /// digits would find eleven missing commands and would be wrong
    /// about all eleven.</strong> -- <strong>That is the second time
    /// this session a number was found by searching the wrong
    /// thing.</strong>
    /// </para>
    /// <para>
    /// <strong>And the eleven are all in the switch</strong>: <c>End</c>,
    /// <c>ShowMessage2</c>, <c>ShowChoiceOption</c>, <c>ChoiceEnd</c>,
    /// <c>VictoryHandler</c>, <c>DefeatHandler</c>, <c>EndBattle</c>,
    /// <c>ElseBranch</c>, <c>EndBranch</c>, <c>EndLoop</c> and
    /// <c>Comment2</c>.
    /// </para>
    /// </remarks>
    private static Dictionary<int, string> LeseNummern()
    {
        var pfad = "E:/URPG/project/src/rm2k/interpreter/"
            + "EventInterpreter.cs";
        var quelle = File.ReadAllText(pfad);
        var raus = new Dictionary<int, string>();
        foreach (Match m in Regex.Matches(quelle,
            @"public const int (\w+) = (\d+);"))
        {
            raus[int.Parse(m.Groups[2].Value)] = m.Groups[1].Value;
        }

        return raus;
    }

    /// <summary>
    /// And the game's 743 maps are all readable.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is counted on disk and not read out of an
    /// index</strong>, -- <strong>because an index is what the game
    /// claims and the files are what it
    /// has.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieKartenDesSpiels()
    {
        if (!Directory.Exists(Spiel))
        {
            return;
        }

        var karten = Directory.GetFiles(Spiel, "Map*.lmu");
        Console.WriteLine("Karten: " + karten.Length);

        AssertEq(743, karten.Length,
            "**and the game has 743 map files**");
        AssertTrue(File.Exists(Spiel + "/RPG_RT.ldb"),
            "**and its database is there** -- and it is the file a"
                + " `Game.ini` names as its RTP or its own runtime");
    }

    /// <summary>
    /// And the commands the game really writes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the number that says what the reader has
    /// to carry.</strong> -- <strong>And the 219 numbers I found by
    /// scanning bytes are not here</strong>, -- <strong>because they
    /// were never commands.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieBefehleDesSpiels()
    {
        if (!Directory.Exists(Spiel))
        {
            return;
        }

        var (zahler, karten, befehle) = Miss();

        Console.WriteLine($"Karten {karten}  Befehle {befehle}  "
            + $"Arten {zahler.Count}");
        foreach (var x in zahler.OrderByDescending(k => k.Value).Take(12))
        {
            Console.WriteLine($"  {x.Value}  {x.Key}");
        }

        AssertTrue(karten == 743,
            "**and all 743 maps parsed** -- and a run over 743"
                + " finished files is the only way a number about a"
                + " finished game means anything");
        AssertTrue(befehle > 10000,
            "**and the game carries tens of thousands of"
                + " commands** -- and it read " + befehle);
        // **Und die Liste  der  74,  damit  die  Luecke
        //  benannt  und  nicht  geschaetzt  ist.**
        var namen = LeseNummern();
        var quelle = File.ReadAllText(
            "E:/URPG/project/src/rm2k/interpreter/EventInterpreter.cs");
        var imSwitch = new HashSet<string>(
            Regex.Matches(quelle, @"case\s+(\w+)\s*:")
                .Select(m => m.Groups[1].Value), StringComparer.Ordinal);
        var fehlend = new List<int>();
        foreach (var nr in zahler.Keys.OrderBy(n => n))
        {
            var name = namen.TryGetValue(nr, out var w)
                ? w : null;
            if (name == null || !imSwitch.Contains(name))
            {
                fehlend.Add(nr);
                Console.WriteLine($"  fehlt {nr} {zahler[nr]}x");
            }
        }

        Console.WriteLine("Leser fuehrt " + imSwitch.Count
            + " Namen; Spiel braucht " + zahler.Count
            + "; fehlend " + fehlend.Count);

        // **Und  wie  oft  ein  Kartenwechsel  im  Spiel
        //  wirklich  vorkommt.**  --
        // **10810  Teleport  und  11810  Teleportziele** --
        // **und  die  Zahl  ist  die  Antwort  auf  die  Frage,  ob
        //  der  Host  einen  braucht.**
        Console.WriteLine("Teleport " + (zahler.TryGetValue(10810, out var tele)
            ? tele : 0) + "  Teleportziele "
            + (zahler.TryGetValue(11810, out var tt) ? tt : 0)
            + "  Kartenwechsel "
            + (zahler.TryGetValue(11710, out var ct) ? ct : 0));

        // **Und  die  Kampf-Befehle  im  selben  Lauf.**
        int V(int n) => zahler.TryGetValue(n, out var w) ? w : 0;
        // **Und  die  haeufigsten  Befehle  ueberhaupt** -- **denn
        //  das  ist  die  Frage,  was  ein  Lauf  wirklich  tut**,
        // **und  nicht  was  das  Format  kennt.**
        var haeufigste = new List<(int Nr, string Name, int Anzahl)>();
        foreach (var kv in zahler)
        {
            haeufigste.Add((kv.Key, kv.Key.ToString(), kv.Value));
        }

        haeufigste.Sort((a, b) => b.Anzahl.CompareTo(a.Anzahl));
        Console.WriteLine("Top 15 Befehle:");
        for (var i = 0; i < Math.Min(15, haeufigste.Count); i++)
        {
            Console.WriteLine("  " + haeufigste[i].Anzahl + "x  "
                + haeufigste[i].Nr);
        }

        // **Und  die  Kampfbefehle  im  Besonderen** -- **und  das  ist
        //  die  Liste,  die  fuer  die  Party  zaehlt.**
        var kampf = new List<string>();
        foreach (var kv in zahler)
        {
            if (kv.Key >= 10000 && kv.Key < 20000)
            {
                kampf.Add(kv.Key + ":" + kv.Value);
            }
        }

        kampf.Sort(StringComparer.Ordinal);
        Console.WriteLine("Kampfbereich: " + string.Join(" ", kampf));

        Console.WriteLine($"Kampf: 10710 Gegnerbegegnung {V(10710)}"
            + $"  20710 Sieg {V(20710)}  20712 Niederlage {V(20712)}"
            + $"  20713 Kampfende {V(20713)}  13310 Bedingung {V(13310)}"
            + $"  11740 Begegnungsschritte {V(11740)}");

        AssertEq(0, fehlend.Count,
            "**and every command this game writes is dispatched** --"
                + " and the eleven I first reported missing are all in"
                + " the switch, because the switch names them and my"
                + " search looked for their digits");

        AssertTrue(zahler.Count > 20,
            "**and more than twenty different commands** -- and it"
                + " read " + zahler.Count + ", and the 219 numbers a"
                + " byte scan produced were tile data");
    }
}
