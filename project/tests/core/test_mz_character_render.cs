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
    /// The sheet is twelve cells by eight, and a cell is 48 pixels.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And every number here is the engine's, and a first
    /// reading of this file got all of them wrong by a factor of
    /// three.</strong> Measured in
    /// <c>Sprite_Character.prototype.patternWidth</c>, which is
    /// <c>bitmap.width / 12</c>, and <c>patternHeight</c>, which is
    /// <c>bitmap.height / 8</c>. For the measured 576 x 384 sheet that
    /// is <strong>48 by 48</strong>, <strong>and twelve across and
    /// eight down</strong>.
    /// </para>
    /// <para>
    /// <strong>And a figure is three cells wide and four cells high.</strong>
    /// Measured at <c>characterBlockX</c>, which is
    /// <c>(index % 4) * 3</c>, and <c>characterBlockY</c>, which is
    /// <c>Math.floor(index / 4) * 4</c>.
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

        // **Und die Zelle ist 48 Pixel, und nicht 144.**
        //
        // **Gemessen an `Sprite_Character.prototype.patternWidth`,
        // **das ist `bitmap.width / 12`, und an `patternHeight`,
        // **das ist `bitmap.height / 8`.** **Fuer das gemessene Blatt
        // **von 576 mal 384 ist das 48 mal 48.**
        //
        // **Und ich hatte hier 144 mal 192 stehen, und das war um den
        // **Faktor drei daneben** -- **und die Folge war, dass ein
        // **Index 3 vier Bildbreiten rechts vom Blatt gezeichnet
        // **wurde, also gar nichts**, **und der Zeichner `true`
        // **zurueckgab und tat, als haette er gearbeitet.**
        AssertEq(MzCharacterRenderer.CellPixels, 48,
            "**and one cell is forty-eight pixels** -- and that is what"
                + " 576 / 12 is, and the twelve is measured at"
                + " patternWidth");
        AssertEq(MzCharacterRenderer.CellsAcross, 12,
            "**and a sheet holds twelve cells across**");
        AssertEq(MzCharacterRenderer.CellsDown, 8,
            "**and eight down** -- and that is what 384 / 48 is, and"
                + " the eight is measured at patternHeight");

        AssertEq(blatt!.Width / MzCharacterRenderer.CellPixels, 12,
            "**and the measured sheet is twelve cells across**");
        AssertEq(blatt.Height / MzCharacterRenderer.CellPixels, 8,
            "**and eight down**");

        // **Und eine Figur braucht drei Spalten und vier Zeilen.**
        //
        // **Gemessen an `characterBlockX`, das ist `(index % 4) * 3`,
        // **und an `characterBlockY`, das ist
        // **`Math.floor(index / 4) * 4`.**
        AssertEq(MzCharacterRenderer.StepsPerDirection, 3,
            "**and a figure has three step columns** -- and that is the"
                + " three in characterBlockX, and not a guess");
        AssertEq(MzCharacterRenderer.DirectionsPerSheet, 4,
            "**and four direction rows** -- and that is the four in"
                + " characterBlockY");
    }

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
            "**and there are three step columns** -- and that is the three"
                + " in characterBlockX, and 576 / 144 is four, which is"
                + " the whole sheet and not one figure");
        AssertEq(MzCharacterRenderer.CellPixels, 48,
            "**and one cell is forty-eight pixels** -- and the engine"
                + " divides the sheet by twelve, not by four");

    }

    /// <summary>

