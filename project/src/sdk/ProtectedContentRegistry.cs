using System;
using System.Collections.Generic;
using System.Linq;

namespace UniversalRPG.Sdk;

/// <summary>
/// Deterministic trusted-provider registry for packed/protected game content.
/// Providers are host/application components, never code loaded from imported
/// games. The registry resolves by explicit scheme IDs and fails on ambiguity.
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
        foreach (var scheme in pProvider.SchemeIds)
        {
            if (!IsStableId(scheme))
            {
                return SdkOperationResult.Failed(
                    "content-provider.invalid-scheme",
                    $"Provider '{pProvider.Id}' declares invalid scheme ID '{scheme}'.");
            }
        }

        _providers.Add(pProvider);
        _providers.Sort((pLeft, pRight) => string.CompareOrdinal(pLeft.Id, pRight.Id));
        return SdkOperationResult.Succeeded();
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

        var candidates = _providers
            .Where(pProvider => ContainsOrdinal(pProvider.SchemeIds, pDescriptor.SchemeId))
            .Where(pProvider => pProvider.CanOpen(pDescriptor))
            .ToArray();
        if (candidates.Length == 0)
        {
            return ContentSourceResult.Failed(
                "content.provider-unavailable",
                $"No trusted provider is registered for protected-content scheme '{pDescriptor.SchemeId}'.");
        }
        if (candidates.Length > 1)
        {
            return ContentSourceResult.Failed(
                "content.provider-ambiguous",
                $"Multiple trusted providers accepted protected-content scheme '{pDescriptor.SchemeId}'.");
        }
        return candidates[0].Open(pDescriptor);
    }

    private static bool ContainsOrdinal(IReadOnlyList<string> pValues, string pValue)
    {
        for (var index = 0; index < pValues.Count; index++)
        {
            if (pValues[index].Equals(pValue, StringComparison.Ordinal)) return true;
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
