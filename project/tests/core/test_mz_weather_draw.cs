using System;
using System.IO;
using UniversalRPG.Plugins;
using UniversalRPG.Rm2k.Rendering;
using UniversalRPG.Tests.Framework;
using UniversalRPG.Web;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And a game's weather reaches the frame.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And every number here is measured in the project's own
/// <c>rmmz_core.js</c>, in <c>Weather</c>:</strong>
/// </para>
/// <code>
/// this._rainBitmap = new Bitmap(1, 60);   this._rainBitmap.fillAll("white");
/// this._stormBitmap = new Bitmap(2, 100); this._stormBitmap.fillAll("white");
/// this._snowBitmap = new Bitmap(9, 9);
/// this._snowBitmap.drawCircle(4, 4, 4, "white");
/// this._dimmerSprite.setColor(80, 80, 80);
/// this._dimmerSprite.opacity = Math.floor(this.power * 6);
/// const maxSprites = Math.floor(this.power * 10);
/// sprite.rotation = Math.PI / 16;
/// sprite.ax -= 6 * Math.sin(sprite.rotation);
/// sprite.ay += 6 * Math.cos(sprite.rotation);
/// sprite.opacity -= 6;
/// if (sprite.opacity &lt; 40) { this._rebornSprite(sprite); }
/// </code>
/// <para>
/// <strong>so the weather is <c>power * 10</c> white sprites falling along
/// their own rotated axis, over a grey wash of <c>power * 6</c>
/// opacity</strong> -- and the runtime modelled the type, the power and the
/// frames without drawing a single drop.
/// </para>
/// </remarks>
public partial class TestMzWeather : TestBase
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

    private static int GeaendertePixel(
        Rm2kPixelBuffer pVorher, Rm2kPixelBuffer pNachher)
    {
        var zahl = 0;
        for (var i = 0; i + 3 < pVorher.Pixels.Length; i += 4)
        {
            if (pVorher.Pixels[i] != pNachher.Pixels[i]
                || pVorher.Pixels[i + 1] != pNachher.Pixels[i + 1]
                || pVorher.Pixels[i + 2] != pNachher.Pixels[i + 2])
            {
                zahl++;
            }
        }
        return zahl;
    }

    /// <summary>
    /// And rain is power times ten drops over a grey wash.
    /// </summary>
    public void Test_RegenIstKraftMalZehnTropfen()
    {
        if (!Vorhanden())
        {
            return;
        }

        var runtime = Start();
        runtime.Repaint();
        var trocken = runtime.PaintedMap!;
        Console.WriteLine($"MZ weather: with none, drawn={runtime.WeatherDrawn}");
        AssertEq(runtime.WeatherDrawn, 0, "no weather draws nothing");

        runtime.Facts.Screen.Wetter.Setze(MzWeather.Rain, 5, 0);
        runtime.Tick();
        runtime.Repaint();
        Console.WriteLine($"MZ weather: rain at power 5 -> drawn="
            + $"{runtime.WeatherDrawn}");
        AssertEq(runtime.WeatherDrawn, 50,
            "and the engine's own count is power * 10");

        var nass = runtime.PaintedMap!;
        var geaendert = GeaendertMitSchleier(trocken, nass);
        Console.WriteLine($"MZ weather: {geaendert} pixels differ from the dry map");
        AssertTrue(geaendert > 1000, "and the frame shows the rain");
    }

    /// <summary>And how many pixels differ beyond the grey wash alone.</summary>
    /// <remarks>
    /// <strong>And this has to discount the wash.</strong> The dimmer covers
    /// the whole frame, so every pixel of the map differs from the dry one
    /// whether or not a drop landed on it -- and a test that counted all of
    /// them would call a wash with no rain a rainstorm. A drop paints white,
    /// so what is counted here is the pixels that got <em>lighter</em>.
    /// </remarks>
    private static int GeaendertMitSchleier(
        Rm2kPixelBuffer pVorher, Rm2kPixelBuffer pNachher)
    {
        var zahl = 0;
        for (var i = 0; i + 3 < pVorher.Pixels.Length; i += 4)
        {
            if (pNachher.Pixels[i] > pVorher.Pixels[i])
            {
                zahl++;
            }
        }
        return zahl;
    }

    /// <summary>
    /// And the wash is grey and goes when the weather does.
    /// </summary>
    public void Test_DerSchleierIstGrauUndGehtWeg()
    {
        if (!Vorhanden())
        {
            return;
        }

        var runtime = Start();
        runtime.Repaint();
        var klar = runtime.PaintedMap!;

        runtime.Facts.Screen.Wetter.Setze(MzWeather.Rain, 5, 0);
        runtime.Tick();
        runtime.Repaint();
        var verhangen = runtime.PaintedMap!;

        // **And power 5 is a wash of 30**, which is `Math.floor(power * 6)`.
        var stelle = -1;
        for (var i = 0; i + 3 < klar.Pixels.Length && stelle < 0; i += 4)
        {
            if (klar.Pixels[i] > 200 && klar.Pixels[i] < 255)
            {
                stelle = i;
            }
        }
        if (stelle >= 0)
        {
            var erwartet = (80 * 30 + klar.Pixels[stelle] * 225) / 255;
            Console.WriteLine($"MZ weather: a pixel of {klar.Pixels[stelle]} "
                + $"under a wash of 30 is {verhangen.Pixels[stelle]}, "
                + $"and the arithmetic says {erwartet}");
            AssertTrue(Math.Abs(verhangen.Pixels[stelle] - erwartet) <= 1
                    || verhangen.Pixels[stelle] > klar.Pixels[stelle],
                "and the wash is the engine's own grey at power * 6");
        }

        runtime.Facts.Screen.Wetter.Setze(MzWeather.None, 0, 0);
        runtime.Tick();
        runtime.Repaint();
        Console.WriteLine($"MZ weather: after none, drawn={runtime.WeatherDrawn}");
        AssertEq(runtime.WeatherDrawn, 0, "and the drops are gone");

        var wieder = runtime.PaintedMap!;
        var gleich = 0;
        for (var i = 0; i + 3 < klar.Pixels.Length; i += 4)
        {
            if (klar.Pixels[i] == wieder.Pixels[i])
            {
                gleich++;
            }
        }
        Console.WriteLine($"MZ weather: {gleich} pixels are back to the clear map");
        AssertTrue(gleich > klar.Pixels.Length / 4 / 2,
            "and so is the wash, so a cleared weather leaves no trace");
    }

    /// <summary>
    /// And snow is the same crowd with a different step and shape.
    /// </summary>
    public void Test_SchneeIstEineAndereFigur()
    {
        if (!Vorhanden())
        {
            return;
        }

        var runtime = Start();
        runtime.Repaint();
        var klar = runtime.PaintedMap!;

        runtime.Facts.Screen.Wetter.Setze(MzWeather.Snow, 3, 0);
        runtime.Tick();
        runtime.Repaint();
        Console.WriteLine($"MZ weather: snow at power 3 -> drawn={runtime.WeatherDrawn}");
        AssertEq(runtime.WeatherDrawn, 30, "and snow counts the same way");

        var schnee = runtime.PaintedMap!;
        var heller = GeaendertMitSchleier(klar, schnee);
        Console.WriteLine($"MZ weather: {heller} pixels are lighter than the map");
        AssertTrue(heller > 0, "and snow paints white on the frame");
    }

    /// <summary>
    /// And the power walks toward what the game asked for.
    /// </summary>
    /// <remarks>
    /// <strong>And this was the fourth thing written, tested and called by
    /// nobody.</strong> <c>MzWeather.EinBild</c> moves the power one step per
    /// frame toward the target and counts the frames down -- measured in the
    /// engine at <c>Game_Screen.prototype.updateWeather</c>. Without it, a
    /// game that asks for rain at power 9 over 60 frames gets it at the power
    /// it already had, for ever.
    /// </remarks>
    public void Test_DieKraftWandertZumZiel()
    {
        if (!Vorhanden())
        {
            return;
        }

        var runtime = Start();
        runtime.Facts.Screen.Wetter.Setze(MzWeather.Rain, 9, 60);
        AssertEq(runtime.Facts.Screen.Wetter.Kraft, 0,
            "a weather change starts from no rain at all");
        AssertEq(runtime.Facts.Screen.Wetter.KraftZiel, 9,
            "and it is on its way to nine");

        for (var i = 0; i < 5; i++)
        {
            runtime.Tick();
        }
        Console.WriteLine($"MZ weather: after five of sixty frames the power is "
            + $"{runtime.Facts.Screen.Wetter.Kraft}");
        AssertEq(runtime.Facts.Screen.Wetter.Kraft, 5,
            "and it moves one step per frame, which needs the tick");

        for (var i = 0; i < 60; i++)
        {
            runtime.Tick();
        }
        Console.WriteLine($"MZ weather: at the end the power is "
            + $"{runtime.Facts.Screen.Wetter.Kraft}, frames left "
            + $"{runtime.Facts.Screen.Wetter.Dauer}");
        AssertEq(runtime.Facts.Screen.Wetter.Kraft, 9, "and it arrives at nine");
        AssertEq(runtime.Facts.Screen.Wetter.Dauer, 0, "and stops there");
    }
}
