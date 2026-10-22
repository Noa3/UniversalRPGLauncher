using System;
using System.Reflection;
using UniversalRPG.Plugins;
using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>Pixel operations must reach real storage through each production bootstrap.</summary>
public partial class TestRgssBitmapPixels : TestBase
{
    public void Test_ImageCacheReusesPixelsAndReleasesThemOnClear()
    {
        foreach (var type in new[] { typeof(XpEngineRuntime), typeof(VxEngineRuntime), typeof(VxAceEngineRuntime) })
        {
            var graphics = new RgssGrafikHost();
            var interpreter = new RubyInterpreter(graphics);
            using var runtime = (IEngineRuntime)Activator.CreateInstance(type, "test", new PluginGameInfo())!;
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            type.GetField("_interpreter", flags)!.SetValue(runtime, interpreter);
            type.GetField("_grafik", flags)!.SetValue(runtime, graphics);
            type.GetMethod("DefineBuiltinClasses", flags)!.Invoke(runtime, null);
            // Synthetic metadata isolates cache ownership from unfinished file decoding.
            interpreter.RunProgram(new RubyParser(new RubyLexer("""
                module RPG::Cache
                  def self.BildGroesse(art, name); [2, 2]; end
                end
                $first = RPG::Cache.picture("shared")
                $first.set_pixel(0, 0, Color.new(17, 23, 29, 255))
                200.times do
                  $again = RPG::Cache.picture("shared")
                end
                $red = $again.get_pixel(0, 0).red
                $other = RPG::Cache.title("shared")
                """).Tokenize()).ParseProgram());
            AssertTrue(ReferenceEquals(interpreter.Global("$first"), interpreter.Global("$again")),
                type.Name + " must return the cached object");
            AssertEq(interpreter.Global("$red").Integer, 17);
            AssertEq(graphics.Bitmaps.Count, 2, type.Name + " repeated requests must not grow storage");
            AssertEq(graphics.Bitmaps.AllocatedBytes, 32L);
            interpreter.RunProgram(new RubyParser(new RubyLexer("""
                $first.dispose
                $replacement = RPG::Cache.picture("shared")
                $replaced = !$replacement.disposed?
                RPG::Cache.clear
                $cleared = $replacement.disposed? && $other.disposed?
                RPG::Cache.clear
                """).Tokenize()).ParseProgram());
            AssertTrue(interpreter.Global("$replaced").Boolean);
            AssertTrue(interpreter.Global("$cleared").Boolean);
            AssertEq(graphics.Bitmaps.Count, 0);
            AssertEq(graphics.Bitmaps.AllocatedBytes, 0L);
            AssertEq(interpreter.Diagnostics.Count, 0, string.Join("; ", interpreter.Diagnostics));
        }
    }

    public void Test_DupAndCloneOwnIndependentPixelsAfterSourceDisposal()
    {
        foreach (var type in new[] { typeof(XpEngineRuntime), typeof(VxEngineRuntime), typeof(VxAceEngineRuntime) })
        {
            var graphics = new RgssGrafikHost();
            var interpreter = new RubyInterpreter(graphics);
            using var runtime = (IEngineRuntime)Activator.CreateInstance(type, "test", new PluginGameInfo())!;
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            type.GetField("_interpreter", flags)!.SetValue(runtime, interpreter);
            type.GetField("_grafik", flags)!.SetValue(runtime, graphics);
            type.GetMethod("DefineBuiltinClasses", flags)!.Invoke(runtime, null);
            interpreter.RunProgram(new RubyParser(new RubyLexer("""
                original = Bitmap.new(2, 2)
                original.set_pixel(1, 1, Color.new(17, 23, 29, 31))
                $copy = original.dup
                $clone = original.clone
                $copied_red = $copy.get_pixel(1, 1).red
                $copy.set_pixel(1, 1, Color.new(41, 43, 47, 53))
                $original_red = original.get_pixel(1, 1).red
                original.dispose
                $disposed_copy_refused = false
                begin
                  original.dup
                rescue RGSSError
                  $disposed_copy_refused = true
                end
                $copy_red = -1
                $clone_red = -1
                begin
                  $copy_red = $copy.get_pixel(1, 1).red
                  $clone_red = $clone.get_pixel(1, 1).red
                rescue RGSSError
                  $copy_failed = true
                end
                """).Tokenize()).ParseProgram());
            AssertEq(interpreter.Global("$copied_red").Integer, 17, type.Name + " copies existing pixels");
            AssertTrue(interpreter.Global("$disposed_copy_refused").Boolean);
            AssertEq(interpreter.Global("$original_red").Integer, 17, type.Name + " independent writes");
            AssertEq(interpreter.Global("$copy_red").Integer, 41, type.Name + " duplicate survives source disposal");
            AssertEq(interpreter.Global("$clone_red").Integer, 17, type.Name + " clone survives source disposal");
            AssertEq(graphics.Bitmaps.Count, 2);
            AssertEq(graphics.Bitmaps.AllocatedBytes, 32L);
            AssertEq(interpreter.Diagnostics.Count, 0, string.Join("; ", interpreter.Diagnostics));
        }
    }

