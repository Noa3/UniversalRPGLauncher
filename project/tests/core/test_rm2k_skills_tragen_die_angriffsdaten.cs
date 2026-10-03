using System;
using System.Collections.Generic;
using System.Linq;
using UniversalRPG.Rm2k.Parser;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And the skills, and that they carry the attack's numbers.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is the other half of the answer to "where is an
/// actor's strike".</strong> --
/// <strong>liblcf gives <c>BattleCommand::attack</c> the number
/// <c>0</c> and no event command for it exists.</strong> --
/// <strong>And <c>Skill.power</c> at <c>0x18</c> and
/// <c>Skill.hit</c> at <c>0x19</c> are where the numbers of an
/// attack live.</strong>
/// </para>
/// </remarks>
public partial class TestRm2kSkillsTragenDieAngriffsdaten : TestBase
{
    private const string Ldb =
        "res://tests/fixtures/rm2k-dragon-destiny/RPG_RT.ldb";

    /// <summary>
    /// And the skills the game ships.
    /// </summary>
    public void Test_DieSkillsDesSpiels()
    {
        var parser = new Rm2kParser();
        var result = parser.ParseDatabase(Ldb);
        if (!result.IsSuccess())
        {
            AssertTrue(false, "**and the bank is read**");
            return;
        }

        var skills = (Godot.Collections
            .Array<Godot.Collections.Dictionary>)
            result.GetData()["skills"];
        Console.WriteLine("Skills: " + skills.Count);

        var zeilen = new List<string>();
        for (var i = 0; i < skills.Count && i < 8; i++)
        {
            var felder = new List<string>();
            foreach (var k in skills[i].Keys)
            {
                felder.Add(k.AsString());
            }

            felder.Sort(StringComparer.Ordinal);
            zeilen.Add("#" + skills[i]["id"].AsInt32() + " "
                + skills[i]["name"].AsString() + ": "
                + string.Join(", ", felder));
        }

        foreach (var z in zeilen)
        {
            Console.WriteLine(z);
        }

        AssertTrue(skills.Count > 0,
            "**and the game ships skills**");
    }

    /// <summary>
    /// And the numbers inside them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this asserts nothing about which field holds the
    /// numbers until it has looked</strong>, -- <strong>because the
    /// last two commits both guessed a field id and both guesses
    /// were wrong.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieZahlenInDenSkills()
    {
        var parser = new Rm2kParser();
        var result = parser.ParseDatabase(Ldb);
        if (!result.IsSuccess())
        {
            return;
        }

        var skills = (Godot.Collections
            .Array<Godot.Collections.Dictionary>)
            result.GetData()["skills"];

        var mitPower = 0;
        var mitHit = 0;
        var mitVariance = 0;
        var ids = new List<string>();
        for (var i = 0; i < skills.Count; i++)
        {
            // **Und  `power`  ist  ein  BENANNTES  Feld  und  nicht
            //  ein  Rohfeld** -- **und  das  ist  besser,  als  meine
            //  Vermutung  war.**
            if (skills[i].ContainsKey("power"))
            {
                mitPower++;
            }

            if (skills[i].ContainsKey("variance"))
            {
                mitVariance++;
            }

            if (!skills[i].ContainsKey("unknown_fields"))
            {
                continue;
            }

            var roh = skills[i]["unknown_fields"];
            if (mitPower + mitHit < 4)
            {
                var teile = new List<string>();
                foreach (var f in (Godot.Collections
                    .Array<Godot.Collections.Dictionary>)roh)
                {
                    teile.Add("0x" + f["id"].AsInt32().ToString("X2")
                        + "(" + ((byte[])f["data"]).Length + "b)");
                }

                ids.Add(skills[i]["name"].AsString() + ": "
                    + string.Join(" ", teile));
            }

            foreach (var f in (Godot.Collections
                .Array<Godot.Collections.Dictionary>)roh)
            {
                if (f["id"].AsInt32() == 0x18)
                {
                    mitPower++;
                }
                else if (f["id"].AsInt32() == 0x19)
                {
                    mitHit++;
                }
            }
        }

        foreach (var z in ids)
        {
            Console.WriteLine(z);
        }

        Console.WriteLine($"benanntes power: {mitPower}x   "
            + $"benanntes variance: {mitVariance}x   "
            + $"Feld 0x19 hit: {mitHit}x   von {skills.Count}");

        AssertTrue(mitPower > 0,
            "**and the game's own attack skills carry a named"
                + " `power`** -- and \"Dragonflame\" and \"Dragon"
                + " Power\" both do, and the parser already reads"
                + " it, and my guess of a raw field 0x18 was"
                + " wrong because the parser names it");

        AssertTrue(mitVariance > 0,
            "**and a named `variance`** -- and that is the value"
                + " `Rm2kSimulatedAttack.Compute` already takes as"
                + " its last number, so the formula and the data"
                + " meet at a field both of them already name");
    }
}
