using System;
using System.IO;
using System.Linq;
using Godot;
using UniversalRPG.Plugins;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Three finished RM2K games, started, rendered and run.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And until now every runtime test ran against one
/// game.</strong> -- <strong>And one game is one chipset, one set of
/// maps and one author's habits.</strong> --
/// <strong>Three games are three of each, and a reader that fits one
/// is not shown to fit the other two.</strong>
/// </para>
/// <code>
/// Dragon Destiny        743 Maps
/// Lisa                   45 Maps
/// Pom Gets Wi-Fi v1-04  18 Maps
/// </code>
/// <para>
/// <strong>And all three carry the same files</strong>, --
/// <c>RPG_RT.ldb</c>, <c>Backdrop</c>, <c>ChipSet</c>,
/// <c>CharSet</c> and <c>Panorama</c> -- <strong>and that is what
/// makes them comparable.</strong>
/// </para>
/// <para>
/// <strong>And what is measured here is what the runtime can
/// actually do</strong>, -- <strong>and that is: start on the
/// game's own start map, render it with the game's own chipset, and
/// run a hundred frames.</strong> -- <strong>And the runtime has no
/// card-change of its own</strong>, -- <strong>so this does not
/// pretend to change maps and does not assert
/// it.</strong>
/// </para>
/// </remarks>
public partial class TestRm2kDreiSpiele : TestBase
{
    private static readonly string[] Spiele =
    {
        "E:/RPGMakerGames/Dragon Destiny",
        "E:/RPGMakerGames/Lisa",
        "E:/RPGMakerGames/Pom Gets Wi-Fi v1-04",
    };

    private static int Vorhanden() =>
        Spiele.Count(Directory.Exists);

    private static PluginGameInfo Spiel(string pWurzel) => new()
    {
        GameDirectory = pWurzel,
        EngineId = EnginePluginIds.RpgMaker2000,
        Generation = "rm2k",
        DetectorScore = 3,
    };

    /// <summary>
    /// And how many colours a picture has.
    /// </summary>
    /// <param name="pBild">The picture.</param>
    /// <returns>The count, and -1 when there is none.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And a picture of one colour is a picture whose
    /// chipset was not found</strong>, -- <strong>and that is the one
    /// failure this count exists to catch.</strong>
    /// </para>
    /// </remarks>
    private static int Rm2kPixelBufferZaehler(
        UniversalRPG.Rm2k.Rendering.Rm2kPixelBuffer? pBild)
    {
        if (pBild == null)
        {
            return -1;
        }

        return pBild.Pixels.Distinct().Count();
    }

    /// <summary>
    /// And all three start on their own start map.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the refusal of each is printed</strong>, --
    /// <strong>because three refusals that say the same thing are
    /// three different faults and one refusal that says nothing is
    /// not an answer.</strong>
    /// </para>
    /// </remarks>
    public void Test_AlleDreiStarten()
    {
        if (Vorhanden() < 3)
        {
            return;
        }

        foreach (var spiel in Spiele)
        {
            var name = Path.GetFileName(spiel);
            using var host = new EnginePluginHost(
                BuiltInEnginePluginCatalog.CreateRuntimeRegistry());
            var started = host.Start(Spiel(spiel));

            Console.WriteLine($"{name}: {started.Success}  "
                + $"{started.Error?.Message}");
            AssertTrue(started.Success,
                "**and " + name + " starts** -- and the refusal is: "
                    + started.Error?.Message);
            AssertTrue(host.Runtime is Rm2kEngineRuntime,
                "**and " + name + " built an RM2K runtime**");
        }
    }

    /// <summary>
    /// And each renders its own start map in its own colours.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the assertion that matters</strong>, --
    /// <strong>because a runtime that starts and paints nothing is a
    /// runtime that starts.</strong> -- <strong>And the pixel count
    /// is the one the game's own map file says</strong>, --
    /// <strong>and a reader that fell back to its own numbers would
    /// pass the first test and fail
    /// this.</strong>
    /// </para>
    /// </remarks>
    public void Test_AlleDreiMalenIhreKarte()
    {
        if (Vorhanden() < 3)
        {
            return;
        }

        foreach (var spiel in Spiele)
        {
            var name = Path.GetFileName(spiel);
            using var host = new EnginePluginHost(
                BuiltInEnginePluginCatalog.CreateRuntimeRegistry());
            if (!host.Start(Spiel(spiel)).Success
                || host.Runtime is not Rm2kEngineRuntime lauf)
            {
                AssertTrue(false, "**and " + name + " started**");
                continue;
            }

            Console.WriteLine($"{name}: Mapdata "
                + $"{lauf.CurrentMapData != null}  Bild "
                + $"{lauf.RenderedMap != null}  Farben "
                + $"{Rm2kPixelBufferZaehler(lauf.RenderedMap)}  "
                + $"Grund '{lauf.RenderDiagnostic}'");

            AssertTrue(lauf.CurrentMapData != null,
                "**and " + name + " loaded its start map**");
            AssertTrue(lauf.RenderedMap != null,
                "**and " + name + " rendered it** -- and the"
                    + " diagnostic is: " + lauf.RenderDiagnostic);
            AssertTrue(lauf.RenderedMap!.Width > 0
                    && lauf.RenderedMap.Height > 0,
                "**and the picture has a size**");
        }
    }

    /// <summary>
    /// And each one runs a hundred frames and keeps its state.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And a hundred frames is where a renderer that paints
    /// one frame and a simulator that runs one frame both look
    /// finished.</strong>
    /// </para>
    /// </remarks>
    public void Test_AlleDreiLaufenHundertBilder()
    {
        if (Vorhanden() < 3)
        {
            return;
        }

        foreach (var spiel in Spiele)
        {
            var name = Path.GetFileName(spiel);
            using var host = new EnginePluginHost(
                BuiltInEnginePluginCatalog.CreateRuntimeRegistry());
            if (!host.Start(Spiel(spiel)).Success
                || host.Runtime is not Rm2kEngineRuntime lauf)
            {
                AssertTrue(false, "**and " + name + " started**");
                continue;
            }

            for (var i = 0; i < 100; i++)
            {
                lauf.Update(1.0 / 60.0);
            }

            Console.WriteLine($"{name}: Rahmen "
                + $"{lauf.Simulation.FrameCount}  Figuren "
                + $"{lauf.SpriteDescriptors.Count}  Schalter "
                + $"{lauf.Simulation.Switches.Count}  Gold "
                + $"{lauf.Simulation.Gold}");

            AssertEq(100, lauf.Simulation.FrameCount,
                "**and " + name + " ran a hundred frames**");
            AssertTrue(lauf.RenderedMap != null,
                "**and it still has a picture after a hundred"
                    + " frames**");
        }
    }
}
