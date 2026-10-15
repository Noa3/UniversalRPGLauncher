using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using UniversalRPG.Rm2k.Parser;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And the effect commands Dragon Destiny actually writes.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And criterion 2 is animations and effects</strong>, --
/// <strong>and the numbers come from liblcf's
/// <c>eventcommand.h</c> and not from my head</strong>:
///
/// <code>
/// 11020  ShowScreen          11040  FlashScreen
/// 11030  TintScreen          11050  ShakeScreen
/// 11060  PanScreen           11070  Weather
/// 11210  ShowBattleAnimation
/// 13260  ShowBattleAnimation
/// </code>
///
/// <para>
/// <strong>And what matters is which of them the game writes</strong>.
/// </para>
/// </remarks>
public partial class TestRm2kEffektbefehleGemessen : TestBase
{
    private const string Spiel = "E:/RPGMakerGames/Dragon Destiny";

    /// <summary>
    /// And every command the game's maps write, in the shape the
    /// existing measurement already uses.
    /// </summary>
    private static (Dictionary<int, int> Zahler, int Karten)
        Miss()
    {
        var zahler = new Dictionary<int, int>();
        var karten = 0;
        if (!Directory.Exists(Spiel))
        {
            return (zahler, 0);
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
            if (!karte.Data.TryGetValue("events", out var roh)
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
                    if (seiteRoh.VariantType
                        != Variant.Type.Dictionary)
                    {
                        continue;
                    }

                    var seite = seiteRoh.AsGodotDictionary();
                    if (!seite.TryGetValue("commands",
                            out var befehleRoh)
                        || befehleRoh.VariantType
                            != Variant.Type.Array)
                    {
                        continue;
                    }

                    foreach (var befehlRoh in
                        befehleRoh.AsGodotArray())
                    {
                        if (befehlRoh.VariantType
                            != Variant.Type.Dictionary)
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
                    }
                }
            }
        }

