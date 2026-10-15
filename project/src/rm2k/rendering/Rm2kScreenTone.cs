using System;

namespace UniversalRPG.Rm2k.Rendering;

/// <summary>
/// The screen tone of an RM2K frame, in the engine's own units.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the two scales are not the same scale.</strong> The event
/// command writes <strong>0 to 200 with 100 as the neutral value</strong> --
/// measured over the owned collection, <c>11030</c> carries
/// <c>[200,200,200,200]</c>, <c>[100,100,100,100]</c> and <c>[200,0,0,200]</c> --
/// while the engine's tone works in <strong>0 to 255 with 128 as neutral</strong>.
/// EasyRPG's <c>Game_Screen::GetTone()</c> is the conversion:
/// </para>
/// <code>
/// Tone((int)(tint_current_red * 128 / 100), ... , (int)(tint_current_sat * 128 / 100))
/// </code>
/// <para>
/// <strong>And the neutral value is 128 rather than 100 in both scales</strong>,
/// which is why a command of 100 is a no-op and not a slight tint.
/// </para>
/// <para>
/// <strong>And the top of the command range overflows the engine's table:</strong>
/// 200 becomes 256, while the hard-light lookup the colour tone indexes is 256
/// entries wide, so the reference reads one past its own table there. This type
/// clamps to 255 and says so instead of reproducing the read.
/// </para>
/// </remarks>
public readonly struct Rm2kScreenTone
{
    /// <summary>The tone value that changes nothing.</summary>
    public const int Neutral = 128;

    /// <summary>The command value that changes nothing.</summary>
    public const int CommandNeutral = 100;

    /// <summary>The top of the command's range.</summary>
    public const int MaxCommandValue = 200;

    /// <summary>The top of the tone's own range, which the lookup table is wide enough for.</summary>
    public const int MaxToneValue = 255;

    /// <summary>Creates a tone from the engine's own units.</summary>
    /// <param name="pRed">Red.</param>
    /// <param name="pGreen">Green.</param>
    /// <param name="pBlue">Blue.</param>
    /// <param name="pSaturation">Saturation, called <c>gray</c> by the engine.</param>
    public Rm2kScreenTone(int pRed, int pGreen, int pBlue, int pSaturation)
    {
        Red = Clamp(pRed);
        Green = Clamp(pGreen);
        Blue = Clamp(pBlue);
        Saturation = Clamp(pSaturation);
    }

    /// <summary>Red, in the tone's own 0 to 255 range.</summary>
    public int Red { get; }

    /// <summary>Green, in the tone's own 0 to 255 range.</summary>
    public int Green { get; }

    /// <summary>Blue, in the tone's own 0 to 255 range.</summary>
    public int Blue { get; }

    /// <summary>Saturation, in the tone's own 0 to 255 range.</summary>
    public int Saturation { get; }

    /// <summary>Whether this tone would leave every pixel alone.</summary>
    public bool IsNeutral =>
        Red == Neutral && Green == Neutral && Blue == Neutral && Saturation == Neutral;

    /// <summary>
    /// Converts a tint command's four parameters into a tone.
    /// </summary>
    /// <param name="pRed">The command's red, 0 to 200.</param>
    /// <param name="pGreen">The command's green, 0 to 200.</param>
    /// <param name="pBlue">The command's blue, 0 to 200.</param>
    /// <param name="pSaturation">The command's saturation, 0 to 200.</param>
    /// <returns>The tone.</returns>
    /// <remarks>
    /// <strong>And it is the reference's own conversion</strong>, including the
    /// integer division that makes 200 land on 256 before clamping.
    /// </remarks>
    public static Rm2kScreenTone FromCommand(
        int pRed, int pGreen, int pBlue, int pSaturation) =>
        new(pRed * Neutral / CommandNeutral,
            pGreen * Neutral / CommandNeutral,
            pBlue * Neutral / CommandNeutral,
            pSaturation * Neutral / CommandNeutral);

    /// <summary>The tone a game starts with, from the engine's map change.</summary>
    /// <remarks>
    /// <strong>And it is the neutral tone and not zero</strong>: EasyRPG's
    /// <c>Game_Screen::OnMapChange</c> resets a negative tint -- the state before
    /// the first tint command -- to exactly 100 in the command's scale. A game
    /// that started at zero would begin with a black screen.
    /// </remarks>
    public static Rm2kScreenTone NeutralTone =>
        FromCommand(CommandNeutral, CommandNeutral, CommandNeutral, CommandNeutral);

    private static int Clamp(int pValue) =>
        pValue < 0 ? 0 : pValue > MaxToneValue ? MaxToneValue : pValue;
}

