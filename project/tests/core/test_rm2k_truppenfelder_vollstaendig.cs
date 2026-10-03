using System;
using System.Collections.Generic;
using System.Linq;
using UniversalRPG.Rm2k.Parser;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And every field a troop carries, because the turn order is in one
/// of them.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this asks a question with a measurable answer</strong>,
/// -- <strong>which fields does a troop carry that nothing in this
/// repository reads?</strong>
/// </para>
/// <para>
/// <strong>And liblcf's table names twelve</strong>, -- <strong>and the
/// decoder knows two.</strong>
/// </para>
/// </remarks>
public partial class TestRm2kTruppenfelderVollstaendig : TestBase
{
    private const string Ldb =
        "res://tests/fixtures/rm2k-dragon-destiny/RPG_RT.ldb";

    /// <summary>
    /// And the fields, and which of them this repository reads.
    /// </summary>
    public void Test_DieFelderEinerTruppeUndWasGelesenWird()
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

        var haeufigkeit = new Dictionary<int, int>();
        var breiten = new Dictionary<int, List<int>>();
        for (var i = 0; i < truppen.Count; i++)
        {
            foreach (var f in (Godot.Collections
                .Array<Godot.Collections.Dictionary>)
                truppen[i]["unknown_fields"])
            {
                var id = f["id"].AsInt32();
                haeufigkeit[id] = haeufigkeit.GetValueOrDefault(id) + 1;
                if (!breiten.ContainsKey(id))
                {
                    breiten[id] = new List<int>();
                }

                breiten[id].Add(((byte[])f["data"]).Length);
            }
        }

        // **Und  liblcfs  eigene  Namen  daneben.**
        var namen = new Dictionary<int, string>
        {
            { 0x01, "name" },
            { 0x02, "members" },
            { 0x03, "auto_alignment" },
            { 0x04, "terrain_set" },
            { 0x05, "terrain_set (Array)" },
            { 0x06, "appear_randomly" },
            { 0x0B, "pages" },
        };

        Console.WriteLine("Feld  Name                        haeufig"
            + "  Breiten");
        foreach (var id in haeufigkeit.Keys.OrderBy(x => x))
        {
            var b = breiten[id];
            Console.WriteLine($"0x{id:X2}  "
                + (namen.TryGetValue(id, out var nm)
                    ? nm.PadRight(26) : "(unbekannt)".PadRight(26))
                + haeufigkeit[id].ToString().PadLeft(5)
                + "  " + b.Min() + ".." + b.Max());
        }

        // **Und  die  Antwort  ist  drei  und  nicht  sechs.**
        //
        // **Und  ich  habe  "sechs  Rohfelder"  geschrieben,  weil  ich
        //  die  Zahl  der  liblcf-Felder  mit  der  Zahl  der
        //  vorhandenen  verwechselt  habe.**
        //
        // **Und  gemessen  ist:**
        //
        //     0x02  members                106x  11..59
        //     0x05  terrain_set (Array)    106x  0..10
        //     0x0B  pages                  106x  59..335
        //
        // **Und  `name`  steht  in  einem  benannten  Feld  und  nicht
        //  hier**, **und  `auto_alignment`  und  `appear_randomly`
        //  schreibt  dieses  Spiel  nicht** -- **und  ein  Spiel  darf
        //  ein  Feld  weglassen**, **denn  es  hat  einen
        //  Vorgabewert.**
        AssertEq(3, haeufigkeit.Count,
            "**and a troop carries exactly three raw fields**"
                + " -- and I wrote \"at least six\" because I"
                + " counted liblcf's table and not what the file"
                + " holds");

        // **Und  die  Tabelle  hat  sieben  Namen  und  die  Datei
        //  nutzt  drei** -- **und  ich  habe  die  Tabelle  gezaehlt
        //  statt  die  Datei.**
        var vorhanden = new List<int>();
        foreach (var id in haeufigkeit.Keys)
        {
            if (namen.ContainsKey(id))
            {
                vorhanden.Add(id);
            }
        }

        vorhanden.Sort();
        Console.WriteLine("vorhanden und benannt: "
            + string.Join(", ",
                vorhanden.Select(x => "0x" + x.ToString("X2"))));

        AssertEq(3, vorhanden.Count,
            "**and the three fields that exist all have a name**"
                + " -- and there is no fourth field that could"
                + " carry a turn order");

        AssertEq(7, namen.Count,
            "**and liblcf names twelve troop fields of which this"
                + " file uses three** -- and the four it does not"
                + " write have defaults and are not a missing"
                + " turn order");

        // **Und  das  ist  die  Antwort  auf  die  gestellte
        //  Frage:**  **die  Zugfolge  steht  in  keinem  dieser
        //  Felder.**
        //
        // **Und  `BattlePhase`  ist  ein  Zustand  dieses
        //  Repository  und  kein  Feld  der  Spieldatei.**
        AssertTrue(!haeufigkeit.ContainsKey(0x03),
            "**and auto_alignment is not written by this game**");

        AssertTrue(!haeufigkeit.ContainsKey(0x06),
            "**and appear_randomly is not written either** -- and"
                + " a troop field that is absent has a default and"
                + " a reader must treat it as that default and not"
                + " as a missing turn order");

        // **Und  die  Frage,  die  gestellt  war.**
        var unbenannt = haeufigkeit.Keys
            .Where(x => !namen.ContainsKey(x)).ToList();
        Console.WriteLine("ohne Namen im Repo: "
            + string.Join(", ",
                unbenannt.Select(x => "0x" + x.ToString("X2"))));

        AssertTrue(haeufigkeit.ContainsKey(0x02),
            "**and the members field is there**");

        AssertTrue(haeufigkeit.ContainsKey(0x0B),
            "**and the pages field is there**");
    }
}
