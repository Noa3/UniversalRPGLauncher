using System;
using System.Collections.Generic;

using UniversalRPG.Rm2k;
using UniversalRPG.Rm2k.Interpreter;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The five RPG2K3 menu commands, executed, and the gate that decides whether
/// they do anything.
/// </summary>
/// <remarks>
/// <para>
/// The commands were named and parameter-verified in K-135 and were not
/// executed. This runs them, and the part that matters is the refusal:
/// <strong>EasyRPG returns true — a silent no-op — on a game that is not an
/// E-command game</strong>, and a silent no-op in an interpreter is a bug that
/// survives every test because nothing changed.
/// </para>
/// <para>
/// These go through <c>ExecuteFrame</c>, the real runner, because a test that
/// calls <c>ExecuteMenuCommand</c> directly proves the method works and not that
/// the keypress reaches it.
/// </para>
/// </remarks>
public partial class TestRm2kMenuExecution : TestBase
{
	private static Rm2kMap.EventCommand Menu(int pCode)
	{
		return new Rm2kMap.EventCommand
		{
			Code = pCode,
			Text = "",
			Parameters = new List<int>(),
		};
	}

	/// <summary>
	/// A page of the given commands, on a state that says whether the game
	/// declares the E commands.
	/// </summary>
	private static (EventInterpreter Interpreter, GameSimulationState State) Run(
		bool pECommands, params Rm2kMap.EventCommand[] pCommands)
	{
		var state = new GameSimulationState { MapId = 1 };
		state.SupportsRpg2k3ECommands = pECommands;
		return (new EventInterpreter(state, 1, pCommands), state);
	}

	/// <summary>
	/// A game that does not declare the E commands gets a diagnostic, not a
	/// menu and not silence.
	/// </summary>
	/// <remarks>
	/// **This is the assertion the whole file exists for.** A reader that
	/// returned true without opening anything would pass every other test here,
	/// because the state would look the same after a refusal as after a no-op.
	/// </remarks>
	public void Test_ACommandOnAGameThatIsNotAnECommandGameIsRefusedVisibly()
	{
		var (interpreter, state) = Run(
			pECommands: false, Menu(EventInterpreter.OpenLoadMenu));

		interpreter.ExecuteFrame();

		AssertEq(
			state.CurrentScene, "",
			"and no scene opened, because the gate is the whole command and the"
			+ " game did not pass it; the current scene is \"" +
			state.CurrentScene + "\"");
		AssertEq(
			state.SceneStack.Count, 0,
			"and the scene stack is empty, because a reader that pushed a scene"
			+ " anyway would open a menu a 2000 game never asked for; it holds " +
			state.SceneStack.Count);
		AssertTrue(
			state.Diagnostics.Count > 0,
			"**and there is a diagnostic**, because EasyRPG's no-op is exactly"
			+ " what must not be copied; there are " + state.Diagnostics.Count);
		AssertContains(
			state.Diagnostics[0], "5001",
			"and the diagnostic names the command's number, so a reader can look"
			+ " it up; the first line is \"" + state.Diagnostics[0] + "\"");
		// A first draft asserted on the phrase "did not declare" and the
		// diagnostic said "does not declare", and the failure was the test's:
		// **asserting on prose a test invented makes the test the thing that
		// has to be right, and it was the wrong one.** The assertion is now
		// on the word that carries the meaning, and the wording in the code is
		// free to improve.
		AssertContains(
			state.Diagnostics[0], "E command",
			"and it says the command is an E command, so a reader knows the"
			+ " reason is the engine and not the command; the text is \"" +
			state.Diagnostics[0] + "\"");
		AssertContains(
			state.Diagnostics[0], "nothing opened",
			"**and it says nothing opened**, because EasyRPG's version of this"
			+ " is a no-op that reports nothing, and a diagnostic that only says"
			+ " \"skipped\" leaves the same question open");
	}

