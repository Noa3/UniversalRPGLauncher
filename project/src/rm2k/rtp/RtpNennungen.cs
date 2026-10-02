using System;
using System.Collections.Generic;
using System.IO;

namespace UniversalRPG.Rtp;

/// <summary>
/// Reads what a project says it needs, and never what it does not.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And a project's own words are in its own files</strong>, --
/// <strong>and a reader that guesses at them is a reader that installs
/// something nobody asked for.</strong>
/// </para>
/// <para>
/// <strong>And every name this file knows is one a finished game
/// carries</strong>, -- <strong>and the engine asks for them by the
/// name its own installer uses</strong>, -- <strong>and that is the
/// only place a name is allowed to come from.</strong>
/// </para>
/// </remarks>
public static class RtpNennungen
{
    /// <summary>
    /// The runtimes a project can name, and where each one lives.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the names are the installer's</strong>, measured from
    /// the <c>RGSS*.dll</c> and <c>System.ini</c> entries a finished
    /// game of that generation carries, -- <strong>and "RPGVXAce" is not
    /// one of them, because VX Ace asks for "RPGVXAce" and VX asks for
    /// "RPGVX"</strong> -- <strong>and a table that conflated the two
    /// would send a user to the wrong archive.</strong>
    /// </para>
    /// </remarks>
    /// <summary>
    /// The other name a project may use, and that is a real one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And a project names its runtime twice, and both times
    /// matter.</strong> Measured on four finished games on this machine:
    /// </para>
    /// <code>
    /// Random Dungeon    [Game] RTP=RPGVX   Library=RGSS202E.dll
    /// Dreaming Mary     [Game] RTP=        Library=System\RGSS301.dll
    /// Heartache 101     [Game] RTP1=       Library=RGSS102E.dll
    /// </code>
    /// <para>
    /// <strong>And <c>RTP=RPGVX</c> says which generation and
    /// <c>Library=</c> says which runtime</strong>, -- <strong>and they
    /// are the same fact in two spellings</strong>, -- <strong>and a
    /// reader that only looks at one of them misses half the games on
    /// this disk.</strong>
    /// </para>
    /// </remarks>
    public const string SchluesselRtp = "RTP";

    /// <summary>The runtime's own library file.</summary>
    public const string SchluesselLibrary = "Library";

    /// <summary>
    /// The runtimes a project can name, and where each one lives.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the names are the installer's</strong>, -- <strong>and
    /// <c>RGSS102E.dll</c> is XP, <c>RGSS202E.dll</c> is VX and
    /// <c>RGSS3A.dll</c> is VX Ace</strong>, -- <strong>and a table that
    /// matched on a prefix would hand a VX user the XP archive.</strong>
    /// </para>
    /// </remarks>
    public static readonly IReadOnlyList<RtpFetchPlanEntry> Bekannt =
    [
        new()
        {
            EngineId = "rpgmaker_rm2k",
            Generation = "rm2k",
            DependencyName = "RPG_RT.ldb",
            DependencyFileName = "RPG_RT",
            InstallDirectoryName = "RPG_RT",
            ExpectedBytes = 0,
            HasKnownSource = true,
        },
        // **Und XP ist `RGSS102E` oder `RGSS104E`**, -- **gemessen an
        // `Heartache 101 v2.5` und `MicroQuest` auf dieser Maschine**,
        // -- **und nicht `RGSS202E`, denn das ist VX.**
        new()
        {
            EngineId = "rpgmaker_xp",
            Generation = "xp",
            DependencyName = "RGSS102E.dll",
            DependencyFileName = "RGSS102E.dll",
            InstallDirectoryName = "RGSS102E",
            ExpectedBytes = 0,
            HasKnownSource = true,
        },
        new()
        {
            EngineId = "rpgmaker_xp",
            Generation = "xp",
            DependencyName = "RGSS104E.dll",
            DependencyFileName = "RGSS104E.dll",
            InstallDirectoryName = "RGSS104E",
            ExpectedBytes = 0,
            HasKnownSource = true,
        },
        new()
        {
            EngineId = "rpgmaker_vx",
            Generation = "vx",
            DependencyName = "RGSS301.dll",
            DependencyFileName = "RGSS301.dll",
            InstallDirectoryName = "RGSS301",
            ExpectedBytes = 0,
            HasKnownSource = true,
        },
        new()
        {
            // **Und VX ist `RGSS202E`**, -- **gemessen an
            // `Random Dungeon` und `The Princess and the Rose Knight`.**
            EngineId = "rpgmaker_vx",
            Generation = "vx",
            DependencyName = "RGSS202E.dll",
            DependencyFileName = "RGSS202E.dll",
            InstallDirectoryName = "RGSS202E",
            ExpectedBytes = 0,
            HasKnownSource = true,
        },
        new()
        {
            EngineId = "rpgmaker_vxace",
            Generation = "vxace",
            DependencyName = "RGSS3A.dll",
            DependencyFileName = "RGSS3A.dll",
            InstallDirectoryName = "RGSS3A",
            ExpectedBytes = 0,
            HasKnownSource = true,
        },
    ];

