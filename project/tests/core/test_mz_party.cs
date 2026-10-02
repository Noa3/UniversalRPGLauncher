using System.Collections.Generic;
using Godot;
using UniversalRPG.Tests.Framework;
using UniversalRPG.Web;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// What a 126 does to what the party is carrying.
/// </summary>
/// <remarks>
/// <para>
/// K-121 to K-125 read MZ data and walked event lists, and neither needed to
/// know what a game <b>owns</b>. <b>This game's map uses 126 seventeen times</b>
/// across the two event pages that can be reached here, so it is the next
/// command with a real effect that can be checked against the game's own data.
/// </para>
/// <para>
/// The rules are read out of <c>Game_Party</c> and not inferred:
///
/// <list type="number">
/// <item><b>The count is clamped to ninety-nine.</b> This game's events ask for
/// 999 and for 10, and the engine holds 99.</item>
/// <item><b>A count that lands on zero is deleted</b>, not stored as a
/// zero.</item>
/// <item><b>Losing more than there is clamps to zero</b>, it does not go
/// negative.</item>
/// <item><b>An id with no item behind it does nothing</b>, and says so.</item>
/// </list>
///
/// And one that is easy to get wrong in the other direction: <b>the clamp is
/// invisible in the middle of the range</b>, so a test that only ever added
/// four to an empty bag would pass with no clamp at all. Every rule here is
/// asked about at its boundary.
/// </para>
/// </remarks>
partial class TestMzParty : TestBase
{
    private const string FixtureRoot = "res://tests/fixtures/mz";

    /// <summary>The item ids this game really stores, read out of its file.</summary>
    private static List<int> ItemIdsInThisGame()
    {
        var ids = new List<int>();
        var path = FixtureRoot.PathJoin("data").PathJoin("Items.json");
        var file = MzDataFile.Read(
            "Items.json", Godot.FileAccess.GetFileAsBytes(path));
        for (var i = 1; i < file.Root.Items.Count; i++)
        {
            if (file.Root.Items[i].Kind == MzKind.Object)
            {
                ids.Add(i);
            }
        }
        return ids;
    }

    public void Test_TheEngineHoldsNinetyNineAndThisGameAsksForNineHundredNinetyNine()
    {
        // `maxItems` is `return 99`, and `gainItem` is
        //   container[item.id] = newNumber.clamp(0, this.maxItems(item));
        // **This game's events ask for 999.** An implementation that added the
        // number as written would give a player a thousand of something the
        // engine refuses to hold, and a game that checks `hasMaxItems` would
        // then disagree with the reader that filled the bag.
        var ids = ItemIdsInThisGame();
        AssertTrue(
            ids.Count > 100,
            $"the fixture holds {ids.Count} item ids, which is what the game"
            + " stores");

        var facts = new MzBranchFacts();
        var party = new MzParty(facts, ids);

        var said = party.GainItem(2, 0, 0, 999);

        AssertEq(
            party.NumItems(2), 99,
            "and asking for 999 leaves 99, because that is the engine's own"
            + $" maximum; it has {party.NumItems(2)}");
        AssertTrue(
            said.Contains("99") && said.Contains("999"),
            "and what it says names both the count and the amount asked for, so"
            + $" a log shows where the difference went: {said}");
    }

    public void Test_ACountThatLandsOnZeroIsRemovedAndNotLeftBehindAsAZero()
    {
        // `if (container[item.id] === 0) { delete container[item.id]; }` — the
        // engine deletes the entry. **A reader that kept a zero would answer
        // `hasItem` differently the moment a game asked**, because `hasItem` is
        // `this.numItems(item) > 0` and a zero is not greater than a zero, but
        // a branch that asked "is this id in my list" would see it.
        var party = new MzParty(new MzBranchFacts(), ItemIdsInThisGame());

        party.GainItem(3, 0, 0, 5);
        AssertEq(
            party.NumItems(3), 5,
            $"and the five is there; it is {party.NumItems(3)}");

        party.GainItem(3, 0, 0, -5);
        AssertEq(
            party.NumItems(3), 0,
            "and taking the five away leaves none; it is"
            + $" {party.NumItems(3)}");
        AssertTrue(
            !party.Facts.Items.ContainsKey(3),
            "and the entry is gone rather than sitting there as a zero,"
            + " because the engine deletes it");
        AssertTrue(
            !party.HasItem(3),
            "and the party does not have it, as `hasItem` asks");
        AssertTrue(
            !party.Carried().Contains(3),
            "and it is not among what the party carries, which is a count and"
            + " not the container");
    }

