using System;
using System.Collections.Generic;
using System.Linq;
using UniversalRPG.Sdk;

namespace UniversalRPG.Plugins;

/// <summary>
/// Engine-level MV/MZ custom-plugin loader independent from the concrete
/// JavaScript implementation. Only plugins enabled by plugins.js are loaded,
/// in configured order. Static compatibility classification is enforced against
/// the explicit host security policy before game-authored JavaScript is loaded.
/// </summary>
public sealed class WebScriptRuntime : IEngineScriptingRuntime, IDisposable
{
    private readonly IEmbeddedScriptVm _vm;
    private readonly string _languageId;
    private readonly IReadOnlyList<WebScriptInventoryEntry> _entries;
    private bool _loaded;
    private bool _bootstrapped;
    private bool _disposed;

    public WebScriptRuntime(
        string pLanguageId,
        IEmbeddedScriptVm pVm,
        IEnumerable<WebScriptInventoryEntry> pEntries)
    {
        if (pLanguageId is not (ScriptLanguageIds.RpgMakerMvJavaScript or ScriptLanguageIds.RpgMakerMzJavaScript))
        {
            throw new ArgumentException("Web script runtime requires an MV or MZ language ID.", nameof(pLanguageId));
        }
        _languageId = pLanguageId;
        _vm = pVm ?? throw new ArgumentNullException(nameof(pVm));
        if (pEntries == null) throw new ArgumentNullException(nameof(pEntries));
        _entries = pEntries
            .Where(pEntry => pEntry.Enabled)
            .OrderBy(pEntry => pEntry.Script.LoadOrder)
            .ToArray();
    }

    public IReadOnlyList<string> LanguageIds => new[] { _languageId };
    public ScriptRuntimeCapability Capabilities =>
        ScriptRuntimeCapability.Discover
        | ScriptRuntimeCapability.OrderedLoad
        | ScriptRuntimeCapability.Bootstrap
        | ScriptRuntimeCapability.HostHooks;
    public IReadOnlyList<EngineScriptDescriptor> Scripts => _entries.Select(pEntry => pEntry.Script).ToArray();

    public SdkOperationResult DiscoverScripts()
    {
        if (_disposed) return Disposed();
        foreach (var entry in _entries)
        {
            var validation = entry.Script.Validate();
            if (!validation.Success)
            {
                return SdkOperationResult.Failed(
                    "web.invalid-script-descriptor",
                    $"Plugin '{entry.Script.DisplayName}' has invalid metadata: {validation.ErrorMessage}");
            }
            if (!entry.Script.LanguageId.Equals(_languageId, StringComparison.Ordinal))
            {
                return SdkOperationResult.Failed(
                    "web.language-mismatch",
                    $"Plugin '{entry.Script.DisplayName}' targets '{entry.Script.LanguageId}' instead of '{_languageId}'.");
            }
            if (entry.Compatibility == WebScriptCompatibility.MissingFile)
            {
                return SdkOperationResult.Failed(
                    "web.plugin-file-missing",
                    $"Enabled plugin '{entry.Script.DisplayName}' has no source file.");
            }
            if (entry.Compatibility == WebScriptCompatibility.Truncated)
            {
                return SdkOperationResult.Failed(
                    "web.plugin-source-truncated",
                    $"Enabled plugin '{entry.Script.DisplayName}' could not be fully inspected.");
            }
        }
        return SdkOperationResult.Succeeded();
    }

