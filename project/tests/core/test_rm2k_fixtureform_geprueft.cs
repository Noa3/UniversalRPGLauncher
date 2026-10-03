using System;
using System.Linq;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And the fixture, checked against a troop from the real game.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this exists because seven battle tests stopped
/// passing</strong>, -- <strong>and the fixture I wrote for them does
/// not build</strong>, -- <strong>and the question is whether the
/// fixture is wrong or the decoder is.</strong>
/// </para>
/// <para>
/// <strong>And the answer comes from the game's own bytes, not from
/// reasoning about mine.</strong>
/// </para>
/// </remarks>
public partial class TestRm2kFixtureformGeprueft : TestBase
{
    /// <summary>
    /// And the real game's troop two, byte for byte, against mine.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the only honest way to write a
    /// fixture</strong>, -- <strong>by copying what the parser
    /// produced and not by composing what a structure should
    /// look like.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieEchteTruppeUndMeineFixture()
    {
        var parser = new UniversalRPG.Rm2k.Parser.Rm2kParser();
        var result = parser.ParseDatabase(
            "res://tests/fixtures/rm2k-dragon-destiny/RPG_RT.ldb");
        AssertTrue(result.IsSuccess(), "**and the bank is read**");
        if (!result.IsSuccess())
        {
            return;
        }

        var bank = result.GetData();
        var truppen = (Godot.Collections
            .Array<Godot.Collections.Dictionary>)bank["troops"];
        foreach (var t in truppen)
        {
            if (t["id"].AsInt32() != 2)
            {
                continue;
            }

            var felder = (Godot.Collections
                .Array<Godot.Collections.Dictionary>)
                t["unknown_fields"];
            foreach (var f in felder)
            {
                if (f["id"].AsInt32() != 0x02)
                {
                    continue;
                }

                var bytes = (byte[])f["data"];
                Console.WriteLine("echt #2 (" + bytes.Length + "b): "
                    + string.Join(",", bytes));

                // **Und  jetzt  meine  Fixture  daneben.**
                var bank2 = Rm2kBegegnungsFixture.Bank();
                var eigene = (Godot.Collections
                    .Array<Godot.Collections.Dictionary>)
                    bank2["troops"];
                var felder2 = (Godot.Collections
                    .Array<Godot.Collections.Dictionary>)
                    eigene[0]["unknown_fields"];
                var bytes2 = (byte[])felder2[0]["data"];
                Console.WriteLine("meine #3 (" + bytes2.Length
                    + "b): " + string.Join(",", bytes2));

                AssertTrue(bytes.Length > 0,
                    "**and the real troop carries bytes**");
                return;
            }
        }

        AssertTrue(false, "**and troop two exists**");
    }

    /// <summary>
    /// And whether my fixture builds at all, and if not, why.
    /// </summary>
    public void Test_BautMeineFixture()
    {
        var bank = Rm2kBegegnungsFixture.Bank();
        var ok = Rm2kBegegnungAufbauen.Versuche(
            bank, 3, out var m, out var warum);
        Console.WriteLine("gebaut: " + ok + "  -> " + warum
            + "  mitglieder: " + m.Count);

        if (ok)
        {
            foreach (var x in m)
            {
                Console.WriteLine("  " + x["name"].AsString() + " "
                    + x["hp"].AsInt32() + " hp");
            }
        }

        AssertTrue(ok, "**and my fixture builds** -- and: " + warum);
    }
}
