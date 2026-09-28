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
	/// <strong>2,000,000, which is the help's own number.</strong> The help's
	/// examples say that 2,000,000 means normal variable 0, and it says so in
	/// the branch help while describing the same reference. A reader that
	/// computed the offset as a million times a band number would put normal
	/// variable 0 at 1,100,000, which is a map self variable.
	/// </remarks>
	private static int Normal0 => WolfVariable.BaseNormal;

	/// <summary>Map self variable 3, as a reference.</summary>
	/// <remarks>
	/// <strong>1,100,003, and not 1,000,003.</strong> The help's page-call note
	/// names 1,100,000 for map self variables. An earlier version of this
	/// reader put self at one million, which addresses a range the format does
	/// not have, and the tests here asserted that wrong number until the help
	/// was read properly.
	/// </remarks>
	private static int MapSelf3 => WolfVariable.BaseMapSelf + 3;

	/// <summary>System variable 0, as a reference.</summary>
	/// <remarks>
	/// <strong>4,000,000, and not 3,000,000.</strong> Three million is the
	/// string band, so a reader that computed the system band as the fourth
	/// million would read a game's system clock out of the string range.
	/// </remarks>
	private static int System0 => WolfVariable.BaseSystem;

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
		bands.Set(WolfVariable.BandMapSelf, 0, 111);
		bands.Set(WolfVariable.BandSystem, 0, 222);

		AssertEq(
			bands.Get(WolfVariable.BandMapSelf, 0), 111,
			"**and self variable 0 is 111**, because the self band has its own"
			+ $" storage; it is {bands.Get(WolfVariable.BandMapSelf, 0)}");
		AssertEq(
			bands.Get(WolfVariable.BandSystem, 0), 222,
			"**and system variable 0 is still 222**, which a reader with one flat"
			+ " dictionary would have overwritten with 111;"
			+ $" it is {bands.Get(WolfVariable.BandSystem, 0)}");
	}

	/// <summary>
	/// The band offsets are the help's own numbers, and they are not a run.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>1,100,000, 1,600,000, 2,000,000 and 4,000,000 — four fixed
	/// numbers.</strong> The help's page-call note says
	/// <c>1100000～:マップセルフ変数</c> and <c>1600000～:コモンセルフ変数</c>,
	/// and the variable and value help says <c>2000000</c> is normal variable 0
	/// and <c>3000000</c> is string variable 0.
	/// </para>
	/// <para>
	/// <strong>An earlier version of this reader computed them as a million
	/// times a band number,</strong> which put map self at 1,000,000, common
	/// self at 2,000,000 — a normal variable — and the system band at
	/// 3,000,000, which is the string band. The tests here asserted that wrong
	/// scheme and passed, and they are the reason the error survived a
	/// mutation run and a commit.
	/// </para>
	/// </remarks>
	public void Test_TheBandOffsetsAreTheHelpsOwnNumbers()
	{
		AssertEq(WolfVariable.BaseMapSelf, 1_100_000,
			"**and map self is 1,100,000**, because the help's page-call note"
			+ " names it and a reader that put it at one million addresses a"
			+ $" range the format does not have; it is {WolfVariable.BaseMapSelf}");
		AssertEq(WolfVariable.BaseCommonSelf, 1_600_000,
			"**and common self is 1,600,000** — which a computed block would have"
			+ $" put at 2,000,000, a normal variable; it is {WolfVariable.BaseCommonSelf}");
		AssertEq(WolfVariable.BaseNormal, 2_000_000,
			"**and normal is 2,000,000**, the number the help's own example uses"
			+ $" for normal variable 0; it is {WolfVariable.BaseNormal}");
		AssertEq(WolfVariable.BaseString, 3_000_000,
			"**and the string band is 3,000,000**, which is why a computed block"
			+ $" must not be used for the system band — it would land here;"
			+ $" it is {WolfVariable.BaseString}");
		AssertEq(WolfVariable.BaseSystem, 4_000_000,
			"**and the system band is 4,000,000**, not 3,000,000;"
			+ $" it is {WolfVariable.BaseSystem}");
	}

	/// <summary>
	/// A number at the million is a reference that names no band.
	/// </summary>
	/// <remarks>
	/// <strong>Two different answers, and the reason matters.</strong> The help
	/// says a million or more is called, so 1,000,000 is a reference — it is
	/// simply in the gap between the million and the map self base. Reporting
	/// that as "not a reference" would tell a caller the number is a value, and
	/// a caller that stored it would keep a pointer in a variable the game
	/// reads as a number.
	/// </remarks>
	public void Test_TheMillionItselfNamesNoBand()
	{
		AssertEq(
			WolfVariable.IsReference(1_000_000), true,
			"**and 1,000,000 is still a reference**, because the help says a"
			+ " million or more is called");
		AssertEq(
			WolfVariable.BandOf(1_000_000), WolfVariable.NoBandForReference,
			"**and it names no band**, which is -2 and not the -1 that means"
			+ " not a reference, because the two are different answers;"
			+ $" it is {WolfVariable.BandOf(1_000_000)}");
		AssertEq(
			WolfVariable.BandOf(1_050_000), WolfVariable.NoBandForReference,
			"**and the middle of the gap says the same, at 1,050,000** — a"
			+ " number there is at or above the million and below the map self"
			+ " base, so it is a reference that names nothing. Rounding it into"
			+ " a neighbouring band would be a silent wrong answer."
			+ $" It is {WolfVariable.BandOf(1_050_000)}");
		AssertEq(
			WolfVariable.BandOf(1_000_001), WolfVariable.NoBandForReference,
			"**and 1,000,001, one over the boundary, says the same too**;"
			+ $" it is {WolfVariable.BandOf(1_000_001)}");
	}

	/// <summary>
	/// The string band is recognised and refused.
	/// </summary>
	/// <remarks>
	/// <strong>A reader that fell through to the normal band would answer a
	/// string reference with a number</strong> — and the number would be a
	/// plausible one, so a game comparing it would not look broken. This reader
	/// has no string variables, and it says that rather than guessing.
	/// </remarks>
	public void Test_TheStringBandIsRefusedAndNotAnswered()
	{
		var bands = new WolfVariableBands();
		bands.Set(WolfVariable.BandNormal, 0, 500);

		AssertEq(
			bands.Resolve(WolfVariable.BaseString), 0,
			"**and a string reference resolves to 0**, not to normal variable 0"
			+ $"'s 500 — a reader that fell through would answer 500;"
			+ $" it is {bands.Resolve(WolfVariable.BaseString)}");
	}

	/// <summary>
	/// The database band is smaller than the others.
	/// </summary>
	/// <remarks>
	/// **A reader that used one bound for all four would let a game address
	/// database row 50,000** — a row the editor cannot hold and a save file
	/// cannot carry. The refusal is per band and the diagnostic says which.
	/// </remarks>
	public void Test_TheDatabaseIsAddressedByTypeAndColumn()
	{
		var bands = new WolfVariableBands();

		AssertEq(
			bands.SetDatabase(4, 999, 5), true,
			"**and database type 4, column 999 is writable**, because a thousand"
			+ " columns is the editor's own bound for a row");
		AssertEq(
			bands.SetDatabase(4, 1000, 5), false,
			"**and column 1000 is refused**, because a reader with one bound for"
			+ " every table would let a game address a column no row has");
		AssertEq(
			bands.GetDatabase(4, 999), 5,
			"**and it reads back**, which is the whole point of a table that is"
			+ $" separate; it is {bands.GetDatabase(4, 999)}");
		AssertEq(
			bands.GetDatabase(4, 998), 0,
			"**and column 998 of the same type is empty**, so a caller that"
			+ " dropped the type would read the wrong cell's zero as its own;"
			+ $" it is {bands.GetDatabase(4, 998)}");
		AssertEq(
			bands.GetDatabase(5, 999), 0,
			"**and type 5, column 999 is a different cell**, because the type is"
			+ " part of the address and not dropped;"
			+ $" it is {bands.GetDatabase(5, 999)}");
		AssertEq(
			bands.SetDatabase(-1, 0, 5), false,
			"**and a negative type is refused**");
		AssertEq(
			bands.Set(WolfVariable.BandDatabase, 0, 5), false,
			"**and the database is not a number band at all** — the help says a"
			+ " variable call such as 1,600,000 may not be given when the"
			+ " database is the source, so it has no offset to be indexed by;"
			+ " BandDatabase is -1 and the band set refuses it");
		AssertEq(
			bands.Set(WolfVariable.BandNormal, 99_999, 5), true,
			"**and normal variable 99,999 is writable**, because that band is a"
			+ " hundred thousand wide and a reader that used the database bound"
			+ " for it would refuse a variable the game had used for years");
		AssertEq(
			bands.Set(WolfVariable.BandNormal, 100_000, 5), false,
			"**and normal variable 100,000 is refused**");
	}

	/// <summary>
	/// A new game empties the database too.
	/// </summary>
	public void Test_ANewGameEmptiesTheDatabase()
	{
		var bands = new WolfVariableBands();
		bands.SetDatabase(2, 3, 77);

		bands.Clear();

		AssertEq(
			bands.GetDatabase(2, 3), 0,
			"**and the cell is empty**, because a reset that cleared the bands"
			+ " and not the database would leave the last game's progress flags"
			+ $" in place; it is {bands.GetDatabase(2, 3)}");
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
			WolfVariable.BandOf(WolfVariable.BaseMapSelf), WolfVariable.BandMapSelf,
			"**and 1,100,000 is the map self band**, because that is the offset"
			+ $" the help's page-call note names; it is {WolfVariable.BandOf(WolfVariable.BaseMapSelf)}");
		AssertEq(
			WolfVariable.IndexInBand(WolfVariable.BaseMapSelf + 5), 5,
			"**and its index is 5**, so 1,100,005 is map self variable 5; it is"
			+ $" {WolfVariable.IndexInBand(WolfVariable.BaseMapSelf + 5)}");
		AssertEq(
			WolfVariable.BandOf(Normal0), WolfVariable.BandNormal,
			"**and 2,000,000 is the normal band**, the number the help's own"
			+ $" example uses; it is {WolfVariable.BandOf(Normal0)}");
		AssertEq(
			WolfVariable.BandOf(999_999), WolfVariable.NoReference,
			"**and a plain value is not a reference at all**, which is -1 and"
			+ " not the -2 that means a reference naming no band;"
			+ $" it is {WolfVariable.BandOf(999_999)}");
		AssertEq(
			WolfVariable.BandOf(WolfVariable.BaseCommonSelf), WolfVariable.BandCommonSelf,
			"**and 1,600,000 is the common self band**, which a computed block"
			+ " would have put on the normal band;"
			+ $" it is {WolfVariable.BandOf(WolfVariable.BaseCommonSelf)}");
		AssertEq(
			WolfVariable.BandOf(WolfVariable.BaseSystem), WolfVariable.BandSystem,
			"**and 4,000,000 is the system band** — the fourth number, and not"
			+ " the third, because the third is the string band;"
			+ $" it is {WolfVariable.BandOf(WolfVariable.BaseSystem)}");
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
		bands.Set(WolfVariable.BandMapSelf, 3, 42);
		bands.Set(WolfVariable.BandSystem, 0, 77);

		AssertEq(
			bands.Resolve(MapSelf3), 42,
			"**and self variable 3 reads back as 42**, because 1,000,003 is a"
			+ $" reference to it; it is {bands.Resolve(MapSelf3)}");
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
			bands.SetByReference(MapSelf3, 99), true,
			"**and writing through a self reference succeeds**");
		AssertEq(
			bands.Get(WolfVariable.BandMapSelf, 3), 99,
			$"**and it landed in the self band**; it is"
			+ $" {bands.Get(WolfVariable.BandMapSelf, 3)}");
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
			WolfVariable.BandOf(5_000_000), WolfVariable.BandSystem,
			"**and 5,000,000 is in the system band**, because that band starts"
			+ " at 4,000,000 and a million-block band runs a million wide. An"
			+ " earlier version of this test claimed it named no band, and that"
			+ $" claim was wrong; it is {WolfVariable.BandOf(5_000_000)}");
		AssertEq(
			WolfVariable.IndexInBand(5_000_000), -1,
			"**and its index is refused, because a million is past the end of"
			+ " the band** — so 5,000,000 is system variable 1,000,000, which"
			+ " no editor holds. The band and the index are two separate bounds"
			+ " and this is the second one refusing."
			+ $" It is {WolfVariable.IndexInBand(5_000_000)}");
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
			vm.VariableBands.Get(WolfVariable.BandMapSelf, 0), 0,
			"**and the self band is still empty**, because the accessor named"
			+ " the normal one and not the first band by accident;"
			+ $" it is {vm.VariableBands.Get(WolfVariable.BandMapSelf, 0)}");
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
		vm.VariableBands.Set(WolfVariable.BandMapSelf, 0, 1);
		vm.VariableBands.Set(WolfVariable.BandCommonSelf, 0, 2);
		vm.VariableBands.Set(WolfVariable.BandNormal, 0, 3);
		vm.VariableBands.Set(WolfVariable.BandSystem, 0, 4);
		vm.VariableBands.SetDatabase(1, 0, 5);

		vm.ResetState();

		// **Every band and the database, named rather than counted.** A loop
		// over 0 to MaxBand would have missed a fifth store the day one is
		// added, which is exactly how the common self band survived a reset
		// that nobody noticed.
		foreach (var band in new[]
		{
			WolfVariable.BandMapSelf,
			WolfVariable.BandCommonSelf,
			WolfVariable.BandNormal,
			WolfVariable.BandSystem,
		})
		{
			AssertEq(
				vm.VariableBands.Get(band, 0), 0,
				$"**and band {band} is empty**, because a reset that cleared"
				+ " only one would leave the others and the new game would"
				+ $" start with the last game's values;"
				+ $" it is {vm.VariableBands.Get(band, 0)}");
		}
		AssertEq(
			vm.VariableBands.GetDatabase(1, 0), 0,
			"**and the database is empty as well**, because it is a store of"
			+ " its own and a reset that only walked the bands would leave the"
			+ $" last game's table behind; it is {vm.VariableBands.GetDatabase(1, 0)}");
	}
}
