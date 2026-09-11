using System;
using System.Collections.Generic;

namespace UniversalRPG.Sdk;

public enum EngineSupportLevel
{
    Unknown,
    DetectionOnly,
    ParsingOnly,
    ExperimentalRuntime,
    PartialRuntime,
    Playable,
    BroadCompatibility,
}

public sealed class EngineSupportDescriptor
{
    public string EngineId { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public string Generation { get; init; } = "";
    public EngineSupportLevel SupportLevel { get; init; }
    public bool SupportsScripting { get; init; }
    public IReadOnlyList<string> ScriptLanguageIds { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Platforms { get; init; } = Array.Empty<string>();
}

public sealed class GameAnalysis
{
    public string GameDirectory { get; init; } = "";
    public string EngineId { get; init; } = "";
    public string Generation { get; init; } = "";
    public string Title { get; init; } = "";
    public int ConfidenceScore { get; init; }
    public EngineSupportLevel SupportLevel { get; init; }
    public IReadOnlyList<string> Evidence { get; init; } = Array.Empty<string>();
    public IReadOnlyList<SdkDiagnostic> Diagnostics { get; init; } = Array.Empty<SdkDiagnostic>();
}

/// <summary>
/// Godot-free public embedding surface. An implementation may be provided by
/// the full UniversalRPG runtime package/application; external callers should
/// depend on this interface rather than launcher UI classes.
/// </summary>
public interface IUniversalRpgLibrary
{
    int ApiVersion { get; }
    IReadOnlyList<EngineSupportDescriptor> Engines { get; }
    GameAnalysis Analyze(string pGameDirectory);
    SdkSessionResult CreateSession(GameAnalysis pAnalysis);
}

public sealed class SdkSessionResult
{
    private SdkSessionResult(bool pSuccess, IUniversalRpgSession? pSession, SdkOperationResult pResult)
    {
        Success = pSuccess;
        Session = pSession;
        Result = pResult;
    }

    public bool Success { get; }
    public IUniversalRpgSession? Session { get; }
    public SdkOperationResult Result { get; }

    public static SdkSessionResult Succeeded(IUniversalRpgSession pSession, IEnumerable<SdkDiagnostic>? pDiagnostics = null)
    {
        if (pSession == null) throw new ArgumentNullException(nameof(pSession));
        return new SdkSessionResult(true, pSession, SdkOperationResult.Succeeded(pDiagnostics));
    }

    public static SdkSessionResult Failed(string pCode, string pMessage, IEnumerable<SdkDiagnostic>? pDiagnostics = null)
        => new(false, null, SdkOperationResult.Failed(pCode, pMessage, pDiagnostics));
}

public interface IUniversalRpgSession : IDisposable
{
    string EngineId { get; }
    bool IsRunning { get; }
    IEngineScriptingRuntime? Scripting { get; }

    SdkOperationResult Start();
    SdkOperationResult Update(double pDeltaSeconds);
    SdkOperationResult Stop();
}

/// <summary>
/// Contract for trusted host-level extensions. This is deliberately different
/// from game-authored Ruby/JavaScript/native content. The launcher must never
/// auto-load arbitrary assemblies from an imported game directory.
/// </summary>
public interface IUniversalRpgExtension
{
    UniversalRpgExtensionMetadata Metadata { get; }
    SdkOperationResult Initialize(IUniversalRpgExtensionHost pHost);
    SdkOperationResult Shutdown();
}

public sealed class UniversalRpgExtensionMetadata
{
    public string Id { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public Version Version { get; init; } = new(0, 1, 0);
    public int MinimumApiVersion { get; init; } = UniversalRpgSdkVersion.ApiVersion;
}

public interface IUniversalRpgExtensionHost
{
    int ApiVersion { get; }
    void Report(SdkDiagnostic pDiagnostic);
}
