using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;

using System.Linq;
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

    /// <summary>
    /// Copies a rectangular region of this buffer into another one. Used to cut
    /// the visible screen out of a cached full map buffer, which is how the
    /// Player's two static tile sprites are scrolled by an offset: the layers
    /// stay whole and the viewport reads a window out of them.
    /// </summary>
    /// <param name="pSourceX">Left edge of the region, in this buffer's pixels.</param>
    /// <param name="pSourceY">Top edge of the region, in this buffer's pixels.</param>
    /// <param name="pWidth">Region width in pixels; copied entirely or refused.</param>
    /// <param name="pHeight">Region height in pixels; copied entirely or refused.</param>
    /// <param name="pTarget">Destination buffer.</param>
    /// <param name="pTargetX">Left edge of the region in the destination.</param>
    /// <param name="pTargetY">Top edge of the region in the destination.</param>
    /// <returns>
    /// False when the region is not fully inside the source, which happens for
    /// a viewport near a map edge. The caller then has to decide between a
    /// partial window and a black border, so it is not silently clipped here.
    /// </returns>
    public bool TryCopyRegion(
        int pSourceX, int pSourceY, int pWidth, int pHeight,
        Rm2kPixelBuffer pTarget, int pTargetX, int pTargetY)
    {
        ArgumentNullException.ThrowIfNull(pTarget);
        if (pWidth <= 0 || pHeight <= 0)
        {
            return false;
        }
        if (pSourceX < 0 || pSourceY < 0
            || pSourceX + pWidth > Width || pSourceY + pHeight > Height
            || pTargetX < 0 || pTargetY < 0
            || pTargetX + pWidth > pTarget.Width || pTargetY + pHeight > pTarget.Height)
        {
            return false;
        }
        for (var row = 0; row < pHeight; row++)
        {
            var source = ((pSourceY + row) * Width + pSourceX) * 4;
            var destination = ((pTargetY + row) * pTarget.Width + pTargetX) * 4;
            Array.Copy(Pixels, source, pTarget.Pixels, destination, pWidth * 4);
        }
        return true;
    }

    /// <summary>
    /// Copies another buffer over this one, keeping this buffer's pixels where
    /// the source is transparent. This is the verified transparency rule the
    /// chipset blit uses: an upper tile that is transparent must not erase the
    /// lower layer, which is how a hero behind a wall stays hidden.
    /// </summary>
    public void PaintOver(Rm2kPixelBuffer pSource)
    {
        ArgumentNullException.ThrowIfNull(pSource);
        if (pSource.Width != Width || pSource.Height != Height)
        {
            throw new ArgumentException("Buffers must have the same dimensions.", nameof(pSource));
        }
        for (var offset = 0; offset < Pixels.Length; offset += 4)
        {
            if (pSource.Pixels[offset + 3] == 0)
            {
                continue;
            }
            Pixels[offset] = pSource.Pixels[offset];
            Pixels[offset + 1] = pSource.Pixels[offset + 1];
            Pixels[offset + 2] = pSource.Pixels[offset + 2];
            Pixels[offset + 3] = pSource.Pixels[offset + 3];
        }
    }
}

/// <summary>
/// A decoded indexed 8 bit PNG with a palette, the form the Player loads
/// chipset, charset and picture material in. Palette index 0 is transparent and
/// every other index is opaque (EasyRPG Player <c>src/image_png.cpp</c>,
/// <c>ReadPalettedData</c>).
/// </summary>
/// <remarks>
/// The palette index is kept rather than only the converted colour, because the
/// chipset autotile composition selects rectangles and the transparency rule has
/// to survive the blit.
/// </remarks>
public sealed class Rm2kIndexedImage
{
    /// <summary>Chipset width in pixels, verified by the Player chipset spec.</summary>
    public const int ExpectedChipsetWidth = 480;

    /// <summary>Chipset height in pixels, verified by the Player chipset spec.</summary>
    public const int ExpectedChipsetHeight = 256;

    /// <summary>Size of one map tile in pixels, from <c>#define TILE_SIZE 16</c>.</summary>
    public const int MapTileSize = 16;

