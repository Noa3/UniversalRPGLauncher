using System;
using System.Collections.Generic;
using UniversalRPG.Rm2k.Rendering;

namespace UniversalRPG.Web;

/// <summary>
/// One character sheet, whichever of the two kinds it is.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the project's four sheets are two kinds, and
/// measured.</strong> <c>MC_Sprite_sheet</c>, <c>!Flame</c> and
/// <c>Vehicle</c> are <strong>colour type 3 — a palette</strong>;
/// <c>SlimeCharacters</c> is <strong>colour type 6 — four
/// channels.</strong> <strong>A reader that knew one of them drew three
/// of the four kinds of figure out of nothing</strong>, <strong>and one
/// that counted files rather than pictures reported four sheets read
/// while holding one.</strong>
/// </para>
/// <para>
/// <strong>And transparency comes from a different place in each.</strong>
/// A palette has index zero, and the engine's rule is that index zero
/// is see-through; <strong>four channels have no index at all</strong>,
/// <strong>and their transparency is the alpha byte.</strong>
/// <strong>Wrapping both behind one four-byte pixel is what lets the
/// renderer have one code path</strong>, <strong>and it is why this
/// class exists instead of making the renderer ask which kind it
/// holds.</strong>
/// </para>
/// </remarks>
public sealed class MzCharacterSheet
{
    /// <summary>How wide the sheet is.</summary>
    public int Width { get; private set; }

    /// <summary>How tall the sheet is.</summary>
    public int Height { get; private set; }

    /// <summary>Four bytes per pixel: red, green, blue, alpha.</summary>
    public byte[] Pixels { get; private set; } = Array.Empty<byte>();

    /// <summary>Whether the sheet was a palette.</summary>
    /// <remarks>
    /// <strong>And this is worth knowing, and not for drawing.</strong> A
    /// palette sheet is smaller and a caller that measures memory asks.
    /// </remarks>
    public bool WasPalette { get; private set; }

    /// <summary>
    /// Reads a sheet of either kind.
    /// </summary>
    /// <param name="pData">The PNG bytes.</param>
    /// <param name="pSheet">The sheet, when it read.</param>
    /// <param name="pFehler">Why it did not, when it did not.</param>
    /// <returns>Whether it read.</returns>
    public static bool Read(
        byte[] pData, out MzCharacterSheet? pSheet, out string pFehler)
    {
        pSheet = null;
        pFehler = "";
        if (MzRgbaImage.TryParse(pData, out var echt, out pFehler))
        {
            pSheet = Von(echt!, false);
            return true;
        }

        // **Und jetzt die Palette** -- **und die Fehlermeldung der
        // ersten Runde wird nicht weggeworfen**, **denn sie sagt, was
        // beide nicht konnten.**
        var grund = pFehler;
        if (Rm2kIndexedImage.TryParse(pData, out var palette, out pFehler))
        {
            pSheet = VonPalette(palette!, out pFehler);
            return pSheet != null;
        }

        pFehler = $"The sheet is neither four-channel nor a palette. The "
            + $"four-channel reader said: {grund} The palette reader said: "
            + pFehler;
        return false;
    }

    /// <summary>
    /// The red, green, blue and alpha of one pixel.
    /// </summary>
    /// <param name="pX">The column.</param>
    /// <param name="pY">The row.</param>
    /// <param name="pColour">Red, green, blue and alpha, four bytes.</param>
    /// <returns>Whether the pixel is inside the sheet.</returns>
    /// <remarks>
    /// <strong>And this is where the two kinds meet.</strong>
    /// <strong>For four channels the bytes are the pixel;</strong>
    /// <strong>for a palette the bytes are looked up</strong>, <strong>and
    /// index zero becomes alpha zero.</strong>
    /// </remarks>
    public bool TryGetPixel(int pX, int pY, out byte[] pColour)
    {
        pColour = Array.Empty<byte>();
        if (pX < 0 || pY < 0 || pX >= Width || pY >= Height)
        {
            return false;
        }

        var versatz = (pX + pY * Width) * 4;
        pColour = new[]
        {
            Pixels[versatz], Pixels[versatz + 1], Pixels[versatz + 2],
            Pixels[versatz + 3],
        };
        return true;
    }

    private static MzCharacterSheet Von(MzRgbaImage pBild, bool pPalette)
    {
        return new MzCharacterSheet
        {
            Width = pBild.Width,
            Height = pBild.Height,
            Pixels = pBild.Pixels,
            WasPalette = pPalette,
        };
    }

    private static MzCharacterSheet? VonPalette(
        Rm2kIndexedImage pBild, out string pFehler)
    {
        pFehler = "";
        var pixel = new byte[pBild.Width * pBild.Height * 4];
        for (var y = 0; y < pBild.Height; y++)
        {
            for (var x = 0; x < pBild.Width; x++)
            {
                var index = pBild.IndexAt(x, y);
                var ziel = (x + y * pBild.Width) * 4;
                if (index <= 0 || index >= pBild.Palette.Length)
                {
                    // **Und Index null ist durchsichtig** -- **und ein
                    // Index ausserhalb der Tabelle ist ebenso gut
                    // nichts**, **und es wird hier zu durchsichtig und
                    // nicht zu schwarz**, **denn ein schwarzes Rechteck
                    // um jede Figur ist schlimmer als kein Rechteck.**
                    continue;
                }

                var farbe = pBild.Palette[index];
                pixel[ziel] = farbe[0];
                pixel[ziel + 1] = farbe[1];
                pixel[ziel + 2] = farbe[2];
                pixel[ziel + 3] = 255;
            }
        }

        if (pixel.Length == 0)
        {
            pFehler = "The palette sheet brought no pixels.";
            return null;
        }

        return Von(new MzRgbaImage
        {
            Width = pBild.Width,
            Height = pBild.Height,
            Pixels = pixel,
        }, true);
    }
}