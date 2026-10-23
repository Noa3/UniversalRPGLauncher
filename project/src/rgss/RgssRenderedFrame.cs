using System;
using UniversalRPG.Rm2k.Rendering;

namespace UniversalRPG.Rgss;

/// <summary>An owned immutable RGBA snapshot; consumers receive copies, never worker storage.</summary>
public sealed class RgssRenderedFrame
{
    private readonly byte[] _pixels;
    public int Width { get; }
    public int Height { get; }
    public string Warning { get; }

    internal RgssRenderedFrame(Rm2kPixelBuffer pixels, string warning)
    {
        Width = pixels.Width;
        Height = pixels.Height;
        _pixels = pixels.Pixels;
        Warning = warning;
    }

    public Rm2kPixelBuffer CopyPixels()
    {
        var copy = new Rm2kPixelBuffer(Width, Height);
        Array.Copy(_pixels, copy.Pixels, _pixels.Length);
        return copy;
    }
}

public interface IRgssFrameSource
{
    int Frames { get; }
    RgssRenderedFrame? RenderedFrame { get; }
}
