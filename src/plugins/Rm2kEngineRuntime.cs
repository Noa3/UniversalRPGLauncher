using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniversalRPG.Core;
using UniversalRPG.Rm2k.Parser;

namespace UniversalRPG.Plugins;

/// <summary>
/// Minimal native RM2K/RM2K3 runtime backend. It loads the validated LDB/LMT
/// and first LMU through the existing bounded parser, then advances a
/// deterministic 60 Hz simulation clock. Event interpretation and presentation
/// remain separate follow-up work, but launching no longer requires the
/// original RPG_RT executable.
/// </summary>
public sealed class Rm2kEngineRuntime : IEngineRuntime
{
    private readonly string _pluginId;
    private readonly PluginGameInfo _game;
    private readonly Rm2kParser _parser = new();
    private readonly VirtualClock _clock = new();

    public Rm2kEngineRuntime(string pPluginId, PluginGameInfo pGame)
    {
        _pluginId = pPluginId;
        _game = pGame;
    }

    public PluginRuntimeState State { get; private set; } = PluginRuntimeState.Created;
    public Godot.Collections.Dictionary? DatabaseData { get; private set; }
    public Godot.Collections.Dictionary? MapTreeData { get; private set; }
    public Godot.Collections.Dictionary? CurrentMapData { get; private set; }
    public int SimulationTicks => _clock.GetSimulationTicks();

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
        _clock.ProcessFrame(pDeltaSeconds);
        return PluginOperationResult.Succeeded();
    }

    public PluginOperationResult Stop()
    {
        if (State != PluginRuntimeState.Initialized && State != PluginRuntimeState.Running)
        {
            return Fail(PluginErrorCode.InvalidLifecycleTransition,
                $"RM2K runtime cannot stop from state {State}.", "stop");
        }
        State = PluginRuntimeState.Stopped;
        return PluginOperationResult.Succeeded();
    }

    public void Dispose()
    {
        State = PluginRuntimeState.Disposed;
        DatabaseData = null;
        MapTreeData = null;
        CurrentMapData = null;
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

    private PluginOperationResult Fail(PluginErrorCode pCode, string pMessage, string pPhase)
    {
        return PluginOperationResult.Failed(PluginError.Create(pCode, pMessage, _pluginId, pPhase));
    }
}
