using System;
using System.Collections.Generic;

namespace UniversalRPG.Wolf;

/// <summary>
/// WOLF's assignment operators, from the editor's help page
/// <c>silversecond.com/WolfRPGEditor/Help/04ev_calc.html</c>.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Fourteen operators and not two.</strong> The help tabulates the
/// assignment operators as <c>=</c>, <c>+=</c>, <c>-=</c>, <c>*=</c>,
/// <c>/=</c>, <c>%=</c>, pull-up, pull-down, absolute value, arctan, sine,
/// cosine, and square root. A reader with only assignment and addition would
/// make a game compute a hit rate, a damage formula, and an angle — none of
/// which it can express at all.
/// </para>
/// <para>
/// <strong>Division by zero leaves the variable alone and is not an error.</strong>
/// The help says 0 for the divisor treats it as divide by one, so the value
/// does not change. A reader that returned zero, or threw, or set the variable
/// to a sentinel would be wrong in three different ways for one line of the
/// help.
/// </para>
/// <para>
/// <strong>Trigonometry is scaled and the scale is the point.</strong> The
/// angle is in tenths of a degree and the result is in thousandths, so 600
/// (sixty degrees) gives 866 for sine and 500 for cosine. A reader that worked
/// in degrees and floating point would return 0.866 and break every game that
/// uses it — and it would not look wrong, it would look like a small number.
/// </para>
/// </remarks>
public static class WolfVariableOperator
{
	/// <summary>Assign the right hand side as it is.</summary>
	public const int Assign = 0;

	/// <summary>Add the right hand side.</summary>
	public const int Add = 1;

	/// <summary>Subtract the right hand side.</summary>
	public const int Subtract = 2;

	/// <summary>Multiply by the right hand side.</summary>
	public const int Multiply = 3;

	/// <summary>Divide, and leave the value alone when the divisor is zero.</summary>
	public const int Divide = 4;

	/// <summary>The remainder of the division.</summary>
	public const int Modulo = 5;

	/// <summary>Pull up: take the right hand side when it is larger.</summary>
	public const int PullUp = 6;

	/// <summary>Pull down: take the right hand side when it is smaller.</summary>
	public const int PullDown = 7;

	/// <summary>Absolute value, and leave a non-negative value alone.</summary>
	public const int Absolute = 8;

	/// <summary>Arc tangent of a slope, as tenths of a degree.</summary>
	public const int ArcTangent = 9;

	/// <summary>Sine of an angle in tenths of a degree, in thousandths.</summary>
	public const int Sine = 10;

	/// <summary>Cosine of an angle in tenths of a degree, in thousandths.</summary>
	public const int Cosine = 11;

	/// <summary>Square root, in thousandths.</summary>
	public const int SquareRoot = 12;

	/// <summary>The highest operator the editor offers.</summary>
	public const int MaxOperator = 12;

	/// <summary>The largest value a map event keeps.</summary>
	/// <remarks>
	/// **The help states the bound twice and for both event kinds**: a map
	/// event always stays within plus or minus two billion, and a common event
	/// only if it uses none of the variable operation checkboxes. A reader
	/// that left an int to wrap would turn an accumulating common event into a
	/// negative number, which is the overflow the help warns about.
	/// </remarks>
	public const int ValueCeiling = 2_000_000_000;

	/// <summary>
	/// Applies one operator to the current value and the right hand side.
	/// </summary>
	/// <param name="pOperator">Which of the fourteen operators.</param>
	/// <param name="pCurrent">What the variable holds now.</param>
	/// <param name="pRight">
	/// The right hand side; the second number for the two-number operators.
	/// </param>
	/// <returns>The value to store, already clamped to the ceiling.</returns>
	/// <remarks>
	/// **The whole computation is wide, and not just the multiply.** Subtraction
	/// leaves the int range as easily as multiplication does: a variable at
	/// minus one minus a right hand side of two billion is minus two billion
	/// and one, which no int holds. A reader that clamped an int would be
	/// clamping the number *after* the wrap, and the clamp would be applied to
	/// the wrong value.
	/// </remarks>
	public static int Apply(int pOperator, int pCurrent, long pRight)
	{
		// **The right hand side is a long and not an int,** and the help is the
		// reason: it allows plus or minus two billion, which is the whole int
		// range plus change. A right hand side typed as int could not express a
		// legal value at the negative end, and a caller that clamped it to fit
		// would have replaced the number the game meant with a different one.
		// **Every branch clamps, and the clamp is the documented bound and not
		// a safety net.** The help says a map event always keeps its result
		// within plus or minus two billion; a reader that skipped it would wrap
		// a multiplication into a negative number and the game would read a
		// negative where it expects a positive.
		return Clamp(Switch(pOperator, pCurrent, pRight));
	}

