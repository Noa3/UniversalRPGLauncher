using System;
using System.IO;
using System.Text;
using UniversalRPG.Rm2k.Rendering;
using UniversalRPG.Tests.Framework;
using UniversalRPG.Web;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Painting a real MZ map out of its real tileset.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is the step the runtime could not take.</strong>
/// Before it, <c>MzEngineRuntime</c> read a project's maps and ran
/// their commands, <strong>and painted nothing</strong> — a project ran
/// and no player could see it.
/// </para>
/// <para>
/// <strong>And the obstacles were all measured, and none of them was
/// obvious.</strong> The images are encrypted; the tileset is named by
/// its own name and not by a letter; a tile is six numbers and not one;
/// a tile id addresses a cell of eight by eight.
/// </para>
/// </remarks>
public partial class TestMzMapRender : TestBase
{
    private const string Projekt = "E:/RPGMakerGames/CamelliaCoronation-Win";

    private static bool Vorhanden()
    {
        return File.Exists(Projekt + "/data/Map002.json")
            && File.Exists(Projekt + "/data/Tilesets.json")
            && File.Exists(Projekt + "/img/tilesets/Overworld.png_");
    }

    /// <summary>
    /// The map's tileset is found, opened and decoded.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the name is the tileset's own.</strong> Measured:
    /// <c>data/Tilesets.json</c> gives the tileset of <c>Map002</c> the
    /// name <em>Overworld</em>, and the file is
    /// <c>img/tilesets/Overworld.png_</c>. <strong>A reader that guessed
    /// the single-letter editor name found a file that does not
    /// exist</strong> — and the editor's own letter is <em>A</em>.
    /// </para>
    /// <para>
    /// <strong>And this is the whole chain in one test:</strong> the
    /// name, the encrypted file, the key from <c>System.json</c>, the
    /// unscrambling, and the decoding. <strong>Four steps, and each of
    /// them alone proves nothing.</strong>
    /// </para>
    /// </remarks>
    public void Test_DasTilesetDerKarteWirdGefundenUndGeoeffnet()
    {
        if (!Vorhanden())
        {
            return;
        }

        // **Und die Karte nennt ihre Tileset-Id.**
        var karte = MzDataFile.Read(
            "data/Map002.json",
            File.ReadAllBytes(Projekt + "/data/Map002.json"));
        var id = karte.Root.Member("tilesetId")?.IntOr(0) ?? 0;
        AssertTrue(id > 0,
            "**and the map names a tileset** -- and the field is "
                + "tilesetId, and the map's own \"tileset\" is null");

        // **Und der Name steht in der Tileset-Tabelle.**
        var tabelle = MzDataFile.Read(
            "data/Tilesets.json",
            File.ReadAllBytes(Projekt + "/data/Tilesets.json"));
        var name = "";
        foreach (var eintrag in tabelle.Root.Items)
        {
            if ((eintrag.Member("id")?.IntOr(-1) ?? -1) == id)
            {
                name = eintrag.Member("name")?.StringOr("") ?? "";
            }
        }

        AssertTrue(name.Length > 0,
            "**and the table gives it a name** -- and a reader that "
                + "guessed the editor's single letter found a file that "
                + "does not exist for every map but one");

        // **Und die Datei heisst nach dem Namen, und nicht nach dem
        // Buchstaben.**
        var datei = Projekt + "/img/tilesets/"
            + MzMapRenderer.TilesetFileName(name);
        AssertTrue(File.Exists(datei),
            "**and there is a file of that name** -- and it is named "
                + MzMapRenderer.TilesetFileName(name) + ", and the "
                + "extension is the encrypted one");

        // **Und es ist verschluesselt, und der Schluessel steht in
        // System.json.**
        var roh = File.ReadAllBytes(datei);
        AssertTrue(MzImageReader.IsEncrypted(roh),
            "**and it is encrypted** -- and the project says so itself in "
                + "System.json with hasEncryptedImages: true");

        var system = MzDataFile.ReadText(
            "data/System.json",
            File.ReadAllText(Projekt + "/data/System.json"));
        var schluessel = system.Root.Member("encryptionKey")?.StringOr("") ?? "";
        AssertTrue(schluessel.Length >= 32,
            "**and the key is there**");

        var bild = MzImageReader.Read(roh, schluessel, out var _);
        AssertTrue(bild != null, "**and it opens**");
        AssertTrue(Rm2kIndexedImage.TryParse(bild!, out var image, out _),
            "**and it decodes**");
        AssertTrue(image.Width % MzMapRenderer.TilePixels == 0,
            "**and its width is a whole number of tiles** -- and the tile "
                + "is 48 pixels, and it measured " + image.Width);
    }

