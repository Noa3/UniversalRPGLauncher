using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using UniversalRPG.Plugins;

namespace UniversalRPG.Wolf;

/// <summary>Loads bounded WOLF maps and common-event programs as data only.</summary>
public sealed class WolfMapReader
{
    private readonly WolfParseLimits _limits;

    public WolfMapReader(WolfParseLimits pLimits)
    {
        _limits = pLimits ?? throw new ArgumentNullException(nameof(pLimits));
    }

    public PluginResult<WolfMapData> Read(string pPath)
    {
        var document = new WolfDataReader(_limits).ReadDocument(pPath, "map");
        if (!document.Success)
        {
            return PluginResult<WolfMapData>.Failed(document.Error!, document.Diagnostics);
        }
        var root = document.Value;
        if (WolfDataReader.IsProtected(root))
        {
            return PluginResult<WolfMapData>.Failed(PluginError.Create(
                PluginErrorCode.UnsupportedEngine,
                "Protected or encrypted WOLF map data is not decrypted by this runtime.",
                EnginePluginIds.WolfRpg,
                "wolf-map"), new[]
            {
                PluginDiagnostic.Warning("wolf.protected-data", "Protected WOLF map data was rejected.", EnginePluginIds.WolfRpg),
            });
        }

        var mapId = WolfDataReader.ReadOptionalInt(root, "id", ParseIdFromFileName(pPath));
        var width = WolfDataReader.ReadOptionalInt(root, "width", 0);
        var height = WolfDataReader.ReadOptionalInt(root, "height", 0);
        if (mapId < 0 || width <= 0 || height <= 0
            || width > _limits.MaxMapDimension || height > _limits.MaxMapDimension)
        {
            return WolfDataReader.Failed<WolfMapData>(
                $"WOLF map '{Path.GetFileName(pPath)}' has invalid bounded dimensions or ID.", "wolf-map");
        }
        var expectedTiles = checked(width * height);
        if (expectedTiles > _limits.MaxMapTiles)
        {
            return WolfDataReader.Failed<WolfMapData>(
                $"WOLF map '{Path.GetFileName(pPath)}' exceeds the tile limit.", "wolf-map");
        }

        var tiles = new List<int>(expectedTiles);
        foreach (var tile in WolfDataReader.ReadArray(root, "tiles"))
        {
            if (!tile.TryGetInt32(out var value))
            {
                return WolfDataReader.Failed<WolfMapData>(
                    $"WOLF map '{Path.GetFileName(pPath)}' contains a non-integer tile.", "wolf-map");
            }
            tiles.Add(value);
            if (tiles.Count > expectedTiles)
            {
                return WolfDataReader.Failed<WolfMapData>(
                    $"WOLF map '{Path.GetFileName(pPath)}' contains too many tiles.", "wolf-map");
            }
        }
        if (tiles.Count != expectedTiles)
        {
            return WolfDataReader.Failed<WolfMapData>(
                $"WOLF map '{Path.GetFileName(pPath)}' contains {tiles.Count} tiles; expected {expectedTiles}.", "wolf-map");
        }

        var events = ParseEvents(root, pPath, _limits);
        if (!events.Success || events.Value == null)
        {
            return PluginResult<WolfMapData>.Failed(events.Error!, document.Diagnostics.Concat(events.Diagnostics));
        }
        return PluginResult<WolfMapData>.Succeeded(new WolfMapData
        {
            Id = mapId,
            Name = WolfDataReader.ReadOptionalString(root, "name", _limits.MaxStringBytes),
            Width = width,
            Height = height,
            Tiles = tiles,
            Events = events.Value,
        }, document.Diagnostics.Concat(events.Diagnostics));
    }

    internal static PluginResult<IReadOnlyList<WolfEventProgram>> ReadCommonEvents(
        string pPath,
        WolfParseLimits pLimits)
    {
        var document = new WolfDataReader(pLimits).ReadDocument(pPath, "common-events");
        if (!document.Success)
        {
            return PluginResult<IReadOnlyList<WolfEventProgram>>.Failed(document.Error!, document.Diagnostics);
        }
        if (WolfDataReader.IsProtected(document.Value))
        {
            return PluginResult<IReadOnlyList<WolfEventProgram>>.Failed(PluginError.Create(
                PluginErrorCode.UnsupportedEngine,
                "Protected or encrypted WOLF common-event data is not decrypted by this runtime.",
                EnginePluginIds.WolfRpg,
                "wolf-events"), document.Diagnostics);
        }
        var events = ParseEvents(document.Value, pPath, pLimits);
        if (!events.Success || events.Value == null)
        {
            return PluginResult<IReadOnlyList<WolfEventProgram>>.Failed(events.Error!, document.Diagnostics.Concat(events.Diagnostics));
        }
        return PluginResult<IReadOnlyList<WolfEventProgram>>.Succeeded(events.Value, document.Diagnostics.Concat(events.Diagnostics));
    }

