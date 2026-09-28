using System;
using System.Collections.Generic;

using UniversalRPG.Rm2k;
using UniversalRPG.Rm2k.Interpreter;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Commands 20140 Show Choice Option and 20141 Show Choice End, and the
/// LCF indent that makes them possible.
/// </summary>
/// <remarks>
/// <para>
/// <strong>This slice found a field the parser was throwing away.</strong>
/// <c>20140</c> is a sub-command pair: the branch a player did not choose has
/// to be skipped, and the number that says which branch is the chosen one comes
/// from the LCF <c>0x0D</c> indent chunk. The decoder read that chunk, wrote it
/// into its dictionary, and <c>EventCommand</c> had no field for it — so every
/// event parsed completely and no branch could ever be identified.
/// </para>
/// <para>
/// A test of the codes, the parameters and the strings would have been green
/// the whole time.
/// </para>
/// </remarks>
public partial class TestRm2kChoiceBranches : TestBase
{
	private static Rm2kMap.EventCommand Cmd(
		int pCode, int pBranch = 0, int pIndent = 0, string pText = "")
	{
		return new Rm2kMap.EventCommand
		{
			Code = pCode,
			Text = pText,
			Parameters = [pBranch],
			Indent = pIndent,
		};
	}

	/// <summary>
	/// Three branches, each with a marker, ending with one choice end.
	/// </summary>
	/// <summary>
	/// Three branches, each ending with its own choice end, and one marker
	/// after the list.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>Each branch has its own <c>20141</c>, and that is the reference's
	/// shape.</strong> <c>CommandOptionGeneric</c> skips to the next command
	/// from <c>{ShowChoiceOption, ShowChoiceEnd}</c>, so a branch that had no end
	/// of its own would have its skip swallow every branch after it. A list
	/// written as one block with a single end is therefore a different command
	/// list, and a reader that treated it as the same one would skip the rest
	/// of the page on the first unchosen branch.
	/// </para>
	/// <para>
	/// The layout is <c>20140(n), Gold, 20141</c> three times and then the
	/// trailing Gold, so a reader that ran every branch would show 607 and a
	/// reader that ran none would show 7.
	/// </para>
	/// </remarks>
	private static (EventInterpreter Interpreter, GameSimulationState State)
		ThreeBranches(int pIndent, int pChosen)
	{
		var state = new GameSimulationState { MapId = 1 };
		var commands = new List<Rm2kMap.EventCommand>();
		for (var branch = 1; branch <= 3; branch++)
		{
			commands.Add(Cmd(EventInterpreter.ShowChoiceOption, branch, pIndent));
			commands.Add(new Rm2kMap.EventCommand
			{
				Code = EventInterpreter.ChangeGold,
				Text = "",
				Parameters = [0, EventInterpreter.GoldOpAdd, branch * 100, 0, 0],
				Indent = pIndent + 1,
			});
			commands.Add(Cmd(EventInterpreter.ChoiceEnd, 0, pIndent));
		}
		commands.Add(new Rm2kMap.EventCommand
		{
			Code = EventInterpreter.ChangeGold,
			Text = "",
			Parameters = [0, EventInterpreter.GoldOpAdd, 7, 0, 0],
			Indent = pIndent,
		});
		var interpreter = new EventInterpreter(state, 1, commands);
		interpreter.SubIndex = pChosen;
		return (interpreter, state);
	}

	// ---- Der Kern

	/// <summary>
	/// Genau ein Zweig läuft, und es ist der gewählte.
	/// </summary>
	/// <remarks>
	/// <strong>A reader that ran every branch would have a hero who asks a
	/// question, walks away, fights the guard, buys the sword and leaves — all
	/// in one frame.</strong> Each of the three branches writes a different
	/// variable, so "all of them ran" is visible in the state and not only in
	/// the diagnostics.
	/// </remarks>
	public void Test_OnlyTheChosenBranchRuns()
	{
		foreach (var chosen in new[] { 1, 2, 3 })
		{
			var (interpreter, state) = ThreeBranches(pIndent: 0, pChosen: chosen);
			// **Twelve frames, and not eight.** A skipped branch is two
			// commands the loop walks past in one frame, so the count of
			// commands is not the count of frames — a test that stopped after
			// the command count would have measured the skip as a stall.
			for (var i = 0; i < 16; i++)
			{
				if (!interpreter.ExecuteFrame())
				{
					break;
				}
			}

			// **The chosen branch hundred, plus the 7 after the list.** A
			// reader that ran every branch would show 607, because each branch
			// adds its own hundred; that is why the branches carry distinct
			// amounts and not a boolean.
			AssertEq(
				state.Gold, chosen * 100 + 7,
				$"**and only branch {chosen} ran, plus the 7 after the list** —"
				+ " a reader that ran every branch would show 607, and a reader"
				+ $" that ran none would show 7; the gold is {state.Gold}");
		}
	}

