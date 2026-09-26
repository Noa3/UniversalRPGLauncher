using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using UniversalRPG.Rm2k;
using UniversalRPG.Rm2k.Interpreter;
using UniversalRPG.Rm2k.Presentation;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;
using UniversalRPG.Rm2k.Input;

namespace UniversalRPG.Tests.Core;

public partial class TestEventInterpreter : TestBase
{
	private static List<Rm2kMap.EventCommand> EmptyCommands()
	{
		return new List<Rm2kMap.EventCommand>();
	}

	public void Test_EmptyEventEndsImmediately()
	{
		var state = new GameSimulationState();
		var interpreter = new EventInterpreter(state, 1, EmptyCommands());

		var result = interpreter.ExecuteFrame();

		AssertFalse(result);
		AssertFalse(interpreter.IsRunning);
	}

	public void Test_TriggerValuesMatchVerifiedLiblcfEventPageTrigger()
	{
		AssertEq((int)Rm2kEventTrigger.Action, 0);
		AssertEq((int)Rm2kEventTrigger.Touched, 1);
		AssertEq((int)Rm2kEventTrigger.Collision, 2);
		AssertEq((int)Rm2kEventTrigger.AutoStart, 3);
		AssertEq((int)Rm2kEventTrigger.Parallel, 4);
	}

	public void Test_ActionFacingSearchesThroughThreeCounterTiles()
	{
		// Verified Game_Player::CheckEventTriggerThere: the action trigger is
		// searched on the tile in front, and the search continues over at most
		// three counter tiles in the facing direction.
		var state = new GameSimulationState();
		state.ConfigureMap(1, 5, 5, new byte[25]);
		state.MapX = 0;
		state.MapY = 4;
		state.FacingDirection = 8; // up

		var upper = new int[25];
		var upperFlags = new byte[144];
		upperFlags[0] = Rm2kChipset.PassCounter;
		// A counter chain at (0,3), (0,2) and (0,1); (0,0) stays plain.
		upper[0 + 3 * 5] = Rm2kChipset.BlockF;
		upper[0 + 2 * 5] = Rm2kChipset.BlockF;
		upper[0 + 1 * 5] = Rm2kChipset.BlockF;
		state.UpperLayer = upper;
		state.UpperPassability = upperFlags;

		var events = new List<Rm2kMap.Event>();
		var reachable = new Rm2kMap.Event(1, 0, 1);
		reachable.Pages.Add(new Rm2kMap.EventPage
		{
			Trigger = (int)Rm2kEventTrigger.Action,
			Layer = Rm2kEventScheduler.LayerSame,
		});
		reachable.Pages[0].Commands.Add(new Rm2kMap.EventCommand(EventInterpreter.End));
		events.Add(reachable);

		var scheduler = new Rm2kEventScheduler(state);
		scheduler.SetEvents(events);
		AssertTrue(scheduler.TriggerActionFacing(),
			"the action event is found through the counter tile chain");
	}

	public void Test_ActionFacingStopsAtTheThirdCounterTile()
	{
		// The Player checks the tile in front and then steps over at most three
		// counter tiles, so the fourth counter tile in a row blocks the search.
		var state = new GameSimulationState();
		state.ConfigureMap(1, 5, 10, new byte[50]);
		state.MapX = 0;
		state.MapY = 9;
		state.FacingDirection = 8; // up

		var upper = new int[50];
		var upperFlags = new byte[144];
		upperFlags[0] = Rm2kChipset.PassCounter;
		foreach (var y in new[] { 8, 7, 6, 5, 4 })
		{
			upper[0 + y * 5] = Rm2kChipset.BlockF;
		}
		state.UpperLayer = upper;
		state.UpperPassability = upperFlags;

		// A four counter tile chain between the player and the event at (0,4).
		var beyond = new Rm2kMap.Event(1, 0, 4);
		beyond.Pages.Add(new Rm2kMap.EventPage
		{
			Trigger = (int)Rm2kEventTrigger.Action,
			Layer = Rm2kEventScheduler.LayerSame,
		});
		beyond.Pages[0].Commands.Add(new Rm2kMap.EventCommand(EventInterpreter.End));

		var scheduler = new Rm2kEventScheduler(state);
		scheduler.SetEvents(new List<Rm2kMap.Event> { beyond });
		AssertFalse(scheduler.TriggerActionFacing(),
			"four counter tiles in a row stop the action search");

		// The same event is reachable when only three counter tiles separate it:
		// with counter tiles at (0,8), (0,7) and (0,6) the search checks
		// (0,8), (0,7), (0,6) and then (0,5).
		upper[0 + 5 * 5] = 0;
		var reachable = new Rm2kMap.Event(2, 0, 5);
		reachable.Pages.Add(new Rm2kMap.EventPage
		{
			Trigger = (int)Rm2kEventTrigger.Action,
			Layer = Rm2kEventScheduler.LayerSame,
		});
		reachable.Pages[0].Commands.Add(new Rm2kMap.EventCommand(EventInterpreter.End));
		scheduler.SetEvents(new List<Rm2kMap.Event> { reachable });
		AssertTrue(scheduler.TriggerActionFacing(),
			"three counter tiles still reach the event");
	}

	public void Test_ActionLayersFollowThePlayerRules()
	{
		// Verified: events in front of the player must share its layer, events on
		// the player's own tile must not.
		var state = new GameSimulationState();
		state.ConfigureMap(1, 3, 3, new byte[9]);
		state.MapX = 1;
		state.MapY = 1;
		state.FacingDirection = 8; // up, so the event at (1,0) is in front

		var sameLayerInFront = new Rm2kMap.Event(1, 1, 0);
		sameLayerInFront.Pages.Add(new Rm2kMap.EventPage
		{
			Trigger = (int)Rm2kEventTrigger.Action,
			Layer = Rm2kEventScheduler.LayerSame,
		});
		sameLayerInFront.Pages[0].Commands.Add(new Rm2kMap.EventCommand(EventInterpreter.End));

		var belowHere = new Rm2kMap.Event(2, 1, 1);
		belowHere.Pages.Add(new Rm2kMap.EventPage
		{
			Trigger = (int)Rm2kEventTrigger.Action,
			Layer = Rm2kEventScheduler.LayerBelow,
		});
		belowHere.Pages[0].Commands.Add(new Rm2kMap.EventCommand(EventInterpreter.End));

		var scheduler = new Rm2kEventScheduler(state);
		scheduler.SetEvents(new List<Rm2kMap.Event> { sameLayerInFront, belowHere });

		AssertTrue(scheduler.TriggerActionFacing(), "a same layer event in front triggers");
		AssertTrue(scheduler.TriggerActionHere(), "a below layer event on the own tile triggers");
	}

	public void Test_TouchAndCollisionIgnoreCounterChains()
	{
		// Verified: the walking case evaluates only touched and collision pages on
		// the tile in front, with no counter tile walk.
		var state = new GameSimulationState();
		state.ConfigureMap(1, 3, 3, new byte[9]);
		state.MapX = 1;
		state.MapY = 1;
		state.FacingDirection = 2;

		var upper = new int[9];
		var upperFlags = new byte[144];
		upperFlags[0] = Rm2kChipset.PassCounter;
		upper[1 + 0 * 3] = Rm2kChipset.BlockF;
		upper[1 + 2 * 3] = Rm2kChipset.BlockF;
		state.UpperLayer = upper;
		state.UpperPassability = upperFlags;

		var beyond = new Rm2kMap.Event(5, 1, 0);
		beyond.Pages.Add(new Rm2kMap.EventPage
		{
			Trigger = (int)Rm2kEventTrigger.Touched,
			Layer = Rm2kEventScheduler.LayerSame,
		});
		beyond.Pages[0].Commands.Add(new Rm2kMap.EventCommand(EventInterpreter.End));

		var scheduler = new Rm2kEventScheduler(state);
		scheduler.SetEvents(new List<Rm2kMap.Event> { beyond });
		AssertFalse(scheduler.TriggerTouchOrCollisionFacing(),
			"the touch trigger does not walk counter tiles");
	}

	private static Rm2kMap.Event EventWithTrigger(int pId, int pX, int pY, Rm2kEventTrigger pTrigger, int pLayer)
	{
		var mapEvent = new Rm2kMap.Event(pId, pX, pY);
		var page = new Rm2kMap.EventPage
		{
			Trigger = (int)pTrigger,
			Layer = pLayer,
		};
		page.Commands.Add(new Rm2kMap.EventCommand(EventInterpreter.ControlSwitches,
			new List<int> { EventInterpreter.TargetEvalSingle, pId, 1, EventInterpreter.SwitchModeOn }));
		page.Commands.Add(new Rm2kMap.EventCommand(EventInterpreter.End));
		mapEvent.Pages.Add(page);
		return mapEvent;
	}

	public void Test_PlayerTurnSuccessfulStepTriggersTouchedOnItsOwnTile()
	{
		// Verified Game_Player::UpdateMovement: after a successful step the
		// touched/collision lookup happens on the player's own tile and must not
		// share its layer.
		var state = new GameSimulationState();
		state.ConfigureMap(1, 5, 5, Enumerable.Repeat((byte)Rm2kChipset.AllDirections, 25));
		state.MapX = 2;
		state.MapY = 2;
		state.FacingDirection = 2;

		var ownTile = EventWithTrigger(1, 2, 3, Rm2kEventTrigger.Touched, Rm2kEventScheduler.LayerBelow);
		var inFront = EventWithTrigger(2, 2, 4, Rm2kEventTrigger.Touched, Rm2kEventScheduler.LayerSame);
		var scheduler = new Rm2kEventScheduler(state);
		scheduler.SetEvents(new List<Rm2kMap.Event> { ownTile, inFront });
		var turn = new Rm2kPlayerTurn(state, scheduler);

		AssertTrue(turn.Apply(Rm2kInputAction.MoveDown), "the step onto the touched tile triggers");
		AssertEq(state.MapY, 3, "the player moved");
		AssertEq(scheduler.ActiveInterpreterCount, 1, "only the own tile page runs");
	}

	public void Test_PlayerTurnBlockedStepTriggersTouchedInFront()
	{
		// A blocked step leaves the player stopping, so the Player evaluates
		// touched/collision on the tile in front, which must share the layer.
		var state = new GameSimulationState();
		var masks = Enumerable.Repeat((byte)Rm2kChipset.AllDirections, 25).ToArray();
		// Block the tile in front so the step fails.
		masks[2 + 3 * 5] = 0;
		state.ConfigureMap(1, 5, 5, masks);
		state.MapX = 2;
		state.MapY = 2;
		state.FacingDirection = 2;

		var inFront = EventWithTrigger(2, 2, 3, Rm2kEventTrigger.Touched, Rm2kEventScheduler.LayerSame);
		var scheduler = new Rm2kEventScheduler(state);
		scheduler.SetEvents(new List<Rm2kMap.Event> { inFront });
		var turn = new Rm2kPlayerTurn(state, scheduler);

		AssertTrue(turn.Apply(Rm2kInputAction.MoveDown), "the blocked step triggers the tile in front");
		AssertEq(state.MapY, 2, "the player did not move");
		AssertEq(scheduler.ActiveInterpreterCount, 1);
	}

