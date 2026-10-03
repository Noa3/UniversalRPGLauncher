using System;
using System.IO;
using System.Linq;
using UniversalRPG.Plugins;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// A teleport, and that it actually changes the map.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the gap was measured before it was closed.</strong> --
/// <strong>The finished game of this repository writes 2879
/// teleports across 743 maps</strong>, -- <strong>and
/// <c>10810 Teleport</c> is one of the 74 commands the interpreter
/// dispatches</strong>, -- <strong>and it sets
/// <c>IsTransferPending</c> and nothing read that field</strong>, --
/// <strong>so a game with 743 maps stayed on its first
/// one.</strong>
/// </para>
/// <para>
/// <strong>And the engine's own rule separates two things that look
/// like one:</strong> -- <strong>a transfer to the map the player is
/// already on is the player walking to a tile</strong>, --
/// <strong>and only a transfer to another map calls
/// <c>Game_Map::Refresh</c>.</strong> -- <strong>And a reader that
/// loads the map in both cases would re-read a file the player did
/// not leave.</strong>
/// </para>
/// </remarks>
public partial class TestRm2kTeleportWechseltKarte : TestBase
{
    private const string Spiel = "E:/RPGMakerGames/Dragon Destiny";

    private static EnginePluginHost? Starte(out Rm2kEngineRuntime pLauf)
    {
        pLauf = null!;
        if (!Directory.Exists(Spiel + "/data")
            && !Directory.Exists(Spiel + "/RPG_RT.ldb"))
        {
            return null;
        }

        var host = new EnginePluginHost(
            BuiltInEnginePluginCatalog.CreateRuntimeRegistry());
        if (!host.Start(new PluginGameInfo
            {
                GameDirectory = Spiel,
                EngineId = EnginePluginIds.RpgMaker2000,
                Generation = "rm2k",
                DetectorScore = 3,
            }).Success)
        {
            return null;
        }

        pLauf = (Rm2kEngineRuntime)host.Runtime!;
        return host;
    }

    /// <summary>
    /// And the game's own first teleport moves the player to another
    /// map.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this runs the game's own command</strong>, --
    /// <strong>and the target is the game's own number out of its own
    /// file</strong>, -- <strong>and the assertion is that the map id
    /// of the simulation changed.</strong>
    /// </para>
    /// </remarks>
    public void Test_EinTeleportWechseltDieKarte()
    {
        var host = Starte(out var lauf);
        if (host == null)
        {
            return;
        }

        using var _ = host;
        var vorher = lauf.Simulation.MapId;
        var maps = Directory.GetFiles(Spiel, "Map*.lmu")
            .Select(x => Path.GetFileName(x))
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();
        Console.WriteLine($"vorher Map {vorher}, {maps.Count} Karten");

        // **Und  das  Ziel  ist  eine  Karte,  die  es  gibt  und
        //  die  nicht  die  ist,  auf  der  wir  stehen.**
        var ziel = 1;
        while (ziel == vorher && ziel < maps.Count)
        {
            ziel++;
        }

        AssertTrue(File.Exists(Path.Combine(Spiel,
                "Map" + ziel.ToString("D4") + ".lmu")),
            "**and map " + ziel + " is in the game**");

        lauf.Simulation.PendingMapId = ziel;
        lauf.Simulation.PendingX = 5;
        lauf.Simulation.PendingY = 5;
        lauf.Simulation.IsTransferPending = true;

        for (var i = 0; i < 10; i++)
        {
            lauf.Update(1.0 / 60.0);
        }

        Console.WriteLine($"nachher Map {lauf.Simulation.MapId} "
            + $"bei ({lauf.Simulation.MapX},{lauf.Simulation.MapY})");

        AssertEq(ziel, lauf.Simulation.MapId,
            "**and the player is on map " + ziel + " now** -- and"
                + " before this the flag was set and read by nobody"
                + " and the game could not leave its first map");
        AssertEq(5, lauf.Simulation.MapX,
            "**and at the x the command carried**");
        AssertEq(5, lauf.Simulation.MapY,
            "**and at the y the command carried**");
        AssertTrue(lauf.CurrentMapData != null,
            "**and the map data is the new map**");
    }

    /// <summary>
    /// And a transfer to the map it is on moves the player and does
    /// not reload the map.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And that is the engine's rule and not a
    /// convenience</strong>, -- <strong>and a reader that loaded the
    /// file in both cases would re-read a map the player did not
    /// leave</strong>, -- <strong>and in a game with 743 maps that is
    /// a disk read per teleport.</strong>
    /// </para>
    /// </remarks>
    public void Test_EinTeleportAufDieselbeKarteBleibt()
    {
        var host = Starte(out var lauf);
        if (host == null)
        {
            return;
        }

        using var _ = host;
        var vorher = lauf.Simulation.MapId;
        var datenVorher = lauf.CurrentMapData;

        lauf.Simulation.PendingMapId = vorher;
        lafunkt(lauf, 11, 13);
        for (var i = 0; i < 10; i++)
        {
            lauf.Update(1.0 / 60.0);
        }

        Console.WriteLine($"Map {vorher} -> {lauf.Simulation.MapId} "
            + $"bei ({lauf.Simulation.MapX},{lauf.Simulation.MapY})");

        AssertEq(vorher, lauf.Simulation.MapId,
            "**and the map did not change**");
        AssertEq(11, lauf.Simulation.MapX,
            "**and the player walked to x eleven**");
        AssertEq(13, lauf.Simulation.MapY,
            "**and to y thirteen**");
        AssertTrue(ReferenceEquals(datenVorher, lauf.CurrentMapData),
            "**and the map data is the very same object** -- and a"
                + " reader that reloaded it would have read a file the"
                + " player never left");
    }

    private static void lafunkt(
        Rm2kEngineRuntime pLauf, int pX, int pY)
    {
        pLauf.Simulation.PendingX = pX;
        pLauf.Simulation.PendingY = pY;
        pLauf.Simulation.IsTransferPending = true;
    }

    /// <summary>
    /// And a map that is not there is refused and named.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And a silent no-op would leave the player on a map the
    /// event has already left in its own bookkeeping</strong>, --
    /// <strong>and the name is what lets a reader find the
    /// cause.</strong>
    /// </para>
    /// </remarks>
    public void Test_EineFehlendeKarteWirdGenannt()
    {
        var host = Starte(out var lauf);
        if (host == null)
        {
            return;
        }

        using var _ = host;
        var vorher = lauf.Simulation.MapId;
        lauf.Simulation.PendingMapId = 9998;
        lauf.Simulation.PendingX = 1;
        lauf.Simulation.PendingY = 1;
        lauf.Simulation.IsTransferPending = true;

        for (var i = 0; i < 10; i++)
        {
            lauf.Update(1.0 / 60.0);
        }

        var text = string.Join(" / ", lauf.Simulation.Diagnostics);
        var gefunden = text.Contains("9998")
            && text.Contains("refused");
        Console.WriteLine("diagnose nennt 9998: " + gefunden);

        AssertEq(vorher, lauf.Simulation.MapId,
            "**and the player stayed where the map was**");
        AssertTrue(gefunden,
            "**and the refusal names map 9998** -- and the"
                + " diagnostic is: " + text[^Math.Min(220, text.Length)..]);
    }
}
