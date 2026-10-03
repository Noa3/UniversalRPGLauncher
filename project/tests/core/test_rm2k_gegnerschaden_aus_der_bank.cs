using System;
using System.Collections.Generic;
using System.IO;
using UniversalRPG.Plugins;
using UniversalRPG.Rm2k;
using UniversalRPG.Rm2k.Interpreter;
using UniversalRPG.Rm2k.Presentation;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And the damage a monster takes, and that the formula is the
/// reference's.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this closes the last measured gap in criterion 1.</strong>
/// --
/// <strong><c>Rm2kSimulatedAttack.Compute</c> existed and one command
/// reached it</strong>, -- <strong>and that command damages heroes
/// outside a battle.</strong> -- <strong>No battle command reached
/// it.</strong>
/// </para>
/// </remarks>
public partial class TestRm2kGegnerschadenAusDerBank : TestBase
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

    private static Rm2kMap.EventCommand Begegnung(int pTrupe) =>
        new()
        {
            Code = EventInterpreter.EnemyEncounter,
            Text = "",
            Parameters = new List<int> { 0, pTrupe, 0, 1, 0, 0 },
        };

    /// <summary>
    /// And a monster from the game's own file takes a calculated hit.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the number is worked out from the bank's own
    /// defence</strong>, -- <strong>and the assertion repeats the
    /// formula by hand</strong>, -- <strong>because a test that
    /// asserted the method's own output would prove
    /// nothing.</strong>
    /// </para>
    /// </remarks>
    public void Test_DerGegnerVerliertDieBerechneteZahl()
    {
        var lauf = Starte(out var host);
        if (lauf == null)
        {
            return;
        }

        using var h = host!;
        var state = lauf.Simulation;
        var zufall = new Rm2kDamageRandom();
        zufall.Seed(12345);

        AssertTrue(Rm2kBegegnungAufbauen.Versuche(
                state.DatabaseData, 2, out var m, out var warum),
            "**and troop two builds** -- and: " + warum);

        foreach (var g in m)
        {
            state.TroopMembers.Add(g);
        }

        var start = state.MonsterHp(0);
        var verteidigung = m[0]["defense"].AsInt32();
        var geist = m[0]["spirit"].AsInt32();
        Console.WriteLine("Scorpion: hp=" + start
            + " verteidigung=" + verteidigung
            + " geist=" + geist + " atk="
            + m[0]["attack"].AsInt32());

        // **Und  100  Angriff  gegen  0  Raten** -- **das  ist  der
        //  Fall,  der  nichts  von  den  Teilern  abhaengig  macht**
        // **und  deshalb  der  ehrlichste  erste  Test.**
        var schaden = Rm2kGegnerSchaden.Berechne(
            state, 0, 100, 0, 0, 0, zufall);

        Console.WriteLine("Schaden: " + schaden
            + "  hp jetzt " + state.MonsterHp(0));
        AssertEq(100, schaden,
            "**and an attack of a hundred against no rates is a"
                + " hundred damage**");

        // **Und  hier  ist  meine  Behauptung  falsch  gewesen  und
        //  der  Zustand  hat  recht.**
        //
        // **Und  `Scorpion`  hat  25  Trefferpunkte  und  ich  habe
        //  100  Schaden  gegeben**, -- **und  der  Zustand  klemmt
        //  bei  null**, -- **denn  `SetMonsterHp`  ist  die  Klammer
        //  der  Referenz  und  ein  Monster  mit  negativen
        //  Trefferpunkten  ist  ein  Zustand,  den  kein  Spiel
        //  hat.**
        //
        // **Und  ich  habe  25  abgezogen  und  mich  gewundert,
        //  dass  0  herauskam** -- **und  das  war  ein  Test,  der
        //  eine  Zahl  behauptet  hat,  die  die  Formel  nicht
        //  liefern  kann.**
        AssertEq(0, state.MonsterHp(0),
            "**and the hit points stopped at zero** -- and a"
                + " monster at minus seventy five is a state no"
                + " game produced, and the clamp is the"
                + " reference's");

        AssertTrue(state.IsMonsterDead(0),
            "**and it is dead**");
    }

    /// <summary>
    /// And damage that does not exceed the monster's health.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is where the formula is checked without the
    /// clamp.</strong>
    /// </para>
    /// </remarks>
    public void Test_EinTrefferDerNichtZuGrossIst()
    {
        var lauf = Starte(out var host);
        if (lauf == null)
        {
            return;
        }

        using var h = host!;
        var state = lauf.Simulation;
        var zufall = new Rm2kDamageRandom();
        zufall.Seed(99);

        AssertTrue(Rm2kBegegnungAufbauen.Versuche(
                state.DatabaseData, 2, out var m, out _),
            "**and the troop builds**");
        foreach (var g in m)
        {
            state.TroopMembers.Add(g);
        }

        var schaden = Rm2kGegnerSchaden.Berechne(
            state, 0, 10, 0, 0, 0, zufall);
        Console.WriteLine("10 Schaden, hp jetzt "
            + state.MonsterHp(0));

        AssertEq(10, schaden,
            "**and ten is ten**");

        AssertEq(15, state.MonsterHp(0),
            "**and twenty five minus ten is fifteen** -- and this"
                + " is the assertion that would have caught my"
                + " own wrong number of twenty five minus a hundred");
    }

    /// <summary>
    /// And the two divisors, which are not the same.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the assertion that catches a reader who
    /// shared one divisor.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieZweiTeilerSindVerschieden()
    {
        var lauf = Starte(out var host);
        if (lauf == null)
        {
            return;
        }

        using var h = host!;
        var state = lauf.Simulation;
        var zufall = new Rm2kDamageRandom();
        zufall.Seed(1);

        AssertTrue(Rm2kBegegnungAufbauen.Versuche(
                state.DatabaseData, 2, out var m, out _),
            "**and the troop builds**");
        foreach (var g in m)
        {
            state.TroopMembers.Add(g);
        }

        var v = m[0]["defense"].AsInt32();
        var g2 = m[0]["spirit"].AsInt32();

        // **Und  ich  messe  die  Teiler  an  zwei  Werten  und
        //  vergleiche  sie  mit  der  Formel.**
        //
        // 400 * rate / 400  =  rate
        // 800 * rate / 800  =  rate
        Console.WriteLine($"verteidigung {v}, geist {g2}");

        var a = Rm2kGegnerSchaden.Berechne(state, 0, 1000, 1, 1, 0, zufall);
        Console.WriteLine("beide Raten 1: " + a);

        var erwartet = Math.Max(0, 1000 - v * 1 / 400 - g2 * 1 / 800);
        AssertEq(erwartet, a,
            "**and the damage is attack minus defence over four"
                + " hundred minus spirit over eight hundred** -- and"
                + " a reader with one divisor for both would have"
                + " made spirit half as strong as the game meant");

        AssertEq(1000 - v - g2, a,
            "**and with a rate of one the two divisors cancel and"
                + " the whole value counts**");
    }

    /// <summary>
    /// And a dead monster is not hit again.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the guard that keeps the hit points from
    /// running away</strong>, -- <strong>and a game that showed a
    /// number below zero would have shown one the game never
    /// produced.</strong>
    /// </para>
    /// </remarks>
    public void Test_EinGefallenerGegnerWirdNichtGetroffen()
    {
        var lauf = Starte(out var host);
        if (lauf == null)
        {
            return;
        }

        using var h = host!;
        var state = lauf.Simulation;
        var zufall = new Rm2kDamageRandom();
        zufall.Seed(7);

        AssertTrue(Rm2kBegegnungAufbauen.Versuche(
                state.DatabaseData, 2, out var m, out _),
            "**and the troop builds**");
        foreach (var g in m)
        {
            state.TroopMembers.Add(g);
        }

        state.SetMonsterHp(0, 0);
        Console.WriteLine("hp auf 0, tot="
            + state.IsMonsterDead(0));

        var schaden = Rm2kGegnerSchaden.Berechne(
            state, 0, 999, 0, 0, 0, zufall);
        Console.WriteLine("Schaden auf einen toten: " + schaden);

        AssertEq(-1, schaden,
            "**and a dead monster takes no further damage**");

        AssertEq(0, state.MonsterHp(0),
            "**and his hit points stay at zero** -- and a monster"
                + " at minus nine hundred and ninety nine is a"
                + " number no game produced");
    }
}