    /// <summary>Size of one chipset tile in pixels.</summary>
    public const int TileSize = MapTileSize;

    /// <summary>Chipset width in pixels, verified by the Player chipset spec.</summary>
    public const int ExpectedWidth = ExpectedChipsetWidth;

    /// <summary>Chipset height in pixels, verified by the Player chipset spec.</summary>
    public const int ExpectedHeight = ExpectedChipsetHeight;

    /// <summary>Palette index the Player treats as transparent.</summary>
    public const byte TransparentIndex = 0;

    /// <summary>Upper bound for a chipset image, so a malformed file cannot allocate freely.</summary>
    public const int MaxDimension = 4096;

    /// <summary>
    /// And it builds one out of a Windows bitmap.
    /// </summary>
    /// <param name="pBreite">The width in pixels.</param>
    /// <param name="pHoehe">The height in pixels.</param>
    /// <param name="pIndizes">One palette index per pixel.</param>
    /// <param name="pPaletteVierBytes">
    /// The palette as a Windows bitmap writes it: blue, green, red and
    /// one spare byte per entry.
    /// </param>
    /// <returns>The image.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And the two orders are written out here rather than
    /// assumed.</strong> -- <strong>A bitmap writes B G R and a spare,
    /// and this type holds R G B</strong>, -- <strong>and a reader
    /// that skipped the swap produced a picture in which the red and
    /// the blue chipsets of a game had traded
    /// places.</strong>
    /// </para>
    /// <para>
    /// <strong>And the transparent index is not read out of the
    /// palette.</strong> -- <strong>Index zero is transparent in every
    /// RM2K asset format</strong>, -- <strong>and a Windows bitmap
    /// writes zero as 0, 0, 0, 0</strong>, -- <strong>which means
    /// black and not a colour</strong>, -- <strong>and taking the
    /// transparency from the colour would make every black tile of a
    /// chipset invisible.</strong>
    /// </para>
    /// </remarks>
    public static Rm2kIndexedImage FromBmp(
        int pBreite, int pHoehe, byte[] pIndizes,
        byte[] pPaletteVierBytes)
    {
        var farben = new List<byte[]>();
        for (var i = 0; i + 3 < pPaletteVierBytes.Length; i += 4)
        {
            // **Und  ein  Bitmap  schreibt  B  G  R  und  ein  Fuellbyte.**
            farben.Add(new[]
            {
                pPaletteVierBytes[i + 2],
                pPaletteVierBytes[i + 1],
                pPaletteVierBytes[i],
            });
        }

        return new Rm2kIndexedImage(
            pBreite, pHoehe, pIndizes, farben.ToArray());
    }

    /// <summary>Upper bound for palette entries, matching the 8 bit PNG limit.</summary>
    public const int MaxPaletteEntries = 256;

    private static readonly byte[] PngSignature = [137, 80, 78, 71, 13, 10, 26, 10];

    private Rm2kIndexedImage(int pWidth, int pHeight, byte[] pIndices, byte[][] pPalette)
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

    /// <summary>
    /// And the alpha of the first palette entries, from a PNG's
    /// <c>tRNS</c> chunk.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And an empty table means the file named none</strong>, and
    /// then <see cref="TransparentIndex"/> is the rule -- <strong>which is
    /// RM2K's own rule for its own formats.</strong>
    /// </para>
    /// <para>
    /// <strong>And a paletted PNG with a <c>tRNS</c> chunk says something
    /// else entirely:</strong> the first <c>n</c> palette entries carry an
    /// alpha each, and every entry after them is opaque. Measured at
    /// <c>img/system/Balloon.png_</c>: <c>PLTE</c> 768 bytes (256 entries)
    /// and <c>tRNS</c> 65 bytes -- so entries 0 to 64 have their own alpha
    /// and 65 to 255 are opaque, <strong>and a reader that assumed index
    /// zero drew the icon as an opaque black square with a white blob in
    /// it.</strong>
    /// </para>
    /// </remarks>
    public byte[] Transparency { get; private init; } = Array.Empty<byte>();

    /// <summary>And whether the file carried a <c>tRNS</c> chunk.</summary>
    public bool HasTransparencyTable => Transparency.Length > 0;