    public void Test_CopyRejectsDisposedSourcesAndFullBudgetsWithoutLeaking()
    {
        var store = new RgssBitmapStore();
        var full = store.Create(RgssBitmapStore.MaximumDimension, RgssBitmapStore.MaximumDimension);
        store.SetPixel(full, 0, 0, 7, 11, 13, 17);
        var errorClass = "";
        try { store.Copy(full); }
        catch (RubyRuntimeException error) { errorClass = error.Class; }
        AssertEq(errorClass, "RuntimeError");
        AssertEq(store.Count, 1);
        AssertEq(store.AllocatedBytes, RgssBitmapStore.MaximumBytes);
        AssertEq(store.GetPixel(full, 0, 0).Items[0].Integer, 7);
        store.ReleaseAll();
        errorClass = "";
        try { store.Copy(full); }
        catch (RubyRuntimeException error) { errorClass = error.Class; }
        AssertEq(errorClass, "RGSSError");
        AssertEq(store.Count, 0);
        AssertEq(store.AllocatedBytes, 0L);
        var next = store.Create(1, 1);
        AssertTrue(next != full, "ReleaseAll must not recycle an old handle.");
        store.ReleaseAll();
    }

    public void Test_RuntimeDisposalReleasesUndisposedBitmaps()
    {
        foreach (var type in new[] { typeof(XpEngineRuntime), typeof(VxEngineRuntime), typeof(VxAceEngineRuntime) })
        {
            var graphics = new RgssGrafikHost();
            using var runtime = (IEngineRuntime)Activator.CreateInstance(type, "test", new PluginGameInfo())!;
            type.GetField("_grafik", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(runtime, graphics);
            graphics.Bitmaps.Create(3, 2);
            AssertEq(graphics.Bitmaps.Count, 1);
            runtime.Dispose();
            AssertEq(graphics.Bitmaps.Count, 0, type.Name + " must release abandoned game bitmaps");
            AssertEq(graphics.Bitmaps.AllocatedBytes, 0L);
        }
    }

    public void Test_FractionalPixelCoordinatesTruncateAndRejectNonFiniteValues()
    {
        foreach (var type in new[] { typeof(XpEngineRuntime), typeof(VxEngineRuntime), typeof(VxAceEngineRuntime) })
        {
            var graphics = new RgssGrafikHost();
            var interpreter = new RubyInterpreter(graphics);
            using var runtime = (IEngineRuntime)Activator.CreateInstance(type, "test", new PluginGameInfo())!;
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            type.GetField("_interpreter", flags)!.SetValue(runtime, interpreter);
            type.GetField("_grafik", flags)!.SetValue(runtime, graphics);
            type.GetMethod("DefineBuiltinClasses", flags)!.Invoke(runtime, null);
            interpreter.RunProgram(new RubyParser(new RubyLexer("""
                b = Bitmap.new(2, 2)
                b.fill_rect(Rect.new(-0.9, 0.9, 1.9, 1.1), Color.new(19, 23, 29, 255))
                $filled = b.get_pixel(0.7, 0.7).red
                $outside = b.get_pixel(1.2, 0.7).alpha
                b.set_pixel(1.9, 1.1, Color.new(31, 37, 41, 255))
                $written = b.get_pixel(1.5, 1.5).red
                """).Tokenize()).ParseProgram());
            AssertEq(interpreter.Global("$filled").Integer, 19, type.Name);
            AssertEq(interpreter.Global("$outside").Integer, 0);
            AssertEq(interpreter.Global("$written").Integer, 31);
            AssertEq(interpreter.Diagnostics.Count, 0, string.Join("; ", interpreter.Diagnostics));
            var id = graphics.Bitmaps.Create(1, 1);
            foreach (var coordinate in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity, double.MaxValue })
            {
                var refused = false;
                try
                {
                    graphics.CallMethod(RubyValue.Nil, "rgss_bitmap", new[]
                    {
                        RubyValue.OfSymbol("get_pixel"), RubyValue.OfInteger(id),
                        RubyValue.OfReal(coordinate), RubyValue.OfInteger(0),
                    });
                }
                catch (RubyRuntimeException) { refused = true; }
                AssertTrue(refused, "Non-finite/out-of-range coordinates must not reach integer conversion.");
            }
        }
    }

