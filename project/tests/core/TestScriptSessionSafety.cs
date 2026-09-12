using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UniversalRPG.Plugins;
using UniversalRPG.Rgss;
using UniversalRPG.Sdk;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The fake VM deliberately permits reuse after failures. These tests therefore
/// verify the engine pipeline guard, not a coincidental guard in one backend.
/// All five script-language pipelines must obey the same fail-stop lifecycle.
/// </summary>
public sealed class TestScriptSessionSafety : TestBase
{
    public void Test_PartialBootstrapNeverReplaysEarlierScripts()
    {
        for (var kind = 0; kind < 5; kind++)
        {
            using var f = Fixture.Create(kind);
            f.Vm.FailPhase = "execute";
            f.Vm.FailId = f.Runtime.Scripts[1].Id;
            AssertTrue(f.Runtime.LoadScripts(ScriptExecutionPolicy.SafeDefault).Success);
            AssertFalse(f.Runtime.ExecuteBootstrap().Success);
            AssertEq(string.Join(",", f.Vm.Executed), string.Join(",", f.Runtime.Scripts.Take(2).Select(s => s.Id)));
            f.Vm.FailPhase = "";
            AssertEq(f.Runtime.ExecuteBootstrap().ErrorCode, f.Prefix + ".session-faulted");
            AssertEq(f.Runtime.LoadScripts(ScriptExecutionPolicy.SafeDefault).ErrorCode, f.Prefix + ".session-faulted");
            AssertEq(f.Runtime.InvokeHook(Hook()).ErrorCode, f.Prefix + ".session-faulted");
            AssertEq(f.Vm.Executed.Count, 2);
            AssertEq(f.Vm.ConfigureCount, 1);
            AssertEq(f.Vm.InvokeCount, 0);
            AssertEq(f.Vm.ResetCount, 0, "failure must not silently reset and replay game initialization");
        }
    }

    public void Test_PartialLoadFailureCannotBeRetriedInSameSession()
    {
        for (var kind = 0; kind < 5; kind++)
        {
            using var f = Fixture.Create(kind);
            f.Vm.FailPhase = "load";
            f.Vm.FailId = f.Runtime.Scripts[1].Id;
            AssertFalse(f.Runtime.LoadScripts(ScriptExecutionPolicy.SafeDefault).Success);
            f.Vm.FailPhase = "";
            AssertEq(f.Runtime.LoadScripts(ScriptExecutionPolicy.SafeDefault).ErrorCode, f.Prefix + ".session-faulted");
            AssertEq(f.Runtime.ExecuteBootstrap().ErrorCode, f.Prefix + ".session-faulted");
            AssertEq(f.Vm.Loaded.Count, 2);
            AssertEq(f.Vm.Executed.Count, 0);
        }
    }

    public void Test_ConfigureFailureConsumesPotentiallyMutatedVm()
    {
        for (var kind = 0; kind < 5; kind++)
        {
            using var f = Fixture.Create(kind);
            f.Vm.FailPhase = "configure";
            AssertFalse(f.Runtime.LoadScripts(ScriptExecutionPolicy.SafeDefault).Success);
            f.Vm.FailPhase = "";
            AssertEq(f.Runtime.LoadScripts(ScriptExecutionPolicy.SafeDefault).ErrorCode, f.Prefix + ".session-faulted");
            AssertEq(f.Vm.ConfigureCount, 1);
        }
    }

    public void Test_HooksRequireCompletedBootstrap()
    {
        for (var kind = 0; kind < 5; kind++)
        {
            using var f = Fixture.Create(kind);
            AssertEq(f.Runtime.InvokeHook(Hook()).ErrorCode, f.Prefix + ".not-loaded");
            AssertTrue(f.Runtime.LoadScripts(ScriptExecutionPolicy.SafeDefault).Success);
            AssertEq(f.Runtime.InvokeHook(Hook()).ErrorCode, f.Prefix + ".not-bootstrapped");
            AssertEq(f.Vm.InvokeCount, 0);
            AssertTrue(f.Runtime.ExecuteBootstrap().Success);
            AssertTrue(f.Runtime.InvokeHook(Hook()).Success);
            AssertEq(f.Vm.InvokeCount, 1);
        }
    }

