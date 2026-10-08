using System;
using System.IO;
using UniversalRPG.Plugins;
using UniversalRPG.Rm2k.Rendering;
using UniversalRPG.Tests.Framework;
using UniversalRPG.Web;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And a game's pictures are drawn over the map.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this was the largest thing a running game showed and this
/// runtime did not.</strong> Measured in the three test projects:
/// <strong>Skies asks for 168 pictures</strong> -- its whole intro is built
/// from them -- <strong>LegalTruck for 116 and Camellia for 4</strong>. The
/// runtime modelled every one of them, with origin, scale and opacity, and
/// drew none.
/// </para>
/// <para>
/// <strong>And the placement is the engine's own, measured in the project's
/// <c>rmmz_sprites.js</c>:</strong> <c>this.x = Math.round(picture.x())</c>, and
/// the anchor is <c>0</c> at origin 0 and <c>0.5</c> at everything else -- so
/// origin 0 puts the picture's upper left on (x, y) and origin 1, the editor's
/// default, puts its centre there.
/// </para>
/// <para>
/// <strong>And the measurement is changed pixels, not alpha.</strong> A map
/// covers the whole frame and every pixel of it is opaque, so a test that
/// counted opaque pixels would report the same number with a picture and
/// without one -- which is what the first version of this test did, and it
/// failed for that reason and not because the picture was missing.
/// </para>
/// </remarks>
public partial class TestMzPictures : TestBase
{
    private const string Projekt = "E:/RPGMakerGames/CamelliaCoronation-Win";

    private static bool Vorhanden() =>
        File.Exists(Projekt + "/img/pictures/Illustration.png_")
        && File.Exists(Projekt + "/data/System.json");

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

    /// <summary>And how many pixels of a region a picture changed.</summary>
    private static int GeaendertePixel(
        Rm2kPixelBuffer pVorher, Rm2kPixelBuffer pNachher, int pBisX, int pBisY)
    {
        var zahl = 0;
        for (var y = 0; y < Math.Min(pBisY, pVorher.Height); y++)
        {
            for (var x = 0; x < Math.Min(pBisX, pVorher.Width); x++)
            {
                var i = (y * pVorher.Width + x) * 4;
                if (pVorher.Pixels[i] != pNachher.Pixels[i]
                    || pVorher.Pixels[i + 1] != pNachher.Pixels[i + 1]
                    || pVorher.Pixels[i + 2] != pNachher.Pixels[i + 2])
                {
                    zahl++;
                }
            }
        }
        return zahl;
    }

    /// <summary>
    /// And a picture the project really has reaches the frame it is drawn on.
    /// </summary>
    public void Test_EinBildDesSpielsWirdGezeichnet()
    {
        if (!Vorhanden())
        {
            return;
        }

        var runtime = Start();
        runtime.Repaint();
        var blatt = runtime.PictureSheet("Illustration");
        Console.WriteLine($"MZ picture sheet: {blatt?.Width}x{blatt?.Height} "
            + $"palette={blatt?.WasPalette}");
        AssertTrue(blatt != null,
            "the project's own picture is read out of img/pictures");
        if (blatt == null)
        {
            return;
        }

        var ohne = runtime.PaintedMap!;

        // **And now the picture, where the game puts it**: origin 0, upper
        // left on (0, 0), full scale and full opacity.
        runtime.Facts.Screen.Show(
            1, "Illustration", 0, 0, 0, 100, 100, 255, 0);
        AssertTrue(runtime.Repaint(), "the frame repaints");
        Console.WriteLine($"MZ picture: drawn={runtime.PicturesDrawn}");

        AssertEq(runtime.PicturesDrawn, 1, "one picture is drawn");
        var mit = runtime.PaintedMap!;
        var geaendert = GeaendertePixel(ohne, mit, 200, 200);
        Console.WriteLine($"MZ picture: changed pixels in the top left "
            + $"{geaendert} of {200 * 200}");
        AssertTrue(geaendert > 200 * 200 / 4,
            "and the frame it drew on shows the picture where the map was");
    }

