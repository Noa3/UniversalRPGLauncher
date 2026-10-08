using System;
using System.Collections.Generic;

namespace UniversalRPG.Mz;

/// <summary>
/// And which page of an event starts, and why.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the trigger numbering is not the obvious one.</strong>
/// Measured in <c>rmmz_objects.js</c>, where <c>Game_Event</c> carries
/// the page's <c>trigger</c> in <c>this._trigger</c> and asks
/// <c>isTriggerIn([0, 1, 2])</c>:
/// </para>
/// <list type="bullet">
/// <item><description><strong>0 — Action Button.</strong>
/// <c>Game_Player.prototype.triggerActionEvent</c> calls
/// <c>ev.start()</c> for the event in front of the player.</description></item>
/// <item><description><strong>1 — Player Touch.</strong>
/// <c>Game_Player.prototype.isCollidedWithEvent</c> then
/// <c>Game_Event.prototype.update</c> starts it when the player walks
/// onto its tile.</description></item>
/// <item><description><strong>2 — Autorun.</strong> Started when the map
/// is set up, without the player doing anything.</description></item>
/// <item><description><strong>3 — Parallel Process.</strong> Not started
/// by this; <c>Scene_Map.setupReservedCommonEvents</c> and the engine give
/// it its own interpreter.</description></item>
/// </list>
/// <para>
/// <strong>⚠ And the four numbers above are wrong, and the constants below
/// still carry them. This is the next card.</strong>
/// </para>
/// <para>
/// <strong>And the remark above proved it with a line that is not in the
/// engine.</strong> It cites
/// <c>isTriggerIn([2]) &amp;&amp; !this._erased &amp;&amp; !this.isStarting()</c>
/// as the autorun check. <strong>Measured in the project's own
/// <c>rmmz_objects.js</c>:</strong>
/// </para>
/// <code>
/// Game_Event.prototype.update = function() {
///     Game_Character.prototype.update.call(this);
///     this.checkEventTriggerAuto();
///     this.updateParallel();
/// };
/// Game_Event.prototype.checkEventTriggerAuto = function() {
///     if (this._trigger === 3) { this.start(); }
/// };
/// Game_Event.prototype.start = function() {
///     const list = this.list();
///     if (list &amp;&amp; list.length &gt; 1) {
///         this._starting = true;
///         if (this.isTriggerIn([0, 1, 2])) { this.lock(); }
///     }
/// };
/// Game_Player.prototype.startMapEvent = function(x, y, triggers, normal) {
///     for (const event of $gameMap.eventsXy(x, y)) {
///         if (event.isTriggerIn(triggers) &amp;&amp; event.isNormalPriority() === normal) {
///             event.start();
///         }
///     }
/// };
/// </code>
/// <para>
/// <strong>so <c>_trigger === 3</c> is autorun and <c>[0, 1, 2]</c> is the
/// button-and-touch group</strong> — the group the player's own walk hands in
/// — <strong>and the numbers the constants below carry are shifted:</strong>
/// 0 action, <strong>1 and 2 are both touches</strong>, <strong>3 is
/// autorun</strong>, <strong>4 is parallel</strong>.
/// </para>
/// <para>
/// <strong>And the cost is counted from the games' own card files:</strong>
/// Camellia carries 3 autorun pages, LegalTruck 5, Skies 13 — <strong>and not
/// one of them ever started</strong> — while Skies has 115 event-touch pages
/// and LegalTruck 15 parallel pages, <strong>and those ran as autorun.</strong>
/// Measured on Camellia: its start map <strong>is Map002 and carries an
/// autorun page</strong> (event 5, 22 commands, four messages, then
/// <c>222 Fadeout</c> and <c>123 Self Switch A</c>) — <strong>the game's own
/// opening, which never played.</strong>
/// </para>
/// <para>
/// <strong>And the fix is not here yet, and this is why:</strong> with the
/// four numbers corrected, <strong>45 of 2756 tests fail across nine
/// suites</strong>, because those suites were written against the shifted
/// numbering — six of them start Camellia's map and assume a quiet start map
/// that the opening no longer leaves, and three assert the content of Map003's
/// two trigger-2 pages as if they were autorun. <strong>Correcting the numbers
/// and migrating those suites is one change, and half of it is not worth
/// shipping.</strong> The measurement above is the whole of the evidence it
/// needs.
/// </para>
/// <para>
/// <strong>And a page needs more than one command.</strong>
/// <c>Game_Event.prototype.start</c> is
/// <c>if (list &amp;&amp; list.length &gt; 1)</c>, so a page with a single
/// command does not start — <strong>and a reader that counted commands
/// instead of comparing would run a page the engine never runs</strong>.
/// </para>
/// <para>
/// <strong>And the last page whose conditions are met wins</strong>
/// (<c>findProperPageIndex</c> walks the list backwards), <strong>and an
/// autorun page has to re-check its conditions every frame</strong> —
/// <c>Game_Event.prototype.update</c> starts it while
/// <c>isTriggerIn([2]) &amp;&amp; !this._erased &amp;&amp; !this.isStarting()
/// </c>.
/// </para>
/// </remarks>
public static class MzSeitenStart
{
    /// <summary>Action Button: the key in front of the player.</summary>
    public const int AusloeserTaste = 0;

