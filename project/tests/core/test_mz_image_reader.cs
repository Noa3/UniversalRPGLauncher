using System;
using System.IO;
using UniversalRPG.Tests.Framework;
using UniversalRPG.Web;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Reading an image out of a project whose files RPG Maker encrypted.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the algorithm is not from a library and not from a
/// forum.</strong> It is in the project's own
/// <c>js/rmmz_core.js</c>, in <c>Utils.decryptArrayBuffer</c>, and this
/// file quotes it because quoting a constant is not the same as
/// guessing it.
/// </para>
/// <para>
/// <strong>And the measured project really does encrypt its images.</strong>
/// <c>data/System.json</c> says
/// <c>"hasEncryptedImages": true</c>, and
/// <c>"encryptionKey": "995ce5201bae90d34dbac5a6311e0d5c"</c>, and every
/// file in <c>img/</c> is named <c>.png_</c> rather than <c>.png</c>.
/// </para>
/// </remarks>
public partial class TestMzImageReader : TestBase
{
    private const string Projekt = "E:/RPGMakerGames/CamelliaCoronation-Win";

    private static bool Vorhanden()
    {
        return File.Exists(Projekt + "/img/tilesets/Dungeon_A1.png_")
            && File.Exists(Projekt + "/data/System.json");
    }

    private static string Schluessel()
    {
        var text = File.ReadAllText(Projekt + "/data/System.json");
        var anfang = text.IndexOf("\"encryptionKey\"", StringComparison.Ordinal);
        if (anfang < 0)
        {
            return "";
        }

        var offen = text.IndexOf('"', anfang + 16);
        var zu = text.IndexOf('"', offen + 1);
        return zu > offen ? text.Substring(offen + 1, zu - offen - 1) : "";
    }

    /// <summary>
    /// The project's own image comes back as a PNG of a real size.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the assertion is the size, and not "it did not
    /// throw".</strong> Sixteen bytes of correct prefix and a file that
    /// says <c>13x1229472850</c> is a file that was scrambled and stayed
    /// scrambled, <strong>and both look like success to a reader that
    /// only checks for exceptions.</strong>
    /// </para>
    /// <para>
    /// <strong>And a tileset is a whole sheet of tiles, so its width is
    /// a multiple of forty-eight</strong> — <strong>the engine's own
    /// tile size</strong> — <strong>and a size that is not, is a size
    /// nobody drew.</strong>
    /// </para>
    /// </remarks>
    public void Test_DasVerschluesselteBildKommtAlsPngZurueck()
    {
        if (!Vorhanden())
        {
            return;
        }

        var roh = File.ReadAllBytes(
            Projekt + "/img/tilesets/Dungeon_A1.png_");

        AssertTrue(MzImageReader.IsEncrypted(roh),
            "**and the project encrypted its images** -- and the header is "
                + "the literal in the project's own rmmz_core.js, and not "
                + "a pattern guessed from one file");

        var bild = MzImageReader.Read(roh, Schluessel(), out var unverschluesselt);
        AssertTrue(!unverschluesselt,
            "**and the reader says it had to work** -- and a reader that "
                + "always reports \"plain\" would hide every failure this "
                + "test is about");
        AssertTrue(bild != null, "**and it worked**");

        AssertTrue(MzImageReader.TrySize(bild!, out var breite, out var hoehe),
            "**and the result is a PNG** -- and a reader that scrambled it "
                + "instead of unscrambling it would also come out of a "
                + "try that checks nothing");

        AssertTrue(breite % 48 == 0 && hoehe % 48 == 0,
            "**and its size is a whole number of tiles** -- and the "
                + "engine's tile is 48 pixels, and it measured "
                + $"{breite}x{hoehe}, and a size that is not a whole "
                + "number of tiles is a size nobody drew");
        AssertTrue(breite > 0 && hoehe > 0,
            "**and it is bigger than nothing**");

        // **Und der Decoder dieses Projekts kann es auch lesen** --
        // **das ist der Schritt von "entschluesselt" zu "benutzbar".**
        var dekodiert = UniversalRPG.Rm2k.Rendering.Rm2kIndexedImage.TryParse(
            bild!, out var image, out var fehler);
        AssertTrue(dekodiert,
            "**and the existing PNG reader can read it** -- and the "
                + "encryption was one step and the decoding another, and "
                + "a reader that proved the first and not the second had "
                + "a picture nobody could look at; the refusal is: "
                + fehler);
        AssertEq(image.Width, breite,
            "**and it says the same width the header did**");
        AssertEq(image.Height, hoehe,
            "**and the same height**");
    }

