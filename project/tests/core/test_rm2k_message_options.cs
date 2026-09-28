using System;
using System.Collections.Generic;

using UniversalRPG.Rm2k;
using UniversalRPG.Rm2k.Interpreter;
using UniversalRPG.Rm2k.Presentation;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Commands 10120 Message Options, 10130 Change Face Graphic and 10230 Timer
/// Operation.
/// </summary>
/// <remarks>
/// <para>
/// <c>10230</c> carries a fault that predates this card:
/// <c>SetTimer</c> <strong>started the timer</strong>, and the reference has a
/// separate start operation for that. A game that wrote <c>SetTimer</c> to arm
/// a countdown it would start later started it immediately — the exact
/// difference between a timer that counts and one that does not.
/// </para>
/// <para>
/// The other two need message options this reader did not have at all.
/// </para>
/// </remarks>
public partial class TestRm2kMessageOptions : TestBase
{
	private static Rm2kMap.EventCommand Options(
		int pTransparent, int pPosition, int pFixed, int pContinues)
	{
		return new Rm2kMap.EventCommand
		{
			Code = EventInterpreter.MessageOptions,
			Text = "",
			Parameters = [pTransparent, pPosition, pFixed, pContinues],
		};
	}

	private static Rm2kMap.EventCommand Face(
		string pName, int pIndex, int pOnRight, int pFlipped)
	{
		return new Rm2kMap.EventCommand
		{
			Code = EventInterpreter.ChangeFaceGraphic,
			Text = pName,
			Parameters = [pIndex, pOnRight, pFlipped],
		};
	}

	/// <summary>
	/// <c>[operation, valueMode, value, visible, inBattle, timerId]</c>.
	/// </summary>
	private static Rm2kMap.EventCommand Timer(
		int pOperation, int pMode = 0, int pValue = 0,
		int pVisible = 0, int pInBattle = 0, int pTimerId = 0)
	{
		return new Rm2kMap.EventCommand
		{
			Code = EventInterpreter.TimerOperation,
			Text = "",
			Parameters =
				[pOperation, pMode, pValue, pVisible, pInBattle, pTimerId],
		};
	}

	private static (EventInterpreter Interpreter, PresentationState Presentation,
		GameSimulationState State) Run(params Rm2kMap.EventCommand[] pCommands)
	{
		var state = new GameSimulationState { MapId = 1 };
		var presentation = new PresentationState();
		var interpreter = new EventInterpreter(state, 1, pCommands, presentation);
		return (interpreter, presentation, state);
	}

	// ---- 10120 Message Options

	/// <summary>
	/// The four flags are four flags.
	/// </summary>
	/// <remarks>
	/// **A reader that collapsed them would make a transparent bottom message
	/// and a top-positioned one the same request.** The four assertions are
	/// separate because a collapse of any two of them passes the other two.
	/// </remarks>
	public void Test_TheFourOptionsAreFourFlags()
	{
		var (interpreter, presentation, _) = Run(Options(1, 0, 1, 1));

		interpreter.ExecuteFrame();

		AssertEq(
			presentation.MessageTransparent, true,
			"**and it is transparent**, from a non-zero first parameter; it is"
			+ $" {presentation.MessageTransparent}");
		AssertEq(
			presentation.MessagePosition,
			PresentationState.MessagePositionTop,
			"**and it sits at the top**, from parameters[1] being 0; it is"
			+ $" {presentation.MessagePosition}");
		AssertEq(
			presentation.MessagePositionFixed, false,
			"**and it does not hold its position**, because a one in"
			+ $" parameters[2] is the moving case; it is {presentation.MessagePositionFixed}");
		AssertEq(
			presentation.MessageContinuesEvents, true,
			"**and the map's events keep running**, from a non-zero fourth"
			+ $" parameter; it is {presentation.MessageContinuesEvents}");
	}