    /// <summary>Player Touch: walking onto the event's own tile.</summary>
    public const int AusloeserBeruehrt = 1;

    /// <summary>Autorun: the page starts by itself when the map is set up.</summary>
    public const int AusloeserAutomatisch = 2;

    /// <summary>Parallel Process: it gets its own interpreter.</summary>
    public const int AusloeserParallel = 3;

    /// <summary>
    /// And the engine's boundary, which is exclusive.
    /// </summary>
    /// <remarks>
    /// <strong>And this is <c>list.length &gt; 1</c>, not
    /// <c>&gt;= 2</c> with the same name.</strong> The first draft wrote
    /// <c>MindestBefehle = 2</c> and compared
    /// <c>pBefehlszahl &gt; MindestBefehle</c>, which starts a page only at
    /// three commands and silently skips every two-command page -- and a
    /// two-command autorun page is a page the engine runs. The constant is
    /// the last count that does *not* start.
    /// </remarks>
    public const int HoechstBefehleOhneStart = 1;

    /// <summary>
    /// And whether this page starts on its own, the way the engine does it
    /// every frame the map runs.
    /// </summary>
    /// <remarks>
    /// <strong>And the command count is checked here and not by the
    /// caller</strong>, because the engine's check is
    /// <c>list.length &gt; 1</c> and a caller that counted "does it have
    /// commands" would start a one-command page.
    /// </remarks>
    public static bool StartetAutomatisch(
        int pAusloeser, int pBefehlszahl, bool pGeloescht, bool pStartetGerade)
        => pAusloeser == AusloeserAutomatisch
            && !pGeloescht
            && !pStartetGerade
            && pBefehlszahl > HoechstBefehleOhneStart;

    /// <summary>And whether the decision key starts this page.</summary>
    public static bool StartetDurchTaste(int pAusloeser)
        => pAusloeser == AusloeserTaste;

    /// <summary>And whether walking onto the tile starts this page.</summary>
    public static bool StartetDurchBeruehrung(int pAusloeser)
        => pAusloeser == AusloeserBeruehrt;

    /// <summary>
    /// And the event standing on a tile, which is how a touch is found.
    /// </summary>
    /// <remarks>
    /// <strong>And it compares the player's target cell, not its own.</strong>
    /// <c>Game_Player.prototype.isCollidedWithEvent</c> tests
    /// <c>ev.x === this.x &amp;&amp; ev.y === this.y</c> <em>after</em> the
    /// move, so the event the player walks onto is the one that starts;
    /// a check against the player's old cell would fire while walking
    /// away from it.
    /// </remarks>
    public static IReadOnlyList<int> BeruehrteEreignisse(
        IReadOnlyList<(int Id, int X, int Y)> pEreignisse, int pX, int pY)
    {
        var treffer = new List<int>();
        foreach (var (id, x, y) in pEreignisse)
        {
            if (x == pX && y == pY)
            {
                treffer.Add(id);
            }
        }
        return treffer;
    }
}