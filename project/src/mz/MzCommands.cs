using System.Collections.Generic;

namespace UniversalRPG.Web;

/// <summary>
/// Something a command asked the game to do, as a record rather than as an
/// effect.
/// </summary>
/// <remarks>
/// The interpreter does not change the game. It walks a list of commands,
/// decides the branches it meets, and writes down what each command asked for, so
/// a caller can act on it and can see what was skipped. That is a deliberate
/// boundary: nothing here opens a file, moves a character or shows a window.
/// </remarks>
public readonly record struct MzAction(MzCommandEntry Command, string What)
{
    /// <summary>The command's number, so a caller can group them.</summary>
    public int Code => Command.Code;

    /// <summary>How deep the command sat, which is what says whether it ran.</summary>
    public int Indent => Command.Indent;

    public static MzAction Branch(MzCommandEntry pCommand, MzBranchResult pResult) =>
        new(
            pCommand,
            $"branch {pResult.Outcome}"
            + (pResult.Comparison == "" ? "" : $" by {pResult.Comparison}")
            + (pResult.Missing == "" ? "" : $": needs {pResult.Missing}"));

    public static MzAction Switches(
        MzCommandEntry pCommand, int pFrom, int pTo, bool pOn) =>
        new(pCommand, $"switch {pFrom} to {pTo} set {(pOn ? "on" : "off")}");

    public static MzAction Variable(
        MzCommandEntry pCommand, int pId, int pWas, int pIs, string pHow) =>
        new(pCommand, $"variable {pId} {pHow} {pWas} to {pIs}");

    /// <summary>
    /// A wait, with the frames it asked for. The engine counts these down one
    /// per frame, and a run with no frames is waiting rather than finished.
    /// </summary>
    public static MzAction Wait(MzCommandEntry pCommand, int pFrames) =>
        new(pCommand, pFrames > 0
            ? $"wait {pFrames} frames"
            : "wait for something outside this reader");

    /// <summary>A common event being called, by the index the game named.</summary>
    public static MzAction CommonEvent(MzCommandEntry pCommand, int pIndex) =>
        new(pCommand, $"call common event {pIndex}");
}

/// <summary>
/// The commands that change the game's numbers, as recorded changes.
/// </summary>
/// <remarks>
/// <para>
/// Written from <c>command121</c> and <c>command122</c>. Two details in the
/// engine are not what a first reading gives, and both are kept here.
/// </para>
/// <para>
/// <b>A range is a range.</b> Both commands run from the first id to the last
/// one inclusive, so a command that names a range switches every switch in it.
/// </para>
/// <para>
/// <b>Dividing by zero writes zero.</b> The engine wraps each operation in a
/// try and, on any failure, sets the variable to zero. A reader that let the
/// failure out would stop a game the engine plays on.
/// </para>
/// <para>
/// The fourth operand of a variable command is the author's own script, and the
/// engine evaluates it. This repository does not, so such a command is refused
/// and says so, and the other five operands are read.
/// </para>
/// </remarks>
public static class MzCommands
{
    /// <summary>
    /// Is this a command this reader acts on? A command it acts on neither is
    /// one the engine has no method for — which the engine steps over, and so
    /// does this — nor one whose effect belongs to a part of the runtime that
    /// does not exist yet. Both are stepped over rather than refused, because
    /// refusing would strand a game on a command the engine itself ran past.
    /// </summary>
    /// <remarks>
    /// The name says "effect" and not "method" on purpose. The engine's own
    /// question is <c>typeof this["command" + code] === "function"</c>, and
    /// <b>every command this game stores that has no method is one it stores on
    /// purpose</b>: 0 the end of a block, 401 a line of text under a 101, 412
    /// the end of a branch, and 655 and 657 the two halves of a script. Answering
    /// the engine's question exactly would mean calling all five of those
    /// "stepped over because there is no method", which is true of none of them
    /// and would hide the reason a command did nothing.
    /// </remarks>
    public static bool HasEffect(int pCode) =>
        pCode is MzCommandTable.ShowText
            or MzCommandTable.Else
            or MzCommandTable.Loop
            or MzCommandTable.BreakLoop
            or MzCommandTable.RepeatAbove
            or MzCommandTable.ExitEventProcessing
            or MzCommandTable.Label
            or MzCommandTable.JumpToLabel
            or MzCommandTable.ControlSwitches
            or MzCommandTable.ControlVariables
            or MzCommandTable.ChangeItems
            or MzCommandTable.ShowDialogue
            or MzCommandTable.ShowTextLine
            or MzCommandTable.MoveRoute
            or MzCommandTable.TransferPlayer
            or MzCommandTable.OpenMenu
            or MzCommandTable.PluginCommand
            or MzCommandTable.ShowPicture
            or MzCommandTable.MovePicture
            or MzCommandTable.ErasePicture
            or MzCommandTable.PlayBgm
            or MzCommandTable.FadeOutBgm
            or MzCommandTable.PlayBgs
            or MzCommandTable.FadeOutBgs
            or MzCommandTable.PlayMe
            or MzCommandTable.PlaySe
            or MzCommandTable.StopSe
            or MzCommandTable.ControlSelfSwitch
            or MzCommandTable.ChangePartyMember
            or MzCommandTable.ShowBalloonIcon
            or MzCommandTable.ShowAnimation
            or MzCommandTable.EraseEvent
            or MzCommandTable.Wait;

