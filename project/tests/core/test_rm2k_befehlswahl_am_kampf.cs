using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniversalRPG.Plugins;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And the commands a hero may choose, and that they are the
/// reference's six.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is the last measured piece of criterion 1.</strong>
/// --
/// <strong>liblcf's <c>BattleCommand</c> enumeration is
/// <c>attack 0, skill 1, subskill 2, defense 3, item 4, escape 5,
/// special 6</c>.</strong>
/// </para>
/// <para>
/// <strong>And this game's <c>battle_commands</c> capsule is empty</strong>
/// -- <strong>measured, <c>{  }</c></strong> -- <strong>and it writes
/// <c>1009</c> zero times across seven hundred and forty three
/// maps</strong>, -- <strong>so the six stand.</strong>
/// </para>
/// </remarks>
public partial class TestRm2kBefehlswahlAmKampf : TestBase
{
    private const string Spiel = "E:/RPGMakerGames/Dragon Destiny";

    private static Rm2kEngineRuntime? Starte(out EnginePluginHost? pHost)
    {
        pHost = null;
        if (!File.Exists(Spiel + "/RPG_RT.ldb"))
        {
            return null;
        }

        pHost = new EnginePluginHost(
            BuiltInEnginePluginCatalog.CreateRuntimeRegistry());
        if (!pHost.Start(new PluginGameInfo
            {
                GameDirectory = Spiel,
                EngineId = EnginePluginIds.RpgMaker2000,
                Generation = "rm2k",
                DetectorScore = 3,
            }).Success)
        {
            pHost.Dispose();
            pHost = null;
            return null;
        }

        return (Rm2kEngineRuntime)pHost.Runtime!;
    }

    /// <summary>
    /// And the six numbers are the reference's six.
    /// </summary>
    public void Test_DieSechsNummernSindDieDerReferenz()
    {
        Console.WriteLine("Angriff "
            + (int)Rm2kBefehlswahl.Befehl.Angriff
            + "  Faehigkeit "
            + (int)Rm2kBefehlswahl.Befehl.Faehigkeit
            + "  Teil " + (int)Rm2kBefehlswahl.Befehl.Teilfaehigkeit
            + "  Verteidigung "
            + (int)Rm2kBefehlswahl.Befehl.Verteidigung
            + "  Gegenstand "
            + (int)Rm2kBefehlswahl.Befehl.Gegenstand
            + "  Flucht " + (int)Rm2kBefehlswahl.Befehl.Flucht
            + "  Spezial " + (int)Rm2kBefehlswahl.Befehl.Spezial);

        AssertEq(0, (int)Rm2kBefehlswahl.Befehl.Angriff,
            "**and attack is command zero**");
        AssertEq(1, (int)Rm2kBefehlswahl.Befehl.Faehigkeit,
            "**and skill is one**");
        AssertEq(2, (int)Rm2kBefehlswahl.Befehl.Teilfaehigkeit,
            "**and subskill is two**");
        AssertEq(3, (int)Rm2kBefehlswahl.Befehl.Verteidigung,
            "**and defence is three**");
        AssertEq(4, (int)Rm2kBefehlswahl.Befehl.Gegenstand,
            "**and item is four**");
        AssertEq(5, (int)Rm2kBefehlswahl.Befehl.Flucht,
            "**and escape is five**");
        AssertEq(6, (int)Rm2kBefehlswahl.Befehl.Spezial,
            "**and special is six** -- and those are liblcf's"
                + " own numbers and not this repository's scale");
    }

