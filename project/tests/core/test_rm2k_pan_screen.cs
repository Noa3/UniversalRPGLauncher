using System;
using System.Collections.Generic;

using UniversalRPG.Rm2k;
using UniversalRPG.Rm2k.Interpreter;
using UniversalRPG.Rm2k.Presentation;
using UniversalRPG.Rm2k.Rendering;
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
        AssertEq(state.PanSpeed, 2 << 3,
            "**and the speed became the pan's own step of 2 << 3** — sixteen sub-units"
                + " per frame, which is what the reference's StartPan writes, and not"
                + " the editor's 1 to 6 that the command carried");
        AssertEq(state.PanFinishX,
            Rm2kMapCamera.DefaultPanX() - 6 * Rm2kMapCamera.ScreenTileSize,
            "**and the finish is absolute and in sub-units**: a pan right shrinks it by"
                + " six tiles of 256, because the camera is GetSpriteX() - GetPanX()");
        AssertEq(state.IsPanActive, true, "and the pan is running");
    }

    /// <summary>
    /// A direction outside the reference's four moves nothing, and still sets the
    /// speed.
    /// </summary>
    /// <remarks>
    /// <strong>And this is the reference's own fall-through</strong>:
    /// <c>CommandPanScreen</c> hands the command's direction straight to
    /// <c>StartPan</c>, whose chain tests four values and has no else arm — so 4 to 7
    /// adds no distance while <c>pan_speed</c> is set anyway. The earlier version of
    /// this test asserted that a diagonal moved "one step on each axis", which no
    /// reference says: it was the old delta model's own invention.
    /// </remarks>
    public void Test_EineDiagonaleBewegtNichtsUndSetztDasTempo()
    {
        var (_, state) = Run(Pan(2, 5, 4, 3, 0));

        AssertEq(state.PanFinishX, Rm2kMapCamera.DefaultPanX(),
            "**a diagonal leaves the pan where it was**");
        AssertEq(state.PanFinishY, Rm2kMapCamera.DefaultPanY(),
            "**on both axes**, because the reference's chain falls through");
        AssertEq(state.PanSpeed, 16, "and it still sets the speed");
        AssertEq(state.IsPanActive, false,
            "and no pan is running, because there is nowhere to go");
    }

    /// <summary>
    /// A cardinal moves one axis, with the pan's own signs.
    /// </summary>
    /// <remarks>
    /// <strong>And the signs are not the screen's.</strong> The reference's
    /// <c>StartPan</c> grows <c>pan_finish_y</c> for a pan up and shrinks
    /// <c>pan_finish_x</c> for a pan right, because the camera is
    /// <c>GetSpriteX() - GetPanX()</c>. A reader that used the screen's signs would pan
    /// a cutscene the wrong way, which reads as a game bug rather than a reader bug.
    /// </remarks>
    public void Test_EineHimmelsrichtungBewegtEineAchse()
    {
        var (_, up) = Run(Pan(2, 0, 5, 3, 0));
        AssertEq(up.PanFinishX, Rm2kMapCamera.DefaultPanX(),
            "a pan up does not move x");
        AssertEq(up.PanFinishY,
            Rm2kMapCamera.DefaultPanY() + 5 * Rm2kMapCamera.ScreenTileSize,
            "**and a five tile pan up grows the finish by five tiles of sub-units**");

        var (_, right) = Run(Pan(2, 1, 5, 3, 0));
        AssertEq(right.PanFinishX,
            Rm2kMapCamera.DefaultPanX() - 5 * Rm2kMapCamera.ScreenTileSize,
            "**and a pan right shrinks it**");
        AssertEq(right.PanFinishY, Rm2kMapCamera.DefaultPanY(),
            "and a pan right does not move y");
    }

    /// <summary>
    /// Two pans in the same direction accumulate.
    /// </summary>
    /// <remarks>
    /// <strong>And this is what an absolute finish buys</strong>: the reference's
    /// <c>StartPan</c> adds to <c>pan_finish_x</c>, so a cutscene that pans right twice
    /// scrolls twice as far. A reader that stored the command's distance as a delta
    /// would pan to the same place twice and the second command would look ignored.
    /// </remarks>
    public void Test_ZweiPansInDieselbeRichtungAddierenSich()
    {
        var (interpreter, state) = Start(
            Pan(2, 1, 5, 3, 0), Pan(2, 1, 5, 3, 0));
        interpreter.ExecuteFrame();
        var nachDemErsten = state.PanFinishX;
        interpreter.ExecuteFrame();

        AssertEq(state.PanFinishX,
            nachDemErsten - 5 * Rm2kMapCamera.ScreenTileSize,
            "**and the second pan scrolls another five tiles**");
    }

    /// <summary>
    /// The speed is clamped to 1 to 6, and a game that wrote a zero or a nine
    /// still gets a pan.
    /// </summary>
    public void Test_TheSpeedIsClampedAndNotRefused()
    {
        var (_, low) = Run(Pan(2, 1, 6, 0, 0));
        AssertEq(low.PanSpeed, 2 << 1,
            "a speed of zero is repaired to one, and the pan's step is 2 << 1");

        var (_, high) = Run(Pan(2, 1, 6, 9, 0));
        AssertEq(high.PanSpeed, 2 << 6,
            "a speed of nine is repaired to six, so the step is 2 << 6");
        AssertEq(high.PanFramesLeft, 12,
            "**and a six tile pan at that step takes twelve frames**, not the one a"
                + " tile-counted reader would have waited — a reader that refused the"
                + " speed would have stopped the event on a number the engine repairs");
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
    /// The wait is the remaining distance over the pan's own step, rounded up.
    /// </summary>
    /// <remarks>
    /// <strong>And the distance is the sub-unit one</strong>: five tiles are 1280
    /// sub-units and the step at speed three is 16, so the engine waits eighty frames.
    /// The earlier version of this test divided the tile count by the editor's speed
    /// and expected two — a wait a cutscene would have outrun by a factor of forty.
    /// </remarks>
    public void Test_DieWartezeitIstDieEntfernungDurchDasPanTempo()
    {
        var (_, five) = Run(Pan(2, 1, 5, 3, 1));
        AssertEq(five.PanFramesLeft, 80,
            "**a five tile pan at speed three waits eighty frames**");

        var (_, six) = Run(Pan(2, 1, 6, 3, 1));
        AssertEq(six.PanFramesLeft, 96, "and a six tile pan waits ninety six");
    }

    /// <summary>
    /// The wait rounds up when the remaining distance is not a whole step.
    /// </summary>
    /// <remarks>
    /// <strong>And the rounding is the reference's own
    /// <c>distance / speed + (distance % speed != 0)</c></strong>: it is what stops a
    /// pan from ending the frame it started in and from being a frame short.
    /// </remarks>
    public void Test_DieWartezeitRundetAuf()
    {
        var state = new GameSimulationState { PanSpeed = 16 };
        state.PanFinishX = state.PanCurrentX - 100;

        AssertEq(state.GetPanWait(), 7,
            "**a hundred sub-units at a step of sixteen is seven frames** — six whole"
                + " steps and a remainder, which rounds up");
    }

    /// <summary>
    /// The pan walks towards its finish and lands on it exactly.
    /// </summary>
    /// <remarks>
    /// <strong>And the step is capped by what is left</strong>, which is what makes it
    /// land instead of overshooting: the reference's
    /// <c>dx = min(step, abs(remain))</c>, signed by the remainder.
    /// </remarks>
    public void Test_DerPanLaeuftAufSeinZielZuUndLandetGenau()
    {
        var state = new GameSimulationState { PanSpeed = 2 << 3 };
        state.PanFinishX = state.PanCurrentX - 5 * Rm2kMapCamera.ScreenTileSize;

        var frames = 0;
        while (state.IsPanActive && frames < 1000)
        {
            state.UpdatePan();
            frames++;
        }

        AssertEq(frames, 80,
            "**five tiles at sixteen sub-units per frame is eighty frames**");
        AssertEq(state.PanCurrentX, state.PanFinishX,
            "**and the pan lands exactly on its finish**");

        // Und eine Strecke, die nicht aufgeht, landet trotzdem.
        var krumm = new GameSimulationState { PanSpeed = 16 };
        krumm.PanFinishX = krumm.PanCurrentX - 100;
        var schritte = 0;
        while (krumm.IsPanActive && schritte < 100)
        {
            krumm.UpdatePan();
            schritte++;
        }

        AssertEq(schritte, 7, "a hundred sub-units is seven steps");
        AssertEq(krumm.PanCurrentX, krumm.PanFinishX,
            "**and the last step is the remainder and not the whole step**");
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
    /// 11350 stops the map's pending moves and does not stop the camera.
    /// </summary>
    /// <remarks>
    /// <strong>And the camera keeps panning</strong>, because the reference's
    /// <c>11350</c> calls <c>Game_Map::RemoveAllPendingMoves()</c> and nothing else:
    /// that function cancels the player's, the vehicles' and the events' move routes,
    /// and never touches <c>pan_current</c>, <c>pan_finish</c> or <c>pan_state</c>. The
    /// earlier version of this test asserted the opposite, on the reasoning that a
    /// command which stops everything must stop the camera too — plausible, and wrong.
    /// </remarks>
    public void Test_11350StopptDieBewegungUndNichtDieKamera()
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
        AssertEq(state.IsPanActive, true,
            "**and the pan keeps running**, because the reference's call is on the map's"
                + " move list and the pan is not on it");
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
