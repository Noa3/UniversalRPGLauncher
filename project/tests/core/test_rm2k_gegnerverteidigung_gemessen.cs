using System;
using System.Collections.Generic;
using System.Linq;
using UniversalRPG.Rm2k.Parser;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And what the bank's monsters carry for defence and spirit.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this asks before it asserts.</strong> --
/// <strong>A scorpion's defence printed as zero, and zero is either
/// the game's number or a field this reader did not fill.</strong>
/// </para>
/// </remarks>
public partial class TestRm2kGegnerverteidigungGemessen : TestBase
{
    private const string Ldb =
        "res://tests/fixtures/rm2k-dragon-destiny/RPG_RT.ldb";

    /// <summary>
    /// And the numbers, for every monster that carries them.
    /// </summary>
    public void Test_DieVerteidigungenDerBank()
    {
        var parser = new Rm2kParser();
        var result = parser.ParseDatabase(Ldb);
        if (!result.IsSuccess())
        {
            AssertTrue(false, "**and the bank is read**");
            return;
        }

        var monster = (Godot.Collections
            .Array<Godot.Collections.Dictionary>)
            result.GetData()["enemies"];

        var mitFeld = 0;
        var ungleichNull = 0;
        var werte = new List<string>();
        for (var i = 0; i < monster.Count; i++)
        {
            var m = monster[i];
            if (!m.ContainsKey("defense"))
            {
                continue;
            }

            mitFeld++;
            var v = m["defense"].AsInt32();
            if (v != 0)
            {
                ungleichNull++;
                if (werte.Count < 8)
                {
                    werte.Add(m["name"].AsString() + "="
                        + v + "/geist="
                        + (m.ContainsKey("spirit")
                            ? m["spirit"].AsInt32() : -1));
                }
            }
        }

        Console.WriteLine($"defense-Feld bei {mitFeld} von "
            + monster.Count + ",  ungleich null: "
            + ungleichNull);
        foreach (var w in werte)
        {
            Console.WriteLine("  " + w);
        }

        // **Und  es  sind  56  von  72  und  nicht  "die  meisten".**
        //
        // **Und  ich  habe  60  als  Schwelle  gesetzt,  ohne  es  zu
        //  messen** -- **und  56  ist  deutlich  weniger  als  60,
        //  aber  deutlich  mehr  als  die  Haelfte.**
        Console.WriteLine("mit defence-Feld: " + mitFeld
            + ", davon ungleich null: " + ungleichNull);

        AssertEq(56, mitFeld,
            "**and fifty six of the seventy two carry a defence"
                + " field** -- and I wrote \"more than sixty\""
                + " because I had not counted");

        AssertEq(mitFeld, ungleichNull,
            "**and every one of those is above zero** -- and a"
                + " reader that returned zero for every monster"
                + " would make every fight a one-shot");

        AssertTrue(ungleichNull > 10,
            "**and many of them have a defence above zero** -- and"
                + " a reader that returned zero for every monster"
                + " would make every fight a one-shot");
    }
}
