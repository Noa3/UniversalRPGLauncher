using System;
using System.IO;
using Godot;
using UniversalRPG.Plugins;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

public partial class TestSafeGameInspectorBounds : TestBase
{
    private const string Root = "user://safe_inspector_bounds";

    public override void Setup()
    {
        Remove(Root);
        DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath(Root));
    }

    public override void Teardown() => Remove(Root);

    public void Test_LargePayloadsUsePrefixBudget()
    {
        var path = ProjectSettings.GlobalizePath(Root).PathJoin("large.bin");
        File.WriteAllBytes(path, new byte[64 * 1024]);

        var result = SafeGameInspector.Inspect(ProjectSettings.GlobalizePath(Root), new GameInspectionLimits
        {
            MaxDepth = 2,
            MaxEntries = 8,
            MaxFileBytes = 1024 * 1024,
            MaxPrefixBytes = 32,
        });

        AssertTrue(result.Success, "large payload inspection succeeds");
        AssertTrue(result.Value != null, "large payload snapshot exists");
        AssertTrue(result.Value!.TryGet("large.bin", out var file), "large payload is listed");
        AssertEq(file.Length, 64 * 1024, "large payload length is preserved");
        AssertEq(file.Data.Length, 32, "large payload uses prefix budget");
        AssertTrue(file.IsTruncated, "large payload is marked truncated");
    }

    public void Test_KnownJsonMetadataKeepsBoundedContent()
    {
        var path = ProjectSettings.GlobalizePath(Root).PathJoin("System.json");
        File.WriteAllText(path, "{\"gameTitle\":\"Bounded\"}");

        var result = SafeGameInspector.Inspect(ProjectSettings.GlobalizePath(Root), new GameInspectionLimits
        {
            MaxDepth = 2,
            MaxEntries = 8,
            MaxFileBytes = 1024,
            MaxPrefixBytes = 4,
        });

        AssertTrue(result.Success, "metadata inspection succeeds");
        AssertTrue(result.Value != null, "metadata snapshot exists");
        if (result.Value == null)
        {
            return;
        }
        AssertTrue(result.Value.TryGet("System.json", out var file), "metadata file is listed");
        AssertFalse(file.IsTruncated, "small metadata is complete");
        AssertTrue(file.Data.Length > 4, "small metadata exceeds prefix budget");
    }

    private static void Remove(string pPath)
    {
        var absolute = ProjectSettings.GlobalizePath(pPath);
        if (Directory.Exists(absolute))
        {
            Directory.Delete(absolute, recursive: true);
        }
    }
}
