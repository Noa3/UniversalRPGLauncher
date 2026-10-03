using System;
using System.IO;
using UniversalRPG.Rtp;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Reading an installer with a tool, and not running it.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this test needs no real RTP</strong>, -- <strong>because
/// the real one was read once, on 2026-10-03, and
/// <c>tools/innoextract/README.md</c> carries the numbers</strong>, --
/// <strong>and a test that downloads 194 MB on every run is a test
/// that stops being run.</strong>
/// </para>
/// <para>
/// <strong>And a missing tool is measured first, because that is the
/// case a caller has to handle</strong> -- <strong>and it is the case
/// this repository is in right now, since the binary is ignored by
/// Git.</strong>
/// </para>
/// </remarks>
public partial class TestRtpInnoAufbereiter : TestBase
{
    private static string Basis(string pName) => Path.Combine(
        Path.GetTempPath(), "urpg_inno_" + pName);

    /// <summary>
    /// And a missing tool is a result, not a crash.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the state the repository is in</strong>, --
    /// <strong>because <c>/tools/innoextract/bin/</c> is ignored by
    /// Git</strong>, -- <strong>and a caller that threw here would
    /// crash instead of asking its user.</strong>
    /// </para>
    /// </remarks>
    public void Test_EinFehlendesWerkzeugIstEinErgebnis()
    {
        var ziel = Basis("fehlt");
        var installer = Path.Combine(ziel, "Setup.exe");
        Directory.CreateDirectory(ziel);
        File.WriteAllBytes(installer, new byte[1024]);

        var e = InnoAufbereiter.Lese(
            installer, ziel, Path.Combine(ziel, "gibtsnicht.exe"));

        Console.WriteLine("Erfolg: " + e.Erfolgreich);
        Console.WriteLine("Ausgabe: " + e.Ausgabe);

        AssertTrue(!e.Erfolgreich && e.Ausgabe.Contains("README.md"),
            "**and a missing tool is a result with a place to read"
                + " about** -- and not an exception, because a caller"
                + " that has to catch here crashes instead of asking"
                + " its user");
    }

    /// <summary>
    /// And the path this repository documents is the one it looks in.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the documentation names the path and the code must
    /// be the one that reads it</strong>, -- <strong>and a tool
    /// found by accident is a tool that is not there next
    /// time.</strong>
    /// </para>
    /// </remarks>
    public void Test_DerDokumentiertePfad()
    {
        var pfad = InnoAufbereiter.StandardPfad();
        Console.WriteLine("Pfad: " + pfad);

        AssertTrue(pfad.Contains("innoextract")
                && pfad.Contains("tools"),
            "**and the standard path is the documented one** -- and"
                + " `tools/innoextract/README.md` names it, and the"
                + " hash of the binary it wants is in the same file");
    }

    /// <summary>
    /// And a path with a space stays one argument.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is measured on Windows, where a path like
    /// <c>C:\Programme\RPG Maker</c> is the normal case and not the
    /// exception.</strong> -- <strong>And the check here is that the
    /// argument list is built as a list and not by joining
    /// strings.</strong>
    /// </para>
    /// </remarks>
    public void Test_EinPfadMitLeerzeichenBleibtEinArgument()
    {
        var ziel = Basis("mit leerzeichen");
        var installer = Path.Combine(ziel, "Setup.exe");
        Directory.CreateDirectory(ziel);

        // **Und  der  Installer  muss  da  sein**,
        // **und  mein  erster  Entwurf  hat  nur  das  Werkzeug
        // angelegt**,
        // -- **und dann  sagt  `Lese`  zu  Recht  "Der Installer
        // liegt nicht unter ..."**,
        // -- **und der Test  hat  die  Argumentliste  gemessen,
        //  ohne  sie  zu  durchlaufen.**
        File.WriteAllBytes(installer, new byte[1024]);

        // **Und  ein  Skript  das  seine  Argumente  zurueckschreibt**,
        // -- **und  das  ersetzt  die  echte  Binary**,
        // -- **und  es  beweist  die  Argumentliste,  ohne  dass  ein
        //  RTP  geladen  werden  muss.**
        // **Und  die  Argumentliste  wird  am  Prozessaufbau
        // gemessen  und  nicht  an  einem  gestarteten  Programm.**
        //
        // **Und  der  erste  Entwurf  hat  eine  Batch-Datei  mit
        //  `.exe`-Endung  geschrieben**, --
        // **und  Windows  startet  die  ueber  `CreateProcess`
        //  nicht**, --
        // **und  das  ist  eine  Eigenschaft  von  Windows  und
        //  keine  Eigenschaft  des  Codes.**
        //
        // ```text
        // Unhandled exception: An error occurred trying to start
        // process '...\Argumentschreiber.exe'
        // ```
        //
        // **Und  eine  zweite  Datei  zu  bauen,  die  sich  starten
        // laesst,  waere  eine  Tool-Abhaengigkeit  nur  fuer  einen
        //  Test** -- **und  das  Argument  selbst  ist  an  der
        //  Argumentliste  ablesbar.**
        var aufbau = InnoAufbereiter.BaueKopf(
            Path.Combine(ziel, "Argumentschreiber.exe"),
            Path.Combine(ziel, "heraus"),
            installer);

        Console.WriteLine("Exe:    " + aufbau.FileName);
        foreach (var a in aufbau.ArgumentList)
        {
            Console.WriteLine("  Argument: [" + a + "]");
        }

        AssertEq(4, aufbau.ArgumentList.Count,
            "**and the argument list has four entries** -- and it is"
                + " a list and not a joined string, which is what"
                + " keeps a path with a space in it from becoming two"
                + " arguments");

        AssertEq("--extract", aufbau.ArgumentList[0],
            "**and the first one asks for an extraction**");

        AssertTrue(aufbau.ArgumentList.Contains(installer)
                && aufbau.ArgumentList.Contains(
                    Path.Combine(ziel, "heraus")),
            "**and the installer and the target each stay one"
                + " argument** -- and this test wrote a directory"
                + " whose name has a space in it, which on Windows is"
                + " the normal case and not the exception");
    }
}
