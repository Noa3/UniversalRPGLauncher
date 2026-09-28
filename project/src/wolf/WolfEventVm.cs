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
    /// How deep a call may nest.
    /// </summary>
    /// <remarks>
    /// <strong>64 and not "as deep as the file says".</strong> A common event that
    /// calls itself — directly or through a ring of two others — would run until
    /// the process ended, and the limit is what turns that into a failure with a
    /// name in it. WOLF's own nesting is far shallower than this; the number is
    /// a bound and not a claim about the editor.
    /// </remarks>
    public const int MaxCallDepth = 64;

    /// <summary>
    /// The common events a call can reach, by database id.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>A call needs somewhere to call to, and the table is that
    /// place.</strong> The project data carries the common events as programs,
    /// the same shape as a map event, and a reader that had no table could only
    /// report that the call was unsupported.
    /// </para>
    /// <para>
    /// <strong>Set once, before the first start.</strong> A table that could be
    /// replaced mid-run would have a call that resolved differently from the
    /// same call a frame later, and a game that changed its own event library
    /// while an event ran would be a thing no test could pin down.
    /// </para>
    /// </remarks>
    public IReadOnlyList<WolfEventProgram> CommonEvents { get; set; } =
        Array.Empty<WolfEventProgram>();

    /// <summary>
    /// The return value of the last common event that finished, if it had one.
    /// </summary>
    /// <remarks>
    /// <strong>Set by a call and cleared by the next one.</strong> A call whose
    /// event returns nothing leaves this alone rather than writing zero, because
    /// a reader that wrote zero would make a call that returns nothing
    /// indistinguishable from a call that returns zero — and a common event
    /// used as a function would then read a zero the game never produced.
    /// </remarks>
    public int? LastCommonEventResult { get; private set; }

    /// <summary>
    /// One frame of an unfinished call, so a call can come back.
    /// </summary>
    /// <remarks>
    /// <strong>The program and the index, and nothing else.</strong> The state
    /// the VM has — the variables, the board, the choices — belongs to the
    /// caller and is shared, which is the point of a call: a common event that
    /// sets a variable changes the game, not a copy of it. So a frame is only
    /// the place to come back to.
    /// </remarks>
    private readonly Stack<WolfCallFrame> _callStack = new();

    /// <summary>One suspended caller.</summary>
    private sealed class WolfCallFrame
    {
        public required WolfEventProgram Program { get; init; }

        /// <summary>Where to carry on, not including the call itself.</summary>
        public required int ResumeIndex { get; init; }

        /// <summary>Whether this caller waits for the call to finish.</summary>
        public required bool Waits { get; init; }
    }

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

    /// <summary>
    /// The characters on the map and the routes they are running.
    /// </summary>
    /// <remarks>
    /// **A board of its own and not a field the caller fills in.** The move
    /// route reader produces routes, the route type table verifies the steps,
    /// and until this card nothing held a figure to run them on — so a game
    /// with a patrol route loaded and stood still, and the suite stayed green
    /// because it only ever read steps.
    /// </remarks>
    public WolfCharacterBoard Board { get; }

    /// <summary>Creates a VM whose board shares this VM's variable bands.</summary>
    /// <remarks>
    /// **The board gets this VM's bands and not its own.</strong> A route step
    /// that stores to a variable has to write where the event can read it, and
    /// two band sets would mean a route's value vanished into a store nobody
    /// looks at.
    /// </remarks>
    public WolfEventVm()
    {
        Board = new WolfCharacterBoard(_variables);
    }

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

    /// <summary>
    /// Whether the VM is waiting for a route rather than for frames.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Two waits and one flag, because they end differently.</strong>
    /// The frame wait counts down. The route wait ends when the board says the
    /// movement is finished — and the instruction index has to advance at that
    /// moment, because the move route command deliberately left it where it
    /// was.
    /// </para>
    /// <para>
    /// <strong>The bug this flag exists to prevent:</strong> without it the VM
    /// resumed on the move route command itself and started the route over,
    /// forever. The event would hang with no error anywhere — the frames were
    /// zero, the route was finished, and the command at the index was the one
    /// that had started it.
    /// </para>
    /// </remarks>
    private bool _waitingForRoute;
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
        // **The board moves every frame, waiting or not.** A figure on a patrol
        // keeps walking while a message is on screen, and a reader that ticked
        // it only in the move-route wait would freeze every figure for the
        // length of a text box. The tick sits above the state check on purpose.
        Board.Tick();

        if (State == WolfVmState.Waiting)
        {
            // **The route branch comes first, because its frames are zero.**
            // A reader that checked the frame count first would fall through
            // to the frame branch, decrement nothing, and never leave the
            // state — the event would hang with the route long finished.
            if (_waitingForRoute)
            {
                if (Board.IsEveryRouteFinished())
                {
                    _waitingForRoute = false;
                    _instructionIndex += 1;
                    State = WolfVmState.Running;
                }
                return PluginOperationResult.Succeeded();
            }
            if (_pendingChoiceIndex < 0 && _waitRemaining > 0)
            {
                _waitRemaining -= 1;
                if (_waitRemaining == 0)
                {
                    State = WolfVmState.Running;
                }
                return PluginOperationResult.Succeeded();
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
        // **The call stack goes with everything else.** A stack left over from
        // the last game would have the first End of the new one pop a frame
        // that points at a program from the old one, and the new game's event
        // would resume inside the last game's.
        _callStack.Clear();
        LastCommonEventResult = null;
        _program = null;
        _instructionIndex = 0;
        _waitRemaining = 0;
        _pendingChoiceIndex = -1;
        _messageSequence = 0;
        _waitRemaining = 0;
        _waitingForRoute = false;
        _variables.Clear();
        // **The board is cleared with the variables, and not left behind.** A
        // new game that kept the last game's figures would have two heroes on
        // the same tile, and a route that walked into a map with no walls.
        Board.Clear();
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
            case WolfEventOpcode.MoveRoute:
                // **The route is a list on the command, and not a name looked up
                // somewhere.** The reader produced the steps; this hands them
                // to the board, which owns the index and the frame budget. A
                // reader that ran the steps here would have no place to keep
                // "how many frames are left on this one", and that state is
                // the whole reason a route takes time.
                if (pCommand.Route is not { } route)
                {
                    // **No route and no movement, and the event continues.**
                    // A command with no route is a command that says nothing;
                    // stopping the event would fail a game whose program has a
                    // route step this reader's file did not carry.
                    _instructionIndex += 1;
                    return PluginOperationResult.Succeeded();
                }
                Board.StartRoute(pCommand.CharacterId, route);
                if (route.WaitUntilDone)
                {
                    // **The wait is remembered, and not only the state.** The
                    // state is the same Waiting the frame wait uses, so the
                    // branch that ends this one has to be able to tell them
                    // apart — see _waitingForRoute.
                    _waitingForRoute = true;
                    // **The instruction index does not move, and that is the
                    // whole of the wait.** A reader that advanced first would
                    // run the *next* command and then wait, so the event would
                    // do one thing too many before it stopped — and the
                    // difference is one command per wait, which is exactly the
                    // kind of error a game never reports.
                    State = WolfVmState.Waiting;
                }
                else
                {
                    // **Wait only when the route says to.** The flag is the
                    // format's, and a reader that always waited would hold
                    // every event in the game until its figure stopped moving.
                    _instructionIndex += 1;
                }
                return PluginOperationResult.Succeeded();
            case WolfEventOpcode.WaitUntilRouteDone:
                // **The event waits for the board, and not for a frame count.**
                // The format's option holds the event until the movement
                // finishes, so a reader that counted frames would let the
                // event continue while the figure is still walking.
                //
                // **The index advances only once the board is done**, for the
                // same reason the move route does not advance: one command too
                // many is a command the game never wrote.
                if (Board.IsEveryRouteFinished())
                {
                    _instructionIndex += 1;
                }
                else
                {
                    // **The flag, and not just the state.** This is the second
                    // way into the route wait, and it ends in the same place:
                    // the index advances once the board is done. Without the
                    // flag this wait would fall through to the frame branch,
                    // where the frames are zero and nothing ever ends it.
                    _waitingForRoute = true;
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
            case WolfEventOpcode.CallCommonEvent:
                return CallCommon(pCommand);
            case WolfEventOpcode.End:
                return EndProgram();
            case WolfEventOpcode.Unknown:
            default:
                return Fail($"Unsupported WOLF event operation '{pCommand.RawOperation}'.", "command");
        }
    }

    /// <summary>
    /// Starts a common event, or reports the three ways that cannot work.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>A call shares the state and not a copy of it.</strong> A common
    /// event that sets a variable changes the game, and that is the point of
    /// the call — so the board, the variables and the switches stay where they
    /// are and only the place to carry on is put on the stack. A reader that
    /// copied the state would have a common event that gave an item the party
    /// never received.
    /// </para>
    /// <para>
    /// <strong>The three refusals each name something.</strong> An id the table
    /// does not have, a call with nothing to call, and a call deeper than the
    /// limit are three different mistakes in a game's files, and a single
    /// "call failed" would have a person looking in the wrong place.
    /// </para>
    /// <para>
    /// <strong>The depth limit is the guard against a common event that calls
    /// itself.</strong> Without it the VM would run until the process ended, and
    /// the event would be a game that hangs on one tile.
    /// </para>
    /// </remarks>
    private PluginOperationResult CallCommon(WolfEventCommand pCommand)
    {
        if (_callStack.Count >= MaxCallDepth)
        {
            return Fail(
                $"A WOLF common event call is {MaxCallDepth} deep, which is the"
                + " limit; a common event that calls itself would otherwise run"
                + " until the process ends.",
                "call");
        }
        if (pCommand.Operand <= 0)
        {
            return Fail(
                "A WOLF common event call names no event. The database id 0 is"
                + " the hero and not a common event.",
                "call");
        }
        WolfEventProgram? target = null;
        foreach (var candidate in CommonEvents)
        {
            if (candidate.Id == pCommand.Operand)
            {
                target = candidate;
                break;
            }
        }
        if (target == null)
        {
            return Fail(
                $"A WOLF common event call names event {pCommand.Operand}, which"
                + $" is not in the table of {CommonEvents.Count}.",
                "call");
        }
        if (target.Commands.Count == 0)
        {
            // **An event with no commands is already finished.** A reader that
            // pushed a frame and ran nothing would leave the caller suspended
            // for a call that has nothing to wait for.
            _instructionIndex += 1;
            LastCommonEventResult = null;
            return PluginOperationResult.Succeeded();
        }
        _callStack.Push(new WolfCallFrame
        {
            Program = _program!,
            ResumeIndex = _instructionIndex + 1,
            Waits = true,
        });
        _program = target;
        _instructionIndex = 0;
        _trace.Add($"event:{target.Id}:call");
        return PluginOperationResult.Succeeded();
    }

    /// <summary>
    /// Ends the running program, or comes back to whoever called it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Coming back is the whole point, and it is not an End of the
    /// game.</strong> The end of a common event resumes the caller; only the
    /// end of the outermost program completes the VM. A reader that treated
    /// both as the end would have a game's first common event call stop the
    /// game dead.
    /// </para>
    /// <para>
    /// <strong>The index is the caller's, already advanced past the call.</strong>
    /// The frame is pushed with the caller's own next index, so resuming needs
    /// no arithmetic and no knowledge of how long the call instruction is.
    /// </para>
    /// </remarks>
    private PluginOperationResult EndProgram()
    {
        if (_callStack.Count == 0)
        {
            _instructionIndex = _program?.Commands.Count ?? 0;
            State = WolfVmState.Completed;
            _trace.Add($"event:{CurrentEventId}:end");
            return PluginOperationResult.Succeeded();
        }
        var frame = _callStack.Pop();
        _program = frame.Program;
        _instructionIndex = frame.ResumeIndex;
        _trace.Add($"event:{CurrentEventId}:return");
        return PluginOperationResult.Succeeded();
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
