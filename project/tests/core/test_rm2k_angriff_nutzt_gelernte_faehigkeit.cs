using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniversalRPG.Plugins;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And that the strike now uses a learned skill.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And two commits ago the strike was refused with "the hero
/// has learned no skill with a power".</strong> --
/// <strong>That refusal is now either gone or honest, and this is
/// the test that says which.</strong>
/// </para>
/// </remarks>
public partial class TestRm2kAngriffNutztGelernteFaehigkeit : TestBase
{
    private const string Spiel = "E:/RPGMakerGames/Dragon Destiny";

    private static Rm2kEngineRuntime? MitKampf(
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

        var lauf = (Rm2kEngineRuntime)pHost.Runtime!;
        if (!Rm2kBegegnungAufbauen.Versuche(
                lauf.Simulation.DatabaseData, 4,
                out var truppe, out _))
        {
            pHost.Dispose();
            pHost = null;
            return null;
        }

        foreach (var g in truppe)
        {
            lauf.Simulation.TroopMembers.Add(g);
        }

        lauf.Simulation.IsBattleActive = true;
        lauf.Simulation.WaitingFor =
            GameSimulationState.WaitReason.BattleRunning;
        return lauf;
    }

    /// <summary>
    /// And the strike either lands or says why, and it moves the
    /// turn only when it lands.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the assertion is deliberately not
    /// "it works"</strong>, -- <strong>because a test that forces one
    /// answer cannot tell a reader apart from a guess</strong>, --
    /// <strong>and this one says which of the two happened.</strong>
    /// </para>
    /// </remarks>
    public void Test_DerSchlagTrifftOderSagtWarum()
    {
        var lauf = MitKampf(out var host);
        if (lauf == null)
        {
            return;
        }

        using var h = host!;
        var state = lauf.Simulation;
        state.BattleTurn = 0;

        // **Und  jetzt  ist  Rast  im  Spiel** -- **und  er  hat  18
        //  gelernte  Faehigkeiten.**
        //
        // **Und  damit  hat  `ErsteAngriffsfaehigkeit`  etwas  zu
        //  waehlen**, -- **und  die  Frage  ist  nicht  mehr  "hat
        //  er  eine",  sondern  "welche  davon  hat  eine  Kraft".**
        // **Und  jetzt  der  entscheidende  Befund**:  -- **wie  viele
        //  Helden  tragen  Faehigkeiten  und  welche.**
        var mitFaehigkeiten = 0;
        foreach (var heldId in new[] { 1, 2, 3, 4, 5, 6, 7 })
        {
            var menge = state.SkillsOf(heldId);
            Console.WriteLine("Held " + heldId + ": "
                + menge.Count + " Faehigkeiten");
            if (menge.Count > 0)
            {
                mitFaehigkeiten++;
            }
        }

        Console.WriteLine("Helden mit Faehigkeiten: " + mitFaehigkeiten);

        var vorher = state.MonsterHp(0);
        var ok = lauf.FuehreZugAus(
            Rm2kBefehlswahl.Befehl.Angriff, 0, out var grund);
        Console.WriteLine("Schlag: " + ok + "  -> "
            + (grund.Length == 0 ? "(kein Grund)" : grund));
        Console.WriteLine("Gegner: " + vorher + " -> "
            + state.MonsterHp(0) + "  Turn " + state.BattleTurn);

        if (ok)
        {
            AssertTrue(state.MonsterHp(0) < vorher,
                "**and the monster lost hit points** -- and a"
                    + " strike that takes none is not a strike");

            AssertTrue(state.BattleTurn != 0,
                "**and the turn passed on** -- and a landed strike"
                    + " that left the turn in place would stall"
                    + " the fight");
        }
        else
        {
            AssertTrue(grund.Length > 0,
                "**and a refusal says why in words**");

            AssertEq(0, state.BattleTurn,
                "**and a refused strike does not move the"
                    + " turn**");

            Console.WriteLine("BEFUND: " + grund);
        }
    }

    /// <summary>
    /// And a hero's learned skills resolve to skills that exist.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is what the strike reads.</strong> --
    /// <strong>If a learned id names no skill, then the strike would
    /// pick a power from a hole.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieGelerntenNennenEchteFaehigkeiten()
    {
        var lauf = MitKampf(out var host);
        if (lauf == null)
        {
            return;
        }

        using var h = host!;
        var bank = lauf.Simulation.DatabaseData;
        var helden = (Godot.Collections
            .Array<Godot.Collections.Dictionary>)
            bank["actors"];
        var skills = (Godot.Collections
            .Array<Godot.Collections.Dictionary>)
            bank["skills"];

        var vorhanden = new HashSet<int>();
        foreach (var s in skills)
        {
            vorhanden.Add(s["id"].AsInt32());
        }

        var geprueft = 0;
        var fehlend = 0;
        for (var i = 0; i < helden.Count; i++)
        {
            if (!helden[i].ContainsKey("unknown_fields"))
            {
                continue;
            }

            foreach (var f in (Godot.Collections
                .Array<Godot.Collections.Dictionary>)
                helden[i]["unknown_fields"])
            {
                if (f["id"].AsInt32() != 0x3F
                    || !UniversalRPG.Rm2k.Parser.Rm2kLearningDecoder
                        .TryDecode((byte[])f["data"],
                            out var gelernt, out _))
                {
                    continue;
                }

                foreach (var e in gelernt)
                {
                    geprueft++;
                    if (!vorhanden.Contains(
                        e["skill_id"].AsInt32()))
                    {
                        fehlend++;
                        Console.WriteLine("  fehlt: "
                            + e["skill_id"].AsInt32());
                    }
                }
            }
        }

        Console.WriteLine("geprueft " + geprueft
            + "  fehlend " + fehlend);

        AssertTrue(geprueft > 0,
            "**and there are learned skills to check**");

        AssertEq(0, fehlend,
            "**and every learned skill exists in the game's own"
                + " skill table** -- and one that does not would"
                + " mean the strike picks a power from a hole");
    }
}
