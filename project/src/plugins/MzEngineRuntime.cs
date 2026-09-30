using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniversalRPG.Core;
using UniversalRPG.Rm2k.Rendering;
using UniversalRPG.Web;

namespace UniversalRPG.Plugins;

/// <summary>
/// Runs an MZ project's maps through the reader in
/// <c>project/src/mz</c>, one frame at a time.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is the class whose absence was the whole gap.</strong>
/// Measured before it was written: <c>MzEventRunner</c>,
/// <c>MzInterpreter</c>, <c>MzCommandEntry</c> and <c>MzBranchFacts</c>
/// had <em>no caller outside <c>project/src/mz/</c> at all</em> — the
/// dispatch read real commands out of a real finished project and
/// executed them in the right order, <strong>and no image, no sound and
/// no map of an MZ game was ever drawn or played by it.</strong>
/// </para>
/// <para>
/// <strong>And the same gap as the other engines, and the same fix.</strong>
/// <c>RgssEngineRuntime.Update</c> does one thing —
/// <c>_clock.ProcessFrame(pDeltaSeconds)</c> — <strong>and that is
/// exactly as far as XP, VX and VX Ace get.</strong> <strong>What a
/// project has and does not run is a separate question from whether its
/// reader is correct</strong>, <strong>and the reader here is measured
/// command by command against a finished project.</strong>
/// </para>
/// <para>
/// <strong>And no project's JavaScript is executed.</strong> The
/// boundary from <c>AGENTS.md</c> and
/// <c>BuiltInEnginePlugins.cs</c> holds: a game's own scripts are data.
/// <strong>A plugin command is reported and not run</strong>, which is
/// the same answer <c>357 Plugin Command</c> already gives.
/// </para>
/// </remarks>
public sealed class MzEngineRuntime : IEngineRuntime
{
    private readonly VirtualClock _clock = new();
    private readonly string _generation;
    private readonly PluginGameInfo _game;

    /// <summary>Builds a runtime for one detected project.</summary>
    /// <param name="pPluginId">Which plugin asked for it.</param>
    /// <param name="pGeneration">"MV" or "MZ", for the messages.</param>
    /// <param name="pGame">The detected project.</param>
    public MzEngineRuntime(
        string pPluginId, string pGeneration, PluginGameInfo pGame)
    {
        PluginId = pPluginId;
        _generation = pGeneration;
        _game = pGame;
    }

    /// <summary>Which plugin this runtime belongs to.</summary>
    public string PluginId { get; }

    /// <summary>Where the run is in its lifecycle.</summary>
    public PluginRuntimeState State { get; private set; } =
        PluginRuntimeState.Created;

    /// <summary>How many frames the run has spent.</summary>
    public int SimulationTicks => _clock.GetSimulationTicks();

    /// <summary>The state the project's own commands wrote into.</summary>
    public MzBranchFacts Facts { get; private set; } = new();

    /// <summary>What the run did, in the order it did it.</summary>
    public List<MzAction> Actions { get; } = new();

    /// <summary>Every map this runtime has read, by the project's own ids.</summary>
    public Dictionary<int, MzDataFile> Maps { get; } = new();

    /// <summary>The map the run is on, or zero before it starts.</summary>
    public int CurrentMapId { get; private set; }

    /// <summary>Whether a choice is waiting for the player.</summary>
    public bool ChoicePending => Facts.ChoicePending;

    /// <summary>What the player is being asked, if anything.</summary>
    public IReadOnlyList<string> ChoiceOptions => Facts.ChoiceOptions;

    /// <summary>
    /// What stopped the run, or an empty string while it runs.
    /// </summary>
    /// <remarks>
    /// <strong>And this says out loud why, and not only that.</strong> A
    /// run that stopped on a refused command is a project this reader
    /// cannot finish, <strong>and a log that only said "stopped" would
    /// leave a reader guessing whether the file or this program was
    /// wrong.</strong>
    /// </remarks>
    public string StopReason { get; private set; } = "";

