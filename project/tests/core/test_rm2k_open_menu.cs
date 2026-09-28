using System;
using System.Collections.Generic;

using UniversalRPG.Rm2k;
using UniversalRPG.Rm2k.Interpreter;
using UniversalRPG.Rm2k.Presentation;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Commands 11910 and 11950 — the two that open a menu, and the two that are
/// not the same as the three that say whether the player may.
/// </summary>
/// <remarks>
/// <para>
/// The board listed <c>11910</c>, <c>11930</c>, <c>11950</c> and
/// <c>11960</c> as one open family. <strong>Re-measured, that is two.</strong>
/// <c>ChangeSaveAccess</c> (11930) and <c>ChangeMainMenuAccess</c> (11960) were
/// already dispatched through the one-line access handler with the teleport and
/// escape commands; what was missing was the pair that <em>opens</em> a menu.
/// </para>
/// <para>
/// <strong>The two are not the same shape.</strong> The access commands take one
/// parameter and flip a flag. The openers take none at all, and the reference's
/// dispatch line says zero — so a reader that required a parameter would refuse
/// every game's menu.
/// </para>
/// </remarks>
public partial class TestRm2kOpenMenu : TestBase
{
    /// <summary>An opener command, which takes no parameters.</summary>
    private static Rm2kMap.EventCommand Opener(int pCode)
    {
        return new Rm2kMap.EventCommand
        {
            Code = pCode,
            Text = "",
            Parameters = [],
        };
    }

    private static (EventInterpreter Interpreter, PresentationState Presentation,
        GameSimulationState State) Run(params Rm2kMap.EventCommand[] pCommands)
    {
        var state = new GameSimulationState { MapId = 1 };
        var presentation = new PresentationState();
        var interpreter = new EventInterpreter(state, 1, pCommands, presentation);
        return (interpreter, presentation, state);
    }

    // ---- Der Befehl erreicht den Zustand

    /// <summary>
    /// A save menu command is a save menu and not an unsupported command.
    /// </summary>
    /// <remarks>
    /// <strong>With no parameters at all.</strong> The reference gives the
    /// command a width of zero, so a fixture that padded it would have been
    /// testing a command the format does not have.
    /// </remarks>
    public void Test_ASaveMenuCommandOpensTheSaveMenu()
    {
        var (interpreter, _, state) = Run(Opener(EventInterpreter.OpenSaveMenu));

        interpreter.ExecuteFrame();

        AssertEq(
            state.IsSaveMenuActive, true,
            "**and the save menu is up**, which is what an unsupported command"
            + $" would not have done; it is {state.IsSaveMenuActive}");
        AssertEq(
            state.WaitingFor, GameSimulationState.WaitReason.SaveMenuOpen,
            "**and the page holds on the menu**, because the reference makes it"
            + $" an asynchronous operation; it is {state.WaitingFor}");
    }

    /// <summary>
    /// A main menu command opens the main menu.
    /// </summary>
    /// <remarks>
    /// <strong>Its own flag, and not the save one.</strong> A reader that stored
    /// "a menu" in a single field would have the save command clear the main
    /// menu's request — and a game that opened the main menu and then saved
    /// would find neither of them up.
    /// </remarks>
    public void Test_AMainMenuCommandOpensTheMainMenu()
    {
        var (interpreter, _, state) = Run(Opener(EventInterpreter.OpenMainMenu));

        interpreter.ExecuteFrame();

        AssertEq(
            state.IsMainMenuActive, true,
            "**and the main menu is up**;"
            + $" it is {state.IsMainMenuActive}");
        AssertEq(
            state.IsSaveMenuActive, false,
            "**and the save menu is not**, because the two have their own flags"
            + $" and not one field between them; it is {state.IsSaveMenuActive}");
    }

    /// <summary>
    /// A held page re-runs its command, and that is what a menu open costs.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>The second command is never reached, and that is correct.</strong>
    /// The page holds at the first opener, the index does not move, and every
    /// frame runs the same command again — so a program that opens the main
    /// menu and then the save menu gets the main menu, for as long as the
    /// player is in it. **This is the price of a held page, and the reference
    /// pays the same price**: a game that wants both menus opens one, closes it,
    /// and reaches the next.
    /// </para>
    /// <para>
    /// <strong>Which is why the two flags are independent and the test says
    /// so.</strong> The second command never runs here, so it cannot prove the
    /// flags do not collide — the test for that is
    /// <c>Test_AMainMenuCommandOpensTheMainMenu</c>, which checks that the save
    /// flag is untouched by a main menu command. <strong>A test that claimed
    /// otherwise would be asserting a behaviour the format does not have.</strong>
    /// </para>
    /// </remarks>
    public void Test_OneMenuDoesNotCloseTheOther()
    {
        var (interpreter, _, state) = Run(
            Opener(EventInterpreter.OpenMainMenu),
            Opener(EventInterpreter.OpenSaveMenu));

        interpreter.ExecuteFrame();
        AssertEq(
            state.IsMainMenuActive, true,
            "**and the main menu opened**, because it is the first command;"
            + $" it is {state.IsMainMenuActive}");

        // **The page holds, and the same command runs again.** Three frames, so
        // the shape of a hold is visible and not just its first frame.
        interpreter.ExecuteFrame();
        interpreter.ExecuteFrame();
        AssertEq(
            state.IsMainMenuActive, true,
            "**and it is still the main menu on the third frame**, because the"
            + " index did not move and the second command was never reached;"
            + $" it is {state.IsMainMenuActive}");
        AssertEq(
            state.IsSaveMenuActive, false,
            "**and the save menu was never opened**, which is what a held page"
            + " means and not a bug; it is {state.IsSaveMenuActive}");
        AssertEq(
            state.WaitingFor, GameSimulationState.WaitReason.MainMenuOpen,
            "**and the reason is still the main menu**, so the re-run wrote the"
            + $" same reason and not the save one; it is {state.WaitingFor}");
    }

