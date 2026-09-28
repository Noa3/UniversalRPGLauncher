using System;

using UniversalRPG.Tests.Framework;
using UniversalRPG.Wolf;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// WOLF's character target numbers, the party, and the two approach steps.
/// </summary>
/// <remarks>
/// <para>
/// The help's target list is explicit: <c>0以上の場合 ＝ その値のIDを持つイベント</c>,
/// <c>-1＝このイベント</c>, <c>-2＝主人公(隊列先頭)</c>, and -3 to -7 for the five
/// companions. <strong>Zero is an event id and the hero is minus two</strong> — a
/// reader that used zero for the hero would answer a command about event 0 with
/// the player, and both exist in a real map.
/// </para>
/// <para>
/// The two approach steps were refused outright for two cards, because the board
/// had neither a second figure nor a map. It has both now.
/// </para>
/// </remarks>
public partial class TestWolfApproach : TestBase
{
	/// <summary>One step of a type, with the given four byte arguments.</summary>
	private static WolfMoveRouteStep Step(byte pType, params int[] pArguments)
	{
		return new WolfMoveRouteStep
		{
			Type = pType,
			Arguments = pArguments,
		};
	}

	/// <summary>A board with open ground, sized for the tests.</summary>
	private static WolfCharacterBoard Board(int pWidth = 30, int pHeight = 30)
	{
		var board = new WolfCharacterBoard(new WolfVariableBands());
		board.LoadMap(1, new WolfPassabilityGrid(pWidth, pHeight));
		return board;
	}

	// ---- Die Zielnummern

	/// <summary>
	/// The seven numbers, and the hero is -2.
	/// </summary>
	/// <remarks>
	/// <strong>-1 is this event, not event number minus one.</strong> The help
	/// writes <c>-1＝このイベント</c> and gives the rest as -2 through -7, so a
	/// reader that treated the negatives as ids would look for an event numbered
	/// minus one, find none, and answer "nowhere" — which is how a guard walking
	/// to the hero walks to the wall instead.
	/// </remarks>
	public void Test_TheSevenTargetNumbers()
	{
		AssertEq(WolfCharacterTarget.ThisEvent, -1,
			"**and -1 is this event**, which is the help's このイベント;"
			+ $" it is {WolfCharacterTarget.ThisEvent}");
		AssertEq(WolfCharacterTarget.Hero, -2,
			"**and -2 is the hero**, the head of the column — not zero, which is"
			+ $" a real event id; it is {WolfCharacterTarget.Hero}");
		AssertEq(WolfCharacterTarget.Companion1, -3,
			"**and -3 is the first companion**;"
			+ $" it is {WolfCharacterTarget.Companion1}");
		AssertEq(WolfCharacterTarget.Companion5, -7,
			"**and -7 is the fifth**, which is the last the help lists;"
			+ $" it is {WolfCharacterTarget.Companion5}");
		AssertEq(
			WolfCharacterTarget.Classify(0),
			WolfCharacterTarget.Kind.EventId,
			"**and zero is an event id**, because the help says 0 and above is");
		AssertEq(
			WolfCharacterTarget.Classify(-8),
			WolfCharacterTarget.Kind.Unknown,
			"**and -8 names nobody**, because the list stops at -7 and a reader"
			+ " that answered it with a sixth companion would put a figure on the"
			+ " map the editor cannot name");
	}

	/// <summary>
	/// The companion numbers count from one and -2 is the hero.
	/// </summary>
	/// <remarks>
	/// <strong>One based, and that is the help's own numbering.</strong> A reader
	/// that counted from the negative number would call -3 companion zero and
	/// give the party a member who is not on the map.
	/// </remarks>
	public void Test_TheCompanionNumbersCountFromOne()
	{
		AssertEq(WolfCharacterTarget.CompanionNumber(-3), 1,
			"**and -3 is companion one**;"
			+ $" it is {WolfCharacterTarget.CompanionNumber(-3)}");
		AssertEq(WolfCharacterTarget.CompanionNumber(-7), 5,
			"**and -7 is companion five**;"
			+ $" it is {WolfCharacterTarget.CompanionNumber(-7)}");
		AssertEq(WolfCharacterTarget.CompanionNumber(-2), -1,
			"**and -2 is no companion at all**, because it is the hero;"
			+ $" it is {WolfCharacterTarget.CompanionNumber(-2)}");
	}

