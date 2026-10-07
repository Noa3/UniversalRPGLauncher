using System.Collections.Generic;
using System.Linq;
using Godot;
using UniversalRPG.Tests.Framework;
using UniversalRPG.Web;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// What 201 does, and what it refuses.
/// </summary>
/// <remarks>
/// <para>
/// A transfer is the first command in this reader that is <b>not a change and
/// not a number of frames</b>. K-125 made a run wait for frames, K-127 for a
/// picture's movement, and a 201 for neither: the engine sets
/// <c>setWaitMode("transfer")</c> and <c>updateWaitMode</c> asks
/// <c>$gamePlayer.isTransferring()</c> every frame until the map has changed.
/// A condition wait has no length, and a caller passing frames cannot end it.
/// </para>
/// <para>
/// Three rules, and each is a place a first reading goes wrong:
///
/// <list type="number">
/// <item><b>A transfer is reserved, not carried out.</b> <c>reserveTransfer</c>
/// writes <c>_transferring = true</c> and the new map and position, and
/// changes nothing the player can see. <c>performTransfer</c> is what moves
/// them. Applying it while reading the command would move the player before
/// the commands after it had run.</item>
/// <item><b>The engine returns false and transfers nobody</b> in a battle or
/// with a message on the screen. That is not a wait and not a finish: the
/// index stays and the transfer happens in the frame in which the message
/// closes.</item>
/// <item><b>The direction is set on the way, not on the reservation</b>, because
/// <c>performTransfer</c> is what calls <c>setDirection</c>. A player that
/// turned one frame early would face a map they are not on yet.</item>
/// </list>
///
/// And the numbers are this game's: **33 transfers over sixteen maps**, all
/// with the first parameter at zero, so the place is written out rather than
/// read from variables.
/// </para>
/// </remarks>
partial class TestMzPlayerTransfer : TestBase
{
    private const string Root = "res://tests/fixtures/mz_plain/data";

    private static List<MzCommandEntry> TransfersInThisGame()
    {
        var found = new List<MzCommandEntry>();
        for (var i = 1; i < 20; i++)
        {
            // **Three digits, and `$"Map{i:03}"` is not how you get them.**
            // A first draft wrote that and it produced **Map13.json for i = 1**:
            // in an interpolated string `i:03` is read as a fill character of
            // `0` and a **precision** of `3`, and a whole number with a
            // precision is padded on the right — 1 becomes "13", 2 becomes
            // "23". Every file was missing and the reader's own error about a
            // file ending mid-value was the only thing that said so.
            //
            // `ToString("000")` is the right spelling: a precision of `000` in
            // the *format* string means three digits on the left, which is what
            // a map file name wants.
            var name = "Map" + i.ToString("000") + ".json";
            var file = MzDataFile.Read(
                name, Godot.FileAccess.GetFileAsBytes(Root.PathJoin(name)));
            foreach (var item in file.Root.Member("events")?.Items
                ?? new List<MzValue>())
            {
                if (item.Kind != MzKind.Object)
                {
                    continue;
                }
                foreach (var page in item.Member("pages")?.Items
                    ?? new List<MzValue>())
                {
                    foreach (var command in page.Member("list")?.Items
                        ?? new List<MzValue>())
                    {
                        var entry = MzCommandEntry.From(command);
                        if (entry.Code == MzCommandTable.TransferPlayer)
                        {
                            found.Add(entry);
                        }
                    }
                }
            }
        }
        return found;
    }

