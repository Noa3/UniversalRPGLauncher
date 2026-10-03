using System;
using System.IO;
using UniversalRPG.Plugins;
using UniversalRPG.Rm2k;
using UniversalRPG.Rm2k.Interpreter;
using UniversalRPG.Rm2k.Presentation;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And that a page which is stepped twice does not get two troops.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this found a real fault in the wiring I added.</strong>
/// --
/// <strong>The encounter appends its members to
/// <c>TroopMembers</c></strong>, -- <strong>and the reference's
/// <c>EndBattle</c> clears <c>IsBattleActive</c> but never empties
/// the troop</strong>, -- <strong>and so a page that is stepped
/// again after the battle grew a second troop.</strong>
/// </para>
/// <para>
/// <strong>And two monsters with the same game data is not a fight,
/// it is a rendering accident</strong>, -- <strong>and a battle
/// screen that draws four when the game said two is a bug a player
/// sees immediately.</strong>
/// </para>
/// </remarks>
public partial class TestRm2kBegegnungVerdoppeltNicht : TestBase
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
    /// And a second battle replaces the first troop.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the fix belongs where the troop is built</strong>,
    /// -- <strong>not in a reset</strong>, -- <strong>because a reset
    /// would be a second thing to keep in step.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieZweiteBegegnungErsetztDieErste()
    {
        var lauf = Starte(out var host);
        if (lauf == null)
        {
            return;
        }

        using var h = host!;
        var state = lauf.Simulation;
        var befehle = new Rm2kMap.EventCommand[]
        {
            new()
            {
                Code = EventInterpreter.EnemyEncounter,
                Text = "",
                Parameters = new System.Collections.Generic
                    .List<int> { 0, 2, 0, 1, 0, 0 },
            },
        };

        var interpreter = new EventInterpreter(
            state, 1, befehle, new PresentationState());

        interpreter.ExecuteFrame();
        Console.WriteLine("1. Kampf: " + state.TroopMembers.Count);
        AssertEq(1, state.TroopMembers.Count,
            "**and the first battle has one monster**");

        lauf.BeendeKampf(true);
        interpreter.ExecuteFrame();
        Console.WriteLine("nach dem Ende: " + state.TroopMembers.Count
            + " Monster");

        AssertEq(1, state.TroopMembers.Count,
            "**and the second pass does not add a second monster**"
                + " -- and a battle screen that draws two where the"
                + " game said one is a fault a player sees at once");
    }

    /// <summary>
    /// And a second battle with two monsters replaces two, not four.
    /// </summary>
    public void Test_EineZweimonstertruppeBleibtZwei()
    {
        var lauf = Starte(out var host);
        if (lauf == null)
        {
            return;
        }

        using var h = host!;
        var state = lauf.Simulation;
        // **Truppe  4  ist  zwei  Scorpions** -- **gemessen  in
        //  `test_rm2k_echte_begegnung_startet_kampf`.**
        var befehle = new Rm2kMap.EventCommand[]
        {
            new()
            {
                Code = EventInterpreter.EnemyEncounter,
                Text = "",
                Parameters = new System.Collections.Generic
                    .List<int> { 0, 4, 0, 1, 0, 0 },
            },
        };

        var interpreter = new EventInterpreter(
            state, 1, befehle, new PresentationState());
        interpreter.ExecuteFrame();
        var erste = state.TroopMembers.Count;
        Console.WriteLine("Truppe 4: " + erste + " Monster");
        AssertEq(2, erste, "**and troop four has two**");

        lauf.BeendeKampf(true);
        interpreter.ExecuteFrame();
        Console.WriteLine("nach dem Ende: " + state.TroopMembers.Count);

        AssertEq(erste, state.TroopMembers.Count,
            "**and it stays two**");
    }
}
