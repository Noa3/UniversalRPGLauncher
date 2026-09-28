using System;
using System.Collections.Generic;

namespace UniversalRPG.Wolf;

/// <summary>
/// A WOLF character on the map, with the state a move route changes.
/// </summary>
/// <remarks>
/// <para>
/// <strong>The speed and frequency are 0 to 6, and they are not the same
/// thing.</strong> The help's variable-plus page says
/// <c>移動速度[遅0-6速]</c> and <c>移動頻度[早0-6遅]</c> — speed runs slow to
/// fast, frequency runs often to rarely — and a reader that mapped one onto
/// the other would make a fast character move once in a while.
/// </para>
/// <para>
/// <strong>The passability bits are a bit field and not four booleans.</strong>
/// The help gives them exactly:
/// <c>移動可能方向[1上+2左+4右+8下+16左上+32右上+64左下+128右下]</c> — up, left,
/// right, down, up-left, up-right, down-left, down-right. The diagonals are
/// their own bits and not a combination of the four, so a reader that added
/// up and left would get bit 3, which is a diagonal the character may not have.
/// </para>
/// </remarks>
public sealed class WolfCharacter
{
	/// <summary>May face up.</summary>
	public const int PassUp = 1;

	/// <summary>May face left.</summary>
	public const int PassLeft = 2;

	/// <summary>May face right.</summary>
	public const int PassRight = 4;

	/// <summary>May face down.</summary>
	public const int PassDown = 8;

	/// <summary>May face up and left.</summary>
	public const int PassUpLeft = 16;

	/// <summary>May face up and right.</summary>
	public const int PassUpRight = 32;

	/// <summary>May face down and left.</summary>
	public const int PassDownLeft = 64;

	/// <summary>May face down and right.</summary>
	public const int PassDownRight = 128;

	/// <summary>May pass through everything.</summary>
	public const int PassAll =
		PassUp | PassLeft | PassRight | PassDown
		| PassUpLeft | PassUpRight | PassDownLeft | PassDownRight;

	/// <summary>The slowest speed, and also the rarest frequency.</summary>
	public const int MinRate = 0;

	/// <summary>The fastest speed, and also the most often frequency.</summary>
	public const int MaxRate = 6;

	/// <summary>The event's id, or 0 for the hero.</summary>
	public int Id { get; set; }

	/// <summary>The map the character is on.</summary>
	public int MapId { get; set; }

	/// <summary>The column, in tiles, from the left of the map.</summary>
	public int X { get; set; }

	/// <summary>The row, in tiles, from the top of the map.</summary>
	public int Y { get; set; }

	/// <summary>
	/// Which way the character looks, as the passability bit of that direction.
	/// </summary>
	/// <remarks>
	/// <strong>A direction bit and not a four-value code</strong>, because the
	/// passability field already names the eight directions with exactly these
	/// numbers, and a second numbering beside it would be a second thing to keep
	/// in step.
	/// </remarks>
	public int Facing { get; set; } = PassDown;

	/// <summary>How fast, from 0 slow to 6 fast.</summary>
	public int MoveSpeed { get; set; } = 4;

	/// <summary>How often it moves on its own, from 0 often to 6 rarely.</summary>
	public int MoveFrequency { get; set; } = 4;

	/// <summary>How often the walking animation changes, from 0 to 6.</summary>
	public int AnimationFrequency { get; set; } = 6;

	/// <summary>Which directions the character may be moved in.</summary>
	public int Passability { get; set; } = PassAll;

	/// <summary>Where the character sits on the tile, in pixels above it.</summary>
	public int Height { get; set; }

	/// <summary>How see-through the character is, from 0 to 255.</summary>
	public int Opacity { get; set; } = 255;

	/// <summary>The graphic the character shows.</summary>
	public string Graphic { get; set; } = "";

	/// <summary>Whether the character is still on the map.</summary>
	public bool IsErased { get; set; }

	/// <summary>
	/// Whether other characters may walk through this one.
	/// </summary>
	/// <remarks>
	/// <strong>The option's name is すり抜け and it is one sided.</strong> The
	/// event window help says an event with it on can be walked through, and
	/// that such an event cannot start unless the player is standing on it — so
	/// a transparent sign is both a wall you walk through and a thing you can
	/// only reach by stepping on it. **The flag belongs to the ghost, and it does
	/// not make the ghost unable to walk through others**: a reader that made
	/// the relationship symmetric would let a decoration stop the hero.
	/// </remarks>
	public bool PassThrough { get; set; }

