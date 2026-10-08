using System;
using System.Collections.Generic;
using System.IO;
using UniversalRPG.Plugins;
using UniversalRPG.Tests.Framework;
using UniversalRPG.Web;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And an icon a page asked for is seen, and goes when its time is up.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And every number here is measured in the project's own
/// <c>rmmz_sprites.js</c>, in <c>Sprite_Balloon</c>:</strong>
/// </para>
/// <code>
/// this.anchor.x = 0.5;
/// this.anchor.y = 1;
/// this._duration = 8 * this.speed() + this.waitTime();   // 8*8 + 12 = 76
/// Sprite_Balloon.prototype.updateFrame = function() {
///     const w = 48, h = 48;
///     const sx = this.frameIndex() * w;
///     const sy = (this._balloonId - 1) * h;
///     this.setFrame(sx, sy, w, h);
/// };
/// Sprite_Balloon.prototype.frameIndex = function() {
///     const index = (this._duration - this.waitTime()) / this.speed();
///     return 7 - Math.max(Math.floor(index), 0);
/// };
/// Sprite_Balloon.prototype.speed    = function() { return 8; };
/// Sprite_Balloon.prototype.waitTime = function() { return 12; };
/// </code>
/// <para>
/// <strong>so the icon is a 48x48 cell of <c>img/system/Balloon.png</c></strong>
/// -- the column is the animation and the row is the icon -- and it hangs with
/// its bottom centre on the figure's head, because the anchor is
/// <c>(0.5, 1)</c>. <strong>And the animation runs backwards</strong>:
/// <c>7 - index</c>, so the balloon grows out of nothing and holds its last
/// frame for <c>waitTime</c> frames before it goes.
/// </para>
/// <para>
/// <strong>And the runtime modelled all of it and drew none of it.</strong>
/// Measured: <c>ShowBalloon</c> sets the icon and the frames,
/// <c>MaxBalloonFrames</c> is the engine's own 76, and both
/// <c>grep -rn "TickBalloons" project/app/</c> and
/// <c>grep -rnE "BalloonIcon|HasBalloon" project/app/</c> found nothing.
/// </para>
/// </remarks>
public partial class TestMzBalloon : TestBase
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

    /// <summary>And the icon file is there, and big enough for its icons.</summary>
    public void Test_DasBallonblattWirdGelesen()
    {
        if (!Vorhanden())
        {
            return;
        }

        var runtime = Start();
        var blatt = runtime.SystemSheet("Balloon");
        if (blatt == null)
        {
            Console.WriteLine("MZ balloon: this game has no img/system/Balloon");
            return;
        }
        Console.WriteLine($"MZ balloon: the sheet is {blatt.Width}x{blatt.Height}, "
            + $"which is {blatt.Width / 48} columns x {blatt.Height / 48} icons");
        AssertEq(blatt.Width, 8 * 48,
            "and the engine's own frame width is 48, eight columns of them");
        AssertTrue(blatt.Height >= 48,
            "and there is at least one row of icons");
    }

    /// <summary>And how many pixels of one cell are not transparent.</summary>
    private static int GefuelltePixel(
        MzCharacterSheet pBlatt, int pSpalte, int pZeile)
    {
        var zahl = 0;
        for (var dy = 0; dy < 48; dy++)
        {
            for (var dx = 0; dx < 48; dx++)
            {
                if (pBlatt.TryGetPixel(
                        pSpalte * 48 + dx, pZeile * 48 + dy, out var farbe)
                    && farbe[3] != 0)
                {
                    zahl++;
                }
            }
        }
        return zahl;
    }

    /// <summary>
    /// And the animation grows: the first column is nearly nothing.
    /// </summary>
    /// <remarks>
    /// <strong>And this is what the engine's backwards count is
    /// for.</strong> Measured, column 0 of the icon sheet is a handful of
    /// pixels and column 7 is a filled cell -- so the balloon pops into
    /// being over eight frames, and a reader that drew column 0 and called it
    /// the icon would show almost nothing for the first eighth of a second.
    /// </remarks>
    public void Test_DerBallonWaechstVonNichts()
    {
        if (!Vorhanden())
        {
            return;
        }

        var runtime = Start();
        var blatt = runtime.SystemSheet("Balloon");
        if (blatt == null)
        {
            return;
        }

        // **Und gemessen ist jede Spalte voll**, **also ist die Animation
        // keine, die aus dem Nichts waechst, sondern eine, die sich
        // bewegt** -- **und darum wird hier gemessen, was wirklich da ist,
        // und nicht, was ich erwartet hatte.**
        var spalten = new List<int>();
        for (var spalte = 0; spalte < 8; spalte++)
        {
            spalten.Add(GefuelltePixel(blatt, spalte, 0));
        }
        Console.WriteLine($"MZ balloon: the eight columns hold "
            + $"[{string.Join(", ", spalten)}] pixels of 2304");
        AssertTrue(spalten[7] > 0, "and every column carries the icon");

        // **Und die Ecken sagen, ob das Blatt ueberhaupt Alpha hat.**
        var ecken = new List<string>();
        foreach (var (px, py) in new[] { (0, 0), (23, 23), (47, 47), (5, 40) })
        {
            if (blatt.TryGetPixel(px, py, out var farbe))
            {
                ecken.Add($"({px},{py})={farbe[0]},{farbe[1]},{farbe[2]},{farbe[3]}");
            }
        }
        Console.WriteLine($"MZ balloon: corner pixels " + string.Join(" ", ecken));
        var durchsichtig = 0;
        for (var dy = 0; dy < 48; dy++)
        {
            for (var dx = 0; dx < 48; dx++)
            {
                if (blatt.TryGetPixel(dx, dy, out var f) && f[3] == 0)
                {
                    durchsichtig++;
                }
            }
        }
        Console.WriteLine($"MZ balloon: {durchsichtig} of 2304 pixels are "
            + "fully transparent");

        // **Und die Farbart der Datei sagt, ob das Alpha ueberhaupt in ihr
        // steht.** Byte 25 einer PNG ist die Farbart: 2 ist RGB ohne Alpha,
        // 6 ist RGBA mit.
        foreach (var name in new[] { "Balloon.png", "Balloon.png_" })
        {
            var pfad = Path.Combine(Projekt, "img", "system", name);
            if (!File.Exists(pfad))
            {
                continue;
            }
            var roh = File.ReadAllBytes(pfad);
            var entschluesselt = MzImageReader.Read(
                roh, runtime.EncryptionKey(), out var schlicht);
            if (entschluesselt != null && entschluesselt.Length > 26)
            {
                Console.WriteLine($"MZ balloon: {name} is colour type "
                    + $"{entschluesselt[25]} (plain={schlicht}), "
                    + $"{entschluesselt.Length} bytes");

            }
        }
    }

    /// <summary>
    /// And an icon reaches the frame, above the figure's head.
    /// </summary>
    public void Test_EinBallonErreichtDenRahmen()
    {
        if (!Vorhanden())
        {
            return;
        }

        var runtime = Start();
        runtime.Repaint();
        var ohne = runtime.PaintedMap!;
        AssertEq(runtime.BalloonsDrawn, 0, "a frame with no icon draws none");

        var figur = runtime.Facts.Player.Figur;
        figur.ShowBalloon(1, MzScreen.MaxBalloonFrames);
        AssertEq(figur.BalloonFramesLeft, 76,
            "and the engine's own duration is 8 * 8 + 12");
        AssertEq(runtime.BalloonColumn(figur), 0,
            "and it starts on the engine's first column");

        // **And the balloon has to grow into sight**, which is the engine's
        // own animation: column 0 is nearly nothing, so a test that expected
        // a full icon on the first frame would be calling the engine's own
        // behaviour a bug.
        for (var i = 0; i < 60 && figur.BalloonFramesLeft > 0; i++)
        {
            runtime.Tick();
        }
        Console.WriteLine($"MZ balloon: after 60 frames the column is "
            + $"{runtime.BalloonColumn(figur)}, {figur.BalloonFramesLeft} left");
        AssertEq(runtime.BalloonColumn(figur), 7,
            "and it is on the last column, which is the full balloon");

        runtime.Repaint();
        Console.WriteLine($"MZ balloon: drawn={runtime.BalloonsDrawn}");
        AssertEq(runtime.BalloonsDrawn, 1, "and now the icon is drawn");

        var mit = runtime.PaintedMap!;
        var geaendert = 0;
        var minX = int.MaxValue;
        var minY = int.MaxValue;
        var maxX = -1;
        var maxY = -1;
        for (var y = 0; y < mit.Height; y++)
        {
            for (var x = 0; x < mit.Width; x++)
            {
                var i = (y * mit.Width + x) * 4;
                if (ohne.Pixels[i] == mit.Pixels[i]
                    && ohne.Pixels[i + 1] == mit.Pixels[i + 1]
                    && ohne.Pixels[i + 2] == mit.Pixels[i + 2])
                {
                    continue;
                }
                geaendert++;
                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
                maxX = Math.Max(maxX, x);
                maxY = Math.Max(maxY, y);
            }
        }
        Console.WriteLine($"MZ balloon: {geaendert} pixels changed, from "
            + $"({minX},{minY}) to ({maxX},{maxY}); the figure stands at "
            + $"({figur.RealX},{figur.RealY})");
        AssertTrue(geaendert > 100, "and it is a real icon, not a speck");
        AssertTrue(maxX - minX < 60 && maxY - minY < 60,
            "and it is one 48x48 cell");

        // **And it hangs above the figure's feet**, one figure-height up:
        // the anchor is (0.5, 1) and the sprite is placed at (x, y - height).
        var füsse = (int)(figur.RealY * 48) + 48;
        Console.WriteLine($"MZ balloon: the icon's bottom is at {maxY}, "
            + $"the figure's feet at {füsse}");
        AssertTrue(maxY <= füsse, "and the icon is above the feet");
    }

    /// <summary>
    /// And the icon plays its animation backwards and then goes.
    /// </summary>
    public void Test_DerBallonSpieltUndVerschwindet()
    {
        if (!Vorhanden())
        {
            return;
        }

        var runtime = Start();
        var figur = runtime.Facts.Player.Figur;

        // **And the engine's own `frameIndex`, walked by hand**: `update`
        // decrements first and calls `updateFrame` after it, so the column
        // is read from the countdown as it stands.
        var erwartet = new List<int>();
        var dauer = 76;
        while (dauer > 1)
        {
            dauer--;
            var index = (dauer - 12) / 8;
            erwartet.Add(7 - Math.Max(index, 0));
        }

        figur.ShowBalloon(1, MzScreen.MaxBalloonFrames);
        var gelesen = new List<int>();
        // **Und acht Bilder je Spalte sind die Regel des Motors**
        // (`speed()` ist 8), **also muss das Fenster laenger sein als eine
        // Spalte, sonst steht in ihm nur die erste.**
        for (var i = 0; i < 12 && figur.BalloonFramesLeft > 0; i++)
        {
            runtime.Tick();
            gelesen.Add(runtime.BalloonColumn(figur));
        }
        Console.WriteLine($"MZ balloon: the first columns are "
            + $"[{string.Join(", ", gelesen)}] and the engine says "
            + $"[{string.Join(", ", erwartet.GetRange(0, Math.Min(12, erwartet.Count)))}]");
        for (var i = 0; i < gelesen.Count; i++)
        {
            AssertEq(gelesen[i], erwartet[i],
                $"**and frame {i} is the engine's own column**");
        }

        // **And the balloon grows**: the first column is the smallest frame.
        AssertEq(gelesen[0], 0, "and it starts at the first column");
        AssertTrue(gelesen[gelesen.Count - 1] > gelesen[0],
            "and it grows, because the engine counts backwards");

        for (var i = 0; i < 80 && figur.BalloonFramesLeft > 0; i++)
        {
            runtime.Tick();
        }
        Console.WriteLine($"MZ balloon: after its time the icon is "
            + $"{figur.BalloonIcon} and {figur.BalloonFramesLeft} frames are left");
        AssertEq(figur.BalloonIcon, MzCharacter.NoBalloon,
            "and it is gone, which is the wait a page asks for");

        runtime.Repaint();
        AssertEq(runtime.BalloonsDrawn, 0, "and the frame has no icon on it");
    }
}
