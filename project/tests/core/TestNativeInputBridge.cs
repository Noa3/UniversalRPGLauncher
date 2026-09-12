using System;
using System.Text;
using UniversalRPG.JavaScript.Jint;
using UniversalRPG.Plugins;
using UniversalRPG.Sdk;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

public sealed class TestNativeInputBridge : TestBase
{
    public void Test_OriginalStyleInputListenerReceivesKeyState()
    {
        using var runtime=Runtime("""
            globalThis.keys={};
            document.addEventListener('keydown',e=>{if([37,38,39,40].includes(e.keyCode))e.preventDefault();keys[e.keyCode]=true;});
            document.addEventListener('keyup',e=>keys[e.keyCode]=false);
            addEventListener('blur',()=>{keys={};});
            globalThis.UniversalRPG={CheckDown(){if(keys[37]!==true)throw Error('left not down')},CheckUp(){if(keys[37]!==false)throw Error('left not up')},CheckBlur(){if(Object.keys(keys).length)throw Error('focus not cleared')}};
            """);
        Start(runtime);
        Pass(runtime.DispatchKeyEvent(37,true));Pass(Hook(runtime,"CheckDown"));
        Pass(runtime.DispatchKeyEvent(37,false));Pass(Hook(runtime,"CheckUp"));
        Pass(runtime.DispatchKeyEvent(13,true));Pass(runtime.DispatchFocusLost());Pass(Hook(runtime,"CheckBlur"));
    }

    public void Test_InputBeforeWindowLoadIsRejectedWithoutPoisoning()
    {
        using var runtime=Runtime("globalThis.UniversalRPG={Ok(){}};");
        Pass(runtime.LoadScripts(Policy()));Pass(runtime.ExecuteBootstrap());Pass(runtime.ExecuteEntryPoint());
        AssertEq(runtime.DispatchKeyEvent(13,true).ErrorCode,"web.input-before-startup");
        Pass(runtime.DispatchWindowLoad());Pass(runtime.DispatchKeyEvent(13,true));Pass(Hook(runtime,"Ok"));
    }

    public void Test_InvalidHostKeyCodeDoesNotEnterVm()
    {
        using var runtime=Runtime("globalThis.UniversalRPG={Ok(){}};");Start(runtime);
        AssertEq(runtime.DispatchKeyEvent(-1,true).ErrorCode,"web.key-code-invalid");
        AssertEq(runtime.DispatchKeyEvent(65536,true).ErrorCode,"web.key-code-invalid");
        Pass(Hook(runtime,"Ok"));
    }

    private WebScriptRuntime Runtime(string source)
    {
        var entry=new ScriptModule{Descriptor=new EngineScriptDescriptor{Id="entry:main",DisplayName="main",LanguageId=ScriptLanguageIds.RpgMakerMvJavaScript,RelativePath="js/main.js",Origin=ScriptOrigin.Game},Source=Encoding.UTF8.GetBytes(source)};
        var prelude=NativeEntryPointHostPrelude.Build(ScriptLanguageIds.RpgMakerMvJavaScript,new[]{"js/rpg_core.js"});
        return new WebScriptRuntime(ScriptLanguageIds.RpgMakerMvJavaScript,new JintEmbeddedScriptVm(ScriptLanguageIds.RpgMakerMvJavaScript),new EmptySource(),Array.Empty<WebScriptInventoryEntry>(),new[]{prelude},new WebBrowserHostOptions(),entry);
    }
    private void Start(WebScriptRuntime runtime){Pass(runtime.LoadScripts(Policy()));Pass(runtime.ExecuteBootstrap());Pass(runtime.ExecuteEntryPoint());Pass(runtime.DispatchWindowLoad());}
    private static ScriptExecutionPolicy Policy()=>new(){MaxMemoryMegabytes=64,MaxExecutionMillisecondsPerTick=250,MaxCallDepth=128};
    private static SdkOperationResult Hook(WebScriptRuntime runtime,string hook)=>runtime.InvokeHook(new ScriptHookRequest{Hook=hook});
    private void Pass(SdkOperationResult result){AssertTrue(result.Success,result.ErrorMessage);if(!result.Success)throw new InvalidOperationException(result.ErrorMessage);}
    private sealed class EmptySource:IWebScriptSourceProvider{public ScriptSourceResult Read(EngineScriptDescriptor script)=>ScriptSourceResult.Failed("unused");}
}
