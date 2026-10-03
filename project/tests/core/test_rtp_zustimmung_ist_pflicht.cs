using System;
using System.Collections.Generic;
using System.IO;
using UniversalRPG.Rtp;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The consent, and that without it nothing is fetched.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is what criterion 8 actually asked for</strong>,
/// -- <strong>and it was not what the repository had.</strong> --
/// <strong>The download chain existed, the dialog existed, and the
/// chain itself had no consent parameter at all</strong>, --
/// <strong>so "the dialog asks" was a property of the one call site
/// and not of the thing that fetches.</strong>
/// </para>
/// <para>
/// <strong>And that is the fault this closes:</strong> a second call
/// site would have downloaded without asking, and no test would have
/// said so.
/// </para>
/// <para>
/// <strong>And the fetcher is counted, because "no" that still opens
/// a connection is not consent.</strong>
/// </para>
/// </remarks>
public partial class TestRtpZustimmungIstPflicht : TestBase
{
    /// <summary>
    /// And a fetcher that remembers every call.
    /// </summary>
    private sealed class ZaehlendeQuelle : IRtpArchivQuelle
    {
        public int Aufrufe;
        public readonly List<string> Angefordert = new();

        public RtpHoleErgebnis Hole(
            RtpFetchPlanEntry pEintrag,
            string pZiel,
            Action<long, long>? pFortschritt = null,
            Func<bool>? pAbbruch = null)
        {
            Aufrufe++;
            Angefordert.Add(pEintrag.EngineId);
            return new RtpHoleErgebnis
            {
                EngineId = pEintrag.EngineId,
                Erfolgreich = false,
                Grund = "this source counts and never delivers",
            };
        }
    }

    /// <summary>
    /// And no consent means no fetch, and a message that says so.
    /// </summary>
    public void Test_OhneZustimmungNichtsGeholt()
    {
        var quelle = new ZaehlendeQuelle();
        var lauf = new RtpAblauf(quelle);
        var ziel = Weg();
        try
        {
            var e = lauf.FuehreAus(false, "rmvxace", ziel);
            Console.WriteLine("Meldung: " + e.Meldung);

            AssertEq(0, quelle.Aufrufe,
                "**and the fetcher was not touched at all** -- and"
                    + " that is the whole point: a refusal that"
                    + " still opened a connection is not a refusal");
            AssertEq(0, quelle.Angefordert.Count,
                "**and no archive was asked for**");
            AssertTrue(!e.Erfolgreich,
                "**and the run reports that it did not happen**");
            AssertTrue(e.Meldung.Contains("Zustimmung"),
                "**and the message names the consent** -- and so a"
                    + " user who said no can read why nothing came");
            AssertEq(0, Directory.GetFiles(ziel).Length,
                "**and the target directory is empty** -- and an"
                    + " empty directory that exists is harmless, but"
                    + " a file in it would not be");
        }
        finally
        {
            if (Directory.Exists(ziel))
            {
                Directory.Delete(ziel, true);
            }
        }
    }

    /// <summary>
    /// And consent is what lets it run.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is a 195 megabyte archive and it is not
    /// going to be fetched.</strong> -- <strong>The assertion is on
    /// the call, not on the download.</strong>
    /// </para>
    /// </remarks>
    public void Test_MitZustimmungWirdEsVersucht()
    {
        var quelle = new ZaehlendeQuelle();
        var lauf = new RtpAblauf(quelle);
        var ziel = Weg();
        try
        {
            var e = lauf.FuehreAus(true, "rmvxace", ziel);
            Console.WriteLine("nach Zustimmung: " + quelle.Aufrufe
                + "  Meldung: " + e.Meldung.Substring(0,
                    Math.Min(90, e.Meldung.Length)));

            AssertEq(1, quelle.Aufrufe,
                "**and with consent the fetcher is asked once**");
            AssertEq(1, quelle.Angefordert.Count,
                "**and for rmvxace**");
            AssertEq("rmvxace", quelle.Angefordert[0],
                "**and the engine the game named**");
        }
        finally
        {
            if (Directory.Exists(ziel))
            {
                Directory.Delete(ziel, true);
            }
        }
    }

    /// <summary>
    /// And the signature itself, so a call site cannot omit it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is a source-level assertion on purpose.</strong>
    /// -- <strong>If the parameter ever gets a default value, this
    /// test fails</strong>, -- <strong>and that is the moment a caller
    /// stops being asked.</strong>
    /// </para>
    /// </remarks>
    public void Test_DerErsteParameterIstDieZustimmung()
    {
        var quelle = File.ReadAllText(
            "E:/URPG/project/src/rm2k/rtp/RtpAblauf.cs");
        var stelle = quelle.IndexOf(
            "public Ergebnis FuehreAus(", StringComparison.Ordinal);
        AssertTrue(stelle > 0,
            "**and the method is there**");
        var kopf = quelle.Substring(stelle, 220);
        Console.WriteLine("Kopf: "
            + kopf.Replace('\n', ' ').Substring(0, 150));

        AssertTrue(kopf.Contains("bool pZustimmung"),
            "**and the first parameter is the consent**");

        AssertTrue(!kopf.Contains("bool pZustimmung ="),
            "**and it has no default** -- and a default would let a"
                + " call site leave it out, which is exactly the"
                + " quiet download this repository must never do");

        // **Und  kein  Downloadpfad  ohne  das  Wort.**
        var aufrufer = 0;
        foreach (var datei in new[]
        {
            "E:/URPG/project/app/ui/Main.cs",
        })
        {
            var t = File.ReadAllText(datei);
            var pos = 0;
            while ((pos = t.IndexOf("FuehreAus(", pos,
                StringComparison.Ordinal)) >= 0)
            {
                aufrufer++;
                var danach = t.Substring(pos,
                    Math.Min(60, t.Length - pos));
                AssertTrue(danach.Contains("true,")
                        || danach.Contains("false,"),
                    "**and every call site says whether the person"
                        + " agreed** -- and that call site says: "
                        + danach.Split('\n')[0].Trim());
                pos += 10;
            }
        }

        AssertTrue(aufrufer > 0,
            "**and there is at least one call site to check**");
        Console.WriteLine("Aufrufer geprueft: " + aufrufer);
    }

    private static string Weg()
    {
        var ziel = Path.Combine(
            System.IO.Path.GetTempPath(),
            "urpg-rtp-zustimmung-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(ziel);
        return ziel;
    }
}
