using System;
using System.Collections.Generic;

using UniversalRPG.Tests.Framework;
using UniversalRPG.Wolf;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The character board, and the move route running through the event VM.
/// </summary>
/// <remarks>
/// <para>
/// The previous card built a runner that executed one step. <strong>This card
/// puts it on a board, gives the VM two opcodes, and makes the board move in
/// time</strong> — because a runner that can be called is not a game, and a
/// board that does not tick in the VM is a board the VM does not have.
/// </para>
/// <para>
/// The format's four route flags are the shape: how the route is driven,
/// whether the event waits for it, whether an impossible step is skipped, and
/// whether the route repeats.
/// </para>
/// </remarks>
public partial class TestWolfCharacterBoard : TestBase
{
	/// <summary>Normal variable 0, as a reference.</summary>
	private static int Normal0 => WolfVariable.BaseNormal;

	/// <summary>One step of a type, with the given four byte arguments.</summary>
	private static WolfMoveRouteStep Step(byte pType, params int[] pArguments)
	{
		return new WolfMoveRouteStep
		{
			Type = pType,
			Arguments = pArguments,
		};
	}

	/// <summary>A route over the given steps.</summary>
	private static WolfMoveRoute Route(
		IReadOnlyList<WolfMoveRouteStep> pSteps,
		bool pWait = false,
		bool pSkip = false,
		bool pRepeat = false)
	{
		return new WolfMoveRoute
		{
			Steps = pSteps,
			Mode = WolfMoveRouteMode.Custom,
			WaitUntilDone = pWait,
			SkipImpossibleMoves = pSkip,
			RepeatActions = pRepeat,
		};
	}

	// ---- Das Brett

	/// <summary>
	/// The same event id twice is one character and not two.
	/// </summary>
	/// <remarks>
	/// <strong>A map event in two programs is one figure.</strong> A reader that
	/// appended would put two figures on one tile, and the renderer draws by id
	/// — so one of them would be a figure the game can see standing still.
	/// </remarks>
	public void Test_TheSameEventIdIsOneCharacter()
	{
		var bands = new WolfVariableBands();
		var board = new WolfCharacterBoard(bands);
		board.Add(new WolfCharacter { Id = 3, X = 1, Y = 1 });
		board.Add(new WolfCharacter { Id = 3, X = 9, Y = 9 });

		AssertEq(
			board.Find(3)!.X, 9,
			"**and the second placement moved the one figure**, because a map"
			+ " event that appears in two programs refers to one character;"
			+ $" X is {board.Find(3)!.X}");
		AssertEq(
			board.All.Count, 2,
			"**and the board holds the hero and one event**, which is the count"
			+ $" a reader that appended would have got wrong; it is {board.All.Count}");
	}

	/// <summary>
	/// The hero is event id zero, and only event id zero.
	/// </summary>
	/// <remarks>
	/// <strong>Zero is the hero because the format's character list starts with
	/// the hero.</strong> A reader that treated zero as a missing id would have
	/// no hero at all, and every map command would act on an event instead.
	/// </remarks>
	public void Test_TheHeroIsEventIdZero()
	{
		var board = new WolfCharacterBoard(new WolfVariableBands());
		var hero = board.Add(new WolfCharacter { Id = 0, X = 4, Y = 5 });

		AssertEq(
			board.Hero.X, 4,
			"**and adding id zero sets the hero**, not an event with id zero;"
			+ $" X is {board.Hero.X}");
		AssertEq(
			board.Find(0), hero,
			"**and Find(0) is the hero**");
		AssertEq(
			board.Find(99), null,
			"**and an id nobody placed is nothing**, not a fresh figure with"
			+ $" zeroes; it is {(board.Find(99) == null ? "nothing" : "a figure")}");
	}

	// ---- Das Ticken

