using System;
using System.Collections.Generic;

using UniversalRPG.Rm2k;
using UniversalRPG.Rm2k.Interpreter;
using UniversalRPG.Rm2k.Presentation;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Tests for 13310 Conditional Branch (the battle form), 13410 Terminate
/// Battle, 23310 Else Branch (the battle form) and 23311 End Branch (the battle
/// form) — from liblcf's <c>eventcommand.h</c> and EasyRPG's
/// <c>Game_Interpreter_Battle</c>.
/// </summary>
public partial class TestRm2kBattleBranch : TestBase
{
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

    private static Godot.Collections.Dictionary Monster(int pHp)
    {
        return new Godot.Collections.Dictionary
        {
            ["hp"] = pHp,
            ["max_hp"] = 100,
            ["sp"] = 0,
            ["max_sp"] = 50,
            ["hidden"] = 0,
        };
    }

    private static GameSimulationState Battle(params Godot.Collections.Dictionary[] pMonsters)
    {
        var state = new GameSimulationState { MapId = 1, IsBattleActive = true };
        foreach (var m in pMonsters)
        {
            state.TroopMembers.Add(m);
        }

        return state;
    }

    private static GameSimulationState WithVariables(params int[] pValues)
    {
        var state = Battle();
        foreach (var v in pValues)
        {
            state.Variables.Add(v);
        }

        return state;
    }

    private static (EventInterpreter Interpreter, GameSimulationState State) Run(
        GameSimulationState pState,
        params Rm2kMap.EventCommand[] pCommands)
    {
        var interpreter = new EventInterpreter(
            pState, 1, pCommands, new PresentationState());
        for (var i = 0; i < pCommands.Length; i++)
        {
            interpreter.ExecuteFrame();
        }

        return (interpreter, pState);
    }

    // ---- 13410 Terminate Battle

    /// <summary>
    /// 13410 is an abort, and it is a fourth result and not a defeat.
    /// </summary>
    /// <remarks>
    /// <strong>The reference's whole command is
    /// <c>MakeTerminateBattle(BattleResult::Abort)</c>.</strong> **A reader
    /// that wrote "defeat" there would have had a game that deliberately
    /// abandons a fight reach the game over screen** — and no handler is named
    /// for an abort, so the three outcome blocks stay untouched.
    /// </remarks>
    public void Test_13410IsAnAbortAndNotADefeat()
    {
        var (_, state) = Run(Battle(), Cmd(EventInterpreter.TerminateBattle));

        AssertEq(state.Result, GameSimulationState.BattleResult.Abort,
            "**13410 ends the battle as an abort** — the reference's own "
                + "BattleResult::Abort, and it is a fourth result beside "
                + "victory, escape and defeat");
        AssertEq(state.Result == GameSimulationState.BattleResult.Defeat, false,
            "**and not as a defeat** — a reader that wrote defeat here would "
                + "have had a game that abandons a fight reach the game over "
                + "screen");
    }

    /// <summary>
    /// 13410 holds its frame, and the command after it does not run.
    /// </summary>
    /// <remarks>
    /// <strong>The reference returns false</strong> — the frame stops and the
    /// result arrives later. **A reader that advanced would have run a game's
    /// victory rewards after it abandoned the fight.**
    /// </remarks>
    public void Test_13410HoldsThePage()
    {
        var state = Battle();
        var interpreter = new EventInterpreter(
            state, 1,
            new[]
            {
                Cmd(EventInterpreter.TerminateBattle),
                Cmd(EventInterpreter.ChangeBattleBg, "Rewards", 0),
            },
            new PresentationState());
        interpreter.ExecuteFrame();
        interpreter.ExecuteFrame();

        AssertEq(state.BattleBackground, "",
            "**the page after 13410 did not run** — the reference returns "
                + "false, and a reader that advanced would have shown a "
                + "game's victory rewards after it abandoned the fight");
    }

    // ---- 13310, die sechs Modi

