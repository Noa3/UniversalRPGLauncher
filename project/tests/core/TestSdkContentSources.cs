using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Godot;
using UniversalRPG.Sdk;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

public sealed class TestSdkContentSources : TestBase
{
    private const string Root = "user://sdk_content_sources";

    public override void Setup()
    {
        Cleanup();
        Directory.CreateDirectory(ProjectSettings.GlobalizePath(Root));
    }

    public override void Teardown() => Cleanup();

    public void Test_DirectorySourceResolvesWindowsCaseInsensitively()
    {
        var root = ProjectSettings.GlobalizePath(Root.PathJoin("case"));
        Directory.CreateDirectory(Path.Combine(root, "Graphics", "Characters"));
        File.WriteAllText(Path.Combine(root, "Graphics", "Characters", "Hero.PNG"), "hero");
        using var source = new DirectoryGameContentSource(root, "game");

        var result = source.Read("graphics/characters/hero.png");

        AssertTrue(result.Success, result.ErrorMessage);
        AssertEq(Encoding.UTF8.GetString(result.Data.Span), "hero");
    }

    public void Test_DirectorySourceRejectsTraversal()
    {
        var root = ProjectSettings.GlobalizePath(Root.PathJoin("safe"));
        Directory.CreateDirectory(root);
        using var source = new DirectoryGameContentSource(root, "game");

        var result = source.Read("../outside.dat");

        AssertFalse(result.Success);
        AssertEq(result.ErrorCode, "content.path-unresolved");
    }

    public void Test_LayeredSourcePrefersOverrideOverGame()
    {
        var gameRoot = ProjectSettings.GlobalizePath(Root.PathJoin("game"));
        var overrideRoot = ProjectSettings.GlobalizePath(Root.PathJoin("override"));
        Directory.CreateDirectory(Path.Combine(gameRoot, "Text"));
        Directory.CreateDirectory(Path.Combine(overrideRoot, "Text"));
        File.WriteAllText(Path.Combine(gameRoot, "Text", "message.txt"), "original");
        File.WriteAllText(Path.Combine(overrideRoot, "Text", "message.txt"), "translated");

        using var layered = new LayeredGameContentSource(new[]
        {
            new LayeredGameContentSource.Layer
            {
                Id = "override",
                Source = new DirectoryGameContentSource(overrideRoot, "override"),
            },
            new LayeredGameContentSource.Layer
            {
                Id = "game",
                Source = new DirectoryGameContentSource(gameRoot, "game"),
            },
        });

        var result = layered.Read("text/MESSAGE.TXT");

        AssertTrue(result.Success);
        AssertEq(Encoding.UTF8.GetString(result.Data.Span), "translated");
    }

    public void Test_LayeredSourceDoesNotHideClaimedLayerFailure()
    {
        var lower = new MemorySource("game", new Dictionary<string, byte[]>
        {
            ["data/value.bin"] = new byte[] { 1, 2, 3 },
        });
        var brokenOverride = new BrokenClaimingSource("data/value.bin");
        using var layered = new LayeredGameContentSource(new[]
        {
            new LayeredGameContentSource.Layer { Id = "override", Source = brokenOverride },
            new LayeredGameContentSource.Layer { Id = "game", Source = lower },
        });

        var result = layered.Read("data/value.bin");

        AssertFalse(result.Success);
        AssertEq(result.ErrorCode, "synthetic.broken");
    }

    public void Test_LayeredSourceFallsThroughWhenHigherLayerDoesNotContainPath()
    {
        var upper = new MemorySource("override", new Dictionary<string, byte[]>());
        var lower = new MemorySource("game", new Dictionary<string, byte[]>
        {
            ["data/value.bin"] = new byte[] { 4, 5, 6 },
        });
        using var layered = new LayeredGameContentSource(new[]
        {
            new LayeredGameContentSource.Layer { Id = "override", Source = upper },
            new LayeredGameContentSource.Layer { Id = "game", Source = lower },
        });

        var result = layered.Read("data/value.bin");

        AssertTrue(result.Success);
        AssertTrue(result.Data.Span.SequenceEqual(new byte[] { 4, 5, 6 }));
    }

    private sealed class MemorySource : IGameContentSource
    {
        private readonly Dictionary<string, byte[]> _data;

        public MemorySource(string pId, Dictionary<string, byte[]> pData)
        {
            SourceId = pId;
            _data = pData;
        }

        public string SourceId { get; }
        public GameContentProtectionKind Protection => GameContentProtectionKind.None;
        public bool Exists(string pLogicalPath) => _data.ContainsKey(pLogicalPath.ToLowerInvariant());
        public ContentReadResult Read(string pLogicalPath)
            => _data.TryGetValue(pLogicalPath.ToLowerInvariant(), out var value)
                ? ContentReadResult.Succeeded(value)
                : ContentReadResult.Failed("content.not-found", "not found");
        public void Dispose() { }
    }

    private sealed class BrokenClaimingSource : IGameContentSource
    {
        private readonly string _path;
        public BrokenClaimingSource(string pPath) => _path = pPath;
        public string SourceId => "broken";
        public GameContentProtectionKind Protection => GameContentProtectionKind.None;
        public bool Exists(string pLogicalPath) => pLogicalPath.Equals(_path, StringComparison.OrdinalIgnoreCase);
        public ContentReadResult Read(string pLogicalPath) => ContentReadResult.Failed("synthetic.broken", "synthetic failure");
        public void Dispose() { }
    }

    private static void Cleanup()
    {
        var root = ProjectSettings.GlobalizePath(Root);
        if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
    }
}
