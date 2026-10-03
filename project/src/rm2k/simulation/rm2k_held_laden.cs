using System;
using System.Collections.Generic;

namespace UniversalRPG.Rm2k.Simulation;

/// <summary>
/// Loads a hero into the battle state from the game's own bank.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And <c>ActorSkills</c> is the sixth field in this
/// repository of that shape</strong>, -- <strong>written by a test and
/// filled by nothing a game could reach</strong>, -- <strong>after
/// <c>IsTransferPending</c>, <c>TroopMembers</c>,
/// <c>BattleSubcommand</c>, <c>BattleTurn</c> and
/// <c>CurrentTargetIndex</c>.</strong>
/// </para>
/// <para>
/// <strong>And it cost the strike</strong>, -- <strong>because
/// <c>ErsteAngriffsfaehigkeit</c> asks <c>FaehigkeitenVon</c> and that
/// asks <c>SkillsOf</c> and that reads a set nothing had ever
/// filled</strong>, -- <strong>so a hero in a game with three hundred
/// skills had none of them.</strong>
/// </para>
/// <para>
/// <strong>And a hero's learned skills are the ones whose level the
/// hero has reached</strong>, -- <strong>and this game's bank writes
/// no level field, so every entry counts</strong>, -- <strong>and
/// dropping the ones that do not count would be inventing a rule.</strong>
/// </para>
/// </remarks>
public static class Rm2kHeldLaden
{
    /// <summary>
    /// And it puts one hero's learned skills into the state.
    /// </summary>
    /// <param name="pZustand">The state.</param>
    /// <param name="pBank">The database.</param>
    /// <param name="pHeldId">Which hero.</param>
    /// <param name="pFehler">Why not, and empty on success.</param>
    /// <returns>Whether the hero was loaded.</returns>
    public static bool Lade(
        GameSimulationState pZustand,
        Godot.Collections.Dictionary pBank,
        int pHeldId,
        out string pFehler)
    {
        pFehler = "";

        if (pZustand == null || pBank == null)
        {
            pFehler = "the state or the bank is missing";
            return false;
        }

        if (!pBank.ContainsKey("actors"))
        {
            pFehler = "the bank carries no actors";
            return false;
        }

        var helden = (Godot.Collections
            .Array<Godot.Collections.Dictionary>)pBank["actors"];
        Godot.Collections.Dictionary?held = null;
        foreach (var h in helden)
        {
            if (h["id"].AsInt32() == pHeldId)
            {
                held = h;
                break;
            }
        }

        if (held == null)
        {
            pFehler = "the bank carries no hero with id " + pHeldId;
            return false;
        }

        if (!held.ContainsKey("unknown_fields"))
        {
            // **Und  ein  Held  ohne  Lernfeld  hat  nichts  gelernt.**
            pZustand.SkillsOf(pHeldId).Clear();
            return true;
        }

        var gelernt = 0;
        foreach (var f in (Godot.Collections
            .Array<Godot.Collections.Dictionary>)
            held["unknown_fields"])
        {
            if (f["id"].AsInt32() != 0x3F)
            {
                continue;
            }

            if (!Parser.Rm2kLearningDecoder.TryDecode(
                    (byte[])f["data"], out var eintraege,
                    out var warum))
            {
                pFehler = "hero " + pHeldId + ": " + warum;
                return false;
            }

            var menge = pZustand.SkillsOf(pHeldId);
            foreach (var e in eintraege)
            {
                menge.Add(e["skill_id"].AsInt32());
                gelernt++;
            }
        }

        pZustand.AddDiagnostic(
            "RM2K hero " + pHeldId + " enters with "
            + gelernt + " learned skills");

        return true;
    }

    /// <summary>
    /// And it fills the battle animation table from the bank.
    /// </summary>
    /// <param name="pZustand">The state.</param>
    /// <param name="pBank">The database.</param>
    /// <returns>
    /// How many animations were read, and it is zero rather than a
    /// fault when the bank carries none.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <strong>And this is the eighth field of that shape</strong>, --
    /// <strong>and Dragon Destiny names 792 battle animations</strong>,
    /// -- <strong>and without this every one of them returns zero
    /// frames and plays nothing.</strong>
    /// </para>
    /// </remarks>
    public static int LadeAnimationen(
        GameSimulationState pZustand,
        Godot.Collections.Dictionary pBank)
    {
        if (pZustand == null || pBank == null
            || !pBank.ContainsKey("animations"))
        {
            return 0;
        }

        var tabelle = new System.Collections.Generic
            .Dictionary<int, int>();
        var gelesen = 0;
        var verweigert = 0;
        foreach (var a in (Godot.Collections
            .Array<Godot.Collections.Dictionary>)
            pBank["animations"])
        {
            var felder = a.ContainsKey("unknown_fields")
                ? (Godot.Collections.Array<Godot.Collections
                    .Dictionary>)a["unknown_fields"] : null;
            if (!Parser.Rm2kAnimationDecoder.TryDecode(
                    felder!, out var frames, out var warum))
            {
                verweigert++;
                continue;
            }

            tabelle[a["id"].AsInt32()] = frames;
            gelesen++;
        }

        // **Und  `BattleAnimationDurations`  ist  `{ get; init; }`**,
        // -- **und  deshalb  muss  der  Host  es  setzen**, -- **und
        //  `init`  laesst  das  nur  bei  der  Erzeugung  zu.**
        pZustand.SetBattleAnimationDurations(tabelle);

        if (gelesen > 0)
        {
            pZustand.AddDiagnostic(
                "RM2K " + gelesen + " battle animations read, "
                + verweigert + " refused");
        }

        return gelesen;
    }

    /// <summary>
    /// And it puts every hero of the bank into the state.
    /// </summary>
    /// <param name="pZustand">The state.</param>
    /// <param name="pBank">The database.</param>
    /// <returns>
    /// How many heroes were loaded, and it is zero rather than a
    /// fault when the bank carries none.
    /// </returns>
    public static int LadeAlle(
        GameSimulationState pZustand,
        Godot.Collections.Dictionary pBank)
    {
        if (pZustand == null || pBank == null
            || !pBank.ContainsKey("actors"))
        {
            return 0;
        }

        var geladen = 0;
        foreach (var h in (Godot.Collections
            .Array<Godot.Collections.Dictionary>)pBank["actors"])
        {
            if (Lade(pZustand, pBank, h["id"].AsInt32(), out _))
            {
                geladen++;
            }
        }

        return geladen;
    }
}
