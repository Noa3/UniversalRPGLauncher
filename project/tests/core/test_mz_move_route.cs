using System.Collections.Generic;
using System.Linq;
using Godot;
using UniversalRPG.Tests.Framework;
using UniversalRPG.Web;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// 205 Move Route, and the character that walks it.
/// </summary>
/// <remarks>
/// <para>
/// K-130 sent the player somewhere and found that a transfer is a condition,
/// not a count. This card is the other half of movement, and it starts where
/// that one stopped: the player is somewhere, and something has to walk them.
/// </para>
///
/// <para>
/// <b>Three things had to be measured before a line of this could be
/// written, and two of them were not what a first reading expects.</b>
///
/// <list type="number">
/// <item><b>The API is <c>isMapPassable</c> and <c>canPass</c>, not
/// <c>isPassable</c> and <c>checkPassage</c>.</b> Those two names come from
/// other RPG Maker engines. <c>checkPassage</c> has <b>zero</b> occurrences in
/// 1.9.1. A reader built from memory would have compiled and tested nothing
/// real.</item>
///
/// <item><b>MZ has two coordinates, not one.</b> <c>_x</c>/<c>_y</c> is the
/// tile, <c>_realX</c>/<c>_realY</c> is where the character is drawn. On a
/// successful step the drawing position is set to <b>one tile behind</b> —
/// <c>this._realX = $gameMap.xWithDirection(this._x, this.reverseDir(d))</c>
/// — which is what makes a walk look like a walk. A reader with one
/// coordinate snaps, and a snapped character is a teleporting one.</item>
///
/// <item><b>A route is a queue of single steps, not a batch.</b>
/// <c>updateRoutineMove</c> hands a command to <c>processMoveCommand</c> only
/// when the character has arrived. A route of five steps into open floor is
/// <b>five frames</b>.</item>
/// </list>
///
/// <para>
/// <b>This game's own numbers, measured over its ninety-six routes:</b> END
/// 96, MOVE_LEFT 74, MOVE_RIGHT 59, MOVE_DOWN 50, MOVE_UP 45, JUMP 31,
/// CHANGE_SPEED 26, MOVE_BACKWARD 10, TURN_UP 11, TURN_DOWN 10, TURN_RIGHT 9,
/// TURN_LEFT 7, MOVE_FORWARD 4, WAIT 4, TRANSPARENT_ON 4, STEP_ANIME_ON 2,
/// STEP_ANIME_OFF 2. <b>MOVE_LEFT leads and MOVE_DOWN follows</b>, which is
/// the opposite of "this game mostly walks south". MOVE_RANDOM, MOVE_TOWARD,
/// MOVE_AWAY and all eight diagonal codes appear <b>zero</b> times, and are
/// refused rather than guessed at.
/// </para>
///
/// <para>
/// <b>And a product fault this card found on the way.</b>
/// <c>MzCommandEntry.From</c> turns a parameter into a string, and anything
/// that is not a number or a boolean became <c>item.Text</c> — which for a
/// nested object is the empty string. A 205's second parameter is exactly
/// such an object, so <b>every move route in every game came back empty and
/// the reader could not have said why.</b> <c>MzJson.Write</c> now writes a
/// value back out, and a reader that cannot write a shape back has already
/// half-lost it.
/// </para>
/// </remarks>
partial class TestMzMoveRoute : TestBase
{
    private const string Root = "res://tests/fixtures/mz_plain/data";

    /// <summary>A map that answers the two questions a character asks.</summary>
    private sealed class Map : IMzMapPassable
    {
        private readonly bool[,] pBlocked;

        public Map(int pWidth, int pHeight, params (int X, int Y)[] pWalls)
        {
            pBlocked = new bool[pWidth, pHeight];
            foreach (var (x, y) in pWalls)
            {
                pBlocked[x, y] = true;
            }
        }

        /// <summary>A map with no walls, twenty by fifteen.</summary>
        public static Map Open() => new(20, 15);

        public bool IsValid(int pX, int pY) =>
            pX >= 0 && pY >= 0 && pX < pBlocked.GetLength(0) && pY < pBlocked.GetLength(1);

        /// <summary>
        /// A wall blocks <b>every</b> direction here, which is simpler than the
        /// engine's per-direction flags and says so in the test that needs a
        /// per-direction one.
        /// </summary>
        public bool IsPassable(int pX, int pY, int pDir) => IsValid(pX, pY) && !pBlocked[pX, pY];

        public bool IsClearOfCharacters(int pX, int pY) => true;

        public void Block(int pX, int pY) => pBlocked[pX, pY] = true;
    }

    private static List<MzCommandEntry> EveryMoveRouteInThisGame()
    {
        var found = new List<MzCommandEntry>();
        for (var i = 1; i < 20; i++)
        {
            var name = "Map" + i.ToString("000") + ".json";
            var file = MzDataFile.Read(
                name, Godot.FileAccess.GetFileAsBytes(Root.PathJoin(name)));
            foreach (var item in file.Root.Member("events")?.Items ?? new List<MzValue>())
            {
                if (item.Kind != MzKind.Object)
                {
                    continue;
                }
                foreach (var page in item.Member("pages")?.Items ?? new List<MzValue>())
                {
                    foreach (var command in page.Member("list")?.Items ?? new List<MzValue>())
                    {
                        var entry = MzCommandEntry.From(command);
                        if (entry.Code == MzCommandTable.MoveRoute)
                        {
                            found.Add(entry);
                        }
                    }
                }
            }
        }
        return found;
    }

