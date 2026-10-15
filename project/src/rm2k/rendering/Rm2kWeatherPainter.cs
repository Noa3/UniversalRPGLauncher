using System;

using UniversalRPG.Rm2k.Presentation;

namespace UniversalRPG.Rm2k.Rendering;

/// <summary>
/// Draws the weather over a finished frame, from EasyRPG's <c>src/weather.cpp</c>.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And nothing drew the weather at all</strong>, which is the defect this file
/// closes: the type and the strength were set, clamped and saved, and no renderer file
/// read them, so a game that turned on rain showed a dry screen. Measured demand: one
/// <c>11070</c> in the whole owned collection.
/// </para>
/// <para>
/// <strong>And the particles need no asset</strong>: the reference builds them in code
/// -- a 6x24 streak with one white pixel per row for rain, a 2x2 white square for snow,
/// a 16x16 tile of random greys for fog and sand, and a 1x2 pixel in four colours for
/// the sandstorm -- which is why this is a slice and not an RTP dependency.
/// </para>
/// <para>
/// <strong>And the fog tile's randomness is seeded and not drawn from the engine's
/// stream</strong>, so the same frame is the same picture twice. The colours, the sizes
/// and the opacities are the reference's; only the source of the noise differs, and it
/// is stated here rather than hidden.
/// </para>
/// </remarks>
public static class Rm2kWeatherPainter
{
    /// <summary>The weather surface's width, which is <c>GetPanLimitX() / 16</c>.</summary>
    public const int SurfaceWidth = 320;

    /// <summary>The weather surface's height, which is <c>GetPanLimitY() / 16</c>.</summary>
    public const int SurfaceHeight = 160;

    /// <summary>The fog and sand overlay's size, in pixels.</summary>
    public const int OverlaySize = 16;

    /// <summary>Rain's particle, in pixels: six wide and twenty four tall.</summary>
    public const int RainWidth = 6;

    /// <summary>Rain's particle height.</summary>
    public const int RainHeight = 24;

    /// <summary>The fog overlay's three greys.</summary>
    public static readonly (byte R, byte G, byte B)[] FogOverlayColors =
        { (230, 230, 230), (240, 240, 240), (255, 255, 255) };

    /// <summary>The sand overlay's three tans.</summary>
    public static readonly (byte R, byte G, byte B)[] SandOverlayColors =
        { (220, 220, 160), (230, 230, 170), (240, 240, 180) };

    /// <summary>The sand particle's four colours, one per two pixel band.</summary>
    public static readonly (byte R, byte G, byte B)[] SandParticleColors =
        { (255, 255, 32), (255, 180, 32), (255, 32, 32), (255, 255, 240) };

    /// <summary>
    /// The two fog layers' opacities by strength, the reference's <c>fog_opacity</c>.
    /// </summary>
    public static readonly int[,] FogOpacity = { { 32, 64, 96, 0 }, { 64, 80, 160, 255 } };

    /// <summary>
    /// Draws the current weather over a frame.
    /// </summary>
    /// <param name="pFrame">The composed frame.</param>
    /// <param name="pWeather">The weather's particles.</param>
    /// <param name="pType">The weather type.</param>
    /// <param name="pStrength">The weather strength.</param>
    /// <param name="pPanX">The camera pan in sub-units.</param>
    /// <param name="pPanY">The camera pan in sub-units, vertically.</param>
    /// <param name="pShakeX">The shake offset in pixels.</param>
    /// <param name="pShakeY">The shake offset in pixels, vertically.</param>
    /// <param name="pOverlaySeed">The seed for the fog and sand overlay's noise.</param>
    /// <exception cref="ArgumentNullException">The frame or the weather is null.</exception>
    public static void Apply(
        Rm2kPixelBuffer pFrame,
        Rm2kWeatherState pWeather,
        int pType,
        int pStrength,
        int pPanX,
        int pPanY,
        int pShakeX,
        int pShakeY,
        int pOverlaySeed)
    {
        ArgumentNullException.ThrowIfNull(pFrame);
        ArgumentNullException.ThrowIfNull(pWeather);
        switch (pType)
        {
            case Rm2kWeatherState.TypeRain:
            case Rm2kWeatherState.TypeSnow:
                ApplyRainOrSnow(pFrame, pWeather, pType, pStrength, pPanX, pPanY, pShakeX, pShakeY);
                break;
            case Rm2kWeatherState.TypeFog:
                ApplyFog(pFrame, pWeather, pStrength, pShakeX, pShakeY, pOverlaySeed);
                break;
            case Rm2kWeatherState.TypeSandstorm:
                // **And a sandstorm is the fog plus its own particles**, which is the
                // reference's own shape: DrawSandstorm runs the fog overlay and then
                // the sand.
                ApplyFog(pFrame, pWeather, pStrength, pShakeX, pShakeY, pOverlaySeed);
                ApplySand(pFrame, pWeather, pStrength, pOverlaySeed);
                break;
        }
    }

