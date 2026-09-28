using System.Collections.Generic;

using UniversalRPG.Rm2k;
using UniversalRPG.Rm2k.Interpreter;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Command 1009, Change Battle Commands, executed against the three actor
/// modes EasyRPG's <c>GetActors</c> defines.
/// </summary>
/// <remarks>
/// <para>
/// The command was for several cards recorded as a message continuation line,
/// which it is not: every <c>1009</c> in the pinned fixtures carries four
/// integers and no text, and they are exactly this command's parameters. K-135
/// corrected the reading; this is the half that runs it.
/// </para>
/// <para>
/// The expected rows are the measured fixture values —
/// <c>[1,1,1,1]</c>, <c>[1,3,8,1]</c>, <c>[1,4,10,0]</c> — and the column
/// meanings come from <c>CommandChangeBattleCommands</c>, not from a table
/// someone wrote.
/// </para>
/// </remarks>
public partial class TestRm2kBattleCommands : TestBase
{
	private static Rm2kMap.EventCommand Command1009(
		int pActorMode, int pActorId, int pCommandId, int pAdd)
	{
		return new Rm2kMap.EventCommand
		{
			Code = EventInterpreter.ChangeBattleCommands,
			Text = "",
			Parameters = new List<int> { pActorMode, pActorId, pCommandId, pAdd },
		};
	}

	private static (EventInterpreter Interpreter, GameSimulationState State) Run(
		GameSimulationState pState, Rm2kMap.EventCommand pCommand)
	{
		var interpreter = new EventInterpreter(pState, 1, [pCommand]);
		return (interpreter, pState);
	}

	/// <summary>
	/// An actor that has never been touched has the database's commands, which
	/// is not the same as having none.
	/// </summary>
	/// <remarks>
	/// **This is the assertion the whole file depends on.** A reader that
	/// stored an empty list as the actor's commands would strip every ability
	/// from every actor the moment command 1009 ran. Absent is not empty, and
	/// the reference's <c>GetActor</c> hands back a null for exactly this.
	/// </remarks>
	public void Test_AnActorNobodyHasTouchedStillHasTheDatabasesCommands()
	{
		var state = new GameSimulationState
		{
		    MapId = 1,
		    PartyMemberIds = [1, 2],
		};

		AssertEq(
			state.GetActorBattleCommands(1), null,
			"**and no list exists yet, which means \"the database's set\"** and"
			+ " not \"nothing\"; the list is null");
		AssertEq(
			state.BattleCommands.ContainsKey(1), false,
			"and the actor has no entry at all, because an empty entry would"
			+ " look like a claim about the actor and is not one;"
			+ $" the dictionary holds {state.BattleCommands.Count} entries");
	}

	/// <summary>
	/// A command added to a hero reaches that hero and nobody else.
	/// </summary>
	public void Test_ACommandAddedToAHeroReachesThatHeroOnly()
	{
		var state = new GameSimulationState
		{
		    MapId = 1,
		    PartyMemberIds = [1, 2],
		};
		var (interpreter, _) = Run(state, Command1009(1, 1, 3, 1));

		interpreter.ExecuteFrame();

		var liste = state.GetActorBattleCommands(1);
		AssertTrue(
			liste != null && liste.Contains(3),
			"and actor 1 has command 3, because the mode was one hero by id;"
			+ $" the list is {(liste == null ? "null" : string.Join(",", liste))}");
		AssertEq(
			state.GetActorBattleCommands(2), null,
			"**and actor 2 is untouched**, because mode 1 is one hero and not"
			+ $" the party; actor 2's list is null");
	}

	/// <summary>
	/// The party mode is the whole party, in party order.
	/// </summary>
	public void Test_ThePartyModeReachesEveryPartyMember()
	{
		var state = new GameSimulationState
		{
		    MapId = 1,
		    PartyMemberIds = [1, 2, 3, 4],
		};
		var (interpreter, _) = Run(
			state, Command1009(0, 0, 7, 1));

		interpreter.ExecuteFrame();

		foreach (var actorId in new[] { 1, 2, 3, 4 })
		{
			var liste = state.GetActorBattleCommands(actorId);
			AssertTrue(
				liste != null && liste.Contains(7),
				$"and actor {actorId} has command 7, because mode 0 is the"
				+ $" party and this is member {actorId}");
		}
	}

