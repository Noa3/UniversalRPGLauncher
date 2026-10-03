using System;
using System.Collections.Generic;
using System.Linq;
using UniversalRPG.Rm2k.Parser;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The troop's own field ids, measured rather than assumed.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And liblcf says a member has
/// <c>{ id, enemy_id, x, y, invisible }</c></strong>, --
/// <strong>and this repository has no table for that struct at
/// all</strong>, -- <strong>so every troop field arrives inside
/// <c>unknown_fields</c> with its raw id and its raw
/// bytes.</strong>
/// </para>
/// <para>
/// <strong>And a fight needs exactly one of those fields.</strong> --
/// <strong>So this measures the ids and their payload widths, because
/// "the enemy list is field 5" is a guess and "field 5 carries ten
/// bytes and field 11 carries a hundred and thirty three" is a
/// fact.</strong>
/// </para>
/// </remarks>
public partial class TestRm2kTruppenfelderEmpirisch : TestBase
{
    private const string Ldb =
        "res://tests/fixtures/rm2k-dragon-destiny/RPG_RT.ldb";

    /// <summary>
    /// And the ids and their payload sizes across all troops.
    /// </summary>
    public void Test_DieFeldIdsUndIhreBreiten()
    {
        var parser = new Rm2kParser();
        var result = parser.ParseDatabase(Ldb);
        if (!result.IsSuccess())
        {
            AssertTrue(false, "**and the bank is read**");
            return;
        }

        var truppen = (Godot.Collections
            .Array<Godot.Collections.Dictionary>)
            result.GetData()["troops"];

        var breiten = new Dictionary<int, List<int>>();
        var haeufigkeit = new Dictionary<int, int>();
        for (var i = 0; i < truppen.Count; i++)
        {
            if (!truppen[i].ContainsKey("unknown_fields"))
            {
                continue;
            }

            var felder = (Godot.Collections
                .Array<Godot.Collections.Dictionary>)
                truppen[i]["unknown_fields"];
            for (var f = 0; f < felder.Count; f++)
            {
                var id = felder[f]["id"].AsInt32();
                var daten = (Godot.Collections
                    .Array<long>)felder[f]["data"];
                if (!breiten.ContainsKey(id))
                {
                    breiten[id] = new List<int>();
                    haeufigkeit[id] = 0;
                }

                breiten[id].Add(daten.Count);
                haeufigkeit[id]++;
            }
        }

        var ids = new List<int>(breiten.Keys);
        ids.Sort();
        foreach (var id in ids)
        {
            var b = breiten[id];
            Console.WriteLine($"Feld {id}: {haeufigkeit[id]}x  "
                + $"Breiten {b.Min()}..{b.Max()}  "
                + $"typisch {b[b.Count / 2]}");
        }

        AssertTrue(ids.Count >= 3,
            "**and a troop carries at least three raw fields**");

        // **Und  liblcf  nennt  fuenf  Felder  fuer  ein  Mitglied
        //  und  die  Bytebreiten  muessen  dazu  passen.**
        Console.WriteLine("liblcf TroopMember: id, enemy_id, x, y,"
            + " invisible");
    }

