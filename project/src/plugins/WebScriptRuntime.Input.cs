using UniversalRPG.Sdk;

namespace UniversalRPG.Plugins;

public sealed partial class WebScriptRuntime
{
    /// <summary>
    /// Feed a browser-compatible key event into the original engine input
    /// listeners after startup. Mapping a physical keyboard/controller button
    /// to the legacy keyCode is a platform-layer decision, not game logic.
    /// </summary>
    public SdkOperationResult DispatchKeyEvent(
        int keyCode,
        bool pressed,
        bool repeat = false,
        bool alt = false,
        bool control = false,
        bool shift = false,
        bool meta = false)
    {
        var gate = CheckSession();
        if (!gate.Success) return gate;
        if (!_windowLoadDispatched)
            return SdkOperationResult.Failed("web.input-before-startup", "Window load must complete before gameplay input is dispatched.");
        if (keyCode is < 0 or > 65535)
            return SdkOperationResult.Failed("web.key-code-invalid", "Legacy keyCode is outside 0..65535.");
        var modifiers = (alt ? 1 : 0) | (control ? 2 : 0) | (shift ? 4 : 0) | (meta ? 8 : 0);
        _busy = true;
        _faulted = true;
        try
        {
            var result = _vm.Invoke(new ScriptInvocation
            {
                Target = NativeEntryPointHostPrelude.ControlTarget,
                Member = "dispatchKey",
                Arguments = new[]
                {
                    new ScriptValue(pressed ? "keydown" : "keyup"),
                    new ScriptValue(keyCode),
                    new ScriptValue(repeat),
                    new ScriptValue(modifiers),
                },
            });
            if (result.Success) _faulted = false;
            return result.Success ? result : SdkOperationResult.Failed("web.input-dispatch-failed", result.ErrorMessage, result.Diagnostics);
        }
        catch (System.Exception exception) when (!IsCritical(exception))
        {
            return ExternalFailure("input", keyCode.ToString(System.Globalization.CultureInfo.InvariantCulture), exception);
        }
        finally { _busy = false; }
    }

    /// <summary>Notify original Input handlers that application focus was lost.</summary>
    public SdkOperationResult DispatchFocusLost()
    {
        var gate = CheckSession();
        if (!gate.Success) return gate;
        if (!_windowLoadDispatched)
            return SdkOperationResult.Failed("web.input-before-startup", "Window load must complete before focus events are dispatched.");
        _busy = true; _faulted = true;
        try
        {
            var result = _vm.Invoke(new ScriptInvocation
            {
                Target = NativeEntryPointHostPrelude.ControlTarget,
                Member = "dispatchWindow",
                Arguments = new[] { new ScriptValue("blur") },
            });
            if (result.Success) _faulted = false;
            return result.Success ? result : SdkOperationResult.Failed("web.focus-dispatch-failed", result.ErrorMessage, result.Diagnostics);
        }
        catch (System.Exception exception) when (!IsCritical(exception))
        {
            return ExternalFailure("focus", "blur", exception);
        }
        finally { _busy = false; }
    }
}
