namespace UniversalRPG.Rm2k.Presentation;

/// <summary>
/// Which way a screen transition goes, from <c>11010</c> and <c>11020</c>.
/// </summary>
/// <remarks>
/// <strong>Show and erase are the same 21 kinds read from opposite ends.</strong>
/// The reference's two <c>switch</c>es have the same arms in the same order,
/// and each pair is one kind seen forwards and backwards — <c>FadeOut</c>
/// against <c>FadeIn</c>, <c>BlindClose</c> against <c>BlindOpen</c>,
/// <c>ZoomIn</c> against <c>ZoomOut</c>.
/// <para>
/// <strong>That pairing is a trap and it is not regular.</strong> The scroll and
/// stripe arms mirror their *name* — <c>ScrollUpOut</c> against
/// <c>ScrollUpIn</c> — while the division and combine arms invert their
/// *meaning*: <c>VerticalDivision</c> pairs with <c>VerticalCombine</c>, not
/// with another division, and <c>CrossDivision</c> with <c>CrossCombine</c>.
/// A reader that mirrored the suffix would pair <c>CrossDivision</c> with
/// <c>CrossDivision</c> and animate nothing.
/// </para>
/// </remarks>
public enum Rm2kTransitionDirection
{
	/// <summary>
	/// <c>11020</c>, Show Screen: the screen appears.
	/// </summary>
	Show = 0,

	/// <summary>
	/// <c>11010</c>, Erase Screen: the screen goes away.
	/// </summary>
	Erase = 1,
}

/// <summary>
/// The 21 transition kinds, from the reference's two <c>switch</c>es in
/// <c>CommandEraseScreen</c> and <c>CommandShowScreen</c>.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Numbered by the command's one parameter, and the numbering is not
/// the same for both commands.</strong> Parameter 0 is a fade in either
/// direction, but 4 is <c>BlindClose</c> for an erase and <c>BlindOpen</c> for
/// a show, and 18 is <c>ZoomIn</c> for an erase and <c>ZoomOut</c> for a show.
/// The two tables are the same *kind list* read from opposite ends, not one
/// table with a direction flag.
/// </para>
/// <para>
/// Parameter −1 is not a kind at all: it is
/// <c>GetTransition(Transition_TeleportErase)</c> or
/// <c>Transition_TeleportShow</c>, the game-wide transition the editor's
/// settings hold. <strong>A reader that treated −1 as a kind would pick the
/// first arm of the switch, which is none</strong>, and a teleport would lose
/// its transition. See <see cref="Rm2kTransitionKind.FromParameter"/>.
/// </para>
/// <para>
/// Nothing plays a transition here. A value in this enum is a request a
/// renderer may honour.
/// </para>
/// </remarks>
public enum Rm2kTransition
{
	/// <summary>
	/// No transition at all, and what an unknown parameter falls through to.
	/// </summary>
	None = 0,

	// The erase table, parameters 0 to 20, in the reference's order.
	FadeOut = 1,
	RandomBlocks = 2,
	RandomBlocksDown = 3,
	RandomBlocksUp = 4,
	BlindClose = 5,
	VerticalStripesOut = 6,
	HorizontalStripesOut = 7,
	BorderToCenterOut = 8,
	CenterToBorderOut = 9,
	ScrollUpOut = 10,
	ScrollDownOut = 11,
	ScrollLeftOut = 12,
	ScrollRightOut = 13,
	VerticalDivision = 14,
	HorizontalDivision = 15,
	CrossDivision = 16,
	ZoomIn = 17,
	MosaicOut = 18,
	WaveOut = 19,
	CutOut = 20,

	// The show table, parameters 0 to 20. **The numbering restarts**, and the
	// names are the mirror image of the erase table's — except for the
	// division and combine arms, which invert their meaning rather than their
	// suffix.
	FadeIn = 101,
	BlindOpen = 105,
	VerticalStripesIn = 106,
	HorizontalStripesIn = 107,
	BorderToCenterIn = 108,
	CenterToBorderIn = 109,
	ScrollUpIn = 110,
	ScrollDownIn = 111,
	ScrollLeftIn = 112,
	ScrollRightIn = 113,
	VerticalCombine = 114,
	HorizontalCombine = 115,
	CrossCombine = 116,
	ZoomOut = 117,
	MosaicIn = 118,
	WaveIn = 119,
	CutIn = 120,
}

/// <summary>
/// Reads a transition parameter into a kind, and says what the two directions
/// do with the same number.
/// </summary>
/// <remarks>
/// <strong>Parameter −1 is the game's own teleport transition, not a kind.</strong>
/// The reference reaches for <c>GetTransition(Transition_TeleportErase)</c> or
/// <c>GetTransition(Transition_TeleportShow)</c>, which come from the editor's
/// settings and are not in the file's command at all. This reader has not read
/// those settings, so it reports
/// <see cref="Rm2kTransitionRequestResult.GameWideTransition"/> and stores
/// <see cref="Rm2kTransition.None"/> — **which is the honest answer and not a
/// guess**: a reader that fell through to the first arm of the switch, as the
/// reference does for a number it does not know, would make every teleport in
/// the game lose its transition and report nothing.
/// </remarks>
public static class Rm2kTransitionKind
{
	/// <summary>
	/// The parameter value that means "the game's own teleport transition".
	/// </summary>
	public const int TeleportSentinel = -1;