    public void Test_LosingMoreThanThePartyHasClampsToZeroAndNotBelowIt()
    {
        // The clamp is from below as well as above:
        // `clamp(0, this.maxItems(item))`. **A reader that only guarded the
        // top would answer minus three** for a party taking four of something
        // it has one of, and a game that then adds three back would end up
        // with a negative count where the engine has none at all.
        var facts = new MzBranchFacts();
        var party = new MzParty(facts, ItemIdsInThisGame());

        party.GainItem(4, 0, 0, 1);
        var said = party.GainItem(4, 0, 0, -4);

        AssertEq(
            party.NumItems(4), 0,
            "taking four of one leaves none and not a negative number; it is"
            + $" {party.NumItems(4)}");
        AssertTrue(
            said.Contains("none"),
            $"and it says the party ran out rather than that a number changed;"
            + $" {said}");

        // And the round trip the engine would do: add three back and there are
        // three, not minus one.
        party.GainItem(4, 0, 0, 3);
        AssertEq(
            party.NumItems(4), 3,
            "and adding three to a party that had none gives three, which is"
            + $" what the engine's clamp makes true; it is {party.NumItems(4)}");
    }

    public void Test_AnIdWithNoItemBehindItChangesNothingAndSaysWhich()
    {
        // `itemContainer` returns null for an item that is not there, and
        // `gainItem` returns early. So the engine steps over it. **This reader
        // says it did not happen**, because a game that asks for an item this
        // repository cannot hand over would otherwise look like a game that
        // had it and used it.
        var ids = ItemIdsInThisGame();
        var facts = new MzBranchFacts();
        var party = new MzParty(facts, ids);

        var said = party.GainItem(9999, 0, 0, 3);

        AssertEq(
            party.Carried().Count, 0,
            "and nothing was carried by it; the party carries"
            + $" {party.Carried().Count}");
        AssertTrue(
            said.Contains("9999"),
            $"and what it says names the id, so the gap can be found: {said}");
        AssertEq(
            party.Notices.Count, 1,
            "and it is recorded as a notice, because a run that asked for"
            + $" something and did not get it is not the same as one that never"
            + $" asked; there are {party.Notices.Count}");

        // **And the other side, which a first draft got backwards.** A party
        // with no list of known ids does not take them at its word: nothing
        // known means nothing allowed. A reader that read an empty set as "no
        // id to object to" would hand a player three of an item the game never
        // had, and the game would look as if it worked.
        var open = new MzParty(new MzBranchFacts());
        open.GainItem(9999, 0, 0, 3);
        AssertEq(
            open.NumItems(9999), 0,
            "and a party that has been told nothing adds nothing, because"
            + " nothing known is not everything allowed; it has"
            + $" {open.NumItems(9999)}");
        AssertEq(
            open.Notices.Count, 1,
            "and says so, so the gap is named rather than silent: there is"
            + $" {open.Notices.Count} notice");
    }

    public void Test_AnOperationOfOneRemovesAndTheValueIsNegatedNotSwapped()
    {
        // `operateValue` is
        //   const value = operandType === 0 ? operand : $gameVariables.value(operand);
        //   return operation === 0 ? value : -value;
        // — **there is no third case.** A reader that treated an operation of
        // two as "multiply" or "set" would invent a third case, and a game
        // cannot ask for one because the editor has no such control.
        var party = new MzParty(new MzBranchFacts(), ItemIdsInThisGame());

        party.GainItem(5, 0, 0, 4);
        AssertEq(
            party.NumItems(5), 4,
            $"four added; it is {party.NumItems(5)}");

        party.GainItem(5, 1, 0, 4);
        AssertEq(
            party.NumItems(5), 0,
            "and the same four with an operation of one removes four, because"
            + $" the engine negates and not swaps; it is {party.NumItems(5)}");

        party.GainItem(5, 7, 0, 4);
        AssertEq(
            party.NumItems(5), 0,
            "and an operation of seven also removes, because every operation"
            + " that is not zero is the same case in the engine's line; it is"
            + $" {party.NumItems(5)}");
    }

