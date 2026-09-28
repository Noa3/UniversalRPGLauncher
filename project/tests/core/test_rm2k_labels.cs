using System.Collections.Generic;

using UniversalRPG.Rm2k;
using UniversalRPG.Rm2k.Interpreter;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Commands 12110 Label and 12120 Jump to Label.
/// </summary>
/// <remarks>
/// <para>
/// These are the two commands that change <em>where</em> a page goes rather
/// than what it does, and they were the sharpest entry on K-136's list because
/// a reader without them runs the jump as a no-op and lands at the end of the
/// page — which feels like a game that quietly skipped half its script.
/// </para>
/// <para>
/// The expectations come from <c>CommandJumpToLabel</c>, and two of them are
/// the whole reason the command is worth its own file: the search starts at
/// <strong>zero</strong>, so a backward jump is a loop; and the index lands
/// <strong>on</strong> the label, not after it.
/// </para>
/// </remarks>
public partial class TestRm2kLabels : TestBase
{
	private static Rm2kMap.EventCommand Label(int pId)
	{
		return new Rm2kMap.EventCommand
		{
			Code = EventInterpreter.Label,
			Text = "",
			Parameters = [pId],
		};
	}

	private static Rm2kMap.EventCommand Jump(int pId)
	{
		return new Rm2kMap.EventCommand
		{
			Code = EventInterpreter.JumpToLabel,
			Text = "",
			Parameters = [pId],
		};
	}

	/// <summary>The page's end, as a command.</summary>
	private static Rm2kMap.EventCommand End()
	{
		return new Rm2kMap.EventCommand
		{
			Code = EventInterpreter.End,
			Text = "",
			Parameters = new List<int>(),
		};
	}

	/// <summary>A command that records that it ran, by switching variable 1.</summary>
	private static Rm2kMap.EventCommand Mark(int pSlot)
	{
		// **Control Variables takes six parameters**, and a first draft wrote
		// five: [targetMode, startId, endId, operation, operandType, operand].
		// Target mode 0 is one variable, operation 0 is "set", operand type 0
		// is a constant. The missing end id shifted every field, so the command
		// was refused and nothing was ever marked.
		return new Rm2kMap.EventCommand
		{
			Code = EventInterpreter.ControlVars,
			Text = "",
			Parameters = [0, pSlot, pSlot, 0, 0, 1],
		};
	}

	private static (EventInterpreter Interpreter, GameSimulationState State) Run(
		params Rm2kMap.EventCommand[] pCommands)
	{
		var state = new GameSimulationState
		{
			MapId = 1,
		};
		// **The array is filled element by element.** A first draft built it as
		// `new Array<int>(new List<int>(20))`, which sets a capacity rather
		// than twenty zeros, so every `Variables[slot - 1]` threw an
		// out-of-range — and the error pointed at the test rather than at the
		// twenty-zero assumption underneath it.
		for (var i = 0; i < 20; i++)
		{
			state.Variables.Add(0);
		}
		return (new EventInterpreter(state, 1, pCommands), state);
	}

	private static int Get(GameSimulationState pState, int pSlot)
	{
		return pState.Variables[pSlot - 1];
	}

	/// <summary>
	/// A label does nothing at all, and that is the point.
	/// </summary>
	/// <remarks>
	/// The reference's case for <c>12110</c> is <c>return true</c> and nothing
	/// else — no method, no parameters read. <strong>A label is a name, not an
	/// instruction</strong>, and a reader that gave it an effect would invent a
	/// semantic the format does not have.
	/// </remarks>
	public void Test_ALabelDoesNothingAndIsNotAStepThatDoesSomething()
	{
		var (interpreter, state) = Run(Label(1), Mark(5), End());

		interpreter.ExecuteFrame();

		AssertEq(
			interpreter.CurrentCommandIndex, 1,
			"**and the page moved past it**, because the label is a no-op and"
			+ " the index advances; the index is"
			+ $" {interpreter.CurrentCommandIndex}");
		AssertEq(
			Get(state, 5), 0,
			"and the command after it has not run yet, because a frame is one"
			+ $" step; variable 5 is {Get(state, 5)}");

		interpreter.ExecuteFrame();
		AssertEq(
			Get(state, 5), 1,
			"**and it ran on the next step**, which is what the mark is for and"
			+ $" what a five parameter command could not do; variable 5 is {Get(state, 5)}");
	}

