using System;

using UniversalRPG.Tests.Framework;
using UniversalRPG.Wolf;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// WOLF's four variable bands and the million boundary.
/// </summary>
/// <remarks>
/// <para>
/// The editor lists them as <c>Self / Var / Sys / 可変DB</c> — self,
/// normal and reserve, system, and the variable database. The VM had one
/// <c>Dictionary&lt;int, int&gt;</c>, so <strong>a self variable and a system
/// variable with the same index collided</strong> — and the collision is silent:
/// both reads answer with a number, and the wrong one.
/// </para>
/// <para>
/// The million boundary is the addressing scheme itself: a number at or above
/// 1,000,000 is a <em>reference</em>, not a value.
/// </para>
/// </remarks>
public partial class TestWolfVariableBands : TestBase
{
	/// <summary>Normal variable 0, as a reference.</summary>
	/// <remarks>
	/// **A method and not a constant**, because a constant has to be computed at
	/// compile time and the band offsets are runtime calls. A reader that inlined
	/// the numbers would hard-code the million blocks and lose the single place
	/// the scheme is defined.
	/// </remarks>
	private static int Normal0 => WolfVariable.BandOffset(WolfVariable.BandNormal);

	/// <summary>Self variable 3, as a reference.</summary>
	private static int Self3 => WolfVariable.BandOffset(WolfVariable.BandSelf) + 3;

	/// <summary>System variable 0, as a reference.</summary>
	private static int System0 => WolfVariable.BandOffset(WolfVariable.BandSystem);

	// ---- Die Bänder

	/// <summary>
	/// Four bands, and the same index in two of them is two values.
	/// </summary>
	/// <remarks>
	/// **This is the whole reason the bands are separate**, and a reader with
	/// one flat dictionary would have these two collide — the second write would
	/// overwrite the first and the test would still see *a* number.
	/// </remarks>
	public void Test_TheSameIndexInTwoBandsIsTwoValues()
	{
		var bands = new WolfVariableBands();
		bands.Set(WolfVariable.BandSelf, 0, 111);
		bands.Set(WolfVariable.BandSystem, 0, 222);

		AssertEq(
			bands.Get(WolfVariable.BandSelf, 0), 111,
			"**and self variable 0 is 111**, because the self band has its own"
			+ $" storage; it is {bands.Get(WolfVariable.BandSelf, 0)}");
		AssertEq(
			bands.Get(WolfVariable.BandSystem, 0), 222,
			"**and system variable 0 is still 222**, which a reader with one flat"
			+ " dictionary would have overwritten with 111;"
			+ $" it is {bands.Get(WolfVariable.BandSystem, 0)}");
	}

	/// <summary>
	/// The four bands are numbered from zero and there are four of them.
	/// </summary>
	public void Test_TheFourBandsAreNumberedFromZero()
	{
		AssertEq(WolfVariable.BandSelf, 0, "**and self is 0**, the first in the"
			+ $" help's list; it is {WolfVariable.BandSelf}");
		AssertEq(WolfVariable.BandNormal, 1, "and normal is 1;"
			+ $" it is {WolfVariable.BandNormal}");
		AssertEq(WolfVariable.BandSystem, 2, "and system is 2;"
			+ $" it is {WolfVariable.BandSystem}");
		AssertEq(WolfVariable.BandDatabase, 3, "and the database is 3;"
			+ $" it is {WolfVariable.BandDatabase}");
		AssertEq(WolfVariable.MaxBand, 3, "**and the highest is 3**, because the"
			+ $" help lists four; it is {WolfVariable.MaxBand}");
	}

