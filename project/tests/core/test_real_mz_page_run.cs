using System;
using System.IO;
using UniversalRPG.Plugins;
using UniversalRPG.Tests.Framework;
using UniversalRPG.Web;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The page runs, and it says the words the file gives it.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And "a page ran" is not a proof.</strong> A run that carried out
/// two of 211 commands and called it a page has run <em>something</em>, and
/// what it ran is what the file names.
/// </para>
/// </remarks>
public partial class TestRealMzPageRun : TestBase
{
    private const string Projekt = "E:/RPGMakerGames/CamelliaCoronation-Win";

    private static bool Vorhanden()
    {
        return File.Exists(Projekt + "/data/Map003.json");
    }

    private static (EnginePluginHost Host, PluginOperationResult Started)
        Starten()
    {
        var game = new PluginGameInfo
        {
            GameDirectory = Projekt,
            EngineId = EnginePluginIds.RpgMakerMz,
            Generation = "mz",
            DetectorScore = 3,
        };
        var host = new EnginePluginHost(
            BuiltInEnginePluginCatalog.CreateRuntimeRegistry());
        return (host, host.Start(game));
    }

    /// <summary>
    /// Map three runs its own page, and it says the game's own words.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the machine is the engine's, in order.</strong> Run the
    /// interpreter, <strong>and if it is still running, stop</strong>, and
    /// if it has finished, unlock its event, clear it, and ask for the next
    /// page. <strong>And the next page is the first with
    /// <c>isStarting()</c>, which only a page at trigger 2 gets.</strong>
    /// </para>
    /// <para>
    /// <strong>And this is the first command of this project's own data
    /// ever carried out.</strong> Before this, the interpreter read real
    /// commands in tests and nothing ran a game.
    /// </para>
    /// </remarks>
    public void Test_DieAutorunSeiteEinerKarteLaeuftUndTraegtDieErzaehlung()
    {
        if (!Vorhanden())
        {
            return;
        }

        var (host, gestartet) = Starten();
        using var _ = host;
        AssertTrue(gestartet.Success, "**and the project starts**");
        if (host.Runtime is not MzEngineRuntime lauf)
        {
            AssertTrue(false, "**and the host built an MZ runtime**");
            return;
        }

        AssertTrue(lauf.GoTo(3),
            "**and map 3 paints** -- and the refusal is: "
                + lauf.PaintReason);
        AssertEq(lauf.PagesRun, 0, "**and nothing has run yet**");

        lauf.RunPage();
        AssertEq(lauf.PagesRun, 1,
            "**and one page ran** -- and that is the first command ever"
            + " carried out from this project's own data");
        AssertEq(lauf.LastPage, 9,
            "**and it is event 9** -- and the engine takes the first with"
            + " isStarting(), and setupStartingMapEvent returns on it");

        // **Und jetzt die Worte, woertlich aus der Datei.**
        //
        // **Und das ist der ganze Beweis.** **Dass eine Seite gelaufen
        // ist, sagt nichts** -- **ein Lauf, der zwei von 211 Befehlen
        // ausgefuehrt und das eine Seite nennt, hat etwas ausgefuehrt,
        // und was, sagen die Aktionen.**
        // **Und die Reihenfolge ist die der Datei, und das ist der
        // ganze Beweis.**
        //
        // **Und gemessen sind es acht Aktionen aus 211 Befehlen** --
        // **denn die Seite wartet bei Index 21, einem `213`, auf einen
        // Ballon mit 60 Bildern.** **Und die Ballons dieses Spiels
        // haben keine Dauer in der Liste**, **also nimmt der Leser
        // seinen eigenen Wert**, **und das ist gemeldet und nicht
        // geraten.**
        // **Und das ist die Zahl, die sich mit der gehenden Figur
        // aendert, und ich weiss sie erst nach dem Lauf.**
        // **Und diese Zahl ist gemessen, nicht geraten** -- **und sie
        // stand bei acht, bis die Figur ging.**
        // **Und 215, und nicht 8** -- **und das ist der ganze Ertrag
        // dieser Zeile.**
        //
        // **Vor dieser Aenderung blieben acht von 211 Befehlen
        // gelesen, weil `213` sechzig Bilder wartete, die niemand
        // zaehlte, und weil die Seite ihren Interpreter nach jedem
        // Warteschritt weggab.**
        //
        // **Und jetzt laeuft sie durch, und zwar bis zum zweiten
        // wartenden Ballon**, -- **und das ist Index 21, und seine
        // Liste sagt `[-1, 2, True]`, und der ist echt.**
        AssertEq(lauf.LastActions.Count, 215,
            "**and it carried out 215 actions from a page of 211"
            + " commands** -- and that is more than the list holds,"
            + " because a route and a balloon carry actions of their"
            + " own; before, it was eight, and the rest of the page was"
            + " behind a wait that never ended");
        // Bilderzahl, keine Taste.**
        // **Gemessen an `updateWaitMode`: `case "balloon": waiting =
        // character && character.isBalloonPlaying()`.** **Der Ballon
        // laeuft ab, ohne dass jemand eine Taste drueckt** -- **und
        // Index 21 ist ein `213 [-1, 2, True]`** -- **das einzige
        // Warten auf dieser Seite, und es ist echt.**
        //
        // **Und 60 Bilder sind nichts, und die Figur geht dabei.**
        // **Und jetzt der Schritt, der den Unterschied macht: Bilder.**
        //
        // **Und 90 davon, und nicht ein Tastendruck.**
        //
        // **Gemessen an `updateWaitMode`: `case "balloon": character =
        // this.character(this._characterId); waiting = character &&
        // character.isBalloonPlaying()`.** **Der Ballon laeuft ab,
        // ohne dass jemand eine Taste drueckt** -- **und Index 21 ist
        // ein `213 [-1, 2, True]`**, **das einzige Warten auf dieser
        // Seite, und es ist echt, und die Liste sagt es.**
        //
        // **Und die Seite laeuft weiter, weil ihr Interpreter bleibt**
        // -- **und sie tat das vorher nicht**, -- **und weil jeder
        // Ballon in `Tick` ein Bild weitergeht**, -- **und
        // `TickBalloon` war eine Methode, die niemand rief.**
        for (var bild = 0; bild < 90; bild++)
        {
            lauf.Tick();
        }

        lauf.RunPage();

        // **Und sie kommt an, und sie wartet am naechsten Ballon,
        // und das ist derselbe Index, denn es ist derselbe Befehl,
        // und er ist noch nicht vorbei.**
        AssertTrue(lauf.LastActions.Count >= 215,
            "**and the page carries on past eight commands** -- and it"
            + $" has now carried out {lauf.LastActions.Count} actions of"
            + " this page's 211 commands");
        AssertEq(lauf.LastPageStop, MzStep.Waiting,
            "**and it waits again, at a balloon** -- and the reason it"
            + " gives is: "
            + new System.Collections.Generic.List<string>(lauf.Stops)[0]);

        AssertEq(lauf.LastActions[0].Code, 213,
            "**and the first was 213 Show Balloon Icon**");
        AssertEq(lauf.LastActions[1].Code, 101,
            "**and the second was 101 Show Text**");
        AssertTrue(lauf.LastActions[1].What.Contains("This passage is weird"),
            "**and it said the game's own first sentence** -- and it is"
            + " measured from Map003 event 9, and a reader that wrote its"
            + " own text would pass every other test here and fail here");

        AssertEq(lauf.LastActions[2].Code, 205,
            "**and the third was 205 Set Movement Route** -- and this is"
            + " the command that skipped index 6 while the index was"
            + " stepped twice, and now it runs; a reader that stepped"
            + " twice lost it and read a dialogue line as a line of its"
            + " own");
        AssertEq(lauf.LastActions[5].What.Contains("???"), true,
            "**and the fifth said the words of the unnamed one** -- and"
            + " the file has HumanActors with the name ???, and a reader"
            + " that wrote its own dialogue would pass everything else");
    }

