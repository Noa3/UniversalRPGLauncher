using System;
using System.Text;
using UniversalRPG.JavaScript.Jint;
using UniversalRPG.Plugins;
using UniversalRPG.Sdk;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

public sealed class TestNativeGamepadBridge : TestBase
{
    public void Test_OriginalStylePollReadsButtonAndAxisState()
    {
        using var runtime=Runtime("""
            globalThis.UniversalRPG={Check(){const p=navigator.getGamepads()[0];if(!p||!p.buttons[0].pressed||p.axes[0]!==-0.5)throw Error('gamepad state');}};
            """);Start(runtime);
        Pass(runtime.DispatchGamepadButton(0,0,true));Pass(runtime.DispatchGamepadAxis(0,0,-0.5));Pass(Hook(runtime,"Check"));
    }

    public void Test_SnapshotsAndDisconnectFollowStandardShape()
    {
        using var runtime=Runtime("""
            globalThis.first=null;globalThis.UniversalRPG={Capture(){first=navigator.getGamepads()[1];},CheckOld(){if(!first.buttons[1].pressed)throw Error('snapshot mutated');},CheckGone(){if(navigator.getGamepads()[1]!==undefined)throw Error('device retained');}};
            """);Start(runtime);
        Pass(runtime.DispatchGamepadButton(1,1,true));Pass(Hook(runtime,"Capture"));
        Pass(runtime.DispatchGamepadButton(1,1,false));Pass(Hook(runtime,"CheckOld"));
        Pass(runtime.DispatchGamepadDisconnected(1));Pass(Hook(runtime,"CheckGone"));
    }

    public void Test_InvalidHostStateIsRejectedWithoutEnteringVm()
    {
        using var runtime=Runtime("globalThis.UniversalRPG={Ok(){}};");Start(runtime);
        AssertEq(runtime.DispatchGamepadButton(-1,0,true).ErrorCode,"web.gamepad-button-invalid");
        AssertEq(runtime.DispatchGamepadAxis(0,4,0).ErrorCode,"web.gamepad-axis-invalid");
        AssertEq(runtime.DispatchGamepadButton(0,0,true,2).ErrorCode,"web.gamepad-value-invalid");
        Pass(Hook(runtime,"Ok"));
    }

    private static WebScriptRuntime Runtime(string script)
    {
        var language=ScriptLanguageIds.RpgMakerMvJavaScript;
        var entry=new ScriptModule{Descriptor=new EngineScriptDescriptor{Id="entry:main",DisplayName="main",LanguageId=language,RelativePath="js/main.js",Origin=ScriptOrigin.Game},Source=Encoding.UTF8.GetBytes(script)};
        var preludes=new[]{NativeEntryPointHostPrelude.Build(language,new[]{"js/rpg_core.js"}),NativeGamepadHostPrelude.Build(language)};
        return new WebScriptRuntime(language,new JintEmbeddedScriptVm(language),new EmptySource(),Array.Empty<WebScriptInventoryEntry>(),preludes,new WebBrowserHostOptions(),entry);
    }
    private static void Start(WebScriptRuntime runtime){PassStatic(runtime.LoadScripts(Policy()));PassStatic(runtime.ExecuteBootstrap());PassStatic(runtime.ExecuteEntryPoint());PassStatic(runtime.DispatchWindowLoad());}
    private static ScriptExecutionPolicy Policy()=>new(){MaxMemoryMegabytes=64,MaxExecutionMillisecondsPerTick=250,MaxCallDepth=128};
    private static SdkOperationResult Hook(WebScriptRuntime runtime,string hook)=>runtime.InvokeHook(new ScriptHookRequest{Hook=hook});
    private void Pass(SdkOperationResult result)=>PassStatic(result);
    private static void PassStatic(SdkOperationResult result){if(!result.Success)throw new InvalidOperationException($"[{result.ErrorCode}] {result.ErrorMessage}");}
    private sealed class EmptySource:IWebScriptSourceProvider{public ScriptSourceResult Read(EngineScriptDescriptor script)=>ScriptSourceResult.Failed("unused");}
}
