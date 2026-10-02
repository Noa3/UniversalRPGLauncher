using System;
using System.Collections.Generic;
using System.IO;
using UniversalRPG.Plugins;
using UniversalRPG.Tests.Framework;
using UniversalRPG.Web;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The longest waiting page of a real MV game, and how far it gets.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And MV shares this repository's MZ command set</strong> -- <c>MV
/// is a branch of the same command set and not a second one</c>, -- <strong>
/// and so the wait rule that was measured against MZ applies here without
/// a second implementation.</strong>
/// </para>
/// <para>
/// <strong>And MV's own engine agrees with MZ's</strong>, read from
/// <c>www/js/rpg_objects.js</c> of a finished game: the same twelve
/// commands set a wait, <c>101</c> through <c>105</c>, <c>201</c>,
/// <c>204</c>, <c>205</c>, <c>212</c>, <c>213</c>, <c>217</c>, <c>261</c>
/// and <c>339</c>, <strong>and the same six answer <c>true</c> and the
/// same six answer <c>false</c>.</strong>
/// </para>
/// </remarks>
public partial class TestMvParallelPageDepth : TestBase
{
    private const string Projekt =
        "E:/RPGMakerGames/Fatal Fantasy Update/Fatal Fantasy/www";

    /// <summary>
    /// Four hundred and forty-five commands and ten balloon waits.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the longest waiting page in the whole
    /// game</strong> -- measured across all five hundred and fourteen maps,
    /// <strong>and three hundred and eighty-two of them carry at least one
    /// waiting balloon.</strong>
    /// </para>
    /// <para>
    /// <strong>And ten waits of seventy-six frames each is seven hundred
    /// and sixty frames</strong>, -- <strong>and a reader that answered
    /// <c>false</c> where the engine answers <c>true</c> never gets past
    /// the first one</strong>, -- <strong>because the index stays on the
    /// <c>213</c> and the icon is set again on every frame.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieLaengsteWartendeSeiteEinesMvSpielsLaeuftDurch()
    {
        if (!File.Exists(Projekt + "/data/Map231.json"))
        {
            return;
        }

        var game = new PluginGameInfo
        {
            GameDirectory = Projekt,
            EngineId = EnginePluginIds.RpgMakerMv,
            Generation = "mv",
            DetectorScore = 3,
        };
        using var host = new EnginePluginHost(
            BuiltInEnginePluginCatalog.CreateRuntimeRegistry());
        AssertTrue(host.Start(game).Success, "**and the project starts**");
        if (host.Runtime is not MzEngineRuntime lauf)
        {
            AssertTrue(false,
                "**and the host built a runtime** -- and MV and MZ share"
                + " this repository's one; they are a branch of the same"
                + " command set and not two engines here");
            return;
        }

        // **Und diese Karte laesst sich nicht malen, und das ist nicht
        // der Grund, warum sie hier steht.**
        //
        // **Dieses Spiel liefert 245 `.rpgmvp`-Bloecke** -- **und kein
        // entpacktes Bild** -- **und `Repaint` sagt genau das:**
        // *"Every sheet of this tileset is missing."*
        //
        // **Und eine Seite braucht keine Augen** -- **und der Befehlssatz
        // ist genau dieselbe Datei, ob das Tileset da ist oder nicht.**
        var gegangen = lauf.GoTo(231, true);
        System.Console.WriteLine(
            "MV Map231: GoTo sagt " + gegangen + ", PaintReason ist \""
            + lauf.PaintReason + "\"");
        AssertTrue(gegangen,
            "**and map 231 is among the maps this runtime read** -- and"
            + " it does not paint, and that is the tileset's business and"
            + " not the page's; the refusal was: " + lauf.PaintReason);

        // **Und diese Seite ist eine parallele**, -- **trigger 3**,
        // **und die Engine gibt ihr einen eigenen Interpreter, und
        // `RunPage` nimmt nur die mit `isStarting()`.**
        //
        // **Und Map231 hat einundzwanzig Ereignisse, und zwanzig davon
        // tragen einen einzigen Befehl mit Code 0** -- **das ist eine
        // leere Seite** -- **und Event 2 ist die einzige, die etwas
        // tut**, -- **und es ist genau die mit den zehn Ballons.**
        var spur = new List<string>();
        var tiefste = 0;
        for (var ballon = 0; ballon < 120; ballon++)
        {
            for (var bild = 0; bild < 80; bild++)
            {
                lauf.Tick();
            }

            var bericht = lauf.RunParallel();
            var erreicht = GelesenerIndex(bericht);
            if (erreicht > tiefste)
            {
                tiefste = erreicht;
            }

            spur.Add($"{erreicht}");
            if (bericht.Count == 0)
            {
                break;
            }
        }

        System.Console.WriteLine(
            "MV Map231 Event 2: " + string.Join(" ", spur));
        // **Und 440, und nicht 445, und das ist gemessen.**
        //
        // **Index 440 ist ein `201 [0, 232, 8, 8, 0, 2]`** -- **ein
        // Kartenwechsel auf Karte 232** -- **und dieser Leser fuehrt ihn
        // aus und gibt die Seite danach frei.**
        //
        // **Und dahinter stehen nur noch drei Befehle:** ein `101`, eine
        // `401` mit dem Wort `ALEX: ENOUGH!` und ein `230 [120]`, und
        // dann `code 0`.
        //
        // **Und `code 0` ist das Listenende**, -- **und eine Seite, die
        // dort ankommt, ist zu Ende gelaufen**, -- **und die 445 Befehle
        // enthalten diesen Abschluss, aber nicht mehr Inhalt.**
        //
        // **Und davor liegen 439 Befehle mit sechzehn `230 [60]`,
        // zehn wartenden Ballons und einem Bildschirmblitz** -- **und das
        // ist der ganze Ertrag dieser Seite.**
        AssertEq(tiefste, 440,
            "**and it carries the page to its map change** -- and it"
            + $" reached {tiefste}, and index 440 is a `201` that hands"
            + " the page over to map 232, and a reader that answered false"
            + " where the engine answers true stood still at index 9");
    }

    /// <summary>
    /// How far a report says the page got.
    /// </summary>
    /// <param name="pBericht">What <c>RunParallel</c> said.</param>
    /// <returns>The highest command index any page reached.</returns>
    /// <remarks>
    /// <strong>And the report says it outright</strong> -- "and it reached
    /// command 130 of 176" -- <strong>and a reader that counts the round's
    /// own actions reads a number that is not the page's progress</strong>,
    /// <strong>because every round starts where the last one stopped.</strong>
    /// </remarks>
    private static int GelesenerIndex(IReadOnlyList<string> pBericht)
    {
        var hoechste = 0;
        foreach (var kette in pBericht)
        {
            const string kMarke = "and it reached command ";
            var stelle = kette.IndexOf(kMarke, StringComparison.Ordinal);
            if (stelle < 0)
            {
                continue;
            }

            var lese = new List<char>();
            for (var k = stelle + kMarke.Length; k < kette.Length; k++)
            {
                if (!char.IsDigit(kette[k]))
                {
                    break;
                }

                lese.Add(kette[k]);
            }

            if (lese.Count > 0)
            {
                var eine = int.Parse(new string(lese.ToArray()));
                if (eine > hoechste)
                {
                    hoechste = eine;
                }
            }
        }

        return hoechste;
    }
}
