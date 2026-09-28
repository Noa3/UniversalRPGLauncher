using System;

using UniversalRPG.Tests.Framework;
using UniversalRPG.Wolf;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// WOLF's seven variable comparisons, and the branch that uses them.
/// </summary>
/// <remarks>
/// <para>
/// <strong>This slice found an untested equality test.</strong> The WOLF event
/// VM compared a variable with <c>==</c> and nothing else, and
/// <em>no test in the repository exercised <c>IfVariable</c> at all</c> — so the
/// one comparison it had was as unproven as the six it was missing.
/// </para>
/// <para>
/// The seven come from the editor's own help page: greater, greater or equal,
/// equal, less or equal, less, not equal, bit and.
/// </para>
/// </remarks>
public partial class TestRm2kWolfComparisons : TestBase
{
	/// <summary>
	/// A branch program: branch on a variable, then run one arm or the other.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>The branch jumps to index 3 and the fall-through runs index 1.</strong>
	/// That layout is the only one where both arms are reachable: a jump target
	/// of 1 would make the fall-through unreachable, and a jump target of 2
	/// would make it unreachable in the other direction.
	/// </para>
	/// <para>
	/// Each arm writes a different number into variable 9, so which one ran is
	/// readable from the state afterwards — <em>a test that only checked the
	/// trace would prove nothing, because the trace records both arms
	/// existing</em>.
	/// </para>
	/// </remarks>
					private static WolfEventProgram Program(int pComparison, int pLeft, int pRight)
	{
		// **Index 0 is the branch, 1 the true arm, 2 the false arm, 3 the end.**
		// Both arms are one command, and **both carry NextIndex = 3** — the
		// last command of an arm has to step over the other arm, and a command
		// with no jump runs straight into it.
		return new WolfEventProgram
		{
			Id = 1,
			Commands =
			[
				new WolfEventCommand
				{
					Opcode = WolfEventOpcode.IfVariable,
					// **A reference, and not a bare zero.** The operand is
					// resolved through the bands, and 0 is a value — it would
					// compare the number zero against the limit and every
					// comparison would run against nothing.
					Operand = WolfVariable.BandOffset(WolfVariable.BandNormal),
					// **The right side may be a value or a reference too**, and
					// the help says a number at or above a million is called.
					// A comparison value like 3 stays a 3.
					Value = pRight,
					Comparison = pComparison,
					TrueJumpIndex = 1,
					JumpIndex = 2,
				},
				new WolfEventCommand
				{
					Opcode = WolfEventOpcode.SetVariable,
					// **A reference, because SetVariable now writes through
					// one.** A bare 9 is a value and not a place, and the
					// command would refuse it — which is the correct refusal
					// and not what this test wants to measure.
					Operand = WolfVariable.BandOffset(WolfVariable.BandNormal) + 9,
					Value = 111,
					// **Over the false arm.** Without this the true arm writes
					// its value and then the false arm overwrites it, and both
					// ran — a chest that opens and a guard that attacks in the
					// same frame.
					NextIndex = 3,
				},
				new WolfEventCommand
				{
					Opcode = WolfEventOpcode.SetVariable,
					Operand = WolfVariable.BandOffset(WolfVariable.BandNormal) + 9,
					Value = 222,
					NextIndex = 3,
				},
				new WolfEventCommand
				{
					Opcode = WolfEventOpcode.End,
				},
			],
		};
	}

	/// <summary>
	/// Runs the branch and reads variable 9, which the two arms set apart.
	/// </summary>
	/// <returns>111 when the branch was taken, 222 when it was not.</returns>
		private static int RunBranch(WolfEventVm pVm, int pComparison, int pLeft, int pRight)
	{
		// **Reset first and set the variable after it.** ResetState clears the
		// variable dictionary, so a test that set the variable first would
		// branch on zero — and a reader that wrote the same order would pass
		// every comparison test while testing nothing.
		pVm.ResetState();
		// **The normal band, named.** The accessors address normal variable 0,
		// and the branch resolves its operand through the bands — so this is
		// 2,000,000, not 0. A test that wrote a bare 0 would put a value into
		// a variable the branch does not read, and every comparison would
		// silently run against zero.
		pVm.VariableBands.Set(
			WolfVariable.BandNormal, 0, pLeft);
		pVm.Start(Program(pComparison, pLeft, pRight));
		for (var tick = 0; tick < 10 && pVm.State == WolfVmState.Running; tick++)
		{
			pVm.StepTick();
		}
		// **The marker is read from the normal band too**, because
		// GetVariable addresses normal and a raw 9 would read nothing.
		return pVm.VariableBands.Get(WolfVariable.BandNormal, 9);
	}