    public void Test_AReservedTransferHasNotHappenedYetAndThePlayerIsStillWhereTheyWere()
    {
        // **`reserveTransfer` changes nothing a player can see.** It is
        // `this._transferring = true; this._newMapId = mapId; this._newX = x;
        // this._newY = y; this._newDirection = d; this._fadeType = fadeType;`
        // — a record of where to go, and no more.
        //
        // A reader that applied it while reading the command would have moved
        // the player before the commands after the transfer ran, which is the
        // opposite of what the engine does and the difference between a game
        // that leads the player and one that teleports them mid-sentence.
        var player = new MzPlayer();
        player.StandAt(3, 7, 4);
        var said = player.Reserve(12, 20, 30, 2, 0);

        AssertEq(
            player.MapId, 3,
            "and the player is still on the map they were on; they are on"
            + $" {player.MapId}");
        AssertEq(
            player.X, 7,
            $"and still where they stood, which is {player.X}");
        AssertEq(
            player.Y, 4,
            $"and still where they stood, which is {player.Y}");
        AssertEq(
            player.IsTransferring, true,
            "and a transfer is on its way, which is the condition a 201's page"
            + $" waits on; it is {player.IsTransferring}");
        AssertTrue(
            said.Contains("still at"),
            $"and what it says says the player has not moved, so a log shows it:"
            + $" {said}");

        // **And the direction has not turned either**, because
        // `performTransfer` is what calls `setDirection`.
        AssertEq(
            player.Direction, 0,
            "and the player has not turned, because the turn happens when the"
            + $" transfer is carried out and not when it is reserved; they face"
            + $" {player.Direction}");

        // **Now carry it out, and everything moves at once.**
        var done = player.PerformTransfer(new[] { 3, 12 });
        AssertEq(
            player.MapId, 12,
            "and carrying it out moves the player to the map that was"
            + $" reserved; they are on {player.MapId}");
        AssertEq(
            player.X, 20,
            $"and to the x that was written; they are at {player.X}");
        AssertEq(
            player.Y, 30,
            $"and to the y; they are at {player.Y}");
        AssertEq(
            player.Direction, 2,
            "and only now do they turn, because that is where the engine puts"
            + $" setDirection; they face {player.Direction}");
        AssertEq(
            player.IsTransferring, false,
            "and the condition is met, so the page is released;"
            + $" it is {player.IsTransferring}");
        AssertTrue(
            done.Contains("facing"),
            $"and what it says names the direction, so a caller can see the"
            + $" turn happened: {done}");
    }