	/// <summary>
	/// A moving step takes its frames and not one.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>This is the timing rule the whole board exists for.</strong> A
	/// figure at speed 4 takes four frames per tile, so four ticks put it one
	/// tile along and the fifth starts the next step.
	/// </para>
	/// <para>
	/// A reader that ran a whole step per tick would make a slow guard and a
	/// fast one identical, and the speed setting would be a number nothing
	/// read — which is exactly what the previous card's runner could not detect
	/// on its own, because it had no clock.
	/// </para>
	/// </remarks>
	public void Test_AMovingStepTakesItsFrames()
	{
		var board = new WolfCharacterBoard(new WolfVariableBands());
		var guard = board.Add(new WolfCharacter { Id = 1, X = 0, Y = 0, MoveSpeed = 4 });
		board.StartRoute(1, Route([Step(WolfMoveRouteType.MoveRight)]));

		board.Tick();
		AssertEq(
			guard.X, 1,
			"**and one frame of the four already put it one tile along**, because"
			+ " the step is applied when it starts and the frames are the time it"
			+ $" takes; X is {guard.X}");
		AssertEq(
			board.IsMoving(1), true,
			"**and the route is still running**, because three of the four frames"
			+ $" are left; it is {(board.IsMoving(1) ? "still moving" : "done")}");

		// **Three more frames and the route is done.** Four frames per tile at
		// speed 4 means the first tick spends the step and leaves three, and
		// the three following ticks spend them. An earlier version of this test
		// ticked four times in total and expected the route to be done, which
		// counts the step's own frame as one of the four and so ends one frame
		// early.
		board.Tick();
		board.Tick();
		board.Tick();
		AssertEq(
			board.IsMoving(1), false,
			"**and after four frames the route is done**, which is what four"
			+ " frames per tile at speed 4 means;"
			+ $" it is {(board.IsMoving(1) ? "still moving" : "done")}");
	}

	/// <summary>
	/// A facing step takes no time at all.
	/// </summary>
	/// <remarks>
	/// <strong>Ten facing commands in one frame.</strong> They change no
	/// position, and a reader that spent a frame on each would slow every route
	/// in the game by the number of steps in it — a hundred step patrol would
	/// crawl.
	/// </remarks>
	public void Test_AFacingStepTakesNoTime()
	{
		var board = new WolfCharacterBoard(new WolfVariableBands());
		var guard = board.Add(new WolfCharacter { Id = 1, X = 0, Y = 0 });
		board.StartRoute(1, Route(
		[
			Step(WolfMoveRouteType.FacingLeft),
			Step(WolfMoveRouteType.FacingUp),
			Step(WolfMoveRouteType.FacingRight),
		]));

		board.Tick();

		AssertEq(
			board.IsMoving(1), false,
			"**and three facing steps finish in one frame**, because none of them"
			+ " moves anybody;"
			+ $" it is {(board.IsMoving(1) ? "still moving" : "done")}");
		AssertEq(
			guard.Facing, WolfCharacter.PassRight,
			"**and the last one took effect**, which is the proof they all ran"
			+ $" and not just the first; it is {guard.Facing}");
	}

	/// <summary>
	/// A wait step takes exactly the frames it carries.
	/// </summary>
	/// <remarks>
	/// <strong>The wait is the one step whose duration is in the step.</strong> A
	/// pause at a corner is what makes a patrol look like a patrol, and a reader
	/// that gave it a frame would have every guard pause for the length of one
	/// frame.
	/// </remarks>
	public void Test_AWaitStepTakesItsFrames()
	{
		var board = new WolfCharacterBoard(new WolfVariableBands());
		board.Add(new WolfCharacter { Id = 1, X = 0, Y = 0 });
		board.StartRoute(1, Route([Step(WolfMoveRouteType.WaitXFrames, 3)]));

		board.Tick();
		AssertEq(
			board.IsMoving(1), true,
			"**and after one frame it is still waiting**, because the step asked"
			+ $" for three; it is {(board.IsMoving(1) ? "still moving" : "done")}");
		// **Three more, and the same count as the movement test.** The step's
		// own frame is one of the three it asked for, exactly as a moving
		// step's first frame is one of the frames per tile — the difference
		// would be a wait that lasts one frame longer than it says.
		board.Tick();
		board.Tick();
		board.Tick();
		AssertEq(
			board.IsMoving(1), false,
			"**and after four it is done**, because three is the count the step"
			+ " carried and the first of them is the frame that started it;"
			+ $" it is {(board.IsMoving(1) ? "still moving" : "done")}");
	}

