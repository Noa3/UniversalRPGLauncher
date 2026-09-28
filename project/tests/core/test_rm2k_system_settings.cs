using System;

using UniversalRPG.Rm2k;
using UniversalRPG.Rm2k.Interpreter;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Commands 10660 Change System BGM, 10670 Change System SFX, 10680 Change
/// System Graphics and 10690 Change Screen Transitions.
/// </summary>
/// <remarks>
/// <para>
/// None of these four was on the board. They came out of a fresh measurement
/// of the reference against the interpreter rather than from the card's list,
/// which is the second time that list was short.
/// </para>
/// <para>
/// The interesting one is <c>10690</c>: a transition set to the value the
/// database already holds is stored as <c>-1</c>, which means "stop
/// overriding it". That is the reference's own <c>return t != db ? t : -1;</c>
/// and a reader that stored the value would pin the transition forever.
/// </para>
/// </remarks>
public partial class TestRm2kSystemSettings : TestBase
{
	private static Rm2kMap.EventCommand Cmd(
		int pCode, string pText, params int[] pParameters)
	{
		return new Rm2kMap.EventCommand
		{
			Code = pCode,
			Text = pText,
			Parameters = [.. pParameters],
		};
	}

	private static (EventInterpreter Interpreter, GameSimulationState State) Run(
		params Rm2kMap.EventCommand[] pCommands)
	{
		var state = new GameSimulationState { MapId = 1 };
		for (var i = 0; i < 10; i++)
		{
			state.Variables.Add(0);
		}
		var interpreter = new EventInterpreter(state, 1, pCommands);
		return (interpreter, state);
	}

	// ---- 10660 and 10670

	/// <summary>
	/// The music slots are seven and the sound slots are twelve.
	/// </summary>
	/// <remarks>
	/// **A reader that offered one music slot would let a game replace its
	/// battle theme and its inn theme at once**, and one that offered twelve
	/// would offer five music slots that do not exist. For sounds, **a reader
	/// that offered only the menu four would leave a game with a silent
	/// battle** — the six battle contexts are the ones a player notices.
	/// </remarks>
	public void Test_TheTwoFamiliesHaveDifferentWidths()
	{
		var (bgm, state1) = Run(Cmd(
			EventInterpreter.ChangeSystemBGM, "Battle1", 0, 0, 100, 100, 50, 0, 0));
		bgm.ExecuteFrame();
		var (sfx, state2) = Run(Cmd(
			EventInterpreter.ChangeSystemSFX, "Decision", 1, 100, 100, 50, 0, 0));
		sfx.ExecuteFrame();

		AssertEq(
			GameSimulationState.MaxSystemBgmContext, 7,
			"**and there are seven music contexts** — battle, victory, inn, boat,"
			+ $" ship, airship, game over; the reference's switch has that many arms");
		AssertEq(
			GameSimulationState.MaxSystemSfxContext, 12,
			"**and twelve sound contexts** — four for the menu, one for the"
			+ $" battle, and one per battle event; the reference's switch has that many");
		AssertEq(
			state1.SystemBgmSlots[0].Name, "Battle1",
			"**and the music name reached the slot**, from the string field;"
			+ $" it is \"{state1.SystemBgmSlots[0].Name}\"");
		AssertEq(
			state2.SystemSfxSlots[1].Name, "Decision",
			"and the sound name reached its slot; it is"
			+ $" \"{state2.SystemSfxSlots[1].Name}\"");
	}

