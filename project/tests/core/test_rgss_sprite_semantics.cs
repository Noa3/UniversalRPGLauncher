using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;
using UniversalRPG.Plugins;
using UniversalRPG.Rgss;

namespace UniversalRPG.Tests.Core;

public partial class TestRgssSpriteFrames
{
    private void ForEachGeneration(Action<Type, RgssGrafikHost, RubyInterpreter, IEngineRuntime> test)
    {
        foreach (var type in new[] { typeof(XpEngineRuntime), typeof(VxEngineRuntime), typeof(VxAceEngineRuntime) })
        {
            var graphics = new RgssGrafikHost();
            var interpreter = new RubyInterpreter(graphics);
            using var runtime = CreateRuntime(type, graphics, interpreter);
            test(type, graphics, interpreter, runtime);
        }
    }

    public void Test_SortingUsesCreationOrderInXpAndRawYInVx()
    {
        ForEachGeneration((type, graphics, interpreter, runtime) =>
        {
            Run(interpreter, """
                Graphics.resize_screen(2, 2)
                red = Bitmap.new(1, 1)
                red.set_pixel(0, 0, Color.new(255, 0, 0, 255))
                blue = Bitmap.new(1, 1)
                blue.set_pixel(0, 0, Color.new(0, 0, 255, 255))
                $first = Sprite.new
                $first.bitmap = red
                $first.y = 1
                $first.oy = 1
                $second = Sprite.new
                $second.bitmap = blue
                Graphics.update
                """);
            var pixels = graphics.RenderedFrame!.CopyPixels();
            AssertEq(pixels.Pixels[0], type == typeof(XpEngineRuntime) ? 0 : 255, type.Name);
            AssertEq(pixels.Pixels[2], type == typeof(XpEngineRuntime) ? 255 : 0, type.Name);
            Run(interpreter, "$first.z = -1; Graphics.update");
            pixels = graphics.RenderedFrame!.CopyPixels();
            AssertEq(pixels.Pixels[2], 255, "z takes priority over y and creation order.");
            Run(interpreter, "$first.z = 0; $first.y = 0; $first.oy = 0; Graphics.update");
            AssertEq(graphics.RenderedFrame!.CopyPixels().Pixels[2], 255, "Equal z/y uses creation order.");
        });
    }

    public void Test_NormalBlendingUsesSourceAlphaAndOpacity()
    {
        ForEachGeneration((type, graphics, interpreter, runtime) =>
        {
            Run(interpreter, """
                Graphics.resize_screen(1, 1)
                red = Bitmap.new(1, 1)
                red.set_pixel(0, 0, Color.new(255, 0, 0, 255))
                $base = Sprite.new
                $base.bitmap = red
                $blue = Bitmap.new(1, 1)
                $blue.set_pixel(0, 0, Color.new(0, 0, 255, 255))
                $overlay = Sprite.new
                $overlay.bitmap = $blue
                $overlay.opacity = 128
                Graphics.update
                """);
            var pixels = graphics.RenderedFrame!.CopyPixels().Pixels;
            AssertEq(pixels[0], 127);
            AssertEq(pixels[2], 128);
            AssertEq(pixels[3], 255);
            Run(interpreter, "$blue.set_pixel(0, 0, Color.new(0, 0, 255, 128)); Graphics.update");
            pixels = graphics.RenderedFrame!.CopyPixels().Pixels;
            AssertEq(pixels[0], 191);
            AssertEq(pixels[2], 64);
            Run(interpreter, "$overlay.opacity = 0; Graphics.update");
            AssertEq(graphics.RenderedFrame!.CopyPixels().Pixels[0], 255);
        });
    }

