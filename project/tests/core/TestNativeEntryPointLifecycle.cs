using System;
using System.Collections.Generic;
using System.Text;
using UniversalRPG.JavaScript.Jint;
using UniversalRPG.Plugins;
using UniversalRPG.Sdk;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

public sealed class TestNativeEntryPointLifecycle : TestBase
{
    private const string Mv = ScriptLanguageIds.RpgMakerMvJavaScript;
    private const string Mz = ScriptLanguageIds.RpgMakerMzJavaScript;

    public void Test_MvEntryPointIsDeferredUntilExplicitExecution()
    {
        using var fixture = Create(Mv, "globalThis.started=(globalThis.started||0)+1;window.onload=function(){globalThis.loaded=true;};");
        Pass(fixture.Runtime.LoadScripts(Policy()));
        Pass(fixture.Runtime.ExecuteBootstrap());
        Pass(Hook(fixture.Runtime, "CheckBefore"));
        Pass(fixture.Runtime.ExecuteEntryPoint());
        AssertTrue(fixture.Runtime.EntryPointExecuted);
        Pass(Hook(fixture.Runtime, "CheckAfterEntry"));
        Pass(fixture.Runtime.DispatchWindowLoad());
        AssertTrue(fixture.Runtime.WindowLoadDispatched);
        Pass(Hook(fixture.Runtime, "CheckAfterLoad"));
    }

    public void Test_MzPreloadedScriptCallbacksArePumpedBeforeWindowLoad()
    {
        const string main = """
            globalThis.scheduled=0;globalThis.loadedEvent=false;
            for(const src of ['js/rmmz_core.js','js/rmmz_managers.js']){
                const s=document.createElement('script');s.src=src;s.onload=()=>scheduled++;document.body.appendChild(s);
            }
            window.addEventListener('load',()=>{if(scheduled!==2)throw Error('scripts not ready');loadedEvent=true;});
            """;
        using var fixture = Create(Mz, main, new[] { "js/rmmz_core.js", "js/rmmz_managers.js" });
        Pass(fixture.Runtime.LoadScripts(Policy())); Pass(fixture.Runtime.ExecuteBootstrap()); Pass(fixture.Runtime.ExecuteEntryPoint());
        Pass(Hook(fixture.Runtime,"BeforePump"));
        Pass(fixture.Runtime.AdvanceFrame(0));
        Pass(fixture.Runtime.DispatchWindowLoad());
        Pass(Hook(fixture.Runtime,"AfterLoad"));
    }

    public void Test_UnknownDynamicScriptDoesNotPretendToLoad()
    {
        const string main = "const s=document.createElement('script');s.src='js/plugins/Dynamic.js';s.onerror=()=>globalThis.failed=true;document.body.appendChild(s);";
        using var fixture = Create(Mz, main, new[] { "js/rmmz_core.js" });
        StartToEntry(fixture);
        Pass(fixture.Runtime.AdvanceFrame(0));
        Pass(Hook(fixture.Runtime,"CheckDynamicFailure"));
    }

    public void Test_EntryFailurePoisonsSessionAndCannotReplay()
    {
        using var fixture = Create(Mv, "globalThis.count=(globalThis.count||0)+1;throw Error('main failed');");
        Pass(fixture.Runtime.LoadScripts(Policy())); Pass(fixture.Runtime.ExecuteBootstrap());
        var result=fixture.Runtime.ExecuteEntryPoint();
        AssertFalse(result.Success);AssertTrue(result.ErrorMessage.Contains("main failed",StringComparison.Ordinal));
        AssertEq(fixture.Runtime.ExecuteEntryPoint().ErrorCode,"web.session-faulted");
    }

