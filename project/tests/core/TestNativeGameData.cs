using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UniversalRPG.JavaScript.Jint;
using UniversalRPG.Plugins;
using UniversalRPG.Sdk;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

public sealed class TestNativeGameData : TestBase
{
    public void Test_ReadsRealContentOnlyWhenFrameIsAdvanced()
    {
        var content = new Content(); content.Files["data/System.json"] = Encoding.UTF8.GetBytes("{\"value\":42}");
        using var fixture = Create(content, """
            let result=null; const xhr=new XMLHttpRequest();xhr.open('GET','data/System.json');
            xhr.onload=()=>{result=JSON.parse(xhr.responseText).value;};xhr.send();
            globalThis.UniversalRPG={verify(){if(result!==42)throw Error('data missing');}};
            """);
        Start(fixture);
        AssertEq(content.Reads.Count, 0);
        Pass(fixture.Runtime.AdvanceFrame(0));
        AssertEq(string.Join(",", content.Reads), "data/System.json");
        Verify(fixture);
    }

    public void Test_MzRequestsUseTheSameNativeContentBoundary()
    {
        var content = new Content();content.Files["data/System.json"] = Encoding.UTF8.GetBytes("{\"gameTitle\":\"MZ native\"}");
        using var fixture = Create(content, """
            let title='';const x=new XMLHttpRequest();x.open('GET','data/System.json');
            x.onload=()=>title=JSON.parse(x.responseText).gameTitle;x.send();
            globalThis.UniversalRPG={verify(){if(title!=='MZ native')throw Error('MZ data');}};
            """, mz: true);
        Start(fixture);Pass(fixture.Runtime.AdvanceFrame(0));Verify(fixture);
    }

    public void Test_ReadPolicyIsEnforcedAtNativeBoundary()
    {
        var content = new Content();
        using var fixture = Create(content, ErrorProbe("data/System.json", "data.read-denied"));
        Pass(fixture.Runtime.LoadScripts(new ScriptExecutionPolicy { AllowReadGameFiles = false }));
        Pass(fixture.Runtime.ExecuteBootstrap()); Pass(fixture.Runtime.AdvanceFrame(0)); Verify(fixture);
        AssertEq(content.Reads.Count, 0);
    }

    public void Test_UnsafePathsNeverReachContentProvider()
    {
        foreach (var path in new[] { "../secret.json", "file:///tmp/key.json", "https://example.org/a.json", "data/../key.json", "data/%2e%2e/key.json", "js/plugins/A.js" })
        {
            var content = new Content();
            using var fixture = Create(content, ErrorProbe(path, "data.path-denied"));
            Start(fixture); Pass(fixture.Runtime.AdvanceFrame(0)); Verify(fixture);
            AssertEq(content.Reads.Count, 0);
        }
    }

    public void Test_WwwPrefixNeverFallsBackToDifferentRoot()
    {
        var content = new Content();
        content.Files["data/System.json"] = Encoding.UTF8.GetBytes("{\"id\":1}");
        content.Files["www/data/System.json"] = Encoding.UTF8.GetBytes("{\"id\":2}");
        using var fixture = Create(content, """
            let id=0;const x=new XMLHttpRequest();x.open('GET','data/System.json');
            x.onload=()=>id=JSON.parse(x.responseText).id;x.send();
            globalThis.UniversalRPG={verify(){if(id!==2)throw Error('wrong root');}};
            """, "www");
        Start(fixture); Pass(fixture.Runtime.AdvanceFrame(0)); Verify(fixture);
        AssertEq(string.Join(",",content.Reads),"www/data/System.json");
    }

