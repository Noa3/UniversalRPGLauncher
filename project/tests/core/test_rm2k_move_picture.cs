using System;
using System.Collections.Generic;

using UniversalRPG.Rm2k;
using UniversalRPG.Rm2k.Interpreter;
using UniversalRPG.Rm2k.Presentation;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Command 11120, Move Picture — the third of the picture family, and the one
/// that was named in the constant list and had no case.
/// </summary>
/// <remarks>
/// <para>
/// <c>ShowPicture</c> (11110) and <c>ErasePicture</c> (11130) both ran. This
/// one sat in the interpreter's constant list with a summary and no dispatch
/// arm — <strong>so a game that slid a title card across the screen fell into
/// the default arm and was reported as an unsupported command.</strong>
/// </para>
/// <para>
/// <strong>A reader that looks at the constant list will not find a gap.</strong>
/// The constant is there, the summary is there, and the file compiles. Only the
/// dispatch is short, and the dispatch is what a command has to reach.
/// </para>
/// </remarks>
public partial class TestRm2kMovePicture : TestBase
{
    /// <summary>
    /// A show command with the fourteen parameters EasyRPG's <c>CmdSetup</c>
    /// requires, so a move has something to move.
    /// </summary>
    private static Rm2kMap.EventCommand Show(int pId, int pX, int pY)
    {
        // **The name is the command's text and not a parameter.** The
        // interpreter reads `pCmd.Text` for the file and the parameters for
        // everything else, so a fixture that put a name index in parameters[0]
        // would show a picture with no file and every move would then have
        // nothing to move.
        //
        // **The magnification is 100, because a command that shows a picture at
        // zero is refused** and the reference's own bound is magnify 0 to 2000
        // with 0 refused as no picture at all.
        return new Rm2kMap.EventCommand
        {
            Code = EventInterpreter.ShowPicture,
            Text = "Picture",
            Parameters =
            [
                pId,     // 0: the picture id
                0,       // 1: the position mode
                pX,      // 2
                pY,      // 3
                0,       // 4: not fixed to the map
                100,     // 5: the magnification
                0,       // 6: the top transparency
                0,       // 7: no transparent colour
                255, 255, 255,  // 8, 9, 10: the colour
                100,     // 11: the saturation
                0,       // 12: no effect
                100,     // 13: the effect power
                0,       // 14: the bottom transparency
            ],
        };
    }

    /// <summary>
    /// A move command: id, mode, X, Y, frames — the first five of the eight
    /// <c>CmdSetup</c> demands.
    /// </summary>
    private static Rm2kMap.EventCommand Move(
        int pId, int pX, int pY, int pFrames, int pPad = 0)
    {
        var parameters = new List<int> { pId, 0, pX, pY, pFrames };
        while (parameters.Count < 8)
        {
            parameters.Add(pPad);
        }
        return new Rm2kMap.EventCommand
        {
            Code = EventInterpreter.MovePicture,
            Text = "",
            Parameters = parameters,
        };
    }

    private static (EventInterpreter Interpreter, PresentationState Presentation,
        GameSimulationState State) Run(params Rm2kMap.EventCommand[] pCommands)
    {
        var state = new GameSimulationState { MapId = 1 };
        var presentation = new PresentationState();
        var interpreter = new EventInterpreter(state, 1, pCommands, presentation);
        return (interpreter, presentation, state);
    }

    // ---- Der Befehl erreicht den Zustand

    /// <summary>
    /// A move command is a move and not an unsupported command.
    /// </summary>
    /// <remarks>
    /// <strong>This is the test the constant list could not write.</strong> The
    /// constant exists, the summary exists, the build is clean — and the
    /// command fell into the default arm anyway. A test that only checked "is
    /// the constant defined" would have passed on the broken dispatch for as
    /// long as anyone wrote it.
    /// </remarks>
    public void Test_AMoveCommandIsAMoveAndNotAnUnsupportedOne()
    {
        var (interpreter, presentation, state) = Run(
            Show(1, 0, 0),
            Move(1, 100, 50, 10));

        interpreter.ExecuteFrame();
        interpreter.ExecuteFrame();
        presentation.Tick(5);

        AssertEq(
            presentation.IsPictureMoving(1), true,
            "**and the picture is moving**, which is what an unsupported command"
            + $" would not have done; it is {presentation.IsPictureMoving(1)}");
        // **One diagnostic and not zero, and it is the show's own.** The show
        // command records that it placed the picture, and a test that demanded
        // silence would be demanding a command that says nothing happened —
        // which is the opposite of what a reader wants from a trace.
        AssertEq(
            state.Diagnostics.Count, 1,
            "**and the only thing reported is the show placing the picture**,"
            + " because an unsupported command would have added a second line and"
            + $" said so; the diagnostics are: {string.Join(" | ", state.Diagnostics)}");
        var unsupported = false;
        foreach (var diagnostic in state.Diagnostics)
        {
            if (diagnostic.Contains("Unsupported", StringComparison.OrdinalIgnoreCase))
            {
                unsupported = true;
            }
        }
        AssertTrue(
            !unsupported,
            "**and nothing was called unsupported**, which is what the missing"
            + " dispatch arm produced; the diagnostics are: "
            + string.Join(" | ", state.Diagnostics));
    }

