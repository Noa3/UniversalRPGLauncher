using System;
using System.Collections.Generic;
using System.Linq;
using UniversalRPG.Rm2k.Rendering;

namespace UniversalRPG.Web;

/// <summary>
/// Paints an MZ map's tiles out of its tileset.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the map's own shape is six numbers per tile, and not
/// one.</strong> Measured on <c>CamelliaCoronation-Win/Map002.json</c>:
/// <c>width</c> 14, <c>height</c> 18, and <c>data</c> of
/// <strong>1512 = 14 × 18 × 6</strong> numbers. <strong>All six are in
/// use</strong> — <strong>50, 48, 46, 51, 52 and 52 of the 252 tiles
/// carry one</strong> — <strong>and a reader that took one number per
/// tile painted a quarter of the floor and left the rest empty.</strong>
/// </para>
/// <para>
/// <strong>And a tile id addresses a cell of eight by eight.</strong>
/// Measured: <c>5</c> is column 5 row 0, <c>81</c> is column 1 row 10,
/// <c>5519</c> is column 7 row 689 — <strong>that is <c>id % 8</c> and
/// <c>id / 8</c></strong>, <strong>and the engine's own tile size is
/// forty-eight pixels.</strong>
/// </para>
/// <para>
/// <strong>And this paints the lower half of each tile.</strong> The
/// upper half is the second number of each pair, <strong>and a painter
/// that drew only the lower half gave every tile a flat top</strong> —
/// <strong>which is what a floor looks like and what a wall does
/// not.</strong>
/// </para>
/// </remarks>
public sealed class MzMapRenderer
{
    /// <summary>How many numbers one tile carries in the map file.</summary>
    /// <remarks>
    /// <summary>How many numbers a cell carries.</summary>
    /// <remarks>
    /// <strong>And they are six planes and not three layers of two
    /// halves.</strong> Measured: the file stores them plane by plane --
    /// <c>data[(z * height + y) * width + x]</c> -- and planes zero to three
    /// are the four tile layers, plane four is the shadow and plane five is
    /// the region. <strong>A reader that paired them as lower and upper
    /// halves read the shadow plane as a tile.</strong>
    /// </remarks>
    public const int NumbersPerTile = 6;

    /// <summary>
    /// How many tiles a B-E sheet holds per side.
    /// </summary>
    /// <remarks>
    /// Measured: a sheet is 768 pixels wide and a tile is 48, so sixteen.
    /// <strong>And the engine's own arithmetic says so</strong> --
    /// <c>sy = (Math.floor((tileId % 256) / 8) % 16) * h</c>, where the
    /// modulo sixteen is the row wrap of a sixteen row sheet.
    /// </remarks>
    public const int SheetSideTiles = 16;

    /// <summary>The first tile number of the A5 sheet.</summary>
    /// <remarks>
    /// Measured at <c>Tilemap.isTileA5</c>: <c>tileId &gt;= 1536 &amp;&amp;
    /// tileId &lt; 1664</c>. **And A5 is sheet four, and the number does not
    /// say so** -- <c>Math.floor(1536 / 256)</c> is six, which would name a
    /// sheet this tileset does not have.
    /// </remarks>
    public const int TileA5First = 1536;

    /// <summary>The last tile number of the A5 sheet.</summary>
    public const int TileA5Last = 1663;

    /// <summary>Which sheet A5 is.</summary>
    public const int A5Sheet = 4;

    /// <summary>How many sheets a tileset is spread over.</summary>
    /// <remarks>
    /// <strong>And this is measured, and it is nine.</strong> Every
    /// tileset of the finished project carries
    /// <c>tilesetNames</c> with nine entries — <em>World_A1, World_A2,
    /// (leer), (leer), (leer), World_B, World_C, (leer), (leer)</em> —
    /// <strong>and an empty entry is a sheet that does not
    /// exist.</strong> <strong>A reader that took the tileset's own name
    /// and appended <c>.png_</c> looked for
    /// <c>img/tilesets/Overworld.png_</c></strong>, **which is not a
    /// file in this project** — <strong>and the map painted
    /// nothing.</strong>
    /// </remarks>
    public const int SheetsPerTileset = 9;

