using System;
using System.Collections.Generic;

using UniversalRPG.Rm2k;
using UniversalRPG.Rm2k.Interpreter;
using UniversalRPG.Rm2k.Presentation;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Command 10840, Get On/Off Vehicle — the third vehicle command, and the only
/// one of the three that had no way to be reached.
/// </summary>
/// <remarks>
/// <para>
/// <c>ChangeVehicleGraphic</c> (10650) and <c>SetVehicleLocation</c> (10850)
/// were both dispatched. This one was not — <strong>and
/// <c>Rm2kVehicleBoarding</c> had fifteen boarding methods, tested, that no
/// command could reach.</strong>
/// </para>
/// <para>
/// Same island shape as the pictures in <c>PresentationState</c> and as
/// <c>Rm2kMoveRouteState</c> before it: a class that is complete, tested, and
/// unreachable. <strong>The only way to find these is to ask what the class is
/// for and compare it against what the reference's commands do</strong> — not to
/// read the constant list, where the number is already there.
/// </para>
/// </remarks>
public partial class TestRm2kGetOnOffVehicle : TestBase
{
    /// <summary>The get on/off command, which takes no parameters.</summary>
    private static Rm2kMap.EventCommand Toggle()
    {
        return new Rm2kMap.EventCommand
        {
            Code = EventInterpreter.GetOnOffVehicle,
            Text = "",
            // **No parameters at all.** The vehicle is not named here — it is
            // whatever is under the player or in front of it — so a fixture
            // that padded this list would be testing a command the format does
            // not have.
            Parameters = [],
        };
    }

    private static GameSimulationState StateWithABoat()
    {
        var state = new GameSimulationState { MapId = 1 };
        // **The constructor, and not an object initialiser** — the type, the
        // map and the position are the four things it takes, and it sets the
        // direction and the move speed itself. Type 1 is the boat, because the
        // liblcf enum is None 0, Boat 1, Ship 2, Airship 3.
        state.Vehicles.Add(new Rm2kVehicleState(Rm2kVehicle.Boat, 1, 3, 3));
        return state;
    }

    // ---- Der Befehl erreicht den Zustand

    /// <summary>
    /// A get on/off command calls the boarding hook.
    /// </summary>
    /// <remarks>
    /// <strong>This is the test the constant list could not write.</strong>
    /// The constant is there, the summary is there, the build is clean, and the
    /// fifteen boarding rules are there — and the command fell into the default
    /// arm. A test that only checked "is the constant defined" would have
    /// passed on the broken dispatch for as long as anyone wrote it.
    /// </remarks>
    public void Test_AGetOnOffCommandCallsTheBoardingHook()
    {
        var gerufen = 0;
        var (interpreter, _, _) = Run(
            StateWithABoat(),
            () =>
            {
                gerufen += 1;
                return true;
            },
            Toggle());

        interpreter.ExecuteFrame();

        AssertEq(
            gerufen, 1,
            "**and the boarding rules were asked**, which is what the command is"
            + $" for; they were asked {gerufen} times");
    }

    /// <summary>
    /// The hook is asked once per command, and not once per frame.
    /// </summary>
    /// <remarks>
    /// <strong>The command advances.</strong> Unlike the menu openers, which hold
    /// the page and re-run, this one takes no parameters and holds nothing — the
    /// reference's does not wait for the boarding animation either, and a
    /// command that re-asked every frame would board on frame one and then try
    /// again forever.
    /// </remarks>
    public void Test_TheHookIsAskedOnceAndThePageMovesOn()
    {
        var gerufen = 0;
        var (interpreter, _, _) = Run(
            StateWithABoat(),
            () =>
            {
                gerufen += 1;
                return true;
            },
            Toggle(),
            Toggle());

        interpreter.ExecuteFrame();

        AssertEq(
            gerufen, 1,
            "**and only once in the first frame**, because the page advanced"
            + $" past the first command; it was asked {gerufen} times");

        interpreter.ExecuteFrame();
        AssertEq(
            gerufen, 2,
            "**and once more for the second command**, which is the ordinary"
            + $" shape of two commands in a row; it was asked {gerufen} times");
    }

    // ---- Was der Befehl nicht kann

    /// <summary>
    /// A command with no hook is refused, and the refusal is named.
    /// </summary>
    /// <remarks>
    /// <strong>Two different silences.</strong> "There is no vehicle here" is
    /// the reference's silence and the command succeeds at nothing; "this reader
    /// cannot board" is a different thing, and a caller that gave no hook has
    /// not said the first. **A reader that treated both as success would have a
    /// game whose boarding cutscene ran and never boarded.**
    /// </remarks>
    public void Test_ACommandWithNoHookIsRefusedAndNamed()
    {
        var (interpreter, _, state) = Run(StateWithABoat(), null, Toggle());

        interpreter.ExecuteFrame();

        var genannt = false;
        foreach (var diagnostic in state.Diagnostics)
        {
            if (diagnostic.Contains("vehicle", StringComparison.OrdinalIgnoreCase)
                || diagnostic.Contains("Vehicle", StringComparison.Ordinal))
            {
                genannt = true;
            }
        }
        AssertTrue(
            genannt,
            "**and the diagnostic names the vehicle**, because a person fixing"
            + " the file needs to know which command; they are: "
            + string.Join(" | ", state.Diagnostics));
    }

    /// <summary>
    /// Nothing to board is a success that changed nothing, and not a failure.
    /// </summary>
    /// <remarks>
    /// <strong>The reference's silence, and it is load-bearing.</strong> A game
    /// that runs a boarding command where there is no vehicle is a game with a
    /// cutscene the author wrote for a tile and moved; the reference does
    /// nothing and carries on, and a reader that reported it as an error would
    /// have that game stop on a tile the author changed.
    /// </remarks>
    public void Test_NothingToBoardIsASuccessThatChangedNothing()
    {
        var (interpreter, _, state) = Run(
            StateWithABoat(), () => false, Toggle());

        var result = interpreter.ExecuteFrame();

        AssertEq(
            result, true,
            "**and the frame succeeded**, because there was nothing to do and"
            + $" not something to refuse; it returned {result}");
        AssertEq(
            state.Boarding?.IsBoarding ?? false, false,
            "**and the party is still on foot**, which is what nothing happening"
            + $" means; it is {state.Boarding?.IsBoarding}");
    }

    /// <summary>
    /// An interpreter over a state, with the boarding hook the test wants.
    /// </summary>
    /// <remarks>
    /// <strong>The hook is a constructor argument and not a field the caller
    /// fills in later</strong>, for the same reason the route starter is: a test
    /// has to be able to see what the interpreter was given, and a null hook is
    /// itself a case worth testing.
    /// </remarks>
    private static (EventInterpreter Interpreter, PresentationState Presentation,
        GameSimulationState State) Run(
        GameSimulationState pState,
        Func<bool>? pBoardToggle,
        params Rm2kMap.EventCommand[] pCommands)
    {
        var presentation = new PresentationState();
        var interpreter = new EventInterpreter(
            pState, 1, pCommands, presentation, vehicleBoardToggle: pBoardToggle);
        return (interpreter, presentation, pState);
    }
}
