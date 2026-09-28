using System;

using UniversalRPG.Rm2k;
using UniversalRPG.Rm2k.Interpreter;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Command 10500 Simulated Attack.
/// </summary>
/// <remarks>
/// <para>
/// <strong>It is not a battle.</strong> No turn order, no troop, no target
/// selection — the command picks heroes, computes one number from their defence
/// and spirit, and subtracts it. This slice did not have to build a combat
/// system first, and that is worth knowing before the next one does.
/// </para>
/// <para>
/// The three things a reader gets wrong are the two divisors, the two clamps,
/// and the result variable.
/// </para>
/// </remarks>
public partial class TestRm2kSimulatedAttack : TestBase
{
	/// <summary>
	/// <c>[actorMode, actorId, attack, defRate, spiritRate, variance,
	/// storeResult, resultVar]</c>.
	/// </summary>
	private static Rm2kMap.EventCommand Attack(
		int pAttack, int pDefRate = 0, int pSpiritRate = 0, int pVariance = 0,
		int pStore = 0, int pResultVar = 0, int pActorId = 1, int pActorMode = 1)
	{
		return new Rm2kMap.EventCommand
		{
			Code = EventInterpreter.SimulatedAttack,
			Text = "",
			Parameters =
				[pActorMode, pActorId, pAttack, pDefRate, pSpiritRate,
					pVariance, pStore, pResultVar],
		};
	}

	private static (EventInterpreter Interpreter, GameSimulationState State) Run(
		int pDefence = 0, int pSpirit = 0, int pMaxHp = 0,
		params Rm2kMap.EventCommand[] pCommands)
	{
		var state = new GameSimulationState { MapId = 1 };
		for (var i = 0; i < 10; i++)
		{
			state.Variables.Add(0);
		}
		var values = new Rm2kActorValues();
		values.AddToParameter(Rm2kActorValues.ParameterDefense, pDefence);
		values.AddToParameter(Rm2kActorValues.ParameterSpirit, pSpirit);
		values.AddToParameter(Rm2kActorValues.ParameterMaxHp, pMaxHp);
		state.ActorValues[1] = values;
		state.CurrentHp[1] = pMaxHp;
		var interpreter = new EventInterpreter(state, 1, pCommands);
		return (interpreter, state);
	}

	private static int Damage(
		int pAttack, int pDefence, int pDefRate, int pSpirit, int pSpiritRate,
		int pVariance = 0, int pSeed = 1)
	{
		var random = new Rm2kDamageRandom();
		random.Seed(pSeed);
		return Rm2kSimulatedAttack.Compute(
			pAttack, pDefence, pDefRate, pSpirit, pSpiritRate, pVariance, random);
	}

	// ---- Die Formel

	/// <summary>
	/// Defence is divided by 400 and spirit by 800.
	/// </summary>
	/// <remarks>
	/// <strong>So 800 points of spirit block twice as much as 400 points of
	/// defence.</strong> A reader that used one divisor for both would make spirit
	/// twice as strong as the game meant it, and every damage number in a game
	/// using this command would be wrong — by a factor of two, on the axis a
	/// game tunes.
	/// </remarks>
	public void Test_DefenceAndSpiritUseDifferentDivisors()
	{
		// **A rate of 1, and not 100.** At a hundred percent every value in
		// this test blocks the whole attack, all three cases come out as zero,
		// and a test that cannot tell the divisors apart proves nothing about
		// them. At one percent the two divisors separate cleanly.
		AssertEq(
			Damage(pAttack: 20, pDefence: 400, pDefRate: 1, pSpirit: 0,
				pSpiritRate: 0),
			19,
			"**and 400 defence at one percent costs one point**, because the"
			+ $" divisor is 400; the damage is 19");

		AssertEq(
			Damage(pAttack: 20, pDefence: 800, pDefRate: 1, pSpirit: 0,
				pSpiritRate: 0),
			18,
			"**and 800 defence at the same rate costs two**, which is the"
			+ " linear half of the divisor; the damage is 18");

		AssertEq(
			Damage(pAttack: 20, pDefence: 0, pDefRate: 0, pSpirit: 400,
				pSpiritRate: 1),
			20,
			"**and 400 spirit at one percent costs nothing**, because the"
			+ " divisor is 800 and 400 over 800 rounds to zero — this is the"
			+ $" case a shared divisor would get wrong; the damage is 20");

		AssertEq(
			Damage(pAttack: 20, pDefence: 0, pDefRate: 0, pSpirit: 800,
				pSpiritRate: 1),
			19,
			"**and 800 spirit costs one, the same as 400 defence** — so 800"
			+ " points of spirit block exactly as much as 400 points of"
			+ $" defence, which is the whole difference; the damage is 19");

		AssertEq(
			Damage(pAttack: 20, pDefence: 0, pDefRate: 0, pSpirit: 1600,
				pSpiritRate: 1),
			18,
			"**and 1600 spirit costs two**, the same as 800 defence; the"
			+ $" damage is 18");
	}

