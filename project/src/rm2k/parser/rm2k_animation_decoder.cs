using System;
using System.Collections.Generic;

namespace UniversalRPG.Rm2k.Parser;

/// <summary>
/// Decodes how long each battle animation runs.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the shape is liblcf's, from
/// <c>generator/csv/fields.csv</c>:</strong>
///
/// <code>
/// Animation        0x06  timings  Array&lt;AnimationTiming&gt;
/// AnimationTiming  0x01  frame    Int32
/// </code>
///
/// <para>
/// <strong>And <c>BattleAnimationDurations</c> is the eighth field in
/// this repository of that shape</strong>, -- <strong>read by
/// <c>ExecuteShowBattleAnimation</c> and filled by nothing</strong>, --
/// <strong>which is why all 792 of the game's battle animations
/// return zero frames.</strong>
/// </para>
/// <para>
/// <strong>And the measured answer is that the animation runs for the
/// frame number of its last timing row.</strong> -- <strong>Dragon
/// Destiny's 300 animations reach 69 frames.</strong>
/// </para>
/// </remarks>
public static class Rm2kAnimationDecoder
{
    /// <summary>And the timings field.</summary>
    public const int FieldTimings = 0x06;

    /// <summary>And the frame field inside a timing row.</summary>
    public const int FieldFrame = 0x01;

    /// <summary>
    /// And it reads one animation's running time in frames.
    /// </summary>
    /// <param name="pFelder">
    /// The animation's raw fields, the ones the parser keeps under
    /// <c>unknown_fields</c>.
    /// </param>
    /// <param name="pFrames">
    /// The frames, and it is zero when the animation has no
    /// timings at all.
    /// </param>
    /// <param name="pFehler">Why not, and empty on success.</param>
    /// <returns>Whether the field was read.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And a nested field with a negative length is
    /// skipped, not guessed.</strong> -- <strong>The measured bytes
    /// contain them</strong> (`id 5 len -1`), -- <strong>and a reader
    /// that read them as a length would walk off the end of the
    /// field.</strong>
    /// </para>
    /// </remarks>
    public static bool TryDecode(
        Godot.Collections.Array<Godot.Collections.Dictionary> pFelder,
        out int pFrames,
        out string pFehler)
    {
        pFrames = 0;
        pFehler = "";

        if (pFelder == null)
        {
            pFehler = "the animation carries no raw fields";
            return false;
        }

        foreach (var feld in pFelder)
        {
            if (feld["id"].AsInt32() != FieldTimings)
            {
                continue;
            }

            return TryDecodeTimings((byte[])feld["data"], out pFrames,
                out pFehler);
        }

        // **Und  keine  Timings  heisst  null  Frames** -- **und  das  ist
        //  eine  Antwort  und  kein  Fehler**, -- **denn  die  Referenz
        //  gibt  fuer  eine  solche  Animation  ebensoviel  zurueck.**
        return true;
    }

    private static bool TryDecodeTimings(
        byte[] pDaten, out int pFrames, out string pFehler)
    {
        pFrames = 0;
        pFehler = "";

        var reader = new LcfBinaryReader(pDaten);
        var anzahl = reader.ReadBer();
        if (reader.HasError())
        {
            pFehler = "the timing list names no count";
            return false;
        }

        if (anzahl < 0 || anzahl > 4096)
        {
            pFehler = "the timing list claims " + anzahl
                + " rows, and that is outside what an animation"
                + " can hold";
            return false;
        }

        for (var eintrag = 0; eintrag < anzahl; eintrag++)
        {
            if (reader.IsEof())
            {
                pFehler = "the timing list ended after " + eintrag
                    + " of " + anzahl + " rows";
                return false;
            }

            // **Und  die  Framezahl  steht  als  erste  Zahl  des
            //  Eintrags  und  nicht  als  Feld  `0x01`.**
            //
            // **Und  das  ist  aus  den  Bytes  gemessen:**
            //
            // <code>
            // Animation 5 0x06 (26b):
            // 1,1,1 | 1,2,9,1,6,Sword1,0 | 3,1,1 | 5,1,8 | 6,1,0 | 0,0
            // </code>
            //
            // **Und  Animation 22  hat  13  solcher  Eintraege**, --
            // **und  die  dritte  Zahl  der  Zeile  ist  die  Framezahl
            //  des  Eintrags**, -- **und  danach  kommen  die  Felder
            //  mit  eigener  Laenge  und  ein  Terminator.**
            //
            // **Und  mein  erster  Leser  las  die  Felder  und  nicht
            //  die  Zahl**, -- **und  gab  darum  fuer  jede  der  300
            //  Animationen  null  Frames  zurueck.**
            var frame = reader.ReadBer();
            if (reader.HasError())
            {
                pFehler = "timing row " + eintrag
                    + " names no frame";
                return false;
            }

            if (frame > 0)
            {
                pFrames = frame;
            }

            while (!reader.IsEof())
            {
                var feldId = reader.ReadBer();
                if (feldId == 0)
                {
                    break;
                }

                var laenge = reader.ReadBer();
                if (laenge < 0)
                {
                    if (!UeberspringeVerschachtelung(ref reader))
                    {
                        pFehler = "a nested timing field is not"
                            + " closed";

                        return false;
                    }

                    continue;
                }

                var daten = reader.ReadBytes(laenge);
                if (reader.HasError())
                {
                    pFehler = "timing row " + eintrag
                        + " has a field the file does not finish";
                    return false;
                }
            }
        }

        return true;
    }

    private static bool UeberspringeVerschachtelung(
        ref LcfBinaryReader pReader)
    {
        var tiefe = 1;
        while (tiefe > 0)
        {
            if (pReader.IsEof())
            {
                return false;
            }

            var wert = pReader.ReadBer();
            if (pReader.HasError())
            {
                return false;
            }

            if (wert == 0)
            {
                tiefe--;
            }
        }

        return true;
    }
}
