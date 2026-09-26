using Godot;
using UniversalRPG.Rm2k.Rendering;

namespace UniversalRPG.App.Ui;

public partial class Rm2kMapPreview : Control
{
    private Godot.Collections.Dictionary? _mapData;
    private VirtualFramebuffer? _framebuffer;
    private Rm2kPixelBuffer? _renderedMap;
    private ImageTexture? _renderedTexture;
    private string _renderedSignature = "";
    private int _playerX;
    private int _playerY;

    public void SetMapData(Godot.Collections.Dictionary? pMapData)
    {
        _mapData = pMapData;
        QueueRedraw();
    }

    public void SetPlayerPosition(int pMapX, int pMapY)
    {
        _playerX = pMapX;
        _playerY = pMapY;
        QueueRedraw();
    }

    public void SetFramebuffer(VirtualFramebuffer? pFramebuffer)
    {
        if (ReferenceEquals(_framebuffer, pFramebuffer))
        {
            return;
        }
        _framebuffer = pFramebuffer;
        QueueRedraw();
    }

    /// <summary>
    /// Sets the chipset rendered pixels. The tile id framebuffer is kept for the
    /// tile queries, so the real map image and the diagnostic view coexist.
    /// </summary>
    public void SetRenderedMap(Rm2kPixelBuffer? pRenderedMap)
    {
        if (ReferenceEquals(_renderedMap, pRenderedMap))
        {
            return;
        }
        _renderedMap = pRenderedMap;
        _renderedTexture = null;
        _renderedSignature = "";
        QueueRedraw();
    }

    /// <summary>Why the map image is missing, empty when the map is rendered.</summary>
    public string RenderDiagnostic { get; set; } = "";

    /// <summary>
    /// Uploads the rendered pixels once per change. The signature guards against
    /// re-uploading a 320 by 240 image on every frame.
    /// </summary>
    private ImageTexture? GetRenderedTexture()
    {
        if (_renderedMap == null)
        {
            return null;
        }
        var signature = $"{_renderedMap.Width}x{_renderedMap.Height}:{_renderedMap.Pixels.Length}";
        if (_renderedTexture != null && signature == _renderedSignature)
        {
            return _renderedTexture;
        }
        var image = Image.CreateFromData(
            _renderedMap.Width,
            _renderedMap.Height,
            false,
            Image.Format.Rgba8,
            _renderedMap.Pixels);
        if (image == null)
        {
            return null;
        }
        _renderedTexture = ImageTexture.CreateFromImage(image);
        _renderedSignature = signature;
        return _renderedTexture;
    }

    public bool TryGetPreviewTile(RenderLayer pLayer, int pX, int pY, out int pTileId)
    {
        pTileId = 0;
        if (_framebuffer != null)
        {
            if (pX < 0 || pX >= _framebuffer.Width || pY < 0 || pY >= _framebuffer.Height)
            {
                return false;
            }
            pTileId = _framebuffer.GetTile(pLayer, pX, pY);
            return true;
        }
        if (_mapData == null || !TryReadInt("width", out var width) || !TryReadInt("height", out var height)
            || pX < 0 || pX >= width || pY < 0 || pY >= height)
        {
            return false;
        }
        var key = pLayer == RenderLayer.Lower ? "lower_layer" : "upper_layer";
        var tiles = ReadTiles(key, checked(width * height));
        pTileId = tiles[pY * width + pX];
        return true;
    }

