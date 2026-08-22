using System.Collections.Generic;
using Godot;
using UniversalRPG.Rm2k;
using UniversalRPG.Rm2k.Interpreter;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

public partial class TestEventInterpreter : TestBase
{
	private static List<Rm2kMap.EventCommand> EmptyCommands()
	{
		return new List<Rm2kMap.EventCommand>();
	}

	public void Test_EmptyEventEndsImmediately()
	{
		var state = new GameSimulationState();
		var interpreter = new EventInterpreter(state, 1, EmptyCommands());

		var result = interpreter.ExecuteFrame();

		AssertFalse(result);
		AssertFalse(interpreter.IsRunning);
	}

	public void Test_ConstantValuesMatchVerifiedLiblcfCodes()
	{
		AssertEq(EventInterpreter.End, 0);
		AssertEq(EventInterpreter.ShowMessage, 10110);
		AssertEq(EventInterpreter.ShowChoice, 10140);
		AssertEq(EventInterpreter.InputNumber, 10150);
		AssertEq(EventInterpreter.ControlSwitches, 10210);
		AssertEq(EventInterpreter.ControlVars, 10220);
		AssertEq(EventInterpreter.Teleport, 10810);
		AssertEq(EventInterpreter.Wait, 11410);
		AssertEq(EventInterpreter.ConditionalBranch, 12010);
		AssertEq(EventInterpreter.Loop, 12210);
		AssertEq(EventInterpreter.BreakLoop, 12220);
		AssertEq(EventInterpreter.Comment, 12410);
		AssertEq(EventInterpreter.ShowMessage2, 20110);
		AssertEq(EventInterpreter.ElseBranch, 22010);
		AssertEq(EventInterpreter.EndBranch, 22011);
		AssertEq(EventInterpreter.EndLoop, 22210);
		AssertEq(EventInterpreter.Comment2, 22410);
	}

	public void Test_ShowMessageRecordedWithContinuationLines()
	{
		var state = new GameSimulationState();
		var commands = new List<Rm2kMap.EventCommand>
		{
			new Rm2kMap.EventCommand(EventInterpreter.ShowMessage, null, "Hello"),
			new Rm2kMap.EventCommand(EventInterpreter.ShowMessage2, null, "World"),
			new Rm2kMap.EventCommand(EventInterpreter.End),
		};

		var interpreter = new EventInterpreter(state, 1, commands);

		var result = interpreter.ExecuteFrame();
		AssertTrue(result);
		AssertTrue(state.Diagnostics.Count > 0);
		AssertTrue(state.Diagnostics[0].Contains("Hello\\nWorld"), "message contains both lines");

		result = interpreter.ExecuteFrame();
		AssertFalse(result);
	}

	public void Test_WaitConvertsTenthsToFrames()
	{
		var state = new GameSimulationState();
		var commands = new List<Rm2kMap.EventCommand>
		{
			new Rm2kMap.EventCommand(EventInterpreter.Wait, new List<int> { 5 }),
			new Rm2kMap.EventCommand(EventInterpreter.Wait, new List<int> { 0 }),
			new Rm2kMap.EventCommand(EventInterpreter.End),
		};

		var interpreter = new EventInterpreter(state, 1, commands);

		interpreter.ExecuteFrame();
		AssertEq(interpreter.WaitFramesRemaining, 30, "5 tenths -> 30 frames");

		var frames = 1;
		while (interpreter.WaitFramesRemaining > 0 && frames <= 40)
		{
			interpreter.ExecuteFrame();
			frames++;
		}
		AssertEq(interpreter.WaitFramesRemaining, 0, "wait fully consumed");
		AssertTrue(state.Diagnostics[0].Contains("Wait 30 frames"));

		interpreter.ExecuteFrame();
		AssertEq(interpreter.WaitFramesRemaining, 1, "zero tenths still waits one frame");
	}

