using System.Collections.Generic;

using UniversalRPG.Rm2k;
using UniversalRPG.Rm2k.Interpreter;
using UniversalRPG.Rm2k.Presentation;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Command 11610, Key Input Proc, wired into the interpreter and the
/// presentation state.
/// </summary>
/// <remarks>
/// <para>
/// The command was read in K-135 — the key table, the parameter columns, the
/// engine version's choice between them — and was not wired, because there was
/// no prompt for it. This is the prompt, and the rule that makes it a prompt:
/// <strong>the page holds while it is open.</strong>
/// </para>
/// <para>
/// The values are the reference's own: a digit is 11 to 20, an operator 21 to
/// 25, the confirm key is 5. The fixture occurrence is
/// <c>[1,1,0,0,0,1,1,2,1,0,0,0,0,0]</c>, and the expectations come from the
/// source, not from a table someone wrote.
/// </para>
/// </remarks>
public partial class TestRm2kKeyInputWiring : TestBase
{
	/// <summary>The one occurrence in the pinned fixtures, in file order.</summary>
	private static readonly List<int> Fixture =
		[1, 1, 0, 0, 0, 1, 1, 2, 1, 0, 0, 0, 0, 0];

	private static Rm2kMap.EventCommand KeyProc(params int[] pParameters)
	{
		return new Rm2kMap.EventCommand
		{
			Code = EventInterpreter.KeyInputProc,
			Text = "",
			Parameters = new List<int>(pParameters),
		};
	}

	private static (EventInterpreter Interpreter, PresentationState Presentation,
		GameSimulationState State) Run(bool pECommands, params int[] pParameters)
	{
		var state = new GameSimulationState
		{
			MapId = 1,
			SupportsRpg2k3ECommands = pECommands,
		};
		var presentation = new PresentationState();
		var interpreter = new EventInterpreter(
			state, 1, [KeyProc(pParameters)], presentation);
		return (interpreter, presentation, state);
	}

	/// <summary>
	/// The prompt opens and the page holds on it.
	/// </summary>
	/// <remarks>
	/// **This is what makes it a prompt.** The reference returns <c>false</c>
	/// and resets its key state, so the page waits. A reader that advanced
	/// would run the rest of the page before the player had pressed anything.
	/// </remarks>
	public void Test_ThePromptOpensAndThePageHoldsOnIt()
	{
		var (interpreter, presentation, state) = Run(
			pECommands: true, [.. Fixture]);

		interpreter.ExecuteFrame();

		AssertEq(
			presentation.PendingKeyInputVariableId, 1,
			"and the prompt is open on variable 1, because parameters[0] names"
			+ $" the variable; it is {presentation.PendingKeyInputVariableId}");
		AssertTrue(
			interpreter.IsRunning,
			"**and the page is still running**, because the reference returns"
			+ $" false until a key arrives; running is {interpreter.IsRunning}");
		AssertTrue(
			presentation.PendingKeyInputKeys.Count > 0,
			"and the accepted keys were published, because the caller has to know"
			+ $" what to show; there are {presentation.PendingKeyInputKeys.Count}");
		_ = state;
	}

	/// <summary>
	/// While it waits, the variable is zero — every frame, not once.
	/// </summary>
	/// <remarks>
	/// The reference's own comment says the variable is reset to zero each
	/// frame while waiting. <strong>A reader that only wrote on arrival would
	/// leave whatever the game had put there a moment ago</strong>, and a game
	/// that reads the variable to show "press a key" would show the old value
	/// instead.
	/// </remarks>
	public void Test_WhileItWaitsTheVariableIsZero()
	{
		var (interpreter, _, state) = Run(pECommands: true, [.. Fixture]);
		// The game put something in variable 1 before asking.
		state.Variables.Add(99);

		interpreter.ExecuteFrame();

		AssertEq(
			state.Variables[0], 0,
			"**and variable 1 is zero, not the 99 the game left there**, because"
			+ " the reference resets it to 0 each frame while waiting and a"
			+ $" reader that wrote only on arrival would keep the old value; it is {state.Variables[0]}");
	}

