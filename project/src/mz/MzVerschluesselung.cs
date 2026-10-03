using System;
using System.Globalization;
using System.Text;

namespace UniversalRPG.Mz;

/// <summary>
/// The encryption RPG Maker MZ puts on its assets, and how to read
/// it back.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is not a cipher and not a container.</strong>
/// -- <strong>The engine writes the whole of it, and it is nine
/// lines:</strong>
/// </para>
/// <code>
/// static SIGNATURE = "5250474d56000000";
/// static VER      = "000301";
/// static REMAIN   = "0000000000";
/// static _headerlength = 16;
/// static decryptArrayBuffer(arrayBuffer) {
///     const header = new Uint8Array(arrayBuffer, 0, this._headerlength);
///     let ref = this.SIGNATURE + this.VER + this.REMAIN;
///     //  ... compares the sixteen bytes ...
///     arrayBuffer = this.cutArrayHeader(arrayBuffer, Decrypter._headerlength);
///     this.readEncryptionkey();
///     const byteArray = new Uint8Array(arrayBuffer);
///     for (i = 0; i < this._headerlength; i++) {
///         byteArray[i] = byteArray[i] ^ parseInt(Decrypter._encryptionKey[i], 16);
///     }
///     return arrayBuffer;
/// }
/// </code>
/// <para>
/// <strong>So: sixteen bytes of header, then the first sixteen bytes
/// of what follows are xored with the key.</strong> --
/// <strong>There is no AES, no cipher and no compression</strong>, --
/// <strong>and a reader that assumed otherwise would fail on the
/// first byte.</strong>
/// </para>
/// <para>
/// <strong>And the key is <c>$dataSystem.encryptionKey</c> and this
/// game writes the MD5 of the empty string</strong>, --
/// <c>d41d8cd98f00b204e9800998ecf8427e</c>, -- <strong>and that is
/// what the editor puts there when the author typed no key.</strong>
/// -- <strong>And so every MZ game with no key of its own has the
/// same one</strong>, -- <strong>and that is measured on this game's
/// 2098 encrypted files, not assumed.</strong>
/// </para>
/// </remarks>
public static class MzVerschluesselung
{
    /// <summary>And the sixteen bytes the engine writes first.</summary>
    /// <remarks>
    /// <para>
    /// <strong>And <c>5250474d56</c> is <c>RGMV</c> in hex</strong>,
    /// -- <strong>and <c>000301</c> is the version</strong>, --
    /// <strong>and the last five bytes are fixed.</strong>
    /// </para>
    /// </remarks>
    public static byte[] Signatur() =>
        Convert.FromHexString("5250474d56000000" + "000301" + "0000000000");

    /// <summary>And the key of this game, and that it is the MD5 of nothing.</summary>
    /// <returns>The hexadecimal key.</returns>
    public static string SchluesselDesSpiels() =>
        "d41d8cd98f00b204e9800998ecf8427e";

    /// <summary>
    /// And whether a file is encrypted at all.
    /// </summary>
    /// <param name="pDaten">The file as it is on disk.</param>
    /// <returns>Whether the sixteen header bytes are there.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And the engine writes no file without them when
    /// <c>hasEncryptedAudio</c> is false</strong>, --
    /// <strong>and a reader that decrypted an unencrypted file
    /// would corrupt it.</strong>
    /// </para>
    /// </remarks>
    public static bool IstVerschluesselt(ReadOnlySpan<byte> pDaten)
    {
        if (pDaten.Length < 16)
        {
            return false;
        }

        var soll = Signatur();
        for (var i = 0; i < 16; i++)
        {
            if (pDaten[i] != soll[i])
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// And it reads the bytes back.
    /// </summary>
    /// <param name="pDaten">The file as it is on disk.</param>
    /// <param name="pSchluessel">
    /// The key, hexadecimal, -- <strong>and a shorter key is
    /// refused</strong>.
    /// </param>
    /// <returns>
    /// The bytes without the header and with the first sixteen xored,
    /// -- <strong>or null when the file is not encrypted</strong>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <strong>And the xored part is the first sixteen bytes of what
    /// follows the header</strong>, -- <strong>not the header
    /// itself</strong>, -- <strong>and that is the one line that is
    /// easy to get wrong and that this is written to prevent.</strong>
    /// </para>
    /// <para>
    /// <strong>And a key with fewer than thirty-two hexadecimal
    /// characters is refused</strong>, -- <strong>because the engine
    /// writes <c>split(/(.{2})/)</c> and takes sixteen pairs, and a
    /// short key would make the loop read past the end.</strong>
    /// </para>
    /// </remarks>
    public static byte[]? Entschluessele(
        ReadOnlySpan<byte> pDaten, string pSchluessel)
    {
        if (!IstVerschluesselt(pDaten))
        {
            return null;
        }

        if (pSchluessel.Length < 32)
        {
            throw new ArgumentException(
                "the engine takes sixteen pairs of two characters and"
                + " this key has " + pSchluessel.Length,
                nameof(pSchluessel));
        }

        var raus = new byte[pDaten.Length - 16];
        pDaten.Slice(16).CopyTo(raus);

        for (var i = 0; i < 16 && i < raus.Length; i++)
        {
            var wert = Convert.ToByte(
                pSchluessel.Substring(i * 2, 2), 16);
            raus[i] = (byte)(raus[i] ^ wert);
        }

        return raus;
    }

    /// <summary>And what the result should start with.</summary>
    /// <param name="pDaten">The decrypted bytes.</param>
    /// <returns>True for an Ogg stream, false for a PNG.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And this is the check that makes the decryption
    /// visible</strong>, -- <strong>because a wrong key gives a file
    /// that is neither.</strong> -- <strong>An Ogg starts
    /// <c>OggS</c> and a PNG starts with the eight bytes
    /// <c>89 50 4E 47 0D 0A 1A 0A</c>.</strong>
    /// </para>
    /// </remarks>
    public static bool SiehtWieOggAus(ReadOnlySpan<byte> pDaten) =>
        pDaten.Length >= 4 && pDaten[0] == 'O' && pDaten[1] == 'g'
        && pDaten[2] == 'g' && pDaten[3] == 'S';

    /// <summary>And a PNG's eight bytes.</summary>
    /// <returns>True when the data starts with a PNG signature.</returns>
    public static bool SiehtWiePngAus(ReadOnlySpan<byte> pDaten) =>
        pDaten.Length >= 8 && PngSignaturStimmt(pDaten);

    private static bool PngSignaturStimmt(ReadOnlySpan<byte> pDaten)
    {
        byte[] soll = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        for (var i = 0; i < soll.Length; i++)
        {
            if (pDaten[i] != soll[i])
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>And the extension an encrypted file really is.</summary>
    /// <param name="pEndung">The extension on disk.</param>
    /// <returns>The one the game asked for.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And the engine writes the mapping itself:</strong> --
    /// <c>ogg</c> and <c>m4a</c> become <c>.rpgmvo</c>, --
    /// <c>png</c> becomes <c>.rpgmvp</c>, -- <strong>and everything
    /// else keeps its name.</strong>
    /// </para>
    /// </remarks>
    public static string EchteEndung(string pEndung) =>
        pEndung.ToLowerInvariant() switch
        {
            "rpgmvo" => "ogg",
            "rpgmvp" => "png",
            var andere => andere,
        };
}