	/// <summary>
	/// Parameters[2] is inverted: a zero means fixed.
	/// </summary>
	/// <remarks>
	/// <c>SetMessagePositionFixed(com.parameters[2] == 0)</c> — **a reader that
	/// mapped a non-zero to fixed would scroll every window a game had pinned**,
	/// and the difference is visible only while the map moves, which is why no
	/// test of a still map could have caught it.
	/// </remarks>
	public void Test_AZeroParameterTwoMeansFixed()
	{
		var (interpreter, presentation, _) = Run(Options(0, 2, 0, 0));

		interpreter.ExecuteFrame();

		AssertEq(
			presentation.MessagePositionFixed, true,
			"**and a zero holds the window still**, because the reference writes"
			+ " SetMessagePositionFixed(parameters[2] == 0); it is"
			+ $" {presentation.MessagePositionFixed}");
		AssertEq(
			presentation.MessagePosition,
			PresentationState.MessagePositionBottom,
			"**and position 2 is the bottom, one of three** — top 0, middle 1,"
			+ $" bottom 2, and a boolean would have put the middle at the top; it is {presentation.MessagePosition}");
	}

	/// <summary>
	/// The middle position exists and is reachable.
	/// </summary>
	public void Test_TheMiddlePositionIsReachable()
	{
		var (interpreter, presentation, _) = Run(
			Options(0, PresentationState.MessagePositionMiddle, 0, 0));

		interpreter.ExecuteFrame();

		AssertEq(
			presentation.MessagePosition,
			PresentationState.MessagePositionMiddle,
			"**and 1 is the middle**, which a reader that stored a boolean would"
			+ $" have turned into the top or the bottom; it is {presentation.MessagePosition}");
	}

	/// <summary>
	/// A position outside the three is refused and nothing changes.
	/// </summary>
	public void Test_APositionOutsideTheThreeIsRefused()
	{
		var (interpreter, presentation, state) = Run(Options(0, 7, 0, 0));
		var vorher = presentation.MessagePosition;

		interpreter.ExecuteFrame();

		AssertEq(
			presentation.MessagePosition, vorher,
			"**and the position is unchanged**, because the reference has no"
			+ " validation at all and a reader that stored a seven would put a"
			+ $" window nowhere; it is {presentation.MessagePosition}");
		AssertTrue(
			ContainsDiagnostic(state, "not 0, 1 or 2"),
			"and the diagnostic says which values are allowed, because"
			+ " \"refused\" without them cannot be acted on;"
			+ $" the diagnostics are {state.Diagnostics.Count}");
	}

	// ---- 10130 Change Face Graphic

	/// <summary>
	/// The face name and slot reach the state, and this is a request.
	/// </summary>
	public void Test_AFaceCommandReachesTheState()
	{
		var (interpreter, presentation, _) = Run(Face("Actor", 2, 1, 1));

		interpreter.ExecuteFrame();

		AssertEq(
			presentation.FaceName, "Actor",
			"**and the name is the one the command's string field carried**,"
			+ $" because the name is not among the parameters; it is \"{presentation.FaceName}\"");
		AssertEq(
			presentation.FaceIndex, 2,
			$"and the slot is 2, from parameters[0]; it is {presentation.FaceIndex}");
		AssertEq(
			presentation.FaceOnRight, true,
			"**and it sits on the right**, from a non-zero parameters[1]; it is"
			+ $" {presentation.FaceOnRight}");
		AssertEq(
			presentation.FaceFlipped, true,
			"and it is mirrored, from a non-zero parameters[2]; it is"
			+ $" {presentation.FaceFlipped}");
	}

	/// <summary>
	/// A slot outside the file's four is refused.
	/// </summary>
	public void Test_ASlotOutsideTheFourIsRefused()
	{
		var (interpreter, presentation, state) = Run(Face("Actor", 9, 0, 0));

		interpreter.ExecuteFrame();

		AssertEq(
			presentation.FaceName, "",
			"**and no face was set**, because the file has four slots and a"
			+ $" reader that stored a ninth would name a face that is not there; the name is \"{presentation.FaceName}\"");
		AssertTrue(
			ContainsDiagnostic(state, "four slots"),
			"and the diagnostic says why, because a refusal without a reason is"
			+ $" the same as silence; the diagnostics are {state.Diagnostics.Count}");
	}

	/// <summary>
	/// A face with no name is refused.
	/// </summary>
	public void Test_AFaceWithNoNameIsRefused()
	{
		var (interpreter, presentation, state) = Run(Face("", 0, 0, 0));

		interpreter.ExecuteFrame();

		AssertEq(
			presentation.FaceIndex, 0,
			"**and nothing was set**, because the name is in the string field"
			+ $" and this command has none; the slot is {presentation.FaceIndex}");
		AssertTrue(
			ContainsDiagnostic(state, "no file name"),
			"and the diagnostic says the name was missing;"
			+ $" the diagnostics are {state.Diagnostics.Count}");
	}

