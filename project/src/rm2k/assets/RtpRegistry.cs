using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace UniversalRPG.Rm2k.Assets;

/// <summary>
/// User-provided RTP mount metadata. The registry never ships or downloads RTP data.
/// </summary>
public sealed class RtpProfile
{
    public string Id { get; init; } = "";
    public string EngineId { get; init; } = "";
    public string Generation { get; init; } = "";
    public string DependencyName { get; init; } = "";
    public string RootPath { get; init; } = "";
}

public enum RtpResolutionStatus
{
    Found,
    NoMatchingProfile,
    MissingAsset,
    InvalidPath,
}

public sealed class RtpRegistrationResult
{
    private RtpRegistrationResult(bool pSuccess, string pError)
    {
        Success = pSuccess;
        Error = pError;
    }

    public bool Success { get; }
    public string Error { get; }

    public static RtpRegistrationResult Succeeded() => new(true, "");
    public static RtpRegistrationResult Failed(string pError) => new(false, pError);
}

public sealed class RtpResolutionResult
{
    private RtpResolutionResult(
        bool pSuccess,
        RtpResolutionStatus pStatus,
        string pResolvedPath,
        string pProfileId,
        string pError)
    {
        Success = pSuccess;
        Status = pStatus;
        ResolvedPath = pResolvedPath;
        ProfileId = pProfileId;
        Error = pError;
    }

    public bool Success { get; }
    public RtpResolutionStatus Status { get; }
    public string ResolvedPath { get; }
    public string ProfileId { get; }
    public string Error { get; }

    public static RtpResolutionResult Found(string pPath, string pProfileId)
        => new(true, RtpResolutionStatus.Found, pPath, pProfileId, "");

    public static RtpResolutionResult Failed(RtpResolutionStatus pStatus, string pError)
        => new(false, pStatus, "", "", pError);
}

/// <summary>
/// Deterministic resolver for explicitly registered RTP directories.
/// It performs bounded path validation and file existence checks only.
/// </summary>
public sealed class RtpRegistry
{
    private const int MaxProfileIdLength = 96;
    private const int MaxEngineIdLength = 96;
    private const int MaxGenerationLength = 96;
    private const int MaxDependencyNameLength = 128;
    private const int MaxRelativePathLength = 1024;
    private readonly List<RtpProfile> _profiles = new();

    public IReadOnlyList<RtpProfile> Profiles => _profiles;

    public RtpRegistrationResult Register(RtpProfile pProfile)
    {
        if (pProfile == null)
        {
            return RtpRegistrationResult.Failed("RTP profile is required.");
        }
        if (!IsIdentifier(pProfile.Id, MaxProfileIdLength)
            || !IsIdentifier(pProfile.EngineId, MaxEngineIdLength)
            || !IsIdentifier(pProfile.Generation, MaxGenerationLength))
        {
            return RtpRegistrationResult.Failed("RTP profile identifiers are invalid.");
        }
        if (!IsDependencyName(pProfile.DependencyName))
        {
            return RtpRegistrationResult.Failed("RTP dependency name is invalid.");
        }
        if (!TryGetDirectory(pProfile.RootPath, out var rootPath))
        {
            return RtpRegistrationResult.Failed("RTP root directory does not exist or is unsafe.");
        }
        if (_profiles.Any(pProfileItem => pProfileItem.Id.Equals(pProfile.Id, StringComparison.OrdinalIgnoreCase)))
        {
            return RtpRegistrationResult.Failed("RTP profile ID is already registered.");
        }

        _profiles.Add(new RtpProfile
        {
            Id = pProfile.Id,
            EngineId = pProfile.EngineId,
            Generation = pProfile.Generation,
            DependencyName = pProfile.DependencyName,
            RootPath = rootPath,
        });
        return RtpRegistrationResult.Succeeded();
    }

