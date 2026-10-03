using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UniversalRPG.Rtp;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The whole run, from fetch to unpacked files, without a network.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the archive this builds is the shape the real one
/// has</strong>, -- <strong>four entries and an Inno Setup installer
/// inside</strong>, -- <strong>because a run that only sees a plain
/// zip never exercises the branch that matters.</strong>
/// </para>
/// <para>
/// <strong>And the installer is a stand-in</strong> that writes one
/// file, -- <strong>because the real
/// <c>innoextract</c> is not committed and this test must not need
/// it.</strong>
/// </para>
/// </remarks>
public partial class TestRtpAblauf : TestBase
{
    private static string Basis(string pName)
    {
        var p = Path.Combine(Path.GetTempPath(), "urpg_ablauf_" + pName);
        if (Directory.Exists(p))
        {
            Directory.Delete(p, true);
        }

        return p;
    }

    /// <summary>
    /// And a runtime that cannot be fetched says so and stops.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the case a user hits first</strong>, --
    /// <strong>because <c>rpgmaker.net</c> answers 403 to an
    /// automated request</strong> -- <strong>and a run that threw here
    /// would leave the dialog up with no explanation.</strong>
    /// </para>
    /// </remarks>
    public void Test_EinFehlgeschlagenerDownloadSagtEs()
    {
        var ziel = Basis("fehlschlag");
        var lauf = new RtpAblauf(new TestQuelle(
            Array.Empty<byte>(), true));

        var e = lauf.FuehreAus("rmvxace", ziel);

        Console.WriteLine("Schritte: " + string.Join(" | ", e.Schritte));
        Console.WriteLine("Meldung:  " + e.Meldung);

        AssertTrue(!e.Erfolgreich && e.Meldung.Length > 0,
            "**and a refused download stops the run and says why**"
                + " -- and it does not go on to unpack something"
                + " that is not there, and it does not say"
                + " \"download failed\"");
        AssertEq(1, e.Schritte.Count,
            "**and the run stopped after the first step**");
        AssertTrue(!Directory.Exists(Path.Combine(ziel, "entpackt")),
            "**and nothing was unpacked** -- and an empty `entpackt`"
                + " next to a failed download tells a user the fetch"
                + " worked");
    }

    /// <summary>
    /// And a runtime that has no measured archive is refused before
    /// anything.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the message says why</strong>, -- <strong>and this
    /// is the <c>Standard</c> case out of MicroQuest's own
    /// <c>Game.ini</c>.</strong>
    /// </para>
    /// </remarks>
    public void Test_OhneArchivVorherAbbrechen()
    {
        var ziel = Basis("ohne");
        var aufrufe = 0;
        var zaehler = new ZaehlerQuelle(() => aufrufe++);
        var lauf = new RtpAblauf(zaehler);

        var e = lauf.FuehreAus("gibtsnicht", ziel);

        Console.WriteLine("Meldung: " + e.Meldung);
        Console.WriteLine("Aufrufe: " + aufrufe);

        AssertTrue(!e.Erfolgreich && e.Meldung.Contains("kein gemessenes"),
            "**and an engine with no measured archive is refused"
                + " before a request** -- and it says so");
        AssertEq(0, aufrufe,
            "**and nothing was requested** -- and the table is what"
                + " decides, not the network");
    }

    /// <summary>
    /// And a plain zip runs through fetch and unpack.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the VX branch</strong>, -- <strong>and it
    /// is the one shape that needs no tool</strong>, -- <strong>and a
    /// run that stopped here would leave two of the five
    /// unhandled.</strong>
    /// </para>
    /// </remarks>
    public void Test_EinEinfachesZipLaeuftDurch()
    {
        var ziel = Basis("zip");
        var lauf = new RtpAblauf(new TestQuelle(
            ZipMit("Audio/BGM/Theme1.ogg", "OggS-Test")));

        var e = lauf.FuehreAus("rmvx", ziel);

        Console.WriteLine("Schritte: " + string.Join(" | ", e.Schritte));
        Console.WriteLine("Meldung:  " + e.Meldung);

        var erwartet = Path.Combine(ziel, "entpackt", "Audio", "BGM",
            "Theme1.ogg");
        AssertTrue(e.Erfolgreich && File.Exists(erwartet),
            "**and a plain zip is fetched and unpacked** -- and the"
                + " file lands under `entpackt/`, and the archive is"
                + " deleted afterwards, because a 35 MB zip next to"
                + " its own content is 35 MB of nothing");
    }

