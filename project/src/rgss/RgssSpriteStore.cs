using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UniversalRPG.Rm2k.Rendering;

namespace UniversalRPG.Rgss;

/// <summary>Worker-owned sprites. Implements only untransformed normal-blend sprites without viewports.</summary>
public sealed class RgssSpriteStore
{
    public const int MaximumSprites = 4096;
    public const int MaximumFramePixels = 4 * 1024 * 1024;
    public const long MaximumPixelWork = 16L * 1024 * 1024;
    private readonly Dictionary<long, RubyValue> _sprites = new();
    private long _nextId = 1;
    public int Count => _sprites.Count;
    public bool SortByY { get; set; }

    public RubyValue Call(IReadOnlyList<RubyValue> arguments, RgssBitmapStore bitmaps)
    {
        if (arguments.Count < 2 || arguments[0].Kind != RubyValueKind.Symbol)
            throw new RubyRuntimeException("ArgumentError", "rgss_sprite requires an operation and a receiver.");
        var operation = arguments[0].Name;
        if (arguments.Count != (operation is "assign" or "opacity" ? 3 : 2))
            throw new RubyRuntimeException("ArgumentError", "Wrong argument count for sprite " + operation);
        if (operation == "create")
        {
            if (arguments[1].Kind != RubyValueKind.Object)
                throw new RubyRuntimeException("TypeError", "A sprite receiver must be an object.");
            if (_sprites.Count >= MaximumSprites || _nextId == long.MaxValue)
                throw new RubyRuntimeException("RuntimeError", "The RGSS sprite resource limit was exceeded.");
            var id = _nextId++;
            _sprites.Add(id, arguments[1]);
            return RubyValue.OfInteger(id);
        }
        if (arguments[1].Kind != RubyValueKind.Integer)
            throw new RubyRuntimeException("RGSSError", "The sprite has no registered handle.");
        var handle = arguments[1].Integer;
        if (operation == "dispose")
        {
            _sprites.Remove(handle);
            return RubyValue.Nil;
        }
        if (operation == "copy")
            throw new RubyRuntimeException("TypeError", "Sprite copying is not supported by this renderer.");
        if (operation is not ("assign" or "opacity"))
            throw new RubyRuntimeException("ArgumentError", "Unknown sprite operation: " + operation);
        if (!_sprites.TryGetValue(handle, out var sprite))
            throw new RubyRuntimeException("RGSSError", "The sprite has been disposed.");
        if (operation == "opacity")
        {
            var value = arguments[2];
            var numeric = value.Kind == RubyValueKind.Integer ? value.Integer
                : value.Kind == RubyValueKind.Float ? value.Real : double.NaN;
            if (!double.IsFinite(numeric))
                throw new RubyRuntimeException("ArgumentError", "Sprite opacity must be a finite number.");
            var opacity = RubyValue.OfInteger((int)Math.Clamp(numeric, 0, 255));
            sprite.Felder["@opacity"] = opacity;
            return opacity;
        }
        var bitmap = arguments[2];
        if (bitmap.Kind != RubyValueKind.Nil && Field(bitmap, "pixel_surface").Kind != RubyValueKind.Integer)
            throw new RubyRuntimeException("TypeError", "Sprite.bitmap requires a Bitmap or nil.");
        if (ReferenceEquals(Field(sprite, "bitmap"), bitmap)) return RubyValue.OfBoolean(false);
        sprite.Felder["@bitmap"] = bitmap;
        return RubyValue.OfBoolean(bitmap.Kind != RubyValueKind.Nil
            && bitmaps.FindSurface(Field(bitmap, "pixel_surface").Integer) != null);
    }

    public void ReleaseAll() => _sprites.Clear();

