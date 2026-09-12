using System;
using System.Collections.Generic;
using System.Text;
using UniversalRPG.JavaScript.Jint;
using UniversalRPG.Plugins;
using UniversalRPG.Sdk;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

public sealed class TestJintWebScriptIntegration : TestBase
{
    public void Test_CustomMvPluginReadsConfiguredParametersThroughRealVm()
    {
        var vm = CreateVm(ScriptLanguageIds.RpgMakerMvJavaScript);
        if (vm == null) return;
        using var ownedVm = vm;

        var descriptor = Descriptor("Custom", ScriptLanguageIds.RpgMakerMvJavaScript);
        var entry = new WebScriptInventoryEntry
        {
            Script = descriptor,
            Enabled = true,
            Compatibility = WebScriptCompatibility.StandardBrowserApi,
            Parameters = new Dictionary<string, string> { ["Greeting"] = "Hello" },
        };
        var source = new StaticSourceProvider(new Dictionary<string, byte[]>
        {
            [descriptor.Id] = Encoding.UTF8.GetBytes(
                "const urpgParams = PluginManager.parameters('Custom');\n" +
                "if (urpgParams.Greeting !== 'Hello') throw new Error('PluginManager parameters mismatch');\n" +
                "globalThis.__urpg_custom_plugin_runs = (globalThis.__urpg_custom_plugin_runs || 0) + 1;\n" +
                "function urpgCustomPluginProbe() {\n" +
                "  if (globalThis.__urpg_custom_plugin_runs !== 1) throw new Error('plugin order/state mismatch');\n" +
                "}\n"),
        });
        var prelude = WebPluginManagerShimBuilder.Build(
            ScriptLanguageIds.RpgMakerMvJavaScript,
            new[] { entry });
        using var runtime = new WebScriptRuntime(
            ScriptLanguageIds.RpgMakerMvJavaScript,
            ownedVm,
            source,
            new[] { entry },
            new[] { prelude });

        var load = runtime.LoadScripts(SafePolicy());
        AssertTrue(load.Success, load.ErrorMessage);
        AssertEq(ownedVm.State, ScriptVmState.Ready);
        AssertEq(runtime.Scripts.Count, 2, "compatibility prelude is visible before the game plugin");
        AssertEq(runtime.Scripts[0].Id, WebPluginManagerShimBuilder.ModuleId);

        var bootstrap = runtime.ExecuteBootstrap();
        AssertTrue(bootstrap.Success, bootstrap.ErrorMessage);
        AssertEq(ownedVm.State, ScriptVmState.Running);

        var invoke = ownedVm.Invoke(new ScriptInvocation
        {
            Target = "globalThis",
            Member = "urpgCustomPluginProbe",
        });
        AssertTrue(invoke.Success, invoke.ErrorMessage);
        AssertEq(runtime.GetPluginParameters("custom")["Greeting"], "Hello");
    }

    public void Test_MzRegisterCommandAndCallCommandWorkInsideCompatibilityRealm()
    {
        var vm = CreateVm(ScriptLanguageIds.RpgMakerMzJavaScript);
        if (vm == null) return;
        using var ownedVm = vm;

        var descriptor = Descriptor("CommandPlugin", ScriptLanguageIds.RpgMakerMzJavaScript);
        var entry = new WebScriptInventoryEntry
        {
            Script = descriptor,
            Enabled = true,
            Compatibility = WebScriptCompatibility.StandardBrowserApi,
        };
        var source = new StaticSourceProvider(new Dictionary<string, byte[]>
        {
            [descriptor.Id] = Encoding.UTF8.GetBytes(
                "PluginManager.registerCommand('CommandPlugin', 'Ping', function(args) {\n" +
                "  globalThis.__urpg_command_value = args.value;\n" +
                "});\n" +
                "function urpgCommandProbe() {\n" +
                "  PluginManager.callCommand({}, 'CommandPlugin', 'Ping', { value: 'ok' });\n" +
                "  if (globalThis.__urpg_command_value !== 'ok') throw new Error('command callback mismatch');\n" +
                "}\n"),
        });
        var prelude = WebPluginManagerShimBuilder.Build(
            ScriptLanguageIds.RpgMakerMzJavaScript,
            new[] { entry });
        using var runtime = new WebScriptRuntime(
            ScriptLanguageIds.RpgMakerMzJavaScript,
            ownedVm,
            source,
            new[] { entry },
            new[] { prelude });

        AssertTrue(runtime.LoadScripts(SafePolicy()).Success);
        AssertTrue(runtime.ExecuteBootstrap().Success);
        var invoke = ownedVm.Invoke(new ScriptInvocation
        {
            Target = "globalThis",
            Member = "urpgCommandProbe",
        });
        AssertTrue(invoke.Success, invoke.ErrorMessage);
    }

    public void Test_FullMvProfileStillFailsClosedUntilBrowserHostExists()
    {
        var result = WebVmProfiles.CreateVm(
            pMZ: false,
            new JintScriptVmFactory(),
            SafePolicy());

        AssertFalse(result.Success);
        AssertEq(result.Result.ErrorCode, "jint.feature-unsupported");
    }

    private IEmbeddedScriptVm? CreateVm(string pLanguageId)
    {
        var factory = new JintScriptVmFactory();
        var created = factory.Create(new ScriptVmRequest
        {
            LanguageId = pLanguageId,
            CompatibilityProfile = "javascript-core",
            Policy = SafePolicy(),
            RequiredFeatures = new[] { "javascript" },
        });
        AssertTrue(created.Success, created.Result.ErrorMessage);
        AssertTrue(created.Vm != null);
        return created.Vm;
    }

    private static EngineScriptDescriptor Descriptor(string pName, string pLanguageId) => new()
    {
        Id = "plugin:" + pName.ToLowerInvariant(),
        DisplayName = pName,
        LanguageId = pLanguageId,
        RelativePath = "js/plugins/" + pName + ".js",
        Origin = ScriptOrigin.Plugin,
        Required = true,
        LoadOrder = 0,
    };

    private static ScriptExecutionPolicy SafePolicy() => new()
    {
        AllowReadGameFiles = true,
        AllowWriteSaveFiles = true,
        AllowWriteCacheFiles = true,
        AllowArbitraryHostFileSystem = false,
        AllowNetwork = false,
        AllowClipboard = false,
        AllowProcessExecution = false,
        AllowNativeInterop = false,
        MaxMemoryMegabytes = 64,
        MaxExecutionMillisecondsPerTick = 250,
        MaxCallDepth = 128,
    };

    private sealed class StaticSourceProvider : IWebScriptSourceProvider
    {
        private readonly IReadOnlyDictionary<string, byte[]> _sources;

        public StaticSourceProvider(IReadOnlyDictionary<string, byte[]> pSources)
        {
            _sources = pSources;
        }

        public ScriptSourceResult Read(EngineScriptDescriptor pScript)
        {
            return _sources.TryGetValue(pScript.Id, out var source)
                ? ScriptSourceResult.Succeeded(source)
                : ScriptSourceResult.Failed("missing source");
        }
    }
}
