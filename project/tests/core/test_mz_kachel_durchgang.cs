using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using UniversalRPG.Mz;
using UniversalRPG.Plugins;
using UniversalRPG.Tests.Framework;
using UniversalRPG.Web;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And whether a tile lets the player through, read the way both web
/// engines answer it.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the whole rule is twenty lines in the engine, and it is not
/// the RM2K rule.</strong> Measured in
/// <c>rmmz_objects.js</c> and <c>rpg_objects.js</c> -- both engines carry
/// the identical code:
/// <c>checkPassage(x, y, bit)</c> walks every tile on the cell,
/// skips any whose flag has <c>0x10</c> set (<em>"*", no effect on
/// passage</em>), and returns <c>true</c> on the first flag where the
/// requested bit is <em>clear</em>.
/// </para>
/// <para>
/// <strong>And the flags live in one array, not in seven.</strong>
/// <c>Game_Map.prototype.tilesetFlags()</c> returns
/// <c>$dataTilesets[id].flags</c> -- 8192 entries in every tileset of
/// every game measured, and one integer per tile id.
/// </para>
/// </remarks>
public partial class TestMzKachelDurchgang : TestBase
{
    private const string MzSpiel = "E:/RPGMakerGames/CamelliaCoronation-Win";
    private const string MvSpiel = "E:/RPGMakerGames/LegalTruck_v1.1/www";

    /// <summary>And the flag array is where the engine reads it from.</summary>
    public void Test_DieFlagKommtAusDemTilesetUndNichtAusDemBild()
    {
        foreach (var spiel in new[] { MzSpiel, MvSpiel })
        {
            if (!File.Exists(spiel + "/data/Tilesets.json"))
            {
                continue;
            }
            var tilesets = MzDataFile.Read("data/Tilesets.json",
                File.ReadAllBytes(spiel + "/data/Tilesets.json"));
            var gefunden = 0;
            // **Und ein leerer Tileset-Eintrag wird uebersprungen** --
            // **`Tilesets.json` fuehrt einen Platzhalter an Index 0 und
            // laesst Luecken, wo der Editor ein Tileset geloescht hat**,
            // **und ein Leser, der Index 0 wie ein Tileset behandelt,
            // meldet "keine Flags" fuer ein Spiel, das sechs hat.**
            foreach (var eintrag in tilesets.Root.Items ?? [])
            {
                if (eintrag.Items == null
                    || eintrag.Member("name")?.StringOr("") is not { Length: > 0 } name)
                {
                    continue;
                }
                var flags = eintrag.Member("flags")?.Items;
                AssertTrue(flags != null && flags.Count >= 4096,
                    $"{Path.GetFileName(spiel)} tileset '{name}' carries its flags: {flags?.Count}");
                if (flags == null)
                {
                    continue;
                }
                gefunden += 1;
                AssertTrue(flags.Any(pFlag => pFlag.IntOr(0) == MzKachelDurchgang.KeinEffekt),
                    $"tileset '{name}' uses the '*' no-effect flag somewhere");
            }
            AssertTrue(gefunden > 0, $"{spiel} has readable tilesets");
        }
    }

    /// <summary>And a star-flagged tile never decides anything.</summary>
    /// <remarks>
    /// <para>
    /// <strong>And "never decides" is not "always allows".</strong> The
    /// engine skips a <c>*</c> tile with <c>continue</c>, and a cell made
    /// only of <c>*</c> tiles reaches <c>return false</c> -- impassable.
    /// <strong>A test that read the skip as an allow would have made the
    /// hero walk through decorative stars.</strong>
    /// </para>
    /// </remarks>
    public void Test_EinSternFlagEntscheidetNichts()
    {
        foreach (var richtung in MzKachelDurchgang.Richtungen)
        {
            AssertFalse(MzKachelDurchgang.IstBegehbar(
                    new[] { MzKachelDurchgang.KeinEffekt }, richtung),
                $"a cell of only star tiles refuses {richtung}");
            AssertTrue(MzKachelDurchgang.IstBegehbar(
                    new[] { MzKachelDurchgang.KeinEffekt, 0 }, richtung),
                $"a star tile does not stop a passable tile from allowing {richtung}");
        }
    }

    /// <summary>And a wall in one direction refuses only that direction.</summary>
    public void Test_EineWandSperrtNurIhreRichtung()
    {
        var unten = new[] { MzKachelDurchgang.Bit(MzKachelDurchgang.Unten) };
        AssertFalse(MzKachelDurchgang.IstBegehbar(unten, MzKachelDurchgang.Unten),
            "a tile with the down bit set refuses down");
        AssertTrue(MzKachelDurchgang.IstBegehbar(unten, MzKachelDurchgang.Oben),
            "the same tile still allows up");
        AssertTrue(MzKachelDurchgang.IstBegehbar(unten, MzKachelDurchgang.Rechts),
            "the same tile still allows right");
    }

