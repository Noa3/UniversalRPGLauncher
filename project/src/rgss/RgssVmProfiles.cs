using System;
using System.Collections.Generic;
using UniversalRPG.Sdk;

namespace UniversalRPG.Rgss;

public sealed class RgssVmProfile
{
    public required RgssGeneration Generation { get; init; }
    public required string LanguageId { get; init; }
    public required string CompatibilityProfile { get; init; }
    public required string HistoricalRubyLine { get; init; }
    public IReadOnlyList<string> RequiredFeatures { get; init; } = Array.Empty<string>();
}

public static class RgssVmProfiles
{
    public static RgssVmProfile For(RgssGeneration pGeneration)
    {
        return pGeneration switch
        {
            RgssGeneration.Rgss1 => new RgssVmProfile
            {
                Generation = pGeneration,
                LanguageId = ScriptLanguageIds.Rgss1Ruby,
                CompatibilityProfile = "rgss1-ruby18",
                HistoricalRubyLine = "ruby-1.8-compatible",
                RequiredFeatures = new[]
                {
                    "ruby-class-reopen",
                    "ruby-alias",
                    "ruby-marshal-4.8",
                    "rgss1-host-api",
                },
            },
            RgssGeneration.Rgss2 => new RgssVmProfile
            {
                Generation = pGeneration,
                LanguageId = ScriptLanguageIds.Rgss2Ruby,
                CompatibilityProfile = "rgss2-ruby18",
                HistoricalRubyLine = "ruby-1.8-compatible",
                RequiredFeatures = new[]
                {
                    "ruby-class-reopen",
                    "ruby-alias",
                    "ruby-marshal-4.8",
                    "rgss2-host-api",
                },
            },
            _ => new RgssVmProfile
            {
                Generation = pGeneration,
                LanguageId = ScriptLanguageIds.Rgss3Ruby,
                CompatibilityProfile = "rgss3-ruby192",
                HistoricalRubyLine = "ruby-1.9.2-compatible",
                RequiredFeatures = new[]
                {
                    "ruby-class-reopen",
                    "ruby-alias",
                    "ruby-marshal-4.8",
                    "ruby-encoding-metadata",
                    "rgss3-host-api",
                },
            },
        };
    }

    public static SdkVmResult CreateVm(
        RgssGeneration pGeneration,
        IEmbeddedScriptVmFactory pFactory,
        ScriptExecutionPolicy? pPolicy = null)
    {
        if (pFactory == null) throw new ArgumentNullException(nameof(pFactory));
        var profile = For(pGeneration);
        if (!ContainsOrdinal(pFactory.LanguageIds, profile.LanguageId))
        {
            return SdkVmResult.Failed(
                "rgss.vm-factory-language",
                $"VM factory '{pFactory.Id}' does not advertise '{profile.LanguageId}' support.");
        }

        var policy = pPolicy ?? ScriptExecutionPolicy.SafeDefault;
        var validation = policy.Validate();
        if (!validation.Success)
        {
            return SdkVmResult.Failed(validation.ErrorCode, validation.ErrorMessage, validation.Diagnostics);
        }

        var result = pFactory.Create(new ScriptVmRequest
        {
            LanguageId = profile.LanguageId,
            CompatibilityProfile = profile.CompatibilityProfile,
            Policy = policy,
            RequiredFeatures = profile.RequiredFeatures,
        });
        if (!result.Success || result.Vm == null)
        {
            return result;
        }
        if (!result.Vm.LanguageId.Equals(profile.LanguageId, StringComparison.Ordinal))
        {
            result.Vm.Dispose();
            return SdkVmResult.Failed(
                "rgss.vm-factory-mismatch",
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
