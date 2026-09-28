using System;
using System.Collections.Generic;

using UniversalRPG.Rm2k;
using UniversalRPG.Rm2k.Interpreter;
using UniversalRPG.Rm2k.Presentation;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Tests for 11320 Flash Sprite — from liblcf's <c>Code::FlashSprite</c> and
/// EasyRPG's <c>Game_Interpreter_Map::CommandFlashSprite</c>.
/// </summary>
/// <remarks>
/// <para>
/// The command is width 7, and the seventh parameter is a mode byte that only
/// the Maniac patch reads. <strong>Without that patch the reference's
/// <c>ValueOrVariableBitfield</c> returns <c>parameters[val_idx]</c> and
/// nothing else</strong> — so every 2K game's channels are plain values.
/// </para>
/// </remarks>
public partial class TestRm2kFlashSprite : TestBase
{
    /// <summary>
    /// What a flash call asked for, as the test saw it.
    /// </summary>
    private sealed class Flash
    {
        public int EventId;
        public int Red;
        public int Green;
        public int Blue;
        public int Strength;
        public int Frames;
        public bool Wait;
    }

    private static Rm2kMap.EventCommand Cmd(int pCode, params int[] pParameters)
    {
        return new Rm2kMap.EventCommand
        {
            Code = pCode,
            Text = "",
            Parameters = new List<int>(pParameters),
        };
    }

    /// <summary>
    /// A seven-wide flash: id, red, green, blue, strength, tenths, wait.
    /// </summary>
    private static Rm2kMap.EventCommand FlashCmd(
        int pEventId = 3,
        int pRed = 31,
        int pGreen = 0,
        int pBlue = 16,
        int pStrength = 20,
        int pTenths = 10,
        int pWait = 1)
    {
        return Cmd(
            EventInterpreter.FlashSprite,
            pEventId, pRed, pGreen, pBlue, pStrength, pTenths, pWait);
    }

    private static (EventInterpreter Interpreter, List<Flash> Calls) Run(
        params Rm2kMap.EventCommand[] pCommands)
    {
        var state = new GameSimulationState { MapId = 1 };
        var calls = new List<Flash>();
        var interpreter = new EventInterpreter(
            state, 1, pCommands, new PresentationState(),
            moveRouteStarter: null,
            vehicleBoardToggle: null,
            spriteFlasher: (id, r, g, b, s, f, w) =>
            {
                calls.Add(new Flash
                {
                    EventId = id,
                    Red = r,
                    Green = g,
                    Blue = b,
                    Strength = s,
                    Frames = f,
                    Wait = w,
                });
                return true;
            });
        for (var i = 0; i < pCommands.Length; i++)
        {
            interpreter.ExecuteFrame();
        }

        return (interpreter, calls);
    }

    // ---- Der Befehl

    /// <summary>
    /// A flash reaches the character and carries the four channels.
    /// </summary>
    public void Test_AFlashReachesTheCharacterWithItsFourChannels()
    {
        var (_, calls) = Run(FlashCmd(3, 31, 0, 16, 20, 10, 1));

        AssertEq(calls.Count, 1, "**the flash reached the character**");
        AssertEq(calls[0].EventId, 3, "on the figure the command named");
        AssertEq(calls[0].Red, 31, "with the red channel the file carries");
        AssertEq(calls[0].Green, 0, "the green channel");
        AssertEq(calls[0].Blue, 16, "the blue channel");
        AssertEq(calls[0].Strength, 20, "and the strength");
    }

