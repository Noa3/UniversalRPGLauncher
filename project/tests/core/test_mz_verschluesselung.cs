using System;
using System.IO;
using System.Linq;
using UniversalRPG.Mz;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The encryption MZ puts on its assets, read against the game's own
/// 2098 encrypted files.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is nine lines of the engine's own
/// <c>rpg_core.js</c></strong>, -- <strong>and there is no cipher in
/// them</strong>:
///
/// <code>
/// static SIGNATURE = "5250474d56000000";
/// static VER      = "000301";
/// static REMAIN   = "0000000000";
/// arrayBuffer = this.cutArrayHeader(arrayBuffer, Decrypter._headerlength);
/// for (i = 0; i < this._headerlength; i++) {
///     byteArray[i] = byteArray[i] ^ parseInt(Decrypter._encryptionKey[i], 16);
/// }
/// </code>
///
/// <para>
/// <strong>And the key is the MD5 of the empty string</strong>, --
/// <c>d41d8cd98f00b204e9800998ecf8427e</c>, -- <strong>and that is
/// what the editor writes when the author typed no key of
/// their own.</strong>
/// </para>
/// </remarks>
public partial class TestMzVerschluesselung : TestBase
{
    private const string Audio = "D:/Itch/sister/www/audio";

    /// <summary>
    /// And the game's own key, and that it is the MD5 of nothing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this matters for every MZ game</strong>, --
    /// <strong>because a game that typed no key has this one</strong>,
    /// -- <strong>and a reader that asked the user for it would be
    /// asking about nothing.</strong>
    /// </para>
    /// </remarks>
    public void Test_DerSchluesselIstDerVonNichts()
    {
        var system = File.ReadAllText(
            "D:/Itch/sister/www/data/System.json");
        var schluessel = MzVerschluesselung.SchluesselDesSpiels();

        AssertTrue(system.Contains(schluessel),
            "**and the game's `System.json` holds that key** -- and"
                + " it is `" + schluessel + "`");

        AssertEq("d41d8cd98f00b204e9800998ecf8427e", schluessel,
            "**and the key is the MD5 of the empty string** -- and"
                + " that is what the editor writes when the author"
                + " typed none");
        AssertEq(16, MzVerschluesselung.Signatur().Length,
            "**and the header is sixteen bytes** -- and it is"
                + " `RGMV` in hex, the version 000301 and five fixed"
                + " bytes, all of it written by the engine as"
                + " SIGNATURE, VER and REMAIN");
    }

    /// <summary>
    /// And a real file comes back as a real Ogg stream.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the whole point of the commit</strong>,
    /// -- <strong>because without it none of the game's 2098
    /// audio files or images can be read at all</strong>, --
    /// <strong>and a wrong key gives a file that is neither Ogg nor
    /// PNG.</strong>
    /// </para>
    /// </remarks>
    public void Test_EineEchteDateiWirdOgg()
    {
        var datei = Directory.GetFiles(Audio + "/bgm", "*.rpgmvo")
            .OrderBy(x => x, StringComparer.Ordinal).First();

        var roh = File.ReadAllBytes(datei);
        Console.WriteLine(Path.GetFileName(datei) + "  "
            + roh.Length + " Bytes");

        AssertTrue(MzVerschluesselung.IstVerschluesselt(roh),
            "**and the file on disk carries the sixteen header"
                + " bytes**");

        AssertTrue(!MzVerschluesselung.SiehtWieOggAus(roh),
            "**and the file on disk is not an Ogg yet** -- and that"
                + " is what a reader that skipped the decryption"
                + " would hand to an audio device");

        var offen = MzVerschluesselung.Entschluessele(
            roh, MzVerschluesselung.SchluesselDesSpiels());

        AssertTrue(offen != null,
            "**and the decryption returns something**");
        AssertEq(roh.Length - 16, offen!.Length,
            "**and sixteen bytes shorter than the file** -- and"
                + " that is the header and nothing else");
        AssertTrue(MzVerschluesselung.SiehtWieOggAus(offen),
            "**and it starts with `OggS`** -- and a wrong key would"
                + " give neither Ogg nor PNG");
        AssertTrue(!MzVerschluesselung.SiehtWiePngAus(offen),
            "**and it is not a PNG** -- and the two are told apart"
                + " so that a wrong answer is visible");
    }

