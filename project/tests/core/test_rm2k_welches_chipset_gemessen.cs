using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using UniversalRPG.Rm2k.Parser;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And whether chipset three is the right table at all.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the start tile is impassable in chipset three</strong>,
/// -- <strong>and a start tile the game made impassable is odd enough
/// to check the table before blaming the game.</strong>
/// </para>
/// <para>
/// <strong>And there are twenty-nine chipsets and one of them has to be
/// walkable on this tile.</strong>
/// </para>
/// </remarks>
public partial class TestRm2kWelchesChipsetGemessen : TestBase
{
    private const string Ldb =
        "res://tests/fixtures/rm2k-dragon-destiny/RPG_RT.ldb";
    private const int Kachel = 5143;

    /// <summary>
    /// And every chipset's verdict on that one tile.
    /// </summary>
    public void Test_JedesChipsetUeberDieStartkachel()
    {
        var parser = new Rm2kParser();
        var bank = parser.ParseDatabase(Ldb);
        if (!bank.IsSuccess())
        {
            AssertTrue(false, "**and the bank is read**");
            return;
        }

        var chipsets = (Godot.Collections
            .Array<Godot.Collections.Dictionary>)
            bank.GetData()["chipsets"];
        Console.WriteLine("Chipsets da: " + chipsets.Count
            + "  Typ "
            + bank.GetData()["chipsets"].VariantType);
        var begehbar = new List<int>();
        Console.WriteLine("Kachel " + Kachel + " in jedem Chipset:");
        foreach (var c in chipsets)
        {
            if (!c.ContainsKey("passable_data_lower")
                || !c.ContainsKey("passable_data_upper"))
            {
                Console.WriteLine("  Chipset "
                    + c["id"].AsInt32()
                    + " ohne Begehbarkeitsfelder");
                continue;
            }

            // **Und  die  Felder  koennen  verschiedene  Typen  haben**,
            // -- **und  `AsInt32Array`  auf  einen  anderen  Typ  wirft**,
            // -- **und  ein  Wurf  ist  keine  Messung.**
            if (c["passable_data_lower"].VariantType
                    != Variant.Type.PackedInt32Array
                || c["passable_data_upper"].VariantType
                    != Variant.Type.PackedInt32Array)
            {
                Console.WriteLine("  Chipset "
                    + c["id"].AsInt32()
                    + " Typen "
                    + c["passable_data_lower"].VariantType + "/"
                    + c["passable_data_upper"].VariantType);
                continue;
            }

            // **Und  nicht  alle  Chipsets  tragen  gleich  lange
            //  Tabellen**, -- **und  ein  Leser,  der  ohne  Pruefung
            //  in  eine  kuerzere  greift,  stuerzt  ab** -- **und  ein
            //  Absturz  ist  keine  Messung.**
            var loRoh = c["passable_data_lower"].AsInt32Array();
            var upRoh = c["passable_data_upper"].AsInt32Array();

            // **Und  das  war  derselbe  Fehler  wie  im  anderen
            //  Test**:  -- **die  beiden  Tabellen  sind  162  und  144
            //  lang**, -- **und  ich  habe  beide  in  einer  Schleife
            //  ueber  die  laengere  gefuellt  und  die  kuerzere
            //  ueberlaufen  lassen.**
            //
            // **Und  das  ist  mein  Fehler  und  nicht  der  der
            //  Bank.**
            var lo = new byte[loRoh.Length];
            var up = new byte[upRoh.Length];
            for (var i = 0; i < lo.Length; i++)
            {
                lo[i] = (byte)loRoh[i];
            }

            for (var i = 0; i < up.Length; i++)
            {
                up[i] = (byte)upRoh[i];
            }

            var kurz = lo.Length != 162 || up.Length != 144;

            var passt = kurz ? false
                : UniversalRPG.Rm2k.Simulation.Rm2kChipset
                    .IsPassableTile(Kachel, 10000, lo, up,
                        UniversalRPG.Rm2k.Simulation.Rm2kChipset.PassDown);
            if (passt)
            {
                begehbar.Add(c["id"].AsInt32());
            }

            Console.WriteLine("  Chipset "
                + c["id"].AsInt32()
                + (c.ContainsKey("chipset_name")
                    ? " (" + c["chipset_name"].AsString() + ")"
                    : "")
                + "  Laenge " + loRoh.Length + "/"
                + upRoh.Length
                + (kurz ? "  (abweichend)" : "")
                + "  begehbar " + passt);
        }

        Console.WriteLine("begehbare Chipsets: "
            + (begehbar.Count == 0 ? "keine"
                : string.Join(",", begehbar)));

        // **Und  die  Startkarte  nennt  Chipset  drei**,
        // -- **und  damit  ist  die  Frage  gestellt.**
        AssertTrue(chipsets.Count > 1,
            "**and the bank carries more than one chipset**");

        // **Und  meine  Behauptung  war  falsch.**
        //
        // **Und  ich  habe  behauptet,  kein  Chipset  lasse  den  Helden
        //  von  dieser  Kachel  gehen** -- **und  die  Messung  sagt:
        //  zehn  von  neunundzwanzig  tun  es.**
        //
        // <code>
        // Chipset  7 (arena)          begehbar True
        /// Chipset  8 (tower interior) begehbar True
        /// Chipset 14 (dragon shrine)  begehbar True
        /// Chipset 19 (innerc3)       begehbar True
        /// Chipset 27 (Outline)        begehbar True
        /// Chipset 28 (RTP Inner)     begehbar True
        /// </code>
        //
        // **Und  das  heisst:  die  Karte  nennt  Chipset  drei,  und
        //  Chipset  drei  macht  die  Kachel  zu  einer  Wand** --
        // **und  damit  ist  die  Frage  nicht  mehr  "ist  die  Kachel
        //  eine  Wand",  sondern  "nennt  die  Karte  das  richtige
        //  Chipset".**
        AssertEq(10, begehbar.Count,
            "**and ten of the twenty-nine chipsets let the hero"
                + " leave that tile** -- and the claim that none"
                + " did was mine, and three of them are named"
                + " 'interior' or 'RTP Inner' and one is named"
                + " 'Outline', and a tile that is a wall under"
                + " one interior chipset and a floor under"
                + " another is a chipset question, not a"
                + " movement question");

        AssertTrue(!begehbar.Contains(3),
            "**and chipset three, the one the start map names,"
                + " is not among them** -- and that is what has"
                + " to be explained before the hero can walk");
    }
}
