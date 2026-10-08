using System;
using System.Collections.Generic;
using System.IO;
using UniversalRPG.App.Ui;
using UniversalRPG.Web;
using UniversalRPG.Plugins;
using UniversalRPG.Rm2k.Input;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And an MZ game's dialogue reaches the player, and holds them while it does.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this closes the largest MV/MZ gap there was.</strong> Measured
/// before this: the runtime had <c>MessageText</c>, <c>MessageVisible</c>,
/// <c>ChoiceOptions</c>, <c>ChoicePending</c> and <c>CloseMessage()</c>, all
/// covered by tests -- and <c>grep -rnE "mz\.(MessageText|ChoiceOptions)"
/// project/app/</c> found <em>nothing</em>. The presentation block belonged to
/// RM2K. So in a real MV/MZ game the player could walk around and
/// <strong>nothing was ever said</strong>.
/// </para>
/// <para>
/// <strong>And the player could walk away from a conversation.</strong>
/// Measured in the project's own <c>rmmz_objects.js</c>:
/// </para>
/// <code>
/// Game_Player.prototype.canMove = function() {
///     if ($gameMap.isEventRunning() || $gameMessage.isBusy()) {
///         return false;
///     }
///     ...
/// };
/// </code>
/// <para>
/// so a direction key must reach the player only when nothing is being said.
/// </para>
/// </remarks>
public partial class TestMzMessageOnScreen : TestBase
{
    private const string Projekt = "E:/RPGMakerGames/CamelliaCoronation-Win";

    private static bool Vorhanden() =>
        File.Exists(Projekt + "/data/Map003.json")
        && File.Exists(Projekt + "/data/System.json");

    /// <summary>And a running game, driven to its own first dialogue.</summary>
    private static (EnginePluginHost Host, MzEngineRuntime Runtime) BisZumDialog()
    {
        var host = new EnginePluginHost(
            BuiltInEnginePluginCatalog.CreateRuntimeRegistry());
        var started = host.Start(new PluginGameInfo
        {
            GameDirectory = Projekt,
            EngineId = EnginePluginIds.RpgMakerMz,
            Generation = "mz",
            DetectorScore = 850,
        });
        if (!started.Success)
        {
            host.Dispose();
            throw new InvalidOperationException(
                $"the MZ game did not start: {started.Error?.Message}");
        }

        var runtime = (MzEngineRuntime)host.Runtime!;
        runtime.Transfer(3, 4, 11);
        for (var frame = 0; frame < 3000 && !runtime.MessageVisible; frame++)
        {
            runtime.Update(1.0 / 60.0);
        }
        return (host, runtime);
    }

    /// <summary>
    /// And a dialogue holds the player until the decision key takes it down.
    /// </summary>
    public void Test_EinDialogHaeltDenSpieler()
    {
        if (!Vorhanden())
        {
            return;
        }

        var (host, runtime) = BisZumDialog();
        using var _ = host;
        AssertTrue(runtime.MessageVisible,
            "the game's own dialogue reaches the window");
        Console.WriteLine($"MZ message on screen: \"{runtime.MessageText}\"");
        AssertTrue(runtime.MessageText.Trim().Length > 0,
            "and carries the game's own text");

        // **And the direction keys do not reach the player while it is up.**
        var vorherX = runtime.PlayerX;
        var vorherY = runtime.PlayerY;
        Console.WriteLine($"MZ player held at {vorherX}/{vorherY}, "
            + $"holdsPlayer={runtime.MessageHoldsPlayer}");
        AssertTrue(runtime.MessageHoldsPlayer,
            "an open message holds the player, which is $gameMessage.isBusy()");

        AssertFalse(runtime.SubmitInput(Rm2kInputAction.MoveDown),
            "a direction key is not the message's to take");
        AssertEq(runtime.PlayerY, vorherY,
            "and the player does not step while the game is talking");
        AssertEq(runtime.PlayerX, vorherX, "in either direction");

        // **And the decision key takes it down.**
        AssertTrue(runtime.SubmitInput(Rm2kInputAction.Confirm),
            "the decision key takes the dialogue down");
        AssertFalse(runtime.MessageVisible, "and it is gone");
        AssertFalse(runtime.MessageHoldsPlayer, "and the player is free again");
    }