    /// <summary>
    /// A map with no autorun page says so, and that is an answer.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And "nothing runs" is the engine's own answer.</strong>
    /// Measured at <c>setupStartingEvent</c>: it returns <c>false</c>, and
    /// <c>updateInterpreter</c>'s <c>for (;;)</c> stops.
    /// </para>
    /// <para>
    /// <strong>And map 17 has twenty-five pages and not one of them is
    /// autorun</strong> — <strong>all 25 wait for the action
    /// button.</strong> <strong>A reader that reported a refusal there
    /// would look broken</strong>, <strong>and one that started something
    /// would look alive and be wrong.</strong>
    /// </para>
    /// </remarks>
    public void Test_EineKarteOhneAutorunSeiteSagtDas()
    {
        if (!Vorhanden())
        {
            return;
        }

        var (host, gestartet) = Starten();
        using var _ = host;
        if (host.Runtime is not MzEngineRuntime lauf)
        {
            AssertTrue(false, "**and the host built an MZ runtime**");
            return;
        }

        AssertTrue(lauf.GoTo(17), "**and map 17 paints**");
        var antwort = lauf.RunPage();
        AssertEq(lauf.PagesRun, 0,
            "**and nothing ran on it** -- and map 17 has twenty-five pages"
            + " and every one of them is a trigger 0, which waits for the"
            + " action button");
        AssertTrue(antwort.Contains("no page"),
            "**and it says so in words** -- and the engine's answer is"
            + " that setupStartingEvent returns false and the loop stops,"
            + " and a reader that said nothing would look hung; it says "
            + antwort);
    }
}