using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using UniversalRPG.Sdk;

namespace UniversalRPG.Plugins;

/// <summary>
/// Calls the project's ORIGINAL PluginManager.setup on its ORIGINAL $plugins.
/// Only synchronous script scheduling is captured/checked; actual plugin files
/// are executed by the existing ordered loader. No replacement parameter or
/// command API is installed, and no dynamic-script support is fabricated.
/// </summary>
public static class NativeCorePluginSetup
{
    public const string ModuleId = "compat:original-plugin-setup";

    // JS_ORIGINAL_PLUGIN_SETUP_BEGIN
    internal const string FactorySource = """
        ((expected, mz) => {
            'use strict';
            const manager = globalThis.PluginManager;
            const config = globalThis.$plugins;
            const fail = message => { throw new Error(message); };
            if (!manager || typeof manager.setup !== 'function' || typeof manager.loadScript !== 'function'
                || typeof manager.setParameters !== 'function' || typeof manager.parameters !== 'function'
                || !Array.isArray(config) || !Array.isArray(manager._scripts))
                fail('boot.original-plugin-manager-required');
            if (manager._scripts.length !== 0) fail('boot.plugins-already-scheduled');
            if (!Array.isArray(expected) || expected.length > 4096) fail('boot.plugin-plan-invalid');
            // This is not a security boundary against an explicitly executed
            // game. It prevents accidental loading a different project/config.
            const apply = Reflect.apply;
            const descriptor = Object.getOwnPropertyDescriptor(manager, 'loadScript');
            if (!descriptor || !descriptor.configurable || !Object.hasOwn(descriptor, 'value'))
                fail('boot.script-scheduler-not-replaceable');
            let next = 0;
            const capture = function(name) {
                const target = next < expected.length ? expected[next] + (mz ? '' : '.js') : null;
                if (typeof name !== 'string' || name !== target) fail('boot.plugin-schedule-mismatch');
                next++;
            };
            Object.defineProperty(manager, 'loadScript', { ...descriptor, value: capture });
            try {
                apply(manager.setup, manager, [config]);
                if (next !== expected.length || manager._scripts.length !== expected.length)
                    fail('boot.plugin-schedule-incomplete');
                for (let i = 0; i < expected.length; i++) {
                    if (manager._scripts[i] !== expected[i]) fail('boot.plugin-order-mismatch');
                }
            } finally {
                Object.defineProperty(manager, 'loadScript', descriptor);
            }
            // Leave original methods and their custom aliases intact. The next
            // actual plugin module is evaluated by WebScriptRuntime, not eval().
        })
        """;
    // JS_ORIGINAL_PLUGIN_SETUP_END

    public static ScriptModule Build(string language, IEnumerable<WebScriptInventoryEntry> entries)
    {
        if (language is not (ScriptLanguageIds.RpgMakerMvJavaScript or ScriptLanguageIds.RpgMakerMzJavaScript))
            throw new ArgumentException("Original plugin setup requires MV or MZ.", nameof(language));
        var names = WebPluginLoadPlan.SelectEnabled(entries).Select(e => e.Script.DisplayName).ToArray();
        var source = FactorySource + "(" + JsonSerializer.Serialize(names) + ", "
            + (language == ScriptLanguageIds.RpgMakerMzJavaScript ? "true" : "false") + ");\n";
        return new ScriptModule
        {
            Descriptor = new EngineScriptDescriptor
            {
                Id = ModuleId, DisplayName = "Original project PluginManager setup",
                LanguageId = language, Origin = ScriptOrigin.CompatibilityShim,
                Required = true, LoadOrder = int.MaxValue,
            },
            Source = Encoding.UTF8.GetBytes(source),
        };
    }
}
