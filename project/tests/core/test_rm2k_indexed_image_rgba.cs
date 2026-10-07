using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using UniversalRPG.Rm2k.Rendering;
using UniversalRPG.Tests.Framework;
using UniversalRPG.Web;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And a chipset image that carries its colours as RGBA, which is what MV
/// writes and MZ does not.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the refusal was correct for its own engine and wrong for
/// ours.</strong> <c>Rm2kChipsetBitmap.TryParse</c> accepted only colour
/// type 3, <em>because</em> RM2K chipsets are paletted and the Player
/// transparency rule is a palette index.
/// </para>
/// <para>
/// <strong>And MV tilesets are colour type 6.</strong> Measured at
/// <c>LegalTruck/www/img/tilesets/World_A1.rpgmvp</c>: 768x576, depth 8,
/// colour type 6. MZ's <c>Outside_A1.png_</c> is colour type 3 in the same
/// dimensions. <strong>So the same reader refused every MV tileset and
/// accepted every MZ one</strong>, and an MV run started and painted
/// nothing.
/// </para>
/// <para>
/// <strong>And the fix quantises instead of branching the renderer.</strong>
/// Identical colours share one palette entry, so every tile rule above
/// this layer -- the eight-by-eight cell, the lower and upper half, the
/// passability table -- keeps working on an indexed image and nothing
/// downstream has to learn about RGBA.
/// </para>
/// </remarks>
public partial class TestRm2kIndexedImageLiestRgbaChipsets : TestBase
{
    private const string MvTileset =
        "E:/RPGMakerGames/LegalTruck_v1.1/www/img/tilesets/World_A1.rpgmvp";

    /// <summary>And the real MV tileset decodes into an indexed image.</summary>
    public void Test_DasEchteMvTilesetWirdZuEinemIndiziertenBild()
    {
        if (!File.Exists(MvTileset))
        {
            return;
        }

        var schluessel = MzSchluesselDesSpiels(
            "E:/RPGMakerGames/LegalTruck_v1.1/www");
        var bild = MzImageReader.Read(File.ReadAllBytes(MvTileset), schluessel, out var grund);
        AssertTrue(bild != null, $"the MV tileset is decrypted: {grund}");
        if (bild == null)
        {
            return;
        }

        var ok = Rm2kIndexedImage.TryParse(bild, out var ind, out var fehler);
        AssertTrue(ok, $"the RGBA MV tileset becomes an indexed image: {fehler}");
        if (!ok)
        {
            return;
        }

        Console.WriteLine($"MV tileset: {ind!.Width}x{ind.Height}, "
            + $"palette={ind.Palette.Length}, distinct={ind.Palette.Distinct().Count()}");
        AssertEq(ind.Width, 768, "the tileset keeps its own width");
        AssertEq(ind.Height, 576, "the tileset keeps its own height");
        AssertTrue(ind.Palette.Length > 8,
            "an RGBA tileset yields a real palette rather than a blank one");
        AssertTrue(ind.Palette.Length <= Rm2kIndexedImage.MaxPaletteEntries,
            "the quantised palette stays inside the 8 bit limit the tile rules assume");
    }

    /// <summary>
    /// And identical colours share one index, so the tile rules see a palette.
    /// </summary>
    public void Test_GleicheFarbenTeilenEinenIndex()
    {
        var rgba = ErzeugeRgbaPng(4, 1,
            new byte[]
            {
                255, 0, 0, 255, 0, 255, 0, 255,
                255, 0, 0, 255, 0, 0, 255, 128,
            });
        var ok = Rm2kIndexedImage.TryParse(rgba, out var ind, out var fehler);
        AssertTrue(ok, $"a hand built RGBA PNG decodes: {fehler}");
        if (!ok)
        {
            return;
        }

        var indizes = ind!.Indices;
        AssertEq(indizes[0], indizes[2],
            "the same RGB in two places gets one index");
        AssertTrue(indizes[1] != indizes[0],
            "a different colour gets a different index");
        AssertTrue(ind.Palette.Length == 3,
            $"three colours make three entries, got {ind.Palette.Length}");
    }