    public void Test_BomAndUnicodeArePreserved()
    {
        var content = new Content();
        content.Files["data/世界.json"] = new byte[] { 0xEF,0xBB,0xBF }.Concat(Encoding.UTF8.GetBytes("{\"name\":\"世界\"}")).ToArray();
        using var fixture = Create(content, """
            let name='';const x=new XMLHttpRequest();x.open('GET','data/%E4%B8%96%E7%95%8C.json?v=1');
            x.onload=()=>name=JSON.parse(x.responseText).name;x.send();
            globalThis.UniversalRPG={verify(){if(name!=='世界')throw Error('encoding');}};
            """);
        Start(fixture); Pass(fixture.Runtime.AdvanceFrame(0)); Verify(fixture);
        AssertEq(content.Reads[0],"data/世界.json");
    }

    public void Test_InvalidUtf8ReportsAnError()
    {
        var content=new Content();content.Files["data/A.json"]=new byte[]{0xC3,0x28};
        using var fixture=Create(content,ErrorProbe("data/A.json","data.invalid-utf8"));
        Start(fixture);Pass(fixture.Runtime.AdvanceFrame(0));Verify(fixture);
    }

    public void Test_OversizedFileFailsBeforeJsonTextConversion()
    {
        var content=new Content();content.Files["data/A.json"]=new byte[NativeGameDataSource.MaxFileBytes+1];
        using var fixture=Create(content,ErrorProbe("data/A.json","data.file-too-large"));
        Start(fixture);Pass(fixture.Runtime.AdvanceFrame(0));Verify(fixture);
    }

    public void Test_ProviderExceptionDoesNotLeakNativePath()
    {
        var content=new Content { Throw=true };
        using var fixture=Create(content,ErrorProbe("data/A.json","data.provider-failed"));
        Start(fixture);Pass(fixture.Runtime.AdvanceFrame(0));Verify(fixture);
    }

    public void Test_AbortCancelsNativeRead()
    {
        var content=new Content();
        using var fixture=Create(content,"const x=new XMLHttpRequest();x.open('GET','data/A.json');x.send();x.abort();");
        Start(fixture);Pass(fixture.Runtime.AdvanceFrame(0));AssertEq(content.Reads.Count,0);
    }

    public void Test_VmDoesNotOwnOrDisposeSharedMount()
    {
        var content=new Content();var fixture=Create(content,"");Start(fixture);fixture.Dispose();
        AssertFalse(content.Disposed);
        AssertEq(fixture.Runtime.AdvanceFrame(0).ErrorCode,"web.disposed");
    }

    public void Test_DefaultVmHasNoImplicitDataCapability()
    {
        using var vm=new JintEmbeddedScriptVm(ScriptLanguageIds.RpgMakerMvJavaScript);
        AssertFalse(vm.HasNativeGameData);Pass(vm.Configure(ScriptExecutionPolicy.SafeDefault));
        Pass(vm.LoadModule(Module("if(typeof XMLHttpRequest!=='undefined')throw Error('implicit data API');")));
        Pass(vm.ExecuteModule("plugin:data"));
    }

    public void Test_NativeProviderCannotResetVmDuringRead()
    {
        var content=new Content();content.Files["data/A.json"]=Encoding.UTF8.GetBytes("{}");
        using var fixture=Create(content,"const x=new XMLHttpRequest();x.open('GET','data/A.json');x.send();");
        SdkOperationResult? reset=null;content.OnRead=()=>reset=fixture.Vm.Reset();
        Start(fixture);Pass(fixture.Runtime.AdvanceFrame(0));
        AssertTrue(reset!=null);AssertEq(reset!.ErrorCode,"jint.operation-in-progress");
    }

    public void Test_LocalPathContractRejectsCrossPlatformEscapes()
    {
        var bad=new[] { "", "/data/A.json", "//host/data/A.json", "C:/data/A.json", "data\\A.json",
            "data/./A.json", "data/../A.json", "data/%252e%252e/A.json", "data/%00A.json", "data/%GG.json",
            " data/A.json", "data/A.json ", "data//A.json", "data/A.png", "data/A.json:stream", "data/%3fA.json", "data/con.json", "data/LPT1.json", "data/COM¹.json", "data/A.json." };
        foreach(var path in bad) AssertFalse(NativeGameDataSource.TryResolveDataPath(path,out _),path);
    }

