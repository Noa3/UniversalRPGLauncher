using System;
using System.Collections.Generic;
using Godot;
using UniversalRPG.Tests.Framework;
using UniversalRPG.Web;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The data files a real RPG Maker MZ game wrote, read as data.
/// </summary>
/// <remarks>
/// <para>
/// The fixtures are eleven data files of a real 1.9.1 game: the system file, the
/// database, the map list and two maps. The JavaScript is deliberately not here.
/// A script is code, and this repository does not run a game's code; the
/// <c>js</c> folder is a signature for the detector and nothing else.
/// </para>
/// <para>
/// Nothing here is asserted from documentation. Every count, every field name and
/// the one trap in the format were measured against these files, and the two
/// places where a first draft of this test was wrong are written down below
/// because they are the reason the format is described the way it is.
/// </para>
/// </remarks>
partial class TestRealMzData : TestBase
{
    private const string FixtureRoot = "res://tests/fixtures/mz";

    private MzDataFile ReadFixture(string pName)
    {
        var path = FixtureRoot.PathJoin(pName);
        AssertTrue(
            System.IO.File.Exists(ProjectSettings.GlobalizePath(path)),
            $"{pName} is in the fixtures");
        return MzDataFile.Read(pName, Godot.FileAccess.GetFileAsBytes(path));
    }

    public void Test_TheSystemFileOfThisGameIsRead()
    {
        var system = ReadFixture("data/System.json");

        AssertTrue(system.HasKnownShape, "and the reader knows the shape by name");
        var title = system.Root.Member("gameTitle");
        AssertTrue(
            title != null && title.Kind == MzKind.String,
            "the game keeps its own title as text");
        AssertTrue(
            !string.IsNullOrEmpty(title?.Text),
            $"and it is not empty: '{title?.Text}'");
        AssertTrue(
            system.Root.Member("locale")?.StringOr("") == "ja_JP",
            "and the locale is the one the game was written in, not this"
            + " machine's");
    }

    public void Test_TheFirstEntryOfADatabaseFileIsNullAndIsNotAnActor()
    {
        // The trap, and the reason this test exists before any other. A database
        // file is an array whose index zero is null: the editor numbers its
        // actors from one so that zero can mean "no actor". A reader written from
        // the documentation's shape alone reads that null as the first actor and
        // the first item and the first enemy, and a game then has three entries
        // that are nothing at all.
        var actors = ReadFixture("data/Actors.json");

        AssertEq(
            actors.Root.Kind, MzKind.Array,
            "so the actors file is an array");
        AssertEq(
            actors.Root.Items[0].Kind, MzKind.Null,
            "whose first entry is a null and not an actor");
        AssertTrue(
            actors.Entries.Count > 0,
            $"and the actors are the entries after it, {actors.Entries.Count} of them");
        AssertTrue(
            actors.Entries[0].Kind == MzKind.Object,
            "each of which is an object holding that actor's fields");
        AssertTrue(
            !string.IsNullOrEmpty(
                actors.Entries[0].Member("name")?.Text ?? ""),
            $"the first of them is named"
            + $" '{actors.Entries[0].Member("name")?.Text}'");
    }

    public void Test_TheMapListIsAnArrayOfSeventeenWhoseFirstIsNull()
    {
        // Measured from this game: seventeen entries, the first null, and the
        // names are the author's own text.
        var infos = ReadFixture("data/MapInfos.json");

        AssertEq(infos.Root.Kind, MzKind.Array, "the list is an array");
        AssertEq(infos.Root.Items[0].Kind, MzKind.Null,
            "whose first entry is null");
        AssertTrue(
            infos.Entries.Count >= 16,
            $"and the game has {infos.Entries.Count} maps after it");
        var first = infos.Entries[0];
        AssertTrue(
            first.Member("id")?.Kind == MzKind.Number
            && first.Member("name")?.Kind == MzKind.String,
            "each of which carries its own number and its own name");
    }

    public void Test_AMapCarriesItsEventsAsAnArrayWhoseUnusedCellsAreNull()
    {
        // Measured from this game's map, because the shape was guessed wrong
        // once. A first draft of this test claimed the array was one entry per
        // cell of the field, on the strength of the previous generation's
        // editor. It is not: the field is seventeen by thirteen and the array
        // holds seven entries, of which the first is null. What the null means
        // is that the array is indexed by event number and the editor writes
        // from the first one, not that it is padded to the field.
        var map = ReadFixture("data/Map002.json");

        AssertTrue(
            map.Root.Member("width")?.IntOr(0) == 17
            && map.Root.Member("height")?.IntOr(0) == 13,
            "the map has the size the game gave it, "
            + $"{map.Root.Member("width")?.IntOr(0)} by"
            + $" {map.Root.Member("height")?.IntOr(0)}");
        var events = map.Root.Member("events");
        AssertTrue(
            events != null && events.Kind == MzKind.Array,
            "and its events are an array");

        var nulls = 0;
        var present = 0;
        foreach (var item in events!.Items)
        {
            if (item.Kind == MzKind.Null)
            {
                nulls++;
            }
            else
            {
                present++;
            }
        }

        AssertEq(nulls, 1, "the array starts with one null");
        AssertEq(present, 6, $"and holds {present} events, which is what this"
            + " game has on this map");
        AssertTrue(
            events.Items.Count < 17 * 13,
            "so the array is indexed by event and not padded out to the size of"
            + $" the field: {events.Items.Count} entries for a field of"
            + " seventeen by thirteen");
    }