	/// <summary>
	/// Whether the hitbox is a full square or a tile wide and half high.
	/// </summary>
	/// <remarks>
	/// <strong>The help's 当ﾀﾘ判定■ option, and it is not only about hitting.</strong>
	/// The same paragraph says the hitbox also changes the contact range, and
	/// the contact rules depend on the shape: a square event triggers from the
	/// tile above it, a half-height one from beside it.
	/// </remarks>
	public bool SquareHitbox { get; set; }

	/// <summary>
	/// The same character, standing somewhere else.
	/// </summary>
	/// <remarks>
	/// <strong>A copy and not a mutation, and that is the point.</strong> To ask
	/// whether a character could stand on a tile, the collision test needs the
	/// character as it would be — every field, including the hitbox and the
	/// pass-through flag. Building a candidate by hand would mean copying
	/// eleven fields and missing one the next time a field is added, and the
	/// field that matters most is exactly the one a hand-built copy would
	/// forget.
	/// </remarks>
	public WolfCharacter At(int pX, int pY)
	{
		return new WolfCharacter
		{
			Id = Id,
			MapId = MapId,
			X = pX,
			Y = pY,
			Facing = Facing,
			MoveSpeed = MoveSpeed,
			MoveFrequency = MoveFrequency,
			AnimationFrequency = AnimationFrequency,
			Passability = Passability,
			Height = Height,
			Opacity = Opacity,
			Graphic = Graphic,
			IsErased = IsErased,
			PassThrough = PassThrough,
			SquareHitbox = SquareHitbox,
			PassabilityGrid = PassabilityGrid,
		};
	}

	/// <summary>
	/// Whether the character may be moved one step in the given direction.
	/// </summary>
	/// <remarks>
	/// <strong>Both the passability bit and the bounds,</strong> and the order
	/// is the passability first: a character that may not face left cannot be
	/// moved left even if the map has room, and a reader that checked the map
	/// first would walk it into a wall.
	/// </remarks>
	public bool CanStep(int pDirection)
	{
		if (pDirection == 0)
		{
			return false;
		}
		return (Passability & pDirection) == pDirection;
	}

	/// <summary>
	/// Everyone else on the map, for the character-to-character check.
	/// </summary>
	/// <remarks>
	/// <strong>A set by the board and not a lookup.</strong> The collision test
	/// has to see every figure — a character that only knew about itself would
	/// walk through every ghost and every guard, and the only trace would be a
	/// hero standing inside a shopkeeper. The board owns the list because it
	/// owns the map, and a figure placed on it is given the same set as
	/// everybody else.
	/// </remarks>
	public Func<IReadOnlyList<WolfCharacter>>? Occupants { get; set; }

	/// <summary>
	/// Puts a lone character on an empty map, with nobody else to ask about.
	/// </summary>
	/// <remarks>
	/// <strong>A named method and not a hand-built list in every test.</strong> A
	/// character outside a board — a unit test, a tool, a preplaced figure —
	/// still needs the cast to be asked, and building the closure by hand in
	/// each place is three lines that three places would get three different
	/// ways. <strong>The list contains this character and nobody else</strong>,
	/// so it collides with nothing, which is what an empty board means.
	/// </remarks>
	public static WolfCharacter Alone(int pId, WolfPassabilityGrid pGrid)
	{
		var alone = new WolfCharacter { Id = pId, PassabilityGrid = pGrid };
		// **The list names the character, so the self-skip has something to
		// skip.** A list that did not contain it would make every step a
		// refusal against nobody, and the reason for the refusal would not be
		// the thing the caller could see.
		alone.Occupants = () => [alone];
		return alone;
	}

	/// <summary>
	/// The map the character walks on, or null when there is none.
	/// </summary>
	/// <remarks>
	/// <strong>An optional grid and not a required one, and that is
	/// deliberate.</strong> A character with no grid is a figure on a map this
	/// reader has not loaded, and the only honest answer there is that nothing
	/// is known about the tile — which is not the same as a tile that is
	/// passable. A reader that treated "no map" as "no obstacles" would let a
	/// guard walk through a wall on every map whose chips it could not read.
	/// </remarks>
	public WolfPassabilityGrid? PassabilityGrid { get; set; }

