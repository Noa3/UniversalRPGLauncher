using System;
using System.Collections.Generic;

namespace UniversalRPG.Wolf;

/// <summary>
/// Resolves the target numbers the help lists for a character command.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Negative numbers are roles and positive ones are event ids.</strong>
/// The help's target list is explicit:
/// <c>0以上の場合 ＝ その値のIDを持つイベント</c>, <c>-1＝このイベント</c>,
/// <c>-2＝主人公(隊列先頭)</c>, and <c>-3</c> through <c>-7</c> for the first
/// through fifth companion.
/// </para>
/// <para>
/// <strong>The hero is -2 and not 0.</strong> Zero is a valid event id and the
/// board's own convention for the hero, so a reader that used 0 for "the hero"
/// would answer a command about event 0 with the player and a command about the
/// player with event 0 — and both of those exist in a real map.
/// </para>
/// </remarks>
public static class WolfCharacterTarget
{
	/// <summary>This event, and for a common event the caller.</summary>
	public const int ThisEvent = -1;

	/// <summary>The hero, at the head of the party.</summary>
	public const int Hero = -2;

	/// <summary>The first companion.</summary>
	public const int Companion1 = -3;

	/// <summary>The fifth and last companion.</summary>
	public const int Companion5 = -7;

	/// <summary>The lowest target number the help offers.</summary>
	public const int MinTarget = Companion5;

	/// <summary>
	/// What a target number means, before anyone is looked up.
	/// </summary>
	/// <remarks>
	/// <strong>Three answers and not two.</strong> A role, an event id, or
	/// nothing — and a reader that treated a role as an id would look for an
	/// event numbered minus two, find none, and answer "nowhere", which is how
	/// a hero who walks to a companion ends up walking to the wall instead.
	/// </remarks>
	public enum Kind
	{
		/// <summary>This event, or its caller.</summary>
		ThisEvent,

		/// <summary>The hero.</summary>
		Hero,

		/// <summary>One of the five companions.</summary>
		Companion,

		/// <summary>An event by its id.</summary>
		EventId,

		/// <summary>A number the help does not offer.</summary>
		Unknown,
	}

	/// <summary>What a target number means.</summary>
	public static Kind Classify(int pTarget)
	{
		if (pTarget >= 0)
		{
			return Kind.EventId;
		}
		return pTarget switch
		{
			ThisEvent => Kind.ThisEvent,
			Hero => Kind.Hero,
			// **The bounds in the order the compiler can read them.** The help
			// counts down from -2, so Companion1 is -3 and Companion5 is -7, and
			// the range written as "greater than -3 and less than -7" is empty —
			// a reader that wrote the two the other way round classified every
			// companion target as unknown, and a guard walking to the third
			// companion would have stood still.
			>= Companion5 and <= Companion1 => Kind.Companion,
			_ => Kind.Unknown,
		};
	}

	/// <summary>
	/// The companion a target names, counting from one, or -1.
	/// </summary>
	/// <remarks>
	/// <strong>One based, and -2 is the hero and not a companion.</strong> The
	/// help counts the companions from one, and a reader that counted from the
	/// negative number would call -3 companion zero and give the party a member
	/// who is not on the map.
	/// </remarks>
	public static int CompanionNumber(int pTarget)
	{
		// **The bounds the way the numbers run**, and this is the second time
		// this card got it wrong. The help counts down: -3 is the first
		// companion and -7 the fifth, so "at least -3 and at most -7" is an
		// empty range. An earlier version wrote it that way and answered -1 for
		// every companion — and the compiler said nothing, because a range that
		// is always false is perfectly valid C#.
		//
		// **Read it as "between the fifth and the first",** which is the order
		// the numbers appear in, and the subtraction counts up from one.
		return pTarget >= Companion5 && pTarget <= Companion1
			? Companion1 - pTarget + 1
			: -1;
	}
}

/// <summary>
/// The party, so a companion target has something to name.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Five slots and not a list.</strong> The help's target list stops at
/// <c>-7</c> for the fifth companion, so a party has at most five members
/// besides the hero. A reader that grew a list would answer a target of -8 with
/// a sixth companion the editor cannot name.
/// </para>
/// <para>
/// <strong>The hero is at the head of the party and is also slot zero</strong>,
/// which is the help's own <c>主人公(隊列先頭)</c> — the hero as the head of the
/// column. A member that has left the party is a gap, not a shift, so the
/// numbers after it do not move.
/// </para>
/// </remarks>
public sealed class WolfParty
{
	/// <summary>The most companions the help's target list names.</summary>
	public const int MaxCompanions = 5;

	/// <summary>Whether a companion slot is filled.</summary>
	private readonly WolfCharacter?[] _members = new WolfCharacter?[MaxCompanions];

	/// <summary>The hero, who is not a companion.</summary>
	public WolfCharacter? Hero { get; set; }

	/// <summary>How many companions are in the party.</summary>
	public int CompanionCount
	{
		get
		{
			var count = 0;
			foreach (var member in _members)
			{
				if (member != null)
				{
					count++;
				}
			}
			return count;
		}
	}

	/// <summary>Whether a companion slot is filled.</summary>
	public bool HasCompanion(int pNumber)
	{
		return pNumber >= 1
			&& pNumber <= MaxCompanions
			&& _members[pNumber - 1] != null;
	}

	/// <summary>Reads a companion, or null where the slot is empty.</summary>
	/// <remarks>
	/// <strong>An empty slot is null and not a fresh character.</strong> A
	/// reader that invented one would put a companion on the map that the party
	/// does not have, and a guard that approached it would walk to nobody.
	/// </remarks>
	public WolfCharacter? Companion(int pNumber)
	{
		return pNumber >= 1 && pNumber <= MaxCompanions
			? _members[pNumber - 1]
			: null;
	}

	/// <summary>Puts a companion into a slot, replacing whoever was there.</summary>
	/// <returns>False when the number is outside one to five.</returns>
	public bool SetCompanion(int pNumber, WolfCharacter? pCharacter)
	{
		if (pNumber < 1 || pNumber > MaxCompanions)
		{
			return false;
		}
		_members[pNumber - 1] = pCharacter;
		return true;
	}

	/// <summary>Empties the party, for a new game.</summary>
	public void Clear()
	{
		Array.Clear(_members);
		Hero = null;
	}
}
