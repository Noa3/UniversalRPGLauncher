using System;
using System.Collections.Generic;

using UniversalRPG.Rm2k;
using UniversalRPG.Rm2k.Interpreter;
using UniversalRPG.Rm2k.Presentation;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Tests for 11560 Play Movie — from liblcf's <c>Code::PlayMovie</c> and
/// EasyRPG's <c>Game_Interpreter_Map::CommandPlayMovie</c> and
/// <c>Game_Screen::PlayMovie</c>.
/// </summary>
public partial class TestRm2kPlayMovie : TestBase
{
    private static Rm2kMap.EventCommand Movie(
        int pMode,
        int pX,
        int pY,
        int pResX,
        int pResY,
        string pFile = "Opening")
    {
        return new Rm2kMap.EventCommand
        {
            Code = EventInterpreter.PlayMovie,
            Text = pFile,
            Parameters = new List<int> { pMode, pX, pY, pResX, pResY },
        };
    }

    private static GameSimulationState Run(Rm2kMap.EventCommand pCmd)
    {
        var state = new GameSimulationState { MapId = 1 };
        var interpreter = new EventInterpreter(
            state, 1, new[] { pCmd }, new PresentationState());
        interpreter.ExecuteFrame();
        return state;
    }

    // ---- The five fields

    /// <summary>
    /// The name is a string, and the first parameter is not it.
    /// </summary>
    /// <remarks>
    /// <strong>The reference reads <c>ToString(com.string)</c> for the file
    /// and <c>parameters[0..4]</c> for everything else.</strong> <strong>A
    /// reader that looked for the name in the first parameter would have read
    /// the mode byte as a file name</strong> — and that byte is 0 or 1, so
    /// every movie would have been one file called "0".
    /// </remarks>
    public void Test_TheNameIsAStringAndNotAParameter()
    {
        var state = Run(Movie(0, 20, 30, 320, 240, "Opening"));

        AssertEq(state.MovieFileName, "Opening",
            "**the file name came from the command's text** — the reference's "
                + "own ToString(com.string), and a reader that read parameter "
                + "zero would have looked for a file called \"0\"");
    }

    /// <summary>
    /// The first parameter is the mode for both positions.
    /// </summary>
    /// <remarks>
    /// <strong>The reference writes
    /// <c>ValueOrVariable(com.parameters[0], com.parameters[1])</c> for x and
    /// the same mode with <c>parameters[2]</c> for y</strong> — the same shape
    /// <c>10910</c> has, and a reader that gave the row its own mode would
    /// have read a game's row from a constant while its column came from a
    /// variable.
    /// </remarks>
    public void Test_TheFirstParameterIsTheModeForBothPositions()
    {
        var state = new GameSimulationState { MapId = 1 };
        state.Variables.Add(0);
        state.Variables.Add(0);
        state.Variables.Add(0);
        // **Variablen 2 und 3 mit 40 und 60.**
        state.Variables[1] = 40;
        state.Variables[2] = 60;
        var interpreter = new EventInterpreter(
            state, 1, new[] { Movie(1, 2, 3, 320, 240) },
            new PresentationState());
        interpreter.ExecuteFrame();

        AssertEq(state.MoviePosX, 40,
            "**the left edge came out of variable 2** — the mode says so");
        AssertEq(state.MoviePosY, 60,
            "**and the top edge out of variable 3** — the same mode byte "
                + "governs both, and a reader that gave the row its own mode "
                + "would have read it as a constant");
    }

    /// <summary>
    /// The fourth and fifth are the width and the height, read plainly.
    /// </summary>
    public void Test_TheFourthAndFifthAreTheSize()
    {
        var state = Run(Movie(0, 20, 30, 320, 240));

        AssertEq(state.MovieResX, 320, "the width is the fourth parameter");
        AssertEq(state.MovieResY, 240,
            "**and the height the fifth** — the reference reads both directly, "
                + "with no mode byte, because a size is never a variable");
    }

    // ---- The request and what is not there

    /// <summary>
    /// The command records the request, and the page still moves.
    /// </summary>
    /// <remarks>
    /// <strong>The reference stores the request and returns true, which
    /// advances the page</strong> — <strong>and a reader that refused the
    /// command would have stalled a game's event</strong> on a cutscene it
    /// cannot show.
    /// </remarks>
    public void Test_TheCommandRecordsTheRequestAndThePageMoves()
    {
        var state = new GameSimulationState { MapId = 1 };
        var interpreter = new EventInterpreter(
            state, 1, new[] { Movie(0, 20, 30, 320, 240) },
            new PresentationState());
        var weiter = interpreter.ExecuteFrame();

        AssertEq(state.IsMoviePending, true, "the request is pending");
        AssertEq(weiter, true,
            "**and the page still moves** — the reference's return true, and a "
                + "reader that held the event would have frozen a game on a "
                + "cutscene it cannot show");
    }

