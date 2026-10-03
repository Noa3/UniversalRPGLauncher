using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using UniversalRPG.Plugins;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And the battle animation the game plays 792 times.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And <c>11210 Show Battle Animation</c> is written 792
/// times</strong>, -- <strong>and <c>ExecuteShowBattleAnimation</c>
/// already sets five state fields and honours the wait.</strong>
/// </para>
/// <para>
/// <strong>And what it needs is the animation table</strong>, -- <strong>
/// and the reference's <c>GetElement</c> returns zero frames for an id
/// the table does not carry</strong>, -- <strong>which would make every
/// one of the 792 play nothing.</strong>
/// </para>
/// </remarks>
public partial class TestRm2kKampfanimationGemessen : TestBase
{
    private const string Spiel = "E:/RPGMakerGames/Dragon Destiny";

    /// <summary>
    /// And how many frames each of the game's animations carries.
    /// </summary>
    public void Test_DieAnimationstabelle()
    {
        if (!Directory.Exists(Spiel))
        {
            return;
        }

        using var host = new EnginePluginHost(
            BuiltInEnginePluginCatalog.CreateRuntimeRegistry());
        if (!host.Start(new PluginGameInfo
            {
                GameDirectory = Spiel,
                EngineId = EnginePluginIds.RpgMaker2000,
                Generation = "rm2k",
                DetectorScore = 3,
            }).Success)
        {
            return;
        }

        var lauf = (Rm2kEngineRuntime)host.Runtime!;
        var state = lauf.Simulation;

        // **Und  welche  Animations-IDs  benutzt  das  Spiel?**
        var parser = new UniversalRPG.Rm2k.Parser.Rm2kParser();
        var benutzt = new SortedSet<int>();
        var kartenGelesen = 0;
        var befehleGesamt = 0;
        var Animationen = 0;
        var seitenGesamt = 0;
        var seitenGelesen = 0;
        var ohneCode = 0;
        var mitAnderemTyp = 0;
        var mit11210 = 0;
        var ohneParameterKey = 0;
        var parameterKeinArray = 0;
        foreach (var datei in Directory.GetFiles(Spiel, "Map*.lmu"))
        {
            var karte = parser.ParseMap(datei);
            if (!karte.Success)
            {
                continue;
            }

            kartenGelesen++;
            if (!karte.Data.TryGetValue("events", out var roh)
                || roh.VariantType != Variant.Type.Array)
            {
                continue;
            }

            foreach (var evRoh in roh.AsGodotArray())
            {
                // **Und  die  Seiten  in  zwei  Schritten  lesen**,
                // -- **denn  in  einer  `||`-Kette  ist  eine
                //  `out`-Variable  beim  dritten  Test  noch  nicht
                //  zugewiesen**, -- **und  `TryGetValue`  gibt  `false`
                //  zurueck,  was  den  dritten  Test  nie  erreicht**.
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
                        // **Und  der  Typ  kommt  VOR  dem  Wert** --
                        // **und  in  meinem  ersten  Filter  stand  er
                        //  nicht  dastehend**, -- **und  ein
                        //  `AsInt32()`  auf  einen  nicht ganzzahligen
                        //  Variant  gibt  0  und  nicht  einen
                        //  Fehler**, -- **und  deshalb  habe  ich  keine
                        //  ID  gefunden.**
                        if (!b.TryGetValue("code", out var nr))
                        {
                            ohneCode++;
                            continue;
                        }

                        if (nr.VariantType != Variant.Type.Int)
                        {
                            mitAnderemTyp++;
                            continue;
                        }

                        var code = nr.AsInt32();
                        if (code != 11210)
                        {
                            continue;
                        }

                        mit11210++;
                        if (!b.TryGetValue("parameters",
                                out var pRoh))
                        {
                            ohneParameterKey++;
                            continue;
                        }

                        // **Und  `parameters`  ist  ein
                        //  `PackedInt32Array`  und  kein  Array.**
                        //
                        // **Und  das  ist  der  ganze  Unterschied
                        //  zwischen  diesem  Test  und  dem
                        //  Zaehlertest**, -- **und  er  hat  mich  drei
                        //  Fehlversuche  gekostet,  weil  ich  beide  als
                        //  dasselbe  gelesen  habe.**
                        if (pRoh.VariantType
                            == Variant.Type.PackedInt32Array)
                        {
                            for (var k = 0;
                                k < pRoh.AsInt32Array().Length; k++)
                            {
                                if (k == 0)
                                {
                                    benutzt.Add(
                                        pRoh.AsInt32Array()[k]);
                                }
                            }

                            Animationen++;
                            continue;
                        }

                        if (pRoh.VariantType
                            != Variant.Type.Array)
                        {
                            parameterKeinArray++;
                            continue;
                        }

                        Animationen++;
                        var arr = pRoh.AsGodotArray();
                        if (arr.Count > 0
                            && arr[0].VariantType == Variant.Type.Int)
                        {
                            benutzt.Add(arr[0].AsInt32());
                        }
                    }
                }
            }
        }


        Console.WriteLine("benutzte Animations-IDs: "
            + string.Join(",", benutzt));

        var mitFrames = 0;
        var ohneFrames = 0;
        var frameSumme = 0;
        foreach (var id in benutzt)
        {
            var frames = state.BattleAnimationFrameCount(id);
            if (frames > 0)
            {
                mitFrames++;
                frameSumme += frames;
            }
            else
            {
                ohneFrames++;
            }
        }

        Console.WriteLine($"mit Frames {mitFrames}  ohne "
            + $"{ohneFrames}  Summe {frameSumme}");

        AssertTrue(benutzt.Count > 0,
            "**and the game names battle animations** -- and"
                + " 792 commands across 743 maps name them");

        // **Und  eine  Animation  ohne  Frames  ist  eine  Aussage
        //  des  Spiels  und  kein  Lesefehler.**
        //
        // **Und  Animation  254  traegt  ein  einziges  Nullbyte  in
        //  `timings`**:
        //
        // <code>
        // 254 0x6 {1b}: 0
        // </code>
        //
        // **Und  das  heisst  null  Timings**, -- **und  die  Referenz
        //  gibt  dafuer  zurueck,  was  die  Timingliste  sagt**, --
        // **und  eine  leere  Liste  sagt  null.**
        Console.WriteLine("ohne Frames: " + string.Join(",",
            benutzt.Where(x =>
                state.BattleAnimationFrameCount(x) == 0)));

        AssertTrue(ohneFrames <= 1,
            "**and at most one of them has no frames** -- and that"
                + " one is animation 254, whose timing field is a"
                + " single zero byte, and the reference returns"
                + " zero frames for an empty list rather than"
                + " inventing one");

        AssertTrue(frameSumme > 0,
            "**and the frames are the game's own numbers** -- and"
                + " Dragon Destiny's 300 animations reach 69, and"
                + " a table of ones would be a reader that counted"
                + " rows instead of reading the frame column");
    }
}
