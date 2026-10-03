using System;
using System.IO;
using System.IO.Compression;
using UniversalRPG.Rtp;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Unpacking an RTP archive, and refusing what must be refused.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And these are archives this repository builds itself</strong>,
/// -- <strong>because the four real ones cannot be fetched from here</strong>,
/// -- <strong>and the refusal tests need a hostile archive and no vendor
/// ships one on purpose.</strong>
/// </para>
/// <para>
/// <strong>And a zip with a <c>..</c> entry is built here and offered
/// to the unpacker</strong>, -- <strong>and that is the only way to
/// show that it refuses rather than that it is careful.</strong>
/// </para>
/// </remarks>
public partial class TestRtpEntpacken : TestBase
{
    private static string Ziel(string pName) => Path.Combine(
        System.IO.Path.GetTempPath(), "urpg_rtp_" + pName);

    /// <summary>
    /// And a well formed archive unpacks.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the archive carries what a real RTP carries</strong>,
    /// -- <strong><c>RTP100</c> and a DLL and a sound</strong> --
    /// <strong>and none of them is run.</strong>
    /// </para>
    /// </remarks>
    public void Test_EinGutesArchivEntpackt()
    {
        var ziel = Ziel("gut");
        if (Directory.Exists(ziel))
        {
            Directory.Delete(ziel, true);
        }

        var archiv = Path.Combine(ziel, "RTP.zip");
        Directory.CreateDirectory(ziel);
        using (var a = ZipFile.Open(archiv, ZipArchiveMode.Create))
        {
            Schreibe(a, "RTP100/Setup.exe", "MZ");
            Schreibe(a, "RTP100/System/RGSS301.dll", "MZ");
            Schreibe(a, "RTP100/Audio/SE/Decision.ogg", "OggS");
        }

        var e = RtpEntpacker.Entpacke(archiv, Path.Combine(ziel, "heraus"));
        Console.WriteLine("Dateien: " + e.Dateien + " Bytes: " + e.Bytes
            + " verweigert: " + e.Verweigert.Count);

        AssertEq(3, e.Dateien,
            "**and three files came out** -- and an RTP carries a"
                + " setup, a runtime and a sound, and none of them"
                + " was run");
        AssertEq(0, e.Verweigert.Count,
            "**and nothing was refused** -- and a good archive has"
                + " nothing to refuse");
        AssertTrue(File.Exists(Path.Combine(ziel, "heraus", "RTP100",
                "System", "RGSS301.dll")),
            "**and the runtime is where the archive said** -- and the"
                + " path came out of the entry name and not out of a"
                + " guess");
    }

    /// <summary>
    /// And an entry that climbs out is refused, and named.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the escape target is checked by reading it
    /// back</strong>, -- <strong>because "the unpacker refused" and
    /// "nothing landed outside" are different claims and only the
    /// second one is what matters.</strong>
    /// </para>
    /// </remarks>
    public void Test_EinAusbruchWirdVerweigert()
    {
        var basis = Ziel("ausbruch");
        if (Directory.Exists(basis))
        {
            Directory.Delete(basis, true);
        }

        var opfer = Path.Combine(basis, "draussen.txt");
        var archiv = Path.Combine(basis, "RTP.zip");
        Directory.CreateDirectory(basis);
        using (var a = ZipFile.Open(archiv, ZipArchiveMode.Create))
        {
            Schreibe(a, "harmlos.txt", "in Ordnung");
            Schreibe(a, "../draussen.txt", "das haette nicht passieren");
        }

        var e = RtpEntpacker.Entpacke(archiv, Path.Combine(basis, "ziel"));
        Console.WriteLine("verweigert: " + string.Join(" | ", e.Verweigert));

        AssertTrue(!File.Exists(opfer),
            "**and nothing landed outside the directory** -- and this"
                + " is the claim that matters, and it was checked by"
                + " reading the path back and not by trusting the"
                + " refusal count");
        AssertEq(1, e.Verweigert.Count,
            "**and the refused entry is counted** -- and an RTP"
                + " archive with one such entry is refused entry by"
                + " entry and not wholesale");
        AssertTrue(File.Exists(Path.Combine(basis, "ziel", "harmlos.txt")),
            "**and the harmless entry still came out** -- and one bad"
                + " entry does not cost the caller the rest");
    }

    /// <summary>
    /// And an absolute path is refused too.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is a second way out and not the same
    /// one</strong>, -- <strong>and a guard that only counted
    /// <c>..</c> would pass this archive through.</strong>
    /// </para>
    /// </remarks>
    public void Test_EinAbsoluterPfadWirdVerweigert()
    {
        var basis = Ziel("absolut");
        if (Directory.Exists(basis))
        {
            Directory.Delete(basis, true);
        }

        var archiv = Path.Combine(basis, "RTP.zip");
        Directory.CreateDirectory(basis);
        using (var a = ZipFile.Open(archiv, ZipArchiveMode.Create))
        {
            Schreibe(a, "gut.txt", "drinnen");
            Schreibe(a, "C:/urpg_absolut_opfer.txt", "aussen");
        }

        var e = RtpEntpacker.Entpacke(archiv, Path.Combine(basis, "ziel"));
        Console.WriteLine("verweigert: " + string.Join(" | ", e.Verweigert));

        AssertTrue(!File.Exists("C:/urpg_absolut_opfer.txt"),
            "**and nothing landed at an absolute path** -- and the"
                + " second escape is closed as well");
        AssertEq(1, e.Verweigert.Count,
            "**and the absolute entry is refused and named**");
    }

    /// <summary>
    /// And a page that handed back HTML is not an archive.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is measured:</strong> all four mirror pages
    /// answered <c>text/html</c> for <c>?do=download</c>. -- <strong>And
    /// a reader that trusted the extension would try to unpack
    /// 185 MB of HTML.</strong>
    /// </para>
    /// </remarks>
    public void Test_KeinZipIstKeinArchiv()
    {
        var basis = Ziel("html");
        if (Directory.Exists(basis))
        {
            Directory.Delete(basis, true);
        }

        Directory.CreateDirectory(basis);
        var datei = Path.Combine(basis, "RTPVXAce_RTP.zip");
        File.WriteAllText(datei, "<!DOCTYPE html><html>bin nicht da"
            + "</html>");

        AssertTrue(!RtpEntpacker.IstZip(datei),
            "**and a file that starts with `<html>` is not an"
                + " archive** -- and it is named `.zip`, and that is"
                + " exactly what the mirror hands back");
    }

    private static void Schreibe(ZipArchive pArchiv, string pName,
        string pInhalt)
    {
        var e = pArchiv.CreateEntry(pName);
        using var s = e.Open();
        using var w = new StreamWriter(s);
        w.Write(pInhalt);
    }
}