    /// <summary>
    /// And a choice is pointed at with the arrow keys and taken with ok.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is <c>Window_ChoiceList</c>'s cursor.</strong> Measured
    /// before this: a choice was modelled and answerable <em>by number</em>,
    /// and the arrow keys moved the player, because <c>SubmitInput</c> read
    /// every direction as a step. A player could not point at an answer.
    /// </para>
    /// <para>
    /// <strong>And the question is opened through the runtime's own facts</strong>,
    /// with the game's own two answers -- measured in this project's
    /// <c>Map004.json</c>, event 14 asks <c>102 [["Yes", "No"], 1, 0, 2, 0]</c>.
    /// That event is not reachable at the start of the game: its page requires
    /// switch 3. What is being tested here is the cursor, so the question is
    /// opened directly and the answers are the game's.
    /// </para>
    /// </remarks>
    public void Test_EineWahlNimmtDieRichtungstasten()
    {
        if (!Vorhanden())
        {
            return;
        }

        var (host, runtime) = BisZumDialog();
        using var _ = host;

        runtime.CloseMessage();
        runtime.Facts.StartChoice(
            new MzChoice.Set { Options = new[] { "Yes", "No" }, DefaultType = 0 },
            false);
        AssertTrue(runtime.ChoicePending, "the question is open");
        Console.WriteLine($"MZ choice on screen: "
            + $"[{string.Join(" / ", runtime.ChoiceOptions)}] "
            + $"cursor={runtime.ChoiceIndex}");
        AssertEq(runtime.ChoiceOptions.Count, 2,
            "and it is the game's own question with two answers");

        var vorherX = runtime.PlayerX;
        var vorherY = runtime.PlayerY;
        AssertEq(runtime.ChoiceIndex, 0, "the cursor starts on the first option");

        AssertTrue(runtime.SubmitInput(Rm2kInputAction.MoveDown),
            "the down key is the choice's to take");
        AssertEq(runtime.ChoiceIndex, 1, "and it moves the cursor, not the player");
        AssertEq(runtime.PlayerY, vorherY, "and the player stands still");
        AssertEq(runtime.PlayerX, vorherX, "in either direction");

        AssertTrue(runtime.SubmitInput(Rm2kInputAction.MoveUp),
            "the up key moves it back");
        AssertEq(runtime.ChoiceIndex, 0, "to the first option");

        // And up from the first option wraps to the last, which is what
        // Window_Selectable does with a fresh press.
        AssertTrue(runtime.SubmitInput(Rm2kInputAction.MoveUp), "and past the top");
        AssertEq(runtime.ChoiceIndex, 1, "it wraps to the last option");

        AssertTrue(runtime.SubmitInput(Rm2kInputAction.Confirm),
            "and the decision key answers the option the cursor is on");
        AssertFalse(runtime.ChoicePending, "so the question is answered");
        AssertEq(runtime.Facts.ChoiceResult, 2,
            "and it landed in the branch the cursor pointed at");
    }

