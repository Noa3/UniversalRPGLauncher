using System.Collections.Generic;
using Godot;
using UniversalRPG.Tests.Framework;
using UniversalRPG.Web;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Every command of this generation, named the way the engine names it.
/// </summary>
/// <remarks>
/// <para>
/// The names were read out of the engine's own source of a real game, from the
/// comment each method carries: the editor names a command after the method the
/// engine dispatches it to, and the two are the same name. Nothing here comes
/// from a page someone wrote about the engine.
/// </para>
/// <para>
/// That matters more than it sounds, because the numbering is the generation's
/// own. In the one before it a command's number is its own value times a
/// thousand; here the numbers run from 101 to 603 with nothing multiplied by
/// anything, so a reader written for that generation and pointed at this game's
/// files divides every command to zero.
/// </para>
/// </remarks>
partial class TestMzCommandTable : TestBase
{
    private const string FixtureRoot = "res://tests/fixtures/mz";

    public void Test_EveryCommandOfThisGenerationIsNamed()
    {
        // 114 is the count the engine's own source carries, measured rather than
        // remembered: a table with fewer names is missing commands a real game
        // uses, and a table with more is carrying names the engine does not have.
        AssertEq(
            MzCommandTable.Count, 114,
            "every command the engine dispatches is named, and no more");
    }

    public void Test_TheCommandsThisGamesEventsActuallyUseAreAllNamed()
    {
        // A table can name 114 commands and still miss the ones a game uses. The
        // commands come out of this game's own maps, and every one of them is
        // either a command the engine dispatches or a number a command above it
        // reads as its own data. Nothing else may appear.
        var used = CommandsInTheGame();
        AssertTrue(used.Count > 0, $"the game uses {used.Count} numbers in its"
            + " event commands");

        var unknown = new List<string>();
        foreach (var code in used)
        {
            if (MzCommandTable.KindOf(code) == MzCommandKind.Unknown)
            {
                unknown.Add(code.ToString());
            }
        }

        AssertTrue(
            unknown.Count == 0,
            "and every one of them is either a command or the data of a command"
            + "; the reader has no name for: "
            + string.Join(", ", unknown.GetRange(0, System.Math.Min(5, unknown.Count))));
    }

    public void Test_TheNamesAreTheOnesTheEngineUses()
    {
        // Spot checks against the engine's own source, chosen because they are
        // the ones a reader guesses wrongly: 231 shows a picture in this
        // generation and is a movement rate in the one before, and 121 is a
        // switch here and a party member there.
        AssertEq(
            MzCommandTable.NameOf(101), "Show Text",
            "101 is the command that shows text");
        AssertEq(
            MzCommandTable.NameOf(121), "Control Switches",
            "121 controls the switches, and in the generation before it is a"
            + " party member");
        AssertEq(
            MzCommandTable.NameOf(231), "Show Picture",
            "231 shows a picture, and in the generation before it is a movement"
            + " rate, so a reader carrying that numbering over would name the"
            + " wrong command for the same number");
        AssertEq(
            MzCommandTable.NameOf(355), "Script",
            "355 is the script block, which this repository reads as data and"
            + " never runs");
    }

    public void Test_ALineOfTextIsNotACommandAndIsNotRunAsOne()
    {
        // The trap, and the one that would be worst if it were got wrong. A
        // message is a 101 and then one or more 401s, and the 401 is the text
        // the message shows. A reader that dispatched 401 as a command of its
        // own would take a character's line of dialogue for an instruction.
        AssertEq(
            MzCommandTable.KindOf(401), MzCommandKind.Data,
            "401 is not a command of its own");
        AssertEq(
            MzCommandTable.OwnerOf(401), 101,
            "it belongs to the message above it");
        AssertEq(
            MzCommandTable.KindOf(101), MzCommandKind.Command,
            "and 101 is the command that reads it");
    }

    public void Test_TheOwnerOfEachDataNumberIsTheOneMeasuredAndNotARule()
    {
        // The rule that is tempting here is "the data number is three hundred
        // above its command". Four of the first five are, and that is how the
        // rule is found. **It is wrong for the rest**, and wrong in a way a
        // reader cannot see: 605 belongs to 302 while three hundred above it is
        // 305, and 412 belongs to 111 while three hundred above it is 112, which
        // is a loop and not a branch. A reader that used the rule would be right
        // often enough to look checked, and a shop's purchases would be read as
        // belonging to whatever 305 does.
        AssertEq(
            MzCommandTable.OwnerOf(605), 302,
            "605 is read by 302 and not by the 305 that three hundred above it"
            + " would name");
        AssertEq(
            MzCommandTable.OwnerOf(412), 111,
            "and 412 is read by 111, the branch, and not by the 112 that three"
            + " hundred above it would name, which is a loop");
        AssertTrue(
            MzCommandTable.IsCommand(112),
            "and 112 is a command of its own, so the two are not the same thing");
    }