    /// <summary>
    /// The channels are read plainly, and the seventh parameter is not a
    /// bitfield.
    /// </summary>
    /// <remarks>
    /// <strong>Without the Maniac patch the reference's
    /// <c>ValueOrVariableBitfield</c> is <c>return com.parameters[val_idx];</c>
    /// and nothing else.</strong> **A reader that always shifted by the
    /// channel's index would have taken a red channel of 31 down to 15** for
    /// every game ever written in RPG Maker 2000.
    /// </remarks>
    public void Test_TheChannelsAreReadPlainlyAndNotShifted()
    {
        foreach (var (r, g, b) in new[]
                 {
                     (0, 0, 0),
                     (31, 0, 0),
                     (0, 31, 0),
                     (0, 0, 31),
                     (31, 31, 31),
                     (17, 23, 29),
                 })
        {
            var (_, calls) = Run(FlashCmd(1, r, g, b, 10, 10, 0));
            AssertEq(calls[0].Red, r,
                "**a red channel of " + r + " stays " + r + "** — the "
                    + "reference's helper returns the value unchanged without "
                    + "the patch, and a shift would have moved it");
            AssertEq(calls[0].Green, g, "a green channel of " + g + " stays " + g);
            AssertEq(calls[0].Blue, b, "a blue channel of " + b + " stays " + b);
        }
    }

    // ---- Die Dauer

    /// <summary>
    /// The duration is in tenths, and it arrives at the character in frames.
    /// </summary>
    public void Test_TheDurationIsInTenthsAndArrivesInFrames()
    {
        // **Die Referenzrate ist 60 Frames pro Sekunde**, also
        // `tenths * 60 / 10` = `tenths * 6` -- ein Zehntel ist sechs Frames
        // und nicht einer.
        foreach (var (zehntel, frames) in new[]
                 {
                     (1, 6),
                     (5, 30),
                     (10, 60),
                     (20, 120),
                 })
        {
            var (_, calls) = Run(FlashCmd(1, 31, 0, 0, 10, zehntel, 0));
            AssertEq(calls[0].Frames, frames,
                "**a duration of " + zehntel + " tenths is " + frames
                    + " frames** — the reference multiplies by its own frame "
                    + "rate of sixty and divides by ten, and a reader that "
                    + "passed the tenths on would have flashed a sixth as "
                    + "long");
        }
    }

    /// <summary>
    /// A duration of zero still waits one frame, and does not advance.
    /// </summary>
    /// <remarks>
    /// <strong>The reference's <c>SetupWait</c> has a separate arm for
    /// zero</strong> — <c>if (duration == 0) wait_time = 1; else wait_time =
   /// duration * DEFAULT_FPS / 10;</c> — so a game's "flash for no time"
    /// still holds its page for a frame, and a reader that passed zero to the
    /// frame budget would have clamped it to one for the wrong reason and
    /// skipped the flash's own length entirely.
    /// </remarks>
    public void Test_ADurationOfZeroStillHoldsOneFrame()
    {
        var state = new GameSimulationState { MapId = 1 };
        var calls = new List<Flash>();
        var interpreter = new EventInterpreter(
            state, 1,
            new[]
            {
                FlashCmd(1, 31, 0, 0, 10, 0, 1),
                Cmd(EventInterpreter.ChangeGold, 0, 0, 5),
            },
            new PresentationState(),
            moveRouteStarter: null,
            vehicleBoardToggle: null,
            spriteFlasher: (id, r, g, b, s, f, w) =>
            {
                calls.Add(new Flash { Frames = f });
                return true;
            });
        interpreter.ExecuteFrame();
        var nachFlash = state.Gold;

        AssertEq(calls.Count, 1, "the flash happened");
        AssertEq(calls[0].Frames, 0,
            "**the character was told zero frames** — the reference passes "
                + "`tenths * DEFAULT_FPS / 10` to the flash and only the wait "
                + "gets the zero arm");
        AssertEq(nachFlash, 0,
            "**and the page is still held** — the reference's SetupWait gives "
                + "a zero duration one frame, so the next command has not run "
                + "yet");

        // **Ein Frame mehr, und das ist der Mechanismus und nicht der
        // Befehl.** `ExecuteFrame` prueft das Wartebudget ZUERST und gibt
        // die Seite zurueck, ohne einen Befehl zu lesen -- also dekrementiert
        // der zweite Aufruf die eine Frame und der dritte fuehrt den naechsten
        // Befehl aus. Die Referenz schreibt `wait_time = 1` und arbeitet mit
        // demselben Mechanismus.
        // **Der zweite Aufruf dekrementiert das eine Frame Budget, und der
        // dritte fuehrt den naechsten Befehl aus.** Das ist der Mechanismus
        // jedes zeitgesteuerten Befehls hier: das Budget wird VOR dem
        // Rueckschritt geprueft.
        interpreter.ExecuteFrame();
        AssertEq(state.Gold, 0,
            "**the frame that drains the wait runs no command** — gold is "
                + state.Gold + " and the wait left is "
                + interpreter.WaitFramesRemaining);
        interpreter.ExecuteFrame();
        AssertEq(state.Gold, 5,
            "**and the next command runs on the frame after that** — gold is "
                + state.Gold + " and the wait left is "
                + interpreter.WaitFramesRemaining + ", with the diagnostics "
                + string.Join(" | ", state.Diagnostics));
    }

