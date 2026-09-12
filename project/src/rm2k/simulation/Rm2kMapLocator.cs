using System;
using System.IO;
using System.Linq;
using Godot;

namespace UniversalRPG.Rm2k.Simulation;

/// <summary>
/// Resolves LMU files deterministically. RPG Maker stores the party start map
/// in LMT start data; falling back to the first map is only appropriate when
/// that referenced LMU is unavailable (for example in reduced test fixtures).
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
        var maps = EnumerateMaps(pRoot);
        if (maps.Length == 0)
        {
            return new Selection
            {
                Diagnostic = Directory.Exists(pRoot)
                    ? "No LMU map is present in the imported game directory."
                    : "RM2K game directory is unavailable."
            };
        }

        var requestedMapId = ReadPartyMapId(pMapTree);
        if (requestedMapId > 0)
        {
            var requested = FindMap(maps, requestedMapId);
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
                Diagnostic = $"Start map {requestedMapId} (Map{requestedMapId:D4}.lmu) is unavailable; using {Path.GetFileName(maps[0])} as a deterministic fallback.",
            };
        }

        return new Selection
        {
            Path = maps[0],
            UsedFallback = true,
            Diagnostic = $"LMT start-map metadata is unavailable; using {Path.GetFileName(maps[0])} as a deterministic fallback.",
        };
    }

    public static Selection SelectMapById(string pRoot, int pMapId)
    {
        if (pMapId < 1 || pMapId > GameSimulationState.MaxMapId)
        {
            return new Selection
            {
                RequestedMapId = pMapId,
                Diagnostic = $"Map ID {pMapId} is outside the supported RM2K simulation range.",
            };
        }

        var maps = EnumerateMaps(pRoot);
        var requested = FindMap(maps, pMapId);
        return requested != null
            ? new Selection { Path = requested, RequestedMapId = pMapId }
            : new Selection
            {
                RequestedMapId = pMapId,
                Diagnostic = $"Map{pMapId:D4}.lmu is not present in the imported game directory.",
            };
    }

    private static string[] EnumerateMaps(string pRoot)
    {
        if (string.IsNullOrWhiteSpace(pRoot) || !Directory.Exists(pRoot))
        {
            return Array.Empty<string>();
        }
        return Directory.EnumerateFiles(pRoot, "*", SearchOption.TopDirectoryOnly)
            .Where(pPath => Path.GetExtension(pPath).Equals(".lmu", StringComparison.OrdinalIgnoreCase))
            .OrderBy(pPath => Path.GetFileName(pPath), StringComparer.OrdinalIgnoreCase)
            .ThenBy(pPath => Path.GetFileName(pPath), StringComparer.Ordinal)
            .ToArray();
    }

    private static string? FindMap(string[] pMaps, int pMapId)
    {
        var expectedName = $"Map{pMapId:D4}.lmu";
        return pMaps.FirstOrDefault(pPath =>
            Path.GetFileName(pPath).Equals(expectedName, StringComparison.OrdinalIgnoreCase));
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