	// ---- Die sieben Vergleiche

	/// <summary>
	/// Greater takes its branch when the variable is above the value.
	/// </summary>
	public void Test_GreaterIsGreaterAndNotGreaterOrEqual()
	{
		AssertEq(
			RunBranch(new WolfEventVm(), (int)WolfComparison.Greater, 5, 3), 111,
			"**and 5 above 3 takes the greater branch**, so the true arm ran and"
			+ $" wrote 111; variable 9 is {111}");
		AssertEq(
			RunBranch(new WolfEventVm(), (int)WolfComparison.Greater, 3, 3), 222,
			"**and 3 against 3 does not**, because greater is strict and this is"
			+ " the case a reader that used >= would take; variable 9 is 222");
		AssertEq(
			RunBranch(new WolfEventVm(), (int)WolfComparison.Greater, 2, 3), 222,
			"and 2 against 3 does not either; variable 9 is 222");
	}

	/// <summary>
	/// Greater-or-equal differs from greater on exactly one value.
	/// </summary>
	/// <remarks>
	/// **The two branches that differ only on equality are the pair a reader is
	/// most likely to collapse**, and a game that writes "V0 is at least 1" to
	/// open a chest would never open it.
	/// </remarks>
	public void Test_GreaterOrEqualTakesTheEqualCase()
	{
		AssertEq(
			RunBranch(new WolfEventVm(), (int)WolfComparison.GreaterOrEqual, 3, 3), 111,
			"**and 3 against 3 takes the branch**, because the comparison is not"
			+ " strict; variable 9 is 111");
		AssertEq(
			RunBranch(new WolfEventVm(), (int)WolfComparison.GreaterOrEqual, 2, 3), 222,
			"**and 2 against 3 still does not**, so the two arms really are"
			+ " different and not the same code; variable 9 is 222");
	}

	/// <summary>
	/// Less and less-or-equal are the mirror of the two above.
	/// </summary>
	public void Test_LessAndLessOrEqualAreMirrors()
	{
		AssertEq(
			RunBranch(new WolfEventVm(), (int)WolfComparison.Less, 2, 3), 111,
			"**and 2 below 3 takes the less branch**, so the branch ran;"
			+ " variable 9 is 111");
		AssertEq(
			RunBranch(new WolfEventVm(), (int)WolfComparison.Less, 3, 3), 222,
			"**and 3 against 3 does not**, because less is strict; variable 9 is 222");
		AssertEq(
			RunBranch(new WolfEventVm(), (int)WolfComparison.LessOrEqual, 3, 3), 111,
			"**and less-or-equal does take it**, so the pair is not one"
			+ $" comparison; variable 9 is 111");
		AssertEq(
			RunBranch(new WolfEventVm(), (int)WolfComparison.LessOrEqual, 4, 3), 222,
			"**and 4 against 3 still does not**; variable 9 is 222");
	}

	/// <summary>
	/// Not equal is the one that takes every value but one.
	/// </summary>
	public void Test_NotEqualTakesEverythingButTheValue()
	{
		AssertEq(
			RunBranch(new WolfEventVm(), (int)WolfComparison.NotEqual, 2, 3), 111,
			"**and 2 against 3 takes it**, because anything but 3 qualifies;"
			+ " variable 9 is 111");
		AssertEq(
			RunBranch(new WolfEventVm(), (int)WolfComparison.NotEqual, 3, 3), 222,
			"**and 3 against 3 does not**, which is the single case it excludes;"
			+ $" variable 9 is 222");
	}

