using System;
using System.Collections.Generic;
using System.Linq;
using UniversalRPG.Sdk;

namespace UniversalRPG.Plugins;

public sealed class WebVmProfile
{
    public required string LanguageId { get; init; }
    public required string CompatibilityProfile { get; init; }
    public IReadOnlyList<string> RequiredFeatures { get; init; } = Array.Empty<string>();
}

public static class WebVmProfiles
{
    public static WebVmProfile For(bool pMZ)
    {
        return pMZ
            ? new WebVmProfile
            {
                LanguageId = ScriptLanguageIds.RpgMakerMzJavaScript,
                CompatibilityProfile = "rmmz-web-runtime",
                RequiredFeatures = new[]
                {
                    "javascript",
                    "window",
                    "document",
                    "timers",
                    "request-animation-frame",
                    "canvas",
                    "webgl",
                    "webaudio",
                    "storage",
                    "rmmz-api",
                },
            }
            : new WebVmProfile
            {
                LanguageId = ScriptLanguageIds.RpgMakerMvJavaScript,
                CompatibilityProfile = "rmmv-web-runtime",
                RequiredFeatures = new[]
                {
                    "javascript",
                    "window",
                    "document",
                    "timers",
                    "request-animation-frame",
                    "canvas",
                    "webgl",
                    "webaudio",
                    "storage",
                    "rmmv-api",
                },
            };
    }

    public static SdkVmResult CreateVm(
        bool pMZ,
        IEmbeddedScriptVmFactory pFactory,
        ScriptExecutionPolicy? pPolicy = null,
        IEnumerable<string>? pAdditionalFeatures = null)
    {
        if (pFactory == null) throw new ArgumentNullException(nameof(pFactory));
        var profile = For(pMZ);
        if (!ContainsOrdinal(pFactory.LanguageIds, profile.LanguageId))
        {
            return SdkVmResult.Failed(
                "web.vm-factory-language",
                $"VM factory '{pFactory.Id}' does not advertise '{profile.LanguageId}' support.");
        }

        var policy = pPolicy ?? ScriptExecutionPolicy.SafeDefault;
        var validation = policy.Validate();
        if (!validation.Success)
        {
            return SdkVmResult.Failed(validation.ErrorCode, validation.ErrorMessage, validation.Diagnostics);
        }

        var features = new List<string>(profile.RequiredFeatures);
        if (pAdditionalFeatures != null)
        {
            foreach (var feature in pAdditionalFeatures)
            {
                if (!string.IsNullOrWhiteSpace(feature) && !features.Contains(feature, StringComparer.Ordinal))
                {
                    features.Add(feature);
                }
            }
        }

        var result = pFactory.Create(new ScriptVmRequest
        {
            LanguageId = profile.LanguageId,
            CompatibilityProfile = profile.CompatibilityProfile,
            Policy = policy,
            RequiredFeatures = features,
        });
        if (!result.Success || result.Vm == null) return result;
        if (!result.Vm.LanguageId.Equals(profile.LanguageId, StringComparison.Ordinal))
        {
            result.Vm.Dispose();
            return SdkVmResult.Failed(
                "web.vm-factory-mismatch",
                $"VM factory '{pFactory.Id}' returned '{result.Vm.LanguageId}' for requested '{profile.LanguageId}'.");
        }
        return result;
    }

    private static bool ContainsOrdinal(IReadOnlyList<string> pValues, string pValue)
    {
        for (var index = 0; index < pValues.Count; index++)
        {
            if (pValues[index].Equals(pValue, StringComparison.Ordinal)) return true;
        }
        return false;
    }
}
