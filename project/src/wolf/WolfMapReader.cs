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
                Operator = Math.Clamp(
                    WolfDataReader.ReadOptionalInt(element, "operator", 0),
                    0,
                    WolfVariableOperator.MaxOperator),
                // **Read by name, and not "is it non-zero".** A file that
                // writes `"right2": 0` is a file that gives a second number of
                // zero, and a file that does not mention it gives no second
                // number at all. The arc tangent reads two vectors, and the
                // difference is a slope of zero against a slope that uses both.
                Right2 = WolfDataReader.ReadOptionalInt(element, "right2", 0),
                HasRight2 = element.TryGetProperty("right2", out _),
                Frames = Math.Clamp(WolfDataReader.ReadOptionalInt(element, "frames", 0), 0, 1_000_000),
                MapId = WolfDataReader.ReadOptionalInt(element, "mapId", WolfDataReader.ReadOptionalInt(element, "map_id", 0)),
                X = WolfDataReader.ReadOptionalInt(element, "x", 0),
                Y = WolfDataReader.ReadOptionalInt(element, "y", 0),
                // **The two route fields, and they are the difference between a
                // patrol that walks and a patrol that does not.** A move route
                // command names the figure it drives and carries the steps; a
                // reader that filled neither would reach the VM with an
                // instruction to start a route with no figure and no steps,
                // and the route would finish at once and report itself done.
                CharacterId = WolfDataReader.ReadOptionalInt(
                    element, "character", WolfDataReader.ReadOptionalInt(element, "characterId", 0)),
                Route = ParseRoute(element, pLimits),
                JumpIndex = WolfDataReader.ReadOptionalInt(element, "jump", -1),
                Choices = choices,
                RawOperation = operation,
            });
        }
        return PluginResult<IReadOnlyList<WolfEventCommand>>.Succeeded(commands);
    }

    /// <summary>
    /// A command's move route, or an empty one when it carries none.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>The steps are text and not numbers.</strong> A route step's type is
    /// the editor's own numbering, and a file that writes it as a name says
    /// what it means — so this maps the names the verified route table already
    /// has and leaves an unknown name as an unmapped step, which the runner
    /// refuses rather than guesses.
    /// </para>
    /// <para>
    /// <strong>An empty route is not a route that finished.</strong> A command
    /// with no steps gets an empty list and no mode of its own, and the runner
    /// treats it as a route with nothing to do — which is what a file that
    /// wrote none asked for.
    /// </para>
    /// </remarks>
    private static WolfMoveRoute? ParseRoute(
        JsonElement pElement,
        WolfParseLimits pLimits)
    {
        if (!pElement.TryGetProperty("route", out var value))
        {
            return null;
        }
        if (value.ValueKind != JsonValueKind.Object)
        {
            return null;
        }
        var stepElements = WolfDataReader.ReadArray(value, "steps").ToArray();
        if (stepElements.Length > pLimits.MaxDatabaseFieldsPerRecord)
        {
            return null;
        }
        var steps = new List<WolfMoveRouteStep>(stepElements.Length);
        foreach (var element in stepElements)
        {
            if (element.ValueKind != JsonValueKind.Object)
            {
                return null;
            }
            var type = ParseRouteType(
                WolfDataReader.ReadOptionalString(element, "type", 32));
            var numbers = new List<int>();
            foreach (var number in WolfDataReader.ReadArray(element, "args"))
            {
                if (number.ValueKind != JsonValueKind.Number)
                {
                    return null;
                }
                numbers.Add(number.GetInt32());
            }
            var bytes = new List<byte>();
            foreach (var single in WolfDataReader.ReadArray(element, "bytes"))
            {
                if (single.ValueKind != JsonValueKind.Number)
                {
                    return null;
                }
                bytes.Add((byte)Math.Clamp(single.GetInt32(), 0, 255));
            }
            steps.Add(new WolfMoveRouteStep
            {
                // **A step type above the table is not stored as itself.** The
                // field is one byte and the verified types stop at 0x3A, so a
                // name this reader does not know becomes 0xFF — which no real
                // type is, and which the runner refuses.
                Type = (byte)(type > 0xFF ? 0xFF : type),
                Arguments = numbers,
                ByteArguments = bytes,
            });
        }
        return new WolfMoveRoute
        {
            Steps = steps,
            Mode = ParseRouteMode(WolfDataReader.ReadOptionalString(value, "mode", 16)),
            // **The three flags are read as numbers, because that is the only
            // shape the reader has and a new file format is not being invented
            // here.** A file writes 1 for true and 0 for false, which is what a
            // JSON number reads back as; anything that is not a number is not a
            // flag and falls to false rather than throwing, so one bad line
            // does not lose the whole route.
            WaitUntilDone = Flag(value, "wait"),
            SkipImpossibleMoves = Flag(value, "skip"),
            RepeatActions = Flag(value, "repeat"),
        };
    }

    /// <summary>
    /// A route step's name, as the verified route table numbers it.
    /// </summary>
    /// <remarks>
    /// <strong>The names are the table's and not mine.</strong> The step types are
    /// numbered values, not an enumeration, and the names here are the ones the
    /// table already uses — a facing step is FacingUp and not TurnUp, and a
    /// script's variable step is AssignToVariable and not SetVariable. A reader
    /// that renamed them would answer a file's "facing_up" with an unknown step
    /// and the runner would refuse it.
    /// </remarks>
    /// <summary>
    /// A flag, read as the number the file writes for it.
    /// </summary>
    /// <remarks>
    /// <strong>True is 1 and not "any number".</strong> A file that wrote 2 into
    /// a flag is a file that is wrong, and a reader that treated it as true
    /// would take a route that does not wait and make it wait — the kind of
    /// error a player sees as a guard who stopped on a staircase.
    /// </remarks>
    private static bool Flag(JsonElement pRoot, string pName)
    {
        return WolfDataReader.ReadOptionalInt(pRoot, pName, 0) == 1;
    }

    private static int ParseRouteType(string pName)
    {
        return pName.ToLowerInvariant() switch
        {
            "move_down" => WolfMoveRouteType.MoveDown,
            "move_left" => WolfMoveRouteType.MoveLeft,
            "move_right" => WolfMoveRouteType.MoveRight,
            "move_up" => WolfMoveRouteType.MoveUp,
            "move_down_left" => WolfMoveRouteType.MoveDownLeft,
            "move_down_right" => WolfMoveRouteType.MoveDownRight,
            "move_up_left" => WolfMoveRouteType.MoveUpLeft,
            "move_up_right" => WolfMoveRouteType.MoveUpRight,
            "facing_down" => WolfMoveRouteType.FacingDown,
            "facing_left" => WolfMoveRouteType.FacingLeft,
            "facing_right" => WolfMoveRouteType.FacingRight,
            "facing_up" => WolfMoveRouteType.FacingUp,
            "jump" => WolfMoveRouteType.Jump,
            "assign_to_variable" => WolfMoveRouteType.AssignToVariable,
            "set_move_speed" => WolfMoveRouteType.SetMoveSpeed,
            "set_move_frequency" => WolfMoveRouteType.SetMoveFrequency,
            "set_animation_frequency" => WolfMoveRouteType.SetAnimationFrequency,
            "set_graphic" => WolfMoveRouteType.SetGraphic,
            "set_opacity" => WolfMoveRouteType.SetOpacity,
            "set_sound" => WolfMoveRouteType.SetSound,
            "wait_x_frames" => WolfMoveRouteType.WaitXFrames,
            "move_approach_event" => WolfMoveRouteType.MoveApproachEvent,
            "move_approach_position" => WolfMoveRouteType.MoveApproachPosition,
            "add_to_variable" => WolfMoveRouteType.AddToVariable,
            "set_height" => WolfMoveRouteType.SetHeight,
            // **An unmapped name is the unknown type and not the first one.**
            // A reader that returned 0 would read it as a step down — so a file
            // with a typo in a step name would send its figure one tile south
            // and the game would look right until the day it did not.
            _ => UnmappedRouteType,
        };
    }

    /// <summary>
    /// A route step whose name this reader does not know.
    /// </summary>
    /// <remarks>
    /// <strong>256 and not zero.</strong> The verified step types run from 0x00
    /// to 0x3A, so a value above them cannot collide with a real one, and the
    /// runner refuses it as the unknown type it is.
    /// </remarks>
    private const int UnmappedRouteType = 0x100;

    /// <summary>How a route is driven, as the mode table numbers it.</summary>
    private static int ParseRouteMode(string pName)
    {
        return pName.ToLowerInvariant() switch
        {
            "dont_move" => WolfMoveRouteMode.DontMove,
            "random" => WolfMoveRouteMode.Random,
            "toward_hero" => WolfMoveRouteMode.TowardHero,
            // **Custom and not zero for an unmapped name.** Zero is "do not
            // move", so a reader that defaulted there would take a patrol that
            // wrote its mode as something else and stand it still.
            _ => WolfMoveRouteMode.Custom,
        };
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
            // **The two route opcodes were missing here, and the VM ran them
            // anyway.** A map file that said "move_route" arrived as Unknown,
            // which the VM refuses — so a patrol written in the editor stood
            // still, and nothing said why. The three names below close that:
            // the opcode, the character the route belongs to and the route.
            "move_route" => WolfEventOpcode.MoveRoute,
            "move_route_wait" => WolfEventOpcode.WaitUntilRouteDone,
            // **A common event is called by its database id**, and the editor
            // writes the id in the same operand field a variable command uses —
            // so the operand is the id and not a variable number.
            "call_common" => WolfEventOpcode.CallCommonEvent,
            "call_event" => WolfEventOpcode.CallCommonEvent,
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