	/// <summary>How many kinds each direction's table has.</summary>
	public const int KindsPerDirection = 21;

	/// <summary>
	/// Maps one parameter to the kind it names, for one direction.
	/// </summary>
	/// <returns>
	/// <see cref="Rm2kTransitionRequestResult"/> so the caller can tell a
	/// refusal from a game-wide transition, which are both a stored
	/// <see cref="Rm2kTransition.None"/>.
	/// </returns>
	public static Rm2kTransitionRequestResult FromParameter(
		int pParameter, Rm2kTransitionDirection pDirection)
	{
		if (pParameter == TeleportSentinel)
		{
			return new Rm2kTransitionRequestResult(
				Rm2kTransition.None, Rm2kTransitionRequestResultKind.GameWideTransition);
		}
		if (pParameter < 0 || pParameter >= KindsPerDirection)
		{
			return new Rm2kTransitionRequestResult(
				Rm2kTransition.None, Rm2kTransitionRequestResultKind.UnknownParameter);
		}
		// The erase table, read straight out of the reference's switch.
		var erase = pParameter switch
		{
			0 => Rm2kTransition.FadeOut,
			1 => Rm2kTransition.RandomBlocks,
			2 => Rm2kTransition.RandomBlocksDown,
			3 => Rm2kTransition.RandomBlocksUp,
			4 => Rm2kTransition.BlindClose,
			5 => Rm2kTransition.VerticalStripesOut,
			6 => Rm2kTransition.HorizontalStripesOut,
			7 => Rm2kTransition.BorderToCenterOut,
			8 => Rm2kTransition.CenterToBorderOut,
			9 => Rm2kTransition.ScrollUpOut,
			10 => Rm2kTransition.ScrollDownOut,
			11 => Rm2kTransition.ScrollLeftOut,
			12 => Rm2kTransition.ScrollRightOut,
			13 => Rm2kTransition.VerticalDivision,
			14 => Rm2kTransition.HorizontalDivision,
			15 => Rm2kTransition.CrossDivision,
			16 => Rm2kTransition.ZoomIn,
			17 => Rm2kTransition.MosaicOut,
			18 => Rm2kTransition.WaveOut,
			19 => Rm2kTransition.CutOut,
			_ => Rm2kTransition.None,
		};
		if (pDirection == Rm2kTransitionDirection.Erase)
		{
			return new Rm2kTransitionRequestResult(
				erase, Rm2kTransitionRequestResultKind.Named);
		}
		// The show table, which is the same list read from the other end — and
		// **not a mirror of the names**. The stripe and scroll arms mirror their
		// suffix, while the division and combine arms invert their meaning, so
		// `CrossDivision` pairs with `CrossCombine` and a suffix mirror would
		// have paired it with itself.
		var show = pParameter switch
		{
			0 => Rm2kTransition.FadeIn,
			1 => Rm2kTransition.RandomBlocks,
			2 => Rm2kTransition.RandomBlocksDown,
			3 => Rm2kTransition.RandomBlocksUp,
			4 => Rm2kTransition.BlindOpen,
			5 => Rm2kTransition.VerticalStripesIn,
			6 => Rm2kTransition.HorizontalStripesIn,
			7 => Rm2kTransition.BorderToCenterIn,
			8 => Rm2kTransition.CenterToBorderIn,
			9 => Rm2kTransition.ScrollUpIn,
			10 => Rm2kTransition.ScrollDownIn,
			11 => Rm2kTransition.ScrollLeftIn,
			12 => Rm2kTransition.ScrollRightIn,
			13 => Rm2kTransition.VerticalCombine,
			14 => Rm2kTransition.HorizontalCombine,
			15 => Rm2kTransition.CrossCombine,
			16 => Rm2kTransition.ZoomOut,
			17 => Rm2kTransition.MosaicIn,
			18 => Rm2kTransition.WaveIn,
			19 => Rm2kTransition.CutIn,
			_ => Rm2kTransition.None,
		};
		return new Rm2kTransitionRequestResult(
			show, Rm2kTransitionRequestResultKind.Named);
	}
}

/// <summary>
/// Why a transition parameter did or did not name a kind.
/// </summary>
public enum Rm2kTransitionRequestResultKind
{
	/// <summary>The parameter named a kind.</summary>
	Named,

	/// <summary>
	/// The parameter was −1, which means the game's own teleport transition
	/// from the editor's settings.
	/// </summary>
	GameWideTransition,

	/// <summary>
	/// The parameter was outside both tables, and the reference falls through
	/// to none without saying so.
	/// </summary>
	UnknownParameter,
}

/// <summary>
/// What one transition parameter produced.
/// </summary>
public readonly struct Rm2kTransitionRequestResult
{
	public Rm2kTransitionRequestResult(
		Rm2kTransition pTransition, Rm2kTransitionRequestResultKind pKind)
	{
		Transition = pTransition;
		Kind = pKind;
	}

	/// <summary>
	/// The kind, or <see cref="Rm2kTransition.None"/> when the parameter did
	/// not name one.
	/// </summary>
	public Rm2kTransition Transition { get; }

	/// <summary>Why.</summary>
	public Rm2kTransitionRequestResultKind Kind { get; }
}
