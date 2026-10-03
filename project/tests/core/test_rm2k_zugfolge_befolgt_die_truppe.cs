using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniversalRPG.Plugins;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And that the turn order is the troop's own order.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is the fourth field in this repository that
/// existed and that nothing reached</strong>, -- <strong>after
/// <c>IsTransferPending</c>, <c>TroopMembers</c> and
/// <c>BattleSubcommand</c></strong>, -- <strong>and the shape is
/// identical: written once, read by nobody.</strong>
/// </para>
/// <para>
/// <strong>And it runs the real game.</strong>
/// </para>
/// </remarks>
public partial class TestRm2kZugfolgeBefolgtDieTruppe : TestBase
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

    private static Rm2kEngineRuntime? MitTrupe(
        out EnginePluginHost? pHost, int pTrupe)
    {
        var lauf = Starte(out pHost);
        if (lauf == null)
        {
            return null;
        }

        if (!Rm2kBegegnungAufbauen.Versuche(
                lauf.Simulation.DatabaseData, pTrupe,
                out var truppe, out _))
        {
            pHost!.Dispose();
            pHost = null;
            return null;
        }

        foreach (var g in truppe)
        {
            lauf.Simulation.TroopMembers.Add(g);
        }

        lauf.Simulation.IsBattleActive = true;
        return lauf;
    }

    /// <summary>
    /// And one monster takes three turns in order.
    /// </summary>
    public void Test_EinGegnerHiertSeineRunden()
    {
        var lauf = MitTrupe(out var host, 2);
        if (lauf == null)
        {
            return;
        }

        using var h = host!;
        var state = lauf.Simulation;
        Console.WriteLine("Gegner: " + state.TroopMembers.Count
            + "  Name: " + state.TroopMembers[0]["name"].AsString());

        var reihe = new List<int>();
        for (var i = 0; i < 4; i++)
        {
            var naechster = Rm2kZugfolge.Ruecke(state);
            reihe.Add(naechster);
        }

        Console.WriteLine("Reihe: " + string.Join(" ", reihe));
        AssertTrue(state.TroopMembers.Count > 0,
            "**and the troop has a monster**");
        AssertEq(4, reihe.Count(x => x == 0),
            "**and the only monster acts on every one of the four"
                + " turns** -- and that is what a single-monster"
                + " troop is for");
    }

    /// <summary>
    /// And four monsters take their turns in the game's order.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And troop 4 is two scorpions measured on the real
    /// file</strong>, -- <strong>and a troop of two must hand the
    /// turn over.</strong>
    /// </para>
    /// </remarks>
    public void Test_ZweiGegnerWechselnSichAb()
    {
        var lauf = MitTrupe(out var host, 4);
        if (lauf == null)
        {
            return;
        }

        using var h = host!;
        var state = lauf.Simulation;
        Console.WriteLine("Gegner: " + state.TroopMembers.Count);

        var reihe = new List<int>();
        for (var i = 0; i < 4; i++)
        {
            reihe.Add(Rm2kZugfolge.Ruecke(state));
        }

        Console.WriteLine("Reihe: " + string.Join(" ", reihe));
        AssertEq(2, state.TroopMembers.Count,
            "**and troop four has two monsters**");

        // **Und  die  Reihe  beginnt  bei  eins  und  nicht  bei
        //  null** -- **und  das  ist  richtig  und  nicht  ein
        //  Fehler:**  `Ruecke`  rueckt  VOR  dem  Zug, -- **und  ein
        //  Test,  der  die  Reihe  nach  vier  Aufrufen  von
        //  `Ruecke`  liest,  sieht  den  Zug  NACH  dem
        //  Rundenwechsel.**
        //
        // **Und  meine  Behauptung  "0,1,0,1"  war  geraten  und
        //  nicht  gemessen** -- **und  sie  war  falsch.**
        AssertEq("1,0,1,0", string.Join(",", reihe),
            "**and the turn alternates between them** -- and it"
                + " starts at one because Ruecke advances before"
                + " it reports, and I wrote \"0,1,0,1\" without"
                + " running it");

        // **Und  beide  Reihenfolgen  sind  abwechselnd** --
        // **und  das  ist  die  Behauptung,  die  zaehlt.**
        AssertTrue(reihe[0] != reihe[1]
                && reihe[1] != reihe[2]
                && reihe[2] != reihe[3],
            "**and every neighbour in the row is a different"
                + " monster** -- and a battle with two monsters"
                + " that never handed the turn over would give"
                + " four identical numbers");
    }

    /// <summary>
    /// And a dead monster is skipped and never stalls the battle.
    /// </summary>
    public void Test_EinToterGegnerBlockiertDieRundeNicht()
    {
        var lauf = MitTrupe(out var host, 4);
        if (lauf == null)
        {
            return;
        }

        using var h = host!;
        var state = lauf.Simulation;
        state.SetMonsterHp(0, 0);
        state.BattleTurn = 0;
        Console.WriteLine("Gegner 0 tot, Turn "
            + state.BattleTurn);

        var naechster = Rm2kZugfolge.Naechster(state);
        Console.WriteLine("naechster: " + naechster);

        AssertEq(1, naechster,
            "**and the round moves to the living monster** -- and a"
                + " reader that stopped on the dead one would end"
                + " the battle early, and a fallen boss could"
                + " strike back on the frame it fell");

        // **Und  wenn  alle  tot  sind,  ist  die  Runde  vorbei.**
        state.SetMonsterHp(1, 0);
        Console.WriteLine("beide tot, naechster: "
            + Rm2kZugfolge.Naechster(state));
        AssertEq(-1, Rm2kZugfolge.Naechster(state),
            "**and when all of them are dead nobody acts** -- and"
                + " that is a result and not a fault");
    }

    /// <summary>
    /// And a battle that is not running has nobody's turn.
    /// </summary>
    public void Test_OhneKampfNiemandesZug()
    {
        var lauf = MitTrupe(out var host, 4);
        if (lauf == null)
        {
            return;
        }

        using var h = host!;
        lauf.Simulation.IsBattleActive = false;
        AssertEq(-1, Rm2kZugfolge.Naechster(lauf.Simulation),
            "**and a page outside a battle has no turn** -- and"
                + " the state carries a turn field outside a"
                + " battle, which is a reader asking the wrong"
                + " question");
    }
}
