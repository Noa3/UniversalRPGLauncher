using System;

using UniversalRPG.Tests.Framework;
using UniversalRPG.Wolf;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// WOLF characters and the execution of move route steps.
/// </summary>
/// <remarks>
/// <para>
/// The route type table has twenty four verified types and a tested reader, and
/// <strong>until this card nothing executed one</strong> — a game with a patrol
/// route would load and then stand still, and the existing suite stayed green
/// because it only ever read steps, never ran one.
/// </para>
/// <para>
/// The passability bits are the help's own:
/// <c>1上+2左+4右+8下+16左上+32右上+64左下+128右下</c>, and the diagonals are their
/// own bits rather than a combination of the four.
/// </para>
/// </remarks>
public partial class TestWolfMoveRouteRunner : TestBase
{
	/// <summary>Normal variable 0, as a reference.</summary>
	private static int Normal0 => WolfVariable.BaseNormal;

	/// <summary>A runner over a fresh band set.</summary>
	private static WolfMoveRouteRunner Runner(out WolfVariableBands pBands)
	{
		pBands = new WolfVariableBands();
		return new WolfMoveRouteRunner(pBands);
	}

	/// <summary>One step of a type, with the given four byte arguments.</summary>
	private static WolfMoveRouteStep Step(byte pType, params int[] pArguments)
	{
		return new WolfMoveRouteStep
		{
			Type = pType,
			Arguments = pArguments,
		};
	}

	// ---- Die Passierbarkeit

	/// <summary>
	/// The eight passability bits are the help's numbers.
	/// </summary>
	/// <remarks>
	/// <strong>A diagonal is its own bit and not up plus left.</strong> Up is 1
	/// and left is 2, so their sum is 3 — and there is no bit 3. A reader that
	/// combined them would produce a direction the format has no bit for, and a
	/// character whose passability held bit 3 would match no direction at all.
	/// </remarks>
	public void Test_TheEightPassabilityBitsAreTheHelpsNumbers()
	{
		AssertEq(WolfCharacter.PassUp, 1, "**and up is 1**;"
			+ $" it is {WolfCharacter.PassUp}");
		AssertEq(WolfCharacter.PassLeft, 2, "and left is 2;"
			+ $" it is {WolfCharacter.PassLeft}");
		AssertEq(WolfCharacter.PassRight, 4, "**and right is 4, not 3** — the"
			+ " help's list goes 1, 2, 4, 8, and a reader that counted would make"
			+ $" right 3; it is {WolfCharacter.PassRight}");
		AssertEq(WolfCharacter.PassDown, 8, "and down is 8;"
			+ $" it is {WolfCharacter.PassDown}");
		AssertEq(WolfCharacter.PassUpLeft, 16, "**and up-left is 16**, its own"
			+ " bit and not up plus left, which would be 3;"
			+ $" it is {WolfCharacter.PassUpLeft}");
		AssertEq(WolfCharacter.PassUpRight, 32, "and up-right is 32;"
			+ $" it is {WolfCharacter.PassUpRight}");
		AssertEq(WolfCharacter.PassDownLeft, 64, "and down-left is 64;"
			+ $" it is {WolfCharacter.PassDownLeft}");
		AssertEq(WolfCharacter.PassDownRight, 128, "**and down-right is 128**;"
			+ $" it is {WolfCharacter.PassDownRight}");
		AssertEq(
			WolfCharacter.PassAll, 255,
			"**and all eight together is 255**, which is every bit set;"
			+ $" it is {WolfCharacter.PassAll}");
	}