    public void Test_LocalPathContractKeepsNestedPluginData()
    {
        AssertTrue(NativeGameDataSource.TryResolveDataPath("./data/Plugin/My%20Data.json?cache=123#x",out var path));
        AssertEq(path,"data/Plugin/My Data.json");
        AssertTrue(NativeGameDataSource.TryResolveDataPath("DATA/System.JSON",out path));
        AssertEq(path,"DATA/System.JSON");
    }

    private void Start(Fixture fixture)
    {
        Pass(fixture.Runtime.LoadScripts(new ScriptExecutionPolicy { MaxMemoryMegabytes=128,MaxExecutionMillisecondsPerTick=1000,MaxCallDepth=128 }));
        Pass(fixture.Runtime.ExecuteBootstrap());
    }
    private void Verify(Fixture fixture)=>Pass(fixture.Runtime.InvokeHook(new ScriptHookRequest{Hook="verify"}));
    private void Pass(SdkOperationResult result)
    {
        AssertTrue(result.Success,$"[{result.ErrorCode}] {result.ErrorMessage}");
        if(!result.Success)throw new InvalidOperationException(result.ErrorMessage);
    }
    private static string ErrorProbe(string path,string code) =>
        "let code='';const x=new XMLHttpRequest();x.open('GET',"+System.Text.Json.JsonSerializer.Serialize(path)+");"+
        "x.onload=()=>{throw Error('unexpected success');};x.onerror=()=>code=x.urpgErrorCode;x.send();"+
        "globalThis.UniversalRPG={verify(){if(code!=="+System.Text.Json.JsonSerializer.Serialize(code)+")throw Error('wrong error: '+code);}};";
    private static ScriptModule Module(string text, string language = ScriptLanguageIds.RpgMakerMvJavaScript)=>new()
    {
        Descriptor=new EngineScriptDescriptor{Id="plugin:data",DisplayName="Data",LanguageId=language,RelativePath="js/plugins/Data.js"},
        Source=Encoding.UTF8.GetBytes(text),
    };
    private static Fixture Create(Content content,string text,string prefix="",bool mz=false)
    {
        var language=mz?ScriptLanguageIds.RpgMakerMzJavaScript:ScriptLanguageIds.RpgMakerMvJavaScript;
        var vm=new JintEmbeddedScriptVm(language,content,prefix);
        var module=Module(text,language);
        var entry=new WebScriptInventoryEntry{Enabled=true,Script=module.Descriptor,Compatibility=WebScriptCompatibility.StandardBrowserApi};
        var runtime=new WebScriptRuntime(vm.LanguageId,vm,new Scripts(module.Source),new[]{entry},null,new WebBrowserHostOptions());
        return new Fixture(vm,runtime);
    }
    private sealed record Fixture(JintEmbeddedScriptVm Vm,WebScriptRuntime Runtime):IDisposable
    { public void Dispose()=>Runtime.Dispose(); }
    private sealed class Scripts(ReadOnlyMemory<byte> bytes):IWebScriptSourceProvider
    { public ScriptSourceResult Read(EngineScriptDescriptor script)=>ScriptSourceResult.Succeeded(bytes); }
    private sealed class Content:IGameContentSource
    {
        public readonly Dictionary<string,byte[]> Files=new(StringComparer.OrdinalIgnoreCase);
        public readonly List<string> Reads=new();
        public bool Throw;public bool Disposed;public Action? OnRead;
        public string SourceId=>"memory";
        public GameContentProtectionKind Protection=>GameContentProtectionKind.None;
        public bool Exists(string path)=>Files.ContainsKey(path);
        public ContentReadResult Read(string path)
        {
            Reads.Add(path);OnRead?.Invoke();
            if(Throw)throw new System.IO.IOException("/private/host/SECRET");
            return Files.TryGetValue(path,out var bytes)?ContentReadResult.Succeeded(bytes):ContentReadResult.Failed("content.missing","missing");
        }
        public void Dispose()=>Disposed=true;
    }
}