    public void Test_AConstantAmountIsNotReadFromAVariableAndAVariableIs()
    {
        // `operateValue` asks the operand's **kind** first, and only reads
        // `$gameVariables` when the kind says so. A first draft read the
        // variable either way, which made every 126 with a constant amount
        // depend on whatever a variable happened to hold — and this game has
        // seventeen of them with a literal.
        var facts = new MzBranchFacts();
        facts.Variables[7] = 42;
        var party = new MzParty(facts, ItemIdsInThisGame());

        // Kind zero: the operand is the number. The variable 7 holds 42 and
        // must make no difference.
        party.GainItem(6, 0, 0, 3, 42);
        AssertEq(
            party.NumItems(6), 3,
            "a kind of zero uses the number written in the event, even though"
            + " a variable holds 42; it is " + party.NumItems(6));

        // Kind one: the operand is a variable number, and 7 holds 42.
        party.GainItem(23, 0, 1, 7, 42);
        AssertEq(
            party.NumItems(23), 42,
            "and a kind of one reads the variable, which is what the engine's"
            + $" operand branch does; it is {party.NumItems(23)}");

        // A kind of one naming a variable that holds nothing is zero, because
        // `$gameVariables.value` of a variable that was never set is 0.
        party.GainItem(49, 0, 1, 999, 0);
        AssertEq(
            party.NumItems(49), 0,
            "and a variable that was never set is worth nothing, as the"
            + $" engine's value() is; it is {party.NumItems(49)}");
    }

    public void Test_TheClampIsHeldAtBothEndsAndIsTheEnginesOwnNinetyNine()
    {
        // The maximum is a field so a test can stand on it, and the default is
        // the engine's own ninety-nine — **not** a number chosen here. A test
        // that only ever used a maximum of three would pass with a reader that
        // had the wrong default.
        var facts = new MzBranchFacts();
        var party = new MzParty(facts, ItemIdsInThisGame());

        AssertEq(
            party.MaxItems, 99,
            "and the default is the engine's own maxItems, which is a hundred"
            + $" less than a thousand; it is {party.MaxItems}");

        // Stand on the boundary from both sides.
        party.GainItem(69, 0, 0, 98);
        AssertEq(
            party.NumItems(69), 98,
            "one below the maximum is one below it; it is"
            + $" {party.NumItems(69)}");
        AssertTrue(
            !party.HasMaxItems(69),
            "and the party is not yet at the maximum, because 98 is not 99");

        party.GainItem(69, 0, 0, 1);
        AssertEq(
            party.NumItems(69), 99,
            "and one more lands on the maximum exactly; it is"
            + $" {party.NumItems(69)}");
        AssertTrue(
            party.HasMaxItems(69),
            "and now it is at the maximum, as hasMaxItems asks");

        party.GainItem(69, 0, 0, 1);
        AssertEq(
            party.NumItems(69), 99,
            "and one more does not go past it, because the clamp is on the"
            + $" top; it is {party.NumItems(69)}");
    }

    public void Test_EveryChangeItemCommandInThisGameClampsToTheEngineLimit()
    {
        // **Every 126 in the one map read here, counted off the game's own
        // list.** Measured, not guessed: eighteen commands over fifteen
        // different items, and five of them ask for more than the engine
        // holds. **A first draft ran the lists and counted what the walk
        // reached, and got nine** — because one of the two pages stops at a
        // 230, and a walk with no frames stops where the engine would be
        // waiting. That is a statement about the walk, not about the game, and
        // the claim is about the game's data, so the data is read directly.
        var ids = ItemIdsInThisGame();
        var asked = new Dictionary<int, int>();
        var over = 0;
        var total = 0;

        foreach (var commands in EventListsInThisGame())
        {
            foreach (var command in commands)
            {
                if (command.Code != MzCommandTable.ChangeItems)
                {
                    continue;
                }
                total++;
                var id = At(command, 0);
                var amount = At(command, 3);
                asked[id] = amount;
                if (amount > 99)
                {
                    over++;
                }
            }
        }

        AssertEq(
            total, 18,
            "and the map read here holds eighteen of them, which is what the"
            + $" fixture carries; it holds {total}");
        AssertEq(
            asked.Count, 15,
            "over fifteen different items; they are"
            + $" {string.Join(", ", asked.Keys)}");
        AssertEq(
            over, 5,
            "and five of them ask for more than the engine holds, which is the"
            + $" claim the fixture exists for; {over} do");

        foreach (var pair in asked)
        {
            var room = new MzBranchFacts();
            var bag = new MzParty(room, ids);
            bag.GainItem(pair.Key, 0, 0, pair.Value);
            var expected = pair.Value > 99 ? 99 : pair.Value;
            AssertEq(
                bag.NumItems(pair.Key), expected,
                $"and item {pair.Key}, which the game asks {pair.Value} of,"
                + $" ends at {expected}");
        }
    }

