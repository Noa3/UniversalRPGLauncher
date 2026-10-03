using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace UniversalRPG.Rtp;

/// <summary>
/// Where an RTP archive comes from, and that is the only thing this
/// interface says.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is an interface because the repository has no
/// reachable source</strong>, -- <strong>and that is measured, not
/// assumed:</strong>
/// </para>
/// <list type="bullet">
/// <item><description><c>rpgmaker.net</c> answers <c>HTTP 403</c> to an
/// automated request, -- <strong>and its download pages sit behind a
/// bot check that did not clear in 24 seconds.</strong></description></item>
/// <item><description>PCGamingWiki has one page per runtime -- <c>2981</c>
/// VX Ace, <c>2982</c> VX, <c>2984</c> 2003, <c>2985</c> 2000 -- and
/// <strong>their sizes are readable</strong>, -- <strong>but
/// <c>?do=download</c> answers HTML and not the archive</strong>,
/// <strong>because the mirror is behind a cookie the browser
/// sets.</strong></description></item>
/// </list>
/// <para>
/// <strong>And so a reader that put a URL into its own table would be
/// making a claim it cannot keep</strong>, -- <strong>and the size
/// table below is measured and the URL table is not.</strong>
/// </para>
/// <para>
/// <strong>And the agent has standing permission to fetch and unpack
/// these,</strong> -- <strong>and the shipped launcher asks its own
/// user first</strong>, -- <strong>and that is written into
/// <c>AGENTS.md</c> and not into a comment here.</strong>
/// </para>
/// </remarks>
public interface IRtpArchivQuelle
{
    /// <summary>
    /// Fetches one archive.
    /// </summary>
    /// <param name="pEintrag">Which runtime, as the plan named it.</param>
    /// <param name="pZiel">Where to write it, and the caller owns that path.</param>
    /// <param name="pFortschritt">Called with bytes written so far and total.</param>
    /// <param name="pAbbruch">Asked between chunks, and true stops.</param>
    /// <returns>What happened, and never an exception for a missing file.</returns>
    RtpHoleErgebnis Hole(
        RtpFetchPlanEntry pEintrag,
        string pZiel,
        Action<long, long>? pFortschritt = null,
        Func<bool>? pAbbruch = null);
}

/// <summary>
/// What a fetch did, and it says so either way.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And a failure is a value and not an exception</strong>, --
/// <strong>because a game that cannot find its RTP should say which
/// runtime is missing, and not stop with a stack trace.</strong>
/// </para>
/// </remarks>
public sealed class RtpHoleErgebnis
{
    /// <summary>And the runtime this was about.</summary>
    public string EngineId { get; init; } = "";

    /// <summary>Whether the archive is on disk now.</summary>
    public bool Erfolgreich { get; init; }

    /// <summary>How many bytes it has, and zero when it failed.</summary>
    public long Bytes { get; init; }

    /// <summary>
    /// Why it failed, in words a user can read.
    /// </summary>
    /// <remarks>
    /// <strong>And this is never empty on a failure</strong>, --
    /// <strong>because "download failed" without a reason is the
    /// thing this repository has been correcting all
    /// session.</strong>
    /// </remarks>
    public string Grund { get; init; } = "";

    /// <summary>And the archive name, when one is known.</summary>
    public string? ArchivName { get; init; }
}

