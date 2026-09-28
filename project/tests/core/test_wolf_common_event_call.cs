using System;
using System.Collections.Generic;

using UniversalRPG.Tests.Framework;
using UniversalRPG.Wolf;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Calling a common event, and what happens when the call comes back.
/// </summary>
/// <remarks>
/// <para>
/// The binary reader decoded type 300 — a call to a common event by name —
/// including its argument block and its return value flag, and the opcode enum
/// had no value for it. So a game whose events call a common event could not
/// run the call at all, and every WOLF shop is built from common events:
/// initialise, add a product, run the shop.
/// </para>
/// <para>
/// <strong>Coming back is the whole point.</strong> The end of a common event
/// resumes the caller; only the end of the outermost program completes the VM.
/// A reader that treated both as the end would have a game's first common
/// event call stop the game dead.
/// </para>
/// </remarks>
public partial class TestWolfCommonEventCall : TestBase
{
    /// <summary>The normal band, variable zero — the addressing scheme's 2,000,000.</summary>
    private const int Normal0 = 2_000_000;

    /// <summary>A common event that sets one variable and ends.</summary>
    private static WolfEventProgram Setter(int pId, int pValue)
    {
        return new WolfEventProgram
        {
            Id = pId,
            Commands =
            [
                new WolfEventCommand
                {
                    Opcode = WolfEventOpcode.SetVariable,
                    Operand = Normal0,
                    Value = pValue,
                },
                new WolfEventCommand { Opcode = WolfEventOpcode.End },
            ],
        };
    }

    /// <summary>A program that calls the given events and then sets a variable.</summary>
    private static WolfEventProgram Caller(int pId, params int[] pCalls)
    {
        var commands = new List<WolfEventCommand>();
        foreach (var call in pCalls)
        {
            commands.Add(new WolfEventCommand
            {
                Opcode = WolfEventOpcode.CallCommonEvent,
                Operand = call,
            });
        }
        commands.Add(new WolfEventCommand
        {
            Opcode = WolfEventOpcode.SetVariable,
            Operand = Normal0,
            Value = 99,
        });
        commands.Add(new WolfEventCommand { Opcode = WolfEventOpcode.End });
        return new WolfEventProgram { Id = pId, Commands = commands };
    }

    /// <summary>A program that only calls, and does nothing after it.</summary>
    private static WolfEventProgram CallerOnly(int pId, params int[] pCalls)
    {
        var commands = new List<WolfEventCommand>();
        foreach (var call in pCalls)
        {
            commands.Add(new WolfEventCommand
            {
                Opcode = WolfEventOpcode.CallCommonEvent,
                Operand = call,
            });
        }
        commands.Add(new WolfEventCommand { Opcode = WolfEventOpcode.End });
        return new WolfEventProgram { Id = pId, Commands = commands };
    }

    /// <summary>A board with open ground and a figure on it, ready for a route.</summary>
    private static WolfEventVm VmWithFigure()
    {
        var vm = new WolfEventVm();
        vm.Board.LoadMap(1, new WolfPassabilityGrid(20, 20));
        // **Speed 6.** A tile takes sixteen frames at speed 1, and a test that
        // sits on that boundary fails when something unrelated changes.
        vm.Board.Add(new WolfCharacter { Id = 1, X = 0, Y = 0, MoveSpeed = 6 });
        return vm;
    }

    // ---- Der Aufruf laeuft