	/// <summary>
	/// The database band is smaller than the others.
	/// </summary>
	/// <remarks>
	/// **A reader that used one bound for all four would let a game address
	/// database row 50,000** — a row the editor cannot hold and a save file
	/// cannot carry. The refusal is per band and the diagnostic says which.
	/// </remarks>
	public void Test_TheDatabaseBandIsSmaller()
	{
		var bands = new WolfVariableBands();

		AssertEq(
			bands.Set(WolfVariable.BandDatabase, 999, 5), true,
			"**and database row 999 is writable**, because that is the bound;"
			+ " the editor holds a thousand rows");
		AssertEq(
			bands.Set(WolfVariable.BandDatabase, 1000, 5), false,
			"**and row 1000 is refused**, because a reader with one bound for"
			+ " all four bands would let a game address a row that does not exist");
		AssertEq(
			bands.Set(WolfVariable.BandNormal, 99_999, 5), true,
			"**and normal variable 99,999 is writable**, because that band is"
			+ " larger and a reader that used the database bound for it would"
			+ " refuse a variable the game had used for years");
		AssertEq(
			bands.Set(WolfVariable.BandNormal, 100_000, 5), false,
			"**and normal variable 100,000 is refused**");
	}

	// ---- Die Millionenschranke

	/// <summary>
	/// A number at or above a million is a reference and not a value.
	/// </summary>
	/// <remarks>
	/// <strong>At or above, and not above.</strong> The help says a value of
	/// 1,000,000 or more is called rather than used, so a reader that tested
	/// <c>&gt;</c> would treat exactly 1,000,000 as a value and never resolve
	/// self variable 0 — the one variable every WOLF event uses.
	/// </remarks>
	public void Test_TheMillionIsAReferenceAndNotAValue()
	{
		AssertEq(
			WolfVariable.IsReference(999_999), false,
			"**and 999,999 is a value**, because it is below the boundary");
		AssertEq(
			WolfVariable.IsReference(1_000_000), true,
			"**and exactly 1,000,000 is a reference**, because the boundary is"
			+ " inclusive — a reader that tested above would treat this one as"
			+ " a value and never resolve self variable 0");
	}

	/// <summary>
	/// A reference names a band and an index.
	/// </summary>
	/// <remarks>
	/// <strong>The block is one based and the band is zero based</strong>, so
	/// 1,000,000 is block 1 and self band 0. A reader that used the block
	/// directly would be off by one for every band — and the first band would
	/// address one that does not exist.
	/// </remarks>
	public void Test_AReferenceNamesABandAndAnIndex()
	{
		AssertEq(
			WolfVariable.BandOf(1_000_000), WolfVariable.BandSelf,
			"**and 1,000,000 is the self band**, because the block is one based"
			+ " and the band is zero based;"
			+ $" it is {WolfVariable.BandOf(1_000_000)}");
		AssertEq(
			WolfVariable.IndexInBand(1_000_005), 5,
			$"**and its index is 5**, so 1,000,005 is self variable 5; it is"
			+ $" {WolfVariable.IndexInBand(1_000_005)}");
		AssertEq(
			WolfVariable.BandOf(Normal0), WolfVariable.BandNormal,
			"**and 2,000,000 is the normal band**, a whole million block per"
			+ $" band; it is {WolfVariable.BandOf(Normal0)}");
		AssertEq(
			WolfVariable.BandOf(999_999), -1,
			"**and a plain value names no band at all**, which is what -1 is for;"
			+ $" it is {WolfVariable.BandOf(999_999)}");
	}

	/// <summary>
	/// Resolving a reference reads the band, and resolving a value returns it.
	/// </summary>
	/// <remarks>
	/// **The number itself says which it is** — that is what the help's "do not
	/// call the data" checkbox means, and it is why a field may hold either
	/// without a separate flag.
	/// </remarks>
	public void Test_ResolvingAReferenceReadsTheBand()
	{
		var bands = new WolfVariableBands();
		bands.Set(WolfVariable.BandSelf, 3, 42);
		bands.Set(WolfVariable.BandSystem, 0, 77);

		AssertEq(
			bands.Resolve(Self3), 42,
			"**and self variable 3 reads back as 42**, because 1,000,003 is a"
			+ $" reference to it; it is {bands.Resolve(Self3)}");
		AssertEq(
			bands.Resolve(System0), 77,
			"and system variable 0 reads back as 77, through its own block;"
			+ $" it is {bands.Resolve(System0)}");
		AssertEq(
			bands.Resolve(500), 500,
			"**and a plain value resolves to itself**, because a number below the"
			+ $" million is the value and not a pointer; 500 resolves to {bands.Resolve(500)}");
	}

