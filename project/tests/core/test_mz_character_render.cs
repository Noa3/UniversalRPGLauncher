using System;
using System.Collections.Generic;
using System.IO;
using UniversalRPG.Rm2k.Rendering;
using UniversalRPG.Tests.Framework;
using UniversalRPG.Web;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Drawing the figures of an MZ map over its tiles.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the sheet is a grid, and the grid was measured.</strong>
/// Every character sheet in the finished project is 576 × 384, and a
/// figure tile is 144 × 192 — <strong>so the sheet is exactly a
/// 3 × 4 grid</strong>: three walk steps and four directions.
/// </para>
/// <para>
/// <strong>And the direction numbers are the engine's, not
/// 0–3.</strong> The project's own code numbers them
/// <c>down = 0, left = 1, right = 2, up = 3</c> and the map's field says
/// <c>2, 4, 6, 8</c>.
/// </para>
/// </remarks>
public partial class TestMzCharacterRender : TestBase
{
    private const string Projekt = "E:/RPGMakerGames/CamelliaCoronation-Win";

    private static bool Vorhanden()
    {
        return File.Exists(Projekt + "/img/characters/SlimeCharacters.png_")
            && File.Exists(Projekt + "/data/Map017.json")
            && File.Exists(Projekt + "/data/System.json");
    }

    private static string Fehler;

    private static MzCharacterSheet? Blatt()
    {
        var system = MzDataFile.ReadText(
            "data/System.json", File.ReadAllText(Projekt + "/data/System.json"));
        var schluessel = system.Root.Member("encryptionKey")?.StringOr("") ?? "";
        var bild = MzImageReader.Read(
            File.ReadAllBytes(Projekt + "/img/characters/SlimeCharacters.png_"),
            schluessel, out var _);
        if (bild == null)
        {
            return null;
        }

        // **Und dieses Blatt ist Farbtyp 6, und der Chipsatz-Leser
        // nimmt nur Farbtyp 3** -- **denn die Tilesets eines Projekts
        // und seine Figuren sind von zweierlei Art**: **das Tileset ist
        // eine Palette, die Figur hat echte Farben.**
        if (MzCharacterSheet.Read(bild, out var blatt, out var fehler))
        {
            return blatt;
        }

        Fehler = fehler;
        return null;
    }

    /// <summary>
    /// The sheet is a grid of three steps by four directions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the assertion is the size, and the sizes divide.</strong>
    /// 576 ÷ 144 is four figures across, 384 ÷ 192 is two down —
    /// <strong>and a sheet that does not divide is not a character
    /// sheet</strong>, <strong>and dividing one anyway gives a picture
    /// of the wrong figure, in the wrong pose, at the wrong place.</strong>
    /// </para>
    /// <para>
    /// <strong>And three steps per direction, measured as
    /// 576 ÷ 144 = 4, and two of the four columns are the second
    /// character of the row.</strong> <strong>The grid is three wide per
    /// character and two characters per row.</strong>
    /// </para>
    /// </remarks>
    public void Test_DasBlattIstEinRasterUndSeineGroesseStimmt()
    {
        if (!Vorhanden())
        {
            return;
        }

        var blatt = Blatt();
        AssertTrue(blatt != null,
            "**and the sheet opens** -- and the refusal is: "
                + Fehler);

        AssertEq(blatt!.Width % MzCharacterRenderer.FigurePixels, 0,
            "**and its width is a whole number of figures** -- and a "
                + "figure is 144 pixels, and it measured " + blatt.Width);
        AssertEq(blatt.Height % MzCharacterRenderer.FigureHeight, 0,
            "**and its height is a whole number of figure heights** -- and "
                + "that is 192, and it measured " + blatt.Height);

        AssertEq(blatt.Width / MzCharacterRenderer.FigurePixels, 4,
            "**and it holds four figures across** -- and that is what "
                + "576 divided by 144 is");
        AssertEq(blatt.Height / MzCharacterRenderer.FigureHeight, 2,
            "**and two down** -- and a sheet of two rows holds two "
                + "characters, which is why characterIndex picks a slot "
                + "and not a row");
    }

