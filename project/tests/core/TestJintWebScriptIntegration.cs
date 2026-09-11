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
    public void Test_CustomMvPluginExecutesThroughRealEmbeddedVm()
    {
        var factory = new JintScriptVmFactory();
        var created = factory.Create(new ScriptVmRequest
        {
            LanguageId = ScriptLanguageIds.RpgMakerMvJavaScript,
            CompatibilityProfile = "javascript-core",
            Policy = SafePolicy(),
            RequiredFeatures = new[] { "javascript" },
        });
        AssertTrue(created.Success, created.Result.ErrorMessage);
        AssertTrue(created.Vm != null);
        if (created.Vm == null) return;

        using var vm = created.Vm;
        var descriptor = new EngineScriptDescriptor
        {
            Id = "plugin:custom",
            DisplayName = "Custom",
            LanguageId = ScriptLanguageIds.RpgMakerMvJavaScript,
            RelativePath = "js/plugins/Custom.js",
            Origin = ScriptOrigin.Plugin,
            Required = true,
            LoadOrder = 0,
        };
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
                "globalThis.__urpg_custom_plugin_runs = (globalThis.__urpg_custom_plugin_runs || 0) + 1;\n" +
                "function urpgCustomPluginProbe() {\n" +
                "  if (globalThis.__urpg_custom_plugin_runs !== 1) throw new Error('plugin order/state mismatch');\n" +
                "}\n"),
        });
        using var runtime = new WebScriptRuntime(
            ScriptLanguageIds.RpgMakerMvJavaScript,
            vm,
            source,
            new[] { entry });

        var load = runtime.LoadScripts(SafePolicy());
        AssertTrue(load.Success, load.ErrorMessage);
        AssertEq(vm.State, ScriptVmState.Ready);

        var bootstrap = runtime.ExecuteBootstrap();
        AssertTrue(bootstrap.Success, bootstrap.ErrorMessage);
        AssertEq(vm.State, ScriptVmState.Running);

        var invoke = vm.Invoke(new ScriptInvocation
        {
            Target = "globalThis",
            Member = "urpgCustomPluginProbe",
        });
        AssertTrue(invoke.Success, invoke.ErrorMessage);
        AssertEq(runtime.GetPluginParameters("custom")["Greeting"], "Hello");
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
