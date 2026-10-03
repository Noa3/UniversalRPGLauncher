using System;
using System.IO;
using UniversalRPG.Plugins;
using UniversalRPG.App;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And that the launcher shows the battle, and the game's own
/// numbers.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this measures a gap first.</strong> --
/// <strong><c>IsBattleActive</c> appeared in no file under
/// <c>app/ui</c></strong>, -- <strong>and a battle that happens but
/// cannot be seen is indistinguishable from a battle that did not
/// happen.</strong>
/// </para>
/// </remarks>
public partial class TestRm2kKampfansichtZeigtDieBank : TestBase
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
    /// And before a battle, nothing is shown.
    /// </summary>
    public void Test_OhneKampfNichtsAnzeigen()
    {
        var lauf = Starte(out var host);
        if (lauf == null)
        {
            return;
        }

        using var h = host!;
        var ansicht = new Rm2kBattleView();
        ansicht.SetzeZustand(lauf.Simulation);

        Console.WriteLine("Kampf vor dem Befehl: "
            + ansicht.LaeuftEinKampf());
        AssertTrue(!ansicht.LaeuftEinKampf(),
            "**and no battle is shown before the encounter"
                + " command**");
        AssertEq(0, ansicht.LebendeGegner(),
            "**and there are no opponents**");
    }

    /// <summary>
    /// And a battle shows the game's own monsters.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this asserts on the numbers out of
    /// <c>RPG_RT.ldb</c></strong>, -- <strong>because a view that drew
    /// a plausible number would look right and be
    /// wrong.</strong>
    /// </para>
    /// </remarks>
    public void Test_DerKampfZeigtDieEigenenGegner()
    {
        var lauf = Starte(out var host);
        if (lauf == null)
        {
            return;
        }

        using var h = host!;
        var state = lauf.Simulation;
        AssertTrue(Rm2kBegegnungAufbauen.Versuche(
                state.DatabaseData, 4, out var m, out var warum),
            "**and troop four builds** -- and: " + warum);
        foreach (var g in m)
        {
            state.TroopMembers.Add(g);
        }

        state.IsBattleActive = true;
        state.BattleTurn = 2;
        state.ActiveTroopId = 4;

        var ansicht = new Rm2kBattleView();
        ansicht.SetzeZustand(state);
        Console.WriteLine("Kampf: " + ansicht.LaeuftEinKampf()
            + "  lebend: " + ansicht.LebendeGegner());

        AssertTrue(ansicht.LaeuftEinKampf(),
            "**and the launcher knows a battle is running** -- and"
                + " before this no file under app/ui mentioned"
                + " IsBattleActive at all");
        AssertEq(2, ansicht.LebendeGegner(),
            "**and it counts two living opponents** -- and troop"
                + " four is two scorpions, measured");

        AssertEq(25, state.MonsterHp(0),
            "**and their hit points are the game's own**");

        AssertEq(4, state.ActiveTroopId,
            "**and the encounter id is the game's own**");
    }

    /// <summary>
    /// And a dead monster stops counting.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the reason the view asks
    /// <c>CanMonsterAct</c> and does not count members.</strong>
    /// </para>
    /// </remarks>
    public void Test_EinGefallenerGegnerZaehltNicht()
    {
        var lauf = Starte(out var host);
        if (lauf == null)
        {
            return;
        }

        using var h = host!;
        var state = lauf.Simulation;
        AssertTrue(Rm2kBegegnungAufbauen.Versuche(
                state.DatabaseData, 4, out var m, out _),
            "**and the troop builds**");
        foreach (var g in m)
        {
            state.TroopMembers.Add(g);
        }

        state.IsBattleActive = true;
        var ansicht = new Rm2kBattleView();
        ansicht.SetzeZustand(state);
        AssertEq(2, ansicht.LebendeGegner(),
            "**and both are counted**");

        state.SetMonsterHp(0, 0);
        Console.WriteLine("nach dem Tod: "
            + ansicht.LebendeGegner());
        AssertEq(1, ansicht.LebendeGegner(),
            "**and a fallen one stops counting** -- and a view"
                + " that counted members would draw a dead"
                + " scorpion standing there");
    }
}
