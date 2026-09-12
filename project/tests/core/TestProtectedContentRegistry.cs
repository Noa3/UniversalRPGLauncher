using System;
using System.Collections.Generic;
using UniversalRPG.Sdk;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

public sealed class TestProtectedContentRegistry : TestBase
{
    public void Test_DuplicateSchemeInsideProviderIsRejected()
    {
        var registry = new ProtectedContentRegistry();
        var result = registry.Register(new SyntheticProvider(
            "duplicate-provider",
            new[] { "scheme-a", "scheme-a" }));

        AssertFalse(result.Success);
        AssertEq(result.ErrorCode, "content-provider.duplicate-scheme");
    }

    public void Test_ThrowingProbeDoesNotHideHealthyProvider()
    {
        var registry = new ProtectedContentRegistry();
        AssertTrue(registry.Register(new SyntheticProvider(
            "a-throwing",
            new[] { "scheme-a" },
            throwOnProbe: true)).Success);
        AssertTrue(registry.Register(new SyntheticProvider(
            "b-healthy",
            new[] { "scheme-a" })).Success);
        var descriptor = Descriptor("scheme-a");

        var ids = registry.MatchingProviderIds(descriptor);
        var opened = registry.Open(descriptor);

        AssertEq(ids.Count, 1);
        AssertEq(ids[0], "b-healthy");
        AssertTrue(opened.Success, opened.Result.ErrorMessage);
        AssertTrue(opened.Source != null);
        opened.Source?.Dispose();
    }

    public void Test_ThrowingOpenIsReturnedAsStableFailure()
    {
        var registry = new ProtectedContentRegistry();
        AssertTrue(registry.Register(new SyntheticProvider(
            "throw-open",
            new[] { "scheme-a" },
            throwOnOpen: true)).Success);

        var result = registry.Open(Descriptor("scheme-a"));

        AssertFalse(result.Success);
        AssertEq(result.Result.ErrorCode, "content.provider-open-exception");
        AssertTrue(result.Result.ErrorMessage.Contains("throw-open", StringComparison.Ordinal));
    }

    public void Test_AmbiguousHealthyProvidersFailClosed()
    {
        var registry = new ProtectedContentRegistry();
        AssertTrue(registry.Register(new SyntheticProvider("a-provider", new[] { "scheme-a" })).Success);
        AssertTrue(registry.Register(new SyntheticProvider("b-provider", new[] { "scheme-a" })).Success);

        var result = registry.Open(Descriptor("scheme-a"));

        AssertFalse(result.Success);
        AssertEq(result.Result.ErrorCode, "content.provider-ambiguous");
    }

    private static ProtectedContentDescriptor Descriptor(string pScheme)
        => new()
        {
            SchemeId = pScheme,
            SourcePath = "/synthetic",
            EngineId = "synthetic-engine",
            Protection = GameContentProtectionKind.ProtectedArchive,
        };

    private sealed class SyntheticProvider : IProtectedContentProvider
    {
        private readonly bool _throwOnProbe;
        private readonly bool _throwOnOpen;

        public SyntheticProvider(
            string pId,
            IReadOnlyList<string> pSchemes,
            bool throwOnProbe = false,
            bool throwOnOpen = false)
        {
            Id = pId;
            SchemeIds = pSchemes;
            _throwOnProbe = throwOnProbe;
            _throwOnOpen = throwOnOpen;
        }

        public string Id { get; }
        public IReadOnlyList<string> SchemeIds { get; }

        public bool CanOpen(ProtectedContentDescriptor pDescriptor)
        {
            if (_throwOnProbe) throw new InvalidOperationException("probe failed");
            return true;
        }

        public ContentSourceResult Open(ProtectedContentDescriptor pDescriptor)
        {
            if (_throwOnOpen) throw new InvalidOperationException("open failed");
            return ContentSourceResult.Succeeded(new EmptySource());
        }
    }

    private sealed class EmptySource : IGameContentSource
    {
        public string SourceId => "empty";
        public GameContentProtectionKind Protection => GameContentProtectionKind.ProtectedArchive;
        public bool Exists(string pLogicalPath) => false;
        public ContentReadResult Read(string pLogicalPath) => ContentReadResult.Failed("content.not-found", "missing");
        public void Dispose() { }
    }
}