	/// <summary>
	/// A forward jump lands on the label, and the label then falls through.
	/// </summary>
	public void Test_AForwardJumpLandsOnTheLabelAndFallsThrough()
	{
		var (interpreter, state) = Run(
			Mark(1),
			Jump(7),
			Mark(2),
			Label(7),
			Mark(3),
			End());

		interpreter.ExecuteFrame(); // mark 1
		interpreter.ExecuteFrame(); // the jump

		AssertEq(
			interpreter.CurrentCommandIndex, 3,
			"**and the index is on the label itself, not after it**, because the"
			+ " reference assigns `index = idx` and the label is a no-op; the"
			+ $" index is {interpreter.CurrentCommandIndex}");
		AssertEq(
			Get(state, 1), 1,
			"and the command before the jump ran; variable 1 is"
			+ $" {Get(state, 1)}");
		AssertEq(
			Get(state, 2), 0,
			"**and the command the jump skipped did not run**, which is the"
			+ " whole point of a jump; variable 2 is"
			+ $" {Get(state, 2)}");

		// **The label is a no-op, and it is reached as its own step.** The
		// engine increments the index only when a command left it alone, and the
		// jump moved it — so the next step runs the label itself, and only the
		// step after that is the instruction behind it. Two drafts got this
		// wrong in opposite directions and both were silent: one left the page
		// on the jump forever, which looks like a hang, and one advanced past
		// the label and skipped the no-op the format puts there on purpose.
		interpreter.ExecuteFrame();
		AssertEq(
			interpreter.CurrentCommandIndex, 4,
			"and the label fell through on the next step, because it does"
			+ $" nothing; the index is {interpreter.CurrentCommandIndex}");
		interpreter.ExecuteFrame();
		AssertEq(
			Get(state, 3), 1,
			"**and the command after the label ran**, which is where the page"
			+ $" actually is; variable 3 is {Get(state, 3)}");
	}

	/// <summary>
	/// The search starts at zero, so a backward jump is a loop.
	/// </summary>
	/// <remarks>
	/// <strong>This is the reason the command exists in this form.</strong> The
	/// reference's loop is <c>for (int idx = 0; idx &lt; list.size(); idx++)</c>
	/// — from the beginning, not from here. A reader that searched forwards from
	/// the current index would turn every backward jump into a fall-through to
	/// the end of the page, and a game that loops with a jump would run its
	/// body once and stop.
	/// </remarks>
	public void Test_ABackwardJumpGoesBackwardsAndLoops()
	{
		// **The label stands before the body**, which is the only arrangement
		// that loops. A first draft put the label *after* the jump, so the jump
		// landed on the next command, the page fell through to the end, and the
		// test asserted a loop the page did not have — which is also how an
		// author writes it, so the fixture is the honest one.
		var (interpreter, state) = Run(
			Label(1),
			Mark(1),
			Mark(2),
			Jump(1),
			End());

		interpreter.ExecuteFrame(); // the label, a no-op
		interpreter.ExecuteFrame(); // mark 1
		interpreter.ExecuteFrame(); // mark 2
		interpreter.ExecuteFrame(); // the jump

		AssertEq(
			interpreter.CurrentCommandIndex, 0,
			"**and the index went back to the label at 0, not forward to the"
			+ " end**, because the search starts at zero and a backward jump is"
			+ $" a loop; the index is {interpreter.CurrentCommandIndex}");
		AssertEq(
			Get(state, 2), 1,
			"and the body ran once before the jump; variable 2 is"
			+ $" {Get(state, 2)}");

		// Second pass: the label is a no-op, then the body again.
		interpreter.ExecuteFrame(); // the label again
		interpreter.ExecuteFrame(); // mark 1 again
		interpreter.ExecuteFrame(); // mark 2 again
		AssertEq(
			Get(state, 1), 1,
			"**and the body ran a second time**, which is the loop a backward"
			+ $" jump writes; variable 1 is {Get(state, 1)}");
	}

