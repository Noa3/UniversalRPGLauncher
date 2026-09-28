using System;

using UniversalRPG.Tests.Framework;
using UniversalRPG.Wolf;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// WOLF's six chip passability states, and the two layers they ask about.
/// </summary>
/// <remarks>
/// <para>
/// The editor's tileset window cycles them
/// <c>○ → × → ▲ → ★ → □ → ○</c>, and the tileset help gives the meaning of
/// each. <strong>A reader with a boolean loses four of the six</strong>, and
/// the sixth is the one that decides movement: a chip marked <c>↓</c> is
/// passable or not according to the layer below, and where there is no lower
/// layer it is passable.
/// </para>
/// <para>
/// That last case is an ordinary one. A floor with nothing under it is the most
/// common tile in a map, and a reader that refused it would make the entire
/// bottom layer impassable.
/// </para>
/// </remarks>
public partial class TestWolfPassability : TestBase
{
	// ---- Die sechs Zustaende

	/// <summary>
	/// The six states are the editor's six, in the editor's order.
	/// </summary>
	public void Test_TheSixStatesAreTheEditorsSix()
	{
		AssertEq(WolfChipPassability.Passable, 0,
			"**and ○ is 0**, the first in the cycle ○ → × → ▲ → ★ → □ → ○;"
			+ $" it is {WolfChipPassability.Passable}");
		AssertEq(WolfChipPassability.Blocked, 1,
			"**and × is 1**, not passable;"
			+ $" it is {WolfChipPassability.Blocked}");
		AssertEq(WolfChipPassability.PassableHiddenBehind, 2,
			"**and ▲ is 2**, passable with the figure hidden behind it;"
			+ $" it is {WolfChipPassability.PassableHiddenBehind}");
		AssertEq(WolfChipPassability.PassableDrawnOver, 3,
			"**and ★ is 3**, passable and always drawn over the figure;"
			+ $" it is {WolfChipPassability.PassableDrawnOver}");
		AssertEq(WolfChipPassability.PassableHalfFeet, 4,
			"**and □ is 4**, passable with half transparent feet — and the help"
			+ " says not at all when the figure's height is one or more;"
			+ $" it is {WolfChipPassability.PassableHalfFeet}");
		AssertEq(WolfChipPassability.FollowsLowerLayer, 5,
			"**and ↓ is 5**, the one that asks the layer below;"
			+ $" it is {WolfChipPassability.FollowsLowerLayer}");
		AssertEq(WolfChipPassability.MaxState, 5,
			"**and the highest is 5**, because the editor lists six states"
			+ $" numbered from zero; it is {WolfChipPassability.MaxState}");
	}

	/// <summary>
	/// Four of the six are passable and one is not.
	/// </summary>
	/// <remarks>
	/// <strong>Only × blocks.</strong> ▲, ★ and □ all say passable, and each
	/// adds a drawing rule on top. A reader that read "hidden behind" or "drawn
	/// over" as impassable would put a guard outside a staircase railing.
	/// </remarks>
	public void Test_FourOfTheSixArePassable()
	{
		foreach (var state in new[]
		{
			WolfChipPassability.Passable,
			WolfChipPassability.PassableHiddenBehind,
			WolfChipPassability.PassableDrawnOver,
			WolfChipPassability.PassableHalfFeet,
		})
		{
			AssertEq(
				WolfChipPassability.AllowsStanding(state, -1), true,
				$"**and state {state} is passable** ({WolfChipPassability.Describe(state)}),"
				+ " because it adds a drawing rule and not an obstacle;"
				+ $" it is {WolfChipPassability.AllowsStanding(state, -1)}");
		}
		AssertEq(
			WolfChipPassability.AllowsStanding(WolfChipPassability.Blocked, -1),
			false,
			"**and × is the only state that blocks**;"
			+ $" it is {WolfChipPassability.AllowsStanding(WolfChipPassability.Blocked, -1)}");
	}

	/// <summary>
	/// A ↓ chip takes the layer below's answer, and passable where there is
	/// none.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>Where there is no lower layer, ↓ is passable</strong>, and that
	/// is the help's own answer — <c>下のレイヤーに合わせます。下のレイヤーがない
	/// 場合は通行可能です</c> — and not a decision this reader makes.
	/// </para>
	/// <para>
	/// A reader that refused the tile instead would make the whole bottom layer
	/// of a map impassable, because a floor tile with nothing under it is the
	/// most ordinary tile there is.
	/// </para>
	/// </remarks>
	public void Test_TheLowerLayerDecidesTheArrowChip()
	{
		AssertEq(
			WolfChipPassability.AllowsStanding(
				WolfChipPassability.FollowsLowerLayer, WolfChipPassability.Passable),
			true,
			"**and a ↓ chip over a passable one is passable**;"
			+ $" it is {WolfChipPassability.AllowsStanding(WolfChipPassability.FollowsLowerLayer, WolfChipPassability.Passable)}");
		AssertEq(
			WolfChipPassability.AllowsStanding(
				WolfChipPassability.FollowsLowerLayer, WolfChipPassability.Blocked),
			false,
			"**and over a blocked one it is blocked**, which is a bridge over"
			+ " water behaving like the water under it;"
			+ $" it is {WolfChipPassability.AllowsStanding(WolfChipPassability.FollowsLowerLayer, WolfChipPassability.Blocked)}");
		AssertEq(
			WolfChipPassability.AllowsStanding(
				WolfChipPassability.FollowsLowerLayer, -1),
			true,
			"**and with no lower layer at all it is passable**, because the help"
			+ " says so — a reader that refused it would freeze the hero on the"
			+ $" floor; it is {WolfChipPassability.AllowsStanding(WolfChipPassability.FollowsLowerLayer, -1)}");
	}