    /// <summary>
    /// Runs a command that changes the game's numbers. It returns false when the
    /// interpreter has stopped, which only the operand that is a script does.
    /// </summary>
    public static bool TryExecute(
        MzInterpreter pInterpreter, MzCommandEntry pCommand,
        List<MzAction> pActions, MzBranchFacts pFacts, MzRandom pRandom)
    {
        switch (pCommand.Code)
        {
            case MzCommandTable.ShowPicture:
            {
                // `command231` is
                //   const point = this.picturePoint(params);
                //   $gameScreen.showPicture(params[0], params[1], params[2],
                //       point.x, point.y, params[6], params[7], params[8], params[9]);
                // and `picturePoint` is
                //   if (params[3] === 0) { point.x = params[4]; point.y = params[5]; }
                //   else { point.x = $gameVariables.value(params[4]);
                //          point.y = $gameVariables.value(params[5]); }
                // **so the fourth parameter decides where the other two are
                // read from** — a number written in the event, or a variable
                // to look up. A first draft read them as numbers and a game
                // that places a picture from a variable would have put it at
                // the variable's own number.
                var show = Point(pCommand, pFacts, out var shownX, out var shownY);
                var said = pFacts.Screen.Show(
                    At(pCommand, 0),
                    Text(pCommand, 1),
                    At(pCommand, 2),
                    shownX, shownY,
                    At(pCommand, 6),
                    At(pCommand, 7),
                    At(pCommand, 8),
                    At(pCommand, 9));
                pActions.Add(new MzAction(pCommand, said));
                _ = show;
                return true;
            }

            case MzCommandTable.MovePicture:
            {
                // `command232` is
                //   $gameScreen.movePicture(params[0], params[2], point.x, point.y,
                //       params[6], params[7], params[8], params[9], params[10],
                //       params[12] || 0);
                //   if (params[11]) { this.wait(params[10]); }
                // **and there is no second one** — the reader that assumed
                // there was one would wait on every move, and this game asks
                // for the wait on two of its four and not on the other two.
                Point(pCommand, pFacts, out var movedX, out var movedY);
                var moving = pFacts.Screen.Move(
                    At(pCommand, 0),
                    At(pCommand, 2),
                    movedX, movedY,
                    At(pCommand, 6),
                    At(pCommand, 7),
                    At(pCommand, 8),
                    At(pCommand, 9),
                    At(pCommand, 10),
                    // `params[12] || 0` — an easing the game did not write is
                    // zero, and not "whatever is in the next slot".
                    At(pCommand, 12),
                    Truth(pCommand, 11),
                    out var frames);

                // **The command runs and the index moves, and the wait is the
                // interpreter's, not this command's.** A first draft returned
                // false here to hold the list up, and that is what
                // `return false` means to this reader: the index stays where
                // it was. So the next frame read the same 232 again, set the
                // same twenty frames again, and the picture never arrived —
                // a move that had to wait became a move that waited for ever.
                //
                // The engine has none of this trouble because `command232`
                // ends in `return true` whatever it asked for, and the wait it
                // set lives in `this._waitCount` where the next command cannot
                // reach it. **That is the shape to keep**: the command is done,
                // and the frame belongs to the interpreter.
                pActions.Add(new MzAction(pCommand, moving));
                if (frames > 0)
                {
                    pActions.Add(MzAction.Wait(pCommand, frames));
                    pInterpreter.Wait(frames);
                }
                return true;
            }

            case MzCommandTable.ErasePicture:
            {
                // `command235` is `$gameScreen.erasePicture(params[0])`, which
                // sets the slot to null. One parameter, and a picture that is
                // not there is not an error.
                pActions.Add(new MzAction(
                    pCommand, pFacts.Screen.Erase(At(pCommand, 0))));
                return true;
            }

            case MzCommandTable.ShowDialogue:
            {
                // `command101` is
                //   if ($gameMessage.isBusy()) { return false; }
                //   $gameMessage.setFaceImage(params[0], params[1]);
                //   $gameMessage.setBackground(params[2]);
                //   $gameMessage.setPositionType(params[3]);
                //   $gameMessage.setSpeakerName(params[4]);
                //   while (this.nextEventCode() === 401) {
                //       this._index++;
                //       $gameMessage.add(this.currentCommand().parameters[0]);
                //   }
                //   switch (this.nextEventCode()) {
                //       case 102: this._index++; this.setupChoices(…); break;
                //       case 103: this._index++; this.setupNumInput(…); break;
                //       case 104: this._index++; this.setupItemChoice(…); break;
                //   }
                //   this.setWaitMode("message");
                //   return true;
                //
                // **This is the first command in this reader that eats other
                // commands.** The lines of a dialogue are never dispatched:
                // `nextEventCode()` looks one ahead and this command steps
                // the index over each line itself. A reader that ran a 401 as
                // a command of its own would be running something the engine
                // never runs.
                //
                // **And a dialogue that is already up is refused** —
                // `isBusy()` is text *or* a choice *or* a number *or* an item
                // to choose, so a 101 behind an unanswered choice is refused
                // as firmly as one behind a line.
                if (pFacts.MessageBusy)
                {
                    pInterpreter.Refuse(
                        "a dialogue is not shown, because the engine puts"
                        + " nobody's words on the screen while another"
                        + " message is up, and the run stops at index"
                        + $" {pInterpreter.Index} to try again later");
                    return false;
                }

                var (block, consumed) =
                    MzDialogue.Read(pInterpreter.Commands, pInterpreter.Index, pFacts);
                pFacts.LastDialogue = block;
                pFacts.MessageBusy = true;
                pActions.Add(new MzAction(pCommand, block.ToString()));

                // **The index moves by what was eaten, not by one** — and
                // `consumed` counts the 101 itself.
                //
                // The lines are behind the index now, and a reader that moved
                // it by one would run the first line as a command of its own
                // — which is the thing this whole method exists to avoid.
                // **A first draft wrote `consumed - 1`**, on the reading that
                // the index was already on the last thing taken, and it is
                // not: it is on the 101. So the index landed on the first
                // line, the next frame read that line as a command of its own,
                // and every dialogue in this game ended in a refusal.
                // **`this._index++` happens after every command that returns
                // true**, in the interpreter and not in the command. And
                // `command101` has already moved the index itself: once per
                // line in its own `while` loop, and once more for the 102
                // its `switch` took.
                //
                // **So `Index += consumed` is the whole move**, and a 101 at
                // the end of a list leaves the index **one past the end** —
                // which is what the engine's own arithmetic says and what
                // this reader reproduces. **A first draft "fixed" this to
                // `consumed - 1`** on the strength of a test that had
                // forgotten the interpreter's own step, and the fix broke
                // four index assertions in this file and one in K-124's.
                var after = pInterpreter.Index + consumed;

                // **The one the 101 took is the *last* thing it ate**, which
                // is `Index + consumed - 1` before the jump and not the
                // index after it.
                //
                // **A first draft read `Commands[Index]` after
                // `Index += consumed`** — one further on, which in a real
                // game is a 122 and here a variable assignment. **Eight
                // choices in this game, and every one of them was read from
                // the wrong command.** A first draft that then read
                // `Index + 1` was one further on again.
                var genommen = pInterpreter.Index + consumed - 1;
                pInterpreter.Index = after;

                // **One of 102, 103 and 104, and the switch ran once.** A
                // 102 that does not sit directly after the last line is not
                // taken here and is reached later as a command of its own.
                //
                // **The one it took is *at* the index**, because
                // `consumed` counted it and the index moved past everything
                // the command ate. **A first draft read it at
                // `Index + 1`** and so read the command *after* the choice —
                // a 122, usually — and built a set of options out of a
                // variable assignment. **Eight choices in this game, and every
                // one of them was wrong.**
                //
                // **And the index is only read when it is inside the list.**
                // A dialogue at the end of a list takes a 102 that is the last
                // command, the index lands on it, and a reader that walked
                // past the end to read the next one would throw on a list
                // that is perfectly good. `nextEventCode` returns 0 at the
                // end of a list, and the engine's switch simply does not
                // match — so neither does this.
                if (block.Follower == MzCommandTable.ShowChoiceList
                    && genommen < pInterpreter.Commands.Count)
                {
                    pFacts.LastChoice = MzChoice.Read(
                        pInterpreter.Commands[genommen].Parameters);
                }
                else if (block.Follower is 103 or 104
                    && genommen < pInterpreter.Commands.Count)
                {
                    // **A 103 asks for a number and a 104 for an item, and
                    // neither is a list of options** — so neither is read
                    // through `MzChoice`, which would count an item id as an
                    // option and a digit count as a choice.
                    //
                    // **`setupNumInput(params)` is `setNumberInput(params[0],
                    // params[1])`** and **`setupItemChoice(params)` is
                    // `setItemChoice(params[0], params[1] || 2)`** — so a
                    // missing second parameter is **2, the whole party**, and
                    // not zero. **This game has neither a 103 nor a 104**, so
                    // a reader that got that default wrong would never hear
                    // about it from the data — and the one that wrote them
                    // through `MzChoice` would have read an item id as a list
                    // of options.
                    var befehl = pInterpreter.Commands[genommen];
                    pFacts.LastPrompt = MzPrompt.Read(
                        befehl.Code, befehl.Parameters);
                }

                // **`setWaitMode("message")` is outside the switch**, so a
                // dialogue with no choice holds its page all the same.
                pInterpreter.WaitFor(MzWaitMode.Message);
                return true;
            }

            case MzCommandTable.ShowTextLine:
            {
                // **A 401 that arrives on its own is not a line of a
                // dialogue — it is a line whose 101 is missing, and the
                // engine never runs one of these.**
                //
                // K-132 read them one by one here, which was true of the
                // data and false of the engine: `command101` eats its own
                // lines with `while (this.nextEventCode() === 401) {
                // this._index++; … }`, so a line that reaches the dispatcher
                // is a line **without a dialogue over it**. A first draft of
                // this case read it as a line in its own right and reported
                // a game whose dialogue begins with a line rather than a
                // 101.
                //
                // **So it is refused, and named.** A game that ships one has
                // an event list the editor would not write, and a reader that
                // quietly showed the line would be papering over it.
                pInterpreter.Refuse(
                    "a line of text has no dialogue over it, because the"
                    + " engine's command101 reads its own lines and there is"
                    + " no command401 to run one on its own, and the words"
                    + $" are kept unread at index {pInterpreter.Index}");
                return false;
            }

            case MzCommandTable.MoveRoute:
            {
                // `command205` is
                //   $gameMap.refreshIfNeeded();
                //   this._characterId = params[0];
                //   const character = this.character(params[0]);
                //   if (character) {
                //       character.forceMoveRoute(params[1]);
                //       if (params[1].wait) { this.setWaitMode("route"); }
                //   }
                //   return true;
                //
                // **Two things, and the first is that it always returns
                // true.** A route for a character this reader has not got is
                // a route that goes nowhere — `if (character)` guards the
                // work, not the command. A reader that returned false there
                // would stop the page on a character it simply does not have,
                // and a game that moves event 9 in a map this reader has
                // read half of would stall for ever.
                var id = At(pCommand, 0);
                var character = pFacts.Characters.TryGetValue(id, out var c)
                    ? c
                    : null;
                if (character == null)
                {
                    pActions.Add(new MzAction(
                        pCommand,
                        $"character {id} is not one this reader has, so the"
                        + " route is not carried out, and the page carries on"
                        + " as the engine's own `if (character)` does"));
                    return true;
                }

                // **The route is nested, not a flat list of numbers** —
                // `{list: [{code, parameters, indent}], repeat, skippable,
                // wait}`. A first reading expected `[1, 0, 3, 0, 0]` and got
                // nothing, and the reason is in the test.
                var route = MzRouteStep.ReadFromParameter(Text(pCommand, 1));
                pActions.Add(new MzAction(
                    pCommand,
                    character.Route.Force(route)));

                // **`if (params[1].wait)` and nothing else.** `repeat` and
                // `skippable` are read by the character's index and by the
                // input side, and a reader that invented a repeat would loop a
                // route the engine loops only while the character is moving.
                if (route.Wait)
                {
                    pInterpreter.WaitFor(MzWaitMode.Route);
                }
                return true;
            }

            case MzCommandTable.TransferPlayer:
            {
                // `command201` is
                //   if ($gameParty.inBattle() || $gameMessage.isBusy()) {
                //       return false;
                //   }
                //   let mapId, x, y;
                //   if (params[0] === 0) {
                //       mapId = params[1]; x = params[2]; y = params[3];
                //   } else {
                //       mapId = $gameVariables.value(params[1]);
                //       x = $gameVariables.value(params[2]);
                //       y = $gameVariables.value(params[3]);
                //   }
                //   $gamePlayer.reserveTransfer(mapId, x, y, params[4], params[5]);
                //   this.setWaitMode("transfer");
                //   return true;
                //
                // **Three things, and the first is a refusal.** In a battle or
                // with a message on screen the engine returns false and
                // transfers nothing, so a reader that moved the player there
                // would take a player out of a fight.
                //
                // **The second is that a transfer is not a move.** `reserve`
                // records where to go and changes nothing; the map changes in
                // `performTransfer`, which the scene calls. Applying it here
                // would move the player before the commands after it ran.
                //
                // **The third is the wait, and it is a new kind.** `command201`
                // returns **true** and does not hold the index — it sets
                // `setWaitMode("transfer")`, and `updateWaitMode` answers
                // `waiting = $gamePlayer.isTransferring()`. So the page is held
                // by a **condition** and not by a frame count, which is a third
                // shape next to a 230's frames and a 232's movement.
                if (pFacts.InBattle || pFacts.MessageBusy)
                {
                    pActions.Add(new MzAction(
                        pCommand,
                        "the player is not transferred, because the engine"
                        + " transfers nobody in a battle or while a message is"
                        + " on the screen"));
                    pInterpreter.Refuse(
                        "a transfer in a battle or during a message is refused"
                        + $" by the engine, and the run stops at index"
                        + $" {pInterpreter.Index} where it stands");
                    return false;
                }

                // **The place, from the numbers or from the variables** — the
                // same kind decision a 231 makes, and the same reason: a first
                // parameter decides where the other three are read from.
                var fromVariables = At(pCommand, 0) != 0;
                var toMap = At(pCommand, 1);
                var toX = At(pCommand, 2);
                var toY = At(pCommand, 3);
                if (fromVariables)
                {
                    toMap = From(pCommand, pFacts, 1, true);
                    toX = From(pCommand, pFacts, 2, true);
                    toY = From(pCommand, pFacts, 3, true);
                }

                pActions.Add(new MzAction(
                    pCommand,
                    pFacts.Player.Reserve(
                        toMap, toX, toY,
                        At(pCommand, 4), At(pCommand, 5))));
                pInterpreter.WaitFor(MzWaitMode.Transfer);
                return true;
            }

            case MzCommandTable.OpenMenu:
            {
                // `command351` is
                //   if (!$gameParty.inBattle()) {
                //       SceneManager.push(Scene_Menu);
                //       Window_MenuCommand.initCommandPosition();
                //   }
                //   return true;
                // — **and the battle check is the whole of it.** A menu in a
                // battle is not this command with a different scene, it is
                // nothing at all, and a reader that opened one would put a
                // menu over a fight the engine keeps the menu out of.
                //
                // Nothing here changes a number, so there is nothing to apply
                // and nothing to be wrong about numerically: a caller is told
                // the menu is open and why it is or is not.
                if (pFacts.InBattle)
                {
                    pActions.Add(new MzAction(
                        pCommand,
                        "the menu is not opened, because the party is in a"
                        + " battle and the engine opens no menu there"));
                    return true;
                }
                pFacts.Menu = MzMenuState.Open;
                pActions.Add(new MzAction(pCommand, "the menu is open"));
                return true;
            }

            case MzCommandTable.PluginCommand:
            {
                // `command357` is
                //   const pluginName = Utils.extractFileName(params[0]);
                //   PluginManager.callCommand(this, pluginName, params[1],
                //                                 params[3]);
                //   return true;
                // — **and this is where a reader stops.** Not because a
                // plugin call is harder than the rest of the engine, and not
                // because this particular plugin is unusual, but because
                // running it would mean running somebody else's JavaScript,
                // which is the one thing this repository does not do.
                //
                // What can be read without running a line of it is kept: the
                // plugin's file name, the command inside it, the author's own
                // description in the third parameter, and the parameters as
                // data. What the plugin *does* is not answerable and is not
                // guessed — a crafted-recipe system and a picture of text
                // need the plugin to mean anything at all.
                //
                // **And it is not stepped over.** A silent step over would
                // make a game look as if it worked while eleven of its
                // commands did nothing, and this map's eleven `357` commands
                // are how this game's crafting and its floating text are
                // reached. A caller is told, by name, that they were not run.
                pFacts.Notices.Add(
                    $"a plugin command of \"{Text(pCommand, 0)}\" asks"
                    + $" \"{Text(pCommand, 1)}\" to run, which this reader"
                    + " does not do");
                pActions.Add(new MzAction(
                    pCommand,
                    $"plugin {Text(pCommand, 0)} was asked to"
                    + $" \"{Text(pCommand, 1)}\" and was not run; its"
                    + " JavaScript is not executed here"));
                return true;
            }

            case MzCommandTable.ChangeItems:
            {
                // `command126` is
                //   const value = this.operateValue(params[1], params[2], params[3]);
                //   $gameParty.gainItem($dataItems[params[0]], value);
                // and **all four parameters are read**: the item, the
                // operation, the operand's kind and the operand itself. A first
                // draft read the item and the number and left the other two
                // out, which made every 126 in this game a gain of a literal —
                // and the game writes nine of them as a gain of 999, which the
                // engine refuses to hold.
                // The party is built over **the facts this interpreter was
                // given**, so it knows the ids only when the caller has read
                // this game's `Items.json`. **A reader with no data behind it
                // must not answer for an item it cannot name**, and building
                // the party without the ids made every 126 a silent no-op:
                // the first draft of the wiring test caught exactly that, with
                // every count coming back zero and nothing saying why.
                var party = new MzParty(pFacts, pFacts.KnownItems);
                var said = party.GainItem(
                    At(pCommand, 0),
                    At(pCommand, 1),
                    At(pCommand, 2),
                    At(pCommand, 3),
                    pFacts.Variables.TryGetValue(At(pCommand, 3), out var held)
                        ? held
                        : 0);
                pActions.Add(new MzAction(pCommand, said));
                return true;
            }

            case MzCommandTable.Wait:
            {
                // The engine's wait is `this._waitCount = params[0]`, and
                // `updateWaitCount` takes one off it per frame and breaks the
                // frame while it is above zero. **A command that is waiting does
                // not advance the index**, so the same 230 is read again next
                // frame, and a reader that stepped over it would run the rest of
                // the list a whole list of frames too early.
                //
                // There are no frames here, so the run is handed back waiting and
                // the caller decides when the next frame is. The count is kept so
                // that it can be counted down.
                var frames = At(pCommand, 0);
                pActions.Add(MzAction.Wait(pCommand, frames));
                pInterpreter.Wait(frames);
                // `false` here does not mean "carry on" and `true` would not
                // mean it either. `ExecuteOne` finishes with
                // `return Stopped == MzStep.Stepped`, and `Wait` has just set
                // `Stopped` to `Waiting`, so the run stops either way. **This
                // return value is a dead branch** — a first mutation run proved
                // it, by changing this to `true` and watching every test still
                // pass. It is left as `false` because it says what the command
                // meant, and the next branch in this switch is a real one.
                return false;
            }

            case MzCommandTable.PlayBgm:
            case MzCommandTable.PlayBgs:
            case MzCommandTable.PlayMe:
            case MzCommandTable.PlaySe:
            {
                // `command241` is
                //   $gameSystem.playBgm(params[0].name, params[0].volume,
                //                          params[0].pitch, params[0].pan)
                // **und der Parameter ist ein Objekt, und nicht vier
                // Zahlen.** Gemessen an einem fertigen Projekt:
                // `241 [{"name":"Scene8","volume":40,"pitch":80,"pan":0}]`
                // **und XP schreibt denselben Befehl als 11510 mit vier
                // Bitfeldern.**
                //
                // **Und `MzJson.Write` hat das Objekt als Text
                // zurueckgegeben** (Zeile 58 in `MzCommandEntry.From`),
                // **und `MzJson.TryParse` ist derselbe Parser, der es
                // geschrieben hat** -- **ein Leser, der die
                // Textform nicht wieder einliest, haette einen Kanal
                // namens `{"name"` mit Lautstaerke 0.**
                var kanal = pCommand.Code switch
                {
                    MzCommandTable.PlayBgm => pFacts.Screen.Bgm,
                    MzCommandTable.PlayBgs => pFacts.Screen.Bgs,
                    MzCommandTable.PlayMe => pFacts.Screen.Me,
                    _ => pFacts.Screen.Se,
                };
                var (name, volume, pitch, pan) = AudioOf(pCommand);
                var said = pFacts.Screen.Play(kanal, name, volume, pitch, pan);
                pActions.Add(new MzAction(pCommand, said));
                return true;
            }

            case MzCommandTable.FadeOutBgm:
            case MzCommandTable.FadeOutBgs:
            case MzCommandTable.StopSe:
            {
                // `command242` is `$gameSystem.fadeOutBgm(params[0])`, und
                // `command251` ist `$gameSystem.stopSe()`.
                //
                // **Und `stopSe` hat keinen Parameter** -- **und dieser
                // Zweig liest trotzdem `parameters[0]`, und bei 251 ist
                // die Liste leer, und `At` gibt dann 0 zurueck**, **und
                // 0 Bilder ist "jetzt".**
                var aus = pCommand.Code == MzCommandTable.StopSe
                    ? pFacts.Screen.Se
                    : pCommand.Code == MzCommandTable.FadeOutBgm
                        ? pFacts.Screen.Bgm
                        : pFacts.Screen.Bgs;
                var frames = pCommand.Code == MzCommandTable.StopSe
                    ? 0
                    : At(pCommand, 0);
                var gesagt = pFacts.Screen.FadeOut(aus, frames);
                pActions.Add(new MzAction(pCommand, gesagt));
                return true;
            }


            case MzCommandTable.ControlSelfSwitch:
            {
                // Die offizielle Hilfe zu `123 Control Self Switch`
                // sagt: *Self Switch — Specify the target self switch
                // (A through D). Operation — Specify the value
                // (ON/OFF) to store in the switch.*
                //
                // **Und gemessen an einem fertigen Projekt: `123 ["A",
                // 0]`** — **ein Buchstabe als erster Parameter, und keine
                // Zahl.**
                //
                // **Und die zweite Zahl ist 0 fuer an und 1 fuer
                // aus**, **und nicht umgekehrt** -- **denn 121 Control
                // Switches benutzt 0 fuer an, und ein Leser, der den
                // Wert fuer beide gleichnahm, schaltete jeden
                // Selbstschalter genau um.**
                // **Und der erste Parameter ist ein Buchstabe, und keine
                // Zahl** -- **also kann `At` hier nichts lesen**, **und
                // `At("A")` gibt 0, und 0 ist Schalter A**, **und das
                // geht fuer A, B, C und D gleichermassen falsch.**
                //
                // **Also wird der Buchstabe gelesen, und nicht die Zahl
                // an seiner Stelle.**
                var roh = pCommand.Parameters.Count > 0
                    ? pCommand.Parameters[0]
                    : "";
                var schalter = SelfSwitchIndex(roh);
                if (schalter < 0)
                {
                    pFacts.Notices.Add(
                        $"self switch '{roh}' is not one of A to D, and "
                        + "there are four and no fifth");
                    return true;
                }

                var an = At(pCommand, 1) == 0;
                pFacts.SelfSwitches[SelfSwitchName(schalter)] = an;
                pActions.Add(new MzAction(pCommand,
                    $"self switch {SelfSwitchName(schalter)} "
                    + (an ? "on" : "off")));
                return true;
            }

            case MzCommandTable.ChangePartyMember:
            {
                // Die Hilfe zu `129 Change Party Members` sagt: *Actors —
                // Select the actor to change. Operation — Select which
                // operation to perform (Add/Remove). Initialize — When
                // enabled, the traits when adding an actor will be reset
                // according to the parameters in the Database.*
                //
                // **Und gemessen: `129 [2, 0, false]`** -- **also
                // Darsteller, dann 0 fuer Hinzufuegen und 1 fuer
                // Entfernen, und dann das Flag.**
                var darsteller = At(pCommand, 0);
                var entfernen = At(pCommand, 1) == 1;
                if (entfernen)
                {
                    pFacts.PartyMembers.Remove(darsteller);
                }
                else
                {
                    pFacts.PartyMembers.Add(darsteller);
                }

                pActions.Add(new MzAction(pCommand,
                    $"actor {darsteller} "
                    + (entfernen ? "removed from" : "added to")
                    + " the party, and the party holds "
                    + pFacts.PartyMembers.Count));
                return true;
            }

            case MzCommandTable.ShowAnimation:
            {
                // Die Hilfe zu `221 Show Animation` sagt dieselben drei
                // Saetze wie zu `213 Show Balloon Icon`: *Character — The
                // display location will be based on the position of the
                // player or event. Animations — Specify the animation to
                // display. Wait for Completion — When enabled, the event
                // will be paused until the animation being displayed has
                // finished.*
                //
                // **Und minus eins ist hier auch der Spieler**,
                // **denn es ist derselbe erste Parameter und dieselbe
                // Hilfe.**
                var ziel = At(pCommand, 0);
                var warten = Flag(pCommand, 2);
                if (ziel < 0)
                {
                    pFacts.Player.ShowAnimation(
                        At(pCommand, 1), MzScreen.MaxAnimationFrames);
                }
                else if (pFacts.Characters.TryGetValue(ziel, out var figur)
                    && figur != null)
                {
                    figur.ShowAnimation(
                        At(pCommand, 1), MzScreen.MaxAnimationFrames);
                }
                else
                {
                    pFacts.Notices.Add(
                        $"animation asked for character {ziel}, and this "
                        + "map has no such character");
                    return true;
                }

                // **Und wie beim Ballon wartet der Befehl ueber
                // `Wait`, und der Rueckgabewert sagt, ob die Liste
                // weitergeht** -- **und die beiden sind nicht
                // dasselbe**, **und ein `return false` ohne zu warten
                // las den Befehl im naechsten Bild noch einmal und
                // setzte die Uhr bei jedem Durchgang neu.**
                if (warten)
                {
                    pInterpreter.Wait(MzScreen.MaxAnimationFrames);
                }

                pActions.Add(new MzAction(pCommand,
                    $"animation {At(pCommand, 1)} over "
                    + (ziel < 0 ? "the player" : $"character {ziel}")
                    + (warten ? ", waiting for it to finish" : "")));
                return !warten;
            }

            case MzCommandTable.EraseEvent:
            {
                // Die Hilfe zu `222 Erase Event` sagt woertlich:
                // *Temporarily removes the event currently being run.
                // There are no parameters to set. The event will remain
                // erased until the party moves to another map.*
                //
                // **Und "es gibt keine Parameter" ist eine Aussage ueber
                // die Datei, und nicht ueber den Code** -- **der Befehl
                // kann traeger sein und ist dann kein Zaehler, und ein
                // Test, der ihm drei Parameter gibt, hat einen anderen
                // Befehl gebaut.**
                var ereignis = pInterpreter.EventId;
                if (pFacts.Characters.TryGetValue(ereignis, out var laeuft)
                    && laeuft != null)
                {
                    laeuft.Erase();
                }

                pActions.Add(new MzAction(pCommand,
                    $"event {ereignis} erased until the party moves to "
                    + "another map"));
                return true;
            }


            case MzCommandTable.ShowBalloonIcon:
            {
                // Die Hilfe zu `213 Show Balloon Icon` sagt: *Character —
                // The display location will be based on the position of
                // the player or event. Balloon Icon — Specify the
                // balloon icon to display. Wait for Completion — When
                // enabled, the event will be paused until the balloon
                // icon being displayed has disappeared.*
                //
                // **Und gemessen: `213 [-1, 2, false]`** -- **und
                // minus eins ist der Spieler**, **und nicht eine
                // Darsteller-Id**, **und ein Leser, der die erste Zahl
                // als Darsteller las, suchte Darsteller minus eins und
                // fand niemanden.**
                // **Und der Spieler ist ein eigener Typ, und traegt
                // denselben Zustand** -- **und gemessen kommt -1 in
                // dem Spiel 15 mal vor, also fuer den Spieler, und
                // 21 mal fuer Figuren.**
                // **Und der erste Parameter waehlt, ueber wem das Icon
                // steht** -- **und minus eins ist der Spieler.**
                // **Gemessen: -1 kommt 15 mal vor, Figurnummern 21.**
                // **Und wie lange das Icon bleibt, sagt die Hilfe
                // nicht.** Die drei Einstellungen des Befehls sind
                // die Figur, das Icon und das Warten,
                // **und keine vierte, und keine Dauer irgendwo.**
                // **Und ohne eine Dauer waere *Wait for Completion*
                // eine Wartezeit, die nie endet** -- **also nimmt
                // dieser Leser eine Sekunde, und sagt es zweimal:**
                // einmal hier und einmal an der Konstante.
                var ziel = At(pCommand, 0);
                // **Und der dritte Parameter sagt, ob gewartet wird,
                // und der Rueckgabewert sagt, ob die Liste weitergeht.**
                // **Die beiden sind nicht dasselbe**, **und der
                // Ballon wartet ueber `pInterpreter.Wait`**, **und
                // `Wait` setzt den Zustand selbst.**
                //
                // **Also gibt dieser Zweig `true` zurueck, wenn er nicht
                // wartet, und `false`, wenn er wartet** -- **und ein
                // `return false` ohne Warten liest denselben Befehl im
                // naechsten Bild noch einmal**, **und das Bild, in dem
                // die Liste wieder laeuft, ist dasselbe Bild, in dem das
                // Icon erscheint**, **und also wartet die Liste auf
                // einen Ballon, den sie selbst nicht beendet.**
                if (ziel < 0)
                {
                var warten = Flag(pCommand, 2);
                    pFacts.Player.ShowBalloon(
                        At(pCommand, 1), MzScreen.MaxBalloonFrames);
                    if (warten)
                    {
                        pInterpreter.Wait(MzScreen.MaxBalloonFrames);
                    }

                    pActions.Add(new MzAction(pCommand,
                        $"balloon icon {At(pCommand, 1)} over the player"
                        + (warten ? ", waiting for it to go" : "")));
                    return !warten;
                }

                if (!pFacts.Characters.TryGetValue(ziel, out var figur)
                    || figur == null)
                {
                    pFacts.Notices.Add(
                        $"balloon icon asked for character {ziel}, and "
                        + "this map has no such character");
                    return true;
                }

                var warte = Flag(pCommand, 2);
                figur.ShowBalloon(
                    At(pCommand, 1), MzScreen.MaxBalloonFrames);
                if (warte)
                {
                    pInterpreter.Wait(MzScreen.MaxBalloonFrames);
                }

                pActions.Add(new MzAction(pCommand,
                    $"balloon icon {At(pCommand, 1)} over character {ziel}"
                    + (warte ? ", waiting for it to go" : "")));
                return !warte;
            }

            case MzCommandTable.ControlSwitches:
            {
                // for (let i = params[0]; i <= params[1]; i++) — inclusive, and
                // the third parameter says which way, 0 being on.
                var from = At(pCommand, 0);
                var to = At(pCommand, 1);
                var on = At(pCommand, 2) == 0;
                foreach (var id in Range(from, to))
                {
                    pFacts.SetSwitch(id, on);
                    pActions.Add(MzAction.Switches(pCommand, id, id, on));
                }
                return true;
            }

            case MzCommandTable.ControlVariables:
            {
                if (!TryOperand(pCommand, pFacts, out var value, out var missing))
                {
                    pInterpreter.Stop(MzStep.Refused, missing);
                    return false;
                }
                // A random operand picks a number in its own range, and the
                // engine draws **inside** its loop:
                //
                //     for (let i = startId; i <= endId; i++) {
                //         const realValue = value + Math.randomInt(randomMax);
                //         this.operateVariable(i, operationType, realValue);
                //     }
                //
                // so every variable of a range gets its own roll. That is kept
                // because a reader that drew once for the range would give a
                // game the same number five times over in a roll of "how many
                // of these did I get" — and this comment once said the opposite
                // of what the line below it does.
                var randomMax = 1;
                if ((MzOperand)At(pCommand, 3) == MzOperand.Random)
                {
                    var low = At(pCommand, 4);
                    randomMax = System.Math.Max(At(pCommand, 5) - low + 1, 1);
                    value = low;
                }
                var operation = (MzOperation)At(pCommand, 2);
                var from = At(pCommand, 0);
                var to = At(pCommand, 1);
                foreach (var id in Range(from, to))
                {
                    var was = pFacts.Variable(id);
                    // The engine draws inside its own loop:
                    //
                    //     for (let i = startId; i <= endId; i++) {
                    //         const realValue = value + Math.randomInt(randomMax);
                    //         this.operateVariable(i, operationType, realValue);
                    //     }
                    //
                    // so every variable of a range gets its OWN roll. A reader
                    // that drew once for the range would give a game the same
                    // number five times over in a roll of "how many of these did
                    // I get", and this file's own comment claimed that was the
                    // engine's shape until the source was read again.
                    var drawn = randomMax > 1
                        ? value + (pRandom?.Next(randomMax) ?? 0)
                        : value;
                    var is_ = Operate(was, drawn, operation);
                    pFacts.SetVariable(id, is_);
                    pActions.Add(MzAction.Variable(
                        pCommand, id, was, is_, Operation(operation)));
                }
                return true;
            }

            default:
                return true;
        }
    }

