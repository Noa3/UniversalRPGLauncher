using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using UniversalRPG.JavaScript.Jint;
using UniversalRPG.Plugins;
using UniversalRPG.Sdk;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

public sealed class TestNativeCoreScriptSet : TestBase
{
    public void Test_MvOriginalFileOrderAndBytesAreRetained()
    {
        var content = Fixture(false);
        var result = NativeCoreScriptSet.Read(content, ScriptLanguageIds.RpgMakerMvJavaScript);
        Good(result);
        AssertEq(result.Modules.Count, 8);
        AssertEq(result.Modules[0].Descriptor.RelativePath, "js/libs/pixi.js");
        AssertEq(result.Modules[7].Descriptor.RelativePath, "js/plugins.js");
        AssertTrue(result.Modules.All(m => m.Source.Span.SequenceEqual(content.Files[m.Descriptor.RelativePath])));
        AssertEq(result.EntryPointSha256.Length, 64);
        AssertEq(result.IndexSha256.Length, 64);
        AssertFalse(result.Modules.Any(m => m.Descriptor.RelativePath == "js/main.js"));
    }

    public void Test_MzUsesDeclaredLibrariesWithoutExecutingMain()
    {
        var content = Fixture(true);
        var result = NativeCoreScriptSet.Read(content, ScriptLanguageIds.RpgMakerMzJavaScript);
        Good(result);
        AssertEq(result.Modules.Count, 8);
        AssertEq(result.Modules[1].Descriptor.RelativePath, "js/rmmz_core.js");
        // main.js deliberately throws after its literal declaration in the
        // fixture. Preparing a manifest must NOT evaluate that entry point.
    }

    public void Test_WwwPrefixIsAppliedToEveryRead()
    {
        var content = Fixture(false, "www/");
        var result = NativeCoreScriptSet.Read(content, ScriptLanguageIds.RpgMakerMvJavaScript, "www");
        Good(result);
        AssertTrue(content.ReadPaths.All(p => p.StartsWith("www/", StringComparison.Ordinal)));
        AssertTrue(result.Modules.All(m => m.Descriptor.RelativePath.StartsWith("www/", StringComparison.Ordinal)));
    }

    public void Test_MissingLibraryDoesNotFallBackToAnotherRoot()
    {
        var content = Fixture(false, "www/");
        content.Files.Remove("www/js/libs/pixi.js");
        content.Files["js/libs/pixi.js"] = Encoding.UTF8.GetBytes("wrong project");
        var result = NativeCoreScriptSet.Read(content, ScriptLanguageIds.RpgMakerMvJavaScript, "www");
        AssertFalse(result.Success);
        AssertTrue(result.Error.Contains("www/js/libs/pixi.js", StringComparison.Ordinal));
        AssertFalse(content.ReadPaths.Contains("js/libs/pixi.js"));
    }

    public void Test_ProviderOwnedBytesAreDetached()
    {
        var content = Fixture(false);
        var result = NativeCoreScriptSet.Read(content, ScriptLanguageIds.RpgMakerMvJavaScript);
        Good(result);
        var old = result.Modules[0].Source.ToArray();
        Array.Fill(content.Files["js/libs/pixi.js"], (byte)0);
        AssertTrue(result.Modules[0].Source.Span.SequenceEqual(old));
    }

    public void Test_OversizedStartupFileIsRejected()
    {
        var content = Fixture(false);
        content.Files["index.html"] = new byte[NativeCoreScriptSet.MaxStartupBytes + 1];
        AssertFalse(NativeCoreScriptSet.Read(content, ScriptLanguageIds.RpgMakerMvJavaScript).Success);
    }

    public void Test_OversizedSourceAndAggregateBudgetAreRejected()
    {
        var content = Fixture(false);
        content.Files["js/libs/pixi.js"] = new byte[NativeCoreScriptSet.MaxScriptBytes + 1];
        AssertFalse(NativeCoreScriptSet.Read(content, ScriptLanguageIds.RpgMakerMvJavaScript).Success);
        // Shared provider memory is intentionally reused; the loader's retained
        // source budget still counts each original script independently.
        var bytes = Encoding.UTF8.GetBytes(new string(' ', 5 * 1024 * 1024));
        foreach (var path in content.Files.Keys.Where(p => p.EndsWith(".js", StringComparison.Ordinal) && p != "js/main.js").ToArray())
            content.Files[path] = bytes;
        AssertFalse(NativeCoreScriptSet.Read(content, ScriptLanguageIds.RpgMakerMvJavaScript).Success);
    }

