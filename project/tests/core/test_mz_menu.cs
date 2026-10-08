using System;
using System.IO;
using System.Linq;
using UniversalRPG.Plugins;
using UniversalRPG.Tests.Framework;
using UniversalRPG.Web;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And <c>351 Open Menu</c> opens a menu that can be seen and used.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the state existed and nobody read it.</strong> Measured:
/// <c>grep -rn "MzMenuState"</c> finds one declaration, one write in
/// <c>MzCommands</c> (<c>pFacts.Menu = MzMenuState.Open</c>) and one reader in
/// a test -- <strong>and the write itself landed on a facts object the runtime
/// stopped reading after the first map change</strong>, because <c>Repaint()</c>
/// replaces <c>Facts</c> with a fresh copy while <c>_facts</c> -- the object the
/// commands run against -- stayed where it was. <strong>So a menu an event
/// opened was never seen.</strong>
/// </para>
/// <para>
/// <strong>And the menu's own words and shape are measured, not chosen</strong>,
/// in the project's <c>rmmz_windows.js</c> and <c>rmmz_scenes.js</c>:
/// </para>
/// <code>
/// Window_MenuCommand.prototype.makeCommandList = function() {
///     this.addMainCommands();     // item, skill, equip, status
///     this.addFormationCommand(); // formation
///     this.addOriginalCommands(); // the plugins
///     this.addOptionsCommand();   // options
///     this.addSaveCommand();      // save
///     this.addGameEndCommand();   // gameEnd
/// };
/// Window_MenuCommand.prototype.needsCommand = function(name) {
///     const table = ["item", "skill", "equip", "status", "formation", "save"];
///     const index = table.indexOf(name);
///     if (index &gt;= 0) return $dataSystem.menuCommands[index];
///     return true;
/// };
/// Scene_Menu.prototype.commandWindowRect = function() {
///     const ww = this.mainCommandWidth();                                  // 240
///     const wh = this.mainAreaHeight() - this.goldWindowRect().height;
///     const wx = this.isRightInputMode() ? Graphics.boxWidth - ww : 0;
///     const wy = this.mainAreaTop();                                       // 52
///     return new Rectangle(wx, wy, ww, wh);
/// };
/// </code>
/// <para>
/// <strong>and the words come from the game's own <c>terms.commands</c></strong>
/// -- measured, <c>item = commands[4]</c>, <c>save = commands[9]</c>,
/// <c>gameEnd = commands[10]</c>, <c>options = commands[11]</c> -- <strong>not
/// from two strings this reader writes itself.</strong>
/// </para>
/// </remarks>
public partial class TestMzMenu : TestBase
{
    private const string Projekt = "E:/RPGMakerGames/CamelliaCoronation-Win";

    private const string Skies =
        "D:/NextCloud/Games/PornGames/SkiesInflateableAdventure";

    private static bool Vorhanden() =>
        File.Exists(Projekt + "/data/System.json")
        && File.Exists(Projekt + "/data/Map002.json");

    private static MzEngineRuntime Start(string pOrdner)
    {
        var host = new EnginePluginHost(
            BuiltInEnginePluginCatalog.CreateRuntimeRegistry());
        var started = host.Start(new PluginGameInfo
        {
            GameDirectory = pOrdner,
            EngineId = EnginePluginIds.RpgMakerMz,
            Generation = "mz",
            DetectorScore = 850,
        });
        if (!started.Success)
        {
            host.Dispose();
            throw new InvalidOperationException(
                $"the MZ game did not start: {started.Error?.Message}");
        }
        return (MzEngineRuntime)host.Runtime!;
    }

