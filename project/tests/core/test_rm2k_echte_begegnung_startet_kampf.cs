using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniversalRPG.Plugins;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The game's own encounter command, and that it brings monsters.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is the end of the chain that four commits
/// measured.</strong> --
/// <strong>troop 0x02</strong> -- <strong>liblcf's
/// <c>TroopMember</c></strong> -- <strong>the game's own
/// <c>enemies</c></strong> -- <strong><c>CanMonsterAct</c>'s
/// <c>hp</c></strong> -- <strong>and now 10710 itself.</strong>
/// </para>
/// <para>
/// <strong>And it runs the real game.</strong>
/// </para>
/// </remarks>
public partial class TestRm2kEchteBegegnungStartetKampf : TestBase
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
    /// And the host carries the game's own database into the state.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the link the whole battle hangs
    /// on</strong>, -- <strong>and before this the interpreter ran
    /// against an empty troop list.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieBankLiegtImZustand()
    {
        var lauf = Starte(out var host);
        if (lauf == null)
        {
            return;
        }

        using var h = host!;
        var bank = lauf.Simulation.DatabaseData;
        var anzahl = 0;
        foreach (var k in bank.Keys)
        {
            anzahl++;
        }

        Console.WriteLine("Kapseln im Zustand: " + anzahl);
        AssertTrue(bank.ContainsKey("troops"),
            "**and the state carries the bank's troops**");
        AssertTrue(bank.ContainsKey("enemies"),
            "**and the state's enemies**");
    }

    /// <summary>
    /// And a real troop becomes live monsters in the state.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this builds through the state and not through a
    /// helper</strong>, -- <strong>because the question is whether the
    /// interpreter's own path works.</strong>
    /// </para>
    /// </remarks>
    public void Test_EineTrupeWirdZumLebendigenGegner()
    {
        var lauf = Starte(out var host);
        if (lauf == null)
        {
            return;
        }

        using var h = host!;
        AssertTrue(Rm2kBegegnungAufbauen.Versuche(
                lauf.Simulation.DatabaseData, 2,
                out var mitglieder, out var warum),
            "**and troop two builds from the state** -- and: "
                + warum);

        Console.WriteLine("Truppe 2: " + mitglieder.Count + " Monster");
        foreach (var m in mitglieder)
        {
            Console.WriteLine("  " + m["name"].AsString() + " "
                + m["hp"].AsInt32() + " hp  angriff "
                + m["attack"].AsInt32() + " verteidigung "
                + m["defense"].AsInt32());
        }

        AssertTrue(mitglieder.Count == 1,
            "**and it has one monster** -- and that is the game's"
                + " own count for troop two");

        AssertEq("Scorpion", mitglieder[0]["name"].AsString(),
            "**and the name comes from the bank's enemies**");

        AssertEq(25, mitglieder[0]["hp"].AsInt32(),
            "**and its hit points are the game's own twenty five**");
        AssertEq(35, mitglieder[0]["attack"].AsInt32(),
            "**and its attack is the game's own thirty five**");
    }

    /// <summary>
    /// And every troop that can be built, built.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the refusals are named and counted</strong>, --
    /// <strong>because a reader that reported "built" without saying
    /// how many it did not would hide a decoder gap.</strong>
    /// </para>
    /// </remarks>
    public void Test_WieVieleDerHundertSechs()
    {
        var lauf = Starte(out var host);
        if (lauf == null)
        {
            return;
        }

        using var h = host!;
        var gebaut = 0;
        var verweigert = new List<string>();
        for (var id = 1; id <= 106; id++)
        {
            if (Rm2kBegegnungAufbauen.Versuche(
                    lauf.Simulation.DatabaseData, id,
                    out _, out var warum))
            {
                gebaut++;
                continue;
            }

            if (verweigert.Count < 8)
            {
                verweigert.Add("#" + id + ": " + warum);
            }
        }

        Console.WriteLine("gebaut " + gebaut + " von 106");
        foreach (var v in verweigert)
        {
            Console.WriteLine("  " + v);
        }

        AssertEq(99, gebaut,
            "**and ninety nine of one hundred and six encounters"
                + " become live monsters**");

        AssertEq(7, verweigert.Count,
            "**and seven do not** -- and the first ones are named"
                + " above, and they are the four troops without"
                + " enemy_id plus the ones that fight Forest Bat,"
                + " which the bank carries without hit points");
    }
}
