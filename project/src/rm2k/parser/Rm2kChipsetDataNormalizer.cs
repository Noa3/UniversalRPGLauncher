using System;
using Godot;

namespace UniversalRPG.Rm2k.Parser;

/// <summary>
/// Promotes the verified Chipset vector fields that older parser output kept in
/// unknown_fields into first-class typed values. The operation is idempotent:
/// already-typed parser output is validated and normalized to the canonical
/// bounded vector lengths, while unrelated unknown fields remain preserved.
///
/// Verified liblcf fields:
/// 0x03 terrain_data          Vector&lt;Int16&gt; length 162, default 1
/// 0x04 passable_data_lower  Vector&lt;UInt8&gt; length 162, default 0x0F
/// 0x05 passable_data_upper  Vector&lt;UInt8&gt; length 144, default [0x1F, 0x0F...]
/// </summary>
public static class Rm2kChipsetDataNormalizer
{
    public const int TerrainCount = 162;
    public const int LowerPassageCount = 162;
    public const int UpperPassageCount = 144;

    public static bool TryNormalizeDatabase(Godot.Collections.Dictionary pDatabase, out string pError)
    {
        pError = "";
        if (pDatabase == null)
        {
            pError = "RM2K database is null.";
            return false;
        }
        if (!pDatabase.TryGetValue("chipsets", out var rawChipsets)
            || rawChipsets.VariantType != Variant.Type.Array)
        {
            pError = "RM2K database does not contain a chipset array.";
            return false;
        }

        foreach (var rawEntry in rawChipsets.AsGodotArray())
        {
            if (rawEntry.VariantType != Variant.Type.Dictionary)
            {
                pError = "RM2K chipset array contains a non-dictionary entry.";
                return false;
            }
            if (!TryNormalizeChipset(rawEntry.AsGodotDictionary(), out pError))
            {
                return false;
            }
        }
        return true;
    }

    public static bool TryNormalizeChipset(Godot.Collections.Dictionary pChipset, out string pError)
    {
        pError = "";
        if (pChipset == null)
        {
            pError = "Chipset entry is null.";
            return false;
        }

        if (!TryReadUnknownFields(pChipset, out var unknownFields, out pError))
        {
            return false;
        }

        var retained = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        byte[]? terrainRaw = null;
        byte[]? lowerRaw = null;
        byte[]? upperRaw = null;
        foreach (var field in unknownFields)
        {
            if (!TryReadField(field, out var id, out var data))
            {
                pError = "Chipset unknown field is malformed.";
                return false;
            }
            switch (id)
            {
                case 0x03:
                    if (terrainRaw != null)
                    {
                        pError = "Chipset contains duplicate terrain_data fields.";
                        return false;
                    }
                    terrainRaw = data;
                    break;
                case 0x04:
                    if (lowerRaw != null)
                    {
                        pError = "Chipset contains duplicate passable_data_lower fields.";
                        return false;
                    }
                    lowerRaw = data;
                    break;
                case 0x05:
                    if (upperRaw != null)
                    {
                        pError = "Chipset contains duplicate passable_data_upper fields.";
                        return false;
                    }
                    upperRaw = data;
                    break;
                default:
                    retained.Add(field);
                    break;
            }
        }

        int[] terrain;
        if (pChipset.ContainsKey("terrain_data"))
        {
            if (!TryNormalizeTypedIntVector(pChipset["terrain_data"], TerrainCount, 1, "terrain_data", out terrain, out pError))
            {
                return false;
            }
        }
        else if (!TryDecodeTerrain(terrainRaw, out terrain, out pError))
        {
            return false;
        }

        byte[] lower;
        if (pChipset.ContainsKey("passable_data_lower"))
        {
            if (!TryNormalizeTypedByteVector(pChipset["passable_data_lower"], LowerPassageCount, 0x0F, 0x0F, false,
                    "passable_data_lower", out lower, out pError))
            {
                return false;
            }
        }
        else if (!TryDecodeByteVector(lowerRaw, LowerPassageCount, 0x0F, 0x0F, false,
                     "passable_data_lower", out lower, out pError))
        {
            return false;
        }

        byte[] upper;
        if (pChipset.ContainsKey("passable_data_upper"))
        {
            if (!TryNormalizeTypedByteVector(pChipset["passable_data_upper"], UpperPassageCount, 0x0F, 0x1F, true,
                    "passable_data_upper", out upper, out pError))
            {
                return false;
            }
        }
        else if (!TryDecodeByteVector(upperRaw, UpperPassageCount, 0x0F, 0x1F, true,
                     "passable_data_upper", out upper, out pError))
        {
            return false;
        }

        pChipset["terrain_data"] = terrain;
        pChipset["passable_data_lower"] = lower;
        pChipset["passable_data_upper"] = upper;
        pChipset["unknown_fields"] = retained;
        return true;
    }

    private static bool TryReadUnknownFields(
        Godot.Collections.Dictionary pChipset,
        out Godot.Collections.Array<Godot.Collections.Dictionary> pFields,
        out string pError)
    {
        pError = "";
        pFields = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        if (!pChipset.TryGetValue("unknown_fields", out var rawUnknown))
        {
            return true;
        }
        if (rawUnknown.VariantType != Variant.Type.Array)
        {
            pError = "Chipset unknown_fields is malformed.";
            return false;
        }
        try
        {
            pFields = (Godot.Collections.Array<Godot.Collections.Dictionary>)rawUnknown;
            return true;
        }
        catch
        {
            pError = "Chipset unknown_fields contains incompatible entries.";
            return false;
        }
    }

