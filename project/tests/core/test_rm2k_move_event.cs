using System;
using System.Collections.Generic;

using UniversalRPG.Rm2k;
using UniversalRPG.Rm2k.Interpreter;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Commands 11310 Player Visibility and 11330 Move Event.
/// </summary>
/// <remarks>
/// <para>
/// <c>11330</c> is the first command that can reach
/// <c>Rm2kMoveRouteState</c>, and that class <strong>had no caller anywhere in
/// the project</strong> — K-131 built the decoder and the state machine and
/// tested both as free-standing objects, and no event could put one on a
/// character. Same island shape as the pictures.
/// </para>
/// <para>
/// The expectations come from <c>CommandPlayerVisibility</c> and
/// <c>CommandMoveEvent</c>.
/// </para>
/// </remarks>
public partial class TestRm2kMoveEvent : TestBase
{
	private static Rm2kMap.EventCommand Visibility(int pValue)
	{
		return new Rm2kMap.EventCommand
		{
			Code = EventInterpreter.PlayerVisibility,
			Text = "",
			Parameters = [pValue],
		};
	}

	/// <summary>
	/// <c>[eventId, moveFreq, idBitfield, skippable, route...]</c>.
	/// </summary>
	private static Rm2kMap.EventCommand Move(
		int pEventId, int pMoveFreq, int pIdBitfield, int pSkippable,
		params int[] pRoute)
	{
		var parameters = new List<int>
		{
			pEventId, pMoveFreq, pIdBitfield, pSkippable,
		};
		parameters.AddRange(pRoute);
		return new Rm2kMap.EventCommand
		{
			Code = EventInterpreter.MoveEvent,
			Text = "",
			Parameters = parameters,
		};
	}

	private sealed class RouteLog
	{
		public List<(int EventId, int Freq, IReadOnlyList<Rm2kMap.MoveCommand> Route,
			bool Repeat, bool Skippable)> Calls { get; } = [];

		public HashSet<int> Known { get; } = [];

		/// <summary>
		/// The hook a runtime would supply: it starts a route on a character it
		/// knows, and reports <c>false</c> for an id it does not.
		/// </summary>
		public bool Start(
			int pEventId, int pFreq, IReadOnlyList<Rm2kMap.MoveCommand> pRoute,
			bool pRepeat, bool pSkippable)
		{
			Calls.Add((pEventId, pFreq, pRoute, pRepeat, pSkippable));
			return Known.Contains(pEventId);
		}
	}

	private static (EventInterpreter Interpreter, GameSimulationState State, RouteLog Log)
		Run(RouteLog? pLog, params Rm2kMap.EventCommand[] pCommands)
	{
		var state = new GameSimulationState { MapId = 1 };
		var log = pLog ?? new RouteLog();
		var interpreter = new EventInterpreter(
			state, 1, pCommands, presentation: null,
			moveRouteStarter: log.Start);
		return (interpreter, state, log);
	}

	/// <summary>
	/// A hide command is the zero case, which is backwards from the name.
	/// </summary>
	/// <remarks>
	/// <c>bool hidden = (com.parameters[0] == 0);</c> — **a reader that mapped a
	/// non-zero to visible gets a hide right and a show wrong**, and a game
	/// whose only use of this command is to hide a sprite would work until the
	/// first time it showed one again.
	/// </remarks>
	public void Test_ZeroHidesAndNonZeroShows()
	{
		var (erst, state, _) = Run(null, Visibility(0));

		erst.ExecuteFrame();
		AssertEq(
			state.PlayerIsHidden, true,
			"**and the player is hidden**, because parameters[0] == 0 is the"
			+ $" hide case; it is {state.PlayerIsHidden}");

		var (zweit, state2, _) = Run(null, Visibility(1));
		zweit.ExecuteFrame();
		AssertEq(
			state2.PlayerIsHidden, false,
			"**and a non-zero shows**, which is the same rule read the other way"
			+ $" and not a second rule; it is {state2.PlayerIsHidden}");
	}

