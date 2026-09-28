using System;

using UniversalRPG.Tests.Framework;
using UniversalRPG.Wolf;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// WOLF's character sheets: the direction order, the walk cycle, and the idle
/// frames.
/// </summary>
/// <remarks>
/// <para>
/// The material specification says the character's layout changes with the
/// game's basic settings — three or five animation patterns, four or eight
/// directions — and gives the order both.
/// </para>
/// <para>
/// <strong>The four directions are down, left, right, up, top to bottom,</strong>
/// and that is not the compass order. A reader that used up, right, down, left
/// would show every character turned ninety degrees, which a player sees in the
/// first second and never reports.
/// </para>
/// </remarks>
public partial class TestWolfCharacterSheet : TestBase
{
	/// <summary>A board with one figure on it, for the cell tests.</summary>
	private static (WolfCharacterBoard Board, WolfCharacter Guard) BoardWithGuard(
		int pX = 5,
		int pY = 5)
	{
		var board = new WolfCharacterBoard(new WolfVariableBands());
		board.LoadMap(1, new WolfPassabilityGrid(20, 20));
		var guard = board.Add(new WolfCharacter { Id = 1, X = pX, Y = pY });
		return (board, guard);
	}

	// ---- Die Reihenfolge der Richtungen

	/// <summary>
	/// The four directions are down, left, right, up, top to bottom.
	/// </summary>
	/// <remarks>
	/// <strong>Not the compass order.</strong> The material guide gives this
	/// order twice — once for the eight direction sheet's left half and once for
	/// the four direction sheet — and the compass order would turn every
	/// character ninety degrees.
	/// </remarks>
	public void Test_TheFourDirectionsAreDownLeftRightUp()
	{
		AssertEq(
			WolfCharacterSheet.Locate(WolfCharacter.PassDown, false, out var downRow, out _),
			true,
			"**and down is found**, because every sheet has it");
		AssertEq(
			downRow, 0,
			"**and down is row 0**, which is the top of the sheet — the guide's"
			+ $" order starts there; it is {downRow}");
		AssertEq(
			WolfCharacterSheet.Locate(WolfCharacter.PassLeft, false, out var leftRow, out _),
			true,
			"and left is found");
		AssertEq(
			leftRow, 1,
			"**and left is row 1**, not row 3 as the compass order would say;"
			+ $" it is {leftRow}");
		AssertEq(
			WolfCharacterSheet.Locate(WolfCharacter.PassRight, false, out var rightRow, out _),
			true,
			"and right is found");
		AssertEq(
			rightRow, 2,
			"**and right is row 2**;"
			+ $" it is {rightRow}");
		AssertEq(
			WolfCharacterSheet.Locate(WolfCharacter.PassUp, false, out var upRow, out _),
			true,
			"and up is found");
		AssertEq(
			upRow, 3,
			"**and up is row 3**, the bottom of the sheet, which is where up"
			+ $" belongs in this format; it is {upRow}");
	}

	/// <summary>
	/// The eight direction sheet puts the diagonals in the second column.
	/// </summary>
	/// <remarks>
	/// <strong>Right half, and the guide's own order:</strong> down-left,
	/// down-right, up-left, up-right, from the top of the second column.
	/// </remarks>
	public void Test_TheDiagonalsLiveInTheSecondColumn()
	{
		AssertEq(
			WolfCharacterSheet.Locate(
				WolfCharacter.PassDownLeft, true, out var dlRow, out var dlCol),
			true,
			"**and down-left is found**, because an eight direction sheet has one");
		AssertEq(
			dlRow, 0, "**and it is row 0 of the second column**;"
			+ $" the row is {dlRow}");
		AssertEq(
			dlCol, 1,
			"**and column 1**, the right half of the sheet — a reader that put"
			+ $" the diagonals in the left column would draw the wrong character;"
			+ $" it is {dlCol}");
		AssertEq(
			WolfCharacterSheet.Locate(
				WolfCharacter.PassUpRight, true, out var urRow, out var urCol),
			true,
			"and up-right is found");
		AssertEq(
			urRow, 3,
			"**and it is row 3**, the last of the four diagonals;"
			+ $" it is {urRow}");
		AssertEq(
			urCol, 1,
			"**and still in column 1**;"
			+ $" it is {urCol}");
	}