	/// <summary>
	/// A repeating route starts over and a plain one stops.
	/// </summary>
	/// <remarks>
	/// <strong>The flag is the format's</strong>, and a patrol that stopped
	/// after one lap would be a route that ran once — which is what the flag is
	/// for. Both are tested because a reader that always repeated would never
	/// let a character stop, and a reader that never repeated would make every
	/// route a single lap.
	/// </remarks>
	public void Test_ARepeatingRouteStartsOver()
	{
		var bands = new WolfVariableBands();
		var board = new WolfCharacterBoard(bands);
		var guard = board.Add(new WolfCharacter { Id = 1, X = 0, Y = 0, MoveSpeed = 1 });
		board.StartRoute(1, Route(
			[Step(WolfMoveRouteType.MoveRight)],
			pRepeat: true));

		// **Sixteen frames is one tile at speed 1, and the sixteenth is the
		// frame that finds the route out of steps and restarts it.** The first
		// tick starts the step and leaves fifteen; the next fifteen spend them,
		// and the last of them also looks at what comes next — which is the end
		// of the route and, because the flag is set, the start of the second
		// lap.
		//
		// This test counted seventeen and expected the figure at one, and it is
		// at two: the wrap happens on the sixteenth tick and the seventeenth
		// starts the second lap's step. **Getting the count wrong here is how
		// the timing bug above hid** — the wrong expectation still failed, but
		// for a reason that had nothing to do with the bug.
		for (var i = 0; i < 16; i++)
		{
			board.Tick();
		}

		AssertEq(
			board.IsMoving(1), true,
			"**and the repeating route is still going**, because the flag restarts"
			+ $" it; it is {(board.IsMoving(1) ? "still moving" : "done")}");
		AssertEq(
			guard.X, 1,
			"**and the figure is one tile along**, which is the first lap's step"
			+ " and the second lap has only just been started — the wrap happened"
			+ " between steps and not as a jump back;"
			+ $" it is {guard.X}");

		board.Tick();
		AssertEq(
			guard.X, 2,
			"**and the next frame starts the second lap**, which is what makes"
			+ " this a repeating route and not a single one that stopped after"
			+ " sixteen frames of standing still;"
			+ $" X is {guard.X}");
	}

	/// <summary>
	/// A plain route stops and a skipping route walks past a refusal.
	/// </summary>
	/// <remarks>
	/// <strong>The two flags decide the same refusal two different ways.</strong> A
	/// patrol that skips keeps going when a step cannot be done, and a route
	/// that does not skip stops there — and a reader that always skipped would
	/// let a guard walk through a wall, while a reader that never skipped would
	/// freeze a guard at the first step it cannot do.
	/// </remarks>
	public void Test_TheSkipFlagDecidesWhatARefusalDoes()
	{
		var plain = new WolfCharacterBoard(new WolfVariableBands());
		var plainGuard = plain.Add(new WolfCharacter
		{
			Id = 1,
			X = 0,
			Y = 0,
			Passability = WolfCharacter.PassUp,
		});
		plain.StartRoute(1, Route(
		[
			Step(WolfMoveRouteType.MoveRight),
			Step(WolfMoveRouteType.FacingUp),
		]));
		plain.Tick();

		AssertEq(
			plain.IsMoving(1), false,
			"**and a route that does not skip stops at the refused step**, because"
			+ " the flag is the format's and it is off;"
			+ $" it is {(plain.IsMoving(1) ? "still moving" : "done")}");
		AssertEq(
			plainGuard.Facing, WolfCharacter.PassRight,
			"**and the facing is the refused step's own**, which is the rule a"
			+ " stopped route still leaves behind: a guard pressed against a wall"
			+ " turns to face it even when it gives up. An earlier version of"
			+ " this test expected the original facing here and was wrong about"
			+ " its own previous card's rule;"
			+ $" it is {plainGuard.Facing}");

		var skipping = new WolfCharacterBoard(new WolfVariableBands());
		var skipGuard = skipping.Add(new WolfCharacter
		{
			Id = 1,
			X = 0,
			Y = 0,
			Passability = WolfCharacter.PassUp,
		});
		skipping.StartRoute(1, Route(
			[
				Step(WolfMoveRouteType.MoveRight),
				Step(WolfMoveRouteType.FacingDown),
			],
			pSkip: true));
		skipping.Tick();

		AssertEq(
			skipping.IsMoving(1), false,
			"**and a skipping route runs past it**, which is the other half of"
			+ $" the flag; it is {(skipping.IsMoving(1) ? "still moving" : "done")}");
		AssertEq(
			skipGuard.Facing, WolfCharacter.PassDown,
			"**and the facing after the refusal took effect**, which is what"
			+ " skipping means and what stopping did not do;"
			+ $" it is {skipGuard.Facing}");
	}

