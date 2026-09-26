using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;

namespace UniversalRPG.Rm2k.Rendering;

/// <summary>
/// An RGBA pixel buffer with 8 bit channels, kept free of Godot types so the
/// chipset blitting stays deterministic and testable.
/// </summary>
public sealed class Rm2kPixelBuffer
{
    public Rm2kPixelBuffer(int pWidth, int pHeight)
    {
        if (pWidth <= 0 || pHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pWidth), "Pixel buffer dimensions must be positive.");
        }
        Width = pWidth;
        Height = pHeight;
        Pixels = new byte[checked(pWidth * pHeight * 4)];
    }

    public int Width { get; }
    public int Height { get; }

    /// <summary>Row major RGBA bytes, 4 per pixel.</summary>
    public byte[] Pixels { get; }

    public bool TrySetPixel(int pX, int pY, byte pRed, byte pGreen, byte pBlue, byte pAlpha)
    {
        if (pX < 0 || pY < 0 || pX >= Width || pY >= Height)
        {
            return false;
        }
        var offset = (pX + pY * Width) * 4;
        Pixels[offset] = pRed;
        Pixels[offset + 1] = pGreen;
        Pixels[offset + 2] = pBlue;
        Pixels[offset + 3] = pAlpha;
        return true;
    }

    public void Clear()
    {
        Array.Clear(Pixels);
    }
}

/// <summary>
/// Decoded RM2K chipset bitmap. The Player loads a chipset as a paletted PNG
/// with transparency, where palette index 0 is transparent and every other
/// index is opaque (EasyRPG Player <c>src/image_png.cpp</c>,
/// <c>ReadPalettedData</c>), and expects a 480 by 256 image
/// (<c>src/cache.cpp</c>, the <c>Material::Chipset</c> spec, which also marks
/// the load as transparent).
/// </summary>
/// <remarks>
/// The palette index is kept rather than only the converted colour, because the
/// autotile composition of K-096 and K-097 selects chipset rectangles, and
/// transparency has to survive the blit.
/// </remarks>
public sealed class Rm2kChipsetBitmap
{
    /// <summary>Chipset width in pixels, verified by the Player chipset spec.</summary>
    public const int ExpectedWidth = 480;

    /// <summary>Chipset height in pixels, verified by the Player chipset spec.</summary>
    public const int ExpectedHeight = 256;

    /// <summary>Size of one chipset tile in pixels.</summary>
    public const int TileSize = 16;

    /// <summary>Palette index the Player treats as transparent.</summary>
    public const byte TransparentIndex = 0;

    /// <summary>Upper bound for a chipset image, so a malformed file cannot allocate freely.</summary>
    public const int MaxDimension = 4096;

    /// <summary>Upper bound for palette entries, matching the 8 bit PNG limit.</summary>
    public const int MaxPaletteEntries = 256;

    private static readonly byte[] PngSignature = [137, 80, 78, 71, 13, 10, 26, 10];

    private Rm2kChipsetBitmap(int pWidth, int pHeight, byte[] pIndices, byte[][] pPalette)
    {
        Width = pWidth;
        Height = pHeight;
        Indices = pIndices;
        Palette = pPalette;
    }

    public int Width { get; }
    public int Height { get; }

    /// <summary>Row major palette indices, one byte per pixel.</summary>
    public byte[] Indices { get; }

    /// <summary>RGB triples indexed by palette index.</summary>
    public byte[][] Palette { get; }

    /// <summary>True when the image matches the chipset size the Player expects.</summary>
    public bool HasExpectedSize => Width == ExpectedWidth && Height == ExpectedHeight;

    public static bool TryLoad(string pPath, out Rm2kChipsetBitmap pBitmap, out string pError)
    {
        pBitmap = null!;
        pError = "";
        try
        {
            var data = File.ReadAllBytes(pPath);
            return TryParse(data, out pBitmap, out pError);
        }
        catch (IOException exception)
        {
            pError = $"Could not read the chipset image: {exception.Message}";
            return false;
        }
        catch (UnauthorizedAccessException exception)
        {
            pError = $"Could not read the chipset image: {exception.Message}";
            return false;
        }
    }

