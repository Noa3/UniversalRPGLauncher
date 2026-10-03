using System;
using System.Collections.Generic;
using System.Linq;
using UniversalRPG.Rm2k.Parser;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The raw bytes of a troop's member list, and the pattern in them.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And my decoder read one hundred and two of one hundred and
/// six troops</strong>, -- <strong>and the four it refused all carry
/// an <c>xN</c> in their name.</strong> -- <strong>And that is too
/// neat to be a coincidence and too neat to be a conclusion.</strong>
/// </para>
/// <para>
/// <strong>So this prints the bytes and counts what is in
/// them</strong>, -- <strong>because the shape decides where the
/// reader is wrong.</strong>
/// </para>
/// </remarks>
public partial class TestRm2kTruppenbytesMuster : TestBase
{
    private const string Ldb =
        "res://tests/fixtures/rm2k-dragon-destiny/RPG_RT.ldb";

    /// <summary>
    /// And the bytes, sorted by how many they are.
    /// </summary>
    public void Test_DieRohbytesNachLaenge()
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

        var nachLaenge = new List<string>();
        for (var i = 0; i < truppen.Count; i++)
        {
            var bytes = Bytes(truppen[i]);
            if (bytes == null)
            {
                continue;
            }

            var gelesen = Rm2kTroopMemberDecoder.TryDecode(
                bytes, out var mitglieder, out _);
            nachLaenge.Add($"{bytes.Length}b "
                + $"gelesen {(gelesen ? mitglieder.Count.ToString() : "-")}"
                + "  " + string.Join(",", bytes.Take(14)));
        }

        nachLaenge.Sort(StringComparer.Ordinal);
        foreach (var z in nachLaenge.Take(12))
        {
            Console.WriteLine(z);
        }

        Console.WriteLine("...");
        foreach (var z in nachLaenge.Skip(nachLaenge.Count - 6))
        {
            Console.WriteLine(z);
        }

        AssertTrue(nachLaenge.Count == 106,
            "**and every troop carries field two**");
    }

    /// <summary>
    /// And whether the first byte is the count after all.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And I believed this once and it was wrong</strong>, --
    /// <strong>but I believed it from eight printed lines rather than
    /// from all one hundred and six</strong>, -- <strong>and this
    /// counts it properly.</strong>
    /// </para>
    /// <para>
    /// <strong>And the test is a question, because the answer decides
    /// whether the decoder is right or wrong.</strong>
    /// </para>
    /// </remarks>
    public void Test_IstDasErsteByteDieAnzahl()
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

        var passt = 0;
        var geprueft = 0;
        var widerspruch = new List<string>();
        for (var i = 0; i < truppen.Count; i++)
        {
            var bytes = Bytes(truppen[i]);
            if (bytes == null || bytes.Length == 0)
            {
                continue;
            }

            if (!Rm2kTroopMemberDecoder.TryDecode(
                    bytes, out var mitglieder, out _))
            {
                continue;
            }

            geprueft++;
            if (bytes[0] == mitglieder.Count)
            {
                passt++;
            }
            else if (widerspruch.Count < 6)
            {
                widerspruch.Add("#" + (i + 1) + ": erstes Byte "
                    + bytes[0] + ", gelesen " + mitglieder.Count);
            }
        }

        Console.WriteLine($"erstes Byte = Anzahl bei {passt} von"
            + $" {geprueft}");
        foreach (var w in widerspruch)
        {
            Console.WriteLine("  " + w);
        }

        AssertEq(geprueft, passt,
            "**and the first byte is the member count** -- and this"
                + " is the claim I made once from eight lines, took"
                + " back, and have now measured over all one hundred"
                + " and two troops that decode: it holds for every"
                + " single one of them");

        // **Und  die  vier  Ausnahmen  --  denn  "102  von  106"  ist
        //  keine  vollstaendige  Aussage.**
        //
        // **Und  ihre  Bytes  sehen  aus  wie
        //  `1,1,2,2,129,32,3,2,129,5,0`** -- **und  das  ist  nicht
        //  das  Muster  der  102**, **denn  dort  auf  die  Anzahl
        //  folgt  `1,1,1,1,<enemy_id>,2,...`**.
        //
        // **Und  die  zweiten  Byte  sind  nicht  1** -- **und  das
        //  ist  der  Unterschied,  und  er  ist  messbar.**
        var abweichend = new List<string>();
        for (var i = 0; i < truppen.Count; i++)
        {
            var bytes = Bytes(truppen[i]);
            if (bytes == null || bytes.Length == 0)
            {
                continue;
            }

            if (Rm2kTroopMemberDecoder.TryDecode(
                    bytes, out _, out var warum))
            {
                continue;
            }

            abweichend.Add("#" + (i + 1) + " \""
                + truppen[i]["name"].AsString() + "\": " + warum
                + "  roh " + string.Join(",", bytes.Take(11)));
        }

        foreach (var a in abweichend)
        {
            Console.WriteLine("  abweichend " + a);
        }

        AssertEq(4, abweichend.Count,
            "**and four troops do not decode** -- and they are named"
                + " and listed above, and a reader that quietly"
                + " turned them into an empty fight would hide"
                + " exactly what it cannot read");

        // **Und  jetzt  der  Vergleich,  der  es  erklaert.**
        //
        //     gelesen:  anzahl, 1, 1, 1, <enemy_id>, 2, 2, ...
        //     abweichend: anzahl, 1, 2, 2, 129, 32, 3, 2, ...
        //
        // **Und  das  dritte  Byte  ist  es** -- **und  das  ist  der
        //  ganze  Unterschied.**
        for (var i = 0; i < truppen.Count; i++)
        {
            var bytes = Bytes(truppen[i]);
            if (bytes == null || bytes.Length < 4)
            {
                continue;
            }

            var dekodiert = Rm2kTroopMemberDecoder.TryDecode(
                bytes, out _, out _);
            Console.WriteLine($"  #{i + 1} {(dekodiert ? "ok  " : "FEHL")}"
                + $" anzahl={bytes[0]} b2={bytes[1]} b3={bytes[2]}"
                + $" b4={bytes[3]}");
        }
    }

    private static byte[]? Bytes(Godot.Collections.Dictionary pTruppe)
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