    public void Test_AllocationLimitsRejectBeforeAddingAResourceAndDisposalInvalidatesHandles()
    {
        var store = new RgssBitmapStore();
        foreach (var width in new[] { 0, -1, int.MaxValue, RgssBitmapStore.MaximumDimension + 1 })
        {
            var refused = false;
            try { store.Create(width, 2); }
            catch (RubyRuntimeException) { refused = true; }
            AssertTrue(refused, "Invalid dimensions must fail before allocation.");
        }
        AssertEq(store.Count, 0);
        AssertEq(store.AllocatedBytes, 0L);
        var full = store.Create(RgssBitmapStore.MaximumDimension, RgssBitmapStore.MaximumDimension);
        AssertEq(store.AllocatedBytes, RgssBitmapStore.MaximumBytes);
        var overBudget = false;
        try { store.Create(1, 1); }
        catch (RubyRuntimeException) { overBudget = true; }
        AssertTrue(overBudget);
        AssertEq(store.Count, 1);
        store.DisposeBitmap(full);
        AssertEq(store.AllocatedBytes, 0L);
        var next = store.Create(1, 1);
        AssertTrue(next != full, "Handles must not be recycled.");
        var disposed = false;
        try { store.GetPixel(full, 0, 0); }
        catch (RubyRuntimeException) { disposed = true; }
        AssertTrue(disposed);
        store.ReleaseAll();
        AssertEq(store.Count, 0);
        AssertEq(store.AllocatedBytes, 0L);
    }

