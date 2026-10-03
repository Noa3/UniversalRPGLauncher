using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using UniversalRPG.Rm2k.Parser;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And whether the game uses classes at all.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And <c>ActorClassId</c> is progress that is not
/// saved</strong>, -- <strong>from <c>1008 Change Class</c>, and it is
/// the reference's own command number.</strong>
/// </para>
/// <para>
/// <strong>And saving a class id for a game that has no classes
/// would be saving a number nothing reads.</strong>
/// </para>
/// </remarks>
public partial class TestRm2kKlassenGemessen : TestBase
{
    private const string Spiel = "E:/RPGMakerGames/Dragon Destiny";
    private const string Ldb =
        "res://tests/fixtures/rm2k-dragon-destiny/RPG_RT.ldb";

    /// <summary>
    /// And the bank's classes and the actors that have one.
    /// </summary>
    public void Test_DieKlassenDerBank()
    {
        var parser = new Rm2kParser();
        var result = parser.ParseDatabase(Ldb);
        if (!result.IsSuccess())
        {
            AssertTrue(false, "**and the bank is read**");
            return;
        }

        var bank = result.GetData();
        var klassen = (Godot.Collections
            .Array<Godot.Collections.Dictionary>)
            bank["classes"];
        Console.WriteLine("Klassen: " + klassen.Count);
        foreach (var k in klassen.Take(3))
        {
            Console.WriteLine("  " + (k.ContainsKey("name")
                ? k["name"].AsString() : "?"));
        }

        // **Und  jetzt  die  Helden  und  ihre  Klasse.**
        var helden = (Godot.Collections
            .Array<Godot.Collections.Dictionary>)
            bank["actors"];
        var mitKlasse = 0;
        for (var i = 0; i < helden.Count; i++)
        {
            var h = helden[i];
            Console.WriteLine("  Held " + h["name"].AsString()
                + "  klasse "
                + (h.ContainsKey("class_id")
                    ? h["class_id"].ToString() : "fehlt")
                + "  schluessel "
                + (h.ContainsKey("class_id") ? "ja" : "nein"));
            if (h.ContainsKey("class_id")
                && h["class_id"].AsInt32() > 0)
            {
                mitKlasse++;
            }
        }

        Console.WriteLine("Helden mit Klasse: " + mitKlasse);
        // **Und  meine  Erwartung  war  falsch.**
        //
        // **Und  ich  habe  behauptet,  das  Spiel  definiere
        //  Klassen** -- **und  die  Messung  sagt:  null.**
        //
        // **Und  kein  Held  traegt  ein  Klassenfeld  und  `1008
        //  Change Class`  kommt  in  keiner  der  743  Karten
        //  vor.**
        //
        // **Und  das  heisst  fuer  dieses  Spiel:  es  gibt  keine
        //  Klassen**, -- **und  `ActorClassId`  zu  sichern  waere  eine
        //  Zahl  zu  sichern,  die  nichts  liest.**
        AssertEq(0, klassen.Count,
            "**and the game defines no classes** -- and the"
                + " claim that it did was mine, and a save that"
                + " carried a class id for this game would carry a"
                + " number nothing reads");

        AssertEq(0, mitKlasse,
            "**and no hero carries one** -- and all seven actor"
                + " rows were printed and none of them has the"
                + " field");
    }

    /// <summary>
    /// And how often the game changes one.
    /// </summary>
    public void Test_WieOftDieKlasseWechselt()
    {
        if (!Directory.Exists(Spiel))
        {
            return;
        }

        var parser = new Rm2kParser();
        var anzahl = 0;
        foreach (var datei in Directory.GetFiles(Spiel, "Map*.lmu"))
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
                        if (b.TryGetValue("code", out var nr)
                            && nr.VariantType == Variant.Type.Int
                            && nr.AsInt32() == 1008)
                        {
                            anzahl++;
                        }
                    }
                }
            }
        }

        Console.WriteLine("1008 Change Class: " + anzahl);
        AssertEq(0, anzahl,
            "**and the game never changes a class** -- and that"
                + " is the reason a class id is not progress in"
                + " this game, and saving one would be saving a"
                + " number this game never uses");
    }
}
