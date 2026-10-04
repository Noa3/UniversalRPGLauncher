using System;
using System.IO;
using System.Linq;
using Godot;
using UniversalRPG.Plugins;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And that a skill is a turn, and not a refusal.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And <c>10220</c>, Skill, stands on eighty two of the
/// hundred and five troop pages</strong>, -- <strong>and the host
/// answered it with "this runtime does not ask for that
/// yet"</strong>.
/// </para>
/// <para>
/// <strong>And the scope decides the target</strong>, -- <strong>and
/// the bank is the source of the scope</strong>.
/// </para>
/// </remarks>
public partial class TestRm2kFaehigkeitIstEinZug : TestBase
{
    private const string Spiel = "E:/RPGMakerGames/Dragon Destiny";

    /// <summary>
    /// And a bank skill becomes a turn.
    /// </summary>
    public void Test_EineBankfaehigkeitIstEinZug()
    {
        if (!File.Exists(Spiel + "/RPG_RT.ldb"))
        {
            return;
        }

        using var host = new EnginePluginHost(
            BuiltInEnginePluginCatalog.CreateRuntimeRegistry());
        var start = host.Start(new PluginGameInfo
        {
            GameDirectory = Spiel,
            EngineId = EnginePluginIds.RpgMaker2000,
            Generation = "rm2k",
            DetectorScore = 3,
        });
        if (!start.Success)
        {
            AssertTrue(false, "**and the game starts**");
            return;
        }

        var lauf = (Rm2kEngineRuntime)host.Runtime!;
        var state = lauf.Simulation;
        Console.WriteLine("Party: ["
            + string.Join(",", state.PartyMemberIds) + "]");

        if (!Rm2kBegegnungAufbauen.Versuche(state.DatabaseData, 4,
                out var truppe, out var grund))
        {
            AssertTrue(false, "**and a troop is built** -- " + grund);
            return;
        }

        foreach (var mitglied in truppe)
        {
            state.TroopMembers.Add(mitglied);
        }

        state.IsBattleActive = true;
        state.WaitingFor = GameSimulationState.WaitReason.BattleRunning;

        // **Und  ohne  gewaehlte  Faehigkeit  wird  verweigert.**
        var ohne = lauf.FuehreZugAus(
            Rm2kBefehlswahl.Befehl.Faehigkeit, -1, out var fehler1);
        Console.WriteLine("ohne Faehigkeit: " + ohne + " -> "
            + fehler1);
        AssertTrue(!ohne,
            "**and casting an unchosen skill is refused**");

        // **Und  jetzt  eine  echte  Bankfaehigkeit  mit  Reichweite
        //  "enemy".**
        var skill = lauf.AktiveFaehigkeit();
        var bank = state.DatabaseData;
        var treffer = bank["skills"].AsGodotArray()
            .Select(x => x.AsGodotDictionary())
            .FirstOrDefault(x => x.ContainsKey("scope")
                && x["scope"].AsInt32() == Rm2kFertigkeitZiel.ScopeEnemy);
        if (treffer == null)
        {
            AssertTrue(false, "**and the bank has an enemy skill**");
            return;
        }

        var skillId = treffer["id"].AsInt32();
        Console.WriteLine("Faehigkeit " + skillId + " '"
            + treffer["name"].AsString() + "' scope "
            + treffer["scope"].AsInt32());

        // **Und  die  Zielwahl  fehlt  noch.**
        lauf.GewaehlteFaehigkeit = skillId;
        lauf.AktuellesZiel.WaehleGegner(state, 0, out var zielFehler);
        var keinZiel = lauf.AktuellesZiel.WaehleGegner(state, 0,
            out _);
        Console.WriteLine("Ziel: " + keinZiel + " -> " + zielFehler);

        var turnVorher = state.BattleTurn;
        var ok = lauf.FuehreZugAus(
            Rm2kBefehlswahl.Befehl.Faehigkeit, 0, out var zugGrund);
        Console.WriteLine("Zug: " + ok + " -> " + zugGrund
            + "  Turn " + turnVorher + " -> " + state.BattleTurn);

        AssertTrue(ok,
            "**and casting it is a turn** -- and 10220 stands on"
                + " eighty two of the hundred and five troop pages");

        AssertTrue(state.BattleTurn != turnVorher,
            "**and the turn passes on** -- and a command that did"
                + " nothing would leave the turn where it was");

        AssertEq("RM2K the hero casts '" + treffer["name"].AsString()
                + "' with the scope enemy",
            SimulationLetzteMeldung(state),
            "**and the diagnostic names the skill and its"
                + " scope**");
    }

    private static string SimulationLetzteMeldung(GameSimulationState pState)
    {
        foreach (var zeile in pState.Diagnostics)
        {
            if (zeile.StartsWith("RM2K the hero casts '",
                StringComparison.Ordinal))
            {
                return zeile;
            }
        }

        return "";
    }
}