    // ---- Das Warten

    /// <summary>
    /// The wait flag holds the page, and without it the page moves on.
    /// </summary>
    public void Test_TheWaitFlagHoldsThePage()
    {
        // **Der Test zaehlt die Frames, statt alle Befehle blind zu fahren**
        // -- sonst hat der Lauf die Wartezeit schon verbraucht, bevor die
        // Frage gestellt wird.
        var state = new GameSimulationState { MapId = 1 };
        var wartend = new EventInterpreter(
            state, 1,
            new[]
            {
                FlashCmd(1, 31, 0, 0, 10, 10, 1),
                Cmd(EventInterpreter.ChangeGold, 0, 0, 7),
            },
            new PresentationState(),
            moveRouteStarter: null,
            vehicleBoardToggle: null,
            spriteFlasher: (_, _, _, _, _, _, _) => true);
        wartend.ExecuteFrame();
        AssertEq(state.Gold, 0,
            "**a flash that was asked to wait holds its page** — a duration "
                + "of ten tenths is sixty frames, and the next command has "
                + "not run on any of them");

        var state2 = new GameSimulationState { MapId = 1 };
        var laufend = new EventInterpreter(
            state2, 1,
            new[]
            {
                FlashCmd(1, 31, 0, 0, 10, 10, 0),
                Cmd(EventInterpreter.ChangeGold, 0, 0, 7),
            },
            new PresentationState(),
            moveRouteStarter: null,
            vehicleBoardToggle: null,
            spriteFlasher: (_, _, _, _, _, _, _) => true);
        laufend.ExecuteFrame();
        laufend.ExecuteFrame();
        AssertEq(state2.Gold, 7,
            "**and without the wait flag the next command runs at once**");
    }

    // ---- Die Figur

    /// <summary>
    /// The page is held for the flash's own length, in frames.
    /// </summary>
    /// <remarks>
    /// <strong>Two mutations survived because every test asked the hook what
    /// it saw and not the page how long it waited.</strong> A rule that passed
    /// the tenths on unchanged, and one that dropped the zero arm, both left
    /// the hook's argument correct in the second case and the page's timing
    /// wrong. <strong>The hook is what the figure sees; the page is what the
    /// game sees</strong>, and a game can only tell the difference by waiting.
    /// </remarks>
    public void Test_ThePageIsHeldForTheFlashsOwnLengthInFrames()
    {
        foreach (var (zehntel, frames) in new[]
                 {
                     (1, 6),
                     (5, 30),
                     (10, 60),
                 })
        {
            var state = new GameSimulationState { MapId = 1 };
            var interpreter = new EventInterpreter(
                state, 1,
                new[]
                {
                    FlashCmd(1, 31, 0, 0, 10, zehntel, 1),
                    Cmd(EventInterpreter.ChangeGold, 0, 0, 3),
                },
                new PresentationState(),
                moveRouteStarter: null,
                vehicleBoardToggle: null,
                spriteFlasher: (_, _, _, _, _, _, _) => true);
            interpreter.ExecuteFrame();

            // **Und jetzt zaehlen wir, wie viele Frames die Seite wirklich
            // haelt -- nicht was der Hook bekommen hat.**
            var gehalten = 0;
            while (state.Gold == 0 && gehalten < frames + 5)
            {
                interpreter.ExecuteFrame();
                gehalten += 1;
            }

            // **Ein Frame mehr, und das ist der Mechanismus.** Der Frame,
            // der den Flash ausloest, gehoert nicht selbst zur Wartezeit --
            // `ExecuteFrame` prueft das Budget ZUERST. Das ist derselbe
            // Versatz wie bei der Kampfanimation.
            AssertEq(gehalten, frames + 1,
                "**a flash of " + zehntel + " tenths held the page for "
                    + frames + " frames** — it released the page after "
                    + gehalten + " calls, and the frame that triggered the "
                    + "flash is not itself part of the wait");
        }
    }

