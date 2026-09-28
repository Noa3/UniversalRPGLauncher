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

	/// <summary>The name a hero is called by, from <c>10620</c>.</summary>
	/// <remarks>
	/// <strong>Separate from the database name</strong>, because that is what
	/// the reference does: <c>SetTitle</c> is its own field, and a game that
	/// gives a hero a title keeps the database name for the party window.
	/// </remarks>

	/// <summary>
	/// The sentinel the reference uses for "this hero still answers to the
	/// name the database gives".
	/// </summary>
	/// <remarks>
	/// <strong>An empty string is not the same thing.</strong> The reference's
	/// <c>SetName</c> writes <c>kEmptyName</c> when the new name equals the
	/// database's, and its <c>GetName</c> falls back to the database only for
	/// that one value. <strong>A reader that stored "" for "unchanged" would
	/// have made a hero nameless</strong> — and a save written with "" would
	/// have lost the distinction between "never renamed" and "renamed to
	/// nothing", which the reference can tell apart and a save file cannot
	/// without the sentinel.
	/// </remarks>
	public const string UnchangedName = "\u0000";

	/// <summary>
	/// The name a hero answers to, from <c>10740</c> Enter Hero Name, and the
	/// sentinel when the hero still answers to the database's name.
	/// </summary>
	/// <remarks>
	/// <strong>Only a name that differs from the database's is stored</strong>,
	/// and that is the reference's own rule in
	/// <c>Game_Actor::SetName</c>:
	/// <c>data.name = (new_name != dbActor-&gt;name) ? new_name :
	/// kEmptyName</c>. <strong>A reader that stored every name would have put
	/// a hero's original name into every save file</strong> — which is not
	/// only wasteful but <em>wrong</em>: rename a hero in the editor and an
	/// old save would keep the old name instead of the new one.
	/// </remarks>
	public string Name { get; set; } = UnchangedName;

	/// <summary>
	/// Gives a hero a name, and stores nothing when it is the one the
	/// database already gives.
	/// </summary>
	/// <param name="pName">The name the player typed or chose.</param>
	/// <param name="pDatabaseName">The name that hero's database entry
	/// carries.</param>
	/// <remarks>
	/// <para>
	/// <strong>This is the reference's own rule, in
	/// <c>Game_Actor::SetName</c>:</strong>
	/// <c>data.name = (new_name != dbActor-&gt;name) ? new_name :
	/// lcf::rpg::SaveActor::kEmptyName</c>. <strong>Only a name that differs
	/// from the database's is kept</strong> — and that is not a
	/// simplification. A save file that carried the database name into every
	/// hero would keep the <em>old</em> name after the game was renamed in
	/// the editor, <strong>and the reference's save format has a sentinel
	/// precisely so that distinction survives.</strong>
	/// </para>
	/// <para>
	/// <strong>An empty name is a real name and not the sentinel.</strong> A
	/// player may call a hero nothing, and the reference stores that as a
	/// name of zero length — <strong>while the sentinel is a different value
	/// entirely.</strong> A reader that used "" for "unchanged" would have
	/// made every renamed hero nameless the moment the save was written.
	/// </para>
	/// </remarks>
	public void SetName(string pName, string pDatabaseName)
	{
		Name = pName != pDatabaseName ? pName : UnchangedName;
	}

	/// <summary>
	/// The name a hero answers to, given the name its database entry carries.
	/// </summary>
	/// <param name="pDatabaseName">The name that hero's database entry
	/// carries.</param>
	/// <returns>The saved name, or the database's when none was saved.</returns>
	/// <remarks>
	/// <strong>The mirror of <see cref="SetName"/> and its own rule:</strong>
	/// the reference's <c>GetName</c> falls back to the database only for
	/// <c>kEmptyName</c> and for nothing else. <strong>A reader that fell back
	/// for every empty string would have answered with the database's name
	/// for a hero the player deliberately named nothing</strong>.
	/// </remarks>
	public string ResolveName(string pDatabaseName)
	{
		return Name != UnchangedName ? Name : pDatabaseName;
	}

	/// <summary>
	/// Whether the name screen for this hero should offer the database name.
	/// </summary>
	/// <remarks>
	/// <strong>This is <c>10740</c>'s third parameter and it is a flag and
	/// not a text.</strong> The reference passes it as
	/// <c>use_default_name</c> to its name scene, and a reader that treated it
	/// as part of the name would have shown every hero a name that ends in a
	/// digit.
	/// </remarks>
	public bool OfferDefaultName { get; set; }

	/// <summary>
	/// The face index the name screen shows, from <c>10740</c>'s second
	/// parameter.
	/// </summary>
	/// <remarks>
	/// <strong>An index and not a file name.</strong> The reference's
	/// <c>Scene_Name</c> takes an <c>int</c>, because a face is a position in
	/// the actor's own face set. <strong>A reader that read the parameter as
	/// a file name would have looked for a charset called "2"</strong> and
	/// found nothing, and a game's naming screen would have shown no face at
	/// all.
	/// </remarks>
	public int NameCharsetIndex { get; set; }
	public string Title { get; set; } = "";

	/// <summary>The walk sprite a hero wears, from <c>10630</c>.</summary>
	/// <remarks>
	/// <strong>The index is a walk-cycle offset and not a character number.</strong>
	/// A costume is the same file with a different index, and a reader that
	/// treated the index as a character number would put a hero in somebody
	/// else's costume.
	/// </remarks>
	public string SpriteName { get; set; } = "";

	/// <summary>Which pose of that file the hero wears.</summary>
	public int SpriteIndex { get; set; }

	/// <summary>Whether the hero is drawn transparent, from <c>parameters[2]</c>.</summary>
	public bool SpriteTransparent { get; set; }

	/// <summary>The face a hero shows in a message, from <c>10640</c>.</summary>
	/// <remarks>
	/// <strong>A request and not a drawn portrait</strong>, like <c>10130</c>:
	/// nothing here loads a file.
	/// </remarks>
	public string FaceName { get; set; } = "";

	/// <summary>Which of the four faces in the file.</summary>
	public int FaceIndex { get; set; }

	/// <summary>The highest face index a file's four slots allow.</summary>
	public const int MaxFaceIndex = 3;

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
	/// <summary>
	/// Writes one base value, from <c>1008</c> Change Class.
	/// </summary>
	/// <param name="pParameter">The parameter number, zero to five.</param>
	/// <param name="pValue">The value to write, before the clamp.</param>
	/// <returns>False when the parameter named none of the six.</returns>
	/// <remarks>
	/// <strong>Assignment and not addition.</strong>
	/// <c>AddToParameter</c> exists for <c>10430</c>, which changes a value by
	/// a difference; the class change writes the numbers the reference secured
	/// at the top of <c>Game_Actor::ChangeClass</c> and puts back at the
	/// bottom, after the parameter mode has halved them.
	/// <strong>A reader that added a difference here would have doubled a
	/// hero's statistics on every class change</strong>, and a game that
	/// changes a class in a loop would have run the party's hit points away.
	/// </remarks>
	public bool SetBaseParameter(int pParameter, int pValue)
	{
		switch (pParameter)
		{
			case ParameterMaxHp:
				BaseMaxHp = Clamp(pValue, 1, MaxHitPoints);
				return true;
			case ParameterMaxSp:
				BaseMaxSp = Clamp(pValue, 0, MaxHitPoints);
				return true;
			case ParameterAttack:
				BaseAttack = Clamp(pValue, 1, MaxStat);
				return true;
			case ParameterDefense:
				BaseDefense = Clamp(pValue, 1, MaxStat);
				return true;
			case ParameterSpirit:
				BaseSpirit = Clamp(pValue, 1, MaxStat);
				return true;
			case ParameterAgility:
				BaseAgility = Clamp(pValue, 1, MaxStat);
				return true;
			default:
				return false;
		}
	}

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
