using System;

namespace UniversalRPG.Rm2k.Rendering;

/// <summary>
/// The 8 bit Windows bitmap RPG Maker 2000 wrote its assets in.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is not a rare format here.</strong> --
/// <strong>Measured on three finished games:</strong> Lisa has
/// <strong>ten chipsets and twenty-five character sheets as
/// <c>.bmp</c></strong>, -- Dragon Destiny has
/// <strong>one</strong>, -- Pom Gets Wi-Fi has
/// <strong>none</strong>. -- <strong>And a reader that only reads
/// PNG refuses to open a third of one game's images</strong>, --
/// <strong>and refuses them with a message that names the
/// format.</strong>
/// </para>
/// <para>
/// <strong>And what a real file carries</strong>, -- <strong>read out
/// of Lisas <c>main2.bmp</c>:</strong>
/// </para>
/// <code>
/// 'BM'                 two bytes
/// pixel offset         1078   (at byte 10)
/// info header size      40   (BITMAPINFOHEADER, at byte 14)
/// width                480
/// height               256
/// planes                 1
/// bits per pixel         8
/// compression            0   (BI_RGB)
/// palette at            54   (right behind the info header)
/// </code>
/// <para>
/// <strong>And the palette is four bytes per entry and in BGR
/// order</strong>, -- <strong>and the pixel rows run bottom
/// up</strong>, -- <strong>and both are written out here rather than
/// assumed, because a reader that forgets either one produces an
/// image that looks like it loaded.</strong>
/// </para>
/// <para>
/// <strong>And index 0 is the transparent one</strong>, -- <strong>
/// which the RM2K spec names in every format, so it does not change
/// with the container.</strong>
/// </para>
/// </remarks>
public static class Rm2kBmp
{
    /// <summary>And the two bytes every Windows bitmap starts with.</summary>
    public static byte[] Signatur() => [(byte)'B', (byte)'M'];

    /// <summary>And whether a file is one.</summary>
    /// <param name="pDaten">The file as it is on disk.</param>
    /// <returns>Whether the two bytes are there.</returns>
    public static bool IstBitmap(System.ReadOnlySpan<byte> pDaten) =>
        pDaten.Length >= 2 && pDaten[0] == (byte)'B' && pDaten[1] == (byte)'M';

    /// <summary>
    /// And it reads the eight bits per pixel, uncompressed form.
    /// </summary>
    /// <param name="pDaten">The file as it is on disk.</param>
    /// <param name="pBreite">The width in pixels.</param>
    /// <param name="pHoehe">The height in pixels.</param>
    /// <param name="pIndizes">One palette index per pixel, top row
    /// first.</param>
    /// <param name="pPalette">The colours, four bytes each, as
    /// B G R and a spare.</param>
    /// <param name="pFehler">Why it was refused, and empty when it
    /// was not.</param>
    /// <returns>Whether it read.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And the two refusals are the two that a real file can
    /// cause:</strong> -- <strong>sixteen bits per pixel</strong>
    /// -- <strong>and a compression the engine does not
    /// write.</strong> -- <strong>And anything refused here is named,
    /// because "not a PNG" is not an answer for a file that
    /// is one.</strong>
    /// </para>
    /// </remarks>
    public static bool TryParse(
        byte[] pDaten,
        out int pBreite, out int pHoehe,
        out byte[] pIndizes, out byte[] pPalette,
        out string pFehler)
    {
        pBreite = 0;
        pHoehe = 0;
        pIndizes = Array.Empty<byte>();
        pPalette = Array.Empty<byte>();
        pFehler = "";

        if (!IstBitmap(pDaten))
        {
            pFehler = "This is not a Windows bitmap.";
            return false;
        }

        if (pDaten.Length < 54)
        {
            pFehler = "The bitmap is shorter than its own header.";
            return false;
        }

        var pixelOffset = BitConverter.ToInt32(pDaten, 10);
        var infoSize = BitConverter.ToInt32(pDaten, 14);
        if (infoSize < 40)
        {
            pFehler = "The bitmap's info header is " + infoSize
                + " bytes, and the eight bit form is 40.";
            return false;
        }

        pBreite = BitConverter.ToInt32(pDaten, 18);
        var rohHoehe = BitConverter.ToInt32(pDaten, 22);
        var bpp = BitConverter.ToInt16(pDaten, 28);
        var kompression = BitConverter.ToInt32(pDaten, 30);

        if (bpp != 8)
        {
            pFehler = "The bitmap has " + bpp
                + " bits per pixel, and RPG Maker 2000 wrote eight.";
            return false;
        }

        if (kompression != 0)
        {
            pFehler = "The bitmap is compressed with method "
                + kompression + ", and RPG Maker 2000 wrote none.";
            return false;
        }

        // **Und  eine  positive  Hoehe  heisst  "von  unten  nach
        //  oben"** --  so  steht  es  im  Bitmap-Vertrag  von
        //  Windows:  das  erste  Byte  der  Bilddaten  gehoert
        //  zur  untersten  Zeile.  --  **Und  die  Spiele
        //  schreiben  genau  diese  Form,  also  wird  die
        //  Vorzeichenregel  von  BMP  beachtet  und  nicht
        //  angenommen.**
        //
        // **Und  das  war  der  Fehler  hinter  einem  echten
        //  Spiel,  das  in  Magenta  gemalt  wurde:**  der  Leser
        //  drehte  die  Regel  um  und  las  jede  Chipset- und
        //  CharSet-Kachel  von  Lisa  (zehn  Chipsets,  fuenfund-
        //  zwanzig  CharSets  als  `.bmp`)  auf  dem  Kopf,  --
        //  **und  die  Adresszeile  der  oberen  Ebene  landete
        //  damit  auf  der  pinkfarbenen  Marker-Flaeche  des
        //  Bildes,  die  das  ganze  Feld  ueberdeckte.**
        var vonUnten = rohHoehe > 0;
        pHoehe = Math.Abs(rohHoehe);

        if (pBreite <= 0 || pHoehe <= 0
            || pBreite > 4096 || pHoehe > 4096)
        {
            pFehler = "The bitmap is " + pBreite + " by " + pHoehe
                + ", which is not a size a chipset can have.";
            return false;
        }

        var paletteStart = 14 + infoSize;
        var farben = 1 << bpp;
        var paletteBytes = farben * 4;
        if (paletteStart + paletteBytes > pDaten.Length)
        {
            pFehler = "The bitmap's palette runs past the end of the"
                + " file.";
            return false;
        }

        pPalette = new byte[paletteBytes];
        Array.Copy(pDaten, paletteStart, pPalette, 0, paletteBytes);

        var zeile = (pBreite + 3) & ~3;
        var gebraucht = pixelOffset + zeile * pHoehe;
        if (pixelOffset < 0 || gebraucht > pDaten.Length)
        {
            pFehler = "The bitmap's pixels run past the end of the"
                + " file; it wants " + gebraucht + " bytes and has "
                + pDaten.Length + ".";
            return false;
        }

        pIndizes = new byte[pBreite * pHoehe];
        for (var y = 0; y < pHoehe; y++)
        {
            var quelle = vonUnten
                ? pixelOffset + (pHoehe - 1 - y) * zeile
                : pixelOffset + y * zeile;
            Array.Copy(pDaten, quelle, pIndizes, y * pBreite, pBreite);
        }

        return true;
    }
}
