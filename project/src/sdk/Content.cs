using System;
using System.Collections.Generic;

namespace UniversalRPG.Sdk;

public enum GameContentProtectionKind
{
    None,
    EngineManagedEncryption,
    ProtectedArchive,
    ExternalDrm,
    Unknown,
}

public static class ProtectedContentSchemes
{
    public const string RpgMakerMvAssets = "rpg-maker-mv-assets";
    public const string RpgMakerMzAssets = "rpg-maker-mz-assets";
    public const string Rgss1Archive = "rpg-maker-rgss1-archive";
    public const string Rgss2Archive = "rpg-maker-rgss2-archive";
    public const string Rgss3Archive = "rpg-maker-rgss3-archive";
    public const string WolfProtectedArchive = "wolf-protected-archive";
}

public sealed class GameContentEntry
{
    public string LogicalPath { get; init; } = "";
    public string SourcePath { get; init; } = "";
    public long Length { get; init; }
    public GameContentProtectionKind Protection { get; init; }
    public string ProtectionScheme { get; init; } = "";
}

public sealed class ContentReadResult
{
    private ContentReadResult(bool pSuccess, ReadOnlyMemory<byte> pData, string pErrorCode, string pErrorMessage)
    {
        Success = pSuccess;
        Data = pData;
        ErrorCode = pErrorCode;
        ErrorMessage = pErrorMessage;
    }

    public bool Success { get; }
    public ReadOnlyMemory<byte> Data { get; }
    public string ErrorCode { get; }
    public string ErrorMessage { get; }

    public static ContentReadResult Succeeded(ReadOnlyMemory<byte> pData)
        => new(true, pData, "", "");

    public static ContentReadResult Failed(string pCode, string pMessage)
        => new(false, ReadOnlyMemory<byte>.Empty, pCode ?? "content.failure", pMessage ?? "Content read failed.");
}

/// <summary>
/// Read-only logical game-content source. Implementations may map a plain game
/// directory, an engine archive, or transparent engine-managed encryption. The
/// public contract intentionally exposes logical bytes, not extraction APIs.
/// </summary>
public interface IGameContentSource : IDisposable
{
    string SourceId { get; }
    GameContentProtectionKind Protection { get; }
    bool Exists(string pLogicalPath);
    ContentReadResult Read(string pLogicalPath);
}

public sealed class ProtectedContentDescriptor
{
    public string SchemeId { get; init; } = "";
    public string SourcePath { get; init; } = "";
    public string EngineId { get; init; } = "";
    public GameContentProtectionKind Protection { get; init; } = GameContentProtectionKind.Unknown;
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
}

/// <summary>
/// Analysis-time status for one protected/packed game-content source. This lets
/// clients distinguish supported engine-managed encryption from a detected
/// archive that still requires another trusted provider or explicit policy.
/// </summary>
public sealed class ProtectedContentStatus
{
    public ProtectedContentDescriptor Descriptor { get; init; } = new();
    public bool RuntimeReadable { get; init; }
    public string ProviderId { get; init; } = "";
    public string Note { get; init; } = "";
}

public sealed class ContentSourceResult
{
    private ContentSourceResult(bool pSuccess, IGameContentSource? pSource, SdkOperationResult pResult)
    {
        Success = pSuccess;
        Source = pSource;
        Result = pResult;
    }

    public bool Success { get; }
    public IGameContentSource? Source { get; }
    public SdkOperationResult Result { get; }

    public static ContentSourceResult Succeeded(IGameContentSource pSource, IEnumerable<SdkDiagnostic>? pDiagnostics = null)
    {
        if (pSource == null) throw new ArgumentNullException(nameof(pSource));
        return new ContentSourceResult(true, pSource, SdkOperationResult.Succeeded(pDiagnostics));
    }

    public static ContentSourceResult Failed(string pCode, string pMessage, IEnumerable<SdkDiagnostic>? pDiagnostics = null)
        => new(false, null, SdkOperationResult.Failed(pCode, pMessage, pDiagnostics));
}

/// <summary>
/// Trusted host extension point for protected/packed engine content. UniversalRPG
/// may ship providers for engine-managed formats whose runtime-compatible access
/// is intentionally supported. Imported games must never supply executable
/// provider code themselves.
/// </summary>
public interface IProtectedContentProvider
{
    string Id { get; }
    IReadOnlyList<string> SchemeIds { get; }
    bool CanOpen(ProtectedContentDescriptor pDescriptor);
    ContentSourceResult Open(ProtectedContentDescriptor pDescriptor);
}
