using System;

namespace UniversalRPG.Rm2k.Simulation;

/// <summary>
/// The damage formula of <c>10500 Simulated Attack</c>, and the generator the
/// variance needs.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Three divisors, and they are not the same.</strong> Defence is
/// divided by 400 and spirit by 800 — so <em>800 points of spirit block twice as
/// much damage as 400 points of defence</em>. A reader that used one divisor for
/// both would make spirit twice as strong as the game meant it, and every
/// damage number in a game using this command would be wrong.
/// </para>
/// <para>
/// <strong>The result is floored at zero twice, and the order matters.</strong>
/// The reference clamps after the two subtractions, adjusts the variance, and
/// clamps again — because a variance draw can push a small result below zero.
/// A reader that clamped once, at the end, would let a variance of a negative
/// intermediate hand out negative damage and heal the target.
/// </para>
/// <para>
/// The generator is the same shape as <c>MzRandom</c> and is deliberately not a
/// claim about the engine's numbers: <strong>the engine's are not
/// repeatable</strong>. What it gives is a run that can be replayed, which is
/// what a save file and a test both need.
/// </para>
/// </remarks>
public static class Rm2kSimulatedAttack
{
	/// <summary>The divisor the reference uses for defence.</summary>
	public const int DefenceDivisor = 400;

	/// <summary>The divisor the reference uses for spirit.</summary>
	public const int SpiritDivisor = 800;

	/// <summary>
	/// The damage one attack does, from the reference's body.
	/// </summary>
	/// <param name="pAttack">The attack value, <c>parameters[2]</c>.</param>
	/// <param name="pDefence">The defender's base defence.</param>
	/// <param name="pDefenceRate">
	/// How much of the defence counts, <c>parameters[3]</c>.
	/// </param>
	/// <param name="pSpirit">The defender's base spirit.</param>
	/// <param name="pSpiritRate">
	/// How much of the spirit counts, <c>parameters[4]</c>.
	/// </param>
	/// <param name="pVariance">
	/// The variance in tenths, <c>parameters[5]</c>. Zero means none.
	/// </param>
	/// <param name="pRandom">The generator, for the variance only.</param>
	public static int Compute(
		int pAttack, int pDefence, int pDefenceRate,
		int pSpirit, int pSpiritRate, int pVariance, Rm2kDamageRandom pRandom)
	{
		var result = pAttack;
		// **Integer division on both, and the two divisors differ.** A reader
		// that divided once, by a shared value, would make spirit half as
		// strong as the game meant it.
		result -= pDefence * pDefenceRate / DefenceDivisor;
		result -= pSpirit * pSpiritRate / SpiritDivisor;
		// **The first clamp.** The reference does this before the variance, and
		// it is what keeps a hopeless attack at zero rather than negative.
		result = Math.Max(result, 0);
		// The variance, which the reference only applies to a positive result.
		result = VarianceAdjustEffect(result, pVariance, pRandom);
		// **The second clamp**, because a variance draw can go below zero.
		return Math.Max(0, result);
	}

	/// <summary>
	/// The variance, from <c>Algo::VarianceAdjustEffect</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>Only a positive variance on a positive base does anything.</strong>
	/// The reference guards it with <c>if (var &gt; 0 &amp;&amp; base &gt; 0)</c>,
	/// and a zero base stays zero however large the variance is — which is what
	/// makes an attack that a defender fully blocks a guaranteed zero.
	/// </para>
	/// <para>
	/// <strong>The spread is <c>var * base / 10</c> and at least one.</strong> The
	/// <c>max(1, ...)</c> matters for a small base with a large variance: without
	/// it the spread would round to zero and a game that asked for 10 percent
	/// variance would get none.
	/// </para>
	/// <para>
	/// The draw is <c>0 .. spread</c> and the result is
	/// <c>base + draw - spread / 2</c>, so the spread is symmetric around the
	/// base and its half is subtracted — not the whole spread.
	/// </para>
	/// </remarks>
	public static int VarianceAdjustEffect(
		int pBase, int pVariance, Rm2kDamageRandom pRandom)
	{
		// The reference does not apply variance to a zero or negative base.
		if (pVariance <= 0 || pBase <= 0)
		{
			return pBase;
		}
		// **The spread is at least one**, so a small base with a large variance
		// still varies.
		var spread = Math.Max(1, pVariance * pBase / 10);
		// **Half the spread is subtracted, not all of it**, which is what makes
		// the variance symmetric rather than a downward bias.
		return pBase + pRandom.Next(spread + 1) - spread / 2;
	}
}

/// <summary>
/// The generator <c>10500</c> uses for its variance.
/// </summary>
/// <remarks>
/// <para>
/// <strong>The same shape as <c>MzRandom</c> and deliberately not a claim about
/// the engine's numbers</strong> — the engine's are not repeatable. What this
/// gives is a run that can be replayed, which is what a save file needs and what
/// a test needs to prove a formula at all.
/// </para>
/// <para>
/// A separate class and not a shared static, because <strong>MZ and RM2K do not
/// share a stream</strong>: a game that uses both should not have one command's
/// draws move the other's.
/// </para>
/// </remarks>
public sealed class Rm2kDamageRandom
{
	private int _state = 1;

	/// <summary>Draws the next number up to and including a bound.</summary>
	/// <param name="pMax">
	/// The exclusive upper bound plus one; the reference draws from
	/// <c>Rand::GetRandomNumber(0, adj)</c>, which includes both ends.
	/// </param>
	public int Next(int pMax)
	{
		if (pMax <= 1)
		{
			return 0;
		}
		_state = (int)((uint)(_state * 1103515245 + 12345) & 0x7fffffff);
		return _state % pMax;
	}

	/// <summary>Sets the starting point, so a caller can replay a known run.</summary>
	public void Seed(int pState) => _state = pState == 0 ? 1 : pState;
}
