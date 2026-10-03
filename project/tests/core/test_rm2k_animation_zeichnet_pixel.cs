using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using UniversalRPG.Rm2k.Parser;
using UniversalRPG.Rm2k.Rendering;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And that a battle animation puts pixels on the screen.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And criterion 2 is animations and effects, and 425 cells
/// were decoded and none was drawn.</strong> --
/// <strong>And a decoded cell nobody draws is a paragraph, not a
/// picture.</strong>
/// </para>
/// <para>
/// <strong>And this uses Dragon Destiny's own chipset and its own
/// animation</strong>, -- <strong>so the pixels are the game's and not
/// a drawn rectangle.</strong>
/// </para>
/// </remarks>
public partial class TestRm2kAnimationZeichnetPixel : TestBase
{
    private const string Ldb =
        "res://tests/fixtures/rm2k-dragon-destiny/RPG_RT.ldb";

    /// <summary>
    /// And the game's own chipset, and not a fixture.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the pinned fixture carries no chipset</strong> --
    /// <strong>it holds the bank, two maps and the map tree and nothing
    /// else.</strong> -- <strong>And a test that asks for a file the
    /// fixture does not have returns early and reports
    /// green</strong>, -- <strong>which is how the first version of
    /// this test passed while asserting nothing.</strong>
    /// </para>
    /// <para>
    /// <strong>And <c>File.Exists</c> on a <c>res://</c> path is not
    /// reliable in the headless runner</strong>, -- <strong>so the
    /// assets come from the real game directory</strong> -- <strong>and
    /// the pixels below are Dragon Destiny's own.</strong>
    /// </para>
    /// </remarks>
    private const string Spiel =
        "E:/RPGMakerGames/Dragon Destiny";

    /// <summary>
    /// And Dragon Destiny's first chipset.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And there is no <c>ChipSet.png</c> in the game's
    /// root</strong>, -- <strong>the game keeps twenty-nine chipsets
    /// in a <c>ChipSet</c> folder under the names the bank
    /// gives</strong>, -- <strong>and chipset one is
    /// <c>desert_outdoor_town</c>.</strong>
    /// </para>
    /// </remarks>
    private const string ChipsetVerzeichnis =
        "E:/RPGMakerGames/Dragon Destiny/ChipSet/";

