using System;
using System.Collections.Generic;
using Godot;
using UniversalRPG.Rm2k.Simulation;

namespace UniversalRPG.Rm2k.Interpreter;

/// <summary>
/// Event command interpreter for RM2K/2003 games.
/// Executes commands deterministically against a GameSimulationState.
/// No JavaScript, DLL, or native execution.
/// </summary>
public sealed class EventInterpreter
{
	public const int MaxCommandPayloadBytes = 65536;
	public const int MaxScriptRecursion = 256;
	public const int MaxWaitFrames = 600; // 10 seconds at 60fps

	// Command IDs for the first slice
	public const int CmdShowMessage = 101;
	public const int ShowChoice = 102;
	public const int InputNumber = 103;
	public const int WhenDecide = 104;
	public const int If = 111;
	public const int Else = 112;
	public const int EndIf = 113;
	public const int Loop = 114;
	public const int BreakLoop = 115;
	public const int Wait = 117;
	public const int Comment = 118;

	private readonly GameSimulationState _state;
	private readonly int _eventId;
	private readonly int _pageId;
	private readonly IReadOnlyList<Godot.Collections.Dictionary> _pages;
	private readonly IReadOnlyList<Godot.Collections.Dictionary> _commands;
	private int _commandIndex;
	private int _loopStackDepth;
	private bool _shouldBreak;
	private int _ifDepth;
	private bool _skipBlock;

	public EventInterpreter(GameSimulationState state, int eventId, int pageId,
		IReadOnlyList<Godot.Collections.Dictionary> pages,
		IReadOnlyList<Godot.Collections.Dictionary> commands)
	{
		_state = state;
		_eventId = eventId;
		_pageId = pageId;
		_pages = pages;
		_commands = commands;
		_commandIndex = 0;
		_loopStackDepth = 0;
		_shouldBreak = false;
		_ifDepth = 0;
		_skipBlock = false;
	}

	public GameSimulationState State => _state;

	public int EventId => _eventId;
	public int PageId => _pageId;
	public int CurrentCommandIndex => _commandIndex;
	public bool IsRunning { get; private set; } = true;

	/// <summary>
	/// Execute one frame of this event's commands.
	/// Returns true if the event should continue running.
	/// </summary>
	public bool ExecuteFrame()
	{
		if (!IsRunning)
		{
			return false;
		}

		if (_commandIndex >= _commands.Count)
		{
			IsRunning = false;
			return false;
		}

		var cmd = _commands[_commandIndex];
		var cmdId = GetCmdId(cmd);
		var paramsData = GetCmdParams(cmd);

		switch (cmdId)
		{
			case 0: // End
				IsRunning = false;
				return false;

			case CmdShowMessage:
				ExecuteShowMessage(paramsData);
				_commandIndex++;
				return true;

			case Wait:
				ExecuteWait(paramsData);
				_commandIndex++;
				return true;

			case If:
				ExecuteIf(paramsData);
				_commandIndex++;
				return true;

			case Else:
				ExecuteElse();
				_commandIndex++;
				return true;

			case EndIf:
				ExecuteEndIf();
				_commandIndex++;
				return true;

			case Loop:
				ExecuteLoop();
				_commandIndex++;
				return true;

			case BreakLoop:
				ExecuteBreakLoop();
				_commandIndex++;
				return true;

			case 0x69: // Set move route (skip, not implementing movement yet)
				ExecuteSetMoveRoute(paramsData);
				_commandIndex++;
				return true;

			default:
				// Unknown command - skip
				_commandIndex++;
				return true;
		}
	}

	private int GetCmdId(Godot.Collections.Dictionary pCmd)
	{
		if (pCmd.ContainsKey("code"))
		{
			return (int)(long)pCmd["code"];
		}
		if (pCmd.ContainsKey("cmd_id"))
		{
			return (int)(long)pCmd["cmd_id"];
		}
		return 0;
	}

	private byte[] GetCmdParams(Godot.Collections.Dictionary pCmd)
	{
		if (pCmd.ContainsKey("parameters"))
		{
			var raw = pCmd["parameters"];
			var bytes = raw as byte[];
			if (bytes != null)
			{
				return bytes;
			}
			var arr = raw as Godot.Collections.Array;
			if (arr != null)
			{
				var result = new List<byte>();
				foreach (var item in arr)
				{
					var val = Convert.ToByte(item, System.Globalization.CultureInfo.InvariantCulture);
					result.Add(val);
				}
				return result.ToArray();
			}
		}
		return Array.Empty<byte>();
	}

	private void ExecuteShowMessage(byte[] pParams)
	{
		if (pParams.Length == 0)
		{
			return;
		}

		var text = System.Text.Encoding.UTF8.GetString(pParams);
		_state.AddDiagnostic($"[Event {_eventId}] Show message: {text.Substring(0, Math.Min(80, text.Length))}");
	}

	private void ExecuteWait(byte[] pParams)
	{
		if (pParams.Length == 0)
		{
			return;
		}

		// First byte is usually the wait type (0=f, 1=frames, etc.)
		var frames = pParams[0];
		if (frames > 0)
		{
			_state.AddDiagnostic($"[Event {_eventId}] Wait {frames} frames");
		}
	}

	private void ExecuteIf(byte[] pParams)
	{
		_ifDepth++;
		if (_skipBlock)
		{
			_skipBlock = true; // Nested skip
			return;
		}

		// RM2K: If condition = switch/variable check
		// Simplified: always enter block for now
		// Real: would decode params[0] (condition type) and params[1+] (values)
	}

	private void ExecuteElse()
	{
		if (_ifDepth <= 0)
		{
			return;
		}

		// Toggle skip for if/else block
		if (!_skipBlock)
		{
			_skipBlock = true;
		}
	}

	private void ExecuteEndIf()
	{
		if (_ifDepth <= 0)
		{
			return;
		}

		_ifDepth--;
		if (_skipBlock)
		{
			_skipBlock = false;
		}
	}

	private void ExecuteLoop()
	{
		_loopStackDepth++;
		_shouldBreak = false;
	}

	private void ExecuteBreakLoop()
	{
		if (_loopStackDepth > 0)
		{
			_shouldBreak = true;
			_loopStackDepth--;
		}
	}

	private void ExecuteSetMoveRoute(byte[] pParams)
	{
		// Move route is encoded in RM2K format
		// Skip for now - map movement is K-022
	}
}
