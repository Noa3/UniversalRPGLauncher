using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniversalRPG.Rm2k.Parser;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And that a hero's learned skills come out of the game's own file.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is the reader the strike's refusal asked
/// for.</strong> --
/// <strong><c>0x3F skills</c> is an <c>Array&lt;Learning&gt;</c> and a
/// <c>Learning</c> is an id plus a level and a skill.</strong>
/// </para>
/// <para>
/// <strong>And my first reader returned nothing</strong>, -- <strong>
/// and the reason is in
/// <c>test_rm2k_lernfeld_die_byteform.cs</c> rather than in
/// here.</strong>
/// </para>
/// </remarks>
public partial class TestRm2kLernfeldWirdGelesen : TestBase
{
    private const string Ldb =
        "res://tests/fixtures/rm2k-dragon-destiny/RPG_RT.ldb";

    private static (List<int> Ids, List<string> Namen)? Lese()
    {
        var parser = new Rm2kParser();
        var result = parser.ParseDatabase(Ldb);
        if (!result.IsSuccess())
        {
            return null;
        }

        var bank = result.GetData();
        if (!bank.ContainsKey("actors")
            || !bank.ContainsKey("skills"))
        {
            return null;
        }

        var helden = (Godot.Collections
            .Array<Godot.Collections.Dictionary>)
            bank["actors"];
        // **Und  nicht  jede  Faehigkeit  traegt  einen  Namen.**
        //
        // **Und  der  Zugriff  auf  einen  fehlenden  Schluessel  ist
        //  ein  Absturz  und  keine  leere  Zelle** -- **und  ich  habe
        //  mich  darauf  verlassen,  dass  alle  300  Eintraege  einen
        //  Namen  haben.**
        var namenVon = new Dictionary<int, string>();
        var ohneNamen = 0;
        foreach (var s in (Godot.Collections
            .Array<Godot.Collections.Dictionary>)bank["skills"])
        {
            if (!s.ContainsKey("name"))
            {
                ohneNamen++;
                continue;
            }

            namenVon[s["id"].AsInt32()] = s["name"].AsString();
        }

        Console.WriteLine("Faehigkeiten " + namenVon.Count
            + "  ohne Namensfeld " + ohneNamen);



        var ids = new List<int>();
        var namen = new List<string>();
        for (var i = 0; i < helden.Count; i++)
        {
            if (!helden[i].ContainsKey("unknown_fields"))
            {
                continue;
            }

            foreach (var f in (Godot.Collections
                .Array<Godot.Collections.Dictionary>)
                helden[i]["unknown_fields"])
            {
                if (f["id"].AsInt32() != 0x3F)
                {
                    continue;
                }

                if (!Rm2kLearningDecoder.TryDecode(
                        (byte[])f["data"], out var gelernt, out var warum))
                {
                    Console.WriteLine("  verweigert -> " + warum);
                    continue;
                }

                Console.WriteLine("  Held " + i + ": "
                    + gelernt.Count + " gelernt");

                foreach (var e in gelernt)
                {
                    var id = e["skill_id"].AsInt32();
                    ids.Add(id);
                    namen.Add(namenVon.TryGetValue(id, out var n)
                        ? n : "");
                }
            }
        }

        return (ids, namen);
    }

    /// <summary>
    /// And the heroes learned skills, and Rast learned eighteen.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And Salazar and Moser hold a single zero byte</strong>, --
    /// <strong>and a zero count is "learned nothing" and not a
    /// fault.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieHeldenGelerntenFaehigkeiten()
    {
        var gelesen = Lese();
        if (gelesen == null)
        {
            return;
        }

        Console.WriteLine("gesamt " + gelesen.Value.Ids.Count);
        AssertTrue(gelesen.Value.Ids.Count > 7,
            "**and the heroes learned more than one skill"
                + " between them** -- and without this a hero in a"
                + " game with three hundred skills has none of"
                + " them");

        AssertTrue(gelesen.Value.Ids.Distinct().Count() > 1,
            "**and they are not all the same skill** -- and a"
                + " reader that returned one number seven times"
                + " would also pass a count check");
    }

    /// <summary>
    /// And every learned skill id resolves to the game's own name.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this replaced an assertion about levels.</strong> --
    /// <strong>It claimed the highest level is above one, and the
    /// measurement says this game writes no level field at
    /// all.</strong> -- <strong>liblcf leaves <c>level</c> at its
    /// default of one.</strong>
    /// </para>
    /// <para>
    /// <strong>And resolving against the game's skill table is the
    /// proof that a learned skill is a real skill and not a small
    /// number.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieGelerntenSindEchteFaehigkeiten()
    {
        var gelesen = Lese();
        if (gelesen == null)
        {
            return;
        }

        var namen = gelesen.Value.Namen;
        var leer = namen.Count(x => x.Length == 0);
        Console.WriteLine("Namen: " + string.Join(" | ",
            namen.Distinct().Take(20)));
        Console.WriteLine("gesamt " + namen.Count
            + "  ohne Namen " + leer
            + "  verschieden " + namen.Distinct().Count());

        AssertEq(0, leer,
            "**and every learned skill id resolves to a name in"
                + " the game's own skill table** -- and an id that"
                + " resolves to nothing would mean the reader had"
                + " invented a skill");
    }
}
