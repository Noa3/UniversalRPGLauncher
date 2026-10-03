using System;
using System.IO;

namespace UniversalRPG.Rtp;

/// <summary>
/// Fetch, unpack, unpack the installer, and say what happened.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is the piece that joins the four others</strong>,
/// -- <strong>and it is here and not in the launcher</strong>, --
/// <strong>because a launcher node cannot be tested in a headless
/// run and this can.</strong>
/// </para>
/// <para>
/// <strong>And every step reports rather than throws</strong>, --
/// <strong>because a user who declined an elevation is not looking
/// at a stack trace.</strong>
/// </para>
/// </remarks>
public sealed class RtpAblauf
{
    /// <summary>And what a run did, step by step.</summary>
    public sealed class Ergebnis
    {
        /// <summary>And whether the runtime is where it belongs now.</summary>
        public bool Erfolgreich { get; set; }

        /// <summary>And where it was put.</summary>
        public string Ziel { get; set; } = "";

        /// <summary>And what every step said, in order.</summary>
        public System.Collections.Generic.List<string> Schritte { get; } =
            new();

        /// <summary>
        /// And one line a user can read.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <strong>And this is the last step and not an
        /// exception</strong>, -- <strong>because "the download failed"
        /// without a reason is the thing this repository has been
        /// correcting all session.</strong>
        /// </para>
        /// </remarks>
        public string Meldung { get; set; } = "";
    }

    private readonly IRtpArchivQuelle _quelle;
    private readonly string? _werkzeug;

    /// <summary>Builds a run over a fetcher.</summary>
    /// <param name="pQuelle">Where the archive comes from.</param>
    /// <param name="pWerkzeug">The Inno reader, and null means the
    /// standard path.</param>
    public RtpAblauf(IRtpArchivQuelle pQuelle, string? pWerkzeug = null)
    {
        _quelle = pQuelle;
        _werkzeug = pWerkzeug;
    }

    /// <summary>
    /// And does everything one runtime needs, into one directory.
    /// </summary>
    /// <param name="pEngineId">Which runtime.</param>
    /// <param name="pZiel">Where the files go.</param>
    /// <param name="pFortschritt">Called with what happened so far.</param>
    /// <param name="pAbbruch">Asked between steps and while downloading.</param>
    /// <param name="pZustimmung">
    /// <strong>The visible consent of the person in front of the
    /// screen</strong>, -- <strong>and it is the first parameter and
    /// not the last on purpose</strong>.
    /// </param>
    /// <returns>What it did.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And the consent is a parameter and not a convention.</strong>
    /// -- <strong>A run that downloads 194 million bytes without asking
    /// is the one thing this repository must never do quietly</strong>,
    /// -- <strong>and a call site that has a boolean next to a string
    /// is harder to get wrong than a call site that has to remember
    /// an earlier <c>if</c>.</strong>
    /// </para>
    /// <para>
    /// <strong>And this method has already done the right thing when
    /// told no</strong>, -- <strong>and the test asserts that the
    /// fetcher was not touched at all, because "no" that still
    /// opens a connection is not consent.</strong>
    /// </para>
    /// </remarks>
    public Ergebnis FuehreAus(
        bool pZustimmung,
        string pEngineId,
        string pZiel,
        Action<string>? pFortschritt = null,
        Func<bool>? pAbbruch = null)
    {
        var e = new Ergebnis { Ziel = pZiel };

        if (!pZustimmung)
        {
            e.Meldung = "Ohne ausdrueckliche Zustimmung wurde nichts"
                + " geholt und nichts entpackt.";
            return e;
        }

        if (!RtpArchivFakten.Alle().TryGetValue(pEngineId, out var fakt))
        {
            e.Meldung = "Fuer " + pEngineId + " steht kein gemessenes"
                + " Archiv in der Tabelle.";
            return e;
        }

        Directory.CreateDirectory(pZiel);
        var archiv = Path.Combine(pZiel, fakt.ArchivName);

        // **Schritt 1: holen.**
        var geholt = _quelle.Hole(
            new RtpFetchPlanEntry { EngineId = pEngineId },
            archiv,
            (ist, soll) => pFortschritt?.Invoke(
                $"{ist} von {soll} Bytes"),
            pAbbruch);
        e.Schritte.Add("Holen: " + (geholt.Erfolgreich
            ? geholt.Bytes + " Bytes"
            : geholt.Grund));
        if (!geholt.Erfolgreich)
        {
            e.Meldung = geholt.Grund;
            return e;
        }

        // **Schritt 2: auspacken, und die Form entscheidet.**
        if (fakt.Form == RtpArchivForm.Zip)
        {
            var entpackt = RtpEntpacker.Entpacke(archiv,
                Path.Combine(pZiel, "entpackt"));
            e.Schritte.Add("Entpacken: " + entpackt.Dateien + " Dateien");
            if (entpackt.Verweigert.Count > 0)
            {
                e.Schritte.Add("Verweigert: "
                    + entpackt.Verweigert.Count + " Eintraege");
            }

            File.Delete(archiv);
            e.Erfolgreich = entpackt.Dateien > 0;
            e.Meldung = e.Erfolgreich
                ? entpackt.Dateien + " Dateien entpackt."
                : "Das Archiv war leer.";
            return e;
        }

        // **Schritt 3: der Installer, und er wird nie gestartet.**
        if (fakt.Form == RtpArchivForm.ZipMitInstaller)
        {
            var huelle = RtpEntpacker.Entpacke(archiv,
                Path.Combine(pZiel, "huelle"));
            e.Schritte.Add("Huellschicht: " + huelle.Dateien + " Dateien");

            var setup = Path.Combine(pZiel, "huelle", "RTP100", "Setup.exe");
            if (!File.Exists(setup))
            {
                e.Meldung = "Die Huellschicht enthaelt kein Setup.exe,"
                    + " und ein Installer, den es nicht gibt, wird"
                    + " nicht gestartet.";
                return e;
            }

            var gelesen = InnoAufbereiter.Lese(setup,
                Path.Combine(pZiel, "heraus"), _werkzeug);
            e.Schritte.Add("Installer lesen: "
                + (gelesen.Erfolgreich
                    ? gelesen.Dateien + " Dateien"
                    : gelesen.Ausgabe));
            if (!gelesen.Erfolgreich)
            {
                e.Meldung = gelesen.Ausgabe;
                return e;
            }

            e.Erfolgreich = gelesen.Dateien > 0;
            e.Meldung = gelesen.Dateien + " Dateien gelesen.";
            File.Delete(archiv);
            return e;
        }

        // **Und das ist ein Installer ohne Hülle, und er wird auch
        // nicht gestartet.**
        var direkt = InnoAufbereiter.Lese(archiv,
            Path.Combine(pZiel, "heraus"), _werkzeug);
        e.Schritte.Add("Installer lesen: " + (direkt.Erfolgreich
            ? direkt.Dateien + " Dateien"
            : direkt.Ausgabe));
        e.Erfolgreich = direkt.Erfolgreich && direkt.Dateien > 0;
        e.Meldung = e.Erfolgreich
            ? direkt.Dateien + " Dateien gelesen."
            : direkt.Ausgabe;
        return e;
    }
}
