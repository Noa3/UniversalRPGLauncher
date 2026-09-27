using System.Collections.Generic;
using UniversalRPG.Rm2k;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Proves the move route state machine from
/// <c>Game_Character::UpdateMoveRoute</c>.
/// </summary>
/// <remarks>
/// The two behaviours that matter and are easy to get wrong are the immediate
/// return after a command that starts a step, and the wrap test against the
/// start index rather than against the length. Both are asserted here as
/// transitions, because a test that only checked the final index would pass for
/// a machine that ran the whole route in one update.
/// </remarks>
public partial class TestRm2kMoveRouteState : TestBase
{
	private static Rm2kMap.MoveCommand Command(int pId)
	{
		return new Rm2kMap.MoveCommand(pId);
	}

	private static Rm2kMoveRouteState Route(params int[] pCommandIds)
	{
		var commands = new List<Rm2kMap.MoveCommand>();
		foreach (var id in pCommandIds)
		{
			commands.Add(Command(id));
		}
		return new Rm2kMoveRouteState(commands);
	}

	public void Test_ARouteThatWasNeverForcedHasNoCurrentCommand()
	{
		// ForceMoveRoute is what starts a route. A route that exists but was
		// never forced has no current command, which is why an event that merely
		// defines a route does not walk.
		var route = Route(Rm2kMoveRoute.MoveDown, Rm2kMoveRoute.MoveDown);
		AssertEq(route.Current, null, "an unforced route has no current command");
		AssertTrue(!route.Active, "and it is not active");
		route.Force(0);
		AssertEq(route.Current!.CommandId, Rm2kMoveRoute.MoveDown, "forcing starts it at index 0");
		AssertTrue(route.Active, "and it is active");
	}

	public void Test_EachAdvanceMovesOnByOneCommand()
	{
		var route = Route(Rm2kMoveRoute.MoveDown, Rm2kMoveRoute.Wait, Rm2kMoveRoute.MoveLeft);
		route.Force(0);
		AssertEq(route.CurrentIndex, 0, "starts at 0");
		AssertEq(route.Current!.CommandId, Rm2kMoveRoute.MoveDown, "first is move down");
		route.Advance();
		AssertEq(route.CurrentIndex, 1, "advances to 1");
		AssertEq(route.Current!.CommandId, Rm2kMoveRoute.Wait, "second is wait");
		route.Advance();
		AssertEq(route.CurrentIndex, 2, "advances to 2");
		AssertEq(route.Current!.CommandId, Rm2kMoveRoute.MoveLeft, "third is move left");
	}

	public void Test_ARouteAdvancesOneCommandAtATimeRatherThanRunningToCompletion()
	{
		// This is the property that makes a route look like walking. A machine
		// that ran the whole route in one update would also leave the index at
		// the end, so the index alone cannot prove it; the intermediate states
		// can.
		var route = Route(Rm2kMoveRoute.MoveDown, Rm2kMoveRoute.MoveDown, Rm2kMoveRoute.MoveDown);
		route.Force(0);
		var seen = new List<int>();
		for (var step = 0; step < 3; step++)
		{
			var current = route.Current;
			AssertTrue(current != null, $"command {step} is available before it is handled");
			seen.Add(current!.CommandId);
			route.Advance();
		}
		AssertEq(seen.Count, 3, "three commands were handled one at a time");
		AssertEq(seen[0], Rm2kMoveRoute.MoveDown, "first");
		AssertEq(seen[1], Rm2kMoveRoute.MoveDown, "second");
		AssertEq(seen[2], Rm2kMoveRoute.MoveDown, "third");
	}

	public void Test_ARepatingRouteWrapsAndFinishesOnReturningToItsStart()
	{
		// The Player compares the index against start_index, not against the
		// length, which is what makes a route that was forced part way through
		// still terminate.
		var route = Route(Rm2kMoveRoute.MoveDown, Rm2kMoveRoute.MoveLeft, Rm2kMoveRoute.MoveUp);
		route.Force(0);
		AssertTrue(!route.Finished, "not finished at the start");

		route.Advance();
		route.Advance();
		AssertEq(route.CurrentIndex, 2, "at the last command");
		AssertTrue(!route.Finished, "and still not finished");

		route.Advance();
		AssertEq(route.CurrentIndex, 0, "wrapping returns to the start index");
		AssertTrue(route.Finished, "and arriving back at the start finishes the route");
		AssertTrue(!route.Active, "and stops it");
		AssertEq(route.Current, null, "so it has no current command");
	}

	public void Test_ANonRepeatingRouteStopsOnTheStepPastItsLastCommand()
	{
		// A non repeating route is not wrapped: it stops, and it reports itself
		// finished so a page can move on.
		var route = new Rm2kMoveRouteState(
			new List<Rm2kMap.MoveCommand> { Command(Rm2kMoveRoute.MoveDown) }, pRepeat: false);
		route.Force(0);
		route.Advance();
		AssertTrue(route.Finished, "a non repeating route finishes after its last command");
		AssertTrue(!route.Active, "and stops");
		AssertEq(route.Current, null, "so it has no current command");
		AssertEq(route.CurrentIndex, 1,
			"and the index sits past the end, where the Player leaves it");
	}

