using System;
using System.Collections.Generic;
using Godot;

namespace UniversalRPG.Rm2k.Parser;

/// <summary>
/// Promotes the verified Chipset vector fields that older parser output kept in
/// unknown_fields into first-class typed values. This is a transitional parser
/// boundary: runtime code consumes only the typed fields, while unrelated
/// unknown LDB chunks remain preserved losslessly.
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
            var entry = rawEntry.AsGodotDictionary();
            if (!TryNormalizeChipset(entry, out pError))
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

        var unknownFields = ReadUnknownFields(pChipset);
        if (unknownFields == null)
        {
            pError = "Chipset unknown_fields is missing or malformed.";
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

        if (!TryDecodeTerrain(terrainRaw, out var terrain, out pError)
            || !TryDecodeByteVector(lowerRaw, LowerPassageCount, 0x0F, 0x0F, false, "passable_data_lower", out var lower, out pError)
            || !TryDecodeByteVector(upperRaw, UpperPassageCount, 0x0F, 0x1F, true, "passable_data_upper", out var upper, out pError))
        {
            return false;
        }

        pChipset["terrain_data"] = terrain;
        pChipset["passable_data_lower"] = lower;
        pChipset["passable_data_upper"] = upper;
        pChipset["unknown_fields"] = retained;
        return true;
    }

    private static Godot.Collections.Array<Godot.Collections.Dictionary>? ReadUnknownFields(
        Godot.Collections.Dictionary pChipset)
    {
        if (!pChipset.TryGetValue("unknown_fields", out var rawUnknown)
            || rawUnknown.VariantType != Variant.Type.Array)
        {
            return null;
        }
        try
        {
            return (Godot.Collections.Array<Godot.Collections.Dictionary>)rawUnknown;
        }
        catch
        {
            return null;
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
        pData = new byte[pLength];
        Array.Fill(pData, pFill);
        if (pDistinctFirstDefault && pData.Length > 0) pData[0] = pFirstDefault;
        if (pRaw == null) return true;
        if (pRaw.Length > pLength)
        {
            pError = $"Chipset {pName} has {pRaw.Length} bytes, expected at most {pLength}.";
            return false;
        }
        Array.Copy(pRaw, pData, pRaw.Length);
        return true;
    }
}