	/// <summary>
	/// The result is floored at zero twice, and a variance cannot make it
	/// negative.
	/// </summary>
	/// <remarks>
	/// The reference clamps after the two subtractions, adjusts the variance and
	/// clamps again. <strong>A reader that clamped once, at the end, would let a
	/// variance draw on a small result hand out negative damage</strong> — and
	/// negative damage added to the hit points <em>heals</em> the hero the
	/// command was aimed at.
	/// </remarks>
	public void Test_TheResultIsFlooredAtZero()
	{
		AssertEq(
			Damage(pAttack: 5, pDefence: 1000, pDefRate: 100, pSpirit: 1000,
				pSpiritRate: 100),
			0,
			"**and an attack that is fully blocked is zero**, because the first"
			+ " clamp is before the variance; the damage is 0");

		// With a variance, the second clamp is what holds it at zero.
		var random = new Rm2kDamageRandom();
		random.Seed(3);
		for (var i = 0; i < 20; i++)
		{
			var wert = Rm2kSimulatedAttack.Compute(
				1, 0, 100, 0, 100, 90, random);
			AssertTrue(
				wert >= 0,
				$"**and a variance never pushes a result below zero**, because the"
				+ $" reference clamps twice; the draw gave {wert}");
		}
	}

	/// <summary>
	/// A variance only touches a positive result.
	/// </summary>
	/// <remarks>
	/// The reference guards with <c>if (var &gt; 0 &amp;&amp; base &gt; 0)</c>.
	/// <strong>A zero base stays zero however large the variance is</strong>, and
	/// that is what makes an attack a defender fully blocks a guaranteed zero
	/// rather than a small random number.
	/// </remarks>
	public void Test_AVarianceOnlyTouchesAPositiveResult()
	{
		AssertEq(
			Damage(pAttack: 0, pDefence: 0, pDefRate: 0, pSpirit: 0,
				pSpiritRate: 0, pVariance: 100),
			0,
			"**and a base of zero stays zero at a hundred percent variance**,"
			+ " because the reference guards on the base; the damage is 0");
	}

	/// <summary>
	/// A large variance on a small base still varies.
	/// </summary>
	/// <remarks>
	/// The reference writes <c>std::max(1, var * base / 10)</c>. <strong>Without
	/// the one, the spread rounds to zero</strong> and a game that asked for ten
	/// percent variance would get none — the exact opposite of what it asked
	/// for.
	/// </remarks>
	public void Test_ASmallBaseStillVaries()
	{
		var gesehen = new System.Collections.Generic.HashSet<int>();
		var random = new Rm2kDamageRandom();
		random.Seed(7);
		for (var i = 0; i < 30; i++)
		{
			gesehen.Add(Rm2kSimulatedAttack.Compute(
				1, 0, 100, 0, 100, 50, random));
		}
		AssertTrue(
			gesehen.Count > 1,
			$"**and a base of one with fifty percent variance still varies**,"
			+ $" because the spread is at least one; the draws gave {gesehen.Count}"
			+ " different values");
	}

	// ---- Der Befehl

	/// <summary>
	/// The damage comes off the hero's hit points.
	/// </summary>
	/// <remarks>
	/// <strong>It is subtracted, not added.</strong> A reader that added it
	/// would heal the hero the command was aimed at, and the diagnostic would
	/// still say the right number.
	/// </remarks>
	public void Test_TheDamageComesOffTheHitPoints()
	{
		var (interpreter, state) = Run(
			pDefence: 0, pSpirit: 0, pMaxHp: 200,
			Attack(pAttack: 30));

		interpreter.ExecuteFrame();

		AssertEq(
			state.GetActorCurrentHp(1), 170,
			"**and 200 hit points become 170**, because the damage is a loss"
			+ $" and not a gain; they are {state.GetActorCurrentHp(1)}");
	}

