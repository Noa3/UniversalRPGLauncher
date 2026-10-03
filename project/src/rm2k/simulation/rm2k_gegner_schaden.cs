using System;
using System.Collections.Generic;

namespace UniversalRPG.Rm2k.Simulation;

/// <summary>
/// And the damage a member of the troop takes, calculated with the
/// reference's own formula.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is the piece that was missing</strong>, -- <strong>
/// and it was missing because
/// <c>Rm2kSimulatedAttack.Compute</c> existed and exactly one command
/// reached it</strong>, -- <strong>and that command was
/// <c>10500 Simulated Attack</c>, which damages heroes outside a
/// battle.</strong>
/// </para>
/// <para>
/// <strong>And the formula is the reference's, unchanged:</strong>
///
/// <code>
/// result  = attack
/// result -= defence  * defenceRate  / 400
/// result -= spirit   * spiritRate   / 800
/// result  = max(result, 0)
/// result  = VarianceAdjustEffect(result, variance, random)
/// result  = max(result, 0)
/// </code>
///
/// <para>
/// <strong>And a monster's <c>defense</c> and <c>spirit</c> come out
/// of the game's own database</strong>, -- <strong>and a reader that
/// substituted a hero's defence for a monster's would have made every
/// fight end in one hit.</strong>
/// </para>
/// <para>
/// <strong>And the two divisors differ</strong>, -- <strong>so 800
/// points of spirit block twice as much as 400 of defence</strong>,
/// -- <strong>and using one divisor for both would have made spirit
/// half as strong as the game meant.</strong>
/// </para>
/// </remarks>
public static class Rm2kGegnerSchaden
{
    /// <summary>
    /// And how much a monster loses, and it is written into the
    /// member the state holds.
    /// </summary>
    /// <param name="pState">The state.</param>
    /// <param name="pIndex">Which member.</param>
    /// <param name="pAngriff">
    /// The attack value, and it is the formula's first number and not
    /// the monster's own attack.
    /// </param>
    /// <param name="pVerteidigungsrate">The defence rate.</param>
    /// <param name="pGeistrate">The spirit rate.</param>
    /// <param name="pVarianz">The variance, and zero is exact.</param>
    /// <param name="pZufaellig">The draw, and it is the state's.</param>
    /// <returns>The damage, or -1 when there is no such monster.</returns>
    public static int Berechne(
        GameSimulationState pState,
        int pIndex,
        int pAngriff,
        int pVerteidigungsrate,
        int pGeistrate,
        int pVarianz,
        Rm2kDamageRandom pZufaellig)
    {
        if (pIndex < 0 || pIndex >= pState.TroopMembers.Count)
        {
            return -1;
        }

        var gegner = pState.TroopMembers[pIndex];

        // **Und  ein  Gegner  ohne  Leben  wird  nicht  getroffen.**
        //
        // **Und  das  ist  nicht  eine  Kleinigkeit**:  ein  Schlag
        //  gegen  einen  gefallenen  Gegner  wuerde  ihn  von  minus
        // 74  auf  minus  99  ziehen, -- **und  `IsMonsterDead`  ist
        //  dann  immer  wahr  und  die  Zahl  waere  eine
        //  Anzeige,  die  kein  Spiel  erzeugt  hat.**
        if (!pState.CanMonsterAct(pIndex))
        {
            return -1;
        }

        var verteidigung = Zahl(gegner, "defense");
        var geist = Zahl(gegner, "spirit");
        var schaden = Rm2kSimulatedAttack.Compute(
            pAngriff, verteidigung, pVerteidigungsrate,
            geist, pGeistrate, pVarianz, pZufaellig);

        // **Und  der  Schaden  ist  ein  Verlust** -- **und  er  wird
        //  durch  die  Klammer  des  Zustands  gefuehrt.**
        pState.SetMonsterHp(
            pIndex, pState.MonsterHp(pIndex) - schaden);
        return schaden;
    }

    private static int Zahl(
        Godot.Collections.Dictionary pFeld, string pName)
    {
        return pFeld.ContainsKey(pName)
            ? pFeld[pName].AsInt32() : 0;
    }
}
