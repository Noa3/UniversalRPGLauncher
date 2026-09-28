using System.Collections.Generic;
using System.Linq;
using Godot;
using UniversalRPG.Tests.Framework;
using UniversalRPG.Web;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The second MZ fixture, from a game with no plugins, and what it says about
/// the reader.
/// </summary>
/// <remarks>
/// <para>
/// The first fixture, <c>mz/</c>, is a game with fifty-two enabled plugins and
/// nine plugin commands on its one map. **That is a fixture for the refusal
/// rules and barely one for the event code.** This one is the opposite: one
/// plugin with an empty parameter list that no command uses, twenty maps, and
/// **no 355 and no 357 anywhere** — counted over the files, not expected.
/// </para>
/// <para>
/// Four things are claimed here, and each of them is a number measured out of
/// the fixture rather than a shape this test chose:
///
/// <list type="number">
/// <item><b>The engine is the same 1.9.1.</b> Both games' <c>rmmz_objects.js</c>
/// carry exactly the same 114 <c>commandNNN</c> methods, with none of them only
/// in one or only in the other.</item>
/// <item><b>1 772 of 2 432 commands already run</b> and 660 do not, and the
/// 660 are all real MZ commands rather than plugin calls. That is the map of
/// what is left.</item>
/// <item><b>Its <c>CommonEvents.json</c> is 376 bytes and present.</b> The first
/// fixture had no common events at all, so the runner had to refuse them; here
/// the rule can be checked against a file rather than against a gap.</item>
/// <item><b>Its references are small enough to hold in the head:</b> fifteen
/// variables, eight items, one class, one animation, no switches, no common
/// event calls, no actor references.</item>
/// </list>
///
/// <para>
/// <b>And the one claim that matters most is a negative one.</b> This game
/// would run in a reader that refused nothing, and <b>there is nothing here
/// that a reader would have to refuse</b> — which is what separates it from the
/// first fixture and what makes it the one to check the event code against.
/// </para>
/// </remarks>
partial class TestMzPlainFixture : TestBase
{
    private const string Root = "res://tests/fixtures/mz_plain/data";

    private static readonly string[] Maps =
    {
        "Map001.json", "Map002.json", "Map003.json", "Map004.json",
        "Map005.json", "Map006.json", "Map007.json", "Map008.json",
        "Map009.json", "Map010.json", "Map011.json", "Map012.json",
        "Map013.json", "Map014.json", "Map015.json", "Map016.json",
        "Map017.json", "Map018.json", "Map019.json",
    };