    /// <summary>
    /// And the menu's commands come out of the game's own switches.
    /// </summary>
    public void Test_DieBefehleKommenAusDenSchalternDesSpiels()
    {
        if (!Vorhanden())
        {
            return;
        }

        var runtime = Start(Projekt);
        Console.WriteLine($"MZ menu: [{string.Join(", ", runtime.MenuCommands)}] "
            + $"from symbols [{string.Join(", ", runtime.MenuCommandSymbols)}]");
        var frage = "?";
        Console.WriteLine($"MZ menu: the game's own words are "
            + $"item='{runtime.Begriff(4, frage)}' save='{runtime.Begriff(9, frage)}' "
            + $"gameEnd='{runtime.Begriff(10, frage)}' "
            + $"options='{runtime.Begriff(11, frage)}'");

        // **Und gemessen an Camellia: `menuCommands = [true, false, false,
        // false, false, true]`** -- also `Item` aus dem vierten Schalter und
        // `Save` aus dem sechsten, und `Options` und `Game End` fragen einen
        // Namen, der nicht in der Tabelle steht.
        AssertTrue(runtime.MenuCommands.Contains("Item"),
            "**and the fourth switch turns Item on**");
        AssertFalse(runtime.MenuCommands.Contains("Skill"),
            "**and the first switch that is off turns Skill off**");
        AssertFalse(runtime.MenuCommands.Contains("Equip"),
            "and Equip too");
        AssertFalse(runtime.MenuCommands.Contains("Status"),
            "and Status too");
        AssertFalse(runtime.MenuCommands.Contains("Formation"),
            "and Formation, which is the sixth table entry");
        AssertTrue(runtime.MenuCommands.Contains("Options"),
            "**and Options asks a name outside the table and gets true**");
        AssertTrue(runtime.MenuCommands.Contains("Save"),
            "and Save comes from the sixth switch");

        // **Und die Reihenfolge ist die von `makeCommandList`.**
        AssertEq(runtime.MenuCommands[0], "Item",
            "**and Item stands first, because addMainCommands runs first**");
        AssertEq(runtime.MenuCommands[runtime.MenuCommands.Count - 1], "Game End",
            "**and Game End last, because addGameEndCommand runs last**");
    }

    /// <summary>
    /// And the menu is where <c>Scene_Menu</c> puts it.
    /// </summary>
    public void Test_DieFensterStehenWoSceneMenuSieHinstellt()
    {
        if (!File.Exists(Skies + "/data/System.json"))
        {
            return;
        }

        var runtime = Start(Skies);
        Console.WriteLine($"MZ menu: on {runtime.ScreenWidth}x{runtime.ScreenHeight} "
            + $"the command window is ({runtime.MenuWindow.X},"
            + $"{runtime.MenuWindow.Y},{runtime.MenuWindow.Width},"
            + $"{runtime.MenuWindow.Height})");
        Console.WriteLine($"MZ menu: the gold window is ({runtime.MenuGoldWindow.X},"
            + $"{runtime.MenuGoldWindow.Y},{runtime.MenuGoldWindow.Width},"
            + $"{runtime.MenuGoldWindow.Height})");
        Console.WriteLine($"MZ menu: the status window is ({runtime.MenuStatusWindow.X},"
            + $"{runtime.MenuStatusWindow.Y},{runtime.MenuStatusWindow.Width},"
            + $"{runtime.MenuStatusWindow.Height})");

        // **Und gerechnet aus den eigenen Zahlen des Motors:** `mainAreaTop`
        // ist `buttonAreaBottom = 52`, `mainAreaHeight` ist `720 - 52 = 668`,
        // und das Goldfenster ist `1 * 36 + 12 * 2 = 60`.
        AssertEq(runtime.MenuWindow.X, runtime.ScreenWidth - 240,
            "**and the command window stands on the right, 240 wide**");
        AssertEq(runtime.MenuWindow.Y, 52,
            "**and starts where the button area ends**");
        AssertEq(runtime.MenuGoldWindow.Height, 36 + 12 * 2,
            "and the gold window is one line plus the padding");
        // **Und das Befehlsfenster endet da, wo das Goldfenster anfaengt** --
        // nicht beide auf derselben Linie: `commandWindowRect` rechnet
        // `mainAreaHeight() - goldWindowRect().height`, **also hoert es
        // genau eine Goldfensterhoehe ueber `mainAreaBottom` auf**, und das
        // Goldfenster selbst endet auf `mainAreaBottom`.
        AssertEq(runtime.MenuWindow.Y + runtime.MenuWindow.Height,
            runtime.MenuGoldWindow.Y,
            "**and the command window ends where the gold window begins**");
        AssertEq(runtime.MenuGoldWindow.Y + runtime.MenuGoldWindow.Height,
            runtime.ScreenHeight,
            "**and the gold window ends on mainAreaBottom, which is the "
            + "screen's own bottom**");
        AssertEq(runtime.MenuStatusWindow.Width, runtime.ScreenWidth - 240,
            "and the status window has everything the command window does not");
        AssertEq(runtime.MenuStatusWindow.X, 0,
            "and it stands on the left when the command window is right");
    }