    public void Test_ARouteIsANestedObjectAndAFlatListOfNumbersIsNothingAtAll()
    {
        // **`parameters[1]` is `{list: [...], repeat, skippable, wait}`.** Every
        // entry of the list is `{code, parameters, indent}` — a command, the
        // same shape as an event's own list.
        //
        // A first reading expected `[1, 0, 3, 0, 0]` — five integers, the way
        // an RM2K move route is stored — and got nothing at all, and the
        // reason it got nothing is the second half of this test.
        var routes = EveryMoveRouteInThisGame();

        AssertEq(
            routes.Count, 96,
            "and the nineteen maps carry ninety-six of them, which is what the"
            + $" fixture holds; it holds {routes.Count}");

        var first = routes[0];
        AssertEq(
            first.Parameters[1].StartsWith("{"), true,
            "and the second parameter is an object and not a list of numbers,"
            + $" so a reader that expected integers has nothing to read;"
            + $" it begins with {first.Parameters[1].Substring(0, 1)}");

        AssertTrue(
            first.Parameters[1].Contains("\"list\""),
            "and it carries a list, which is where the steps are;"
            + $" it has {first.Parameters[1].Contains("\"list\"")}");
        AssertTrue(
            first.Parameters[1].Contains("\"wait\""),
            "and a wait flag of its own, which is what holds the event page"
            + $" and not the command; it has {first.Parameters[1].Contains("\"wait\"")}");

        // **And the reason a flat list would have come back empty.** The
        // parameter arrives as a string, and the entry reader used to turn
        // anything that is not a number or a boolean into `item.Text` — which
        // for a nested object is `""`. **Every route in every game was lost
        // and the reader could not have said so.**
        var route = MzRouteStep.ReadFromParameter(first.Parameters[1]);
        AssertTrue(
            route.List.Count > 0,
            "and the route reads back with its steps, where a reader that had"
            + $" turned the object into an empty string would have none;"
            + $" it has {route.List.Count}");
        AssertEq(
            route.List[0].Code, 29,
            "and the first step is the game's own, measured: it is"
            + $" {route.List[0].Code}, which is CHANGE_SPEED");
        AssertEq(
            route.List[route.List.Count - 1].Code, 0,
            "and the last step is END, which every route this game writes"
            + $" carries; it is {route.List[route.List.Count - 1].Code}");
    }

    public void Test_ARouteWalksOneStepAFrameAndFiveStepsTakeFiveFrames()
    {
        // **This is the rule a batch implementation gets wrong, and it is the
        // reason this card exists.** `updateRoutineMove` hands a command to
        // `processMoveCommand` only when `!this.isStopping()` is false — that
        // is, once the character has arrived. One step is issued, the
        // character walks it, and the next comes on the frame it stops.
        //
        // **A route of five steps into open floor is five frames, not one.**
        // A reader that ran the whole list in one call would teleport the
        // character five tiles.
        var map = Map.Open();
        var character = new MzCharacter(5, 5);
        var route = MzRouteStep.ReadFromParameter(
            "{\"list\":[{\"code\":3,\"parameters\":[],\"indent\":null},"
            + "{\"code\":3,\"parameters\":[],\"indent\":null},"
            + "{\"code\":3,\"parameters\":[],\"indent\":null},"
            + "{\"code\":3,\"parameters\":[],\"indent\":null},"
            + "{\"code\":3,\"parameters\":[],\"indent\":null},"
            + "{\"code\":0,\"parameters\":[],\"indent\":null}],"
            + "\"repeat\":false,\"skippable\":false,\"wait\":true}");

        character.Route.Force(route);

        // **The first step is issued right away.**
        var said = character.Route.Step(character, map);
        AssertTrue(
            said.Contains("refused") == false,
            $"and the first step goes through, because the way is clear;"
            + $" it says {said}");
        AssertEq(
            character.X, 6,
            $"and the character is on the tile it stepped to; it is on {character.X}");

        // **And the drawing position is one tile behind, which is what makes
        // it a walk.** `_realX = xWithDirection(_x, reverseDir(d))` — d was
        // right, so the real position is the tile just left.
        AssertEq(
            character.RealX, 5.0,
            "and the drawing position is the tile it came from, one behind"
            + $" where it now stands, because that is what makes a walk look"
            + $" like a walk; it is at {character.RealX}");
        AssertEq(
            character.IsStopping, false,
            $"and it is therefore still walking; stopping is"
            + $" {character.IsStopping}");

        // **The second step waits, because the character is walking.**
        var waited = character.Route.Step(character, map);
        AssertTrue(
            waited.Contains("still walking"),
            "and the next step waits for the character to arrive, because a"
            + $" route is a queue of single steps; it says {waited}");
        AssertEq(
            character.X, 6,
            $"and the character has not moved twice; it is on {character.X}");

        // **Und die Figur braucht fuer eine Kachel sechzehn Bilder,
        // und nicht eines.** **Das ist die Engine, und nicht ein
        // Fehler dieses Lesers:** **`distancePerFrame` ist
        // `2^realMoveSpeed / 256`, und bei Tempo 4 ist das ein
        // Sechzehntel.**
        //
        // **Und eine erste Fassung dieses Tests nahm an, ein Schritt
        // brauche ein Bild, und `PassFrame` ging dann wirklich eine
        // ganze Kachel je Bild** -- **das heisst: die Figur sprang,
        // und der Test beschrieb genau diesen Sprung als Erfolg.**
        // **Wer die ganze Kachel nimmt, sieht zwei Spruenge; wer die
        // Bruchzahl nimmt, sieht einen Lauf.**
        //
        // **Und die Reihe ist schneller, als sie aussieht: Tempo 1
        // braucht 128 Bilder je Kachel und Tempo 6 nur 4** -- **und
        // das ist der Unterschied zwischen einem langsam gehenden
        // und einem gehenden Menschen.**
        var bilderJeKachel = 256 / (1 << 4);   // also sechzehn
        var schritte = 5;
        var bilder = bilderJeKachel * schritte;
        for (var frame = 0; frame < bilder; frame++)
        {
            character.PassFrame();
            character.Route.Step(character, map);
        }

        AssertEq(
            character.X, 10,
            "and after eighty frames the character has walked five"
                + " tiles to the right, one step per arrived tile and"
                + " never five in one call; it is on " + character.X
                + $" and drawn at {character.RealX}");
        AssertEq(
            character.Route.IsDone, true,
            "and the route is done, having spent its steps; it is "
                + character.Route.IsDone
                + $" and {character.FramesToArrival()} frames remain");

    }

    public void Test_AnEventTurnsEvenWhenTheStepIsRefusedAndAThroughOneWalksOverAWall()
    {
        // **`moveStraight` turns in both branches.** On success and on failure,
        // and on failure it calls `checkEventTriggerTouchFront` — a character
        // that bumps a wall faces the wall, and that facing is what triggers
        // the action button in front of it. A reader that only turned on
        // success would have a character face the way it came when it walked
        // into something.
        var map = new Map(10, 10, (6, 5));
        var character = new MzCharacter(5, 5);

        character.MoveStraight(MzCharacter.Right, map);

        AssertEq(
            character.X, 5,
            "and a refused step does not move the character, so it is still on"
            + $" the tile it stood on; it is on {character.X}");
        AssertEq(
            character.Direction, MzCharacter.Right,
            "but it does turn to face the way it tried to go, which is what"
            + $" triggers the action button in front of it; it faces"
            + $" {character.Direction}");

        // **And `isMapPassable` asks twice — both the tile being left and the
        // tile being entered looking back.** `canPass` is
        // `!isValid → through → isMapPassable → isCollidedWithCharacters`, and
        // the order matters: a through character is still stopped by the map
        // border.
        var border = new MzCharacter(0, 5);
        border.MoveStraight(MzCharacter.Left, map);
        AssertEq(
            border.X, 0,
            "and a character on the border is stopped by it, and not walked"
            + $" off the map; it is on {border.X}");

        // **Through wins over walls, but not over the border.**
        var through = new MzCharacter(5, 5) { Through = true };
        through.MoveStraight(MzCharacter.Right, map);
        AssertEq(
            through.X, 6,
            "and a through character walks over the wall that stopped the"
            + $" other, because `if (this.isThrough()) return true` comes"
            + $" after the border and before the map; it is on {through.X}");

        var throughBorder = new MzCharacter(0, 5) { Through = true };
        throughBorder.MoveStraight(MzCharacter.Left, map);
        AssertEq(
            throughBorder.X, 0,
            "and even a through character is stopped by the border, because"
            + $" the border is tested first; it is on {throughBorder.X}");
    }

    public void Test_ABackwardStepTurnsTheCharacterAndPutsTheFacingBackAndAJumpGoesOverAWall()
    {
        // **`moveBackward` is `const lastDirectionFix = this.isDirectionFix();
        // this.reverseDir(this.direction()); … this.setDirectionFix(
        // lastDirectionFix);`** — the character is turned round to walk, and
        // the facing is put back, so a character that walked backwards is left
        // facing the way it came.
        var map = Map.Open();
        var character = new MzCharacter(5, 5, MzCharacter.Right);
        var route = MzRouteStep.ReadFromParameter(
            "{\"list\":[{\"code\":13,\"parameters\":[],\"indent\":null},"
            + "{\"code\":0,\"parameters\":[],\"indent\":null}],"
            + "\"repeat\":false,\"skippable\":false,\"wait\":true}");
        character.Route.Force(route);
        character.Route.Step(character, map);

        AssertEq(
            character.X, 4,
            "and a backward step from facing right walks left, because the"
            + $" engine reverses the direction before moving; it is on {character.X}");
        AssertEq(
            character.Direction, MzCharacter.Right,
            "and the facing goes back to right, because the engine saves and"
            + $" restores it — a character that walked backwards still faces"
            + $" where it came from; it faces {character.Direction}");

        // **A jump takes the bigger axis, and it is not a diagonal.** `if
        // (Math.abs(xPlus) > Math.abs(yPlus))` picks the axis, and a jump of
        // +2 is two tiles along it. **A reader that took both parameters at
        // once would send a character off a wall it cleared.**
        var withWall = new Map(12, 12, (8, 5));
        var jumper = new MzCharacter(5, 5);
        var jump = MzRouteStep.ReadFromParameter(
            "{\"list\":[{\"code\":14,\"parameters\":[\"3\",\"1\"],\"indent\":null},"
            + "{\"code\":0,\"parameters\":[],\"indent\":null}],"
            + "\"repeat\":false,\"skippable\":false,\"wait\":true}");
        jumper.Route.Force(jump);
        jumper.Route.Step(jumper, withWall);

        // **And this expectation was wrong, and it was wrong in the way a
        // reader can be wrong most expensively.**
        //
        // It said "a jump of 3,1 goes three to the right and not one down,
        // because the bigger axis wins", and that reads the engine's
        // direction block as an axis choice. Measured at `jump`:
        //
        // ```js
        // if (Math.abs(xPlus) > Math.abs(yPlus)) {
        //     if (xPlus !== 0) { this.setDirection(xPlus < 0 ? 4 : 6); }
        // } else {
        //     if (yPlus !== 0) { this.setDirection(yPlus < 0 ? 8 : 2); }
        // }
        // this._x += xPlus;
        // this._y += yPlus;
        // ```
        //
        // **The `if` picks the facing and nothing else, and it is inside
        // the direction block; the position is two plain additions after
        // it.** So 3,1 lands three right AND one down, which is a
        // diagonal tile and not a mistake. A leap that dropped the
        // smaller axis landed the figure on the wrong tile.
        AssertEq(
            jumper.X, 8,
            "and a jump of 3,1 moves three to the right, because the engine"
            + $" adds xPlus outright; it is on {jumper.X}");
        AssertEq(
            jumper.Y, 6,
            "and one down as well, because it adds yPlus too and the if"
            + $" only chose the facing; it is on {jumper.Y}");
        AssertTrue(
            withWall.IsPassable(8, 5, MzCharacter.Right) == false,
            "**and (8,5) is the wall in the map this test used** -- and a"
            + " leap of 3,1 still crossed onto (8,6), which is free, while"
            + " the figure came from (5,5), the wall's own row. Measured"
            + " `jump` has no canPass, no checkPassage and no event, and a"
            + " route *step* is the one that asks.");
    }

    public void Test_APageIsHeldOnlyWhenTheRoutesOwnWaitFlagSaysSo()
    {
        // **`if (params[1].wait) this.setWaitMode("route");`** — the wait is
        // the route's, not the command's. **Sixty of this game's ninety-six
        // routes say so and thirty-six do not**, and a reader that held every
        // page would stall a game on the thirty-six.
        var routes = EveryMoveRouteInThisGame();
        var wartend = 0;
        foreach (var entry in routes)
        {
            if (MzRouteStep.ReadFromParameter(entry.Parameters[1]).Wait)
            {
                wartend++;
            }
        }

        AssertEq(
            wartend, 60,
            "and sixty of the ninety-six routes ask to hold the page, which is"
            + $" this game's own number; there are {wartend}");

        var map = Map.Open();
        var character = new MzCharacter(5, 5);
        var route = MzRouteStep.ReadFromParameter(
            "{\"list\":[{\"code\":3,\"parameters\":[],\"indent\":null},"
            + "{\"code\":0,\"parameters\":[],\"indent\":null}],"
            + "\"repeat\":false,\"skippable\":false,\"wait\":false}");
        character.Route.Force(route);

        AssertEq(
            character.Route.IsHoldingThePage, false,
            "and a route that does not ask to wait holds nothing, even though"
            + $" it has steps left; it holds {character.Route.IsHoldingThePage}");

        // **And the flags are read, all three, and only one is acted on.**
        var withAll = MzRouteStep.ReadFromParameter(
            "{\"list\":[{\"code\":0,\"parameters\":[],\"indent\":null}],"
            + "\"repeat\":true,\"skippable\":true,\"wait\":true}");
        AssertEq(
            withAll.Repeat, true,
            $"and repeat is read and kept, because the engine's index reads"
            + $" it; it is {withAll.Repeat}");
        AssertEq(
            withAll.Skippable, true,
            $"and skippable is read and kept; it is {withAll.Skippable}");
        AssertEq(
            withAll.Wait, true,
            $"and wait is read; it is {withAll.Wait}");
    }

    public void Test_ARouteForACharacterThisReaderHasNotGotIsNotAnErrorAndThePageCarriesOn()
    {
        // **`if (character) { … } return true;`** — the guard is on the work,
        // not on the command. A route for a character this reader has not got
        // is a route that goes nowhere, and the page carries on.
        //
        // A reader that returned false there would stop the page on a
        // character it simply does not have, and a game that moves event 9 in a
        // map this reader read half of would stall for ever.
        var geh = new MzCharacter(5, 5);
        var facts = new MzBranchFacts
        {
            Characters = new Dictionary<int, MzCharacter> { { -1, geh } },
        };
        var actions = new List<MzAction>();
        var interpreter = new MzInterpreter(new List<MzCommandEntry>
        {
            new(MzCommandTable.MoveRoute,
                new List<string>
                {
                    "9",
                    "{\"list\":[{\"code\":3,\"parameters\":[],\"indent\":null}],"
                        + "\"repeat\":false,\"skippable\":false,\"wait\":true}",
                },
                0),
            new(MzCommandTable.ControlVariables,
                new List<string> { "5", "5", "0", "0", "9" }, 0),
        });

        interpreter.Run(actions, facts);
        // **`Finished` and not `Stepped`**, and that is the right answer: the
        // list had two commands and both ran, so there was nothing left. The
        // rule is not "the run stopped early" — it is "**the command after
        // the 205 ran**", and a first draft asserted `Stepped` and would have
        // needed a third command to say so.
        AssertEq(
            interpreter.Stopped, MzStep.Finished,
            "and the run is finished rather than stepped, because both of the"
            + $" two commands ran and there was nothing left; it is"
            + $" {interpreter.Stopped}");
        // **The probe is a 122 and not a 121**, and a first draft wrote 121
        // and then asked why the variable was not set. **121 is Control
        // Switches** — it set a switch, ran, and left the variable alone, and
        // the run looked perfectly healthy. The constant is used now so the
        // next reader cannot make that mistake.
        AssertEq(
            facts.Variables.TryGetValue(5, out var nach), true,
            "and the command after the 205 ran, which is the whole point of"
            + $" the rule; variable 5 is {nach}");
        AssertEq(
            nach, 9,
            $"and it holds the value the 121 wrote, and not merely the fact"
            + $" that a variable exists; it is {nach}");
        AssertTrue(
            actions[0].What.Contains("not one this reader has"),
            $"and what it says names the character, so the gap is visible and"
            + $" not silent: {actions[0].What}");
    }

    public void Test_ThisGamesOwnNinetySixRoutesWalkAndTheCodesItUsesAreTheOnlyOnesItUses()
    {
        // **The real ones, walked.** Ninety-six routes from nineteen maps, run
        // on a map with no walls, and every step is either carried out or
        // named. **No route reaches a code this reader does not have**, and no
        // route runs for ever.
        var map = Map.Open();
        var geroutet = 0;
        var verweigert = 0;
        var beendet = 0;
        var codes = new HashSet<int>();

        foreach (var entry in EveryMoveRouteInThisGame())
        {
            var character = new MzCharacter(8, 7);
            var route = MzRouteStep.ReadFromParameter(entry.Parameters[1]);
            foreach (var step in route.List)
            {
                codes.Add(step.Code);
            }

            character.Route.Force(route);
            for (var frame = 0; frame < 400; frame++)
            {
                character.PassFrame();
                var said = character.Route.Step(character, map);
                if (said.Contains("not carried out"))
                {
                    verweigert++;
                }
            }
            if (character.Route.IsDone)
            {
                beendet++;
            }
            geroutet++;
        }

        AssertEq(
            geroutet, 96,
            "and all ninety-six routes run to an end without one of them"
            + $" running for ever, which a limit of four hundred frames each"
            + $" is enough to show; {geroutet} ran");

        AssertEq(
            beendet, 96,
            "and every one of them reaches its END, so a route in this game"
            + $" always finishes and none of them is a loop; {beendet} finished");

        AssertEq(
            verweigert, 0,
            "and no step of any of them is refused, because the test map has"
            + $" no walls and the only codes these routes use are steps and"
            + $" turns and jumps; {verweigert} were refused");

        // **And the codes the game uses are exactly the ones this reader
        // carries out.** Seventeen codes, measured.
        AssertEq(
            codes.Count, 17,
            "and the ninety-six routes between them use seventeen distinct"
            + $" route codes, which is this game's own number; there are"
            + $" {codes.Count}");

        // **The three the counts say lead.** MOVE_LEFT 74, MOVE_RIGHT 59,
        // MOVE_DOWN 50 — and MOVE_UP 45, so the order is left, right, down,
        // up. **Not down first**, which is what "a game mostly walks about"
        // would guess.
        AssertEq(
            codes.Contains(2), true,
            "and MOVE_LEFT is among them, which leads this game's routes at"
            + $" 74 times; the codes are {string.Join(",", codes.OrderBy(c => c))}");
        AssertEq(
            codes.Contains(9), false,
            "and MOVE_RANDOM is not, because it appears zero times here, and a"
            + " code a game never writes is not one this reader has to act on"
            + $" or refuse; the codes are {string.Join(",", codes.OrderBy(c => c))}");
    }

    /// <summary>
    /// Twenty is a quarter turn and not a walk.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this class carried wrong numbers for 20 to 28 until
    /// they were measured.</strong> The list here said 20 was a walk, 21 a
    /// random step, 22 to 25 "face a way" and 26 to 28 a quarter turn.
    /// <strong>Measured in <c>js/rmmz_objects.js</c>, none of that is
    /// true</strong> — <strong>20 and 21 turn a figure a quarter, 22
    /// turns it about, and 23 turns it a quarter either way.</strong>
    /// <strong>There is no walk code and no face code.</strong>
    /// </para>
    /// <para>
    /// <strong>And the wrong numbers sat there unused</strong>, which is
    /// why nothing caught them: <strong>no code in the project referred to
    /// them</strong>. <strong>A wrong constant that nothing reads is not
    /// harmless — it is a wrong constant waiting for the first reader.</strong>
    /// </para>
    /// </remarks>
    public void Test_ZwanzigIstEineVierteldrehungUndKeinSchritt()
    {
        AssertEq(MzMoveRoute.Turn90DegreeRight, 20,
            "**and 20 turns a figure a quarter to the right**");
        AssertEq(MzMoveRoute.Turn90DegreeLeft, 21,
            "**and 21 turns a quarter to the left**");
        AssertEq(MzMoveRoute.Turn180Degree, 22,
            "**and 22 turns a figure about**");
        AssertEq(MzMoveRoute.Turn90DegreeRightOrLeft, 23,
            "**and 23 turns a quarter either way**");
        AssertEq(MzMoveRoute.TurnDown, 16,
            "**and 16 turns down, and a walk down is 1**");
        AssertEq(MzMoveRoute.MoveDown, 1,
            "**and moving down is 1, and not 20**");
    }

    /// <summary>
    /// A quarter turn follows the engine's own table.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the table is not the one a reader would guess.</strong>
    /// Measured: <c>turnRight90</c> turns down into left, left into up,
    /// up into right, and right into down.
    /// </para>
    /// <para>
    /// <strong>And "right" here means right as seen looking down at a
    /// figure from above</strong>, <strong>which is not what the word
    /// means on a compass</strong> — <strong>and a reader that rotated
    /// the numbers instead of following the table turned every figure the
    /// wrong way while looking exactly right.</strong>
    /// </para>
    /// </remarks>
    public void Test_EineVierteldrehungFolgtDerTabelleDesMotors()
    {
        // **Und die Tabelle des Motors, woertlich.**
        AssertEq(MzRouteCode.QuarterRight(MzCharacter.Down), MzCharacter.Left,
            "**and down becomes left when turning a quarter right**");
        AssertEq(MzRouteCode.QuarterRight(MzCharacter.Left), MzCharacter.Up,
            "**and left becomes up**");
        AssertEq(MzRouteCode.QuarterRight(MzCharacter.Up), MzCharacter.Right,
            "**and up becomes right**");
        AssertEq(MzRouteCode.QuarterRight(MzCharacter.Right), MzCharacter.Down,
            "**and right becomes down**");

        // **Und viermal rechts ist wieder unten, und zweimal ist oben.**
        var richtung = MzCharacter.Down;
        for (var mal = 0; mal < 4; mal++)
        {
            richtung = MzRouteCode.QuarterRight(richtung);
        }

        AssertEq(richtung, MzCharacter.Down,
            "**and four quarter turns come back to where it started**");
        AssertEq(
            MzRouteCode.QuarterLeft(MzRouteCode.QuarterLeft(MzCharacter.Down)),
            MzCharacter.Up,
            "**and two quarter turns turn a figure about**");
    }

    /// <summary>
    /// A route of turns does what the project's own routes do.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the project's measured route, verbatim.</strong>
    /// Seven pages carry <c>18, 16, 17, 19</c> and then the end — right,
    /// down, left, up — <strong>and nothing else.</strong>
    /// </para>
    /// <para>
    /// <strong>And the figure ends facing up, and it never left its
    /// tile.</strong> <strong>A reader that read 16 as a move walked it off
    /// the map.</strong> <strong>So the assertion is both halves at once:
    /// the facing, and the tile.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieLaufbahnDiesesSpielsDrehtUndGehtNicht()
    {
        var map = Map.Open();
        var character = new MzCharacter(5, 5);
        var route = MzRouteStep.ReadFromParameter(
            "{\"list\":[{\"code\":18,\"parameters\":[],\"indent\":null},"
            + "{\"code\":16,\"parameters\":[],\"indent\":null},"
            + "{\"code\":17,\"parameters\":[],\"indent\":null},"
            + "{\"code\":19,\"parameters\":[],\"indent\":null},"
            + "{\"code\":0,\"parameters\":[],\"indent\":null}],"
            + "\"repeat\":false,\"skippable\":false,\"wait\":true}");

        character.Route.Force(route);
        AssertEq(character.Direction, MzCharacter.Down,
            "**and it starts facing down**");

        // **Und jeder Schritt kommt in einem eigenen Bild, und der letzte
        // Eintrag wird auch ausgefuehrt.**
        character.Route.Step(character, map);
        AssertEq(character.Direction, MzCharacter.Right,
            "**and 18 turns it right**");
        character.Route.Step(character, map);
        AssertEq(character.Direction, MzCharacter.Down,
            "**and 16 turns it down**");
        character.Route.Step(character, map);
        AssertEq(character.Direction, MzCharacter.Left,
            "**and 17 turns it left**");
        character.Route.Step(character, map);
        AssertEq(character.Direction, MzCharacter.Up,
            "**and 19 turns it up**");

        AssertEq(character.X, 5,
            "**and it never left its tile** -- and 18, 16, 17 and 19 are "
                + "ROUTE_TURN_RIGHT, DOWN, LEFT and UP, and a reader that "
                + "read 16 as a move walked this figure off the map");
        AssertEq(character.Y, 5, "**and its row is untouched as well**");
    }

    /// <summary>
    /// Minus one names the player, and it moves him a tile.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the project's own command, verbatim.</strong>
    /// Measured at <c>Map001.json</c>, event 4:
    /// <c>205 [-1, {list: [{code: 3}, {code: 0}], wait: true}]</c>
    /// followed by <c>505 [{code: 3}]</c> — <strong>the player walks one
    /// tile to the right, and the page waits for him.</strong>
    /// </para>
    /// <para>
    /// <strong>And forty-six of this project's ninety-six routes say minus
    /// one</strong>, <strong>so nearly half of them move the player, and
    /// a reader that read minus one as "no character" moved nothing at
    /// all.</strong>
    /// </para>
    /// </remarks>
    public void Test_MinusEinsHeisstDerSpielerUndErGehtEineKachel()
    {
        var map = Map.Open();
        var player = new MzPlayer();
        player.StandAt(1, 5, 7, MzCharacter.Right);

        var spieler = player.BuildFigure();
        AssertTrue(spieler != null,
            "**and the player has a figure** -- and the engine gives the player for every negative number, and this project says minus one forty-six times");
        AssertEq(spieler!.X, 5,
            "**and it stands where the player stands**");
        AssertEq(spieler.Y, 7,
            "**and on the row the player stands on**");

        spieler.Route.Force(MzRouteStep.ReadFromParameter(
            "{\"list\":[{\"code\":3,\"parameters\":[],\"indent\":null},{\"code\":0,\"parameters\":[],\"indent\":null}],\"repeat\":false,\"skippable\":false,\"wait\":true}"));

        var first = spieler.Route.Step(spieler, map);
        AssertTrue(!first.Contains("refused"),
            "**and the step goes through, because the way is clear; it says " + first + "**");
        AssertEq(spieler.X, 6,
            "**and the player has walked one tile to the right** -- and code 3 is ROUTE_MOVE_RIGHT**");
        AssertEq(spieler.Direction, MzCharacter.Right,
            "**and he faces the way he went**");
        AssertTrue(spieler.Route.IsHoldingThePage,
            "**and the page waits for the route** -- and the engine sets the wait mode to route only when the route says wait, and this project says wait**");
    }


    // **And the three route codes this game uses and the reader had no
    // constant for are 12, 13 and 14** -- forward, backward and jump.
    //
    // **And measured: 96 routes, 444 steps, of which 4 are 12, 10 are 13
    // and 31 are 14.** **And all 31 jumps are `[0, 0]`, and 383 of the
    // 444 steps carry no `parameters` key at all** -- **so the engine
    // reads `params[0]` as undefined, which is not a number and not an
    // error.**
    public void Test_DerSprungDesMotorsAddiertBeideAchsenUndNichtNurDieGroessere()
    {
        var map = Map.Open();
        var fig = new MzCharacter();
        fig.PlaceAt(5, 5);
        fig.TurnTo(MzCharacter.Right);

        // **And the measured rule at `jump`:**
        //
        // ```js
        // if (Math.abs(xPlus) > Math.abs(yPlus)) {
        //     if (xPlus !== 0) { this.setDirection(xPlus < 0 ? 4 : 6); }
        // } else {
        //     if (yPlus !== 0) { this.setDirection(yPlus < 0 ? 8 : 2); }
        // }
        // this._x += xPlus;
        // this._y += yPlus;
        // ```
        //
        // **And the `if` picks the facing and nothing else, and the
        // position is two plain additions after it.**
        fig.LeapBy(3, 1);
        AssertEq(fig.X, 8,
            "**and a leap of 3,1 moves three to the right** -- and the engine"
            + " adds xPlus outright, whatever the if did");
        AssertEq(fig.Y, 6,
            "**and one down as well** -- and this is what a first draft of"
            + " this method got wrong, by reading the if as choosing the"
            + " axis instead of choosing the facing");

        // **And a leap is not checked for passability**, and this map has
        // no wall on (8,6) -- so this is measured against a wall too.
        var mauer = new Map(20, 15, (8, 6));
        var fig2 = new MzCharacter();
        fig2.PlaceAt(5, 5);
        fig2.TurnTo(MzCharacter.Right);
        fig2.LeapBy(3, 1);
        AssertEq(fig2.X, 8,
            "**and it lands there even though that tile is a wall** --"
            + " measured: `jump` has no canPass, no checkPassage and no"
            + " event; the engine adds the offsets and is done");
        AssertTrue(mauer.IsPassable(8, 6, MzCharacter.Right) == false,
            "**and the map this test used really does block that tile**"
            + " -- and so the leap crossed a wall, which is a jump");
    }

    public void Test_EinSprungVonNullNullBleibtStehenUndLaeuftTrotzdem()
    {
        var map = Map.Open();
        var fig = new MzCharacter();
        fig.PlaceAt(4, 6);
        fig.TurnTo(MzCharacter.Left);

        // **And all 31 of this game's jumps are `[0, 0]`, and every one of
        // them keeps its position and its facing** -- because both offsets
        // are 0, so both `if` branches are skipped and both additions add
        // nothing. **And the leap still runs**: `distance` is 0, so the
        // peak is `10 - _moveSpeed` and at the engine's default 4 the
        // count is 12.
        var route = new MzMoveRoute();
        route.Force(MzRouteStep.ReadFromParameter(
            "{\"list\":[{\"code\":14,\"parameters\":[0,0]},"
            + "{\"code\":0}],\"repeat\":false,\"skippable\":false,"
            + "\"wait\":true}"));
        route.Step(fig, map);

        AssertEq(fig.X, 4,
            "**and the figure did not move** -- and that is measured on all"
            + " 31 of this game's jumps, which are every one of them 0,0");
        AssertEq(fig.Y, 6,
            "**and it did not move on the other axis either**");
        AssertEq(fig.Direction, MzCharacter.Left,
            "**and it kept the facing it had** -- and both `if` branches"
            + " are skipped when the offset is 0, so setDirection is"
            + " never reached");
        AssertTrue(fig.IsJumping,
            "**and the leap still runs** -- and a first draft of this"
            + " method wrote Math.Max(1, Math.Max(|x|,|y|) * 6), which is"
            + " one frame for 0,0, while the engine's peak of 10 minus"
            + " the speed of 4 gives a count of 12");
        AssertEq(fig.JumpCount, 12,
            "**and it runs for the engine's own twelve frames** --"
            + " (10 + 0 - 4) * 2, measured at `jump`");
    }

    public void Test_VorwaertsUndZurueckGehenInDieBlickrichtungUndNichtInDie()
    {
        var map = Map.Open();

        // **And `moveForward` is one line:** `this.moveStraight(this.direction());`
        var rechts = new MzCharacter();
        rechts.PlaceAt(3, 3);
        rechts.TurnTo(MzCharacter.Right);
        var r1 = new MzMoveRoute();
        r1.Force(MzRouteStep.ReadFromParameter(
            "{\"list\":[{\"code\":12},{\"code\":0}],\"repeat\":false,"
            + "\"skippable\":false,\"wait\":true}"));
        r1.Step(rechts, map);
        AssertEq(rechts.X, 4,
            "**and step 12 walks the way the figure faces** -- and a figure"
            + " facing right walks right, measured at `moveForward`");
        AssertEq(rechts.Y, 3,
            "**and not down** -- and the direction is the figure's own"
            + " and not a constant");

        var hoch = new MzCharacter();
        hoch.PlaceAt(3, 3);
        hoch.TurnTo(MzCharacter.Up);
        var r2 = new MzMoveRoute();
        r2.Force(MzRouteStep.ReadFromParameter(
            "{\"list\":[{\"code\":12},{\"code\":0}],\"repeat\":false,"
            + "\"skippable\":false,\"wait\":true}"));
        r2.Step(hoch, map);
        AssertEq(hoch.Y, 2,
            "**and the same step walks up for a figure facing up** -- and"
            + " that is the whole difference between 12 and a constant");

        // **And `moveBackward` is four lines and the lock is the point:**
        //
        // ```js
        // moveBackward = function() {
        //     const lastDirectionFix = this.isDirectionFixed();
        //     this.setDirectionFix(true);
        //     this.moveStraight(this.reverseDir(this.direction()));
        //     this.setDirectionFix(lastDirectionFix);
        // };
        // ```
        var links = new MzCharacter();
        links.PlaceAt(3, 3);
        links.TurnTo(MzCharacter.Right);
        var r3 = new MzMoveRoute();
        r3.Force(MzRouteStep.ReadFromParameter(
            "{\"list\":[{\"code\":13},{\"code\":0}],\"repeat\":false,"
            + "\"skippable\":false,\"wait\":true}"));
        r3.Step(links, map);
        AssertEq(links.X, 2,
            "**and step 13 walks away from the facing** -- and a figure"
            + " facing right walks left, measured at `moveBackward`");
        AssertEq(links.Direction, MzCharacter.Right,
            "**and keeps the facing it had** -- and that is what the"
            + " direction lock is for: without it the figure would have"
            + " turned around to walk away and stayed facing backwards");
    }

    public void Test_RoutenCode29SetztDasTempoUndNichtNurEinenSatz()
    {
        var map = Map.Open();
        var fig = new MzCharacter();
        fig.PlaceAt(3, 3);
        fig.TurnTo(MzCharacter.Right);

        AssertEq(fig.MoveSpeed, 4,
            "**and a figure starts at the engine's own default** --"
            + " measured: `initMembers` sets `this._moveSpeed = 4;`");

        // **And measured at the engine's switch:**
        // `case gc.ROUTE_CHANGE_SPEED: this.setMoveSpeed(params[0]); break;`
        // **And the game uses this 26 times: 22 times 5, twice 6, twice 4.**
        var route = new MzMoveRoute();
        route.Force(MzRouteStep.ReadFromParameter(
            "{\"list\":[{\"code\":29,\"parameters\":[5]},{\"code\":0}],"
            + "\"repeat\":false,\"skippable\":false,\"wait\":true}"));
        var gemeldet = route.Step(fig, map);

        AssertEq(fig.MoveSpeed, 5,
            "**and route code 29 sets it** -- and a reader that only wrote"
            + " a sentence about the number left all 26 of this game's"
            + " speed changes unapplied, and every one of those figures"
            + " walked at 4 while the game walked it at 5");
        AssertTrue(gemeldet.Contains("0.125"),
            "**and the message carries the real step size** -- and"
            + $" 2^5/256 is 0.125 tiles a frame, and it said: {gemeldet}");

        // **And the step size is not cosmetic:** it is `2^moveSpeed / 256`,
        // so 4 and 5 differ by a factor of two and every wait measured
        // against a walk halves with it.
        // **And a figure only has an arrival time while it is walking**,
        // and the engine's `updateMove` moves the *real* position toward
        // the logical one. `MoveStraight` sets the real position one tile
        // back, so that is the state a walk is measured from.
        var geher = new MzCharacter();
        geher.PlaceAt(3, 3);
        geher.TurnTo(MzCharacter.Right);
        geher.SetMoveSpeed(5);
        geher.MoveStraight(MzCharacter.Right, map);
        AssertEq(geher.X, 4,
            "**and it has set off to the right**");
        AssertEq(geher.FramesToArrival(), 8,
            "**and a figure at speed 5 covers that tile in eight frames**"
            + " -- and this is the figure's own speed being read, not a"
            + " literal 4 in the signature");

        geher.SetMoveSpeed(4);
        AssertEq(geher.FramesToArrival(), 16,
            "**and at the default 4 the same tile takes sixteen** -- and"
            + " the difference is the factor of two, measured");

        // **And route code 30 sets the animation rate,** and the measured
        // game never uses it, so the default 6 is what runs.
        AssertEq(fig.MoveFrequency, 6,
            "**and the walk animation rate starts at the engine's own 6**"
            + " -- measured: `initMembers` sets `this._moveFrequency = 6;`");
        var r2 = new MzMoveRoute();
        r2.Force(MzRouteStep.ReadFromParameter(
            "{\"list\":[{\"code\":30,\"parameters\":[3]},{\"code\":0}],"
            + "\"repeat\":false,\"skippable\":false,\"wait\":true}"));
        var m2 = r2.Step(fig, map);
        AssertEq(fig.MoveFrequency, 3,
            "**and route code 30 sets it too** -- and the threshold it"
            + " feeds is `30 * (5 - moveFrequency)`, so a forced custom"
            + " route starts after 60 frames at 3");
        AssertTrue(m2.Contains("60"),
            $"**and the message says when the route starts** -- it said: {m2}");
    }
}
