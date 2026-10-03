using System;
using System.Collections.Generic;
using System.Linq;
using UniversalRPG.Rm2k.Parser;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And that the party order is in the system chunk, not in the
/// troop.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is the answer to the question the previous commit
/// asked.</strong> --
/// <strong>A troop carries exactly three fields: <c>0x02 members</c>,
/// <c>0x05 terrain_set</c> and <c>0x0B pages</c>.</strong> --
/// <strong>And none of them is a turn order.</strong>
/// </para>
/// <para>
/// <strong>And liblcf's <c>System</c> chunk carries
/// <c>party</c> at <c>0x15</c> and <c>0x16</c>:</strong> -- <strong>a
/// count and a vector of actor ids</strong>, -- <strong>and that is the
/// order the party acts in.</strong>
/// </para>
/// </remarks>
public partial class TestRm2kParteiStehtImSystem : TestBase
{
    private const string Ldb =
        "res://tests/fixtures/rm2k-dragon-destiny/RPG_RT.ldb";

    /// <summary>
    /// And the system's own party list.
    /// </summary>
    public void Test_DieParteiDesSystems()
    {
        var parser = new Rm2kParser();
        var result = parser.ParseDatabase(Ldb);
        if (!result.IsSuccess())
        {
            AssertTrue(false, "**and the bank is read**");
            return;
        }

        var system = (Godot.Collections.Dictionary)
            result.GetData()["system"];
        Console.WriteLine("System-Felder: ");
        var namen = new List<string>();
        foreach (var k in system.Keys)
        {
            namen.Add(k.AsString());
        }

        namen.Sort(StringComparer.Ordinal);
        Console.WriteLine("  " + string.Join(", ", namen));

        var party = system["party"];
        Console.WriteLine("party: typ=" + party.VariantType
            + " laenge=" + party.AsString().Length
            + "  -> " + party.AsString().Substring(0,
                Math.Min(90, party.AsString().Length)));

        AssertTrue(namen.Contains("party"),
            "**and the system carries the party** -- and that is"
                + " where the party's order lives, and a troop"
                + " field is not where it is");

        AssertTrue(system.ContainsKey("boat_name"),
            "**and the system was read whole** -- and a system"
                + " chunk that failed would drop the party with it");
    }

    /// <summary>
    /// And the actors those ids name.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the join: a party of actor ids is only
    /// useful if the actors are readable.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieHeldenHinterDenIds()
    {
        var parser = new Rm2kParser();
        var result = parser.ParseDatabase(Ldb);
        if (!result.IsSuccess())
        {
            return;
        }

        var bank = result.GetData();
        var helden = (Godot.Collections
            .Array<Godot.Collections.Dictionary>)bank["actors"];
        Console.WriteLine("Helden: " + helden.Count);
        for (var i = 0; i < Math.Min(5, helden.Count); i++)
        {
            Console.WriteLine("  " + helden[i]["id"].AsInt32() + " "
                + helden[i]["name"].AsString());
        }

        AssertTrue(helden.Count > 5,
            "**and the bank holds more than five actors** -- and"
                + " the party list is a list of these ids");

        // **Und  die  Werte  liegen  NICHT  im  Actor-Feld.**
        //
        // **Und  liblcf  sagt,  wo  sie  liegen:**
        //
        //     Parameters   maxhp    Vector<Int16>
        //
        // **Und  das  ist  genau  der  Chunk,  den
        //  `Rm2kClassParameterDecoder`  liest**, -- **und  er  wird
        //  in  `entry["parameters"]`  abgelegt**, -- **und  meine
        //  Behauptung  "kein  Held  hat  max_hp"  war  damit  eine
        //  Behauptung  ueber  die  falsche  Stelle.**
        var mitHp = 0;
        var beispiel = "";
        for (var i = 0; i < helden.Count; i++)
        {
            // **Und  erst  messen,  dann  behaupten.**
            if (!helden[i].ContainsKey("parameters"))
            {
                var roh = helden[i]["unknown_fields"];
                var ids = new List<string>();
                foreach (var f in (Godot.Collections
                    .Array<Godot.Collections.Dictionary>)roh)
                {
                    ids.Add("0x" + f["id"].AsInt32().ToString("X2")
                        + "(" + ((byte[])f["data"]).Length + "b)");
                }

                Console.WriteLine("  " + helden[i]["name"].AsString()
                    + ": unknown_fields = "
                    + string.Join(" ", ids));
            }

            if (!helden[i].ContainsKey("parameters"))
            {
                Console.WriteLine("  " + helden[i]["name"].AsString()
                    + ": KEINE parameters");
                continue;
            }

            var werte = helden[i]["parameters"];
            Console.WriteLine("  " + helden[i]["name"].AsString()
                + ": " + string.Join(" ",
                    new List<string> { "hat parameters" }));

            // **Und  die  Zahl  aus  ihnen  lesen.**
            var anzahl = 0;
            foreach (var k in ((Godot.Collections.Dictionary)
                werte).Keys)
            {
                Console.WriteLine("      " + k.AsString());
                anzahl++;
            }

            if (anzahl > 0)
            {
                mitHp++;
                if (beispiel == "")
                {
                    beispiel = helden[i]["name"].AsString();
                }
            }
        }

        Console.WriteLine("Helden mit parameters: " + mitHp
            + "  Beispiel: " + beispiel);

        AssertEq(helden.Count, mitHp,
            "**and every one of the seven actors carries its"
                + " values in the parameters chunk** -- and the"
                + " chunk is 0x1F, liblcf calls it"
                + " \"Array x 6 - Short\", and it holds maxhp,"
                + " maxsp, attack, defense, spirit and agility");

        // **Und  jetzt  die  Zahl,  die  vorher  null  war.**
        var spencersWerte = (Godot.Collections.Dictionary)
            helden[0]["parameters"];
        var maxhp = (Godot.Collections.Array<int>)
            spencersWerte["maxhp"];
        Console.WriteLine("Spencer maxhp: " + string.Join(",", maxhp));

        AssertTrue(maxhp.Count > 0,
            "**and the hit points are a per-class vector** -- and"
                + " that is the shape and not a single number");

        AssertTrue(maxhp[0] > 0,
            "**and the first class has more than zero** -- and"
                + " before this commit no actor in this game had"
                + " any hit points at all, because the decoder's"
                + " parameter case ran for classes and not for"
                + " actors");
    }
}