    /// <summary>
    /// And a battle always offers the strike and the defence.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And that is the rule that matters</strong>, --
    /// <strong>because a hero with no skill and an empty bag must
    /// still be able to act.</strong>
    /// </para>
    /// </remarks>
    public void Test_AngriffUndVerteidigungImmer()
    {
        var lauf = Starte(out var host);
        if (lauf == null)
        {
            return;
        }

        using var h = host!;
        var state = lauf.Simulation;
        AssertTrue(Rm2kBegegnungAufbauen.Versuche(
                state.DatabaseData, 2, out var truppe, out _),
            "**and the troop builds**");
        foreach (var g in truppe)
        {
            state.TroopMembers.Add(g);
        }

        state.IsBattleActive = true;

        var verfuegbar = Rm2kBefehlswahl.Verfuegbar(state, -1);
        Console.WriteLine("verfuegbar: "
            + string.Join(" ", verfuegbar.Select(x => x.ToString())));

        AssertTrue(verfuegbar.Contains(Rm2kBefehlswahl.Befehl.Angriff),
            "**and the strike is offered**");

        AssertTrue(verfuegbar.Contains(Rm2kBefehlswahl.Befehl.Verteidigung),
            "**and the defence**");

        AssertTrue(verfuegbar.Count >= 2,
            "**and the list is never empty** -- and a hero with"
                + " nothing to choose would have no way to end a"
                + " turn");
    }

    /// <summary>
    /// And escape follows the encounter command, not the reader's
    /// wish.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the reference's
    /// <c>allow_escape = (escape_mode != 0)</c>.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieFluchtFolgtDemBegegnungsbefehl()
    {
        var lauf = Starte(out var host);
        if (lauf == null)
        {
            return;
        }

        using var h = host!;
        var state = lauf.Simulation;
        AssertTrue(Rm2kBegegnungAufbauen.Versuche(
                state.DatabaseData, 2, out var truppe, out _),
            "**and the troop builds**");
        foreach (var g in truppe)
        {
            state.TroopMembers.Add(g);
        }

        state.IsBattleActive = true;

        state.BattleEscape =
            GameSimulationState.BattleEscapeMode.Disallow;
        var ohne = Rm2kBefehlswahl.Verfuegbar(state, -1);
        Console.WriteLine("ohne Flucht: " + string.Join(" ",
            ohne.Select(x => x.ToString())));

        AssertTrue(!ohne.Contains(Rm2kBefehlswahl.Befehl.Flucht),
            "**and with escape forbidden it is not offered**"
                + " -- and the encounter command's escape mode"
                + " is the game's own rule and not a setting");

        state.BattleEscape =
            GameSimulationState.BattleEscapeMode.EndEvent;
        var mit = Rm2kBefehlswahl.Verfuegbar(state, -1);
        Console.WriteLine("mit Flucht: " + string.Join(" ",
            mit.Select(x => x.ToString())));

        AssertTrue(mit.Contains(Rm2kBefehlswahl.Befehl.Flucht),
            "**and with escape allowed it is** -- and mode one"
                + " ends the event and mode two runs the game's"
                + " own handler and both are escapable");

        state.BattleEscape =
            GameSimulationState.BattleEscapeMode.CustomHandler;
        AssertTrue(Rm2kBefehlswahl.Verfuegbar(state, -1)
            .Contains(Rm2kBefehlswahl.Befehl.Flucht),
            "**and the custom handler mode is escapable too**");
    }

    /// <summary>
    /// And no battle means no choice.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And asking the wrong question of the state is what
    /// this repository keeps correcting</strong>, -- <strong>and a
    /// hero outside a battle has nothing to choose.</strong>
    /// </para>
    /// </remarks>
    public void Test_OhneKampfNurDieGrundbefehle()
    {
        var lauf = Starte(out var host);
        if (lauf == null)
        {
            return;
        }

        using var h = host!;
        lauf.Simulation.IsBattleActive = false;
        var verfuegbar = Rm2kBefehlswahl.Verfuegbar(
            lauf.Simulation, -1);
        Console.WriteLine("ohne Kampf: " + verfuegbar.Count
            + " befehle");

        AssertEq(2, verfuegbar.Count,
            "**and outside a battle only the strike and the"
                + " defence are listed** -- and the state carries a"
                + " battle escape mode outside a battle, and"
                + " asking that outside one is asking the wrong"
                + " question");
    }
}
