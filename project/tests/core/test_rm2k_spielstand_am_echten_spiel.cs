using System;
using System.IO;
using System.Linq;
using Godot;
using UniversalRPG.Plugins;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And that a slot file really holds a real game's progress.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the codec has <c>TryWriteFile</c> and
/// <c>TryReadFile</c> and nothing calls them</strong>, -- <strong>and
/// the runtime only exports a string into memory</strong>.
/// </para>
/// <para>
/// <strong>And a save the player cannot reach is a save that does
/// not exist</strong>.
/// </para>
/// </remarks>
public partial class TestRm2kSpielstandAmEchtenSpiel : TestBase
{
    private const string Spiel = "E:/RPGMakerGames/Dragon Destiny";

    /// <summary>
    /// And a slot round trip on the state a real game produced.
    /// </summary>
    public void Test_EinSlotSpeichertUndLaedtDasEchteSpiel()
    {
        if (!File.Exists(Spiel + "/RPG_RT.ldb"))
        {
            return;
        }

        using var host = new EnginePluginHost(
            BuiltInEnginePluginCatalog.CreateRuntimeRegistry());
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

        var lauf = (Rm2kEngineRuntime)host.Runtime!;
        var state = lauf.Simulation;

        // **Und  echte  Fortschrittsdaten**, -- **und  nicht  eine
        //  erfundene  Zahl**, -- **denn  der  Codec  soll  das  Spiel
        //  speichern  und  nicht  eine  Fixture.**
        state.Gold = 777;
        Console.WriteLine("Schalter " + state.Switches.Count
            + "  Variablen " + state.Variables.Count
            + "  CurrentHp " + state.CurrentHp.Count);

        // **Und  die  Listen  wachsen  erst  bei  Nutzung** -- **und  ein
        //  direkter  Schreibzugriff  auf  einen  Index,  den  das  Spiel
        //  nie  gesetzt  hat,  liegt  ausserhalb  der  Grenze.**
        //
        // **Und  das  ist  eine  Eigenschaft  des  Zustands  und  keine
        //  Eigenschaft  des  Spiels**, -- **und  RM2K  hat  Schalter  1
        // bis  50  und  Variablen  1  bis  50.**
        for (var i = 0; i <= 5; i++)
        {
            state.Switches.Add(false);
            state.Variables.Add(0);
        }

        state.CurrentHp[1] = 42;
        state.Switches[3] = true;
        state.Variables[5] = 12345;
        Console.WriteLine("Gold " + state.Gold + "  Party ["
            + string.Join(",", state.PartyMemberIds) + "]");

        // **Und  der  Weg  traegt  die  PID  des  Prozesses.**
        //
        // **Und  das  ist  kein  Kosmetik**, -- **denn
        //  `DirAccess.RemoveAbsolute`  loescht  einen  gefuellten  Ordner
        //  nicht  zuverlaessig**, -- **und  ein  Weg  aus  einem  frueheren
        //  Lauf  liegt  noch  da** -- **und  darum  hat  derselbe  Test
        //  im  Einzelaufruf  gruen  und  in  der  Suite  rot  gelaufen.**
        var ordner = ProjectSettings.GlobalizePath(
            "user://rm2k-slots-" + OS.GetProcessId());
        DirAccess.MakeDirRecursiveAbsolute(ordner);
        var geschrieben = Rm2kSimulationSaveCodec.TryWriteFile(
            ordner, "slot1", state, out var fehler);
        Console.WriteLine("geschrieben: " + geschrieben + " -> "
            + fehler);
        var datei = Path.Combine(ordner, "slot1.json");
        Console.WriteLine("Datei: " + (File.Exists(datei)
            ? new FileInfo(datei).Length + " Byte" : "fehlt"));
        Console.WriteLine("Dateien: " + string.Join(", ",
            Directory.GetFiles(ordner).Select(Path.GetFileName)));

        AssertTrue(geschrieben,
            "**and the slot is written** -- and the codec has had"
                + " TryWriteFile all along and nothing called it");

        // **Und  jetzt  das  Laden  in  einen  frischen  Zustand.**
        var frisch = new GameSimulationState();
        var gelesen = Rm2kSimulationSaveCodec.TryReadFile(
            ordner, "slot1", frisch, out var lesFehler);
        Console.WriteLine("gelesen: " + gelesen + " -> " + lesFehler);
        Console.WriteLine("zurueck: Gold " + frisch.Gold
            + "  Party [" + string.Join(",",
                frisch.PartyMemberIds) + "]"
            + "  Schalter " + (frisch.Switches.Count > 3
                && frisch.Switches[3] ? "an" : "aus")
            + "  Variable " + (frisch.Variables.Count > 5
                ? frisch.Variables[5].ToString() : "-"));

        AssertTrue(gelesen,
            "**and the slot is read back**");
        AssertEq(777, frisch.Gold,
            "**and the gold survives**");
        AssertEq(12345, frisch.Variables[5],
            "**and a variable survives**");

        // **Und  jetzt  der  Hostpfad** -- **und  gegen  eine
        //  Kopie  des  Spiels**, -- **denn  das  Original  gehoert  dem
        //  Nutzer  und  ein  Test  schreibt  nicht  hinein.**
        // **Und  `DirAccess.RemoveAbsolute`  loescht  einen  gefuellten
        //  Ordner  nicht  zuverlaessig.**
        //
        // **Und  darum  liegt  unter  dem  Weg  aus  dem  ersten  Teil
        //  noch  eine  Datei  und  der  Ordner  wurde  nicht  leer.**
        //
        // **Und  das  ist  kein  Fehler  des  Codes  und  einer  der
        //  Fixture** -- **und  der  Weg  ist  deshalb  neu  und  das
        //  Loeschen  ausdruecklich  rekursiv.**
        var kopie = ProjectSettings.GlobalizePath(
            "user://rm2k-slot-game-" + OS.GetProcessId());
        DirAccess.MakeDirRecursiveAbsolute(kopie);
        File.Copy(
            ProjectSettings.GlobalizePath(
                "res://tests/fixtures/rm2k-dragon-destiny/RPG_RT.ldb"),
            Path.Combine(kopie, "RPG_RT.ldb"));
        File.Copy(
            ProjectSettings.GlobalizePath(
                "res://tests/fixtures/rm2k-dragon-destiny/RPG_RT.lmt"),
            Path.Combine(kopie, "RPG_RT.lmt"));

        // **Und  ohne  eine  Karte  startet  der  Host  nicht** --
        // -- **und  er  hat  sie  beim  Start  auch  wirklich
        //  gebraucht**, -- **denn  er  liest  sie  vor  dem  ersten
        //  Bild**.
        //
        // **Und  meine  Kopie  hatte  nur  LDB  und  LMT** -- **und  darum
        //  war  das  Laden  nicht  der  Codec,  sondern  eine  fehlende
        //  Karte.**
        File.Copy(
            ProjectSettings.GlobalizePath(
                "res://tests/fixtures/rm2k-dragon-destiny/Map0001.lmu"),
            Path.Combine(kopie, "Map0001.lmu"));
        File.Copy(
            ProjectSettings.GlobalizePath(
                "res://tests/fixtures/rm2k-dragon-destiny/Map0100.lmu"),
            Path.Combine(kopie, "Map0100.lmu"));

        using var host2 = new EnginePluginHost(
            BuiltInEnginePluginCatalog.CreateRuntimeRegistry());
        var start2 = host2.Start(new PluginGameInfo
        {
            GameDirectory = kopie,
            EngineId = EnginePluginIds.RpgMaker2000,
            Generation = "rm2k",
            DetectorScore = 3,
        });
        if (!start2.Success)
        {
            AssertTrue(false, "**and the copied game starts**");
            return;
        }

        var lauf2 = (Rm2kEngineRuntime)host2.Runtime!;
        Console.WriteLine("Save-Verzeichnis: "
            + lauf2.SaveDirectory);
        lauf2.Simulation.Gold = 3141;
        var gespeichert = lauf2.TrySaveSlot("slotA",
            out var hostFehler);
        Console.WriteLine("Host gespeichert: " + gespeichert
            + " -> " + hostFehler);
        Console.WriteLine("Slots: " + string.Join(", ",
            lauf2.SaveSlots()));

        AssertTrue(gespeichert,
            "**and the runtime writes a slot beside the game** --"
                + " and the codec's TryWriteFile existed all along"
                + " and nothing called it");

        AssertTrue(File.Exists(Path.Combine(lauf2.SaveDirectory,
                "slotA.json")),
            "**and the file is really on disk**");

        AssertEq(1, lauf2.SaveSlots().Count,
            "**and the runtime lists the slot**");

        lauf2.Simulation.Gold = 0;
        var geladen = lauf2.TryLoadSlot("slotA", out var lastFehler);
        Console.WriteLine("Host geladen: " + geladen + " -> "
            + lastFehler + "  Gold " + lauf2.Simulation.Gold);

        AssertTrue(geladen,
            "**and the runtime reads it back**");
        AssertEq(3141, lauf2.Simulation.Gold,
            "**and the gold is the game's own again**");

        // **Und  jetzt  der  fehlende  Slot.**
        var fehlt = Rm2kSimulationSaveCodec.TryReadFile(ordner,
            "gibtsnicht", frisch, out var slotFehler);
        Console.WriteLine("fehlender Slot: " + fehlt + " -> "
            + slotFehler);
        AssertTrue(!fehlt,
            "**and a slot that does not exist is refused**");
        AssertTrue(slotFehler.Length > 0,
            "**and the refusal is in words**");
    }
}
