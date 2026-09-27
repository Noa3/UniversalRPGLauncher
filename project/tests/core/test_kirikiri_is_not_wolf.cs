using System.Collections.Generic;
using UniversalRPG.Plugins;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// A game that has a Wolf game's folders and is not a Wolf game.
/// </summary>
/// <remarks>
/// <para>
/// A KiriKiri game was offered to this repository as a Wolf game. It carries
/// folders called `BasicData` and `MapData`, which are two of the three things
/// the Wolf detector looks for, and its data folder is laid out the way a Wolf
/// game's is. Had the detector gone on folders alone it would have answered Wolf
/// for a game that holds no Wolf data anywhere, and every later layer would have
/// been handed a file that does not exist.
/// </para>
/// <para>
/// The third thing is what saves it. A Wolf game has a `Game.dat` at the root of
/// its data, and this game has no file of that name anywhere. The detection below
/// gives the detector the two folders and checks that it still refuses, because
/// a refusal is the answer that is right and a folder name is not an engine.
/// </para>
/// </remarks>
public partial class TestKirikiriIsNotAWolfGame : TestBase
{
    public void Test_TwoWolfFoldersWithoutTheFileThatProvesItAreNotEnough()
    {
        var plugin = new WolfRpgPlugin();
        var context = new EngineInspectionContext(InspectionOf(
            ["Game.exe", "Game.ini", "Config.exe", "Data/conf.txt"]));

        var probe = plugin.Detect(context);

        AssertTrue(
            !probe.IsMatch,
            "a folder with a Wolf game's folders and none of its data is not a Wolf"
            + $" game; the probe said matched={probe.IsMatch}");
        AssertTrue(
            probe.Diagnostics.Count > 0,
            "and it says why, so the refusal can be checked rather than trusted");
    }

    public void Test_TheFileThatProvesItIsStillNeededWhenTheFoldersAreThere()
    {
        // The other half of the same claim, and the reason the detector is
        // written the way it is. Adding the one file turns the same folder into a
        // Wolf game, which is what makes the refusal above a decision rather
        // than an accident of not having looked.
        var plugin = new WolfRpgPlugin();
        var context = new EngineInspectionContext(InspectionOf(
            ["Game.exe", "Data/Game.dat", "Data/BasicData/CommonData.dat"]));

        var probe = plugin.Detect(context);

        AssertTrue(
            probe.IsMatch,
            "and the same folder with the file that only a Wolf game has is one");
    }

    private static GameInspectionSnapshot InspectionOf(IReadOnlyList<string> pFiles)
    {
        // Built from the names that were really there, in the same shape the two
        // games' folders have: the data folder, a BasicData and a MapData inside
        // it, and the files. Nothing is opened. The point of the test is the
        // decision the detector makes from names, and reading the game to find
        // that out would be the thing this repository does not do.
        var files = new Dictionary<string, InspectedGameFile>();
        foreach (var path in pFiles)
        {
            files[path] = new InspectedGameFile(
                path, 0, System.Array.Empty<byte>(), false, false);
        }

        foreach (var folder in new[]
        {
            "Data", "Data/BasicData", "Data/MapData", "Data/BGM", "Data/SE",
        })
        {
            if (!files.ContainsKey(folder + "/"))
            {
                files[folder + "/"] = new InspectedGameFile(
                    folder + "/", 0, System.Array.Empty<byte>(), true, false);
            }
        }

        return new GameInspectionSnapshot(
            "kirikiri-fixture", false, false, files,
            new List<EngineInspectionDiagnostic>());
    }
}