    /// <summary>
    /// The map the run painted last, and nothing before it has.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is what was missing, and it is a whole
    /// thing.</strong> The runtime read a project's maps and ran their
    /// commands, <strong>and a player could not see any of it</strong> —
    /// <strong>and the reason was not a missing picture and not a
    /// missing pixel buffer, it was that nothing ever asked for
    /// one.</strong>
    /// </para>
    /// <para>
    /// <strong>And the three obstacles were all measured:</strong> the
    /// images are encrypted, the tileset is named by its own name and
    /// not by a letter, and a tile is six numbers and not one.
    /// </para>
    /// </remarks>
    public Rm2kPixelBuffer? PaintedMap { get; private set; }

    /// <summary>How many colours the painted map has.</summary>
    /// <remarks>
    /// <strong>And this is the number a caller can read without a
    /// picture.</strong> One colour is a map whose tileset was not
    /// found, <strong>and on the RM2K project <c>Map0001</c> was one
    /// colour and passed everything until a test asked.</strong>
    /// </remarks>
    public int PaintedColours =>
        PaintedMap == null ? 0 : MzMapRenderer.DistinctColours(PaintedMap);

    /// <summary>Why the map was not painted, or an empty string.</summary>
    public string PaintReason { get; private set; } = "";

    /// <summary>The last refusal, if the run stopped on one.</summary>
    public MzStep Stopped { get; private set; } = MzStep.Finished;

    /// <summary>
    /// Answers the open choice, from a key press.
    /// </summary>
    /// <param name="pBranch">Which answer, counting from one.</param>
    /// <returns>Whether the answer was one of the options.</returns>
    public bool AnswerChoice(int pBranch)
    {
        if (!Facts.AnswerChoice(pBranch))
        {
            return false;
        }

        _branch = pBranch;
        return true;
    }

    private int _branch;

    /// <inheritdoc />
    public PluginOperationResult Initialize(
        EnginePluginRuntimeContext pContext)
    {
        if (State != PluginRuntimeState.Created)
        {
            return Fail(
                PluginErrorCode.InvalidLifecycleTransition,
                $"The {_generation} runtime was already initialized.");
        }

        if (pContext?.Game == null)
        {
            return Fail(
                PluginErrorCode.InvalidGame,
                $"The {_generation} runtime context is missing the game.");
        }

        var inspection = SafeGameInspector.Inspect(_game.GameDirectory);
        if (!inspection.Success || inspection.Value == null)
        {
            return Fail(
                PluginErrorCode.InvalidGame,
                inspection.Error?.Message
                    ?? "The detected project could not be inspected.");
        }

        var snapshot = inspection.Value;
        if (snapshot.IsMalformed)
        {
            return Fail(
                PluginErrorCode.InvalidGame,
                "The detected project contains malformed or over-budget input.");
        }

        var gelesen = 0;
        var verweigert = new List<string>();
        foreach (var datei in snapshot.Files
            .Where(pFile => IsMap(pFile.RelativePath))
            .OrderBy(pFile => pFile.RelativePath, StringComparer.Ordinal))
        {
            MzDataFile karte;
            try
            {
                // **Und die Karte wird vom Datentraeger gelesen, und
                // nicht aus dem Snapshot.** **Der Inspektor liest
                // Verzeichnisdateien nur als Vorspann von 4096 Bytes**
                // -- **und 18 von 20 Karten des gemessenen Projekts sind
                // groesser**, **und jede davon kam als abgeschnittenes
                // JSON an, und jede wurde uebersprungen.**
                //
                // **Und die Grenze ist 1 MiB fuer eine volle Datei, und
                // die groesste Karte des Projekts hat knapp 50 KiB.**
                var voll = File.ReadAllBytes(
                    Path.Combine(
                        _game.GameDirectory,
                        datei.RelativePath.Replace('/', Path.DirectorySeparatorChar)));
                karte = MzDataFile.Read(datei.RelativePath, voll);
            }
            catch (MzDataException ausnahme)
            {
                // **Und eine kaputte Karte wird uebersprungen und
                // gesagt, und nicht das ganze Spiel verweigert** --
                // **denn ein Projekt mit einer kaputten Karte ist kein
                // Projekt, aus dem nichts wird.**
                verweigert.Add(ausnahme.Message);
                continue;
            }

            var id = MapIdOf(karte);
            if (id >= 0)
            {
                Maps[id] = karte;
                gelesen++;
            }
        }

        if (gelesen == 0)
        {
            return Fail(
                PluginErrorCode.InvalidGame,
                "The project has no map this reader could read, and "
                    + (verweigert.Count == 0
                        ? "and it reported no reason."
                        : "and it reported: " + verweigert[0]));
        }

        MapCount = gelesen;
        SkippedMaps = verweigert;
        _tilesets = ReadTilesets();
        State = PluginRuntimeState.Initialized;
        return PluginOperationResult.Succeeded();
    }