	/// <summary>
	/// A new route replaces the one the figure was running.
	/// </summary>
	/// <remarks>
	/// <strong>Replace and not queue.</strong> The format's route command sets a
	/// character's movement, and a figure that queued them would walk the old
	/// patrol after the new one — a behaviour no game asks for.
	/// </remarks>
	public void Test_ANewRouteReplacesTheOldOne()
	{
		var board = new WolfCharacterBoard(new WolfVariableBands());
		var guard = board.Add(new WolfCharacter { Id = 1, X = 0, Y = 0, MoveSpeed = 1 });
		board.StartRoute(1, Route([Step(WolfMoveRouteType.MoveRight), Step(WolfMoveRouteType.MoveRight)]));

		board.StartRoute(1, Route([Step(WolfMoveRouteType.MoveDown)]));

		for (var i = 0; i < 20; i++)
		{
			board.Tick();
		}
		AssertEq(
			guard.X, 0,
			"**and the old route's steps never ran**, because the second route"
			+ " replaced the first and not queued behind it;"
			+ $" X is {guard.X}");
		AssertEq(
			guard.Y, 1,
			"**and only the new route's step ran**;"
			+ $" Y is {guard.Y}");
	}

	// ---- Die VM

	/// <summary>
	/// The VM's move route opcode starts a route and does not wait.
	/// </summary>
	/// <remarks>
	/// <strong>Without the wait flag the event continues straight away.</strong>
	/// A reader that always waited would hold every event in the game until its
	/// figure stopped moving, and most events move nothing at all.
	/// </remarks>
	public void Test_TheMoveRouteOpcodeStartsWithoutWaiting()
	{
		var vm = new WolfEventVm();
		vm.Board.Add(new WolfCharacter { Id = 1, X = 0, Y = 0, MoveSpeed = 1 });
		vm.Start(new WolfEventProgram
		{
			Id = 1,
			Commands =
			[
				new WolfEventCommand
				{
					Opcode = WolfEventOpcode.MoveRoute,
					CharacterId = 1,
					Route = Route([Step(WolfMoveRouteType.MoveRight)]),
				},
				new WolfEventCommand
				{
					Opcode = WolfEventOpcode.SetVariable,
					Operand = Normal0,
					Value = 55,
				},
				new WolfEventCommand { Opcode = WolfEventOpcode.End },
			],
		});
		vm.StepTick();

		AssertEq(
			vm.VariableBands.Get(WolfVariable.BandNormal, 0), 55,
			"**and the next command ran in the same tick**, because the route"
			+ " does not wait and the flag was off;"
			+ $" it is {vm.VariableBands.Get(WolfVariable.BandNormal, 0)}");
		AssertEq(
			vm.Board.IsMoving(1), true,
			"**and the figure is still walking**, which is what the same tick"
			+ $" means; it is {(vm.Board.IsMoving(1) ? "moving" : "done")}");
	}

