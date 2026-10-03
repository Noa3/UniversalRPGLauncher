using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And what the save codec leaves out.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the codec covers 28 of 147 state fields</strong>, --
/// <strong>and 119 are not saved.</strong>
/// </para>
/// <para>
/// <strong>And they are not equal.</strong> -- <strong>A field that
/// carries progress and a field that carries only what the screen is
/// showing right now are different things</strong>, -- <strong>and
/// saving both in one flat list would make the format claim more
/// than it delivers.</strong>
/// </para>
/// </remarks>
public partial class TestRm2kLueckenGezaehlt : TestBase
{
    private static readonly string[] BildschirmPraefixe =
    {
        "Battle", "IsBattle", "Shop", "IsShop", "Inn", "IsInn",
        "Pan", "Movie", "Fade", "Animation", "Character", "Tint",
        "Weather", "Atb", "Volume", "Tempo", "FrameRate",
        "SystemGraphic", "Passability", "UpperLayer", "LowerLayer",
        "TerrainData", "Chipset",
    };

    /// <summary>
    /// And the fields the codec does not write.
    /// </summary>
    public void Test_DieNichtGesichertenFelder()
    {
        // **Und  `res://`  ist  im  headless  Runner  fuer  diese
        //  Quellen  nicht  lesbar** -- **und  ein  Pfad,  den  der
        //  Test  nicht  oeffnen  kann,  ist  kein  Befund  und  kein
        //  Fehler**, -- **sondern  eine  Ausrede.**
        //
        // **Und  darum  wird  das  Projektverzeichnis  genommen,  und
        //  ein  fehlender  Pfad  wird  zu  einer  Behauptung.**
        var basis = "E:/URPG/project/src/rm2k/simulation/";
        var codecPfad = basis + "Rm2kSimulationSaveCodec.cs";
        var zustandPfad = basis + "GameSimulationState.cs";
        foreach (var pfad in new[] { codecPfad, zustandPfad })
        {
            if (!File.Exists(pfad))
            {
                Console.WriteLine("fehlt: " + pfad);
                AssertTrue(false,
                    "**and this repository's own sources are"
                    + " there** -- and a missing path is neither a"
                    + " finding nor a failure");

                return;
            }
        }

        var codec = File.ReadAllText(codecPfad);
        var zustand = File.ReadAllText(zustandPfad);

        var gesichert = new HashSet<string>(
            Regex.Matches(codec, @"pState\.(\w+)")
                .Select(m => m.Groups[1].Value));

        var felder = Regex.Matches(
            zustand,
            @"public\s+[\w<>,\?\[\]\.]+[\s\w]*?\s(\w+)\s*\{\s*get;\s*(?:init|set|private set)")
            .Select(m => m.Groups[1].Value)
            .ToHashSet();

        var fehlend = felder.Where(x => !gesichert.Contains(x))
            .OrderBy(x => x).ToList();

        Console.WriteLine("Zustandsfelder: " + felder.Count);
        Console.WriteLine("vom Codec beruehrt: " + gesichert.Count);
        Console.WriteLine("nicht gesichert: " + fehlend.Count);

        var bildschirm = fehlend.Where(x =>
            BildschirmPraefixe.Any(p =>
                x.StartsWith(p, StringComparison.Ordinal))
            || x.Contains("Graphic", StringComparison.Ordinal)
            || x.Contains("Pan", StringComparison.Ordinal)
            || x.Contains("Subcommand", StringComparison.Ordinal))
            .OrderBy(x => x).ToList();

        Console.WriteLine("davon Bildschirm: " + bildschirm.Count);
        Console.WriteLine("davon Fortschritt: "
            + (fehlend.Count - bildschirm.Count));

        foreach (var name in bildschirm)
        {
            Console.WriteLine("  Bildschirm " + name);
        }

        AssertTrue(fehlend.Count > 100,
            "**and more than a hundred state fields are not"
                + " saved** -- and the number is counted from the"
                + " sources and not estimated");

        AssertTrue(bildschirm.Count > 20,
            "**and more than twenty of them are screen state** --"
                + " and a screen that is showing a battle is not"
                + " the same thing as a party that is walking"
                + " home, and saving both in one flat list would"
                + " claim a completeness the codec does not"
                + " have");
    }
}
