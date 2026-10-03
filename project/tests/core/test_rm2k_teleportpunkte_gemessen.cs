using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniversalRPG.Plugins;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And the warp points a save would lose.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And <c>11810 Teleport Targets</c> is written 2879 times</strong>,
/// -- <strong>and the targets belong to the game and not to the
/// map</strong>, -- <strong>and the codec does not save
/// them.</strong>
/// </para>
/// <para>
/// <strong>And a save that drops the warp points reloads a game whose
/// warps the author wrote are not there.</strong> --
/// <strong>And a warp that needs a switch to be open is the sharpest
/// case, because dropping it makes a secret entrance vanish
/// entirely.</strong>
/// </para>
/// </remarks>
public partial class TestRm2kTeleportpunkteGemessen : TestBase
{
    private const string Spiel = "E:/RPGMakerGames/Dragon Destiny";

    private static GameSimulationState? EchterZustand(
        out EnginePluginHost? pHost)
    {
        pHost = null;
        if (!File.Exists(Spiel + "/RPG_RT.ldb"))
        {
            return null;
        }

        pHost = new EnginePluginHost(
            BuiltInEnginePluginCatalog.CreateRuntimeRegistry());
        if (!pHost.Start(new PluginGameInfo
            {
                GameDirectory = Spiel,
                EngineId = EnginePluginIds.RpgMaker2000,
                Generation = "rm2k",
                DetectorScore = 3,
            }).Success)
        {
            pHost.Dispose();
            pHost = null;
            return null;
        }

        return ((Rm2kEngineRuntime)pHost.Runtime!).Simulation;
    }

    /// <summary>
    /// And the state's own warp table.
    /// </summary>
    public void Test_DieTeleportpunkteDesZustands()
    {
        var state = EchterZustand(out var host);
        if (state == null)
        {
            return;
        }

        using var h = host!;
        var punkte = state.TeleportTargets;
        Console.WriteLine("Teleportpunkte: " + punkte.Count
            + " Karten");

        AssertTrue(punkte != null,
            "**and the state keeps the game's warp points**");

        // **Und  ein  Schalter-abhaengiger  Punkt  wird  getestet**,
        // -- **denn  das  ist  der  Fall,  den  ein  stiller
        //  Datenverlust  am  meisten  trifft.**
        // **Und  `TeleportTargets`  ist  `Dictionary<int,
        //  List<TeleportTarget>>`** -- **und  das  ist  genau  die
        //  Form,  die  `JsonSerializer`  als  leeres  Objekt  schreibt**,
        // -- **und  es  ist  dieselbe  Form,  die  die  gelernten
        //  Faehigkeiten  im  Spielstand  verloren  hat.**
        state.TeleportTargets[1] = new List<GameSimulationState.TeleportTarget>
        {
            new GameSimulationState.TeleportTarget
            {
                MapId = 500,
                X = 12,
                Y = 34,
                RequiresSwitchOn = true,
                SwitchId = 77,
            },
            new GameSimulationState.TeleportTarget
            {
                MapId = 500,
                X = 13,
                Y = 34,
                RequiresSwitchOn = false,
                SwitchId = 0,
            },
        };
        Console.WriteLine("nach dem Setzen: "
            + state.TeleportTargets.Count + " Karten, "
            + state.TeleportTargets[1].Count + " Punkte");

        var json = Rm2kSimulationSaveCodec.Serialize(state);
        Console.WriteLine("JSON nennt TeleportTargets: "
            + json.Contains("TeleportTargets"));

        var geladen = new GameSimulationState();
        var ok = Rm2kSimulationSaveCodec.TryRestore(
            json, geladen, out var fehler);
        Console.WriteLine("Restore: " + ok + " -> " + fehler
            + "  Punkte zurueck: "
            + geladen.TeleportTargets.Count);

        AssertTrue(ok,
            "**and the save is read back** -- and: " + fehler);

        Console.WriteLine("Karten zurueck: "
            + geladen.TeleportTargets.Count);

        AssertEq(1, geladen.TeleportTargets.Count,
            "**and the warp survives** -- and a save that drops"
                + " it reloads a game whose warps the author"
                + " wrote are gone");

        Console.WriteLine("Punkte zurueck: "
            + geladen.TeleportTargets[1].Count + "  Schalter "
            + geladen.TeleportTargets[1][0].RequiresSwitchOn);

        AssertEq(2, geladen.TeleportTargets[1].Count,
            "**and both points survive** -- and a save that"
                + " dropped one would leave a warp the author"
                + " wrote unusable");

        AssertTrue(geladen.TeleportTargets[1][0]
            .RequiresSwitchOn,
            "**and the switch requirement survives** -- and the"
                + " flag means \"the switch must be on\", and"
                + " a reader that read it as \"use a switch\" would"
                + " make a secret entrance open at the start of"
                + " the game");

        AssertEq(77, geladen.TeleportTargets[1][0].SwitchId,
            "**and the switch it depends on survives**");

        AssertEq(500, geladen.TeleportTargets[1][0].MapId,
            "**and where it leads survives**");

        AssertEq(12, geladen.TeleportTargets[1][0].X,
            "**and its column survives**");
    }
}
