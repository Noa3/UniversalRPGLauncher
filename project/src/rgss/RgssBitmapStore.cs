using System;
using System.Collections.Generic;
using UniversalRPG.Rm2k.Rendering;

namespace UniversalRPG.Rgss;

/// <summary>Worker-owned RGBA storage. Limits are checked before allocating pixel arrays.</summary>
public sealed class RgssBitmapStore
{
    private readonly Dictionary<long, Rm2kPixelBuffer> _surfaces = new();
    private long _nextId = 1;

    public const int MaximumDimension = 4096;
    public const int MaximumSurfaces = 4096;
    public const long MaximumBytes = 64L * 1024 * 1024;
    public long AllocatedBytes { get; private set; }
    public int Count => _surfaces.Count;

    public long Create(int width, int height)
    {
        if (width <= 0 || height <= 0 || width > MaximumDimension || height > MaximumDimension)
            throw new RubyRuntimeException("ArgumentError", "Bitmap dimensions must be positive and no larger than 4096.");
        var bytes = (long)width * height * 4;
        if (_surfaces.Count >= MaximumSurfaces || bytes > MaximumBytes - AllocatedBytes || _nextId == long.MaxValue)
            throw new RubyRuntimeException("RuntimeError", "The RGSS bitmap resource limit was exceeded.");
        var surface = new Rm2kPixelBuffer(width, height);
        var id = _nextId++;
        _surfaces.Add(id, surface);
        AllocatedBytes += bytes;
        return id;
    }

    public long Copy(long id)
    {
        var source = Require(id);
        var copy = Create(source.Width, source.Height);
        Array.Copy(source.Pixels, _surfaces[copy].Pixels, source.Pixels.Length);
        return copy;
    }

    public void SetPixel(long id, int x, int y, byte red, byte green, byte blue, byte alpha) =>
        Require(id).TrySetPixel(x, y, red, green, blue, alpha);

    public RubyValue GetPixel(long id, int x, int y)
    {
        var surface = Require(id);
        var channels = new List<RubyValue>(4);
        var inside = x >= 0 && y >= 0 && x < surface.Width && y < surface.Height;
        var offset = inside ? (x + y * surface.Width) * 4 : 0;
        for (var channel = 0; channel < 4; channel++)
            channels.Add(RubyValue.OfInteger(inside ? surface.Pixels[offset + channel] : 0));
        return RubyValue.OfArray(channels);
    }

    /// <summary>Normalize negative extents and clip before touching the bounded buffer.</summary>
    public void FillRect(long id, int x, int y, int width, int height,
        byte red, byte green, byte blue, byte alpha)
    {
        var surface = Require(id);
        // Long arithmetic also covers int.MinValue extents without negation overflow.
        var x0 = (int)Math.Clamp(Math.Min((long)x, (long)x + width), 0, surface.Width);
        var y0 = (int)Math.Clamp(Math.Min((long)y, (long)y + height), 0, surface.Height);
        var x1 = (int)Math.Clamp(Math.Max((long)x, (long)x + width), 0, surface.Width);
        var y1 = (int)Math.Clamp(Math.Max((long)y, (long)y + height), 0, surface.Height);
        for (var row = y0; row < y1; row++)
        {
            var offset = (row * surface.Width + x0) * 4;
            for (var column = x0; column < x1; column++)
            {
                surface.Pixels[offset++] = red;
                surface.Pixels[offset++] = green;
                surface.Pixels[offset++] = blue;
                surface.Pixels[offset++] = alpha;
            }
        }
    }

    public void Clear(long id) => Require(id).Clear();

    public void DisposeBitmap(long id)
    {
        if (_surfaces.Remove(id, out var surface))
            AllocatedBytes -= surface.Pixels.LongLength;
    }

    public void ReleaseAll()
    {
        _surfaces.Clear();
        AllocatedBytes = 0;
        // Never recycle IDs: an older Ruby object must not alias a later allocation.
    }

    private Rm2kPixelBuffer Require(long id) => _surfaces.TryGetValue(id, out var surface)
        ? surface
        : throw new RubyRuntimeException("RGSSError", "The bitmap has been disposed or has no pixel surface.");
}