    public void Test_ABranchsOptionsAreTwoCommandsAndOnePieceOfData()
    {
        // This is the measurement that took the most looking, and it is the
        // reason nothing here is a rule. Three numbers sit at an indent of their
        // own under a branch: 411, 412 and 413. **Two of them are commands the
        // engine dispatches and one is not.** The engine names 411 "Else" and
        // 413 "Repeat Above", and gives 412 no method at all, so 412 is the one
        // that a branch reads and the other two are things it does.
        //
        // A reader that assumed the whole family was data would refuse two real
        // commands, and one that assumed the whole family was commands would run
        // a branch's else as if it were an instruction. Both are silent.
        AssertEq(
            MzCommandTable.NameOf(411), "Else",
            "411 is a command, and the engine names it the else of a branch");
        AssertEq(
            MzCommandTable.NameOf(413), "Repeat Above",
            "and 413 is a command too, the loop that repeats what came before");

        AssertEq(
            MzCommandTable.KindOf(412), MzCommandKind.Data,
            "but 412 is not a command of its own");
        AssertEq(
            MzCommandTable.OwnerOf(412), 111,
            "it is the branch above that reads it");
    }

    public void Test_ThreeHundredAboveANumberIsNotHowItsOwnerIsFound()
    {
        // The rule this format invites, stated and then shown wrong twice, once
        // in each direction. 605 minus three hundred is 305 and 605 belongs to
        // 302. 412 minus three hundred is 112 and 412 belongs to 111. And 411
        // minus three hundred is 111 and 411 is a command of its own, so the
        // rule would make a real command into data.
        AssertEq(
            MzCommandTable.OwnerOf(605), 302,
            "605 belongs to 302 and not to the 305 it would point at");
        AssertEq(
            MzCommandTable.OwnerOf(412), 111,
            "412 belongs to 111 and not to the 112 it would point at");
        AssertEq(
            MzCommandTable.KindOf(411), MzCommandKind.Command,
            "and 411 is a command of its own, where the rule would have made it"
            + " the data of 111");
    }

    public void Test_ALineOfScriptIsDataOfTheScriptBlockAndIsNeverRun()
    {
        // A game's 657 lines carry the author's own text, and this game's carry
        // a picture's colours and its font. They are read as bytes and kept as
        // what they are. **A reader that dispatched them as a command would run
        // a game's script**, and the one thing this repository does not do.
        AssertEq(
            MzCommandTable.KindOf(657), MzCommandKind.Data,
            "a line of script is not a command of its own");
        AssertEq(
            MzCommandTable.OwnerOf(657), 355,
            "it belongs to the script block above it");

        // And what those lines actually hold in this game, which is why the
        // boundary matters: they are settings, not instructions this repository
        // may act on.
        var lines = ScriptLinesInTheGame();
        AssertTrue(
            lines.Count > 0,
            $"the game has {lines.Count} lines of script, which are read as text");
        AssertTrue(
            MzCommandTable.NameOf(657) != null
            && MzCommandTable.NameOf(657)!.Contains("Script"),
            "and the reader names what they are rather than guessing");
    }