    /// <summary>
    /// And the origin decides which corner stands on (x, y).
    /// </summary>
    /// <remarks>
    /// <strong>And this is the part a reader gets wrong.</strong> The editor's
    /// default is origin 1, so a picture shown at (x, y) has its
    /// <em>centre</em> there and its upper left at (x - width/2, y - height/2) --
    /// and a renderer that always used the upper left would put every
    /// screen-filling picture half off the frame.
    /// </remarks>
    public void Test_DerUrsprungEntscheidetDieEcke()
    {
        if (!Vorhanden())
        {
            return;
        }

        var runtime = Start();

        // The map alone, so that a difference is the picture and nothing else.
        runtime.Repaint();
        var karte = runtime.PaintedMap!;

        // Origin 0 at (64, 64): the upper left corner stands there, so the
        // first 64 columns hold the map and nothing of the picture.
        runtime.Facts.Screen.Show(1, "Illustration", 0, 64, 64, 100, 100, 255, 0);
        runtime.Repaint();
        var links = GeaendertePixel(karte, runtime.PaintedMap!, 60, 200);
        var rechts = GeaendertePixel(
            karte, runtime.PaintedMap!, 200, 200);
        Console.WriteLine($"MZ picture origin 0 at 64,64: columns up to 60 "
            + $"changed {links} pixels, up to 200 changed {rechts}");
        AssertEq(links, 0,
            "origin 0 leaves the columns before x to the map");
        AssertTrue(rechts > 0, "and draws from x on");

        // Origin 1 at the same point: the centre stands there instead, so the
        // picture begins at 64 - width/2 and reaches the frame's own corner.
        runtime.Facts.Screen.Show(2, "Illustration", 1, 64, 64, 100, 100, 255, 0);
        runtime.Repaint();
        var vorne = GeaendertePixel(karte, runtime.PaintedMap!, 60, 200);
        Console.WriteLine($"MZ picture origin 1 at 64,64: columns up to 60 "
            + $"changed {vorne} pixels");
        AssertTrue(vorne > 0,
            "and with origin 1 the same picture at the same point reaches "
                + "the frame's own corner, because its centre is on (64, 64)");
    }

    /// <summary>
    /// And a picture with nothing in it, or no file, is not counted.
    /// </summary>
    /// <remarks>
    /// <strong>And this matters for a report and not for the picture.</strong>
    /// A game that shows a picture the project does not have -- or one with
    /// its opacity at zero, which <c>showPicture</c> allows -- must not be
    /// counted as drawn, or a caller reading that number would believe the
    /// frame shows something it does not.
    /// </remarks>
    public void Test_EinLeeresBildWirdNichtGezaehlt()
    {
        if (!Vorhanden())
        {
            return;
        }

        var runtime = Start();
        runtime.Facts.Screen.Show(
            1, "NoSuchPictureInThisProject", 0, 0, 0, 100, 100, 255, 0);
        runtime.Repaint();
        Console.WriteLine($"MZ picture missing file: drawn={runtime.PicturesDrawn}");
        AssertEq(runtime.PicturesDrawn, 0,
            "a picture whose file the project does not have is not drawn");

        runtime.Facts.Screen.Show(2, "Illustration", 0, 0, 0, 100, 100, 0, 0);
        runtime.Repaint();
        Console.WriteLine($"MZ picture opacity 0: drawn={runtime.PicturesDrawn}");
        AssertEq(runtime.PicturesDrawn, 0,
            "and neither is one at zero opacity");

        runtime.Facts.Screen.Show(3, "Illustration", 0, 0, 0, 100, 100, 128, 0);
        runtime.Repaint();
        Console.WriteLine($"MZ picture opacity 128: drawn={runtime.PicturesDrawn}");
        AssertEq(runtime.PicturesDrawn, 1, "and a half-transparent one is");
    }
}