	/// <summary>
	/// The wait flag holds the event at the move route command.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>The instruction index does not move while waiting, and that is
	/// the whole of the wait.</strong> A reader that advanced first would run
	/// the *next* command and then wait, so the event would do one thing too
	/// many before it stopped — and the difference is one command per wait,
	/// which is the kind of error a game never reports.
	/// </para>
	/// </remarks>
	public void Test_TheWaitFlagHoldsTheEventAtTheRouteCommand()
	{
		var vm = new WolfEventVm();
		vm.Board.Add(new WolfCharacter { Id = 1, X = 0, Y = 0, MoveSpeed = 1 });
		vm.Start(new WolfEventProgram
		{
			Id = 1,
			Commands =
			[
				new WolfEventCommand
				{
					Opcode = WolfEventOpcode.MoveRoute,
					CharacterId = 1,
					Route = Route([Step(WolfMoveRouteType.MoveRight)], pWait: true),
				},
				new WolfEventCommand
				{
					Opcode = WolfEventOpcode.SetVariable,
					Operand = Normal0,
					Value = 55,
				},
				new WolfEventCommand { Opcode = WolfEventOpcode.End },
			],
		});
		vm.StepTick();

		AssertEq(
			vm.VariableBands.Get(WolfVariable.BandNormal, 0), 0,
			"**and the next command has not run**, because the wait flag held the"
			+ " event at the route command and did not advance past it;"
			+ $" it is {vm.VariableBands.Get(WolfVariable.BandNormal, 0)}");
		AssertEq(
			vm.State, WolfVmState.Waiting,
			"**and the VM is waiting**, which is the state a frame wait uses too,"
			+ " so the two waits are not distinguishable from the outside — and"
			+ " the ending has to tell them apart;"
			+ $" it is {vm.State}");
	}

	/// <summary>
	/// The event continues once the route is finished.
	/// </summary>
	/// <remarks>
	/// <strong>The wait ends on the board, not on a frame count.</strong> The
	/// frames here are zero for the whole wait, so a reader that only counted
	/// frames would leave the VM waiting forever — and the game's script would
	/// stop with no error anywhere.
	/// </remarks>
	public void Test_TheEventContinuesWhenTheRouteIsFinished()
	{
		var vm = new WolfEventVm();
		vm.Board.Add(new WolfCharacter { Id = 1, X = 0, Y = 0, MoveSpeed = 1 });
		vm.Start(new WolfEventProgram
		{
			Id = 1,
			Commands =
			[
				new WolfEventCommand
				{
					Opcode = WolfEventOpcode.MoveRoute,
					CharacterId = 1,
					Route = Route([Step(WolfMoveRouteType.MoveRight)], pWait: true),
				},
				new WolfEventCommand
				{
					Opcode = WolfEventOpcode.SetVariable,
					Operand = Normal0,
					Value = 55,
				},
				new WolfEventCommand { Opcode = WolfEventOpcode.End },
			],
		});
		vm.StepTick();

		// **Eighteen frames is more than the sixteen a speed-1 tile takes**, so
		// this is past the end of the route and the event must be free.
		for (var i = 0; i < 18; i++)
		{
			vm.StepTick();
		}

		AssertEq(
			vm.VariableBands.Get(WolfVariable.BandNormal, 0), 55,
			"**and the next command ran after the route finished**, which is the"
			+ " second half of the wait and the half a frame count cannot supply;"
			+ $" it is {vm.VariableBands.Get(WolfVariable.BandNormal, 0)}");
	}

