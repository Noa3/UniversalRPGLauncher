using System;
using System.Collections.Generic;
using System.Linq;
using UniversalRPG.Rm2k.Presentation;
using UniversalRPG.Rm2k.Simulation;

namespace UniversalRPG.Rm2k.Interpreter;

/// <summary>
/// Owns bounded interpreters and runtime movement state for the current RM2K
/// map. Imported commands remain data; only EventInterpreter executes the
/// verified native command subset.
///
/// RPG_RT has one foreground interpreter (autorun/action/touch/collision) while
/// parallel pages run independently. Event coordinates/facing/through state are
/// runtime data rather than immutable LMU metadata so movement routes can
/// evolve without mutating parsed map structures.
/// </summary>
public sealed class Rm2kEventScheduler
{
    public const int MaxEvents = 1000;

    private readonly GameSimulationState _state;
    private readonly List<Rm2kMap.Event> _events = new();
    private readonly Dictionary<int, EventInterpreter> _active = new();
    private readonly Dictionary<int, Rm2kEventTrigger> _activeTriggers = new();
    private readonly Dictionary<int, (int X, int Y)> _positions = new();
    private readonly Dictionary<int, int> _facings = new();
    private readonly HashSet<int> _through = new();
    private readonly HashSet<int> _facingLocked = new();
    private PresentationState? _presentation;

    public Rm2kEventScheduler(GameSimulationState pState, PresentationState? pPresentation = null)
    {
        _state = pState ?? throw new ArgumentNullException(nameof(pState));
        _presentation = pPresentation;
    }

    public int ActiveInterpreterCount => _active.Count;
    public int EventCount => _events.Count;
    public bool ForegroundBusy => _activeTriggers.Values.Any(pTrigger => pTrigger != Rm2kEventTrigger.Parallel);

    public void SetEvents(IEnumerable<Rm2kMap.Event> pEvents)
    {
        if (pEvents == null) throw new ArgumentNullException(nameof(pEvents));
        _events.Clear();
        _positions.Clear();
        _facings.Clear();
        _through.Clear();
        _facingLocked.Clear();
        var inspected = 0;
        var inputExceeded = false;
        foreach (var eventData in pEvents)
        {
            inspected++;
            if (inspected > MaxEvents)
            {
                inputExceeded = true;
                break;
            }
            if (eventData == null)
            {
                continue;
            }
            if (eventData.Id <= 0 || _positions.ContainsKey(eventData.Id))
            {
                _state.AddDiagnostic($"RM2K event with invalid/duplicate ID {eventData.Id} was ignored.");
                continue;
            }
            _events.Add(eventData);
            _positions[eventData.Id] = (eventData.X, eventData.Y);
            _facings[eventData.Id] = 2; // RPG direction 2 = down.
        }

        if (inputExceeded)
        {
            _state.AddDiagnostic($"RM2K event limit reached; input inspection stopped after {MaxEvents} entries.");
        }
        _active.Clear();
        _activeTriggers.Clear();
        SyncPlayerInputLock();
    }

    public void Clear()
    {
        _events.Clear();
        _positions.Clear();
        _facings.Clear();
        _through.Clear();
        _facingLocked.Clear();
        _active.Clear();
        _activeTriggers.Clear();
        SyncPlayerInputLock();
    }

    public void SetPresentation(PresentationState? pPresentation) => _presentation = pPresentation;

    public void ExecuteFrame()
    {
        StartParallelPages();
        StartAutorunPage();
        ExecuteActive();
    }

    public bool TriggerAction(int pEventId) => Trigger(pEventId, Rm2kEventTrigger.Action);
    public bool TriggerTouch(int pEventId) => Trigger(pEventId, Rm2kEventTrigger.Touch);
    public bool TriggerCollision(int pEventId) => Trigger(pEventId, Rm2kEventTrigger.Collision);

    public bool TriggerAt(int pX, int pY, Rm2kEventTrigger pTrigger)
    {
        foreach (var eventData in _events)
        {
            if (!_positions.TryGetValue(eventData.Id, out var position)
                || position.X != pX || position.Y != pY)
            {
                continue;
            }
            if (Trigger(eventData.Id, pTrigger))
            {
                return true;
            }
        }
        return false;
    }

    public bool TryGetEventPosition(int pEventId, out int pX, out int pY)
    {
        if (_positions.TryGetValue(pEventId, out var position))
        {
            pX = position.X;
            pY = position.Y;
            return true;
        }
        pX = 0;
        pY = 0;
        return false;
    }

    public bool TrySetEventPosition(int pEventId, int pX, int pY)
    {
        if (!_positions.ContainsKey(pEventId)) return false;
        _positions[pEventId] = (pX, pY);
        return true;
    }