    public void Test_FillRectOverloadsNormalizeClipAndReplaceRgba()
    {
        foreach (var type in new[] { typeof(XpEngineRuntime), typeof(VxEngineRuntime), typeof(VxAceEngineRuntime) })
        {
            var graphics = new RgssGrafikHost();
            var interpreter = new RubyInterpreter(graphics);
            using var runtime = (IEngineRuntime)Activator.CreateInstance(type, "test", new PluginGameInfo())!;
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            type.GetField("_interpreter", flags)!.SetValue(runtime, interpreter);
            type.GetField("_grafik", flags)!.SetValue(runtime, graphics);
            type.GetMethod("DefineBuiltinClasses", flags)!.Invoke(runtime, null);
            interpreter.RunProgram(new RubyParser(new RubyLexer("""
                b = Bitmap.new(4, 3)
                $fill_self = b.fill_rect(0, 0, 4, 3, Color.new(10, 20, 30, 255)) == b
                b.fill_rect(Rect.new(3, 2, -2, -1), Color.new(100, 110, 120, 64))
                $inside = b.get_pixel(1, 1).red
                $alpha = b.get_pixel(2, 1).alpha
                $left = b.get_pixel(0, 1).red
                $right = b.get_pixel(3, 1).red
                b.fill_rect(-1, -1, 2, 2, Color.new(9, 8, 7, 6))
                $clipped = b.get_pixel(0, 0).red
                $outside = b.get_pixel(1, 0).red
                b.dispose
                """).Tokenize()).ParseProgram());
            AssertTrue(interpreter.Global("$fill_self").Boolean, type.Name + " fill returns self");
            AssertEq(interpreter.Global("$inside").Integer, 100, type.Name + " normalized rectangle");
            AssertEq(interpreter.Global("$alpha").Integer, 64, type.Name + " replaces, not blends");
            AssertEq(interpreter.Global("$left").Integer, 10, type.Name + " untouched left");
            AssertEq(interpreter.Global("$right").Integer, 10, type.Name + " untouched right");
            AssertEq(interpreter.Global("$clipped").Integer, 9, type.Name + " clips to bitmap");
            AssertEq(interpreter.Global("$outside").Integer, 10, type.Name + " exclusive right edge");
            AssertEq(interpreter.Diagnostics.Count, 0, string.Join("; ", interpreter.Diagnostics));
        }
    }

    public void Test_ClearRectIsAvailableOnlyInVxGenerationsAndClearsBothOverloads()
    {
        foreach (var type in new[] { typeof(XpEngineRuntime), typeof(VxEngineRuntime), typeof(VxAceEngineRuntime) })
        {
            var graphics = new RgssGrafikHost();
            var interpreter = new RubyInterpreter(graphics);
            using var runtime = (IEngineRuntime)Activator.CreateInstance(type, "test", new PluginGameInfo())!;
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            type.GetField("_interpreter", flags)!.SetValue(runtime, interpreter);
            type.GetField("_grafik", flags)!.SetValue(runtime, graphics);
            type.GetMethod("DefineBuiltinClasses", flags)!.Invoke(runtime, null);
            var source = """
                $bitmap = Bitmap.new(3, 2)
                $available = $bitmap.respond_to?(:clear_rect)
                """;
            interpreter.RunProgram(new RubyParser(new RubyLexer(source).Tokenize()).ParseProgram());
            AssertEq(interpreter.Global("$available").Boolean, type != typeof(XpEngineRuntime), type.Name);
            if (type == typeof(XpEngineRuntime)) continue;
            interpreter.RunProgram(new RubyParser(new RubyLexer("""
                $bitmap.fill_rect(0, 0, 3, 2, Color.new(12, 34, 56, 78))
                $clear_rect_self = $bitmap.clear_rect(0, 0, 1, 1) == $bitmap
                $bitmap.clear_rect(Rect.new(2, 1, 1, 1))
                $first = $bitmap.get_pixel(0, 0).alpha
                $last = $bitmap.get_pixel(2, 1).alpha
                $middle = $bitmap.get_pixel(1, 1).alpha
                $zero_red = $bitmap.get_pixel(2, 1).red
                """).Tokenize()).ParseProgram());
            AssertTrue(interpreter.Global("$clear_rect_self").Boolean);
            AssertEq(interpreter.Global("$first").Integer, 0);
            AssertEq(interpreter.Global("$last").Integer, 0);
            AssertEq(interpreter.Global("$middle").Integer, 78);
            AssertEq(interpreter.Global("$zero_red").Integer, 0);
            AssertEq(interpreter.Diagnostics.Count, 0, string.Join("; ", interpreter.Diagnostics));
        }
    }

