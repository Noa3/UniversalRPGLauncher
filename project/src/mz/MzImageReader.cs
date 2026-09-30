using System;
using System.Collections.Generic;
using System.Globalization;

namespace UniversalRPG.Web;

/// <summary>
/// Reads an image out of a project whose files RPG Maker encrypted.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the algorithm is the project's own, and it is in the
/// project's own file.</strong> Measured in
/// <c>CamelliaCoronation-Win/js/rmmz_core.js</c>,
/// <c>Utils.decryptArrayBuffer</c>:
/// </para>
/// <code>
/// const header = new Uint8Array(source, 0, 16);
/// if (headerHex !== "52,50,47,4d,56,0,0,0,0,3,1,0,0,0,0,0") throw ...;
/// const body = source.slice(16);
/// const key = this._encryptionKey.match(/.{2}/g);
/// for (let i = 0; i &lt; 16; i++)
///     view.setUint8(i, view.getUint8(i) ^ parseInt(key[i], 16));
/// </code>
/// <para>
/// <strong>And two details of it are the whole difference between an
/// image and noise.</strong> The header is <em>checked</em>, not
/// stripped blindly; <strong>and the XOR applies to the first sixteen
/// bytes <em>after</em> the header</strong> — <strong>so a reader that
/// XORs the first sixteen bytes of the file has encrypted the header
/// instead of the image</strong>, and got a file starting
/// <c>cb0ca26d…</c> where a PNG starts <c>89504e47…</c>.
/// </para>
/// <para>
/// <strong>And the key is two hex digits at a time, not sixteen
/// characters.</strong> <c>995ce5201bae90d3…</c> gives the bytes
/// <c>0x99 0x5c 0xe5 0x20 …</c>, <strong>and a reader that took the
/// first sixteen characters as ASCII produced
/// <c>6b69722e33353230</c></strong> — <strong>which is the text
/// "kir.3520", and looks plausible enough to be believed.</strong>
/// </para>
/// <para>
/// <strong>And this is not a bypass of the security boundary.</strong>
/// The rule from <c>AGENTS.md</c> is that a game's own scripts are data
/// and are not executed, <strong>and an image is not a script</strong> —
/// it is the drawing a player looks at, <strong>and the engine's own
/// JavaScript carries the algorithm in plain text.</strong>
/// </para>
/// </remarks>
public static class MzImageReader
{
    /// <summary>The sixteen bytes every encrypted file starts with.</summary>
    /// <remarks>
    /// <strong>And this is a fact and not a pattern.</strong> It is the
    /// literal in the project's own <c>rmmz_core.js</c>,
    /// <c>"52,50,47,4d,56,0,0,0,0,3,1,0,0,0,0,0"</c> — <strong>and a
    /// reader that guessed the pattern from one file would fail on a
    /// game whose maker wrote a newer version number.</strong>
    /// </remarks>
    public static byte[] EncryptedHeader { get; } =
    {
        0x52, 0x50, 0x47, 0x4D, 0x56, 0x00, 0x00, 0x00,
        0x00, 0x03, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00,
    };

    /// <summary>How many bytes at the start of the body are scrambled.</summary>
    /// <remarks>
    /// <strong>And this is the loop bound, and not a guess:</strong> the
    /// project's own code writes <c>for (let i = 0; i &lt; 16; i++)</c>.
    /// </remarks>
    public const int ScrambledBytes = 16;