	public void Test_PlayerTurnConfirmRunsTheActionEventCases()
	{
		// Verified CheckActionEvent: the decision key evaluates touched/collision
		// in front, action on the own tile, and the action chain in front.
		var state = new GameSimulationState();
		state.ConfigureMap(1, 5, 5, Enumerable.Repeat((byte)Rm2kChipset.AllDirections, 25));
		state.MapX = 2;
		state.MapY = 2;
		state.FacingDirection = 8;

		var actionInFront = EventWithTrigger(1, 2, 1, Rm2kEventTrigger.Action, Rm2kEventScheduler.LayerSame);
		var scheduler = new Rm2kEventScheduler(state);
		scheduler.SetEvents(new List<Rm2kMap.Event> { actionInFront });
		var turn = new Rm2kPlayerTurn(state, scheduler);

		AssertTrue(turn.Apply(Rm2kInputAction.Confirm), "the action event in front triggers");
		AssertEq(scheduler.ActiveInterpreterCount, 1);
	}

	public void Test_PlayerTurnConfirmDoesNothingWithoutAPage()
	{
		var state = new GameSimulationState();
		state.ConfigureMap(1, 5, 5, Enumerable.Repeat((byte)Rm2kChipset.AllDirections, 25));
		var scheduler = new Rm2kEventScheduler(state);
		var turn = new Rm2kPlayerTurn(state, scheduler);
		AssertFalse(turn.Apply(Rm2kInputAction.Confirm), "no event means no result");
		AssertEq(scheduler.ActiveInterpreterCount, 0);
	}

	public void Test_PlayerTurnRespectsPauseAndRunningEvents()
	{
		var state = new GameSimulationState();
		state.ConfigureMap(1, 5, 5, Enumerable.Repeat((byte)Rm2kChipset.AllDirections, 25));
		state.MapX = 2;
		state.MapY = 2;
		var scheduler = new Rm2kEventScheduler(state);
		var turn = new Rm2kPlayerTurn(state, scheduler);

		state.IsPaused = true;
		AssertFalse(turn.Apply(Rm2kInputAction.MoveRight), "a paused simulation does not move");
		AssertEq(state.MapX, 2);
		state.IsPaused = false;

		// A running event page blocks movement, like Game_Map::IsRunning.
		var autorun = EventWithTrigger(1, 0, 0, Rm2kEventTrigger.AutoStart, Rm2kEventScheduler.LayerSame);
		scheduler.SetEvents(new List<Rm2kMap.Event> { autorun });
		scheduler.ExecuteFrame();
		AssertTrue(scheduler.ActiveInterpreterCount > 0, "the autorun page is running");
		AssertFalse(turn.Apply(Rm2kInputAction.MoveRight), "a running event blocks movement");
		AssertEq(state.MapX, 2, "the player did not move while an event runs");
	}

	public void Test_PlayerTurnIgnoresUnrelatedActions()
	{
		var state = new GameSimulationState();
		state.ConfigureMap(1, 3, 3, Enumerable.Repeat((byte)Rm2kChipset.AllDirections, 9));
		var scheduler = new Rm2kEventScheduler(state);
		var turn = new Rm2kPlayerTurn(state, scheduler);
		AssertFalse(turn.Apply(Rm2kInputAction.None), "no action does nothing");
		AssertFalse(turn.Apply(Rm2kInputAction.Menu), "the menu action is not a map step");
		AssertFalse(turn.Apply(Rm2kInputAction.Cancel), "cancel is not a map step");
		AssertEq(state.MapX, 0);
		AssertEq(state.MapY, 0);
	}

	public void Test_EventPageSelectorUsesHighestEligiblePage()
	{
		var state = new GameSimulationState();
		state.Switches.Add(true);
		var eventData = new Rm2kMap.Event(7, 3, 4);
		eventData.Pages.Add(new Rm2kMap.EventPage { Trigger = (int)Rm2kEventTrigger.Action });
		eventData.Pages.Add(new Rm2kMap.EventPage
		{
			Trigger = (int)Rm2kEventTrigger.Action,
			Conditions = new Dictionary<string, object> { ["switch_id"] = 1, ["switch_value"] = true },
		});

		var page = Rm2kEventPageSelector.Select(eventData, state, Rm2kEventTrigger.Action);

		AssertTrue(page != null);
		AssertEq(page!.Trigger, (int)Rm2kEventTrigger.Action);
	}

	public void Test_EventPageSelectorIgnoresOtherTriggerKinds()
	{
		var state = new GameSimulationState();
		var eventData = new Rm2kMap.Event(21, 1, 1);
		eventData.Pages.Add(new Rm2kMap.EventPage { Trigger = (int)Rm2kEventTrigger.Touched });
		eventData.Pages.Add(new Rm2kMap.EventPage { Trigger = (int)Rm2kEventTrigger.Parallel });

		AssertTrue(Rm2kEventPageSelector.Select(eventData, state, Rm2kEventTrigger.Action) == null,
			"action trigger does not select touched or parallel pages");
		AssertTrue(Rm2kEventPageSelector.Select(eventData, state, Rm2kEventTrigger.Touched) != null,
			"touched trigger selects the touched page");
		AssertTrue(Rm2kEventPageSelector.Select(eventData, state, Rm2kEventTrigger.Parallel) != null,
			"parallel trigger selects the parallel page");
	}

	public void Test_EventPageSelectorRejectsUnsatisfiedConditions()
	{
		var state = new GameSimulationState();
		var eventData = new Rm2kMap.Event(8, 1, 1);
		eventData.Pages.Add(new Rm2kMap.EventPage
		{
			Trigger = (int)Rm2kEventTrigger.AutoStart,
			Conditions = new Dictionary<string, object> { ["switch_id"] = 2, ["switch_value"] = true },
		});

		var page = Rm2kEventPageSelector.Select(eventData, state, Rm2kEventTrigger.AutoStart);

		AssertTrue(page == null);
	}

	public void Test_EventPageSelectorEvaluatesSwitchBAndVariableComparison()
	{
		var state = new GameSimulationState();
		state.Switches.Add(false);
		state.Switches.Add(true);
		state.Variables.Add(10);
		var eventData = new Rm2kMap.Event(11, 1, 1);
		eventData.Pages.Add(new Rm2kMap.EventPage
		{
			Trigger = (int)Rm2kEventTrigger.AutoStart,
			Conditions = new Dictionary<string, object>
			{
				["switch_b_enabled"] = true, ["switch_b_id"] = 2,
				["variable_enabled"] = true, ["variable_id"] = 1,
				["variable_value"] = 5, ["compare_operator"] = 3,
			},
		});

		var page = Rm2kEventPageSelector.Select(eventData, state, Rm2kEventTrigger.AutoStart);

		AssertTrue(page != null);
	}

	public void Test_EventPageSelectorEvaluatesItemAndActorConditions()
	{
		var state = new GameSimulationState();
		state.ItemCounts[12] = 2;
		state.PartyMemberIds.Add(4);
		var eventData = new Rm2kMap.Event(12, 1, 1);
		eventData.Pages.Add(new Rm2kMap.EventPage
		{
			Trigger = (int)Rm2kEventTrigger.AutoStart,
			Conditions = new Dictionary<string, object>
			{
				["item_enabled"] = true, ["item_id"] = 12,
				["actor_enabled"] = true, ["actor_id"] = 4,
			},
		});

		AssertTrue(Rm2kEventPageSelector.Select(eventData, state, Rm2kEventTrigger.AutoStart) != null);
	}

	public void Test_EventPageSelectorEvaluatesDeterministicTimerConditions()
	{
		var state = new GameSimulationState();
		state.SetTimer(1, 2);
		state.SetTimer(2, 4);
		var eventData = new Rm2kMap.Event(13, 1, 1);
		eventData.Pages.Add(new Rm2kMap.EventPage
		{
			Trigger = (int)Rm2kEventTrigger.AutoStart,
			Conditions = new Dictionary<string, object>
			{
				["timer_enabled"] = true, ["timer_sec"] = 2,
				["timer2_enabled"] = true, ["timer2_sec"] = 4,
			},
		});

		AssertTrue(Rm2kEventPageSelector.Select(eventData, state, Rm2kEventTrigger.AutoStart) != null);
		state.AdvanceTimers(60);
		AssertTrue(state.Timer1Seconds == 1);
		AssertTrue(state.Timer2Seconds == 3);
	}

	public void Test_EventSchedulerRunsAutorunInterpreter()
	{
		var state = new GameSimulationState();
		var eventData = new Rm2kMap.Event(9, 0, 0);
		var page = new Rm2kMap.EventPage { Trigger = (int)Rm2kEventTrigger.AutoStart };
		page.Commands.Add(new Rm2kMap.EventCommand(EventInterpreter.ControlSwitches, new List<int> { EventInterpreter.TargetEvalSingle, 1, 1, EventInterpreter.SwitchModeOn }));
		page.Commands.Add(new Rm2kMap.EventCommand(EventInterpreter.End));
		eventData.Pages.Add(page);
		var scheduler = new Rm2kEventScheduler(state);
		scheduler.SetEvents(new[] { eventData });
		AssertEq(scheduler.EventCount, 1);

		scheduler.ExecuteFrame();
		scheduler.ExecuteFrame();

		AssertTrue(state.Switches.Count >= 1);
		AssertTrue(state.Switches[0]);
	}

	public void Test_EventSchedulerDiagnosesEventCap()
	{
		var state = new GameSimulationState();
		var events = new List<Rm2kMap.Event>();
		for (var index = 0; index < 1001; index++)
		{
			events.Add(new Rm2kMap.Event(index + 1, 0, 0));
		}

		var scheduler = new Rm2kEventScheduler(state);
		scheduler.SetEvents(events);

		AssertEq(scheduler.EventCount, 1000);
		AssertTrue(state.Diagnostics.Any(pEntry => pEntry.Contains(
			"event limit", StringComparison.OrdinalIgnoreCase)),
			"RM2K scheduler diagnoses event truncation at its bounded limit");
	}

	public void Test_EventSchedulerDoesNotFullyEnumerateBeyondEventCap()
	{
		var state = new GameSimulationState();
		var consumed = 0;
		IEnumerable<Rm2kMap.Event> Events()
		{
			for (var index = 0; index < 2000; index++)
			{
				consumed++;
				yield return new Rm2kMap.Event(index + 1, 0, 0);
			}
		}

		var scheduler = new Rm2kEventScheduler(state);
		scheduler.SetEvents(Events());

		AssertEq(scheduler.EventCount, Rm2kEventScheduler.MaxEvents);
		AssertEq(consumed, Rm2kEventScheduler.MaxEvents + 1);
		AssertTrue(state.Diagnostics.Any(pEntry => pEntry.Contains(
			"event limit", StringComparison.OrdinalIgnoreCase)),
			"RM2K scheduler diagnoses bounded source inspection");
	}

