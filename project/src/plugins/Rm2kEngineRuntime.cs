using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniversalRPG.Core;
using UniversalRPG.Rm2k;
using UniversalRPG.Rm2k.Interpreter;
using UniversalRPG.Rm2k.Parser;
using UniversalRPG.Rm2k.Presentation;
using UniversalRPG.Rm2k.Rendering;
using UniversalRPG.Rm2k.Simulation;

namespace UniversalRPG.Plugins;

/// <summary>
/// Minimal native RM2K/RM2K3 runtime backend. It loads the validated LDB/LMT
/// and first LMU through the existing bounded parser, then advances a
/// deterministic 60 Hz simulation clock. Decoded native event pages are driven
/// by the scheduler during Update(); unsupported commands remain data-only and
/// diagnostic. Launching no longer requires the original RPG_RT executable.
/// </summary>
public sealed class Rm2kEngineRuntime : IEngineRuntime, IRuntimeSaveTools, IRuntimeDebugTools
{
    private const int MaxGold = 999999;
    private readonly string _pluginId;
    private readonly PluginGameInfo _game;
    private readonly Rm2kParser _parser = new();
    private readonly VirtualClock _clock = new();
    private readonly Rm2kEventScheduler _eventScheduler;
    private readonly Rm2kPlayerTurn _playerTurn;
    private readonly Rm2kRendererAdapter _rendererAdapter = new();
    private readonly Rm2kSpriteAdapter _spriteAdapter = new();
    private bool _debugToolsEnabled;

    public Rm2kEngineRuntime(string pPluginId, PluginGameInfo pGame)
    {
        _pluginId = pPluginId;
        _game = pGame;
        _eventScheduler = new Rm2kEventScheduler(Simulation, Presentation);
        _playerTurn = new Rm2kPlayerTurn(Simulation, _eventScheduler);
    }

    public PluginRuntimeState State { get; private set; } = PluginRuntimeState.Created;
    public Godot.Collections.Dictionary? DatabaseData { get; private set; }
    public Godot.Collections.Dictionary? MapTreeData { get; private set; }
    public Godot.Collections.Dictionary? CurrentMapData { get; private set; }
    public VirtualFramebuffer? Framebuffer { get; private set; }
    public IReadOnlyList<Rm2kSpriteDescriptor> SpriteDescriptors { get; private set; } = Array.Empty<Rm2kSpriteDescriptor>();
    public PresentationState Presentation { get; } = new();
    public GameSimulationState Simulation { get; } = new();
    public Rm2kEventScheduler EventScheduler => _eventScheduler;
    public int SimulationTicks => _clock.GetSimulationTicks();
    public bool DebugToolsEnabled => _debugToolsEnabled;

