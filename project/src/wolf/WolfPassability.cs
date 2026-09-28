using System;
using System.Collections.Generic;

namespace UniversalRPG.Wolf;

/// <summary>
/// One of the six passability states a WOLF chip can have.
/// </summary>
/// <remarks>
/// <para>
/// The editor's tileset window cycles them in the order
/// <c>○ → × → ▲ → ★ → □ → ○</c>, and the meaning of each is in the tileset
/// help:
/// </para>
/// <list type="bullet">
/// <item><description><c>○</c> passable.</description></item>
/// <item><description><c>×</c> not passable.</description></item>
/// <item><description>
/// <c>▲</c> passable, and a figure behind it is hidden — the help notes this
/// tile is in effect one tile high, so a 1.5 tile figure shows half.
/// </description></item>
/// <item><description><c>★</c> passable, and always drawn over the figure.</description></item>
/// <item><description>
/// <c>□</c> passable, and the figure's feet go half transparent — and not at
/// all when the figure's height is one or more.
/// </description></item>
/// <item><description>
/// <c>↓</c> passable or not according to the layer below; where there is no
/// lower layer, passable.
/// </description></item>
/// </list>
/// <para>
/// <strong>Six states and not two.</strong> A reader with a boolean loses
/// <c>▲</c>, <c>★</c>, <c>□</c> and <c>↓</c>, and the last one is the one that
/// matters for movement: a bridge over water is passable because the tile
/// below it is, and a reader that asked the bridge's own chip would say no.
/// </para>
/// </remarks>
public static class WolfChipPassability
{
	/// <summary>○ — passable.</summary>
	public const int Passable = 0;

	/// <summary>× — not passable.</summary>
	public const int Blocked = 1;

	/// <summary>▲ — passable, and a figure behind it is hidden.</summary>
	public const int PassableHiddenBehind = 2;

	/// <summary>★ — passable, and always drawn over the figure.</summary>
	public const int PassableDrawnOver = 3;

	/// <summary>□ — passable, and the figure's feet go half transparent.</summary>
	public const int PassableHalfFeet = 4;

	/// <summary>↓ — passable or not according to the layer below.</summary>
	public const int FollowsLowerLayer = 5;

	/// <summary>The highest state the editor offers.</summary>
	public const int MaxState = 5;

	/// <summary>
	/// Whether a state allows a figure to walk onto the tile.
	/// </summary>
	/// <param name="pState">One of the six numbers.</param>
	/// <param name="pLowerState">
	/// The state of the layer below, or -1 where there is none.
	/// </param>
	/// <returns>
	/// True when a figure may stand on the tile, resolving the ↓ state against
	/// the layer below.
	/// </returns>
	/// <remarks>
	/// <para>
	/// <strong>Four of the six are passable and two are not.</strong> Only
	/// <c>×</c> blocks, and <c>↓</c> asks whatever is below it — which the help
	/// answers with "passable" where there is nothing below.
	/// </para>
	/// <para>
	/// <strong>Where there is no lower layer, ↓ is passable.</strong> The help
	/// says <c>下のレイヤーに合わせます。下のレイヤ���がない場合は通行可能です</c>,
	/// and a reader that refused the tile instead would make the entire bottom
	/// layer of a map impassable — a floor with nothing under it is the most
	/// ordinary tile there is.
	/// </para>
	/// </remarks>
	public static bool AllowsStanding(int pState, int pLowerState)
	{
		// **Unknown states are refused, and not treated as passable.** The six
		// are the editor's list; a seventh is a value the format does not
		// define, and a reader that fell through to "not ×, therefore
		// passable" would walk a figure onto a chip the game had never heard of.
		return pState switch
		{
			Passable or PassableHiddenBehind or PassableDrawnOver
				or PassableHalfFeet => true,
			FollowsLowerLayer =>
				// **No lower layer means passable**, and that is the help's own
				// answer rather than a decision this reader makes.
				pLowerState < 0 || AllowsStanding(pLowerState, -1),
			_ => false,
		};
	}

