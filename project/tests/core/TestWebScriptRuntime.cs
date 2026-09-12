using System;
using System.Collections.Generic;
using System.Text;
using UniversalRPG.Plugins;
using UniversalRPG.Sdk;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

public sealed class TestWebScriptRuntime : TestBase
{
    public void Test_LoadAndBootstrapUseEnabledPluginOrder()
    {
        var vm = new RecordingVm(ScriptLanguageIds.RpgMakerMzJavaScript);
        var source = new MemorySourceProvider();
        var entries = new[]
        {
            Entry("third", 2, enabled: true, WebScriptCompatibility.StandardBrowserApi, source),
            Entry("disabled", 1, enabled: false, WebScriptCompatibility.StandardBrowserApi, source),
            Entry("first", 0, enabled: true, WebScriptCompatibility.StandardBrowserApi, source),
        };
        using var runtime = new WebScriptRuntime(
            ScriptLanguageIds.RpgMakerMzJavaScript,
            vm,
            source,
            entries);

        AssertTrue(runtime.LoadScripts(ScriptExecutionPolicy.SafeDefault).Success);
        AssertEq(string.Join(",", vm.LoadedIds), "plugin:first,plugin:third");
        AssertEq(vm.ExecutedIds.Count, 0, "loading does not execute game-authored JavaScript");

        AssertTrue(runtime.ExecuteBootstrap().Success);
        AssertEq(string.Join(",", vm.ExecutedIds), "plugin:first,plugin:third");
        AssertEq(runtime.Scripts.Count, 2, "disabled plugins are not part of executable runtime script set");
    }

    public void Test_SafePolicyRejectsProcessExecutionPluginBeforeVmLoad()
    {
        var vm = new RecordingVm(ScriptLanguageIds.RpgMakerMvJavaScript);
        var source = new MemorySourceProvider { LanguageId = ScriptLanguageIds.RpgMakerMvJavaScript };
        var process = Entry(
            "process",
            0,
            enabled: true,
            WebScriptCompatibility.RequiresProcessExecution,
            source);
        using var runtime = new WebScriptRuntime(
            ScriptLanguageIds.RpgMakerMvJavaScript,
            vm,
            source,
            new[] { process });

        var result = runtime.LoadScripts(ScriptExecutionPolicy.SafeDefault);

        AssertFalse(result.Success);
        AssertEq(result.ErrorCode, "web.process-denied");
        AssertEq(vm.LoadedIds.Count, 0);
    }

    public void Test_SafePolicyRejectsNativeAddonPluginBeforeVmLoad()
    {
        var vm = new RecordingVm(ScriptLanguageIds.RpgMakerMzJavaScript);
        var source = new MemorySourceProvider();
        var native = Entry(
            "native",
            0,
            enabled: true,
            WebScriptCompatibility.RequiresNativeAddon,
            source);
        using var runtime = new WebScriptRuntime(
            ScriptLanguageIds.RpgMakerMzJavaScript,
            vm,
            source,
            new[] { native });

        var result = runtime.LoadScripts(ScriptExecutionPolicy.SafeDefault);

        AssertFalse(result.Success);
        AssertEq(result.ErrorCode, "web.native-addon-denied");
        AssertEq(vm.LoadedIds.Count, 0);
    }

    public void Test_ExplicitPolicyCanPermitProcessCompatibilityPath()
    {
        var vm = new RecordingVm(ScriptLanguageIds.RpgMakerMvJavaScript);
        var source = new MemorySourceProvider { LanguageId = ScriptLanguageIds.RpgMakerMvJavaScript };
        var process = Entry(
            "process",
            0,
            enabled: true,
            WebScriptCompatibility.RequiresProcessExecution,
            source);
        using var runtime = new WebScriptRuntime(
            ScriptLanguageIds.RpgMakerMvJavaScript,
            vm,
            source,
            new[] { process });
        var policy = new ScriptExecutionPolicy
        {
            AllowProcessExecution = true,
        };

        AssertTrue(runtime.LoadScripts(policy).Success);
        AssertEq(vm.LoadedIds.Count, 1);
        AssertTrue(vm.Policy.AllowProcessExecution);
    }

