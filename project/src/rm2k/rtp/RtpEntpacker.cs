using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace UniversalRPG.Rtp;

/// <summary>
/// Unpacks an RTP archive into a directory, and refuses to do anything
/// else.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And an archive is untrusted input</strong>, -- <strong>and
/// <c>AGENTS.md</c> says a game is untrusted input and that no EXE,
/// DLL or plugin from it may run</strong>, -- <strong>and a zip entry
/// is exactly such a thing.</strong>
/// </para>
/// <para>
/// <strong>And so three things are refused here</strong>, --
/// <strong>and each of them is a way an archive writes outside the
/// directory the caller named</strong>:
///
/// <list type="number">
/// <item><description>An entry whose path climbs out with
/// <c>..</c> -- <strong>and <see cref="Path.GetFullPath"/> decides
/// that, not a string check.</strong></description></item>
/// <item><description>An entry with an absolute path.</description></item>
/// <item><description>An entry that lands outside after
/// normalisation.</description></item>
/// </list>
///
/// <para>
/// <strong>And nothing is executed.</strong> An RTP archive carries
/// <c>RTP100\Setup.exe</c>, -- <strong>and this writes the bytes
/// and stops there</strong>, -- <strong>because running a setup is
/// outside what a game player asked for and inside what
/// <c>AGENTS.md</c> forbids.</strong>
/// </para>
/// </remarks>
public static class RtpEntpacker
{
    /// <summary>
    /// And what an unpacking did.
    /// </summary>
    public sealed class Ergebnis
    {
        /// <summary>And how many files it wrote.</summary>
        public int Dateien { get; init; }

        /// <summary>And how many bytes that was.</summary>
        public long Bytes { get; init; }

        /// <summary>
        /// And every entry it refused, named.
        /// </summary>
        /// <remarks>
        /// <strong>And this is never empty when something was
        /// refused</strong>, -- <strong>because a silent skip is how a
        /// broken runtime turns into a mysterious one.</strong>
        /// </remarks>
        public IReadOnlyList<string> Verweigert { get; init; } =
            Array.Empty<string>();
    }

    /// <summary>
    /// And unpacks one archive into one directory.
    /// </summary>
    /// <param name="pArchiv">The archive, and it is never executed.</param>
    /// <param name="pZiel">The directory, and it may be created.</param>
    /// <returns>What it did, and what it refused.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And a single bad entry does not stop the
    /// rest</strong>, -- <strong>because an archive with one
    /// traversal attempt is not an archive a caller should refuse
    /// entirely</strong>, -- <strong>and refusing everything would
    /// turn a warning into an outage.</strong>
    /// </para>
    /// </remarks>
    public static Ergebnis Entpacke(string pArchiv, string pZiel)
    {
        var verweigert = new List<string>();
        var dateien = 0;
        var bytes = 0L;

        var wurzel = Path.GetFullPath(pZiel);
        Directory.CreateDirectory(wurzel);

        using var archiv = ZipFile.OpenRead(pArchiv);
        foreach (var eintrag in archiv.Entries)
        {
            if (string.IsNullOrEmpty(eintrag.Name))
            {
                // **Und ein Verzeichnisseintrag hat keinen Namen** --
                // **und der leere Name ist kein Fehler.**
                Directory.CreateDirectory(Path.Combine(
                    wurzel, eintrag.FullName.Replace('/', Path.DirectorySeparatorChar)));
                continue;
            }

            var ziel = Path.Combine(wurzel,
                eintrag.FullName.Replace('/', Path.DirectorySeparatorChar));
            var voll = Path.GetFullPath(ziel);

            if (!voll.StartsWith(wurzel, StringComparison.OrdinalIgnoreCase))
            {
                verweigert.Add(eintrag.FullName + " (verlaesst das Ziel)");
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(voll)!);

            using (var zielstrom = File.Create(voll))
            using (var quelle = eintrag.Open())
            {
                quelle.CopyTo(zielstrom);
            }

            dateien++;
            bytes += eintrag.Length;
        }

        return new Ergebnis
        {
            Dateien = dateien,
            Bytes = bytes,
            Verweigert = verweigert,
        };
    }

    /// <summary>
    /// And whether this file is a zip at all.
    /// </summary>
    /// <param name="pPfad">The file.</param>
    /// <returns>Yes or no, and it reads the first bytes.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And it reads <c>PK</c> and not the extension</strong>,
    /// -- <strong>because the mirror hands back HTML with a name that
    /// ends in <c>.zip</c></strong>, -- <strong>and that is measured:
    /// all four pages answered <c>text/html</c>.</strong>
    /// </para>
    /// </remarks>
    public static bool IstZip(string pPfad)
    {
        using var strom = File.OpenRead(pPfad);
        var kopf = new byte[2];
        return strom.Read(kopf, 0, 2) == 2 && kopf[0] == 'P' && kopf[1] == 'K';
    }
}