    /// <summary>And the alpha of one palette index.</summary>
    /// <param name="pIndex">The palette index.</param>
    /// <returns>The alpha, 0 to 255.</returns>
    /// <remarks>
    /// <strong>And this is the one place the two rules meet</strong>:
    /// a file that named its alphas keeps them, and a file that named none
    /// falls back to <see cref="TransparentIndex"/>.
    /// </remarks>
    public byte AlphaAt(int pIndex)
    {
        if (HasTransparencyTable)
        {
            return pIndex >= 0 && pIndex < Transparency.Length
                ? Transparency[pIndex] : (byte)255;
        }
        return pIndex == TransparentIndex ? (byte)0 : (byte)255;
    }

    /// <summary>True when the image matches the chipset size the Player expects.</summary>
    public bool HasExpectedSize => Width == ExpectedWidth && Height == ExpectedHeight;

    public static bool TryLoad(string pPath, out Rm2kIndexedImage pImage, out string pError)
    {
        pImage = null!;
        pError = "";
        try
        {
            var data = File.ReadAllBytes(pPath);
            return TryParse(data, out pImage, out pError);
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
    public static bool TryParse(byte[] pData, out Rm2kIndexedImage pImage, out string pError)
    {
        pImage = null!;
        pError = "";
        if (pData == null || pData.Length < 8)
        {
            pError = "Chipset image is too small to be a PNG.";
            return false;
        }
        // **Und  RPG  Maker  2000  schrieb  `.bmp`  und  2003  `.png`,
        //  und  der  Leser  nah  nur  eins  von  beiden.**
        //
        // **Und das ist gemessen an drei fertigen Spielen:** Lisas
        //  ChipSet  hat  zehn  BMP  und  sechs  PNG,  und  fuer
        //  `main2`  gibt  es  dort  nur  `main2.bmp`,  --  und  das
        //  Spiel  verweigerte  den  Dienst  mit  der  Meldung
        //  "main2.png is missing",  --  und  die  Datei  lag  direkt
        //  daneben.
        if (Rm2kBmp.IstBitmap(pData))
        {
            if (!Rm2kBmp.TryParse(pData, out var bmpBreite,
                out var bmpHoehe, out var bmpIndizes,
                out var bmpPalette, out var bmpFehler))
            {
                pError = "The chipset is a Windows bitmap and could not"
                    + " be read: " + bmpFehler;
                return false;
            }

            pImage = Rm2kIndexedImage.FromBmp(
                bmpBreite, bmpHoehe, bmpIndizes, bmpPalette);
            pError = "";
            return true;
        }

        for (var index = 0; index < PngSignature.Length; index++)
        {
            if (pData[index] != PngSignature[index])
            {
                    pError = "Chipset image is neither a PNG nor a"
                    + " Windows bitmap.";
                return false;
                return false;
            }
        }

        var width = 0;
        var height = 0;
        var farbtyp = 3;
        byte[]? paletteData = null;

        // **Und eine Paletten-PNG nennt ihre durchsichtigen Farben in einem
        // `tRNS`-Chunk und nicht durch den Index null.**
        //
        // **Gemessen an `img/system/Balloon.png_`:** Farbtyp 3, `PLTE` 768
        // Bytes (256 Eintraege), `tRNS` 65 Bytes -- **also tragen die ersten
        // 65 Palettenfarben je ein Alpha, und alle weiteren sind deckend.**
        byte[]? trnsData = null;
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
                    farbtyp = colorType;
                    if (bitDepth != 8)
                    {
                        pError = $"Chipset PNG must use 8 bit depth but uses {bitDepth}.";
                        return false;
                    }
                    // **Und Farbtyp 6 (RGBA) wird neben 3 angenommen.**
                    //
                    // **Und die Ablehnung war richtig fuer ihre eigene Engine
                    // und falsch fuer uns:**  RM2K-Chipsets sind
                    // palettiert,  und  die  Transparenzregel  des  Players
                    // ist  ein  Palettenindex.  **MV schreibt  aber  Farbtyp
                    // 6**  --  gemessen  an
                    //  `LegalTruck/www/img/tilesets/World_A1.rpgmvp`:
                    //  768x576, Tiefe 8, Farbtyp 6 --  **wahrend MZs
                    //  `Outside_A1.png_`  in  denselben  Massen  Farbtyp 3
                    //  ist.**  **Ein Leser,  der  nur  3  annimmt,  lehnt
                    //  damit  jedes  MV-Tileset  ab.**
                    // **And colour type 2 is RGB without an alpha
                    // channel, and it is what a title screen is made of.**
                    // Measured on this project: `img/titles1/SkieTitle.png_`
                    // decrypts to 1280x720, depth 8, **colour type 2** --
                    // and a reader that accepted only 3 and 6 refused it,
                    // so the game's own front page could not be painted.
                    // Its alpha is 255 everywhere, which is exactly what a
                    // background means.
                    if (colorType is not 2 and not 3 and not 6)
                    {
                        pError = $"Chipset PNG must be paletted (colour type 3),"
                            + $" RGB (colour type 2) or RGBA (colour type 6) but"
                            + $" uses {colorType}.";
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
                case "tRNS":
                    trnsData = new byte[length];
                    Array.Copy(pData, payload, trnsData, 0, length);
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
        // **Und die Palettenpruefung gilt nur fuer Farbtyp 3.**
        //
        // **Und sie stand vorher vor der Quantisierung und hat damit jedes
        // RGBA-Bild abgelehnt** -- **ein Bild, das seine Farben selbst
        // traegt, hat keine PLTE-Chunk, und "no usable palette" war die
        // Diagnose fuer einen Code, der die falsche Frage stellte.**
        var paletteEntries = 0;
        if (farbtyp == 3)
        {
            if (paletteData == null || paletteData.Length == 0 || paletteData.Length % 3 != 0)
            {
                pError = "Chipset PNG has no usable palette.";
                return false;
            }
            paletteEntries = paletteData.Length / 3;
            if (paletteEntries > MaxPaletteEntries)
            {
                pError = $"Chipset PNG palette has {paletteEntries} entries,"
                    + $" more than {MaxPaletteEntries}.";
                return false;
            }
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

        // **Und die Zeilenlaenge folgt der Farbtiefe, nicht der Pixelzahl.**
        // RGBA braucht vier Bytes pro Pixel, RGB drei, und ein Unfilter, der
        // mit der Pixelzahl rechnet, verschiebt jede Zeile -- was als "not
        // valid deflate data" ankommt und nicht als der Fehler, der sie ist.
        var bytesPerPixel = farbtyp switch
        {
            6 => 4,
            2 => 3,
            _ => 1,
        };
        var stride = checked(width * bytesPerPixel);
        var expected = checked((stride + 1) * height);
        if (raw.Length < expected)
        {
            pError = $"Chipset PNG image data is {raw.Length} bytes, expected at least {expected}.";
            return false;
        }

        if (!TryUnfilter(raw, width, height, bytesPerPixel, out var indices, out pError))
        {
            return false;
        }

        byte[][] palette;
        if (farbtyp is 6 or 2)
        {
            // **Und RGBA und RGB werden quantisiert, nicht separat
            // gezeichnet.** **Und `FarbSchluessel` liest die drei
            // Farbbytes, die beide Sorten an derselben Stelle tragen** --
            // **ein RGB-Bild hat nur keinen vierten.**
            if (!Quantisiere(indices, stride, bytesPerPixel,
                out var quantisiert, out palette, out pError))
            {
                return false;
            }
            indices = quantisiert;
        }
        else
        {
            palette = new byte[paletteEntries][];
            for (var entry = 0; entry < paletteEntries; entry++)
            {
                // paletteData is non-null here: the guard above returned for
                // colour type 3 without one.
                palette[entry] = [
                    paletteData[entry * 3],
                    paletteData[entry * 3 + 1],
                    paletteData[entry * 3 + 2],
                ];
            }
        }

        pImage = new Rm2kIndexedImage(width, height, indices, palette)
        {
            Transparency = trnsData ?? Array.Empty<byte>(),
        };
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
    /// <summary>
    /// And RGBA pixels become palette entries, one per distinct colour.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is a quantisation and not a second renderer.</strong>
    /// Every rule above this layer -- the eight-by-eight cell, the lower
    /// and upper half, the passability table -- reads an indexed image,
    /// and an indexed image built from distinct colours satisfies it
    /// unchanged. Alpha is read and ignored on purpose: <strong>MZ tilesets
    /// are opaque tilesets that happen to carry an alpha channel</strong>,
    /// and treating a partially transparent pixel as a hole would put
    /// holes in a map the game draws solid.
    /// </para>
    /// <para>
    // **Und mehr Farben als der Index adressieren kann werden
    // reduziert, nicht abgelehnt** -- **denn die Karte waere sonst
    // komplett unbrauchbar, und ein Spiel, das startet und nichts
    // anzeigt, ist schlimmer als eines, das ein paar Kacheln in
    // naher Nachbarschaft gleich faerbt.**
    // **Und die Reduktion ist deterministisch und nach Helligkeit
    // sortiert**, -- **damit derselbe Fehler bei jedem Lauf dasselbe
    // Bild liefert und nicht ueber den Frame hinweg flackert.**
    /// </para>
    /// </remarks>
    private static bool Quantisiere(
        byte[] pRoh, int pStride, int pBytesPerPixel,
        out byte[] pIndizes, out byte[][] pPalette, out string pError)
    {
        pIndizes = [];
        pPalette = [];
        pError = "";
        var pixelCount = pRoh.Length / pBytesPerPixel;
        var indizes = new byte[pixelCount];
        var palette = new List<byte[]>();
        var verzeichnis = new Dictionary<uint, byte>(pixelCount);
        for (var pixel = 0; pixel < pixelCount; pixel++)
        {
            var basis = pixel * pBytesPerPixel;
            var schluessel = FarbSchluessel(pRoh, basis);
            if (verzeichnis.ContainsKey(schluessel))
            {
                continue;
            }
            verzeichnis[schluessel] = (byte)palette.Count;
            palette.Add([pRoh[basis], pRoh[basis + 1], pRoh[basis + 2]]);
        }
        if (palette.Count > Rm2kIndexedImage.MaxPaletteEntries)
        {
            return Reduziere(pRoh, pixelCount, pBytesPerPixel, palette,
                out pIndizes, out pPalette, out pError);
        }
        for (var pixel = 0; pixel < pixelCount; pixel++)
        {
            var basis = pixel * pBytesPerPixel;
            indizes[pixel] = verzeichnis[FarbSchluessel(pRoh, basis)];
        }
        pPalette = palette.ToArray();
        pIndizes = indizes;
        return true;
    }

    /// <summary>And the three colour bytes packed into one key.</summary>
    private static uint FarbSchluessel(byte[] pRoh, int pBasis)
        => (uint)(pRoh[pBasis] | (pRoh[pBasis + 1] << 8) | (pRoh[pBasis + 2] << 16));

    /// <summary>
    /// And a tileset with more distinct colours than the index addresses is
    /// reduced to a fixed set, and the rest is snapped to the nearest one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the count is a fact about the game, not a bug.</strong>
    /// Measured at <c>LegalTruck</c>'s <c>World_A1.rpgmvp</c>: more than
    /// 256 distinct colours in one 768x576 sheet. <strong>An RGBA tileset
    /// has no reason to stay inside a limit that exists only because
    /// RM2K chipsets are indexed.</strong>
    /// </para>
    /// <para>
    /// <strong>And reducing is better than refusing.</strong> A refused
    /// tileset means an MV game that starts and paints nothing at all;
    /// a reduced one means a game whose crowded tiles differ slightly
    /// from the original. <strong>The choice is deterministic</strong> --
    /// sorted by luminance and then by channel, so the same file always
    /// produces the same image and never flickers between frames.
    /// </para>
    /// </remarks>
    private static bool Reduziere(
        byte[] pRoh, int pPixelCount, int pBytesPerPixel, List<byte[]> pFarben,
        out byte[] pIndizes, out byte[][] pPalette, out string pError)
    {
        pIndizes = [];
        pPalette = [];
        pError = "";
        var geordnet = pFarben
            .OrderBy(pFarbe => Luminanz(pFarbe[0], pFarbe[1], pFarbe[2]))
            .ThenBy(pFarbe => pFarbe[0])
            .ThenBy(pFarbe => pFarbe[1])
            .ThenBy(pFarbe => pFarbe[2])
            .ToList();

        // One colour per luminance step keeps the whole ramp instead of
        // only the most common entries, which is what makes a reduced
        // tileset still look like the tileset.
        var palette = new List<byte[]>(Rm2kIndexedImage.MaxPaletteEntries);
        var verzeichnis = new Dictionary<uint, byte>(Rm2kIndexedImage.MaxPaletteEntries);
        var schritt = (geordnet.Count - 1)
            / (double)(Rm2kIndexedImage.MaxPaletteEntries - 1);
        for (var index = 0; index < Rm2kIndexedImage.MaxPaletteEntries; index++)
        {
            var farbe = geordnet[(int)Math.Round(index * schritt)];
            var schluessel = (uint)(farbe[0] | (farbe[1] << 8) | (farbe[2] << 16));
            if (verzeichnis.ContainsKey(schluessel))
            {
                continue;
            }
            verzeichnis[schluessel] = (byte)palette.Count;
            palette.Add(farbe);
        }

        // **And the palette's luminance is computed once, and not once per
        // pixel per entry.** Measured on this project's sheets: the inner
        // loop ran up to 256 times for every pixel whose colour was not in
        // the palette, and every run recomputed the luminance of every
        // palette entry -- 135 seconds for 108 MP of sheets, which a player
        // sees as a game that hangs on the loading screen.
        var palettenLum = new int[palette.Count];
        for (var index = 0; index < palette.Count; index++)
        {
            palettenLum[index] = Luminanz(
                palette[index][0], palette[index][1], palette[index][2]);
        }

        var indizes = new byte[pPixelCount];
        for (var pixel = 0; pixel < pPixelCount; pixel++)
        {
            var basis = pixel * pBytesPerPixel;
            var schluessel = FarbSchluessel(pRoh, basis);
            if (!verzeichnis.TryGetValue(schluessel, out var ziel))
            {
                var lum = Luminanz(pRoh[basis], pRoh[basis + 1], pRoh[basis + 2]);
                var best = 0;
                var bestAbstand = int.MaxValue;
                for (var index = 0; index < palettenLum.Length; index++)
                {
                    var abstand = Math.Abs(palettenLum[index] - lum);
                    if (abstand < bestAbstand)
                    {
                        bestAbstand = abstand;
                        best = index;
                    }
                }
                ziel = (byte)best;

                // **And the answer is kept.** A colour that snapped to an
                // entry once snaps to the same entry for every later pixel,
                // so the scan runs once per distinct colour instead of once
                // per pixel -- which is the difference between 135 seconds
                // and a load a player waits out.
                verzeichnis[schluessel] = ziel;
            }
            indizes[pixel] = ziel;
        }
        pPalette = palette.ToArray();
        pIndizes = indizes;
        return true;
    }

    private static int Luminanz(int pR, int pG, int pB)
        => (pR * 299 + pG * 587 + pB * 114) / 1000;

    private static bool TryUnfilter(
        byte[] pRaw, int pWidth, int pHeight, int pBytesPerPixel,
        out byte[] pIndices, out string pError)
    {
        pIndices = [];
        pError = "";
        var stride = checked(pWidth * pBytesPerPixel);
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
                        // **Und der linke Nachbar zaehlt in Bytes, nicht in
                        // Pixel** -- **bei RGBA liegt er vier Positionen
                        // zurueck,  und ein Abstand von einem Byte  rechnet
                        // Kanalwerte statt Pixel und zerlegt das Bild
                        // lautlos in Farbrauschen.**
                        var left = index >= pBytesPerPixel
                            ? current[index - pBytesPerPixel] : (byte)0;
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
                        var left = index >= pBytesPerPixel
                            ? current[index - pBytesPerPixel] : (byte)0;
                        current[index] = (byte)(current[index] + (left + previous[index]) / 2);
                    }
                    break;
                case 4:
                    for (var index = 0; index < stride; index++)
                    {
                        var left = index >= pBytesPerPixel
                            ? current[index - pBytesPerPixel] : (byte)0;
                        var up = previous[index];
                        var upLeft = index >= pBytesPerPixel
                            ? previous[index - pBytesPerPixel] : (byte)0;
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

/// <summary>
/// A decoded RM2K chipset image. The decoding, the palette and the
/// transparency rule come from <see cref="Rm2kIndexedImage"/>; this type only adds
/// the chipset specific size contract.
/// </summary>
public sealed class Rm2kChipsetBitmap
{
    private Rm2kChipsetBitmap(Rm2kIndexedImage pImage)
    {
        Image = pImage;
    }

    /// <summary>The decoded pixels.</summary>
    public Rm2kIndexedImage Image { get; }

    /// <summary>Width in pixels, 480 for a verified chipset.</summary>
    public int Width => Image.Width;

    /// <summary>Height in pixels, 256 for a verified chipset.</summary>
    public int Height => Image.Height;

    /// <summary>Row major palette indices, one byte per pixel.</summary>
    public byte[] Indices => Image.Indices;

    /// <summary>RGB triples indexed by palette index.</summary>
    public byte[][] Palette => Image.Palette;

    /// <summary>True when the image matches the size the Player chipset spec expects.</summary>
    public bool HasExpectedSize => Width == Rm2kIndexedImage.ExpectedChipsetWidth
        && Height == Rm2kIndexedImage.ExpectedChipsetHeight;

    /// <summary>Chipset width in pixels, verified by the Player chipset spec.</summary>
    public const int ExpectedWidth = Rm2kIndexedImage.ExpectedChipsetWidth;

    /// <summary>Chipset height in pixels, verified by the Player chipset spec.</summary>
    public const int ExpectedHeight = Rm2kIndexedImage.ExpectedChipsetHeight;

    /// <summary>Size of one chipset tile in pixels.</summary>
    public const int TileSize = Rm2kIndexedImage.MapTileSize;

    /// <summary>Palette index the Player treats as transparent.</summary>
    public const byte TransparentIndex = Rm2kIndexedImage.TransparentIndex;

    /// <summary>Upper bound for palette entries, matching the 8 bit PNG limit.</summary>
    public const int MaxPaletteEntries = Rm2kIndexedImage.MaxPaletteEntries;

    public static bool TryLoad(string pPath, out Rm2kChipsetBitmap pBitmap, out string pError)
    {
        if (Rm2kIndexedImage.TryLoad(pPath, out var image, out pError))
        {
            pBitmap = new Rm2kChipsetBitmap(image);
            return true;
        }
        pBitmap = null!;
        return false;
    }

    public static bool TryParse(byte[] pData, out Rm2kChipsetBitmap pBitmap, out string pError)
    {
        if (Rm2kIndexedImage.TryParse(pData, out var image, out pError))
        {
            pBitmap = new Rm2kChipsetBitmap(image);
            return true;
        }
        pBitmap = null!;
        return false;
    }

    /// <summary>Palette index of a pixel, or -1 when outside the image.</summary>
    public int IndexAt(int pX, int pY) => Image.IndexAt(pX, pY);

    public bool TryBlitTile(int pColumn, int pRow, Rm2kPixelBuffer pTarget, int pTargetX, int pTargetY)
    {
        return Image.TryBlitRectangle(
            pColumn * Rm2kIndexedImage.TileSize, pRow * Rm2kIndexedImage.TileSize,
            Rm2kIndexedImage.TileSize, Rm2kIndexedImage.TileSize, pTarget, pTargetX, pTargetY);
    }

    public bool TryBlitRectangle(
        int pSourceX, int pSourceY, int pWidth, int pHeight,
        Rm2kPixelBuffer pTarget, int pTargetX, int pTargetY)
    {
        return Image.TryBlitRectangle(pSourceX, pSourceY, pWidth, pHeight, pTarget, pTargetX, pTargetY);
    }
}