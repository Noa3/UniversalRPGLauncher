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
        const long maxEntryBytes = 256L * 1024 * 1024;
        const long maxTotalBytes = 2L * 1024 * 1024 * 1024;
        if (archiv.Entries.Count > 10000)
        {
            return new Ergebnis { Verweigert = new[] { "Das Archiv enthaelt mehr als 10000 Eintraege." } };
        }
        foreach (var eintrag in archiv.Entries)
        {
            if (eintrag.Length > maxEntryBytes || eintrag.Length > maxTotalBytes - bytes
                || ((eintrag.ExternalAttributes >> 16) & 0xF000) == 0xA000)
            {
                verweigert.Add(eintrag.FullName + " (Groessenlimit oder symbolischer Link)");
                continue;
            }
            var name = eintrag.FullName.Replace('\\', '/');
            var segments = name.TrimEnd('/').Split('/');
            if (Path.IsPathRooted(name) || name.Contains(':')
                || Array.Exists(segments, segment => string.IsNullOrEmpty(segment)
                    || segment is "." or ".." || segment.EndsWith('.') || segment.EndsWith(' ')
                    || IsWindowsDeviceName(segment)
                    || segment.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0))
            {
                verweigert.Add(eintrag.FullName + " (ungueltiger Archivpfad)");
                continue;
            }

            var voll = Path.GetFullPath(Path.Combine(wurzel,
                name.Replace('/', Path.DirectorySeparatorChar)));
            var prefix = Path.TrimEndingDirectorySeparator(wurzel)
                + Path.DirectorySeparatorChar;
            if (!voll.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                verweigert.Add(eintrag.FullName + " (verlaesst das Ziel)");
                continue;
            }

            if (HasReparsePoint(voll))
            {
                verweigert.Add(eintrag.FullName + " (Link im Zielpfad)");
                continue;
            }
            if (File.Exists(voll))
            {
                verweigert.Add(eintrag.FullName + " (vorhandene Datei wird nicht ersetzt)");
                continue;
            }

            // Directory entries need the same confinement check as files.
            if (string.IsNullOrEmpty(eintrag.Name))
            {
                Directory.CreateDirectory(voll);
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(voll)!);

            using (var zielstrom = new FileStream(voll, FileMode.CreateNew, FileAccess.Write))
            using (var quelle = eintrag.Open())
            {
                var buffer = new byte[81920];
                var written = 0L;
                int read;
                while ((read = quelle.Read(buffer, 0, buffer.Length)) > 0)
                {
                    written += read;
                    if (written > eintrag.Length || written > maxEntryBytes
                        || written > maxTotalBytes - bytes)
                        throw new InvalidDataException("Archivinhalt ueberschreitet das Groessenlimit.");
                    zielstrom.Write(buffer, 0, read);
                }
                if (written != eintrag.Length)
                    throw new InvalidDataException("Archivinhalt ist abgeschnitten.");
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
    private static bool IsWindowsDeviceName(string pSegment)
    {
        var name = pSegment.Split('.')[0].ToUpperInvariant();
        return name is "CON" or "PRN" or "AUX" or "NUL" or "CONIN$" or "CONOUT$"
            || (name.Length == 4 && (name.StartsWith("COM") || name.StartsWith("LPT"))
                && (name[3] is >= '1' and <= '9' or '¹' or '²' or '³'));
    }

    private static bool HasReparsePoint(string pPath)
    {
        for (var current = pPath; !string.IsNullOrEmpty(current);
            current = Path.GetDirectoryName(current))
        {
            if ((File.Exists(current) || Directory.Exists(current))
                && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                return true;
        }
        return false;
    }

    public static bool IstZip(string pPfad)
    {
        using var strom = File.OpenRead(pPfad);
        var kopf = new byte[2];
        return strom.Read(kopf, 0, 2) == 2 && kopf[0] == 'P' && kopf[1] == 'K';
    }
}