	/// <summary>
	/// Music has a fade-in and sounds do not.
	/// </summary>
	/// <remarks>
	/// <c>10660</c> reads <c>parameters[1]</c> as the fade and starts its
	/// numbers at one; <c>10670</c> has no fade and starts at volume. **A
	/// reader that read both the same way would fade a sound effect, and a
	/// sound effect with a fade is a sound effect the player waited for.**
	/// </remarks>
	public void Test_MusicHasAFadeAndSoundsDoNot()
	{
		var (bgm, state1) = Run(Cmd(
			EventInterpreter.ChangeSystemBGM, "Theme", 0, 1500, 80, 90, 50, 0, 0));
		bgm.ExecuteFrame();

		AssertEq(
			state1.SystemBgmSlots[0].FadeIn, 1500,
			"**and the music carries a fade-in of 1500 ms**, from parameters[1],"
			+ $" because the reference sets music.fadein; it is {state1.SystemBgmSlots[0].FadeIn}");
		AssertEq(
			state1.SystemBgmSlots[0].Volume, 80,
			"**and the volume is 80**, which is a different parameter and a"
			+ $" reader that shifted by one would fade at 80; it is {state1.SystemBgmSlots[0].Volume}");
		AssertEq(
			state1.SystemBgmSlots[0].Tempo, 90,
			$"and the tempo is 90; it is {state1.SystemBgmSlots[0].Tempo}");
		AssertEq(
			state1.SystemBgmSlots[0].Balance, 50,
			"and the balance is 50, which is centred; it is"
			+ $" {state1.SystemBgmSlots[0].Balance}");
	}

public void Test_TheNumbersAreTheParametersThemselves()
	{
		var state = new GameSimulationState { MapId = 1 };
		var interpreter = new EventInterpreter(state, 1, [Cmd(
			EventInterpreter.ChangeSystemSFX, "Cursor", 0, 42, 90, 50, 0, 0)]);

		interpreter.ExecuteFrame();

		AssertEq(
			state.SystemSfxSlots[0].Volume, 42,
			"**and the volume is 42**, because without the Maniac patch the"
			+ " reference returns parameters[val_idx] directly and the value index"
			+ " is the same number as the mode index; a reader that read"
			+ $" parameters[5] as the value would have heard 0; it is {state.SystemSfxSlots[0].Volume}");
		AssertEq(
			state.SystemSfxSlots[0].Tempo, 90,
			"**and the tempo is 90**, which is parameters[2] and not parameters[3];"
			+ $" it is {state.SystemSfxSlots[0].Tempo}");
	}

	/// <summary>
	/// A Maniac game is told, because this runtime has no game-string mirror.
	/// </summary>
	public void Test_AManiacGameIsToldRatherThanGuessedAt()
	{
		var state = new GameSimulationState { MapId = 1 };
		state.SupportsManiacPatch = true;
		var interpreter = new EventInterpreter(state, 1, [Cmd(
			EventInterpreter.ChangeSystemSFX, "Cursor", 0, 42, 90, 50, 0, 0)]);

		interpreter.ExecuteFrame();

		AssertEq(
			state.SystemSfxSlots[0].Volume, 42,
			"**and the plain value was still used**, because a packed bitfield"
			+ " read out of a mode index would be a guess; the volume is"
			+ $" {state.SystemSfxSlots[0].Volume}");
		AssertTrue(
			ContainsDiagnostic(state, "Maniac bitfield"),
			"**and the diagnostic says the patch is in play**, because a game"
			+ " that uses it and gets the plain value deserves to know;"
			+ $" the diagnostics are {state.Diagnostics.Count}");
	}

	public void Test_AnUnknownContextIsRefused()
	{
		var (interpreter, state) = Run(
			Cmd(EventInterpreter.ChangeSystemBGM, "Theme", 0, 0, 100, 100, 50, 0, 0),
			Cmd(EventInterpreter.ChangeSystemBGM, "Bogus", 9, 0, 100, 100, 50, 0, 0));

		interpreter.ExecuteFrame();
		interpreter.ExecuteFrame();

		AssertEq(
			state.SystemBgmSlots.Count, 1,
			"**and there is still one slot, not two**, because the reference's"
			+ " switch has no default arm; there are"
			+ $" {state.SystemBgmSlots.Count} slots");
		AssertEq(
			state.SystemBgmSlots[0].Name, "Theme",
			"**and the first one is untouched**, because the refused command"
			+ $" named a context that does not exist; it is \"{state.SystemBgmSlots[0].Name}\"");
		AssertTrue(
			ContainsDiagnostic(state, "is outside 0 to 6"),
			"and the diagnostic names the bound, because 'refused' without it"
			+ $" cannot be acted on; the diagnostics are {state.Diagnostics.Count}");
	}