	/// <summary>
	/// Hiding also clears the through-position.
	/// </summary>
	/// <remarks>
	/// The reference calls <c>ResetThrough</c> right after hiding, with its own
	/// comment "RPG_RT does this here" — so a player who walked through a wall
	/// and is then hidden **does not stay standing in the wall.**
	/// </remarks>
	public void Test_HidingClearsTheThroughPosition()
	{
		var (_, state, _) = Run(null, Visibility(1));
		state.PlayerIsThrough = true;
		var (zweit, _, _) = Run(null, Visibility(0));
		_ = state;

		// Ein frischer Zustand, damit der Test den einen Weg prueft.
		var frisch = new GameSimulationState { MapId = 1 };
		frisch.PlayerIsThrough = true;
		var interpreter = new EventInterpreter(frisch, 1, [Visibility(0)]);

		interpreter.ExecuteFrame();

		AssertEq(
			frisch.PlayerIsHidden, true,
			"and the player is hidden; it is {frisch.PlayerIsHidden}");
		AssertEq(
			frisch.PlayerIsThrough, false,
			"**and the through-position is cleared**, because the reference"
			+ " calls ResetThrough right there and a player left standing in a"
			+ $" wall is a player the game has lost track of; it is {frisch.PlayerIsThrough}");
	}

	/// <summary>
	/// Showing does not clear it, because the reference only resets on hide.
	/// </summary>
	public void Test_ShowingDoesNotClearTheThroughPosition()
	{
		var state = new GameSimulationState { MapId = 1 };
		state.PlayerIsThrough = true;
		var interpreter = new EventInterpreter(state, 1, [Visibility(1)]);

		interpreter.ExecuteFrame();

		AssertEq(
			state.PlayerIsHidden, false,
			"and the player is shown; it is {state.PlayerIsHidden}");
		AssertEq(
			state.PlayerIsThrough, true,
			"**and the through-position is untouched**, because the reference's"
			+ " ResetThrough sits in the hide branch and not beside it; a reader"
			+ " that cleared it on every call would teleport a player out of a"
			+ $" wall just by making them visible again; it is {state.PlayerIsThrough}");
	}

	/// <summary>
	/// A move command reaches the character hook.
	/// </summary>
	/// <remarks>
	/// **This is the assertion the file exists for.** The route state machine
	/// was built, mutation checked and tested as a free-standing object, and
	/// nothing could put one on a character.
	/// </remarks>
	public void Test_AMoveCommandReachesTheCharacter()
	{
		var log = new RouteLog();
		log.Known.Add(3);
		var (interpreter, _, _) = Run(
			log,
			// Drei Routenschritte. A first draft wrote four and then asserted
			// three, which reads exactly like a reader that dropped the last
			// step — **and the count really is the rest of the list**, so the
			// assertion is the only thing that can be wrong.
			Move(3, 6, 0, 0, 0, 2, 1));

		interpreter.ExecuteFrame();

		AssertEq(
			log.Calls.Count, 1,
			"**and the hook was called once**, because 11330 is Move Event;"
			+ $" it was called {log.Calls.Count} times");
		AssertEq(
			log.Calls[0].EventId, 3,
			"and with the event id the command named; it is {log.Calls[0].EventId}");
		AssertEq(
			log.Calls[0].Freq, 6,
			"and the move frequency; it is {log.Calls[0].Freq}");
		AssertEq(
			log.Calls[0].Route.Count, 3,
			"**and three route steps, because the route is the rest of the"
			+ $" list from index four on**; it has {log.Calls[0].Route.Count}");
		AssertEq(
			log.Calls[0].Route[1].CommandId, 2,
			"and the second step is the code the command carried, in the second"
			+ $" place; it is {log.Calls[0].Route[1].CommandId}");
	}

	/// <summary>
	/// The id mode and the repeat flag share one word.
	/// </summary>
	/// <remarks>
	/// The reference reads <c>ValueOrVariableBitfield(com.parameters[2], 2,
	/// com.parameters[0])</c> and <c>ManiacBitmask(com.parameters[2], 0x1)</c> —
	/// **the mode is the low two bits and the repeat is the low bit.** A reader
	/// that took the whole number as the mode would read mode 3 where a game
	/// meant mode 1 and a repeat.
	/// </remarks>
	public void Test_TheIdModeAndTheRepeatFlagShareOneWord()
	{
		var log = new RouteLog();
		log.Known.Add(1);
		var (interpreter, _, _) = Run(
			log, Move(1, 6, 0b11, 0, 0));

		interpreter.ExecuteFrame();

		AssertEq(
			log.Calls[0].Repeat, true,
			"**and the low bit made it repeat**, because the third parameter's"
			+ $" low bit is the flag and not part of the mode; it is {log.Calls[0].Repeat}");
		AssertEq(
			log.Calls[0].EventId, 1,
			"and the id mode was the low two bits, so a constant was read as a"
			+ $" constant and not as a variable number; the id is {log.Calls[0].EventId}");
	}