    /// <summary>The engine's tile size in pixels.</summary>
    public const int TilePixels = 48;

    /// <summary>Where the tileset for a map's tileset id lives.</summary>
    /// <param name="pTilesetId">The map's <c>tilesetId</c>.</param>
    /// <returns>The file name, and never a path the project did not
    /// write.</returns>
    /// <remarks>
    /// <strong>And the name is the tileset's own, and not
    /// <c>"A"</c>.</strong> Measured: <c>data/Tilesets.json</c> gives each
    /// one a <c>name</c> like <em>Overworld</em>, and the file on disk is
    /// <c>img/tilesets/&lt;name&gt;.png_</c> — <strong>and a reader
    /// that guessed the single-letter name found a file that does not
    /// exist for every map but one.</strong>
    /// </remarks>
    /// <summary>And every spelling a tileset image carries, in one list.</summary>
    /// <remarks>
    /// <para>
    /// <strong>And MZ and MV do not agree on the suffix.</strong> Measured
    /// on this machine: <c>CamelliaCoronation</c> (MZ) writes
    /// <c>img/tilesets/Overworld.png_</c>, and <c>LegalTruck</c> (MV)
    /// writes <c>img/tilesets/Dungeon_A1.rpgmvp</c> for the very same
    /// concept. <strong>A reader that appends <c>.png_</c> and stops
    /// finds an MZ game and no MV game at all</strong>, and an MV run
    /// then starts and paints nothing, which looks like a broken
    /// runtime rather than a missing suffix.
    /// </para>
    /// <para>
    /// <strong>And both spellings are tried, in the order the games
    /// write them</strong>, because which one exists is a fact about the
    /// disk and not something to guess from the engine name.
    /// </para>
    /// </remarks>
    public static IReadOnlyList<string> TilesetFileNames(string pName)
    {
        if (pName.Length == 0)
        {
            return Array.Empty<string>();
        }
        if (pName.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
            || pName.EndsWith(".png_", StringComparison.OrdinalIgnoreCase)
            || pName.EndsWith(".rpgmvp", StringComparison.OrdinalIgnoreCase))
        {
            return new[] { pName };
        }
        return new[] { pName + ".rpgmvp", pName + ".png_", pName + ".png" };
    }

    public static string TilesetFileName(string pName)
    {
        return pName.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
            || pName.EndsWith(".png_", StringComparison.OrdinalIgnoreCase)
            ? pName
            : pName + ".png_";
    }

