using System;
using System.Security.Cryptography;
using System.Text;
using UniversalRPG.Plugins;
using UniversalRPG.Sdk;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

public sealed class TestWebScriptSourceIdentity : TestBase
{
    public void Test_MatchingSourceHashIsCaseInsensitive()
    {
        var content = new Content("const answer=42;");
        var provider = new GameContentWebScriptSourceProvider(content);
        var hash = Convert.ToHexString(SHA256.HashData(content.Bytes));
        AssertTrue(provider.Read(Descriptor(hash)).Success);
        AssertTrue(provider.Read(Descriptor(hash.ToLowerInvariant())).Success);
    }

    public void Test_ChangedSourceRequiresFreshInspection()
    {
        var content = new Content("const answer=42;");
        var provider = new GameContentWebScriptSourceProvider(content);
        var descriptor = Descriptor(Convert.ToHexString(SHA256.HashData(content.Bytes)));
        AssertTrue(provider.Read(descriptor).Success);
        content.Bytes = Encoding.UTF8.GetBytes("const answer=43;");
        var result = provider.Read(descriptor);
        AssertFalse(result.Success);
        AssertTrue(result.Error.Contains("scripts.source-changed", StringComparison.Ordinal));
    }

    public void Test_InvalidHashIsRejectedBeforeReadingContent()
    {
        foreach (var hash in new[] { "abc", new string('z',64) })
        {
            var content = new Content("42");
            var result = new GameContentWebScriptSourceProvider(content).Read(Descriptor(hash));
            AssertFalse(result.Success);
            AssertEq(content.Reads,0);
        }
    }

    public void Test_MissingHashDoesNotInventIdentityVerification()
    {
        var content = new Content("42");
        AssertTrue(new GameContentWebScriptSourceProvider(content).Read(Descriptor("")).Success);
        AssertEq(content.Reads,1);
    }

    private static EngineScriptDescriptor Descriptor(string hash) => new()
    {
        Id="plugin:test", LanguageId=ScriptLanguageIds.RpgMakerMvJavaScript,
        DisplayName="Test", RelativePath="js/plugins/Test.js", Sha256=hash,
    };

    private sealed class Content : IGameContentSource
    {
        public Content(string text) => Bytes=Encoding.UTF8.GetBytes(text);
        public byte[] Bytes { get; set; }
        public int Reads { get; private set; }
        public string SourceId => "test-memory";
        public GameContentProtectionKind Protection => GameContentProtectionKind.None;
        public bool Exists(string path) => true;
        public ContentReadResult Read(string path) { Reads++; return ContentReadResult.Succeeded(Bytes); }
        public void Dispose() { }
    }
}
