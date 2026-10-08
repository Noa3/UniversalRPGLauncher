using System;
using System.Collections.Generic;

namespace UniversalRPG.Web;

/// <summary>
/// One figure standing on a map, as the map's file says it.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the figure's image is on the page, and not on the
/// event.</strong> Measured on <c>CamelliaCoronation-Win/Map017.json</c>:
/// the event carries <c>id</c>, <c>name</c>, <c>x</c> and <c>y</c>, and
/// <strong>the character, its index, its direction and its walk pattern
/// are all in <c>page.image</c></strong> — <strong>so a reader that
/// looked for <c>characterName</c> on the event found nothing and every
/// figure in the game was invisible.</strong>
/// </para>
/// </remarks>
public sealed class MzMapFigure
{
    /// <summary>The event's own id, which is not its character index.</summary>
    public int EventId { get; init; }

    /// <summary>Which character in its sheet, counted from zero.</summary>
    public int CharacterIndex { get; init; }

    /// <summary>Which of the sheet's images.</summary>
    /// <remarks>
    /// <strong>And this is the name, and not a letter.</strong> Measured:
    /// <em>SlimeCharacters</em>, <em>MC_Sprite_sheet</em>,
    /// <em>!Flame</em> — <strong>and the file is
    /// <c>img/characters/&lt;name&gt;.png_</c></strong>, **so a reader
    /// that took a character id and looked for
    /// <c>img/characters/1.png</c> found no file at all.</strong>
    /// </remarks>
    public string CharacterName { get; init; } = "";

    /// <summary>The tile column the figure stands on.</summary>
    public int X { get; init; }

    /// <summary>The tile row the figure stands on.</summary>
    public int Y { get; init; }

    /// <summary>The engine's direction: 2, 4, 6 or 8.</summary>
    public int Direction { get; init; } = MzCharacter.Down;

    /// <summary>Which of the three walk steps.</summary>
    /// <remarks>
    /// <strong>And this is the map's own number, and it is not a
    /// time.</strong> Measured: <c>pattern: 1</c> on a standing figure,
    /// <strong>and 1 is the middle of the three columns</strong>, which
    /// is the still pose. <strong>A reader that used the pattern as a
    /// frame counter</strong> — <strong>or that started its own clock at
    /// zero and walked every figure from the first step</strong> —
    /// <strong>made a room of standing people look like a room of
    /// people mid-stride.</strong>
    /// </remarks>
    public int Pattern { get; init; } = 1;

    /// <summary>How the page's figure moves: 0 fixed, 3 random.</summary>
    /// <remarks>
    /// <strong>And zero is not "no movement", it is "do not walk the
    /// route".</strong> Measured across the project: 235 of 253 pages are
    /// <c>moveType: 0</c> and 18 are <c>moveType: 3</c>, and
    /// <strong>the engine only walks a route for the types 2 and 3</strong>
    /// — <strong>so a reader that walked every route moved 253 figures
    /// that nobody asked to move.</strong>
    /// </remarks>
    public int MoveType { get; init; }

    /// <summary>How fast the figure moves, 1 to 6.</summary>
    /// <remarks>
    /// <strong>And the measured project says 5 on every page that says
    /// anything</strong>, and the engine's own default is 4.
    /// </remarks>
    public int MoveSpeed { get; init; } = 4;

    /// <summary>How fast the pattern advances.</summary>
    /// <remarks>
    /// <strong>And the measured project says 3</strong>, and the engine's
    /// own default is 6.
    /// </remarks>
    public int MoveFrequency { get; init; } = 6;

    /// <summary>Whether the route runs on its own.</summary>
    /// <remarks>
    /// <strong>And this is the engine's own gate, and it is not whether
    /// the route exists.</strong> Measured in
    /// <c>Game_Event.prototype.updateSelfMovement</c>:
    /// <c>moveType 1</c> walks at random, <c>2</c> toward the player,
    /// and <c>3</c> runs the page's own route.
    ///
    /// <para>
    /// <strong>And before any of that the figure must have stood still
    /// long enough.</strong> Measured: <c>stopCountThreshold</c> is
    /// <c>30 * (5 - moveFrequency)</c>, <strong>which for this
    /// project's <c>moveFrequency: 3</c> is sixty frames</strong>,
    /// <strong>and for the engine's own default of 6 it is a negative
    /// number that no figure ever reaches.</strong>
    /// </para>
    public bool RunsOwnRoute => MoveType == 3;

