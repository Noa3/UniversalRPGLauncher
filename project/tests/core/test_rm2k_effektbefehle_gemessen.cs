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
}
