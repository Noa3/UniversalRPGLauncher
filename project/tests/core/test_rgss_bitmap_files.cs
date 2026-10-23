using System;
using System.IO;
using System.Reflection;
using Godot;
using UniversalRPG.Plugins;
using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

internal sealed class RgssImageFixture : IDisposable
{
    internal string Root { get; } = Path.Combine(System.Environment.GetEnvironmentVariable("TMPDIR")
        ?? ProjectSettings.GlobalizePath("user://"), "urpg-cache-image-" + Guid.NewGuid().ToString("N"));

    internal RgssImageFixture()
    {
        foreach (var folder in new[] { "Pictures", "Titles" })
        {
            var directory = Path.Combine(Root, "Graphics", folder);
            Directory.CreateDirectory(directory);
            using var image = Image.CreateFromData(2, 2, false, Image.Format.Rgba8, new byte[16]);
            if (image.SavePng(Path.Combine(directory, "shared.png")) != Error.Ok)
                throw new IOException("Unable to write cache image fixture.");
        }
    }
    public void Dispose() => Directory.Delete(Root, true);
}

public partial class TestRgssBitmapFiles : TestBase
{
    private string _root = "";
    private static readonly byte[] Pixels =
    {
        17, 23, 29, 255, 70, 80, 90, 128,
        101, 103, 107, 0, 151, 157, 163, 255,
    };

    public override void Setup()
    {
        _root = Path.Combine(System.Environment.GetEnvironmentVariable("TMPDIR")
            ?? ProjectSettings.GlobalizePath("user://"), "urpg-file-bitmap-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_root, "Graphics", "Pictures"));
        using var image = Image.CreateFromData(2, 2, false, Image.Format.Rgba8, Pixels);
        var result = image.SavePng(Path.Combine(_root, "Graphics", "Pictures", "known.png"));
        if (result != Error.Ok) throw new IOException("Could not create the PNG fixture: " + result);
    }

    public override void Teardown() => Directory.Delete(_root, true);

    public void Test_CacheLoadsFilePixelsAndARealRuntimePublishesThem()
    {
        foreach (var type in new[] { typeof(XpEngineRuntime), typeof(VxEngineRuntime), typeof(VxAceEngineRuntime) })
        {
            var graphics = new RgssGrafikHost { Wurzel = _root };
            var ruby = new RubyInterpreter(graphics);
            using var runtime = (IEngineRuntime)Activator.CreateInstance(type, "test", new PluginGameInfo())!;
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            type.GetField("_interpreter", flags)!.SetValue(runtime, ruby);
            type.GetField("_grafik", flags)!.SetValue(runtime, graphics);
            type.GetField("_scriptsRun", flags)!.SetValue(runtime, true);
            type.GetMethod("DefineBuiltinClasses", flags)!.Invoke(runtime, null);
            type.GetProperty("State")!.GetSetMethod(true)!.Invoke(runtime, new object[] { PluginRuntimeState.Initialized });
            ruby.RunProgram(new RubyParser(new RubyLexer("""
                class FileScene
                  def main
                    Graphics.resize_screen(2, 2)
                    $image = RPG::Cache.picture("known")
                    $again = RPG::Cache.picture("known")
                    sprite = Sprite.new
                    sprite.bitmap = $image
                    loop do
                      Graphics.update
                    end
                  end
                end
                $scene = FileScene.new
                """).Tokenize()).ParseProgram());
            AssertTrue(runtime.Start().Success);
            var update = runtime.Update(1.0 / 60);
            AssertTrue(update.Success, update.Error?.Message ?? "");
            var frame = ((IRgssFrameSource)runtime).RenderedFrame;
            AssertTrue(frame != null);
            if (frame != null)
            {
                var pixels = frame.CopyPixels().Pixels;
                AssertEq(pixels[0], (byte)17, type.Name + " must compose the file's red channel, not an empty bitmap.");
                AssertEq(pixels[1], (byte)23);
                AssertEq(pixels[2], (byte)29);
            }
            AssertTrue(runtime.Stop().Success);
            AssertTrue(ReferenceEquals(ruby.Global("$image"), ruby.Global("$again")));
            AssertEq(graphics.Bitmaps.Count, 1);
        }
    }

    public void Test_FileConstructorDecodesRealPixelsThroughEveryProductionBootstrap()
    {
        foreach (var type in new[] { typeof(XpEngineRuntime), typeof(VxEngineRuntime), typeof(VxAceEngineRuntime) })
        {
            var graphics = new RgssGrafikHost { Wurzel = _root };
            var ruby = new RubyInterpreter(graphics);
            using var runtime = (IEngineRuntime)Activator.CreateInstance(type, "test", new PluginGameInfo())!;
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            type.GetField("_interpreter", flags)!.SetValue(runtime, ruby);
            type.GetField("_grafik", flags)!.SetValue(runtime, graphics);
            type.GetMethod("DefineBuiltinClasses", flags)!.Invoke(runtime, null);
            ruby.RunProgram(new RubyParser(new RubyLexer("""
                $image = Bitmap.new("Graphics/Pictures/known")
                $width = $image.width
                $height = $image.height
                $pixel = $image.get_pixel(1, 0)
                $red = $pixel.red
                $green = $pixel.green
                $blue = $pixel.blue
                $alpha = $pixel.alpha
                """).Tokenize()).ParseProgram());
            AssertEq(ruby.Global("$width").Integer, 2, type.Name);
            AssertEq(ruby.Global("$height").Integer, 2, type.Name);
            AssertEq(ruby.Global("$red").Integer, 70, type.Name);
            AssertEq(ruby.Global("$green").Integer, 80, type.Name);
            AssertEq(ruby.Global("$blue").Integer, 90, type.Name);
            AssertEq(ruby.Global("$alpha").Integer, 128, "File alpha must survive decoding, not become opaque.");
            AssertEq(graphics.Bitmaps.Count, 1);
            AssertEq(graphics.Bitmaps.AllocatedBytes, (long)Pixels.Length);
            AssertEq(ruby.Diagnostics.Count, 0, string.Join("; ", ruby.Diagnostics));
            ruby.RunProgram(new RubyParser(new RubyLexer("$image.dispose").Tokenize()).ParseProgram());
            AssertEq(graphics.Bitmaps.Count, 0);
            AssertEq(graphics.Bitmaps.AllocatedBytes, 0L);
        }
    }
}