    public void Test_DieWartezeitGehoertDemInterpreterUndNichtDemBefehl()
    {
        // **Und diese Seite traegt neun `126`, einen `230 [30]` bei Index
        // 9 und drei `117` dahinter.**
        //
        // **Gemessen an `tests/fixtures/mz/data/Map002.json`, und das ist
        // der Weg, den dieser Test liest:**
        //
        // ```text
        //   0..8   126 [1, 0, 0, 4]  ...  126 [150, 0, 0, 4]
        //     9    230 [30]
        //    10..12 117 [342] 117 [343] 117 [344]
        //    13     230 [3]
        //    14     351
        //    15     117 [345]
        //    16     351
        //    17     0
        // ```
        //
        // **Und ein Leser, der `230` mit `false` beantwortete, blieb bei
        // Index 9 stehen** -- **und kam nie an die drei `117`.**
        var facts = new MzBranchFacts();
        var listen = EventListsInThisGame();
        AssertTrue(listen.Count >= 2,
            "**and the fixture holds at least two pages** -- and it holds "
            + listen.Count);
        var zweite = listen[1];
        AssertEq(
            zweite.Count, 18,
            "**and the second page has eighteen commands** -- and it has "
            + zweite.Count + ", and nine of them are 126 and index 9 is a"
            + " 230 that asks for thirty frames");
        AssertEq(
            zweite[9].Code, MzCommandTable.Wait,
            "**and index 9 is the wait** -- and it is "
            + zweite[9].Code);
        AssertEq(
            At(zweite[9], 0), 30,
            "**and it asks for thirty frames** -- and it asks for "
            + At(zweite[9], 0));

        // **Und jetzt der Lauf, und das ist die Behauptung.**
        //
        // **Ein `230 [30]` gibt `true` zurueck**, -- **gemessen an
        // `command230`**:
        //
        // ```js
        // Game_Interpreter.prototype.command230 = function() {
        //     this.wait(this._params[0]);
        //     return true;
        // };
        // ```
        //
        // **Und der Index geht hinauf** -- **und die Wartezeit zaeht im
        // naechsten `updateWaitCount` herunter.**
        var lauf = new MzInterpreter(zweite);
        lauf.Setup(0, 0);
        var hinzugefuegt = 0;
        var schutz = 0;
        while (lauf.IsRunning && schutz++ < 2000)
        {
            if (lauf.Stopped == MzStep.Waiting && !lauf.PassFrame())
            {
                // **Und `updateWaitCount` bricht das Bild ab, und es wird
                // kein Befehl gelesen**, -- **und so kann sich eine
                // Wartezeit nicht selbst neu stellen.**
                continue;
            }

            if (lauf.Index < lauf.Commands.Count
                && lauf.Commands[lauf.Index].Code
                    == MzCommandTable.ChangeItems)
            {
                hinzugefuegt++;
            }

            if (!lauf.ExecuteOne(new List<MzAction>(), facts)
                && lauf.Stopped != MzStep.Waiting)
            {
                break;
            }
        }

        AssertEq(
            hinzugefuegt, 9,
            "**and all nine of the 126s are carried out** -- and it carried"
            + $" out {hinzugefuegt}");
        AssertTrue(
            lauf.Index > 9,
            "**and the index is past the wait, and that is the whole"
            + " finding** -- and it is at " + lauf.Index
            + ", and a reader that kept the index on the 230 read the same"
            + " command again every frame, set the same thirty frames"
            + " again, and never reached the 117 behind it");

        // **Und es steht bei 18, und nicht bei 10, und der Grund ist der
        // Befehl `117`, den ein einzelner Interpreter nicht kennt.**
        //
        // **Und das ist keine Fehlmeldung und kein Fehler**: `117` ist der
        // Aufruf eines gemeinsamen Ereignisses, und ein `MzInterpreter`
        // allein weiss keine gemeinsamen Ereignisse.
        AssertEq(
            lauf.Index, 18,
            "**and it stands where a lone interpreter stands** -- and it"
            + $" is at {lauf.Index}, and that is the 351 at index 16"
            + " being stepped over like a 0, because a lone interpreter"
            + " has no common events to hand over to");
    }