    /// <summary>
    /// Paints one map's tiles into a buffer.
    /// </summary>
    /// <param name="pMap">The map file's root.</param>
    /// <param name="pTileset">The tileset image.</param>
    /// <param name="pPixels">Where to paint.</param>
    /// <param name="pWhy">Why it did not paint, when it did not.</param>
    /// <returns>Whether it painted.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And the lower half comes first, and the upper half on
    /// top of it.</strong> The map's numbers are
    /// <c>[lower 1, upper 1, left 1, lower 2, upper 2, right 2, …]</c>
    /// per layer, <strong>and the third number of each layer is the
    /// side of the triangle, not a tile.</strong> <strong>A painter that
    /// treated all six as tiles pointed its triangles at the sheet as
    /// if they were walls</strong>, <strong>and a game's floor grew
    /// spikes.</strong>
    /// </para>
    /// <para>
    /// <strong>And a map with no tileset paints nothing, and says
    /// so.</strong> Measured: <c>Map002.json</c> has
    /// <c>"tileset": null</c>, <strong>and the tileset comes from
    /// <c>tilesetId</c> and the tileset table instead.</strong>
    /// </para>
    /// </remarks>
    public bool Paint(
        MzValue pMap,
        IReadOnlyList<Rm2kIndexedImage?> pBlätter,
        Rm2kPixelBuffer pPixels,
        out string pWhy)
    {
        pWhy = "";
        if (pMap == null || pMap.Kind != MzKind.Object)
        {
            pWhy = "The map file is not an object.";
            return false;
        }

        if (pBlätter == null || pBlätter.Count == 0)
        {
            pWhy = "The project has no tileset sheet this reader could "
                + "read, so there is nothing to paint the map out of.";
            return false;
        }

        if (pBlätter.All(pBlatt => pBlatt == null))
        {
            pWhy = "Every sheet of this tileset is missing, and the map "
                + "would be a colour with nothing on it.";
            return false;
        }

        var breite = pMap.Member("width")?.IntOr(0) ?? 0;
        var hoehe = pMap.Member("height")?.IntOr(0) ?? 0;
        var felder = pMap.Member("data")?.Items;
        if (breite <= 0 || hoehe <= 0 || felder == null)
        {
            pWhy = $"The map says {breite}x{hoehe} and has "
                + $"{felder?.Count ?? 0} tile numbers, and a painter needs "
                + $"{breite * hoehe * NumbersPerTile}.";
            return false;
        }

        var erwartet = breite * hoehe * NumbersPerTile;
        if (felder.Count < erwartet)
        {
            pWhy = $"The map has {felder.Count} tile numbers and needs "
                + $"{erwartet}, and the rest would have been read from "
                + "beyond the end.";
            return false;
        }

        // **And the map's numbers are stored plane by plane, and not cell by
        // cell.** Measured at `Game_Map.prototype.tileId`:
        //
        //     return $dataMap.data[(z * height + y) * width + x] || 0;
        //
        // **so layer three's whole plane comes first, and a reader that
        // stepped six numbers per cell read the right tile for the first cell
        // and a different one for every cell after it.**
        //
        // **And there are four tile layers, drawn bottom to top.** Measured
        // at `Game_Map.prototype.layeredTiles`:
        //
        //     for (let i = 0; i < 4; i++) tiles.push(this.tileId(x, y, 3 - i));
        //
        // **and a tile is forty-eight pixels and not a sixteen pixel half
        // over a thirty-two pixel half** -- the halves are RM2K's model, not
        // this engine's. Planes four and five are the shadow and the region
        // and are not tiles at all.
        var flaeche = breite * hoehe;
        pPixels.Clear();
        for (var y = 0; y < hoehe; y++)
        {
            for (var x = 0; x < breite; x++)
            {
                for (var ebene = 3; ebene >= 0; ebene--)
                {
                    var kachel = felder[ebene * flaeche + y * breite + x].IntOr(0);
                    if (kachel > 0)
                    {
                        BlitTile(
                            pBlätter, kachel, x * TilePixels, y * TilePixels,
                            pPixels);
                    }
                }
            }
        }

        return true;
    }

    /// <summary>
    /// And where a tile number sits: which sheet, and which cell of it.
    /// </summary>
    /// <param name="pTileId">The number the map stores.</param>
    /// <returns>
    /// The sheet, the column and the row, in tiles. A number no sheet holds
    /// gives a sheet outside the nine, which the caller refuses.
    /// </returns>
    /// <remarks>
    /// <strong>And this is the engine's own arithmetic, measured at
    /// <c>Tilemap.prototype._addNormalTile</c>:</strong>
    /// <code>
    /// setNumber = isTileA5(tileId) ? 4 : 5 + Math.floor(tileId / 256);
    /// sx = ((Math.floor(tileId / 128) % 2) * 8 + (tileId % 8)) * w;
    /// sy = (Math.floor((tileId % 256) / 8) % 16) * h;
    /// </code>
    /// <strong>and <c>isTileA5</c> is <c>tileId &gt;= 1536 &amp;&amp; tileId
    /// &lt; 1664</c>.</strong> A tile of the B to E sheets is forty-eight
    /// pixels and covers one cell whole; the autotiles (2048 and up) are a
    /// different arithmetic and are not decoded here yet.
    /// </remarks>
    public static (int Sheet, int Column, int Row) TileCell(int pTileId)
    {
        var blatt = pTileId >= TileA5First && pTileId <= TileA5Last
            ? A5Sheet
            : 5 + (pTileId / 256);
        var spalte = ((pTileId / 128) % 2) * 8 + (pTileId % 8);
        var zeile = ((pTileId % 256) / 8) % SheetSideTiles;
        return (blatt, spalte, zeile);
    }

