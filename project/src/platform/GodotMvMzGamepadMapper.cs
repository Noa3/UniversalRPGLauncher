using Godot;

namespace UniversalRPG.Platform;

/// <summary>Maps Godot's SDL-style controller layout to the browser Standard Gamepad layout.</summary>
public static class GodotStandardGamepadMapper
{
    public static bool TryMapButton(JoyButton pButton, out int pStandardButton)
    {
        pStandardButton = pButton switch
        {
            JoyButton.A => 0,
            JoyButton.B => 1,
            JoyButton.X => 2,
            JoyButton.Y => 3,
            JoyButton.LeftShoulder => 4,
            JoyButton.RightShoulder => 5,
            JoyButton.Back => 8,
            JoyButton.Start => 9,
            JoyButton.LeftStick => 10,
            JoyButton.RightStick => 11,
            JoyButton.DpadUp => 12,
            JoyButton.DpadDown => 13,
            JoyButton.DpadLeft => 14,
            JoyButton.DpadRight => 15,
            JoyButton.Guide => 16,
            _ => -1,
        };
        return pStandardButton >= 0;
    }

    public static bool TryMapAxis(JoyAxis pAxis, out int pStandardAxis)
    {
        pStandardAxis = pAxis switch
        {
            JoyAxis.LeftX => 0,
            JoyAxis.LeftY => 1,
            JoyAxis.RightX => 2,
            JoyAxis.RightY => 3,
            _ => -1,
        };
        return pStandardAxis >= 0;
    }

    public static bool TryMapTrigger(JoyAxis pAxis, double pRawValue, out int pStandardButton, out double pValue)
    {
        pStandardButton = pAxis switch
        {
            JoyAxis.TriggerLeft => 6,
            JoyAxis.TriggerRight => 7,
            _ => -1,
        };
        pValue = 0;
        if (pStandardButton < 0 || !double.IsFinite(pRawValue) || pRawValue is < -1 or > 1) return false;
        // Godot's standard trigger mappings are normally 0..1, while some
        // backends/controllers expose -1 as rest. Accept both normalized forms.
        pValue = pRawValue < 0 ? (pRawValue + 1.0) * 0.5 : pRawValue;
        pValue = System.Math.Clamp(pValue, 0.0, 1.0);
        return true;
    }
}
