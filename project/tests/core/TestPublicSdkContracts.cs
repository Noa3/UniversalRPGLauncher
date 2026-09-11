using System.Linq;
using Godot;
using UniversalRPG.Plugins;
using UniversalRPG.Sdk;
using UniversalRPG.SdkHost;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

public sealed class TestPublicSdkContracts : TestBase
{
    public void Test_ScriptPolicySafeDefaultDeniesHostImpactingCapabilities()
    {
        var policy = ScriptExecutionPolicy.SafeDefault;

        AssertTrue(policy.AllowReadGameFiles);
        AssertTrue(policy.AllowWriteSaveFiles);
        AssertTrue(policy.AllowWriteCacheFiles);
        AssertFalse(policy.AllowArbitraryHostFileSystem);
        AssertFalse(policy.AllowNetwork);
        AssertFalse(policy.AllowClipboard);
        AssertFalse(policy.AllowProcessExecution);
        AssertFalse(policy.AllowNativeInterop);
        AssertTrue(policy.Validate().Success);
    }

    public void Test_ScriptDescriptorAcceptsStableEngineScriptMetadata()
    {
        var descriptor = new EngineScriptDescriptor
        {
            Id = "game:main-script",
            DisplayName = "Main Script",
            LanguageId = ScriptLanguageIds.Rgss3Ruby,
            RelativePath = "Data/Scripts.rvdata2",
            Sha256 = new string('a', 64),
            Origin = ScriptOrigin.Game,
            Required = true,
            LoadOrder = 10,
            Dependencies = new[] { "engine:rgss3-defaults" },
        };

        AssertTrue(descriptor.Validate().Success);
    }

    public void Test_ScriptDescriptorRejectsUnstableLanguageId()
    {
        var descriptor = new EngineScriptDescriptor
        {
            Id = "game:script",
            LanguageId = "Ruby RGSS3",
        };

        var validation = descriptor.Validate();
        AssertFalse(validation.Success);
        AssertEq(validation.ErrorCode, "script.invalid-language");
    }

    public void Test_LibraryAdapterExposesCapabilityTruth()
    {
        var library = new UniversalRpgLibraryAdapter();
        AssertEq(library.ApiVersion, UniversalRpgSdkVersion.ApiVersion);

        var rm2k = library.Engines.First(pEngine => pEngine.EngineId == EnginePluginIds.RpgMaker2000);
        var xp = library.Engines.First(pEngine => pEngine.EngineId == EnginePluginIds.RpgMakerXp);
        var mv = library.Engines.First(pEngine => pEngine.EngineId == EnginePluginIds.RpgMakerMv);
        var wolf = library.Engines.First(pEngine => pEngine.EngineId == EnginePluginIds.WolfRpg);

        AssertEq(rm2k.SupportLevel, EngineSupportLevel.PartialRuntime);
        AssertEq(wolf.SupportLevel, EngineSupportLevel.ExperimentalRuntime);
        AssertEq(xp.SupportLevel, EngineSupportLevel.ParsingOnly);
        AssertEq(mv.SupportLevel, EngineSupportLevel.ParsingOnly);
        AssertFalse(xp.SupportsScripting, "RGSS is not advertised until a real embedded Ruby runtime exists");
        AssertFalse(mv.SupportsScripting, "MV scripting is not advertised until a real JS runtime exists");
    }

    public void Test_LibraryAdapterCreatesRealRm2kSession()
    {
        var library = new UniversalRpgLibraryAdapter();
        var fixture = ProjectSettings.GlobalizePath("res://tests/fixtures/easyrpg-testgame/rm2000");
        var analysis = library.Analyze(fixture);

        AssertEq(analysis.EngineId, EnginePluginIds.RpgMaker2000);
        AssertEq(analysis.SupportLevel, EngineSupportLevel.PartialRuntime);

        var created = library.CreateSession(analysis);
        AssertTrue(created.Success, created.Result.ErrorMessage);
        AssertTrue(created.Session != null);
        if (created.Session == null) return;

        using var session = created.Session;
        AssertTrue(session.Start().Success);
        AssertTrue(session.IsRunning);
        AssertTrue(session.Scripting == null,
            "RM2K event execution is native runtime behavior, not falsely advertised as a general script VM");
        AssertTrue(session.Update(1.0 / 60.0).Success);
        AssertTrue(session.Stop().Success);
    }

    public void Test_LibraryAdapterRefusesParsingOnlySession()
    {
        var library = new UniversalRpgLibraryAdapter();
        var analysis = new GameAnalysis
        {
            GameDirectory = "/synthetic/xp",
            EngineId = EnginePluginIds.RpgMakerXp,
            Generation = "rgss1",
            ConfidenceScore = 1000,
            SupportLevel = EngineSupportLevel.ParsingOnly,
        };

        var created = library.CreateSession(analysis);
        AssertFalse(created.Success);
        AssertEq(created.Result.ErrorCode, "session.runtime-unavailable");
    }
}
