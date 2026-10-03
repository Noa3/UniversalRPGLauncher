using System;
using System.Collections.Generic;

using UniversalRPG.Rm2k;
using UniversalRPG.Rm2k.Interpreter;
using UniversalRPG.Rm2k.Presentation;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Tests for 11060 Pan Screen, 11340 Proceed With Movement and 11350 Halt All
/// Movement, from liblcf's <c>Code::PanScreen</c>,
/// <c>Code::ProceedWithMovement</c> and <c>Code::HaltAllMovement</c> and
/// EasyRPG's <c>Game_Interpreter_Map::CommandPanScreen</c>,
/// <c>CommandProceedWithMovement</c> and <c>CommandHaltAllMovement</c>.
/// </summary>
public partial class TestRm2kPanScreen : TestBase
{
    private static Rm2kMap.EventCommand Cmd(int pCode, params int[] pParameters)
    {
        return new Rm2kMap.EventCommand
        {
            Code = pCode,
            Text = "",
            Parameters = new List<int>(pParameters),
        };
    }

    private static Rm2kMap.EventCommand Pan(
        int pMode,
        int pDirection,
        int pDistance,
        int pSpeed,
        int pWait)
    {
        return Cmd(
            EventInterpreter.PanScreen, pMode, pDirection, pDistance,
            pSpeed, pWait);
    }

    private static (EventInterpreter Interpreter, GameSimulationState State) Run(
        params Rm2kMap.EventCommand[] pCommands)
    {
        var state = new GameSimulationState { MapId = 1 };
        var presentation = new PresentationState();
        var interpreter = new EventInterpreter(
            state, 1, pCommands, presentation);
        interpreter.ExecuteFrame();
        return (interpreter, state);
    }

    private static (EventInterpreter Interpreter, GameSimulationState State) Start(
        params Rm2kMap.EventCommand[] pCommands)
    {
        // **Und  dieser  Test  benutzt  `10710`  nur  als
        //  Beispielbefehl  --  und  nicht  als  Kampf.**
        //
        // **Und  seit  der  Begegnung  ihre  Gegner  aus  der  Bank
        //  baut,  startet  sie  ohne  Bank  nicht  mehr** --
        // **und  dann  prueft  der  Test  den  Kampf  statt  den
        //  Pan.**  **Und  die  Bank  gehoert  dazu.**
        var state = new GameSimulationState
        {
            MapId = 1,
            DatabaseData = Rm2kBegegnungsFixture.Bank(),
        };
        var interpreter = new EventInterpreter(
            state, 1, pCommands, new PresentationState());
        return (interpreter, state);
    }

    // ---- Der Befehl erreicht den Dispatch

    /// <summary>
    /// A pan command reaches the dispatch and asks for a scroll.
    /// </summary>
    /// <remarks>
    /// <strong>This is the test the state could not write.</strong> Every pan
    /// field exists in the simulation, and a test that only checked "does the
    /// state have a pan" would have passed for ever on a dispatch that started
    /// nothing.
    /// </remarks>
    public void Test_APanCommandReachesTheDispatch()
    {
        var (_, state) = Run(Pan(2, 1, 6, 3, 0));

        AssertEq(state.PanLastMode, GameSimulationState.PanMode.Pan,
            "a pan of six tiles to the right at speed three");
        AssertEq(
            state.PanLastDirection,
            GameSimulationState.PanDirection.Right,
            "the direction");
        AssertEq(state.PanLastDistance, 6, "the distance");
        AssertEq(state.PanSpeed, 3, "the speed");
        AssertEq(state.IsPanActive, true, "and the pan is running");
    }

    /// <summary>
    /// A diagonal is one step on each axis, and not two.
    /// </summary>
    public void Test_ADiagonalIsOneStepOnEachAxis()
    {
        var (_, state) = Run(Pan(2, 5, 4, 3, 0));

        AssertEq(state.PanTargetX, 4,
            "a four step pan to the upper right moves four on x");
        AssertEq(state.PanTargetY, -4,
            "and four up on y, and not eight — a diagonal is one step on each "
                + "axis, and a reader that added them would have ended past the "
                + "corner");
    }

    /// <summary>
    /// A cardinal moves one axis only.
    /// </summary>
    public void Test_ACardinalMovesOneAxisOnly()
    {
        var (_, up) = Run(Pan(2, 0, 5, 3, 0));
        AssertEq(up.PanTargetX, 0, "a pan up does not move x");
        AssertEq(up.PanTargetY, -5, "a five step pan up moves five up");

        var (_, right) = Run(Pan(2, 1, 5, 3, 0));
        AssertEq(right.PanTargetX, 5,
            "a five step pan right moves five right");
        AssertEq(right.PanTargetY, 0, "and a pan right does not move y");
    }

