using System.Collections.Generic;

namespace UniversalRPG.Rm2k.Simulation;

/// <summary>
/// And who a skill may be pointed at, from liblcf's own scope.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And liblcf's <c>rpg::Skill::Scope</c> is five
/// numbers</strong>:
///
/// <code>
/// Scope_enemy    = 0
/// Scope_enemies = 1
/// Scope_self     = 2
/// Scope_ally     = 3
/// Scope_party    = 4
/// </code>
///
/// <strong>And <c>scope</c> is field <c>0x0C</c> of a skill entry</strong>,
/// -- <strong>and Dragon Destiny spreads its three hundred skills
/// across all five</strong>.
/// </para>
/// <para>
/// <strong>And a reader that assumed one scope would let a cure heal
/// an enemy</strong>, -- <strong>and that is not a small mistake, it is
/// the opposite of what the game wrote</strong>.
/// </para>
/// <para>
/// <strong>And the scope decides the list, not the list itself</strong>
/// -- <strong>and an empty list is a refusal, not a silent pass</strong>.
/// </para>
/// </remarks>
public static class Rm2kFertigkeitZiel
{
    /// <summary>One enemy, by liblcf.</summary>
    public const int ScopeEnemy = 0;

    /// <summary>Every enemy, by liblcf.</summary>
    public const int ScopeEnemies = 1;

    /// <summary>The user, by liblcf.</summary>
    public const int ScopeSelf = 2;

    /// <summary>One ally, by liblcf.</summary>
    public const int ScopeAlly = 3;

    /// <summary>The whole party, by liblcf.</summary>
    public const int ScopeParty = 4;

    /// <summary>
    /// And the name liblcf gives a scope.
    /// </summary>
    /// <param name="pScope">The scope number.</param>
    /// <returns>The liblcf tag, or a marked unknown.</returns>
    public static string Name(int pScope)
    {
        return pScope switch
        {
            ScopeEnemy => "enemy",
            ScopeEnemies => "enemies",
            ScopeSelf => "self",
            ScopeAlly => "ally",
            ScopeParty => "party",
            _ => "unbekannt(" + pScope + ")"
        };
    }

    /// <summary>
    /// And the heroes a skill may be pointed at.
    /// </summary>
    /// <param name="pZustand">The state.</param>
    /// <param name="pScope">The skill's scope.</param>
    /// <returns>
    /// The party members in the game's own order, and it is empty
    /// for a scope that does not aim at heroes.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <strong>And the order is the troop's own</strong>, -- <strong>because
    /// the index a battle branch names counts into the same
    /// list</strong>.
    /// </para>
    /// <para>
    /// <strong>And a fallen hero is not a target</strong>, -- <strong>because
    /// aiming a cure at a body is a command the game never
    /// issued</strong>.
    /// </para>
    /// </remarks>
    public static List<int> MoeglicheHeldenziele(
        GameSimulationState pZustand, int pScope)
    {
        var liste = new List<int>();
        if (pZustand == null || pZustand.PartyMemberIds.Count == 0)
        {
            return liste;
        }

        if (pScope != ScopeSelf && pScope != ScopeAlly
            && pScope != ScopeParty)
        {
            return liste;
        }

        for (var i = 0; i < pZustand.PartyMemberIds.Count; i++)
        {
            var held = pZustand.PartyMemberIds[i];
            if (pZustand.GetActorCurrentHp(held) > 0)
            {
                liste.Add(held);
            }
        }

        return liste;
    }

    /// <summary>
    /// And whether the scope needs the player to pick someone.
    /// </summary>
    /// <param name="pScope">The skill's scope.</param>
    /// <returns>Whether a target has to be chosen.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And only the two single scopes ask</strong>, -- <strong>and
    /// that is what makes <c>TargetsSingleEnemy</c> a fact about the
    /// skill rather than a flag the caller guesses</strong>.
    /// </para>
    /// </remarks>
    public static bool BrauchtZiel(int pScope)
    {
        return pScope == ScopeEnemy || pScope == ScopeAlly;
    }

    /// <summary>
    /// And whether the scope aims at the enemy side.
    /// </summary>
    /// <param name="pScope">The skill's scope.</param>
    /// <returns>Whether monsters are the targets.</returns>
    public static bool ZieltAufGegner(int pScope)
    {
        return pScope == ScopeEnemy || pScope == ScopeEnemies;
    }
}