	/// <summary>
	/// The load menu opens and the page does not advance past it.
	/// </summary>
	/// <remarks>
	/// The second half of the rule: EasyRPG returns <c>false</c> after pushing
	/// a scene, so the page waits. A reader that advanced would run the rest of
	/// the page behind a menu nobody had closed.
	/// </remarks>
	public void Test_TheLoadMenuOpensAndThePageWaitsForIt()
	{
		var (interpreter, state) = Run(
			pECommands: true,
			Menu(EventInterpreter.OpenLoadMenu),
			Menu(EventInterpreter.End));

		var fertig = interpreter.ExecuteFrame();

		AssertEq(
			state.CurrentScene, "Load",
			"and the load scene is current, because 5001 is OpenLoadMenu;"
			+ " the scene is \"" + state.CurrentScene + "\"");
		AssertEq(
			state.SceneStack.Count, 1,
			"and it is on the stack once, because a second press of the same"
			+ " scene must not stack it twice; the stack holds " +
			state.SceneStack.Count);
		AssertEq(
			fertig, true,
			"**and the page is still running**, because the source returns false"
			+ " after pushing the scene and the page advances when the scene is"
			+ " popped; finished is " + fertig);
		AssertEq(
			interpreter.CurrentCommandIndex, 0,
			"and the index did not move, because advancing past a command that"
			+ " has not finished is how a page runs itself behind a menu; the"
			+ " index is " + interpreter.CurrentCommandIndex);
	}

	/// <summary>
	/// The same scene pushed twice does not stack twice.
	/// </summary>
	public void Test_PushingTheSameSceneTwiceDoesNotStackItTwice()
	{
		var (interpreter, state) = Run(
			pECommands: true, Menu(EventInterpreter.OpenLoadMenu));

		interpreter.ExecuteFrame();
		// The page is held on the command, so a second frame runs it again.
		interpreter.ExecuteFrame();

		AssertEq(
			state.SceneStack.Count, 1,
			"and the stack still holds one, because a scene that is already"
			+ " current is not pushed again, and a stack of two would need two"
			+ " pops to get back; it holds " + state.SceneStack.Count);
	}

	/// <summary>
	/// The settings scene is a different scene, and it holds the page too.
	/// </summary>
	public void Test_TwoDifferentScenesAreNotPushedInOneFrame()
	{
		var (interpreter, state) = Run(
			pECommands: true,
			Menu(EventInterpreter.OpenVideoOptions),
			Menu(EventInterpreter.OpenLoadMenu));

		interpreter.ExecuteFrame();

		AssertEq(
			state.CurrentScene, "Settings",
			"and the settings scene opened, because 5005 is OpenVideoOptions;"
			+ " the scene is \"" + state.CurrentScene + "\"");
		AssertEq(
			state.SceneStack.Count, 1,
			"and the load menu behind it has not opened, because the page held"
			+ " on the first command; the stack holds " + state.SceneStack.Count);
	}

	/// <summary>
	/// The three commands that do not open a scene run through.
	/// </summary>
	public void Test_ExitAtbAndFullscreenRunThroughAndAdvanceThePage()
	{
		var (interpreter, state) = Run(
			pECommands: true,
			Menu(EventInterpreter.ToggleAtbMode),
			Menu(EventInterpreter.ToggleFullscreen),
			Menu(EventInterpreter.ExitGame),
			Menu(EventInterpreter.End));

		// **One frame runs one command.** A first draft read the three toggles
		// in a single frame and failed on two of them, which reads exactly like
		// a broken command. It is the frame's shape: `ExecuteFrame` is one step
		// of the interpreter, and the page is a program that takes a step per
		// frame.
		interpreter.ExecuteFrame();
		AssertEq(
			state.AtbWaitMode, false,
			"and the ATB wait mode is off after the first frame, because 5003"
			+ " toggles it and it started on; it is " + state.AtbWaitMode);
		AssertEq(
			state.FullscreenRequested, false,
			"**and nothing else has run yet**, because one frame is one command;"
			+ " the request is " + state.FullscreenRequested);

		interpreter.ExecuteFrame();
		AssertEq(
			state.FullscreenRequested, true,
			"and the fullscreen change is requested after the second frame,"
			+ " because 5004 asks the display layer rather than claiming a"
			+ " screen state; the request is " + state.FullscreenRequested);

		interpreter.ExecuteFrame();
		var fertig = interpreter.ExecuteFrame();

		AssertEq(
			state.ExitRequested, true,
			"and an exit was requested by the third frame, because 5002 is"
			+ " ExitGame; the request is " + state.ExitRequested);
		AssertEq(
			fertig, false,
			"and the page finished, because none of the three opens a scene and"
			+ " all three return true; finished is " + fertig);
	}

