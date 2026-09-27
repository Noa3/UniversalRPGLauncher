using System;
using Godot;
using UniversalRPG.Rm2k.Rendering;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// K-105: the map camera. The pinned fixture is exactly 20 by 15 tiles, which is
/// exactly one screen, so a one screen map cannot detect a missing or wrong
/// camera at all. These tests therefore use an explicitly larger map.
/// </summary>
public partial class TestRm2kMapCamera : TestBase
{
    /// <summary>A map clearly wider than the 320 pixel screen.</summary>
    private const int WideMap = 40;

    /// <summary>A map clearly taller than the 240 pixel screen.</summary>
    private const int TallMap = 40;

    /// <summary>The pinned fixture width: exactly one screen in pixels.</summary>
    private const int OneScreenWide = 20;

    /// <summary>The pinned fixture height: exactly one screen in pixels.</summary>
    private const int OneScreenHigh = 15;

    /// <summary>
    /// <c>GetDefaultPanX</c> at 320 pixels is <c>ceil(320 / 16 / 2) - 1 = 9</c>
    /// screen tiles, and Y at 240 pixels is <c>ceil(240 / 16 / 2) - 1 = 7</c>.
    /// </summary>
    public void Test_TheDefaultPanFollowsTheVerifiedFormula()
    {
        AssertEq(Rm2kMapCamera.DefaultPanX(), 9 * 256, "DefaultPanX is nine screen tiles");
        AssertEq(Rm2kMapCamera.DefaultPanY(), 7 * 256, "DefaultPanY is seven screen tiles");
        AssertEq(Rm2kMapCamera.DefaultPanX(640), 19 * 256, "a 640 pixel screen widens the pan");
    }

    /// <summary>
    /// The pan is a screen tile offset, not a pixel offset. A pixel pan would
    /// leave the camera almost static, which is the mistake this pins.
    /// </summary>
    public void Test_ThePanIsAScreenTileOffsetAndNotAPixelOffset()
    {
        var pan = Rm2kMapCamera.DefaultPanX();
        AssertTrue(pan > 9 * Rm2kMapCamera.TileSize,
            $"DefaultPanX is {pan}, which is larger than a nine pixel offset");
    }

    public void Test_AMapWiderThanTheScreenScrolls()
    {
        AssertEq(Rm2kMapCamera.PositionX(0, WideMap), 0, "the offset at the left edge");
        // The right edge of the screen shows the right edge of the map, so the
        // offset is tiles * 256 - screen_width, exactly as SetPositionX clamps.
        AssertEq(Rm2kMapCamera.PositionX(100, WideMap), WideMap * 256 - 320,
            "the offset at the right edge");
        // Halfway the player is off centre by the pan, so the offset is the
        // player position in screen tiles minus the pan, clamped into range.
        AssertEq(Rm2kMapCamera.PositionX(10, WideMap), 1 * 256, "the offset follows the pan");
        AssertTrue(WideMap * 256 - 320 > 0, "a map wider than the screen can actually scroll");
    }

    public void Test_AMapTallerThanTheScreenScrolls()
    {
        AssertEq(Rm2kMapCamera.PositionY(0, TallMap), 0, "the offset at the top edge");
        AssertEq(Rm2kMapCamera.PositionY(100, TallMap), TallMap * 256 - 240,
            "the offset at the bottom edge");
        AssertEq(Rm2kMapCamera.PositionY(8, TallMap), 1 * 256, "the offset follows the pan");
    }

