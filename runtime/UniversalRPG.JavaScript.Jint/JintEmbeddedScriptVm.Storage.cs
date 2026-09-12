using System;
using global::Jint;
using global::Jint.Native;
using global::Jint.Runtime.Interop;
using UniversalRPG.Sdk;

namespace UniversalRPG.JavaScript.Jint;

public sealed partial class JintEmbeddedScriptVm
{
    private readonly IScriptKeyValueStorage? _nativeStorage;

    public JintEmbeddedScriptVm(string languageId,IScriptKeyValueStorage storage)
        : this(languageId)
    {
        _nativeStorage=storage??throw new ArgumentNullException(nameof(storage));
    }

    public JintEmbeddedScriptVm(string languageId,IGameContentSource gameContent,string contentPrefix,IScriptKeyValueStorage storage)
        : this(languageId,gameContent,contentPrefix)
    {
        _nativeStorage=storage??throw new ArgumentNullException(nameof(storage));
    }

    public bool HasNativeStorage=>_nativeStorage!=null;

    private void InstallNativeStorage()
    {
        if(_nativeStorage==null||_engine==null)return;
        var source=new NativeScriptStorageSource(_nativeStorage,()=>_policy.AllowWriteSaveFiles);
        var callback=new ClrFunction(_engine,"nativeLocalStorage",(_,arguments)=>
        {
            if(arguments.Length!=3||!arguments[0].IsString()||!arguments[1].IsString()||!arguments[2].IsString())
                return (JsValue)NativeScriptStorageSource.Failure("storage.arguments-invalid");
            return (JsValue)source.Invoke(arguments[0].AsString(),arguments[1].AsString(),arguments[2].AsString());
        },3);
        var install=_engine.Evaluate(NativeStoragePrelude.Source);
        _engine.Invoke(install,callback);
    }
}
