using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using UniversalRPG.Plugins;

namespace UniversalRPG.Wolf;

/// <summary>Loads one schema-free WOLF database from the bounded plain envelope.</summary>
public sealed class WolfDatabaseReader
{
    private readonly WolfParseLimits _limits;

    public WolfDatabaseReader(WolfParseLimits pLimits)
    {
        _limits = pLimits ?? throw new ArgumentNullException(nameof(pLimits));
    }

    public PluginResult<WolfDatabaseData> Read(string pPath, string pDatabaseId)
    {
        var document = new WolfDataReader(_limits).ReadDocument(pPath, "database");
        if (!document.Success)
        {
            return PluginResult<WolfDatabaseData>.Failed(document.Error!, document.Diagnostics);
        }
        var root = document.Value;
        if (WolfDataReader.IsProtected(root))
        {
            return PluginResult<WolfDatabaseData>.Failed(PluginError.Create(
                PluginErrorCode.UnsupportedEngine,
                "Protected or encrypted WOLF database data is not decrypted by this runtime.",
                EnginePluginIds.WolfRpg,
                "wolf-database"), new[]
            {
                PluginDiagnostic.Warning("wolf.protected-data", "Protected WOLF database data was rejected.", EnginePluginIds.WolfRpg),
            });
        }

        var records = WolfDataReader.ReadArray(root, "records").ToArray();
        if (records.Length > _limits.MaxDatabaseRecords)
        {
            return WolfDataReader.Failed<WolfDatabaseData>(
                $"WOLF database '{pDatabaseId}' exceeds the record limit.", "wolf-database");
        }

        var parsed = new List<WolfDatabaseRecord>(records.Length);
        for (var index = 0; index < records.Length; index += 1)
        {
            var record = records[index];
            if (record.ValueKind != JsonValueKind.Object)
            {
                return WolfDataReader.Failed<WolfDatabaseData>(
                    $"WOLF database '{pDatabaseId}' record {index} is not an object.", "wolf-database");
            }
            var id = WolfDataReader.ReadOptionalInt(record, "id", index + 1);
            var fields = new Dictionary<string, string>(StringComparer.Ordinal);
            if (record.TryGetProperty("fields", out var fieldObject))
            {
                if (fieldObject.ValueKind != JsonValueKind.Object)
                {
                    return WolfDataReader.Failed<WolfDatabaseData>(
                        $"WOLF database '{pDatabaseId}' record {index} has non-object fields.", "wolf-database");
                }
                foreach (var property in fieldObject.EnumerateObject())
                {
                    if (fields.Count >= _limits.MaxDatabaseFieldsPerRecord)
                    {
                        return WolfDataReader.Failed<WolfDatabaseData>(
                            $"WOLF database '{pDatabaseId}' record {index} exceeds the field limit.", "wolf-database");
                    }
                    if (property.Name.Length == 0 || property.Name.Length > 256)
                    {
                        return WolfDataReader.Failed<WolfDatabaseData>(
                            $"WOLF database '{pDatabaseId}' record {index} contains an invalid field name.", "wolf-database");
                    }
                    var rawValue = property.Value.GetRawText();
                    if (rawValue.Length > _limits.MaxStringBytes)
                    {
                        return WolfDataReader.Failed<WolfDatabaseData>(
                            $"WOLF database '{pDatabaseId}' record {index} contains an oversized field.", "wolf-database");
                    }
                    fields[property.Name] = rawValue;
                }
            }
            parsed.Add(new WolfDatabaseRecord { Id = id, Fields = fields });
        }

        var displayName = WolfDataReader.ReadOptionalString(root, "name", _limits.MaxStringBytes);
        return PluginResult<WolfDatabaseData>.Succeeded(new WolfDatabaseData
        {
            DatabaseId = string.IsNullOrWhiteSpace(pDatabaseId) ? "database" : pDatabaseId,
            DisplayName = displayName,
            Records = parsed,
        }, document.Diagnostics);
    }
}
