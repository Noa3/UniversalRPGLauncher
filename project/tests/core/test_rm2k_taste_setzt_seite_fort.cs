using System;
using System.IO;
using System.Linq;
using UniversalRPG.Plugins;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// A key press, and that a page which was waiting carries on.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this closes the fault that
/// <c>TestRm2kWartezustandImLauf</c> measured.</strong> --
/// <strong>That test proved the wait never ends</strong>, --
/// <strong>and this one proves there is now a way to end it.</strong>
/// </para>
/// <para>
/// <strong>And the battle is the one case that is deliberately
/// left alone</strong>, -- <strong>because ending a battle with a key
/// press would be inventing gameplay.</strong> --
/// <strong>The game decides a battle, and this repository reads the
/// result.</strong>
/// </para>
/// </remarks>
public partial class TestRm2kTasteSetztSeiteFort : TestBase
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
    /// And the wait is set and a key press ends it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this runs the real game</strong>, -- <strong>and
    /// the state is set the way the interpreter sets it</strong>, --
    /// <strong>and the assertion is that the page is free
    /// afterwards.</strong>
    /// </para>
    /// </remarks>
    public void Test_EinTastendruckBeendetDieWarte()
    {
        var lauf = Starte(out var host);
        if (lauf == null)
        {
            return;
        }

        using var _ = host!;
        lauf.Simulation.WaitingFor =
            GameSimulationState.WaitReason.MessageOpen;
        lauf.Presentation.ShowMessage("eine Zeile");

        for (var i = 0; i < 100; i++)
        {
            lauf.Update(1.0 / 60.0);
        }
        AssertEq(GameSimulationState.WaitReason.MessageOpen,
            lauf.Simulation.WaitingFor,
            "**and the wait is still standing after a hundred"
                + " frames**");

        lauf.DrueckeFort();
        Console.WriteLine("nach Tastendruck: "
            + lauf.Simulation.WaitingFor
            + "  Dialog sichtbar " + lauf.Presentation.MessageVisible);

        AssertEq(GameSimulationState.WaitReason.None,
            lauf.Simulation.WaitingFor,
            "**and a key press ends the wait**");
        AssertTrue(!lauf.Presentation.MessageVisible,
            "**and the window is closed**");
    }

    /// <summary>
    /// And both menus end the same way.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And they are two flags and not one</strong>, -- <strong>and
    /// a reader that stored "a menu" in a single field would have the
    /// save command close the main menu's request.</strong>
    /// </para>
    /// </remarks>
    public void Test_BeideMenuesGehenAuf()
    {
        var lauf = Starte(out var host);
        if (lauf == null)
        {
            return;
        }

        using var _ = host!;

        lauf.Simulation.IsSaveMenuActive = true;
        lauf.Simulation.WaitingFor =
            GameSimulationState.WaitReason.SaveMenuOpen;
        lauf.DrueckeFort();
        AssertEq(GameSimulationState.WaitReason.None,
            lauf.Simulation.WaitingFor,
            "**and the save menu closed**");
        AssertTrue(!lauf.Simulation.IsSaveMenuActive,
            "**and its flag is down**");

        lauf.Simulation.IsMainMenuActive = true;
        lauf.Simulation.WaitingFor =
            GameSimulationState.WaitReason.MainMenuOpen;
        lauf.DrueckeFort();
        AssertEq(GameSimulationState.WaitReason.None,
            lauf.Simulation.WaitingFor,
            "**and the main menu closed**");
        AssertTrue(!lauf.Simulation.IsMainMenuActive,
            "**and its flag is down**");
        AssertTrue(!lauf.Simulation.IsSaveMenuActive,
            "**and the save menu stayed down**");
    }

    /// <summary>
    /// And a key press does not end a battle, and one result does.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the part that is not a
    /// convenience.</strong> -- <strong>A battle has three outcomes in
    /// the format and two of them are not the same</strong>, --
    /// <strong>and a key press that ended a battle would be a fourth
    /// one this repository invented.</strong>
    /// </para>
    /// </remarks>
    public void Test_EinKampfEndetNichtMitEinerTaste()
    {
        var lauf = Starte(out var host);
        if (lauf == null)
        {
            return;
        }

        using var _ = host!;
        lauf.Simulation.WaitingFor =
            GameSimulationState.WaitReason.BattleRunning;
        lauf.DrueckeFort();

        AssertEq(GameSimulationState.WaitReason.BattleRunning,
            lauf.Simulation.WaitingFor,
            "**and a key press does not end a battle** -- and a"
                + " battle ends with a result and not with a key");

        lauf.BeendeKampf(true);
        Console.WriteLine("nach Sieg: " + lauf.Simulation.WaitingFor);

        AssertEq(GameSimulationState.WaitReason.None,
            lauf.Simulation.WaitingFor,
            "**and a result ends it**");
        AssertTrue(!lauf.Simulation.IsBattleActive,
            "**and the battle is no longer active**");
    }

    /// <summary>
    /// And a key press with nothing open does nothing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And a method that changed the state when there was
    /// nothing to change would be a method that could not be called
    /// from an input handler without a guard.</strong>
    /// </para>
    /// </remarks>
    public void Test_OhneOffenenDialogNichts()
    {
        var lauf = Starte(out var host);
        if (lauf == null)
        {
            return;
        }

        using var _ = host!;
        AssertEq(GameSimulationState.WaitReason.None,
            lauf.Simulation.WaitingFor,
            "**and the page is running**");

        lauf.DrueckeFort();
        AssertEq(GameSimulationState.WaitReason.None,
            lauf.Simulation.WaitingFor,
            "**and a key press on a running page changes"
                + " nothing**");
        AssertEq(0, lauf.Simulation.FrameCount,
            "**and does not move the clock**");
    }
}
