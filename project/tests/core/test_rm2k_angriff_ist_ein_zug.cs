using System;
using System.Collections.Generic;
using System.IO;
using UniversalRPG.Plugins;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And the strike, and that a chosen target turns it into a turn.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the previous commit refused the strike</strong>, --
/// <strong>and this one is what ends the refusal</strong>: -- <strong>
/// the target is chosen by the player and the skill is the
/// hero's own.</strong>
/// </para>
/// </remarks>
public partial class TestRm2kAngriffIstEinZug : TestBase
{
    private const string Spiel = "E:/RPGMakerGames/Dragon Destiny";

    private static Rm2kEngineRuntime? MitKampf(
        out EnginePluginHost? pHost)
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

        var lauf = (Rm2kEngineRuntime)pHost.Runtime!;
        if (!Rm2kBegegnungAufbauen.Versuche(
                lauf.Simulation.DatabaseData, 4,
                out var truppe, out _))
        {
            pHost.Dispose();
            pHost = null;
            return null;
        }

        foreach (var g in truppe)
        {
            lauf.Simulation.TroopMembers.Add(g);
        }

        lauf.Simulation.IsBattleActive = true;
        lauf.Simulation.WaitingFor =
            GameSimulationState.WaitReason.BattleRunning;
        return lauf;
    }

    /// <summary>
    /// And the strike without a target is still refused.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And that is the rule that keeps a target from being
    /// invented.</strong>
    /// </para>
    /// </remarks>
    public void Test_OhneZielKeinSchlag()
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
            Rm2kBefehlswahl.Befehl.Angriff, -1, out var grund);
        Console.WriteLine("ohne Ziel: " + ok + " -> " + grund);

        AssertTrue(!ok,
            "**and a strike without a chosen target is refused**");

        AssertTrue(grund.Contains("invented"),
            "**and the refusal says why** -- and a strike at a"
                + " target this runtime made up would be a rule it"
                + " invented");

        AssertEq(0, state.BattleTurn,
            "**and the turn did not move**");
    }

    /// <summary>
    /// And the strike with a target hits it and passes the turn.
    /// </summary>
    public void Test_MitZielTrifftUndWeiter()
    {
        var lauf = MitKampf(out var host);
        if (lauf == null)
        {
            return;
        }

        using var h = host!;
        var state = lauf.Simulation;
        state.BattleTurn = 0;

        var vorher = state.MonsterHp(0);
        Console.WriteLine("Gegner 0 vor dem Schlag: " + vorher
            + " hp, " + vorher + "/" + state.MonsterMaxHp(0));

        var ok = lauf.FuehreZugAus(
            Rm2kBefehlswahl.Befehl.Angriff, 0, out var grund);
        Console.WriteLine("schlag: " + ok + " -> " + grund
            + "  hp jetzt " + state.MonsterHp(0)
            + "  Turn " + state.BattleTurn
            + "  Ziel " + state.CurrentTargetIndex);

        // **Und  ob  der  Held  eine  Angriffsfaehigkeit  hat,  ist
        //  die  Frage** -- **und  die  Antwort  darf  beides  sein.**
        if (ok)
        {
            AssertTrue(state.MonsterHp(0) < vorher,
                "**and the monster lost hit points** -- and a"
                    + " strike that hits nothing is not a strike");

            AssertEq(0, state.CurrentTargetIndex,
                "**and it is the monster that was chosen**");

            AssertTrue(state.BattleTurn != 0,
                "**and the turn passed on**");
        }
        else
        {
            AssertTrue(grund.Contains("no skill"),
                "**and a hero with no learned attack skill is"
                    + " refused with that reason** -- and that is"
                    + " the game's own data and not a fault");

            Console.WriteLine("Befund: " + grund);
        }

        AssertEq(0, state.TroopMembers.Count - 2,
            "**and the troop still holds the game's two"
                + " monsters**");
    }

    /// <summary>
    /// And a target that has fallen is refused at the strike too.
    /// </summary>
    public void Test_EinTotesZielWirdAbgelehnt()
    {
        var lauf = MitKampf(out var host);
        if (lauf == null)
        {
            return;
        }

        using var h = host!;
        var state = lauf.Simulation;
        state.SetMonsterHp(0, 0);
        state.BattleTurn = 0;

        var ok = lauf.FuehreZugAus(
            Rm2kBefehlswahl.Befehl.Angriff, 0, out var grund);
        Console.WriteLine("totes Ziel: " + ok + " -> " + grund
            + "  Turn " + state.BattleTurn);

        AssertTrue(!ok,
            "**and a strike at a fallen monster is refused**");

        AssertEq(0, state.BattleTurn,
            "**and the turn did not move** -- and a refused"
                + " strike that advanced the turn would skip a"
                + " monster's action");
    }
}
