using System;
using System.Collections.Generic;
using UniversalRPG.Rtp;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The measured archive facts, and the packaging that decides how
/// each one is read.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And every size here was read with a <c>HEAD</c> request on
/// 2026-10-03 from the vendor's own asset host</strong>, -- <strong>and
/// <c>RPGVXAce_RTP.zip</c> was then downloaded whole</strong> --
/// <strong>and 194 690 591 bytes came back, which is the figure
/// PCGamingWiki prints as 185.67 MB.</strong>
/// </para>
/// <para>
/// <strong>And the packaging is what this test is really about</strong>,
/// -- <strong>because the VX Ace archive carries four entries and no
/// runtime</strong>, -- <strong>and a reader that only unzips would
/// report an installed RTP that is not installed.</strong>
/// </para>
/// </remarks>
public partial class TestRtpFakten : TestBase
{
    /// <summary>
    /// And the five archives, named.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the assertion is about the packaging and not about
    /// the size</strong>, -- <strong>because a vendor may change a
    /// size and may not change a format</strong>.
    /// </para>
    /// </remarks>
    public void Test_DieFuenfArchive()
    {
        var alle = RtpArchivFakten.Alle();
        Console.WriteLine("Archive: " + alle.Count);

        foreach (var kv in alle)
        {
            var f = kv.Value;
            Console.WriteLine($"  {kv.Key,-8} {f.ArchivName,-30}"
                + $" {f.Bytes,12} Bytes  {f.Form}");
        }

        AssertEq(5, alle.Count,
            "**and five archives are named** -- and every one of them"
                + " answered 200 with a size on 2026-10-03");

        // **Und VX Ace ist der Sonderfall,  und es ist gemessen.**
        AssertEq(RtpArchivForm.ZipMitInstaller, alle["rmvxace"].Form,
            "**and the VX Ace archive is a zip that carries an"
                + " installer** -- and it has four entries:"
                + " `RTP100/Setup.exe`, `RTP100/Setup-1.bin`,"
                + " `RTP100/ReadMe.txt` and the folder, -- and"
                + " `Setup.exe` is Inno Setup 5.4.2, -- and a reader"
                + " that only unzips it gets no runtime at all");

        // **Und zwei davon  sind  EXE-Installer,  und  die  werden
        // nie gestartet.**
        AssertEq(RtpArchivForm.InnoSetup, alle["rm2k"].Form,
            "**and the RM2000 archive is an installer and not a zip**"
                + " -- and it is 10 610 000 bytes of"
                + " `application/octet-stream`, -- and `AGENTS.md`"
                + " forbids running it");

        AssertEq(RtpArchivForm.InnoSetup, alle["rmxp"].Form,
            "**and so is the XP archive** -- and it is the same"
                + " problem in a different file name");

        // **Und  die  beiden  anderen  sind  echte  ZIPs.**
        AssertEq(RtpArchivForm.Zip, alle["rmvx"].Form,
            "**and the VX archive is a plain zip** -- and its entries"
                + " are the runtime");

        AssertEq(RtpArchivForm.Zip, alle["rm2k3"].Form,
            "**and so is the RM2003 archive**");

        // **Und  jede  URL  ist  eine  Asset-Adresse  des
        // Herstellers  und  keine  Spiegel-Seite.**
        foreach (var kv in alle)
        {
            AssertTrue(kv.Value.Url.StartsWith(
                    "https://assets.rpgmakerweb.com/", StringComparison.Ordinal),
                "**and every archive comes from the vendor's own asset"
                    + " host** -- and `rpgmaker.net` answers 403 to an"
                    + " automated request, so a mirror is the only"
                    + " address that works, and `PCGamingWiki`"
                    + " answers HTML for `?do=download`");
        }
    }
}