	/// <summary>
	/// The variable mode names the hero through a variable.
	/// </summary>
	public void Test_TheVariableModeNamesTheHeroThroughAVariable()
	{
		var state = new GameSimulationState
		{
		    MapId = 1,
		    PartyMemberIds = [1, 2],
		    // **Variables are 1 based in the event and 0 based in the
		    // array**, because `GetVariable` reads `Variables[pId - 1]`. So
		    // id 2 lives at index 1 and the slot for id 1 has to exist first.
		    // A first draft wrote `Variables[1] = 2` on an empty array and
		    // threw — which is what an index out of range looks like on a
		    // reader that is one-based in the event and zero-based here.
		    Variables = [0, 2],
		};
		var (interpreter, _) = Run(
			state, Command1009(2, 2, 5, 1));

		interpreter.ExecuteFrame();

		AssertTrue(
			state.GetActorBattleCommands(2) != null,
			"**and actor 2 — the one the variable named — has a list**, because"
			+ " mode 2 reads parameters[1] as a variable id and not as an actor"
			+ " id; actor 1's list is"
			+ $" {(state.GetActorBattleCommands(1) == null ? "null" : "set")}");
		AssertEq(
			state.GetActorBattleCommands(1), null,
			"and actor 1 does not, because the variable said 2 and not 1;"
			+ " actor 1's list is null");
	}

	/// <summary>
	/// An actor id of zero touches nobody, and the page carries on.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Hero ids run from 1 to <c>MaxActorId</c>, so **0 is the one value a
	/// game can actually reach that names no actor** — a variable that was never
	/// set, or one that was set to nothing on purpose. The reference logs a
	/// warning and returns an empty actor list, so the command runs and touches
	/// nobody.
	/// </para>
	/// <para>
	/// <strong>A reader that refused the whole page would drop the rest of an
	/// event because one id was wrong</strong>, which is how a typo in the
	/// editor becomes a game that stops halfway through a cutscene.
	/// </para>
	/// </remarks>
	public void Test_AnActorIdOfZeroTouchesNobodyAndThePageCarriesOn()
	{
		var state = new GameSimulationState
		{
		    MapId = 1,
		    PartyMemberIds = [1],
		};
		var (interpreter, _) = Run(state, Command1009(1, 0, 3, 1));

		interpreter.ExecuteFrame();

		AssertEq(
			state.BattleCommands.Count, 0,
			"and nobody was touched, because hero 0 does not exist and the"
			+ $" reference returns an empty actor list; the dictionary holds {state.BattleCommands.Count}");
		AssertTrue(
			interpreter.IsRunning,
			"**and the page is still running**, because a bad id is a warning"
			+ $" and not the end of the event; running is {interpreter.IsRunning}");
		AssertTrue(
			ContainsDiagnostic(state, "invalid actor id 0"),
			"and the diagnostic names the id that was wrong, because \"skipped\""
			+ " without saying what was wrong leaves the reader guessing;"
			+ $" the diagnostics are {state.Diagnostics.Count}");
	}

	/// <summary>
	/// A variable that holds no actor is the same case, one step later.
	/// </summary>
	public void Test_AVariableHoldingNoActorTouchesNobody()
	{
		var state = new GameSimulationState
		{
		    MapId = 1,
		    PartyMemberIds = [1],
		};
		// Variable 3 was never set, so it reads 0.
		var (interpreter, _) = Run(state, Command1009(2, 3, 3, 1));

		interpreter.ExecuteFrame();

		AssertEq(
			state.BattleCommands.Count, 0,
			"and nobody was touched, because the variable names hero 0 and no"
			+ $" hero has that id; the dictionary holds {state.BattleCommands.Count}");
		AssertTrue(
			ContainsDiagnostic(state, "holds invalid actor id 0"),
			"**and the diagnostic says the variable held nothing usable**,"
			+ " because the variable and the id are two different mistakes and"
			+ $" one message about them would hide half; the diagnostics are {state.Diagnostics.Count}");
	}

	/// <summary>
	/// Removing a command that is there takes it away.
	/// </summary>
	public void Test_RemovingACommandTakesItAway()
	{
		var state = new GameSimulationState
		{
		    MapId = 1,
		    PartyMemberIds = [1],
		};
		var (interpreter, _) = Run(state, Command1009(1, 1, 8, 1));
		interpreter.ExecuteFrame();
		AssertTrue(
			state.GetActorBattleCommands(1) != null,
			"sanity: the command was added");

		var (zweit, _) = Run(state, Command1009(1, 1, 8, 0));
		zweit.ExecuteFrame();

		var liste = state.GetActorBattleCommands(1);
		AssertTrue(
			liste != null && !liste.Contains(8),
			"**and the removal took it away**, because parameters[3] is a flag"
			+ " and not a count, and a reader that counted with it would have"
			+ $" removed nothing or everything; the list is {(liste == null ? "null" : string.Join(",", liste))}");
	}