    public void Test_HookFailurePoisonsOnlyItsSession()
    {
        for (var kind = 0; kind < 5; kind++)
        {
            using var f = Fixture.Create(kind);
            AssertTrue(f.Runtime.LoadScripts(ScriptExecutionPolicy.SafeDefault).Success);
            AssertTrue(f.Runtime.ExecuteBootstrap().Success);
            f.Vm.FailPhase = "invoke";
            AssertFalse(f.Runtime.InvokeHook(Hook()).Success);
            f.Vm.FailPhase = "";
            AssertEq(f.Runtime.InvokeHook(Hook()).ErrorCode, f.Prefix + ".session-faulted");
            AssertEq(f.Vm.InvokeCount, 1);
            using var fresh = Fixture.Create(kind);
            AssertTrue(fresh.Runtime.LoadScripts(ScriptExecutionPolicy.SafeDefault).Success);
            AssertTrue(fresh.Runtime.ExecuteBootstrap().Success);
            AssertTrue(fresh.Runtime.InvokeHook(Hook()).Success);
        }
    }

    public void Test_OrdinaryVmExceptionsBecomeFailStopDiagnostics()
    {
        foreach (var phase in new[] { "configure", "load", "execute", "invoke" })
        {
            for (var kind = 0; kind < 5; kind++)
            {
                using var f = Fixture.Create(kind);
                f.Vm.FailPhase = phase;
                f.Vm.FailId = f.Runtime.Scripts[1].Id;
                f.Vm.ThrowInstead = true;
                var result = f.Runtime.LoadScripts(ScriptExecutionPolicy.SafeDefault);
                if (phase is "execute" or "invoke")
                {
                    AssertTrue(result.Success);
                    result = f.Runtime.ExecuteBootstrap();
                }
                if (phase == "invoke")
                {
                    AssertTrue(result.Success);
                    result = f.Runtime.InvokeHook(Hook());
                }
                AssertFalse(result.Success);
                AssertEq(result.ErrorCode, f.Prefix + ".external-exception");
                AssertTrue(result.ErrorMessage.Contains("synthetic", StringComparison.Ordinal));
                AssertEq(f.Runtime.ExecuteBootstrap().ErrorCode, f.Prefix + ".session-faulted");
            }
        }
    }

    public void Test_InvalidPolicyDoesNotConsumeUnconfiguredSession()
    {
        for (var kind = 0; kind < 5; kind++)
        {
            using var f = Fixture.Create(kind);
            AssertFalse(f.Runtime.LoadScripts(new ScriptExecutionPolicy { MaxMemoryMegabytes = 0 }).Success);
            AssertEq(f.Vm.ConfigureCount, 0);
            AssertTrue(f.Runtime.LoadScripts(ScriptExecutionPolicy.SafeDefault).Success);
            AssertTrue(f.Runtime.ExecuteBootstrap().Success);
        }
    }

    public void Test_ReentrantBootstrapCannotRunEarlierScriptsAgain()
    {
        for (var kind = 0; kind < 5; kind++)
        {
            using var f = Fixture.Create(kind);
            AssertTrue(f.Runtime.LoadScripts(ScriptExecutionPolicy.SafeDefault).Success);
            var attempts = 0;
            f.Vm.OnExecute = () =>
            {
                attempts++;
                var nested = f.Runtime.ExecuteBootstrap();
                if (nested.ErrorCode != f.Prefix + ".operation-in-progress")
                    throw new InvalidOperationException("reentrant bootstrap was not refused");
            };
            AssertTrue(f.Runtime.ExecuteBootstrap().Success);
            AssertEq(attempts, 3);
            AssertEq(f.Vm.Executed.Count, 3);
        }
    }

    public void Test_DoubleSuccessfulBootstrapDoesNotExecuteTwice()
    {
        for (var kind = 0; kind < 5; kind++)
        {
            using var f = Fixture.Create(kind);
            AssertTrue(f.Runtime.LoadScripts(ScriptExecutionPolicy.SafeDefault).Success);
            AssertTrue(f.Runtime.ExecuteBootstrap().Success);
            AssertEq(f.Runtime.ExecuteBootstrap().ErrorCode, f.Prefix + ".already-bootstrapped");
            AssertEq(f.Vm.Executed.Count, 3);
        }
    }

    public void Test_DisposalIsIdempotentAfterFailure()
    {
        for (var kind = 0; kind < 5; kind++)
        {
            var f = Fixture.Create(kind);
            f.Vm.FailPhase = "configure";
            AssertFalse(f.Runtime.LoadScripts(ScriptExecutionPolicy.SafeDefault).Success);
            f.Dispose(); f.Dispose();
            AssertEq(f.Vm.DisposeCount, 1);
            AssertEq(f.Runtime.LoadScripts(ScriptExecutionPolicy.SafeDefault).ErrorCode, f.Prefix + ".disposed");
        }
    }