    /// <summary>
    /// Whether a file is one of this project's encrypted images.
    /// </summary>
    /// <param name="pData">The file as it sits on disk.</param>
    /// <returns>
    /// True when it starts with the header the engine's own code names.
    /// </returns>
    public static bool IsEncrypted(byte[] pData)
    {
        if (pData == null || pData.Length < EncryptedHeader.Length)
        {
            return false;
        }

        for (var index = 0; index < EncryptedHeader.Length; index++)
        {
            if (pData[index] != EncryptedHeader[index])
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Returns the image bytes, and decrypts them when the project
    /// encrypted them.
    /// </summary>
    /// <param name="pData">The file as it sits on disk.</param>
    /// <param name="pKey">The project's <c>encryptionKey</c>.</param>
    /// <param name="pPlain">Whether what comes back is a plain image.</param>
    /// <returns>
    /// The bytes of the image, or null when the file is encrypted and
    /// the key is not usable.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <strong>And a file that is not encrypted comes back as it
    /// is.</strong> The engine's own loader asks
    /// <c>Utils.hasEncryptedImages()</c> before it decrypts anything,
    /// <strong>and a reader that decrypts unconditionally turned every
    /// plain image in a mixed project into noise.</strong>
    /// </para>
    /// <para>
    /// <strong>And a key that is not thirty-two hex digits is
    /// refused.</strong> Measured: a project without image encryption
    /// writes an empty key, <strong>and half a key would scramble the
    /// first sixteen bytes of the image and leave the rest
    /// alone</strong> — <strong>which is a picture with a torn corner
    /// rather than an error.</strong>
    /// </para>
    /// </remarks>
    public static byte[]? Read(byte[] pData, string pKey, out bool pPlain)
    {
        pPlain = false;
        if (pData == null || pData.Length == 0)
        {
            return null;
        }

        if (!IsEncrypted(pData))
        {
            pPlain = true;
            return pData;
        }

        if (!TryKeyBytes(pKey, out var schluessel))
        {
            return null;
        }

        // **Und der Header wird abgeschnitten, und nicht
        // mitentschluesselt** -- **denn er steht in der
        // Vergleichsliste des Codes als unveraendert.**
        var body = new byte[pData.Length - EncryptedHeader.Length];
        Array.Copy(pData, EncryptedHeader.Length, body, 0, body.Length);
        for (var index = 0;
            index < ScrambledBytes && index < body.Length;
            index++)
        {
            body[index] ^= schluessel[index];
        }

        return body;
    }

    /// <summary>
    /// The project's key as sixteen bytes, two hex digits at a time.
    /// </summary>
    /// <param name="pKey">The project's <c>encryptionKey</c>.</param>
    /// <param name="pBytes">Sixteen bytes, or null.</param>
    /// <returns>Whether the key was usable.</returns>
    /// <remarks>
    /// <strong>And two hex digits per byte, and not one character.</strong>
    /// The project's own code is
    /// <c>this._encryptionKey.match(/.{2}/g)</c> and then
    /// <c>parseInt(key[i], 16)</c> — <strong>and a reader that took one
    /// character per byte read the string "kir.3520" out of the image
    /// and believed it.</strong>
    /// </remarks>
    public static bool TryKeyBytes(string pKey, out byte[]? pBytes)
    {
        pBytes = null;
        if (string.IsNullOrEmpty(pKey) || pKey.Length < ScrambledBytes * 2)
        {
            return false;
        }

        var bytes = new byte[ScrambledBytes];
        for (var index = 0; index < ScrambledBytes; index++)
        {
            if (!byte.TryParse(
                pKey.Substring(index * 2, 2),
                NumberStyles.HexNumber,
                CultureInfo.InvariantCulture, out bytes[index]))
            {
                return false;
            }
        }

        pBytes = bytes;
        return true;
    }

    /// <summary>
    /// The width and height a PNG says of itself.
    /// </summary>
    /// <param name="pImage">The image bytes.</param>
    /// <param name="pWidth">The width.</param>
    /// <param name="pHeight">The height.</param>
    /// <returns>Whether the bytes begin with a PNG header.</returns>
    /// <remarks>
    /// <strong>And this reads the IHDR rather than guessing.</strong> A
    /// file that begins <c>89504e47</c> is a PNG, <strong>and a reader
    /// that assumed forty-eight pixels and found out later had a picture
    /// of the wrong size and no way to know.</strong>
    /// </remarks>
    public static bool TrySize(
        byte[]? pImage, out int pWidth, out int pHeight)
    {
        pWidth = 0;
        pHeight = 0;
        if (pImage == null || pImage.Length < 24)
        {
            return false;
        }

        if (pImage[0] != 0x89 || pImage[1] != 0x50
            || pImage[2] != 0x4E || pImage[3] != 0x47)
        {
            return false;
        }

        pWidth = (pImage[16] << 24) | (pImage[17] << 16)
            | (pImage[18] << 8) | pImage[19];
        pHeight = (pImage[20] << 24) | (pImage[21] << 16)
            | (pImage[22] << 8) | pImage[23];
        return true;
    }
}
