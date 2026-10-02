using System;
using System.IO;
using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Prints the shape of one marshalled map, and that is its only job.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this test exists because guessing an index is how a
/// reader ends up confident and wrong.</strong> <c>RgssMapReader</c>
/// reads a page's conditions by the engine's own keys and its command
/// list by position, -- <strong>and neither of those was measured yet,
/// and a page read from the wrong index has the wrong commands on it
/// and still looks like a map.</strong>
/// </para>
/// </remarks>
public partial class TestRgssMapProbe : TestBase
{
    private const string Xp = "E:/RPGMakerGames/MicroQuest - Beneath"
        + " Brimestone 1.0/Data/Map001.rxdata";
    private const string Vx = "E:/RPGMakerGames/Random Dungeon"
        + " -English Version-/Data/Map001.rvdata";

    /// <summary>
    /// And what a page really holds.
    /// </summary>
    /// <remarks>
    /// <strong>And the output is the measurement</strong>, -- <strong>and
    /// it goes to the log and not into an assertion</strong>, -- <strong>
    /// because what this test is for is a human reading the shape and
    /// then writing the reader.</strong>
    /// </remarks>
    public void Test_DieFormEinerEchtenKarteGehoertInsProtokoll()
    {
        foreach (var pfad in new[] { Xp, Vx })
        {
            if (!File.Exists(pfad))
            {
                continue;
            }

            var wert = new MarshalReader(File.ReadAllBytes(pfad)).Read();
            System.Console.WriteLine(
                "=== " + Path.GetFileName(pfad) + ": Art=" + wert.Kind
                + ", Elemente=" + wert.Items.Count
                + ", Schluessel=" + wert.Keys.Count
                + ", Klasse=" + (wert.ClassName ?? "-"));
            for (var i = 0; i < wert.Keys.Count
                && i < wert.Items.Count; i++)
            {
                var schluessel = wert.Keys[i];
                if (schluessel != "@events" && schluessel != "@data"
                    && schluessel != "@width" && schluessel != "@height")
                {
                    continue;
                }

                System.Console.WriteLine(
                    $"  {schluessel} = Art {wert.Items[i].Kind}"
                    + $", Elemente {wert.Items[i].Items.Count}"
                    + $", Schluessel {wert.Items[i].Keys.Count}"
                    + $", {(wert.Items[i].Link == null ? "-" : wert.Items[i].Link.ToString())}");
                if (schluessel != "@events"
                    || wert.Items[i].Keys.Count == 0)
                {
                    continue;
                }

                // **Und jetzt eine Seite, und ihre Befehlsliste.**
                //
                // **Und ein Hash traegt die Ereignisnummern als
                // Schluessel** -- **und in VX sind es 43** -- **und der
                // Wert eines Schluessels ist das Ereignis mit seinen
                // Seiten unter `@pages`.**
                var ersterIndex = -1;
                var ersterSchluessel = "";
                for (var q = 0; q < wert.Items[i].Keys.Count; q++)
                {
                    if (wert.Items[i].Keys[q].StartsWith("@",
                        StringComparison.Ordinal))
                    {
                        continue;
                    }

                    ersterIndex = q;
                    ersterSchluessel = wert.Items[i].Keys[q];
                    break;
                }

                if (ersterIndex < 0)
                {
                    var namen = new System.Collections.Generic
                        .List<string>();
                    for (var q = 0; q < wert.Items[i].Keys.Count
                        && namen.Count < 6; q++)
                    {
                        namen.Add(wert.Items[i].Keys[q]);
                    }

                    System.Console.WriteLine(
                        "    (kein Ereignis, denn die Schluessel sind: "
                        + string.Join(" ", namen) + ")");
                    continue;
                }

                // **Und ein Hash legt Schluessel und Wert
                // abwechselnd in `Items`** -- **und `Keys[i]` ist nur
                // der Name des Paares**, -- **und der Schluessel eines
                // Ruby-Hashs ist ein Integer hier**, -- **und der Wert
                // steht deshalb bei `2 * Index + 1`.**
                //
                // **Und der erste Versuch griff auf `Items[Index]`**,
                // **und das ist der Schluessel**, -- **und die Ausgabe
                // sagte "Art integer"**, -- **und das war die
                // richtige Antwort auf die falsche Frage.**
                var erstesEreignis = wert.Items[i].Items[ersterIndex * 2 + 1];
                System.Console.WriteLine(
                    $"    Ereignis {ersterSchluessel}:"
                    + $" Art {erstesEreignis.Kind},"
                    + $" {erstesEreignis.Keys.Count} Felder, "
                    + $"Klasse {erstesEreignis.ClassName ?? "-"}, "
                    + $"Integer {erstesEreignis.Integer?.ToString() ?? "-"}, "
                    + $"Link {erstesEreignis.Link?.ToString() ?? "-"}");
                System.Console.WriteLine(
                    "    (alle Schluessel des Events-Hash: "
                    + string.Join(" ", AlleErste6(wert.Items[i].Keys))
                    + ")");
                for (var q = 0; q < erstesEreignis.Keys.Count
                    && q < erstesEreignis.Items.Count; q++)
                {
                    System.Console.WriteLine(
                        $"      {erstesEreignis.Keys[q]} = "
                        + Beschreibung(erstesEreignis.Items[q], 0));
                }

                foreach (var schluessel2 in erstesEreignis.Keys)
                {
                    if (schluessel2 != "@pages")
                    {
                        continue;
                    }

                    var seitenFeld = -1;
                    for (var q = 0; q < erstesEreignis.Keys.Count; q++)
                    {
                        if (erstesEreignis.Keys[q] == "@pages")
                        {
                            seitenFeld = q;
                            break;
                        }
                    }

                    if (seitenFeld < 0)
                    {
                        continue;
                    }

                    var seiten = erstesEreignis.Items[seitenFeld];
                    System.Console.WriteLine(
                        $"    Seiten: {seiten.Items.Count}");
                    if (seiten.Items.Count == 0)
                    {
                        continue;
                    }

                    var seite = seiten.Items[0];
                    System.Console.WriteLine(
                        "    Seite 0: " + seite.Keys.Count + " Felder, Klasse "
                        + (seite.ClassName ?? "-"));
                    for (var k = 0; k < seite.Keys.Count; k++)
                    {
                        if (seite.Keys[k] != "@list")
                        {
                            continue;
                        }

                        var liste = seite.Items[k];
                        System.Console.WriteLine(
                            "      @list: Art " + liste.Kind + ", "
                            + liste.Items.Count + " Befehle");
                        for (var c = 0; c < liste.Items.Count
                            && c < 6; c++)
                        {
                            var befehl = liste.Items[c];
                            System.Console.WriteLine(
                                $"        Befehl {c}: Art {befehl.Kind}"
                                + $", Klasse {befehl.ClassName ?? "-"},"
                                + $" {befehl.Items.Count} Felder");
                            for (var f = 0; f < befehl.Keys.Count
                                && f < befehl.Items.Count; f++)
                            {
                                System.Console.WriteLine(
                                    $"          {befehl.Keys[f]} = Art "
                                    + befehl.Items[f].Kind + ", "
                                    + Beschreibung(befehl.Items[f], 2));
                            }
                        }
                    }
                    for (var k = 0; k < seite.Keys.Count
                        && k < seite.Items.Count; k++)
                    {
                        System.Console.WriteLine(
                            $"      {seite.Keys[k]} = Art "
                            + seite.Items[k].Kind + ", "
                            + Beschreibung(seite.Items[k], 0));
                    }
                }
            }
        }

        AssertTrue(true,
            "**and the shape is in the log** -- and this test asserts"
                + " nothing about it on purpose, because its whole job is"
                + " the measurement");
    }

