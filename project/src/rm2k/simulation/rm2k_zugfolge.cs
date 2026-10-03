using System;
using System.Collections.Generic;

namespace UniversalRPG.Rm2k.Simulation;

/// <summary>
/// And whose turn it is, and who acts next.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And <c>BattleTurn</c> and <c>BattlePhase</c> were written
/// once and read by nobody.</strong> -- <strong>That is the fourth
/// time in this repository that a state field existed and nothing
/// reached it</strong>, -- <strong>and the same shape as
/// <c>IsTransferPending</c>, <c>TroopMembers</c> and
/// <c>BattleSubcommand</c> before it.</strong>
/// </para>
/// <para>
/// <strong>And the reference's rule is the order of the troop.</strong>
/// --
/// <strong>A troop's <c>members</c> array is in the order the game
/// wrote them</strong>, -- <strong>and <c>1006 ForceFlee</c> and the
/// troop page's <c>ConditionalBranch_B</c> are the game's own
/// statements about it.</strong>
/// </para>
/// <para>
/// <strong>And a round is one pass over the members</strong>, --
/// <strong>and the first strike decides whether it starts with a
/// monster or with a hero</strong>, -- <strong>which is the
/// encounter command's own fifth parameter.</strong>
/// </para>
/// </remarks>
public static class Rm2kZugfolge
{
    /// <summary>
    /// And the index of whoever acts next, and -1 when the battle is
    /// over.
    /// </summary>
    /// <param name="pZustand">The state.</param>
    /// <returns>
    /// The index into <c>TroopMembers</c>, and -1 when nobody acts.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <strong>And it skips a monster that cannot act</strong>, --
    /// <strong>because a dead or hidden one has already had its
    /// turn</strong>, -- <strong>and a reader that stopped on the
    /// first dead monster would end the battle early.</strong>
    /// </para>
    /// <para>
    /// <strong>And it never loops for ever</strong>: -- <strong>it
    /// walks the members once and gives up if none of them can
    /// act</strong>.
    /// </para>
    /// </remarks>
    public static int Naechster(
        GameSimulationState pZustand)
    {
        if (pZustand == null || !pZustand.IsBattleActive)
        {
            return -1;
        }

        var anzahl = pZustand.TroopMembers.Count;
        if (anzahl == 0)
        {
            return -1;
        }

        var start = pZustand.BattleTurn % anzahl;
        for (var schritt = 0; schritt < anzahl; schritt++)
        {
            var index = (start + schritt) % anzahl;
            if (pZustand.CanMonsterAct(index))
            {
                return index;
            }
        }

        // **Und  niemand  kann  handeln  --  und  das  ist  ein
        //  Ergebnis  und  kein  Fehler.**
        //
        // **Und  der  Aufrufer  erhaelt  -1  und  weiss,  dass  die
        //  Runde  vorbei  ist.**
        return -1;
    }

    /// <summary>
    /// And it advances to the next turn.
    /// </summary>
    /// <param name="pZustand">The state.</param>
    /// <returns>The index that will act next.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And the turn advances by one and not by one living
    /// monster</strong>, -- <strong>because the reference walks the
    /// member list</strong> -- <strong>and skipping the dead ones is
    /// what <see cref="Naechster"/> does</strong>.
    /// </para>
    /// </remarks>
    public static int Ruecke(GameSimulationState pZustand)
    {
        var anzahl = pZustand.TroopMembers.Count;
        if (anzahl > 0)
        {
            pZustand.BattleTurn = (pZustand.BattleTurn + 1) % anzahl;
        }

        return Naechster(pZustand);
    }
}
