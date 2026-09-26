using System;
using System.Collections.Generic;
using System.Linq;
using UniversalRPG.Rm2k.Presentation;
using UniversalRPG.Rm2k.Simulation;

namespace UniversalRPG.Rm2k.Interpreter;

/// <summary>Layer rule the Player applies when looking for a triggering event.</summary>
public enum Rm2kTriggerLayerRule
{
    /// <summary>Event in front of the player: it must share the player's layer.</summary>
    MustBeSame,
    /// <summary>Event on the player's own tile: it must not share the player's layer.</summary>
    MustNotBeSame,
}

/// <summary>
/// Owns bounded interpreters for the current RM2K map. Imported commands are
/// still data; only the native EventInterpreter receives them. Autorun pages
/// are started once, parallel pages may be restarted after completion, and
/// action/touch pages require an explicit trigger call from the host.
/// </summary>
public sealed class Rm2kEventScheduler
{
    public const int MaxEvents = 1000;

    /// <summary>liblcf rpg::EventPage::Layers: below = 0, same = 1, above = 2.</summary>
    public const int LayerBelow = 0;
    public const int LayerSame = 1;
    public const int LayerAbove = 2;

    /// <summary>RPG_RT allows a maximum of 3 counter tiles in an action chain.</summary>
    public const int MaxCounterTiles = 3;

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
        _autorunStarted.Clear();
        _parallelStarted.Clear();
    }

    public void Clear()
    {
        _events.Clear();
        _active.Clear();
        _autorunStarted.Clear();
        _parallelStarted.Clear();
    }

    public void SetPresentation(PresentationState? pPresentation) => _presentation = pPresentation;

    /// <summary>
    /// Supplies the nested CallEvent resolver. Map events are already owned by
    /// this scheduler, so the default resolver answers from its own event list.
    /// </summary>
    public Func<int, int, IReadOnlyList<Rm2kMap.EventCommand>?>? EventCommandResolver { get; set; }

    /// <summary>Moves a map event; returns false when the id is unknown.</summary>
    public Func<int, int, int, int, bool>? EventLocationSetter { get; set; }

    /// <summary>Deactivates a map event and stops its interpreter.</summary>
    public Func<int, bool>? EventDeactivator { get; set; }

    private bool ApplyEventLocation(int pEventId, int pX, int pY, int pDirection)
    {
        if (EventLocationSetter != null)
        {
            return EventLocationSetter(pEventId, pX, pY, pDirection);
        }
        foreach (var eventData in _events)
        {
            if (eventData.Id != pEventId)
            {
                continue;
            }
            eventData.X = pX;
            eventData.Y = pY;
            return true;
        }
        return false;
    }

    private bool DeactivateEvent(int pEventId)
    {
        if (EventDeactivator != null)
        {
            return EventDeactivator(pEventId);
        }
        var found = false;
        foreach (var eventData in _events)
        {
            if (eventData.Id == pEventId)
            {
                found = true;
            }
        }
        if (!found)
        {
            return false;
        }
        _active.Remove(pEventId);
        _autorunStarted.Add(pEventId);
        _parallelStarted.Add(pEventId);
        return true;
    }

    private IReadOnlyList<Rm2kMap.EventCommand>? ResolveCommands(int pEventId, int pPageIndex)
    {
        if (EventCommandResolver != null)
        {
            return EventCommandResolver(pEventId, pPageIndex);
        }
        foreach (var eventData in _events)
        {
            if (eventData.Id != pEventId || eventData.Pages.Count == 0)
            {
                continue;
            }
            var pageNumber = pPageIndex <= 0 ? 0 : pPageIndex - 1;
            return pageNumber < eventData.Pages.Count ? eventData.Pages[pageNumber].Commands : null;
        }
        return null;
    }

    public void ExecuteFrame()
    {
        StartAutomaticPages(Rm2kEventTrigger.AutoStart, _autorunStarted, restartWhenFinished: false);
        StartAutomaticPages(Rm2kEventTrigger.Parallel, _parallelStarted, restartWhenFinished: true);
        ExecuteActive();
    }

    public bool TriggerAction(int pEventId) => Trigger(pEventId, Rm2kEventTrigger.Action);

    public bool TriggerAt(int pX, int pY, Rm2kEventTrigger pTrigger)
    {
        var eventData = _events.FirstOrDefault(pEvent => pEvent.X == pX && pEvent.Y == pY);
        return eventData != null && Trigger(eventData.Id, pTrigger);
    }

    public bool TriggerTouch(int pEventId) => Trigger(pEventId, Rm2kEventTrigger.Touched);

    /// <summary>
    /// Verified Game_Player::CheckEventTriggerThere. The action trigger is
    /// searched on the tile in front of the player, and when that tile is a
    /// counter tile the search continues over at most three counter tiles in the
    /// facing direction. RPG_RT allows a maximum of three counter tiles, and the
    /// loop stops as soon as an action event was found.
    /// </summary>
    public bool TriggerActionFacing()
    {
        var (frontX, frontY) = _state.FrontTile(_state.MapX, _state.MapY, _state.FacingDirection);
        // The Player checks the tile in front first, then steps over a counter
        // tile before checking the next one, at most three times.
        var gotAction = TriggerAt(frontX, frontY, Rm2kEventTrigger.Action, Rm2kTriggerLayerRule.MustBeSame);
        for (var step = 0; !gotAction && step < MaxCounterTiles; step++)
        {
            if (!_state.IsCounterAt(frontX, frontY))
            {
                break;
            }
            (frontX, frontY) = _state.FrontTile(frontX, frontY, _state.FacingDirection);
            gotAction |= TriggerAt(frontX, frontY, Rm2kEventTrigger.Action, Rm2kTriggerLayerRule.MustBeSame);
        }
        return gotAction;
    }

    /// <summary>
    /// Verified Game_Player::CheckEventTriggerHere: action pages on the player's
    /// own tile count when they are not on the same layer.
    /// </summary>
    public bool TriggerActionHere()
    {
        return TriggerAt(_state.MapX, _state.MapY, Rm2kEventTrigger.Action, Rm2kTriggerLayerRule.MustNotBeSame);
    }

    /// <summary>
    /// Verified Game_Player::CheckEventTriggerThere for the walking case: only
    /// touched and collision pages on the tile in front are evaluated, with no
    /// counter tile walk.
    /// </summary>
    public bool TriggerTouchOrCollisionFacing()
    {
        var (frontX, frontY) = _state.FrontTile(_state.MapX, _state.MapY, _state.FacingDirection);
        return TriggerAt(frontX, frontY, Rm2kEventTrigger.Touched, Rm2kTriggerLayerRule.MustBeSame)
            || TriggerAt(frontX, frontY, Rm2kEventTrigger.Collision, Rm2kTriggerLayerRule.MustBeSame);
    }

    /// <summary>
    /// Starts the first matching page at the given tile. The Player keeps the
    /// layer rule explicit: events in front of the player must share its layer,
    /// events on the player's own tile must not.
    /// </summary>
    private bool TriggerAt(int pX, int pY, Rm2kEventTrigger pTrigger, Rm2kTriggerLayerRule pRule)
    {
        var triggered = false;
        foreach (var eventData in _events)
        {
            if (eventData.X != pX || eventData.Y != pY)
            {
                continue;
            }
            var matched = false;
            foreach (var page in eventData.Pages)
            {
                if (page.Trigger != (int)pTrigger)
                {
                    continue;
                }
                var isSameLayer = page.Layer == LayerSame;
                if (pRule == Rm2kTriggerLayerRule.MustBeSame ? !isSameLayer : isSameLayer)
                {
                    continue;
                }
                matched = true;
            }
            if (!matched)
            {
                continue;
            }
            triggered |= Trigger(eventData.Id, pTrigger);
        }
        return triggered;
    }

    private bool Trigger(int pEventId, Rm2kEventTrigger pTrigger)
    {
        if (_active.ContainsKey(pEventId)) return false;
        var eventData = _events.FirstOrDefault(pEvent => pEvent.Id == pEventId);
        var page = eventData == null ? null : Rm2kEventPageSelector.Select(eventData, _state, pTrigger);
        if (page == null) return false;
        _active[pEventId] = new EventInterpreter(_state, pEventId, page.Commands, _presentation, ResolveCommands, ApplyEventLocation, DeactivateEvent);
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
            _active[eventData.Id] = new EventInterpreter(_state, eventData.Id, page.Commands, _presentation, ResolveCommands, ApplyEventLocation, DeactivateEvent);
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