    /// <summary>And a paletted image still decodes exactly as before.</summary>
    public void Test_EinPalettiertesBildBleibtUnveraendert()
    {
        var palettiert = ErzeugePalettenPng(2, 1, new byte[] { 0, 1 });
        AssertTrue(Rm2kIndexedImage.TryParse(palettiert, out var ind, out var fehler),
            $"the palette path still works: {fehler}");
        if (ind == null)
        {
            return;
        }
        AssertEq(ind.Indices[0], 0, "index zero stays index zero");
        AssertEq(ind.Indices[1], 1, "index one stays index one");
        AssertEq(ind.Palette.Length, 2, "the palette is not rebuilt for a paletted image");
    }

    private static string MzSchluesselDesSpiels(string pWurzel)
    {
        var system = MzDataFile.Read("data/System.json",
            File.ReadAllBytes(Path.Combine(pWurzel, "data", "System.json")));
        return system.Root.Member("encryptionKey")?.StringOr("") ?? "";
    }

    /// <summary>A minimal 8 bit RGBA PNG, written here and not taken from a game.</summary>
    private static byte[] ErzeugeRgbaPng(int pBreite, int pHoehe, byte[] pPixel)
    {
        var roh = new List<byte>();
        for (var row = 0; row < pHoehe; row++)
        {
            roh.Add(0);
            for (var index = 0; index < pBreite * 4; index++)
            {
                roh.Add(pPixel[row * pBreite * 4 + index]);
            }
        }
        return BauPng(pBreite, pHoehe, 8, 6, null, roh.ToArray());
    }

    private static byte[] ErzeugePalettenPng(int pBreite, int pHoehe, byte[] pIndizes)
    {
        var roh = new List<byte>();
        for (var row = 0; row < pHoehe; row++)
        {
            roh.Add(0);
            for (var index = 0; index < pBreite; index++)
            {
                roh.Add(pIndizes[row * pBreite + index]);
            }
        }
        var palette = new byte[] { 255, 0, 0, 0, 0, 255 };
        return BauPng(pBreite, pHoehe, 8, 3, palette, roh.ToArray());
    }

    private static byte[] BauPng(
        int pBreite, int pHoehe, int pTiefe, int pFarbtyp, byte[]? pPalette, byte[] pRoh)
    {
        using var output = new MemoryStream();
        output.Write(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
        var ihdr = new List<byte>();
        ihdr.AddRange(BigEndian(pBreite));
        ihdr.AddRange(BigEndian(pHoehe));
        ihdr.Add((byte)pTiefe);
        ihdr.Add((byte)pFarbtyp);
        ihdr.Add(0);
        ihdr.Add(0);
        ihdr.Add(0);
        SchreibeChunk(output, "IHDR", ihdr.ToArray());
        if (pPalette != null)
        {
            SchreibeChunk(output, "PLTE", pPalette);
        }
        SchreibeChunk(output, "IDAT", Deflate(pRoh));
        SchreibeChunk(output, "IEND", []);
        return output.ToArray();
    }

    private static byte[] BigEndian(int pWert)
    {
        return new[]
        {
            (byte)(pWert >> 24), (byte)(pWert >> 16),
            (byte)(pWert >> 8), (byte)pWert,
        };
    }

    private static void SchreibeChunk(Stream pStream, string pTyp, byte[] pDaten)
    {
        pStream.Write(BigEndian(pDaten.Length));
        var typ = System.Text.Encoding.ASCII.GetBytes(pTyp);
        pStream.Write(typ);
        pStream.Write(pDaten);
        pStream.Write(BigEndian(0));
    }

    private static byte[] Deflate(byte[] pDaten)
    {
        using var output = new MemoryStream();
        // **Und zlib, nicht roher Deflate** -- **denn ein PNG-IDAT traegt
        // den ZLib-Wrapper, und ein Reader, der ZLib erwartet, meldet
        // handgeschriebenes Deflate als "not valid deflate data".**
        using (var deflate = new System.IO.Compression.ZLibStream(
            output, System.IO.Compression.CompressionLevel.Optimal, leaveOpen: true))
        {
            deflate.Write(pDaten, 0, pDaten.Length);
        }
        return output.ToArray();
    }
}
