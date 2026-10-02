using System;
using System.Collections.Generic;
using System.IO;
using UniversalRPG.Plugins;
using UniversalRPG.Tests.Framework;
using UniversalRPG.Web;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// How far one real parallel page gets, measured and not estimated.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And a page of this game has 176 commands</strong>, and the
/// reader has to be asked again and again how many it carried out, because
/// "it waits somewhere" is not a statement about how much it did.
/// </para>
/// <para>
/// <strong>And this test prints the number</strong>, so a change that makes
/// it smaller is visible in the log and not only in a failed assertion.
/// </para>
/// </remarks>
public partial class TestMzParallelPageDepth : TestBase
{
    private const string Projekt = "E:/RPGMakerGames/CamelliaCoronation-Win";

    /// <summary>
    /// The page reads its commands until it really cannot go on.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the loop asks sixty times and ticks 240 frames
    /// between</strong>, -- and 240 frames is three balloons of
    /// <c>8 * 8 + 12 = 76</c>, -- and a page that is still going after
    /// that is going slowly and not stuck.
    /// </para>
    /// </remarks>
    public void Test_DieParalleleSeiteLiestSoVielWieSieKann()
    {
        if (!File.Exists(Projekt + "/data/Map003.json"))
        {
            return;
        }

        var game = new PluginGameInfo
        {
            GameDirectory = Projekt,
            EngineId = EnginePluginIds.RpgMakerMz,
            Generation = "mz",
            DetectorScore = 3,
        };
        using var host = new EnginePluginHost(
            BuiltInEnginePluginCatalog.CreateRuntimeRegistry());
        AssertTrue(host.Start(game).Success, "**and the project starts**");
        if (host.Runtime is not MzEngineRuntime lauf)
        {
            AssertTrue(false, "**and the host built an MZ runtime**");
            return;
        }

        AssertTrue(lauf.GoTo(5),
            "**and map 5 paints** -- and the refusal is: "
                + lauf.PaintReason);

        var tiefste = 0;
        var bericht = string.Empty;
        // **Und 240 Bilder zwischen zwei Abfragen sind zu viel**, -- und
        // **das ist der Befund und nicht ein Fehler**, -- **denn die Seite
        // wartet auf einen Ballon, und der Ballon braucht 76 Bilder, und
        // sie fragt einmal je 240** -- **und also liest sie in grossen
        // Spruengen und sieht ihren eigenen Fortschritt nicht.**
        var spur = new System.Text.StringBuilder();
        for (var mal = 0; mal < 60; mal++)
        {
            var jetzt = lauf.RunParallel();
            if (jetzt.Count == 0)
            {
                break;
            }

            bericht = string.Join(" | ", jetzt);
            var gelesen = 0;
            foreach (var kette in jetzt)
            {
                var eine = GeleseneBefehle(kette);
                if (eine > gelesen)
                {
                    gelesen = eine;
                }
            }

            if (gelesen > tiefste)
            {
                tiefste = gelesen;
            }

            if (mal < 6)
            {
                lauf.EventFigures.TryGetValue(3, out var f3);
                lauf.EventFigures.TryGetValue(15, out var f15);
                spur.Append(" ").Append(mal).Append("->").Append(gelesen)
                    .Append("(E3=").Append(f3 != null ? f3.BalloonFramesLeft : -1)
                    .Append(" E15=").Append(f15 != null ? f15.BalloonFramesLeft : -1)
                    .Append(" S=").Append(
                        lauf.Facts.Player.Figur?.BalloonFramesLeft ?? -1)
                    .Append(')');
            }

            for (var bild = 0; bild < 80; bild++)
            {
                lauf.Tick();
            }
        }

        System.Console.WriteLine("Spur:" + spur);
        System.Console.WriteLine(
            "Parallele Seite: " + tiefste + " Befehle gelesen von 176"
            + ", und der Bericht ist: " + bericht);
        AssertEq(tiefste, 176,
            "**and it reads all hundred and seventy-six** -- and it"
            + $" read {tiefste}, and 130 was where it stood while"
            + " `213` answered false where the engine answers true");
    }

    /// <summary>
    /// How many commands a report says were carried out.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And <c>Describe()</c> writes it in a fixed shape</strong> --
    /// <c>"waiting 0 frames at code 213, 4 commands so far"</c> -- <strong>and
    /// a reader that guesses the number out of that sentence breaks the
    /// day the sentence is reworded</strong>, and so this walks back from
    /// <c>" commands so far"</c> to the comma in front of it.
    /// </strong>
    /// </para>
    /// </remarks>
    private static int GeleseneBefehle(string pKette)
    {
        // **Und der Bericht sagt: "and it reached command 130 of 176"**
        // -- **und das ist der Index des Interpreters**, -- **und nicht
        // die "4 commands so far" derselben Zeile**, -- **denn die
        // zaehlt nur den einen Durchgang**, -- **und jeder Durchgang
        // beginnt dort, wo der vorige aufhoerte.**
        const string kMarke = "and it reached command ";
        var stelle = pKette.IndexOf(
            kMarke, StringComparison.Ordinal);
        if (stelle < 0)
        {
            return 0;
        }

        var lese = new List<char>();
        for (var k = stelle + kMarke.Length; k < pKette.Length; k++)
        {
            if (!char.IsDigit(pKette[k]))
            {
                break;
            }

            lese.Add(pKette[k]);
        }

        return lese.Count > 0 ? int.Parse(new string(lese.ToArray())) : 0;
    }
}
