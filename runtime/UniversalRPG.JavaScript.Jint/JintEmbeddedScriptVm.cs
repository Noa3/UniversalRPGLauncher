using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using global::Jint;
using global::Jint.Native;
using UniversalRPG.Sdk;

namespace UniversalRPG.JavaScript.Jint;

/// <summary>
/// JavaScript-only factory. Browser and RPG Maker host features must be
/// implemented above this layer; recognizing a language is not a full runtime.
/// </summary>
public sealed class JintScriptVmFactory : IEmbeddedScriptVmFactory
{
    private static readonly IReadOnlyList<string> Languages = Array.AsReadOnly(new[]
    {
        ScriptLanguageIds.RpgMakerMvJavaScript,
        ScriptLanguageIds.RpgMakerMzJavaScript,
    });

    public string Id => "jint";
    public IReadOnlyList<string> LanguageIds => Languages;

    public SdkVmResult Create(ScriptVmRequest pRequest)
    {
        if (pRequest == null)
            return SdkVmResult.Failed("jint.request-required", "A JavaScript VM request is required.");
        if (!Languages.Contains(pRequest.LanguageId, StringComparer.Ordinal))
            return SdkVmResult.Failed("jint.language-unsupported", $"Jint does not advertise '{pRequest.LanguageId}'.");
        if (pRequest.Policy == null)
            return SdkVmResult.Failed("jint.policy-required", "A JavaScript execution policy is required.");

        var validation = pRequest.Policy.Validate();
        if (!validation.Success)
            return SdkVmResult.Failed(validation.ErrorCode, validation.ErrorMessage, validation.Diagnostics);

        var unsupported = (pRequest.RequiredFeatures ?? Array.Empty<string>())
            .Where(pFeature => !string.IsNullOrWhiteSpace(pFeature))
            .Where(pFeature => !pFeature.Equals("javascript", StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(pFeature => pFeature, StringComparer.Ordinal)
            .ToArray();
        if (unsupported.Length > 0)
            return SdkVmResult.Failed("jint.feature-unsupported", "Raw Jint VM does not provide: " + string.Join(", ", unsupported) + ".");
        if ((pRequest.CompatibilityProfile?.Length ?? 0) > 128)
            return SdkVmResult.Failed("jint.profile-invalid", "Compatibility profile name is too long.");

        return SdkVmResult.Succeeded(new JintEmbeddedScriptVm(pRequest.LanguageId), new[]
        {
            SdkDiagnostic.Info("jint.javascript-core", "Created a JavaScript-only VM; browser/RPG Maker APIs are not installed."),
        });
    }
}

/// <summary>
/// Jint 4.16.2 adapter. Each instance must be used by one host thread at a time.
/// Constraints and restricted host values reduce risk; an in-process VM is not
/// an operating-system sandbox. Dynamic string compilation remains disabled.
/// </summary>
public sealed partial class JintEmbeddedScriptVm : IEmbeddedScriptVm
{
    private const int MaxStoredModules = 4096;
    private const int DefaultMaxStatements = 5_000_000;
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);
    private readonly Dictionary<string, string> _modules = new(StringComparer.Ordinal);
    private Engine? _engine;
    private JsValue? _invokeBridge;
    private ScriptExecutionPolicy _policy = ScriptExecutionPolicy.SafeDefault;
    private bool _disposed;
    private bool _executing;
    private long _storedSourceBytes;

    public JintEmbeddedScriptVm(string pLanguageId)
    {
        if (pLanguageId is not (ScriptLanguageIds.RpgMakerMvJavaScript or ScriptLanguageIds.RpgMakerMzJavaScript))
            throw new ArgumentException("Jint VM requires an MV or MZ JavaScript language ID.", nameof(pLanguageId));
        LanguageId = pLanguageId;
    }

    public string LanguageId { get; }
    public ScriptVmState State { get; private set; } = ScriptVmState.Created;
    public ScriptExecutionPolicy Policy => _policy;

    public SdkOperationResult Configure(ScriptExecutionPolicy pPolicy)
    {
        if (_disposed) return Disposed();
        if (_executing) return SdkOperationResult.Failed("jint.operation-in-progress", "A VM execution is already in progress.");
        if (State is not (ScriptVmState.Created or ScriptVmState.Configured))
            return SdkOperationResult.Failed("jint.configure-state", $"Cannot configure from {State}.");
        return RebuildEngine(pPolicy);
    }

    public SdkOperationResult LoadModule(ScriptModule pModule)
    {
        if (_disposed) return Disposed();
        if (_executing) return SdkOperationResult.Failed("jint.operation-in-progress", "A VM execution is already in progress.");
        if (State is not (ScriptVmState.Configured or ScriptVmState.Ready))
            return SdkOperationResult.Failed("jint.load-state", $"Cannot load modules from {State}.");
        if (pModule == null || pModule.Descriptor == null)
            return SdkOperationResult.Failed("jint.module-required", "A module and descriptor are required.");
        var validation = pModule.Validate();
        if (!validation.Success) return validation;
        if (!pModule.Descriptor.LanguageId.Equals(LanguageId, StringComparison.Ordinal))
            return SdkOperationResult.Failed("jint.module-language", $"Script '{pModule.Descriptor.Id}' targets a different language.");
        if (_modules.ContainsKey(pModule.Descriptor.Id))
            return SdkOperationResult.Failed("jint.module-duplicate", $"Script '{pModule.Descriptor.Id}' is already loaded.");
        if (_modules.Count >= MaxStoredModules)
            return SdkOperationResult.Failed("jint.module-limit", $"Cannot store more than {MaxStoredModules} modules.");

        string source;
        long sourceBytes;
        try
        {
            // Source text is stored outside Jint's per-execution allocation
            // counter. Bound the aggregate before allocating the UTF-16 string.
            sourceBytes = (long)StrictUtf8.GetCharCount(pModule.Source.Span) * sizeof(char);
            var budget = (long)_policy.MaxMemoryMegabytes * 1024 * 1024;
            if (sourceBytes > budget - _storedSourceBytes)
                return SdkOperationResult.Failed("jint.source-memory-limit", "Stored module source exceeds the session source budget.");
            source = StrictUtf8.GetString(pModule.Source.Span);
        }
        catch (DecoderFallbackException)
        {
            return SdkOperationResult.Failed("jint.source-encoding", $"Script '{pModule.Descriptor.Id}' is not valid UTF-8.");
        }

        _modules.Add(pModule.Descriptor.Id, source);
        _storedSourceBytes += sourceBytes;
        State = ScriptVmState.Ready;
        return SdkOperationResult.Succeeded();
    }

    public SdkOperationResult ExecuteModule(string pScriptId)
    {
        if (_disposed) return Disposed();
        if (_executing) return SdkOperationResult.Failed("jint.operation-in-progress", "A VM execution is already in progress.");
        if (State is not (ScriptVmState.Ready or ScriptVmState.Running))
            return SdkOperationResult.Failed("jint.execute-state", $"Cannot execute modules from {State}.");
        if (string.IsNullOrWhiteSpace(pScriptId) || !_modules.TryGetValue(pScriptId, out var source))
            return SdkOperationResult.Failed("jint.module-missing", $"Script '{pScriptId}' is not loaded.");
        if (_engine == null)
            return SdkOperationResult.Failed("jint.not-configured", "Jint engine is unavailable.");
        try
        {
            _executing = true;
            _nativeGameData?.BeginExecution(_policy.AllowReadGameFiles);
            _engine.Execute(source);
            State = ScriptVmState.Running;
            return SdkOperationResult.Succeeded();
        }
        catch (Exception exception) when (!IsCritical(exception))
        {
            return ExecutionFailed(exception, $"JavaScript module '{pScriptId}'");
        }
        finally { _nativeGameData?.EndExecution(); _executing = false; }
    }

    public SdkOperationResult Invoke(ScriptInvocation pInvocation)
    {
        if (_disposed) return Disposed();
        if (_executing) return SdkOperationResult.Failed("jint.operation-in-progress", "A VM execution is already in progress.");
        if (State != ScriptVmState.Running)
            return SdkOperationResult.Failed("jint.invoke-state", $"Cannot invoke script from {State}.");
        if (_engine == null || _invokeBridge == null)
            return SdkOperationResult.Failed("jint.not-configured", "Jint engine is unavailable.");

        var validation = ScriptInvocationBridge.SerializeArguments(pInvocation, out var argumentsJson);
        if (!validation.Success) return validation;
        try
        {
            // Resolve the receiver AND method inside the constrained call. A
            // property getter is game code too. Never use host-side GetValue
            // followed by Invoke(member), which loses `this` and splits budgets.
            _executing = true;
            _nativeGameData?.BeginExecution(_policy.AllowReadGameFiles);
            _engine.Invoke(_invokeBridge, pInvocation.Target ?? "", pInvocation.Member, argumentsJson);
            return SdkOperationResult.Succeeded();
        }
        catch (Exception exception) when (!IsCritical(exception))
        {
            return ExecutionFailed(exception, $"JavaScript invocation '{pInvocation.Target}.{pInvocation.Member}'");
        }
        finally { _nativeGameData?.EndExecution(); _executing = false; }
    }

    public SdkOperationResult Reset()
    {
        if (_disposed) return Disposed();
        if (_executing) return SdkOperationResult.Failed("jint.operation-in-progress", "A VM execution is already in progress.");
        if (State == ScriptVmState.Created)
        {
            _modules.Clear();
            _storedSourceBytes = 0;
            return SdkOperationResult.Succeeded();
        }
        return RebuildEngine(_policy);
    }

    public void Dispose()
    {
        if (_disposed) return;
        if (_executing) throw new InvalidOperationException("Cannot dispose a VM during execution.");
        _disposed = true;
        _modules.Clear();
        _storedSourceBytes = 0;
        DisposeEngine();
        State = ScriptVmState.Disposed;
    }

    private SdkOperationResult RebuildEngine(ScriptExecutionPolicy pPolicy)
    {
        if (pPolicy == null)
            return SdkOperationResult.Failed("jint.policy-required", "A JavaScript execution policy is required.");
        var validation = pPolicy.Validate();
        if (!validation.Success) return validation;
        if (pPolicy.AllowArbitraryHostFileSystem || pPolicy.AllowProcessExecution || pPolicy.AllowNativeInterop)
            return SdkOperationResult.Failed("jint.host-capability-unsupported", "Arbitrary filesystem, process and native access require separate host shims.");

        DisposeEngine();
        _modules.Clear();
        _storedSourceBytes = 0;
        _policy = pPolicy;
        try
        {
            var options = new Options()
                .LimitMemory((long)pPolicy.MaxMemoryMegabytes * 1024 * 1024)
                .TimeoutInterval(TimeSpan.FromMilliseconds(pPolicy.MaxExecutionMillisecondsPerTick))
                .MaxStatements((int)Math.Clamp((long)pPolicy.MaxExecutionMillisecondsPerTick * 100_000, 10_000, DefaultMaxStatements))
                .LimitRecursion(pPolicy.MaxCallDepth)
                .DisableStringCompilation();
            options.Interop.Enabled = false;
            options.Interop.AllowGetType = false;
            options.Interop.AllowSystemReflection = false;
            options.Interop.AllowWrite = false;
            options.AgentCanSuspend = false;
            options.Constraints.StackOverflowGuard = true;

            _engine = new Engine(options);
            _invokeBridge = _engine.Evaluate(ScriptInvocationBridge.Source);
            InstallNativeGameData();
            State = ScriptVmState.Configured;
            return SdkOperationResult.Succeeded(new[]
            {
                SdkDiagnostic.Info("jint.configured", "Jint 4.16.2 configured with execution limits, primitive-only invocation and CLR access disabled."),
            });
        }
        catch (Exception exception) when (!IsCritical(exception))
        {
            DisposeEngine();
            State = ScriptVmState.Faulted;
            return SdkOperationResult.Failed("jint.configure-failed", exception.Message);
        }
    }

    private SdkOperationResult ExecutionFailed(Exception exception, string context)
    {
        State = ScriptVmState.Faulted;
        return SdkOperationResult.Failed(ConstraintErrorCode(exception), $"{context} failed: {exception.Message}");
    }

    private void DisposeEngine()
    {
        _nativeGameData?.EndExecution();
        _invokeBridge = null;
        _engine?.Dispose();
        _engine = null;
    }

    private static string ConstraintErrorCode(Exception exception) => exception.GetType().Name switch
    {
        "MemoryLimitExceededException" => "jint.memory-limit",
        "StatementsCountOverflowException" => "jint.statement-limit",
        "TimeoutException" => "jint.timeout",
        "RecursionDepthOverflowException" => "jint.recursion-limit",
        "ParsingLimitException" => "jint.parsing-limit",
        _ => "jint.execution-failed",
    };

    private static bool IsCritical(Exception exception) => exception is OutOfMemoryException or StackOverflowException;
    private static SdkOperationResult Disposed() => SdkOperationResult.Failed("jint.disposed", "The Jint VM has been disposed.");
}