    /// <summary>
    /// And a transfer in a battle or during a message is <b>held</b>, not
    /// refused.
    /// </summary>
    /// <remarks>
    /// <strong>And the engine's answer is a false return, and a false
    /// return is a wait.</strong>  Measured in the game's own
    /// <c>rmmz_objects.js</c>:
    /// <c>Game_Interpreter.prototype.command201</c> begins
    /// <c>if ($gameParty.inBattle() || $gameMessage.isBusy()) { return
    /// false; }</c>, **and <c>executeCommand</c> does
    /// <c>if (!this[methodName](command.parameters)) { return false; }
    /// this._index++;</c>** -- **the index only moves on a true one.**
    /// <strong>A refusal would end the page; the engine holds it.</strong>
    /// </remarks>
    public void Test_ATransferInABattleOrDuringAMessageWaitsAndNothingIsReserved()
    {
        // **`if ($gameParty.inBattle() || $gameMessage.isBusy()) return
        // false;` is the first line of the command.** In a battle or with a
        // message on screen the engine transfers nobody at all.
        //
        // A reader that moved the player anyway would take a player out of a
        // fight and leave an old message running on a new map, and the two
        // halves of that would both be the reader's own doing.
        var inBattle = new MzBranchFacts();
        inBattle.EnterBattle();
        var battlePlayer = new MzPlayer();
        battlePlayer.StandAt(1, 5, 5);
        inBattle.GetType(); // the facts are built the same way either way
        var battleActions = new List<MzAction>();
        var battle = new MzInterpreter(new List<MzCommandEntry>
        {
            new(MzCommandTable.TransferPlayer,
                new List<string> { "0", "12", "20", "30", "2", "0" }, 0),
            new(0, new List<string>(), 0),
        });

        // The facts carry the player, so build them around it.
        var battleFacts = new MzBranchFacts { Player = battlePlayer };
        battleFacts.EnterBattle();
        battle.Run(battleActions, battleFacts);

        //
        // **Und die Antwort der Engine ist `Waiting`, und nicht
        // `Refused` -- und das ist an der Quelle gemessen, woher diese
        // Seite kommt (`CamelliaCoronation-Win/js/rmmz_objects.js`):**
        //
        // ```js
        // Game_Interpreter.prototype.command201 = function(params) {
        //     if ($gameParty.inBattle() || $gameMessage.isBusy()) {
        //         return false;
        //     }
        // ```
        //
        // **Und was `false` bedeutet, steht eine Ebene hoeher:**
        // `executeCommand` macht `if (!this[methodName](command
        // .parameters)) { return false; } this._index++;` -- **der Index
        // rueckt nur bei `true` weiter**, **und dieselbe Seite wartet im
        // naechsten Bild erneut.**
        //
        // **Ein Test, der hier `Refused` verlangt, prueft eine
        // Erfindung.**  **Und gemessen war der Unterschied an Camellias
        // Map005 Ereignis 4: mit `Refuse` blieb die Seite bei Index 174
        // von 176 stehen, **und mit `WaitFor` lief sie durch.**
        AssertEq(
            battle.Stopped, MzStep.Waiting,
            "and the page waits rather than being refused, because the"
            + " engine's answer is a false return and the index only"
            + " moves on a true one"
            + $" -- and it is {battle.Stopped}");
        AssertEq(
            battlePlayer.MapId, 1,
            "and the player has not moved, because nothing was reserved; they"
            + $" are on {battlePlayer.MapId}");
        AssertEq(
            battlePlayer.IsTransferring, false,
            $"and there is no transfer on its way; it is"
            + $" {battlePlayer.IsTransferring}");
        AssertTrue(
            battleActions[0].What.Contains("battle"),
            "and what it says names the battle, so a caller can tell this from"
            + $" a transfer that happened: {battleActions[0].What}");

        var messagePlayer = new MzPlayer();
        messagePlayer.StandAt(1, 5, 5);
        var duringMessage = new MzBranchFacts
        {
            MessageBusy = true,
            Player = messagePlayer,
        };
        var messageActions = new List<MzAction>();
        var message = new MzInterpreter(new List<MzCommandEntry>
        {
            new(MzCommandTable.TransferPlayer,
                new List<string> { "0", "12", "20", "30", "2", "0" }, 0),
            new(0, new List<string>(), 0),
        });
        message.Run(messageActions, duringMessage);

        AssertEq(
            message.Stopped, MzStep.Waiting,
            "and a message on the screen holds it the same way, because the"
            + " engine tests both with the same false return"
            + $" -- and it is {message.Stopped}");
        AssertEq(
            duringMessage.Player.MapId, 1,
            $"and the player is still where they were; they are on"
            + $" {duringMessage.Player.MapId}");
        AssertTrue(
            messageActions[0].What.Contains("message"),
            $"and what it says names the message, not the battle, so a caller"
            + $" can tell the two apart: {messageActions[0].What}");
    }