    public void Test_BitmapAssignmentPreservesRectIdentityAndResetsOnlyForANewLiveBitmap()
    {
        ForEachGeneration((type, graphics, interpreter, runtime) =>
        {
            Run(interpreter, """
                $bitmap = Bitmap.new(2, 1)
                $sprite = Sprite.new
                $rect = $sprite.src_rect
                $sprite.bitmap = $bitmap
                $initial_width = $rect.width
                $rect.set(1, 0, 1, 1)
                $sprite.bitmap = $bitmap
                $same_width = $rect.width
                $sprite.bitmap = nil
                $nil_width = $rect.width
                disposed = Bitmap.new(3, 1)
                disposed.dispose
                $sprite.bitmap = disposed
                $disposed_width = $rect.width
                $sprite.bitmap = Bitmap.new(4, 1)
                $new_width = $rect.width
                $current_rect = $sprite.src_rect
                """);
            AssertEq(interpreter.Global("$initial_width").Integer, 2);
            AssertEq(interpreter.Global("$same_width").Integer, 1);
            AssertEq(interpreter.Global("$nil_width").Integer, 1);
            AssertEq(interpreter.Global("$disposed_width").Integer, 1);
            AssertEq(interpreter.Global("$new_width").Integer, 4);
            AssertTrue(ReferenceEquals(interpreter.Global("$rect"), interpreter.Global("$current_rect")));
        });
    }

    public void Test_RectMutationOriginClippingAndDisposedBitmapAffectPixels()
    {
        ForEachGeneration((type, graphics, interpreter, runtime) =>
        {
            Run(interpreter, """
                Graphics.resize_screen(3, 2)
                $bitmap = Bitmap.new(2, 1)
                $bitmap.set_pixel(0, 0, Color.new(255, 0, 0, 255))
                $bitmap.set_pixel(1, 0, Color.new(0, 255, 0, 255))
                $sprite = Sprite.new
                $sprite.bitmap = $bitmap
                $sprite.src_rect.set(1, 0, 1, 1)
                $sprite.x = 2
                $sprite.ox = 1
                Graphics.update
                """);
            var pixels = graphics.RenderedFrame!.CopyPixels().Pixels;
            AssertEq(pixels[5], 255, "src_rect mutation samples the green texel at x-ox.");
            AssertEq(pixels[0], 0);
            Run(interpreter, "$sprite.src_rect.set(0, 0, 2, 1); $sprite.x = 0; Graphics.update");
            AssertEq(graphics.RenderedFrame!.CopyPixels().Pixels[1], 255, "Negative destination clips safely.");
            Run(interpreter, "$bitmap.dispose; Graphics.update");
            AssertEq(graphics.RenderedFrame!.CopyPixels().Pixels[1], 0);
            Run(interpreter, "$sprite.dispose; $sprite.dispose; $disposed = $sprite.disposed?");
            AssertTrue(interpreter.Global("$disposed").Boolean);
            AssertEq(graphics.Sprites.Count, 0);
        });
    }

    public void Test_UnsupportedFractionalTransformIsNamedInsteadOfSilentlyIgnored()
    {
        ForEachGeneration((type, graphics, interpreter, runtime) =>
        {
            Run(interpreter, """
                Graphics.resize_screen(1, 1)
                bitmap = Bitmap.new(1, 1)
                bitmap.set_pixel(0, 0, Color.new(255, 0, 0, 255))
                $sprite = Sprite.new
                $sprite.bitmap = bitmap
                $sprite.zoom_x = 1.5
                Graphics.update
                """);
            AssertEq(graphics.RenderedFrame!.CopyPixels().Pixels[0], 0);
            AssertTrue(graphics.RenderedFrame.Warning.Contains("Unsupported"));
            Run(interpreter, "$sprite.zoom_x = 1; Graphics.update");
            AssertEq(graphics.RenderedFrame!.CopyPixels().Pixels[0], 255);
            AssertEq(graphics.RenderedFrame.Warning, string.Empty);
            Run(interpreter, "$sprite.color = Color.new(0, 0, 0, 0.5); Graphics.update");
            AssertEq(graphics.RenderedFrame!.CopyPixels().Pixels[0], 0);
            AssertTrue(graphics.RenderedFrame.Warning.Contains("Unsupported"));
        });
    }

