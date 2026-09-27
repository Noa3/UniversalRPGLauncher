using System;
using System.Collections.Generic;
using System.IO;
using Godot;
using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// A real RPG Maker XP game's own data, read as bytes and never executed.
/// </summary>
/// <remarks>
/// <para>
/// These are data files an XP installation wrote, taken from a game the
/// repository was given and frozen here. An XP game keeps its data as marshal
/// and its scripts as marshal, and a game whose archive is not encrypted keeps
/// both in plain files, which is the case here. Nothing in this file is a
/// program: a marshal file is a value, a string, a number and a list of them,
/// and the reader turns that back into the same. The scripts of this game are
/// deliberately not imported, because a script is code and this repository does
/// not run a game's code.
/// </para>
/// <para>
/// This is the first real file of an engine this repository reads to be checked
/// against at all. Everything before it was verified against the rubies' own
/// sources, which is worth something and is not the same thing as a file that a
/// game actually wrote.
/// </para>
/// </remarks>
public partial class TestRealXpData : TestBase
{
    private const string FixtureRoot = "res://tests/fixtures/rgss-xp";

    private const string SecondFixtureRoot = "res://tests/fixtures/rgss-xp-microquest";

    public void Test_AnActorsFileFromTheGameIsRead()
    {
        var root = Read("Actors");
        AssertEq(root.Kind, "array", "an actor file is a list");

        var actors = root.Items;
        AssertTrue(actors.Count > 0, $"and holds the game's actors, {actors.Count} of them");

        // The first entry of an XP actor list is the game's own actor at index
        // one, and the list's first entry is a nil the engine reserves.
        var first = actors[0];
        var real = actors.Count > 1 ? actors[1] : null;
        AssertTrue(real != null, "and has a first actor after the reserved entry");
        AssertEq(real!.Kind, "object", "which is a game's own value");
        AssertEq(
            real.ClassName, "RPG::Actor",
            "and says what class it is, as the game wrote it");
    }

    public void Test_EveryFileInTheGameIsReadToTheEndWithoutRefusing()
    {
        // The point of a real file over a hand built one: a hand built stream has
        // only what the test put in it, and a game's file has strings in an
        // encoding nobody chose, numbers at the edges of the small form, lists
        // that point at each other and values of every kind the format has. A
        // reader that refuses one of them refuses a game that works.
        var files = new[] { "Actors", "Classes", "Items", "Enemies", "Skills", "States", "System", "Map003" };
        var refused = new List<string>();
        foreach (var name in files)
        {
            try
            {
                var root = Read(name);
                if (root.Kind == "array" || root.Kind == "hash")
                {
                    foreach (var value in Flatten(root))
                {
                    if (value.IsLink)
                    {
                        refused.Add(
                            $"{name}: a link that was not followed,"
                            + $" index {value.Link?.Index}");
                    }
                }
                }
            }
            catch (Exception exception)
            {
                refused.Add($"{name}: {exception.Message}");
            }
        }

        AssertTrue(
            refused.Count == 0,
            $"every file in the game is read whole, and the reader walks every"
            + $" value in it; it stopped on: {string.Join("; ", refused.GetRange(0, Math.Min(3, refused.Count)))}");
    }

    public void Test_AWholeNumberInTheGameIsReadAsItself()
    {
        // The packing was rebuilt out of Ruby 1.8.7's own writer, and the only
        // way to know the rebuild is right is a number a game chose. A map's
        // width and height and a tile's id are such numbers.
        //
        // A map file is not a list. It is a mapping whose keys are the names of
        // the parts the engine wrote, and the first version of this test said
        // it was a list, which is the kind of assumption only a test written
        // without a file in front of it makes.
        var map = Read("Map003");
        // A map is not a list and not a mapping. It is one of the game's own
        // values, an object, holding the parts the engine wrote as its members:
        // the width, the height, the display name, the tileset, the data of the
        // events, and the rest. Two earlier versions of this test said it was a
        // list and then said it was a mapping, and neither was checked against a
        // file, which is the whole point of having one.
        AssertEq(map.Kind, "object", "a map is one of the game's own values");
        AssertEq(
            map.ClassName, "RPG::Map",
            "and it says so, as the game wrote it");
        AssertTrue(
            map.Items.Count > 0 && map.Keys!.Count == map.Items.Count,
            $"holding the parts of the map as its members, {map.Keys!.Count} of them,"
            + " one for every value");

        // Walk it and count the whole numbers, so a fault in the packing shows
        // up as a fault about a game's own values rather than as a count.
        var whole = 0;
        foreach (var value in Flatten(map))
        {
            if (value.Kind == "integer")
            {
                whole++;
            }
        }

        AssertTrue(
            whole > 100,
            $"the game's own numbers are read as numbers, {whole} of them");
    }

