using System;
using System.Collections.Generic;

namespace UniversalRPG.Rm2k.Parser;

/// <summary>
/// Decodes what one battle animation draws on one frame.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the shape is liblcf's, from
/// <c>generator/csv/fields.csv</c>:</strong>
///
/// <code>
/// AnimationFrame     0x01  cells  Array&lt;AnimationCellData&gt;
/// AnimationCellData  0x01  valid          Int32
/// AnimationCellData  0x02  cell_id        Int32
/// AnimationCellData  0x03  x              Int32
/// AnimationCellData  0x04  y              Int32
/// AnimationCellData  0x05  zoom           Int32
/// AnimationCellData  0x06  tone_red       Int32
/// AnimationCellData  0x07  tone_green     Int32
/// AnimationCellData  0x08  tone_blue      Int32
/// AnimationCellData  0x09  tone_gray      Int32
/// AnimationCellData  0x0A  transparency   Int32
/// </code>
///
/// <para>
/// <strong>And the measured bytes of animation five, frame two:</strong>
///
/// <code>
/// 4 | 5,len 143,255,255,255,88,0,0 | 2,len 19 | 1,len 16 | 4,len 5,...
/// </code>
///
/// <para>
/// <strong>And the third number is the cell id and not a
/// length</strong>, -- <strong>and the cell's own fields follow</strong>,
/// -- <strong>and the frame ends at id zero.</strong>
/// </para>
/// </remarks>
public static class Rm2kAnimationZelle
{
    /// <summary>And the cells field of a frame.</summary>
    public const int FieldCells = 0x01;

    /// <summary>And whether the cell draws at all.</summary>
    public const int CellValid = 0x01;

    /// <summary>And which chipset cell.</summary>
    public const int CellId = 0x02;

    /// <summary>And the x offset.</summary>
    public const int CellX = 0x03;

    /// <summary>And the y offset.</summary>
    public const int CellY = 0x04;

    /// <summary>And the zoom.</summary>
    public const int CellZoom = 0x05;

    /// <summary>And the red tone.</summary>
    public const int ToneRed = 0x06;

    /// <summary>And the green tone.</summary>
    public const int ToneGreen = 0x07;

    /// <summary>And the blue tone.</summary>
    public const int ToneBlue = 0x08;

    /// <summary>And the grey tone.</summary>
    public const int ToneGray = 0x09;

    /// <summary>And how see-through it is.</summary>
    public const int Transparency = 0x0A;

    /// <summary>
    /// And it reads the cells of every frame of one animation.
    /// </summary>
    /// <param name="pFelder">The animation's raw fields.</param>
    /// <param name="pFrames">
    /// One list per frame, and each cell carries <c>cell_id</c>,
    /// <c>x</c>, <c>y</c>, <c>zoom</c>, the four tones and
    /// <c>transparency</c>.
    /// </param>
    /// <param name="pFehler">Why not, and empty on success.</param>
    /// <returns>Whether the field was read.</returns>
    public static bool TryDecode(
        Godot.Collections.Array<Godot.Collections.Dictionary> pFelder,
        out Godot.Collections.Array<Godot.Collections
            .Array<Godot.Collections.Dictionary>> pFrames,
        out string pFehler)
    {
        pFrames = new Godot.Collections
            .Array<Godot.Collections.Array<Godot.Collections
                .Dictionary>>();
        pFehler = "";

        if (pFelder == null)
        {
            pFehler = "the animation carries no raw fields";
            return false;
        }

        foreach (var feld in pFelder)
        {
            if (feld["id"].AsInt32() != 0x0C)
            {
                continue;
            }

            return TryDecodeFrames((byte[])feld["data"], out pFrames,
                out pFehler);
        }

        return true;
    }

