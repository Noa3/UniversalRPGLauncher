using System;
using System.Collections.Generic;

using UniversalRPG.Tests.Framework;
using UniversalRPG.Wolf;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Calling a map event, passing it inputs, and the two things the help says
/// that are surprising.
/// </summary>
/// <remarks>
/// <para>
/// The binary reader decoded type 210 — a call to an event — and told the two
/// kinds apart by the id's range: below 500,000 is a map event, from 500,000 a
/// common one, and only then does the call carry arguments. The opcode enum had
/// no value for it at all, so a map event that calls another map event could
/// not run the call.
/// </para>
/// <para>
/// <strong>Two of the help's rules contradict the instinct.</strong> An event
/// that does not exist is <em>ignored</em> and not an error, and the called
/// event's self variables are its own and not the caller's. Both are below.
/// </para>
/// </remarks>
public partial class TestWolfEventCall : TestBase
{
    /// <summary>The normal band, variable zero — the addressing scheme's 2,000,000.</summary>
    private const int Normal0 = 2_000_000;

    /// <summary>Where a common event's own self 0 lives.</summary>
    private const int CommonSelf0 = WolfVariable.BaseCommonSelf;

    /// <summary>Where a map event's own self 0 lives.</summary>
    private const int MapSelf0 = WolfVariable.BaseMapSelf;

    /// <summary>The first common event id the help's range starts at.</summary>
    private const int CommonEventBase = 500_000;