	public void Test_AMissingLabelLeavesThePageWhereItWas()
	{
		var (interpreter, state) = Run(
			Jump(99),
			Mark(1),
			End());

		interpreter.ExecuteFrame();

		AssertEq(
			interpreter.CurrentCommandIndex, 1,
			"**and the page carried on to the next command**, because the"
			+ " reference's loop finds nothing, the assignment never runs, and"
			+ " the engine's own rule then increments the index because nothing"
			+ $" moved it; the index is {interpreter.CurrentCommandIndex}");
		AssertTrue(
			ContainsDiagnostic(state, "no such label"),
			"and it says the label was not there, because a silent fall-through"
			+ " looks exactly like a page that meant to; the diagnostics are"
			+ $" {state.Diagnostics.Count}");
		AssertTrue(
			interpreter.IsRunning,
			"**and the page is still running**, because a missing label is a"
			+ $" fall-through and not the end of the event; running is {interpreter.IsRunning}");
	}

	public void Test_TheFirstLabelWithThatIdWins()
	{
		var (interpreter, _) = Run(
			Jump(3),
			Label(3),
			Mark(1),
			Label(3),
			Mark(2),
			End());

		interpreter.ExecuteFrame();

		AssertEq(
			interpreter.CurrentCommandIndex, 1,
			"**and it landed on the first one**, because the reference breaks on"
			+ " the first match and does not check that the id is unique; the"
			+ $" index is {interpreter.CurrentCommandIndex}");
	}

	/// <summary>
	/// A label's own id is what a jump matches, and two labels can sit next to
	/// each other.
	/// </summary>
	public void Test_TwoLabelsSideBySideAreToldApart()
	{
		var (interpreter, state) = Run(
			Jump(2),
			Label(1),
			Mark(1),
			Label(2),
			Mark(2),
			End());

		interpreter.ExecuteFrame();
		AssertEq(
			interpreter.CurrentCommandIndex, 3,
			"**and the jump went to label 2 and not to label 1**, because the"
			+ $" id is compared and not the position; the index is {interpreter.CurrentCommandIndex}");
		interpreter.ExecuteFrame(); // the label
		interpreter.ExecuteFrame(); // mark 2
		AssertEq(
			Get(state, 2), 1,
			"and the command after label 2 ran, not the one after label 1;"
			+ $" variable 2 is {Get(state, 2)}");
		AssertEq(
			Get(state, 1), 0,
			"**and the command after label 1 did not**, because the jump named"
			+ $" 2; variable 1 is {Get(state, 1)}");
	}

	/// <summary>
	/// A label with no parameters matches nothing.
	/// </summary>
	/// <remarks>
	/// The reference checks <c>next_cmd.parameters.empty()</c> and skips. **A
	/// reader that compared against a defaulted zero would treat a truncated
	/// label as label 0</strong> and jump to it, which is a command that never
	/// said what it was.
	/// </remarks>
	public void Test_ALabelWithNoParametersMatchesNothing()
	{
		var (interpreter, state) = Run(
			Jump(0),
			new Rm2kMap.EventCommand
			{
				Code = EventInterpreter.Label,
				Text = "",
				Parameters = new List<int>(),
			},
			Mark(1),
			End());

		interpreter.ExecuteFrame();

		AssertEq(
			interpreter.CurrentCommandIndex, 1,
			"**and the jump fell through to the empty label instead of matching"
			+ " it**, because the reference checks `parameters.empty()` and"
			+ " skips, and a reader that defaulted it to zero would have jumped"
			+ $" to a command that never said what it was; the index is {interpreter.CurrentCommandIndex}");
		AssertEq(
			Get(state, 1), 0,
			"and nothing has been marked, because the page only moved one step"
			+ $" and the mark is after the label; variable 1 is {Get(state, 1)}");
	}

	public void Test_AJumpWithNoParametersIsRefused()
	{
		var (interpreter, state) = Run(
			new Rm2kMap.EventCommand
			{
				Code = EventInterpreter.JumpToLabel,
				Text = "",
				Parameters = new List<int>(),
			},
			Mark(1),
			End());

		interpreter.ExecuteFrame();

		AssertTrue(
			ContainsDiagnostic(state, "malformed"),
			"**and the diagnostic says the parameters were malformed**, which is"
			+ " what CmdSetup's width check does; the diagnostics are"
			+ $" {state.Diagnostics.Count}");
		AssertTrue(
			interpreter.IsRunning,
			"and the page is still running, because a malformed jump is a"
			+ $" diagnostic and not the end of the event; running is {interpreter.IsRunning}");
	}

	private static bool ContainsDiagnostic(
		GameSimulationState pState, string pNeedle)
	{
		foreach (var line in pState.Diagnostics)
		{
			if (line.Contains(pNeedle, System.StringComparison.Ordinal))
			{
				return true;
			}
		}
		return false;
	}
}