    /// <summary>
    /// <c>SetPositionX</c> counts the map extent in screen tiles but the screen
    /// in pixels, so a map that is exactly one screen wide in pixels
    /// (<c>20 * 16 = 320</c>) still has a positive upper bound
    /// (<c>20 * 256 - 320 = 4800</c>) and therefore still scrolls. This test
    /// pins that upstream unit mix instead of assuming a one screen map is
    /// pinned to the origin, which is what a naive reading would expect.
    /// </summary>
    public void Test_AMapExactlyOneScreenWideStillHasAPositiveBound()
    {
        AssertEq(Rm2kMapCamera.PositionX(0, OneScreenWide), 0,
            "the offset at the left edge is zero because of the clamp, not the bound");
        AssertEq(Rm2kMapCamera.PositionX(19, OneScreenWide), 19 * 256 - 9 * 256,
            "the bound does not bite for a one screen wide map");
        AssertEq(Rm2kMapCamera.PositionY(14, OneScreenHigh), 14 * 256 - 7 * 256,
            "the same holds vertically");
    }

    /// <summary>
    /// A map genuinely smaller than the screen has a non-positive upper bound
    /// and stays at the origin. This is the case where the Player's explicit
    /// "do not use std::clamp" comment matters, because the lower bound would
    /// exceed the upper bound.
    /// </summary>
    public void Test_AMapSmallerThanTheScreenStaysAtTheOrigin()
    {
        AssertEq(Rm2kMapCamera.PositionX(0, 4), 0, "a four tile wide map at the left");
        AssertEq(Rm2kMapCamera.PositionX(3, 4), 0, "a four tile wide map at the right");
        AssertEq(Rm2kMapCamera.PositionY(0, 3), 0, "a three tile high map at the top");
        AssertEq(Rm2kMapCamera.PositionY(2, 3), 0, "a three tile high map at the bottom");
    }

    /// <summary>The camera never leaves the map, for any player position.</summary>
    public void Test_TheOffsetAlwaysStaysInsideTheMap()
    {
        var maxX = WideMap * 256 - 320;
        var maxY = TallMap * 256 - 240;
        for (var x = -5; x <= 45; x++)
        {
            var position = Rm2kMapCamera.PositionX(x, WideMap);
            AssertTrue(position >= 0 && position <= maxX,
                $"the offset for player x={x} is {position}, outside [0, {maxX}]");
        }
        for (var y = -5; y <= 45; y++)
        {
            var position = Rm2kMapCamera.PositionY(y, TallMap);
            AssertTrue(position >= 0 && position <= maxY,
                $"the offset for player y={y} is {position}, outside [0, {maxY}]");
        }
    }

    /// <summary>A looping map wraps with a positive modulo instead of clamping.</summary>
    public void Test_ALoopingMapWrapsInsteadOfClamping()
    {
        const int loopMap = 10;
        var wrapped = Rm2kMapCamera.PositionX(-1, loopMap, 320, pLoopHorizontal: true);
        var expected = Rm2kMapCamera.PositiveModulo(-1 * 256 - 9 * 256, loopMap * 256);
        AssertEq(wrapped, expected, "the wrapped offset matches the positive modulo");
        // -1 * 256 - 9 * 256 is exactly -10 * 256, which wraps to the origin.
        AssertEq(wrapped, 0, "a player one screen tile before the origin wraps to zero");
        // A player that is not a whole map away does wrap visibly.
        var partial = Rm2kMapCamera.PositionX(-2, loopMap, 320, pLoopHorizontal: true);
        AssertTrue(partial > 0, "a player before the origin is visible near the right edge");
        AssertTrue(partial < loopMap * 256, "the wrapped offset stays inside the map");
    }

    /// <summary><c>Utils::PositiveModulo</c> never returns a negative value.</summary>
    public void Test_ThePositiveModuloIsNeverNegative()
    {
        AssertEq(Rm2kMapCamera.PositiveModulo(-1, 10), 9, "a negative remainder");
        AssertEq(Rm2kMapCamera.PositiveModulo(21, 10), 1, "a large value");
        AssertEq(Rm2kMapCamera.PositiveModulo(0, 10), 0, "zero");
        var rejected = false;
        try
        {
            Rm2kMapCamera.PositiveModulo(1, 0);
        }
        catch (ArgumentOutOfRangeException)
        {
            rejected = true;
        }
        AssertTrue(rejected, "a zero modulus is refused");
    }
}