    /// <summary>
    /// A duration of zero holds one frame and not the budget's own minimum.
    /// </summary>
    /// <remarks>
    /// <strong>The reference's <c>SetupWait</c> has a separate arm for
    /// zero</strong>, and a rule that removed it survived because the
    /// frame-budget clamp turned a zero into a one anyway — <strong>so the
    /// two paths agree on this page and the difference is only visible in
    /// the count.</strong> This asserts the count, and the count is what a
    /// game's timing feels.
    /// </remarks>
    public void Test_ADurationOfZeroHoldsExactlyOneFrame()
    {
        var state = new GameSimulationState { MapId = 1 };
        var framesAnFlash = -1;
        var interpreter = new EventInterpreter(
            state, 1,
            new[]
            {
                FlashCmd(1, 31, 0, 0, 10, 0, 1),
                Cmd(EventInterpreter.ChangeGold, 0, 0, 3),
            },
            new PresentationState(),
            moveRouteStarter: null,
            vehicleBoardToggle: null,
            spriteFlasher: (_, _, _, _, _, f, _) =>
            {
                framesAnFlash = f;
                return true;
            });
        interpreter.ExecuteFrame();

        var gehalten = 0;
        while (state.Gold == 0 && gehalten < 8)
        {
            interpreter.ExecuteFrame();
            gehalten += 1;
        }

        // **Und auch hier ein Aufruf mehr**, aus demselben Grund.
        AssertEq(gehalten, 2,
            "**a duration of zero held the page for one frame** — the "
                + "reference's SetupWait writes wait_time = 1 and not zero, "
                + "and the page was released after " + gehalten + " calls: "
                + "the frame that ran the flash, and the one that drained "
                + "the single frame of wait");

        // **Und der Arm ist trotzdem nicht toetbar, und das ist eine
        // Eigenschaft des Lesers und nicht des Tests.** `WaitForFrames`
        // klemmt auf mindestens eine Frame, also ergibt `WaitForFrames(0)`
        // und `WaitForFrames(1)` dieselbe Seite -- **die Regel, die den
        // Null-Arm entfernt, aendert hier nichts.** Der beobachtbare
        // Unterschied ist die Zahl, die der Hook bekommt, und die ist null.
        AssertEq(framesAnFlash, 0,
            "**and the character was told zero frames** — the reference "
                + "passes `tenths * DEFAULT_FPS / 10` to the flash, and the "
                + "zero arm belongs to the wait and not to the figure");
    }