    public void Test_ACommandInThisGameIsNamedByANumberAndNotAPackedOne()
    {
        // The other guess that was wrong, and it would have been the expensive
        // one. In the generation before this a command's number is its own value
        // multiplied by a thousand, so a reader written for that divides by a
        // thousand to learn what a command is. **This game's commands are the
        // small numbers themselves**: 121, 231, 357, 657, with nothing above a
        // thousand anywhere in the file. A reader that divided would turn a
        // command into zero and a game into nothing.
        var map = ReadFixture("data/Map002.json");
        var events = map.Root.Member("events")!;
        MzValue? event_ = null;
        foreach (var item in events.Items)
        {
            if (item.Kind == MzKind.Object)
            {
                event_ = item;
                break;
            }
        }
        AssertTrue(event_ != null, "the map has an event to look at");

        var page = event_!.Member("pages")!.Items[0];
        var list = page.Member("list")!;
        AssertTrue(list.Items.Count > 0, $"and it has {list.Items.Count} commands");

        var aboveAThousand = 0;
        var seen = new List<int>();
        foreach (var command in list.Items)
        {
            var code = command.Member("code");
            AssertTrue(
                code != null && code.Kind == MzKind.Number,
                "a command names itself with a field called code");
            var number = command.Member("code")!.IntOr(-1);
            if (number > 1000)
            {
                aboveAThousand++;
            }
            seen.Add(number);
        }

        AssertTrue(
            aboveAThousand == 0,
            $"and none of this game's {list.Items.Count} commands is a packed"
            + $" number; they are {string.Join(", ", seen.GetRange(0, 8))} and the"
            + " rest, with nothing above a thousand, so a reader from the"
            + " generation before would divide every one of them to zero");
    }

    public void Test_EveryFieldOfAnEventIsKeptEvenTheOnesWithNoName()
    {
        // A game's event carries the fields this reader has no name for, and
        // dropping them would lose a game the first time anything was written
        // back. The names are measured from the file rather than from a shape in
        // a document, which is how the previous test's `commandId` was found to
        // be this generation's `code`.
        var map = ReadFixture("data/Map002.json");
        var events = map.Root.Member("events")!;
        MzValue? first = null;
        foreach (var item in events.Items)
        {
            if (item.Kind == MzKind.Object)
            {
                first = item;
                break;
            }
        }
        AssertTrue(first != null, "the map has an event to look at");

        var event_ = first!;
        AssertTrue(
            event_.Member("id")?.Kind == MzKind.Number,
            "an event carries the number it is known by");
        AssertTrue(
            event_.Member("name")?.Kind == MzKind.String,
            "and the name the author gave it");
        AssertTrue(
            event_.Member("pages")?.Kind == MzKind.Array
            && event_.Member("pages")!.Items.Count > 0,
            "and its pages, which is where its commands are");
        var page = event_.Member("pages")!.Items[0];
        AssertTrue(
            page.Member("list")?.Kind == MzKind.Array,
            "a page carries a list of commands");
        AssertTrue(
            page.Member("list")!.Items.Count > 0,
            $"and this event's first page has {page.Member("list")!.Items.Count}"
            + " of them");
        AssertTrue(
            page.Member("list")!.Items[0].Member("parameters")?.Kind == MzKind.Array,
            "each of which carries its own parameters as an array, and those"
            + " are the values the engine will hand to a running game");
    }

    public void Test_AStringThatIsNeverClosedIsRefusedRatherThanRunToTheEnd()
    {
        // A game that was interrupted while saving leaves a file that stops in
        // the middle of a name. Read to the end and it becomes the longest
        // string in the game, still valid, and nothing downstream can tell it
        // from a name the author wrote.
        var refused = "";
        try
        {
            MzDataFile.ReadText(
                "data/Actors.json", "[null,{\"name\":\"a name that stops");
        }
        catch (MzDataException exception)
        {
            refused = exception.Message;
        }

        AssertTrue(
            refused.Contains("not closed"),
            $"the file is refused at the string that never ends: '{refused}'");
    }

    public void Test_EscapeTheEditorNeverWritesIsRefusedRatherThanTakenAsText()
    {
        // `\q` is not an escape JSON has. A reader that accepts it puts a letter
        // into a game's name that the author never typed, and a name is the one
        // thing in a file a person reads.
        var refused = "";
        try
        {
            MzDataFile.ReadText("data/Actors.json", "[null,{\"name\":\"a\\qb\"}]");
        }
        catch (MzDataException exception)
        {
            refused = exception.Message;
        }

        AssertTrue(
            refused.Contains("not an escape"),
            $"and says which escape it did not know: '{refused}'");
    }