    public void Test_OpacityClampsItsPublicValueAndCopyingCannotAliasARegisteredSprite()
    {
        ForEachGeneration((type, graphics, interpreter, runtime) =>
        {
            Run(interpreter, """
                sprite = Sprite.new
                sprite.opacity = 300
                $high = sprite.opacity
                sprite.opacity = -4
                $low = sprite.opacity
                $copy_refused = false
                begin
                  copy = sprite.dup
                rescue TypeError
                  $copy_refused = true
                end
                """);
            AssertEq(interpreter.Global("$high").Integer, 255);
            AssertEq(interpreter.Global("$low").Integer, 0);
            AssertTrue(interpreter.Global("$copy_refused").Boolean);
            AssertEq(graphics.Sprites.Count, 1);
        });
    }

    public void Test_SpriteAndCompositionBudgetsAreIndependentAndCancellationIsObserved()
    {
        var graphics = new RgssGrafikHost();
        var arguments = new[] { RubyValue.OfSymbol("create"), RubyValue.OfEmptyObject("Sprite") };
        for (var i = 0; i < RgssSpriteStore.MaximumSprites; i++) graphics.Sprites.Call(arguments, graphics.Bitmaps);
        var refused = false;
        try { graphics.Sprites.Call(arguments, graphics.Bitmaps); }
        catch (RubyRuntimeException) { refused = true; }
        AssertTrue(refused);
        AssertEq(graphics.Sprites.Count, RgssSpriteStore.MaximumSprites);
        graphics.Sprites.ReleaseAll();
        refused = false;
        try { graphics.Sprites.Compose(graphics.Bitmaps, 4096, 4096, CancellationToken.None); }
        catch (RubyRuntimeException) { refused = true; }
        AssertTrue(refused);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var cancelled = false;
        try { graphics.Sprites.Compose(graphics.Bitmaps, 1, 1, cancellation.Token); }
        catch (OperationCanceledException) { cancelled = true; }
        AssertTrue(cancelled);
        var bitmap = graphics.Bitmaps.Create(1024, 1024);
        for (var i = 0; i < 17; i++)
        {
            var sprite = RubyValue.OfEmptyObject("Sprite");
            sprite.Felder["@visible"] = RubyValue.OfBoolean(true);
            sprite.Felder["@opacity"] = RubyValue.OfInteger(255);
            var image = RubyValue.OfEmptyObject("Bitmap");
            image.Felder["@pixel_surface"] = RubyValue.OfInteger(bitmap);
            sprite.Felder["@bitmap"] = image;
            var rect = RubyValue.OfEmptyObject("Rect");
            rect.Felder["@width"] = RubyValue.OfInteger(1024);
            rect.Felder["@height"] = RubyValue.OfInteger(1024);
            sprite.Felder["@src_rect"] = rect;
            graphics.Sprites.Call(new[] { RubyValue.OfSymbol("create"), sprite }, graphics.Bitmaps);
        }
        refused = false;
        try { graphics.Sprites.Compose(graphics.Bitmaps, 1024, 1024, CancellationToken.None); }
        catch (RubyRuntimeException error) { refused = error.Message.Contains("work limit"); }
        AssertTrue(refused, "Sharing one bitmap must not bypass the per-frame work limit.");
        AssertEq(graphics.Bitmaps.Count, 1);
    }

    public void Test_StartupCancellationAndFrameBudgetDoNotExecuteFollowingScripts()
    {
        foreach (var cancel in new[] { false, true })
        ForEachGeneration((type, graphics, interpreter, runtime) =>
        {
            var scripts = new Dictionary<string, byte[]>
            {
                ["entry"] = Encoding.UTF8.GetBytes("loop do; Graphics.update; end"),
                ["after entry"] = Encoding.UTF8.GetBytes("$unexpected_after = true"),
            };
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var host = (RgssSkriptHost)Activator.CreateInstance(typeof(RgssSkriptHost), flags,
                null, new object[] { scripts }, null)!;
            type.GetField("_scriptHost", flags)!.SetValue(runtime, host);
            type.GetField("_bootstrapPending", flags)!.SetValue(runtime, true);
            if (!cancel) graphics.Budget = 1;
            AssertTrue(runtime.Start().Success);
            AssertTrue(runtime.Update(1.0 / 60).Success);
            AssertTrue(cancel ? runtime.Stop().Success : runtime.Update(1.0 / 60).Success);
            AssertEq(runtime.State, PluginRuntimeState.Stopped);
            AssertEq(interpreter.Global("$unexpected_after").Kind, RubyValueKind.Nil);
            var problems = (IReadOnlyList<string>)type.GetProperty("ScriptProblems")!.GetValue(runtime)!;
            AssertEq(problems.Count, 0, string.Join("; ", problems));
        });
    }

