using System;
using System.IO;
using Godot;
using UniversalRPG.Rm2k.Assets;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

public partial class TestRtpDiagnostics : TestBase
{
    private const string TempBase = "user://rtp_diagnostics";
    private string _root = "";

    public override void Setup()
    {
        Cleanup();
        _root = ProjectSettings.GlobalizePath(TempBase.PathJoin("standard"));
        Directory.CreateDirectory(Path.Combine(_root, "CharSet"));
        File.WriteAllText(Path.Combine(_root, "CharSet", "Hero.png"), "fixture");
    }

    public override void Teardown()
    {
        Cleanup();
    }

    public void Test_ReportsFoundAndMissingAssetsWithoutOpeningThem()
    {
        var registry = new RtpRegistry();
        AssertTrue(registry.Register(new RtpProfile
        {
            Id = "standard",
            EngineId = "rpg-maker-2000",
            Generation = "rm2k",
            DependencyName = "Standard",
            RootPath = _root,
        }).Success);
        var profile = new RtpGameProfile
        {
            GamePath = "fixture://rm2k",
            EngineId = "rpg-maker-2000",
            Generation = "rm2k",
            DependencyName = "Standard",
            RtpProfileId = "standard",
            RequiredAssets = new() { "CharSet/Hero.png", "ChipSet/Missing.png" },
        };

        var report = RtpAssetDiagnostics.Analyze(registry, profile);

        AssertTrue(report.Success);
        AssertEq(report.Diagnostics.Count, 2);
        AssertEq(report.Diagnostics[0].Status, RtpAssetStatus.Available);
        AssertEq(report.Diagnostics[0].ProfileId, "standard");
        AssertEq(report.Diagnostics[1].Status, RtpAssetStatus.MissingAsset);
        AssertTrue(report.HasMissingAssets);
    }

    public void Test_ReportsNoProfileAndInvalidPathAsSeparateStatuses()
    {
        var registry = new RtpRegistry();
        var noProfile = RtpAssetDiagnostics.Analyze(registry, Profile("CharSet/Hero.png"));
        AssertTrue(noProfile.Success);
        AssertEq(noProfile.Diagnostics[0].Status, RtpAssetStatus.NoMatchingProfile);

        AssertTrue(registry.Register(new RtpProfile
        {
            Id = "standard",
            EngineId = "rpg-maker-2000",
            Generation = "rm2k",
            DependencyName = "Standard",
            RootPath = _root,
        }).Success);
        var invalidPath = RtpAssetDiagnostics.Analyze(registry, Profile("../outside.png"));
        AssertTrue(invalidPath.Success);
        AssertEq(invalidPath.Diagnostics[0].Status, RtpAssetStatus.InvalidPath);
    }

    public void Test_GameProfileJsonRoundTripKeepsOnlyBoundedMetadata()
    {
        var profile = Profile("CharSet/Hero.png");
        var json = RtpGameProfileCodec.Serialize(profile);
        AssertTrue(json.Contains("rpg-maker-2000", StringComparison.Ordinal));
        AssertFalse(json.Contains(_root, StringComparison.OrdinalIgnoreCase));

        AssertTrue(RtpGameProfileCodec.TryDeserialize(json, out var restored, out var error), error);
        AssertEq(restored!.GamePath, profile.GamePath);
        AssertEq(restored.RequiredAssets.Count, 1);
        AssertEq(restored.RequiredAssets[0], "CharSet/Hero.png");
    }

    public void Test_GameProfileRejectsUnboundedRequiredAssetLists()
    {
        var profile = Profile("asset.png");
        for (var index = 0; index < RtpGameProfile.MaxRequiredAssets + 1; index++)
        {
            profile.RequiredAssets.Add($"asset-{index}.png");
        }

        AssertFalse(RtpGameProfileCodec.TrySerialize(profile, out _, out var error));
        AssertTrue(error.Contains("required asset", StringComparison.OrdinalIgnoreCase));
    }

    private RtpGameProfile Profile(string pAsset)
    {
        return new RtpGameProfile
        {
            GamePath = "fixture://rm2k",
            EngineId = "rpg-maker-2000",
            Generation = "rm2k",
            DependencyName = "Standard",
            RtpProfileId = "standard",
            RequiredAssets = new() { pAsset },
        };
    }

    private void Cleanup()
    {
        var path = ProjectSettings.GlobalizePath(TempBase);
        if (Directory.Exists(path))
        {
            Directory.Delete(path, true);
        }
    }
}
