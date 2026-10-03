using System;
using System.Collections.Generic;
using UniversalRPG.Rm2k.Parser;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The encounters, and what a troop names inside it.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this measures the last thing a fight needs</strong>,
/// -- <strong>because a troop is the list of monsters the game puts
/// into a battle, and the monster list is worthless without
/// it.</strong>
/// </para>
/// </remarks>
public partial class TestRm2kTruppenGemessen : TestBase
{
    private const string Ldb =
        "res://tests/fixtures/rm2k-dragon-destiny/RPG_RT.ldb";

    private static Godot.Collections.Dictionary Bank()
    {
        var parser = new Rm2kParser();
        var result = parser.ParseDatabase(Ldb);
        return result.IsSuccess()
            ? result.GetData()
            : new Godot.Collections.Dictionary();
    }

    /// <summary>
    /// And how many troops, and what the first one contains.
    /// </summary>
    public void Test_DieTruppenUndIhreErste()
    {
        var bank = Bank();
        if (!bank.ContainsKey("troops"))
        {
            AssertTrue(false,
                "**and the bank has a troops capsule** -- and it does"
                    + " not, and the sections above say which"
                    + " capsules came back: "
                    + string.Join(", ", NAMES(bank)));
            return;
        }

        var truppen = (Godot.Collections
            .Array<Godot.Collections.Dictionary>)bank["troops"];
        Console.WriteLine("Truppen: " + truppen.Count);

        for (var i = 0; i < Math.Min(3, truppen.Count); i++)
        {
            var feld = new List<string>();
            foreach (var k in truppen[i].Keys)
            {
                feld.Add(k.AsString());
            }

            feld.Sort(StringComparer.Ordinal);
            Console.WriteLine($"  #{i + 1}: "
                + string.Join(", ", feld));
        }

        AssertTrue(truppen.Count > 10,
            "**and the game has more than ten encounters**");
    }

    /// <summary>
    /// And whether a troop names real monsters.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the join.</strong> -- <strong>If a troop
    /// holds monster ids and the monster list holds those ids, a fight
    /// can be built from the game's own data.</strong>
    /// </para>
    /// </remarks>
    public void Test_EineTruppeNenntEchteMonster()
    {
        var bank = Bank();
        if (!bank.ContainsKey("troops")
            || !bank.ContainsKey("enemies"))
        {
            return;
        }

        var truppen = (Godot.Collections
            .Array<Godot.Collections.Dictionary>)bank["troops"];
        var monster = (Godot.Collections
            .Array<Godot.Collections.Dictionary>)bank["enemies"];
        if (truppen.Count == 0 || monster.Count == 0)
        {
            return;
        }

        var erste = truppen[0];
        Console.WriteLine("Truppe #1 roh: "
            + erste.ToString().Substring(0,
                Math.Min(240, erste.ToString().Length))
                .Replace("\n", " "));

        // **Und  die  Monsterliste  liegt  in  `unknown_fields`  und
        //  nicht  in  einem  benannten  Schluessel** -- --
        // **und  das  ist  der  ganze  Befund**:  der  Decoder  kennt
        //  die  Felder  einer  Truppe  nicht  und  traegt  sie
        //  deshalb  roh  weiter.
        var unbekannt = erste["unknown_fields"];
        Console.WriteLine("unknown_fields Typ: "
            + unbekannt.VariantType + "  Laenge: "
            + unbekannt.ToString().Length);
        Console.WriteLine("unknown_fields Anfang: "
            + unbekannt.ToString().Substring(0,
                Math.Min(400, unbekannt.ToString().Length))
                .Replace("\n", " "));

        AssertTrue(unbekannt.VariantType
                == Godot.Variant.Type.Array,
            "**and the troop's monster list is carried as an"
                + " array inside unknown_fields** -- and a decoder"
                + " that dropped it would leave 106 encounters with"
                + " nothing to fight");

        // **Und  die  Monsterliste  als  Menge  von  IDs.**
        var ids = new List<int>();
        for (var i = 0; i < monster.Count; i++)
        {
            ids.Add(monster[i]["id"].AsInt32());
        }

        Console.WriteLine("Monster-IDs: " + string.Join(",",
            ids.GetRange(0, Math.Min(10, ids.Count))));
        AssertTrue(ids.Contains(1),
            "**and monster id one exists**");
    }

    private static List<string> NAMES(
        Godot.Collections.Dictionary pBank)
    {
        var namen = new List<string>();
        foreach (var k in pBank.Keys)
        {
            namen.Add(k.AsString());
        }

        namen.Sort(StringComparer.Ordinal);
        return namen;
    }
}