    /// <summary>
    /// A figure that does not resolve is a warning, and the page still moves.    /// <summary>
    /// A figure that does not resolve is a warning, and the page still moves.
    /// </summary>
    /// <remarks>
    /// <strong>The reference's <c>GetCharacter</c> returns null, the whole
    /// flash is skipped, and the command still returns true.</strong> A reader
    /// that refused would have stopped a game's event on a name no figure
    /// carries, and one that advanced to the wait would have held the page for
    /// a flash that never happened.
    /// </remarks>
    public void Test_AFigureThatDoesNotResolveIsAWarningAndThePageMoves()
    {
        var state = new GameSimulationState { MapId = 1 };
        var interpreter = new EventInterpreter(
            state, 1,
            new[]
            {
                FlashCmd(99, 31, 0, 0, 10, 10, 1),
                Cmd(EventInterpreter.ChangeGold, 0, 0, 7),
            },
            new PresentationState(),
            moveRouteStarter: null,
            vehicleBoardToggle: null,
            spriteFlasher: (_, _, _, _, _, _, _) => false);
        interpreter.ExecuteFrame();
        var weiter = interpreter.ExecuteFrame();

        AssertEq(weiter, true,
            "**the page moved on** — the reference's command returns true "
                + "whether or not the figure resolved, and **it never set a "
                + "wait at all** for a flash that never happened");
        AssertEq(state.Gold, 7,
            "**and the next command ran at once** — gold is " + state.Gold
            + " and the wait left is " + interpreter.WaitFramesRemaining
            + ", with the diagnostics "
            + string.Join(" | ", state.Diagnostics));
        var gewarnt = false;
        foreach (var d in state.Diagnostics)
        {
            if (d.Contains("no character carries the id 99"))
            {
                gewarnt = true;
            }
        }

        AssertTrue(gewarnt, "and it warns — the diagnostics were "
            + string.Join(" | ", state.Diagnostics));
    }

    /// <summary>
    /// No hook at all is a diagnostic and not a refusal.
    /// </summary>
    public void Test_NoHookIsADiagnosticAndNotARefusal()
    {
        var state = new GameSimulationState { MapId = 1 };
        var interpreter = new EventInterpreter(
            state, 1,
            new[] { FlashCmd() }, new PresentationState());
        var weiter = interpreter.ExecuteFrame();

        AssertEq(weiter, true,
            "**an interpreter with no character hook still advances** — this "
                + "is the same distinction 11330 makes, and a refusal would "
                + "have stopped every event that flashes");
        var gesagt = false;
        foreach (var d in state.Diagnostics)
        {
            if (d.Contains("cannot reach a character"))
            {
                gesagt = true;
            }
        }

        AssertTrue(gesagt, "and it says so — the diagnostics were "
            + string.Join(" | ", state.Diagnostics));
    }

    /// <summary>
    /// A six-wide command is refused, and says so.
    /// </summary>
    public void Test_ASixWideCommandIsRefused()
    {
        // **Jede Breite unter sieben ist abgelehnt, und nicht nur die
        // sechste** -- eine Regel, die die Grenze auf vier senkte, wuerde
        // eine sechs Parameter breite Datei abnehmen, und genau das ist der
        // Fehler, den ein Spiel mit einer alten Datei trifft.
        foreach (var breite in new[] { 0, 4, 5, 6 })
        {
            var parameter = new int[breite];
            for (var i = 0; i < breite; i++)
            {
                parameter[i] = i == 0 ? 1 : 0;
            }

            var state = new GameSimulationState { MapId = 1 };
            var aufrufe = 0;
            var interpreter = new EventInterpreter(
                state, 1,
                new[] { Cmd(EventInterpreter.FlashSprite, parameter) },
                new PresentationState(),
                moveRouteStarter: null,
                vehicleBoardToggle: null,
                spriteFlasher: (_, _, _, _, _, _, _) =>
                {
                    aufrufe += 1;
                    return true;
                });
            interpreter.ExecuteFrame();

            AssertEq(aufrufe, 0,
                "**a command of " + breite + " parameters flashed nothing** — "
                    + "the reference's CmdSetup gives 11320 a width of seven, "
                    + "and a reader that took four would have flashed a game "
                    + "whose file predates the colour channels");
            var gemeldet = false;
            foreach (var d in state.Diagnostics)
            {
                if (d.Contains("Flash sprite: malformed"))
                {
                    gemeldet = true;
                }
            }

            AssertTrue(gemeldet,
                "and a width of " + breite + " says so — the diagnostics were "
                    + string.Join(" | ", state.Diagnostics));
        }
    }
}
