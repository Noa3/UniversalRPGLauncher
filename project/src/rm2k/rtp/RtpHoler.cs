using System;
using System.IO;
using System.Net.Http;
using System.Threading;

namespace UniversalRPG.Rtp;

/// <summary>
/// Fetches an RTP archive from the vendor's own asset host.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is the one class that reaches the network, and it
/// is the only one</strong>, -- <strong>because a repository whose
/// every type can fetch is a repository whose boundary cannot be
/// tested.</strong>
/// </para>
/// <para>
/// <strong>And the address comes from <see cref="RtpArchivFakten"/>
/// and never from a string built here</strong>, -- <strong>because
/// every one of those addresses was measured with a <c>HEAD</c>
/// request</strong>, -- <strong>and a reader that assembled one would
/// be guessing.</strong>
/// </para>
/// <para>
/// <strong>And the size is checked before and after the transfer</strong>,
/// -- <strong>because a mirror that answers a 200 with an error page
/// is the failure this repository has been correcting all
/// session.</strong>
/// </para>
/// </remarks>
public sealed class RtpHoler : IRtpArchivQuelle
{
    private readonly HttpClient _client;
    private readonly bool _besitzeClient;

    /// <summary>
    /// Builds a fetcher with its own client.
    /// </summary>
    public RtpHoler()
        : this(new HttpClient(), true)
    {
    }

    /// <summary>
    /// Builds a fetcher over a client the caller owns.
    /// </summary>
    /// <param name="pClient">The client, and the caller closes it.</param>
    public RtpHoler(HttpClient pClient)
        : this(pClient, false)
    {
    }

    private RtpHoler(HttpClient pClient, bool pBesitze)
    {
        _client = pClient;
        _besitzeClient = pBesitze;
    }

