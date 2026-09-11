using System;
using System.Collections.Generic;

namespace UniversalRPG.Sdk;

public enum ScriptVmState
{
    Created,
    Configured,
    Ready,
    Running,
    Faulted,
    Disposed,
}

public sealed class ScriptModule
{
    public EngineScriptDescriptor Descriptor { get; init; } = new();
    public ReadOnlyMemory<byte> Source { get; init; } = ReadOnlyMemory<byte>.Empty;

    public SdkOperationResult Validate()
    {
        var descriptor = Descriptor.Validate();
        if (!descriptor.Success) return descriptor;
        if (Source.Length > 64 * 1024 * 1024)
        {
            return SdkOperationResult.Failed("script.module-too-large", "A single script module exceeds the 64 MiB SDK boundary.");
        }
        return SdkOperationResult.Succeeded();
    }
}

public sealed class ScriptValue
{
    public ScriptValue(object? pValue) => Value = pValue;
    public object? Value { get; }
}

public sealed class ScriptInvocation
{
    public string Target { get; init; } = "";
    public string Member { get; init; } = "";
    public IReadOnlyList<ScriptValue> Arguments { get; init; } = Array.Empty<ScriptValue>();
}

/// <summary>
/// Low-level embedded VM abstraction. Implementations may wrap CRuby, QuickJS,
/// V8, or another suitable runtime, but game code never receives the host VM
/// implementation directly. Engine backends are responsible for reproducing
/// historical RGSS/MV/MZ APIs and load ordering above this layer.
/// </summary>
public interface IEmbeddedScriptVm : IDisposable
{
    string LanguageId { get; }
    ScriptVmState State { get; }
    ScriptExecutionPolicy Policy { get; }

    SdkOperationResult Configure(ScriptExecutionPolicy pPolicy);
    SdkOperationResult LoadModule(ScriptModule pModule);
    SdkOperationResult ExecuteModule(string pScriptId);
    SdkOperationResult Invoke(ScriptInvocation pInvocation);
    SdkOperationResult Reset();
}

/// <summary>
/// Trusted embedding applications may provide compatibility libraries/shims to
/// an engine runtime. Imported games must never be allowed to register arbitrary
/// host libraries through this interface.
/// </summary>
public interface IScriptLibraryProvider
{
    string Id { get; }
    IReadOnlyList<string> LanguageIds { get; }
    SdkOperationResult Register(IScriptLibraryRegistry pRegistry);
}

public interface IScriptLibraryRegistry
{
    int ApiVersion { get; }
    SdkOperationResult AddModule(ScriptModule pModule);
}
