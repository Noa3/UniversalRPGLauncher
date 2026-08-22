using Godot;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Rm2k.Interpreter;
using UniversalRPG.Tests.Framework;
using System.Collections.Generic;

namespace UniversalRPG.Tests.Core;

public partial class TestEventInterpreter : TestBase
{
	public void Test_NewInterpreterInitialState()
	{
		var state = new GameSimulationState();
		var emptyPages = new List<Godot.Collections.Dictionary>();
		var emptyCommands = new List<Godot.Collections.Dictionary>();

		var interpreter = new EventInterpreter(state, 1, 0, emptyPages, emptyCommands);

		AssertTrue(interpreter.IsRunning);
		AssertEq(interpreter.EventId, 1);
		AssertEq(interpreter.PageId, 0);
		AssertEq(interpreter.CurrentCommandIndex, 0);
	}

	public void Test_EmptyCommandsStopsImmediately()
	{
		var state = new GameSimulationState();
		var emptyPages = new List<Godot.Collections.Dictionary>();
		var emptyCommands = new List<Godot.Collections.Dictionary>();

		var interpreter = new EventInterpreter(state, 1, 0, emptyPages, emptyCommands);

		var result = interpreter.ExecuteFrame();
		AssertFalse(result);
		AssertFalse(interpreter.IsRunning);
	}

	public void Test_EndCommandStopsExecution()
	{
		var state = new GameSimulationState();
		var emptyPages = new List<Godot.Collections.Dictionary>();
		var commands = new List<Godot.Collections.Dictionary>
		{
			new Godot.Collections.Dictionary { { "code", 0L } }
		};

		var interpreter = new EventInterpreter(state, 1, 0, emptyPages, commands);

		var result = interpreter.ExecuteFrame();
		AssertFalse(result);
		AssertFalse(interpreter.IsRunning);
	}

	public void Test_MultipleCommandsAdvanceIndex()
	{
		var state = new GameSimulationState();
		var emptyPages = new List<Godot.Collections.Dictionary>();
		var commands = new List<Godot.Collections.Dictionary>
		{
			new Godot.Collections.Dictionary { { "code", 0L } },
			new Godot.Collections.Dictionary { { "code", 0L } },
			new Godot.Collections.Dictionary { { "code", 0L } }
		};

		var interpreter = new EventInterpreter(state, 1, 0, emptyPages, commands);

		// First command: End (0) -> stops execution
		AssertEq(interpreter.CurrentCommandIndex, 0);
		var result = interpreter.ExecuteFrame();
		AssertFalse(result);
		AssertEq(interpreter.CurrentCommandIndex, 0);
	}

	public void Test_ConstantValuesAreCorrect()
	{
		AssertEq(EventInterpreter.CmdShowMessage, 101);
		AssertEq(EventInterpreter.ShowChoice, 102);
		AssertEq(EventInterpreter.InputNumber, 103);
		AssertEq(EventInterpreter.If, 111);
		AssertEq(EventInterpreter.Else, 112);
		AssertEq(EventInterpreter.EndIf, 113);
		AssertEq(EventInterpreter.Loop, 114);
		AssertEq(EventInterpreter.BreakLoop, 115);
		AssertEq(EventInterpreter.ControlSwitches, 105);
		AssertEq(EventInterpreter.ControlVariables, 106);
		AssertEq(EventInterpreter.TransferPlayer, 107);
		AssertEq(EventInterpreter.Wait, 117);
		AssertEq(EventInterpreter.Comment, 118);
		AssertEq(EventInterpreter.MaxCommandPayloadBytes, 65536);
		AssertEq(EventInterpreter.MaxScriptRecursion, 256);
		AssertEq(EventInterpreter.MaxWaitFrames, 600);
	}

	public void Test_StateReferenceReturned()
	{
		var state = new GameSimulationState();
		var emptyPages = new List<Godot.Collections.Dictionary>();
		var emptyCommands = new List<Godot.Collections.Dictionary>();

		var interpreter = new EventInterpreter(state, 1, 0, emptyPages, emptyCommands);

		AssertEq(interpreter.State, state);
	}

	public void Test_UnknownCommandSkipped()
	{
		var state = new GameSimulationState();
		var emptyPages = new List<Godot.Collections.Dictionary>();
		var commands = new List<Godot.Collections.Dictionary>
		{
			new Godot.Collections.Dictionary { { "code", 999L } },
			new Godot.Collections.Dictionary { { "code", 0L } }
		};

		var interpreter = new EventInterpreter(state, 1, 0, emptyPages, commands);

		// Unknown command (999) should be skipped
		var result = interpreter.ExecuteFrame();
		AssertTrue(result);
		AssertEq(interpreter.CurrentCommandIndex, 1);

		// End command should stop
		result = interpreter.ExecuteFrame();
		AssertFalse(result);
		AssertFalse(interpreter.IsRunning);
	}

