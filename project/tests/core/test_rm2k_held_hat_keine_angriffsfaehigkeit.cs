using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniversalRPG.Plugins;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And why the strike is still refused, measured.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the refusal says the hero has learned no skill with a
/// power</strong>, -- <strong>and that is a statement about this
/// game's data and not a fault in this repository.</strong>
/// </para>
/// <para>
/// <strong>And it is worth knowing whether it is true</strong>, --
/// <strong>because a hero with no attack skill in a game with three
/// hundred of them would point at something the parser is
/// missing.</strong>
/// </para>
/// </remarks>
public partial class TestRm2kHeldHatKeineAngriffsfaehigkeit : TestBase
{
    private const string Spiel = "E:/RPGMakerGames/Dragon Destiny";

    /// <summary>
    /// And the heroes, their levels and their skills.
    /// </summary>
    public void Test_DieHeldenUndIhreFaehigkeiten()
    {
        if (!File.Exists(Spiel + "/RPG_RT.ldb"))
        {
            return;
        }

        using var host = new EnginePluginHost(
            BuiltInEnginePluginCatalog.CreateRuntimeRegistry());
        if (!host.Start(new PluginGameInfo
            {
                GameDirectory = Spiel,
                EngineId = EnginePluginIds.RpgMaker2000,
                Generation = "rm2k",
                DetectorScore = 3,
            }).Success)
        {
            return;
        }

        var lauf = (Rm2kEngineRuntime)host.Runtime!;
        var bank = lauf.Simulation.DatabaseData;
        var helden = (Godot.Collections
            .Array<Godot.Collections.Dictionary>)bank["actors"];

        Console.WriteLine("Helden: " + helden.Count);
        var mitNiveau = 0;
        var mitFaehigkeiten = 0;
        for (var i = 0; i < helden.Count; i++)
        {
            var h = helden[i];
            var name = h["name"].AsString();
            var niveau = h.ContainsKey("initial_level")
                ? h["initial_level"].AsInt32() : -1;
            if (niveau > 0)
            {
                mitNiveau++;
            }

            // **Und  das  Lernfeld  ist  `skills`  bei  `0x3F`.**
            var hatGelernt = false;
            if (h.ContainsKey("unknown_fields"))
            {
                foreach (var f in (Godot.Collections
                    .Array<Godot.Collections.Dictionary>)
                    h["unknown_fields"])
                {
                    if (f["id"].AsInt32() == 0x3F)
                    {
                        hatGelernt = true;
                    }
                }
            }

            if (hatGelernt)
            {
                mitFaehigkeiten++;
            }

            Console.WriteLine($"  {name}  niveau={niveau}"
                + $"  lernfeld={(hatGelernt ? "ja" : "nein")}"
                + "  felder="
                + string.Join(",", new List<string> {
                    h.ContainsKey("initial_level") ? "niveau" : "",
                    h.ContainsKey("unknown_fields") ? "roh" : "",
                }.Where(x => x.Length > 0)));
        }

        Console.WriteLine($"mit Startniveau: {mitNiveau}"
            + "  mit Lernfeld: " + mitFaehigkeiten);

        AssertTrue(mitNiveau > 0,
            "**and the heroes carry a starting level**");

        AssertEq(7, mitFaehigkeiten,
            "**and all seven carry a skills field at 0x3F** --"
                + " and liblcf calls it `Array<Learning>`, and it"
                + " is where a hero's learned skills live, and"
                + " this repository reads it nowhere, and that is"
                + " why the strike above is refused with \"no"
                + " learned skill\" -- and the data is there and"
                + " the reader is not");
    }
}