    /// <summary>
    /// Parses a paletted 8 bit PNG. Only the indexed colour type is accepted:
    /// the Player's chipset loader does an index to RGBA conversion itself, and
    /// a non paletted image would silently lose the transparency rule.
    /// </summary>
    public static bool TryParse(byte[] pData, out Rm2kChipsetBitmap pBitmap, out string pError)
    {
        pBitmap = null!;
        pError = "";
        if (pData == null || pData.Length < 8)
        {
            pError = "Chipset image is too small to be a PNG.";
            return false;
        }
        for (var index = 0; index < PngSignature.Length; index++)
        {
            if (pData[index] != PngSignature[index])
            {
                pError = "Chipset image is not a PNG.";
                return false;
            }
        }

        var width = 0;
        var height = 0;
        byte[]? paletteData = null;
        var compressed = new List<byte>();

        var offset = 8;
        while (offset + 8 <= pData.Length)
        {
            var length = ReadBigEndianInt32(pData, offset);
            var type = System.Text.Encoding.ASCII.GetString(pData, offset + 4, 4);
            var payload = offset + 8;
            if (length < 0 || payload + length > pData.Length)
            {
                pError = $"Chipset PNG chunk '{type}' is truncated.";
                return false;
            }
            switch (type)
            {
                case "IHDR":
                    if (length < 13)
                    {
                        pError = "Chipset PNG header chunk is too small.";
                        return false;
                    }
                    width = ReadBigEndianInt32(pData, payload);
                    height = ReadBigEndianInt32(pData, payload + 4);
                    var bitDepth = pData[payload + 8];
                    var colorType = pData[payload + 9];
                    if (bitDepth != 8)
                    {
                        pError = $"Chipset PNG must use 8 bit depth but uses {bitDepth}.";
                        return false;
                    }
                    if (colorType != 3)
                    {
                        pError = $"Chipset PNG must be paletted (colour type 3) but uses {colorType}.";
                        return false;
                    }
                    if (pData[payload + 12] != 0)
                    {
                        pError = "Chipset PNG must not be interlaced.";
                        return false;
                    }
                    break;
                case "PLTE":
                    paletteData = new byte[length];
                    Array.Copy(pData, payload, paletteData, 0, length);
                    break;
                case "IDAT":
                    for (var index = 0; index < length; index++)
                    {
                        compressed.Add(pData[payload + index]);
                    }
                    break;
                case "IEND":
                    offset = pData.Length;
                    continue;
            }
            offset = payload + length + 4;
        }

        if (width <= 0 || height <= 0 || width > MaxDimension || height > MaxDimension)
        {
            pError = $"Chipset PNG dimensions {width}x{height} are outside the accepted range.";
            return false;
        }
        if (paletteData == null || paletteData.Length == 0 || paletteData.Length % 3 != 0)
        {
            pError = "Chipset PNG has no usable palette.";
            return false;
        }
        var paletteEntries = paletteData.Length / 3;
        if (paletteEntries > MaxPaletteEntries)
        {
            pError = $"Chipset PNG palette has {paletteEntries} entries, more than {MaxPaletteEntries}.";
            return false;
        }
        if (compressed.Count == 0)
        {
            pError = "Chipset PNG has no image data.";
            return false;
        }

        byte[] raw;
        try
        {
            raw = Inflate(compressed);
        }
        catch (InvalidDataException)
        {
            pError = "Chipset PNG image data is not valid deflate data.";
            return false;
        }

        var stride = width;
        var expected = checked((stride + 1) * height);
        if (raw.Length < expected)
        {
            pError = $"Chipset PNG image data is {raw.Length} bytes, expected at least {expected}.";
            return false;
        }

        if (!TryUnfilter(raw, width, height, out var indices, out pError))
        {
            return false;
        }

        var palette = new byte[paletteEntries][];
        for (var entry = 0; entry < paletteEntries; entry++)
        {
            palette[entry] = [paletteData[entry * 3], paletteData[entry * 3 + 1], paletteData[entry * 3 + 2]];
        }
        pBitmap = new Rm2kChipsetBitmap(width, height, indices, palette);
        return true;
    }

    /// <summary>Palette index of a pixel, or -1 when the coordinate is outside the image.</summary>
    public int IndexAt(int pX, int pY)
    {
        if (pX < 0 || pY < 0 || pX >= Width || pY >= Height)
        {
            return -1;
        }
        return Indices[pX + pY * Width];
    }

    /// <summary>
    /// Blits one 16 by 16 chipset tile using the verified transparency rule:
    /// palette index 0 is transparent, every other index is opaque. Pixels that
    /// are transparent are left untouched so a background can show through.
    /// </summary>
    public bool TryBlitTile(int pColumn, int pRow, Rm2kPixelBuffer pTarget, int pTargetX, int pTargetY)
    {
        ArgumentNullException.ThrowIfNull(pTarget);
        return TryBlitRectangle(pColumn * TileSize, pRow * TileSize, TileSize, TileSize, pTarget, pTargetX, pTargetY);
    }