    public void Test_ThePageIsHeldUntilTheTransferIsCarriedOutAndNotForAnyNumberOfFrames()
    {
        // **`setWaitMode("transfer")` is a third kind of waiting**, and it is
        // the reason this card exists. A 230 waits for a count and a 232 for a
        // movement; a 201 waits for a **condition**, and `updateWaitMode` asks
        // `$gamePlayer.isTransferring()` every frame until it answers false.
        //
        // **A condition wait has no length.** A caller passing frames cannot
        // end it — the transfer has to be carried out, and until it is, the run
        // stays exactly where it is. A first draft counted frames here as it
        // does for a 230, and the page went on with the player still on the
        // old map.
        var player = new MzPlayer();
        player.StandAt(1, 2, 3);
        var facts = new MzBranchFacts { Player = player };
        var actions = new List<MzAction>();
        var interpreter = new MzInterpreter(new List<MzCommandEntry>
        {
            new(MzCommandTable.TransferPlayer,
                new List<string> { "0", "12", "20", "30", "2", "0" }, 0),
            new(122, new List<string> { "1", "1", "0", "0", "5" }, 0),
            new(0, new List<string>(), 0),
        });
        interpreter.Run(actions, facts);

        AssertEq(
            interpreter.Stopped, MzStep.Waiting,
            "and the run is held, because a transfer holds its page until the"
            + $" map has changed; it is {interpreter.Stopped}");
        AssertEq(
            interpreter.WaitMode, MzWaitMode.Transfer,
            "and the hold is a condition and not a count, which is the whole"
            + $" of this card; it is {interpreter.WaitMode}");
        AssertEq(
            interpreter.Index, 1,
            "and the index is past the transfer, on the command after it,"
            + " because the transfer itself ran; it is at {interpreter.Index}");
        AssertEq(
            interpreter.WaitFrames, 0,
            $"and no frames are being counted, because there is no count to"
            + $" count; it is waiting {interpreter.WaitFrames} frames");
        AssertEq(
            facts.Variables.ContainsKey(1), false,
            "and the command after the transfer has not run, because the page"
            + " is still held");

        // **Frames alone do not release it.** This is the rule a frame-counting
        // reader gets wrong, and it is wrong quietly.
        for (var frame = 0; frame < 200; frame++)
        {
            interpreter.PassFrame(_ => player.IsTransferring);
        }
        AssertEq(
            facts.Variables.ContainsKey(1), false,
            "and two hundred frames release nothing, because the condition is"
            + " not about time; the command after the transfer still has not"
            + $" run, and the page is {interpreter.Stopped}");

        // **Carrying the transfer out releases it**, and that is the only
        // thing that does.
        player.PerformTransfer(new[] { 1, 12 });
        interpreter.PassFrame(_ => player.IsTransferring);
        AssertEq(
            interpreter.WaitMode, MzWaitMode.None,
            "and carrying the transfer out ends the hold, because the"
            + $" condition is met; the wait mode is {interpreter.WaitMode}");

        interpreter.Run(actions, facts);
        AssertEq(
            facts.Variables.TryGetValue(1, out var after), true,
            "and then the command after the transfer runs, which is the whole"
            + $" of what the hold was for; variable 1 is {after}");
    }

    public void Test_ATransferToAMapThisReaderHasNotReadIsNamedAndNotHalfCarriedOut()
    {
        // **`command201` does not check that the map exists**, and
        // `$gameMap.setup` fails further on where nobody is looking. This
        // reader names it instead, and **leaves the player where they were**.
        //
        // Half-applying it would be worse than not moving: the caller would see
        // a position and no file behind it, and a game would look as if it had
        // changed rooms when it had changed into nothing.
        var player = new MzPlayer();
        player.StandAt(1, 4, 4);
        player.Reserve(99, 10, 10, 2, 0);

        var said = player.PerformTransfer(new[] { 1, 2, 3 });

        AssertEq(
            player.MapId, 1,
            "and the player is still on the map they were on, because a"
            + $" transfer into nothing is not a transfer; they are on"
            + $" {player.MapId}");
        AssertEq(
            player.X, 4,
            $"and where they stood; they are at {player.X}");
        AssertEq(
            player.IsTransferring, true,
            "and the reservation is still on its way, because the map may"
            + " still be read later, and throwing it away would lose the"
            + $" game's own request; it is {player.IsTransferring}");
        AssertEq(
            player.Notices.Count, 1,
            "and it is said once, so the gap is in a log and not only in the"
            + $" silence; there is {player.Notices.Count} notice");
        AssertTrue(
            player.Notices[0].Contains("99"),
            $"and the notice names the map the game asked for, so it can be"
            + $" found: {player.Notices[0]}");
        AssertTrue(
            said.Contains("not carried out"),
            $"and what the caller is told says the same thing: {said}");

        // **And a map the reader does have is carried out**, which is the other
        // half of the rule.
        player.KnownMaps.Add(99);
        var done = player.PerformTransfer();
        AssertEq(
            player.MapId, 99,
            "and carrying it out once the map is there moves the player, which"
            + $" is the engine's own arrangement; they are on {player.MapId}");
        AssertEq(
            player.Notices.Count, 1,
            "and says nothing further, because the second attempt worked;"
            + $" there is {player.Notices.Count} notice");
        AssertTrue(
            done.Contains("map 99"),
            $"and what it says names where they arrived: {done}");
    }

