using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniversalRPG.Rm2k.Parser;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And what a skill actually carries.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the strike refuses because it looks for
/// <c>power</c> and <c>affect_hp</c> in a dictionary, and I chose those
/// field names from my own head.</strong> --
/// <strong>That is the same mistake the troop parser made, and it is
/// worth measuring instead of renaming.</strong>
/// </para>
/// </remarks>
public partial class TestRm2kFaehigkeitsfelderGemessen : TestBase
{
    private const string Ldb =
        "res://tests/fixtures/rm2k-dragon-destiny/RPG_RT.ldb";

    /// <summary>
    /// And the fields of the skills the heroes learned.
    /// </summary>
    public void Test_DieFelderDerGelerntenFaehigkeiten()
    {
        var parser = new Rm2kParser();
        var result = parser.ParseDatabase(Ldb);
        if (!result.IsSuccess())
        {
            AssertTrue(false, "**and the bank is read**");
            return;
        }

        var bank = result.GetData();
        var skills = (Godot.Collections
            .Array<Godot.Collections.Dictionary>)bank["skills"];
        var helden = (Godot.Collections
            .Array<Godot.Collections.Dictionary>)bank["actors"];

        var gelerntIds = new SortedSet<int>();
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
                if (f["id"].AsInt32() != 0x3F
                    || !Rm2kLearningDecoder.TryDecode(
                        (byte[])f["data"], out var gelernt, out _))
                {
                    continue;
                }

                foreach (var e in gelernt)
                {
                    gelerntIds.Add(e["skill_id"].AsInt32());
                }
            }
        }

        Console.WriteLine("gelernte IDs: " + string.Join(",",
            gelerntIds));

        foreach (var id in gelerntIds.Take(4))
        {
            foreach (var s in skills)
            {
                if (s["id"].AsInt32() != id)
                {
                    continue;
                }

                Console.WriteLine("Skill " + id + ": "
                    + string.Join(" ", s.Keys.Select(
                        x => x.ToString()).OrderBy(x => x)));
                Console.WriteLine("   roh: " + string.Join(" | ",
                    new List<string> {
                        s.ContainsKey("power") ? "power=" +
                            s["power"].AsInt32() : "",
                        s.ContainsKey("affect_hp") ? "affect_hp=" +
                            s["affect_hp"].AsInt32() : "",
                        s.ContainsKey("hit") ? "hit=" +
                            s["hit"].AsInt32() : "",
                        s.ContainsKey("scope") ? "scope=" +
                            s["scope"].AsInt32() : "",
                        s.ContainsKey("type") ? "type=" +
                            s["type"].AsInt32() : "",
                    }.Where(x => x.Length > 0)));
            }
        }

        // **Und  wie  viele  der  27  haben  eine  Kraft?**
        var mitKraft = 0;
        var mitHPRichtung = 0;
        var trefferGesamt = 0;
        foreach (var id in gelerntIds)
        {
            foreach (var s in skills)
            {
                if (s["id"].AsInt32() != id)
                {
                    continue;
                }

                var hatKraft = s.ContainsKey("power");
                if (hatKraft)
                {
                    mitKraft++;
                    Console.WriteLine("  Kraft Skill " + id
                        + " = " + s["power"].AsInt32()
                        + "  scope "
                        + (s.ContainsKey("scope")
                            ? s["scope"].AsInt32() : -1)
                        + "  type "
                        + (s.ContainsKey("type")
                            ? s["type"].AsInt32() : -1)
                        + "  affect_hp "
                        + (s.ContainsKey("affect_hp")
                            ? s["affect_hp"].AsInt32() : -1));
                }

                if (s.ContainsKey("affect_hp"))
                {
                    mitHPRichtung++;
                }

                if (s.ContainsKey("hit"))
                {
                    trefferGesamt++;
                }
            }
        }

        Console.WriteLine("mit Kraft " + mitKraft
            + "  mit affect_hp " + mitHPRichtung
            + "  mit hit " + trefferGesamt
            + "  gelernt " + gelerntIds.Count);

        // **Und  was  steht  in  unknown_fields  einer  Faehigkeit
        //  ohne  sichtbares  `power`?**
        foreach (var s in skills)
        {
            if (s["id"].AsInt32() != 2
                || !s.ContainsKey("unknown_fields"))
            {
                continue;
            }

            foreach (var f in (Godot.Collections
                .Array<Godot.Collections.Dictionary>)
                s["unknown_fields"])
            {
                Console.WriteLine("  Skill 2 roh 0x"
                    + f["id"].AsInt32().ToString("X")
                    + "  " + ((byte[])f["data"]).Length
                    + "b");
            }
        }

        AssertTrue(gelerntIds.Count > 0,
            "**and the heroes learned skills**");
    }
}
