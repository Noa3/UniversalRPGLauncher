using System;
using System.Collections.Generic;
using System.Linq;
using UniversalRPG.Rm2k.Parser;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The troop's pages, and that they hold the battle itself.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this asks where a turn comes from.</strong> --
/// <strong>The measured game writes 428 encounters and zero
/// <c>13310 Battle Branch</c> and zero <c>11740 Encounter
/// Steps</c> across seven hundred and forty three maps.</strong> --
/// <strong>So the battle's own commands are not on the maps.</strong>
/// </para>
/// <para>
/// <strong>And liblcf says they are:</strong> <c>Troop.pages</c> is
/// field <c>0x0B</c>, an array of <c>TroopPage</c>, and a
/// <c>TroopPage</c> carries <c>event_commands</c> at <c>0x0B</c> and
/// <c>0x0C</c>.
/// </para>
/// </remarks>
public partial class TestRm2kTruppenseitenGemessen : TestBase
{
    private const string Ldb =
        "res://tests/fixtures/rm2k-dragon-destiny/RPG_RT.ldb";

    /// <summary>
    /// And how many troops carry a page field at all.
    /// </summary>
    public void Test_WelcheTrupeHatSeiten()
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

        var mitSeiten = 0;
        var ids = new List<string>();
        for (var i = 0; i < truppen.Count; i++)
        {
            var felder = (Godot.Collections
                .Array<Godot.Collections.Dictionary>)
                truppen[i]["unknown_fields"];
            foreach (var f in felder)
            {
                if (f["id"].AsInt32() == 0x0B)
                {
                    mitSeiten++;
                    if (ids.Count < 6)
                    {
                        ids.Add("#" + truppen[i]["id"].AsInt32()
                            + " " + ((byte[])f["data"]).Length + "b");
                    }
                }
            }
        }

        Console.WriteLine("Truepen mit Seiten (0x0B): "
            + mitSeiten + " von " + truppen.Count);
        Console.WriteLine("Beispiele: " + string.Join(", ", ids));

        AssertTrue(mitSeiten > 0,
            "**and at least one troop carries a pages field**");

        AssertEq(106, mitSeiten,
            "**and every one of the hundred and six does** -- and"
                + " that is what makes the battle reachable");
    }

    /// <summary>
    /// And what is inside a page field.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this prints, because the answer decides whether
    /// the battle can be built from the game at all.</strong>
    /// </para>
    /// </remarks>
    public void Test_DerInhaltEinerSeite()
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
        for (var i = 0; i < truppen.Count; i++)
        {
            var felder = (Godot.Collections
                .Array<Godot.Collections.Dictionary>)
                truppen[i]["unknown_fields"];
            foreach (var f in felder)
            {
                if (f["id"].AsInt32() != 0x0B)
                {
                    continue;
                }

                var daten = (byte[])f["data"];
                Console.WriteLine("Trupe #"
                    + truppen[i]["id"].AsInt32() + " ("
                    + truppen[i]["name"].AsString() + ") "
                    + daten.Length + "b:");
                Console.WriteLine("  " + string.Join(",",
                    daten.Take(40)));
                AssertTrue(daten.Length > 0,
                    "**and the pages field carries bytes**");
                return;
            }
        }

        AssertTrue(false, "**and a troop with pages exists**");
    }
}
