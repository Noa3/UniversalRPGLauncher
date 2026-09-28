using System;
using System.Collections.Generic;

using Godot;

using UniversalRPG.Rm2k;
using UniversalRPG.Rm2k.Interpreter;
using UniversalRPG.Rm2k.Presentation;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Tests for the battle-only command family — 13110 Change Monster HP, 13120
/// Change Monster MP, 13130 Change Monster Condition, 13150 Show Hidden
/// Monster and 13210 Change Battle BG — from liblcf's
/// <c>eventcommand.h</c> and EasyRPG's
/// <c>Game_Interpreter_Battle</c>.
/// </summary>
/// <remarks>
/// <para>
/// All five are <c>CmdSetup</c> commands with a width the reference states
/// exactly, and <strong>four of the five take a width that a reader would not
/// guess</strong>: 5, 5, 3, 1 and 1. The fifth one's value is not in its
/// parameters at all.
/// </para>
/// </remarks>
public partial class TestRm2kBattleMonsterCommands : TestBase
{
    private static Godot.Collections.Dictionary Monster(
        int pHp,
        int pMaxHp,
        int pSp = 0,
        bool pHidden = false)
    {
        return new Godot.Collections.Dictionary
        {
            ["hp"] = pHp,
            ["max_hp"] = pMaxHp,
            ["sp"] = pSp,
            ["max_sp"] = 50,
            ["hidden"] = pHidden ? 1 : 0,
        };
    }

    private static (EventInterpreter Interpreter, GameSimulationState State) Start(
        params Rm2kMap.EventCommand[] pCommands)
    {
        var state = new GameSimulationState { MapId = 1 };
        var interpreter = new EventInterpreter(
            state, 1, pCommands, new PresentationState());
        return (interpreter, state);
    }

    private static Rm2kMap.EventCommand Cmd(
        int pCode,
        string pText = "",
        params int[] pParameters)
    {
        return new Rm2kMap.EventCommand
        {
            Code = pCode,
            Text = pText,
            Parameters = new List<int>(pParameters),
        };
    }

    private static GameSimulationState WithTroop(
        params Godot.Collections.Dictionary[] pMonsters)
    {
        var state = new GameSimulationState { MapId = 1, IsBattleActive = true };
        foreach (var m in pMonsters)
        {
            state.TroopMembers.Add(m);
        }

        return state;
    }

    // ---- 13110 Change Monster HP

    /// <summary>
    /// A constant change reaches the dispatch and moves the hit points.
    /// </summary>
    public void Test_AConstantHitPointChangeReachesTheDispatch()
    {
        var state = WithTroop(Monster(100, 100));
        var interpreter = new EventInterpreter(
            state, 1,
            new[]
            {
                Cmd(EventInterpreter.ChangeMonsterHp, "", 0, 1, 0, 30, 0),
            },
            new PresentationState());
        interpreter.ExecuteFrame();

        AssertEq(state.MonsterHp(0), 70,
            "**a constant change of thirty takes a monster from 100 to 70**");
    }

    /// <summary>
    /// The sign is a flag and not the value's own sign.
    /// </summary>
    public void Test_TheSignIsAFlagAndNotTheValuesOwnSign()
    {
        // Flag at zero mit einer negativen Zahl: der Wert wird ADDiert, weil
        // die Referenz `change` unveraendert laesst und `change = -change`
        // nur bei gesetztem Flag schreibt.
        var state = WithTroop(Monster(100, 100));
        var interpreter = new EventInterpreter(
            state, 1,
            new[]
            {
                Cmd(EventInterpreter.ChangeMonsterHp, "", 0, 0, 0, -30, 0),
            },
            new PresentationState());
        interpreter.ExecuteFrame();

        AssertEq(state.MonsterHp(0), 70,
            "**a negative number with the lose flag at zero still heals** — "
                + "the reference reads the sign from the flag and never from "
                + "the value");
    }

