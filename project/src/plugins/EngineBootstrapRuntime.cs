using System;
using System.Collections.Generic;
using UniversalRPG.Core;

namespace UniversalRPG.Plugins;

/// <summary>
/// Safe lifecycle bootstrap for engines whose full compatibility VM is still in
/// progress. It proves that a detected source can enter an in-process runtime
/// without executing engine binaries or scripts, and gives every engine family
/// a deterministic clock boundary for incremental implementation.
/// </summary>
public sealed class EngineBootstrapRuntime : IEngineRuntime
{
    private readonly string _pluginId;
    private readonly PluginGameInfo _game;
    private readonly VirtualClock _clock = new();

    public EngineBootstrapRuntime(string pPluginId, PluginGameInfo pGame)
    {
        _pluginId = pPluginId;
        _game = pGame;
    }

    public PluginRuntimeState State { get; private set; } = PluginRuntimeState.Created;
    public int SimulationTicks => _clock.GetSimulationTicks();
    public int InspectedFileCount { get; private set; }

    public PluginOperationResult Initialize(EnginePluginRuntimeContext pContext)
    {
        if (State != PluginRuntimeState.Created)
        {
            return Fail(PluginErrorCode.InvalidLifecycleTransition, "Runtime was already initialized.", "initialize");
        }
        var inspection = SafeGameInspector.Inspect(_game.GameDirectory);
        if (!inspection.Success || inspection.Value == null)
        {
            return Fail(
                PluginErrorCode.InvalidGame,
                inspection.Error?.Message ?? "The detected game could not be inspected safely.",
                "initialize");
        }
        if (inspection.Value.IsMalformed)
        {
            return Fail(
                PluginErrorCode.InvalidGame,
                "The detected game contains malformed or over-budget input.",
                "initialize");
        }
        InspectedFileCount = inspection.Value.Files.Count;
        State = PluginRuntimeState.Initialized;
        var diagnostics = new List<PluginDiagnostic>
        {
            PluginDiagnostic.Info(
                "runtime.bootstrap-initialized",
                $"Initialized {_pluginId} bootstrap with {InspectedFileCount} bounded metadata files; engine scripts and binaries were not executed.",
                _pluginId),
        };
        if (inspection.Value.IsPartial)
        {
            diagnostics.Add(PluginDiagnostic.Warning(
                "runtime.bootstrap-partial-scan",
                $"The project exceeded the bounded inspection entry budget ({InspectedFileCount} files scanned); metadata is advisory for files outside the covered set.",
                _pluginId));
        }
        return PluginOperationResult.Succeeded(diagnostics.ToArray());
    }

    public PluginOperationResult Start()
    {
        if (State != PluginRuntimeState.Initialized)
        {
            return Fail(PluginErrorCode.InvalidLifecycleTransition, $"Runtime cannot start from state {State}.", "start");
        }
        State = PluginRuntimeState.Running;
        return PluginOperationResult.Succeeded();
    }

    public PluginOperationResult Update(double pDeltaSeconds)
    {
        if (State != PluginRuntimeState.Running)
        {
            return Fail(PluginErrorCode.InvalidLifecycleTransition, $"Runtime cannot update from state {State}.", "update");
        }
        if (double.IsNaN(pDeltaSeconds) || double.IsInfinity(pDeltaSeconds) || pDeltaSeconds < 0)
        {
            return Fail(PluginErrorCode.InvalidLifecycleTransition, "Delta time must be finite and non-negative.", "update");
        }
        _clock.ProcessFrame(pDeltaSeconds);
        return PluginOperationResult.Succeeded();
    }

    public PluginOperationResult Stop()
    {
        if (State != PluginRuntimeState.Initialized && State != PluginRuntimeState.Running)
        {
            return Fail(PluginErrorCode.InvalidLifecycleTransition, $"Runtime cannot stop from state {State}.", "stop");
        }
        State = PluginRuntimeState.Stopped;
        return PluginOperationResult.Succeeded();
    }

    public void Dispose()
    {
        State = PluginRuntimeState.Disposed;
    }

    private PluginOperationResult Fail(PluginErrorCode pCode, string pMessage, string pPhase)
    {
        return PluginOperationResult.Failed(PluginError.Create(pCode, pMessage, _pluginId, pPhase));
    }
}