	/// <summary>
	/// A state the editor does not offer is refused.
	/// </summary>
	/// <remarks>
	/// <strong>Refused and not treated as passable.</strong> A reader that fell
	/// through to "not ×, therefore passable" would walk a figure onto a chip
	/// the game had never heard of, and the symptom would be a hero walking
	/// through a wall nobody had drawn.
	/// </remarks>
	public void Test_AnUnknownStateIsRefused()
	{
		AssertEq(
			WolfChipPassability.AllowsStanding(6, -1), false,
			"**and a seventh state is refused**, because the editor lists six"
			+ $" and this reader does not guess a seventh;"
			+ $" it is {WolfChipPassability.AllowsStanding(6, -1)}");
		AssertEq(
			WolfChipPassability.Describe(6),
			"a state the editor does not offer",
			"**and the diagnostic says so**, because a refusal that names the"
			+ " value is one a reader can act on;"
			+ $" it is \"{WolfChipPassability.Describe(6)}\"");
	}

	// ---- Das Gitter

	/// <summary>
	/// The two layers, and the upper one decides.
	/// </summary>
	/// <remarks>
	/// <strong>The upper layer answers, and the lower only where the upper
	/// asks.</strong> That is the editor's rule: a chip on the upper layer
	/// marked ↓ takes the lower layer's answer, and every other upper chip
	/// answers for itself — a ★ tile over water is passable, because ★ says
	/// passable.
	/// </remarks>
	public void Test_TheTwoLayersAreTwoLayers()
	{
		var grid = new WolfPassabilityGrid(4, 4);
		grid.Set(1, 1, WolfChipPassability.Blocked, false);
		grid.Set(1, 1, WolfChipPassability.FollowsLowerLayer, true);

		AssertEq(
			grid.AllowsStanding(1, 1), false,
			"**and a ↓ chip over a blocked tile is blocked**, which is the whole"
			+ " reason there are two layers;"
			+ $" it is {grid.AllowsStanding(1, 1)}");

		grid.Set(1, 1, WolfChipPassability.Blocked, false);
		grid.Set(1, 1, WolfChipPassability.PassableDrawnOver, true);
		AssertEq(
			grid.AllowsStanding(1, 1), true,
			"**and a ★ chip over a blocked tile is passable**, because ★ says"
			+ " passable and only ↓ asks what is underneath — a reader that let"
			+ " the lower layer decide everything would make a signpost over a"
			+ $" wall unusable; it is {grid.AllowsStanding(1, 1)}");
	}

	/// <summary>
	/// Outside the map is not standing anywhere.
	/// </summary>
	/// <remarks>
	/// <strong>Refused at every edge.</strong> A figure at the right edge may
	/// not step off it, and a reader that grew the array on demand would let a
	/// guard walk into the void and keep walking — with no error anywhere,
	/// because every read was in range by then.
	/// </remarks>
	public void Test_OutsideTheMapIsNotStandingAnywhere()
	{
		var grid = new WolfPassabilityGrid(3, 3);

		AssertEq(grid.AllowsStanding(-1, 0), false,
			"**and one left of the map is refused**;"
			+ $" it is {grid.AllowsStanding(-1, 0)}");
		AssertEq(grid.AllowsStanding(3, 0), false,
			"**and one right of it**, because the width is three and the"
			+ $" columns are 0 to 2; it is {grid.AllowsStanding(3, 0)}");
		AssertEq(grid.AllowsStanding(0, 3), false,
			"**and one below it**;"
			+ $" it is {grid.AllowsStanding(0, 3)}");
		AssertEq(grid.AllowsStanding(2, 2), true,
			"**and the last tile inside is passable**, which is the fourth edge"
			+ $" and the one a boundary check that used >= would refuse;"
			+ $" it is {grid.AllowsStanding(2, 2)}");
	}