    /// <summary>
    /// One line about a value, deep enough to place it.
    /// </summary>
    /// <param name="pWert">The value.</param>
    /// <param name="pTiefe">How far down to go.</param>
    /// <returns>A line, and never an exception.</returns>
    /// <remarks>
    /// <strong>And this stops at four levels</strong>, -- <strong>and a
    /// page's command list is three levels down and its first command
    /// four</strong>, -- <strong>and stopping there keeps one line per
    /// field instead of one line per command of a 200 command
    /// page.</strong>
    /// </remarks>
    private static System.Collections.Generic.IReadOnlyList<string>
        AlleErste6(System.Collections.Generic.IReadOnlyList<string> pListe)
    {
        var heraus = new System.Collections.Generic.List<string>();
        for (var i = 0; i < pListe.Count && heraus.Count < 6; i++)
        {
            heraus.Add(pListe[i]);
        }

        return heraus;
    }

    private static string Beschreibung(MarshalValue pWert, int pTiefe)
    {
        if (pTiefe >= 6)
        {
            return pWert.Items.Count + " Elemente";
        }

        if (pWert.Text != null)
        {
            return "\"" + pWert.Text + "\"";
        }

        if (pWert.Integer.HasValue)
        {
            return pWert.Integer.Value.ToString(
                System.Globalization.CultureInfo.InvariantCulture);
        }

        if (pWert.Items.Count == 0)
        {
            return "";
        }

        var teile = new System.Collections.Generic.List<string>();
        var n = Math.Min(pWert.Items.Count, 6);
        for (var i = 0; i < n; i++)
        {
            teile.Add(Beschreibung(pWert.Items[i], pTiefe + 1));
        }

        return "[" + string.Join(" ", teile)
            + (pWert.Items.Count > n ? ", ..." : "") + "]";
    }
}