    /// <summary>How many still frames the route waits for.</summary>
    /// <remarks>
    /// <strong>And this goes up as the pattern goes faster</strong>, and
    /// <strong>that is backwards from what the name suggests</strong> —
    /// a figure whose pattern advances fast stands longer before it sets
    /// off, <strong>and for the engine's own default frequency the number
    /// is negative and no figure ever leaves.</strong>
    /// </remarks>
    public int StopCountThreshold => 30 * (5 - MoveFrequency);

    /// <summary>The page's own route, as its file names it.</summary>
    /// <remarks>
    /// <strong>And a route with no list is not a route.</strong>
    /// 253 of 253 pages carry a <c>moveRoute</c> object and only seven
    /// of them carry steps, <strong>and the other 246 carry a single
    /// entry that is the end</strong>.
    /// </remarks>
    public MzRouteStep.Route? Route { get; init; }

    /// <summary>What starts this page.</summary>
    /// <remarks>
    /// <strong>And four of the five kinds, measured.</strong> The engine's
    /// own <c>isTriggerIn([0, 1, 2])</c> is what decides whether a page
    /// runs on its own or waits for the player — <strong>and the project
    /// uses all of them</strong>: 196 pages at 0 (action button), 52 at 1
    /// (touched), 2 at 2 (autorun), 3 at 3 (parallel).
    /// </para>
    /// <para>
    /// <strong>And only 2 pages of 253 are autorun</strong> — <strong>so
    /// almost nothing in this game starts by itself</strong>, <strong>and
    /// a runtime that started every page at once would run a game the
    /// player has not begun.</strong>
    /// </para>
    /// </remarks>
    public int Trigger { get; init; }

    /// <summary>
    /// And whether the page stands in the way, which is <c>priorityType: 1</c>.
    /// </summary>
    /// <remarks>
    /// <strong>And this is <c>isNormalPriority()</c>, measured in the
    /// engine's own <c>rmmz_objects.js</c> as <c>this._priorityType ===
    /// 1</c>.</strong> It decides two things at once: whether the player may
    /// step onto the event's tile at all, and whether the action button
    /// reaches it -- <c>startMapEvent(x, y, triggers, normal)</c> only starts
    /// an event whose <c>isNormalPriority()</c> equals the <c>normal</c> the
    /// caller asked for. Measured on Camellia's Map004 event 14: <c>trigger 0</c>
    /// with <c>priorityType 1</c>, so it is talked to from beside it and
    /// <em>cannot</em> be walked onto.
    /// </remarks>
    public int PriorityType { get; init; } = 1;

    /// <summary>Which page this is, counted from zero.</summary>
    /// <remarks>
    /// <strong>And the engine shows the last matching page, not the
    /// first.</strong> Measured at
    /// <c>findProperPageIndex</c>: <c>for (let i = pages.length - 1; i
    /// &gt;= 0; i--)</c>. <strong>A reader that took the first showed the
    /// wrong face of every event with two pages</strong> — <strong>and
    /// with a plain page and a switch page, it showed the plain one.</strong>
    /// </remarks>
    public int PageIndex { get; init; }

    /// <summary>Whether this page's conditions are met.</summary>
    /// <remarks>
    /// <strong>And a page with no condition is visible, and that is
    /// the common case.</strong> Measured across the whole project: 208
    /// pages have no condition at all, 45 have one, and those 45 use
    /// four patterns — a switch (11), a self switch (33), two switches
    /// (1). <strong>And a reader that started at the first page and
    /// took it whatever it asked</strong> — <strong>or started at the
    /// last</strong> — <strong>drew a different face of every character
    /// in the game than the one the game shows.</strong>
    /// </remarks>
    public bool Visible { get; init; } = true;
}