	/// <summary>
	/// Adding a command the actor already has changes nothing, and says so.
	/// </summary>
	/// <remarks>
	/// **Both no-change directions are reported.** The reference is silent, but
	/// a command that was asked to do something and did not is the case a
	/// diagnostic exists for — and the two directions mean opposite things.
	/// "Add what it already has" is an author's habit; "remove what it does not
	/// have" is usually a mistake worth naming.
	/// </remarks>
	public void Test_AddingACommandTheActorAlreadyHasChangesNothingAndSaysSo()
	{
		var state = new GameSimulationState
		{
		    MapId = 1,
		    PartyMemberIds = [1],
		};
		var (erst, _) = Run(state, Command1009(1, 1, 4, 1));
		erst.ExecuteFrame();
		// **A second run needs a second page.** A first draft called
		// ExecuteFrame three times on one page and expected three executions;
		// a frame is one step, and the page held on nothing but ran out. The
		// add flag makes the first run the only one that changed anything, so
		// the second page is where the no-change case is actually reached.
		var (zweit, _) = Run(state, Command1009(1, 1, 4, 1));
		zweit.ExecuteFrame();

		var liste = state.GetActorBattleCommands(1);
		AssertEq(
			liste?.Count, 1,
			"**and the list has one entry and not three**, because a reader that"
			+ $" appended unconditionally would grow it every time; it holds {(liste == null ? -1 : liste.Count)}");
		AssertTrue(
			ContainsDiagnostic(state, "already has"),
			"and a diagnostic says the actor already had it, because a command"
			+ " that did nothing is exactly what a diagnostic is for; the"
			+ $" diagnostics are {state.Diagnostics.Count}");
	}

	/// <summary>
	/// Removing a command the actor does not have changes nothing either.
	/// </summary>
	public void Test_RemovingACommandTheActorDoesNotHaveSaysSo()
	{
		var state = new GameSimulationState
		{
		    MapId = 1,
		    PartyMemberIds = [1],
		};
		var (interpreter, _) = Run(state, Command1009(1, 1, 11, 0));

		interpreter.ExecuteFrame();

		var liste = state.GetActorBattleCommands(1);
		AssertEq(
			liste?.Count ?? 0, 0,
			"and nothing was removed from nothing, because an absent list means"
			+ " the database's set and not an empty one;"
			+ $" the list holds {(liste == null ? 0 : liste.Count)}");
		AssertTrue(
			ContainsDiagnostic(state, "lacks"),
			"**and a diagnostic says the actor already lacked it**, because the"
			+ " two no-change directions mean opposite things and one message"
			+ $" would not say which; the diagnostics are {state.Diagnostics.Count}");
	}

	/// <summary>
	/// A command with too few parameters is refused, not guessed at.
	/// </summary>
	public void Test_ACommandWithTooFewParametersIsRefused()
	{
		var state = new GameSimulationState
		{
		    MapId = 1,
		    PartyMemberIds = [1],
		};
		var (interpreter, _) = Run(
			state,
			new Rm2kMap.EventCommand
			{
				Code = EventInterpreter.ChangeBattleCommands,
				Text = "",
				Parameters = [1, 1, 3],
			});

		interpreter.ExecuteFrame();

		AssertEq(
			state.BattleCommands.Count, 0,
			"and nothing changed, because parameters[3] is the add flag and a"
			+ " reader that defaulted it would have added the command the game"
			+ $" never asked to add; the dictionary holds {state.BattleCommands.Count}");
		AssertTrue(
			ContainsDiagnostic(state, "Malformed")
				|| ContainsDiagnostic(state, "malformed"),
			"and the diagnostic says the command was malformed, because"
			+ " CmdSetup gives it a minimum width of four;"
			+ $" the diagnostics are {state.Diagnostics.Count}");
	}

	/// <summary>
	/// A new game does not inherit the last one's battle commands.
	/// </summary>
	public void Test_BattleCommandsDoNotSurviveANewGame()
	{
		var state = new GameSimulationState
		{
		    MapId = 1,
		    PartyMemberIds = [1],
		};
		var (interpreter, _) = Run(state, Command1009(1, 1, 2, 1));
		interpreter.ExecuteFrame();
		AssertEq(
			state.GetActorBattleCommands(1)?.Count, 1,
			"sanity: one command was added");

		state.Reset();

		AssertEq(
			state.BattleCommands.Count, 0,
			"**and a new game has no battle command overrides**, because they"
			+ " were this game's changes and not the actors' own; the dictionary"
			+ $" holds {state.BattleCommands.Count}");
		AssertEq(
			state.GetActorBattleCommands(1), null,
			"**and actor 1 is back to the database's set**, which is the null"
			+ " and not an empty list; the list is null");
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
