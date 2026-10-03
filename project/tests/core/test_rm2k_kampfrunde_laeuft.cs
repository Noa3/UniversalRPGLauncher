using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniversalRPG.Plugins;
using UniversalRPG.Rm2k.Interpreter;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And that a battle's own page runs, on the game's own commands.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is the wiring the previous commit said was
/// left.</strong> --
/// <strong>One hundred and six troops carry a 133 byte page and no
/// reader ran it.</strong>
/// </para>
/// <para>
/// <strong>And the battle page is an ordinary command list</strong>, --
/// <strong>so it runs on the same reader as a map page</strong>, --
/// <strong>and this asserts exactly that.</strong>
/// </para>
/// </remarks>
public partial class TestRm2kKampfrundeLaeuft : TestBase
{
    private const string Spiel = "E:/RPGMakerGames/Dragon Destiny";

    private static Rm2kEngineRuntime? Starte(out EnginePluginHost? pHost)
    {
        pHost = null;
        if (!File.Exists(Spiel + "/RPG_RT.ldb"))
        {
            return null;
        }

        pHost = new EnginePluginHost(
            BuiltInEnginePluginCatalog.CreateRuntimeRegistry());
        if (!pHost.Start(new PluginGameInfo
            {
                GameDirectory = Spiel,
                EngineId = EnginePluginIds.RpgMaker2000,
                Generation = "rm2k",
                DetectorScore = 3,
            }).Success)
        {
            pHost.Dispose();
            pHost = null;
            return null;
        }

        return (Rm2kEngineRuntime)pHost.Runtime!;
    }

    /// <summary>
    /// And the troop's page loads and runs to its end.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the assertion is on the game's own sentence</strong>,
    /// -- <strong>because a page that ran and produced no text would
    /// be a page that ran the wrong commands.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieKampfrundeLaeuftUndZeigtDenText()
    {
        var lauf = Starte(out var host);
        if (lauf == null)
        {
            return;
        }

        using var h = host!;
        var state = lauf.Simulation;
        var lauf2 = new Rm2kBattleRunner(state);

        AssertTrue(lauf2.Lade(state.DatabaseData, 1, out var warum),
            "**and troop one's page loads** -- and: " + warum);
        Console.WriteLine("geladen: " + lauf2.Laeuft);

        AssertTrue(lauf2.Laeuft,
            "**and the runner carries a page**");

        var schritte = 0;
        while (lauf2.Schritt() && schritte < 200)
        {
            schritte++;
        }

        Console.WriteLine("Schritte: " + schritte);
        var letzte = state.Diagnostics.Count > 0
            ? state.Diagnostics[state.Diagnostics.Count - 1]
            : "(leer)";
        Console.WriteLine("letzte Diagnose: " + letzte);

        AssertTrue(schritte > 0,
            "**and the page ran** -- and before this the troop's"
                + " 133 bytes were read into a dictionary and"
                + " nothing ever executed them");

        var text = string.Join(" | ", state.Diagnostics);
        AssertTrue(text.Contains("fled")
                || text.Contains("Nothing happened"),
            "**and the game's own sentences came out of the"
                + " page** -- and \"The monsters fled!\" was in"
                + " those bytes since the last commit, unread");
    }

    /// <summary>
    /// And a troop whose page does not read is named.
    /// </summary>
    public void Test_EineTrupeOhneSeiteSagtEs()
    {
        var lauf = Starte(out var host);
        if (lauf == null)
        {
            return;
        }

        using var h = host!;
        var state = lauf.Simulation;
        var lauf2 = new Rm2kBattleRunner(state);

        var ok = lauf2.Lade(state.DatabaseData, 9999, out var warum);
        Console.WriteLine("Trupe 9999: " + ok + "  -> " + warum);

        AssertTrue(!ok,
            "**and a troop that is not in the game does not"
                + " load**");
        AssertTrue(warum.Contains("9999"),
            "**and the refusal names the troop**");

        AssertTrue(!lauf2.Laeuft,
            "**and nothing is left running**");
    }
}