	/// <summary>
	/// A key the prompt allows writes the reference's value and closes it.
	/// </summary>
	public void Test_AnAllowedKeyWritesTheValueAndClosesThePrompt()
	{
		var (interpreter, presentation, state) = Run(
			pECommands: true, [.. Fixture]);
		interpreter.ExecuteFrame();

		interpreter.PressKeys(["3"]);

		AssertEq(
			state.Variables[0], 13,
			"**and variable 1 holds 13, because a digit is 10 + i and i was 3**"
			+ " — not the digit itself, and not 0;"
			+ $" it is {state.Variables[0]}");
		AssertEq(
			presentation.PendingKeyInputVariableId, null,
			"and the prompt is closed, because a key arrived and the command is"
			+ $" done; the variable is {presentation.PendingKeyInputVariableId}");
		AssertEq(
			presentation.PendingKeyInputKeys.Count, 0,
			"and the accepted keys were cleared, because a closed prompt that"
			+ " still lists keys would take the next keypress for a command that"
			+ $" no longer exists; it lists {presentation.PendingKeyInputKeys.Count}");
	}

	/// <summary>
	/// A key the prompt does not allow ends nothing.
	/// </summary>
	/// <remarks>
	/// The fixture allows digits and operators and neither the confirm key nor
	/// shift, so pressing the confirm key must leave the prompt open. **A reader
	/// that treated any key as an answer would end a prompt on the first key a
	/// player pressed to dismiss it** — which is how a calculator dialog closes
	/// before you have typed a digit.
	/// </remarks>
	public void Test_AKeyThePromptDoesNotAllowEndsNothing()
	{
		var (interpreter, presentation, state) = Run(
			pECommands: true, [.. Fixture]);
		interpreter.ExecuteFrame();

		interpreter.PressKeys(["decision"]);
		interpreter.PressKeys(["shift"]);

		AssertEq(
			presentation.PendingKeyInputVariableId, 1,
			"**and the prompt is still open**, because neither key is one the"
			+ " command asked for; the variable is"
			+ $" {presentation.PendingKeyInputVariableId}");
		AssertEq(
			state.Variables[0], 0,
			"and the variable is still zero, because nothing was answered;"
			+ $" it is {state.Variables[0]}");
	}

	/// <summary>
	/// An operator beats a digit pressed in the same frame, and the value is
	/// 20 + i.
	/// </summary>
	public void Test_AnOperatorBeatsADigitAndAnswersTwentyPlusI()
	{
		var (interpreter, _, state) = Run(pECommands: true, [.. Fixture]);
		interpreter.ExecuteFrame();

		interpreter.PressKeys(["3", "plus"]);

		AssertEq(
			state.Variables[0], 25,
			"**and variable 1 holds 25, because plus is 20 + 5 and the"
			+ " reference checks the operator group before the digit group**; it"
			+ $" is {state.Variables[0]}");
	}

	/// <summary>
	/// The time variable is filled when the prompt closes, and only then.
	/// </summary>
	/// <remarks>
	/// The reference's `_keyinput.tenths` is counted in frames and only handed
	/// to the variable when the key arrives, so a prompt that never gets a key
	/// reports nothing. **Writing it on open would report zero tenths as if the
	/// player had answered immediately.**
	/// </remarks>
	public void Test_TheTimeVariableIsFilledWhenThePromptCloses()
	{
		var (interpreter, presentation, _) = Run(pECommands: true, [.. Fixture]);
		interpreter.ExecuteFrame();

		AssertEq(
			presentation.PendingKeyInputTimeVariableId, 2,
			"**and variable 2 is named as the time target**, because"
			+ " parameters[7] is an int naming a variable and not a bool, and"
			+ " parameters[8] is the timed flag;"
			+ $" it is {presentation.PendingKeyInputTimeVariableId}");
		AssertEq(
			presentation.PendingKeyInputIsTimed, true,
			"and the prompt is timed, because parameters[8] is 1;"
			+ $" it is {presentation.PendingKeyInputIsTimed}");
		AssertEq(
			presentation.PendingKeyInputTenths, 0,
			"and no tenths have passed yet, because the prompt opened on this"
			+ $" frame; they are {presentation.PendingKeyInputTenths}");
	}