	/// <summary>
	/// The board moves while the event waits on something else.
	/// </summary>
	/// <remarks>
	/// <strong>A figure on a patrol keeps walking while a message is up.</strong>
	/// A reader that ticked the board only in the route wait would freeze every
	/// figure for the length of a text box, and a game that has a guard patrolling
	/// behind a dialogue would show a guard standing still.
	/// </remarks>
	public void Test_TheBoardMovesWhileTheEventWaitsOnFrames()
	{
		var vm = new WolfEventVm();
		vm.Board.Add(new WolfCharacter { Id = 1, X = 0, Y = 0, MoveSpeed = 1 });
		vm.Board.StartRoute(1, Route([Step(WolfMoveRouteType.MoveRight)]));
		vm.Start(new WolfEventProgram
		{
			Id = 1,
			Commands =
			[
				new WolfEventCommand
				{
					Opcode = WolfEventOpcode.Wait,
					Frames = 10,
				},
				new WolfEventCommand { Opcode = WolfEventOpcode.End },
			],
		});
		vm.StepTick();

		for (var i = 0; i < 5; i++)
		{
			vm.StepTick();
		}

		AssertEq(
			vm.Board.Find(1)!.X, 1,
			"**and the figure moved during the frame wait**, because the board is"
			+ " ticked above the state check and not inside the route wait;"
			+ $" X is {vm.Board.Find(1)!.X}");
	}

	/// <summary>
	/// A new game empties the board.
	/// </summary>
	/// <remarks>
	/// <strong>The board is cleared with the variables and not left behind.</strong>
	/// A new game that kept the last game's figures would have two heroes on
	/// one tile, and a route that walked into a map with no walls.
	/// </remarks>
	public void Test_ANewGameEmptiesTheBoard()
	{
		var vm = new WolfEventVm();
		vm.Board.Add(new WolfCharacter { Id = 1, X = 5, Y = 5 });
		vm.Board.MapId = 7;
		vm.Board.Width = 20;
		vm.Board.StartRoute(1, Route([Step(WolfMoveRouteType.MoveRight)]));

		vm.ResetState();

		AssertEq(
			vm.Board.Find(1), null,
			"**and the event is gone**, because a new game starts with no"
			+ " figures but the hero;"
			+ $" it is {(vm.Board.Find(1) == null ? "gone" : "still there")}");
		AssertEq(
			vm.Board.MapId, 0,
			"**and the map id is cleared**, because a board that kept the last"
			+ $" game's map would scroll to a map that is not loaded;"
			+ $" it is {vm.Board.MapId}");
		AssertEq(
			vm.Board.Width, 0,
			"**and so is the map width**, which is the third of the four board"
			+ $" fields the reset has to clear; it is {vm.Board.Width}");
	}

	/// <summary>
	/// A route step writes to the VM's own variable bands.
	/// </summary>
	/// <remarks>
	/// <strong>One band set, and not one per board.</strong> A route step that
	/// stores to a variable has to write where the event can read it, and two
	/// band sets would mean a route's value vanished into a store nobody looks
	/// at — a game's patrol would count steps and the event would read zero.
	/// </remarks>
	public void Test_ARouteStepWritesToTheVmsBands()
	{
		var vm = new WolfEventVm();
		vm.Board.Add(new WolfCharacter { Id = 1, X = 0, Y = 0 });
		vm.Board.StartRoute(1, Route(
			[Step(WolfMoveRouteType.AssignToVariable, Normal0, 77)]));
		// **A program, and not just a route.** The VM refuses to step from
		// NotStarted, which is the same refusal a game gets from a runner that
		// forgot to load — and an earlier version of this test called StepTick
		// with no program and expected the board to move anyway.
		vm.Start(new WolfEventProgram
		{
			Id = 1,
			Commands = [new WolfEventCommand { Opcode = WolfEventOpcode.End }],
		});

		vm.StepTick();

		AssertEq(
			vm.VariableBands.Get(WolfVariable.BandNormal, 0), 77,
			"**and the value is in the VM's own bands**, which is the only place"
			+ " the event looks;"
			+ $" it is {vm.VariableBands.Get(WolfVariable.BandNormal, 0)}");
	}
}