    public void Test_SourceProviderFailureStopsBeforeExecution()
    {
        var vm = new RecordingVm(ScriptLanguageIds.RpgMakerMzJavaScript);
        var source = new MemorySourceProvider { FailId = "plugin:missing" };
        var missing = Entry(
            "missing",
            0,
            enabled: true,
            WebScriptCompatibility.StandardBrowserApi,
            source);
        using var runtime = new WebScriptRuntime(
            ScriptLanguageIds.RpgMakerMzJavaScript,
            vm,
            source,
            new[] { missing });

        var result = runtime.LoadScripts(ScriptExecutionPolicy.SafeDefault);

        AssertFalse(result.Success);
        AssertEq(result.ErrorCode, "web.plugin-source-unavailable");
        AssertEq(vm.ExecutedIds.Count, 0);
    }

    private static WebScriptInventoryEntry Entry(
        string pName,
        int pOrder,
        bool enabled,
        WebScriptCompatibility compatibility,
        MemorySourceProvider pSource)
    {
        var descriptor = new EngineScriptDescriptor
        {
            Id = "plugin:" + pName,
            DisplayName = pName,
            LanguageId = pSource.LanguageId,
            RelativePath = "js/plugins/" + pName + ".js",
            Sha256 = new string('a', 64),
            Origin = ScriptOrigin.Plugin,
            Required = enabled,
            LoadOrder = pOrder,
        };
        pSource.Sources[descriptor.Id] = Encoding.UTF8.GetBytes("window." + pName + " = true;");
        return new WebScriptInventoryEntry
        {
            Script = descriptor,
            Enabled = enabled,
            Compatibility = compatibility,
        };
    }

    private sealed class MemorySourceProvider : IWebScriptSourceProvider
    {
        public string LanguageId { get; set; } = ScriptLanguageIds.RpgMakerMzJavaScript;
        public Dictionary<string, byte[]> Sources { get; } = new();
        public string FailId { get; init; } = "";

        public ScriptSourceResult Read(EngineScriptDescriptor pScript)
        {
            if (pScript.Id == FailId || !Sources.TryGetValue(pScript.Id, out var source))
            {
                return ScriptSourceResult.Failed("synthetic missing source");
            }
            return ScriptSourceResult.Succeeded(source);
        }
    }

    private sealed class RecordingVm : IEmbeddedScriptVm
    {
        public RecordingVm(string pLanguageId) => LanguageId = pLanguageId;

        public string LanguageId { get; }
        public ScriptVmState State { get; private set; } = ScriptVmState.Created;
        public ScriptExecutionPolicy Policy { get; private set; } = ScriptExecutionPolicy.SafeDefault;
        public List<string> LoadedIds { get; } = new();
        public List<string> ExecutedIds { get; } = new();

        public SdkOperationResult Configure(ScriptExecutionPolicy pPolicy)
        {
            Policy = pPolicy;
            State = ScriptVmState.Configured;
            return SdkOperationResult.Succeeded();
        }

        public SdkOperationResult LoadModule(ScriptModule pModule)
        {
            LoadedIds.Add(pModule.Descriptor.Id);
            State = ScriptVmState.Ready;
            return SdkOperationResult.Succeeded();
        }

        public SdkOperationResult ExecuteModule(string pScriptId)
        {
            ExecutedIds.Add(pScriptId);
            State = ScriptVmState.Running;
            return SdkOperationResult.Succeeded();
        }

        public SdkOperationResult Invoke(ScriptInvocation pInvocation) => SdkOperationResult.Succeeded();

        public SdkOperationResult Reset()
        {
            LoadedIds.Clear();
            ExecutedIds.Clear();
            State = ScriptVmState.Created;
            return SdkOperationResult.Succeeded();
        }

        public void Dispose() => State = ScriptVmState.Disposed;
    }
}