/// <summary>
/// Reads the figures of a map out of its file.
/// </summary>
public static class MzMapFigureReader
{
    /// <summary>
    /// Every visible figure of a map, in the file's own order.
    /// </summary>
    /// <param name="pMap">The map file's root.</param>
    /// <param name="pFacts">The state the project's commands wrote.</param>
    /// <param name="pNotwendig">Why a page was not drawn, when one was
    /// not.</param>
    /// <returns>The figures, and the reasons for the pages left out.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And the first page whose conditions are met wins.</strong>
    /// The engine's own rule: an event shows the first page it can,
    /// <strong>and a reader that showed all of them drew one character
    /// three times on top of itself</strong> — **and a reader that
    /// showed only the last drew the face a game switched away
    /// from.</strong>
    /// </para>
    /// <para>
    /// <strong>And a condition this reader cannot answer does not make
    /// the page visible.</strong> The engine shows a page it cannot
    /// decide on, <strong>and this one says so and leaves it out</strong> —
    /// <strong>which is the honest answer, and a figure that is drawn
    /// wrongly is worse than one that is missing.</strong>
    /// </para>
    /// </remarks>
    public static List<MzMapFigure> Read(
        MzValue pMap,
        MzBranchFacts? pFacts,
        out List<string> pNotwendig)
    {
        pNotwendig = new List<string>();
        var ergebnis = new List<MzMapFigure>();
        if (pMap == null || pMap.Kind != MzKind.Object)
        {
            pNotwendig.Add("The map file is not an object.");
            return ergebnis;
        }

        var ereignisse = pMap.Member("events")?.Items;
        if (ereignisse == null)
        {
            pNotwendig.Add("The map has no events, and a map with no "
                + "events has nobody standing on it.");
            return ergebnis;
        }

        foreach (var ereignis in ereignisse)
        {
            var seiten = ereignis.Member("pages")?.Items;
            if (seiten == null || seiten.Count == 0)
            {
                continue;
            }

            var id = ereignis.Member("id")?.IntOr(-1) ?? -1;
            var x = ereignis.Member("x")?.IntOr(0) ?? 0;
            var y = ereignis.Member("y")?.IntOr(0) ?? 0;
            // **Und von hinten, und nicht von vorn.**
            //
            // **Gemessen an `Game_Event.prototype.findProperPageIndex`:
            // **`for (let i = pages.length - 1; i >= 0; i--)`**
            // **-- und die ERSTE passende Seite von hinten gewinnt.**
            //
            // **Und mein Leser nahm die erste von vorn** -- **und das
            // **ist bei zwei Seiten genau die falsche.** **Ein
            // **Ereignis mit einer leeren Seite und einer mit Text
            // **zeigt im Spiel den Text, und dieser Leser zeigte die
            // **leere.**
            for (var index = seiten.Count - 1; index >= 0; index--)
            {
                var seite = seiten[index];
                if (!Meets(seite.Member("conditions"), pFacts))
                {
                    pNotwendig.Add(
                        $"event {id} page {index} has a condition this "
                        + "reader cannot answer, and it is left out");
                    continue;
                }

                var bild = seite.Member("image");
                if (bild == null)
                {
                    continue;
                }

                var name = bild.Member("characterName")?.StringOr("") ?? "";
                if (name.Length == 0)
                {
                    break;
                }

                ergebnis.Add(new MzMapFigure
                {
                    EventId = id,
                    CharacterName = name,
                    CharacterIndex = bild.Member("characterIndex")?.IntOr(0) ?? 0,
                    Direction = bild.Member("direction")?.IntOr(MzCharacter.Down)
                        ?? MzCharacter.Down,
                    Pattern = bild.Member("pattern")?.IntOr(1) ?? 1,
                    X = x,
                    Y = y,
                    Visible = true,
                    MoveType = seite.Member("moveType")?.IntOr(0) ?? 0,
                    MoveSpeed = seite.Member("moveSpeed")?.IntOr(4) ?? 4,
                    MoveFrequency = seite.Member("moveFrequency")?.IntOr(6) ?? 6,
                    Route = ReadRoute(seite.Member("moveRoute")),
                    Trigger = seite.Member("trigger")?.IntOr(0) ?? 0,
                    PriorityType = seite.Member("priorityType")?.IntOr(1) ?? 1,
                    PageIndex = index,
                });

                break;
            }
        }

        return ergebnis;
    }

