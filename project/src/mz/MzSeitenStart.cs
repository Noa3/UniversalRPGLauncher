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
/// <item><description><strong>2 — Event Touch.</strong> The event walks
/// into the player.</description></item>
/// <item><description><strong>3 — Autorun.</strong>
/// <c>Game_Event.prototype.checkEventTriggerAuto</c> is
/// <c>if (this._trigger === 3) { this.start(); }</c>, and it runs every
/// frame.</description></item>
/// <item><description><strong>4 — Parallel Process.</strong> Not started
/// by this; the engine gives it its own interpreter
/// (<c>updateParallel</c>).</description></item>
/// </list>
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

    /// <summary>Event Touch: the event walking into the player.</summary>
    public const int AusloeserBeruehrtVorne = 2;

    /// <summary>Autorun: the page starts by itself, every frame.</summary>
    public const int AusloeserAutomatisch = 3;

    /// <summary>Parallel Process: it gets its own interpreter.</summary>
    public const int AusloeserParallel = 4;

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
    /// <remarks>
    /// <strong>And both touch triggers count.</strong> Measured at
    /// <c>Game_Player.prototype.checkEventTriggerTouch</c>, which hands
    /// <c>[1, 2]</c> to <c>startMapEvent</c> -- <strong>so a page the event
    /// triggers (2) is found by the same walk as one the player triggers
    /// (1).</strong>
    /// </remarks>
    public static bool StartetDurchBeruehrung(int pAusloeser)
        => pAusloeser == AusloeserBeruehrt
            || pAusloeser == AusloeserBeruehrtVorne;

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