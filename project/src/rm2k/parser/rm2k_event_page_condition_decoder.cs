using System;

namespace UniversalRPG.Rm2k.Parser;

/// <summary>Decodes the official LMU EventPageCondition chunk fields.</summary>
public static class Rm2kEventPageConditionDecoder
{
    public static Rm2kParser.ParseResult Decode(Godot.Collections.Dictionary pFields)
    {
        var flags = ReadFlags(pFields);
        var result = new Godot.Collections.Dictionary
        {
            { "switch_a_enabled", (flags & 0x01) != 0 },
            { "switch_b_enabled", (flags & 0x02) != 0 },
            { "variable_enabled", (flags & 0x04) != 0 },
            { "item_enabled", (flags & 0x08) != 0 },
            { "actor_enabled", (flags & 0x10) != 0 },
            { "timer_enabled", (flags & 0x20) != 0 },
            { "timer2_enabled", (flags & 0x40) != 0 },
        };

        var fields = new (int Id, string Name, int Default)[]
        {
            (0x02, "switch_a_id", 1), (0x03, "switch_b_id", 1),
            (0x04, "variable_id", 1), (0x05, "variable_value", 0),
            (0x06, "item_id", 1), (0x07, "actor_id", 1),
            (0x08, "timer_sec", 0), (0x09, "timer2_sec", 0),
            (0x0a, "compare_operator", 1),
        };
        foreach (var field in fields)
        {
            var read = ReadInteger(pFields, field.Id, field.Default);
            if (!read.Success) return read;
            result[field.Name] = read.Data["value"];
        }
        return new Rm2kParser.ParseResult(true, null, result);
    }

    private static int ReadFlags(Godot.Collections.Dictionary pFields)
    {
        if (!pFields.TryGetValue(0x01, out var rawField)) return 0;
        var field = (Godot.Collections.Dictionary)rawField;
        if (!field.TryGetValue("data", out var rawData) || rawData.VariantType != Godot.Variant.Type.PackedByteArray) return 0;
        var data = rawData.AsByteArray();
        if (data.Length == 0) return 0;
        return data[0];
    }

    private static Rm2kParser.ParseResult ReadInteger(Godot.Collections.Dictionary pFields, int pId, int pDefault)
    {
        if (!pFields.TryGetValue(pId, out var rawField))
        {
            return new Rm2kParser.ParseResult(true, null, new Godot.Collections.Dictionary { { "value", pDefault } });
        }
        var field = (Godot.Collections.Dictionary)rawField;
        if (!field.TryGetValue("data", out var rawData) || rawData.VariantType != Godot.Variant.Type.PackedByteArray)
        {
            return new Rm2kParser.ParseResult(false, new Rm2kParser.ParseError(-1, $"Condition field 0x{pId:x2} has no integer payload"));
        }
        var data = rawData.AsByteArray();
        // LCF scalar integers are signed 32-bit values. An explicitly empty
        // integer field is zero, distinct from the default for an absent field.
        using var reader = new LcfBinaryReader(data);
        var value = data.Length == 0 ? 0 : reader.ReadSignedBer();
        if (reader.HasError() || !reader.IsEof())
        {
            var offset = field.TryGetValue("payload_offset", out var rawOffset) ? (int)rawOffset : 0;
            return new Rm2kParser.ParseResult(false, new Rm2kParser.ParseError(
                offset + Math.Max(reader.ErrorOffset, 0), $"Invalid condition integer field 0x{pId:x2}"));
        }
        return new Rm2kParser.ParseResult(true, null, new Godot.Collections.Dictionary { { "value", value } });
    }
}
