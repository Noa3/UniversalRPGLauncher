using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniversalRPG.Rm2k.Parser;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And that the cells say where to draw.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And a battle animation that the state knows but nobody draws
/// is not an animation.</strong> --
/// <strong>And this proves the cells carry a position and a cell
/// id.</strong>
/// </para>
/// </remarks>
public partial class TestRm2kAnimationszellenSindEcht : TestBase
{
    private const string Ldb =
        "res://tests/fixtures/rm2k-dragon-destiny/RPG_RT.ldb";

    /// <summary>
    /// And animation five, every frame, every cell.
    /// </summary>
    public void Test_DieZellenSagenWoZeichnen()
    {
        var parser = new Rm2kParser();
        var result = parser.ParseDatabase(Ldb);
        if (!result.IsSuccess())
        {
            AssertTrue(false, "**and the bank is read**");
            return;
        }

        var anims = (Godot.Collections
            .Array<Godot.Collections.Dictionary>)
            result.GetData()["animations"];

        var zellenGesamt = 0;
        var mitZelle = 0;
        var mitPosition = 0;
        foreach (var a in anims)
        {
            if (!a.ContainsKey("unknown_fields"))
            {
                continue;
            }

            var ok = Rm2kAnimationZelle.TryDecode(
                (Godot.Collections.Array<Godot.Collections.Dictionary>)
                a["unknown_fields"], out var frames, out var warum);
            if (!ok)
            {
                continue;
            }

            foreach (var frame in frames)
            {
                foreach (var z in frame)
                {
                    zellenGesamt++;
                    if (z["cell_id"].AsInt32() > 0)
                    {
                        mitZelle++;
                    }

                    if (z["x"].AsInt32() != 0
                        || z["y"].AsInt32() != 0)
                    {
                        mitPosition++;
                    }
                }
            }

            if (a["id"].AsInt32() == 5)
            {
                Console.WriteLine("Animation 5: "
                    + frames.Count + " Frames");
                for (var f = 0; f < frames.Count && f < 3; f++)
                {
                    var teile = new List<string>();
                    foreach (var z in frames[f])
                    {
                        teile.Add($"zelle {z["cell_id"]}"
                            + $" ({z["x"]},{z["y"]})"
                            + $" zoom {z["zoom"]}"
                            + $" ton {z["tone_red"]}/"
                            + $"{z["tone_green"]}/"
                            + $"{z["tone_blue"]}"
                            + $" tr {z["transparency"]}");
                    }

                    Console.WriteLine("  Frame " + f + ": "
                        + string.Join("  ", teile));
                }
            }
        }

        Console.WriteLine("Zellen gesamt " + zellenGesamt
            + "  mit Zelle " + mitZelle
            + "  mit Position " + mitPosition);

        AssertTrue(zellenGesamt > 0,
            "**and the game's animations carry cells**");

        AssertTrue(mitZelle > 0,
            "**and the cells name a chipset cell** -- and a"
                + " renderer cannot draw a cell without one");

        AssertTrue(mitPosition > 0,
            "**and the cells carry an offset** -- and a cell at"
                + " the origin for every animation would mean the"
                + " reader had dropped the x and y fields");
    }
}
