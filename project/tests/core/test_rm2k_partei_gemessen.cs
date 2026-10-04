using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
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

        // **Und  jetzt  die  echte  Quelle.**
        //
        // **Und  EasyRPG Players  `Game_Party::SetupNewGame` liest**
        // -- **`data.party = lcf::Data::system.party`** -- **und  nicht
        //  eine  Karte  und  nicht  ein  Befehl.**
        //
        // **Und  liblcfs  `struct ChunkSystem`  hat  bei  0x16  ein**
        // -- **`Array - Short`  namens  `party`**, -- **und  0x15  ein
        //  `Integer`  namens  `party_size`.**
        var bank = new Rm2kParser().ParseDatabase(
            "res://tests/fixtures/rm2k-dragon-destiny/RPG_RT.ldb");
        if (!bank.IsSuccess()
            || !bank.GetData().ContainsKey("system"))
        {
            AssertTrue(false, "**and the bank's system chunk is"
                + " read**");
            return;
        }

        var system = bank.GetData()["system"].AsGodotDictionary();
        Console.WriteLine("System-Felder: " + system.Count);

        foreach (var k in system.Keys)
        {
            var v = system[k];
            Console.WriteLine("  " + k + " : " + v.VariantType
                + (v.VariantType == Godot.Variant.Type.Int
                    ? " = " + v.AsInt32()
                    : v.VariantType == Godot.Variant.Type.String
                        ? " = '" + v.AsString().Substring(0,
                            Math.Min(24, v.AsString().Length)) + "'"
                        : v.VariantType
                            == Godot.Variant.Type.Array
                                ? " n=" + v.AsGodotArray().Count
                                : ""));
        }
        Console.WriteLine("party_size: "
            + (system.ContainsKey("party_size")
                ? system["party_size"].AsInt32() : -1));
        if (system.ContainsKey("party"))
        {
            var party = system["party"];
            Console.WriteLine("party-Typ: " + party.VariantType);
            if (party.VariantType == Godot.Variant.Type.Array)
            {
                var helden = party.AsGodotArray();
                Console.WriteLine("Party: "
                    + string.Join(",", helden.Select(x =>
                        x.AsInt32())));
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
                + " so neither the map tree nor a party command"
                + " supplies this game's party");

        // **Und  die  echte  Quelle  ist  der  System-Chunk  der  Bank.**
        //
        // **Und  das  ist  keine  Vermutung**, -- **denn  die  Rohbytes
        //  des  498-Byte-`System`-Chunks  enthalten  bei  `0x16` genau
        //  zwei  Byte:  `[1, 0]`** -- **und  das  ist  Little-Endian
        //  `1`** -- **und  das  ist  Held  eins.**
        var partyEintraege = system["party"].AsGodotArray();
        AssertEq(1, partyEintraege.Count,
            "**and the system's party chunk holds exactly one"
                + " entry** -- and its raw bytes are [1, 0]");

        AssertEq(1, partyEintraege[0].AsInt32(),
            "**and that entry is actor one** -- so the party"
                + " comes from ChunkSystem 0x16 and not from a map,"
                + " which is what EasyRPG's SetupNewGame reads");

        // **Und  `party_size`  fehlt  in  diesem  Spiel.**
        //
        // **Und  mein  Decoder  hat  die  zwei  vorhandenen  Bytes
        //  verworfen**, -- **weil  er  die  Bytezahl  aus  dem
        //  fehlenden  Groessenfeld  genommen  hat** -- **und  daraus
        //  wurde  eine  leere  Party.**
        AssertTrue(!system.ContainsKey("party_size"),
            "**and this game writes no party_size field** -- and"
                + " the decoder had reported an empty party because"
                + " a missing size field must not discard the"
                + " bytes that are there");

        // **Und  die  Quelle  ist  messbar  und  nicht  geraten.**
        AssertTrue(!string.IsNullOrEmpty(
                new UniversalRPG.Rm2k.Parser.Rm2kParser()
                    .ParseDatabase("res://tests/fixtures/rm2k-dragon-"
                        + "destiny/RPG_RT.ldb").IsSuccess()
                    ? "gelesen" : ""),
            "**and the bank that carries that party is the game's"
                + " own file**");
    }
}
