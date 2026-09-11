using System;
using System.IO;
using System.Linq;
using Godot;

namespace UniversalRPG.Rm2k.Simulation;

/// <summary>
/// Resolves the initial LMU deterministically. RPG Maker stores the party start
/// map in LMT start data; falling back to the first map is only appropriate when
/// that referenced LMU is unavailable (for example in deliberately reduced test
/// fixtures).
/// </summary>
public static class Rm2kMapLocator
{
    public sealed class Selection
    {
        public string? Path { get; init; }
        public int RequestedMapId { get; init; }
        public bool UsedFallback { get; init; }
        public string Diagnostic { get; init; } = "";
    }

    public static Selection SelectInitialMap(string pRoot, Godot.Collections.Dictionary pMapTree)
    {
        if (string.IsNullOrWhiteSpace(pRoot) || !Directory.Exists(pRoot))
        {
            return new Selection { Diagnostic = "RM2K game directory is unavailable." };
        }

        var maps = Directory.EnumerateFiles(pRoot, "*", SearchOption.TopDirectoryOnly)
            .Where(pPath => Path.GetExtension(pPath).Equals(".lmu", StringComparison.OrdinalIgnoreCase))
            .OrderBy(pPath => Path.GetFileName(pPath), StringComparer.OrdinalIgnoreCase)
            .ThenBy(pPath => Path.GetFileName(pPath), StringComparer.Ordinal)
            .ToArray();
        if (maps.Length == 0)
        {
            return new Selection { Diagnostic = "No LMU map is present in the imported game directory." };
        }

        var requestedMapId = ReadPartyMapId(pMapTree);
        if (requestedMapId > 0)
        {
            var expectedName = $"Map{requestedMapId:D4}.lmu";
            var requested = maps.FirstOrDefault(pPath =>
                Path.GetFileName(pPath).Equals(expectedName, StringComparison.OrdinalIgnoreCase));
            if (requested != null)
            {
                return new Selection
                {
                    Path = requested,
                    RequestedMapId = requestedMapId,
                };
            }

            return new Selection
            {
                Path = maps[0],
                RequestedMapId = requestedMapId,
                UsedFallback = true,
                Diagnostic = $"Start map {requestedMapId} ({expectedName}) is unavailable; using {Path.GetFileName(maps[0])} as a deterministic fallback.",
            };
        }

        return new Selection
        {
            Path = maps[0],
            UsedFallback = true,
            Diagnostic = $"LMT start-map metadata is unavailable; using {Path.GetFileName(maps[0])} as a deterministic fallback.",
        };
    }

    private static int ReadPartyMapId(Godot.Collections.Dictionary pMapTree)
    {
        if (!pMapTree.TryGetValue("start", out var rawStart)
            || rawStart.VariantType != Variant.Type.Dictionary)
        {
            return 0;
        }
        var start = rawStart.AsGodotDictionary();
        if (!start.TryGetValue("party_map_id", out var rawMapId)
            || rawMapId.VariantType != Variant.Type.Int)
        {
            return 0;
        }
        var mapId = rawMapId.AsInt32();
        return mapId > 0 ? mapId : 0;
    }
}
