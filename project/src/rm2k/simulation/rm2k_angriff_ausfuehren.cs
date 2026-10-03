using System;
using System.Collections.Generic;

namespace UniversalRPG.Rm2k.Simulation;

/// <summary>
/// And a skill of the game's own, striking a monster of the game's
/// own.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this joins the four measured ends</strong>: -- <strong>
/// the hero's own <c>parameters</c></strong>, -- <strong>the
/// monster's own <c>enemies</c> row</strong>, -- <strong>the skill's
/// own <c>power</c> and <c>variance</c></strong>, -- <strong>and
/// <c>Rm2kSimulatedAttack.Compute</c>, the reference's
/// formula.</strong>
/// </para>
/// <para>
/// <strong>And every number in the calculation comes out of the
/// file.</strong> -- <strong>A reader that used a constant for the
/// attack would produce a fight that looks right and is
/// wrong.</strong>
/// </para>
/// <para>
/// <strong>And a skill that does not affect hit points does
/// nothing</strong>, -- <strong>because the game says so in
/// <c>affect_hp</c></strong>, -- <strong>and a reader that struck
/// anyway would make a healing skill into a weapon.</strong>
/// </para>
/// </remarks>
public static class Rm2kAngriffAusfuehren
{
    /// <summary>
    /// And it strikes, or it says why not.
    /// </summary>
    /// <param name="pZustand">The state.</param>
    /// <param name="pSkill">
    /// One entry of the bank's <c>skills</c>.
    /// </param>
    /// <param name="pGegnerIndex">Which member.</param>
    /// <param name="pZufaellig">The draw.</param>
    /// <param name="pSchaden">The damage dealt.</param>
    /// <param name="pFehler">Why not, and empty on success.</param>
    /// <returns>Whether the strike happened.</returns>
    public static bool FuehreAus(
        GameSimulationState pZustand,
        Godot.Collections.Dictionary pSkill,
        int pGegnerIndex,
        Rm2kDamageRandom pZufaellig,
        out int pSchaden,
        out string pFehler)
    {
        pSchaden = 0;
        pFehler = "";

        if (pSkill == null)
        {
            pFehler = "there is no skill";
            return false;
        }

        // **Und  eine  Faehigkeit,  die  keine  Trefferpunkte
        //  beruehrt,  trifft  nicht.**
        //
        // **Und  das  Spiel  sagt  das  in  `affect_hp`** --
        // **und  eine  Heilkunefaehigkeit  zu  einer  Waffe  zu
        //  machen  waere  eine  Spielregel,  die  erfunden  ist.**
        if (!pSkill.ContainsKey("affect_hp")
            || pSkill["affect_hp"].AsInt32() == 0)
        {
            pFehler = "the skill \""
                + (pSkill.ContainsKey("name")
                    ? pSkill["name"].AsString() : "")
                + "\" does not affect hit points, and a skill"
                + " that heals must not strike";
            return false;
        }

        // **Und  ohne  `power`  gibt  es  nichts  zu  rechnen.**
        if (!pSkill.ContainsKey("power"))
        {
            pFehler = "the skill carries no power, and a strike"
                + " without a number is not a strike";
            return false;
        }

        var power = pSkill["power"].AsInt32();
        if (power <= 0)
        {
            pFehler = "the skill's power is " + power
                + ", and a strike of nothing is not a strike";
            return false;
        }

        var varianz = pSkill.ContainsKey("variance")
            ? pSkill["variance"].AsInt32() : 0;

        // **Und  jetzt  die  Formel  der  Referenz** -- **mit  den
        //  Werten  des  Gegners  aus  der  Bank.**
        var schaden = Rm2kGegnerSchaden.Berechne(
            pZustand, pGegnerIndex,
            power, 0, 0, varianz, pZufaellig);

        if (schaden < 0)
        {
            pFehler = pGegnerIndex < 0
                || pGegnerIndex >= pZustand.TroopMembers.Count
                ? "there is no monster at index " + pGegnerIndex
                : "the monster at index " + pGegnerIndex
                    + " cannot be struck -- it is dead or hidden,"
                    + " and a strike on it would take it further"
                    + " below zero";
            return false;
        }

        pSchaden = schaden;
        return true;
    }
}