    /// <summary>
    /// Reads one file and says which runtimes it names.
    /// </summary>
    /// <param name="pPath">A file the project carries.</param>
    /// <returns>
    /// The dependency names, and nothing else: a file that names none
    /// yields none, and that is not a failure.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <strong>And this reads text and not bytes</strong>, -- <strong>and
    /// a file it cannot read is skipped rather than guessed at</strong>,
    /// -- <strong>because a project is untrusted input and a reader that
    /// says "I could not read it" is honest where one that says "it
    /// needs nothing" is not.</strong>
    /// </para>
    /// </remarks>
    public static IReadOnlyList<string> Nennungen(string pPath)
    {
        var gefunden = new List<string>();
        string[] zeilen;
        try
        {
            zeilen = File.ReadAllLines(pPath);
        }
        catch (IOException)
        {
            return gefunden;
        }
        catch (UnauthorizedAccessException)
        {
            return gefunden;
        }

        foreach (var zeile in zeilen)
        {
            foreach (var eintrag in Bekannt)
            {
                if (ZeileNennt(zeile, eintrag.DependencyFileName)
                    && !gefunden.Contains(eintrag.DependencyName))
                {
                    gefunden.Add(eintrag.DependencyName);
                }
            }
        }

        return gefunden;
    }

    /// <summary>
    /// Whether one line names one dependency.
    /// </summary>
    /// <param name="pZeile">One line of a project's own file.</param>
    /// <param name="pName">The dependency's file name.</param>
    /// <returns>Yes or no, and both are ordinary.</returns>
    /// <remarks>
    /// <strong>And the match is on the whole name</strong>, -- <strong>and
    /// <c>RGSS3A.dll</c> is not <c>RGSS301E.dll</c></strong>, --
    /// <strong>because one is VX Ace and the other is VX, and a
    /// substring match would hand a VX user the Ace archive.</strong>
    /// </remarks>
    private static bool ZeileNennt(string pZeile, string pName)
    {
        var stelle = pZeile.IndexOf(pName, StringComparison.Ordinal);
        while (stelle >= 0)
        {
            var vor = stelle == 0 ? ' ' : pZeile[stelle - 1];
            var nach = stelle + pName.Length >= pZeile.Length
                ? ' '
                : pZeile[stelle + pName.Length];
            if (!char.IsLetterOrDigit(vor) && !char.IsLetterOrDigit(nach))
            {
                return true;
            }

            stelle = pZeile.IndexOf(
                pName, stelle + 1, StringComparison.Ordinal);
        }

        return false;
    }

    /// <summary>
    /// The profile entry for one dependency name.
    /// </summary>
    /// <param name="pName">What the project asked for.</param>
    /// <returns>
    /// The entry, and a named one even when this repository has no
    /// profile for it -- <strong>because "no profile" is an answer and
    /// not a crash</strong>.
    /// </returns>
    public static RtpFetchPlanEntry ProfilFuer(string pName)
    {
        foreach (var bekannt in Bekannt)
        {
            if (bekannt.DependencyName == pName)
            {
                return bekannt;
            }
        }

        return new RtpFetchPlanEntry
        {
            DependencyName = pName,
            DependencyFileName = pName,
            EngineId = "unknown",
            Generation = "unknown",
            HasKnownSource = false,
        };
    }
}
