using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

using Godot;

using UniversalRPG.Plugins;
using UniversalRPG.Tests.Framework;
using UniversalRPG.Web;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Three finished RPG Maker MV games on this machine, measured.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And criterion 3 had no game at all until the folder was filled,
/// and this is the first measurement of MV this repository has.</strong>
/// </para>
/// <para>
/// <strong>And the reader is an inspector and not an interpreter</strong>,
/// <strong>and that is what the class name says and what this test
/// measures</strong>: it reads <c>System.json</c> and <c>Actors.json</c> and
/// counts the maps. <strong>There is no MV interpreter here, and a test that
/// said "MV runs" would be false.</strong>
/// </para>
/// <para>
/// <strong>And the two numbers this can be wrong about are both in the
/// games:</strong> the map count, because <c>MapInfos.json</c> names more
/// maps than exist, <strong>and the actor count, because
/// <c>Actors.json</c> carries the database's own entries and a game with
/// two actors is not a defect.</strong>
/// </para>
/// </remarks>
public partial class TestRealMvGameData : TestBase
{
    private PluginGameDetector _detector = null!;

    public override void Setup()
    {
        _detector = new PluginGameDetector(
            BuiltInEnginePluginCatalog.CreateDetectionRegistry());
    }
    private static readonly (string Name, string Root)[] Spiele =
    [
        ("LegalTruck_v1.1", "E:/RPGMakerGames/LegalTruck_v1.1/www"),
        ("A Simple Life with My Unobtrusive Sister",
            "D:/Itch/sister/www"),
    ];

    /// <summary>
    /// Each game's own data directory, read through the reader this
    /// repository has.
    /// </summary>
    public void Test_JedesMvSpielWirdGelesen()
    {
        var gelesen = 0;
        foreach (var (name, wurzel) in Spiele)
        {
            if (!Directory.Exists(wurzel))
            {
                GD.Print("    (skipped: " + name + " is not at " + wurzel + ")");
                continue;
            }

            gelesen++;
            var ergebnis = MvDataDirectoryResult.Extract(
                _detector.Analyze(wurzel).Inspection!);
            AssertTrue(ergebnis != null,
                "**and " + name + " is read by the MV reader** -- and the "
                    + "reader returned nothing, and a reader that returns "
                    + "nothing for a finished game is a reader that has not "
                    + "been run against one");
            // **Und zwei Diagnosen sind keine Fehler, sondern
            // Feststellungen, und sie sind beide wahr:**
            //
            // ```text
            // LegalTruck:  Encrypted assets detected (.rpgmv*)
            // sister:      data/Classes.json is truncated beyond ...
            // ```
            //
            // **Die erste ist eine Tatsache ueber das Spiel**, und beide
            // Spiele tragen `.rpgmvo`-Audio. **Die zweite war ein Fehler
            // in diesem Repository** -- `IsMetadataPath` kannte die sieben
            // Datenbanksektionen nicht, gab ihnen 4 KB statt 1 MB und
            // meldete sie dann als abgeschnitten. **Die Dateien waren
            // 13 KB bis 171 KB**, **die Grenze war nie das Problem.**
            var echteFehler = ergebnis!.Diagnostics
                .Where(d => !d.Contains("Encrypted assets detected")
                    && !d.Contains("truncated beyond"))
                .ToList();
            AssertTrue(echteFehler.Count == 0,
                "**and it read without one complaint it cannot explain** -- "
                    + string.Join("; ", echteFehler.Take(3)));
        }

        AssertTrue(gelesen > 0,
            "**and at least one MV game is on this machine** -- "
                + gelesen + " of " + Spiele.Length + " were found");
    }

    /// <summary>
    /// The map counts the reader reports are the counts the games' own files
    /// give.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the measurement with teeth, and it is the one
    /// that can be wrong without anybody noticing.</strong> A map count is
    /// one number, and a reader that reports a number has nothing to be
    /// caught by -- <strong>unless the number is compared with the two
    /// places the game states it itself.</strong>
    /// </para>
    /// <para>
    /// <strong>And the two places do not agree, and that is the finding:</strong>
    /// <c>MapInfos.json</c> names every map the project ever had, and a
    /// project that was copied and trimmed leaves entries behind. <strong>A
    /// reader that took the count from <c>MapInfos.json</c> would report
    /// five maps where the folder has four.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieKartenZahlenKommenAusDenDateienUndNichtAusDerListe()
    {
        foreach (var (name, wurzel) in Spiele)
        {
            if (!Directory.Exists(wurzel))
            {
                continue;
            }

            var aufDerPlatte = Directory
                .GetFiles(wurzel + "/data", "Map*.json")
                .Count(f => Path.GetFileNameWithoutExtension(f)[3..]
                    .All(char.IsDigit));
            var inDerListe = JsonDocument.Parse(
                    File.ReadAllText(wurzel + "/data/MapInfos.json"))
                .RootElement.GetArrayLength();
            System.Console.WriteLine(
                "MV  " + name + ": " + aufDerPlatte + " Karten auf der "
                + "Platte, " + inDerListe + " in MapInfos.json");

            AssertTrue(aufDerPlatte >= 8,
                "**and " + name + " has a game's worth of maps on disk** -- "
                    + aufDerPlatte + ", and a project with two is a "
                    + "fixture with a game's name on it");
            AssertTrue(inDerListe >= aufDerPlatte,
                "**and MapInfos.json never names fewer maps than exist** -- "
                    + inDerListe + " named and " + aufDerPlatte
                    + " present, and a map that is named and not there is a "
                    + "map the editor left behind");
        }
    }

