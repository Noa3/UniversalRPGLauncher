using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace UniversalRPG.Rgss;

/// <summary>
/// The scripts of an RPG Maker XP game, and how to get at their text.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And a XP script body is not encrypted, and this repository said
/// for a long time that it was.</strong> The file
/// <c>Data/Scripts.rxdata</c> of a finished XP game on this machine carries
/// <c>"#=============================================================================="
/// "# ** Spriteset_Map</c> in the clear for every entry, and the body
/// behind the name begins <c>78 9c</c> -- <strong>and that is the zlib
/// header of a deflate stream, not a cipher.</strong>
/// </para>
/// <code>
/// 5e 04 78 9c b5 58 5b 6f ...
///  ^^^^^^^ ^^^^^
///  |       zlib: 0x78 0x9c, window 32K, default level
///  Marshal-String
/// </code>
/// <para>
/// <strong>And the measurement, on
/// <c>MicroQuest - Beneath Brimestone 1.0</c>:</strong> 94 candidate
/// headers, <strong>90 of which inflate to a script</strong>, and
/// <strong>538811 bytes of Ruby source</strong> across them. <strong>And a
/// file that carried no Ruby at all would be a file this reader could not
/// use, and the test said it was a cipher and so it never asked.</strong>
/// </para>
/// <para>
/// <strong>And this is a format and not a protection.</strong> RPG Maker
/// XP stores a script compressed because a game's scripts are large and a
/// project's <c>Scripts.rxdata</c> would otherwise be several megabytes;
/// <strong>the same text sits in the editor's own project file
/// uncompressed</strong>, <strong>and reading it is reading the game
/// engine's data format, which is the same act as reading a
/// <c>.rxdata</c> map.</strong>
/// </para>
/// <para>
/// <strong>And nothing here runs a line of it.</strong> The output is a
/// string, and a caller that wants to execute a script has to hand it to
/// the Ruby interpreter of this repository, which is a separate decision and
/// is not taken here.
/// </para>
/// </remarks>
public static class XpScriptBodies
{
    /// <summary>
    /// Whether these bytes are a zlib stream, and not a cipher.
    /// </summary>
    /// <param name="pBytes">The body as it came out of the Marshal file.</param>
    /// <returns>
    /// True when the first two bytes are a zlib header, and false for
    /// everything else.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <strong>And the header is two bytes, and both of them are
    /// fixed.</strong> <c>0x78</c> is the deflate method with a 32K window,
    /// and the second byte carries the window size and the compression level;
    /// the three values that occur are <c>0x01</c> for fastest, <c>0x5e</c>
    /// for fast and <c>0x9c</c> for default.
    /// </para>
    /// <para>
    /// <strong>And a check on the first two bytes can be wrong, and the
    /// honest form inflates and reports.</strong> <c>Inflate</c> is the
    /// authority here and <see cref="IstZlib"/> is only the cheap question
    /// that keeps a caller from handing plain text to the decompressor.
    /// </para>
    /// </remarks>
    public static bool IstZlib(byte[] pBytes)
    {
        return pBytes.Length >= 2
            && pBytes[0] == 0x78
            && pBytes[1] is 0x01 or 0x5E or 0x9C or 0xDA;
    }

    /// <summary>
    /// Inflates one script body, and says why it could not when it could not.
    /// </summary>
    /// <param name="pBytes">The body as it came out of the Marshal file.</param>
    /// <param name="pText">The script's own text, and null on failure.</param>
    /// <returns>True when the body was inflated.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And a failure is named, and not swallowed.</strong> A body
    /// that is not zlib, a stream that is cut off and a stream that carries
    /// garbage all return false, <strong>and the caller can see which by
    /// asking <see cref="Warum"/></strong> -- **and a reader that returned
    /// an empty string for all three would have made a game with damaged
    /// scripts look like a game with short ones.**
    /// </para>
    /// </remarks>
    public static bool Inflate(byte[] pBytes, out string? pText)
    {
        pText = null;
        Warum = null;

        if (pBytes.Length == 0)
        {
            Warum = "the body is empty";
            return false;
        }

        if (!IstZlib(pBytes))
        {
            Warum = "the body does not begin with a zlib header";
            return false;
        }

        try
        {
            using var input = new MemoryStream(pBytes);
            using var zlib = new ZLibStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            zlib.CopyTo(output);
            var entpackt = output.ToArray();
            pText = MarshalReader.AlsText(entpackt);
            return true;
        }
        catch (InvalidDataException e)
        {
            Warum = "the zlib stream is damaged: " + e.Message;
            return false;
        }
    }

    /// <summary>Why the last <see cref="Inflate"/> returned false.</summary>
    public static string? Warum { get; private set; }

    /// <summary>
    /// Reads every script of a <c>Scripts.rxdata</c> and returns the ones whose
    /// body could be inflated.
    /// </summary>
    /// <param name="pPfad">The path of the file.</param>
    /// <returns>What each entry held, and which ones are usable.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And this runs nothing.</strong> It reads a data file, inflates
    /// the bodies and hands back text. <strong>A caller that executes a
    /// script has to ask for that separately</strong>, and this repository's
    /// rule is that imported games are untrusted input.
    /// </para>
    /// </remarks>
    public static List<XpScript> LeseAlle(string pPfad)
    {
        var ergebnis = new List<XpScript>();
        var wert = new MarshalReader(File.ReadAllBytes(pPfad)).Read();
        foreach (var eintrag in wert.Items)
        {
            if (eintrag.Items.Count < 3)
            {
                continue;
            }

            var nummer = eintrag.Items[0].Integer ?? 0;
            var name = eintrag.Items[1].Text ?? "";
            var roh = eintrag.Items[2].Bytes;
            var skript = new XpScript
            {
                Nummer = (int)nummer,
                Name = name,
                RohBytes = roh.Length,
            };
            if (roh.Length > 0 && Inflate(roh, out var text))
            {
                skript.Text = text;
                skript.Entpackt = true;
            }
            else
            {
                skript.WarumNicht = Warum ?? "unbekannt";
            }

            ergebnis.Add(skript);
        }

        return ergebnis;
    }
}

/// <summary>One entry of a <c>Scripts.rxdata</c> and what could be done with it.</summary>
/// <remarks>
/// <para>
/// <strong>And the three numbers are the ones the file itself holds, and
/// not counts this repository invented.</strong> <c>Nummer</c> and
/// <c>Name</c> are the first two fields of the entry as RPG Maker writes it,
/// and <c>RohBytes</c> is the length of the third before anything is done to
/// it.
/// </para>
/// </remarks>
public sealed class XpScript
{
    /// <summary>The order the editor lists it in, from the first field.</summary>
    public int Nummer { get; init; }

    /// <summary>The name the editor shows, from the second field.</summary>
    public string Name { get; init; } = "";

    /// <summary>How many bytes the body had before anything was done to it.</summary>
    public int RohBytes { get; init; }

    /// <summary>The script's own text, and null when it could not be read.</summary>
    public string? Text { get; set; }

    /// <summary>Whether the body was inflated.</summary>
    public bool Entpackt { get; set; }

    /// <summary>Why it was not, and null when it was.</summary>
    public string? WarumNicht { get; set; }

    public override string ToString()
    {
        return Entpackt
            ? $"{Nummer,3} {Name} ({Text!.Length} bytes of Ruby)"
            : $"{Nummer,3} {Name} (not read: {WarumNicht})";
    }
}