	// ---- 10230 Timer Operation

	/// <summary>
	/// Setting a timer does not start it, and that is the fault this card
	/// found.
	/// </summary>
	/// <remarks>
	/// <c>SetTimer</c> used to set <c>Timer1Active = true</c>, and the reference
	/// has a separate start operation that takes the visible and battle flags.
	/// <strong>A game that armed a countdown it would start later started it
	/// immediately</strong> — the exact difference between a timer that counts
	/// and one that does not.
	/// </remarks>
	public void Test_SettingATimerDoesNotStartIt()
	{
		var (interpreter, _, state) = Run(Timer(0, pValue: 30));

		interpreter.ExecuteFrame();

		AssertEq(
			state.Timer1Seconds, 30,
			"**and timer 1 holds 30 seconds**, from the value; it is"
			+ $" {state.Timer1Seconds}");
		AssertEq(
			state.Timer1Active, false,
			"**and it is NOT running**, because the reference has a separate"
			+ " start operation and a reader that started on set would start a"
			+ $" countdown the game meant to arm; active is {state.Timer1Active}");
	}

	/// <summary>
	/// Starting is the second operation, and it takes the two flags.
	/// </summary>
	/// <remarks>
	/// <strong>The two flags are separate</strong> because a game can want a
	/// timer it hides from the player and one that stops for a battle, and
	/// collapsing them would make "hidden" and "not in battle" the same choice.
	/// </remarks>
	public void Test_StartingATimerTakesTheTwoFlags()
	{
		var (interpreter, _, state) = Run(
			Timer(0, pValue: 45), Timer(1, pVisible: 0, pInBattle: 1));

		interpreter.ExecuteFrame();
		interpreter.ExecuteFrame();

		AssertEq(
			state.Timer1Active, true,
			"**and it is running**, because operation 1 is the start; active is"
			+ $" {state.Timer1Active}");
		AssertEq(
			state.Timer1Visible, false,
			"**and it is not drawn**, because parameters[3] was zero; it is"
			+ $" {state.Timer1Visible}");
		AssertEq(
			state.Timer1InBattle, true,
			"**and it keeps running during a battle**, because parameters[4] was"
			+ $" one and that is a separate flag from visible; it is {state.Timer1InBattle}");
	}

	/// <summary>
	/// Stopping keeps the seconds.
	/// </summary>
	/// <remarks>
	/// **A game that stops a timer to show the count and then starts it again
	/// expects the count to still be there** — and a reader that reset on stop
	/// would hand back a timer that reads zero and looks like the game losing
	/// track of it.
	/// </remarks>
	public void Test_StoppingKeepsTheSeconds()
	{
		var (interpreter, _, state) = Run(
			Timer(0, pValue: 60), Timer(1, pVisible: 1), Timer(2));

		interpreter.ExecuteFrame();
		interpreter.ExecuteFrame();
		interpreter.ExecuteFrame();

		AssertEq(
			state.Timer1Active, false,
			"**and it is stopped**, because operation 2 is the stop; active is"
			+ $" {state.Timer1Active}");
		AssertEq(
			state.Timer1Seconds, 60,
			"**and it still reads 60**, because the reference's StopTimer does"
			+ " not reset the count and a reader that did would lose it;"
			+ $" the seconds are {state.Timer1Seconds}");
	}

	/// <summary>
	/// The seconds can come from a variable.
	/// </summary>
	public void Test_TheSecondsCanComeFromAVariable()
	{
		var state = new GameSimulationState { MapId = 1 };
		state.Variables.Add(0);
		state.Variables.Add(99);
		var presentation = new PresentationState();
		// Mode 1 is a variable, and the value names the variable.
		var interpreter = new EventInterpreter(
			state, 1, [Timer(0, pMode: 1, pValue: 2)], presentation);

		interpreter.ExecuteFrame();

		AssertEq(
			state.Timer1Seconds, 99,
			"**and the timer holds what variable 2 held**, because the value is"
			+ " read through ValueOrVariable and a reader that read it as a"
			+ $" constant would have armed two seconds; it is {state.Timer1Seconds}");
	}