    public override void _Draw()
    {
        DrawRect(new Rect2(Vector2.Zero, Size), new Color("111119"), true);
        if (TryDrawRenderedMap())
        {
            return;
        }
        if (_framebuffer != null)
        {
            DrawFramebuffer(_framebuffer);
            return;
        }
        if (_mapData == null || !TryReadInt("width", out var width) || !TryReadInt("height", out var height) || width <= 0 || height <= 0)
        {
            DrawString(ThemeDB.FallbackFont, new Vector2(12, 24), "No parsed RM2K map", HorizontalAlignment.Left, -1, 14, new Color("aaa7b5"));
            return;
        }

        var lower = ReadTiles("lower_layer", width * height);
        var upper = ReadTiles("upper_layer", width * height);
        var tileSize = Mathf.Max(4.0f, Mathf.Min(Size.X / width, Size.Y / height));
        var origin = new Vector2((Size.X - width * tileSize) / 2.0f, (Size.Y - height * tileSize) / 2.0f);
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var index = y * width + x;
                var lowerId = lower[index];
                var upperId = upper[index];
                var color = lowerId == 0 ? new Color("20202b") : new Color("3b4552");
                DrawRect(new Rect2(origin + new Vector2(x, y) * tileSize, new Vector2(tileSize - 1, tileSize - 1)), color, true);
                if (upperId != 0)
                {
                    DrawRect(new Rect2(origin + new Vector2(x, y) * tileSize + Vector2.One * tileSize * 0.25f, Vector2.One * tileSize * 0.5f), new Color("e8a24a"), true);
                }
            }
        }

        DrawEvents(origin, tileSize, width, height);
        if (_playerX >= 0 && _playerX < width && _playerY >= 0 && _playerY < height)
        {
            var playerPosition = origin + new Vector2(_playerX, _playerY) * tileSize;
            DrawCircle(playerPosition + Vector2.One * tileSize * 0.5f, tileSize * 0.28f, new Color("6fcf97"));
            DrawArc(playerPosition + Vector2.One * tileSize * 0.5f, tileSize * 0.32f, 0, Mathf.Tau, 16, new Color("d8ffe8"), 1.5f);
        }
    }

    /// <summary>
    /// Draws the real chipset pixels scaled to the control, keeping the pixel
    /// aspect ratio and the nearest neighbour filter so the map keeps the
    /// 16 pixel tile grid instead of a blurred version of it.
    /// </summary>
    private bool TryDrawRenderedMap()
    {
        var texture = GetRenderedTexture();
        if (texture == null)
        {
            return false;
        }
        var scale = Mathf.Min(Size.X / texture.GetWidth(), Size.Y / texture.GetHeight());
        if (scale <= 0.0f)
        {
            return false;
        }
        var drawn = new Vector2(texture.GetWidth() * scale, texture.GetHeight() * scale);
        var origin = new Vector2((Size.X - drawn.X) / 2.0f, (Size.Y - drawn.Y) / 2.0f);
        DrawTextureRect(
            texture,
            new Rect2(origin, drawn),
            false,
            Colors.White,
            false);
        var tileSize = drawn.X / Rm2kChipsetBitmap.TileSize;
        DrawPlayerMarker(origin, tileSize, _framebuffer?.Width ?? 0, _framebuffer?.Height ?? 0);
        if (!string.IsNullOrEmpty(RenderDiagnostic))
        {
            DrawString(ThemeDB.FallbackFont, new Vector2(12, Size.Y - 12),
                RenderDiagnostic, HorizontalAlignment.Left, Size.X - 24, 12, new Color("e8a24a"));
        }
        return true;
    }

    private void DrawPlayerMarker(Vector2 pOrigin, float pTileSize, int pMapWidth, int pMapHeight)
    {
        if (_playerX < 0 || _playerY < 0 || _playerX >= pMapWidth || _playerY >= pMapHeight)
        {
            return;
        }
        var position = pOrigin + new Vector2(_playerX + 0.5f, _playerY + 0.5f) * pTileSize;
        DrawRect(new Rect2(position - Vector2.One * pTileSize * 0.4f,
            Vector2.One * pTileSize * 0.8f), new Color(0.43f, 0.81f, 0.59f, 0.55f), true);
        DrawRect(new Rect2(position - Vector2.One * pTileSize * 0.4f,
            Vector2.One * pTileSize * 0.8f), new Color("d8ffe8"), false, 1.0f);
    }

    private void DrawFramebuffer(VirtualFramebuffer pFramebuffer)
    {
        var width = pFramebuffer.Width;
        var height = pFramebuffer.Height;
        if (width <= 0 || height <= 0)
        {
            DrawString(ThemeDB.FallbackFont, new Vector2(12, 24), "Invalid framebuffer dimensions",
                HorizontalAlignment.Left, -1, 14, new Color("aaa7b5"));
            return;
        }

        var tileSize = Mathf.Max(4.0f, Mathf.Min(Size.X / width, Size.Y / height));
        var origin = new Vector2((Size.X - width * tileSize) / 2.0f, (Size.Y - height * tileSize) / 2.0f);
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var lowerId = pFramebuffer.GetTile(RenderLayer.Lower, x, y);
                var upperId = pFramebuffer.GetTile(RenderLayer.Upper, x, y);
                var tileRect = new Rect2(origin + new Vector2(x, y) * tileSize,
                    new Vector2(tileSize - 1, tileSize - 1));
                DrawRect(tileRect, lowerId == 0 ? new Color("20202b") : new Color("3b4552"), true);
                if (upperId != 0)
                {
                    DrawRect(new Rect2(tileRect.Position + Vector2.One * tileSize * 0.25f,
                        Vector2.One * tileSize * 0.5f), new Color("e8a24a"), true);
                }
            }
        }

        DrawEvents(origin, tileSize, width, height);
        if (_playerX >= 0 && _playerX < width && _playerY >= 0 && _playerY < height)
        {
            var playerPosition = origin + new Vector2(_playerX, _playerY) * tileSize;
            DrawCircle(playerPosition + Vector2.One * tileSize * 0.5f, tileSize * 0.28f, new Color("6fcf97"));
            DrawArc(playerPosition + Vector2.One * tileSize * 0.5f, tileSize * 0.32f,
                0, Mathf.Tau, 16, new Color("d8ffe8"), 1.5f);
        }
    }

    private void DrawEvents(Vector2 pOrigin, float pTileSize, int pWidth, int pHeight)
    {
        if (_mapData == null || !_mapData.TryGetValue("events", out var raw) || raw.VariantType != Variant.Type.Array)
        {
            return;
        }
        foreach (var value in raw.AsGodotArray())
        {
            if (value.VariantType != Variant.Type.Dictionary)
            {
                continue;
            }
            var eventData = value.AsGodotDictionary();
            if (!TryGetInt(eventData, "x", out var x) || !TryGetInt(eventData, "y", out var y)
                || x < 0 || x >= pWidth || y < 0 || y >= pHeight)
            {
                continue;
            }
            var eventPosition = pOrigin + new Vector2(x, y) * pTileSize;
            DrawRect(new Rect2(eventPosition + Vector2.One * pTileSize * 0.3f, Vector2.One * pTileSize * 0.4f), new Color("e06c75"), true);
        }
    }

    private static bool TryGetInt(Godot.Collections.Dictionary pDictionary, string pKey, out int pValue)
    {
        pValue = 0;
        return pDictionary.TryGetValue(pKey, out var raw) && raw.VariantType == Variant.Type.Int
            && (pValue = raw.AsInt32()) >= 0;
    }

    private int[] ReadTiles(string pKey, int pExpected)
    {
        if (_mapData == null || !_mapData.TryGetValue(pKey, out var raw)) return new int[pExpected];
        if (raw.VariantType == Variant.Type.PackedInt32Array)
        {
            var packed = raw.AsInt32Array();
            return packed.Length == pExpected ? packed : new int[pExpected];
        }
        if (raw.VariantType == Variant.Type.Array)
        {
            var values = raw.AsGodotArray();
            if (values.Count != pExpected)
            {
                return new int[pExpected];
            }
            var result = new int[pExpected];
            for (var index = 0; index < values.Count; index++)
            {
                if (values[index].VariantType != Variant.Type.Int)
                {
                    return new int[pExpected];
                }
                result[index] = values[index].AsInt32();
            }
            return result;
        }
        return new int[pExpected];
    }

    private bool TryReadInt(string pKey, out int pValue)
    {
        pValue = 0;
        return _mapData != null && _mapData.TryGetValue(pKey, out var raw) && raw.VariantType == Variant.Type.Int && (pValue = raw.AsInt32()) > 0;
    }
}