    private static void BlitTile(
        IReadOnlyList<Rm2kIndexedImage?> pBlätter,
        int pTileId,
        int pX,
        int pY,
        Rm2kPixelBuffer pPixels)
    {
        // **And the tile number is decoded the way the engine decodes it.**
        // Measured at `Tilemap.prototype._addNormalTile`:
        //
        //     setNumber = isTileA5(tileId) ? 4 : 5 + Math.floor(tileId / 256);
        //     sx = ((Math.floor(tileId / 128) % 2) * 8 + (tileId % 8)) * w;
        //     sy = (Math.floor((tileId % 256) / 8) % 16) * h;
        //
        // **and `isTileA5` is `tileId >= 1536 && tileId < 1664`.** So a
        // B-E sheet holds sixteen by sixteen tiles and not eight by eight,
        // the A5 sheet is sheet four, and the sheet number comes from the
        // division by 256 and not by the sheet's tile count.
        //
        // **And the old arithmetic here was `tileId / 64`, which is a
        // different number entirely.** Measured on this machine: of
        // Camellia's 56 distinct tile ids, 24 fell outside the nine sheets
        // and painted nothing; of the start map of Skies, all 221 tiles are
        // id 1536 -- an A5 tile -- which `1536 / 64` sends to sheet 24, so
        // the whole room was black.
        var (blatt, spalte, zeile) = TileCell(pTileId);
        if (blatt < 0 || blatt >= pBlätter.Count)
        {
            return;
        }

        var tileset = pBlätter[blatt];
        if (tileset == null)
        {
            return;
        }

        var x0 = spalte * TilePixels;
        var y0 = zeile * TilePixels;

        for (var dy = 0; dy < TilePixels; dy++)
        {
            var quelle = y0 + dy;
            if (quelle >= tileset.Height)
            {
                return;
            }

            for (var dx = 0; dx < TilePixels; dx++)
            {
                var sx = x0 + dx;
                if (sx >= tileset.Width)
                {
                    return;
                }

                var index = tileset.IndexAt(sx, quelle);
                var farbe = tileset.Palette[
                    Math.Min(index, tileset.Palette.Length - 1)];
                pPixels.TrySetPixel(
                    pX + dx, pY + dy, farbe[0], farbe[1], farbe[2], 255);
            }
        }
    }

    /// <summary>
    /// How many different colours a painted map has.
    /// </summary>
    /// <param name="pPixels">The painted buffer.</param>
    /// <returns>The count, and zero for an empty picture.</returns>
    /// <remarks>
    /// <strong>And this is the number a test asserts on, and it is
    /// stronger than "no exception".</strong> A map painted in one
    /// colour is a map whose tileset was not found, <strong>and a test
    /// that only checks that painting did not throw says nothing about
    /// whether anything was drawn.</strong> <strong>Measured on the
    /// RM2K project: <c>Map0001</c> painted one colour and
    /// <c>Map0002</c> painted 64, and that difference is the whole
    /// evidence.</strong>
    /// </remarks>
    public static int DistinctColours(Rm2kPixelBuffer pPixels)
    {
        var gesehen = new HashSet<int>();
        var pix = pPixels.Pixels;
        for (var index = 0; index + 3 < pix.Length; index += 4)
        {
            if (pix[index + 3] == 0)
            {
                continue;
            }

            gesehen.Add(
                (pix[index] << 16) | (pix[index + 1] << 8) | pix[index + 2]);
        }

        return gesehen.Count;
    }
}