	/// <summary>
	/// Applies the ±999999 clamp the editor offers as a checkbox.
	/// </summary>
	/// <remarks>
	/// <strong>The clamp exists because a result at or above a million stops
	/// being a value</strong> and starts naming another variable. The help says
	/// exactly that: with the box ticked, a result at or above 1,000,000 becomes
	/// 999,999 and a result at or below minus 1,000,000 becomes minus 999,999.
	/// </remarks>
	public static int ClampToVariableRange(int pValue)
	{
		if (pValue >= WolfVariable.ReferenceBase)
		{
			return WolfVariable.ReferenceBase - 1;
		}
		if (pValue <= -WolfVariable.ReferenceBase)
		{
			return -(WolfVariable.ReferenceBase - 1);
		}
		return pValue;
	}

	/// <summary>Clamps to the two billion ceiling, wrapping included.</summary>
	private static int Clamp(long pValue)
	{
		// **The parameter is a long because by now it may be outside the int
		// range in either direction,** and the clamp is the last place that can
		// still see the real number. Clamping after the wrap would clamp the
		// wrong one.
		if (pValue > ValueCeiling)
		{
			return ValueCeiling;
		}
		if (pValue < -ValueCeiling)
		{
			return -ValueCeiling;
		}
		return (int)pValue;
	}

	/// <summary>
	/// The one switch every operator goes through, without the clamp.
	/// </summary>
	/// <remarks>
	/// **Every arm returns and not every arm assigns.** The division arm, the
	/// remainder arm, and the pull arms do not write the variable on every
	/// input, and a shape that computed into a local and stored once at the end
	/// would have to invent a "no change" value for them.
	/// </remarks>
	private static long Switch(int pOperator, int pCurrent, long pRight)
	{
		switch (pOperator)
		{
			case Assign:
				return pRight;
			case Add:
				return (long)pCurrent + pRight;
			case Subtract:
				return (long)pCurrent - pRight;
			case Multiply:
				return (long)pCurrent * pRight;
			case Divide:
				// **A divisor of zero leaves the value alone**, and the help is
				// explicit that it behaves as divide by one. Returning the
				// current value is that behaviour, and it is not an error path.
				return pRight == 0 ? pCurrent : pCurrent / pRight;
			case Modulo:
				// **A zero modulus also leaves the value alone**, for the same
				// reason. The help does not name this case, and throwing here
				// would stop an event that the real game runs to completion.
				return pRight == 0 ? pCurrent : pCurrent % pRight;
			case PullUp:
				// **Take the larger of the two, and keep the current value when
				// it is already the larger.** The help says the right hand side
				// is taken when it is larger than the left. The result is
				// widened here and not narrowed, because the two sides are
				// already ints and the comparison is what decides.
				return pRight > pCurrent ? pRight : pCurrent;
			case PullDown:
				// **The mirror, and the comparison is strict.** Equal values keep
				// the current one, which is the same number, so this costs
				// nothing and keeps the two arms symmetrical.
				return pRight < pCurrent ? pRight : pCurrent;
			case Absolute:
				// **A non-negative value is left alone and not negated twice.**
				// Abs of 5 is 5, and a reader that negated unconditionally would
				// make every positive variable negative.
				return pRight < 0 ? -pRight : pRight;
			case ArcTangent:
				// **Unreachable through the VM and refused here on purpose.**
				// The arc tangent needs two right hand sides and this switch has
				// one, so the caller's own dispatch hands that one case to
				// ArcTangentOf directly. Answering with the current value means
				// a caller that reaches this switch with the arc tangent gets
				// "no change" rather than an angle made from a destination
				// value that has no part in the slope.
				return pCurrent;
			case Sine:
				return Scaled(Math.Sin(TenthsToRadians(pRight)));
			case Cosine:
				return Scaled(Math.Cos(TenthsToRadians(pRight)));
			case SquareRoot:
				// **A negative root has no value**, and the help does not name
				// the case. Math.Sqrt returns NaN for it, and a NaN that reaches
				// the clamp would compare false against both bounds and be
				// stored as an arbitrary number. Zero is the answer that leaves
				// the game running.
				return pRight < 0 ? 0 : Scaled(Math.Sqrt(pRight));
			default:
				// **An operator this reader does not have changes nothing and
				// says so.** Falling back to assignment would silently rewrite a
				// variable with the right hand side, and a game written in a
				// newer editor would lose values instead of being refused.
				return pCurrent;
		}
	}