    public void Test_LoadDispatchBeforeEntryIsRejectedWithoutPoisoning()
    {
        using var fixture=Create(Mv,"window.onload=()=>{};");
        Pass(fixture.Runtime.LoadScripts(Policy()));Pass(fixture.Runtime.ExecuteBootstrap());
        AssertEq(fixture.Runtime.DispatchWindowLoad().ErrorCode,"web.entry-not-executed");
        Pass(fixture.Runtime.ExecuteEntryPoint());Pass(fixture.Runtime.DispatchWindowLoad());
    }

    public void Test_EntryPointCurrentScriptIsScopedAndCleared()
    {
        using var fixture=Create(Mv,"if(!document.currentScript.src.endsWith('/js/main.js'))throw Error('missing main metadata');globalThis.afterMain=()=>document.currentScript===null;");
        StartToEntry(fixture);Pass(Hook(fixture.Runtime,"CheckScope"));
    }

    private Fixture Create(string language,string main,IReadOnlyList<string>? known=null)
    {
        known ??= language==Mz ? new[] { "js/rmmz_core.js" } : new[] { "js/rpg_core.js" };
        var entry=new ScriptModule
        {
            Descriptor=new EngineScriptDescriptor { Id="entry:main",DisplayName="js/main.js",LanguageId=language,RelativePath="js/main.js",Origin=ScriptOrigin.Game,Required=true,LoadOrder=int.MaxValue },
            Source=Encoding.UTF8.GetBytes(main)
        };
        var hooks = language==Mz ? """
            globalThis.UniversalRPG={BeforePump(){if(scheduled!==0)throw Error('early')},AfterLoad(){if(!loadedEvent)throw Error('load')},CheckDynamicFailure(){if(!failed)throw Error('dynamic script fake success')},CheckScope(){if(!afterMain())throw Error('scope')}};
            """ : """
            globalThis.UniversalRPG={CheckBefore(){if(typeof started!=='undefined')throw Error('early entry')},CheckAfterEntry(){if(started!==1||loaded===true)throw Error('entry state')},CheckAfterLoad(){if(!loaded)throw Error('load missing')},CheckScope(){if(!afterMain())throw Error('scope')}};
            """;
        var preludes=new[]
        {
            NativeEntryPointHostPrelude.Build(language,known),
            Module(language,"core:hooks","js/test_hooks.js",hooks)
        };
        var vm=new JintEmbeddedScriptVm(language);
        var runtime=new WebScriptRuntime(language,vm,new EmptySource(),Array.Empty<WebScriptInventoryEntry>(),preludes,new WebBrowserHostOptions(),entry);
        return new Fixture(runtime);
    }

    private void StartToEntry(Fixture fixture){Pass(fixture.Runtime.LoadScripts(Policy()));Pass(fixture.Runtime.ExecuteBootstrap());Pass(fixture.Runtime.ExecuteEntryPoint());}
    private static ScriptExecutionPolicy Policy()=>new(){MaxMemoryMegabytes=64,MaxExecutionMillisecondsPerTick=250,MaxCallDepth=128};
    private static SdkOperationResult Hook(WebScriptRuntime runtime,string name)=>runtime.InvokeHook(new ScriptHookRequest{Hook=name});
    private void Pass(SdkOperationResult result){AssertTrue(result.Success,$"[{result.ErrorCode}] {result.ErrorMessage}");if(!result.Success)throw new InvalidOperationException(result.ErrorMessage);}
    private static ScriptModule Module(string language,string id,string path,string source)=>new(){Descriptor=new EngineScriptDescriptor{Id=id,DisplayName=id,LanguageId=language,RelativePath=path,Origin=ScriptOrigin.Game},Source=Encoding.UTF8.GetBytes(source)};
    private sealed class EmptySource:IWebScriptSourceProvider{public ScriptSourceResult Read(EngineScriptDescriptor script)=>ScriptSourceResult.Failed("unexpected plugin read");}
    private sealed class Fixture:IDisposable{public Fixture(WebScriptRuntime runtime)=>Runtime=runtime;public WebScriptRuntime Runtime{get;}public void Dispose()=>Runtime.Dispose();}
}
