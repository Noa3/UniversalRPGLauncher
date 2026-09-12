using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UniversalRPG.JavaScript.Jint;
using UniversalRPG.Plugins;
using UniversalRPG.Sdk;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

public sealed class TestWebPluginShimRegression : TestBase
{
    public void Test_ParameterJsonKeepsPrototypeNamedKeysAndEscapesCodeLikeText()
    {
        var entries = new[]
        {
            Entry("__proto__", new Dictionary<string, string>
            {
                ["Note"] = "kept",
                ["__proto__"] = "ordinary parameter",
                ["Text"] = "'}); globalThis.injected = true; //",
            }),
        };
        ExecuteProbe(ScriptLanguageIds.RpgMakerMvJavaScript, entries, """
            const p = PluginManager.parameters('__PROTO__');
            if (p.Note !== 'kept' || p.__proto__ !== 'ordinary parameter') throw Error('data keys lost');
            if (typeof injected !== 'undefined') throw Error('parameter executed as code');
            if (Object.getPrototypeOf(PluginManager._parameters) !== null) throw Error('parameter table prototype');
            """);
    }

    public void Test_MzCallbackRetainsSelfAndFalsyArgument()
    {
        ExecuteProbe(ScriptLanguageIds.RpgMakerMzJavaScript, Array.Empty<WebScriptInventoryEntry>(), """
            const owner = { count: 0 };
            PluginManager.registerCommand('Test', 'Run', function(args) {
                if (this !== owner || args !== false) throw Error('callback semantics lost');
                this.count++;
            });
            PluginManager.callCommand(owner, 'Test', 'Run', false);
            if (owner.count !== 1) throw Error('callback not called once');
            PluginManager.registerCommand('Test', 'Run', null);
            PluginManager.callCommand(owner, 'Test', 'Run', false);
            if (owner.count !== 1) throw Error('old registration survived replacement');
            """);
    }

    public void Test_WhitespaceInPluginNameIsNotSilentlyTrimmed()
    {
        ExecuteProbe(ScriptLanguageIds.RpgMakerMvJavaScript,
            new[] { Entry(" Space ", new Dictionary<string, string> { ["Value"] = "42" }) }, """
                if (PluginManager.parameters(' SPACE ').Value !== '42') throw Error('plugin name was renamed');
                if (PluginManager.parameters('space').Value !== undefined) throw Error('unexpected trimmed alias');
                """);
    }

    public void Test_SharedBuilderRejectsTooManyEntriesBeforeSorting()
    {
        var rejected = false;
        try
        {
            WebPluginManagerShimBuilder.Build(ScriptLanguageIds.RpgMakerMvJavaScript,
                Enumerable.Repeat(Entry("Example", new Dictionary<string, string>()), WebScriptInventory.MaxPlugins + 1));
        }
        catch (ArgumentException) { rejected = true; }
        AssertTrue(rejected);
    }

    private void ExecuteProbe(string language, IEnumerable<WebScriptInventoryEntry> entries, string source)
    {
        using var vm = new JintEmbeddedScriptVm(language);
        var configured = vm.Configure(new ScriptExecutionPolicy
        {
            MaxMemoryMegabytes = 64,
            MaxExecutionMillisecondsPerTick = 500,
            MaxCallDepth = 128,
        });
        AssertTrue(configured.Success, configured.ErrorMessage);
        var shim = WebPluginManagerShimBuilder.Build(language, entries);
        var probe = new ScriptModule
        {
            Descriptor = new EngineScriptDescriptor
            {
                Id = "plugin:probe", DisplayName = "Probe", LanguageId = language,
                RelativePath = "js/plugins/Probe.js", LoadOrder = 0,
            },
            Source = Encoding.UTF8.GetBytes(source),
        };
        var loadedShim = vm.LoadModule(shim);
        AssertTrue(loadedShim.Success, loadedShim.ErrorMessage);
        var loadedProbe = vm.LoadModule(probe);
        AssertTrue(loadedProbe.Success, loadedProbe.ErrorMessage);
        var runShim = vm.ExecuteModule(shim.Descriptor.Id);
        AssertTrue(runShim.Success, runShim.ErrorMessage);
        var runProbe = vm.ExecuteModule(probe.Descriptor.Id);
        AssertTrue(runProbe.Success, runProbe.ErrorMessage);
    }

    private static WebScriptInventoryEntry Entry(string name, IReadOnlyDictionary<string, string> parameters) => new()
    {
        Enabled = true,
        Parameters = parameters,
        Compatibility = WebScriptCompatibility.StandardBrowserApi,
        Script = new EngineScriptDescriptor
        {
            Id = "plugin:fixture", DisplayName = name,
            LanguageId = ScriptLanguageIds.RpgMakerMvJavaScript,
            RelativePath = "js/plugins/Fixture.js", LoadOrder = 0,
        },
    };
}