	/// <summary>
	/// The sixth parameter names the timer, and only on an E-command game.
	/// </summary>
	/// <remarks>
	/// The reference reads it <strong>only when the command carries more than
	/// five parameters and the game is RPG2K3</strong>. Without it every timer
	/// command means timer one — which is why a 2K game has one timer and a
	/// 2003 game has two.
	/// </remarks>
	public void Test_TheSixthParameterNamesTheTimerOnlyOn2k3()
	{
		var (ohnePatch, _, state1) = Run(Timer(0, pValue: 10, pTimerId: 2));
		ohnePatch.ExecuteFrame();
		AssertEq(
			state1.Timer1Seconds, 10,
			"**and a game that is not an E-command game uses timer one**, because"
			+ $" the reference gates the sixth parameter on that; timer 1 holds {state1.Timer1Seconds}");
		AssertEq(
			state1.Timer2Seconds, 0,
			"**and timer two is untouched**, which is the whole difference"
			+ $" between a 2K game and a 2003 one; it holds {state1.Timer2Seconds}");

		var state2 = new GameSimulationState { MapId = 1 };
		state2.SupportsRpg2k3ECommands = true;
		var mitPatch = new EventInterpreter(
			state2, 1, [Timer(0, pValue: 10, pTimerId: 2)], new PresentationState());
		mitPatch.ExecuteFrame();
		AssertEq(
			state2.Timer2Seconds, 10,
			"**and an E-command game reads the sixth parameter**, so the same"
			+ $" command set timer two; it holds {state2.Timer2Seconds}");
	}

	/// <summary>
	/// An operation that is not set, start or stop is refused.
	/// </summary>
	/// <remarks>
	/// The reference's <c>default: return false</c> holds the page. This reader
	/// says so and advances, because a page that hangs on a bad operation id is
	/// a page that never finishes and looks like a hang.
	/// </remarks>
	public void Test_AnUnknownOperationIsRefused()
	{
		var (interpreter, _, state) = Run(Timer(9, pValue: 10));

		interpreter.ExecuteFrame();

		AssertEq(
			state.Timer1Seconds, 0,
			"**and nothing was set**, because operation 9 is not one of the"
			+ $" three; timer 1 holds {state.Timer1Seconds}");
		AssertTrue(
			ContainsDiagnostic(state, "not set, start or stop"),
			"and the diagnostic says which three it could have been;"
			+ $" the diagnostics are {state.Diagnostics.Count}");
	}

	/// <summary>
	/// Seconds beyond a day are refused.
	/// </summary>
	public void Test_SecondsBeyondADayAreRefused()
	{
		var (interpreter, _, state) = Run(
			Timer(0, pValue: GameSimulationState.MaxTimerSeconds + 1));

		interpreter.ExecuteFrame();

		AssertEq(
			state.Timer1Seconds, 0,
			"**and nothing was set**, because a day is the bound and a reader"
			+ " that accepted more would arm a timer the game never asked for;"
			+ $" timer 1 holds {state.Timer1Seconds}");
		AssertTrue(
			ContainsDiagnostic(state, "outside 0 to 86400"),
			"and the diagnostic names the bound; the diagnostics are"
			+ $" {state.Diagnostics.Count}");
	}

	/// <summary>
	/// A new game has no message options and no face.
	/// </summary>
	public void Test_ANewGameHasNoOptionsAndNoFace()
	{
		var (interpreter, presentation, state) = Run(
			Options(1, 0, 1, 1), Face("Actor", 1, 1, 1));
		interpreter.ExecuteFrame();
		interpreter.ExecuteFrame();

		presentation.Reset();
		state.Reset();

		AssertEq(
			presentation.MessageTransparent, false,
			"**and a new game's message window is not transparent**, because a"
			+ $" game that begins with a transparent box has a bug; it is {presentation.MessageTransparent}");
		AssertEq(
			presentation.MessagePosition,
			PresentationState.MessagePositionBottom,
			"and the window sits at the bottom, the position a new game has;"
			+ $" it is {presentation.MessagePosition}");
		AssertEq(
			presentation.FaceName, "",
			"**and there is no face**, because a game that opens with the last"
			+ " one's portrait has a bug; the name is empty");
		AssertEq(
			state.Timer1Active, false,
			$"and no timer runs; active is {state.Timer1Active}");
	}

	private static bool ContainsDiagnostic(
		GameSimulationState pState, string pNeedle)
	{
		foreach (var line in pState.Diagnostics)
		{
			if (line.Contains(pNeedle, StringComparison.Ordinal))
			{
				return true;
			}
		}
		return false;
	}
}
