using System;
using System.Text;
using UniversalRPG.JavaScript.Jint;
using UniversalRPG.Sdk;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

public sealed class TestNativeScriptStorage : TestBase
{
    public void Test_StoragePersistsAcrossFreshVmWithSameHostStore()
    {
        using var store=new MemoryScriptKeyValueStorage();
        using(var first=Vm(store))
        {
            Pass(first.Configure(Policy()));
            Pass(first.LoadModule(Module("localStorage.setItem('RPG File1','payload');")));
            Pass(first.ExecuteModule("storage:test"));
        }
        using(var second=Vm(store))
        {
            Pass(second.Configure(Policy()));
            Pass(second.LoadModule(Module("if(localStorage.getItem('RPG File1')!=='payload')throw Error('missing save');")));
            Pass(second.ExecuteModule("storage:test"));
        }
    }

    public void Test_WebStorageSurfaceSupportsBackupStyleKeys()
    {
        using var store=new MemoryScriptKeyValueStorage();
        using var vm=Vm(store);Pass(vm.Configure(Policy()));
        Pass(vm.LoadModule(Module("""
            localStorage.setItem('RPG File1','one');
            localStorage.setItem('RPG File1bak',localStorage.getItem('RPG File1'));
            localStorage.setItem('RPG File1','two');
            if(localStorage.getItem('RPG File1bak')!=='one')throw Error('backup');
            localStorage.removeItem('RPG File1');
            if(localStorage.getItem('RPG File1')!==null)throw Error('remove');
            """)));
        Pass(vm.ExecuteModule("storage:test"));
    }

    public void Test_PolicyDenialFailsInsideScriptAndFaultsVm()
    {
        using var store=new MemoryScriptKeyValueStorage();using var vm=Vm(store);
        Pass(vm.Configure(new ScriptExecutionPolicy{AllowWriteSaveFiles=false,MaxMemoryMegabytes=64,MaxExecutionMillisecondsPerTick=250,MaxCallDepth=128}));
        Pass(vm.LoadModule(Module("localStorage.setItem('a','b');")));
        var result=vm.ExecuteModule("storage:test");
        AssertFalse(result.Success);AssertTrue(result.ErrorMessage.Contains("storage.denied",StringComparison.Ordinal));
        AssertEq(vm.State,ScriptVmState.Faulted);
    }

    public void Test_ResetKeepsCallerOwnedPersistentStoreButFreshRealm()
    {
        using var store=new MemoryScriptKeyValueStorage();using var vm=Vm(store);Pass(vm.Configure(Policy()));
        Pass(vm.LoadModule(Module("localStorage.setItem('config','x');globalThis.realmOnly=42;")));Pass(vm.ExecuteModule("storage:test"));
        Pass(vm.Reset());AssertEq(vm.State,ScriptVmState.Configured);
        Pass(vm.LoadModule(Module("if(localStorage.getItem('config')!=='x'||typeof realmOnly!=='undefined')throw Error('reset');")));Pass(vm.ExecuteModule("storage:test"));
    }

    public void Test_DisposingVmDoesNotDisposeCallerOwnedStore()
    {
        using var store=new MemoryScriptKeyValueStorage();var vm=Vm(store);Pass(vm.Configure(Policy()));vm.Dispose();
        AssertTrue(store.SetItem("after","ok").Success);AssertTrue(store.GetItem("after").Found);
    }

    public void Test_MemoryStoreBoundsEntriesAndValues()
    {
        using var store=new MemoryScriptKeyValueStorage();
        AssertFalse(store.SetItem(new string('k',MemoryScriptKeyValueStorage.MaxKeyCharacters+1),"x").Success);
        AssertFalse(store.SetItem("x",new string('v',MemoryScriptKeyValueStorage.MaxValueCharacters+1)).Success);
        for(var i=0;i<MemoryScriptKeyValueStorage.MaxEntries;i++)Pass(store.SetItem("k"+i,"x"));
        AssertFalse(store.SetItem("overflow","x").Success);
        Pass(store.SetItem("k0","updated"));
    }

    private static JintEmbeddedScriptVm Vm(IScriptKeyValueStorage storage)=>new(ScriptLanguageIds.RpgMakerMvJavaScript,storage);
    private static ScriptExecutionPolicy Policy()=>new(){AllowWriteSaveFiles=true,MaxMemoryMegabytes=64,MaxExecutionMillisecondsPerTick=250,MaxCallDepth=128};
    private static ScriptModule Module(string source)=>new(){Descriptor=new EngineScriptDescriptor{Id="storage:test",DisplayName="storage test",LanguageId=ScriptLanguageIds.RpgMakerMvJavaScript,RelativePath="js/test-storage.js",Origin=ScriptOrigin.Game},Source=Encoding.UTF8.GetBytes(source)};
    private void Pass(SdkOperationResult result){AssertTrue(result.Success,result.ErrorMessage);if(!result.Success)throw new InvalidOperationException(result.ErrorMessage);}
}
