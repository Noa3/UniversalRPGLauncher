using System;

using UniversalRPG.Tests.Framework;
using UniversalRPG.Wolf;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// WOLF's fourteen assignment operators, from the editor's help page
/// <c>04ev_calc.html</c>.
/// </summary>
/// <remarks>
/// <para>
/// The help tabulates them as <c>=</c>, <c>+=</c>, <c>-=</c>, <c>*=</c>,
/// <c>/=</c>, <c>%=</c>, pull-up, pull-down, absolute value, arctan, sine,
/// cosine, and square root. The VM knew two, and the second one — addition —
/// was hard coded into its own opcode.
/// </para>
/// <para>
/// The help's own worked examples are the expected values here: 600 gives 866
/// for sine and 500 for cosine, 5 gives 2236 for the square root, and 5 and 3
/// give 1 for the bit product and 7 for the bit sum.
/// </para>
/// </remarks>
public partial class TestWolfVariableOperator : TestBase
{
	/// <summary>Normal variable 0, as a reference.</summary>
	private static int Normal0 => WolfVariable.BandOffset(WolfVariable.BandNormal);

	/// <summary>
	/// A right hand side past the int range, for the clamp.
	/// </summary>
	/// <remarks>
	/// **A method and not a cast.</strong> An <c>(int)3_000_000_000</c> literal
	/// is a compile error, not a truncation, and <c>unchecked</c> would be a
	/// different number entirely — so the value arrives as a long and the
	/// overflow that the clamp exists for is actually reachable.
	/// </remarks>
	private static long CheckedInt(long pValue) => pValue;

	// ---- Die Operatoren einzeln

	/// <summary>
	/// The first six operators, which the VM already had or did not have.
	/// </summary>
	/// <remarks>
	/// <strong>Division by zero leaves the value alone.</strong> The help says a
	/// zero divisor is treated as divide by one, so the value does not change.
	/// A reader that returned zero, or threw, or wrote a sentinel would be
	/// wrong in three different ways for one line of the help.
	/// </remarks>
	public void Test_TheFirstSixOperators()
	{
		AssertEq(
			WolfVariableOperator.Apply(WolfVariableOperator.Assign, 5, 9), 9,
			"**and assignment stores the right hand side**, whatever the left"
			+ $" held; it is {WolfVariableOperator.Apply(WolfVariableOperator.Assign, 5, 9)}");
		AssertEq(
			WolfVariableOperator.Apply(WolfVariableOperator.Add, 5, 9), 14,
			"and addition adds; it is"
			+ $" {WolfVariableOperator.Apply(WolfVariableOperator.Add, 5, 9)}");
		AssertEq(
			WolfVariableOperator.Apply(WolfVariableOperator.Subtract, 5, 9), -4,
			"and subtraction subtracts and may go negative; it is"
			+ $" {WolfVariableOperator.Apply(WolfVariableOperator.Subtract, 5, 9)}");
		AssertEq(
			WolfVariableOperator.Apply(WolfVariableOperator.Multiply, 5, 9), 45,
			"and multiplication multiplies; it is"
			+ $" {WolfVariableOperator.Apply(WolfVariableOperator.Multiply, 5, 9)}");
		AssertEq(
			WolfVariableOperator.Apply(WolfVariableOperator.Divide, 9, 5), 1,
			"**and division is integer division**, which is what a variable"
			+ " model with no floating point can be;"
			+ $" it is {WolfVariableOperator.Apply(WolfVariableOperator.Divide, 9, 5)}");
		AssertEq(
			WolfVariableOperator.Apply(WolfVariableOperator.Modulo, 17, 5), 2,
			"and the remainder is the remainder and not a fraction;"
			+ $" it is {WolfVariableOperator.Apply(WolfVariableOperator.Modulo, 17, 5)}");
	}