	/// <summary>
	/// A party has five companion slots and an empty one is nobody.
	/// </summary>
	/// <remarks>
	/// <strong>Five and not a list.</strong> The help's target list stops at -7,
	/// so a party has at most five members besides the hero, and a reader that
	/// grew a list would answer -8 with a sixth companion the editor cannot
	/// name. <strong>An empty slot is null and not a fresh figure</strong> — a
	/// guard that approached an invented companion would walk to nobody.
	/// </remarks>
	public void Test_ThePartyHasFiveSlots()
	{
		var party = new WolfParty();

		AssertEq(
			party.Companion(1), null,
			"**and an empty slot is nobody**, not a fresh figure — a guard that"
			+ " approached an invented companion would walk to nobody;"
			+ $" it is {(party.Companion(1) == null ? "nobody" : "a figure")}");
		AssertEq(
			party.SetCompanion(6, new WolfCharacter { Id = 8 }), false,
			"**and slot six is refused**, because the help names five;"
			+ $" it is {party.SetCompanion(6, new WolfCharacter { Id = 8 })}");
		AssertEq(
			party.SetCompanion(1, new WolfCharacter { Id = 7 }), true,
			"**and slot one takes a companion**;"
			+ $" it is {party.SetCompanion(1, new WolfCharacter { Id = 7 })}");
		AssertEq(
			party.CompanionCount, 1,
			"**and the party counts one**, which is what the counter is for;"
			+ $" it is {party.CompanionCount}");
	}

	// ---- Die Zielaufloesung

	/// <summary>
	/// The board resolves each of the target numbers.
	/// </summary>
	/// <remarks>
	/// <strong>-1 is the route's own event, which the route carries.</strong>
	/// "This event" means the program the route runs in, and the runner has no
	/// such context — so the number travels with the route rather than being
	/// looked up.
	/// </remarks>
	public void Test_TheBoardResolvesEveryTargetNumber()
	{
		var board = Board();
		var guard = board.Add(new WolfCharacter { Id = 1, X = 1, Y = 1 });
		var shopper = board.Add(new WolfCharacter { Id = 4, X = 4, Y = 4 });
		board.Party.Hero = guard;
		board.Party.SetCompanion(1, shopper);

		AssertEq(
			board.FindTarget(WolfCharacterTarget.ThisEvent, 4), shopper,
			"**and -1 is the route's own event**, which is event four here;"
			+ $" it is {(board.FindTarget(WolfCharacterTarget.ThisEvent, 4)?.Id.ToString() ?? "nobody")}");
		AssertEq(
			board.FindTarget(WolfCharacterTarget.Hero, 1), guard,
			"**and -2 is the hero from the party**, which is the guard here"
			+ " because the test put him there;"
			+ $" it is {(board.FindTarget(WolfCharacterTarget.Hero, 1)?.Id.ToString() ?? "nobody")}");
		AssertEq(
			board.FindTarget(WolfCharacterTarget.Companion1, 1), shopper,
			"**and -3 is the first companion**;"
			+ $" it is {(board.FindTarget(WolfCharacterTarget.Companion1, 1)?.Id.ToString() ?? "nobody")}");
		AssertEq(
			board.FindTarget(4, 1), shopper,
			"**and 4 is the event with id four**;"
			+ $" it is {(board.FindTarget(4, 1)?.Id.ToString() ?? "nobody")}");
		AssertEq(
			board.FindTarget(-5, 1), null,
			"**and -5 is nobody**, because the party has no third companion and a"
			+ " reader that answered it with coordinates would walk the guard to"
			+ " minus three;"
			+ $" it is {(board.FindTarget(-5, 1) == null ? "nobody" : "a figure")}");
	}

	// ---- Die Annaeherungsschritte

