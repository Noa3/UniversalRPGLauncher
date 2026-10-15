using System;
using System.Collections.Generic;

using UniversalRPG.Rm2k.Presentation;
using UniversalRPG.Rm2k.Rendering;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The screen tone: the command's scale, the engine's scale, and the pixels.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is the pass that was missing entirely:</strong> no renderer
/// file referenced <c>TintRed</c>, <c>TintGreen</c>, <c>TintBlue</c> or
/// <c>TintSaturation</c>, so a game that tints its screen -- 2,924 tint commands
/// over the owned collection, one game issuing them 150 times in 300 frames --
/// showed an untinted screen.
/// </para>
/// <para>
/// <strong>And the arithmetic is not invented here</strong>: it is EasyRPG's
/// <c>Game_Screen::GetTone()</c> for the conversion, and
/// <c>Bitmap::ToneBlit</c> with <c>saturation_tone</c> and <c>color_tone</c> for
/// the pixels.
/// </para>
/// </remarks>
public partial class TestRm2kBildschirmton : TestBase
{
    /// <summary>A frame of one colour, so a tone's effect is readable.</summary>
    private static Rm2kPixelBuffer Frame(
        byte pRed, byte pGreen, byte pBlue, byte pAlpha = 255, int pPixels = 4)
    {
        var frame = new Rm2kPixelBuffer(pPixels, 1);
        for (var index = 0; index < pPixels; index++)
        {
            frame.TrySetPixel(index, 0, pRed, pGreen, pBlue, pAlpha);
        }
        return frame;
    }

    private static (byte Red, byte Green, byte Blue, byte Alpha) PixelAt(
        Rm2kPixelBuffer pFrame, int pIndex)
    {
        var offset = pIndex * 4;
        return (pFrame.Pixels[offset], pFrame.Pixels[offset + 1],
            pFrame.Pixels[offset + 2], pFrame.Pixels[offset + 3]);
    }

    /// <summary>
    /// The command's hundred is the tone's hundred and twenty eight.
    /// </summary>
    /// <remarks>
    /// <strong>And this is the conversion the reference makes</strong>
    /// (<c>value * 128 / 100</c>): the command writes 0 to 200 with 100 as the
    /// neutral value, the tone works in 0 to 255 with 128 as neutral, and 100
    /// therefore lands exactly on 128 and changes nothing.
    /// </remarks>
    public void Test_BefehlshundertIstDerNeutraleTon()
    {
        var neutral = Rm2kScreenTone.FromCommand(100, 100, 100, 100);
        AssertEq(neutral.Red, 128, "**and the command's 100 is the tone's 128**");
        AssertEq(neutral.Green, 128);
        AssertEq(neutral.Blue, 128);
        AssertEq(neutral.Saturation, 128);
        AssertTrue(neutral.IsNeutral,
            "**and it is the tone that changes nothing**");

        var black = Rm2kScreenTone.FromCommand(0, 0, 0, 100);
        AssertEq(black.Red, 0, "and the command's 0 is the tone's 0");
        AssertTrue(!black.IsNeutral, "and it is not neutral");

        // **And the top of the range overflows the reference's table**: 200 * 128
        // / 100 is 256, while the hard-light lookup is 256 entries wide, so the
        // engine reads one past its own table there. This clamps to 255.
        var brightest = Rm2kScreenTone.FromCommand(200, 200, 200, 200);
        AssertEq(brightest.Red, 255,
            "**and the command's 200 is clamped to the tone's 255**, because"
                + " 200 * 128 / 100 is 256 and the lookup table is 256 wide");
    }

    /// <summary>
    /// A neutral tone leaves every byte of the frame alone.
    /// </summary>
    /// <remarks>
    /// <strong>And this is what keeps every existing rendering test honest:</strong>
    /// a frame nobody tinted has to come out byte for byte as it was, or the pass
    /// would have changed every map in the repository.
    /// </remarks>
    public void Test_EinNeutralerTonLaesstJedenByteGleich()
    {
        var frame = Frame(37, 200, 5);
        var before = (byte[])frame.Pixels.Clone();

        Rm2kScreenTonePainter.Apply(frame, Rm2kScreenTone.NeutralTone);

        for (var index = 0; index < before.Length; index++)
        {
            AssertEq(frame.Pixels[index], before[index],
                "**and byte " + index + " is untouched**");
        }
    }