	/// <summary>
	/// The bit-and test is equal to the value and not "any bit set".
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>This is the one that surprises people, and the editor's help
	/// spends a paragraph on it.</strong> With V0 = 5 (<c>101</c>) and value 1
	/// (<c>001</c>), the test <em>fails</em> — the variable cannot cover the
	/// second digit.
	/// </para>
	/// <para>
	/// A reader that wrote <c>(variable &amp; value) != 0</c> would pass every
	/// test with any bit set, and <em>a game that guards a door with a bit test
	/// would open it for everyone.</em>
	/// </para>
	/// </remarks>
		public void Test_TheBitAndTestIsEqualToTheValue()
	{
		AssertEq(
			RunBranch(new WolfEventVm(), (int)WolfComparison.BitAnd, 3, 1), 111,
			"**and 3 and 1 is 1, so it takes the branch**, which is the rule: the"
			+ $" and must equal the value; variable 9 is 111");
		AssertEq(
			RunBranch(new WolfEventVm(), (int)WolfComparison.BitAnd, 2, 1), 222,
			"**and 2 and 1 is 0, not 1, so it does not** — and a reader that"
			+ " tested for any bit set would have taken it and opened whatever"
			+ $" the branch guards; variable 9 is 222");
		AssertEq(
			RunBranch(new WolfEventVm(), (int)WolfComparison.BitAnd, 5, 2), 222,
			"**and 5 and 2 is 0, so that fails too** — this is the exact case the"
			+ " help works through: V0 is 5, which is 101, and the value is 2,"
			+ " which is 010, so the variable cannot cover that digit;"
			+ $" variable 9 is 222");
		AssertEq(
			RunBranch(new WolfEventVm(), (int)WolfComparison.BitAnd, 7, 4), 111,
			"**and 7 and 4 is 4, so it takes the branch** — the same value in a"
			+ " position the variable does have set, which is the difference"
			+ $" between the two cases; variable 9 is 111");
	}

	/// <summary>
	/// A comparison value of zero satisfies the bit test in every case.
	/// </summary>
	/// <remarks>
	/// Anything bit anded with zero is zero, so the test is always true — **and
	/// the help says so outright.** A game that has written a zero there has
	 /// written a condition that is always true, and a reader that treated it
	/// as "no bits set" would report the opposite.
	/// </remarks>
	public void Test_ABitAndAgainstZeroIsAlwaysTrue()
	{
		foreach (var left in new[] { 0, 1, 5, 999 })
		{
			AssertEq(
				RunBranch(new WolfEventVm(), (int)WolfComparison.BitAnd, left, 0), 111,
				$"**and {left} against 0 takes the branch**, because anything and"
				+ " zero is zero and zero equals zero; a reader that read it as"
				+ $" \"no bits set\" would report the opposite; variable 9 is 111");
		}
	}

	/// <summary>
	/// A comparison number outside the seven is refused and says so.
	/// </summary>
	/// <remarks>
	/// <strong>Refused and not treated as equal.</strong> A reader that fell
	/// back to <c>==</c> would run a branch the game did not write, and a game
	/// whose guard is broken that way loses whatever the branch protects.
	/// </remarks>
	public void Test_AComparisonOutsideTheSevenIsRefused()
	{
		var vm = new WolfEventVm();
		vm.SetVariable(0, 5);
		vm.Start(Program(7, 5, 5));

		var result = vm.StepTick();

		AssertEq(
			result.Success, false,
			$"**and the tick fails**, because seven is not one of the seven"
			+ $" comparisons; the error is \"{vm.LastError?.Message}\"");
		AssertEq(
			vm.State, WolfVmState.Faulted,
			$"**and the VM faults** rather than guessing; the state is {vm.State}");
		AssertTrue(
			vm.LastError?.Message.Contains("seven") ?? false,
			$"**and the message says what was expected**, because a number alone"
			+ $" cannot be acted on; the message is \"{vm.LastError?.Message}\"");
	}

	/// <summary>
	/// The seven numbers are the ones the editor writes.
	/// </summary>
	/// <remarks>
	/// <strong>This is a model of the editor's choice list and not a claim about
	/// a file on disk.</strong> The binary WOLF format is not public
	/// documentation and this repository has no native WOLF fixture, so the
	/// numbers are pinned against the editor's help and the gap is stated here
	/// rather than papered over.
	/// </remarks>
	public void Test_TheSevenNumbersAreTheOnesTheEditorWrites()
	{
		AssertEq(
			WolfComparisonEvaluator.MaxComparison, 6,
			"**and the highest is 6**, because the help lists seven and they"
			+ $" start at zero; it is {WolfComparisonEvaluator.MaxComparison}");
		AssertEq(
			(int)WolfComparison.Greater, 0, "and greater is 0");
		AssertEq(
			(int)WolfComparison.GreaterOrEqual, 1, "and greater-or-equal is 1");
		AssertEq(
			(int)WolfComparison.Equal, 2, "and equal is 2");
		AssertEq(
			(int)WolfComparison.LessOrEqual, 3, "and less-or-equal is 3");
		AssertEq(
			(int)WolfComparison.Less, 4, "and less is 4");
		AssertEq(
			(int)WolfComparison.NotEqual, 5, "and not-equal is 5");
		AssertEq(
			(int)WolfComparison.BitAnd, 6, "and bit-and is 6");
	}



}
