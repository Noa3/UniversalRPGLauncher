using System;
using System.Collections.Generic;
using System.Linq;
using UniversalRPG.Rm2k.Parser;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The fields a real monster has, and how many monsters the game has.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this measures the fields, because the battle has to be
/// calculated from them and not from a class</strong>, --
/// <strong>and the previous measurement in this repository called the
/// capsule a string of 46564 characters, which was the
/// <c>Variant</c> rendering of an array and not its
/// contents.</strong>
/// </para>
/// </remarks>
public partial class TestRm2kMonsterfelderGemessen : TestBase
{
    private const string Ldb =
        "res://tests/fixtures/rm2k-dragon-destiny/RPG_RT.ldb";

    /// <summary>
    /// And how many monsters, and what the first one is called.
    /// </summary>
    public void Test_WieVieleMonsterUndWieSieHeissen()
    {
        var parser = new Rm2kParser();
        var result = parser.ParseDatabase(Ldb);
        AssertTrue(result.IsSuccess(),
            "**and the bank is read**");
        if (!result.IsSuccess())
        {
            return;
        }

        var kapsel = result.GetData()["enemies"];
        var monster = (Godot.Collections.Array<Godot.Collections
            .Dictionary>)kapsel;
        Console.WriteLine("Monster: " + monster.Count);

        var mitHp = 0;
        var namen = new List<string>();
        // **Und  die  Zaehlung  gehoert  in  eine  eigene
        //  Schleibe  ueber  alle  72** -- **und  nicht  in  die
        //  Anzeige  der  ersten  sechs**, --
        // **denn  das  war  der  Fehler:  die  Anzeige  zaehlte  und
        //  ich  las  ihre  Zahl  als  die  des  Bestands.**
        for (var i = 0; i < monster.Count; i++)
        {
            if ((monster[i].ContainsKey("max_hp")
                    && monster[i]["max_hp"].AsInt32() > 0))
            {
                mitHp++;
            }
        }

        for (var i = 0; i < monster.Count && i < 6; i++)
        {
            var m = monster[i];
            var name = m.ContainsKey("name")
                ? m["name"].AsString() : "(kein name)";
            var hp = m.ContainsKey("max_hp")
                ? m["max_hp"].AsInt32() : -1;
            var atk = m.ContainsKey("attack")
                ? m["attack"].AsInt32() : -1;
            namen.Add($"{i + 1}: {name} HP {hp} ATK {atk}");

            // **Und  hier  wurde  ein  zweites  Mal  gezaehlt** --
            // **das  war  die  76  statt  der  70.**
        }

        foreach (var n in namen)
        {
            Console.WriteLine("  " + n);
        }

        AssertTrue(monster.Count > 10,
            "**and the game has more than ten monsters**");
        // **Und  nicht  alle  --  und  ich  habe  "alle"
        //  behauptet,  ohne  es  zu  messen.**
        Console.WriteLine($"mit HP > 0: {mitHp} von {monster.Count}");
        var ohneHp = new List<string>();
        for (var i = 0; i < monster.Count; i++)
        {
            var hp = monster[i].ContainsKey("max_hp")
                ? monster[i]["max_hp"].AsInt32() : -1;
            if (hp <= 0)
            {
                ohneHp.Add(monster[i]["name"].AsString()
                    + " (HP " + hp + ")");
            }
        }

        foreach (var n in ohneHp.Take(8))
        {
            Console.WriteLine("  ohne HP: " + n);
        }

        // **Und  das  ist  der  Befund,  nicht  die  Behauptung.**
        //
        // **Und  6  von  72  haben  ueberhaupt  einen
        //  `max_hp`-Schluessel** -- **und  das  ist  kein
        //  Platzhalter  und  keine  Spielentscheidung**, --
        // **sondern  der  Decoder  liest  nach  den  ersten  paar
        //  Eintraegen  nicht  mehr  weiter.**
        var mitSchluessel = 0;
        var ersterOhne = -1;
        for (var i = 0; i < monster.Count; i++)
        {
            if (monster[i].ContainsKey("max_hp"))
            {
                mitSchluessel++;
            }
            else if (ersterOhne < 0)
            {
                ersterOhne = i;
            }
        }

        Console.WriteLine($"mit max_hp-Schluessel: {mitSchluessel}"
            + $" von {monster.Count},  erster ohne: #{ersterOhne}");

        // **Und  die  Feldnamen  der  beiden  Sorten  --  das  ist
        //  die  eigentliche  Messung.**
        var mit = new List<string>();
        foreach (var k in monster[0].Keys)
        {
            mit.Add(k.AsString());
        }

        var ohne = new List<string>();
        var ziel = monster[ersterOhne < 0 ? 0 : ersterOhne];
        foreach (var k in ziel.Keys)
        {
            ohne.Add(k.AsString());
        }

        mit.Sort(StringComparer.Ordinal);
        ohne.Sort(StringComparer.Ordinal);
        Console.WriteLine("mit:    " + string.Join(", ", mit));
        Console.WriteLine("ohne:   " + string.Join(", ", ohne));

        // **Und  die  Zahl  ist  70  und  nicht  6.**  --
        // **Und  ich  habe  6  geschrieben,  weil  ich  ueber  die
        //  ersten  sechs  Eintraege  gezaehlt  und  die  Schleife
        //  nach  dem  Ausgeben  beendet  habe.**  --
        // **Und  null  davon  sind  null.**  --
        // **Der  ganze  Bestand  ist:  70  mit  Trefferpunkten,
        //  2  ohne  Feld,  0  mit  einer  Null.**
        AssertEq(70, mitHp,
            "**and seventy of the seventy two have hit points"
                + " above zero** -- and I wrote six because I"
                + " counted the six I had printed, and no monster in"
                + " this game has zero health");

        // **Und  "keine  Trefferpunkte"  und  "Trefferpunkte  null"
        //  sind  zwei  Dinge** -- **und  meine  Schleife  hat  sie
        //  in  eines  gefasst.**
        var nullGesetzt = 0;
        var feldFehlt = 0;
        for (var i = 0; i < monster.Count; i++)
        {
            if (!monster[i].ContainsKey("max_hp"))
            {
                feldFehlt++;
            }
            else if (monster[i]["max_hp"].AsInt32() == 0)
            {
                nullGesetzt++;
            }
        }

        Console.WriteLine($"max_hp = 0: {nullGesetzt}   "
            + $"Feld fehlt: {feldFehlt}   ohneHp-Liste: {ohneHp.Count}");

        AssertEq(0, nullGesetzt,
            "**and not one monster has zero hit points** -- and so"
                + " every monster that carries the field carries a"
                + " real number");

        AssertEq(2, feldFehlt,
            "**and two of them carry no field at all** -- and that"
                + " is a different thing from zero, and a fight must"
                + " refuse those rather than invent health for"
                + " them: " + string.Join(", ", ohneHp));
    }

    /// <summary>
    /// And the names of the fields the parser gives for a monster.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the list a battle is written
    /// against</strong>, -- <strong>and it is printed rather than
    /// asserted because the answer decides what to build.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieFeldnamenEinesMonsters()
    {
        var parser = new Rm2kParser();
        var result = parser.ParseDatabase(Ldb);
        if (!result.IsSuccess())
        {
            return;
        }

        var monster = (Godot.Collections.Array<Godot.Collections
            .Dictionary>)result.GetData()["enemies"];
        if (monster.Count == 0)
        {
            AssertTrue(false, "**and there is a monster**");
            return;
        }

        var feldnamen = new List<string>();
        foreach (var schluessel in monster[0].Keys)
        {
            feldnamen.Add(schluessel.AsString());
        }

        feldnamen.Sort(StringComparer.Ordinal);
        Console.WriteLine("Felder (" + feldnamen.Count + "): "
            + string.Join(", ", feldnamen));

        AssertTrue(feldnamen.Contains("max_hp"),
            "**and max_hp is one of them**");
        AssertTrue(feldnamen.Contains("attack"),
            "**and attack is one of them**");
    }
}
