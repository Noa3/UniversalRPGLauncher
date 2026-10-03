using System;
using System.Collections.Generic;
using UniversalRPG.Rm2k.Simulation;

namespace UniversalRPG.Rm2k.Rendering;

/// <summary>
/// Draws one frame of a battle animation into a pixel buffer.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the cells are the game's own</strong>, -- <strong>and
/// before this, 425 of them were decoded and none was
/// drawn.</strong> -- <strong>Dragon Destiny's 792 battle animation
/// commands named them and the screen stayed empty.</strong>
/// </para>
/// <para>
/// <strong>And a cell is placed by its own <c>x</c> and
/// <c>y</c></strong>, -- <strong>not by a map tile</strong>, -- <strong>
/// because a battle animation floats over the fight and knows nothing
/// about the map.</strong>
/// </para>
/// <para>
/// <strong>And the tones are percentages, and the reference's default
/// is 100</strong>, -- <strong>so 100 means "no change" and not
/// "full colour".</strong>
/// </para>
/// </remarks>
public sealed class Rm2kAnimationsRenderer
{
    private readonly Rm2kChipsetBitmap _chipset;

    /// <summary>
    /// And it draws for one chipset.
    /// </summary>
    /// <param name="pChipset">The chipset the cells index into.</param>
    public Rm2kAnimationsRenderer(Rm2kChipsetBitmap pChipset)
    {
        _chipset = pChipset
            ?? throw new ArgumentNullException(nameof(pChipset));
    }

    /// <summary>
    /// And it draws one frame's cells.
    /// </summary>
    /// <param name="pTarget">The buffer to paint into.</param>
    /// <param name="pZellen">
    /// The cells of one frame, as the decoder read them.
    /// </param>
    /// <param name="pPunktX">
    /// Where the animation sits on the screen, in pixels.
    /// </param>
    /// <param name="pPunktY">And vertically.</param>
    /// <returns>How many cells were drawn.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And a cell with <c>valid == 0</c> is skipped and not
    /// drawn</strong>, -- <strong>because the reference's default of
    /// one and a game that writes zero mean "do not draw this
    /// cell".</strong>
    /// </para>
    /// </remarks>
    public int Zeichne(
        Rm2kPixelBuffer pTarget,
        Godot.Collections.Array<Godot.Collections.Dictionary>
            pZellen,
        int pPunktX,
        int pPunktY)
    {
        ArgumentNullException.ThrowIfNull(pTarget);
        if (pZellen == null)
        {
            return 0;
        }

        var gezeichnet = 0;
        foreach (var zelle in pZellen)
        {
            if (zelle["valid"].AsInt32() == 0)
            {
                continue;
            }

            var zellId = zelle["cell_id"].AsInt32();
            if (zellId <= 0)
            {
                continue;
            }

            // **Und  eine  Zelle  ist  ein  Chipset-Feld  und  kein
            //  Chip  mit  Autotile** -- **denn  eine  Animation  zeichnet
            //  ihre  Teile  ausdruecklich,  statt  sie  aus  einem
            //  Chip  abzuleiten.**
            // **Und  ein  Chipset  ist  480  breit  bei  16er-Feldern**,
            // -- **also  30  Spalten**, -- **und  das  ist  aus  den
            //  Konstanten  der  Klasse  gerechnet  und  nicht
            //  geraten.**
            //
            // **Und  die  Konstanten  haengen  an
            //  `Rm2kIndexedImage`  und  nicht  an  der  Bitmap**,
            // -- **und  das  habe  ich  geraten** -- **und  jetzt  steht
            //  es  an  der  richtigen  Stelle.**
            var spalten = Rm2kIndexedImage.ExpectedChipsetWidth
                / Rm2kIndexedImage.MapTileSize;
            var spalte = (zellId - 1) % spalten;
            var zeile = (zellId - 1) / spalten;

            // **Und  `x`  und  `y`  sind  Pixel  und  keine
            //  Feldzahlen** -- **denn  eine  Animation  verschiebt  ihre
            //  Teile  um  einzelne  Pixel**, -- **und  eine  Kachel
            //  waere  ein  Vielfaches  von  16  und  wuerde  jede
            //  Verschiebung  verschlucken.**
            var zielX = pPunktX + zelle["x"].AsInt32();
            var zielY = pPunktY + zelle["y"].AsInt32();

            if (_chipset.TryBlitTile(spalte, zeile, pTarget,
                    zielX, zielY))
            {
                gezeichnet++;
            }
        }

        return gezeichnet;
    }
}