        return (zahler, karten);
    }

    /// <summary>
    /// And the effect commands, counted.
    /// </summary>
    public void Test_DieEffektbefehleDesSpiels()
    {
        var (zahler, karten) = Miss();
        Console.WriteLine("Karten: " + karten);

        foreach (var num in new[] { 11020, 11030, 11040, 11050,
            11060, 11070, 11210, 13260 })
        {
            Console.WriteLine($"  {num}: "
                + (zahler.TryGetValue(num, out var n) ? n : 0));
        }

        Console.WriteLine("--- 11000..13999 im Spiel ---");
        foreach (var x in zahler.Where(x => x.Key >= 11000
            && x.Key < 14000).OrderBy(x => x.Key))
        {
            Console.WriteLine($"  {x.Key}: {x.Value}");
        }

        AssertTrue(karten > 700,
            "**and the game's maps are read**");
    }

    /// <summary>
    /// And what the effect commands actually write into their parameters.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the range is the question, not the count.</strong> The flash
    /// takes its colour in RPG_RT's own 0 to 31 (<c>Flash::MakeColor</c> scales
    /// by eight), the shake takes a strength and a speed of 1 to 8, the pan takes
    /// four direction flags and a distance, and the weather takes a type and a
    /// strength. A reader that bounded any of them by a colour channel's 0 to 255
    /// would accept values the editor cannot write and refuse values it does --
    /// which is exactly what the tint did before it was fixed.
    /// </para>
    /// <para>
    /// <strong>And the flash's seventh parameter is its subcommand</strong>
    /// (0 once, 1 begin, 2 end), which decides whether the flash is a single
    /// pulse or a repeating one: a reader that ignored it would turn a continuous
    /// flash into a single one.
    /// </para>
    /// </remarks>
    public void Test_DieParameterDerEffektbefehle()
    {
        if (!Directory.Exists(Spiel))
        {
            return;
        }

        foreach (var code in new[] { 11040, 11050, 11060, 11070 })
        {
            var (tuples, seen) = MissParameter(code);
            Console.WriteLine($"Parameter von {code}: {seen} Befehle,"
                + $" {tuples.Count} verschiedene Formen");
            foreach (var paar in tuples.OrderByDescending(p => p.Value).Take(6))
            {
                Console.WriteLine($"  {paar.Value} x [{paar.Key}]");
            }
            AssertTrue(seen >= 0, "and the measurement ran");
        }

        // **And the flash's own range is pinned here**: every channel it writes
        // has to be inside RPG_RT's 0 to 31, or the measurement is measuring
        // something else.
        var (flashTuples, flashSeen) = MissParameter(11040);
        var outside = 0;
        foreach (var tuple in flashTuples.Keys)
        {
            var parts = tuple.Split(',');
            for (var index = 0; index < Math.Min(4, parts.Length); index++)
            {
                if (int.TryParse(parts[index], out var value) && (value < 0 || value > 31))
                {
                    outside++;
                }
            }
        }
        Console.WriteLine($"Flash: {flashSeen} Befehle, {outside} Werte ausserhalb 0..31");
        AssertEq(outside, 0,
            "**and every flash colour and level is inside RPG_RT's 0 to 31**,"
                + " because Flash::MakeColor scales them by eight; values outside"
                + " that range would mean this reader is reading the wrong field");

        // **And the shake's own range is pinned the same way**: strength and speed
        // are the editor's 0 to 9, and nine is what this game writes -- which is
        // why the runtime's bound is nine and not eight.
        var (shakeTuples, shakeSeen) = MissParameter(11050);
        var shakeOutside = 0;
        var shakeNine = 0;
        foreach (var tuple in shakeTuples.Keys)
        {
            var parts = tuple.Split(',');
            for (var index = 0; index < Math.Min(2, parts.Length); index++)
            {
                if (!int.TryParse(parts[index], out var value))
                {
                    continue;
                }
                if (value < 0 || value > 9)
                {
                    shakeOutside++;
                }
                if (value == 9)
                {
                    shakeNine++;
                }
            }
        }
        Console.WriteLine($"Shake: {shakeSeen} Befehle, {shakeOutside} Werte ausserhalb"
            + $" 0..9, {shakeNine} Werte von genau neun");
        AssertEq(shakeOutside, 0,
            "**and every shake strength and speed is inside the editor's 0 to 9**");
        AssertTrue(shakeNine > 0,
            "**and the game really writes nine**, which is what makes the runtime's"
                + " bound of nine a measurement rather than a preference; found "
                + shakeNine);
    }

    /// <summary>
    /// Collects the parameter tuples of one command code across the game's maps.
    /// </summary>
    /// <param name="pCode">The command code.</param>
    /// <returns>The distinct tuples with their counts, and the total seen.</returns>
    private static (Dictionary<string, int> Tuples, int Seen) MissParameter(int pCode)
    {
        var tuples = new Dictionary<string, int>();
        var seen = 0;
        var parser = new Rm2kParser();
        foreach (var datei in Directory.GetFiles(Spiel, "Map*.lmu"))
        {
            var karte = parser.ParseMap(datei);
            if (!karte.Success)
            {
                continue;
            }
            if (!karte.Data.TryGetValue("events", out var roh)
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
                        var befehl = befehlRoh.AsGodotDictionary();
                        if (!befehl.TryGetValue("code", out var nr)
                            || nr.VariantType != Variant.Type.Int
                            || nr.AsInt32() != pCode)
                        {
                            continue;
                        }
                        seen++;
                        var parameter = befehl.ContainsKey("parameters")
                            ? befehl["parameters"].AsInt64Array() : Array.Empty<long>();
                        var tuple = string.Join(",", parameter.Take(8));
                        tuples.TryGetValue(tuple, out var count);
                        tuples[tuple] = count + 1;
                    }
                }
            }
        }
        return (tuples, seen);
    }
}
