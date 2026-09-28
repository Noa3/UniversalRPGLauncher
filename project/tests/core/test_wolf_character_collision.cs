using System;
using System.Collections.Generic;

using UniversalRPG.Tests.Framework;
using UniversalRPG.Wolf;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// WOLF's character-to-character collision, and the two flags that decide it.
/// </summary>
/// <remarks>
/// <para>
/// The event window help gives the option <c>すり抜け</c> — an event can be
/// walked through — and says that such an event cannot start unless the player
/// is standing on it. <strong>So a transparent sign is both a wall you walk
/// through and a thing you can only reach by stepping on it.</strong>
/// </para>
/// <para>
/// The same help gives the hitbox: <c>当ﾀﾘ判定■(正方形)</c> makes it a full
/// square, and off it is one tile wide and half a tile high — and it says the
/// hitbox also changes the contact range, so the shape is not only about
/// collisions.
/// </para>
/// </remarks>
public partial class TestWolfCharacterCollision : TestBase
{
	/// <summary>A board with open ground and one guard on it.</summary>
	private static WolfCharacterBoard BoardWith(
		WolfCharacter pGuard,
		int pWidth = 20,
		int pHeight = 20)
	{
		var board = new WolfCharacterBoard(new WolfVariableBands());
		board.LoadMap(1, new WolfPassabilityGrid(pWidth, pHeight));
		board.Add(pGuard);
		return board;
	}

	// ---- Die halbe Kachel

	/// <summary>
	/// Two figures on the same tile overlap, and the tile above a half-height
	/// one does not.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>Half a tile is the whole of this test.</strong> The help says the
	/// default hitbox is <c>横1マス×縦0.5マス</c> — a figure's feet and not its
	/// whole body — and a figure on the tile above reaches down to exactly the
	/// top of it, so the two touch and do not overlap.
	/// </para>
	/// <para>
	/// A reader that used one tile for both would make every half-height figure
	/// collide with the figure on the tile above, and a crowd in a corridor
	/// would lock solid. That is a game that cannot be finished, and it looks
	/// like a bug in the pathfinding rather than in the hitbox.
	/// </para>
	/// </remarks>
	public void Test_TheDefaultHitboxIsHalfATileHigh()
	{
		var sign = new WolfCharacter { Id = 1, X = 5, Y = 5 };
		var hero = new WolfCharacter { Id = 0, X = 5, Y = 6 };

		AssertEq(
			WolfCharacterCollision.Overlaps(sign, hero), false,
			"**and a figure on the tile above a half-height one does not overlap"
			+ " it**, because the hitbox is half a tile and the two meet at the"
			+ " boundary without crossing it;"
			+ $" it is {WolfCharacterCollision.Overlaps(sign, hero)}");

		var sameTile = new WolfCharacter { Id = 0, X = 5, Y = 5 };
		AssertEq(
			WolfCharacterCollision.Overlaps(sign, sameTile), true,
			"**and two figures on the same tile do overlap**, which is the case"
			+ " that matters — a sign you have to step on is a tile you cannot"
			+ $" share; it is {WolfCharacterCollision.Overlaps(sign, sameTile)}");
	}

	/// <summary>
	/// The square option makes it a full tile.
	/// </summary>
	/// <remarks>
	/// <strong>The help's 当ﾀﾘ判定■ option, and it is not only about hitting.</strong>
	/// The same paragraph says the hitbox also changes the contact range, and a
	/// square event triggers from the tile above while a half-height one
	/// triggers from beside it.
	/// </remarks>
	public void Test_TheSquareOptionMakesItAFullTile()
	{
		var sign = new WolfCharacter { Id = 1, X = 5, Y = 5, SquareHitbox = true };
		var above = new WolfCharacter { Id = 0, X = 5, Y = 6 };

		AssertEq(
			WolfCharacterCollision.Overlaps(sign, above), true,
			"**and a square figure reaches into the tile above**, which is what"
			+ " the option is for — a big sign you cannot squeeze past;"
			+ $" it is {WolfCharacterCollision.Overlaps(sign, above)}");
		AssertEq(
			WolfCharacterCollision.HitboxHeight(sign), 1f,
			"**and its height is one tile**, against half for the default;"
			+ $" it is {WolfCharacterCollision.HitboxHeight(sign)}");
	}

	/// <summary>
	/// Figures on neighbouring tiles do not touch.
	/// </summary>
	/// <remarks>
	/// <strong>Half open on both axes.</strong> A figure on tile 2 reaches from
	/// 2 to 3 and one on tile 3 from 3 to 4; a closed comparison would have them
	/// overlap on the boundary and block a whole corridor of two-tile rooms.
	/// </remarks>
	public void Test_NeighboursDoNotTouch()
	{
		var left = new WolfCharacter { Id = 1, X = 2, Y = 5 };
		var right = new WolfCharacter { Id = 0, X = 3, Y = 5 };

		AssertEq(
			WolfCharacterCollision.Overlaps(left, right), false,
			"**and figures on adjacent tiles do not overlap**, which is what the"
			+ " half-open range is for;"
			+ $" it is {WolfCharacterCollision.Overlaps(left, right)}");
	}