    /// <summary>
    /// Every tileset of the project, by the id the maps name.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the name is the tileset's own.</strong> Measured on
    /// <c>data/Tilesets.json</c>: the tileset of map 2 is called
    /// <em>Overworld</em> and the file is
    /// <c>img/tilesets/Overworld.png_</c> — <strong>and the editor's
    /// single letter, <em>A</em>, names no file in this project.</strong>
    /// A reader that guessed the letter found nothing for every map but
    /// one.
    /// </para>
    /// <para>
    /// <strong>And the images are encrypted.</strong> The project's
    /// <c>System.json</c> says <c>hasEncryptedImages: true</c> and
    /// carries the key; <strong>and every one of its 81 images is named
    /// <c>.png_</c> rather than <c>.png</c>.</strong>
    /// </para>
    /// </remarks>
    private Dictionary<int, List<Rm2kIndexedImage?>> ReadTilesets()
    {
        var ergebnis = new Dictionary<int, List<Rm2kIndexedImage?>>();
        var pfad = Path.Combine(_game.GameDirectory, "data", "Tilesets.json");
        if (!File.Exists(pfad))
        {
            TilesetProblem = "The project has no data/Tilesets.json, and "
                + "without it there is no name for any tileset.";
            return ergebnis;
        }

        MzDataFile system;
        MzDataFile tabelle;
        try
        {
            system = MzDataFile.Read(
                "data/System.json", File.ReadAllBytes(
                    Path.Combine(_game.GameDirectory, "data", "System.json")));
            tabelle = MzDataFile.Read("data/Tilesets.json", File.ReadAllBytes(pfad));
        }
        catch (MzDataException ausnahme)
        {
            // **Und der Grund wird gesagt, und nicht geschluckt** --
            // **denn dieser `catch` stand hier zuerst stumm**, **und
            // die Karte malte nichts**, **und der einzige Grund war ein
            // leeres Wörterbuch**, **das aussah wie ein Projekt ohne
            // Tilesets.**
            TilesetProblem = ausnahme.Message;
            return ergebnis;
        }

        var schluessel = system.Root.Member("encryptionKey")?.StringOr("") ?? "";
        foreach (var eintrag in tabelle.Root.Items)
        {
            // **Und `IntOr` liest den Wert des Elements selbst, und
            // nicht dessen Feld `id`.** **Ein Element, das ein Objekt
            // ist, hat keine Zahl**, **und `IntOr` gab deshalb -1 fuer
            // jede der sechs Zeilen zurueck**, **und jedes Tileset
            // wurde uebersprungen**, **und die Karte malte nichts, und
            // der `catch` sagte nichts, weil nichts geworfen wurde.**
            var id = eintrag.Member("id")?.IntOr(-1) ?? -1;
            // **Und die Blaetter kommen aus `tilesetNames`, und nicht
            // aus dem Namen des Tilesets.** **Gemessen:** jedes der
            // sechs Tilesets traegt neun Namen, **und mehrere davon
            // sind leer** -- **[World_A1, World_A2, (leer), (leer),
            // (leer), World_B, World_C, (leer), (leer)]** -- **und keine
            // der Dateien heisst `Overworld.png_`.**
            var blaetter = new List<Rm2kIndexedImage?>();
            var namen = eintrag.Member("tilesetNames")?.Items;
            if (id < 0 || namen == null)
            {
                continue;
            }

            for (var index = 0; index < MzMapRenderer.SheetsPerTileset; index++)
            {
                var name = index < namen.Count
                    ? namen[index].StringOr("")
                    : "";
                Rm2kIndexedImage? decodiert = null;
                if (name.Length > 0)
                {
                    var datei = Path.Combine(
                        _game.GameDirectory, "img", "tilesets",
                        MzMapRenderer.TilesetFileName(name));
                    if (File.Exists(datei))
                    {
                        var bild = MzImageReader.Read(
                            File.ReadAllBytes(datei), schluessel, out var _);
                        if (bild != null)
                        {
                            Rm2kIndexedImage.TryParse(
                                bild, out decodiert, out var _);
                        }
                    }
                }

                blaetter.Add(decodiert);
            }

            ergebnis[id] = blaetter;
        }

        return ergebnis;
    }

