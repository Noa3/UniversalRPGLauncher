using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using UniversalRPG.Rm2k.Parser;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And how this game uses its menu.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the menu key opened a menu and nothing showed
/// it</strong>, -- <strong>and that was a launcher gap</strong>, --
/// <strong>and this measures what the game asks the menu to
/// do</strong>.
/// </para>
/// </remarks>
public partial class TestRm2kMenueGemessen : TestBase
{
    private const string Spiel = "E:/RPGMakerGames/Dragon Destiny";

    /// <summary>
    /// And the menu commands the game writes.
    /// </summary>
    public void Test_DieMenuebefehleDesSpiels()
    {
        if (!Directory.Exists(Spiel))
        {
            return;
        }

        var parser = new Rm2kParser();
        var zaehler = new SortedDictionary<int, int>();
        var karten = 0;
        foreach (var datei in Directory.GetFiles(Spiel, "Map*.lmu"))
        {
            var karte = parser.ParseMap(datei);
            if (!karte.Success || !karte.Data.ContainsKey("events"))
            {
                continue;
            }

            var ereignisse = karte.Data["events"].AsGodotArray();
            if (ereignisse.Count == 0)
            {
                continue;
            }

            karten++;
            foreach (var e in ereignisse)
            {
                if (e.VariantType
                    != Godot.Variant.Type.Dictionary)
                {
                    continue;
                }

                var seiten = e.AsGodotDictionary()
                    .GetValueOrDefault("pages",
                        default(Godot.Variant));
                if (seiten.VariantType
                    != Godot.Variant.Type.Array)
                {
                    continue;
                }

                foreach (var seite in seiten.AsGodotArray())
                {
                    // **Und  der  Schluessel  heisst  `commands`  und
                    //  nicht  `event_commands`.**
                    //
                    // **Und  mein  erster  Zaehlversuch  las
                    //  `event_commands`** -- **und  fand  nichts** --
                    // **und  das  war  dasselbe  Problem  wie  bei
                    //  `11110`:  ein  geratener  Schluessel.**
                    var befehle = seite.AsGodotDictionary()
                        .GetValueOrDefault("commands",
                            default(Godot.Variant));
                    if (befehle.VariantType
                        != Godot.Variant.Type.Array)
                    {
                        continue;
                    }

                    foreach (var c in befehle.AsGodotArray())
                    {
                        var code = c.AsGodotDictionary()
                            .GetValueOrDefault("code", -1)
                            .AsInt32();
                        if (code >= 11900 && code < 12000)
                        {
                            zaehler[code] = zaehler.TryGetValue(
                                code, out var v) ? v + 1 : 1;
                        }
                    }
                }
            }
        }

        Console.WriteLine("Karten mit Ereignissen: " + karten);

        Console.WriteLine("Menuebefehle: " + zaehler.Count);
        foreach (var paar in zaehler)
        {
            Console.WriteLine("  " + paar.Key + " : " + paar.Value);
        }

        // **Und  meine  erste  Zaehlung  sagte  null.**
        //
        // **Und  das  war  ein  geratener  Schluessel** -- **und  nicht
        //  eine  Tatsache  ueber  das  Spiel.**
        //
        // <code>
        /// Karten mit Ereignissen: 659
        /// Menuebefehle: 3
        ///   11910 : 3     OpenSaveMenu
        ///   11930 : 190   ChangeSaveAccess
        ///   11960 : 60    ChangeMainMenuAccess
        /// </code>
        //
        // **Und  `11950`, OpenMainMenu,  kommt  null  Mal  vor** --
        // **und  das  Menue  oeffnet  dieses  Spiel  nur  ueber  die
        //  Taste  oder  ueber  `11910`.**
        AssertEq(659, karten,
            "**and six hundred and fifty nine of the maps carry"
                + " events** -- and the rest are canvases");

        AssertEq(3, zaehler[11910],
            "**and the menu is opened three times** -- and 11910"
                + " is the only menu command that appears");

        AssertEq(190, zaehler[11930],
            "**and save access is changed a hundred and ninety"
                + " times**");

        AssertEq(60, zaehler[11960],
            "**and menu access is changed sixty times** -- and"
                + " that is the command that forbids the menu"
                + " key, which the input test already proves");

        AssertEq(0, zaehler.TryGetValue(11950, out var offen)
                ? offen : 0,
            "**and 11950, OpenMainMenu, never appears** -- and"
                + " this game opens its menu through the key or"
                + " through 11910, and a command it never"
                + " writes needs no host path");
    }
}
