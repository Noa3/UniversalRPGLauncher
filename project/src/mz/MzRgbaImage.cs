using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace UniversalRPG.Web;

/// <summary>
/// Reads a PNG that has real colours and not a palette.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this class exists because the project's own images are of
/// two kinds, and measured.</strong> On the finished project:
/// <c>img/tilesets/World_A1.png_</c> is <strong>colour type 3</strong>,
/// a palette, <strong>and <c>img/characters/SlimeCharacters.png_</c> is
/// <strong>colour type 6</strong>, four channels — red, green, blue and
/// alpha.</strong> <strong>The existing chipset reader refuses
/// anything that is not a palette</strong>, <strong>and a game whose
/// characters are all real colours had no reader at all.</strong>
/// </para>
/// <para>
/// <strong>And this is not a second format but the same format.</strong>
/// The chunk walk, the length, the inflate and the filter are the same
/// ones the palette reader uses; <strong>what differs is what comes out
/// of the scanlines</strong> — <strong>one index per pixel against four
/// bytes.</strong>
/// </para>
/// <para>
/// <strong>And the filters are the part a hand-written decoder gets
/// wrong.</strong> Each scanline begins with a filter byte, and the four
/// filters the specification names — none, sub, up, average and Paeth —
/// each say how the row relates to the row above it. <strong>A decoder
/// that reads the rows as raw bytes paints a picture that is roughly the
/// right colours in roughly the right places and is not the
/// picture.</strong>
/// </para>
/// </remarks>
public sealed class MzRgbaImage
{
    /// <summary>How wide the image is.</summary>
    public int Width { get; init; }

    /// <summary>How tall the image is.</summary>
    public int Height { get; init; }

    /// <summary>Four bytes per pixel: red, green, blue, alpha.</summary>
    public byte[] Pixels { get; init; } = Array.Empty<byte>();

