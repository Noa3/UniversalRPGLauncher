using System;
using System.Collections.Generic;
using UniversalRPG.Rm2k.Simulation;

namespace UniversalRPG.Rm2k.Interpreter;

/// <summary>
/// Event command interpreter for RM2K/2003 games.
/// Executes commands deterministically against a GameSimulationState.
/// No JavaScript, DLL, or native execution.
///
/// Command codes and parameter layouts are verified against the generated
/// liblcf table (src/generated/lcf/rpg/eventcommand.h) and EasyRPG Player's
/// interpreter implementation (game_interpreter.cpp, game_interpreter_map.cpp).
/// </summary>
public sealed class EventInterpreter
{
	public const int MaxScriptRecursion = 256;
	public const int MaxWaitFrames = 600; // 10 seconds at 60fps

	// Verified RM2K/2003 event command codes (liblcf lcf::rpg::Cmd).
	public const int End = 0;
	public const int ShowMessage = 10110;
	public const int ShowChoice = 10140;
	public const int InputNumber = 10150;
	public const int ControlSwitches = 10210;
	public const int ControlVars = 10220;
	public const int Teleport = 10810;
	public const int Wait = 11410;
	public const int ConditionalBranch = 12010;
	public const int Loop = 12210;
	public const int BreakLoop = 12220;
	public const int Comment = 12410;
	public const int ShowMessage2 = 20110; // message continuation line
	public const int ElseBranch = 22010;
	public const int EndBranch = 22011;
	public const int EndLoop = 22210;
	public const int Comment2 = 22410; // comment continuation line

	// ControlSwitches mode values (EasyRPG CommandControlSwitches).
	public const int SwitchModeOn = 0;
	public const int SwitchModeOff = 1;
	public const int SwitchModeFlip = 2;

	// ControlVars operation values (EasyRPG CommandControlVariables).
	public const int VarOpSet = 0;
	public const int VarOpAdd = 1;
	public const int VarOpSub = 2;
	public const int VarOpMul = 3;
	public const int VarOpDiv = 4;
	public const int VarOpMod = 5;

	// ControlVars operand types (subset implemented so far).
	public const int VarOperandConstant = 0;
	public const int VarOperandVariable = 1;

	private readonly GameSimulationState _state;
	private readonly int _eventId;
	private readonly IReadOnlyList<Rm2kMap.EventCommand> _commands;
	private readonly Stack<int> _loopStack = new();
	private int _commandIndex;
	private int _waitFramesRemaining;

	public EventInterpreter(GameSimulationState state, int eventId,
		IReadOnlyList<Rm2kMap.EventCommand> commands)
	{
		_state = state ?? throw new ArgumentNullException(nameof(state));
		_eventId = eventId;
		_commands = commands ?? throw new ArgumentNullException(nameof(commands));
		_commandIndex = 0;
	}

	public GameSimulationState State => _state;

	public int EventId => _eventId;
	public int CurrentCommandIndex => _commandIndex;
	public int WaitFramesRemaining => _waitFramesRemaining;
	public bool IsRunning { get; private set; } = true;

	/// <summary>
	/// Execute one frame of this event's commands.
	/// Returns true if the event should continue running.
	/// Active waits consume frames before the next command executes.
	/// </summary>
	public bool ExecuteFrame()
	{
		if (!IsRunning)
		{
			return false;
		}

		if (_waitFramesRemaining > 0)
		{
			_waitFramesRemaining--;
			return true;
		}

		if (_commandIndex >= _commands.Count)
		{
			IsRunning = false;
			return false;
		}

		var cmd = _commands[_commandIndex];

		switch (cmd.Code)
		{
			case End:
				IsRunning = false;
				return false;

			case ShowMessage:
			case Comment:
				ExecuteMessageOrComment(cmd);
				return Advance();

			case ShowMessage2:
			case Comment2:
				// Continuation line without a preceding ShowMessage/Comment: skip.
				return Advance();

			case Wait:
				ExecuteWait(cmd);
				return Advance();

			case ControlSwitches:
				ExecuteControlSwitches(cmd);
				return Advance();

			case ControlVars:
				ExecuteControlVars(cmd);
				return Advance();

			case Teleport:
				ExecuteTeleport(cmd);
				return Advance();

			case ConditionalBranch:
				ExecuteConditionalBranch();
				return Advance();

			case ElseBranch:
				ExecuteElseBranch();
				return Advance();

			case EndBranch:
				ExecuteEndBranch();
				return Advance();

			case Loop:
				_loopStack.Push(_commandIndex);
				return Advance();

			case BreakLoop:
				ExecuteBreakLoop();
				return true; // index already moved past the matching EndLoop

			case EndLoop:
				ExecuteEndLoop();
				return true; // index points at the matching Loop command

			default:
				// Unknown or not-yet-implemented command: skip safely.
				return Advance();
		}
	}

