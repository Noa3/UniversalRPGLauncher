using System;
using System.Collections.Generic;
using System.Linq;
using UniversalRPG.Sdk;

namespace UniversalRPG.Rgss;

/// <summary>
/// Ordered RGSS pipeline; no concrete Ruby VM is installed by this class.
/// Failed loads/bootstrap/hooks poison the session to prevent duplicated Ruby
/// aliases and initialization side effects. Recovery requires a fresh VM and
/// runtime. Use on one host thread; reentrant callbacks are explicitly refused.
/// </summary>
public sealed class RgssScriptRuntime : IEngineScriptingRuntime, IDisposable
{
    public const int MaxScripts = 100_000;
    private readonly IEmbeddedScriptVm _vm;
    private readonly IReadOnlyList<RgssScriptEntry> _entries;
    private readonly string _languageId;
    private bool _loaded;
    private bool _bootstrapped;
    private bool _faulted;
    private bool _busy;
    private bool _disposed;

    public RgssScriptRuntime(RgssGeneration pGeneration, IEmbeddedScriptVm pVm,
        IEnumerable<RgssScriptEntry> pEntries)
    {
        _languageId = pGeneration switch
        {
            RgssGeneration.Rgss1 => ScriptLanguageIds.Rgss1Ruby,
            RgssGeneration.Rgss2 => ScriptLanguageIds.Rgss2Ruby,
            RgssGeneration.Rgss3 => ScriptLanguageIds.Rgss3Ruby,
            _ => throw new ArgumentOutOfRangeException(nameof(pGeneration)),
        };
        _vm = pVm ?? throw new ArgumentNullException(nameof(pVm));
        if (pEntries == null) throw new ArgumentNullException(nameof(pEntries));
        var entries = pEntries.Take(MaxScripts + 1).ToArray();
        if (entries.Length > MaxScripts || entries.Any(entry => entry == null))
            throw new ArgumentException("RGSS entries are null or exceed the bounded limit.", nameof(pEntries));
        _entries = entries.OrderBy(entry => entry.ArchiveIndex).ToArray();
    }

    public IReadOnlyList<string> LanguageIds => new[] { _languageId };
    public ScriptRuntimeCapability Capabilities => ScriptRuntimeCapability.Discover
        | ScriptRuntimeCapability.OrderedLoad | ScriptRuntimeCapability.Bootstrap | ScriptRuntimeCapability.HostHooks;
    public IReadOnlyList<EngineScriptDescriptor> Scripts => _entries.Select(entry => entry.Descriptor).ToArray();

    public SdkOperationResult DiscoverScripts()
    {
        if (_disposed) return Disposed();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var indices = new HashSet<int>();
        foreach (var entry in _entries)
        {
            if (entry.Descriptor == null || entry.Source == null)
                return SdkOperationResult.Failed("rgss.invalid-script-descriptor", "RGSS entry lacks a descriptor or source.");
            var validation = new ScriptModule { Descriptor = entry.Descriptor, Source = entry.Source }.Validate();
            if (!validation.Success)
                return SdkOperationResult.Failed("rgss.invalid-script-descriptor", $"Script '{entry.Name}': {validation.ErrorMessage}");
            if (!entry.Descriptor.LanguageId.Equals(_languageId, StringComparison.Ordinal))
                return SdkOperationResult.Failed("rgss.language-mismatch", $"Script '{entry.Name}' targets a different language.");
            if (!ids.Add(entry.Descriptor.Id))
                return SdkOperationResult.Failed("rgss.duplicate-script-id", $"Duplicate script ID '{entry.Descriptor.Id}'.");
            if (entry.ArchiveIndex < 0 || !indices.Add(entry.ArchiveIndex))
                return SdkOperationResult.Failed("rgss.invalid-archive-order", "RGSS archive indices must be non-negative and unique.");
        }
        return SdkOperationResult.Succeeded();
    }

