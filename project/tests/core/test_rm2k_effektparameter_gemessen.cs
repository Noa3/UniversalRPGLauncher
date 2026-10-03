using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using UniversalRPG.Rm2k.Parser;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And the parameters an effect command actually carries.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the last test found four effect commands with empty
/// parameter lists</strong>, -- <strong>and Dragon Destiny writes
/// <c>11030</c> 2910 times, so an empty list would mean the reader
/// lost them.</strong>
/// </para>
/// </remarks>
public partial class TestRm2kEffektparameterGemessen : TestBase
{
    private const string Spiel = "E:/RPGMakerGames/Dragon Destiny";

    /// <summary>
    /// And the raw shape of an effect command.
    /// </summary>
    public void Test_DieRohformDesEffektbefehls()
    {
        if (!Directory.Exists(Spiel))
        {
            return;
        }

        var parser = new Rm2kParser();
        var gefunden = 0;
        foreach (var datei in Directory.GetFiles(Spiel, "Map*.lmu")
            .OrderBy(x => x))
        {
            var karte = parser.ParseMap(datei);
            if (!karte.Success
                || !karte.Data.TryGetValue("events", out var roh)
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
                        if (n != 11030 && n != 11040
                            && n != 11050 && n != 11210)
                        {
                            continue;
                        }

                        gefunden++;
                        if (gefunden > 3)
                        {
                            Console.WriteLine("--- Schlüssel des"
                                + " Befehls: " + string.Join(" ",
                                    b.Keys.Select(x => x.ToString())
                                        .OrderBy(x => x)));
                            foreach (var schluessel in new[] {
                                "parameters", "params",
                                "parameters_size", "indent",
                                "string", "event_id" })
                            {
                                if (!b.ContainsKey(schluessel))
                                {
                                    continue;
                                }

                                var w = b[schluessel];
                                var text = w.VariantType
                                    == Variant.Type.Array
                                    ? string.Join(",", w.AsGodotArray()
                                        .Select(x => x.VariantType
                                            == Variant.Type.Int
                                            ? x.AsInt32().ToString()
                                            : x.ToString()))
                                    : w.ToString();
                                Console.WriteLine("  " + schluessel
                                    + " = " + text);
                            }
                        }
                    }
                }
            }

            if (gefunden > 3)
            {
                break;
            }
        }

        Console.WriteLine("gefunden " + gefunden);
        AssertTrue(gefunden > 0,
            "**and the game writes effect commands**");
    }
}