    /// <summary>
    /// A tone of zero makes the frame black and leaves the alpha channel alone.
    /// </summary>
    public void Test_EinTonVonNullMachtDenFrameSchwarz()
    {
        var frame = Frame(200, 100, 50, 128);
        Rm2kScreenTonePainter.Apply(frame, Rm2kScreenTone.FromCommand(0, 0, 0, 100));

        var pixel = PixelAt(frame, 0);
        AssertEq(pixel.Red, 0, "**and red is zero**");
        AssertEq(pixel.Green, 0, "and green is zero");
        AssertEq(pixel.Blue, 0, "and blue is zero");
        AssertEq(pixel.Alpha, 128,
            "**and the alpha channel is not the tone's business**");
    }

    /// <summary>
    /// The colour follows the reference's hard-light table.
    /// </summary>
    /// <remarks>
    /// <strong>And the two branches are what make 128 neutral</strong>: at or
    /// below it the channel is multiplied by the tone and divided by 255, above it
    /// the complements are multiplied and subtracted from white. Both are checked
    /// here against arithmetic written out in the test, not against the table.
    /// </remarks>
    public void Test_DieFarbeFolgtDerHardLightFormel()
    {
        var darker = Frame(200, 200, 200);
        Rm2kScreenTonePainter.Apply(darker, new Rm2kScreenTone(64, 64, 64, 128));
        AssertEq(PixelAt(darker, 0).Red, 2 * 64 * 200 / 255,
            "**and 200 under a tone of 64 is 2 * 64 * 200 / 255**");

        var brighter = Frame(200, 200, 200);
        Rm2kScreenTonePainter.Apply(brighter, new Rm2kScreenTone(192, 192, 192, 128));
        AssertEq(PixelAt(brighter, 0).Red, 255 - 2 * (255 - 192) * (255 - 200) / 255,
            "**and under a tone of 192 it is the complement form**");
    }

    /// <summary>
    /// A saturation of zero takes the colour out and leaves the luminance.
    /// </summary>
    /// <remarks>
    /// <strong>And the luminance is the reference's own integer form</strong>:
    /// <c>(7471 * b + 38470 * g + 19595 * r) &gt;&gt; 16</c>, which for pure red is
    /// 76 rather than the 0.299 * 255 a float would give.
    /// </remarks>
    public void Test_DieSaettigungNimmtDieFarbeHeraus()
    {
        var frame = Frame(255, 0, 0);
        Rm2kScreenTonePainter.Apply(frame, Rm2kScreenTone.FromCommand(100, 100, 100, 0));

        var luminance = (19595 * 255) >> 16;
        AssertEq(luminance, 76, "**and pure red's luminance is 76**");
        var pixel = PixelAt(frame, 0);
        AssertEq(pixel.Red, luminance, "and the red becomes that luminance");
        AssertEq(pixel.Green, luminance, "and so does the green");
        AssertEq(pixel.Blue, luminance, "and so does the blue");
    }

    /// <summary>
    /// The saturation runs before the colour, not after it.
    /// </summary>
    /// <remarks>
    /// <strong>And the two orders are distinguishable</strong>: with the
    /// saturation first, a red pixel becomes its luminance and the red tone then
    /// scales that luminance; with the colour first, the red tone would push the
    /// channel to white before the desaturation, and the grey would be built from
    /// a value that never existed.
    /// </remarks>
    public void Test_DieSaettigungLaeuftVorDerFarbe()
    {
        var frame = Frame(255, 0, 0);
        Rm2kScreenTonePainter.Apply(frame, new Rm2kScreenTone(128, 0, 0, 0));

        var luminance = (19595 * 255) >> 16;
        var expected = 2 * 128 * luminance / 255;
        var pixel = PixelAt(frame, 0);
        AssertEq(pixel.Red, expected,
            "**and the red is the luminance scaled by the tone**");
        AssertEq(pixel.Green, 0,
            "**and the green is gone**, which is what a colour tone of zero does"
                + " after the desaturation");
        AssertEq(pixel.Blue, 0, "and the blue is gone too");
    }
}