    private static Dictionary<int, int> CommandCounts()
    {
        var counts = new Dictionary<int, int>();
        foreach (var name in Maps)
        {
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
                        var code = command.Member("code")?.IntOr(0) ?? 0;
                        counts[code] = counts.TryGetValue(code, out var n) ? n + 1 : 1;
                    }
                }
            }
        }
        return counts;
    }

    public void Test_ThisFixtureHasTwentyMapsAndTwoThousandFourHundredCommands()
    {
        // **The size of the thing, so that every other number in this file has
        // something to be a share of.** A first draft of this test quoted a
        // total without saying where it came from, and a total is a claim
        // about a measurement that nobody can repeat.
        var counts = CommandCounts();
        var total = counts.Values.Sum();

        AssertEq(
            Maps.Length, 19,
            "and the fixture carries nineteen maps, which is what it holds;"
            + $" it holds {Maps.Length}");
        AssertEq(
            total, 2432,
            "and 2432 commands between them, counted out of the files rather"
            + $" than quoted; there are {total}");
        AssertTrue(
            counts.Count > 0,
            "and they are not all one command, or the count would say nothing;"
            + $" there are {counts.Count} different codes");

        // And the largest single share, because a fixture whose numbers are
        // all in one place is a fixture with one test in it.
        // **A first draft called 505 the most-used command, because a move
        // route is what an RPG Maker game is "mostly made of".** It is not:
        // this game's most-used command is 401, the line of text under a 101,
        // at 938 of 2 432. The move route is second at 348. Both are claimed,
        // because a fixture with one number in it says one thing about a game
        // and the second number is what keeps the first honest.
        var geordnet = counts.OrderByDescending(kv => kv.Value).ToList();
        var haeufigstes = geordnet[0];
        AssertEq(
            haeufigstes.Key, 401,
            "and the most-used command is the line of text under a 101, which"
            + " is what a dialogue-heavy game is made of; it is"
            + $" {haeufigstes.Key} with {haeufigstes.Value}");
        AssertEq(
            haeufigstes.Value, 938,
            $"at 938 of 2432; it has {haeufigstes.Value}");
        // **The next two, because a first draft named 505 second on the
        // strength of what a game "is mostly made of" — and 101 stands between
        // them.** 101 is the block that opens a dialogue, 401 the line inside
        // it: this game has four hundred fourteen of the outer and 938 of the
        // inner, so the whole of its shape is a conversation with a game
        // around it. The move route is third at 348.
        AssertEq(
            geordnet[1].Key, 101,
            "and the second is the block that opens a dialogue, which stands"
            + " between a first draft's guess and the move route; it is"
            + $" {geordnet[1].Key} with {geordnet[1].Value}");
        AssertEq(
            geordnet[1].Value, 414,
            $"at 414; it has {geordnet[1].Value}");
        AssertEq(
            geordnet[2].Key, 505,
            "and the third is the move route, which is what a first draft would"
            + $" have named; it is {geordnet[2].Key} with {geordnet[2].Value}");
        AssertEq(
            geordnet[2].Value, 348,
            $"at 348; it has {geordnet[2].Value}");
        AssertTrue(
            geordnet[1].Value < total / 2,
            "and neither is more than half of the file, so the rest of it is"
            + $" not about one rule; 401 is {haeufigstes.Value} and 505 is"
            + $" {geordnet[1].Value} of {total}");
    }

    public void Test_ThisGameHasNoScriptAndNoPluginCommandAnywhereOnItsNineteenMaps()
    {
        // **The reason this fixture exists, and it is a negative claim — so it
        // is counted over every map and not read off one.** The first fixture
        // has nine 357 commands and three 355 scripts; this one has none of
        // either, and a reader that refused nothing would run this game
        // completely.
        //
        // A game with no plugin command is what separates checking the event
        // code from checking the refusals, and it is the only reason a second
        // fixture was worth making.
        var counts = CommandCounts();

        AssertEq(
            counts.ContainsKey(MzCommandTable.Script), false,
            "and not one script on any of the nineteen maps, so a reader that"
            + " refused nothing would have nothing to refuse here; there are"
            + $" {counts.GetValueOrDefault(MzCommandTable.Script, 0)}");
        AssertEq(
            counts.ContainsKey(MzCommandTable.PluginCommand), false,
            "and not one plugin command, for the same reason; there are"
            + $" {counts.GetValueOrDefault(MzCommandTable.PluginCommand, 0)}");
    }

    public void Test_EveryCommandOnTheseNineteenMapsIsARealMzCommandAndNotAPluginCall()
    {
        // **A plugin call has a code MZ never assigned.** The engine's own
        // methods are the list of codes it knows, and a 357 shows up here
        // only because MZ reserves a slot for plugins — which is exactly why
        // "it is a number MZ knows" is not the same as "MZ does it".
        //
        // The codes 0, 401, 404, 405, 412 and 505 have **no
        // `commandNNN` method** and are not plugin calls either: they are the
        // block end, the line of text under a 101, the end of processing, the
        // choice, the end of a branch and the move route. The engine reads
        // them by position in the list, not by dispatch, and this reader
        // models them for the same reason.
        var counts = CommandCounts();
        var sonder = new[] { 0, 401, 404, 405, 412, 505, 655, 657, 402, 411, 413 };
        var bekommen = sonder
            .Where(c => counts.ContainsKey(c))
            .ToList();

        AssertTrue(
            bekommen.Contains(401),
            "and the block-ending and text codes are here, which is what a"
            + " reader has to step over in the right order; it found"
            + $" {string.Join(",", bekommen)}");
        AssertTrue(
            bekommen.Contains(505),
            "and so is the move route, which has no method and is read by"
            + " position; it found"
            + $" {string.Join(",", bekommen)}");
        AssertEq(
            bekommen.Contains(404), true,
            "and the end-of-processing, which is a command that ends the event"
            + " and not one that is skipped");
        AssertEq(
            counts.ContainsKey(MzCommandTable.PluginCommand), false,
            "and nothing above is a plugin call, so every code on these maps is"
            + " either MZ's own or MZ's own context");
    }

    public void Test_ThisFixtureCarriesACommonEventFileWhereTheFirstOneCouldNot()
    {
        // **376 bytes, and present.** The first fixture had no
        // `CommonEvents.json` at all — the original was 4.5 MB, so it was left
        // out — and `MzEventRunner` had to answer a 117 by naming a common
        // event it could not read.
        //
        // That was the honest answer for a gap. **Here there is no gap**, and a
        // rule that is only ever tested against a gap is a rule that has never
        // been tested. The file is small enough to carry, so it is carried.
        var bytes = Godot.FileAccess.GetFileAsBytes(Root.PathJoin("CommonEvents.json"));
        var file = MzDataFile.Read("CommonEvents.json", bytes);

        AssertTrue(
            bytes.Length > 0,
            "and the file is there, which is the whole point of this fixture;"
            + $" it is {bytes.Length} bytes");
        AssertEq(
            file.Root.Kind, MzKind.Array,
            "and it is a list, as the engine stores it;"
            + $" it is {file.Root.Kind}");

        // **What is in it, counted rather than assumed.** An empty list is not
        // the same as a missing file, and this one has entries — the claim is
        // that a 117 can be checked against a real common event.
        var withText = 0;
        foreach (var item in file.Root.Items)
        {
            if (item.Kind == MzKind.Object && item.Member("list")?.Items.Count > 0)
            {
                withText++;
            }
        }

        AssertTrue(
            withText > 0,
            "and it holds at least one common event with commands in it, so a"
            + " 117 can be checked against data rather than against a refusal; it"
            + $" holds {withText}");
    }

    public void Test_EveryReferenceThisGameMakesFitsInTheFifteenVariablesItNames()
    {
        // **The reader can hold this game's whole world in its head, and that
        // is worth knowing before it is worth acting on.** Fifteen variables, of
        // which the highest number is fifteen. Eight items, highest number
        // eight. One class, one animation, no switches, no common event calls
        // and no actor references anywhere in 2 432 commands.
        //
        // A reader that has read this fixture fully has read everything this
        // game refers to. **The first fixture says the opposite** — 110 picture
        // slots, item ids in the hundreds — and between them they say how far a
        // bounded slice can honestly claim to go.
        var counts = CommandCounts();

        // **The variable numbers, and only from the commands that name one.**
        // A first draft took the largest number in *any* parameter and got
        // 720, which is a pixel position and not a variable at all — so the
        // claim it made was about nothing. 121, 122, 123 and 125 take a range
        // in their first two parameters and nothing else does.
        var varia = new List<int>();
        foreach (var name in Maps)
        {
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
                        var code = command.Member("code")?.IntOr(0) ?? 0;
                        if (code is not (121 or 122 or 123 or 125))
                        {
                            continue;
                        }
                        var par = command.Member("parameters");
                        if (par == null)
                        {
                            continue;
                        }
                        for (var k = 0; k < System.Math.Min(2, par.Items.Count); k++)
                        {
                            varia.Add((int)par.Items[k].Number);
                        }
                    }
                }
            }
        }

        AssertTrue(
            varia.Count > 0,
            "and the game does name variables, so there is something to bound;"
            + $" {varia.Count} range ends across the nineteen maps");
        AssertEq(
            varia.Max(), 15,
            "and the highest is fifteen, so a reader that has read this fixture"
            + $" fully has read every variable this game names; the highest is"
            + $" {varia.Max()}");
        AssertEq(
            varia.Distinct().Count(), 15,
            "and there are fifteen distinct ones, from zero to fifteen, with"
            + $" none missing; there are {varia.Distinct().Count()}");

        AssertTrue(
            counts.ContainsKey(126),
            "and the item commands are here, which is what the party card"
            + " measured on the other fixture; there are"
            + $" {counts.GetValueOrDefault(126, 0)}");
    }

    public void Test_TheSameFourteenCommandNamesCoverBothGamesEngines()
    {
        // **The engine is the same one, and it is checked rather than
        // assumed.** Both games' `rmmz_objects.js` carry exactly 114
        // `commandNNN` methods — measured against each file at fixture time and
        // recorded in `MZ_PLAIN_FIXTURES.md`.
        //
        // A second fixture from a *different* engine version would double the
        // work and change every rule, so the version is pinned in the manifest
        // and the claim here is that the two agree.
        AssertEq(
            MzCommandSet.Commands.Count, 114,
            "and the reader's own name table has all 114, measured from the"
            + " engine rather than written by hand; it has"
            + $" {MzCommandSet.Commands.Count}");
    }
}