    /// <summary>
    /// The picture travels, and it arrives exactly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Half way at half the time.</strong> A move from 0 to 100 over
    /// ten frames is at 50 on the fifth tick, and a reader that stepped the
    /// picture once at the start and left it there would also read 50 on the
    /// fifth frame — which is why the tenth frame is the one that matters.
    /// </para>
    /// <para>
    /// <strong>It arrives on the last frame and not one before.</strong> The
    /// step is the frames already spent over the frames in total, so at the
    /// last frame the fraction is one and the picture is exactly on its target.
    /// A reader that rounded would have stopped a pixel short and jumped.
    /// </para>
    /// </remarks>
    public void Test_ThePictureTravelsAndArrivesExactly()
    {
        var (interpreter, presentation, _) = Run(
            Show(1, 0, 0),
            Move(1, 100, 50, 10));
        interpreter.ExecuteFrame();
        interpreter.ExecuteFrame();

        presentation.Tick(4);
        AssertEq(
            presentation.Pictures[1].X, 40,
            "**and after four of ten frames it is at a tenth of the way**,"
            + " because the position is interpolated and not set once;"
            + $" it is at X {presentation.Pictures[1].X}");
        AssertEq(
            presentation.Pictures[1].Y, 20,
            "**and vertically too**, on the same fraction;"
            + $" it is at Y {presentation.Pictures[1].Y}");

        presentation.Tick(6);
        AssertEq(
            presentation.Pictures[1].X, 100,
            "**and on the last frame it is exactly on its target**, because the"
            + $" step is the spent frames over the total; it is at X {presentation.Pictures[1].X}");
        AssertEq(
            presentation.Pictures[1].Y, 50,
            "**and vertically as well**;"
            + $" it is at Y {presentation.Pictures[1].Y}");
        AssertEq(
            presentation.IsPictureMoving(1), false,
            "**and the movement is over**, and the entry is gone rather than"
            + $" kept; it is {presentation.IsPictureMoving(1)}");
    }

    /// <summary>
    /// Zero frames is a placement, and not a refusal.
    /// </summary>
    /// <remarks>
    /// <strong>An editor field the author never touched reads as zero.</strong>
    /// The reference sets the position and returns, and a reader that refused
    /// would stop the event — so a game whose title card is placed by a
    /// zero-frame move would lose the card instead of having it appear.
    /// </remarks>
    public void Test_ZeroFramesIsAPlacementAndNotARefusal()
    {
        var (interpreter, presentation, _) = Run(
            Show(1, 0, 0),
            Move(1, 200, 100, 0));
        interpreter.ExecuteFrame();
        interpreter.ExecuteFrame();

        AssertEq(
            presentation.Pictures[1].X, 200,
            "**and the picture is where it was told to go**, immediately and"
            + $" without a frame; it is at X {presentation.Pictures[1].X}");
        AssertEq(
            presentation.IsPictureMoving(1), false,
            "**and there is nothing left to move**, because a move with no"
            + $" frames has already happened; it is {presentation.IsPictureMoving(1)}");
    }

    // ---- Was der Befehl nicht kann

    /// <summary>
    /// Moving a picture that is not on the screen is refused and named.
    /// </summary>
    /// <remarks>
    /// <strong>Refused and not silently created.</strong> A game that moves an id
    /// it never showed has a file that does not mean what it says, and a reader
    /// that created a picture there would put an image on the screen that no
    /// command asked for.
    /// </remarks>
    public void Test_MovingAPictureThatIsNotThereIsRefusedAndNamed()
    {
        var (interpreter, presentation, state) = Run(Move(7, 100, 100, 10));
        interpreter.ExecuteFrame();

        AssertEq(
            presentation.Pictures.ContainsKey(7), false,
            "**and no picture was invented**, because nothing ever showed one;"
            + $" it contains {presentation.Pictures.Count} pictures");
        var genannt = false;
        foreach (var diagnostic in state.Diagnostics)
        {
            if (diagnostic.Contains("7"))
            {
                genannt = true;
            }
        }
        AssertTrue(
            genannt,
            "**and the diagnostic names the picture**, because a person fixing the"
            + $" file needs to know which one; they are: {string.Join(" | ", state.Diagnostics)}");
    }