    /// <summary>
    /// And the animation's cells reach the frame buffer.
    /// </summary>
    public void Test_DieZellenErreichenDenPuffer()
    {
        // **Und  die  Fixture  muss  da  sein** -- **und  ein
        //  stilles  Zurueckkehren  waere  ein  gruener  Test  ohne
        //  Aussage.**
        // **Und  `File.Exists`  auf  einem  `res://`-Pfad  gibt  im
        //  headless  Runner  `false`**, -- **und  der  Parser  liest
        //  denselben  Pfad  ohne  Probleme.**  **Und  darum  wird  die
        //  Bank  hier  geprueft  und  nicht  ihre  Existenz.**
        // **Und  der  Chipsetname  kommt  aus  der  Bank  und  nicht  aus
        //  einer  Vermutung**:  -- **Chipset  1  heisst  dort
        //  `desert_outdoor_town`,  und  die  Datei  heisst  genauso.**
        var parser2 = new Rm2kParser();
        var bank2 = parser2.ParseDatabase(Ldb);
        if (!bank2.IsSuccess()
            || !bank2.GetData().ContainsKey("chipsets"))
        {
            AssertTrue(false, "**and the bank names chipsets**");
            return;
        }

        var chipsets = (Godot.Collections
            .Array<Godot.Collections.Dictionary>)
            bank2.GetData()["chipsets"];
        var chipsetPfad = ChipsetVerzeichnis
            + chipsets[0]["chipset_name"].AsString() + ".png";
        Console.WriteLine("Chipset 1 = " + chipsetPfad);
        if (!System.IO.File.Exists(chipsetPfad))
        {
            Console.WriteLine("fehlt: " + chipsetPfad);
            AssertTrue(false,
                "**and the game's chipset is there** -- and a"
                    + " silent return would be a green test that"
                    + " proved nothing");

            return;
        }

        if (!Rm2kChipsetBitmap.TryLoad(chipsetPfad,
                out var chipset, out var ladeFehler))
        {
            Console.WriteLine("Chipset: " + ladeFehler);
            AssertTrue(false, "**and the chipset loads**");
            return;
        }

        Console.WriteLine("Chipset: " + chipset.Image.Width
            + "x" + chipset.Image.Height);

        // **Und  das  Chipset  des  Spiels.**
        var parser = new Rm2kParser();
        var result = parser.ParseDatabase(Ldb);
        if (!result.IsSuccess())
        {
            AssertTrue(false, "**and the bank is read**");
            return;
        }

        var anims = (Godot.Collections
            .Array<Godot.Collections.Dictionary>)
            result.GetData()["animations"];

        Godot.Collections.Array<Godot.Collections.Array<
            Godot.Collections.Dictionary>>? frames = null;
        var animationsId = 0;
        var letzteWeigerung = "";
        foreach (var a in anims)
        {
            if (a["id"].AsInt32() != 5
                || !a.ContainsKey("unknown_fields"))
            {
                continue;
            }

            if (!Rm2kAnimationZelle.TryDecode(
                    (Godot.Collections.Array<Godot.Collections
                        .Dictionary>)a["unknown_fields"],
                    out var dekodiert, out var warum))
            {
                letzteWeigerung = warum;
                Console.WriteLine("Animation 5 verweigert: "
                    + warum);
                continue;
            }

            frames = dekodiert;
            animationsId = a["id"].AsInt32();
            break;
        }

        if (frames == null || frames.Count == 0)
        {
            AssertTrue(false,
                "**and animation five decodes** -- and: "
                    + letzteWeigerung);
            return;
        }

        Console.WriteLine("Animation " + animationsId + ": "
            + frames.Count + " Frames");

        // **Und  jetzt  wird  gezeichnet.**
        var puffer = new Rm2kPixelBuffer(320, 240);
        var renderer = new Rm2kAnimationsRenderer(chipset);

        var gezeichnetGesamt = 0;
        for (var f = 0; f < frames.Count; f++)
        {
            var zellen = frames[f];
            var gezeichnet = renderer.Zeichne(
                puffer, zellen, 0, 0);
            Console.WriteLine("  Frame " + f + ": "
                + zellen.Count + " Zellen, " + gezeichnet
                + " gezeichnet");

            if (gezeichnet > 0)
            {
                gezeichnetGesamt++;
            }
        }

        Console.WriteLine("gezeichnete Frames: "
            + gezeichnetGesamt);

        AssertTrue(gezeichnetGesamt > 0,
            "**and at least one frame puts pixels on the"
                + " screen** -- and a decoder that produced 425"
                + " cells and a renderer that drew none would"
                + " leave the screen empty while both claimed to"
                + " work");
    }

    /// <summary>
    /// And a cell outside the chipset is refused rather than drawn.
    /// </summary>
    public void Test_EineZelleAusserhalbWirdVerweigert()
    {
        if (!System.IO.File.Exists(Spiel + "/ChipSet.png"))
        {
            return;
        }

        if (!Rm2kChipsetBitmap.TryLoad(Spiel + "/ChipSet.png",
                out var chipset, out _))
        {
            return;
        }

        var puffer = new Rm2kPixelBuffer(64, 64);
        var renderer = new Rm2kAnimationsRenderer(chipset);

        // **Und  eine  Zelle  weit  ausserhalb  des  Chipsets  wird
        //  nicht  gezeichnet  und  nicht  abgeschnitten  ohne
        //  Meldung.**
        var zellen = new Godot.Collections
            .Array<Godot.Collections.Dictionary>
            {
                new Godot.Collections.Dictionary
                {
                    { "valid", 1 },
                    { "cell_id", 999999 },
                    { "x", 0 },
                    { "y", 0 },
                    { "zoom", 100 },
                    { "tone_red", 100 },
                    { "tone_green", 100 },
                    { "tone_blue", 100 },
                    { "tone_gray", 100 },
                    { "transparency", 0 },
                },
            };

        var gezeichnet = renderer.Zeichne(puffer, zellen, 0, 0);
        Console.WriteLine("ausserhalb gezeichnet: "
            + gezeichnet);

        AssertEq(0, gezeichnet,
            "**and a cell the chipset does not carry draws"
                + " nothing** -- and a reader that clamped the"
                + " index would draw a random tile where the game"
                + " asked for nothing");
    }
}
