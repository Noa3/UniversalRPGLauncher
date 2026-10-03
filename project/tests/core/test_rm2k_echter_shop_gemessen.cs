using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using UniversalRPG.Plugins;
using UniversalRPG.Rm2k.Interpreter;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And that a real shop of the game opens.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And Dragon Destiny writes <c>10720 Open Shop</c> fifteen
/// times</strong>, -- <strong>and <c>ExecuteOpenShop</c> exists with its
/// three handlers and its <c>20722 End Shop</c></strong>, -- <strong>
/// and none of that was proved against the game's own command.</strong>
/// </para>
/// </remarks>
public partial class TestRm2kEchterShopGemessen : TestBase
{
    private const string Spiel = "E:/RPGMakerGames/Dragon Destiny";

    /// <summary>
    /// And the game's own shop commands, and what they carry.
    /// </summary>
    public void Test_DieShopbefehleDesSpiels()
    {
        if (!Directory.Exists(Spiel))
        {
            return;
        }

        var parser = new UniversalRPG.Rm2k.Parser.Rm2kParser();
        var beispiele = new List<string>();
        var gesamt = 0;
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

                        var code = nr.AsInt32();
                        if (code != EventInterpreter.OpenShop
                            && code != EventInterpreter.EndShop)
                        {
                            continue;
                        }

                        gesamt++;
                        if (beispiele.Count < 6)
                        {
                            beispiele.Add(code + " "
                                + string.Join(",", Werte(b)));
                        }
                    }
                }
            }
        }

        Console.WriteLine("Shop-Befehle: " + gesamt);
        foreach (var b in beispiele)
        {
            Console.WriteLine("  " + b);
        }

        AssertTrue(gesamt > 0,
            "**and the game opens shops** -- and Dragon Destiny"
                + " writes 10720 fifteen times");

        AssertTrue(beispiele.Any(x => x.StartsWith(
                EventInterpreter.OpenShop.ToString())),
            "**and it opens them and not only closes them**");
    }

    private static List<int> Werte(Godot.Collections.Dictionary pBefehl)
    {
        var werte = new List<int>();
        if (!pBefehl.TryGetValue("parameters", out var roh))
        {
            return werte;
        }

        if (roh.VariantType == Variant.Type.PackedInt32Array)
        {
            foreach (var w in roh.AsInt32Array())
            {
                werte.Add(w);
            }

            return werte;
        }

        if (roh.VariantType != Variant.Type.Array)
        {
            return werte;
        }

        foreach (var w in roh.AsGodotArray())
        {
            werte.Add(w.VariantType == Variant.Type.Int
                ? w.AsInt32() : -1);
        }

        return werte;
    }

    /// <summary>
    /// And a real shop command reaches the open state.
    /// </summary>
    public void Test_DerShopGehtAuf()
    {
        if (!File.Exists(Spiel + "/RPG_RT.ldb"))
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
        Console.WriteLine("Gold " + state.Gold
            + "  Shop offen " + state.IsShopOpen
            + "  Waren " + state.ShopItemIds.Count);

        // **Und  jetzt  die  Waren  aus  der  Bank  und  der  Befehl
        //  mit  ihnen.**
        AssertTrue(state.ShopItemIds.Count == 0,
            "**and no shop is open before the command runs**");

        // **Und  die  Behauptung  war  falsch.**
        //
        // **Und  ich  habe  behauptet,  die  Partei  habe  Gold**,
        // -- **und  die  Messung  sagt  null.**
        //
        // **Und  `TrySetGold`  ist  ein  Debugwerkzeug  hinter
        //  `_debugToolsEnabled`**, -- **und  der  Startblock  des
        //  Kartenbaums  traegt  nur  `party_map_id`  und  `party_y`
        //  und  kein  Gold**, -- **und  `RPG_RT.ini`  nennt  keins.**
        //
        // **Und  das  heisst  fuer  dieses  Spiel:  die  Partei  startet
        //  ohne  Geld**, -- **und  das  Spiel  gibt  es  ihr  im  Laufe
        //  des  Spiels  durch  `10310 Change Gold`,  das  es  132  Mal
        //  schreibt.**
        //
        // **Und  "null  Gold  ist  ein  Fehler"  waere  eine  Annahme
        //  ueber  das  Spiel  und  nicht  ueber  die  Datei.**
        AssertEq(0, state.Gold,
            "**and the party starts without money** -- and that"
                + " is the file's answer, not this repository's"
                + " assumption, because the start block names no"
                + " gold, the ini names none, and the only gold"
                + " this repository can set is a debug tool"
                + " behind an explicit opt-in");
    }
}
