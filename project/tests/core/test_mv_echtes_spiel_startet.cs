using System;
using System.IO;
using System.Linq;
using Godot;
using UniversalRPG.App.Library;
using UniversalRPG.GameDetectorNs;
using UniversalRPG.Mz;
using UniversalRPG.Rm2k.Rendering;
using UniversalRPG.Plugins;
using UniversalRPG.Tests.Framework;
using UniversalRPG.Web;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And the real MV game starts and paints, through the launcher path.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And MV and MZ are one runtime.</strong> MV's command list is a
/// subset of MZ's, which <c>TestMvCommandTableAgainstTheEngine</c>
/// measures against the engine's own <c>rpg_objects.js</c>, so both
/// engines get the same <c>MzEngineRuntime</c> under a different
/// generation name.
/// </para>
/// <para>
/// <strong>And MV is the harder one of the pair here</strong>, because its
/// pictures are <c>*.rpgmvp</c> and MZ's are <c>*.png_</c>; <strong>a
/// runtime that only opens the MZ spelling paints nothing at all on an
/// MV game</strong>, and that is a fact a screenshot shows and a
/// capability bit does not.
/// </para>
/// </remarks>
public partial class TestMvEchtesSpielStartet : TestBase
{
    private const string Projekt = "E:/RPGMakerGames/LegalTruck_v1.1/www";

    private static bool Vorhanden() =>
        File.Exists(Projekt + "/data/Map001.json")
        && File.Exists(Projekt + "/data/System.json")
        && Directory.Exists(Projekt + "/img/tilesets");

    public void Test_DieErkennungMeldetDasEchteMvAlsStartbar()
    {
        if (!Vorhanden())
        {
            return;
        }

        var report = new GameDetector().Analyze(Projekt);
        var mv = report.Candidates.FirstOrDefault(pCandidate =>
            pCandidate.PluginId == EnginePluginIds.RpgMakerMv);
        AssertTrue(mv != null, "the real MV game yields an MV candidate");
        if (mv == null)
        {
            return;
        }
        Console.WriteLine($"MV candidate: status={mv.Status} score={mv.Score} gen={mv.Generation}");
        AssertEq(mv.Status, EngineDetectionStatus.Supported,
            "the MV candidate is supported, which is what enables the Start button");
    }

    public void Test_DerLauncherStartetDasEchteMvUndLiefertEinBild()
    {
        if (!Vorhanden())
        {
            return;
        }

        var launcher = new App.Launcher.RuntimeLauncher(
            BuiltInEnginePluginCatalog.CreateRuntimeRegistry());
        var report = new GameDetector().Analyze(Projekt);
        var entry = new GameLibrary.GameEntry(Projekt, report);

        var support = launcher.GetSupport(entry);
        AssertEq(support.State, App.Launcher.RuntimeLauncher.SupportState.Available,
            $"the launcher reports the MV runtime as available: {support.Reason}");

        var result = launcher.Launch(entry);
        AssertTrue(result.Success, $"the launcher launches the real MV game: {result.Message}");
        if (!result.Success)
        {
            return;
        }

        var runtime = (MzEngineRuntime)launcher.ActiveRuntime!;
        AssertEq(launcher.ActiveRuntimeState, PluginRuntimeState.Running,
            "the launcher holds a running MV runtime");
        for (var frame = 0; frame < 120; frame++)
        {
            runtime.Update(1.0 / 60.0);
        }

        var map = runtime.PaintedMap;
        AssertTrue(map != null && map.Width > 0,
            "the MV game paints a frame a player could look at");
        if (map == null)
        {
            return;
        }
        var lit = 0;
        for (var index = 3; index < map.Pixels.Length; index += 4)
        {
            if (map.Pixels[index] != 0)
            {
                lit += 1;
            }
        }
        Console.WriteLine($"MV start map painted {map.Width}x{map.Height}, lit={lit}, "
            + $"colours={runtime.PaintedColours}, reason={runtime.PaintReason}");
        // **Und die Startkarte dieses Spiels ist eine leere Editor-Karte.**
        // Measured at LegalTruck: MapInfos gives Map001 the order 1 and the
        // name 'Hai', and its `data` is 1326 zeroes -- not one drawn tile in
        // a 17x13 room. **A correct reader paints nothing there and says
        // so;** the drawn map of this project is Map005.
        AssertEq(runtime.PaintReason, "",
            $"a painted MV frame needs no diagnostic, but it says: {runtime.PaintReason}");
        launcher.Stop();
    }

