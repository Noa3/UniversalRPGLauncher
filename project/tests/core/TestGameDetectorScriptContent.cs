using System.IO;
using Godot;
using UniversalRPG.GameDetectorNs;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

public sealed class TestGameDetectorScriptContent : TestBase
{
    private const string Root = "user://detector_script_content";

    public override void Setup()
    {
        Cleanup();
        var root = ProjectSettings.GlobalizePath(Root);
        Directory.CreateDirectory(Path.Combine(root, "data"));
        Directory.CreateDirectory(Path.Combine(root, "js"));
        File.WriteAllText(Path.Combine(root, "index.html"), "<!doctype html><html></html>");
        File.WriteAllText(Path.Combine(root, "data", "System.json"), "{\"gameTitle\":\"No Plugins\"}");
        File.WriteAllText(Path.Combine(root, "js", "rpg_core.js"), "// RPG Maker MV engine core");
    }

    public override void Teardown() => Cleanup();

    public void Test_MvCoreJavaScriptIsNotReportedAsCustomPlugin()
    {
        var result = new GameDetector().Analyze(ProjectSettings.GlobalizePath(Root));

        AssertEq(result.Engine, GameDetector.EngineType.RpgMakerMv);
        AssertFalse(result.HasCustomScripts,
            "RPG Maker engine JavaScript is runtime infrastructure, not a custom game plugin");
    }

    public void Test_MvPluginFileIsReportedAsCustomScriptContent()
    {
        var root = ProjectSettings.GlobalizePath(Root);
        Directory.CreateDirectory(Path.Combine(root, "js", "plugins"));
        File.WriteAllText(Path.Combine(root, "js", "plugins", "MyFeature.js"), "window.MyFeature = true;");

        var result = new GameDetector().Analyze(root);

        AssertEq(result.Engine, GameDetector.EngineType.RpgMakerMv);
        AssertTrue(result.HasCustomScripts,
            "a real js/plugins plugin is reported as game-authored script content");
    }

    private static void Cleanup()
    {
        var root = ProjectSettings.GlobalizePath(Root);
        if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
    }
}