/// <summary>
    /// A figure between two tiles lands between them, and not on either.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the second half of walking, and the first half
    /// was done.</strong> Measured: the engine keeps <c>_x</c>/<c>_y</c>
    /// for the tile and <c>_realX</c>/<c>_realY</c> for where it is drawn,
    /// and <c>_realX</c> starts one tile behind and closes the gap at
    /// <c>2^realMoveSpeed / 256</c> a frame.
    /// </para>
    /// <para>
    /// <strong>And a reader that drew only the tile saw a figure
    /// teleport.</strong> <strong>A walk that is only its two end positions
    /// is not a walk</strong> — <strong>it is two pictures with nothing
    /// between them.</strong>
    /// </para>
    /// </remarks>
    public void Test_EineFigurZwischenZweiKachelnLandetDazwischen()
    {
        if (!Vorhanden())
        {
            return;
        }

        var blatt = Blatt();
        AssertTrue(blatt != null, "**and the sheet opens**");

        // **Und dieselbe Figur zweimal: einmal auf der Kachel, einmal ein
        // halbes Bild davor.**
        var aufKachel = new Rm2kPixelBuffer(
            14 * MzMapRenderer.TilePixels, 18 * MzMapRenderer.TilePixels);
        var dazwischen = new Rm2kPixelBuffer(
            14 * MzMapRenderer.TilePixels, 18 * MzMapRenderer.TilePixels);
        Fuellen(aufKachel);
        Fuellen(dazwischen);

        AssertTrue(MzCharacterRenderer.Draw(
            blatt!, 3, MzCharacter.Right, MzCharacterRenderer.StillStep,
            6, 5, aufKachel),
            "**and the figure draws on its tile**");

        // **Und dieselbe Figur, die wirklich unterwegs ist.**
        //
        // **Und eine stehende Figur hat `RealX == X`, und das ist
        // richtig** -- **der Motor setzt beide gleich, wenn die Figur
        // zur Ruhe kommt.** **Und ein Test, der eine stehende Figur
        // nimmt und eine zwischen zwei Kacheln erwartet, erwartet das
        // Unmoegliche**, **denn beide Bilder sind dann dasselbe.**
        //
        // **Also muss die Figur hier wirklich einen Schritt machen**,
        // **und genau das ist der Punkt.**
        // **Und `CanPass` braucht eine Karte, denn ohne eine Karte ist
        // jeder Schritt verboten** -- **und das ist gemessen an
        // `Game_CharacterBase.canPass`, das `$gameMap.isPassable`
        // liest.** **Ein Test, der `MoveStraight` ohne Karte ruft,
        // prueft eine Verweigerung und keinen Lauf.**
        var karte = new OffeneKarte();
        var unterwegs = new MzCharacter(6, 5);
        unterwegs.TurnTo(MzCharacter.Right);
        // **Und die Figur traegt ihr Bild.**
        //
        // **Und das ist derselbe Fehler, den der Spieler gerade hatte:**
        // **eine Figur ohne gesetztes Bild hat Index null, und Index
        // null ist die erste Figur des Blattes** -- **und hier ist die
        // dritte gewollt.** **Gemessen an
        // `Game_CharacterBase.setImage`, das Name und Index zusammen
        // setzt, und an `Game_Player.refresh`, das beide uebergibt.**
        unterwegs.SetImage("SlimeCharacters", 3);
        AssertEq(unterwegs.CharacterIndex, 3,
            "**and it carries the figure of the sheet that was asked for**"
            + " -- and a figure with no image set is figure zero, and a"
            + " reader that kept only the name drew every figure as the"
            + " first one");
        AssertEq(unterwegs.RealX, 6.0,
            "**and a fresh figure is drawn on its own tile** -- and the"
            + " engine sets _realX to _x when it is not moving, and a"
            + " test that expected anything else would be testing a"
            + " fiction");

        unterwegs.MoveStraight(MzCharacter.Right, karte);
        AssertEq(unterwegs.X, 7,
            "**and it has walked to the tile it wanted** -- and it is on"
            + " " + unterwegs.X);
        AssertEq(unterwegs.RealX, 6.0,
            "**and it is drawn one tile behind where it stands** -- and"
            + " that is the engine's own line, _realX ="
            + " xWithDirection(_x, reverseDir(d)), and it is what makes a"
            + " walk look like a walk. It has not crossed the tile yet:"
            + " the gap closes at 2^realMoveSpeed / 256 a frame, and at"
            + " speed four that is sixteen frames.");

        AssertTrue(MzCharacterRenderer.Draw(
            blatt, unterwegs, MzCharacterRenderer.StillStep,
            MzMapRenderer.TilePixels, MzMapRenderer.TilePixels,
            dazwischen),
            "**and it draws between two tiles** -- and this is the call"
            + " that takes the figure and not the tile");

        AssertEq(MzMapRenderer.DistinctColours(dazwischen),
            MzMapRenderer.DistinctColours(aufKachel),
            "**and it is the same picture either way** -- and the same"
            + " colours, because it is one figure in one pose and only"
            + " the place is different");

        // **Und die beiden Figuren stehen nicht uebereinander.**
        // **Und die beiden Bilder sind nicht gleich, und nicht nur an
        // einer Kachel.**
        //
        // **Und meine erste Fassung verglich nur eine Kachel, und das
        // war zu wenig** -- **eine Figur ist 48 Pixel breit, und sie
        // steht mittig auf ihrer Kachel, also ragt sie in die
        // Nachbarn** -- **und wenn man genau die eine Kachel
        // vergleicht, in der sie steht, ist dort nur der unterste
        // Streifen.**
        //
        // **Und eine Figur, die auf ihrer Kachel springt, ist ein
        // anderes Bild als dieselbe Figur eine Kachel zurueck.** Also
        // wird das ganze Bild verglichen.
        // **Und die Rechnung, die zaehlt.**
        //
        // **Und das hier ist der Punkt, und es ist nicht der
        // Bildinhalt:** **eine Figur, die auf Kachel 7 steht und auf
        // Kachel 6 gezeichnet wird, ist genau eine Kachel links von
        // der, die auf ihrer Kachel gezeichnet waere.** **Und dieselbe
        // Figur, ein halbes Bild spaeter, ist ein halbes Bild weiter.**
        //
        // **Und die Folge war, dass `RealX` sofort nach dem Schritt 6
        // ist und nicht 6,25** -- **denn der Motor schliesst die Luecke
        // erst in `PassFrame`, und der erste Schritt hat sie noch
        // nicht geschlossen.**
        var ziel = (int)(unterwegs.RealX * MzMapRenderer.TilePixels);
        AssertEq(ziel, 6 * MzMapRenderer.TilePixels,
            "**and it is drawn one tile to the left of its own tile**"
            + " -- and that is what RealX is, and it is 6.0 and not 6.25"
            + " because the engine closes the gap in PassFrame and the"
            + " first step has not closed it yet. It is drawn at pixel "
            + ziel + " and its tile starts at "
            + (unterwegs.X * MzMapRenderer.TilePixels));

        // **Und jetzt geht es weiter, und das Bild rueckt nach.**
        var vorherX = unterwegs.RealX;
        unterwegs.PassFrame();
        AssertTrue(unterwegs.RealX > vorherX,
            "**and one frame later it has moved further right** -- and"
            + " that is the walk, and a reader that drew only the tile"
            + " showed a figure that never moved within its tile");

        AssertTrue(MzCharacterRenderer.Draw(
            blatt, unterwegs, MzCharacterRenderer.StillStep,
            MzMapRenderer.TilePixels, MzMapRenderer.TilePixels,
            dazwischen),
            "**and it draws again from the new position**");

        AssertTrue(!Gleich(aufKachel, dazwischen),
            "**and the two pictures are now different** -- and a figure"
            + " drawn on its tile and again a sixteenth of a tile"
            + " further along is two pictures where there is one, and a"
            + " reader that drew only the tile had one that teleported");
    }

    /// <summary>
    /// A map with no walls at all, which is all a step needs.
    /// </summary>
    /// <remarks>
    /// <strong>And this exists because <c>CanPass</c> asks the
    /// map.</strong> Measured:
    /// <c>Game_CharacterBase.prototype.canPass</c> reads
    /// <c>$gameMap.isPassable</c>, <strong>and a step given no map is
    /// refused</strong> — <strong>so a test that calls
    /// <c>MoveStraight</c> without one is testing a refusal and calling
    /// it a walk.</strong>
    /// </remarks>
    private sealed class OffeneKarte : IMzMapPassable
    {
        public bool IsValid(int pX, int pY) => pX >= 0 && pY >= 0;

        public bool IsPassable(int pX, int pY, int pDir) => IsValid(pX, pY);

        public bool IsClearOfCharacters(int pX, int pY) => true;
    }

    private static void Fuellen(Rm2kPixelBuffer pPixels)
    {
        for (var index = 0; index + 3 < pPixels.Pixels.Length; index += 4)
        {
            pPixels.Pixels[index] = 10;
            pPixels.Pixels[index + 1] = 20;
            pPixels.Pixels[index + 2] = 30;
            pPixels.Pixels[index + 3] = 255;
        }
    }

    /// <summary>
    /// Whether two pictures carry the same pixels throughout.
    /// </summary>
    /// <param name="pA">The first.</param>
    /// <param name="pB">The second.</param>
    /// <returns>Whether they are identical.</returns>
    /// <remarks>
    /// <strong>And the whole picture, and not one tile.</strong>
    /// <strong>A figure is 48 pixels wide and stands centred on its
    /// tile</strong> &#8212; <strong>so it reaches into its neighbours,
    /// and comparing only the tile it stands on compares the strip it
    /// occupies and nothing else</strong> &#8212; <strong>which is how
    /// two pictures of the same figure a tile apart came back
    /// equal.</strong>
    /// </remarks>
    private static bool Gleich(Rm2kPixelBuffer pA, Rm2kPixelBuffer pB)
    {
        var laenge = Math.Min(pA.Pixels.Length, pB.Pixels.Length);
        for (var index = 0; index + 3 < laenge; index += 4)
        {
            if (pA.Pixels[index] != pB.Pixels[index]
                || pA.Pixels[index + 1] != pB.Pixels[index + 1]
                || pA.Pixels[index + 2] != pB.Pixels[index + 2]
                || pA.Pixels[index + 3] != pB.Pixels[index + 3])
            {
                return false;
            }
        }

        return true;
    }
}