    public void Test_WebSourceProviderThrowCannotLeaveRetryablePartialLoad()
    {
        for (var kind = 0; kind < 2; kind++)
        {
            using var f = Fixture.Create(kind);
            f.Source!.OnRead = script =>
            {
                if (script.Id == f.Runtime.Scripts[1].Id) throw new InvalidOperationException("synthetic provider error");
            };
            AssertEq(f.Runtime.LoadScripts(ScriptExecutionPolicy.SafeDefault).ErrorCode, "web.external-exception");
            AssertEq(f.Vm.Loaded.Count, 1);
            f.Source.OnRead = null;
            AssertEq(f.Runtime.LoadScripts(ScriptExecutionPolicy.SafeDefault).ErrorCode, "web.session-faulted");
            AssertEq(f.Vm.Loaded.Count, 1);
        }
    }

    public void Test_WebProviderReentryCannotRestartConfigure()
    {
        using var f = Fixture.Create(0);
        var attempts = 0;
        f.Source!.OnRead = _ =>
        {
            attempts++;
            var result = f.Runtime.LoadScripts(ScriptExecutionPolicy.SafeDefault);
            if (result.ErrorCode != "web.operation-in-progress") throw new InvalidOperationException("reentrant load allowed");
        };
        AssertTrue(f.Runtime.LoadScripts(ScriptExecutionPolicy.SafeDefault).Success);
        AssertEq(attempts, 3);
        AssertEq(f.Vm.ConfigureCount, 1);
    }

    public void Test_PreludeFailureCannotRunPluginsOrReplayPrelude()
    {
        var vm = new RecordingVm(ScriptLanguageIds.RpgMakerMvJavaScript) { FailPhase = "execute", FailId = "compat:test" };
        var prelude = new ScriptModule
        {
            Descriptor = Descriptor("compat:test", "Prelude", ScriptLanguageIds.RpgMakerMvJavaScript, 0),
            Source = Encoding.UTF8.GetBytes("// fixture"),
        };
        using var runtime = new WebScriptRuntime(vm.LanguageId, vm, new SourceProvider(),
            new[] { WebEntry(0, vm.LanguageId) }, new[] { prelude });
        AssertTrue(runtime.LoadScripts(ScriptExecutionPolicy.SafeDefault).Success);
        AssertEq(runtime.ExecuteBootstrap().ErrorCode, "web.prelude-execution-failed");
        AssertEq(runtime.ExecuteBootstrap().ErrorCode, "web.session-faulted");
        AssertEq(string.Join(",", vm.Executed), "compat:test");
    }

    public void Test_RgssDuplicateArchiveIndicesFailBeforeVmConfiguration()
    {
        var vm = new RecordingVm(ScriptLanguageIds.Rgss3Ruby);
        using var runtime = new RgssScriptRuntime(RgssGeneration.Rgss3, vm,
            new[]
            {
                RubyEntry(0, vm.LanguageId),
                new RgssScriptEntry
                {
                    ArchiveIndex = 0, Id = 2, Name = "DuplicateIndex", Source = Encoding.UTF8.GetBytes("nil"),
                    Descriptor = Descriptor("rgss-script:other", "DuplicateIndex", vm.LanguageId, 1),
                },
            });
        AssertEq(runtime.LoadScripts(ScriptExecutionPolicy.SafeDefault).ErrorCode, "rgss.invalid-archive-order");
        AssertEq(vm.ConfigureCount, 0);
    }

    public void Test_InvalidRgssGenerationIsNotSilentlyTreatedAsRgss3()
    {
        var rejected = false;
        using var vm = new RecordingVm(ScriptLanguageIds.Rgss3Ruby);
        try { using var ignored = new RgssScriptRuntime((RgssGeneration)999, vm, Array.Empty<RgssScriptEntry>()); }
        catch (ArgumentOutOfRangeException) { rejected = true; }
        AssertTrue(rejected);
    }

    public void Test_WebLoaderAndParameterTableUseSameFirstEnabledEntry()
    {
        var language = ScriptLanguageIds.RpgMakerMvJavaScript;
        var vm = new RecordingVm(language);
        using var runtime = new WebScriptRuntime(language, vm, new SourceProvider(), new[]
        {
            new WebScriptInventoryEntry
            {
                Enabled = true, Script = Descriptor("plugin:first", "Duplicate", language, 0),
                Parameters = new Dictionary<string, string> { ["Value"] = "first" },
            },
            new WebScriptInventoryEntry
            {
                Enabled = true, Script = Descriptor("plugin:later", "Duplicate", language, 1),
                Parameters = new Dictionary<string, string> { ["Value"] = "later" },
            },
        });
        AssertTrue(runtime.LoadScripts(ScriptExecutionPolicy.SafeDefault).Success);
        AssertTrue(runtime.ExecuteBootstrap().Success);
        AssertEq(string.Join(",", vm.Executed), "plugin:first");
        AssertEq(runtime.GetPluginParameters("DUPLICATE")["Value"], "first");
        AssertEq(runtime.Scripts.Count, 1);
    }

