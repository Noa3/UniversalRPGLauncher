using System;
using System.Collections.Generic;
using System.IO;
using UniversalRPG.Plugins;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And that a chosen command is a turn.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is the last wiring of criterion 1</strong>, --
/// <strong>from the player's click to the battle's own
/// state.</strong>
/// </para>
/// <para>
/// <strong>And three of the six commands are answered and three are
/// refused</strong>, -- <strong>because a command that silently does
/// nothing is worse than one that says why not.</strong>
/// </para>
/// </remarks>
public partial class TestRm2kBefehlWirdEinZug : TestBase
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

    private static Rm2kEngineRuntime? MitKampf(
        out EnginePluginHost? pHost)
    {
        var lauf = Starte(out pHost);
        if (lauf == null)
        {
            return null;
        }

        if (!Rm2kBegegnungAufbauen.Versuche(
                lauf.Simulation.DatabaseData, 4,
                out var truppe, out _))
        {
            pHost!.Dispose();
            pHost = null;
            return null;
        }

        foreach (var g in truppe)
        {
            lauf.Simulation.TroopMembers.Add(g);
        }

        lauf.Simulation.IsBattleActive = true;

        // **Und  die  Wartefrage  gehoert  dazu** -- **denn
        //  `BeendeKampf`  verweigert  zu  arbeiten,  wenn  sie  nicht
        //  `BattleRunning`  ist.**
        //
        // **Und  mein  Test  hat  nur  `IsBattleActive`  gesetzt  und
        //  sich  dann  gewundert,  dass  die  Flucht  den  Kampf  nicht
        //  beendet  hat.**
        lauf.Simulation.WaitingFor =
            GameSimulationState.WaitReason.BattleRunning;
        return lauf;
    }

    /// <summary>
    /// And the defence is a turn and it passes on.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the turn counter is the assertion</strong>, --
    /// <strong>because a command that did not move the turn did not
    /// happen.</strong>
    /// </para>
    /// </remarks>
    public void Test_VerteidigungIstEinZug()
    {
        var lauf = MitKampf(out var host);
        if (lauf == null)
        {
            return;
        }

        using var h = host!;
        var state = lauf.Simulation;
        state.BattleTurn = 0;
        Console.WriteLine("vorher Turn " + state.BattleTurn
            + "  Phase " + state.BattlePhase);

        var ok = lauf.FuehreZugAus(
            Rm2kBefehlswahl.Befehl.Verteidigung,
            Rm2kZugfolge.Naechster(state), out var grund);
        Console.WriteLine("verteidigung: " + ok + "  -> " + grund
            + "  Turn jetzt " + state.BattleTurn);

        AssertTrue(ok, "**and the defence happened** -- and: "
            + grund);
        AssertEq(1, state.BattleTurn,
            "**and the turn passed on** -- and a command that did"
                + " not move the turn is not a turn");
    }

    /// <summary>
    /// And a refused command is not a turn.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the strike is refused today</strong>, -- <strong>
    /// and it says why</strong>, -- <strong>and it does not move the
    /// turn</strong>.
    /// </para>
    /// </remarks>
    public void Test_EineVerweigerungIstKeinZug()
    {
        var lauf = MitKampf(out var host);
        if (lauf == null)
        {
            return;
        }

        using var h = host!;
        var state = lauf.Simulation;
        state.BattleTurn = 0;

        var ok = lauf.FuehreZugAus(
            Rm2kBefehlswahl.Befehl.Angriff,
            Rm2kZugfolge.Naechster(state), out var grund);
        Console.WriteLine("angriff: " + ok + "  -> " + grund
            + "  Turn jetzt " + state.BattleTurn);

        AssertTrue(!ok,
            "**and the strike is refused**");

        AssertTrue(grund.Contains("needs a skill"),
            "**and the refusal says what it needs** -- and a"
                + " command that silently did nothing would be"
                + " indistinguishable from one that worked");

        AssertEq(0, state.BattleTurn,
            "**and the turn did not move** -- and a refused command"
                + " that advanced the turn would skip a monster's"
                + " action without anything having happened");
    }

    /// <summary>
    /// And escape ends the battle only when the game allowed it.
    /// </summary>
    public void Test_DieFluchtBeendetDenKampfNurWennErlaubt()
    {
        var lauf = MitKampf(out var host);
        if (lauf == null)
        {
            return;
        }

        using var h = host!;
        var state = lauf.Simulation;

        state.BattleEscape =
            GameSimulationState.BattleEscapeMode.Disallow;
        var verboten = lauf.FuehreZugAus(
            Rm2kBefehlswahl.Befehl.Flucht, 0, out var grund1);
        Console.WriteLine("verboten: " + verboten + " -> "
            + grund1);
        AssertTrue(!verboten,
            "**and a forbidden escape does not end the"
                + " battle** -- and the encounter command's own"
                + " parameter is the rule");

        AssertTrue(state.IsBattleActive,
            "**and the battle is still running**");

        state.BattleEscape =
            GameSimulationState.BattleEscapeMode.EndEvent;
        var erlaubt = lauf.FuehreZugAus(
            Rm2kBefehlswahl.Befehl.Flucht, 0, out var grund2);
        Console.WriteLine("erlaubt: " + erlaubt + " -> " + grund2);

        AssertTrue(erlaubt,
            "**and an allowed escape ends it**");
        AssertTrue(!state.IsBattleActive,
            "**and the battle is over**");
    }
}