    public bool TryGetEventFacing(int pEventId, out int pFacing)
    {
        return _facings.TryGetValue(pEventId, out pFacing);
    }

    public bool TrySetEventFacing(int pEventId, int pFacing, bool pRespectFacingLock = true)
    {
        if (!_facings.ContainsKey(pEventId) || !IsFacing(pFacing)) return false;
        if (pRespectFacingLock && _facingLocked.Contains(pEventId)) return true;
        _facings[pEventId] = pFacing;
        return true;
    }

    public bool TrySetEventThrough(int pEventId, bool pThrough)
    {
        if (!_positions.ContainsKey(pEventId)) return false;
        if (pThrough) _through.Add(pEventId);
        else _through.Remove(pEventId);
        return true;
    }

    public bool IsEventThrough(int pEventId) => _through.Contains(pEventId);

    public bool TrySetEventFacingLocked(int pEventId, bool pLocked)
    {
        if (!_positions.ContainsKey(pEventId)) return false;
        if (pLocked) _facingLocked.Add(pEventId);
        else _facingLocked.Remove(pEventId);
        return true;
    }

    public bool IsEventFacingLocked(int pEventId) => _facingLocked.Contains(pEventId);

    public IReadOnlyDictionary<int, (int X, int Y)> SnapshotEventPositions()
        => new Dictionary<int, (int X, int Y)>(_positions);

    public bool TryGetActiveLayer(int pEventId, out int pLayer)
    {
        var eventData = _events.FirstOrDefault(pEvent => pEvent.Id == pEventId);
        var page = eventData == null ? null : Rm2kEventPageSelector.SelectActive(eventData, _state);
        if (page == null)
        {
            pLayer = 0;
            return false;
        }
        pLayer = page.Layer;
        return true;
    }

    public bool HasBlockingSameLayerEventAt(int pX, int pY, int pIgnoreEventId = 0)
    {
        foreach (var eventData in _events)
        {
            if (eventData.Id == pIgnoreEventId
                || _through.Contains(eventData.Id)
                || !_positions.TryGetValue(eventData.Id, out var position)
                || position.X != pX || position.Y != pY)
            {
                continue;
            }
            var page = Rm2kEventPageSelector.SelectActive(eventData, _state);
            if (page != null && page.Layer == 1)
            {
                return true;
            }
        }
        return false;
    }

    private bool Trigger(int pEventId, Rm2kEventTrigger pTrigger)
    {
        if (pTrigger == Rm2kEventTrigger.Parallel)
        {
            return false;
        }
        if (ForegroundBusy || _active.ContainsKey(pEventId))
        {
            return false;
        }

        var eventData = _events.FirstOrDefault(pEvent => pEvent.Id == pEventId);
        var page = eventData == null ? null : Rm2kEventPageSelector.Select(eventData, _state, pTrigger);
        if (page == null)
        {
            return false;
        }
        AddActive(eventData!.Id, pTrigger, page);
        return true;
    }

    private void StartAutorunPage()
    {
        if (ForegroundBusy) return;
        foreach (var eventData in _events)
        {
            if (_active.ContainsKey(eventData.Id)) continue;
            var page = Rm2kEventPageSelector.Select(eventData, _state, Rm2kEventTrigger.Autorun);
            if (page == null) continue;
            AddActive(eventData.Id, Rm2kEventTrigger.Autorun, page);
            return;
        }
    }

    private void StartParallelPages()
    {
        foreach (var eventData in _events)
        {
            if (_active.ContainsKey(eventData.Id)) continue;
            var page = Rm2kEventPageSelector.Select(eventData, _state, Rm2kEventTrigger.Parallel);
            if (page == null) continue;
            AddActive(eventData.Id, Rm2kEventTrigger.Parallel, page);
        }
    }

    private void AddActive(int pEventId, Rm2kEventTrigger pTrigger, Rm2kMap.EventPage pPage)
    {
        _active[pEventId] = new EventInterpreter(_state, pEventId, pPage.Commands, _presentation);
        _activeTriggers[pEventId] = pTrigger;
        SyncPlayerInputLock();
    }

    private void ExecuteActive()
    {
        foreach (var entry in _active.ToArray())
        {
            if (!entry.Value.ExecuteFrame())
            {
                _active.Remove(entry.Key);
                _activeTriggers.Remove(entry.Key);
                SyncPlayerInputLock();
            }
            if (_state.IsTransferPending) break;
        }
    }

    private void SyncPlayerInputLock()
    {
        _state.PlayerInputLocked = ForegroundBusy;
    }

    private static bool IsFacing(int pFacing) => pFacing is 2 or 4 or 6 or 8;
}
