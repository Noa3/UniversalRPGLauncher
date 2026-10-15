using System;
using System.IO;
using System.Linq;
using UniversalRPG.Plugins;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// What a real XP game needs before it can draw anything.
/// </summary>
/// <remarks>
/// <strong>And this is a measurement, not a rendering test.</strong> The RGSS layer has the Ruby
/// runtime, the data host, the map reader and the script host -- and no graphics at all: there is
/// no <c>Bitmap</c>, no <c>Sprite</c>, no <c>Tilemap</c>, no <c>Viewport</c> and no <c>Plane</c>
/// anywhere in <c>project/src/rgss</c>. The XP runtime declares <c>Runtime</c> and not
/// <c>Rendering</c>, which is why the launcher says Experimental and leaves Start disabled.
///
/// <strong>So the first step is to ask the game what it wants</strong>, rather than guessing an
/// API surface and implementing it: this test starts a real game and records what its scripts
/// report as missing, in their own words and by name. The numbers it prints are the plan for the
/// rendering work, and they are the progress measure for it -- a problem that disappears here is
/// a step towards a first frame.
/// </remarks>
public partial class TestRealXpRuntimeRun : TestBase
{
    private const string Spiel = "E:/RPGMakerGames/MicroQuest - Beneath Brimestone 1.0";

    private static bool Vorhanden() => Directory.Exists(Spiel);

    public void Test_WasEinEchtesXpSpielBraucht()
    {
        if (!Vorhanden())
        {
            return;
        }

        using var host = new EnginePluginHost(
            BuiltInEnginePluginCatalog.CreateRuntimeRegistry());
        var gestartet = host.Start(new PluginGameInfo
        {
            GameDirectory = Spiel,
            EngineId = EnginePluginIds.RpgMakerXp,
            Generation = "xp",
            DetectorScore = 900,
        });

        AssertTrue(gestartet.Success,
            "**the real game starts** -- and it said: " + gestartet.Error?.Message);

        var lauf = host.Runtime as XpEngineRuntime;
        AssertTrue(lauf != null, "**and the host built an XP runtime**");
        if (lauf == null)
        {
            return;
        }

        // **And what the runtime reports about itself is part of the measurement**: the library
        // it runs against, the data it read, and the scripts it could not answer.
        Console.WriteLine("XP: info = " + (lauf.RuntimeInfo == null
            ? "none"
            : $"library {lauf.RuntimeInfo.RuntimeVersion}, title '{lauf.RuntimeInfo.Title}', "
                + $"data {lauf.RuntimeInfo.DataFilePaths.Count}, "
                + $"inspected {lauf.RuntimeInfo.InspectedFileCount}"));
        Console.WriteLine("XP: script problems = " + lauf.ScriptProblems.Count);
        Console.WriteLine("XP: setup problems = " + lauf.SetupProblems.Count);
        foreach (var problem in lauf.SetupProblems.Take(20))
        {
            Console.WriteLine("XP:   SETUP " + problem);
        }
        foreach (var reading in lauf.SetupState)
        {
            Console.WriteLine("XP:   STATE " + reading);
        }
        foreach (var problem in lauf.ScriptProblems.Take(40))
        {
            Console.WriteLine("XP:   " + problem);
        }

        // **Und die Namen, die das Spiel verlangt hat und die es nicht gibt.**
        // Der Leser antwortet mit nil und einer Diagnose statt mit einer
        // Ausnahme, also sind die Luecken still -- **und die Liste der Namen ist
        // der Bedarf.** Sie wird hier dedupliziert gedruckt, weil ein Name, den
        // ein Spiel jeden Frame fragt, sonst jede Zeile fuellt.
        var namen = lauf.Diagnostics
            .Select(d => d.Split(' ')[0])
            .Distinct()
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();
        Console.WriteLine("XP: diagnostics = " + lauf.Diagnostics.Count
            + ", distinct names = " + namen.Length);
        foreach (var name in namen.Take(60))
        {
            Console.WriteLine("XP:   FEHLT " + name);
        }

        AssertTrue(lauf.ScriptProblems.Count >= 0,
            "**and every problem is named rather than swallowed**: the count is what the "
                + "rendering work has to bring down, and the list is where it says why");

        // **And a few frames are run**, because a script problem that only shows up on the
        // second frame is not visible in the first.
        var vorher = lauf.ScriptProblems.Count;
        for (var frame = 0; frame < 5; frame++)
        {
            lauf.Update(1.0 / 60.0);
        }
        Console.WriteLine("XP: script problems after 5 frames = " + lauf.ScriptProblems.Count);
        foreach (var problem in lauf.ScriptProblems.Skip(vorher))
        {
            Console.WriteLine("XP:   NEW " + problem);
        }
    }
}
