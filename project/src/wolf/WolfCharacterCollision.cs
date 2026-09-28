using System;
using System.Collections.Generic;

namespace UniversalRPG.Wolf;

/// <summary>
/// Whether one character may stand on the tile another one is on.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Two rules and not one: pass-through, and the square hitbox.</strong>
/// The event window help says the pass-through option
/// <c>イベントをすり抜けられるようにします</c> makes an event walk-through, and
/// that an event with it on cannot start unless the player is standing on it —
/// so a transparent sign is a wall you walk through and a thing you can only
/// reach by stepping on it. The same help gives the hitbox:
/// <c>当ﾀﾘ判定■(正方形)</c> makes it one tile, and off it is one tile wide by
/// half a tile high.
/// </para>
/// <para>
/// <strong>Half a tile is not a rounding detail.</strong> The help spells out that
/// the hitbox also changes the contact range, and the contact rules depend on
/// which shape it has — a square event is triggered from the tile above it,
/// a half-height one from beside it.
/// </para>
/// </remarks>
public static class WolfCharacterCollision
{
	/// <summary>One tile, as a half-open range on both axes.</summary>
	public const int TileSize = 1;

	/// <summary>Half a tile, which is the default hitbox height.</summary>
	/// <remarks>
	/// <strong>0.5 and not 0.</strong> The help says the hitbox is off by
	/// default and is <c>横1マス×縦0.5マス</c> then — a figure's feet and not its
	/// whole body. A reader that used 0 would let two figures share a tile
	/// whenever they were only standing next to each other, and a crowd around
	/// a sign would stack.
	/// </remarks>
	public const float DefaultHitboxHeight = 0.5f;

	/// <summary>
	/// Whether two characters' hitboxes overlap.
	/// </summary>
	/// <param name="pOne">The first character.</param>
	/// <param name="pTwo">The second character.</param>
	/// <returns>
	/// False when either is erased, or when one of them passes through.
	/// </returns>
	/// <remarks>
	/// <para>
	/// <strong>Pass-through is checked first, and it is one sided.</strong> A
	/// ghost can walk through a solid, and a solid cannot walk through a ghost
	/// — which is not symmetric, and a reader that checked "either passes
	/// through" would let the hero walk through a wall because a decoration on
	/// that tile is a ghost.
	/// </para>
	/// <para>
	/// <strong>The Y axis counts the height, and it is the one that matters.</strong>
	/// Two figures on the same tile overlap, and a figure on the tile above a
	/// half-height one does not — which is what makes a sign triggerable from
	/// beside it rather than only from on top.
	/// </para>
	/// </remarks>
	public static bool Overlaps(WolfCharacter pOne, WolfCharacter pTwo)
	{
		// **An erased figure is nowhere.** The help's position read returns
		// nothing for a temporarily erased event, and a reader that still
		// collided with it would put a hero stopped by a sign the sign was not
		// showing.
		if (pOne.IsErased || pTwo.IsErased)
		{
			return false;
		}
		// **One sided, and the mover is the one who decides.** The ghost's own
		// flag does not stop anybody from standing where the ghost is; what
		// stops them is whether the *moving* figure is a ghost.
		if (pOne.PassThrough || pTwo.PassThrough)
		{
			return false;
		}
		var oneHeight = HitboxHeight(pOne);
		var twoHeight = HitboxHeight(pTwo);
		// **Half open on X, and that is what keeps a corridor walkable.** A
		// figure standing on tile 2 reaches from 2 to 3, and one on tile 3 from
		// 3 to 4; a closed comparison would have them overlap on the boundary
		// and block every two-tile room in the game.
		//
		// **Closed on Y, and that is the other half.** The hitbox reaches up
		// from the figure's feet, so a square figure on tile 5 occupies 5 to 6
		// and a figure on tile 6 occupies 6 to 6.5 — they meet at six. Whether
		// they collide there is the one rule the help does not spell out, and it
		// is the rule a reader has to choose. **Touching counts here**, because
		// a square figure is a solid object and a solid object that another
		// figure may stand inside is not solid; and a closed Y is also what
		// makes the square option mean anything at all — with a half open Y, a
		// square figure would reach exactly as far as a half height one and the
		// option would be a name for nothing.
		return pOne.X < pTwo.X + TileSize
			&& pTwo.X < pOne.X + TileSize
			&& pOne.Y <= pTwo.Y + twoHeight
			&& pTwo.Y <= pOne.Y + oneHeight;
	}

	/// <summary>
	/// A character's hitbox height, in tiles.
	/// </summary>
	/// <remarks>
	/// <strong>The square option makes it one, and off it is half.</strong> The
	/// help's <c>当ﾀﾘ判定■(正方形)</c> is a square, one tile by one; without it
	/// the hitbox is one wide and half high. A reader that used one for both
	/// would make every half-height figure collide with the figure on the tile
	/// above, and a crowd in a corridor would lock solid.
	/// </remarks>
	public static float HitboxHeight(WolfCharacter pCharacter)
	{
		return pCharacter.SquareHitbox ? TileSize : DefaultHitboxHeight;
	}

	/// <summary>
	/// Whether a character may step onto a tile, given who is already there.
	/// </summary>
	/// <param name="pMover">The character that wants to move.</param>
	/// <param name="pTargetX">The tile it wants to reach.</param>
	/// <param name="pTargetY">The row it wants to reach.</param>
	/// <param name="pOthers">Everyone on the board, including the mover.</param>
	/// <returns>False when another solid figure is in the way.</returns>
	/// <remarks>
	/// <strong>The mover is skipped by identity, not by id.</strong> A
	/// character that has walked to its own tile would otherwise collide with
	/// itself, and a reader that skipped "the one at these coordinates" would
	/// let a figure pass through a ghost standing on the same tile.
	/// </remarks>
	public static bool CanOccupy(
		WolfCharacter pMover,
		int pTargetX,
		int pTargetY,
		IReadOnlyList<WolfCharacter> pOthers)
	{
		// **A ghost is never stopped by anybody,** which is the whole point of
		// the option and the reason the flag is on the mover and not on the
		// target.
		if (pMover.PassThrough)
		{
			return true;
		}
		// **The candidate is the mover, moved.** Building a throwaway character
		// to test would mean copying eleven fields and forgetting one of them
		// the next time a field is added, and the field that matters most —
		// the hitbox — is exactly the one a copy would miss.
		var candidate = pMover.At(pTargetX, pTargetY);
		foreach (var other in pOthers)
		{
			if (ReferenceEquals(other, pMover))
			{
				continue;
			}
			if (Overlaps(candidate, other))
			{
				return false;
			}
		}
		return true;
	}
}
