using System;
using System.IO;
using System.Linq;
using Godot;
using UniversalRPG.Rm2k.Parser;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And what a map's dictionary really carries.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And my menu measurement read <c>events</c> and
/// <c>event_commands</c> and found nothing</strong>, -- <strong>and
/// that is the second time I have guessed a key</strong>.
/// </para>
/// <para>
/// <strong>And the parser's own keys are the answer</strong>.
/// </para>
/// </remarks>
public partial class TestRm2kKartenschluesselGemessen : TestBase
{
    private const string Spiel = "E:/RPGMakerGames/Dragon Destiny";

    /// <summary>
    /// And the keys the parser gives a map.
    /// </summary>
    public void Test_DieSchluesselEinerKarte()
    {
        if (!Directory.Exists(Spiel))
        {
            return;
        }

        // **Und  Map0001  traegt  `events: Array n=0`** -- **und  das  ist
        //  eine  von  743  Karten.**
        //
        // **Und  ich  muss  eine  mit  Ereignissen  nehmen**, -- **und
        //  eine  Leinwand  zeigt  keine  Befehle**, -- **und  darum  war
        //  meine  Menuezaehlung  bei  einer  leeren  Karte.**
        var datei = Directory.GetFiles(Spiel, "Map*.lmu")
            .First(x => new Rm2kParser().ParseMap(x).Data
                .TryGetValue("events", out var e)
                && e.AsGodotArray().Count > 0);
        Console.WriteLine("Karte: " + Path.GetFileName(datei));
        var karte = new Rm2kParser().ParseMap(datei);
        if (!karte.Success)
        {
            AssertTrue(false, "**and a map is read** -- "
                + karte.Error!.Message);
            return;
        }

        // **Und  jetzt  die  Seite  und  ihre  Befehle.**
        var ersteSeite = karte.Data["events"].AsGodotArray()[0]
            .AsGodotDictionary();
        Console.WriteLine("Ereignis-Schluessel:");
        foreach (var k in ersteSeite.Keys)
        {
            Console.WriteLine("  " + k + " : "
                + ersteSeite[k].VariantType);
        }

        if (ersteSeite.ContainsKey("pages"))
        {
            var seiten = ersteSeite["pages"];
            if (seiten.VariantType == Godot.Variant.Type.Array
                && seiten.AsGodotArray().Count > 0)
            {
                Console.WriteLine("Seiten-Schluessel:");
                for (var s = 0; s < seiten.AsGodotArray().Count;
                    s++)
                {
                    var seite = seiten.AsGodotArray()[s]
                        .AsGodotDictionary();
                    Console.WriteLine(" Seite " + s);
                    foreach (var k in seite.Keys)
                    {
                        var v = seite[k];
                        Console.WriteLine("   " + k + " : "
                            + v.VariantType
                            + (v.VariantType
                                == Godot.Variant.Type.Array
                                ? " n=" + v.AsGodotArray().Count
                                : ""));
                    }
                }
            }
        }

        foreach (var k in karte.Data.Keys)
        {
            var v = karte.Data[k];
            Console.WriteLine("  " + k + " : " + v.VariantType
                + (v.VariantType == Godot.Variant.Type.Array
                    ? " n=" + v.AsGodotArray().Count : ""));
        }

        AssertTrue(karte.Data.Count > 0,
            "**and the map carries keys**");
    }
}