    /// <summary>
    /// The map is painted, and it is not one colour.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the assertion is the colour count.</strong> A map
    /// painted in one colour is a map whose tileset was not found —
    /// <strong>and on the RM2K project that is exactly what
    /// <c>Map0001</c> looked like, one colour, and it passed
    /// everything until a test asked how many there were.</strong>
    /// </para>
    /// <para>
    /// <strong>And a real map of fourteen by eighteen tiles is not one
    /// colour.</strong> Measured on this project: 252 tiles, and their
    /// numbers run from 0 to 5519 across three sheets.
    /// </para>
    /// </remarks>
    public void Test_DieKarteWirdGemaltUndIstNichtEinfarbig()
    {
        if (!Vorhanden())
        {
            return;
        }

        var karte = MzDataFile.Read(
            "data/Map002.json",
            File.ReadAllBytes(Projekt + "/data/Map002.json"));
        var system = MzDataFile.ReadText(
            "data/System.json",
            File.ReadAllText(Projekt + "/data/System.json"));
        var schluessel = system.Root.Member("encryptionKey")?.StringOr("") ?? "";

        var bild = MzImageReader.Read(
            File.ReadAllBytes(
                Projekt + "/img/tilesets/Overworld.png_"),
            schluessel, out var _);
        Rm2kIndexedImage? tileset = null;
        AssertTrue(bild != null
            && Rm2kIndexedImage.TryParse(bild!, out tileset, out _),
            "**and the tileset opens**");

        // **Und ein Tileset ist neun Blaetter, und der Maler bekommt
        // alle davon** -- **und das erste heisst `World_A1`, und nicht
        // `Overworld`.**
        var blaetter = new System.Collections.Generic.List
            <UniversalRPG.Rm2k.Rendering.Rm2kIndexedImage?> { tileset };
        for (var index = 1; index < MzMapRenderer.SheetsPerTileset; index++)
        {
            blaetter.Add(null);
        }

        var breite = karte.Root.Member("width")?.IntOr(0) ?? 0;
        var hoehe = karte.Root.Member("height")?.IntOr(0) ?? 0;
        var pixel = new Rm2kPixelBuffer(
            breite * MzMapRenderer.TilePixels,
            hoehe * MzMapRenderer.TilePixels);

        var renderer = new MzMapRenderer();
        AssertTrue(renderer.Paint(karte.Root, blaetter, pixel, out var warum),
            "**and the map paints** -- and the refusal is: " + warum);

        var farben = MzMapRenderer.DistinctColours(pixel);
        AssertTrue(farben > 1,
            "**and it is not one colour** -- and it painted " + farben
                + ", and a map painted in one colour is a map whose "
                + "tileset was not found, and that is what Map0001 on "
                + "the RM2K project looked like until a test asked how "
                + "many colours there were");

        AssertEq(pixel.Width, breite * MzMapRenderer.TilePixels,
            "**and it is as wide as the map says**");
        AssertEq(pixel.Height, hoehe * MzMapRenderer.TilePixels,
            "**and as tall**");
    }

