using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UniversalRPG.JavaScript.Jint;
using UniversalRPG.Sdk;

namespace UniversalRPG.Plugins;

/// <summary>
/// A bounded snapshot of the ORIGINAL project libraries, cores, plugins.js and
/// main.js. Read never executes imported content. The entry point is retained as
/// a separate module so the host can prove core/plugin initialization first.
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
    public ScriptModule? EntryPoint { get; private init; }
    public IReadOnlyList<ScriptModule> Modules { get; private init; } = Array.Empty<ScriptModule>();

    public static NativeCoreScriptSet Read(IGameContentSource content, string language, string prefix = "")
    {
        if (content == null) throw new ArgumentNullException(nameof(content));
        if (language is not (ScriptLanguageIds.RpgMakerMvJavaScript or ScriptLanguageIds.RpgMakerMzJavaScript))
            return Fail("boot.language-unsupported");
        try
        {
            if (prefix.Length != 0 && !LogicalGamePath.TryNormalize(prefix, out prefix))
                return Fail("boot.prefix-invalid");
            var indexPath = LogicalGamePath.Combine(prefix, "index.html");
            var mainPath = LogicalGamePath.Combine(prefix, "js/main.js");
            var index = ReadBytes(content, indexPath, MaxStartupBytes);
            var main = ReadBytes(content, mainPath, MaxStartupBytes);
            var indexText = Utf8.GetString(index).TrimStart('\uFEFF');
            var mainText = Utf8.GetString(main).TrimStart('\uFEFF');
            var manifest = NativeBootManifestParser.Parse(language == ScriptLanguageIds.RpgMakerMzJavaScript,
                indexText, mainText);
            if (!manifest.Success) return Fail(manifest.Error);

            var modules = new List<ScriptModule>(manifest.Paths.Count);
            long total = index.Length + main.Length;
            foreach (var path in manifest.Paths)
            {
                var logical = LogicalGamePath.Combine(prefix, path);
                var remaining = MaxTotalSourceBytes - total;
                if (remaining <= 0) return Fail("boot.source-total-limit");
                var bytes = ReadBytes(content, logical, (int)Math.Min(MaxScriptBytes, remaining));
                total += bytes.Length;
                _ = Utf8.GetCharCount(bytes);
                var descriptor = new EngineScriptDescriptor
                {
                    Id = $"core:{modules.Count:d4}", DisplayName = path,
                    LanguageId = language, RelativePath = logical,
                    Sha256 = Sha(bytes), Origin = ScriptOrigin.Game,
                    Required = true, LoadOrder = modules.Count,
                };
                var validation = descriptor.Validate();
                if (!validation.Success) return Fail(validation.ErrorMessage);
                modules.Add(new ScriptModule { Descriptor = descriptor, Source = bytes });
            }

            var entryDescriptor = new EngineScriptDescriptor
            {
                Id = "entry:main", DisplayName = "js/main.js", LanguageId = language,
                RelativePath = mainPath, Sha256 = Sha(main), Origin = ScriptOrigin.Game,
                Required = true, LoadOrder = int.MaxValue,
            };
            var entryValidation = entryDescriptor.Validate();
            if (!entryValidation.Success) return Fail(entryValidation.ErrorMessage);
            var entry = new ScriptModule { Descriptor = entryDescriptor, Source = main };
            var moduleValidation = entry.Validate();
            if (!moduleValidation.Success) return Fail(moduleValidation.ErrorMessage);

            return new NativeCoreScriptSet
            {
                Success = true,
                Modules = modules.AsReadOnly(),
                EntryPoint = entry,
                IndexSha256 = Sha(index),
                EntryPointSha256 = entryDescriptor.Sha256,
            };
        }
        catch (BootReadException exception) { return Fail(exception.Message); }
        catch (DecoderFallbackException) { return Fail("boot.source-invalid-utf8"); }
        catch (Exception exception) when (exception is not OutOfMemoryException and not StackOverflowException)
        {
            return Fail("boot.source-failed: " + exception.GetType().Name);
        }
    }

    private static byte[] ReadBytes(IGameContentSource source, string path, int maxBytes)
    {
        if (maxBytes <= 0) throw new BootReadException("boot.source-total-limit");
        var result = source.Read(path);
        if (result == null || !result.Success) throw new BootReadException("boot.source-unavailable: " + path);
        if (result.Data.Length > maxBytes) throw new BootReadException("boot.source-size-limit: " + path);
        return result.Data.ToArray();
    }

    private static string Sha(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    private static NativeCoreScriptSet Fail(string error) => new() { Error = error };
    private sealed class BootReadException : Exception
    {
        public BootReadException(string message) : base(message) { }
    }
}
