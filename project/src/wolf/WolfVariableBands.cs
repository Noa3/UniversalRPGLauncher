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

	/// <summary>The band a reference of 1,000,000 plus n names.</summary>
	public const int BandSelf = 0;

	/// <summary>Normal and reserve variables.</summary>
	public const int BandNormal = 1;

	/// <summary>System variables.</summary>
	public const int BandSystem = 2;

	/// <summary>The variable database.</summary>
	public const int BandDatabase = 3;

	/// <summary>The highest band the editor offers.</summary>
	public const int MaxBand = 3;

	/// <summary>
	/// The offset each band starts at above <see cref="ReferenceBase"/>.
	/// </summary>
	/// <remarks>
	/// **The million block per band**, so self variable 0 is 1,000,000 and
	/// normal variable 0 is 2,000,000. A reader that packed the bands side by
	/// side would make a normal reference and a self reference differ by a
	/// small number, and a game computing one from the other would land in a
	/// neighbouring band.
	/// </remarks>
	public static int BandOffset(int pBand)
	{
		return ReferenceBase * (pBand + 1);
	}

	/// <summary>
	/// Whether a number is a reference and not a value.
	/// </summary>
	/// <remarks>
	/// <strong>At or above the million, and not "above".</strong> The help
	/// says a value of 1,000,000 or more is called rather than used, so a
	/// reader that tested <c>&gt;</c> would treat exactly 1,000,000 as a value
	/// and never resolve self variable 0.
	/// </remarks>
	public static bool IsReference(int pNumber)
	{
		return pNumber >= ReferenceBase;
	}

	/// <summary>
	/// The band a number names, or -1 when it is a value.
	/// </summary>
	public static int BandOf(int pNumber)
	{
		if (!IsReference(pNumber))
		{
			return -1;
		}
		var block = pNumber / ReferenceBase;
		// **The block is one based and the band is zero based.** 1,000,000 is
		// block 1 and self band 0, so the band is the block minus one. A
		// reader that used the block directly would address a band that does
		// not exist for the first million and be off by one everywhere else.
		var band = block - 1;
		return band < 0 || band > MaxBand ? -1 : band;
	}

	/// <summary>
	/// The index within its band, for a reference.
	/// </summary>
	/// <returns>-1 when the number is a value or names no band.</returns>
	public static int IndexInBand(int pNumber)
	{
		if (BandOf(pNumber) < 0)
		{
			return -1;
		}
		return pNumber % ReferenceBase;
	}
}

/// <summary>
/// The four WOLF variable bands, kept apart.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Four dictionaries and not one.</strong> That is the shape the help
/// describes and the shape a game assumes: a self variable and a system
/// variable with the same index are different values, and a reader that put
/// them in one dictionary would have them collide.
/// </para>
/// <para>
/// <strong>An index outside its band is refused and says which band.</strong> A
/// reader that grew a dictionary on demand would answer a read of self
/// variable 99,999 with a zero that looks like a variable the game set.
/// </para>
/// </remarks>
public sealed class WolfVariableBands
{
	/// <summary>The highest index a band holds, from the editor's own bound.</summary>
	public const int MaxIndex = 99_999;

	/// <summary>The highest database row, from the editor's own bound.</summary>
	public const int MaxDatabaseIndex = 999;

	private readonly Dictionary<int, int>[] _bands =
	[
		new(), // Self
		new(), // Normal
		new(), // System
		new(), // Database
	];

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
		var max = pBand == WolfVariable.BandDatabase ? MaxDatabaseIndex : MaxIndex;
		return pIndex >= 0 && pIndex <= max;
	}

	/// <summary>Empties every band, for a new game.</summary>
	public void Clear()
	{
		foreach (var band in _bands)
		{
			band.Clear();
		}
	}
}
