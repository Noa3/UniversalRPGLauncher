using System.Collections.Generic;
using System.Linq;
using Godot;
using UniversalRPG.Tests.Framework;
using UniversalRPG.Web;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// What 231, 232 and 235 do to what is on the screen.
/// </summary>
/// <remarks>
/// <para>
/// K-126 changed what the party carries. <b>A picture is the next thing this
/// game's map actually asks for</b>: nine across the one map in the fixture, on
/// images 1, 86 and 87, and four of them are moved rather than shown. They are
/// the first commands in this game that need something other than numbers to
/// have an effect.
/// </para>
/// <para>
/// Five rules, and each is a place a first reading gets it wrong:
///
/// <list type="number">
/// <item><b>A shown picture replaces the old one entirely.</b> The engine makes
/// a new object, so a tint or a movement is gone with the old one.</item>
/// <item><b>A picture id is routed through <c>realPictureId</c>.</b> In a
/// battle a map picture and a battle picture share the editor's number, and
/// <b>this game sets <c>picturesUpperLimit</c> to 110</b>, not the hundred the
/// engine falls back on.</item>
/// <item><b>The fourth parameter says where the fifth and sixth are read
/// from</b> — a number in the event, or a variable to look up.</item>
/// <item><b>A move sets a target, not a value</b>, and a move of zero frames
/// changes nothing at all.</item>
/// <item><b>A move on a slot with nothing in it does nothing</b> and is
/// recorded, because a game that moved a picture it never showed has a reason
/// a log should hold.</item>
/// </list>
///
/// And one that is invisible in the middle of the range: <b>only a move that
/// asks to wait holds the list up</b>, and this game asks for the wait on two
/// of its four moves and not on the other two.
/// </para>
/// </remarks>
partial class TestMzScreen : TestBase
{
    private const string FixtureRoot = "res://tests/fixtures/mz";