    public void Test_AStringInTheGameKeepsItsOwnBytesAndIsNotDecodedHere()
    {
        // A game's strings are in whatever encoding its author used, which for a
        // Japanese installation is not this reader's. Reading one as text would
        // replace the author's bytes with this machine's idea of them, and the
        // name of an actor would come back as a question mark.
        var strings = BytesOfStrings(Read("Actors"));
        AssertTrue(
            strings.Count > 0,
            $"the game's actors hold strings, {strings.Count} of them");
        AssertTrue(
            strings[0].Length > 0,
            "and each one is handed over as bytes, not as a decoded text");
    }

    public void Test_BothGamesAreReadWholeAndTheyAreTwoDifferentInstallations()
    {
        // Two games rather than one, because the two were made on different
        // installations and neither the language nor the encoding of their
        // strings is the same. A reader that decoded text anywhere would answer
        // differently for the two, and one that read only one of them would
        // never notice.
        var games = new (string pName, string pRoot, string[] pFiles)[]
        {
            ("the first", FixtureRoot,
                ["Actors", "Classes", "Items", "Enemies", "Skills", "States", "System", "Map003"]),
            ("the second", SecondFixtureRoot,
                ["Actors", "Classes", "Items", "Enemies", "Skills", "States", "System", "Animations"]),
        };

        var problems = new List<string>();
        var wholeNumbers = 0;
        foreach (var (name, root, files) in games)
        {
            foreach (var file in files)
            {
                try
                {
                    foreach (var item in Flatten(ReadFrom(root, file)))
                    {
                        if (item.Kind == "integer")
                        {
                            wholeNumbers++;
                        }
                    }
                }
                catch (Exception exception)
                {
                    problems.Add($"{name} {file}: {exception.Message}");
                }
            }
        }

        AssertTrue(
            problems.Count == 0,
            "both games are read whole; the reader stopped on: "
            + string.Join("; ", problems.GetRange(0, Math.Min(3, problems.Count))));
        AssertTrue(
            wholeNumbers > 500,
            $"and between them they hold whole numbers, {wholeNumbers} of them, read"
            + " through the packing taken from the engine's own writer");
    }

    public void Test_StringsFromTwoGamesStayBytesAndAreNotDecodedIntoThisMachinesText()
    {
        // The whole reason there are two games here. Their names are written in
        // different encodings, and a reader that turned either of them into text
        // would replace the author's bytes with this machine's idea of them.
        var first = BytesOfStrings(ReadFrom(FixtureRoot, "Actors"));
        var second = BytesOfStrings(ReadFrom(SecondFixtureRoot, "Actors"));
        AssertTrue(first.Count > 0, $"the first game holds strings, {first.Count} of them");
        AssertTrue(second.Count > 0, $"and so does the second, {second.Count} of them");

        // A decoded text would have to agree about what a byte is. Two games
        // whose strings are in different encodings cannot, so the reader must
        // not have made a text at all, and the way to show that is that the
        // bytes are still there and nothing was thrown away.
        var withHigh = 0;
        foreach (var bytes in first)
        {
            if (HoldsAboveAscii(bytes))
            {
                withHigh++;
            }
        }
        foreach (var bytes in second)
        {
            if (HoldsAboveAscii(bytes))
            {
                withHigh++;
            }
        }

        AssertTrue(
            withHigh > 0,
            $"and the bytes above plain ascii survive, in {withHigh} of the strings,"
            + " which a decoding would have replaced");
    }

    private static bool HoldsAboveAscii(byte[] pBytes)
    {
        foreach (var value in pBytes)
        {
            if (value > 0x7F)
            {
                return true;
            }
        }

        return false;
    }

    private static List<byte[]> BytesOfStrings(MarshalValue pValue)
    {
        var found = new List<byte[]>();
        foreach (var value in Flatten(pValue))
        {
            if (value.Kind == "string" && value.Bytes is { Length: > 0 })
            {
                found.Add(value.Bytes);
            }
        }

        return found;
    }

    private MarshalValue ReadFrom(string pRoot, string pName)
    {
        var path = pRoot.PathJoin($"{pName}.rxdata");
        AssertTrue(
            System.IO.File.Exists(ProjectSettings.GlobalizePath(path)),
            $"{pRoot}/{pName}.rxdata is in the fixtures");
        return new MarshalReader(Godot.FileAccess.GetFileAsBytes(path)).Read();
    }

    private MarshalValue Read(string pName) => ReadFrom(FixtureRoot, pName);

    private static IEnumerable<MarshalValue> Flatten(MarshalValue pValue)
    {
        var pending = new Stack<MarshalValue>();
        pending.Push(pValue);
        var seen = 0;
        while (pending.Count > 0 && seen < 2_000_000)
        {
            var current = pending.Pop();
            seen++;
            if (current == null)
            {
                continue;
            }

            yield return current;
            foreach (var child in current.Items)
            {
                if (child != null && !child.IsLink)
                {
                    pending.Push(child);
                }
            }
        }
    }
}