	/// <summary>
	/// Converts tenths of a degree to radians.
	/// </summary>
	/// <remarks>
	/// **Tenths, and not degrees.** The whole reason the editor stores 600 for
	/// sixty degrees is that it has no floating point in the variable model, so
	/// a reader that used degrees here would be off by a factor of ten and the
	/// sine of sixty degrees would come out as the sine of six hundred degrees.
	/// </remarks>
	private static double TenthsToRadians(long pTenthsOfDegree)
	{
		return pTenthsOfDegree * Math.PI / 1800.0;
	}

	/// <summary>
	/// Scales a result to thousandths and rounds.
	/// </summary>
	/// <remarks>
	/// <strong>Thousandths and rounding and not truncation.</strong> The help's
	/// own worked example is 5 becoming 2236, and truncating would give 2236
	/// as well, but the square root of 2 would give 1414 instead of 1414.2
	/// rounded, and a game that accumulated a thousand of those would drift by
	/// a visible amount. Rounding is what the editor does.
	/// </remarks>
	private static int Scaled(double pValue)
	{
		return (int)Math.Round(pValue * 1000.0, MidpointRounding.AwayFromZero);
	}

	/// <summary>
	/// The angle of a slope, in tenths of a degree.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>Two numbers and not one packed word.</strong> The help says the
	/// right hand side's <em>two</em> variables are the X and the Y vector, each
	/// stored in its own field. A reader that packed them into a single int
	/// would have to invent a bit layout the format does not have, and the first
	/// slope it read would be off by however many bits it guessed wrong.
	/// </para>
	/// <para>
	/// <strong>The sign is fixed by the editor's own convention</strong>: the help
	/// says the X vector is right positive and the Y vector is down positive,
	/// which is screen coordinates. Atan2 with that order already returns the
	/// screen-space angle, so a slope pointing straight down is +90 degrees and
	/// not -90, and a reader that mirrored the X axis to reach the
	/// mathematical frame would flip every angle in the game.
	/// </para>
	/// </remarks>
	/// <summary>
	/// The angle of a slope, in tenths of a degree.
	/// </summary>
	/// <param name="pX">The X vector, right positive.</param>
	/// <param name="pY">The Y vector, down positive.</param>
	public static int ArcTangentOf(int pX, int pY)
	{
		// **A slope of zero has no angle**, and the help does not name the case.
		// Returning zero is the direction the vector points when it has no
		// length, and it is the only answer that leaves the game running.
		if (pX == 0 && pY == 0)
		{
			return 0;
		}

		// **Atan2, and not a bare arc tangent.** The bare arc tangent reaches
		// only plus or minus ninety degrees, so a slope pointing left or up
		// would come out at the wrong angle. Atan2 already covers the whole
		// circle.
		//
		// **The order is Y, X, and that is the editor's convention and not a
		// slip.** The help says X is right positive and Y is down positive, so
		// this order already yields the screen-space angle: straight down is
		// +90, not -90. Swapping the arguments would mirror the game.
		var angle = Math.Atan2(pY, pX) * 1800.0 / Math.PI;

		// **Tenths of a degree**, which is the unit the editor stores, and
		// rounded so a slope of one is 450 and not 449.
		return (int)Math.Round(angle, MidpointRounding.AwayFromZero);
	}
}