	/// <summary>
	/// A divisor of zero leaves the value alone and is not an error.
	/// </summary>
	/// <remarks>
	/// **The help is explicit: it behaves as divide by one.** Throwing here
	/// would stop an event that the real game runs to completion, and
	/// returning zero would turn "no change" into "wiped".
	/// </remarks>
	public void Test_AZeroDivisorLeavesTheValueAlone()
	{
		AssertEq(
			WolfVariableOperator.Apply(WolfVariableOperator.Divide, 42, 0), 42,
			"**and 42 divided by zero is still 42**, because the help says a zero"
			+ " divisor is treated as divide by one;"
			+ $" it is {WolfVariableOperator.Apply(WolfVariableOperator.Divide, 42, 0)}");
		AssertEq(
			WolfVariableOperator.Apply(WolfVariableOperator.Modulo, 42, 0), 42,
			"**and the remainder by zero is also unchanged**, for the same"
			+ " reason, and a reader that threw would crash an event the real"
			+ " game runs to the end;"
			+ $" it is {WolfVariableOperator.Apply(WolfVariableOperator.Modulo, 42, 0)}");
	}

	/// <summary>
	/// Pull up, pull down, and absolute value.
	/// </summary>
	/// <remarks>
	/// <strong>Pull up takes the larger, and a value that is already larger
	/// stays.</strong> That is the whole operation: a variable used as a
	/// maximum. A reader that always assigned the right hand side would lower
	/// a maximum that the game had already raised.
	/// </remarks>
	public void Test_PullUpPullDownAndAbsolute()
	{
		AssertEq(
			WolfVariableOperator.Apply(WolfVariableOperator.PullUp, 5, 9), 9,
			"**and pull up takes the larger**, so 5 and 9 give 9;"
			+ $" it is {WolfVariableOperator.Apply(WolfVariableOperator.PullUp, 5, 9)}");
		AssertEq(
			WolfVariableOperator.Apply(WolfVariableOperator.PullUp, 9, 5), 9,
			"**and when the current value is already the larger it stays**, which"
			+ " is the whole point of a maximum — a reader that assigned the"
			+ " right hand side would lower a maximum the game had raised;"
			+ $" it is {WolfVariableOperator.Apply(WolfVariableOperator.PullUp, 9, 5)}");
		AssertEq(
			WolfVariableOperator.Apply(WolfVariableOperator.PullDown, 5, 9), 5,
			"**and pull down takes the smaller**;"
			+ $" it is {WolfVariableOperator.Apply(WolfVariableOperator.PullDown, 5, 9)}");
		AssertEq(
			WolfVariableOperator.Apply(WolfVariableOperator.PullDown, 9, 5), 5,
			"and pull down lowers when the right hand side is smaller;"
			+ $" it is {WolfVariableOperator.Apply(WolfVariableOperator.PullDown, 9, 5)}");
		AssertEq(
			WolfVariableOperator.Apply(WolfVariableOperator.Absolute, 5, -9), 9,
			"**and absolute value negates a negative**;"
			+ $" it is {WolfVariableOperator.Apply(WolfVariableOperator.Absolute, 5, -9)}");
		AssertEq(
			WolfVariableOperator.Apply(WolfVariableOperator.Absolute, 5, 9), 9,
			"**and a positive value is left alone and not negated twice** — a"
			+ " reader that negated unconditionally would make every positive"
			+ " variable negative;"
			+ $" it is {WolfVariableOperator.Apply(WolfVariableOperator.Absolute, 5, 9)}");
	}

	// ---- Die trigonometrischen Operatoren