	public void Test_EventSchedulerTriggersActionAtPosition()
	{
		var state = new GameSimulationState();
		var eventData = new Rm2kMap.Event(10, 5, 6);
		var page = new Rm2kMap.EventPage { Trigger = (int)Rm2kEventTrigger.Action };
		page.Commands.Add(new Rm2kMap.EventCommand(EventInterpreter.ControlSwitches, new List<int> { EventInterpreter.TargetEvalSingle, 2, 2, EventInterpreter.SwitchModeOn }));
		page.Commands.Add(new Rm2kMap.EventCommand(EventInterpreter.End));
		eventData.Pages.Add(page);
		var scheduler = new Rm2kEventScheduler(state);
		scheduler.SetEvents(new[] { eventData });

		AssertTrue(scheduler.TriggerAt(5, 6, Rm2kEventTrigger.Action));
		scheduler.ExecuteFrame();

		AssertTrue(state.Switches.Count >= 2);
		AssertTrue(state.Switches[1]);
	}

	public void Test_EventInterpreterDiagnosesUnknownCommandWithoutExecutingIt()
	{
		var state = new GameSimulationState();
		var interpreter = new EventInterpreter(state, 77, new[]
		{
			new Rm2kMap.EventCommand(99999),
			new Rm2kMap.EventCommand(EventInterpreter.End),
		});

		interpreter.ExecuteFrame();

		AssertTrue(state.Diagnostics.Count == 1);
		AssertTrue(state.Diagnostics[0].Contains("99999"));
	}

	public void Test_ConstantValuesMatchVerifiedLiblcfCodes()
	{
		AssertEq(EventInterpreter.End, 10);
		AssertEq(EventInterpreter.ShowMessage, 10110);
		AssertEq(EventInterpreter.ShowChoice, 10140);
		AssertEq(EventInterpreter.InputNumber, 10150);
		AssertEq(EventInterpreter.ChangeGold, 10310);
		AssertEq(EventInterpreter.ChangeItems, 10320);
		AssertEq(EventInterpreter.ChangePartyMembers, 10330);
		AssertEq(EventInterpreter.GoldOpAdd, 0);
		AssertEq(EventInterpreter.GoldOpSubtract, 1);
		AssertEq(EventInterpreter.ItemOpAdd, 0);
		AssertEq(EventInterpreter.ItemOpSubtract, 1);
		AssertEq(EventInterpreter.ItemIdConstant, 0);
		AssertEq(EventInterpreter.ItemIdVariable, 1);
		AssertEq(EventInterpreter.ControlSwitches, 10210);
		AssertEq(EventInterpreter.ControlVars, 10220);
		AssertEq(EventInterpreter.Teleport, 10810);
		AssertEq(EventInterpreter.Wait, 11410);
		AssertEq(EventInterpreter.ConditionalBranch, 12010);
		AssertEq(EventInterpreter.Loop, 12210);
		AssertEq(EventInterpreter.BreakLoop, 12220);
		AssertEq(EventInterpreter.Comment, 12410);
		AssertEq(EventInterpreter.ShowMessage2, 20110);
		AssertEq(EventInterpreter.ElseBranch, 22010);
		AssertEq(EventInterpreter.EndBranch, 22011);
		AssertEq(EventInterpreter.EndLoop, 22210);
		AssertEq(EventInterpreter.Comment2, 22410);
	}

	public void Test_LiblcfEndCommandStopsInterpreterWithoutDiagnostic()
	{
		var state = new GameSimulationState();
		var commands = new List<Rm2kMap.EventCommand>
		{
			new(EventInterpreter.ShowMessage)
			{
				Text = "before end",
			},
			new(EventInterpreter.End),
			new(EventInterpreter.ShowMessage)
			{
				Text = "after end",
			},
		};

		var interpreter = new EventInterpreter(state, 1, commands);
		var frames = 0;
		var unsupported = 0;
		while (interpreter.ExecuteFrame() && frames < 10)
		{
			frames++;
			foreach (var diagnostic in state.Diagnostics)
			{
				if (diagnostic.Contains("Unsupported"))
				{
					unsupported++;
				}
			}
		}

		AssertFalse(interpreter.IsRunning, "liblcf END command stops the interpreter");
		AssertEq(unsupported, 0, "END command is not reported as unsupported");
		AssertEq(interpreter.CurrentCommandIndex, 1, "interpreter stopped on the END command");
	}

	public void Test_ShowMessageRecordedWithContinuationLines()
	{
		var state = new GameSimulationState();
		var commands = new List<Rm2kMap.EventCommand>
		{
			new Rm2kMap.EventCommand(EventInterpreter.ShowMessage, null, "Hello"),
			new Rm2kMap.EventCommand(EventInterpreter.ShowMessage2, null, "World"),
			new Rm2kMap.EventCommand(EventInterpreter.End),
		};

		var interpreter = new EventInterpreter(state, 1, commands);

		var result = interpreter.ExecuteFrame();
		AssertTrue(result);
		AssertTrue(state.Diagnostics.Count > 0);
		AssertTrue(state.Diagnostics[0].Contains("Hello\\nWorld"), "message contains both lines");

		result = interpreter.ExecuteFrame();
		AssertFalse(result);
	}

	public void Test_ShowMessageUpdatesPresentationState()
	{
		var state = new GameSimulationState();
		var presentation = new UniversalRPG.Rm2k.Presentation.PresentationState();
		var commands = new List<Rm2kMap.EventCommand>
		{
			new Rm2kMap.EventCommand(EventInterpreter.ShowMessage, null, "Presented"),
			new Rm2kMap.EventCommand(EventInterpreter.End),
		};

		var interpreter = new EventInterpreter(state, 2, commands, presentation);
		AssertTrue(interpreter.ExecuteFrame());
		AssertTrue(presentation.MessageVisible);
		AssertEq(presentation.MessageText, "Presented");
	}

	public void Test_ShowChoicePausesUntilSelection()
	{
		var state = new GameSimulationState();
		var presentation = new UniversalRPG.Rm2k.Presentation.PresentationState();
		var commands = new List<Rm2kMap.EventCommand>
		{
			new Rm2kMap.EventCommand(EventInterpreter.ShowChoice, null, "Yes\nNo"),
			new Rm2kMap.EventCommand(EventInterpreter.End),
		};
		var interpreter = new EventInterpreter(state, 3, commands, presentation);

		AssertTrue(interpreter.ExecuteFrame());
		AssertTrue(presentation.ActiveChoice != null);
		AssertEq(interpreter.CurrentCommandIndex, 0);
		AssertTrue(presentation.SelectChoice(1));
		AssertTrue(interpreter.ExecuteFrame());
		AssertEq(interpreter.CurrentCommandIndex, 1);
		AssertTrue(presentation.ActiveChoice == null, "choice state is cleared after confirmation");
	}

	public void Test_InputNumberPausesThenStoresSubmittedValue()
	{
		var state = new GameSimulationState();
		var presentation = new UniversalRPG.Rm2k.Presentation.PresentationState();
		var commands = new List<Rm2kMap.EventCommand>
		{
			new Rm2kMap.EventCommand(EventInterpreter.InputNumber, new List<int> { 4 }),
			new Rm2kMap.EventCommand(EventInterpreter.End),
		};
		var interpreter = new EventInterpreter(state, 4, commands, presentation);

		AssertTrue(interpreter.ExecuteFrame());
		AssertEq(interpreter.CurrentCommandIndex, 0);
		AssertTrue(presentation.SetInputValue(123));
		AssertTrue(interpreter.ExecuteFrame());
		AssertEq(state.Variables[3], 123);
		AssertEq(interpreter.CurrentCommandIndex, 1);
	}

	public void Test_InputNumberDoesNotConsumePendingValueForDifferentVariable()
	{
		var state = new GameSimulationState();
		var presentation = new UniversalRPG.Rm2k.Presentation.PresentationState();
		AssertTrue(presentation.BeginInput(4));
		AssertTrue(presentation.SetInputValue(123));
		var commands = new List<Rm2kMap.EventCommand>
		{
			new Rm2kMap.EventCommand(EventInterpreter.InputNumber, new List<int> { 5 }),
			new Rm2kMap.EventCommand(EventInterpreter.End),
		};
		var interpreter = new EventInterpreter(state, 5, commands, presentation);

		AssertTrue(interpreter.ExecuteFrame());
		AssertEq(state.Variables.Count, 0, "conflicting input is not written to another variable");
		AssertEq(presentation.PendingInputVariableId, 4, "original pending variable is preserved");
		AssertEq(presentation.InputValue, 123, "original pending value is preserved");
	}

	public void Test_ChangePartyMembersAddsConstantActor()
	{
		var state = new GameSimulationState();
		var interpreter = new EventInterpreter(state, 14, new[]
		{
			new Rm2kMap.EventCommand(EventInterpreter.ChangePartyMembers, new List<int> { EventInterpreter.PartyOpAdd, EventInterpreter.ActorIdConstant, 3 }),
			new Rm2kMap.EventCommand(EventInterpreter.End),
		});

		AssertTrue(interpreter.ExecuteFrame());
		AssertEq(state.PartyMemberIds.Count, 1, "ChangePartyMembers adds one actor");
		AssertEq(state.PartyMemberIds[0], 3, "ChangePartyMembers stores the actor id");
	}

	public void Test_ChangePartyMembersRemovesVariableActor()
	{
		var state = new GameSimulationState();
		state.PartyMemberIds.Add(3);
		state.PartyMemberIds.Add(7);
		state.Variables.Add(7);
		var interpreter = new EventInterpreter(state, 15, new[]
		{
			new Rm2kMap.EventCommand(EventInterpreter.ChangePartyMembers, new List<int> { EventInterpreter.PartyOpRemove, EventInterpreter.ActorIdVariable, 1 }),
			new Rm2kMap.EventCommand(EventInterpreter.End),
		});

		AssertTrue(interpreter.ExecuteFrame());
		AssertEq(state.PartyMemberIds.Count, 1, "ChangePartyMembers removes one actor");
		AssertEq(state.PartyMemberIds[0], 3, "ChangePartyMembers keeps remaining actor");
	}

	public void Test_ChangePartyMembersRejectsDuplicateAndInvalidActor()
	{
		var state = new GameSimulationState();
		state.PartyMemberIds.Add(3);
		var duplicate = new EventInterpreter(state, 16, new[]
		{
			new Rm2kMap.EventCommand(EventInterpreter.ChangePartyMembers, new List<int> { EventInterpreter.PartyOpAdd, EventInterpreter.ActorIdConstant, 3 }),
			new Rm2kMap.EventCommand(EventInterpreter.End),
		});
		AssertTrue(duplicate.ExecuteFrame());
		AssertEq(state.PartyMemberIds.Count, 1, "duplicate actor is not added");

		var invalid = new EventInterpreter(state, 17, new[]
		{
			new Rm2kMap.EventCommand(EventInterpreter.ChangePartyMembers, new List<int> { EventInterpreter.PartyOpAdd, EventInterpreter.ActorIdConstant, 0 }),
			new Rm2kMap.EventCommand(EventInterpreter.End),
		});
		AssertTrue(invalid.ExecuteFrame());
		AssertEq(state.PartyMemberIds.Count, 1, "invalid actor does not mutate party");
		AssertTrue(state.Diagnostics.Count >= 2, "party failures are diagnosed");
	}

	public void Test_ChangeItemsAddsConstantItemCount()
	{
		var state = new GameSimulationState();
		var interpreter = new EventInterpreter(state, 11, new[]
		{
			new Rm2kMap.EventCommand(EventInterpreter.ChangeItems, new List<int>
			{
				EventInterpreter.ItemOpAdd, EventInterpreter.ItemIdConstant, 7,
				EventInterpreter.VarOperandConstant, 3
			}),
			new Rm2kMap.EventCommand(EventInterpreter.End),
		});

		AssertTrue(interpreter.ExecuteFrame());
		AssertEq(state.ItemCounts[7], 3, "ChangeItems adds a constant item count");
	}