    /// <summary>
    /// The value a variable command works with. Five of the engine's six
    /// operands are read here; the sixth is the author's own script and is
    /// refused, because this repository does not evaluate a game's JavaScript.
    /// </summary>
    private static bool TryOperand(
        MzCommandEntry pCommand, MzBranchFacts pFacts,
        out int pValue, out string pMissing)
    {
        pValue = 0;
        pMissing = "";
        switch ((MzOperand)At(pCommand, 3))
        {
            case MzOperand.Constant:
                pValue = At(pCommand, 4);
                return true;

            case MzOperand.Variable:
            {
                var id = At(pCommand, 4);
                if (!pFacts.HasVariable(id))
                {
                    pMissing = $"variable {id}, which the command works with";
                    return false;
                }
                pValue = pFacts.Variable(id);
                return true;
            }

            case MzOperand.Random:
                // The drawn value is added by the caller, which also holds the
                // range; here the low end is enough to say it is answerable.
                pValue = At(pCommand, 4);
                return true;

            case MzOperand.GameData:
            {
                // Nine kinds of thing the game can count, named by the engine.
                // Each needs a database this reader has not opened, so each is
                // refused by name rather than answered with a zero that would
                // look like the engine had counted nothing.
                var what = At(pCommand, 4) switch
                {
                    0 => "the number of items held",
                    1 => "the number of weapons held",
                    2 => "the number of armours held",
                    3 => "an actor's weapon count",
                    4 => "an actor's armour count",
                    5 => "another actor's weapon count",
                    6 => "another actor's armour count",
                    7 => "an enemy's number",
                    8 => "how often the player has escaped",
                    9 => "how many times the player has saved",
                    var other =>
                        $"a count this reader has no name for ({other})",
                };
                pMissing = what;
                return false;
            }

            case MzOperand.Script:
                // The engine writes `value = eval(params[4])`. This repository
                // does not evaluate a game's JavaScript and the one thing it may
                // not do is answer as though it had.
                pMissing = "the author's own script, which is not run here";
                return false;

            default:
                pMissing = $"an operand this reader has no name for"
                    + $" ({At(pCommand, 3)})";
                return false;
        }
    }