    /// <summary>
    /// The four directions are the engine's numbers, in its order.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And they are 2, 4, 6 and 8, and not 0, 1, 2 and 3.</strong>
    /// Measured on <c>Map017</c>: the images carry
    /// <c>direction: 6</c> and the rest of the four.
    /// </para>
    /// <para>
    /// <strong>And the order across the sheet is down, left, right,
    /// up.</strong> The project's own <c>rmmz_managers.js</c> numbers
    /// them <c>down = 0, left = 1, right = 2, up = 3</c> —
    /// <strong>and a reader that used 0–3 pointed every figure at the
    /// top-left corner</strong>, <strong>which is the up-facing
    /// row, and every figure in the game looked like it was walking
    /// away.</strong>
    /// </para>
    /// <para>
    /// <strong>And a direction that is none of the four is refused.</strong>
    /// A figure with a direction of zero is a file this reader cannot
    /// answer, <strong>and treating it as "down" pointed it somewhere
    /// the game never said.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieVierRichtungenSindDieZahlenDesMotors()
    {
        AssertTrue(MzCharacterRenderer.Cell(
            MzCharacter.Down, 0, out var zeile, out var spalte),
            "**and 2 is a direction** -- and it is down, and it is what "
                + "the engine's own constant says");
        AssertTrue(MzCharacterRenderer.Cell(
            MzCharacter.Left, 0, out var l, out _),
            "**and 4 is left**");
        AssertTrue(MzCharacterRenderer.Cell(
            MzCharacter.Right, 0, out var r, out _),
            "**and 6 is right**");
        AssertTrue(MzCharacterRenderer.Cell(
            MzCharacter.Up, 0, out var u, out _),
            "**and 8 is up**");

        AssertTrue(!MzCharacterRenderer.Cell(0, 0, out _, out _),
            "**and zero is not a direction** -- and a reader that treated "
                + "it as down pointed a figure somewhere the game never "
                + "said");
        AssertTrue(!MzCharacterRenderer.Cell(1, 0, out _, out _),
            "**and neither is one**");
    }

    /// <summary>
    /// A figure lands on the map and changes what is there.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the assertion is that the colours grew, and not that
    /// nothing threw.</strong> A figure drawn in the same colours as the
    /// floor it stands on is a figure nobody can see,
    /// <strong>and a test that only checks for exceptions cannot tell
    /// the two apart.</strong>
    /// </para>
    /// <para>
    /// <strong>And the figure is 144 pixels wide and a tile is 48, so it
    /// overhangs.</strong> <strong>That overhang is why the engine
    /// centres it</strong> — <strong>and a figure drawn at the tile's
    /// left edge looks wrong, and the test's width check is what would
    /// notice.</strong>
    /// </para>
    /// </remarks>
    public void Test_EineFigurLandetAufDerKarteUndAendertSie()
    {
        if (!Vorhanden())
        {
            return;
        }

        var blatt = Blatt();
        AssertTrue(blatt != null, "**and the sheet opens**");

        // **Und der Hintergrund, gegen den die Figur geprueft wird.**
        var pixel = new Rm2kPixelBuffer(
            14 * MzMapRenderer.TilePixels, 18 * MzMapRenderer.TilePixels);
        for (var index = 0; index + 3 < pixel.Pixels.Length; index += 4)
        {
            pixel.Pixels[index] = 10;
            pixel.Pixels[index + 1] = 20;
            pixel.Pixels[index + 2] = 30;
            // **Und Alpha 255, und nicht 0** -- **denn `Clear()`
            // setzt sie auf 0, und `DistinctColours` zaehlt nur, was
            // Alpha hat.** **Ein Hintergrund aus vier Nullen ist kein
            // Hintergrund, sondern ein Loch**, **und ein Test, das
            // einen aufbaut und dann eine Farbe zaehlt, zaehlt
            // null.**
            pixel.Pixels[index + 3] = 255;
        }

        var vorher = MzMapRenderer.DistinctColours(pixel);
        AssertEq(vorher, 1,
            "**and the background is one colour to begin with** -- and "
                + "that is what a picture with nothing on it looks like");

        AssertTrue(MzCharacterRenderer.Draw(
            blatt!, 3, MzCharacter.Down, MzCharacterRenderer.StillStep,
            4, 11, pixel),
            "**and the figure draws**");

        var nachher = MzMapRenderer.DistinctColours(pixel);
        AssertTrue(nachher > vorher,
            "**and the picture now has colours it did not have** -- and it "
                + "went from " + vorher + " to " + nachher + ", and a "
                + "figure drawn in the colours of the floor it stands on "
                + "is a figure nobody can see");

        // **Und die Figur steht nicht ausserhalb des Bildes.**
        AssertTrue(nachher > 1,
            "**and it is more than one**");
    }

    /// <summary>
    /// The still pose is the middle column, not the first.
    /// </summary>
    /// <remarks>
    /// <strong>And this is measured, and it is a matter of one
    /// column.</strong> The three columns of a direction are the two
    /// walk steps and the still pose; <strong>a reader that took column
    /// zero as the still pose put every standing figure mid-step</strong>,
    /// and a game full of characters looked as though it were
    /// constantly walking.
    /// </remarks>
    public void Test_DieStillPoseIstDieMittlereSpalte()
    {
        AssertEq(MzCharacterRenderer.StillStep, 1,
            "**and the still pose is column one** -- and the three columns "
                + "are left foot, right foot and still, and a reader that "
                + "took column zero put every standing figure mid-step");
        AssertEq(MzCharacterRenderer.StepsPerDirection, 3,
            "**and there are three steps** -- and 576 divided by 144 is "
                + "four, and two of the four columns are the second "
                + "character of the row");
        AssertEq(MzCharacterRenderer.FigurePixels, 144,
            "**and a figure is 144 pixels wide**");
        AssertEq(MzCharacterRenderer.FigureHeight, 192,
            "**and 192 high**");
    }
}