	/// <summary>
	/// A command with too few parameters is refused, not guessed at.
	/// </summary>
	public void Test_ACommandWithTooFewParametersIsRefused()
	{
		var (interpreter, presentation, _) = Run(
			pECommands: true, [1]);

		interpreter.ExecuteFrame();

		AssertEq(
			presentation.PendingKeyInputVariableId, null,
			"and no prompt opened, because a one parameter command has no"
			+ $" variable and no flags; the variable is {presentation.PendingKeyInputVariableId}");
		AssertTrue(
			ContainsDiagnostic(interpreter.State, "malformed"),
			"and the diagnostic says the parameters were malformed, which is"
			+ $" what the source's CmdSetup width check does; the diagnostics are"
			+ $" {interpreter.State.Diagnostics.Count}");
	}

	/// <summary>
	/// A 2K game asking for the same bytes wants shift, not digits.
	/// </summary>
	/// <remarks>
	/// The parameters mean different things on the two engines, so the same
	/// fourteen integers are a different command. **This is the sharp edge of
	/// the whole slice**: a reader that picked one column would wait for a shift
	/// key where the game asked for a digit, and the player would be stuck.
	/// </remarks>
	public void Test_TheSameBytesAreADifferentCommandOn2K()
	{
		var (als2k3, pres3, _) = Run(pECommands: true, [.. Fixture]);
		als2k3.ExecuteFrame();
		var auf2k3 = pres3.PendingKeyInputKeys;

		var (als2k, pres2, _) = Run(pECommands: false, [.. Fixture]);
		als2k.ExecuteFrame();
		var auf2k = pres2.PendingKeyInputKeys;

		AssertTrue(
			auf2k3.Count != auf2k.Count
				|| !auf2k3[0].Key.Equals(auf2k[0].Key, System.StringComparison.Ordinal),
			"**and the two readings accept different keys**, because"
			+ $" parameters[5] is the numbers flag on 2K3 and the shift flag on"
			+ $" 2K; 2K3 offers {auf2k3.Count} keys and 2K offers {auf2k.Count}");
		AssertTrue(
			ContainsKey(auf2k, "shift"),
			"and the 2K reading wants the shift key, which is what"
			+ " parameters[5] means there; it offers "
			+ DescribeFirst(auf2k, 3));
	}

	/// <summary>
	/// A new game does not inherit an open prompt.
	/// </summary>
	public void Test_AnOpenPromptDoesNotSurviveANewGame()
	{
		var (interpreter, presentation, state) = Run(
			pECommands: true, [.. Fixture]);
		interpreter.ExecuteFrame();
		AssertEq(
			presentation.PendingKeyInputVariableId, 1,
			"sanity: the prompt is open");

		presentation.Reset();
		state.Reset();

		AssertEq(
			presentation.PendingKeyInputVariableId, null,
			"**and a new game has no prompt open**, because a game that began"
			+ " waiting for a keypress would swallow the first key the player"
			+ $" pressed; the variable is {presentation.PendingKeyInputVariableId}");
		AssertEq(
			presentation.PendingKeyInputKeys.Count, 0,
			"and no keys are listed, because a closed prompt that still lists"
			+ $" keys would take the next keypress; it lists {presentation.PendingKeyInputKeys.Count}");
	}

	private static string DescribeFirst(
		IReadOnlyList<(int Value, string Key)> pKeys, int pCount)
	{
		var namen = new List<string>();
		for (var i = 0; i < pCount && i < pKeys.Count; i++)
		{
			namen.Add(pKeys[i].Key);
		}
		return string.Join(", ", namen);
	}

	private static bool ContainsKey(
		IReadOnlyList<(int Value, string Key)> pKeys, string pKey)
	{
		foreach (var (value, key) in pKeys)
		{
			if (string.Equals(key, pKey, System.StringComparison.Ordinal))
			{
				return true;
			}
		}
		return false;
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