    /// <summary>How many maps this runtime read.</summary>
    public int MapCount { get; private set; }

    /// <summary>The maps it could not read, and why.</summary>
    public IReadOnlyList<string> SkippedMaps { get; private set; } =
        Array.Empty<string>();

    /// <inheritdoc />
    public PluginOperationResult Start()
    {
        if (State != PluginRuntimeState.Initialized)
        {
            return Fail(
                PluginErrorCode.InvalidLifecycleTransition,
                $"The {_generation} runtime was not initialized.");
        }

        // **Und die Startkarte kommt aus `MapInfos.json`**, **denn ein
        // Leser, der die erste gefundene Karte nahm, zeigte einem Spiel
        // eine Karte, die es nicht als Startkarte geschrieben hat** --
        // **das ist derselbe Fehler, den RM2K bei
        // `Directory.EnumerateFiles(...).FirstOrDefault()` gemacht hat.**
        var start = StartMapFromSystem();
        if (start < 0 || !Maps.ContainsKey(start))
        {
            return Fail(
                PluginErrorCode.InvalidGame,
                start < 0
                    ? "The project's MapInfos names no start map, and this "
                        + "reader will not guess one."
                    : $"The project starts on map {start}, and that map is "
                        + "not among the maps it could read.");
        }

        CurrentMapId = start;
        Repaint();
        _runner = new MzEventRunner();
        _facts = Facts;
        _clock.Reset();
        State = PluginRuntimeState.Running;
        return PluginOperationResult.Succeeded();
    }

    /// <summary>
    /// Paints the current map, and says why it did not when it did not.
    /// </summary>
    /// <remarks>
    /// <strong>And the map is painted once at the start, and not
    /// every frame.</strong> A map's tiles do not change while its
    /// commands run; <strong>and a painter that repainted every frame
    /// spent the whole frame on work whose answer was the same as
    /// the frame before.</strong> <strong>What changes per frame is the
    /// player's position over that map, and that is not a tile.</strong>
    /// </remarks>
    public bool Repaint()
    {
        PaintedMap = null;
        PaintReason = "";
        if (!Maps.TryGetValue(CurrentMapId, out var karte))
        {
            PaintReason = $"Map {CurrentMapId} is not among the maps "
                + "this runtime read.";
            return false;
        }

        var tilesetId = karte.Root.Member("tilesetId")?.IntOr(-1) ?? -1;
        if (tilesetId < 0 || !_tilesets.TryGetValue(tilesetId, out var blaetter))
        {
            PaintReason = $"Map {CurrentMapId} names tileset {tilesetId}, "
                + "and this runtime read no sheets for it. Measured on a "
                + "finished project, a tileset is nine sheets named in "
                + "tilesetNames, several of them empty, and no file is "
                + "named after the tileset itself.";
            return false;
        }

        var breite = karte.Root.Member("width")?.IntOr(0) ?? 0;
        var hoehe = karte.Root.Member("height")?.IntOr(0) ?? 0;
        if (breite <= 0 || hoehe <= 0)
        {
            PaintReason = $"Map {CurrentMapId} says {breite}x{hoehe}, "
                + "and there is no picture of nothing.";
            return false;
        }

        var pixel = new Rm2kPixelBuffer(
            breite * MzMapRenderer.TilePixels,
            hoehe * MzMapRenderer.TilePixels);
        if (!new MzMapRenderer().Paint(karte.Root, blaetter, pixel, out var warum))
        {
            PaintReason = warum;
            return false;
        }

        PaintedMap = pixel;
        return true;
    }

    private MzEventRunner? _runner;

    private MzBranchFacts? _facts;