    /// <summary>
    /// A program that stores its own self 0 into a normal variable and ends.
    /// </summary>
    private static WolfEventProgram ReadsItsSelf(int pId)
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
                    Value = CommonSelf0,
                },
                new WolfEventCommand { Opcode = WolfEventOpcode.End },
            ],
        };
    }

    /// <summary>A program that only calls an event and then ends.</summary>
    private static WolfEventProgram CallerOf(int pId, int pTarget, params int[] pInputs)
    {
        return new WolfEventProgram
        {
            Id = pId,
            Commands =
            [
                new WolfEventCommand
                {
                    Opcode = WolfEventOpcode.CallEvent,
                    Operand = pTarget,
                    CallInputs = pInputs,
                },
                new WolfEventCommand { Opcode = WolfEventOpcode.End },
            ],
        };
    }

    // ---- Der Aufruf laeuft

    /// <summary>
    /// An id below 500,000 is a map event, and it runs.
    /// </summary>
    /// <remarks>
    /// <strong>The split is the help's and not ours.</strong> The page-call note
    /// says 0 and above is a map event and 500,000 and above is a common one,
    /// and it is the same split the switch numbers use — so one command names
    /// either kind and the number alone says which.
    /// </remarks>
    public void Test_AnIdBelowTheCommonBaseIsAMapEvent()
    {
        var vm = new WolfEventVm
        {
            MapEvents =
            [
                new WolfEventProgram
                {
                    Id = 5,
                    Commands =
                    [
                        new WolfEventCommand
                        {
                            Opcode = WolfEventOpcode.SetVariable,
                            Operand = Normal0,
                            Value = 77,
                        },
                        new WolfEventCommand { Opcode = WolfEventOpcode.End },
                    ],
                },
            ],
        };
        vm.Start(CallerOf(1, 5));
        vm.StepTick();

        AssertEq(
            vm.VariableBands.Resolve(Normal0), 77,
            "**and the map event ran**, because an id below 500,000 is a map"
            + " event per the help's page-call note;"
            + $" it is {vm.VariableBands.Resolve(Normal0)}");
    }

    /// <summary>
    /// An id from 500,000 up is a common event, and the offset comes off.
    /// </summary>
    /// <remarks>
    /// <strong>The offset is subtracted and not looked up as it stands.</strong> A
    /// common event's database id is small — the help caps the list at 10,000 —
    /// and the call adds 500,000 to say which kind it means. A reader that
    /// looked the raw number up in the common table would find nothing and
    /// ignore the call, and a game's shops would silently do nothing.
    /// </remarks>
    public void Test_AnIdFromTheCommonBaseIsACommonEvent()
    {
        var vm = new WolfEventVm
        {
            CommonEvents =
            [
                new WolfEventProgram
                {
                    Id = 3,
                    Commands =
                    [
                        new WolfEventCommand
                        {
                            Opcode = WolfEventOpcode.SetVariable,
                            Operand = Normal0,
                            Value = 88,
                        },
                        new WolfEventCommand { Opcode = WolfEventOpcode.End },
                    ],
                },
            ],
        };
        vm.Start(CallerOf(1, CommonEventBase + 3));
        vm.StepTick();

        AssertEq(
            vm.VariableBands.Resolve(Normal0), 88,
            "**and the common event ran**, because 500,000 and above is a common"
            + " event and the offset is what says which one;"
            + $" it is {vm.VariableBands.Resolve(Normal0)}");
    }

    // ---- Die Self-Variablen

    /// <summary>
    /// The inputs become the called event's own self variables.
    /// </summary>
    /// <remarks>
    /// <strong>Input 1 is self 0, input 2 is self 1.</strong> The help's page-call
    /// note says exactly that. A reader that wrote the inputs somewhere else
    /// would have a shop's helper read a self variable nobody filled, and every
    /// common event that took an argument would see zero.
    /// </remarks>
    public void Test_TheInputsBecomeTheCalleesOwnSelfVariables()
    {
        var vm = new WolfEventVm
        {
            MapEvents = [ReadsItsSelf(5)],
        };
        vm.Start(CallerOf(1, 5, 1234));
        vm.StepTick();

        AssertEq(
            vm.VariableBands.Resolve(Normal0), 1234,
            "**and the called event read its own self 0 and found the input**,"
            + " because a call passes a value into the called event's self"
            + $" variables; it is {vm.VariableBands.Resolve(Normal0)}");
    }

    /// <summary>
    /// A call's self variables are the call's, and the caller's survive.
    /// </summary>
    /// <remarks>
    /// <strong>Two frames and not one for the game.</strong> A reader that kept one
    /// set of self variables would have the second call read the first call's
    /// arguments — and a shop that adds two products would price the second from
    /// the first one's numbers.
    /// </remarks>
    public void Test_EachCallHasItsOwnSelfVariables()
    {
        var vm = new WolfEventVm
        {
            MapEvents = [ReadsItsSelf(5)],
        };
        // **The caller passes one value and calls the same event twice with
        // two different ones.** A shared set of self variables would leave the
        // second call reading the first call's number.
        vm.Start(new WolfEventProgram
        {
            Id = 1,
            Commands =
            [
                new WolfEventCommand
                {
                    Opcode = WolfEventOpcode.CallEvent,
                    Operand = 5,
                    CallInputs = [111],
                },
                new WolfEventCommand
                {
                    Opcode = WolfEventOpcode.CallEvent,
                    Operand = 5,
                    CallInputs = [222],
                },
                new WolfEventCommand { Opcode = WolfEventOpcode.End },
            ],
        });
        vm.StepTick();

        AssertEq(
            vm.VariableBands.Resolve(Normal0), 222,
            "**and the second call's input is the one the game has**, because"
            + " each call gets its own self variables and not the game's one"
            + $" set; it is {vm.VariableBands.Resolve(Normal0)}");
    }

    /// <summary>
    /// A called map event shares the self variables of the event that called it.
    /// </summary>
    /// <remarks>
    /// <strong>Map self and common self are not the same thing.</strong> The
    /// help gives map self at 1,100,000 and common self at 1,600,000, and a
    /// map event called by an event has no call of its own — its self variables
    /// are the ones of the event that called it. A reader that gave it a frame
    /// of its own would have a chain of map events lose the values the first one
    /// was given.
    /// </remarks>
    public void Test_AMapEventSharesTheCallersSelfVariables()
    {
        var vm = new WolfEventVm
        {
            MapEvents =
            [
                // **The inner event writes only to map self 0, and the outer one
                // reads it back into normal 0.** A map event's self variables
                // are the caller's — that is what map self means — so the write
                // the inner event makes has to be visible to the outer program
                // after the call returns. Nothing else is written, so the one
                // value at the end is the one this test is about.
                new WolfEventProgram
                {
                    Id = 5,
                    Commands =
                    [
                        new WolfEventCommand
                        {
                            Opcode = WolfEventOpcode.SetVariable,
                            Operand = MapSelf0,
                            Value = 42,
                        },
                        new WolfEventCommand { Opcode = WolfEventOpcode.End },
                    ],
                },
            ],
        };
        vm.Start(new WolfEventProgram
        {
            Id = 1,
            Commands =
            [
                new WolfEventCommand
                {
                    Opcode = WolfEventOpcode.CallEvent,
                    Operand = 5,
                    CallInputs = [7],
                },
                new WolfEventCommand
                {
                    Opcode = WolfEventOpcode.SetVariable,
                    Operand = Normal0,
                    Value = MapSelf0,
                },
                new WolfEventCommand { Opcode = WolfEventOpcode.End },
            ],
        });
        vm.StepTick();

        AssertEq(
            vm.VariableBands.Resolve(Normal0), 42,
            "**and the caller's own map self 0 is the one the inner event"
            + " wrote**, because a map event called by an event has no self"
            + $" variables of its own; it is {vm.VariableBands.Resolve(Normal0)}");
    }

    // ---- Das Ueberraschende

    /// <summary>
    /// An event that does not exist is ignored, and the game carries on.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>The help says so in one line</strong> — an event that does not
    /// exist is ignored — <strong>and it is the one place in this VM where a
    /// missing thing is deliberately not a fault.</strong> The reason is that a
    /// game deletes an event and leaves a call behind all the time.
    /// </para>
    /// <para>
    /// <strong>A reader that failed the event there would have a game that
    /// stopped dead</strong> at a call to a treasure chest the author removed,
    /// with a message no player could act on. The instinct is to refuse, and
    /// the specification says not to.
    /// </para>
    /// </remarks>
    public void Test_AMissingEventIsIgnoredAndTheGameCarriesOn()
    {
        var vm = new WolfEventVm
        {
            MapEvents = [],
        };
        vm.Start(new WolfEventProgram
        {
            Id = 1,
            Commands =
            [
                new WolfEventCommand
                {
                    Opcode = WolfEventOpcode.CallEvent,
                    Operand = 999,
                },
                new WolfEventCommand
                {
                    Opcode = WolfEventOpcode.SetVariable,
                    Operand = Normal0,
                    Value = 5,
                },
                new WolfEventCommand { Opcode = WolfEventOpcode.End },
            ],
        });

        var result = vm.StepTick();

        AssertEq(
            result.Success, true,
            "**and the call is not a fault**, because the help says a missing"
            + $" event is ignored; it failed: {result.Error?.Message}");
        AssertEq(
            vm.VariableBands.Resolve(Normal0), 5,
            "**and the line after the call ran**, which is what a reader that"
            + " failed there would have stopped;"
            + $" it is {vm.VariableBands.Resolve(Normal0)}");
    }

    /// <summary>
    /// A call that names no event is a failure, and not an ignored one.
    /// </summary>
    /// <remarks>
    /// <strong>A negative number and not a missing one.</strong> Zero is the
    /// hero, which is a real target, and a negative number is below the first
    /// id there is no "does not exist" reading of — it is a number the format
    /// cannot hold, and a game that has one is broken in a way worth naming.
    /// </remarks>
    public void Test_ANegativeIdIsAFailureAndNotAnIgnoredOne()
    {
        var vm = new WolfEventVm { MapEvents = [] };
        vm.Start(CallerOf(1, -1));

        var result = vm.StepTick();

        AssertEq(
            result.Success, false,
            "**and the call is refused**, because a number below the first event"
            + $" id is not a missing event; it succeeded: {result.Success}");
    }

    /// <summary>
    /// An event that calls itself stops at the limit here too.
    /// </summary>
    /// <remarks>
    /// <strong>The same guard as the common call, and for the same reason.</strong>
    /// A map event that calls itself through a ring of two is the same mistake
    /// in a different place, and a reader that guarded one kind and not the
    /// other would have half a protection — which is worse than none, because
    /// it looks like it works.
    /// </remarks>
    public void Test_ASelfCallingMapEventStopsAtTheLimit()
    {
        var vm = new WolfEventVm
        {
            MapEvents =
            [
                new WolfEventProgram
                {
                    Id = 20,
                    Commands =
                    [
                        new WolfEventCommand
                        {
                            Opcode = WolfEventOpcode.CallEvent,
                            Operand = 20,
                        },
                        new WolfEventCommand { Opcode = WolfEventOpcode.End },
                    ],
                },
            ],
        };
        vm.Start(CallerOf(1, 20));

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
    }

    /// <summary>
    /// A new game leaves no self variables behind.
    /// </summary>
    /// <remarks>
    /// <strong>The self stack goes with the call stack.</strong> A frame left
    /// over would have the new game's first call read the last game's arguments
    /// — and a shop in the new game would open with the prices of whatever was
    /// loaded before it.
    /// </remarks>
    public void Test_ANewGameLeavesNoSelfVariablesBehind()
    {
        var vm = new WolfEventVm { MapEvents = [ReadsItsSelf(5)] };
        vm.Start(CallerOf(1, 5, 111));
        vm.StepTick();

        vm.ResetState();
        vm.Start(CallerOf(2, 5, 222));
        vm.StepTick();

        AssertEq(
            vm.VariableBands.Resolve(Normal0), 222,
            "**and the new game's call sees its own input**, which a frame from"
            + $" the last game would have replaced; it is {vm.VariableBands.Resolve(Normal0)}");
    }

    /// <summary>
    /// A call with no inputs is a call with an empty self zero.
    /// </summary>
    /// <remarks>
    /// <strong>Zero and not the caller's own.</strong> A common event called with
    /// nothing reads self 0 as zero, and a reader that let it fall through to
    /// the caller's value would have a shop whose price depends on which event
    /// happened to call it.
    /// </remarks>
    public void Test_ACallWithNoInputsReadsZero()
    {
        var vm = new WolfEventVm { MapEvents = [ReadsItsSelf(5)] };
        vm.Start(CallerOf(1, 5));
        vm.StepTick();

        AssertEq(
            vm.VariableBands.Resolve(Normal0), 0,
            "**and the self variable is zero**, and not the caller's value from"
            + $" the last call; it is {vm.VariableBands.Resolve(Normal0)}");
    }
}
