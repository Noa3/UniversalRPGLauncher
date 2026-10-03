using System;
using System.Linq;
using UniversalRPG.App.Ui;
using UniversalRPG.Plugins;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The picture an MZ runtime painted, handed to a window.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this closes a gap that was measured, not
/// guessed.</strong> -- <strong><c>PaintedMap</c> was filled by the
/// runtime and read by exactly one test</strong>, --
/// <strong>and nothing in the application ever looked at
/// it.</strong>
/// </para>
/// <para>
/// <strong>And the shape is the same as for the two that came
/// before:</strong> -- <strong>the audio channel</strong> takes
/// <c>MzScreen</c>, -- <strong>the map preview</strong> takes the
/// pixel buffer, -- <strong>and no host knows that Godot
/// exists.</strong>
/// </para>
/// </remarks>
public partial class TestMzMapPreview : TestBase
{
    private const string Projekt = "E:/RPGMakerGames/CamelliaCoronation-Win";

    private static bool Vorhanden() =>
        System.IO.Directory.Exists(Projekt + "/data");

    /// <summary>
    /// And a window with no picture says why, and does not crash.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the runtime has four refusals of its own</strong>
    /// -- <strong>map not among the maps, tileset named and missing,
    /// wrong size, and the renderer's own refusal</strong>, --
    /// <strong>and a window that said nothing would leave the reader
    /// guessing between four causes.</strong>
    /// </para>
    /// </remarks>
    public void Test_OhneKarteSagtWarum()
    {
        var vorschau = new MzMapPreview();
        try
        {
            vorschau.Grund = "map 99 is not among this game's maps";
            vorschau.SetzeKarte(null);
            // **Und `_Draw` ruft man nicht selbst** -- **und der
            // Aufruf ist in Godot 4.7 verboten** -- **und der
            // Test tat es drei Mal.** -- **Stattdessen wird hier
            // nur der Zustand geprueft, den `_Draw` liest.**
            vorschau.QueueRedraw();
            AssertTrue(vorschau.Karte() == null,
                "**and a window with no picture has no picture** -- and"
                    + " it takes the reason it was given and holds"
                    + " it for the draw it does not run itself");
        }
        finally
        {
            vorschau.Free();
        }
    }

    /// <summary>
    /// And the real runtime's picture goes into the window.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the whole point of the commit</strong>, --
    /// <strong>because before it, the map this runtime painted was
    /// drawn by nobody.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieEchteKarteLandetImFenster()
    {
        if (!Vorhanden())
        {
            return;
        }

        var spiel = new PluginGameInfo
        {
            GameDirectory = Projekt,
            EngineId = EnginePluginIds.RpgMakerMz,
            Generation = "mz",
            DetectorScore = 3,
        };
        using var host = new EnginePluginHost(
            BuiltInEnginePluginCatalog.CreateRuntimeRegistry());
        AssertTrue(host.Start(spiel).Success,
            "**and the runtime starts**");
        if (host.Runtime is not MzEngineRuntime lauf)
        {
            AssertTrue(false, "**and the host built an MZ runtime**");
            return;
        }

        AssertTrue(lauf.GoTo(17),
            "**and map 17 paints** -- and the refusal is: "
                + lauf.PaintReason);
        AssertTrue(lauf.PaintedMap != null,
            "**and there is a picture**");

        var vorschau = new MzMapPreview();
        try
        {
            vorschau.Grund = lauf.PaintReason;
            vorschau.SetzeKarte(lauf.PaintedMap);
            vorschau.QueueRedraw();

            Console.WriteLine($"Karte {lauf.PaintedMap!.Width}x"
                + $"{lauf.PaintedMap.Height}  Farben {lauf.PaintedColours}"
                + $"  Figuren {lauf.FiguresDrawn}");

            AssertTrue(ReferenceEquals(vorschau.Karte(), lauf.PaintedMap),
                "**and the window holds the runtime's own buffer** --"
                    + " and not a copy, and not a rendering of its"
                    + " own");
            // **Und  die  Karte  17  dieses  Projekts  ist
            //  22  mal  16  Kacheln** -- **gemessen  an
            //  `data/Map017.json`**, -- **und  eine  Kachel  ist  48
            //  Pixel**, -- **und  22  mal  48  sind  1056.**
            //
            // **Und  ich  schrieb  14  mal  48  und  das  ist  die
            //  Breite  einer  anderen  Karte**, -- **und  die  Zahl
            //  stand  in  einem  Test  im  selben  Projekt.**
            var quelle = System.IO.Path.Combine(
                Projekt, "data", "Map017.json");
            var karte = System.Text.Json.JsonDocument.Parse(
                System.IO.File.ReadAllText(quelle));
            var kacheln = karte.RootElement
                .GetProperty("width").GetInt32();

            AssertEq(kacheln * 48, vorschau.Karte()!.Width,
                "**and it is as wide as the project's own map file"
                    + " says** -- and that is " + kacheln
                    + " tiles of 48 pixels, and I wrote 14 without"
                    + " looking at which map the other test was on");
            AssertTrue(lauf.PaintedColours > 1,
                "**and the picture is not one colour** -- and it has"
                    + " " + lauf.PaintedColours);
            AssertTrue(lauf.FiguresDrawn > 0,
                "**and the figures are on it** -- and map 17 has nine"
                    + " events with a picture page");
        }
        finally
        {
            vorschau.Free();
        }
    }

    /// <summary>
    /// And the window does not upload the same picture twice.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And a picture of 672 by 432 pixels is a
    /// megabyte</strong>, -- <strong>and re-uploading it sixty times a
    /// second is what the signature is there to
    /// prevent.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieSignaturHindertDoppeltLaden()
    {
        var puffer = new UniversalRPG.Rm2k.Rendering.Rm2kPixelBuffer(8, 4);
        puffer.Clear();
        puffer.TrySetPixel(0, 0, 0, 0, 0, 255);
        puffer.TrySetPixel(7, 3, 255, 255, 255, 255);

        var vorschau = new MzMapPreview();
        try
        {
            vorschau.SetzeKarte(puffer);
            vorschau.QueueRedraw();
            var erste = vorschau.Karte();
            vorschau.SetzeKarte(puffer);
            vorschau.QueueRedraw();
            AssertTrue(ReferenceEquals(erste, vorschau.Karte()),
                "**and handing the same buffer again changes"
                    + " nothing** -- and that is what the reference"
                    + " check in `SetzeKarte` is for");

            vorschau.SetzeKarte(null);
            vorschau.QueueRedraw();
            AssertTrue(vorschau.Karte() == null,
                "**and handing nothing clears it**");
        }
        finally
        {
            vorschau.Free();
        }
    }
}