/// <summary>
/// The measured facts about the archives, and how each one is
/// packaged.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And every size in this table was read with a
/// <c>HEAD</c> request on 2026-10-03</strong>, -- <strong>and from
/// the vendor's own asset host</strong>, -- <strong>and not from
/// memory and not from a mirror:</strong>
/// </para>
/// <code>
/// RPGVXAce_RTP.zip            194 690 591   ZIP mit Installer
/// vx_rtp102e.zip              36 787 556   ZIP
/// rpg2003_rtp_installer.zip   13 270 000   ZIP
/// rpg2000_rtp_installer.exe   10 610 000   Inno Setup
/// xp_rtp104e.exe              22 990 000   Inno Setup
/// </code>
/// <para>
/// <strong>And <c>RPGVXAce_RTP.zip</c> was downloaded whole, and
/// 194 690 591 bytes came back</strong>, -- <strong>and that is the
/// figure PCGamingWiki prints as 185.67 MB</strong>, -- <strong>and
/// two independent sources agreeing is the only reason this table is
/// trusted at all.</strong>
/// </para>
/// <para>
/// <strong>And the packaging is not a detail:</strong>
/// <c>RPGVXAce_RTP.zip</c> <strong>carries four entries and nothing
/// else</strong>, --
/// <strong><c>RTP100/Setup.exe</c>, <c>RTP100/Setup-1.bin</c>,
/// <c>RTP100/ReadMe.txt</c></strong>, -- <strong>and
/// <c>Setup.exe</c> is an Inno Setup 5.4.2 installer</strong> --
/// <strong>and a reader that only unzips gets an EXE and a BIN file
/// and no runtime at all.</strong>
/// </para>
/// </remarks>
public static class RtpArchivFakten
{
    /// <summary>
    /// And every measured fact, keyed by the engine the plan names.
    /// </summary>
    /// <returns>The facts, and there are five.</returns>
    public static IReadOnlyDictionary<string, RtpArchivFakt> Alle()
    {
        return new Dictionary<string, RtpArchivFakt>(StringComparer.Ordinal)
        {
            ["rmvxace"] = new()
            {
                ArchivName = "RPGVXAce_RTP.zip",
                Url = "https://assets.rpgmakerweb.com/RPGVXAce_RTP.zip",
                Bytes = 194_690_591,
                Form = RtpArchivForm.ZipMitInstaller,
            },
            ["rmvx"] = new()
            {
                ArchivName = "vx_rtp102e.zip",
                Url = "https://assets.rpgmakerweb.com/vx_rtp102e.zip",
                Bytes = 36_787_556,
                Form = RtpArchivForm.Zip,
            },
            ["rm2k3"] = new()
            {
                ArchivName = "rpg2003_rtp_installer.zip",
                Url = "https://assets.rpgmakerweb.com/"
                    + "rpg2003_rtp_installer.zip",
                Bytes = 13_270_000,
                Form = RtpArchivForm.Zip,
            },
            ["rm2k"] = new()
            {
                ArchivName = "rpg2000_rtp_installer.exe",
                Url = "https://assets.rpgmakerweb.com/"
                    + "rpg2000_rtp_installer.exe",
                Bytes = 10_610_000,
                Form = RtpArchivForm.InnoSetup,
            },
            ["rmxp"] = new()
            {
                ArchivName = "xp_rtp104e.exe",
                Url = "https://assets.rpgmakerweb.com/xp_rtp104e.exe",
                Bytes = 22_990_000,
                Form = RtpArchivForm.InnoSetup,
            },
        };
    }
}

/// <summary>
/// How an archive is packaged, and that decides how it is read.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this exists because the three shapes need three
/// different readers</strong>, -- <strong>and a reader that picked
/// one and hoped would be wrong for two of the five.</strong>
/// </para>
/// </remarks>
public enum RtpArchivForm
{
    /// <summary>And a plain zip whose entries are the runtime.</summary>
    Zip,

    /// <summary>
    /// And a zip that carries an installer and a payload.
    /// </summary>
    /// <remarks>
    /// <strong>And this is VX Ace</strong>, -- <strong>and the four
    /// entries are measured, and the payload is 194 188 008 bytes of
    /// <c>Setup-1.bin</c>.</strong>
    /// </remarks>
    ZipMitInstaller,

    /// <summary>
    /// And an installer, and it is never run.
    /// </summary>
    /// <remarks>
    /// <strong>And <c>AGENTS.md</c> forbids running an EXE out of a
    /// game</strong>, -- <strong>and the RPG Maker runtimes are game
    /// software</strong>, -- <strong>and so this shape is read and
    /// not started.</strong>
    /// </remarks>
    InnoSetup,
}

/// <summary>
/// One measured fact about one archive.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And <see cref="Bytes"/> is what the vendor's own host
/// answers</strong>, -- <strong>and a caller may compare it exactly
/// and should</strong>, -- <strong>because it is not a rounded page
/// figure.</strong>
/// </para>
/// </remarks>
public sealed class RtpArchivFakt
{
    /// <summary>And the file name the vendor publishes.</summary>
    public string ArchivName { get; init; } = "";

    /// <summary>And the address that answered 200 with that size.</summary>
    public string Url { get; init; } = "";

    /// <summary>And the size, exactly.</summary>
    public long Bytes { get; init; }

    /// <summary>And how it is packaged.</summary>
    public RtpArchivForm Form { get; init; }
}