	public void Test_ControlSwitchesOnOffFlip()
	{
		var state = new GameSimulationState();
		var commands = new List<Rm2kMap.EventCommand>
		{
			SwitchCmd(2, 4, EventInterpreter.SwitchModeOn),
			SwitchCmd(3, 3, EventInterpreter.SwitchModeOff),
			SwitchCmd(3, 3, EventInterpreter.SwitchModeFlip),
			new Rm2kMap.EventCommand(EventInterpreter.End),
		};

		var interpreter = new EventInterpreter(state, 7, commands);

		AssertTrue(interpreter.ExecuteFrame());
		AssertEq(state.Switches.Count, 4, "switch array padded to range end");
		AssertFalse(state.Switches[0], "switch 1 untouched default");
		AssertTrue(state.Switches[1], "switch 2 ON");
		AssertTrue(state.Switches[2], "switch 3 ON");
		AssertTrue(state.Switches[3], "switch 4 ON");

		AssertTrue(interpreter.ExecuteFrame());
		AssertFalse(state.Switches[2], "switch 3 OFF after second command");
		AssertTrue(state.Switches[3], "switch 4 still ON");

		AssertTrue(interpreter.ExecuteFrame());
		AssertTrue(state.Switches[2], "switch 3 flipped back ON");

		AssertTrue(state.Diagnostics[0].Contains("Switches 2-4 -> ON"));
	}

	public void Test_ControlSwitchesRejectsInvalidRangeAndMode()
	{
		var state = new GameSimulationState();
		var commands = new List<Rm2kMap.EventCommand>
		{
			SwitchCmd(5, 2, EventInterpreter.SwitchModeOn),
			SwitchCmd(1, 2, 9),
			new Rm2kMap.EventCommand(EventInterpreter.ControlSwitches, new List<int> { 1 }),
			new Rm2kMap.EventCommand(EventInterpreter.End),
		};

		var interpreter = new EventInterpreter(state, 1, commands);

		for (var index = 0; index < 3; index++)
		{
			AssertTrue(interpreter.ExecuteFrame(), $"frame {index} skipped safely");
		}
		AssertEq(state.Switches.Count, 0, "no switch changes from invalid commands");
		AssertEq(state.Diagnostics.Count, 3, "one diagnostic per rejected command");
	}

	public void Test_ControlVarsConstantOperations()
	{
		var state = new GameSimulationState();
		state.Variables.Add(10);
		var commands = new List<Rm2kMap.EventCommand>
		{
			VarOp(1, EventInterpreter.VarOpAdd, 5),   // 15
			VarOp(1, EventInterpreter.VarOpSub, 20),  // -5
			VarOp(1, EventInterpreter.VarOpMul, 4),   // -20
			VarOp(1, EventInterpreter.VarOpDiv, 6),   // -3
			VarOp(1, EventInterpreter.VarOpMod, 2),   // -1
			VarOp(1, EventInterpreter.VarOpSet, 42),
			new Rm2kMap.EventCommand(EventInterpreter.End),
		};

		var interpreter = new EventInterpreter(state, 1, commands);

		for (var index = 0; index < commands.Count - 1; index++)
		{
			AssertTrue(interpreter.ExecuteFrame(), $"frame {index} executed");
		}
		AssertEq(state.Variables[0], 42, "final variable value after ops");
	}

	public void Test_ControlVarsVariableOperandReadsOtherVariable()
	{
		var state = new GameSimulationState();
		state.Variables.Add(7);
		state.Variables.Add(30);
		var commands = new List<Rm2kMap.EventCommand>
		{
			new Rm2kMap.EventCommand(
				EventInterpreter.ControlVars,
				new List<int> { 1, 1, 0, EventInterpreter.VarOpMul, EventInterpreter.VarOperandVariable, 2 }),
			new Rm2kMap.EventCommand(EventInterpreter.End),
		};

		var interpreter = new EventInterpreter(state, 1, commands);

		AssertTrue(interpreter.ExecuteFrame());
		AssertEq(state.Variables[0], 210, "var1 = var1 * var2 (operand type 1)");
	}

	public void Test_ControlVarsRejectsUnsupportedModes()
	{
		var state = new GameSimulationState();
		state.Variables.Add(11);
		var commands = new List<Rm2kMap.EventCommand>
		{
			new Rm2kMap.EventCommand(EventInterpreter.ControlVars,
				new List<int> { 1, 1, 1, EventInterpreter.VarOpSet, 0, 5 }), // indirect target mode
			new Rm2kMap.EventCommand(EventInterpreter.ControlVars,
				new List<int> { 1, 1, 0, 9, EventInterpreter.VarOperandConstant, 5 }), // unsupported op
			new Rm2kMap.EventCommand(EventInterpreter.End),
		};

		var interpreter = new EventInterpreter(state, 1, commands);

		AssertTrue(interpreter.ExecuteFrame());
		AssertEq(state.Variables[0], 11, "indirect target mode not applied yet");
		AssertTrue(state.Diagnostics[^1].Contains("target mode"));

		AssertTrue(interpreter.ExecuteFrame());
		AssertEq(state.Variables[0], 11, "unsupported operation not applied");
		AssertTrue(state.Diagnostics[^1].Contains("operation"));
	}