    private static ScriptHookRequest Hook() => new() { Hook = "probe" };
    private static EngineScriptDescriptor Descriptor(string id, string name, string language, int order) => new()
    {
        Id = id, DisplayName = name, LanguageId = language, RelativePath = "", LoadOrder = order,
    };
    private static WebScriptInventoryEntry WebEntry(int index, string language) => new()
    {
        Enabled = true, Script = Descriptor("plugin:" + index, "Plugin" + index, language, index),
        Compatibility = WebScriptCompatibility.StandardBrowserApi,
    };
    private static RgssScriptEntry RubyEntry(int index, string language) => new()
    {
        ArchiveIndex = index, Id = index + 1, Name = "Script" + index, Source = Encoding.UTF8.GetBytes("nil"),
        Descriptor = Descriptor("rgss-script:" + index, "Script" + index, language, index),
    };

    private sealed class Fixture : IDisposable
    {
        public required RecordingVm Vm { get; init; }
        public required IEngineScriptingRuntime Runtime { get; init; }
        public required string Prefix { get; init; }
        public SourceProvider? Source { get; init; }
        public void Dispose() => ((IDisposable)Runtime).Dispose();
        public static Fixture Create(int kind)
        {
            var language = kind switch
            {
                0 => ScriptLanguageIds.RpgMakerMvJavaScript, 1 => ScriptLanguageIds.RpgMakerMzJavaScript,
                2 => ScriptLanguageIds.Rgss1Ruby, 3 => ScriptLanguageIds.Rgss2Ruby, _ => ScriptLanguageIds.Rgss3Ruby,
            };
            var vm = new RecordingVm(language);
            if (kind < 2)
            {
                var source = new SourceProvider();
                return new Fixture
                {
                    Vm = vm, Source = source, Prefix = "web",
                    Runtime = new WebScriptRuntime(language, vm, source, Enumerable.Range(0, 3).Select(i => WebEntry(i, language))),
                };
            }
            return new Fixture
            {
                Vm = vm, Prefix = "rgss",
                Runtime = new RgssScriptRuntime((RgssGeneration)(kind - 2), vm, Enumerable.Range(0, 3).Select(i => RubyEntry(i, language))),
            };
        }
    }
    private sealed class SourceProvider : IWebScriptSourceProvider
    {
        public Action<EngineScriptDescriptor>? OnRead { get; set; }
        public ScriptSourceResult Read(EngineScriptDescriptor script)
        {
            OnRead?.Invoke(script);
            return ScriptSourceResult.Succeeded(Encoding.UTF8.GetBytes("// synthetic plugin"));
        }
    }
    private sealed class RecordingVm : IEmbeddedScriptVm
    {
        public RecordingVm(string language) => LanguageId = language;
        public string LanguageId { get; }
        public ScriptVmState State { get; private set; } = ScriptVmState.Created;
        public ScriptExecutionPolicy Policy { get; private set; } = ScriptExecutionPolicy.SafeDefault;
        public string FailPhase { get; set; } = "";
        public string FailId { get; set; } = "";
        public bool ThrowInstead { get; set; }
        public Action? OnExecute { get; set; }
        public int ConfigureCount { get; private set; }
        public int InvokeCount { get; private set; }
        public int ResetCount { get; private set; }
        public int DisposeCount { get; private set; }
        public List<string> Loaded { get; } = new();
        public List<string> Executed { get; } = new();
        public SdkOperationResult Configure(ScriptExecutionPolicy policy)
        {
            ConfigureCount++; Policy = policy; State = ScriptVmState.Configured;
            return Result("configure");
        }
        public SdkOperationResult LoadModule(ScriptModule module)
        {
            Loaded.Add(module.Descriptor.Id); State = ScriptVmState.Ready;
            return Result("load", module.Descriptor.Id);
        }
        public SdkOperationResult ExecuteModule(string id)
        {
            Executed.Add(id); State = ScriptVmState.Running; OnExecute?.Invoke();
            return Result("execute", id);
        }
        public SdkOperationResult Invoke(ScriptInvocation invocation) { InvokeCount++; return Result("invoke"); }
        public SdkOperationResult Reset() { ResetCount++; return SdkOperationResult.Succeeded(); }
        public void Dispose() { DisposeCount++; State = ScriptVmState.Disposed; }
        private SdkOperationResult Result(string phase, string id = "")
        {
            if (phase != FailPhase || (phase is "load" or "execute") && id != FailId)
                return SdkOperationResult.Succeeded();
            if (ThrowInstead) throw new InvalidOperationException("synthetic " + phase + " failure");
            return SdkOperationResult.Failed("fake." + phase, "synthetic failure");
        }
    }
}
