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
}