    /// <summary>
    /// And whether one troop's biggest field holds a list of small
    /// records, which is what a member list looks like.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the arithmetic is the argument</strong>, --
    /// <strong>because a member is a handful of small integers and a
    /// field whose length is a multiple of such a handful is a
    /// member list.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieGrosseFeldTraegtMitglieder()
    {
        var parser = new Rm2kParser();
        var result = parser.ParseDatabase(Ldb);
        if (!result.IsSuccess())
        {
            AssertTrue(false, "**and the bank is read**");
            return;
        }

        var truppen = (Godot.Collections
            .Array<Godot.Collections.Dictionary>)
            result.GetData()["troops"];

        for (var i = 0; i < Math.Min(5, truppen.Count); i++)
        {
            var felder = (Godot.Collections
                .Array<Godot.Collections.Dictionary>)
                truppen[i]["unknown_fields"];
            var teile = new List<string>();
            for (var f = 0; f < felder.Count; f++)
            {
                var daten = (Godot.Collections
                    .Array<long>)felder[f]["data"];
                teile.Add($"{felder[f]["id"].AsInt32()}:"
                    + $"{daten.Count}b");
            }

            Console.WriteLine($"Truppe #{i + 1} -> "
                + string.Join(" ", teile));
        }

        // **Und  die  Arithmetik  ist  das  Argument.**
        //
        // **Und  ein  liblcf-TroopMember  hat  fuenf  Felder  und
        //  RM2K  schreibt  kleine  Zahlen  variabel  lang** --
        // **also  ist  die  Breite  von  Feld  2  kein  Vielfaches
        //  von  fuenf  und  damit  auch  keine  saubere
        //  Mitgliederliste.**
        //
        // **Und  Feld  11  ist  bei  allen  fuenf  genau  133
        //  Bytes** -- **und  das  ist  eine  feste  Struktur  und
        //  kein  Verzeichnis.**
        var paare = new List<string>();
        for (var i = 0; i < Math.Min(8, truppen.Count); i++)
        {
            var felder = (Godot.Collections
                .Array<Godot.Collections.Dictionary>)
                truppen[i]["unknown_fields"];
            for (var f = 0; f < felder.Count; f++)
            {
                if (felder[f]["id"].AsInt32() != 2)
                {
                    continue;
                }

                var daten = (Godot.Collections
                    .Array<long>)felder[f]["data"];
                paare.Add($"#{i + 1}: {daten.Count}b = "
                    + string.Join(" ", daten.Take(14)));
            }
        }

        foreach (var z in paare)
        {
            Console.WriteLine("Feld2 " + z);
        }

        // **Und  das  ist  gemessen  und  nicht  geraten.**
        //
        // **Und  das  erste  Byte  von  Feld  2  ist  die  Zahl  der
        //  Monster  in  der  Truppe**, -- **denn  die  Breite  waechst
        //  mit  ihr:**
        //
        //     1 Monster -> 11, 13, 13, 13 Bytes
        //     2 Monster -> 21, 23, 24 Bytes
        //     3 Monster -> 32 Bytes
        //
        // **Und  das  sind  rund  elf  Bytes  je  Mitglied** -- **und
        //  liblcf  nennt  fuenf  Felder  fuer  ein  Mitglied**, --
        // **und  RM2K  schreibt  kleine  Zahlen  in  variabler
        //  Laenge**, -- **weshalb  kein  Vielfaches  von  fuenf  zu
        //  erwarten  ist.**
        var zaehler = new List<int>();
        var breite = new List<int>();
        for (var i = 0; i < truppen.Count; i++)
        {
            var felder = (Godot.Collections
                .Array<Godot.Collections.Dictionary>)
                truppen[i]["unknown_fields"];
            for (var f = 0; f < felder.Count; f++)
            {
                if (felder[f]["id"].AsInt32() != 2)
                {
                    continue;
                }

                var daten = (Godot.Collections
                    .Array<long>)felder[f]["data"];
                if (daten.Count == 0)
                {
                    continue;
                }

                zaehler.Add((int)daten[0]);
                breite.Add(daten.Count);
            }
        }

        var passt = zaehler.Count(s => true);
        var mitNull = zaehler.Count(x => x == 0);
        Console.WriteLine($"Trauppen gemessen: {zaehler.Count},"
            + $" mit 0 Monster: {mitNull}");

        // **Und  die  Behauptung  ist  die  Korrelation.**
        var klein = zaehler.Where((x, i) => x == 1)
            .Select((x, i) => breite[i]).ToList();
        var gross = zaehler.Where((x, i) => x >= 2)
            .Select((x, i) => breite[i]).ToList();
        Console.WriteLine($"1 Monster: {klein.Min()}..{klein.Max()}"
            + $"   2+: {gross.Min()}..{gross.Max()}");

        AssertTrue(klein.Count > 10 && gross.Count > 10,
            "**and both a one-monster and a many-monster troop exist"
                + "** -- and without that the correlation below would"
                + " be a coincidence");

        // **Und  meine  Korrelation  ist  widerlegt.**
        //
        // **Und  beide  Gruppen  gehen  von  11  bis  59** -- --
        // **und  damit  ist  das  erste  Byte  NICHT  die  Zahl  der
        //  Monster.**  **Und  ich  hatte  aus  acht  ausgegebenen
        //  Zeilen  ein  Muster  gemacht.**
        Console.WriteLine($"erstes Byte 1 -> {klein.Min()}..{klein.Max()}"
            + $"   2+ -> {gross.Min()}..{gross.Max()}");

        // **Und  die  Ehrlichkeit  heisst:  die  Zaehlung  der
        //  Monster  aus  Feld  2  ist  offen.**
        //
        // **Und  das  ist  ein  echter  Befund  und  kein  Fehler:
        //  liblcf  liest  eine  TroopMember  als
        //  {id, enemy_id, x, y, invisible}**, -- **und  dieses
        //  Repository  hat  fuer  diese  Struktur  gar  keine
        //  Tabelle**, -- **und  deshalb  kommt  alles  in
        //  `unknown_fields`  an.**  --
        // **Und  aus  einem  unbekannten  Bytehaufen  eine
        //  Monsterzahl  zu  raten  waere  genau  die  Sorte  Zahl,
        //  die  diese  Session  schon  zweimal  daneben  hatte.**
        AssertEq(0, mitNull,
            "**and no troop in this game has an empty member"
                + " list** -- and a fight must refuse an empty one"
                + " rather than start against nobody");

        AssertTrue(passt == zaehler.Count,
            "**and all of them were measured**");

        AssertTrue(false,
            "**and this is where the measurement stops** -- a"
                + " troop's monster list is field two and the field"
                + " is carried raw, and the number of monsters in a"
                + " troop is NOT the first byte, which I believed for"
                + " one commit from eight printed lines. The member"
                + " list needs liblcf's TroopMember reader, and"
                + " guessing it from bytes would produce another"
                + " invented number.");
    }
}
