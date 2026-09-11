using System;
using System.Collections.Generic;

namespace UniversalRPG.Sdk;

/// <summary>
/// Stable language/runtime identifiers. They describe compatibility targets,
/// not the implementation used internally by UniversalRPG.
/// </summary>
public static class ScriptLanguageIds
{
    public const string Rm2kEvent = "rm2k-event";
    public const string Rm2k3Event = "rm2k3-event";
    public const string Rgss1Ruby = "ruby-rgss1";
    public const string Rgss2Ruby = "ruby-rgss2";
    public const string Rgss3Ruby = "ruby-rgss3";
    public const string RpgMakerMvJavaScript = "javascript-rmmv";
    public const string RpgMakerMzJavaScript = "javascript-rmmz";
    public const string WolfEvent = "wolf-event";
    public const string NativePlugin = "native-plugin";
}

public enum ScriptOrigin
{
    EngineDefault,
    Game,
    Plugin,
    UserOverride,
    CompatibilityShim,
}

[Flags]
public enum ScriptRuntimeCapability
{
    None = 0,
    Discover = 1 << 0,
    OrderedLoad = 1 << 1,
    Bootstrap = 1 << 2,
    HostHooks = 1 << 3,
    DebugEvaluation = 1 << 4,
    FileSystem = 1 << 5,
    Network = 1 << 6,
    Clipboard = 1 << 7,
    NativeInterop = 1 << 8,
}

/// <summary>
/// Describes a script that belongs to the imported game/runtime. RelativePath
/// is informational and must always be interpreted through the runtime's VFS.
/// </summary>
public sealed class EngineScriptDescriptor
{
    public string Id { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public string LanguageId { get; init; } = "";
    public string RelativePath { get; init; } = "";
    public string Sha256 { get; init; } = "";
    public ScriptOrigin Origin { get; init; } = ScriptOrigin.Game;
    public bool Required { get; init; } = true;
    public int LoadOrder { get; init; }
    public IReadOnlyList<string> Dependencies { get; init; } = Array.Empty<string>();

    public SdkOperationResult Validate()
    {
        if (!IsStableId(Id, 160))
        {
            return SdkOperationResult.Failed("script.invalid-id", "Script IDs must be stable lowercase identifiers.");
        }
        if (!IsStableId(LanguageId, 96))
        {
            return SdkOperationResult.Failed("script.invalid-language", "Script language IDs must be stable lowercase identifiers.");
        }
        if (DisplayName.Length > 512 || RelativePath.Length > 2048 || Sha256.Length > 128)
        {
            return SdkOperationResult.Failed("script.metadata-too-long", "Script metadata exceeds bounded SDK limits.");
        }
        if (Dependencies == null || Dependencies.Count > 4096)
        {
            return SdkOperationResult.Failed("script.invalid-dependencies", "Script dependency metadata is missing or exceeds the bounded limit.");
        }
        foreach (var dependency in Dependencies)
        {
            if (!IsStableId(dependency, 160))
            {
                return SdkOperationResult.Failed("script.invalid-dependency", $"Invalid dependency ID '{dependency}'.");
            }
        }
        return SdkOperationResult.Succeeded();
    }

    private static bool IsStableId(string pValue, int pMaxLength)
    {
        if (string.IsNullOrWhiteSpace(pValue) || pValue.Length > pMaxLength)
        {
            return false;
        }
        for (var index = 0; index < pValue.Length; index++)
        {
            var character = pValue[index];
            var valid = (character >= 'a' && character <= 'z')
                || (character >= '0' && character <= '9')
                || character is '-' or '_' or '.' or ':' or '/';
            if (!valid)
            {
                return false;
            }
        }
        return true;
    }
}

/// <summary>
/// Security policy supplied by the host before game-authored code executes.
/// The safe default intentionally denies host-impacting capabilities.
/// </summary>
public sealed class ScriptExecutionPolicy
{
    public bool AllowReadGameFiles { get; init; } = true;
    public bool AllowWriteSaveFiles { get; init; } = true;
    public bool AllowWriteCacheFiles { get; init; } = true;
    public bool AllowArbitraryHostFileSystem { get; init; }
    public bool AllowNetwork { get; init; }
    public bool AllowClipboard { get; init; }
    public bool AllowProcessExecution { get; init; }
    public bool AllowNativeInterop { get; init; }
    public int MaxMemoryMegabytes { get; init; } = 256;
    public int MaxExecutionMillisecondsPerTick { get; init; } = 50;
    public int MaxCallDepth { get; init; } = 1024;

    public static ScriptExecutionPolicy SafeDefault { get; } = new();

    public SdkOperationResult Validate()
    {
        if (MaxMemoryMegabytes is < 16 or > 16384)
        {
            return SdkOperationResult.Failed("script.policy-memory", "Script memory limit must be between 16 and 16384 MiB.");
        }
        if (MaxExecutionMillisecondsPerTick is < 1 or > 60000)
        {
            return SdkOperationResult.Failed("script.policy-time", "Per-tick script execution limit must be between 1 ms and 60000 ms.");
        }
        if (MaxCallDepth is < 16 or > 65536)
        {
            return SdkOperationResult.Failed("script.policy-depth", "Script call-depth limit is outside the supported range.");
        }
        return SdkOperationResult.Succeeded();
    }
}

public sealed class ScriptHookRequest
{
    public string Hook { get; init; } = "";
    public IReadOnlyDictionary<string, string> Arguments { get; init; } = new Dictionary<string, string>();

    public SdkOperationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Hook) || Hook.Length > 256)
        {
            return SdkOperationResult.Failed("script.invalid-hook", "Script hook name is empty or too long.");
        }
        if (Arguments == null || Arguments.Count > 256)
        {
            return SdkOperationResult.Failed("script.invalid-hook-arguments", "Script hook arguments exceed the bounded limit.");
        }
        foreach (var argument in Arguments)
        {
            if (argument.Key.Length > 256 || argument.Value.Length > 8192)
            {
                return SdkOperationResult.Failed("script.hook-argument-too-long", "A script hook argument exceeds the bounded limit.");
            }
        }
        return SdkOperationResult.Succeeded();
    }
}

/// <summary>
/// Optional capability exposed by an engine runtime that can execute the
/// original engine's game-authored scripting model. Loading/execution must use
/// the engine's original ordering and compatibility semantics.
/// </summary>
public interface IEngineScriptingRuntime
{
    IReadOnlyList<string> LanguageIds { get; }
    ScriptRuntimeCapability Capabilities { get; }
    IReadOnlyList<EngineScriptDescriptor> Scripts { get; }

    /// <summary>Discovers scripts without executing them.</summary>
    SdkOperationResult DiscoverScripts();

    /// <summary>Loads scripts according to engine-defined ordering under an explicit policy.</summary>
    SdkOperationResult LoadScripts(ScriptExecutionPolicy pPolicy);

    /// <summary>Runs the engine-defined script bootstrap/initialization sequence.</summary>
    SdkOperationResult ExecuteBootstrap();

    /// <summary>
    /// Optional host hook used by tooling/debuggers/extensions. Engines that do
    /// not expose host-callable hooks should return a stable unsupported error.
    /// </summary>
    SdkOperationResult InvokeHook(ScriptHookRequest pRequest);
}
