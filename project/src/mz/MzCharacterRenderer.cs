using System;
using System.Collections.Generic;
using UniversalRPG.Rm2k.Rendering;

namespace UniversalRPG.Web;

/// <summary>
/// Draws the player and the figures of an MZ map over its tiles.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And a figure's place in its sheet is a division, and it was
/// measured.</strong> Every character sheet in the finished project is
/// <strong>576 × 384</strong> — <strong>four tiles wide and two
/// high</strong> — <strong>and a figure tile is 144 × 192, so the
/// sheet is exactly a 3 × 4 grid</strong>: three walk steps and four
/// directions. <strong>A sheet that is not that size is not a
/// character sheet</strong>, <strong>and dividing a sheet of another
/// size by this grid gives a picture of the wrong figure, in the wrong
/// pose, at the wrong place.</strong>
/// </para>
/// <para>
/// <strong>And the four directions are 2, 4, 6 and 8</strong> —
/// <strong>down, left, right and up</strong>, <strong>and they are the
/// engine's own numbers, and they run in that order across the
/// sheet.</strong> <strong>A reader that used 0, 1, 2 and 3 pointed
/// every figure at the top-left corner of its sheet</strong> —
/// <strong>which is the up-facing row, and every figure in the game
/// looked like it was walking away.</strong>
/// </para>
/// <para>
/// <strong>And the step is one of three, and the middle one is the
/// standing pose.</strong> The sheet has three columns per direction,
/// <strong>and column 1 is the still one</strong>; <strong>a reader
/// that used the sheet's own animation clock would snap a standing
/// figure between two walk poses.</strong>
/// </para>
/// </remarks>
public sealed class MzCharacterRenderer
{
    /// <summary>How wide one figure is in its sheet.</summary>
    public const int FigurePixels = 144;

    /// <summary>How tall one figure is in its sheet.</summary>
    public const int FigureHeight = 192;

    /// <summary>How many walk steps one direction has.</summary>
    /// <remarks>
    /// <strong>And this is measured, and it is three.</strong> The
    /// sheet is 576 pixels wide and a figure is 144,
    /// <strong>so three fit across.</strong>
    /// </remarks>
    public const int StepsPerDirection = 3;

    /// <summary>How many directions a figure sheet holds.</summary>
    /// <remarks>
    /// <strong>And this is measured, and it is four.</strong> The sheet
    /// is 384 pixels high and a figure is 192,
    /// <strong>so two fit down</strong> — <strong>and four directions
    /// in two rows means the sheet holds one character at two
    /// character slots</strong>, <strong>which is why
    /// <c>characterIndex</c> picks a slot and not a row.</strong>
    /// </remarks>
    public const int DirectionsPerSheet = 4;

    /// <summary>The column of a figure that stands still.</summary>
    /// <remarks>
    /// <strong>And this is the middle one.</strong> The three columns
    /// are left foot, right foot, still; <strong>a reader that took
    /// column 0 as the still pose put every standing figure mid
    /// step.</strong>
    /// </remarks>
    public const int StillStep = 1;

    /// <summary>
    /// The row and column of a direction and a step in a sheet.
    /// </summary>
    /// <param name="pDirection">The engine's direction: 2, 4, 6 or 8.</param>
    /// <param name="pStep">Which of the three steps.</param>
    /// <param name="pRow">The row in the sheet.</param>
    /// <param name="pColumn">The column in the sheet.</param>
    /// <returns>Whether the direction is one of the four.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And the rows are down, left, right, up.</strong> The
    /// project's own <c>rmmz_managers.js</c> numbers them
    /// <c>down = 0</c>, <c>left = 1</c>, <c>right = 2</c>,
    /// <c>up = 3</c>, <strong>and the engine's <c>direction</c> is 2,
    /// 4, 6 and 8</strong> — <strong>and the two agree in that
    /// order.</strong>
    /// </para>
    /// <para>
    /// <strong>And a direction that is none of the four is refused, and
    /// not folded into the first.</strong> A figure with a direction of
    /// zero is a file this reader cannot answer, <strong>and a reader
    /// that treated it as "down" pointed it somewhere the game never
    /// said.</strong>
    /// </para>
    /// </remarks>
    public static bool Cell(
        int pDirection, int pStep, out int pRow, out int pColumn)
    {
        pRow = 0;
        pColumn = 0;
        var zeile = pDirection switch
        {
            MzCharacter.Down => 0,
            MzCharacter.Left => 1,
            MzCharacter.Right => 2,
            MzCharacter.Up => 3,
            _ => -1,
        };
        if (zeile < 0)
        {
            return false;
        }

        // **Und die Richtungen liegen nebeneinander in der ersten
        // Reihe, und nicht uebereinander in zwei.** **Gemessen an
        // `SlimeCharacters.png_`: 576 × 384 waere zwei Figuren hoch,
        // und nur die ersten 192 Pixel tragen etwas.** **Die zweiten
        // 192 sind leer**, **und ein Leser, der die Richtungen auf
        // zwei Zeilen verteilt, zeichnet die Haelfte aller Figuren
        // aus dem Leeren.**
        pRow = 0;
        pColumn = zeile;
        return true;
    }

