using System;
using System.Collections.Generic;
using System.IO;
using UniversalRPG.Plugins;
using UniversalRPG.Rm2k;
using UniversalRPG.Rm2k.Interpreter;
using UniversalRPG.Rm2k.Presentation;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And that the commands behind a battle run once it is over.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this test found the fault it now protects.</strong> --
/// <strong>10710 returned from the dispatch without moving
/// <c>_commandIndex</c></strong>, -- <strong>and so the encounter
/// started again on every frame</strong>, -- <strong>which is not
/// asynchrony but an endless loop with a monster in
/// it.</strong>
/// </para>
/// <para>
/// <strong>And it walks the whole way a game does</strong>: the
/// scheduler owns the page, the runtime ends the battle, the
/// scheduler tells the page.
/// </para>
/// </remarks>
public partial class TestRm2kBefehlNachKampf : TestBase
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

    private static Rm2kMap.EventCommand Begegnung(int pTrupe) =>
        new()
        {
            Code = EventInterpreter.EnemyEncounter,
            Text = "",
            Parameters = new List<int> { 0, pTrupe, 0, 1, 0, 0 },
        };

    private static Rm2kMap.EventCommand Schaden(int pWert) =>
        new()
        {
            Code = EventInterpreter.ChangeMonsterHp,
            Text = "",
            Parameters = new List<int> { 0, 1, 0, pWert, 0 },
        };

    /// <summary>
    /// And the encounter runs once and the arms behind it run after.
    /// </summary>
    public void Test_DieBegegnungLaeuftEinmalUndDerRestDanach()
    {
        var lauf = Starte(out var host);
        if (lauf == null)
        {
            return;
        }

        using var h = host!;
        var state = lauf.Simulation;
        var befehle = new Rm2kMap.EventCommand[]
        {
            Begegnung(2), Schaden(10), Schaden(5),
        };
        var interpreter = new EventInterpreter(
            state, 1, befehle, new PresentationState());
        lauf.EventScheduler.SeiteStarten(interpreter);

        lauf.EventScheduler.ExecuteFrame();
        AssertEq(GameSimulationState.WaitReason.BattleRunning,
            state.WaitingFor,
            "**and the page holds on the battle**");

        lauf.BeendeKampf(true);
        lauf.EventScheduler.ExecuteFrame();
        Console.WriteLine("nach dem Kampf: hp=" + state.MonsterHp(0)
            + " gegner=" + state.TroopMembers.Count);

        AssertEq(15, state.MonsterHp(0),
            "**and the first arm behind the encounter ran** -- and"
                + " before this fix the encounter ran again instead"
                + " and the hit points never moved");

        lauf.EventScheduler.ExecuteFrame();
        AssertEq(10, state.MonsterHp(0),
            "**and the second one too**");

        // **Und  jetzt  die  eigentliche  Behauptung**:  die
        //  Begegnung  darf  sich  nicht  wiederholen.
        var diagnose = string.Join(" | ", state.Diagnostics);
        var anzahlBegegnungen = 0;
        foreach (var d in state.Diagnostics)
        {
            if (d.Contains("Encounter: troop"))
            {
                anzahlBegegnungen++;
            }
        }

        Console.WriteLine("Begegnungen im Protokoll: "
            + anzahlBegegnungen);
        AssertEq(1, anzahlBegegnungen,
            "**and the encounter is in the log exactly once** -- and"
                + " before this it was repeated on every frame and"
                + " the troop grew to two, then to four");

        AssertEq(1, state.TroopMembers.Count,
            "**and the troop is the game's one monster**");
        AssertTrue(diagnose.Contains("carries on"),
            "**and the log says the page carries on**");
    }

    /// <summary>
    /// And a battle that ends with a defeat, and the same path.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the two outcomes are not a boolean and this
    /// test proves the path does not care.</strong>
    /// </para>
    /// </remarks>
    public void Test_AuchNachNiederlageLaeuftDieSeiteWeiter()
    {
        var lauf = Starte(out var host);
        if (lauf == null)
        {
            return;
        }

        using var h = host!;
        var state = lauf.Simulation;
        var befehle = new Rm2kMap.EventCommand[]
        {
            Begegnung(2), Schaden(25),
        };
        var interpreter = new EventInterpreter(
            state, 1, befehle, new PresentationState());
        lauf.EventScheduler.SeiteStarten(interpreter);

        lauf.EventScheduler.ExecuteFrame();
        lauf.BeendeKampf(false);
        lauf.EventScheduler.ExecuteFrame();

        Console.WriteLine("nach Niederlage: ausgang="
            + state.BattleSubcommand + " hp=" + state.MonsterHp(0));

        AssertEq(EventInterpreter.SubIdxDefeat,
            state.BattleSubcommand,
            "**and the outcome is the defeat's own subcommand**");
        AssertEq(0, state.MonsterHp(0),
            "**and the arm behind the battle ran as well** -- and"
                + " a path that only worked for a victory would"
                + " have made every lost fight a frozen page");
    }
}
