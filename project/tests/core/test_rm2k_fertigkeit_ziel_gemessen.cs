using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And that a skill's scope decides who it may hit.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And <c>Rm2kZielwahl</c> knows monsters only</strong>, --
/// <strong>and a cure pointed at an enemy is the opposite of what the
/// game wrote</strong>.
/// </para>
/// <para>
/// <strong>And this uses the bank's own skills</strong>, -- <strong>and
/// not one this test invented</strong>.
/// </para>
/// </remarks>
public partial class TestRm2kFertigkeitZielGemessen : TestBase
{
    private const string Ldb =
        "res://tests/fixtures/rm2k-dragon-destiny/RPG_RT.ldb";

    /// <summary>
    /// And the five scopes route to the right side.
    /// </summary>
    public void Test_DieReichweitenWaehlenDieRichtigeSeite()
    {
        var bank = new UniversalRPG.Rm2k.Parser.Rm2kParser()
            .ParseDatabase(Ldb);
        if (!bank.IsSuccess() || !bank.GetData().ContainsKey("skills"))
        {
            AssertTrue(false, "**and the bank's skills are read**");
            return;
        }

        var zustand = new GameSimulationState
        {
            PartyMemberIds = new Godot.Collections.Array<int> { 1, 2 }
        };
        zustand.CurrentHp[1] = 30;
        zustand.CurrentHp[2] = 0;

        // **Und  Held  zwei  ist  bewusst  am  Boden** -- **und  darum
        //  muss  er  kein  Ziel  sein.**
        Console.WriteLine("Held 2 bei " + zustand.GetActorCurrentHp(2)
            + " HP");

        foreach (var (scope, name) in new[] {
            (Rm2kFertigkeitZiel.ScopeEnemy, "enemy"),
            (Rm2kFertigkeitZiel.ScopeEnemies, "enemies"),
            (Rm2kFertigkeitZiel.ScopeSelf, "self"),
            (Rm2kFertigkeitZiel.ScopeAlly, "ally"),
            (Rm2kFertigkeitZiel.ScopeParty, "party"),
        })
        {
            var helden = Rm2kFertigkeitZiel.MoeglicheHeldenziele(
                zustand, scope);
            Console.WriteLine(name + " -> [" + string.Join(",",
                helden) + "]  braucht Ziel "
                + Rm2kFertigkeitZiel.BrauchtZiel(scope)
                + "  Gegner " + Rm2kFertigkeitZiel.ZieltAufGegner(scope));
            AssertEq(name, Rm2kFertigkeitZiel.Name(scope),
                "**and scope " + scope + " is named '" + name
                    + "' by liblcf**");
        }

        var heilZiele = Rm2kFertigkeitZiel.MoeglicheHeldenziele(
            zustand, Rm2kFertigkeitZiel.ScopeAlly);
        AssertEq(1, heilZiele.Count,
            "**and a cure aims at the one standing hero** -- and"
                + " hero two is at zero hit points");

        AssertEq(1, heilZiele[0],
            "**and that hero is the first one alive**");

        AssertEq(0, Rm2kFertigkeitZiel.MoeglicheHeldenziele(
                zustand, Rm2kFertigkeitZiel.ScopeEnemy).Count,
            "**and an attack aims at no hero at all** -- and a"
                + " reader that gave it one would let a strike"
                + " land on the player's own side");

        AssertTrue(Rm2kFertigkeitZiel.BrauchtZiel(
                Rm2kFertigkeitZiel.ScopeEnemy),
            "**and an attack asks who to hit**");

        AssertTrue(!Rm2kFertigkeitZiel.BrauchtZiel(
                Rm2kFertigkeitZiel.ScopeEnemies),
            "**and an all enemy attack does not** -- and that is"
                + " what makes the branch's flag a fact");

        // **Und  jetzt  eine  echte  Faehigkeit  aus  der  Bank.**
        var geheilt = bank.GetData()["skills"].AsGodotArray()
            .Select(x => x.AsGodotDictionary())
            .FirstOrDefault(x => x.ContainsKey("scope")
                && x["scope"].AsInt32() == Rm2kFertigkeitZiel.ScopeAlly
                && x.ContainsKey("name"));
        if (geheilt == null)
        {
            AssertTrue(false, "**and this game has a single ally"
                + " skill**");
            return;
        }

        var skillName = geheilt["name"].AsString();
        Console.WriteLine("Heilfaehigkeit: '" + skillName
            + "'  scope "
            + geheilt["scope"].AsInt32());
        AssertTrue(Rm2kFertigkeitZiel.MoeglicheHeldenziele(zustand,
                geheilt["scope"].AsInt32()).Count > 0,
            "**and the bank's own ally skill reaches a hero** -- '"
                + skillName + "'");
    }
}