	/// <summary>
	/// The audio numbers are clamped, because the format bounds them.
	/// </summary>
	/// <remarks>
	/// <strong>The reference does not clamp and the format does</strong>, so a
	/// command asking for 400 would write a number no player could hear.
	/// Clamping is the difference between "too loud" and "no sound at all".
	/// </remarks>
	public void Test_TheAudioNumbersAreClamped()
	{
		var (interpreter, state) = Run(Cmd(
			EventInterpreter.ChangeSystemBGM, "Loud", 0, 0, 400, -20, 50, 0, 0));

		interpreter.ExecuteFrame();

		AssertEq(
			state.SystemBgmSlots[0].Volume, 100,
			"**and the volume is 100, not 400**, because the bound is what a"
			+ $" player can hear; it is {state.SystemBgmSlots[0].Volume}");
		AssertEq(
			state.SystemBgmSlots[0].Tempo, 0,
			"**and the tempo is 0, not -20**, because a negative tempo is not a"
			+ $" tempo; it is {state.SystemBgmSlots[0].Tempo}");
	}

	// ---- 10690

	/// <summary>
	/// A transition is stored as asked.
	/// </summary>
	public void Test_ATransitionIsStoredAsAsked()
	{
		var (interpreter, state) = Run(
			Cmd(EventInterpreter.ChangeScreenTransitions, "", 0, 3));

		interpreter.ExecuteFrame();

		AssertEq(
			state.SystemTransitions[GameSimulationState.TransitionTeleportErase], 3,
			"**and transition 0 is 3**, because the reference stores what it was"
			+ " given when it differs from the database;"
			+ $" it is {state.SystemTransitions[GameSimulationState.TransitionTeleportErase]}");
	}

	/// <summary>
	/// The six transitions are separate.
	/// </summary>
	/// <remarks>
	/// **A reader that offered one "transition" would make a game that fades
	/// out on teleport also fade out on entering a battle** — and those are
	/// separate choices a game makes on purpose.
	/// </remarks>
	public void Test_TheSixTransitionsAreSeparate()
	{
		var codes = new[]
		{
			GameSimulationState.TransitionTeleportErase,
			GameSimulationState.TransitionTeleportShow,
			GameSimulationState.TransitionBeginBattleErase,
			GameSimulationState.TransitionBeginBattleShow,
			GameSimulationState.TransitionEndBattleErase,
			GameSimulationState.TransitionEndBattleShow,
		};
		var cmds = new Rm2kMap.EventCommand[codes.Length];
		for (var i = 0; i < codes.Length; i++)
		{
			cmds[i] = Cmd(EventInterpreter.ChangeScreenTransitions, "", codes[i], i + 1);
		}
		var (interpreter, state) = Run(cmds);
		for (var i = 0; i < codes.Length; i++)
		{
			interpreter.ExecuteFrame();
		}

		AssertEq(
			state.SystemTransitions.Count, 6,
			"**and all six are set**, because they are six fields and not one;"
			+ $" there are {state.SystemTransitions.Count}");
		for (var i = 0; i < codes.Length; i++)
		{
			AssertEq(
				state.SystemTransitions[codes[i]], i + 1,
				$"**and transition {codes[i]} kept its own value**, because a reader"
				+ " that wrote one field for all six would have left five at"
				+ $" nothing; it is {state.SystemTransitions[codes[i]]}");
		}
	}

	/// <summary>
	/// A transition outside the six is refused.
	/// </summary>
	/// <remarks>
	/// The reference <c>assert(false)</c>s on an unknown one, which in a debug
	/// build is a crash and in a release build is a write to nothing. **This
	/// reader says which values are allowed**, which is the difference between a
	/// game bug and a crash.
	/// </remarks>
	public void Test_AnUnknownTransitionIsRefused()
	{
		var (interpreter, state) = Run(
			Cmd(EventInterpreter.ChangeScreenTransitions, "", 9, 3));

		interpreter.ExecuteFrame();

		AssertEq(
			state.SystemTransitions.Count, 0,
			"**and nothing was stored**, because nine is not one of the six;"
			+ $" there are {state.SystemTransitions.Count} entries");
		AssertTrue(
			ContainsDiagnostic(state, "is not one of the six"),
			"and the diagnostic lists the shape of the command; the diagnostics"
			+ $" are {state.Diagnostics.Count}");
	}

	// ---- 10680

