using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniversalRPG.Plugins;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And that a real game saves and reloads.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And <c>Rm2kSimulationSaveCodec</c> had two users and both
/// were invented states</strong>, -- <strong>no test ever saved a real
/// game and read it back</strong>.
/// </para>
/// <para>
/// <strong>And a save that was never reloaded is a claim, not a
/// save.</strong>
/// </para>
/// </remarks>
public partial class TestRm2kSpeichernAmEchtenSpiel : TestBase
{
    private const string Spiel = "E:/RPGMakerGames/Dragon Destiny";

    /// <summary>
    /// And the quoted key the codec writes.
    /// </summary>
    /// <param name="pName">The field's name.</param>
    /// <returns>The name in the form the JSON carries it.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And this exists because a quote inside a C# string
    /// needs escaping</strong>, -- <strong>and writing that escape by
    /// hand went wrong twice</strong>, -- <strong>so the test builds
    /// the key instead.</strong>
    /// </para>
    /// </remarks>
    private static string Schluessel(string pName)
    {
        return string.Concat("\"", pName, "\"");
    }

    private static Rm2kEngineRuntime? Starte(
        out EnginePluginHost? pHost)
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
    /// And what the game's own state holds before and after a save.
    /// </summary>
    public void Test_DerEchteZustandReist()
    {
        var lauf = Starte(out var host);
        if (lauf == null)
        {
            return;
        }

        using var h = host!;
        var state = lauf.Simulation;

        // **Und  der  Zustand  wird  mit  echten  Werten  gefuellt**,
        // -- **denn  ein  Test,  der  eine  erfundene  Zahl  speichert,
        //  beweist  nur  dass  der  Codec  Zahlen  schreibt.**
        state.Gold = 4321;
        // **Und  die  Position  kommt  aus  der  Karte  und  nicht  aus
        //  meinem  Kopf** -- **denn  der  Codec  hat  meinen  erfundenen
        //  Wert  zu  Recht  abgelehnt:**
        //
        // <code>
        // Restore: False -> Save player position is outside map bounds.
        /// </code>
        //
        // **Und  das  war  der  Codec,  der  richtig  handelte**, --
        // **und  mein  Test,  der  eine  Position  behauptet  hat,  die
        //  es  auf  Karte  742  nicht  gibt.**
        Console.WriteLine("Karte " + state.MapId + "  "
            + state.MapWidth + "x" + state.MapHeight);
        state.MapX = Math.Max(0, state.MapWidth - 1);
        state.MapY = Math.Max(0, state.MapHeight - 1);
        // **Und  Schalter  und  Variablen  waechsen  erst  beim
        //  Gebrauch** -- **denn  RM2K  hat  50000  von  beiden  und  ein
        //  Zustand,  der  alle  vorab anlegt,  waere  100  Millionen
        //  Eintraege.**
        //
        // **Und  der  Interpreter  macht  genau  das**,
        // -- **und  mein  Test  hat  direkt  indiziert  und  ist  an  der
        //  Grenze  abgebrochen.**
        while (state.Switches.Count < 7)
        {
            state.Switches.Add(false);
        }

        while (state.Variables.Count < 3)
        {
            state.Variables.Add(0);
        }

        state.Switches[6] = true;
        state.Variables[2] = 99;
        state.ItemCounts[12] = 5;
        // **Und  Held  eins  ist  schon  da**, -- **denn  der  Host  liest
        //  die  Startparty  aus  dem  System-Chunk  der  Bank**, --
        // **und  die  Rohbytes  tragen  `[1, 0]`**, -- **und  das  ist
        //  Little-Endian  `1`**.
        //
        // **Und  darum  steht  hier  nur  noch  Held  vier.**
        Console.WriteLine("Party nach dem Start: ["
            + string.Join(",", state.PartyMemberIds) + "]");
        state.PartyMemberIds.Add(4);
        // **Und  die  gelernten  Faehigkeiten** -- **denn  das  ist  das
        //  Feld,  das  eine  Party  nach  dem  Laden  wirklich  braucht**,
        // -- **und  `ActorValues`  traegt  die  Basiswerte  und  nicht  die
        //  Faehigkeiten.**
        state.SkillsOf(1).Add(5);
        state.SkillsOf(1).Add(19);
        state.SetMonsterHp(0, 0);

        var erwarteteX = state.MapX;
        var erwarteteY = state.MapY;
        var json = Rm2kSimulationSaveCodec.Serialize(state);
        Console.WriteLine("JSON-Laenge: " + json.Length);
        Console.WriteLine("Karte " + state.MapId + "  Gold "
            + state.Gold + "  Schalter 6 "
            + state.Switches[6] + "  Var 2 "
            + state.Variables[2] + "  Item 12 "
            + state.ItemCounts[12]);

        AssertTrue(json.Length > 100,
            "**and the save is substantial**");

        // **Und  die  Behauptung  prueft  den  Text  und  nicht
        //  einen  Zahlenvergleich**, -- **denn  der  Zahlenvergleich
        //  waere  derselbe  Weg  wie  beim  Wiederherstellen.**
        AssertTrue(json.Contains(Schluessel("Gold")),
            "**and the save names the gold**");

        AssertTrue(json.Contains(Schluessel("MapX"))
                && json.Contains(Schluessel("MapY")),
            "**and it names both coordinates**");

        Console.WriteLine("Position " + state.MapX + "/"
            + state.MapY);

        // **Und  jetzt  das  Wichtigste:  ein  zweiter  Zustand  laedt
        //  es.**
        var geladen = new GameSimulationState();
        var ok = Rm2kSimulationSaveCodec.TryRestore(
            json, geladen, out var fehler);
        Console.WriteLine("Restore: " + ok + "  -> " + fehler);

        AssertTrue(ok,
            "**and a fresh state reads it back** -- and: "
                + fehler);

        AssertEq(4321, geladen.Gold,
            "**and the gold survives the round trip**");

        AssertEq(erwarteteX, geladen.MapX,
            "**and the column survives**");

        AssertEq(erwarteteY, geladen.MapY,
            "**and the row survives**");

        AssertTrue(geladen.Switches.Count > 6
                && geladen.Switches[6],
            "**and a set switch survives**");

        AssertEq(99, geladen.Variables[2],
            "**and a variable survives**");

        AssertEq(5, geladen.ItemCounts[12],
            "**and an item count survives**");

        AssertEq(2, geladen.PartyMemberIds.Count,
            "**and the party survives** -- and it starts with"
                + " one hero from ChunkSystem 0x16 and gains a"
                + " second through the interpreter, and both"
                + " come back");

        AssertEq(1, geladen.PartyMemberIds[0],
            "**and it survives in order** -- and a party that came"
                + " back sorted would change which hero the"
                + " player commands");

        // **Und  jetzt  der  entscheidende  Teil  des  JSON**,
        // -- **denn  "enthaelt 19"  allein  beweist  nichts.**
        var stelle = json.IndexOf(Schluessel("ActorSkills"),
            StringComparison.Ordinal);
        if (stelle >= 0)
        {
            Console.WriteLine("JSON-Auszug: "
                + json.Substring(stelle, Math.Min(180,
                    json.Length - stelle)));
        }
        else
        {
            Console.WriteLine("JSON nennt das Feld nicht");
        }

        AssertTrue(geladen.SkillsOf(1).Contains(5)
                && geladen.SkillsOf(1).Contains(19),
            "**and the learned skills survive** -- and a save"
                + " that dropped them would reload a hero with"
                + " none of the skills the game gave him, and the"
                + " battle command list would come back empty");
    }