	/// <summary>
	/// A figure walks on the map and stops at its walls.
	/// </summary>
	/// <remarks>
	/// <strong>This is what the grid is for.</strong> The previous card's
	/// character could step anywhere, because a character with no grid cannot
	/// step at all and the route tests were measuring that refusal rather than
	/// a wall. A guard that walks through a wall is a visible failure in every
	/// game; a guard that never moves is a visible failure in every game too,
	/// and the difference between the two is this file.
	/// </remarks>
	public void Test_AFigureStopsAtAWall()
	{
		var grid = new WolfPassabilityGrid(5, 5);
		// **The wall is at four, and not at three.** The figure starts at two
		// and the first step puts it on three, so a wall at three would refuse
		// the very first step and the test would measure the refusal twice
		// without ever seeing a figure walk. An earlier version of this test
		// put the wall at three and called the first step "open".
		grid.Set(4, 0, WolfChipPassability.Blocked, true);
		var guard = new WolfCharacter { Id = 1, X = 2, Y = 0 };
		guard.PassabilityGrid = grid;

		AssertEq(
			guard.Step(WolfCharacter.PassRight), true,
			"**and the first step right succeeds**, because the tile at three is"
			+ $" open; it is {guard.Step(WolfCharacter.PassRight)}");
		AssertEq(
			guard.X, 3,
			"**and the figure is on three**;"
			+ $" X is {guard.X}");
		AssertEq(
			guard.Step(WolfCharacter.PassRight), false,
			"**and the next one is refused**, because the wall is at four;"
			+ $" it is {guard.Step(WolfCharacter.PassRight)}");
		AssertEq(
			guard.X, 3,
			"**and the figure stayed put**, which is what a refusal means and"
			+ " not a step of half a tile;"
			+ $" X is {guard.X}");
	}

	/// <summary>
	/// A figure with no map at all cannot step.
	/// </summary>
	/// <remarks>
	/// <strong>Refused, and that is the honest answer.</strong> "The map was
	/// not read" is not "the tile is passable", and a reader that treated a
	/// missing grid as open ground would let a guard walk through every wall on
	/// every map whose chips it could not read — with no error anywhere, because
	/// a walk through a wall is a walk.
	/// </remarks>
	public void Test_AFigureWithNoMapCannotStep()
	{
		var guard = new WolfCharacter { Id = 1, X = 1, Y = 1 };

		AssertEq(
			guard.Step(WolfCharacter.PassRight), false,
			"**and stepping is refused**, because there is no map to ask and"
			+ $" this reader does not assume open ground; it is {guard.Step(WolfCharacter.PassRight)}");
		AssertEq(
			guard.Facing, WolfCharacter.PassRight,
			"**and the figure still turns to face it**, which is the same rule a"
			+ " wall produces — the refusal is about the position, not the"
			+ $" facing; it is {guard.Facing}");
	}

	/// <summary>
	/// The board hands the map to the figures, including the ones already there.
	/// </summary>
	/// <remarks>
	/// <strong>Every figure, and not only the ones placed after the map.</strong>
	/// A figure placed before the map was loaded has the same question as one
	/// placed after, and a reader that handed the grid at placement time would
	/// leave the earlier figures walking through walls.
	/// </remarks>
	public void Test_TheBoardHandsTheMapToEveryFigure()
	{
		var board = new WolfCharacterBoard(new WolfVariableBands());
		var before = board.Add(new WolfCharacter { Id = 1, X = 1, Y = 1 });
		var grid = new WolfPassabilityGrid(5, 5);
		grid.Set(2, 1, WolfChipPassability.Blocked, true);
		board.LoadMap(1, grid);
		var after = board.Add(new WolfCharacter { Id = 2, X = 1, Y = 1 });

		AssertEq(
			before.Step(WolfCharacter.PassRight), false,
			"**and the figure placed before the map cannot step into the wall**,"
			+ " because LoadMap handed it the grid as well as the new figure;"
			+ $" it is {before.Step(WolfCharacter.PassRight)}");
		AssertEq(
			after.Step(WolfCharacter.PassRight), false,
			"**and so cannot the one placed after**, because the two are asked"
			+ " the same question and answered the same way;"
			+ $" it is {after.Step(WolfCharacter.PassRight)}");
	}

	/// <summary>
	/// The map's size is the grid's and not a field beside it.
	/// </summary>
	/// <remarks>
	/// <strong>One source, because two could disagree.</strong> A board with a
	/// width of 20 and a grid of 10 would let a figure walk to tile 15 and read
	/// a row that does not exist — and a reader that answered "passable" out
	/// there would put a guard in the void.
	/// </remarks>
	public void Test_TheSizeComesFromTheGrid()
	{
		var board = new WolfCharacterBoard(new WolfVariableBands());
		board.LoadMap(3, new WolfPassabilityGrid(12, 7));

		AssertEq(
			board.Width, 12,
			"**and the width is the grid's width**;"
			+ $" it is {board.Width}");
		AssertEq(
			board.Height, 7,
			"**and so is the height**;"
			+ $" it is {board.Height}");
		AssertEq(
			board.MapId, 3,
			"**and the map id is the one that was loaded**;"
			+ $" it is {board.MapId}");
	}
}
