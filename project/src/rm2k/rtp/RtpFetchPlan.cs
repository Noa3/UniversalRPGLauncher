using System;
using System.Collections.Generic;
using System.IO;

namespace UniversalRPG.Rtp;

/// <summary>
/// What this repository would have to fetch, and nothing more.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this file is a plan and not an action</strong>, and that
/// distinction is the whole of it. <c>AGENTS.md</c> says imported games
/// are untrusted input, and a user said the RTP may be downloaded
/// <strong>after asking first</strong>.
/// </para>
/// <para>
/// <strong>And so this type cannot download, cannot extract, and cannot
/// write outside a directory the caller names</strong> -- <strong>and
/// it has no network code at all</strong>, -- <strong>and the reason is
/// written into it rather than left to the caller to remember.</strong>
/// </para>
/// <para>
/// <strong>And a plan that names a URL is a claim about where a file
/// lives, and that claim has to be checkable</strong> -- <strong>so
/// every entry carries the version, the size it expects and the engine
/// it belongs to</strong>, -- <strong>and a caller that finds one of
/// them wrong changes the table here and not somewhere else.</strong>
/// </para>
/// </remarks>
public sealed class RtpFetchPlanEntry
{
    /// <summary>Which engine this belongs to, and not a label.</summary>
    public string EngineId { get; init; } = "";

    /// <summary>Which generation, because XP and VX Ace are not it.</summary>
    public string Generation { get; init; } = "";

    /// <summary>What the installer calls the runtime.</summary>
    public string DependencyName { get; init; } = "";

    /// <summary>What the game asks for when it cannot find a file.</summary>
    public string DependencyFileName { get; init; } = "";

    /// <summary>Where the installer would put it.</summary>
    public string InstallDirectoryName { get; init; } = "";

    /// <summary>How many bytes the archive has, when that is known.</summary>
    public long ExpectedBytes { get; init; }

    /// <summary>
    /// Whether this repository knows where to get it, and that is a
    /// different question from whether it is allowed to.
    /// </summary>
    public bool HasKnownSource { get; init; }
}

/// <summary>
/// The whole set of runtimes one project names, and nothing fetched.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And a plan is built from the project's own request and never
/// from a guess</strong>, -- <strong>and building one reads no file
/// outside the project</strong> -- <strong>and building one asks
/// nothing.</strong>
/// </para>
/// <para>
/// <strong>And the difference between "the plan names it" and "the plan
/// has a source" is the difference between a runtime this repository
/// knows where to get and one that a user has to point at</strong>, --
/// <strong>and the second kind is not an error here, it is an answer.</strong>
/// </para>
/// </remarks>
public sealed class RtpFetchPlan
{
    /// <summary>Every runtime the project named, deduplicated.</summary>
    public IReadOnlyList<RtpFetchPlanEntry> Entries { get; init; } =
        Array.Empty<RtpFetchPlanEntry>();

    /// <summary>Where the caller said the runtimes would go.</summary>
    public string TargetDirectory { get; init; } = "";

    /// <summary>
    /// Whether every entry has a source this repository knows, and that
    /// is the question a caller asks before promising anything.
    /// </summary>
    public bool FullySourced
    {
        get
        {
            foreach (var eintrag in Entries)
            {
                if (!eintrag.HasKnownSource)
                {
                    return false;
                }
            }

            return Entries.Count > 0;
        }
    }

    /// <summary>
    /// Writes the plan as a human reads it, because a plan nobody can
    /// read is not consent.
    /// </summary>
    /// <returns>One line per entry, and no archive has been touched.</returns>
    public IReadOnlyList<string> Describe()
    {
        var zeilen = new List<string>
        {
            "Zielverzeichnis: " + TargetDirectory,
            "Benoetigt: " + Entries.Count
                + " Laufzeitumgebung(en), und keine davon ist geladen.",
        };
        foreach (var eintrag in Entries)
        {
            zeilen.Add(
                "  - " + eintrag.EngineId + " / " + eintrag.Generation
                + ": " + eintrag.DependencyName
                + " (gefragt als " + eintrag.DependencyFileName + ")"
                + (eintrag.HasKnownSource
                    ? ", und dieses Repository weiss, woher"
                    : ", und fuer diese weiss dieses Repository keine Quelle"));
        }

        return zeilen;
    }
}

/// <summary>
/// Builds a plan from what a project actually asks for.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this reads the project's own data and nothing
/// else</strong>, -- <strong>and it never opens an archive, never
/// writes and never asks</strong>, -- <strong>because a plan that acts
/// is not a plan.</strong>
/// </para>
/// </remarks>
public static class RtpFetchPlanner
{
    /// <summary>
    /// Names every runtime the project's own files ask for.
    /// </summary>
    /// <param name="pProjectDirectory">The game, and nothing above it.</param>
    /// <param name="pTargetDirectory">Where the caller would put them.</param>
    /// <returns>A plan, and it is always a plan.</returns>
    public static RtpFetchPlan Build(
        string pProjectDirectory, string pTargetDirectory)
    {
        var gefunden = new List<RtpFetchPlanEntry>();
        var gesehen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var datei in System.IO.Directory
            .EnumerateFiles(pProjectDirectory, "*", SearchOption.AllDirectories))
        {
            if (!DateiNenntRtp(datei))
            {
                continue;
            }

            foreach (var nennung in RtpNennungen.Nennungen(datei))
            {
                if (!gesehen.Add(nennung))
                {
                    continue;
                }

                gefunden.Add(RtpNennungen.ProfilFuer(nennung));
            }
        }

        return new RtpFetchPlan
        {
            Entries = gefunden,
            TargetDirectory = pTargetDirectory,
        };
    }

    /// <summary>
    /// Whether this file is one that names a runtime at all.
    /// </summary>
    /// <param name="pPath">The file.</param>
    /// <returns>Yes or no, and the reason is the extension.</returns>
    /// <remarks>
    /// <strong>And a scanned file is text this repository has to read
    /// anyway</strong> -- <strong>and an image is not</strong>, --
    /// <strong>and one that is neither is skipped rather than
    /// guessed at</strong>.
    /// </remarks>
    public static bool DateiNenntRtp(string pPath)
    {
        var endung = System.IO.Path.GetExtension(pPath)
            .ToLowerInvariant();
        return endung is ".lmt" or ".lmu" or ".lcf" or ".ldb" or ".json"
            or ".ini" or ".txt";
    }
}
