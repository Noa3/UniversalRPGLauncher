using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace UniversalRPG.Rtp;

/// <summary>
/// Turns an Inno Setup installer into the files it carries, by reading
/// it with <c>innoextract</c> and never by running it.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is the second class that starts a process, and it
/// starts exactly one program</strong>, -- <strong>and that program is
/// a build tool with a recorded hash, not anything out of the
/// game.</strong>
/// </para>
/// <para>
/// <strong>And the installer itself is never run.</strong>
/// <c>RPGVXAce_RTP.zip</c> carries <c>RTP100/Setup.exe</c>, which is an
/// Inno Setup 5.4.2 installer, -- <strong>and running it would be
/// running game software on a user's machine without asking</strong>,
/// -- <strong>and <c>AGENTS.md</c> forbids it.</strong>
/// </para>
/// <para>
/// <strong>And the arguments are passed as a list, never as a
/// string</strong>, -- <strong>because a path with a space in it must
/// not become two arguments</strong>.
/// </para>
/// </remarks>
public static class InnoAufbereiter
{
    /// <summary>
    /// And what a run of the tool did.
    /// </summary>
    public sealed class Ergebnis
    {
        /// <summary>And how many files came out.</summary>
        public int Dateien { get; init; }

        /// <summary>And where they are.</summary>
        public string Wurzel { get; init; } = "";

        /// <summary>And what the tool said, both streams.</summary>
        public string Ausgabe { get; init; } = "";

        /// <summary>And whether it worked.</summary>
        public bool Erfolgreich { get; init; }
    }

    /// <summary>
    /// And where this repository expects the tool.
    /// </summary>
    /// <returns>The path, and it may not exist.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And the path is the one
    /// <c>tools/innoextract/README.md</c> documents</strong>, --
    /// <strong>and it is computed from the repository root rather than
    /// taken from the working directory</strong>, -- <strong>because a
    /// headless test runs from anywhere and a tool found by accident is
    /// a tool that is not there next time.</strong>
    /// </para>
    /// </remarks>
    public static string StandardPfad()
    {
        var basis = AppContext.BaseDirectory;

        // **Und  vier  Ebenen  hoch  ist  das  Projektverzeichnis,
        //  und  das  ist  gemessen  und  nicht  geraten.**
        for (var i = 0; i < 6; i++)
        {
            var kandidat = Path.Combine(basis, "tools", "innoextract",
                "bin", "innoextract.exe");
            if (File.Exists(kandidat))
            {
                return kandidat;
            }

            basis = Path.GetDirectoryName(basis) ?? basis;
        }

        return Path.Combine("tools", "innoextract", "bin", "innoextract.exe");
    }

    /// <summary>
    /// And builds the process call without starting it.
    /// </summary>
    /// <param name="pWerkzeug">The tool.</param>
    /// <param name="pZiel">Where the files go.</param>
    /// <param name="pInstaller">The file to read.</param>
    /// <returns>The start info, and nothing has run yet.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And this is a method of its own and not a private
    /// helper</strong>, -- <strong>because "does the argument list
    /// keep a path with a space in it as one argument" is a question a
    /// test has to be able to ask</strong>, --
    /// <strong>and asking it means starting a process, and a test that
    /// starts a process to check a list needs a real tool
    /// installed.</strong>
    /// </para>
    /// <para>
    /// <strong>And it is the same call <see cref="Lese"/> makes</strong>,
    /// -- <strong>and a test that checks a second construction instead
    /// of this one would be checking something nobody
    /// runs.</strong>
    /// </para>
    /// </remarks>
    public static ProcessStartInfo BaueKopf(
        string pWerkzeug, string pZiel, string pInstaller)
    {
        var kopf = new ProcessStartInfo
        {
            FileName = pWerkzeug,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        kopf.ArgumentList.Add("--extract");
        kopf.ArgumentList.Add("--output");
        kopf.ArgumentList.Add(Path.GetFullPath(pZiel));
        kopf.ArgumentList.Add(Path.GetFullPath(pInstaller));
        return kopf;
    }

    /// <summary>
    /// And reads one installer into one directory.
    /// </summary>
    /// <param name="pInstaller">The file to read, and never to run.</param>
    /// <param name="pZiel">Where the files go.</param>
    /// <param name="pWerkzeug">The tool, and null means the standard path.</param>
    /// <returns>What it did, and what it said.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And a missing tool is a result and not an
    /// exception</strong>, -- <strong>because a caller that has to
    /// catch to ask its user is a caller that will crash instead of
    /// asking.</strong>
    /// </para>
    /// </remarks>
    public static Ergebnis Lese(
        string pInstaller, string pZiel, string? pWerkzeug = null)
    {
        var werkzeug = pWerkzeug ?? StandardPfad();

        if (!File.Exists(werkzeug))
        {
            return new Ergebnis
            {
                Erfolgreich = false,
                Ausgabe = "innoextract liegt nicht unter " + werkzeug
                    + " -- und die Anleitung steht in"
                    + " tools/innoextract/README.md.",
            };
        }

        if (!File.Exists(pInstaller))
        {
            return new Ergebnis
            {
                Erfolgreich = false,
                Ausgabe = "Der Installer liegt nicht unter " + pInstaller,
            };
        }

        Directory.CreateDirectory(pZiel);

        var kopf = BaueKopf(werkzeug, pZiel, pInstaller);

        var standard = new StringBuilder();
        var fehler = new StringBuilder();

        using var prozess = new Process { StartInfo = kopf };
        prozess.Start();
        prozess.StandardOutput.ReadToEndAsync()
            .ContinueWith(t => standard.Append(t.Result));
        prozess.StandardError.ReadToEndAsync()
            .ContinueWith(t => fehler.Append(t.Result));
        prozess.WaitForExit();

        var dateien = 0;
        foreach (var f in Directory.EnumerateFiles(
            pZiel, "*", SearchOption.AllDirectories))
        {
            dateien++;
        }

        return new Ergebnis
        {
            Erfolgreich = prozess.ExitCode == 0,
            Dateien = dateien,
            Wurzel = Path.GetFullPath(pZiel),
            Ausgabe = (standard.ToString() + fehler.ToString()).Trim(),
        };
    }
}