	/// <summary>
	/// A diagonal has no cell on a four direction sheet, and that is said.
	/// </summary>
	/// <remarks>
	/// <strong>Not clamped to the nearest cardinal.</strong> A reader that
	/// clamped would draw a figure facing down while it walks down-left, and the
	/// setting guide says four and eight cannot be mixed in one game — so a
	 /// diagonal on a four direction sheet is a misconfiguration, not a case to
	/// approximate.
	/// </remarks>
	public void Test_ADiagonalHasNoCellOnFourDirections()
	{
		AssertEq(
			WolfCharacterSheet.Locate(
				WolfCharacter.PassDownLeft, false, out _, out _),
			false,
			"**and down-left is refused**, because a four direction sheet has no"
			+ " cell for it and a reader that clamped it to down would draw a"
			+ " figure facing down while it walks down-left");
	}

	// ---- Der Laufzyklus

	/// <summary>
	/// The walk cycle is B → A → B → C → B, and not A → B → C.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>The middle cell appears twice and the first once.</strong> The
	/// guide writes <c>B-&gt;A-&gt;B-&gt;C-&gt;B-&gt;…</c>, and the three cells are A, B
	/// and C from the left.
	/// </para>
	/// <para>
	/// A reader that played them in order would show a figure stepping forward
	/// three times and then snapping back, and a walk cycle that does not return
	/// to its middle pose is a walk cycle that looks like a hiccup.
	/// </para>
	/// </remarks>
	public void Test_TheWalkCycleReturnsToTheMiddle()
	{
		var zyklus = new int[8];
		for (var i = 0; i < zyklus.Length; i++)
		{
			zyklus[i] = WolfCharacterSheet.WalkPattern(i, WolfCharacterSheet.Patterns3);
		}

		AssertEq(zyklus[0], 1,
			"**and the first step shows the middle cell**, which is B in the"
			+ $" guide's lettering; it is {zyklus[0]}");
		AssertEq(zyklus[1], 0,
			"**and the second shows the first cell**;"
			+ $" it is {zyklus[1]}");
		AssertEq(zyklus[2], 1,
			"**and the third is back to the middle**, which is what makes the"
			+ $" contact with the ground; it is {zyklus[2]}");
		AssertEq(zyklus[3], 2,
			"**and the fourth shows the third cell**;"
			+ $" it is {zyklus[3]}");
		AssertEq(zyklus[4], 1,
			"**and the fifth is the middle again**, so the cycle is B, A, B, C, B"
			+ $" and not a rotation of the three; it is {zyklus[4]}");
		AssertEq(zyklus[4], zyklus[0],
			"**and the cycle repeats every four steps**, which is what a reader"
			+ " that ran the cells in order would not do; a walk that shows every"
			+ " cell once per cycle never returns to the contact pose");
	}

	/// <summary>
	/// A five pattern sheet uses the same three cells.
	/// </summary>
	/// <remarks>
	/// <strong>The count changes the columns, not the cycle.</strong> The guide
	/// gives the order for three patterns; for five the extra cells are the same
	/// walk with more poses in it, and the passing pose is still the middle of
	/// what is used. A reader that ran a five pattern sheet in order would show
	/// every cell once and never return to the contact.
	/// </remarks>
	public void Test_AFivePatternSheetUsesTheSameCycle()
	{
		var three = WolfCharacterSheet.WalkPattern(0, WolfCharacterSheet.Patterns3);
		var five = WolfCharacterSheet.WalkPattern(0, WolfCharacterSheet.Patterns5);

		AssertEq(five, three,
			"**and the first cell is the same on a five pattern sheet**, because"
			+ " the order is the shape of the walk and not the length of the"
			+ $" sheet; three gives {three} and five gives {five}");
		AssertEq(
			WolfCharacterSheet.ColumnsPerRow(WolfCharacterSheet.Patterns5, false),
			WolfCharacterSheet.Patterns5,
			"**and a five pattern row is five columns wide**;"
			+ $" it is {WolfCharacterSheet.ColumnsPerRow(WolfCharacterSheet.Patterns5, false)}");
		AssertEq(
			WolfCharacterSheet.ColumnsPerRow(WolfCharacterSheet.Patterns3, true),
			WolfCharacterSheet.Patterns3WithIdle,
			"**and a T sheet is four wide**, because the idle frame is added to"
			+ " the left of each direction's walk;"
			+ $" it is {WolfCharacterSheet.ColumnsPerRow(WolfCharacterSheet.Patterns3, true)}");
	}

	// ---- Die Stillstehframes

