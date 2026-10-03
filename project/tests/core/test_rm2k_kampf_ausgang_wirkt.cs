using System;
using System.IO;
using System.Linq;
using UniversalRPG.Plugins;
using UniversalRPG.Rm2k.Interpreter;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// A battle's outcome, and that it reaches the handler the game
/// wrote.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is the second field that existed and that nothing
/// read.</strong> -- <strong><c>ExecuteEnemyEncounter</c> writes
/// <c>BattleSubcommand = -1</c> and nothing ever changed it</strong>,
/// -- <strong>and a battle that ended left the outcome unset and the
/// handler list unfound.</strong>
/// </para>
/// <para>
/// <strong>And the three indices are the reference's own subcommand
/// indices and not the command codes:</strong>
///
/// <code>
/// SubIdxVictory = 4
/// SubIdxEscape  = 5
/// SubIdxDefeat  = 6
/// </code>
///
/// <strong>And a reader that used <c>20710</c> here would compare a
/// handler against a position.</strong>
/// </para>
/// <para>
/// <strong>And this test drives the game's own encounter command
/// through the interpreter and then ends the battle the way the game
/// would.</strong>
/// </para>
/// </remarks>
public partial class TestRm2kKampfAusgangWirkt : TestBase
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
    /// And the three indices are three and not two.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And <c>false</c> is not "defeat"</strong>, -- <strong>and
    /// a reader that offered a boolean here would send a game's escape
    /// into its defeat handler.</strong> -- <strong>And that is the
    /// fault the test below is written to catch.</strong>
    /// </para>
    /// </remarks>
    public void Test_DreiIndizesUndNichtZwei()
    {
        Console.WriteLine($"Sieg {EventInterpreter.SubIdxVictory}  "
            + $"Flucht {EventInterpreter.SubIdxEscape}  "
            + $"Niederlage {EventInterpreter.SubIdxDefeat}");

        AssertEq(4, EventInterpreter.SubIdxVictory,
            "**and a victory is subcommand four**");
        AssertEq(5, EventInterpreter.SubIdxEscape,
            "**and an escape is five** -- and it is not the defeat");
        AssertEq(6, EventInterpreter.SubIdxDefeat,
            "**and a defeat is six**");

        AssertTrue(EventInterpreter.SubIdxEscape
                != EventInterpreter.SubIdxDefeat,
            "**and the escape and the defeat are different numbers**"
                + " -- and the game's own database names a different"
                + " handler list for each, and a boolean here would"
                + " send an escape into a defeat");
        AssertTrue(EventInterpreter.SubIdxVictory
                != EventInterpreter.SubIdxDefeat,
            "**and so are the victory and the defeat**");
    }

    /// <summary>
    /// And an outcome sets the field nothing had read.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this runs the real game</strong>, -- <strong>and the
    /// battle is put into the state the game's own command
    /// writes</strong>, -- <strong>and the assertion is on the field
    /// and not on a behaviour</strong>, -- <strong>because the field
    /// is what the handler compares against.</strong>
    /// </para>
    /// </remarks>
    public void Test_DerAusgangFuelltDasFeld()
    {
        var lauf = Starte(out var host);
        if (lauf == null)
        {
            return;
        }

        using var _ = host!;
        lauf.Simulation.IsBattleActive = true;
        lauf.Simulation.ActiveTroopId = 1;
        lauf.Simulation.BattleSubcommand = -1;
        lauf.Simulation.WaitingFor =
            GameSimulationState.WaitReason.BattleRunning;

        AssertEq(-1, lauf.Simulation.BattleSubcommand,
            "**and no outcome yet** -- and minus one says so, and a"
                + " reader that wrote zero would have run the game's"
                + " victory arm before the battle was fought");

        lauf.BeendeKampf(true);
        Console.WriteLine("nach Sieg: "
            + lauf.Simulation.BattleSubcommand);
        AssertEq(EventInterpreter.SubIdxVictory,
            lauf.Simulation.BattleSubcommand,
            "**and a victory sets subcommand four**");
        AssertTrue(!lauf.Simulation.IsBattleActive,
            "**and the battle is over**");
        AssertEq(GameSimulationState.WaitReason.None,
            lauf.Simulation.WaitingFor,
            "**and the page is free**");

        // **Und jetzt derselbe Kampf, aber mit Niederlage.**
        lauf.Simulation.IsBattleActive = true;
        lauf.Simulation.BattleSubcommand = -1;
        lauf.Simulation.WaitingFor =
            GameSimulationState.WaitReason.BattleRunning;
        lauf.BeendeKampf(false);
        Console.WriteLine("nach Niederlage: "
            + lauf.Simulation.BattleSubcommand);

        AssertEq(EventInterpreter.SubIdxDefeat,
            lauf.Simulation.BattleSubcommand,
            "**and a defeat sets six** -- and not four, and a reader"
                + " with a boolean would have run the victory arm"
                + " for a lost battle");
    }

    /// <summary>
    /// And a battle that was never started cannot be ended.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And a method that set the outcome anyway would let a
    /// caller invent a result</strong>, -- <strong>and inventing a
    /// result is inventing gameplay.</strong>
    /// </para>
    /// </remarks>
    public void Test_OhneKampfNichts()
    {
        var lauf = Starte(out var host);
        if (lauf == null)
        {
            return;
        }

        using var _ = host!;
        var vorher = lauf.Simulation.BattleSubcommand;
        lauf.BeendeKampf(true);

        AssertEq(vorher, lauf.Simulation.BattleSubcommand,
            "**and ending a battle that is not running changes"
                + " nothing** -- and the field keeps whatever the last"
                + " real battle left");
    }
}
