using System.Collections.Generic;
using System.IO;
using Godot;
using UniversalRPG.GameDetectorNs;
using UniversalRPG.Plugins;
using UniversalRPG.Tests.Framework;
using static UniversalRPG.GameDetectorNs.GameDetector;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// A real RPG Maker MZ game, laid out as the editor leaves it.
/// </summary>
/// <remarks>
/// <para>
/// An MZ game is a web game: an index, a folder of plain JSON, and a JavaScript
/// runtime. Nothing in it is compiled and nothing in it is encrypted, which is
/// why a data reader for it can exist without a runtime and without running a
/// line of the game's own code.
/// </para>
/// <para>
/// The fixtures are the twelve data files of a real 1.9.1 game plus the two
/// files that name the engine. The JavaScript is deliberately not here: a script
/// is code, and this repository does not run a game's code. What a map, an
/// actor and a system file hold is data, and the reader takes them as data.
/// </para>
/// </remarks>
partial class TestRealMzDetection : TestBase
{
    private const string FixtureRoot = "res://tests/fixtures/mz";

    public void Test_AGameFolderWithTheProjectFileAndTheWebRuntimeIsAnMzGame()
    {
        var root = LayOutAGame();
        var detector = new GameDetector();
        var result = detector.Analyze(root);

        AssertEq(
            result.Engine, EngineType.RpgMakerMz,
            "and the engine is named from the files alone, not from its name");
        AssertTrue(
            result.Evidence.Count > 0,
            "with the files it decided by, so the answer can be checked");
    }

    public void Test_ItIsNotTheGenerationBeforeIt()
    {
        // MV and MZ are laid out the same way: an index, a data folder of plain
        // JSON, a runtime in the same place, a package file beside it. The
        // runtime file's name is what tells the two apart, `rmmz_` for MZ and
        // `rpg_core` for MV, and a detector that only counts folders would answer
        // MV for an MZ game and MZ for an MV game, which is worse than not
        // knowing.
        var root = LayOutAGame();
        var detector = new GameDetector();
        var result = detector.Analyze(root);

        AssertTrue(
            result.Engine != EngineType.RpgMakerMv,
            $"an MZ game is not an MV game; it was answered {result.Engine}");
    }

    public void Test_TheTwoGenerationsAreToldApartByTheFileTheyEachWrite()
    {
        // The other half, and the reason the one above is worth having. The same
        // folder, with the previous generation's runtime written into it instead,
        // has to be answered differently. A detector that answered the same for
        // both would have learned nothing from either.
        var root = LayOutAGame();
        ReplaceRuntime(root, "rmmz_core.js", "rpg_core.js");
        ReplaceRuntime(root, "rmmz_managers.js", "rpg_managers.js");

        var detector = new GameDetector();
        var result = detector.Analyze(root);

        AssertTrue(
            result.Engine == EngineType.RpgMakerMv
            || result.Engine == EngineType.Unknown,
            "the folder with the previous generation's runtime is that generation,"
            + $" or is refused; it was answered {result.Engine}");
    }

    private static void ReplaceRuntime(string pRoot, string pFrom, string pTo)
    {
        var from = pRoot.PathJoin("js").PathJoin(pFrom);
        var to = pRoot.PathJoin("js").PathJoin(pTo);
        if (System.IO.File.Exists(from))
        {
            System.IO.File.Move(from, to, true);
        }
    }

    private string LayOutAGame()
    {
        var root = ProjectSettings.GlobalizePath("user://mz_detection_fixture");
        var data = root.PathJoin("data");
        var js = root.PathJoin("js");

        foreach (var folder in new[] { data, js })
        {
            if (DirAccess.DirExistsAbsolute(folder))
            {
                foreach (var file in DirAccess.GetFilesAt(folder))
                {
                    DirAccess.RemoveAbsolute(folder.PathJoin(file));
                }
            }
            else
            {
                DirAccess.MakeDirRecursiveAbsolute(folder);
            }
        }

        CopyFixture("data/System.json", data.PathJoin("System.json"));
        CopyFixture("data/Actors.json", data.PathJoin("Actors.json"));
        CopyFixture("data/MapInfos.json", data.PathJoin("MapInfos.json"));
        CopyFixture("data/Map001.json", data.PathJoin("Map001.json"));
        CopyFixture("js/rmmz_core.js", js.PathJoin("rmmz_core.js"));
        CopyFixture("js/rmmz_managers.js", js.PathJoin("rmmz_managers.js"));
        CopyFixture("package.json", root.PathJoin("package.json"));
        Write(root.PathJoin("index.html"), "<html><body></body></html>");
        Write(root.PathJoin("game.rmmzproject"), "RPGMZ 1.9.1\n");

        return root;
    }

    private static void Write(string pPath, string pText)
    {
        System.IO.File.WriteAllText(pPath, pText, System.Text.Encoding.UTF8);
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