    public void Test_ThisGamesOwnThirtyThreeTransfersAllNameAPlaceAndNoneOfThemRunsNow()
    {
        // **This game's thirty-three transfers, measured and not guessed.** All
        // of them carry a zero in the first parameter, so the place is written
        // out rather than read from a variable — which is what a first draft
        // measured and a second one nearly called an error.
        var transfers = TransfersInThisGame();

        AssertEq(
            transfers.Count, 33,
            "and the nineteen maps carry thirty-three of them, which is what"
            + $" the fixture holds; it holds {transfers.Count}");

        // **The place, from the numbers** — and every one of them reads it the
        // same way, so a reader that always looked in the variables would
        // transfer every player in this game to variable four.
        var alleNull = transfers.All(t => t.Parameters[0] == "0");
        AssertTrue(
            alleNull,
            "and every one of them writes the place out rather than naming"
            + " variables, so a reader that always read the variables would"
            + " send every player in this game to variable four; the zeros are"
            + $" {transfers.Count(t => t.Parameters[0] == "0")} of"
            + $" {transfers.Count}");

        // **And they are all real maps this fixture has**, which is the other
        // half: a transfer to a map that is not there is the refusal above,
        // and this game has none of those.
        var ziele = transfers
            .Select(t => int.Parse(
                t.Parameters[1], System.Globalization.CultureInfo.InvariantCulture))
            .Distinct()
            .OrderBy(x => x)
            .ToList();
        AssertTrue(
            ziele.Count > 1,
            "and they go to more than one map, so the fixture can show a"
            + $" transfer that changes the map; they go to {ziele.Count}");
        AssertEq(
            ziele.Max() <= 19, true,
            "and no further than the nineteen maps the fixture holds, so every"
            + $" one of them is a map a reader has; the highest is"
            + $" {ziele.Max()}");

        // **And one of them, run, leaves the player where the engine leaves
        // them** — which is the claim the other tests make, made once with
        // this game's own numbers.
        var first = transfers[0];
        var player = new MzPlayer();
        player.StandAt(1, 0, 0);
        var facts = new MzBranchFacts { Player = player };
        var interpreter = new MzInterpreter(new List<MzCommandEntry>
        {
            first,
            new(0, new List<string>(), 0),
        });
        interpreter.Run(new List<MzAction>(), facts);

        var ziel = int.Parse(
            first.Parameters[1], System.Globalization.CultureInfo.InvariantCulture);
        var zielX = int.Parse(
            first.Parameters[2], System.Globalization.CultureInfo.InvariantCulture);
        var zielY = int.Parse(
            first.Parameters[3], System.Globalization.CultureInfo.InvariantCulture);
        var richtung = int.Parse(
            first.Parameters[4], System.Globalization.CultureInfo.InvariantCulture);

        AssertEq(
            player.MapId, 1,
            "and the first of them, read, has not moved the player, because a"
            + $" 201 only reserves; they are on {player.MapId}");

        player.PerformTransfer(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14,
            15, 16, 17, 18, 19 });
        AssertEq(
            player.MapId, ziel,
            "and carrying it out puts them where the game wrote, which is map"
            + $" {ziel}; they are on {player.MapId}");
        AssertEq(
            player.X, zielX,
            $"at x {zielX}, which is what the game wrote; they are at {player.X}");
        AssertEq(
            player.Y, zielY,
            $"and y {zielY}; they are at {player.Y}");
        AssertEq(
            player.Direction, richtung,
            $"facing {richtung}, which is the fifth parameter and is set only"
            + $" once the transfer has happened; they face {player.Direction}");
    }
}