    /// <summary>
    /// Mode 2 is a share of the monster's own maximum.
    /// </summary>
    public void Test_ModeTwoIsAShareOfTheMonstersMaximum()
    {
        var state = WithTroop(Monster(1000, 1000));
        var interpreter = new EventInterpreter(
            state, 1,
            new[]
            {
                Cmd(EventInterpreter.ChangeMonsterHp, "", 0, 1, 2, 10, 0),
            },
            new PresentationState());
        interpreter.ExecuteFrame();

        AssertEq(state.MonsterHp(0), 900,
            "**mode 2 takes a tenth of the monster's own maximum** — a "
                + "reader that read it as another constant would have taken "
                + "ten hit points off a thousand-point boss");
    }

    /// <summary>
    /// An unknown change mode changes nothing at all.
    /// </summary>
    public void Test_AnUnknownHitPointModeChangesNothing()
    {
        var state = WithTroop(Monster(100, 100));
        var interpreter = new EventInterpreter(
            state, 1,
            new[]
            {
                Cmd(EventInterpreter.ChangeMonsterHp, "", 0, 0, 7, 30, 0),
            },
            new PresentationState());
        interpreter.ExecuteFrame();

        AssertEq(state.MonsterHp(0), 100,
            "**a mode the reference's switch does not know changes "
                + "nothing** — it has 0, 1 and 2 and no default arm that "
                + "guesses");
        var genannt = false;
        foreach (var d in state.Diagnostics)
        {
            if (d.Contains("is not 0, 1 or 2"))
            {
                genannt = true;
            }
        }

        AssertTrue(genannt,
            "and it says so — the diagnostics were "
                + string.Join(" | ", state.Diagnostics));
    }

    /// <summary>
    /// Zero hit points is a death, and it is a timer and not a removal.
    /// </summary>
    public void Test_ZeroHitPointsIsATimedDeath()
    {
        var state = WithTroop(Monster(100, 100));
        var interpreter = new EventInterpreter(
            state, 1,
            new[]
            {
                Cmd(EventInterpreter.ChangeMonsterHp, "", 0, 1, 0, 100, 0),
            },
            new PresentationState());
        interpreter.ExecuteFrame();

        AssertEq(state.MonsterHp(0), 0, "the monster is at zero");
        AssertEq(state.IsMonsterDead(0), true, "and it is dead");
        AssertEq(
            state.MonsterExitOf(0),
            GameSimulationState.MonsterExit.Timed,
            "**and it left the troop on a timer** — the reference plays the "
                + "enemy-kill sound and calls SetDeathTimer(), and a reader "
                + "that dropped him at once would have had him vanish before "
                + "the animation had anything to play on");
    }

    /// <summary>
    /// A subtraction larger than the monster has lands on zero, not below.
    /// </summary>
    public void Test_ASubtractionLargerThanTheMonsterHasLandsOnZero()
    {
        var state = WithTroop(Monster(30, 100));
        var interpreter = new EventInterpreter(
            state, 1,
            new[]
            {
                Cmd(EventInterpreter.ChangeMonsterHp, "", 0, 1, 0, 999, 0),
            },
            new PresentationState());
        interpreter.ExecuteFrame();

        AssertEq(state.MonsterHp(0), 0,
            "**zero is a floor and not a ceiling** — a reader without the "
                + "clamp would have a dead monster whose hit points count "
                + "backwards");
    }

    /// <summary>
    /// An id that is not in the troop warns and grows nothing.
    /// </summary>
    public void Test_AnEnemyIdThatIsNotInTheTroopGrowsNothing()
    {
        var state = WithTroop(Monster(100, 100));
        var interpreter = new EventInterpreter(
            state, 1,
            new[]
            {
                Cmd(EventInterpreter.ChangeMonsterHp, "", 7, 0, 0, 30, 0),
            },
            new PresentationState());
        interpreter.ExecuteFrame();

        AssertEq(state.TroopMembers.Count, 1,
            "**a bad enemy id does not create a monster** — the reference's "
                + "GetEnemy returns nothing and it warns and returns");
        var gewarnt = false;
        foreach (var d in state.Diagnostics)
        {
            if (d.Contains("invalid enemy ID 7"))
            {
                gewarnt = true;
            }
        }

        AssertTrue(gewarnt, "and it warns — the diagnostics were "
            + string.Join(" | ", state.Diagnostics));
    }

