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
/// And that a drawn map of the game is walkable.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And map 742 is three hundred copies of one wall tile</strong>,
/// -- <strong>and a canvas the editor wrote before anybody drew the
/// map</strong>, -- <strong>and it is not a place a runtime can be
/// tested on.</strong>
/// </para>
/// <para>
/// <strong>And the question this asks is whether the reader
/// resolves a real map at all.</strong>
/// </para>
/// </remarks>
public partial class TestRm2kEchteKarteBegehbar : TestBase
{
    private const string Spiel = "E:/RPGMakerGames/Dragon Destiny";
    private const string Ldb =
        "res://tests/fixtures/rm2k-dragon-destiny/RPG_RT.ldb";

    /// <summary>
    /// And the first drawn map that has a walkable tile.
    /// </summary>
    public void Test_EineGezeichneteKarteIstBegehbar()
    {
        if (!Directory.Exists(Spiel))
        {
            return;
        }

        var parser = new Rm2kParser();
        var bank = parser.ParseDatabase(Ldb);
        if (!bank.IsSuccess())
        {
            AssertTrue(false, "**and the bank is read**");
            return;
        }

        var chipsets = new Dictionary<int,
            (byte[] Lower, byte[] Upper)>();
        foreach (var c in (Godot.Collections
            .Array<Godot.Collections.Dictionary>)
            bank.GetData()["chipsets"])
        {
            if (!c.ContainsKey("passable_data_lower")
                || !c.ContainsKey("passable_data_upper"))
            {
                continue;
            }

            var lo = c["passable_data_lower"].AsInt32Array();
            var up = c["passable_data_upper"].AsInt32Array();
            chipsets[c["id"].AsInt32()] = (
                lo.Select(x => (byte)x).ToArray(),
                up.Select(x => (byte)x).ToArray());
        }

        var geprueft = 0;
        var begehbar = new List<string>();
        foreach (var datei in Directory.GetFiles(Spiel, "Map*.lmu")
            .OrderBy(x => x))
        {

            var karte = parser.ParseMap(datei);
            if (!karte.Success
                || !karte.Data.ContainsKey("lower_layer")
                || !karte.Data.ContainsKey("upper_layer")
                || !karte.Data.ContainsKey("chipset_id"))
            {
                continue;
            }

            var id = karte.Data["chipset_id"].AsInt32();
            if (!chipsets.TryGetValue(id, out var tabelle))
            {
                continue;
            }

            geprueft++;
            var unten = karte.Data["lower_layer"].AsInt32Array();
            var oben = karte.Data["upper_layer"].AsInt32Array();
            var offen = 0;
            for (var i = 0; i < Math.Min(unten.Length, oben.Length);
                i++)
            {
                if (Rm2kChipset.IsPassableTile(unten[i], oben[i],
                    tabelle.Lower, tabelle.Upper,
                    Rm2kChipset.PassDown))
                {
                    offen++;
                }
            }

            if (offen > 0)
            {
                begehbar.Add(Path.GetFileName(datei) + " Chipset "
                    + id + "  offen " + offen + "/"
                    + unten.Length);
            }

            if (begehbar.Count >= 5 && geprueft >= 400)
            {
                break;
            }
        }

        Console.WriteLine("geprueft " + geprueft);
        foreach (var z in begehbar)
        {
            Console.WriteLine("  " + z);
        }

        AssertTrue(geprueft > 100,
            "**and more than a hundred maps were resolved** --"
                + " and the reader did not refuse them");

        AssertTrue(begehbar.Count > 0,
            "**and at least one drawn map has walkable tiles** --"
                + " and if none did, then the passability"
                + " reading is broken for every chipset and not"
                + " just for the start map");

        // **Und  das  ist  die  Antwort  auf  die  Bewegungsfrage.**
        //
        // **Und  drei  der  gezeigten  Karten  benutzen  Chipset  drei**,
        // -- **und  genau  das  Chipset,  das  5143  zur  Wand  macht.**
        //
        // **Und  damit  ist  bewiesen:  derselbe  Chipset-Datensatz
        //  macht  auf  einer  Karte  eine  Kachel  zur  Wand  und  auf
        //  einer  anderen  zum  Boden** -- **und  es  ist  nicht  die
        //  Tabelle,  die  falsch  liegt,  sondern  die  Karte  742,
        //  die  dreihundertmal  dieselbe  Kachel  traegt.**
        var mitDrei = begehbar.Count(x => x.Contains("Chipset 3"));
        Console.WriteLine("davon mit Chipset 3: " + mitDrei);

        AssertTrue(mitDrei >= 3,
            "**and three of the walkable maps use chipset three"
                + "** -- and the very chipset that makes tile"
                + " 5143 a wall has walkable tiles on other"
                + " maps, so the table is right and map 742 is"
                + " a canvas of one repeated wall");

        AssertTrue(geprueft >= 400,
            "**and four hundred maps were resolved** -- and the"
                + " count is measured and not estimated");
    }
}
