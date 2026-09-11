using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Jint;
using UniversalRPG.Sdk;

namespace UniversalRPG.JavaScript.Jint;

/// <summary>
/// Pure-.NET JavaScript VM factory used as the first UniversalRPG MV/MZ VM
/// spike. This factory deliberately advertises only the JavaScript language
/// layer. Browser, NW.js and RPG Maker APIs must be supplied by higher-level
/// compatibility hosts before a full MV/MZ runtime profile can be accepted.
/// </summary>
public sealed class JintScriptVmFactory : IEmbeddedScriptVmFactory
{
    private static readonly string[] Languages =
    {
        ScriptLanguageIds.RpgMakerMvJavaScript,
        ScriptLanguageIds.RpgMakerMzJavaScript,
    };

    public string Id => "jint";
    public IReadOnlyList<string> LanguageIds => Languages;

    public SdkVmResult Create(ScriptVmRequest pRequest)
    {
        if (pRequest == null)
        {
            return SdkVmResult.Failed("jint.request-required", "A JavaScript VM request is required.");
        }
        if (!Languages.Contains(pRequest.LanguageId, StringComparer.Ordinal))
        {
            return SdkVmResult.Failed(
                "jint.language-unsupported",
                $"Jint adapter does not advertise language '{pRequest.LanguageId}'.");
        }
        if (pRequest.Policy == null)
        {
            return SdkVmResult.Failed("jint.policy-required", "A JavaScript execution policy is required.");
        }
        var policyValidation = pRequest.Policy.Validate();
        if (!policyValidation.Success)
        {
            return SdkVmResult.Failed(
                policyValidation.ErrorCode,
                policyValidation.ErrorMessage,
                policyValidation.Diagnostics);
        }

        var unsupported = (pRequest.RequiredFeatures ?? Array.Empty<string>())
            .Where(pFeature => !string.IsNullOrWhiteSpace(pFeature))
            .Where(pFeature => !pFeature.Equals("javascript", StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(pFeature => pFeature, StringComparer.Ordinal)
            .ToArray();
        if (unsupported.Length > 0)
        {
            return SdkVmResult.Failed(
                "jint.feature-unsupported",
                "Raw Jint VM does not yet provide required host features: " + string.Join(", ", unsupported) + ".");
        }

        var profile = pRequest.CompatibilityProfile ?? "";
        if (profile.Length > 128)
        {
            return SdkVmResult.Failed("jint.profile-invalid", "JavaScript compatibility profile name is too long.");
        }

        return SdkVmResult.Succeeded(
            new JintEmbeddedScriptVm(pRequest.LanguageId),
            new[]
            {
                SdkDiagnostic.Info(
                    "jint.javascript-core",
                    "Created a constrained JavaScript-only Jint VM. Browser/RPG Maker host APIs are not installed yet."),
            });
    }
}

/// <summary>
/// Constrained Jint-backed implementation of UniversalRPG's VM contract.
/// CLR exposure is never enabled. Game-authored JavaScript is decoded as strict
/// UTF-8 and each host entry runs under Jint's untrusted-code operation budget.
/// </summary>
public sealed class JintEmbeddedScriptVm : IEmbeddedScriptVm
{
    private const int MaxStoredModules = 4096;
    private const int DefaultMaxStatements = 5_000_000;
    private const uint DefaultMaxArraySize = 1_000_000;

    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    private readonly Dictionary<string, string> _modules = new(StringComparer.Ordinal);
    private Engine? _engine;
    private UntrustedCodeLimits? _limits;
    private ScriptExecutionPolicy _policy = ScriptExecutionPolicy.SafeDefault;
    private bool _disposed;

    public JintEmbeddedScriptVm(string pLanguageId)
    {
        if (pLanguageId is not (ScriptLanguageIds.RpgMakerMvJavaScript or ScriptLanguageIds.RpgMakerMzJavaScript))
        {
            throw new ArgumentException("Jint VM requires an MV or MZ JavaScript language ID.", nameof(pLanguageId));
        }
        LanguageId = pLanguageId;
    }

    public string LanguageId { get; }
    public ScriptVmState State { get; private set; } = ScriptVmState.Created;
    public ScriptExecutionPolicy Policy => _policy;

    public SdkOperationResult Configure(ScriptExecutionPolicy pPolicy)
    {
        if (_disposed) return Disposed();
        if (State is not (ScriptVmState.Created or ScriptVmState.Configured))
        {
            return SdkOperationResult.Failed("jint.configure-state", $"Jint VM cannot be configured from state {State}.");
        }
        return RebuildEngine(pPolicy);
    }

    public SdkOperationResult LoadModule(ScriptModule pModule)
    {
        if (_disposed) return Disposed();
        if (State is not (ScriptVmState.Configured or ScriptVmState.Ready))
        {
            return SdkOperationResult.Failed("jint.load-state", $"Jint VM cannot load modules from state {State}.");
        }
        if (pModule == null)
        {
            return SdkOperationResult.Failed("jint.module-required", "A JavaScript module is required.");
        }
        var validation = pModule.Validate();
        if (!validation.Success) return validation;
        if (!pModule.Descriptor.LanguageId.Equals(LanguageId, StringComparison.Ordinal))
        {
            return SdkOperationResult.Failed(
                "jint.module-language",
                $"Script '{pModule.Descriptor.Id}' targets '{pModule.Descriptor.LanguageId}', not '{LanguageId}'.");
        }
        if (_modules.Count >= MaxStoredModules && !_modules.ContainsKey(pModule.Descriptor.Id))
        {
            return SdkOperationResult.Failed("jint.module-limit", $"Jint VM cannot store more than {MaxStoredModules} modules.");
        }
        if (_modules.ContainsKey(pModule.Descriptor.Id))
        {
            return SdkOperationResult.Failed("jint.module-duplicate", $"Script module '{pModule.Descriptor.Id}' is already loaded.");
        }

        string source;
        try
        {
            source = StrictUtf8.GetString(pModule.Source.Span);
        }
        catch (DecoderFallbackException exception)
        {
            return SdkOperationResult.Failed(
                "jint.source-encoding",
                $"Script '{pModule.Descriptor.Id}' is not valid UTF-8: {exception.Message}");
        }

        _modules.Add(pModule.Descriptor.Id, source);
        State = ScriptVmState.Ready;
        return SdkOperationResult.Succeeded();
    }

    public SdkOperationResult ExecuteModule(string pScriptId)
    {
        if (_disposed) return Disposed();
        if (State is not (ScriptVmState.Ready or ScriptVmState.Running))
        {
            return SdkOperationResult.Failed("jint.execute-state", $"Jint VM cannot execute modules from state {State}.");
        }
        if (string.IsNullOrWhiteSpace(pScriptId) || !_modules.TryGetValue(pScriptId, out var source))
        {
            return SdkOperationResult.Failed("jint.module-missing", $"JavaScript module '{pScriptId}' is not loaded.");
        }
        if (_engine == null || _limits == null)
        {
            return SdkOperationResult.Failed("jint.not-configured", "Jint engine is unavailable.");
        }

        try
        {
            using (_limits.BeginOperation(_engine, CancellationToken.None))
            {
                _engine.Execute(source);
            }
            State = ScriptVmState.Running;
            return SdkOperationResult.Succeeded();
        }
        catch (Exception exception) when (!IsCritical(exception))
        {
            State = ScriptVmState.Faulted;
            return SdkOperationResult.Failed(
                ConstraintErrorCode(exception),
                $"JavaScript module '{pScriptId}' failed: {exception.Message}");
        }
    }

    public SdkOperationResult Invoke(ScriptInvocation pInvocation)
    {
        if (_disposed) return Disposed();
        if (State != ScriptVmState.Running)
        {
            return SdkOperationResult.Failed("jint.invoke-state", $"Jint VM cannot invoke script from state {State}.");
        }
        if (pInvocation == null || string.IsNullOrWhiteSpace(pInvocation.Member) || pInvocation.Member.Length > 512)
        {
            return SdkOperationResult.Failed("jint.invocation-invalid", "JavaScript invocation requires a bounded member name.");
        }
        if (_engine == null || _limits == null)
        {
            return SdkOperationResult.Failed("jint.not-configured", "Jint engine is unavailable.");
        }

        var arguments = (pInvocation.Arguments ?? Array.Empty<ScriptValue>())
            .Select(pValue => pValue?.Value)
            .ToArray();
        try
        {
            using (_limits.BeginOperation(_engine, CancellationToken.None))
            {
                if (string.IsNullOrWhiteSpace(pInvocation.Target)
                    || pInvocation.Target.Equals("globalThis", StringComparison.Ordinal))
                {
                    _engine.Invoke(pInvocation.Member, arguments);
                }
                else
                {
                    var target = _engine.GetValue(pInvocation.Target);
                    var member = _engine.GetValue(target, pInvocation.Member);
                    _engine.Invoke(member, arguments);
                }
            }
            return SdkOperationResult.Succeeded();
        }
        catch (Exception exception) when (!IsCritical(exception))
        {
            State = ScriptVmState.Faulted;
            return SdkOperationResult.Failed(
                ConstraintErrorCode(exception),
                $"JavaScript invocation '{pInvocation.Target}.{pInvocation.Member}' failed: {exception.Message}");
        }
    }

    public SdkOperationResult Reset()
    {
        if (_disposed) return Disposed();
        if (State == ScriptVmState.Created)
        {
            _modules.Clear();
            return SdkOperationResult.Succeeded();
        }
        return RebuildEngine(_policy);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _modules.Clear();
        DisposeEngine();
        State = ScriptVmState.Disposed;
    }

    private SdkOperationResult RebuildEngine(ScriptExecutionPolicy pPolicy)
    {
        if (pPolicy == null)
        {
            return SdkOperationResult.Failed("jint.policy-required", "A JavaScript execution policy is required.");
        }
        var validation = pPolicy.Validate();
        if (!validation.Success) return validation;
        if (pPolicy.AllowArbitraryHostFileSystem || pPolicy.AllowProcessExecution || pPolicy.AllowNativeInterop)
        {
            return SdkOperationResult.Failed(
                "jint.host-capability-unsupported",
                "Raw Jint VM never grants arbitrary filesystem, process, or native CLR access; those capabilities require explicit higher-level shims.");
        }

        DisposeEngine();
        _modules.Clear();
        _policy = pPolicy;
        _limits = CreateLimits(pPolicy);
        try
        {
            var options = new Options().ForUntrustedCode(_limits);
            _engine = new Engine(options);
            State = ScriptVmState.Configured;
            return SdkOperationResult.Succeeded(new[]
            {
                SdkDiagnostic.Info("jint.configured", "Jint VM configured with untrusted-code resource limits and CLR access disabled."),
            });
        }
        catch (Exception exception) when (!IsCritical(exception))
        {
            _limits = null;
            State = ScriptVmState.Faulted;
            return SdkOperationResult.Failed("jint.configure-failed", exception.Message);
        }
    }

    private void DisposeEngine()
    {
        _engine?.Dispose();
        _engine = null;
        _limits = null;
    }

    private static UntrustedCodeLimits CreateLimits(ScriptExecutionPolicy pPolicy)
    {
        var timeout = TimeSpan.FromMilliseconds(pPolicy.MaxExecutionMillisecondsPerTick);
        var operationTimeout = TimeSpan.FromMilliseconds(Math.Max(2L, (long)pPolicy.MaxExecutionMillisecondsPerTick * 2L));
        var memoryBytes = checked((long)pPolicy.MaxMemoryMegabytes * 1024L * 1024L);
        var requestedStatements = (long)pPolicy.MaxExecutionMillisecondsPerTick * 100_000L;
        var maxStatements = (int)Math.Clamp(requestedStatements, 10_000L, DefaultMaxStatements);
        return new UntrustedCodeLimits
        {
            TimeoutInterval = timeout,
            MaxStatements = maxStatements,
            MemoryLimit = memoryBytes,
            MaxRecursionDepth = pPolicy.MaxCallDepth,
            MaxArraySize = DefaultMaxArraySize,
            RegexTimeout = TimeSpan.FromMilliseconds(Math.Max(1, Math.Min(250, pPolicy.MaxExecutionMillisecondsPerTick))),
            PromiseTimeout = operationTimeout,
            MaxOperationDuration = operationTimeout,
        };
    }

    private static string ConstraintErrorCode(Exception pException)
    {
        return pException.GetType().Name switch
        {
            "MemoryLimitExceededException" => "jint.memory-limit",
            "StatementsCountOverflowException" => "jint.statement-limit",
            "TimeoutException" => "jint.timeout",
            "RecursionDepthOverflowException" => "jint.recursion-limit",
            "ParsingLimitException" => "jint.parsing-limit",
            _ => "jint.execution-failed",
        };
    }

    private static bool IsCritical(Exception pException)
        => pException is OutOfMemoryException or StackOverflowException;

    private static SdkOperationResult Disposed()
        => SdkOperationResult.Failed("jint.disposed", "The Jint VM has been disposed.");
}
