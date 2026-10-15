using System;

using UniversalRPG.Rm2k.Presentation;
using UniversalRPG.Rm2k.Rendering;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The screen flash: RPG_RT's own range, the decaying level, and the pixels.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is the second effect that was state only:</strong> the flash
/// was accepted, stored, counted down and saved, and no renderer file referenced
/// <c>FlashRed</c> or <c>FlashAlpha</c>, so a game that flashes its screen showed
/// nothing. Measured demand: 379 flash commands in one owned game alone.
/// </para>
/// <para>
/// <strong>And the arithmetic is EasyRPG's <c>src/flash.h</c></strong>:
/// <c>Update</c> decays the level with <c>level -= level / time_left</c> and
/// <c>MakeColor</c> scales every value by eight.
/// </para>
/// </remarks>
public partial class TestRm2kFlash : TestBase
{
    private static Rm2kPixelBuffer Frame(byte pRed, byte pGreen, byte pBlue)
    {
        var frame = new Rm2kPixelBuffer(2, 1);
        frame.TrySetPixel(0, 0, pRed, pGreen, pBlue, 255);
        frame.TrySetPixel(1, 0, pRed, pGreen, pBlue, 255);
        return frame;
    }

    /// <summary>
    /// A flash starts at the command's level and decays to zero over its frames.
    /// </summary>
    /// <remarks>
    /// <strong>And the decay is the reference's own</strong>: with a level of 31
    /// over four frames it reads 23.25, 15.5, 7.75 and then 0, because the last
    /// step divides by one. A reader that counted frames down without decaying
    /// would flash at full strength until the frame it vanished.
    /// </remarks>
    public void Test_EinFlashKlingtUeberSeineFramesAb()
    {
        var presentation = new PresentationState();
        AssertTrue(presentation.FlashOnce(31, 31, 31, 31, 4),
            "**and the flash of four frames is accepted**");
        AssertEq(presentation.FlashCurrentLevel, 31.0,
            "**and it starts at the command's own level**");

        presentation.Tick(1);
        AssertEq(presentation.FlashCurrentLevel, 31.0 - 31.0 / 4,
            "**and one frame takes a quarter of the level away**");
        presentation.Tick(1);
        AssertEq(presentation.FlashCurrentLevel, 23.25 - 23.25 / 3);
        presentation.Tick(1);
        AssertEq(presentation.FlashCurrentLevel, 15.5 - 15.5 / 2);
        presentation.Tick(1);
        AssertEq(presentation.FlashCurrentLevel, 0.0,
            "**and the last frame takes the rest**, because it divides by one");
        AssertEq(presentation.IsFlashActive, false, "and the flash is over");
        AssertEq(presentation.FlashFramesRemaining, 0);
    }

    /// <summary>
    /// The flash's range is RPG_RT's 0 to 31 and not a colour channel's.
    /// </summary>
    /// <remarks>
    /// <strong>And the measurement is what pins it</strong>: all 379 flash
    /// commands of the owned game stay inside 0 to 31, and <c>MakeColor</c> scales
    /// each value by eight. A bound of 255 would accept what the editor cannot
    /// write.
    /// </remarks>
    public void Test_DieFlashRangeIstNullBisEinunddreissig()
    {
        var presentation = new PresentationState();
        AssertTrue(presentation.FlashOnce(31, 31, 31, 31, 10),
            "**and thirty one is accepted**, because it is white at full strength");
        AssertTrue(!presentation.FlashOnce(32, 31, 31, 31, 10),
            "**and thirty two is refused**, because the editor cannot write it");
        AssertTrue(!presentation.FlashOnce(31, 31, 31, 32, 10),
            "and so is a level of thirty two");
        AssertTrue(!presentation.FlashOnce(-1, 0, 0, 0, 10),
            "and a negative value");
    }

    /// <summary>
    /// A flash of no level touches no byte at all.
    /// </summary>
    /// <remarks>
    /// <strong>And that is what keeps every existing rendering test honest</strong>:
    /// the reference's <c>MakeColor</c> returns an empty colour at a level of
    /// zero, so there is no blend to make.
    /// </remarks>
    public void Test_EinFlashOhneLevelBeruehrtKeinByte()
    {
        var frame = Frame(37, 200, 5);
        var before = (byte[])frame.Pixels.Clone();

        Rm2kScreenFlashPainter.Apply(frame, 31, 31, 31, 0.0);

        for (var index = 0; index < before.Length; index++)
        {
            AssertEq(frame.Pixels[index], before[index],
                "**and byte " + index + " is untouched**");
        }
    }

    /// <summary>
    /// The blend is the reference's scale and a source-over mix.
    /// </summary>
    /// <remarks>
    /// <strong>And both numbers are written out here</strong> rather than taken
    /// from the painter: <c>MakeColor</c> scales the colour and the level by eight,
    /// and the colour is then mixed over the frame with the scaled level as its
    /// alpha.
    /// </remarks>
    public void Test_DerFlashMischtSeineFarbeUeberDenFrame()
    {
        // A white flash at full strength: alpha 248 and a source of 248.
        var white = Frame(10, 20, 30);
        Rm2kScreenFlashPainter.Apply(white, 31, 31, 31, 31.0);
        AssertEq(white.Pixels[0], (10 * (255 - 248) + 248 * 248) / 255,
            "**and the red is the frame mixed with the flash's own red**");
        AssertEq(white.Pixels[1], (20 * (255 - 248) + 248 * 248) / 255);
        AssertEq(white.Pixels[2], (30 * (255 - 248) + 248 * 248) / 255);
        AssertEq(white.Pixels[3], 255,
            "**and the alpha channel is not the flash's business**");

        // A red flash at a level of eight: alpha 64, and only the red channel is
        // lit.
        var red = Frame(10, 20, 30);
        Rm2kScreenFlashPainter.Apply(red, 31, 0, 0, 8.0);
        AssertEq(red.Pixels[0], (10 * (255 - 64) + 248 * 64) / 255,
            "**and a red flash lifts the red channel**");
        AssertEq(red.Pixels[1], (20 * (255 - 64) + 0 * 64) / 255,
            "**and leaves the green where it was**");
        AssertEq(red.Pixels[2], (30 * (255 - 64) + 0 * 64) / 255,
            "and the blue too");
    }
}
