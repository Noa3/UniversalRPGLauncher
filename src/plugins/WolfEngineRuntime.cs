using System;
using System.Linq;
using UniversalRPG.Core;
using UniversalRPG.Plugins;

namespace UniversalRPG.Wolf;

/// <summary>
/// In-process WOLF runtime slice for explicitly unencrypted plain data. Loading
/// is bounded and read-only; event execution is limited to WolfEventVm's typed
/// deterministic command set.
/// </summary>
public sealed class WolfEngineRuntime : IEngineRuntime
{
    private readonly string _pluginId;
    private readonly PluginGameInfo _game;
    private readonly WolfDataReader _reader;
    private readonly VirtualClock _clock = new();
    private readonly WolfEventVm _eventVm = new();

    public WolfEngineRuntime(string pPluginId, PluginGameInfo pGame, WolfParseLimits? pLimits = null)
    {
        _pluginId = pPluginId;
        _game = pGame;
        _reader = new WolfDataReader(pLimits);
    }

    public PluginRuntimeState State { get; private set; } = PluginRuntimeState.Created;
    public WolfProjectData? ProjectData { get; private set; }
    public WolfMapData? CurrentMap { get; private set; }
    public WolfEventVm EventVm => _eventVm;
    public int SimulationTicks => _clock.GetSimulationTicks();

    public PluginOperationResult Initialize(EnginePluginRuntimeContext pContext)
    {
        if (State != PluginRuntimeState.Created)
        {
            return Fail(PluginErrorCode.InvalidLifecycleTransition, "WOLF runtime was already initialized.", "initialize");
        }
        if (!_game.EngineId.Equals(EnginePluginIds.WolfRpg, StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(_game.GameDirectory))
        {
            return Fail(PluginErrorCode.InvalidGame, "WOLF runtime requires a detected WOLF game directory.", "initialize");
        }

        var loaded = _reader.Load(_game.GameDirectory);
        if (!loaded.Success || loaded.Value == null)
        {
            return PluginOperationResult.Failed(loaded.Error ?? PluginError.Create(
                PluginErrorCode.InvalidGame,
                "WOLF data could not be loaded.",
                _pluginId,
                "initialize"), loaded.Diagnostics);
        }
        ProjectData = loaded.Value;
        CurrentMap = ProjectData.Maps.OrderBy(pMap => pMap.Id).FirstOrDefault();
        State = PluginRuntimeState.Initialized;
        return PluginOperationResult.Succeeded(loaded.Diagnostics.Concat(new[]
        {
            PluginDiagnostic.Info(
                "wolf.runtime-initialized",
                $"Loaded WOLF plain data: {ProjectData.Maps.Count} maps, {ProjectData.UserDatabases.Count} user databases, and {ProjectData.CommonEvents.Count} common events.",
                _pluginId),
        }));
    }

    public PluginOperationResult Start()
    {
        if (State != PluginRuntimeState.Initialized)
        {
            return Fail(PluginErrorCode.InvalidLifecycleTransition, $"WOLF runtime cannot start from state {State}.", "start");
        }
        State = PluginRuntimeState.Running;
        return PluginOperationResult.Succeeded();
    }

    public PluginOperationResult StartEvent(int pEventId, bool pCommonEvent = false)
    {
        if (State != PluginRuntimeState.Running || ProjectData == null)
        {
            return Fail(PluginErrorCode.InvalidLifecycleTransition, "WOLF events can only start while the runtime is running.", "event-start");
        }
        var program = pCommonEvent
            ? ProjectData.CommonEvents.FirstOrDefault(pEvent => pEvent.Id == pEventId)
            : CurrentMap?.Events.FirstOrDefault(pEvent => pEvent.Id == pEventId);
        if (program == null)
        {
            return Fail(PluginErrorCode.InvalidGame, $"WOLF event {pEventId} was not found.", "event-start");
        }
        return _eventVm.Start(program);
    }

    public PluginOperationResult Update(double pDeltaSeconds)
    {
        if (State != PluginRuntimeState.Running)
        {
            return Fail(PluginErrorCode.InvalidLifecycleTransition, $"WOLF runtime cannot update from state {State}.", "update");
        }
        if (double.IsNaN(pDeltaSeconds) || double.IsInfinity(pDeltaSeconds) || pDeltaSeconds < 0)
        {
            return Fail(PluginErrorCode.InvalidLifecycleTransition, "Delta time must be finite and non-negative.", "update");
        }

        var before = _clock.GetSimulationTicks();
        _clock.ProcessFrame(pDeltaSeconds);
        var ticks = _clock.GetSimulationTicks() - before;
        for (var index = 0; index < ticks; index += 1)
        {
            if (_eventVm.State is not (WolfVmState.Running or WolfVmState.Waiting))
            {
                break;
            }
            var eventResult = _eventVm.StepTick();
            if (!eventResult.Success)
            {
                return eventResult;
            }
        }
        return PluginOperationResult.Succeeded();
    }

    public PluginOperationResult Stop()
    {
        if (State != PluginRuntimeState.Initialized && State != PluginRuntimeState.Running)
        {
            return Fail(PluginErrorCode.InvalidLifecycleTransition, $"WOLF runtime cannot stop from state {State}.", "stop");
        }
        State = PluginRuntimeState.Stopped;
        return PluginOperationResult.Succeeded();
    }

    public void Dispose()
    {
        State = PluginRuntimeState.Disposed;
        ProjectData = null;
        CurrentMap = null;
        _eventVm.ResetState();
    }

    private PluginOperationResult Fail(PluginErrorCode pCode, string pMessage, string pPhase)
    {
        return PluginOperationResult.Failed(PluginError.Create(pCode, pMessage, _pluginId, pPhase));
    }
}

/// <summary>Named analyzer entry point for import/report code.</summary>
public sealed class WolfGameAnalyzer
{
    private readonly WolfDataReader _reader;

    public WolfGameAnalyzer(WolfParseLimits? pLimits = null)
    {
        _reader = new WolfDataReader(pLimits);
    }

    public PluginResult<WolfProjectData> Analyze(string pGameDirectory) => _reader.Analyze(pGameDirectory);
}

/// <summary>
/// Small facade used by callers that need an explicit WOLF-only analyzer
/// without constructing the full built-in detection catalog.
/// </summary>
public sealed class WolfGameDetector
{
    public bool LooksLikeWolfDirectory(string pGameDirectory)
    {
        var result = new WolfDataReader().Load(pGameDirectory);
        return result.Success && result.Value != null;
    }
}
