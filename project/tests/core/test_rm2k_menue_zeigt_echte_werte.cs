using System;
using System.IO;
using System.Linq;
using Godot;
using UniversalRPG.Plugins;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And that the menu shows the game's own numbers.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the menu key opened a menu and nothing displayed
/// it</strong>, -- <strong>because the numbers only existed inside the
/// state</strong>, -- <strong>and the formatting lived nowhere a
/// headless run can reach</strong>.
/// </para>
/// <para>
/// <strong>And this starts the real game</strong>, -- <strong>because the
/// step that fills the hero's values is the host's, and a test that
/// built the state itself would not have exercised it</strong>.
/// </para>
/// </remarks>
public partial class TestRm2kMenueZeigtEchteWerte : TestBase
{
    private const string Spiel = "E:/RPGMakerGames/Dragon Destiny";

    /// <summary>
    /// And the rows carry the bank's name and the bank's hit points.
    /// </summary>
    public void Test_DieMenueZeilenTragenDieBankWerte()
    {
        if (!File.Exists(Spiel + "/RPG_RT.ldb"))
        {
            return;
        }

        using var host = new EnginePluginHost(
            BuiltInEnginePluginCatalog
                .CreateRuntimeRegistry());
        var start = host.Start(new PluginGameInfo
        {
            GameDirectory = Spiel,
            EngineId = EnginePluginIds.RpgMaker2000,
            Generation = "rm2k",
            DetectorScore = 3,
        });
        if (!start.Success)
        {
            AssertTrue(false, "**and the game starts**");
            return;
        }

        var lauf = (Rm2kEngineRuntime)
            host.Runtime!;
        var state = lauf.Simulation;

        Console.WriteLine("Helden in der Party: ["
            + string.Join(",", state.PartyMemberIds) + "]  Anzahl "
            + state.PartyMemberIds.Count);
        Console.WriteLine("vor dem Laden der Bank: "
            + (state.FindActorValues(1)?.Name ?? "<leer>")
            + "  MaxHp "
            + (state.FindActorValues(1)?.BaseMaxHp ?? -1));

        // **Und  jetzt  die  Party  --  und  meine  Behauptung  war
        //  falsch.**
        //
        // **Und  ich  habe  gemessen,  das  Spiel  schreibe  weder
        //  `11110`  noch  `11120`  in  seinen  743  Karten**, --
        // **und  daraus  habe  ich  geschlossen,  die  Party  sei
        //  leer.**
        //
        // **Und  EasyRPG Players  `Game_Party::SetupNewGame` liest**
        // -- **`data.party = lcf::Data::system.party`** -- **und  damit
        //  aus  dem  System-Chunk  der  Bank  und  nicht  aus  einer
        //  Karte.**
        //
        // **Und  Dragon  Destinys  Rohbytes  sagen  `0x16  party  len 2
        //  [1, 0]`** -- **und  das  ist  Little-Endian  `1`** -- **und
        //  das  ist  Held  eins.**
        //
        // **Und  mein  eigener  Decoder  hatte  daraus  null  gemacht**,
        // -- **weil  `party_size`  im  Spiel  fehlt  und  mein  Code  die
        //  vorhandenen  Bytes  verworfen  hat**, -- **sobald  das
        //  Groessenfeld  fehlte.**
        AssertEq(1, state.PartyMemberIds.Count,
            "**and the game's own party has one hero** -- and it"
                + " comes from ChunkSystem field 0x16, and the raw"
                + " bytes are [1, 0] which is little endian 1, and"
                + " the decoder had reported an empty party"
                + " because party_size is absent and a missing"
                + " size field must not discard present bytes");

        AssertEq(1, state.PartyMemberIds[0],
            "**and that hero is actor one** -- which the bank"
                + " calls '" + (state.FindActorValues(1)?.Name ?? "?")
                + "'");

        var zeilen = Rm2kMenueZeile.Helden(state);
        var rechts = Rm2kMenueZeile.Zustand(state);
        foreach (var z in zeilen.Concat(rechts))
        {
            Console.WriteLine("  " + z);
        }

        // **Und  die  Party  dieses  Spiels  ist  leer** -- **und  das
        //  ist  gemessen** -- **und  deshalb  kann  das  Menue  keine
        //  Heldenzeile  zeigen,  und  das  ist  ehrlich.**
        //
        // **Und  was  jetzt  da  ist,  sind  die  Bankwerte  im
        //  Zustand** -- **und  die  sind  die  Hälfte  des  Beweises.**
        var werte = state.FindActorValues(1);
        Console.WriteLine("Held 1 aus der Bank: " + werte?.Name
            + "  MaxHp " + werte?.BaseMaxHp
            + "  MaxSp " + werte?.BaseMaxSp
            + "  Angriff " + werte?.BaseAttack);

        AssertTrue(state.ActorValues.Count > 0,
            "**and the host filled the state with the bank's"
                + " heroes** -- and before this step it read the"
                + " bank only for sprite names and"
                + " GetOrCreateActorValues left every value blank");

        AssertTrue(werte != null && !string.IsNullOrEmpty(werte.Name),
            "**and the hero carries the bank's name** -- and the"
                + " bank says '" + werte?.Name + "'");

        AssertTrue(werte!.BaseMaxHp > 1,
            "**and the hero's maximum hit points come from the"
                + " bank** -- and they were zero before this step,"
                + " and now they are the bank's own number");

        // **Und  jetzt  der  Weg  von  der  Zeile  zu  diesen  Werten.**
        var mitHeld = new Godot.Collections.Array<int> { 1 };
        var zeilenMitHeld = Rm2kMenueZeile.Helden(new GameSimulationState
        {
            PartyMemberIds = mitHeld,
            ActorValues = state.ActorValues,
            DatabaseData = state.DatabaseData
        });

        Console.WriteLine("  " + string.Join(" | ", zeilenMitHeld));

        AssertEq(1, zeilenMitHeld.Count,
            "**and the menu writes one row for one party member**"
                + " -- and the running game's own party is empty,"
                + " because this game writes neither 11110 nor"
                + " 11120 in its 743 maps, and a menu that put a"
                + " hero there anyway would be inventing one");

        AssertTrue(zeilenMitHeld[0].Contains(werte.Name)
                && zeilenMitHeld[0].Contains(
                    werte.BaseMaxHp.ToString()),
            "**and that row carries the bank's name and the bank's"
                + " maximum hit points** -- and a menu with"
                + " invented numbers teaches the player nothing"
                + " about the game");
    }
}