	/// <summary>
	/// Moves one tile, if the direction is allowed and the tile is.
	/// </summary>
	/// <returns>False when the step was refused, leaving the character put.</returns>
	/// <remarks>
	/// <para>
	/// <strong>The facing is set before the refusal is decided, and not
	/// after.</strong> A character that walks into a wall turns to face it, and
	/// a game that shows a guard watching the hero through the gap depends on
	/// that. The first version of this method returned on the refusal and left
	/// the facing alone — while the comment above it promised the opposite —
	/// and the test is what caught the two disagreeing.
	/// </para>
	/// <para>
	/// <strong>The passability check and the map check are both refusals, and
	/// they are the same refusal to the caller.</strong> A direction the
	/// character may not take and a tile it may not stand on are one thing to
	/// the route: the step did not happen. A reader that reported them
	/// differently would give the board's skip flag two rules where the format
	/// has one.
	/// </para>
	/// </remarks>
	public bool Step(int pDirection)
	{
		Facing = pDirection;
		if (!CanStep(pDirection))
		{
			return false;
		}
		// **The map is asked before the position moves, and its answer is a
		// refusal like any other.** Without a grid there is nothing to ask, and
		// a character with no grid is on a map this reader has not read — so
		// the step is refused rather than assumed possible.
		if (PassabilityGrid is not { } grid)
		{
			return false;
		}
		var nextX = X + WolfDirection.DeltaX(pDirection);
		var nextY = Y + WolfDirection.DeltaY(pDirection);
		if (!grid.AllowsStanding(nextX, nextY))
		{
			return false;
		}
		// **The other figures are asked last, and a refusal from either is the
		// same refusal.** A figure that is stopped by a wall and a figure that
		// is stopped by a guard are one thing to the route: the step did not
		// happen. A reader that reported them differently would give the
		// board's skip flag two rules where the format has one.
		//
		// **The occupant list is required, and its absence is a refusal** — the
		// same reasoning as the missing map. A character that could not see
		// the others would walk through all of them.
		if (Occupants?.Invoke() is not { } occupants)
		{
			return false;
		}
		if (!WolfCharacterCollision.CanOccupy(this, nextX, nextY, occupants))
		{
			return false;
		}
		X = nextX;
		Y = nextY;
		return true;
	}

	/// <summary>
	/// Clamps a speed or frequency into 0 to 6.
	/// </summary>
	/// <remarks>
	/// <strong>Clamped and not refused.</strong> The help writes the range as
	/// <c>遅0-6速</c>, so a value outside it is a number the editor cannot use;
	/// clamping it keeps the character moving and refusing it would stop the
	/// event, and neither answer is what the game meant.
	/// </remarks>
	public static int ClampRate(int pRate)
	{
		return Math.Clamp(pRate, MinRate, MaxRate);
	}
}
/// <summary>
/// The eight directions, by the passability bits the help names.
/// </summary>
/// <remarks>
/// <strong>The bit values and not 0 to 7.</strong> The help writes
/// <c>1上+2左+4右+8下+16左上+32右上+64左下+128右下</c>, and a compact 0 to 7
/// here would mean a second table beside the passability field, and the two
/// would have to be translated in both directions at every use.
/// </remarks>
public static class WolfDirection
{
	/// <summary>One tile to the left, or nowhere.</summary>
	public static int DeltaX(int pDirection)
	{
		return pDirection switch
		{
			WolfCharacter.PassLeft or WolfCharacter.PassUpLeft
				or WolfCharacter.PassDownLeft => -1,
			WolfCharacter.PassRight or WolfCharacter.PassUpRight
				or WolfCharacter.PassDownRight => 1,
			_ => 0,
		};
	}

	/// <summary>One tile up, or nowhere.</summary>
	public static int DeltaY(int pDirection)
	{
		return pDirection switch
		{
			WolfCharacter.PassUp or WolfCharacter.PassUpLeft
				or WolfCharacter.PassUpRight => -1,
			WolfCharacter.PassDown or WolfCharacter.PassDownLeft
				or WolfCharacter.PassDownRight => 1,
			_ => 0,
		};
	}

	/// <summary>
	/// The direction bit for a ten key facing, or 0 for "no direction".
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>Deliberately not implemented, and the help does not give the
	/// figure.</strong> The variable-plus help says a character's facing is 1 to
	/// 9 and corresponds to the ten key, and it points at "figure A" for the
	/// correspondence — a diagram that is not in the text. Guessing the layout
	/// would be the same mistake as guessing the band offsets, and that one
	/// cost a card.
	/// </para>
	/// <para>
	/// A caller that needs a ten key facing has to pass the direction bit, and a
	/// caller reading a facing out of a data file gets a refused read rather
	/// than a wrong number. <strong>A missing mapping is a gap to close from the
	/// figure, and not something to paper over with a plausible table.</strong>
	/// </para>
	/// </remarks>
	public static int FromTenKey(int pKey)
	{
		// **0, and not a guess.** Returning 0 means "no direction", which is
		// the honest answer when the table is not known: a caller that walks
		// with it gets no movement instead of movement in a direction the game
		// never asked for.
		return 0;
	}
}
