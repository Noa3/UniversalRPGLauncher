using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Godot;
using UniversalRPG.Plugins;
using UniversalRPG.Rm2k;
using UniversalRPG.Rm2k.Rendering;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>Controlled event pages on a real parsed map and pinned real chipset/charset.</summary>
public partial class TestRm2kActiveEventGraphic : TestBase
{
    private const string Fixture = "res://tests/fixtures/easyrpg-testgame/rm2000";
    private string _root = "";
    private EnginePluginHost _host = null!;
    private Rm2kEngineRuntime _runtime = null!;
    private Rm2kMap.Event _event = null!;

    public override void Setup()
    {
        _root = ProjectSettings.GlobalizePath("user://active-graphic-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
        foreach (var file in new[] { "RPG_RT.ldb", "RPG_RT.lmt", "Map0001.lmu", "CharSet/Chara1.png", "ChipSet/World.png" })
        {
            var destination = Path.Combine(_root, file);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(ProjectSettings.GlobalizePath(Fixture + "/" + file), destination);
        }
        _host = new EnginePluginHost(BuiltInEnginePluginCatalog.CreateRuntimeRegistry());
        var started = _host.Start(new PluginGameInfo
        {
            GameDirectory = _root, EngineId = EnginePluginIds.RpgMaker2000, Generation = "rm2k", DetectorScore = 3
        });
        AssertTrue(started.Success, $"The pinned native runtime starts: {started.Error?.Message}");
        _runtime = (Rm2kEngineRuntime)_host.Runtime!;
        // Only the control event changes; no test-only production graphics API is added.
        var events = (List<Rm2kMap.Event>)typeof(Rm2kEngineRuntime)
            .GetField("_mapEvents", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_runtime)!;
        events.Clear();
        _event = new Rm2kMap.Event(900, 8, 8) { Direction = 6, AnimationFrame = 2 };
        events.Add(_event);
        _runtime.EventScheduler.SetEvents(events);
    }

    public override void Teardown()
    {
        _host?.Dispose();
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }

    public void Test_HighestEligiblePageOwnsGraphicIndexLayerAndRealPixels()
    {
        _event.Pages.Add(Page("Chara1", 0, 0));
        _event.Pages.Add(Page("Chara1", 3, 2));
        Repaint();
        var sprite = EventSprite();
        AssertTrue(sprite != null, "The active event contributes a sprite");
        AssertEq(sprite!.CharacterIndex, 3, "Highest eligible page owns the charset cell, not the first graphic-bearing page");
        AssertEq(sprite.Stage, Rm2kMapFrameRenderer.SpriteStage.AboveLayer, "The active page owns the draw layer");
        AssertEq(sprite.FacingDirection, (byte)6, "Graphics refresh retains live facing");
        AssertEq(sprite.Frame, 2, "Graphics refresh retains live animation frame");
        AssertPinnedCellPixels(3);
    }

    public void Test_StationaryConditionChangeRepaintsThroughTheRealUpdatePath()
    {
        _event.Pages.Add(Page("Chara1", 0, 2));
        var higher = Page("Chara1", 3, 2);
        higher.Conditions["switch_a_enabled"] = true;
        higher.Conditions["switch_a_id"] = 1;
        _event.Pages.Add(higher);
        while (_runtime.Simulation.Switches.Count < 1) _runtime.Simulation.Switches.Add(false);
        _runtime.Simulation.Switches[0] = false;
        Repaint();
        var original = _runtime.RenderedMap!;
        AssertEq(EventSprite()!.CharacterIndex, 0, "An unmet higher condition leaves the lower page active");
        AssertTrue(_runtime.Update(1.0 / 60.0).Success);
        AssertTrue(ReferenceEquals(_runtime.RenderedMap, original), "An unchanged idle page does not allocate a fresh frame");
        _runtime.Simulation.Switches[0] = true;
        AssertTrue(_runtime.Update(1.0 / 60.0).Success);
        AssertEq(EventSprite()!.CharacterIndex, 3, "A stationary switch change reaches the real renderer without test dirty hooks");
        AssertFalse(ReferenceEquals(_runtime.RenderedMap, original), "The visual page change invalidates the composed frame");
        AssertPinnedCellPixels(3);
        _runtime.Simulation.Switches[0] = false;
        AssertTrue(_runtime.Update(1.0 / 60.0).Success);
        AssertEq(EventSprite()!.CharacterIndex, 0, "Returning to the original condition restores the lower page");
        AssertPinnedCellPixels(0);
        AssertEq(_event.X, 8, "Page refresh does not reset event position");
        AssertEq(_event.Direction, (byte)6, "Page refresh does not reset live facing");
        AssertEq(_event.AnimationFrame, 2, "Page refresh does not reset live pose");
    }

    public void Test_BlankActivePageHidesLowerGraphic()
    {
        _event.Pages.Add(Page("Chara1", 0, 2));
        Repaint();
        var visible = (byte[])_runtime.RenderedMap!.Pixels.Clone();
        _event.Pages.Add(Page("", 0, 2));
        Repaint();
        AssertTrue(EventSprite() == null, "A blank higher page does not reveal a hidden lower graphic");
        AssertFalse(visible.SequenceEqual(_runtime.RenderedMap!.Pixels), "Hiding the real graphic changes rendered pixels");
    }

    public void Test_MissingActiveCharsetIsDiagnosedWithoutLowerFallback()
    {
        _event.Pages.Add(Page("Chara1", 0, 2));
        _event.Pages.Add(Page("MissingActiveCharset", 3, 2));
        Repaint();
        AssertTrue(EventSprite() == null, "A missing active charset does not reveal a hidden available one");
        AssertTrue(_runtime.Simulation.Diagnostics.Any(message => message.Contains("event 900")
            && message.Contains("MissingActiveCharset")), "The missing active graphic names the event and asset");
    }

    public void Test_NoEligiblePageDrawsNothingAndBecomesVisibleWhenEligible()
    {
        var higher = Page("Chara1", 3, 2);
        higher.Trigger = 4; // Graphics selection is independent of execution role.
        higher.Conditions["switch_a_enabled"] = true;
        higher.Conditions["switch_a_id"] = 1;
        _event.Pages.Add(higher);
        while (_runtime.Simulation.Switches.Count < 1) _runtime.Simulation.Switches.Add(false);
        _runtime.Simulation.Switches[0] = false;
        Repaint();
        AssertTrue(EventSprite() == null, "No eligible page contributes no graphic");
        _runtime.Simulation.Switches[0] = true;
        AssertTrue(_runtime.Update(1.0 / 60.0).Success);
        var sprite = EventSprite();
        AssertTrue(sprite != null, "A previously invisible parallel page repaints when it becomes eligible");
        if (sprite == null) return;
        AssertEq(sprite.CharacterIndex, 3, "The newly eligible page owns the graphic cell");
        AssertPinnedCellPixels(3);
    }

    public void Test_GraphicRefreshPreservesAnEventMidRoute()
    {
        var lower = Page("Chara1", 0, 2);
        lower.MoveRouteCommands.Add(new Rm2kMap.MoveCommand(UniversalRPG.Rm2k.Simulation.Rm2kMoveRoute.MoveRight));
        lower.MoveRouteRepeat = false;
        _event.Pages.Add(lower);
        var higher = Page("Chara1", 3, 2);
        higher.Conditions["switch_a_enabled"] = true;
        higher.Conditions["switch_a_id"] = 1;
        _event.Pages.Add(higher);
        for (var index = 0; index < _runtime.Simulation.PassabilityMasks.Count; index++)
            _runtime.Simulation.PassabilityMasks[index] = 15;
        while (_runtime.Simulation.Switches.Count < 1) _runtime.Simulation.Switches.Add(false);
        _runtime.Simulation.Switches[0] = false;
        Repaint();
        AssertEq(_runtime.StartEventMoveRouteForTest(900), 1);
        AssertTrue(_runtime.Update(1.0 / 60.0).Success);
        AssertEq(_event.X, 9, "The real route begins its eastward step");
        var remaining = _runtime.EventRemainingStepForTest(900);
        AssertTrue(remaining > 0, "The event is genuinely mid-step before the page changes");
        var route = ((System.Collections.IDictionary)typeof(Rm2kEngineRuntime)
            .GetField("_eventRoutes", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_runtime)!)[900];
        _runtime.Simulation.Switches[0] = true;
        AssertTrue(_runtime.Update(1.0 / 60.0).Success);
        AssertEq(EventSprite()!.CharacterIndex, 3, "The new graphic is composed during the running route");
        AssertEq(_event.X, 9, "Page change does not teleport the event back to its origin");
        AssertEq(_event.Direction, (byte)6);
        AssertEq(_event.AnimationFrame, 2);
        AssertTrue(_runtime.EventRemainingStepForTest(900) > 0
            && _runtime.EventRemainingStepForTest(900) < remaining, "The existing step budget advances instead of resetting");
        AssertTrue(ReferenceEquals(route, ((System.Collections.IDictionary)typeof(Rm2kEngineRuntime)
            .GetField("_eventRoutes", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_runtime)!)[900]),
            "Graphics refresh retains the actual route instance");
    }

    private static Rm2kMap.EventPage Page(string name, int index, int layer) => new()
    {
        Trigger = 0, Layer = layer,
        Graphic = new Dictionary<string, object> { ["character_name"] = name, ["character_index"] = index }
    };

    private void Repaint()
    {
        _runtime.MarkFrameDirtyForTest();
        _runtime.RefreshFrameForTest();
    }

    private Rm2kCharacterSprite? EventSprite()
    {
        var sprites = (IReadOnlyList<Rm2kCharacterSprite>)typeof(Rm2kEngineRuntime)
            .GetProperty("_heroSpriteProbe", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_runtime)!;
        return sprites.SingleOrDefault(sprite => sprite.MapX == _event.X && sprite.MapY == _event.Y);
    }

    private void AssertPinnedCellPixels(int index)
    {
        AssertTrue(Rm2kIndexedImage.TryLoad(Path.Combine(_root, "CharSet", "Chara1.png"), out var image, out var error), error);
        // Independent geometry from pinned EasyRPG Sprite_Character: 72x128 cell,
        // 24x32 frame, row=right(1), frame=right(2), feet at tile bottom.
        var sourceX = (index % 4) * 72 + 2 * 24;
        var sourceY = (index / 4) * 128 + 1 * 32;
        // Game_Character::GetScreenX adds TILE_SIZE, not half a tile.
        var destinationX = _event.X * 16 + 16 - 12 + _runtime.AppliedCameraOffsetX;
        var destinationY = _event.Y * 16 + 16 - 32 + _runtime.AppliedCameraOffsetY;
        var frame = _runtime.RenderedMap!;
        var sampled = 0;
        var mismatches = 0;
        for (var y = 0; y < 32; y++)
        for (var x = 0; x < 24; x++)
        {
            var paletteIndex = image.Indices[(sourceY + y) * image.Width + sourceX + x];
            if (image.AlphaAt(paletteIndex) != 255) continue;
            sampled++;
            var color = image.Palette[paletteIndex];
            var offset = ((destinationY + y) * frame.Width + destinationX + x) * 4;
            if (frame.Pixels[offset] != color[0] || frame.Pixels[offset + 1] != color[1]
                || frame.Pixels[offset + 2] != color[2] || frame.Pixels[offset + 3] != 255) mismatches++;
        }
        AssertTrue(sampled > 0, "The real pinned cell has opaque pixels; a blank fixture cannot pass");
        AssertEq(mismatches, 0, $"The real composed frame contains the active cell's opaque pixels ({sampled} sampled)");
    }
}