    public PluginOperationResult Initialize(EnginePluginRuntimeContext pContext)
    {
        if (State != PluginRuntimeState.Created)
        {
            return Fail(PluginErrorCode.InvalidLifecycleTransition, "RM2K runtime was already initialized.", "initialize");
        }
        var root = ResolveGameDirectory();
        if (root == null)
        {
            return Fail(PluginErrorCode.InvalidGame, "RM2K/RM2K3 runtime requires an imported game directory.", "initialize");
        }

        var databasePath = FindRootFile(root, "RPG_RT.ldb");
        var mapTreePath = FindRootFile(root, "RPG_RT.lmt");
        if (databasePath == null || mapTreePath == null)
        {
            return Fail(PluginErrorCode.InvalidGame, "RM2K/RM2K3 requires RPG_RT.ldb and RPG_RT.lmt.", "initialize");
        }

        var database = _parser.ParseDatabase(databasePath);
        if (!database.Success)
        {
            return Fail(PluginErrorCode.InvalidGame,
                $"Could not parse RPG_RT.ldb: {database.Error?.Describe() ?? "unknown parser error"}", "initialize");
        }
        var mapTree = _parser.ParseMapTree(mapTreePath);
        if (!mapTree.Success)
        {
            return Fail(PluginErrorCode.InvalidGame,
                $"Could not parse RPG_RT.lmt: {mapTree.Error?.Describe() ?? "unknown parser error"}", "initialize");
        }

        Godot.Collections.Dictionary? currentMap = null;
        var mapPath = Directory.EnumerateFiles(root, "*.lmu", SearchOption.TopDirectoryOnly)
            .OrderBy(pPath => Path.GetFileName(pPath), StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
        if (mapPath != null)
        {
            var map = _parser.ParseMap(mapPath);
            if (!map.Success)
            {
                return Fail(PluginErrorCode.InvalidGame,
                    $"Could not parse {Path.GetFileName(mapPath)}: {map.Error?.Describe() ?? "unknown parser error"}", "initialize");
            }
            currentMap = map.Data;
        }

        DatabaseData = database.Data;
        MapTreeData = mapTree.Data;
        CurrentMapData = currentMap;
        try
        {
            ConfigureSimulationMap(currentMap, mapTree.Data, mapPath);
        }
        catch (InvalidDataException exception)
        {
            return Fail(PluginErrorCode.InvalidGame, exception.Message, "initialize-map");
        }
        if (currentMap != null)
        {
            var renderResult = _rendererAdapter.CreateFramebuffer(currentMap);
            if (!renderResult.Success || renderResult.Framebuffer == null)
            {
                return Fail(PluginErrorCode.InvalidGame,
                    $"Could not create RM2K map framebuffer: {renderResult.Error}", "initialize-render");
            }
            Framebuffer = renderResult.Framebuffer;
            RenderCurrentMap(
                currentMap,
                (int)currentMap["width"],
                (int)currentMap["height"]);

            var spriteResult = _spriteAdapter.BuildDescriptors(
                currentMap, Simulation.MapX, Simulation.MapY);
            if (!spriteResult.Success)
            {
                return Fail(PluginErrorCode.InvalidGame,
                    $"Could not create RM2K sprite descriptors: {spriteResult.Error}", "initialize-sprites");
            }
            SpriteDescriptors = spriteResult.Descriptors;
        }
        LoadCurrentMapEvents(currentMap);
        State = PluginRuntimeState.Initialized;
        return PluginOperationResult.Succeeded(new[]
        {
            PluginDiagnostic.Info(
                "rm2k.runtime-initialized",
                currentMap == null
                    ? "Loaded RM2K/RM2K3 database and map tree; no LMU map was present."
                    : $"Loaded RM2K/RM2K3 database, map tree, and {Path.GetFileName(mapPath)}.",
                _pluginId),
        });
    }

    public PluginOperationResult Start()
    {
        if (State != PluginRuntimeState.Initialized)
        {
            return Fail(PluginErrorCode.InvalidLifecycleTransition,
                $"RM2K runtime cannot start from state {State}.", "start");
        }
        State = PluginRuntimeState.Running;
        return PluginOperationResult.Succeeded();
    }

    public bool TryMove(int pDeltaX, int pDeltaY)
    {
        if (State != PluginRuntimeState.Running || !Simulation.TryMove(pDeltaX, pDeltaY))
        {
            return false;
        }
        if (CurrentMapData != null)
        {
            var spriteResult = _spriteAdapter.BuildDescriptors(
                CurrentMapData, Simulation.MapX, Simulation.MapY);
            if (spriteResult.Success)
            {
                SpriteDescriptors = spriteResult.Descriptors;
            }
        }
        return true;
    }

    public PluginOperationResult Update(double pDeltaSeconds)
    {
        if (State != PluginRuntimeState.Running)
        {
            return Fail(PluginErrorCode.InvalidLifecycleTransition,
                $"RM2K runtime cannot update from state {State}.", "update");
        }
        if (double.IsNaN(pDeltaSeconds) || double.IsInfinity(pDeltaSeconds) || pDeltaSeconds < 0)
        {
            return Fail(PluginErrorCode.InvalidLifecycleTransition,
                "Delta time must be finite and non-negative.", "update");
        }
        var beforeTicks = _clock.GetSimulationTicks();
        _clock.ProcessFrame(pDeltaSeconds);
        var elapsedTicks = _clock.GetSimulationTicks() - beforeTicks;
        if (elapsedTicks > 0)
        {
            Simulation.FrameCount += elapsedTicks;
            Simulation.AdvanceTimers(elapsedTicks);
            Presentation.Tick(elapsedTicks);
            for (var tick = 0; tick < elapsedTicks; tick++)
            {
                _eventScheduler.ExecuteFrame();
            }
        }
        return PluginOperationResult.Succeeded();
    }

    /// <summary>
    /// Applies one resolved input action to the running map. The host feeds this
    /// from <see cref="Rm2kInputMapper"/>; the ordering inside a turn follows the
    /// verified Player sequence.
    /// </summary>
    public bool SubmitInput(UniversalRPG.Rm2k.Input.Rm2kInputAction pAction)
    {
        if (State != PluginRuntimeState.Running)
        {
            return false;
        }
        return _playerTurn.Apply(pAction);
    }

    public PluginResult<string> ExportSaveSnapshot()
    {
        try
        {
            return PluginResult<string>.Succeeded(Rm2kSimulationSaveCodec.Serialize(Simulation));
        }
        catch (InvalidOperationException exception)
        {
            return PluginResult<string>.Failed(PluginError.Create(PluginErrorCode.LifecycleFailure,
                exception.Message, _pluginId, "save-export", exception));
        }
    }

    public PluginOperationResult ImportSaveSnapshot(string pSnapshot)
    {
        if (!Rm2kSimulationSaveCodec.TryRestore(pSnapshot, Simulation, out var error))
        {
            return Fail(PluginErrorCode.InvalidGame, error, "save-import");
        }
        return PluginOperationResult.Succeeded(new[]
        {
            PluginDiagnostic.Info("rm2k.save-imported", "Bounded RM2K simulation snapshot imported in memory.", _pluginId),
        });
    }

    public PluginOperationResult SetDebugToolsEnabled(bool pEnabled)
    {
        _debugToolsEnabled = pEnabled;
        return PluginOperationResult.Succeeded(new[]
        {
            PluginDiagnostic.Info("rm2k.debug-tools", pEnabled ? "Local RM2K debug tools enabled." : "Local RM2K debug tools disabled.", _pluginId),
        });
    }

    public PluginOperationResult TrySetGold(int pGold)
    {
        if (!_debugToolsEnabled) return DebugToolsDisabled();
        if (pGold < 0 || pGold > MaxGold)
        {
            return Fail(PluginErrorCode.InvalidGame, $"Gold must be between 0 and {MaxGold}.", "debug-gold");
        }
        Simulation.Gold = pGold;
        return PluginOperationResult.Succeeded();
    }

    public PluginOperationResult TrySetSwitch(int pSwitchId, bool pValue)
    {
        if (!_debugToolsEnabled) return DebugToolsDisabled();
        if (pSwitchId < 1 || pSwitchId > GameSimulationState.MaxSwitches)
        {
            return Fail(PluginErrorCode.InvalidGame, $"Switch ID must be between 1 and {GameSimulationState.MaxSwitches}.", "debug-switch");
        }
        while (Simulation.Switches.Count < pSwitchId) Simulation.Switches.Add(false);
        Simulation.Switches[pSwitchId - 1] = pValue;
        return PluginOperationResult.Succeeded();
    }

    private PluginOperationResult DebugToolsDisabled()
    {
        return Fail(PluginErrorCode.InvalidLifecycleTransition,
            "Local debug tools require explicit opt-in.", "debug-tools");
    }

    public PluginOperationResult Stop()
    {
        if (State != PluginRuntimeState.Initialized && State != PluginRuntimeState.Running)
        {
            return Fail(PluginErrorCode.InvalidLifecycleTransition,
                $"RM2K runtime cannot stop from state {State}.", "stop");
        }
        _eventScheduler.Clear();
        _clock.Reset();
        Presentation.Reset();
        Simulation.Reset();
        DatabaseData = null;
        MapTreeData = null;
        CurrentMapData = null;
        Framebuffer = null;
        ClearRendering();
        SpriteDescriptors = Array.Empty<Rm2kSpriteDescriptor>();
        State = PluginRuntimeState.Stopped;
        return PluginOperationResult.Succeeded();
    }

    public void Dispose()
    {
        State = PluginRuntimeState.Disposed;
        DatabaseData = null;
        MapTreeData = null;
        CurrentMapData = null;
        Framebuffer = null;
        ClearRendering();
        SpriteDescriptors = Array.Empty<Rm2kSpriteDescriptor>();
    }

    /// <summary>
    /// Rendered RGBA pixels of the current map, or null when the chipset image is
    /// missing or unreadable. The tile id framebuffer stays available in that
    /// case, so a game without chipset images still runs and still reports.
    /// </summary>
    public Rm2kPixelBuffer? RenderedMap { get; private set; }

    /// <summary>Chipset image of the loaded map, when it was found.</summary>
    public Rm2kChipsetBitmap? ChipsetImage { get; private set; }

    /// <summary>Reason the map could not be rendered, empty when it rendered.</summary>
    public string RenderDiagnostic { get; private set; } = "";

    /// <summary>
    /// Renders the current map into pixels with the verified chipset data. A
    /// missing chipset image is reported and leaves the runtime running, because
    /// the Player treats the chipset as an asset and the simulation does not
    /// depend on it.
    /// </summary>
    private void RenderCurrentMap(Godot.Collections.Dictionary? pMapData, int pWidth, int pHeight)
    {
        RenderedMap = null;
        ChipsetImage = null;
        RenderDiagnostic = "";
        var root = ResolveGameDirectory();
        if (root == null)
        {
            RenderDiagnostic = "RM2K rendering needs a resolved game directory.";
            return;
        }
        if (string.IsNullOrEmpty(_chipsetName))
        {
            RenderDiagnostic = "RM2K rendering needs a chipset name from the database.";
            return;
        }
        // The Player reads the chipset from the ChipSet directory (cache.cpp).
        var chipsetPath = Path.Combine(root, "ChipSet", _chipsetName + ".png");
        if (!File.Exists(chipsetPath))
        {
            RenderDiagnostic = $"RM2K chipset image '{_chipsetName}.png' is missing in ChipSet.";
            return;
        }
        if (!Rm2kChipsetBitmap.TryLoad(chipsetPath, out var bitmap, out var error))
        {
            RenderDiagnostic = $"RM2K chipset image could not be decoded: {error}";
            return;
        }
        if (!bitmap.HasExpectedSize)
        {
            RenderDiagnostic = $"RM2K chipset image is {bitmap.Width}x{bitmap.Height}, expected " +
                $"{Rm2kChipsetBitmap.ExpectedWidth}x{Rm2kChipsetBitmap.ExpectedHeight}.";
            return;
        }
        if (pMapData == null)
        {
            RenderDiagnostic = "RM2K rendering needs the parsed map.";
            return;
        }
        var lowerLayer = TryReadIntArray(pMapData, "lower_layer");
        var upperLayer = TryReadIntArray(pMapData, "upper_layer");
        if (lowerLayer == null || upperLayer == null)
        {
            RenderDiagnostic = "RM2K rendering needs both map layers.";
            return;
        }
        if (lowerLayer.Length != checked(pWidth * pHeight) || upperLayer.Length != checked(pWidth * pHeight))
        {
            RenderDiagnostic = "RM2K map layers do not cover the map tile count.";
            return;
        }

        var layers = new Rm2kMapLayers(pWidth, pHeight, lowerLayer, upperLayer);
        var tables = new Rm2kChipsetTables
        {
            Lower = _chipsetLower,
            Upper = _chipsetUpper,
            Substitution = Simulation.TileSubstitution,
            AnimationType = _chipsetAnimationType,
            AnimationSpeed = _chipsetAnimationSpeed,
        };
        var target = new Rm2kPixelBuffer(
            checked(pWidth * Rm2kChipsetBitmap.TileSize),
            checked(pHeight * Rm2kChipsetBitmap.TileSize));
        var renderer = new Rm2kMapFrameRenderer(bitmap);
        renderer.RenderLower(target, layers, tables, Simulation.FrameCount);
        renderer.RenderUpper(target, layers, tables, Simulation.FrameCount);
        ChipsetImage = bitmap;
        RenderedMap = target;
    }

    private void ClearRendering()
    {
        RenderedMap = null;
        ChipsetImage = null;
        RenderDiagnostic = "";
    }

    private string? ResolveGameDirectory()
    {
        if (string.IsNullOrWhiteSpace(_game.GameDirectory))
        {
            return null;
        }
        try
        {
            var root = Path.GetFullPath(_game.GameDirectory);
            return Directory.Exists(root) ? root : null;
        }
        catch
        {
            return null;
        }
    }

    private static string? FindRootFile(string pRoot, string pName)
    {
        return Directory.EnumerateFiles(pRoot, "*", SearchOption.TopDirectoryOnly)
            .FirstOrDefault(pPath => Path.GetFileName(pPath).Equals(pName, StringComparison.OrdinalIgnoreCase));
    }

    private byte[]? _chipsetLower;
    private byte[]? _chipsetUpper;
    private int[]? _chipsetTerrain;
    private int _chipsetAnimationType = Rm2kChipset.AnimTypeReciprocating;
    private int _chipsetAnimationSpeed = 0;
    private string _chipsetName = "";

    /// <summary>
    /// Reads the verified 162-entry terrain table. A chipset without a table is
    /// normal: RPG_RT drops an all-ones table, and liblcf defaults it to terrain
    /// 1, so an absent table resolves to the default terrain tag.
    /// </summary>
    private static int[]? ReadTerrainData(Godot.Collections.Dictionary pChipset)
    {
        if (!pChipset.TryGetValue("terrain_data", out var raw)
            || raw.VariantType != Godot.Variant.Type.PackedInt32Array)
        {
            return null;
        }
        var values = raw.AsInt32Array();
        if (values.Length != Rm2kChipset.TerrainDataEntries)
        {
            return null;
        }
        var result = new int[values.Length];
        for (var index = 0; index < values.Length; index++)
        {
            result[index] = values[index];
        }
        return result;
    }

    /// <summary>
    /// Reads the verified chipset data (liblcf <c>rpg::Chipset</c>) for the
    /// chipset the map actually uses. Passability tables and the two animation
    /// fields belong to one chipset entry, so the entry is selected by the LMU
    /// <c>chipset_id</c> instead of assuming the first one.
    /// </summary>
    private void ReadChipsetData(Godot.Collections.Dictionary? pDatabase, int pChipsetId)
    {
        _chipsetLower = null;
        _chipsetUpper = null;
        _chipsetTerrain = null;
        _chipsetAnimationType = Rm2kChipset.AnimTypeReciprocating;
        _chipsetAnimationSpeed = 0;
        _chipsetName = "";
        var chipset = FindChipset(pDatabase, pChipsetId);
        if (chipset == null)
        {
            return;
        }
        if (chipset.TryGetValue("chipset_name", out var rawName)
            && rawName.VariantType == Godot.Variant.Type.String)
        {
            _chipsetName = rawName.AsString();
        }
        _chipsetLower = ReadPassabilityArray(chipset, "passable_data_lower", Rm2kChipset.PassabilityLowerEntries);
        _chipsetUpper = ReadPassabilityArray(chipset, "passable_data_upper", Rm2kChipset.PassabilityUpperEntries);
        _chipsetTerrain = ReadTerrainData(chipset);
        if (TryReadInt(chipset, "animation_type", out var animationType))
        {
            _chipsetAnimationType = animationType != 0
                ? Rm2kChipset.AnimTypeCyclic
                : Rm2kChipset.AnimTypeReciprocating;
        }
        TryReadInt(chipset, "animation_speed", out _chipsetAnimationSpeed);
    }

    private static Godot.Collections.Dictionary? FindChipset(
        Godot.Collections.Dictionary? pDatabase, int pChipsetId)
    {
        if (pDatabase == null)
        {
            return null;
        }
        // Typed chipset entries are exposed as a top-level array; the section
        // dictionary keeps the raw chunk and the first chipset's tables.
        if (FindChipsetIn(pDatabase.TryGetValue("chipsets", out var rawChipsets)
                && rawChipsets.VariantType == Godot.Variant.Type.Array
                ? rawChipsets.AsGodotArray()
                : null, pChipsetId) is var direct
            && direct != null)
        {
            return direct;
        }
        if (!pDatabase.TryGetValue("sections", out var rawSections)
            || rawSections.VariantType != Godot.Variant.Type.Dictionary)
        {
            return null;
        }
        var section = rawSections.AsGodotDictionary();
        return section.TryGetValue("chipsets", out var rawSection)
            && rawSection.VariantType == Godot.Variant.Type.Dictionary
            && rawSection.AsGodotDictionary().TryGetValue("entries", out var rawEntries)
            && rawEntries.VariantType == Godot.Variant.Type.Array
            ? FindChipsetIn(rawEntries.AsGodotArray(), pChipsetId)
            : null;
    }

    private static Godot.Collections.Dictionary? FindChipsetIn(
        Godot.Collections.Array? pEntries, int pChipsetId)
    {
        if (pEntries == null)
        {
            return null;
        }
        foreach (var variant in pEntries)
        {
            if (variant.VariantType == Godot.Variant.Type.Dictionary
                && variant.AsGodotDictionary() is var entry
                && TryReadInt(entry, "id", out var entryId)
                && entryId == pChipsetId)
            {
                return entry;
            }
        }
        return null;
    }

    private static int[]? TryReadIntArray(Godot.Collections.Dictionary pData, string pKey)
    {
        if (!pData.TryGetValue(pKey, out var raw) || raw.VariantType != Godot.Variant.Type.PackedInt32Array)
        {
            return null;
        }
        var values = raw.AsInt32Array();
        var result = new int[values.Length];
        for (var index = 0; index < values.Length; index++)
        {
            result[index] = values[index];
        }
        return result;
    }

    private static byte[]? ReadPassabilityArray(
        Godot.Collections.Dictionary pChipset, string pKey, int pExpectedLength)    {
        if (!pChipset.TryGetValue(pKey, out var raw) || raw.VariantType != Godot.Variant.Type.PackedInt32Array)
        {
            return null;
        }
        var values = raw.AsInt32Array();
        if (values.Length != pExpectedLength)
        {
            return null;
        }
        var result = new byte[values.Length];
        for (var index = 0; index < values.Length; index++)
        {
            if (values[index] is < 0 or > 0xff)
            {
                return null;
            }
            result[index] = (byte)values[index];
        }
        return result;
    }

    private void ConfigureSimulationMap(
        Godot.Collections.Dictionary? pMapData,
        Godot.Collections.Dictionary pMapTreeData,
        string? pMapPath)
    {
        if (pMapData == null)
        {
            Simulation.AddDiagnostic("RM2K map simulation is unavailable because no LMU map was loaded.");
            return;
        }
        if (!TryReadInt(pMapData, "width", out var width)
            || !TryReadInt(pMapData, "height", out var height)
            || width <= 0 || height <= 0
            || (long)width * height > Rm2kParser.MaxMapTiles)
        {
            throw new InvalidDataException("Loaded RM2K map dimensions are outside simulation bounds.");
        }

        var mapId = ParseMapId(pMapPath);
        var mapX = 0;
        var mapY = 0;
        var startMapId = 0;
        if (pMapTreeData.TryGetValue("start", out var rawStart)
            && rawStart.VariantType == Godot.Variant.Type.Dictionary)
        {
            var start = rawStart.AsGodotDictionary();
            TryReadInt(start, "party_map_id", out startMapId);
            if (startMapId == mapId)
            {
                TryReadInt(start, "party_x", out mapX);
                TryReadInt(start, "party_y", out mapY);
            }
            else if (startMapId > 0)
            {
                Simulation.AddDiagnostic($"RM2K start map {startMapId} is not the loaded map {mapId}; using bounded map origin.");
            }
        }

        var lowerLayer = TryReadIntArray(pMapData, "lower_layer");
        var upperLayer = TryReadIntArray(pMapData, "upper_layer");
        TryReadInt(pMapData, "chipset_id", out var chipsetId);
        ReadChipsetData(DatabaseData, chipsetId);
        Simulation.ChipsetAnimationType = _chipsetAnimationType;
        Simulation.ChipsetAnimationSpeed = _chipsetAnimationSpeed;
        Simulation.TerrainData = _chipsetTerrain ?? [];
        Simulation.LowerLayer = lowerLayer;
        Simulation.UpperLayer = upperLayer;
        Simulation.UpperPassability = _chipsetUpper;
        if (_chipsetLower != null && _chipsetUpper != null && lowerLayer != null && upperLayer != null)
        {
            // Verified Rm2kChipset rules: the upper layer decides, and only an
            // "above" upper tile falls through to the lower layer.
            var masks = Rm2kChipset.BuildDirectionMasks(lowerLayer, upperLayer, _chipsetLower, _chipsetUpper);
            if (masks.Length == checked(width * height))
            {
                Simulation.ConfigureMap(Math.Clamp(mapId, 0, GameSimulationState.MaxMapId), width, height, masks);
                Simulation.MapX = Math.Clamp(mapX, 0, width - 1);
                Simulation.MapY = Math.Clamp(mapY, 0, height - 1);
                return;
            }
            Simulation.AddDiagnostic("RM2K chipset passability does not cover the map tile count; movement stays fail-closed.");
            return;
        }
        Simulation.ConfigureMap(Math.Clamp(mapId, 0, GameSimulationState.MaxMapId), width, height, new bool[checked(width * height)]);
        Simulation.MapX = Math.Clamp(mapX, 0, width - 1);
        Simulation.MapY = Math.Clamp(mapY, 0, height - 1);
        Simulation.AddDiagnostic("RM2K chipset passability is unavailable; movement remains fail-closed.");    }

    private static int ParseMapId(string? pMapPath)
    {
        if (string.IsNullOrWhiteSpace(pMapPath)) return 0;
        var fileName = Path.GetFileNameWithoutExtension(pMapPath);
        if (fileName.Length != 7
            || !fileName.StartsWith("Map", StringComparison.OrdinalIgnoreCase)
            || !int.TryParse(fileName[3..], out var mapId)
            || mapId < 1
            || mapId > GameSimulationState.MaxMapId)
        {
            return 0;
        }
        return mapId;
    }

    private void LoadCurrentMapEvents(Godot.Collections.Dictionary? pMapData)
    {
        var events = new List<Rm2kMap.Event>();
        if (pMapData != null && pMapData.TryGetValue("events", out var rawEvents)
            && rawEvents.VariantType == Godot.Variant.Type.Array)
        {
            foreach (var rawEvent in rawEvents.AsGodotArray())
            {
                if (rawEvent.VariantType != Godot.Variant.Type.Dictionary) continue;
                var data = rawEvent.AsGodotDictionary();
                if (!TryReadInt(data, "id", out var id) || !TryReadInt(data, "x", out var x) || !TryReadInt(data, "y", out var y)) continue;
                var mapEvent = new Rm2kMap.Event(id, x, y);
                if (data.TryGetValue("pages", out var rawPages) && rawPages.VariantType == Godot.Variant.Type.Array)
                {
                    foreach (var rawPage in rawPages.AsGodotArray())
                    {
                        if (rawPage.VariantType != Godot.Variant.Type.Dictionary) continue;
                        var pageData = rawPage.AsGodotDictionary();
                        if (!TryReadInt(pageData, "trigger", out var trigger)) continue;
                        // Fail closed: a page whose command vector could not be
                        // decoded must not run as if it were empty.
                        if (pageData.TryGetValue("command_error", out var rawCommandError)
                            && rawCommandError.VariantType == Godot.Variant.Type.String
                            && rawCommandError.AsString().Length > 0)
                        {
                            Simulation.AddDiagnostic(
                                $"RM2K event {id} page with trigger {trigger} skipped: {rawCommandError.AsString()}");
                            continue;
                        }
                        var page = new Rm2kMap.EventPage { Trigger = trigger };
                        TryReadInt(pageData, "layer", out var layer);
                        page.Layer = layer;
                        if (pageData.TryGetValue("conditions", out var rawConditions) && rawConditions.VariantType == Godot.Variant.Type.Dictionary)
                        {
                            foreach (var pair in rawConditions.AsGodotDictionary())
                            {
                                var key = pair.Key.ToString();
                                if (pair.Value.VariantType == Godot.Variant.Type.Bool)
                                {
                                    page.Conditions[key] = pair.Value.AsBool();
                                }
                                else if (pair.Value.VariantType == Godot.Variant.Type.Int)
                                {
                                    page.Conditions[key] = pair.Value.AsInt32();
                                }
                            }
                        }
                        if (pageData.TryGetValue("commands", out var rawCommands) && rawCommands.VariantType == Godot.Variant.Type.Array)
                        {
                            foreach (var rawCommand in rawCommands.AsGodotArray())
                            {
                                if (rawCommand.VariantType != Godot.Variant.Type.Dictionary) continue;
                                var command = rawCommand.AsGodotDictionary();
                                if (!TryReadInt(command, "code", out var code)) continue;
                                var text = command.TryGetValue("text", out var rawText) ? rawText.ToString() : "";
                                var parameters = new List<int>();
                                if (command.TryGetValue("parameters", out var rawParameters) && rawParameters.VariantType == Godot.Variant.Type.PackedInt32Array)
                                {
                                    foreach (var parameter in rawParameters.AsInt32Array()) parameters.Add(parameter);
                                }
                                page.Commands.Add(new Rm2kMap.EventCommand(code, parameters, text));
                            }
                        }
                        mapEvent.Pages.Add(page);
                    }
                }
                events.Add(mapEvent);
            }
        }
        _eventScheduler.SetEvents(events);
        _mapEvents = events;
    }

    private List<Rm2kMap.Event> _mapEvents = new();

    /// <summary>
    /// Resolves the command list for a nested CallEvent. RM2K page indices are
    /// one-based; index 0 addresses the first page of the event.
    /// </summary>
    private IReadOnlyList<Rm2kMap.EventCommand>? ResolveEventCommands(int pEventId, int pPageIndex)
    {
        foreach (var mapEvent in _mapEvents)
        {
            if (mapEvent.Id != pEventId || mapEvent.Pages.Count == 0)
            {
                continue;
            }
            var pageNumber = pPageIndex <= 0 ? 0 : pPageIndex - 1;
            if (pageNumber >= mapEvent.Pages.Count)
            {
                return null;
            }
            return mapEvent.Pages[pageNumber].Commands;
        }
        return null;
    }

    private static bool TryReadInt(Godot.Collections.Dictionary pData, string pKey, out int pValue)
    {
        if (!pData.TryGetValue(pKey, out var rawValue))
        {
            pValue = 0;
            return false;
        }
        try
        {
            pValue = (int)rawValue;
            return true;
        }
        catch (InvalidCastException)
        {
            pValue = 0;
            return false;
        }
    }

    private PluginOperationResult Fail(PluginErrorCode pCode, string pMessage, string pPhase)
    {
        return PluginOperationResult.Failed(PluginError.Create(pCode, pMessage, _pluginId, pPhase));
    }
}
