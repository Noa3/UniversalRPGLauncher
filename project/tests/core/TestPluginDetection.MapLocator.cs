using System.IO;
using Godot;
using UniversalRPG.Rm2k.Simulation;

namespace UniversalRPG.Tests.Core;

public partial class TestPluginDetection
{
    public void Test_Rm2kMapLocatorPrefersLmtPartyStartMap()
    {
        var root = TempBase.PathJoin("LocatorPreferred");
        WriteText(root.PathJoin("Map0001.lmu"), "fallback");
        WriteText(root.PathJoin("Map0002.lmu"), "start");
        var mapTree = LocatorMapTree(2);

        var selected = Rm2kMapLocator.SelectInitialMap(ProjectSettings.GlobalizePath(root), mapTree);

        AssertTrue(selected.Path != null);
        AssertEq(Path.GetFileName(selected.Path), "Map0002.lmu");
        AssertEq(selected.RequestedMapId, 2);
        AssertFalse(selected.UsedFallback);
        AssertEq(selected.Diagnostic, "");
    }

    public void Test_Rm2kMapLocatorFallsBackDeterministicallyWhenStartMapIsMissing()
    {
        var root = TempBase.PathJoin("LocatorFallback");
        WriteText(root.PathJoin("Map0007.lmu"), "later");
        WriteText(root.PathJoin("Map0001.LMU"), "first");
        var mapTree = LocatorMapTree(30);

        var selected = Rm2kMapLocator.SelectInitialMap(ProjectSettings.GlobalizePath(root), mapTree);

        AssertTrue(selected.Path != null);
        AssertEq(Path.GetFileName(selected.Path), "Map0001.LMU");
        AssertEq(selected.RequestedMapId, 30);
        AssertTrue(selected.UsedFallback);
        AssertTrue(selected.Diagnostic.Contains("Start map 30"));
    }

    public void Test_Rm2kMapLocatorReportsMissingMapDirectoryContent()
    {
        var root = TempBase.PathJoin("LocatorEmpty");
        DirAccess.MakeDirRecursiveAbsolute(root);

        var selected = Rm2kMapLocator.SelectInitialMap(
            ProjectSettings.GlobalizePath(root), LocatorMapTree(1));

        AssertTrue(selected.Path == null);
        AssertTrue(selected.Diagnostic.Contains("No LMU map"));
    }

    private static Godot.Collections.Dictionary LocatorMapTree(int pPartyMapId)
    {
        return new Godot.Collections.Dictionary
        {
            {
                "start",
                new Godot.Collections.Dictionary
                {
                    { "party_map_id", pPartyMapId },
                    { "party_x", 0 },
                    { "party_y", 0 },
                }
            },
        };
    }
}
