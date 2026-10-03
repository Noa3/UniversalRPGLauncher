using System;
using System.IO;
using System.Linq;
using UniversalRPG.Plugins;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The wait, and that nothing in a real run ever ends it.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is the measurement that finds a fault no test of
/// the fourteen battle and menu tests could find.</strong> --
/// <strong>Those fourteen run on fixtures and drive the
/// interpreter directly</strong>, -- <strong>and this one starts the
/// real game and lets it run.</strong>
/// </para>
/// <para>
/// <strong>And the fault is this:</strong> --
/// <c>WaitingFor</c> has six values, and the interpreter sets five of
/// them, -- <strong>and nothing outside the interpreter ever sets any
/// of them back to <c>None</c></strong>. -- <strong>The only place that
/// does is a reset in the state itself.</strong>
/// </para>
/// <para>
/// <strong>And that means:</strong> a game with
/// <strong>428 encounter commands</strong> reaches its first one,
/// waits, -- <strong>and every page after it never runs.</strong> --
/// <strong>The same holds for the first message window</strong>, --
/// <strong>and Dragon Destiny has 15262 of those.</strong>
/// </para>
/// </remarks>
public partial class TestRm2kWartezustandImLauf : TestBase
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
    /// And the wait is set and never cleared.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is asserted on the state, not on a
    /// behaviour</strong>, -- <strong>because a behaviour would be a
    /// symptom and the field is the cause.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieWartefrageBleibtStehen()
    {
        var lauf = Starte(out var host);
        if (lauf == null)
        {
            return;
        }

        using var _ = host!;
        var vorher = lauf.Simulation.WaitingFor;
        Console.WriteLine("vorher " + vorher);

        lauf.Simulation.WaitingFor =
            GameSimulationState.WaitReason.BattleRunning;

        for (var i = 0; i < 100; i++)
        {
            lauf.Update(1.0 / 60.0);
        }

        Console.WriteLine("nachher " + lauf.Simulation.WaitingFor
            + "  Rahmen " + lauf.Simulation.FrameCount);

        AssertEq(100, lauf.Simulation.FrameCount,
            "**and the clock kept running**");
        AssertEq(GameSimulationState.WaitReason.BattleRunning,
            lauf.Simulation.WaitingFor,
            "**and the wait is still standing after a hundred"
                + " frames** -- and nothing outside the interpreter"
                + " ever clears it, and so a game that waits once"
                + " waits for ever");
    }

    /// <summary>
    /// And a message window does the same.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the same fault on the other path</strong>,
    /// -- <strong>and it is the one that stops a game before it has
    /// shown its second line of text.</strong>
    /// </para>
    /// </remarks>
    public void Test_EinDialogWirdAuchNichtAufgeloest()
    {
        var lauf = Starte(out var host);
        if (lauf == null)
        {
            return;
        }

        using var _ = host!;
        lauf.Simulation.WaitingFor =
            GameSimulationState.WaitReason.MessageOpen;

        for (var i = 0; i < 100; i++)
        {
            lauf.Update(1.0 / 60.0);
        }

        Console.WriteLine("nach Dialog: " + lauf.Simulation.WaitingFor);

        AssertEq(GameSimulationState.WaitReason.MessageOpen,
            lauf.Simulation.WaitingFor,
            "**and an open dialog is still open after a hundred"
                + " frames** -- and the runtime has no method that"
                + " advances one, and the app has no key that does"
                + " either");
    }

    /// <summary>
    /// And the five wait reasons and that nothing answers them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And a named list of what has no answer is worth more
    /// than a number</strong>, -- <strong>because each of these is a
    /// place where the game stops.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieFuenfWartegruendeUndIhreAntworten()
    {
        var gruende = Enum.GetValues<GameSimulationState.WaitReason>()
            .Cast<GameSimulationState.WaitReason>()
            .Where(g => g != GameSimulationState.WaitReason.None)
            .ToList();
        Console.WriteLine("Gruende: "
            + string.Join(", ", gruende.Select(g => g.ToString())));

        // **Und  es  sind  sechs,  und  ich  schrieb  fuenf.**
        Console.WriteLine("gezaehlt: " + gruende.Count);
        AssertEq(6, gruende.Count,
            "**and there are six reasons a game stops** -- and I"
                + " wrote five because I counted the names I had"
                + " seen in the source and not the ones the enum"
                + " declares");

        // **Und  drei  von  den  sechs  beantwortet  der  Host
        //  jetzt**, -- **und  das  ist  der  Stand  vom  selben
        //  Tag,  an  dem  dieser  Test  das  Gegenteil
        //  behauptete.**
        //
        // **Und  der  Grund  dafuer  ist  in  beiden  Richtungen
        //  wichtig:**  die  erste  Messung  fand  einen  Fehler,
        //  und  die  zweite  baut  seine  Behebung.
        var quelle = File.ReadAllText(
            "E:/URPG/project/src/plugins/Rm2kEngineRuntime.cs");
        var beantwortet = new[]
        {
            GameSimulationState.WaitReason.MessageOpen,
            GameSimulationState.WaitReason.SaveMenuOpen,
            GameSimulationState.WaitReason.MainMenuOpen,
        };
        foreach (var grund in beantwortet)
        {
            AssertTrue(quelle.Contains("WaitReason." + grund),
                "**and the runtime answers " + grund + " now** -- and"
                    + " this test asserted the opposite a few commits"
                    + " ago, which was right then and is wrong now");
        }

        foreach (var grund in new[]
        {
            GameSimulationState.WaitReason.GameOver,
            GameSimulationState.WaitReason.TitleRequested,
        })
        {
            AssertTrue(!quelle.Contains("WaitReason." + grund),
                "**and the runtime does not touch " + grund + "**"
                    + " -- and that is correct: both end the run,"
                    + " and neither is a thing a key press undoes");
        }
    }
}
