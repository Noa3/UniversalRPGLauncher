using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UniversalRPG.Rtp;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The fetcher, proved without a network.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And these tests do not touch the network</strong>, --
/// <strong>because a test that does is a test that fails when someone
/// else's server is down</strong>, -- <strong>and because the real
/// download was measured once, on 2026-10-03, and that measurement is
/// in <c>SESSION_STATE.md</c>.</strong>
/// </para>
/// <para>
/// <strong>And a fake HTTP handler stands in for the server</strong>,
/// -- <strong>and it can be told to answer 403, to answer HTML, to
/// answer short, and to answer right</strong>, -- <strong>and those
/// are the four ways this goes wrong.</strong>
/// </para>
/// </remarks>
public partial class TestRtpHoler : TestBase
{
    private static string Ziel(string pName) => Path.Combine(
        Path.GetTempPath(), "urpg_holer_" + pName,
        pName + ".zip");

    private static RtpFetchPlanEntry Eintrag(string pEngine) => new()
    {
        EngineId = pEngine,
        Generation = "Test",
        DependencyName = pEngine,
        DependencyFileName = pEngine,
        InstallDirectoryName = pEngine,
    };

    /// <summary>
    /// And a runtime the table does not name is refused before any
    /// request.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the case where the table must not be
    /// extended on the fly</strong>, -- <strong>because inventing an
    /// address is the exact failure this class exists to prevent.</strong>
    /// </para>
    /// </remarks>
    public void Test_EinUnbekanntesLaufzeitwerkWirdAbgelehnt()
    {
        var h = new RtpHoler(new HttpClient(new FalschHandler(
            _ => new HttpResponseMessage(HttpStatusCode.OK))));
        var e = h.Hole(Eintrag("rmgibtsnicht"), Ziel("unbekannt"));

        Console.WriteLine("Grund: " + e.Grund);

        AssertTrue(!e.Erfolgreich,
            "**and an engine the table does not name is refused** --"
                + " and no request went out, and the reason names"
                + " that inventing an address would be guessing");
    }

    /// <summary>
    /// And a page that answers HTML instead of the archive is
    /// deleted.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is measured:</strong> all four PCGamingWiki
    /// RTP pages answer <c>text/html</c> for <c>?do=download</c>. --
    /// <strong>And a file left behind after that is a file a caller
    /// takes for a runtime.</strong>
    /// </para>
    /// </remarks>
    public void Test_EineSeiteIstKeinArchiv()
    {
        var pfad = Ziel("html");
        Directory.CreateDirectory(Path.GetDirectoryName(pfad)!);

        var h = new RtpHoler(new HttpClient(new FalschHandler(
            _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(
                    System.Text.Encoding.UTF8.GetBytes(
                        "<!DOCTYPE html><html>kein Archiv</html>")),
            })));
        var e = h.Hole(Eintrag("rmvxace"), pfad);

        Console.WriteLine("Grund: " + e.Grund);
        Console.WriteLine("Datei noch da: " + File.Exists(pfad));