/// <summary>
/// Applies a screen tone to a finished frame.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is the pass that was missing:</strong> the tint command was
/// accepted, stored, counted down and saved, and no renderer file referenced it,
/// so a game that tints its screen showed an untinted screen. Measured demand:
/// 2,924 <c>11030</c> instances over the owned collection, and one game tints
/// from a parallel page 150 times in 300 frames.
/// </para>
/// <para>
/// <strong>The order and the arithmetic are the reference's</strong>
/// (<c>Bitmap::ToneBlit</c>, <c>saturation_tone</c>, <c>color_tone</c>): the
/// saturation runs first and the colour second, the saturation is a Y'CbCr
/// scaling, and the colour is a hard-light lookup. A neutral tone returns before
/// touching a single byte, which is what keeps every existing rendering test's
/// pixels exactly as they were.
/// </para>
/// </remarks>
public static class Rm2kScreenTonePainter
{
    private static readonly byte[,] HardLight = BuildHardLight();

    /// <summary>
    /// Applies a tone to a frame's pixels, leaving the alpha channel alone.
    /// </summary>
    /// <param name="pFrame">The finished frame.</param>
    /// <param name="pTone">The tone.</param>
    /// <exception cref="ArgumentNullException">The frame is null.</exception>
    public static void Apply(Rm2kPixelBuffer pFrame, Rm2kScreenTone pTone)
    {
        ArgumentNullException.ThrowIfNull(pFrame);
        // **And the reference does the same**: a neutral tone blits the source
        // unchanged, so nothing is recomputed and nothing can drift.
        if (pTone.IsNeutral)
        {
            return;
        }

        var pixels = pFrame.Pixels;
        var applySaturation = pTone.Saturation != Rm2kScreenTone.Neutral;
        var applyColor = pTone.Red != Rm2kScreenTone.Neutral
            || pTone.Green != Rm2kScreenTone.Neutral
            || pTone.Blue != Rm2kScreenTone.Neutral;
        var saturation = pTone.Saturation > Rm2kScreenTone.Neutral
            ? 1024 + (pTone.Saturation - Rm2kScreenTone.Neutral) * 16
            : pTone.Saturation * 8;

        for (var offset = 0; offset + 3 < pixels.Length; offset += 4)
        {
            int red = pixels[offset];
            int green = pixels[offset + 1];
            int blue = pixels[offset + 2];

            if (applySaturation)
            {
                // Y' = 0.299 R' + 0.587 G' + 0.114 B', then the chroma is scaled
                // away from that luminance.
                var luminance = (7471 * blue + 38470 * green + 19595 * red) >> 16;
                red = ClampByte((luminance * 1024 + (red - luminance) * saturation) >> 10);
                green = ClampByte((luminance * 1024 + (green - luminance) * saturation) >> 10);
                blue = ClampByte((luminance * 1024 + (blue - luminance) * saturation) >> 10);
            }

            if (applyColor)
            {
                red = HardLight[pTone.Red, red];
                green = HardLight[pTone.Green, green];
                blue = HardLight[pTone.Blue, blue];
            }

            pixels[offset] = (byte)red;
            pixels[offset + 1] = (byte)green;
            pixels[offset + 2] = (byte)blue;
        }
    }

    /// <summary>
    /// The reference's hard-light table, built once.
    /// </summary>
    /// <returns>The table, indexed by tone value and then by channel value.</returns>
    /// <remarks>
    /// <strong>And the branch at 128 is what makes 128 the neutral value:</strong>
    /// below it the channel is multiplied by the tone, above it the complement is
    /// multiplied by the complement and subtracted from white.
    /// </remarks>
    private static byte[,] BuildHardLight()
    {
        var table = new byte[Rm2kScreenTone.MaxToneValue + 1, 256];
        for (var tone = 0; tone <= Rm2kScreenTone.MaxToneValue; tone++)
        {
            for (var channel = 0; channel < 256; channel++)
            {
                var result = tone <= Rm2kScreenTone.Neutral
                    ? 2 * tone * channel / 255
                    : 255 - 2 * (255 - tone) * (255 - channel) / 255;
                table[tone, channel] = (byte)ClampByte(result);
            }
        }
        return table;
    }

    private static int ClampByte(int pValue) =>
        pValue < 0 ? 0 : pValue > 255 ? 255 : pValue;
}