    /// <summary>
    /// And the menu opens, moves, and closes.
    /// </summary>
    public void Test_DasMenueGehtAufBewegtSichUndZu()
    {
        if (!Vorhanden())
        {
            return;
        }

        var runtime = Start(Projekt);
        runtime.Repaint();
        Console.WriteLine($"MZ menu: visible={runtime.MenuVisible} "
            + $"holdsPlayer={runtime.MenuHoldsPlayer}");
        AssertFalse(runtime.MenuVisible, "a fresh game has no menu open");
        AssertFalse(runtime.MenuHoldsPlayer, "and the player is free");

        // **Und so oeffnet der Befehl es**, auf denselben Tatsachen, die die
        // Laufzeit liest.
        runtime.OpenMenu();
        runtime.Tick();
        Console.WriteLine($"MZ menu: after 351 visible={runtime.MenuVisible}, "
            + $"index={runtime.MenuIndex}");
        AssertTrue(runtime.MenuVisible, "**and 351 opens a menu a reader sees**");
        AssertTrue(runtime.MenuHoldsPlayer,
            "**and the map stands still while a scene is over it**");
        AssertEq(runtime.MenuIndex, 0,
            "**and the cursor starts at the top, which is initCommandPosition**");

        // **Und die Tasten sind die von `Scene_Menu`.**
        runtime.SubmitInput(UniversalRPG.Rm2k.Input.Rm2kInputAction.MoveDown);
        Console.WriteLine($"MZ menu: after one step down index={runtime.MenuIndex} "
            + $"selection='{runtime.MenuSelection}'");
        AssertEq(runtime.MenuIndex, 1, "and down moves the cursor");
        AssertEq(runtime.MenuSelection, runtime.MenuCommands[1],
            "and the selection follows it");

        runtime.SubmitInput(UniversalRPG.Rm2k.Input.Rm2kInputAction.MoveUp);
        AssertEq(runtime.MenuIndex, 0, "and up moves it back");

        // **Und der Motor laeuft um**, denn `processCursorMove` klemmt den
        // Zeiger in die Liste und haelt ihn nicht am Rand an.
        runtime.SubmitInput(UniversalRPG.Rm2k.Input.Rm2kInputAction.MoveUp);
        Console.WriteLine($"MZ menu: up from the top lands on "
            + $"{runtime.MenuIndex} of {runtime.MenuCommands.Count}");
        AssertEq(runtime.MenuIndex, runtime.MenuCommands.Count - 1,
            "**and up from the top wraps to the bottom**");

        runtime.SubmitInput(UniversalRPG.Rm2k.Input.Rm2kInputAction.Confirm);
        Console.WriteLine($"MZ menu: confirming said '{runtime.MenuReport}'");
        AssertTrue(runtime.MenuReport.Length > 0,
            "**and the command is reported, which is what this reader can do**");

        runtime.SubmitInput(UniversalRPG.Rm2k.Input.Rm2kInputAction.Cancel);
        Console.WriteLine($"MZ menu: after cancel visible={runtime.MenuVisible}");
        AssertFalse(runtime.MenuVisible,
            "**and cancel closes it, which is popScene**");
        AssertFalse(runtime.MenuHoldsPlayer, "and the player walks again");

        // **Und ein Menue, das zum zweiten Mal aufgeht, faengt wieder oben
        // an** -- `initCommandPosition` setzt `_lastCommandSymbol = null`.
        runtime.OpenMenu();
        runtime.SubmitInput(UniversalRPG.Rm2k.Input.Rm2kInputAction.MoveDown);
        runtime.SubmitInput(UniversalRPG.Rm2k.Input.Rm2kInputAction.MoveDown);
        var zweimal = runtime.MenuIndex;
        runtime.CloseMenu();
        runtime.OpenMenu();
        runtime.Tick();
        Console.WriteLine($"MZ menu: it stood on {zweimal} before, and on "
            + $"{runtime.MenuIndex} when it opened again");
        AssertEq(runtime.MenuIndex, 0,
            "**and a menu that opens remembers nothing from the last time**");
    }
}
