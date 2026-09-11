using System.IO;
using Godot;
using UniversalRPG.Rgss;
using UniversalRPG.Sdk;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

public sealed class TestRgssProjectConfiguration : TestBase
{
    private const string Root = "user://rgss_project_configuration";

    public override void Setup()
    {
        Cleanup();
        Directory.CreateDirectory(ProjectSettings.GlobalizePath(Root));
    }

    public override void Teardown() => Cleanup();

    public void Test_MissingGameIniUsesGenerationDefault()
    {
        using var content = new DirectoryGameContentSource(ProjectSettings.GlobalizePath(Root));

        var result = RgssProjectConfigurationReader.Read(content, RgssGeneration.Rgss2);

        AssertTrue(result.Success, result.Result.ErrorMessage);
        AssertTrue(result.Value != null);
        AssertEq(result.Value!.ScriptArchivePath, "Data/Scripts.rvdata");
        AssertFalse(result.Value.UsesConfiguredScriptPath);
    }

    public void Test_CustomWindowsStyleScriptsPathIsNormalized()
    {
        var root = ProjectSettings.GlobalizePath(Root);
        File.WriteAllText(Path.Combine(root, "Game.ini"),
            "[Game]\nLibrary=System\\RGSS301.dll\nScripts=c\\Scripts.rvdata2\nTitle=Custom\n");
        using var content = new DirectoryGameContentSource(root);

        var result = RgssProjectConfigurationReader.Read(content, RgssGeneration.Rgss3);

        AssertTrue(result.Success, result.Result.ErrorMessage);
        AssertEq(result.Value!.ScriptArchivePath, "c/Scripts.rvdata2");
        AssertTrue(result.Value.UsesConfiguredScriptPath);
    }

    public void Test_GameIniLookupIsCaseInsensitiveThroughContentSource()
    {
        var root = ProjectSettings.GlobalizePath(Root);
        File.WriteAllText(Path.Combine(root, "GAME.INI"),
            "[game]\nscripts=DATA\\Custom.rxdata\n");
        using var content = new DirectoryGameContentSource(root);

        var result = RgssProjectConfigurationReader.Read(content, RgssGeneration.Rgss1);

        AssertTrue(result.Success, result.Result.ErrorMessage);
        AssertEq(result.Value!.ScriptArchivePath, "DATA/Custom.rxdata");
    }

    public void Test_TraversalScriptPathIsRejected()
    {
        var root = ProjectSettings.GlobalizePath(Root);
        File.WriteAllText(Path.Combine(root, "Game.ini"),
            "[Game]\nScripts=..\\outside\\Scripts.rvdata2\n");
        using var content = new DirectoryGameContentSource(root);

        var result = RgssProjectConfigurationReader.Read(content, RgssGeneration.Rgss3);

        AssertFalse(result.Success);
        AssertEq(result.Result.ErrorCode, "rgss.scripts-path-unsafe");
    }

    public void Test_AbsoluteScriptPathIsRejected()
    {
        var root = ProjectSettings.GlobalizePath(Root);
        File.WriteAllText(Path.Combine(root, "Game.ini"),
            "[Game]\nScripts=C:\\Games\\Scripts.rvdata2\n");
        using var content = new DirectoryGameContentSource(root);

        var result = RgssProjectConfigurationReader.Read(content, RgssGeneration.Rgss3);

        AssertFalse(result.Success);
        AssertEq(result.Result.ErrorCode, "rgss.scripts-path-unsafe");
    }

    private static void Cleanup()
    {
        var root = ProjectSettings.GlobalizePath(Root);
        if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
    }
}