	// ---- Die Durchlassbarkeit

	/// <summary>
	/// A ghost is walked through, and a solid is not stopped by one.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>One sided, and the mover decides.</strong> The help says an event
	/// with the option on can be walked through, and that such an event cannot
	/// start unless the player is standing on it — so the flag belongs to the
	/// ghost and makes the ghost walk through others, not the other way round.
	/// </para>
	/// <para>
	/// A reader that checked "either passes through" would let the hero walk
	/// through a wall because a decoration on that tile is a ghost, and a
	/// reader that made it fully symmetric would let a decoration stop the
	/// hero. <strong>Both are wrong in the direction of an unplayable game.</strong>
	/// </para>
	/// </remarks>
		public void Test_AGhostIsWalkedThroughFromBothSides()
	{
		var ghost = new WolfCharacter { Id = 1, X = 5, Y = 5, PassThrough = true };
		var hero = new WolfCharacter { Id = 0, X = 5, Y = 5 };

		AssertEq(
			WolfCharacterCollision.Overlaps(ghost, hero), false,
			"**and a ghost and the hero on one tile do not collide**, because the"
			+ " help says the option makes the event walk-through;"
			+ $" it is {WolfCharacterCollision.Overlaps(ghost, hero)}");

		// **The hero walks through it, and that is the point of the flag.** An
		// earlier version of this test asserted a *refusal* here — it read the
		// rule backwards, as though the ghost stopped whoever walked into it.
		// That would make a transparent decoration a wall, which is the
		// opposite of what the option is for, and a reader that believed the
		// test would have walled off every invisible trigger in the game.
		var board = BoardWith(ghost);
		var heroOnBoard = board.Hero;
		heroOnBoard.X = 5;
		heroOnBoard.Y = 4;
		var stepped = heroOnBoard.Step(WolfCharacter.PassDown);
		AssertEq(
			stepped, true,
			"**and the hero steps onto a ghost's tile**, because the ghost is a"
			+ " thing you walk through and not a thing that blocks you;"
			+ $" it is {stepped}");

		// **And the other way round is a different question.** A ghost stepping
		// onto a solid figure is also allowed, because the ghost's own flag
		// answers before anybody is asked — which is what
		// Test_AGhostWalksThroughAnybody measures.
	}

	/// <summary>
	/// A ghost walks through anybody, and its own tile is no obstacle.
	/// </summary>
	/// <remarks>
	/// <strong>The mover's flag is what the collision test reads first,</strong>
	/// and it returns before the occupant list is walked. A reader that asked
	/// the other figures instead would let a decoration be stopped by the hero,
	/// which is the same inversion as the previous test seen from the other
	/// side.
	/// </remarks>
	public void Test_AGhostWalksThroughAnybody()
	{
		var hero = new WolfCharacter { Id = 0, X = 5, Y = 5 };
		var ghost = new WolfCharacter { Id = 1, X = 6, Y = 5, PassThrough = true };
		var board = BoardWith(ghost);
		board.Hero.X = 6;
		board.Hero.Y = 5;
		board.Hero.PassThrough = true;

		var stepped = board.Hero.Step(WolfCharacter.PassDown);
		AssertEq(
			stepped, true,
			"**and a ghost steps onto the hero's tile**, because the mover's own"
			+ " flag answers before anybody else is asked;"
			+ $" it is {stepped}");
	}

	// ---- Auf dem Brett

	public void Test_AGuardIsNotWalkedThrough()
	{
		var guard = new WolfCharacter { Id = 1, X = 6, Y = 5 };
		var board = BoardWith(guard);
		// **Taken once.** The board replaces its hero object when the cast
		// changes, so a test that reads board.Hero again after a step is
		// looking at a different character than the one it moved.
		var hero = board.Hero;
		hero.X = 5;
		hero.Y = 5;

		var stepped = hero.Step(WolfCharacter.PassRight);
		AssertEq(
			stepped, false,
			"**and stepping into the guard is refused**, because the guard is not"
			+ $" a ghost; it is {stepped}");
		AssertEq(
			hero.X, 5,
			"**and the hero stayed put**, which is what a refusal means;"
			+ $" X is {hero.X}");

		var stepped2 = hero.Step(WolfCharacter.PassDown);
		AssertEq(
			stepped2, true,
			"**and stepping past the guard succeeds**, because a half-height"
			+ " hitbox on the tile above does not reach down into it;"
			+ $" it is {stepped}");
	}

