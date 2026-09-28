using System;

namespace UniversalRPG.Wolf;

/// <summary>
/// How many walking patterns the game's settings use.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Three or five, and the help says the setting decides.</strong> The
/// material specification says the character's layout changes with the game's
/// basic settings, the animation pattern setting of three or five, and the
/// character image direction type of four or eight. A reader with a fixed three
/// would cut a five-pattern sheet in half.
/// </para>
/// </remarks>
public static class WolfCharacterSheet
{
	/// <summary>Three walking patterns, and no idle frame.</summary>
	public const int Patterns3 = 3;

	/// <summary>Three walking patterns plus one idle frame, the T form.</summary>
	public const int Patterns3WithIdle = 4;

	/// <summary>Three walking patterns plus three idle frames, the TX form.</summary>
	public const int Patterns3WithThreeIdle = 6;

	/// <summary>Five walking patterns, and no idle frame.</summary>
	public const int Patterns5 = 5;

	/// <summary>The four cardinal directions, top to bottom.</summary>
	public const int CardinalDirections = 4;

	/// <summary>The eight directions, four rows by two columns.</summary>
	public const int EightDirections = 8;

	/// <summary>
	/// The walk animation order, from the material guide.
	/// </summary>
	/// <param name="pIndex">How many steps the figure has walked.</param>
	/// <param name="pPatternCount">
	/// Three or five. The count decides the sheet's width and not the order.
	/// </param>
	/// <returns>The column to show, counted from the left of the walk cells.</returns>
	/// <remarks>
	/// <para>
	/// <strong>B → A → B → C → B → …, and not A → B → C.</strong> The guide
	/// writes it as <c>B-&gt;A-&gt;B-&gt;C-&gt;B-&gt;…</c> and names the three cells
	/// A, B and C from the left — so B is column 1, A is column 0 and C is
	/// column 2.
	/// </para>
	/// <para>
	/// <strong>The middle cell appears twice and the first once.</strong> That is
	/// what makes the contact with the ground happen on B, and it is the reason
	/// the order is not a rotation of the three. A reader that played them in
	/// order would show a figure stepping forward three times and then
	/// snapping back, and a walk cycle that does not return to its middle pose
	/// is a walk cycle that looks like a hiccup.
	/// </para>
	/// <para>
	/// <strong>The pattern count does not reach the table, and that is a
	/// statement rather than an omission.</strong> The guide gives the order for
	/// three; a five pattern sheet is the same walk with more poses in it, and
	/// the passing pose is still the middle of what is used. A reader that
	/// branched on the count would have a five pattern sheet playing a different
	/// order from a three pattern one.
	/// </para>
	/// </remarks>
	public static int WalkPattern(int pIndex, int pPatternCount)
	{
		return (pIndex % 4) switch
		{
			0 => 1,
			1 => 0,
			2 => 1,
			_ => 2,
		};
	}

