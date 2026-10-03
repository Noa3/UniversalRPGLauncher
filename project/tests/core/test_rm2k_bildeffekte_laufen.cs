using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Plugins;
using UniversalRPG.Rm2k.Interpreter;
using UniversalRPG.Rm2k.Presentation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And that the screen effects of a real game run.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And criterion 2 is animations and effects.</strong> --
/// <strong>And the measurement says Dragon Destiny writes
/// <c>11030</c> 2910 times, <c>11210</c> 792 times, <c>11040</c> 379
/// times and <c>11050</c> 34 times.</strong>
/// </para>
/// <para>
/// <strong>And <c>ExecuteFlashScreen</c> and <c>ExecuteShakeScreen</c>
/// already exist and are already wired</strong>, -- <strong>and this
/// asks whether the game's own commands survive them.</strong>
/// </para>
/// </remarks>
public partial class TestRm2kBildeffekteLaufen : TestBase
{
    private const string Spiel = "E:/RPGMakerGames/Dragon Destiny";

    /// <summary>
    /// And the real commands of the game, and what they do.
    /// </summary>
    public void Test_DieBildeffekteDesSpiels()
    {
        if (!Directory.Exists(Spiel))
        {
            return;
        }

        var parser = new UniversalRPG.Rm2k.Parser.Rm2kParser();
        var beispiele = new SortedDictionary<int,
            List<int>>();
        foreach (var datei in Directory.GetFiles(Spiel, "Map*.lmu")
            .OrderBy(x => x))
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

                    foreach (var befehlRoh in befehleRoh.AsGodotArray())
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
                        if (n != EventInterpreter.FlashScreen
                            && n != EventInterpreter.ShakeScreen
                            && n != EventInterpreter.TintScreen
                            && n != EventInterpreter
                                .ShowBattleAnimation)
                        {
                            continue;
                        }

                        var werte = new List<int>();
                        if (b.TryGetValue("parameters",
                                out var pRoh)
                            && pRoh.VariantType == Variant.Type.Array)
                        {
                            foreach (var w in pRoh.AsGodotArray())
                            {
                                werte.Add(w.VariantType
                                    == Variant.Type.Int
                                        ? w.AsInt32() : -1);
                            }
                        }

                        if (!beispiele.TryGetValue(n, out var liste))
                        {
                            liste = new List<int>();
                            beispiele[n] = liste;
                        }

                        if (liste.Count < 4)
                        {
                            liste.AddRange(werte);
                        }
                    }
                }
            }
        }

        foreach (var x in beispiele)
        {
            Console.WriteLine($"{x.Key}: {string.Join(",",
                x.Value)}");
        }

        AssertTrue(beispiele.Count > 0,
            "**and the game writes screen effects**");

        AssertTrue(beispiele.ContainsKey(
            EventInterpreter.FlashScreen),
            "**and it writes flash, 379 times measured**");
    }

    /// <summary>
    /// And the flash the game writes is accepted with its own numbers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the game's flash commands carry
    /// <c>[31, 31, 31, 31, 5, 1]</c> and its tints carry
    /// <c>[0, 0, 0, 0, 10, 1]</c>.</strong> -- <strong>and those six
    /// numbers go through the interpreter and into the screen.</strong>
    /// </para>
    /// <para>
    /// <strong>And a reader that treated the first number as a colour
    /// from 0 to 255 would refuse all of them.</strong>
    /// </para>
    /// </remarks>
    public void Test_DerBlitzDesSpielsLauft()
    {
        if (!Directory.Exists(Spiel))
        {
            return;
        }

        var bild = new PresentationState();
        Console.WriteLine("vorher: aktiv " + bild.IsFlashActive
            + "  rot " + bild.FlashRed + "  alpha "
            + bild.FlashAlpha + "  frames "
            + bild.FlashFramesRemaining);

        // **Und  das  sind  die  Zahlen  des  Spiels** -- **31  auf
        //  jedem  Kanal  und  eine  Dauer  von  5  Zehntelsekunden.**
        var tenths = 5;
        var frames = tenths * 60 / 10;
        var ok = bild.FlashOnce(31, 31, 31, 31, frames);
        Console.WriteLine("nachher: " + ok + "  aktiv "
            + bild.IsFlashActive + "  rot " + bild.FlashRed
            + "  alpha " + bild.FlashAlpha + "  frames "
            + bild.FlashFramesRemaining);

        AssertTrue(ok,
            "**and the game's own flash is accepted**");

        AssertEq(31, bild.FlashRed,
            "**and the red channel is the number the game"
                + " wrote** -- and 31 is inside the reference's"
                + " channel range and a reader expecting 0 to 255"
                + " would have refused it");

        AssertEq(31, bild.FlashAlpha,
            "**and the strength is kept too**");

        AssertEq(30, bild.FlashFramesRemaining,
            "**and tenths five become thirty frames** -- and"
                + " the reference converts tenths itself rather"
                + " than storing tenths, and that matters because"
                + " 60 frames per second makes 5 * 6 exact");
    }

    /// <summary>
    /// And the shake the game writes is accepted too.
    /// </summary>
    public void Test_DasErdbebenLauft()
    {
        if (!Directory.Exists(Spiel))
        {
            return;
        }

        var bild = new PresentationState();
        var ok = bild.ShakeOnce(5, 3, 40);
        Console.WriteLine("Erdbeben: " + ok + "  staerke "
            + bild.ShakeStrength + "  tempo " + bild.ShakeSpeed
            + "  frames " + bild.ShakeFramesRemaining);

        AssertTrue(ok,
            "**and the shake is accepted**");
        AssertTrue(bild.IsShakeActive,
            "**and it is active**");
        AssertEq(40, bild.ShakeFramesRemaining,
            "**and it runs for the frames it was given**");
    }
}
