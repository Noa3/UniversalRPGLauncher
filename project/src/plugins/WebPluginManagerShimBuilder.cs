using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using UniversalRPG.Sdk;

namespace UniversalRPG.Plugins;

/// <summary>
/// VM-neutral PluginManager parameter and MZ command surface. The host supplies
/// the same bounded load plan used by WebScriptRuntime. Dynamic script loading
/// still requires a real VFS/DOM host and is not simulated here.
/// </summary>
public static class WebPluginManagerShimBuilder
{
    public const string ModuleId = "compat:plugin-manager";

    // JS_PLUGIN_MANAGER_BEGIN
    internal const string FactorySource = """
        ((rawParameters, isMZ, scheduledNames = []) => {
            const root = globalThis;
            const parameters = Object.create(null);
            for (const key of Object.keys(rawParameters)) parameters[key] = rawParameters[key];
            const manager = root.PluginManager || (root.PluginManager = {});
            manager._parameters = parameters;
            // RPG Maker fills _scripts while scheduling loads, before plugin
            // evaluation. This list is not proof of successful initialization.
            manager._scripts = scheduledNames.slice();
            manager.parameters = function(name) {
                return this._parameters[name.toLowerCase()] || {};
            };
            manager.setParameters = function(name, values) {
                this._parameters[name.toLowerCase()] = values;
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
        var entries = WebPluginLoadPlan.SelectEnabled(pEntries);
        var table = new SortedDictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal);
        foreach (var entry in entries)
            table[entry.Script.DisplayName.ToLowerInvariant()] = entry.Parameters;

        // JSON.parse preserves __proto__ as a data property, unlike an object
        // literal. Names/values are serialized, never interpolated as code.
        var jsonStringLiteral = JsonSerializer.Serialize(JsonSerializer.Serialize(table));
        var namesJson = JsonSerializer.Serialize(entries.Select(entry => entry.Script.DisplayName).ToArray());
        var namesStringLiteral = JsonSerializer.Serialize(namesJson);
        var isMZ = pLanguageId == ScriptLanguageIds.RpgMakerMzJavaScript;
        var source = FactorySource + "(JSON.parse(" + jsonStringLiteral + "), "
            + (isMZ ? "true" : "false") + ", JSON.parse(" + namesStringLiteral + "));\n";
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