    /// <inheritdoc />
    public PluginOperationResult Update(double pDeltaSeconds)
    {
        if (State != PluginRuntimeState.Running)
        {
            return Fail(
                PluginErrorCode.InvalidLifecycleTransition,
                $"The {_generation} runtime is not running.");
        }

        if (double.IsNaN(pDeltaSeconds)
            || double.IsInfinity(pDeltaSeconds) || pDeltaSeconds < 0)
        {
            return Fail(
                PluginErrorCode.InvalidLifecycleTransition,
                $"A frame of {pDeltaSeconds} seconds is not a length of "
                + "time, and the runtime refuses it rather than counting "
                + "it.");
        }

        _clock.ProcessFrame(pDeltaSeconds);

        // **Und eine offene Wahl wartet auf den Spieler, und nicht auf
        // das naechste Bild** -- **denn "waehle eine Antwort" ist keine
        // Zeit, die vergeht.**
        if (Facts.ChoicePending)
        {
            return PluginOperationResult.Succeeded();
        }

        var lauf = _runner;
        var fakten = _facts;
        if (lauf == null || fakten == null)
        {
            return Fail(
                PluginErrorCode.InvalidLifecycleTransition,
                "The runtime is running without a runner, and this is a "
                    + "state the engine never reaches.");
        }

        // **Und der Runner laeuft die Seite der Startkarte, und nicht
        // die ganze Karte**, -- **denn jede Seite ist ein eigener
        // Ablauf, und ein Lauf, der alle drei einer Seite nacheinander
        // startet, hat am Ende nur die letzte davon ausgefuehrt.**
        foreach (var seite in PageOf(CurrentMapId))
        {
            var ergebnis = lauf.Run(
                seite, fakten, CurrentMapId, 0, new MzRandom());

            // **Und die Aktionen des Laufs sind seine eigenen, und
            // nicht meine Liste.** **`Run` baeuft eine `actions` fuer
            // jeden Lauf**, **und ein erster Entwurf haette die
            // zurueckgegebene Liste gelesen** -- **und so blieben nach
            // hundert Bildern null Aktionen, obwohl das Projekt
            // fuenfzig Befehle auf der Startkarte hat.**
            Actions.AddRange(ergebnis.Actions);

            if (ergebnis.Stopped == MzStep.Waiting)
            {
                // **Und ein Warten ist kein Ende**, **und der Lauf
                // wird dem naechsten Bild wiedergegeben**, **und der
                // Befehl, auf den er wartet, bleibt stehen.**
                State = PluginRuntimeState.Running;
                return PluginOperationResult.Succeeded();
            }

            if (ergebnis.Stopped == MzStep.Frozen
                || ergebnis.Stopped == MzStep.Refused
                || ergebnis.Stopped == MzStep.Truncated)
            {
                Stopped = ergebnis.Stopped;
                StopReason = ergebnis.Reason;
                State = PluginRuntimeState.Stopped;
                return PluginOperationResult.Succeeded();
            }
        }

        return PluginOperationResult.Succeeded();
    }

    /// <inheritdoc />
    public PluginOperationResult Stop()
    {
        if (State != PluginRuntimeState.Running
            && State != PluginRuntimeState.Stopped)
        {
            return Fail(
                PluginErrorCode.InvalidLifecycleTransition,
                $"The {_generation} runtime cannot stop from "
                + $"{State}.");
        }

        State = PluginRuntimeState.Stopped;
        return PluginOperationResult.Succeeded();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        State = PluginRuntimeState.Disposed;
    }

    /// <summary>How many sheets this runtime read over all tilesets.</summary>
    public int TilesetSheetCount
    {
        get
        {
            var anzahl = 0;
            foreach (var blaetter in _tilesets.Values)
            {
                foreach (var blatt in blaetter)
                {
                    if (blatt != null)
                    {
                        anzahl++;
                    }
                }
            }

            return anzahl;
        }
    }

    /// <summary>
    /// Why the tilesets could not be read, and an empty string when
    /// they were.
    /// </summary>
    /// <remarks>
    /// <strong>And this exists because a silent catch is a hole.</strong>
    /// The first version of this reader returned an empty dictionary on
    /// failure, <strong>and an empty dictionary and a project with no
    /// tilesets look exactly the same</strong> — <strong>and the map
    /// painted nothing and the log said nothing.</strong>
    /// </remarks>
    public string TilesetProblem { get; private set; } = "";

    private Dictionary<int, List<Rm2kIndexedImage?>> _tilesets = new();

    private static bool IsMap(string pRelativePath)
    {
        var name = Path.GetFileName(pRelativePath);
        return name.StartsWith("Map", StringComparison.OrdinalIgnoreCase)
            && name.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
            && !name.Equals("MapInfos.json", StringComparison.OrdinalIgnoreCase);
    }