	/// <summary>
	/// A guard walks one tile toward its target and stops beside it.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>One tile per step, and the target is re-read every step.</strong> A
	/// guard approaching a moving hero has to keep closing the distance, and a
	/// reader that computed the whole path once would walk to where the hero was.
	/// </para>
	/// <para>
	/// <strong>Arrived when the X gap is gone, whatever the Y gap is.</strong> A
	/// half-height hitbox means standing beside the target counts as being on it,
	/// and a reader that demanded both gaps be zero would have guards shuffling
	/// up and down one tile forever.
	/// </para>
	/// </remarks>
	public void Test_AGuardWalksTowardItsTarget()
	{
		var board = Board();
		var guard = board.Add(new WolfCharacter
		{
			Id = 1,
			X = 5,
			Y = 5,
			MoveSpeed = 1,
		});
		// **The hero is on the board as well as in the party, and that is the
		// point of this test.** A party member who is not on the map is
		// somewhere the collision test cannot see, so the guard would walk onto
		// its tile. The previous card made the party and the board separate
		// things, and a test that set only the party measured a guard walking
		// through the player.
		var hero = board.Add(new WolfCharacter { Id = 0, X = 10, Y = 5 });
		board.Party.Hero = hero;
		board.StartRoute(1, new WolfMoveRoute
		{
			Steps = [Step(WolfMoveRouteType.MoveApproachEvent, WolfCharacterTarget.Hero)],
			Mode = WolfMoveRouteMode.Custom,
			RepeatActions = true,
		});

		board.Tick();
		AssertEq(
			guard.X, 6,
			"**and one frame closed the gap by one tile**, because the step moves"
			+ $" one tile and the X gap is the larger one; X is {guard.X}");
		AssertEq(
			guard.Y, 5,
			"**and it did not move on Y**, because the X gap of five is the"
			+ $" larger and the axis with the larger gap goes first; Y is {guard.Y}");

		// **Nine more frames at speed one, which is sixteen per tile.**
		for (var i = 0; i < 200; i++)
		{
			board.Tick();
		}
		// **It stops at nine and not at ten, and the collision rule is why.**
		// The previous card made the hitbox a tile wide and half a tile high, so
		// a figure at nine and a figure at ten do not overlap — the gap of one
		// is where the guard arrives. A reader that asked the guard to stand on
		// the target's own tile would have it refused by the collision test on
		// the last step, and the route would end there — which looks like a
		// wall rather than like an arrival.
		AssertEq(
			guard.X, 9,
			"**and it stops at nine**, because a half-height hitbox makes nine and"
			+ $" ten the same tile for collision; X is {guard.X}");
		// **And the repeat flag put the step back at the start, which is how a
		// chase keeps following.** An earlier version of this test expected the
		// route to be finished here, having read the arrival as the end of the
		// walk. It is not: the guard has arrived, the route has one step, and the
		// repeat flag restarts it — so the next frame asks again, finds the X
		// gap still closed, and the guard stands where it is. That is what a
		// guard following a stationary hero does.
		AssertEq(
			board.IsMoving(1), true,
			"**and the route is still going**, because the repeat flag restarts"
			+ " the step and the next frame asks again — the arrival ends the"
			+ " walk, not the route;"
			+ $" it is {(board.IsMoving(1) ? "still moving" : "done")}");
	}

	/// <summary>
	/// The larger gap goes first, and a tie closes X.
	/// </summary>
	/// <remarks>
	/// <strong>That is what makes a diagonal read as a diagonal.</strong> A guard
	/// five to the right and one down closes the five and then the one; a reader
	/// that closed both axes at once would produce a diagonal step the format has
	/// no type for, and a reader that closed Y on a tie would step vertically out
	/// of a diagonal and arrive one frame later.
	/// </remarks>
	public void Test_TheLargerGapGoesFirst()
	{
		var board = Board();
		// **The hero is offset in both axes,** because the whole question is
		// which axis closes first and a hero on the same X would end the step
		// before Y was ever looked at — a half-height hitbox makes a figure
		// beside the target already on it, so a straight line is a straight
		// arrival.
		var guard = board.Add(new WolfCharacter { Id = 1, X = 5, Y = 5, MoveSpeed = 1 });
		board.Party.Hero = board.Add(new WolfCharacter { Id = 0, X = 8, Y = 9 });
		board.StartRoute(1, new WolfMoveRoute
		{
			Steps = [Step(WolfMoveRouteType.MoveApproachEvent, WolfCharacterTarget.Hero)],
			Mode = WolfMoveRouteMode.Custom,
		});

		board.Tick();

		// **Y goes first, because four is larger than three.** An earlier
		// version of this test put the hero at (8, 9) and expected X to close,
		// having compared "three" with "four minus one" — the gaps are four on
		// Y and three on X, and the larger absolute value is what the rule
		// looks at. The step down is the one the guard took.
		AssertEq(
			guard.Y, 6,
			"**and the guard closed the Y gap**, because four is larger than"
			+ $" three; Y is {guard.Y}");
		AssertEq(
			guard.X, 5,
			"**and left X alone**, because the Y gap won the comparison;"
			+ $" X is {guard.X}");
	}

	/// <summary>
	/// A position approach reads its coordinates and may read them from a
	/// variable.
	/// </summary>
	/// <remarks>
	/// <strong>The help allows a variable wherever a number is entered,</strong>
	/// and a coordinate of 2,000,000 is normal variable 0. A reader that used
	/// the raw number would send the guard to tile two million — off the map,
	/// where the step is refused rather than executed at a nonsense place.
	/// </remarks>
	public void Test_APositionApproachReadsItsCoordinates()
	{
		var bands = new WolfVariableBands();
		var board = new WolfCharacterBoard(bands);
		board.LoadMap(1, new WolfPassabilityGrid(30, 30));
		bands.Set(WolfVariable.BandNormal, 0, 12);
		bands.Set(WolfVariable.BandNormal, 1, 7);
		var guard = board.Add(new WolfCharacter { Id = 1, X = 5, Y = 5, MoveSpeed = 1 });
		board.StartRoute(1, new WolfMoveRoute
		{
			Steps =
			[
				Step(
					WolfMoveRouteType.MoveApproachPosition,
					WolfVariable.BaseNormal,
					WolfVariable.BaseNormal + 1),
			],
			Mode = WolfMoveRouteMode.Custom,
		});

		board.Tick();

		AssertEq(
			guard.X, 6,
			"**and the guard moved toward the variable's value**, which is"
			+ " twelve and not two million — the reference resolved through the"
			+ $" bands; X is {guard.X}");
	}