    /// <summary>
    /// <summary>
    /// The switch comparison is a boolean equality: a third parameter of zero
    /// asks whether the switch is on.
    /// </summary>
    /// <remarks>
    /// <strong>The reference writes
    /// <c>Get(id) == (parameters[2] == 0)</c> — and <c>0 == 0</c> is
    /// <c>true</c>.</strong> So the parameter is compared with a boolean and
    /// not read as a bare "is it off": **a reader that read it that way would
    /// have had every switch in every game the wrong way round**, and the
    /// error is invisible in a test that only uses one of the two directions.
    /// </remarks>
    public void Test_TheSwitchComparisonIsABooleanEquality()
    {
        // **Parameter 0: der Schalter muss AN sein.**
        var an = Battle();
        an.Switches.Add(true);
        Run(an, Cmd(EventInterpreter.ConditionalBranchBattle, "", 0, 1, 0, 0, 0));
        AssertEq(an.LastBattleBranch, true,
            "**a third parameter of zero asks whether the switch is on** — "
                + "`0 == 0` is true, and the reference compares the switch "
                + "with that");

        var aus = Battle();
        aus.Switches.Add(false);
        Run(aus, Cmd(EventInterpreter.ConditionalBranchBattle, "", 0, 1, 0, 0, 0));
        AssertEq(aus.LastBattleBranch, false,
            "**and an off switch does not satisfy it**");

        // **Parameter 1: der Schalter muss AUS sein.**
        var aus2 = Battle();
        aus2.Switches.Add(false);
        Run(aus2, Cmd(EventInterpreter.ConditionalBranchBattle, "", 0, 1, 1, 0, 0));
        AssertEq(aus2.LastBattleBranch, true,
            "**a third parameter of one asks whether the switch is off** — "
                + "`1 == 0` is false, and an off switch equals false");

        var an2 = Battle();
        an2.Switches.Add(true);
        Run(an2, Cmd(EventInterpreter.ConditionalBranchBattle, "", 0, 1, 1, 0, 0));
        AssertEq(an2.LastBattleBranch, false,
            "**and an on switch does not satisfy that**");
    }

    /// <summary>
    /// Six comparison kinds, and a seventh leaves the result false.
    /// </summary>
    public void Test_SixComparisonKindsAndNotFive()
    {
        // **5 und 3, und jede der sechs Arten gegen beide Werte.**
        // **5 gegen 3, und die Tabelle ist die Referenzreihenfolge:**
        // 0 gleich, 1 groesser-oder-gleich, 2 kleiner-oder-gleich,
        // 3 groesser, 4 kleiner, 5 ungleich.
        foreach (var (art, erwartet) in new[]
                 {
                     (0, false),  // 5 == 3
                     (1, true),   // 5 >= 3
                     (2, false),  // 5 <= 3
                     (3, true),   // 5 > 3
                     (4, false),  // 5 < 3
                     (5, true),   // 5 != 3
                 })
        {
            // **Wert 2 aus der Variablen 2** -- der vierte Parameter ist
            // eine Variable-Id, wenn der dritte ungleich null ist.
            // **GetVariable(2) liest Variables[1]** -- die Referenz 2 zeigt
            // auf Index 1, und der Wert 3 muss dort stehen.
            var state = WithVariables(5, 3, 99);
            Run(state, Cmd(EventInterpreter.ConditionalBranchBattle, "",
                1, 1, 1, 2, art));
            AssertEq(state.LastBattleBranch, erwartet,
                "**5 and 3 with comparison " + art + "** is "
                    + (erwartet ? "true" : "false") + ", and it read "
                    + state.LastBattleBranch + " with the diagnostics "
                    + string.Join(" | ", state.Diagnostics));
        }

        var siebte = WithVariables(5, 3, 99);
        Run(siebte, Cmd(EventInterpreter.ConditionalBranchBattle, "",
            1, 1, 1, 2, 6));
        AssertEq(siebte.LastBattleBranch, false,
            "**a seventh comparison is false** — the reference's switch ends "
                + "at five and falls out with the false it started from");
    }

    /// <summary>
    /// Greater-than and greater-or-equal differ only when the two values are
    /// equal.
    /// </summary>
    /// <remarks>
    /// <strong>A mutation that turned <c>&gt;</c> into <c>&gt;=</c> survived
    /// the six-way table</strong>, because every row of that table compares 5
    /// with 3, and on those two numbers the two operators agree. <strong>A test
    /// that only ever uses unequal values cannot tell a strict comparison from
    /// a non-strict one</strong> — and a game's "if the counter is more than
    /// what I have" would have fired on equality.
    /// </remarks>
    public void Test_TheStrictComparisonsDifferOnEqualValues()
    {
        foreach (var (art, erwartet) in new[]
                 {
                     (0, true),   // 5 == 5
                     (1, true),   // 5 >= 5
                     (2, true),   // 5 <= 5
                     (3, false),  // 5 >  5
                     (4, false),  // 5 <  5
                     (5, false),  // 5 != 5
                 })
        {
            var state = WithVariables(5, 5, 0);
            Run(state, Cmd(EventInterpreter.ConditionalBranchBattle, "",
                1, 1, 1, 2, art));
            AssertEq(state.LastBattleBranch, erwartet,
                "**5 against 5 with comparison " + art + "** is "
                    + (erwartet ? "true" : "false") + ", and it read "
                    + state.LastBattleBranch);
        }
    }

