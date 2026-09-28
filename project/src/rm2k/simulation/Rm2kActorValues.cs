using System;

namespace UniversalRPG.Rm2k.Simulation;

/// <summary>
/// One actor's battle values, from the base set the database gives and the
/// changes the commands make.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Base and current are not the same thing, and only the base is
/// stored here.</strong> The reference keeps both — <c>SetBaseMaxHp</c> and
/// <c>SetMaxHp</c> are different calls — because a command can change either.
/// <c>10430</c> changes the <em>base</em>, which is what survives a level change
/// and a save, while a temporary buff changes the current value and must not.
/// </para>
/// <para>
/// So a current value is computed from the base plus whatever equipment and
/// temporary modifiers the game applies, and this class deliberately has no
/// field for it. <strong>A reader that stored the current value would let a
/// saved game keep a buff that ended three maps ago.</strong>
/// </para>
/// <para>
/// The bounds are the format's: HP and SP up to 9999, the others up to 999, and
/// no floor below 1 for HP and SP because a living actor has at least one.
/// </para>
/// </remarks>
public sealed class Rm2kActorValues
{
	/// <summary>The bound liblcf gives max HP and max SP.</summary>
	public const int MaxHitPoints = 9999;

	/// <summary>The bound liblcf gives every other base value.</summary>
	public const int MaxStat = 999;

	/// <summary>The base maximum hit points, from the actor's database entry.</summary>
	public int BaseMaxHp { get; private set; }

	/// <summary>The base maximum skill points.</summary>
	public int BaseMaxSp { get; private set; }

	/// <summary>The base attack.</summary>
	public int BaseAttack { get; private set; }

	/// <summary>The base defence.</summary>
	public int BaseDefense { get; private set; }

	/// <summary>The base spirit.</summary>
	public int BaseSpirit { get; private set; }

	/// <summary>The base agility.</summary>
	public int BaseAgility { get; private set; }

	/// <summary>
	/// A fresh actor, from the bounds liblcf writes into an empty database row.
	/// </summary>
	/// <remarks>
	/// These are the format's minimums and not a game's design: a database row
	/// that says otherwise supplies its own values, and a command changes them
	/// from whatever was there. <strong>Starting at zero would make a change of
	/// minus one go to a negative base</strong>, which the reference clamps and
	/// this does not.
	/// </remarks>
	public Rm2kActorValues()
	{
		BaseMaxHp = 1;
		BaseMaxSp = 0;
		BaseAttack = 1;
		BaseDefense = 1;
		BaseSpirit = 1;
		BaseAgility = 1;
	}

	/// <summary>
	/// The six values <c>10430 Change Parameters</c> addresses, in the
	/// reference's switch order.
	/// </summary>
	public const int ParameterMaxHp = 0;
	public const int ParameterMaxSp = 1;
	public const int ParameterAttack = 2;
	public const int ParameterDefense = 3;
	public const int ParameterSpirit = 4;
	public const int ParameterAgility = 5;

	/// <summary>
	/// Adds to one base value, from <c>SetBaseMaxHp</c> and its five siblings.
	/// </summary>
	/// <remarks>
	/// <strong>Every one of them clamps the same way, and the clamp is the
	/// command.</strong> A reader that stored a negative base would hand a game
	/// an actor it cannot kill, and one that let it grow past the bound would
	/// write a number the format cannot hold.
	/// </remarks>
	/// <returns>False when the parameter named none of the six.</returns>
	public bool AddToParameter(int pParameter, int pDelta)
	{
		switch (pParameter)
		{
			case ParameterMaxHp:
				BaseMaxHp = Clamp(BaseMaxHp + pDelta, 1, MaxHitPoints);
				return true;
			case ParameterMaxSp:
				BaseMaxSp = Clamp(BaseMaxSp + pDelta, 0, MaxHitPoints);
				return true;
			case ParameterAttack:
				BaseAttack = Clamp(BaseAttack + pDelta, 1, MaxStat);
				return true;
			case ParameterDefense:
				BaseDefense = Clamp(BaseDefense + pDelta, 1, MaxStat);
				return true;
			case ParameterSpirit:
				BaseSpirit = Clamp(BaseSpirit + pDelta, 1, MaxStat);
				return true;
			case ParameterAgility:
				BaseAgility = Clamp(BaseAgility + pDelta, 1, MaxStat);
				return true;
			default:
				return false;
		}
	}

	/// <summary>
	/// Reads one base value by its parameter number, for a caller that wants to
	/// report the result.
	/// </summary>
	public int GetParameter(int pParameter)
	{
		return pParameter switch
		{
			ParameterMaxHp => BaseMaxHp,
			ParameterMaxSp => BaseMaxSp,
			ParameterAttack => BaseAttack,
			ParameterDefense => BaseDefense,
			ParameterSpirit => BaseSpirit,
			ParameterAgility => BaseAgility,
			_ => 0,
		};
	}

	/// <summary>
	/// Clamps a hit point count to what a body can hold, from
	/// <c>Game_Actor::ChangeHp</c>.
	/// </summary>
	/// <remarks>
	/// <strong>The ceiling is the current maximum, not the base maximum</strong>,
	/// and that is the whole reason the base lives in its own place: a hero
	/// with a base of 40 and equipment worth 10 cannot be healed past 50, and a
	/// reader that clamped to the base would stop the heal at 40.
	/// </remarks>
	/// <param name="pCurrentHp">Where the actor is now.</param>
	/// <param name="pDelta">
	/// The change, already signed: negative for the reference's
	/// <c>remove</c> branch.
	/// </param>
	/// <param name="pCurrentMaxHp">
	/// The current maximum, which this reader does not store.
	/// </param>
	/// <param name="pLethal">
	/// Whether the change may kill, from <c>parameters[5]</c>. A non-lethal
	/// change stops at one hit point.
	/// </param>
	public static int ChangeHp(
		int pCurrentHp, int pDelta, int pCurrentMaxHp, bool pLethal)
	{
		var hp = pCurrentHp + pDelta;
		if (!pLethal && hp < 1)
		{
			// **A non-lethal change stops at one, and the reference says so with
			// its own comment.** A reader that let it reach zero would kill a
			// hero a game had explicitly protected.
			return 1;
		}
		return Math.Clamp(hp, 0, pCurrentMaxHp);
	}

	/// <summary>
	/// Clamps a skill point count, from <c>CommandChangeSP</c>.
	/// </summary>
	/// <remarks>
	/// <strong>Skill points clamp at zero and have no floor of one</strong> —
	/// the reference writes <c>if (sp &lt; 0) sp = 0;</c> and no lethal flag.
	/// HP and SP are not symmetric here, and a reader that gave SP the same
	/// floor as HP would leave a hero unable to cast anything.
	/// </remarks>
	public static int ChangeSp(int pCurrentSp, int pDelta, int pCurrentMaxSp)
	{
		return Math.Clamp(pCurrentSp + pDelta, 0, pCurrentMaxSp);
	}

	private static int Clamp(int pValue, int pMin, int pMax)
	{
		return Math.Clamp(pValue, pMin, pMax);
	}
}