	private bool Advance()
	{
		_commandIndex++;
		return IsRunning;
	}

	private void ExecuteMessageOrComment(Rm2kMap.EventCommand pCmd)
	{
		var kind = pCmd.Code == ShowMessage ? "Show message" : "Comment";
		var text = pCmd.Text;
		// Consume continuation lines (ShowMessage_2 / Comment_2).
		while (_commandIndex + 1 < _commands.Count
			&& (_commands[_commandIndex + 1].Code == (pCmd.Code == ShowMessage ? ShowMessage2 : Comment2)))
		{
			_commandIndex++;
			text += "\n" + _commands[_commandIndex].Text;
		}
		_state.AddDiagnostic($"[Event {_eventId}] {kind}: {Truncate(text)}");
	}

	private static string Truncate(string pText)
	{
		var singleLine = pText.Replace("\n", "\\n");
		return singleLine.Length <= 80 ? singleLine : singleLine[..80];
	}

	private void ExecuteWait(Rm2kMap.EventCommand pCmd)
	{
		// params[0] is a duration in tenths of a second (EasyRPG SetupWait);
		// 0.0 seconds still waits exactly one frame.
		var tenths = Param(pCmd, 0);
		var frames = tenths == 0 ? 1 : checked(tenths * 6);
		if (frames > MaxWaitFrames)
		{
			frames = MaxWaitFrames;
		}
		_waitFramesRemaining = frames;
		_state.AddDiagnostic($"[Event {_eventId}] Wait {frames} frames");
	}

	private void ExecuteControlSwitches(Rm2kMap.EventCommand pCmd)
	{
		if (pCmd.Parameters.Count < 4)
		{
			Malformed("Control switches");
			return;
		}
		var startId = pCmd.Parameters[0];
		var endId = pCmd.Parameters[1];
		var mode = pCmd.Parameters[3];
		if (startId < 1 || endId < startId || endId > GameSimulationState.MaxSwitches)
		{
			_state.AddDiagnostic($"[Event {_eventId}] Control switches: invalid range {startId}-{endId} skipped");
			return;
		}
		if (mode is not (SwitchModeOn or SwitchModeOff or SwitchModeFlip))
		{
			_state.AddDiagnostic($"[Event {_eventId}] Control switches: unknown mode {mode} skipped");
			return;
		}
		for (var id = startId; id <= endId; id++)
		{
			while (_state.Switches.Count < id)
			{
				_state.Switches.Add(false);
			}
			switch (mode)
			{
				case SwitchModeOn:
					_state.Switches[id - 1] = true;
					break;
				case SwitchModeOff:
					_state.Switches[id - 1] = false;
					break;
				default:
					_state.Switches[id - 1] = !_state.Switches[id - 1];
					break;
			}
		}
		var effect = mode switch
		{
			SwitchModeOn => "ON",
			SwitchModeOff => "OFF",
			_ => "FLIP",
		};
		_state.AddDiagnostic($"[Event {_eventId}] Switches {startId}-{endId} -> {effect}");
	}

