using System;
using System.Collections.Generic;
using System.Linq;
using UniversalRPG.Sdk;

namespace UniversalRPG.Rgss;

/// <summary>
/// Engine-level RGSS script loader/boot sequence independent from the concrete
/// Ruby implementation. Scripts are loaded and executed in archive order so
/// custom aliases, monkey patches and script-editor ordering remain meaningful.
/// </summary>
public sealed class RgssScriptRuntime : IEngineScriptingRuntime, IDisposable
{
    private readonly IEmbeddedScriptVm _vm;
    private readonly RgssGeneration _generation;
    private readonly IReadOnlyList<RgssScriptEntry> _entries;
    private readonly string _languageId;
    private bool _loaded;
    private bool _bootstrapped;
    private bool _disposed;

    public RgssScriptRuntime(
        RgssGeneration pGeneration,
        IEmbeddedScriptVm pVm,
        IEnumerable<RgssScriptEntry> pEntries)
    {
        _generation = pGeneration;
        _vm = pVm ?? throw new ArgumentNullException(nameof(pVm));
        if (pEntries == null) throw new ArgumentNullException(nameof(pEntries));
        _entries = pEntries.OrderBy(pEntry => pEntry.ArchiveIndex).ToArray();
        _languageId = pGeneration switch
        {
            RgssGeneration.Rgss1 => ScriptLanguageIds.Rgss1Ruby,
            RgssGeneration.Rgss2 => ScriptLanguageIds.Rgss2Ruby,
            _ => ScriptLanguageIds.Rgss3Ruby,
        };
    }

    public IReadOnlyList<string> LanguageIds => new[] { _languageId };
    public ScriptRuntimeCapability Capabilities =>
        ScriptRuntimeCapability.Discover
        | ScriptRuntimeCapability.OrderedLoad
        | ScriptRuntimeCapability.Bootstrap
        | ScriptRuntimeCapability.HostHooks;
    public IReadOnlyList<EngineScriptDescriptor> Scripts => _entries.Select(pEntry => pEntry.Descriptor).ToArray();

    public SdkOperationResult DiscoverScripts()
    {
        if (_disposed) return Disposed();
        foreach (var entry in _entries)
        {
            var validation = entry.Descriptor.Validate();
            if (!validation.Success)
            {
                return SdkOperationResult.Failed(
                    "rgss.invalid-script-descriptor",
                    $"RGSS script '{entry.Name}' has invalid metadata: {validation.ErrorMessage}");
            }
            if (!entry.Descriptor.LanguageId.Equals(_languageId, StringComparison.Ordinal))
            {
                return SdkOperationResult.Failed(
                    "rgss.language-mismatch",
                    $"RGSS script '{entry.Name}' targets '{entry.Descriptor.LanguageId}' instead of '{_languageId}'.");
            }
        }
        return SdkOperationResult.Succeeded();
    }

    public SdkOperationResult LoadScripts(ScriptExecutionPolicy pPolicy)
    {
        if (_disposed) return Disposed();
        if (_loaded)
        {
            return SdkOperationResult.Failed("rgss.already-loaded", "RGSS scripts were already loaded into this VM session.");
        }
        if (pPolicy == null)
        {
            return SdkOperationResult.Failed("rgss.policy-required", "A script execution policy is required before loading RGSS code.");
        }
        var policyValidation = pPolicy.Validate();
        if (!policyValidation.Success) return policyValidation;

        var discovered = DiscoverScripts();
        if (!discovered.Success) return discovered;

        if (!_vm.LanguageId.Equals(_languageId, StringComparison.Ordinal))
        {
            return SdkOperationResult.Failed(
                "rgss.vm-language-mismatch",
                $"Embedded VM language '{_vm.LanguageId}' cannot host '{_languageId}' scripts.");
        }

        var configured = _vm.Configure(pPolicy);
        if (!configured.Success) return configured;

        foreach (var entry in _entries)
        {
            var module = new ScriptModule
            {
                Descriptor = entry.Descriptor,
                Source = entry.Source,
            };
            var moduleValidation = module.Validate();
            if (!moduleValidation.Success) return moduleValidation;

            var loaded = _vm.LoadModule(module);
            if (!loaded.Success)
            {
                return SdkOperationResult.Failed(
                    "rgss.module-load-failed",
                    $"Failed to load RGSS script #{entry.ArchiveIndex} '{entry.Name}': {loaded.ErrorMessage}",
                    loaded.Diagnostics);
            }
        }

        _loaded = true;
        return SdkOperationResult.Succeeded(new[]
        {
            SdkDiagnostic.Info("rgss.scripts-loaded", $"Loaded {_entries.Count} {_languageId} scripts in archive order."),
        });
    }

    public SdkOperationResult ExecuteBootstrap()
    {
        if (_disposed) return Disposed();
        if (!_loaded)
        {
            return SdkOperationResult.Failed("rgss.not-loaded", "RGSS scripts must be loaded before bootstrap execution.");
        }
        if (_bootstrapped)
        {
            return SdkOperationResult.Failed("rgss.already-bootstrapped", "RGSS script bootstrap has already been executed for this session.");
        }

        foreach (var entry in _entries)
        {
            var executed = _vm.ExecuteModule(entry.Descriptor.Id);
            if (!executed.Success)
            {
                return SdkOperationResult.Failed(
                    "rgss.script-execution-failed",
                    $"RGSS script #{entry.ArchiveIndex} '{entry.Name}' failed: {executed.ErrorMessage}",
                    executed.Diagnostics);
            }
        }

        _bootstrapped = true;
        return SdkOperationResult.Succeeded(new[]
        {
            SdkDiagnostic.Info("rgss.bootstrap-complete", $"Executed {_entries.Count} {_languageId} scripts in archive order."),
        });
    }

    public SdkOperationResult InvokeHook(ScriptHookRequest pRequest)
    {
        if (_disposed) return Disposed();
        if (pRequest == null)
        {
            return SdkOperationResult.Failed("rgss.hook-required", "A hook request is required.");
        }
        var validation = pRequest.Validate();
        if (!validation.Success) return validation;
        if (!_loaded)
        {
            return SdkOperationResult.Failed("rgss.not-loaded", "RGSS scripts must be loaded before invoking host hooks.");
        }

        var arguments = pRequest.Arguments
            .OrderBy(pPair => pPair.Key, StringComparer.Ordinal)
            .Select(pPair => new ScriptValue(pPair.Value))
            .ToArray();
        return _vm.Invoke(new ScriptInvocation
        {
            Target = "UniversalRPG",
            Member = pRequest.Hook,
            Arguments = arguments,
        });
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _vm.Dispose();
    }

    private static SdkOperationResult Disposed()
        => SdkOperationResult.Failed("rgss.disposed", "The RGSS script runtime has been disposed.");
}
