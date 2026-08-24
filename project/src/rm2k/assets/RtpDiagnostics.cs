using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace UniversalRPG.Rm2k.Assets;

public sealed class RtpGameProfile
{
    public const int MaxRequiredAssets = 256;
    public const int MaxAssetPathLength = 1024;
    public const int MaxGamePathLength = 4096;

    public string GamePath { get; init; } = "";
    public string EngineId { get; init; } = "";
    public string Generation { get; init; } = "";
    public string DependencyName { get; init; } = "";
    public string RtpProfileId { get; init; } = "";
    public List<string> RequiredAssets { get; init; } = new();

    public bool TryValidate(out string pError)
    {
        pError = "";
        if (string.IsNullOrWhiteSpace(GamePath)
            || GamePath.Length > MaxGamePathLength
            || GamePath.IndexOf('\u0000') >= 0)
        {
            pError = "RTP game profile path is invalid.";
            return false;
        }
        if (!IsIdentifier(EngineId) || !IsIdentifier(Generation) || !IsIdentifier(RtpProfileId))
        {
            pError = "RTP game profile identifiers are invalid.";
            return false;
        }
        if (string.IsNullOrWhiteSpace(DependencyName)
            || DependencyName.Length > 128
            || DependencyName.IndexOfAny(new[] { '/', '\\', ':', '\u0000' }) >= 0)
        {
            pError = "RTP game profile dependency name is invalid.";
            return false;
        }
        if (RequiredAssets == null || RequiredAssets.Count > MaxRequiredAssets)
        {
            pError = "RTP game profile required asset list exceeds its bounded limit.";
            return false;
        }
        foreach (var asset in RequiredAssets)
        {
            if (string.IsNullOrWhiteSpace(asset) || asset.Length > MaxAssetPathLength)
            {
                pError = "RTP game profile contains an oversized required asset path.";
                return false;
            }
        }
        return true;
    }

    private static bool IsIdentifier(string pValue)
    {
        if (string.IsNullOrEmpty(pValue) || pValue.Length > 96)
        {
            return false;
        }
        if (!IsAsciiAlphaNumeric(pValue[0]) || !IsAsciiAlphaNumeric(pValue[^1]))
        {
            return false;
        }
        return pValue.All(pCharacter =>
            IsAsciiAlphaNumeric(pCharacter) || pCharacter is '-' or '_' or '.');
    }

    private static bool IsAsciiAlphaNumeric(char pCharacter)
    {
        return (pCharacter >= 'a' && pCharacter <= 'z')
            || (pCharacter >= 'A' && pCharacter <= 'Z')
            || (pCharacter >= '0' && pCharacter <= '9');
    }
}

public static class RtpGameProfileCodec
{
    public const int MaxPayloadBytes = 64 * 1024;

    public static string Serialize(RtpGameProfile pProfile)
    {
        ArgumentNullException.ThrowIfNull(pProfile);
        if (!pProfile.TryValidate(out var error))
        {
            throw new InvalidOperationException(error);
        }
        var json = JsonSerializer.Serialize(pProfile);
        if (Encoding.UTF8.GetByteCount(json) > MaxPayloadBytes)
        {
            throw new InvalidOperationException("RTP game profile exceeds the bounded payload limit.");
        }
        return json;
    }

    public static bool TrySerialize(RtpGameProfile pProfile, out string pJson, out string pError)
    {
        pJson = "";
        pError = "";
        try
        {
            pJson = Serialize(pProfile);
            return true;
        }
        catch (ArgumentNullException)
        {
            pError = "RTP game profile is required.";
            return false;
        }
        catch (InvalidOperationException exception)
        {
            pError = exception.Message;
            return false;
        }
    }

    public static bool TryDeserialize(string pJson, out RtpGameProfile? pProfile, out string pError)
    {
        pProfile = null;
        pError = "";
        if (string.IsNullOrWhiteSpace(pJson)
            || Encoding.UTF8.GetByteCount(pJson) > MaxPayloadBytes)
        {
            pError = "RTP game profile is empty or exceeds the bounded payload limit.";
            return false;
        }
        try
        {
            var profile = JsonSerializer.Deserialize<RtpGameProfile>(pJson);
            if (profile == null || !profile.TryValidate(out pError))
            {
                if (string.IsNullOrEmpty(pError))
                {
                    pError = "RTP game profile is null.";
                }
                return false;
            }
            pProfile = profile;
            return true;
        }
        catch (JsonException)
        {
            pError = "RTP game profile is not valid JSON.";
            return false;
        }
    }
}

public enum RtpAssetStatus
{
    Available,
    MissingAsset,
    NoMatchingProfile,
    InvalidPath,
}

public sealed class RtpAssetDiagnostic
{
    public string RelativePath { get; init; } = "";
    public RtpAssetStatus Status { get; init; }
    public string ProfileId { get; init; } = "";
    public string ResolvedPath { get; init; } = "";
    public string Message { get; init; } = "";
}

public sealed class RtpAssetDiagnosticReport
{
    internal RtpAssetDiagnosticReport(bool pSuccess, string pError, IReadOnlyList<RtpAssetDiagnostic> pDiagnostics)
    {
        Success = pSuccess;
        Error = pError;
        Diagnostics = pDiagnostics;
    }

    public bool Success { get; }
    public string Error { get; }
    public IReadOnlyList<RtpAssetDiagnostic> Diagnostics { get; }
    public bool HasMissingAssets => Diagnostics.Any(pDiagnostic =>
        pDiagnostic.Status == RtpAssetStatus.MissingAsset
        || pDiagnostic.Status == RtpAssetStatus.NoMatchingProfile
        || pDiagnostic.Status == RtpAssetStatus.InvalidPath);
}

public static class RtpAssetDiagnostics
{
    public static RtpAssetDiagnosticReport Analyze(RtpRegistry pRegistry, RtpGameProfile pProfile)
    {
        if (pRegistry == null)
        {
            return new RtpAssetDiagnosticReport(false, "RTP registry is required.", Array.Empty<RtpAssetDiagnostic>());
        }
        var error = "";
        if (pProfile == null || !pProfile.TryValidate(out error))
        {
            return new RtpAssetDiagnosticReport(false, error == "" ? "RTP game profile is required." : error, Array.Empty<RtpAssetDiagnostic>());
        }

        var diagnostics = new List<RtpAssetDiagnostic>(pProfile.RequiredAssets.Count);
        foreach (var relativePath in pProfile.RequiredAssets)
        {
            var result = pRegistry.Resolve(
                pProfile.EngineId,
                pProfile.Generation,
                pProfile.DependencyName,
                relativePath);
            diagnostics.Add(new RtpAssetDiagnostic
            {
                RelativePath = relativePath,
                Status = result.Status switch
                {
                    RtpResolutionStatus.Found => RtpAssetStatus.Available,
                    RtpResolutionStatus.MissingAsset => RtpAssetStatus.MissingAsset,
                    RtpResolutionStatus.InvalidPath => RtpAssetStatus.InvalidPath,
                    _ => RtpAssetStatus.NoMatchingProfile,
                },
                ProfileId = result.ProfileId,
                ResolvedPath = result.ResolvedPath,
                Message = result.Error,
            });
        }
        return new RtpAssetDiagnosticReport(true, "", diagnostics);
    }
}
