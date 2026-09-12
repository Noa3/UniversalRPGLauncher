using System;
using System.Collections.Generic;
using System.Linq;

namespace UniversalRPG.Sdk;

/// <summary>
/// Deterministic trusted-provider registry for packed/protected game content.
/// Providers are host/application components, never code loaded from imported
/// games. The registry resolves by explicit scheme IDs and fails on ambiguity.
/// Provider failures are isolated so one optional integration cannot crash game
/// analysis or another provider.
/// </summary>
public sealed class ProtectedContentRegistry
{
    private readonly List<IProtectedContentProvider> _providers = new();

    public IReadOnlyList<IProtectedContentProvider> Providers => _providers.ToArray();

    public SdkOperationResult Register(IProtectedContentProvider pProvider)
    {
        if (pProvider == null) throw new ArgumentNullException(nameof(pProvider));
        if (!IsStableId(pProvider.Id))
        {
            return SdkOperationResult.Failed(
                "content-provider.invalid-id",
                "Protected-content provider ID must be a stable lowercase identifier.");
        }
        if (_providers.Any(pExisting => pExisting.Id.Equals(pProvider.Id, StringComparison.Ordinal)))
        {
            return SdkOperationResult.Failed(
                "content-provider.duplicate-id",
                $"Protected-content provider '{pProvider.Id}' is already registered.");
        }
        if (pProvider.SchemeIds == null || pProvider.SchemeIds.Count == 0)
        {
            return SdkOperationResult.Failed(
                "content-provider.no-schemes",
                $"Protected-content provider '{pProvider.Id}' does not declare any scheme IDs.");
        }

        var seenSchemes = new HashSet<string>(StringComparer.Ordinal);
        foreach (var scheme in pProvider.SchemeIds)
        {
            if (!IsStableId(scheme))
            {
                return SdkOperationResult.Failed(
                    "content-provider.invalid-scheme",
                    $"Provider '{pProvider.Id}' declares invalid scheme ID '{scheme}'.");
            }
            if (!seenSchemes.Add(scheme))
            {
                return SdkOperationResult.Failed(
                    "content-provider.duplicate-scheme",
                    $"Provider '{pProvider.Id}' declares scheme '{scheme}' more than once.");
            }
        }

        _providers.Add(pProvider);
        _providers.Sort((pLeft, pRight) => string.CompareOrdinal(pLeft.Id, pRight.Id));
        return SdkOperationResult.Succeeded();
    }

    public IReadOnlyList<string> MatchingProviderIds(ProtectedContentDescriptor pDescriptor)
    {
        if (pDescriptor == null || !IsStableId(pDescriptor.SchemeId)) return Array.Empty<string>();
        return MatchingProviders(pDescriptor, out _).Select(pProvider => pProvider.Id).ToArray();
    }

    public ContentSourceResult Open(ProtectedContentDescriptor pDescriptor)
    {
        if (pDescriptor == null)
        {
            return ContentSourceResult.Failed("content.descriptor-required", "A protected-content descriptor is required.");
        }
        if (!IsStableId(pDescriptor.SchemeId))
        {
            return ContentSourceResult.Failed("content.scheme-invalid", "Protected-content scheme ID is invalid.");
        }

        var candidates = MatchingProviders(pDescriptor, out var probeDiagnostics);
        if (candidates.Length == 0)
        {
            return ContentSourceResult.Failed(
                "content.provider-unavailable",
                $"No trusted provider is registered for protected-content scheme '{pDescriptor.SchemeId}'.",
                probeDiagnostics);
        }
        if (candidates.Length > 1)
        {
            return ContentSourceResult.Failed(
                "content.provider-ambiguous",
                $"Multiple trusted providers accepted protected-content scheme '{pDescriptor.SchemeId}'.",
                probeDiagnostics);
        }

        try
        {
            var result = candidates[0].Open(pDescriptor);
            if (result == null)
            {
                return ContentSourceResult.Failed(
                    "content.provider-invalid-result",
                    $"Protected-content provider '{candidates[0].Id}' returned no result.",
                    probeDiagnostics);
            }
            return result;
        }
        catch (Exception exception)
        {
            return ContentSourceResult.Failed(
                "content.provider-open-exception",
                $"Protected-content provider '{candidates[0].Id}' failed while opening '{pDescriptor.SchemeId}': {exception.GetType().Name}: {exception.Message}",
                probeDiagnostics);
        }
    }

    private IProtectedContentProvider[] MatchingProviders(
        ProtectedContentDescriptor pDescriptor,
        out IReadOnlyList<SdkDiagnostic> pDiagnostics)
    {
        var matches = new List<IProtectedContentProvider>();
        var diagnostics = new List<SdkDiagnostic>();
        foreach (var provider in _providers)
        {
            if (!ContainsOrdinal(provider.SchemeIds, pDescriptor.SchemeId)) continue;
            try
            {
                if (provider.CanOpen(pDescriptor)) matches.Add(provider);
            }
            catch (Exception exception)
            {
                diagnostics.Add(SdkDiagnostic.Warning(
                    "content-provider.probe-exception",
                    $"Provider '{provider.Id}' failed while probing '{pDescriptor.SchemeId}': {exception.GetType().Name}: {exception.Message}"));
            }
        }
        pDiagnostics = diagnostics;
        return matches.ToArray();
    }

    private static bool ContainsOrdinal(IReadOnlyList<string> pValues, string pValue)
    {
        if (pValues == null) return false;
        for (var index = 0; index < pValues.Count; index++)
        {
            if (pValues[index]?.Equals(pValue, StringComparison.Ordinal) == true) return true;
        }
        return false;
    }

    private static bool IsStableId(string pValue)
    {
        if (string.IsNullOrWhiteSpace(pValue) || pValue.Length > 96) return false;
        if (!IsAlphaNumeric(pValue[0]) || !IsAlphaNumeric(pValue[^1])) return false;
        foreach (var character in pValue)
        {
            if (!IsAlphaNumeric(character) && character is not '-' and not '_' and not '.') return false;
        }
        return true;
    }

    private static bool IsAlphaNumeric(char pCharacter)
        => pCharacter is >= 'a' and <= 'z' or >= '0' and <= '9';
}