	public void Test_ChangeItemsSubtractsAndReadsVariableOperands()
	{
		var state = new GameSimulationState();
		state.Variables.Add(7);
		state.Variables.Add(2);
		state.ItemCounts[7] = 5;
		var interpreter = new EventInterpreter(state, 12, new[]
		{
			new Rm2kMap.EventCommand(EventInterpreter.ChangeItems, new List<int>
			{
				EventInterpreter.ItemOpSubtract, EventInterpreter.ItemIdVariable, 1,
				EventInterpreter.VarOperandVariable, 2
			}),
			new Rm2kMap.EventCommand(EventInterpreter.End),
		});

		AssertTrue(interpreter.ExecuteFrame());
		AssertEq(state.ItemCounts[7], 3, "ChangeItems subtracts a variable amount from a variable item");
	}

	public void Test_ChangeItemsClampsAndRejectsInvalidOperation()
	{
		var state = new GameSimulationState();
		state.ItemCounts[7] = 1;
		var interpreter = new EventInterpreter(state, 13, new[]
		{
			new Rm2kMap.EventCommand(EventInterpreter.ChangeItems, new List<int>
			{
				EventInterpreter.ItemOpSubtract, EventInterpreter.ItemIdConstant, 7,
				EventInterpreter.VarOperandConstant, 10
			}),
			new Rm2kMap.EventCommand(EventInterpreter.ChangeItems, new List<int> { 2, EventInterpreter.ItemIdConstant, 7, EventInterpreter.VarOperandConstant, 5 }),
			new Rm2kMap.EventCommand(EventInterpreter.End),
		});

		AssertTrue(interpreter.ExecuteFrame());
		AssertTrue(interpreter.ExecuteFrame());
		AssertEq(state.ItemCounts[7], 0, "ChangeItems clamps subtraction below zero and ignores invalid operation");
		AssertTrue(state.Diagnostics.Count > 0, "invalid ChangeItems operation is diagnosed");
	}

	public void Test_ChangeGoldAddsConstantOperand()
	{
		var state = new GameSimulationState { Gold = 100 };
		var interpreter = new EventInterpreter(state, 6, new[]
		{
			new Rm2kMap.EventCommand(EventInterpreter.ChangeGold, new List<int> { EventInterpreter.GoldOpAdd, EventInterpreter.VarOperandConstant, 25 }),
			new Rm2kMap.EventCommand(EventInterpreter.End),
		});

		AssertTrue(interpreter.ExecuteFrame());
		AssertEq(state.Gold, 125, "ChangeGold adds a constant operand");
	}

	public void Test_ChangeGoldClampsToBoundedRange()
	{
		var state = new GameSimulationState { Gold = EventInterpreter.MaxGold - 1 };
		var interpreter = new EventInterpreter(state, 7, new[]
		{
			new Rm2kMap.EventCommand(EventInterpreter.ChangeGold, new List<int> { EventInterpreter.GoldOpAdd, EventInterpreter.VarOperandConstant, 100 }),
			new Rm2kMap.EventCommand(EventInterpreter.End),
		});

		AssertTrue(interpreter.ExecuteFrame());
		AssertEq(state.Gold, EventInterpreter.MaxGold, "ChangeGold clamps above the RM2K gold limit");
	}

	public void Test_ChangeGoldSubtractsAndClampsBelowZero()
	{
		var state = new GameSimulationState { Gold = 100 };
		var interpreter = new EventInterpreter(state, 8, new[]
		{
			new Rm2kMap.EventCommand(EventInterpreter.ChangeGold, new List<int> { EventInterpreter.GoldOpSubtract, EventInterpreter.VarOperandConstant, 25 }),
			new Rm2kMap.EventCommand(EventInterpreter.ChangeGold, new List<int> { EventInterpreter.GoldOpSubtract, EventInterpreter.VarOperandConstant, 200 }),
			new Rm2kMap.EventCommand(EventInterpreter.End),
		});

		AssertTrue(interpreter.ExecuteFrame());
		AssertTrue(interpreter.ExecuteFrame());
		AssertEq(state.Gold, 0, "ChangeGold clamps subtraction below zero");
	}

	public void Test_ChangeGoldReadsVariableOperand()
	{
		var state = new GameSimulationState { Gold = 100 };
		state.Variables.Add(40);
		var interpreter = new EventInterpreter(state, 9, new[]
		{
			new Rm2kMap.EventCommand(EventInterpreter.ChangeGold, new List<int> { EventInterpreter.GoldOpSubtract, EventInterpreter.VarOperandVariable, 1 }),
			new Rm2kMap.EventCommand(EventInterpreter.End),
		});

		AssertTrue(interpreter.ExecuteFrame());
		AssertEq(state.Gold, 60, "ChangeGold reads the selected variable operand");
	}

	public void Test_ChangeGoldRejectsInvalidParametersFailClosed()
	{
		var state = new GameSimulationState { Gold = 100 };
		var interpreter = new EventInterpreter(state, 10, new[]
		{
			new Rm2kMap.EventCommand(EventInterpreter.ChangeGold, new List<int> { 2, EventInterpreter.VarOperandConstant, 50 }),
			new Rm2kMap.EventCommand(EventInterpreter.End),
		});

		AssertTrue(interpreter.ExecuteFrame());
		AssertEq(state.Gold, 100, "unsupported ChangeGold operation does not mutate state");
		AssertTrue(state.Diagnostics.Count > 0, "unsupported ChangeGold operation is diagnosed");
	}

	public void Test_MalformedChoiceIsSkippedSafely()
	{
		var state = new GameSimulationState();
		var presentation = new UniversalRPG.Rm2k.Presentation.PresentationState();
		var commands = new List<Rm2kMap.EventCommand>
		{
			new Rm2kMap.EventCommand(EventInterpreter.ShowChoice, null, ""),
			new Rm2kMap.EventCommand(EventInterpreter.End),
		};
		var interpreter = new EventInterpreter(state, 5, commands, presentation);

		AssertTrue(interpreter.ExecuteFrame());
		AssertEq(interpreter.CurrentCommandIndex, 1);
		AssertTrue(state.Diagnostics.Count > 0);
	}

	public void Test_WaitConvertsTenthsToFrames()
	{
		var state = new GameSimulationState();
		var commands = new List<Rm2kMap.EventCommand>
		{
			new Rm2kMap.EventCommand(EventInterpreter.Wait, new List<int> { 5 }),
			new Rm2kMap.EventCommand(EventInterpreter.Wait, new List<int> { 0 }),
			new Rm2kMap.EventCommand(EventInterpreter.End),
		};

		var interpreter = new EventInterpreter(state, 1, commands);

		interpreter.ExecuteFrame();
		AssertEq(interpreter.WaitFramesRemaining, 30, "5 tenths -> 30 frames");

		var frames = 1;
		while (interpreter.WaitFramesRemaining > 0 && frames <= 40)
		{
			interpreter.ExecuteFrame();
			frames++;
		}
		AssertEq(interpreter.WaitFramesRemaining, 0, "wait fully consumed");
		AssertTrue(state.Diagnostics[0].Contains("Wait 30 frames"));

		interpreter.ExecuteFrame();
		AssertEq(interpreter.WaitFramesRemaining, 1, "zero tenths still waits one frame");
	}

	public void Test_ControlSwitchesOnOffFlip()
	{
		var state = new GameSimulationState();
		var commands = new List<Rm2kMap.EventCommand>
		{
			SwitchCmd(2, 4, EventInterpreter.SwitchModeOn),
			SwitchCmd(3, 3, EventInterpreter.SwitchModeOff),
			SwitchCmd(3, 3, EventInterpreter.SwitchModeFlip),
			new Rm2kMap.EventCommand(EventInterpreter.End),
		};

		var interpreter = new EventInterpreter(state, 7, commands);

		AssertTrue(interpreter.ExecuteFrame());
		AssertEq(state.Switches.Count, 4, "switch array padded to range end");
		AssertFalse(state.Switches[0], "switch 1 untouched default");
		AssertTrue(state.Switches[1], "switch 2 ON");
		AssertTrue(state.Switches[2], "switch 3 ON");
		AssertTrue(state.Switches[3], "switch 4 ON");

		AssertTrue(interpreter.ExecuteFrame());
		AssertFalse(state.Switches[2], "switch 3 OFF after second command");
		AssertTrue(state.Switches[3], "switch 4 still ON");

		AssertTrue(interpreter.ExecuteFrame());
		AssertTrue(state.Switches[2], "switch 3 flipped back ON");

		AssertTrue(state.Diagnostics[0].Contains("Switches 2-4 -> ON"));
	}

	public void Test_ControlSwitchesRejectsInvalidRangeAndMode()
	{
		var state = new GameSimulationState();
		var commands = new List<Rm2kMap.EventCommand>
		{
			SwitchCmd(5, 2, EventInterpreter.SwitchModeOn),
			SwitchCmd(1, 2, 9),
			new Rm2kMap.EventCommand(EventInterpreter.ControlSwitches, new List<int> { 1 }),
			new Rm2kMap.EventCommand(EventInterpreter.End),
		};

		var interpreter = new EventInterpreter(state, 1, commands);

		for (var index = 0; index < 3; index++)
		{
			AssertTrue(interpreter.ExecuteFrame(), $"frame {index} skipped safely");
		}
		AssertEq(state.Switches.Count, 0, "no switch changes from invalid commands");
		AssertEq(state.Diagnostics.Count, 3, "one diagnostic per rejected command");
	}

	public void Test_ControlVarsConstantOperations()
	{
		var state = new GameSimulationState();
		state.Variables.Add(10);
		var commands = new List<Rm2kMap.EventCommand>
		{
			VarOp(1, EventInterpreter.VarOpAdd, 5),   // 15
			VarOp(1, EventInterpreter.VarOpSub, 20),  // -5
			VarOp(1, EventInterpreter.VarOpMul, 4),   // -20
			VarOp(1, EventInterpreter.VarOpDiv, 6),   // -3
			VarOp(1, EventInterpreter.VarOpMod, 2),   // -1
			VarOp(1, EventInterpreter.VarOpSet, 42),
			new Rm2kMap.EventCommand(EventInterpreter.End),
		};

		var interpreter = new EventInterpreter(state, 1, commands);

		for (var index = 0; index < commands.Count - 1; index++)
		{
			AssertTrue(interpreter.ExecuteFrame(), $"frame {index} executed");
		}
		AssertEq(state.Variables[0], 42, "final variable value after ops");
	}

	public void Test_ControlVarsVariableOperandReadsOtherVariable()
	{
		var state = new GameSimulationState();
		state.Variables.Add(7);
		state.Variables.Add(30);
		var commands = new List<Rm2kMap.EventCommand>
		{
			new Rm2kMap.EventCommand(
				EventInterpreter.ControlVars,
				new List<int>
				{
					EventInterpreter.TargetEvalSingle, 1, 1, EventInterpreter.VarOpMul,
					EventInterpreter.VarOperandVariable, 2, 0,
				}),
			new Rm2kMap.EventCommand(EventInterpreter.End),
		};

		var interpreter = new EventInterpreter(state, 1, commands);

		AssertTrue(interpreter.ExecuteFrame());
		AssertEq(state.Variables[0], 210, "var1 = var1 * var2 (operand type 1)");
	}