    /// <summary>
    /// The tile count in the file is the map's size times six.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is measured, and it is not one number per
    /// tile.</strong> <c>Map002</c> is 14 × 18 and its <c>data</c> holds
    /// <strong>1512</strong> numbers — <strong>and 14 × 18 = 252</strong>,
    /// <strong>so a reader that took one number per tile painted a
    /// quarter of the floor and left the rest empty.</strong>
    /// </para>
    /// <para>
    /// <strong>And all six are in use.</strong> Measured: 50, 48, 46, 51,
    /// 52 and 52 of the 252 tiles carry one.
    /// </para>
    /// </remarks>
    public void Test_EineKachelSechsZahlenUndNichtEine()
    {
        if (!Vorhanden())
        {
            return;
        }

        var karte = MzDataFile.Read(
            "data/Map002.json",
            File.ReadAllBytes(Projekt + "/data/Map002.json"));
        var breite = karte.Root.Member("width")?.IntOr(0) ?? 0;
        var hoehe = karte.Root.Member("height")?.IntOr(0) ?? 0;
        var felder = karte.Root.Member("data")?.Items;

        AssertEq(MzMapRenderer.NumbersPerTile, 6,
            "**and a tile is six numbers** -- and the constant is named, "
                + "so a reader that changes it says so");
        AssertEq(felder!.Count, breite * hoehe * MzMapRenderer.NumbersPerTile,
            "**and the file holds exactly that many** -- and 14 times 18 "
                + "is 252, and the file holds 1512, and a reader that took "
                + "one number per tile painted a quarter of the floor");

        // **Und alle sechs Felder tragen etwas.**
        var benutzt = new int[MzMapRenderer.NumbersPerTile];
        for (var index = 0; index + 5 < felder.Count; index += 6)
        {
            for (var feld = 0; feld < 6; feld++)
            {
                if (felder[index + feld].IntOr(0) != 0)
                {
                    benutzt[feld]++;
                }
            }
        }

        for (var feld = 0; feld < MzMapRenderer.NumbersPerTile; feld++)
        {
            AssertTrue(benutzt[feld] > 0,
                $"**and field {feld} carries a number** -- and it was on "
                    + $"{benutzt[feld]} cells");
        }

        // **And the six are six planes, and only the first four are tiles.**
        // Measured: plane four holds the shadow (the values 1 and 5 on this
        // map) and plane five holds the region (zero here). **A painter that
        // paired them as a lower and an upper half painted shadow values as
        // B-sheet tiles**, which is what this reader did before the layer
        // model was corrected.
        AssertEq(MzMapRenderer.NumbersPerTile, 6, "six planes per cell");
        var schatten = 0;
        for (var index = 4 * breite * hoehe; index < 5 * breite * hoehe; index++)
        {
            if (felder[index].IntOr(0) != 0)
            {
                schatten += 1;
            }
        }
        Console.WriteLine($"MZ layers: plane 4 carries {schatten} shadow marks");
        AssertTrue(schatten > 0,
            "plane four is the shadow and not a tile layer");
    }

    /// <summary>
    /// A tile number names a sheet and a cell of it, the engine's way.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the arithmetic is the engine's own, and not a reading of
    /// it.</strong> Measured at <c>Tilemap.prototype._addNormalTile</c>:
    /// <c>setNumber = isTileA5(tileId) ? 4 : 5 + Math.floor(tileId / 256)</c>,
    /// <c>sx = ((Math.floor(tileId / 128) % 2) * 8 + (tileId % 8)) * w</c>,
    /// <c>sy = (Math.floor((tileId % 256) / 8) % 16) * h</c>, and
    /// <c>isTileA5</c> is <c>tileId &gt;= 1536 &amp;&amp; tileId &lt; 1664</c>.
    /// </para>
    /// <para>
    /// <strong>And the older reading here was <c>id % 8</c> and
    /// <c>id / 8</c>, which is a sheet eight tiles wide.</strong> It agrees
    /// with the engine for every number below 128 -- which is why it looked
    /// right -- <strong>and disagrees from 128 on, and it sends an A5 tile
    /// to sheet twenty-four, which no tileset has.</strong> Measured: of
    /// Camellia's 56 tile ids, 24 fell outside the nine sheets and painted
    /// nothing.
    /// </para>
    /// </remarks>
    public void Test_EineKachelIdNenntEinBlattUndEineZelle()
    {
        AssertEq(MzMapRenderer.SheetSideTiles, 16,
            "a B-E sheet is sixteen tiles wide and high, which is 768 / 48");

        // Below 128 the two readings agree, which is why the old one looked
        // right: tile 5 is column five of row zero.
        var (blatt, spalte, zeile) = MzMapRenderer.TileCell(5);
        AssertEq(blatt, 5, "tile 5 is the B sheet");
        AssertEq(spalte, 5, "and column five");
        AssertEq(zeile, 0, "and row zero");

        (blatt, spalte, zeile) = MzMapRenderer.TileCell(81);
        AssertEq(blatt, 5, "tile 81 is the B sheet");
        AssertEq(spalte, 1, "and column one");
        AssertEq(zeile, 10, "and row ten");

        // And here the two readings part. The engine says column ten of row
        // zero; id / 8 said column two of row sixteen, which is off the sheet.
        (blatt, spalte, zeile) = MzMapRenderer.TileCell(130);
        AssertEq(blatt, 5, "tile 130 is the B sheet");
        AssertEq(spalte, 10, "and column ten");
        AssertEq(zeile, 0, "and row zero -- id / 8 said row sixteen");

        // An A5 tile. 1536 / 256 is six, and the number does not name the
        // sheet at all: A5 is sheet four.
        (blatt, spalte, zeile) = MzMapRenderer.TileCell(1536);
        AssertEq(blatt, 4, "tile 1536 is the A5 sheet");
        AssertEq(spalte, 0, "and column zero");
        AssertEq(zeile, 0, "and row zero");
    }

