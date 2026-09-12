using UniversalRPG.Sdk;

namespace UniversalRPG.Plugins;

public sealed partial class WebScriptRuntime
{
    public SdkOperationResult DispatchGamepadButton(int pDevice, int pStandardButton, bool pPressed, double pValue = -1)
    {
        var gate = CheckSession();
        if (!gate.Success) return gate;
        if (!_windowLoadDispatched)
            return SdkOperationResult.Failed("web.input-before-startup", "Window load must complete before gamepad state is dispatched.");
        if (pDevice is < 0 or >= NativeGamepadHostPrelude.MaxDevices
            || pStandardButton is < 0 or >= NativeGamepadHostPrelude.StandardButtons)
            return SdkOperationResult.Failed("web.gamepad-button-invalid", "Gamepad device/button is outside the standard bridge range.");
        var value = pValue < 0 ? (pPressed ? 1.0 : 0.0) : pValue;
        if (!double.IsFinite(value) || value < 0 || value > 1)
            return SdkOperationResult.Failed("web.gamepad-value-invalid", "Gamepad button value must be finite in [0,1].");
        return InvokeGamepad("setButton", new ScriptValue(pDevice), new ScriptValue(pStandardButton), new ScriptValue(pPressed), new ScriptValue(value));
    }

    public SdkOperationResult DispatchGamepadAxis(int pDevice, int pStandardAxis, double pValue)
    {
        var gate = CheckSession();
        if (!gate.Success) return gate;
        if (!_windowLoadDispatched)
            return SdkOperationResult.Failed("web.input-before-startup", "Window load must complete before gamepad state is dispatched.");
        if (pDevice is < 0 or >= NativeGamepadHostPrelude.MaxDevices
            || pStandardAxis is < 0 or >= NativeGamepadHostPrelude.StandardAxes
            || !double.IsFinite(pValue) || pValue < -1 || pValue > 1)
            return SdkOperationResult.Failed("web.gamepad-axis-invalid", "Gamepad axis input is outside the standard bridge range.");
        return InvokeGamepad("setAxis", new ScriptValue(pDevice), new ScriptValue(pStandardAxis), new ScriptValue(pValue));
    }

    public SdkOperationResult DispatchGamepadDisconnected(int pDevice)
    {
        var gate = CheckSession();
        if (!gate.Success) return gate;
        if (!_windowLoadDispatched)
            return SdkOperationResult.Failed("web.input-before-startup", "Window load must complete before gamepad state is dispatched.");
        if (pDevice is < 0 or >= NativeGamepadHostPrelude.MaxDevices)
            return SdkOperationResult.Failed("web.gamepad-device-invalid", "Gamepad device is outside the standard bridge range.");
        return InvokeGamepad("disconnect", new ScriptValue(pDevice));
    }

    private SdkOperationResult InvokeGamepad(string pMember, params ScriptValue[] pArguments)
    {
        _busy = true;
        _faulted = true;
        try
        {
            var result = _vm.Invoke(new ScriptInvocation
            {
                Target = NativeGamepadHostPrelude.ControlTarget,
                Member = pMember,
                Arguments = pArguments,
            });
            if (result.Success) _faulted = false;
            return result.Success ? result : SdkOperationResult.Failed("web.gamepad-dispatch-failed", result.ErrorMessage, result.Diagnostics);
        }
        catch (System.Exception exception) when (!IsCritical(exception))
        {
            return ExternalFailure("gamepad", pMember, exception);
        }
        finally { _busy = false; }
    }
}