    /// <summary>
    /// The speed is clamped to 1 to 6, and a game that wrote a zero or a nine
    /// still gets a pan.
    /// </summary>
    public void Test_TheSpeedIsClampedAndNotRefused()
    {
        var (_, low) = Run(Pan(2, 1, 6, 0, 0));
        AssertEq(low.PanSpeed, 1,
            "a speed of zero is repaired to one, and the pan still runs");

        var (_, high) = Run(Pan(2, 1, 6, 9, 0));
        AssertEq(high.PanSpeed, 6, "a speed of nine is repaired to six");
        AssertEq(high.PanFramesLeft, 1,
            "and it still has a frame to run — a reader that refused would "
                + "have stopped the event on a number the engine repairs");
    }

    /// <summary>
    /// An unknown mode does nothing at all, and says so.
    /// </summary>
    public void Test_AnUnknownModeDoesNothingAndSaysSo()
    {
        var (_, state) = Run(Pan(4, 1, 6, 3, 0));

        AssertEq(state.IsPanActive, false,
            "a mode the reference does not know starts no pan — its switch "
                + "has 0, 1, 2 and 3 and nothing else");
        AssertEq(state.PanLastMode, GameSimulationState.PanMode.Unlock,
            "and it is not taken for a pan");
        var genannt = false;
        foreach (var d in state.Diagnostics)
        {
            if (d.Contains("is not 0, 1, 2 or 3"))
            {
                genannt = true;
            }
        }

        AssertTrue(genannt,
            "an unknown mode says so — the diagnostics were "
                + string.Join(" | ", state.Diagnostics));
    }

    /// <summary>
    /// Lock and unlock hold the camera without starting a pan.
    /// </summary>
    public void Test_LockAndUnlockHoldTheCameraWithoutStartingAPan()
    {
        var (_, locked) = Run(Pan(0, 0, 0, 0, 0));
        AssertEq(locked.IsPanLocked, true, "mode 0 locks the camera");
        AssertEq(locked.IsPanActive, false,
            "and mode 0 starts no pan, it only holds");
        AssertEq(locked.PanLastMode, GameSimulationState.PanMode.Lock,
            "**and mode 0 is recorded as a lock and not as a pan** — a "
                + "reader that reported every mode as a pan would have a "
                + "game's hold-the-camera command read as a scroll");

        var (_, unlocked) = Run(Pan(1, 0, 0, 0, 0));
        AssertEq(unlocked.IsPanLocked, false, "mode 1 unlocks the camera");
        AssertEq(unlocked.PanLastMode, GameSimulationState.PanMode.Unlock,
            "and mode 1 is recorded as an unlock");

        // Und der Reset, der vierte Modus.
        var (_, reset) = Run(Pan(3, 0, 0, 3, 0));
        AssertEq(reset.PanLastMode, GameSimulationState.PanMode.Reset,
            "mode 3 is recorded as a reset");
    }

    /// <summary>
    /// A lock does not stop a pan that is already running.
    /// </summary>
    public void Test_ALockDoesNotStopARunningPan()
    {
        var (erster, state) = Start(Pan(2, 1, 6, 3, 0));
        erster.ExecuteFrame();
        AssertEq(state.IsPanActive, true, "the first pan started");
        var frames = state.PanFramesLeft;

        var (locker, _) = Start(Pan(0, 0, 0, 0, 0));
        locker.ExecuteFrame();

        AssertEq(state.IsPanActive, true,
            "a lock does not stop a pan that is already running — the "
                + "reference calls LockPan() and nothing else, and a reader "
                + "that halted the pan would freeze a camera mid scroll");
        AssertEq(state.PanFramesLeft, frames,
            "and a lock does not change the pan's remaining frames");
    }

    /// <summary>
    /// The wait is the distance over the speed, rounded up.
    /// </summary>
    public void Test_TheWaitIsTheRoundedDistanceOverTheSpeed()
    {
        // Fuenf bei drei: 5/3 = 1 und 5%3 != 0, also zwei.
        var (_, rounded) = Run(Pan(2, 1, 5, 3, 1));
        AssertEq(rounded.PanFramesLeft, 2,
            "a five tile pan at speed three waits two frames and not one — "
                + "the reference rounds up, so a pan never ends the frame it "
                + "started in");

        // Sechs bei drei: 6/3 = 2 und 6%3 == 0, also zwei. Nicht drei.
        var (_, even) = Run(Pan(2, 1, 6, 3, 1));
        AssertEq(even.PanFramesLeft, 2,
            "a six tile pan at speed three waits exactly two frames");
    }