    /// <summary>Blits an arbitrary chipset rectangle with the same transparency rule.</summary>
    public bool TryBlitRectangle(
        int pSourceX, int pSourceY, int pWidth, int pHeight,
        Rm2kPixelBuffer pTarget, int pTargetX, int pTargetY)
    {
        ArgumentNullException.ThrowIfNull(pTarget);
        if (pWidth <= 0 || pHeight <= 0
            || pSourceX < 0 || pSourceY < 0
            || pSourceX + pWidth > Width || pSourceY + pHeight > Height)
        {
            return false;
        }
        for (var row = 0; row < pHeight; row++)
        {
            for (var column = 0; column < pWidth; column++)
            {
                var index = Indices[pSourceX + column + (pSourceY + row) * Width];
                if (index == TransparentIndex || index >= Palette.Length)
                {
                    continue;
                }
                var colour = Palette[index];
                pTarget.TrySetPixel(
                    pTargetX + column, pTargetY + row,
                    colour[0], colour[1], colour[2], 255);
            }
        }
        return true;
    }

    private static int ReadBigEndianInt32(byte[] pData, int pOffset)
    {
        return (pData[pOffset] << 24)
            | (pData[pOffset + 1] << 16)
            | (pData[pOffset + 2] << 8)
            | pData[pOffset + 3];
    }

    private static byte[] Inflate(List<byte> pCompressed)
    {
        using var input = new MemoryStream(pCompressed.ToArray());
        using var deflate = new ZLibStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        deflate.CopyTo(output);
        return output.ToArray();
    }

    /// <summary>
    /// Reverses the PNG per scanline filters. Every filter type of the PNG
    /// specification is implemented; an unknown type is refused instead of being
    /// treated as "none".
    /// </summary>
    private static bool TryUnfilter(byte[] pRaw, int pWidth, int pHeight, out byte[] pIndices, out string pError)
    {
        pIndices = [];
        pError = "";
        var stride = pWidth;
        var result = new byte[checked(stride * pHeight)];
        var previous = new byte[stride];
        var current = new byte[stride];
        var rawOffset = 0;
        for (var row = 0; row < pHeight; row++)
        {
            var filter = pRaw[rawOffset++];
            for (var index = 0; index < stride; index++)
            {
                current[index] = pRaw[rawOffset + index];
            }
            rawOffset += stride;
            switch (filter)
            {
                case 0:
                    break;
                case 1:
                    for (var index = 0; index < stride; index++)
                    {
                        var left = index >= 1 ? current[index - 1] : (byte)0;
                        current[index] = (byte)(current[index] + left);
                    }
                    break;
                case 2:
                    for (var index = 0; index < stride; index++)
                    {
                        current[index] = (byte)(current[index] + previous[index]);
                    }
                    break;
                case 3:
                    for (var index = 0; index < stride; index++)
                    {
                        var left = index >= 1 ? current[index - 1] : (byte)0;
                        current[index] = (byte)(current[index] + (left + previous[index]) / 2);
                    }
                    break;
                case 4:
                    for (var index = 0; index < stride; index++)
                    {
                        var left = index >= 1 ? current[index - 1] : (byte)0;
                        var up = previous[index];
                        var upLeft = index >= 1 ? previous[index - 1] : (byte)0;
                        current[index] = (byte)(current[index] + Paeth(left, up, upLeft));
                    }
                    break;
                default:
                    pError = $"Chipset PNG uses the unknown filter type {filter}.";
                    return false;
            }
            Array.Copy(current, 0, result, row * stride, stride);
            (previous, current) = (current, previous);
        }
        pIndices = result;
        return true;
    }

    private static int Paeth(byte pLeft, byte pUp, byte pUpLeft)
    {
        var estimate = pLeft + pUp - pUpLeft;
        var distanceLeft = Math.Abs(estimate - pLeft);
        var distanceUp = Math.Abs(estimate - pUp);
        var distanceUpLeft = Math.Abs(estimate - pUpLeft);
        if (distanceLeft <= distanceUp && distanceLeft <= distanceUpLeft)
        {
            return pLeft;
        }
        return distanceUp <= distanceUpLeft ? pUp : pUpLeft;
    }
}