    public void Test_AFileThatNestsPastTheLimitIsRefusedWithTheDepthInIt()
    {
        // A game's data nests a few levels. A file that nests further is either
        // broken or hostile, and reading it would end the process rather than
        // the read, so the limit is stated and the refusal says what it was.
        var deep = new System.Text.StringBuilder("[");
        for (var level = 0; level < 200; level++)
        {
            deep.Append("[");
        }
        for (var level = 0; level < 200; level++)
        {
            deep.Append("]");
        }
        deep.Append("]");

        var refused = "";
        try
        {
            MzDataFile.ReadText("data/Map001.json", deep.ToString());
        }
        catch (MzDataException exception)
        {
            refused = exception.Message;
        }

        AssertTrue(
            refused.Contains("nests deeper"),
            $"a file that nests past the limit is refused: '{refused}'");
    }

    public void Test_ANumberWithAnExponentAndNoDigitsIsRefused()
    {
        var refused = "";
        try
        {
            MzDataFile.ReadText("data/System.json", "{\"n\":1e}");
        }
        catch (MzDataException exception)
        {
            refused = exception.Message;
        }

        AssertTrue(
            refused.Contains("exponent"),
            $"a broken exponent is not turned into a number: '{refused}'");
    }

    public void Test_TheFileKeepsItsOwnTextSoACallerCanHashWhatItRead()
    {
        // The reader's word about a file is its own idea of it. The bytes are
        // what a game is, and a caller that wants to know it read the same bytes
        // again, which it can only do if the text is kept.
        var system = ReadFixture("data/System.json");
        AssertTrue(
            system.Text.Length > 1000,
            $"the file's own text is kept, {system.Text.Length} characters");
        AssertTrue(
            system.Text.StartsWith("{"),
            "and it is the text the game wrote, starting with its own brace");
        AssertTrue(
            system.Text.Contains("gameTitle"),
            "and it holds the game's own fields");
    }

    public void Test_TheShortcutsAreOnlyForTheShapesTheReaderKnows()
    {
        // A caller is told whether a file is one of the shapes this reader knows
        // by name, and the file it is given may be valid JSON and not one of
        // them. Claiming a shape that is not there would let a caller use the
        // shortcuts on a file they were written for something else.
        var actors = MzDataFile.ReadText("data/Actors.json", "[null,{\"id\":1}]");
        AssertTrue(
            actors.HasKnownShape,
            "the actors file is a shape the reader knows");

        var other = MzDataFile.ReadText("data/Something.json", "[null,{\"id\":1}]");
        AssertTrue(
            !other.HasKnownShape,
            "and a file the reader has no name for is not claimed as one, even"
            + " though it reads the same way");
    }

    public void Test_TheEntriesWithoutTheNullsAreTheThingsInTheFile()
    {
        // The trap the format has, tested on its own so that a reader which
        // compacted the nulls away would be caught here and not three files
        // later.
        var file = MzDataFile.ReadText(
            "data/Actors.json", "[null,{\"id\":1},{\"id\":2},null,{\"id\":4}]");

        AssertEq(
            file.Root.Items.Count, 5,
            "the array holds every cell the editor wrote");
        AssertEq(
            file.Entries.Count, 3,
            "and the entries without the nulls are the three things in it");
        AssertEq(
            file.Entries[0].Member("id")?.IntOr(0), 1,
            "the first of which is the one the editor numbered one");
        AssertEq(
            file.Entries[2].Member("id")?.IntOr(0), 4,
            "and the last is four, so a null in the middle is a gap and not the"
            + " end of the list");
    }

    public void Test_AFileThatIsNotJsonIsRefusedWithAReason()
    {
        // The engine's own files are JSON and nothing else, so a file that is not
        // is either a different engine's file or a corrupt one, and in both cases
        // the caller is told which rather than handed a half read value.
        var refused = "";
        try
        {
            MzDataFile.ReadText("data/Map001.json", "{ this is not json");
        }
        catch (MzDataException exception)
        {
            refused = exception.Message;
        }

        AssertTrue(refused.Length > 0, "a file that is not json is refused");
        AssertTrue(
            refused.Contains("not JSON"),
            $"and the reason says so rather than pointing somewhere else: {refused}");
    }

    public void Test_AFolderFullOfNullsIsStillAFileAndNotAnEmptyOne()
    {
        // The shape that a reader written for "a list of things" would treat as
        // empty, and that a game's first database file really is at index zero.
        var file = MzDataFile.ReadText("data/Actors.json", "[null,null,null]");

        AssertEq(
            file.Root.Items.Count, 3,
            "the array holds every cell, not only the ones with something in them");
        AssertEq(
            file.Entries.Count, 0,
            "and the entries without the nulls are none, which is not the same"
            + " thing as a file with no cells");
    }
}