    /// <summary>
    /// Opening a menu is not changing the access to it.
    /// </summary>
    /// <remarks>
    /// <strong>The pair the board listed as one family, measured apart.</strong>
    /// <c>11930</c> says whether the player may save and flips a flag; this one
    /// opens the menu. A reader that treated the second as the first would have
    /// a game whose save menu opened every time a cutscene unlocked saving —
    /// and one that never opened when a player pressed the button.
    /// </remarks>
    public void Test_OpeningAMenuIsNotChangingTheAccessToIt()
    {
        var (interpreter, _, state) = Run(Opener(EventInterpreter.OpenSaveMenu));

        interpreter.ExecuteFrame();

        AssertEq(
            state.AllowSave, true,
            "**and the access is untouched**, because this command opens the menu"
            + " and the other one changes what the player may do; it is "
            + $"{state.AllowSave}");

        // **And the access command still works, and still does not open
        // anything.**
        var (zugang, _, zustand) = Run(new Rm2kMap.EventCommand
        {
            Code = EventInterpreter.ChangeSaveAccess,
            Text = "",
            Parameters = [0],
        });
        zugang.ExecuteFrame();
        AssertEq(
            zustand.AllowSave, false,
            "**and the access command locks saving**, which is its own thing;"
            + $" it is {zustand.AllowSave}");
        AssertEq(
            zustand.IsSaveMenuActive, false,
            "**and it opened no menu**, because that is not what it does;"
            + $" it is {zustand.IsSaveMenuActive}");
    }

    /// <summary>
    /// An open message comes first, and the menu waits for it.
    /// </summary>
    /// <remarks>
    /// <strong>The same rule as the game over screen and the title request.</strong>
    /// The reference's first two lines are
    /// <c>if (Game_Message::IsMessageActive()) return false;</c> — a hero who
    /// says "here, take this menu" and has the menu cover the line is a game
    /// that hid a line the author wrote for that moment.
    /// </remarks>
    public void Test_AnOpenMessageComesFirstAndTheMenuWaits()
    {
        var (interpreter, presentation, state) = Run(
            new Rm2kMap.EventCommand
            {
                Code = EventInterpreter.ShowMessage,
                Text = "Take a look at these goods.",
                Parameters = [0],
            },
            Opener(EventInterpreter.OpenSaveMenu));

        interpreter.ExecuteFrame();
        AssertEq(
            presentation.MessageVisible, true,
            "**and the line is on screen**, which is what the show command does;"
            + $" it is {presentation.MessageVisible}");

        interpreter.ExecuteFrame();
        AssertEq(
            state.IsSaveMenuActive, false,
            "**and the menu did not open over it**, because the reference waits"
            + $" for the message first; it is {state.IsSaveMenuActive}");
        AssertEq(
            state.WaitingFor, GameSimulationState.WaitReason.MessageOpen,
            "**and the reason is the open message**, not the menu, so a caller"
            + $" can tell the two apart; it is {state.WaitingFor}");

        // **And once the line is read, the menu opens.**
        presentation.DismissMessage();
        interpreter.ExecuteFrame();
        AssertEq(
            state.IsSaveMenuActive, true,
            "**and after the line is dismissed the menu opens**, which is what"
            + $" waiting rather than skipping means; it is {state.IsSaveMenuActive}");
    }

    /// <summary>
    /// The page holds, and the same command runs again until the menu is gone.
    /// </summary>
    /// <remarks>
    /// <strong>A held page and not a skip.</strong> The event continues when the
    /// menu closes, so advancing past the command would have a cutscene run its
    /// next lines behind a menu the player is still looking at.
    /// </remarks>
    public void Test_ThePageHoldsWhileTheMenuIsUp()
    {
        var (interpreter, _, state) = Run(
            Opener(EventInterpreter.OpenSaveMenu),
            new Rm2kMap.EventCommand
            {
                // **A switch set, and not a variable**, because the switch list
                // is the one an event writes and the one a reader can see in
                // the state without going through the interpreter's own
                // accessor.
                Code = EventInterpreter.ControlSwitches,
                Text = "",
                Parameters =
                [
                    EventInterpreter.TargetEvalSingle, 1, 1,
                    EventInterpreter.SwitchModeOn,
                ],
            });

        interpreter.ExecuteFrame();
        AssertEq(
            state.Switches.Count > 0 && state.Switches[0], false,
            "**and the line after the menu has not run**, because the page holds"
            + " while the menu is up; the switch is "
            + $"{state.Switches.Count > 0 && state.Switches[0]}");

        // **And the same command runs again the next frame** -- that is what a
        // held page is: the index did not move.
        state.IsSaveMenuActive = false;
        state.WaitingFor = GameSimulationState.WaitReason.None;
        interpreter.ExecuteFrame();
        AssertEq(
            state.IsSaveMenuActive, true,
            "**and with the menu gone the next frame asks for it again**, which"
            + " is the price of holding the index, and the reference pays the"
            + $" same; it is {state.IsSaveMenuActive}");
    }
}
