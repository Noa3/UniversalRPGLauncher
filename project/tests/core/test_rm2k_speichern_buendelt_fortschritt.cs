using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniversalRPG.Plugins;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And that the save carries the story and not only the numbers.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the codec was measured at one hundred and nineteen
/// unsaved fields</strong>, -- <strong>sixty-six of which carry
/// progress.</strong>
/// </para>
/// <para>
/// <strong>And this step takes the ones a save without them would
/// quietly destroy: a hero's conditions and the four access
/// rights.</strong>
/// </para>
/// </remarks>
public partial class TestRm2kSpeichernBuendeltFortschritt : TestBase
{
    private const string Spiel = "E:/RPGMakerGames/Dragon Destiny";

    /// <summary>
    /// And a real state, and the host stays alive with it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And a first draft disposed the host before the state
    /// was saved</strong>, -- <strong>and the host's disposal clears
    /// the map metadata</strong>, -- <strong>and the codec then
    /// refused with "Save map metadata is outside bounds"</strong>, --
    /// <strong>and that refusal was correct.</strong>
    /// </para>
    /// <para>
    /// <strong>And the host has to travel out of the method, because a
    /// state that outlives its host is a state without a
    /// map.</strong>
    /// </para>
    /// </remarks>
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
    /// And a poisoned, sleeping hero comes back poisoned and asleep.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And condition three is the sleep, in every 2K
    /// database</strong>, -- <strong>and the state's own constant says
    /// so</strong>, -- <strong>and a save that dropped them reloads a
    /// hero who is perfectly healthy.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieBedingungenReisen()
    {
        var state = EchterZustand(out var host);
        if (state == null)
        {
            return;
        }

        using var h = host!;

        // **Und  jetzt  eine  echte  Krankheit** -- **die  aus  der
        //  Bank  des  Spiels  und  nicht  aus  dem  Kopf.**
        state.ConditionsOf(1).Add(2);
        state.ConditionsOf(1).Add(
            GameSimulationState.SleepConditionId);
        state.ConditionsOf(4).Add(5);
        Console.WriteLine("Held 1: "
            + string.Join(",", state.ConditionsOf(1)));
        Console.WriteLine("Held 4: "
            + string.Join(",", state.ConditionsOf(4)));

        var json = Rm2kSimulationSaveCodec.Serialize(state);
        var geladen = new GameSimulationState();
        AssertTrue(Rm2kSimulationSaveCodec.TryRestore(
            json, geladen, out var fehler),
            "**and the save is read back** -- and: " + fehler);

        Console.WriteLine("zurueck Held 1: "
            + string.Join(",", geladen.ConditionsOf(1)));

        AssertTrue(geladen.ConditionsOf(1).Contains(2),
            "**and the poison survives** -- and a hero who was"
                + " poisoned and reloads healthy is a save that"
                + " quietly undid the game's own work");

        AssertTrue(geladen.ConditionsOf(1).Contains(
                GameSimulationState.SleepConditionId),
            "**and the sleep survives** -- and condition three is"
                + " the sleep in every 2K database");

        AssertTrue(geladen.ConditionsOf(4).Contains(5),
            "**and a second hero's condition survives too**");
    }

    /// <summary>
    /// And a locked game comes back locked.
    /// </summary>
    public void Test_DieZugangsrechteReisen()
    {
        var state = EchterZustand(out var host);
        if (state == null)
        {
            return;
        }

        using var h = host!;

        // **Und  das  ist  der  Weg,  den  der  Interpreter  geht**,
        // -- **und  nicht  ein  oeffentlicher  Setter**, -- **denn  die
        //  vier  Rechte  bewegen  sich  zusammen.**
        state.SetAccess(pEscape: true, pSave: false, pMenu: false,
            pTeleport: false);
        Console.WriteLine("Flucht " + state.AllowEscape
            + "  Speichern " + state.AllowSave + "  Menue "
            + state.AllowMenu + "  Teleport " + state.AllowTeleport);

        var json = Rm2kSimulationSaveCodec.Serialize(state);
        var geladen = new GameSimulationState();
        AssertTrue(Rm2kSimulationSaveCodec.TryRestore(
            json, geladen, out var fehler),
            "**and the save is read back** -- and: " + fehler);

        Console.WriteLine("zurueck Speichern "
            + geladen.AllowSave + "  Menue "
            + geladen.AllowMenu);

        AssertTrue(!geladen.AllowSave,
            "**and a game that forbade saving comes back"
                + " forbidden** -- and a save that dropped the"
                + " rights would let the player save in a game"
                + " whose author took it away from them");

        AssertTrue(!geladen.AllowMenu,
            "**and a game that forbade the menu comes back"
                + " forbidden**");

        AssertTrue(!geladen.AllowTeleport,
            "**and a game that forbade teleporting comes back"
                + " forbidden**");

        AssertTrue(geladen.AllowEscape,
            "**and the one right that stayed open stays open**");
    }

    /// <summary>
    /// And the parallel events and their counter travel.
    /// </summary>
    public void Test_DieGemeinsamenEreignisseReisen()
    {
        var state = EchterZustand(out var host);
        if (state == null)
        {
            return;
        }

        using var h = host!;

        state.CommonEventIds.Add(3);
        state.CommonEventIds.Add(9);
        state.CommonEventCounter = 12;

        var json = Rm2kSimulationSaveCodec.Serialize(state);
        var geladen = new GameSimulationState();
        AssertTrue(Rm2kSimulationSaveCodec.TryRestore(
            json, geladen, out var fehler),
            "**and the save is read back** -- and: " + fehler);

        Console.WriteLine("Ereignisse "
            + string.Join(",", geladen.CommonEventIds)
            + "  Zaehler " + geladen.CommonEventCounter);

        AssertEq(2, geladen.CommonEventIds.Count,
            "**and the running common events survive**");

        AssertEq(3, geladen.CommonEventIds[0],
            "**and in order** -- and two parallel copies of the"
                + " same event sharing a name is a save that"
                + " changes what the game does");

        AssertEq(12, geladen.CommonEventCounter,
            "**and the counter survives** -- and a reload that"
                + " restarted it would let the next event reuse"
                + " a name that is still running");
    }
}