    private static PluginResult<IReadOnlyList<WolfEventProgram>> ParseEvents(
        JsonElement pRoot,
        string pPath,
        WolfParseLimits pLimits)
    {
        var eventElements = WolfDataReader.ReadArray(pRoot, "events").ToArray();
        if (eventElements.Length > pLimits.MaxEventsPerMap)
        {
            return WolfDataReader.Failed<IReadOnlyList<WolfEventProgram>>(
                $"WOLF event file '{Path.GetFileName(pPath)}' exceeds the event limit.", "wolf-events");
        }

        var events = new List<WolfEventProgram>(eventElements.Length);
        for (var index = 0; index < eventElements.Length; index += 1)
        {
            var element = eventElements[index];
            if (element.ValueKind != JsonValueKind.Object)
            {
                return WolfDataReader.Failed<IReadOnlyList<WolfEventProgram>>(
                    $"WOLF event {index} in '{Path.GetFileName(pPath)}' is not an object.", "wolf-events");
            }
            var commands = ParseCommands(element, pPath, index, pLimits);
            if (!commands.Success || commands.Value == null)
            {
                return PluginResult<IReadOnlyList<WolfEventProgram>>.Failed(commands.Error!, commands.Diagnostics);
            }
            events.Add(new WolfEventProgram
            {
                Id = WolfDataReader.ReadOptionalInt(element, "id", index + 1),
                X = WolfDataReader.ReadOptionalInt(element, "x", 0),
                Y = WolfDataReader.ReadOptionalInt(element, "y", 0),
                Commands = commands.Value,
            });
        }
        return PluginResult<IReadOnlyList<WolfEventProgram>>.Succeeded(events);
    }

    private static PluginResult<IReadOnlyList<WolfEventCommand>> ParseCommands(
        JsonElement pEvent,
        string pPath,
        int pEventIndex,
        WolfParseLimits pLimits)
    {
        var commandElements = WolfDataReader.ReadArray(pEvent, "commands").ToArray();
        if (commandElements.Length > pLimits.MaxCommandsPerEvent)
        {
            return WolfDataReader.Failed<IReadOnlyList<WolfEventCommand>>(
                $"WOLF event {pEventIndex} in '{Path.GetFileName(pPath)}' exceeds the command limit.", "wolf-events");
        }

        var commands = new List<WolfEventCommand>(commandElements.Length);
        foreach (var element in commandElements)
        {
            if (element.ValueKind != JsonValueKind.Object)
            {
                return WolfDataReader.Failed<IReadOnlyList<WolfEventCommand>>(
                    $"WOLF event {pEventIndex} in '{Path.GetFileName(pPath)}' contains a non-object command.", "wolf-events");
            }
            var operation = WolfDataReader.ReadOptionalString(element, "op", 64);
            var opcode = ParseOpcode(operation);
            var text = WolfDataReader.ReadOptionalString(element, "text", pLimits.MaxStringBytes);
            var choices = ParseChoices(element, pLimits);
            if (choices == null)
            {
                return WolfDataReader.Failed<IReadOnlyList<WolfEventCommand>>(
                    $"WOLF event {pEventIndex} in '{Path.GetFileName(pPath)}' contains invalid choices.", "wolf-events");
            }
            commands.Add(new WolfEventCommand
            {
                Opcode = opcode,
                Text = text,
                Operand = WolfDataReader.ReadOptionalInt(element, "operand", 0),
                Value = WolfDataReader.ReadOptionalInt(element, "value", 0),
                Frames = Math.Clamp(WolfDataReader.ReadOptionalInt(element, "frames", 0), 0, 1_000_000),
                MapId = WolfDataReader.ReadOptionalInt(element, "mapId", WolfDataReader.ReadOptionalInt(element, "map_id", 0)),
                X = WolfDataReader.ReadOptionalInt(element, "x", 0),
                Y = WolfDataReader.ReadOptionalInt(element, "y", 0),
                JumpIndex = WolfDataReader.ReadOptionalInt(element, "jump", -1),
                Choices = choices,
                RawOperation = operation,
            });
        }
        return PluginResult<IReadOnlyList<WolfEventCommand>>.Succeeded(commands);
    }

    private static IReadOnlyList<string>? ParseChoices(JsonElement pElement, WolfParseLimits pLimits)
    {
        if (!pElement.TryGetProperty("choices", out var value))
        {
            return Array.Empty<string>();
        }
        if (value.ValueKind != JsonValueKind.Array)
        {
            return null;
        }
        var choices = new List<string>();
        foreach (var choice in value.EnumerateArray())
        {
            if (choice.ValueKind != JsonValueKind.String)
            {
                return null;
            }
            var text = choice.GetString() ?? "";
            if (System.Text.Encoding.UTF8.GetByteCount(text) > pLimits.MaxStringBytes)
            {
                return null;
            }
            choices.Add(text);
        }
        return choices;
    }

    private static WolfEventOpcode ParseOpcode(string pOperation)
    {
        return pOperation.ToLowerInvariant() switch
        {
            "message" => WolfEventOpcode.Message,
            "set_variable" => WolfEventOpcode.SetVariable,
            "add_variable" => WolfEventOpcode.AddVariable,
            "set_switch" => WolfEventOpcode.SetSwitch,
            "if_switch" => WolfEventOpcode.IfSwitch,
            "if_variable" => WolfEventOpcode.IfVariable,
            "wait" => WolfEventOpcode.Wait,
            "choice" => WolfEventOpcode.Choice,
            "transfer" => WolfEventOpcode.Transfer,
            "end" => WolfEventOpcode.End,
            _ => WolfEventOpcode.Unknown,
        };
    }

    private static int ParseIdFromFileName(string pPath)
    {
        var digits = new string(Path.GetFileNameWithoutExtension(pPath).Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out var result) ? result : -1;
    }
}
