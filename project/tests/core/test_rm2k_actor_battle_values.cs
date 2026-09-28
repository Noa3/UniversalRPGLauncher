using System;
using System.Collections.Generic;

using UniversalRPG.Rm2k;
using UniversalRPG.Rm2k.Interpreter;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Commands 10430 Change Parameters, 10460 Change HP and 10470 Change SP.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Base and current are the whole subject of this file.</strong> The
/// reference has <c>SetBaseMaxHp</c> and <c>SetMaxHp</c> as different calls,
/// because a base survives a level change and a save and a temporary buff does
/// not. A reader that kept one number for both would let a saved game keep a
/// buff that ended three maps ago.
/// </para>
/// <para>
/// The expectations come from <c>CommandChangeParameters</c>,
/// <c>CommandChangeHP</c> and <c>CommandChangeSP</c>.
/// </para>
/// </remarks>
public partial class TestRm2kActorBattleValues : TestBase
{
	/// <summary>
	/// <c>[actorMode, actorId, operation, parameter, operandMode, operand]</c>.
	/// </summary>
	private static Rm2kMap.EventCommand Parameters(
		int pActorId, int pOperation, int pParameter,
		int pOperandMode = 0, int pOperand = 0, int pActorMode = 1)
	{
		return new Rm2kMap.EventCommand
		{
			Code = EventInterpreter.ChangeParameters,
			Text = "",
			Parameters =
				[pActorMode, pActorId, pOperation, pParameter, pOperandMode, pOperand],
		};
	}

	/// <summary>
	/// <c>[actorMode, actorId, remove, valueMode, value, lethal]</c> for HP and
	/// the same minus the last for SP.
	/// </summary>
	private static Rm2kMap.EventCommand Hp(
		int pActorId, int pRemove, int pValue, int pLethal = 0,
		int pActorMode = 1, int pValueMode = 0)
	{
		return new Rm2kMap.EventCommand
		{
			Code = EventInterpreter.ChangeHP,
			Text = "",
			Parameters = [pActorMode, pActorId, pRemove, pValueMode, pValue, pLethal],
		};
	}

	private static Rm2kMap.EventCommand Sp(
		int pActorId, int pRemove, int pValue, int pActorMode = 1)
	{
		return new Rm2kMap.EventCommand
		{
			Code = EventInterpreter.ChangeSP,
			Text = "",
			Parameters = [pActorMode, pActorId, pRemove, 0, pValue],
		};
	}

	/// <summary>
	/// Runs a command with the actor at a known current count and a known
	/// base maximum, because a test that sets the state after the interpreter
	/// exists is testing the wrong starting point, and a base left at its
	/// format minimum of 1 puts the ceiling where the floor is.
	/// </summary>
	private static (EventInterpreter Interpreter, GameSimulationState State) RunAt(
		int pCurrentHp, int pBaseMax, params Rm2kMap.EventCommand[] pCommands)
	{
		var state = new GameSimulationState { MapId = 1 };
		var values = new Rm2kActorValues();
		values.AddToParameter(Rm2kActorValues.ParameterMaxHp, pBaseMax - 1);
		state.ActorValues[1] = values;
		state.CurrentHp[1] = pCurrentHp;
		return (new EventInterpreter(state, 1, pCommands), state);
	}

	/// <summary>
	/// Runs a command with a known base maximum for the actor and, when given,
	/// a known current value.
	/// </summary>
	/// <remarks>
	/// The parameter the base goes into follows the first command, so a test
	/// about skill points gets a skill point maximum and not a hit point one.
	/// </remarks>
	private static (EventInterpreter Interpreter, GameSimulationState State) RunWithBase(
		int pBaseMax, Rm2kMap.EventCommand[] pCommands, int? pCurrentHp, int? pCurrentSp)
	{
		var state = new GameSimulationState { MapId = 1 };
		var values = new Rm2kActorValues();
		var which = pCommands.Length > 0
			&& pCommands[0].Code == EventInterpreter.ChangeSP
			? Rm2kActorValues.ParameterMaxSp
			: Rm2kActorValues.ParameterMaxHp;
		values.AddToParameter(which, pBaseMax - 1);
		state.ActorValues[1] = values;
		if (pCurrentHp.HasValue) state.CurrentHp[1] = pCurrentHp.Value;
		if (pCurrentSp.HasValue) state.CurrentSp[1] = pCurrentSp.Value;
		return (new EventInterpreter(state, 1, pCommands), state);
	}
	private static (EventInterpreter Interpreter, GameSimulationState State) Run(
		params Rm2kMap.EventCommand[] pCommands)
	{
		var state = new GameSimulationState { MapId = 1 };
		var interpreter = new EventInterpreter(state, 1, pCommands);
		return (interpreter, state);
	}