    private static bool TryReadField(
        Godot.Collections.Dictionary pField,
        out int pId,
        out byte[] pData)
    {
        pId = 0;
        pData = Array.Empty<byte>();
        if (!pField.TryGetValue("id", out var rawId) || rawId.VariantType != Variant.Type.Int
            || !pField.TryGetValue("data", out var rawData) || rawData.VariantType != Variant.Type.PackedByteArray)
        {
            return false;
        }
        pId = rawId.AsInt32();
        pData = rawData.AsByteArray();
        return true;
    }

    private static bool TryDecodeTerrain(
        byte[]? pRaw,
        out int[] pTerrain,
        out string pError)
    {
        pError = "";
        pTerrain = new int[TerrainCount];
        Array.Fill(pTerrain, 1);
        if (pRaw == null) return true;
        if ((pRaw.Length & 1) != 0 || pRaw.Length / 2 > TerrainCount)
        {
            pError = $"Chipset terrain_data has invalid {pRaw.Length}-byte length; expected at most {TerrainCount * 2} even bytes.";
            return false;
        }
        for (var index = 0; index < pRaw.Length / 2; index++)
        {
            pTerrain[index] = unchecked((short)(pRaw[index * 2] | (pRaw[index * 2 + 1] << 8)));
        }
        return true;
    }

    private static bool TryDecodeByteVector(
        byte[]? pRaw,
        int pLength,
        byte pFill,
        byte pFirstDefault,
        bool pDistinctFirstDefault,
        string pName,
        out byte[] pData,
        out string pError)
    {
        pError = "";
        pData = CreateDefaultByteVector(pLength, pFill, pFirstDefault, pDistinctFirstDefault);
        if (pRaw == null) return true;
        if (pRaw.Length > pLength)
        {
            pError = $"Chipset {pName} has {pRaw.Length} bytes, expected at most {pLength}.";
            return false;
        }
        Array.Copy(pRaw, pData, pRaw.Length);
        return true;
    }

    private static bool TryNormalizeTypedIntVector(
        Variant pRaw,
        int pLength,
        int pFill,
        string pName,
        out int[] pData,
        out string pError)
    {
        pError = "";
        pData = new int[pLength];
        Array.Fill(pData, pFill);
        int[] source;
        if (pRaw.VariantType == Variant.Type.PackedInt32Array)
        {
            source = pRaw.AsInt32Array();
        }
        else if (pRaw.VariantType == Variant.Type.Array)
        {
            var array = pRaw.AsGodotArray();
            source = new int[array.Count];
            for (var index = 0; index < array.Count; index++)
            {
                if (array[index].VariantType != Variant.Type.Int)
                {
                    pError = $"Chipset {pName} contains a non-integer value.";
                    return false;
                }
                source[index] = array[index].AsInt32();
            }
        }
        else
        {
            pError = $"Chipset {pName} is not an integer vector.";
            return false;
        }
        if (source.Length > pLength)
        {
            pError = $"Chipset {pName} has {source.Length} values, expected at most {pLength}.";
            return false;
        }
        Array.Copy(source, pData, source.Length);
        return true;
    }

    private static bool TryNormalizeTypedByteVector(
        Variant pRaw,
        int pLength,
        byte pFill,
        byte pFirstDefault,
        bool pDistinctFirstDefault,
        string pName,
        out byte[] pData,
        out string pError)
    {
        pError = "";
        pData = CreateDefaultByteVector(pLength, pFill, pFirstDefault, pDistinctFirstDefault);
        byte[] source;
        if (pRaw.VariantType == Variant.Type.PackedByteArray)
        {
            source = pRaw.AsByteArray();
        }
        else if (pRaw.VariantType == Variant.Type.Array)
        {
            var array = pRaw.AsGodotArray();
            source = new byte[array.Count];
            for (var index = 0; index < array.Count; index++)
            {
                if (array[index].VariantType != Variant.Type.Int)
                {
                    pError = $"Chipset {pName} contains a non-integer value.";
                    return false;
                }
                var value = array[index].AsInt32();
                if (value < byte.MinValue || value > byte.MaxValue)
                {
                    pError = $"Chipset {pName} contains byte value {value} outside 0..255.";
                    return false;
                }
                source[index] = (byte)value;
            }
        }
        else
        {
            pError = $"Chipset {pName} is not a byte vector.";
            return false;
        }
        if (source.Length > pLength)
        {
            pError = $"Chipset {pName} has {source.Length} values, expected at most {pLength}.";
            return false;
        }
        Array.Copy(source, pData, source.Length);
        return true;
    }

    private static byte[] CreateDefaultByteVector(
        int pLength,
        byte pFill,
        byte pFirstDefault,
        bool pDistinctFirstDefault)
    {
        var result = new byte[pLength];
        Array.Fill(result, pFill);
        if (pDistinctFirstDefault && result.Length > 0) result[0] = pFirstDefault;
        return result;
    }
}