    // ---- 13120 Change Monster MP

    /// <summary>
    /// Two change modes, and there is no third.
    /// </summary>
    public void Test_SpiritPointsHaveTwoModesAndNotThree()
    {
        // 13110 hat einen Prozentmodus, 13120 nicht.
        var mitModus = WithTroop(Monster(100, 100, 50));
        var interpHp = new EventInterpreter(
            mitModus, 1,
            new[]
            {
                Cmd(EventInterpreter.ChangeMonsterHp, "", 0, 1, 2, 10, 0),
            },
            new PresentationState());
        interpHp.ExecuteFrame();
        AssertEq(mitModus.MonsterHp(0), 90,
            "13110 mode 2 is a share of the maximum");

        var ohneModus = WithTroop(Monster(100, 100, 50));
        var interpMp = new EventInterpreter(
            ohneModus, 1,
            new[]
            {
                Cmd(EventInterpreter.ChangeMonsterMp, "", 0, 0, 2, 10, 0),
            },
            new PresentationState());
        interpMp.ExecuteFrame();
        AssertEq(ohneModus.MonsterSp(0), 50,
            "**13120 mode 2 changes nothing** — the reference's switch is a "
                + "constant and a variable and no third case, so a reader "
                + "that reused the hit-point command's modes would have "
                + "changed a share of a maximum this command never reads");
    }

    /// <summary>
    /// A spirit point change reaches the dispatch.
    /// </summary>
    public void Test_ASpiritPointChangeReachesTheDispatch()
    {
        var state = WithTroop(Monster(100, 100, 50));
        var interpreter = new EventInterpreter(
            state, 1,
            new[]
            {
                Cmd(EventInterpreter.ChangeMonsterMp, "", 0, 1, 0, 20, 0),
            },
            new PresentationState());
        interpreter.ExecuteFrame();

        AssertEq(state.MonsterSp(0), 30,
            "**a constant change of twenty takes a monster from 50 spirit "
            + "points to 30**");
    }

    // ---- 13130 Change Monster Condition

    /// <summary>
    /// The second parameter is remove-or-add and not add-or-remove.
    /// </summary>
    public void Test_TheSecondParameterIsRemoveOrAdd()
    {
        var state = WithTroop(Monster(100, 100));
        state.AddMonsterCondition(0, 3);
        AssertEq(state.MonsterConditions(0).Count, 1, "the monster is poisoned");

        var interpreter = new EventInterpreter(
            state, 1,
            new[]
            {
                Cmd(EventInterpreter.ChangeMonsterCondition, "", 0, 1, 3),
            },
            new PresentationState());
        interpreter.ExecuteFrame();

        AssertEq(state.MonsterConditions(0).Count, 0,
            "**a truth in the second parameter removes the condition** — the "
                + "reference reads `bool remove = parameters[1] > 0`, and a "
                + "reader that read it as 'add' would have healed a poisoned "
                + "monster with the command meant to cure him");
    }

    /// <summary>
    /// Removing the death condition is an immediate death, and it is not the
    /// hit-point death.
    /// </summary>
    public void Test_RemovingTheDeathConditionIsAnImmediateDeath()
    {
        var state = WithTroop(Monster(100, 100));
        state.AddMonsterCondition(0, GameSimulationState.DeathConditionId);
        var interpreter = new EventInterpreter(
            state, 1,
            new[]
            {
                Cmd(
                    EventInterpreter.ChangeMonsterCondition, "", 0, 1,
                    GameSimulationState.DeathConditionId),
            },
            new PresentationState());
        interpreter.ExecuteFrame();

        AssertEq(
            state.MonsterExitOf(0),
            GameSimulationState.MonsterExit.Immediate,
            "**removing the death condition leaves the troop at once** — the "
                + "reference's own comment says the monster disappears and "
                + "does not animate death, and it is written down as an "
                + "RPG_RT bug that it reproduces");
    }