	public void Test_ControlVarsRejectsUnsupportedModes()
	{
		var state = new GameSimulationState();
		state.Variables.Add(11);
		var commands = new List<Rm2kMap.EventCommand>
		{
			new Rm2kMap.EventCommand(EventInterpreter.ControlVars,
				new List<int> { EventInterpreter.TargetEvalIndirectSingle, 1, 1, EventInterpreter.VarOpSet, 0, 5, 0 }), // indirect target mode
			new Rm2kMap.EventCommand(EventInterpreter.ControlVars,
				new List<int> { EventInterpreter.TargetEvalSingle, 1, 1, 9, EventInterpreter.VarOperandConstant, 5, 0 }), // unsupported op
			new Rm2kMap.EventCommand(EventInterpreter.End),
		};

		var interpreter = new EventInterpreter(state, 1, commands);

		AssertTrue(interpreter.ExecuteFrame());
		AssertEq(state.Variables[0], 11, "indirect target mode not applied yet");
		AssertTrue(state.Diagnostics[^1].Contains("target mode"));

		AssertTrue(interpreter.ExecuteFrame());
		AssertEq(state.Variables[0], 11, "unsupported operation not applied");
		AssertTrue(state.Diagnostics[^1].Contains("operation"));
	}

	public void Test_ControlSwitchesAndVarsUseVerifiedParameterLayout()
	{
		var state = new GameSimulationState();
		var commands = new List<Rm2kMap.EventCommand>
		{
			// Real RM2K/2003 layout: [targetMode, start, end, mode].
			new Rm2kMap.EventCommand(EventInterpreter.ControlSwitches,
				new List<int> { EventInterpreter.TargetEvalSingle, 3, 0, EventInterpreter.SwitchModeOn }),
			// [targetMode, start, end, op, operandMode, operand, bitfield].
			new Rm2kMap.EventCommand(EventInterpreter.ControlVars,
				new List<int>
				{
					EventInterpreter.TargetEvalSingle, 1, 1, EventInterpreter.VarOpSet,
					EventInterpreter.VarOperandConstant, 5, 0,
				}),
			new Rm2kMap.EventCommand(EventInterpreter.End),
		};

		var interpreter = new EventInterpreter(state, 1, commands);
		AssertTrue(interpreter.ExecuteFrame());
		AssertTrue(state.Switches[2], "single-target switch command sets switch 3");

		AssertTrue(interpreter.ExecuteFrame());
		AssertEq(state.Variables[0], 5, "single-target variable command sets variable 1");
		foreach (var diagnostic in state.Diagnostics)
		{
			AssertFalse(diagnostic.Contains("unsupported") || diagnostic.Contains("invalid"),
				$"verified layout must not be rejected: {diagnostic}");
		}
	}

	public void Test_ControlVarsRangeTargetWritesEveryVariableInRange()
	{
		var state = new GameSimulationState();
		var commands = new List<Rm2kMap.EventCommand>
		{
			new Rm2kMap.EventCommand(EventInterpreter.ControlVars,
				new List<int>
				{
					EventInterpreter.TargetEvalRange, 2, 4, EventInterpreter.VarOpSet,
					EventInterpreter.VarOperandConstant, 7, 0,
				}),
			new Rm2kMap.EventCommand(EventInterpreter.End),
		};

		var interpreter = new EventInterpreter(state, 1, commands);
		AssertTrue(interpreter.ExecuteFrame());
		AssertEq(state.Variables.Count, 4, "range target padded to the range end");
		AssertEq(state.Variables[0], 0, "variable 1 untouched");
		AssertEq(state.Variables[1], 7, "variable 2 set");
		AssertEq(state.Variables[2], 7, "variable 3 set");
		AssertTrue(state.Diagnostics[0].Contains("Variables 2-4"), "range diagnostic reports both ids");	}

	public void Test_ControlVarsIndirectOperandReadsVariableOfVariable()
	{
		var state = new GameSimulationState();
		state.Variables.Add(3);   // v1 = 3
		state.Variables.Add(3);   // v2 = 3 -> points at v3
		state.Variables.Add(99);  // v3 = 99
		var commands = new List<Rm2kMap.EventCommand>
		{
			// v1 = v[v[2]] with operand mode 2.
			new Rm2kMap.EventCommand(EventInterpreter.ControlVars,
				new List<int>
				{
					EventInterpreter.TargetEvalSingle, 1, 1, EventInterpreter.VarOpSet,
					EventInterpreter.VarOperandVariableIndirect, 2, 0,
				}),
			new Rm2kMap.EventCommand(EventInterpreter.End),
		};

		var interpreter = new EventInterpreter(state, 1, commands);
		AssertTrue(interpreter.ExecuteFrame());
		AssertEq(state.Variables[0], 99, "indirect operand resolves v[v[2]]");
	}

	public void Test_ControlSwitchesAndVarsRejectPatchOnlyTargetModes()
	{
		var state = new GameSimulationState();
		var commands = new List<Rm2kMap.EventCommand>
		{
			new Rm2kMap.EventCommand(EventInterpreter.ControlSwitches,
				new List<int> { EventInterpreter.TargetEvalIndirectSingle, 1, 1, EventInterpreter.SwitchModeOn }),
			new Rm2kMap.EventCommand(EventInterpreter.ControlVars,
				new List<int>
				{
					EventInterpreter.TargetEvalExpression, 1, 1, EventInterpreter.VarOpSet,
					EventInterpreter.VarOperandConstant, 5, 0,
				}),
			new Rm2kMap.EventCommand(EventInterpreter.End),
		};

		var interpreter = new EventInterpreter(state, 1, commands);
		AssertTrue(interpreter.ExecuteFrame());
		AssertTrue(state.Diagnostics[^1].Contains("target mode"));
		AssertTrue(interpreter.ExecuteFrame());
		AssertTrue(state.Diagnostics[^1].Contains("target mode"));
		AssertEq(state.Variables.Count, 0, "no variable written for patch-only target modes");
	}

	public void Test_ChangeLevelAdjustsSelectedActorsAndClamps()
	{
		var state = new GameSimulationState();
		state.PartyMemberIds.Add(1);
		state.PartyMemberIds.Add(2);
		state.SetActorLevel(1, 10);
		state.SetActorLevel(2, 50);
		var commands = new List<Rm2kMap.EventCommand>
		{
			// Party-wide add 5.
			ActorStatCmd(EventInterpreter.ChangeLevel, EventInterpreter.ActorSelectParty, 0,
				EventInterpreter.ActorValueAdd, EventInterpreter.VarOperandConstant, 5),
			new Rm2kMap.EventCommand(EventInterpreter.End),
		};

		var interpreter = new EventInterpreter(state, 1, commands);
		AssertTrue(interpreter.ExecuteFrame());
		AssertEq(state.GetActorLevel(1), 15, "actor 1 gained five levels");
		AssertEq(state.GetActorLevel(2), 55, "actor 2 gained five levels");
	}

	public void Test_ChangeLevelSubtractsAndClampsToLevelBounds()
	{
		var state = new GameSimulationState();
		state.SetActorLevel(3, 2);
		var commands = new List<Rm2kMap.EventCommand>
		{
			// Single actor 3, subtract 10 -> clamped to the minimum level.
			ActorStatCmd(EventInterpreter.ChangeLevel, EventInterpreter.ActorSelectHero, 3,
				EventInterpreter.ActorValueSubtract, EventInterpreter.VarOperandConstant, 10),
			new Rm2kMap.EventCommand(EventInterpreter.End),
		};

		var interpreter = new EventInterpreter(state, 1, commands);
		AssertTrue(interpreter.ExecuteFrame());
		AssertEq(state.GetActorLevel(3), GameSimulationState.MinActorLevel, "level clamped to the minimum");
	}

	public void Test_ChangeLevelReadsVariableOperandAndVariableActorId()
	{
		var state = new GameSimulationState();
		state.Variables.Add(4);   // v1 = 4 -> level delta
		state.Variables.Add(7);   // v2 = 7 -> actor id
		state.SetActorLevel(7, 20);
		var commands = new List<Rm2kMap.EventCommand>
		{
			ActorStatCmd(EventInterpreter.ChangeLevel, EventInterpreter.ActorSelectVariableHero, 2,
				EventInterpreter.ActorValueAdd, EventInterpreter.VarOperandVariable, 1),
			new Rm2kMap.EventCommand(EventInterpreter.End),
		};

		var interpreter = new EventInterpreter(state, 1, commands);
		AssertTrue(interpreter.ExecuteFrame());
		AssertEq(state.GetActorLevel(7), 24, "actor id and delta both read from variables");
	}

	public void Test_ChangeExpClampsToExpBounds()
	{
		var state = new GameSimulationState();
		state.PartyMemberIds.Add(1);
		state.SetActorExp(1, 10);
		var commands = new List<Rm2kMap.EventCommand>
		{
			ActorStatCmd(EventInterpreter.ChangeExp, EventInterpreter.ActorSelectParty, 0,
				EventInterpreter.ActorValueSubtract, EventInterpreter.VarOperandConstant, 50),
			new Rm2kMap.EventCommand(EventInterpreter.End),
		};

		var interpreter = new EventInterpreter(state, 1, commands);
		AssertTrue(interpreter.ExecuteFrame());
		AssertEq(state.GetActorExp(1), 0, "exp clamped at zero");
	}

	public void Test_ChangeLevelAndExpRejectInvalidModesFailClosed()
	{
		var state = new GameSimulationState();
		state.PartyMemberIds.Add(1);
		state.SetActorLevel(1, 10);
		var commands = new List<Rm2kMap.EventCommand>
		{
			// Unsupported actor mode.
			ActorStatCmd(EventInterpreter.ChangeLevel, 5, 0,
				EventInterpreter.ActorValueAdd, EventInterpreter.VarOperandConstant, 1),
			// Unsupported operation.
			ActorStatCmd(EventInterpreter.ChangeLevel, EventInterpreter.ActorSelectParty, 0,
				7, EventInterpreter.VarOperandConstant, 1),
			// Unsupported operand mode.
			ActorStatCmd(EventInterpreter.ChangeExp, EventInterpreter.ActorSelectParty, 0,
				EventInterpreter.ActorValueAdd, 9, 1),
			// Invalid actor id.
			ActorStatCmd(EventInterpreter.ChangeLevel, EventInterpreter.ActorSelectHero, 0,
				EventInterpreter.ActorValueAdd, EventInterpreter.VarOperandConstant, 1),
			// Too few parameters.
			new Rm2kMap.EventCommand(EventInterpreter.ChangeLevel, new List<int> { 0, 0, 0 }),
			new Rm2kMap.EventCommand(EventInterpreter.End),
		};

		var interpreter = new EventInterpreter(state, 1, commands);
		for (var index = 0; index < 5; index++)
		{
			AssertTrue(interpreter.ExecuteFrame(), $"frame {index} skipped safely");
		}
		AssertEq(state.GetActorLevel(1), 10, "level unchanged by rejected commands");
		AssertEq(state.Diagnostics.Count, 5, "one diagnostic per rejected command");
	}