	/// <summary>
	/// A branch the player did not choose is skipped from its first command.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>This is the part that is easy to get wrong, and the three
	/// branches in the test above are laid out in the reference order: the
	/// chosen one is last among those before it.</strong> A list of three with
	/// branch 2 chosen runs 20140(1), skips to the end, runs 20140(2) and then
	/// — because branch 2 cleared the sub index — skips to the end again.
	/// </para>
	/// <para>
	/// The alternative reading, "run the list and pick", would give the same
	/// result here and is still wrong: <em>a reader that ran every branch
	/// would have a hero who asks a question, walks away, fights the guard,
	/// buys the sword and leaves, all in one frame.</em> The test that catches
	/// it is the amount, not the order.
	/// </para>
	/// </remarks>
	public void Test_AnUnchosenBranchIsSkippedFromItsFirstCommand()
	{
		// Branch 1 is chosen, so 1 runs; 2 and 3 do not.
		var (interpreter, state) = ThreeBranches(pIndent: 0, pChosen: 1);
		for (var i = 0; i < 16; i++)
		{
			if (!interpreter.ExecuteFrame())
			{
				break;
			}
		}

		AssertEq(
			state.Gold, 107,
			"**and the gold is 107, not 307 or 607**, because branch 2 and"
			+ " branch 3 were skipped at the first 20141; the gold is"
			+ $" {state.Gold}");
		AssertTrue(
			ContainsDiagnostic(state, "is not the chosen one"),
			"**and the diagnostic says the branches were skipped**, because a"
			+ " reader that produced the right gold by accident would be"
			+ $" indistinguishable here; the diagnostics are {state.Diagnostics.Count}");
	}

	/// <summary>
	/// Der gewählte Zweig löscht den Sub-Index, also läuft kein zweiter. den Sub-Index, also läuft kein zweiter.
	/// </summary>
	/// <remarks>
	/// The reference writes <c>SetSubcommandIndex(com.indent, sentinel)</c> on
	/// the branch that runs. <strong>Without that, a second list on the same
	/// page would compare against a stale number</strong> and the player's second
	/// answer would pick the branch the first answer cleared.
	/// </remarks>
	public void Test_TheChosenBranchClearsTheSubIndex()
	{
		var (interpreter, state) = ThreeBranches(0, 2);
		interpreter.ExecuteFrame();
		interpreter.ExecuteFrame();

		AssertEq(
			interpreter.SubIndex, -1,
			"**and the sub index is the sentinel after the branch ran**, because"
			+ $" the reference sets it so no second branch can match; it is {interpreter.SubIndex}");
		AssertTrue(
			ContainsDiagnostic(state, "branch 2 runs"),
			"and the diagnostic names the branch, so a page that ran the wrong"
			+ $" one can be read back; the diagnostics are {state.Diagnostics.Count}");
	}

	/// <summary>
	/// Ein Zweig ohne Ende läuft bis zum Listenende, und sagt es.
	/// </summary>
	/// <remarks>
	/// The reference's <c>SkipToNextConditional</c> walks to the next command
	/// from its set. <strong>A branch list whose last option has no end is a
	/// game bug</strong>, and a reader that ran off the end of the command array
	/// would read past the list.
	/// </remarks>
	public void Test_ABranchWithoutAnEndSaysSo()
	{
		var state = new GameSimulationState { MapId = 1 };
		var interpreter = new EventInterpreter(state, 1, [
			Cmd(EventInterpreter.ShowChoiceOption, 1, 0),
			Cmd(EventInterpreter.ShowChoiceOption, 2, 0),
		]);
		// **SubIndex 2 and not 1**: the first command is branch 1, and a sub
		// index of 1 would *run* it instead of skipping. The test wants the
		// skip, so it has to name a branch that is not the first one.
		interpreter.SubIndex = 2;

		interpreter.ExecuteFrame();
		var weiter = interpreter.ExecuteFrame();

		AssertEq(
			weiter, false,
			"**and the page finished instead of walking past the list**, because"
			+ $" the skip stopped at the end; the frame reported {weiter}");
		AssertTrue(
			ContainsDiagnostic(state, "has no 20141"),
			"**and the diagnostic names what is missing**, because 'skipped' alone"
			+ $" cannot be acted on; the diagnostics are {state.Diagnostics.Count}");
	}

	/// <summary>
	/// 20141 tut nichts, und genau das ist der Befehl.
	/// </summary>
	/// <remarks>
	/// The reference writes <c>return true;</c> and never reads the command.
	/// <strong>A reader that treated it as "the end of the choice" would close
	/// a window the player is still looking at</strong>, and the test asserts
	/// that nothing was written rather than that a flag flipped.
	/// </remarks>
	public void Test_ChoiceEndDoesNothing()
	{
		var state = new GameSimulationState { MapId = 1 };
		state.Variables.Add(0);
		var interpreter = new EventInterpreter(state, 1, [
			Cmd(EventInterpreter.ChoiceEnd, 0, 0),
		]);

		interpreter.ExecuteFrame();

		AssertEq(
			state.Variables.Count, 1,
			"**and no variable was written**, because the command reads nothing"
			+ $" and writes nothing; there are {state.Variables.Count}");
		AssertTrue(
			ContainsDiagnostic(state, "does nothing"),
			"**and the diagnostic says so**, because a command that appears to do"
			+ " nothing without saying so looks like a missing dispatch case;"
			+ $" the diagnostics are {state.Diagnostics.Count}");
	}

	/// <summary>
	/// The indent survives from the decoder to the event.
	/// </summary>
	/// <remarks>
	/// <strong>This is the field that was being dropped.</strong> The decoder
	/// read the LCF <c>0x0D</c> chunk and wrote "indent" into its dictionary, and
	/// <c>EventCommand</c> had no place for it — so every event parsed
	/// completely and no branch could ever be identified.
	/// </remarks>
	public void Test_TheIndentSurvivesIntoTheEvent()
	{
		var command = new Rm2kMap.EventCommand(
			EventInterpreter.ShowChoiceOption, [2], "", 7);

		AssertEq(
			command.Indent, 7,
			"**and the event carries the indent**, because a constructor that"
			+ $" dropped it would make every branch look like branch zero; it is {command.Indent}");

		var ohne = new Rm2kMap.EventCommand(EventInterpreter.ShowChoiceOption, [2]);
		AssertEq(
			ohne.Indent, 0,
			"**and a command built without one defaults to zero**, which is a"
			+ $" real block and not a marker; it is {ohne.Indent}");
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