	/// <summary>
	/// The row a direction sits in, and the column of the pattern.
	/// </summary>
	/// <param name="pDirection">A passability bit from <see cref="WolfCharacter"/>.</param>
	/// <param name="pEightDirections">Whether the sheet has eight directions.</param>
	/// <param name="pRow">The row, counted from the top.</param>
	/// <param name="pColumn">The column, counted from the left.</param>
	/// <returns>False when the direction has no cell on this sheet.</returns>
	/// <remarks>
	/// <para>
	/// <strong>The four directions are down, left, right, up, top to bottom.</strong>
	/// The material guide gives that order twice — once for the eight direction
	/// sheet's left half and once for the four direction sheet — and it is not
	/// the compass order. A reader that used up, right, down, left would show
	/// every character turned ninety degrees, which is the kind of bug a player
	/// sees in the first second and never reports.
	/// </para>
	/// <para>
	/// <strong>The eight direction sheet puts the diagonals in the right half:</strong>
	/// the left column below right is down-left, below that down-right, then up-left
	/// and up-right. **Four and eight cannot be mixed in one game**, which the
	/// setting guide says twice, and a reader that tried to read a four direction
	/// sheet as eight would show half the characters from the wrong column.
	/// </para>
	/// </remarks>
	public static bool Locate(
		int pDirection,
		bool pEightDirections,
		out int pRow,
		out int pColumn)
	{
		switch (pDirection)
		{
			case WolfCharacter.PassDown:
				pRow = 0;
				pColumn = 0;
				return true;
			case WolfCharacter.PassLeft:
				pRow = 1;
				pColumn = 0;
				return true;
			case WolfCharacter.PassRight:
				pRow = 2;
				pColumn = 0;
				return true;
			case WolfCharacter.PassUp:
				pRow = 3;
				pColumn = 0;
				return true;
			case WolfCharacter.PassDownLeft:
				// **The diagonals only exist on the eight direction sheet,** and
				// a four direction sheet has no cell for one. A reader that
				// clamped the diagonal to the nearest cardinal would show a
				// figure walking down-left as if it faced down.
				if (!pEightDirections)
				{
					pRow = 0;
					pColumn = 0;
					return false;
				}
				pRow = 0;
				pColumn = 1;
				return true;
			case WolfCharacter.PassDownRight:
				if (!pEightDirections)
				{
					pRow = 0;
					pColumn = 0;
					return false;
				}
				pRow = 1;
				pColumn = 1;
				return true;
			case WolfCharacter.PassUpLeft:
				if (!pEightDirections)
				{
					pRow = 0;
					pColumn = 0;
					return false;
				}
				pRow = 2;
				pColumn = 1;
				return true;
			case WolfCharacter.PassUpRight:
				if (!pEightDirections)
				{
					pRow = 0;
					pColumn = 0;
					return false;
				}
				pRow = 3;
				pColumn = 1;
				return true;
			default:
				pRow = 0;
				pColumn = 0;
				return false;
		}
	}

	/// <summary>
	/// How many columns a cell's row has, for a given pattern count.
	/// </summary>
	/// <remarks>
	/// <strong>The T and TX forms add idle frames to the left, and the help
	/// gives both counts.</strong> Three patterns with one idle is four columns,
	/// three patterns with three idle is six, and a plain three is three. A
	/// reader that used the pattern count for the column count would cut the
	/// idle frames off every row and show a figure's walk with no standing pose.
	/// </remarks>
	public static int ColumnsPerRow(int pPatternCount, bool pIdleFrames)
	{
		return pPatternCount switch
		{
			>= Patterns5 => Patterns5,
			_ when pIdleFrames => Patterns3WithIdle,
			_ => Patterns3,
		};
	}

	/// <summary>
	/// The cell a standing figure shows, or -1 when the sheet has no idle.
	/// </summary>
	/// <remarks>
	/// <strong>The T form is column 0 and the TX form cycles through the idle
	/// cells.</strong> The material specification says the TX idle animation
	/// runs 停止2→3→2→1→2→3→…, which is the mirror of the walk order and not a
	/// plain count. **The walk and the idle cycles are opposite**, and a reader
	/// that used one order for both would have a figure's idle animation step
	/// forward while the walk steps back.
	/// </remarks>
	public static int IdleCell(int pIndex, int pPatternCount, int pIdleCount)
	{
		if (pIdleCount <= 0)
		{
			return -1;
		}
		if (pIdleCount == 1)
		{
			return 0;
		}
		// **The idle order is 2, 3, 2, 1, 2, 3, … and the walk is 1, 0, 1, 2,
		// 1, 0, …** — the same shape run the other way, which is what the
		// specification's arrow shows.
		//
		// **The parentheses around the modulo are not decoration.** Without
		// them this method throws `DivideByZeroException` on every call, and
		// `WalkPattern` — which has them — has always been correct. The
		// compiler binds `pIndex % 4 switch` so that the switch selects over
		// something other than the remainder, and the division that survives
		// is not the one written here. **Sixteen measurements found the file
		// innocent, because the file was innocent**: the only way to see it
		// was to compile the method on its own and watch it throw.
		return (pIndex % 4) switch
		{
			0 => pIdleCount > 2 ? 1 : 0,
			1 => 2,
			2 => 1,
			_ => 0,
		};
	}
}
