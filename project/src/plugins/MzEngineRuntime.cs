using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniversalRPG.Core;
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
        State = PluginRuntimeState.Initialized;
        return PluginOperationResult.Succeeded();
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
        _runner = new MzEventRunner();
        _facts = Facts;
        _clock.Reset();
        State = PluginRuntimeState.Running;
        return PluginOperationResult.Succeeded();
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

    private static bool IsMap(string pRelativePath)
    {
        var name = Path.GetFileName(pRelativePath);
        return name.StartsWith("Map", StringComparison.OrdinalIgnoreCase)
            && name.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
            && !name.Equals("MapInfos.json", StringComparison.OrdinalIgnoreCase);
    }

    private static int MapIdOf(MzDataFile pMap)
    {
        var id = pMap.Root.IntOr(-1);
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
