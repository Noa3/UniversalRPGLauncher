using System;
using System.Collections.Generic;

namespace UniversalRPG.Sdk;

public sealed class ScriptVmRequest
{
    public string LanguageId { get; init; } = "";
    public string CompatibilityProfile { get; init; } = "";
    public ScriptExecutionPolicy Policy { get; init; } = ScriptExecutionPolicy.SafeDefault;
    public IReadOnlyList<string> RequiredFeatures { get; init; } = Array.Empty<string>();
}

public interface IEmbeddedScriptVmFactory
{
    string Id { get; }
    IReadOnlyList<string> LanguageIds { get; }

    /// <summary>
    /// Creates a fresh VM session. A factory must return a stable unsupported
    /// error instead of silently substituting incompatible language semantics.
    /// </summary>
    SdkVmResult Create(ScriptVmRequest pRequest);
}

public sealed class SdkVmResult
{
    private SdkVmResult(bool pSuccess, IEmbeddedScriptVm? pVm, SdkOperationResult pResult)
    {
        Success = pSuccess;
        Vm = pVm;
        Result = pResult;
    }

    public bool Success { get; }
    public IEmbeddedScriptVm? Vm { get; }
    public SdkOperationResult Result { get; }

    public static SdkVmResult Succeeded(IEmbeddedScriptVm pVm, IEnumerable<SdkDiagnostic>? pDiagnostics = null)
    {
        if (pVm == null) throw new ArgumentNullException(nameof(pVm));
        return new SdkVmResult(true, pVm, SdkOperationResult.Succeeded(pDiagnostics));
    }

    public static SdkVmResult Failed(string pCode, string pMessage, IEnumerable<SdkDiagnostic>? pDiagnostics = null)
        => new(false, null, SdkOperationResult.Failed(pCode, pMessage, pDiagnostics));
}