	/// <summary>
	/// A state's name, for a diagnostic that says which one it was.
	/// </summary>
	public static string Describe(int pState)
	{
		return pState switch
		{
			Passable => "passable",
			Blocked => "not passable",
			PassableHiddenBehind => "passable, figure hidden behind",
			PassableDrawnOver => "passable, drawn over the figure",
			PassableHalfFeet => "passable, half transparent feet",
			FollowsLowerLayer => "passable according to the layer below",
			_ => "a state the editor does not offer",
		};
	}
}

/// <summary>
/// The passability of every tile on the map, in two layers.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Two layers, because the ↓ state asks the one below.</strong> The
/// editor has a lower layer and an upper layer, and a chip on the upper layer
/// marked ↓ is passable or not according to the chip underneath it. A reader
/// with one array could not answer that at all, and a bridge over water is the
/// ordinary case where it matters.
/// </para>
/// <para>
/// <strong>Out of range is blocked, and says so.</strong> A figure at the edge
/// of a map may not step off it, and a reader that grew the array on demand
/// would let a guard walk off the right edge and keep going.
/// </para>
/// </remarks>
public sealed class WolfPassabilityGrid
{
	private readonly int[] _lower;
	private readonly int[] _upper;
	private readonly int _width;
	private readonly int _height;

	/// <summary>Creates a grid of the given size, every tile passable.</summary>
	public WolfPassabilityGrid(int pWidth, int pHeight)
	{
		_width = Math.Max(0, pWidth);
		_height = Math.Max(0, pHeight);
		_lower = new int[_width * _height];
		_upper = new int[_width * _height];
		// **Everything passable, because that is the safest default and not the
		// other way round.** A grid that starts blocked would freeze a figure
		// on a map whose passability this reader could not load, and a game
		// would show an empty screen with a hero who cannot move.
		Array.Fill(_lower, WolfChipPassability.Passable);
		Array.Fill(_upper, WolfChipPassability.Passable);
	}

	/// <summary>How wide the grid is, in tiles.</summary>
	public int Width => _width;

	/// <summary>How tall the grid is, in tiles.</summary>
	public int Height => _height;

	/// <summary>Whether a tile is inside the grid.</summary>
	public bool IsInRange(int pX, int pY)
	{
		return pX >= 0 && pY >= 0 && pX < _width && pY < _height;
	}

	/// <summary>Writes one tile's passability state.</summary>
	/// <returns>False when the tile is outside the grid.</returns>
	public bool Set(int pX, int pY, int pState, bool pUpperLayer)
	{
		if (!IsInRange(pX, pY))
		{
			return false;
		}
		(pUpperLayer ? _upper : _lower)[pY * _width + pX] = pState;
		return true;
	}

	/// <summary>Reads one tile's passability state.</summary>
	/// <returns>-1 when the tile is outside the grid.</returns>
	public int Get(int pX, int pY, bool pUpperLayer)
	{
		if (!IsInRange(pX, pY))
		{
			return -1;
		}
		return (pUpperLayer ? _upper : _lower)[pY * _width + pX];
	}

	/// <summary>
	/// Whether a figure may stand on a tile, resolving the ↓ state.
	/// </summary>
	/// <remarks>
	/// <strong>The upper layer decides, and the lower only where the upper
	/// asks.</strong> That is the editor's rule and not this reader's: a chip
	/// on the upper layer marked ↓ takes the lower layer's answer, and every
	/// other upper chip answers for itself.
	/// </remarks>
	public bool AllowsStanding(int pX, int pY)
	{
		// **Outside the map is not standing anywhere.** A figure at the edge
		// may not step off it, and a reader that answered "passable" out there
		// would let a guard walk into the void and keep walking.
		if (!IsInRange(pX, pY))
		{
			return false;
		}
		return WolfChipPassability.AllowsStanding(
			Get(pX, pY, true),
			Get(pX, pY, false));
	}

	/// <summary>
	/// The lower layer's state, used by a chip that asks for it.
	/// </summary>
	/// <remarks>
	/// **-1 where there is no lower layer, and not a default state.** The help
	/// says a ↓ chip is passable where there is nothing below, and that answer
	/// depends on knowing there is nothing below — a reader that substituted
	/// ○ for "no layer" would make the two cases indistinguishable.
	/// </remarks>
	public int LowerLayerState(int pX, int pY)
	{
		return Get(pX, pY, false);
	}
}
