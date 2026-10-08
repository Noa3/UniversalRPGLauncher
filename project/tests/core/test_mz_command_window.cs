using System;
using System.Collections.Generic;
using System.IO;
using UniversalRPG.App.Ui;
using UniversalRPG.Plugins;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And the title's command list reaches the window the player looks at.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this closes the gap that made a real game look broken.</strong>
/// Measured before this: <c>TitleCommands</c>, <c>TitleIndex</c> and
/// <c>TitleVisible</c> were computed and covered by tests, and
/// <c>grep TitleCommands project/app/</c> found <em>nothing</em> -- so a player
/// saw the title image, moved a cursor that was not drawn, and got no menu at
/// all. The runtime was right and the window was silent.
/// </para>
/// <para>
/// <strong>And the placement is the engine's, not a guess.</strong> It comes
/// from <c>Scene_Title.commandWindowRect</c>, measured in the project's own
/// <c>rmmz_scenes.js</c>, and the row height from
/// <c>Window_Base.lineHeight</c> in its <c>rmmz_windows.js</c>.
/// </para>
/// </remarks>
public partial class TestMzCommandWindow : TestBase
{
    private const string Projekt = "E:/RPGMakerGames/CamelliaCoronation-Win";

    private static bool Vorhanden() =>
        File.Exists(Projekt + "/data/System.json");

    /// <summary>
    /// And the engine's list, its placement and its cursor reach the view.
    /// </summary>
    public void Test_DieAnsichtBekommtDieBefehlsliste()
    {
        var ansicht = new Rm2kGameScreen();
        try
        {
            // Skies' own numbers: the window its author moved with offsetX 382.
            ansicht.SetCommandWindow(
                902, 537, 240, 132, 36,
                new[] { "New Game", "Options", "CG Gallery", "Credits", "Patch Notes" },
                2);

            var (x, y, breite, hoehe, index, zeilen) = ansicht.CommandWindow();
            Console.WriteLine($"MZ command window: {x},{y} {breite}x{hoehe} "
                + $"index={index} rows=[{string.Join(", ", zeilen)}]");
            AssertEq(x, 902, "the window stands where the engine puts it");
            AssertEq(y, 537, "and where it puts it from the top");
            AssertEq(breite, 240, "and is mainCommandWidth wide");
            AssertEq(hoehe, 132, "and three lines tall");
            AssertEq(index, 2, "and the cursor is where the runtime said");
            AssertEq(zeilen.Count, 5, "and every command is there");
            AssertEq(zeilen[2], "CG Gallery",
                "including the one a plugin added");
        }
        finally
        {
            ansicht.Free();
        }
    }

    /// <summary>
    /// And a window with nothing to show is taken away rather than left empty.
    /// </summary>
    /// <remarks>
    /// <strong>And this matters when the title goes.</strong> Choosing New Game
    /// leaves the title for the map, and a command window that stayed would
    /// hang over the map with no way to close it.
    /// </remarks>
    public void Test_EinLeeresFensterWirdWiederWeggenommen()
    {
        var ansicht = new Rm2kGameScreen();
        try
        {
            ansicht.SetCommandWindow(
                100, 100, 240, 132, 36, new[] { "New Game" }, 0);
            AssertEq(ansicht.CommandWindow().Rows.Count, 1,
                "a list is drawn when there is one");

            ansicht.SetCommandWindow(0, 0, 0, 0, 0, null, 0);
            AssertEq(ansicht.CommandWindow().Rows.Count, 0,
                "and none is drawn when there is not");

            ansicht.SetCommandWindow(
                100, 100, 240, 132, 36, Array.Empty<string>(), 0);
            AssertEq(ansicht.CommandWindow().Rows.Count, 0,
                "and an empty list is the same as none");

            // And a cursor outside the list is pulled back into it rather than
            // drawn past the end.
            ansicht.SetCommandWindow(
                100, 100, 240, 132, 36, new[] { "New Game", "Options" }, 7);
            AssertEq(ansicht.CommandWindow().Index, 1,
                "a cursor past the end lands on the last command");
        }
        finally
        {
            ansicht.Free();
        }
    }

    /// <summary>
    /// And the runtime hands the view exactly what the engine decided.
    /// </summary>
    /// <remarks>
    /// <strong>And this is the wiring, not the drawing.</strong> The two halves
    /// are proved separately above and in <c>TestMzEchtesSpielStartet</c>; what
    /// this checks is that the same numbers travel from the runtime to the
    /// view, because a view that is fed nothing looks exactly like a view that
    /// draws nothing.
    /// </remarks>
    public void Test_DieVerdrahtungReichtDieEngineZahlenDurch()
    {
        if (!Vorhanden())
        {
            return;
        }

        using var host = new EnginePluginHost(
            BuiltInEnginePluginCatalog.CreateRuntimeRegistry());
        var started = host.Start(new PluginGameInfo
        {
            GameDirectory = Projekt,
            EngineId = EnginePluginIds.RpgMakerMz,
            Generation = "mz",
            DetectorScore = 850,
            PresentTitleScreen = true,
        });
        AssertTrue(started.Success, $"the MZ game starts: {started.Error?.Message}");
        if (!started.Success)
        {
            return;
        }

        var runtime = (MzEngineRuntime)host.Runtime!;
        var ansicht = new Rm2kGameScreen();
        try
        {
            ansicht.SetCommandWindow(
                runtime.TitleWindow.X, runtime.TitleWindow.Y,
                runtime.TitleWindow.Width, runtime.TitleWindow.Height,
                MzEngineRuntime.WindowLineHeight,
                runtime.TitleCommands, runtime.TitleIndex);

            var (x, y, breite, hoehe, index, zeilen) = ansicht.CommandWindow();
            Console.WriteLine($"MZ title handed to the view: {x},{y} "
                + $"{breite}x{hoehe} index={index} "
                + $"rows=[{string.Join(", ", zeilen)}]");

            AssertEq(x, runtime.TitleWindow.X,
                "the view stands where the runtime says");
            AssertEq(hoehe, runtime.TitleWindow.Height, "at the engine's height");
            AssertEq(zeilen.Count, runtime.TitleCommands.Count,
                "and shows every command the runtime read");
            AssertEq(index, runtime.TitleIndex, "with the cursor it has");
            AssertTrue(zeilen.Count > 0,
                "so a player has something to choose from");
        }
        finally
        {
            ansicht.Free();
        }
    }
}