	// ---- 10430 Change Parameters

	/// <summary>
	/// It changes the base, and the base is what survives a save.
	/// </summary>
	/// <remarks>
	/// <strong>Not the current value</strong> — the reference calls
	/// <c>SetBaseMaxHp</c> here and <c>SetMaxHp</c> somewhere else. There is no
	/// current value at all in this reader, so a buff has nothing to write to,
	/// and that is the point: <em>a stored current value would outlive the buff
	/// that made it.</em>
	/// </remarks>
	public void Test_ItChangesTheBaseNotTheCurrentValue()
	{
		var (interpreter, state) = Run(
			Parameters(1, EventInterpreter.VarOpAdd, Rm2kActorValues.ParameterMaxHp,
				pOperand: 40));

		interpreter.ExecuteFrame();
		var werte = state.ActorValues[1];

		AssertEq(
			werte.BaseMaxHp, 41,
			"**and the base maximum HP is 41**, because the actor started at the"
			+ $" format minimum of 1 and 40 were added; it is {werte.BaseMaxHp}");
		AssertEq(
			state.CurrentHp.ContainsKey(1), false,
			"**and no current hit points were touched**, because this command"
			+ " changes the base and the current value is not stored; the"
			+ $" dictionary holds {state.CurrentHp.Count} entries");
	}

	/// <summary>
	/// The six parameters are six parameters.
	/// </summary>
	public void Test_EveryOneOfTheSixParametersIsReachable()
	{
		foreach (var parameter in new[]
		{
			Rm2kActorValues.ParameterMaxHp,
			Rm2kActorValues.ParameterMaxSp,
			Rm2kActorValues.ParameterAttack,
			Rm2kActorValues.ParameterDefense,
			Rm2kActorValues.ParameterSpirit,
			Rm2kActorValues.ParameterAgility,
		})
		{
			var (interpreter, state) = Run(
				Parameters(1, EventInterpreter.VarOpAdd, parameter, pOperand: 5));
			interpreter.ExecuteFrame();
			AssertTrue(
				state.ActorValues[1].GetParameter(parameter) >= 1,
				$"**and parameter {parameter} responded**, because the reference's"
				+ $" switch has an arm for it; it is now {state.ActorValues[1].GetParameter(parameter)}");
		}
	}

	/// <summary>
	/// A parameter outside the six is refused, and the value is left alone.
	/// </summary>
	public void Test_AParameterOutsideTheSixIsRefused()
	{
		var (interpreter, state) = Run(
			Parameters(1, EventInterpreter.VarOpAdd, 9, pOperand: 5));

		interpreter.ExecuteFrame();

		AssertEq(
			state.ActorValues[1].BaseMaxHp, 1,
			"**and nothing changed**, because nine is not one of the six and a"
			+ $" reader that wrote somewhere anyway would corrupt a value; the base HP is {state.ActorValues[1].BaseMaxHp}");
		AssertTrue(
			ContainsDiagnostic(state, "not one of the six"),
			"and the diagnostic says so; the diagnostics are"
			+ $" {state.Diagnostics.Count}");
	}