    /// <summary>
    /// Nothing claims that the movie is playing.
    /// </summary>
    /// <remarks>
    /// <strong>The reference's own body says it plainly:</strong>
    /// <c>Output::Warning("Couldn't play movie: {}. Movie playback is not
    /// implemented (yet).", filename)</c>. <strong>A reader that reported
    /// success would have claimed a capability the reference does not have</strong>
    /// — and a game's own error handling would have taken a branch that never
    /// fires in the original.
    /// </remarks>
    public void Test_NothingClaimsThatTheMovieIsPlaying()
    {
        var state = Run(Movie(0, 20, 30, 320, 240, "Opening"));

        var gesagt = false;
        foreach (var d in state.Diagnostics)
        {
            if (d.Contains("nothing is playing it")
                && d.Contains("not implemented"))
            {
                gesagt = true;
            }
        }

        AssertTrue(gesagt,
            "**the diagnostic says the movie is not being played** — the "
                + "reference's own warning, and a reader that reported success "
                + "would have claimed a capability it does not have. The "
                + "diagnostics were: " + string.Join(" | ", state.Diagnostics));
    }

    /// <summary>
    /// There is no timer, because the reference has none.
    /// </summary>
    /// <remarks>
    /// <strong>Five fields and no frame counter, no duration and no
    /// stop.</strong> <c>Game_Screen::PlayMovie</c> is five assignments and
    /// nothing else. <strong>A reader that modelled a movie as something that
    /// plays and then ends would have had to invent the end</strong>, and an
    /// invented end is a number a game can be wrong about — a cutscene would
    /// advance before its last frame on a machine slower than the author's.
    /// </remarks>
    public void Test_ThereIsNoTimerBecauseTheReferenceHasNone()
    {
        var state = Run(Movie(0, 20, 30, 320, 240));

        // **Zehn Bilder lang laeuft nichts und aendert sich nichts** -- die
        // Felder bleiben, wie der Befehl sie geschrieben hat, und es gibt
        // kein Feld, das auf ein Ende zaehlt.
        for (var frame = 0; frame < 10; frame++)
        {
            state.FrameCount += 1;
        }

        AssertEq(state.MovieFileName, "Opening",
            "**the request is still the request ten frames later** — the "
                + "reference stores five numbers and never looks at them "
                + "again, and a reader that counted down would have invented "
                + "an end the reference does not have");
        AssertEq(state.IsMoviePending, true,
            "**and nothing cleared it** — the state is a description, and a "
                + "test that can end a movie on its own has invented a "
                + "duration");
    }

    /// <summary>
    /// A position of zero is a position, and not "unset".
    /// </summary>
    /// <remarks>
    /// <strong>The reference stores whatever it is given without a range
    /// check</strong>, so a movie asked for at 0, 0 is asked for at the
    /// screen's corner — <strong>and a reader that used zero as "unset" would
    /// have drawn a game's cutscene somewhere the file did not say.</strong>
    /// </remarks>
    public void Test_APositionOfZeroIsAPosition()
    {
        var state = Run(Movie(0, 0, 0, 0, 0, "Corner"));

        AssertEq(state.IsMoviePending, true,
            "**a movie at 0, 0 with a size of 0 by 0 is still a request** — the "
                + "reference's own body has no range check, and a reader that "
                + "refused it would have refused a game that means the corner");
        AssertEq(state.MoviePosX, 0, "the left edge is 0");
        AssertEq(state.MoviePosY, 0, "**and the top edge too**");
        AssertEq(state.MovieResX, 0, "**and the width is 0, not unset**");
    }