    /// <summary>
    /// A third parameter of zero takes the value itself    /// <summary>
    /// A third parameter of zero takes the value itself, and any other takes
    /// a variable.
    /// </summary>
    public void Test_TheValueIsAConstantOrAVariable()
    {
        var konstant = WithVariables(5, 0, 0);
        Run(konstant, Cmd(EventInterpreter.ConditionalBranchBattle, "",
            1, 1, 0, 5, 0));
        AssertEq(konstant.LastBattleBranch, true,
            "**a third parameter of zero compares against the fourth itself** "
                + "— the reference's `value2 = com.parameters[3]`, and 5 "
                + "against 5 is equal");

        var variabel = WithVariables(5, 3, 0);
        Run(variabel, Cmd(EventInterpreter.ConditionalBranchBattle, "",
            1, 1, 1, 2, 0));
        AssertEq(variabel.LastBattleBranch, false,
            "**and any other value takes a variable** — the reference reads "
                + "`value2 = Get(parameters[3])`, and variable 2 holds 3, so "
                + "5 against 3 is not equal");

        var passt = WithVariables(5, 5, 0);
        Run(passt, Cmd(EventInterpreter.ConditionalBranchBattle, "",
            1, 1, 1, 2, 0));
        AssertEq(passt.LastBattleBranch, true,
            "**and with the right value in the variable it matches**");
    }

    /// <summary>
    /// A hero can act only above zero, and an id that is not there is a
    /// warning and a false.
    /// </summary>
    public void Test_AHeroCanActOnlyAboveZero()
    {
        var lebend = Battle();
        lebend.CurrentHp[1] = 10;
        Run(lebend, Cmd(EventInterpreter.ConditionalBranchBattle, "", 2, 1, 0, 0, 0));
        AssertEq(lebend.LastBattleBranch, true,
            "a hero with hit points acts — hp is "
            + lebend.GetActorCurrentHp(1) + " and the diagnostics were "
            + string.Join(" | ", lebend.Diagnostics));

        var tot = Battle();
        tot.CurrentHp[1] = 0;
        Run(tot, Cmd(EventInterpreter.ConditionalBranchBattle, "", 2, 1, 0, 0, 0));
        AssertEq(tot.LastBattleBranch, false, "and one at zero does not");

        var ungueltig = Battle();
        Run(ungueltig, Cmd(EventInterpreter.ConditionalBranchBattle, "",
            2, 0, 0, 0, 0));
        AssertEq(ungueltig.LastBattleBranch, false,
            "**an id that is not there is false** — the reference warns and "
                + "leaves the result at the false it started from");
        var gewarnt = false;
        foreach (var d in ungueltig.Diagnostics)
        {
            if (d.Contains("invalid actor ID 0"))
            {
                gewarnt = true;
            }
        }

        AssertTrue(gewarnt, "and it warns — the diagnostics were "
            + string.Join(" | ", ungueltig.Diagnostics));
    }

    /// <summary>
    /// A dying monster cannot act while it is still in the troop.
    /// </summary>
    /// <remarks>
    /// <strong>The reference's own killing command started a death
    /// timer,</strong> so the monster is still in the list while it animates.
    /// **A reader that asked "is he in the troop" instead of "can he act"
    /// would have had a dying boss strike back on the frame he fell.**
    /// </remarks>
    public void Test_ADyingMonsterCannotActWhileItIsStillInTheTroop()
    {
        var state = Battle(Monster(100), Monster(0));
        AssertEq(state.TroopMembers.Count, 2,
            "**the dead monster is still in the troop** — the reference gives "
                + "him a death timer and not a removal");

        // **Index 0 ist der lebende und Index 1 der sterbende.**
        Run(state, Cmd(EventInterpreter.ConditionalBranchBattle, "", 3, 0, 0, 0, 0));
        AssertEq(state.LastBattleBranch, true,
            "**the living one can act** — the mode names the monster by its "
                + "index in the troop, and index 0 is the one with hit "
                + "points");

        Run(state, Cmd(EventInterpreter.ConditionalBranchBattle, "", 3, 1, 0, 0, 0));
        AssertEq(state.LastBattleBranch, false,
            "**and the dead one cannot while he is still in the troop** — the "
                + "reference gives him a death timer and not a removal, and a "
                + "reader that asked 'is he in the troop' would have had a "
                + "dying boss strike back on the frame he fell");

        // **Und ein Index ausserhalb der Truppe ist false.**
        Run(state, Cmd(EventInterpreter.ConditionalBranchBattle, "", 3, 9, 0, 0, 0));
        AssertEq(state.LastBattleBranch, false,
            "**and an index that is not in the troop is false**");
    }