	public void Test_WaitFramesRecorded()
	{
		var state = new GameSimulationState();
		var emptyPages = new List<Godot.Collections.Dictionary>();
		var commands = new List<Godot.Collections.Dictionary>
		{
			new Godot.Collections.Dictionary { { "code", EventInterpreter.Wait }, { "parameters", new byte[] { 10 } } },
			new Godot.Collections.Dictionary { { "code", 0L } }
		};

		var interpreter = new EventInterpreter(state, 1, 0, emptyPages, commands);

		var result = interpreter.ExecuteFrame();
		AssertTrue(result);
		AssertTrue(state.Diagnostics.Count > 0);
		AssertTrue(state.Diagnostics[0].Contains("Wait 10 frames"));

		result = interpreter.ExecuteFrame();
		AssertFalse(result);
	}

	public void Test_ShowMessageRecorded()
	{
		var state = new GameSimulationState();
		var emptyPages = new List<Godot.Collections.Dictionary>();
		var commands = new List<Godot.Collections.Dictionary>
		{
			new Godot.Collections.Dictionary { { "code", EventInterpreter.CmdShowMessage }, { "parameters", new byte[] { (byte)'H', (byte)'e', (byte)'l', (byte)'l', (byte)'o' } } },
			new Godot.Collections.Dictionary { { "code", 0L } }
		};

		var interpreter = new EventInterpreter(state, 1, 0, emptyPages, commands);

		var result = interpreter.ExecuteFrame();
		AssertTrue(result);
		AssertTrue(state.Diagnostics.Count > 0);
		AssertTrue(state.Diagnostics[0].Contains("Show message"));

		result = interpreter.ExecuteFrame();
		AssertFalse(result);
	}

	private static byte[] Int32(int pValue)
	{
		return new byte[]
		{
			(byte)(pValue & 0xFF),
			(byte)((pValue >> 8) & 0xFF),
			(byte)((pValue >> 16) & 0xFF),
			(byte)((pValue >> 24) & 0xFF),
		};
	}

	public void Test_ControlSwitchesSetsRangeOnAndOff()
	{
		var state = new GameSimulationState();
		var emptyPages = new List<Godot.Collections.Dictionary>();
		var onParams = new List<byte>();
		onParams.AddRange(Int32(2));
		onParams.AddRange(Int32(4));
		onParams.Add(1);
		var offParams = new List<byte>();
		offParams.AddRange(Int32(3));
		offParams.AddRange(Int32(3));
		offParams.Add(0);
		var commands = new List<Godot.Collections.Dictionary>
		{
			new Godot.Collections.Dictionary { { "code", EventInterpreter.ControlSwitches }, { "parameters", onParams.ToArray() } },
			new Godot.Collections.Dictionary { { "code", EventInterpreter.ControlSwitches }, { "parameters", offParams.ToArray() } },
			new Godot.Collections.Dictionary { { "code", 0L } }
		};

		var interpreter = new EventInterpreter(state, 7, 0, emptyPages, commands);

		AssertTrue(interpreter.ExecuteFrame());
		AssertEq(state.Switches.Count, 4, "switch array padded to range end");
		AssertFalse(state.Switches[0], "switch 1 untouched default");
		AssertTrue(state.Switches[1], "switch 2 ON");
		AssertTrue(state.Switches[2], "switch 3 ON");
		AssertTrue(state.Switches[3], "switch 4 ON");

		AssertTrue(interpreter.ExecuteFrame());
		AssertFalse(state.Switches[2], "switch 3 OFF after second command");
		AssertTrue(state.Switches[3], "switch 4 still ON");

		AssertTrue(state.Diagnostics[0].Contains("Switches 2-4 -> ON"));
	}

	public void Test_ControlSwitchesRejectsInvalidRange()
	{
		var state = new GameSimulationState();
		var emptyPages = new List<Godot.Collections.Dictionary>();
		var badParams = new List<byte>();
		badParams.AddRange(Int32(5));
		badParams.AddRange(Int32(2));
		badParams.Add(1);
		var commands = new List<Godot.Collections.Dictionary>
		{
			new Godot.Collections.Dictionary { { "code", EventInterpreter.ControlSwitches }, { "parameters", badParams.ToArray() } },
			new Godot.Collections.Dictionary { { "code", 0L } }
		};

		var interpreter = new EventInterpreter(state, 1, 0, emptyPages, commands);

		AssertTrue(interpreter.ExecuteFrame());
		AssertEq(state.Switches.Count, 0, "no switches created for invalid range");
		AssertTrue(state.Diagnostics[0].Contains("invalid range"));
	}