    /// <summary>
    /// And all four of this game's audio folders are encrypted, and
    /// all of them open.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And 2098 files is the number of assets, not of
    /// folders</strong>, -- <strong>and the game plays 1122 of them:
    /// 61 BGM, 49 BGS and 1012 SE commands.</strong>
    /// </para>
    /// </remarks>
    public void Test_AlleOrdnerOeffnen()
    {
        var zahler = new (string Ordner, int Dateien, int Ogg)[]
        {
            ("bgm", 0, 0),
            ("bgs", 0, 0),
            ("me", 0, 0),
            ("se", 0, 0),
        };

        foreach (var name in new[] { "bgm", "bgs", "me", "se" })
        {
            var pfad = Path.Combine(Audio, name);
            if (!Directory.Exists(pfad))
            {
                continue;
            }

            var index = Array.FindIndex(zahler, z => z.Ordner == name);
            foreach (var datei in Directory.GetFiles(pfad, "*.rpgmvo"))
            {
                zahler[index].Dateien++;
                var offen = MzVerschluesselung.Entschluessele(
                    File.ReadAllBytes(datei),
                    MzVerschluesselung.SchluesselDesSpiels());
                if (offen != null && MzVerschluesselung.SiehtWieOggAus(offen))
                {
                    zahler[index].Ogg++;
                }
            }
        }

        Console.WriteLine(string.Join("  ", zahler.Select(
            z => z.Ordner + " " + z.Dateien + "/" + z.Ogg)));

        AssertEq(106, zahler[0].Dateien,
            "**and the BGM folder holds 106 files**");
        AssertEq(106, zahler[0].Ogg,
            "**and every one of them comes back as an Ogg**");
        AssertEq(46, zahler[1].Dateien,
            "**and 46 BGS**");
        AssertEq(557, zahler[3].Dateien,
            "**and 557 SE, and the game plays one of them 1012"
                + " times**");

        AssertEq(728, zahler.Sum(z => z.Dateien),
            "**and 728 audio files in the four folders the game"
                + " plays** -- and with the 1370 voice files that is"
                + " the 2098 that were encrypted, and I wrote 716"
                + " and had not added 46 and 557");
        AssertEq(zahler.Sum(z => z.Dateien), zahler.Sum(z => z.Ogg),
            "**and not one of them failed** -- and a count that is"
                + " lower would mean the reader guessed");
    }

    /// <summary>
    /// And a file that is not encrypted is left alone.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the engine writes no header at all when
    /// <c>hasEncryptedAudio</c> is false</strong>, --
    /// <strong>and a reader that decrypted such a file would corrupt
    /// it.</strong>
    /// </para>
    /// </remarks>
    public void Test_EineNichtVerschluesselteDateiBleibt()
    {
        var ogg = new byte[] { (byte)'O', (byte)'g', (byte)'g', (byte)'S' };
        AssertTrue(!MzVerschluesselung.IstVerschluesselt(ogg),
            "**and four bytes are not a header**");
        AssertTrue(MzVerschluesselung.Entschluessele(
            ogg, MzVerschluesselung.SchluesselDesSpiels()) == null,
            "**and such a file is returned as nothing** -- and the"
                + " engine does the same, because its loop runs only"
                + " behind a header check");

        var zuKurz = new byte[] { (byte)'R', (byte)'G', (byte)'M', (byte)'V' };
        AssertTrue(!MzVerschluesselung.IstVerschluesselt(zuKurz),
            "**and four bytes of the signature are not sixteen**");

        AssertEq("ogg", MzVerschluesselung.EchteEndung("rpgmvo"),
            "**and `.rpgmvo` is an Ogg** -- and that is the engine's"
                + " own mapping");
        AssertEq("png", MzVerschluesselung.EchteEndung("rpgmvp"),
            "**and `.rpgmvp` is a PNG**");
        AssertEq("wav", MzVerschluesselung.EchteEndung("wav"),
            "**and everything else keeps its name**");
    }
}