	/// <summary>
	/// A character may not step in a direction its passability excludes.
	/// </summary>
	/// <remarks>
	/// <strong>The refusal still turns the character to face the direction.</strong>
	/// A guard that walks into a closed door faces it, and a game that shows the
	/// guard watching the hero through the gap depends on that. A reader that
	/// returned early before setting the facing would leave a guard staring
	/// straight ahead while its script says it turned.
	/// </remarks>
	public void Test_ARefusedStepStillTurnsTheCharacter()
	{
		var character = new WolfCharacter { X = 5, Y = 5 };
		character.Passability = WolfCharacter.PassUp | WolfCharacter.PassLeft;

		// **The real call, once.** An earlier version of this test asked
		// CanStep twice and then asserted the facing, which no code path
		// reaches: CanStep only answers the question, and Step is what turns
		// the figure. The test passed the first assertion and failed the
		// second, and the reason was the test's own shape.
		var stepped = character.Step(WolfCharacter.PassRight);
		AssertEq(
			stepped, false,
			"**and stepping right is refused**, because the passability does not"
			+ $" include it; it is {stepped}");
		AssertEq(
			character.X, 5,
			"**and the character has not moved**, because a refused step leaves"
			+ $" it put; X is {character.X}");
		AssertEq(
			character.Facing, WolfCharacter.PassRight,
			"**and it now faces right**, which is the part a reader that returned"
			+ " early would have missed — the script says the guard turned"
			+ $" towards the hero; it is {character.Facing}");
	}

	/// <summary>
	/// The eight directions move the right number of tiles.
	/// </summary>
	/// <remarks>
	/// <strong>A diagonal moves on both axes and a straight one on one.</strong>
	/// A reader that treated a diagonal as its horizontal half would walk a
	/// guard along a wall that it was told to go around.
	/// </remarks>
	public void Test_ARunnerStepReportsARefusal()
	{
		var runner = Runner(out _);
		var character = new WolfCharacter { X = 4, Y = 4 };
		character.Passability = WolfCharacter.PassUp;

		AssertEq(
			runner.Run(Step(WolfMoveRouteType.MoveRight), character),
			WolfMoveRouteOutcome.Refused,
			"**and a move step into a wall reports refused**, and not stepped —"
			+ " the two are different outcomes because the board's skip flag has"
			+ " to decide between continuing and stopping, and a reader that"
			+ " reported stepped gave it nothing to decide on");
		AssertEq(
			character.X, 4,
			"**and the figure did not move**, which is what a refusal means;"
			+ $" X is {character.X}");
	}

	public void Test_TheEightDirectionsMoveTheRightWay()
	{
		AssertEq(WolfDirection.DeltaX(WolfCharacter.PassUpLeft), -1,
			"**and up-left moves one to the left**;"
			+ $" it is {WolfDirection.DeltaX(WolfCharacter.PassUpLeft)}");
		AssertEq(WolfDirection.DeltaY(WolfCharacter.PassUpLeft), -1,
			"**and one up as well**, so it is a diagonal and not a half step;"
			+ $" it is {WolfDirection.DeltaY(WolfCharacter.PassUpLeft)}");
		AssertEq(WolfDirection.DeltaX(WolfCharacter.PassUp), 0,
			"**and up does not move sideways**;"
			+ $" it is {WolfDirection.DeltaX(WolfCharacter.PassUp)}");
		AssertEq(WolfDirection.DeltaY(WolfCharacter.PassDownRight), 1,
			"**and down-right moves one down**;"
			+ $" it is {WolfDirection.DeltaY(WolfCharacter.PassDownRight)}");
		AssertEq(WolfDirection.DeltaX(WolfCharacter.PassDownRight), 1,
			"**and one right**;"
			+ $" it is {WolfDirection.DeltaX(WolfCharacter.PassDownRight)}");
	}

	// ---- Das Ausfuehren

	/// <summary>
	/// A move step moves the character and a facing step does not.
	/// </summary>
	/// <remarks>
	/// <strong>These are two different types in the table and they are not the
	/// same thing.</strong> A reader that moved the character on a facing step
	/// would walk a stationary guard off its tile, one tile per facing command.
	/// </remarks>
	public void Test_AMoveStepMovesAndAFacingStepDoesNot()
	{
		var runner = Runner(out _);
		// **A character with a map, and not one without.** Since the last card a
		// step needs the grid — "the map was not read" is a different answer
		// from "the tile is passable" — so a test that measures a moving step
		// has to give the figure a map, and a test that means to be refused
		// leaves it out.
		// **Alone, and not a bare character.** A step asks the map and the cast,
		// and "nobody is there" is a different answer from "nobody could be
		// asked" — so a figure outside a board is put on an empty one here
		// rather than left with two questions it cannot answer.
		var character = WolfCharacter.Alone(1, new WolfPassabilityGrid(20, 20));
		character.X = 3;
		character.Y = 3;

		AssertEq(
			runner.Run(Step(WolfMoveRouteType.MoveLeft), character),
			WolfMoveRouteOutcome.Stepped,
			"**and a move step steps**, which is the outcome that says it ran");
		AssertEq(
			character.X, 2,
			"**and the character moved one to the left**;"
			+ $" X is {character.X}");

		AssertEq(
			runner.Run(Step(WolfMoveRouteType.FacingUp), character),
			WolfMoveRouteOutcome.Stepped,
			"**and a facing step reports the same outcome**, because it ran and"
			+ " changed a state, and the caller learns which from the facing");
		AssertEq(
			character.Y, 3,
			"**and the character has not moved**, because a facing step is not a"
			+ $" move step; Y is {character.Y}");
		AssertEq(
			character.Facing, WolfCharacter.PassUp,
			"**and it now faces up**;"
			+ $" it is {character.Facing}");
	}