	/// <summary>
	/// The result variable holds the last actor's damage and not the sum.
	/// </summary>
	/// <remarks>
	/// The reference writes it <strong>inside</strong> the loop, so a command
	/// against a party leaves the last actor's damage in it. <em>A reader that
	/// summed would make a game that shows "you took N" show a number the game
	/// never produced</em> — and the game would be consistent about it, because
	/// it is the same wrong number every time.
	/// </remarks>
	public void Test_TheResultVariableHoldsTheLastActorsDamage()
	{
		var state = new GameSimulationState { MapId = 1 };
		for (var i = 0; i < 10; i++)
		{
			state.Variables.Add(0);
		}
		// Two heroes with different defences, both in the party range.
		for (var id = 1; id <= 2; id++)
		{
			var v = new Rm2kActorValues();
			// **The add is relative, not absolute.** The class starts at the format
			// minimum of 1, so 200 means 201 — and a test that set the
			// maximum to a different number than the current hit points would
			// clamp every change to zero.
			v.AddToParameter(Rm2kActorValues.ParameterMaxHp, 199);
			// Hero 2 has defence, hero 1 does not, so the two differ.
			v.AddToParameter(Rm2kActorValues.ParameterDefense, id == 2 ? 400 : 0);
			state.ActorValues[id] = v;
			state.CurrentHp[id] = 200;
			// **The party is what mode 0 means**, and a test that left it
			// empty would resolve to no actors and prove nothing.
			state.PartyMemberIds.Add(id);
		}
		var interpreter = new EventInterpreter(state, 1, [
			new Rm2kMap.EventCommand
			{
				Code = EventInterpreter.SimulatedAttack,
				Text = "",
				// **Rate 1 and not 100.** At a hundred percent both terms
				// block the whole attack and every hero takes zero, which would
				// make this test pass with a formula that never subtracts
				// anything.
				Parameters = [0, 0, 20, 1, 1, 0, 1, 3],
			},
		]);

		interpreter.ExecuteFrame();

		AssertEq(
			state.Variables[2], 19,
			"**and variable 3 holds 19, the second hero's damage**, because the"
			+ " reference writes the variable inside the loop and the second"
			+ " hero is the one with the 400 defence that costs one point;"
			+ $" it is {state.Variables[2]}");
		AssertEq(
			state.GetActorCurrentHp(1), 180,
			"**and the first hero lost 20**, because it has no defence at all"
			+ " and the attack is twenty; the"
			+ $" hit points are {state.GetActorCurrentHp(1)}");
		AssertEq(
			state.GetActorCurrentHp(2), 181,
			"**and the second lost 19**, which is the number in the variable"
			+ " and not the total of the two;"
			+ $" the hit points are {state.GetActorCurrentHp(2)}");
	}

	/// <summary>
	/// A cleared sixth parameter stores nothing.
	/// </summary>
	/// <remarks>
	/// The reference checks <c>parameters[6] != 0</c>, and a cleared flag means
	/// <em>no variable at all</em> — not variable zero.
	/// </remarks>
	public void Test_AClearedFlagStoresNothing()
	{
		var (interpreter, state) = Run(
			pDefence: 0, pSpirit: 0, pMaxHp: 200,
			Attack(pAttack: 30, pStore: 0, pResultVar: 3));

		interpreter.ExecuteFrame();

		AssertEq(
			state.Variables[2], 0,
			"**and variable 3 is still zero**, because the sixth parameter was"
			+ $" cleared and the reference then does not write; it is {state.Variables[2]}");
		AssertEq(
			state.GetActorCurrentHp(1), 170,
			"**and the damage still happened**, because the flag is about the"
			+ $" variable and not about the attack; the hit points are {state.GetActorCurrentHp(1)}");
	}

	/// <summary>
	/// A result variable outside the bounds changes nothing at all.
	/// </summary>
	public void Test_AnOutOfBoundsResultVariableChangesNothing()
	{
		var (interpreter, state) = Run(
			pDefence: 0, pSpirit: 0, pMaxHp: 200,
			Attack(pAttack: 30, pStore: 1, pResultVar: 0));

		interpreter.ExecuteFrame();

		AssertEq(
			state.GetActorCurrentHp(1), 200,
			"**and no damage was dealt**, because the variable is checked before"
			+ $" the first hero and a refused command changes nothing; the hit"
			+ $" points are {state.GetActorCurrentHp(1)}");
		AssertTrue(
			ContainsDiagnostic(state, "outside 1 to"),
			"and the diagnostic names the bound; the diagnostics are"
			+ $" {state.Diagnostics.Count}");
	}

	private static bool ContainsDiagnostic(
		GameSimulationState pState, string pNeedle)
	{
		foreach (var line in pState.Diagnostics)
		{
			if (line.Contains(pNeedle, StringComparison.Ordinal))
			{
				return true;
			}
		}
		return false;
	}
}
