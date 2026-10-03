using System;
using System.Collections.Generic;

namespace UniversalRPG.Rm2k.Simulation;

/// <summary>
/// And which monster a hero's command is aimed at.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And <c>CurrentTargetIndex</c> and
/// <c>TargetsSingleEnemy</c> were read by the interpreter and written
/// by nothing but a test.</strong> -- <strong>That is the fifth field
/// in this repository of that shape</strong>, -- <strong>after
/// <c>IsTransferPending</c>, <c>TroopMembers</c>,
/// <c>BattleSubcommand</c> and <c>BattleTurn</c>.</strong>
/// </para>
/// <para>
/// <strong>And the reference's rule is the branch's own
/// condition.</strong> -- <strong><c>13310 Battle Branch</c> takes
/// a target index and a "one enemy or all" flag</strong>, -- <strong>
/// and the target is chosen in the battle screen and the branch
/// asks about it.</strong>
/// </para>
/// <para>
/// <strong>And a target that cannot be hit is refused</strong>, --
/// <strong>because aiming a strike at a fallen monster is a command
/// the game never issued</strong>, -- <strong>and it would move the
/// turn without anything happening.</strong>
/// </para>
/// </remarks>
public static class Rm2kZielwahl
{
    /// <summary>
    /// And the monsters a hero may aim at right now.
    /// </summary>
    /// <param name="pZustand">The state.</param>
    /// <returns>
    /// The indices of the living members, and it may be empty when
    /// the battle is over.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <strong>And it is the troop's own order and not a
    /// re-sorting</strong>, -- <strong>because the battle branch's
    /// target index counts into the same list.</strong>
    /// </para>
    /// </remarks>
    public static List<int> MoeglicheZiele(
        GameSimulationState pZustand)
    {
        var liste = new List<int>();
        if (pZustand == null || !pZustand.IsBattleActive)
        {
            return liste;
        }

        for (var i = 0; i < pZustand.TroopMembers.Count; i++)
        {
            if (pZustand.CanMonsterAct(i))
            {
                liste.Add(i);
            }
        }

        return liste;
    }

    /// <summary>
    /// And it points the next command at one of them.
    /// </summary>
    /// <param name="pZustand">The state.</param>
    /// <param name="pIndex">Which monster.</param>
    /// <param name="pAlle">Whether the command hits every one.</param>
    /// <param name="pFehler">Why not, and empty on success.</param>
    /// <returns>Whether the target was set.</returns>
    public static bool Waehle(
        GameSimulationState pZustand,
        int pIndex,
        bool pAlle,
        out string pFehler)
    {
        pFehler = "";

        var moeglich = MoeglicheZiele(pZustand);
        if (moeglich.Count == 0)
        {
            pFehler = "no monster can be aimed at, and the battle"
                + " has nothing left to strike";
            return false;
        }

        if (!pAlle && !moeglich.Contains(pIndex))
        {
            pFehler = "monster " + pIndex
                + " cannot be aimed at, and aiming at a fallen one"
                + " is a command the game never issued";
            return false;
        }

        pZustand.CurrentTargetIndex = pIndex;
        pZustand.TargetsSingleEnemy = !pAlle;
        return true;
    }
}
