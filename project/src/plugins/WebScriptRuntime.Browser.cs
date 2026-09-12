using System;
using System.Linq;
using UniversalRPG.Sdk;

namespace UniversalRPG.Plugins;

public sealed partial class WebScriptRuntime
{
    private readonly WebBrowserHostOptions? _browserHostOptions;

    /// <summary>
    /// Only the timer/animation/currentScript subset is installed. This does not
    /// advertise a complete DOM or make the game's engine plugin launchable.
    /// </summary>
    public bool HasBrowserFrameHost => _browserHostOptions != null;

    /// <summary>
    /// Advance the virtual browser clock in seconds after successful bootstrap.
    /// Pump every due callback through ONE VM invocation so the VM's time and
    /// statement budget applies to the whole batch, not separately per callback.
    /// A caller pauses by not pumping; monitor refresh does not change elapsed time.
    /// </summary>
    public SdkOperationResult AdvanceFrame(double pDeltaSeconds)
    {
        var gate = CheckSession();
        if (!gate.Success) return gate;
        if (_browserHostOptions == null)
            return SdkOperationResult.Failed("web.frame-host-unavailable", "This session has no browser frame host.");
        if (!_loaded) return SdkOperationResult.Failed("web.not-loaded", "Load scripts before advancing the frame host.");
        if (!_bootstrapped) return SdkOperationResult.Failed("web.not-bootstrapped", "Bootstrap must complete before advancing frames.");
        if (!double.IsFinite(pDeltaSeconds) || pDeltaSeconds < 0
            || pDeltaSeconds > _browserHostOptions.MaxFrameMilliseconds / 1000.0)
            return SdkOperationResult.Failed("web.invalid-frame-delta", "Frame time is not finite, is negative, or exceeds the configured limit.");

        _busy = true;
        _faulted = true;
        try
        {
            var result = InvokeBrowserControl("advance", new ScriptValue(Math.Min(pDeltaSeconds * 1000.0, _browserHostOptions.MaxFrameMilliseconds)));
            if (!result.Success)
                return SdkOperationResult.Failed("web.frame-execution-failed",
                    $"Browser frame failed [{result.ErrorCode}]: {result.ErrorMessage}", result.Diagnostics);
            _faulted = false;
            return result;
        }
        catch (Exception exception) when (!IsCritical(exception))
        {
            return ExternalFailure("frame", WebBrowserHostPrelude.ControlTarget, exception);
        }
        finally { _busy = false; }
    }

    private SdkOperationResult EnterPluginScript(string relativePath)
    {
        if (_browserHostOptions == null) return SdkOperationResult.Succeeded();
        // Synthetic absolute source metadata. Never fetch this URI. Preserve
        // plugin path identity while escaping spaces, Unicode and URI delimiters.
        var src = "urpg://game/" + string.Join("/", relativePath.Replace('\\', '/').Split('/').Select(Uri.EscapeDataString));
        return InvokeBrowserControl("enterScript", new ScriptValue(src));
    }

    private SdkOperationResult LeavePluginScript()
        => _browserHostOptions == null ? SdkOperationResult.Succeeded() : InvokeBrowserControl("leaveScript");

    private SdkOperationResult InvokeBrowserControl(string member, params ScriptValue[] arguments)
        => _vm.Invoke(new ScriptInvocation
        {
            Target = WebBrowserHostPrelude.ControlTarget,
            Member = member,
            Arguments = arguments,
        });
}