    /// <summary>
    /// The 126s of a page, plus the commands around them, taken from the
    /// game's own list. **A page built here rather than read**, because the
    /// claim here is about a shape and not about a game's data.
    /// </summary>
    private static List<MzCommandEntry> ListFromWithout(int[] pWanted)
    {
        var page = new List<MzCommandEntry>();
        var list = EventListsInThisGame()[1];
        foreach (var index in pWanted)
        {
            page.Add(list[index]);
        }
        page.Add(new MzCommandEntry(0, new List<string>(), 0));
        return page;
    }

    public void Test_AnInterpreterRunsAChangeItemsAndReadsAllFourParameters()
    {
        // **The wiring, and it is the four rules again from the other side.**
        // Every test above calls `GainItem` directly, which proves the rules
        // and proves nothing about whether the interpreter passes the right
        // four numbers. A first draft read `params[3]` straight off the
        // command, and three mutations survived for exactly that reason: the
        // tests never went through the interpreter.
        //
        // This game's own forms are used: a literal gain of 999, and a gain
        // whose amount is a variable.
        var ids = ItemIdsInThisGame();
        var commands = new List<MzCommandEntry>
        {
            // **The first number is 126, the command's own code, and not the
            // item.** A first draft wrote `2` there and the page was a list of
            // commands the engine has no method for — so it stepped over all of
            // them, recorded nothing, and every count came back zero. The item
            // is the first *parameter*, and that is where the confusion starts.
            //
            // [item, operation, operandKind, operand]
            new(126, new List<string> { "2", "0", "0", "999" }, 0),
            // A gain whose amount is variable 5, which holds 3.
            new(126, new List<string> { "3", "0", "1", "5" }, 0),
            // A removal: operation 1, literal four.
            new(126, new List<string> { "4", "1", "0", "4" }, 0),
            new(0, new List<string>(), 0),
        };

        // A first draft ran this twice — once to "read" the command and once
        // to do it — and the first loop claimed nothing, because it wrote to
        // the same facts the assertions below read and so proved nothing about
        // the numbers. The run below is the claim, and it is one run.
        // **The facts carry the ids**, and that is the wiring: a first draft
        // built the party with `ids` and the interpreter with facts that had
        // none, so every 126 was answered from a list the interpreter could
        // not see and every count came back zero.
        var own = new MzBranchFacts { KnownItems = new HashSet<int>(ids) };
        own.Variables[5] = 3;
        var ownParty = new MzParty(own);
        // **Through `Run` and not a hand-written loop.** A first draft drove
        // the interpreter itself, and a fresh one has `Stopped` at whatever it
        // starts as rather than at `Stepped` — so `ExecuteOne` answered false
        // on the very first command and the loop gave up before it had run
        // anything. `Run` is what the engine's own loop goes through, and it
        // does the setup a caller would otherwise have to remember.
        var runner = new MzInterpreter(commands);
        var ownActions = new List<MzAction>();
        runner.Run(ownActions, own);

        AssertEq(
            ownParty.NumItems(2), 99,
            "and the first 126, which asks for 999, left the engine's 99 rather"
            + $" than a thousand; it left {ownParty.NumItems(2)}");
        AssertEq(
            ownParty.NumItems(3), 3,
            "and the second, whose amount is a variable holding 3, left 3 — so"
            + $" the fourth parameter really is the variable; it left"
            + $" {ownParty.NumItems(3)}");
        AssertEq(
            ownParty.NumItems(4), 0,
            "and the third, which removes four of a fresh item, left none, so"
            + $" the second parameter really is the operation; it left"
            + $" {ownParty.NumItems(4)}");
        AssertEq(
            runner.Stopped, MzStep.Finished,
            $"and the page ran through: {runner.Stopped}");
    }

