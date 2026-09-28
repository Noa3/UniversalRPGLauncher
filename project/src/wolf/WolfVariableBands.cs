using System;
using System.Collections.Generic;

namespace UniversalRPG.Wolf;

/// <summary>
/// WOLF's four variable bands, from the editor's help page
/// <c>silversecond.com/WolfRPGEditor/Help/04ev_value.html</c>.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Four bands and not one flat array.</strong> The help lists them as
/// <c>Self / Var / Sys / 可変DB</c> — self variables, normal and reserve
/// variables, system variables, and the variable database. A reader with a
/// single <c>Dictionary&lt;int, int&gt;</c> gives every band the same storage, and
/// <em>a game that puts its self variable 0 and its system variable 0 to
/// different values would have them collide</em> — which is a silent wrong
/// answer rather than an error.
/// </para>
/// <para>
/// <strong>The million boundary is the whole addressing scheme.</strong> A
/// number at or above 1,000,000 is not a value, it is a <em>reference</em>:
/// the help says that putting 2,000,005 in a field and resolving it lands on
/// normal variable 5. A reader that treated the number as a value would store
/// two million in a field meant to point at variable five, and the game would
/// read a number it never wrote.
/// </para>
/// </remarks>
public static class WolfVariable
{
	/// <summary>The number at which a field stops being a value and becomes a reference.</summary>
	public const int ReferenceBase = 1_000_000;

	/// <summary>A map event's self variables.</summary>
	/// <remarks>
	/// <strong>1,100,000 and not 1,000,000.</strong> The help's own examples
	/// name the offsets: <c>1100000～:マップセルフ変数</c> and
	/// <c>1600000～:コモンセルフ変数</c> for the common event page call, and
	/// <c>2000000</c> for normal variable 0 and <c>3000000</c> for string
	/// variable 0. A reader that put self at one million — as this one did —
	/// addresses a range the format does not have, and every self variable in
	/// a real game lands in the middle of nothing.
	/// </remarks>
	public const int BaseMapSelf = 1_100_000;

	/// <summary>A common event's self variables.</summary>
	public const int BaseCommonSelf = 1_600_000;

	/// <summary>Normal and reserve variables.</summary>
	public const int BaseNormal = 2_000_000;

	/// <summary>String variables.</summary>
	/// <remarks>
	/// <strong>A number band and not a variable band.</strong> The help's
	/// variable notation lists <c>S?</c> for string variable ? and gives
	/// <c>3000000</c> as the number that names string variable 0. A number
	/// band is not something this reader can execute — it has no string
	/// variables yet — so it is recognised and refused rather than silently
	/// read as a normal variable, which would answer a game's question with a
	/// wrong number instead of admitting it does not know.
	/// </remarks>
	public const int BaseString = 3_000_000;

	/// <summary>System variables.</summary>
	public const int BaseSystem = 4_000_000;

	/// <summary>The variable database.</summary>
	/// <remarks>
	/// <strong>The database is not a fixed offset, and the help says so.</strong>
	/// The branch help states that when the variable database is the comparison
	/// source, a variable call such as <c>1600000</c> may not be given. The
	/// database is therefore addressed by type and column rather than by a
	/// block, and <see cref="BandDatabase"/> is the flag the command carries
	/// rather than an offset this reader computes.
	/// </remarks>
	public const int BandDatabase = -1;

	/// <summary>A number below the million: a value, and not a reference.</summary>
	public const int NoReference = -1;

	/// <summary>
	/// A number at or above the million that names no band, because the bands
	/// are fixed offsets and not every million is one of them.
	/// </summary>
	/// <remarks>
	/// <strong>A different answer from <see cref="NoReference"/>, on purpose.</strong>
	/// The help says a number of a million or more is called, so 1,000,000 is a
	/// reference — it is simply one that points at nothing this reader knows.
	/// Reporting that as "not a reference" would tell a caller the number is a
	/// value, and a caller that stores it would keep a pointer in a variable
	/// the game reads as a number.
	/// </remarks>
	public const int NoBandForReference = -2;

	/// <summary>The first offset this reader knows, used to name a band.</summary>
	public const int BandMapSelf = 0;

	/// <summary>The common self band.</summary>
	public const int BandCommonSelf = 1;

	/// <summary>The normal band.</summary>
	public const int BandNormal = 2;

	/// <summary>The system band.</summary>
	public const int BandSystem = 3;

	/// <summary>The highest number band this reader knows.</summary>
	public const int MaxBand = 3;