	/// <summary>
	/// A figure does not collide with itself.
	/// </summary>
	/// <remarks>
	/// <strong>Skipped by identity, not by coordinates.</strong> A figure that
	/// had already reached its tile would otherwise collide with itself, and a
	/// reader that skipped "whoever is at these coordinates" would let a figure
	/// pass through a ghost standing on the same tile.
	/// </remarks>
	public void Test_AFigureDoesNotCollideWithItself()
	{
		// **The guard and not the hero,** because the hero is always on the
		// board and a board with two figures does not measure "the only one".
		var guard = new WolfCharacter { Id = 1, X = 3, Y = 3 };
		var board = BoardWith(guard);
		board.Hero.X = 9;
		board.Hero.Y = 9;

		var stepped = board.Find(1)!.Step(WolfCharacter.PassDown);
		AssertEq(
			stepped, true,
			"**and the only figure on the map steps**, because it is skipped as"
			+ " the mover and not asked about itself;"
			+ $" it is {stepped}");
		AssertEq(
			board.Find(1)!.Y, 4,
			"**and it moved**, which is the point — a board where the only"
			+ " character cannot move is a game that never starts;"
			+ $" Y is {board.Find(1)!.Y}");
	}

	/// <summary>
	/// A figure placed after the others is seen by them.
	/// </summary>
	/// <remarks>
	/// <strong>The occupant list is rebuilt on every change to the cast,</strong>
	/// because a figure added in the middle of a frame would otherwise be
	/// invisible to the collision test until something asked — and a guard who
	/// walked into a newly placed event would stand inside it.
	/// </remarks>
	public void Test_AFigurePlacedLaterIsSeenAtOnce()
	{
		var board = BoardWith(new WolfCharacter { Id = 1, X = 5, Y = 5 });
		board.Hero.X = 4;
		board.Hero.Y = 5;

		board.Add(new WolfCharacter { Id = 2, X = 5, Y = 5 });

		var stepped = board.Hero.Step(WolfCharacter.PassRight);
		AssertEq(
			stepped, false,
			"**and the hero is stopped by the figure that was just placed**, which"
			+ " is what rebuilding the list on placement means;"
			+ $" it is {stepped}");
	}

	/// <summary>
	/// An erased figure is not there.
	/// </summary>
	/// <remarks>
	/// <strong>The help's position read returns nothing for a temporarily
	/// erased event,</strong> and a reader that still collided with it would
	/// stop the hero at a sign the sign was not showing.
	/// </remarks>
	public void Test_AnErasedFigureIsNotThere()
	{
		var board = BoardWith(new WolfCharacter
		{
			Id = 1,
			X = 6,
			Y = 5,
			IsErased = true,
		});
		var hero = board.Hero;
		hero.X = 5;
		hero.Y = 5;

		var stepped = hero.Step(WolfCharacter.PassRight);
		AssertEq(
			stepped, true,
			"**and the hero steps onto an erased figure's tile**, because an"
			+ " erased event is nowhere and not merely invisible;"
			+ $" it is {stepped}");
	}

	/// <summary>
	/// A figure with no list of occupants cannot step.
	/// </summary>
	/// <remarks>
	/// <strong>Refused, for the same reason the missing map is refused.</strong> A
	/// character that could not see the others would walk through every guard
	/// in the game, and the only trace would be a hero standing inside a
	/// shopkeeper — no error anywhere, because a walk through a guard is a
	/// walk.
	/// </remarks>
	public void Test_AFigureWithNoOccupantsCannotStep()
	{
		var loner = new WolfCharacter { Id = 9, X = 3, Y = 3 };
		loner.PassabilityGrid = new WolfPassabilityGrid(10, 10);

		var stepped = loner.Step(WolfCharacter.PassRight);
		AssertEq(
			stepped, false,
			"**and the step is refused**, because there is no cast to ask and"
			+ $" this reader does not assume an empty one; it is {stepped}");
		AssertEq(
			loner.Occupants, null,
			"**and the list is the reason**, which the test names so a reader"
			+ " does not think the refusal came from the missing map;"
			+ $" it is {(loner.Occupants == null ? "nothing" : "a list")}");
	}

	/// <summary>
	/// A new game empties the cast, and the hero can walk again.
	/// </summary>
	/// <remarks>
	/// <strong>The list is rebuilt last in the clear,</strong> because clearing
	/// it while a figure still points at it would leave the hero asking a list
	/// that no longer contains it — and asking is how it is told the world is
	/// empty.
	/// </remarks>
	public void Test_ANewGameEmptiesTheCast()
	{
		var board = BoardWith(new WolfCharacter { Id = 1, X = 5, Y = 5 });
		board.Hero.X = 4;
		board.Hero.Y = 5;

		board.Clear();
		board.LoadMap(1, new WolfPassabilityGrid(20, 20));
		board.Hero.X = 4;
		board.Hero.Y = 5;

		// **The hero is taken once and used once.** A board replaces its hero
		// object on Clear, and a test that read board.Hero twice around a
		// mutation would be measuring two different characters — an earlier
		// version of this test did that and failed for a reason that had
		// nothing to do with the cast.
		var hero = board.Hero;
		var stepped = hero.Step(WolfCharacter.PassRight);
		AssertEq(
			stepped, true,
			"**and the hero walks where the guard used to be**, because the new"
			+ " game has no guard and the list says so;"
			+ $" it is {stepped}");
	}
}
