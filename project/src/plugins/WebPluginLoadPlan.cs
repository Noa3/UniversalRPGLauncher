using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace UniversalRPG.Plugins;

/// <summary>
/// Shared selection for the loader and PluginManager parameter table. Matches
/// MV setup semantics: enabled plugins, stable configured order, first exact
/// plugin name wins. Name de-duplication is case-sensitive; parameter lookup is
/// a separate case-insensitive operation. This does not execute game code.
/// </summary>
public static class WebPluginLoadPlan
{
    public const int MaxTotalParameterCharacters = 4 * 1024 * 1024;

    public static IReadOnlyList<WebScriptInventoryEntry> SelectEnabled(
        IEnumerable<WebScriptInventoryEntry> pEntries)
    {
        if (pEntries == null) throw new ArgumentNullException(nameof(pEntries));
        var entries = pEntries.Take(WebScriptInventory.MaxPlugins + 1).ToArray();
        if (entries.Length > WebScriptInventory.MaxPlugins)
            throw new ArgumentException("Plugin input exceeds the bounded entry limit.", nameof(pEntries));
        if (entries.Any(entry => entry == null || entry.Script == null))
            throw new ArgumentException("Plugin input contains a null entry or descriptor.", nameof(pEntries));

        var names = new HashSet<string>(StringComparer.Ordinal);
        var selected = new List<WebScriptInventoryEntry>();
        var totalCharacters = 0;
        foreach (var entry in entries.Where(entry => entry.Enabled).OrderBy(entry => entry.Script.LoadOrder))
        {
            var name = entry.Script.DisplayName;
            if (string.IsNullOrEmpty(name) || name.Length > 512)
                throw new ArgumentException("An enabled plugin requires a bounded non-empty name.", nameof(pEntries));
            if (!names.Add(name)) continue;
            if (entry.Parameters == null || entry.Parameters.Count > WebScriptInventory.MaxParametersPerPlugin)
                throw new ArgumentException("Plugin parameters exceed the bounded entry limit.", nameof(pEntries));

            var parameters = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var parameter in entry.Parameters)
            {
                if (parameter.Key == null || parameter.Value == null
                    || parameter.Key.Length > WebScriptInventory.MaxParameterNameLength
                    || parameter.Value.Length > WebScriptInventory.MaxParameterValueLength
                    || parameters.Count >= WebScriptInventory.MaxParametersPerPlugin)
                    throw new ArgumentException("Plugin parameter metadata is invalid or too large.", nameof(pEntries));
                var length = parameter.Key.Length + parameter.Value.Length;
                if (length > MaxTotalParameterCharacters - totalCharacters)
                    throw new ArgumentException("Plugin parameters exceed the aggregate character budget.", nameof(pEntries));
                totalCharacters += length;
                parameters.Add(parameter.Key, parameter.Value);
            }
            selected.Add(new WebScriptInventoryEntry
            {
                Script = entry.Script,
                Enabled = true,
                Compatibility = entry.Compatibility,
                Reasons = entry.Reasons,
                Parameters = new ReadOnlyDictionary<string, string>(parameters),
            });
        }
        return selected.AsReadOnly();
    }
}
