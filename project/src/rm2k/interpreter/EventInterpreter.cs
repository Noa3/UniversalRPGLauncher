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
	/// <summary>10120, Message Options, <c>CmdSetup</c> width 4.</summary>
	public const int MessageOptions = 10120;

	/// <summary>
	/// 10130, Change Face Graphic, from <c>Code::ChangeFaceGraphic</c>
	/// and <c>CommandChangeFaceGraphic</c>, whose <c>CmdSetup</c> gives
	/// it a minimum width of 3.
	/// </summary>
	public const int ChangeFaceGraphic = 10130;

	/// <summary>10230, Timer Operation, <c>CmdSetup</c> width 5.</summary>
	public const int TimerOperation = 10230;

	public const int ShowMessage = 10110;
	public const int ShowChoice = 10140;
	public const int InputNumber = 10150;
	public const int ChangeGold = 10310;
	public const int ChangeItems = 10320;
	public const int ChangePartyMembers = 10330;
	public const int ChangeExp = 10410;
	/// <summary>10430, Change Parameters, <c>CmdSetup</c> width 6.</summary>
	public const int ChangeParameters = 10430;

	/// <summary>10460, Change HP, from <c>CommandChangeHP</c>, width 6.</summary>
	public const int ChangeHP = 10460;

	/// <summary>10470, Change SP, from <c>CommandChangeSP</c>, width 5.</summary>
	public const int ChangeSP = 10470;

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
	/// <summary>
	/// 11110, Show Picture, from liblcf <c>Code::ShowPicture</c> and EasyRPG's
	/// <c>CommandShowPicture</c>, whose <c>CmdSetup</c> gives it a minimum
	/// width of 14.
	/// </summary>
	public const int ShowPicture = 11110;

	/// <summary>11130, Erase Picture, from the same pair.</summary>
	public const int ErasePicture = 11130;

	/// <summary>11120, Move Picture.</summary>
	public const int MovePicture = 11120;

	/// <summary>11510, Play Background Music, from liblcf <c>Code::PlayBGM</c>.</summary>
	public const int PlayBGM = 11510;

	/// <summary>11520, Fade Out Background Music, from <c>Code::FadeOutBGM</c>.</summary>
	public const int FadeOutBGM = 11520;

	/// <summary>11530, Memorize Background Music.</summary>
	public const int MemorizeBGM = 11530;

	/// <summary>11540, Play Memorized Background Music.</summary>
	public const int PlayMemorizedBGM = 11540;

	/// <summary>11550, Play Sound Effect, from liblcf <c>Code::PlaySound</c>.</summary>
	public const int PlaySound = 11550;

	/// <summary>
	/// 12110, Label, from liblcf <c>Code::Label</c>.
	/// </summary>
	/// <remarks>
	/// <strong>The reference's case for it is <c>return true</c> and nothing
	/// else</strong> — it has no method, no parameters read, and no effect. A
	/// label is a name, not an instruction, and a reader that gave it one would
	/// invent a semantic the format does not have.
	/// </remarks>
	public const int Label = 12110;

	/// <summary>
	/// 12120, Jump to Label, from liblcf <c>Code::JumpToLabel</c> and EasyRPG's
	/// <c>CommandJumpToLabel</c>, whose <c>CmdSetup</c> gives it a minimum
	/// width of 1.
	/// </summary>
	public const int JumpToLabel = 12120;

	/// <summary>11010, Erase Screen, from liblcf <c>Code::EraseScreen</c>.</summary>
	public const int EraseScreen = 11010;

	/// <summary>11020, Show Screen.</summary>
	public const int ShowScreen = 11020;

	/// <summary>
	/// 11030, Tint Screen, from <c>Code::TintScreen</c> and EasyRPG
	/// <c>CommandTintScreen</c>, whose <c>CmdSetup</c> gives it a
	/// minimum width of 6.
	/// </summary>
	/// <summary>11310, Player Visibility, from <c>Code::PlayerVisibility</c>.</summary>
	public const int PlayerVisibility = 11310;

	/// <summary>
	/// 11330, Move Event, from <c>Code::MoveEvent</c> and EasyRPG's
	/// <c>CommandMoveEvent</c>, whose <c>CmdSetup</c> gives it a
	/// minimum width of 4.
	/// </summary>
	/// <summary>
	/// 10820, Memorize Location, from <c>Code::MemorizeLocation</c> and
	/// EasyRPG's <c>CommandMemorizeLocation</c>, whose <c>CmdSetup</c>
	/// gives it a minimum width of 3.
	/// </summary>
	/// <summary>
	/// 11840, 11930 and 11960, the three access commands: Change Escape
	/// Access, Change Save Access and Change Main Menu Access.
	/// </summary>
	/// <remarks>
	/// The reference has three one-line methods with the same shape, so one
	/// handler and three constants is the honest reading and not a saving.
	/// </remarks>
	public const int ChangeEscapeAccess = 11840;
	public const int ChangeSaveAccess = 11930;
	public const int ChangeMainMenuAccess = 11960;
	public const int MemorizeLocation = 10820;

	public const int MoveEvent = 11330;

	public const int TintScreen = 11030;

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

	/// <summary>
	/// 1009, from liblcf <c>Code::ChangeBattleCommands</c> and EasyRPG
	/// <c>CommandChangeBattleCommands</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>This was misread as a second message continuation line and the
	/// mistake was pushed.</strong> A card counted 20 occurrences of a bare
	/// <c>1009</c> with a string after a <c>10110</c>, compared them to MZ's
	/// <c>401</c> following a <c>101</c>, and concluded the two engines share a
	/// convention. They do not.
	/// </para>
	/// <para>
	/// The fixture settles it: <c>1009</c> carries <strong>four integers and an
	/// empty text</strong> — <c>[1,1,1,1]</c>, <c>[1,1,2,1]</c>,
	/// <c>[1,3,8,1]</c>, <c>[1,4,10,1]</c> — which are exactly
	/// <c>parameters[0..3]</c> of <c>CommandChangeBattleCommands</c>: actor,
	/// class, battle command id, and whether to add. <strong>A message line
	/// carries text and no integers, and these carry neither.</strong>
	/// </para>
	/// <para>
	/// <strong>Two engines having the same number for different things is not a
	/// coincidence worth acting on</strong> — it is the normal state of a
	/// twenty year old command table, and a pattern that fits two readings is
	/// not evidence for either.
	/// </para>
	/// </remarks>
	/// <summary>
	/// 11610, Key Input Proc, from liblcf <c>Code::KeyInputProc</c> and EasyRPG
	/// <c>Game_Interpreter::CommandKeyInputProc</c>.
	/// </summary>
	/// <remarks>
	/// It waits for a key and writes a <em>code</em> into a variable, not the
	/// key's own value — a digit answers 11 to 20, an operator 21 to 25, the
	/// confirm key is 5. It is not the number input command with a different
	/// name, and <c>Rm2kKeyInput</c> holds the whole table.
	/// </remarks>
	public const int KeyInputProc = 11610;

	public const int ChangeBattleCommands = 1009;

	/// <summary>5001, from liblcf <c>Code::OpenLoadMenu</c>.</summary>
	public const int OpenLoadMenu = 5001;

	/// <summary>5002, from liblcf <c>Code::ExitGame</c>.</summary>
	public const int ExitGame = 5002;

	/// <summary>5003, from liblcf <c>Code::ToggleAtbMode</c>.</summary>
	public const int ToggleAtbMode = 5003;

	/// <summary>5004, from liblcf <c>Code::ToggleFullscreen</c>.</summary>
	public const int ToggleFullscreen = 5004;

	/// <summary>5005, from liblcf <c>Code::OpenVideoOptions</c>.</summary>
	public const int OpenVideoOptions = 5005;

	/// <summary>
	/// The RPG2K3 E commands, which is the only engine version that runs any of
	/// the five menu commands. EasyRPG's
	/// <c>Player::IsRPG2k3ECommands</c> gate.
	/// </summary>
	/// <remarks>
	/// **All five return true — a silent no-op — on a game that is not
	/// RPG2K3 E commands**, and a silent no-op in an interpreter is the worst
	/// possible answer: the page carries on as though the game had asked for
	/// nothing, and a player who pressed the button to open the load menu
	/// watches the game do nothing at all.
	/// </remarks>
	public static bool IsMenuCommand(int pCode)
	{
		return pCode >= OpenLoadMenu && pCode <= OpenVideoOptions;
	}
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
	/// <summary>
	/// Starts a move route on a character, for command 11330.
	/// </summary>
	/// <remarks>
	/// The event id, the move frequency, the commands, whether the
	/// route repeats and whether the page is held by it. **A null
	/// means the character this interpreter cannot name**, which is
	/// not the same as a character that refused the route.
	/// </remarks>
	private readonly Func<int, int, IReadOnlyList<Rm2kMap.MoveCommand>,
		bool, bool, bool>? _moveRouteStarter;
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
		Func<int, bool>? eventDeactivator = null,
		Func<int, int, IReadOnlyList<Rm2kMap.MoveCommand>, bool, bool, bool>?
			moveRouteStarter = null)
			{
		_state = state ?? throw new ArgumentNullException(nameof(state));
		_eventId = eventId;
		_commands = commands ?? throw new ArgumentNullException(nameof(commands));
		_presentation = presentation;
		_eventCommandResolver = eventCommandResolver;
		_eventLocationSetter = eventLocationSetter;
		_eventDeactivator = eventDeactivator;
		_moveRouteStarter = moveRouteStarter;
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

			case MessageOptions:
				ExecuteMessageOptions(cmd);
				return Advance();

			case ChangeFaceGraphic:
				ExecuteChangeFaceGraphic(cmd);
				return Advance();

			case TimerOperation:
				ExecuteTimerOperation(cmd);
				return Advance();

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

			case ChangeParameters:
				ExecuteChangeParameters(cmd);
				return Advance();

			case ChangeHP:
				ExecuteChangeHpOrSp(cmd, pIsHp: true);
				return Advance();

			case ChangeSP:
				ExecuteChangeHpOrSp(cmd, pIsHp: false);
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

			case ShowPicture:
				ExecuteShowPicture(cmd);
				return Advance();

			case ErasePicture:
				ExecuteErasePicture(cmd);
				return Advance();

			case ChangeEscapeAccess:
				ExecuteAccessChange(cmd, pWhich: AccessFlag.Escape);
				return Advance();

			case ChangeSaveAccess:
				ExecuteAccessChange(cmd, pWhich: AccessFlag.Save);
				return Advance();

			case ChangeMainMenuAccess:
				ExecuteAccessChange(cmd, pWhich: AccessFlag.Menu);
				return Advance();

			case MemorizeLocation:
				ExecuteMemorizeLocation(cmd);
				return Advance();

			case PlayerVisibility:
				ExecutePlayerVisibility(cmd);
				return Advance();

			case MoveEvent:
				ExecuteMoveEvent(cmd);
				return Advance();

			case TintScreen:
				// **The wait is the same conditional the reference uses**, and the
				// index moves only when the command did not set one. A first
				// draft returned a bare true here, so a tint that asked to wait
				// still advanced the page and the wait never happened.
				var waitBefore = _waitFramesRemaining;
				ExecuteTintScreen(cmd);
				return _waitFramesRemaining == waitBefore ? Advance() : IsRunning;

			case EraseScreen:
			case ShowScreen:
				ExecuteScreenTransition(cmd);
				return true;

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

			case PlayBGM:
			case PlaySound:
				ExecutePlayTrack(cmd);
				return Advance();

			case FadeOutBGM:
				ExecuteFadeOutBGM(cmd);
				return Advance();

			case MemorizeBGM:
				_state.AddDiagnostic(
					_state.Audio.MemorizeBgm()
						? $"[Event {_eventId}] Memorized the current BGM"
						: $"[Event {_eventId}] Memorize BGM: there was none, and"
							+ " the reference copies the pointer and leaves it empty");
				return Advance();

			case PlayMemorizedBGM:
				_state.AddDiagnostic(
					_state.Audio.PlayMemorizedBgm()
						? $"[Event {_eventId}] Played the memorised BGM"
						: $"[Event {_eventId}] Play memorised BGM: nothing was"
							+ " memorised");
				return Advance();

			case Label:
				// `case Cmd::Label: return true;` in the reference. A label is a
				// name, not an instruction, and giving it an effect would invent
				// a semantic the format does not have.
				return Advance();

			case JumpToLabel:
			{
				// **The engine's own rule, in full.** EasyRPG increments the
				// index only when the command left it alone:
				//
				//     if (index_before_exec == frame->current_command) {
				//         frame->current_command++;
				//     }
				//
				// A jump that finds its label moves the index, so it does not
				// advance — the page lands *on* the label, and the label is a
				// no-op that costs a frame of its own. A jump that finds
				// nothing leaves the index where it was, so the rule increments
				// it and the page carries on.
				//
				// **Two drafts got this wrong in opposite directions and both
				// were silent**: one returned a bare true and left the page on
				// the jump forever, which looks like a hang; the other advanced
				// unconditionally and skipped the no-op the format puts there
				// on purpose. Only the engine's conditional form is both.
				var before = _commandIndex;
				ExecuteJumpToLabel(cmd);
				return _commandIndex == before ? Advance() : IsRunning;
			}

			case Loop:
				_loopStack.Push(_commandIndex);
				return Advance();

			case BreakLoop:
				ExecuteBreakLoop();
				return true; // index already moved past the matching EndLoop

			case EndLoop:
				ExecuteEndLoop();
				return true; // index points at the matching Loop command

			case KeyInputProc:
				ExecuteKeyInputProc(cmd);
				return true;

			case ChangeBattleCommands:
				ExecuteChangeBattleCommands(cmd);
				return Advance();

			case OpenLoadMenu:
			case ExitGame:
			case ToggleAtbMode:
			case ToggleFullscreen:
			case OpenVideoOptions:
				return ExecuteMenuCommand(cmd);

			default:
				_state.AddDiagnostic($"[Event {_eventId}] Unsupported RM2K command {cmd.Code} skipped");
				return Advance();
		}
	}

	/// <summary>
	/// The five RPG2K3 menu commands, and the rule that decides whether they do
	/// anything at all.
	/// </summary>
	/// <remarks>
	/// <para>
	/// **The version gate is the whole command.** EasyRPG's
	/// <c>Player::IsRPG2k3ECommands()</c> guards all five, and every one of them
	/// <c>return true</c> — a silent no-op — on a game that is not RPG2K3 E
	/// commands. That is the worst answer an interpreter can give, because a
	/// player who pressed a button to open the load menu watches the game do
	/// nothing, and nothing in the log says why.
	/// </para>
	/// <para>
	/// <strong>So this reader does not copy the no-op.</strong> It refuses
	/// visibly: a diagnostic names the command and says the game is not an
	/// E-command game. <em>A command that silently does nothing is a bug that
	/// survives every test, because nothing changed.</em>
	/// </para>
	/// <para>
	/// <strong>On an E-command game the menu opens and the interpreter waits
	/// for it</strong>, which is the second half of the rule: EasyRPG returns
	/// <c>false</c> from <c>CommandOpenVideoOptions</c> after pushing a scene,
	/// so the page does not advance until the scene closes. A reader that
	/// advanced immediately would run the rest of the page behind a menu nobody
	/// had opened yet.
	/// </para>
	/// </remarks>
	private bool ExecuteMenuCommand(Rm2kMap.EventCommand pCommand)
	{
		if (!_state.SupportsRpg2k3ECommands)
		{
			_state.AddDiagnostic(
				$"[Event {_eventId}] {DescribeMenuCommand(pCommand.Code)} is an"
				+ " RPG2K3 E command and this game does not declare them;"
				+ " skipped, and nothing opened");
			return Advance();
		}

		switch (pCommand.Code)
		{
			case OpenLoadMenu:
				return PushScene("Load");
			case OpenVideoOptions:
				return PushScene("Settings");
			case ExitGame:
				_state.ExitRequested = true;
				return Advance();
			case ToggleAtbMode:
				_state.AtbWaitMode = !_state.AtbWaitMode;
				return Advance();
			case ToggleFullscreen:
				_state.FullscreenRequested = !_state.FullscreenRequested;
				return Advance();
			default:
				_state.AddDiagnostic(
					$"[Event {_eventId}] {DescribeMenuCommand(pCommand.Code)} is"
					+ " not one of the five and was refused rather than guessed at");
				return Advance();
		}
	}

	/// <summary>
	/// Pushes a scene and holds the page, from the source's <c>return false</c>.
	/// </summary>
	private bool PushScene(string pScene)
	{
		if (_state.CurrentScene != pScene)
		{
			_state.SceneStack.Add(pScene);
			_state.CurrentScene = pScene;
		}
		// The page does not advance. It advances when the scene is popped, which
		// is the caller's next decision and not this command's.
		return true;
	}

	/// <summary>
	/// The liblcf names of the five, for a diagnostic a reader can act on.
	/// </summary>
	private static string DescribeMenuCommand(int pCode)
	{
		return pCode switch
		{
			OpenLoadMenu => "Open Load Menu (5001)",
			ExitGame => "Exit Game (5002)",
			ToggleAtbMode => "Toggle ATB Mode (5003)",
			ToggleFullscreen => "Toggle Fullscreen (5004)",
			OpenVideoOptions => "Open Video Options (5005)",
			_ => $"Command {pCode}",
		};
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

	/// <summary>
	/// 10120, Message Options, from EasyRPG's
	/// <c>CommandMessageOptions</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>Four independent flags and not one "style".</strong> The
	/// reference sets four fields on the game system and a reader that
	/// collapsed them would make a transparent bottom message and a
	/// top-positioned one the same request.
	/// </para>
	/// <para>
	/// <strong>Parameters[2] is inverted</strong> — <c>== 0</c> means the
	/// window holds its position while the map scrolls. A reader that
	/// mapped a non-zero to fixed would scroll every window a game had
	/// pinned, and the difference is visible only while the map moves.
	/// </para>
	/// <remarks>
	/// <strong>Parameters[1] has three values and not two</strong>: top,
	/// middle, bottom. A reader that stored a boolean would put the
	/// middle where the top belongs.
	/// </para>
	/// </remarks>
	private void ExecuteMessageOptions(Rm2kMap.EventCommand pCmd)
	{
		if (_presentation == null)
		{
			Malformed("Message options");
			return;
		}
		// CmdSetup minimum width 4.
		if (pCmd.Parameters.Count < 4)
		{
			Malformed("Message options");
			return;
		}
		if (!_presentation.SetMessageOptions(
			Param(pCmd, 0) != 0,
			Param(pCmd, 1),
		// **Inverted, and the comment is the whole reason.** The reference
		// writes SetMessagePositionFixed(com.parameters[2] == 0), so a
		// zero is the fixed case and not the moving one.
		Param(pCmd, 2) == 0,
			Param(pCmd, 3) != 0))
		{
			_state.AddDiagnostic(
				$"[Event {_eventId}] Message options refused: position"
				+ $" {Param(pCmd, 1)} is not 0, 1 or 2, and nothing was changed");
			return;
		}
		_state.AddDiagnostic(
			$"[Event {_eventId}] Message options: transparent is"
			+ $" {Param(pCmd, 0) != 0}, position {Param(pCmd, 1)},"
			+ $" fixed is {Param(pCmd, 2) == 0},"
			+ $" continuing events is {Param(pCmd, 3) != 0}");
	}

	/// <summary>
	/// 10130, Change Face Graphic, from EasyRPG's <c>CommandChangeFaceGraphic</c>.
	/// </summary>
	/// <remarks>
	/// The name is in the command string field and the index in
	/// <c>parameters[0]</c>, and <c>parameters[1]</c> and <c>parameters[2]</c>
	/// are the right-side and flipped flags. This is <strong>a request and
	/// not a drawn portrait</strong> — nothing loads a file, and a face that
	/// is set with no renderer behind it is what this reader can say.
	/// </remarks>
	private void ExecuteChangeFaceGraphic(Rm2kMap.EventCommand pCmd)
	{
		if (_presentation == null)
		{
			Malformed("Change face graphic");
			return;
		}
		// CmdSetup minimum width 3.
		if (pCmd.Parameters.Count < 3)
		{
			Malformed("Change face graphic");
			return;
		}
		if (string.IsNullOrEmpty(pCmd.Text))
		{
			_state.AddDiagnostic(
				$"[Event {_eventId}] Change face graphic: no file name, and the"
				+ " string field is where the name lives; nothing was set");
			return;
		}
		if (!_presentation.SetFace(
			pCmd.Text, Param(pCmd, 0), Param(pCmd, 1) != 0, Param(pCmd, 2) != 0))
		{
			_state.AddDiagnostic(
				$"[Event {_eventId}] Change face graphic refused: the index"
				+ " is outside the file four slots, or the name was empty or too long");
			return;
		}
		_state.AddDiagnostic(
			$"[Event {_eventId}] Face set to slot {Param(pCmd, 0)},"
			+ $" on the right is {Param(pCmd, 1) != 0},"
			+ $" flipped is {Param(pCmd, 2) != 0}");
	}

	/// <summary>
	/// 10230, Timer Operation, from EasyRPG's <c>CommandTimerOperation</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>One command, three operations, chosen by
	/// <c>parameters[0]</c></strong>: 0 sets the seconds, 1 starts the timer
	/// with the visible and battle flags, 2 stops it. Anything else is
	/// <c>return false</c> in the reference, which holds the page.
	/// </para>
	/// <para>
	/// <strong>The seconds can come from a variable</strong> — this is
	/// <c>ValueOrVariable(parameters[1], parameters[2])</c> and not a plain
	/// read, so a game can set a timer from a counter it already keeps.
	/// </para>
	/// <para>
	/// <strong>A sixth parameter names the timer</strong>, and the reference
	/// reads it <em>only</em> when the command carries more than five and
	/// the game is RPG2K3. Without it, every timer command means timer one —
	/// which is why a 2K game has only one timer and a 2003 game has two.
	/// </para>
	/// </remarks>
	private void ExecuteTimerOperation(Rm2kMap.EventCommand pCmd)
	{
		// CmdSetup minimum width 5.
		if (pCmd.Parameters.Count < 5)
		{
			Malformed("Timer operation");
			return;
		}
		// **The sixth parameter names the timer, and only when the command
		// carries it and the game is RPG2K3.** A 2K game has one timer and a
		// 2003 game has two, and that is the whole difference.
		var timerId = pCmd.Parameters.Count > 5 && _state.SupportsRpg2k3ECommands
			? pCmd.Parameters[5] : 1;
		switch (pCmd.Parameters[0])
		{
			case 0:
			{
				var seconds = ValueOrVariable(Param(pCmd, 1), Param(pCmd, 2));
				if (!_state.SetTimer(timerId, seconds))
				{
					_state.AddDiagnostic(
						$"[Event {_eventId}] Timer {timerId}: {seconds} seconds is"
						+ " outside 0 to 86400, and nothing was set");
					return;
				}
				_state.AddDiagnostic(
					$"[Event {_eventId}] Timer {timerId} set to {seconds} seconds,"
					+ " and **not started**, because the reference has a separate"
					+ " start operation and a reader that started on set would start a"
					+ " countdown the game meant to arm");
				return;
			}
			case 1:
			{
				if (!_state.StartTimer(
					timerId, Param(pCmd, 3) != 0, Param(pCmd, 4) != 0))
				{
					_state.AddDiagnostic(
						$"[Event {_eventId}] Timer {timerId} cannot be started, because"
						+ " a game has two and that is not one of them");
					return;
				}
				_state.AddDiagnostic(
					$"[Event {_eventId}] Timer {timerId} started, visible is"
					+ $" {Param(pCmd, 3) != 0}, in battle is {Param(pCmd, 4) != 0}");
				return;
			}
			case 2:
			{
				if (!_state.StopTimer(timerId))
				{
					_state.AddDiagnostic(
						$"[Event {_eventId}] Timer {timerId} cannot be stopped, because"
						+ " a game has two and that is not one of them");
					return;
				}
				_state.AddDiagnostic(
					$"[Event {_eventId}] Timer {timerId} stopped, and its seconds"
					+ " are kept, because the reference does not reset them and a game"
					+ " that shows a count and restarts it expects it to still be there");
				return;
			}
			default:
				_state.AddDiagnostic(
					$"[Event {_eventId}] Timer operation {pCmd.Parameters[0]} is not"
					+ " set, start or stop, and the reference holds the page for it");
				return;
		}
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
		if (pCmd.Code == ShowMessage && _presentation != null)
		{
			if (!_presentation.ShowMessage(text))
			{
				_state.AddDiagnostic($"[Event {_eventId}] Presentation rejected message: exceeds bounds");
			}
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
		WaitForFrames(tenths == 0 ? 1 : TenthsToFrames(tenths));
		_state.AddDiagnostic($"[Event {_eventId}] Wait {_waitFramesRemaining} frames");
	}

	/// <summary>EasyRPG converts tenths of a second at 60 simulation frames per second.</summary>
	private static int TenthsToFrames(int pTenths) => Math.Min(checked(pTenths * 6), MaxWaitFrames);

	private void WaitForFrames(int pFrames)
	{
		_waitFramesRemaining = Math.Clamp(pFrames, 1, MaxWaitFrames);
	}

	/// <summary>
	/// 1009, Change Battle Commands, from EasyRPG's
	/// <c>CommandChangeBattleCommands</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Parameters: <c>[actorMode, actorId, commandId, add]</c>, which is what
	/// <c>CmdSetup&lt;..., 4&gt;</c> says — a minimum width of four. The
	/// reference reads <c>parameters[0..1]</c> through <c>GetActors</c>,
	/// <c>parameters[2]</c> as the command id and <c>parameters[3] != 0</c> as
	/// "add".
	/// </para>
	/// <para>
	/// <strong>Three actor modes, and they are not the same set of people.</strong>
	/// Mode 0 is the party, 1 is one hero by id, 2 is the hero named by a
	/// variable. An invalid hero id is <em>a warning and an empty list</em>, not
	/// an error — the command runs and touches nobody, and a reader that
	/// refused the whole page would drop the rest of an event because one hero
	/// id was wrong.
	/// </para>
	/// </remarks>
	/// <summary>
	/// 11610, Key Input Proc, from EasyRPG's
	/// <c>CommandKeyInputProc</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The reference resets the key state and returns <c>false</c>, so the page
	/// holds until a key arrives. <strong>And while waiting it sets the variable
	/// to zero every frame</strong> — the reference's own comment says so, and
	/// a reader that only wrote on arrival would leave whatever the game had put
	/// there a moment ago.
	/// </para>
	/// <para>
	/// <strong>The engine version is a parameter of the read, not an
	/// assumption.</strong> Parameters 5 to 9 mean shift/down/left/right/up on
	/// RM2K and numbers/operators/time-variable/timed on RM2K3, and reading the
	/// wrong column produces a command that waits for the wrong keys rather than
	/// an error.
	/// </para>
	/// </remarks>
	private void ExecuteKeyInputProc(Rm2kMap.EventCommand pCmd)
	{
		if (_presentation == null)
		{
			Malformed("Key input proc");
			return;
		}
		// The version is read from the save data, and a game that has not said
		// which it is is treated as 2K, which is the reading the command's own
		// legacy branch expects.
		var request = Rm2kKeyInput.Read(
			pCmd.Parameters,
			pIsRpg2k3: _state.SupportsRpg2k3ECommands,
			pIsMajorUpdated: _state.SupportsRpg2k3ECommands);
		if (request == null)
		{
			Malformed("Key input proc");
			return;
		}
		if (_presentation.PendingKeyInputVariableId == request.VariableId
			&& _presentation.PendingKeyInputKeys.Count > 0)
			{
			// Still open, and no key has arrived. The variable is zeroed every
			// frame the reference waits, so a game reading it sees 0 rather
			// than whatever it last held.
			if (request.Wait && request.VariableId <= GameSimulationState.MaxVariables)
			{
				WriteVariable(request.VariableId, 0);
			}
			return;
		}
		if (!_presentation.BeginKeyInput(request))
		{
			Malformed("Key input proc");
			return;
		}
		if (request.Wait && request.VariableId <= GameSimulationState.MaxVariables)
		{
			WriteVariable(request.VariableId, 0);
		}
		_state.AddDiagnostic(
			$"[Event {_eventId}] Key input proc: waiting for one of"
			+ $" {request.AllowedKeys.Count} keys -> variable {request.VariableId}");
	}

	/// <summary>
	/// Delivers a key press to an open 11610 prompt and writes the answer.
	/// </summary>
	/// <remarks>
	/// This is the frame-step side of the command, and it is separate from the
	/// dispatch on purpose: <c>ExecuteFrame</c> is one interpreter step, and a
	/// key arrives on an input frame, which is not the same thing. A reader that
	/// put the wait inside the dispatch would re-arm the prompt on every frame
	/// it stayed open, and the reference resets its key state on every call.
	/// </remarks>
	public void PressKeys(IReadOnlyList<string> pPressed)
	{
		if (_presentation == null)
		{
			return;
		}
		if (!_presentation.TryConsumeKeyInput(pPressed, out var value))
		{
			return;
		}
		if (!_presentation.TryConsumeKeyInput(
			out var variableId, out _, out _, out _))
			{
			return;
		}
		WriteVariable(variableId, value);
		_state.AddDiagnostic(
			$"[Event {_eventId}] Key input proc: value {value} -> variable {variableId}");
	}

	private void WriteVariable(int pVariableId, int pValue)
	{
		while (_state.Variables.Count < pVariableId)
		{
			_state.Variables.Add(0);
		}
		_state.Variables[pVariableId - 1] = pValue;
	}

	/// <summary>
	/// 12120, Jump to Label, from EasyRPG's <c>CommandJumpToLabel</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The search is <strong>from the beginning of the page, not from here</strong>
	/// — <c>for (int idx = 0; idx &lt; list.size(); idx++)</c> — so a jump can
	/// go backwards, and a backward jump is how an author writes a loop
	/// without a loop command. A reader that searched forwards from the current
	/// index would turn every backward jump into a fall-through to the end of
	/// the page.
	/// </para>
	/// <para>
	/// <strong>The index lands on the label, not after it.</strong>
	/// <c>index = idx</c> and the label itself does nothing, so the next step
	/// runs the label and then the instruction after it. Pointing past the label
	/// would work the same way — which is why this looks like a detail and is
	/// not: <em>the loop the search runs is what makes a missing label
	/// distinguishable from a label that is simply the next command.</em>
	/// </para>
	/// <para>
	/// <strong>A label that is not there leaves the page where it was</strong>,
	/// because the loop finds nothing and the assignment never runs. The
	/// reference is silent about it, and a reader that reported a failure would
	/// turn a page that merely falls through into a page that stops with an
	/// error — so this one says so and carries on.
	/// </para>
	/// </remarks>
	/// <summary>
	/// 11510 and 11550, from EasyRPG's <c>CommandPlayBGM</c> and
	/// <c>CommandPlaySound</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The parameters are <c>[fadeIn, volume, tempo, balance]</c> and the
	/// file name is in the command's string field, and <strong>the first
	/// parameter is the fade, not the name</strong> — the name arrives through
	/// the Maniac branch of <c>CommandStringOrVariableBitfield</c>, which reads
	/// the string field first and only falls back to a variable when the patch
	/// is active.
	/// </para>
	/// <para>
	/// <strong>Balance is 0 to 100 with 50 in the middle</strong>, and not
	/// -100 to 100: the editor writes 0 to 100 and the engine stores it as
	/// written, so a reader that treated the middle as 0 would call every
	/// centred track hard left.
	/// </para>
	/// <para>
	/// <strong>This is data and not sound.</strong> Nothing here has a player
	/// behind it, and a reader that reported a track as playing would be
	/// claiming a sound nobody can hear. The diagnostic says what was asked for
	/// and not that it was heard.
	/// </para>
	/// </remarks>
	private void ExecutePlayTrack(Rm2kMap.EventCommand pCmd)
	{
		var istBgm = pCmd.Code == PlayBGM;
		var label = istBgm ? "Play BGM" : "Play sound";
		// **CmdSetup gives the music a width of 4 and the effect a width of 3**,
		// and a first draft wrote 5 and 4. The counts are not the same as the
		// number of values: the music's four are fade, volume, tempo and
		// balance, and the effect's three are volume, tempo and balance. The
		// mode slot only appears when the Maniac patch is active, and the
		// function's own `assert(mode_idx != val_idx)` would not tolerate a
		// command that always carried it.
		var minWidth = istBgm ? 4 : 3;
		if (pCmd.Parameters.Count < minWidth)
		{
			Malformed(label);
			return;
		}
		if (string.IsNullOrEmpty(pCmd.Text))
		{
			_state.AddDiagnostic(
				$"[Event {_eventId}] {label}: no file name, and the string"
				+ " field is where the name lives; nothing was played");
			return;
		}
		var channel = istBgm
			? Rm2kChannel.BackgroundMusic
			: Rm2kChannel.SoundEffect;
		//
		// **The four numbers are `parameters[0..3]` and nothing else.** The
		// reference reads them through
		// `ValueOrVariableBitfield(com, 4, 0..3, 1..4)` and that function's
		// first line is `if (!IsPatchManiac()) return com.parameters[val_idx]`
		// — so *without the Maniac patch each value is simply its own
		// parameter*, and the fifth parameter exists only to hold four
		// two-bit mode fields when the patch is active.
		//
		// **A first draft read `parameters[1]` as the mode for the other three
		// and then refused every command whose values were non-zero** — which is
		// every music command a real game writes. Worse, the refusal was
		// dressed as caution: "this reader does not decode a bitfield yet" reads
		// as care, and it was actually a wrong reading of the source that had
		// not been read to the end. **Refusing loudly is not a substitute for
		// knowing.**
		//
		// A game that *does* carry the patch is refused here, and the reason
		// names the patch rather than the format — because the patch's values
		// are the same numbers read through four two-bit mode fields, and
		// reading them as plain numbers would produce a plausible volume that
		// is not the volume.
		if (_state.SupportsManiacPatch)
		{
			_state.AddDiagnostic(
				$"[Event {_eventId}] {label}: this game carries the Maniac patch,"
				+ " which packs the four values into mode fields this reader does"
				+ " not decode; nothing was played");
			return;
		}
		//
		// **The fade is parameters[0] on the music and there is none on the
		// effect**, so the two commands' lists do not line up: the music is
		// [fade, volume, tempo, balance] and the effect is [volume, tempo,
		// balance]. A first draft read both from the same offsets, which put
		// the effect's volume where its balance belongs.
		var fadeInTenths = istBgm ? Param(pCmd, 0) : 0;
		var volume = Param(pCmd, istBgm ? 1 : 0);
		var tempo = Param(pCmd, istBgm ? 2 : 1);
		var balance = Param(pCmd, istBgm ? 3 : 2);
		var cancelledFade = _state.Audio.Play(
			channel, pCmd.Text, volume, tempo, balance, fadeInTenths);
			// `Play` returns whether it *cancelled a fade*, so the track is read
			// back rather than assumed. **Reading the channel back is what tells a
			// refusal from a success** — the name is unique per play, so a channel
			// that does not hold this name is a channel nothing was written to.
			var track = istBgm ? _state.Audio.Bgm : _state.Audio.SoundEffect;
			if (track == null
				|| !string.Equals(track.Name, pCmd.Text, StringComparison.Ordinal))
				{
				_state.AddDiagnostic(
					$"[Event {_eventId}] {label}: refused \"{Truncate(pCmd.Text)}\" because"
					+ " one of the numbers was out of range");
				return;
			}
			_state.AddDiagnostic(
			$"[Event {_eventId}] {label}: {track}"
			+ (cancelledFade
				? ", and it cancelled a fade that was still running"
				: string.Empty));
	}

	/// <summary>
	/// 11520, Fade Out Background Music, from EasyRPG's
	/// <c>CommandFadeOutBGM</c>.
	/// </summary>
	/// <remarks>
	/// One parameter, the fade in tenths of a second, and it is passed straight
	/// to the audio system. <strong>Fading a channel that is silent is a
	/// no-op, not an error and not an empty track</strong> — the reference hands
	/// the number over and the audio system has nothing to fade, and a reader
	/// that invented a track to fade would be claiming a sound that was never
	/// asked for.
	/// </remarks>
	private void ExecuteFadeOutBGM(Rm2kMap.EventCommand pCmd)
	{
		if (pCmd.Parameters.Count < 1)
		{
			Malformed("Fade out BGM");
			return;
		}
		var tenths = ValueOrVariable(EventInterpreter.VarOperandConstant, pCmd.Parameters[0]);
		if (!_state.Audio.FadeOut(Rm2kChannel.BackgroundMusic, tenths))
		{
			_state.AddDiagnostic(
				$"[Event {_eventId}] Fade out BGM: nothing to fade, and the"
				+ " reference hands the number to a channel that has no track; the"
				+ $" fade of {tenths} was not started");
			return;
		}
		_state.AddDiagnostic(
			$"[Event {_eventId}] Fade out BGM over {tenths} tenths");
	}

	private void ExecuteJumpToLabel(Rm2kMap.EventCommand pCmd)
	{
		// CmdSetup minimum width 1.
		if (pCmd.Parameters.Count < 1)
		{
			Malformed("Jump to label");
			return;
		}
		var labelId = pCmd.Parameters[0];
		var found = -1;
		// **From the start of the page, not from here.** A backward jump is a
		// loop, and the reference's own loop begins at zero.
		for (var idx = 0; idx < _commands.Count; idx++)
		{
			if (_commands[idx].Code != Label)
			{
				continue;
			}
			if (_commands[idx].Parameters.Count == 0
				|| _commands[idx].Parameters[0] != labelId)
				{
				continue;
			}
			found = idx;
			break;
		}
		if (found < 0)
		{
			_state.AddDiagnostic(
				$"[Event {_eventId}] Jump to label {labelId}: no such label, and"
				+ " the reference leaves the page where it was");
			return;
		}
		// On the label itself, and **not** one past it. The engine's own rule
		// is `if (index_before_exec == frame->current_command) { ++; }`, so a
		// command that moved the index is not incremented again — and the label
		// is a no-op that costs a frame of its own before the instruction
		// behind it.
		//
		// A first draft pointed past the label and got a green suite, because
		// a no-op that is skipped and a no-op that runs differ only in a frame
		// nobody counts.
		_commandIndex = found;
		_state.AddDiagnostic(
			$"[Event {_eventId}] Jump to label {labelId} -> index {found}");
	}

	private void ExecuteChangeBattleCommands(Rm2kMap.EventCommand pCmd)
	{
		// EasyRPG: CmdSetup minimum width 4.
		if (pCmd.Parameters.Count < 4)
		{
			Malformed("Change battle commands");
			return;
		}
		var actors = ResolveActors(
			pCmd.Parameters[0], pCmd.Parameters[1], "Change battle commands");
		if (actors == null)
		{
			return;
		}
		var commandId = pCmd.Parameters[2];
		var add = pCmd.Parameters[3] != 0;
		foreach (var actorId in actors)
		{
			if (!_state.ChangeActorBattleCommand(actorId, commandId, add))
			{
				// **Nothing changed, and this reader says so.** The reference is
				// silent here, but a command that was asked to do something and
				// did not is exactly the case a diagnostic exists for — and the
				// two directions mean opposite things: "add what it already
				// has" is a game author's habit, "remove what it does not have"
				// is usually a mistake worth naming.
				_state.AddDiagnostic(
					$"[Event {_eventId}] Change battle commands: actor {actorId}"
					+ $" already {(add ? "has" : "lacks")} command {commandId},"
					+ $" so nothing changed");
				continue;
			}
			_state.AddDiagnostic(
				$"[Event {_eventId}] Change battle commands: actor {actorId}"
				+ $" {(add ? "gained" : "lost")} command {commandId}");
		}
	}

	/// <summary>
	/// 10430, Change Parameters, from EasyRPG's
	/// <c>CommandChangeParameters</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>It changes the <em>base</em>, not the current value</strong> —
	/// <c>SetBaseMaxHp</c> and <c>SetMaxHp</c> are different calls in the
	/// reference and this command is the first. <em>That is the whole
	/// difference</em>: the base survives a level change and a save, a buff
	/// does not, and a reader that wrote the current value would let a
	/// saved game keep a buff that ended three maps ago.
	/// </para>
	/// <para>
	/// The value goes through <c>OperateValue</c>, so the operation is in
	/// <c>parameters[2]</c>, the operand mode in <c>parameters[4]</c> and
	/// the operand itself in <c>parameters[5]</c>.
	/// </para>
	/// </remarks>
	private void ExecuteChangeParameters(Rm2kMap.EventCommand pCmd)
	{
		// CmdSetup minimum width 6.
		if (pCmd.Parameters.Count < 6)
		{
			Malformed("Change parameters");
			return;
		}
		var actors = ResolveActors(pCmd.Parameters[0], pCmd.Parameters[1], "Change parameters");
		if (actors == null)
		{
			return;
		}
		var operation = pCmd.Parameters[2];
		if (operation != VarOpSet && operation != VarOpAdd && operation != VarOpSub)
		{
			_state.AddDiagnostic(
				$"[Event {_eventId}] Change parameters: operation {operation}"
				+ " is not set, add or subtract, and the reference treats it as add");
			operation = VarOpAdd;
		}
		int operand;
		switch (pCmd.Parameters[4])
		{
			case VarOperandConstant:
				operand = pCmd.Parameters[5];
				break;
			case VarOperandVariable:
				operand = GetVariable(pCmd.Parameters[5]);
				break;
			case VarOperandVariableIndirect:
				if (pCmd.Parameters[5] < 1 || pCmd.Parameters[5] > GameSimulationState.MaxVariables)
			{
					_state.AddDiagnostic(
						$"[Event {_eventId}] Change parameters: invalid indirect variable"
						+ $" {pCmd.Parameters[5]} skipped");
					return;
			}
				operand = GetVariable(GetVariable(pCmd.Parameters[5]));
				break;
			default:
				_state.AddDiagnostic(
					$"[Event {_eventId}] Change parameters: operand mode"
					+ $" {pCmd.Parameters[4]} is not one of the three, and the constant"
					+ $" {pCmd.Parameters[5]} was used");
				operand = pCmd.Parameters[5];
				break;
		}
		// **OperateValue negates for the subtract operation**, and that is the
		// reference own helper and not a reader side switch.
		if (operation == VarOpSub)
		{
			operand = -operand;
		}
		var parameter = pCmd.Parameters[3];
		foreach (var actorId in actors)
		{
			var values = _state.GetOrCreateActorValues(actorId);
			var vorher = values.GetParameter(parameter);
			if (!values.AddToParameter(parameter, operand))
			{
				_state.AddDiagnostic(
					$"[Event {_eventId}] Change parameters: {parameter} is not one"
					+ " of the six, and nothing was changed");
				continue;
			}
			_state.AddDiagnostic(
				$"[Event {_eventId}] Change parameters: actor {actorId}"
				+ $" parameter {parameter} {vorher} -> {values.GetParameter(parameter)}"
				+ (vorher + operand != values.GetParameter(parameter)
					? ", and the difference is a clamp"
					: string.Empty));
		}
	}

	/// <summary>
	/// 10460 and 10470, Change HP and Change SP, from
	/// EasyRPG's <c>CommandChangeHP</c> and <c>CommandChangeSP</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>Parameters[2] is a "remove" flag and not an operation</strong>,
	/// and the reference negates the amount when it is set. A reader that
	/// read it as a sign would subtract when the game meant to add.
	/// </para>
	/// <para>
	/// <strong>HP and SP clamp differently and that is not an accident.</strong>
	/// HP has a lethal flag and a floor of one when it is not set, because
	/// a game can protect a hero from a hit. <c>SP</c> has neither: the
	/// reference writes <c>if (sp &lt; 0) sp = 0;</c> and nothing else, and a
	/// reader that gave SP the same floor as HP would leave a hero unable to
	/// cast anything.
	/// </para>
	/// <para>
	/// <strong>The ceiling is the current maximum, not the base</strong>, and
	/// that is the reason the base lives in its own place: a hero with a base
	/// of 40 and equipment worth 10 cannot be healed past 50.
	/// </para>
	/// </remarks>
	private void ExecuteChangeHpOrSp(Rm2kMap.EventCommand pCmd, bool pIsHp)
	{
		var label = pIsHp ? "Change HP" : "Change SP";
		// **HP needs six parameters and SP five**: the sixth is the lethal flag
		// and SP has none, so the two commands do not line up.
		var minWidth = pIsHp ? 6 : 5;
		if (pCmd.Parameters.Count < minWidth)
		{
			Malformed(label);
			return;
		}
		var actors = ResolveActors(pCmd.Parameters[0], pCmd.Parameters[1], label);
		if (actors == null)
		{
			return;
		}
		// **A remove flag and not a sign.** The reference writes
		// `bool remove = com.parameters[2] != 0; if (remove) amount = -amount;`.
		var remove = pCmd.Parameters[2] != 0;
		var amount = ValueOrVariable(Param(pCmd, 3), Param(pCmd, 4));
		if (remove)
		{
			amount = -amount;
		}
		var lethal = pIsHp && pCmd.Parameters[5] != 0;
		foreach (var actorId in actors)
		{
			if (pIsHp)
			{
				var vorher = _state.GetActorCurrentHp(actorId);
				var nachher = Rm2kActorValues.ChangeHp(
					vorher, amount, _state.GetOrCreateActorValues(actorId).BaseMaxHp, lethal);
				_state.CurrentHp[actorId] = nachher;
				_state.AddDiagnostic(
					$"[Event {_eventId}] {label}: actor {actorId}"
					+ $" {vorher} -> {nachher} by {amount},"
					+ (nachher == 1 && !lethal && vorher + amount < 1
						? ", and it stopped at one because the change was not lethal"
						: string.Empty));
			}
			else
			{
				var vorher = _state.GetActorCurrentSp(actorId);
				var nachher = Rm2kActorValues.ChangeSp(
					vorher, amount, _state.GetOrCreateActorValues(actorId).BaseMaxSp);
				_state.CurrentSp[actorId] = nachher;
				_state.AddDiagnostic(
					$"[Event {_eventId}] {label}: actor {actorId}"
					+ $" {vorher} -> {nachher} by {amount}");
			}
		}
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

	/// <summary>
	/// 11110, Show Picture, from EasyRPG's <c>CommandShowPicture</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The parameters are
	/// <c>[id, positionMode, x, y, fixedToMap, magnify, topTransparency,
	/// useTransparent, red, green, blue, saturation, effectMode, effectPower,
	/// bottomTransparency]</c>, and the last one is **optional**: the reference
	/// only reads it when the command carries more than 14 parameters.
	/// </para>
	/// <para>
	/// <strong>Position and the picture number can both be variables</strong>,
	/// and <c>parameters[1]</c> is not a mode number in the usual sense: it is
	/// the value's mode <em>and</em> the Maniac patch packs the X and Y origin
	/// into its upper bits, which the reference masks off with
	/// <c>ManiacBitmask(com.parameters[1], 0xFF)</c>. A reader that treated the
	/// whole number as a mode would read mode 257 where a game meant mode 1.
	/// </para>
	/// <para>
	/// <strong>The magnitude is one value, not two.</strong> The reference sets
	/// <c>magnify_height = magnify_width</c>, so the picture is square in the
	/// magnification and <c>parameters[6]</c> is the top colour, not a height.
	/// </para>
	/// </remarks>
	private void ExecuteShowPicture(Rm2kMap.EventCommand pCmd)
	{
		if (_presentation == null)
		{
			Malformed("Show picture");
			return;
		}
		// CmdSetup minimum width 14. A command that is shorter is a truncated
		// file and not a picture with defaults.
		if (pCmd.Parameters.Count < 14)
		{
			Malformed("Show picture");
			return;
		}
		var pictureId = ValueOrVariable(Param(pCmd, 1), pCmd.Parameters[0]);
		// The mode lives in the low byte; the Maniac patch packs the origin
		// into the rest.
		var positionMode = pCmd.Parameters[1] & 0xFF;
		var x = ValueOrVariable(positionMode, pCmd.Parameters[2]);
		var y = ValueOrVariable(positionMode, pCmd.Parameters[3]);
		//
		// **The Maniac bitmask applies to the bottom transparency, and only to
		// the bottom one.** The reference masks parameters[14] with 0xFF because
		// the patch puts flags above the value, and it does *not* mask
		// parameters[6] — the top transparency is read whole. A first draft
		// masked both, which turned a top transparency of 100 into 100 and a
		// bottom of 100 into 100 by luck and any value above 255 into garbage.
		var topTransparency = Param(pCmd, 6);
		int? bottom = pCmd.Parameters.Count > 14
			? (pCmd.Parameters[14] & 0xFF)
			: null;

		var ok = _presentation.ShowPicture(
			pictureId, pCmd.Text, x, y,
			Param(pCmd, 4) > 0,
			Param(pCmd, 5),
			topTransparency,
			Param(pCmd, 7) > 0,
			Param(pCmd, 8), Param(pCmd, 9), Param(pCmd, 10),
			Param(pCmd, 11), Param(pCmd, 12), Param(pCmd, 13),
			bottom);
		if (!ok)
		{
			_state.AddDiagnostic(
				$"[Event {_eventId}] Show picture {pictureId} \"{Truncate(pCmd.Text)}\""
				+ " refused: a bound was out of range");
			return;
		}
		_state.AddDiagnostic(
			$"[Event {_eventId}] Show picture {pictureId} \"{Truncate(pCmd.Text)}\""
			+ $" at {x},{y}");
	}

	/// <summary>
	/// 11130, Erase Picture, from EasyRPG's <c>CommandErasePicture</c>.
	/// </summary>
	/// <remarks>
	/// The id can be a variable, and <strong>erasing a picture that is not there
	/// is not an error</strong> — the reference returns true regardless. A
	/// reader that reported a refusal would make a game that legitimately erases
	/// twice look broken, so this one says which of the two happened.
	/// </remarks>
		private void ExecuteErasePicture(Rm2kMap.EventCommand pCmd)
		{
		if (_presentation == null)
		{
			Malformed("Erase picture");
			return;
		}
		// CmdSetup minimum width 1 -- a bare id, with no mode at all, is the
		// old form and the common one.
		if (pCmd.Parameters.Count < 1)
		{
			Malformed("Erase picture");
			return;
		}
		// **The id comes first and the mode second.** A first draft read
		// parameters[0] as the mode and parameters[1] as the id, which is the
		// order the *show* command uses for its position mode and not the
		// order this one does -- so a one parameter command, the form the
		// editor writes most, erased the picture numbered by nothing at all.
		var mode = pCmd.Parameters.Count > 1 ? pCmd.Parameters[1] : 0;
		var first = pCmd.Parameters[0];
		int last;
		switch (mode)
		{
			case 0:
			case 1:
				last = ValueOrVariable(mode, first);
				break;
			case 2:
			case 3:
				// A range, and the end is parameters[2].
				last = pCmd.Parameters.Count > 2
					? ValueOrVariable(mode, pCmd.Parameters[2])
					: first;
				break;
			default:
				_state.AddDiagnostic(
					$"[Event {_eventId}] Erase picture: unsupported mode {mode},"
					+ $" erasing only {first}");
				last = ValueOrVariable(0, first);
				break;
		}
		// A range's start is always a plain number -- the reference reads
		// parameters[0] straight out -- and only the end goes through the
		// mode. Modes 2 and 3 therefore resolve both ends as constants, and a
		// reader that ran the start through the mode would look up a variable
		// the game never named.
		var start = mode is 2 or 3 ? first : ValueOrVariable(mode, first);
		var low = Math.Min(start, last);
		var high = Math.Max(start, last);
		var erased = 0;
		var missing = 0;
		for (var id = low; id <= high; id++)
		{
			if (_presentation.ErasePicture(id, out var hatte))
			{
				erased++;
				if (!hatte)
				{
					missing++;
				}
			}
		}
		if (erased == 0)
		{
			// **Nothing was in range at all, which is not the same as the id
			// being out of bounds.** `ErasePicture` returns false for an id of
			// zero or past the limit, and true for an id in range that holds no
			// picture -- so a run of ids that were all in range and all empty
			// is a success with nothing in it. A first draft reported both as
			// a refusal, which told a game author their command was out of
			// bounds when the truth was that they had already erased it.
			var ausserhalb = low < 1 || high > PresentationState.MaxPictures;
			_state.AddDiagnostic(
				$"[Event {_eventId}] Erase picture {low}"
				+ (low == high ? string.Empty : $"..{high}")
				+ (ausserhalb
					? " refused: the id is out of bounds"
					: ": there was none, and the reference treats that as success"));
			return;
		}
		// **Erasing a picture that is not there is success.** The reference
		// returns true regardless, and a reader that reported a refusal would
		// make a game that legitimately erases twice look broken -- and games
		// do erase twice, because a page runs and then runs again.
		_state.AddDiagnostic(
			$"[Event {_eventId}] Erase picture {low}"
			+ (low == high ? string.Empty : $"..{high}")
			+ $": {erased} removed"
			+ (missing > 0
				? $", {missing} of them were not there, and the reference"
					+ " treats that as success"
				: string.Empty));
	}

	/// <summary>
	/// Reads a parameter that is a constant or a variable, from
	/// <c>ValueOrVariable</c> and this project's own <c>TargetEval</c> modes.
	/// </summary>
	/// <remarks>
	/// <strong>The mode is the same three the branch command uses</strong>:
	/// constant, variable, and — in the branch command — the indirect forms. The
	/// picture command reads a variable directly, not an expression, so a mode
	/// it does not know is a diagnostic rather than a guess.
	/// </remarks>
	private int ValueOrVariable(int pMode, int pValue)
	{
		switch (pMode)
		{
			case TargetEvalSingle:
				return pValue;
			case VarOperandVariable:
				return GetVariable(pValue);
			default:
				_state.AddDiagnostic(
					$"[Event {_eventId}] Picture: unsupported value mode {pMode},"
					+ $" using the constant {pValue}");
				return pValue;
		}
	}

	/// <summary>
	/// 11030, Tint Screen, from EasyRPG's <c>CommandTintScreen</c>.
	/// </summary>
	/// <remarks>
	/// Parameters are <c>[red, green, blue, saturation, tenths, wait]</c>,
	/// and <strong>the duration is in tenths of a second</strong> — the
	/// reference converts it itself with <c>tenths * DEFAULT_FPS / 10</c>,
	/// so a reader that stored tenths would report a number the engine
	/// never had.
	/// <para>
	/// <strong>Saturation is a percentage where 100 means untinted.</strong> A
	/// reader that treated 0 as "no tint" would have tinted the screen to
	/// grey at the one value that means "leave it alone" — and 0 is what a
	/// game writes when it wants no tint.
	/// </para>
	/// <para>
	/// <strong>The wait is conditional and it is a wait.</strong> The
	/// reference calls <c>SetupWait(tenths)</c> only when the last parameter
	/// is non-zero, which holds the page for as long as the tint runs.
	/// </para>
	/// </remarks>
	/// <summary>
	/// 11310, Player Visibility, from EasyRPG's
	/// <c>CommandPlayerVisibility</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// **The parameter is inverted, and that is the whole command:**
	/// <c>parameters[0] == 0</c> means <em>hidden</em>. A reader that
	/// mapped a non-zero to visible got the right answer for a command that
	/// hides and the wrong one for a command that shows — and a game whose
	/// only use of this command is to hide a sprite would work until the
	/// first time it showed one again.
	/// </para>
	/// <para>
	/// <strong>It also resets the through-position</strong>, which the
	/// reference does with its own comment "RPG_RT does this here" — so
	/// a player who has walked through a wall and is then hidden does not
	/// stay standing in the wall.
	/// </para>
	/// </remarks>
	/// <summary>
	/// 10820, Memorize Location, from EasyRPG's <c>CommandMemorizeLocation</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The three parameters are <strong>the variables to write into</strong>,
	/// not the values: <c>parameters[0]</c> gets the map id,
	/// <c>parameters[1]</c> the player x and <c>parameters[2]</c> the y. A
	/// reader that read them as the position to store would write the
	/// player's tile into three variables and store nothing at all.
	/// </para>
	/// <para>
	/// <strong>There is no matching recall here.</strong> <c>10830</c> is in
	/// liblcf and EasyRPG has no method for it in this build, so a game that
	/// memorizes and then recalls would have the first half and not the
	/// second. That asymmetry is the reference's, and it is worth recording
	/// rather than implementing a half from imagination.
	/// </para>
	/// </remarks>
/// <summary>Which of the three player access rights a command addresses.</summary>
private enum AccessFlag
{
	Escape,
	Save,
	Menu,
}

/// <summary>
/// 11840, 11930 and 11960, from EasyRPG's <c>CommandChangeEscapeAccess</c>,
/// <c>CommandChangeSaveAccess</c> and <c>CommandChangeMainMenuAccess</c>.
/// </summary>
/// <remarks>
/// <para>
/// All three are one line in the reference —
/// <c>SetAllowEscape(com.parameters[0] != 0)</c> and its two siblings — and
/// <strong>that is the whole command</strong>. A zero is therefore not "no
/// change" but a removal: a cutscene that locks the menu and a cutscene that
/// unlocks it again write the same field, and **a reader that only ever set
/// it to true could never give a player their menu back.**
/// </para>
/// <para>
/// One parameter is the width, and the commands carry no more.
/// </para>
/// </remarks>
private void ExecuteAccessChange(Rm2kMap.EventCommand pCmd, AccessFlag pWhich)
{
	if (pCmd.Parameters.Count < 1)
	{
		Malformed("Change access");
		return;
	}
	var allowed = pCmd.Parameters[0] != 0;
	// **One call, and the two other flags come along unchanged.** A reader
	// that only ever wrote the flag its command named could never give a
	// player their menu back, and one that wrote all three from defaults
	// would unlock a cutscene the game had just locked.
	switch (pWhich)
	{
		case AccessFlag.Escape:
			_state.SetAccess(allowed, _state.AllowSave, _state.AllowMenu);
			break;
		case AccessFlag.Save:
			_state.SetAccess(_state.AllowEscape, allowed, _state.AllowMenu);
			break;
		case AccessFlag.Menu:
			_state.SetAccess(_state.AllowEscape, _state.AllowSave, allowed);
			break;
		default:
			_state.AddDiagnostic(
				$"[Event {_eventId}] Change access: the flag is not one of the three");
			return;
	}
	_state.AddDiagnostic(
		$"[Event {_eventId}] Change access: {pWhich} is now {allowed}, from a"
		+ $" {pCmd.Parameters[0]}");
}

	private void ExecuteMemorizeLocation(Rm2kMap.EventCommand pCmd)
	{
		// CmdSetup minimum width 3.
		if (pCmd.Parameters.Count < 3)
		{
			Malformed("Memorize location");
			return;
		}
		var varMap = pCmd.Parameters[0];
		var varX = pCmd.Parameters[1];
		var varY = pCmd.Parameters[2];
		// **All three must be writable**, and a variable id of 0 is not one —
		// a first draft would have written to index -1 and thrown, and a
		// game whose editor left a field empty would have stopped its event.
		foreach (var id in new int[] { varMap, varX, varY })
		{
			if (id < 1 || id > GameSimulationState.MaxVariables)
			{
				_state.AddDiagnostic(
					$"[Event {_eventId}] Memorize location: variable {id} is"
					+ " outside 1 to 50000, and nothing was stored");
				return;
			}
		}
		WriteVariable(varMap, _state.MapId);
		WriteVariable(varX, _state.MapX);
		WriteVariable(varY, _state.MapY);
		_state.AddDiagnostic(
			$"[Event {_eventId}] Memorized map {_state.MapId}"
			+ $" at {_state.MapX},{_state.MapY} into variables"
			+ $" {varMap}, {varX} and {varY}");
	}

	private void ExecutePlayerVisibility(Rm2kMap.EventCommand pCmd)
	{
		if (pCmd.Parameters.Count < 1)
		{
			Malformed("Player visibility");
			return;
}
		var hidden = pCmd.Parameters[0] == 0;
		_state.PlayerIsHidden = hidden;
		if (hidden)
		{
			// The reference calls ResetThrough here as well, and a hidden
			// player who has walked through a wall must not stay in it.
			_state.PlayerIsThrough = false;
		}
		_state.AddDiagnostic(
			$"[Event {_eventId}] Player is now"
			+ $" {(hidden ? "hidden" : "visible")}, because parameters[0] == 0"
			+ " is the hide case");
}

	/// <summary>
	/// 11330, Move Event, from EasyRPG's <c>CommandMoveEvent</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Parameters are <c>[eventId, moveFreq, idBitfield, skippable,
	/// route...]</c>, and <strong>the route is the rest of the list</strong>
	/// — the reference walks it from index four to the end. A reader that
	/// read a fixed count would silently drop a long route, and the game
	/// would run a shortened version of what the author wrote.
	/// </para>
	/// <para>
	/// <strong>A move frequency outside 1 to 8 becomes 6</strong>, and that
	/// is the reference own default rather than a refusal: <c>if (move_freq
	/// &lt;= 0 || move_freq &gt; 8) move_freq = 6;</c>. A reader that
	/// refused would stop a route RPG_RT happily runs.
	/// </para>
	/// <para>
	/// <strong>The id mode and the repeat flag share one word.</strong> The
	/// mode is the low two bits of the third parameter and the repeat is
	/// its low bit, so a reader that took the whole number as the mode
	/// would read mode 3 where a game meant mode 1 and a repeat.
	/// </para>
	/// </remarks>
	private void ExecuteMoveEvent(Rm2kMap.EventCommand pCmd)
	{
		// CmdSetup minimum width 4.
		if (pCmd.Parameters.Count < 4)
		{
			Malformed("Move event");
			return;
		}
		if (_moveRouteStarter == null)
		{
			_state.AddDiagnostic(
				$"[Event {_eventId}] Move event: this interpreter cannot reach a"
				+ " character, so no route was started; the steps are still in the"
				+ " file");
			return;
}
		var idBitfield = pCmd.Parameters[2];
		var eventId = ValueOrVariable(idBitfield & 0x3, pCmd.Parameters[0]);
		var repeat = (idBitfield & 0x1) != 0;
		var moveFreq = pCmd.Parameters[1];
		if (moveFreq <= 0 || moveFreq > 8)
		{
			// The reference own fallback, not a refusal.
			moveFreq = 6;
}
		var skippable = pCmd.Parameters[3] != 0;
		var route = new List<Rm2kMap.MoveCommand>();
		for (var i = 4; i < pCmd.Parameters.Count; i++)
		{
			route.Add(new Rm2kMap.MoveCommand(pCmd.Parameters[i]));
}
		var started = _moveRouteStarter(
			eventId, moveFreq, route, repeat, skippable);
		if (!started)
		{
			_state.AddDiagnostic(
				$"[Event {_eventId}] Move event {eventId}: no character carries"
				+ " that id, and the reference logs a warning and touches nobody;"
				+ $" the route had {route.Count} steps");
			return;
}
		_state.AddDiagnostic(
			$"[Event {_eventId}] Move event {eventId}: {route.Count} steps"
			+ $" at frequency {moveFreq}, repeating is {repeat},"
			+ $" skippable is {skippable}");
}

	private void ExecuteTintScreen(Rm2kMap.EventCommand pCmd)
	{
		if (_presentation == null)
		{
			Malformed("Tint screen");
			return;
}
		// CmdSetup minimum width 6.
		if (pCmd.Parameters.Count < 6)
		{
			Malformed("Tint screen");
			return;
}
		var tenths = pCmd.Parameters[4];
		if (tenths < 0 || tenths > EventInterpreterMaxTenths)
		{
			_state.AddDiagnostic(
				$"[Event {_eventId}] Tint screen: {tenths} tenths is outside"
				+ " the format's 0 to 100; nothing was tinted");
			return;
}
		// The reference's own conversion. **The multiplier is the frame rate
		// and not a literal**, and a reader that hardcoded one would disagree
		// with a game that declared another.
		var frames = tenths * 60 / 10;
		if (!_presentation.TintScreen(
			Param(pCmd, 0), Param(pCmd, 1), Param(pCmd, 2), Param(pCmd, 3), frames))
			{
			_state.AddDiagnostic(
				$"[Event {_eventId}] Tint screen refused: a channel was outside"
				+ " 0 to 255 or the saturation outside 0 to 100");
			return;
}
		_state.AddDiagnostic(
			$"[Event {_eventId}] Tint screen r{Param(pCmd, 0)}"
			+ $" g{Param(pCmd, 1)} b{Param(pCmd, 2)}"
			+ $" saturation {Param(pCmd, 3)} over {tenths} tenths"
			+ $", which is {frames} frames");
		if (Param(pCmd, 5) != 0)
		{
			_waitFramesRemaining = tenths * 6;
}
}

	/// <summary>
	/// 11010 and 11020, Erase and Show Screen, from EasyRPG's
	/// <c>CommandEraseScreen</c> and <c>CommandShowScreen</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// One parameter, the transition kind. <strong>Parameter -1 is not a
	/// kind</strong> — it means "the game's own teleport transition", which
	/// comes from the editor's settings and is not in the command at all.
	/// This reader has not read those settings, so it says so rather than
	/// falling through to none, because <strong>a silent fall-through is
	/// what the reference does for a number it does not know, and it makes
	/// every teleport in a game lose its transition without a word.</strong>
	/// </para>
	/// <para>
	/// <strong>A message blocks the transition</strong> — both commands
	/// return false while one is open — and so does a requested battle or
	/// game-over scene. This reader holds the page for the message, because
	/// the reference does; the pending scene is not modelled here yet.
	/// </para>
	/// </remarks>
	private void ExecuteScreenTransition(Rm2kMap.EventCommand pCmd)
	{
		var istShow = pCmd.Code == ShowScreen;
		var label = istShow ? "Show screen" : "Erase screen";
		if (_presentation == null)
		{
			Malformed(label);
			return;
}
		if (pCmd.Parameters.Count < 1)
		{
			Malformed(label);
			return;
}
		if (_presentation.MessageVisible)
		{
			_state.AddDiagnostic(
				$"[Event {_eventId}] {label}: a message is open, and the"
				+ " reference holds the transition until it is not; nothing happened");
			_waitFramesRemaining = 1;
			return;
}
		var richtung = istShow
			? Rm2kTransitionDirection.Show
			: Rm2kTransitionDirection.Erase;
		var ergebnis = Rm2kTransitionKind.FromParameter(pCmd.Parameters[0], richtung);
		switch (ergebnis.Kind)
		{
			case Rm2kTransitionRequestResultKind.Named:
				if (!_presentation.RequestTransition(richtung, ergebnis.Transition))
				{
					_state.AddDiagnostic(
						$"[Event {_eventId}] {label}: refused a transition this"
						+ " presentation does not define");
					return;
}
				_state.AddDiagnostic(
					$"[Event {_eventId}] {label}: {ergebnis.Transition}");
				return;
			case Rm2kTransitionRequestResultKind.GameWideTransition:
				_state.AddDiagnostic(
					$"[Event {_eventId}] {label}: parameter -1 is the game's"
					+ " own teleport transition from the editor's settings, and this"
					+ " reader has not read those; no transition was chosen");
				return;
			default:
				_state.AddDiagnostic(
					$"[Event {_eventId}] {label}: parameter {pCmd.Parameters[0]}"
					+ " names no transition in either table, and the reference falls"
					+ " through to none without saying so; none was chosen");
				return;
}
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