    /// <summary>
    /// A call runs the event it names, and the game finishes.
    /// </summary>
    /// <remarks>
    /// <strong>The value is the common event's, and not the caller's.</strong> The
    /// caller here does nothing after the call, so a value of 42 can only have
    /// come from the event — and a reader that ran the call and then fell
    /// through to the caller's own line would have left the caller's value.
    /// </remarks>
    public void Test_ACallRunsAndTheCallerCarriesOn()
    {
        var vm = new WolfEventVm { CommonEvents = [Setter(7, 42)] };
        vm.Start(CallerOnly(1, 7));

        // **One tick is enough, because a tick runs up to 256 commands and a
        // call runs in the middle of one.** The two halves are in the same
        // tick, which is what makes a common event cheap: a shop that calls
        // three helpers does not cost three frames.
        vm.StepTick();

        AssertEq(
            vm.VariableBands.Resolve(Normal0), 42,
            "**and the common event's variable is the one the game has**, because"
            + " a call shares the state and not a copy of it;"
            + $" it is {vm.VariableBands.Resolve(Normal0)}");
        AssertEq(
            vm.State, WolfVmState.Completed,
            "**and the game is finished**, and not stopped inside the call;"
            + $" it is {vm.State}");
    }

    /// <summary>
    /// The caller runs the line after the call.
    /// </summary>
    /// <remarks>
    /// <strong>99, not 42.</strong> The caller sets normal 0 to 99 after the
    /// call, and the common event had set it to 42 — so a value of 42 at the
    /// end would mean the caller's own line never ran, and the game would be an
    /// event that calls something and then stops.
    /// </remarks>
    public void Test_TheCallerRunsItsOwnLineAfterTheCall()
    {
        var vm = new WolfEventVm { CommonEvents = [Setter(7, 42)] };
        vm.Start(Caller(1, 7));
        vm.StepTick();

        AssertEq(
            vm.VariableBands.Resolve(Normal0), 99,
            "**and the caller's own line is the one the game has**, because the"
            + " call comes back and does not end the caller;"
            + $" it is {vm.VariableBands.Resolve(Normal0)}");
    }

    /// <summary>
    /// A common event that calls a common event comes back twice.
    /// </summary>
    /// <remarks>
    /// <strong>Two frames and two returns.</strong> This is what a shop is: a
    /// common event that adds a product calls another that increments a
    /// counter, and a reader with one level of return would have the inner
    /// event's end resume nobody and the outer caller lost.
    /// </remarks>
    public void Test_NestedCallsComeBackTwice()
    {
        var vm = new WolfEventVm
        {
            CommonEvents =
            [
                new WolfEventProgram
                {
                    Id = 10,
                    Commands =
                    [
                        new WolfEventCommand
                        {
                            Opcode = WolfEventOpcode.CallCommonEvent,
                            Operand = 11,
                        },
                        new WolfEventCommand
                        {
                            Opcode = WolfEventOpcode.SetVariable,
                            Operand = Normal0,
                            Value = 20,
                        },
                        new WolfEventCommand { Opcode = WolfEventOpcode.End },
                    ],
                },
                Setter(11, 11),
            ],
        };
        // **A caller that only calls**, because a caller that sets a variable of
        // its own overwrites the value the outer common event left — and a
        // value of 99 here would be the caller's line running, which is the
        // test above and not this one.
        vm.Start(CallerOnly(1, 10));
        vm.StepTick();

        AssertEq(
            vm.VariableBands.Resolve(Normal0), 20,
            "**and the outer common event's value is the one left**, which means"
            + " the inner one ran and the outer one carried on after it;"
            + $" it is {vm.VariableBands.Resolve(Normal0)}");
        AssertEq(
            vm.State, WolfVmState.Completed,
            "**and the game is finished**, not stopped in a frame of the stack;"
            + $" it is {vm.State}");
    }