	/// <summary>
	/// A plain sheet has no idle cell, and that is said.
	/// </summary>
	/// <remarks>
	/// <strong>-1 and not a fabricated cell.</strong> A standing figure on a
	/// plain sheet shows the first walk cell, because the format has nothing
	/// else for it — and a reader that invented an idle column would draw a
	/// cell the artist's sheet does not contain.
	/// </remarks>
	public void Test_APlainSheetHasNoIdleCell()
	{
		AssertEq(
			WolfCharacterSheet.IdleCell(0, WolfCharacterSheet.Patterns3, 0),
			-1,
			"**and a sheet without idle frames says -1**, which is the honest"
			+ $" answer; it is {WolfCharacterSheet.IdleCell(0, WolfCharacterSheet.Patterns3, 0)}");
		AssertEq(
			WolfCharacterSheet.IdleCell(0, WolfCharacterSheet.Patterns3, 1),
			0,
			"**and a T sheet's single idle is column 0**, the cell the"
			+ $" specification adds to the left; it is {WolfCharacterSheet.IdleCell(0, WolfCharacterSheet.Patterns3, 1)}");
	}

	/// <summary>
	/// The idle cycle runs the other way from the walk.
	/// </summary>
	/// <remarks>
	/// <strong>2 → 3 → 2 → 1 → 2 → 3, against the walk's 1 → 0 → 1 → 2.</strong> The
	/// TX specification gives the idle order, and a reader that used one order
	/// for both would have a figure's idle animation step forward while the walk
	 /// steps back.
	/// </remarks>


	/// <summary>
	/// A standing figure and a walking one are different cells on a T sheet.
	/// </summary>
	/// <remarks>
	/// <strong>The idle sits to the left of the walk, so its column is
	/// smaller.</strong> A reader that added the offset the other way would put
	/// the standing pose in the middle of the walk — a figure that never stops
	/// walking and never appears to stand.
	/// </remarks>
	public void Test_StandingAndWalkingAreDifferentCells()
	{
		var (board, guard) = BoardWithGuard();
		board.CharacterIdleFrames = 1;
		guard.Facing = WolfCharacter.PassDown;
		guard.AnimationStep = 0;

		guard.IsWalking = false;
		var stood = board.CellOf(guard, out var stoodRow, out var stoodCol);
		guard.IsWalking = true;
		var walked = board.CellOf(guard, out var walkedRow, out var walkedCol);

		AssertEq(stood, true,
			"**and a standing figure is placed**, because a T sheet has an idle"
			+ " cell for every direction");
		AssertEq(walked, true,
			"**and so is a walking one**");
		AssertEq(stoodRow, walkedRow,
			"**and both are in the same row**, because the facing did not change"
			+ $" — it is {stoodRow} against {walkedRow}");
		AssertEq(
			stoodCol, 0,
			"**and the standing one is at column 0**, the idle cell added to the"
			+ $" left; it is {stoodCol}");
		// **Column 2, and that is B on a T sheet.** The T form adds one idle
		// cell to the left of each direction's walk, so the row reads
		// idle, A, B, C — and step 0 of the walk cycle is B, which lands on
		// column 2. An earlier version of this test expected column 1, which
		// is A: the cell the cycle shows *second*, not first. **The offset is
		// right and the test was counting the walk's own index instead of its
		// place on the sheet.**
		AssertEq(
			walkedCol, 2,
			"**and the walking one is at column 2**, which is B after the idle"
			+ $" — the row reads idle, A, B, C; it is {walkedCol}");
	}

	/// <summary>
	/// A figure with no cell on the sheet is told, not approximated.
	/// </summary>
	/// <remarks>
	/// <strong>The board returns false and says where it would have drawn.</strong>
	/// A diagonal on a four direction sheet is a misconfiguration, and the
	/// setting guide says four and eight cannot be mixed — so the honest answer
	/// is "no cell" and not the nearest cardinal.
	/// </remarks>
	public void Test_AFigureWithNoCellIsTold()
	{
		var (board, guard) = BoardWithGuard();
		board.EightDirectionCharacters = false;
		guard.Facing = WolfCharacter.PassDownLeft;

		AssertEq(
			board.CellOf(guard, out _, out _), false,
			"**and the board refuses**, because a four direction sheet has no"
			+ " down-left cell and a reader that drew the nearest cardinal would"
			+ " show a figure facing down while it walks down-left");
	}

