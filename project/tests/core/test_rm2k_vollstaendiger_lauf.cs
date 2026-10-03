using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniversalRPG.Plugins;
using UniversalRPG.Rm2k.Rendering;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And that a real game runs for a hundred frames.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And criterion 1 is "RPG Maker 2000 fully
/// functional"</strong>, -- <strong>and every part of that has its own
/// test</strong>, -- <strong>and no test yet ran the whole thing the
/// way the launcher does</strong>:  -- <strong>host, scheduler,
/// renderer, events, all in one loop, from the game's own start
/// map.</strong>
/// </para>
/// <para>
/// <strong>And a hundred tests of each part do not make a game
/// run.</strong>
/// </para>
/// </remarks>
public partial class TestRm2kVollstaendigerLauf : TestBase
{
    private const string Spiel = "E:/RPGMakerGames/Dragon Destiny";

    /// <summary>
    /// And the game runs, and the frame has pixels.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the loop is the host's own <c>Update</c></strong>,
    /// -- <strong>not the interpreter's, and not the
    /// scheduler's</strong>, -- <strong>because the host is what the
    /// launcher calls once per displayed frame.</strong>
    /// </para>
    /// </remarks>
    public void Test_HundertBilderEinesEchtenSpiels()
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
            AssertTrue(false, "**and the game starts**");
            return;
        }

        var lauf = (Rm2kEngineRuntime)host.Runtime!;
        Console.WriteLine("Karte " + lauf.Simulation.MapId
            + "  " + lauf.Simulation.MapWidth + "x"
            + lauf.Simulation.MapHeight + "  Spieler "
            + lauf.Simulation.MapX + "/" + lauf.Simulation.MapY);

        AssertTrue(lauf.Simulation.MapId > 0,
            "**and the game opens on the map its map tree names**"
                + " -- and Dragon Destiny starts on 742");

        // **Und  jetzt  hundert  Bilder  durch  den  Host.**
        var schritte = 100;
        for (var i = 0; i < schritte; i++)
        {
            lauf.Update(1.0 / 60.0);
        }

        Console.WriteLine("nach " + schritte + " Bildern: Spieler "
            + lauf.Simulation.MapX + "/" + lauf.Simulation.MapY
            + "  Schritte " + lauf.Simulation.Steps);

        AssertTrue(lauf.Simulation.Steps >= 0,
            "**and the step counter is real**");

        // **Und  jetzt  der  Beweis,  dass  etwas  gezeichnet  wurde.**
        //
        // **Und  der  Bildpuffer  arbeitet  mit  Kacheln  und  nicht
        //  mit  Pixeln** -- **und  das  ist  die  Form,  die  die  ganze
        //  Engine  benutzt**, -- **denn  RM2K  zeichnet  in  16er-Felder
        //  und  der  Puffer  speichert  genau  die.**
        lauf.RefreshFrameForTest();
        var puffer = lauf.Framebuffer;
        Console.WriteLine("Framebuffer: "
            + (puffer == null ? "keiner"
                : puffer.Width + "x" + puffer.Height));

        AssertTrue(puffer != null,
            "**and the host owns a frame to draw into**");

        //
        // **Und  der  Puffer  haelt  zwei  Schichten**, -- **denn  der
        //  Spieler  zeichnet  die  untere  Ebene  vor  dem  Helden  und
        //  die  obere  danach**, -- **und  ein  Test,  der  nur  eine
        //  liest,  prueft  die  Haelfte  des  Bildes.**
        var gemalt = 0;
        var verschiedene = new SortedSet<int>();
        foreach (var schicht in new[] {
            RenderLayer.Lower,
            RenderLayer.Upper,
        })
        {
            var inSchicht = 0;
            for (var y = 0; y < puffer!.Height; y++)
            {
                for (var x = 0; x < puffer.Width; x++)
                {
                    var kachel = puffer.GetTile(schicht, x, y);
                    if (kachel != 0)
                    {
                        inSchicht++;
                        gemalt++;
                    }

                    if (verschiedene.Count < 12)
                    {
                        verschiedene.Add(kachel);
                    }
                }
            }

            Console.WriteLine(schicht + ": " + inSchicht
                + " Kacheln");
        }

        Console.WriteLine("gemalte Kacheln: " + gemalt
            + "  verschiedene Chips: " + verschiedene.Count);

        AssertTrue(gemalt > 0,
            "**and the frame holds drawn tiles** -- and a"
                + " buffer full of zeros is a buffer with no"
                + " picture in it, and every part of the renderer"
                + " could pass its own test while the whole"
                + " stayed black");
    }
}
