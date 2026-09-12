using System;
using System.Collections.Generic;
using Godot;

namespace UniversalRPG.Rm2k.Parser;

public partial class Rm2kParser
{
    // LMU ChunkEventPage IDs and absent-field defaults follow liblcf's
    // eventpage.h/chunks.h. Unknown patch fields stay data, never guessed aliases.
    private static readonly (int Id, string Name, int Default)[] LmuPageScalars =
    {
        (0x16, "character_index", 0), (0x17, "character_direction", 2),
        (0x18, "character_pattern", 1), (0x19, "translucent", 0),
        (0x1F, "move_type", 1), (0x20, "move_frequency", 3),
        (0x21, "trigger", 0), (0x22, "priority", 0),
        (0x23, "overlap_forbidden", 0), (0x24, "animation_type", 0),
        (0x25, "move_speed", 3),
    };

    private ParseResult DecodeLmuEventPages(byte[] data)
    {
        // false here counts structures but discards both objects and fields.
        // The runtime needs actual pages, not merely the declared page count.
        var array = ParseStructArray(data, true);
        if (!array.Success) return array;
        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        var ids = new HashSet<int>();
        foreach (var rawPage in (Godot.Collections.Array<Godot.Collections.Dictionary>)array.Data["objects"])
        {
            var id = (int)rawPage["id"];
            if (id <= 0 || !ids.Add(id)) return Failure($"Invalid or duplicate LMU page ID {id}");
            var rawFields = (Godot.Collections.Array<Godot.Collections.Dictionary>)rawPage["fields"];
            var unique = LmuUniqueFields(rawFields, $"page {id}");
            if (!unique.Success) return unique;
            var fields = unique.Data;
            var page = new Godot.Collections.Dictionary { { "id", id } };
            var known = new HashSet<int> { 0x02, 0x15, 0x29, 0x33, 0x34 };
            foreach (var scalar in LmuPageScalars)
            {
                known.Add(scalar.Id);
                var value = IntegerFromFields(fields, scalar.Id, scalar.Default);
                if (!value.Success) return LmuPageError(id, scalar.Name, value, 0);
                page[scalar.Name] = value.Data["value"];
            }

            var name = GetField(fields, 0x15);
            if (name.Count != 0 && ((byte[])name["data"]).Length > MaxLdbStringBytes)
                return Failure($"Page {id} character name exceeds the string limit", (int)name["payload_offset"]);
            page["character_name"] = DecodeTextField(fields, 0x15);

            var conditions = new Godot.Collections.Dictionary();
            var conditionChunk = GetField(fields, 0x02);
            var conditionOffset = 0;
            if (conditionChunk.Count != 0)
            {
                conditionOffset = (int)conditionChunk["payload_offset"];
                var bytes = (byte[])conditionChunk["data"];
                if (bytes.Length != 0)
                {
                    // A chunk holds bytes, not an already decoded `fields` member.
                    using var reader = new LcfBinaryReader(bytes);
                    var nested = ReadStructFields(reader, true);
                    if (!nested.Success) return LmuPageError(id, "condition", nested, conditionOffset);
                    if (!reader.IsEof())
                        return Failure($"Page {id} condition has trailing bytes", conditionOffset + reader.GetPosition());
                    var conditionFields = LmuUniqueFields(
                        (Godot.Collections.Array<Godot.Collections.Dictionary>)nested.Data["fields"], $"page {id} condition");
                    if (!conditionFields.Success) return LmuPageError(id, "condition", conditionFields, conditionOffset);
                    conditions = conditionFields.Data;
                }
            }
            var decodedCondition = Rm2kEventPageConditionDecoder.Decode(conditions);
            if (!decodedCondition.Success) return LmuPageError(id, "condition", decodedCondition, conditionOffset);
            page["conditions"] = decodedCondition.Data;

            var commandChunk = GetField(fields, 0x34);
            page["has_command_list"] = commandChunk.Count != 0;
            var commands = new Godot.Collections.Array<Godot.Collections.Dictionary>();
            if (commandChunk.Count != 0)
            {
                var decoded = Rm2kEventCommandDecoder.Decode((byte[])commandChunk["data"]);
                if (!decoded.Success) return LmuPageError(id, "commands", decoded, (int)commandChunk["payload_offset"]);
                commands = (Godot.Collections.Array<Godot.Collections.Dictionary>)decoded.Data["commands"];
            }
            page["commands"] = commands;

            // The size hint is a serialized byte size, not an instruction count.
            // liblcf reads it as advisory. Never allocate or iterate from this hint.
            if (fields.ContainsKey(0x33))
            {
                var sizeHint = IntegerFromFields(fields, 0x33, 0);
                if (!sizeHint.Success) return LmuPageError(id, "command byte-size hint", sizeHint, 0);
                page["declared_command_bytes"] = sizeHint.Data["value"];
            }

            var routeChunk = GetField(fields, 0x29);
            page["has_move_list"] = routeChunk.Count != 0;
            var routeBytes = routeChunk.Count == 0 ? Array.Empty<byte>() : (byte[])routeChunk["data"];
            var route = Rm2kMoveRouteDecoder.Decode(routeBytes);
            if (!route.Success)
                return Failure($"Page {id} movement route: {route.Error}",
                    routeChunk.Count == 0 ? -1 : (int)routeChunk["payload_offset"]);
            var routeCommands = new Godot.Collections.Array<Godot.Collections.Dictionary>();
            foreach (var command in route.Route!.Commands)
            {
                routeCommands.Add(new Godot.Collections.Dictionary
                {
                    { "code", (int)command.Code }, { "text", command.ParameterString },
                    { "parameter_a", command.ParameterA }, { "parameter_b", command.ParameterB },
                    { "parameter_c", command.ParameterC },
                });
            }
            page["move_route"] = new Godot.Collections.Dictionary
            {
                { "commands", routeCommands }, { "repeat", route.Route.Repeat },
                { "skippable", route.Route.Skippable },
            };
            var unknown = new Godot.Collections.Array<Godot.Collections.Dictionary>();
            foreach (var field in rawFields)
                if (!known.Contains((int)field["id"])) unknown.Add(field);
            page["unknown_fields"] = unknown;
            result.Add(page);
        }
        return new ParseResult(true, null, new Godot.Collections.Dictionary { { "pages", result } });
    }

    private static ParseResult LmuUniqueFields(
        Godot.Collections.Array<Godot.Collections.Dictionary> rawFields, string label)
    {
        var result = new Godot.Collections.Dictionary();
        foreach (var field in rawFields)
        {
            var id = (int)field["id"];
            if (result.ContainsKey(id))
                return Failure($"Duplicate field 0x{id:X} in {label}", (int)field["offset"]);
            result[id] = field;
        }
        return new ParseResult(true, null, result);
    }

    private static ParseResult LmuPageError(int id, string field, ParseResult result, int baseOffset)
        => Failure($"Page {id} {field}: {result.Error?.Message ?? "invalid data"}",
            baseOffset + Math.Max(result.Error?.Offset ?? 0, 0));
}