    /// <summary>
    /// Reads a PNG of colour type 2 or 6.
    /// </summary>
    /// <param name="pData">The PNG bytes.</param>
    /// <param name="pImage">The image, when it read.</param>
    /// <param name="pError">Why it did not, when it did not.</param>
    /// <returns>Whether it read.</returns>
    public static bool TryParse(
        byte[] pData, out MzRgbaImage? pImage, out string pError)
    {
        pImage = null;
        pError = "";
        var grenze = 1024 * 1024;
        if (pData == null || pData.Length < 8)
        {
            pError = "The file is too small to be a PNG.";
            return false;
        }

        var signatur = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        for (var index = 0; index < signatur.Length; index++)
        {
            if (pData[index] != signatur[index])
            {
                pError = "The file does not begin with a PNG header.";
                return false;
            }
        }

        var breite = 0;
        var hoehe = 0;
        var tiefe = 0;
        var farbtyp = 0;
        var komprimiert = new List<byte>();
        var versatz = 8;
        while (versatz + 8 <= pData.Length)
        {
            var laenge = Read(pData, versatz);
            var typ = System.Text.Encoding.ASCII.GetString(
                pData, versatz + 4, 4);
            var last = versatz + 8;
            if (laenge < 0 || last + laenge > pData.Length)
            {
                pError = $"The PNG chunk '{typ}' is truncated.";
                return false;
            }

            switch (typ)
            {
                case "IHDR":
                    if (laenge < 13)
                    {
                        pError = "The PNG header chunk is too small.";
                        return false;
                    }

                    breite = Read(pData, last);
                    hoehe = Read(pData, last + 4);
                    tiefe = pData[last + 8];
                    farbtyp = pData[last + 9];
                    if (tiefe != 8)
                    {
                        pError = $"The PNG uses {tiefe} bit depth, and this "
                            + "reader reads eight.";
                        return false;
                    }

                    if (farbtyp != 2 && farbtyp != 6)
                    {
                        pError = $"The PNG uses colour type {farbtyp}, and "
                            + "this reader reads 2 and 6. Type 3 is a "
                            + "palette, and the chipset reader takes "
                            + "those.";
                        return false;
                    }

                    if (pData[last + 12] != 0)
                    {
                        pError = "The PNG is interlaced, and this reader "
                            + "reads what comes in order.";
                        return false;
                    }

                    break;
                case "IDAT":
                    for (var index = 0; index < laenge; index++)
                    {
                        komprimiert.Add(pData[last + index]);
                    }

                    break;
                case "IEND":
                    versatz = pData.Length;
                    continue;
            }

            versatz = last + laenge + 4;
        }

        if (breite <= 0 || hoehe <= 0
            || breite > grenze || hoehe > grenze)
        {
            pError = $"The PNG says {breite}x{hoehe}.";
            return false;
        }

        if (komprimiert.Count == 0)
        {
            pError = "The PNG has no image data.";
            return false;
        }

        var kanale = farbtyp == 6 ? 4 : 3;
        byte[] roh;
        try
        {
            roh = Inflate(komprimiert);
        }
        catch (Exception ausnahme)
        {
            pError = $"The PNG's image data could not be unpacked: "
                + ausnahme.Message;
            return false;
        }

        var breiteZeile = breite * kanale;
        var erwartet = (breiteZeile + 1) * hoehe;
        if (roh.Length < erwartet)
        {
            pError = $"The PNG's rows need {erwartet} bytes and it brought "
                + $"{roh.Length}.";
            return false;
        }

        var pixel = new byte[breite * hoehe * 4];
        for (var y = 0; y < hoehe; y++)
        {
            var filter = roh[y * (breiteZeile + 1)];
            var zeilenStart = y * (breiteZeile + 1) + 1;
            var zielStart = y * breiteZeile;
            var obenStart = zielStart - breiteZeile;
            for (var x = 0; x < breiteZeile; x++)
            {
                var rohWert = roh[zeilenStart + x];
                int links = x >= kanale
                    ? pixel[zielStart + x - kanale]
                    : 0;
                int oben = y > 0 ? pixel[obenStart + x] : 0;
                int linksOben = y > 0 && x >= kanale
                    ? pixel[obenStart + x - kanale]
                    : 0;
                pixel[zielStart + x] = (byte)(Apply(
                    filter, rohWert, links, oben, linksOben) & 0xFF);
            }
        }

        // **Und aus den Kanaelen wird ein Vierbytes-Bild**, **denn die
        // beiden Typen haben entweder drei oder vier.** **Und ein Bild,
        // das an der vierten Stelle keine Zahl traegt, hat keine
        // Durchsicht** -- **und `TrySetPixel` braucht eine, denn ohne
        // sie zaehlt `DistinctColours` kein einziges Pixel.**
        for (var index = 0; index < breite * hoehe; index++)
        {
            var quell = index * kanale;
            var ziel = index * 4;
            pixel[ziel] = pixel[quell];
            pixel[ziel + 1] = pixel[quell + 1];
            pixel[ziel + 2] = pixel[quell + 2];
            pixel[ziel + 3] = farbtyp == 6 ? pixel[quell + 3] : (byte)255;
        }

        pImage = new MzRgbaImage
        {
            Width = breite,
            Height = hoehe,
            Pixels = pixel,
        };
        return true;
    }

    private static int Apply(int pFilter, int pRaw, int pLeft, int pUp,
        int pUpLeft)
    {
        return pFilter switch
        {
            0 => pRaw,
            1 => pRaw + pLeft,
            2 => pRaw + pUp,
            3 => pRaw + ((pLeft + pUp) / 2),
            4 => pRaw + Paeth(pLeft, pUp, pUpLeft),
            _ => pRaw,
        };
    }

    private static int Paeth(int pLeft, int pUp, int pUpLeft)
    {
        var schaetzung = pLeft + pUp - pUpLeft;
        var dLeft = Math.Abs(schaetzung - pLeft);
        var dUp = Math.Abs(schaetzung - pUp);
        var dUpLeft = Math.Abs(schaetzung - pUpLeft);
        if (dLeft <= dUp && dLeft <= dUpLeft)
        {
            return pLeft;
        }

        return dUp <= dUpLeft ? pUp : pUpLeft;
    }

    private static byte[] Inflate(List<byte> pCompressed)
    {
        using var input = new MemoryStream(pCompressed.ToArray());
        using var entpacker = new ZLibStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        entpacker.CopyTo(output);
        return output.ToArray();
    }

    private static int Read(byte[] pData, int pOffset)
    {
        return (pData[pOffset] << 24) | (pData[pOffset + 1] << 16)
            | (pData[pOffset + 2] << 8) | pData[pOffset + 3];
    }

    /// <summary>
    /// The red, green, blue and alpha of one pixel.
    /// </summary>
    /// <param name="pX">The column.</param>
    /// <param name="pY">The row.</param>
    /// <param name="pColour">Red, green, blue and alpha, four bytes.</param>
    /// <returns>Whether the pixel is inside the image.</returns>
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
}
