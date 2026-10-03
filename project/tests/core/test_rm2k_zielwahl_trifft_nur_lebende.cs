using System;
using System.Collections.Generic;
using System.IO;
using UniversalRPG.Plugins;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And the target, and that a fallen monster cannot be aimed at.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is the fifth field of that shape in this
/// repository</strong>, -- <strong>written by a test and read by
/// nobody that a game could reach.</strong>
/// </para>
/// </remarks>
public partial class TestRm2kZielwahlTrifftNurLebende : TestBase
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
    /// And both of the game's scorpions are aimable.
    /// </summary>
    public void Test_BeideSkorpioneSindZielfaehig()
    {
        var lauf = MitKampf(out var host);
        if (lauf == null)
        {
            return;
        }

        using var h = host!;
        var ziele = Rm2kZielwahl.MoeglicheZiele(lauf.Simulation);
        Console.WriteLine("ziele: " + string.Join(" ", ziele));

        AssertEq(2, ziele.Count,
            "**and both monsters of troop four can be aimed"
                + " at**");

        var ok = Rm2kZielwahl.Waehle(
            lauf.Simulation, 1, false, out var grund);
        Console.WriteLine("wahl: " + ok + " -> " + grund
            + "  index " + lauf.Simulation.CurrentTargetIndex
            + "  einzeln " + lauf.Simulation.TargetsSingleEnemy);

        AssertTrue(ok, "**and a living monster is chosen**");
        AssertEq(1, lauf.Simulation.CurrentTargetIndex,
            "**and the target index is the one chosen** -- and"
                + " `13310` compares against exactly this field");
        AssertTrue(lauf.Simulation.TargetsSingleEnemy,
            "**and it is a single enemy and not all**");
    }

    /// <summary>
    /// And a fallen monster is not aimable.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the fault that would move a turn without
    /// anything happening.</strong>
    /// </para>
    /// </remarks>
    public void Test_EinToterGegnerIstKeinZiel()
    {
        var lauf = MitKampf(out var host);
        if (lauf == null)
        {
            return;
        }

        using var h = host!;
        lauf.Simulation.SetMonsterHp(0, 0);
        Console.WriteLine("ziele nach dem Tod: "
            + string.Join(" ",
                Rm2kZielwahl.MoeglicheZiele(lauf.Simulation)));

        var ok = Rm2kZielwahl.Waehle(
            lauf.Simulation, 0, false, out var grund);
        Console.WriteLine("wahl auf einen toten: " + ok
            + " -> " + grund);

        AssertTrue(!ok,
            "**and a fallen monster cannot be aimed at**");

        AssertTrue(grund.Contains("fallen"),
            "**and the refusal says why**");

        AssertEq(1, Rm2kZielwahl.MoeglicheZiele(
            lauf.Simulation).Count,
            "**and only the living one is left as a target**");
    }

    /// <summary>
    /// And "all enemies" is a choice of its own.
    /// </summary>
    public void Test_AlleGegnerIstEineEigeneWahl()
    {
        var lauf = MitKampf(out var host);
        if (lauf == null)
        {
            return;
        }

        using var h = host!;
        lauf.Simulation.SetMonsterHp(1, 0);

        var ok = Rm2kZielwahl.Waehle(
            lauf.Simulation, 0, true, out var grund);
        Console.WriteLine("alle: " + ok + " -> " + grund
            + "  einzeln " + lauf.Simulation.TargetsSingleEnemy);

        AssertTrue(ok,
            "**and \"all\" is offered even when one has"
                + " fallen** -- and that is the reference's second"
                + " mode and not a reader's choice");
        AssertTrue(!lauf.Simulation.TargetsSingleEnemy,
            "**and the flag says so** -- and `13310` compares"
                + " this field and not the target index alone");
    }
}
