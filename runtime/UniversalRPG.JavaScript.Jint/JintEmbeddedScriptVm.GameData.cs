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
    /// Only local data/*.json and the selected main.js startup check can reach
    /// it. This overload does not install a renderer or general browser.
    /// </summary>
    public JintEmbeddedScriptVm(string languageId, IGameContentSource gameContent, string contentPrefix = "")
        : this(languageId)
    {
        _nativeGameData = new NativeGameDataSource(gameContent, contentPrefix);
    }

    public bool HasNativeGameData => _nativeGameData != null;

    /// <summary>
    /// Install optional native data and storage bridges after the realm is
    /// rebuilt. Storage is deliberately installed from here too so a VM that
    /// has only save/config persistence still receives localStorage.
    /// </summary>
    private void InstallNativeGameData()
    {
        if (_engine != null && _nativeGameData != null)
        {
            var data = _nativeGameData;
            var read = new ClrFunction(_engine, "readLocalGameJson", (_, arguments) =>
            {
                if (arguments.Length != 1 || !arguments[0].IsString())
                    return (JsValue)NativeGameDataSource.Failure("data.invalid-url");
                return (JsValue)data.ReadEnvelope(arguments[0].AsString());
            }, 1);
            var install = _engine.Evaluate(NativeDataRequestPrelude.Source);
            _engine.Invoke(install, read);
        }
        InstallNativeStorage();
    }
}
