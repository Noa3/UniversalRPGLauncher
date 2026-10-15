using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using UniversalRPG.Rm2k;
using UniversalRPG.Rm2k.Parser;
using UniversalRPG.Rm2k.Rendering;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Looping maps: the LMU's <c>scroll_type</c>, the camera wrap and the movement wrap.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And four of the eight hundred and six owned maps loop</strong> -- measured with
/// a scan of every LMU, <c>scroll_type</c> 0 for 802 of them and 3 (both directions) for
/// <c>Dragon Destiny/Map0196</c> and <c>Lisa/Map0039</c>, <c>Map0040</c> and
/// <c>Map0042</c>. The chunk was read by the parser and never surfaced, so
/// <c>Rm2kMapCamera</c>'s loop parameters and its <c>PositiveModulo</c> were dead code and
/// a player on one of those maps walked into an invisible wall at the edge.
/// </para>
/// <para>
/// <strong>And the reference wraps in three places, not one</strong>:
/// <c>Game_Map::SetPositionX</c> for the camera, <c>Game_Map::RoundX</c> before every
/// passability and terrain lookup, and <c>Game_Character::Move</c> for the candidate
/// position of every step -- player and events alike. A reader that wrapped only the
/// camera would show the world around but refuse to walk around it.
/// </para>
/// </remarks>
public partial class TestRm2kLoopMaps : TestBase
{
    private const string Spiel = "E:/RPGMakerGames/Dragon Destiny";

    private const string Lisa = "E:/RPGMakerGames/Lisa";

    private static byte[] MapWithScrollType(int pScrollType, int pWidth = 5, int pHeight = 5)
    {
        var bytes = new List<byte>();
        var header = Encoding.ASCII.GetBytes("LcfMapUnit");
        bytes.AddRange(TestRm2kParser.Ber(header.Length));
        bytes.AddRange(header);
        bytes.AddRange(TestRm2kParser.Chunk(0x01, TestRm2kParser.Ber(1)));
        bytes.AddRange(TestRm2kParser.Chunk(0x02, TestRm2kParser.Ber(pWidth)));
        bytes.AddRange(TestRm2kParser.Chunk(0x03, TestRm2kParser.Ber(pHeight)));
        if (pScrollType != 0)
        {
            bytes.AddRange(TestRm2kParser.Chunk(0x0B, TestRm2kParser.Ber(pScrollType)));
        }
        var lower = new byte[pWidth * pHeight * 2];
        var upper = new byte[lower.Length];
        for (var index = 0; index < lower.Length; index += 2)
        {
            lower[index] = (byte)(5000 & 255); lower[index + 1] = (byte)(5000 >> 8);
            upper[index] = (byte)(10000 & 255); upper[index + 1] = (byte)(10000 >> 8);
        }
        bytes.AddRange(TestRm2kParser.Chunk(0x47, lower));
        bytes.AddRange(TestRm2kParser.Chunk(0x48, upper));
        bytes.Add(0);
        return bytes.ToArray();
    }

