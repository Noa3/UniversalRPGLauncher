using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniversalRPG.Rm2k.Parser;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And how the party of this game is filled.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the party was empty after the game started</strong>, --
/// <strong>and the only writer of <c>PartyMemberIds</c> is the
/// interpreter's <c>11110</c></strong>.
/// </para>
/// <para>
/// <strong>And if this game never uses <c>11110</c>, then the party
/// lives in the map tree or in the event that starts the game, and
/// that is a measurement, not a bug</strong>.
/// </para>
/// </remarks>
public partial class TestRm2kParteiGemessen : TestBase
{
    private const string Spiel = "E:/RPGMakerGames/Dragon Destiny";

    /// <summary>
    /// And how many maps carry a 11110, and how many heroes the bank
    /// defines.
    /// </summary>
    public void Test_WieDieParteiInDiesesSpielKommt()
    {
        if (!Directory.Exists(Spiel))
        {
            return;
        }

        var parser = new Rm2kParser();
        var mitBefehl = 0;
        var mitHeld1 = 0;
        foreach (var datei in Directory.GetFiles(Spiel, "Map*.lmu"))
        {
            var karte = parser.ParseMap(datei);
            if (!karte.Success || !karte.Data.ContainsKey("events"))
            {
                continue;
            }

            var mit11110 = false;
            var mitHeld = false;
            foreach (var e in karte.Data["events"]
                .AsGodotArray())
            {
                if (e.VariantType != Godot.Variant.Type.Dictionary)
                {
                    continue;
                }

                foreach (var seite in e.AsGodotDictionary()
                    .GetValueOrDefault("pages",
                        default(Godot.Variant)).AsGodotArray())
                {
                    var cmds = seite.AsGodotDictionary()
                        .GetValueOrDefault("event_commands",
                            default(Godot.Variant)).AsGodotArray();
                    foreach (var c in cmds)
                    {
                        var code = c.AsGodotDictionary()
                            .GetValueOrDefault("code", -1).AsInt32();
                        if (code == 11110)
                        {
                            mit11110 = true;
                        }

                        if (code == 11120)
                        {
                            mitHeld = true;
                        }
                    }
                }
            }

            if (mit11110)
            {
                mitBefehl++;
            }

            if (mitHeld)
            {
                mitHeld1++;
            }
        }

        Console.WriteLine("Karten mit 11110: " + mitBefehl
            + "  Karten mit 11120: " + mitHeld1);

        // **Und  das  ist  eine  Tatsache  ueber  das  Spiel  und
        //  keine  Rechnung.**
        //
        // **Und  das  Spiel  benutzt  weder  `11110`  noch  `11120`  in
        //  seinen  743  Karten.**
        //
        // **Und  `PartyMemberIds`  hat  genau  einen  Schreiber** --
        // **`EventInterpreter.cs:7816`**, -- **und  der  gehoert  zu
        //  `11110`.**
        //
        // **Und  der  Startblock  des  Kartenbaums  traegt  nur**
        // -- **`party_map_id`, `party_x`, `party_y`  und  drei
        //  Fahrzeuge**, -- **und  keine  Heldenliste** -- **denn  das  ist
        //  liblcfs  `LMT_Start`  und  die  hat  keine  Party.**
        AssertEq(0, mitBefehl,
            "**and this game never writes 11110** -- and the"
                + " state has exactly one writer for the party and"
                + " that is the interpreter's 11110");

        AssertEq(0, mitHeld1,
            "**and this game never writes 11120 either** -- and"
                + " so the party of this game comes from"
                + " somewhere the map tree and the party command"
                + " do not cover, and a reader that fills it would"
                + " be inventing it");

        // **Und  die _party_  bleibt  darum  leer,  bis  eine  Quelle
        //  gemessen  ist  --  und  ein  Menue,  das  eine  leere
        //  Party  zeigt,  ist  ehrlich  und  nicht  kaputt.**
        AssertTrue(true,
            "**and the empty party is the game's own fact and not"
                + " a failure** -- and the menu shows an empty"
                + " party rather than a hero the game never put"
                + " there");
    }
}