    /// <summary>
    /// The fourth mode needs a single target and the index to match.
    /// </summary>
    public void Test_TheFourthModeNeedsASingleTarget()
    {
        var einzeln = Battle();
        einzeln.TargetsSingleEnemy = true;
        einzeln.CurrentTargetIndex = 2;
        Run(einzeln, Cmd(EventInterpreter.ConditionalBranchBattle, "",
            4, 2, 0, 0, 0));
        AssertEq(einzeln.LastBattleBranch, true,
            "**the mode matches when the single target is that enemy**");

        var falscher = Battle();
        falscher.TargetsSingleEnemy = true;
        falscher.CurrentTargetIndex = 3;
        Run(falscher, Cmd(EventInterpreter.ConditionalBranchBattle, "",
            4, 2, 0, 0, 0));
        AssertEq(falscher.LastBattleBranch, false,
            "and not when it is another one");

        var alle = Battle();
        alle.TargetsSingleEnemy = false;
        alle.CurrentTargetIndex = 2;
        Run(alle, Cmd(EventInterpreter.ConditionalBranchBattle, "", 4, 2, 0, 0, 0));
        AssertEq(alle.LastBattleBranch, false,
            "**and never when every monster is targeted at once** — the "
                + "reference compares the single-target flag first, and a "
                + "reader that only compared the index would have taken a "
                + "branch in a battle with no single target");
    }

    /// <summary>
    /// The fifth mode needs the right hero and the right command.
    /// </summary>
    public void Test_TheFifthModeNeedsTheHeroAndTheCommand()
    {
        var passend = Battle();
        passend.CurrentActorId = 3;
        passend.LastBattleAction = 1;
        Run(passend, Cmd(EventInterpreter.ConditionalBranchBattle, "", 5, 3, 1, 0, 0));
        AssertEq(passend.LastBattleBranch, true,
            "**the mode matches the hero and the command they last chose**");

        var falscherHeld = Battle();
        falscherHeld.CurrentActorId = 4;
        falscherHeld.LastBattleAction = 1;
        Run(falscherHeld, Cmd(EventInterpreter.ConditionalBranchBattle, "", 5, 3, 1, 0, 0));
        AssertEq(falscherHeld.LastBattleBranch, false,
            "and not for another hero");

        var falscherBefehl = Battle();
        falscherBefehl.CurrentActorId = 3;
        falscherBefehl.LastBattleAction = 2;
        Run(falscherBefehl, Cmd(EventInterpreter.ConditionalBranchBattle, "", 5, 3, 1, 0, 0));
        AssertEq(falscherBefehl.LastBattleBranch, false,
            "**and not for another command** — the reference compares the "
                + "hero's last battle action, not the hero's name");
    }

    // ---- Der Else-Zweig

    /// <summary>
    /// A false branch runs its else block, and a true one skips it.
    /// </summary>
    /// <remarks>
    /// <strong>The sub-index is written before the skip</strong> — the
    /// reference's own order — so the else handler that comes next finds its
    /// option chosen. A reader that skipped first and wrote afterwards would
    /// have had the else branch skip itself.
    /// </remarks>
    public void Test_AFalseBranchRunsItsElseBlock()
    {
        // **Parameter 0 fragt "ist der Schalter AN"** -- also nimmt der
        // ausgeschaltete Schalter den else-Zweig, und genau den wollen wir
        // hier sehen.
        var falsch = Battle();
        falsch.Switches.Add(false);
        // **Der then-Block steht VOR dem else-Zweig**, so wie ihn der
        // Editor schreibt: der Else-Zweig wird uebersprungen, wenn die
        // Bedingung wahr ist.
        var (_, state) = Run(
            falsch,
            Cmd(EventInterpreter.ConditionalBranchBattle, "", 0, 1, 0, 0, 0),
            Cmd(EventInterpreter.ChangeBattleBg, "Then", 0),
            Cmd(EventInterpreter.EndBranchBattle),
            Cmd(EventInterpreter.ElseBranchBattle),
            Cmd(EventInterpreter.ChangeBattleBg, "Else", 0));

        AssertEq(state.BattleBackground, "Else",
            "**the false branch ran its else block** — the background is '"
                + state.BattleBackground + "' and the diagnostics were "
                + string.Join(" | ", state.Diagnostics));

        // **Und der eingeschaltete Schalter nimmt den then-Block** -- der
        // else-Zweig wird dann uebersprungen.
        var richtig = Battle();
        richtig.Switches.Add(true);
        var (_, state2) = Run(
            richtig,
            Cmd(EventInterpreter.ConditionalBranchBattle, "", 0, 1, 0, 0, 0),
            Cmd(EventInterpreter.ChangeBattleBg, "Then", 0),
            Cmd(EventInterpreter.EndBranchBattle),
            Cmd(EventInterpreter.ElseBranchBattle),
            Cmd(EventInterpreter.ChangeBattleBg, "Else", 0),
            Cmd(EventInterpreter.EndBranchBattle));

        AssertEq(state2.BattleBackground, "Then",
            "**and the true branch skipped it and ran the then block**");
    }

