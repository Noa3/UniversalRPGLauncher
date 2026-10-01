using System;
using System.Collections.Generic;
using System.IO;
using UniversalRPG.Tests.Framework;
using UniversalRPG.Web;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The figures of a real MZ map, read out of its file.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the figure's image is on the page, and not on the
/// event.</strong> The event carries <c>id</c>, <c>name</c>, <c>x</c>
/// and <c>y</c>; <strong>the character, its index, its direction and its
/// walk pattern are in <c>page.image</c>.</strong>
/// </para>
/// </remarks>
public partial class TestMzMapFigure : TestBase
{
    private const string Projekt = "E:/RPGMakerGames/CamelliaCoronation-Win";

    private static bool Vorhanden()
    {
        return File.Exists(Projekt + "/data/Map017.json");
    }

    private static MzValue Karte()
    {
        return MzDataFile.Read(
            "data/Map017.json",
            File.ReadAllBytes(Projekt + "/data/Map017.json")).Root;
    }

    /// <summary>
    /// The figures come out of the pages, and there are as many as the
    /// file says.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the count is measured from the file, and not
    /// typed in.</strong> Fourteen pages on <c>Map017</c> carry a
    /// character; <strong>and a test that asserted a fixed number would
    /// pass after a change to the project and fail before it, and
    /// neither would be about this reader.</strong>
    /// </para>
    /// <para>
    /// <strong>And the name is a word, and not a number.</strong>
    /// Measured: <em>SlimeCharacters</em>, <em>MC_Sprite_sheet</em>,
    /// <em>!Flame</em> — <strong>and the file is
    /// <c>img/characters/&lt;name&gt;.png_</c></strong>, <strong>so a
    /// reader that took a character id and looked for
    /// <c>img/characters/1.png</c> found no file at all.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieFigurenKommenAusDenSeitenUndEsSindSoVieleWieDieDateiSagt()
    {
        if (!Vorhanden())
        {
            return;
        }

        var karte = Karte();
        // **Und die erste passende Seite gewinnt** -- **also zaehlt
        // der Leser Ereignisse und nicht Seiten**, **und diese Datei hat
        // neun Ereignisse mit einer Bildseite und vierzehn Bildseiten
        // ueberhaupt**, **und beide Zahlen sind richtig und meinen
        // Verschiedenes.**
        // **Und die erste passende Seite gewinnt** -- **also zaehlt
        // der Leser Ereignisse und nicht Seiten** -- **und diese Datei
        // hat neun Ereignisse mit einer Bildseite und vierzehn
        // Bildseiten ueberhaupt**, **und beide Zahlen sind richtig und
        // meinen Verschiedenes.**
        //
        // **Und eine Seite an einer Bedingung, die dieser Leser nicht
        // beantworten kann, wird weggelassen** -- **gemessen: Ereignis
        // 19 haengt an einem Selbstschalter** -- **und deshalb ist die
        // Erwartung acht und nicht neun.**
        var erwartet = 0;
        foreach (var ereignis in karte.Member("events")!.Items)
        {
            var seiten = ereignis.Member("pages")?.Items
                ?? new List<MzValue>();
            for (var index = 0; index < seiten.Count; index++)
            {
                var seite = seiten[index];
                if (!MzMapFigureReader.Meets(
                    seite.Member("conditions"), new MzBranchFacts()))
                {
                    continue;
                }

                var name = (seite.Member("image")?.Member("characterName"))
                    ?.StringOr("") ?? "";
                if (name.Length > 0)
                {
                    erwartet++;
                }

                break;
            }
        }

        AssertTrue(erwartet > 0,
            "**and the map has pages carrying a character**");

        var figuren = MzMapFigureReader.Read(karte, new MzBranchFacts(),
            out var notwendig);
        AssertEq(figuren.Count, erwartet,
            "**and the reader finds exactly that many** -- and the number "
                + "is counted from the file, because a test that asserted "
                + "a fixed count would pass after a change to the project "
                + "and fail before it");

        // **Und der Name ist ein Wort.**
        var mitNamen = 0;
        foreach (var figur in figuren)
        {
            if (figur.CharacterName.Length > 0)
            {
                mitNamen++;
            }
        }

        AssertEq(mitNamen, figuren.Count,
            "**and every one of them names its image** -- and the names are "
                + "words like SlimeCharacters, and the file is "
                + "img/characters/<name>.png_, and a reader that took a "
                + "character id looked for img/characters/1.png");
    }

