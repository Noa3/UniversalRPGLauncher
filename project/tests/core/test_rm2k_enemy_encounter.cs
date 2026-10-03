using System;

using UniversalRPG.Rm2k;
using UniversalRPG.Rm2k.Interpreter;
using UniversalRPG.Rm2k.Presentation;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Command 10710, Enemy Encounter — the command that starts a battle, and the
/// five state fields that existed with nothing to set them.
/// </summary>
/// <remarks>
/// <para>
/// <c>IsBattleActive</c>, <c>ActiveTroopId</c>, <c>BattleTurn</c>,
/// <c>BattlePhase</c> and <c>TroopMembers</c> were all in the simulation
/// state. <strong>No command reached any of them</strong> — so a game's
/// encounter command fell into the default arm, no battle ever started, and the
/// state carried a battle phase of its own.
/// </para>
/// <para>
/// Same island shape as the pictures, the move routes and the vehicle boarding:
/// state that is complete, and unreachable. <strong>The only way to find it is
/// to ask what the state is for and compare it against what the reference's
/// commands do.</strong>
/// </para>
/// </remarks>
public partial class TestRm2kEnemyEncounter : TestBase
{
    /// <summary>
    /// The encounter command: troop mode, troop id, terrain mode, escape mode,
    /// defeat mode, first strike.
    /// </summary>
    /// <remarks>
    /// <strong>Six parameters, from the reference's plain dispatch line.</strong>
    /// The reference has a second line with a width of 10 for the RPG2K3 form,
    /// so a fixture padded to ten would be testing a shape this reader does not
    /// accept — and a fixture that left it at six is the common case.
    /// </remarks>
    private static Rm2kMap.EventCommand Encounter(
        int pTroopId = 1,
        int pTerrainMode = 0,
        int pEscapeMode = 0,
        int pDefeatMode = 0,
        int pFirstStrike = 0,
        int pWidth = 6)
    {
        var parameters = new System.Collections.Generic.List<int>
        {
            0, pTroopId, pTerrainMode, pEscapeMode, pDefeatMode, pFirstStrike,
        };
        while (parameters.Count < pWidth)
        {
            parameters.Add(0);
        }
        return new Rm2kMap.EventCommand
        {
            Code = EventInterpreter.EnemyEncounter,
            Text = "",
            Parameters = parameters,
        };
    }

    private static (EventInterpreter Interpreter, PresentationState Presentation,
        GameSimulationState State) Run(params Rm2kMap.EventCommand[] pCommands)
    {
        // **Und  die  Bank  gehoert  in  den  Zustand** -- **denn
        //  `10710`  baut  seine  Gegner  daraus**, --
        // **und  vorher  stand  hier  eine  leere  Bank  und  der
        //  Kampf  verweigerte  den  Start  und  die  Tests  schlugen
        //  fehl.**  **Das  war  richtig  und  die  Tests  sind  das,
        //  was  sich  aendern  musste.**
        var state = new GameSimulationState
        {
            MapId = 1,
            DatabaseData = Rm2kBegegnungsFixture.Bank(),
        };
        var presentation = new PresentationState();
        var interpreter = new EventInterpreter(state, 1, pCommands, presentation);
        return (interpreter, presentation, state);
    }

    // ---- Der Befehl startet einen Kampf

    /// <summary>
    /// An encounter command starts a battle.
    /// </summary>
    /// <remarks>
    /// <strong>This is the test the state could not write.</strong> All five
    /// fields were in the simulation, and a test that only checked "does the
    /// state have a BattlePhase" would have passed for ever on a dispatch that
    /// started nothing.
    /// </remarks>
    public void Test_AnEncounterCommandStartsABattle()
    {
        var (interpreter, _, state) = Run(Encounter(pTroopId: 3));

        interpreter.ExecuteFrame();

        AssertEq(
            state.IsBattleActive, true,
            "**and a battle is running**, which is what an unsupported command"
            + $" would not have done; it is {state.IsBattleActive}");
        AssertEq(
            state.ActiveTroopId, 3,
            "**with the troop the command named**, because the id comes through"
            + $" the mode and the value like every other number; it is {state.ActiveTroopId}");
    }