	public void Test_ControlVarsDivisionByZeroSkips()
	{
		var state = new GameSimulationState();
		state.Variables.Add(10);
		var commands = new List<Rm2kMap.EventCommand>
		{
			VarOp(1, EventInterpreter.VarOpDiv, 0),
			new Rm2kMap.EventCommand(EventInterpreter.End),
		};

		var interpreter = new EventInterpreter(state, 1, commands);

		AssertTrue(interpreter.ExecuteFrame());
		AssertEq(state.Variables[0], 10, "value unchanged on division by zero");
		AssertTrue(state.Diagnostics[0].Contains("division by zero"));
	}

	public void Test_TeleportSetsPendingTransfer()
	{
		var state = new GameSimulationState();
		var commands = new List<Rm2kMap.EventCommand>
		{
			new Rm2kMap.EventCommand(EventInterpreter.Teleport, new List<int> { 12, 30, 44 }),
			new Rm2kMap.EventCommand(EventInterpreter.End),
		};

		var interpreter = new EventInterpreter(state, 3, commands);

		AssertTrue(interpreter.ExecuteFrame());
		AssertTrue(state.IsTransferPending, "transfer pending flag");
		AssertEq(state.PendingMapId, 12, "pending map id");
		AssertEq(state.PendingX, 30, "pending x");
		AssertEq(state.PendingY, 44, "pending y");
		AssertTrue(state.Diagnostics[0].Contains("Transfer pending"));
	}

	public void Test_LoopExecutesUntilBreakThenContinuesPastEndLoop()
	{
		var state = new GameSimulationState();
		var commands = new List<Rm2kMap.EventCommand>
		{
			new Rm2kMap.EventCommand(EventInterpreter.Loop),
			VarOp(1, EventInterpreter.VarOpAdd, 1),
			new Rm2kMap.EventCommand(EventInterpreter.BreakLoop),
			new Rm2kMap.EventCommand(EventInterpreter.EndLoop),
			VarOp(2, EventInterpreter.VarOpSet, 99),
			new Rm2kMap.EventCommand(EventInterpreter.End),
		};

		var interpreter = new EventInterpreter(state, 1, commands);

		// Frame 1: Loop, frame 2: var1 += 1, frame 3: BreakLoop -> past EndLoop.
		AssertTrue(interpreter.ExecuteFrame());
		AssertTrue(interpreter.ExecuteFrame());
		AssertTrue(interpreter.ExecuteFrame());
		AssertEq(state.Variables[0], 1, "loop body ran once before break");
		AssertEq(interpreter.CurrentCommandIndex, 4, "break jumped past EndLoop");

		AssertTrue(interpreter.ExecuteFrame());
		AssertEq(state.Variables[1], 99, "commands after the loop execute");
		AssertFalse(interpreter.ExecuteFrame(), "event ends on End command");
	}

	public void Test_EndLoopsJumpsBackToLoopStart()
	{
		var state = new GameSimulationState();
		var commands = new List<Rm2kMap.EventCommand>
		{
			new Rm2kMap.EventCommand(EventInterpreter.Loop),          // idx 0
			VarOp(1, EventInterpreter.VarOpAdd, 1),                   // idx 1
			new Rm2kMap.EventCommand(EventInterpreter.EndLoop),       // idx 2
		};

		var interpreter = new EventInterpreter(state, 1, commands);

		// Cycle of 2 frames: body command / EndLoop-jump to first body command.
		for (var index = 0; index < 8; index++)
		{
			AssertTrue(interpreter.ExecuteFrame(), $"looping frame {index}");
		}
		AssertEq(state.Variables[0], 4, "four loop iterations after 8 frames");
	}

