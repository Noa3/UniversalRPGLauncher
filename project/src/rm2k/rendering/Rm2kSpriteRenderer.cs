using System;
using System.Collections.Generic;

namespace UniversalRPG.Rm2k.Rendering;

public enum Rm2kSpriteKind
{
    Player,
    Event,
}

public sealed class Rm2kSpriteDescriptor
{
    public Rm2kSpriteKind Kind { get; init; }
    public int EventId { get; init; }
    public int X { get; init; }
    public int Y { get; init; }
    public string Name { get; init; } = "";
}

public sealed class Rm2kCameraState
{
    public Rm2kCameraState(int pMapWidth, int pMapHeight, int pViewportWidth, int pViewportHeight)
    {
        if (pMapWidth <= 0 || pMapHeight <= 0 || pViewportWidth <= 0 || pViewportHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(pMapWidth), "Map and viewport dimensions must be positive.");
        MapWidth = pMapWidth;
        MapHeight = pMapHeight;
        ViewportWidth = Math.Min(pViewportWidth, pMapWidth);
        ViewportHeight = Math.Min(pViewportHeight, pMapHeight);
    }

    public int MapWidth { get; }
    public int MapHeight { get; }
    public int ViewportWidth { get; }
    public int ViewportHeight { get; }
    public int CenterX { get; private set; }
    public int CenterY { get; private set; }

    public void SetCenter(int pX, int pY)
    {
        var halfWidth = ViewportWidth / 2;
        var halfHeight = ViewportHeight / 2;
        CenterX = Math.Clamp(pX, halfWidth, MapWidth - (ViewportWidth - halfWidth));
        CenterY = Math.Clamp(pY, halfHeight, MapHeight - (ViewportHeight - halfHeight));
    }
}

public sealed class SpriteDescriptorResult
{
    private SpriteDescriptorResult(bool pSuccess, List<Rm2kSpriteDescriptor> pDescriptors, string pError)
    {
        Success = pSuccess;
        Descriptors = pDescriptors;
        Error = pError;
    }

    public bool Success { get; }
    public List<Rm2kSpriteDescriptor> Descriptors { get; }
    public string Error { get; }
    public static SpriteDescriptorResult Succeeded(List<Rm2kSpriteDescriptor> pItems) => new(true, pItems, "");
    public static SpriteDescriptorResult Failed(string pError) => new(false, new List<Rm2kSpriteDescriptor>(), pError);
}

public sealed class Rm2kSpriteAdapter
{
    public SpriteDescriptorResult BuildDescriptors(Godot.Collections.Dictionary pMapData, int pPlayerX, int pPlayerY)
    {
        if (!ReadPositiveInt(pMapData, "width", out var width) || !ReadPositiveInt(pMapData, "height", out var height))
            return SpriteDescriptorResult.Failed("Sprite adapter requires positive map dimensions.");
        if (!Inside(pPlayerX, pPlayerY, width, height))
            return SpriteDescriptorResult.Failed("Player position is outside map bounds.");

        var result = new List<Rm2kSpriteDescriptor>
        {
            new() { Kind = Rm2kSpriteKind.Player, X = pPlayerX, Y = pPlayerY, Name = "Player" }
        };
        if (pMapData.TryGetValue("events", out var rawEvents) && rawEvents.VariantType == Godot.Variant.Type.Array)
        {
            foreach (var raw in rawEvents.AsGodotArray())
            {
                if (raw.VariantType != Godot.Variant.Type.Dictionary)
                    return SpriteDescriptorResult.Failed("Map event entry is not a dictionary.");
                var item = raw.AsGodotDictionary();
                if (!ReadInt(item, "id", out var id) || !ReadInt(item, "x", out var x) || !ReadInt(item, "y", out var y) || !Inside(x, y, width, height))
                    return SpriteDescriptorResult.Failed("Map event descriptor is malformed or outside map bounds.");
                var name = ReadString(item, "name");
                result.Add(new() { Kind = Rm2kSpriteKind.Event, EventId = id, X = x, Y = y, Name = name });
            }
        }
        return SpriteDescriptorResult.Succeeded(result);
    }

    private static bool ReadPositiveInt(Godot.Collections.Dictionary pData, string pKey, out int pValue)
    {
        return ReadInt(pData, pKey, out pValue) && pValue > 0;
    }

    private static bool ReadInt(Godot.Collections.Dictionary pData, string pKey, out int pValue)
    {
        pValue = 0;
        if (!pData.TryGetValue(pKey, out var raw) || raw.VariantType != Godot.Variant.Type.Int) return false;
        pValue = raw.AsInt32();
        return true;
    }

    private static string ReadString(Godot.Collections.Dictionary pData, string pKey)
    {
        return pData.TryGetValue(pKey, out var raw) && raw.VariantType == Godot.Variant.Type.String ? raw.AsString() : "";
    }

    private static bool Inside(int pX, int pY, int pWidth, int pHeight) => pX >= 0 && pX < pWidth && pY >= 0 && pY < pHeight;
}

public sealed class Rm2kSpriteRendererPlaceholder { }