	public void Test_ChangeHeroNameStoresBoundedActorName()
	{
		var state = new GameSimulationState();
		var commands = new List<Rm2kMap.EventCommand>
		{
			new Rm2kMap.EventCommand(EventInterpreter.ChangeHeroName, new List<int> { 4 })
			{
				Text = "Aldo",
			},
			new Rm2kMap.EventCommand(EventInterpreter.End),
		};

		var interpreter = new EventInterpreter(state, 1, commands);
		AssertTrue(interpreter.ExecuteFrame());
		AssertEq(state.GetActorName(4), "Aldo", "actor name stored");
	}

	public void Test_ChangeHeroNameRejectsInvalidActorId()
	{
		var state = new GameSimulationState();
		var commands = new List<Rm2kMap.EventCommand>
		{
			new Rm2kMap.EventCommand(EventInterpreter.ChangeHeroName, new List<int> { 0 }) { Text = "Nobody" },
			new Rm2kMap.EventCommand(EventInterpreter.End),
		};

		var interpreter = new EventInterpreter(state, 1, commands);
		AssertTrue(interpreter.ExecuteFrame());
		AssertEq(state.ActorState.Count, 0, "no actor state created for an invalid id");
		AssertTrue(state.Diagnostics[^1].Contains("invalid actor id"));
	}

	public void Test_EndEventProcessingStopsInterpreterImmediately()
	{
		var state = new GameSimulationState();
		var commands = new List<Rm2kMap.EventCommand>
		{
			new Rm2kMap.EventCommand(EventInterpreter.ShowMessage) { Text = "before" },
			new Rm2kMap.EventCommand(EventInterpreter.EndEventProcessing),
			new Rm2kMap.EventCommand(EventInterpreter.ShowMessage) { Text = "after" },
		};

		var interpreter = new EventInterpreter(state, 1, commands);
		AssertTrue(interpreter.ExecuteFrame(), "message frame runs");
		AssertFalse(interpreter.ExecuteFrame(), "EndEventProcessing stops the interpreter");
		AssertFalse(interpreter.IsRunning);
	}

	public void Test_FlashScreenStoresBoundedFlashAndCanWait()
	{
		var state = new GameSimulationState();
		var presentation = new PresentationState();
		var commands = new List<Rm2kMap.EventCommand>
		{
			// [red, green, blue, alpha, tenths, wait]
			new Rm2kMap.EventCommand(EventInterpreter.FlashScreen,
				new List<int> { 255, 128, 0, 200, 10, 1 }),
			new Rm2kMap.EventCommand(EventInterpreter.End),
		};

		var interpreter = new EventInterpreter(state, 1, commands, presentation);
		AssertTrue(interpreter.ExecuteFrame());
		AssertTrue(presentation.IsFlashActive, "flash started");
		AssertEq(presentation.FlashRed, 255);
		AssertEq(presentation.FlashFramesRemaining, 60, "ten tenths equal sixty frames");
		AssertEq(interpreter.WaitFramesRemaining, 60, "wait flag blocks the next commands");

		presentation.Tick(60);
		AssertFalse(presentation.IsFlashActive, "flash ends after its duration");
	}

	public void Test_FlashScreenRejectsOutOfBoundsColorAndDuration()
	{
		var state = new GameSimulationState();
		var presentation = new PresentationState();
		var commands = new List<Rm2kMap.EventCommand>
		{
			new Rm2kMap.EventCommand(EventInterpreter.FlashScreen,
				new List<int> { 999, 0, 0, 0, 1, 0 }),
			new Rm2kMap.EventCommand(EventInterpreter.FlashScreen,
				new List<int> { 0, 0, 0, 0, -5, 0 }),
			new Rm2kMap.EventCommand(EventInterpreter.FlashScreen, new List<int> { 1, 2, 3 }),
			new Rm2kMap.EventCommand(EventInterpreter.End),
		};

		var interpreter = new EventInterpreter(state, 1, commands, presentation);
		for (var index = 0; index < 3; index++)
		{
			AssertTrue(interpreter.ExecuteFrame(), $"frame {index} skipped safely");
		}
		AssertFalse(presentation.IsFlashActive, "no flash started from rejected payloads");
		AssertEq(state.Diagnostics.Count, 3, "one diagnostic per rejected flash");
	}

	public void Test_ShakeScreenEndsOnZeroDuration()
	{
		var state = new GameSimulationState();
		var presentation = new PresentationState();
		var commands = new List<Rm2kMap.EventCommand>
		{
			// [strength, speed, tenths, wait] with zero tenths ends the shake.
			new Rm2kMap.EventCommand(EventInterpreter.ShakeScreen,
				new List<int> { 4, 4, 0, 0 }),
			new Rm2kMap.EventCommand(EventInterpreter.End),
		};

		var interpreter = new EventInterpreter(state, 1, commands, presentation);
		AssertTrue(interpreter.ExecuteFrame());
		AssertFalse(presentation.IsShakeActive, "zero duration ends the shake");
		AssertTrue(state.Diagnostics[^1].Contains("ended"));
	}

	public void Test_ShakeScreenStoresBoundedShake()
	{
		var state = new GameSimulationState();
		var presentation = new PresentationState();
		var commands = new List<Rm2kMap.EventCommand>
		{
			new Rm2kMap.EventCommand(EventInterpreter.ShakeScreen,
				new List<int> { 5, 3, 5, 0 }),
			new Rm2kMap.EventCommand(EventInterpreter.End),
		};

		var interpreter = new EventInterpreter(state, 1, commands, presentation);
		AssertTrue(interpreter.ExecuteFrame());
		AssertTrue(presentation.IsShakeActive, "shake started");
		AssertEq(presentation.ShakeStrength, 5);
		AssertEq(presentation.ShakeFramesRemaining, 30, "five tenths equal thirty frames");
		AssertEq(interpreter.WaitFramesRemaining, 0, "no wait without the wait flag");
	}

	public void Test_WeatherEffectsClampsStrengthAndUnknownType()
	{
		var state = new GameSimulationState();
		var presentation = new PresentationState();
		var commands = new List<Rm2kMap.EventCommand>
		{
			// Strength above 2 is clamped, unknown RM2K types fold to 0.
			new Rm2kMap.EventCommand(EventInterpreter.WeatherEffects, new List<int> { 9, 7 }),
			new Rm2kMap.EventCommand(EventInterpreter.WeatherEffects, new List<int> { 2, 2 }),
			new Rm2kMap.EventCommand(EventInterpreter.WeatherEffects, new List<int> { 1 }),
			new Rm2kMap.EventCommand(EventInterpreter.End),
		};

		var interpreter = new EventInterpreter(state, 1, commands, presentation);
		AssertTrue(interpreter.ExecuteFrame());
		AssertEq(presentation.WeatherType, 0, "unknown type folds to none");
		AssertEq(presentation.WeatherStrength, 2, "strength clamped to the maximum");

		AssertTrue(interpreter.ExecuteFrame());
		AssertEq(presentation.WeatherType, 2, "valid type stored");
		AssertEq(presentation.WeatherStrength, 2);

		AssertTrue(interpreter.ExecuteFrame());
		AssertEq(presentation.WeatherType, 2, "malformed weather command does not change state");
		AssertTrue(state.Diagnostics[^1].Contains("malformed parameters skipped"),
			$"malformed weather command reports: {state.Diagnostics[^1]}");
	}

	public void Test_ScreenEffectsRequirePresentationState()
	{
		var state = new GameSimulationState();
		var commands = new List<Rm2kMap.EventCommand>
		{
			new Rm2kMap.EventCommand(EventInterpreter.FlashScreen,
				new List<int> { 1, 2, 3, 4, 5, 0 }),
			new Rm2kMap.EventCommand(EventInterpreter.End),
		};

		var interpreter = new EventInterpreter(state, 1, commands);
		AssertTrue(interpreter.ExecuteFrame());
		AssertTrue(state.Diagnostics[^1].Contains("presentation state unavailable"));
	}

	public void Test_CallEventRunsNestedCommandsAndReturnsToCaller()
	{
		var state = new GameSimulationState();
		state.Variables.Add(0);
		var nested = new List<Rm2kMap.EventCommand>
		{
			new(EventInterpreter.ControlVars)
			{
				Parameters = new List<int>
				{
					EventInterpreter.TargetEvalSingle, 1, 1, EventInterpreter.VarOpSet,
					EventInterpreter.VarOperandConstant, 42, 0,
				},
			},
			new(EventInterpreter.End),
		};
		var commands = new List<Rm2kMap.EventCommand>
		{
			// Call map event 5, first page.
			new(EventInterpreter.CallEvent, new List<int> { EventInterpreter.CallTargetMapEvent, 5, 1 }),
			new(EventInterpreter.ControlVars)
			{
				Parameters = new List<int>
				{
					EventInterpreter.TargetEvalSingle, 2, 2, EventInterpreter.VarOpSet,
					EventInterpreter.VarOperandConstant, 7, 0,
				},
			},
			new(EventInterpreter.End),
		};

		IReadOnlyList<Rm2kMap.EventCommand>? Resolve(int pEventId, int pPageIndex) =>
			pEventId == 5 && pPageIndex == 1 ? nested : null;
		var interpreter = new EventInterpreter(state, 1, commands, null, Resolve);

		AssertTrue(interpreter.ExecuteFrame(), "call frame starts");
		AssertEq(interpreter.CallDepth, 1, "nested frame pushed");
		AssertTrue(interpreter.ExecuteFrame(), "nested command runs");
		AssertEq(state.Variables[0], 42, "nested command applied");

		AssertTrue(interpreter.ExecuteFrame(), "nested END returns to the caller");
		AssertEq(interpreter.CallDepth, 0, "nested frame popped");
		AssertTrue(interpreter.ExecuteFrame(), "caller continues after the call");
		AssertEq(state.Variables[1], 7, "caller command after the call executed");

		AssertFalse(interpreter.ExecuteFrame(), "base frame ends the event");
		AssertFalse(interpreter.IsRunning);
	}

	public void Test_CallEventRejectsUnsupportedTargetsAndUnknownEvents()
	{
		var state = new GameSimulationState();
		var commands = new List<Rm2kMap.EventCommand>
		{
			// Common events are not decoded yet.
			new(EventInterpreter.CallEvent, new List<int> { EventInterpreter.CallTargetCommonEvent, 1, 1 }),
			// Unknown event id.
			new(EventInterpreter.CallEvent, new List<int> { EventInterpreter.CallTargetMapEvent, 99, 1 }),
			// Too few parameters.
			new(EventInterpreter.CallEvent, new List<int> { EventInterpreter.CallTargetMapEvent, 5 }),
			new(EventInterpreter.End),
		};

		var interpreter = new EventInterpreter(state, 1, commands, null,
			(int pEventId, int pPageIndex) => null);

		AssertTrue(interpreter.ExecuteFrame());
		AssertTrue(state.Diagnostics[^1].Contains("not supported yet"));
		AssertTrue(interpreter.ExecuteFrame());
		AssertTrue(state.Diagnostics[^1].Contains("no commands"));
		AssertTrue(interpreter.ExecuteFrame());
		AssertTrue(state.Diagnostics[^1].Contains("malformed parameters skipped"));
		AssertEq(interpreter.CallDepth, 0, "no nested frame was pushed");
	}

