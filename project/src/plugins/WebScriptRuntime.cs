using System;
using System.Collections.Generic;
using System.Linq;
using UniversalRPG.Sdk;

namespace UniversalRPG.Plugins;

/// <summary>Reads executable bytes from a host-authorized game/VFS mount.</summary>
public interface IWebScriptSourceProvider
{
    ScriptSourceResult Read(EngineScriptDescriptor pScript);
}

public sealed class ScriptSourceResult
{
    private ScriptSourceResult(bool pSuccess, ReadOnlyMemory<byte> pSource, string pError)
    {
        Success = pSuccess;
        Source = pSource;
        Error = pError;
    }
    public bool Success { get; }
    public ReadOnlyMemory<byte> Source { get; }
    public string Error { get; }
    public static ScriptSourceResult Succeeded(ReadOnlyMemory<byte> pSource) => new(true, pSource, "");
    public static ScriptSourceResult Failed(string pError)
        => new(false, ReadOnlyMemory<byte>.Empty, pError ?? "Plugin source unavailable.");
}

/// <summary>
/// Ordered MV/MZ plugin pipeline. A failed VM load, bootstrap or hook poisons
/// this session: callers must create a fresh runtime AND VM rather than replay
/// earlier plugins into a partially initialized realm. Use on one host thread;
/// the busy flag rejects reentrant callbacks, not arbitrary concurrent access.
/// </summary>
public sealed partial class WebScriptRuntime : IEngineScriptingRuntime, IDisposable
{
    public const int MaxPreludeModules = 128;
    private readonly IEmbeddedScriptVm _vm;
    private readonly IWebScriptSourceProvider _sourceProvider;
    private readonly string _languageId;
    private readonly IReadOnlyList<WebScriptInventoryEntry> _entries;
    private readonly IReadOnlyList<ScriptModule> _preludeModules;
    private bool _loaded;
    private bool _bootstrapped;
    private bool _faulted;
    private bool _busy;
    private bool _disposed;

    public WebScriptRuntime(string pLanguageId, IEmbeddedScriptVm pVm,
        IWebScriptSourceProvider pSourceProvider, IEnumerable<WebScriptInventoryEntry> pEntries,
        IEnumerable<ScriptModule>? pPreludeModules = null)
        : this(pLanguageId, pVm, pSourceProvider, pEntries, pPreludeModules, null)
    {
    }

    public WebScriptRuntime(string pLanguageId, IEmbeddedScriptVm pVm,
        IWebScriptSourceProvider pSourceProvider, IEnumerable<WebScriptInventoryEntry> pEntries,
        IEnumerable<ScriptModule>? pPreludeModules, WebBrowserHostOptions? pBrowserHostOptions)
    {
        if (pLanguageId is not (ScriptLanguageIds.RpgMakerMvJavaScript or ScriptLanguageIds.RpgMakerMzJavaScript))
            throw new ArgumentException("Web script runtime requires an MV or MZ language ID.", nameof(pLanguageId));
        _languageId = pLanguageId;
        _vm = pVm ?? throw new ArgumentNullException(nameof(pVm));
        _sourceProvider = pSourceProvider ?? throw new ArgumentNullException(nameof(pSourceProvider));
        _entries = WebPluginLoadPlan.SelectEnabled(pEntries);
        var preludes = (pPreludeModules ?? Array.Empty<ScriptModule>()).Take(MaxPreludeModules + 1).ToArray();
        if (preludes.Length + (pBrowserHostOptions == null ? 0 : 1) > MaxPreludeModules)
            throw new ArgumentException("Compatibility preludes exceed the bounded limit.", nameof(pPreludeModules));
        _browserHostOptions = pBrowserHostOptions;
        _preludeModules = pBrowserHostOptions == null ? preludes
            : new[] { WebBrowserHostPrelude.Build(_languageId, pBrowserHostOptions) }.Concat(preludes).ToArray();
    }

    public IReadOnlyList<string> LanguageIds => new[] { _languageId };
    public ScriptRuntimeCapability Capabilities => ScriptRuntimeCapability.Discover
        | ScriptRuntimeCapability.OrderedLoad | ScriptRuntimeCapability.Bootstrap | ScriptRuntimeCapability.HostHooks;
    public IReadOnlyList<EngineScriptDescriptor> Scripts => _preludeModules
        .Where(module => module != null).Select(module => module.Descriptor)
        .Concat(_entries.Select(entry => entry.Script)).ToArray();