    public SdkOperationResult LoadScripts(ScriptExecutionPolicy pPolicy)
    {
        if (_disposed) return Disposed();
        if (_loaded) return SdkOperationResult.Failed("web.already-loaded", "Web plugins were already loaded for this session.");
        if (pPolicy == null) return SdkOperationResult.Failed("web.policy-required", "A script execution policy is required.");

        var policyValidation = pPolicy.Validate();
        if (!policyValidation.Success) return policyValidation;
        var discovered = DiscoverScripts();
        if (!discovered.Success) return discovered;
        if (!_vm.LanguageId.Equals(_languageId, StringComparison.Ordinal))
        {
            return SdkOperationResult.Failed(
                "web.vm-language-mismatch",
                $"Embedded VM language '{_vm.LanguageId}' cannot host '{_languageId}' plugins.");
        }

        foreach (var entry in _entries)
        {
            var permission = CheckPolicy(entry, pPolicy);
            if (!permission.Success) return permission;
        }

        var configured = _vm.Configure(pPolicy);
        if (!configured.Success) return configured;

        foreach (var entry in _entries)
        {
            var source = ReadSource(entry);
            if (!source.Success || source.Value == null)
            {
                return SdkOperationResult.Failed(
                    "web.plugin-source-unavailable",
                    $"Plugin '{entry.Script.DisplayName}' source is unavailable to the runtime.");
            }
            var module = new ScriptModule
            {
                Descriptor = entry.Script,
                Source = source.Value,
            };
            var validation = module.Validate();
            if (!validation.Success) return validation;
            var loaded = _vm.LoadModule(module);
            if (!loaded.Success)
            {
                return SdkOperationResult.Failed(
                    "web.plugin-load-failed",
                    $"Failed to load plugin '{entry.Script.DisplayName}': {loaded.ErrorMessage}",
                    loaded.Diagnostics);
            }
        }

        _loaded = true;
        return SdkOperationResult.Succeeded(new[]
        {
            SdkDiagnostic.Info("web.plugins-loaded", $"Loaded {_entries.Count} enabled {_languageId} plugins in configured order."),
        });
    }

    public SdkOperationResult ExecuteBootstrap()
    {
        if (_disposed) return Disposed();
        if (!_loaded) return SdkOperationResult.Failed("web.not-loaded", "Web plugins must be loaded before bootstrap execution.");
        if (_bootstrapped) return SdkOperationResult.Failed("web.already-bootstrapped", "Web plugin bootstrap already ran for this session.");

        foreach (var entry in _entries)
        {
            var executed = _vm.ExecuteModule(entry.Script.Id);
            if (!executed.Success)
            {
                return SdkOperationResult.Failed(
                    "web.plugin-execution-failed",
                    $"Plugin '{entry.Script.DisplayName}' failed: {executed.ErrorMessage}",
                    executed.Diagnostics);
            }
        }
        _bootstrapped = true;
        return SdkOperationResult.Succeeded(new[]
        {
            SdkDiagnostic.Info("web.bootstrap-complete", $"Executed {_entries.Count} enabled {_languageId} plugins in configured order."),
        });
    }

    public SdkOperationResult InvokeHook(ScriptHookRequest pRequest)
    {
        if (_disposed) return Disposed();
        if (pRequest == null) return SdkOperationResult.Failed("web.hook-required", "A hook request is required.");
        var validation = pRequest.Validate();
        if (!validation.Success) return validation;
        if (!_loaded) return SdkOperationResult.Failed("web.not-loaded", "Web plugins must be loaded before host hooks are invoked.");

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

    private static SdkOperationResult CheckPolicy(WebScriptInventoryEntry pEntry, ScriptExecutionPolicy pPolicy)
    {
        return pEntry.Compatibility switch
        {
            WebScriptCompatibility.RequiresProcessExecution when !pPolicy.AllowProcessExecution =>
                SdkOperationResult.Failed(
                    "web.process-denied",
                    $"Plugin '{pEntry.Script.DisplayName}' requests process execution, denied by the current policy."),
            WebScriptCompatibility.RequiresNativeAddon when !pPolicy.AllowNativeInterop =>
                SdkOperationResult.Failed(
                    "web.native-addon-denied",
                    $"Plugin '{pEntry.Script.DisplayName}' requests native addon loading, denied by the current policy."),
            _ => SdkOperationResult.Succeeded(),
        };
    }

    private static ScriptSourceResult ReadSource(WebScriptInventoryEntry pEntry)
    {
        // Inventory is intentionally metadata-only. Executable source is supplied
        // by an engine/VFS integration later; this placeholder makes the boundary
        // explicit and prevents the metadata analyzer from becoming an executor.
        if (pEntry is IExecutableWebScriptInventoryEntry executable)
        {
            return new ScriptSourceResult(true, executable.Source);
        }
        return new ScriptSourceResult(false, null);
    }

    private static SdkOperationResult Disposed()
        => SdkOperationResult.Failed("web.disposed", "The web script runtime has been disposed.");

    private sealed record ScriptSourceResult(bool Success, ReadOnlyMemory<byte>? Value);
}

/// <summary>
/// Optional internal bridge used by the future VFS-backed runtime inventory.
/// Public metadata inventories intentionally do not expose executable bytes.
/// </summary>
public interface IExecutableWebScriptInventoryEntry
{
    ReadOnlyMemory<byte> Source { get; }
}
