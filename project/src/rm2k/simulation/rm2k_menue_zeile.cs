using System.Collections.Generic;
using System.Text;

namespace UniversalRPG.Rm2k.Simulation;

/// <summary>
/// And the lines the main menu shows, built from the game's own state.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the menu key opened a menu that no surface
/// displayed</strong>, -- <strong>because the numbers only exist in
/// <see cref="GameSimulationState"/></strong>.
/// </para>
/// <para>
/// <strong>And the formatting lives here and not in the window</strong>,
/// -- <strong>because a window cannot be asserted on in a headless run
/// and a number that only exists in a window cannot be checked at
/// all</strong>.
/// </para>
/// <para>
/// <strong>And every value comes from the game</strong>:  -- <strong>the
/// name from the database, the hit points from the state, the gold the
/// game gave, and the four rights <c>11960</c> sets</strong>.  -- <strong>A
/// menu with invented numbers teaches the player nothing about the
/// game.</strong>
/// </para>
/// </remarks>
public static class Rm2kMenueZeile
{
    /// <summary>
    /// And the party's rows, one per member, in the game's own order.
    /// </summary>
    /// <param name="pState">The simulation state.</param>
    /// <returns>One line per party member; empty when there are
    /// none.</returns>
    public static List<string> Helden(GameSimulationState pState)
    {
        var zeilen = new List<string>();
        if (pState == null)
        {
            return zeilen;
        }

        // **Und  der  Index  und  nicht  die  Aufzaehlung.**
        //
        // **Und  `Godot.Collections.Array`  ist  keine  `IEnumerable`
        //  mit  stabillem  Lauf  ueber  eine  Aenderung  hinweg.**
        for (var i = 0; i < pState.PartyMemberIds.Count; i++)
        {
            var held = pState.PartyMemberIds[i];
            var werte = pState.FindActorValues(held);
            var hp = pState.GetActorCurrentHp(held);
            var maxHp = werte?.BaseMaxHp ?? hp;
            var sp = pState.GetActorCurrentSp(held);
            var maxSp = werte?.BaseMaxSp ?? sp;

            var name = string.IsNullOrEmpty(werte?.Name)
                ? "Held " + held
                : werte!.Name;
            var titel = werte?.Title ?? "";
            zeilen.Add(string.Format("{0}  {1}  {2}/{3}  {4}/{5}",
                name, titel, hp, maxHp, sp, maxSp));
        }

        return zeilen;
    }

    /// <summary>
    /// And the line that carries the gold and the four rights.
    /// </summary>
    /// <param name="pState">The simulation state.</param>
    /// <returns>Two lines: the gold and the rights.</returns>
    public static List<string> Zustand(GameSimulationState pState)
    {
        var zeilen = new List<string>();
        if (pState == null)
        {
            return zeilen;
        }

        zeilen.Add(string.Format("Gold  {0}", pState.Gold));
        zeilen.Add(string.Format(
            "Speichern {0}  Menue {1}  Teleport {2}  Flucht {3}",
            Recht(pState.AllowSave), Recht(pState.AllowMenu),
            Recht(pState.AllowTeleport), Recht(pState.AllowEscape)));
        return zeilen;
    }

    private static string Recht(bool pErlaubt)
    {
        return pErlaubt ? "an" : "aus";
    }
}