	public void Test_ARouteForcedPartWayThroughStillFinishesAtItsOwnStart()
	{
		// A forced route records where it started. A repeating route that comes
		// back to that index is finished, even if it started in the middle of a
		// longer list, because the index comparison is against the start and not
		// against the length.
		var route = Route(
			Rm2kMoveRoute.MoveDown, Rm2kMoveRoute.MoveDown, Rm2kMoveRoute.MoveDown,
			Rm2kMoveRoute.MoveDown);
		route.Force(2);
		AssertEq(route.CurrentIndex, 2, "forced at index 2");
		AssertEq(route.StartIndex, 2, "which is recorded as the start");

		route.Advance();
		AssertEq(route.CurrentIndex, 3, "at 3");
		AssertTrue(!route.Finished, "not finished at 3");
		route.Advance();
		AssertEq(route.CurrentIndex, 0, "wrapped past the end to 0");
		AssertTrue(!route.Finished, "still not finished, because the start was 2");
		route.Advance();
		AssertEq(route.CurrentIndex, 1, "at 1");
		AssertTrue(!route.Finished, "still not finished");
		route.Advance();
		AssertEq(route.CurrentIndex, 2, "back at the start");
		AssertTrue(route.Finished, "and finished");
	}

	public void Test_CancellingStopsTheRouteWithoutFinishingIt()
	{
		// CancelMoveRoute stops the route but does not claim it completed, so a
		// page can tell the difference between a route that ran out and one that
		// was interrupted.
		var route = Route(Rm2kMoveRoute.MoveDown, Rm2kMoveRoute.MoveLeft);
		route.Force(0);
		route.Advance();
		route.Cancel();
		AssertTrue(!route.Active, "a cancelled route is not active");
		AssertTrue(!route.Finished, "and is not finished either");
		AssertEq(route.Current, null, "so it has no current command");
	}

	public void Test_TheIndexCanBeRestoredTheWayASaveRestoresIt()
	{
		// GetMoveRouteIndex and SetMoveRouteIndex exist so a save can resume a
		// route part way through, which is what a mid route load depends on.
		var route = Route(Rm2kMoveRoute.MoveDown, Rm2kMoveRoute.Wait, Rm2kMoveRoute.MoveLeft);
		route.Force(0);
		route.Advance();
		var saved = route.CurrentIndex;
		AssertEq(saved, 1, "one command in");

		var restored = Route(Rm2kMoveRoute.MoveDown, Rm2kMoveRoute.Wait, Rm2kMoveRoute.MoveLeft);
		restored.Force(0);
		restored.SetIndex(saved);
		AssertEq(restored.CurrentIndex, 1, "and the restored route resumes there");
		AssertEq(restored.Current!.CommandId, Rm2kMoveRoute.Wait, "at the same command");
	}

	public void Test_AMoveFailureIsCountedAndClearedByTheNextCommand()
	{
		// The Player keeps a failure count so it can stop a route that is stuck
		// against a wall, and resets it once a command succeeds.
		var route = Route(Rm2kMoveRoute.MoveDown, Rm2kMoveRoute.MoveLeft);
		route.Force(0);
		AssertEq(route.MoveFailureCount, 0, "no failures yet");
		route.NoteMoveFailure();
		route.NoteMoveFailure();
		AssertEq(route.MoveFailureCount, 2, "two failures are counted");
		route.Advance();
		AssertEq(route.MoveFailureCount, 0, "and advancing clears them");
	}

	public void Test_AnEmptyRouteHasNothingToRunAndDoesNotSpin()
	{
		// A page with no route must not wrap forever on an empty list.
		var route = new Rm2kMoveRouteState();
		route.Force(0);
		AssertEq(route.Current, null, "an empty route has no current command");
		route.Advance();
		AssertEq(route.CurrentIndex, 0, "and the index stays at the start");
		AssertTrue(!route.Active, "and the route is not left active");
	}

	public void Test_TheOverwrittenFlagIsDistinctFromBeingActive()
	{
		// IsMoveRouteOverwritten says the page's own route was replaced by a
		// forced one, which is a different question from whether a route is
		// running.
		var route = Route(Rm2kMoveRoute.MoveDown);
		AssertTrue(!route.Overwritten, "not overwritten to begin with");
		route.Force(0);
		AssertTrue(!route.Overwritten, "forcing does not set it either");
		route.SetOverwritten(true);
		AssertTrue(route.Overwritten, "it is set explicitly");
		AssertTrue(route.Active, "and the route is still active at the same time");
	}

	public void Test_TheSpeedAndFrequencyDefaultToTheLiblcfValues()
	{
		// liblcf EventPage::move_speed defaults to 3 and move_frequency to 3.
		var route = Route(Rm2kMoveRoute.MoveDown);
		AssertEq(route.MoveSpeed, 3, "move speed defaults to 3");
		AssertEq(route.MoveFrequency, 3, "move frequency defaults to 3");
	}
}
