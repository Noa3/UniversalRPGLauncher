using System;
using System.Collections.Generic;
using System.IO;
using Godot;
using System.Linq;
using UniversalRPG.Rm2k.Parser;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Zero hit points, and whether that is the game's number or this
/// reader's.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is the question the last measurement raised.</strong>
/// --
/// <strong>Seventy of seventy two monsters carry a
/// <c>max_hp</c> key and six of them are above zero.</strong> --
/// <strong>That is either a game full of dead monsters or a reader
/// that stops reading.</strong>
/// </para>
/// <para>
/// <strong>And the answer is not a guess and not a preference for the
/// happy path.</strong> -- <strong>If the file really says zero, a
/// fight must not invent health.</strong> -- <strong>If the reader
/// stops, sixty six monsters are unusable.</strong>
/// </para>
/// </remarks>
public partial class TestRm2kMonsterHpIstNullOderEcht : TestBase
{
    private const string Ldb =
        "res://tests/fixtures/rm2k-dragon-destiny/RPG_RT.ldb";

    private static Godot.Collections.Array<Godot.Collections.Dictionary>
        Monster()
    {
        var parser = new Rm2kParser();
        var result = parser.ParseDatabase(Ldb);
        if (!result.IsSuccess())
        {
            return new Godot.Collections
                .Array<Godot.Collections.Dictionary>();
        }

        return (Godot.Collections.Array<Godot.Collections
            .Dictionary>)result.GetData()["enemies"];
    }

    /// <summary>
    /// And the whole list of hit points, in order.
    /// </summary>
    public void Test_DieGanzenTrefferpunkteInReihenfolge()
    {
        var monster = Monster();
        if (monster.Count == 0)
        {
            return;
        }

        var werte = new List<int>();
        for (var i = 0; i < monster.Count; i++)
        {
            werte.Add(monster[i].ContainsKey("max_hp")
                ? monster[i]["max_hp"].AsInt32() : -1);
        }

        Console.WriteLine("HP: " + string.Join(",", werte));

        var null_ = werte.Count(x => x == 0);
        var positiv = werte.Count(x => x > 0);
        var fehlend = werte.Count(x => x < 0);
        Console.WriteLine($"null {null_}  positiv {positiv}"
            + $"  fehlend {fehlend}");

        // **Und  das  Muster  ist  die  Frage.**  --
        // **Wenn  die  Nullen  hinten  stehen  und  die  Positiven
        // vorn,  ist  es  der  Decoder.**  --
        // **Wenn  sie  durcheinander  stehen,  ist  es  das
        // Spiel.**
        var ersterNull = werte.FindIndex(x => x == 0);
        var letzterPositiv = werte.FindLastIndex(x => x > 0);
        Console.WriteLine($"erste 0: #{ersterNull}   "
            + $"letztes >0: #{letzterPositiv}");
    }

    /// <summary>
    /// And whether the raw bytes hold the numbers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this looks at the file instead of the
    /// parser</strong>, -- <strong>because the question is where the
    /// zero comes from and only the file answers that.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieRohbytesDesMonsterteils()
    {
        var pfad = ProjectSettings.GlobalizePath(Ldb);
        if (!File.Exists(pfad))
        {
            return;
        }

        var bytes = File.ReadAllBytes(pfad);
        Console.WriteLine($"ldb {bytes.Length} Bytes");

        // **Und  die  Signatur  der  Kapsel  --  0x0e  ist  die
        //  Kapsel  "enemies"  laut  der  eigenen  Tabelle.**
        var zeilen = new List<string>();
        for (var off = 0; off + 4 < bytes.Length && off < 40000; off++)
        {
            if (bytes[off] == 0x0e
                && off > 1000 && bytes[off + 1] == 0x01)
            {
                zeilen.Add("0x0e @ " + off);
            }

            if (zeilen.Count > 4)
            {
                break;
            }
        }

        Console.WriteLine("Kapselmarker: "
            + (zeilen.Count == 0 ? "(keine)" : string.Join(", ", zeilen)));

        AssertTrue(bytes.Length > 100000,
            "**and the file is the real one**");
    }
}