    public void Test_InvalidUtf8InActualSourceIsRejected()
    {
        var content = Fixture(false);
        content.Files["js/rpg_objects.js"] = new byte[] { 0xC3, 0x28 };
        var result = NativeCoreScriptSet.Read(content, ScriptLanguageIds.RpgMakerMvJavaScript);
        AssertFalse(result.Success);
        AssertEq(result.Error, "boot.source-invalid-utf8");
    }

    public void Test_ProviderExceptionsDoNotExposeNativePaths()
    {
        var content = Fixture(false);
        content.ThrowOnRead = true;
        var result = NativeCoreScriptSet.Read(content, ScriptLanguageIds.RpgMakerMvJavaScript);
        AssertFalse(result.Success);
        AssertFalse(result.Error.Contains("secret", StringComparison.Ordinal));
    }

    public void Test_UnsupportedEngineDoesNotReadFiles()
    {
        var content = Fixture(false);
        AssertFalse(NativeCoreScriptSet.Read(content, ScriptLanguageIds.Rgss1Ruby).Success);
        AssertEq(content.ReadPaths.Count, 0);
    }

    public void Test_CallerKeepsMountOwnership()
    {
        var content = Fixture(false);
        Good(NativeCoreScriptSet.Read(content, ScriptLanguageIds.RpgMakerMvJavaScript));
        AssertFalse(content.Disposed);
    }

    public void Test_UnsafeManifestFailsBeforeReadingListedSource()
    {
        var content = Fixture(false);
        content.Files["index.html"] = Encoding.UTF8.GetBytes("<script src=\"../../outside.js\"></script><script src=\"js/main.js\"></script>");
        AssertFalse(NativeCoreScriptSet.Read(content, ScriptLanguageIds.RpgMakerMvJavaScript).Success);
        AssertEq(content.ReadPaths.Count, 2);
    }

    public void Test_MainAndIndexAreMandatory_NotInventedFromFilenames()
    {
        var content = Fixture(false); content.Files.Remove("index.html");
        AssertFalse(NativeCoreScriptSet.Read(content, ScriptLanguageIds.RpgMakerMvJavaScript).Success);
        content = Fixture(true); content.Files.Remove("js/main.js");
        AssertFalse(NativeCoreScriptSet.Read(content, ScriptLanguageIds.RpgMakerMzJavaScript).Success);
    }

    public void Test_InvalidManifestReturnsNoPartialExecutableModules()
    {
        var content = Fixture(false); content.Files.Remove("js/rpg_windows.js");
        var result = NativeCoreScriptSet.Read(content, ScriptLanguageIds.RpgMakerMvJavaScript);
        AssertFalse(result.Success); AssertEq(result.Modules.Count, 0);
    }

    private void Good(NativeCoreScriptSet result)
    {
        AssertTrue(result.Success, result.Error);
        if (!result.Success) throw new InvalidOperationException(result.Error);
    }

    private static MemoryContent Fixture(bool mz, string prefix = "")
    {
        var content = new MemoryContent();
        var family = mz ? "rmmz_" : "rpg_";
        var paths = new[] { "js/libs/pixi.js" }
            .Concat(new[] { "core", "managers", "objects", "scenes", "sprites", "windows" }.Select(s => "js/" + family + s + ".js"))
            .Concat(new[] { "js/plugins.js" }).ToArray();
        foreach (var path in paths) content.Files[prefix + path] = Encoding.UTF8.GetBytes("// synthetic " + path);
        content.Files[prefix + "index.html"] = Encoding.UTF8.GetBytes(string.Join("\n",
            (mz ? new[] { "js/main.js" } : paths.Concat(new[] { "js/main.js" })).Select(p => "<script src=\"" + p + "\"></script>")));
        content.Files[prefix + "js/main.js"] = Encoding.UTF8.GetBytes((mz ? "const scriptUrls = " + JsonSerializer.Serialize(paths) + ";\n" : "")
            + "throw Error('entry point must not execute during manifest inspection');");
        return content;
    }

    private sealed class MemoryContent : IGameContentSource
    {
        public Dictionary<string, byte[]> Files { get; } = new(StringComparer.Ordinal);
        public List<string> ReadPaths { get; } = new();
        public bool ThrowOnRead { get; set; }
        public bool Disposed { get; private set; }
        public string SourceId => "synthetic-core-test";
        public GameContentProtectionKind Protection => GameContentProtectionKind.None;
        public bool Exists(string path) => Files.ContainsKey(path);
        public ContentReadResult Read(string path)
        {
            ReadPaths.Add(path);
            if (ThrowOnRead) throw new InvalidOperationException("/home/secret/private-provider");
            return Files.TryGetValue(path, out var bytes) ? ContentReadResult.Succeeded(bytes) : ContentReadResult.Failed("missing", "missing");
        }
        public void Dispose() => Disposed = true;
    }
}
