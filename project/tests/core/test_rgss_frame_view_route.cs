using System;
using System.IO;
using System.Reflection;
using Godot;
using UniversalRPG.App.Launcher;
using UniversalRPG.App.Library;
using UniversalRPG.App.Ui;
using UniversalRPG.Plugins;
using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>Tests the actual UI consumer, not permission to launch an Experimental engine.</summary>
public partial class TestRgssFrameViewRoute : TestBase
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private Main _main = null!;
    private string _root = "";
    private string _locale = "";
    private int _fps;

    public override void Setup()
    {
        _fps = Engine.MaxFps;
        _locale = TranslationServer.GetLocale();
        _root = ProjectSettings.GlobalizePath("user://rgss-frame-route/" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
        _main = new Main();
        Field<GameLibrary>(_main, "_library").Dispose();
        SetField(_main, "_library", new GameLibrary(pSettingsPath: Path.Combine(_root, "library.cfg")));
        (Host ?? throw new InvalidOperationException("Missing test scene host.")).AddChild(_main);
        _main.SetProcess(false);
    }

    public override void Teardown()
    {
        Field<RuntimeLauncher>(_main, "_launcher").Stop();
        _main.Free();
        Directory.Delete(_root, true);
        TranslationServer.SetLocale(_locale);
        Engine.MaxFps = _fps;
    }

    public void Test_AllRgssGenerationsReachTheNativeViewWithTheirOwnFrameNumbers()
    {
        foreach (var type in new[] { typeof(XpEngineRuntime), typeof(VxEngineRuntime), typeof(VxAceEngineRuntime) })
        {
            var graphics = new RgssGrafikHost();
            var interpreter = new RubyInterpreter(graphics);
            using var runtime = CreateRuntime(type, graphics, interpreter);
            interpreter.RunProgram(new RubyParser(new RubyLexer("""
                class ViewScene
                  def main
                    Graphics.resize_screen(4, 3)
                    bitmap = Bitmap.new(1, 1)
                    bitmap.set_pixel(0, 0, Color.new(255, 0, 0, 255))
                    sprite = Sprite.new
                    sprite.bitmap = bitmap
                    unsupported = Sprite.new
                    unsupported.bitmap = bitmap
                    unsupported.angle = 1
                    Graphics.update
                    bitmap.set_pixel(0, 0, Color.new(0, 255, 0, 255))
                    loop do
                      Graphics.update
                    end
                  end
                end
                $scene = ViewScene.new
                """).Tokenize()).ParseProgram());
            AssertTrue(runtime.Start().Success);
            AssertTrue(runtime.Update(1.0 / 60).Success);
            AssertTrue(runtime.Update(1.0 / 60).Success);
            var source = (IRgssFrameSource)runtime;
            AssertEq(source.Frames, 2);
            var launcher = Field<RuntimeLauncher>(_main, "_launcher");
            var screen = Field<Rm2kGameScreen>(_main, "_gameScreen");
            using var pluginHost = new EnginePluginHost(BuiltInEnginePluginCatalog.CreateRuntimeRegistry());
            SetProperty(pluginHost, "Runtime", runtime);
            SetProperty(pluginHost, "State", PluginRuntimeState.Running);
            SetField(launcher, "_activeHost", pluginHost);
            ImageTexture? texture = null;
            try
            {
                screen.SetCommandWindow(0, 0, 100, 80, 20, new[] { "Old menu" }, 0);
                screen.SetPresentation(true, "Old message", new[] { "Old choice" }, 0, true, 7);
                _main._Process(1.0 / 60);
                AssertTrue(screen.Visible, type.Name + " must enter the full game view.");
                AssertFalse(Field<MarginContainer>(_main, "_pageMargin").Visible);
                AssertEq(source.Frames, 3);
                AssertEq(Field<int>(screen, "_frameCount"), source.Frames,
                    "Use the engine's frame number, not a count of consumed snapshots.");
                AssertEq(screen.CommandWindow().Rows.Count, 0);
                AssertFalse(Field<bool>(screen, "_messageVisible"));
                AssertFalse(Field<bool>(screen, "_inputPending"));
                AssertTrue(source.RenderedFrame!.Warning.Length > 0);
                AssertEq(screen.RenderDiagnostic, source.RenderedFrame.Warning);
                texture = (ImageTexture?)typeof(Rm2kGameScreen).GetMethod("GetMapTexture", Private)!.Invoke(screen, null);
                AssertTrue(texture != null, "The routed frame must become a native texture.");
                if (texture != null)
                {
                    AssertEq(texture.GetWidth(), 4);
                    AssertEq(texture.GetHeight(), 3);
                    using var image = texture.GetImage();
                    AssertTrue(image != null && !image.IsEmpty());
                    if (image != null && !image.IsEmpty()) AssertEq(image.GetPixel(0, 0), new Color(0, 1, 0, 1));
                }
                screen.OpenPause();
                _main._Process(1.0 / 60);
                AssertEq(source.Frames, 3, "The visible pause menu must prevent another runtime advance.");
                AssertTrue(launcher.Stop().Success);
                _main._Process(0);
                AssertFalse(screen.Visible);
                AssertTrue(Field<MarginContainer>(_main, "_pageMargin").Visible);
                AssertFalse(screen.IsPauseOpen);
                AssertEq(Field<int>(screen, "_frameCount"), 0);
            }
            finally
            {
                launcher.Stop();
                SetField(launcher, "_activeHost", null);
                screen.Reset();
                texture?.Dispose();
            }
        }
    }

    private static IEngineRuntime CreateRuntime(Type type, RgssGrafikHost graphics, RubyInterpreter interpreter)
    {
        var runtime = (IEngineRuntime)Activator.CreateInstance(type, "test", new PluginGameInfo())!;
        SetField(runtime, "_interpreter", interpreter);
        SetField(runtime, "_grafik", graphics);
        SetField(runtime, "_scriptsRun", true);
        type.GetMethod("DefineBuiltinClasses", Private)!.Invoke(runtime, null);
        SetProperty(runtime, "State", PluginRuntimeState.Initialized);
        return runtime;
    }

    private static T Field<T>(object owner, string name) =>
        (T)owner.GetType().GetField(name, Private)!.GetValue(owner)!;
    private static void SetField(object owner, string name, object? value) =>
        owner.GetType().GetField(name, Private)!.SetValue(owner, value);
    private static void SetProperty(object owner, string name, object value) =>
        owner.GetType().GetProperty(name)!.GetSetMethod(true)!.Invoke(owner, new[] { value });
}
