using System;
using System.Collections.Generic;
using System.Linq;
using UniversalRPG.Rm2k.Parser;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And a troop page's bytes in full, and what a reader can make of
/// them.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And liblcf says a <c>TroopPage</c> has two fields:</strong>
///
/// <code>
/// condition        0x02
/// event_commands   0x0B   Vector&lt;EventCommand&gt;   (Integer)
/// event_commands   0x0C   Vector&lt;EventCommand&gt;   (Array)
/// </code>
///
/// <para>
/// <strong>And this prints the bytes and tries the page's own event
/// command reader on the field</strong>, -- <strong>because whether
/// field <c>0x0B</c> is the commands themselves or a list of indices
/// into <c>0x0C</c> decides whether a reader exists at
/// all.</strong>
/// </para>
/// </remarks>
public partial class TestRm2kTruppenseiteBytesVollstaendig : TestBase
{
    private const string Ldb =
        "res://tests/fixtures/rm2k-dragon-destiny/RPG_RT.ldb";

    /// <summary>
    /// And the bytes of several pages, and the reader's answer.
    /// </summary>
    public void Test_DieBytesUndDerLeser()
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
        var gezeigt = 0;
        for (var i = 0; i < truppen.Count && gezeigt < 3; i++)
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
                gezeigt++;
                Console.WriteLine("Trupe #"
                    + truppen[i]["id"].AsInt32() + " ("
                    + truppen[i]["name"].AsString() + ") "
                    + daten.Length + "b:");
                Console.WriteLine("  " + string.Join(",", daten));

                // **Und  jetzt  der  Befehlsleser  auf  denselben
                //  Bytes** -- **und  das  ist  die  entscheidende
                //  Frage.**
                var dekodiert = Rm2kEventCommandDecoder.Decode(daten);
                Console.WriteLine("  als Befehle: "
                    + (dekodiert.Success
                        ? dekodiert.Data["count"].AsInt32().ToString()
                        : "nein: " + dekodiert.Error?.Describe()));
            }
        }

        AssertEq(3, gezeigt,
            "**and three pages were printed**");
    }
}