    /// <summary>
    /// Adding a condition reaches the dispatch and grows the list.
    /// </summary>
    public void Test_AddingAConditionReachesTheDispatch()
    {
        var state = WithTroop(Monster(100, 100));
        var interpreter = new EventInterpreter(
            state, 1,
            new[]
            {
                Cmd(EventInterpreter.ChangeMonsterCondition, "", 0, 0, 4),
            },
            new PresentationState());
        interpreter.ExecuteFrame();

        AssertEq(state.MonsterConditions(0).Count, 1,
            "**the command added a condition**");
        AssertTrue(state.MonsterConditions(0).Contains(4),
            "and it is the one the command named");
    }

    // ---- 13150 Show Hidden Monster

    /// <summary>
    /// The one parameter shows a monster, and there is no hiding arm.
    /// </summary>
    public void Test_TheOneParameterShowsAMonsterAndThereIsNoHidingArm()
    {
        var state = WithTroop(Monster(100, 100, 0, true));
        AssertEq(state.IsMonsterHidden(0), true, "the monster starts hidden");

        var interpreter = new EventInterpreter(
            state, 1,
            new[]
            {
                Cmd(EventInterpreter.ShowHiddenMonster, "", 0),
            },
            new PresentationState());
        interpreter.ExecuteFrame();

        AssertEq(state.IsMonsterHidden(0), false,
            "**13150 shows a monster** — the reference's whole command is "
                + "`SetHidden(false)`, so a monster is hidden in its "
                + "database row and this is the only command that shows it");
    }

    // ---- 13210 Change Battle BG

    /// <summary>
    /// The file name is in the command's text and not in its parameters.
    /// </summary>
    public void Test_TheBattleBackgroundIsInTheTextAndNotTheParameters()
    {
        var state = WithTroop(Monster(100, 100));
        var interpreter = new EventInterpreter(
            state, 1,
            new[]
            {
                // **The one parameter is a zero, and it is not the file name.**
                Cmd(EventInterpreter.ChangeBattleBg, "Battleback", 0),
            },
            new PresentationState());
        interpreter.ExecuteFrame();

        AssertEq(state.BattleBackground, "Battleback",
            "**the background came from the command's text** — the reference "
                + "writes ChangeBackground(ToString(com.string)), and a reader "
                + "that looked in parameters would have found a single zero");
    }

    /// <summary>
    /// All five commands are reached and none of them is refused.
    /// </summary>
    public void Test_AllFiveAreReachedAndNoneIsRefused()
    {
        var state = WithTroop(Monster(100, 100, 50, true));
        var interpreter = new EventInterpreter(
            state, 1,
            new[]
            {
                Cmd(EventInterpreter.ChangeMonsterHp, "", 0, 1, 0, 10, 0),
                Cmd(EventInterpreter.ChangeMonsterMp, "", 0, 1, 0, 5, 0),
                Cmd(EventInterpreter.ChangeMonsterCondition, "", 0, 0, 2),
                Cmd(EventInterpreter.ShowHiddenMonster, "", 0),
                Cmd(EventInterpreter.ChangeBattleBg, "Panorama", 0),
            },
            new PresentationState());
        for (var i = 0; i < 5; i++)
        {
            interpreter.ExecuteFrame();
        }

        foreach (var d in state.Diagnostics)
        {
            AssertTrue(!d.Contains("not wired") && !d.Contains("unsupported"),
                "a command was refused, its diagnostics were "
                    + string.Join(" | ", state.Diagnostics));
        }

        AssertEq(state.MonsterHp(0), 90, "the hit point change ran");
        AssertEq(state.MonsterSp(0), 45, "the spirit point change ran");
        AssertEq(state.MonsterConditions(0).Count, 1, "the condition ran");
        AssertEq(state.IsMonsterHidden(0), false, "the hidden monster showed");
        AssertEq(state.BattleBackground, "Panorama", "the background ran");
    }
}