	public void Test_CallEventStopsAtRecursionLimit()
	{
		var state = new GameSimulationState();
		var recursive = new List<Rm2kMap.EventCommand>
		{
			new(EventInterpreter.CallEvent, new List<int> { EventInterpreter.CallTargetMapEvent, 5, 1 }),
			new(EventInterpreter.End),
		};
		var interpreter = new EventInterpreter(state, 1, recursive, null,
			(int pEventId, int pPageIndex) => recursive);

		var frames = 0;
		var maxDepth = 0;
		while (interpreter.CallDepth < EventInterpreter.MaxScriptRecursion
            && interpreter.ExecuteFrame() && frames < EventInterpreter.MaxScriptRecursion + 5)
		{
			frames++;
			maxDepth = Math.Max(maxDepth, interpreter.CallDepth);
		}

		AssertEq(maxDepth, EventInterpreter.MaxScriptRecursion, "recursion is bounded");
		// The diagnostic buffer is intentionally capped, so clear it before the
		// frame that crosses the limit and assert the reported reason.
		state.ClearDiagnostics();
		interpreter.ExecuteFrame();
		AssertTrue(state.Diagnostics.Count == 1 && state.Diagnostics[0].Contains("recursion limit"),
			$"recursion limit is diagnosed: {string.Join(" | ", state.Diagnostics)}");
	}

	public void Test_ChangeEventLocationMovesEventThroughHook()
	{
		var state = new GameSimulationState();
		var commands = new List<Rm2kMap.EventCommand>
		{
			// [eventId, operandMode, x, y]
			new(EventInterpreter.ChangeEventLocation, new List<int> { 4, EventInterpreter.VarOperandConstant, 7, 9 }),
			new(EventInterpreter.End),
		};

		var moved = new System.Collections.Generic.List<(int Id, int X, int Y, int Direction)>();
		bool SetLocation(int pEventId, int pX, int pY, int pDirection)
		{
			moved.Add((pEventId, pX, pY, pDirection));
			return pEventId == 4;
		}
		var interpreter = new EventInterpreter(state, 1, commands, null, null, SetLocation);

		AssertTrue(interpreter.ExecuteFrame());
		AssertEq(moved.Count, 1, "location hook invoked once");
		AssertEq(moved[0].Id, 4);
		AssertEq(moved[0].X, 7);
		AssertEq(moved[0].Y, 9);
		AssertEq(moved[0].Direction, -1, "no direction without the RPG2K3 parameter");
	}

	public void Test_ChangeEventLocationReadsVariableCoordinatesAndRejectsUnknownEvent()
	{
		var state = new GameSimulationState();
		state.Variables.Add(5);   // v1 = 5
		state.Variables.Add(12);  // v2 = 12
		var commands = new List<Rm2kMap.EventCommand>
		{
			new(EventInterpreter.ChangeEventLocation,
				new List<int> { 2, EventInterpreter.VarOperandVariable, 1, 2, 3 }),
			new(EventInterpreter.ChangeEventLocation, new List<int> { 99, 0, 1, 1 }),
			new(EventInterpreter.ChangeEventLocation, new List<int> { 1, 0, 1 }),
			new(EventInterpreter.End),
		};

		var applied = new System.Collections.Generic.List<(int X, int Y, int Direction)>();
		bool SetLocation(int pEventId, int pX, int pY, int pDirection)
		{
			applied.Add((pX, pY, pDirection));
			return pEventId != 99;
		}
		var interpreter = new EventInterpreter(state, 1, commands, null, null, SetLocation);

		AssertTrue(interpreter.ExecuteFrame());
		AssertEq(applied.Count, 1, "variable coordinates resolved");
		AssertEq(applied[0].X, 5);
		AssertEq(applied[0].Y, 12);
		AssertEq(applied[0].Direction, 2, "RPG2K3 direction parameter is one-based");

		AssertTrue(interpreter.ExecuteFrame());
		AssertTrue(state.Diagnostics[^1].Contains("not found"));
		AssertTrue(interpreter.ExecuteFrame());
		AssertTrue(state.Diagnostics[^1].Contains("malformed parameters skipped"));
	}

	public void Test_EraseEventDeactivatesOwningEventAndEndsFrame()
	{
		var state = new GameSimulationState();
		var deactivated = new System.Collections.Generic.List<int>();
		var commands = new List<Rm2kMap.EventCommand>
		{
			new(EventInterpreter.ShowMessage) { Text = "before erase" },
			new(EventInterpreter.EraseEvent),
			new(EventInterpreter.ShowMessage) { Text = "after erase" },
		};

		var interpreter = new EventInterpreter(state, 6, commands, null, null, null,
			pEventId =>
			{
				deactivated.Add(pEventId);
				return true;
			});

		AssertTrue(interpreter.ExecuteFrame(), "message runs before the erase");
		AssertFalse(interpreter.ExecuteFrame(), "erase ends the frame");
		AssertEq(deactivated.Count, 1, "deactivation hook invoked once");
		AssertEq(deactivated[0], 6, "owning event deactivated");
		AssertFalse(interpreter.IsRunning);
	}

	public void Test_EraseEventRejectsParameterizedFormAndUnknownEvent()
	{
		var state = new GameSimulationState();
		var commands = new List<Rm2kMap.EventCommand>
		{
			new(EventInterpreter.EraseEvent, new List<int> { 3, 1, 0 }),
			new(EventInterpreter.EraseEvent),
			new(EventInterpreter.End),
		};

		var interpreter = new EventInterpreter(state, 6, commands, null, null, null,
			pEventId => false);

		AssertTrue(interpreter.ExecuteFrame());
		AssertTrue(state.Diagnostics[^1].Contains("not supported yet"));
		AssertTrue(interpreter.ExecuteFrame());
		AssertTrue(state.Diagnostics[^1].Contains("not found"));
	}

	public void Test_ControlVarsDivisionByZeroSkips()
	{
		var state = new GameSimulationState();
		state.Variables.Add(10);
		var commands = new List<Rm2kMap.EventCommand>
		{
			VarOp(1, EventInterpreter.VarOpDiv, 0),
			new Rm2kMap.EventCommand(EventInterpreter.End),
		};

		var interpreter = new EventInterpreter(state, 1, commands);

		AssertTrue(interpreter.ExecuteFrame());
		AssertEq(state.Variables[0], 10, "value unchanged on division by zero");
		AssertTrue(state.Diagnostics[0].Contains("division by zero"));
	}

	public void Test_TeleportSetsPendingTransfer()
	{
		var state = new GameSimulationState();
		var commands = new List<Rm2kMap.EventCommand>
		{
			new Rm2kMap.EventCommand(EventInterpreter.Teleport, new List<int> { 12, 30, 44 }),
			new Rm2kMap.EventCommand(EventInterpreter.End),
		};

		var interpreter = new EventInterpreter(state, 3, commands);

		AssertTrue(interpreter.ExecuteFrame());
		AssertTrue(state.IsTransferPending, "transfer pending flag");
		AssertEq(state.PendingMapId, 12, "pending map id");
		AssertEq(state.PendingX, 30, "pending x");
		AssertEq(state.PendingY, 44, "pending y");
		AssertTrue(state.Diagnostics[0].Contains("Transfer pending"));
	}

	public void Test_TeleportRejectsInvalidMapIdsWithoutOverwritingPendingState()
	{
		var state = new GameSimulationState();
		state.IsTransferPending = true;
		state.PendingMapId = 12;
		state.PendingX = 30;
		state.PendingY = 44;
		var commands = new List<Rm2kMap.EventCommand>
		{
			new Rm2kMap.EventCommand(EventInterpreter.Teleport, new List<int> { 0, 1, 2 }),
			new Rm2kMap.EventCommand(EventInterpreter.Teleport, new List<int> { GameSimulationState.MaxMapId + 1, 3, 4 }),
			new Rm2kMap.EventCommand(EventInterpreter.End),
		};

		var interpreter = new EventInterpreter(state, 3, commands);

		AssertTrue(interpreter.ExecuteFrame());
		AssertTrue(interpreter.ExecuteFrame());
		AssertEq(state.PendingMapId, 12, "invalid map IDs preserve pending map");
		AssertEq(state.PendingX, 30, "invalid map IDs preserve pending x");
		AssertEq(state.PendingY, 44, "invalid map IDs preserve pending y");
		AssertTrue(state.IsTransferPending, "invalid map IDs preserve pending flag");
		AssertEq(state.Diagnostics.Count, 2, "one diagnostic per invalid map ID");
		AssertFalse(state.Diagnostics[0].Contains("Transfer pending"));
		AssertFalse(state.Diagnostics[1].Contains("Transfer pending"));
	}

	public void Test_TeleportAppliesValidFacingAndRejectsInvalidFacingAtomically()
	{
		var state = new GameSimulationState();
		var commands = new List<Rm2kMap.EventCommand>
		{
			new Rm2kMap.EventCommand(EventInterpreter.Teleport, new List<int> { 12, 30, 44, 6 }),
			new Rm2kMap.EventCommand(EventInterpreter.Teleport, new List<int> { 13, 31, 45, 5 }),
			new Rm2kMap.EventCommand(EventInterpreter.End),
		};

		var interpreter = new EventInterpreter(state, 3, commands);

		AssertTrue(interpreter.ExecuteFrame());
		AssertEq(state.FacingDirection, 6, "valid transfer facing is applied");
		AssertEq(state.PendingMapId, 12, "valid transfer map is pending");
		AssertTrue(interpreter.ExecuteFrame());
		AssertEq(state.FacingDirection, 6, "invalid facing preserves direction");
		AssertEq(state.PendingMapId, 12, "invalid facing preserves pending map");
		AssertEq(state.PendingX, 30, "invalid facing preserves pending x");
		AssertEq(state.PendingY, 44, "invalid facing preserves pending y");
		AssertTrue(state.IsTransferPending, "invalid facing preserves pending flag");
		AssertEq(state.Diagnostics.Count, 2, "valid and invalid transfer diagnostics");
		AssertTrue(state.Diagnostics[0].Contains("Transfer pending"));
		AssertFalse(state.Diagnostics[1].Contains("Transfer pending"));
	}

	public void Test_LoopExecutesUntilBreakThenContinuesPastEndLoop()
	{
		var state = new GameSimulationState();
		var commands = new List<Rm2kMap.EventCommand>
		{
			new Rm2kMap.EventCommand(EventInterpreter.Loop),
			VarOp(1, EventInterpreter.VarOpAdd, 1),
			new Rm2kMap.EventCommand(EventInterpreter.BreakLoop),
			new Rm2kMap.EventCommand(EventInterpreter.EndLoop),
			VarOp(2, EventInterpreter.VarOpSet, 99),
			new Rm2kMap.EventCommand(EventInterpreter.End),
		};

		var interpreter = new EventInterpreter(state, 1, commands);

		// Frame 1: Loop, frame 2: var1 += 1, frame 3: BreakLoop -> past EndLoop.
		AssertTrue(interpreter.ExecuteFrame());
		AssertTrue(interpreter.ExecuteFrame());
		AssertTrue(interpreter.ExecuteFrame());
		AssertEq(state.Variables[0], 1, "loop body ran once before break");
		AssertEq(interpreter.CurrentCommandIndex, 4, "break jumped past EndLoop");

		AssertTrue(interpreter.ExecuteFrame());
		AssertEq(state.Variables[1], 99, "commands after the loop execute");
		AssertFalse(interpreter.ExecuteFrame(), "event ends on End command");
	}

