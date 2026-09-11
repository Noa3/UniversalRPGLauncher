using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using UniversalRPG.Sdk;

namespace UniversalRPG.Plugins;

/// <summary>
/// Builds the small, VM-neutral PluginManager surface that custom MV/MZ plugins
/// need before the full original browser/RPG Maker runtime is available.
/// Parameters are copied from bounded plugins.js metadata. MZ command callbacks
/// are stored inside the JavaScript realm; no CLR object or host capability is
/// exposed by this shim.
/// </summary>
public static class WebPluginManagerShimBuilder
{
    public const string ModuleId = "compat:plugin-manager";

    public static ScriptModule Build(
        string pLanguageId,
        IEnumerable<WebScriptInventoryEntry> pEntries)
    {
        if (pLanguageId is not (ScriptLanguageIds.RpgMakerMvJavaScript or ScriptLanguageIds.RpgMakerMzJavaScript))
        {
            throw new ArgumentException("PluginManager shim requires an MV or MZ JavaScript language ID.", nameof(pLanguageId));
        }
        if (pEntries == null) throw new ArgumentNullException(nameof(pEntries));

        var table = new SortedDictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal);
        foreach (var entry in pEntries.Where(pEntry => pEntry.Enabled).OrderBy(pEntry => pEntry.Script.LoadOrder))
        {
            var name = entry.Script.DisplayName?.Trim() ?? "";
            if (name.Length == 0) continue;
            // RPG Maker's parameters(name) normalizes by lower-case plugin name,
            // and later configured entries replace earlier ones.
            table[name.ToLowerInvariant()] = new Dictionary<string, string>(entry.Parameters, StringComparer.Ordinal);
        }

        var json = JsonSerializer.Serialize(table);
        var isMZ = pLanguageId == ScriptLanguageIds.RpgMakerMzJavaScript;
        var source = new StringBuilder(2048 + json.Length);
        source.Append("(() => {\n");
        source.Append("  const root = globalThis;\n");
        source.Append("  const rawParameters = ").Append(json).Append(";\n");
        source.Append("  const parameters = Object.create(null);\n");
        source.Append("  for (const key of Object.keys(rawParameters)) parameters[key] = rawParameters[key];\n");
        source.Append("  const manager = root.PluginManager || (root.PluginManager = {});\n");
        source.Append("  manager._parameters = parameters;\n");
        source.Append("  manager.parameters = function(name) {\n");
        source.Append("    const key = String(name == null ? '' : name).toLowerCase();\n");
        source.Append("    return this._parameters[key] || {};\n");
        source.Append("  };\n");

        if (isMZ)
        {
            source.Append("  manager._commands = Object.create(null);\n");
            source.Append("  manager.registerCommand = function(pluginName, commandName, func) {\n");
            source.Append("    if (typeof func !== 'function') return;\n");
            source.Append("    const key = String(pluginName) + ':' + String(commandName);\n");
            source.Append("    this._commands[key] = func;\n");
            source.Append("  };\n");
            source.Append("  manager.callCommand = function(self, pluginName, commandName, args) {\n");
            source.Append("    const key = String(pluginName) + ':' + String(commandName);\n");
            source.Append("    const func = this._commands[key];\n");
            source.Append("    if (typeof func === 'function') return func.call(self, args || {});\n");
            source.Append("  };\n");
        }

        source.Append("})();\n");
        return new ScriptModule
        {
            Descriptor = new EngineScriptDescriptor
            {
                Id = ModuleId,
                DisplayName = "UniversalRPG PluginManager compatibility",
                LanguageId = pLanguageId,
                RelativePath = "",
                Origin = ScriptOrigin.CompatibilityShim,
                Required = true,
                LoadOrder = int.MinValue,
            },
            Source = Encoding.UTF8.GetBytes(source.ToString()),
        };
    }
}