    /// <summary>
    /// The sixteen bytes after the header are the scrambled ones.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the detail a first reader gets wrong, and
    /// it is the whole difference between a picture and noise.</strong>
    /// The project's code is
    /// <c>const body = source.slice(16)</c> and then the loop — <strong>so
    /// the header is cut off and the sixteen bytes behind it are
    /// unscrambled.</strong>
    /// </para>
    /// <para>
    /// <strong>A reader that unscrambled the first sixteen bytes of the
    /// file unscrambled the header</strong> — <strong>and got a file
    /// beginning <c>cb0ca26d</c> where a PNG begins
    /// <c>89504e47</c></strong>, <strong>and the rest of the file was
    /// left as it was</strong>, <strong>so it was a PNG header followed
    /// by a picture's middle.</strong>
    /// </para>
    /// </remarks>
    public void Test_DerHeaderWirdAbgeschnittenUndNichtEntschluesselt()
    {
        if (!Vorhanden())
        {
            return;
        }

        var roh = File.ReadAllBytes(
            Projekt + "/img/tilesets/Dungeon_A1.png_");
        var bild = MzImageReader.Read(roh, Schluessel(), out var _);
        AssertTrue(bild != null, "**and it worked**");

        AssertEq(bild!.Length, roh.Length - MzImageReader.EncryptedHeader.Length,
            "**and what comes back is the file without its header** -- and "
                + "the header is cut off and not unscrambled, because the "
                + "project's own code compares it unchanged and a reader "
                + "that unscrambled it got a file beginning cb0ca26d where "
                + "a PNG begins 89504e47");

        AssertEq((int)bild[0], 0x89,
            "**and the first byte is a PNG's first byte**");
        AssertEq((int)bild[1], 0x50,
            "**and its second**");
    }

    /// <summary>
    /// The key is two hex digits to a byte, and not one character.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the project's code is
    /// <c>this._encryptionKey.match(/.{2}/g)</c> and then
    /// <c>parseInt(key[i], 16)</c>.</strong> <strong>A reader that took
    /// sixteen characters as ASCII produced the bytes of the text
    /// "kir.3520"</strong> — <strong>and that result is printable, and
    /// looks like an answer, and is not one.</strong>
    /// </para>
    /// <para>
    /// <strong>And a key that is too short is refused, and not
    /// half-used.</strong> A project without image encryption writes an
    /// empty key; <strong>half a key would tear the corner off the
    /// picture and leave the rest of it correct</strong> — <strong>and a
    /// torn corner is not an error a reader can see.</strong>
    /// </para>
    /// </remarks>
    public void Test_DerSchluesselIstZweiHexZiffernProByte()
    {
        if (!Vorhanden())
        {
            return;
        }

        var text = Schluessel();
        AssertTrue(MzImageReader.TryKeyBytes(text, out var schluessel),
            "**and the project's key is usable**");
        AssertEq(schluessel!.Length, 16, "**and it is sixteen bytes**");
        AssertEq((int)schluessel[0], 0x99,
            "**and the first byte is 0x99** -- and the key starts with the "
                + "text \"99\", and a reader that took one character per "
                + "byte read the 0x39 of the character '9' and got a "
                + "picture that begins with the text \"kir.3520\"");
        AssertEq((int)schluessel[1], 0x5C,
            "**and the second is 0x5c** -- and the key continues with "
                + "\"5c\", which is one byte and not one character");

        AssertTrue(!MzImageReader.TryKeyBytes("", out var leer),
            "**and an empty key is refused** -- and a project without "
                + "image encryption writes one, and half a key would tear "
                + "the corner off the picture instead of saying so");
        AssertTrue(leer == null, "**and nothing comes back**");
    }

    /// <summary>
    /// A plain file is left alone.
    /// </summary>
    /// <remarks>
    /// <strong>And the engine asks before it decrypts.</strong> The
    /// project's own scene boot reads
    /// <c>$dataSystem.hasEncryptedImages</c> and passes it to
    /// <c>Utils.setEncryptionInfo</c> — <strong>and a reader that
    /// unscrambled unconditionally turned every plain image in a mixed
    /// project into noise.</strong>
    /// </remarks>
    public void Test_EineUnverschluesselteDateiBleibtWieSieIst()
    {
        var bild = new byte[] { 0x89, 0x50, 0x4E, 0x47, 1, 2, 3, 4 };

        AssertTrue(!MzImageReader.IsEncrypted(bild),
            "**and a plain PNG is not seen as encrypted**");
        var gelesen = MzImageReader.Read(bild, Schluessel(), out var unverschluesselt);
        AssertTrue(unverschluesselt,
            "**and the reader says so** -- and a reader that always "
                + "unscrambled turned every plain image in a mixed project "
                + "into noise");
        AssertEq(gelesen!.Length, bild.Length,
            "**and it comes back the same length**");
        AssertEq((int)gelesen[0], 0x89,
            "**and the same first byte**");
    }
}
