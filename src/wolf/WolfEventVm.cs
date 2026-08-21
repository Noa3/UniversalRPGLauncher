using System;
using System.Collections.Generic;
using UniversalRPG.Plugins;

namespace UniversalRPG.Wolf;

public enum WolfVmState
{
    NotStarted,
    Running,
    Waiting,
    Completed,
    Faulted,
}

/// <summary>
/// Deterministic, data-only WOLF event VM foundation. It executes only the
/// explicitly modelled synthetic commands; unknown operations fail with a
/// diagnostic instead of being guessed or forwarded to a host process.
/// </summary>
public sealed class WolfEventVm
{
    public const int MaxCommandsPerTick = 256;

    private readonly Dictionary<int, int> _variables = new();
    private readonly Dictionary<int, bool> _switches = new();
    private readonly List<WolfEventMessage> _messages = new();
    private readonly List<string> _trace = new();
    private WolfEventProgram? _program;
    private int _instructionIndex;
    private int _waitRemaining;
    private int _pendingChoiceIndex = -1;
    private long _messageSequence;

    public WolfVmState State { get; private set; } = WolfVmState.NotStarted;
    public int CurrentEventId => _program?.Id ?? 0;
    public int InstructionIndex => _instructionIndex;
    public int WaitRemainingFrames => _waitRemaining;
    public IReadOnlyDictionary<int, int> Variables => _variables;
    public IReadOnlyDictionary<int, bool> Switches => _switches;
    public IReadOnlyList<WolfEventMessage> Messages => _messages;
    public IReadOnlyList<string> Trace => _trace;
    public IReadOnlyList<string> PendingChoices { get; private set; } = Array.Empty<string>();
    public int SelectedChoiceIndex { get; private set; } = -1;
    public WolfTransferRequest? PendingTransfer { get; private set; }
    public PluginError? LastError { get; private set; }

    public PluginOperationResult Start(WolfEventProgram pProgram)
    {
        if (pProgram == null)
        {
            return Fail("A WOLF event program is required before starting the VM.", "start");
        }
        if (State is WolfVmState.Running or WolfVmState.Waiting)
        {
            return Fail("The WOLF event VM is already running.", "start");
        }
        _program = pProgram;
        _instructionIndex = 0;
        _waitRemaining = 0;
        _pendingChoiceIndex = -1;
        PendingChoices = Array.Empty<string>();
        SelectedChoiceIndex = -1;
        PendingTransfer = null;
        LastError = null;
        State = WolfVmState.Running;
        _trace.Add($"event:{pProgram.Id}:start");
        return PluginOperationResult.Succeeded();
    }

    public PluginOperationResult StepTick()
    {
        if (State == WolfVmState.Completed)
        {
            return PluginOperationResult.Succeeded();
        }
        if (State == WolfVmState.Faulted || State == WolfVmState.NotStarted)
        {
            return Fail($"The WOLF event VM cannot step from state {State}.", "tick");
        }
        if (State == WolfVmState.Waiting)
        {
            if (_pendingChoiceIndex < 0 && _waitRemaining > 0)
            {
                _waitRemaining -= 1;
                if (_waitRemaining == 0)
                {
                    State = WolfVmState.Running;
                }
            }
            return PluginOperationResult.Succeeded();
        }

        if (_program == null)
        {
            return Fail("The WOLF event VM has no active program.", "tick");
        }
        var executed = 0;
        while (State == WolfVmState.Running && executed < MaxCommandsPerTick)
        {
            if (_instructionIndex < 0 || _instructionIndex >= _program.Commands.Count)
            {
                State = WolfVmState.Completed;
                _trace.Add($"event:{_program.Id}:complete");
                return PluginOperationResult.Succeeded();
            }
            var command = _program.Commands[_instructionIndex];
            var result = Execute(command);
            if (!result.Success)
            {
                return result;
            }
            executed += 1;
        }
        if (State == WolfVmState.Running && executed >= MaxCommandsPerTick)
        {
            return Fail("The WOLF event VM exceeded its per-tick command budget.", "tick");
        }
        return PluginOperationResult.Succeeded();
    }

    public PluginOperationResult SelectChoice(int pChoiceIndex)
    {
        if (State != WolfVmState.Waiting || _pendingChoiceIndex < 0)
        {
            return Fail("The WOLF event VM is not waiting for a choice.", "choice");
        }
        if (pChoiceIndex < 0 || pChoiceIndex >= PendingChoices.Count)
        {
            return Fail("The selected WOLF choice is outside the available range.", "choice");
        }
        SelectedChoiceIndex = pChoiceIndex;
        _pendingChoiceIndex = -1;
        PendingChoices = Array.Empty<string>();
        _instructionIndex += 1;
        State = WolfVmState.Running;
        _trace.Add($"event:{CurrentEventId}:choice:{pChoiceIndex}");
        return PluginOperationResult.Succeeded();
    }

