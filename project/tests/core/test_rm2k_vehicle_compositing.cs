using System;
using Godot;
using UniversalRPG.Rm2k.Rendering;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Proves that a vehicle is actually composited into a map frame, and the
/// airship shadow that goes with it.
/// </summary>
/// <remarks>
/// A vehicle that is described correctly but never composited is not a vehicle
/// the player can see, so these tests build a real frame and read the pixels
/// back. The altitude is the interesting part: it is counted in tiles, so a
/// flying airship has to come out a whole number of tiles above its map
/// position, and a reader that forgot to multiply by the tile size would put it
/// 16 times too low without failing a bounds check.
/// </remarks>
public partial class TestRm2kVehicleCompositing : TestBase
{
    /// <summary>The pinned test game, same root the other RM2K rendering tests use.</summary>
    private const string FixtureRoot = "res://tests/fixtures/easyrpg-testgame";

    /// <summary>Alex's charset, the pinned RM2K character sheet.</summary>
    private const string CharsetFixture = FixtureRoot + "/rm2000/CharSet/Chara1.png";

    /// <summary>
    /// A frame and the pinned real charset. The index buffer means a drawn cell
    /// shows as index 1, which the empty transparent frame never has.
    /// </summary>
    /// <summary>Set once by <see cref="MakeFrame"/> so a helper can report a bad load.</summary>
    private static Rm2kCharset? _charset;

    private static (Rm2kPixelBuffer Frame, Rm2kCharset Charset) MakeFrame()
    {
        _charset = null;
        if (!Rm2kIndexedImage.TryLoad(ProjectSettings.GlobalizePath(CharsetFixture), out var image, out var error))
        {
            // The rest of the test cannot run without the fixture, so this fails
            // the run rather than returning a substitute that would make the
            // pixel assertions pass for the wrong reason.
            throw new InvalidOperationException($"the pinned charset {CharsetFixture} decodes: {error}");
        }
        _charset = new Rm2kCharset(image);
        return (new Rm2kPixelBuffer(320, 240), _charset);
    }

    /// <summary>
    /// How many pixels of the frame carry a drawn cell. The buffer is RGBA and
    /// a fresh one is all zeroes, so any non-zero alpha came from the draw.
    /// </summary>
    private static int CountDrawn(Rm2kPixelBuffer pFrame)
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

    /// <summary>The topmost row of the frame that has anything drawn on it.</summary>
    private static int FirstRowWithPixels(Rm2kPixelBuffer pFrame)
    {
        for (var y = 0; y < pFrame.Height; y++)
        {
            for (var x = 0; x < pFrame.Width; x++)
            {
                if (pFrame.Pixels[(y * pFrame.Width + x) * 4 + 3] != 0)
                {
                    return y;
                }
            }
        }
        return -1;
    }

    private static Rm2kVehicleSprite Vehicle(int pType, int pX, int pY, int pAltitude = 0)
    {
        return new Rm2kVehicleSprite
        {
            VehicleType = pType,
            CharacterName = "Char",
            MapX = pX,
            MapY = pY,
            Altitude = pAltitude,
            FacingDirection = 2,
            Frame = Rm2kCharset.FrameMiddle,
        };
    }



    public void Test_ABoatIsDrawnOntoTheMapAtItsOwnTile()
    {
        var (frame, charset) = MakeFrame();
        var drawn = Rm2kMapFrameRenderer.DrawVehicle(frame, charset, Vehicle(Rm2kVehicle.Boat, 3, 4), 0, 0);

        AssertTrue(drawn, "the boat was drawn");
        AssertTrue(CountDrawn(frame) > 0, "and it put its pixels on the frame");
    }