    private static bool TryDecodeFrames(
        byte[] pDaten,
        out Godot.Collections.Array<Godot.Collections
            .Array<Godot.Collections.Dictionary>> pFrames,
        out string pFehler)
    {
        pFrames = new Godot.Collections
            .Array<Godot.Collections.Array<Godot.Collections
                .Dictionary>>();
        pFehler = "";

        var reader = new LcfBinaryReader(pDaten);
        var anzahl = reader.ReadBer();
        if (reader.HasError())
        {
            pFehler = "the frame list names no count";
            return false;
        }

        if (anzahl < 0 || anzahl > 4096)
        {
            pFehler = "the frame list claims " + anzahl
                + " frames, and that is outside what an animation"
                + " can hold";
            return false;
        }

        for (var frame = 0; frame < anzahl; frame++)
        {
            if (reader.IsEof())
            {
                pFehler = "the frame list ended after " + frame
                    + " of " + anzahl;
                return false;
            }

            var frameId = reader.ReadBer();
            if (frameId == 0)
            {
                return true;
            }

            // **Und  die  Zellen  kommen  als  Feld  `0x01`  des  Frames
            //  und  nicht  als  nackte  Zahl.**
            //
            // **Und  das  ist  ueber  alle  sechs  Frames  des  Spiels
            //  konsistent  gemessen:**
            //
            // <code>
            // Frame 0: id 1
            //   Feld 0x1 len 13: 1, 1, 3,1,32, 4,5,143,255,255,255,88, 0
            ///     Zellen 1
            ///     Zelle 1: id 1  Felder 0x3=32  0x4=(143,255,255,255,88)
            ///   Ende
            /// Frame 1: id 2
            ///   Feld 0x1 len 19: 1, 1, 2,1,1, 3,1,16, 4,5,143,...,0
            ///     Zelle 1: id 1  Felder 0x2=1  0x3=16  0x4=...  0x5=125
            /// </code>
            //
            // **Und  meine  zwei  vorherigen  Leser  nahmen  die  Zellen
            //  direkt  nach  der  Frame-Nummer  an**, -- **und  einer
            //  las  die  erste  Zahl  als  Laenge  und  stolperte  bei
            //  <c>0x8F</c>** -- **und  der  andere  las  sie  als  Feld-
            //  Id  und  gab  7112  Zellen  ohne  Namen  zurueck.**
            var zellen = new Godot.Collections
                .Array<Godot.Collections.Dictionary>();
            var gelesen = false;

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
                    if (!Ueberspringe(ref reader))
                    {
                        pFehler = "frame " + frame
                            + " has a field the file does not"
                            + " close";

                        return false;
                    }

                    continue;
                }

                var inhalt = reader.ReadBytes(laenge);
                if (reader.HasError())
                {
                    pFehler = "frame " + frame
                        + " has a field the file does not finish";
                    return false;
                }

                if (feldId != FieldCells)
                {
                    continue;
                }

                gelesen = true;
                if (!ZellenAusKapsel(inhalt, zellen,
                        out var warum))
                {
                    pFehler = "frame " + frame + ": " + warum;
                    return false;
                }
            }

            if (!gelesen)
            {
                pFehler = "frame " + frame + " names no cells";
                return false;
            }

