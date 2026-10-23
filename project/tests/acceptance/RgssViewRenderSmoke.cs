using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using UniversalRPG.App.Launcher;
using UniversalRPG.App.Library;
using UniversalRPG.App.Ui;
using UniversalRPG.Plugins;
using UniversalRPG.Rgss;

namespace UniversalRPG.Tests.Acceptance;

/// <summary>Rendered synthetic RGSS witness; run separately with a native renderer, not headless.</summary>
public partial class RgssViewRenderSmoke : Node
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    public override async void _Ready()
    {
        Main? main = null;
        var exitCode = 1;
        var output = "";
        try
        {
            foreach (var argument in OS.GetCmdlineUserArgs())
                if (argument.StartsWith("--output=", StringComparison.Ordinal)) output = argument[9..];
            if (output.Length == 0) throw new InvalidOperationException("Pass --output=<isolated capture directory>.");
            Directory.CreateDirectory(output);
            var pictures = Path.Combine(output, "Graphics", "Pictures");
            Directory.CreateDirectory(pictures);
            var seedPixels = new byte[8 * 6 * 4];
            for (var i = 0; i < seedPixels.Length; i += 4)
            {
                seedPixels[i] = 255;
                seedPixels[i + 3] = 255;
            }
            using (var seed = Image.CreateFromData(8, 6, false, Image.Format.Rgba8, seedPixels))
                if (seed.SavePng(Path.Combine(pictures, "witness.png")) != Error.Ok)
                    throw new IOException("Could not write the file-backed sprite fixture.");
            using var scene = GD.Load<PackedScene>("res://scenes/main.tscn");
            main = scene.Instantiate<Main>();
            Field<GameLibrary>(main, "_library").Dispose();
            SetField(main, "_library", new GameLibrary(pSettingsPath: Path.Combine(output, "library.cfg")));
            AddChild(main);
            main.SetProcess(false);
            var graphics = new RgssGrafikHost { Wurzel = output };
            var ruby = new RubyInterpreter(graphics);
            using var runtime = new XpEngineRuntime("render-smoke", new PluginGameInfo());
            SetField(runtime, "_grafik", graphics);
            SetField(runtime, "_interpreter", ruby);
            SetField(runtime, "_scriptsRun", true);
            typeof(XpEngineRuntime).GetMethod("DefineBuiltinClasses", Private)!.Invoke(runtime, null);
            SetProperty(runtime, "State", PluginRuntimeState.Initialized);
            ruby.RunProgram(new RubyParser(new RubyLexer("""
                class RenderWitness
                  def main
                    Graphics.resize_screen(8, 6)
                    bitmap = RPG::Cache.picture("witness")
                    sprite = Sprite.new
                    sprite.bitmap = bitmap
                    Graphics.update
                    bitmap.fill_rect(0, 0, 8, 6, Color.new(0, 255, 0, 255))
                    loop do
                      Graphics.update
                    end
                  end
                end
                $scene = RenderWitness.new
                """).Tokenize()).ParseProgram());
            if (!runtime.Start().Success) throw new InvalidOperationException("Runtime start failed.");
            using var pluginHost = new EnginePluginHost(BuiltInEnginePluginCatalog.CreateRuntimeRegistry());
            SetProperty(pluginHost, "Runtime", runtime);
            SetProperty(pluginHost, "State", PluginRuntimeState.Running);
            var launcher = Field<RuntimeLauncher>(main, "_launcher");
            SetField(launcher, "_activeHost", pluginHost);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            main._Process(1.0 / 60);
            await Capture(output, "frame-red.png", Colors.Red);
            main._Process(1.0 / 60);
            await Capture(output, "frame-green.png", new Color(0, 1, 0, 1));
            if (runtime.Frames != 2) throw new InvalidOperationException("Unexpected runtime frame count.");
            if (!launcher.Stop().Success) throw new InvalidOperationException("Runtime stop failed.");
            main._Process(0);
            if (Field<Rm2kGameScreen>(main, "_gameScreen").Visible)
                throw new InvalidOperationException("Stopped view remained visible.");
            GD.Print("RGSS_RENDER_WITNESS_PASS: file-backed cached PNG rendered red, then pixel writes rendered green; stop cleared the view.");
            exitCode = 0;
        }
        catch (Exception error)
        {
            GD.PushError("RGSS_RENDER_WITNESS_FAILED: " + error);
        }
        finally
        {
            main?.Free();
            GetTree().Quit(exitCode);
        }
    }

    private async Task Capture(string output, string filename, Color expected)
    {
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        using var image = GetViewport().GetTexture().GetImage();
        if (image == null || image.IsEmpty()) throw new InvalidOperationException("No native viewport image.");
        var result = image.SavePng(Path.Combine(output, filename));
        if (result != Error.Ok) throw new IOException("Capture write failed: " + result);
        var pixel = image.GetPixel(image.GetWidth() / 2, image.GetHeight() / 2);
        if (!pixel.IsEqualApprox(expected))
            throw new InvalidOperationException($"Unexpected viewport center {pixel}; expected {expected}.");
        GD.Print($"RGSS_CAPTURE: {filename}, {image.GetWidth()}x{image.GetHeight()}, center={pixel}");
    }

    private static T Field<T>(object owner, string name) =>
        (T)owner.GetType().GetField(name, Private)!.GetValue(owner)!;
    private static void SetField(object owner, string name, object value) =>
        owner.GetType().GetField(name, Private)!.SetValue(owner, value);
    private static void SetProperty(object owner, string name, object value) =>
        owner.GetType().GetProperty(name)!.GetSetMethod(true)!.Invoke(owner, new[] { value });
}
