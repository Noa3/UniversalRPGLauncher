using System;

namespace UniversalRPG.Plugins;

/// <summary>
/// Fail-closed guard used only when a built-in plugin advertises Runtime without
/// providing a concrete runtime implementation. A metadata/bootstrap lifecycle
/// must never be mistaken for playable engine support.
/// </summary>
public sealed class EngineBootstrapRuntime : IEngineRuntime
{
    private readonly string _pluginId;

    public EngineBootstrapRuntime(string pPluginId, PluginGameInfo pGame)
    {
        _pluginId = pPluginId;
        _ = pGame ?? throw new ArgumentNullException(nameof(pGame));
    }

    public PluginRuntimeState State { get; private set; } = PluginRuntimeState.Created;

    public PluginOperationResult Initialize(EnginePluginRuntimeContext pContext)
    {
        _ = pContext ?? throw new ArgumentNullException(nameof(pContext));
        if (State != PluginRuntimeState.Created)
        {
            return Fail(PluginErrorCode.InvalidLifecycleTransition, "Runtime was already initialized.", "initialize");
        }

        State = PluginRuntimeState.Faulted;
        return Fail(
            PluginErrorCode.UnsupportedEngine,
            "This plugin advertises runtime capability but does not provide a concrete engine runtime. Generic metadata bootstraps are intentionally non-launchable.",
            "initialize");
    }

    public PluginOperationResult Start()
    {
        return Fail(
            PluginErrorCode.InvalidLifecycleTransition,
            $"Generic bootstrap runtime cannot start from state {State}.",
            "start");
    }

    public PluginOperationResult Update(double pDeltaSeconds)
    {
        _ = pDeltaSeconds;
        return Fail(
            PluginErrorCode.InvalidLifecycleTransition,
            $"Generic bootstrap runtime cannot update from state {State}.",
            "update");
    }

    public PluginOperationResult Stop()
    {
        if (State == PluginRuntimeState.Disposed)
        {
            return Fail(PluginErrorCode.InvalidLifecycleTransition, "Disposed runtime cannot be stopped.", "stop");
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