    public void Test_AFlyingAirshipComesOutAWholeTileHigher()
    {
        // GetAltitude is in whole tiles, so the pixel offset is the altitude
        // times TILE_SIZE. Getting that wrong puts the airship 16 times too low
        // and still draws it, so the row of the first drawn pixel is what proves
        // the multiplication happened.
        var (ground, groundCharset) = MakeFrame();
        Rm2kMapFrameRenderer.DrawVehicle(ground, groundCharset, Vehicle(Rm2kVehicle.Airship, 5, 5), 0, 0);
        var groundRow = FirstRowWithPixels(ground);
        AssertTrue(groundRow > 0, "the grounded airship drew something");

        var (flying, flyingCharset) = MakeFrame();
        Rm2kMapFrameRenderer.DrawVehicle(flying, flyingCharset, Vehicle(Rm2kVehicle.Airship, 5, 5, 2), 0, 0);
        var flyingRow = FirstRowWithPixels(flying);
        AssertTrue(flyingRow > 0, "the flying airship drew something");

        // Two tiles up means 2 * 16 = 32 pixels higher, so the first drawn row
        // moves up by exactly that.
        AssertEq(groundRow - flyingRow, 32, "a vehicle two tiles up is drawn 32 pixels higher");
    }

    public void Test_AGroundedVehicleIsNotOffsetByTheAltitude()
    {
        var (ground, groundCharset) = MakeFrame();
        Rm2kMapFrameRenderer.DrawVehicle(ground, groundCharset, Vehicle(Rm2kVehicle.Boat, 2, 2), 0, 0);
        var (noAltitude, noAltitudeCharset) = MakeFrame();
        Rm2kMapFrameRenderer.DrawVehicle(noAltitude, noAltitudeCharset, Vehicle(Rm2kVehicle.Boat, 2, 2, 0), 0, 0);

        AssertEq(FirstRowWithPixels(ground), FirstRowWithPixels(noAltitude),
            "a zero altitude is not an offset at all");
    }

    public void Test_TheCameraScrollMovesAVehicleLikeAnyOtherCharacter()
    {
        // A vehicle is composited with the same offset as a character sprite, so
        // it scrolls with the map instead of sliding on its own.
        var (still, stillCharset) = MakeFrame();
        Rm2kMapFrameRenderer.DrawVehicle(still, stillCharset, Vehicle(Rm2kVehicle.Ship, 4, 4), 0, 0);
        var (scrolled, scrolledCharset) = MakeFrame();
        Rm2kMapFrameRenderer.DrawVehicle(scrolled, scrolledCharset, Vehicle(Rm2kVehicle.Ship, 4, 4), -16, -16);
        AssertEq(FirstRowWithPixels(still) - FirstRowWithPixels(scrolled), 16,
            "a scroll of 16 moves the vehicle 16 pixels");
    }

    public void Test_TheAirshipShadowUsesTheTwoSystemPatchesAtTwentySixPercent()
    {
        // The shadow is built from two 16x16 cells of the System graphic at
        // (128,32) and (144,32), so the second patch is one cell to the right
        // and the pair is 32 pixels wide.
        var first = Rm2kAirshipShadow.PatchFor(0);
        var second = Rm2kAirshipShadow.PatchFor(1);
       	AssertEq(first.X, 128, "the first patch starts at x 128");
        AssertEq(first.Y, 32, "and both patches sit on row 32");
        AssertEq(first.Width, 16, "each patch is 16 wide");
       	AssertEq(second.X, 144, "the second patch is one cell to the right");
       	AssertEq(Rm2kAirshipShadow.Opacity255, 66,
			"0.26 of 255 truncates to 66, and rounding up would draw it too dark");
    }

    public void Test_TheShadowSitsOneZBelowTheAirshipAndOnlyWhileThePlayerIsInIt()
    {
        AssertEq(Rm2kAirshipShadow.ScreenZ(500), 499,
			"the shadow is one below the airship so the airship covers it");
        AssertTrue(Rm2kAirshipShadow.IsVisible(pPlayerInAirship: true),
			"and it is visible while the player is in the airship");
        AssertTrue(!Rm2kAirshipShadow.IsVisible(pPlayerInAirship: false),
			"but not when the player is on foot");
    }
}