    private static Rm2kParser.ParseResult ParseBytes(byte[] pBytes)
    {
        var directory = Path.Combine(
            Path.GetTempPath(), "urpg-loop-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "Map0001.lmu");
        File.WriteAllBytes(path, pBytes);
        try
        {
            return new Rm2kParser().ParseMap(path);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    /// <summary>
    /// The parser reports the map's scroll type, and zero when the chunk is absent.
    /// </summary>
    public void Test_DerParserMeldetDenScrollType()
    {
        var looping = ParseBytes(MapWithScrollType(3));
        AssertTrue(looping.Success,
            $"the looping map parsed: {looping.Error?.Describe() ?? "no error"}");
        AssertEq((int)looping.Data["scroll_type"], 3,
            "**and the map reports both directions**");

        var plain = ParseBytes(MapWithScrollType(0));
        AssertTrue(plain.Success, "and a map without the chunk parsed");
        AssertEq((int)plain.Data["scroll_type"], 0,
            "**and it reports no looping at all**, which is the format's default");

        var horizontal = ParseBytes(MapWithScrollType(2));
        AssertEq((int)horizontal.Data["scroll_type"], 2,
            "and a horizontal map reports bit one");
    }

    /// <summary>
    /// The wrap happens on a looping map and nowhere else.
    /// </summary>
    public void Test_DieRundungWickeltNurAufLoopendenKarten()
    {
        var state = new GameSimulationState();
        state.ConfigureMap(1, 5, 4, new bool[20]);
        AssertEq(state.RoundX(-1), -1,
            "**a map that does not loop leaves a column alone**, which is what puts the"
                + " wall at its edge");
        AssertEq(state.RoundY(4), 4);

        state.ScrollType = 2;
        AssertEq(state.RoundX(5), 0, "**and a horizontal loop wraps the fifth column**");
        AssertEq(state.RoundX(-1), 4, "**and the one before the first**");
        AssertEq(state.RoundY(4), 4, "**and leaves the rows alone**");

        state.ScrollType = 3;
        AssertEq(state.RoundY(4), 0, "**and both wraps the fourth row**");
        AssertEq(state.RoundY(-1), 3);
    }

    /// <summary>
    /// A step across a looping map's edge is a step, and one across a wall is not.
    /// </summary>
    /// <remarks>
    /// <strong>And this is the invisible wall the slice removes</strong>: with every tile
    /// passable, a player at the last column of a looping map walks onto the first column
    /// instead of being refused by the map's bounds.
    /// </remarks>
    public void Test_EinSchrittUeberDenRandIstAufEinerLoopendenKarteErlaubt()
    {
        var state = new GameSimulationState();
        // **Und alle Kacheln sind begehbar**: sonst verweigert die Passierbarkeit den
        // Schritt und der Test prueft die Kartengrenze nicht mehr.
        var passable = new bool[20];
        Array.Fill(passable, true);
        state.ConfigureMap(1, 5, 4, passable);
        state.MapX = 4;
        state.MapY = 0;

        AssertTrue(!state.TryMove(1, 0),
            "**without the loop the step is refused by the map's bounds**");
        AssertEq(state.MapX, 4, "and the player stays where it was");

        state.ScrollType = 2;
        AssertTrue(state.TryMove(1, 0),
            "**and with the loop it is a step around the world**");
        AssertEq(state.MapX, 0, "**and the player lands on the first column**");
        AssertEq(state.MapY, 0, "and keeps its row");

        AssertTrue(state.TryMove(-1, 0), "and the way back works too");
        AssertEq(state.MapX, 4, "**and lands on the last column**");
    }

    /// <summary>
    /// The camera wraps on a looping map, which is what makes the world look round.
    /// </summary>
    public void Test_DieKameraWickeltAufEinerLoopendenKarte()
    {
        // Der Standard-Pan ist neun Kacheln; ein Spieler am linken Rand einer
        // loopenden Karte hat die Kamera davor, also wickelt sie ans Ende.
        var withoutLoop = Rm2kMapCamera.PositionX(0, 20, 320, false);
        AssertEq(withoutLoop, 0,
            "**a map that does not loop keeps its camera at the left edge**");

        var looping = Rm2kMapCamera.PositionX(0, 20, 320, true);
        AssertEq(looping, Rm2kMapCamera.PositiveModulo(
            -Rm2kMapCamera.DefaultPanX(), 20 * Rm2kMapCamera.ScreenTileSize),
            "**and a looping one wraps it to the far end**, from Game_Map::SetPositionX");

        AssertEq(Rm2kMapCamera.PositionY(0, 15, 240, true),
            Rm2kMapCamera.PositiveModulo(
                -Rm2kMapCamera.DefaultPanY(), 15 * Rm2kMapCamera.ScreenTileSize),
            "**and the same vertically**");
    }

    /// <summary>
    /// The four looping maps of the owned collection report their type.
    /// </summary>
    /// <remarks>
    /// <strong>And this is the measurement the slice is for</strong>: the scan found
    /// <c>scroll_type</c> 0 for 802 of 806 maps and 3 for these four. The test reads the
    /// four files rather than rescanning the collection, so it stays a check of the
    /// parser and not a five minute walk.
    /// </remarks>
    public void Test_DieVierLoopendenMapsDesKorpus()
    {
        var maps = new[]
        {
            (Path.Combine(Spiel, "Map0196.lmu"), "Dragon Destiny/Map0196"),
            (Path.Combine(Lisa, "Map0039.lmu"), "Lisa/Map0039"),
            (Path.Combine(Lisa, "Map0040.lmu"), "Lisa/Map0040"),
            (Path.Combine(Lisa, "Map0042.lmu"), "Lisa/Map0042"),
        };
        var gesehen = 0;
        foreach (var (path, name) in maps)
        {
            if (!File.Exists(path))
            {
                continue;
            }
            var parsed = new Rm2kParser().ParseMap(path);
            AssertTrue(parsed.Success,
                $"{name} parsed: {parsed.Error?.Describe() ?? "no error"}");
            AssertEq((int)parsed.Data["scroll_type"], 3,
                $"**and {name} loops in both directions**");
            gesehen++;
        }
        AssertTrue(gesehen == 0 || gesehen == 4,
            "**and either all four of the measured maps are here or none are**, so a"
                + " missing game cannot make this test look green by skipping; found "
                + gesehen);
    }
}
