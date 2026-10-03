using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniversalRPG.Rm2k.Parser;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And the learning field's own byte form.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is the evidence the decoder rests on.</strong> --
/// <strong>A count, then per entry an id, a chunk
/// <c>0x02</c> carrying the skill, and a zero.</strong>
/// </para>
/// <para>
/// <strong>And it also prints, per hero, how many entries were
/// read</strong>, -- <strong>because "the reader works" is a weaker
/// claim than "here are the seven numbers".</strong>
/// </para>
/// <para>
/// <strong>And it started as a throwaway that said where the reading
/// stopped</strong>, -- <strong>because three tests failed with "the
/// given key was not present" and a message that does not say where
/// costs a session.</strong>
/// </para>
/// </remarks>
public partial class TestRm2kLernfeldDieByteform : TestBase
{
    private const string Ldb =
        "res://tests/fixtures/rm2k-dragon-destiny/RPG_RT.ldb";

    /// <summary>
    /// And it says which line and which hero.
    /// </summary>
    public void Test_DieByteformTragetDieGelernten()
    {
        var parser = new Rm2kParser();
        var result = parser.ParseDatabase(Ldb);
        if (!result.IsSuccess())
        {
            AssertTrue(false, "**and the bank is read**");
            return;
        }

        var bank = result.GetData();
        Console.WriteLine("A: bank gelesen");

        var helden = (Godot.Collections
            .Array<Godot.Collections.Dictionary>)
            bank["actors"];
        Console.WriteLine("B: actors geholt " + helden.Count);

        var skills = (Godot.Collections
            .Array<Godot.Collections.Dictionary>)
            bank["skills"];
        Console.WriteLine("C: skills geholt " + skills.Count);

        var gelerntGesamt = 0;
        for (var i = 0; i < helden.Count; i++)
        {
            var h = helden[i];
            Console.WriteLine("D" + i + ": " + string.Join(" ",
                h.Keys.Select(x => x.ToString()).OrderBy(x => x)));

            if (!h.ContainsKey("unknown_fields"))
            {
                Console.WriteLine("D" + i + ": kein unknown_fields");
                continue;
            }

            var felder = (Godot.Collections
                .Array<Godot.Collections.Dictionary>)
                h["unknown_fields"];
            Console.WriteLine("D" + i + ": " + felder.Count
                + " Felder");

            foreach (var f in felder)
            {
                if (f["id"].AsInt32() != 0x3F)
                {
                    continue;
                }

                Console.WriteLine("D" + i + ": 0x3F gefunden");

                if (!Rm2kLearningDecoder.TryDecode(
                        (byte[])f["data"], out var gelernt,
                        out var warum))
                {
                    Console.WriteLine("D" + i + ": verweigert "
                        + warum);
                    continue;
                }

                Console.WriteLine("D" + i + ": " + gelernt.Count
                    + " gelernt");
                gelerntGesamt += gelernt.Count;

                foreach (var e in gelernt.Take(2))
                {
                    Console.WriteLine("     id "
                        + e["skill_id"].AsInt32());
                    if (skills.Count == 0)
                    {
                        continue;
                    }

                    var treffer = 0;
                    foreach (var s in skills)
                    {
                        if (s["id"].AsInt32()
                            == e["skill_id"].AsInt32())
                        {
                            treffer++;
                        }
                    }

                    Console.WriteLine("     im Skilltable "
                        + treffer);
                }
            }
        }

        // **Und  die  sieben  Helden  mit  ihren  Zahlen.**
        Console.WriteLine("Helden mit Lernfeld: " + helden.Count);
        AssertEq(7, helden.Count,
            "**and the bank carries seven heroes**");

        AssertTrue(gelerntGesamt > 0,
            "**and their learning fields hold entries**");
    }
}