    /// <summary>
    /// A four-wide command is refused, and says so.
    /// </summary>
    /// <remarks>
    /// <strong>The reference reads <c>parameters[3]</c> and
    /// <c>parameters[4]</c> unguarded, so the file is written for five.</strong>
    /// A reader that accepted four would have read past the end of the list on
    /// a game whose file predates the size.
    /// </remarks>
    public void Test_AShortCommandIsRefused()
    {
        foreach (var breite in new[] { 0, 3, 4 })
        {
            var state = new GameSimulationState { MapId = 1 };
            var parameter = new int[breite];
            var interpreter = new EventInterpreter(
                state, 1,
                new[]
                {
                    new Rm2kMap.EventCommand
                    {
                        Code = EventInterpreter.PlayMovie,
                        Text = "Opening",
                        Parameters = new List<int>(parameter),
                    },
                },
                new PresentationState());
            interpreter.ExecuteFrame();

            AssertEq(state.IsMoviePending, false,
                "**a command of " + breite + " parameters asks for no movie** — "
                    + "the reference's CmdSetup gives 11560 a width of five, "
                    + "and a reader that took four would have read past the end "
                    + "of the parameter list");
            var gemeldet = false;
            foreach (var d in state.Diagnostics)
            {
                if (d.Contains("Play movie: malformed"))
                {
                    gemeldet = true;
                }
            }

            AssertTrue(gemeldet,
                "and a width of " + breite + " says so — the diagnostics were "
                    + string.Join(" | ", state.Diagnostics));
        }
    }

    /// <summary>
    /// A second movie replaces the first, and the page does not stop for it.
    /// </summary>
    /// <remarks>
    /// <strong>The five fields are written, not queued.</strong> The reference
    /// has no list, and <strong>a reader that queued would have played a
    /// game's second cutscene after its first</strong> — which is a
    /// different sequence than the one the event describes.
    /// </remarks>
    public void Test_ASecondMovieReplacesTheFirst()
    {
        var state = new GameSimulationState { MapId = 1 };
        var interpreter = new EventInterpreter(
            state, 1,
            new[]
            {
                Movie(0, 1, 2, 320, 240, "First"),
                Movie(0, 5, 6, 640, 480, "Second"),
            },
            new PresentationState());
        interpreter.ExecuteFrame();
        interpreter.ExecuteFrame();

        AssertEq(state.MovieFileName, "Second",
            "**the second request is the one that stands** — the reference's "
                + "five assignments, and a reader that queued would have "
                + "played a game's later cutscene after its earlier one");
        AssertEq(state.MovieResX, 640, "and the size came with it");
    }

    /// <summary>
    /// The row is read with the same mode byte as the column, and the two
    /// answers differ.
    /// </summary>
    /// <remarks>
    /// <strong>The first version of this test put both variables on tiles
    /// that would answer the same way</strong>, so a reader that gave the row
    /// its own mode would have passed it. **Here variable 3 holds 60 and
    /// variable 2 holds 40, and the constant branch would have read 2** — a
    /// number nothing else in this file produces.
    /// </remarks>
    public void Test_TheRowIsReadWithTheSameModeAndTheTwoAnswersDiffer()
    {
        var state = new GameSimulationState { MapId = 1 };
        state.Variables.Add(0);
        state.Variables.Add(0);
        state.Variables.Add(0);
        state.Variables[1] = 40;
        state.Variables[2] = 60;
        var interpreter = new EventInterpreter(
            state, 1, new[] { Movie(1, 2, 3, 320, 240) },
            new PresentationState());
        interpreter.ExecuteFrame();

        AssertEq(state.MoviePosX, 40, "the left edge came out of variable 2");
        AssertEq(state.MoviePosY, 60,
            "**and the top edge out of variable 3, not out of the parameter** "
                + "— a reader that gave the row its own mode would have read "
                + "the constant 2 and put the movie three pixels down "
                + "instead of sixty");
    }

    /// <summary>
    /// A reset clears the request, because a new game has no cutscene.
    /// </summary>
    /// <remarks>
    /// <strong><c>Reset</c> is what <c>Rm2kEngineRuntime</c> calls before
    /// every game.</strong> A reader that left the movie fields alone would
    /// have started a second game with the first game's cutscene still marked
    /// as requested — <strong>and a title screen that checks the flag would
    /// have skipped the opening movie of the game the player just
    /// started.</strong>
    /// </remarks>
    public void Test_AResetClearsTheRequest()
    {
        var state = Run(Movie(0, 20, 30, 320, 240, "Opening"));
        AssertEq(state.IsMoviePending, true, "the request is there to start");

        state.Reset();

        AssertEq(state.IsMoviePending, false,
            "**a reset clears the request** — Reset is what the runtime calls "
                + "before every game, and a reader that left the flag alone "
                + "would have started the second game with the first game's "
                + "cutscene still marked as requested");
        AssertEq(state.MovieFileName, "",
            "**and the file name with it** — a stale name in a fresh game is "
                + "the kind of state a player sees as a cutscene that plays "
                + "when nothing asked for one");
        AssertEq(state.MovieResX, 0, "**and the size**");
    }
}