    /// <summary>
    /// And the map's autotiles are drawn, four quadrants each.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the half of a map that was missing.</strong>
    /// Measured on this project's start map: 297 tile numbers, of which 266
    /// are autotiles (41 of A1, 191 of A2 and 34 of A3) and 31 are A5 or B
    /// to E. <strong>A reader that draws only the thirty-one covers 10.4% of
    /// the map</strong>, and the room is a floor with holes in it.
    /// </para>
    /// <para>
    /// <strong>And an autotile is not one rectangle.</strong> Measured at
    /// <c>Tilemap.prototype._addAutotile</c>: the kind is
    /// <c>Math.floor((tileId - TILE_ID_A1) / 48)</c>, the shape is the
    /// remainder, and the shape's four entries of <c>FLOOR_AUTOTILE_TABLE</c>
    /// say which twenty-four pixel quadrant of the sheet goes into which
    /// corner. <strong>A reader that drew a whole rectangle there put the
    /// right picture in the wrong place, four times per tile.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieAutokachelnDerKarteWerdenGemalt()
    {
        if (!Vorhanden())
        {
            return;
        }

        AssertTrue(MzMapRenderer.IsAutotile(2048),
            "2048 is an autotile, the first of A1");
        AssertTrue(MzMapRenderer.IsAutotile(6426),
            "6426 is an autotile of A4");
        AssertTrue(!MzMapRenderer.IsAutotile(1536),
            "1536 is A5 and is drawn as one whole tile");
        AssertTrue(!MzMapRenderer.IsAutotile(130),
            "130 is a B sheet tile");

        var system = MzDataFile.Read("data/System.json",
            File.ReadAllBytes(Projekt + "/data/System.json"));
        var schluessel = system.Root.Member("encryptionKey")?.StringOr("") ?? "";
        var karte = MzDataFile.Read("data/Map002.json",
            File.ReadAllBytes(Projekt + "/data/Map002.json"));
        var tabelle = MzDataFile.Read("data/Tilesets.json",
            File.ReadAllBytes(Projekt + "/data/Tilesets.json"));
        var id = karte.Root.Member("tilesetId")?.IntOr(-1) ?? -1;
        var namen = tabelle.Root.Items[id].Member("tilesetNames")?.Items;

        var blaetter = new System.Collections.Generic.List
            <UniversalRPG.Rm2k.Rendering.Rm2kIndexedImage?>();
        for (var index = 0; index < MzMapRenderer.SheetsPerTileset; index++)
        {
            var name = index < namen!.Count ? namen[index].StringOr("") : "";
            Rm2kIndexedImage? blatt = null;
            if (name.Length > 0)
            {
                foreach (var dateiname in MzMapRenderer.TilesetFileNames(name))
                {
                    var datei = Projekt + "/img/tilesets/" + dateiname;
                    if (!File.Exists(datei))
                    {
                        continue;
                    }
                    var bild = MzImageReader.Read(
                        File.ReadAllBytes(datei), schluessel, out _);
                    if (bild != null)
                    {
                        Rm2kIndexedImage.TryParse(bild, out blatt, out _);
                    }
                    if (blatt != null)
                    {
                        break;
                    }
                }
            }
            blaetter.Add(blatt);
        }

        var breite = karte.Root.Member("width")?.IntOr(0) ?? 0;
        var hoehe = karte.Root.Member("height")?.IntOr(0) ?? 0;
        var pixel = new Rm2kPixelBuffer(
            breite * MzMapRenderer.TilePixels,
            hoehe * MzMapRenderer.TilePixels);
        var renderer = new MzMapRenderer();
        AssertTrue(renderer.Paint(karte.Root, blaetter, pixel, out var warum),
            "the map paints: " + warum);

        var lit = 0;
        for (var index = 3; index < pixel.Pixels.Length; index += 4)
        {
            if (pixel.Pixels[index] != 0)
            {
                lit += 1;
            }
        }
        var gesamt = pixel.Width * pixel.Height;
        var anteil = 100.0 * lit / gesamt;
        Console.WriteLine($"MZ autotiles: {lit} of {gesamt} pixels "
            + $"({anteil:F1}%), {MzMapRenderer.DistinctColours(pixel)} colours");

        // This map's tiles are opaque, so a painter that draws every layer
        // covers it. Measured before autotiles: 10.7%.
        AssertTrue(anteil > 95.0,
            $"the autotiles are drawn: {anteil:F1}% of the map is painted, and"
            + " a reader that draws only the A5 and B to E tiles covers 10.4%");
        AssertTrue(MzMapRenderer.DistinctColours(pixel) > 100,
            "and the map carries its colours, not one flat fill");
    }
}
