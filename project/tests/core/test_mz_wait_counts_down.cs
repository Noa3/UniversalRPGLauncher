using System.Collections.Generic;
using System.IO;
using UniversalRPG.Plugins;
using UniversalRPG.Tests.Framework;
using UniversalRPG.Web;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// One page, one wait, and the frames after it.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And three tests failed with the same word in them</strong>, --
/// <strong>"waiting 24 frames"</strong>, -- <strong>and the index stood
/// still at every one of them.</strong>
/// </para>
/// <para>
/// <strong>And this test asks the only question that matters</strong>:
/// does a page that waits twenty-four frames count them down and go on?
/// </para>
/// </remarks>
public partial class TestMzWaitCountsDown : TestBase
{
    private const string Projekt = "E:/RPGMakerGames/CamelliaCoronation-Win";

    /// <summary>
    /// And what it says when it does.
    /// </summary>
    public void Test_EineSeiteDieWartetZaehltDieBilderUndGehtWeiter()
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

        AssertTrue(lauf.GoTo(3),
            "**and map 3 paints** -- and it says: " + lauf.PaintReason);

        var spur = new List<string>();

        // **Und die Seite dieser Karte wird BETRETEN, und nicht
        // automatisch gestartet.**
        //
        // **Denn Map003 hat keine Autorun-Seite** -- gemessen: seine
        // Ausloeser sind `{0: 9, 1: 1, 2: 2}`, **und keine einzige 3.** Die
        // beiden grossen Erzaehlseiten (Ereignis 9 und 10, je 211 Befehle)
        // tragen **`trigger 2`**, und den startet der Motor ueber
        // `isTriggerIn([1, 2])`, wenn der Spieler auf ihrer Kachel
        // **ankommt**.
        //
        // **Und hier stand `lauf.RunPage();`**, das den Ausloeser 2 als
        // Autorun las, **weil die Nummern im Leser um eins verschoben
        // waren.**
        lauf.Betrete(6, 7);
        spur.Add($"Start: Index {lauf.LastPageIndex}, {lauf.LastPageStop}");

        // **Und jetzt Bilder, und sonst nichts.**
        //
        // **Und das ist der Motorweg** -- **denn
        // `Game_Map.prototype.update` ruft `this._interpreter.update()`
        // jedes Bild.**
        for (var bild = 0; bild < 400; bild++)
        {
            // **Und ein Dialog wartet auf einen Tastendruck** -- **und
            // `Tick` drueckt keinen** -- **und darum wird hier jede
            // Runde ein Bild getickt und danach weitergeblattert.**
            lauf.Tick();
            if (bild % 60 == 0)
            {
                spur.Add($"{bild}:{lauf.LastPageIndex}");
            }

            // **Und `RunPage` setzt die wartende Seite fort** -- **denn
            // es nimmt den Interpreter aus `Laeufer`**, -- **und es
            // drueckt fuer die Dialoge `Ok`.**
            lauf.RunPage();
        }

