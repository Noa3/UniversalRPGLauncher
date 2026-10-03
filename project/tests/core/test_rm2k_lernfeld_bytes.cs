using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniversalRPG.Rm2k.Parser;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And the learning field's own bytes.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is a measurement, not a reader.</strong> --
/// <strong>My first reader returned nothing and the test failed with
/// an empty dictionary, and guessing again would be the third
/// wrong attempt.</strong>
/// </para>
/// </remarks>
public partial class TestRm2kLernfeldBytes : TestBase
{
    private const string Ldb =
        "res://tests/fixtures/rm2k-dragon-destiny/RPG_RT.ldb";

    /// <summary>
    /// And the bytes, in full.
    /// </summary>
    public void Test_DieBytesDesLernfelds()
    {
        var parser = new Rm2kParser();
        var result = parser.ParseDatabase(Ldb);
        if (!result.IsSuccess())
        {
            AssertTrue(false, "**and the bank is read**");
            return;
        }

        var helden = (Godot.Collections
            .Array<Godot.Collections.Dictionary>)
            result.GetData()["actors"];
        var gezeigt = 0;
        for (var i = 0; i < helden.Count; i++)
        {
            foreach (var f in (Godot.Collections
                .Array<Godot.Collections.Dictionary>)
                helden[i]["unknown_fields"])
            {
                if (f["id"].AsInt32() != 0x3F)
                {
                    continue;
                }

                var daten = (byte[])f["data"];
                gezeigt++;
                Console.WriteLine(helden[i]["name"].AsString()
                    + "  " + daten.Length + "b:  "
                    + string.Join(",", daten.Take(40)));
            }
        }

        Console.WriteLine("Heroen mit Lernfeld: " + gezeigt);
        AssertTrue(gezeigt > 0,
            "**and at least one hero carries the field**");
    }
}
