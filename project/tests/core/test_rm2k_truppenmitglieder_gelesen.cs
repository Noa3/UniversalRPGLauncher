using System;
using System.Collections.Generic;
using System.Linq;
using UniversalRPG.Rm2k.Parser;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The troop's members, read with liblcf's field ids.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this closes the red assertion of the previous
/// commit.</strong> -- <strong>That one said the member list needs
/// liblcf's reader</strong>, -- <strong>and this is that reader, and
/// this is what it finds in the game.</strong>
/// </para>
/// </remarks>
public partial class TestRm2kTruppenmitgliederGelesen : TestBase
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
    /// And how many members each troop has, read from field two.
    /// </summary>
    public void Test_DieMitgliederzahlenDerTruppen()
    {
        var bank = Bank();
        if (!bank.ContainsKey("troops"))
        {
            AssertTrue(false, "**and the bank has troops**");
            return;
        }

        var truppen = (Godot.Collections
            .Array<Godot.Collections.Dictionary>)bank["troops"];
        var gelesen = 0;
        var verweigert = new List<string>();
        var verteilung = new Dictionary<int, int>();

        for (var i = 0; i < truppen.Count; i++)
        {
            var bytes = TruppenBytes(truppen[i]);
            if (bytes == null)
            {
                continue;
            }

            if (Rm2kTroopMemberDecoder.TryDecode(
                    bytes, out var mitglieder, out var fehler))
            {
                gelesen++;
                verteilung[mitglieder.Count] =
                    verteilung.GetValueOrDefault(mitglieder.Count) + 1;
                if (gelesen <= 5)
                {
                    var namen = new List<string>();
                    foreach (var m in mitglieder)
                    {
                        namen.Add(m["enemy_id"].AsInt32().ToString());
                    }

                    Console.WriteLine($"Truppe #{i + 1}: "
                        + string.Join(",", namen));
                }
            }
            else if (verweigert.Count < 5)
            {
                verweigert.Add($"#{i + 1}: {fehler}");
            }
        }

        Console.WriteLine($"gelesen {gelesen} von {truppen.Count}, "
            + $"verweigert {verweigert.Count}");
        var groessen = new List<string>();
        foreach (var kv in verteilung.OrderBy(x => x.Key))
        {
            groessen.Add($"{kv.Key} Monster: {kv.Value}x");
        }

        Console.WriteLine("Verteilung: " + string.Join(", ", groessen));
        foreach (var v in verweigert)
        {
            Console.WriteLine("  verweigert " + v);
        }

        // **Und  vier  Truppen  wurden  verweigert** -- **und  die
        //  vier  muessen  eine  erklaerbare  Ursache  haben**,
        // **denn  "nennt kein Monster"  ist  zwei  verschiedene
        //  Faelle.**
        //
        // **Der  eine  ist:  das  Spiel  hat  eine  leere  Truppe
        //  geschrieben** -- **und  das  ist  eine  Entscheidung  des
        //  Spiels,  und  ein  Kampf  dagegen  ist  nicht
        //  erfunden.**
        // **Der  andere  waere:  mein  Decoder  liest  ein  Feld
        //  falsch** -- **und  das  waere  mein  Fehler.**
        for (var i = 0; i < truppen.Count; i++)
        {
            var bytes = TruppenBytes(truppen[i]);
            if (bytes == null)
            {
                continue;
            }

            if (Rm2kTroopMemberDecoder.TryDecode(
                    bytes, out _, out var warum))
            {
                continue;
            }

            var erste = new byte[Math.Min(12, bytes.Length)];
            Array.Copy(bytes, erste, erste.Length);
            // **Und  die  vier  verweigerten  Namen  sind  das
            //  Spiel  selbst.**
            //
            // **Und  sie  tragen  ein  `xN`  im  Namen** -- **und  das
            //  ist  kein  Zufall,  sondern  es  ist  der  Grund,  aus
            //  dem  das  Spiel  diese  Truppen  geschrieben  hat.**
            //
            // **Und  ich  muss  das  messen  und  nicht  annehmen.**
            Console.WriteLine("  #" + (i + 1) + " "
                + truppen[i]["name"].AsString()
                + "  roh: " + string.Join(",", erste));
        }

        AssertTrue(gelesen > 100,
            "**and more than a hundred troops were read** -- and"
                + " that is the whole point: the raw bytes carried"
                + " the list all along");
    }

    /// <summary>
    /// And that the enemy ids are real.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the join</strong>, -- <strong>because a
    /// member that names an enemy which does not exist would produce
    /// a fight against nothing.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieGegnerIdsSindEcht()
    {
        var bank = Bank();
        if (!bank.ContainsKey("troops") || !bank.ContainsKey("enemies"))
        {
            return;
        }

        var truppen = (Godot.Collections
            .Array<Godot.Collections.Dictionary>)bank["troops"];
        var monster = (Godot.Collections
            .Array<Godot.Collections.Dictionary>)bank["enemies"];
        var ids = new HashSet<int>();
        for (var i = 0; i < monster.Count; i++)
        {
            ids.Add(monster[i]["id"].AsInt32());
        }

        var namenVon = new Dictionary<int, string>();
        for (var i = 0; i < monster.Count; i++)
        {
            namenVon[monster[i]["id"].AsInt32()] =
                monster[i]["name"].AsString();
        }

        var alle = new List<string>();
        var unbekannt = 0;
        for (var i = 0; i < truppen.Count; i++)
        {
            var bytes = TruppenBytes(truppen[i]);
            if (bytes == null
                || !Rm2kTroopMemberDecoder.TryDecode(
                    bytes, out var mitglieder, out _))
            {
                continue;
            }

            foreach (var m in mitglieder)
            {
                var id = m["enemy_id"].AsInt32();
                alle.Add(id.ToString());
                if (!ids.Contains(id))
                {
                    unbekannt++;
                }
            }
        }

        Console.WriteLine("Mitglieder gesamt: " + alle.Count
            + "  unbekannte Ids: " + unbekannt);
        Console.WriteLine("erste 40: "
            + string.Join(",", alle.Take(40)));

        AssertTrue(alle.Count > 100,
            "**and the troops name more than a hundred members"
                + " between them**");

        AssertEq(0, unbekannt,
            "**and every one of them names a monster that exists**"
                + " -- and a member that names nothing would be a"
                + " fight against an empty slot");

        Console.WriteLine("Beispiel: Truppe #1 nennt "
            + alle[0] + " = "
            + (namenVon.TryGetValue(int.Parse(alle[0]),
                out var n) ? n : "?"));
    }

    private static byte[]? TruppenBytes(
        Godot.Collections.Dictionary pTruppe)
    {
        if (!pTruppe.ContainsKey("unknown_fields"))
        {
            return null;
        }

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
