using System;
using System.Collections.Generic;
using UniversalRPG.Rm2k.Parser;

namespace UniversalRPG.Rm2k.Simulation;

/// <summary>
/// And it turns the game's own encounter into living monsters.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is the join, and it was the last missing
/// piece.</strong> --
/// <strong><c>TroopMembers</c> was read by six places in the
/// interpreter and filled by nobody</strong>, -- <strong>and every
/// test that put a monster in it did so with a literal.</strong>
/// </para>
/// <para>
/// <strong>And a member's schema is the reference's</strong>, -- <strong>
/// because <c>CanMonsterAct</c> reads <c>hp</c> out of it.</strong> --
/// <strong>And that is a hard requirement, and a reader that wrote
/// <c>max_hp</c> would have left every monster unable to
/// act.</strong>
/// </para>
/// <para>
/// <strong>And a member without hit points is refused</strong>, --
/// <strong>and refused with the monster's name in the
/// message</strong>, -- <strong>because two of the game's seventy two
/// monsters carry no <c>max_hp</c> at all and a fight must not invent
/// health for them.</strong>
/// </para>
/// </remarks>
public static class Rm2kBegegnungAufbauen
{
    /// <summary>
    /// And it builds the troop or it says why not.
    /// </summary>
    /// <param name="pBank">The parsed database.</param>
    /// <param name="pTroopId">The encounter the game's command named.</param>
    /// <param name="pMitglieder">The living members.</param>
    /// <param name="pFehler">
    /// Why it did not build one, and empty on success.
    /// </param>
    /// <returns>Whether a fight can start.</returns>
    public static bool Versuche(
        Godot.Collections.Dictionary pBank,
        int pTroopId,
        out Godot.Collections.Array<Godot.Collections.Dictionary> pMitglieder,
        out string pFehler)
    {
        pMitglieder = new Godot.Collections
            .Array<Godot.Collections.Dictionary>();
        pFehler = "";

        var bytes = TruppenBytes(pBank, pTroopId);
        if (bytes == null)
        {
            pFehler = "the bank carries no troop with id " + pTroopId;
            return false;
        }

        if (!Rm2kTroopMemberDecoder.TryDecode(
                bytes, out var gelesen, out var warum))
        {
            pFehler = "troop " + pTroopId + ": " + warum;
            return false;
        }

        foreach (var m in gelesen)
        {
            var enemyId = m["enemy_id"].AsInt32();
            var monster = Monster(pBank, enemyId);
            if (monster == null)
            {
                pFehler = "troop " + pTroopId + " names enemy "
                    + enemyId + ", which the bank does not have";
                return false;
            }

            // **Und  null  ist  nicht  null  und  nicht  eins.**
            if (!monster.ContainsKey("max_hp"))
            {
                pFehler = "enemy " + enemyId + " ("
                    + monster["name"].AsString() + ") carries no"
                    + " hit points, and a fight must not invent"
                    + " health for it";
                return false;
            }

            var maxHp = monster["max_hp"].AsInt32();
            if (maxHp <= 0)
            {
                pFehler = "enemy " + enemyId + " ("
                    + monster["name"].AsString() + ") has "
                    + maxHp + " hit points, and a fight against that"
                    + " is not a fight";
                return false;
            }

            pMitglieder.Add(new Godot.Collections.Dictionary
            {
                { "enemy_id", enemyId },
                { "name", monster["name"].AsString() },
                { "battler_name",
                    monster.ContainsKey("battler_name")
                        ? monster["battler_name"].AsString() : "" },
                { "hp", maxHp },
                { "max_hp", maxHp },
                { "attack", Int(monster, "attack") },
                { "defense", Int(monster, "defense") },
                { "spirit", Int(monster, "spirit") },
                { "agility", Int(monster, "agility") },
                { "x", m["x"].AsInt32() },
                { "y", m["y"].AsInt32() },
                { "invisible", (bool)m["invisible"] },
                { "hidden", false },
            });
        }

        if (pMitglieder.Count > GameSimulationState.MaxTroopMembers)
        {
            pFehler = "troop " + pTroopId + " has "
                + pMitglieder.Count + " members, and the state holds "
                + GameSimulationState.MaxTroopMembers;
            return false;
        }

        return true;
    }

    private static int Int(
        Godot.Collections.Dictionary pFeld, string pName)
    {
        return pFeld.ContainsKey(pName)
            ? pFeld[pName].AsInt32() : 0;
    }

    private static Godot.Collections.Dictionary? Monster(
        Godot.Collections.Dictionary pBank, int pId)
    {
        if (!pBank.ContainsKey("enemies"))
        {
            return null;
        }

        var liste = (Godot.Collections
            .Array<Godot.Collections.Dictionary>)pBank["enemies"];
        foreach (var m in liste)
        {
            if (m.ContainsKey("id") && m["id"].AsInt32() == pId)
            {
                return m;
            }
        }

        return null;
    }

    private static byte[]? TruppenBytes(
        Godot.Collections.Dictionary pBank, int pId)
    {
        if (!pBank.ContainsKey("troops"))
        {
            return null;
        }

        var truppen = (Godot.Collections
            .Array<Godot.Collections.Dictionary>)pBank["troops"];
        foreach (var t in truppen)
        {
            if (!t.ContainsKey("id") || t["id"].AsInt32() != pId)
            {
                continue;
            }

            if (!t.ContainsKey("unknown_fields"))
            {
                return null;
            }

            var felder = (Godot.Collections
                .Array<Godot.Collections.Dictionary>)
                t["unknown_fields"];
            foreach (var f in felder)
            {
                if (f["id"].AsInt32() == 0x02)
                {
                    return (byte[])f["data"];
                }
            }

            return null;
        }

        return null;
    }
}