	/// <summary>
	/// The subtract operation goes down, and the clamp is reported.
	/// </summary>
	/// <remarks>
	/// <strong>Both halves are the command.</strong> The reference negates for
	/// subtract — that is <c>OperateValue</c>, not a reader-side switch — and
	/// every setter clamps. Subtracting more than a base holds stops at the
	/// floor rather than going negative, and a negative base would be an actor
	/// a game cannot kill.
	/// </remarks>
	public void Test_SubtractGoesDownAndTheClampIsVisible()
	{
		var (interpreter, state) = Run(
			Parameters(1, EventInterpreter.VarOpAdd, Rm2kActorValues.ParameterMaxHp,
				pOperand: 30),
			Parameters(1, EventInterpreter.VarOpSub, Rm2kActorValues.ParameterMaxHp,
				pOperand: 100));

		interpreter.ExecuteFrame();
		interpreter.ExecuteFrame();

		AssertEq(
			state.ActorValues[1].BaseMaxHp, 1,
			"**and the base is back at the floor of 1**, because every setter"
			+ " clamps and a negative base would be an actor no game can kill;"
			+ $" it is {state.ActorValues[1].BaseMaxHp}");
		AssertTrue(
			ContainsDiagnostic(state, "the difference is a clamp"),
			"**and the diagnostic says the arithmetic did not fit**, because a"
			+ " change that was clamped and one that landed look identical"
			+ $" otherwise; the diagnostics are {state.Diagnostics.Count}");
	}

	/// <summary>
	/// The operand can come from a variable, and from a variable through a
	/// variable.
	/// </summary>
	public void Test_TheOperandCanComeFromAVariable()
	{
		var state = new GameSimulationState { MapId = 1 };
		state.Variables.Add(0);
		state.Variables.Add(7);
		state.Variables.Add(2);
		state.Variables[1] = 25;
		// Variable 2 holds 7, so the indirect read is variable 7 — which the
		// test has not set, so it reads zero. Only the direct case is asserted
		// here; the indirect one is exercised by the branch and its own
		// diagnostic.
		var interpreter = new EventInterpreter(
			state, 1, [Parameters(
				1, EventInterpreter.VarOpAdd, Rm2kActorValues.ParameterMaxHp,
				EventInterpreter.VarOperandVariable, 2)]);

		interpreter.ExecuteFrame();

		AssertEq(
			state.ActorValues[1].BaseMaxHp, 26,
			"**and the base grew by what variable 2 held**, because the value is"
			+ " read through ValueOrVariable and a reader that read the operand"
			+ $" as a constant would have added seven; it is {state.ActorValues[1].BaseMaxHp}");
	}

	// ---- 10460 Change HP

	/// <summary>
	/// Parameters[2] is a remove flag and not a sign.
	/// </summary>
	/// <remarks>
	/// <c>bool remove = com.parameters[2] != 0; if (remove) amount = -amount;</c>
	/// — <strong>a reader that read it as a sign would subtract when the game
	/// meant to add</strong>, and the two are opposite: a game heals by setting
	/// remove to one and a *negative* value, or hurts by clearing it.
	/// </remarks>
	public void Test_TheThirdParameterIsARemoveFlagNotASign()
	{
		// **A remove flag of one and an amount of 20 is a loss of 20**, and the
		// actor starts at its base of 1, so a lethal change of that size
		// reaches zero. A test that cleared the flag here would have measured
		// the other direction and called it the same case.
		var (heilen, state1) = Run(Hp(1, pRemove: 1, pValue: 20, pLethal: 1));
		heilen.ExecuteFrame();
		AssertEq(
			state1.GetActorCurrentHp(1), 0,
			"**and a remove of 20 takes the hit points to zero**, because the"
			+ " actor started at its base of 1 and the flag negated the amount"
			+ " and the change was lethal; the HP is"
			+ $" {state1.GetActorCurrentHp(1)}");

		// The same cleared flag and the same direction: 50 plus 20 is 70, and
		// both assertions are the remove flag read as not a sign.
		var (tueten, state2) = RunWithBase(100, [Hp(1, pRemove: 0, pValue: 20, pLethal: 1)],
			pCurrentHp: 50, pCurrentSp: null);
		tueten.ExecuteFrame();
		AssertEq(
			state2.GetActorCurrentHp(1), 70,
			"**and a cleared flag adds 20**, which is the other direction of the"
			+ $" same flag and not a second rule; the HP is {state2.GetActorCurrentHp(1)}");
	}

