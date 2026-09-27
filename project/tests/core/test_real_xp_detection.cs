using System.Collections.Generic;
using Godot;
using UniversalRPG.GameDetectorNs;
using UniversalRPG.Tests.Framework;
using static UniversalRPG.GameDetectorNs.GameDetector;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The engine of a real game folder, decided from the files that are in it.
/// </summary>
/// <remarks>
/// The fixtures below are a whole game's worth of files, laid out the way an XP
/// installation leaves them, so the detector sees a folder rather than a list of
/// names. The point is not that the detector is given the answer: it is given
/// the same folder it would meet on disk, with a project's file, a data folder
/// of marshal files, and a settings file naming the runtime.
/// </remarks>
public partial class TestRealXpDetection : TestBase
{
    private const string FixtureRoot = "res://tests/fixtures/rgss-xp";

    public void Test_AGameFolderWithAProjectFileAndMarshalDataIsAnXpGame()
    {
        var root = LayOutAGameFolder();
        var detector = new GameDetector();
        var result = detector.Analyze(root);

        AssertEq(
            result.Engine, EngineType.RpgMakerXp,
            "the engine is named from the files alone");
        AssertTrue(
            result.Confidence >= Confidence.Medium,
            $"with at least ordinary confidence, {result.Confidence}");
        AssertTrue(
            result.Evidence.Count > 0,
            "and it says which file it decided by, so the answer can be checked");
    }

    public void Test_TheSameFolderIsNotAnyOfTheOtherEngines()
    {
        // The folders of two engines overlap: a VX game has a project file and
        // marshal data as well, and the only thing that separates them is a name
        // and a version. A detector that answered "an RGSS game" would be right
        // and useless, and one that answered VX would be wrong and playable.
        var root = LayOutAGameFolder();
        var detector = new GameDetector();
        var result = detector.Analyze(root);

        AssertTrue(
            result.Engine != EngineType.RpgMakerVx
            && result.Engine != EngineType.RpgMakerVxAce
            && result.Engine != EngineType.WolfRpg,
            $"and not another engine of the same family; it said {result.Engine}");
    }

    public void Test_AFolderWithOnlyMarshalDataAndNoProjectIsStillRecognised()
    {
        // A folder missing its project file is a game a player has half of, and
        // refusing it would refuse a game that a person can still open. The
        // runtime named in the settings is the second thing that says so.
        var root = ProjectSettings.GlobalizePath(FixtureRoot);
        var detector = new GameDetector();
        var result = detector.Analyze(root);

        AssertTrue(
            result.Engine == EngineType.RpgMakerXp || result.Engine == EngineType.Unknown,
            $"a folder with the data but no layout is either an XP game or nothing;"
            + $" it said {result.Engine}");
    }

    private string LayOutAGameFolder()
    {
        // A copy in the scratch area, laid out as an installation leaves it, so
        // the detector walks a folder and not a list of names. Nothing is
        // executed: these are bytes copied from one place to another.
        var root = ProjectSettings.GlobalizePath("user://xp_detection_fixture");
        var data = root.PathJoin("Data");
        if (DirAccess.DirExistsAbsolute(data))
        {
            foreach (var file in DirAccess.GetFilesAt(data))
            {
                DirAccess.RemoveAbsolute(data.PathJoin(file));
            }
        }
        else
        {
            DirAccess.MakeDirRecursiveAbsolute(data);
        }

        foreach (var name in new[]
        {
            "Actors", "Classes", "Items", "Enemies", "Skills", "States", "System",
        })
        {
            CopyFixture($"{name}.rxdata", data.PathJoin($"{name}.rxdata"));
        }
        CopyFixture("Game.ini", root.PathJoin("Game.ini"));
        CopyFixture("Game.rxproj", root.PathJoin("Game.rxproj"));
        return root;
    }

    private void CopyFixture(string pName, string pTarget)
    {
        var source = FixtureRoot.PathJoin(pName);
        AssertTrue(
            System.IO.File.Exists(ProjectSettings.GlobalizePath(source)),
            $"{pName} is in the fixtures");
        System.IO.File.Copy(ProjectSettings.GlobalizePath(source), pTarget, true);
    }
}
