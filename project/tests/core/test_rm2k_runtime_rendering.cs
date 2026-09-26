using System;
using System.IO;
using Godot;
using UniversalRPG.Plugins;
using UniversalRPG.Rm2k.Rendering;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Verifies that the runtime turns a real RM2K game directory into rendered map
/// pixels, and that a missing or malformed chipset image is reported instead of
/// silently producing a blank map.
/// </summary>
public partial class TestRm2kRuntimeRendering : TestBase
{
    private const string FixtureRoot = "res://tests/fixtures/easyrpg-testgame";
    private const string ChipsetFixture = "rm2000/ChipSet/World.png";

    public void Test_RuntimeRendersTheRealMapFromARealChipset()
    {
        var gameDir = CopyRealGame(nameof(Test_RuntimeRendersTheRealMapFromARealChipset));
        if (gameDir == null)
        {
            return;
        }
        try
        {
            var created = CreateRuntime(gameDir);
            if (created == null)
            {
                return;
            }
            var runtime = created.Value.Runtime;
            AssertEq(runtime.State, PluginRuntimeState.Running, "the runtime starts");
            AssertEq(runtime.RenderedMap != null, true,
                $"the map rendered: {runtime.RenderDiagnostic}");
            if (runtime.RenderedMap == null)
            {
                return;
            }
            AssertEq(runtime.RenderedMap.Width, 20 * Rm2kChipsetBitmap.TileSize, "20 tiles wide");
            AssertEq(runtime.RenderedMap.Height, 15 * Rm2kChipsetBitmap.TileSize, "15 tiles high");
            AssertEq(runtime.ChipsetImage != null, true, "the chipset image is exposed");
            AssertEq(runtime.RenderDiagnostic, "", "a rendered map has no diagnostic");

            var opaque = 0;
            var colours = new System.Collections.Generic.HashSet<uint>();
            for (var index = 0; index < runtime.RenderedMap.Pixels.Length; index += 4)
            {
                if (runtime.RenderedMap.Pixels[index + 3] != 0)
                {
                    opaque++;
                }
                var packed = (uint)(runtime.RenderedMap.Pixels[index]
                    | (runtime.RenderedMap.Pixels[index + 1] << 8)
                    | (runtime.RenderedMap.Pixels[index + 2] << 16)
                    | (runtime.RenderedMap.Pixels[index + 3] << 24));
                colours.Add(packed);
            }
            AssertTrue(opaque > 0, "the rendered map has painted pixels");
            // The pinned room is covered by floor and wall tiles that fill every
            // pixel, so the meaningful check is that the image is not a flat fill.
            // The exact colour count is a property of the fixture, so it is only
            // reported, not asserted; the dump below is the real evidence.
            System.Console.WriteLine(
                $"RM2K rendered map: {runtime.RenderedMap.Width}x{runtime.RenderedMap.Height} " +
                $"colours={colours.Count} opaque={opaque}");
            var dump = Image.CreateFromData(
                runtime.RenderedMap.Width,
                runtime.RenderedMap.Height,
                false,
                Image.Format.Rgba8,
                runtime.RenderedMap.Pixels);
            if (dump != null)
            {
                dump.SavePng("user://rm2k-rendered-map.png");
            }

            // The tile id framebuffer keeps working next to the pixels.
            AssertEq(runtime.Framebuffer != null, true, "the tile id framebuffer still exists");
            AssertEq(runtime.Framebuffer!.Width, 20, "the framebuffer is 20 tiles wide");

            AssertRenderedMapMatchesGoldenImage(runtime.RenderedMap);
        }
        finally
        {
            Cleanup(gameDir);
        }
    }

    /// <summary>
    /// Compares the rendered map against the pinned golden image so a change in
    /// the chipset resolution, the autotile tables or the draw order shows up as
    /// a pixel difference instead of a silently different picture.
    /// </summary>
    private void AssertRenderedMapMatchesGoldenImage(Rm2kPixelBuffer pRenderedMap)
    {
        var goldenPath = ProjectSettings.GlobalizePath(FixtureRoot.PathJoin("rm2000/rendered/Map0001.png"));
        AssertTrue(File.Exists(goldenPath), $"the golden image exists: {goldenPath}");
        if (!File.Exists(goldenPath))
        {
            return;
        }
        var image = Image.LoadFromFile(goldenPath);
        AssertTrue(image != null, "the golden image loads");
        if (image == null)
        {
            return;
        }
        AssertEq(image.GetWidth(), pRenderedMap.Width, "the golden image width matches the frame");
        AssertEq(image.GetHeight(), pRenderedMap.Height, "the golden image height matches the frame");
        if (image.GetFormat() != Image.Format.Rgba8)
        {
            image.Convert(Image.Format.Rgba8);
        }
        var goldenPixels = image.GetData();
        if (goldenPixels == null || goldenPixels.Length != pRenderedMap.Pixels.Length)
        {
            AssertTrue(false, "the golden image exposes RGBA pixels of the same length");
            return;
        }
        var differences = 0;
        for (var index = 0; index < pRenderedMap.Pixels.Length; index++)
        {
            if (pRenderedMap.Pixels[index] != goldenPixels[index])
            {
                differences++;
            }
        }
        AssertEq(differences, 0,
            $"the rendered map matches the pinned golden image (differing bytes: {differences})");
    }

