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
    public bool GoTo(int pMapId)
    {
        if (!Maps.ContainsKey(pMapId))
        {
            PaintReason = $"Map {pMapId} is not among the maps this "
                + "runtime read.";
            return false;
        }

        CurrentMapId = pMapId;
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
        ReadCharacters();
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

        // **Und die Figuren werden ueber die Kacheln gemalt, und nicht
        // in sie hinein** -- **denn eine Figur ist 144 Pixel breit und
        // eine Kachel 48**, **und sie steht mittig auf dreien.**
        Figures = MzMapFigureReader.Read(
            karte.Root, _facts, out var notwendig);
        FigureNotes = notwendig;

        // **Und jede Figur bekommt ihre Uhr, und aus den Werten, die
        // ihre Seite nennt** -- **und nicht aus geratenen
        // Standardwerten**, **denn die Seite sagt Tempo und Takt
        // ausdruecklich.**
        Clocks = new Dictionary<int, MzWalkClock?>();
        foreach (var figur in Figures)
        {
            Clocks[figur.EventId] = new MzWalkClock
            {
                MoveSpeed = figur.MoveSpeed,
                MoveFrequency = figur.MoveFrequency,
                Direction = figur.Direction,
                Pattern = figur.Pattern < MzWalkClock.MaxPattern
                    && figur.Pattern >= 0 ? figur.Pattern : 1,
                // **Und eine Seite mit `moveType: 0` bewegt sich gar
                // nicht** -- **denn "fest" ist eine Art des
                // Stillstands, und nicht eine leere Angabe.**
                Moving = figur.MoveType != 0,
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
    public int Tick()
    {
        var wechselt = 0;
        foreach (var uhr in Clocks.Values)
        {
            if (uhr != null && uhr.Tick())
            {
                wechselt++;
            }
        }

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

    /// <summary>How many frames this runtime has run.</summary>
    public int Frames { get; private set; }

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
            if (Clocks.TryGetValue(figur.EventId, out var uhr)
                && uhr != null)
            {
                schritt = uhr.Column;
            }

            if (MzCharacterRenderer.Draw(
                blatt, figur.CharacterIndex, figur.Direction, schritt,
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
            if (MzCharacterRenderer.Draw(
                PlayerSheet, PlayerIndex, PlayerDirection,
                _playerClock?.Column ?? 1, PlayerX, PlayerY, pPixels))
            {
                gezeichnet++;
            }
        }

        FiguresDrawn = gezeichnet;
        return gezeichnet;
    }

    /// <summary>How many figures the last paint drew.</summary>
    public int FiguresDrawn { get; private set; }

    /// <summary>Which character sheet belongs to which name.</summary>
    public Dictionary<string, MzCharacterSheet?> Characters { get; private set; } =
        new(StringComparer.Ordinal);

    /// <summary>The sheet the player is drawn from, if it was read.</summary>
    public MzCharacterSheet? PlayerSheet { get; private set; }

    /// <summary>Which character in that sheet the player is.</summary>
    public int PlayerIndex { get; private set; }

    /// <summary>The tile the player stands on.</summary>
    public int PlayerX { get; private set; }

    /// <summary>The tile row the player stands on.</summary>
    public int PlayerY { get; private set; }

    /// <summary>Which way the player faces.</summary>
    public int PlayerDirection { get; private set; } = 2;

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
                PlayerX = system.Root.Member("startX")?.IntOr(0) ?? 0;
                PlayerY = system.Root.Member("startY")?.IntOr(0) ?? 0;
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
