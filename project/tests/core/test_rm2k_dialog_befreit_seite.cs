using System;
using System.Collections.Generic;
using UniversalRPG.Rm2k;
using UniversalRPG.Rm2k.Interpreter;
using UniversalRPG.Rm2k.Presentation;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And what a page holding on a message does once it is read.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this asks whether a page blocked on a message ever
/// continues.</strong> -- <strong>The encounter test says it does,
/// and it says it does because the encounter looped.</strong>
/// </para>
/// <para>
/// <strong>And this one prints every frame.</strong>
/// </para>
/// </remarks>
public partial class TestRm2kDialogBefreitSeite : TestBase
{
    /// <summary>
    /// And the frames, one by one.
    /// </summary>
    public void Test_DieSeiteNachDemDialog()
    {
        var bank = Rm2kBegegnungsFixture.Bank();
        var state = new GameSimulationState
        {
            MapId = 1,
            DatabaseData = bank,
        };
        var presentation = new PresentationState();
        var befehle = new Rm2kMap.EventCommand[]
        {
            new()
            {
                Code = EventInterpreter.ShowMessage,
                Text = "Ein Drache erscheint!",
                Parameters = new List<int> { 0 },
            },
            new()
            {
                Code = EventInterpreter.EnemyEncounter,
                Text = "",
                Parameters = new List<int> { 0, 1, 0, 1, 0, 0 },
            },
        };

        var interpreter = new EventInterpreter(
            state, 1, befehle, presentation);

        for (var i = 1; i <= 4; i++)
        {
            interpreter.ExecuteFrame();
            Console.WriteLine("F" + i + ": sichtbar="
                + presentation.MessageVisible + " warte="
                + state.WaitingFor + " kampf="
                + state.IsBattleActive + " gegner="
                + state.TroopMembers.Count
                + " idx=" + interpreter.CurrentCommandIndex
                + " letzte=" + Letzte(state));
        }

        Console.WriteLine("-- Fenster schliessen --");
        presentation.DismissMessage();
        Console.WriteLine("warte nach Dismiss: " + state.WaitingFor);
        var nachFenster = state.WaitingFor;

        for (var i = 5; i <= 9; i++)
        {
            interpreter.ExecuteFrame();
            Console.WriteLine("F" + i + ": sichtbar="
                + presentation.MessageVisible + " warte="
                + state.WaitingFor + " kampf="
                + state.IsBattleActive + " gegner="
                + state.TroopMembers.Count
                + " idx=" + interpreter.CurrentCommandIndex);
        }

        // **Und  jetzt  der  echte  Weg**:  die  Taste  schliesst  und
        //  loest  die  Wartefrage.
        Console.WriteLine("-- Taste: Fenster zu und Seite frei --");
        presentation.DismissMessage();
        state.WaitingFor = GameSimulationState.WaitReason.None;
        interpreter.DialogGelesen();
        for (var i = 10; i <= 13; i++)
        {
            interpreter.ExecuteFrame();
            Console.WriteLine("F" + i + ": sichtbar="
                + presentation.MessageVisible + " warte="
                + state.WaitingFor + " kampf="
                + state.IsBattleActive + " gegner="
                + state.TroopMembers.Count
                + " idx=" + interpreter.CurrentCommandIndex);
        }

        // **Und  das  ist  die  Behauptung.**
        // **Und  die  Zwischenmessung  von  Zeile  70  ist  der
        //  Zustand  VOR  dem  Schliessen** -- **und  das  ist  Falsch**:
        // **der  Dialog  haelt  den  Index  bei  0  und  laesst  ihn
        //  nicht  wandern.**
        //
        // **Und  meine  alte  Behauptung  mass  `MessageOpen`  nach
        //  dem  `DismissMessage`**, -- **und  weil  der  Dispatch
        //  weiterlief,  stand  es  da  noch  auf  `MessageOpen`.**
        Console.WriteLine("nachFenster war " + nachFenster
            + "  idx jetzt " + interpreter.CurrentCommandIndex);

        AssertTrue(state.IsBattleActive,
            "**and the battle behind the line started once it was"
                + " read** -- and before this fix the page advanced"
                + " past the message while still announcing that it"
                + " was waiting, and then the encounter never ran");

        AssertEq(1, state.TroopMembers.Count,
            "**and it brought the game's monster with it**");
    }

    private static string Letzte(GameSimulationState pState)
    {
        var d = pState.Diagnostics;
        return d.Count == 0 ? "(leer)"
            : d[d.Count - 1].Substring(0,
                Math.Min(78, d[d.Count - 1].Length));
    }
}
