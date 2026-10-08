using System;
using System.IO;
using UniversalRPG.Plugins;
using UniversalRPG.Tests.Framework;
using UniversalRPG.Web;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And a fade and a flash reach the frame, and move while they do.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the screen's own effects were written, tested and called by
/// nobody.</strong> <c>MzScreen</c> has <c>TickTon</c>, <c>TickBildschirm</c>,
/// <c>TickBlitz</c>, <c>TickWackeln</c> and <c>TickAudio</c> -- every one of
/// them with the engine's arithmetic in its remarks -- and
/// <c>grep -rn "TickBildschirm" project/app/</c> found <em>nothing</em>, and
/// <c>grep -n "TickBildschirm" project/src/plugins/MzEngineRuntime.cs</c>
/// found nothing either. So a fade never ran, a flash never ran, and a tone
/// with a duration sat where it was instead of travelling.
/// </para>
/// <para>
/// <strong>And the arithmetic is the engine's own</strong>, measured in the
/// project's <c>rmmz_objects.js</c>:
/// </para>
/// <code>
/// Game_Screen.prototype.updateFadeOut = function() {
///     this._brightness = (this._brightness * (d - 1)) / d;
///     this._fadeOutDuration--;
/// };
/// Game_Screen.prototype.updateFadeIn = function() {
///     this._brightness = (this._brightness * (d - 1) + 255) / d;
///     this._fadeInDuration--;
/// };
/// Game_Screen.prototype.updateFlash = function() {
///     this._flashColor[3] *= (d - 1) / d;
///     this._flashDuration--;
/// };
/// </code>
/// <para>
/// <strong>so the screen fade is a <em>brightness</em> and not an overlay</strong>
/// -- 255 is normal and 0 is black -- <strong>and the flash is a colour whose
/// alpha decays from the one the game named.</strong>
/// </para>
/// <para>
/// <strong>And this test is not the model's own test.</strong>
/// <c>TestMzScreenFade</c> already proves the arithmetic inside
/// <c>MzScreen</c>, by calling its tick itself. What was missing is that
/// <em>nothing in the runtime ever called it</em> -- so this drives
/// <c>MzEngineRuntime.Tick()</c>, which is the frame a game gets.
/// </para>
/// </remarks>
public partial class TestMzScreenFadeAndFlash : TestBase
{
    private const string Projekt = "E:/RPGMakerGames/CamelliaCoronation-Win";

    private static bool Vorhanden() =>
        File.Exists(Projekt + "/data/System.json")
        && File.Exists(Projekt + "/data/Map002.json");

    private static MzEngineRuntime Start()
    {
        var host = new EnginePluginHost(
            BuiltInEnginePluginCatalog.CreateRuntimeRegistry());
        var started = host.Start(new PluginGameInfo
        {
            GameDirectory = Projekt,
            EngineId = EnginePluginIds.RpgMakerMz,
            Generation = "mz",
            DetectorScore = 850,
        });
        if (!started.Success)
        {
            host.Dispose();
            throw new InvalidOperationException(
                $"the MZ game did not start: {started.Error?.Message}");
        }
        return (MzEngineRuntime)host.Runtime!;
    }

    /// <summary>
    /// And a fade out darkens the frame, and a fade in brings it back.
    /// </summary>
    public void Test_AbdunkelnUndAufhellenErreichenDenRahmen()
    {
        if (!Vorhanden())
        {
            return;
        }

        var runtime = Start();
        runtime.Repaint();
        var hell = runtime.PaintedMap!;
        AssertEq(runtime.Facts.Screen.Brightness, 255,
            "a screen starts bright, which is the engine's own 255");
        AssertFalse(runtime.BrightnessDrawn,
            "and a bright frame is not walked over");

        // **And the fade moves only when a frame runs**, which is what was
        // missing: `startFadeOut` sets the countdown and not the brightness.
        runtime.Facts.Screen.StarteAbdunkeln(24);
        var vorher = runtime.Facts.Screen.Brightness;
        runtime.Tick();
        var nachEinem = runtime.Facts.Screen.Brightness;
        Console.WriteLine($"MZ fade: brightness {vorher} -> {nachEinem} "
            + $"after one frame, duration left "
            + $"{runtime.Facts.Screen.FadeOutDuration}");
        AssertTrue(nachEinem < vorher,
            "one frame darkens the screen, which needs the tick");
        AssertEq(runtime.Facts.Screen.FadeOutDuration, 23,
            "and the countdown moves by one");

        for (var i = 0; i < 30; i++)
        {
            runtime.Tick();
        }
        var dunkel = runtime.Facts.Screen.Brightness;
        Console.WriteLine($"MZ fade: after the fade the brightness is {dunkel}");
        AssertTrue(dunkel < 24,
            "and a fade out of 24 frames leaves the screen nearly black");

        runtime.Repaint();
        Console.WriteLine($"MZ fade: drawn={runtime.BrightnessDrawn}");
        AssertTrue(runtime.BrightnessDrawn, "and the frame carries it");
        var mit = runtime.PaintedMap!;
        var geaendert = 0;
        var geprueft = -1;
        for (var i = 0; i + 3 < hell.Pixels.Length; i += 4)
        {
            if (hell.Pixels[i] != mit.Pixels[i])
            {
                geaendert++;
            }
            if (geprueft < 0 && hell.Pixels[i] > 40 && mit.Pixels[i] > 0)
            {
                geprueft = i;
            }
        }
        Console.WriteLine($"MZ fade: {geaendert} pixels are darker than the map");
        AssertTrue(geaendert > 1000, "and the map under it is darker for it");

        // **And the brightness is a multiplier and nothing else**, which is
        // what the engine's `filter.setBrightness` does.
        if (geprueft >= 0)
        {
            var erwartet = hell.Pixels[geprueft] * dunkel / 255;
            Console.WriteLine($"MZ fade: a pixel of {hell.Pixels[geprueft]} became "
                + $"{mit.Pixels[geprueft]}, and the brightness says {erwartet}");
            AssertTrue(Math.Abs(mit.Pixels[geprueft] - erwartet) <= 1,
                "and every channel is the map's own times the brightness");
        }

        // **And a fade in brings it back.**
        runtime.Facts.Screen.StarteAufhellen(24);
        for (var i = 0; i < 30; i++)
        {
            runtime.Tick();
        }
        var wieder = runtime.Facts.Screen.Brightness;
        Console.WriteLine($"MZ fade: after the fade in the brightness is {wieder}");
        AssertTrue(wieder > dunkel, "and a fade in lightens the screen again");
    }