	/// <summary>
	/// A target that names nobody ends the route.
	/// </summary>
	/// <remarks>
	/// <strong>Refused, and the route stops unless it says to skip.</strong> A
	/// party with no third companion makes -5 name nobody, and a reader that
	/// treated it as coordinates would walk the guard to minus three — a guard
	/// that has left the map in a direction the map does not have.
	/// </remarks>
	public void Test_ATargetThatNamesNobodyEndsTheRoute()
	{
		var board = Board();
		var guard = board.Add(new WolfCharacter { Id = 1, X = 5, Y = 5 });
		board.StartRoute(1, new WolfMoveRoute
		{
			Steps = [Step(WolfMoveRouteType.MoveApproachEvent, -5)],
			Mode = WolfMoveRouteMode.Custom,
		});

		board.Tick();

		AssertEq(
			board.IsMoving(1), false,
			"**and the route is done**, because -5 names no companion and the"
			+ " skip flag is off;"
			+ $" it is {(board.IsMoving(1) ? "still moving" : "done")}");
		AssertEq(
			guard.X, 5,
			"**and the guard did not move**, which is what a refusal means;"
			+ $" X is {guard.X}");
	}

	/// <summary>
	/// A guard pressed against a wall is not arrived and the route stops.
	/// </summary>
	/// <remarks>
	/// <strong>Blocked and not arrived.</strong> A reader that treated a refused
	/// step as "there is nowhere to go" would have ended the route on a step that
	/// never happened, and a guard chasing the hero would give up the moment the
	/// hero stood behind a pillar.
	/// </remarks>
	public void Test_AGuardAgainstAWallStopsTheRoute()
	{
		var board = Board();
		var grid = (WolfPassabilityGrid)board.Passability!;
		// **A wall at six, between the guard at five and the hero at eight.**
		grid.Set(6, 5, WolfChipPassability.Blocked, true);
		var guard = board.Add(new WolfCharacter { Id = 1, X = 5, Y = 5, MoveSpeed = 1 });
		board.Party.Hero = board.Add(new WolfCharacter { Id = 0, X = 8, Y = 5 });
		board.StartRoute(1, new WolfMoveRoute
		{
			Steps = [Step(WolfMoveRouteType.MoveApproachEvent, WolfCharacterTarget.Hero)],
			Mode = WolfMoveRouteMode.Custom,
		});

		for (var i = 0; i < 40; i++)
		{
			board.Tick();
		}

		AssertEq(
			board.IsMoving(1), false,
			"**and the route gave up**, because the step was refused and the skip"
			+ $" flag is off; it is {(board.IsMoving(1) ? "still moving" : "done")}");
		AssertEq(
			guard.X, 5,
			"**and the guard stayed at the wall**, which is the honest outcome"
			+ $" and not one tile further; X is {guard.X}");
	}

	/// <summary>
	/// A guard already beside its target does not shuffle.
	/// </summary>
	/// <remarks>
	/// <strong>Arrived ends the step, and the next step runs now.</strong> A
	/// reader that spent a tile's worth of frames standing still would make a
	/// guard that is already there freeze for sixteen frames before doing
	/// anything else — visible, and wrong.
	/// </remarks>
	public void Test_AGuardBesideItsTargetDoesNotShuffle()
	{
		var board = Board();
		// **On the same tile, because the X gap of zero is what this test is
		// about,** and the hero is on the board so the collision test can see
		// that the two cannot share it.
		var guard = board.Add(new WolfCharacter { Id = 1, X = 10, Y = 5, MoveSpeed = 1 });
		board.Party.Hero = board.Add(new WolfCharacter { Id = 0, X = 10, Y = 5 });
		board.StartRoute(1, new WolfMoveRoute
		{
			Steps = [Step(WolfMoveRouteType.MoveApproachEvent, WolfCharacterTarget.Hero)],
			Mode = WolfMoveRouteMode.Custom,
		});

		board.Tick();

		AssertEq(
			guard.X, 10,
			"**and the guard did not move**, because the X gap is already gone"
			+ $" and the same tile is as close as it gets; X is {guard.X}");
		AssertEq(
			board.IsMoving(1), false,
			"**and the route is done**, because that was the only step;"
			+ $" it is {(board.IsMoving(1) ? "still moving" : "done")}");
	}
}