	public void Test_EndLoopsJumpsBackToLoopStart()
	{
		var state = new GameSimulationState();
		var commands = new List<Rm2kMap.EventCommand>
		{
			new Rm2kMap.EventCommand(EventInterpreter.Loop),          // idx 0
			VarOp(1, EventInterpreter.VarOpAdd, 1),                   // idx 1
			new Rm2kMap.EventCommand(EventInterpreter.EndLoop),       // idx 2
		};

		var interpreter = new EventInterpreter(state, 1, commands);

		// Cycle of 2 frames: body command / EndLoop-jump to first body command.
		for (var index = 0; index < 8; index++)
		{
			AssertTrue(interpreter.ExecuteFrame(), $"looping frame {index}");
		}
		AssertEq(state.Variables[0], 4, "four loop iterations after 8 frames");
	}

	public void Test_MalformedParametersSkipsSafely()
	{
		var state = new GameSimulationState();
		var commands = new List<Rm2kMap.EventCommand>
		{
			new Rm2kMap.EventCommand(EventInterpreter.ControlSwitches, new List<int> { 1, 2 }),
			new Rm2kMap.EventCommand(EventInterpreter.ControlVars),
			new Rm2kMap.EventCommand(EventInterpreter.Teleport, new List<int> { 9 }),
			new Rm2kMap.EventCommand(EventInterpreter.End),
		};

		var interpreter = new EventInterpreter(state, 1, commands);

		for (var index = 0; index < 3; index++)
		{
			AssertTrue(interpreter.ExecuteFrame(), $"malformed frame {index} skipped without crash");
		}
		AssertFalse(state.IsTransferPending, "no transfer from malformed payload");
		AssertEq(state.Switches.Count, 0, "no switch changes from malformed payload");
		AssertEq(state.Variables.Count, 0, "no variable changes from malformed payload");
		AssertEq(state.Diagnostics.Count, 3, "one diagnostic per malformed command");
	}

	public void Test_ConditionalBranchSwitchConditions()
	{
		var state = new GameSimulationState();
		var commands = new List<Rm2kMap.EventCommand>
		{
			SwitchCmd(1, 1, EventInterpreter.SwitchModeOn),
			Cmd(EventInterpreter.ConditionalBranch, 0, 1, 0), // switch 1 is ON -> true
			VarOp(10, EventInterpreter.VarOpAdd, 1),
			Cmd(EventInterpreter.ElseBranch),
			VarOp(20, EventInterpreter.VarOpAdd, 1),
			Cmd(EventInterpreter.EndBranch),
			Cmd(EventInterpreter.ConditionalBranch, 0, 2, 1), // switch 2 is OFF -> true
			VarOp(11, EventInterpreter.VarOpAdd, 1),
			Cmd(EventInterpreter.ElseBranch),
			VarOp(21, EventInterpreter.VarOpAdd, 1),
			Cmd(EventInterpreter.EndBranch),
			Cmd(EventInterpreter.End),
		};
		var interpreter = new EventInterpreter(state, 1, commands);

		RunBounded(interpreter, 64);

		AssertEq(state.Variables[9], 1, "switch-ON condition took then-body");
		AssertEq(VarValue(state, 20), 0, "then-body else block skipped (never written)");
		AssertEq(state.Variables[10], 1, "switch-OFF condition took then-body");
		AssertEq(VarValue(state, 21), 0, "second else block skipped (never written)");
		AssertEq(state.Diagnostics.Count, 3, "one diagnostic per executed switch/variable op");
	}

	public void Test_ConditionalBranchFalseConditionRunsElse()
	{
		var state = new GameSimulationState();
		var commands = new List<Rm2kMap.EventCommand>
		{
			Cmd(EventInterpreter.ConditionalBranch, 0, 3, 0), // switch 3 is ON -> false (unset)
			VarOp(10, EventInterpreter.VarOpAdd, 1),
			Cmd(EventInterpreter.ElseBranch),
			VarOp(20, EventInterpreter.VarOpAdd, 1),
			Cmd(EventInterpreter.EndBranch),
			Cmd(EventInterpreter.End),
		};
		var interpreter = new EventInterpreter(state, 1, commands);

		RunBounded(interpreter, 64);

		AssertEq(state.Variables[9], 0, "then-body skipped");
		AssertEq(state.Variables[19], 1, "else body executed");
		AssertEq(state.Diagnostics.Count, 1, "only the executed else-body var op is traced");
	}

	public void Test_ConditionalBranchVariableComparisons()
	{
		var state = new GameSimulationState();
		var commands = new List<Rm2kMap.EventCommand>
		{
			VarOp(5, EventInterpreter.VarOpSet, 10),
			Cmd(EventInterpreter.ConditionalBranch, 1, 5, 0, 10, EventInterpreter.BranchOpEqual),
			VarOp(20, EventInterpreter.VarOpAdd, 1),
			Cmd(EventInterpreter.EndBranch),
			Cmd(EventInterpreter.ConditionalBranch, 1, 5, 0, 11, EventInterpreter.BranchOpGreaterOrEqual),
			VarOp(21, EventInterpreter.VarOpAdd, 1),
			Cmd(EventInterpreter.ElseBranch),
			VarOp(22, EventInterpreter.VarOpAdd, 1),
			Cmd(EventInterpreter.EndBranch),
			Cmd(EventInterpreter.ConditionalBranch, 1, 5, 0, 10, EventInterpreter.BranchOpNotEqual),
			VarOp(23, EventInterpreter.VarOpAdd, 1),
			Cmd(EventInterpreter.ElseBranch),
			VarOp(24, EventInterpreter.VarOpAdd, 1),
			Cmd(EventInterpreter.EndBranch),
			Cmd(EventInterpreter.End),
		};
		var interpreter = new EventInterpreter(state, 1, commands);

		RunBounded(interpreter, 64);

		AssertEq(state.Variables[4], 10, "variable seeded");
		AssertEq(state.Variables[19], 1, "== comparison matched");
		AssertEq(state.Variables[20], 0, ">= false skipped then-body");
		AssertEq(state.Variables[21], 1, ">= false ran else");
		AssertEq(state.Variables[22], 0, "!= false skipped then-body");
		AssertEq(state.Variables[23], 1, "!= false ran else");
	}

	public void Test_ConditionalBranchVariableOperandAndNesting()
	{
		var state = new GameSimulationState();
		var commands = new List<Rm2kMap.EventCommand>
		{
			VarOp(5, EventInterpreter.VarOpSet, 7),
			VarOp(6, EventInterpreter.VarOpSet, 7),
			Cmd(EventInterpreter.ConditionalBranch, 1, 5, 1, 6, EventInterpreter.BranchOpEqual), // var5 == var6
			Cmd(EventInterpreter.ConditionalBranch, 1, 6, 0, 99, EventInterpreter.BranchOpLess), // nested: var6 < 99
			VarOp(30, EventInterpreter.VarOpAdd, 1),
			Cmd(EventInterpreter.EndBranch),
			Cmd(EventInterpreter.ElseBranch), // belongs to OUTER branch
			VarOp(31, EventInterpreter.VarOpAdd, 1),
			Cmd(EventInterpreter.EndBranch),
			Cmd(EventInterpreter.End),
		};
		var interpreter = new EventInterpreter(state, 1, commands);

		RunBounded(interpreter, 64);

		AssertEq(state.Variables[29], 1, "nested then-bodies both ran");
		AssertEq(VarValue(state, 31), 0, "outer else not taken (var never written)");
	}

	public void Test_ConditionalBranchUnsupportedTypeDiagnosed()
	{
		var state = new GameSimulationState();
		var commands = new List<Rm2kMap.EventCommand>
		{
			Cmd(EventInterpreter.ConditionalBranch, 3, 100, 1), // gold condition: unsupported
			VarOp(10, EventInterpreter.VarOpAdd, 1),
			Cmd(EventInterpreter.ElseBranch),
			VarOp(20, EventInterpreter.VarOpAdd, 1),
			Cmd(EventInterpreter.EndBranch),
			Cmd(EventInterpreter.End),
		};
		var interpreter = new EventInterpreter(state, 1, commands);

		RunBounded(interpreter, 64);

		AssertEq(state.Variables[9], 0, "unsupported type skips then-body");
		AssertEq(state.Variables[19], 1, "unsupported type takes else path");
		AssertTrue(
			DiagnosticsMention(state.Diagnostics, "not supported"),
			"diagnostic names the unsupported condition type");
	}

	/// <summary>Reads a variable by 1-based id; never-written variables default to 0 (RM semantics).</summary>
	private static int VarValue(GameSimulationState pState, int pId)
	{
		return pState.Variables.Count >= pId ? pState.Variables[pId - 1] : 0;
	}

	private static bool DiagnosticsMention(Godot.Collections.Array<string> pDiagnostics, string pFragment)
	{
		foreach (var diagnostic in pDiagnostics)
		{
			if (diagnostic.Contains(pFragment, StringComparison.Ordinal))
			{
				return true;
			}
		}
		return false;
	}

	private static void RunBounded(EventInterpreter pInterpreter, int pMaxFrames)
	{
		for (var frame = 0; frame < pMaxFrames && pInterpreter.ExecuteFrame(); frame++)
		{
		}
	}

	private static Rm2kMap.EventCommand Cmd(int pCode, params int[] pParameters)
	{
		return new Rm2kMap.EventCommand(pCode, new List<int>(pParameters));
	}

	private static Rm2kMap.EventCommand ActorStatCmd(
		int pCode,
		int pActorMode,
		int pActorId,
		int pOperation,
		int pOperandMode,
		int pOperand)
	{
		// Verified layout: [actorMode, actorId, operation, operandMode, operand, showMessage].
		return new Rm2kMap.EventCommand(
			pCode,
			new List<int> { pActorMode, pActorId, pOperation, pOperandMode, pOperand, 0 });
	}

	private static Rm2kMap.EventCommand SwitchCmd(int pStart, int pEnd, int pMode)
	{
		// Verified layout: [targetMode, start, end, mode] with range mode 1.
		var targetMode = pStart == pEnd ? EventInterpreter.TargetEvalSingle : EventInterpreter.TargetEvalRange;
		return new Rm2kMap.EventCommand(
			EventInterpreter.ControlSwitches,
			new List<int> { targetMode, pStart, pEnd, pMode });
	}

	private static Rm2kMap.EventCommand VarOp(int pTarget, int pOp, int pOperand)
	{
		// Verified layout: [targetMode, start, end, op, operandMode, operand, bitfield].
		return new Rm2kMap.EventCommand(
			EventInterpreter.ControlVars,
			new List<int>
			{
				EventInterpreter.TargetEvalSingle, pTarget, pTarget, pOp,
				EventInterpreter.VarOperandConstant, pOperand, 0,
			});
	}
}