	/// <summary>
	/// A toggled ATB mode toggles back on the next run of the same command.
	/// </summary>
	public void Test_AtbModeTogglesBothWays()
	{
		var state = new GameSimulationState { MapId = 1 };
		state.SupportsRpg2k3ECommands = true;
		var erst = new EventInterpreter(
			state, 1, [Menu(EventInterpreter.ToggleAtbMode)]);
		erst.ExecuteFrame();
		AssertEq(state.AtbWaitMode, false, "and it is off after the first run");

		var zweit = new EventInterpreter(
			state, 2, [Menu(EventInterpreter.ToggleAtbMode)]);
		zweit.ExecuteFrame();
		AssertEq(
			state.AtbWaitMode, true,
			"**and it is on again after the second, because the command toggles"
			+ " rather than sets**; it is " + state.AtbWaitMode);
	}

	/// <summary>
	/// A new game does not inherit a menu the last one opened.
	/// </summary>
	public void Test_AMenuFromTheLastGameDoesNotSurviveANewGame()
	{
		var (interpreter, state) = Run(
			pECommands: true, Menu(EventInterpreter.OpenLoadMenu));
		interpreter.ExecuteFrame();
		AssertEq(state.CurrentScene, "Load", "sanity: the menu is open");

		state.Reset();

		AssertEq(
			state.CurrentScene, "",
			"**and a new game starts with nothing open**, because a game that"
			+ " began in the load menu would be a game that began without a"
			+ " map; the scene is \"" + state.CurrentScene + "\"");
		AssertEq(
			state.SceneStack.Count, 0,
			"and the stack is empty; it holds " + state.SceneStack.Count);
		AssertEq(
			state.ExitRequested, false,
			"and no exit is pending, because a new game is a new game and not"
			+ " the end of the last one; it is " + state.ExitRequested);
		AssertEq(
			state.AtbWaitMode, true,
			"and the ATB mode is back to the default; it is " + state.AtbWaitMode);
		AssertEq(
			state.FullscreenRequested, false,
			"**and no fullscreen change is pending**, because the display layer"
			+ " of the last game has nothing to do with this one; the request is "
			+ state.FullscreenRequested);
	}

	/// <summary>
	/// A game that has not declared the E commands is not one, by default.
	/// </summary>
	public void Test_AGameThatHasNotSaidYesHasNotSaidYes()
	{
		var state = new GameSimulationState();
		AssertEq(
			state.SupportsRpg2k3ECommands, false,
			"**and the gate is closed until the game says otherwise**, because"
			+ " defaulting it open would open menus in every 2000 game that"
			+ " happens to carry a 5001; it is " + state.SupportsRpg2k3ECommands);
	}

	/// <summary>
	/// The scene stack starts empty, not holding an invented scene name.
	/// </summary>
	/// <remarks>
	/// A previous version of the state pushed <c>"Menu"</c> and made it current
	/// on reset. <strong>"Menu" is not an RPG_RT scene name</strong>, and a
	/// fictional default makes every scene test pass against a fiction.
	/// </remarks>
	public void Test_TheSceneStackStartsEmptyAndNotWithAMadeUpScene()
	{
		var state = new GameSimulationState();
		AssertEq(
			state.CurrentScene, "",
			"and nothing is current, because nothing is open;"
			+ " the scene is \"" + state.CurrentScene + "\"");
		AssertEq(
			state.SceneStack.Count, 0,
			"and the stack is empty; it holds " + state.SceneStack.Count);
	}

	private void AssertContains(string pHaystack, string pNeedle, string pWhy)
	{
		AssertTrue(
			pHaystack.Contains(pNeedle, StringComparison.Ordinal),
			pWhy + "; the text is \"" + pHaystack + "\" and it does not name \"" +
			pNeedle + "\"");
	}
}