    public void SetVariable(int pId, int pValue) => _variables[pId] = pValue;

    public int GetVariable(int pId) => _variables.TryGetValue(pId, out var value) ? value : 0;

    public void SetSwitch(int pId, bool pValue) => _switches[pId] = pValue;

    public bool GetSwitch(int pId) => _switches.TryGetValue(pId, out var value) && value;

    public void ResetState()
    {
        _program = null;
        _instructionIndex = 0;
        _waitRemaining = 0;
        _pendingChoiceIndex = -1;
        _messageSequence = 0;
        _variables.Clear();
        _switches.Clear();
        _messages.Clear();
        _trace.Clear();
        PendingChoices = Array.Empty<string>();
        SelectedChoiceIndex = -1;
        PendingTransfer = null;
        LastError = null;
        State = WolfVmState.NotStarted;
    }

    private PluginOperationResult Execute(WolfEventCommand pCommand)
    {
        switch (pCommand.Opcode)
        {
            case WolfEventOpcode.Message:
                _messages.Add(new WolfEventMessage
                {
                    Sequence = ++_messageSequence,
                    EventId = CurrentEventId,
                    Text = pCommand.Text,
                });
                _trace.Add($"event:{CurrentEventId}:message");
                _instructionIndex += 1;
                return PluginOperationResult.Succeeded();
            case WolfEventOpcode.SetVariable:
                _variables[pCommand.Operand] = pCommand.Value;
                _instructionIndex += 1;
                return PluginOperationResult.Succeeded();
            case WolfEventOpcode.AddVariable:
                _variables[pCommand.Operand] = GetVariable(pCommand.Operand) + pCommand.Value;
                _instructionIndex += 1;
                return PluginOperationResult.Succeeded();
            case WolfEventOpcode.SetSwitch:
                _switches[pCommand.Operand] = pCommand.Value != 0;
                _instructionIndex += 1;
                return PluginOperationResult.Succeeded();
            case WolfEventOpcode.IfSwitch:
                return Branch(GetSwitch(pCommand.Operand) == (pCommand.Value != 0), pCommand);
            case WolfEventOpcode.IfVariable:
                return Branch(GetVariable(pCommand.Operand) == pCommand.Value, pCommand);
            case WolfEventOpcode.Wait:
                _instructionIndex += 1;
                _waitRemaining = pCommand.Frames;
                if (_waitRemaining > 0)
                {
                    State = WolfVmState.Waiting;
                }
                return PluginOperationResult.Succeeded();
            case WolfEventOpcode.Choice:
                if (pCommand.Choices.Count == 0)
                {
                    _instructionIndex += 1;
                    return PluginOperationResult.Succeeded();
                }
                _pendingChoiceIndex = _instructionIndex;
                PendingChoices = pCommand.Choices;
                SelectedChoiceIndex = -1;
                State = WolfVmState.Waiting;
                return PluginOperationResult.Succeeded();
            case WolfEventOpcode.Transfer:
                PendingTransfer = new WolfTransferRequest
                {
                    MapId = pCommand.MapId,
                    X = pCommand.X,
                    Y = pCommand.Y,
                };
                _trace.Add($"event:{CurrentEventId}:transfer:{pCommand.MapId}:{pCommand.X}:{pCommand.Y}");
                _instructionIndex += 1;
                return PluginOperationResult.Succeeded();
            case WolfEventOpcode.End:
                _instructionIndex = _program?.Commands.Count ?? 0;
                State = WolfVmState.Completed;
                _trace.Add($"event:{CurrentEventId}:end");
                return PluginOperationResult.Succeeded();
            case WolfEventOpcode.Unknown:
            default:
                return Fail($"Unsupported WOLF event operation '{pCommand.RawOperation}'.", "command");
        }
    }

    private PluginOperationResult Branch(bool pCondition, WolfEventCommand pCommand)
    {
        if (pCondition)
        {
            _instructionIndex += 1;
            return PluginOperationResult.Succeeded();
        }
        if (pCommand.JumpIndex < 0 || _program == null || pCommand.JumpIndex >= _program.Commands.Count)
        {
            return Fail("A WOLF event branch target is outside the command list.", "command");
        }
        _instructionIndex = pCommand.JumpIndex;
        return PluginOperationResult.Succeeded();
    }

    private PluginOperationResult Fail(string pMessage, string pPhase)
    {
        LastError = PluginError.Create(PluginErrorCode.LifecycleFailure, pMessage, EnginePluginIds.WolfRpg, pPhase);
        State = WolfVmState.Faulted;
        return PluginOperationResult.Failed(LastError);
    }
}