	public void Test_MalformedParametersSkipsSafely()
	{
		var state = new GameSimulationState();
		var commands = new List<Rm2kMap.EventCommand>
		{
			new Rm2kMap.EventCommand(EventInterpreter.ControlSwitches, new List<int> { 1, 2 }),
			new Rm2kMap.EventCommand(EventInterpreter.ControlVars),
			new Rm2kMap.EventCommand(EventInterpreter.Teleport, new List<int> { 9 }),
			new Rm2kMap.EventCommand(EventInterpreter.End),
		};

		var interpreter = new EventInterpreter(state, 1, commands);

		for (var index = 0; index < 3; index++)
		{
			AssertTrue(interpreter.ExecuteFrame(), $"malformed frame {index} skipped without crash");
		}
		AssertFalse(state.IsTransferPending, "no transfer from malformed payload");
		AssertEq(state.Switches.Count, 0, "no switch changes from malformed payload");
		AssertEq(state.Variables.Count, 0, "no variable changes from malformed payload");
		AssertEq(state.Diagnostics.Count, 3, "one diagnostic per malformed command");
	}

	public void Test_ConditionalBranchSwitchConditions()
	{
		var state = new GameSimulationState();
		var commands = new List<Rm2kMap.EventCommand>
		{
			SwitchCmd(1, 1, EventInterpreter.SwitchModeOn),
			Cmd(EventInterpreter.ConditionalBranch, 0, 1, 0), // switch 1 is ON -> true
			VarOp(10, EventInterpreter.VarOpAdd, 1),
			Cmd(EventInterpreter.ElseBranch),
			VarOp(20, EventInterpreter.VarOpAdd, 1),
			Cmd(EventInterpreter.EndBranch),
			Cmd(EventInterpreter.ConditionalBranch, 0, 2, 1), // switch 2 is OFF -> true
			VarOp(11, EventInterpreter.VarOpAdd, 1),
			Cmd(EventInterpreter.ElseBranch),
			VarOp(21, EventInterpreter.VarOpAdd, 1),
			Cmd(EventInterpreter.EndBranch),
			Cmd(EventInterpreter.End),
		};
		var interpreter = new EventInterpreter(state, 1, commands);

		RunBounded(interpreter, 64);

		AssertEq(state.Variables[9], 1, "switch-ON condition took then-body");
		AssertEq(state.Variables[19], 0, "then-body else block skipped");
		AssertEq(state.Variables[10], 1, "switch-OFF condition took then-body");
		AssertEq(state.Variables[20], 0, "second else block skipped");
		AssertEq(state.Diagnostics.Count, 0, "no diagnostics on supported conditions");
	}

	public void Test_ConditionalBranchFalseConditionRunsElse()
	{
		var state = new GameSimulationState();
		var commands = new List<Rm2kMap.EventCommand>
		{
			Cmd(EventInterpreter.ConditionalBranch, 0, 3, 0), // switch 3 is ON -> false (unset)
			VarOp(10, EventInterpreter.VarOpAdd, 1),
			Cmd(EventInterpreter.ElseBranch),
			VarOp(20, EventInterpreter.VarOpAdd, 1),
			Cmd(EventInterpreter.EndBranch),
			Cmd(EventInterpreter.End),
		};
		var interpreter = new EventInterpreter(state, 1, commands);

		RunBounded(interpreter, 64);

		AssertEq(state.Variables[9], 0, "then-body skipped");
		AssertEq(state.Variables[19], 1, "else body executed");
		AssertEq(state.Diagnostics.Count, 0, "no diagnostics");
	}

	public void Test_ConditionalBranchVariableComparisons()
	{
		var state = new GameSimulationState();
		var commands = new List<Rm2kMap.EventCommand>
		{
			VarOp(5, EventInterpreter.VarOpSet, 10),
			Cmd(EventInterpreter.ConditionalBranch, 1, 5, 0, 10, EventInterpreter.BranchOpEqual),
			VarOp(20, EventInterpreter.VarOpAdd, 1),
			Cmd(EventInterpreter.EndBranch),
			Cmd(EventInterpreter.ConditionalBranch, 1, 5, 0, 11, EventInterpreter.BranchOpGreaterOrEqual),
			VarOp(21, EventInterpreter.VarOpAdd, 1),
			Cmd(EventInterpreter.ElseBranch),
			VarOp(22, EventInterpreter.VarOpAdd, 1),
			Cmd(EventInterpreter.EndBranch),
			Cmd(EventInterpreter.ConditionalBranch, 1, 5, 0, 10, EventInterpreter.BranchOpNotEqual),
			VarOp(23, EventInterpreter.VarOpAdd, 1),
			Cmd(EventInterpreter.ElseBranch),
			VarOp(24, EventInterpreter.VarOpAdd, 1),
			Cmd(EventInterpreter.EndBranch),
			Cmd(EventInterpreter.End),
		};
		var interpreter = new EventInterpreter(state, 1, commands);

		RunBounded(interpreter, 64);

		AssertEq(state.Variables[4], 10, "variable seeded");
		AssertEq(state.Variables[19], 1, "== comparison matched");
		AssertEq(state.Variables[20], 0, ">= false skipped then-body");
		AssertEq(state.Variables[21], 1, ">= false ran else");
		AssertEq(state.Variables[22], 0, "!= false skipped then-body");
		AssertEq(state.Variables[23], 1, "!= false ran else");
	}