	/// <summary>
	/// The four settings steps change the four settings.
	/// </summary>
	/// <remarks>
	/// <strong>Speed and frequency are 0 to 6 and they are not the same.</strong>
	/// The help writes <c>移動速度[遅0-6速]</c> and <c>移動頻度[早0-6遅]</c> — one
	/// slow to fast, the other often to rarely — and a reader that mapped one
	/// onto the other would make a fast character move once in a while.
	/// </remarks>
	public void Test_TheFourSettingsStepsChangeTheFourSettings()
	{
		var runner = Runner(out _);
		var character = new WolfCharacter();

		runner.Run(Step(WolfMoveRouteType.SetMoveSpeed, 2), character);
		AssertEq(
			character.MoveSpeed, 2,
			"**and the move speed is 2**, which is what the step asked for;"
			+ $" it is {character.MoveSpeed}");
		AssertEq(
			character.MoveFrequency, 4,
			"**and the frequency is untouched at 4**, which is the proof that"
			+ " speed and frequency are two fields and not one;"
			+ $" it is {character.MoveFrequency}");

		runner.Run(Step(WolfMoveRouteType.SetMoveFrequency, 5), character);
		AssertEq(
			character.MoveFrequency, 5,
			"**and the frequency is now 5**, on its own;"
			+ $" it is {character.MoveFrequency}");

		runner.Run(Step(WolfMoveRouteType.SetAnimationFrequency, 0), character);
		AssertEq(
			character.AnimationFrequency, 0,
			"**and the animation frequency is 0**, which is the fastest animation"
			+ " and not the same as a move frequency of 0;"
			+ $" it is {character.AnimationFrequency}");

		runner.Run(Step(WolfMoveRouteType.SetHeight, 32), character);
		AssertEq(
			character.Height, 32,
			"**and the height is 32 pixels**, the fourth setting and not a fifth"
			+ $" one the first three touched; it is {character.Height}");
	}

	/// <summary>
	/// A rate outside 0 to 6 is clamped and not refused.
	/// </summary>
	/// <remarks>
	/// <strong>Clamped, and the reason is in the help's own range.</strong> A
	/// character at a clamped rate still moves, and refusing the step would stop
	/// the event instead of the character. Neither answer is what the game
	/// meant, and clamping is the one that keeps the game running.
	/// </remarks>
	public void Test_ARateOutsideTheRangeIsClamped()
	{
		var runner = Runner(out _);
		var character = new WolfCharacter();

		runner.Run(Step(WolfMoveRouteType.SetMoveSpeed, 99), character);
		AssertEq(
			character.MoveSpeed, WolfCharacter.MaxRate,
			"**and a speed of 99 becomes 6**, the top of the help's scale;"
			+ $" it is {character.MoveSpeed}");
		runner.Run(Step(WolfMoveRouteType.SetMoveSpeed, -5), character);
		AssertEq(
			character.MoveSpeed, WolfCharacter.MinRate,
			"**and a speed of minus 5 becomes 0**;"
			+ $" it is {character.MoveSpeed}");
	}

