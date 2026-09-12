using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UniversalRPG.JavaScript.Jint;
using UniversalRPG.Sdk;

namespace UniversalRPG.Plugins;

/// <summary>
/// A bounded snapshot of the ORIGINAL project libraries, cores and plugins.js.
/// The application's renderer and startup entry point are not supplied here.
/// Content sources stay caller-owned; no file is modified or executed by Read.
/// </summary>
public sealed class NativeCoreScriptSet
{
    public const int MaxScriptBytes = 16 * 1024 * 1024;
    public const int MaxTotalSourceBytes = 32 * 1024 * 1024;
    public const int MaxStartupBytes = 512 * 1024;
    private static readonly UTF8Encoding Utf8 = new(false, true);

    public bool Success { get; private init; }
    public string Error { get; private init; } = "";
    public string EntryPointSha256 { get; private init; } = "";
    public string IndexSha256 { get; private init; } = "";
    public IReadOnlyList<ScriptModule> Modules { get; private init; } = Array.Empty<ScriptModule>();

    public static NativeCoreScriptSet Read(IGameContentSource content, string language, string prefix = "")
    {
        if (content == null) throw new ArgumentNullException(nameof(content));
        if (language is not (ScriptLanguageIds.RpgMakerMvJavaScript or ScriptLanguageIds.RpgMakerMzJavaScript))
            return Fail("boot.language-unsupported");
        try
        {
            // Normalize once. A caller selects a single root/www mount; this
            // method never searches a second root when one file is missing.
            if (prefix.Length != 0 && !LogicalGamePath.TryNormalize(prefix, out prefix))
                return Fail("boot.prefix-invalid");
            var index = ReadBytes(content, LogicalGamePath.Combine(prefix, "index.html"), MaxStartupBytes);
            var main = ReadBytes(content, LogicalGamePath.Combine(prefix, "js/main.js"), MaxStartupBytes);
            var manifest = NativeBootManifestParser.Parse(language == ScriptLanguageIds.RpgMakerMzJavaScript,
                Utf8.GetString(index).TrimStart('\uFEFF'), Utf8.GetString(main).TrimStart('\uFEFF'));
            if (!manifest.Success) return Fail(manifest.Error);
            var modules = new List<ScriptModule>(manifest.Paths.Count);
            long total = index.Length + main.Length;
            foreach (var path in manifest.Paths)
            {
                var logical = LogicalGamePath.Combine(prefix, path);
                var bytes = ReadBytes(content, logical, (int)Math.Min(MaxScriptBytes, MaxTotalSourceBytes - total));
                total += bytes.Length;
                // Validate text without evaluating it. Keep original bytes and
                // hashes, including a BOM, for provenance/identity diagnostics.
                _ = Utf8.GetCharCount(bytes);
                var descriptor = new EngineScriptDescriptor
                {
                    Id = $"core:{modules.Count:d4}", DisplayName = path,
                    LanguageId = language, RelativePath = logical,
                    Sha256 = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(),
                    Origin = ScriptOrigin.Game, Required = true, LoadOrder = modules.Count,
                };
                var validation = descriptor.Validate();
                if (!validation.Success) return Fail(validation.ErrorMessage);
                modules.Add(new ScriptModule { Descriptor = descriptor, Source = bytes });
            }
            return new NativeCoreScriptSet
            {
                Success = true, Modules = modules.AsReadOnly(),
                IndexSha256 = Convert.ToHexString(SHA256.HashData(index)).ToLowerInvariant(),
                EntryPointSha256 = Convert.ToHexString(SHA256.HashData(main)).ToLowerInvariant(),
            };
        }
        catch (BootReadException exception) { return Fail(exception.Message); }
        catch (DecoderFallbackException) { return Fail("boot.source-invalid-utf8"); }
        catch (Exception exception) when (exception is not OutOfMemoryException and not StackOverflowException)
        {
            // Providers may disclose native paths in their messages. Publish
            // only the exception type and engine-relative diagnostics above.
            return Fail("boot.source-failed: " + exception.GetType().Name);
        }
    }

    private static byte[] ReadBytes(IGameContentSource source, string path, int maxBytes)
    {
        if (maxBytes <= 0) throw new BootReadException("boot.source-total-limit");
        var result = source.Read(path);
        if (result == null || !result.Success) throw new BootReadException("boot.source-unavailable: " + path);
        if (result.Data.Length > maxBytes) throw new BootReadException("boot.source-size-limit: " + path);
        // Detach caller-owned memory before hashing/loading. The underlying VFS
        // must also bound allocation BEFORE Read returns; this is a second gate.
        return result.Data.ToArray();
    }

    private static NativeCoreScriptSet Fail(string error) => new() { Error = error };
    private sealed class BootReadException : Exception
    {
        public BootReadException(string message) : base(message) { }
    }
}
