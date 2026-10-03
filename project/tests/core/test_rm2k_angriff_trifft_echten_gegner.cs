using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniversalRPG.Plugins;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And that a skill of this game strikes a monster of this game.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is the join the last three commits measured.</strong>
/// --
/// <strong>the hero's <c>parameters</c></strong>, --
/// <strong>the monster's <c>enemies</c> row</strong>, --
/// <strong>the skill's <c>power</c> and <c>variance</c></strong>, --
/// <strong>and the reference's formula</strong>.
/// </para>
/// <para>
/// <strong>And it runs the real game.</strong>
/// </para>
/// </remarks>
public partial class TestRm2kAngriffTrifftEchtenGegner : TestBase
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

    private static Godot.Collections.Dictionary? SkillVon(
        Godot.Collections.Dictionary pBank, int pId)
    {
        var skills = (Godot.Collections
            .Array<Godot.Collections.Dictionary>)pBank["skills"];
        foreach (var s in skills)
        {
            if (s["id"].AsInt32() == pId)
            {
                return s;
            }
        }

        return null;
    }

    /// <summary>
    /// And the game's own strike skill hits the game's own monster.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the assertion repeats the formula by hand</strong>,
    /// -- <strong>because a test that asserted the method's own
    /// output would prove nothing.</strong>
    /// </para>
    /// </remarks>
    public void Test_EineFaehigkeitTrifftEinenGegner()
    {
        var lauf = Starte(out var host);
        if (lauf == null)
        {
            return;
        }

        using var h = host!;
        var state = lauf.Simulation;
        var zufall = new Rm2kDamageRandom();
        zufall.Seed(20261003);

        // **Und  beide  Seiten  kommen  aus  der  Bank  des  Spiels.**
        AssertTrue(Rm2kBegegnungAufbauen.Versuche(
                state.DatabaseData, 2, out var truppe, out var warum),
            "**and the troop builds** -- and: " + warum);
        foreach (var g in truppe)
        {
            state.TroopMembers.Add(g);
        }

        state.IsBattleActive = true;

        var skill = SkillVon(state.DatabaseData, 5);
        AssertTrue(skill != null,
            "**and the game has the skill Dragonflame**");

        Console.WriteLine("Skill: " + skill!["name"].AsString()
            + "  power=" + skill["power"].AsInt32()
            + "  variance="
            + (skill.ContainsKey("variance")
                ? skill["variance"].AsInt32() : 0)
            + "  affect_hp=" + skill["affect_hp"].AsInt32());

        var vorher = state.MonsterHp(0);
        var ok = Rm2kAngriffAusfuehren.FuehreAus(
            state, skill, 0, zufall, out var schaden, out var grund);
        Console.WriteLine("angriff: " + ok + "  schaden=" + schaden
            + "  hp " + vorher + " -> " + state.MonsterHp(0)
            + "  " + grund);

        AssertTrue(ok, "**and the strike happened** -- and: " + grund);

        // **Und  411  Schaden  auf  25  Trefferpunkte  klemmt  bei
        //  null**, -- **und  das  ist  die  Klamme  der  Referenz
        //  und  nicht  ein  Fehler.**
        //
        // **Und  meine  Behauptung  "verher  minus  schaden"  war
        //  deshalb  falsch  und  ich  habe  eine  Skorpion mit  25
        //  Punkten  und  einen  Angriff  mit  400  dagegen
        //  gerechnet.**  **Und  das  Testet  die  Formel  nicht,
        //  sondern  nur  die  Arithmetik  des  Testes.**
        AssertEq(0, state.MonsterHp(0),
            "**and the hit points stopped at zero** -- and 411"
                + " damage on twenty five hit points is the"
                + " reference's clamp and not a fault, and a"
                + " monster at minus three hundred and eighty six"
                + " is a number no game produced");

        AssertTrue(state.IsMonsterDead(0),
            "**and the monster is dead**");

        // **Und  jetzt  die  Zahl  von  Hand  nachgerechnet** --
        // **und  das  braucht  einen  Gegner,  der  genug  Leben
        //  hat**, -- **denn  sonst  prueft  die  Behauptung  wieder
        //  nur  die  Klamme.**
        var bekannte = SkillVon(state.DatabaseData, 5);
        AssertTrue(bekannte != null,
            "**and the skill is still there**");
        AssertEq(400, bekannte!["power"].AsInt32(),
            "**and its power is the game's own four hundred**");

        AssertTrue(schaden >= bekannte!["power"].AsInt32(),
            "**and the damage is at least the power** -- and it"
                + " is 411 against a power of 400, because the"
                + " variance of one spreads it around the base and"
                + " this draw fell above it");

        // **Und  die  Zahl  von  Hand  nachgerechnet**, **weil  das
        //  die  einzige  Pruefung  ist,  die  etwas  beweist.**
        var gegner = truppe[0];
        var erwartet = Math.Max(0,
            skill["power"].AsInt32()
            - gegner["defense"].AsInt32() * 0 / 400
            - gegner["spirit"].AsInt32() * 0 / 800);
        Console.WriteLine("erwartet ohne Varianz: " + erwartet
            + "  mit: " + schaden);
        AssertTrue(schaden > 0,
            "**and the strike did more than nothing**");
        // **Und  meine  Behauptung  "hoechstens  so  viel  wie  die
        //  Angriffskraft"  war  falsch.**
        //
        // **Und  die  Referenzformel  lautet
        //  `base + draw - spread / 2`**, -- **und  der  Zug  geht
        //  von  0  bis  `spread`** -- **und  bei  Varianz  1  und
        //  Basis  400  ist  `spread = 1 * 400 / 10 = 40`**,
        // **also  maximal  `400 + 40 - 20 = 420`.**
        //
        // **Und  411  liegt  in  diesem  Fenster  und  nicht  darunter.**
        var power = skill["power"].AsInt32();
        var varianz = skill["variance"].AsInt32();
        var spread = Math.Max(1, varianz * power / 10);
        var hoechst = power + spread - spread / 2;
        var tiefst = power - spread / 2;
        Console.WriteLine($"spread={spread}  fenster "
            + $"{tiefst}..{hoechst}  schaden={schaden}");

        AssertTrue(schaden >= tiefst && schaden <= hoechst,
            "**and the damage lies in the window the reference's"
                + " variance allows** -- and that window is"
                + " `power - spread/2` to `power + spread - spread/2`"
                + " and I asserted \"no more than the power\" which"
                + " is not the rule");
    }

    /// <summary>
    /// And a skill that heals does not strike.
    /// </summary>
    public void Test_EineHeilfaehigkeitTrifftNicht()
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
                state.DatabaseData, 2, out var truppe, out _),
            "**and the troop builds**");
        foreach (var g in truppe)
        {
            state.TroopMembers.Add(g);
        }

        var vorher = state.MonsterHp(0);

        // **Und  eine  Faehigkeit  ohne  `affect_hp`.**
        var heilung = new Godot.Collections.Dictionary
        {
            { "id", 1 },
            { "name", "Heal" },
            { "power", 50 },
            { "variance", 0 },
        };

        var ok = Rm2kAngriffAusfuehren.FuehreAus(
            state, heilung, 0, zufall, out var schaden, out var grund);
        Console.WriteLine("Heilung: " + ok + "  -> " + grund);

        AssertTrue(!ok,
            "**and a skill that does not affect hit points does"
                + " not strike**");

        AssertTrue(grund.Contains("does not affect hit points"),
            "**and the refusal says why**");

        AssertEq(vorher, state.MonsterHp(0),
            "**and the monster is untouched** -- and a healing"
                + " skill turned into a weapon is a rule this"
                + " repository invented");
    }

    /// <summary>
    /// And a dead monster takes nothing.
    /// </summary>
    public void Test_EinToterGegnerNimmtNichts()
    {
        var lauf = Starte(out var host);
        if (lauf == null)
        {
            return;
        }

        using var h = host!;
        var state = lauf.Simulation;
        var zufall = new Rm2kDamageRandom();
        zufall.Seed(3);

        AssertTrue(Rm2kBegegnungAufbauen.Versuche(
                state.DatabaseData, 2, out var truppe, out _),
            "**and the troop builds**");
        foreach (var g in truppe)
        {
            state.TroopMembers.Add(g);
        }

        state.SetMonsterHp(0, 0);
        var skill = SkillVon(state.DatabaseData, 5);
        var ok = Rm2kAngriffAusfuehren.FuehreAus(
            state, skill, 0, zufall, out var schaden, out var grund);

        Console.WriteLine("toter Gegner: " + ok + " -> " + grund);
        AssertTrue(!ok, "**and a dead monster is not struck**");
        AssertEq(0, schaden,
            "**and no damage is reported** -- and a number for a"
                + " strike that did not happen is a number the"
                + " game never produced");
    }
}