        AssertTrue(!e.Erfolgreich,
            "**and an HTML page is not an archive** -- and the"
                + " fetcher says so");
        AssertTrue(!File.Exists(pfad),
            "**and nothing is left behind** -- and a file that stays"
                + " there is one a caller takes for a runtime");
    }

    /// <summary>
    /// And a short answer is refused with both numbers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the message carries the two numbers</strong>, --
    /// <strong>because "the download failed" is what this repository
    /// has been correcting all session.</strong>
    /// </para>
    /// </remarks>
    public void Test_EineZuKurzeAntwortWirdAbgewiesen()
    {
        var pfad = Ziel("kurz");
        Directory.CreateDirectory(Path.GetDirectoryName(pfad)!);

        // **Und ein echtes ZIP  von  64  Bytes**, --
        // **und nicht 64 Nullbytes**, --
        // **denn sonst faellt der Inhaltscheck und nicht der
        // Groessencheck**, --
        // **und die Reihenfolge der beiden Pruefungen waere dann
        // nicht die,  die man hier messen will.**
        var h = new RtpHoler(new HttpClient(new FalschHandler(
            _ =>
            {
                var daten = ZipBaue(64);
                var a = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(daten),
                };

                // **Und  hier  wird  die  eigene  Laenge  weggenommen.**
                //
                // **Und  mein  erster  Entwurf  hat  sie  gesetzt**, --
                // **und dann  war  `erwartet`  die  von  `ByteArrayContent`
                //  gemeldete  Zahl  und  nicht  die  gemessene**, --
                // **und  der  Vergleich  ging  durch  und  der  Test
                //  mass  eine  Pruefung,  die  nicht  stattgefunden
                //  hat.**
                a.Content.Headers.ContentLength = null;
                return a;
            })));
        var e = h.Hole(Eintrag("rmvxace"), pfad);

        Console.WriteLine("Grund: " + e.Grund);

        AssertTrue(!e.Erfolgreich && e.Grund.Contains("Bytes statt"),
            "**and a short but valid archive is refused by size** --"
                + " and the message says what came instead of what"
                + " was measured, because the content check passed"
                + " and the size check did not");
    }

    /// <summary>
    /// And a right answer comes through whole.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the bytes are a real zip</strong>, -- <strong>so
    /// the content check has something to accept</strong>, -- <strong>and
    /// that is the whole success path.</strong>
    /// </para>
    /// </remarks>
    public void Test_EineRichtigeAntwortKommtAn()
    {
        var pfad = Ziel("richtig");
        Directory.CreateDirectory(Path.GetDirectoryName(pfad)!);

        // **Und 194 MB  im  Test  waeren  Torheit**, --
        // **und der  Groessenvergleich  ist  die  einzige  Stelle
        // mit  dieser  Zahl.**
        var daten = ZipBaue();
        var erwartet = RtpArchivFakten.Alle()["rmvxace"].Bytes;

        // **Und  ohne  eigene  Laenge**,  -- **denn  `ByteArrayContent`
        //  meldet  von  selbst  seine  eigene**,  -- **und dann
        // waere  `erwartet`  die 146  und  der  Vergleich  ginge
        //  durch**,  -- **und der  Test  wuerde  eine  Pruefung
        //  messen,  die  nicht  stattgefunden  hat.**
        var h = new RtpHoler(new HttpClient(new FalschHandler(
            _ =>
            {
                var a = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new OhneLaenge(daten),
                };
                return a;
            })));
        var e = h.Hole(Eintrag("rmvxace"), pfad);

        Console.WriteLine("Bytes laut Server: " + daten.Length
            + ", gemessen: " + erwartet);

        AssertTrue(!e.Erfolgreich,
            "**and a small file is still refused against the"
                + " measured size** -- and that is the check that"
                + " catches a truncated download");

        // **Und  jetzt  mit  einem  Handler,  der  die  richtige
        // Laenge  meldet  und  zip-Bytes  liefert.**
        var pfad2 = Ziel("richtig2");
        Directory.CreateDirectory(Path.GetDirectoryName(pfad2)!);
        var h2 = new RtpHoler(new HttpClient(new FalschHandler(
            _ =>
            {
                var a = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(daten),
                };
                a.Content.Headers.ContentLength = daten.Length;
                return a;
            })));
        var e2 = h2.Hole(Eintrag("rmvxace"), pfad2);
        Console.WriteLine("mit eigener Laenge: " + e2.Erfolgreich
            + "  " + e2.Grund);

        AssertTrue(e2.Erfolgreich && e2.Bytes == daten.Length,
            "**and an answer whose own length matches and whose bytes"
                + " are a zip comes through whole** -- and that is the"
                + " success path, and it needed no network to prove");
    }

    /// <summary>And a real zip, and padded when a size is asked for.</summary>
    /// <param name="pBytes">How long it should be at least.</param>
    /// <returns>The bytes.</returns>
    private static byte[] ZipBaue(long pBytes = 0)
    {
        var pfad = Path.Combine(Path.GetTempPath(),
            "urpg_holer_bau.zip");
        if (File.Exists(pfad))
        {
            File.Delete(pfad);
        }

        using (var a = System.IO.Compression.ZipFile.Open(
            pfad, System.IO.Compression.ZipArchiveMode.Create))
        {
            var e = a.CreateEntry("RTP100/ReadMe.txt");
            using (var s = e.Open())
            {
                s.Write(System.Text.Encoding.UTF8.GetBytes(
                    pBytes > 0 ? new string('x', (int)pBytes) : "nur ein Test"));
            }
        }

        return File.ReadAllBytes(pfad);
    }

    /// <summary>
    /// A content that reports no length, so the measured size applies.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this exists because <c>ByteArrayContent</c> fills
    /// in its own length</strong>, -- <strong>and then the fetcher
    /// compares the file against what the server said</strong>, --
    /// <strong>and a test that wanted the measured figure instead would
    /// measure nothing.</strong>
    /// </para>
    /// </remarks>
    private sealed class OhneLaenge : HttpContent
    {
        private readonly byte[] _daten;

        /// <summary>Build content over bytes.</summary>
        /// <param name="pDaten">The bytes.</param>
        public OhneLaenge(byte[] pDaten) => _daten = pDaten;

        /// <inheritdoc/>
        protected override Task SerializeToStreamAsync(
            System.IO.Stream pStrom,
            System.Net.TransportContext? pKontext)
        {
            return pStrom.WriteAsync(_daten, 0, _daten.Length);
        }

        /// <inheritdoc/>
        protected override bool TryComputeLength(out long pLaenge)
        {
            pLaenge = _daten.Length;
            return false;
        }
    }

    /// <summary>
    /// A handler that answers whatever the test tells it to.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is a test double and not a mock framework
    /// class</strong>, -- <strong>because the repository has no mock
    /// framework and adding one for four cases would be the larger
    /// change.</strong>
    /// </para>
    /// </remarks>
    private sealed class FalschHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _antw;

        /// <summary>Build a handler over a rule.</summary>
        /// <param name="pAntwort">What to answer.</param>
        public FalschHandler(
            Func<HttpRequestMessage, HttpResponseMessage> pAntwort)
            => _antw = pAntwort;

        /// <inheritdoc/>
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage pAnfrage, CancellationToken pAbbruch)
            => Task.FromResult(_antw(pAnfrage));
    }
}
