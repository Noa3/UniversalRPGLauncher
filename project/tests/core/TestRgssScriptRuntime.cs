using System;
using System.Collections.Generic;
using System.Text;
using UniversalRPG.Rgss;
using UniversalRPG.Sdk;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

public sealed class TestRgssScriptRuntime : TestBase
{
    public void Test_LoadAndBootstrapPreserveArchiveOrder()
    {
        var vm = new RecordingVm(ScriptLanguageIds.Rgss3Ruby);
        using var runtime = new RgssScriptRuntime(
            RgssGeneration.Rgss3,
            vm,
            new[]
            {
                Entry(2, 200, "Second", "SECOND", ScriptLanguageIds.Rgss3Ruby),
                Entry(0, 100, "First", "FIRST", ScriptLanguageIds.Rgss3Ruby),
                Entry(1, 150, "Patch", "PATCH", ScriptLanguageIds.Rgss3Ruby),
            });

        AssertTrue(runtime.DiscoverScripts().Success);
        AssertTrue(runtime.LoadScripts(ScriptExecutionPolicy.SafeDefault).Success);
        AssertEq(string.Join(",", vm.LoadedIds), "rgss-script:0,rgss-script:1,rgss-script:2");

        AssertTrue(runtime.ExecuteBootstrap().Success);
        AssertEq(string.Join(",", vm.ExecutedIds), "rgss-script:0,rgss-script:1,rgss-script:2");
        AssertEq(runtime.Scripts[0].DisplayName, "First");
        AssertEq(runtime.Scripts[1].DisplayName, "Patch");
        AssertEq(runtime.Scripts[2].DisplayName, "Second");
    }

    public void Test_RejectsVmWithWrongRubyCompatibilityLanguage()
    {
        var vm = new RecordingVm(ScriptLanguageIds.Rgss1Ruby);
        using var runtime = new RgssScriptRuntime(
            RgssGeneration.Rgss3,
            vm,
            new[] { Entry(0, 1, "Main", "nil", ScriptLanguageIds.Rgss3Ruby) });

        var result = runtime.LoadScripts(ScriptExecutionPolicy.SafeDefault);

        AssertFalse(result.Success);
        AssertEq(result.ErrorCode, "rgss.vm-language-mismatch");
        AssertEq(vm.LoadedIds.Count, 0);
    }

    public void Test_DoesNotExecuteBeforeExplicitBootstrap()
    {
        var vm = new RecordingVm(ScriptLanguageIds.Rgss1Ruby);
        using var runtime = new RgssScriptRuntime(
            RgssGeneration.Rgss1,
            vm,
            new[] { Entry(0, 1, "Main", "puts 'x'", ScriptLanguageIds.Rgss1Ruby) });

        AssertTrue(runtime.LoadScripts(ScriptExecutionPolicy.SafeDefault).Success);
        AssertEq(vm.ExecutedIds.Count, 0, "loading game-authored code does not silently execute it");
        AssertTrue(runtime.ExecuteBootstrap().Success);
        AssertEq(vm.ExecutedIds.Count, 1);
    }

    public void Test_StopsAtFirstScriptExecutionFailure()
    {
        var vm = new RecordingVm(ScriptLanguageIds.Rgss2Ruby)
        {
            FailExecuteId = "rgss-script:1",
        };
        using var runtime = new RgssScriptRuntime(
            RgssGeneration.Rgss2,
            vm,
            new[]
            {
                Entry(0, 1, "A", "a", ScriptLanguageIds.Rgss2Ruby),
                Entry(1, 2, "B", "b", ScriptLanguageIds.Rgss2Ruby),
                Entry(2, 3, "C", "c", ScriptLanguageIds.Rgss2Ruby),
            });

        AssertTrue(runtime.LoadScripts(ScriptExecutionPolicy.SafeDefault).Success);
        var result = runtime.ExecuteBootstrap();

        AssertFalse(result.Success);
        AssertEq(result.ErrorCode, "rgss.script-execution-failed");
        AssertEq(string.Join(",", vm.ExecutedIds), "rgss-script:0,rgss-script:1");
    }

    public void Test_HostHookIsExplicitAndBounded()
    {
        var vm = new RecordingVm(ScriptLanguageIds.Rgss3Ruby);
        using var runtime = new RgssScriptRuntime(
            RgssGeneration.Rgss3,
            vm,
            new[] { Entry(0, 1, "Main", "nil", ScriptLanguageIds.Rgss3Ruby) });
        AssertTrue(runtime.LoadScripts(ScriptExecutionPolicy.SafeDefault).Success);
        AssertTrue(runtime.ExecuteBootstrap().Success);

        var result = runtime.InvokeHook(new ScriptHookRequest
        {
            Hook = "debug_query",
            Arguments = new Dictionary<string, string>
            {
                ["b"] = "2",
                ["a"] = "1",
            },
        });

        AssertTrue(result.Success);
        AssertEq(vm.Invocations.Count, 1);
        AssertEq(vm.Invocations[0].Member, "debug_query");
        AssertEq(vm.Invocations[0].Arguments.Count, 2);
    }

    private static RgssScriptEntry Entry(
        int pIndex,
        int pId,
        string pName,
        string pSource,
        string pLanguageId)
    {
        var source = Encoding.UTF8.GetBytes(pSource);
        var relativePath = pLanguageId switch
        {
            ScriptLanguageIds.Rgss1Ruby => "Data/Scripts.rxdata",
            ScriptLanguageIds.Rgss2Ruby => "Data/Scripts.rvdata",
            _ => "Data/Scripts.rvdata2",
        };
        return new RgssScriptEntry
        {
            ArchiveIndex = pIndex,
            Id = pId,
            Name = pName,
            Source = source,
            Descriptor = new EngineScriptDescriptor
            {
                Id = $"rgss-script:{pIndex}",
                DisplayName = pName,
                LanguageId = pLanguageId,
                RelativePath = relativePath,
                Sha256 = new string('a', 64),
                Origin = ScriptOrigin.Game,
                Required = true,
                LoadOrder = pIndex,
            },
        };
    }

    private sealed class RecordingVm : IEmbeddedScriptVm
    {
        private readonly string _languageId;

        public RecordingVm(string pLanguageId)
        {
            _languageId = pLanguageId;
        }

        public string LanguageId => _languageId;
        public ScriptVmState State { get; private set; } = ScriptVmState.Created;
        public ScriptExecutionPolicy Policy { get; private set; } = ScriptExecutionPolicy.SafeDefault;
        public List<string> LoadedIds { get; } = new();
        public List<string> ExecutedIds { get; } = new();
        public List<ScriptInvocation> Invocations { get; } = new();
        public string FailExecuteId { get; init; } = "";

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
            if (pScriptId == FailExecuteId)
            {
                State = ScriptVmState.Faulted;
                return SdkOperationResult.Failed("fake.execute", "Synthetic script failure.");
            }
            State = ScriptVmState.Running;
            return SdkOperationResult.Succeeded();
        }

        public SdkOperationResult Invoke(ScriptInvocation pInvocation)
        {
            Invocations.Add(pInvocation);
            return SdkOperationResult.Succeeded();
        }

        public SdkOperationResult Reset()
        {
            LoadedIds.Clear();
            ExecutedIds.Clear();
            Invocations.Clear();
            State = ScriptVmState.Created;
            return SdkOperationResult.Succeeded();
        }

        public void Dispose() => State = ScriptVmState.Disposed;
    }
}