    /// <summary>Draws rain or snow: particles on a surface, tiled onto the frame.</summary>
    private static void ApplyRainOrSnow(
        Rm2kPixelBuffer pFrame,
        Rm2kWeatherState pWeather,
        int pType,
        int pStrength,
        int pPanX,
        int pPanY,
        int pShakeX,
        int pShakeY)
    {
        var count = Rm2kWeatherState.DrawnParticles(pType, pStrength);
        // **And the two types differ in three numbers**: rain lives twelve frames and
        // is drawn from an alpha base of five, snow lives thirty from seven.
        var tmax = pType == Rm2kWeatherState.TypeRain ? 12 : 30;
        var abase = pType == Rm2kWeatherState.TypeRain ? 5 : 7;

        var surface = new Rm2kPixelBuffer(SurfaceWidth, SurfaceHeight);
        for (var index = 0; index < count && index < pWeather.Particles.Count; index++)
        {
            var particle = pWeather.Particles[index];
            if (particle.T > tmax)
            {
                continue;
            }
            var alpha = Math.Min((abase + pStrength) * particle.T, 255);
            if (alpha <= 0)
            {
                continue;
            }
            DrawParticle(surface, pType, particle.X, particle.Y, alpha);
        }

        // **And the surface is tiled onto the frame**, which is the reference's
        // TiledBlit: it is one screen wide and two thirds of one tall, so a particle
        // that falls off the bottom comes back at the top.
        var offsetX = pShakeX - pPanX / 16;
        var offsetY = pShakeY - pPanY / 16;
        var pixels = pFrame.Pixels;
        for (var y = 0; y < pFrame.Height; y++)
        {
            var sourceY = PositiveModulo(y - offsetY, SurfaceHeight);
            for (var x = 0; x < pFrame.Width; x++)
            {
                var sourceX = PositiveModulo(x - offsetX, SurfaceWidth);
                BlendOver(pixels, (y * pFrame.Width + x) * 4, surface.Pixels, (sourceY * SurfaceWidth + sourceX) * 4, 255);
            }
        }
    }

    /// <summary>Draws one rain or snow particle onto the surface, edge mirrored.</summary>
    private static void DrawParticle(
        Rm2kPixelBuffer pSurface, int pType, int pX, int pY, int pAlpha)
    {
        if (pType == Rm2kWeatherState.TypeRain)
        {
            // **And the streak is a diagonal**: one white pixel per row at
            // x = width - (y / 4) - 1, which is the reference's own arithmetic.
            for (var row = 0; row < RainHeight; row++)
            {
                var column = RainWidth - row / 4 - 1;
                SetMirrored(pSurface, pX + column, pY + row, 255, 255, 255, pAlpha);
            }
            return;
        }
        for (var row = 0; row < 2; row++)
        {
            for (var column = 0; column < 2; column++)
            {
                SetMirrored(pSurface, pX + column, pY + row, 255, 255, 255, pAlpha);
            }
        }
    }

    /// <summary>Draws the two fog layers, tiled over the frame.</summary>
    private static void ApplyFog(
        Rm2kPixelBuffer pFrame,
        Rm2kWeatherState pWeather,
        int pStrength,
        int pShakeX,
        int pShakeY,
        int pOverlaySeed)
    {
        var overlay = BuildOverlay(FogOverlayColors, pOverlaySeed);
        var strength = Math.Clamp(pStrength, 0, Rm2kWeatherState.NumStrength);
        var backOpacity = FogOpacity[0, strength];
        var frontOpacity = FogOpacity[1, strength];

        // RPG_RT uses the first two particles as the fog layers' frame counters.
        var background = pWeather.Particles.Count > 0 ? pWeather.Particles[0].X : 0;
        var foreground = pWeather.Particles.Count > 1 ? pWeather.Particles[1].X : 0;

        // The front layer moves left one pixel every eight frames; the back layer one
        // every four; and only the front moves vertically, from the background's own
        // counter.
        var frontX = pShakeX + foreground / 8 % OverlaySize;
        var backX = pShakeX - background / 4 % OverlaySize;
        var frontY = pShakeY
            - (int)Math.Round(Math.Sin(background * Math.PI / 4096.0) * (OverlaySize / 2.0))
            - OverlaySize / 4;
        var backY = pShakeY;

        TileOver(pFrame, overlay, backX, backY, backOpacity);
        TileOver(pFrame, overlay, frontX, frontY, frontOpacity);
    }

