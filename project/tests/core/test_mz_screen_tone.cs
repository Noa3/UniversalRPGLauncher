using System;
using System.IO;
using UniversalRPG.Plugins;
using UniversalRPG.Rm2k.Rendering;
using UniversalRPG.Tests.Framework;
using UniversalRPG.Web;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And the colour tone a game puts over the screen reaches the frame.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is <c>223 Screen Tint</c>, which the runtime modelled and
/// never drew.</strong> <c>MzScreen</c> keeps the tone, its target and the
/// frames it has left, and <c>ToneIsMoving</c> is what a wait asks about --
/// and nothing applied it to a single pixel.
/// </para>
/// <para>
/// <strong>And the arithmetic is the engine's own shader</strong>, measured in
/// the project's <c>rmmz_core.js</c> at
/// <c>ColorFilter.prototype._fragmentSrc</c>:
/// </para>
/// <code>
/// hsl.y = hsl.y * (1.0 - colorTone.a / 255.0);
/// r = clamp((r / a + colorTone.r / 255.0) * a, 0.0, 1.0);
/// </code>
/// <para>
/// so a tone is an <em>additive</em> shift of each channel by
/// <c>tone[0..2]</c> and a <em>desaturation</em> toward the colour's own HSL
/// lightness by <c>tone[3]</c>.
/// </para>
/// </remarks>
public partial class TestMzScreenTone : TestBase
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

    /// <summary>And a frame with the given tone over it, and without it.</summary>
    private static (Rm2kPixelBuffer Ohne, Rm2kPixelBuffer Mit, bool Gezeichnet)
        MitTon(MzEngineRuntime pRuntime, int[] pTon)
    {
        pRuntime.Facts.Screen.StarteTon(new[] { 0, 0, 0, 0 }, 0);
        pRuntime.Repaint();
        var ohne = pRuntime.PaintedMap!;

        pRuntime.Facts.Screen.StarteTon(pTon, 0);
        pRuntime.Repaint();
        return (ohne, pRuntime.PaintedMap!, pRuntime.ToneDrawn);
    }

    /// <summary>
    /// And no tone is not a tone.
    /// </summary>
    /// <remarks>
    /// <strong>And this matters for the cost.</strong> A tone of
    /// <c>[0,0,0,0]</c> changes nothing, and a frame that walks every pixel to
    /// multiply it by one and add zero is work without a result -- and every
    /// map in every game is shown most of the time with no tone at all.
    /// </remarks>
    public void Test_OhneTonWirdNichtGerechnet()
    {
        if (!Vorhanden())
        {
            return;
        }

        var runtime = Start();
        runtime.Facts.Screen.StarteTon(new[] { 0, 0, 0, 0 }, 0);
        runtime.Repaint();
        Console.WriteLine($"MZ tone: none -> drawn={runtime.ToneDrawn}");
        AssertFalse(runtime.ToneDrawn, "a tone of nothing is not applied");
    }

    /// <summary>
    /// And the shift is added to each channel, and clamped.
    /// </summary>
    public void Test_DerVersatzWirdAddiert()
    {
        if (!Vorhanden())
        {
            return;
        }

        var runtime = Start();
        var (ohne, mit, gezeichnet) = MitTon(runtime, new[] { -64, 32, 0, 0 });
        AssertTrue(gezeichnet, "the tone is applied");

        // **And the arithmetic is exact**: red minus 64, green plus 32, and
        // neither wraps around.
        var geprueft = 0;
        for (var i = 0; i + 3 < ohne.Pixels.Length; i += 4)
        {
            if (ohne.Pixels[i + 3] == 0)
            {
                continue;
            }
            var erwartetRot = Math.Clamp(ohne.Pixels[i] - 64, 0, 255);
            var erwartetGruen = Math.Clamp(ohne.Pixels[i + 1] + 32, 0, 255);
            if (mit.Pixels[i] != erwartetRot
                || mit.Pixels[i + 1] != erwartetGruen
                || mit.Pixels[i + 2] != ohne.Pixels[i + 2])
            {
                Console.WriteLine($"MZ tone: pixel {i / 4} was "
                    + $"{ohne.Pixels[i]},{ohne.Pixels[i + 1]},{ohne.Pixels[i + 2]}"
                    + $" and became {mit.Pixels[i]},{mit.Pixels[i + 1]},{mit.Pixels[i + 2]}");
                AssertTrue(false,
                    "every pixel shifts by the tone and by nothing else");
            }
            geprueft++;
        }
        Console.WriteLine($"MZ tone: -64,+32 checked on {geprueft} pixels");
        AssertTrue(geprueft > 1000, "and there were pixels to check");
    }

    /// <summary>
    /// And the fourth number desaturates toward the colour's own lightness.
    /// </summary>
    /// <remarks>
    /// <strong>And a fully desaturated colour has all three channels
    /// equal</strong>, which is what makes this checkable without knowing the
    /// HSL round trip: at <c>tone[3] = 255</c> the saturation is multiplied by
    /// zero, and what is left is the lightness.
    /// </remarks>
    public void Test_DieSaettigungWirdGenommen()
    {
        if (!Vorhanden())
        {
            return;
        }

        var runtime = Start();
        var (ohne, mit, gezeichnet) = MitTon(runtime, new[] { 0, 0, 0, 255 });
        AssertTrue(gezeichnet, "a full desaturation is applied");

        var geprueft = 0;
        var buntVorher = 0;
        for (var i = 0; i + 3 < ohne.Pixels.Length; i += 4)
        {
            if (ohne.Pixels[i + 3] == 0)
            {
                continue;
            }
            if (ohne.Pixels[i] != ohne.Pixels[i + 1]
                || ohne.Pixels[i + 1] != ohne.Pixels[i + 2])
            {
                buntVorher++;
            }
            if (mit.Pixels[i] != mit.Pixels[i + 1]
                || mit.Pixels[i + 1] != mit.Pixels[i + 2])
            {
                Console.WriteLine($"MZ tone: pixel {i / 4} is still coloured: "
                    + $"{mit.Pixels[i]},{mit.Pixels[i + 1]},{mit.Pixels[i + 2]}");
                AssertTrue(false, "and every colour comes out grey");
            }
            geprueft++;
        }
        Console.WriteLine($"MZ tone: fully desaturated {geprueft} pixels, "
            + $"{buntVorher} of them were coloured before");
        AssertTrue(geprueft > 1000, "and there were pixels to check");
        AssertTrue(buntVorher > 100,
            "and the map really had colours to take away");
    }

    /// <summary>
    /// And half the saturation takes half of it, and keeps the lightness.
    /// </summary>
    /// <remarks>
    /// <strong>And this is the part that tells a mix from a round trip
    /// through HSL.</strong> Measured at the shader: the colour becomes
    /// <c>l + (rgb - l) * k</c>, so the <em>middle</em> of the three channels
    /// does not move at all -- it is the lightness of a colour whose channels
    /// are ordered, and scaling toward it leaves it where it was. A reader
    /// that converted to HSL and back would land on the same numbers; one that
    /// mixed toward the average instead of the lightness would not.
    /// </remarks>
    public void Test_DieMitteBleibtStehen()
    {
        if (!Vorhanden())
        {
            return;
        }

        var runtime = Start();
        var (ohne, mit, _) = MitTon(runtime, new[] { 0, 0, 0, 128 });

        var geprueft = 0;
        for (var i = 0; i + 3 < ohne.Pixels.Length; i += 4)
        {
            if (ohne.Pixels[i + 3] == 0)
            {
                continue;
            }
            var r = ohne.Pixels[i];
            var g = ohne.Pixels[i + 1];
            var b = ohne.Pixels[i + 2];
            var min = Math.Min(r, Math.Min(g, b));
            var max = Math.Max(r, Math.Max(g, b));
            var helligkeit = (min + max) / 2.0;

            // The lightness is what the desaturation keeps, so it survives
            // the tone -- up to the rounding of a byte.
            var mitMin = Math.Min(mit.Pixels[i], Math.Min(mit.Pixels[i + 1], mit.Pixels[i + 2]));
            var mitMax = Math.Max(mit.Pixels[i], Math.Max(mit.Pixels[i + 1], mit.Pixels[i + 2]));
            var mitHelligkeit = (mitMin + mitMax) / 2.0;
            if (Math.Abs(mitHelligkeit - helligkeit) > 1.0)
            {
                Console.WriteLine($"MZ tone: pixel {i / 4} had lightness "
                    + $"{helligkeit} and now has {mitHelligkeit}");
                AssertTrue(false,
                    "and the desaturation keeps the colour's lightness");
            }
            geprueft++;
        }
        Console.WriteLine($"MZ tone: half saturation kept the lightness on "
            + $"{geprueft} pixels");
        AssertTrue(geprueft > 1000, "and there were pixels to check");
    }
}
