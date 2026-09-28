using System;
using System.Collections.Generic;

using UniversalRPG.Rm2k;
using UniversalRPG.Rm2k.Interpreter;
using UniversalRPG.Rm2k.Presentation;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Tests for 11210 Show Battle Animation and 13260 Show Battle Animation (the
/// battle form) — from liblcf's <c>eventcommand.h</c> and EasyRPG's
/// <c>Game_Interpreter_Battle::CommandShowBattleAnimation</c>.
/// </summary>
/// <remarks>
/// <para>
/// The two codes are the same command: the reference's dispatch hands both to
/// <c>CmdSetup&lt;&amp;CommandShowBattleAnimation, 3&gt;</c> with no second
/// implementation. <strong>They differ in their number and in nothing
/// else.</strong>
/// </para>
/// </remarks>
public partial class TestRm2kBattleAnimation : TestBase
{
    private static Rm2kMap.EventCommand Cmd(
        int pCode,
        string pText,
        params int[] pParameters)
    {
        return new Rm2kMap.EventCommand
        {
            Code = pCode,
            Text = pText,
            Parameters = new List<int>(pParameters),
        };
    }

    private static GameSimulationState WithAnimation(int pId, int pFrames)
    {
        var state = new GameSimulationState { MapId = 1, IsBattleActive = true };
        state.BattleAnimationDurations[pId] = pFrames;
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

    // ---- Der Dispatch

    /// <summary>
    /// Both codes reach the dispatch, and they behave the same.
    /// </summary>
    /// <remarks>
    /// <strong>The reference hands both to one method</strong> — there is no
    /// second implementation — so a reader that gave them different behaviour
    /// would have invented a difference the format does not have.
    /// </remarks>
    public void Test_BothCodesReachTheDispatchAndBehaveTheSame()
    {
        foreach (var code in new[]
                 {
                     EventInterpreter.ShowBattleAnimation,
                     EventInterpreter.ShowBattleAnimationBattle,
                 })
        {
            var state = WithAnimation(5, 24);
            Run(state, Cmd(code, "", 5, 1, 0, 0));

            AssertEq(state.BattleAnimationId, 5,
                "**command " + code + " reached the dispatch and played the "
                    + "animation**");
            AssertEq(state.BattleAnimationFrames, 24,
                "and it carried the animation's own length");
        }
    }

    // ---- Die Nummerierung der beiden Seiten

    /// <summary>
    /// Allies count from one and enemies from zero.
    /// </summary>
    /// <remarks>
    /// <strong>The reference subtracts one for a party target and not for a
    /// monster target.</strong> So a target of 0 is the first enemy and the
    /// <em>zeroth</em> ally, which does not exist — <strong>and a reader that
    /// used one numbering for both would have played a game's first hero's
    /// animation on its second hero.</strong>
    /// </remarks>
    public void Test_AlliesCountFromOneAndEnemiesFromZero()
    {
        var feind = WithAnimation(5, 10);
        Run(feind, Cmd(EventInterpreter.ShowBattleAnimation, "", 5, 2, 0, 0));
        AssertEq(feind.BattleAnimationTarget, 2,
            "**an enemy target is the parameter unchanged** — the reference "
                + "does not subtract for a monster");

        var held = WithAnimation(5, 10);
        Run(held, Cmd(EventInterpreter.ShowBattleAnimation, "", 5, 2, 0, 1));
        AssertEq(held.BattleAnimationTarget, 1,
            "**and an ally target is the parameter minus one** — the "
                + "reference's own `target -= 1` for a party member");

        // Und die Null: der nullte Held existiert nicht, der nullte
        // Monster schon.
        var heldNull = WithAnimation(5, 10);
        Run(heldNull, Cmd(EventInterpreter.ShowBattleAnimation, "", 5, 0, 0, 1));
        AssertEq(heldNull.BattleAnimationTarget, -1,
            "**a target of zero on the party is minus one after the "
                + "subtraction** — which is out of range, and the reference's "
                + "own range check then finds no battler");
    }

    // ---- Der vierte Parameter

    /// <summary>
    /// A three-wide command aims at the enemies, and only a four-wide one at
    /// the party.
    /// </summary>
    /// <remarks>
    /// <strong>The reference reads the fourth parameter only when there is
    /// one</strong> — <c>if (IsRPG2k3() &amp;&amp; parameters.size() &gt;
    /// 3)</c> — so a reader that required four would have refused every 2K
    /// game, and one that read the fourth unconditionally would have shown a
    /// 2K game's "aim at the party" as "aim at the enemies".
    /// </remarks>
    public void Test_TheFourthParameterIsReadOnlyWhenThereIsOne()
    {
        var drei = WithAnimation(5, 10);
        Run(drei, Cmd(EventInterpreter.ShowBattleAnimation, "", 5, 1, 0));
        AssertEq(drei.BattleAnimationOnAllies, false,
            "**a three-wide command aims at the enemies** — the reference "
                + "reads no fourth parameter and its flag stays false");

        var vier = WithAnimation(5, 10);
        Run(vier, Cmd(EventInterpreter.ShowBattleAnimation, "", 5, 1, 0, 1));
        AssertEq(vier.BattleAnimationOnAllies, true,
            "**and a four-wide one aims at the party**");
    }

    // ---- Das negative Ziel

    /// <summary>
    /// A negative target is the whole side, and the flag says which.
    /// </summary>
    public void Test_ANegativeTargetIsTheWholeSide()
    {
        var alleFeinde = WithAnimation(5, 10);
        Run(alleFeinde, Cmd(EventInterpreter.ShowBattleAnimation, "", 5, -1, 0, 0));
        AssertEq(alleFeinde.BattleAnimationOnAllTargets, true,
            "**a target of minus one is the whole side** — the reference reads "
                + "`target < 0` and collects the party or the enemy party");
        AssertEq(alleFeinde.BattleAnimationOnAllies, false,
            "**and without the flag that is the whole enemy party** — a "
                + "reader that treated -1 as 'no target' would have played "
                + "nothing at all");

        var alleHelden = WithAnimation(5, 10);
        Run(alleHelden, Cmd(EventInterpreter.ShowBattleAnimation, "", 5, -1, 0, 1));
        AssertEq(alleHelden.BattleAnimationOnAllies, true,
            "**and with the flag it is the whole party**");
    }

    /// <summary>
    /// A target of zero is the first enemy and not the whole side.
    /// </summary>
    /// <remarks>
    /// <strong>The reference reads <c>target &lt; 0</c> and not
    /// <c>&lt;= 0</c>.</strong> A mutation that made zero the whole side
    /// survived, because the test for a target of zero on the party only read
    /// <c>BattleAnimationTarget</c> — <strong>and the mutation left that value
    /// alone.</strong> The flag is the field that says "everything", and it is
    /// the field the assertion has to name.
    /// </remarks>
    public void Test_ATargetOfZeroIsOneBattlerAndNotTheWholeSide()
    {
        var ersterFeind = WithAnimation(5, 10);
        Run(ersterFeind, Cmd(EventInterpreter.ShowBattleAnimation, "", 5, 0, 0, 0));
        AssertEq(ersterFeind.BattleAnimationOnAllTargets, false,
            "**a target of zero is the first enemy** — the reference reads "
                + "`target < 0` for the whole side and not `<= 0`, so a "
                + "reader that made zero the whole side would have played "
                + "one sword animation on every monster");
        AssertEq(ersterFeind.BattleAnimationTarget, 0,
            "**and it is the zeroth enemy** — no subtraction on that side");

        var ersterHeld = WithAnimation(5, 10);
        Run(ersterHeld, Cmd(EventInterpreter.ShowBattleAnimation, "", 5, 0, 0, 1));
        AssertEq(ersterHeld.BattleAnimationOnAllTargets, false,
            "**and on the party side a target of zero is not the whole party "
                + "either** — it is the zeroth ally, which the reference's own "
                + "range check then finds no battler for");
    }

    /// <summary>
    /// The wait is the animation's own length and not a constant.
    /// </summary>
    /// <remarks>
    /// <strong>Two mutations, one test: the wait never happening, and the
    /// wait being a fixed ten frames.</strong> The first shows up as the next
    /// command running; the second shows up as the next command running after
    /// exactly ten frames — <strong>and a test that only steps once cannot see
    /// the difference between ten and twenty.</strong> So this drives the
    /// interpreter and asks it twice.
    /// </remarks>
    public void Test_TheWaitIsTheAnimationsOwnLengthAndNotAConstant()
    {
        foreach (var frames in new[] { 3, 10, 20, 45 })
        {
            var state = WithAnimation(5, frames);
            var interpreter = new EventInterpreter(
                state, 1,
                new[]
                {
                    Cmd(EventInterpreter.ShowBattleAnimation, "", 5, 0, 1, 0),
                    Cmd(EventInterpreter.ChangeBattleBg, "Panorama", 0),
                },
                new PresentationState());
            interpreter.ExecuteFrame();

            var gelaufen = 0;
            for (var i = 0; i < frames + 4; i++)
            {
                if (state.BattleBackground == "Panorama")
                {
                    gelaufen = i;
                    break;
                }

                interpreter.ExecuteFrame();
            }

            // **Ein Frame mehr, und das ist die Referenzmechanik und nicht
            // ein Rechenfehler.** `ExecuteFrame` prueft das Wartebudget
            // ZUERST, also laeuft der Frame, der die Animation ausloest,
            // noch nicht in der Wartezeit -- die Seite steht danach genau
            // `frames` Frames, und der Befehl danach braucht einen weiteren.
            // **Ein Leser, der `frames - 1` gesetzt haette, wuerde die letzte
            // Sekunde der Animation abgeschnitten haben.**
            AssertEq(gelaufen, frames + 1,
                "**an animation of " + frames + " frames held the page for "
                    + frames + " and released it on frame " + gelaufen + "** — "
                    + "the reference writes the animation's own count into "
                    + "its wait, and this reader's budget is checked before "
                    + "the command runs");
        }
    }

    // ---- Die Framezahl    // ---- Die Framezahl

    /// <summary>
    /// The frames come out of the animation's own timing rows.
    /// </summary>
    /// <remarks>
    /// <strong>The reference's wait is
    /// <c>BattleAnimationBattle::GetFrames()</c>,</strong> so a reader that
    /// invented a duration would have held the page for a number the game
    /// never wrote — and a battle where the hero's sword animation is 30
    /// frames would have frozen for 12.
    /// </remarks>
    public void Test_TheFramesComeOutOfTheAnimationsOwnTimings()
    {
        foreach (var frames in new[] { 1, 12, 30, 240 })
        {
            var state = WithAnimation(5, frames);
            Run(state, Cmd(EventInterpreter.ShowBattleAnimation, "", 5, 0, 0, 0));
            AssertEq(state.BattleAnimationFrames, frames,
                "**an animation of " + frames + " frames reports " + frames
                    + "** — the number is the animation's own, not a constant");
        }
    }

    /// <summary>
    /// An animation that is not in the table plays nothing and waits for
    /// nothing.
    /// </summary>
    /// <remarks>
    /// <strong>The reference's <c>GetElement</c> returns nothing, warns, and
    /// returns zero frames</strong> — so a game with a mistyped animation id
    /// waits for nothing rather than for ever, and a reader that defaulted to
    /// some default length would have frozen a game's page on a typo.
    /// </remarks>
    public void Test_AnAnimationThatIsNotInTheTableDoesNothing()
    {
        var state = WithAnimation(5, 30);
        Run(state, Cmd(EventInterpreter.ShowBattleAnimation, "", 99, 0, 1, 0));

        AssertEq(state.BattleAnimationId, null,
            "**an animation that is not in the table played nothing**");
        var gewarnt = false;
        foreach (var d in state.Diagnostics)
        {
            if (d.Contains("is not in the table"))
            {
                gewarnt = true;
            }
        }

        AssertTrue(gewarnt,
            "**and it warns** — the diagnostics were "
                + string.Join(" | ", state.Diagnostics));
    }

    // ---- Das Warten

    /// <summary>
    /// The wait flag holds the page for the animation's own length.
    /// </summary>
    public void Test_TheWaitFlagHoldsThePageForTheAnimationsLength()
    {
        var wartend = WithAnimation(5, 20);
        var (interpreter, state) = Run(
            wartend,
            Cmd(EventInterpreter.ShowBattleAnimation, "", 5, 0, 1, 0),
            Cmd(EventInterpreter.ChangeBattleBg, "", 0));

        AssertEq(state.BattleAnimationWaitRequested, true,
            "**the command asked to wait**");
        // **Und der naechste Befehl ist noch nicht dran**, weil die Wartezeit
        // im Frame-Budget steckt.
        AssertEq(state.BattleBackground, "",
            "**and the next command has not run yet** — the wait is the "
                + "animation's own 20 frames, and a reader that waited for a "
                + "constant would have let the cutscene run on");

        var nicht = WithAnimation(5, 20);
        var (interpreter2, state2) = Run(
            nicht,
            Cmd(EventInterpreter.ShowBattleAnimation, "", 5, 0, 0, 0),
            Cmd(EventInterpreter.ChangeBattleBg, "Panorama", 0));
        AssertEq(state2.BattleBackground, "Panorama",
            "**and without the wait flag the next command runs at once**");
    }
}
