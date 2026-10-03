using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniversalRPG.Plugins;
using UniversalRPG.Rm2k;
using UniversalRPG.Rm2k.Interpreter;
using UniversalRPG.Rm2k.Presentation;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And that a monster from the game's own file can be hit.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is the last link.</strong> --
/// <strong>10710 brings monsters out of the bank's own bytes</strong>,
/// -- <strong>and 60110 Change Monster HP writes into the member the
/// state holds</strong>, -- <strong>and nothing had ever put the two
/// together.</strong>
/// </para>
/// <para>
/// <strong>And it runs the real game.</strong>
/// </para>
/// </remarks>
public partial class TestRm2kGegnerTreffenEchteBank : TestBase
{
    private const string Spiel = "E:/RPGMakerGames/Dragon Destiny";

    private static Rm2kEngineRuntime? Starte(out EnginePluginHost? pHost)
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

        return (Rm2kEngineRuntime)pHost.Runtime!;
    }

    /// <summary>
    /// And the encounter brings a monster and a command hurts it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the order is the reference's</strong>: encounter
    /// first, then the damage command, because a game's battle
    /// handlers are written after the encounter.</strong>
    /// </para>
    /// </remarks>
    public void Test_DerEchteGegnerLaesstSichTreffen()
    {
        var lauf = Starte(out var host);
        if (lauf == null)
        {
            return;
        }

        using var h = host!;
        var state = lauf.Simulation;

        var befehle = new List<Rm2kMap.EventCommand>
        {
            new()
            {
                Code = EventInterpreter.EnemyEncounter,
                Text = "",
                Parameters = new List<int> { 0, 2, 0, 1, 0, 0 },
            },
            new()
            {
                Code = EventInterpreter.ChangeMonsterHp,
                Text = "",
                // Index 0, lose, mode 0, value 10.
                Parameters = new List<int> { 0, 1, 0, 10, 0 },
            },
        };

        // **Und  der  Test  geht  den  Weg,  den  ein  Spiel  geht**,
        // -- **denn  ein  Test,  der  den  Interpreter  direkt
        //  redet,  redet  an  einem  vorbei,  den  der  Host  nie
        //  sieht.**
        var interpreter = new EventInterpreter(
            state, 1, befehle.ToArray(), new PresentationState());
        lauf.EventScheduler.SeiteStarten(interpreter);
        lauf.EventScheduler.ExecuteFrame();

        // **Und  jetzt  wird  klar,  dass  die  Seite  haelt** --
        // **und  das  ist  richtig:**  die  Begegnung  macht  den
        //  Kampf  zu  einem  asynchronen  Vorgang  und  die  Befehle
        //  danach  laufen,  wenn  er  endet.
        Console.WriteLine("nach der Begegnung: Gegner "
            + state.TroopMembers.Count + "  Kampf "
            + state.IsBattleActive + "  wartet auf "
            + state.WaitingFor + "  HP " + state.MonsterHp(0));

        AssertEq(GameSimulationState.WaitReason.BattleRunning,
            state.WaitingFor,
            "**and the page holds on the battle** -- and this is"
                + " the reference's rule and not a stall");

        // **Und  jetzt  endet  der  Kampf**, -- **und  erst  dann
        //  laeuft  der  Schadensbefehl.**
        // **Und  das  stand  einmal  an  anderer  Stelle  und  war
        //  dort  falsch**, -- **denn  `BeendeKampf`  setzt  die
        //  Wartefrage  sofort  auf  `None`** --
        // **und  eine  Behauptung  ueber  die  Wartephase  muss
        //  VOR  dem  Beenden  stehen  und  nicht  danach.**
        // **Und  die  Ausgangszahl  wird  VOR  dem  Schaden
        //  geprueft**  --  **denn  danach  ist  sie  nicht  mehr
        //  25.**
        AssertEq(25, state.MonsterHp(0),
            "**and the monster has its own twenty five hit points** -- and that is the number the bank gave and not one of ours");

        lauf.BeendeKampf(true);
        lauf.EventScheduler.ExecuteFrame();

        Console.WriteLine("nach dem Kampf: Gegner "
            + state.TroopMembers.Count + "  HP " + state.MonsterHp(0));

        AssertTrue(state.TroopMembers.Count > 0,
            "**and the encounter brought a monster from the game's"
                + " own bytes**");


        AssertEq(15, state.MonsterHp(0),
            "**and the command took ten away** -- and before the"
                + " previous commit the troop list was empty here"
                + " and the command refused with \"invalid enemy"
                + " ID 0\"");

        AssertTrue(!state.IsMonsterDead(0),
            "**and it is still alive**");
    }

    /// <summary>
    /// And more damage than it has health, and the death.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the reference's clamp</strong>, --
    /// <strong>not a value this repository chose.</strong>
    /// </para>
    /// </remarks>
    public void Test_DerGegnerStirbtUndNichtDarueberHinaus()
    {
        var lauf = Starte(out var host);
        if (lauf == null)
        {
            return;
        }

        using var h = host!;
        var state = lauf.Simulation;

        var befehle = new List<Rm2kMap.EventCommand>
        {
            new()
            {
                Code = EventInterpreter.EnemyEncounter,
                Text = "",
                Parameters = new List<int> { 0, 2, 0, 1, 0, 0 },
            },
            new()
            {
                Code = EventInterpreter.ChangeMonsterHp,
                Text = "",
                Parameters = new List<int> { 0, 1, 0, 99, 0 },
            },
        };

        var interpreter = new EventInterpreter(
            state, 1, befehle.ToArray(), new PresentationState());
        lauf.EventScheduler.SeiteStarten(interpreter);
        lauf.EventScheduler.ExecuteFrame();
        lauf.BeendeKampf(true);
        lauf.EventScheduler.ExecuteFrame();

        Console.WriteLine("HP nach 99 Schaden: " + state.MonsterHp(0)
            + "  tot: " + state.IsMonsterDead(0));

        AssertEq(0, state.MonsterHp(0),
            "**and the hit points stop at zero** -- and a monster"
                + " at minus seventy four is a state a battle screen"
                + " cannot draw");
        AssertTrue(state.IsMonsterDead(0),
            "**and it is dead**");
        AssertTrue(!state.CanMonsterAct(0),
            "**and it can no longer strike back** -- and that is"
                + " the whole point of the field `CanMonsterAct`"
                + " reads, which no monster had ever reached");
    }
}