    /// <summary>
    /// Every one of the four codes reaches the dispatch.
    /// </summary>
    /// <remarks>
    /// <strong>Two mutations survived because no test drove 23311 and no
    /// test asked the hero question with a false answer.</strong> 23311 is
    /// <c>return true;</c> and changes nothing — <strong>and a command that
    /// changes nothing produces no diagnostic</strong>, so a dispatch that
    /// pointed it at nothing looked exactly like one that ran it. The fix is
    /// the same shape as the shop closers: drive the command and ask what
    /// follows it.
    /// </remarks>
    public void Test_AllFourCodesReachTheDispatch()
    {
        foreach (var (code, sagt) in new[]
                 {
                     (EventInterpreter.TerminateBattle, "Terminate battle"),
                     (EventInterpreter.ConditionalBranchBattle,
                         "Conditional branch, battle"),
                     (EventInterpreter.ElseBranchBattle, "Battle else branch"),
                     (EventInterpreter.EndBranchBattle, "End battle branch"),
                 })
        {
            var state = Battle(Monster(100));
            state.Switches.Add(false);
            state.SubcommandIndex = GameSimulationState.SubcommandSentinel;
            state.BattleAnimationDurations[1] = 5;
            var interpreter = new EventInterpreter(
                state, 1, new[] { Cmd(code, "", 0, 1, 0, 0, 0) },
                new PresentationState());
            interpreter.ExecuteFrame();

            var gesagt = false;
            foreach (var d in state.Diagnostics)
            {
                if (d.Contains(sagt))
                {
                    gesagt = true;
                }
            }

            AssertTrue(gesagt,
                "**command " + code + " reached the dispatch and said "
                    + sagt + "** — a code that fell into the default arm "
                    + "would have said Unsupported instead, and the "
                    + "diagnostics were "
                    + string.Join(" | ", state.Diagnostics));
            var ungestuetzt = false;
            foreach (var d in state.Diagnostics)
            {
                if (d.Contains("Unsupported"))
                {
                    ungestuetzt = true;
                }
            }

            AssertTrue(!ungestuetzt,
                "**and no command fell through to the default arm** — the "
                    + "diagnostics were "
                    + string.Join(" | ", state.Diagnostics));
        }
    }

    /// <summary>
    /// A hero at zero cannot act, and the answer is not a constant.
    /// </summary>
    /// <remarks>
    /// <strong>The false answer is the one a mutation survives.</strong> A
    /// rule that made <c>CanHeroAct</c> return true died on the living-hero
    /// test, but a rule that made the *caller* return true would have left
    /// the living case green and only the dead one red — and the dead case
    /// is the one that says something about the game.
    /// </remarks>
    public void Test_AHeroAtZeroIsAskedAndAnswersNo()
    {
        var tot = Battle();
        tot.CurrentHp[1] = 0;
        var interpreter = new EventInterpreter(
            tot, 1,
            new[]
            {
                Cmd(EventInterpreter.ConditionalBranchBattle, "", 2, 1, 0, 0, 0),
            },
            new PresentationState());
        interpreter.ExecuteFrame();

        AssertEq(tot.LastBattleBranch, false,
            "**the hero at zero answered no** — the reference's own CanAct() "
                + "is false for him, and a reader that answered yes would "
                + "have had a game's dead hero act in a cutscene");
    }

    /// <summary>
    /// The end branch is a bare return true, like every other one.    /// <summary>
    /// The end branch is a bare return true, like every other one.
    /// </summary>
    public void Test_TheEndBranchIsABareReturnTrue()
    {
        var state = Battle();
        var interpreter = new EventInterpreter(
            state, 1,
            new[]
            {
                Cmd(EventInterpreter.EndBranchBattle),
                Cmd(EventInterpreter.ChangeBattleBg, "After", 0),
            },
            new PresentationState());
        interpreter.ExecuteFrame();
        interpreter.ExecuteFrame();

        AssertEq(state.BattleBackground, "After",
            "**23311 is `return true` and moves on** — a branch is a structure "
                + "and this only says the structure is over");
    }
}