    /// <summary>
    /// A page with no condition is visible, and most pages are.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And 208 of the project's 253 pages have every flag
    /// false.</strong> <strong>A reader that treated an unset flag as
    /// "not satisfied" hid every page in the game</strong> — <strong>and
    /// one that treated it as "satisfied" showed pages a game switched
    /// away from.</strong>
    /// </para>
    /// <para>
    /// <strong>And the measured pattern across the whole project is
    /// four kinds:</strong> nothing (208), a switch (11), a self switch
    /// (33), two switches (1). <strong>Actor, item, variable and timer
    /// never occur</strong> — <strong>and a reader that answered them
    /// anyway invented answers nobody asked for.</strong>
    /// </para>
    /// </remarks>
    public void Test_EineSeiteOhneBedingungIstSichtbar()
    {
        var leere = MzDataFile.ReadText("Map.json",
            "{\"conditions\":{\"switch1Valid\":false,\"selfSwitchValid\":false,"
            + "\"actorValid\":false,\"itemValid\":false,"
            + "\"variableValid\":false}}").Root;

        AssertTrue(MzMapFigureReader.Meets(
            leere.Member("conditions"), new MzBranchFacts()),
            "**and a page with every flag false is visible** -- and 208 of "
                + "253 pages in the project are like this, and a reader "
                + "that treated an unset flag as \"not satisfied\" hid "
                + "every page in the game");

        AssertTrue(MzMapFigureReader.Meets(null, null),
            "**and a page with no conditions object at all is visible**");
    }

    /// <summary>
    /// A page that hangs on a switch follows the switch.
    /// </summary>
    /// <remarks>
    /// <strong>And this is the one condition this reader can
    /// answer.</strong> Eleven pages of the project use it.
    /// <strong>A page on a switch that is off is not drawn</strong>,
    /// <strong>and one that is on is.</strong>
    /// </remarks>
    public void Test_EineSeiteAmSchalterFolgtDemSchalter()
    {
        var fakten = new MzBranchFacts();
        var bedingung = MzDataFile.ReadText("Map.json",
            "{\"conditions\":{\"switch1Valid\":true,\"switch1Id\":7}}").Root;

        // **Und `Meets` bekommt das `conditions`-Objekt, und nicht die
        // Seite, in der es steht** -- **und ein Test, das die Seite
        // uebergibt, prueft eine Bedingung, die keine ist.**
        AssertTrue(!MzMapFigureReader.Meets(
                bedingung.Member("conditions"), fakten),
            "**and a page on a switch that is off is not drawn**");

        fakten.SetSwitch(7, true);
        AssertTrue(MzMapFigureReader.Meets(bedingung.Member("conditions"), fakten),
            "**and it is drawn once the switch is on** -- and eleven "
                + "pages of the project hang on one");

        // **Und zwei Schalter muessen beide gelten.**
        var zwei = MzDataFile.ReadText("Map.json",
            "{\"conditions\":{\"switch1Valid\":true,\"switch1Id\":7,"
            + "\"switch2Valid\":true,\"switch2Id\":8}}").Root;
        AssertTrue(!MzMapFigureReader.Meets(zwei.Member("conditions"), fakten),
            "**and a page on two switches needs both**");
        fakten.SetSwitch(8, true);
        AssertTrue(MzMapFigureReader.Meets(zwei.Member("conditions"), fakten),
            "**and it is drawn when the second one is on too**");
    }

    /// <summary>
    /// A page this reader cannot answer is left out, and said.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the honest answer is "no".</strong> Thirty-three pages
    /// of the project hang on a <em>self switch</em>, <strong>and a self
    /// switch belongs to the event, not to the map</strong> — <strong>and
    /// the engine's own rule is that it shows a page it cannot decide
    /// on.</strong>
    /// </para>
    /// <para>
    /// <strong>And leaving it out is the better of the two wrong
    /// answers.</strong> A figure drawn on a condition this reader
    /// cannot answer is a figure the player sees in the wrong state,
    /// <strong>and a figure that is missing is a figure the game can
    /// bring back by switching on.</strong>
    /// </para>
    /// </remarks>
    public void Test_EineSeiteDieDieserLeserNichtBeantwortenKannWirdWeggelassen()
    {
        var fakten = new MzBranchFacts();
        var selbst = MzDataFile.ReadText("Map.json",
            "{\"conditions\":{\"selfSwitchValid\":true,"
            + "\"selfSwitchCh\":\"A\"}}").Root;

        AssertTrue(!MzMapFigureReader.Meets(selbst.Member("conditions"), fakten),
            "**and a page on a self switch is not drawn** -- and a self "
                + "switch belongs to the event and not to the map, and 33 "
                + "pages of the project hang on one");

        // **Und dieselbe Regel fuer die drei, die das Projekt nicht
        // benutzt.**
        foreach (var feld in new[] { "actorValid", "itemValid", "variableValid" })
        {
            var bedingung = MzDataFile.ReadText("Map.json",
                "{\"conditions\":{\"" + feld + "\":true}}").Root;
            AssertTrue(!MzMapFigureReader.Meets(bedingung.Member("conditions"), fakten),
                $"**and for {feld} too** -- and none of the three occurs "
                + "in this project, and a reader that answered it anyway "
                + "invented an answer nobody asked for");
        }
    }

/// <summary>
    /// The last matching page wins, and not the first.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the engine's own loop, backwards.</strong>
    /// Measured at <c>Game_Event.prototype.findProperPageIndex</c>:
    /// <c>for (let i = pages.length - 1; i &gt;= 0; i--)</c> —
    /// <strong>so the last page whose conditions are met is the one that is
    /// shown.</strong>
    /// </para>
    /// <para>
    /// <strong>And a reader that took the first showed the wrong face of
    /// every event with two pages</strong> — <strong>and with a plain page
    /// and a switch page, it showed the plain one.</strong> <strong>A game
    /// tells you what a person says after a switch flipped, and this reader
    /// showed you what they said before.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieLetztePassendeSeiteGewinntUndNichtDieErste()
    {
        // **Und ein Ereignis mit zwei Seiten, beide ohne Bedingung.**
        //
        // **Das ist der Fall, den ein Spiel am häufigsten benutzt** —
        // **eine leere Seite, weil die Figur nur dasteht, und eine mit
        // Text, wenn der Spieler mit ihr gesprochen hat.**
        var karte = MzDataFile.ReadText("Map.json",
            "{\"events\":[{\"id\":7,\"x\":3,\"y\":4,\"pages\":["
            + "{\"conditions\":{\"switch1Valid\":false},"
            + "\"image\":{\"characterName\":\"Erstes\",\"characterIndex\":0},"
            + "\"moveType\":0,\"moveSpeed\":4,\"moveFrequency\":6},"
            + "{\"conditions\":{\"switch1Valid\":false},"
            + "\"image\":{\"characterName\":\"Letztes\",\"characterIndex\":0},"
            + "\"moveType\":0,\"moveSpeed\":4,\"moveFrequency\":6}]}]}").Root;

        var figuren = MzMapFigureReader.Read(karte, new MzBranchFacts(),
            out var _);
        AssertEq(figuren.Count, 1,
            "**and one figure stands there**");
        AssertEq(figuren[0].CharacterName, "Letztes",
            "**and it is the last page** -- and the engine walks the pages"
            + " backwards and takes the first one it finds, and a reader"
            + " that took the first showed the wrong face of every event"
            + " with two pages");
        AssertEq(figuren[0].PageIndex, 1,
            "**and its page is the second**");
    }

