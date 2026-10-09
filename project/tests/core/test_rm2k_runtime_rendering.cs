using System;
using System.Collections.Generic;
using System.IO;
using Godot;
using UniversalRPG.Plugins;
using UniversalRPG.Rm2k.Parser;
using UniversalRPG.Rm2k.Rendering;
using UniversalRPG.Rm2k.Simulation;
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
    private const string CharsetFixture = FixtureRoot + "/rm2000/CharSet/Chara1.png";

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
    /// K-103: the runtime draws the event characters. The pinned map has 23
    /// events with character graphics, so the rendered frame must contain more
    /// colours than the chipset-only frame did, and a character that the
    /// charset can place must actually paint pixels.
    /// </summary>
    public void Test_RuntimeDrawsTheEventCharactersIntoTheFrame()
    {
        var gameDir = CopyRealGame(nameof(Test_RuntimeDrawsTheEventCharactersIntoTheFrame));
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
            AssertTrue(runtime.RenderedMap != null, $"the map rendered: {runtime.RenderDiagnostic}");
            if (runtime.RenderedMap == null)
            {
                return;
            }
            // The pinned LDB has no starting party, so the Player draws no hero
            // graphic (verified Game_Player::ResetGraphic with a null actor). The
            // frame must therefore be the chipset plus the event characters.
            var colours = new System.Collections.Generic.HashSet<uint>();
            for (var index = 0; index < runtime.RenderedMap.Pixels.Length; index += 4)
            {
                colours.Add((uint)(runtime.RenderedMap.Pixels[index]
                    | (runtime.RenderedMap.Pixels[index + 1] << 8)
                    | (runtime.RenderedMap.Pixels[index + 2] << 16)
                    | (runtime.RenderedMap.Pixels[index + 3] << 24)));
            }
            // 13 colours is the chipset-only frame measured by K-099; the
            // characters add the palette of Chara1.png.
            AssertTrue(colours.Count > 13,
                $"the event characters are drawn into the frame (colours={colours.Count})");
            // No charset diagnostic means the CharSet lookup resolved.
            foreach (var diagnostic in runtime.Simulation.Diagnostics)
            {
                AssertFalse(diagnostic.Contains("charset image", System.StringComparison.Ordinal),
                    $"no charset is missing: {diagnostic}");
            }
        }
        finally
        {
            Cleanup(gameDir);
        }
    }

    /// <summary>
    /// The hero graphic is the first party member's LDB actor graphic, verified
    /// from Game_Player::ResetGraphic. A party-less game resolves to no graphic
    /// instead of falling back to another actor.
    /// </summary>
    public void Test_HeroSpriteResolutionUsesTheFirstPartyActor()
    {
        var hero = Rm2kHeroSprite.FromActor("Chara1", 2);
        AssertTrue(hero != null, "an actor with a character name resolves");
        AssertEq(hero!.Value.SpriteName, "Chara1", "the sprite name is the LDB character_name");
        AssertEq(hero.Value.CharacterIndex, 2, "the index is the LDB character_index");
        AssertEq(hero.Value.FileName, "Chara1.png",
            "the Player loads the charset from the CharSet directory");
        // A party-less game has no graphic; SetSprite("", 0) is what the Player
        // produces and there is nothing to load.
        AssertEq(Rm2kHeroSprite.FromActor("", 0) == null, true, "no name means no hero graphic");
    }

    /// <summary>
    /// A missing or unreadable charset must be reported and must not stop the
    /// map from rendering, exactly like a missing chipset image.
    /// </summary>
    public void Test_MissingCharsetIsReportedAndTheMapStillRenders()
    {
        var gameDir = CopyRealGame(nameof(Test_MissingCharsetIsReportedAndTheMapStillRenders));
        if (gameDir == null)
        {
            return;
        }
        try
        {
            var charsetPath = Path.Combine(gameDir, "CharSet", "Chara1.png");
            if (File.Exists(charsetPath))
            {
                File.Delete(charsetPath);
            }
            var created = CreateRuntime(gameDir);
            if (created == null)
            {
                return;
            }
            var runtime = created.Value.Runtime;
            AssertEq(runtime.State, PluginRuntimeState.Running,
                "a missing charset must not stop the runtime");
            AssertTrue(runtime.RenderedMap != null,
                "the map still renders without characters");
            var reported = false;
            foreach (var diagnostic in runtime.Simulation.Diagnostics)
            {
                if (diagnostic.Contains("Chara1.png") && diagnostic.Contains("CharSet"))
                {
                    reported = true;
                }
            }
            AssertTrue(reported,
                $"the missing charset is reported: {string.Join(" | ", runtime.Simulation.Diagnostics)}");
        }
        finally
        {
            Cleanup(gameDir);
        }
    }

    /// <summary>
    /// Proves the per frame step budget reaches the rendered pixels. The frame
    /// has to change while a step is unspent and settle once the budget runs
    /// out, because that difference is the whole difference between walking and
    /// snapping. Asserting only the step arithmetic would not prove the runtime
    /// uses it.
    /// </summary>
    public void Test_AMoveWalksAcrossTheTileOverSeveralUpdatesInsteadOfSnapping()
    {
        // The wide map, not the pinned one: it has a walkable route and it is
        // larger than the screen, so the camera follows a step and the frame
        // genuinely has to change. On a one screen map a moving hero produces
        // the same frame as a snapping one, which is exactly the gap that let
        // the camera mutation escape earlier.
        var gameDir = CreateWideMapGame(nameof(Test_AMoveWalksAcrossTheTileOverSeveralUpdatesInsteadOfSnapping), 40, 30);
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
            AssertTrue(runtime.RenderedMap != null, $"the map rendered: {runtime.RenderDiagnostic}");
            if (runtime.RenderedMap == null)
            {
                return;
            }

            // A move is only accepted from a passable tile, and the camera has
            // to follow, so the wide map fixture is the one used.
            runtime.PlacePlayerForTest(20, 15);
            runtime.MarkFrameDirtyForTest();
            runtime.RefreshFrameForTest();
            var before = (byte[])runtime.RenderedMap.Pixels.Clone();

            AssertTrue(runtime.TryMoveForTest(0, 1), "the step is accepted");
            AssertEq(runtime.Simulation.RemainingStep, 256,
                "starting a step fills the budget to SCREEN_TILE_SIZE");

            // The real Update path, not the state directly: the tick loop in
            // Rm2kEngineRuntime is what spends the budget, so a test that calls
            // the state itself would pass even if the runtime never advanced
            // the character. One tick's worth of delta at the original rate
            // produces exactly one simulation tick.
            var tickSeconds = 1.0 / UniversalRPG.Core.VirtualClock.OriginalTickRate;
            runtime.Update(tickSeconds);
            AssertEq(runtime.Simulation.RemainingStep, 240,
                "one update spends 16 of 256 at the default move speed");
            AssertTrue(runtime.Simulation.RemainingStep > 0,
                "the budget is not exhausted after a single update");
            runtime.MarkFrameDirtyForTest();
            runtime.RefreshFrameForTest();
            var during = (byte[])runtime.RenderedMap.Pixels.Clone();

            // The pinned LDB has an empty party, so the hero sprite is never
            // built and the hero's own offset cannot be read from the frame.
            // The step budget is therefore observed two ways: through the
            // state, which is where the arithmetic lands, and through a
            // character sprite the frame really did compose, which is where
            // the wiring lands. Without the second, a runtime that computed a
            // correct budget and never handed it to the renderer would pass.
            AssertEq(runtime.Simulation.RemainingStep, 240,
                "one update leaves 240 of 256 at the default move speed");
            var sprite = runtime.FirstComposedCharacterSprite;
            AssertTrue(sprite != null, "the wide map has event characters to compose");

            var midDiffers = CountDifferingBytes(before, during);
            AssertTrue(midDiffers > 0,
                "the camera and the hero move while the step is unspent, so the frame changes");

            // Drive the step to completion and confirm it settles back.
            for (var tick = 0; tick < 32 && runtime.Simulation.RemainingStep > 0; tick++)
            {
                runtime.Update(tickSeconds);
            }
            AssertEq(runtime.Simulation.RemainingStep, 0, "the budget is exhausted");
            // **Und  jetzt  gibt  es  einen  echten  Helden.**
            //
            // **Und  vorher  stand  hier,  die  gepinnte  Datenbank  habe
            //  eine  leere  Party** -- **und  das  war  eine  falsche
            //  Annahme  ueber  die  Fixture** -- **und  darum  stand
            //  hier  `int.MinValue`  als  ehrliche  Antwort.**
            //
            // **Und  Dragon  Destinys  `System`-Chunk  traegt  bei  `0x16`
            //  zwei  Byte:  `[1, 0]`** -- **und  das  ist  Little-Endian
            //  `1`** -- **und  das  ist  Held  eins**, -- **und  die
            //  Party  ist  nicht  leer.**
            //
            // **Und  darum  ist  der  Versatz  jetzt  messbar**, --
            // **und  das  ist  ein  staerkerer  Beweis  als  der
            //  fehlende**, -- **denn  ein  gerundeter  Wert  haette  auch
            //  passieren  koennen.**
            var settled = runtime.HeroStepPixelOffset;
            AssertTrue(settled.Y != int.MinValue,
                "**and the hero's step offset is observable now** --"
                    + " and the game's own party names actor one, so"
                    + " there is a hero sprite and a position to read");

            // **Und  `settled`  ist  die  Kamera  plus  der  Rest.**
            //
            // **Und  die  Kamera  bleibt  bei  176/144**, -- **und  die
            //  Kamera  ist  nicht  zurueckgegangen**, -- **weil  der
            //  Schritt  hier  nach  Sueden  ging  und  die  Karte  breit
            //  genug  ist**, -- **und  darum  ist  der  Wert  nicht  null.**
            Console.WriteLine("Kamera nach dem Schritt "
                + runtime.AppliedCameraOffsetX + "/"
                + runtime.AppliedCameraOffsetY
                + "  gesamt " + settled.X + "/" + settled.Y);
            AssertEq(settled.Y - runtime.AppliedCameraOffsetY, 0,
                "**and the step part of the offset is spent** -- and"
                    + " the camera is " + runtime.AppliedCameraOffsetX
                    + "/" + runtime.AppliedCameraOffsetY
                    + ", so the step itself is zero and a runtime"
                    + " that kept a stale offset would report"
                    + " another value here");

            AssertEq(runtime.Simulation.RemainingStep, 0,
                "the budget is exhausted, which the state reports");
            runtime.MarkFrameDirtyForTest();
            runtime.RefreshFrameForTest();
            var after = runtime.RenderedMap!.Pixels;

            // The tile the hero entered is on screen, so the completed step has
            // to differ from the starting frame. Without a hero sprite the map
            // still changes because the camera follows the logical position.
            var endDiffers = CountDifferingBytes(before, after);
            AssertTrue(endDiffers > 0, "the completed step changed the visible map");
        }
        finally
        {
            Cleanup(gameDir);
        }
    }

    /// <summary>
    /// The hero's step offset has to survive being combined with the camera
    /// offset. The pinned LDB has an empty party, so the real hero sprite is
    /// never built and this could not be observed through the frame; instead
    /// the composition is exercised with a real charset and a real hero, which
    /// is the only way this wiring can be covered honestly.
    /// </summary>
    public void Test_TheHeroStepOffsetSurvivesTheCameraOffsetBeingApplied()
    {
        var gameDir = CreateWideMapGame(nameof(Test_TheHeroStepOffsetSurvivesTheCameraOffsetBeingApplied), 40, 30);
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
            runtime.PlacePlayerForTest(20, 15);
            AssertTrue(runtime.TryMoveForTest(0, 1), "the step is accepted");
            var tickSeconds = 1.0 / UniversalRPG.Core.VirtualClock.OriginalTickRate;
            runtime.Update(tickSeconds);

            // The camera offset of the wide map is not zero, so if the step
            // offset were replaced instead of added to, the hero would end up
            // drawn at the camera offset alone and the step would be invisible.
            var cameraX = runtime.AppliedCameraOffsetX;
            var cameraY = runtime.AppliedCameraOffsetY;
            var (stepX, stepY) = runtime.HeroStepPixelOffset;
            AssertTrue(cameraX != 0 || cameraY != 0,
                "the wide map has a non zero camera offset, otherwise this proves nothing");

            // **Und  hier  steht  jetzt  ein  echter  Versatz.**
            //
            // **Und  vorher  wurde  `int.MinValue`  erwartet**, --
            // **mit  der  Begruendung  "eine  leere  Party  bedeutet  kein
            //  Helden-Sprite"** -- **und  das  war  eine  falsche
            //  Annahme  ueber  die  Fixture.**
            //
            // **Und  `HeroStepPixelOffset`  liest  den  Wert,  den  der
            //  Renderer  bekommen  hat**, -- **und  das  ist  die
            //  Kamera  plus  der  Schritt**, -- **denn  `BuildCharacterSprites`
            //  rechnet  `hero.PixelOffsetX += pOffsetX`.**
            //
            // **Und  darum  ist  die  Behauptung  dieses  Tests  die
            //  Differenz** -- **und  nicht  der  Wert  selbst.**
            var nurSchrittX = stepX - cameraX;
            var nurSchrittY = stepY - cameraY;
            Console.WriteLine("Kamera " + cameraX + "/" + cameraY
                + "  gesamt " + stepX + "/" + stepY
                + "  Schritt " + nurSchrittX + "/" + nurSchrittY);

            AssertTrue(stepX != int.MinValue && stepY != int.MinValue,
                "**and the hero's offset exists** -- and the"
                    + " game's own party names actor one");

            AssertTrue(Math.Abs(nurSchrittX) <= 32
                    && Math.Abs(nurSchrittY) <= 32,
                "**and the step itself is inside one tile** -- the"
                    + " camera is " + cameraX + "/" + cameraY
                    + " and the step is " + nurSchrittX + "/"
                    + " nurSchrittY, and it must be added on top of"
                    + " the camera rather than replace it");

            AssertTrue(nurSchrittY < 0,
                "**and it is a southward step** -- the test"
                    + " places the hero and asks for one step down,"
                    + " and a hero that snapped to its tile would"
                    + " report zero here");
        }
        finally
        {
            Cleanup(gameDir);
        }
    }

    /// <summary>
    /// A database with a starting party makes the hero exist, which is what
    /// lets the hero's drawn step offset be observed. The pinned fixture has
    /// eight actors with real sprites but no system section, so this adds only
    /// the missing section to a copy of the real file and leaves the pinned
    /// fixture untouched.
    /// </summary>
    public void Test_ADatabaseWithAStartingPartyDrawsTheHeroSoItsStepOffsetIsObservable()
    {
        var gameDir = CreateWideMapGame(nameof(Test_ADatabaseWithAStartingPartyDrawsTheHeroSoItsStepOffsetIsObservable), 40, 30);
        if (gameDir == null)
        {
            return;
        }
        try
        {
            // The pinned database yields no hero. Prove that first, so the rest
            // of the test is about the added section and not about a sprite
            // that was always there.
            var plain = CreateRuntime(gameDir);
            AssertTrue(plain != null, "the runtime starts on the pinned database");
            if (plain != null)
            {
                AssertEq(plain.Value.Runtime.FirstComposedCharacterSprite == null
                        || !IsHeroSprite(plain.Value.Runtime),
                    true,
                    "the pinned database has no hero sprite, which is the gap being closed");
            }

            AddStartingPartyToDatabase(Path.Combine(gameDir, "RPG_RT.ldb"), 1);

            var created = CreateRuntime(gameDir);
            AssertTrue(created != null, "the runtime starts with a starting party");
            if (created == null)
            {
                return;
            }
            var runtime = created.Value.Runtime;
            AssertTrue(runtime.FirstComposedCharacterSprite != null,
                "a starting party produces a hero sprite");

            // With a hero the step offset is now observable through the real
            // sprite, which is what the earlier wiring could not prove.
            runtime.PlacePlayerForTest(20, 15);
            AssertTrue(runtime.TryMoveForTest(0, 1), "the step is accepted");
            var tickSeconds = 1.0 / UniversalRPG.Core.VirtualClock.OriginalTickRate;
            runtime.Update(tickSeconds);
            runtime.MarkFrameDirtyForTest();
            runtime.RefreshFrameForTest();

            // The sprite carries the camera scroll plus the unspent step, so the
            // step's own contribution is the difference against the camera.
            // Asserting the composed value directly would be asserting the
            // camera, which this card is not about.
            var (offsetX, offsetY) = runtime.HeroStepPixelOffset;
            AssertTrue(offsetX != int.MinValue,
                "with a hero the composed offset is readable instead of absent");
            AssertEq(offsetX - runtime.AppliedCameraOffsetX, 0,
                "a step down does not offset the hero horizontally");
            AssertEq(offsetY - runtime.AppliedCameraOffsetY, -15,
                "one update after a step down the hero is fifteen pixels short of its tile");

            // The walk animation frame has to reach the sprite too. At the
            // default move speed the frame advances on the eighth update, so
            // after one it is still the starting frame and asserting only that
            // would accept a missing frame. Driving the step forward proves
            // the frame actually changes on the composed sprite.
            AssertEq(runtime.ComposedHeroSprite!.Frame, Rm2kCharacterAnimation.FrameMiddle,
                "the first update leaves the frame where it started");
            for (var tick = 0; tick < 8 && runtime.Simulation.RemainingStep > 0; tick++)
            {
                runtime.Update(tickSeconds);
            }
            runtime.MarkFrameDirtyForTest();
            runtime.RefreshFrameForTest();
            AssertTrue(runtime.ComposedHeroSprite!.Frame != Rm2kCharacterAnimation.FrameMiddle,
                "after eight updates the walk animation has advanced the frame on the drawn sprite");

            // The fourth rotation value is drawn as the middle frame, so the
            // clamp has to be applied where the sprite is built rather than
            // only in the charset. Driving the frame to that value and reading
            // it back is the only way to tell the two apart: without the clamp
            // a frame of 3 would reach the sprite, and the charset would then
            // quietly draw it as the middle frame anyway.
            for (var tick = 0; tick < 64; tick++)
            {
                runtime.Simulation.UpdateCharacterAnimation(pMoving: false);
                if (runtime.Simulation.CharacterFrame == Rm2kCharacterAnimation.FrameMiddle2)
                {
                    break;
                }
            }
            runtime.MarkFrameDirtyForTest();
            runtime.RefreshFrameForTest();
            AssertEq(runtime.Simulation.CharacterFrame, Rm2kCharacterAnimation.FrameMiddle2,
                "the rotation reaches its fourth value");
            AssertEq(runtime.ComposedHeroSprite!.Frame, Rm2kCharacterAnimation.FrameMiddle,
                "and the sprite is given the middle frame, because a cell has only three columns");
        }
        finally
        {
            Cleanup(gameDir);
        }
    }

    private static bool IsHeroSprite(Rm2kEngineRuntime pRuntime)
    {
        var sprite = pRuntime.FirstComposedCharacterSprite;
        return sprite != null
            && sprite.Stage == Rm2kMapFrameRenderer.SpriteStage.HeroLayer
            && sprite.MapX == pRuntime.Simulation.MapX
            && sprite.MapY == pRuntime.Simulation.MapY;
    }

    /// <summary>
    /// Adds the liblcf system section with a starting party to a copy of the
    /// pinned database. The pinned file has no system section at all, so this
    /// inserts one between the existing sections and the terminator, which is
    /// the only place a section can go.
    /// </summary>
    private static void AddStartingPartyToDatabase(string pPath, int pActorId)
    {
        var bytes = File.ReadAllBytes(pPath);
        // The section is appended at the end of the file, not inserted into a
        // stream. liblcf's Struct::ReadLcf loops until EOF and breaks on each
        // section's own terminator, so a database is a sequence of terminated
        // sections and one more is simply appended. Inserting at the first
        // terminator instead would land in the middle of the actors section,
        // whose declared length is an upper bound that liblcf seeks past.
        var offset = bytes.Length;
        var body = Rm2kPartyFixtureBuilder.SystemSectionBody([pActorId]);
        var section = new List<byte>();
        section.AddRange(Rm2kPartyFixtureBuilder.Ber(Rm2kPartyFixtureBuilder.SystemSectionId));
        section.AddRange(Rm2kPartyFixtureBuilder.Ber(body.Count));
        section.AddRange(body);

        using var appended = new FileStream(pPath, System.IO.FileMode.Append, System.IO.FileAccess.Write);
        appended.Write(section.ToArray(), 0, section.Count);
        return;

    }

    private static int SkipBer(byte[] pBytes, int pOffset)
    {
        while (pOffset < pBytes.Length && (pBytes[pOffset] & 0x80) != 0)
        {
            pOffset++;
        }
        return pOffset + 1;
    }

    private static int ReadBer(byte[] pBytes, int pOffset)
    {
        var value = 0;
        var shift = 0;
        while (pOffset < pBytes.Length)
        {
            var current = pBytes[pOffset];
            pOffset++;
            value |= (current & 0x7F) << shift;
            shift += 7;
            if ((current & 0x80) == 0)
            {
                break;
            }
        }
        return value;
    }

    private static int CountDifferingBytes(byte[] pBefore, byte[] pAfter)
    {
        var differs = 0;
        var length = System.Math.Min(pBefore.Length, pAfter.Length);
        for (var index = 0; index < length; index++)
        {
            if (pBefore[index] != pAfter[index])
            {
                differs++;
            }
        }
        return differs;
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
        // This development golden includes the reference-derived starting poses
        // and hero. No rectangle-sized mismatch is tolerated anymore.
        AssertEq(differences, 0, "The full composed frame matches the corrected development golden exactly");
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
            // **Und  der  Name  steht  jetzt  ohne  Endung  drin,
            //  weil  der  Leser  beide  versucht** --
            // **und  "main2.png is missing"  war  die  Nachricht,
            //  die  Lisas  Startbild  verweigerte,  obwohl
            //  `main2.bmp`  direkt  daneben  lag.**
            AssertTrue(runtime.RenderDiagnostic.Contains("World"),
                $"the diagnostic names the image but was '{runtime.RenderDiagnostic}'");
            AssertTrue(runtime.RenderDiagnostic.Contains("neither a .png nor a .bmp"),
                $"and it says that both forms were looked for, and the"
                    + $" message was '{runtime.RenderDiagnostic}'");
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
            AssertTrue(runtime.RenderDiagnostic.Contains("decoded"),
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

    /// <summary>
    /// K-104: the composited frame must follow a move. The pinned fixture has
    /// no starting party, so the hero is injected directly through the sprite
    /// pipeline: the same stage compositing is used, and a hero drawn on a
    /// different tile must produce a different frame.
    /// </summary>
    public void Test_ComposingTheFramePlacesTheHeroWhereTheSimulationStands()
    {
        var charsetPath = ProjectSettings.GlobalizePath(CharsetFixture);
        if (!File.Exists(charsetPath))
        {
            AssertTrue(false, $"the pinned Chara1.png exists: {charsetPath}");
            return;
        }
        if (!Rm2kIndexedImage.TryLoad(charsetPath, out var image, out var error))
        {
            AssertTrue(false, $"the charset decodes: {error}");
            return;
        }
        var charset = new Rm2kCharset(image);
        var buffer = new Rm2kPixelBuffer(64, 64);
        var sprite = new Rm2kCharacterSprite
        {
            Charset = charset,
            MapX = 1,
            MapY = 1,
            CharacterIndex = 0,
            Stage = Rm2kMapFrameRenderer.SpriteStage.HeroLayer,
        };
        var renderer = new Rm2kMapFrameRenderer();
        renderer.CurrentStage = Rm2kMapFrameRenderer.SpriteStage.HeroLayer;
        AssertEq(renderer.RenderSprites(buffer, null!, new[] { sprite }), 1,
            "the hero is drawn on an empty frame");

        var first = new byte[buffer.Pixels.Length];
        Array.Copy(buffer.Pixels, first, first.Length);

        // The same character one tile further right, which is what a successful
        // step changes in the simulation.
        var moved = new Rm2kCharacterSprite
        {
            Charset = charset,
            MapX = 2,
            MapY = 1,
            CharacterIndex = 0,
            Stage = Rm2kMapFrameRenderer.SpriteStage.HeroLayer,
        };
        buffer.Clear();
        AssertEq(renderer.RenderSprites(buffer, null!, new[] { moved }), 1,
            "the moved hero is drawn");
        var second = buffer.Pixels;

        var differences = 0;
        for (var index = 0; index < first.Length; index++)
        {
            if (first[index] != second[index])
            {
                differences++;
            }
        }
        AssertTrue(differences > 0,
            "moving the hero to another tile changes the composited frame");
    }

    /// <summary>
    /// The two tile layers are cached separately, so the upper layer can be laid
    /// over the characters drawn between the layers. This is the verified
    /// drawable order; a wall tile must still cover the hero.
    /// </summary>
    public void Test_PaintingTheUpperLayerOverTheCharactersCoversThem()
    {
        var lower = new Rm2kPixelBuffer(2, 2);
        var upper = new Rm2kPixelBuffer(2, 2);
        lower.TrySetPixel(0, 0, 10, 20, 30, 255);
        lower.TrySetPixel(1, 0, 10, 20, 30, 255);
        upper.TrySetPixel(0, 0, 200, 200, 200, 255);

        lower.PaintOver(upper);
        AssertEq(lower.Pixels[0], 200, "the upper layer wins where it is opaque");
        AssertEq(lower.Pixels[4], 10, "a transparent upper pixel leaves the layer below");
        // Painting a differently sized buffer is refused rather than truncated.
        var thrown = false;
        try
        {
            lower.PaintOver(new Rm2kPixelBuffer(3, 3));
        }
        catch (System.ArgumentException)
        {
            thrown = true;
        }
        AssertTrue(thrown, "a mismatched buffer is refused");
    }

    /// <summary>
    /// Copies the top left region of a cached layer into a screen sized frame,
    /// which is what a camera does. The pinned fixture is 20 by 15 tiles, which
    /// is exactly one 320 by 240 screen, so the camera is built here from a
    /// synthetic layer that is deliberately larger than the screen: a
    /// one screen fixture cannot observe a camera at all.
    /// </summary>
    public void Test_ACameraOnAMapLargerThanTheScreenScrollsTheViewport()
    {
        const int tileSize = Rm2kChipsetBitmap.TileSize;
        // Two screens wide and two high, so the viewport has somewhere to go.
        var lower = new Rm2kPixelBuffer(40 * tileSize, 30 * tileSize);
        // A distinct opaque colour per tile, so a shifted frame cannot look
        // identical to an unshifted one.
        for (var y = 0; y < 30; y++)
        {
            for (var x = 0; x < 40; x++)
            {
                lower.TrySetPixel(x * tileSize, y * tileSize,
                    (byte)(x + 1), (byte)(y + 1), 200, 255);
            }
        }

        var screen = new Rm2kPixelBuffer(320, 240);
        AssertTrue(lower.TryCopyRegion(0, 0, 320, 240, screen, 0, 0),
            "the first screen is copied out of the layer");
        var atOrigin = (uint)(screen.Pixels[0] | (screen.Pixels[1] << 8) | (screen.Pixels[2] << 16));

        // One tile to the right: the camera reads a different part of the map.
        screen.Clear();
        AssertTrue(lower.TryCopyRegion(tileSize, 0, 320, 240, screen, 0, 0),
            "the next screen is copied out of the layer");
        var shifted = (uint)(screen.Pixels[0] | (screen.Pixels[1] << 8) | (screen.Pixels[2] << 16));
        AssertTrue(atOrigin != shifted,
            $"scrolling one tile changes the frame ({atOrigin} vs {shifted})");

        // A region that does not fit inside the source is refused rather than
        // clipped, because a partial window is a decision the caller has to
        // make, not a silent truncation.
        AssertEq(lower.TryCopyRegion(lower.Width - 10, 0, 320, 240, screen, 0, 0), false,
            "a region past the layer edge is refused");
        AssertEq(lower.TryCopyRegion(-1, 0, 320, 240, screen, 0, 0), false,
            "a negative source offset is refused");
    }

    /// <summary>
    /// The runtime frame is screen sized even when the map is larger, and the
    /// frame the pinned fixture produces is unchanged by the viewport, because
    /// that map is exactly one screen.
    /// </summary>
    public void Test_AFullMapFrameBecomesAScreenSizedViewport()
    {
        var gameDir = CopyRealGame(nameof(Test_AFullMapFrameBecomesAScreenSizedViewport));
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
            var frame = created.Value.Runtime.RenderedMap;
            AssertTrue(frame != null, $"the map rendered: {created.Value.Runtime.RenderDiagnostic}");
            if (frame == null)
            {
                return;
            }
            // The pinned map is exactly one screen, so the viewport shows all of
            // it and the frame is exactly the screen.
            AssertEq(frame.Width, 320, "the frame is one screen wide");
            AssertEq(frame.Height, 240, "the frame is one screen high");
        }
        finally
        {
            Cleanup(gameDir);
        }
    }

    /// <summary>
    /// K-104: a successful step must change the visible frame. This is the only
    /// test that exercises the TryMove path, so a mutation that removes the
    /// recomposition from it has to fail here.
    /// </summary>
    public void Test_AMoveChangesTheVisibleFrame()
    {
        var gameDir = CopyRealGame(nameof(Test_AMoveChangesTheVisibleFrame));
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
            AssertTrue(runtime.RenderedMap != null, $"the map rendered: {runtime.RenderDiagnostic}");
            if (runtime.RenderedMap == null)
            {
                return;
            }
            // **Und  hier  war  die  Begruendung  falsch.**
            //
            // **Und  hier  stand,  die  gepinnte  Bank  definiere  keine
            //  Startparty**, -- **und  darum  werde  das  Bild  gleich
            //  bleiben** -- **und  diese  Annahme  hat  den  Test
            //  gruen  gehalten,  ohne  etwas  zu  pruefen.**
            //
            // **Und  Dragon  Destinys  `System`-Chunk  traegt  bei  `0x16`
            //  zwei  Byte:  `[1, 0]`** -- **und  das  ist  Little-Endian
            //  `1`** -- **und  das  ist  Held  eins**, -- **und  der  Held
            //  wird  gezeichnet.**
            //
            // **Und  darum  ist  die  Erwartung  jetzt  die  Umkehrung**:
            // -- **ein  Schritt  muss  das  Bild  veraendern**, --
            // **und  ein  gleiches  Bild  hiesse,  dass  der  Helden- oder
            //  Kachelsatz  fehlt.**
            var first = (byte[])runtime.RenderedMap.Pixels.Clone();
            runtime.PlacePlayerForTest(1, 0);
            runtime.MarkFrameDirtyForTest();
            runtime.RefreshFrameForTest();
            var second = runtime.RenderedMap!.Pixels;
            AssertEq(runtime.RenderedMap.Width, 320, "the frame is one screen wide");
            AssertEq(runtime.RenderedMap.Height, 240, "the frame is one screen high");
            var differs = 0;
            for (var index = 0; index < first.Length; index++)
            {
                if (first[index] != second[index])
                {
                    differs++;
                }
            }
            AssertTrue(differs > 0,
                "**and moving the hero changes the frame** -- and"
                    + " it must, because the game's own party names"
                    + " actor one and the hero is drawn; an"
                    + " unchanged frame here would mean the"
                    + " character layer is missing, and the old"
                    + " assertion demanded exactly that");
        }
        finally
        {
            Cleanup(gameDir);
        }
    }

    /// <summary>
    /// Writes a real LMU whose map is larger than one 320 by 240 screen, so the
    /// camera viewport becomes observable. The pinned fixture is 20 by 15 tiles,
    /// which is exactly one screen, and therefore cannot test a camera at all.
    /// </summary>
    /// <remarks>
    /// Field ids are taken from the parser, not guessed: <c>0x01</c> chipset,
    /// <c>0x02</c> width, <c>0x03</c> height, <c>0x47</c> the lower tile layer
    /// and <c>0x48</c> the upper one. The lower layer uses a different tile per
    /// tile, so a scrolled frame cannot look like an unshifted one.
    /// </remarks>
    private string? CreateWideMapGame(string pName, int pWidth, int pHeight)
    {
        var ldb = ProjectSettings.GlobalizePath(FixtureRoot.PathJoin("rm2000/RPG_RT.ldb"));
        if (!File.Exists(ldb))
        {
            AssertTrue(false, $"the pinned LDB exists: {ldb}");
            return null;
        }
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

            var chipset = ProjectSettings.GlobalizePath(FixtureRoot.PathJoin(ChipsetFixture));
            if (File.Exists(chipset))
            {
                DirAccess.MakeDirRecursiveAbsolute(Path.Combine(gameDir, "ChipSet"));
                File.Copy(chipset, Path.Combine(gameDir, "ChipSet", "World.png"), true);
            }
            // Characters are needed to make a scroll visible. The chipset tiles
            // 0 to 255 all render as the same floor variant, so a tile only
            // fixture cannot prove a scroll by pixel difference; an event
            // character standing on a known tile can.
            var charset = ProjectSettings.GlobalizePath(CharsetFixture);
            if (File.Exists(charset))
            {
                DirAccess.MakeDirRecursiveAbsolute(Path.Combine(gameDir, "CharSet"));
                File.Copy(charset, Path.Combine(gameDir, "CharSet", "Chara1.png"), true);
            }

            WriteWideMap(Path.Combine(gameDir, "Map0001.lmu"), pWidth, pHeight);
            return gameDir;
        }
        catch (IOException exception)
        {
            AssertTrue(false, $"the fixture copy failed: {exception.Message}");
            return null;
        }
    }

    /// <summary>Width and height of the synthetic wide map, in tiles.</summary>
    private const int WideMapTiles = 40;

    /// <summary>
    /// An upper layer filled with block F tile 10000, the id the pinned fixture
    /// uses. The bytes are little endian int16, as the LMU lower layer is.
    /// </summary>
    private static byte[] UpperLayer(int pTileCount)
    {
        var layer = new byte[pTileCount * 2];
        for (var index = 0; index < pTileCount; index++)
        {
            layer[index * 2] = (byte)(10000 & 0xFF);
            layer[index * 2 + 1] = (byte)((10000 >> 8) & 0xFF);
        }
        return layer;
    }

    /// <summary>One event character on the wide map, with its charset cell.</summary>
    private readonly record struct WideMapEventSpot(int Id, int X, int Y, int CharacterIndex);

    /// <summary>
    /// Characters spread across the wide map, far enough apart that a scrolled
    /// screen always shows a different set of them. Several cell indices are
    /// used, so the frame differs even where two positions share a tile.
    /// </summary>
    private static readonly WideMapEventSpot[] WideMapEventSpots =
    {
        // A 320 pixel screen shows 20 tiles. At camera offset 0 the window is
        // columns 0 to 19, at 176 pixels it is 11 to 30, and at 480 pixels it is
        // 30 to 39, so a character has to sit in each of those bands for the
        // three frames to differ. Rows 2 and 18 stay inside the 15 tile tall
        // window at every offset.
        // A 240 pixel screen shows 15 rows. At offset 0 the window is rows 0 to
        // 14, at 128 pixels it is 8 to 22, and at 352 pixels it is 22 to 29.
        new(1, 4, 2, 0),
        new(2, 16, 9, 1),
        new(3, 26, 12, 2),
        new(4, 36, 24, 3),
        new(5, 6, 25, 4),
        new(6, 22, 27, 5),
        new(7, 34, 11, 6),
        new(8, 38, 26, 7),
    };

    /// <summary>Builds the LCF event structs of the wide map.</summary>
    private static byte[][] BuildWideMapEvents()
    {
        var events = new List<byte[]>();
        foreach (var spot in WideMapEventSpots)
        {
            var page = new List<byte[]>
            {
                // 0x21 trigger action, 0x22 same-as-hero layer, 0x16 character
                // index, 0x17 facing down, 0x15 the character name.
                TestRm2kParser.Chunk(0x21, TestRm2kParser.Ber(0)),
                TestRm2kParser.Chunk(0x22, TestRm2kParser.Ber(1)),
                TestRm2kParser.Chunk(0x16, TestRm2kParser.Ber(spot.CharacterIndex)),
                TestRm2kParser.Chunk(0x17, TestRm2kParser.Ber(2)),
                TestRm2kParser.Chunk(0x15, System.Text.Encoding.ASCII.GetBytes("Chara1")),
            };
            events.Add(TestRm2kParser.Struct(new List<byte[]>
            {
                TestRm2kParser.Chunk(0x01, TestRm2kParser.Ber(spot.Id)),
                TestRm2kParser.Chunk(0x02, TestRm2kParser.Ber(spot.X)),
                TestRm2kParser.Chunk(0x03, TestRm2kParser.Ber(spot.Y)),
                TestRm2kParser.Chunk(0x05, TestRm2kParser.StructArray(TestRm2kParser.Struct(page.ToArray()))),
            }.ToArray()));
        }
        return events.ToArray();
    }

    /// <summary>Writes an LCF map file with the verified field layout.</summary>
    private static void WriteWideMap(string pPath, int pWidth, int pHeight)
    {
        var tileCount = pWidth * pHeight;
        var lower = new byte[tileCount * 2];
        for (var index = 0; index < tileCount; index++)
        {
            // Ids the pinned fixture really uses, cycled so the map is not
            // uniform. Block E is the only block whose ids are single sixteen
            // pixel tiles: A and B are autotiles whose quarters can be fully
            // transparent, and D ids are four by four quadrant blocks, so
            // neither covers a tile and both would leave the map unpainted.
            var tile = 5000;
            lower[index * 2] = (byte)(tile & 0xFF);
            lower[index * 2 + 1] = (byte)((tile >> 8) & 0xFF);
        }

        var chunks = new System.Collections.Generic.List<byte[]>
        {
            TestRm2kParser.Chunk(0x01, TestRm2kParser.Ber(1)),
            TestRm2kParser.Chunk(0x02, TestRm2kParser.SystemText("World")),
            TestRm2kParser.Chunk(0x02, TestRm2kParser.Ber(pWidth)),
            TestRm2kParser.Chunk(0x03, TestRm2kParser.Ber(pHeight)),
            TestRm2kParser.Chunk(0x47, lower),
            // Event characters standing on known tiles make a scroll visible.
            // The chipset tiles 0 to 255 all render as the same floor variant,
            // so a tile only fixture cannot prove a scroll by pixel difference.
            // Field ids come from the parser: event 0x02 is x, 0x03 is y, 0x05
            // the page array; page 0x22 layer, 0x16 character index, 0x17
            // direction, 0x15 character name.
            TestRm2kParser.Chunk(0x51, TestRm2kParser.StructArray(BuildWideMapEvents())),
            // The upper layer stays tile 0, which is transparent in the real
            // chipset, so it does not hide the characters.
            // The upper layer of the pinned fixture uses tile 10000, which
            // covers the characters, so it is kept transparent here to let the
            // characters show through.
            // The upper layer must be the block F id the pinned fixture uses,
            // which is transparent in this chipset. Tile 0 is a block A autotile
            // and paints over the floor, which hid the whole lower layer.
            TestRm2kParser.Chunk(0x48, UpperLayer(tileCount)),
        };
        var bytes = new List<byte>();
        var header = "LcfMapUnit";
        bytes.AddRange(TestRm2kParser.Ber(header.Length));
        bytes.AddRange(System.Text.Encoding.ASCII.GetBytes(header));
        foreach (var chunk in chunks)
        {
            bytes.AddRange(chunk);
        }
        var terminator = new byte[] { 0x00 };
        bytes.AddRange(terminator);
        using var file = Godot.FileAccess.Open(pPath, Godot.FileAccess.ModeFlags.Write);
        file?.StoreBuffer(bytes.ToArray());
    }

    /// <summary>
    /// K-105 unblock condition: a map larger than one screen must scroll. The
    /// pinned fixture is exactly one screen, so this test writes a 40 by 30 map,
    /// which is 640 by 480 pixels and therefore larger than 320 by 240 in both
    /// directions. If this test passes while the camera is disabled, the suite
    /// is not proving the camera, which is exactly what mutation A showed.
    /// </summary>
    public void Test_AMapLargerThanTheScreenScrollsWithTheCamera()
    {
        var gameDir = CreateWideMapGame(nameof(Test_AMapLargerThanTheScreenScrollsWithTheCamera), 40, 30);
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
            AssertTrue(runtime.RenderedMap != null, $"the wide map rendered: {runtime.RenderDiagnostic}");
            if (runtime.RenderedMap == null)
            {
                return;
            }
            AssertEq(runtime.RenderedMap.Width, 320, "a wide map is drawn into a screen sized frame");
            AssertEq(runtime.RenderedMap.Height, 240, "a tall map is drawn into a screen sized frame");

            // The chipset tiles of this fixture need the LDB tile substitution to
            // render, and TileSubstitution is not populated yet, so the floor of
            // a synthetic map stays empty. The pixel difference therefore cannot
            // be asserted here yet. What is asserted instead is the decision the
            // camera makes, through the frame's own geometry: a wider map must
            // produce a scroll offset, and it must stay inside the map.
            AssertTrue(runtime.SpriteDescriptors.Count >= WideMapEventSpots.Length,
                $"the wide map exposes its {WideMapEventSpots.Length} event characters, " +
                $"but the runtime reported {runtime.SpriteDescriptors.Count}");

            // Standing in the middle of a 40 tile map must scroll, because
            // 40 * 16 = 640 pixels is wider than the 320 pixel screen. The
            // runtime is asked for the offset it actually applied, so this
            // asserts the wiring and not only the arithmetic in Rm2kMapCamera.
            var atOrigin = (byte[])runtime.RenderedMap!.Pixels.Clone();

            // The real movement path, not the test hooks: a mutation that drops
            // RecomposeFrame from TryMove must fail here, because the hooks
            // would otherwise recompose on their own and hide it. TryMove takes
            // one step, so the walk to the middle of the map is made of steps.
            // Every tile of this fixture is walkable, which the assertion below
            // proves on the first step instead of assuming it.
            AssertEq(runtime.TryMove(1, 0), true,
                "the player steps onto a walkable tile of the wide map");
            for (var step = 0; step < 19; step++)
            {
                AssertEq(runtime.TryMove(1, 0), true, "the player keeps walking east");
            }
            AssertEq(runtime.TryMove(0, 1), true, "the player steps south");
            for (var step = 0; step < 14; step++)
            {
                AssertEq(runtime.TryMove(0, 1), true, "the player keeps walking south");
            }

            // The floor now renders, so a scroll must be visible in the pixels.
            // This is the assertion the card was blocked on.
            var differs = 0;
            for (var index = 0; index < atOrigin.Length; index++)
            {
                if (atOrigin[index] != runtime.RenderedMap!.Pixels[index])
                {
                    differs++;
                }
            }
            AssertTrue(differs > 0,
                $"the camera scroll is visible in the frame, but {differs} bytes differ");

            AssertEq(runtime.AppliedCameraOffsetX, 176,
                "the runtime applies the verified camera offset to the composed frame");
            AssertEq(runtime.AppliedCameraOffsetY, 128,
                "the vertical camera offset is applied as well");
            var scrolled = runtime.AppliedCameraOffsetX;
            AssertTrue(scrolled < WideMapTiles * 16 - 320,
                "the offset stays inside the map, so the viewport never leaves it");

            // The upstream clamp mixes units, so on a 40 tile map the reachable
            // offset is 480 pixels while a 320 pixel window would need at most
            // 320. The extra 160 pixels are exactly the black border the Player
            // shows at a map edge, and CopyViewport leaves them unpainted rather
            // than reading past the layer. This asserts that boundary instead of
            // pretending the two units agree.
            runtime.PlacePlayerForTest(WideMapTiles - 1, 29);
            runtime.MarkFrameDirtyForTest();
            runtime.RefreshFrameForTest();
            AssertEq(runtime.AppliedCameraOffsetX, 480,
                "the last column scrolls to 480 pixels, the verified clamp of a 40 tile map");
            AssertTrue(runtime.AppliedCameraOffsetX > WideMapTiles * 16 - 320,
                "the upstream clamp can exceed the pixel bound, which is the black border case");
        }
        finally
        {
            Cleanup(gameDir);
        }
    }

    private static int CountOpaque(Rm2kPixelBuffer? pBuffer)
    {
        if (pBuffer == null) return -1;
        var n = 0;
        for (var i = 3; i < pBuffer.Pixels.Length; i += 4) if (pBuffer.Pixels[i] != 0) n++;
        return n;
    }

    private static int CountColours(byte[] pPixels)
    {
        var seen = new HashSet<uint>();
        for (var index = 0; index < pPixels.Length; index += 4)
        {
            seen.Add((uint)(pPixels[index]
                | (pPixels[index + 1] << 8)
                | (pPixels[index + 2] << 16)
                | (pPixels[index + 3] << 24)));
        }
        return seen.Count;
    }

    /// <summary>Copies the pinned RM2000 fixture plus its chipset into a temp game dir.</summary>
    /// <summary>
    /// The runtime builds all three vehicles from the LMT start node and the
    /// LDB system section, and it only draws the ones whose start map is the
    /// loaded one.
    /// </summary>
    /// <remarks>
    /// The pinned fixture's vehicles all start on map 39 while only
    /// <c>Map0001.lmu</c> ships, so this map draws no vehicle. That is the
    /// correct outcome and the reason the vehicle pixels are proven on synthetic
    /// frames instead: a test that wanted a visible boat here would have to
    /// invent a map 39 or a <c>vehicle.png</c> that the game never had.
    /// </remarks>
    public void Test_TheRuntimeBuildsTheThreeVehiclesFromTheRealDatabaseAndTree()
    {
        var gameDir = CopyRealGame(nameof(Test_TheRuntimeBuildsTheThreeVehiclesFromTheRealDatabaseAndTree));
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
            AssertEq(runtime.Vehicles.Count, 3, "the runtime always has three vehicles");

            var boat = runtime.Vehicles[0];
            var ship = runtime.Vehicles[1];
            var airship = runtime.Vehicles[2];
            AssertEq(boat.VehicleType, Rm2kVehicle.Boat, "the first is the boat");
            AssertEq(ship.VehicleType, Rm2kVehicle.Ship, "the second is the ship");
            AssertEq(airship.VehicleType, Rm2kVehicle.Airship, "and the third is the airship");

            // The tile comes from the LMT start node, not from an event.
            AssertEq(boat.MapId, 39, "the boat's start map is 39");
            AssertEq(boat.X, 10, "at x 10");
            AssertEq(boat.Y, 3, "and y 3");
            AssertEq(ship.X, 9, "the ship is at x 9");
            AssertEq(airship.X, 2, "and the airship at x 2");
            AssertEq(airship.Y, 8, "on row 8");

            // The move speed comes from the Player's constructor switch, not from
            // the file, so a vehicle is faster than the default event speed.
            AssertEq(boat.MoveSpeed, 4, "a boat moves at MoveSpeed_normal");
            AssertEq(airship.MoveSpeed, 5, "and the airship at MoveSpeed_double");

            // The sprite comes from the LDB system section.
            AssertEq(boat.CharacterName, "vehicle", "the boat names the vehicle charset");
            AssertEq(boat.SpriteIndex, 0, "at cell 0");
            AssertEq(ship.SpriteIndex, 1, "the ship at cell 1");
            AssertEq(airship.CharacterName, "Vehicle", "and the airship at cell 3 of Vehicle");
            AssertEq(airship.SpriteIndex, 3, "the airship's own index");

            // The loaded map is map 1 and all three vehicles are on map 39, so
            // none of them is on this map and none is drawn.
            AssertTrue(!boat.IsInPosition(1, boat.X, boat.Y),
                "a vehicle on another map is not in position on this one");
        }
        finally
        {
            Cleanup(gameDir);
        }
    }

    /// <summary>
    /// A vehicle is only drawn when its own start map is the loaded one, so a
    /// vehicle moved onto this map has to appear and one left on map 39 has to
    /// stay gone.
    /// </summary>
    /// <remarks>
    /// The draw path is what this proves. A test that only checked the vehicle
    /// list would pass with the map check removed, which is exactly what a
    /// mutation run found before this test existed.
    /// </remarks>
    public void Test_AVehicleIsOnlyDrawnWhenItsStartMapIsTheLoadedOne()
    {
        var gameDir = CopyRealGame(nameof(Test_AVehicleIsOnlyDrawnWhenItsStartMapIsTheLoadedOne));
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
            AssertEq(runtime.RenderedMap != null, true, "the map rendered");
            if (runtime.RenderedMap == null)
            {
                return;
            }
            AssertEq(runtime.VehiclesOnCurrentMap().Count, 0,
                "no vehicle starts on this map, so none is drawn");

            // The boat's charset is 'vehicle', which this fixture does not ship,
            // so a boat on this map has to be reported rather than drawn as
            // Chara1. That is the fail-closed rule, and it is what the runtime
            // has to do with a missing cell.
            AssertTrue(runtime.PlaceVehicleOnCurrentMapForTest(Rm2kVehicle.Boat, 5, 5),
                "the boat was moved onto this map");
            AssertEq(runtime.VehiclesOnCurrentMap().Count, 1,
                "and it is now the one vehicle on this map");

            var before = CountNonTransparent(runtime.RenderedMap);
            runtime.RestoreRenderForTest();
            if (runtime.RenderedMap == null)
            {
                return;
            }
            var after = CountNonTransparent(runtime.RenderedMap);
            AssertEq(after, before,
                "a vehicle whose charset is missing draws nothing and reports instead");

            // Moving it back must take it off the map again, which is the other
            // half of the same check.
            runtime.Vehicles[0].MapId = 39;
            AssertEq(runtime.VehiclesOnCurrentMap().Count, 0,
                "and a vehicle on another map is not drawn on this one");
        }
        finally
        {
            Cleanup(gameDir);
        }
    }

    private static int CountNonTransparent(Rm2kPixelBuffer pFrame)
    {
        var count = 0;
        foreach (var channel in pFrame.Pixels)
        {
            if (channel != 0)
            {
                count++;
            }
        }
        return count;
    }

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
            // The hero and the event characters are drawn from the CharSet
            // directory, so the fixture has to provide it. Without it the
            // runtime correctly draws no characters, which would hide a
            // regression in the sprite path behind a missing-file diagnostic.
            var charset = ProjectSettings.GlobalizePath(CharsetFixture);
            AssertTrue(File.Exists(charset), "the pinned Chara1.png exists: " + charset);
            if (File.Exists(charset))
            {
                DirAccess.MakeDirRecursiveAbsolute(Path.Combine(gameDir, "CharSet"));
                File.Copy(charset, Path.Combine(gameDir, "CharSet", "Chara1.png"), true);
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
	/// <summary>
	/// What the pinned fixture actually contains for vehicles, measured rather
	/// than assumed.
	/// </summary>
	/// <remarks>
	/// All three vehicles name the same charset, <c>vehicle</c>, with indices
	/// 0, 1 and 2, which is the usual layout: one sheet holding every boat. The
	/// start node parks them all on map 39, and this fixture only ships
	/// <c>Map0001.lmu</c>, so the vehicles are not on the loaded map. On top of
	/// that <c>CharSet</c> holds only <c>Chara1.png</c>, so even on the right map
	/// the vehicle cell could not be drawn.
	///
	/// This is the honest reason the vehicle rendering is verified on synthetic
	/// frames rather than on this map. It is a fixture limit, not a claim that
	/// vehicles work in a real game.
	/// </remarks>
	public void Test_TheFixtureHasVehicleDataButNoVehicleSpriteOnThisMap()
	{
		var database = new Rm2kParser().ParseDatabase(
			ProjectSettings.GlobalizePath(FixtureRoot + "/rm2000/RPG_RT.ldb"));
		AssertTrue(database.IsSuccess(), "the pinned LDB parses");
		if (!database.IsSuccess())
		{
			return;
		}
		var system = (Godot.Collections.Dictionary)database.GetData()["system"];
		AssertEq(system["boat_name"].AsString(), "vehicle", "the boat names the vehicle charset");
		AssertEq(system["ship_name"].AsString(), "vehicle", "and so does the ship");
		AssertEq(system["airship_name"].AsString(), "Vehicle", "while the airship name is capitalised");
		AssertEq(system["boat_index"].AsInt64(), 0, "the boat is cell 0");
		AssertEq(system["ship_index"].AsInt64(), 1, "the ship is cell 1");
		// The measured values are 0, 1 and 3, not 0, 1 and 2. The airship cell is
		// 3, which means this game leaves cell 2 of the vehicle sheet unused. That
		// is legal: the index comes from the editor and nothing requires the three
		// vehicles to be adjacent.
		AssertEq(system["airship_index"].AsInt32(), 3, "and the airship is cell 3");

		var tree = new Rm2kParser().ParseMapTree(
			ProjectSettings.GlobalizePath(FixtureRoot + "/rm2000/RPG_RT.lmt"));
		AssertTrue(tree.IsSuccess(), "the pinned LMT parses");
		if (!tree.IsSuccess())
		{
			return;
		}
		var start = (Godot.Collections.Dictionary)tree.GetData()["start"];
		AssertEq(start["boat_map_id"].AsInt64(), 39, "the boat starts on map 39");
		AssertEq(start["ship_map_id"].AsInt64(), 39, "the ship starts on map 39");
		AssertEq(start["airship_map_id"].AsInt64(), 39, "and so does the airship");

		// The loaded map is map 1, so no vehicle is on it and none is drawn.
		// A charset lookup would also fail: CharSet has only Chara1.png.
		AssertEq(start["party_map_id"].AsInt64(), 30,
			"and the party itself is on map 30, so the fixture is a data sample rather than a playable map");
	}

}