	/// <summary>
	/// The offset one band starts at, or -1 when the name is not a band.
	/// </summary>
	/// <remarks>
	/// <strong>Fixed offsets and not a computed block.</strong> The bands are
	/// not a single arithmetic run: 1.1, 1.6, 2.0 and 4.0 million. A reader that
	/// computed <c>base * (band + 1)</c> would put the system band at three
	/// million, which is the string band, and a game reading a system clock
	/// would read a string variable's offset instead.
	/// </remarks>
	public static int BaseOf(int pBand)
	{
		return pBand switch
		{
			BandMapSelf => BaseMapSelf,
			BandCommonSelf => BaseCommonSelf,
			BandNormal => BaseNormal,
			BandSystem => BaseSystem,
			_ => -1,
		};
	}

	/// <summary>
	/// The offset each band starts at, from the help's own numbers.
	/// </summary>
	public static int BandOffset(int pBand)
	{
		return BaseOf(pBand);
	}

	/// <summary>
	/// Whether a number is a reference and not a value.
	/// </summary>
	/// <remarks>
	/// <strong>At or above the million, and not "above".</strong> The help
	/// says a value of 1,000,000 or more is called rather than used, so a
	/// reader that tested <c>&gt;</c> would treat exactly 1,000,000 as a value
	/// and never resolve anything at all.
	/// </remarks>
	public static bool IsReference(int pNumber)
	{
		return pNumber >= ReferenceBase;
	}

	/// <summary>
	/// The band a number names, or -1 when it is a value.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>By the table and not by division.</strong> The bands are fixed
	/// offsets, so the band is the one whose offset the number reaches — and a
	/// reader that divided by a million and subtracted one would answer 0 for
	/// 1,000,000, 1 for 1,100,000, 1 again for 1,600,000, and 3 for 4,000,000,
	/// which is a table of answers none of them mean.
	/// </para>
	/// <para>
	/// <strong>The string band is recognised and refused.</strong> It is a
	/// number this reader cannot answer with a number, and a reader that fell
	/// through to the normal band would return a normal variable's value for a
	/// reference that means a string.
	/// </para>
	/// </remarks>
	public static int BandOf(int pNumber)
	{
		if (!IsReference(pNumber))
		{
			return -1;
		}
		// **The bands are tested widest first, so a number in a gap falls
		// through to no band at all.** Each base is a million block, so the
		// block between 1,600,000 and 2,000,000 is empty and a number in it
		// names nothing. Rounding it into a neighbouring band would be a
		// silent wrong answer, and a gap is a gap.
		if (pNumber >= BaseSystem)
		{
			return BandSystem;
		}
		if (pNumber >= BaseNormal)
		{
			return BandNormal;
		}
		if (pNumber >= BaseCommonSelf)
		{
			return BandCommonSelf;
		}
		if (pNumber >= BaseMapSelf)
		{
			return BandMapSelf;
		}
		// **The whole gap below the map self base answers -2, not just the
		// million itself.** Every number from 1,000,000 to 1,099,999 is at or
		// above the boundary, so every one of them is a reference, and not one
		// of them names a band. A reader that gave -2 to exactly 1,000,000 and
		// -1 to the rest would tell a caller that 1,050,000 is a *value* — and
		// a caller that stored it would keep a pointer in a variable the game
		// reads as a number, which is the exact failure the two codes exist to
		// keep apart.
		return pNumber >= ReferenceBase ? NoBandForReference : NoReference;
	}

	/// <summary>
	/// The index within its band, for a reference.
	/// </summary>
	/// <returns>-1 when the number is a value or names no band.</returns>
	public static int IndexInBand(int pNumber)
	{
		var band = BandOf(pNumber);
		if (band < 0)
		{
			return -1;
		}
		var baseOf = BaseOf(band);
		// **A negative index would address the dictionary and be wrong**, and
		// the guard is here rather than at the call sites because the band
		// check above is what produces the -2, and a caller that only looked
		// for "less than zero" would have caught it — this one checks the code.
		var index = pNumber - baseOf;
		// **The bound belongs to the band container, and it is named here
		// rather than duplicated**, so the reader that resolves a reference and
		// the reader that writes one cannot disagree about where a band ends.
		return index < 0 || index > WolfVariableBands.MaxIndex ? -1 : index;
	}
}

public sealed class WolfVariableBands
{
	/// <summary>The highest index a band holds, from the editor's own bound.</summary>
	public const int MaxIndex = 99_999;

	/// <summary>The highest database row, from the editor's own bound.</summary>
	public const int MaxDatabaseIndex = 999;

	/// <summary>
	/// One dictionary per number band, in the order
	/// <see cref="WolfVariable.BandMapSelf"/> through
	/// <see cref="WolfVariable.BandSystem"/>.
	/// </summary>
	/// <remarks>
	/// <strong>Four bands and not one, and the order matches the help's
	/// dropdown</strong> — self, normal and reserve, system, variable database.
	/// A reader that kept every band in one dictionary would have a self
	/// variable and a system variable with the same index collide, and the
	/// collision is silent: both reads answer with a number and only the wrong
	/// one.
	/// </remarks>
	private readonly Dictionary<int, int>[] _bands =
	[
		new(), // Map self
		new(), // Common self
		new(), // Normal
		new(), // System
	];

