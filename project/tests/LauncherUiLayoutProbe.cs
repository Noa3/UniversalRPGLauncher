using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using UniversalRPG.App.Library;
using UniversalRPG.App.Ui;
using UniversalRPG.Plugins;

namespace UniversalRPG.Tests;

/// <summary>Renders the real launcher in isolated settings and checks responsive controls.</summary>
public partial class LauncherUiLayoutProbe : Node
{
    private Main _main = null!;
    private string _output = "";

    public override async void _Ready()
    {
        try
        {
            _output = System.Environment.GetEnvironmentVariable("URPG_GUI_PROBE_OUTPUT")
                ?? throw new InvalidOperationException("Set URPG_GUI_PROBE_OUTPUT to an owned scratch directory.");
            Directory.CreateDirectory(_output);
            var library = new GameLibrary(pSettingsPath: Path.Combine(_output, "library.cfg"));
            library.SetRootPath(Path.Combine(_output, "games"));
            _main = new Main();
            Field<GameLibrary>("_library").Dispose();
            typeof(Main).GetField("_library", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(_main, library);
            _main.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            AddChild(_main);
            var timeout = DateTime.UtcNow.AddSeconds(10);
            while (Field<object?>("_scanOperation") != null)
            {
                if (DateTime.UtcNow > timeout) throw new TimeoutException("Isolated initial scan did not finish.");
                await Frames();
            }
            await Capture("empty");
            var fixture = ProjectSettings.GlobalizePath("res://tests/fixtures/rm2k-dragon-destiny");
            var entry = library.Import(fixture, false) ?? throw new InvalidOperationException("Fixture not imported.");
            if (!library.TrySelectEngine(entry, EnginePluginIds.RpgMaker2000, out var error))
                throw new InvalidOperationException(error);
            Invoke("DisplayLibrary");
            GetWindow().ContentScaleMode = Window.ContentScaleModeEnum.Disabled;
            GetWindow().ContentScaleSize = Vector2I.Zero;
            foreach (var size in new[] { new Vector2I(1100, 700), new Vector2I(768, 900),
                new Vector2I(375, 900), new Vector2I(320, 900), new Vector2I(1440, 900) })
            {
                GetWindow().Size = size;
                await Frames();
                Invoke("ApplyResponsiveLayout");
                await Frames();
                if (Field<ItemList>("_gameList").Size.Y < 120)
                    throw new InvalidOperationException($"The game list collapsed at {size}: {Field<ItemList>("_gameList").Size}");
                foreach (var control in new Control[] { Field<LineEdit>("_librarySearch"),
                    Field<OptionButton>("_librarySort"), Field<Button>("_chooseFolderButton"), Field<Button>("_launchButton") })
                {
                    var rect = control.GetGlobalRect();
                    if (rect.Position.X < -1 || rect.End.X > GetViewport().GetVisibleRect().Size.X + 1)
                    {
                        foreach (var child in _main.FindChildren("*", "Control", true, false).OfType<Control>()
                            .Where(p => p.IsVisibleInTree() && p.GetCombinedMinimumSize().X > size.X - 28))
                            GD.Print($"Oversized minimum: {child.GetPath()} {child.GetCombinedMinimumSize()}");
                        throw new InvalidOperationException($"{control.Name} overflows at {size}: {rect}");
                    }
                }
                var launch = Field<Button>("_launchButton");
                if (size.X >= 1000 && launch.GetGlobalRect().End.Y > size.Y)
                    throw new InvalidOperationException("The desktop launch action is below the fold.");
                await Capture($"library-{size.X}");
            }
            GetWindow().Size = new Vector2I(1100, 700);
            await Frames();
            GetWindow().PushInput(new InputEventKey { Keycode = Key.F, CtrlPressed = true, Pressed = true });
            await Frames();
            var search = Field<LineEdit>("_librarySearch");
            if (!search.HasFocus()) throw new InvalidOperationException("Ctrl+F did not focus library search.");
            foreach (var letter in "no-match")
                GetWindow().PushInput(new InputEventKey { Unicode = letter, Pressed = true });
            await Frames();
            if (Field<ItemList>("_gameList").ItemCount != 0 || !Field<Button>("_launchButton").Disabled)
                throw new InvalidOperationException("Typed search did not clear the hidden launch selection.");
            await Capture("no-matches");
            search.Text = "";
            search.EmitSignal(LineEdit.SignalName.TextChanged, "");
            var settings = (Button)_main.FindChild("LauncherPreferencesButton", true, false);
            settings.ButtonPressed = true;
            await Frames();
            await Capture("preferences");
            settings.ButtonPressed = false;
            GetWindow().Size = new Vector2I(375, 900);
            await Frames();
            settings.ButtonPressed = true;
            await Frames();
            await Capture("preferences-375");
            GD.Print("Launcher GUI probe passed: five viewport widths, typed search, Ctrl+F, empty and preferences states.");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    private async Task Frames()
    {
        for (var i = 0; i < 3; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private async Task Capture(string pName)
    {
        await Frames();
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        var error = GetViewport().GetTexture().GetImage().SavePng(Path.Combine(_output, pName + ".png"));
        if (error != Error.Ok) throw new IOException($"Could not capture {pName}: {error}");
    }

    private T Field<T>(string pName) => (T)typeof(Main)
        .GetField(pName, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_main)!;

    private void Invoke(string pName) => typeof(Main)
        .GetMethod(pName, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(_main, null);
}