    /// <summary>
    /// The engine's six operations. Dividing by zero and taking a remainder of
    /// zero both come to zero here, because the engine catches the failure and
    /// writes zero rather than letting it out.
    /// </summary>
    internal static int Operate(int pWas, int pValue, MzOperation pOperation)
    {
        try
        {
            return pOperation switch
            {
                MzOperation.Set => pValue,
                MzOperation.Add => pWas + pValue,
                MzOperation.Subtract => pWas - pValue,
                MzOperation.Multiply => pWas * pValue,
                MzOperation.Divide => pWas / pValue,
                MzOperation.Modulo => pWas % pValue,
                _ => throw new MzDataException(
                    $"an operation this reader has no name for"
                    + $" ({(int)pOperation})"),
            };
        }
        catch (System.ArithmeticException)
        {
            // The engine catches anything and writes zero. So does this.
            return 0;
        }
    }

    private static string Operation(MzOperation pOperation) => pOperation switch
    {
        MzOperation.Set => "set from",
        MzOperation.Add => "added to",
        MzOperation.Subtract => "less",
        MzOperation.Multiply => "times",
        MzOperation.Divide => "divided by",
        MzOperation.Modulo => "mod",
        _ => "by an operation this reader has no name for",
    };

    /// <summary>
    /// Where a picture goes, as <c>picturePoint</c> answers it. **The fourth
    /// parameter decides where the fifth and sixth are read from**: a number
    /// written in the event, or the number a variable holds.
    /// </summary>
    private static bool Point(
        MzCommandEntry pCommand, MzBranchFacts pFacts,
        out int pX, out int pY)
    {
        pX = 0;
        pY = 0;
        var fromVariables = At(pCommand, 3) != 0;
        pX = From(pCommand, pFacts, 4, fromVariables);
        pY = From(pCommand, pFacts, 5, fromVariables);
        return fromVariables;
    }

