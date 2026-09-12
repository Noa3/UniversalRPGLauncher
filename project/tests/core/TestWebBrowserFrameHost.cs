using System;
using System.Collections.Generic;
using System.Text;
using UniversalRPG.JavaScript.Jint;
using UniversalRPG.Plugins;
using UniversalRPG.Sdk;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>Real Jint integration: no substituted browser/timer implementation.</summary>
public sealed class TestWebBrowserFrameHost : TestBase
{
    public void Test_MvPluginTimersRunAfterExplicitBootstrapAndFrame()
    {
        using var fixture = Create(false, """
            const settings = PluginManager.parameters('Timing');
            let calls = 0;
            setTimeout(function(value) {
                if (this !== window || value !== 'ok') throw Error('timer calling convention');
                calls++;
            }, Number(settings.Delay), 'ok');
            globalThis.UniversalRPG = {
                Before() { if(calls !== 0 || performance.now() !== 0) throw Error('early execution'); },
                After() { if(calls !== 1 || performance.now() !== 10) throw Error('timer did not run once'); }
            };
            """);
        Pass(fixture.Runtime.LoadScripts(Policy()));
        AssertEq(fixture.Vm.State, ScriptVmState.Ready);
        Pass(fixture.Runtime.ExecuteBootstrap());
        Pass(Hook(fixture.Runtime, "Before"));
        Pass(fixture.Runtime.AdvanceFrame(0.010));
        Pass(Hook(fixture.Runtime, "After"));
    }

    public void Test_MzCommandRegistersBeforeTimerCallsIt()
    {
        using var fixture = Create(true, """
            const owner = { count: 0 };
            PluginManager.registerCommand('Timing', 'Add', function(args) { this.count += Number(args.n); });
            setTimeout(() => PluginManager.callCommand(owner, 'Timing', 'Add', {n:'2'}), 0);
            globalThis.UniversalRPG = { Verify() { if(owner.count !== 2) throw Error('MZ callback'); } };
            """);
        Start(fixture);
        Pass(fixture.Runtime.AdvanceFrame(0));
        Pass(Hook(fixture.Runtime, "Verify"));
    }

    public void Test_AnimationLoopRunsOncePerHostFrame()
    {
        using var fixture = Create(false, """
            const seen = [];
            function animate(time) { seen.push(time); requestAnimationFrame(animate); }
            requestAnimationFrame(animate);
            globalThis.UniversalRPG = { Verify() { if(JSON.stringify(seen) !== '[16,32]') throw Error('frame loop'); } };
            """);
        Start(fixture);
        Pass(fixture.Runtime.AdvanceFrame(0.016));
        Pass(fixture.Runtime.AdvanceFrame(0.016));
        Pass(Hook(fixture.Runtime, "Verify"));
    }

    public void Test_CurrentScriptIsScopedAndUriEscaped()
    {
        using var fixture = Create(true, """
            if (document.currentScript.src !== 'urpg://game/js/plugins/Timing%20Test.js') throw Error('script source');
            let callbackSawNull = false;
            setTimeout(() => { callbackSawNull = document.currentScript === null; }, 0);
            globalThis.UniversalRPG = { Verify() {
                if(document.currentScript !== null || !callbackSawNull) throw Error('stale script scope');
            } };
            """, path: "js\\plugins\\Timing Test.js");
        Start(fixture);
        Pass(fixture.Runtime.AdvanceFrame(0));
        Pass(Hook(fixture.Runtime, "Verify"));
    }

    public void Test_InvalidHostFrameRequestDoesNotPoisonSession()
    {
        using var fixture = Create(false, "globalThis.UniversalRPG = { Verify(){} };");
        Start(fixture);
        foreach (var delta in new[] { double.NaN, double.PositiveInfinity, -1.0, 1.001 })
        {
            var result = fixture.Runtime.AdvanceFrame(delta);
            AssertFalse(result.Success);
            AssertEq(result.ErrorCode, "web.invalid-frame-delta");
        }
        Pass(fixture.Runtime.AdvanceFrame(0.016));
        Pass(Hook(fixture.Runtime, "Verify"));
    }

