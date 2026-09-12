using System;
using System.Collections.Generic;

namespace UniversalRPG.Sdk;

public enum SdkDiagnosticSeverity
{
    Info,
    Warning,
    Error,
}

public sealed class SdkDiagnostic
{
    public SdkDiagnosticSeverity Severity { get; init; }
    public string Code { get; init; } = "";
    public string Message { get; init; } = "";

    public static SdkDiagnostic Info(string pCode, string pMessage)
        => new() { Severity = SdkDiagnosticSeverity.Info, Code = pCode ?? "", Message = pMessage ?? "" };

    public static SdkDiagnostic Warning(string pCode, string pMessage)
        => new() { Severity = SdkDiagnosticSeverity.Warning, Code = pCode ?? "", Message = pMessage ?? "" };

    public static SdkDiagnostic Error(string pCode, string pMessage)
        => new() { Severity = SdkDiagnosticSeverity.Error, Code = pCode ?? "", Message = pMessage ?? "" };
}

public sealed class SdkOperationResult
{
    private SdkOperationResult(
        bool pSuccess,
        string pErrorCode,
        string pErrorMessage,
        IReadOnlyList<SdkDiagnostic> pDiagnostics)
    {
        Success = pSuccess;
        ErrorCode = pErrorCode;
        ErrorMessage = pErrorMessage;
        Diagnostics = pDiagnostics;
    }

    public bool Success { get; }
    public string ErrorCode { get; }
    public string ErrorMessage { get; }
    public IReadOnlyList<SdkDiagnostic> Diagnostics { get; }

    public static SdkOperationResult Succeeded(IEnumerable<SdkDiagnostic>? pDiagnostics = null)
        => new(true, "", "", Copy(pDiagnostics));

    public static SdkOperationResult Failed(
        string pErrorCode,
        string pErrorMessage,
        IEnumerable<SdkDiagnostic>? pDiagnostics = null)
    {
        if (string.IsNullOrWhiteSpace(pErrorCode))
        {
            throw new ArgumentException("An SDK failure requires a stable error code.", nameof(pErrorCode));
        }
        if (string.IsNullOrWhiteSpace(pErrorMessage))
        {
            throw new ArgumentException("An SDK failure requires an error message.", nameof(pErrorMessage));
        }
        return new SdkOperationResult(false, pErrorCode, pErrorMessage, Copy(pDiagnostics));
    }

    private static IReadOnlyList<SdkDiagnostic> Copy(IEnumerable<SdkDiagnostic>? pDiagnostics)
        => pDiagnostics == null ? Array.Empty<SdkDiagnostic>() : new List<SdkDiagnostic>(pDiagnostics);
}

public sealed class SdkValueResult<T>
{
    private SdkValueResult(bool pSuccess, T? pValue, SdkOperationResult pResult)
    {
        Success = pSuccess;
        Value = pValue;
        Result = pResult;
    }

    public bool Success { get; }
    public T? Value { get; }
    public SdkOperationResult Result { get; }

    public static SdkValueResult<T> Succeeded(T pValue, IEnumerable<SdkDiagnostic>? pDiagnostics = null)
    {
        if (pValue is null) throw new ArgumentNullException(nameof(pValue));
        return new SdkValueResult<T>(true, pValue, SdkOperationResult.Succeeded(pDiagnostics));
    }

    public static SdkValueResult<T> Failed(
        string pErrorCode,
        string pErrorMessage,
        IEnumerable<SdkDiagnostic>? pDiagnostics = null)
        => new(false, default, SdkOperationResult.Failed(pErrorCode, pErrorMessage, pDiagnostics));
}

public static class UniversalRpgSdkVersion
{
    /// <summary>
    /// Major API version for the contracts assembly. Breaking contract changes
    /// require incrementing this value.
    /// </summary>
    public const int ApiVersion = 1;

    /// <summary>
    /// Pre-release contract version. This is not a claim that the complete
    /// UniversalRPG runtime is production-ready.
    /// </summary>
    public const string ContractVersion = "0.1.0-alpha";
}