    /// <inheritdoc/>
    public RtpHoleErgebnis Hole(
        RtpFetchPlanEntry pEintrag,
        string pZiel,
        Action<long, long>? pFortschritt = null,
        Func<bool>? pAbbruch = null)
    {
        if (pEintrag == null)
        {
            throw new ArgumentNullException(nameof(pEintrag));
        }

        if (!RtpArchivFakten.Alle().TryGetValue(
            pEintrag.EngineId, out var fakt))
        {
            return new RtpHoleErgebnis
            {
                EngineId = pEintrag.EngineId,
                Erfolgreich = false,
                Grund = "Fuer " + pEintrag.EngineId + " steht keine"
                    + " gemessene Archivadresse in der Tabelle, -- und"
                    + " eine zu erfinden waere geraten.",
            };
        }

        var ziel = Path.GetFullPath(pZiel);
        Directory.CreateDirectory(Path.GetDirectoryName(ziel)!);

        // **Und ein Abbruch vor  dem  Transfer  ist  ein  Abbruch
        // und  kein  Fehler.**
        if (pAbbruch?.Invoke() == true)
        {
            return new RtpHoleErgebnis
            {
                EngineId = pEintrag.EngineId,
                ArchivName = fakt.ArchivName,
                Erfolgreich = false,
                Grund = "Abgebrochen, bevor etwas geholt wurde.",
            };
        }

        try
        {
            using var antwort = _client
                .GetAsync(fakt.Url, HttpCompletionOption.ResponseHeadersRead)
                .GetAwaiter().GetResult();

            if (!antwort.IsSuccessStatusCode)
            {
                return new RtpHoleErgebnis
                {
                    EngineId = pEintrag.EngineId,
                    ArchivName = fakt.ArchivName,
                    Erfolgreich = false,
                    Grund = "Der Server antwortete "
                        + (int)antwort.StatusCode + " "
                        + antwort.ReasonPhrase + ".",
                };
            }

            var erwartet = antwort.Content.Headers.ContentLength
                ?? fakt.Bytes;

            using var quelle = antwort.Content.ReadAsStreamAsync()
                .GetAwaiter().GetResult();
            using (var senke = File.Create(ziel))
            {
                var puffer = new byte[81920];
                var gehabt = 0L;
                int gelesen;
                while ((gelesen = quelle.Read(puffer, 0, puffer.Length)) > 0)
                {
                    senke.Write(puffer, 0, gelesen);
                    gehabt += gelesen;
                    pFortschritt?.Invoke(gehabt, erwartet);

                    if (pAbbruch?.Invoke() == true)
                    {
                        // **Und  ein  halber  Download  wird  nicht
                        // liegen gelassen**, -- **denn eine Datei mit
                        //  der  Endung  `.zip`  und  der  halben
                        // Groesse  ist  etwas,  das  ein  Aufrufer
                        // fuer  vollstaendig  haelt.**
                        senke.Flush();
                        return new RtpHoleErgebnis
                        {
                            EngineId = pEintrag.EngineId,
                            ArchivName = fakt.ArchivName,
                            Erfolgreich = false,
                            Bytes = gehabt,
                            Grund = "Abgebrochen nach " + gehabt
                                + " von " + erwartet + " Bytes, und die"
                                + " unvollstaendige Datei ist nicht"
                                + " liegen geblieben.",
                        };
                    }
                }
            }

            var ist = new FileInfo(ziel).Length;

            // **Und die Groesse kommt zuerst,  und das ist die
            // Reihenfolge und  nicht  der  Zufall.**
            //
            // **Und gemessen ist der  Fehler,  der  die  andere
            // Reihenfolge  erzeugt:**
            //
            // ```text
            // 64 Bytes:  "Die Antwort war kein Archiv"
            // ```
            //
            // **Und damit weiss der Aufrufer nicht,  dass er 194 MB
            // erwartet  hat**,  -- **und ein abgeschnittener
            // Download  ist  etwas anderes  als  eine  falsche
            // Seite**,  -- **und die Zahlen  gehoeren  in  die
            // Meldung.**
            if (ist != erwartet)
            {
                return new RtpHoleErgebnis
                {
                    EngineId = pEintrag.EngineId,
                    ArchivName = fakt.ArchivName,
                    Erfolgreich = false,
                    Bytes = ist,
                    Grund = "Es kamen " + ist + " Bytes statt der "
                        + " gemessenen " + erwartet + ".",
                };
            }

            // **Und  der  Inhalt  wird  geprueft,  nicht  nur  die
            // Groesse.**
            //
            // **Und das ist gemessen:**  PCGamingWiki antwortete
            // fuer alle vier RTP mit `text/html`,  obwohl der Name
            // auf `.zip` endet.
            if (!RtpEntpacker.IstZip(ziel) && fakt.Form != RtpArchivForm.Zip)
            {
                File.Delete(ziel);
                return new RtpHoleErgebnis
                {
                    EngineId = pEintrag.EngineId,
                    ArchivName = fakt.ArchivName,
                    Erfolgreich = false,
                    Grund = "Die Antwort war kein Archiv, -- und eine"
                        + " Seite, die sich als eines ausgibt, wird"
                        + " nicht behalten.",
                };
            }

            return new RtpHoleErgebnis
            {
                EngineId = pEintrag.EngineId,
                ArchivName = fakt.ArchivName,
                Erfolgreich = true,
                Bytes = ist,
            };
        }
        catch (HttpRequestException ausnahme)
        {
            return new RtpHoleErgebnis
            {
                EngineId = pEintrag.EngineId,
                ArchivName = fakt.ArchivName,
                Erfolgreich = false,
                Grund = ausnahme.Message,
            };
        }
        catch (IOException ausnahme)
        {
            return new RtpHoleErgebnis
            {
                EngineId = pEintrag.EngineId,
                ArchivName = fakt.ArchivName,
                Erfolgreich = false,
                Grund = "Schreiben ging nicht: " + ausnahme.Message,
            };
        }
    }
}