    /// <summary>
    /// A common event that moves a figure moves the one the player can see.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>One board, and that is the point of a call.</strong> A common
    /// event that moves a figure moves the figure on screen; a reader that gave
    /// the call its own board would have a shop's common event run on a board
    /// nobody looks at and the player would see nothing happen.
    /// </para>
    /// <para>
    /// <strong>The wait is what makes the walk visible.</strong> This common
    /// event waits for its own route, which is how a game writes it: the event
    /// that moves a figure either waits or keeps running. Without the wait the
    /// VM reaches the event's end in the same tick and the figure has one
    /// frame — which is not a walk.
    /// </para>
    /// </remarks>
    public void Test_AWaitedRouteInACallMovesTheCallersFigure()
    {
        var vm = VmWithFigure();
        vm.CommonEvents =
        [
            new WolfEventProgram
            {
                Id = 30,
                Commands =
                [
                    new WolfEventCommand
                    {
                        Opcode = WolfEventOpcode.MoveRoute,
                        CharacterId = 1,
                        Route = new WolfMoveRoute
                        {
                            Steps =
                            [
                                new WolfMoveRouteStep
                                {
                                    Type = WolfMoveRouteType.MoveRight,
                                },
                            ],
                            Mode = WolfMoveRouteMode.Custom,
                            WaitUntilDone = true,
                        },
                    },
                    new WolfEventCommand { Opcode = WolfEventOpcode.End },
                ],
            },
        ];
        vm.Start(CallerOnly(1, 30));

        for (var frame = 0; frame < 10; frame++)
        {
            vm.StepTick();
        }

        AssertEq(
            vm.Board.Find(1)!.X, 1,
            "**and the figure the common event drove is the one on the board**,"
            + " because a call shares the state and not a copy of it;"
            + $" it is at X {vm.Board.Find(1)!.X}");
    }

    // ---- Was der Aufruf nicht kann

    /// <summary>
    /// An id the table does not have is a failure that names the id.
    /// </summary>
    /// <remarks>
    /// <strong>A refusal and not a silent skip.</strong> A game's event library
    /// that lost an event is a broken game file, and the person fixing it needs
    /// to know which event — so the message carries the number, and "call
    /// failed" would have sent them looking through every file.
    /// </remarks>
    public void Test_AnUnknownEventIdIsNamed()
    {
        var vm = new WolfEventVm { CommonEvents = [Setter(7, 42)] };
        vm.Start(CallerOnly(1, 999));

        var result = vm.StepTick();

        AssertEq(
            result.Success, false,
            "**and the call is refused**, because the table does not have it;"
            + $" it succeeded: {result.Success}");
        AssertTrue(
            (result.Error?.Message ?? "").Contains("999"),
            "**and the message names the event that is missing**, because a person"
            + $" fixing the file needs to know which one; it said: "
            + result.Error?.Message);
    }

    /// <summary>
    /// A call with no event is refused, and id zero is not one.
    /// </summary>
    /// <remarks>
    /// <strong>Zero is the hero.</strong> WOLF's database ids count the hero
    /// from zero, so a call that names 0 is a call that names the player, and a
    /// reader that treated it as "no event given" would refuse for the wrong
    /// reason while a reader that looked it up would find a figure and try to
    /// run it as an event.
    /// </remarks>
    public void Test_ACallWithNoEventIsRefused()
    {
        var vm = new WolfEventVm { CommonEvents = [Setter(7, 42)] };
        vm.Start(CallerOnly(1, 0));

        var result = vm.StepTick();

        AssertEq(
            result.Success, false,
            "**and the call is refused**, because zero is the hero and not an"
            + $" event; it succeeded: {result.Success}");
        AssertTrue(
            (result.Error?.Message ?? "").Contains("0"),
            "**and the message says the id was zero**, so the person fixing the"
            + $" file knows where to look; it said: {result.Error?.Message}");
    }