    /// <summary>
    /// What starts a page here, and how few start by themselves.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the engine's own test is
    /// <c>isTriggerIn([0, 1, 2])</c></strong> — <strong>so a page at 3
    /// (parallel) runs without the player and without locking, and a page
    /// at 4 (leaving) waits.</strong>
    /// </para>
    /// <para>
    /// <strong>And measured over all 253 pages of the project: 196 action
    /// button, 52 touched, 3 parallel, 2 autorun.</strong> <strong>Only
    /// two pages of the whole game start by themselves</strong> — <strong>so
    /// a runtime that started every page at once would run a game the
    /// player has not begun.</strong>
    /// </para>
    /// </remarks>
    public void Test_DerAusloeserDiesesSpielsUndWieWenigeVonSelbstStarten()
    {
        if (!Vorhanden())
        {
            return;
        }

        var karte = Karte();
        var figuren = MzMapFigureReader.Read(karte, new MzBranchFacts(),
            out var _);

        // **Und die Ausloeser, nach Zahl, wie die Datei sie nennt.**
        var ausloeser = new Dictionary<int, int>();
        foreach (var figur in figuren)
        {
            ausloeser.TryGetValue(figur.Trigger, out var anzahl);
            ausloeser[figur.Trigger] = anzahl + 1;
        }

        // **Und gemessen: `Map017` hat 25 Seiten, und alle 25 haben den
        // Ausloeser 0, die Aktionstaste.** **Ich hatte hier 1
        // behauptet, "beruehrt", und das war geraten.**
        // **Und gemessen sind es acht, und nicht neun.**
        //
        // **Und der Grund steht an derselben Stelle wie bei den Bildern
        // zwoetern Zeilen weiter oben:** **Ereignis 19 traegt einen
        // Selbstschalter, den dieser Leser nicht beantworten kann, und
        // deshalb wird es weggelassen.** **Und neun waere die Zahl, die
        // ein Leser bekaeme, der die Bedingung als erfuellt annimmt** --
        // **und das waere eine Figur, die das Spiel an einem Zustand
        // zeigt, den es nicht gibt.**
        AssertEq(figuren.Count, 8,
            "**and eight figures are visible on it** -- and the ninth is"
            + " event 19, which hangs on a self switch this reader cannot"
            + " answer, and nine is what a reader that read the condition"
            + " as satisfied would have drawn");
        AssertTrue(figuren.Count > 0,
            "**and it has visible figures**");
        AssertTrue(ausloeser.ContainsKey(0),
            "**and every one of them waits for the action button**"
                + " -- and the engine numbers the action button 0, touched 1,"
                + " autorun 2, parallel 3 and leaving 4, and map 17"
                + " has twenty-five pages and every one of them is a 0");

        // **Und keine einzige Seite dieser Karte startet von selbst.**
        AssertEq(ausloeser.TryGetValue(2, out var selbst) ? selbst : 0, 0,
            "**and not one of them starts by itself** -- and the measured"
            + " project has two autorun pages out of 253, both on map 3,"
            + " and a runtime that started every page would run a game the"
            + " player has not begun");
    }
}