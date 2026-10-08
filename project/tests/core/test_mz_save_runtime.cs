using System;
using System.IO;
using UniversalRPG.Plugins;
using UniversalRPG.Tests.Framework;
using UniversalRPG.Web;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And a running game is written to a slot and read back from it.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is the whole point of the last three files</strong>: the
/// contents in the engine's shape, the file in the engine's format, and a
/// runtime that fills its own state back from them.
/// </para>
/// <para>
/// <strong>And the save folder is a scratch folder and not the game's
/// own.</strong> Measured: the engine's <c>fileDirectoryPath</c> is the
/// game's folder plus <c>save/</c> -- <strong>and an imported game is someone
/// else's folder, so a test that wrote into it would change data it does not
/// own, and a launcher that did would do the same.</strong>
/// </para>
/// </remarks>
public partial class TestMzSaveRuntime : TestBase
{
    private const string Projekt = "E:/RPGMakerGames/CamelliaCoronation-Win";

    /// <summary>And the game's own state survives a save and a load.</summary>
    public void Test_EinLaufendesSpielUeberlebtSpeichernUndLaden()
    {
        if (!Vorhanden())
        {
            return;
        }

        var ordner = NeuerOrdner();
        var host = new EnginePluginHost(
            BuiltInEnginePluginCatalog.CreateRuntimeRegistry());
        try
        {
            var gestartet = host.Start(new PluginGameInfo
            {
                GameDirectory = Projekt,
                EngineId = EnginePluginIds.RpgMakerMz,
                Generation = "mz",
                DetectorScore = 850,
            });
            AssertTrue(gestartet.Success,
                "**and the project starts** -- and it said: "
                    + gestartet.Error?.Message);
            if (host.Runtime is not MzEngineRuntime lauf)
            {
                AssertTrue(false, "**and the host built an MZ runtime**");
                return;
            }

            // **And the save goes into a scratch folder**, so that the
            // game's own folder is not touched.
            lauf.SaveDirectory = ordner;
            AssertEq(lauf.Spielstaende().Count, 0,
                "**and no slot has a save yet**");

            // **And the opening is let through**, because it holds the map
            // while it speaks and a test that saved during it would save a
            // dialogue nobody can read back.
            MzTestOpening.Durchlassen(lauf);

            // **And now the game is changed**, in a way a load can prove:
            // a switch on, a variable set, and the player moved.
            lauf.Facts.Switches[7] = true;
            lauf.Facts.Variables[3] = 99;
            lauf.Facts.SelfSwitches["2,5,A"] = true;
            var (x, y) = (lauf.PlayerX, lauf.PlayerY);
            lauf.Facts.Player.StandAt(lauf.CurrentMapId, x + 1, y, 6);

            var gespeichert = lauf.Speichern(1);
            Console.WriteLine($"MZ save: {gespeichert}");
            AssertTrue(lauf.SaveProblem.Length == 0,
                "**and the save is written** -- and it said: " + lauf.SaveProblem);
            AssertTrue(lauf.SpielstandVorhanden(1),
                "**and the slot now has a file** -- which is what "
                    + "`savefileExists` asks");
            AssertTrue(File.Exists(MzSaveStore.PathOf(ordner, "global")),
                "**and the file info is written too** -- because that is "
                    + "what a load screen draws");

            // **And the game is changed again, the other way.**
            lauf.Facts.Switches[7] = false;
            lauf.Facts.Variables[3] = 0;
            lauf.Facts.SelfSwitches.Clear();
            lauf.Facts.Player.StandAt(lauf.CurrentMapId, 1, 1, 8);

            var geladen = lauf.Laden(1);
            Console.WriteLine($"MZ save: {geladen}");
            AssertTrue(lauf.SaveProblem.Length == 0,
                "**and the load works** -- and it said: " + lauf.SaveProblem);

            // **And every value is the one that was saved.**
            AssertTrue(lauf.Facts.Switches.TryGetValue(7, out var an) && an,
                "**and the switch is on again** -- and a load that missed it "
                    + "would leave a game that has forgotten where it was");
            AssertEq(lauf.Facts.Variables[3], 99,
                "**and the variable is 99 again**");
            AssertTrue(lauf.Facts.SelfSwitches.ContainsKey("2,5,A"),
                "**and the self switch came back by its own key**");
            AssertEq(lauf.PlayerX, x + 1,
                "**and the player is where the save left them**");
            AssertEq(lauf.PlayerY, y,
                "**and not where the test put them afterwards**");

            // **And the file info is at the slot's own index.**
            AssertTrue(MzSaveStore.TryLoad(
                    ordner, MzSaveStore.GlobalName, out var global, out var problem),
                "**and the info file reads back** -- and the store said: "
                    + problem);
            AssertTrue(global!.Kind == MzKind.Array,
                "**and it is an array indexed by slot** -- measured: "
                    + "`saveGame` sets `this._globalInfo[savefileId]` and "
                    + "saves the whole array");
            AssertTrue(global.Items.Count > 1,
                "**and it reaches to slot one**");
            AssertTrue(global.Items[1].Kind == MzKind.Object,
                "**and slot one holds an info object** -- and a reader that "
                    + "packed the entries together would put slot seven's "
                    + "playtime under slot two");
            AssertTrue(
                global.Items[1].Member("playtime")?.StringOr("").Length == 8,
                "**and the playtime is hh:mm:ss** -- measured: "
                    + "`playtimeText` pads each field to two digits; it said "
                    + $"{global.Items[1].Member("playtime")?.StringOr("")}");
        }
        finally
        {
            host.Dispose();
            Aufraeumen(ordner);
        }
    }