    public void Test_AChangeItemsThatWasNotGivenTheFourParametersIsNotGuessed()
    {
        // **A short parameter list is not a licence to read past the end.**
        // `At` answers zero for an index that is not there, which is the right
        // answer for a command that was written with fewer numbers than the
        // engine would read — and it means a malformed 126 adds nothing rather
        // than throwing or reading whatever happened to be next.
        var facts = new MzBranchFacts
        {
            KnownItems = new HashSet<int>(ItemIdsInThisGame()),
        };
        var party = new MzParty(facts);
        var commands = new List<MzCommandEntry>
        {
            new(126, new List<string> { "2", "0", "0" }, 0),
            new(0, new List<string>(), 0),
        };

        var interpreter = new MzInterpreter(commands);
        var actions = new List<MzAction>();
        interpreter.Run(actions, facts);

        AssertEq(
            party.NumItems(2), 0,
            "and a 126 with the amount missing adds nothing, because the"
            + $" missing number is zero and not whatever followed; the facts"
            + $" hold {party.NumItems(2)}");
        AssertEq(
            interpreter.Stopped, MzStep.Finished,
            $"and the page still ran through rather than throwing: "
            + $"{interpreter.Stopped}");
    }

    public void Test_AChangeItemsIsRefusedWhenTheGameHasNoItemListToAnswerWith()
    {
        // **A reader with no data behind it must not answer for an item it
        // cannot name.** A first draft built the party without the ids and
        // every 126 became a silent no-op: nothing was added and nothing said
        // why, which is worse than an error because a game looks as if it ran.
        // The ids now travel in the facts, and a facts that carries none means
        // the game has not been read.
        var facts = new MzBranchFacts();
        AssertTrue(
            !facts.ItemsAreKnown,
            "and a facts with no ids says so, rather than being read as"
            + " everything existing");

        var party = new MzParty(facts);
        var commands = new List<MzCommandEntry>
        {
            new(126, new List<string> { "2", "0", "0", "999" }, 0),
            new(0, new List<string>(), 0),
        };

        var interpreter = new MzInterpreter(commands);
        var actions = new List<MzAction>();
        interpreter.Run(actions, facts);

        AssertEq(
            party.NumItems(2), 0,
            "and nothing was added, because there was no item to add; the facts"
            + $" hold {party.NumItems(2)}");
        // **The interpreter builds its own party over the same facts**, so the
        // notice lands on the action it recorded and not on the party this
        // test made — a first draft read `party.Notices` and found none, and
        // that was the reader's own doing rather than a missing notice.
        AssertEq(
            actions.Count, 1,
            "and the interpreter recorded one action for the 126, so a caller"
            + $" can see what happened; it recorded {actions.Count}, stopped"
            + $" {interpreter.Stopped}, at index {interpreter.Index} of"
            + $" {interpreter.Commands.Count}, and the facts hold"
            + $" {party.Carried().Count} ids");
        AssertTrue(
            actions[0].What.Contains("2"),
            "and that action says the item, so the gap can be found from a log:"
            + $" {actions[0].What}");
        AssertTrue(
            actions[0].What.Contains("not in this game"),
            "and it says the item is not in this game's file, rather than"
            + $" pretending it added it: {actions[0].What}");

        // And the run itself finished rather than stopping, because the engine
        // steps over an item that is not there — this reader says it did not
        // happen and carries on, which is the same shape with a name on it.
        AssertEq(
            interpreter.Stopped, MzStep.Finished,
            $"and the page still ran through, as the engine's own early return"
            + $" does: {interpreter.Stopped}");
    }

    private static int At(MzCommandEntry pCommand, int pIndex) =>
        pIndex < pCommand.Parameters.Count
        && int.TryParse(
            pCommand.Parameters[pIndex],
            System.Globalization.NumberStyles.Integer,
            System.Globalization.CultureInfo.InvariantCulture, out var value)
            ? value
            : 0;

    private static List<List<MzCommandEntry>> EventListsInThisGame()
    {
        var lists = new List<List<MzCommandEntry>>();
        var path = FixtureRoot.PathJoin("data").PathJoin("Map002.json");
        var file = MzDataFile.Read(
            "Map002.json", Godot.FileAccess.GetFileAsBytes(path));
        foreach (var item in file.Root.Member("events")!.Items)
        {
            if (item.Kind != MzKind.Object)
            {
                continue;
            }
            foreach (var page in item.Member("pages")!.Items)
            {
                var commands = new List<MzCommandEntry>();
                foreach (var command in page.Member("list")!.Items)
                {
                    commands.Add(MzCommandEntry.From(command));
                }
                lists.Add(commands);
            }
        }
        return lists;
    }
}