    /// <summary>
    /// A move with fewer than eight parameters is a truncated file.
    /// </summary>
    /// <remarks>
    /// <strong>Eight, from the reference's own <c>CmdSetup</c>.</strong> A first
    /// draft read four — the id, the mode, X and Y — and a game that left the
    /// frame field empty by using the short form would have had its move
    /// rejected as a truncated file, which no RM2K/2003 game writes.
    /// </remarks>
    public void Test_AShortMoveIsATruncatedFile()
    {
        var (interpreter, presentation, state) = Run(
            Show(1, 0, 0),
            new Rm2kMap.EventCommand
            {
                Code = EventInterpreter.MovePicture,
                Text = "",
                Parameters = [1, 0, 100, 100],
            });
        interpreter.ExecuteFrame();
        interpreter.ExecuteFrame();

        AssertEq(
            presentation.IsPictureMoving(1), false,
            "**and the picture did not move**, because a command of four"
            + $" parameters is a truncated file; it is {presentation.IsPictureMoving(1)}");
        AssertEq(
            state.Diagnostics.Count > 0, true,
            "**and the file was named as malformed**, which is the diagnostic a"
            + " person needs; the diagnostics are: "
            + string.Join(" | ", state.Diagnostics));
    }

    /// <summary>
    /// A second move starts from where the picture is, and ends at its own
    /// target.
    /// </summary>
    /// <remarks>
    /// <strong>One movement per picture, and the second replaces the
    /// first.</strong> The reference holds a single move for a picture id, so a
    /// game that moves a card and then moves it again — as a later event does —
    /// starts the second movement from where the picture actually is. A reader
    /// that queued them would have the card arrive at the first target and then
    /// set off for the second, which is what a command written twice looks
    /// like.
    /// </remarks>
    public void Test_ANewMoveStartsFromWhereThePictureIsNow()
    {
        // **Two events, and that is how a game writes it.** The card has
        // arrived halfway and a later event moves it somewhere else; one
        // program with two moves would have the second replace the first
        // before a single frame had passed.
        var state = new GameSimulationState { MapId = 1 };
        var live = new PresentationState();
        var erster = new EventInterpreter(
            state, 1, [Show(1, 0, 0), Move(1, 100, 0, 10)], live);
        erster.ExecuteFrame();
        erster.ExecuteFrame();

        live.Tick(5);
        AssertEq(
            live.Pictures[1].X, 50,
            "**and after the first move's five frames it is at fifty**;"
            + $" it is at X {live.Pictures[1].X}");

        var zweiter = new EventInterpreter(state, 2, [Move(1, 20, 0, 10)], live);
        zweiter.ExecuteFrame();

        // **Half of the second movement, which is halfway between where the
        // picture was and where the second move is going.** The picture was at
        // 50 and the second target is 20, so the halfway point is 35 — and a
        // reader that kept the first movement's target would be heading the
        // other way and would read 75 here.
        live.Tick(5);
        AssertEq(
            live.Pictures[1].X, 35,
            "**and after the second move's five frames it is halfway between 50"
            + $" and 20**, because it started from where the picture was; it is at X {live.Pictures[1].X}");

        live.Tick(5);
        AssertEq(
            live.Pictures[1].X, 20,
            "**and on its last frame it is on the second target**, and not on"
            + $" the first one's hundred; it is at X {live.Pictures[1].X}");
    }

    /// <summary>
    /// A new game leaves no movement behind.
    /// </summary>
    /// <remarks>
    /// <strong>The movement goes with the picture.</strong> An entry left over
    /// would have the first tick of the new game walk a picture towards a
    /// position for an image that is not there, and nothing else removes it —
    /// so it would sit in the dictionary for ever.
    /// </remarks>
    public void Test_ANewGameLeavesNoMovementBehind()
    {
        // **A state that has a movement and is then reset.**
        var state = new GameSimulationState { MapId = 1 };
        var live = new PresentationState();
        var runner = new EventInterpreter(
            state, 1, [Show(1, 0, 0), Move(1, 100, 0, 10)], live);
        runner.ExecuteFrame();
        runner.ExecuteFrame();

        live.Reset();

        AssertEq(
            live.IsPictureMoving(1), false,
            "**and the movement is gone with the picture**, because a new game"
            + $" must not walk a picture that is not there; it is {live.IsPictureMoving(1)}");
    }
}