    /// <summary>
    /// A pan with the wait flag spends its frames, and one without it does not.
    /// </summary>
    /// <remarks>
    /// <strong>The wait is not the return value, and this test had it
    /// wrong twice before it had it right.</strong> `ExecuteFrame` returns true
    /// while the event runs — the wait is taken from the frame budget it
    /// checks first, so the pan is already past its command when the wait
    /// starts. The measurable difference is <em>whether the next command ran
    /// yet</em>, not what the call returned.
    /// </remarks>
    public void Test_APanWithTheWaitFlagSpendsItsFrames()
    {
        // Ohne Warteflag: beide Befehle sind nach dem ersten Frame durch.
        var (kurz, kurzState) = Start(
            Pan(2, 1, 5, 3, 0), Cmd(EventInterpreter.EnemyEncounter, 0, 1,
                0, 0, 0, 0));
        kurz.ExecuteFrame();
        kurz.ExecuteFrame();
        AssertEq(kurzState.IsBattleActive, true,
            "a pan without the wait flag runs the next command immediately");

        // Mit Warteflag: nach dem ersten Frame ist der Kampf noch nicht da.
        var (lang, langState) = Start(
            Pan(2, 1, 5, 3, 1), Cmd(EventInterpreter.EnemyEncounter, 0, 1,
                0, 0, 0, 0));
        lang.ExecuteFrame();
        AssertEq(langState.IsBattleActive, false,
            "**a pan that was asked to wait has not run its next command** — "
                + "a cutscene whose camera is still sliding must not open a "
                + "battle on the frame the pan started");
    }

    // ---- Die beiden Bewegungsbefehle

    /// <summary>
    /// 11340 sets a flag, and it is one flag and not two waits.
    /// </summary>
    public void Test_11340SetsAFlagAndNotACounter()
    {
        var (_, once) = Run(Cmd(EventInterpreter.ProceedWithMovement));
        AssertEq(once.ProceedWithMovement, true,
            "11340 sets the wait for movement");

        var (erster, state) = Start(
            Cmd(EventInterpreter.ProceedWithMovement));
        erster.ExecuteFrame();
        var (zweiter, _) = Start(Cmd(EventInterpreter.ProceedWithMovement));
        zweiter.ExecuteFrame();
        AssertEq(state.ProceedWithMovement, true,
            "a second 11340 is the same flag written twice and not two "
                + "waits — the reference writes true and nothing else");
    }

    /// <summary>
    /// 11350 stops the map's pending moves and the running pan.
    /// </summary>
    public void Test_11350StopsTheMapAndTheCamera()
    {
        // **One ExecuteFrame runs one command**, which is the shape every
        // other command in this interpreter has -- so the two setup commands
        // need two frames and not one.
        var (start, state) = Start(
            Cmd(EventInterpreter.ProceedWithMovement),
            Pan(2, 1, 6, 3, 0),
            Cmd(EventInterpreter.HaltAllMovement));
        start.ExecuteFrame();
        AssertEq(state.ProceedWithMovement, true, "the flag was set");
        AssertEq(state.IsPanActive, false,
            "and after one frame the pan has not run yet, because a frame "
                + "runs exactly one command");
        start.ExecuteFrame();
        AssertEq(state.IsPanActive, true, "and now the camera is sliding");

        start.ExecuteFrame();

        AssertEq(state.ProceedWithMovement, false,
            "11350 clears the wait for movement");
        AssertEq(state.IsPanActive, false,
            "and 11350 stops the camera — the reference's call is on the "
                + "map's move list, and a reader that left a pan running "
                + "stopped less than everything");
    }

    /// <summary>
    /// A pan advances the page, and a dispatch that does not advance repeats it.
    /// </summary>
    /// <remarks>
    /// <strong>This is the rule nine of ten mutations could not kill.</strong> A
    /// dispatch that runs the pan and then returns true without writing the
    /// index forward would run the same command for ever — and a test that
    /// only checked the pan's state would pass, because the state was right
    /// and only the page was stuck.
    /// </remarks>
    public void Test_APanAdvancesThePage()
    {
        var (interpreter, state) = Start(
            Pan(2, 1, 5, 3, 0),
            Cmd(EventInterpreter.ProceedWithMovement));

        // Der Pan laeuft im ersten Frame.
        interpreter.ExecuteFrame();
        AssertEq(state.PanLastMode, GameSimulationState.PanMode.Pan,
            "the pan ran in the first frame");

        // Und im zweiten Frame ist der naechste Befehl dran -- und nicht
        // wieder der Pan.
        interpreter.ExecuteFrame();
        AssertEq(state.ProceedWithMovement, true,
            "**the second frame ran the next command and not the pan again** "
                + "— a dispatch that does not advance would repeat the pan "
                + "for ever, and the state would have looked right");
    }

    /// <summary>
    /// All three commands are reached and none of them is refused.
    /// </summary>
    public void Test_TheThreeCommandsAreReachedAndNoneIsRefused()
    {
        var (_, state) = Run(
            Pan(2, 0, 3, 2, 0),
            Cmd(EventInterpreter.ProceedWithMovement),
            Cmd(EventInterpreter.HaltAllMovement));

        foreach (var d in state.Diagnostics)
        {
            AssertTrue(!d.Contains("not wired") && !d.Contains("unsupported"),
                "a command was refused, its diagnostics were "
                    + string.Join(" | ", state.Diagnostics));
        }
    }
}