    private static int MapIdOf(MzDataFile pMap)
    {
        var id = pMap.Root.Member("id")?.IntOr(-1) ?? -1;
        if (id >= 0)
        {
            return id;
        }

        // **Und die Nummer steht auch im Dateinamen**, -- **und ein
        // Leser, der nur die Datei las, hatte fuer jede Karte dieselbe
        // Nummer.**
        var name = Path.GetFileName(pMap.RelativePath);
        var ziffern = new string(name.SkipWhile(pChar => !char.IsDigit(pChar))
            .TakeWhile(char.IsDigit).ToArray());
        return int.TryParse(ziffern, out var ausName) ? ausName : -1;
    }

    /// <summary>
    /// The map the project says it starts on.
    /// </summary>
    /// <returns>The map id, or -1.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And the project says it in <c>System.json</c>, and not in
    /// <c>MapInfos.json</c>.</strong> Measured on a finished project:
    /// <c>"startMapId": 2</c>, <c>"startX": 4</c>, <c>"startY": 11</c>.
    /// </para>
    /// <para>
    /// <strong>And <c>MapInfos.json</c> is the trap.</strong> Measured on
    /// the same project: <em>nineteen</em> entries have no
    /// <c>parentId</c>, <strong>and a reader that took the highest of
    /// those started the game on map 17 instead of map 2</strong> —
    /// <strong>and a reader that took the first of those started it on
    /// map 1</strong>, <strong>and neither is where the game begins.**
    /// This is the same mistake <c>rm2k</c> made with
    /// <c>Directory.EnumerateFiles(...).FirstOrDefault()</c>,
    /// <strong>and here it was nearly made a second time.</strong>
    /// </para>
    /// </remarks>
    private int StartMapFromSystem()
    {
        var pfad = Path.Combine(
            _game.GameDirectory, "data", "System.json");
        if (!File.Exists(pfad))
        {
            return -1;
        }

        MzDataFile system;
        try
        {
            system = MzDataFile.Read("data/System.json", File.ReadAllBytes(pfad));
        }
        catch (MzDataException)
        {
            return -1;
        }

        var start = system.Root.Member("startMapId")?.IntOr(-1) ?? -1;
        if (start >= 0)
        {
            return start;
        }

        // **Und `System.json` ist selbst abgeschnitten, wenn es der
        // Inspektor brachte**, -- **denn es ist eine Metadatei und wird
        // voll gelesen**, **und hier wird es direkt vom Datentraeger
        // gelesen, damit dieselbe Grenze nicht zweimal gilt.**
        return -1;
    }

    private IEnumerable<List<MzCommandEntry>> PageOf(int pMapId)
    {
        var ergebnis = new List<List<MzCommandEntry>>();
        if (!Maps.TryGetValue(pMapId, out var karte))
        {
            return ergebnis;
        }

        // **Und die Seiten liegen unter `events[].pages[].list`, und
        // nicht unter den Feldern der Karte.** **Ein erster Entwurf
        // las die Arrays direkt aus dem Wurzelobjekt**, **und die
        // Karte hat an der Wurzel `events`, `tileset`, `width` und
        // `height`** -- **und keines davon ist eine Befehlsliste**, **und
        // der Lauf hatte nach hundert Bildern nichts getan und keinen
        // Grund gesagt**, **weil er nie eine Seite fand.**
        var ereignisse = karte.Root.Member("events")?.Items;
        if (ereignisse == null)
        {
            return ergebnis;
        }

        foreach (var ereignis in ereignisse)
        {
            var seiten = ereignis.Member("pages")?.Items;
            if (seiten == null)
            {
                continue;
            }

            foreach (var seite in seiten)
            {
                var befehle = (seite.Member("list")?.Items ?? new List<MzValue>())
                    .Select(MzCommandEntry.From)
                    .Where(pBefehl => pBefehl != null)
                    .ToList();
                if (befehle.Count > 0)
                {
                    ergebnis.Add(befehle!);
                }
            }
        }

        return ergebnis;
    }

    private static PluginOperationResult Fail(
        PluginErrorCode pCode, string pMessage)
    {
        return PluginOperationResult.Failed(PluginError.Create(pCode, pMessage));
    }
}