	public void Test_ControlVariablesArithmeticOperations()
	{
		var state = new GameSimulationState();
		state.Variables.Add(10);
		var emptyPages = new List<Godot.Collections.Dictionary>();

		Godot.Collections.Dictionary VarOp(int pOp, int pOperand)
		{
			var parameters = new List<byte>();
			parameters.AddRange(Int32(1));
			parameters.AddRange(Int32(1));
			parameters.Add((byte)pOp);
			parameters.Add(0);
			parameters.AddRange(Int32(pOperand));
			return new Godot.Collections.Dictionary
			{
				{ "code", EventInterpreter.ControlVariables },
				{ "parameters", parameters.ToArray() },
			};
		}

		var commands = new List<Godot.Collections.Dictionary>
		{
			VarOp(1, 5),   // add -> 15
			VarOp(2, 20),  // sub -> -5
			VarOp(3, 4),   // mul -> -20
			VarOp(4, 6),   // div -> -3
			VarOp(5, 2),   // mod -> -1
			VarOp(0, 42),  // set -> 42
			new Godot.Collections.Dictionary { { "code", 0L } }
		};

		var interpreter = new EventInterpreter(state, 1, 0, emptyPages, commands);

		for (var index = 0; index < commands.Count - 1; index++)
		{
			AssertTrue(interpreter.ExecuteFrame(), $"frame {index} executed");
		}
		AssertEq(state.Variables[0], 42, "final variable value after ops");
		AssertFalse(interpreter.ExecuteFrame(), "event ends after final End command");
	}

	public void Test_ControlVariablesDivisionByZeroSkips()
	{
		var state = new GameSimulationState();
		state.Variables.Add(10);
		var emptyPages = new List<Godot.Collections.Dictionary>();
		var parameters = new List<byte>();
		parameters.AddRange(Int32(1));
		parameters.AddRange(Int32(1));
		parameters.Add(4);
		parameters.Add(0);
		parameters.AddRange(Int32(0));
		var commands = new List<Godot.Collections.Dictionary>
		{
			new Godot.Collections.Dictionary { { "code", EventInterpreter.ControlVariables }, { "parameters", parameters.ToArray() } },
			new Godot.Collections.Dictionary { { "code", 0L } }
		};

		var interpreter = new EventInterpreter(state, 1, 0, emptyPages, commands);

		AssertTrue(interpreter.ExecuteFrame());
		AssertEq(state.Variables[0], 10, "value unchanged on division by zero");
		AssertTrue(state.Diagnostics[0].Contains("division by zero"));
	}

	public void Test_TransferPlayerSetsPendingTransfer()
	{
		var state = new GameSimulationState();
		var emptyPages = new List<Godot.Collections.Dictionary>();
		var parameters = new List<byte>();
		parameters.AddRange(Int32(12));
		parameters.AddRange(Int32(30));
		parameters.AddRange(Int32(44));
		var commands = new List<Godot.Collections.Dictionary>
		{
			new Godot.Collections.Dictionary { { "code", EventInterpreter.TransferPlayer }, { "parameters", parameters.ToArray() } },
			new Godot.Collections.Dictionary { { "code", 0L } }
		};

		var interpreter = new EventInterpreter(state, 3, 0, emptyPages, commands);

		AssertTrue(interpreter.ExecuteFrame());
		AssertTrue(state.IsTransferPending, "transfer pending flag");
		AssertEq(state.PendingMapId, 12, "pending map id");
		AssertEq(state.PendingX, 30, "pending x");
		AssertEq(state.PendingY, 44, "pending y");
		AssertTrue(state.Diagnostics[0].Contains("Transfer pending"));
	}

	public void Test_MalformedParametersSkipsSafely()
	{
		var state = new GameSimulationState();
		var emptyPages = new List<Godot.Collections.Dictionary>();
		var commands = new List<Godot.Collections.Dictionary>
		{
			new Godot.Collections.Dictionary { { "code", EventInterpreter.ControlSwitches }, { "parameters", new byte[] { 1, 2 } } },
			new Godot.Collections.Dictionary { { "code", EventInterpreter.ControlVariables }, { "parameters", System.Array.Empty<byte>() } },
			new Godot.Collections.Dictionary { { "code", EventInterpreter.TransferPlayer }, { "parameters", new byte[] { 9 } } },
			new Godot.Collections.Dictionary { { "code", 0L } }
		};

		var interpreter = new EventInterpreter(state, 1, 0, emptyPages, commands);

		for (var index = 0; index < 3; index++)
		{
			AssertTrue(interpreter.ExecuteFrame(), $"malformed frame {index} skipped without crash");
		}
		AssertFalse(state.IsTransferPending, "no transfer from malformed payload");
		AssertEq(state.Switches.Count, 0, "no switch changes from malformed payload");
		AssertEq(state.Variables.Count, 0, "no variable changes from malformed payload");
		AssertEq(state.Diagnostics.Count, 3, "one diagnostic per malformed command");
	}
}