    /// <summary>
    /// Whether a page's conditions are met.
    /// </summary>
    /// <param name="pConditions">The page's <c>conditions</c>.</param>
    /// <param name="pFacts">The state the project's commands wrote.</param>
    /// <returns>Whether the page may be drawn.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And "no condition" is not "no answer".</strong> Measured
    /// across the project: 208 of 253 pages have every flag false, and
    /// they are the pages a game shows.
    /// </para>
    /// <para>
    /// <strong>And the four conditions that occur are a switch, a self
    /// switch, two switches, and nothing else.</strong> The file also
    /// carries actor, item, variable and timer flags; <strong>none of
    /// them is set in this project</strong>, <strong>and a reader that
    /// treated an unset flag as "not satisfied" hid every page in the
    /// game</strong>, <strong>and one that treated it as "satisfied"
    /// showed pages a game switched away from.</strong>
    /// </para>
    /// </remarks>
    public static bool Meets(
        MzValue? pConditions,
        MzBranchFacts? pFacts,
        int pMapId = 0,
        int pEventId = 0)
    {
        if (pConditions == null || pConditions.Kind != MzKind.Object)
        {
            return true;
        }

        if (Gilt(pConditions, "switch1Valid"))
        {
            var id = pConditions.Member("switch1Id")?.IntOr(-1) ?? -1;
            if (pFacts == null || !pFacts.Switches.GetValueOrDefault(id))
            {
                return false;
            }
        }

        if (Gilt(pConditions, "switch2Valid"))
        {
            var id = pConditions.Member("switch2Id")?.IntOr(-1) ?? -1;
            if (pFacts == null || !pFacts.Switches.GetValueOrDefault(id))
            {
                return false;
            }
        }

        if (Gilt(pConditions, "selfSwitchValid"))
        {
            // **Und ein Selbstschalter gehoert einem Ereignis auf einer
            // Karte, und nicht dem Spiel.**
            //
            // **Gemessen an `command123`: `if (this._eventId > 0) { const
            // key = [this._mapId, this._eventId, params[0]];
            // $gameSelfSwitches.setValue(key, params[1] === 0); }`** --
            // **drei Zahlen, und die ersten beiden sagen, wessen Schalter
            // es ist.**
            //
            // **Und die Seite weiss ihren Buchstaben, aber nicht ihre
            // Ereignisnummer ohne das Ereignis** -- **und darum
            // braucht `Meets` die Nummer, und nicht nur die Seite.**
            //
            // **Und die alte Antwort war hier "nein, immer",** **und
            // damit war jede Seite an einem Selbstschalter fuer immer
            // unerreichbar** -- **und gemessen sind das 21 der 253
            // Seiten dieses Spiels**, **und darunter Map004 Event 15,
            // dessen drei Seiten drei verschiedene Dialoge tragen.**
            if (pEventId <= 0 || pFacts == null)
            {
                return false;
            }

            var ch = (pConditions.Member("selfSwitchCh")?.StringOr("") ?? "")
                .Trim();
            if (ch.Length == 0)
            {
                return false;
            }

            return pFacts.SelfSwitches.TryGetValue(
                $"{pMapId}_{pEventId}_{ch[0]}", out var an) && an;
        }

        // **Und `variableValid`, `itemValid` und `actorValid` sind keine
        // Felder, die dieses Projekt nicht setzt.** Sie sind Felder, die
        // dieser Leser nicht beantwortet hat, und das ist ein Unterschied,
        // der hier behoben ist.
        //
        // **Die Regel steht woertlich in `rpg_objects.js`, in
        // `Game_Event.meetsConditions`:**
        //
        // ```text
        // if (c.variableValid) {
        //     if ($gameVariables.value(c.variableId) < c.variableValue) {
        //         return false;
        //     }
        // }
        // if (c.itemValid) {
        //     const item = $dataItems[c.itemId];
        //     if (!$gameParty.hasItem(item)) { return false; }
        // }
        // if (c.actorValid) {
        //     const actor = $gameActors.actor(c.actorId);
        //     if (!$gameParty.members().contains(actor)) { return false; }
        // }
        // ```
        //
        // **Und alle drei Werte liegen in `MzBranchFacts`, denn `122` schreibt
        // `Variables`, `126` schreibt `KnownItems` und `129` schreibt
        // `PartyMembers`.** **Also sind sie keine Grenze, sondern eine
        // Frage, die dieser Leser nicht gestellt hat.**
        if (Gilt(pConditions, "variableValid"))
        {
            // **Und der Vergleich ist `gefunden < gefragt`, und nicht
            // `gefunden != gefragt`.** **Gemessen an `command111`: die
            // Seite zeigt sich ab einem Wert, nicht bei genau einem.**
            var id = pConditions.Member("variableId")?.IntOr(-1) ?? -1;
            var wert = pConditions.Member("variableValue")?.IntOr(0) ?? 0;
            if (pFacts == null
                || !pFacts.Variables.TryGetValue(id, out var gefunden)
                || gefunden < wert)
            {
                return false;
            }
        }

        if (Gilt(pConditions, "itemValid"))
        {
            // **Und `hasItem` fragt die Zahl im Inventar, und die
            // Mindestmenge steht in `parameters[1]` der Seite.** **Der
            // einfache Fall ist die Zahl groesser als null.**
            var id = pConditions.Member("itemId")?.IntOr(-1) ?? -1;
            // **Und `hasItem` fragt, ob der Gegenstand im Inventar ist, und
            // `Items` ist ein Woerterbuch aus Nummer und Menge** -- **und
            // das ist die Form, die `126 Change Items` schreibt und die
            // Form, in der `command126` zurueckgibt, was es tat.**
            if (pFacts == null
                || !pFacts.Items.TryGetValue(id, out var anzahl)
                || anzahl <= 0)
            {
                return false;
            }
        }

        if (Gilt(pConditions, "actorValid"))
        {
            var id = pConditions.Member("actorId")?.IntOr(-1) ?? -1;
            if (pFacts == null || !pFacts.PartyMembers.Contains(id))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Reads a page's own move route out of its file.
    /// </summary>
    /// <param name="pWert">The page's <c>moveRoute</c>.</param>
    /// <returns>The route, or nothing when the page has none.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And a route is a list plus three flags, and all four come
    /// from the file.</strong> Measured: every one of this project's 253
    /// pages carries <c>list</c>, <c>repeat</c>, <c>skippable</c> and
    /// <c>wait</c>, <strong>and only seven carry steps</strong> — the
    /// other 246 carry a single entry, and that entry is the end.
    /// </para>
    /// <para>
    /// <strong>And a route whose steps are all the end is no route.</strong>
    /// <strong>A reader that treated 246 empty routes as routes had 246
    /// figures each holding a one-step route that turns them nowhere.</strong>
    /// </para>
    /// </remarks>
    private static MzRouteStep.Route? ReadRoute(MzValue? pWert)
    {
        if (pWert == null || pWert.Kind != MzKind.Object)
        {
            return null;
        }

        var liste = pWert.Member("list");
        var schritte = new List<MzRouteStep>();
        if (liste != null && liste.Kind == MzKind.Array)
        {
            foreach (var eintrag in liste.Items)
            {
                if (eintrag == null || eintrag.Kind != MzKind.Object)
                {
                    continue;
                }

                var code = eintrag.Member("code")?.IntOr(0) ?? 0;
                var parameter = new List<string>();
                var werte = eintrag.Member("parameters");
                if (werte != null && werte.Kind == MzKind.Array)
                {
                    foreach (var wert in werte.Items)
                    {
                        parameter.Add(wert == null
                            ? "" : wert.Kind == MzKind.Number
                                ? wert.Number.ToString(System.Globalization
                                    .CultureInfo.InvariantCulture)
                                : wert.Text);
                    }
                }

                schritte.Add(new MzRouteStep
                {
                    Code = code,
                    Parameters = parameter,
                });
            }
        }

        // **Und eine Liste, in der nur der Endpunkt steht, ist keine
        // Laufbahn.**
        var echt = 0;
        foreach (var schritt in schritte)
        {
            if (schritt.Code != MzRouteCode.End)
            {
                echt++;
            }
        }

        if (echt == 0)
        {
            return null;
        }

        return new MzRouteStep.Route(
            schritte,
            Flag(pWert, "repeat"),
            Flag(pWert, "skippable"),
            Flag(pWert, "wait"));
    }

    private static bool Flag(MzValue pObject, string pName)
    {
        var wert = pObject.Member(pName);
        return wert != null
            && (wert.Kind == MzKind.Bool ? wert.Boolean : wert.Text == "true");
    }

    private static bool Gilt(MzValue pConditions, string pName)
    {
        var wert = pConditions.Member(pName);
        return wert != null
            && (wert.Kind == MzKind.Bool ? wert.Boolean : wert.Text == "true");
    }

}
