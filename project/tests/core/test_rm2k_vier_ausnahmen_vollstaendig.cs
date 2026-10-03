using System;
using System.Collections.Generic;
using System.Linq;
using UniversalRPG.Rm2k.Parser;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The four troops that do not decode, and all of their bytes.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And my explanation for them was wrong.</strong> --
/// <strong>I said the third byte distinguishes them and troop five
/// disproved that</strong>, -- <strong>and the honest way out of a
/// wrong explanation is to print the whole thing.</strong>
/// </para>
/// </remarks>
public partial class TestRm2kVierAusnahmenVollstaendig : TestBase
{
    private const string Ldb =
        "res://tests/fixtures/rm2k-dragon-destiny/RPG_RT.ldb";

    /// <summary>
    /// And the complete bytes of the ones that fail next to the
    /// complete bytes of one that works.
    /// </summary>
    public void Test_DieVierUndEinVergleich()
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

        var fehler = new List<string>();
        var gut = 0;
        var vergleich = "";
        for (var i = 0; i < truppen.Count; i++)
        {
            var bytes = Bytes(truppen[i]);
            if (bytes == null)
            {
                continue;
            }

            var ok = Rm2kTroopMemberDecoder.TryDecode(
                bytes, out var mitglieder, out var warum);
            if (ok)
            {
                gut++;
                if (vergleich == "")
                {
                    vergleich = "gut #" + (i + 1) + " ("
                        + mitglieder.Count + "): "
                        + string.Join(",", bytes);
                }

                continue;
            }

            fehler.Add("FEHL #" + (i + 1) + " \""
                + truppen[i]["name"].AsString() + "\" "
                + bytes.Length + "b: " + string.Join(",", bytes)
                + "\n      -> " + warum);
        }

        Console.WriteLine(vergleich);
        foreach (var f in fehler)
        {
            Console.WriteLine(f);
        }

        Console.WriteLine($"gut {gut}  fehler {fehler.Count}");

        // **Und  der  Vergleich  von  oben  ist  die  ganze
        //  Erklaerung**:
        //
        //     gut #2: 1,1,1,1,2,2,2,129,32,3,1,116,0
        //     FEHL #1: 1,1,2,2,129,32,3,2,129,5,0
        //
        // **Und  nach  der  Objekt-ID  hat  gut  #2  ein  Feld
        //  `0x01`  --  und  FEHL  #1  springt  direkt  auf
        //  `0x02`.**
        //
        // **Und  `0x01`  ist  liblcfs  `TroopMember.enemy_id`.**  --
        // **Und  diese  vier  Truppen  haben  Mitglieder  OHNE
        //  enemy_id.**
        //
        // **Und  das  ist  kein  Decoder-Fehler  und  keine
        //  Spielentscheidung  --  es  ist  eine  offene  Frage
        //  ueber  das  Format**, -- **denn  die  Namen  sagen  das
        //  Gegenteil:**
        //
        //     #3 "Desert Oozex2"      -> zwei Monster
        //     #5 "Scorpion,Desert Ooze" -> zwei Monster
        //
        // **Und  eine  Trupe  mit  zwei  Namen  und  keinem
        //  enemy_id  ist  eine  Datei,  die  etwas  sagt,  was  wir
        //  noch  nicht  lesen.**
        AssertEq(4, fehler.Count,
            "**and exactly four troops do not decode** -- and all"
                + " four carry members without liblcf's enemy_id"
                + " field 0x01, while their names name one to three"
                + " monsters, and that contradiction is the open"
                + " question rather than a decoder fault");
    }

    /// <summary>
    /// And whether the four share a shape the hundred and two do not.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this asks the question without answering
    /// it</strong>, -- <strong>because a shape that all four share and
    /// none of the others has is a fact, and a shape I recognise is a
    /// guess.</strong>
    /// </para>
    /// </remarks>
    public void Test_HabenDieVierEtwasGemeinsam()
    {
        var parser = new Rm2kParser();
        var result = parser.ParseDatabase(Ldb);
        if (!result.IsSuccess())
        {
            return;
        }

        var truppen = (Godot.Collections
            .Array<Godot.Collections.Dictionary>)
            result.GetData()["troops"];

        // **Und  jede  Trupe  hat  ausser  Feld  2  noch  Feld  5
        //  und  Feld  11**, -- **und  Feld  5  ist  die,  deren
        //  Breite  zwischen  0  und  10  schwankte.**
        var ohneFeld5 = new List<string>();
        var breiten5 = new Dictionary<string, List<int>>();
        for (var i = 0; i < truppen.Count; i++)
        {
            var bytes = Bytes(truppen[i]);
            if (bytes == null)
            {
                continue;
            }

            var ok = Rm2kTroopMemberDecoder.TryDecode(
                bytes, out _, out _);
            var b5 = FeldBreite(truppen[i], 0x05);
            var schluessel = ok ? "gut" : "FEHL";
            if (!breiten5.ContainsKey(schluessel))
            {
                breiten5[schluessel] = new List<int>();
            }

            breiten5[schluessel].Add(b5);
            if (b5 == 0)
            {
                ohneFeld5.Add((ok ? "gut" : "FEHL") + " #" + (i + 1));
            }
        }

        foreach (var kv in breiten5)
        {
            Console.WriteLine(kv.Key + ": Feld5 "
                + kv.Value.Count + "x, breiten "
                + string.Join(",", kv.Value.Take(12)));
        }

        Console.WriteLine("Feld5 = 0 bei " + ohneFeld5.Count + ": "
            + string.Join(", ", ohneFeld5.Take(12)));

        AssertTrue(breiten5.Count == 2,
            "**and there are two groups to compare**");
    }

    private static int FeldBreite(
        Godot.Collections.Dictionary pTruppe, int pId)
    {
        var felder = (Godot.Collections
            .Array<Godot.Collections.Dictionary>)
            pTruppe["unknown_fields"];
        foreach (var f in felder)
        {
            if (f["id"].AsInt32() == pId)
            {
                return ((byte[])f["data"]).Length;
            }
        }

        return -1;
    }

    private static byte[]? Bytes(
        Godot.Collections.Dictionary pTruppe)
    {
        var felder = (Godot.Collections
            .Array<Godot.Collections.Dictionary>)
            pTruppe["unknown_fields"];
        foreach (var f in felder)
        {
            if (f["id"].AsInt32() == 0x02)
            {
                return (byte[])f["data"];
            }
        }

        return null;
    }
}