    public void Test_MissingChipsetImageIsReportedAndTheRuntimeKeepsRunning()
    {
        var gameDir = CopyRealGame(nameof(Test_MissingChipsetImageIsReportedAndTheRuntimeKeepsRunning));
        if (gameDir == null)
        {
            return;
        }
        try
        {
            // Remove only the image, keep the data files.
            var chipsetPath = Path.Combine(gameDir, "ChipSet", "World.png");
            File.Delete(chipsetPath);
            var created = CreateRuntime(gameDir);
            if (created == null)
            {
                return;
            }
            var runtime = created.Value.Runtime;
            AssertEq(runtime.State, PluginRuntimeState.Running,
                "a missing chipset image must not stop the runtime");
            AssertEq(runtime.RenderedMap == null, true, "no pixels without a chipset image");
            AssertTrue(runtime.RenderDiagnostic.Contains("World.png"),
                $"the diagnostic names the image but was '{runtime.RenderDiagnostic}'");
            AssertEq(runtime.Framebuffer != null, true, "the tile id framebuffer still works");
        }
        finally
        {
            Cleanup(gameDir);
        }
    }

    public void Test_MalformedChipsetImageIsReported()
    {
        var gameDir = CopyRealGame(nameof(Test_MalformedChipsetImageIsReported));
        if (gameDir == null)
        {
            return;
        }
        try
        {
            File.WriteAllBytes(Path.Combine(gameDir, "ChipSet", "World.png"), new byte[] { 1, 2, 3 });
            var created = CreateRuntime(gameDir);
            if (created == null)
            {
                return;
            }
            var runtime = created.Value.Runtime;
            AssertEq(runtime.State, PluginRuntimeState.Running, "a broken image must not stop the runtime");
            AssertEq(runtime.RenderedMap == null, true, "no pixels from a broken image");
            AssertTrue(runtime.RenderDiagnostic.Contains("decoded") || runtime.RenderDiagnostic.Contains("PNG"),
                $"the diagnostic explains the failure but was '{runtime.RenderDiagnostic}'");
        }
        finally
        {
            Cleanup(gameDir);
        }
    }

    public void Test_StoppingClearsTheRenderedMap()
    {
        var gameDir = CopyRealGame(nameof(Test_StoppingClearsTheRenderedMap));
        if (gameDir == null)
        {
            return;
        }
        try
        {
            var created = CreateRuntime(gameDir);
            if (created == null)
            {
                return;
            }
            var runtime = created.Value.Runtime;
            AssertEq(runtime.RenderedMap != null, true, "the map rendered before stopping");
            created.Value.Host.Stop();
            AssertEq(runtime.RenderedMap == null, true, "stopping clears the rendered map");
            AssertEq(runtime.ChipsetImage == null, true, "stopping clears the chipset image");
        }
        finally
        {
            Cleanup(gameDir);
        }
    }

    /// <summary>Copies the pinned RM2000 fixture plus its chipset into a temp game dir.</summary>
    private string? CopyRealGame(string pName)
    {
        var ldb = ProjectSettings.GlobalizePath(FixtureRoot.PathJoin("rm2000/RPG_RT.ldb"));
        if (!File.Exists(ldb))
        {
            AssertTrue(false, $"the pinned LDB exists: {ldb}");
            return null;
        }
        var chipset = ProjectSettings.GlobalizePath(FixtureRoot.PathJoin(ChipsetFixture));
        var gameDir = ProjectSettings.GlobalizePath($"user://rm2k-render-{pName}");
        try
        {
            if (DirAccess.DirExistsAbsolute(gameDir))
            {
                DirAccess.RemoveAbsolute(gameDir);
            }
            DirAccess.MakeDirRecursiveAbsolute(gameDir);
            File.Copy(ldb, Path.Combine(gameDir, "RPG_RT.ldb"), true);
            File.Copy(
                ProjectSettings.GlobalizePath(FixtureRoot.PathJoin("rm2000/RPG_RT.lmt")),
                Path.Combine(gameDir, "RPG_RT.lmt"), true);
            File.Copy(
                ProjectSettings.GlobalizePath(FixtureRoot.PathJoin("rm2000/Map0001.lmu")),
                Path.Combine(gameDir, "Map0001.lmu"), true);
            if (File.Exists(chipset))
            {
                DirAccess.MakeDirRecursiveAbsolute(Path.Combine(gameDir, "ChipSet"));
                File.Copy(chipset, Path.Combine(gameDir, "ChipSet", "World.png"), true);
            }
            return gameDir;
        }
        catch (IOException exception)
        {
            AssertTrue(false, $"the fixture copy failed: {exception.Message}");
            return null;
        }
    }

    private (EnginePluginHost Host, Rm2kEngineRuntime Runtime)? CreateRuntime(string pGameDir)
    {
        var game = new PluginGameInfo
        {
            GameDirectory = pGameDir,
            EngineId = EnginePluginIds.RpgMaker2000,
            Generation = "rm2k",
            DetectorScore = 3,
        };
        var host = new EnginePluginHost(BuiltInEnginePluginCatalog.CreateRuntimeRegistry());
        var started = host.Start(game);
        AssertTrue(started.Success, $"the runtime starts: {started.Error?.Message ?? "no error"}");
        if (host.Runtime is not Rm2kEngineRuntime runtime)
        {
            AssertTrue(false, "the host created an RM2K runtime");
            return null;
        }
        return (host, runtime);
    }

    private static void Cleanup(string pGameDir)
    {
        if (DirAccess.DirExistsAbsolute(pGameDir))
        {
            DirAccess.RemoveAbsolute(pGameDir);
        }
    }
}