	/// <summary>
	/// A move frequency outside 1 to 8 becomes 6, as the reference does.
	/// </summary>
	/// <remarks>
	/// <c>if (move_freq &lt;= 0 || move_freq &gt; 8) move_freq = 6;</c> — **that
	/// is the engine's own default and not a refusal.** A reader that refused
	/// would stop a route RPG_RT happily runs, and a game that wrote a zero
	 /// because the editor left the field empty would lose its movement.
	/// </remarks>
	public void Test_AMoveFrequencyOutOfRangeBecomesSix()
	{
		foreach (var kaputt in new[] { 0, -3, 9, 99 })
		{
			var log = new RouteLog();
			log.Known.Add(2);
			var (interpreter, _, _) = Run(log, Move(2, kaputt, 0, 0, 0));

			interpreter.ExecuteFrame();

			AssertEq(
				log.Calls.Count, 1,
				$"**and a frequency of {kaputt} still started the route**, because"
				+ " the reference falls back to six rather than refusing; the hook"
				+ $" was called {log.Calls.Count} times");
			AssertEq(
				log.Calls[0].Freq, 6,
				$"**and it became six**, and not the number the file carried; it is"
				+ $" {log.Calls[0].Freq}");
		}
	}

	/// <summary>
	/// An id no character carries touches nobody, and the page carries on.
	/// </summary>
	/// <remarks>
	/// The reference logs a warning and returns true, because
	/// <c>GetCharacter</c> returns null and the <c>if (event != NULL)</c> simply
	/// does not run. <strong>A reader that stopped the page would drop the rest
	/// of an event because one id was wrong.</strong>
	/// </remarks>
	public void Test_AnUnknownIdTouchesNobodyAndThePageCarriesOn()
	{
		var log = new RouteLog();
		var (interpreter, state, _) = Run(log, Move(99, 6, 0, 0, 0));

		interpreter.ExecuteFrame();

		AssertEq(
			log.Calls[0].EventId, 99,
			"**and the hook was still asked**, because the reference reaches for"
			+ $" the character before it knows whether there is one; the id is {log.Calls[0].EventId}");
		AssertTrue(
			ContainsDiagnostic(state, "no character carries"),
			"and the diagnostic says so, because a silent nothing is a bug that"
			+ $" survives every test; the diagnostics are {state.Diagnostics.Count}");
		AssertTrue(
			interpreter.IsRunning,
			"**and the page is still running**, because a bad id is a warning"
			+ $" and not the end of the event; running is {interpreter.IsRunning}");
	}

	/// <summary>
	/// A command with too few parameters is refused.
	/// </summary>
	public void Test_ACommandShorterThanFourIsRefused()
	{
		var log = new RouteLog();
		var (interpreter, state, _) = Run(
			log,
			new Rm2kMap.EventCommand
			{
				Code = EventInterpreter.MoveEvent,
				Text = "",
				Parameters = [1, 6, 0],
			});

		interpreter.ExecuteFrame();

		AssertEq(
			log.Calls.Count, 0,
			"**and no route was started**, because CmdSetup gives the command a"
			+ $" minimum width of four; the hook was called {log.Calls.Count} times");
		AssertTrue(
			ContainsDiagnostic(state, "malformed"),
			"and the diagnostic says the parameters were malformed;"
			+ $" the diagnostics are {state.Diagnostics.Count}");
	}

	/// <summary>
	/// An interpreter with no way to reach a character says so.
	/// </summary>
	public void Test_AnInterpreterWithoutACharacterHookSaysSo()
	{
		var state = new GameSimulationState { MapId = 1 };
		var interpreter = new EventInterpreter(
			state, 1, [Move(1, 6, 0, 0, 0)]);

		interpreter.ExecuteFrame();

		AssertTrue(
			ContainsDiagnostic(state, "cannot reach a character"),
			"**and it says it cannot reach a character**, because a reader that"
			+ " said nothing would look like a game that asked for no movement;"
			+ $" the diagnostics are {state.Diagnostics.Count}");
	}

	/// <summary>
	/// A new game shows the player again and is not through a wall.
	/// </summary>
	public void Test_ANewGameShowsThePlayer()
	{
		var state = new GameSimulationState { MapId = 1 };
		var interpreter = new EventInterpreter(state, 1, [Visibility(0)]);
		interpreter.ExecuteFrame();
		AssertEq(
			state.PlayerIsHidden, true, "sanity: the player is hidden");

		state.Reset();

		AssertEq(
			state.PlayerIsHidden, false,
			"**and a new game shows the player**, because a game that begins with"
			+ $" no sprite is a game with nothing to look at; it is {state.PlayerIsHidden}");
		AssertEq(
			state.PlayerIsThrough, false,
			"and nobody is through a wall; it is {state.PlayerIsThrough}");
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
