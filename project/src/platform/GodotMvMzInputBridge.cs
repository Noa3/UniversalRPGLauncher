using System;
using Godot;
using UniversalRPG.Plugins;
using UniversalRPG.Sdk;

namespace UniversalRPG.Platform;

/// <summary>
/// Maps Godot keyboard events to the legacy browser keyCode values consumed by
/// original MV/MZ Input implementations. Game logic remains in the imported
/// engine scripts; this class is only a platform adapter.
/// </summary>
public static class GodotLegacyKeyMapper
{
    public static bool TryMap(InputEventKey pEvent, out int pLegacyKeyCode)
    {
        if (pEvent == null) throw new ArgumentNullException(nameof(pEvent));
        var key = pEvent.Keycode != Key.None ? pEvent.Keycode : pEvent.PhysicalKeycode;
        return TryMap(key, out pLegacyKeyCode);
    }

    public static bool TryMap(Key pKey, out int pLegacyKeyCode)
    {
        pLegacyKeyCode = pKey switch
        {
            Key.Backspace => 8,
            Key.Tab => 9,
            Key.Enter or Key.KpEnter => 13,
            Key.Shift => 16,
            Key.Ctrl => 17,
            Key.Alt => 18,
            Key.Pause => 19,
            Key.Capslock => 20,
            Key.Escape => 27,
            Key.Space => 32,
            Key.Pageup => 33,
            Key.Pagedown => 34,
            Key.End => 35,
            Key.Home => 36,
            Key.Left => 37,
            Key.Up => 38,
            Key.Right => 39,
            Key.Down => 40,
            Key.Insert => 45,
            Key.Delete => 46,
            Key.Key0 => 48,
            Key.Key1 => 49,
            Key.Key2 => 50,
            Key.Key3 => 51,
            Key.Key4 => 52,
            Key.Key5 => 53,
            Key.Key6 => 54,
            Key.Key7 => 55,
            Key.Key8 => 56,
            Key.Key9 => 57,
            Key.A => 65,
            Key.B => 66,
            Key.C => 67,
            Key.D => 68,
            Key.E => 69,
            Key.F => 70,
            Key.G => 71,
            Key.H => 72,
            Key.I => 73,
            Key.J => 74,
            Key.K => 75,
            Key.L => 76,
            Key.M => 77,
            Key.N => 78,
            Key.O => 79,
            Key.P => 80,
            Key.Q => 81,
            Key.R => 82,
            Key.S => 83,
            Key.T => 84,
            Key.U => 85,
            Key.V => 86,
            Key.W => 87,
            Key.X => 88,
            Key.Y => 89,
            Key.Z => 90,
            Key.Meta => 91,
            Key.Kp0 => 96,
            Key.Kp1 => 97,
            Key.Kp2 => 98,
            Key.Kp3 => 99,
            Key.Kp4 => 100,
            Key.Kp5 => 101,
            Key.Kp6 => 102,
            Key.Kp7 => 103,
            Key.Kp8 => 104,
            Key.Kp9 => 105,
            Key.KpMultiply => 106,
            Key.KpAdd => 107,
            Key.KpSubtract => 109,
            Key.KpPeriod => 110,
            Key.KpDivide => 111,
            Key.F1 => 112,
            Key.F2 => 113,
            Key.F3 => 114,
            Key.F4 => 115,
            Key.F5 => 116,
            Key.F6 => 117,
            Key.F7 => 118,
            Key.F8 => 119,
            Key.F9 => 120,
            Key.F10 => 121,
            Key.F11 => 122,
            Key.F12 => 123,
            Key.Numlock => 144,
            Key.Scrolllock => 145,
            _ => 0,
        };
        return pLegacyKeyCode != 0;
    }
}

/// <summary>
/// Scene-tree bridge for a running MV/MZ script session. Attach only while the
/// imported game owns input. Unsupported keys remain available to the launcher.
/// Dispatch failures are reported without throwing from Godot's input callback.
/// </summary>
public partial class GodotMvMzInputBridge : Node
{
    private WebScriptRuntime? _runtime;

    public bool ConsumeMappedEvents { get; set; } = true;
    public string LastErrorCode { get; private set; } = "";
    public string LastErrorMessage { get; private set; } = "";
    public WebScriptRuntime? Runtime => _runtime;

    public event Action<SdkOperationResult>? DispatchFailed;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
    }

    public void Attach(WebScriptRuntime pRuntime)
    {
        _runtime = pRuntime ?? throw new ArgumentNullException(nameof(pRuntime));
        LastErrorCode = "";
        LastErrorMessage = "";
    }

    public void Detach()
    {
        _runtime = null;
        LastErrorCode = "";
        LastErrorMessage = "";
    }

    public override void _Input(InputEvent @event)
    {
        var runtime = _runtime;
        if (runtime == null || @event is not InputEventKey keyEvent
            || !GodotLegacyKeyMapper.TryMap(keyEvent, out var keyCode))
        {
            return;
        }

        var result = runtime.DispatchKeyEvent(
            keyCode,
            keyEvent.Pressed,
            keyEvent.Echo,
            keyEvent.AltPressed,
            keyEvent.CtrlPressed,
            keyEvent.ShiftPressed,
            keyEvent.MetaPressed);
        HandleResult(result);
        if (result.Success && ConsumeMappedEvents)
        {
            GetViewport().SetInputAsHandled();
        }
    }

    public override void _Notification(int pWhat)
    {
        if (pWhat != MainLoop.NotificationApplicationFocusOut || _runtime == null)
        {
            return;
        }
        HandleResult(_runtime.DispatchFocusLost());
    }

    private void HandleResult(SdkOperationResult pResult)
    {
        if (pResult.Success)
        {
            LastErrorCode = "";
            LastErrorMessage = "";
            return;
        }
        LastErrorCode = pResult.ErrorCode;
        LastErrorMessage = pResult.ErrorMessage;
        DispatchFailed?.Invoke(pResult);
    }
}