    public void Test_ExtremeCoordinatesAndSurfaceCountRemainBounded()
    {
        var store = new RgssBitmapStore();
        var id = store.Create(2, 2);
        store.FillRect(id, int.MaxValue, 0, int.MinValue, 2, 9, 8, 7, 6);
        AssertEq(store.GetPixel(id, 1, 1).Items[0].Integer, 9);
        store.FillRect(id, int.MinValue, int.MinValue, int.MaxValue, int.MaxValue, 1, 1, 1, 1);
        AssertEq(store.GetPixel(id, 1, 1).Items[0].Integer, 9, "An entirely outside rectangle must not wrap into the buffer.");
        store.SetPixel(id, int.MaxValue, int.MinValue, 100, 100, 100, 100);
        AssertEq(store.GetPixel(id, 1, 1).Items[0].Integer, 9);
        AssertEq(store.GetPixel(id, int.MinValue, int.MaxValue).Items[3].Integer, 0);
        var snapshot = store.GetPixel(id, 1, 1);
        snapshot.Items[0] = RubyValue.OfInteger(200);
        AssertEq(store.GetPixel(id, 1, 1).Items[0].Integer, 9, "A returned color must not alias bitmap memory.");
        store.ReleaseAll();
        for (var i = 0; i < RgssBitmapStore.MaximumSurfaces; i++) store.Create(1, 1);
        var refused = false;
        try { store.Create(1, 1); }
        catch (RubyRuntimeException) { refused = true; }
        AssertTrue(refused, "Small surfaces must also be bounded by object count.");
        AssertEq(store.Count, RgssBitmapStore.MaximumSurfaces);
        store.ReleaseAll();
    }

    public void Test_SetPixelAndClearChangeRealBitmapData()
    {
        foreach (var type in new[] { typeof(XpEngineRuntime), typeof(VxEngineRuntime), typeof(VxAceEngineRuntime) })
        {
            var graphics = new RgssGrafikHost();
            var interpreter = new RubyInterpreter(graphics);
            using var runtime = (IEngineRuntime)Activator.CreateInstance(type, "test", new PluginGameInfo())!;
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            type.GetField("_interpreter", flags)!.SetValue(runtime, interpreter);
            type.GetField("_grafik", flags)!.SetValue(runtime, graphics);
            type.GetMethod("DefineBuiltinClasses", flags)!.Invoke(runtime, null);
            interpreter.RunProgram(new RubyParser(new RubyLexer("""
                first = Bitmap.new(3, 2)
                second = Bitmap.new(3, 2)
                $set_self = first.set_pixel(2, 1, Color.new(12, 34, 56, 78)) == first
                pixel = first.get_pixel(2, 1)
                $red = pixel.red
                $green = pixel.green
                $blue = pixel.blue
                $alpha = pixel.alpha
                $other_alpha = second.get_pixel(2, 1).alpha
                $clear_self = first.clear == first
                $cleared = first.get_pixel(2, 1).alpha
                first.dispose
                first.dispose
                $disposed = first.disposed?
                second.dispose
                """).Tokenize()).ParseProgram());
            AssertEq(interpreter.Global("$red").Integer, 12, type.Name + " red: " + interpreter.Global("$red").Integer);
            AssertEq(interpreter.Global("$green").Integer, 34, type.Name + " green: " + interpreter.Global("$green").Integer);
            AssertEq(interpreter.Global("$blue").Integer, 56, type.Name + " blue: " + interpreter.Global("$blue").Integer);
            AssertEq(interpreter.Global("$alpha").Integer, 78, type.Name + " alpha: " + interpreter.Global("$alpha").Integer);
            AssertEq(interpreter.Global("$other_alpha").Integer, 0, type.Name + " owns independent pixels");
            AssertEq(interpreter.Global("$cleared").Integer, 0, type.Name + " cleared: " + interpreter.Global("$cleared").Integer);
            AssertTrue(interpreter.Global("$disposed").Boolean, type.Name + " disposed flag");
            AssertTrue(interpreter.Global("$set_self").Boolean, type.Name + " set_pixel returns self");
            AssertTrue(interpreter.Global("$clear_self").Boolean, type.Name + " clear returns self");
            AssertEq(interpreter.Diagnostics.Count, 0, string.Join("; ", interpreter.Diagnostics));
            AssertEq(graphics.Bitmaps.Count, 0, "Disposal must release the host surface, not only set a Ruby flag.");
            AssertEq(graphics.Bitmaps.AllocatedBytes, 0L);
        }
    }
}