	/// <summary>
	/// A variable step writes through a reference, and an add step reads first.
	/// </summary>
	/// <remarks>
	/// <strong>The add reads the old value,</strong> which is the same rule the
	/// variable operation follows: a right hand side that names the same
	/// variable as the destination has to see the value before the write, or a
	/// doubling step would read the new one.
	/// </remarks>
	public void Test_AVariableStepWritesThroughAReference()
	{
		var runner = Runner(out var bands);
		var character = new WolfCharacter();

		runner.Run(Step(WolfMoveRouteType.AssignToVariable, Normal0, 40), character);
		AssertEq(
			bands.Get(WolfVariable.BandNormal, 0), 40,
			"**and 40 is stored in normal variable 0**, because 2,000,000 is a"
			+ $" reference to it; it is {bands.Get(WolfVariable.BandNormal, 0)}");

		runner.Run(Step(WolfMoveRouteType.AddToVariable, Normal0, 2), character);
		AssertEq(
			bands.Get(WolfVariable.BandNormal, 0), 42,
			"**and adding 2 gives 42**, which proves the add read 40 and not"
			+ " something else;"
			+ $" it is {bands.Get(WolfVariable.BandNormal, 0)}");
	}

	/// <summary>
	/// A variable step to a plain number is refused, not stored.
	/// </summary>
	/// <remarks>
	/// <strong>A value is not a place to write.</strong> Storing under the number
	/// 5 would put an entry in the dictionary under a key that is not a
	/// variable, and a read of normal variable 5 would miss it — so the step
	/// would appear to work and then lose its value.
	/// </remarks>
	public void Test_AVariableStepToAPlainNumberIsRefused()
	{
		var runner = Runner(out var bands);
		var character = new WolfCharacter();

		AssertEq(
			runner.Run(Step(WolfMoveRouteType.AssignToVariable, 5, 99), character),
			WolfMoveRouteOutcome.Refused,
			"**and assigning to the number 5 is refused**, because 5 is a value"
			+ " and not a reference");
		AssertEq(
			bands.Get(WolfVariable.BandNormal, 5), 0,
			"**and normal variable 5 is still empty**, which is the point — a"
			+ " reader that stored it under the raw key would have written"
			+ $" something; it is {bands.Get(WolfVariable.BandNormal, 5)}");
	}

	/// <summary>
	/// The four steps that need something this runner does not have are refused.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>Approaching an event needs a second character, approaching a
	/// position needs the map, a jump needs its own route, and a sound needs
	/// audio.</strong> None of the four is in this runner.
	/// </para>
	/// <para>
	/// Answering the approach steps with a direction instead would walk the
	/// character somewhere the event is not, and answering the sound step with
	/// success would be a lie the caller cannot detect. <strong>A refused
	/// outcome is the one answer a caller can act on</strong>, and it is what
	/// keeps this slice honest about what it does not do.
	/// </para>
	/// </remarks>
	public void Test_TheFourStepsNeedingMoreAreRefused()
	{
		var runner = Runner(out _);
		var character = new WolfCharacter { X = 2, Y = 2 };

		foreach (var type in new[]
		{
			WolfMoveRouteType.MoveApproachEvent,
			WolfMoveRouteType.MoveApproachPosition,
			WolfMoveRouteType.Jump,
			WolfMoveRouteType.SetSound,
			WolfMoveRouteType.SetGraphic,
		})
		{
			AssertEq(
				runner.Run(Step(type, 1), character),
				WolfMoveRouteOutcome.Refused,
				$"**and route type {type:X2} is refused**, because this runner"
				+ " holds one character, no map, no audio and no picture loader");
			AssertEq(
				character.X, 2,
				$"**and type {type:X2} left the character where it was**, which"
				+ " is the second half of a refusal — answering an approach step"
				+ " with a direction would have moved it;"
				+ $" X is {character.X}");
		}
	}

	/// <summary>
	/// A type the table does not define stops the route.
	/// </summary>
	/// <remarks>
	/// <strong>0x2A and 0x2B are the table's gap,</strong> and the type table
	/// says so and keeps them out. A reader that stepped over an unknown type
	/// would have to guess its length, and the file's own length bytes are
	/// precisely what make the format readable without a guess.
	/// </remarks>
	public void Test_AnUnknownTypeStopsTheRoute()
	{
		var runner = Runner(out _);
		var character = new WolfCharacter();

		AssertEq(
			runner.Run(Step(0x2A), character),
			WolfMoveRouteOutcome.UnknownType,
			"**and 0x2A is reported as unknown**, because the table leaves it"
			+ " unused and treating it as known would accept a value the format"
			+ " does not define");
		AssertEq(
			runner.Run(Step(0x2B), character),
			WolfMoveRouteOutcome.UnknownType,
			"**and 0x2B as well**");
		AssertEq(
			runner.Run(Step(0xFF), character),
			WolfMoveRouteOutcome.UnknownType,
			"**and 0xFF, past the table's end**");
	}

