using System;
using System.Collections.Generic;
using System.Linq;
using UniversalRPG.Rm2k.Presentation;
using UniversalRPG.Rm2k.Simulation;

namespace UniversalRPG.Rm2k.Interpreter;

/// <summary>
/// Owns bounded interpreters for the current RM2K map. Imported commands are
/// still data; only the native EventInterpreter receives them. Active autorun
/// and parallel pages are re-evaluated after completion, while action/touch/
/// collision pages require an explicit runtime trigger.
/// </summary>
public sealed class Rm2kEventScheduler
{
    public const int MaxEvents = 1000;

    private readonly GameSimulationState _state;
    private readonly List<Rm2kMap.Event> _events = new();
    private readonly Dictionary<int, EventInterpreter> _active = new();
    private PresentationState? _presentation;

    public Rm2kEventScheduler(GameSimulationState pState, PresentationState? pPresentation = null)
    {
        _state = pState ?? throw new ArgumentNullException(nameof(pState));
        _presentation = pPresentation;
    }

    public int ActiveInterpreterCount => _active.Count;
    public int EventCount => _events.Count;

    public void SetEvents(IEnumerable<Rm2kMap.Event> pEvents)
    {
        if (pEvents == null) throw new ArgumentNullException(nameof(pEvents));
        _events.Clear();
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

            if (eventData != null)
            {
                _events.Add(eventData);
            }
        }

        if (inputExceeded)
        {
            _state.AddDiagnostic($"RM2K event limit reached; input inspection stopped after {MaxEvents} entries.");
        }
        _active.Clear();
    }

    public void Clear()
    {
        _events.Clear();
        _active.Clear();
    }

    public void SetPresentation(PresentationState? pPresentation) => _presentation = pPresentation;

    public void ExecuteFrame()
    {
        StartAutomaticPages(Rm2kEventTrigger.Autorun);
        StartAutomaticPages(Rm2kEventTrigger.Parallel);
        ExecuteActive();
    }

    public bool TriggerAction(int pEventId) => Trigger(pEventId, Rm2kEventTrigger.Action);

    /// <summary>
    /// Triggers the first event at the coordinate that actually has an eligible
    /// page for the requested trigger. Multiple events may legally share a map
    /// coordinate, so a non-matching earlier event must not mask a later one.
    /// </summary>
    public bool TriggerAt(int pX, int pY, Rm2kEventTrigger pTrigger)
    {
        foreach (var eventData in _events)
        {
            if (eventData.X != pX || eventData.Y != pY)
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

    public bool TriggerTouch(int pEventId) => Trigger(pEventId, Rm2kEventTrigger.Touch);

    /// <summary>
    /// Returns whether an active same-layer event occupies the coordinate.
    /// This is geometry/collision information only; it does not start commands.
    /// Through/move-route overrides are not modeled yet and must be added before
    /// this becomes a complete RPG_RT collision implementation.
    /// </summary>
    public bool HasBlockingSameLayerEventAt(int pX, int pY)
    {
        foreach (var eventData in _events)
        {
            if (eventData.X != pX || eventData.Y != pY)
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
        if (_active.ContainsKey(pEventId)) return false;
        var eventData = _events.FirstOrDefault(pEvent => pEvent.Id == pEventId);
        var page = eventData == null ? null : Rm2kEventPageSelector.Select(eventData, _state, pTrigger);
        if (page == null) return false;
        _active[pEventId] = new EventInterpreter(_state, pEventId, page.Commands, _presentation);
        return true;
    }

    private void StartAutomaticPages(Rm2kEventTrigger pTrigger)
    {
        foreach (var eventData in _events)
        {
            if (_active.ContainsKey(eventData.Id)) continue;
            var page = Rm2kEventPageSelector.Select(eventData, _state, pTrigger);
            if (page == null) continue;
            _active[eventData.Id] = new EventInterpreter(_state, eventData.Id, page.Commands, _presentation);
        }
    }

    private void ExecuteActive()
    {
        foreach (var entry in _active.ToArray())
        {
            if (!entry.Value.ExecuteFrame())
            {
                _active.Remove(entry.Key);
            }
            if (_state.IsTransferPending)
            {
                // A successful map transfer replaces the current scheduler/event
                // set. Do not execute additional events from the old map in the
                // same simulation frame after a transfer request is raised.
                break;
            }
        }
    }
}