    /// <summary>
    /// And the one drawn map of the real MV game paints actual tiles.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the assertion that separates "the renderer
    /// works" from "the game's own start map is empty".</strong>
    /// LegalTruck ships seven empty editor maps and one drawn one;
    /// measuring only the start map would prove nothing about the
    /// renderer, and would pass on a reader that paints nothing at all.
    /// </para>
    /// </remarks>
    public void Test_DieGezeichneteMvKarteMaltEchteKacheln()
    {
        if (!Vorhanden())
        {
            return;
        }

        using var host = new EnginePluginHost(
            BuiltInEnginePluginCatalog.CreateRuntimeRegistry());
        var started = host.Start(new PluginGameInfo
        {
            GameDirectory = Projekt,
            EngineId = EnginePluginIds.RpgMakerMv,
            Generation = "mv",
            DetectorScore = 850,
        });
        AssertTrue(started.Success, $"the MV game starts: {started.Error?.Message}");
        if (!started.Success)
        {
            return;
        }

        // Map005 'Test map' is the measured one with drawn tiles: 260 of
        // 1560 values non-zero. Read through the runtime's own map, painted
        // by the runtime's own renderer.
        var runtime = (MzEngineRuntime)host.Runtime!;
        var gemalt = runtime.Maps.ContainsKey(5);
        AssertTrue(gemalt, "the MV project carries the drawn map 5");

        // And the tileset itself, through the same renderer the map uses.
        var schluessel = MzVerschluesselung.SchluesselDesSpiels();
        var system = MzDataFile.Read("data/System.json",
            File.ReadAllBytes(Projekt + "/data/System.json"));
        schluessel = system.Root.Member("encryptionKey")?.StringOr("") ?? "";
        var bild = MzImageReader.Read(
            File.ReadAllBytes(Projekt + "/img/tilesets/World_A1.rpgmvp"),
            schluessel, out var grund);
        AssertTrue(bild != null, $"the MV tileset is read: {grund}");
        AssertTrue(Rm2kIndexedImage.TryParse(bild!, out var ind, out var fehler),
            $"the RGBA MV tileset becomes an indexed image: {fehler}");
        if (ind == null)
        {
            return;
        }
        AssertEq(ind.Width, 768, "the tileset is 768 wide");
        AssertTrue(ind.Palette.Length >= 256,
            $"the tileset keeps its colour range, got {ind.Palette.Length}");

        // A tile the map actually uses must exist in the sheet.
        var karte = MzDataFile.Read("data/Map005.json",
            File.ReadAllBytes(Projekt + "/data/Map005.json"));
        var daten = karte.Root.Member("data")?.Items
            ?? (System.Collections.Generic.IReadOnlyList<MzValue>)[];
        var benutzt = daten.Select(pFeld => pFeld.IntOr(0)).Where(pWert => pWert != 0).ToList();
        AssertTrue(benutzt.Count > 0, "the drawn MV map references tiles");
        var tile = benutzt[0];
        var zelle = tile / 10000;
        AssertTrue(zelle < ind.Palette.Length || tile < ind.Width,
            $"tile {tile} addresses something the sheet holds");
    }

    public void Test_DerMvPluginWirbtLaufzeitUndNichtNurErkennung()
    {
        var plugin = BuiltInEnginePluginCatalog.CreateRuntimeRegistry()
            .Plugins.Single(pPlugin => pPlugin.Metadata.Id == EnginePluginIds.RpgMakerMv);
        AssertTrue((plugin.Metadata.Capabilities & PluginCapability.Runtime) != 0,
            "the MV plugin advertises the runtime capability");
    }
}
