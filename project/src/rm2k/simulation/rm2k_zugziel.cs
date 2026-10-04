using System;
using System.Collections.Generic;

namespace UniversalRPG.Rm2k.Simulation;

/// <summary>
/// And which target a command names, and whether the name is any good.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And <c>FuehreZugAus</c> took a bare <c>int</c></strong>, --
/// <strong>and a bare <c>int</c> cannot say whether it is a monster
/// index or a hero id</strong>. -- <strong>Hero ids start at one and
/// monster indices at zero</strong>, -- <strong>and an attack at index
/// one would hit the wrong side without any error</strong>.
/// </para>
/// <para>
/// <strong>And a target has to be named before the command runs</strong>,
/// -- <strong>because the reference picks it in the battle screen and
/// the command reads it back</strong>.
/// </para>
/// <para>
/// <strong>And an unnamed target is a refusal</strong>, -- <strong>and
/// not a defaulted index</strong>, -- <strong>because a strike at a
/// target this runtime invented would be a rule this repository made
/// up</strong>.
/// </para>
/// </remarks>
public sealed class Rm2kZugziel
{
    /// <summary>
    /// And nothing has been aimed at yet.
    /// </summary>
    private int _held = 0;

    /// <summary>And no monster is aimed at.</summary>
    private int _gegner = -1;

    /// <summary>Whether a monster was aimed at.</summary>
    public bool HatGegner { get; private set; }

    /// <summary>Whether a hero was aimed at.</summary>
    public bool HatHeld { get; private set; }

    /// <summary>And the monster's index, or -1.</summary>
    public int GegnerIndex => _gegner;

    /// <summary>And the hero's id, or 0.</summary>
    public int HeldId => _held;

    /// <summary>
    /// And it aims at a monster in the troop's own order.
    /// </summary>
    /// <param name="pZustand">The state.</param>
    /// <param name="pIndex">The troop index.</param>
    /// <param name="pFehler">Why not, and empty on success.</param>
    /// <returns>Whether the target was taken.</returns>
    public bool WaehleGegner(GameSimulationState pZustand, int pIndex,
        out string pFehler)
    {
        pFehler = "";
        if (!Rm2kZielwahl.Waehle(pZustand, pIndex, false, out pFehler))
        {
            return false;
        }

        _gegner = pIndex;
        _held = 0;
        HatGegner = true;
        HatHeld = false;
        return true;
    }

    /// <summary>
    /// And it aims at one of the party's heroes.
    /// </summary>
    /// <param name="pZustand">The state.</param>
    /// <param name="pHeldId">The hero id.</param>
    /// <param name="pFehler">Why not, and empty on success.</param>
    /// <returns>Whether the target was taken.</returns>
    public bool WaehleHeld(GameSimulationState pZustand, int pHeldId,
        out string pFehler)
    {
        pFehler = "";
        var moeglich = Rm2kFertigkeitZiel.MoeglicheHeldenziele(
            pZustand, Rm2kFertigkeitZiel.ScopeAlly);
        if (moeglich.Count == 0)
        {
            pFehler = "no hero can be aimed at, and a cure needs"
                + " one";
            return false;
        }

        if (!moeglich.Contains(pHeldId))
        {
            pFehler = "hero " + pHeldId + " cannot be aimed at,"
                + " and a cure pointed at a fallen one is a"
                + " command the game never issued";
            return false;
        }

        _held = pHeldId;
        _gegner = -1;
        HatHeld = true;
        HatGegner = false;
        return true;
    }

    /// <summary>
    /// And it aims at everybody on the side the scope names.
    /// </summary>
    /// <param name="pZustand">The state.</param>
    /// <param name="pScope">The skill's liblcf scope.</param>
    /// <returns>Whether there was anything to aim at.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And the three group scopes need no choice</strong>, --
    /// <strong>and pretending otherwise would ask the player to pick
    /// one of four</strong>.
    /// </para>
    /// </remarks>
    public bool WaehleGruppe(GameSimulationState pZustand, int pScope)
    {
        if (Rm2kFertigkeitZiel.ZieltAufGegner(pScope))
        {
            var moeglich = Rm2kZielwahl.MoeglicheZiele(pZustand);
            if (moeglich.Count == 0)
            {
                return false;
            }

            _gegner = moeglich[0];
            _held = 0;
            HatGegner = true;
            HatHeld = false;
            return true;
        }

        var helden = Rm2kFertigkeitZiel.MoeglicheHeldenziele(
            pZustand, pScope);
        if (helden.Count == 0)
        {
            return false;
        }

        _held = helden[0];
        _gegner = -1;
        HatHeld = true;
        HatGegner = false;
        return true;
    }

    /// <summary>
    /// And it forgets the aim, which is what happens after a turn.
    /// </summary>
    public void Zuruecksetzen()
    {
        _gegner = -1;
        _held = 0;
        HatGegner = false;
        HatHeld = false;
    }
}