    /// <summary>
    /// A common event that calls itself stops at the limit.
    /// </summary>
    /// <remarks>
    /// <strong>A failure with a number in it, and not a hang.</strong> A common
    /// event that calls itself is a mistake in a game's files, and without a
    /// limit the VM would run until the process ended — which a player sees as
    /// a game that froze on one tile and that nobody can report usefully.
    /// </remarks>
    public void Test_ASelfCallingEventStopsAtTheLimit()
    {
        var selfCall = new WolfEventProgram
        {
            Id = 20,
            Commands =
            [
                new WolfEventCommand
                {
                    Opcode = WolfEventOpcode.CallCommonEvent,
                    Operand = 20,
                },
                new WolfEventCommand { Opcode = WolfEventOpcode.End },
            ],
        };
        var vm = new WolfEventVm { CommonEvents = [selfCall] };
        vm.Start(CallerOnly(1, 20));

        for (var frame = 0; frame < WolfEventVm.MaxCallDepth + 5; frame++)
        {
            vm.StepTick();
            if (vm.State == WolfVmState.Faulted)
            {
                break;
            }
        }

        AssertEq(
            vm.State, WolfVmState.Faulted,
            "**and the VM reports a fault**, and not a game that runs forever;"
            + $" it is {vm.State}");
        AssertTrue(
            vm.LastError?.Message.Contains("itself") ?? false,
            "**and the message says the event calls itself**, because that is"
            + $" what has to be fixed; it said: {vm.LastError?.Message}");
    }

    // ---- Die Grenzen des Zustands

    /// <summary>
    /// A new game leaves no frame from the last one.
    /// </summary>
    /// <remarks>
    /// <strong>The stack goes with everything else.</strong> A stack left over
    /// would have the new game's first End pop a frame pointing at a program
    /// from the old game, and the new game's event would resume inside the last
    /// game's — which is not a crash but is worse, because it runs.
    /// </remarks>
    public void Test_ANewGameLeavesNoFrameBehind()
    {
        var vm = new WolfEventVm { CommonEvents = [Setter(7, 42)] };
        vm.Start(Caller(1, 7));
        vm.StepTick();

        vm.ResetState();
        vm.Start(Caller(2, 7));
        vm.StepTick();

        AssertEq(
            vm.VariableBands.Resolve(Normal0), 99,
            "**and the new game's caller runs to its own end**, which a frame"
            + " from the last game would have interrupted;"
            + $" it is {vm.VariableBands.Resolve(Normal0)}");
    }

    /// <summary>
    /// An event with no commands is already finished.
    /// </summary>
    /// <remarks>
    /// <strong>The caller carries on in the same tick.</strong> A reader that
    /// pushed a frame and ran nothing would leave the caller suspended on a call
    /// that has nothing to wait for — and the game would stand still with no
    /// error, which is the hardest kind of wrong to report.
    /// </remarks>
    public void Test_AnEmptyEventDoesNotSuspendTheCaller()
    {
        var vm = new WolfEventVm
        {
            CommonEvents = [new WolfEventProgram { Id = 40, Commands = [] }],
        };
        vm.Start(Caller(1, 40));
        vm.StepTick();

        AssertEq(
            vm.VariableBands.Resolve(Normal0), 99,
            "**and the caller's own line ran**, because an empty event is"
            + $" finished and not something to wait for; it is {vm.VariableBands.Resolve(Normal0)}");
    }

    /// <summary>
    /// A call returns no value, and does not invent a zero.
    /// </summary>
    /// <remarks>
    /// <strong>Null and not 0.</strong> A call whose event returns nothing
    /// leaves the result alone, because a reader that wrote zero would make a
    /// call that returns nothing indistinguishable from one that returns zero —
    /// and a common event used as a function would then read a zero the game
    /// never produced.
    /// </remarks>
    public void Test_ACallReturnsNoValueAndInventsNothing()
    {
        var vm = new WolfEventVm { CommonEvents = [Setter(7, 42)] };
        vm.Start(CallerOnly(1, 7));
        vm.StepTick();

        AssertEq(
            vm.LastCommonEventResult, null,
            "**and the result is nothing**, and not a zero the game never"
            + $" produced; it is {(vm.LastCommonEventResult?.ToString() ?? "null")}");
        AssertEq(
            vm.LastCommonEventResult, null,
            "**and it is still nothing after the call returned**, because this"
            + " reader has no return value to give and does not pretend to;"
            + $" it is {(vm.LastCommonEventResult?.ToString() ?? "null")}");
    }
}