    /// <summary>
    /// Returns the configured snapshot, not later mutations in the JS realm.
    /// The executable load plan supplies parameters. Exact duplicate names are
    /// skipped after the first enabled entry; distinct names differing only by
    /// case share a lowercased parameter key, as in the original PluginManager.
    /// </summary>
    public IReadOnlyDictionary<string, string> GetPluginParameters(string pPluginName)
    {
        if (string.IsNullOrEmpty(pPluginName)) return new Dictionary<string, string>();
        for (var index = _entries.Count - 1; index >= 0; index--)
            if (_entries[index].Script.DisplayName.Equals(pPluginName, StringComparison.OrdinalIgnoreCase))
                return _entries[index].Parameters;
        return new Dictionary<string, string>();
    }

    public SdkOperationResult DiscoverScripts()
    {
        if (_disposed) return Disposed();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var module in _preludeModules)
        {
            if (module == null || module.Descriptor == null)
                return SdkOperationResult.Failed("web.prelude-null", "Compatibility prelude or descriptor is null.");
            var validation = module.Validate();
            if (!validation.Success)
                return SdkOperationResult.Failed("web.invalid-prelude", validation.ErrorMessage);
            if (!module.Descriptor.LanguageId.Equals(_languageId, StringComparison.Ordinal))
                return SdkOperationResult.Failed("web.prelude-language-mismatch", $"Prelude '{module.Descriptor.Id}' targets a different language.");
            if (!ids.Add(module.Descriptor.Id))
                return SdkOperationResult.Failed("web.duplicate-script-id", $"Duplicate script ID '{module.Descriptor.Id}'.");
        }
        foreach (var entry in _entries)
        {
            var validation = entry.Script.Validate();
            if (!validation.Success)
                return SdkOperationResult.Failed("web.invalid-script-descriptor", validation.ErrorMessage);
            if (!entry.Script.LanguageId.Equals(_languageId, StringComparison.Ordinal))
                return SdkOperationResult.Failed("web.language-mismatch", $"Plugin '{entry.Script.DisplayName}' targets a different language.");
            if (!ids.Add(entry.Script.Id))
                return SdkOperationResult.Failed("web.duplicate-script-id", $"Duplicate script ID '{entry.Script.Id}'.");
            if (entry.Compatibility == WebScriptCompatibility.MissingFile)
                return SdkOperationResult.Failed("web.plugin-file-missing", $"Enabled plugin '{entry.Script.DisplayName}' has no source file.");
            if (entry.Compatibility == WebScriptCompatibility.Truncated)
                return SdkOperationResult.Failed("web.plugin-source-truncated", $"Enabled plugin '{entry.Script.DisplayName}' was not fully inspected.");
        }
        return SdkOperationResult.Succeeded();
    }

    public SdkOperationResult LoadScripts(ScriptExecutionPolicy pPolicy)
    {
        var gate = CheckSession();
        if (!gate.Success) return gate;
        if (_loaded) return SdkOperationResult.Failed("web.already-loaded", "Plugins were already loaded.");
        if (pPolicy == null) return SdkOperationResult.Failed("web.policy-required", "A script execution policy is required.");
        var validation = pPolicy.Validate();
        if (!validation.Success) return validation;
        var discovered = DiscoverScripts();
        if (!discovered.Success) return discovered;
        if (!_vm.LanguageId.Equals(_languageId, StringComparison.Ordinal))
            return SdkOperationResult.Failed("web.vm-language-mismatch", "VM and plugin languages do not match.");
        foreach (var entry in _entries)
        {
            var permission = CheckPolicy(entry, pPolicy);
            if (!permission.Success) return permission;
        }

        // VM/provider calls can mutate state even when they return a failure.
        // Mark the attempt before the first external call, not after success.
        _busy = true;
        _faulted = true;
        var current = "VM configuration";
        try
        {
            var configured = _vm.Configure(pPolicy);
            if (!configured.Success) return configured;
            foreach (var module in _preludeModules)
            {
                current = module.Descriptor.Id;
                var loaded = _vm.LoadModule(module);
                if (!loaded.Success)
                    return SdkOperationResult.Failed("web.prelude-load-failed", $"Prelude '{current}': {loaded.ErrorMessage}", loaded.Diagnostics);
            }
            foreach (var entry in _entries)
            {
                current = entry.Script.Id;
                var source = _sourceProvider.Read(entry.Script);
                if (source == null || !source.Success)
                    return SdkOperationResult.Failed("web.plugin-source-unavailable", $"Plugin '{current}': {source?.Error ?? "provider returned null"}");
                var module = new ScriptModule { Descriptor = entry.Script, Source = source.Source };
                validation = module.Validate();
                if (!validation.Success) return validation;
                var loaded = _vm.LoadModule(module);
                if (!loaded.Success)
                    return SdkOperationResult.Failed("web.plugin-load-failed", $"Plugin '{current}': {loaded.ErrorMessage}", loaded.Diagnostics);
            }
            _loaded = true;
            _faulted = false;
            return SdkOperationResult.Succeeded(new[]
            {
                SdkDiagnostic.Info("web.plugins-loaded", $"Loaded {_preludeModules.Count} preludes and {_entries.Count} enabled plugins without executing them."),
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
        if (!_loaded) return SdkOperationResult.Failed("web.not-loaded", "Load plugins before bootstrap.");
        if (_bootstrapped) return SdkOperationResult.Failed("web.already-bootstrapped", "Bootstrap already completed.");
        _busy = true;
        _faulted = true;
        var current = "bootstrap";
        try
        {
            foreach (var module in _preludeModules)
            {
                current = module.Descriptor.Id;
                var result = ExecutePreludeModule(module);
                if (!result.Success)
                    return SdkOperationResult.Failed("web.prelude-execution-failed",
                        $"Prelude '{current}' ({module.Descriptor.RelativePath}): {result.ErrorMessage}", result.Diagnostics);
            }
            foreach (var entry in _entries)
            {
                current = entry.Script.Id;
                var entered = EnterPluginScript(entry.Script.RelativePath);
                if (!entered.Success)
                    return SdkOperationResult.Failed("web.script-scope-failed", $"Plugin '{current}': {entered.ErrorMessage}", entered.Diagnostics);
                var result = _vm.ExecuteModule(current);
                if (!result.Success)
                    return SdkOperationResult.Failed("web.plugin-execution-failed", $"Plugin '{current}': {result.ErrorMessage}", result.Diagnostics);
                var left = LeavePluginScript();
                if (!left.Success)
                    return SdkOperationResult.Failed("web.script-scope-failed", $"Plugin '{current}': {left.ErrorMessage}", left.Diagnostics);
            }
            _bootstrapped = true;
            _faulted = false;
            return SdkOperationResult.Succeeded(new[]
            {
                SdkDiagnostic.Info("web.bootstrap-complete", $"Executed {_preludeModules.Count} preludes and {_entries.Count} plugins in configured order."),
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
        if (pRequest == null) return SdkOperationResult.Failed("web.hook-required", "A hook request is required.");
        var validation = pRequest.Validate();
        if (!validation.Success) return validation;
        if (!_loaded) return SdkOperationResult.Failed("web.not-loaded", "Load plugins before invoking hooks.");
        if (!_bootstrapped) return SdkOperationResult.Failed("web.not-bootstrapped", "Bootstrap must complete before invoking hooks.");
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
            _faulted = true;
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
        if (_busy) return SdkOperationResult.Failed("web.operation-in-progress", "A script operation is already in progress.");
        if (_faulted) return SdkOperationResult.Failed("web.session-faulted", "A previous script operation failed; create a fresh runtime and VM.");
        return SdkOperationResult.Succeeded();
    }

    private static SdkOperationResult CheckPolicy(WebScriptInventoryEntry entry, ScriptExecutionPolicy policy) => entry.Compatibility switch
    {
        WebScriptCompatibility.RequiresProcessExecution when !policy.AllowProcessExecution =>
            SdkOperationResult.Failed("web.process-denied", $"Plugin '{entry.Script.DisplayName}' requires denied process access."),
        WebScriptCompatibility.RequiresNativeAddon when !policy.AllowNativeInterop =>
            SdkOperationResult.Failed("web.native-addon-denied", $"Plugin '{entry.Script.DisplayName}' requires denied native access."),
        _ => SdkOperationResult.Succeeded(),
    };

    private static SdkOperationResult ExternalFailure(string phase, string script, Exception exception)
        => SdkOperationResult.Failed("web.external-exception", $"{phase} failed at '{script}': {exception.GetType().Name}: {exception.Message}");
    private static bool IsCritical(Exception exception) => exception is OutOfMemoryException or StackOverflowException;
    private static SdkOperationResult Disposed() => SdkOperationResult.Failed("web.disposed", "The web runtime has been disposed.");
}