	/// <summary>
	/// Sine and cosine, in the help's own scale.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>The angle is in tenths of a degree and the result is in
	/// thousandths, and the scale is the whole operator.</strong> The help's
	/// worked example is that 600 — sixty degrees — gives 866 for sine and 500
	/// for cosine.
	/// </para>
	/// <para>
	/// A reader that worked in degrees and floating point would return 0.866
	/// for the same input, and it would not look wrong: it would look like a
	/// small number, and a game that scaled it back up would produce a
	 /// thousandfold error that shows up as an angle of nothing.
	/// </para>
	/// </remarks>
	public void Test_SineAndCosineInTheHelpersScale()
	{
		AssertEq(
			WolfVariableOperator.Apply(WolfVariableOperator.Sine, 0, 600), 866,
			"**and 600 — sixty degrees — gives 866**, which is the help's own"
			+ $" example, sine of sixty degrees times a thousand; it is"
			+ $" {WolfVariableOperator.Apply(WolfVariableOperator.Sine, 0, 600)}");
		AssertEq(
			WolfVariableOperator.Apply(WolfVariableOperator.Cosine, 0, 600), 500,
			"**and the same angle gives 500 for cosine**, because the cosine of"
			+ " sixty degrees is exactly one half and the help's example says"
			+ $" so; it is {WolfVariableOperator.Apply(WolfVariableOperator.Cosine, 0, 600)}");
		AssertEq(
			WolfVariableOperator.Apply(WolfVariableOperator.Sine, 0, 0), 0,
			"**and the sine of zero degrees is zero**;"
			+ $" it is {WolfVariableOperator.Apply(WolfVariableOperator.Sine, 0, 0)}");
		AssertEq(
			WolfVariableOperator.Apply(WolfVariableOperator.Sine, 0, 900), 1000,
			"**and ninety degrees gives exactly 1000**, because 900 is ninety"
			+ " degrees in the tenths the editor stores and the sine of ninety"
			+ " degrees is one — the value is the full scale and not an"
			+ $" approximation; it is {WolfVariableOperator.Apply(WolfVariableOperator.Sine, 0, 900)}");
		AssertEq(
			WolfVariableOperator.Apply(WolfVariableOperator.Sine, 0, 1800), 0,
			"**and a half turn gives 0**, because 1800 is a hundred and eighty"
			+ " degrees and that is back where it started. A reader that forgot"
			+ " the ten-times scale would treat 1800 as eighteen degrees and"
			+ $" give 309 here instead; it is {WolfVariableOperator.Apply(WolfVariableOperator.Sine, 0, 1800)}");
		AssertEq(
			WolfVariableOperator.Apply(WolfVariableOperator.Sine, 0, 3600), 0,
			"**and a full turn gives 0 as well**, which is the same wrap one"
			+ " step further and not a special case;"
			+ $" it is {WolfVariableOperator.Apply(WolfVariableOperator.Sine, 0, 3600)}");
		AssertEq(
			WolfVariableOperator.Apply(WolfVariableOperator.Sine, 0, 2700), -1000,
			"**and a quarter turn past that is -1000**, so a game can turn"
			+ " negative and not only up to the top of the scale — a reader that"
			+ " took the absolute value would make every downward slope"
			+ $" impossible; it is {WolfVariableOperator.Apply(WolfVariableOperator.Sine, 0, 2700)}");
	}

	/// <summary>
	/// The square root, in thousandths, from the help's example.
	/// </summary>
	/// <remarks>
	/// <strong>The help's own example: 5 becomes 2236.</strong> A reader that
	/// truncated instead of rounding would give 2236 for this one as well,
	/// which is why the rounding test below uses the root of seven.
	/// </remarks>
	public void Test_TheSquareRootInThousandths()
	{
		AssertEq(
			WolfVariableOperator.Apply(WolfVariableOperator.SquareRoot, 0, 5), 2236,
			"**and 5 becomes 2236**, which is the help's example verbatim;"
			+ $" it is {WolfVariableOperator.Apply(WolfVariableOperator.SquareRoot, 0, 5)}");
		AssertEq(
			WolfVariableOperator.Apply(WolfVariableOperator.SquareRoot, 0, 100), 10_000,
			"**and 100 becomes 10,000**, because the result is a thousandth and"
			+ " ten times a thousand is ten thousand;"
			+ $" it is {WolfVariableOperator.Apply(WolfVariableOperator.SquareRoot, 0, 100)}");
	}

