using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using UniversalRPG.Sdk;

namespace UniversalRPG.Plugins;

/// <summary>
/// Small VM-neutral PluginManager surface. Configuration is JSON data, never a
/// JavaScript object literal: a parameter named __proto__ must remain data too.
/// No CLR object or host capability is exposed by this shim.
/// </summary>
public static class WebPluginManagerShimBuilder
{
    public const string ModuleId = "compat:plugin-manager";

    // JS_PLUGIN_MANAGER_BEGIN
    internal const string FactorySource = """
        ((rawParameters, isMZ) => {
            const root = globalThis;
            const parameters = Object.create(null);
            for (const key of Object.keys(rawParameters)) parameters[key] = rawParameters[key];
            const manager = root.PluginManager || (root.PluginManager = {});
            manager._parameters = parameters;
            manager.parameters = function(name) {
                const key = String(name == null ? '' : name).toLowerCase();
                return this._parameters[key] || {};
            };
            if (isMZ) {
                manager._commands = Object.create(null);
                manager.registerCommand = function(pluginName, commandName, func) {
                    const key = String(pluginName) + ':' + String(commandName);
                    this._commands[key] = func;
                };
                manager.callCommand = function(self, pluginName, commandName, args) {
                    const key = String(pluginName) + ':' + String(commandName);
                    const func = this._commands[key];
                    if (typeof func === 'function') return func.call(self, args);
                };
            }
        })
        """;
    // JS_PLUGIN_MANAGER_END

    public static ScriptModule Build(string pLanguageId, IEnumerable<WebScriptInventoryEntry> pEntries)
    {
        if (pLanguageId is not (ScriptLanguageIds.RpgMakerMvJavaScript or ScriptLanguageIds.RpgMakerMzJavaScript))
            throw new ArgumentException("PluginManager shim requires an MV or MZ JavaScript language ID.", nameof(pLanguageId));
        if (pEntries == null) throw new ArgumentNullException(nameof(pEntries));

        var entries = pEntries.Take(WebScriptInventory.MaxPlugins + 1).ToArray();
        if (entries.Length > WebScriptInventory.MaxPlugins || entries.Any(entry => entry == null || entry.Script == null || entry.Parameters == null))
            throw new ArgumentException("PluginManager configuration is invalid or exceeds the plugin limit.", nameof(pEntries));

        var table = new SortedDictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal);
        foreach (var entry in entries.Where(entry => entry.Enabled).OrderBy(entry => entry.Script.LoadOrder))
        {
            // Keep whitespace in a real filename. RPG Maker's lookup lowercases
            // names but does not silently rename plugins by trimming them.
            var name = entry.Script.DisplayName ?? "";
            if (name.Length == 0) continue;
            table[name.ToLowerInvariant()] = new Dictionary<string, string>(entry.Parameters, StringComparer.Ordinal);
        }

        var json = JsonSerializer.Serialize(table);
        var jsonStringLiteral = JsonSerializer.Serialize(json);
        var isMZ = pLanguageId == ScriptLanguageIds.RpgMakerMzJavaScript;
        var source = FactorySource + "(JSON.parse(" + jsonStringLiteral + "), " + (isMZ ? "true" : "false") + ");\n";
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
            Source = Encoding.UTF8.GetBytes(source),
        };
    }
}
