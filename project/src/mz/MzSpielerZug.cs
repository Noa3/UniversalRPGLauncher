using System;
using System.Collections.Generic;

namespace UniversalRPG.Mz;

/// <summary>
/// And the player turn: which direction a key asks for, and what happens next.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is the web engines' own order, written out.</strong>
/// Measured in <c>rmmz_objects.js</c>:
/// <code>
/// Game_Player.prototype.moveStraight = function(d) {
///     if (this.canPass(this.x, this.y, d)) { this._followers.updateMove(); }
///     Game_Character.prototype.moveStraight.call(this, d);
/// };
/// </code>
/// and <c>Game_Player.prototype.updateMove</c> turns the held direction into
/// one step per update:
/// <code>
/// var direction = this._direction || this._lastDirection;
/// if (this.isTransferring() || this.isMapPassable(direction)) { this.moveStraight(direction); }
/// </code>
/// </para>
/// <para>
/// <strong>And three details decide whether a player can walk a map.</strong>
/// </para>
/// <list type="bullet">
/// <item><description><strong>The key sets the facing even when the step
/// fails.</strong> A player pressing left against a wall turns to face left,
/// and every engine does it; <strong>a runtime that refuses the step and
/// keeps the old facing leaves the sprite looking the wrong way after
/// every bump into a wall.</strong></description></item>
/// <item><description><strong>The step leaves the cell that is being
/// left.</strong> <c>Game_Character.prototype.moveStraight</c> checks
/// <c>Game_Map.isPassable(this.x + dx, this.y + dy, d)</c> for the
/// <em>target</em>, and the engine's own comment on the 101 says a blocked
/// step changes nothing at all.</description></item>
/// <item><description><strong>An event standing in the cell blocks the
/// step</strong>, through <c>isCollidedEvents</c>, and that is not a
/// tile property at all.</description></item>
/// </list>
/// </remarks>
public static class MzSpielerZug
{
    /// <summary>The direction each input action asks for.</summary>
    /// <remarks>
    /// These are the engine's own numbers: 2 down, 4 left, 6 right, 8 up.
    /// <strong>The confirmation key has no direction</strong>, because the
    /// engines do not turn the player on it either.
    /// </remarks>
    public static int? RichtungFuer(UniversalRPG.Rm2k.Input.Rm2kInputAction pAktion)
        => pAktion switch
        {
            UniversalRPG.Rm2k.Input.Rm2kInputAction.MoveDown => MzKachelDurchgang.Unten,
            UniversalRPG.Rm2k.Input.Rm2kInputAction.MoveLeft => MzKachelDurchgang.Links,
            UniversalRPG.Rm2k.Input.Rm2kInputAction.MoveRight => MzKachelDurchgang.Rechts,
            UniversalRPG.Rm2k.Input.Rm2kInputAction.MoveUp => MzKachelDurchgang.Oben,
            _ => null,
        };

    /// <summary>And the column and row a step in that direction leaves to.</summary>
    public static (int Dx, int Dy) Versatz(int pRichtung) => pRichtung switch
    {
        MzKachelDurchgang.Unten => (0, 1),
        MzKachelDurchgang.Links => (-1, 0),
        MzKachelDurchgang.Rechts => (1, 0),
        MzKachelDurchgang.Oben => (0, -1),
        _ => (0, 0),
    };

    /// <summary>
    /// And whether a cell is inside the map at all.
    /// </summary>
    /// <remarks>
    /// <c>Game_Map.prototype.isValid</c> is
    /// <c>x &gt;= 0 &amp;&amp; x &lt; this.width() &amp;&amp; y &gt;= 0 &amp;&amp; y &lt; this.height()</c>,
    /// and it is checked before passability. <strong>A reader that checked
    /// passability alone would read past the end of the tile array at the
    /// map edge</strong>, which on a looping map looks like a wall and on
    /// a normal map looks like a crash.
    /// </remarks>
    public static bool IstGueltig(int pX, int pY, int pBreite, int pHoehe)
        => pX >= 0 && pX < pBreite && pY >= 0 && pY < pHoehe;

    /// <summary>
    /// And what one key press does to the player.
    /// </summary>
    /// <param name="pX">The player's column.</param>
    /// <param name="pY">The player's row.</param>
    /// <param name="pRichtung">The direction the key asks for.</param>
    /// <param name="pBreite">The map's width in tiles.</param>
    /// <param name="pHoehe">The map's height in tiles.</param>
    /// <param name="pFlags">The tileset's flag array.</param>
    /// <param name="pKacheln">
    /// The tile ids on the <em>target</em> cell, top layer first.
    /// </param>
    /// <param name="pEventDort">Where the events on the target cell stand.</param>
    /// <returns>
    /// Whether the player stepped, -- **and the facing is set by the
    /// caller either way, because the engines turn the player on a blocked
    /// key too.**
    /// </returns>
    public static bool Schritt(
        int pX, int pY, int pRichtung, int pBreite, int pHoehe,
        IReadOnlyList<UniversalRPG.Web.MzValue> pFlags,
        IReadOnlyList<int> pKacheln,
        IReadOnlyCollection<(int X, int Y)>? pEventDort = null)
    {
        var (dx, dy) = Versatz(pRichtung);
        var zielX = pX + dx;
        var zielY = pY + dy;
        if (!IstGueltig(zielX, zielY, pBreite, pHoehe))
        {
            return false;
        }
        if (pEventDort != null)
        {
            foreach (var (x, y) in pEventDort)
            {
                // `isCollidedEvents` compares the target cell, and counts an
                // event as blocking only when it is not "through".
                if (x == zielX && y == zielY)
                {
                    return false;
                }
            }
        }
        return MzKachelDurchgang.IstBegehbar(pFlags, pKacheln, pRichtung);
    }
}