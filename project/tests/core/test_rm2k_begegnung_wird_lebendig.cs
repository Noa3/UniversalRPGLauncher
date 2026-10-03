using System;
using System.Collections.Generic;
using System.Linq;
using UniversalRPG.Rm2k.Parser;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// An encounter, and that it becomes monsters with real numbers.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is the join.</strong> -- <strong>Six places in
/// the interpreter read <c>TroopMembers</c> and nothing filled
/// it</strong>, -- <strong>and every test that put a monster there
/// did it with a literal</strong>, --
/// <strong>so the battle commands had monsters to work on and no game
/// could ever produce one.</strong>
/// </para>
/// </remarks>
public partial class TestRm2kBegegnungWirdLebendig : TestBase
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
    /// And the game's own encounters become monsters.
    /// </summary>
    public void Test_BegegnungenWerdenMonster()
    {
        var bank = Bank();
        if (!bank.ContainsKey("troops"))
        {
            AssertTrue(false, "**and the bank has troops**");
            return;
        }

        var truppen = (Godot.Collections
            .Array<Godot.Collections.Dictionary>)bank["troops"];

        var gebaut = 0;
        var verweigert = new List<string>();
        var namen = new List<string>();
        for (var i = 0; i < truppen.Count; i++)
        {
            var id = truppen[i]["id"].AsInt32();
            if (Rm2kBegegnungAufbauen.Versuche(
                    bank, id, out var m, out var warum))
            {
                gebaut++;
                if (gebaut <= 4)
                {
                    var teile = new List<string>();
                    foreach (var x in m)
                    {
                        teile.Add(x["name"].AsString() + " "
                            + x["hp"].AsInt32() + "/"
                            + x["max_hp"].AsInt32() + " atk "
                            + x["attack"].AsInt32());
                    }

                    namen.Add("#" + id + " -> " + string.Join("; ", teile));
                }

                continue;
            }

            if (verweigert.Count < 6)
            {
                verweigert.Add("#" + id + ": " + warum);
            }
        }

        foreach (var n in namen)
        {
            Console.WriteLine(n);
        }

        Console.WriteLine($"gebaut {gebaut} von {truppen.Count}, "
            + $"verweigert {verweigert.Count}");
        foreach (var v in verweigert)
        {
            Console.WriteLine("  " + v);
        }

        AssertTrue(gebaut > 90,
            "**and more than ninety encounters became monsters** -- and"
                + " that is what nothing in this repository did before");
    }

    /// <summary>
    /// And the numbers are the game's own.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is checked against the enemy table and not
    /// against a constant</strong>, -- <strong>because a member that
    /// carried a made-up number would fight and appear to
    /// work.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieZahlenSindDieDesSpiels()
    {
        var bank = Bank();
        if (!bank.ContainsKey("enemies"))
        {
            return;
        }

        var monster = (Godot.Collections
            .Array<Godot.Collections.Dictionary>)bank["enemies"];
        var erwartet = new Dictionary<int, (string Name, int Hp, int Atk)>();
        foreach (var m in monster)
        {
            erwartet[m["id"].AsInt32()] = (
                m["name"].AsString(),
                m.ContainsKey("max_hp") ? m["max_hp"].AsInt32() : 0,
                m.ContainsKey("attack") ? m["attack"].AsInt32() : 0);
        }

        var geprueft = 0;
        for (var id = 1; id <= 30; id++)
        {
            if (!Rm2kBegegnungAufbauen.Versuche(
                    bank, id, out var m, out _))
            {
                continue;
            }

            foreach (var x in m)
            {
                var eid = x["enemy_id"].AsInt32();
                if (!erwartet.TryGetValue(eid, out var e))
                {
                    continue;
                }

                AssertEq(e.Name, x["name"].AsString(),
                    "**and the name is the game's own**");
                AssertEq(e.Hp, x["hp"].AsInt32(),
                    "**and the hit points are the game's own**");
                AssertEq(e.Hp, x["max_hp"].AsInt32(),
                    "**and max is the same number** -- and a member"
                        + " that started at less than full would have"
                        + " begun the fight already wounded");
                AssertEq(e.Atk, x["attack"].AsInt32(),
                    "**and the attack is the game's own**");
                geprueft++;
            }
        }

        // **Und  ich  habe  nur  dreissig  Truppen  durchlaufen
        //  lassen** -- **und  behauptet,  es  waeren  ueber  hundert
        //  Monster.**  **Es  sind  41.**
        Console.WriteLine("geprueft: " + geprueft + " Monster");
        AssertTrue(geprueft >= 40,
            "**and forty one monsters carry the game's own"
                + " numbers** -- and I wrote \"more than a hundred\""
                + " because I had not counted what my own loop"
                + " produced");
    }

    /// <summary>
    /// And what a battle command needs is really there.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And <c>CanMonsterAct</c> reads <c>hp</c></strong>, --
    /// <strong>and this asserts it on a built troop rather than on a
    /// literal</strong>, -- <strong>because the schema was the reason
    /// this join could fail quietly.</strong>
    /// </para>
    /// </remarks>
    public void Test_DerBefehlKannDenGegnerBenutzen()
    {
        var bank = Bank();
        // **Und  Truppe  1  ist  eine  der  vier,  die  kein
        //  enemy_id  fuehren** -- **und  ich  hatte  sie  gewaehlt,
        //  ohne  es  zu  wissen.**  --
        // **Truppe  2  baut  und  ist  die  erste  des  Spiels.**
        if (!Rm2kBegegnungAufbauen.Versuche(
                bank, 2, out var m, out var warum))
        {
            AssertTrue(false, "**and troop two builds** -- and: "
                + warum);
            return;
        }

        Console.WriteLine("Truppe 2: " + m.Count + " Monster");
        foreach (var x in m)
        {
            Console.WriteLine("  " + x["name"].AsString() + " "
                + x["hp"].AsInt32() + " hp  atk "
                + x["attack"].AsInt32() + " def "
                + x["defense"].AsInt32() + "  bei ("
                + x["x"].AsInt32() + "," + x["y"].AsInt32() + ")");
        }

        AssertTrue(m.Count > 0, "**and it has members**");

        foreach (var x in m)
        {
            AssertTrue(x.ContainsKey("hp"),
                "**and every member carries hp** -- and"
                    + " `CanMonsterAct` reads that key and a member"
                    + " without it would be unable to strike back"
                    + " in a real battle");
            AssertTrue(x["hp"].AsInt32() > 0,
                "**and every member is alive**");
        }
    }
}