    /// <summary>
    /// The battle is at the player's turn, and no outcome yet.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Phase 1, not 0.</strong> The phase scale is 0 initial, 1 player,
    /// 2 enemy, 3 reward, 4 escape, -1 none. The battle has started, so the
    /// initial phase is behind it.
    /// </para>
    /// <para>
    /// <strong>And the subcommand is -1, which is "no outcome yet".</strong> A
    /// reader that wrote 0 would have a game whose victory arm ran before the
    /// battle was fought — and 0 is the reference's victory value, so the error
    /// would be invisible in a test that only checked the number.
    /// </para>
    /// </remarks>
    public void Test_TheBattleIsAtThePlayersTurnAndHasNoOutcome()
    {
        var (interpreter, _, state) = Run(Encounter());

        interpreter.ExecuteFrame();

        AssertEq(
            state.BattlePhase, 1,
            "**and the phase is the player's**, because phase 0 is the initial"
            + $" one and the battle has started; it is {state.BattlePhase}");
        AssertEq(
            state.BattleTurn, 0,
            "**and the turn is the first**, which is what a battle that has not"
            + $" advanced says; it is {state.BattleTurn}");
        AssertEq(
            state.BattleSubcommand, -1,
            "**and there is no outcome yet**, which is -1 and not the victory"
            + $" value of 0; it is {state.BattleSubcommand}");
    }

    // ---- Die vier Modi

    /// <summary>
    /// The escape mode is three values, and the middle one ends the event.
    /// </summary>
    /// <remarks>
    /// <strong>Not a boolean.</strong> The reference writes
    /// <c>escape_mode = com.parameters[3]</c> with 0 for "not at all", 1 for
    /// "end the event processing" and 2 for the game's own handler — and the
    /// middle one sets <c>abort_on_escape</c>, which ends the event. **A reader
    /// that read it as a boolean would have a game whose escape returned to the
    /// event's next line where the reference ends the event dead.**
    /// </remarks>
    public void Test_TheEscapeModeIsThreeValuesAndNotABoolean()
    {
        var (nicht, _, nichtState) = Run(Encounter(pEscapeMode: 0));
        nicht.ExecuteFrame();
        AssertEq(
            nichtState.BattleEscape, GameSimulationState.BattleEscapeMode.Disallow,
            "**and 0 means the battle cannot be escaped**;"
            + $" it is {nichtState.BattleEscape}");

        var (endet, _, endetState) = Run(Encounter(pEscapeMode: 1));
        endet.ExecuteFrame();
        AssertEq(
            endetState.BattleEscape, GameSimulationState.BattleEscapeMode.EndEvent,
            "**and 1 means escaping ends the event**, and not merely that it can"
            + $" be escaped; it is {endetState.BattleEscape}");

        var (eigen, _, eigenState) = Run(Encounter(pEscapeMode: 2));
        eigen.ExecuteFrame();
        AssertEq(
            eigenState.BattleEscape, GameSimulationState.BattleEscapeMode.CustomHandler,
            "**and 2 means the game's own handler decides**;"
            + $" it is {eigenState.BattleEscape}");
    }

    /// <summary>
    /// A defeat is a game over unless the command says otherwise.
    /// </summary>
    /// <remarks>
    /// <strong>Zero is the game over, and the reference pushes the screen
    /// itself.</strong> So a defeat mode of 1 is a game that has written its own
    /// defeat handling, and a reader that defaulted it to the custom handler
    /// would have a game where a defeat did nothing visible at all.
    /// </remarks>
    public void Test_ADefeatIsAGameOverUnlessTheCommandSaysOtherwise()
    {
        var (standard, _, standardState) = Run(Encounter(pDefeatMode: 0));
        standard.ExecuteFrame();
        AssertEq(
            standardState.BattleDefeat, GameSimulationState.BattleDefeatMode.GameOver,
            "**and a defeat of zero is a game over**, which is the reference's"
            + $" own default; it is {standardState.BattleDefeat}");

        var (eigen, _, eigenState) = Run(Encounter(pDefeatMode: 1));
        eigen.ExecuteFrame();
        AssertEq(
            eigenState.BattleDefeat, GameSimulationState.BattleDefeatMode.CustomHandler,
            "**and a defeat of one is the game's own handling**, because the"
            + $" author wrote arms for it; it is {eigenState.BattleDefeat}");
    }

