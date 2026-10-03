using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using UniversalRPG.Rm2k.Parser;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And where a new game starts.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And <c>TrySetGold</c> is a debug tool behind
/// <c>_debugToolsEnabled</c></strong>, -- <strong>and a shop the player
/// cannot afford can only refuse</strong>, -- <strong>and the
/// measurement says the gold is zero on a fresh state.</strong>
/// </para>
/// <para>
/// <strong>And <c>RPG_RT.ini</c> carries no start gold</strong>, --
/// <strong>it holds the title and the editor's zoom and nothing
/// else.</strong> -- <strong>And the start is in the map tree.</strong>
/// </para>
/// </remarks>
public partial class TestRm2kStartwerteGemessen : TestBase
{
    private const string Map1 =
        "E:/RPGMakerGames/Dragon Destiny/Map0001.lmu";
    private const string Lmt =
        "E:/RPGMakerGames/Dragon Destiny/RPG_RT.lmt";
    private const string Ini =
        "E:/RPGMakerGames/Dragon Destiny/RPG_RT.ini";

    /// <summary>
    /// And the first map carries no start entry at all.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And my claim that it did was mine.</strong> --
    /// <strong>A new game therefore has no position and no money
    /// without the map tree</strong>, -- <strong>which is where the
    /// reference keeps them.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieErsteKarteTraegtKeinenStart()
    {
        if (!File.Exists(Map1))
        {
            AssertTrue(false, "**and the game's first map is"
                + " there**");

            return;
        }

        var parser = new Rm2kParser();
        var karte = parser.ParseMap(Map1);
        if (!karte.Success)
        {
            AssertTrue(false, "**and it is read**");
            return;
        }

        Console.WriteLine("Kartenschluessel: " + string.Join(" ",
            karte.Data.Keys.Select(x => x.ToString())
                .OrderBy(x => x)));

        AssertTrue(!karte.Data.ContainsKey("start"),
            "**and the map carries no start entry** -- and the"
                + " claim that it did was mine, and a new game"
                + " therefore has no position and no money"
                + " without the map tree, which is where the"
                + " reference keeps them");
    }

    /// <summary>
    /// And the map tree says where the party starts.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the measured block is smaller than the
    /// schema:</strong>
    /// </para>
    /// <code>
    /// active_node = 192      map_count = 744
    /// start = party_map_id=742  party_y=14
    /// unbenannte Startfelder: keine
    /// </code>
    /// <para>
    /// <strong>And <c>party_x</c> is absent and the unrecognised list
    /// is empty</strong>, -- <strong>which means the file really
    /// carries no start column.</strong> --
    /// <strong>Inventing one would be inventing a rule.</strong>
    /// </para>
    /// </remarks>
    public void Test_DerStartblockDesKartenbaums()
    {
        if (!File.Exists(Lmt))
        {
            AssertTrue(false, "**and the map tree is there**");
            return;
        }

        var parser = new Rm2kParser();
        var baum = parser.ParseMapTree(Lmt);
        if (!baum.IsSuccess())
        {
            AssertTrue(false, "**and it is read**");
            return;
        }

        var bd = baum.GetData();
        Console.WriteLine("active_node = "
            + bd["active_node"].ToString()
            + "  map_count = " + bd["map_count"].ToString());

        var start = bd["start"].AsGodotDictionary();
        Console.WriteLine("start = " + string.Join(" ",
            start.Keys.Select(x => x.ToString() + "="
                + start[x].ToString()).OrderBy(x => x)));

        var unbenannt = bd.ContainsKey("unknown_start_fields")
            ? ((Godot.Collections.Array<Godot.Collections.Dictionary>)
                bd["unknown_start_fields"]).Count : 0;
        Console.WriteLine("unbenannte Startfelder: "
            + unbenannt);

        AssertEq(744, bd["map_count"].AsInt32(),
            "**and the tree lists 744 maps** -- and the game has"
                + " 743 map files and the tree one more, which"
                + " is the game's own count");

        AssertEq(742, start["party_map_id"].AsInt32(),
            "**and the party starts on map 742** -- and that is"
                + " the game's own number, and a reader that"
                + " started on the first map instead would put"
                + " the player somewhere the game never chose");

        AssertEq(14, start["party_y"].AsInt32(),
            "**and on row 14** -- and the value is the game's and"
                + " not a default of this repository");

        AssertEq(0, unbenannt,
            "**and no unrecognised field hides the missing"
                + " column** -- and the parser reports its"
                + " unrecognised fields and that list is empty,"
                + " so the file really does not write a start"
                + " x");

        AssertTrue(!start.ContainsKey("party_x"),
            "**and the start block names no x** -- and a reader"
                + " that filled one in would place the party on a"
                + " column the game never chose");
    }

    /// <summary>
    /// And the ini names no gold.
    /// </summary>
    public void Test_DieIniHatKeinGold()
    {
        if (!File.Exists(Ini))
        {
            return;
        }

        var t = File.ReadAllText(Ini);
        AssertTrue(!t.Contains("Gold",
                StringComparison.OrdinalIgnoreCase),
            "**and the ini names no gold** -- and a reader that"
                + " took the start money from here would find"
                + " nothing and leave the party penniless");
    }
}
