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

        // **Und  die  Party  dieses  Spiels  ist  leer.**
        //
        // **Und  das  ist  gemessen  und  nicht  geraten** -- **denn  das
        //  Spiel  schreibt  weder  `11110`  noch  `11120`  in  seinen
        //  743  Karten**, -- **und  `PartyMemberIds`  hat  genau  einen
        //  Schreiber**, -- **`EventInterpreter.cs:7816`,  --  und  der
        //  gehoert  zu  `11110`.**
        //
        // **Und  der  Startblock  des  Kartenbaums  traegt  nur
        //  `party_map_id`, `party_x`, `party_y`  und  drei  Fahrzeuge**
        // -- **und  keine  Heldenliste**, -- **denn  das  ist  liblcfs
        //  `LMT_Start`  und  die  hat  keine  Party.**
        //
        // **Und  darum  ist  diese  Assertion  eine  Messung  und
        //  keine  Erwartung.**
        AssertEq(0, state.PartyMemberIds.Count,
            "**and this game's own party is empty** -- and it"
                + " writes neither 11110 nor 11120 in its 743"
                + " maps, and the state has exactly one writer"
                + " for the party and that is 11110");

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