            pFrames.Add(zellen);
        }

        return true;
    }

    /// <summary>
    /// And it reads the cells out of one frame's cell field.
    /// </summary>
    /// <param name="pInhalt">
    /// The field's bytes: a count, then that many cells, and each
    /// cell is its own number followed by its own fields.
    /// </param>
    /// <param name="pZellen">Where the cells go.</param>
    /// <param name="pFehler">Why not, and empty on success.</param>
    /// <returns>Whether the field was read.</returns>
    private static bool ZellenAusKapsel(
        byte[] pInhalt,
        Godot.Collections.Array<Godot.Collections.Dictionary> pZellen,
        out string pFehler)
    {
        pFehler = "";

        var reader = new LcfBinaryReader(pInhalt);
        var anzahl = reader.ReadBer();
        if (reader.HasError())
        {
            pFehler = "the cell field names no count";
            return false;
        }

        if (anzahl < 0 || anzahl > 4096)
        {
            pFehler = "the cell field claims " + anzahl
                + " cells, and that is outside what a frame can"
                + " hold";
            return false;
        }

        for (var z = 0; z < anzahl; z++)
        {
            if (reader.IsEof())
            {
                pFehler = "the cell list ended after " + z
                    + " of " + anzahl;
                return false;
            }

            var zelleId = reader.ReadBer();
            if (zelleId == 0)
            {
                return true;
            }

            var zelle = ZelleAusFeldern(ref reader, zelleId,
                out var warum);
            if (zelle == null)
            {
                pFehler = "cell " + z + ": " + warum;
                return false;
            }

            pZellen.Add(zelle);
        }

        return true;
    }

    /// <summary>
    /// And it reads one cell's fields, which follow the cell's own
    /// number and end at a zero field id.
    /// </summary>
    /// <param name="pReader">The reader, positioned at the cell.</param>
    /// <param name="pZellId">The cell's own number.</param>
    /// <param name="pFehler">Why not, and empty on success.</param>
    /// <returns>The cell, or null when the file does not finish it.</returns>
    private static Godot.Collections.Dictionary ZelleAusFeldern(
        ref LcfBinaryReader pReader, int pZellId, out string pFehler)
    {
        pFehler = "";

        var zelle = new Godot.Collections.Dictionary
        {
            { "entry_id", pZellId },
            { "cell_id", pZellId },
            { "valid", 1 },
            { "x", 0 },
            { "y", 0 },
            { "zoom", 100 },
            { "tone_red", 100 },
            { "tone_green", 100 },
            { "tone_blue", 100 },
            { "tone_gray", 100 },
            { "transparency", 0 },
        };

        while (!pReader.IsEof())
        {
            var feldId = pReader.ReadBer();
            if (feldId == 0)
            {
                return zelle;
            }

            var laenge = pReader.ReadBer();
            if (laenge < 0)
            {
                if (!Ueberspringe(ref pReader))
                {
                    pFehler = "a nested field is not closed";
                    return null;
                }

                continue;
            }

            var wert = pReader.ReadBytes(laenge);
            if (pReader.HasError())
            {
                pFehler = "a field is not finished by the file";
                return null;
            }

            var leser = new LcfBinaryReader(wert);
            var zahl = wert.Length > 0 ? leser.ReadBer() : 0;
            switch (feldId)
            {
                case CellValid:
                    zelle["valid"] = zahl;
                    break;

                case CellId:
                    zelle["cell_id"] = zahl;
                    break;

                case CellX:
                    zelle["x"] = zahl;
                    break;

                case CellY:
                    zelle["y"] = zahl;
                    break;

                case CellZoom:
                    zelle["zoom"] = zahl;
                    break;

                case ToneRed:
                    zelle["tone_red"] = zahl;
                    break;

                case ToneGreen:
                    zelle["tone_green"] = zahl;
                    break;

                case ToneBlue:
                    zelle["tone_blue"] = zahl;
                    break;

                case ToneGray:
                    zelle["tone_gray"] = zahl;
                    break;

                case Transparency:
                    zelle["transparency"] = zahl;
                    break;

                default:
                    if (!zelle.ContainsKey("unknown_fields"))
                    {
                        zelle["unknown_fields"] =
                            new Godot.Collections
                                .Array<Godot.Collections
                                    .Dictionary>();
                    }

                    ((Godot.Collections.Array<Godot.Collections
                        .Dictionary>)zelle["unknown_fields"])
                        .Add(new Godot.Collections.Dictionary
                        {
                            { "id", feldId },
                            { "data", wert },
                        });
                    break;
            }
        }

        return zelle;
    }

    private static bool Ueberspringe(ref LcfBinaryReader pReader)
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