	private void ExecuteControlVars(Rm2kMap.EventCommand pCmd)
	{
		if (pCmd.Parameters.Count < 6)
		{
			Malformed("Control variables");
			return;
		}
		var startId = pCmd.Parameters[0];
		var endId = pCmd.Parameters[1];
		var targetMode = pCmd.Parameters[2];
		var op = pCmd.Parameters[3];
		var operandType = pCmd.Parameters[4];
		var operandValue = pCmd.Parameters[5];

		if (targetMode != 0)
		{
			_state.AddDiagnostic($"[Event {_eventId}] Control variables: unsupported target mode {targetMode} skipped");
			return;
		}
		if (startId < 1 || endId < startId || endId > GameSimulationState.MaxVariables)
		{
			_state.AddDiagnostic($"[Event {_eventId}] Control variables: invalid range {startId}-{endId} skipped");
			return;
		}
		if (op < VarOpSet || op > VarOpMod)
		{
			_state.AddDiagnostic($"[Event {_eventId}] Control variables: unsupported operation {op} skipped");
			return;
		}
		int operand;
		switch (operandType)
		{
			case VarOperandConstant:
				operand = operandValue;
				break;
			case VarOperandVariable:
				operand = GetVariable(operandValue);
				break;
			default:
				_state.AddDiagnostic($"[Event {_eventId}] Control variables: unsupported operand type {operandType} skipped");
				return;
		}
		if ((op == VarOpDiv || op == VarOpMod) && operand == 0)
		{
			_state.AddDiagnostic($"[Event {_eventId}] Control variables: division by zero skipped");
			return;
		}
		for (var id = startId; id <= endId; id++)
		{
			while (_state.Variables.Count < id)
			{
				_state.Variables.Add(0);
			}
			var index = id - 1;
			var current = _state.Variables[index];
			_state.Variables[index] = op switch
			{
				VarOpSet => operand,
				VarOpAdd => current + operand,
				VarOpSub => current - operand,
				VarOpMul => current * operand,
				VarOpDiv => current / operand,
				_ => current % operand,
			};
		}
		_state.AddDiagnostic($"[Event {_eventId}] Variables {startId}-{endId} <- op {op} {operand}");
	}

	private void ExecuteTeleport(Rm2kMap.EventCommand pCmd)
	{
		// Code 10810 "Place Hero": [0]=map id, [1]=x, [2]=y, optional [3]=facing (2k3).
		if (pCmd.Parameters.Count < 3)
		{
			Malformed("Transfer player");
			return;
		}
		var mapId = pCmd.Parameters[0];
		var x = pCmd.Parameters[1];
		var y = pCmd.Parameters[2];
		if (mapId < 0 || mapId > GameSimulationState.MaxMapId || x < 0 || y < 0)
		{
			_state.AddDiagnostic($"[Event {_eventId}] Transfer player: invalid target ({mapId}, {x}, {y}) skipped");
			return;
		}
		_state.PendingMapId = mapId;
		_state.PendingX = x;
		_state.PendingY = y;
		_state.IsTransferPending = true;
		_state.AddDiagnostic($"[Event {_eventId}] Transfer pending -> map {mapId} at ({x}, {y})");
	}

	private void ExecuteConditionalBranch()
	{
		// Condition decoding (switch/variable/actor/timer comparisons) is a
		// separate slice; for now the true-branch always executes.
	}

	private void ExecuteElseBranch()
	{
		// The condition evaluation above currently never skips blocks, so the
		// else branch must not run yet. Tracked with full branch semantics.
		_commandIndex = FindMatchingBranch(_commandIndex, EndBranch) ?? _commandIndex;
	}

	private void ExecuteEndBranch()
	{
		// Structured block end; nothing to do until conditions are decoded.
	}

	private void ExecuteBreakLoop()
	{
		_loopStack.Clear();
		var endLoop = FindMatchingBranch(_commandIndex, EndLoop);
		if (endLoop.HasValue)
		{
			_commandIndex = endLoop.Value + 1;
		}
		else
		{
			// No matching EndLoop (RPG_RT tolerates this): run to the end.
			_commandIndex = _commands.Count;
		}
	}

	private void ExecuteEndLoop()
	{
		if (_loopStack.Count > 0)
		{
			// Jump to the first body command; the Loop entry stays on the
			// stack so nesting depth stays bounded without re-pushing.
			_commandIndex = _loopStack.Peek() + 1;
		}
		else
		{
			// End without a matching Loop: skip safely.
			_commandIndex++;
		}
	}

	private int? FindMatchingBranch(int pFrom, int pCode)
	{
		for (var i = pFrom + 1; i < _commands.Count; i++)
		{
			if (_commands[i].Code == pCode)
			{
				return i;
			}
		}
		return null;
	}

	private int GetVariable(int pId)
	{
		if (pId < 1 || pId > _state.Variables.Count)
		{
			return 0;
		}
		return _state.Variables[pId - 1];
	}

	private static int Param(Rm2kMap.EventCommand pCmd, int pIndex)
	{
		return pIndex < pCmd.Parameters.Count ? pCmd.Parameters[pIndex] : 0;
	}

	private void Malformed(string pCommand)
	{
		_state.AddDiagnostic($"[Event {_eventId}] {pCommand}: malformed parameters skipped");
	}
}