    public void Test_DisposalClearsAbandonedGraphicsButPreservesPublishedSnapshots()
    {
        ForEachGeneration((type, graphics, interpreter, runtime) =>
        {
            Run(interpreter, """
                class AbandonedScene
                  def main
                    Graphics.resize_screen(1, 1)
                    bitmap = Bitmap.new(1, 1)
                    bitmap.set_pixel(0, 0, Color.new(255, 0, 0, 255))
                    sprite = Sprite.new
                    sprite.bitmap = bitmap
                    loop do
                      Graphics.update
                    end
                  end
                end
                $scene = AbandonedScene.new
                """);
            AssertTrue(runtime.Start().Success);
            AssertTrue(runtime.Update(1.0 / 60).Success);
            var snapshot = ((IRgssFrameSource)runtime).RenderedFrame!;
            var consumerCopy = snapshot.CopyPixels();
            consumerCopy.Pixels[0] = 0;
            AssertEq(snapshot.CopyPixels().Pixels[0], 255);
            AssertEq(graphics.Sprites.Count, 1);
            AssertEq(graphics.Bitmaps.Count, 1);
            runtime.Dispose();
            AssertEq(graphics.Sprites.Count, 0);
            AssertEq(graphics.Bitmaps.Count, 0);
            AssertTrue(graphics.RenderedFrame == null);
            AssertTrue(((IRgssFrameSource)runtime).RenderedFrame == null);
            AssertEq(snapshot.CopyPixels().Pixels[0], 255);
        });
    }

    public void Test_ProjectEntryScriptResumesLocalsAndRunsFollowingScriptsOnlyAfterCompletion()
    {
        ForEachGeneration((type, graphics, interpreter, runtime) =>
        {
            var scripts = new Dictionary<string, byte[]>
            {
                ["arbitrary startup name"] = Encoding.UTF8.GetBytes("""
                    $boot_started = true
                    local = "retained"
                    Graphics.update
                    $retained = local
                    Graphics.update
                    """),
                ["after entry"] = Encoding.UTF8.GetBytes("$after_entry = true"),
            };
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var host = (RgssSkriptHost)Activator.CreateInstance(typeof(RgssSkriptHost), flags,
                null, new object[] { scripts }, null)!;
            type.GetField("_scriptHost", flags)!.SetValue(runtime, host);
            type.GetField("_bootstrapPending", flags)!.SetValue(runtime, true);
            AssertTrue(runtime.Start().Success);
            AssertEq(graphics.Frames, 0);
            AssertEq(interpreter.Global("$boot_started").Kind, RubyValueKind.Nil);
            AssertTrue(runtime.Update(1.0 / 60).Success);
            AssertTrue(interpreter.Global("$boot_started").Boolean);
            AssertEq(interpreter.Global("$after_entry").Kind, RubyValueKind.Nil);
            AssertTrue(runtime.Update(1.0 / 60).Success);
            AssertEq(Encoding.UTF8.GetString(interpreter.Global("$retained").Bytes), "retained");
            AssertEq(interpreter.Global("$after_entry").Kind, RubyValueKind.Nil);
            AssertTrue(runtime.Update(1.0 / 60).Success);
            AssertTrue(interpreter.Global("$after_entry").Boolean);
            AssertEq(runtime.State, PluginRuntimeState.Stopped);
            AssertFalse(runtime.Update(1.0 / 60).Success);
            AssertEq(graphics.Frames, 2);
        });
    }
}
