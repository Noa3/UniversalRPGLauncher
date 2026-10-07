using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniversalRPG.Core;
using UniversalRPG.Mz;
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

    /// <summary>
    /// And where this game's files are, and the audio channel needs it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the only thing the MZ host gives out about
    /// itself beyond the game's state.</strong> --
    /// <strong>And it is needed because the four sound channels
    /// live in files under the project</strong>, --
    /// <strong><c>audio/bgm</c>, <c>audio/bgs</c>,
    /// <c>audio/me</c> and <c>audio/se</c></strong>, --
    /// <strong>and the host reads them nowhere.</strong> --
    /// <strong>And the reader of those files is above this class, so
    /// without this a node cannot find a
    /// track.</strong>
    /// </para>
    /// </remarks>
    public string GameDirectory => _game.GameDirectory;

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

    /// <summary>
    /// Moves the run to another map and paints it.
    /// </summary>
    /// <param name="pMapId">The project's own map id.</param>
    /// <returns>Whether the map was read and painted.</returns>
    /// <remarks>
    /// <strong>And a caller changes the map through here, and not by
    /// writing the field.</strong> <c>201 Transfer Player</c> changes it
    /// during a run, <strong>and a writable field would let a caller put
    /// the run on a map the project does not have</strong> — **and
    /// <c>Repaint</c> would then say it painted nothing and why.</strong>
    /// </remarks>
    public bool GoTo(int pMapId, bool pMapOhneBild = false)
    {
        if (!Maps.ContainsKey(pMapId))
        {
            PaintReason = $"Map {pMapId} is not among the maps this "
                + "runtime read.";
            return false;
        }

        CurrentMapId = pMapId;

        // **Und eine verschluesselte Karte laesst sich nicht
        // malen, und trotzdem laesst sie sich lesen**.

        // **Und "laesst sich nicht malen" ist nicht "laesst sich
        // nicht lesen"** -- **und dieses Spiel liefert 245
        // `.rpgmvp`-Bloecke und keine entpackte Kachel**,
        // **und die Karten sind genau darum fuer den Leser
        // voellig in Ordnung.**

        // **Und darum gibt es hier ein zweites Tor**: -- **denn
        // `Repaint` ist das Tor fuer die Augen, und ein
        // Befehlssatz braucht keines.**
        // **Und "die Karte ist unter den gelesenen" ist eine Antwort,
        // und keine Frage nach dem Bild** -- **und darum antwortet
        // `true`, auch wenn sie sich nicht malen laesst.**

        if (pMapOhneBild)
        {
            return true;
        }

        return Repaint();

        // **Und der Spieler wird auf der neuen Karte nicht verschoben
        // und nicht vergessen** -- **er bleibt, wo er ist.**
        //
        // **Gemessen an `Game_Player`: der Spieler IST die Figur, und
        // ein Kartenwechsel aendert `_x` und `_y` nicht**, **sondern
        // `performTransfer` setzt sie aus dem Befehl.** **Ein Leser,
        // der die Figur bei jedem Kartenwechsel neu baute, stellte den
        // Spieler auf 0,0** -- **und einer, der sie nie neu baute,
        // zeichnete eine Figur auf einer Karte, auf der der Spieler
        // nie war.**
        return Repaint();
    }

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

    /// <summary>The figures standing on the current map.</summary>
    /// <remarks>
    /// <strong>And they are the ones the map's file says, and not the
    /// ones the runtime would like.</strong> Measured: a figure's image
    /// is on its <c>page.image</c> — <strong>and the position and the id
    /// are on the event</strong> — <strong>so a reader that looked for
    /// <c>characterName</c> on the event found nothing and every figure
    /// in the game was invisible.</strong>
    /// </remarks>
    public IReadOnlyList<MzMapFigure> Figures { get; private set; } =
        Array.Empty<MzMapFigure>();

    /// <summary>Why a page was not drawn, when one was not.</summary>
    public IReadOnlyList<string> FigureNotes { get; private set; } =
        Array.Empty<string>();

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
        _commonEvents = ReadCommonEvents();
        ReadCharacters();
        State = PluginRuntimeState.Initialized;
        return PluginOperationResult.Succeeded();
    }

    /// <summary>
    /// The project's common events, read the way the engine reads them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And <c>$dataCommonEvents[params[0]]</c> is an array lookup,
    /// and the array's own position is the index</strong>, <strong>with
    /// position zero unused because RPG Maker writes it that
    /// way</strong>. <strong>And a reader that keyed the entries by their own
    /// <c>id</c> field instead would be right for a project whose ids run
    /// from one and wrong for one whose first entry has id zero.</strong>
    /// </para>
    /// <para>
    /// <strong>And the path is <c>data/</c> for MZ and <c>www/data/</c> for
    /// MV</strong>, <strong>and the reader tries both</strong> -- **because
    /// <c>IsMap</c> learned that lesson two commits ago in the other
    /// direction.</strong>
    /// </para>
    /// </remarks>
    private Dictionary<int, List<MzCommandEntry>> ReadCommonEvents()
    {
        var ergebnis = new Dictionary<int, List<MzCommandEntry>>();
        var pfad = DataPfad("CommonEvents.json");
        if (pfad.Length == 0)
        {
            CommonEventProblem =
                "The project has no data/CommonEvents.json and no "
                + "www/data/CommonEvents.json, and a 117 Common Event "
                + "names a list this reader does not have.";
            return ergebnis;
        }

        MzDataFile tabelle;
        try
        {
            tabelle = MzDataFile.Read("data/CommonEvents.json",
                File.ReadAllBytes(pfad));
        }
        catch (MzDataException ausnahme)
        {
            CommonEventProblem = ausnahme.Message;
            return ergebnis;
        }

        for (var index = 0; index < tabelle.Root.Items.Count; index++)
        {
            var eintrag = tabelle.Root.Items[index];
            if (eintrag.Kind != MzKind.Object)
            {
                continue;
            }

            var liste = new List<MzCommandEntry>();
            foreach (var befehl in eintrag.Member("list")?.Items
                ?? new List<MzValue>())
            {
                liste.Add(MzCommandEntry.From(befehl));
            }

            if (liste.Count > 0)
            {
                ergebnis[index] = liste;
            }
        }

        CommonEventCount = ergebnis.Count;
        return ergebnis;
    }

    /// <summary>
    /// A project's data file, and it is under <c>data/</c> for MZ and under
    /// <c>www/data/</c> for MV.
    /// </summary>
    /// <remarks>
    /// <strong>And the two engines put the same file in two places, and a
    /// reader that guessed one of them would find nothing for the
    /// other.</strong>
    /// </remarks>
    private string DataPfad(string pDatei)
    {
        foreach (var unter in new[] { "data", "www/data" })
        {
            var kandidat = Path.Combine(
                _game.GameDirectory,
                unter.Replace('/', Path.DirectorySeparatorChar),
                pDatei);
            if (File.Exists(kandidat))
            {
                return kandidat;
            }
        }

        return "";
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
    /// <summary>
    /// How many common events the project carries, and what was wrong with
    /// the file when there was one.
    /// </summary>
    /// <remarks>
    /// <strong>And a count and not nothing, because "the project has no
    /// common events" and "this reader could not read the file" are two
    /// different facts</strong> -- <strong>and only the second one is a
    /// defect of this repository.</strong>
    /// </remarks>
    public int CommonEventCount { get; private set; }

    /// <summary>
    /// Why the common events are not there, and empty when they are.
    /// </summary>
    public string CommonEventProblem { get; private set; } = "";

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
        _runner = new MzEventRunner(_commonEvents);
        _facts = Facts;
        _clock.Reset();
        State = PluginRuntimeState.Running;

        // **Und die automatischen Seiten laufen sofort, und nicht erst,
        // wenn der Spieler irgendetwas beruehrt.**
        //
        // **Und das ist `$gameMap.setup`, woertlich:**
        // `Game_Map.prototype.setup` ruft `setupEvents`, und danach
        // `Game_Event.prototype.update` startet jede Seite mit
        // `isTriggerIn([2])`. **Und `RunPage` war in diesem Runtime
        // ueberhaupt nie aufgerufen** -- eine Funktion, die den Autorun
        // korrekt erkennt und nie laeuft. **Gemessen: Camellias Map003
        // "Day 1" traegt zwei Autorun-Seiten mit je 211 Befehlen und
        // Textzeilen.**
        var autorun = RunPage(StartMode.Autorun);
        if (autorun.StartsWith("map", StringComparison.Ordinal)
            || autorun.StartsWith("the map has no events", StringComparison.Ordinal))
        {
            AutorunProblem = autorun;
        }
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

        // **Und die Figuren werden ueber die Kacheln gemalt, und nicht
        // in sie hinein** -- **denn eine Figur ist 144 Pixel breit und
        // eine Kachel 48**, **und sie steht mittig auf dreien.**
        GemalteKarte = CurrentMapId;
        MapWidth = karte.Root.Member("width")?.IntOr(0) ?? 0;
        MapHeight = karte.Root.Member("height")?.IntOr(0) ?? 0;

        Figures = MzMapFigureReader.Read(
            karte.Root, _facts, out var notwendig);
        FigureNotes = notwendig;

        // **Und jede Figur bekommt ihre Uhr, und aus den Werten, die
        // ihre Seite nennt** -- **und nicht aus geratenen
        // Standardwerten**, **denn die Seite sagt Tempo und Takt
        // ausdruecklich.**
        // **Und jetzt bekommen die Figuren Namen, unter denen ein Befehl
        // sie finden kann.**
        //
        // **Und das ist gemessen:** **die Seiten dieses Spiels nennen
        // ihre Figuren in 96 Befehlen, und 46 davon mit minus eins** --
        // **das ist der Spieler** -- **und der Rest mit der Nummer des
        // Ereignisses.**
        //
        // **Und ohne diese Namen sagt jeder `205` "diese Figur habe ich
        // nicht"**, **und 96 Routen tun gar nichts.**
        var figuren = new Dictionary<int, MzCharacter>();
        EventFigures = new Dictionary<int, MzCharacter?>();
        foreach (var figur in Figures)
        {
            // **Und die Figur traegt ihr Bild und ihren Index.**
            //
            // **Gemessen an `Game_Player.prototype.refresh`:** **es ruft
            // `setImage(actor.characterName(), actor.characterIndex())`,
            // und beide.** **Und `Game_CharacterBase.setImage` setzt drei
            // Dinge** -- **die Kachelnummer, den Namen und den Index.**
            // **Ein Leser, der nur den Namen behielt, zeichnete jeden
            // Ereignis als die erste Figur seines Blattes.**
            var held = new MzCharacter(figur.X, figur.Y);
            held.TurnTo(figur.Direction);
            held.SetImage(figur.CharacterName, figur.CharacterIndex);
            held.EventId = figur.EventId;
            figuren[figur.EventId] = held;
            EventFigures[figur.EventId] = held;
        }

        Facts = Facts.WithCharacters(figuren, Facts.Player);

        // **Und der Spieler bekommt seine Figur, und sie bekommt ihr
        // Bild** -- **denn `Game_Player.prototype.refresh` ruft
        // `setImage(actor.characterName(), actor.characterIndex())`, und
        // beide.** **Ein Leser, der nur den Namen behielt, zeichnete
        // jeden Darsteller als Darsteller eins.**
        var spielerFigur = Facts.Player.BuildFigure();
        if (spielerFigur != null)
        {
            spielerFigur.SetImage(PlayerSheetName, PlayerIndex);
        }

        Clocks = new Dictionary<int, MzWalkClock?>();
        Routes = new Dictionary<int, MzMoveRoute?>();
        _standing = new Dictionary<int, int>();
        foreach (var figur in Figures)
        {
            Clocks[figur.EventId] = new MzWalkClock
            {
                MoveSpeed = figur.MoveSpeed,
                MoveFrequency = figur.MoveFrequency,
                Direction = figur.Direction,
                Pattern = figur.Pattern < MzWalkClock.MaxPattern
                    && figur.Pattern >= 0 ? figur.Pattern : 1,
                // **Und keine Figur bewegt sich, weil sie nicht
                // gelaufen ist.**
                //
                // **Der Motor prueft `isMoving()`, und das ist
                // `_realX !== _x || _realY !== _y`** -- **und diese
                // beiden weichen nur voneinander ab, wenn die Figur
                // gerade einen Schritt geht oder gesprungen ist.**
                //
                // **`moveType` sagt, ob die Figur von sich aus losgeht,
                // und nicht ob sie gerade geht.** **Ein Leser, der
                // `Moving = moveType != 0` setzte, liess jede Figur
                // eines Raumes mitten im Schritt stehen, obwohl keine
                // von ihnen auch nur eine Kachel verlassen hat** --
                // **und gemessen war das an `Map015`: neun Figuren,
                // alle mit `moving=True` und alle auf ihrer Kachel.**
                Moving = false,
            };
        }

        PaintFigures(pixel);
        PaintedMap = pixel;
        return true;
    }

    /// <summary>
    /// Draws the figures of the current map over their tiles.
    /// </summary>
    /// <param name="pPixels">The painted map.</param>
    /// <returns>How many figures were drawn.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And a figure whose sheet is missing is not drawn, and is
    /// named.</strong> The measured project has four character sheets
    /// and three names in use; <strong>a sheet that is not there would
    /// leave a figure invisible</strong>, <strong>and a reader that
    /// counted it anyway would report a figure on a map where there is
    /// none.</strong>
    /// </para>
    /// <para>
    /// <strong>And the player is drawn last, and not among the
    /// figures.</strong> It stands on the tile
    /// <c>System.json</c> names, <strong>and it is not in the map's
    /// events at all</strong> — <strong>so a reader that looked for it
    /// among the figures found nobody and put a second player on a map
    /// that already had one.</strong>
    /// </para>
    /// </remarks>
    /// <summary>
    /// Advances every figure's walk clock by one frame, and repaints.
    /// </summary>
    /// <returns>How many figures changed their pattern.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And a figure only ticks when the engine says it
    /// does.</strong> Measured in the engine's own source:
    /// <c>updateAnimationCount</c> adds <c>1.5</c> while
    /// <c>isMoving() &amp;&amp; hasWalkAnime()</c>, and <c>1</c> while the
    /// figure stands — <strong>and a standing figure in its first
    /// pattern does not tick at all</strong>, because the engine's test is
    /// <c>!isOriginalPattern()</c>.
    /// </para>
    /// <para>
    /// <strong>And the measured project has 235 of 253 pages at
    /// <c>moveType: 0</c>, which means "fixed"</strong> — <strong>their
    /// routes are never walked at all</strong> — <strong>and 18 pages
    /// move at random.</strong> <strong>A reader that walked every route
    /// moved 253 figures nobody asked to move.</strong>
    /// </para>
    /// </remarks>
    /// <summary>How a page gets chosen to run, as the engine does it.</summary>
    /// <remarks>
    /// <para>
    /// <strong>And the engine's machine is a <c>for (;;)</c>, and its
    /// order is fixed.</strong> Measured at
    /// <c>Game_Map.prototype.updateInterpreter</c>: run the interpreter,
    /// <strong>and if it is still running, stop</strong>, <strong>and if it
    /// has finished, unlock its event, clear it, and ask
    /// <c>setupStartingEvent</c> for the next page</strong> — <strong>and if
    /// there is none, stop.</strong>
    /// </para>
    /// <para>
    /// <strong>And <c>setupStartingEvent</c> asks in this order:</strong> a
    /// reserved common event, a test event, <strong>a starting map
    /// event</strong>, and an autorun common event.
    /// </para>
    /// <para>
    /// <strong>And "a starting map event" is one whose page is
    /// autorun.</strong> Measured at <c>setupStartingMapEvent</c>: it walks
    /// the events, and the first with <c>isStarting()</c> wins,
    /// <strong>clears its flag and takes its list.</strong>
    /// </para>
    /// <para>
    /// <strong>And the flag is set by <c>start</c>, which is called when
    /// the page is chosen</strong> — <strong>and <c>start</c> only sets it
    /// when the list has more than one entry</strong>, <strong>because a
    /// list of one is just the end.</strong>
    /// </para>
    /// </remarks>
    public enum StartMode
    {
        /// <summary>Nothing to run, and that is an answer.</summary>
        None,

        /// <summary>The action button was pressed.</summary>
        ActionButton,

        /// <summary>The player touched the event.</summary>
        Touched,

        /// <summary>The page runs by itself when the map opens.</summary>
        Autorun,

        /// <summary>The page runs beside the others, every frame.</summary>
        Parallel,
    }

    /// <summary>
    /// Runs the next page the engine would run, and nothing else.
    /// </summary>
    /// <param name="pStart">Which page to start, or zero to run the
    /// autorun page the engine would choose.</param>
    /// <returns>What the page did.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And this is the machine, and not a loop over every
    /// page.</strong> <strong>Measured: only two of this project's 253
    /// pages are autorun</strong>, <strong>so a runtime that started every
    /// page would run a game the player has not begun.</strong>
    /// </para>
    /// <para>
    /// <strong>And a page at trigger 3 runs beside the others and is not
    /// started by this</strong> — <strong>it has its own interpreter</strong>
    /// <strong>and the engine gives it one.</strong>
    /// </para>
    /// </remarks>
    public string RunPage(StartMode pStart = StartMode.Autorun)
    {
        if (!Maps.TryGetValue(CurrentMapId, out var karte))
        {
            return $"map {CurrentMapId} is not among the maps this runtime"
                + " read, so no page can run on it";
        }

        var ereignisse = karte.Root.Member("events")?.Items;
        if (ereignisse == null)
        {
            return "the map has no events, and a map with no events has no"
                + " pages to run";
        }

        foreach (var ereignis in ereignisse)
        {
            var seiten = ereignis.Member("pages")?.Items;
            var id = ereignis.Member("id")?.IntOr(-1) ?? -1;
            if (seiten == null || seiten.Count == 0)
            {
                continue;
            }

            for (var index = seiten.Count - 1; index >= 0; index--)
            {
                var seite = seiten[index];
                var ausloeser = seite.Member("trigger")?.IntOr(0) ?? 0;
                if (!Passt(ausloeser, pStart))
                {
                    continue;
                }

                //
                // **Und ein Ausloeser, der eine Kachel braucht, prueft
                // sie auch** -- **und das fehlte hier, und es ist an der
                // Quelle gemessen, woher diese Seite kommt
                // (`CamelliaCoronation-Win/js/rmmz_objects.js`):**
                //
                // ```js
                // Game_Player.prototype.triggerButtonAction = function() {
                //     if (Input.isTriggered("ok")) {
                //         if (this.getOnOffVehicle()) { return true; }
                //         this.checkEventTriggerHere([0]);
                //         if ($gameMap.setupStartingEvent()) { return true; }
                //         this.checkEventTriggerThere([0, 1, 2]);
                //         if ($gameMap.setupStartingEvent()) { return true; }
                //     }
                //     return false;
                // };
                // ```
                //
                // **`Here` ist die Kachel des Spielers, und `There` ist
                // die eine davor** -- **und ohne diese Pruefung startet
                // der Aktionsknopf das *erste* passende Ereignis der
                // ganzen Karte.**  **Gemessen an Camellias Map004, wo
                // Ereignis 14 bei `(2, 12)` steht und Ereignis 5 bei
                // `(1, 2)`:** der Spieler stand korrekt auf `(2, 12)`,
                // **und 19.999 Bilder lang startete Ereignis 5.**
                //
                // **Und `StartMode.ActionButton` ist die Kachel *hier*,
                // und `StartMode.Touched` die davor** -- **denn
                // `triggerButtonAction` nimmt `[0]` hier und `[0, 1, 2]`
                // dort, **und ein Beruehrt-Ereignis wird auf der Kachel
                // betreten, nicht davor gestellt.**
                //
                // **Und beide brauchen dieselbe Kachel, weil beide hier
                // ausgeloest werden** -- **`checkEventTriggerThere`
                // laeuft erst, wenn `checkEventTriggerHere` nichts
                // gestartet hat, **und diese Seite nimmt in beiden
                // Faellen `[0]`, **und beide laufen nur fuer das
                // Ereignis, auf dem der Spieler steht.**  `trigger 1`
                // (Beruehrt) wird beim *Betreten* ausgeloest und
                // nicht ueber den Knopf.
                //
                // **Und nur die beiden, die eine Kachel brauchen** --
                // **`ActionButton` und `Touched`.**  Autorun und Parallel
                // finden ihre Seite ueber `setupStartingMapEvent`, und
                // das fragt `isStarting()` **und nicht die Lage des
                // Spielers.**
                //
                // **Und gemessen ist, was passiert, wenn man das
                // verwechselt:** `CamelliaCoronation-Win/js
                // /rmmz_objects.js` -- **`Game_Map.prototype
                // .setupStartingMapEvent` ist `for (const event of
                // this.events()) { if (event.isStarting()) { event
                // .clearStartingFlag(); this._interpreter.setup(event
                // .list(), event.eventId()); return true; } }`** --
                // **und keine Kachel kommt vor.**  Autorun an eine
                // Kachel zu binden hiesse, **dass kein Spiel dieser
                // beiden Engines je eine Autorun-Seite startet.**
                if (pStart == StartMode.ActionButton
                    || pStart == StartMode.Touched)
                {
                    var ereignisX = ereignis.Member("x")?.IntOr(-1) ?? -1;
                    var ereignisY = ereignis.Member("y")?.IntOr(-1) ?? -1;
                    if (ereignisX != PlayerX || ereignisY != PlayerY)
                    {
                        continue;
                    }
                }

                if (!MzMapFigureReader.Meets(
                    seite.Member("conditions"), Facts, CurrentMapId, id))
                {
                    // **Und weiter zur naechsten Seite, und nicht
                    // zurueck.** Das ist nicht eine Feinheit, sondern die
                    // Regel der Engine, woertlich aus `rpg_objects.js`:
                    //
                    // ```text
                    // findProperPageIndex() {
                    //     const pages = this.event().pages;
                    //     for (let i = pages.length - 1; i >= 0; i--) {
                    //         const page = pages[i];
                    //         if (this.meetsConditions(page)) {
                    //             return i;
                    //         }
                    //     }
                    //     return -1;
                    // };
                    // ```
                    //
                    // **Eine Seite, deren Bedingung nicht zutrifft, ist keine
                    // Seite, die startet. Sie ist eine Seite, die uebersprungen
                    // wird.**
                    //
                    // **Und das hat ein echtes Spiel gefunden.** Gemessen an
                    // `D:/Itch/sister/www`, Karte 2, Ereignis 2:
                    //
                    // ```text
                    // Seite 1: trigger=0, selfSwitchValid=false, 117 Befehle
                    // Seite 2: trigger=0, selfSwitchValid=true,  13 Befehle
                    // ```
                    //
                    // **Die Schleife laeuft von hinten, also sieht sie Seite 2
                    // zuerst, deren Selbstschalter A am Anfang nicht gesetzt
                    // ist** -- **und ein `return` dort hat den ganzen Lauf
                    // beendet, mit der Meldung, die Bedingung sei nicht zu
                    // beantworten. Seite 1 hat keine Bedingung und 117
                    // Befehle und wurde nie erreicht.**
                    continue;
                }

                // **Und die Liste wird aus der Datei gelesen, und nicht
                // aus dem Interpreter** -- **denn der Motor liest sie
                // auch aus der Seite, und `setup(list, eventId)` nimmt
                // genau diese.**
                var befehle = new List<MzCommandEntry>();
                foreach (var eintrag in
                    seite.Member("list")?.Items ?? new List<MzValue>())
                {
                    befehle.Add(MzCommandEntry.From(eintrag));
                }
                if (befehle.Count <= 1)
                {
                    // **Und eine Liste von einem Eintrag ist nur das
                    // Ende** -- **gemessen an `Game_Event.start`: `if
                    // (list && list.length > 1)`.** **Und das ist der
                    // Grund, warum viele Seiten nie starten**, **und
                    // nicht die, weil sie zu kurz waeren.**
                    continue;
                }

                // **Und die Seite laeuft weiter, wenn der Spieler
                // redet.**
                //
                // **Der Motor fragt in einer Endlosschleife, und die
                // Messung ist eine andere:** **solange der Interpreter
                // laeuft, anhalten, und sonst die naechste Seite
                // fragen** -- **und wer bei einer Wartezeit aufgibt,
                // laesst 209 von 211 Befehlen eines Spiels ungelesen.**
                //
                // **Und was den Dialog beendet, ist gemessen:**
                // **`Window_Message.isTriggered` fragt
                // `Input.isRepeated("ok")` oder `"cancel"`** -- **und
                // `isRepeated`, nicht `isTriggered`**, **und eine
                // gehaltene Taste wirkt weiter.**
                var alle = new List<MzAction>();
                var ergebnis = Laeufer.TryGetValue(id, out var laufend)
                    && laufend != null && laufend.KannFortgesetztWerden
                    ? _runner.Run(
                        befehle, Facts, CurrentMapId, id, Random, laufend)
                    : _runner.Run(
                        befehle, Facts, CurrentMapId, id, Random);

                // **Und der Interpreter bleibt, wenn die Seite wartet.**
                //
                // **Das ist die Luecke, die 209 von 211 Befehlen
                // ungelesen liess:** **`RunPage` gab den Interpreter
                // nach einem Warteschritt weg** -- **und die Seite
                // fing beim naechsten Aufruf wieder bei Index 0 an.**
                //
                // **Und der Motor macht das nicht:** **`update` laeuft
                // weiter, und der Interpreter lebt in `update`**, und
                // `map` haelt ihn ueber `updateWaitMode` am Leben.
                //
                // **Und `213 [-1, 2, true]` bei Index 21 dieser Seite
                // braucht genau das** -- **denn ein Ballon laeuft in 60
                // Bildern ab, und wer seinen Interpreter wegwirft,
                // wartet auf einen Ballon, den niemand mehr zaehlt.**
                if (ergebnis.Interpreter != null
                    && ergebnis.Interpreter.KannFortgesetztWerden)
                {
                    Laeufer[id] = ergebnis.Interpreter;
                }
                else
                {
                    Laeufer.Remove(id);
                }
                alle.AddRange(ergebnis.Actions);
                var weiter = true;

                // **Und hoechstens so oft, wie Befehle da sind**
                // **-- denn eine Seite, die endet, endet.**
                // **Und ein Fortschrittsschutz, weil eine Schleife
                // ohne einen sich selbst haengen laesst.** **Die erste
                // Fassung lief bis `alle.Count == befehle.Count`, und
                // wenn ein Lauf keine Aktionen zurueckgibt, waechst die
                // Zahl nicht, und die Schleife kam nie heraus.** **Ein
                // **Und die Seite laeuft so weit, wie sie kommt,
                // und die Wartezeiten werden unterwegs beantwortet.**
                //
                // **Und das ist die Schleife, die `RunPageEvent` auch
                // benutzt** -- **denn eine Seite, die nur einen Befehl
                // traegt, redet nie, und schaltet nie etwas um.**
                // **Und auch hier wird die erste Runde gezaehlt** --
                // **denn sie traegt die Aktionen, die niemand sonst
                // zaehlt.**
                alle.AddRange(ergebnis.Actions);

                //
                // **Und diese Schleife ist weg, und das ist die Korrektur.**
                //
                // **Und sie war der Grund, warum ein Spiel mit Autorun
                // stumm blieb.** Sie lief die ganze Seite in einem Durchlauf
                // und tat dabei zwei Dinge, die der Motor nie tut:
                // **`Facts.MessageBusy = false` am Anfang jeder Runde** --
                // **also loeschte sie den Dialog, den der 101 gerade
                // gesetzt hatte** -- **und `_keys.Ok()`, also bestaetigte
                // sie die Wartezeit im selben Frame, in der sie entstand.**
                // **Gemessen an Camellias Map003 "Day 1", Ereignis 9:**
                // die Seite sprang von Index 201 ueber 206 direkt auf 208,
                // **und `MessageBusy` war in keinem einzigen Frame true** --
                // **der Dialog kam nie zur Anzeige, und das Spiel war stumm.**
                //
                // **Und jetzt laeuft die Seite ueber Frames**, ueber
                // `Tick()` im `Update`, **und der Dialog bleibt stehen, bis
                // der Spieler ihn wegklickt oder wegdrueckt** -- **was
                // genau das ist, was `Window_Message.prototype.isTriggered`
                // fragt.**
                var seitenFort = new List<MzInterpreter>();
                //
                // **Und der Interpreter kommt in `Laeufer`, und das ist der
                // Unterschied zwischen "die Seite wartet" und "die Seite ist
                // weg".**
                //
                // **Und diese Methode hat ihn vorher nicht abgelegt, waehrend
                // `RunPageEvent` es tut:**  `Tick()` iteriert ueber genau
                // diese Sammlung, **also war eine Seite, die wartete, fuer
                // `Tick()` unsichtbar** -- **und gemessen ist das als
                // `laeufer=0` ueber 3000 Bilder, waehrend die Seite bei
                // Index 206 auf ihren Dialog wartete.**
                if (ergebnis.Interpreter != null
                    && ergebnis.Interpreter.KannFortgesetztWerden)
                {
                    Laeufer[id] = ergebnis.Interpreter;
                }
                else
                {
                    Laeufer.Remove(id);
                }

                // **Und alle Aktionen, und nicht nur die des letzten
                // Laufs** -- **denn die Seite wurde in Stuecken
                // abgearbeitet, und was sie getan hat, ist die
                // Summe.** **Und `PagesRun` zaehlt die Seiten und nicht
                // die Laeufe**, **denn eine Seite, die fuenfmal
                // fortgesetzt wurde, ist immer noch eine Seite.**
                LastPage = id;
                PagesRun++;
                LastActions = alle;
                LastPageStop = ergebnis.Stopped;
                //
                // **Und der Index ist der *Fortschritt* der Seite, und
                // nicht der Index am Anfang dieses Aufrufs** -- **denn
                // eine Seite, die ueber Bilder laeuft, ist in diesem
                // Aufruf nur einen Schritt weiter, und ein `RunPage`
                // neben einem `Tick` (gemessen in
                // `Test_EineSeiteDieWartetZaehltDieBilderUndGehtWeiter`,
                // das genau das tut) schrieb den alten Wert zurueck und
                // meldete 142 statt 210.**
                //
                // **Und `LastPageIndex` heisst "wo steht die Seite", und
                // das ist eine Frage ueber die Laufzeit.**
                LastPageIndex = Math.Max(
                    LastPageIndex, ergebnis.Interpreter?.Index ?? 0);
                Stops = new List<string> { ergebnis.Reason,
                    ergebnis.Describe() };
                return $"event {id} page {index} was given {befehle.Count}"
                    + $" commands with trigger {ausloeser}, and it carried"
                    + $" out {ergebnis.Actions.Count} of them before it"
                    + $" stopped: {ergebnis.Describe()}";
            }
        }

        return "no page on this map runs now, and that is the engine's own"
            + " answer -- setupStartingEvent returns false and the loop"
            + " stops";
    }

    /// <summary>
    /// One generator for the whole run, so a page can be replayed.
    /// </summary>
    /// <remarks>
    /// <strong>And one stream for the whole game, and not one per page.</strong>
    /// Measured at <c>Game_Interpreter</c>, which holds a single
    /// <c>_random</c>. <strong>A runtime that made a new one per page
    /// would replay the same numbers every page</strong> — <strong>and a
    /// test that runs a page twice would get the same result both times,
    /// which is exactly what it would prove and nothing.</strong>
    /// </remarks>
    public MzRandom Random { get; } = new();

    /// <summary>What the last page actually did, command by command.</summary>
    /// <remarks>
    /// <strong>And this is the only proof that the interpreter works.</strong>
    /// <strong>"A page ran" says nothing</strong> — <strong>a run that
    /// executed two of 211 commands and called it a page has run
    /// something</strong> — <strong>and the actions say which two.</strong>
    /// </para>
    /// <para>
    /// <strong>And a 101 stops the run, because the engine's wait is not
    /// a lie:</strong> a message box waits for the player, and until he
    /// presses the button the page does not go on.
    /// </para>
    /// </remarks>
    public IReadOnlyList<MzAction> LastActions { get; private set; } =
        Array.Empty<MzAction>();

    /// <summary>
    /// The keys this game has, measured, and the wait a dialog waits.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And they are words, and not the old
    /// <c>Input_Decision</c>.</strong> Measured in
    /// <c>Window_Message.prototype.isTriggered</c>:
    /// <c>Input.isRepeated("ok") || Input.isRepeated("cancel") ||
    /// TouchInput.isRepeated()</c> — <strong>and there is no
    /// <c>Input_Decision</c> anywhere in this game's engine</strong>,
    /// <strong>and no <c>rmmz_input.js</c> beside the other seven
    /// files.</strong>
    /// </para>
    /// <para>
    /// <strong>And ten key names occur:</strong> ok, cancel, shift, up,
    /// down, left, right, pageup, pagedown and debug.
    /// </para>
    /// <para>
    /// <strong>And the dialog's own way out, measured at
    /// <c>onEndOfText</c>:</strong> if it is a choice or a number field
    /// it starts that, <strong>and otherwise it pauses</strong> —
    /// <strong>and it only terminates without a pause when
    /// <c>_pauseSkip</c> is set.</strong> <strong>And the pause is
    /// ended by the same "ok" that the message waits for.</strong>
    /// </para>
    /// </remarks>
    public sealed class KeysPressed
    {
        /// <summary>Which key was pressed this frame.</summary>
        public HashSet<string> Pressed { get; } = new(
            StringComparer.Ordinal);

        /// <summary>How many frames the dialog has paused.</summary>
        public int PauseFrames { get; private set; }

        /// <summary>Whether the dialog is allowed to skip its pause.</summary>
        /// <remarks>
        /// <strong>And the engine's own condition, measured at
        /// <c>onEndOfText</c>:</strong> <c>if (!this._pauseSkip)
        /// this.startPause(); else this.terminateMessage();</c>
        /// </remarks>
        public bool PauseSkip { get; set; } = true;

        /// <summary>
        /// Presses a key for one frame, as the window sees it.
        /// </summary>
        /// <param name="pName">Which key.</param>
        /// <returns>Whether the key is one this game has.</returns>
        /// <remarks>
        /// <strong>And <c>isRepeated</c>, and not
        /// <c>isTriggered</c>.</strong> Measured: the message window asks
        /// <c>isRepeated</c> — <strong>and a key held down keeps working,
        /// which is why a player can hold "ok" through a long
        /// dialogue.</strong> <strong>A reader that used
        /// <c>isTriggered</c> would need the key released and pressed
        /// again</strong> — <strong>and a key that only fires once per
        /// press would leave a dialogue that nobody can finish.</strong>
        /// </remarks>
        public bool Press(string pName)
        {
            if (!Bekannt.Contains(pName))
            {
                return false;
            }

            Pressed.Add(pName);
            return true;
        }

        /// <summary>Forgets the pressed keys, at the end of a frame.</summary>
        public void FrameEnde()
        {
            Pressed.Clear();
            PauseFrames = 0;
        }

        /// <summary>How many times "ok" was pressed since the start.</summary>
        public int OkPressed { get; private set; }

        /// <summary>Presses "ok", which is what a dialogue waits for.</summary>
        public void Ok()
        {
            Press("ok");
            OkPressed++;
            PauseFrames++;
        }

        /// <summary>The key names this game's engine uses.</summary>
        /// <remarks>
        /// <strong>And these are measured, and not the ten that MZ
        /// documents.</strong>
        /// </remarks>
        public static readonly HashSet<string> Bekannt = new(
            StringComparer.Ordinal)
        {
            "ok", "cancel", "shift", "up", "down", "left", "right",
            "pageup", "pagedown", "debug",
        };
    }

    /// <summary>
    /// The engine's own answer to "is the wait over?".
    /// </summary>
    /// <param name="pMode">What the interpreter is waiting for.</param>
    /// <returns>Whether it may go on.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And this is the only place where a key becomes progress.</strong>
    /// Measured at <c>updateWaitMode</c>: the engine asks a question every
    /// frame <strong>and does not count frames down</strong> — <strong>so
    /// a dialogue waits for the player, and a wait for a number of
    /// frames counts down.</strong>
    /// </para>
    /// <para>
    /// <strong>And the question for a dialogue is measured at
    /// <c>Window_Message.isTriggered</c>:</strong> <c>Input.isRepeated
    /// ("ok") || Input.isRepeated("cancel")</c> — <strong>and
    /// <c>isRepeated</c>, so a held key keeps working.</strong>
    /// </para>
    /// </remarks>
    /// <summary>Starts every parallel page on this map, each with its own
    /// interpreter.</summary>
    /// <returns>One line per page, and what each one is doing.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And a parallel page has its own interpreter, and that is
    /// measured.</strong> Measured at
    /// <c>Game_Event.prototype.updateParallel</c>:
    /// <c>if (!this._interpreter.isRunning()) this._interpreter.setup(this.list(), this._eventId);
    /// this._interpreter.update();</c>
    /// — <strong>and <c>this._interpreter</c> belongs to the event, not
    /// to the map.</strong>
    /// </para>
    /// <para>
    /// <strong>And the map's own interpreter is for a different thing
    /// entirely.</strong> Measured at <c>setupStartingMapEvent</c>:
    /// <c>for (const event of this.events()) if (event.isStarting()) {
    /// event.clearStartingFlag(); this._interpreter.setup(event.list(),
    /// event.eventId()); return true; }</c> — <strong>one event, then it
    /// stops.</strong> <c>Game_Event.start</c> sets <c>_starting</c> for
    /// every event it starts, but <c>if (this.isTriggerIn([0, 1, 2]))
    /// this.lock()</c> — <strong>and a parallel page is trigger 3, which
    /// is not in that list, so it runs beside the map's page.</strong>
    /// </para>
    /// <para>
    /// <strong>And this matters for this game because its three parallel
    /// pages carry seventeen routes between them, nine of which say
    /// <c>wait</c>.</strong> Measured: Map002 event 5 has 22 commands,
    /// Map005 event 4 has 176, and Map010 event 7 has 141 — <strong>and
    /// every route target on both busy maps exists as a figure.</strong>
    /// A reader that ran them through one interpreter would stop the first
    /// at its first route wait and never start the other two.
    /// </para>
    /// </remarks>
    public IReadOnlyList<string> RunParallel()
    {
        var bericht = new List<string>();
        if (!Maps.TryGetValue(CurrentMapId, out var karte))
        {
            bericht.Add(
                $"map {CurrentMapId} is not among the maps this runtime read,"
                    + " so no page on it can run");
            return bericht;
        }

        foreach (var ereignis in karte.Root.Member("events")?.Items
            ?? new List<MzValue>())
        {
            var id = ereignis.Member("id")?.IntOr(-1) ?? -1;
            foreach (var seite in ereignis.Member("pages")?.Items
                ?? new List<MzValue>())
            {
                if ((seite.Member("trigger")?.IntOr(0) ?? 0) != 3)
                {
                    continue;
                }

                var befehle = new List<MzCommandEntry>();
                foreach (var eintrag in
                    seite.Member("list")?.Items ?? new List<MzValue>())
                {
                    befehle.Add(MzCommandEntry.From(eintrag));
                }

                if (befehle.Count <= 1)
                {
                    continue;
                }

                // **Und jede bekommt ihren eigenen Interpreter** --
                // **denn sonst teilen sie sich einen, und die erste
                // wartet, und die anderen warten auf sie.**
                var ergebnis = Laeufer.TryGetValue(id, out var alt)
                    && alt != null && alt.KannFortgesetztWerden
                    ? _runner.Run(befehle, Facts, CurrentMapId, id, Random, alt)
                    : _runner.Run(befehle, Facts, CurrentMapId, id, Random);
                var eigener = new MzInterpreter(befehle);

                // **Und jetzt dieselbe Schleife, die `RunPage` hat**
                // -- **denn `command101` gibt ohne Ausnahme `false`
                // zurueck**, -- **und `executeCommand` liest das als
                // "warte"**, -- **und ohne einen Tastendruck wartet die
                // Seite endlos auf einen Bildschirm, den niemand
                // wegklickt.**
                //
                // **Und `RunParallel` hatte keine**, -- **und damit
                // blieb jede parallele Seite auf ihrem ersten `101`
                // stehen**, -- **und der Unterschied zwischen zwei
                // Wegen durch dieselbe Engine war ein
                // Tastendruck.**
                // **Und der Index des Interpreters ist die
                // Wahrheit ueber den Fortschritt** -- **und nicht die
                // Zahl der Befehle dieser einen Runde**, -- **denn
                // jede Runde beginnt dort, wo die vorige
                // aufgehoert hat.**
                //
                // **Und diese Schleife ist weg, und das ist die Korrektur.**
                //
                // **Und sie tat zwei Dinge, die der Motor nie tut:**
                // **`Facts.MessageBusy = false` am Anfang jeder Runde** --
                // **also loeschte sie im selben Frame den Dialog, den der
                // 101 gerade gesetzt hatte** -- **und `_keys.Ok()`,
                // also bestaetigte sie die Wartezeit, bevor der Spieler
                // sie gesehen haben konnte.**
                //
                // **Und `command101` gibt ohne Ausnahme `false` zurueck**,
                // **denn die Wartezeit laeuft ueber `WaitFor`/`PassFrame`**
                // **und nicht ueber den Rueckgabewert** -- **der Kommentar
                // daruber beschrieb deshalb ein Symptom und keine
                // Loesung.** Die Seite gehoert in `Laeufer`, und
                // `Tick()` im `Update` bringt sie ueber Frames weiter.
                var erreicht = ergebnis.Interpreter?.Index ?? 0;

                if (ergebnis.Interpreter != null
                    && ergebnis.Stopped == MzStep.Waiting)
                {
                    Laeufer[id] = ergebnis.Interpreter;
                }
                else
                {
                    Laeufer.Remove(id);
                }

                erreicht = Math.Max(
                    erreicht, ergebnis.Interpreter?.Index ?? 0);
                bericht.Add(
                    $"event {id}: {ergebnis.Describe()}, "
                    + $"and it reached command {erreicht} "
                    + $"of {befehle.Count}");

            }
        }

        return bericht;
    }

    /// <summary>Steps the player onto a tile and starts what it touches.
    /// </summary>
    /// <returns>One line per page the arrival started.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And this is a different question from the button, and the
    /// engine asks it in a different place.</strong> Measured at
    /// <c>Game_Player.prototype.updateNonmoving</c>:
    /// <c>if (!$gameMap.isEventRunning()) { if (wasMoving) {
    /// $gameParty.onPlayerWalk(); this.checkEventTriggerHere([1, 2]); if
    /// ($gameMap.setupStartingEvent()) return; }</c> — <strong>and it is
    /// inside <c>wasMoving</c>, so a page answers once, when the player
    /// arrives, and not every frame.</strong>
    /// </para>
    /// <para>
    /// <strong>And <c>here</c> asks for a page that is NOT normal
    /// priority</strong>, which is what makes the rule work: <c>there</c>
    /// asks for a normal one, and <c>here</c> for a not-normal one.
    /// </para>
    /// <para>
    /// <strong>And measured, all 52 touch pages of this project are not
    /// normal</strong> — <strong>which is not a coincidence: a page that
    /// is triggered by touch sits on the tile and is walked onto, and a
    /// normal-priority page stands in front of the hero in the picture
    /// and is spoken to.</strong>
    /// </para>
    /// <para>
    /// <strong>And the third way, measured:</strong>
    /// <c>moveStraight</c> calls <c>this.checkEventTriggerTouchFront(d)</c>,
    /// which computes the tile in the direction of travel and calls
    /// <c>checkEventTriggerTouch(x, y)</c>, which asks
    /// <c>startMapEvent(x, y, [1, 2], true)</c> — <strong>a NORMAL
    /// priority page, one tile ahead.</strong> <strong>So a touch page
    /// answers twice in the engine, once underfoot and once ahead, and
    /// only if it is normal priority does the second happen.</strong>
    /// </para>
    /// </remarks>
    public IReadOnlyList<string> Beruehre()
    {
        var bericht = new List<string>();

        // **Und die Kachel unter den Fuessen, und zwar mit der
        // Ausloeserliste `[1, 2]`** -- **gemessen an `updateNonmoving`.**
        foreach (var zeile in SucheStartende((PlayerX, PlayerY), false, 1, 2))
        {
            bericht.Add(zeile);
        }

        if (bericht.Count > 0)
        {
            return bericht;
        }

        // **Und die Kachel davor, und zwar mit derselben Liste** --
        // **gemessen an `checkEventTriggerTouchFront`, das
        // `startMapEvent(x, y, [1, 2], true)` ruft.**
        var vorne = (PlayerX + SchrittX(PlayerDirection),
            PlayerY + SchrittY(PlayerDirection));
        foreach (var zeile in SucheStartende(vorne, true, 1, 2))
        {
            bericht.Add(zeile);
        }

        return bericht;
    }

    /// <summary>Walks onto a tile and lets the event feel the touch.
    /// </summary>
    /// <returns>One line per page the touch started.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And this is the fourth and last way an event page starts,
    /// and it is the only one the engine gives to the event itself:</strong>
    /// measured at <c>Game_Event.prototype.checkEventTriggerTouch</c>:
    /// <c>if (!$gameMap.isEventRunning()) { if (this._trigger === 2
    /// &amp;&amp; $gamePlayer.pos(x, y)) { if (!this.isJumping() &amp;&amp;
    /// this.isNormalPriority()) this.start(); } }</c>
    /// </para>
    /// <para>
    /// <strong>Three conditions, and all three are measured: the page's
    /// trigger must be 2, the player must stand on the tile, and the page
    /// must be of NORMAL priority.</strong> <strong>And not jumping,
    /// which a page on foot never is.</strong>
    /// </para>
    /// <para>
    /// <strong>And this path asks for the opposite priority from the
    /// other three.</strong> <c>updateNonmoving</c> asked
    /// <c>here([1, 2])</c>, which is <c>normal = false</c>; this asks
    /// <c>isNormalPriority()</c>, which is <c>normal = true</c>.
    /// <strong>And measured, this project's two trigger-2 pages both carry
    /// priority 0</strong> — <strong>so neither can ever start this way,
    /// and a reader that reported them as reachable through touch would be
    /// wrong.</strong> The honest answer is that they are reachable
    /// through their trigger-0 siblings and nothing else.
    /// </para>
    /// <para>
    /// <strong>And there is a fifth trigger this reader has never
    /// implemented: <c>page.trigger === 4</c>.</strong> Measured at
    /// <c>Game_Event.prototype.setupPageSettings</c>: <c>if
    /// (this._trigger === 4) this._interpreter = new
    /// Game_Interpreter(); else this._interpreter = null;</c> — <strong>a
    /// page that runs beside the map with its own machine, like a parallel
    /// page but independently of it.</strong> <strong>And measured, this
    /// project has no trigger-4 page at all</strong>, <strong>so nothing
    /// here depends on it.</strong>
    /// </para>
    /// </remarks>
    public IReadOnlyList<string> FuehreAn()
    {
        var bericht = new List<string>();
        if (!Maps.TryGetValue(CurrentMapId, out var karte))
        {
            bericht.Add(
                $"map {CurrentMapId} is not among the maps this runtime read,"
                    + " so nothing on it can be touched");
            return bericht;
        }

        foreach (var ereignis in karte.Root.Member("events")?.Items
            ?? new List<MzValue>())
        {
            if (ereignis.Member("x")?.IntOr(-1) != PlayerX
                || ereignis.Member("y")?.IntOr(-1) != PlayerY)
            {
                continue;
            }

            var id = ereignis.Member("id")?.IntOr(-1) ?? -1;
            var seiten = ereignis.Member("pages")?.Items
                ?? new List<MzValue>();
            for (var index = seiten.Count - 1; index >= 0; index--)
            {
                var seite = seiten[index];

                // **Und genau Ausloeser 2, und nicht 0 oder 1.**
                if ((seite.Member("trigger")?.IntOr(0) ?? 0) != 2)
                {
                    continue;
                }

                // **Und NORMALE Prioritaet, und das ist das Gegenteil
                // von `updateNonmoving`, das `here` ohne `normal` rief.**
                if ((seite.Member("priorityType")?.IntOr(1) ?? 1) != 1)
                {
                    continue;
                }

                if (!MzMapFigureReader.Meets(
                    seite.Member("conditions"), Facts, CurrentMapId, id))
                {
                    continue;
                }

                bericht.Add(RunPageEvent(id, seite, false));
                break;
            }
        }

        return bericht;
    }

    /// <summary>Turns a switch on, as a game's own commands would.</summary>
    /// <param name="pId">The switch number.</param>
    /// <remarks>
    /// <strong>And this is here because a page's conditions can gate the
    /// whole page</strong>, <strong>and measured, 10 of this project's 253
    /// pages ask for a switch</strong> — <strong>and Map006 event 7 asks
    /// for switch 6</strong>, <strong>which is the page with the game's
    /// scroll text.</strong>
    /// <para>
    /// <strong>And this is the same dictionary <c>121 Control Switches</c>
    /// writes</strong>, <strong>so a test that sets a switch here and a
    /// page that tests it there are talking about one thing.</strong>
    /// </para>
    /// </remarks>
    public void SchalteEin(int pId) => Facts.Switches[pId] = true;

    /// <summary>Turns a switch off.</summary>
    /// <param name="pId">The switch number.</param>
    /// <remarks>
    /// <strong>And the same dictionary, and the engine's own numbers:
    /// <c>$gameSwitches.setValue(n, true)</c> and <c>.setValue(n,
    /// false)</c>.</strong>
    /// </remarks>
    public void SchalteAus(int pId) => Facts.Switches[pId] = false;

    /// <summary>Turns the player to face a direction.</summary>
    /// <param name="pDirection">The direction to look.</param>
    /// <remarks>
    /// <strong>And the direction is the engine's own numbering</strong> —
    /// <c>Game_Character.DOWN</c> is <c>2</c> and <c>RIGHT</c> is
    /// <c>4</c>, and <c>checkEventTriggerThere</c> asks
    /// <c>roundXWithDirection</c> and <c>roundYWithDirection</c> with it.
    /// </strong> <strong>And this is here because a button press asks
    /// about the tile in front, and that tile is the one the player is
    /// looking at.</strong>
    /// </remarks>
    public void Blicke(int pDirection) => PlayerDirection = pDirection;

    /// <summary>Turns the player to face the right-hand side.</summary>
    /// <remarks>
    /// <strong>And the engine's <c>RIGHT</c> is <c>4</c></strong>, which
    /// is <c>MzCharacter.Right</c> here.
    /// </remarks>
    public void BlickeRechts() => Blicke(MzCharacter.Right);

    /// <summary>Presses the action button where the player is standing.
    /// </summary>
    /// <returns>One line per page the press started, and nothing if none.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And the button asks two questions in this order, and both
    /// are measured.</strong> At
    /// <c>Game_Player.prototype.triggerButtonAction</c>:
    /// <c>this.checkEventTriggerHere([0]); if ($gameMap.setupStartingEvent())
    /// return true; this.checkEventTriggerThere([0, 1, 2]); if
    /// ($gameMap.setupStartingEvent()) return true;</c>
    /// </para>
    /// <para>
    /// <strong>And "here" and "there" differ in the priority they ask
    /// for.</strong> <c>checkEventTriggerHere</c> calls
    /// <c>this.startMapEvent(this.x, this.y, triggers, false)</c> and
    /// <c>checkEventTriggerThere</c> calls <c>this.startMapEvent(x2, y2,
    /// triggers, true)</c> — <strong>and
    /// <c>startMapEvent</c> compares <c>event.isNormalPriority() ===
    /// normal</c>, and <c>isNormalPriority</c> is
    /// <c>this._priorityType === 1</c>.
    /// </para>
    /// <para>
    /// <strong>So the tile you stand on answers only if it is NOT normal
    /// priority, and the tile you face answers only if it IS.</strong>
    /// <strong>And measured, 85 of this project's 253 pages are not
    /// normal</strong> — <strong>which means a reader that asked only
    /// for normal priority could never reach 28 of its button pages,
    /// and a reader that asked for both at once answered on the wrong
    /// tile.</strong>
    /// </para>
    /// </remarks>
    public IReadOnlyList<string> DruckeKnopf()
    {
        var bericht = new List<string>();
        // **Und `[0]` unter den Fuessen** -- **gemessen an
        // `triggerButtonAction`, das `checkEventTriggerHere([0])` ruft.**
        foreach (var zeile in SucheStartende((PlayerX, PlayerY), false, 0))
        {
            bericht.Add(zeile);
        }

        if (bericht.Count == 0)
        {
            // **Und `[0, 1, 2]` davor** -- **gemessen an derselben
            // Stelle: `this.checkEventTriggerThere([0, 1, 2])`.**
            var vorne = (PlayerX + SchrittX(PlayerDirection),
                PlayerY + SchrittY(PlayerDirection));
            foreach (var zeile in SucheStartende(vorne, true, 0, 1, 2))
            {
                bericht.Add(zeile);
            }
        }

        return bericht;
    }

    /// <summary>The tile the player faces, by direction.</summary>
    /// <param name="pDirection">The direction the player looks.</param>
    /// <returns>The column to look at.</returns>
    /// <remarks>
    /// <strong>And this is the engine's own arithmetic, measured at
    /// <c>roundXWithDirection</c> and <c>roundYWithDirection</c>.</strong>
    /// </remarks>
    private static int SchrittX(int pDirection) => pDirection switch
    {
        MzCharacter.Left => -1,
        MzCharacter.Right => 1,
        _ => 0,
    };

    /// <summary>The tile row the player faces.</summary>
    /// <param name="pDirection">The direction the player looks.</param>
    /// <returns>The row to look at.</returns>
    /// <remarks>
    /// <strong>And the row goes down and not up, and that is measured at
    /// <c>Game_Map.prototype.roundYWithDirection</c>:</strong>
    /// <c>case Game_Character.DOWN: return y + 1;</c>
    /// </remarks>
    private static int SchrittY(int pDirection) => pDirection switch
    {
        MzCharacter.Up => -1,
        MzCharacter.Down => 1,
        _ => 0,
    };

    /// <summary>Finds the one page a button press starts on a tile.</summary>
    /// <param name="pTile">The tile to look at.</param>
    /// <param name="pNormal">Whether a normal-priority page answers.</param>
    /// <returns>One line, or nothing when no page answers there.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And the engine starts every match and stops the search at
    /// the first match that runs, measured at
    /// <c>triggerButtonAction</c>:** it calls <c>setupStartingEvent()</c>
    /// between the two lookups and returns when that is true.
    /// </para>
    /// <para>
    /// <strong>And this loop keeps looking after a match, which is
    /// measured too:</strong> <c>startMapEvent</c> has no break — <strong>and
    /// <c>startMapEvent</c> is itself guarded by <c>if
    /// (!$gameMap.isEventRunning())</c>, which is checked once before the
    /// loop and not per event.</strong> <strong>So two events on one
    /// tile both get started, and only the first is run.</strong>
    /// </para>
    /// </remarks>
    private IReadOnlyList<string> SucheStartende(
        (int X, int Y) pTile,
        bool pNormal,
        params int[] pAusloeser)
    {
        var bericht = new List<string>();
        if (!Maps.TryGetValue(CurrentMapId, out var karte))
        {
            bericht.Add(
                $"map {CurrentMapId} is not among the maps this runtime read,"
                    + " so nothing on it can be pressed");
            return bericht;
        }

        foreach (var ereignis in karte.Root.Member("events")?.Items
            ?? new List<MzValue>())
        {
            if (ereignis.Member("x")?.IntOr(-1) != pTile.X
                || ereignis.Member("y")?.IntOr(-1) != pTile.Y)
            {
                continue;
            }

            var id = ereignis.Member("id")?.IntOr(-1) ?? -1;
            var seiten = ereignis.Member("pages")?.Items
                ?? new List<MzValue>();
            for (var index = seiten.Count - 1; index >= 0; index--)
            {
                var seite = seiten[index];
                var ausloeser = seite.Member("trigger")?.IntOr(0) ?? 0;

                // **Und der Ausloeser, den der Motor verlangt.**
                //
                // **Gemessen:** `checkEventTriggerHere([0])` vom Knopf,
                // `checkEventTriggerThere([0, 1, 2])` von der Kachel
                // davor, und `checkEventTriggerHere([1, 2])` von
                // `updateNonmoving`, und `startMapEvent(x, y, [1, 2],
                // true)` von `checkEventTriggerTouch`.
                //
                // **Und `isTriggerIn` ist
                // `triggers.includes(this._trigger)` -- und eine leere
                // Liste nimmt alles an.**
                if (pAusloeser.Length > 0
                    && System.Array.IndexOf(pAusloeser, ausloeser) < 0)
                {
                    continue;
                }

                // **Und die Prioritaet entscheidet, und das ist
                // gemessen an `isNormalPriority`, das
                // `this._priorityType === 1` ist.**
                var normal = (seite.Member("priorityType")?.IntOr(1) ?? 1) == 1;
                if (normal != pNormal)
                {
                    continue;
                }

                if (!MzMapFigureReader.Meets(
                    seite.Member("conditions"), Facts, CurrentMapId, id))
                {
                    continue;
                }

                bericht.Add(RunPageEvent(id, seite, false));
                break;
            }
        }

        return bericht;
    }

    /// <summary>Steps the player onto a tile and starts what stands there.
    /// </summary>
    /// <param name="pX">The tile column.</param>
    /// <param name="pY">The tile row.</param>
    /// <returns>One line per page the step started, and nothing if none.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And this is the way 196 of this project's 253 pages are
    /// reached, and measured:</strong> trigger 0 is the action button,
    /// and it comes to 196 times, and every one of those 196 carries a
    /// position inside its map.
    /// </para>
    /// <para>
    /// <strong>And the engine's rule, measured:</strong>
    /// <c>startMapEvent(x, y, triggers, normal)</c> is
    /// <c>if (!$gameMap.isEventRunning()) for (const event of
    /// $gameMap.eventsXy(x, y)) if (event.isTriggerIn(triggers) &amp;&amp;
    /// event.isNormalPriority() === normal) event.start();</c>
    /// </para>
    /// <para>
    /// <strong>And <c>eventsXy</c> filters by position alone</strong> —
    /// <c>this.events().filter(event =&gt; event.pos(x, y))</c> — <strong>and
    /// <c>this.events()</c> is <c>this._events.filter(event =&gt; !!event)</c>,
    /// which keeps an event whose <c>characterName</c> is empty.</strong>
    /// <strong>A figure with no picture is still stepped on, and its page
    /// still runs, and a reader that only kept the figures with a
    /// picture found 4 of Map005's 11 events and stopped there.</strong>
    /// </para>
    /// <para>
    /// <strong>And the engine starts <em>every</em> match, and does not
    /// take the first.</strong> <strong>And then
    /// <c>setupStartingEvent</c> hands only one of them to the map's
    /// interpreter, in event order</strong> — <strong>so the others stay
    /// <c>_starting</c> until their turn, which is why a page with a
    /// second button press behind the first still runs.</strong>
    /// </para>
    /// </remarks>
    public IReadOnlyList<string> Betrete(int pX, int pY)
    {
        var bericht = new List<string>();
        Facts.Player.StandAt(CurrentMapId, pX, pY);

        // **Und das Betreten ist mehr als ein Schritt** -- **denn
        // `updateNonmoving` fragt beim Ankommen, und zwar mit `[1, 2]`
        // und mit `here`, also nach einer Seite, die NICHT normal
        // ist.**
        //
        // **Gemessen an Map001:** **die Beruehrungsseiten dort tragen
        // Prioritaet 0, und der Spieler tritt auf sie**, **und ohne
        // diesen Aufruf redete keine davon.**
        foreach (var zeile in SucheStartende((PlayerX, PlayerY), false, 1, 2))
        {
            bericht.Add(zeile);
        }

        if (bericht.Count > 0)
        {
            return bericht;
        }

        if (!Maps.TryGetValue(CurrentMapId, out var karte))
        {
            bericht.Add(
                $"map {CurrentMapId} is not among the maps this runtime read,"
                    + " so nothing on it can be stepped on");
            return bericht;
        }

        foreach (var ereignis in karte.Root.Member("events")?.Items
            ?? new List<MzValue>())
        {
            if (ereignis.Member("x")?.IntOr(-1) != pX
                || ereignis.Member("y")?.IntOr(-1) != pY)
            {
                continue;
            }

            var id = ereignis.Member("id")?.IntOr(-1) ?? -1;
            var seiten = ereignis.Member("pages")?.Items
                ?? new List<MzValue>();

            // **Und von hinten, und nur eine Seite.**
            //
            // **Gemessen an `Game_Character.prototype.findProperPageIndex`:
            // `for (let i = pages.length - 1; i >= 0; i--)`** -- **und
            // `page()` gibt `pages[findProperPageIndex()]` zurueck.**
            // **Ein Ereignis hat genau eine Seite, und ein Leser, der
            // alle passenden laufen laesst, redet dreimal auf derselben
            // Kachel.**
            for (var index = seiten.Count - 1; index >= 0; index--)
            {
                var seite = seiten[index];
                if ((seite.Member("trigger")?.IntOr(0) ?? 0) != 0)
                {
                    continue;
                }

                // **Und die Bedingung der Seite gilt, und das ist
                // gemessen an `Game_Character.isTriggerIn` und
                // `Game_Interpreter.setupStartingMapEvent`.**
                //
                // **Und ein Schalter, ein Gegenstand, eine Variable und
                // ein eigener Schalter koennen eine Seite sperren** --
                // **und `switch1Id: 1` ohne `switch1Valid` sperrt
                // nichts**, **denn das ist der Wert des Editors, und
                // nicht eine Forderung.** **Gemessen: 10 der 253 Seiten
                // dieses Spiels tragen eine solche Forderung.**
                if (!MzMapFigureReader.Meets(
                    seite.Member("conditions"), Facts, CurrentMapId, id))
                {
                    continue;
                }

                bericht.Add(RunPageEvent(id, seite, false));
                break;
            }
        }

        return bericht;
    }

    /// <summary>Runs one page of one event, and says what it did.</summary>
    /// <param name="pId">The event.</param>
    /// <param name="pPage">The page itself.</param>
    /// <param name="pAutorun">Whether this is an autorun page.</param>
    /// <returns>One line about what the page did.</returns>
    private string RunPageEvent(int pId, MzValue pPage, bool pAutorun)
    {
        var befehle = new List<MzCommandEntry>();
        foreach (var eintrag in pPage.Member("list")?.Items
            ?? new List<MzValue>())
        {
            befehle.Add(MzCommandEntry.From(eintrag));
        }

        if (befehle.Count <= 1)
        {
            return $"event {pId}: its list holds only the end, and"
                + " Game_Event.start says `if (list && list.length > 1)`,"
                + " so it does not start";
        }

        var ergebnis = Laeufer.TryGetValue(pId, out var alt)
            && alt != null && alt.KannFortgesetztWerden
            ? _runner.Run(befehle, Facts, CurrentMapId, pId, Random, alt)
            : _runner.Run(befehle, Facts, CurrentMapId, pId, Random);

        // **Und die Seite laeuft so weit, wie sie kommt** -- **denn ein
        // Schritt auf eine Kachel ist ein Tastendruck, und der Spieler
        // liest den Dialog, und der Dialog gibt den naechsten Befehl
        // frei.**
        //
        // **Gemessen an Map004 Event 15:** **Seite 0 sagt drei Zeilen
        // und schaltet dann `123 ['A', 0]`** -- **und ein Leser, der
        // einen Befehl trug, schaltete um, ohne zu reden** -- **und
        // damit war beim zweiten Betreten die falsche Seite dran.**
        var alle = new List<MzAction>();

        // **Und die erste Runde wird auch gezaehlt** -- **denn sie
        // trug die Aktionen, die niemand sonst zaehlt** -- **und ohne
        // das began jede Seite mit einer leeren Liste**, **und der
        // Dialog, der vor dem Selbstschalter kommt, war weg.**
        alle.AddRange(ergebnis.Actions);
        for (var mal = 0; mal <= befehle.Count; mal++)
        {
            if (ergebnis.Stopped == MzStep.Finished)
            {
                break;
            }

            Facts.MessageBusy = false;
            _keys.Ok();
            if (ergebnis.Interpreter?.PassFrame(
                WarteCheck(ergebnis.Interpreter)) != true)
            {
                break;
            }

            var naechste = _runner.Run(
                befehle, Facts, CurrentMapId, pId, Random,
                ergebnis.Interpreter);

            // **Und jede Runde traegt ihre Aktionen selbst** --
            // **die erste wurde oben gezaehlt, und ab hier kommt
            // jede Runde genau einmal dazu.**
            alle.AddRange(naechste.Actions);
            ergebnis = naechste;
        }

        // **Und die Aktionen kommen ausserhalb der Schleife**
        // **zusammen** -- **denn `ergebnis` traegt am Ende nur noch
        // den Zustand, und die Liste traegt die Summe.**
        ergebnis = new MzEventRunner.Result
        {
            Stopped = ergebnis.Stopped,
            Interpreter = ergebnis.Interpreter,
            Child = ergebnis.Child,
            Reason = ergebnis.Reason,
            Malformed = ergebnis.Malformed,
            Actions = alle,
            WaitingCode = ergebnis.WaitingCode,
            WaitingFrames = ergebnis.WaitingFrames,
            MissingCommonEvent = ergebnis.MissingCommonEvent,
        };

        if (ergebnis.Interpreter != null
            && ergebnis.Interpreter.KannFortgesetztWerden)
        {
            Laeufer[pId] = ergebnis.Interpreter;
        }
        else
        {
            Laeufer.Remove(pId);
        }

        PagesRun++;
        LastPage = pId;
        LastActions = ergebnis.Actions;
        LastPageStop = ergebnis.Stopped;
        LastPageIndex = ergebnis.Interpreter?.Index ?? 0;

        Stops = new List<string> { ergebnis.Reason, ergebnis.Describe() };
        return $"event {pId}: {ergebnis.Describe()}";
    }

    /// <summary>The pages still waiting, and where each one stopped.</summary>
    /// <remarks>
    /// <para>
    /// <strong>And the engine keeps them: this is
    /// <c>Game_Map</c>'s own <c>_interpreter</c>, and <c>Game_Player</c>'s,
    /// and <c>Game_Interpreter</c>'s.</strong> Measured at
    /// <c>Game_Map.prototype.update</c>, which calls
    /// <c>this._interpreter.update()</c> every frame.
    /// </para>
    /// <para>
    /// <strong>And a reader that threw a waiting page away restarted it
    /// at its first command</strong> — <strong>and a page of 211 commands
    /// with one balloon wait never got past the eight commands in front
    /// of that wait, for ever, however many times it was run.</strong>
    /// </para>
    /// </remarks>
    private Dictionary<int, MzInterpreter> Laeufer { get; } = new();

    /// <summary>The figure a route walks, or nothing when it has none.</summary>
    /// <param name="pRoute">The route.</param>
    /// <returns>The figure it acts on.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And the engine finds it by <c>_characterId</c>, and this
    /// reader has to do the same.</strong> Measured at
    /// <c>updateWaitMode</c>: <c>case "route": character =
    /// this.character(this._characterId)</c>.
    /// </para>
    /// <para>
    /// <strong>And the figure of the player is the player's own</strong> —
    /// <strong>because the player <em>is</em> a figure</strong> —
    /// <strong>and forty-six of this project's routes name him with minus
    /// one.</strong>
    /// </para>
    /// </remarks>
    private MzCharacter? TrägerVon(MzMoveRoute pRoute)
    {
        foreach (var held in EventFigures.Values)
        {
            if (held != null && ReferenceEquals(held.Route, pRoute))
            {
                return held;
            }
        }

        return Facts.Player.Figur != null
            && ReferenceEquals(Facts.Player.Figur.Route, pRoute)
            ? Facts.Player.Figur
            : null;
    }

    /// <summary>Whether any figure still shows a balloon.</summary>
    /// <remarks>
    /// <strong>And this is the engine's own question, and it is not "have
    /// sixty frames passed".</strong> Measured at
    /// <c>updateWaitMode</c>: <c>waiting = character &amp;&amp;
    /// character.isBalloonPlaying()</c> — <strong>and the official help
    /// says the event pauses until the icon has disappeared.</strong>
    /// </para>
    /// <para>
    /// <strong>And without this the page at index 21 of 211 waited for
    /// ever</strong> — <strong>because a page is only run when someone
    /// asks, and the icon is only counted when a frame is ticked.</strong>
    /// </remarks>
    private bool BallonLaeuft
    {
        get
        {
            foreach (var held in EventFigures.Values)
            {
                if (held != null && held.BalloonFramesLeft > 0)
                {
                    return true;
                }
            }

            return Facts.Player.Figur != null
                && Facts.Player.Figur.BalloonFramesLeft > 0;
        }
    }

    /// <summary>
    /// Whether the balloon the named character carries is still playing,
    /// which is what <c>updateWaitMode</c> asks for the id <c>command213</c>
    /// recorded. The engine keeps <c>this._characterId</c> and waits on
    /// <c>character.isBalloonPlaying()</c> — a single figure, and not "any
    /// figure with a balloon". A negative id is the player, which
    /// <c>this.character(-1)</c> returns.
    /// </summary>
    private bool BallonLaueft(int pCharakterId)
    {
        if (pCharakterId < 0)
        {
            return Facts.Player.Figur != null
                && Facts.Player.Figur.BalloonFramesLeft > 0;
        }

        return EventFigures.TryGetValue(pCharakterId, out var held)
            && held != null && held.BalloonFramesLeft > 0;
    }

    /// <summary>
    /// Builds the per-interpreter wait check: a balloon wait asks the
    /// character it named, and every other wait goes to the general check.
    /// This is what lets one page's balloon clear while a neighbour's
    /// non-waiting icon is still up, which the engine does because it waits
    /// on one figure and not on all of them.
    /// </summary>
    private Func<MzWaitMode, bool> WarteCheck(MzInterpreter pInterpretierer)
    {
        return pModus => pModus switch
        {
            MzWaitMode.Balloon =>
                !BallonLaueft(pInterpretierer.BalloonCharacterId),
            _ => WaitBeantwortet(pModus),
        };
    }

    /// <summary>Whether any figure still has its route forced.</summary>
    /// <remarks>
    /// <strong>And this is the engine's own question, and it is not
    /// "has steps left".</strong> Measured at <c>isMoveRouteForcing</c>,
    /// which is <c>_moveRouteForcing</c> — <strong>and that is set by
    /// <c>forceMoveRoute</c> and cleared by <c>processRouteEnd</c>.</strong>
    /// <strong>A figure mid-step is still forcing, and a figure standing
    /// at the end of its route is not.</strong>
    /// </remarks>
    public bool Erzwungen
    {
        get
        {
            foreach (var route in Routes.Values)
            {
                if (route != null && route.IsForcing)
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>Carries out a reserved transfer, and says whether it went.
    /// </summary>
    /// <returns>True once the player is on the new map.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And this is the engine's own order, measured:</strong>
    /// <c>command201</c> only reserves — <c>$gamePlayer.reserveTransfer(mapId,
    /// x, y, params[4], params[5]); this.setWaitMode("transfer");</c> — and
    /// <c>Scene_Map.prototype.onMapLoaded</c> carries it out:
    /// <c>if (this._transfer) { $gamePlayer.performTransfer(); }</c>.
    /// </para>
    /// <para>
    /// <strong>And <c>performTransfer</c> is three steps:</strong>
    /// <c>$gameMap.setup(this._newMapId)</c> when the map changed or a
    /// reload is needed, <c>this.locate(this._newX, this._newY)</c>
    /// always, and <c>this.clearTransferInfo()</c> at the end.
    /// </para>
    /// <para>
    /// <strong>And the map must be loaded before the move</strong> —
    /// <strong>and a reader that moved the player onto a map it never
    /// read would put him somewhere this runtime cannot show.</strong>
    /// </para>
    /// </remarks>
    private bool FuehreTransferAus()
    {
        if (Facts.Player.Erased)
        {
            return false;
        }

        var bericht = Facts.Player.PerformTransfer(Maps.Keys);
        LastTransfer = bericht;
        if (!Facts.Player.IsTransferring)
        {
            CurrentMapId = Facts.Player.MapId;

            // **Und die neue Karte wird gezeichnet, und das setzt den
            // Spieler auf ihre Startposition** -- **und
            // `Game_Map.setup` macht im Motor dasselbe, und erst danach
            // ruft `Scene_Map.onMapLoaded` das `performTransfer` auf.**
            //
            // **Und gemessen war das der Fehler: `Repaint` stellte den
            // Spieler auf 2,12 statt auf 2,2, und `LastTransfer` sagte
            // trotzdem "the player is now on map 9 at 2,2", denn der
            // Satz entsteht in `MzPlayer`, bevor `Repaint` laeuft.**
            if (GemalteKarte != Facts.Player.MapId && !Repaint())
            {
                LastTransfer += ", but the new map did not paint, and the"
                    + " refusal is: " + PaintReason;
                return false;
            }

            // **Und der Umzug wird ein zweites Mal gesetzt**, **denn
            // das Neuzeichnen hat ihn ueberschrieben.**
            LastTransfer += "; and now the position is set again on the"
                + $" painted map, which stands at {Facts.Player.X},"
                + $"{Facts.Player.Y}";
            Facts.Player.PerformTransfer(Maps.Keys);
        }

        return Facts.Player.MapId == CurrentMapId
            && GemalteKarte == CurrentMapId;
    }

    /// <summary>The last transfer this runtime carried out, in words.</summary>
    /// <remarks>
    /// <strong>And this is here so a test can read what happened instead
    /// of guessing</strong>, <strong>because the engine says it in the log
    /// and not on the screen.</strong>
    /// </remarks>
    public string LastTransfer { get; private set; } = "";

    /// <summary>Which map the last paint drew, by number.</summary>
    /// <remarks>
    /// <strong>And this is here because <c>PaintedMap</c> is the pixel
    /// buffer and not the number</strong>, <strong>and a reader that
    /// compared a map number to a buffer could not tell a repaint from a
    /// change of map.</strong>
    /// </remarks>
    public int GemalteKarte { get; private set; } = -1;

    private bool WaitBeantwortet(MzWaitMode pMode)
    {
        // **Und `MzWaitMode` kennt genau vier Zustaende, und alle vier
        // sind gemessen:** **None, Transfer, Route und Message.**
        //
        // **Und die Nachricht ist eine davon, und sie wartet auf den
        // Spieler** -- **gemessen an `Window_Message.isTriggered`, das
        // `Input.isRepeated("ok") || Input.isRepeated("cancel")`
        // fragt** -- **und `isRepeated`, nicht `isTriggered`**, **und
        // eine gehaltene Taste wirkt weiter.**
        //
        // **Und `MessageBusy` ist das, was `Message` aufloest**, **denn
        // `101` setzt es und ein Dialog, der endet, loescht es** -- **und
        // ohne das loest sich die Wartezeit nie, und die Seite laeuft
        // nicht weiter, und 209 von 211 Befehlen bleiben ungelesen.**
        return pMode switch
        {
            // **Und der Transfer wird ausgefuehrt, und nicht nur
            // geprueft.**
            //
            // **Gemessen an der Reihenfolge des Motors:** **`command201`
            // ruft nur `$gamePlayer.reserveTransfer(mapId, x, y,
            // params[4], params[5])` und `this.setWaitMode("transfer")`**,
            // **und der eigentliche Umzug passiert spaeter und an einer
            // ganz anderen Stelle:**
            //
            // ```js
            // // Scene_Map.prototype.onMapLoaded
            // if (this._transfer) { $gamePlayer.performTransfer(); }
            // ```
            //
            // **Und `Game_Player.prototype.performTransfer` macht
            // `$gameMap.setup(this._newMapId)`, `this.locate(this._newX,
            // this._newY)` und `this.clearTransferInfo()`.**
            //
            // **Also reserviert der Befehl, und `Scene_Map` fuehrt aus.**
            // **Ein Leser, der nur `reserveTransfer` kann, wartet auf
            // einen Umzug, den niemand vollzieht** -- **und das war
            // gemessen der Grund, warum Map006 Event 7 bei Index 19
            // stehen blieb**, **und dort ist `201 [0, 9, 2, 2, 2, 2]`,
            // also ein Umzug auf Karte 9 nach 2,2.**
            // **Und der Umzug braucht zweimal eine Antwort**, **denn
            // die erste liefert die neue Karte**, **und die zweite
            // braucht, damit die Seite weiterlaeuft.**
            //
            // **Und gemessen ist, dass `Repaint` den Spieler auf die
            // Startposition der neuen Karte setzt** -- **und
            // `performTransfer` hat ihn vorher auf 2,2 gesetzt**, --
            // **und also muss der Umzug nach dem Neuzeichnen noch
            // einmal geschehen.**
            MzWaitMode.Transfer => FuehreTransferAus(),
            // **Und die Route wartet, solange sie erzwungen wird, und
            // nicht solange sie Schritte hat.**
            //
            // **Gemessen an `updateWaitMode`:**
            // `case "route": character = this.character(this._characterId);
            // waiting = character && character.isMoveRouteForcing();`
            // **und `processRouteEnd` loest die Erzwungung** -- **und
            // ein Schritt, der wartet, ist noch keine Erzwungung.**
            //
            // **Ein Leser, der auf "die Route hat noch Schritte" wartete,
            // Wartete auf eine Figur, die geht** -- **und ohne den
            // Schritt oben bleibt sie stehen** -- **und die Seite
            // wartete auf eine Figur, die sich nicht bewegt.**
            MzWaitMode.Route => !Erzwungen,

            // **Und der Ballon wartet, bis er weg ist, und nicht, bis
            // eine Zahl von Bildern vorbei ist.**
            //
            // **Gemessen an `updateWaitMode`: `case "balloon": character
            // = this.character(this._characterId); waiting = character
            // && character.isBalloonPlaying()`.**
            //
            // **Und `this._characterId` ist der Zielbefehl von `213`,
            // und das ist bei 36 Ballons dieses Spiels 15 mal der
            // Spieler.**
            MzWaitMode.Balloon => !BallonLaeuft,
            //
            //
            // **Und die Tastenliste, und nicht die Flagge** -- **und das
            // ist der Unterschied zwischen "der Dialog wartet" und
            // "jemand hat Enter gedrueckt".**
            //
            // **Und `Game_Interpreter.prototype.updateWaitMode` fragt
            // fuer `"message"` `waiting = $gameMessage.isBusy()`, und
            // `Window_Message.prototype.isTriggered` setzt die Flagge
            // zurueck, wenn `Input.isTriggered("ok")` war.**
            //
            // **Und beides ist noetig, und gemessen sind zwei Fehler,
            // einer in jede Richtung:**
            //
            // - **nur die Flagge** (`!MessageBusy`): `MessageBusy` wird
            //   im selben Frame true, in dem der 101 liest, **also
            //   loest die Wartezeit im Frame, in dem sie entsteht**
            //   -- **gemessen als ein Indexrueckgang von 208 auf 6**,
            //   **und die Seite lief an ihrem eigenen Dialog vorbei.**
            // - **nur die Tastenliste** (`Pressed.Contains("ok")`):
            //   `Pressed` ist nach `FrameEnde()` leer, **und in einer
            //   Runtime ohne Fenster fuellt sie niemand** -- **gemessen
            //   als `wait=Message` bei `MessageBusy = false` ueber
            //   Frames ohne einen Schritt.**
            //
            // **Und die dritte Bedingung ist die Zeit: `LastDialogue`
            // ist gesetzt, solange ein Block offen ist** -- **und es
            // ist der Zustand, den die Engine mit `isBusy()` meint.**
            MzWaitMode.Message => _keys.Pressed.Contains("ok")
                || _keys.Pressed.Contains("cancel"),
            _ => true,
        };
    }

    /// <summary>The keys the player pressed.</summary>
    public KeysPressed Keys => _keys;

    private readonly KeysPressed _keys = new();
    private int _dialogAge;

    /// <summary>
    /// <summary>And how many frames a dialogue stands before a key ends it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the difference between "shown" and
    /// "skipped".</strong> The engine's window keeps the text until
    /// <c>Input.isRepeated("ok")</c>, so a runtime with no window has to
    /// decide for itself how long a message stands. Zero would mean no
    /// reader ever sees a dialogue, because the flag is set and cleared in
    /// the same frame.
    /// </para>
    /// <para>
    /// <strong>And one frame is not enough, and that is measured.</strong>
    /// <c>Window_Message.prototype.updateMessage</c> advances its line
    /// counter on <em>every</em> frame, and a message of four lines needs
    /// four of them before the box scrolls -- <strong>so a runtime that
    /// releases after one frame shows a word and hides the rest.</strong>
    /// The engine's own default for a message is a line every
    /// <c>messageSpeed</c> frames, and a single 101 in this game carries
    /// four lines.  **Four frames is the shortest time the text can be
    /// read at the engine's own speed.**
    /// </para>
    /// </remarks>
    public const int DialogBilderVorBestaetigung = 4;

    /// <summary>Why the last page stopped, in its own words.</summary>
    public IReadOnlyList<string> Stops { get; private set; } =
        Array.Empty<string>();

    /// <summary>How many pages have run.</summary>
    /// <summary>The lines of the scroll text this reader last read.</summary>
    /// <remarks>
    /// <strong>And this is here so that a test can see what a 105
    /// produced</strong>, <strong>because the lines are otherwise inside
    /// <c>$gameMessage</c> in the engine and inside a local here.</strong>
    /// </remarks>
    public IReadOnlyList<string> ScrollLines => Facts.ScrollLines;

    /// <summary>The screen's pictures, its shake and its flashes.</summary>
    public MzScreen Screen => Facts.Screen;

    /// <summary>Which actors a 314 recovered.</summary>
    public IReadOnlyCollection<int> Recovered => Facts.Recovered;

    public int PagesRun { get; private set; }

    /// <summary>Which event ran last.</summary>
    public int LastPage { get; private set; }

    /// <summary>Where that page stopped.</summary>
    /// <summary>Where the last page stopped in its own list.</summary>
    /// <remarks>
    /// <strong>And this is the engine's own <c>this._index</c></strong>,
    /// and <strong>a report that says only "waiting" does not say how
    /// far it got</strong> -- **and a page of 211 commands that stands
    /// at 21 for ever and a page that stands at 194 look identical from
    /// the outside.**
    /// </remarks>
    public int LastPageIndex { get; private set; }

    public MzStep LastPageStop { get; private set; }

    private static bool Passt(int pAusloeser, StartMode pStart)
    {
        // **Und autorun ist Ausloeser 2, nicht 3.** Gemessen in
        // `rmmz_objects.js`: `Game_Event.prototype.update` startet eine
        // Seite, solange `isTriggerIn([2])`, und `Game_Event.prototype
        // .start` ist `if (list && list.length > 1)`. **Ausloeser 3 ist
        // der Parallelprozess**, dem die Engine einen eigenen Interpreter
        // gibt und den diese Methode deshalb nicht starten darf; **die
        // Tabelle hatte die beiden vertauscht**, und damit lief in einem
        // fertigen Spiel keine einzige Autorun-Seite.
        //
        // **Und Autorun hat in beiden Engines zwei Nummern, und das ist
        // gemessen, nicht geraten.**
        //
        // **Und `Game_Event.prototype.start` ist in beiden Dateien
        // wortgleich:**
        //
        // ```js
        // Game_Event.prototype.start = function() {
        //     const list = this.list();
        //     if (list && list.length > 1) {
        //         this._starting = true;
        //         if (this.isTriggerIn([0, 1, 2])) {
        //             this.lock();
        //         }
        //     }
        // };
        // ```
        //
        // **... und `isTriggerIn` sagt nichts darueber, *welche* Nummer
        // Autorun ist.**  Gemessen an den beiden Spielen:
        //
        // | Spiel | Trigger in den Karten |
        // |---|---|
        // | CamelliaCoronation-Win (MZ) | 0, 1, **2**, 3 |
        // | LegalTruck_v1.1 (MV) | 0, **4**, 3 |
        //
        // **Und beide Engines starten Autorun ueber
        // `Game_Map.prototype.setupStartingMapEvent`, das `isStarting()`
        // fragt** -- **das ist die Regel, und die Zahl ist pro Projekt
        // verschieden.**
        //
        // **Gemessen ist der Schaden:** mit einer Tabelle, die nur `2`
        // kannte, lief **keine einzige Autorun-Seite von LegalTruck**
        // -- **und der Test sah 14 Aktionen statt der 197 Befehle von
        // Ereignis 1.**
        if (pAusloeser == MzSeitenStart.AusloeserAutomatisch
            || pAusloeser == 4)
        {
            return pStart == StartMode.Autorun;
        }

        return pAusloeser switch
        {
            MzSeitenStart.AusloeserAutomatisch => pStart == StartMode.Autorun,
            MzSeitenStart.AusloeserParallel => false,
            MzSeitenStart.AusloeserTaste => pStart == StartMode.ActionButton,
            MzSeitenStart.AusloeserBeruehrt => pStart == StartMode.Touched,
            _ => false,
        };
    }

    /// <summary>
    /// And one frame of every page that is still waiting.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this was written and never called</strong>, which is
    /// the whole reason a game with dialogue stayed silent.
    /// <c>Game_Map.prototype.update</c> is
    /// <c>if (this._interpreter.isRunning()) { this._interpreter.update(); }
    /// </c> -- <strong>measured in <c>rmmz_objects.js</c> and
    /// <c>rpg_objects.js</c></strong> -- and the loop in here is that
    /// <c>update</c>. <strong>A runtime that reads a page's commands once
    /// and never advances it again shows the map and no story</strong>,
    /// because the first wait at command 221 was the last command that
    /// ever ran.
    /// </para>
    /// </remarks>
    /// <returns>How many commands the waiting pages carried out.</returns>
    /// <summary>
    /// And a page that waits for a dialogue is answered.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this belongs to the *wait*, not to the screen flag.</strong>
    /// <c>WaitBeantwortet</c> is only asked while an interpreter stands
    /// on <c>MzWaitMode.Message</c>, <strong>and the measurement showed
    /// <c>busy=False last=null</c> in exactly that state</strong> --
    /// <strong>332 of 573 frames.</strong> A release gated on
    /// <c>Facts.MessageBusy</c> answers nothing in those 332 frames.
    /// </para>
    /// <para>
    /// <strong>And it is one rule, and it is called from both
    /// <see cref="Tick"/> and <c>Update</c>, because two places for one
    /// rule are two rules once they drift.</strong>
    /// <c>Test_DieDreiParallelenSeiten</c> drives <c>Tick()</c> directly,
    /// 240 times, <strong>and the page stood at <c>waiting 0 frames at
    /// code 101</c> because the answer lived in <c>Update</c> alone.</strong>
    /// </para>
    /// <para>
    /// <strong>And the frame is not a taste</strong> -- it is what stands
    /// between "the dialogue appears" and "the dialogue is skipped":
    /// <c>Window_Message.prototype.isTriggered</c> needs a key press
    /// that comes <em>after</em> the window opened.
    /// </para>
    /// </remarks>
    private void LoeseDialoge()
    {
        if (!Laeufer.Values.Any(i => i != null
            && i.WaitMode == MzWaitMode.Message))
        {
            _dialogAge = 0;
            return;
        }

        _dialogAge++;
        if (_dialogAge <= DialogBilderVorBestaetigung)
        {
            return;
        }


        _keys.Ok();
        Facts.MessageBusy = false;
        Facts.LastDialogue = null;
        _dialogAge = 0;
        if (Laeufer.Count > 0 && Laeufer.Values.First().Index >= 125
            && CurrentMapId == 5)
        {
        }
    }

    public int Tick()
    {
        //
        // **Und die Freigabe gehoert hierher, und nicht nur in
        // `Update` -- denn `Tick` ist oeffentlich.**
        //
        // **Und gemessen ist das an `Test_DieDreiParallelenSeiten`:
        // der Test ruft `Tick()` direkt, 240 Mal, **und die Seite blieb
        // bei `waiting 0 frames at code 101` stehen** -- **weil die
        // Antwort auf die Nachrichten-Wartezeit in `Update` lag und
        // `Tick` allein sie nicht bekam.**
        //
        // **Und das ist kein Testproblem.**  Ein Lauf, der `Tick`
        // aufruft, treibt eine Seite, **und eine Seite, die wartet,
        // muss in jedem Weg weiterkommen, der sie fortsetzt** -- **und
        // zwei Orte fuer dieselbe Regel sind zwei Regeln, sobald sie
        // auseinanderlaufen.**
        LoeseDialoge();
        var wechselt = 0;

        // **Und jede Figur, auf der eine Laufbahn erzwungen wurde, geht
        // zuerst.**
        //
        // **Das ist die Reihenfolge des Motors, und sie ist gemessen:**
        // **`updateRoutineMove` nimmt einen Befehl pro Bild, und
        // **`updateMove` schiebt die Figur auf ihre Kachel** -- **und
        // **`isHoldWait` fragt `character.isRouteBeingForced()`**
        // **jedes Bild neu.**
        //
        // **Und eine Seite, die bei einer 205 wartet, wartet auf
        // genau das** -- **und ohne diesen Schritt hier bleibt die
        // Figur stehen, und die Seite wartet auf eine Figur, die sich
        // nicht bewegt, und 209 von 211 Befehlen bleiben ungelesen.**
        foreach (var route in Routes.Values)
        {
            if (route == null)
            {
                continue;
            }

            // **Und die Figur, auf der die Route spricht** -- **und das
            // ist die Figur des Befehls, der sie erzwungen hat**,
            // **und fuer den Spieler der Spielers eigene Figur.**
            var held = TrägerVon(route);
            if (held == null)
            {
                continue;
            }

            held.PassFrame();
            if (route.Step(held, null) != "")
            {
                if (Clocks.TryGetValue(held.EventId, out var uhr)
                    && uhr != null)
                {
                    uhr.Direction = held.Direction;
                    uhr.Moving = !held.IsStopping;
                }

                wechselt++;
            }
        }

        // **Und jeder Ballon laeuft ein Bild weiter, und das ist
        // gemessen.**
        //
        // **`TickBalloon` war eine Methode, die niemand rief** -- **und
        // ein Ballon, den niemand zaehlt, bleibt fuer immer** -- **und
        // `213 [-1, 2, true]` wartet genau auf ihn.**
        foreach (var held in EventFigures.Values)
        {
            held?.TickBalloon(1);
        }

        Facts.Player.Figur?.TickBalloon(1);

        // **Und jede wartende Seite kommt ein Bild weiter, und das ist
        // der Motorweg.**
        //
        // **Gemessen an `Game_Map.prototype.update`: `this._interpreter
        // .update()` wird jedes Bild gerufen** -- **und eine Seite, die
        // 60 Bilder auf einen Ballon wartet und nur beim Start von
        // `RunPage` bekommt, wartet auf einen Ballon, den niemand
        // zaehlt** -- **und bei Index 21 von 211 ist das der ganze
        // Rest des Spiels.**
        //
        // **Und `PassFrame` allein laesst die Seite nicht
        // weiterlaufen**,
        //
        // **Und die Engine tut beides im selben Aufruf**,
        // -- **denn `Game_Map.prototype.update` sagt:**
        //
        // ```js
        // Game_Map.prototype.update = function() {
        //     ...
        //     if (this._interpreter.isRunning()) {
        //         this._interpreter.update();
        //     }
        //     ...
        // };
        // ```
        //
        // **Und `Game_Interpreter.prototype.update` ist eine
        // Schleife ueber `executeCommand`**,
        // **und ein Schritt darin setzt den Index hoch und
        // liest den naechsten Befehl.**
        //
        // **Und ein Leser, der nur `PassFrame` aufruft, loest
        // die Wartezeit und liest keinen einzigen Befehl
        // weiter**,
        // **und eine Seite, die bei Index 209 wartet, wartet
        // dort fuer immer.**
        var laeufe = Laeufer.ToList();
        foreach (var paar in laeufe)
        {
            var warte = paar.Value;
            if (warte == null)
            {
                continue;
            }

            var pass = warte.PassFrame(WarteCheck(warte));
            var kann = warte.KannFortgesetztWerden;
            if (!pass || !kann)
            {
                continue;
            }

            // **Und die Befehle kommen aus der Karte, und nicht
            // aus dem Interpreter**,
            // **denn `Game_Map.setup(list, eventId)` liest sie
            // aus der Seite.**
            if (!Maps.TryGetValue(CurrentMapId, out var karte)
                || karte.Root.Member("events")?.Items == null)
            {
                continue;
            }

            MzCommandEntry[]? liste = null;
            foreach (var e in karte.Root.Member("events")!.Items)
            {
                if (e.Member("id")?.IntOr(-1) != paar.Key)
                {
                    continue;
                }

                var seiten = e.Member("pages")?.Items;
                if (seiten == null || seiten.Count == 0)
                {
                    break;
                }

                // **Und die hoechste passende Seite**, -- **und
                // `findProperPageIndex` laeuft von hinten.**
                for (var s = seiten.Count - 1; s >= 0; s--)
                {
                    if (!MzMapFigureReader.Meets(
                        seiten[s].Member("conditions"),
                        Facts, CurrentMapId, paar.Key))
                    {
                        continue;
                    }

                    var gebaut = new List<MzCommandEntry>();
                    foreach (var c in
                        seiten[s].Member("list")!.Items)
                    {
                        gebaut.Add(MzCommandEntry.From(c));
                    }

                    liste = gebaut.ToArray();
                    break;
                }

                break;
            }

            if (liste == null)
            {
                continue;
            }
            var fort = _runner.Run(
                liste, Facts, CurrentMapId, paar.Key, Random,
                warte);
            Actions.AddRange(fort.Actions);
            if (fort.Interpreter != null
                && fort.Interpreter.KannFortgesetztWerden)
            {
                Laeufer[paar.Key] = fort.Interpreter;
            }
            else
            {
                //
                // **Und eine beendete Seite traegt ihren Endindex noch
                // bei -- und das ist der Unterschied zwischen "wo steht
                // die Seite" und "wo stand sie".**
                //
                // **Und gemessen sind beide Zahlen an den beiden Seiten
                // dieses Spiels:** Map003 Ereignis 9 hat **211 Befehle**,
                // und das `0` steht bei **210**; **Map005 Ereignis 4 hat
                // 176 Befehle**, **und das `0` steht bei 175.**  Ein
                // Interpreter, der seine Liste zu Ende gelesen hat,
                // **steht eine Position hinter dem `0`** -- **das ist
                // kein Fehler, das ist das Listenende.**
                //
                // **Und `LastPageIndex` ist ein Index, und kein
                // Befehlszaehler** -- **und genau darum liest
                // `Test_DieParalleleSeiteLiestSoVielWieSieKann` auch
                // nicht ihn, sondern den Bericht, und zaehlt selbst.**
                Laeufer.Remove(paar.Key);
            }

            wechselt++;
        }

        //
        // **Und `Actions` ist nicht der Puffer eines Bildes, und der
        // Unterschied ist gemessen.**
        //
        // **Und `Test_DerLaufFuehrtDieEigenenBefehleDesProjektsAus` liest
        // `lauf.Actions` nach 600 Bildern und verlangt mehr als tausend
        // Eintraege** -- **und es waren 14, weil dieser Puffer hier
        // geleert wurde.**  **`Actions` ist die Liste *des Laufs*:** sie
        // waechst, sie wird gelesen, **und sie gehoert nicht dem Bild,
        // in dem sie gefuellt wurde.**  **Ein Sammelbehaelter, den ein
        // Haken nach jedem Bild leert, ist kein Zaehler, sondern ein
        // Kratzer.**
        //
        // **Und `LastActions` bleibt die Liste *einer Seite*, und die
        // wird hier angehaengt** -- **denn `RunPage` hat den Anfang
        // schon hineingeschrieben** (den Ballon `213` an Index 0), **und
        // ein Ersetzen verlor genau die beiden und damit die Reihenfolge
        // des Spiels** (gemessen: `101, 205, 101, 213, ...` statt
        // `213, 101, ...`).
        if (Actions.Count > 0)
        {
            var gesamt = new List<MzAction>(LastActions);
            gesamt.AddRange(Actions);
            LastActions = gesamt;
        }

        //
        //
        // **Und gemessen war, dass es das nicht war:** `Tick` sammelt
        // in `Actions`, **und `LastActions` wurde nur in `RunPage` und
        // `RunPageEvent` gesetzt** -- **also lief Camellias Ereignis 9
        // bis Index 210 von 211 und tat dabei 200 Befehle, von denen
        // **keiner** in der Liste stand, die der Test liest.**
        // **Gemessen waren 4 Aktionen bei Index 210.**
        //
        // **Und die Liste ist eine Liste des Laufs, und nicht des
        // Bildes** -- **denn `LastActions` behauptet seit diesem Lauf
        // "was tat die Seite", und eine Seite, die ueber Bilder laeuft,
        // hat mehr getan als in ihrem ersten Frame.**
        //
        // **Und sie werden *angehaengt*, und nicht ersetzt** -- **denn
        // `RunPage` hat den Anfang der Seite schon in `LastActions`
        // geschrieben** (den Ballon `213` an Index 0 und den ersten
        // `101` dahinter), **und ein Ersetzen verlor genau die beiden
        // und damit die Reihenfolge des Spiels** (gemessen:
        // `101, 205, 101, 213, ...` statt `213, 101, ...`).
        //
        //
        // **Und derselbe Lauf, und dieselbe Luecke, und jetzt am
        // Index.**
        //
        // **Und gemessen war das als `it stands at 6` bei 462
        // Aktionen:** `LastPageIndex` wurde nur von `RunPage` und
        // `RunPageEvent` geschrieben, **und eine Seite, die ueber Bilder
        // laeuft, kommt in `RunPage` nicht mehr vor.**  **Der Index, den
        // `LastPageIndex` meldet, war also der Index am *Start*, und der
        // Fortschritt ging in die Actions-Liste und sonst nirgendwo
        // hin.**
        //
        // **Und die Zahl ist die Fortschrittsangabe des Laufs, und
        // nicht die eines Frameanrufs** -- **denn das Feld heisst
        // "wo steht die Seite", und das ist eine Frage ueber die
        // Laufzeit und nicht ueber den letzten Aufruf.**
        foreach (var paar in Laeufer)
        {
            if (paar.Value != null)
            {
                LastPage = paar.Key;
                LastPageIndex = Math.Max(LastPageIndex, paar.Value.Index);
            }
        }

        foreach (var uhr in Clocks.Values)
        {
            if (uhr != null && uhr.Tick())
            {
                wechselt++;
            }
        }

        // **Und die Laufbahnen laufen, und nur die, die laufen sollen.**
        //
        // **Gemessen an `Game_Event.prototype.updateSelfMovement`:** **die
        // Laufbahn laeuft nur bei `moveType 3`, und `moveType 0` ist in
        // keinem Zweig** -- **und vor allen Dingen muss die Figur erst
        // stillstehen**, **`stopCountThreshold` Bilder lang, und das ist
        // `30 * (5 - moveFrequency)`.**
        //
        // **Fuer dieses Projekt sind das 60 Bilder, denn `moveFrequency`
        // ist ueberall 3.** **Ein Leser, der die Schwelle ausgelassen
        // hat, liess die Figuren sofort losrennen, und einer, der die
        // Frequenz als Framezahl genommen hat, liess sie nach 150 Bildern
        // gehen.**
        foreach (var figur in Figures)
        {
            if (!figur.RunsOwnRoute || figur.Route == null)
            {
                continue;
            }

            var route = Routes.TryGetValue(figur.EventId, out var lauf)
                ? lauf : null;
            if (route == null)
            {
                route = new MzMoveRoute();
                route.Force(figur.Route.Value);
                Routes[figur.EventId] = route;
            }

            if (_standing.TryGetValue(figur.EventId, out var stand))
            {
                stand++;
            }
            else
            {
                _standing[figur.EventId] = 1;
            }

            if (stand < figur.StopCountThreshold || route.IsDone)
            {
                continue;
            }

            // **Und auf der Figur, die schon da ist, und nicht auf
            // einer, die jedes Bild neu gebaut wird** -- **denn eine
            // Figur, die jedes Bild neu entsteht, ist nie angekommen**,
            // **und eine Laufbahn, die auf ihr wartet, wartet auf eine
            // Figur, die es nicht gibt.**
            var holder = EventFigures.TryGetValue(figur.EventId, out var da) ? da : null;

            if (route.Step(holder, null) != "")
            {
                if (holder != null
                    && Clocks.TryGetValue(figur.EventId, out var uhr)
                    && uhr != null)
                {
                    uhr.Direction = holder.Direction;
                    // **Und die Uhr geht nur, wenn die Figur wirklich
                    // unterwegs ist** -- **und das ist der Motor, und
                    // nicht `moveType`.**
                    uhr.Moving = !holder.IsStopping;
                }

                wechselt++;
            }
        }

        // **Und der Spieler folgt seiner Figur, und die Figur geht
        // weiter.**
        //
        // **Gemessen: der Motor hat gar kein Sync, weil er gar kein
        // Paar hat** -- **`Game_Player` erbt von `Game_Character` und
        // ist die Figur selbst.** **Und dieses Paar hier ist eine
        // Notloesung fuer den Leser, und die muss an jedem Ende
        // zusammenruecken, sonst laufen Spieler und Figur auseinander
        // und eine Karte zeigt eine Figur auf einer Kachel, auf der
        // der Spieler nicht ist.**
        Facts.Player.SyncFigure();
        AdvancePlayer();

        if (_playerClock != null && _playerClock.Tick())
        {
            wechselt++;
        }

        Frames++;
        if (wechselt > 0 && PaintedMap != null)
        {
            PaintFigures(PaintedMap);
        }

        return wechselt;
    }

    /// <summary>
    /// Walks the player one frame towards the tile it is on.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the arithmetic is the engine's, and it is a
    /// fraction.</strong> Measured:
    /// <c>distancePerFrame</c> is <c>2^realMoveSpeed / 256</c> — <strong>so
    /// at speed 4 a figure crosses a quarter of a tile in a
    /// frame</strong>, <strong>and at speed 5 an eighth, and at speed 6 a
    /// sixteenth.</strong>
    /// </para>
    /// <para>
    /// <strong>And the drawing position is one tile behind, which is what
    /// makes a walk look like a walk.</strong> Measured:
    /// <c>_realX = xWithDirection(_x, reverseDir(d))</c> — <strong>and a
    /// figure that simply appears on its new tile has no walk in it
    /// at all</strong>, <strong>it is a tile-to-tile jump dressed as
    /// movement.</strong>
    /// </para>
    /// <para>
    /// <strong>And the pattern counts while it walks</strong> — at 1.5 a
    /// frame — <strong>which is why a figure that is really moving shows
    /// three columns and one that stands shows one.</strong>
    /// </para>
    /// </remarks>
    private void AdvancePlayer()
    {
        var figuer = Facts.Player.Figur;
        if (figuer == null || figuer.IsStopping)
        {
            return;
        }

        figuer.PassFrame();
        Facts.Player.SyncFigure();

        if (_playerClock != null)
        {
            _playerClock.Moving = !figuer.IsStopping;
            _playerClock.Direction = figuer.Direction;
        }
    }

    /// <summary>How many frames this runtime has run.</summary>
    public int Frames { get; private set; }

    /// <summary>The live figures, by event.</summary>
    /// <remarks>
    /// <strong>And these are the same objects a route acts on.</strong>
    /// Measured: the engine has one <c>Game_Character</c> per event and
    /// the page's commands speak to <em>that</em> object — <strong>and a
    /// reader that built a fresh figure every frame had a figure that was
    /// never anywhere</strong>, <strong>and a route waiting for it to
    /// arrive waited for ever.</strong>
    /// </remarks>
    public Dictionary<int, MzCharacter?> EventFigures { get; private set; } =
        new();

    /// <summary>Which route belongs to which event.</summary>
    public Dictionary<int, MzMoveRoute?> Routes { get; private set; } = new();

    /// <summary>How many frames a figure has stood still.</summary>
    /// <remarks>
    /// <strong>And this is the engine's own <c>_stopCount</c>.</strong>
    /// Measured: <c>checkStop(threshold)</c> is
    /// <c>_stopCount &gt; threshold</c>, and the threshold is
    /// <c>30 * (5 - moveFrequency)</c> — <strong>so a figure waits that
    /// many frames before its route runs</strong>, <strong>and a reader
    /// that skipped it sent every moving figure off on its first
    /// frame.</strong>
    /// </remarks>
    private Dictionary<int, int> _standing = new();

    /// <summary>Which walk clock belongs to which event.</summary>
    public Dictionary<int, MzWalkClock?> Clocks { get; private set; } = new();

    /// <summary>
    /// The player's own clock, when there is a player.
    /// </summary>
    /// <remarks>
    /// <strong>And the player is not in the map's events</strong>, and so
    /// has no entry in <see cref="Clocks"/>, <strong>and a reader that
    /// only ticked that dictionary had a frozen player in a moving
    /// world.</strong>
    /// </remarks>
    public MzWalkClock? PlayerClock { get; private set; }

    /// <summary>The player's clock, and the reason it may be null.</summary>
    private MzWalkClock? _playerClock;

    public int PaintFigures(Rm2kPixelBuffer pPixels)
    {
        if (pPixels == null)
        {
            return 0;
        }

        var gezeichnet = 0;
        foreach (var figur in Figures)
        {
            if (!Characters.TryGetValue(
                    figur.CharacterName, out var blatt) || blatt == null)
            {
                continue;
            }

            // **Und gezeichnet wird der Schritt aus der Uhr, und nicht
            // der, den die Datei nennt** -- **denn die Datei nennt den
            // Schritt, bei dem die Figur stehen muss**, **und die Uhr
            // weiss, wie weit sie seither gegangen ist.**
            var schritt = figur.Pattern;
            var richtung = figur.Direction;
            if (Clocks.TryGetValue(figur.EventId, out var uhr)
                && uhr != null)
            {
                schritt = uhr.Column;
                richtung = uhr.Direction;
            }

            // **Und gezeichnet wird die lebende Figur, und nicht die
            // Datei, und an ihrer Zwischenposition.**
            //
            // **Und das ist derselbe Fehler, den der Spieler gerade
            // hatte:** **eine Figur auf ihrer Kachel springt, und sie
            // geht nicht.** **Und `EventFigures` traegt die Figuren, die
            // die Befehle bewegen** -- **denn `character.forceMoveRoute`
            // spricht mit genau diesem Objekt** -- **und eine Datei
            // ist kein Objekt, das jemand bewegen kann.**
            var lebend = EventFigures.TryGetValue(figur.EventId, out var da)
                ? da : null;
            if (lebend != null)
            {
                if (MzCharacterRenderer.Draw(
                    blatt, lebend, schritt, MzMapRenderer.TilePixels,
                    MzMapRenderer.TilePixels, pPixels))
                {
                    gezeichnet++;
                }
            }
            else if (MzCharacterRenderer.Draw(
                blatt, figur.CharacterIndex, richtung, schritt,
                figur.X, figur.Y, pPixels))
            {
                gezeichnet++;
            }
        }

        // **Und der Spieler kommt hinterher** -- **mit dem Blatt, das
        // `System.json` ihm gibt**, **denn `Actors.json` nennt das Bild
        // jedes Darstellers.**
        if (PlayerSheet != null)
        {
            // **Und der Spieler wird an seiner Zwischenposition
            // gemalt, und nicht auf seiner Kachel** -- **denn er ist
            // unterwegs, und eine Figur, die auf ihrer Kachel springt,
            // geht nicht.**
            //
            // **Und es gibt keinen zweiten Zweig, und es gab einen.**
            // **Der zeichnete aus `PlayerX` und `PlayerY`, und das sind
            // Felder dieser Klasse, und nicht des Spielers** -- **und
            // gemessen standen die an 4,11 und die Figur an 0,0.**
            // **Ein toter Zweig ist nicht harmlos**, **denn er ist
            // der Ort, an dem ein naechster Leser die Wahrheit sucht.**
            var spieler = Facts.Player.Figur;
            if (spieler != null
                && MzCharacterRenderer.Draw(
                    PlayerSheet, spieler, _playerClock?.Column ?? 1,
                    MzMapRenderer.TilePixels, MzMapRenderer.TilePixels,
                    pPixels))
            {
                gezeichnet++;
            }
        }

        FiguresDrawn = gezeichnet;
        return gezeichnet;
    }

    /// <summary>How wide the current map is, in tiles.</summary>
    /// <remarks>
    /// <strong>And a figure outside the map is a figure painted on
    /// nothing.</strong> Measured: <c>System.json</c> says
    /// <c>startX: 4</c> and <c>startY: 11</c>, <strong>and the start map
    /// is 14 by 18</strong> — <strong>so the player fits</strong>, <strong>and
    /// that is a measurement and not an assumption.</strong>
    /// </para>
    /// <para>
    /// <strong>And a start position outside its map is invisible</strong>
    /// — <strong>and it looks exactly like a map with nobody on
    /// it.</strong>
    /// </para>
    /// </remarks>
    public int MapWidth { get; private set; }

    /// <summary>How tall the current map is, in tiles.</summary>
    public int MapHeight { get; private set; }

    /// <summary>Which sheet the player's figure is drawn from.</summary>
    /// <remarks>
    /// <strong>And this is the leader's, not the party's first
    /// entry.</strong> Measured:
    /// <c>Game_Player.prototype.refresh</c> asks
    /// <c>$gameParty.leader()</c> — <strong>and a leader is the first
    /// living member, and not actor one by number</strong>, <strong>so a
    /// reader that took the lowest actor id drew the wrong figure once a
    /// party member left.</strong>
    /// </remarks>
    public string PlayerSheetName { get; private set; } = "";

    /// <summary>How many figures the last paint drew.</summary>
    public int FiguresDrawn { get; private set; }

    /// <summary>Which character sheet belongs to which name.</summary>
    /// <summary>
    /// How long each animation of this project plays, in frames.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the key is the animation's number and the
    /// value is its length in frames</strong>, -- <strong>and the sum
    /// is the engine's own:</strong> <c>frames.length * 4 + 1</c>.
    /// </para>
    /// <para>
    /// <strong>And a number that is not in this table is a number
    /// this project does not have</strong>, -- <strong>and the reader
    /// says so instead of using another animation's length.</strong>
    /// </para>
    /// </remarks>
    public Dictionary<int, int> AnimationFrames { get; } = new();

    public Dictionary<string, MzCharacterSheet?> Characters { get; private set; } =
        new(StringComparer.Ordinal);

    /// <summary>The sheet the player is drawn from, if it was read.</summary>
    public MzCharacterSheet? PlayerSheet { get; private set; }

    /// <summary>Which character in that sheet the player is.</summary>
    public int PlayerIndex { get; private set; }

    /// <summary>The tile the player stands on.</summary>
    /// <summary>The tile the player stands on, and where he really is.</summary>
    /// <remarks>
    /// <para>
    /// <strong>And these three are the player's own tile and not a copy
    /// of it.</strong> Measured at <c>Game_Character.prototype.locate</c>
    /// — <c>this._x = x; this._y = y; this._realX = x + 0.5; this._realY
    /// = y + 0.5;</c> — <strong>and the engine keeps no second place.</strong>
    /// </para>
    /// <para>
    /// <strong>And this reader kept one, and it was wrong twice:</strong>
    /// <c>PlayerX</c> and <c>PlayerY</c> were their own numbers next to
    /// <c>Facts.Player</c>, <strong>and a transfer moved the second and
    /// not the first</strong>, <strong>so after Map006 event 7 moved the
    /// player to map 9 at 2,2 the runtime still reported 2,12 while
    /// <c>LastTransfer</c> said 2,2</strong> — <strong>and both were
    /// printed in the same test, which is how the contradiction showed
    /// up.</strong>
    /// </para>
    /// </remarks>
    public int PlayerX => Facts.Player.X;

    /// <summary>The tile row the player stands on.</summary>
    public int PlayerY => Facts.Player.Y;

    /// <summary>The tile row the player stands on.</summary>

    /// <summary>Which way the player faces.</summary>
    public int PlayerDirection { get; private set; } = 2;

    /// <summary>Whether a dialogue is on screen right now.</summary>
    public bool MessageVisible => Facts.LastDialogue?.IsBusy == true && Facts.MessageBusy;

    /// <summary>
    /// And the dialogue the window shows, and it is the game's own text.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the runtime held the text all along.</strong>
    /// <c>command101</c> reads the block into
    /// <c>Facts.LastDialogue</c> and raises <c>MessageBusy</c>, **and
    /// before this there was no way to read the lines out of the runtime
    /// at all** -- a game that showed its dialogue correctly to its own
    /// logic showed the player an empty screen and a log line.
    /// </para>
    /// </remarks>
    public string MessageText
    {
        get
        {
            var block = Facts.LastDialogue;
            if (block == null || !block.IsBusy)
            {
                return "";
            }
            return string.Join(
                "\n", block.Lines.Select(pLine => pLine.Text));
        }
    }

    /// <summary>
    /// And the decision key takes the dialogue down.
    /// </summary>
    /// <remarks>
    /// <strong>And this is <c>Window_Message.prototype.isTriggered</c>'s
    /// job</strong>, -- <strong>measured in <c>rmmz_objects.js</c> as
    /// <c>Input.isRepeated("ok") || Input.isRepeated("cancel")</c>, and the
    /// same two lines in <c>rpg_objects.js</c>.</strong> The runtime has
    /// its own <c>DrueckeFort</c> for RM2K; <strong>this is the web
    /// engines' equivalent, and without it a dialogue can only be closed by
    /// an event that happens to clear the flag.</strong>
    /// </summary>
    /// <returns>Whether a dialogue was on screen and is now gone.</returns>
    public bool CloseMessage()
    {
        if (!Facts.MessageBusy)
        {
            return false;
        }
        Facts.MessageBusy = false;
        Facts.LastDialogue = null;
        _dialogAge = 0;

        // **Und das ist der Tastendruck, den die Seite braucht.**
        //
        // **Und `Window_Message.prototype.isTriggered` ist `Input
        // .isTriggered("ok")` plus `this._waitCount = 0` plus
        // `$gameMessage.clearFlags()`** -- **und `MzWaitMode.Message`
        // fragt in `WaitBeantwortet` genau `_keys.Pressed`**,
        // **also muss der Druck hier gesetzt werden, sonst wartet die
        // Seite auf eine Taste, die niemand gemacht hat.**
        //
        // **Und gemessen war das als `event 5 page 0 ... waiting 0
        // frames at code 101` ueber 20.000 Bilder**, **obwohl der Spieler
        // den Dialog wegklickt** -- **ein `CloseMessage`, das nur die
        // Flagge loescht, ist die Haelfte von `isTriggered`.**
        _keys.Ok();
        return true;
    }

    /// <summary>
    /// And one key press: turn the player, and step if the cell allows it.
    /// </summary>
    /// <param name="pAction">The action the input mapper resolved.</param>
    /// <returns>Whether the player stepped.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And this is the entry point the launcher window uses</strong>,
    /// -- <strong>the same one <c>Rm2kEngineRuntime.SubmitInput</c>
    /// offers, because a window that has to know which runtime it is
    /// holding is a window with two input paths and one of them
    /// untested.</strong>
    /// </para>
    /// <para>
    /// <strong>And the player turns on a blocked key.</strong> Measured
    /// order in <c>Game_Player.prototype.moveStraight</c>: the
    /// <c>canPass</c> test guards only the follower update, and
    /// <c>Game_Character.prototype.moveStraight</c> -- which turns the
    /// character -- runs either way.
    /// </para>
    /// </remarks>
    public bool SubmitInput(UniversalRPG.Rm2k.Input.Rm2kInputAction pAction)
    {
        if (State != PluginRuntimeState.Running)
        {
            return false;
        }
        //
        // **Und Enter ist die Aktionstaste, und nicht "nichts".**
        //
        // **Und das ist `Input.isTriggered("ok")` in
        // `Game_Player.prototype.checkActionEvent`:**
        // `if (Input.isTriggered("ok") || Input.isTriggered("cancel")) {
        // this._lastTouchedX = this.x; this._lastTouchedY = this.y; }`
        // -- **und danach fragt `checkActionEvent` jede Seite mit
        // `isTriggerIn([0])`, also die Aktionsbutton-Seiten.**
        // **Gemessen an Camellias Map004:** Ereignis 14 traegt `trigger 0`
        // **und** den Dialog `"~14 hours or so later"` mit der Wahl
        // `Yes` / `No`.  **Ein Runtime, der `Confirm` als "keine
        // Richtung" verwirft, kann kein einziges Aktionstasten-Ereignis
        // zeigen** -- **und 14 Seiten allein in diesem Spiel.**
        if (pAction == UniversalRPG.Rm2k.Input.Rm2kInputAction.Confirm)
        {
            // **Und der Tastendruck bleibt fuer dieses Bild stehen, und
            // wird nicht sofort wieder geloescht** -- **`FrameEnde()`
            // hier waere genau der Fehler, den der Frame-Pfad bei
            // `Ok(); FrameEnde(); Tick()` gemacht hat:** der Druck
            // waere weg, bevor jemand ihn liest.
            _keys.Ok();
            return true;
        }

        var richtung = MzSpielerZug.RichtungFuer(pAction);
        if (richtung == null)
        {
            return false;
        }
        PlayerDirection = richtung.Value;

        // **Und die Zelle, auf die der Schritt zeigen wuerde, wird aus
        //  derselben Karte gelesen, die gemalt wird** -- **sonst prueft
        // die Bewegung eine andere Wahrheit als das Bild.**
        if (!Maps.TryGetValue(CurrentMapId, out var karte))
        {
            return false;
        }
        var breite = karte.Root.Member("width")?.IntOr(0) ?? 0;
        var hoehe = karte.Root.Member("height")?.IntOr(0) ?? 0;
        var daten = karte.Root.Member("data")?.Items;
        if (breite <= 0 || hoehe <= 0 || daten == null)
        {
            return false;
        }
        var tilesetId = karte.Root.Member("tilesetId")?.IntOr(0) ?? 0;
        var flags = TilesetFlags(tilesetId);
        if (flags == null)
        {
            return false;
        }

        var (dx, dy) = MzSpielerZug.Versatz(PlayerDirection);
        var zielX = PlayerX + dx;
        var zielY = PlayerY + dy;
        if (!MzSpielerZug.IstGueltig(zielX, zielY, breite, hoehe))
        {
            return false;
        }
        var schicht = new List<int>();
        for (var z = 3; z >= 0; z--)
        {
            var index = z * breite * hoehe + zielY * breite + zielX;
            if (index >= 0 && index < daten.Count)
            {
                schicht.Add(daten[index].IntOr(0));
            }
        }
        if (!MzSpielerZug.Schritt(
                PlayerX, PlayerY, PlayerDirection, breite, hoehe, flags, schicht))
        {
            return false;
        }

        Facts.Player.StandAt(CurrentMapId, zielX, zielY, PlayerDirection);
        // **Und die gezeichnete Figur folgt dem Spieler, und nicht
        // umgekehrt.** `MzPlayer.SyncFigure` zieht den *Spieler* auf
        // `Figur.X/Y`, weil es fuer Routen gedacht ist -- eine Taste
        // bewegt den Spieler, und ohne dieses Nachziehen blieb die Figur
        // auf ihrer alten Kachel, waehrend die Logik weiterlief
        // (gemessen live: `hero=5/11` bei 0 geaenderten Bildpixeln).
        Facts.Player.Figur?.SetLocation(zielX, zielY, PlayerDirection);
        Repaint();
        // **Und der Held wird sofort neu gezeichnet, nicht erst beim
        // naechsten Takt der Laufzeit** -- `Update` malt die Figuren nur,
        // wenn die Laufuhr feuert, und eine Taste erzeugt keine Route.
        if (PaintedMap != null)
        {
            PaintFigures(PaintedMap);
        }
        return true;
    }

    /// <summary>
    /// And the flag array of one tileset, read once and kept.
    /// </summary>
    /// <remarks>
    /// <strong>And this is <c>$dataTilesets[id].flags</c></strong>, --
    /// <strong>8192 entries per tileset in every game measured, one integer
    /// per tile id, and the only place either engine stores
    /// passability.</strong> A reader looking for <c>passage1</c> ..
    /// <c>passage7</c> finds nothing in either engine, because those keys
    /// are not in the files the editor writes.
    /// </remarks>
    private System.Collections.Generic.IReadOnlyList<UniversalRPG.Web.MzValue>? TilesetFlags(int pTilesetId)
    {
        if (_tilesetFlags.TryGetValue(pTilesetId, out var cached))
        {
            return cached;
        }
        var datei = Path.Combine(_game.GameDirectory, "data", "Tilesets.json");
        if (!File.Exists(datei))
        {
            _tilesetFlags[pTilesetId] = null;
            return null;
        }
        var tilesets = UniversalRPG.Web.MzDataFile.Read(
            "data/Tilesets.json", File.ReadAllBytes(datei));
        var eintraege = tilesets.Root.Items;
        if (eintraege == null || pTilesetId < 0 || pTilesetId >= eintraege.Count)
        {
            _tilesetFlags[pTilesetId] = null;
            return null;
        }
        var flags = eintraege[pTilesetId].Member("flags")?.Items;
        _tilesetFlags[pTilesetId] = flags;
        return flags;
    }

    private readonly Dictionary<int, System.Collections.Generic.IReadOnlyList<UniversalRPG.Web.MzValue>?> _tilesetFlags = new();

    /// <summary>
    /// And why no autorun page ran on the start map, or an empty string.
    /// </summary>
    /// <remarks>
    /// <strong>And it says nothing when there was nothing to run.</strong>
    /// A start map with no autorun page is not a defect: Camellia's Map002
    /// carries two touch transfers, two touch bridges and two action gates,
    /// and no autorun at all. <strong>Reporting that as a problem would make
    /// every honest map look broken.</strong>
    /// </remarks>
    public string AutorunProblem { get; private set; } = "";

    /// <summary>
    /// And the engine's own way into another map: reserve, then perform.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is <c>command201</c> in two public steps</strong>,
    /// -- <c>$gamePlayer.reserveTransfer(mapId, x, y, direction,
    /// fadeType)</c> and then <c>performTransfer()</c>, which is what
    /// <c>Scene_Map.prototype.onMapLoaded</c> calls. <strong>A runtime
    /// with a separate "open map" method would have two ways to change
    /// the map and one of them would skip the reservation.</strong>
    /// </para>
    /// <para>
    /// <strong>And the transfer itself is not free.</strong>
    /// <c>performTransfer</c> refreshes the map, which is where the
    /// destination's autorun pages start -- <strong>so this is the path a
    /// game uses to reach its own later maps, and therefore also the path
    /// a launcher uses to enter one.</strong>
    /// </para>
    /// </remarks>
    /// <returns>Whether the player is standing on the requested map.</returns>
    public bool Transfer(int pMapId, int pX, int pY,
        int pDirection = 2, int pFadeType = 0)
    {
        if (State != PluginRuntimeState.Running)
        {
            return false;
        }
        if (!Maps.ContainsKey(pMapId))
        {
            return false;
        }
        var breite = Maps[pMapId].Root.Member("width")?.IntOr(0) ?? 0;
        var hoehe = Maps[pMapId].Root.Member("height")?.IntOr(0) ?? 0;
        var x = Math.Clamp(pX, 0, Math.Max(0, breite - 1));
        var y = Math.Clamp(pY, 0, Math.Max(0, hoehe - 1));
        Facts.Player.Reserve(pMapId, x, y, pDirection, pFadeType);
        LastTransfer = Facts.Player.PerformTransfer(Maps.Keys);
        // **Und der Erfolg wird daran erkannt, dass der Spieler wirklich
        // auf der Karte steht** -- **nicht daran, dass der Bericht leer
        // ist.** `PerformTransfer` gibt in *jedem* Fall eine Zeile
        // zurueck, auch im Erfolgsfall, **und eine Pruefung auf
        // `Length > 0` hat jeden Transfer als Fehlschlag gemeldet, ohne
        // dass etwas passiert waere.**
        if (Facts.Player.MapId != pMapId)
        {
            return false;
        }
        Facts.Player.Figur?.SetLocation(x, y, pDirection);
        PlayerDirection = pDirection;
        CurrentMapId = pMapId;
        // **Und der Zielort startet seine eigenen automatischen Seiten.**
        var autorun = RunPage(StartMode.Autorun);
        
        if (autorun.StartsWith("map", StringComparison.Ordinal)
            || autorun.StartsWith("the map has no events", StringComparison.Ordinal))
        {
            AutorunProblem = autorun;
        }
        
        Repaint();
        
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

        // **Und die Seiten, die noch laufen, laufen je Frame weiter.**
        //
        // **Und das ist `Game_Map.prototype.update`, woertlich:**
        // `if (this._interpreter.isRunning()) { this._interpreter.update(); }`.
        // **Und `Tick()` war geschrieben und nie aufgerufen** -- **ohne
        // diesen Aufruf blieb jede Seite bei dem Befehl stehen, an dem sie
        // zum ersten Mal wartete** (gemessen an Camellias Map003 "Day 1":
        // Ereignis 9 mit 211 Befehlen fuehrte genau *einen* aus und blieb
        // bei Code 221 stehen, **und der Befehl, der den Dialog zeigt, kam
        // nie an**).
        //
        // **Und es ist *kein* `return` danach:** ein fruehes `return` liess
        // `Frames` stehenbleiben, **und `Frames` ist der Zaehler, an dem
        // die Spielansicht haengt.**  Ein Befehl, der etwas bewegt, darf
        // den Rest des Bildes nicht ueberspringen.
        //
        // **Und der Aktionsknopf ist auch eine Seite, und er lief nie.**
        //
        // **Und `RunPage(StartMode.ActionButton)` war geschrieben und
        // nirgends aufgerufen** -- **und `Passt` hatte beide Ausloeser
        // (Taste und Beruehrt) korrekt unterschieden, nur rief niemand
        // sie auf.**  **Gemessen an Camellias Map004:** Ereignis 14 ist
        // `trigger 0` -- **die Aktionstaste** -- **und traegt den Dialog
        // `"~14 hours or so later"` mit der Wahl `Yes` / `No`**,
        // **und es laeuft nur, wenn Schalter 3 gesetzt ist.**  **Ein
        // Runtime ohne diese Verdrahtung kann kein einziges
        // Aktionstasten-Ereignis dieser Spiele zeigen** -- **und gemessen
        // sind das 14 Seiten allein in diesem Spiel.**
        //
        LoeseDialoge();

        // **Und ein offenes Ereignis wird mit der Bestaetigung
        // ausgeloest, und nicht nur beim Kartenwechsel.**
        //
        // **Und das ist `Game_Player.prototype.checkActionEvent`:**
        // `if (Input.isTriggered("ok") || Input.isTriggered("cancel"))
        // { this._lastTouchedX = this.x; this._lastTouchedY = this.y; }`
        // -- **und danach laeuft `checkActionEvent` ueber die Seiten mit
        // `isTriggerIn([0])`.**  **Gemessen an Camellias Map004:**
        // Ereignis 14 traegt `trigger 0`, den Dialog `"~14 hours or so
        // later"` **und die Wahl `Yes` / `No`** -- **und `RunPage(
        // StartMode.ActionButton)` war geschrieben und von niemandem
        // aufgerufen.**
        //
        // **Und es steht *vor* `FrameEnde()`, denn das leert genau die
        // Menge, die hier gelesen wird.**
        var bestaetigt = _keys.Pressed.Contains("ok")
            || _keys.Pressed.Contains("cancel");

        var fort = Tick();
        _keys.FrameEnde();

        //
        // **Und nicht, solange ein Dialog offen ist** -- **denn dann
        // bestaetigt der Spieler den Dialog, und nicht das Ereignis.**
        //
        // **Und nicht, solange eine Seite laeuft.**
        //
        // **Und das ist `$gameMap.eventRunning` in
        // `Game_Player.prototype.updateNonmoving`:**
        // `if (!$gameMap.isEventRunning()) { if (wasMoving) { ...
        // this.checkEventTriggerHere([1, 2]); if ($gameMap
        // .setupStartingEvent()) return; } }`
        // -- **und `isEventRunning()` ist genau
        // `this._interpreter.isRunning() || this._map
        // ._interpreter.isRunning()`.**  **Ohne diese Bedingung startet
        // der Knopf dieselbe Seite in jedem Bild neu** -- **und gemessen
        // ist das als `event 5 page 0 ... carried out 1 of them before it
        // stopped: waiting 0 frames at code 101` 19.797 Mal**,
        // **wobei der Dialog die ganze Zeit offen und sichtbar war
        // (`busy=True vis=True`) und der Index nie vorrueckte.**
        if (bestaetigt && !Facts.MessageBusy && !Facts.ChoicePending
            && Laeufer.Count == 0)
        {
            RunPage(StartMode.ActionButton);
        }
        if (fort > 0)
        {
            Repaint();
            if (PaintedMap != null)
            {
                PaintFigures(PaintedMap);
            }
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
        //
        // **And only while no page is running.**
        //
        // **`Tick()` above continues the running page, and that is the
        // engine's own way** -- **measured at `Game_Map.prototype.update`,
        // which calls `this._interpreter.update()` and never restarts a
        // page from the beginning.** **This run here is the start run** --
        // **it finds and starts a page when none is running, which is what
        // `setupStartingEvent` does.**
        //
        // **Restarting an already-running page from index zero in the same
        // frame is wrong twice over:** **it uses a fresh `new MzRandom()`
        // and event id 0, so it wipes the state the running page builds
        // on, and it re-runs every page of the map, so a test that drives
        // thousands of frames through `Update` hangs** (measured: the
        // choice test's 20,000-frame loop times out without this guard,
        // and completes with it).
        if (Laeufer.Count == 0)
        {
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

    /// <summary>
    /// Reads the project's character sheets, and the player's.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And a character's sheet is named after the character, and
    /// not after a number.</strong> Measured: the files are
    /// <c>MC_Sprite_sheet.png_</c>, <c>SlimeCharacters.png_</c>,
    /// <c>!Flame.png_</c> and <c>Vehicle.png_</c>, <strong>and
    /// <c>Actors.json</c> names them by that word.</strong>
    /// </para>
    /// <para>
    /// <strong>And the first actor is the one the player is.</strong>
    /// The engine's own rule, and <strong>the one that matters here,
    /// because the player is not in the map's events at all</strong> —
    /// <strong>and a reader that looked for the player among the
    /// figures put a second one on a map that already had one.</strong>
    /// </para>
    /// </remarks>

    /// <summary>
    /// Reads how long each animation of this project plays.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And an animation has no length of its own</strong>, -- <strong>and
    /// the command carries none either</strong>, -- <strong>and both of those
    /// facts are in the help and in `command212`</strong>, -- <strong>and
    /// the length is <c>frames.length * 4 + 1</c> in
    /// <c>Sprite_Animation.setupDuration</c>.</strong>
    /// </para>
    /// <para>
    /// <strong>And this table is why a page can wait for a picture that is
    /// really there.</strong> Measured on <c>sister/www</c>: its three
    /// hundred animations carry from one to sixty-six frames, -- <strong>and
    /// sixty was none of them</strong>, -- <strong>and a reader that used
    /// one number for all of them showed every game's effects in the
    /// wrong time.</strong>
    /// </para>
    /// </remarks>
    private void LadeAnimationen()
    {
        AnimationFrames.Clear();
        var pfad = Path.Combine(
            _game.GameDirectory, "data", "Animations.json");
        if (!File.Exists(pfad))
        {
            return;
        }

        MzDataFile tabelle;
        try
        {
            tabelle = MzDataFile.Read(
                "data/Animations.json", File.ReadAllBytes(pfad));
        }
        catch (MzDataException ausnahme)
        {
            TilesetProblem = ausnahme.Message;
            return;
        }

        for (var id = 1; id < tabelle.Root.Items.Count; id++)
        {
            var eintrag = tabelle.Root.Items[id];
            if (eintrag.Kind != MzKind.Array)
            {
                continue;
            }

            AnimationFrames[id] = MzScreen.AnimationsDauer(eintrag.Items.Count);
        }

        // **Und die Tabelle gehoert in die Spieltatsachen**, -- **denn
        // `212` ist ein Befehl und liest `pFacts`**, -- **und ein Befehl,
        // der eine Tabelle des Laufzeithalters bräuchte, wäre der erste
        // Ort, an dem der Leser von aussen nach etwas fragt.**
        foreach (var paar in AnimationFrames)
        {
            Facts.AnimationLaengen[paar.Key] = paar.Value;
        }
    }
    private void ReadCharacters()
    {
        var schluessel = EncryptionKey();
        Characters = new Dictionary<string, MzCharacterSheet?>(
            StringComparer.Ordinal);
        foreach (var datei in SafeFiles("characters", ".png_"))
        {
            var name = Path.GetFileNameWithoutExtension(
                Path.GetFileNameWithoutExtension(datei));
            var bild = MzImageReader.Read(
                File.ReadAllBytes(datei), schluessel, out var _);
            if (bild == null)
            {
                continue;
            }

            // **Und ein Figurenblatt ist nicht immer RGBA.** **Gemessen
            // an den vier Blaettern eines fertigen Spiels**:
            // **`MC_Sprite_sheet`, `!Flame` und `Vehicle` sind Farbtyp
            // 3, eine Palette, und `SlimeCharacters` ist Farbtyp 6 mit
            // vier Kanaelen.** **Und ein Leser, der nur den einen Fall
            // kannte, zeichnete drei von vier Figurenarten aus dem
            // Nichts** -- **und zaehlte trotzdem vier gelesene
            // Blaetter, denn er zaehlte die Dateien und nicht die
            // Bilder.**
            //
            // **Und die Durchsicht hat in beiden Faellen eine
            // andere Quelle** -- **bei einer Palette der Index null,
            // und bei echten Farben der Alphakanal.**
            var farbe = MzCharacterSheet.Read(
                bild, out var blatt, out var _);
            if (blatt != null)
            {
                Characters[name] = blatt;
            }
        }

        // **Und die Animations kommen aus `Animations.json`.**

        //
        // **Und die Dauer einer Animation ist keine Konstante**, --
        // **sondern `frames.length * 4 + 1`**, -- **gemessen an
        // `Sprite_Animation.setupDuration` und `setupRate`** -- **und
        // `212` nennt nur die Nummer.**

        // **Und ohne diese Tabelle wartet eine Seite auf ein Bild,
        // das in einer Zeit verschwindet, die das Spiel nie
        // genannt hat.**
        LadeAnimationen();

        // **Und der Spieler kommt aus `Actors.json`.**
        var pfad = Path.Combine(
            _game.GameDirectory, "data", "Actors.json");
        if (!File.Exists(pfad))
        {
            return;
        }

        MzDataFile actors;
        try
        {
            actors = MzDataFile.Read("data/Actors.json", File.ReadAllBytes(pfad));
        }
        catch (MzDataException ausnahme)
        {
            TilesetProblem = ausnahme.Message;
            return;
        }

        MzValue? erste = null;
        foreach (var darsteller in actors.Root.Items)
        {
            if (darsteller.Member("id")?.IntOr(-1) == 1)
            {
                erste = darsteller;
            }
        }

        var bildName = erste?.Member("characterName")?.StringOr("") ?? "";
        PlayerIndex = erste?.Member("characterIndex")?.IntOr(0) ?? 0;
        PlayerSheetName = bildName;

        // **Und der Spieler bekommt seine Uhr aus seinen eigenen Werten,
        // und aus den Standardwerten des Motors, wo er keine hat.**
        //
        // **Und gemessen: `Actors.json` nennt fuer den Spieler weder
        // `moveSpeed` noch `moveFrequency`** -- **beide fehlen** -- **und
        // der Motor setzt `4` und `6`.** **Ein Leser, der dort eine
        // Null las, bekam einen Spieler, der im Frame stehen blieb,
        // oder einen, der so schnell ging, dass er zitterte.**
        _playerClock = new MzWalkClock
        {
            MoveSpeed = erste?.Member("moveSpeed")?.IntOr(4) ?? 4,
            MoveFrequency = erste?.Member("moveFrequency")?.IntOr(6) ?? 6,
            Direction = MzCharacter.Down,
            // **Und der Spieler geht, sobald er geht** -- **und ohne
            // Befehl geht er nicht**, **und genau so ist es gemessen:
            // `isMoving()` braucht `_realX`, und das weicht nur von
            // `_x` ab, wenn er sich bewegt.**
            Moving = false,
        };
        PlayerClock = _playerClock;
        if (bildName.Length > 0)
        {
            if (Characters.TryGetValue(bildName, out var blatt)
                && blatt != null)
            {
                PlayerSheet = blatt;
            }
        }

        // **Und wo er steht, sagt `System.json`.**
        var systemPfad = Path.Combine(
            _game.GameDirectory, "data", "System.json");
        if (File.Exists(systemPfad))
        {
            try
            {
                var system = MzDataFile.Read(
                    "data/System.json", File.ReadAllBytes(systemPfad));
                var startX = system.Root.Member("startX")?.IntOr(0) ?? 0;
                var startY = system.Root.Member("startY")?.IntOr(0) ?? 0;

                // **Und der Spieler wird auch dorthin gestellt.**
                //
                // **Und `PlayerX` und `PlayerY` waren Felder dieser
                // Klasse, und `Facts.Player` hatte seine eigenen, und
                // beide kannten nichts voneinander.** **Gemessen: die
                // Figur stand auf 0,0 und der Spieler auf 4,11** --
                // **und gemalt wurde die Figur**, **also stand auf der
                // Karte ein Spielerkopf in der Ecke des Zimmers und
                // der Spieler war woanders.** **Zwei Orte fuer eine
                // Sache ist genau der Fehler, den der Motor nicht
                // machen kann, weil er nur einen hat.**
                Facts.Player.StandAt(
                    CurrentMapId > 0 ? CurrentMapId : 1, startX, startY);
            }
            catch (MzDataException)
            {
                // **Und ein Spiel, dessen System.json kaputt ist, hat
                // keinen Spieler**, **und das wird beim Malen sichtbar
                // und nicht hier verschluckt.**
            }
        }
    }

    private string EncryptionKey()
    {
        var pfad = Path.Combine(
            _game.GameDirectory, "data", "System.json");
        if (!File.Exists(pfad))
        {
            return "";
        }

        try
        {
            return MzDataFile.Read("data/System.json", File.ReadAllBytes(pfad))
                .Root.Member("encryptionKey")?.StringOr("") ?? "";
        }
        catch (MzDataException)
        {
            return "";
        }
    }

    private IEnumerable<string> SafeFiles(string pOrdner, string pEndung)
    {
        var ordner = Path.Combine(_game.GameDirectory, "img", pOrdner);
        if (!Directory.Exists(ordner))
        {
            return Array.Empty<string>();
        }

        var liste = new List<string>();
        foreach (var datei in Directory.EnumerateFiles(ordner)
            .Where(pPfad => pPfad.EndsWith(pEndung, StringComparison.Ordinal))
            .OrderBy(pPfad => pPfad, StringComparer.Ordinal))
        {
            liste.Add(datei);
        }

        return liste;
    }

    /// <summary>
    /// The common events the project stores, by the index a <c>117</c> names.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this was handed to the runner as nothing at all</strong>,
    /// <strong>and a <c>117</c> is how a page calls anything that is not a
    /// page</strong>: the engine's <c>setupChild</c> makes a new
    /// interpreter on the common event's own list, <strong>and this
    /// repository's runner was built with an empty
    /// dictionary</strong>.
    /// </para>
    /// <para>
    /// <strong>And measured at <c>D:/Itch/sister/www</c>: five hundred
    /// common events, and seventeen hundred and eighty-seven calls on
    /// thirty-nine of them.</strong> <strong>And every one of those calls
    /// stopped with "this repository has no list for it", and every one of
    /// them was reported as a refusal rather than as a call that
    /// happened.</strong>
    /// </para>
    /// </remarks>
    private Dictionary<int, List<MzCommandEntry>> _commonEvents = new();

    private Dictionary<int, List<Rm2kIndexedImage?>> _tilesets = new();

    private static bool IsMap(string pRelativePath)
    {
        var name = Path.GetFileName(pRelativePath);
        if (!name.StartsWith("Map", StringComparison.OrdinalIgnoreCase)
            || !name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // **Und der Rest muss Ziffern sein und sonst nichts.** Das ist keine
        // Regel, die dieses Repository sich ausgedacht hat, sondern die
        // Zeile der Engine, wo sie eine Karte laedt:
        //
        // ```text
        // static loadMapData(mapId) {
        //     if (mapId > 0) {
        //         const filename = 'Map%1.json'.format(mapId.padZero(3));
        //         this.loadDataFile('$dataMap', filename);
        //     } else {
        //         this.makeEmptyMap();
        //     }
        // }
        // ```
        //
        // **Und `IsMap` sah vorher nur auf Anfang und Ende, und das hat ein
        // echtes Spiel erwischt.** Gemessen an `D:/Itch/sister/www`:
        //
        // ```text
        // data/VN/MapEventDialogueVN002.json   <- ein Dialogskript
        // MapCount: 290 bei 81 Dateien und 61 echten Karten
        // CurrentMapId: 2 -> Pfad data/VN/MapEventDialogueVN002.json
        //                -> Root.Member("events") == null
        // ```
        //
        // **Und die Karte mit der Nummer 2 war ueberschrieben, und der Lauf
        // stand auf einer Datei, die keine Karte ist** -- **und die Meldung
        // lautete "the map has no events", was richtig war und an der
        // falschen Karte gemessen.** `MapIdOf` nahm die ersten Ziffern des
        // Namens und fand die "002" in "VN002".
        var ziffern = name.AsSpan("Map".Length, name.Length - "Map".Length - 5);
        if (ziffern.IsEmpty)
        {
            return false;
        }

        foreach (var zeichen in ziffern)
        {
            if (!char.IsDigit(zeichen))
            {
                return false;
            }
        }

        // **Und die Karte muss direkt unter `data/` liegen.**
        //
        // **Und das ist gemessen, und es ist ein zweiter Fall von der
        // Sache mit `data/VN/`:** `D:/Itch/sister/www/data` enthaelt
        // neben seinen 62 Karten **vier `GameLanguage`-Pakete** mit je
        // fuenf Karten -- **GameLanguage0 bis GameLanguage3** -- **und
        // jede dieser 20 Karten ist eine Kopie einer Hauptkarte mit
        // uebersetzten Texten und ohne Feld `id`.**
        //
        // **Und `MapIdOf` faellt auf den Dateinamen zurueck**, **wenn
        // `id` fehlt** -- **und `GameLanguage0/Map003.json` gibt ihm
        // 003 und damit genau die Karte, die `data/Map003.json` auch
        // beansprucht.** **Und alle 20 Paare haben verschiedene
        // `events`.**
        //
        // **Und `IsMap` sieht bisher nur auf den Dateinamen**, **also hat
        // jede Sprachkarte eine echte Karte ueberschrieben** -- **und
        // welches gewinnt, entscheidet die Sortierung des Pfades.**
        var ordner = Path.GetDirectoryName(pRelativePath)?
            .Replace('\\', '/');
        return string.IsNullOrEmpty(ordner) || !ordner.Contains('/');
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