	public void Test_ConditionalBranchVariableOperandAndNesting()
	{
		var state = new GameSimulationState();
		var commands = new List<Rm2kMap.EventCommand>
		{
			VarOp(5, EventInterpreter.VarOpSet, 7),
			VarOp(6, EventInterpreter.VarOpSet, 7),
			Cmd(EventInterpreter.ConditionalBranch, 1, 5, 1, 6, EventInterpreter.BranchOpEqual), // var5 == var6
			Cmd(EventInterpreter.ConditionalBranch, 1, 6, 0, 99, EventInterpreter.BranchOpLess), // nested: var6 < 99
			VarOp(30, EventInterpreter.VarOpAdd, 1),
			Cmd(EventInterpreter.EndBranch),
			Cmd(EventInterpreter.ElseBranch), // belongs to OUTER branch
			VarOp(31, EventInterpreter.VarOpAdd, 1),
			Cmd(EventInterpreter.EndBranch),
			Cmd(EventInterpreter.End),
		};
		var interpreter = new EventInterpreter(state, 1, commands);

		RunBounded(interpreter, 64);

		AssertEq(state.Variables[29], 1, "nested then-bodies both ran");
		AssertEq(state.Variables[30], 0, "outer else not taken when inner branch consumed its own end");
	}

	public void Test_ConditionalBranchUnsupportedTypeDiagnosed()
	{
		var state = new GameSimulationState();
		var commands = new List<Rm2kMap.EventCommand>
		{
			Cmd(EventInterpreter.ConditionalBranch, 3, 100, 1), // gold condition: unsupported
			VarOp(10, EventInterpreter.VarOpAdd, 1),
			Cmd(EventInterpreter.ElseBranch),
			VarOp(20, EventInterpreter.VarOpAdd, 1),
			Cmd(EventInterpreter.EndBranch),
			Cmd(EventInterpreter.End),
		};
		var interpreter = new EventInterpreter(state, 1, commands);

		RunBounded(interpreter, 64);

		AssertEq(state.Variables[9], 0, "unsupported type skips then-body");
		AssertEq(state.Variables[19], 1, "unsupported type takes else path");
		AssertTrue(
			DiagnosticsMention(state.Diagnostics, "not supported"),
			"diagnostic names the unsupported condition type");
	}

	private static bool DiagnosticsMention(Godot.Collections.Array<string> pDiagnostics, string pFragment)
	{
		foreach (var diagnostic in pDiagnostics)
		{
			if (diagnostic.Contains(pFragment, StringComparison.Ordinal))
			{
				return true;
			}
		}
		return false;
	}

	private static void RunBounded(EventInterpreter pInterpreter, int pMaxFrames)
	{
		for (var frame = 0; frame < pMaxFrames && pInterpreter.ExecuteFrame(); frame++)
		{
		}
	}

	private static Rm2kMap.EventCommand Cmd(int pCode, params int[] pParameters)
	{
		return new Rm2kMap.EventCommand(pCode, new List<int>(pParameters));
	}

	private static Rm2kMap.EventCommand SwitchCmd(int pStart, int pEnd, int pMode)
	{
		return new Rm2kMap.EventCommand(
			EventInterpreter.ControlSwitches,
			new List<int> { pStart, pEnd, 0, pMode });
	}

	private static Rm2kMap.EventCommand VarOp(int pTarget, int pOp, int pOperand)
	{
		return new Rm2kMap.EventCommand(
			EventInterpreter.ControlVars,
			new List<int> { pTarget, pTarget, 0, pOp, EventInterpreter.VarOperandConstant, pOperand });
	}
}
