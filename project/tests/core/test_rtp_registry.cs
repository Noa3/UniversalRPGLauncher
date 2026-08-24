using System;
using System.IO;
using Godot;
using UniversalRPG.Rm2k.Assets;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

public partial class TestRtpRegistry : TestBase
{
    private const string TempBase = "user://rtp_registry";
    private string _standardRoot = "";
    private string _fallbackRoot = "";

    public override void Setup()
    {
        Cleanup();
        _standardRoot = ProjectSettings.GlobalizePath(TempBase.PathJoin("standard"));
        _fallbackRoot = ProjectSettings.GlobalizePath(TempBase.PathJoin("fallback"));
        Directory.CreateDirectory(Path.Combine(_standardRoot, "CharSet"));
        Directory.CreateDirectory(Path.Combine(_fallbackRoot, "CharSet"));
        File.WriteAllText(Path.Combine(_standardRoot, "CharSet", "Hero.png"), "standard");
        File.WriteAllText(Path.Combine(_fallbackRoot, "CharSet", "Hero.png"), "fallback");
    }

    public override void Teardown()
    {
        Cleanup();
    }

    public void Test_ExplicitProfileResolvesCaseInsensitiveAssetDeterministically()
    {
        var registry = new RtpRegistry();
        var registration = registry.Register(new RtpProfile
        {
            Id = "rm2k-standard",
            EngineId = "rpg-maker-2000",
            Generation = "rm2k",
            DependencyName = "Standard",
            RootPath = _standardRoot,
        });

        AssertTrue(registration.Success);
        var result = registry.Resolve("rpg-maker-2000", "RM2K", "standard", "charset\\hero.png");

        AssertTrue(result.Success);
        AssertEq(result.ProfileId, "rm2k-standard");
        AssertTrue(result.ResolvedPath.EndsWith(Path.Combine("CharSet", "Hero.png"), StringComparison.OrdinalIgnoreCase));
        AssertEq(File.ReadAllText(result.ResolvedPath), "standard");
    }

    public void Test_RegistrationOrderIsTheDeterministicTieBreaker()
    {
        var registry = new RtpRegistry();
        AssertTrue(registry.Register(Profile("first", _standardRoot)).Success);
        AssertTrue(registry.Register(Profile("second", _fallbackRoot)).Success);

        var result = registry.Resolve("rpg-maker-2000", "rm2k", "Standard", "CharSet/Hero.png");

        AssertTrue(result.Success);
        AssertEq(result.ProfileId, "first");
    }

    public void Test_UnregisteredDirectoryIsNeverSearchedAutomatically()
    {
        var registry = new RtpRegistry();
        var result = registry.Resolve("rpg-maker-2000", "rm2k", "Standard", "CharSet/Hero.png");

        AssertFalse(result.Success);
        AssertEq(result.Status, RtpResolutionStatus.NoMatchingProfile);
        AssertEq(result.ResolvedPath, "");
    }

    public void Test_TraversalAbsoluteAndNullPathsAreRejectedBeforeLookup()
    {
        var registry = new RtpRegistry();
        AssertTrue(registry.Register(Profile("standard", _standardRoot)).Success);

        foreach (var path in new[] { "../outside.txt", "/absolute.txt", "C:/absolute.txt", "CharSet/../Hero.png", "CharSet/\u0000.png" })
        {
            var result = registry.Resolve("rpg-maker-2000", "rm2k", "Standard", path);
            AssertFalse(result.Success, path);
            AssertEq(result.Status, RtpResolutionStatus.InvalidPath, path);
            AssertEq(result.ResolvedPath, "", path);
        }
    }

    public void Test_InvalidAndDuplicateProfilesAreRejected()
    {
        var registry = new RtpRegistry();
        AssertFalse(registry.Register(Profile("", _standardRoot)).Success);
        AssertFalse(registry.Register(Profile("missing", Path.Combine(_standardRoot, "missing"))).Success);
        AssertTrue(registry.Register(Profile("standard", _standardRoot)).Success);
        AssertFalse(registry.Register(Profile("standard", _fallbackRoot)).Success);
    }

    private static RtpProfile Profile(string pId, string pRootPath)
    {
        return new RtpProfile
        {
            Id = pId,
            EngineId = "rpg-maker-2000",
            Generation = "rm2k",
            DependencyName = "Standard",
            RootPath = pRootPath,
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