        System.Console.WriteLine(
            "Wartezaehlung: " + string.Join(" ", spur));
        // **Und 210, und nicht 211, und das ist gemessen.**
        //
        // **Die Seite hat 211 Befehle, und Index 210 ist ein `0`** --
        // **das Listenende** -- **und der Index, den ein Interpreter an
        // zeigt, ist der, auf dem er als naechstes liest.** -- **und
        // 211 hiesse, er laesst die Liste am Ende noch einmal lesen.**
        //
        // **Und Index 201 war der Stand, bevor dieser Test getickt
        // hat** -- **und 201 ist ein `105 Scroll Text`**, -- **und
        // davor steht ein `221 Fadeout Screen`**, -- **und das sind
        // vierundzwanzig Bilder**, -- **und danach sind noch neun
        // Befehle.**
        AssertEq(lauf.LastPageIndex, 210,
            "**and the page reads to its end** -- and it stands at "
            + lauf.LastPageIndex + ", and it started at 201, and index"
            + " 210 is the `0` that ends the list");
    }

    /// <summary>
    /// And the count itself, without a game at all.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And <c>222</c> asks for <c>fadeSpeed()</c> and that is
    /// <c>24</c></strong>, and the count belongs to the interpreter, and
    /// <strong>a page that asks for twenty-four frames must be able to
    /// pass them.</strong>
    /// </para>
    /// </remarks>

    /// <summary>
    /// And a page that ends right after a wait says so.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the finding</strong>, and a real page ran into
    /// it: <strong>a <c>222</c> at the end of a list waits twenty-four
    /// frames, and the list has one command behind it</strong>, and
    /// <strong>after those frames the interpreter must read that command
    /// and stop.</strong>
    /// </para>
    /// <para>
    /// <strong>And the third real page of this repository stopped there
    /// with its index on the last command and its state still
    /// <c>Waiting</c></strong> -- <strong>and the number of commands it
    /// had read was right and its answer was wrong.</strong>
    /// </para>
    /// </remarks>
    public void Test_EineSeiteDieMitWartenAufhoertSagtDasAuch()
    {
        var liste = new List<MzCommandEntry>
        {
            new(222, [], 0),
            new(0, [], 0),
        };
        var fakten = new MzBranchFacts();
        var lauf = new MzEventRunner()
            .Run(liste, fakten, 1, 1, new MzRandom());

        AssertEq(lauf.Stopped, MzStep.Waiting,
            "**and it waits** -- and it is " + lauf.Stopped);
        AssertEq(lauf.Interpreter!.Index, 1,
            "**and the index is on the `0` behind the wait** -- and it is"
            + $" at {lauf.Interpreter.Index}, and `222` says `return false`"
            + " and `this._index++` and that is the command count");

        // **Und jetzt die vierundzwanzig Bilder.**
        for (var bild = 0; bild < 24; bild++)
        {
            lauf.Interpreter.PassFrame();
        }

        var danach = new MzEventRunner().Run(
            liste, fakten, 1, 1, new MzRandom(), lauf.Interpreter);
        AssertEq(danach.Stopped, MzStep.Finished,
            "**and then the page is over** -- and it is "
            + danach.Stopped + ", and the index is "
            + danach.Interpreter!.Index + ", and a page that stands on"
            + " its last command and says `Waiting` is a page that will"
            + " stand there for ever");
    }
    public void Test_ZwanzigVierBilderSindVierUndZwanzigUndNichtMehr()
    {
        var liste = new List<MzCommandEntry>
        {
            new(222, [], 0),
            new(355, ["// nichts"], 0),
            new(0, [], 0),
        };
        var lauf = new MzInterpreter(liste);
        lauf.Setup(1, 1);
        var fakten = new MzBranchFacts();
        var ergebnis = new MzEventRunner()
            .Run(liste, fakten, 1, 1, new MzRandom());

        AssertEq(ergebnis.Stopped, MzStep.Waiting,
            "**and the page waits** -- and it is " + ergebnis.Stopped);
        AssertEq(ergebnis.WaitingFrames, 24,
            "**and it waits twenty-four frames** -- and it says "
            + ergebnis.WaitingFrames + ", and `fadeSpeed()` is 24");

        // **Und jetzt die Bilder, und einer nach dem anderen.**
        var fertigNach = -1;
        for (var bild = 1; bild <= 30; bild++)
        {
            if (ergebnis.Interpreter?.PassFrame() == true)
            {
                fertigNach = bild;
                break;
            }
        }

        AssertEq(fertigNach, 24,
            "**and exactly twenty-four frames end it** -- and it ended"
            + $" after {fertigNach}, and the engine's `updateWaitCount`"
            + " is `if (this._waitCount > 0) { this._waitCount--;"
            + " return true; }`");
        AssertEq(lauf.Index, 0,
            "**and a fresh page has waited for nothing**");
    }
}