	/// <summary>
	/// A non-lethal change stops at one hit point, and a lethal one reaches
	/// zero.
	/// </summary>
	/// <remarks>
	/// <strong>That is the sixth parameter's whole job</strong>, and it is the
	/// reference's own comment: a non-lethal change stops at one. A reader that
	/// let it reach zero would kill a hero a game had explicitly protected.
	/// </remarks>
	public void Test_NonLethalStopsAtOneAndLethalReachesZero()
	{
		// **The remove flag is one, so the amount is a loss.** A cleared flag
		// would add, and a heal of 999 against a maximum of 100 does not test
		// the floor at all — it stops at the ceiling and looks like it passed.
		var (geschuetzt, state1) = RunAt(50, 100, Hp(1, pRemove: 1, pValue: 999, pLethal: 0));
		geschuetzt.ExecuteFrame();
		AssertEq(
			state1.GetActorCurrentHp(1), 1,
			"**and a non-lethal change of 999 leaves exactly one**, because the"
			+ $" reference stops it there; the HP is {state1.GetActorCurrentHp(1)}");
		AssertTrue(
			ContainsDiagnostic(state1, "not lethal"),
			"and the diagnostic says the change was not lethal, because the"
			+ " reason a big change became a small one is the flag and nothing"
			+ $" else; the diagnostics are {state1.Diagnostics.Count}");

		var (toetlich, state2) = RunAt(50, 100, Hp(1, pRemove: 1, pValue: 999, pLethal: 1));
		toetlich.ExecuteFrame();
		AssertEq(
			state2.GetActorCurrentHp(1), 0,
			"**and a lethal one reaches zero**, which is the same arithmetic"
			+ $" without the floor; the HP is {state2.GetActorCurrentHp(1)}");
	}

	/// <summary>
	/// The ceiling is the current maximum, not the base.
	/// </summary>
	/// <remarks>
	/// A hero with a base of 40 and equipment worth 10 cannot be healed past
	/// 50 — and **a reader that clamped to the base would stop the heal at
	/// 40.** This reader has no equipment, so the current maximum is the base,
	/// and the test says so rather than pretending otherwise.
	/// </remarks>
	public void Test_TheHealStopsAtTheMaximum()
	{
		// **A cleared remove flag, because this one is a heal**, and an amount
		// that overshoots on purpose: 999 more hit points from 10 against a
		// maximum of 100. A heal that landed would prove nothing about the
		// ceiling, and a reader with no ceiling would hand back 1009.
		var (heilen, state) = RunWithBase(100,
			[Hp(1, pRemove: 0, pValue: 999, pLethal: 1)], pCurrentHp: 10, pCurrentSp: null);

		heilen.ExecuteFrame();

		AssertEq(
			state.GetActorCurrentHp(1), 100,
			"**and the heal stopped at the maximum of 100**, because the ceiling"
			+ " is the current maximum and not the 1009 the command asked for; a"
			+ $" reader with no ceiling would have said 1009; the HP is {state.GetActorCurrentHp(1)}");
	}

	// ---- 10470 Change SP

	/// <summary>
	/// Skill points clamp at zero and have no floor of one.
	/// </summary>
	/// <remarks>
	/// <c>if (sp &lt; 0) sp = 0;</c> and no lethal flag. <strong>HP and SP are
	/// not symmetric here</strong>, and a reader that gave SP the same floor as
	/// HP would leave a hero unable to cast anything.
	/// </remarks>
	public void Test_SkillPointsGoToZeroAndStayThere()
	{
		var (abziehen, state) = RunWithBase(50,
			[Sp(1, pRemove: 1, pValue: 999)], pCurrentHp: null, pCurrentSp: 30);

		abziehen.ExecuteFrame();

		AssertEq(
			state.GetActorCurrentSp(1), 0,
			"**and the skill points reached zero and not one**, because the"
			+ $" reference clamps SP at zero with no floor; they are {state.GetActorCurrentSp(1)}");
	}