    public bool Unregister(string pProfileId)
    {
        var index = _profiles.FindIndex(pProfile =>
            pProfile.Id.Equals(pProfileId, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
        {
            return false;
        }
        _profiles.RemoveAt(index);
        return true;
    }

    public RtpResolutionResult Resolve(
        string pEngineId,
        string pGeneration,
        string pDependencyName,
        string pRelativePath)
    {
        if (!IsSafeRelativePath(pRelativePath))
        {
            return RtpResolutionResult.Failed(
                RtpResolutionStatus.InvalidPath,
                "RTP asset path must be a bounded relative path without traversal.");
        }
        if (!IsIdentifier(pEngineId, MaxEngineIdLength)
            || !IsIdentifier(pGeneration, MaxGenerationLength)
            || !IsDependencyName(pDependencyName))
        {
            return RtpResolutionResult.Failed(
                RtpResolutionStatus.NoMatchingProfile,
                "RTP engine, generation, or dependency identifier is invalid.");
        }

        var matchingProfile = false;
        var relativePath = pRelativePath.Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);
        foreach (var profile in _profiles)
        {
            if (!profile.EngineId.Equals(pEngineId, StringComparison.OrdinalIgnoreCase)
                || !profile.Generation.Equals(pGeneration, StringComparison.OrdinalIgnoreCase)
                || !profile.DependencyName.Equals(pDependencyName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            matchingProfile = true;
            if (!TryConfinedPath(profile.RootPath, relativePath, out var candidatePath)
                || !File.Exists(candidatePath)
                || HasReparsePointInPath(profile.RootPath, candidatePath))
            {
                continue;
            }
            return RtpResolutionResult.Found(candidatePath, profile.Id);
        }

        return RtpResolutionResult.Failed(
            matchingProfile ? RtpResolutionStatus.MissingAsset : RtpResolutionStatus.NoMatchingProfile,
            matchingProfile
                ? "No registered RTP profile contains the requested asset."
                : "No matching RTP profile is registered.");
    }

    private static bool TryGetDirectory(string pPath, out string pFullPath)
    {
        pFullPath = "";
        if (string.IsNullOrWhiteSpace(pPath) || pPath.IndexOf('\u0000') >= 0)
        {
            return false;
        }
        try
        {
            pFullPath = Path.GetFullPath(pPath);
            if (!Directory.Exists(pFullPath) || HasReparsePointInPath(pFullPath, pFullPath))
            {
                pFullPath = "";
                return false;
            }
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static bool TryConfinedPath(string pRootPath, string pRelativePath, out string pFullPath)
    {
        pFullPath = "";
        try
        {
            var root = Path.GetFullPath(pRootPath);
            var candidate = Path.GetFullPath(Path.Combine(root, pRelativePath));
            var rootWithSeparator = Path.EndsInDirectorySeparator(root)
                ? root
                : root + Path.DirectorySeparatorChar;
            if (!candidate.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            pFullPath = candidate;
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
    }

    private static bool IsSafeRelativePath(string pPath)
    {
        if (string.IsNullOrWhiteSpace(pPath)
            || pPath.Length > MaxRelativePathLength
            || pPath.IndexOf('\u0000') >= 0
            || Path.IsPathRooted(pPath)
            || pPath.Contains(':', StringComparison.Ordinal))
        {
            return false;
        }
        var segments = pPath.Split(new[] { '/', '\\' }, StringSplitOptions.None);
        return segments.All(pSegment =>
            pSegment.Length > 0
            && pSegment != "."
            && pSegment != ".."
            && pSegment.IndexOf('\u0000') < 0);
    }

    private static bool IsDependencyName(string pValue)
    {
        return !string.IsNullOrWhiteSpace(pValue)
            && pValue.Length <= MaxDependencyNameLength
            && pValue.IndexOf('\u0000') < 0
            && pValue.IndexOfAny(new[] { '/', '\\', ':' }) < 0;
    }

    private static bool IsIdentifier(string pValue, int pMaxLength)
    {
        if (string.IsNullOrEmpty(pValue) || pValue.Length > pMaxLength)
        {
            return false;
        }
        if (!IsAsciiAlphaNumeric(pValue[0]) || !IsAsciiAlphaNumeric(pValue[^1]))
        {
            return false;
        }
        foreach (var character in pValue)
        {
            if (!IsAsciiAlphaNumeric(character) && character != '-' && character != '_' && character != '.')
            {
                return false;
            }
        }
        return true;
    }

    private static bool IsAsciiAlphaNumeric(char pCharacter)
    {
        return (pCharacter >= 'a' && pCharacter <= 'z')
            || (pCharacter >= 'A' && pCharacter <= 'Z')
            || (pCharacter >= '0' && pCharacter <= '9');
    }

    private static bool HasReparsePointInPath(string pRootPath, string pCandidatePath)
    {
        try
        {
            var root = Path.GetFullPath(pRootPath);
            var candidate = Path.GetFullPath(pCandidatePath);
            var relative = Path.GetRelativePath(root, candidate);
            var current = root;
            if (File.GetAttributes(current).HasFlag(FileAttributes.ReparsePoint))
            {
                return true;
            }
            if (relative == ".")
            {
                return false;
            }
            foreach (var segment in relative.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries))
            {
                current = Path.Combine(current, segment);
                if ((File.Exists(current) || Directory.Exists(current))
                    && File.GetAttributes(current).HasFlag(FileAttributes.ReparsePoint))
                {
                    return true;
                }
            }
            return false;
        }
        catch (ArgumentException)
        {
            return true;
        }
        catch (IOException)
        {
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return true;
        }
    }
}