    public void Test_TheFirstStrikeIsAFlagAndTheFourthTerrainModeIsRefused()
    {
        // **One variable and no tuple.** Every other test in this file
        // decomposes Run's return value and works; this one did not, and
        // seventeen measurements of a correct file are not a finding. A test
        // that cannot be read by a compiler is not a test.
        var first = Run(Encounter(pFirstStrike: 1));
        first.Interpreter.ExecuteFrame();
        AssertEq(
            first.State.BattleFirstStrike, true,
            "**and a first strike of one means the party goes first**;"
            + $" it is {first.State.BattleFirstStrike}");

        var refused = Run(Encounter(pTerrainMode: 3));
        refused.Interpreter.ExecuteFrame();
        AssertEq(
            refused.State.IsBattleActive, false,
            "**and a terrain mode of three starts no battle at all**, because the"
            + " reference refuses it; a battle is "
            + $"{refused.State.IsBattleActive}");
    }

    // ---- Das Warten

    /// <summary>
    /// An open message comes first, and the battle waits for it.
    /// </summary>
    /// <remarks>
    /// <strong>The same rule as the game over screen and the two menus.</strong>
    /// The reference's first two lines are
    /// <c>if (Game_Message::IsMessageActive()) return false;</c> — a battle that
    /// covers the line the author wrote for the moment it starts is a game that
    /// hid something.
    /// </remarks>
    public void Test_AnOpenMessageComesFirstAndTheBattleWaits()
    {
        var (interpreter, presentation, state) = Run(
            new Rm2kMap.EventCommand
            {
                Code = EventInterpreter.ShowMessage,
                Text = "A dragon appears!",
                Parameters = [0],
            },
            Encounter());
        interpreter.ExecuteFrame();
        AssertEq(
            presentation.MessageVisible, true,
            "**and the line is on screen**; it is " + $"{presentation.MessageVisible}");

        interpreter.ExecuteFrame();
        AssertEq(
            state.IsBattleActive, false,
            "**and no battle started over it**, because the reference waits for"
            + $" the message first; a battle is {state.IsBattleActive}");
        AssertEq(
            state.WaitingFor, GameSimulationState.WaitReason.MessageOpen,
            "**and the reason is the open message**, so a caller can tell it"
            + $" from a running battle; it is {state.WaitingFor}");

        // **Und  jetzt  wird  die  Zeile  gelesen.**
        //
        // **Und  das  ist  mehr  als  ein  `DismissMessage`**, --
        // **denn  seit  dem  Tastendruck-Fix  ist  das  Schliessen
        //  des  Fensters  eine  Sache  und  das  Aufloesen  der
        //  Wartefrage  eine  andere.**
        //
        // **Und  vorher  genuegte  das  Schliessen,  weil  die
        //  Begegnung  in  einer  Endlosschleife  hing** -- **und
        //  der  naechste  Frame  holte  sie  von  vorn  und  sie
        //  startete  trotzdem.**  **Und  das  war  der  Fehler,
        //  den  dieser  Test  nicht  sehen  konnte,  weil  es  wie
        //  ein  bestandener  Test  aussah.**
        presentation.DismissMessage();
        state.WaitingFor = GameSimulationState.WaitReason.None;
        interpreter.DialogGelesen();
        interpreter.ExecuteFrame();
        AssertEq(
            state.IsBattleActive, true,
            "**and after the line is read the battle starts**, which is"
            + " what waiting rather than skipping means; a battle is"
            + $" {state.IsBattleActive}");

        AssertEq(1, state.TroopMembers.Count,
            "**and it brought one monster with it** -- and before"
                + " the encounter learned to build its troop this"
                + " was zero while the battle said it was running");
    }

    /// <summary>
    /// A command with five parameters is a truncated file.
    /// </summary>
    /// <remarks>
    /// <strong>Six, from the reference's own dispatch line</strong> and not the
    /// ten the RPG2K3 form wants. A reader that required ten would refuse every
    /// 2K game's encounter.
    /// </remarks>
    public void Test_ACommandWithFiveParametersIsATruncatedFile()
    {
        var (interpreter, _, state) = Run(new Rm2kMap.EventCommand
        {
            Code = EventInterpreter.EnemyEncounter,
            Text = "",
            Parameters = [0, 1, 0, 0, 0],
        });

        interpreter.ExecuteFrame();

        AssertEq(
            state.IsBattleActive, false,
            "**and no battle started**, because a command of five parameters is"
            + $" a truncated file; a battle is {state.IsBattleActive}");
    }
}