    private static List<string> ScriptLinesInTheGame()
    {
        var found = new List<string>();
        var path = FixtureRoot.PathJoin("data").PathJoin("Map002.json");
        var file = MzDataFile.Read("Map002.json", Godot.FileAccess.GetFileAsBytes(path));
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
                    if (command.Member("code")?.IntOr(0) != 657)
                    {
                        continue;
                    }
                    var parameters = command.Member("parameters");
                    if (parameters == null || parameters.Items.Count == 0)
                    {
                        continue;
                    }
                    found.Add(parameters.Items[0].StringOr(""));
                }
            }
        }
        return found;
    }

    public void Test_TheEditorIndentIsNotACommand()
    {
        // The editor writes an indent of zero as the number zero. It is the
        // column of a command in a list and not an instruction, and a reader
        // that dispatched it would run a zero.
        AssertEq(
            MzCommandTable.KindOf(0), MzCommandKind.Separator,
            "the editor's own zero is the indent and not a command");
        AssertTrue(
            !MzCommandTable.IsCommand(0),
            "and it is not dispatched to anything");
    }

    public void Test_ACommandNameIsWrittenOnceAndOnlyOnce()
    {
        // The shape of this table was an enum with a name in each member and a
        // second table beside it carrying the same names as strings, because a C#
        // identifier cannot be "Show Text". **The two copies drifted and three
        // mutations of the name were invisible to the suite**, because the reader
        // handed out the string and the enum carried the prose, and changing
        // either one alone changed nothing a test could see. It is a record now
        // and a name exists in one place, so this test says where.
        AssertEq(
            MzCommandSet.Commands.Count, 114,
            "the commands are one list, and the name of each is in the same value"
            + " as its number");

        var names = new System.Collections.Generic.HashSet<string>();
        var codes = new System.Collections.Generic.HashSet<int>();
        foreach (var command in MzCommandSet.Commands)
        {
            AssertTrue(
                !string.IsNullOrWhiteSpace(command.Name),
                $"every command carries the engine own name; {command.Code} does not");
            names.Add(command.Name);
            codes.Add(command.Code);
        }

        AssertEq(
            names.Count, MzCommandSet.Commands.Count,
            "and no two commands share a name, so a name identifies one command");
        AssertEq(
            codes.Count, MzCommandSet.Commands.Count,
            "and no two commands share a number, so a number names one command");

        // The spot check that failed to notice three mutations before, now made
        // against the one place the name is written.
        foreach (var command in MzCommandSet.Commands)
        {
            if (command.Code == 231)
            {
                AssertEq(
                    command.Name, "Show Picture",
                    "231 shows a picture, and in the generation before it is a"
                    + " movement rate");
            }
            if (command.Code == 411)
            {
                AssertEq(command.Name, "Else", "and 411 is the else of a branch");
            }
        }
    }

    public void Test_TheOwnerOfEachDataNumberIsCheckedAgainstTheRuleItBreaks()
    {
        // The table is right because the owners were measured. This says what the
        // measurement is worth by showing the rule it is not: subtracting three
        // hundred finds the owner of four of the eight and gets the other four
        // wrong, and two of those point at a command that exists in this
        // generation and does something else entirely.
        var data = new System.Collections.Generic.Dictionary<int, int>
        {
            [401] = 101, [405] = 105, [408] = 108, [412] = 111,
            [501] = 102, [605] = 302, [655] = 355, [657] = 355,
        };

        var right = 0;
        var wrong = new System.Collections.Generic.List<string>();
        foreach (var (code, owner) in data)
        {
            AssertEq(
                MzCommandTable.OwnerOf(code), owner,
                $"the owner of {code} is the one that was measured");

            if (code - 300 == owner)
            {
                right++;
            }
            else
            {
                var guess = code - 300;
                wrong.Add($"{code}->{owner} but the rule says {guess}"
                    + (MzCommandTable.IsCommand(guess)
                        ? $" ({MzCommandTable.NameOf(guess)})"
                        : " which is not a command here"));
            }
        }

        AssertEq(
            right, 4,
            "and the rule that subtracting three hundred would follow is right"
            + " four times out of eight, which is the number at which a rule"
            + " built on a coincidence stops being checkable");
        AssertTrue(
            wrong.Count == 4,
            $"the four it gets wrong are: {string.Join("; ", wrong)}");
    }

    public void Test_ANumberThatIsNeitherIsRefusedRatherThanGuessed()
    {
        // A game written by a newer editor, or a file that is not this
        // generation's, may hold a number this table has no name for. The
        // answer is that it is unknown, and it is not the name of whatever
        // happens to be nearest.
        AssertEq(
            MzCommandTable.KindOf(9999), MzCommandKind.Unknown,
            "a number nothing claims is unknown");
        AssertEq(
            MzCommandTable.OwnerOf(9999), 0,
            "and it belongs to no command, not to the one three hundred below"
            + " it and not to the one above it");
        AssertTrue(
            MzCommandTable.NameOf(9999) == null,
            "and it is not given the name of a command it might resemble");
        AssertTrue(
            MzCommandTable.NameOf(1010) == null,
            "nor of the command one below it, which is a real command here");
    }

    public void Test_EveryNumberTheTableRefusesIsRefusedForTheReasonGiven()
    {
        // The second of the two mutations this suite let through was a reader
        // that handed a number the name of whatever happened to be next in its
        // table. The single spot checks above cover the numbers someone thought
        // of; this covers every number the table does not hold, which is the
        // whole of what a newer editor could send.
        var refused = new System.Collections.Generic.List<string>();
        for (var code = 0; code < 1000; code++)
        {
            if (!MzCommandTable.IsCommand(code)
                && MzCommandTable.OwnerOf(code) == 0
                && code != 0
                && MzCommandTable.NameOf(code) != null)
            {
                refused.Add(code.ToString());
            }
        }

        AssertTrue(
            refused.Count == 0,
            "no number without a command and without an owner is given a name;"
            + " these are: "
            + string.Join(", ", refused.GetRange(0, System.Math.Min(6, refused.Count))));
    }

    private static List<int> CommandsInTheGame()
    {
        var used = new List<int>();
        var seen = new HashSet<int>();
        foreach (var name in new[] { "Map001.json", "Map002.json" })
        {
            var path = FixtureRoot.PathJoin("data").PathJoin(name);
            var file = MzDataFile.Read(
                name, Godot.FileAccess.GetFileAsBytes(path));
            var events = file.Root.Member("events");
            if (events == null)
            {
                continue;
            }
            foreach (var item in events.Items)
            {
                if (item.Kind != MzKind.Object)
                {
                    continue;
                }
                foreach (var page in item.Member("pages")!.Items)
                {
                    foreach (var command in page.Member("list")!.Items)
                    {
                        var code = command.Member("code");
                        if (code != null && code.Kind == MzKind.Number
                            && seen.Add(code.IntOr(0)))
                        {
                            used.Add(code.IntOr(0));
                        }
                    }
                }
            }
        }
        used.Sort();
        return used;
    }
}