    /// <summary>And the first tile that can answer ends the question.</summary>
    /// <remarks>
    /// <para>
    /// <strong>And it ends the question either way.</strong> The engine
    /// <c>return</c>s inside the loop, not at the end of it, so a closed
    /// tile below a floor tile does not get overruled by the floor. The
    /// layers arrive top to bottom, which is the order
    /// <c>layeredTiles</c> walks.
    /// </para>
    /// </remarks>
    public void Test_DieErsteBewertbareKachelEntscheidet()
    {
        var geschlossen = MzKachelDurchgang.Bit(MzKachelDurchgang.Unten);
        AssertFalse(MzKachelDurchgang.IstBegehbar(
                [geschlossen, 0], MzKachelDurchgang.Unten),
            "the closed upper layer decides, and the floor below cannot allow it");
        AssertTrue(MzKachelDurchgang.IstBegehbar(
                [0, geschlossen], MzKachelDurchgang.Unten),
            "the passable upper layer decides before the closed floor is read");
        // The star is skipped and the closed tile below then decides -- which
        // means it closes the cell, because it is the first tile that can
        // answer at all.
        AssertFalse(MzKachelDurchgang.IstBegehbar(
                [MzKachelDurchgang.KeinEffekt, geschlossen], MzKachelDurchgang.Unten),
            "a star above a closed tile still leaves the cell closed");
        AssertTrue(MzKachelDurchgang.IstBegehbar(
                [MzKachelDurchgang.KeinEffekt, geschlossen], MzKachelDurchgang.Oben),
            "and the same cell is open upwards, because the star did not block it");
        // 0x01 | 0x10 is 0x11, and the engine tests 0x10 first, so a tile
        // carrying both is a star and says nothing at all.
        AssertFalse(MzKachelDurchgang.IstBegehbar(
                [geschlossen | MzKachelDurchgang.KeinEffekt], MzKachelDurchgang.Unten),
            "a tile carrying both the star and a direction bit is a star,"
            + " because the engine tests 0x10 before anything else");
    }

    /// <summary>And an empty cell is not walkable, as in the engine.</summary>
    public void Test_EineLeereZelleIstNichtBegehbar()
    {
        // `checkPassage` returns false when the loop finds nothing that
        // allows it -- and a cell with no tiles at all is that case.
        AssertFalse(MzKachelDurchgang.IstBegehbar([], MzKachelDurchgang.Unten),
            "an empty cell refuses the step");
    }

    /// <summary>And the four directions map onto the four low bits.</summary>
    public void Test_DieVierRichtungenNutzenDieVierNiedrigenBits()
    {
        // `isPassable(x, y, d)` is `(1 << (d / 2 - 1)) & 0x0f` with the
        // engine's own direction numbers 2=down, 4=left, 6=right, 8=up.
        AssertEq(MzKachelDurchgang.Bit(MzKachelDurchgang.Unten), 0x01, "down uses bit 0");
        AssertEq(MzKachelDurchgang.Bit(MzKachelDurchgang.Links), 0x02, "left uses bit 1");
        AssertEq(MzKachelDurchgang.Bit(MzKachelDurchgang.Rechts), 0x04, "right uses bit 2");
        AssertEq(MzKachelDurchgang.Bit(MzKachelDurchgang.Oben), 0x08, "up uses bit 3");
    }

    /// <summary>And the real start tile of the real game answers.</summary>
    public void Test_DieEchteStartkamelDesSpielsAntwortet()
    {
        if (!File.Exists(MzSpiel + "/data/Tilesets.json"))
        {
            return;
        }
        var tilesets = MzDataFile.Read("data/Tilesets.json",
            File.ReadAllBytes(MzSpiel + "/data/Tilesets.json"));
        var system = MzDataFile.Read("data/System.json",
            File.ReadAllBytes(MzSpiel + "/data/System.json"));
        var startId = system.Root.Member("startMapId")?.IntOr(0) ?? 0;
        AssertTrue(startId > 0, "the game names a start map");
        var map = MzDataFile.Read($"data/Map{startId:000}.json",
            File.ReadAllBytes($"{MzSpiel}/data/Map{startId:000}.json"));
        var tilesetId = map.Root.Member("tilesetId")?.IntOr(0) ?? 0;
        var flags = tilesets.Root.Items![tilesetId].Member("flags")?.Items;
        AssertTrue(flags != null, $"tileset {tilesetId} of the start map has flags");
        if (flags == null)
        {
            return;
        }
        var breite = map.Root.Member("width")?.IntOr(0) ?? 0;
        var daten = map.Root.Member("data")?.Items;
        AssertTrue(breite > 0 && daten != null, "the start map is readable");

        // The player's own tile, from the engine's placement.
        var px = system.Root.Member("startX")?.IntOr(0) ?? 0;
        var py = system.Root.Member("startY")?.IntOr(0) ?? 0;
        var schicht = new List<int>();
        for (var z = 3; z >= 0; z--)
        {
            var index = z * breite * map.Root.Member("height")!.IntOr(0) + py * breite + px;
            if (index >= 0 && index < daten.Count)
            {
                schicht.Add(daten[index].IntOr(0));
            }
        }
        Console.WriteLine($"MZ start {px}/{py} on map {startId}, tiles={string.Join(",", schicht)}");
        AssertTrue(schicht.Count > 0, "the start tile carries tile numbers");
        var cellFlags = schicht
            .Select(pKachel => MzKachelDurchgang.FlagFuer(flags, pKachel))
            .ToArray();
        var begehbar = MzKachelDurchgang.IstBegehbar(cellFlags, MzKachelDurchgang.Unten);
        Console.WriteLine("start tile flags=" + string.Join(",", cellFlags)
            + $" down-passable={begehbar}");
        AssertTrue(begehbar,
            "the game's own start tile lets the player step down, or the player"
            + " starts inside a wall and every direction is refused");
    }
}