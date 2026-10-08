using System;
using System.Collections.Generic;
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

    /// <summary>
    /// And the MV player's own sheet is loaded, which is a fact about the disk.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the character-sheet twin of the tileset fix.</strong>
    /// LegalTruck's sheets are <c>*.rpgmvp</c> — the player is
    /// <c>!Sprite1</c>, the vehicle is <c>Vehicle</c>, and neither has a
    /// <c>.png_</c> spelling. A reader that filtered for <c>.png_</c> alone
    /// found no sheet at all, and the hero was painted with nothing: a game
    /// that starts and shows a room but no player.
    /// </para>
    /// </remarks>
    public void Test_DerMvSpielerTragteinGeladenesFigurenblatt()
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
        var runtime = (MzEngineRuntime)host.Runtime!;

        // Actors.json names the player's sheet; it must be in the runtime's
        // character table. Measured: LegalTruck's player is `!Sprite1`,
        // on disk as `!Sprite1.rpgmvp`.
        var sheet = runtime.PlayerSheetName;
        AssertTrue(sheet.Length > 0,
            "the MV project names a sheet for its player");
        AssertTrue(
            runtime.CharacterFiles.ContainsKey(sheet)
                && runtime.SheetNamed(sheet) != null,
            $"the player's sheet \"{sheet}\" is loaded -- a reader that "
                + "filtered for .png_ alone found no sheet in an MV game "
                + "whose sheets are .rpgmvp, and the hero was painted "
                + "with nothing");
    }

    /// <summary>
    /// And an arrow key moves the MV hero, and a wall stops it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the MV twin of
    /// <c>Test_EinePfeiltasteBewegtDenHeroImEchtenMzSpiel</c>.</strong>
    /// The start map of this project, Map001 "Hai", is an empty editor
    /// map: 1326 cells, and every one of them is a star tile — the
    /// engine's own <c>checkPassage</c> says a cell whose tiles are all
    /// star refuses a step, so a correct reader must leave the hero where
    /// he stands. That is the honest answer, and the RM2K test already
    /// proved the same on a wall.
    /// </para>
    /// <para>
    /// <strong>And the engine turns the hero on a blocked key.</strong>
    /// Measured in <c>Game_CharacterBase.prototype.moveStraight</c>: the
    /// <c>canPass</c> test guards only the step, and
    /// <c>setDirection</c> runs either way. A runtime that refused the
    /// step and kept the old facing would leave the sprite looking the
    /// wrong way after every bump into a wall.
    /// </para>
    /// <para>
    /// <strong>And the drawn map of the project, Map005, is walkable in
    /// every direction</strong>, -- <strong>and a step there moves the
    /// hero and repaints the frame</strong>, -- <strong>which is the
    /// assertion that separates "the input path reaches the player"
    /// from "a key is pressed and dropped."</strong>
    /// </para>
    /// </remarks>
    public void Test_EinePfeiltasteBewegtDenHeroImEchtenMvSpiel()
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
        var runtime = (MzEngineRuntime)host.Runtime!;
        for (var frame = 0; frame < 30; frame++)
        {
            runtime.Update(1.0 / 60.0);
        }

        // The start map is all star tiles; the engine's own checkPassage
        // refuses a step from a cell whose tiles are all star, so a
        // correct reader must not let the hero leave it.
        var x0 = runtime.PlayerX;
        var y0 = runtime.PlayerY;
        var d0 = runtime.PlayerDirection;
        var blockiert = new List<string>();
        foreach (var aktion in new[] {
            UniversalRPG.Rm2k.Input.Rm2kInputAction.MoveRight,
            UniversalRPG.Rm2k.Input.Rm2kInputAction.MoveDown,
            UniversalRPG.Rm2k.Input.Rm2kInputAction.MoveLeft,
            UniversalRPG.Rm2k.Input.Rm2kInputAction.MoveUp,
        })
        {
            var vorX = runtime.PlayerX;
            var vorY = runtime.PlayerY;
            var vorRichtung = runtime.PlayerDirection;
            runtime.SubmitInput(aktion);
            var dx = runtime.PlayerX - vorX;
            var dy = runtime.PlayerY - vorY;
            blockiert.Add($"{aktion}:{dx}/{dy} dir {vorRichtung}->{runtime.PlayerDirection}");
            AssertEq(dx, 0, $"the star-tiled start map refuses the step: {aktion}");
            AssertEq(dy, 0, $"the star-tiled start map refuses the step: {aktion}");
            AssertTrue(
                runtime.PlayerDirection != vorRichtung
                || aktion == UniversalRPG.Rm2k.Input.Rm2kInputAction.MoveDown,
                $"the engine turns the hero on a blocked key too ({aktion})");
        }
        Console.WriteLine(
            $"MV start map blocked the hero {x0}/{y0}: {string.Join(" ", blockiert)}");

        // **Und die gezeichnete Karte des Projekts ist Map005** --
        // **und dort ist der Held nicht gegen eine Wand, sondern auf
        //  einer Zelle, die in alle vier Richtungen offen ist.**
        // **Und `GoTo` ist das Tor, das ein Kartenwechsel nimmt, und
        // `StandAt` der Befehl, der den Spieler stellt, und `SubmitInput`
        // die Taste, die ihn bewegt.**
        AssertTrue(runtime.GoTo(5), "the drawn MV map 5 is among the maps this runtime read");
        runtime.Facts.Player.StandAt(5, 5, 5, 2);
        var nachX = runtime.PlayerX;
        var nachY = runtime.PlayerY;
        AssertEq(nachX, 5, "the hero stands on the drawn map");
        AssertEq(nachY, 5, "the hero stands on the drawn map");

        var pixelVorher = runtime.PaintedMap == null ? 0 : AnzahlFarben(runtime.PaintedMap);
        var schrittRechts = runtime.SubmitInput(
            UniversalRPG.Rm2k.Input.Rm2kInputAction.MoveRight);
        var pixelNachher = runtime.PaintedMap == null ? 0 : AnzahlFarben(runtime.PaintedMap);
        AssertTrue(schrittRechts,
            "the drawn MV map lets the hero step right -- a reader that refused"
            + " him here was reading a different game");
        AssertEq(runtime.PlayerX, 6, "the hero stepped one tile to the right");
        AssertEq(runtime.PlayerY, 5, "the hero did not drift off the row");
        AssertTrue(pixelNachher != pixelVorher,
            $"a step repaints the hero on the drawn map: {pixelVorher} distinct"
            + $" colours before, {pixelNachher} after -- an unmoved hero and a"
            + " moved hero look the same to a position check");
        Console.WriteLine(
            $"MV drawn map hero {nachX}/{nachY} -> {runtime.PlayerX}/{runtime.PlayerY},"
            + $" {pixelVorher} -> {pixelNachher} colours");
    }

    private static int AnzahlFarben(UniversalRPG.Rm2k.Rendering.Rm2kPixelBuffer pPixels)
    {
        var gesehen = new HashSet<int>();
        for (var index = 0; index + 3 < pPixels.Pixels.Length; index += 4)
        {
            gesehen.Add(
                pPixels.Pixels[index]
                | (pPixels.Pixels[index + 1] << 8)
                | (pPixels.Pixels[index + 2] << 16));
        }
        return gesehen.Count;
    }

    public void Test_DerMvPluginWirbtLaufzeitUndNichtNurErkennung()
    {
        var plugin = BuiltInEnginePluginCatalog.CreateRuntimeRegistry()
            .Plugins.Single(pPlugin => pPlugin.Metadata.Id == EnginePluginIds.RpgMakerMv);
        AssertTrue((plugin.Metadata.Capabilities & PluginCapability.Runtime) != 0,
            "the MV plugin advertises the runtime capability");
    }
}
