using System;

namespace UniversalRPG.Rm2k.Rendering;

public enum RenderLayer
{
    Lower,
    Upper,
}

/// <summary>
/// Deterministic tile framebuffer. It stores map tile IDs without touching Godot rendering APIs.
/// </summary>
public sealed class VirtualFramebuffer
{
    private readonly int[] _lower;
    private readonly int[] _upper;

    public VirtualFramebuffer(int pWidth, int pHeight)
    {
        if (pWidth <= 0 || pHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pWidth), "Framebuffer dimensions must be positive.");
        }
        Width = pWidth;
        Height = pHeight;
        _lower = new int[checked(pWidth * pHeight)];
        _upper = new int[checked(pWidth * pHeight)];
    }

    public int Width { get; }
    public int Height { get; }

    public int GetTile(RenderLayer pLayer, int pX, int pY)
    {
        return TryGetIndex(pX, pY, out var index) ? GetLayer(pLayer)[index] : 0;
    }

    public bool TrySetTile(RenderLayer pLayer, int pX, int pY, int pTileId)
    {
        if (!TryGetIndex(pX, pY, out var index) || pTileId < 0)
        {
            return false;
        }
        GetLayer(pLayer)[index] = pTileId;
        return true;
    }

    public void SetTile(RenderLayer pLayer, int pX, int pY, int pTileId)
    {
        if (!TrySetTile(pLayer, pX, pY, pTileId))
        {
            throw new ArgumentOutOfRangeException(nameof(pX), "Tile coordinate or ID is outside framebuffer bounds.");
        }
    }

    private bool TryGetIndex(int pX, int pY, out int pIndex)
    {
        if (pX < 0 || pX >= Width || pY < 0 || pY >= Height)
        {
            pIndex = -1;
            return false;
        }
        pIndex = pY * Width + pX;
        return true;
    }

    private int[] GetLayer(RenderLayer pLayer)
    {
        return pLayer switch
        {
            RenderLayer.Lower => _lower,
            RenderLayer.Upper => _upper,
            _ => throw new ArgumentOutOfRangeException(nameof(pLayer)),
        };
    }
}

public sealed class RenderResult
{
    private RenderResult(bool pSuccess, VirtualFramebuffer? pFramebuffer, string pError)
    {
        Success = pSuccess;
        Framebuffer = pFramebuffer;
        Error = pError;
    }

    public bool Success { get; }
    public VirtualFramebuffer? Framebuffer { get; }
    public string Error { get; }

    public static RenderResult Succeeded(VirtualFramebuffer pFramebuffer) => new(true, pFramebuffer, "");
    public static RenderResult Failed(string pError) => new(false, null, pError);
}

/// <summary>
/// Converts bounded parser map output into a renderer-neutral framebuffer.
/// </summary>
public sealed class Rm2kRendererAdapter
{
    public RenderResult CreateFramebuffer(Godot.Collections.Dictionary pMapData)
    {
        if (!TryReadPositiveInt(pMapData, "width", out var width) ||
            !TryReadPositiveInt(pMapData, "height", out var height))
        {
            return RenderResult.Failed("Map renderer requires positive width and height.");
        }

        var expected = checked(width * height);
        if (!TryReadLayer(pMapData, "lower_layer", expected, out var lower, out var error) ||
            !TryReadLayer(pMapData, "upper_layer", expected, out var upper, out error))
        {
            return RenderResult.Failed(error);
        }

        var framebuffer = new VirtualFramebuffer(width, height);
        for (var index = 0; index < expected; index++)
        {
            var x = index % width;
            var y = index / width;
            framebuffer.SetTile(RenderLayer.Lower, x, y, lower[index]);
            framebuffer.SetTile(RenderLayer.Upper, x, y, upper[index]);
        }
        return RenderResult.Succeeded(framebuffer);
    }

    private static bool TryReadPositiveInt(Godot.Collections.Dictionary pData, string pKey, out int pValue)
    {
        pValue = 0;
        if (!pData.TryGetValue(pKey, out var raw) || raw.VariantType != Godot.Variant.Type.Int)
        {
            return false;
        }
        pValue = raw.AsInt32();
        return pValue > 0;
    }

    private static bool TryReadLayer(
        Godot.Collections.Dictionary pData,
        string pKey,
        int pExpected,
        out int[] pTiles,
        out string pError)
    {
        pTiles = Array.Empty<int>();
        pError = "";
        if (!pData.TryGetValue(pKey, out var raw))
        {
            pError = $"Map renderer requires integer layer '{pKey}'.";
            return false;
        }

        if (raw.VariantType == Godot.Variant.Type.PackedInt32Array)
        {
            var packed = raw.AsInt32Array();
            if (packed.Length != pExpected)
            {
                pError = $"Map renderer layer '{pKey}' has {packed.Length} tiles, expected {pExpected}.";
                return false;
            }
            pTiles = packed;
            return ValidateTiles(pKey, pTiles, out pError);
        }

        if (raw.VariantType != Godot.Variant.Type.Array)
        {
            pError = $"Map renderer requires integer layer '{pKey}'.";
            return false;
        }
        var values = raw.AsGodotArray();
        if (values.Count != pExpected)
        {
            pError = $"Map renderer layer '{pKey}' has {values.Count} tiles, expected {pExpected}.";
            return false;
        }
        pTiles = new int[pExpected];
        for (var index = 0; index < values.Count; index++)
        {
            var value = values[index];
            if (value.VariantType != Godot.Variant.Type.Int)
            {
                pError = $"Map renderer layer '{pKey}' contains a non-integer tile ID.";
                pTiles = Array.Empty<int>();
                return false;
            }
            pTiles[index] = value.AsInt32();
        }
        return ValidateTiles(pKey, pTiles, out pError);
    }

    private static bool ValidateTiles(string pKey, int[] pTiles, out string pError)
    {
        pError = "";
        foreach (var tile in pTiles)
        {
            if (tile < 0)
            {
                pError = $"Map renderer layer '{pKey}' contains a negative tile ID.";
                return false;
            }
        }
        return true;
    }
}