	/// <summary>
	/// Writing through a reference goes into that band.
	/// </summary>
	public void Test_WritingThroughAReferenceGoesIntoThatBand()
	{
		var bands = new WolfVariableBands();

		AssertEq(
			bands.SetByReference(Self3, 99), true,
			"**and writing through a self reference succeeds**");
		AssertEq(
			bands.Get(WolfVariable.BandSelf, 3), 99,
			$"**and it landed in the self band**; it is"
			+ $" {bands.Get(WolfVariable.BandSelf, 3)}");
		AssertEq(
			bands.Get(WolfVariable.BandNormal, 3), 0,
			"**and the normal band at the same index is untouched**, because"
			+ " writing through a reference names the band and not just the index;"
			+ $" it is {bands.Get(WolfVariable.BandNormal, 3)}");
		AssertEq(
			bands.SetByReference(500, 99), false,
			"**and writing through a plain value is refused**, because a value is"
			+ " not a place to write to — a reader that allowed it would store a"
			+ " number under a key that is not a variable at all");
	}

	/// <summary>
	/// An unknown band is refused and names nothing.
	/// </summary>
	public void Test_AnUnknownBandIsRefused()
	{
		var bands = new WolfVariableBands();

		AssertEq(
			bands.Set(4, 0, 5), false,
			"**and band 4 is refused**, because the help lists four and this"
			+ " reader does not guess a fifth");
		AssertEq(
			bands.Set(-1, 0, 5), false,
			"**and band -1 is refused**");
		AssertEq(
			WolfVariable.BandOf(5_000_000), -1,
			"**and 5,000,000 names no band**, because it is a fifth block and"
			+ " the editor has four; it is"
			+ $" {WolfVariable.BandOf(5_000_000)}");
	}

	// ---- Die VM

	/// <summary>
	/// The VM's own variables go through the bands.
	/// </summary>
	/// <remarks>
	/// <strong>Its accessors address the normal band</strong>, because a caller
	/// that reaches for "a variable" without saying which one is asking the
	/// question WOLF answers with four — and the convenience accessors have to
	/// answer *something*.
	/// </remarks>
	public void Test_TheVmAccessorsAddressTheNormalBand()
	{
		var vm = new WolfEventVm();
		vm.SetVariable(0, 123);

		AssertEq(
			vm.GetVariable(0), 123,
			"**and what it wrote it reads back**, through the normal band;"
			+ $" it is {vm.GetVariable(0)}");
		AssertEq(
			vm.VariableBands.Get(WolfVariable.BandSelf, 0), 0,
			"**and the self band is still empty**, because the accessor named"
			+ " the normal one and not the first band by accident;"
			+ $" it is {vm.VariableBands.Get(WolfVariable.BandSelf, 0)}");
	}

	/// <summary>
	/// A new game empties every band.
	/// </summary>
	/// <remarks>
	/// **A reset that cleared one band would leave the others**, and a game that
	/// started with the last game's system variables would start with a clock
	/// already running.
	/// </remarks>
	public void Test_ANewGameEmptiesEveryBand()
	{
		var vm = new WolfEventVm();
		vm.VariableBands.Set(WolfVariable.BandSelf, 0, 1);
		vm.VariableBands.Set(WolfVariable.BandNormal, 0, 2);
		vm.VariableBands.Set(WolfVariable.BandSystem, 0, 3);
		vm.VariableBands.Set(WolfVariable.BandDatabase, 0, 4);

		vm.ResetState();

		for (var band = 0; band <= WolfVariable.MaxBand; band++)
		{
			AssertEq(
				vm.VariableBands.Get(band, 0), 0,
				$"**and band {band} is empty**, because a reset that cleared only"
				+ " one would leave the others and the new game would start with"
				+ $" the last game's values; it is {vm.VariableBands.Get(band, 0)}");
		}
	}
}