	/// <summary>
	/// The scale rounds and does not truncate, and sqrt(7) is the case that
	/// proves it.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>The help's own example does not separate the two.</strong> Five
	/// becomes 2236 whether the scale rounds or truncates, and so do two and
	/// three — the root of two is 1414.21 and the root of three is 1732.05, and
	/// both truncate and round to the same integer. A test that only used the
	/// documented examples would pass against a truncating reader.
	/// </para>
	/// <para>
	/// <strong>Seven is the case: the root of seven times a thousand is
	/// 2645.75,</strong> so rounding gives 2646 and truncation gives 2645. That
	/// is the only difference, and a thousand accumulations of it would drift
	/// by a thousand.
	/// </para>
	/// <para>
	/// <strong>And no input has an exact half</strong>, because the root of a
	/// whole number is never a multiple of a thousand and a half, so
	/// half-away-from-zero and half-to-even cannot be told apart here. The code
	/// says away from zero because that is the one the editor's own rounding
	/// does, and the honest position is that this input set does not
	/// distinguish the two.
	/// </para>
	/// </remarks>
	public void Test_TheScaleRoundsAndNotTruncates()
	{
		AssertEq(
			WolfVariableOperator.Apply(WolfVariableOperator.SquareRoot, 0, 7), 2646,
			"**and the root of seven is 2646, not 2645** — that is the case"
			+ " that separates rounding from truncation, because 2645.75 rounds"
			+ " up and truncates down, while the help's own example of five"
			+ " does not separate them at all;"
			+ $" it is {WolfVariableOperator.Apply(WolfVariableOperator.SquareRoot, 0, 7)}");
	}

	/// <summary>
	/// The arc tangent of a slope, in tenths of a degree, in screen coordinates.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>Two numbers, and the sign convention is the editor's.</strong>
	/// The help says the X vector is right positive and the Y vector is down
	/// positive. A slope of (1, 0) points right and is zero degrees, and a slope
	/// of (0, 1) points *down*, which is plus ninety in these coordinates and
	/// not minus ninety.
	/// </para>
	/// <para>
	/// <strong>A bare arc tangent reaches only plus or minus ninety degrees</strong>,
	/// so a slope pointing left would come out at the wrong angle. Atan2 covers
	/// the whole circle and a slope of (-1, 0) is 1800, a half turn.
	/// </para>
	/// </remarks>
	public void Test_TheArcTangentOfASlope()
	{
		AssertEq(
			WolfVariableOperator.ArcTangentOf(1, 0), 0,
			"**and a slope of (1, 0) is 0 degrees**, because it points right and"
			+ $" not up; it is {WolfVariableOperator.ArcTangentOf(1, 0)}");
		AssertEq(
			WolfVariableOperator.ArcTangentOf(0, 1), 900,
			"**and (0, 1) is +900, that is ninety degrees**, because the Y axis"
			+ " points down in the editor's convention and a reader that used the"
			+ $" mathematical frame would give -900; it is {WolfVariableOperator.ArcTangentOf(0, 1)}");
		AssertEq(
			WolfVariableOperator.ArcTangentOf(1, 1), 450,
			"**and (1, 1) is 450**, forty five degrees down and to the right;"
			+ $" it is {WolfVariableOperator.ArcTangentOf(1, 1)}");
		AssertEq(
			WolfVariableOperator.ArcTangentOf(-1, 0), 1800,
			"**and a slope pointing left is 1800**, a half turn — a reader that"
			+ " used a bare arc tangent would clamp it to 900 and point the"
			+ $" slope to the right instead; it is {WolfVariableOperator.ArcTangentOf(-1, 0)}");
	}

	/// <summary>
	/// A slope of zero has no angle and says so.
	/// </summary>
	public void Test_ASlopeOfZeroHasNoAngle()
	{
		AssertEq(
			WolfVariableOperator.ArcTangentOf(0, 0), 0,
			"**and a slope of (0, 0) is 0**, because a vector with no length has"
			+ " no direction, and returning zero is the only answer that leaves"
			+ " the game running instead of dividing by nothing;"
			+ $" it is {WolfVariableOperator.ArcTangentOf(0, 0)}");
	}

	// ---- Die Grenzen

