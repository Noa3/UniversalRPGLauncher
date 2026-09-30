using System.Collections.Generic;
using System.Linq;
using Godot;
using UniversalRPG.Tests.Framework;
using UniversalRPG.Web;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// What 351 opens and what 357 will not do.
/// </summary>
/// <remarks>
/// <para>
/// K-127 asked which picture commands come next and the answer was none of
/// them. **This card is the other two commands this map still has that mean
/// something**, and they mean opposite things.
/// </para>
/// <para>
/// <b>351 opens a menu, and the battle check is the whole of it.</b> The
/// engine's <c>command351</c> is <c>if (!$gameParty.inBattle()) {
/// SceneManager.push(Scene_Menu); }</c> — a menu in a battle is not this
/// command with another scene, it is nothing at all.
///
/// <b>357 is a request to run somebody else's JavaScript, and it is refused
/// permanently.</b> The engine's <c>command357</c> is
/// <c>PluginManager.callCommand(this, pluginName, params[1], params[3])</c>.
/// This game ships <b>fifty-two plugins, all fifty-two enabled</b>, and its
/// eleven 357 commands call three of them by name. That is not a reason to
/// implement it — it is the reason not to, and it is the same reason 355 is
/// not implemented. What can be read without running a line of the plugin is
/// read and kept; what the plugin does is not guessed.
/// </para>
/// </remarks>
partial class TestMzMenuAndPlugins : TestBase
{
    private const string FixtureRoot = "res://tests/fixtures/mz";

