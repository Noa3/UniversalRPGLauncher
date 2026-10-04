using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using UniversalRPG.Rm2k.Parser;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And what the targeting scopes of this game's skills are.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And a battle command needs a target list</strong>, --
/// <strong>and liblcf gives each skill a <c>scope</c> at field
/// <c>0x0C</c> that says who it may be pointed at</strong>.
/// </para>
/// <para>
/// <strong>And a reader that guesses the scope would let a cure
/// heal an enemy</strong>.
/// </para>
/// </remarks>
public partial class TestRm2kZielGemessen : TestBase
{
    private const string Ldb =
        "res://tests/fixtures/rm2k-dragon-destiny/RPG_RT.ldb";

    /// <summary>
    /// And how many skills carry which scope.
    /// </summary>
    public void Test_DieReichweitenDerFaehigkeiten()
    {
        var parser = new Rm2kParser();
        var bank = parser.ParseDatabase(Ldb);
        if (!bank.IsSuccess())
        {
            AssertTrue(false, "**and the bank is read**");
            return;
        }

        var daten = bank.GetData();
        if (!daten.ContainsKey("skills"))
        {
            AssertTrue(false, "**and the bank carries skills**");
            return;
        }

        var skills = daten["skills"]
            .AsGodotArray();
        var zaehler = new SortedDictionary<int, int>();
        var namenJeScope = new Dictionary<int, List<string>>();
        foreach (var roh in skills)
        {
            if (roh.VariantType != Godot.Variant.Type.Dictionary)
            {
                continue;
            }

            var eintrag = roh.AsGodotDictionary();
            if (!eintrag.TryGetValue("scope", out var rohScope))
            {
                continue;
            }

            var scope = rohScope.AsInt32();
            zaehler[scope] = zaehler.TryGetValue(scope, out var v)
                ? v + 1 : 1;
            var name = eintrag.TryGetValue("name", out var rohName)
                ? rohName.AsString() : "?";
            if (!namenJeScope.TryGetValue(scope, out var liste))
            {
                liste = new List<string>();
                namenJeScope[scope] = liste;
            }

            if (liste.Count < 3)
            {
                liste.Add(name);
            }
        }

        Console.WriteLine("Faehigkeiten: " + skills.Count);
        foreach (var paar in zaehler)
        {
            Console.WriteLine("  scope " + paar.Key + " : "
                + paar.Value + "  z.B. "
                + string.Join(", ", namenJeScope[paar.Key]));
        }

        AssertTrue(zaehler.Count > 1,
            "**and this game uses more than one targeting scope** --"
                + " and a reader that assumed one would mis-target"
                + " every other skill");

        // **Und  liblcfs  Skala  ist  eine  Zahl  und  kein  Flag.**
        foreach (var scope in zaehler.Keys)
        {
            AssertTrue(scope >= 0 && scope <= 7,
                "**and scope " + scope + " is inside liblcf's range"
                    + " 0..7**");
        }
    }
}