	/// <summary>
	/// An actor nobody has hurt is at its maximum.
	/// </summary>
	/// <remarks>
	/// The reference's database row has no current HP and the engine fills it
	/// from the base on first read. <strong>A reader that stored a separate
	/// full value would have to keep the two in step forever</strong> — and the
	/// first time a game raised the base, the stored full value would be wrong.
	/// </remarks>
	public void Test_AnUntouchedActorIsAtItsMaximum()
	{
		var state = new GameSimulationState { MapId = 1 };
		state.ActorValues[3] = new Rm2kActorValues();
		state.ActorValues[3].AddToParameter(Rm2kActorValues.ParameterMaxHp, 199);

		AssertEq(
			state.GetActorCurrentHp(3), 200,
			"**and the current hit points read the base**, because an actor"
			+ " nobody has hurt is at its maximum; the HP is"
			+ $" {state.GetActorCurrentHp(3)}");
		AssertEq(
			state.CurrentHp.ContainsKey(3), false,
			"**and nothing was written**, because a read is not a change; the"
			+ $" dictionary holds {state.CurrentHp.Count} entries");
	}

	/// <summary>
	/// A new game has no battle values.
	/// </summary>
	public void Test_ABattleValueDoesNotSurviveANewGame()
	{
		var (interpreter, state) = Run(
			Parameters(1, EventInterpreter.VarOpAdd, Rm2kActorValues.ParameterMaxHp,
				pOperand: 99),
			Hp(1, pRemove: 0, pValue: 40, pLethal: 1));
		interpreter.ExecuteFrame();
		interpreter.ExecuteFrame();
		AssertEq(
			state.ActorValues.Count, 1, "sanity: a base value exists");

		state.Reset();

		AssertEq(
			state.ActorValues.Count, 0,
			"**and a new game has no base values**, because a hero that began"
			+ $" with the last one's stats is a hero the player did not choose;"
			+ $" there are {state.ActorValues.Count} entries");
		AssertEq(
			state.CurrentHp.Count, 0,
			"**and no current hit points either**, because a saved wound is"
			+ $" this game's wound; there are {state.CurrentHp.Count} entries");
	}

	/// <summary>
	/// A state the save codec will accept: the codec validates the map
	/// metadata, and a state that never had a map is not a state to save.
	/// </summary>
	private static GameSimulationState NewSavableState()
	{
		var state = new GameSimulationState { MapId = 7 };
		state.ConfigureMap(7, 2, 2, [true, false, true, true]);
		return state;
	}