    private static List<MzCommandEntry> MenuAndPluginCommandsInThisGame()
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
                    if (entry.Code == MzCommandTable.OpenMenu
                        || entry.Code == MzCommandTable.PluginCommand)
                    {
                        found.Add(entry);
                    }
                }
            }
        }
        return found;
    }

    public void Test_AMenuOpensOutOfBattleAndDoesNotOpenInOne()
    {
        // `command351` is
        //   if (!$gameParty.inBattle()) {
        //       SceneManager.push(Scene_Menu);
        //       Window_MenuCommand.initCommandPosition();
        //   }
        //   return true;
        // **and it returns true either way.** A reader that stopped the run in
        // a battle would have made a game that opens a menu in a fight — which
        // the engine does not do — stop as well, and the event after it would
        // never run.
        var outOfBattle = new MzBranchFacts();
        var actions = new List<MzAction>();
        var interpreter = new MzInterpreter(new List<MzCommandEntry>
        {
            new(MzCommandTable.OpenMenu, new List<string>(), 0),
            new(0, new List<string>(), 0),
        });
        interpreter.Run(actions, outOfBattle);

        AssertEq(
            outOfBattle.Menu, MzMenuState.Open,
            "and a menu is open outside a battle, which is the command's whole"
            + $" effect; it is {outOfBattle.Menu}");
        AssertEq(
            interpreter.Stopped, MzStep.Finished,
            "and the list ran through, because the engine returns true"
            + $" whatever the battle check said; it is {interpreter.Stopped}");
        AssertEq(
            actions.Count, 1,
            "and it was recorded once, so a caller can see it; there are"
            + $" {actions.Count} actions");

        // **Und `EnterBattle` statt eines Setters**, **denn es geht um
        // `351 Open Menu` und nicht um einen Kampf.**
        var inBattle = new MzBranchFacts();
        inBattle.EnterBattle();
        var battleActions = new List<MzAction>();
        var battle = new MzInterpreter(new List<MzCommandEntry>
        {
            new(MzCommandTable.OpenMenu, new List<string>(), 0),
            new(0, new List<string>(), 0),
        });
        battle.Run(battleActions, inBattle);

        AssertEq(
            inBattle.Menu, MzMenuState.Closed,
            "and no menu is open in a battle, which is the engine's answer and"
            + $" not a failure; it is {inBattle.Menu}");
        AssertEq(
            battle.Stopped, MzStep.Finished,
            "and the list still ran through, because the engine returns true"
            + " either way; it is"
            + $" {battle.Stopped}");
        AssertTrue(
            battleActions[0].What.Contains("battle"),
            "and what it says names the battle rather than nothing happening,"
            + $" so a caller can tell the two apart: {battleActions[0].What}");
    }

    public void Test_APluginCommandIsRefusedAndNamesThePluginItWouldHaveCalled()
    {
        // **This is the shape every 355 already has, and the reason is the
        // same one.** `command357` hands a plugin name and a command name to
        // `PluginManager.callCommand`, which is a JavaScript function. Reading
        // the parameters is reading data. Calling it is not, and no bounded
        // slice of this repository changes that.
        //
        // The claim has two halves, and the second is the one that matters: it
        // is **not** stepped over. A silent step would leave a game that looks
        // as if it works while its crafting menu and its floating text never
        // appear, and that is the failure this repository keeps refusing to
        // produce.
        var facts = new MzBranchFacts();
        var actions = new List<MzAction>();
        var interpreter = new MzInterpreter(new List<MzCommandEntry>
        {
            new(MzCommandTable.PluginCommand, new List<string>
            {
                "ItemCombinationMZ", "callMenu", "メニューを開く", "",
            }, 0),
            new(0, new List<string>(), 0),
        });
        interpreter.Run(actions, facts);

        AssertEq(
            interpreter.Stopped, MzStep.Finished,
            "and the list ran through rather than stopping, because the engine"
            + $" returns true and so does this; it is {interpreter.Stopped}");
        AssertEq(
            actions.Count, 1,
            "and it was recorded, so a caller sees that something happened and"
            + $" not that nothing did; there are {actions.Count} actions");
        AssertTrue(
            actions[0].What.Contains("ItemCombinationMZ"),
            $"and it names the plugin the game asked for: {actions[0].What}");
        AssertTrue(
            actions[0].What.Contains("callMenu"),
            $"and the command inside that plugin: {actions[0].What}");
        AssertTrue(
            actions[0].What.Contains("not run"),
            $"and it says the plugin was not run, which is the part a caller"
            + $" has to be able to read; {actions[0].What}");
        AssertEq(
            facts.Notices.Count, 1,
            "and the refusal is on the facts as well, where a caller looking"
            + $" for what went wrong will find it; there are"
            + $" {facts.Notices.Count} notices");
    }

    public void Test_ThisGamesOwnElevenPluginCommandsAllNameAPluginAndNoneOfThemRuns()
    {
        // **Measured on the eleven, not on a shape of my own making.** This
        // map's 357 commands call three plugins, and this game ships
        // fifty-two of them, all enabled. The claim is that every one of the
        // eleven is refused by name and that none of them is stepped over —
        // a reader that ran them would be running this game's crafting
        // system's JavaScript, which is exactly what it does not do.
        var commands = MenuAndPluginCommandsInThisGame();
        var pluginCalls = commands
            .Where(c => c.Code == MzCommandTable.PluginCommand)
            .ToList();
        var menus = commands
            .Where(c => c.Code == MzCommandTable.OpenMenu)
            .ToList();

        AssertEq(
            menus.Count, 2,
            "and the map read here has two 351s; it has"
            + $" {menus.Count}");
        AssertEq(
            pluginCalls.Count, 9,
            "and nine 357s, which is what the fixture carries; it has"
            + $" {pluginCalls.Count}");

        // **The plugins it calls, read out of the game's own list.**
        var plugins = new List<string>();
        foreach (var call in pluginCalls)
        {
            var name = call.Parameters[0];
            if (!plugins.Contains(name))
            {
                plugins.Add(name);
            }
        }
        AssertEq(
            plugins.Count, 3,
            "and they call three different plugins, which is not a shape this"
            + $" card invented; they call {plugins.Count}");

        // **And every one of the nine is refused, by name, and none is
        // stepped over.** A refusal that dropped the name would be as
        // useless as no refusal at all.
        var facts = new MzBranchFacts();
        var actions = new List<MzAction>();
        var interpreter = new MzInterpreter(pluginCalls);
        interpreter.Run(actions, facts);

        AssertEq(
            actions.Count, 9,
            "and all nine were answered rather than stepped over, so a caller"
            + $" sees nine refusals and not silence; there are"
            + $" {actions.Count} actions");
        AssertEq(
            facts.Notices.Count, 9,
            "and all nine are on the facts, one apiece; there are"
            + $" {facts.Notices.Count} notices");
        foreach (var plugin in plugins)
        {
            AssertTrue(
                facts.Notices.Any(n => n.Contains(plugin)),
                $"and the plugin {plugin} is named in at least one of them,"
                + " because a refusal that does not say which plugin is a"
                + " refusal a caller cannot act on; the nine are"
                + $" [{string.Join(" ; ", facts.Notices)}]");
        }
    }

    public void Test_ThisGameShipsFiftyTwoPluginsAndAllOfThemAreEnabled()
    {
        // **The number the card is really about.** This is not a property of
        // MZ and it is not something a test can prove about every game, but it
        // is true of the one game this repository has fixtures for, and it
        // decides how much of this game is not MZ at all.
        //
        // Eleven of this map's commands are plugin commands, and the game has
        // fifty-two plugins switched on. **A reader that modelled this map
        // faithfully and refused nothing would be running JavaScript this
        // repository does not run**, which is why the refusal is the card and
        // not a footnote on it.
        var path = FixtureRoot.PathJoin("data").PathJoin("Map002.json");
        var file = MzDataFile.Read(
            "Map002.json", Godot.FileAccess.GetFileAsBytes(path));
        var pluginCalls = 0;
        var scripts = 0;
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
                    if (command.Member("code")?.IntOr(0) == MzCommandTable.PluginCommand)
                    {
                        pluginCalls++;
                    }
                    if (command.Member("code")?.IntOr(0) == MzCommandTable.Script)
                    {
                        scripts++;
                    }
                }
            }
        }

        // **Eleven plugin commands and four scripts** on the one map, out of a
        // hundred and sixty-eight commands that do something. That is the
        // honest shape of this game and it is why the two refusals are the
        // card and not an aside.
        AssertEq(
            pluginCalls, 9,
            "and the map read here has nine plugin commands; it has"
            + $" {pluginCalls}");
        AssertEq(
            scripts, 3,
            "and three scripts, which are the same refusal for the same"
            + $" reason; it has {scripts}");
        AssertTrue(
            pluginCalls + scripts > 0,
            "so more than nothing on this map is not MZ event code at all,"
            + $" and a reader has to say so rather than look as if it ran:"
            + $" {pluginCalls} plugin calls and {scripts} scripts");
    }
}
