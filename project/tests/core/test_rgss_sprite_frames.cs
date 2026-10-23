using System;
using System.Reflection;
using UniversalRPG.Plugins;
using UniversalRPG.Rgss;
using UniversalRPG.Rm2k.Rendering;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

public partial class TestRgssSpriteFrames : TestBase
{
    public void Test_OrdinaryUpdatesComposeMovementVisibilityAndDisposal()
    {
        foreach (var type in new[] { typeof(XpEngineRuntime), typeof(VxEngineRuntime), typeof(VxAceEngineRuntime) })
        {
            var graphics = new RgssGrafikHost();
            var interpreter = new RubyInterpreter(graphics);
            using var runtime = CreateRuntime(type, graphics, interpreter);
            Run(interpreter, """
                class PixelScene
                  def main
                    Graphics.resize_screen(4, 2)
                    bitmap = Bitmap.new(2, 1)
                    bitmap.fill_rect(0, 0, 2, 1, Color.new(255, 0, 0, 255))
                    sprite = Sprite.new
                    sprite.bitmap = bitmap
                    sprite.x = 1
                    Graphics.update
                    sprite.x = 2
                    Graphics.update
                    sprite.visible = false
                    Graphics.update
                    sprite.visible = true
                    sprite.dispose
                    Graphics.update
                    bitmap.dispose
                    $scene = nil
                  end
                end
                $scene = PixelScene.new
                """);
            AssertTrue(runtime.Start().Success);
            var update = runtime.Update(1.0 / 60);
            AssertTrue(update.Success, update.Error?.Message ?? type.Name + " update");
            var first = Frame(runtime);
            var firstSnapshot = (runtime as IRgssFrameSource)?.RenderedFrame;
            AssertTrue(first != null, type.Name + " must publish a composed frame");
            if (first == null) continue;
            AssertEq(first.Width, 4);
            AssertEq(first.Height, 2);
            AssertEq(Red(first, 1, 0), 255);
            AssertEq(Red(first, 0, 0), 0);
            AssertTrue(runtime.Update(1.0 / 60).Success);
            var second = Frame(runtime)!;
            AssertEq(Red(second, 1, 0), 0);
            AssertEq(Red(second, 3, 0), 255);
            AssertEq(Red(first, 1, 0), 255, "An earlier published frame must remain stable.");
            AssertEq(Red(firstSnapshot!.CopyPixels(), 1, 0), 255);
            AssertTrue(runtime.Update(1.0 / 60).Success);
            AssertEq(Red(Frame(runtime)!, 2, 0), 0);
            AssertTrue(runtime.Update(1.0 / 60).Success);
            AssertEq(Red(Frame(runtime)!, 2, 0), 0);
            AssertTrue(runtime.Update(1.0 / 60).Success);
            AssertEq(runtime.State, PluginRuntimeState.Stopped);
        }
    }

    private static int Red(Rm2kPixelBuffer frame, int x, int y) => frame.Pixels[(y * frame.Width + x) * 4];
    private static Rm2kPixelBuffer? Frame(IEngineRuntime runtime) =>
        (runtime as IRgssFrameSource)?.RenderedFrame?.CopyPixels();

    private static IEngineRuntime CreateRuntime(Type type, RgssGrafikHost graphics, RubyInterpreter interpreter)
    {
        var runtime = (IEngineRuntime)Activator.CreateInstance(type, "test", new PluginGameInfo())!;
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        type.GetField("_interpreter", flags)!.SetValue(runtime, interpreter);
        type.GetField("_grafik", flags)!.SetValue(runtime, graphics);
        type.GetField("_scriptsRun", flags)!.SetValue(runtime, true);
        type.GetMethod("DefineBuiltinClasses", flags)!.Invoke(runtime, null);
        type.GetProperty("State")!.SetValue(runtime, PluginRuntimeState.Initialized);
        return runtime;
    }

    private static void Run(RubyInterpreter interpreter, string source) =>
        interpreter.RunProgram(new RubyParser(new RubyLexer(source).Tokenize()).ParseProgram());
}