	/// <summary>
	/// The animation advances only while walking, and at the frequency.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>The frequency is frames per step, and the order is the opposite
	/// of the speed.</strong> The help writes <c>アニメ頻度[早0-6遅]</c> — often
	/// to rarely — while the move speed is slow to fast. A reader that divided
	/// by the frequency, or that used the speed, would make a figure whose feet
	/// blur also cross the map in a blur.
	/// </para>
	/// <para>
	/// <strong>Zero is every frame and not never.</strong> The help's scale puts
	/// 0 at the fast end, and a reader that divided by zero would freeze the
	/// animation entirely.
	/// </para>
	/// </remarks>
	public void Test_TheAnimationAdvancesAtTheFrequency()
	{
		var (board, guard) = BoardWithGuard();
		guard.AnimationFrequency = 0;
		guard.IsWalking = true;

		board.TickAnimation();
		AssertEq(
			guard.AnimationStep, 1,
			"**and at frequency 0 the animation advances every frame**, which is"
			+ $" what the fast end of the scale means; it is {guard.AnimationStep}");

		var slow = board.Add(new WolfCharacter { Id = 2, X = 1, Y = 1, AnimationFrequency = 6 });
		slow.IsWalking = true;
		board.TickAnimation();
		AssertEq(
			slow.AnimationStep, 0,
			"**and at frequency 6 it has not moved yet**, because six frames pass"
			+ $" between steps; it is {slow.AnimationStep}");
		for (var i = 0; i < 6; i++)
		{
			board.TickAnimation();
		}
		AssertEq(
			slow.AnimationStep, 1,
			"**and after six more it has advanced once**, which is the slow end"
			+ $" of the scale; it is {slow.AnimationStep}");

		guard.IsWalking = false;
		var before = guard.AnimationStep;
		board.TickAnimation();
		AssertEq(
			guard.AnimationStep, before,
			"**and a standing figure does not advance**, because a T sheet"
			+ " animates the idle cells instead and a reader that advanced both"
			+ $" would walk on the spot; it is {guard.AnimationStep}");
	}

	/// <summary>
	/// A finished route stands still.
	/// </summary>
	/// <remarks>
	/// <strong>The animation stops with the route.</strong> A reader that left
	/// IsWalking set would show a guard that has arrived walking in place for
	/// ever — which is the sort of thing a player notices and does not report.
	/// </remarks>
	public void Test_AFinishedRouteStandsStill()
	{
		var (board, guard) = BoardWithGuard();
		board.StartRoute(1, new WolfMoveRoute
		{
			Steps =
			[
				new WolfMoveRouteStep { Type = WolfMoveRouteType.FacingDown },
			],
			Mode = WolfMoveRouteMode.Custom,
		});

		board.Tick();
		AssertEq(
			guard.IsWalking, false,
			"**and a figure whose route is done is not walking**, because a"
			+ " facing step moves nobody and the route has no steps left;"
			+ $" it is {guard.IsWalking}");
	}

	public void Test_TheIdleCycleRunsTheOtherWay()
	{
		// **The idle cycle is 2, 3, 2, 1 against the walk's 1, 0, 1, 2** — the
		// specification's own arrow. A reader that used one order for both would
		// have a figure's idle step forward while its walk steps back, which is
		// the kind of wrong a player sees and does not report.
		//
		// **These four calls throw "Attempted to divide by zero" in the run and
		// I could not find the cause in sixteen measurements.** `IdleCell` has
		// no division at all — it is `pIndex % 4` — `WalkPattern` has none
		// either, the whole file has none, the constant reads 3, a test that
		// touches only the constant passes, and renaming the suite and the
		// method moved the name in the report and nothing else. Deleting
		// `obj`, `bin` and `.godot/mono` does not change it.
		//
		// **The rule is measured and implemented; the run is not understood.**
		// That is the state this card is in, and the card is VERIFY for it.
		AssertEq(
			WolfCharacterSheet.IdleCell(0, WolfCharacterSheet.Patterns3, 3), 1,
			"**and the first idle is the second frame**, which is 停止2 in the"
			+ " specification's lettering");
		AssertEq(
			WolfCharacterSheet.IdleCell(1, WolfCharacterSheet.Patterns3, 3), 2,
			"**and the second is the third**");
		AssertEq(
			WolfCharacterSheet.IdleCell(2, WolfCharacterSheet.Patterns3, 3), 1,
			"**and the third is back to the second**, the shape the walk has");
		AssertEq(
			WolfCharacterSheet.IdleCell(3, WolfCharacterSheet.Patterns3, 3), 0,
			"**and the fourth is the first**, so the cycle is 2, 3, 2, 1 and not"
			+ " the walk's order");
	}

}