    public void Test_FrameCannotRunBeforeLoadOrBootstrap()
    {
        using var fixture = Create(false, "");
        AssertEq(fixture.Runtime.AdvanceFrame(0).ErrorCode, "web.not-loaded");
        Pass(fixture.Runtime.LoadScripts(Policy()));
        AssertEq(fixture.Runtime.AdvanceFrame(0).ErrorCode, "web.not-bootstrapped");
        Pass(fixture.Runtime.ExecuteBootstrap());
        Pass(fixture.Runtime.AdvanceFrame(0));
    }

    public void Test_BrowserHostIsExplicitlyOptIn()
    {
        using var fixture = Create(false, "", enableHost: false);
        AssertFalse(fixture.Runtime.HasBrowserFrameHost);
        AssertEq(fixture.Runtime.AdvanceFrame(0).ErrorCode, "web.frame-host-unavailable");
    }

    public void Test_FrameFailurePermanentlyStopsRuntime()
    {
        using var fixture = Create(false, "setTimeout(() => { throw Error('callback failed'); },0);");
        Start(fixture);
        var result = fixture.Runtime.AdvanceFrame(0);
        AssertFalse(result.Success);
        AssertEq(result.ErrorCode, "web.frame-execution-failed");
        AssertTrue(result.ErrorMessage.Contains("callback failed", StringComparison.Ordinal));
        AssertEq(fixture.Runtime.AdvanceFrame(0).ErrorCode, "web.session-faulted");
        AssertEq(fixture.Runtime.ExecuteBootstrap().ErrorCode, "web.session-faulted");
        AssertEq(Hook(fixture.Runtime, "Anything").ErrorCode, "web.session-faulted");
    }

    public void Test_CallbackBudgetIsSharedByTimersAndAnimationFrames()
    {
        using var fixture = Create(false, "setTimeout(()=>{},0);requestAnimationFrame(()=>{});",
            options: new WebBrowserHostOptions { MaxCallbacksPerFrame = 1 });
        Start(fixture);
        var result = fixture.Runtime.AdvanceFrame(0);
        AssertFalse(result.Success);
        AssertTrue(result.ErrorMessage.Contains("callback-budget", StringComparison.Ordinal));
        AssertEq(fixture.Runtime.AdvanceFrame(0).ErrorCode, "web.session-faulted");
    }

    public void Test_InfiniteTimerCallbackIsConstrainedByRealJint()
    {
        using var fixture = Create(false, "setTimeout(()=>{while(true){}},0);");
        Start(fixture);
        var result = fixture.Runtime.AdvanceFrame(0);
        AssertFalse(result.Success);
        AssertTrue(result.ErrorMessage.Contains("jint.timeout", StringComparison.Ordinal)
            || result.ErrorMessage.Contains("jint.statement-limit", StringComparison.Ordinal));
        AssertEq(fixture.Vm.State, ScriptVmState.Faulted);
    }

    public void Test_StringTimerDoesNotAcquireDynamicEvaluationPermission()
    {
        using var fixture = Create(false, "setTimeout('globalThis.something = true',0);");
        Pass(fixture.Runtime.LoadScripts(Policy()));
        var result = fixture.Runtime.ExecuteBootstrap();
        AssertFalse(result.Success);
        AssertTrue(result.ErrorMessage.Contains("function-callback-required", StringComparison.Ordinal));
        AssertEq(fixture.Runtime.AdvanceFrame(0).ErrorCode, "web.session-faulted");
    }

    public void Test_PausingOneSessionDoesNotStopAnother()
    {
        using var a = Create(false, "let n=0;setTimeout(()=>n++,0);globalThis.UniversalRPG={Verify(){if(n!==1)throw Error('timer');}};");
        using var b = Create(false, "let n=0;setTimeout(()=>n++,0);globalThis.UniversalRPG={Verify(){if(n!==0 || performance.now()!==0)throw Error('cross-session');}};");
        Start(a); Start(b);
        Pass(a.Runtime.AdvanceFrame(0.020));
        Pass(Hook(a.Runtime, "Verify"));
        Pass(Hook(b.Runtime, "Verify"));
    }