    /// <summary>
    /// And a slot that has nothing is a refusal, not an empty game.
    /// </summary>
    public void Test_EinLeererPlatzIstEineAbsage()
    {
        if (!Vorhanden())
        {
            return;
        }

        var ordner = NeuerOrdner();
        var host = new EnginePluginHost(
            BuiltInEnginePluginCatalog.CreateRuntimeRegistry());
        try
        {
            host.Start(new PluginGameInfo
            {
                GameDirectory = Projekt,
                EngineId = EnginePluginIds.RpgMakerMz,
                Generation = "mz",
                DetectorScore = 850,
            });
            if (host.Runtime is not MzEngineRuntime lauf)
            {
                AssertTrue(false, "**and the host built an MZ runtime**");
                return;
            }

            lauf.SaveDirectory = ordner;
            MzTestOpening.Durchlassen(lauf);

            var bericht = lauf.Laden(9);
            AssertTrue(lauf.SaveProblem.Length > 0,
                "**and loading an empty slot fails** -- and it said: " + bericht);
            AssertTrue(lauf.SaveProblem.Contains("no save"),
                "**and it says why**");
            AssertTrue(lauf.Facts.Switches.Count == 0,
                "**and it did not wipe the game it was loading into** -- and "
                    + "a reader that read a missing file as an empty save "
                    + "would have taken the switches away");

            // **And a slot outside the engine's range is refused by name.**
            var weit = lauf.Speichern(21);
            AssertTrue(lauf.SaveProblem.Contains("21"),
                "**and slot twenty-one is refused** -- and it names the "
                    + "number, because `maxSavefiles` returns 20; it said: "
                    + weit);
        }
        finally
        {
            host.Dispose();
            Aufraeumen(ordner);
        }
    }

    private static bool Vorhanden() =>
        Directory.Exists(Projekt + "/data");

    private static string NeuerOrdner() =>
        Path.Combine(
            Path.GetTempPath(), "urpg-mz-runtime-save-" + Guid.NewGuid().ToString("N"));

    private static void Aufraeumen(string pOrdner)
    {
        try
        {
            if (Directory.Exists(pOrdner))
            {
                Directory.Delete(pOrdner, true);
            }
        }
        catch (IOException)
        {
            // **And a scratch folder that will not go is not worth failing a
            // test over.**
        }
    }
}
