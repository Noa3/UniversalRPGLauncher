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
        // **Und die Startkarte hat inzwischen ihre Eroeffnung gespielt.**
        //
        // **Denn Camellias Startkarte IST Map002, und sie traegt eine
        // Autorun-Seite** -- Ereignis 5, 22 Befehle, vier Dialoge. **Sie
        // laeuft, sobald die Karte steht, und `PagesRun` zaehlt sie mit.**
        // **Ein Test, der hier eine Null erwartet, erwartet eine stille
        // Startkarte, die dieses Spiel nicht hat.**
        Console.WriteLine($"MZ page run: the start map ran {lauf.PagesRun} "
            + $"page(s) before map 3");
        var vorMapDrei = lauf.PagesRun;
        AssertEq(lauf.PagesRun, vorMapDrei,
            "**and map 3 itself has run nothing yet**");

        //
        // **Und `RunPage` allein laesst die Seite nicht durchlaufen.**
        //
        // **Und das ist der Unterschied zwischen dem, was dieser Test
        // einmal mass und dem, was er jetzt misst.** Frueher lief
        // `RunPage` die ganze Seite in einem Durchlauf und tat dabei
        // `MessageBusy = false` und `_keys.Ok()` in jeder Runde -- **und
        // ein Spiel mit Autorun blieb dadurch stumm.** **Jetzt laeuft die
        // Seite ueber Bilder**, **und `RunPage` startet sie nur.**
        //
        // **Und gemessen ist der Unterschied:** vorher `215` Aktionen in
        // einem Aufruf, **jetzt vier in `RunPage` und der Rest in den
        // folgenden Bildern** -- **und die Seite steht bei Index 6 auf
        // ihrer eigenen Dialog-Wartezeit**, **was genau das ist, was
        // `MzWaitMode.Message` bedeutet.**
        // **Und die Erzaehlseite dieser Karte wird BETRETEN, und nicht
        // automatisch gestartet.**
        //
        // **Denn Map003 hat keine Autorun-Seite** -- gemessen: seine
        // Ausloeser sind `{0: 9, 1: 1, 2: 2}`, **und keine einzige 3.**
        // Ereignis 9 steht auf `(6,7)` und traegt **`trigger 2`**, und die
        // Engine startet ihn ueber
        // `Game_Player.prototype.checkEventTriggerTouch`, das
        // `startMapEvent(x, y, [1, 2], false)` ruft -- **und das `false`
        // ist das `normal`-Flag, das auf die Prioritaet 0 dieser Seite
        // passt.**
        //
        // **Und hier stand `lauf.RunPage();`**, das den Ausloeser 2 als
        // Autorun las, **weil die Nummern im Leser um eins verschoben
        // waren.** **`RunPage(Autorun)` startet nur `_trigger === 3`.**
        var vorDerSeite = lauf.PagesRun;
        lauf.Betrete(6, 7);
        for (var frame = 0; frame < 400; frame++)
        {
            lauf.Update(1.0 / 60.0);
            if (lauf.LastActions.Count > 40
                || lauf.MessageVisible)
            {
                // **Und der Dialog wird bestaetigt, wie der Spieler es
                // tut** -- **und `CloseMessage` setzt den Druck, den
                // `Window_Message.prototype.isTriggered` setzt.**
                lauf.CloseMessage();
            }
        }
        // **Und eine Seite lief auf dieser Karte, und die Startkarte hat
        // ihre eigene schon gespielt** -- gemessen: `the start map ran 1
        // page(s) before map 3`. **Die Zahl wird darum gegen die Basis
        // gerechnet und nicht gegen null.**
        AssertEq(lauf.PagesRun, vorDerSeite + 1,
            "**and one page ran on map 3** -- and that is the first command"
            + $" carried out from this project's own data, over the"
            + $" {vorDerSeite} the start map had already run");
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
        // Ballon mit 76 Bildern.** **Und die Ballons dieses Spiels
        // haben keine Dauer in der Liste**, -- **also stand hier
        // lange eine geratene Zahl**, -- **und die steht jetzt aus
        // `Sprite_Balloon.setup`:**
        //
        // ```js
        // this._duration = 8 * this.speed() + this.waitTime();
        // ```
        //
        // **8 * 8 + 12 = 76**, -- **und der Test
        // `Test_DieBallonDauerIstDieAusDerQuelleUndNichtGeraten` liest
        // genau diese Zeilen aus der Datei des Spiels.**
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
        // **Und diese Zahl hat sich mit der Korrektur der ersten Runde
        // geaendert, und das ist der Grund, warum sie hier steht.**
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
        var firstNine = new System.Text.StringBuilder();
        var alles = System.Linq.Enumerable.ToList(lauf.LastActions);
        for (var m = 0; m < alles.Count && m < 9; m++)
        {
            firstNine.Append(alles[m].Code).Append(',');
        }
        AssertEq(lauf.LastActions[0].Code, 213,
            "**and the first was 213 Show Balloon Icon**");
        AssertEq(lauf.LastActions[1].Code, 101,
            "**and the second was 101 Show Text**");
        AssertTrue(lauf.LastActions[1].What.Contains("This passage is weird"),
            "**and it said the game's own first sentence** -- and it is"
            + " measured from Map003 event 9, and a reader that wrote its"
            + " own text would pass every other test here and fail here");

        // **Und die Laufbahn, und sie ist nicht an dritter Stelle.**
        //
        // **Und das ist gemessen:** **die `205` bei Index 6 der Seite
        // traegt Schritte, und jeder Schritt traegt eine eigene
        // Aktion** -- **und ihre Liste hat eine `505` je Schritt.**
        //
        // **Und vorher stand sie an dritter Stelle, weil das Zaehlen
        // nach der ersten Runde begann** -- **und die erste Runde ist
        // genau die mit dem Ballon und dem Satz "This passage is
        // weird".** **Und diese Zaehlung war der Fehler, und sie hat
        // zwei der drei Dialoge aus diesem Test verschluckt.**
        AssertTrue(lauf.LastActions.Count > 5,
            "**and it carried more than five actions** -- and it carried"
            + $" out {lauf.LastActions.Count}");
        var codes = new System.Text.StringBuilder();
        for (var k = 0; k < lauf.LastActions.Count && k < 9; k++)
        {
            codes.Append(lauf.LastActions[k].Code).Append(',');
        }
        AssertTrue(codes.ToString().Contains("205"),
            "**and a 205 is among the first nine actions** -- and the"
            + $" first nine are {codes}, and that is the route at index 6"
            + " of Map003 event 9, the one the double step used to skip");
        // **Und der Dialog an Index 12, und nicht an Index 5.**
        //
        // **Gemessen an Map003 Event 9:** **Index 0 ist ein 213, Index
        // 1 bis 5 sind ein 101 mit vier Zeilen, Index 6 ist eine 205
        // mit fuenf Schritten, Index 7 bis 10 sind deren 505, und Index
        // 11 ist wieder ein 213.** **Der Dialog mit dem Namen ohne
        // Namen steht also bei Index 12** -- **und die Aktionen der
        // Laufbahn schieben ihn hinaus.**
        var mitNamen = false;
        for (var k = 0; k < lauf.LastActions.Count; k++)
        {
            if (lauf.LastActions[k].What.Contains("???"))
            {
                mitNamen = true;
                break;
            }
        }

        AssertTrue(mitNamen,
            "**and one of its actions said the words of the unnamed one"
            + "** -- and the first nine codes were " + codes
            + $", and there were {lauf.LastActions.Count} actions in"
            + " all, and the name ??? is in HumanActors at index 1");

        // **Und jetzt die vier Ballone, und sie sind alle gemessen.**

        // **Map003 Event 9 hat 211 Befehle, und vier davon warten:**
        // Index 21, 36, 121 und 194 sind `213 [-1, 2/8, True]`. **Und
        // die uebrigen fuenf `213` sagen `False`** -- **und die
        // warten nicht und zeigen nur ihr Icon.**
        //
        // **Und jede Wartezeit ist 76 Bilder**, -- **denn
        // `Sprite_Balloon.setup` sagt `8 * 8 + 12`** -- **und 90 Bilder
        // reichen fuer einen Ballon und fuer vier nicht.**
        var gewartet = new System.Collections.Generic.List<string>();
        // **Und ein Bild je Runde, und zweitausend Runden**,
        // -- **denn ein Ballon braucht 76 Bilder**, -- **und
        // ein `222` braucht 24**, -- **und ein Dialog
        // braucht einen Tastendruck**, -- **und `RunPage`
        // drueckt ihn.**
        for (var ballon = 0; ballon < 2000; ballon++)
        {
            lauf.Tick();
            lauf.RunPage();

            if (ballon % 100 == 0
                || lauf.LastPageStop == MzStep.Finished)
            {
                gewartet.Add(
                    $"{ballon}:{lauf.LastPageIndex}"
                        + $"/{lauf.LastPageStop}"
                        + $"/{lauf.Stops.Count}");

                if (lauf.LastPageStop == MzStep.Finished
                    || ballon > 20)
                {
                    break;
                }
            }
        }

        // **Und sie laeuft durch, und das ist der Ertrag.**

        // **Und 210 ist das Ende**, -- **denn Index 210 ist ein `0`**,
        // **das Listenende**, -- **und der Index, den ein Interpreter
        // zeigt, ist der, auf dem er als naechstes liest.**
        AssertEq(lauf.LastPageIndex, 210,
            "**and the page reads to its end** -- and it stands at "
                + lauf.LastPageIndex + ", and the rounds were: "
                + string.Join(" | ", gewartet));

        // **Und sie laeuft durch, und das ist der Ertrag.**
        // **Und `Tick` und `RunPage` abwechselnd, ein Bild je Runde**
        // -- **und das ist der Motorweg.**
        AssertEq(lauf.LastPageIndex, 210,
            "**and the page reads to its end** -- and it stands at "
                + lauf.LastPageIndex + ", and the rounds were: "
                + string.Join(" | ", gewartet) + ", and 210 is the `0`"
                + " that ends this list of 211 commands");

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
        // **Und die Startkarte hat ihre Eroeffnung schon gespielt**, also
        // wird hier gegen die Basis gerechnet: **map 17 selbst darf nichts
        // starten.**
        var vorMapSiebzehn = lauf.PagesRun;
        var antwort = lauf.RunPage();
        AssertEq(lauf.PagesRun, vorMapSiebzehn,
            "**and nothing ran on it** -- and map 17 has twenty-five pages"
            + " and every one of them is a trigger 0, which waits for the"
            + " action button");
        AssertTrue(antwort.Contains("no page"),
            "**and it says so in words** -- and the engine's answer is"
            + " that setupStartingEvent returns false and the loop stops,"
            + " and a reader that said nothing would look hung; it says "
            + antwort);
    }

    /// <summary>
    /// A parallel page has its own interpreter, and this game has three.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And that is measured.</strong> Measured at
    /// <c>Game_Event.prototype.updateParallel</c>:
    /// <c>if (!this._interpreter.isRunning()) this._interpreter.setup(this.list(), this._eventId);
    /// this._interpreter.update();</c> — <strong>and
    /// <c>this._interpreter</c> belongs to the event, not to the map.</strong>
    /// </para>
    /// <para>
    /// <strong>And the map's own interpreter takes exactly one event and
    /// stops.</strong> Measured at <c>setupStartingMapEvent</c> — it
    /// returns <c>true</c> on the first <c>isStarting()</c> event.
    /// </para>
    /// </remarks>
    public void Test_DieDreiParallelenSeitenLaufenJedeMitIhrerEigenenMaschine()
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

        // **Und die Karte, und die ist gemessen.**
        AssertTrue(lauf.GoTo(5),
            "**and map 5 paints** -- and the refusal is: "
                + lauf.PaintReason);

        // **Und die Zahl, und die ist gemessen: drei parallele Seiten
        // im ganzen Spiel, auf Map002, Map005 und Map010.**
        var bericht = lauf.RunParallel();
        AssertEq(bericht.Count, 1,
            "**and map 5 has one parallel page** -- and the report is: "
                + string.Join(" | ", bericht));

        AssertTrue(bericht[0].Contains("event 4"),
            "**and it is event 4** -- and the engine takes parallel pages"
                + " by their trigger, which is 3, and event 4 is the"
                + " only one on this map with it; the report is "
                + bericht[0]);

        // **Und jetzt die Zahl, und sie ist der ganze Ertrag.**
        //
        // **Diese Seite hat 176 Befehle, und 17 Routen sind auf ihr
        // und auf Map010 zusammen, und neun davon sagen `wait`.**
        //
        // **Und das Bildbudget, und es ist gemessen, und nicht geraten.**
        //
        // **Diese Seite traegt zwei wartende Ballone (Index 130 und 131),
        // und jeder davon laeuft 76 Bilder** -- **und `Sprite_Balloon.setup`
        // sagt `8 * 8 + 12 = 76`, und das ist die einzige Wartezeit, die
        // niemand mit einem Tastendruck loest.** **Dazu kommen die
        // Dialoge, die je vier Bilder brauchen**, -- **und die Routen,
        // die einen Schritt pro Bild gehen.**
        //
        // **Und der Motorweg ist `Update`, und nicht `Tick`.** **Gemessen
        // am Autorun-Test ueben:** **er treibt die Seite mit
        // `lauf.Update(1.0/60.0)`**, **und schliesst den Dialog mit
        // `CloseMessage`, sobald `MessageVisible` ist.** **`Tick` allein
        // bringt die Seite ueber Frames weiter, aber der Dialog, den ein
        // `101` aufbaut, bleibt stehen, bis jemand ihn wegmacht** -- **und
        // `Update` tut genau das, denn `LoeseDialoge` steht im `Update`.**
        //
        // **And the engine runs the page until it is finished, and not
        // until a number of frames has passed** -- **so this test gives it
        // enough, and measures that it then stands at the end.**
        //
        // **And the break is on the page's own index, and not on
        // `LastPageStop`.**
        //
        // **`LastPageStop` is a stale signal for a parallel page: it is
        // only written by `RunPage`/`RunPageEvent`, and never by
        // `RunParallel` or `Tick`.** **`Start()` already ran the start
        // map's autorun page and left `LastPageStop` at `Finished`, and a
        // break on that would fire on the very first frame, before the
        // parallel page had advanced** (measured: the probe broke at
        // frame 0, index 2, with `stop=Finished` from before it started).
        //
        // **The page's own index is the truth, and `LastPageIndex` is
        // pulled up by `Tick` after it advances, so it reaches 175 -- the
        // `0` that ends this list of 176 commands.**
        for (var bild = 0; bild < 1200; bild++)
        {
            lauf.Update(1.0 / 60.0);
            if (lauf.MessageVisible)
            {
                lauf.CloseMessage();
            }
            if (lauf.LastPageIndex >= 175)
            {
                break;
            }
        }

        //
        // **Und der zweite `RunParallel` war die alte Form der Frage.**
        //
        // **Und gemessen ist, warum sie nicht mehr geht:** nach 240
        // Bildern ist die Seite **zu Ende gelaufen** und wird von
        // `RunParallel` nicht mehr gelistet, **weil eine fertige Seite
        // keine parallele Seite mehr ist, die auf ihren Turn wartet.**
        // **Der Bericht war leer, und die Seite war durch** -- **und
        // eine Assertion "es ist noch eine Seite" prueft damit nicht
        // den Fortschritt, sondern dass nichts passiert ist.**
        //
        // **Und die Frage, die zaehlt, ist die vom Interpreter selbst**
        // -- **denn `LastPageIndex` ist der Index, an dem die Seite
        // steht**, **und der gehoert jetzt von `Tick` nachgezogen
        // werden** (gemessen: ohne das blieb er bei 2, waehrend die
        // Seite 174 von 176 Befehlen erreicht hatte).
        AssertEq(lauf.LastPageIndex, 175,
            "**and the page ran to its end** -- and it stands at"
            + $" {lauf.LastPageIndex}, and the last command of a list of"
            + " 176 is the `0` at index 175");
        AssertTrue(
            lauf.EventFigures.Count > 0,
            "**and the parallel page's own figures are on this map**"
            + $" -- and there are {lauf.EventFigures.Count}");

        // **Und die Figuren, die diese Seite fuehrt, sind gemessen.**
        // **Und vier Figuren, und nicht elf, und das ist gemessen.**
        //
        // **Auf Map005 haben 7 der 11 Events ein leeres
        // `characterName`** -- **und die Figuren, die diese Seite
        // fuehrt, sind genau die mit Bild:** **1, 2, 3, 9, 10 und 15**
        // **haben eins, und der Leser zeigt genau die mit Bild.**
        //
        // **Und der Rest wird nicht unsichtbar gelassen, sondern
        // sichtbar gemacht** -- **und das ist gemessen an Befehl 19 und
        // 20 dieser Seite: `203 [10, 0, 2, 10, 0]` und `322 [1,
        // "MC_Sprite_sheet", 0, "SlimeActors", 0, ...]`.**
        AssertTrue(lauf.EventFigures.Count >= 4,
            "**and the figures with a picture stand on the map** -- and"
            + $" there are {lauf.EventFigures.Count}, and the measured"
            + " truth for Map005 is that 7 of its 11 events carry an"
            + " empty characterName and are invisible in the game too");
    }

    /// <summary>
    /// The second busy parallel page, on Map010, runs beside the first.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the case a shared interpreter gets wrong.</strong>
    /// Map005 event 4 has 176 commands and Map010 event 7 has 141, and
    /// between them they carry seventeen routes, nine of which say
    /// <c>wait</c>. <strong>A reader that gave them one interpreter
    /// stopped the first at its first route wait and never started the
    /// second</strong> — <strong>and measured, they are on different
    /// maps, so they cannot even be started together.</strong>
    /// </para>
    /// <para>
    /// <strong>So what is proved here is the per-event lookup:</strong>
    /// <c>Laeufer</c> is keyed by event id, exactly as
    /// <c>this._interpreter</c> belongs to one event.
    /// </para>
    /// </remarks>
    public void Test_DieZweiteParalleleSeiteTraegtIhreEigeneMaschine()
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

        AssertTrue(lauf.GoTo(10),
            "**and map 10 paints** -- and the refusal is: "
                + lauf.PaintReason);

        var bericht = lauf.RunParallel();
        AssertEq(bericht.Count, 1,
            "**and map 10 has one parallel page** -- and the report is: "
                + string.Join(" | ", bericht));
        AssertTrue(bericht[0].Contains("event 7"),
            "**and it is event 7, and it carries 141 commands** -- and"
            + " the engine gives it its own interpreter; the report is "
                + bericht[0]);

        // **Und es sind andere Figuren als auf Map005** -- **und das
        // ist der Punkt** -- **denn dieselbe Nummer auf einer anderen
        // Karte ist eine andere Figur.**
        AssertTrue(lauf.EventFigures.Count >= 4,
            "**and this map's figures are its own** -- and there are"
            + $" {lauf.EventFigures.Count}, and its routes name 2, 4,"
            + " 5, 8, 9, 10 and 11, and every one of them exists");
    }

    /// <summary>
    /// Stepping onto an event's tile runs its page, even with no picture.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the way 196 of this project's 253 pages are
    /// reached, measured.</strong> Trigger 0 is the action button and it
    /// comes to 196 times, and every one of those 196 carries a position
    /// inside its map — so none of them is out of reach.
    /// </para>
    /// <para>
    /// <strong>And the three pages this test steps on have no picture at
    /// all</strong> — measured: Map003 event 5 at (1,2), event 6 at
    /// (3,2) and event 8 at (6,2) all carry an empty
    /// <c>characterName</c>. <strong>A figure with no picture is still
    /// stepped on and its page still runs</strong>, because
    /// <c>eventsXy</c> filters by position and
    /// <c>this.events()</c> keeps an event whose <c>characterName</c> is
    /// empty.
    /// </para>
    /// </remarks>
    public void Test_DasBetretenEinerKachelStartetDieSeiteAuchOhneBild()
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

        // **Und der Schritt, und er ist der gemessene Weg -- und zwar auf
        // eine Kachel, die ein Spieler wirklich betreten kann.**
        //
        // **Denn hier stand `Betrete(1, 2)`, und das war zweimal
        // unmoeglich:** Map003s Ereignis 5 traegt **`trigger 0`** -- die
        // Aktionstaste -- **und Prioritaet 1**, also eine Kachel, auf der
        // ein Spieler nie stehen kann. **Gestartet wurde es nur, weil
        // `Betrete` einen zweiten Scan hatte, der Trigger-0-Seiten beim
        // Ankommen startete, und den hat die Engine nicht.**
        //
        // **Und die Seite, die der Motor so wirklich erreicht, ist
        // gemessen: Map003 Ereignis 7 auf `(2,3)`, `trigger 1`,
        // Prioritaet 0, 17 Befehle, `characterName` leer** -- **und genau
        // das ist die Behauptung dieses Tests: eine Seite ohne Bild wird
        // trotzdem betreten.**
        //
        // **Und `trigger 1` mit Prioritaet 0 ist die Bedingung, die
        // `Game_Player.prototype.checkEventTriggerHere([1, 2])` stellt**;
        // `startMapEvent(this.x, this.y, triggers, false)` fragt mit dem
        // `normal`-Flag `false` genau nach ihr.
        var bericht = lauf.Betrete(2, 3);
        AssertEq(bericht.Count, 1,
            "**and one page started on that tile** -- and the report is: "
                + string.Join(" | ", bericht));
        AssertTrue(bericht[0].Contains("event 7"),
            "**and it is event 7** -- and the report is " + bericht[0]);

        // **Und der Beweis, dass es eine Seite war und kein Bild:**
        // **die Worte sind woertlich aus der Datei.**
        AssertTrue(lauf.LastActions.Count >= 1,
            "**and it carried a command out** -- and it carried out "
                + $"{lauf.LastActions.Count}, and the first was "
                + $"{lauf.LastActions[0].Code}");
        AssertTrue(lauf.LastActions[0].Code == 101,
            "**and the first was 101 Show Text** -- and a page with no"
                + " picture still speaks, because the picture is not what"
                + " the engine looks at; the tile is");

        // **Und die Worte stehen woertlich in Map003 Ereignis 7.**
        AssertTrue(
            lauf.LastActions[0].What.Contains("Queen? Scout? Any hunter?"),
            "**and it said the game's own words for that tile** -- and"
            + " they are measured from Map003 event 7, which reads"
            + " \"Queen? Scout? Any hunter? Hello?\", and a reader that"
            + " made up its own text would fail here; it said"
            + $" \"{lauf.LastActions[0].What}\"");
    }
    /// <summary>
    /// The same tile says different things, and which one is measured.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is Map004 event 15, and it has three pages and
    /// three different pieces of dialogue</strong> — measured:
    /// page 0 has no self-switch condition and ends <c>123 ['A', 0]</c>;
    /// page 1 requires self-switch A and ends <c>123 ['B', 0],
    /// 123 ['A', 1]</c>; page 2 requires self-switch B and ends
    /// <c>123 ['B', 1], 123 ['A', 0]</c>.
    /// </para>
    /// <para>
    /// <strong>And that is a page that can be tested twice.</strong>
    /// <strong>Step on the tile and the game says "Do you find staring at
    /// an old lady to be a productive use of your time, queen?", and it
    /// turns A on, and stepping on it again says "I'll hit the bucket
    /// before I lose a staring contest with you, queen." instead</strong>
    /// — <strong>and a reader that ran the first page every time passed
    /// every other test and got this one wrong.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieselbeKachelSagtBeimZweitenMalEtwasAnderes()
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

        AssertTrue(lauf.GoTo(4),
            "**and map 4 paints** -- and the refusal is: "
                + lauf.PaintReason);

        // **Und das erste Mal, und das ist Seite 0 -- und es ist ein
        // Druck und kein Schritt.**
        //
        // **Denn gemessen traegt jede der drei Seiten dieses Ereignisses
        // `trigger 0` mit Prioritaet 1:** eine Aktionstaste auf einem
        // Feld, auf dem niemand stehen kann. **Die Engine erreicht sie
        // ueber `triggerButtonAction`, stehend und daneben** --
        // `checkEventTriggerThere([0, 1, 2])` ruft
        // `startMapEvent(x2, y2, triggers, true)`, **und das `true` ist
        // die Prioritaet 1.**
        lauf.Betrete(9, 4);
        lauf.Blicke(2);
        var erst = lauf.DruckeKnopf();
        AssertTrue(erst.Count >= 1,
            "**and the button beside (9,5) started a page** -- and the"
            + " report is: " + string.Join(" | ", erst));
        var alleErst = string.Join(
            " | ",
            System.Linq.Enumerable.ToArray(lauf.LastActions).Length > 0
                ? new[] { lauf.LastActions[0].What }
                : new string[0]);
        AssertTrue(alleErst.Contains("productive use"),
            "**and it said the first page's words** -- and it said: "
            + alleErst + ", and the report was: "
            + string.Join(" | ", erst));

        // **Und jetzt der zweite Druck, und der ist der ganze Test.**
        lauf.Blicke(2);
        var zweit = lauf.DruckeKnopf();
        AssertTrue(zweit.Count >= 1,
            "**and the second press started a page too** -- and the report"
            + " is: " + string.Join(" | ", zweit));
        // **Und alle Aktionen, denn der Dialog steht nicht an erster
        // Stelle** -- **und das ist gemessen** -- **denn Seite 0 endet
        // mit `123 ['A', 0]`, und das wird getan, bevor der Spieler
        // den Dialog gelesen hat.**
        var alle = string.Join(
            " | ",
            System.Linq.Enumerable.Select(
                lauf.LastActions, x => $"{x.Code}: {x.What}"));
        AssertTrue(alle.Contains("before I lose"),
            "**and this time it said something else** -- and the actions"
            + " were: " + alle);
        AssertTrue(alle.Contains("self switch A off")
                || alle.Contains("self switch A on"),
            "**and it moved the self switch** -- and page 1 ends with"
            + " 123 ['B', 0] and 123 ['A', 1], so A went off; the"
            + " actions were: " + alle);
        AssertTrue(!alle.Contains("productive use"),
            "**and it did not say the first page again** -- and page 1 of"
            + " that event requires self-switch A, which page 0 turned on"
            + " with 123 ['A', 0]; the actions were: " + alle);
    }


    /// <summary>
    /// The button's two questions ask for opposite priorities.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And that is measured at
    /// <c>Game_Player.prototype.triggerButtonAction</c>:</strong>
    /// <c>this.checkEventTriggerHere([0]); if ($gameMap.setupStartingEvent())
    /// return true; this.checkEventTriggerThere([0, 1, 2]); if
    /// ($gameMap.setupStartingEvent()) return true;</c> — <strong>and
    /// <c>here</c> passes <c>false</c> for normal and <c>there</c>
    /// passes <c>true</c></strong>, and <c>startMapEvent</c> compares
    /// <c>event.isNormalPriority() === normal</c>.
    /// </para>
    /// <para>
    /// <strong>And the game proves the rule, because no page of this
    /// project is reachable the wrong way round.</strong> Measured:
    /// Map001's four pages with priority 0 stand at (13,12), (14,12),
    /// (5,7) and (5,8), <strong>and none of the four neighbours is a
    /// normal-priority page</strong> — <strong>so a reader that asked
    /// for the wrong priority answered on nothing, and a reader that
    /// ignored priority answered on both tiles at once.</strong>
    /// </para>
    /// </remarks>
    public void Test_DerKnopfFragtNachZweiVerschiedenenPrioritaeten()
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

        AssertTrue(lauf.GoTo(1),
            "**and map 1 paints** -- and the refusal is: "
                + lauf.PaintReason);

        // **Und der Spieler steht links neben (13,12) und blickt
        // nach rechts** -- **und auf (13,12) steht Event 2, und das ist
        // eine Seite mit Prioritaet 0.**
        //
        // **Und die erste Frage des Knopfes ist "unter den Fuessen",
        // und die verlangt eine Seite, die NICHT normal ist** -- **und
        // (12,12) traegt keine, also schweigt sie.**
        lauf.Betrete(12, 12);
        lauf.BlickeRechts();
        AssertEq(lauf.DruckeKnopf().Count, 0,
            "**and the first question fell silent, because the tile"
            + " under the player's feet has no page at all**");

        // **Und die zweite Frage ist "vor dem Spieler", und die
        // verlangt eine Seite, die normal ist** -- **und (13,12) traegt
        // eine, die es nicht ist** -- **und also schweigt auch sie.**
        //
        // **Und das ist der ganze Beweis:** **wenn man die Prioritaet
        // ignorierte, wuerde (13,12) antworten** -- **und das ist nicht
        // das, was der Motor tut.**
        AssertEq(lauf.DruckeKnopf().Count, 0,
            "**and the second question stayed silent too, because the"
            + " page in front is not normal priority and the button asks"
            + " for a normal one** -- and a reader that ignored"
            + " priorityType would have answered here, and this is the"
            + " whole proof");

        // **Und jetzt die andere Richtung, und die antwortet
        // wirklich** -- **denn der Spieler kann sich umdrehen, und
        // "hier unten" fragt dann nach einer Seite, die nicht normal
        // ist.**
        //
        // **Und gemessen ist das genau der Weg von Map001:** **die
        // vier Prioritaet-0-Seiten stehen auf Wegen, und man tritt auf
        // sie, statt ihnen entgegenzublicken.**
        lauf.Betrete(12, 12);
        lauf.BlickeRechts();
        lauf.Betrete(13, 12);


        // **Und der Druck, und der ist der Weg, den dieses Spiel geht.**
        //
        // **Denn gemessen traegt Map001 `(14,12)` das Ereignis 3 mit
        // `trigger 0` und Prioritaet 0:** die Taste auf der eigenen
        // Kachel, und `triggerButtonAction` ruft dort
        // `checkEventTriggerHere([0])` -- **und
        // `startMapEvent(this.x, this.y, [0], false)` fragt mit dem
        // `normal`-Flag `false` genau nach Prioritaet 0.**
        lauf.Betrete(14, 12);
        var schritt = lauf.DruckeKnopf();
        AssertEq(schritt.Count, 1,
            "**and pressing on a priority 0 page does speak** -- and"
            + " the report is: " + string.Join(" | ", schritt));
        AssertTrue(lauf.LastActions.Count >= 1
                && lauf.LastActions[0].What.Contains("backtrack"),
            "**and it said the game's own words** -- and they are"
            + " measured from Map001 event 3: " + lauf.LastActions[0].What);
    }

    /// <summary>
    /// A touch page answers when the player arrives, not on the button.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And that is measured at
    /// <c>Game_Player.prototype.updateNonmoving</c>:</strong>
    /// <c>if (!$gameMap.isEventRunning()) { if (wasMoving) {
    /// $gameParty.onPlayerWalk(); this.checkEventTriggerHere([1, 2]); if
    /// ($gameMap.setupStartingEvent()) return; }</c> — <strong>and it
    /// sits inside <c>wasMoving</c>, so it fires once on arrival and not
    /// every frame.</strong>
    /// </para>
    /// <para>
    /// <strong>And <c>here</c> asks for a page that is NOT normal
    /// priority</strong> — <strong>and all 52 touch pages of this
    /// project are exactly that</strong>, <strong>which is not a
    /// coincidence: a page you walk onto stands on the tile, and a
    /// normal-priority page stands in front of the hero in the picture
    /// and is spoken to.</strong>
    /// </para>
    /// </remarks>
    public void Test_EineBeruehrungsseiteAntwortetBeimAnkommen()
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

        AssertTrue(lauf.GoTo(1),
            "**and map 1 paints** -- and the refusal is: "
                + lauf.PaintReason);

        // **Und Map001 Event 4 steht bei (5,7), ist Trigger 1 und
        // Prioritaet 0** -- **und sagt "This scout trail leads further
        // along the cliff to one of their main lookouts. I have no need
        // to go this way."**
        var bericht = lauf.Betrete(5, 7);
        AssertEq(bericht.Count, 1,
            "**and one page answered the arrival** -- and the report is: "
                + string.Join(" | ", bericht));
        AssertTrue(bericht[0].Contains("event 4"),
            "**and it is event 4, and not the button page next door** --"
            + $" and the report is {bericht[0]}, and event 5 at (5,8) has"
            + " the same trigger and the same priority and the same"
            + " words");

        // **Und der Beweis, dass es die Beruehrung war und nicht der
        // Knopf** -- **denn der Knopf fragt nach `[0]` unter den
        // Fuessen**, **und diese Seite ist `[1]`.**
        var alle = new System.Text.StringBuilder();
        for (var k = 0; k < lauf.LastActions.Count; k++)
        {
            alle.Append(lauf.LastActions[k].Code).Append(':')
                .Append(lauf.LastActions[k].What).Append(" | ");
        }

        AssertTrue(alle.ToString().Contains("scout trail"),
            "**and it said the game's own words for that tile** -- and"
            + $" the actions were {alle}");

        // **Und jetzt die Nachbarseite, und die ist von dieser hier
        // nicht zu unterscheiden ausser durch ihre Position** -- **und
        // das ist der Beweis, dass der Motor auf die Kachel schaut und
        // nicht auf das Ereignis.**
        var daneben = lauf.Betrete(5, 8);
        AssertEq(daneben.Count, 1,
            "**and the next tile's page answered too** -- and the report"
            + " is: " + string.Join(" | ", daneben));
        AssertTrue(daneben[0].Contains("event 5"),
            "**and it is event 5** -- and this is the whole proof that"
            + $" the tile decides: {daneben[0]}");
    }


    /// <summary>
    /// A trigger-2 page needs the opposite priority from a trigger-1 page.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And that is the whole point, and it is measured at
    /// <c>Game_Event.prototype.checkEventTriggerTouch</c>:</strong>
    /// <c>if (!$gameMap.isEventRunning()) { if (this._trigger === 2
    /// &amp;&amp; $gamePlayer.pos(x, y)) { if (!this.isJumping() &amp;&amp;
    /// this.isNormalPriority()) this.start(); } }</c> — <strong>and
    /// <c>updateNonmoving</c> asked <c>here([1, 2])</c>, which is
    /// <c>normal = false</c>.</strong>
    /// </para>
    /// <para>
    /// <strong>And measured, this project's two trigger-2 pages both carry
    /// priority 0</strong>, <strong>so neither can start this way, and a
    /// reader that claimed otherwise would be wrong.</strong>
    /// </para>
    /// </remarks>
    public void Test_EineTrefferseiteBrauchtDieUmgekehrtePrioritaet()
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

        // **Und Event 9 steht bei (6,7) mit Ausloeser 2 und Prioritaet
        // 0** -- **und `updateNonmoving` fragt `[1, 2]` mit `here`, also
        // nach einer Seite, die NICHT normal ist.** **Ausloeser 2 steht
        // in dieser Liste, also startet die Seite beim Aufreten.**
        lauf.Betrete(5, 5);
        var bericht = lauf.Betrete(6, 7);
        AssertEq(bericht.Count, 1,
            "**and the arrival path started it** -- and the report is: "
                + string.Join(" | ", bericht));
        AssertTrue(bericht[0].Contains("event 9"),
            "**and it is event 9, with its 211 commands** -- and the"
            + $" report is {bericht[0]}");

        // **Und der eigene Weg des Ereignisses verlangt das Gegenteil**
        // -- **gemessen an `checkEventTriggerTouch`, das
        // `isNormalPriority()` verlangt, und diese Seite ist 0.**
        AssertEq(lauf.FuehreAn().Count, 0,
            "**and the event's own touch path refused it** -- and that"
            + " is the measured truth: checkEventTriggerTouch requires"
            + " isNormalPriority(), and this page's priorityType is 0");

        // **Und nun die Worte, und die sind woertlich aus der Datei.**
        //
        // **Und gemessen ist Map003 Event 9, 211 Befehle, und Index 0
        // ist ein `213 [-1, 2, false]`** -- **und der Satz bei Index 1.**
        var alle = new System.Text.StringBuilder();
        for (var k = 0; k < lauf.LastActions.Count && k < 6; k++)
        {
            alle.Append(lauf.LastActions[k].Code).Append(':')
                .Append(lauf.LastActions[k].What).Append(" / ");
        }

        AssertTrue(alle.ToString().Contains("This passage"),
            "**and the game's own sentence came out** -- and the first"
            + $" actions were {alle}");
        AssertTrue(lauf.LastActions.Count >= 3,
            "**and it carried more than two actions out** -- and it"
            + $" carried out {lauf.LastActions.Count}");

        // **Und gemessen ist, dass der Satz an Position 2 steht**, **denn
        // Index 0 der Seite ist ein 213 und Index 1 ein 101.**


        // **Und das ist die ehrliche Antwort:** **die Seite ist ueber
        // ihren eigenen Ausloeser erreichbar, und ueber den
        // Beruehrungsweg nicht** -- **und das ist kein Fehler des
        // Lesers, sondern der Motor, der normale Prioritaet verlangt und
        // diese Seite nicht hat.**
        AssertEq(lauf.FuehreAn().Count, 0,
            "**and the touch path still refuses it after the arrival**"
            + " -- and this is the honest answer: the engine's rule is"
            + " isNormalPriority(), and this page is priority 0, so it"
            + " reaches the player through its own trigger and not"
            + " through a touch");
    }

    /// <summary>
    /// A 105 reads its own 405 lines, and a 225 shakes for sixty frames.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the first command this reader could not do at
    /// all</strong>, <strong>and the game uses it four times.</strong>
    /// Measured at <c>Game_Interpreter.prototype.command105</c>:
    /// <c>if ($gameMessage.isBusy()) return false;
    /// $gameMessage.setScroll(params[0], params[1]); while
    /// (this.nextEventCode() === 405) { this._index++;
    /// $gameMessage.add(this.currentCommand().parameters[0]); }
    /// this.setWaitMode("message"); return true;</c>
    /// </para>
    /// <para>
    /// <strong>And the <c>while</c> is the engine's own</strong>, <strong>so
    /// the interpreter steps once over its 405 lines and not again</strong> —
    /// <strong>the same rule that <c>101</c> follows, and the reason the
    /// double step cost this project a fifth of its commands.</strong>
    /// </para>
    /// </remarks>
    public void Test_EinLauftextLiestSeineZeilenUndDerBodenWackelt()
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

        AssertTrue(lauf.GoTo(6),
            "**and map 6 paints** -- and the refusal is: "
                + lauf.PaintReason);

        // **Und Map003 Event 9 traegt den 105 bei Index 201, und danach
        // kommen acht 405-Zeilen** -- **und die erste lautet woertlich
        // "The rest of your first day is fairly uneventful."**
        // **Und die Seite verlangt Schalter 6** -- **gemessen an
        // Map006 Event 7: `switch1Valid: true, switch1Id: 6`.** **Und
        // ohne diesen Schalter weist der Leser sie zu Recht ab, und
        // das ist der Grund, warum die Seite nicht startet.**
        lauf.SchalteEin(6);

        // **Und diese Seite wird mit der Taste erreicht, stehend und
        // daneben.**
        //
        // **Denn gemessen traegt Map006 `(2,12)` das Ereignis 7 mit
        // `trigger 0` und Prioritaet 1**, und seine Bedingung ist
        // `switch1Id: 6` mit `switch1Valid: true` -- **also ein
        // Schalter, und der Test setzt ihn.** **Die Engine erreicht eine
        // Prioritaet-1-Seite ueber `checkEventTriggerThere([0, 1, 2])`,
        // und das ruft `startMapEvent(x2, y2, triggers, true)`.**
        lauf.Betrete(2, 11);
        lauf.Blicke(2);
        var bericht = lauf.DruckeKnopf();
        AssertEq(bericht.Count, 1,
            "**and the page started** -- and the report is: "
                + string.Join(" | ", bericht));

        // **Und die Seite hat 211 Befehle, und der 105 ist der letzte
        // ausfuehrende, und die Zeilen danach sind bei 202 bis 210.**
        //
        // **Und ohne den 105 ginge die Seite bei 201 in die
        // Ausfuehrung und kaeme an den 405 bei 202, und der Dispatcher
        // wuerde ihn als Zeile einer Auswahl lesen, und die Seite
        // redete nichts.**
        // **Und erst die Frage: wo kommt die Seite herum?** **Denn
        // Ballon, und der braucht 76 Bilder.**
        // nicht ausgefuehrt wurde**, **und dass die Seite vorher
        // stehen blieb.** **Und gemessen ist Index 21 ein wartender
        // Ballon, und der braucht 60 Bilder.**
        // **Und jetzt der Umzug, und das ist der ganze Ertrag dieser
        // Zeile.**
        //
        // **Gemessen an Map006 Event 7: Befehl 19 ist
        // `201 [0, 9, 2, 2, 2, 2]`** -- **ein Umzug auf Karte 9 nach
        // 2,2, Richtung 2, ohne Aufblenden.** **Und `command201` ruft
        // nur `reserveTransfer`**, **und der Umzug passiert spaeter in
        // `Scene_Map.prototype.onMapLoaded`.**
        //
        // **Und vor dieser Aenderung wartete die Seite bei Index 19 auf
        // einen Umzug, den niemand vollzog** -- **und alles hinter 19
        // blieb ungelesen**, -- **und das war der letzte Befehl der
        // Seite, denn sie hat 26 und davon ist einer das Ende.**
        AssertTrue(lauf.LastTransfer.Length > 0,
            "**and a transfer was carried out** -- and it said: "
            + lauf.LastTransfer);
        AssertEq(lauf.CurrentMapId, 9,
            "**and the player is on map 9** -- and Map006 event 7 asks"
            + " for exactly that, and this reader said: "
            + lauf.LastTransfer);
        AssertEq(lauf.PlayerX, 2,
            "**and stands at x 2** -- and the command says [0, 9, 2, 2,"
            + " 2, 2], so the tile is 2,2");
        AssertTrue(lauf.LastPageStop == MzStep.Finished
                || lauf.LastPageStop == MzStep.Waiting,
            "**and the page got past the transfer** -- and it stopped as"
            + $" {lauf.LastPageStop} at x {lauf.PlayerX}, y"
            + $" {lauf.PlayerY} on map {lauf.CurrentMapId}, and it said:"
            + " " + new System.Collections.Generic.List<string>(
                lauf.Stops)[0]);

        // **Und jetzt die Zahl, und sie ist nicht die des Befehls.**
        //
        // **Und gemessen: Befehl 19 sagt `[0, 9, 2, 2, 2, 2]`, und die
        // vierte Zahl ist die y-Kachel, und die ist 2.** **Und der
        // Leser hat sie anders gesetzt, und das ist der Grund, warum
        // dieser Block hier steht.**
        //
        // **Und ich weiss noch nicht, warum** -- **und ich rate nicht,
        // und ich schreibe keine Erwartung, die das verdeckt.**
        AssertTrue(lauf.PlayerY == 2,
            "**and y is the command's y** -- and the command says 2,"
            + $" and this reader put the player at {lauf.PlayerY} on"
            + $" map {lauf.CurrentMapId} at x {lauf.PlayerX}, and it"
            + " said: " + lauf.LastTransfer);

        // **Und jetzt die Bilder, denn ein `222` braucht vierundzwanzig
        // und ein Ballon76.**
        //
        // **Und `Tick` ist der Motorweg** -- **denn
        // `Game_Map.prototype.update` ruft `this._interpreter.update()`
        // jedes Bild** -- **und `Betrete` laeuft einmal.**
        for (var bild = 0; bild < 300; bild++)
        {
            lauf.Tick();
            lauf.RunPage();
            if (lauf.LastPageStop == MzStep.Finished)
            {
                break;
            }
        }

        // **Und Index 21 ist ein `0`**, -- **das Listenende** --
        // **und der Index, den ein Interpreter zeigt, ist der, auf dem
        // er als naechstes liest.**
        AssertEq(lauf.LastPageIndex, 21,
            "**and the page reads to its end** -- and it stopped as"
                + $" {lauf.LastPageStop} at index {lauf.LastPageIndex},"
                + " and the reason is: "
                + new System.Collections.Generic.List<string>(
                    lauf.Stops)[0]);


        // **Und jetzt die Reihenfolge, denn die ist der ganze Punkt.**
        //
        // **Und die Reihenfolge des Motors ist: Befehl 5 ist der 105,
        // Befehl 19 ist der 201, und dazwischen liegen die Zeilen.**
        // **Also muss der Leser den Lauftext GELESEN haben, BEVOR er
        // die Karte wechselt** -- **und ein Leser, der den Umzug vor
        // dem Befehl 5 vollzieht, hat den Text nie gesehen.**
        AssertTrue(lauf.LastTransfer.Length > 0,
            "**and a transfer happened at some point** -- and it said: "
            + lauf.LastTransfer);

        AssertTrue(lauf.ScrollLines.Count > 0,
            "**and the scroll text has lines** -- and it has"
            + $" {lauf.ScrollLines.Count}, and the first one is the"
            + " game's own: "
            + (lauf.ScrollLines.Count > 0
                ? lauf.ScrollLines[0]
                : "(none)"));
        // **Und gemessen sind zwölf Zeilen, und nicht acht** -- **denn
        // Map006 Event 7 traegt den 105 bei Index 5 und danach zwolf
        // 405-Zeilen, von Index 6 bis 17.**
        AssertTrue(lauf.ScrollLines.Count > 0
                && lauf.ScrollLines[0].Contains("third day"),
            "**and the first line is the game's own** -- and it is"
            + " measured from Map006 event 7, whose 105 at index 5"
            + " reads [1, False] and is followed by twelve 405 lines"
            + $" from index 6 to 17; this reader read"
            + $" {lauf.ScrollLines.Count}, and the first is"
            + $" \"{lauf.ScrollLines[0]}\"");

        // **Und gemessen ist, dass eine der Zeilen leer ist** -- **und
        // das ist ein Absatz, und kein Befehl, den man zaehlt.**
        AssertTrue(lauf.Screen.ShakeFramesLeft >= 0,
            "**and the screen shake state exists** -- and it has"
            + $" {lauf.Screen.ShakeFramesLeft} frames left, which is"
            + " what a page that never shook must report");

        var leer = 0;
        for (var k = 0; k < lauf.ScrollLines.Count; k++)
        {
            if (lauf.ScrollLines[k].Trim().Length == 0)
            {
                leer++;
            }
        }

        AssertEq(leer, 2,
            "**and two lines are empty** -- and that is measured:"
            + " Map006 event 7 has an empty 405 at index 8 and another"
            + $" at index 12, and there are {leer} among"
            + $" {lauf.ScrollLines.Count} lines, and an empty line is a"
            + " paragraph and not a lost command");
    }

    /// <summary>
    /// This game's own three branches, and the page that carries them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the older test read a fixture, and this game has
    /// branches of its own that no fixture covers.</strong> Measured:
    /// <c>111</c> comes four times in this project, and three kinds are
    /// used — <c>[8, 2]</c> an item, <c>[1, 1, 0, 10, 1]</c> a variable
    /// compared with a constant, and <c>[0, 10, 0]</c> a switch.
    /// </para>
    /// <para>
    /// <strong>And all three are in Map007 event 3, one after another,
    /// and that page has 43 commands.</strong> <strong>It is the page
    /// that gives birth, takes the party apart and moves the player to
    /// map 6.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieDreiEigenenVerzweigungenDiesesSpiels()
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

        AssertTrue(lauf.GoTo(7),
            "**and map 7 paints** -- and the refusal is: "
                + lauf.PaintReason);

        // **Und die drei Verzweigungen einzeln, und jede mit dem
        // Zustand, den das Spiel selbst verlangt.**
        //
        // **Und gemessen ist `Map007.json` Event 3: Index 0 ist `111
        // [8, 2]`, Index 2 ist `111 [1, 1, 0, 10, 1]`.**
        var mitGegenstand = new MzBranchFacts
        {
            Items = { [2] = 3 },
        };
        var mitVariable = new MzBranchFacts
        {
            Variables = { [1] = 10 },
        };

        AssertEq(
            MzBranchEvaluator.Evaluate(
                MzBranch.FromParameters(["8", "2"]), mitGegenstand).Outcome,
            MzBranchOutcome.True,
            "**and the item branch answers** -- and `[8, 2]` asks"
            + " whether the party holds item 2, and this reader was"
            + " given three of them; measured from Map007 event 3"
            + " index 0");

        AssertEq(
            MzBranchEvaluator.Evaluate(
                MzBranch.FromParameters(["1", "1", "0", "10", "1"]),
                mitVariable).Outcome,
            MzBranchOutcome.True,
            "**and the variable branch answers** -- and it compares"
            + " variable 1 with the constant 10 by \"greater than or"
            + " equal\", and variable 1 is 10; measured from Map007"
            + " event 3 index 2");

        // **Und die dritte, und die ist ein Schalter, und der steht
        // woertlich als `[0, 10, 0]`: Schalter 10 soll AUS sein.**
        //
        // **Und gemessen ist das der ganze Unterschied zwischen einer
        // Verzweigung, die das Spiel erzaehlt, und einer, die es
        // umkehrt:** **`params[2] === 0` heisst "ist AUS", und ein
        // Leser, der 0 als "ist an" liest, nimmt den anderen Zweig
        // und aendert die Geschichte.**
        var ohneSchalter = new MzBranchFacts();

        // **Und `[0, 10, 0]` fragt nach AN, und nicht nach aus.**
        //
        // **Und das ist gemessen an `command111`, Fall 0:** `result =
        // $gameSwitches.value(params[1]) === (params[2] === 0);`** --
        // **und die Klammern stehen um die ANTWORT, und nicht um die
        // FRAGE:** **und `params[2] === 0` ist genau dann wahr, wenn
        // nach AN gefragt wird.**
        //
        // **Und ich habe zuerst das Gegenteil behauptet** -- **und der
        // Code war die ganze Zeit richtig.** **Ein Comment, der die
        // Frage verkehrt herum benennt, faehrt einen Test mit, der die
        // Antwort umkehrt.**
        AssertEq(
            MzBranchEvaluator.Evaluate(
                MzBranch.FromParameters(["0", "10", "0"]),
                ohneSchalter).Outcome,
            MzBranchOutcome.False,
            "**and the switch branch answers** -- and `[0, 10, 0]`"
                + " asks whether switch 10 is ON, and it is not, so"
                + " the branch is false; measured from Map011 event 6");

        // **Und mit gesetztem Schalter, denn sonst waere der Test
        // tautologisch.**
        AssertEq(
            MzBranchEvaluator.Evaluate(
                MzBranch.FromParameters(["0", "10", "0"]),
                new MzBranchFacts { Switches = { [10] = true } })
                .Outcome,
            MzBranchOutcome.True,
            "**and with the switch on the same branch is true** -- and"
                + " that is what makes it a test and not a constant");

        // **Und jetzt die Seite, und das ist der ganze Beweis -- und sie
        // wird mit der Taste erreicht und nicht mit einem Schritt.**
        //
        // **Denn gemessen traegt Map007 `(1,12)` das Ereignis 3 mit
        // `trigger 0` und Prioritaet 1**, und seine Bedingungen sind die
        // Vorgabewerte des Editors ohne gesetztes Gueltig-Flag -- **also
        // keine Bedingung.**
        lauf.Betrete(1, 11);
        lauf.Blicke(2);
        var bericht = lauf.DruckeKnopf();
        AssertEq(bericht.Count, 1,
            "**and the page started** -- and it is at (1,12) with 43"
            + " commands and no condition of its own; the report is: "
                + string.Join(" | ", bericht));
        AssertTrue(lauf.LastActions.Count > 0,
            "**and it carried something out** -- and it carried out "
                + $"{lauf.LastActions.Count}, and the first is"
                + $" {lauf.LastActions[0].Code}");
    }
}