    public void Test_DisposedSessionRejectsFrames()
    {
        var fixture = Create(false, "");
        Start(fixture);
        fixture.Dispose();
        AssertEq(fixture.Runtime.AdvanceFrame(0).ErrorCode, "web.disposed");
    }

    public void Test_InvalidOptionsAreRejectedBeforeExecution()
    {
        AssertFalse(new WebBrowserHostOptions { MaxCallbacksPerFrame = 0 }.Validate().Success);
        AssertFalse(new WebBrowserHostOptions { MaxPendingTimers = 4097 }.Validate().Success);
        AssertFalse(new WebBrowserHostOptions { MaxTimerArguments = -1 }.Validate().Success);
        AssertFalse(new WebBrowserHostOptions { MaxFrameMilliseconds = 60001 }.Validate().Success);
        AssertTrue(new WebBrowserHostOptions().Validate().Success);
    }

    public void Test_BrowserPreludeCountsAgainstTotalPreludeLimit()
    {
        using var vm = new JintEmbeddedScriptVm(ScriptLanguageIds.RpgMakerMvJavaScript);
        var preludes = new ScriptModule[WebScriptRuntime.MaxPreludeModules];
        for(var i=0;i<preludes.Length;i++) preludes[i]=WebBrowserHostPrelude.Build(vm.LanguageId,new WebBrowserHostOptions());
        var rejected = false;
        try
        {
            using var runtime = new WebScriptRuntime(vm.LanguageId, vm, new Source(""),
                Array.Empty<WebScriptInventoryEntry>(), preludes, new WebBrowserHostOptions());
        }
        catch(ArgumentException) { rejected = true; }
        AssertTrue(rejected);
    }

    private void Start(Fixture fixture)
    {
        Pass(fixture.Runtime.LoadScripts(Policy()));
        Pass(fixture.Runtime.ExecuteBootstrap());
    }

    private void Pass(SdkOperationResult result)
    {
        AssertTrue(result.Success, $"[{result.ErrorCode}] {result.ErrorMessage}");
        if (!result.Success) throw new InvalidOperationException(result.ErrorMessage);
    }

    private static SdkOperationResult Hook(WebScriptRuntime runtime, string hook)
        => runtime.InvokeHook(new ScriptHookRequest { Hook = hook });

    private static ScriptExecutionPolicy Policy() => new()
    {
        MaxMemoryMegabytes = 64, MaxExecutionMillisecondsPerTick = 250, MaxCallDepth = 128,
    };

    private static Fixture Create(bool mz, string source, WebBrowserHostOptions? options = null,
        bool enableHost = true, string path = "js/plugins/Timing.js")
    {
        var language = mz ? ScriptLanguageIds.RpgMakerMzJavaScript : ScriptLanguageIds.RpgMakerMvJavaScript;
        var entry = new WebScriptInventoryEntry
        {
            Script = new EngineScriptDescriptor
            {
                Id = "plugin:timing", DisplayName = "Timing", LanguageId = language,
                RelativePath = path, Origin = ScriptOrigin.Plugin, LoadOrder = 0,
            },
            Enabled = true, Parameters = new Dictionary<string,string> { ["Delay"] = "10" },
            Compatibility = WebScriptCompatibility.StandardBrowserApi,
        };
        var vm = new JintEmbeddedScriptVm(language);
        var runtime = new WebScriptRuntime(language, vm, new Source(source), new[] { entry },
            new[] { WebPluginManagerShimBuilder.Build(language,new[] { entry }) },
            enableHost ? options ?? new WebBrowserHostOptions() : null);
        return new Fixture(vm, runtime);
    }

    private sealed class Source : IWebScriptSourceProvider
    {
        private readonly byte[] _source;
        public Source(string source) => _source = Encoding.UTF8.GetBytes(source);
        public ScriptSourceResult Read(EngineScriptDescriptor script) => ScriptSourceResult.Succeeded(_source);
    }

    private sealed class Fixture : IDisposable
    {
        public Fixture(IEmbeddedScriptVm vm, WebScriptRuntime runtime) { Vm=vm; Runtime=runtime; }
        public IEmbeddedScriptVm Vm { get; }
        public WebScriptRuntime Runtime { get; }
        public void Dispose() => Runtime.Dispose();
    }
}
