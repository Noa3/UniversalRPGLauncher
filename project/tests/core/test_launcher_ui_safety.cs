using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Godot;
using UniversalRPG.App.Launcher;
using UniversalRPG.App.Library;
using UniversalRPG.App.Ui;
using UniversalRPG.GameDetectorNs;
using UniversalRPG.Plugins;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

public partial class TestLauncherUiSafety : TestBase
{
    private Main _main = null!;
    private string _root = "";
    private int _renderFps;

    public override void Setup()
    {
        _renderFps = Engine.MaxFps;
        _root = ProjectSettings.GlobalizePath("user://launcher-safety/" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
        var fixture = ProjectSettings.GlobalizePath("res://tests/fixtures/rm2k-dragon-destiny");
        foreach (var file in Directory.GetFiles(fixture))
        {
            if (Path.GetExtension(file) is ".ldb" or ".lmt" or ".lmu" or ".ini")
                File.Copy(file, Path.Combine(_root, Path.GetFileName(file)));
        }
        _main = new Main();
        Field<GameLibrary>("_library").Dispose();
        typeof(Main).GetField("_library", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(_main, new GameLibrary(pSettingsPath: Path.Combine(_root, "library.cfg")));
        // Build the real controls off-tree so tests do not modify the live launcher scene.
        Invoke("BuildInterface");
        Field<MzAudioOutput>("_mzAudio")._Ready();
    }

    public override void Teardown()
    {
        Field<RuntimeLauncher>("_launcher").Stop();
        // **Und die Kinder werden *vor* dem Freigeben des Elternknotens in
        // die Freigabe geschickt.**  `Free()` auf einen Knoten, der nicht mehr
        // im Baum ist, raeumt die unterhaengenden Controls nicht zuverlaessig
        // ab, und das liess 50 verwaiste Objekte pro Lauf zurueck.
        foreach (var child in _main.GetChildren())
        {
            child.QueueFree();
        }
        if (_main.IsInsideTree())
        {
            _main.GetParent()?.RemoveChild(_main);
        }
        _main.Free();
        Directory.Delete(_root, true);
        Engine.MaxFps = _renderFps;
    }

    public void Test_MessageButtonReleasesInterpreterWait()
    {
        var runtime = StartRuntime();
        runtime.Presentation.ShowMessage("continue");
        runtime.Simulation.WaitingFor = GameSimulationState.WaitReason.MessageOpen;
        Field<Button>("_dismissMessageButton").EmitSignal(Button.SignalName.Pressed);
        AssertFalse(runtime.Presentation.MessageVisible);
        AssertEq(runtime.Simulation.WaitingFor, GameSimulationState.WaitReason.None,
            "The Continue button must resume the event, not only hide its text.");
    }

    public void Test_RuntimeStopsWhenLauncherLeavesTree()
    {
        StartRuntime();
        var launcher = Field<RuntimeLauncher>("_launcher");
        _main._ExitTree();
        AssertFalse(launcher.ActiveRuntimeState == PluginRuntimeState.Running,
            "Scene reload/close must not leave a running host behind.");
    }

    public void Test_StartButtonUsesFullDetectionReport()
    {
        var library = Field<GameLibrary>("_library");
        var entry = SelectedEntry();
        library.Games.Add(entry);
        Invoke("SelectGame", 0L);
        AssertFalse(Field<Button>("_launchButton").Disabled,
            "A verified selection must not be refused by an enum-only report.");
    }

    public void Test_UnwritableSaveDirectoryReturnsError()
    {
        var runtime = StartRuntime();
        File.WriteAllText(Path.Combine(_root, "Save"), "not a directory");
        var success = runtime.TrySaveSlot("test", out var error);
        AssertFalse(success);
        AssertTrue(!string.IsNullOrEmpty(error), "An I/O failure must be returned, not thrown.");
    }

    public void Test_WorkerProgressOnlyTouchesControlsDuringProcess()
    {
        var status = Field<Label>("_status");
        var previous = status.Text;
        var progress = Field<System.Collections.Concurrent.ConcurrentQueue<string>>("_rtpProgress");
        System.Threading.Tasks.Task.Run(() => progress.Enqueue("worker progress"))
            .GetAwaiter().GetResult();
        AssertEq(status.Text, previous, "Worker callbacks must not change Godot controls.");
        _main._Process(0);
        AssertTrue(status.Text.Contains("worker progress", StringComparison.Ordinal));
    }

    public void Test_EngineChoiceButtonEnablesAmbiguousRealFixture()
    {
        var library = Field<GameLibrary>("_library");
        var entry = library.Import(_root, false)!;
        AssertTrue(entry.Detection.Report.IsAmbiguous);
        Invoke("SelectGame", 0L);
        var field = typeof(Main).GetField("_engineChoice", BindingFlags.Instance | BindingFlags.NonPublic);
        AssertTrue(field != null, "The launcher must offer an explicit choice for ambiguous LCF games.");
        if (field == null) return;
        var menu = (OptionButton)field.GetValue(_main)!;
        var index = -1;
        for (var i = 0; i < menu.ItemCount; i++)
            if (menu.GetItemMetadata(i).AsString() == EnginePluginIds.RpgMaker2000) index = i;
        AssertTrue(index >= 0);
        if (index < 0) return;
        menu.Select(index);
        menu.EmitSignal(OptionButton.SignalName.ItemSelected, (long)index);
        AssertFalse(Field<Button>("_launchButton").Disabled);
        AssertFalse(entry.Detection.Report.IsAmbiguous);
        AssertEq(entry.SelectedPluginId, EnginePluginIds.RpgMaker2000);
    }

    public void Test_LongDetailsScrollWithoutHidingTheStartButton()
    {
        AssertTrue(HasScrollAncestor(Field<Label>("_detailsEvidence")),
            "Long detection details must scroll instead of pushing controls off-screen.");
        AssertFalse(HasScrollAncestor(Field<Button>("_launchButton")));
    }

    public void Test_StopLivesInTheGamePauseMenuAndNotInTheLauncher()
    {
        AssertTrue(typeof(Main).GetField("_stopButton", BindingFlags.Instance | BindingFlags.NonPublic) == null,
            "The launcher must not carry a stop button next to Start; stopping belongs in the game menu.");
        var screen = Field<Rm2kGameScreen>("_gameScreen");
        var menu = (VBoxContainer)NodeOf(screen, "_pauseMain");
        var entries = 0;
        foreach (var child in menu.GetChildren())
        {
            if (child is Button)
            {
                entries += 1;
            }
        }
        AssertEq(entries, 5,
            "The pause menu must offer resume, options, cheats, stop runtime and close program.");
    }

    public void Test_GameViewLetterboxesTheComposedScreen()
    {
        // The frame is already the game's 320x240 view; a 1100x700 window
        // therefore scales by 700/240 and leaves black side bars.
        var (dest, src) = Rm2kGameScreen.ComputeGameView(
            new Vector2(1100, 700), 320, 240, false);
        var scale = 700.0f / 240.0f;
        AssertEq(src.Position, Vector2.Zero, "the source is the whole composed frame");
        AssertEq(src.Size, new Vector2(320, 240), "the frame is exactly one RM2K screen");
        AssertTrue(MathF.Abs(dest.Size.X - 320 * scale) < 0.01f, "width keeps the pixel aspect");
        AssertTrue(MathF.Abs(dest.Size.Y - 700) < 0.01f, "height fills the window");
        AssertTrue(MathF.Abs(dest.Position.X - (1100 - 320 * scale) / 2.0f) < 0.01f,
            "the screen is centred and the sides stay black");
    }

    public void Test_GameViewIntegerScaleUsesWholeFactors()
    {
        var (dest, _) = Rm2kGameScreen.ComputeGameView(
            new Vector2(1100, 700), 320, 240, true);
        AssertTrue(MathF.Abs(dest.Size.X - 640) < 0.01f, "integer scale uses floor(700/240)=2");
        AssertTrue(MathF.Abs(dest.Size.Y - 480) < 0.01f, "integer scale uses floor(700/240)=2");
    }

    public void Test_GameViewBarsAQuadraticWindow()
    {
        var (dest, src) = Rm2kGameScreen.ComputeGameView(
            new Vector2(640, 640), 320, 240, false);
        AssertEq(src.Size, new Vector2(320, 240), "the frame stays one screen");
        AssertTrue(MathF.Abs(dest.Size.X - 640) < 0.01f, "the width fills the window");
        AssertTrue(MathF.Abs(dest.Size.Y - 480) < 0.01f, "the height keeps the aspect");
        AssertTrue(MathF.Abs(dest.Position.Y - 80) < 0.01f, "the remainder is bars above and below");
    }

    public void Test_PauseMenuTogglesAndShowsPresentation()
    {
        var screen = Field<Rm2kGameScreen>("_gameScreen");
        // Opening the menu grabs focus, and GrabFocus on a node outside the tree
        // is a Godot error; the launcher itself is already under the runner from
        // an earlier test, so mount it if needed and leave it there afterwards.
        var runner = Host ?? throw new InvalidOperationException("the runner published no host node");
        if (!screen.IsInsideTree())
        {
            runner.AddChild(_main);
        }
        screen.TogglePause();
        AssertTrue(screen.IsPauseOpen, "F4 opens the pause menu");
        screen.TogglePause();
        AssertFalse(screen.IsPauseOpen, "F4 closes it again");

        screen.SetPresentation(true, "hello", null, -1, false, 0);
        AssertTrue(NodeOf(screen, "_messagePanel").Visible, "a visible message gets a box");
        screen.SetPresentation(false, "", null, -1, false, 0);
        AssertFalse(NodeOf(screen, "_messagePanel").Visible, "a hidden message hides the box");

        screen.SetPresentation(true, "pick", new[] { "Yes", "No" }, 0, false, 0);
        var choiceBox = (VBoxContainer)NodeOf(screen, "_choiceBox");
        AssertEq(choiceBox.GetChildCount(), 2, "one button per choice option");
        var picked = -1;
        screen.ChoiceSelected += index => picked = index;
        ((Button)choiceBox.GetChild(1)).EmitSignal(Button.SignalName.Pressed);
        AssertEq(picked, 1, "clicking the second choice reports index 1");
    }

    private static bool HasButtonWithText(Node pRoot, string pText)
    {
        if (pRoot is Button button && button.Text == pText)
        {
            return true;
        }
        foreach (var child in pRoot.GetChildren())
        {
            if (HasButtonWithText(child, pText))
            {
                return true;
            }
        }
        return false;
    }

    private static Control NodeOf(Node pRoot, string pName) =>
        (Control)typeof(Rm2kGameScreen).GetField(pName, BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(pRoot)!;

    public void Test_IdlePreviewsDoNotConsumeTheLauncherLayout()
    {
        _main._Process(0);
        AssertFalse(Field<MzMapPreview>("_mzMap").Visible);
        AssertFalse(Field<Rm2kMapPreview>("_mapPreview").Visible);
    }

    private static bool HasScrollAncestor(Node pNode)
    {
        for (var parent = pNode.GetParent(); parent != null; parent = parent.GetParent())
            if (parent is ScrollContainer) return true;
        return false;
    }

    private Rm2kEngineRuntime StartRuntime()
    {
        var library = Field<GameLibrary>("_library");
        var entry = SelectedEntry();
        var launcher = Field<RuntimeLauncher>("_launcher");
        var result = launcher.Launch(entry!);
        if (!result.Success) throw new InvalidOperationException(result.Message);
        return (Rm2kEngineRuntime)launcher.ActiveRuntime!;
    }

    /// <summary>
    /// And the arrow keys reach the runtime through the real Godot input
    /// chain, on a real game whose start tile is passable.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this deliberately uses Diary (Lisa)</strong>, --
    /// <strong>because the Dragon Destiny fixture starts on an editor-empty map
    /// whose own lower table carries zero passability, and a hero standing in a
    /// wall cannot move in any correct implementation.</strong>
    /// </para>
    /// <para>
    /// <strong>And the events go through <c>Viewport.PushInput</c></strong>, --
    /// <strong>not into <c>_UnhandledInput</c> directly, because the viewport is
    /// what decides whether a focused Control already ate the key; calling the
    /// handler by hand would test a path the window never takes.</strong>
    /// </para>
    /// </remarks>
    public void Test_ArrowKeysReachTheRuntimeThroughTheRealInputChain()
    {
        const string spiel = "E:/RPGMakerGames/Lisa";
        if (!File.Exists(spiel + "/RPG_RT.ldb"))
        {
            return;
        }
        var library = Field<GameLibrary>("_library");
        var entry = new GameLibrary.GameEntry(spiel, new GameDetector.DetectionResult
        {
            Engine = GameDetector.EngineType.RpgMaker2003,
            GameDirectory = spiel,
            Report = new EngineDetectionReport
            {
                SourcePath = spiel,
                SelectedCandidate = new EngineDetectionCandidate
                {
                    PluginId = EnginePluginIds.RpgMaker2003,
                    EngineId = EnginePluginIds.RpgMaker2003,
                    // **Und die Generation ist `rm2k3`, genau wie im
                    // gespeicherten Candidate der echten Bibliothek** --
                    // **denn `BuiltInEnginePlugin.Match` setzt sie aus der
                    // Plugin-Metadaten-Generation, und `PluginEngineRange
                    // .Matches` vergleicht sie mit der des Spiels.**
                    // **Und `rm2k` haette hier einen `UnsupportedEngine`
                    // geworfen, obwohl der Start in der echten Bibliothek
                    // genau so funktioniert.**
                    Generation = "rm2k3",
                    Status = EngineDetectionStatus.Supported,
                    Score = 1000,
                    Reason = "The launcher starts Diary as rpg-maker-2003.",
                },
            },
        });
        library.Games.Add(entry);
        var launcher = Field<RuntimeLauncher>("_launcher");
        var result = launcher.Launch(entry);
        if (!result.Success) throw new InvalidOperationException(result.Message);
        var runtime = (Rm2kEngineRuntime)launcher.ActiveRuntime!;

        // The launcher already built its controls off-tree in Setup(); mounting
        // it now runs _Ready again, so the input host is what the window sees.
        var runner = Host ?? throw new InvalidOperationException("the runner published no host node");
        var viewport = runner.GetViewport()
            ?? throw new InvalidOperationException("the runner host has no viewport");
        var inputHost = new Node { Name = "ArrowInputHost" };
        runner.AddChild(inputHost);
        var childrenBefore = _main.GetChildCount();
        inputHost.AddChild(_main);
        // _Ready calls BuildInterface() again; building the form twice is a real
        // defect (duplicate controls, doubled theme work), not a test artefact.
        AssertEq(_main.GetChildCount(), childrenBefore,
            "entering the tree must not build a second copy of the launcher form");

        // Simulation.Steps counts the tiles of one move in progress, so it is
        // not a running total; the hero's own map position is the evidence.
        var perKey = new Dictionary<string, string>();
        var movedKeys = 0;
        foreach (var keycode in new[] { Key.Right, Key.Down, Key.Left, Key.Up, Key.D, Key.S })
        {
            var beforeX = runtime.Simulation.MapX;
            var beforeY = runtime.Simulation.MapY;
            viewport.PushInput(new InputEventKey { Keycode = keycode, Pressed = true });
            for (var frame = 0; frame < 30; frame++) runtime.Update(1.0 / 60.0);
            var deltaX = runtime.Simulation.MapX - beforeX;
            var deltaY = runtime.Simulation.MapY - beforeY;
            perKey[keycode.ToString()] = $"{deltaX:+#;-#;0}/{deltaY:+#;-#;0}";
            if (deltaX != 0 || deltaY != 0) movedKeys += 1;
        }
        GD.Print("Lisa arrow delta x/y: " + string.Join(", ", perKey.Select(p => p.Key + "=" + p.Value))
            + $" | start {runtime.Simulation.MapX}/{runtime.Simulation.MapY}");

        inputHost.RemoveChild(_main);
        runner.RemoveChild(inputHost);
        inputHost.QueueFree();

        AssertTrue(movedKeys >= 4,
            $"every arrow key sent to the window must move the hero; moved: "
            + string.Join(", ", perKey.Select(p => p.Key + "=" + p.Value)));
    }

    /// <summary>
    /// And the pause menu is usable with the keyboard alone.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this matters because the launcher marks every key as
    /// handled while the menu is open</strong>, -- <strong>so an arrow key
    /// that nothing handles is a key the player presses and nothing
    /// happens.</strong>
    /// </para>
    /// </remarks>
    public void Test_GameViewFillsTheWindowWithAWholeMap()
    {
        // MV hands over the whole painted map, measured at LegalTruck:
        // 816x624. A view that still assumed 320x240 would show the top
        // left corner of a map three screens wide and call it the game.
        var (dest, src) = Rm2kGameScreen.ComputeGameView(
            new Vector2(1100, 700), 816, 624, false);
        var scale = 700.0f / 624.0f;
        AssertEq(src.Size, new Vector2(816, 624), "the source is the whole MV map");
        AssertTrue(MathF.Abs(dest.Size.Y - 700) < 0.01f, "the map fills the window height");
        AssertTrue(MathF.Abs(dest.Size.X - 816 * scale) < 0.01f,
            "the width keeps the aspect and leaves side bars");
    }

    public void Test_GameViewTakesAnMzFrameOfItsOwnSize()
    {
        // Camellia's start map is 672x864, measured -- taller than wide, so
        // in an 800x900 window the height is the binding side.
        var (dest, src) = Rm2kGameScreen.ComputeGameView(
            new Vector2(800, 900), 672, 864, false);
        var scale = 900.0f / 864.0f;
        AssertEq(src.Size, new Vector2(672, 864), "the source is the MZ frame");
        AssertTrue(MathF.Abs(dest.Size.Y - 900) < 0.01f, "the MZ map fills the height");
        AssertTrue(MathF.Abs(dest.Size.X - 672 * scale) < 0.01f,
            "the width follows the same factor and leaves side bars");
        AssertTrue(MathF.Abs(dest.Position.Y) < 0.01f, "no bar above or below");
    }

    public void Test_GameViewRefusesAFrameWithoutSize()
    {
        var (dest, src) = Rm2kGameScreen.ComputeGameView(
            new Vector2(1100, 700), 0, 0, false);
        AssertEq(src.Size, Vector2.Zero,
            "an empty frame draws nothing instead of a screen of black");
    }

    public void Test_PauseMenuOpensFocusedAndMovesWithTheArrowKeys()
    {
        var runner = Host ?? throw new InvalidOperationException("the runner published no host node");
        var screen = new Rm2kGameScreen();
        var screenHost = new Node { Name = "PauseMenuHost" };
        runner.AddChild(screenHost);
        screenHost.AddChild(screen);
        try
        {
            screen.OpenPause();
            AssertTrue(screen.IsPauseOpen, "F4 opens the pause menu");
            var page = screen.CurrentPausePage();
            AssertTrue(page != null, "the menu shows a page with buttons");
            var buttons = page!.GetChildren().OfType<Button>().ToList();
            AssertEq(buttons.Count, 5,
                "the menu offers resume, options, cheats, stop runtime and close program");
            AssertTrue(buttons[0].HasFocus(),
                "the first entry carries the focus, so Enter has something to press");

            AssertTrue(screen.HandlePauseKey(new InputEventKey { Keycode = Key.Down, Pressed = true }),
                "the down arrow is claimed by the menu");
            AssertTrue(buttons[1].HasFocus(), "the down arrow moves the selection to Options");
            AssertTrue(screen.HandlePauseKey(new InputEventKey { Keycode = Key.Up, Pressed = true }),
                "the up arrow is claimed by the menu");
            AssertTrue(buttons[0].HasFocus(), "the up arrow moves the selection back");
            AssertTrue(screen.HandlePauseKey(new InputEventKey { Keycode = Key.Up, Pressed = true }),
                "the up arrow wraps");
            AssertTrue(buttons[^1].HasFocus(),
                "the up arrow from the first entry wraps to the last one");
            AssertFalse(screen.HandlePauseKey(new InputEventKey { Keycode = Key.Q, Pressed = true }),
                "a key the menu does not use is not swallowed");
            AssertFalse(screen.HandlePauseKey(new InputEventKey { Keycode = Key.Down, Echo = true, Pressed = true }),
                "a key repeat does not walk the selection twice");

            screen.ClosePause();
            AssertFalse(screen.HandlePauseKey(new InputEventKey { Keycode = Key.Down, Pressed = true }),
                "a closed menu does not claim the arrow keys again");
        }
        finally
        {
            runner.RemoveChild(screenHost);
            screenHost.QueueFree();
        }
    }

    private GameLibrary.GameEntry SelectedEntry() => new(_root, new GameDetector.DetectionResult
    {
        Engine = GameDetector.EngineType.RpgMaker2000,
        GameDirectory = _root,
        Report = new EngineDetectionReport
        {
            SourcePath = _root,
            SelectedCandidate = new EngineDetectionCandidate
            {
                PluginId = EnginePluginIds.RpgMaker2000,
                EngineId = EnginePluginIds.RpgMaker2000,
                Generation = "rm2k",
                Status = EngineDetectionStatus.Supported,
                Score = 1000,
                Reason = "Explicit RM2K selection for a real LCF fixture.",
            },
        },
    });

    private T Field<T>(string pName) => (T)typeof(Main)
        .GetField(pName, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_main)!;

    private void Invoke(string pName, params object[] pArguments) => typeof(Main)
        .GetMethod(pName, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(_main, pArguments);
}
