using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using UniversalRPG.Rm2k.Parser;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And which menu and save commands Dragon Destiny writes.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And criterion 1 still lacks menus and saving.</strong> --
/// <strong>And liblcf's <c>eventcommand.h</c> has no save or load
/// command in the map set</strong>, -- <strong>because RM2K saves from
/// its own menu and not from an event.</strong>
/// </para>
/// <para>
/// <strong>And that is a claim about the reference's command
/// table, and the game decides which of the rest it
/// uses.</strong> --
/// <strong>And the numbers come from liblcf and not from my
/// head.</strong>
/// </para>
/// </remarks>
public partial class TestRm2kMenueBefehleGemessen : TestBase
{
    private const string Spiel = "E:/RPGMakerGames/Dragon Destiny";

    /// <summary>
    /// And the shop, party, item and skill commands.
    /// </summary>
    public void Test_DieMenueBefehleDesSpiels()
    {
        if (!Directory.Exists(Spiel))
        {
            return;
        }

        var parser = new Rm2kParser();
        var zahler = new Dictionary<int, int>();
        var maps = 0;
        foreach (var datei in Directory.GetFiles(Spiel, "Map*.lmu"))
        {
            var karte = parser.ParseMap(datei);
            if (!karte.Success)
            {
                continue;
            }

            maps++;
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
                        zahler.TryGetValue(n, out var k);
                        zahler[n] = k + 1;
                    }
                }
            }
        }

        Console.WriteLine("Karten: " + maps);

        // **Und  jetzt  nur  die  Menuebefehle  aus  der
        //  Referenztabelle.**
        foreach (var num in new[] { 10310, 10320, 10330, 10440,
            10450, 10720, 10820, 10830, 10840, 10850, 10860,
            10870, 10880, 10890 })
        {
            Console.WriteLine($"  {num}: "
                + (zahler.TryGetValue(num, out var n) ? n : 0));
        }

        Console.WriteLine("--- 10xxx im Spiel ---");
        foreach (var x in zahler.Where(x => x.Key >= 10300
            && x.Key < 11000).OrderBy(x => x.Key))
        {
            Console.WriteLine($"  {x.Key}: {x.Value}");
        }

        // **Und  die  Herberge  ist  der  Speicherweg  von  RM2K.**
        Console.WriteLine("10730 Herberge: "
            + (zahler.TryGetValue(10730, out var inn)
                ? inn.ToString() : "0"));
        Console.WriteLine("10740 Held benennen: "
            + (zahler.TryGetValue(10740, out var nam)
                ? nam.ToString() : "0"));

        // **Und  gibt  es  ueberhaupt  einen  Speicherbefehl?**
        Console.WriteLine("--- alle 1xxxxx und 2xxxxx, die das "
            + " Spiel schreibt ---");
        foreach (var x in zahler.Where(x => x.Key > 100000)
            .OrderBy(x => x.Key).Take(30))
        {
            Console.WriteLine($"  {x.Key}: {x.Value}");
        }

        AssertTrue(maps > 700,
            "**and the game's maps are read**");

        // **Und  meine  Erwartung  war  falsch.**
        //
        // **Und  ich  habe  behauptet,  das  Spiel  benutze  die
        //  Herberge**, -- **und  die  Messung  sagt:  null  Mal.**
        // **Und  `10740`  Held  benennen  steht  ebenfalls  bei  null.**
        //
        // **Und  liblcfs  `eventcommand.h`  hat  keinen
        //  Speicherbefehl  und  keinen  Ladebefehl** -- **und  dieses
        //  Spiel  schreibt  keinen  Befehl  ueber  100000** -- **und
        //  damit  ist  der  Weg  dieses  Spiels  ausschliesslich  das
        //  Spielmenue  des  Launchers.**
        //
        // **Und  das  ist  keine  Luecke  des  Lesers  und  keine
        //  Luecke  der  Engine  --  es  ist  die  Tatsache,  dass
        //  Dragon  Destiny  sein  Spiel  in  diesem  Launcher  ueber
        //  ein  Menue  speichern  laesst  oder  gar  nicht.**
        AssertEq(0, zahler.TryGetValue(10730, out var h0)
                ? h0 : 0,
            "**and the game writes no inn command** -- and the"
                + " claim that it did was mine and the count says"
                + " zero, and a test that asserts what it hoped"
                + " for is not a measurement");

        AssertEq(0, zahler.Count(x => x.Key > 100000),
            "**and the game writes no battle or page command"
                + " above 99999** -- and that is what makes the"
                + " inn and the save question a menu question"
                + " rather than an interpreter one");
    }
}