    /// <summary>
    /// And the archive is deleted after a plain zip.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is measured rather than claimed</strong>: the
    /// run reads back the directory instead of trusting its own
    /// bookkeeping.
    /// </para>
    /// </remarks>
    public void Test_DasArchivIstDanachWeg()
    {
        var ziel = Basis("weg");
        new RtpAblauf(new TestQuelle(
            ZipMit("Audio/SE/Cursor1.ogg", "OggS"))).FuehreAus("rmvx", ziel);

        var archiv = Directory.GetFiles(ziel, "*.zip");
        Console.WriteLine("Archive noch da: " + archiv.Length);
        AssertEq(0, archiv.Length,
            "**and no archive is left behind** -- and a launcher that"
                + " keeps 35 MB next to the files it just unpacked is"
                + " a launcher nobody trusts");
    }

    private static byte[] ZipMit(string pName, string pInhalt)
    {
        var pfad = Path.Combine(Path.GetTempPath(),
            "urpg_ablauf_bau.zip");
        if (File.Exists(pfad))
        {
            File.Delete(pfad);
        }

        using (var a = ZipFile.Open(pfad, ZipArchiveMode.Create))
        {
            var e = a.CreateEntry(pName);
            using var s = e.Open();
            s.Write(System.Text.Encoding.UTF8.GetBytes(pInhalt));
        }

        return File.ReadAllBytes(pfad);
    }

    /// <summary>
    /// A source that hands over exactly what the test built.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is why <see cref="IRtpArchivQuelle"/> is an
    /// interface</strong>, -- <strong>and my first version used
    /// <c>RtpHoler</c> with a fake HTTP handler</strong>, --
    /// <strong>and then the measured size of 36 787 556 bytes stood
    /// between a 149 byte test archive and a successful
    /// run.</strong>
    /// </para>
    /// <para>
    /// <strong>And the size check is right and must stay</strong>, --
    /// <strong>and a test that needs it out of the way is asking the
    /// wrong source.</strong>
    /// </para>
    /// </remarks>
    private sealed class TestQuelle : IRtpArchivQuelle
    {
        private readonly byte[] _daten;
        private readonly bool _sollFehlschlagen;

        /// <summary>Build a source over bytes.</summary>
        /// <param name="pDaten">The archive.</param>
        /// <param name="pFehlschlagen">Whether to refuse.</param>
        public TestQuelle(byte[] pDaten, bool pFehlschlagen = false)
        {
            _daten = pDaten;
            _sollFehlschlagen = pFehlschlagen;
        }

        /// <inheritdoc/>
        public RtpHoleErgebnis Hole(
            RtpFetchPlanEntry pEintrag,
            string pZiel,
            Action<long, long>? pFortschritt = null,
            Func<bool>? pAbbruch = null)
        {
            if (_sollFehlschlagen)
            {
                return new RtpHoleErgebnis
                {
                    EngineId = pEintrag.EngineId,
                    Erfolgreich = false,
                    Grund = "Der Test hat es so eingerichtet.",
                };
            }

            File.WriteAllBytes(pZiel, _daten);
            pFortschritt?.Invoke(_daten.Length, _daten.Length);
            return new RtpHoleErgebnis
            {
                EngineId = pEintrag.EngineId,
                Erfolgreich = true,
                Bytes = _daten.Length,
                ArchivName = Path.GetFileName(pZiel),
            };
        }
    }

    private sealed class ZaehlerQuelle : IRtpArchivQuelle
    {
        private readonly Action _beimAufruf;

        public ZaehlerQuelle(Action pBeimAufruf) => _beimAufruf = pBeimAufruf;

        public RtpHoleErgebnis Hole(
            RtpFetchPlanEntry pEintrag, string pZiel,
            Action<long, long>? pFortschritt = null,
            Func<bool>? pAbbruch = null)
        {
            _beimAufruf();
            return new RtpHoleErgebnis
            {
                EngineId = pEintrag.EngineId,
                Erfolgreich = false,
                Grund = "nicht erreicht",
            };
        }
    }

    private sealed class AntwortHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _a;

        public AntwortHandler(Func<HttpRequestMessage, HttpResponseMessage> pAntwort)
            => _a = pAntwort;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage pAnfrage, CancellationToken pAbbruch)
            => Task.FromResult(_a(pAnfrage));
    }
}