	/// <summary>
	/// The system graphic is set, and an empty name is the database default.
	/// </summary>
	/// <remarks>
	/// <strong>An empty name is a request to go back to the database and not a
	/// file that does not exist</strong> — the reference has a
	/// <c>ResetSystemGraphic</c>, and this is the command that reaches it.
	/// </remarks>
	public void Test_TheSystemGraphicIsSetAndEmptyMeansTheDatabase()
	{
		var (setzen, state1) = Run(Cmd(
			EventInterpreter.ChangeSystemGraphics, "System2", 1, 2, 0, 0));
		setzen.ExecuteFrame();

		AssertEq(
			state1.SystemGraphicName, "System2",
			"**and the graphic is System2**, from the string field;"
			+ $" it is \"{state1.SystemGraphicName}\"");
		AssertEq(
			state1.SystemGraphicStretch, 1,
			"**and the stretch is 1**, from parameters[0], which is a different"
			+ $" field from the name; it is {state1.SystemGraphicStretch}");
		AssertEq(
			state1.SystemGraphicFont, 2,
			"and the font is 2, from parameters[1]; it is"
			+ $" {state1.SystemGraphicFont}");

		var (zurueck, state2) = Run(Cmd(
			EventInterpreter.ChangeSystemGraphics, "System2", 1, 2, 0, 0),
			Cmd(EventInterpreter.ChangeSystemGraphics, "", 0, 0, 0, 0));
		zurueck.ExecuteFrame();
		zurueck.ExecuteFrame();

		AssertEq(
			state2.SystemGraphicName, "",
			"**and an empty name went back to the database**, because the"
			+ $" reference treats it as the reset; it is \"{state2.SystemGraphicName}\"");
	}

	/// <summary>
	/// The stretch and the font are clamped.
	/// </summary>
	/// <remarks>
	/// The reference casts both straight from the command with no check, so a
	/// value past the enum produces an out-of-range value. <strong>Clamping is
	/// a diagnostic, not an assertion</strong>, and the diagnostic says which
	/// number was wrong.
	/// </remarks>
	public void Test_TheGraphicNumbersAreClamped()
	{
		var (interpreter, state) = Run(Cmd(
			EventInterpreter.ChangeSystemGraphics, "System2", 99, 99, 0, 0));

		interpreter.ExecuteFrame();

		AssertEq(
			state.SystemGraphicStretch, 1,
			"**and the stretch is 1, not 99**, because the enum stops there;"
			+ $" it is {state.SystemGraphicStretch}");
		AssertEq(
			state.SystemGraphicFont, 3,
			$"and the font is 3, not 99; it is {state.SystemGraphicFont}");
	}

	/// <summary>
	/// None of it survives a new game.
	/// </summary>
	/// <remarks>
	/// **A new game that inherited the last game's battle music would start in
	/// silence with somebody else playing.**
	/// </remarks>
	public void Test_NoneOfItSurvivesANewGame()
	{
		var (interpreter, state) = Run(
			Cmd(EventInterpreter.ChangeSystemBGM, "Theme", 0, 0, 100, 100, 50, 0, 0),
			Cmd(EventInterpreter.ChangeSystemSFX, "Cursor", 0, 100, 100, 50, 0, 0),
			Cmd(EventInterpreter.ChangeSystemGraphics, "System2", 1, 2, 0, 0),
			Cmd(EventInterpreter.ChangeScreenTransitions, "", 0, 3));
		interpreter.ExecuteFrame();
		interpreter.ExecuteFrame();
		interpreter.ExecuteFrame();
		interpreter.ExecuteFrame();

		state.Reset();

		AssertEq(
			state.SystemBgmSlots.Count, 0,
			"**and the music is gone**, because it belongs to the last game;"
			+ $" there are {state.SystemBgmSlots.Count} slots");
		AssertEq(
			state.SystemSfxSlots.Count, 0,
			"**and the sounds are gone**, for the same reason;"
			+ $" there are {state.SystemSfxSlots.Count} slots");
		AssertEq(
			state.SystemGraphicName, "",
			"and the graphic is the database default again; it is"
			+ $" \"{state.SystemGraphicName}\"");
		AssertEq(
			state.SystemTransitions.Count, 0,
			"**and no transition is overridden**, because a new game that kept"
			+ " one would fade in a way its author never chose;"
			+ $" there are {state.SystemTransitions.Count} entries");
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
