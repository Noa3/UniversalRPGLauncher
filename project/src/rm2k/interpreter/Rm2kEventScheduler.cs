using System;
using System.Collections.Generic;
using System.Linq;
using UniversalRPG.Rm2k.Presentation;
using UniversalRPG.Rm2k.Simulation;

namespace UniversalRPG.Rm2k.Interpreter;

/// <summary>
/// Owns bounded interpreters for the current RM2K map. Imported commands are
/// still data; only the native EventInterpreter receives them. Autorun pages
/// are started once, parallel pages may be restarted after completion, and
/// action/touch pages require an explicit trigger call from the host.
/// </summary>
public sealed class Rm2kEventScheduler
{
    private readonly GameSimulationState _state;
    private readonly List<Rm2kMap.Event> _events = new();
    private readonly Dictionary<int, EventInterpreter> _active = new();
    private readonly HashSet<int> _autorunStarted = new();
    private readonly HashSet<int> _parallelStarted = new();
    private PresentationState? _presentation;

    public Rm2kEventScheduler(GameSimulationState pState, PresentationState? pPresentation = null)
    {
        _state = pState ?? throw new ArgumentNullException(nameof(pState));
        _presentation = pPresentation;
    }

    public int ActiveInterpreterCount => _active.Count;

    public void SetEvents(IEnumerable<Rm2kMap.Event> pEvents)
    {
        if (pEvents == null) throw new ArgumentNullException(nameof(pEvents));
        _events.Clear();
        _events.AddRange(pEvents.Where(pEvent => pEvent != null).Take(1000));
        _active.Clear();
        _autorunStarted.Clear();
        _parallelStarted.Clear();
    }

    public void SetPresentation(PresentationState? pPresentation) => _presentation = pPresentation;

    public void ExecuteFrame()
    {
        StartAutomaticPages(Rm2kEventTrigger.Autorun, _autorunStarted, restartWhenFinished: false);
        StartAutomaticPages(Rm2kEventTrigger.Parallel, _parallelStarted, restartWhenFinished: true);
        ExecuteActive();
    }

    public bool TriggerAction(int pEventId) => Trigger(pEventId, Rm2kEventTrigger.Action);

    public bool TriggerAt(int pX, int pY, Rm2kEventTrigger pTrigger)
    {
        var eventData = _events.FirstOrDefault(pEvent => pEvent.X == pX && pEvent.Y == pY);
        return eventData != null && Trigger(eventData.Id, pTrigger);
    }

    public bool TriggerTouch(int pEventId) => Trigger(pEventId, Rm2kEventTrigger.Touch);

    private bool Trigger(int pEventId, Rm2kEventTrigger pTrigger)
    {
        if (_active.ContainsKey(pEventId)) return false;
        var eventData = _events.FirstOrDefault(pEvent => pEvent.Id == pEventId);
        var page = eventData == null ? null : Rm2kEventPageSelector.Select(eventData, _state, pTrigger);
        if (page == null) return false;
        _active[pEventId] = new EventInterpreter(_state, pEventId, page.Commands, _presentation);
        return true;
    }

    private void StartAutomaticPages(Rm2kEventTrigger pTrigger, HashSet<int> pStarted, bool restartWhenFinished)
    {
        foreach (var eventData in _events)
        {
            if (_active.ContainsKey(eventData.Id)) continue;
            var page = Rm2kEventPageSelector.Select(eventData, _state, pTrigger);
            if (page == null) continue;
            if (!restartWhenFinished && pStarted.Contains(eventData.Id)) continue;
            _active[eventData.Id] = new EventInterpreter(_state, eventData.Id, page.Commands, _presentation);
            pStarted.Add(eventData.Id);
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
        }
    }
}