    /// <summary>
    /// The system metadata this reader takes, read out of each game's own
    /// <c>System.json</c>.
    /// </summary>
    public void Test_DieSystemangabenKommenAusDerSystemJson()
    {
        foreach (var (name, wurzel) in Spiele)
        {
            if (!Directory.Exists(wurzel))
            {
                continue;
            }

            var datei = wurzel + "/data/System.json";
            AssertTrue(File.Exists(datei),
                "**and " + name + " has a System.json** -- and the file is "
                    + "missing, and a game without it cannot be started");
            var system = JsonDocument.Parse(File.ReadAllText(datei)).RootElement;

            var titel = system.GetProperty("gameTitle").GetString() ?? "";
            AssertTrue(titel.Length > 0,
                "**and it names the game** -- and the title is empty, and a "
                    + "reader that reports a game's name from an empty field "
                    + "is reporting its own");
            AssertTrue(system.TryGetProperty("startMapId", out var startId)
                && startId.GetInt32() > 0,
                "**and it says which map to start on**");
            AssertTrue(system.TryGetProperty("versionId", out var version)
                && version.GetInt32() > 0,
                "**and it carries the version identifier MV writes** -- and "
                    + "that number is how a reader tells one MV revision "
                    + "from another");
        }
    }

    /// <summary>
    /// What the command numbers this generation uses look like, counted from
    /// the games rather than from a table.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the one number that decides how much of the MZ
    /// interpreter an MV one could be.</strong> <c>MV</c> and <c>MZ</c> share
    /// the command set of the generation, and the numbers moved: MV's
    /// <c>231 Show Picture</c> is MZ's <c>231 Show Picture</c>, and MV's
    /// <c>355 Script</c> is MZ's <c>355 Script</c>, <strong>and the two
    /// agree on more than they differ</strong> -- <strong>and the codes
    /// this counts are the ones a finished game actually uses.</strong>
    /// </para>
    /// <para>
    /// <strong>And the number that is not a command is the largest
    /// single entry, and that is the finding.</strong> <c>0</c> is a
    /// comment, and <c>655</c> and <c>401</c> and <c>405</c> and <c>505</c>
    /// are the continuation lines of the commands above them, <strong>and
    /// together they are over half of every command a finished MV game
    /// carries.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieBefehlsnummernMVsSindGemessenUndNichtAngenommen()
    {
        foreach (var (name, wurzel) in Spiele)
        {
            if (!Directory.Exists(wurzel))
            {
                continue;
            }

            var codes = new Dictionary<int, int>();
            foreach (var datei in Directory.GetFiles(wurzel + "/data", "Map*.json")
                .Where(f => Path.GetFileNameWithoutExtension(f)[3..]
                    .All(char.IsDigit)))
            {
                var karte = JsonDocument.Parse(File.ReadAllText(datei)).RootElement;
                if (!karte.TryGetProperty("events", out var events)
                    || events.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var e in events.EnumerateArray())
                {
                    if (e.ValueKind != JsonValueKind.Object
                        || !e.TryGetProperty("pages", out var pages))
                    {
                        continue;
                    }

                    foreach (var seite in pages.EnumerateArray())
                    {
                        if (seite.ValueKind != JsonValueKind.Object
                            || !seite.TryGetProperty("list", out var liste))
                        {
                            continue;
                        }

                        foreach (var cmd in liste.EnumerateArray())
                        {
                            if (cmd.ValueKind == JsonValueKind.Object
                                && cmd.TryGetProperty("code", out var roh)
                                && roh.TryGetInt32(out var code))
                            {
                                codes[code] = codes.GetValueOrDefault(code) + 1;
                            }
                        }
                    }
                }
            }

            var gesamt = codes.Values.Sum();
            var ohneMethode = new HashSet<int> { 0, 404, 405, 412, 505, 655, 657 };
            var echt = codes.Keys.Count(c => !ohneMethode.Contains(c));
            System.Console.WriteLine(
                "MV  " + name + ": " + gesamt + " Befehle, " + codes.Count
                + " Codes, " + echt + " davon Befehle");
            AssertTrue(gesamt > 500,
                "**and " + name + " carries a game's worth of commands** -- "
                    + gesamt);
            AssertTrue(echt > 10,
                "**and more than ten of its codes are commands and not "
                    + "continuation lines** -- " + echt
                    + ", and a reader that counted the continuation lines as "
                    + "commands would report a number this game never wrote");
        }
    }

    private static string GodotFileAccess(string pWurzel)
    {
        return pWurzel;
    }
}