    private static int From(
        MzCommandEntry pCommand, MzBranchFacts pFacts, int pIndex, bool pVariable) =>
        pVariable
        ? pFacts.Variables.TryGetValue(At(pCommand, pIndex), out var held)
            ? held
            : 0
        : At(pCommand, pIndex);

    /// <summary>
    /// A parameter read the way <c>if (params[11])</c> reads it.
    /// </summary>
    /// <remarks>
    /// **A truth value, not a number.** RPG Maker writes booleans into the
    /// event list as <c>"1"</c> and <c>""</c>, and a game may leave the slot
    /// out entirely. A reader that called <c>int.Parse</c> on the string
    /// would throw on an empty one — and a reader that read an empty string as
    /// zero would be right by accident, while a reader that read a missing
    /// parameter as a number at all would not.
    /// </remarks>
    private static bool Truth(MzCommandEntry pCommand, int pIndex)
    {
        if (pIndex >= pCommand.Parameters.Count)
        {
            return false;
        }
        var written = pCommand.Parameters[pIndex];
        return written == "1"
            || string.Equals(written, "true", System.StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// A parameter the game wrote as text — a picture's file name, which is
    /// the only string a 231 carries.
    /// </summary>
    private static string Text(MzCommandEntry pCommand, int pIndex) =>
        pIndex < pCommand.Parameters.Count ? pCommand.Parameters[pIndex] : "";

    /// <summary>
    /// Whether a parameter says yes, and it may say it as a number or as
    /// a word.
    /// </summary>
    /// <param name="pCommand">The command.</param>
    /// <param name="pIndex">Which parameter.</param>
    /// <returns>
    /// True for <c>1</c> and for <c>"true"</c> in any case, false for
    /// <c>0</c>, for <c>"false"</c> and for a parameter that is not
    /// there.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <strong>And a finished MZ project writes both forms, and not
    /// interchangeably.</strong> Measured on <c>CamelliaCoronation</c>:
    /// <c>213</c> carries <c>["-1", "2", "False"]</c> and
    /// <c>["-1", "8", "True"]</c> -- <strong>words</strong> -- and
    /// <c>221</c> carries an <em>empty</em> list, and <c>122</c> carries
    /// <c>["1", "1", "0", "3", "0", "2"]</c> -- <strong>numbers</strong>.
    /// </para>
    /// <para>
    /// <strong>And a reader that only parsed numbers got
    /// <c>At(...)</c> to return 0 for <c>"True"</c></strong>, **and
    /// <c>0 == 1</c> is false, <strong>and so the waiting setting of a
    /// balloon or an animation was dead in every real game.</strong> The
    /// tests all passed, <strong>because the tests wrote the numbers
    /// themselves.</strong>
    /// </para>
    /// <para>
    /// <strong>And a parameter that is not there is "no".</strong> The
    /// engine's own parameters always have a value,
    /// <strong>and a missing one is a file this reader cannot answer,
    /// and saying "no" keeps the list running rather than waiting on
    /// nothing.</strong>
    /// </para>
    /// </remarks>
    private static bool Flag(MzCommandEntry pCommand, int pIndex)
    {
        if (pIndex >= pCommand.Parameters.Count)
        {
            return false;
        }

        var roh = pCommand.Parameters[pIndex];
        if (int.TryParse(
            roh,
            System.Globalization.NumberStyles.Integer,
            System.Globalization.CultureInfo.InvariantCulture, out var zahl))
        {
            return zahl != 0;
        }

        return string.Equals(
            roh, "true", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(
                roh, "on", System.StringComparison.OrdinalIgnoreCase);
    }


    private static int At(MzCommandEntry pCommand, int pIndex) =>
        pIndex < pCommand.Parameters.Count
        && int.TryParse(
            pCommand.Parameters[pIndex],
            System.Globalization.NumberStyles.Integer,
            System.Globalization.CultureInfo.InvariantCulture, out var value)
            ? value
            : 0;

    /// <summary>
    /// Every number from the first to the last, both ends in, which is what the
    /// engine's own loop over a range does.
    /// </summary>
    /// <summary>
    /// The four numbers an audio command carries, as an object in its
    /// first parameter.
    /// </summary>
    /// <param name="pCommand">The command.</param>
    /// <returns>Name, volume, pitch and pan.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And the object comes back as text, and is read again
    /// here.</strong> <c>MzCommandEntry.From</c> writes a nested object
    /// with <c>MzJson.Write</c> so that nothing is thrown away, and
    /// this reads it with <c>MzJson.TryParse</c> — **the same parser
    /// that wrote it**, **and not a second one that could disagree with
    /// the first.**
    /// </para>
    /// <para>
    /// <strong>And a parameter that is not an object falls back to the
    /// XP form</strong>, which is four numbers, **because XP writes
    /// `11510` and MZ writes `241` and a reader that only knew one of
    /// them refused half the engines it claims.**
    /// </para>
    /// <para>
    /// <strong>And a parameter that is neither gets an empty name</strong>,
    /// **and `Play` accepts an empty name**, because that is what the
    /// editor writes when a person cleared the field: **a command that
    /// names nothing is a stop, and refusing it would leave the old
    /// track playing.**
    /// </para>
    /// </remarks>
    private static (string Name, int Volume, int Pitch, int Pan) AudioOf(
        MzCommandEntry pCommand)
    {
        if (pCommand.Parameters.Count > 0
            && MzJson.TryParse(
                pCommand.Parameters[0], out var wert, out _)
            && wert.Kind == MzKind.Object)
        {
            return (wert.Member("name")?.Text ?? "",
                wert.Member("volume")?.IntOr(0) ?? 0,
                wert.Member("pitch")?.IntOr(0) ?? 0,
                wert.Member("pan")?.IntOr(0) ?? 0);
        }

        // **Und die XP-Form: vier Zahlen und kein Name.**
        return ("",
            pCommand.Parameters.Count > 0 ? At(pCommand, 0) : 0,
            pCommand.Parameters.Count > 1 ? At(pCommand, 1) : 0,
            pCommand.Parameters.Count > 2 ? At(pCommand, 2) : 0);
    }


    /// <summary>
    /// The name of one of the four self switches, from the letter the
    /// command carries.
    /// </summary>
    /// <param name="pIndex">Zero for A, one for B, and so on.</param>
    /// <returns>The letter, or a question mark for a number that is not
    /// one of the four.</returns>
    /// <remarks>
    /// <strong>And the letters are the engine's, and not an
    /// abbreviation this repository chose.</strong> The official help
    /// says *Specify the target self switch (A through D)*,
    /// **and a reader that stored the number showed a player "0" where
    /// a game's own event names "A" in its comments.**
    /// </remarks>
    /// <summary>
    /// Which of the four self switches a letter names, or -1.
    /// </summary>
    /// <param name="pLetter">The letter, as the file wrote it.</param>
    /// <returns>Zero for A, one for B, and so on; -1 for anything
    /// else.</returns>
    /// <remarks>
    /// <strong>And a lowercase letter is the same switch.</strong> The
    /// editor writes upper case, **and a plugin that writes its own
    /// events in lower case wrote the same switch** — **and a reader
    /// that compared exactly would have written "a" and branched on
    /// "A" and never seen its own switch turn on.**
    /// </remarks>
    internal static int SelfSwitchIndex(string pLetter)
    {
        if (string.IsNullOrEmpty(pLetter))
        {
            return -1;
        }

        var gross = char.ToUpperInvariant(pLetter[0]);
        for (var index = 0; index < SelfSwitchLetters.Length; index++)
        {
            if (SelfSwitchLetters[index] == gross)
            {
                return index;
            }
        }

        return -1;
    }


    internal static string SelfSwitchName(int pIndex) =>
        pIndex >= 0 && pIndex < SelfSwitchLetters.Length
            ? SelfSwitchLetters[pIndex].ToString()
            : "?";

    private const string SelfSwitchLetters = "ABCD";


    internal static IEnumerable<int> Range(int pFrom, int pTo)
    {
        for (var i = pFrom; i <= pTo; i++)
        {
            yield return i;
        }
    }
}