	/// <summary>
	/// A result that leaves the two billion range is pulled back, not wrapped.
	/// </summary>
	/// <remarks>
	/// <strong>The help states the bound and the reason: overflow turns an
	/// accumulating value negative.</strong> A map event always stays within
	/// plus or minus two billion. A reader that let an int wrap would give
	/// minus two billion where the game expects a positive number, and the
	/// symptom would appear far from the multiplication that caused it.
	/// </remarks>
	public void Test_ASingleResultIsClampedAndNotWrapped()
	{
		AssertEq(
			WolfVariableOperator.Apply(WolfVariableOperator.Multiply, 2_000_000_000, 2),
			WolfVariableOperator.ValueCeiling,
			"**and a multiplication past two billion is pulled back to it**, and"
			+ " not wrapped to a negative number, which is the overflow the"
			+ " help warns about;"
			+ $" it is {WolfVariableOperator.Apply(WolfVariableOperator.Multiply, 2_000_000_000, 2)}");
		AssertEq(
			WolfVariableOperator.Apply(WolfVariableOperator.Subtract, 0, CheckedInt(3_000_000_000)),
			-WolfVariableOperator.ValueCeiling,
			"**and the other direction is pulled back too**, because a game that"
			+ " subtracts its way down must not wrap either;"
			+ $" it is {WolfVariableOperator.Apply(WolfVariableOperator.Subtract, 0, CheckedInt(3_000_000_000))}");
	}

	/// <summary>
	/// The variable range clamp exists because a million is a reference.
	/// </summary>
	/// <remarks>
	/// <strong>The help gives both halves</strong>: with the box ticked, a result
	/// at or above 1,000,000 becomes 999,999 and a result at or below minus
	/// 1,000,000 becomes minus 999,999. The reason is in the same paragraph —
	/// a value over a million calls another variable.
	/// </remarks>
	public void Test_TheVariableRangeClampStopsAtTheMillion()
	{
		AssertEq(
			WolfVariableOperator.ClampToVariableRange(1_000_000), 999_999,
			"**and exactly a million becomes 999,999**, because the boundary is"
			+ " inclusive and a million is the first reference number;"
			+ $" it is {WolfVariableOperator.ClampToVariableRange(1_000_000)}");
		AssertEq(
			WolfVariableOperator.ClampToVariableRange(-1_000_000), -999_999,
			"**and minus a million becomes minus 999,999**;"
			+ $" it is {WolfVariableOperator.ClampToVariableRange(-1_000_000)}");
		AssertEq(
			WolfVariableOperator.ClampToVariableRange(999_999), 999_999,
			"**and 999,999 is left alone**, because it is already inside the"
			+ $" range; it is {WolfVariableOperator.ClampToVariableRange(999_999)}");
		AssertEq(
			WolfVariableOperator.ClampToVariableRange(5_000_000), 999_999,
			"and five million is pulled back as well;"
			+ $" it is {WolfVariableOperator.ClampToVariableRange(5_000_000)}");
	}

	/// <summary>
	/// An operator this reader does not have changes nothing.
	/// </summary>
	/// <remarks>
	/// <strong>Falling back to assignment would silently rewrite the
	/// variable</strong> with the right hand side, and a game written in a
	/// newer editor would lose values instead of being refused. A reader that
	/// kept the current value at least leaves the game running with a variable
	/// that did not move.
	/// </remarks>
	public void Test_AnUnknownOperatorChangesNothing()
	{
		AssertEq(
			WolfVariableOperator.Apply(99, 42, 7), 42,
			"**and an operator 99 leaves the value at 42**, because a reader that"
			+ $" fell back to assignment would store 7; it is {WolfVariableOperator.Apply(99, 42, 7)}");
	}

	// ---- Die VM

	/// <summary>
	/// The VM runs an operator from the program and not a hard coded one.
	/// </summary>
	/// <remarks>
	/// <strong>The addition opcode and the operator are one path</strong>, so a
	/// program that writes subtraction into a variable command gets
	/// subtraction, and a reader that switched on the opcode alone would add.
	/// </remarks>
	public void Test_TheVmRunsTheOperatorFromTheProgram()
	{
		var vm = new WolfEventVm();
		vm.VariableBands.Set(WolfVariable.BandNormal, 0, 100);
		vm.Start(new WolfEventProgram
		{
			Id = 1,
			Commands =
			[
				new WolfEventCommand
				{
					Opcode = WolfEventOpcode.SetVariable,
					Operand = Normal0,
					Value = 7,
					Operator = WolfVariableOperator.Subtract,
				},
				new WolfEventCommand { Opcode = WolfEventOpcode.End },
			],
		});
		vm.StepTick();

		AssertEq(
			vm.VariableBands.Get(WolfVariable.BandNormal, 0), 93,
			"**and 100 minus 7 is 93**, which is the operator from the program"
			+ " and not the assignment the opcode alone would give;"
			+ $" it is {vm.VariableBands.Get(WolfVariable.BandNormal, 0)}");
	}