	/// <summary>
	/// The base values and the current counts survive a save.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>A save that kept the current hit points and dropped the base
	/// would reload a hero clamped to a maximum he no longer has</strong>, and
	/// one that kept the base and dropped the count would reload a hero at full
	/// health after he was nearly dead. Both halves travel together, and a
	/// first draft that had neither would have lost every <c>10430</c> a game
	/// did at the next save.
	/// </para>
	/// <para>
	/// The actor nobody touched is <strong>not</strong> in the save, because
	/// its four base values are the format minimums and its counts are its
	/// maximums — an entry would be a claim that says nothing.
	/// </para>
	/// </remarks>
	public void Test_TheBaseAndCurrentValuesSurviveASave()
	{
		var state = NewSavableState();
		state.ActorValues[2] = new Rm2kActorValues();
		state.ActorValues[2].AddToParameter(Rm2kActorValues.ParameterMaxHp, 149);
		state.ActorValues[2].AddToParameter(Rm2kActorValues.ParameterAgility, 17);
		state.ActorValues[2].AddToParameter(Rm2kActorValues.ParameterMaxSp, 24);
		state.CurrentHp[2] = 33;
		state.CurrentSp[2] = 7;
		// An actor with a base and no counts, and an actor with counts and no
		// base entry of its own: both are legal and both must survive.
		state.ActorValues[5] = new Rm2kActorValues();
		state.CurrentHp[9] = 12;

		var json = Rm2kSimulationSaveCodec.Serialize(state);
		var restored = NewSavableState();
		AssertTrue(
			Rm2kSimulationSaveCodec.TryRestore(json, restored, out var fehler),
			$"and the save restores, because it is a save this codec wrote; {fehler}");

		AssertEq(
			restored.ActorValues[2].BaseMaxHp, 150,
			"**and the base maximum HP came back at 150**, because the base is"
			+ " what a level change and a save both preserve; it is"
			+ $" {restored.ActorValues[2].BaseMaxHp}");
		AssertEq(
			restored.ActorValues[2].BaseAgility, 18,
			"and the base agility came back at 18, which is a different field"
			+ " and a reader that copied the wrong one would show 150; it is"
			+ $" {restored.ActorValues[2].BaseAgility}");
		AssertEq(
			restored.CurrentHp[2], 33,
			"**and the current hit points came back at 33**, not at the maximum"
			+ " and not at zero, because a save that lost the count would heal a"
			+ $" dying hero; they are {restored.CurrentHp[2]}");
		AssertEq(
			restored.CurrentSp[2], 7,
			"and the current skill points came back at 7; they are"
			+ $" {restored.CurrentSp[2]}");
		AssertEq(
			restored.ActorValues.Count, 3,
			"**and three actors are in the save**, which are the two with bases"
			+ " and the one with only a count; a save that wrote an entry per"
			+ $" database row would hold many more; there are {restored.ActorValues.Count}");
		AssertEq(
			restored.CurrentHp[9], 12,
			"and the actor with a count and no base of its own kept it, because a"
			+ $" save that dropped it would load a hero nobody had ever hurt as one at 1; it is {restored.CurrentHp[9]}");
	}

	/// <summary>
	/// An actor nobody touched is not in the save.
	/// </summary>
	public void Test_AnUntouchedActorIsNotInTheSave()
	{
		var state = NewSavableState();

		var json = Rm2kSimulationSaveCodec.Serialize(state);
		var restored = NewSavableState();
		Rm2kSimulationSaveCodec.TryRestore(json, restored, out _);

		AssertEq(
			restored.ActorValues.Count, 0,
			"**and the save holds no actor at all**, because an entry for an"
			+ " untouched hero would be four base values the format minimums"
			+ " already imply, and a save full of claims that say nothing is a"
			+ $" save nobody can read; there are {restored.ActorValues.Count}");
	}

	/// <summary>
	/// A save with an out-of-bounds actor is rejected whole.
	/// </summary>
	/// <remarks>
	/// **Rejected and not clamped**, because a clamped hero is a hero the
	/// player never made, and every other row in this codec throws too. The
	/// test also checks that nothing was applied, which is the half a reader
	/// that validated in a loop after each write would get wrong.
	/// </remarks>
	public void Test_ASaveWithABadActorIsRejectedWhole()
	{
		var state = NewSavableState();
		state.ActorValues[1] = new Rm2kActorValues();
		state.CurrentHp[1] = 40;
		var json = Rm2kSimulationSaveCodec.Serialize(state);
		// One base out of bounds, with a good actor in front of it. The three
		// argument form is used because the two argument one is an
		// ordinal overload on this string, and a comparison culture would
		// make the mutation depend on the machine.
		var kaputt = json.Replace(
			"\"BaseAttack\":1", "\"BaseAttack\":99999",
			StringComparison.Ordinal);
		var restored = NewSavableState();

		AssertEq(
			Rm2kSimulationSaveCodec.TryRestore(kaputt, restored, out var fehler), false,
			"and the save is refused, because 99999 is past the bound the format"
			+ $" gives a base attack; the error is {fehler}");
		AssertEq(
			restored.CurrentHp.Count, 0,
			"**and nothing was applied**, because a reader that validated in a"
			+ " loop after each write would have restored the good actor first"
			+ $" and then thrown; there are {restored.CurrentHp.Count} entries");
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