    /// <summary>Draws the sandstorm's particles, which are plain blits.</summary>
    private static void ApplySand(
        Rm2kPixelBuffer pFrame, Rm2kWeatherState pWeather, int pStrength, int pOverlaySeed)
    {
        _ = pOverlaySeed;
        var count = Rm2kWeatherState.DrawnParticles(Rm2kWeatherState.TypeSandstorm, pStrength);
        var pixels = pFrame.Pixels;
        for (var index = 0; index < count && index < pWeather.Particles.Count; index++)
        {
            var particle = pWeather.Particles[index];
            var colour = SandParticleColors[index % SandParticleColors.Length];
            for (var row = 0; row < 2; row++)
            {
                var y = particle.Y + row;
                if (y < 0 || y >= pFrame.Height)
                {
                    continue;
                }
                var x = particle.X;
                if (x < 0 || x >= pFrame.Width)
                {
                    continue;
                }
                var offset = (y * pFrame.Width + x) * 4;
                var alpha = Math.Clamp(particle.Alpha, 0, 255);
                pixels[offset] = (byte)((pixels[offset] * (255 - alpha) + colour.R * alpha) / 255);
                pixels[offset + 1] = (byte)((pixels[offset + 1] * (255 - alpha) + colour.G * alpha) / 255);
                pixels[offset + 2] = (byte)((pixels[offset + 2] * (255 - alpha) + colour.B * alpha) / 255);
            }
        }
    }

    /// <summary>Builds the 16x16 overlay tile from three colours and a seed.</summary>
    private static byte[] BuildOverlay((byte R, byte G, byte B)[] pColours, int pSeed)
    {
        var tile = new byte[OverlaySize * OverlaySize * 4];
        var state = pSeed == 0 ? 1u : (uint)pSeed;
        for (var index = 0; index < OverlaySize * OverlaySize; index++)
        {
            // A small deterministic stream, so the same seed is the same fog.
            state = state * 1664525u + 1013904223u;
            var pick = (int)(state >> 16) % pColours.Length;
            tile[index * 4] = pColours[pick].R;
            tile[index * 4 + 1] = pColours[pick].G;
            tile[index * 4 + 2] = pColours[pick].B;
            tile[index * 4 + 3] = 255;
        }
        return tile;
    }

    /// <summary>Tiles an overlay over the frame at an offset and an opacity.</summary>
    private static void TileOver(
        Rm2kPixelBuffer pFrame, byte[] pOverlay, int pOffsetX, int pOffsetY, int pOpacity)
    {
        if (pOpacity <= 0)
        {
            return;
        }
        var pixels = pFrame.Pixels;
        for (var y = 0; y < pFrame.Height; y++)
        {
            var sourceY = PositiveModulo(y - pOffsetY, OverlaySize);
            for (var x = 0; x < pFrame.Width; x++)
            {
                var sourceX = PositiveModulo(x - pOffsetX, OverlaySize);
                BlendOver(pixels, (y * pFrame.Width + x) * 4, pOverlay, (sourceY * OverlaySize + sourceX) * 4, pOpacity);
            }
        }
    }

    /// <summary>Blends one source pixel over one destination pixel.</summary>
    private static void BlendOver(
        byte[] pDestination, int pDestinationOffset, byte[] pSource, int pSourceOffset, int pOpacity)
    {
        var sourceAlpha = pSource[pSourceOffset + 3];
        if (sourceAlpha == 0)
        {
            return;
        }
        var alpha = sourceAlpha * pOpacity / 255;
        if (alpha <= 0)
        {
            return;
        }
        var inverse = 255 - alpha;
        pDestination[pDestinationOffset] =
            (byte)((pDestination[pDestinationOffset] * inverse + pSource[pSourceOffset] * alpha) / 255);
        pDestination[pDestinationOffset + 1] =
            (byte)((pDestination[pDestinationOffset + 1] * inverse + pSource[pSourceOffset + 1] * alpha) / 255);
        pDestination[pDestinationOffset + 2] =
            (byte)((pDestination[pDestinationOffset + 2] * inverse + pSource[pSourceOffset + 2] * alpha) / 255);
    }

    /// <summary>Sets a pixel on the surface, wrapping at both edges.</summary>
    private static void SetMirrored(
        Rm2kPixelBuffer pSurface, int pX, int pY, byte pRed, byte pGreen, byte pBlue, int pAlpha)
    {
        var x = PositiveModulo(pX, SurfaceWidth);
        var y = PositiveModulo(pY, SurfaceHeight);
        pSurface.TrySetPixel(x, y, pRed, pGreen, pBlue, (byte)Math.Clamp(pAlpha, 0, 255));
    }

    /// <summary>The reference's <c>Utils::PositiveModulo</c>.</summary>
    private static int PositiveModulo(int pValue, int pModulus)
    {
        var result = pValue % pModulus;
        return result < 0 ? result + pModulus : result;
    }
}
