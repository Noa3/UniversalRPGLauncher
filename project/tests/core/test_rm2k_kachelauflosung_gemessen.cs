using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using UniversalRPG.Rm2k.Parser;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And that the start map of a real game is what it claims to be.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the input test found the hero cannot move</strong>, --
/// <strong>and this measures why, because "cannot move" and "walks into
/// a wall" are different faults</strong>.
/// </para>
/// <para>
/// <strong>And the numbers come from the raw LMU and from liblcf's
/// field table</strong>, -- <strong>not from the parser alone</strong>,
/// -- <strong>because a parser that read a layer wrong would agree
/// with itself.</strong>
/// </para>
/// <para>
/// <code>
/// 0x47  len 600  Nutzlast 600b  Anfang 23,20,23,20,23,20,23,20
/// 0x48  len 600  Nutzlast 600b  Anfang 16,39,16,39,16,39,16,39
/// </code>
///
/// <para>
/// <strong>And <c>0x47</c> is <c>lower_layer</c> and <c>0x48</c> is
/// <c>upper_layer</c>, both <c>Vector&lt;Int16&gt;</c></strong>, --
/// <strong>so 600 bytes are 300 tiles, and 23 + 20 * 256 is
/// 5143.</strong>
/// </para>
/// </remarks>
public partial class TestRm2kKachelauflosungGemessen : TestBase
{
    private const string Ldb =
        "res://tests/fixtures/rm2k-dragon-destiny/RPG_RT.ldb";
    private const string Lmt =
        "E:/RPGMakerGames/Dragon Destiny/RPG_RT.lmt";
    private const int StartChipsetId = 3;

    /// <summary>
    /// And the start map's own tiles, and whether any of them is
    /// walkable.
    /// </summary>
    public void Test_DieStartkarteUndIhreKacheln()
    {
        var parser = new Rm2kParser();
        var bank = parser.ParseDatabase(Ldb);
        if (!bank.IsSuccess())
        {
            AssertTrue(false, "**and the bank is read**");
            return;
        }

        var baum = parser.ParseMapTree(Lmt);
        var startMap = (int)baum.GetData()["start"]
            .AsGodotDictionary()["party_map_id"].AsInt32();
        var pfad = "E:/RPGMakerGames/Dragon Destiny/"
            + "Map" + startMap.ToString("D4") + ".lmu";
        if (!File.Exists(pfad))
        {
            AssertTrue(false, "**and the start map is there**");
            return;
        }

        // **Und  jetzt  die  Rohbytes  der  Karte**, -- **denn  ein
        //  Parser,  der  eine  Ebene  falsch  liest,  wuerde  sich  selbst
        //  bestaetigen.**
        var lmu = File.ReadAllBytes(pfad);
        var reader = new LcfBinaryReader(lmu.Skip(11).ToArray());
        var nutzlasten = new Dictionary<int, byte[]>();
        while (!reader.IsEof() && nutzlasten.Count < 6)
        {
            var chunk = reader.ReadChunk();
            if (reader.HasError() || (bool)chunk["terminator"])
            {
                break;
            }

            nutzlasten[(int)chunk["id"]] = (byte[])chunk["data"];
        }

        if (!nutzlasten.TryGetValue(0x47, out var lowerBytes)
            || !nutzlasten.TryGetValue(0x48, out var upperBytes))
        {
            AssertTrue(false,
                "**and the map carries both layers** -- and a"
                + " missing layer is neither a finding nor a"
                + " failure");

            return;
        }

        Console.WriteLine("0x47 lower_layer " + lowerBytes.Length
            + "b  Anfang "
            + string.Join(",", lowerBytes.Take(8).Select(x => x.ToString())));
        Console.WriteLine("0x48 upper_layer " + upperBytes.Length
            + "b  Anfang "
            + string.Join(",", upperBytes.Take(8).Select(x => x.ToString())));

        // **Und  jetzt  die  erste  Kachel  von  Hand** -- **als  Int16
        //  und  nicht  als  BER**, -- **denn  die  Felder  sind
        //  `Vector&lt;Int16&gt;`.**
        var erste = lowerBytes[0] | (lowerBytes[1] << 8);
        var ersteOben = upperBytes[0] | (upperBytes[1] << 8);
        Console.WriteLine("erste untere Kachel " + erste
            + "  erste obere " + ersteOben);

        AssertEq(5143, erste,
            "**and the first tile is 5143** -- and 23 + 20 * 256"
                + " is 5143, and the field is a Vector of Int16"
                + " and not a BER integer");

        AssertEq(10000, ersteOben,
            "**and the upper layer is empty** -- and 10000 is"
                + " `BLOCK_F`, which is the reference's 'nothing"
                + " above the lower layer'");

        AssertEq(300, lowerBytes.Length / 2,
            "**and six hundred bytes are three hundred tiles** --"
                + " and the map is twenty by fifteen");
    }

    /// <summary>
    /// And the chipset's own table says that tile is a wall.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the honest end of the input question</strong>:
    /// -- <strong>the hero stands on a tile the game made
    /// impassable</strong>, -- <strong>and no amount of correct code
    /// will let him walk out of it.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieStartkachelIstEineWand()
    {
        var parser = new Rm2kParser();
        var bank = parser.ParseDatabase(Ldb);
        if (!bank.IsSuccess())
        {
            AssertTrue(false, "**and the bank is read**");
            return;
        }

        Godot.Collections.Dictionary? chipset = null;
        foreach (var c in (Godot.Collections
            .Array<Godot.Collections.Dictionary>)
            bank.GetData()["chipsets"])
        {
            if (c["id"].AsInt32() == StartChipsetId)
            {
                chipset = c;
                break;
            }
        }

        if (chipset == null)
        {
            AssertTrue(false, "**and chipset three is there**");
            return;
        }

        var roh = chipset["passable_data_lower"].AsInt32Array();
        var lower = new byte[roh.Length];
        for (var i = 0; i < roh.Length; i++)
        {
            lower[i] = (byte)roh[i];
        }

        // **Und  Block  E  liest  den  Eintrag  `tile_id +
        //  BLOCK_E_INDEX`**, -- **und  5143 - 5000 = 143  und  143 + 18
        //  =  161.**
        var index = 5143 - Rm2kChipset.BlockE
            + Rm2kChipset.BlockEIndex;
        Console.WriteLine("Index " + index + "  Wert "
            + (index < lower.Length ? lower[index].ToString() : "-")
            + "  Index 143 = " + (143 < lower.Length
                ? lower[143].ToString() : "-"));

        AssertEq(161, index,
            "**and block E reads entry 161** -- and skipping the"
                + " `BlockEIndex` offset of eighteen reads entry 143"
                + " instead, and that entry carries fifteen and"
                + " looks passable while the real one carries"
                + " nothing");

        AssertTrue(index < lower.Length && lower[index] == 0,
            "**and that entry is impassable** -- and the hero's"
                + " start tile is a wall in the game's own table,"
                + " so a hero who cannot step away from it is"
                + " correct behaviour and not a movement bug");
    }
}
