using System;
using global::Jint;
using global::Jint.Native;
using global::Jint.Runtime.Interop;
using UniversalRPG.Sdk;

namespace UniversalRPG.JavaScript.Jint;

public sealed partial class JintEmbeddedScriptVm
{
    private readonly NativeGameDataSource? _nativeGameData;

    /// <summary>
    /// Explicit native data capability. The content mount remains caller-owned.
    /// Only local data/*.json GET requests can reach it, and policy is checked
    /// for every read. This overload does not install a renderer or a browser.
    /// </summary>
    public JintEmbeddedScriptVm(string languageId, IGameContentSource gameContent, string contentPrefix = "")
        : this(languageId)
    {
        _nativeGameData = new NativeGameDataSource(gameContent, contentPrefix);
    }

    public bool HasNativeGameData => _nativeGameData != null;

    private void InstallNativeGameData()
    {
        if (_nativeGameData == null || _engine == null) return;
        var data = _nativeGameData;
        // An explicit JS native function, not a reflected CLR object/delegate
        // converted through FromObject. Only a primitive URL enters, only a
        // JSON string leaves. The function is captured privately by the adapter.
        var read = new ClrFunction(_engine, "readLocalGameJson", (_, arguments) =>
        {
            if (arguments.Length != 1 || !arguments[0].IsString())
                return (JsValue)NativeGameDataSource.Failure("data.invalid-url");
            return (JsValue)data.ReadEnvelope(arguments[0].AsString());
        }, 1);
        var install = _engine.Evaluate(NativeDataRequestPrelude.Source);
        _engine.Invoke(install, read);
    }
}
