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
        AssertEq(lauf.LastActions.Count, 8,
            "**and it carried out eight commands** -- and the ninth is a"
            + " 213 that waits for a balloon, and the page has 211"
            + " commands in all, and the rest are behind that wait");
        AssertEq(lauf.LastPageStop, MzStep.Waiting,
            "**and it waits at the balloon** -- and a balloon this project"
            + " gives no length in its list, so the reader takes its own"
            + " and says so; it says "
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