    /// <summary>
    /// And a figure stands in the way, and can be talked to from beside it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this was the reason no NPC could ever be spoken to.</strong>
    /// Measured before this: the player walked straight through an event with
    /// <c>priorityType 1</c>, because the step was checked against the map's
    /// tile flags and nothing else. The engine checks the characters too:
    /// </para>
    /// <code>
    /// Game_CharacterBase.prototype.canPass = function(x, y, d) {
    ///     ...
    ///     if (this.isCollidedWithCharacters(x2, y2)) { return false; }
    ///     return true;
    /// };
    /// </code>
    /// <para>
    /// <strong>And standing on the tile is exactly what stops the action
    /// button from reaching the event</strong>: <c>checkEventTriggerThere</c>
    /// starts normal-priority events from the tile in front, while
    /// <c>checkEventTriggerHere</c> only starts the ones that are
    /// <em>not</em> normal -- so a player standing on the event asked the
    /// wrong question and got nothing.
    /// </para>
    /// <para>
    /// <strong>And the event used here has no picture</strong>, which is the
    /// second half of the same defect: Map002's event 6 stands on (4, 14)
    /// with <c>priorityType 1</c>, no conditions and an empty
    /// <c>characterName</c> -- and <c>$gameMap.events()</c> keeps it.
    /// </para>
    /// </remarks>
    public void Test_EinEreignisStehtImWegUndLaesstSichAnsprechen()
    {
        if (!Vorhanden())
        {
            return;
        }

        using var host = new EnginePluginHost(
            BuiltInEnginePluginCatalog.CreateRuntimeRegistry());
        var started = host.Start(new PluginGameInfo
        {
            GameDirectory = Projekt,
            EngineId = EnginePluginIds.RpgMakerMz,
            Generation = "mz",
            DetectorScore = 850,
        });
        AssertTrue(started.Success, $"the MZ game starts: {started.Error?.Message}");
        if (!started.Success)
        {
            return;
        }

        var runtime = (MzEngineRuntime)host.Runtime!;

        // Map002's event 6 stands on (4, 14). The player is put on the tile
        // above it and walks down.
        AssertTrue(runtime.Transfer(2, 4, 13),
            $"the transfer to Map002 succeeds: {runtime.AutorunProblem}");
        runtime.Update(1.0 / 60.0);
        Console.WriteLine($"MZ collision: player at {runtime.PlayerX}/{runtime.PlayerY}");
        AssertEq(runtime.PlayerX, 4, "the player stands beside the event");
        AssertEq(runtime.PlayerY, 13, "on the tile above it");

        AssertFalse(runtime.SubmitInput(Rm2kInputAction.MoveDown),
            "and the step onto the event's tile is refused");
        runtime.Update(1.0 / 60.0);
        Console.WriteLine($"MZ collision: after pressing down the player is "
            + $"{runtime.PlayerX}/{runtime.PlayerY} facing {runtime.PlayerDirection}");
        AssertEq(runtime.PlayerY, 13, "so the player does not walk through it");
        AssertEq(runtime.PlayerDirection, 2, "and turns to face it");

        // **And from there the action button reaches it**, which is the whole
        // reason the step had to be refused.
        runtime.SubmitInput(Rm2kInputAction.Confirm);
        for (var frame = 0; frame < 600 && !runtime.MessageVisible; frame++)
        {
            runtime.Update(1.0 / 60.0);
        }
        Console.WriteLine($"MZ collision: talking to it gives "
            + $"\"{runtime.MessageText}\"");
        Console.WriteLine($"MZ collision: report=[{string.Join(" | ", runtime.ActionButtonReport)}] "
            + $"visible={runtime.MessageVisible} "
            + $"busy={runtime.Facts.MessageBusy} "
            + $"blockBusy={runtime.Facts.LastDialogue?.IsBusy}");

        // **And the press reached the page in front of the player**, which is
        // what could not happen before: the action button asked only the tile
        // the player stands on.
        AssertTrue(runtime.ActionButtonReport.Count > 0,
            "the action button starts the page in front of the player");
        AssertTrue(runtime.MessageText.Trim().Length > 0,
            "and that page says something, so the press reached the game's own"
                + " dialogue and not just a counter");

        // **And the page waits there instead of running past it.** Measured
        // before this: `event 6: ran to its end, 1 commands` with `busy=False`
        // at `blockBusy=True` -- the text was produced and never shown,
        // because the runner that started the page dismissed every message it
        // met and pressed ok in the same frame the wait was created. The
        // engine waits: `Window_Message.isTriggered` asks
        // `Input.isRepeated("ok")`, and `Game_Map.update` drives the
        // interpreter once per frame.
        var bericht = string.Join(" | ", runtime.ActionButtonReport);
        AssertTrue(bericht.Contains("waiting"),
            $"the page waits at its dialogue instead of running past it: {bericht}");
        AssertTrue(runtime.MessageVisible,
            "so the player is shown the dialogue the press started");
        AssertTrue(runtime.MessageHoldsPlayer,
            "and is held while it is up");
    }

    /// <summary>
    /// And the game's own text and options reach the window that shows them.
    /// </summary>
    /// <remarks>
    /// <strong>And this is the wiring, not the drawing.</strong> A view fed
    /// nothing looks exactly like a view that draws nothing -- which is what
    /// the MZ text was: correct in the runtime, absent from the screen.
    /// </remarks>
    public void Test_TextUndWahlKommenInDieAnsicht()
    {
        var ansicht = new Rm2kGameScreen();
        try
        {
            ansicht.SetPresentation(
                true, "~14 hours or so later", null, -1, false, 0);
            var (sichtbar, text, wahl, gewaehlt) = ansicht.Presentation();
            Console.WriteLine($"MZ view: visible={sichtbar} text=\"{text}\"");
            AssertTrue(sichtbar, "the window shows the message");
            AssertEq(text, "~14 hours or so later",
                "and it is the game's own text");
            AssertEq(wahl.Count, 0, "and no choice is up");

            ansicht.SetPresentation(
                false, "", new[] { "Yes", "No" }, 1, false, 0);
            var (sichtbar2, _, wahl2, gewaehlt2) = ansicht.Presentation();
            Console.WriteLine($"MZ view: visible={sichtbar2} "
                + $"choice=[{string.Join(" / ", wahl2)}] cursor={gewaehlt2}");
            AssertFalse(sichtbar2, "the message goes when the choice comes");
            AssertEq(wahl2.Count, 2, "and the choice is shown");
            AssertEq(wahl2[0], "Yes", "with the game's own options");
            AssertEq(gewaehlt2, 1, "and the cursor where the runtime put it");
        }
        finally
        {
            ansicht.Free();
        }
    }
}
