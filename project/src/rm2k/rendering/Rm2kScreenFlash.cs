using System;

namespace UniversalRPG.Rm2k.Rendering;

/// <summary>
/// Draws a screen flash over a finished frame.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is the second effect that was state only:</strong> the flash
/// command was accepted, stored, counted down and saved, and no renderer file
/// referenced <c>FlashRed</c> or <c>FlashAlpha</c>, so a game that flashes its
/// screen showed nothing. Measured demand: 379 flash commands in one owned game
/// alone, every one of them inside RPG_RT's 0 to 31.
/// </para>
/// <para>
/// <strong>And the arithmetic is the reference's</strong>: EasyRPG's
/// <c>Flash::MakeColor(r, g, b, level)</c> is
/// <c>Color(r * 8, g * 8, b * 8, level * 8)</c>, and that colour is drawn over the
/// screen -- a source-over blend of one solid colour, not a per-channel tone. The
/// level is the presentation's decaying one and not the command's own value, which
/// is what makes a flash fade out over its frames.
/// </para>
/// </remarks>
public static class Rm2kScreenFlashPainter
{
    /// <summary>The factor <c>Flash::MakeColor</c> scales every value by.</summary>
    public const int Scale = 8;

    /// <summary>
    /// Blends a flash over a frame's pixels, leaving the alpha channel alone.
    /// </summary>
    /// <param name="pFrame">The finished frame.</param>
    /// <param name="pRed">Red, in RPG_RT's 0 to 31.</param>
    /// <param name="pGreen">Green, in RPG_RT's 0 to 31.</param>
    /// <param name="pBlue">Blue, in RPG_RT's 0 to 31.</param>
    /// <param name="pLevel">The flash's level right now, in RPG_RT's 0 to 31.</param>
    /// <exception cref="ArgumentNullException">The frame is null.</exception>
    public static void Apply(
        Rm2kPixelBuffer pFrame, int pRed, int pGreen, int pBlue, double pLevel)
    {
        ArgumentNullException.ThrowIfNull(pFrame);
        // **And a level of zero draws nothing at all**: the reference's MakeColor
        // returns an empty colour there, so there is no blend to make and no byte
        // to touch.
        if (pLevel <= 0)
        {
            return;
        }

        var alpha = (int)(pLevel * Scale);
        if (alpha <= 0)
        {
            return;
        }
        alpha = alpha > 255 ? 255 : alpha;
        var red = ScaleChannel(pRed);
        var green = ScaleChannel(pGreen);
        var blue = ScaleChannel(pBlue);
        var inverse = 255 - alpha;

        var pixels = pFrame.Pixels;
        for (var offset = 0; offset + 3 < pixels.Length; offset += 4)
        {
            pixels[offset] = (byte)((pixels[offset] * inverse + red * alpha) / 255);
            pixels[offset + 1] = (byte)((pixels[offset + 1] * inverse + green * alpha) / 255);
            pixels[offset + 2] = (byte)((pixels[offset + 2] * inverse + blue * alpha) / 255);
        }
    }

    /// <summary>
    /// Scales one flash channel the way the reference does, without overflowing a
    /// byte.
    /// </summary>
    /// <param name="pValue">The channel, 0 to 31.</param>
    /// <returns>The scaled channel.</returns>
    /// <remarks>
    /// <strong>And the clamp is not decoration</strong>: 31 times 8 is 248 and
    /// fits, but a value a game should not write would wrap a byte in C# rather
    /// than saturate, and the reference's own <c>Color</c> saturates.
    /// </remarks>
    private static int ScaleChannel(int pValue)
    {
        var scaled = pValue * Scale;
        return scaled < 0 ? 0 : scaled > 255 ? 255 : scaled;
    }
}
