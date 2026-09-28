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

    /// <summary>
    /// The four variable bands, kept apart.
    /// </summary>
    /// <remarks>
    /// **Four dictionaries and not one flat map.** A reader that kept every
    /// band in one dictionary would have a self variable and a system
    /// variable with the same index collide, and the collision is silent —
    /// both reads answer with a number, and the wrong one.
    /// </remarks>
    private readonly WolfVariableBands _variables = new();

    /// <summary>The bands, for a caller that wants to address one directly.</summary>
    public WolfVariableBands VariableBands => _variables;
    /// <summary>
    /// The map and common event switches, kept apart.
    /// </summary>
    /// <remarks>
    /// <strong>Two maps and not one.</strong> The help's page-call note says
    /// that 0 and above address a map event and 500,000 and above address a
    /// common event, so a map event and a common event with the same switch
    /// number are different switches. One dictionary would have them collide.
    /// </remarks>
    private readonly Dictionary<int, bool> _mapSwitches = new();

    /// <summary>The common event switches, from 500,000 up.</summary>
    private readonly Dictionary<int, bool> _commonSwitches = new();

    /// <summary>The first number the common event switches use.</summary>
    public const int CommonSwitchBase = 500_000;

    /// <summary>The highest switch number, from the editor's own bound.</summary>
    public const int MaxSwitch = 499_999;

    /// <summary>
    /// The map a switch number belongs to, or -1 when it is out of range.
    /// </summary>
    /// <remarks>
    /// <strong>The base is 500,000 and not a million.</strong> The help names
    /// it in the page-call note: 0 and above for map events, 500,000 and above
    /// for common events. A reader that used the variable bands' million for
    /// switches would put every common switch into the map range and lose it.
    /// </remarks>
    public static int SwitchMapOf(int pNumber)
    {
        if (pNumber >= 0 && pNumber <= MaxSwitch)
        {
            return 0;
        }
        return pNumber >= CommonSwitchBase && pNumber <= CommonSwitchBase + MaxSwitch
            ? 1
            : -1;
    }

    /// <summary>The index within its map, or -1 when the number is out of range.</summary>
    public static int SwitchIndexOf(int pNumber)
    {
        var map = SwitchMapOf(pNumber);
        return map < 0 ? -1 : pNumber - (map == 0 ? 0 : CommonSwitchBase);
    }

    private Dictionary<int, bool> SwitchStore(int pMap)
    {
        return pMap == 0 ? _mapSwitches : _commonSwitches;
    }
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
    /// <summary>
    /// The normal band, for a caller that only ever means a normal variable.
    /// </summary>
    /// <remarks>
    /// <strong>A band and not the old flat map</strong>, because a caller that
    /// reached for "a variable" without saying which one is asking the
    /// question WOLF answers with four.
    /// </remarks>
    public WolfVariableBands Variables => _variables;
    /// <summary>
    /// The map switches, for a caller that only ever means a map event.
    /// </summary>
    /// <remarks>
    /// <strong>The common switches are not in here</strong>, because a caller
    /// that iterates "all switches" to save them would silently leave the
    /// common ones behind, and a game would come back from a save with its
    /// common event progress gone.
    /// </remarks>
    public IReadOnlyDictionary<int, bool> Switches => _mapSwitches;

    /// <summary>The common event switches, for a caller that means those.</summary>
    public IReadOnlyDictionary<int, bool> CommonSwitches => _commonSwitches;
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
            // **A command that jumps, jumps.** The last command of a branch arm
            // carries this, because an arm has to step over the other arm and
            // an ordinary increment runs straight into it. -1 is the ordinary
            // "next command", and the check is here rather than inside Execute
            // so a command that returned early — an End, a wait — keeps the
            // index it set for itself.
            if (command.NextIndex >= 0)
            {
                if (command.NextIndex >= _program.Commands.Count)
                {
                    return Fail(
                        $"A WOLF command jumps to {command.NextIndex}, which is"
                        + $" outside the {_program.Commands.Count} commands.", "command");
                }
                _instructionIndex = command.NextIndex;
            }
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

    /// <summary>Writes the normal band, for a caller that means one.</summary>
    public void SetVariable(int pId, int pValue)
        => _variables.Set(WolfVariable.BandNormal, pId, pValue);

    /// <summary>Reads the normal band.</summary>
    public int GetVariable(int pId)
        => _variables.Get(WolfVariable.BandNormal, pId);

    /// <summary>
    /// Applies a variable command's operator to the destination and stores it.
    /// </summary>
    /// <returns>
    /// False when the destination refused the write, which happens only when
    /// the destination is not a reference at all.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <strong>One path for every operator</strong>, because the destination is
    /// resolved through the bands once and the operator decides what happens to
    /// it. A switch per operator would resolve the destination thirteen times
    /// and would need a thirteenth case the moment the editor adds one.
    /// </para>
    /// <para>
    /// <strong>The current value is read before the write,</strong> and that is
    /// the whole reason the resolve happens first: a right hand side that names
    /// the same variable as the destination has to see the old value, the way
    /// the editor evaluates it, or a doubling command would read the new one.
    /// </para>
    /// </remarks>
    private bool ApplyOperator(
        WolfEventCommand pCommand,
        int pOperator)
    {
        var current = _variables.Resolve(pCommand.Operand);
        var right = _variables.Resolve(pCommand.Value);
        // **A second number is only read when the file gave one.** The arc
        // tangent takes two vectors and a file that gives one of them is not a
        // file that gives a zero.
        var right2 = pCommand.HasRight2 ? _variables.Resolve(pCommand.Right2) : 0;
        // **The arc tangent is the one operator that reads two right hand
        // sides and not the current value.** The help says the right hand
        // side's two variables are the X and the Y vector; the current value
        // is the destination and has no part in the slope. Sending the
        // current value as X would make the angle of a slope depend on what
        // the destination already held, which no game means.
        var result = pOperator == WolfVariableOperator.ArcTangent
            ? WolfVariableOperator.ArcTangentOf(
                pCommand.HasRight2 ? right : current, right2)
            : WolfVariableOperator.Apply(pOperator, current, right);
        return _variables.SetByReference(pCommand.Operand, result);
    }

    /// <summary>Writes a switch by its own number, map or common.</summary>
    public void SetSwitch(int pId, bool pValue)
    {
        var map = SwitchMapOf(pId);
        // **An out of range switch changes nothing and says nothing.** A reader
        // that grew a dictionary would store a switch the editor cannot hold,
        // and the next load would not carry it.
        if (map >= 0)
        {
            SwitchStore(map)[SwitchIndexOf(pId)] = pValue;
        }
    }

    /// <summary>Reads a switch by its own number, map or common.</summary>
    public bool GetSwitch(int pId)
    {
        var map = SwitchMapOf(pId);
        return map >= 0
            && SwitchStore(map).TryGetValue(SwitchIndexOf(pId), out var value)
            && value;
    }

    public void ResetState()
    {
        _program = null;
        _instructionIndex = 0;
        _waitRemaining = 0;
        _pendingChoiceIndex = -1;
        _messageSequence = 0;
        _variables.Clear();
        _mapSwitches.Clear();
        _commonSwitches.Clear();
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
                if (!ApplyOperator(pCommand, pCommand.Operator))
                {
                    return PluginOperationResult.Succeeded();
                }
                _instructionIndex += 1;
                return PluginOperationResult.Succeeded();
            case WolfEventOpcode.AddVariable:
                // **The two operators are two opcodes over one path.** The
                // editor writes addition as its own entry in the operator
                // list, so a program that had a switch per opcode would need a
                // thirteenth case for every operator the editor adds, and the
                // two lists would drift apart.
                if (!ApplyOperator(
                        pCommand, WolfVariableOperator.Add))
                {
                    return PluginOperationResult.Succeeded();
                }
                _instructionIndex += 1;
                return PluginOperationResult.Succeeded();
            case WolfEventOpcode.SetSwitch:
                // **Through the map, and a switch number that is in neither
                // range changes nothing.** The write is not a dictionary
                // assignment here, so a switch number the editor cannot hold
                // is dropped rather than stored where a load would not find it.
                var switchMap = SwitchMapOf(pCommand.Operand);
                if (switchMap >= 0)
                {
                    SwitchStore(switchMap)[SwitchIndexOf(pCommand.Operand)] =
                        pCommand.Value != 0;
                }
                _instructionIndex += 1;
                return PluginOperationResult.Succeeded();
            case WolfEventOpcode.IfSwitch:
                // **An out of range switch reads as off, and not as a
                // condition that cannot be answered.** The help's condition
                // list is "on" or "off" and nothing else, so a switch that does
                // not exist is off — which is also what a game that has not set
                // it yet expects.
                return Branch(
                    GetSwitch(pCommand.Operand) == (pCommand.Value != 0), pCommand);
            case WolfEventOpcode.IfVariable:
                // **Seven comparisons, and not one.** A reader that kept the
                // old equality test would take one branch in seven, and a chest
                // guarded by "V0 is at least 1" would never open.
                // **Both sides go through the bands.** The left one is the
                // operand and it may name any band; the right one is the
                // compared value and the help says it may be a variable too
                // (2000000 means normal variable 0). A reader that resolved
                // only the left side would compare a normal variable against
                // the *number* two million instead of against what it holds.
                if (!WolfComparisonEvaluator.TryEvaluate(
                    pCommand.Comparison,
                    _variables.Resolve(pCommand.Operand),
                    _variables.Resolve(pCommand.Value),
                    out var variableCondition))
                {
                    return Fail(
                        $"A WOLF variable branch uses comparison"                        + $" {pCommand.Comparison}, which is not one of the seven"
                        + " the editor offers.", "command");
                }
                return Branch(variableCondition, pCommand);
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
        // **Two targets, one per arm.** The fall-through used to be the true
        // arm and the single jump the false one, which meant that when the
        // condition held the true arm ran *and* the false arm ran.
        //
        // The arm a command belongs to is decided by the *arm ranges* the
        // branch declares, not by a single end index: the true arm runs from
        // its own start to where the other arm begins, and the false arm runs
        // from there to the end. That is what makes a one-command arm end
        // where it ends, and it needs no hidden state — a program with two
        // branches in a row cannot leak one into the other.
        if (pCondition)
        {
            if (pCommand.TrueJumpIndex < 0)
            {
                _instructionIndex += 1;
                return PluginOperationResult.Succeeded();
            }
            return JumpTo(pCommand.TrueJumpIndex, "true arm");
        }
        if (pCommand.JumpIndex < 0)
        {
            _instructionIndex += 1;
            return PluginOperationResult.Succeeded();
        }
        return JumpTo(pCommand.JumpIndex, "false arm");
    }

    /// <summary>
    /// Jumps to a branch arm and remembers where that arm ends.
    /// </summary>
    private PluginOperationResult JumpTo(int pTarget, string pWhich)
    {
        if (_program == null || pTarget >= _program.Commands.Count)
        {
            return Fail(
                $"A WOLF event branch {pWhich} target {pTarget} is outside the"
                + $" command list of {_program?.Commands.Count ?? 0} commands.",
                "command");
        }
        _instructionIndex = pTarget;
        return PluginOperationResult.Succeeded();
    }

private PluginOperationResult Fail(string pMessage, string pPhase)
    {
        LastError = PluginError.Create(PluginErrorCode.LifecycleFailure, pMessage, EnginePluginIds.WolfRpg, pPhase);
        State = WolfVmState.Faulted;
        return PluginOperationResult.Failed(LastError);
    }
}
