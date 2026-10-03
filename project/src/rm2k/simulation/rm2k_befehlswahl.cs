using System;
using System.Collections.Generic;

namespace UniversalRPG.Rm2k.Simulation;

/// <summary>
/// And the commands a hero may choose, and the game's own rules for
/// them.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the six are liblcf's own <c>BattleCommand</c>
/// enumeration</strong>, -- <strong>and their numbers are the
/// reference's</strong>: -- <strong><c>attack 0, skill 1, subskill 2,
/// defense 3, item 4, escape 5, special 6</c>.</strong>
/// </para>
/// <para>
/// <strong>And this game's <c>battle_commands</c> capsule is
/// empty</strong> -- <strong>measured</strong> -- <strong>and it
/// writes <c>1009 Change Battle Commands</c> zero times across
/// seven hundred and forty three maps</strong>, -- <strong>so the
/// six stand.</strong> -- <strong>And an empty capsule is not "no
/// commands": it is "the game wrote none", and liblcf's defaults
/// then apply.</strong>
/// </para>
/// <para>
/// <strong>And a command a hero cannot pay for is not offered</strong>,
/// -- <strong>because the reference's command window hides what a
/// hero cannot afford</strong>, -- <strong>and a list that offers an
/// unaffordable skill would let a player choose something the game
/// refuses.</strong>
/// </para>
/// </remarks>
public static class Rm2kBefehlswahl
{
    /// <summary>And the reference's numbers, in its own order.</summary>
    public enum Befehl
    {
        /// <summary>And the strike, which needs no skill.</summary>
        Angriff = 0,

        /// <summary>And a skill the hero learned.</summary>
        Faehigkeit = 1,

        /// <summary>And a skill the hero has not learned yet.</summary>
        Teilfaehigkeit = 2,

        /// <summary>And doing nothing.</summary>
        Verteidigung = 3,

        /// <summary>And an item from the party's bag.</summary>
        Gegenstand = 4,

        /// <summary>And leaving the fight.</summary>
        Flucht = 5,

        /// <summary>And a command the game gives its own meaning.</summary>
        Spezial = 6,
    }

    /// <summary>
    /// And the commands that are available right now.
    /// </summary>
    /// <param name="pZustand">The state.</param>
    /// <param name="pHeldId">
    /// The hero, and -1 for "whichever can act".
    /// </param>
    /// <returns>
    /// The available commands, and it is never empty while a battle
    /// runs.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <strong>And it always offers at least the strike and the
    /// defence</strong>, -- <strong>because the reference offers both
    /// to every actor in every battle</strong>, -- <strong>and a list
    /// that could be empty would leave a player with nothing to
    /// choose and no way to end their turn.</strong>
    /// </para>
    /// <para>
    /// <strong>And escape is not offered when the encounter command
    /// forbade it</strong>, -- <strong>because that is what the
    /// reference's <c>allow_escape</c> is for.</strong>
    /// </para>
    /// </remarks>
    public static List<Befehl> Verfuegbar(
        GameSimulationState pZustand, int pHeldId)
    {
        var liste = new List<Befehl>
        {
            Befehl.Angriff,
            Befehl.Verteidigung,
        };

        if (pZustand == null || !pZustand.IsBattleActive)
        {
            return liste;
        }

        // **Und  die  Faehigkeiten  kommen  dazu,  wenn  der  Held
        //  welche  hat  und  welche  er  bezahlen  kann.**
        foreach (var skillId in FaehigkeitenVon(pZustand, pHeldId))
        {
            liste.Add(Befehl.Faehigkeit);
            break;
        }

        // **Und  der  Gegenstand  kommt  dazu,  wenn  die  Tasche
        //  etwas  enthaelt,  das  man  im  Kampf  benutzen  kann.**
        if (GegenstaendeVorhanden(pZustand))
        {
            liste.Add(Befehl.Gegenstand);
        }

        // **Und  die  Flucht  nur,  wenn  der  Befehl  sie  erlaubt.**
        if (FluchtErlaubt(pZustand))
        {
            liste.Add(Befehl.Flucht);
        }

        return liste;
    }

    /// <summary>
    /// And whether the encounter let the party flee.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the reference writes
    /// <c>allow_escape = (escape_mode != 0)</c></strong>, --
    /// <strong>and mode 1 ends the event and mode 2 runs the game's
    /// own handler</strong>, -- <strong>and both are "escapable".</strong>
    /// </para>
    /// </remarks>
    public static bool FluchtErlaubt(GameSimulationState pZustand)
    {
        return pZustand != null
            && pZustand.BattleEscape
            != GameSimulationState.BattleEscapeMode.Disallow;
    }

    /// <summary>
    /// And the hero's skills that can be paid for.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And an empty list is a real answer</strong>, --
    /// <strong>and not a reading failure</strong>, -- <strong>and a
    /// hero with no learned skill has nothing to offer but the
    /// strike.</strong>
    /// </para>
    /// </remarks>
    public static List<int> FaehigkeitenVon(
        GameSimulationState pZustand, int pHeldId)
    {
        var liste = new List<int>();
        if (pZustand == null)
        {
            return liste;
        }

        foreach (var heldId in pHeldId >= 0
            ? new[] { pHeldId } : HeldIds(pZustand))
        {
            foreach (var skillId in pZustand.SkillsOf(heldId))
            {
                liste.Add(skillId);
            }
        }

        return liste;
    }

    private static IEnumerable<int> HeldIds(GameSimulationState pZustand)
    {
        foreach (var held in pZustand.ActorValues.Keys)
        {
            yield return held;
        }
    }

    private static bool GegenstaendeVorhanden(
        GameSimulationState pZustand)
    {
        // **Und  die  Tasche  heisst  `ItemCounts`** -- **und  das  ist
        //  derselbe  Ort,  den  `ExecuteChangeItems` beschreibt.**
        foreach (var eintrag in pZustand.ItemCounts)
        {
            if (eintrag.Value > 0)
            {
                return true;
            }
        }

        return false;
    }
}
