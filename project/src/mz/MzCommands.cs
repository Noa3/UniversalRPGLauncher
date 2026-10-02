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
    /// <summary>
    /// Every command this repository's two dispatchers run.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And it is a set and not a pattern, so that coverage reads the
    /// gate instead of a second hand-written list beside it.</strong>
    /// <strong>And that second list drifted four times</strong> -- <strong>and
    /// every time it reported a command that was in the gate as one that
    /// was not</strong>, <strong>which is the one direction a coverage
    /// number must never be wrong in.</strong>
    /// </para>
    /// <para>
    /// <strong>And this set is asserted against the gate in
    /// <c>test_mz_command_coverage_honest</c></strong>, <strong>so a
    /// command that is added to one and not the other fails the
    /// suite</strong> -- <strong>and not silently.</strong>
    /// </para>
    /// </remarks>
    private static readonly HashSet<int> Gate = new()
    {
            MzCommandTable.BattleProcessing,
            MzCommandTable.BreakLoop,
            MzCommandTable.ChangeActorSkill,
            MzCommandTable.ChangeHp,
            MzCommandTable.ChangeActorState,
            MzCommandTable.ChangeArmor,
            MzCommandTable.ChangeGold,
            MzCommandTable.ChangeItems,
            MzCommandTable.ChangePartyMember,
            MzCommandTable.ChangeVehicleImage,
            MzCommandTable.ChangeWeapon,
            MzCommandTable.ChoicesOption,
            MzCommandTable.Comment,
            MzCommandTable.ControlSelfSwitch,
            MzCommandTable.ControlSwitches,
            MzCommandTable.ControlVariables,
            MzCommandTable.Else,
            MzCommandTable.EraseEvent,
            MzCommandTable.EraseEventFromMap,
            MzCommandTable.ErasePicture,
            MzCommandTable.ExitEventProcessing,
            MzCommandTable.FadeOutBgm,
            MzCommandTable.FadeOutBgs,
            MzCommandTable.GatherFollowers,
            MzCommandTable.JumpToLabel,
            MzCommandTable.Label,
            MzCommandTable.Loop,
            MzCommandTable.MovePicture,
            MzCommandTable.MoveRoute,
            MzCommandTable.OpenMenu,
            MzCommandTable.PlayBgm,
            MzCommandTable.PlayBgs,
            MzCommandTable.PlayMe,
            MzCommandTable.PlayMovie,
            MzCommandTable.PlaySe,
            MzCommandTable.PlayerTransparency,
            MzCommandTable.PluginCommand,
            MzCommandTable.PluginCommandCall,
            MzCommandTable.RecoverAll,
            MzCommandTable.RepeatAbove,
            MzCommandTable.RestoreBgm,
            MzCommandTable.SaveBgm,
            MzCommandTable.ScreenFlash,
            MzCommandTable.ScreenShake,
            MzCommandTable.ScreenTint,
            MzCommandTable.ScrollText,
            MzCommandTable.SetEventLocation,
            MzCommandTable.ShowAnimation,
            MzCommandTable.ShowAnimation2,
            MzCommandTable.ShowBalloonIcon,
            MzCommandTable.ShowChoiceList,
            MzCommandTable.ShowDialogue,
            MzCommandTable.ShowFollowers,
            MzCommandTable.ShowItemChoice,
            MzCommandTable.ShowPicture,
            MzCommandTable.ShowText,
            MzCommandTable.StopSe,
            MzCommandTable.TransferPlayer,
            MzCommandTable.Wait,
        // **Und `401` steht hier, obwohl es keine Wirkung hat** --
        // **und doch ist es richtig.** **Der Interpreter nimmt es als
        // die Zeile unter einem `101`** und **lehnt es ab, wenn keiner
        // da ist**, **und das ist der Vertrag, den
        // `test_mz_interpreter` ueber genau diese beiden Faelle
        // behauptet.** **Und meine erste Fassung dieses Tores hat es
        // rausgenommen**, **weil es neben `0`, `412` und `505` in
        // `MzCommandSet.NoMethodCodes` steht** -- **und das ist eine
        // andere Aussage: `NoMethodCodes` sagt, dass die Engine keine
        // Methode dafuer hat, nicht dass dieses Repository es
        // ablehnen soll.** **Und der Test hat mir das gesagt, und
        // nicht die Liste.**
        MzCommandTable.ShowTextLine,
    };

    /// <summary>
    /// Whether this repository runs the command at all.
    /// </summary>
    public static bool HasEffect(int pCode) => Gate.Contains(pCode);

    /// <summary>
    /// Commands the interpreter reads itself and this gate therefore does
    /// not hold.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And these are not gaps.</strong> <strong>And they are here so
    /// that coverage reads one list for what this repository does, and not
    /// two that drift apart.</strong>
    /// </para>
    /// <para>
    /// <c>101</c> through <c>118</c> are control: a line of text, a
    /// choice, a branch, a loop, a jump. <strong>They change the shape of
    /// the run and not the state of the game.</strong>
    /// <para>
    /// <strong>And <c>117 Common Event</c> is here for a different
    /// reason</strong>: <strong><c>MzEventRunner</c> reads it and sets
    /// up a child interpreter before the gate is ever asked</strong>,
    /// <strong>which is the engine's own <c>setupChild</c>.</strong>
    /// <strong>And it is the second most common command in this game's
    /// MV map data at 1871 uses</strong>, <strong>and a gate that did not
    /// know that would have called it missing.</strong>
    /// </para>
    /// </para>
    /// <para>
    /// <strong>And <c>355</c>, <c>655</c> and <c>657</c> are the script
    /// commands</strong>, <strong>and the interpreter reports them and does
    /// not run them, because AGENTS.md forbids running a game's
    /// JavaScript.</strong> <strong>And <c>401</c> is a line of text under
    /// a <c>101</c></strong>: <strong>the interpreter puts it in the
    /// dialogue's block and refuses it when there is no dialogue</strong>,
    /// <strong>which is the engine's own rule and not this
    /// repository's.</strong>
    /// </para>
    /// </remarks>
    public static IReadOnlyList<int> SteuerungsBefehle() => new[]
    {
        102, 111, 112, 113, 115, 118, 117, 355, 401, 413, 655, 657,
    };

    /// <summary>
    /// Every command the gate holds, so coverage reads it and not a list.
    /// </summary>
    public static IReadOnlyList<int> GateBefehle() =>
        new List<int>(Gate);

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

                // **Und der Index ist damit gesetzt, und der
                // Interpreter zaehlt nicht noch einmal.** **Gemessen am
                // Motor: `command101` liest seine Zeilen mit `this._index++`
                // und laeuft mit `_index` hinter der letzten Zeile
                // zurueck**, **und `update` macht danach kein `++`.**
                pInterpreter.IndexWeitergesetzt = true;

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
                // **Und `character(param)` hat drei Faelle, und alle drei
                // sind gemessen** -- **an
                // `Game_Interpreter.prototype.character`:**
                //
                // **Im Kampf ist es nichts, was auch immer die Zahl
                // ist. Unten null ist es der Spieler, und diese Seite
                // nennt ihn minus eins, vierundvierzigmal. Und eine
                // positive Zahl ist das Ereignis mit dieser Nummer.**
                //
                // **Und die Null ist der billigste Fehler, den man hier
                // machen kann:** **sie sieht aus wie "keine Figur"** --
                // **und sie heisst "ich selbst"** -- **und ein Leser, der
                // sie als keine las, liess eine Seite, die ihr eigenes
                // Ereignis herumfuehrt, stillstehen.**
                var id = At(pCommand, 0);
                // **Und die eigene Ereignisnummer steht am Interpreter,
                // und nicht am Befehl** -- **denn der Motor liest sie aus
                // `this._eventId`, und ein Befehl weiss nichts davon.**
                var ownId = pInterpreter.EventId;
                var character = pFacts.TryNameCharacter(
                    id, ownId, out var gefunden) ? gefunden : null;
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

            case MzCommandTable.PluginCommandCall:
            {
                // `command356` is
                //   const args = this._params[0].split(" ");
                //   const command = args.shift();
                //   this.pluginCommand(command, args);
                //   return true;
                //
                // **Und das ist ein einziger Parameter und kein
                // Parameter-Array**, **und der Motor teilt ihn selbst an
                // Leerzeichen** -- **und der erste Teil ist der Name des
                // Aufrufs und der Rest sind seine Argumente.**
                //
                // **Und gemessen an `D:/Itch/sister/www`: 5472 `356`,
                // alle mit genau einem Parameter, und keine `357` im
                // ganzen Spiel.**
                //
                // ```text
                // >持续动作 : 本事件 : 左右震动 : 持续时间[180] : 周期[6]
                // SetSelectItemType 0
                // >图片快捷操作 : 图片[30] : 修改单个图片: 坐标[12,3]
                // ```
                //
                // **Und was der Aufruf tut, ist hier nicht zu erfahren und
                // wird nicht geraten.** **Diese Spielereignisse setzen
                // Bildersequenzen, schwebenden Text und eine
                // Auswahlliste**, **und das steht in der Plugin-Datei, und
                // die wird hier nicht ausgefuehrt.**
                //
                // **Und es wird auch nicht stillschweigend uebergangen**,
                // **denn 5472 Befehle, die nichts tun, lassen ein Spiel
                // aussehen, als laufe es.**
                var roh = Text(pCommand, 0);
                var teile = roh.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
                var name = teile.Length > 0 ? teile[0] : "";
                var argumente = teile.Length > 1
                    ? string.Join(' ', teile[1..])
                    : "";
                pFacts.Notices.Add(
                    $"plugin command \"{name}\""
                    + (argumente.Length > 0 ? $" with \"{argumente}\"" : "")
                    + " was not run, because this repository does not "
                    + "execute a project's JavaScript");
                pActions.Add(new MzAction(
                    pCommand,
                    $"plugin {name}"
                    + (argumente.Length > 0 ? $" {argumente}" : "")
                    + " was asked for and was not run; its JavaScript is "
                    + "not executed here"));
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


            case MzCommandTable.ScrollText:
            {
                // **Und die Regel, woertlich gemessen an
                // `command105`:**
                //
                // ```js
                // if ($gameMessage.isBusy()) return false;
                // $gameMessage.setScroll(params[0], params[1]);
                // while (this.nextEventCode() === 405) {
                //     this._index++;
                //     $gameMessage.add(this.currentCommand().parameters[0]);
                // }
                // this.setWaitMode("message");
                // return true;
                // ```
                //
                // **Und das `return false` bei einem belegten Bildschirm
                // ist der Grund, warum der Befehl den Index NICHT
                // weiterbewegt** -- **und der Leser muss es genauso
                // tun, denn sonst laeuft der naechste Befehl zweimal.**
                if (pFacts.MessageBusy)
                {
                    return false;
                }

                // **Und das `while` setzt den Index in jedem
                // Durchgang** -- **das ist der Motor**
                // (`while (this.nextEventCode() === 405) { this._index++;
                // ... }`)** -- **und ein Leser, der nur las und den
                // Index nicht bewegte, las dieselbe Zeile endlos, und
                // die Liste des Ereignisses waechst ohne Ende.**
                var scrollZeilen = new List<string>();
                while (pInterpreter.Index + 1 < pInterpreter.Commands.Count
                    && pInterpreter.Commands[pInterpreter.Index + 1].Code
                        == MzCommandTable.ShowChoices)
                {
                    pInterpreter.Index++;
                    scrollZeilen.Add(
                        pInterpreter.Commands[pInterpreter.Index]
                            .Parameters.Count > 0
                            ? pInterpreter.Commands[pInterpreter.Index]
                                .Parameters[0]
                            : "");
                }

                pFacts.ScrollSpeed = At(pCommand, 0);
                pFacts.ScrollLines = scrollZeilen;
                pActions.Add(new MzAction(pCommand,
                    $"scroll text at speed {At(pCommand, 0)} with"
                    + $" {scrollZeilen.Count} choice lines"));

                // **Und der Index steht auf der letzten Zeile, und nicht darueber**
                // -- **denn die while oben hat ihn einmal je gelesener Zeile
                // gesetzt, und der Interpreter setzt ihn nicht noch einmal.**
                pInterpreter.WaitFor(MzWaitMode.Message);
                return false;
            }

            case MzCommandTable.ScreenShake:
            {
                // **Und `command225` in voller Laenge:**
                //
                // ```js
                // $gameScreen.startShake(params[0], params[1], params[2]);
                // if (params[3]) { this.wait(params[2]); }
                // return true;
                // ```
                //
                // **Und die Dauer ist der zweite Wert, und nicht der
                // erste** -- **das ist der Fehler, den man macht, wenn man
                // `params[2]` fuer die Dauer haelt.**
                var staerke = At(pCommand, 0);
                var tempo = At(pCommand, 1);
                var dauer = At(pCommand, 2);
                pFacts.Screen.StarteWackeln(staerke, tempo, dauer);
                pActions.Add(new MzAction(pCommand,
                    $"the screen shakes with power {staerke}, speed"
                    + $" {tempo} and duration {dauer}"));
                if (Flag(pCommand, 3))
                {
                    pInterpreter.Wait(dauer);
                    return false;
                }

                return true;
            }

            case MzCommandTable.EraseEventFromMap:
            {
                // **Und `command214` in voller Laenge, und sie ist drei
                // Zeilen:**
                //
                // ```js
                // if (this.isOnCurrentMap() && this._eventId > 0) {
                //     $gameMap.eraseEvent(this._eventId);
                // }
                // return true;
                // ```
                //
                // **Und die Wache ist `isOnCurrentMap() && _eventId > 0`
                // und nicht eine Pruefung der Liste** -- **denn ein
                // gemeinsames Ereignis hat keine Karte, und ein `214`
                // darin waere ein Befehl ohne Ziel.**
                //
                // **Und gemessen an `D:/Itch/sister/www`: 165 Verwendungen,
                // mehr als jeder andere Befehl ausser `355` und `108`** --
                // **und es ist der Befehl, der eine Truhe oder eine Tuer
                // wegnimmt, nachdem der Spieler sie genommen hat.**
                if (pInterpreter.MapId > 0 && pInterpreter.EventId > 0)
                {
                    var da = pFacts.Map.Erase(pInterpreter.EventId);
                    pActions.Add(new MzAction(pCommand,
                        da == null
                            ? $"event {pInterpreter.EventId} is erased from "
                                + $"map {pInterpreter.MapId}"
                            : da + " is erased from map "
                                + pInterpreter.MapId));
                }
                else
                {
                    pFacts.Notices.Add(
                        "214 Erase Event did nothing, because it ran without "
                        + "a map and an event: MapId "
                        + pInterpreter.MapId + ", EventId "
                        + pInterpreter.EventId + ". The engine's own guard "
                        + "is isOnCurrentMap() && this._eventId > 0.");
                    pActions.Add(new MzAction(pCommand,
                        "erasing an event was asked for without a map and "
                        + "an event, and the engine's guard stops there too"));
                }

                return true;
            }


            case MzCommandTable.ScreenFlash:
            {
                // **Und `command224`:**
                //
                // ```js
                // $gameScreen.startFlash(this._params[0], this._params[1]);
                // if (this._params[2]) { this.wait(this._params[1]); }
                // return true;
                // ```
                //
                // **Und das ist `223` mit einer Farbe und ohne Einfaerbung,
                // und beide nehmen denselben dritten Wert als "warten"** --
                // **und gemessen an `D:/Itch/sister/www`:
                // `[[255, 255, 255, 119], 60, false]`**
                // **[weiss, einhundertneunzehn staerke, sechzig Bilder,
                // ohne Warten].**
                //
                // **Und die vierte Zahl ist die Staerke und keine
                // Deckkraft** -- **und `updateFlash` laeuft mit `i < 4`
                // ueber vier Kanäle**, **genauso wie `updateTone`.**
                var farbe = Vier(pCommand, 0);
                var dauer = At(pCommand, 1);
                var warten = Flag(pCommand, 2);
                pFacts.Screen.StarteBlitz(farbe, dauer);
                pActions.Add(new MzAction(pCommand,
                    $"the screen flashes {farbe[0]},{farbe[1]},{farbe[2]}"
                    + $",{farbe[3]}"
                    + (dauer > 0
                        ? $" over {dauer} frames"
                        : " at once")));
                if (warten)
                {
                    pInterpreter.Wait(dauer);
                    return false;
                }

                return true;
            }


            case MzCommandTable.ScreenTint:
            {
                // **Und `command223` in voller Laenge:**
                //
                // ```js
                // $gameScreen.startTint(this._params[0], this._params[1]);
                // if (this._params[2]) { this.wait(this._params[1]); }
                // return true;
                // ```
                //
                // **Und `startTint` nimmt eine Farbe und eine Dauer, und
                // der dritte Wert ist ein Wahrheitswert und keine Zahl.**
                //
                // **Und die Karte, die diesen Befehl traegt, schreibt
                // `[[-68, -68, -68, 0], 999, false]` -- also eine
                // Einfaerbung ueber neunhundertneunundneunzig Bilder und
                // ohne Warten.** **Und eine Einfaerbung, die nie fertig
                // wird, ist Absicht und kein Fehler.**
                //
                // **Und der erste Wert ist ein Vierer und steht in einem
                // eigenen Array**, **und `updateTone` laeuft mit
                // `for (let i = 0; i < 4; i++)` ueber vier Kanäle und
                // nicht ueber drei** -- **das ist aus `rpg_objects.js`
                // gelesen und nicht aus dem Gedächtnis, denn die erste
                // Fassung dieses Kommentars behauptete, die Engine lese
                // drei, und `updateTone` widerlegt das im selben
                // Bildschirm.**
                var ton = Vier(pCommand, 0);
                var dauer = At(pCommand, 1);
                var warten = Flag(pCommand, 2);
                pFacts.Screen.StarteTon(ton, dauer);
                pActions.Add(new MzAction(pCommand,
                    $"the screen is tinted {ton[0]},{ton[1]},{ton[2]}"
                    + $",{ton[3]}"
                    + (dauer > 0
                        ? $" over {dauer} frames"
                        : " at once")));
                if (warten)
                {
                    pInterpreter.Wait(dauer);
                    return false;
                }

                return true;
            }


            case MzCommandTable.ChangeHp:
            {
                // **Und `command311`:**
                //
                // ```js
                // const value = this.operateValue(this._params[2],
                //     this._params[3], this._params[4]);
                // this.iterateActorEx(this._params[0], this._params[1],
                //     actor => { this.changeHp(actor, value,
                //     this._params[5]); });
                // return true;
                // ```
                //
                // **Und `changeHp` ist** `if (target.isAlive()) { if
                // (!allowDeath && target.hp <= -value) { value = 1 -
                // target.hp; } target.gainHp(value); if (target.isDead())
                // { target.performCollapse(); } }` -- **und die zweite
                // Zeile ist der ganze Befehl**: **ein Schaden, der toeten
                // darf, wird auf einen Punkt vor dem Tod gekuerzt, und
                // einer, der es darf, nicht.**
                //
                // **Und der zweite Parameter ist eine Variable, wenn der
                // erste nicht null ist** -- **denn `iterateActorEx` ist**
                // `if (param1 === 0) { iterateActorId(param2) } else
                // { iterateActorId($gameVariables.value(param2)) }` **--
                // **und `313` wertet ihn genauso aus.**
                // **Und der erste Parameter ist nicht die
                // ganze Partei und auch nicht das Ziel**, **sondern
                // ob das Ziel eine Nummer oder eine Variable
                // ist**:
                //
                // ```js
                // iterateActorEx(param1, param2, callback) {
                //     if (param1 === 0) {
                //         this.iterateActorId(param2, callback);
                //     } else {
                //         this.iterateActorId(
                //             $gameVariables.value(param2), callback);
                //     }
                // }
                // ```
                //
                // **Und gemessen an `D:/Itch/sister/www` sind alle
                // vierzehn `[0, 1, ...]`**, **also Konstante, also
                // Darsteller eins** -- **und `313` und `314` lesen
                // dieselben zwei Plaetze genauso.**
                var hpZiel = At(pCommand, 0) == 0
                    ? At(pCommand, 1)
                    : pFacts.Variable(At(pCommand, 1));
                var hpPartei = hpZiel == 0;
                if (!TryOperateValue(
                    pCommand, pFacts, 2, out var hpWert,
                    out var hpFehlt))
                {
                    pInterpreter.Stop(MzStep.Refused, hpFehlt);
                    return false;
                }

                var sterbenDarf = At(pCommand, 5) != 0;
                foreach (var darsteller in GeordneteZahlen(
                    pFacts.PartyMembers))
                {
                    if (hpPartei || darsteller == hpZiel)
                    {
                        pFacts.HpOrders.Add(new MzHpOrder(
                            darsteller, hpWert, sterbenDarf));
                    }
                }

                pActions.Add(new MzAction(pCommand,
                    (hpPartei ? "every actor, " : "actor " + hpZiel + ", ")
                    + (hpWert < 0 ? "loses " : "gains ")
                    + System.Math.Abs(hpWert) + " hp"
                    + (sterbenDarf ? "" : ", and may not die of it")));
                return true;
            }

            case MzCommandTable.ChangeActorState:
            {
                // **Und `command313`:**
                //
                // ```js
                // this.iterateActorEx(this._params[0], this._params[1],
                //     actor => {
                //         const alreadyDead = actor.isDead();
                //         if (this._params[2] === 0) {
                //             actor.addState(this._params[3]);
                //         } else {
                //             actor.removeState(this._params[3]);
                //         }
                //         if (actor.isDead() && !alreadyDead) {
                //             actor.performCollapse();
                //         }
                //         actor.clearResult();
                //     });
                // return true;
                // ```
                //
                // **Und der dritte Wert ist die Richtung und keine Zahl:**
                // **Null fuegt den Zustand hinzu und alles andere nimmt
                // ihn weg** -- **und gemessen an
                // `D:/Itch/sister/www`: `[0, 2, 0, 25]`, `[0, 2, 0, 26]`,
                // `[0, 2, 0, 28]`**, **also durchgehend "hinzufuegen" an
                // den Zustandsnummern fuenfundzwanzig bis einunddreissig,
                // und das sind Vergiftungen, Schlaf und Krankheiten.**
                //
                // **Und `alreadyDead` wird VOR dem Aendern gelesen**, **und
                // nur wenn der Darsteller danach tot ist und vorher nicht
                // war, bricht er zusammen** -- **das ist der Unterschied
                // zwischen "er ist gerade gestorben" und "er war
                // bereits tot und haelt einen Zustand, der ihn toetet".**
                var ziel = At(pCommand, 0);
                var ganzePartei = At(pCommand, 1) != 0;
                var hinzu = At(pCommand, 2) == 0;
                var zustand = At(pCommand, 3);
                foreach (var darsteller in GeordneteZahlen(pFacts.PartyMembers))
                {
                    if (ganzePartei || darsteller == ziel)
                    {
                        pFacts.States.Add(new MzStateChange(
                            darsteller, zustand, hinzu));
                    }
                }

                pActions.Add(new MzAction(pCommand,
                    (ganzePartei ? "every actor" : $"actor {ziel}")
                    + (hinzu ? " gains state " : " loses state ")
                    + zustand));
                return true;
            }

            case MzCommandTable.ChangeWeapon:
            {
                // **Und `command127` ist `command128` mit einer Waffe**, und
                // `operateValue` beginnt auch hier bei `params[1]`:**
                //
                // ```js
                // const value = this.operateValue(this._params[1],
                //                                  this._params[2],
                //                                  this._params[3]);
                // $gameParty.gainItem($dataWeapons[this._params[0]],
                //                    value, this._params[4]);
                // ```
                //
                // **Und das ist der dritte Befehl dieser Reihe, in dem der
                // erste Platz etwas anderes ist als die Rechnung.**
                var waffe = At(pCommand, 0);
                if (!TryOperateValue(
                    pCommand, pFacts, 1, out var wert, out var fehlt))
                {
                    pInterpreter.Stop(MzStep.Refused, fehlt);
                    return false;
                }

                var vorher = pFacts.Weapons.TryGetValue(
                    waffe, out var alt) ? alt : 0;
                var nachher = vorher + wert;
                var grenze = pFacts.MaxItems;
                nachher = nachher > grenze ? grenze
                    : (nachher < 0 ? 0 : nachher);
                if (nachher == 0)
                {
                    pFacts.Weapons.Remove(waffe);
                }
                else
                {
                    pFacts.Weapons[waffe] = nachher;
                }

                pActions.Add(new MzAction(pCommand,
                    $"weapon {waffe} {vorher} -> {nachher}"));
                return true;
            }

            case MzCommandTable.ChangeActorSkill:
            {
                // **Und `command318` ist `command313` mit einer Fertigkeit
                // fuer einen Zustand**, **und die Form ist in jedem Platz
                // gleich.**
                var ziel = At(pCommand, 0);
                var ganzePartei = At(pCommand, 1) != 0;
                var hinzu = At(pCommand, 2) == 0;
                var fertigkeit = At(pCommand, 3);
                foreach (var darsteller in GeordneteZahlen(pFacts.PartyMembers))
                {
                    if (ganzePartei || darsteller == ziel)
                    {
                        if (hinzu)
                        {
                            pFacts.Skills.Add((darsteller, fertigkeit));
                        }
                        else
                        {
                            pFacts.Skills.Remove((darsteller, fertigkeit));
                        }
                    }
                }

                pActions.Add(new MzAction(pCommand,
                    (ganzePartei ? "every actor" : $"actor {ziel}")
                    + (hinzu ? " learns skill " : " forgets skill ")
                    + fertigkeit));
                return true;
            }

            case MzCommandTable.SaveBgm:
            {
                // **Und `command243` ist eine Zeile:**
                //
                // ```js
                // $gameSystem.saveBgm();
                // return true;
                // ```
                //
                // **Und `saveBgm` kopiert den Kanal**, **und der Kanal
                // traegt einen Namen und drei Zahlen**, **und ein Leser, der
                // nur den Namen merkt, verliert die Lautstaerke und den
                // Ton.** **Und alle fuenfzehn in diesem Spiel tragen keine
                // Parameter.**
                var kanal = pFacts.Screen.Bgm;
                pFacts.RememberedBgm = kanal.Name;
                pFacts.HasRememberedBgm = true;
                pActions.Add(new MzAction(pCommand,
                    "the background music is put aside: "
                    + (kanal.Name.Length > 0
                        ? kanal.Name + " at " + kanal.Volume
                        : "nothing was playing")));
                return true;
            }

            case MzCommandTable.RestoreBgm:
            {
                // **Und `command244` holt ihn wieder** -- **und ein Leser,
                // der `243` einmal und `244` zweimal laufen laesst, legt
                // einen Track zweimal in die Hand.**
                pActions.Add(new MzAction(pCommand,
                    pFacts.HasRememberedBgm
                        ? "the remembered music comes back: "
                            + (pFacts.RememberedBgm.Length > 0
                                ? pFacts.RememberedBgm
                                : "nothing was put aside")
                        : "the remembered music was asked for and there was "
                            + "none, because the engine's own saveBgm slot "
                            + "is empty"));
                return true;
            }

            case MzCommandTable.PlayerTransparency:
            {
                // **Und `command211` liest seinen Parameter verkehrt herum
                // gegen seinen eigenen Namen:**
                //
                // ```js
                // $gamePlayer.setTransparent(this._params[0] === 0);
                // return true;
                // ```
                //
                // **Und der Nachbar `216` ist nicht verkehrt**, **und das
                // ist das Paar, das man falsch liest.**
                var durch = At(pCommand, 0) == 0;
                pFacts.Player.SetTransparent(durch);
                pActions.Add(new MzAction(pCommand,
                    durch
                        ? "the player walks through walls and off the map"
                        : "the player is solid"));
                return true;
            }

            case MzCommandTable.ShowFollowers:
            {
                // **Und `command216` ist nicht verkehrt:**
                //
                // ```js
                // if (this._params[0] === 0) {
                //     $gamePlayer.showFollowers();
                // } else {
                //     $gamePlayer.hideFollowers();
                // }
                // $gamePlayer.refresh();
                // return true;
                // ```
                var gezeigt = At(pCommand, 0) == 0;
                pFacts.Player.SetFollowers(gezeigt);
                pActions.Add(new MzAction(pCommand,
                    gezeigt
                        ? "the followers are on the screen"
                        : "the followers are taken off the screen"));
                return true;
            }

            case MzCommandTable.GatherFollowers:
            {
                // **Und `command217`:**
                //
                // ```js
                // if (!$gameParty.inBattle()) {
                //     $gamePlayer.gatherFollowers();
                //     this.setWaitMode('gather');
                // }
                // return true;
                // ```
                var gesagt = pFacts.Player.GatherFollowers(pFacts.InBattle);
                pActions.Add(new MzAction(pCommand, gesagt));
                if (pFacts.InBattle)
                {
                    return true;
                }

                pInterpreter.WaitFor(MzWaitMode.Gather);
                return false;
            }

            case MzCommandTable.ShowItemChoice:
            {
                // **Und `command104`:**
                //
                // ```js
                // if (!$gameMessage.isBusy()) {
                //     this.setupItemChoice(this._params);
                //     this._index++;
                //     this.setWaitMode('message');
                // }
                // return false;
                // ```
                //
                // **Und es gibt <c>false</c> in jedem Bild zurueck**, **also
                // wird es immer wieder gefragt, bis die Nachricht nicht
                // mehr belegt ist.**
                //
                // **Und `setupItemChoice(params)` ist
                // `$gameMessage.setItemChoice(params[0], params[1] || 2)`
                // -- eine Gegenstandsnummer und eine Kategorie, und keine
                // Spaltenzahl**, **und der Vorgabe ist ZWEI und nicht
                // null**, **weil `0 || 2` in JavaScript auch zwei ist.**
                //
                // **Und gemessen: 23 Stueck, und die haeufigste Form
                // nennt Nummer 90, und Nummer 90 in diesem Projekt ist
                // das Fleisch, das ein Haustierautomat frisst.**
                var gegenstand = At(pCommand, 0);
                var kategorie = At(pCommand, 1) == 0
                    ? 2
                    : At(pCommand, 1);
                pFacts.LastPrompt = new MzPrompt.Item
                {
                    ItemId = gegenstand,
                    Category = kategorie,
                };
                pActions.Add(new MzAction(pCommand,
                    $"the player is asked to hand over item {gegenstand}"
                    + $" of category {kategorie}"));
                pInterpreter.WaitFor(MzWaitMode.Message);
                return false;
            }

            case MzCommandTable.ChangeArmor:
            {
                // **Und `command128`:**
                //
                // ```js
                // const value = this.operateValue(this._params[1],
                //                                  this._params[2],
                //                                  this._params[3]);
                // $gameParty.gainItem($dataArmors[this._params[0]],
                //                    value, this._params[4]);
                // return true;
                // ```
                //
                // **Und `operateValue` faengt hier bei `params[1]` an und
                // nicht bei `params[0]`** -- **denn der erste Platz ist die
                // Ruecksuite und nicht die Rechnung.** **Ein Leser, der
                // `operateValue` immer bei null beginnt, macht aus der
                // Ruecksuite die Rechnung und aus der Rechnung die
                // Operandart** -- **und gemessen an
                // `D:/Itch/sister/www`: `[150, 0, 0, 1, false]`,
                // `[27, 0, 0, 1, false]`, `[100, 0, 0, 1, false]`.**
                //
                // **Und der fuenfte Wert ist ob die Party es equippt**, und
                // **er ist nicht bei allen Fuenfen false**, **sondern das
                // ist eine Entscheidung des Spiels und keine Form.**
                var ruestung = At(pCommand, 0);
                if (!TryOperateValue(
                    pCommand, pFacts, 1, out var wert, out var fehlt))
                {
                    pInterpreter.Stop(MzStep.Refused, fehlt);
                    return false;
                }

                var vorher = pFacts.Armors.TryGetValue(ruestung,
                    out var alt) ? alt : 0;
                var nachher = vorher + wert;
                // **Und `gainItem` klemmt auf neunzigneun und loescht den
                // Eintrag, wenn er auf null landet** -- **und das ist
                // `Game_Party.prototype.gainItem` und nicht etwas, was
                // `128` selbst entscheidet.**
                var grenze = pFacts.MaxItems;
                nachher = nachher > grenze ? grenze
                    : (nachher < 0 ? 0 : nachher);
                if (nachher == 0)
                {
                    pFacts.Armors.Remove(ruestung);
                }
                else
                {
                    pFacts.Armors[ruestung] = nachher;
                }

                pActions.Add(new MzAction(pCommand,
                    $"armour {ruestung} {vorher} -> {nachher}"));
                return true;
            }


            case MzCommandTable.ShowAnimation2:
            {
                // **Und `command212`:**
                //
                // ```js
                // this._character = this.character(this._params[0]);
                // if (this._character) {
                //     this._character.requestAnimation(this._params[1]);
                //     if (this._params[2]) {
                //         this.setWaitMode('animation');
                //     }
                // }
                // return true;
                // ```
                //
                // **Und der erste Wert ist ein Darsteller und kein
                // Darstellernummer** -- **gemessen: `[0, 157, false]`,
                // `[0, 182, false]`, `[-1, 182, false]`** -- **und minus
                // eins ist der Spieler, genau wie bei `213`.**
                //
                // **Und `requestAnimation` nimmt eine Nummer und keine
                // Bildzahl** -- **die Bildzahl steht in `Animations.json`
                /// **und wird hier nicht geraten.**
                //
                // **Und wenn es den Darsteller nicht gibt, passiert nichts
                // und es ist kein Fehler** -- **die Engine prueft `if
                // (this._character)` und geht weiter.**
                var wessen = At(pCommand, 0);
                var animation = At(pCommand, 1);
                var warten = Flag(pCommand, 2);
                if (pFacts.Characters.TryGetValue(wessen, out var zeichen)
                    && zeichen != null)
                {
                    // **Und die Bildzahl ist null, weil sie in der
                    // Projektablage steht und nicht im Befehl** -- **und der
                    // Befehl traegt sie nicht, also wird sie nicht
                    // erfunden.**
                    zeichen.ShowAnimation(animation, 0);
                    pFacts.AnimationAsked.Add(zeichen.EventId);
                    pActions.Add(new MzAction(pCommand,
                        $"character {wessen} is asked for animation "
                        + $"{animation}"
                        + (warten ? ", and the page waits for it" : "")));
                    if (warten)
                    {
                        pInterpreter.WaitFor(MzWaitMode.Animation);
                        return false;
                    }
                }
                else
                {
                    pActions.Add(new MzAction(pCommand,
                        $"character {wessen} was asked for animation "
                        + $"{animation} and is not on this map, which is "
                        + "what the engine's own `if (this._character)` "
                        + "does and is not an error"));
                }

                return true;
            }


            case MzCommandTable.RecoverAll:
            {
                // **Und `command314`:**
                //
                // ```js
                // this.iterateActorEx(params[0], params[1], actor => {
                //     actor.recoverAll();
                // });
                // ```
                //
                // **Und `params[0]` ist der Darsteller und `params[1]`
                // heisst "die ganze Party"** -- **und diese Seite hat
                // `[0, 0]`, und das ist ein Fueller, kein Widerspruch.**
                var ziel = At(pCommand, 0);
                var ganzePartei = At(pCommand, 1) != 0;
                foreach (var darsteller in GeordneteZahlen(pFacts.PartyMembers))
                {
                    if (ganzePartei || darsteller == ziel)
                    {
                        pFacts.Recovered.Add(darsteller);
                    }
                }

                pActions.Add(new MzAction(pCommand,
                    ganzePartei
                        ? $"the whole party recovers to full"
                        : $"actor {ziel} recovers to full"));
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

                // **Und der Schalter gehoert einem Ereignis auf einer
                // Karte, und nicht dem Spiel.**
                //
                // **Gemessen an `command123`: `if (this._eventId > 0)
                // { const key = [this._mapId, this._eventId, params[0]];
                // $gameSelfSwitches.setValue(key, params[1] === 0); }`**
                // -- **drei Zahlen, und die ersten zwei sagen, wessen
                // Schalter es ist.**
                //
                // **Und ein Schalter ohne Ereignis wird vom Motor
                // verworfen** -- **und genau daran haengt, warum eine
                // parallele Seite der Karte keinen setzen kann.**
                var schluessel = SelfSwitchKey(
                    pInterpreter.MapId, pInterpreter.EventId, schalter);
                if (schluessel.Length > 0)
                {
                    pFacts.SelfSwitches[schluessel] = an;
                }

                pActions.Add(new MzAction(pCommand,
                    $"self switch {SelfSwitchName(schalter)} "
                    + (an ? "on" : "off")
                    + (schluessel.Length > 0
                        ? $" on event {pInterpreter.EventId}"
                        : " is dropped, because it belongs to no event")));
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

            case MzCommandTable.ChangeVehicleImage:
            {
                // Die Hilfe sagt: *Change the image used for vehicles.
                // These settings will remain in effect until updated again
                // by using this event command* -- *Vehicle: Specify the
                // target vehicle* -- *Images: Double-click the box to
                // specify the image to be displayed. Setting this to
                // [(None)] will result in no image being displayed.*
                //
                // **Und gemessen: `[1, "MC_Sprite_sheet", 1,
                // "SlimeActors", 5, "Actor1_1"]`** -- **sechs Werte, und
                // das erste ist das Fahrzeug, und es ist 1 in allen
                // sechsen, und 1 ist das Schiff.**
                //
                // **Und `[(None)]` ist ein Bild, und kein leerer
                // Dateiname** -- **und ein Leser, der den Namen
                // genommen hat, zeigte einem Fahrzeug ein Bild, dessen
                // Datei es nicht gibt.**
                var fahrzeug = At(pCommand, 0);
                if (fahrzeug < MzPlayer.Boat || fahrzeug > MzPlayer.Airship)
                {
                    pFacts.Notices.Add(
                        $"vehicle image asked for vehicle {fahrzeug}, and "
                        + "there are three: the boat, the ship and the "
                        + "airship");
                    return true;
                }

                pActions.Add(new MzAction(pCommand,
                    pFacts.Player.SetVehicleImage(
                        fahrzeug,
                        pCommand.Parameters.Count > 1
                            ? pCommand.Parameters[1]
                            : "",
                        pCommand.Parameters.Count > 3
                            ? pCommand.Parameters[3]
                            : "",
                        At(pCommand, 2))));
                return true;
            }


            case MzCommandTable.BattleProcessing:
            {
                // Die Hilfe sagt: *Causes troops to appear and starts a
                // battle* -- *Troops: Specify the troop against which the
                // player will fight* -- *Can Escape: When enabled, the
                // [Escape] command will be enabled during battle* --
                // *Can Lose: When enabled, there will not be a game over
                // even if the entire party is defeated.*
                //
                // **Und der vierte Parameter ist die Verlustart, und
                // nicht die Winneart** -- **und gemessen ist er in
                // diesem Spiel immer `false`.**
                //
                // **Und die beiden Bool-Felder kommen als JSON-Boolean
                // in die Datei**, **und `Flag` liest genau das** --
                // **siehe `213`, wo derselbe Befund schon stand.**
                if (pFacts.InBattle)
                {
                    pFacts.Notices.Add(
                        "a battle is already running, and the engine starts "
                        + "no second one");
                    return true;
                }

                pActions.Add(new MzAction(pCommand,
                    pFacts.StartBattle(
                        At(pCommand, 1),
                        Flag(pCommand, 2),
                        Flag(pCommand, 3))));
                return true;
            }


            case MzCommandTable.SetEventLocation:
            {
                // Die Hilfe nennt die Seite *Set Event Location* und
                // sagt: *Changes the location of an event* -- *Event:
                // Specify the target event. By setting this to [This
                // Event], the event itself will be the target* --
                // *Location: Specify the location to use after the
                // change takes place* -- *Direction: Specify the
                // direction the player should be facing after being
                // moved.*
                //
                // **Und gemessen: `[Ereignis, Ort, X, Y, Richtung]`,
                // und der Ort ist in allen zehn Faellen 0** --
                // **das ist *Direct Designation*, und ein Leser, der
                // den Ort als Bild-Nummer las, hat die Figur auf ein
                // Bild gesetzt, das es nicht gibt.**
                //
                // **Und die Koordinaten kommen aus Variablen, wenn der
                // erste Parameter 1 ist** -- **so wie bei `201`, und der
                // Unterschied wird unten gesagt, statt ihn zu raten.**
                var ziel = At(pCommand, 0);
                if (ziel == 0)
                {
                    pFacts.Notices.Add(
                        "event 0 is not an event, and the engine's own "
                        + "0 means \"this event\"");
                    return true;
                }

                if (ziel != pInterpreter.EventId
                    || !pFacts.Characters.TryGetValue(ziel, out var figur)
                    || figur == null)
                {
                    pFacts.Notices.Add(
                        $"event location asked for event {ziel}, and this "
                        + "map has no such event");
                    return true;
                }

                var ort = At(pCommand, 1);
                var spalte = At(pCommand, 2);
                var zeile = At(pCommand, 3);
                var richtung = At(pCommand, 4);
                pActions.Add(new MzAction(pCommand,
                    ort == 0
                        ? figur.SetLocation(spalte, zeile, richtung)
                        : $"event {ziel} keeps its place, and the location "
                            + $"setting {ort} is one this reader cannot "
                            + "answer"));
                return true;
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
                        // **Und das Warten ist der Zustand "Ballon",
                        // und nicht eine Zahl von Bildern.**
                        //
                        // **Gemessen an `command213`:** `if (params[2])
                        // this.setWaitMode("balloon")` -- **und an
                        // `updateWaitMode`: `case "balloon": waiting =
                        // character && character.isBalloonPlaying()`.**
                        //
                        // **Ein Leser, der hier sechzig Bilder wartete,
                        // wartete auf eine Zeit, die das Spiel nie
                        // genannt hat** -- **und Index 21 einer Seite mit
                        // 211 Befehlen war genau das**, **und die 202
                        // Befehle dahinter kamen nie.**
                        pInterpreter.WaitFor(MzWaitMode.Balloon);
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
                    pInterpreter.WaitFor(MzWaitMode.Balloon);
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

            case MzCommandTable.Comment:
            {
                // `command108` is
                //   this._comments = [this._params[0]];
                //   while (this.nextEventCode() === 408) {
                //     this._index++;
                //     this._comments.push(this.currentCommand().parameters[0]);
                //   }
                //
                // **Und ein Kommentar tut nichts, und das ist die ganze
                // Sache.** **Und die Folgezeilen sind `408` und nicht `0`,
                // und sie gehoeren zum Kommentar und nicht zum Befehl
                // darunter.**
                //
                // **Und die Zeilen sind fuer den Leser nicht belanglos, weil
                // `MzCommandEntry.From` aus jeder Zeile einen Befehl macht** --
                // **und so steht in jedem echten Spiel hinter einem Kommentar
                // eine Zahl, die kein Befehl ist.** **Gemessen an
                // `D:/Itch/sister/www`: 6750 `108` und 1908 `408`.**
                // **Und die Zeilen werden nicht ueber einen Blick nach
                // vorn gezahlt, sondern der Interpreter ueberspringt sie
                // selbst** -- **denn `command108` erhoeht `this._index` in
                // der Schleife, und wenn dieser Leser das nicht tut, laeuft
                // jede Folgezeile als eigener Befehl durch.**
                var zeilen = pInterpreter.SkipCommentLines();
                pActions.Add(new MzAction(pCommand,
                    "comment " + zeilen + " lines"));
                return true;
            }

            case MzCommandTable.ChangeGold:
            {
                // `command125` is
                //   const value = this.operateValue(params[0], params[1],
                //                                        params[2]);
                //   $gameParty.gainGold(value);
                //
                // **Und alle drei Parameter werden gelesen**, **denn der
                // Operand kann eine Variable, eine Konstante, ein
                // Spielerschalter, ein Gegenstand oder ein Zufall sein** --
                // **und derselbe Operandleser steht in `Control Variables`
                // und wird hier wieder gebraucht, weil es derselbe ist.**
                // **Und `operateValue` liest drei Parameter, und nicht
                // fuenf** -- **und das ist der Unterschied zu `122`, das
                // dieselbe Methode mit einem Id-Bereich davor aufruft.**
                //
                // ```text
                // command125: this.operateValue(params[0], params[1], params[2])
                // command122: this.operateValue(params[3], params[4], params[5])
                // ```
                //
                // **Und die Form ist damit `[art, rechnung, wert]` und nicht
                // `[von, bis, rechnung, art, wert]`.** **Ein Leser, der
                // `TryOperand` unveraendert aufruft, laesst drei Parameter
                // fehlen, liest statt dessen den vierten und fuenften -- und
                // die sind nicht da, und beide ergeben null.** **Gemessen an
                // `D:/Itch/sister/www`: 60 `125`, alle in der Form
                // `[1, 0, N]` oder `[1, 1, N]`.**
                if (!TryGoldOperand(
                    pCommand, pFacts, out var wert, out var fehlt))
                {
                    pInterpreter.Stop(MzStep.Refused, fehlt);
                    return false;
                }

                var vorher = pFacts.Gold;
                pFacts.Gold += wert;
                pActions.Add(new MzAction(pCommand,
                    "gold " + vorher + " -> " + pFacts.Gold));
                return true;
            }

            case MzCommandTable.PlayMovie:
            {
                // `command261` is
                //   if (!$gameMessage.isBusy()) {
                //     const name = this._params[0];
                //     if (name.length > 0) {
                //       const ext = this.videoFileExt();
                //       Graphics.playVideo('movies/' + name + ext);
                //       this.setWaitMode('video');
                //     }
                //     this._index++;
                //   }
                //   return false;
                //
                // **Und es gibt `return false`, weil der Befehl in jedem
                // Frame aufgerufen wird, bis das Video vorbei ist.** **Und
                // `this._index++` passiert auch dann, wenn gar kein Film
                // laeuft** -- **und ohne Namen wird ueberhaupt nicht gewartet.**
                if (pFacts.MessageBusy)
                {
                    return false;
                }

                var name = Text(pCommand, 0);
                pActions.Add(new MzAction(pCommand,
                    name.Length > 0 ? "movie " + name : "movie (no name)"));
                if (name.Length > 0)
                {
                    // **Und die Endung kommt aus der Engine und nicht aus
                    // der Liste**, **denn die Liste nennt sie nicht.**
                    pFacts.MoviePlaying = name;
                    pInterpreter.WaitFor(MzWaitMode.Video);
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
    /// <summary>
    /// The operand of a <c>125 Change Gold</c>, which is
    /// <c>[kind, operation, value]</c> and not the five parameters a
    /// <c>122</c> carries.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And it is a separate reader and not an offset.</strong>
    /// <c>operateValue(operationType, operandType, operand)</c> takes three
    /// <strong>by position</strong>, <strong>and <c>122</c> passes its own
    /// fourth, fifth and sixth</strong> -- <strong>so one reader cannot serve
    /// both without being told where to start.</strong>
    /// </para>
    /// </remarks>
    /// <summary>
    /// The value of a <c>125 Change Gold</c>, read the way the engine reads
    /// it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And <c>operateValue</c> is four lines long, and every one of
    /// them changes the answer:</strong>
    /// </para>
    /// <code>
    /// operateValue(operation, operandType, operand) {
    ///     const value = operandType === 0 ? operand
    ///                                     : $gameVariables.value(operand);
    ///     return operation === 0 ? value : -value;
    /// }
    /// </code>
    /// <para>
    /// <strong>So <c>125</c>'s three parameters are
    /// <c>[operation, kind, value]</c></strong> -- **and the first is the
    /// operation and not the kind, which is the reading that costs an
    /// afternoon.** <strong>And <c>operation === 0</c> is <em>take</em> and
    /// anything else is <em>take away</em>, which is not a six-valued
    /// enumeration at all.</strong>
    /// </para>
    /// <para>
    /// <strong>And <c>kind === 0</c> is a constant</strong>, <strong>and that
    /// is the same zero that <c>MzOperand.Constant</c> already names.</strong>
    /// Measured at <c>D:/Itch/sister/www</c>: sixty <c>125</c>, every one
    /// <c>[1, 0, N]</c> or <c>[1, 1, N]</c> -- **which is <em>subtract</em>,
    /// <em>constant</em>, and the amount**, **and a reader that read the
    /// first slot as the kind would look for a constant and find an
    /// operation, and hand the party its own money back.**
    /// </para>
    /// </remarks>
    /// <summary>
    /// The engine's own <c>operateValue</c>, read out of whichever three
    /// parameters the command carries them in.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the position is not the same in every command.</strong>
    /// <c>125</c> calls <c>this.operateValue(params[0], params[1],
    /// params[2])</c> and <c>128</c> calls <c>this.operateValue(params[1],
    /// params[2], params[3])</c> -- <strong>because <c>128</c> spends its
    /// first slot on the armour.</strong> <strong>And one reader that
    /// always started at zero would take the armour as the
    /// operation</strong>, <strong>and the party's stock of plate would go
    /// the wrong way.</strong>
    /// </para>
    /// <para>
    /// <strong>And this is that reader, with the start as an
    /// argument</strong>, <strong>because the engine has one
    /// <c>operateValue</c> and not several.</strong>
    /// </para>
    /// </remarks>
    private static bool TryGoldOperand(
        MzCommandEntry pCommand, MzBranchFacts pFacts,
        out int pValue, out string pMissing)
        => TryOperateValue(pCommand, pFacts, 0, out pValue, out pMissing);

    /// <summary>
    /// <c>operateValue(operation, operandType, operand)</c> at the slot it
    /// starts in.
    /// </summary>
    private static bool TryOperateValue(
        MzCommandEntry pCommand, MzBranchFacts pFacts, int pStart,
        out int pValue, out string pMissing)
    {
        pValue = 0;
        pMissing = "";
        var operation = At(pCommand, pStart);
        var wert = 0;
        switch ((MzOperand)At(pCommand, pStart + 1))
        {
            case MzOperand.Constant:
                wert = At(pCommand, pStart + 2);
                break;

            case MzOperand.Variable:
            {
                var id = At(pCommand, pStart + 2);
                if (!pFacts.HasVariable(id))
                {
                    pMissing = $"variable {id}, which the command works with";
                    return false;
                }

                wert = pFacts.Variable(id);
                break;
            }

            case MzOperand.Script:
                pMissing = "the author's own script, which this repository "
                    + "does not run";
                return false;

            default:
                pMissing = $"an operand of kind {At(pCommand, 1)}, which is "
                    + "a count of something this reader has not opened";
                return false;
        }

        pValue = operation == 0 ? wert : -wert;
        return true;
    }

static bool TryOperand(
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


    /// <summary>
    /// A parameter as a number, and zero where there is none.
    /// </summary>
    /// <remarks>
    /// <strong>And this is internal, and not private, because two files read
    /// command parameters.</strong> It was private while only `MzCommands`
    /// used it, **and the choice block in `MzControlFlow` was a second
    /// reader that could not have had it</strong> — **and a second
    /// reader with its own copy is two places to get a number wrong.**
    /// </remarks>
    internal static int At(MzCommandEntry pCommand, int pIndex) =>
        pIndex < pCommand.Parameters.Count
        && int.TryParse(
            pCommand.Parameters[pIndex],
            System.Globalization.NumberStyles.Integer,
            System.Globalization.CultureInfo.InvariantCulture, out var value)
            ? value
            : 0;


    /// <summary>
    /// A parameter that is itself a list of numbers, read as four of them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is a different shape from every other parameter in
    /// this table.</strong> Measured on a finished project:
    /// <c>223 [[-68, -68, -68, 0], 999, false]</c> -- <strong>and the
    /// first parameter is a list and not a number.</strong>
    /// </para>
    /// <para>
    /// <strong>And there are four numbers in it and the engine reads
    /// four.</strong> <c>updateTone</c> <strong>walks
    /// <c>for (let i = 0; i &lt; 4; i++)</c></strong>, <strong>and a first
    /// draft of this helper read three because the editor's own help names
    /// three colour channels -- and the help is not the engine.</strong>
    /// </para>
    /// <para>
    /// <strong>And a reader that used <see cref="At"/> here got
    /// zero</strong> -- <strong>because <c>At</c> parses the parameter as
    /// text and this one is a list</strong> -- <strong>and a screen that is
    /// tinted to zero, zero, zero is a screen that is not tinted.</strong>
    /// </para>
    /// </remarks>
    internal static int[] Vier(MzCommandEntry pCommand, int pIndex)
    {
        var ergebnis = new int[] { 0, 0, 0, 0 };
        if (pIndex >= pCommand.Parameters.Count)
        {
            return ergebnis;
        }

        var roh = pCommand.Parameters[pIndex];
        var offen = roh.IndexOf('[');
        var zu = roh.LastIndexOf(']');
        if (offen < 0 || zu <= offen)
        {
            ergebnis[0] = At(pCommand, pIndex);
            return ergebnis;
        }

        // **Und `MzCommandEntry.From` schreibt einen verschachtelten Wert
        // mit `MzJson.Write` und **ohne** Leerzeichen** -- **gemessen an
        // `D:/Itch/sister/www`: `[-68,-68,-68,0]`**. **Und ein Leser, der
        // an `", "` trennt, haette hier vier Teile mit je einem Minus und
        // einem Leerzeichen und keine Zahl.**
        var inhalt = roh.Substring(offen + 1, zu - offen - 1);
        var teile = inhalt.Split(',');
        for (var i = 0; i < 4 && i < teile.Length; i++)
        {
            if (int.TryParse(
                teile[i].Trim(),
                System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture,
                out var zahl))
            {
                ergebnis[i] = zahl;
            }
        }

        return ergebnis;
    }

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


    /// <summary>The key an event's self switch is stored under.</summary>
    /// <param name="pMapId">The map the event stands on.</param>
    /// <param name="pEventId">The event, and zero or less for none.</param>
    /// <param name="pIndex">The letter, A to D.</param>
    /// <returns>The key, and nothing when there is no event.</returns>
    /// <remarks>
    /// <strong>And the engine's key is three numbers, measured:</strong>
    /// <c>const key = [this._mapId, this._eventId, params[0]]</c> at
    /// <c>command123</c> — <strong>and it drops the whole write when
    /// <c>this._eventId &lt;= 0</c>.</strong>
    /// <para>
    /// <strong>And this is why a dictionary keyed by the letter alone
    /// answers wrongly.</strong> Measured at Map004: event 9 page 1 asks
    /// for switch 3 and event 10 page 1 asks for switch 3, and a reader
    /// that keyed by letter alone let whichever ran last answer for
    /// both.
    /// </para>
    /// </remarks>
    /// <summary>A set of numbers, in order, and without a dependency.</summary>
    /// <param name="pWerte">The set.</param>
    /// <returns>The numbers, smallest first.</returns>
    /// <remarks>
    /// <strong>And this is here because <c>System.Linq</c> is not
    /// imported in this file</strong>, <strong>and a reader that walked a
    /// <c>HashSet</c> in its own order would name its actors in an order
    /// the game never uses.</strong>
    /// </remarks>
    internal static List<int> GeordneteZahlen(HashSet<int> pWerte)
    {
        var liste = new List<int>(pWerte);
        liste.Sort();
        return liste;
    }

    internal static string SelfSwitchKey(
        int pMapId,
        int pEventId,
        int pIndex)
    {
        return pEventId > 0
            ? $"{pMapId}_{pEventId}_{SelfSwitchName(pIndex)}"
            : "";
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
