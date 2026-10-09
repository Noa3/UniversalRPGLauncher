using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using UniversalRPG.App.Library;
using UniversalRPG.App.Ui;

namespace UniversalRPG.Tests;

/// <summary>Exercises and captures the real folder-scan view without touching user settings.</summary>
public partial class LibraryScanUiProbe : Node
{
    public override async void _Ready()
    {
        GameLibraryScan? scan = null;
        Main? main = null;
        var scratch = System.Environment.GetEnvironmentVariable("URPG_SCAN_PROBE_OUTPUT")
            ?? ProjectSettings.GlobalizePath("user://scan-ui-probe");
        Directory.CreateDirectory(scratch);
        try
        {
            var root = System.Environment.GetEnvironmentVariable("URPG_SCAN_PROBE_ROOT")
                ?? throw new InvalidOperationException("Set URPG_SCAN_PROBE_ROOT to a collection directory.");
            main = new Main();
            Field<GameLibrary>(main, "_library").Dispose();
            var library = new GameLibrary(pSettingsPath: Path.Combine(scratch, "library.cfg"));
            if (library.SetRootPath(root) != Error.Ok) throw new IOException("The probe directory could not be selected.");
            typeof(Main).GetField("_library", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(main, library);
            main.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            var watch = Stopwatch.StartNew();
            AddChild(main);
            var kickoff = watch.ElapsedMilliseconds;
            scan = Field<GameLibraryScan>(main, "_scanOperation");
            if (scan == null) throw new InvalidOperationException("The real launcher's startup scan did not start.");
            var completion = scan.Completion;
            var frames = 0;
            var feedbackSeen = false;
            while (!completion.IsCompleted && watch.Elapsed < TimeSpan.FromMinutes(3))
            {
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                frames++;
                feedbackSeen |= Field<Control>(main, "_scanPanel").Visible;
                if (frames == 3)
                {
                    await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                    var error = GetViewport().GetTexture().GetImage().SavePng(Path.Combine(scratch, "scanning.png"));
                    if (error != Error.Ok) throw new IOException("Scan screenshot could not be written.");
                }
            }
            if (!completion.IsCompleted) throw new TimeoutException("The collection scan exceeded the probe budget.");
            await completion;
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            var result = new
            {
                kickoffMilliseconds = kickoff,
                elapsedMilliseconds = watch.ElapsedMilliseconds,
                renderedFramesWhileScanning = frames,
                feedbackSeen,
                directories = scan.Progress.DirectoriesScanned,
                games = library.Games.Count,
                unreadableDirectories = scan.Progress.UnreadableDirectories,
                limitReached = scan.Progress.LimitReached,
                complete = Field<object?>(main, "_scanOperation") == null
            };
            File.WriteAllText(Path.Combine(scratch, "result.json"), JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
            GD.Print(JsonSerializer.Serialize(result));
            if (kickoff > 500 || !feedbackSeen || frames < 2 || !result.complete)
                throw new InvalidOperationException("The real collection scan did not meet the responsive-feedback contract.");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
        finally
        {
            main?._ExitTree();
            if (scan != null)
            {
                try { await scan.Completion; }
                catch (OperationCanceledException) { }
            }
        }
    }

    private static T Field<T>(Main main, string name) =>
        (T)typeof(Main).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(main)!;
}