    private static List<MzCommandEntry> PictureCommandsInThisGame()
    {
        var found = new List<MzCommandEntry>();
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
                foreach (var command in page.Member("list")!.Items)
                {
                    var entry = MzCommandEntry.From(command);
                    if (entry.Code == MzCommandTable.ShowPicture
                        || entry.Code == MzCommandTable.MovePicture
                        || entry.Code == MzCommandTable.ErasePicture)
                    {
                        found.Add(entry);
                    }
                }
            }
        }
        return found;
    }

    public void Test_TheGameSetsAHundredAndTenPicturesAndNotTheHundredTheEngineFallsBackOn()
    {
        // `maxPictures` is
        //   if ("picturesUpperLimit" in $dataSystem.advanced)
        //       return $dataSystem.advanced.picturesUpperLimit;
        //   else
        //       return 100;
        // **and this game's System.json sets it to 110.** A reader that used
        // the hundred would tell a battle picture from a map one by a hundred
        // instead of a hundred and ten, and would put the second on top of the
        // first where the game keeps them apart.
        var path = FixtureRoot.PathJoin("data").PathJoin("System.json");
        var system = MzDataFile.Read(
            "System.json", Godot.FileAccess.GetFileAsBytes(path));
        var written = (int)system.Root.Member("advanced")!
            .Member("picturesUpperLimit")!.Number;

        AssertEq(
            written, 110,
            "and the game writes a hundred and ten, so a reader that used the"
            + $" hundred would be wrong about every battle picture; it writes"
            + $" {written}");

        var battle = new MzScreen(written, pInBattle: true);
        AssertEq(
            battle.RealPictureId(0), 110,
            "and a battle picture starts a hundred and ten above the map one,"
            + $" so picture 0 lives at {battle.RealPictureId(0)}");
        AssertEq(
            battle.RealPictureId(86), 196,
            $"and picture 86, which this game uses, lives at 196; it lives at"
            + $" {battle.RealPictureId(86)}");

        var map = new MzScreen(written, pInBattle: false);
        AssertEq(
            map.RealPictureId(86), 86,
            "while a map picture is where the number says, because"
            + $" realPictureId only adds in a battle; it is at"
            + $" {map.RealPictureId(86)}");

        // And the two never meet, which is the whole of the rule.
        map.Show(86, "UI/Get_Exp_Background", 0, 1088, 72, 85, 80, 100, 0);
        battle.Show(86, "UI/Get_Exp_Background", 0, 1088, 72, 85, 80, 100, 0);
        AssertEq(
            map.At(86) != null && map.At(196) == null, true,
            "and a map picture and a battle picture of the same number are two"
            + " pictures, which is what the offset is for");
        AssertEq(
            battle.At(196) != null && battle.At(86) == null, true,
            "and the battle screen holds its own copy and not the map's");
    }

    public void Test_ShowingAPictureTwiceThrowsTheOldOneAwayWholesale()
    {
        // `showPicture` is
        //   const realPictureId = this.realPictureId(pictureId);
        //   const picture = new Game_Picture();
        //   picture.show(...);
        //   this._pictures[realPictureId] = picture;
        // — **a new object, and the old one is gone with it.** A reader that
        // changed the existing picture in place would keep a tint, a rotation
        // and a movement the engine has just discarded, and a game that shows
        // the same slot twice with different pictures would show the first one
        // still moving.
        var screen = new MzScreen(110);
        screen.Show(86, "UI/Get_Exp_Background", 0, 1088, 72, 85, 80, 100, 0);
        screen.Move(86, 0, 988, 72, 85, 80, 220, 0, 20, 2, false, out _);
        AssertEq(
            screen.At(86).Duration, 20,
            "and a movement is under way on the first picture; it has"
            + $" {screen.At(86).Duration} frames left");

        var said = screen.Show(87, "UI/Get_Exp_Forground", 0, 1095, 77, 100, 100, 100, 0);

        AssertEq(
            screen.At(86).Duration, 20,
            "and showing a different slot does not touch the first, because the"
            + $" engine keeps them apart; it has {screen.At(86).Duration}");

        // Now the same slot, which is where the replacement matters.
        screen.Show(86, "UI/Get_Exp_Forground", 0, 10, 20, 100, 100, 255, 1);
        var after = screen.At(86);
        AssertEq(
            after.Duration, 0,
            "and showing the same slot again leaves a picture that is not"
            + $" moving, because initTarget takes the duration to zero; it has"
            + $" {after.Duration}");
        AssertEq(
            after.Name, "UI/Get_Exp_Forground",
            $"and the new name is the one that is there; it is {after.Name}");
        AssertTrue(
            said.Contains("87"),
            $"and what a log says names the slot it was given: {said}");
    }

    public void Test_APlaceIsReadFromTheEventOrFromAVariableAndTheFourthParameterSaysWhich()
    {
        // `picturePoint` is
        //   if (params[3] === 0) { point.x = params[4]; point.y = params[5]; }
        //   else { point.x = $gameVariables.value(params[4]);
        //          point.y = $gameVariables.value(params[5]); }
        // **so the fourth parameter decides where the other two are read
        // from.** A first draft read them as numbers either way, and a game
        // that places a picture from a variable would have put it at the
        // variable's own number rather than at what it holds.
        var facts = new MzBranchFacts();
        facts.Variables[40] = 640;
        facts.Variables[41] = 360;
        var screen = new MzScreen(110);

        // Kind 0: the fifth and sixth are the numbers.
        var literal = MzScreenCommands(Show(1, "UI/A", 0, 0, 320, 200, 100, 100, 255, 0));
        var a = new MzInterpreter(literal);
        a.Run(new List<MzAction>(), facts);
        AssertEq(
            facts.Screen.At(1).X, 320,
            "and a kind of zero places the picture at the numbers written, not"
            + $" at any variable's number; it is at {facts.Screen.At(1).X}");

        // Kind 1: the fifth and sixth name variables.
        var fromVariable = MzScreenCommands(Show(2, "UI/B", 0, 1, 40, 41, 100, 100, 255, 0));
        var b = new MzInterpreter(fromVariable);
        b.Run(new List<MzAction>(), facts);
        AssertEq(
            facts.Screen.At(2).X, 640,
            "and a kind of one places it at what the variables hold, which is"
            + $" {facts.Screen.At(2).X} rather than 40");
        AssertEq(
            facts.Screen.At(2).Y, 360,
            $"and the same for the other coordinate; it is at"
            + $" {facts.Screen.At(2).Y}");

        // A variable that was never set is worth nothing, as
        // `$gameVariables.value` of a variable that was never set is 0.
        var unset = MzScreenCommands(Show(3, "UI/C", 0, 1, 998, 999, 100, 100, 255, 0));
        var c = new MzInterpreter(unset);
        c.Run(new List<MzAction>(), facts);
        AssertEq(
            facts.Screen.At(3).X, 0,
            "and a variable that was never set places the picture at zero, as"
            + $" the engine's value() is; it is at {facts.Screen.At(3).X}");
    }

    public void Test_AMoveSetsATargetAndAMoveOfNoFramesChangesNothing()
    {
        // `move` writes `_targetX` and friends and `_duration`, and
        // `updateMove` only moves while `this._duration > 0`. **So a move of
        // zero frames changes nothing at all** — the picture is still where it
        // was and the target is never reached, because the duration is already
        // out. A reader that copied the numbers straight over would snap the
        // picture to the target, and a game that uses a move of no frames to
        // set something without moving it would have it jump instead.
        var screen = new MzScreen(110);
        screen.Show(86, "UI/Get_Exp_Background", 0, 1088, 72, 85, 80, 100, 0);

        // pPictureId=86, origin=0, x=988, y=72, scaleX=85, scaleY=80,
        // opacity=0, blend=0, duration=0, easing=0, wait=true
        var said = screen.Move(
            86, 0, 988, 72, 85, 80, 0, 0, 0, 0, true, out var frames);

        AssertEq(
            screen.At(86).X, 1088,
            "and the picture is still where it was; it is at"
            + $" {screen.At(86).X}, not at {screen.At(86).TargetX}");
        AssertEq(
            screen.At(86).Duration, 0,
            "and it is not moving, because a move of no frames is not a"
            + $" movement; it has {screen.At(86).Duration} frames left");
        AssertEq(
            frames, 0,
            "and a caller is asked to wait for no frames, because the engine's"
            + $" `this.wait(params[10])` is then over at once; it is {frames}");
        AssertTrue(
            said.Contains("still at"),
            $"and what it says says the picture did not move, so a log shows it:"
            + $" {said}");
    }

    public void Test_AMoveOnAPictureThatIsNotThereChangesNothingAndSaysWhich()
    {
        // `movePicture` is
        //   const picture = this.picture(pictureId);
        //   if (picture) { picture.move(...); }
        // — so a move on an empty slot does nothing. **This reader records
        // it**, because a game that moves a picture it never showed has a
        // reason a log should hold, and a silent nothing is what this
        // repository keeps refusing to produce.
        var screen = new MzScreen(110);

        var said = screen.Move(
            86, 0, 988, 72, 85, 80, 220, 0, 20, 2, false, out var frames);

        AssertEq(
            frames, 0,
            "and no frames are asked for, because nothing is moving; it asks"
            + $" for {frames}");
        AssertEq(
            screen.Pictures.Count(), 0,
            "and the screen is still empty; it holds"
            + $" {screen.Pictures.Count()} pictures");
        AssertEq(
            screen.Notices.Count, 1,
            "and it is recorded once, because a move that could not happen is"
            + $" not the same as one that did nothing; there are"
            + $" {screen.Notices.Count}");
        AssertTrue(
            screen.Notices[0].Contains("86"),
            $"and the notice names the slot the game asked for: "
            + $"{screen.Notices[0]}");
    }

    public void Test_OnlyAMoveThatAsksToWaitHoldsTheListUp()
    {
        // `command232` ends with
        //   if (params[11]) { this.wait(params[10]); }
        // **and there is no second one** — a reader that waited on every move
        // would stall a game that only asked to wait on two of its four.
        // Measured on this game: two of its four moves carry a true and two
        // carry a false.
        var waits = new List<bool>();
        foreach (var command in PictureCommandsInThisGame())
        {
            if (command.Code != MzCommandTable.MovePicture)
            {
                continue;
            }
            // **A parameter the game left empty is not a number to parse.**
            // This game's own 232s carry a true and a false in the eleventh
            // slot, and a first draft parsed the string and threw on a slot
            // that is empty. `params[11]` is read as a truth value, so an
            // empty string is false — the same as a "0" and the same as a
            // missing key on a hash.
            waits.Add(
                command.Parameters[11] == "1"
                || string.Equals(
                    command.Parameters[11], "true",
                    System.StringComparison.OrdinalIgnoreCase));
        }

        AssertEq(
            waits.Count, 4,
            "and the map read here has four moves, which is what the fixture"
            + $" carries; it has {waits.Count}");
        // `TrueCount` and `FalseCount` are .NET 9 additions, and a reader
        // that reached for them would be pinned to a runtime this project does
        // not require. `Count(w => w)` says the same thing and works
        // everywhere, and the sum below is the part that matters: **the two
        // states add up to all four**, so neither is being read as the other.
        var asking = waits.Count(w => w);
        var notAsking = waits.Count(w => !w);
        AssertEq(
            asking, 2,
            "two of which ask to wait and two of which do not, which is the"
            + $" whole of the rule; {asking} ask");
        AssertEq(
            asking + notAsking, 4,
            "and none of the two states is being read as the other, because"
            + $" they add up to all four; they add up to {asking + notAsking}");

        // And the reader asks for frames on exactly those two.
        var screen = new MzScreen(110);
        screen.Show(86, "UI/Get_Exp_Background", 0, 1088, 72, 85, 80, 100, 0);
        var asked = screen.Move(
            86, 0, 988, 72, 85, 80, 220, 0, 20, 2, true, out var withWait);
        AssertEq(
            withWait, 20,
            "so a move that asks to wait holds the list for its own length,"
            + $" which is {withWait}");
        AssertTrue(
            asked.Contains("waiting"),
            $"and says it is waiting, so a caller can see why it stopped;"
            + $" {asked}");

        screen.Move(86, 0, 995, 77, 100, 100, 255, 0, 20, 2, false, out _);
        AssertEq(
            screen.At(86).Duration, 20,
            "while one that does not still moves — it is a movement either way,"
            + $" so the picture has {screen.At(86).Duration} frames to go — and"
            + " it only does not hold the list up");
    }

    public void Test_EveryPictureCommandInThisGameRunsAndLeavesWhatTheEngineLeaves()
    {
        // **The nine commands in the one map, walked with the game's own
        // numbers.** Five show a picture, four move one and two erase one, and
        // the claim is that the screen ends up holding what the engine would
        // hold: nothing where a picture was erased, and a picture with the
        // game's own name, place, scale and opacity where one was shown.
        var commands = PictureCommandsInThisGame();
        AssertEq(
            commands.Count, 9,
            "and the map read here holds nine of them, which is what the"
            + $" fixture carries; it holds {commands.Count}");

        var shown = 0;
        var moved = 0;
        var erased = 0;
        var waits = 0;
        foreach (var command in commands)
        {
            switch (command.Code)
            {
                case MzCommandTable.ShowPicture:
                    shown++;
                    break;
                case MzCommandTable.MovePicture:
                    moved++;
                    if (command.Parameters[11] == "1"
                        || string.Equals(
                            command.Parameters[11], "true",
                            System.StringComparison.OrdinalIgnoreCase))
                    {
                        waits++;
                    }
                    break;
                case MzCommandTable.ErasePicture:
                    erased++;
                    break;
            }
        }

        AssertEq(
            shown, 3,
            "of which three show a picture; they do" + string.Empty);
        AssertEq(
            moved, 4,
            $"and four move one; {moved} do");
        AssertEq(
            erased, 2,
            $"and two erase one; {erased} do");
        AssertEq(
            waits, 2,
            "and two of the four moves ask to wait, which is the number the"
            + $" test above counts from the game's own list; {waits} do");
    }

    public void Test_TheGamesOwnPictureCommandsRunThroughTheInterpreterAndLeaveTheScreenWhereTheEngineLeavesIt()
    {
        // **The nine commands, actually run, through the reader a caller uses.**
        // A first draft counted them — "three show, four move, two erase" —
        // and called that a walk. It was not: it never built an interpreter
        // and never read a parameter, so a command that changed the wrong
        // screen, or a move that stopped the list when the game did not ask
        // it to, or a 231 that read its place from the wrong slot, would all
        // have passed. Counting a list is not running it.
        //
        // And the two moves that ask to wait **stop the list**, so this walk
        // hands back one frame at a time the way a caller would, and the
        // claim is that it arrives at the end and the screen holds what the
        // engine holds.
        var commands = PictureCommandsInThisGame();
        var screen = new MzScreen(110);
        var facts = new MzBranchFacts { Screen = screen };
        var actions = new List<MzAction>();

        // **`Run` is one frame's worth, not the whole list.** A first draft
        // called it once and then counted frames, and it stopped on the first
        // move that asked to wait with the index still on that command — five
        // actions for nine commands. `Run` returns as soon as a command is not
        // done, and a caller goes back to it once the frames are counted off,
        // which is what the engine's own `update` does.
        var interpreter = new MzInterpreter(commands);
        var frames = 0;
        var guard = 0;

        while (interpreter.IsRunning && guard < 500)
        {
            // **The frame comes before the command, not after it.** The
            // engine's `Game_Interpreter.prototype.update` is
            //   while (this.isRunning() && !this._waitCount) { this.executeCommand(); }
            //   if (this.isRunning() && this._waitCount > 0) { this._waitCount--; }
            //   this.updateChild();
            // — so a wait is counted off at the START of a frame, and the
            // command that set it is not read again until the next one. A
            // first draft counted frames after `Run` and waited for ever,
            // because every `Run` set the wait again from the same index.
            // `PassFrame` says whether the wait is OVER, not whether one is
            // running — `return true` on a count that has reached zero. A
            // first draft read it the other way round, which is the same
            // mistake as calling it after `Run`.
            if (interpreter.WaitFrames > 0)
            {
                interpreter.PassFrame();
                frames++;
                guard++;
                // The picture is on the same clock, and the engine ticks both
                // in the same frame.
                screen.PassFrame();
                continue;
            }

            interpreter.Run(actions, facts);
            guard++;
        }

        // `Run` leaves `Stopped` where the last frame left it, so a run whose
        // last frame was a counted-off wait ends at `Stepped` rather than at
        // `Finished`. **What matters is that it is not still waiting** and
        // that the index is at the end.
        AssertEq(
            interpreter.Index, interpreter.Commands.Count,
            "and the list ran to its end rather than stopping on the two moves"
            + $" that ask to wait, because a caller hands it the frames;"
            + $" it stopped {interpreter.Stopped} at index"
            + $" {interpreter.Index} of {interpreter.Commands.Count} after"
            + $" {frames} frames, with {actions.Count} actions");

        // **What the engine leaves: nothing where a picture was erased, and a
        // picture with the game's own name, place, scale and opacity where one
        // was shown.** Event 5 erases 86 and 87 at the end, so a run that ends
        // with two empty slots and the one from event 1 is the engine's own
        // answer and not a reader's.
        AssertEq(
            screen.Has(86), false,
            "and picture 86 is not on the screen, because this game's last two"
            + $" commands erase it; it is there: {screen.Has(86)}");
        AssertEq(
            screen.Has(87), false,
            $"and neither is 87, which is erased in the same pair; it is"
            + $" there: {screen.Has(87)}");

        // Which pictures the run touched at all, from the actions it recorded.
        // A minimal check first: one show, one interpreter, one list.
        var shown = new List<string>();
        var codes = new List<int>();
        foreach (var action in actions)
        {
            codes.Add(action.Code);
            if (action.What.Contains("shown"))
            {
                shown.Add($"{action.Code}:{action.What}");
            }
        }

        // Event 1 showed picture 1 and never moved or erased it.
        var first = screen.At(1);
        AssertTrue(
            first != null,
            "and picture 1 is still on the screen, because nothing in this map"
            + $" takes it away; the screen holds {screen.Pictures.Count()}"
            + $" pictures");
        AssertEq(
            first.Name, "UI/Status_HelpCollision",
            "with the game's own file name, and not a name the reader made up;"
            + $" it is {first.Name}");
        // **The place is zero, and the scale is the two thousand.** A first
        // draft read the two thousand out of the scale and put it in both the
        // place and the scale, and would have shown the picture off the
        // bottom left of the screen instead of at its corner. The event says
        // `1, "UI/Status_HelpCollision", 0, 0, 0, 0, 2000, 2000, 255, 0`:
        // a kind of zero, a place of nothing, and a picture two thousand
        // times its own size.
        AssertEq(
            first.X, 0,
            "at the x the event wrote, which is nothing and not the scale the"
            + $" same line carries; it is at {first.X}");
        AssertEq(
            first.Y, 0,
            $"and the same for the y; it is at {first.Y}");
        AssertEq(
            first.ScaleX, 2000,
            "and the scale the event wrote, which this reader does not"
            + $" resolve to a rendering; it is {first.ScaleX}");
        AssertEq(
            first.Opacity, 255,
            $"and the opacity; it is {first.Opacity}");
        AssertEq(
            first.BlendMode, 0,
            "and the blend mode the event wrote, kept as the number the game"
            + $" stored; it is {first.BlendMode}");
        AssertEq(
            first.Duration, 0,
            "and it is not moving, because the event that shows it does not"
            + $" move it and initTarget takes the duration to zero; it has"
            + $" {first.Duration} frames left");

        // And every action the run recorded is one a caller can read.
        AssertTrue(
            actions.Count >= 9,
            "and the run recorded what it did, so a caller can see it; it"
            + $" recorded {actions.Count} actions for nine commands."

            + $" Taken-frames={frames} Guard={guard}");
    }

    public void Test_AMoveThatAsksToWaitHoldsTheListUpForAsManyFramesAsItTakes()
    {
        // **The wait itself, and not only what the screen looks like at the
        // end.** The first draft of the walk-through drove the screen's frames
        // from the test's own loop, so a reader that never set a wait still
        // moved the picture and ended in the same place — the mutation that
        // removes `pInterpreter.Wait(frames)` passed. It is equivalent for the
        // picture and wrong for the page: without the wait the four commands
        // after the move run in the same frame, and a game that fades a
        // picture out over twenty frames would run the rest of the event while
        // it is still at full opacity.
        //
        // So the claim is made about frames: **the interpreter is held for
        // exactly as long as the movement takes, and it is released when they
        // are counted off.**
        var screen = new MzScreen(110);
        var facts = new MzBranchFacts { Screen = screen };
        var actions = new List<MzAction>();

        // One show, one move that asks to wait for twenty, and one command
        // that has to be told apart from the move.
        var commands = new List<MzCommandEntry>
        {
            new(MzCommandTable.ShowPicture, new List<string>
            {
                "86", "UI/Get_Exp_Background", "0", "0", "1088", "72",
                "85", "80", "100", "0",
            }, 0),
            new(MzCommandTable.MovePicture, new List<string>
            {
                "86", "0", "0", "0", "988", "72", "85", "80", "220", "0",
                "20", "true", "2",
            }, 0),
            // 122 is `startId, endId, operationType, operandType, operand`,
            // and **the operand is the fifth parameter** — a first draft wrote
            // four and the value was nowhere to be read from.
            new(122, new List<string> { "1", "1", "0", "0", "5" }, 0),
            new(0, new List<string>(), 0),
        };

        var interpreter = new MzInterpreter(commands);
        interpreter.Run(actions, facts);

        // **After one frame of running, the move has run and the page is held.**
        AssertEq(
            interpreter.Stopped, MzStep.Waiting,
            "and the page is held, because a move that asks to wait stops the"
            + $" run where it stands; it is {interpreter.Stopped}");
        AssertEq(
            interpreter.WaitFrames, 20,
            "for the length of the movement, which is what the engine waits"
            + $" for; it is waiting {interpreter.WaitFrames}");
        AssertEq(
            interpreter.Index, 2,
            "and the index is past the move, on the command after it, because a"
            + $" command that ran is stepped over; it is at {interpreter.Index}");
        AssertEq(
            facts.Variables.Count, 0,
            "and the command after the move has not run, because the page is"
            + $" still held; the facts hold {facts.Variables.Count} variables");

        // **And it is released only when the frames are counted off**, not
        // before and not after.
        var released = 0;
        while (interpreter.WaitFrames > 0 && released < 100)
        {
            interpreter.PassFrame();
            screen.PassFrame();
            released++;
        }

        AssertEq(
            released, 20,
            "and it takes exactly the twenty frames the move asked for, not"
            + $" nineteen and not twenty-one; it took {released}");

        interpreter.Run(actions, facts);
        AssertEq(
            facts.Variables.TryGetValue(1, out var after), true,
            "and then the command after the move runs, which is the whole of"
            + $" what the wait was for; variable 1 is {after}");
    }

    public void Test_AMoveThatDoesNotAskToWaitRunsOnInTheSameFrame()
    {
        // **The other side of the rule, and the one that needed its own test.**
        // The wait test proves a move that asks to wait stops the page. This
        // proves the other two of this game's four do not, and it does it by
        // counting frames rather than by looking at the end: a move that does
        // not ask to wait leaves the picture in motion and the page running.
        //
        // A reader that waited on every move would stop the page for twenty
        // frames on a command the engine runs straight through, and the game
        // would pause four times on a page it wrote to run in one go.
        var screen = new MzScreen(110);
        var facts = new MzBranchFacts { Screen = screen };
        var actions = new List<MzAction>();

        var commands = new List<MzCommandEntry>
        {
            new(MzCommandTable.ShowPicture, new List<string>
            {
                "86", "UI/Get_Exp_Background", "0", "0", "1088", "72",
                "85", "80", "100", "0",
            }, 0),
            // A move of twenty frames that does NOT ask to wait — the shape
            // this game uses twice.
            new(MzCommandTable.MovePicture, new List<string>
            {
                "86", "0", "0", "0", "988", "72", "85", "80", "220", "0",
                "20", "false", "2",
            }, 0),
            new(122, new List<string> { "1", "1", "0", "0", "5" }, 0),
            new(0, new List<string>(), 0),
        };

        var interpreter = new MzInterpreter(commands);
        interpreter.Run(actions, facts);

        // **The whole list ran in the first frame**, which is the claim: no
        // wait was asked for, so nothing held the page.
        AssertEq(
            interpreter.Index, interpreter.Commands.Count,
            "and the list is through in one frame, because a move that does not"
            + " ask to wait holds nothing up; the index is at"
            + $" {interpreter.Index} of {interpreter.Commands.Count}");
        AssertEq(
            interpreter.WaitFrames, 0,
            "and nothing is being waited for, because the eleventh parameter"
            + $" said false; it is waiting {interpreter.WaitFrames}");
        AssertEq(
            interpreter.Stopped, MzStep.Finished,
            $"and the run is finished rather than waiting; it is"
            + $" {interpreter.Stopped}");
        AssertTrue(
            facts.Variables.ContainsKey(1),
            "and the command after the move has already run, in the same"
            + " frame, which is the whole difference between the two shapes");

        // **And the picture is still moving** — the wait was never what made
        // it move, and a reader that tied the two together would have it
        // standing still.
        AssertEq(
            screen.At(86).Duration, 20,
            "and the picture is still on its way, because a movement is a"
            + $" movement whether or not the game asked to wait for it; it has"
            + $" {screen.At(86).Duration} frames to go");
    }

    public void Test_AnEmptyParameterIsFalseAndASlotTheGameLeftOutIsFalse()
    {
        // **`if (params[11])` is a truth value, and "not empty" is not one.**
        // A first draft read the truth as `written != ""`, which is not the
        // same rule: a parameter the game wrote as anything that is not the
        // string "1" and not the word "true" is false, and a reader that read
        // it as "not empty" would treat a stray character as a request to
        // wait. RPG Maker writes a boolean as `true` or `false` and may leave
        // the slot out entirely, so the three shapes below are the three the
        // engine can meet.
        foreach (var written in new[] { "true", "1" })
        {
            var asking = ScreenCommandsWithWait(written);
            var askFacts = new MzBranchFacts { Screen = new MzScreen(110) };
            var askInterpreter = new MzInterpreter(asking);
            askInterpreter.Run(new List<MzAction>(), askFacts);
            AssertEq(
                askInterpreter.WaitFrames, 20,
                $"and \"{written}\" asks to wait, as the engine's truth value"
                + $" does; it is waiting {askInterpreter.WaitFrames}");
        }

        foreach (var written in new[] { "false", "", "0", "no" })
        {
            var notAsking = ScreenCommandsWithWait(written);
            var quietFacts = new MzBranchFacts { Screen = new MzScreen(110) };
            var quietInterpreter = new MzInterpreter(notAsking);
            quietInterpreter.Run(new List<MzAction>(), quietFacts);
            AssertEq(
                quietInterpreter.WaitFrames, 0,
                $"and \"{written}\" does not ask to wait, because a truth value"
                + $" is the two things the engine writes and not the absence of"
                + $" them; it is waiting {quietInterpreter.WaitFrames}");
        }
    }

    private static List<MzCommandEntry> ScreenCommandsWithWait(string pWait) =>
        new()
        {
            new(MzCommandTable.ShowPicture, new List<string>
            {
                "86", "UI/Get_Exp_Background", "0", "0", "1088", "72",
                "85", "80", "100", "0",
            }, 0),
            new(MzCommandTable.MovePicture, new List<string>
            {
                "86", "0", "0", "0", "988", "72", "85", "80", "220", "0",
                "20", pWait, "2",
            }, 0),
            new(0, new List<string>(), 0),
        };

    private static MzCommandEntry Show(
        int pId, string pName, int pOrigin, int pPointKind,
        int pX, int pY, int pScaleX, int pScaleY, int pOpacity, int pBlend) =>
        new(MzCommandTable.ShowPicture, new List<string>
        {
            pId.ToString(),
            pName,
            pOrigin.ToString(),
            pPointKind.ToString(),
            pX.ToString(),
            pY.ToString(),
            pScaleX.ToString(),
            pScaleY.ToString(),
            pOpacity.ToString(),
            pBlend.ToString(),
        }, 0);

    private static List<MzCommandEntry> MzScreenCommands(MzCommandEntry pCommand) =>
        new() { pCommand, new MzCommandEntry(0, new List<string>(), 0) };
}