    public SdkOperationResult LoadScripts(ScriptExecutionPolicy pPolicy)
    {
        var gate = CheckSession();
        if (!gate.Success) return gate;
        if (_loaded) return SdkOperationResult.Failed("rgss.already-loaded", "RGSS scripts were already loaded.");
        if (pPolicy == null) return SdkOperationResult.Failed("rgss.policy-required", "A script execution policy is required.");
        var validation = pPolicy.Validate();
        if (!validation.Success) return validation;
        var discovered = DiscoverScripts();
        if (!discovered.Success) return discovered;
        if (!_vm.LanguageId.Equals(_languageId, StringComparison.Ordinal))
            return SdkOperationResult.Failed("rgss.vm-language-mismatch", "VM and RGSS generation languages do not match.");

        _busy = true;
        _faulted = true;
        var current = "VM configuration";
        try
        {
            var configured = _vm.Configure(pPolicy);
            if (!configured.Success) return configured;
            foreach (var entry in _entries)
            {
                current = entry.Descriptor.Id;
                var module = new ScriptModule { Descriptor = entry.Descriptor, Source = entry.Source };
                var loaded = _vm.LoadModule(module);
                if (!loaded.Success)
                    return SdkOperationResult.Failed("rgss.module-load-failed", $"Script #{entry.ArchiveIndex} '{entry.Name}': {loaded.ErrorMessage}", loaded.Diagnostics);
            }
            _loaded = true;
            _faulted = false;
            return SdkOperationResult.Succeeded(new[]
            {
                SdkDiagnostic.Info("rgss.scripts-loaded", $"Loaded {_entries.Count} {_languageId} scripts in archive order without executing them."),
            });
        }
        catch (Exception exception) when (!IsCritical(exception))
        {
            return ExternalFailure("load", current, exception);
        }
        finally { _busy = false; }
    }

    public SdkOperationResult ExecuteBootstrap()
    {
        var gate = CheckSession();
        if (!gate.Success) return gate;
        if (!_loaded) return SdkOperationResult.Failed("rgss.not-loaded", "Load scripts before bootstrap.");
        if (_bootstrapped) return SdkOperationResult.Failed("rgss.already-bootstrapped", "Bootstrap already completed.");
        _busy = true;
        _faulted = true;
        var current = "bootstrap";
        try
        {
            foreach (var entry in _entries)
            {
                current = entry.Descriptor.Id;
                var result = _vm.ExecuteModule(current);
                if (!result.Success)
                    return SdkOperationResult.Failed("rgss.script-execution-failed", $"Script #{entry.ArchiveIndex} '{entry.Name}': {result.ErrorMessage}", result.Diagnostics);
            }
            _bootstrapped = true;
            _faulted = false;
            return SdkOperationResult.Succeeded(new[]
            {
                SdkDiagnostic.Info("rgss.bootstrap-complete", $"Executed {_entries.Count} {_languageId} scripts in archive order."),
            });
        }
        catch (Exception exception) when (!IsCritical(exception))
        {
            return ExternalFailure("bootstrap", current, exception);
        }
        finally { _busy = false; }
    }

    public SdkOperationResult InvokeHook(ScriptHookRequest pRequest)
    {
        var gate = CheckSession();
        if (!gate.Success) return gate;
        if (pRequest == null) return SdkOperationResult.Failed("rgss.hook-required", "A hook request is required.");
        var validation = pRequest.Validate();
        if (!validation.Success) return validation;
        if (!_loaded) return SdkOperationResult.Failed("rgss.not-loaded", "Load scripts before invoking hooks.");
        if (!_bootstrapped) return SdkOperationResult.Failed("rgss.not-bootstrapped", "Bootstrap must complete before invoking hooks.");
        var arguments = pRequest.Arguments.OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => new ScriptValue(pair.Value)).ToArray();
        _busy = true;
        _faulted = true;
        try
        {
            var result = _vm.Invoke(new ScriptInvocation { Target = "UniversalRPG", Member = pRequest.Hook, Arguments = arguments });
            if (result.Success) _faulted = false;
            return result;
        }
        catch (Exception exception) when (!IsCritical(exception))
        {
            return ExternalFailure("hook", pRequest.Hook, exception);
        }
        finally { _busy = false; }
    }

    public void Dispose()
    {
        if (_disposed) return;
        if (_busy) throw new InvalidOperationException("Cannot dispose a script runtime from inside its active operation.");
        _disposed = true;
        _vm.Dispose();
    }

    private SdkOperationResult CheckSession()
    {
        if (_disposed) return Disposed();
        if (_busy) return SdkOperationResult.Failed("rgss.operation-in-progress", "A script operation is already in progress.");
        if (_faulted) return SdkOperationResult.Failed("rgss.session-faulted", "A previous script operation failed; create a fresh runtime and VM.");
        return SdkOperationResult.Succeeded();
    }
    private static SdkOperationResult ExternalFailure(string phase, string script, Exception exception)
        => SdkOperationResult.Failed("rgss.external-exception", $"{phase} failed at '{script}': {exception.GetType().Name}: {exception.Message}");
    private static bool IsCritical(Exception exception) => exception is OutOfMemoryException or StackOverflowException;
    private static SdkOperationResult Disposed() => SdkOperationResult.Failed("rgss.disposed", "The RGSS runtime has been disposed.");
}