	/// <summary>
	/// The addition opcode is the addition operator over the same path.
	/// </summary>
	public void Test_TheAdditionOpcodeIsTheAdditionOperator()
	{
		var vm = new WolfEventVm();
		vm.VariableBands.Set(WolfVariable.BandNormal, 0, 100);
		vm.Start(new WolfEventProgram
		{
			Id = 1,
			Commands =
			[
				new WolfEventCommand
				{
					Opcode = WolfEventOpcode.AddVariable,
					Operand = Normal0,
					Value = 7,
				},
				new WolfEventCommand { Opcode = WolfEventOpcode.End },
			],
		});
		vm.StepTick();

		AssertEq(
			vm.VariableBands.Get(WolfVariable.BandNormal, 0), 107,
			"**and 100 plus 7 is 107**, the way it was before the operators"
			+ $" existed; it is {vm.VariableBands.Get(WolfVariable.BandNormal, 0)}");
	}

	/// <summary>
	/// The arc tangent reads two right hand sides and not the current value.
	/// </summary>
	/// <remarks>
	/// <strong>This is the wiring rule that is easy to get wrong</strong>,
	/// because every other operator takes one right hand side. The help says
	/// the right hand side's two variables are the X and the Y vector, and the
	/// destination has no part in the slope — a reader that sent the current
	/// value as X would make the angle depend on what the destination already
	/// held, which no game means.
	/// </remarks>
	public void Test_TheArcTangentReadsTwoRightHandSides()
	{
		var vm = new WolfEventVm();
		// **A destination that holds a number which would wreck the angle if
		// it were used as the X vector**: 3000 is 3000 tenths of a degree, and
		// a slope of (3000, 1) is not forty five degrees.
		vm.VariableBands.Set(WolfVariable.BandNormal, 0, 3000);
		vm.Start(new WolfEventProgram
		{
			Id = 1,
			Commands =
			[
				new WolfEventCommand
				{
					Opcode = WolfEventOpcode.SetVariable,
					Operand = Normal0,
					Value = 1,
					Right2 = 1,
					HasRight2 = true,
					Operator = WolfVariableOperator.ArcTangent,
				},
				new WolfEventCommand { Opcode = WolfEventOpcode.End },
			],
		});
		vm.StepTick();

		AssertEq(
			vm.VariableBands.Get(WolfVariable.BandNormal, 0), 450,
			"**and a slope of (1, 1) is 450**, from the two right hand sides and"
			+ " not from the 3000 the destination happened to hold;"
			+ $" it is {vm.VariableBands.Get(WolfVariable.BandNormal, 0)}");
	}

	/// <summary>
	/// A right hand side that names a variable is read through the bands.
	/// </summary>
	/// <remarks>
	/// **The help says the right hand side may be a variable**, and 2,000,000
	/// there is normal variable 0. A reader that used the raw number would
	/// double by two million.
	/// </remarks>
	public void Test_ARightHandSideMayNameAVariable()
	{
		var vm = new WolfEventVm();
		vm.VariableBands.Set(WolfVariable.BandNormal, 0, 100);
		vm.VariableBands.Set(WolfVariable.BandNormal, 1, 7);
		vm.Start(new WolfEventProgram
		{
			Id = 1,
			Commands =
			[
				new WolfEventCommand
				{
					Opcode = WolfEventOpcode.AddVariable,
					Operand = Normal0,
					Value = WolfVariable.BandOffset(WolfVariable.BandNormal) + 1,
				},
				new WolfEventCommand { Opcode = WolfEventOpcode.End },
			],
		});
		vm.StepTick();

		AssertEq(
			vm.VariableBands.Get(WolfVariable.BandNormal, 0), 107,
			"**and the right hand side 2,000,001 adds 7**, because it names"
			+ " normal variable 1 and not the number two million and one;"
			+ $" it is {vm.VariableBands.Get(WolfVariable.BandNormal, 0)}");
	}
}
