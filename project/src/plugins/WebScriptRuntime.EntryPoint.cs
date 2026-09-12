using System;
using UniversalRPG.Sdk;

namespace UniversalRPG.Plugins;

public sealed partial class WebScriptRuntime
{
    public bool EntryPointExecuted => _entryPointExecuted;
    public bool WindowLoadDispatched => _windowLoadDispatched;

    /// <summary>Execute the already-loaded original js/main.js exactly once.</summary>
    public SdkOperationResult ExecuteEntryPoint()
    {
        var gate = CheckSession();
        if (!gate.Success) return gate;
        if (!_loaded) return SdkOperationResult.Failed("web.not-loaded", "Load scripts before the entry point.");
        if (!_bootstrapped) return SdkOperationResult.Failed("web.not-bootstrapped", "Core/plugin bootstrap must complete before the entry point.");
        if (_entryPoint == null) return SdkOperationResult.Failed("web.entry-unavailable", "No original entry-point module was supplied.");
        if (_entryPointExecuted) return SdkOperationResult.Failed("web.entry-already-executed", "The original entry point already executed.");

        _busy = true;
        _faulted = true;
        try
        {
            var entered = EnterPluginScript(_entryPoint.Descriptor.RelativePath);
            if (!entered.Success) return SdkOperationResult.Failed("web.entry-scope-failed", entered.ErrorMessage, entered.Diagnostics);
            var result = _vm.ExecuteModule(_entryPoint.Descriptor.Id);
            if (!result.Success)
                return SdkOperationResult.Failed("web.entry-execution-failed",
                    $"Entry point '{_entryPoint.Descriptor.RelativePath}': {result.ErrorMessage}", result.Diagnostics);
            var left = LeavePluginScript();
            if (!left.Success) return SdkOperationResult.Failed("web.entry-scope-failed", left.ErrorMessage, left.Diagnostics);
            _entryPointExecuted = true;
            _faulted = false;
            return SdkOperationResult.Succeeded(new[]
            {
                SdkDiagnostic.Info("web.entry-executed", "Original js/main.js executed; window load has not been dispatched yet."),
            });
        }
        catch (Exception exception) when (!IsCritical(exception))
        {
            return ExternalFailure("entry", _entryPoint.Descriptor.RelativePath, exception);
        }
        finally { _busy = false; }
    }

    /// <summary>
    /// Dispatch the single startup load event through the native lifecycle
    /// prelude. For MV this reaches window.onload; for MZ it reaches the
    /// registered load listener after preloaded-script callbacks were pumped.
    /// </summary>
    public SdkOperationResult DispatchWindowLoad()
    {
        var gate = CheckSession();
        if (!gate.Success) return gate;
        if (!_entryPointExecuted) return SdkOperationResult.Failed("web.entry-not-executed", "Execute the original entry point before dispatching load.");
        if (_windowLoadDispatched) return SdkOperationResult.Failed("web.load-already-dispatched", "Window load was already dispatched.");
        _busy = true;
        _faulted = true;
        try
        {
            var result = _vm.Invoke(new ScriptInvocation
            {
                Target = NativeEntryPointHostPrelude.ControlTarget,
                Member = "dispatchLoad",
                Arguments = Array.Empty<ScriptValue>(),
            });
            if (!result.Success)
                return SdkOperationResult.Failed("web.load-dispatch-failed", result.ErrorMessage, result.Diagnostics);
            _windowLoadDispatched = true;
            _faulted = false;
            return result;
        }
        catch (Exception exception) when (!IsCritical(exception))
        {
            return ExternalFailure("window-load", NativeEntryPointHostPrelude.ControlTarget, exception);
        }
        finally { _busy = false; }
    }
}