    /// <summary>
    /// And the save goes to disk and comes back.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And a save that only exists in memory is a
    /// variable.</strong>
    /// </para>
    /// </remarks>
    public void Test_DerSpeicherstandLiegtAufDerPlatte()
    {
        var lauf = Starte(out var host);
        if (lauf == null)
        {
            return;
        }

        using var h = host!;
        var ordner = Path.Combine(
            Path.GetTempPath(), "urpg-save-probe");
        Directory.CreateDirectory(ordner);

        try
        {
            var state = lauf.Simulation;
            state.Gold = 777;
            state.MapX = 3;
            state.MapY = 4;

            var ok = Rm2kSimulationSaveCodec.TryWriteFile(
                ordner, "slot1", state, out var fehler);
            Console.WriteLine("schreiben: " + ok + " -> "
                + fehler);
            AssertTrue(ok, "**and the save is written** -- and: "
                + fehler);

            var pfad = Path.Combine(ordner, "slot1.json");
            Console.WriteLine("Datei: " + pfad + "  "
                + (File.Exists(pfad)
                    ? new FileInfo(pfad).Length + "b" : "fehlt"));
            AssertTrue(File.Exists(pfad),
                "**and the file is there**");

            var neu = new GameSimulationState();
            var gelesen = Rm2kSimulationSaveCodec.TryReadFile(
                ordner, "slot1", neu, out var ladeFehler);
            Console.WriteLine("lesen: " + gelesen + " -> "
                + ladeFehler);

            AssertTrue(gelesen,
                "**and it is read back from the file**");

            AssertEq(777, neu.Gold,
                "**and the gold is the one that was written**");
        }
        finally
        {
            if (Directory.Exists(ordner))
            {
                Directory.Delete(ordner, true);
            }
        }
    }
}
