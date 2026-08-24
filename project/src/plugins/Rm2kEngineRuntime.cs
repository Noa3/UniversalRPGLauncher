using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniversalRPG.Core;
using UniversalRPG.Rm2k;
using UniversalRPG.Rm2k.Interpreter;
using UniversalRPG.Rm2k.Parser;
using UniversalRPG.Rm2k.Presentation;
using UniversalRPG.Rm2k.Simulation;

namespace UniversalRPG.Plugins;

/// <summary>
/// Minimal native RM2K/RM2K3 runtime backend. It loads the validated LDB/LMT
/// and first LMU through the existing bounded parser, then advances a
/// deterministic 60 Hz simulation clock. Event interpretation and presentation
/// remain separate follow-up work, but launching no longer requires the
/// original RPG_RT executable.
/// </summary>
public sealed class Rm2kEngineRuntime : IEngineRuntime, IRuntimeSaveTools, IRuntimeDebugTools
{
    private const int MaxGold = 999999;
    private readonly string _pluginId;
    private readonly PluginGameInfo _game;
    private readonly Rm2kParser _parser = new();
    private readonly VirtualClock _clock = new();
    private readonly Rm2kEventScheduler _eventScheduler;
    private bool _debugToolsEnabled;

    public Rm2kEngineRuntime(string pPluginId, PluginGameInfo pGame)
    {
        _pluginId = pPluginId;
        _game = pGame;
        _eventScheduler = new Rm2kEventScheduler(Simulation, Presentation);
    }

    public PluginRuntimeState State { get; private set; } = PluginRuntimeState.Created;
    public Godot.Collections.Dictionary? DatabaseData { get; private set; }
    public Godot.Collections.Dictionary? MapTreeData { get; private set; }
    public Godot.Collections.Dictionary? CurrentMapData { get; private set; }
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
            for (var tick = 0; tick < elapsedTicks; tick++)
            {
                _eventScheduler.ExecuteFrame();
            }
        }
        return PluginOperationResult.Succeeded();
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
                        var page = new Rm2kMap.EventPage { Trigger = trigger };
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
