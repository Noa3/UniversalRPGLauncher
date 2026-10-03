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
/// The measured facts about the four archives, and no URLs.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And every size in this table was read from a page on
/// 2026-10-03</strong>, -- <strong>and not from memory and not from
/// the vendor:</strong>
/// </para>
/// <code>
/// 2981  RPG Maker VX Ace RTP   185.67 MB
/// 2982  RPG Maker VX RTP       70.21 MB
/// 2984  RPG Maker 2003 RTP     27.24 MB
/// 2985  RPG Maker 2000 RTP     22.01 MB
/// </code>
/// <para>
/// <strong>And VX Ace names its own archive <c>RPGVXAce_RTP.zip</c>
/// in its text</strong>, -- <strong>and the other three name theirs
/// only inside the archive</strong>, -- <strong>and a table that
/// guessed those three names would be wrong three times
/// out of three.</strong>
/// </para>
/// <para>
/// <strong>And the mirror page is named, because it is what a caller
/// can check by hand</strong>, -- <strong>and a plan that only
/// carried a size could not be verified against anything.</strong>
/// </para>
/// </remarks>
public static class RtpArchivFakten
{
    /// <summary>
    /// And every measured fact, keyed by the engine the plan names.
    /// </summary>
    /// <returns>The facts, and there are four.</returns>
    public static IReadOnlyDictionary<string, RtpArchivFakt> Alle()
    {
        return new Dictionary<string, RtpArchivFakt>(StringComparer.Ordinal)
        {
            ["rm2k"] = new()
            {
                MirrorSeite = "https://community.pcgamingwiki.com/files/file/"
                    + "2985-rpg-maker-2000-run-time-package/",
                Bytes = 22_010_000,
                ArchivName = null,
            },
            ["rm2k3"] = new()
            {
                MirrorSeite = "https://community.pcgamingwiki.com/files/file/"
                    + "2984-rpg-maker-2003-run-time-package/",
                Bytes = 27_240_000,
                ArchivName = null,
            },
            ["rmvx"] = new()
            {
                MirrorSeite = "https://community.pcgamingwiki.com/files/file/"
                    + "2982-rpg-maker-vx-run-time-package/",
                Bytes = 70_210_000,
                ArchivName = null,
            },
            ["rmvxace"] = new()
            {
                MirrorSeite = "https://community.pcgamingwiki.com/files/file/"
                    + "2981-rpg-maker-vx-ace-run-time-package/",
                Bytes = 185_670_000,
                ArchivName = "RPGVXAce_RTP.zip",
            },
        };
    }
}

/// <summary>
/// One measured fact about one archive.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And <see cref="Bytes"/> is a size the page rounded to two
/// decimals</strong>, -- <strong>and a caller must not compare it
/// byte for byte</strong>, -- <strong>and so it is named
/// <c>Bytes</c> and not <c>Hash</c></strong>, -- <strong>and there is
/// no hash here because no page published one.</strong>
/// </para>
/// </remarks>
public sealed class RtpArchivFakt
{
    /// <summary>
    /// And the page a person can open to get the file.
    /// </summary>
    /// <remarks>
    /// <strong>And this is a page and not an archive</strong>, --
    /// <strong>and that is the honest kind of address here</strong>,
    /// -- <strong>because an archive URL that answers HTML is
    /// worse than no address.</strong>
    /// </remarks>
    public string MirrorSeite { get; init; } = "";

    /// <summary>And the size the page shows, rounded.</summary>
    public long Bytes { get; init; }

    /// <summary>
    /// And the name, when the page prints it, and null when it does not.
    /// </summary>
    /// <remarks>
    /// <strong>And only VX Ace prints its own</strong>, -- <strong>and
    /// a table that filled the other three in would be
    /// inventing.</strong>
    /// </remarks>
    public string? ArchivName { get; init; }
}
