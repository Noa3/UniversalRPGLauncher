using System;
using System.Collections.Generic;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Rm2k.Presentation;

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
	public const int MaxGold = 999999;
	public const int MaxItemId = 50000;
	public const int MaxItemCount = 999999;
	public const int GoldOpAdd = 0;
	public const int GoldOpSubtract = 1;
	public const int ItemOpAdd = 0;
	public const int ItemOpSubtract = 1;
	public const int ItemIdConstant = 0;
	public const int ItemIdVariable = 1;
	public const int PartyOpAdd = 0;
	public const int PartyOpRemove = 1;
	public const int ActorIdConstant = 0;
	public const int ActorIdVariable = 1;

	// Verified RM2K/2003 event command codes (liblcf lcf::rpg::Cmd).
	public const int End = 10;
	public const int ShowMessage = 10110;
	public const int ShowChoice = 10140;
	public const int InputNumber = 10150;
	public const int ChangeGold = 10310;
	public const int ChangeItems = 10320;
	public const int ChangePartyMembers = 10330;
	public const int ChangeExp = 10410;
	public const int ChangeLevel = 10420;
	public const int ControlSwitches = 10210;
	public const int ControlVars = 10220;
	public const int Teleport = 10810;
	public const int Wait = 11410;
	public const int ConditionalBranch = 12010;
	public const int Loop = 12210;
	public const int BreakLoop = 12220;
	public const int Comment = 12410;
	public const int ChangeHeroName = 10610;
	public const int FlashScreen = 11040;
	public const int ShakeScreen = 11050;
	public const int WeatherEffects = 11070;
	public const int EndEventProcessing = 12310;
	public const int CallEvent = 12330;
	public const int EraseEvent = 12320;
	public const int ChangeEventLocation = 10860;

	// CallEvent target kinds (EasyRPG CommandCallEvent).
	public const int CallTargetCommonEvent = 0;
	public const int CallTargetMapEvent = 1;
	public const int ShowMessage2 = 20110; // message continuation line
	// liblcf lcf::rpg::Cmd::ShowMessage_2 is 20110 and the *first* line of a
	// message is inline in 10110; **1009 is the bare continuation line of the
	// same message**, and both games use it. A first draft believed 20110 was
	// the only continuation and therefore dropped every 1009 in the fixtures.
	public const int ShowMessageLine = 1009;
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

	// TargetEvalMode (EasyRPG Game_Interpreter_Shared): lvalue form stored in
	// ControlSwitches/ControlVariables parameters[0]. Indirect and expression
	// modes are patch-only and stay fail-closed.
	public const int TargetEvalSingle = 0;
	public const int TargetEvalRange = 1;
	public const int TargetEvalIndirectSingle = 2;
	public const int TargetEvalIndirectRange = 3;
	public const int TargetEvalExpression = 4;

	// ValueEvalMode (EasyRPG Game_Interpreter_Shared): rvalue form stored in
	// ControlVariables parameters[4].
	public const int VarOperandVariableIndirect = 2;

	// GetActors modes (EasyRPG Game_Interpreter::GetActors).
	public const int ActorSelectParty = 0;
	public const int ActorSelectHero = 1;
	public const int ActorSelectVariableHero = 2;

	// OperateValue operations used by ChangeExp/ChangeLevel.
	public const int ActorValueAdd = 0;
	public const int ActorValueSubtract = 1;

	// Screen effect subcommands (RPG2K3 extension of FlashScreen/ShakeScreen).
	public const int FlashSubOnce = 0;
	public const int FlashSubBegin = 1;
	public const int FlashSubEnd = 2;
	public const int ShakeSubOnce = 0;
	public const int ShakeSubBegin = 1;
	public const int ShakeSubEnd = 2;
	public const int EventInterpreterMaxTenths = MaxWaitFrames / 6;

	// ConditionalBranch condition types (EasyRPG CommandConditionalBranch).
	public const int ConditionSwitch = 0;
	public const int ConditionVariable = 1;

	// ConditionalBranch comparison operators (EasyRPG CheckOperator).
	public const int BranchOpEqual = 0;
	public const int BranchOpGreaterOrEqual = 1;
	public const int BranchOpLessOrEqual = 2;
	public const int BranchOpGreater = 3;
	public const int BranchOpLess = 4;
	public const int BranchOpNotEqual = 5;

	private readonly GameSimulationState _state;
	private readonly int _eventId;
	private IReadOnlyList<Rm2kMap.EventCommand> _commands;
	private readonly Stack<int> _loopStack = new();
	private readonly Stack<CallFrame> _callStack = new();
	private readonly Func<int, int, IReadOnlyList<Rm2kMap.EventCommand>?>? _eventCommandResolver;
	private readonly Func<int, int, int, int, bool>? _eventLocationSetter;
	private readonly Func<int, bool>? _eventDeactivator;
	private readonly PresentationState? _presentation;
	private int _commandIndex;
	private int _waitFramesRemaining;

	/// <summary>Suspended caller state for a bounded nested CallEvent.</summary>
	private sealed class CallFrame
	{
		public CallFrame(IReadOnlyList<Rm2kMap.EventCommand> pCommands, int pReturnIndex, int pLoopDepth)
		{
			Commands = pCommands;
			ReturnIndex = pReturnIndex;
			LoopDepth = pLoopDepth;
		}

		public IReadOnlyList<Rm2kMap.EventCommand> Commands { get; }
		public int ReturnIndex { get; }
		public int LoopDepth { get; }
	}

	public EventInterpreter(GameSimulationState state, int eventId,
		IReadOnlyList<Rm2kMap.EventCommand> commands, PresentationState? presentation = null,
		Func<int, int, IReadOnlyList<Rm2kMap.EventCommand>?>? eventCommandResolver = null,
		Func<int, int, int, int, bool>? eventLocationSetter = null,
		Func<int, bool>? eventDeactivator = null)
	{
		_state = state ?? throw new ArgumentNullException(nameof(state));
		_eventId = eventId;
		_commands = commands ?? throw new ArgumentNullException(nameof(commands));
		_presentation = presentation;
		_eventCommandResolver = eventCommandResolver;
		_eventLocationSetter = eventLocationSetter;
		_eventDeactivator = eventDeactivator;
		_commandIndex = 0;
	}

	public GameSimulationState State => _state;

	public int EventId => _eventId;
	public int CurrentCommandIndex => _commandIndex;
	public int WaitFramesRemaining => _waitFramesRemaining;
	public int CallDepth => _callStack.Count;
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
			return FinishFrame();
		}

		var cmd = _commands[_commandIndex];

		switch (cmd.Code)
		{
			case End:
			case EndEventProcessing:
				// liblcf END terminates the current frame; a nested CallEvent
				// returns to its caller, the base frame stops the event.
				return FinishFrame();

			case ShowMessage:
			case Comment:
				ExecuteMessageOrComment(cmd);
				return Advance();

			case ShowChoice:
				if (!ExecuteShowChoice(cmd)) return true;
				return Advance();

			case InputNumber:
				if (!ExecuteInputNumber(cmd)) return true;
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

			case ChangeGold:
				ExecuteChangeGold(cmd);
				return Advance();

			case ChangeItems:
				ExecuteChangeItems(cmd);
				return Advance();

			case ChangePartyMembers:
				ExecuteChangePartyMembers(cmd);
				return Advance();

			case ControlVars:
				ExecuteControlVars(cmd);
				return Advance();

			case ChangeLevel:
				ExecuteChangeLevelOrExp(cmd, pIsLevel: true);
				return Advance();

			case ChangeExp:
				ExecuteChangeLevelOrExp(cmd, pIsLevel: false);
				return Advance();

			case ChangeHeroName:
				ExecuteChangeHeroName(cmd);
				return Advance();

			case FlashScreen:
				ExecuteFlashScreen(cmd);
				return Advance();

			case ShakeScreen:
				ExecuteShakeScreen(cmd);
				return Advance();

			case WeatherEffects:
				ExecuteWeatherEffects(cmd);
				return Advance();

			case CallEvent:
				return ExecuteCallEvent(cmd);

			case ChangeEventLocation:
				ExecuteChangeEventLocation(cmd);
				return Advance();

			case EraseEvent:
				return ExecuteEraseEvent(cmd);

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
				_state.AddDiagnostic($"[Event {_eventId}] Unsupported RM2K command {cmd.Code} skipped");
				return Advance();
		}
	}

	private bool Advance()
	{
		_commandIndex++;
		return IsRunning;
	}

	/// <summary>
	/// Completes the current command frame. A nested CallEvent resumes its
	/// caller; the base frame stops the interpreter.
	/// </summary>
	private bool FinishFrame()
	{
		if (_callStack.Count == 0)
		{
			IsRunning = false;
			return false;
		}
		var frame = _callStack.Pop();
		_commands = frame.Commands;
		_commandIndex = frame.ReturnIndex;
		TrimLoopStack(frame.LoopDepth);
		return IsRunning;
	}

	private void TrimLoopStack(int pDepth)
	{
		while (_loopStack.Count > pDepth)
		{
			_loopStack.Pop();
		}
	}

	/// <summary>
	/// EasyRPG CommandCallEvent pushes a nested frame. Map events are resolved
	/// through the injected resolver; common events stay diagnostic-only because
	/// the LDB common-event section is not decoded yet.
	/// </summary>
	private bool ExecuteCallEvent(Rm2kMap.EventCommand pCmd)
	{
		// Verified layout: [targetKind, eventId, pageIndex] with minimum width 3.
		if (pCmd.Parameters.Count < 3)
		{
			Malformed("Call event");
			return Advance();
		}
		var targetKind = pCmd.Parameters[0];
		if (targetKind != CallTargetMapEvent)
		{
			_state.AddDiagnostic($"[Event {_eventId}] Call event: target kind {targetKind} is not supported yet");
			return Advance();
		}
		var calledEventId = pCmd.Parameters[1];
		var pageIndex = pCmd.Parameters[2];
		if (calledEventId < 1 || calledEventId > GameSimulationState.MaxActorId)
		{
			_state.AddDiagnostic($"[Event {_eventId}] Call event: invalid event id {calledEventId} skipped");
			return Advance();
		}
		if (pageIndex < 0 || pageIndex > PresentationState.MaxPictures * 1000)
		{
			_state.AddDiagnostic($"[Event {_eventId}] Call event: invalid page index {pageIndex} skipped");
			return Advance();
		}
		if (_eventCommandResolver == null)
		{
			_state.AddDiagnostic($"[Event {_eventId}] Call event: no event command resolver available");
			return Advance();
		}
		if (_callStack.Count >= MaxScriptRecursion)
		{
			_state.AddDiagnostic($"[Event {_eventId}] Call event: recursion limit {MaxScriptRecursion} reached");
			return Advance();
		}
		var called = _eventCommandResolver(calledEventId, pageIndex);
		if (called == null || called.Count == 0)
		{
			_state.AddDiagnostic($"[Event {_eventId}] Call event: event {calledEventId} page {pageIndex} has no commands");
			return Advance();
		}

		_callStack.Push(new CallFrame(_commands, _commandIndex + 1, _loopStack.Count));
		_commands = called;
		_commandIndex = 0;
		_state.AddDiagnostic($"[Event {_eventId}] Call event: running event {calledEventId} page {pageIndex}");
		return true;
	}

	private bool ExecuteShowChoice(Rm2kMap.EventCommand pCmd)
	{
		if (_presentation == null)
		{
			Malformed("Show choice: presentation state unavailable");
			return true;
		}
		if (_presentation.ActiveChoice == null)
		{
			var options = pCmd.Text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
			if (!_presentation.ShowChoices(options))
			{
				Malformed("Show choice: invalid options");
				return true;
			}
			return false;
		}
		if (_presentation.ActiveChoice.SelectedIndex < 0)
		{
			return false;
		}
		_state.AddDiagnostic($"[Event {_eventId}] Choice selected: {_presentation.ActiveChoice.SelectedIndex}");
		_presentation.ClearChoice();
		return true;
	}

	private bool ExecuteInputNumber(Rm2kMap.EventCommand pCmd)
	{
		if (_presentation == null || pCmd.Parameters.Count < 1 || pCmd.Parameters[0] < 1 || pCmd.Parameters[0] > GameSimulationState.MaxVariables)
		{
			Malformed("Input number");
			return true;
		}
		var variableId = pCmd.Parameters[0];
		if (_presentation.PendingInputVariableId != null && _presentation.PendingInputVariableId != variableId)
		{
			_state.AddDiagnostic($"[Event {_eventId}] InputNumber for variable {variableId} paused: pending input for different variable {_presentation.PendingInputVariableId}");
			return false;
		}
		if (_presentation.PendingInputVariableId == null && !_presentation.BeginInput(variableId))
		{
			Malformed("Input number");
			return true;
		}
		if (!_presentation.TryConsumeInput(out _, out var value))
		{
			return false;
		}
		while (_state.Variables.Count < variableId) _state.Variables.Add(0);
		_state.Variables[variableId - 1] = value;
		_state.AddDiagnostic($"[Event {_eventId}] Input number -> variable {variableId}");
		return true;
	}

	private void ExecuteMessageOrComment(Rm2kMap.EventCommand pCmd)
	{
		var kind = pCmd.Code == ShowMessage ? "Show message" : "Comment";
		var text = pCmd.Text;
		// Consume continuation lines (ShowMessage_2 / Comment_2).
		//
		// **The continuation code is not one number.** A message is 10110 with
		// its first line inline, and the lines after it are **20110 in one
		// game and 1009 in the other** — 276 and 20 across the four pinned maps.
		// Reading only 20110 silently loses the 1009 lines, and a first draft
		// did exactly that for every message in both fixtures.
		//
		// **Both are read**, because a reader that guesses which number a game
		// uses is a reader that drops half its dialogue on the other game.
		while (_commandIndex + 1 < _commands.Count
			&& (pCmd.Code == ShowMessage
				? IsMessageLine(_commands[_commandIndex + 1])
				: _commands[_commandIndex + 1].Code == Comment2))
		{
			_commandIndex++;
			text += "\n" + _commands[_commandIndex].Text;
		}
		if (pCmd.Code == ShowMessage && _presentation != null)
		{
			if (!_presentation.ShowMessage(text))
			{
				_state.AddDiagnostic($"[Event {_eventId}] Presentation rejected message: exceeds bounds");
			}
		}
		_state.AddDiagnostic($"[Event {_eventId}] {kind}: {Truncate(text)}");
	}

	/// <summary>
	/// Whether a command continues a message, from both numbers the format uses.
	/// </summary>
	/// <remarks>
	/// <strong>20110 is <c>ShowMessage_2</c> and 1009 is the bare line.</strong>
	/// liblcf gives the codes as <c>0x4E8A</c> and <c>0x03ED</c>, and two games
	/// in the fixtures use one each. **Measuring showed 276 of one and 20 of
	/// the other**, so a reader that handles only the larger number loses 20
	/// lines of dialogue in the game that uses the smaller one — and every test
	/// it has still passes, because the test game uses 20110.
	/// </remarks>
	private static bool IsMessageLine(Rm2kMap.EventCommand pCommand)
	{
		return pCommand.Code == ShowMessage2 || pCommand.Code == ShowMessageLine;
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
		WaitForFrames(tenths == 0 ? 1 : TenthsToFrames(tenths));
		_state.AddDiagnostic($"[Event {_eventId}] Wait {_waitFramesRemaining} frames");
	}

	/// <summary>EasyRPG converts tenths of a second at 60 simulation frames per second.</summary>
	private static int TenthsToFrames(int pTenths) => Math.Min(checked(pTenths * 6), MaxWaitFrames);

	private void WaitForFrames(int pFrames)
	{
		_waitFramesRemaining = Math.Clamp(pFrames, 1, MaxWaitFrames);
	}

	private void ExecuteChangeLevelOrExp(Rm2kMap.EventCommand pCmd, bool pIsLevel)
	{
		var label = pIsLevel ? "Change level" : "Change exp";
		// EasyRPG: [actorMode, actorId, operation, operandMode, operand, showMessage]
		// with CmdSetup minimum width 6.
		if (pCmd.Parameters.Count < 6)
		{
			Malformed(label);
			return;
		}
		var actors = ResolveActors(pCmd.Parameters[0], pCmd.Parameters[1], label);
		if (actors == null)
		{
			return;
		}
		var operation = pCmd.Parameters[2];
		if (operation != ActorValueAdd && operation != ActorValueSubtract)
		{
			_state.AddDiagnostic($"[Event {_eventId}] {label}: unsupported operation {operation} skipped");
			return;
		}
		int operand;
		switch (pCmd.Parameters[3])
		{
			case VarOperandConstant:
				operand = pCmd.Parameters[4];
				break;
			case VarOperandVariable:
				operand = GetVariable(pCmd.Parameters[4]);
				break;
			case VarOperandVariableIndirect:
				if (pCmd.Parameters[4] < 1 || pCmd.Parameters[4] > GameSimulationState.MaxVariables)
				{
					_state.AddDiagnostic($"[Event {_eventId}] {label}: invalid indirect variable {pCmd.Parameters[4]} skipped");
					return;
				}
				operand = GetVariable(GetVariable(pCmd.Parameters[4]));
				break;
			default:
				_state.AddDiagnostic($"[Event {_eventId}] {label}: unsupported operand mode {pCmd.Parameters[3]} skipped");
				return;
		}
		// OperateValue negates the operand for the subtract operation.
		if (operation == ActorValueSubtract)
		{
			operand = -operand;
		}

		foreach (var actorId in actors)
		{
			if (pIsLevel)
			{
				var level = Math.Clamp(
					_state.GetActorLevel(actorId) + operand,
					GameSimulationState.MinActorLevel,
					GameSimulationState.MaxActorLevel);
				_state.SetActorLevel(actorId, level);
				_state.AddDiagnostic($"[Event {_eventId}] Change level: actor {actorId} -> level {level}");
			}
			else
			{
				var exp = Math.Clamp(
					_state.GetActorExp(actorId) + operand,
					0,
					GameSimulationState.MaxActorExp);
				_state.SetActorExp(actorId, exp);
				_state.AddDiagnostic($"[Event {_eventId}] Change exp: actor {actorId} -> exp {exp}");
			}
		}
	}

	/// <summary>
	/// EasyRPG GetActors(): mode 0 selects the party, mode 1 a single actor id,
	/// mode 2 the actor id stored in a variable. Returns null when the request is
	/// invalid, so the caller can fail closed.
	/// </summary>
	private List<int>? ResolveActors(int pActorMode, int pActorId, string pLabel)
	{
		switch (pActorMode)
		{
			case ActorSelectParty:
				return new List<int>(_state.PartyMemberIds);
			case ActorSelectHero:
				if (pActorId < 1 || pActorId > GameSimulationState.MaxActorId)
				{
					_state.AddDiagnostic($"[Event {_eventId}] {pLabel}: invalid actor id {pActorId} skipped");
					return null;
				}
				return new List<int> { pActorId };
			case ActorSelectVariableHero:
				if (pActorId < 1 || pActorId > GameSimulationState.MaxVariables)
				{
					_state.AddDiagnostic($"[Event {_eventId}] {pLabel}: invalid variable id {pActorId} skipped");
					return null;
				}
				var actorId = GetVariable(pActorId);
				if (actorId < 1 || actorId > GameSimulationState.MaxActorId)
				{
					_state.AddDiagnostic($"[Event {_eventId}] {pLabel}: variable {pActorId} holds invalid actor id {actorId}");
					return null;
				}
				return new List<int> { actorId };
			default:
				_state.AddDiagnostic($"[Event {_eventId}] {pLabel}: unsupported actor mode {pActorMode} skipped");
				return null;
		}
	}

	private void ExecuteChangeHeroName(Rm2kMap.EventCommand pCmd)
	{
		// EasyRPG CmdSetup minimum width 1; the actor id is the first parameter
		// and the new name is the command string.
		if (pCmd.Parameters.Count < 1)
		{
			Malformed("Change hero name");
			return;
		}
		var actorId = pCmd.Parameters[0];
		if (actorId < 1 || actorId > GameSimulationState.MaxActorId)
		{
			_state.AddDiagnostic($"[Event {_eventId}] Change hero name: invalid actor id {actorId} skipped");
			return;
		}
		_state.SetActorName(actorId, pCmd.Text);
		_state.AddDiagnostic($"[Event {_eventId}] Change hero name: actor {actorId} renamed");
	}

	/// <summary>
	/// EasyRPG CommandChangeEventLocation: [eventId, operandMode, x, y] with an
	/// optional RPG2K3 direction in parameters[4].
	/// </summary>
	private void ExecuteChangeEventLocation(Rm2kMap.EventCommand pCmd)
	{
		if (pCmd.Parameters.Count < 4)
		{
			Malformed("Change event location");
			return;
		}
		if (_eventLocationSetter == null)
		{
			_state.AddDiagnostic($"[Event {_eventId}] Change event location: no event location hook available");
			return;
		}
		var targetEventId = pCmd.Parameters[0];
		if (targetEventId < 1 || targetEventId > GameSimulationState.MaxActorId)
		{
			_state.AddDiagnostic($"[Event {_eventId}] Change event location: invalid event id {targetEventId} skipped");
			return;
		}
		var x = ResolveEventCoordinate(pCmd.Parameters[1], pCmd.Parameters[2]);
		var y = ResolveEventCoordinate(pCmd.Parameters[1], pCmd.Parameters[3]);
		if (x < 0 || y < 0)
		{
			_state.AddDiagnostic($"[Event {_eventId}] Change event location: invalid coordinates ({x},{y}) skipped");
			return;
		}
		var direction = pCmd.Parameters.Count > 4 ? pCmd.Parameters[4] - 1 : -1;
		if (direction is not (-1 or 0 or 1 or 2 or 3))
		{
			_state.AddDiagnostic($"[Event {_eventId}] Change event location: invalid direction {pCmd.Parameters[4]} skipped");
			return;
		}
		if (!_eventLocationSetter(targetEventId, x, y, direction))
		{
			_state.AddDiagnostic($"[Event {_eventId}] Change event location: event {targetEventId} not found");
			return;
		}
		_state.AddDiagnostic($"[Event {_eventId}] Change event location: event {targetEventId} -> ({x},{y})");
	}

	private int ResolveEventCoordinate(int pOperandMode, int pValue)
	{
		switch (pOperandMode)
		{
			case VarOperandConstant:
				return pValue;
			case VarOperandVariable:
				return pValue >= 1 && pValue <= GameSimulationState.MaxVariables ? GetVariable(pValue) : -1;
			case VarOperandVariableIndirect:
				if (pValue < 1 || pValue > GameSimulationState.MaxVariables)
				{
					return -1;
				}
				return GetVariable(GetVariable(pValue));
			default:
				return -1;
		}
	}

	/// <summary>
	/// EasyRPG CommandEraseEvent: the vanilla command carries no parameters and
	/// deactivates the event that owns the running command list.
	/// </summary>
	private bool ExecuteEraseEvent(Rm2kMap.EventCommand pCmd)
	{
		if (pCmd.Parameters.Count > 0)
		{
			// Patch-provided event ids are not modeled.
			_state.AddDiagnostic($"[Event {_eventId}] Erase event: parameterized form is not supported yet");
			return Advance();
		}
		if (_eventDeactivator == null)
		{
			_state.AddDiagnostic($"[Event {_eventId}] Erase event: no event deactivation hook available");
			return Advance();
		}
		if (!_eventDeactivator(_eventId))
		{
			_state.AddDiagnostic($"[Event {_eventId}] Erase event: event {_eventId} not found");
			return Advance();
		}
		_state.AddDiagnostic($"[Event {_eventId}] Erase event: event {_eventId} deactivated");
		return FinishFrame();
	}

	private void ExecuteFlashScreen(Rm2kMap.EventCommand pCmd)
	{
		// EasyRPG CommandFlashScreen: [red, green, blue, alpha, tenths, wait]
		// with CmdSetup minimum width 6.
		if (pCmd.Parameters.Count < 6)
		{
			Malformed("Flash screen");
			return;
		}
		if (_presentation == null)
		{
			_state.AddDiagnostic($"[Event {_eventId}] Flash screen: presentation state unavailable");
			return;
		}
		var tenths = pCmd.Parameters[4];
		if (tenths < 0 || tenths > EventInterpreterMaxTenths)
		{
			_state.AddDiagnostic($"[Event {_eventId}] Flash screen: invalid duration {tenths} skipped");
			return;
		}
		var subcommand = pCmd.Parameters.Count > 6 ? pCmd.Parameters[6] : FlashSubOnce;
		if (subcommand is not (FlashSubOnce or FlashSubBegin or FlashSubEnd))
		{
			_state.AddDiagnostic($"[Event {_eventId}] Flash screen: unsupported subcommand {subcommand} skipped");
			return;
		}
		if (subcommand == FlashSubEnd)
		{
			_presentation.FlashEnd();
			_state.AddDiagnostic($"[Event {_eventId}] Flash screen: ended");
			return;
		}
		var frames = TenthsToFrames(tenths);
		if (!_presentation.FlashOnce(pCmd.Parameters[0], pCmd.Parameters[1], pCmd.Parameters[2], pCmd.Parameters[3], frames))
		{
			_state.AddDiagnostic($"[Event {_eventId}] Flash screen: parameters outside bounds skipped");
			return;
		}
		_state.AddDiagnostic($"[Event {_eventId}] Flash screen: {frames} frames");
		// SetupWait treats a zero duration as a single frame.
		if (pCmd.Parameters[5] != 0)
		{
			WaitForFrames(tenths <= 0 ? 1 : frames);
		}
	}

	private void ExecuteShakeScreen(Rm2kMap.EventCommand pCmd)
	{
		// EasyRPG CommandShakeScreen: [strength, speed, tenths, wait]
		// with CmdSetup minimum width 4.
		if (pCmd.Parameters.Count < 4)
		{
			Malformed("Shake screen");
			return;
		}
		if (_presentation == null)
		{
			_state.AddDiagnostic($"[Event {_eventId}] Shake screen: presentation state unavailable");
			return;
		}
		var tenths = pCmd.Parameters[2];
		if (tenths < 0 || tenths > EventInterpreterMaxTenths)
		{
			_state.AddDiagnostic($"[Event {_eventId}] Shake screen: invalid duration {tenths} skipped");
			return;
		}
		var subcommand = pCmd.Parameters.Count > 4 ? pCmd.Parameters[4] : ShakeSubOnce;
		if (subcommand is not (ShakeSubOnce or ShakeSubBegin or ShakeSubEnd))
		{
			_state.AddDiagnostic($"[Event {_eventId}] Shake screen: unsupported subcommand {subcommand} skipped");
			return;
		}
		if (subcommand == ShakeSubEnd || tenths == 0)
		{
			// EasyRPG treats a zero duration as ending the shake.
			_presentation.ShakeEnd();
			_state.AddDiagnostic($"[Event {_eventId}] Shake screen: ended");
			return;
		}
		var frames = TenthsToFrames(tenths);
		if (!_presentation.ShakeOnce(pCmd.Parameters[0], pCmd.Parameters[1], frames))
		{
			_state.AddDiagnostic($"[Event {_eventId}] Shake screen: parameters outside bounds skipped");
			return;
		}
		_state.AddDiagnostic($"[Event {_eventId}] Shake screen: {frames} frames");
		if (pCmd.Parameters[3] != 0)
		{
			WaitForFrames(frames);
		}
	}

	private void ExecuteWeatherEffects(Rm2kMap.EventCommand pCmd)
	{
		// EasyRPG CommandWeatherEffects: [type, strength] with minimum width 2.
		if (pCmd.Parameters.Count < 2)
		{
			Malformed("Weather effects");
			return;
		}
		if (_presentation == null)
		{
			_state.AddDiagnostic($"[Event {_eventId}] Weather effects: presentation state unavailable");
			return;
		}
		// EasyRPG clamps the strength to 2 and folds unknown RM2K types to 0.
		var strength = Math.Min(pCmd.Parameters[1], PresentationState.MaxWeatherStrength);
		var type = pCmd.Parameters[0] > PresentationState.MaxWeatherType ? 0 : pCmd.Parameters[0];
		if (type < 0 || !_presentation.SetWeather(type, strength))
		{
			_state.AddDiagnostic($"[Event {_eventId}] Weather effects: type {pCmd.Parameters[0]} rejected");
			return;
		}
		_state.AddDiagnostic($"[Event {_eventId}] Weather effects: type {type} strength {strength}");
	}

	private void ExecuteControlSwitches(Rm2kMap.EventCommand pCmd)
	{
		if (pCmd.Parameters.Count < 4)
		{
			Malformed("Control switches");
			return;
		}
		// EasyRPG Game_Interpreter_Shared::TargetEvalMode: parameters[0] selects
		// the lvalue form, [1] is the first id and [2] the range end.
		var targetMode = pCmd.Parameters[0];
		var startId = pCmd.Parameters[1];
		var endId = targetMode == TargetEvalRange ? pCmd.Parameters[2] : startId;
		var mode = pCmd.Parameters[3];
		if (targetMode != TargetEvalSingle && targetMode != TargetEvalRange)
		{
			_state.AddDiagnostic($"[Event {_eventId}] Control switches: unsupported target mode {targetMode} skipped");
			return;
		}
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

	private void ExecuteChangeGold(Rm2kMap.EventCommand pCmd)
	{
		if (pCmd.Parameters.Count < 3)
		{
			Malformed("Change gold");
			return;
		}
		var operation = pCmd.Parameters[0];
		var operandType = pCmd.Parameters[1];
		var operandValue = pCmd.Parameters[2];
		if (operation is not (0 or 1))
		{
			_state.AddDiagnostic($"[Event {_eventId}] Change gold: unsupported operation {operation} skipped");
			return;
		}

		int operand;
		switch (operandType)
		{
			case VarOperandConstant:
				operand = operandValue;
				break;
			case VarOperandVariable:
				if (operandValue < 1 || operandValue > GameSimulationState.MaxVariables)
				{
					_state.AddDiagnostic($"[Event {_eventId}] Change gold: invalid variable operand {operandValue} skipped");
					return;
				}
				operand = GetVariable(operandValue);
				break;
			default:
				_state.AddDiagnostic($"[Event {_eventId}] Change gold: unsupported operand type {operandType} skipped");
				return;
		}

		var current = (long)_state.Gold;
		var result = operation switch
		{
			GoldOpAdd => current + operand,
			GoldOpSubtract => current - operand,
			_ => current,
		};
		_state.Gold = (int)Math.Clamp(result, 0L, (long)MaxGold);
		_state.AddDiagnostic($"[Event {_eventId}] Gold <- op {operation} {operand}: {_state.Gold}");
	}

	private void ExecuteChangeItems(Rm2kMap.EventCommand pCmd)
	{
		if (pCmd.Parameters.Count < 5)
		{
			Malformed("Change items");
			return;
		}

		var operation = pCmd.Parameters[0];
		var itemMode = pCmd.Parameters[1];
		var itemValue = pCmd.Parameters[2];
		var operandType = pCmd.Parameters[3];
		var operandValue = pCmd.Parameters[4];
		if (operation is not (ItemOpAdd or ItemOpSubtract))
		{
			_state.AddDiagnostic($"[Event {_eventId}] Change items: unsupported operation {operation} skipped");
			return;
		}

		var itemId = itemMode switch
		{
			ItemIdConstant => itemValue,
			ItemIdVariable => GetVariable(itemValue),
			_ => -1,
		};
		if (itemMode is not (ItemIdConstant or ItemIdVariable) || itemId < 1 || itemId > MaxItemId)
		{
			_state.AddDiagnostic($"[Event {_eventId}] Change items: invalid item id {itemId} skipped");
			return;
		}
		if (itemMode == ItemIdVariable && (itemValue < 1 || itemValue > GameSimulationState.MaxVariables))
		{
			_state.AddDiagnostic($"[Event {_eventId}] Change items: invalid item variable {itemValue} skipped");
			return;
		}
		if (operandType == VarOperandVariable && (operandValue < 1 || operandValue > GameSimulationState.MaxVariables))
		{
			_state.AddDiagnostic($"[Event {_eventId}] Change items: invalid amount variable {operandValue} skipped");
			return;
		}
		if (operandType is not (VarOperandConstant or VarOperandVariable))
		{
			_state.AddDiagnostic($"[Event {_eventId}] Change items: invalid operand type {operandType} skipped");
			return;
		}
		var amount = operandType == VarOperandVariable ? GetVariable(operandValue) : operandValue;
		if (amount < 0)
		{
			_state.AddDiagnostic($"[Event {_eventId}] Change items: negative amount {amount} skipped");
			return;
		}

		var current = _state.ItemCounts.TryGetValue(itemId, out var count) ? count : 0;
		var delta = operation == ItemOpSubtract ? -(long)amount : amount;
		var result = Math.Clamp((long)current + delta, 0L, (long)MaxItemCount);
		_state.ItemCounts[itemId] = (int)result;
		_state.AddDiagnostic($"[Event {_eventId}] Item {itemId} count <- op {operation} {amount}: {result}");
	}

	private void ExecuteChangePartyMembers(Rm2kMap.EventCommand pCmd)
	{
		if (pCmd.Parameters.Count < 3)
		{
			Malformed("Change party members");
			return;
		}

		var operation = pCmd.Parameters[0];
		var actorMode = pCmd.Parameters[1];
		var actorValue = pCmd.Parameters[2];
		if (operation is not (PartyOpAdd or PartyOpRemove))
		{
			_state.AddDiagnostic($"[Event {_eventId}] Change party members: unsupported operation {operation} skipped");
			return;
		}

		int actorId;
		switch (actorMode)
		{
			case ActorIdConstant:
				actorId = actorValue;
				break;
			case ActorIdVariable:
				if (actorValue < 1 || actorValue > GameSimulationState.MaxVariables)
				{
					_state.AddDiagnostic($"[Event {_eventId}] Change party members: invalid actor variable {actorValue} skipped");
					return;
				}
				actorId = GetVariable(actorValue);
				break;
			default:
				_state.AddDiagnostic($"[Event {_eventId}] Change party members: unsupported actor mode {actorMode} skipped");
				return;
		}

		if (actorId < 1 || actorId > GameSimulationState.MaxActorId)
		{
			_state.AddDiagnostic($"[Event {_eventId}] Change party members: invalid actor id {actorId} skipped");
			return;
		}

		var existingIndex = -1;
		for (var index = 0; index < _state.PartyMemberIds.Count; index++)
		{
			if (_state.PartyMemberIds[index] == actorId)
			{
				existingIndex = index;
				break;
			}
		}

		if (operation == PartyOpAdd)
		{
			if (existingIndex >= 0)
			{
				_state.AddDiagnostic($"[Event {_eventId}] Change party members: actor {actorId} already in party");
				return;
			}
			if (_state.PartyMemberIds.Count >= GameSimulationState.MaxPartyMembers)
			{
				_state.AddDiagnostic($"[Event {_eventId}] Change party members: party full, actor {actorId} skipped");
				return;
			}
			_state.PartyMemberIds.Add(actorId);
		}
		else
		{
			if (existingIndex < 0)
			{
				_state.AddDiagnostic($"[Event {_eventId}] Change party members: actor {actorId} not in party");
				return;
			}
			_state.PartyMemberIds.RemoveAt(existingIndex);
			if (_state.ActiveActorIndex >= _state.PartyMemberIds.Count)
			{
				_state.ActiveActorIndex = Math.Max(0, _state.PartyMemberIds.Count - 1);
			}
		}

		_state.AddDiagnostic($"[Event {_eventId}] Party <- op {operation} actor {actorId}");
	}

	private void ExecuteControlVars(Rm2kMap.EventCommand pCmd)
	{
		if (pCmd.Parameters.Count < 6)
		{
			Malformed("Control variables");
			return;
		}
		var targetMode = pCmd.Parameters[0];
		var startId = pCmd.Parameters[1];
		var endId = targetMode == TargetEvalRange ? pCmd.Parameters[2] : startId;
		var op = pCmd.Parameters[3];
		var operandType = pCmd.Parameters[4];
		var operandValue = pCmd.Parameters[5];

		if (targetMode != TargetEvalSingle && targetMode != TargetEvalRange)
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
			case VarOperandVariableIndirect:
				// EasyRPG ValueOrVariable mode 2: v[v[x]].
				if (operandValue < 1 || operandValue > GameSimulationState.MaxVariables)
				{
					_state.AddDiagnostic($"[Event {_eventId}] Control variables: invalid indirect variable {operandValue} skipped");
					return;
				}
				operand = GetVariable(GetVariable(operandValue));
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
		var facing = pCmd.Parameters.Count >= 4 ? pCmd.Parameters[3] : (int)_state.FacingDirection;
		if (mapId < 1 || mapId > GameSimulationState.MaxMapId || x < 0 || y < 0 || facing is not (2 or 4 or 6 or 8))
		{
			_state.AddDiagnostic($"[Event {_eventId}] Transfer player: invalid target ({mapId}, {x}, {y}) skipped");
			return;
		}
		_state.FacingDirection = (byte)facing;
		_state.PendingMapId = mapId;
		_state.PendingX = x;
		_state.PendingY = y;
		_state.IsTransferPending = true;
		_state.AddDiagnostic($"[Event {_eventId}] Transfer pending -> map {mapId} at ({x}, {y})");
	}

	private void ExecuteConditionalBranch()
	{
		if (!EvaluateCondition())
		{
			var elseIndex = FindConditionalBoundary(_commandIndex, true);
			var target = elseIndex ?? FindConditionalBoundary(_commandIndex, false);
			// Jump onto the Else/End marker; the frame loop advances past it.
			_commandIndex = target ?? _commands.Count - 1;
		}
	}

	private bool EvaluateCondition()
	{
		var cmd = _commands[_commandIndex];
		switch (Param(cmd, 0))
		{
			case ConditionSwitch:
				if (cmd.Parameters.Count < 3)
				{
					Malformed("ConditionalBranch");
					return false;
				}
				return GetSwitch(Param(cmd, 1)) == (Param(cmd, 2) == 0);
			case ConditionVariable:
				if (cmd.Parameters.Count < 5)
				{
					Malformed("ConditionalBranch");
					return false;
				}
				var left = GetVariable(Param(cmd, 1));
				var right = Param(cmd, 2) == VarOperandConstant
					? Param(cmd, 3)
					: GetVariable(Param(cmd, 3));
				return CompareValues(left, right, Param(cmd, 4));
			default:
				// Timer/gold/item/actor conditions need party/timer state that
				// the deterministic core does not model yet; EasyRPG treats an
				// unevaluable branch as false (else path).
				_state.AddDiagnostic($"[Event {_eventId}] ConditionalBranch type {Param(cmd, 0)} not supported yet; taking else branch");
				return false;
		}
	}

	private bool CompareValues(int pLeft, int pRight, int pOperator)
	{
		switch (pOperator)
		{
			case BranchOpEqual: return pLeft == pRight;
			case BranchOpGreaterOrEqual: return pLeft >= pRight;
			case BranchOpLessOrEqual: return pLeft <= pRight;
			case BranchOpGreater: return pLeft > pRight;
			case BranchOpLess: return pLeft < pRight;
			case BranchOpNotEqual: return pLeft != pRight;
			default:
				Malformed("ConditionalBranch");
				return false;
		}
	}

	/// <summary>
	/// Finds the ElseBranch or EndBranch belonging to this ConditionalBranch,
	/// skipping nested branch blocks by depth counting.
	/// </summary>
	private int? FindConditionalBoundary(int pFrom, bool pSearchElse)
	{
		var depth = 0;
		for (var i = pFrom + 1; i < _commands.Count; i++)
		{
			var code = _commands[i].Code;
			if (code == ConditionalBranch)
			{
				depth++;
			}
			else if (code == EndBranch)
			{
				if (depth == 0 && !pSearchElse)
				{
					return i;
				}
				depth--;
			}
			else if (code == ElseBranch && depth == 0 && pSearchElse)
			{
				return i;
			}
		}
		return null;
	}

	private void ExecuteElseBranch()
	{
		// Reached only when the condition was true and the then-body ran to
		// its end: skip the else block by jumping onto the matching EndBranch.
		_commandIndex = FindConditionalBoundary(_commandIndex, false) ?? _commandIndex;
	}

	private void ExecuteEndBranch()
	{
		// Structured block end; the frame loop advances past it.
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

	private bool GetSwitch(int pId)
	{
		return pId >= 1 && pId <= _state.Switches.Count && _state.Switches[pId - 1];
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