    public RgssRenderedFrame Compose(RgssBitmapStore bitmaps, int width, int height, CancellationToken cancellation)
    {
        if (width <= 0 || height <= 0 || width > 4096 || height > 4096
            || (long)width * height > MaximumFramePixels)
            throw new RubyRuntimeException("ArgumentError", "The RGSS frame dimensions exceed the renderer limit.");
        cancellation.ThrowIfCancellationRequested();
        var frame = new Rm2kPixelBuffer(width, height);
        // The game screen has an opaque black backdrop. Straight-alpha texture RGB
        // is weighted by texture alpha and opacity; never use PaintOver's replacement operation.
        for (var offset = 3; offset < frame.Pixels.Length; offset += 4) frame.Pixels[offset] = 255;
        long work = 0;
        var unsupported = false;
        var ordered = _sprites.OrderBy(pair => Number(pair.Value, "z"))
            .ThenBy(pair => SortByY ? Number(pair.Value, "y") : 0).ThenBy(pair => pair.Key);
        foreach (var pair in ordered)
        {
            cancellation.ThrowIfCancellationRequested();
            var sprite = pair.Value;
            if (!Truthy(Field(sprite, "visible"))) continue;
            var bitmap = Field(sprite, "bitmap");
            var source = bitmaps.FindSurface(Field(bitmap, "pixel_surface").Integer);
            var opacity = Math.Clamp(Number(sprite, "opacity"), 0, 255);
            if (source == null || opacity == 0) continue;
            var color = Field(sprite, "color");
            var tone = Field(sprite, "tone");
            if (Field(sprite, "viewport").Kind != RubyValueKind.Nil
                || !Neutral(sprite, "angle", 0) || !Neutral(sprite, "zoom_x", 1)
                || !Neutral(sprite, "zoom_y", 1) || Truthy(Field(sprite, "mirror"))
                || !Neutral(sprite, "bush_depth", 0) || !Neutral(sprite, "blend_type", 0)
                || !Neutral(sprite, "wave_amp", 0) || !Neutral(color, "alpha", 0)
                || !Neutral(tone, "red", 0) || !Neutral(tone, "green", 0)
                || !Neutral(tone, "blue", 0) || !Neutral(tone, "gray", 0))
            {
                unsupported = true;
                continue;
            }
            var rect = Field(sprite, "src_rect");
            long sx = Number(rect, "x"), sy = Number(rect, "y");
            long dx = (long)Number(sprite, "x") - Number(sprite, "ox");
            long dy = (long)Number(sprite, "y") - Number(sprite, "oy");
            var left = Math.Max(0, Math.Max(-sx, -dx));
            var top = Math.Max(0, Math.Max(-sy, -dy));
            var right = Math.Min(Number(rect, "width"), Math.Min(source.Width - sx, width - dx));
            var bottom = Math.Min(Number(rect, "height"), Math.Min(source.Height - sy, height - dy));
            if (right <= left || bottom <= top) continue;
            work += (right - left) * (bottom - top);
            if (work > MaximumPixelWork)
                throw new RubyRuntimeException("RuntimeError", "The RGSS sprite composition work limit was exceeded.");
            for (var y = top; y < bottom; y++)
            {
                cancellation.ThrowIfCancellationRequested();
                var sourceOffset = (int)(((sy + y) * source.Width + sx + left) * 4);
                var targetOffset = (int)(((dy + y) * width + dx + left) * 4);
                for (var x = left; x < right; x++, sourceOffset += 4, targetOffset += 4)
                {
                    var alpha = source.Pixels[sourceOffset + 3] * opacity;
                    for (var channel = 0; channel < 3; channel++)
                        frame.Pixels[targetOffset + channel] = (byte)((source.Pixels[sourceOffset + channel] * alpha
                            + frame.Pixels[targetOffset + channel] * (65025 - alpha) + 32512) / 65025);
                }
            }
        }
        return new RgssRenderedFrame(frame, unsupported
            ? "Unsupported sprite viewports, transforms or effects were omitted."
            : string.Empty);
    }

    private static RubyValue Field(RubyValue value, string name) =>
        value.Felder.TryGetValue("@" + name, out var field) ? field : RubyValue.Nil;
    private static bool Truthy(RubyValue value) => value.Kind != RubyValueKind.Nil
        && (value.Kind != RubyValueKind.Boolean || value.Boolean);
    private static bool Neutral(RubyValue value, string name, double expected)
    {
        var field = Field(value, name);
        return field.Kind == RubyValueKind.Nil || (field.Kind == RubyValueKind.Integer
            ? field.Integer == expected : field.Kind == RubyValueKind.Float && field.Real == expected);
    }

    private static int Number(RubyValue value, string name, int fallback = 0)
    {
        var field = Field(value, name);
        if (field.Kind == RubyValueKind.Nil) return fallback;
        var number = field.Kind == RubyValueKind.Integer ? field.Integer
            : field.Kind == RubyValueKind.Float ? field.Real : double.NaN;
        if (!double.IsFinite(number) || number < int.MinValue || number > int.MaxValue)
            throw new RubyRuntimeException("ArgumentError", "Invalid sprite property: " + name);
        return (int)number;
    }
}
