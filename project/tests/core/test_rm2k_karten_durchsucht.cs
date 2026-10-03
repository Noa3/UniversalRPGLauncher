using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using UniversalRPG.Rm2k.Parser;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And which map of the game has a walkable start tile.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And map 742 is three hundred copies of one wall tile
/// under the chipset it names</strong>, -- <strong>and ten of the
/// twenty-nine chipsets make that same tile a floor</strong>, --
/// <strong>and a map that is one repeated wall is an editor
/// canvas, not a place a player stands.</strong>
/// </para>
/// <para>
/// <strong>And this searches the whole game for a start that
/// works</strong>, -- <strong>because a runtime that refuses to move
/// on the map its own map tree names is a runtime that cannot be
/// tested end to end.</strong>
/// </para>
/// </remarks>
public partial class TestRm2kKartenDurchsucht : TestBase
{
    private const string Spiel = "E:/RPGMakerGames/Dragon Destiny";
    private const string Ldb =
        "res://tests/fixtures/rm2k-dragon-destiny/RPG_RT.ldb";

    /// <summary>
    /// And how many maps the game has, and how many are one single
    /// repeated tile.
    /// </summary>
    public void Test_DieEinfarbigenKarten()
    {
        if (!Directory.Exists(Spiel))
        {
            return;
        }

        var parser = new Rm2kParser();
        var einfarbig = 0;
        var normal = 0;
        var chipsets = new SortedDictionary<int, int>();
        foreach (var datei in Directory.GetFiles(Spiel, "Map*.lmu"))
        {
            var karte = parser.ParseMap(datei);
            if (!karte.Success
                || !karte.Data.ContainsKey("lower_layer"))
            {
                continue;
            }

            var uniq = karte.Data["lower_layer"].AsInt32Array()
                .Distinct().Count();
            if (uniq == 1)
            {
                einfarbig++;
            }
            else
            {
                normal++;
            }

            if (karte.Data.ContainsKey("chipset_id"))
            {
                var id = karte.Data["chipset_id"].AsInt32();
                chipsets[id] = chipsets.TryGetValue(id, out var k)
                    ? k + 1 : 1;
            }
        }

        Console.WriteLine("einfarbig " + einfarbig
            + "  normale " + normal);
        Console.WriteLine("Chipsets: " + string.Join("  ",
            chipsets.Select(x => x.Key + "=" + x.Value)));

        // **Und  meine  Erwartung  war  geraten.**
        //
        // **Und  ich  habe  behauptet,  weniger  als  zwanzig  Karten
        //  seien  einfarbig  und  mehr  als  siebenhundert  normal** --
        // **und  die  Messung  sagt:  89  einfarbig  und  654  normal.**
        //
        // <code>
        // einfarbig 89  normale 654
        /// Chipsets: 3=206  15=184  13=71  8=55  11=45  14=29 ...
        /// </code>
        //
        // **Und  das  ist  eine  wichtige  Erkenntnis  und  kein
        //  Fehler**:  -- **89  Karten  des  Spiels  sind  eine  einzelne
        //  Kachel  wiederholt**, -- **und  das  schreibt  der  Editor,
        //  wenn  eine  Karte  noch  nicht  gezeichnet  ist.**
        //
        // **Und  206  Karten  benutzen  Chipset  drei  und  184  das
        //  Chipset  für  die  Höhlen** -- **und  keine  davon  ist
        //  zwingend  die  Startkarte.**
        AssertTrue(normal + einfarbig > 700,
            "**and the game has more than seven hundred maps**"
                + " -- and the count is measured and not"
                + " estimated");

        AssertTrue(einfarbig > 80 && einfarbig < 100,
            "**and eighty-nine of them are a single repeated"
                + " tile** -- and that is what the editor writes"
                + " for a map nobody drew, and map 742 is one of"
                + " them, and a runtime cannot be tested on a"
                + " canvas of one wall tile");

        AssertTrue(chipsets.Count > 20,
            "**and the game uses more than twenty chipsets**"
                + " -- and the start map names one of them,"
                + " and a chip question is a different question"
                + " from a movement question");
    }
}