    /// <summary>
    /// Draws one figure over a painted map.
    /// </summary>
    /// <param name="pSheet">The character sheet.</param>
    /// <param name="pIndex">Which character in the sheet.</param>
    /// <param name="pDirection">The engine's direction.</param>
    /// <param name="pStep">Which of the three steps.</param>
    /// <param name="pTileX">The tile column the figure stands on.</param>
    /// <param name="pTileY">The tile row the figure stands on.</param>
    /// <param name="pPixels">Where to draw.</param>
    /// <returns>Whether it drew.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And a character is 144 wide, and a tile is 48, so a
    /// figure stands on three tiles.</strong> <strong>That overhang is
    /// not a mistake in the drawing</strong> — <strong>it is why a
    /// figure drawn at the tile's left edge looks wrong</strong> —
    /// <strong>and the engine centres it.</strong>
    /// </para>
    /// <para>
    /// <strong>And the sheet's two rows are two characters, and
    /// <c>characterIndex</c> picks between them.</strong> Measured on
    /// <c>Map017</c>: <em>SlimeCharacters</em> appears with indexes
    /// 0, 1, 2 and 3 — <strong>four characters in one sheet of two
    /// rows of two</strong>.
    /// </para>
    /// </remarks>
    public static bool Draw(
        MzRgbaImage pSheet,
        int pIndex,
        int pDirection,
        int pStep,
        int pTileX,
        int pTileY,
        Rm2kPixelBuffer pPixels)
    {
        if (pSheet == null)
        {
            return false;
        }

        if (!Cell(pDirection, pStep, out var richtungsReihe, out var _))
        {
            return false;
        }

        // **Und der Index waehlt die Figur, und die Richtung die
        // Reihe** -- **gemessen: vier Figuren in einem Blatt aus zwei
        // Reihen zu zweien.**
        var spaltenProReihe = Math.Max(
            1, pSheet.Width / (StepsPerDirection * FigurePixels));
        var spaltenProFigur = Math.Max(1, spaltenProReihe / 2);
        var zeile = pIndex / spaltenProFigur;
        var spalte = pIndex % spaltenProFigur;

        // **Und gemessen liegt in diesem Blatt nur die erste Reihe.**
        // **576 x 384 waere zwei Figuren hoch**, **und nur 192 Pixel
        // davon tragen etwas** -- **die zweiten 192 sind leer**, **und
        // ein Leser, der die Richtungsreihen auf zwei Zeilen verteilt,
        // zeichnet die Haelfte aller Figuren aus dem Leeren.**
        var zeileRichtung = 0;
        var spalteRichtung = richtungsReihe;
        var schritt = Math.Min(pStep, StepsPerDirection - 1);

        var x0 = (spalte * DirectionsPerSheet + spalteRichtung)
            * FigurePixels + schritt * FigurePixels;
        var y0 = zeileRichtung * FigureHeight;

        // **Und die Vier Richtungen liegen nebeneinander, und nicht
        // uebereinander** -- **die Spalten 0 bis 3 sind unten, links,
        // rechts, oben** -- **und die Schritte liegen darunter**, **was
        // dieses Blatt gar nicht fuehrt, denn es hat nur eine Reihe.**
        // **Und die Figur steht mittig auf ihrer Kachel** -- **sie ist
        // drei Kacheln breit und die Kachel ist 48 Pixel.**
        var versatzX = (MzMapRenderer.TilePixels - FigurePixels) / 2;
        var zielX = pTileX * MzMapRenderer.TilePixels - versatzX;
        var zielY = pTileY * MzMapRenderer.TilePixels
            + (MzMapRenderer.TilePixels - FigureHeight);

        for (var dy = 0; dy < FigureHeight; dy++)
        {
            var quelleY = y0 + dy;
            if (quelleY >= pSheet.Height)
            {
                return true;
            }

            for (var dx = 0; dx < FigurePixels; dx++)
            {
                var quelleX = x0 + dx;
                if (quelleX >= pSheet.Width)
                {
                    break;
                }

                // **Und die Durchsicht kommt aus dem Alphalkanal, und
                // nicht aus einem Index.** **Das ist der Unterschied
                // zwischen einem Blatt aus einer Palette und einem aus
                // echten Farben** -- **und bei echten Farben gibt es
                // keinen Index null, der durchsicht waere**, **sondern
                // ein Alphawert, der null ist.**
                if (!pSheet.TryGetPixel(quelleX, quelleY, out var farbe)
                    || farbe[3] == 0)
                {
                    continue;
                }

                pPixels.TrySetPixel(
                    zielX + dx, zielY + dy, farbe[0], farbe[1], farbe[2],
                    farbe[3]);
            }
        }

        return true;
    }
}