	/// <summary>
	/// The variable database, addressed by type and column and not by a band.
	/// </summary>
	/// <remarks>
	/// <strong>Separate and not a fifth dictionary in the array</strong>, because
	/// the help says the database cannot be named with a variable call: when it
	/// is the comparison source, a value such as 1,600,000 may not be given. A
	/// reader that gave it a band would let a game address database row 50,000
	/// through a block the format does not define.
	/// </remarks>
	private readonly Dictionary<long, int> _database = new();

	/// <summary>Reads one band and index, or zero when it holds nothing.</summary>
	public int Get(int pBand, int pIndex)
	{
		if (!IsInRange(pBand, pIndex))
		{
			return 0;
		}
		return _bands[pBand].TryGetValue(pIndex, out var value) ? value : 0;
	}

	/// <summary>Writes one band and index.</summary>
	/// <returns>False when the band or index is outside the format.</returns>
	public bool Set(int pBand, int pIndex, int pValue)
	{
		if (!IsInRange(pBand, pIndex))
		{
			return false;
		}
		_bands[pBand][pIndex] = pValue;
		return true;
	}

	/// <summary>
	/// Resolves a number that is either a value or a reference.
	/// </summary>
	/// <remarks>
	/// <strong>A number below the million is its own value</strong>, which is
	/// what the help's "do not call the data" checkbox means: a field may hold
	/// either, and the number itself says which.
	/// </remarks>
	public int Resolve(int pNumber)
	{
		if (!WolfVariable.IsReference(pNumber))
		{
			return pNumber;
		}
		var band = WolfVariable.BandOf(pNumber);
		if (band < 0)
		{
			// **Zero, and not an exception and not the number itself.** The
			// number is a reference and this reader cannot answer it — it names
			// the string band, or it is in a gap. Returning the number would put
			// a pointer where a value belongs, and every comparison that used it
			// would be comparing pointers.
			return 0;
		}
		return Get(band, WolfVariable.IndexInBand(pNumber));
	}

	/// <summary>
	/// Writes through a reference, or refuses one.
	/// </summary>
	/// <returns>False when the number is a value, or names no band.</returns>
	public bool SetByReference(int pNumber, int pValue)
	{
		var band = WolfVariable.BandOf(pNumber);
		if (band < 0)
		{
			return false;
		}
		return Set(band, WolfVariable.IndexInBand(pNumber), pValue);
	}

	/// <summary>Whether a band and index are inside the format.</summary>
	public static bool IsInRange(int pBand, int pIndex)
	{
		if (pBand < 0 || pBand > WolfVariable.MaxBand)
		{
			return false;
		}
		// **The database band is smaller than the others**, and a reader that
		// used one bound for all four would let a game address database row
		// 50,000 — a row the editor cannot hold and a save file cannot carry.
		// **The database is not a band and is refused here** with the other
		// out of range names, because it has its own accessor above. A reader
		// that let BandDatabase index the array would read a dictionary that
		// does not exist.
		return pIndex >= 0 && pIndex <= MaxIndex;
	}

	/// <summary>Reads one database cell, by type and column.</summary>
	public int GetDatabase(int pType, int pColumn)
	{
		return pType < 0 || pColumn < 0 || pColumn > MaxDatabaseIndex
			? 0
			: _database.TryGetValue(DatabaseKey(pType, pColumn), out var value) ? value : 0;
	}

	/// <summary>Writes one database cell, by type and column.</summary>
	/// <returns>False when the type is negative or the column is out of range.</returns>
	public bool SetDatabase(int pType, int pColumn, int pValue)
	{
		if (pType < 0 || pColumn < 0 || pColumn > MaxDatabaseIndex)
		{
			return false;
		}
		_database[DatabaseKey(pType, pColumn)] = pValue;
		return true;
	}

	/// <summary>
	/// The key one database cell is stored under.
	/// </summary>
	/// <remarks>
	/// <strong>Type and column in one number, and the type is shifted rather
	/// than multiplied</strong>, so a caller cannot make a large type wrap into
	/// another type's cells. The shift is 16 because a column of a thousand
	/// needs ten bits and a shift that small would leave the two fields
	/// overlapping.
	/// </remarks>
	private static long DatabaseKey(int pType, int pColumn)
	{
		return ((long)pType << 16) | (uint)pColumn;
	}

	/// <summary>Empties every band and the database, for a new game.</summary>
	public void Clear()
	{
		foreach (var band in _bands)
		{
			band.Clear();
		}
		_database.Clear();
	}
}