    /// <summary>
    /// And a flash is a colour over the frame, and it decays.
    /// </summary>
    public void Test_EinBlitzLiegtUeberDemBild()
    {
        if (!Vorhanden())
        {
            return;
        }

        var runtime = Start();
        runtime.Repaint();
        var ohne = runtime.PaintedMap!;
        AssertFalse(runtime.FlashDrawn, "a frame with no flash is not flashed");

        runtime.Facts.Screen.StarteBlitz(new[] { 255, 255, 255, 255 }, 24);
        runtime.Repaint();
        Console.WriteLine($"MZ flash: alpha={runtime.Facts.Screen.Flash[3]} "
            + $"drawn={runtime.FlashDrawn}");
        AssertTrue(runtime.FlashDrawn, "and a white flash at full alpha is drawn");

        var weiss = runtime.PaintedMap!;
        var weisser = 0;
        for (var i = 0; i + 3 < ohne.Pixels.Length; i += 4)
        {
            if (weiss.Pixels[i] > ohne.Pixels[i])
            {
                weisser++;
            }
        }
        Console.WriteLine($"MZ flash: {weisser} pixels are lighter than the map");
        AssertTrue(weisser > 1000, "and it lightens the whole frame");

        // **And the alpha decays by the engine's own rule**, `(d-1)/d`.
        var vorher = runtime.Facts.Screen.Flash[3];
        runtime.Tick();
        var nachher = runtime.Facts.Screen.Flash[3];
        Console.WriteLine($"MZ flash: alpha {vorher} -> {nachher} after one frame, "
            + $"duration left {runtime.Facts.Screen.FlashDuration}");
        AssertTrue(nachher < vorher, "and it fades from frame to frame");
        AssertEq(runtime.Facts.Screen.FlashDuration, 23,
            "with the countdown moving by one");

        // **And a red flash is red**, which is what the colour is for.
        runtime.Facts.Screen.StarteBlitz(new[] { 255, 0, 0, 255 }, 0);
        runtime.Repaint();
        var rot = runtime.PaintedMap!;
        var rotGewachsen = 0;
        var blauGewachsen = 0;
        for (var i = 0; i + 3 < ohne.Pixels.Length; i += 4)
        {
            if (rot.Pixels[i] > ohne.Pixels[i])
            {
                rotGewachsen++;
            }
            if (rot.Pixels[i + 2] > ohne.Pixels[i + 2])
            {
                blauGewachsen++;
            }
        }
        Console.WriteLine($"MZ flash: red grew on {rotGewachsen} pixels, "
            + $"blue on {blauGewachsen}");
        AssertTrue(rotGewachsen > 1000, "a red flash raises the red channel");
        AssertEq(blauGewachsen, 0, "and leaves the others alone");
    }

    /// <summary>
    /// And a tone with a duration travels instead of jumping.
    /// </summary>
    /// <remarks>
    /// <strong>And this was the same defect seen from the other side.</strong>
    /// <c>StarteTon</c> with a duration sets the target and keeps the tone
    /// where it is; without <c>TickTon</c> it stayed there for ever -- so a
    /// tinted screen never arrived, while the model's own test proved the
    /// arithmetic that would have got it there.
    /// </remarks>
    public void Test_EinTonMitDauerWandert()
    {
        if (!Vorhanden())
        {
            return;
        }

        var runtime = Start();
        runtime.Facts.Screen.StarteTon(new[] { 120, 0, 0, 0 }, 12);
        AssertEq(runtime.Facts.Screen.Tone[0], 0,
            "a tone with a duration starts where the screen already was");
        AssertTrue(runtime.Facts.Screen.ToneIsMoving, "and it is on its way");

        for (var i = 0; i < 4; i++)
        {
            runtime.Tick();
        }
        var nachVier = runtime.Facts.Screen.Tone[0];
        Console.WriteLine($"MZ tone: after four of twelve frames the red is "
            + $"{nachVier} of 120");
        AssertTrue(nachVier > 0, "so it travels, which needs the tick");
        AssertTrue(nachVier < 120, "and it has not arrived yet");

        for (var i = 0; i < 12; i++)
        {
            runtime.Tick();
        }
        Console.WriteLine($"MZ tone: at the end the red is "
            + $"{runtime.Facts.Screen.Tone[0]}, moving="
            + $"{runtime.Facts.Screen.ToneIsMoving}");
        AssertEq(runtime.Facts.Screen.Tone[0], 120, "and it arrives exactly");
        AssertFalse(runtime.Facts.Screen.ToneIsMoving, "and stops there");
    }
}