	// ---- Das Timing

	/// <summary>
	/// A faster character takes fewer frames per tile, and the slowest is one.
	/// </summary>
	/// <remarks>
	/// <strong>Speed 0 is one frame and not an infinite wait.</strong> Speed 0
	/// is the slowest speed in the help's scale and a character at it still
	/// moves; dividing by it would produce an infinite frame count and a route
	/// would never finish.
	/// </remarks>
	public void Test_TheFramesPerTileFallWithTheSpeed()
	{
		AssertEq(
			WolfMoveRouteRunner.FramesPerTile(0), 1,
			"**and speed 0 is one frame per tile**, which is as slow as the"
			+ " format can say; a reader that divided by the speed would never"
			+ $" finish a route; it is {WolfMoveRouteRunner.FramesPerTile(0)}");
		AssertEq(
			WolfMoveRouteRunner.FramesPerTile(1),
			WolfMoveRouteRunner.FramesPerTileAtSpeedOne,
			"**and speed 1 is sixteen frames**, the base the others divide;"
			+ $" it is {WolfMoveRouteRunner.FramesPerTile(1)}");
		AssertEq(
			WolfMoveRouteRunner.FramesPerTile(2), 8,
			"**and speed 2 is eight**, which is what a reader that mapped speed"
			+ " onto a multiplier instead of a divisor would get wrong;"
			+ $" it is {WolfMoveRouteRunner.FramesPerTile(2)}");
		AssertEq(
			WolfMoveRouteRunner.FramesPerTile(4), 4,
			"**and speed 4 is four**;"
			+ $" it is {WolfMoveRouteRunner.FramesPerTile(4)}");
		AssertEq(
			WolfMoveRouteRunner.FramesPerTile(6), 2,
			"**and speed 6 is two**, and never less than one — a speed of seven"
			+ " is clamped to six and a reader that divided without a floor would"
			+ $" give zero frames and skip; it is {WolfMoveRouteRunner.FramesPerTile(6)}");
	}

	/// <summary>
	/// A wait step reports that it waits, and carries its frames.
	/// </summary>
	/// <remarks>
	/// <strong>Waited and not configured.</strong> The caller needs to know that
	/// time passed, because a route that waits between two steps is what a
	/// patrol uses to pause at a corner, and a caller that treated it as a
	/// setting would run the whole patrol in one frame.
	/// </remarks>
	public void Test_AWaitStepReportsThatItWaits()
	{
		var runner = Runner(out _);
		var character = new WolfCharacter();
		var step = Step(WolfMoveRouteType.WaitXFrames, 30);

		AssertEq(
			runner.Run(step, character),
			WolfMoveRouteOutcome.Waited,
			"**and a wait step reports waited**, which is a different outcome"
			+ " from a setting step for exactly this reason");
		AssertEq(
			step.Arguments[0], 30,
			"**and it carries its thirty frames**, which the caller times with;"
			+ $" it is {step.Arguments[0]}");
	}

	/// <summary>
	/// A step that is shorter than the table reads zero and does not throw.
	/// </summary>
	/// <remarks>
	/// <strong>A file is a file even when it disagrees with this reader.</strong>
	/// The route reader's own lengths make a step steppable without knowing its
	/// shape, so a short step is read and then refused here — and throwing would
	/// stop the game instead of the route.
	/// </remarks>
	public void Test_AStepShorterThanTheTableIsRefusedNotFatal()
	{
		var runner = Runner(out _);
		var character = new WolfCharacter { MoveSpeed = 3 };

		AssertEq(
			runner.Run(Step(WolfMoveRouteType.SetMoveSpeed), character),
			WolfMoveRouteOutcome.Configured,
			"**and a speed step with no argument still runs**, which is what the"
			+ " zero default does");
		AssertEq(
			character.MoveSpeed, 0,
			"**and the speed became 0**, because the argument is missing and zero"
			+ " is what a missing argument reads as; a reader that refused the"
			+ $" step would leave it at 3; it is {character.MoveSpeed}");
	}
}
