using System;
using Godot;

namespace UniversalRPG.Rm2k.Rendering;

public enum RenderCompatibilityMode
{
    Faithful,
    Enhanced,
}

/// <summary>
/// Explicit renderer policy. Faithful mode preserves the original integer
/// presentation contract; Enhanced mode may use non-integer scaling.
/// </summary>
public sealed class RenderProfile
{
    public const int MinIntegerScale = 1;
    public const int MaxIntegerScale = 8;

    public RenderCompatibilityMode Mode { get; private set; } = RenderCompatibilityMode.Faithful;
    public bool IntegerScaling { get; private set; } = true;
    public int IntegerScale { get; private set; } = MinIntegerScale;

    public bool TrySetMode(RenderCompatibilityMode pMode)
    {
        Mode = pMode;
        if (pMode == RenderCompatibilityMode.Faithful)
        {
            IntegerScaling = true;
        }
        return true;
    }

    public bool TrySetIntegerScaling(bool pEnabled)
    {
        if (Mode == RenderCompatibilityMode.Faithful && !pEnabled)
        {
            return false;
        }
        IntegerScaling = pEnabled;
        return true;
    }

    public bool TrySetIntegerScale(int pScale)
    {
        if (pScale < MinIntegerScale || pScale > MaxIntegerScale)
        {
            return false;
        }
        IntegerScale = pScale;
        IntegerScaling = true;
        return true;
    }

    public Vector2I CalculateViewportSize(Vector2I pBaseSize, Vector2I pViewportSize)
    {
        if (pBaseSize.X <= 0 || pBaseSize.Y <= 0 || pViewportSize.X <= 0 || pViewportSize.Y <= 0)
        {
            return Vector2I.Zero;
        }
        if (!IntegerScaling)
        {
            return pViewportSize;
        }
        var fitScale = Math.Min(pViewportSize.X / pBaseSize.X, pViewportSize.Y / pBaseSize.Y);
        var scale = Math.Clamp(Math.Min(fitScale, IntegerScale), MinIntegerScale, MaxIntegerScale);
        return pBaseSize * scale;
    }
}
