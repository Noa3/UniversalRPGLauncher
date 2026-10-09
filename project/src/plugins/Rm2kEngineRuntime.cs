using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using UniversalRPG.Rm2k.Rendering;
using UniversalRPG.Core;
using UniversalRPG.Rm2k;
using UniversalRPG.Rm2k.Interpreter;
using UniversalRPG.Rm2k.Parser;
using UniversalRPG.Rm2k.Presentation;
using UniversalRPG.Rm2k.Simulation;

namespace UniversalRPG.Plugins;

/// <summary>
/// Minimal native RM2K/RM2K3 runtime backend. It loads the validated LDB/LMT
/// and first LMU through the existing bounded parser, then advances a
/// deterministic 60 Hz simulation clock. Decoded native event pages are driven
/// by the scheduler during Update(); unsupported commands remain data-only and
/// diagnostic. Launching no longer requires the original RPG_RT executable.
/// </summary>
public sealed class Rm2kEngineRuntime : IEngineRuntime, IRuntimeSaveTools, IRuntimeDebugTools
{
    private const int MaxGold = 999999;
    private readonly string _pluginId;
    private readonly PluginGameInfo _game;
    private readonly Rm2kParser _parser = new();
    private readonly VirtualClock _clock = new();
    private readonly Rm2kEventScheduler _eventScheduler;
    private readonly Rm2kPlayerTurn _playerTurn;
    private readonly Rm2kRendererAdapter _rendererAdapter = new();
    private readonly Rm2kSpriteAdapter _spriteAdapter = new();
    private bool _debugToolsEnabled;

    public Rm2kEngineRuntime(string pPluginId, PluginGameInfo pGame)
    {
        _pluginId = pPluginId;
        _game = pGame;
        _eventScheduler = new Rm2kEventScheduler(Simulation, Presentation);
        _playerTurn = new Rm2kPlayerTurn(Simulation, _eventScheduler);
    }

    public PluginRuntimeState State { get; private set; } = PluginRuntimeState.Created;
    public Godot.Collections.Dictionary? DatabaseData { get; private set; }
    public Godot.Collections.Dictionary? MapTreeData { get; private set; }
    public Godot.Collections.Dictionary? CurrentMapData { get; private set; }
    public VirtualFramebuffer? Framebuffer { get; private set; }
    public IReadOnlyList<Rm2kSpriteDescriptor> SpriteDescriptors { get; private set; } = Array.Empty<Rm2kSpriteDescriptor>();
    public PresentationState Presentation { get; } = new();
    public GameSimulationState Simulation { get; } = new();
    public Rm2kEventScheduler EventScheduler => _eventScheduler;
    public int SimulationTicks => _clock.GetSimulationTicks();
    public bool DebugToolsEnabled => _debugToolsEnabled;

    public PluginOperationResult Initialize(EnginePluginRuntimeContext pContext)
    {
        if (State != PluginRuntimeState.Created)
        {
            return Fail(PluginErrorCode.InvalidLifecycleTransition, "RM2K runtime was already initialized.", "initialize");
        }
        var root = ResolveGameDirectory();
        if (root == null)
        {
            return Fail(PluginErrorCode.InvalidGame, "RM2K/RM2K3 runtime requires an imported game directory.", "initialize");
        }

        var databasePath = FindRootFile(root, "RPG_RT.ldb");
        var mapTreePath = FindRootFile(root, "RPG_RT.lmt");
        if (databasePath == null || mapTreePath == null)
        {
            return Fail(PluginErrorCode.InvalidGame, "RM2K/RM2K3 requires RPG_RT.ldb and RPG_RT.lmt.", "initialize");
        }

        var database = _parser.ParseDatabase(databasePath);
        if (!database.Success)
        {
            return Fail(PluginErrorCode.InvalidGame,
                $"Could not parse RPG_RT.ldb: {database.Error?.Describe() ?? "unknown parser error"}", "initialize");
        }
        var mapTree = _parser.ParseMapTree(mapTreePath);
        if (!mapTree.Success)
        {
            return Fail(PluginErrorCode.InvalidGame,
                $"Could not parse RPG_RT.lmt: {mapTree.Error?.Describe() ?? "unknown parser error"}", "initialize");
        }

        Godot.Collections.Dictionary? currentMap = null;
        var mapPath = PickStartMap(root, mapTree.Data);
        if (mapPath != null)
        {
            var map = _parser.ParseMap(mapPath);
            if (!map.Success)
            {
                return Fail(PluginErrorCode.InvalidGame,
                    $"Could not parse {Path.GetFileName(mapPath)}: {map.Error?.Describe() ?? "unknown parser error"}", "initialize");
            }
            currentMap = map.Data;
        }

        DatabaseData = database.Data;
        MapTreeData = mapTree.Data;
        CurrentMapData = currentMap;

        // **Und  der  Zustand  bekommt  dieselbe  Bank** -- **denn
        //  `10710`  laeuft  im  Interpreter  und  nicht  im  Host**,
        // **und  ohne  die  Bank  bleiben  seine  Gegner
        //  leer.**
        Simulation.DatabaseData = database.Data;

        // **Und  die  Heldenbank  wandert  in  den  Zustand.**
        //
        // **Und  vorher  las  der  Host  sie  nur  fuer  Sprite-Namen**,
        // -- **und  `GetOrCreateActorValues`  erfand  sonst  leere
        //  Werte** -- **und  das  Menue  zeigte  darum  einen  Helden
        //  ohne  Namen  und  ohne  Maximalwerte.**
        //
        // **Und  das  war  eine  stille  Luecke**, -- **denn  die  Werte
        //  waren  vorhanden  und  nur  nirgends  angefasst.**
        ReadActorValues(database.Data);

        // **Und  die  Party  kommt  aus  dem  System-Chunk  der  Bank.**
        //
        // **Und  EasyRPG Players  `Game_Party::SetupNewGame` macht  es
        //  genauso**:  -- **`data.party = lcf::Data::system.party`** --
        // -- **und  nicht  aus  einer  Karte  und  nicht  aus  einem
        //  Befehl.**
        //
        // **Und  Dragon  Destinys  Party  ist  `[1, 0]`  in  den
        //  Rohbytes**, -- **und  das  ist  Little-Endian  `1`**, --
        // **und  das  ist  Held  eins**, -- **und  der  Decoder  meldete
        //  vorher  eine  leere  Party**, -- **weil  `party_size`  im
        //  Spiel  fehlt  und  ein  fehlendes  Groessenfeld  die
        //  vorhandenen  Bytes  nicht  verwerfen  darf.**
        ReadStartParty(database.Data);

        // **Und  die  Helden  kommen  mit  ihren  gelernten
        //  Faehigkeiten  in  den  Zustand.**
        //
        // **Und  das  ist  das  sechste  Feld  dieser  Gestalt**, --
        // **geschrieben  von  einem  Test  und  gefuellt  von  nichts,
        //  was  ein  Spiel  erreichen  koennte.**  **Und  es  hat  den
        //  Schlag  gekostet**:  -- **denn  `ErsteAngriffsfaehigkeit`
        //  fragt  `FaehigkeitenVon`,  und  das  fragt  `SkillsOf`,  und
        //  das  las  eine  Menge,  die  niemand  gefuellt  hat.**
        //
        // **Und  es  passiert  hier  und  nicht  erst  beim
        //  Kampfstart**, -- **denn  ein  Held  lernt  nicht  erst
        //  waehrend  eines  Kampfes.**
        var heldenGeladen = UniversalRPG.Rm2k.Simulation
            .Rm2kHeldLaden.LadeAlle(Simulation, database.Data);
        if (heldenGeladen > 0)
        {
            Simulation.AddDiagnostic(
                "RM2K " + heldenGeladen
                + " heroes carry their learned skills");
        }

        // **Und  jetzt  die  Animationstabelle** -- **denn
        //  `11210`  wird  792  Mal  geschrieben  und  ohne  diese
        //  Tabelle  spielt  jede  dieser  Animationen  nichts.**
        var anims = UniversalRPG.Rm2k.Simulation.Rm2kHeldLaden
            .LadeAnimationen(Simulation, database.Data);
        if (anims > 0)
        {
            Simulation.AddDiagnostic(
                "RM2K " + anims
                + " battle animations carry their timings");
        }

        try
        {
            ConfigureSimulationMap(currentMap, mapTree.Data, mapPath);
        }
        catch (InvalidDataException exception)
        {
            return Fail(PluginErrorCode.InvalidGame, exception.Message, "initialize-map");
        }
        if (currentMap != null)
        {
            var renderResult = _rendererAdapter.CreateFramebuffer(currentMap);
            if (!renderResult.Success || renderResult.Framebuffer == null)
            {
                return Fail(PluginErrorCode.InvalidGame,
                    $"Could not create RM2K map framebuffer: {renderResult.Error}", "initialize-render");
            }
            Framebuffer = renderResult.Framebuffer;
            // The events have to be loaded before the first frame: the frame
            // draws their characters, and rendering first produced a map without
            // a single character while still looking plausible.
            LoadCurrentMapEvents(currentMap);
            _currentMap = currentMap;
            _currentMapWidth = (int)currentMap["width"];
            _currentMapHeight = (int)currentMap["height"];
            RenderCurrentMap(currentMap, _currentMapWidth, _currentMapHeight);

            var spriteResult = _spriteAdapter.BuildDescriptors(
                currentMap, Simulation.MapX, Simulation.MapY);
            if (!spriteResult.Success)
            {
                return Fail(PluginErrorCode.InvalidGame,
                    $"Could not create RM2K sprite descriptors: {spriteResult.Error}", "initialize-sprites");
            }
            SpriteDescriptors = spriteResult.Descriptors;
        }
        else
        {
            LoadCurrentMapEvents(null);
        }
        Simulation.SupportsRpg2k3Commands = _pluginId == EnginePluginIds.RpgMaker2003;
        State = PluginRuntimeState.Initialized;
        return PluginOperationResult.Succeeded(new[]
        {
            PluginDiagnostic.Info(
                "rm2k.runtime-initialized",
                currentMap == null
                    ? "Loaded RM2K/RM2K3 database and map tree; no LMU map was present."
                    : $"Loaded RM2K/RM2K3 database, map tree, and {Path.GetFileName(mapPath)}.",
                _pluginId),
        });
    }

    public PluginOperationResult Start()
    {
        if (State != PluginRuntimeState.Initialized)
        {
            return Fail(PluginErrorCode.InvalidLifecycleTransition,
                $"RM2K runtime cannot start from state {State}.", "start");
        }
        State = PluginRuntimeState.Running;
        return PluginOperationResult.Succeeded();
    }

    public bool TryMove(int pDeltaX, int pDeltaY)
    {
        if (State != PluginRuntimeState.Running || !Simulation.TryMove(pDeltaX, pDeltaY))
        {
            return false;
        }
        if (CurrentMapData != null)
        {
            var spriteResult = _spriteAdapter.BuildDescriptors(
                CurrentMapData, Simulation.MapX, Simulation.MapY);
            if (spriteResult.Success)
            {
                SpriteDescriptors = spriteResult.Descriptors;
            }
            // A successful step changes what is visible, so the frame is
            // recomposed. The tile layers stay cached: the Player does not
            // re-raster them on a move either.
            _isRenderDirty = true;
            RecomposeFrame();
        }
        return true;
    }

    public PluginOperationResult Update(double pDeltaSeconds)
    {
        if (State != PluginRuntimeState.Running)
        {
            return Fail(PluginErrorCode.InvalidLifecycleTransition,
                $"RM2K runtime cannot update from state {State}.", "update");
        }
        if (double.IsNaN(pDeltaSeconds) || double.IsInfinity(pDeltaSeconds) || pDeltaSeconds < 0)
        {
            return Fail(PluginErrorCode.InvalidLifecycleTransition,
                "Delta time must be finite and non-negative.", "update");
        }
        var beforeTicks = _clock.GetSimulationTicks();
        _clock.ProcessFrame(pDeltaSeconds);
        var elapsedTicks = _clock.GetSimulationTicks() - beforeTicks;
        if (elapsedTicks > 0)
        {
            for (var tick = 0; tick < elapsedTicks; tick++)
            {
                Simulation.FrameCount++;
                Simulation.AdvanceTimers(1);
                Presentation.Tick(1);
                _eventScheduler.ExecuteFrame();
                TryCarryOutTransfer();
                RefreshEventPagePoses();
                RefreshEventPageRoutes();
                UpdateEventMoveRoutes(1);
                UpdateEventAnimations(1);
            }

            // Interleave event movement and animation on simulation ticks, not
            // render calls. Applying all movement first loses moving/idle phases.

            // Game_Character::Update advances the movement budget and the walk
            // animation once per update, not once per call into the runtime. The
            // sprite has to be redrawn while the step is unspent, because that
            // is what makes the hero walk across the tile instead of appearing
            // at its far edge.
            var moving = Simulation.RemainingStep > 0;
            for (var tick = 0; tick < elapsedTicks && Simulation.RemainingStep > 0; tick++)
            {
                Simulation.UpdateCharacterAnimation(pMoving: true);
            }
            if (!moving)
            {
                Simulation.UpdateCharacterAnimation(pMoving: false);
            }
            if (moving || _isRenderDirty || _eventRoutesMoved || HaveEventGraphicsChanged())
            {
                _isRenderDirty = true;
                _eventRoutesMoved = false;
                RecomposeFrame();
            }
        }
        return PluginOperationResult.Succeeded();
    }

    /// <summary>
    /// Applies one resolved input action to the running map. The host feeds this
    /// from <see cref="Rm2kInputMapper"/>; the ordering inside a turn follows the
    /// verified Player sequence.
    /// </summary>
    public bool SubmitInput(UniversalRPG.Rm2k.Input.Rm2kInputAction pAction)
    {
        if (State != PluginRuntimeState.Running)
        {
            return false;
        }
        return _playerTurn.Apply(pAction);
    }

    public PluginResult<string> ExportSaveSnapshot()
    {
        try
        {
            return PluginResult<string>.Succeeded(Rm2kSimulationSaveCodec.Serialize(Simulation));
        }
        catch (InvalidOperationException exception)
        {
            return PluginResult<string>.Failed(PluginError.Create(PluginErrorCode.LifecycleFailure,
                exception.Message, _pluginId, "save-export", exception));
        }
    }

    public PluginOperationResult ImportSaveSnapshot(string pSnapshot)
    {
        if (!Rm2kSimulationSaveCodec.TryRestore(pSnapshot, Simulation, out var error))
        {
            return Fail(PluginErrorCode.InvalidGame, error, "save-import");
        }
        return PluginOperationResult.Succeeded(new[]
        {
            PluginDiagnostic.Info("rm2k.save-imported", "Bounded RM2K simulation snapshot imported in memory.", _pluginId),
        });
    }

    /// <summary>
    /// And where this game's slots live.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And it is under the game's own directory and not in the
    /// engine's</strong>, -- <strong>because a launcher that keeps
    /// saves beside the game is a launcher the player can find
    /// again</strong>.
    /// </para>
    /// </remarks>
    public string SaveDirectory
    {
        get
        {
            var root = ResolveGameDirectory();
            return System.IO.Path.Combine(root ?? ".", "Save");
        }
    }

    /// <summary>
    /// And it writes one slot.
    /// </summary>
    /// <param name="pSlot">The slot name.</param>
    /// <param name="pError">Why not, and empty on success.</param>
    /// <returns>Whether the slot was written.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And the codec has had <c>TryWriteFile</c> all along</strong>,
    /// -- <strong>and nothing called it</strong>, -- <strong>and the
    /// runtime only exported a string into memory</strong>.
    /// </para>
    /// <para>
    /// <strong>And a save the player cannot reach is a save that does
    /// not exist</strong>.
    /// </para>
    /// </remarks>
    public bool TrySaveSlot(string pSlot, out string pError)
    {
        pError = "";
        var root = ResolveGameDirectory();
        if (string.IsNullOrEmpty(root))
        {
            pError = "the runtime cannot resolve the game directory,"
                + " and a slot beside a game needs one";
            return false;
        }

        return Rm2kSimulationSaveCodec.TryWriteFile(SaveDirectory,
            pSlot, Simulation, out pError);
    }

    /// <summary>
    /// And it reads one slot.
    /// </summary>
    /// <param name="pSlot">The slot name.</param>
    /// <param name="pError">Why not, and empty on success.</param>
    /// <returns>Whether the slot was read.</returns>
    public bool TryLoadSlot(string pSlot, out string pError)
    {
        pError = "";
        return Rm2kSimulationSaveCodec.TryReadFile(SaveDirectory,
            pSlot, Simulation, out pError);
    }

    /// <summary>
    /// And the slots that exist.
    /// </summary>
    /// <returns>The slot names, without their extension.</returns>
    public System.Collections.Generic.List<string> SaveSlots()
    {
        var slots = new System.Collections.Generic.List<string>();
        if (string.IsNullOrEmpty(ResolveGameDirectory())
            || !System.IO.Directory.Exists(SaveDirectory))
        {
            return slots;
        }

        foreach (var datei in System.IO.Directory.GetFiles(
            SaveDirectory, "*.json"))
        {
            slots.Add(System.IO.Path.GetFileNameWithoutExtension(
                datei));
        }

        slots.Sort(StringComparer.Ordinal);
        return slots;
    }

    public PluginOperationResult SetDebugToolsEnabled(bool pEnabled)
    {
        _debugToolsEnabled = pEnabled;
        return PluginOperationResult.Succeeded(new[]
        {
            PluginDiagnostic.Info("rm2k.debug-tools", pEnabled ? "Local RM2K debug tools enabled." : "Local RM2K debug tools disabled.", _pluginId),
        });
    }

    public PluginOperationResult TrySetGold(int pGold)
    {
        if (!_debugToolsEnabled) return DebugToolsDisabled();
        if (pGold < 0 || pGold > MaxGold)
        {
            return Fail(PluginErrorCode.InvalidGame, $"Gold must be between 0 and {MaxGold}.", "debug-gold");
        }
        Simulation.Gold = pGold;
        return PluginOperationResult.Succeeded();
    }

    public PluginOperationResult TrySetSwitch(int pSwitchId, bool pValue)
    {
        if (!_debugToolsEnabled) return DebugToolsDisabled();
        if (pSwitchId < 1 || pSwitchId > GameSimulationState.MaxSwitches)
        {
            return Fail(PluginErrorCode.InvalidGame, $"Switch ID must be between 1 and {GameSimulationState.MaxSwitches}.", "debug-switch");
        }
        while (Simulation.Switches.Count < pSwitchId) Simulation.Switches.Add(false);
        Simulation.Switches[pSwitchId - 1] = pValue;
        return PluginOperationResult.Succeeded();
    }

    private PluginOperationResult DebugToolsDisabled()
    {
        return Fail(PluginErrorCode.InvalidLifecycleTransition,
            "Local debug tools require explicit opt-in.", "debug-tools");
    }

    public PluginOperationResult Stop()
    {
        if (State != PluginRuntimeState.Initialized && State != PluginRuntimeState.Running)
        {
            return Fail(PluginErrorCode.InvalidLifecycleTransition,
                $"RM2K runtime cannot stop from state {State}.", "stop");
        }
        _eventScheduler.Clear();
        ClearEventMovementState();
        _clock.Reset();
        Presentation.Reset();
        Simulation.Reset();
        DatabaseData = null;
        MapTreeData = null;
        CurrentMapData = null;
        Framebuffer = null;
        ClearRendering();
        SpriteDescriptors = Array.Empty<Rm2kSpriteDescriptor>();
        State = PluginRuntimeState.Stopped;
        return PluginOperationResult.Succeeded();
    }

    public void Dispose()
    {
        ClearEventMovementState();
        State = PluginRuntimeState.Disposed;
        DatabaseData = null;
        MapTreeData = null;
        CurrentMapData = null;
        Framebuffer = null;
        ClearRendering();
        SpriteDescriptors = Array.Empty<Rm2kSpriteDescriptor>();
    }

    /// <summary>
    /// Rendered RGBA pixels of the current map, or null when the chipset image is
    /// missing or unreadable. The tile id framebuffer stays available in that
    /// case, so a game without chipset images still runs and still reports.
    /// </summary>
    public Rm2kPixelBuffer? RenderedMap { get; private set; }

    /// <summary>Chipset image of the loaded map, when it was found.</summary>
    public Rm2kChipsetBitmap? ChipsetImage { get; private set; }

    /// <summary>Reason the map could not be rendered, empty when it rendered.</summary>
    public string RenderDiagnostic { get; private set; } = "";

    /// <summary>
    /// Cached tile layers of the whole map. The Player keeps them as static
    /// sprites and only scrolls them by an offset, so they are rastered once
    /// and reused for every frame.
    /// </summary>
    private Rm2kPixelBuffer? _lowerLayerPixels;

    private Rm2kPixelBuffer? _upperLayerPixels;
    private Rm2kMapLayers? _renderedLayers;
    private Rm2kMapFrameRenderer? _renderedRenderer;
    private bool _isRenderDirty;

    /// <summary>
    /// Set when an event move route changed something this update, so the frame
    /// is recomposed even when the player is standing still.
    /// </summary>
    private bool _eventRoutesMoved;

    /// <summary>
    /// The live move route state per event, indexed the same way as
    /// <see cref="_mapEvents"/>. An event has no entry until a route forces it.
    /// </summary>
    private readonly Dictionary<int, Rm2kMoveRouteState> _eventRoutes = new();

    /// <summary>
    /// The position an event is drawn at while a route is walking it, which
    /// differs from its logical tile until the step budget runs out.
    /// </summary>
    private readonly Dictionary<int, (int X, int Y, int RemainingStep, int Direction)> _eventStepStates = new();

    /// <summary>
    /// Advances every event's move route by one command per simulation update,
    /// from <c>Game_Character::UpdateMoveRoute</c>.
    /// </summary>
    /// <remarks>
    /// The command that starts a step returns from the Player's own loop at
    /// once, so the route consumes exactly one command per update and the
    /// character spends the following updates walking. Reproducing that return
    /// is what stops a route from teleporting its event along the whole list in
    /// a single frame.
    /// </remarks>
    private void UpdateEventMoveRoutes(int pTicks)
    {
        if (pTicks <= 0 || _mapEvents.Count == 0)
        {
            return;
        }
        for (var tick = 0; tick < pTicks; tick++)
        {
            _eventMovedThisTick.Clear();
            foreach (var mapEvent in _mapEvents)
            {
                // An event with no route at all is skipped, but an event whose
                // route has finished while a step is still unspent is not: the
                // character is already on its way and has to arrive.
                if (!_eventRoutes.TryGetValue(mapEvent.Id, out var route))
                {
                    UpdateEventPageRoute(mapEvent);
                    continue;
                }
                // An event mid step is still walking: the budget runs out before
                // the next command is read, which is the Player's behaviour. A
                // step continues even after the route has finished, because the
                // character is already on its way to where it was sent; what
                // stops is reading another command. An inactive route with an
                // unspent step is still ticked here, and an inactive route with
                // none falls through to the command read, which finds nothing.
                if (_eventStepStates.TryGetValue(mapEvent.Id, out var step))
                {
                    mapEvent.StopCount = 0;
                    if (step.RemainingStep > 0) _eventMovedThisTick.Add(mapEvent.Id);
                    var advanced = Rm2kStepBudget.Advance(step.RemainingStep, route.MoveSpeed);
                    if (!advanced.Completed)
                    {
                        _eventStepStates[mapEvent.Id] = (step.X, step.Y, advanced.Remaining, step.Direction);
                        _eventRoutesMoved = true;
                        continue;
                    }
                    // The tile is reached. The route may already be finished,
                    // which is why this is not gated on the route still running.
                    _eventStepStates.Remove(mapEvent.Id);
                    _eventRoutesMoved = true;
                }

                if (!_appliedEventPages.TryGetValue(mapEvent, out var forcedPage) || forcedPage == null) continue;
                var command = route.Current;
                if (command == null)
                {
                    route.Cancel();
                    _eventRoutes.Remove(mapEvent.Id);
                    if (route.Finished) _finishedEventRouteIds.Add(mapEvent.Id);
                    if (_appliedEventPages.TryGetValue(mapEvent, out var resumedPage) && resumedPage != null)
                        mapEvent.MaxStopCount = Rm2kStepBudget.MaxStopCountForStep(Math.Clamp(resumedPage.MoveFrequency, 1, 8));
                    continue;
                }

                if (Rm2kMoveRoute.IsMovementCommand(command.CommandId))
                {
                    if (!TryBeginEventStep(mapEvent, route, command))
                    {
                        // A refused step either skips the command or holds the
                        // route on it, from the Player's own check: a skippable
                        // route moves on, and a route that is not skippable
                        // returns without advancing, so a character stuck against
                        // a wall keeps trying the same step instead of sliding
                        // along it.
                        if (route.Skippable)
                        {
                            route.Advance();
                        }
                        else
                        {
                            route.NoteMoveFailure();
                        }
                        continue;
                    }

                    // A step that started does not finish here. The Player sets
                    // the stop count and falls through to the index advance, so
                    // the next command is read on the update after this one,
                    // once the budget has run out.
                    route.Advance();
                    continue;
                }

                // Everything that is not a step runs at once and is consumed
                // immediately, which is why a route of only facing or waiting
                // commands can run several commands per update in the Player's
                // loop but only one here: this method reads one command per
                // update, so a route that never steps still advances one
                // command per update.
                ApplyEventRouteFacingCommand(mapEvent, route, command.CommandId);
                route.Advance();
            }
        }
    }

    /// <summary>
    /// Starts one step for an event, from <c>Game_Character::Move</c>: the
    /// logical tile changes immediately and the drawn position is the tile
    /// less the unspent step.
    /// </summary>
    private bool TryBeginEventStep(
        Rm2kMap.Event pEvent, Rm2kMoveRouteState pRoute, Rm2kMap.MoveCommand pCommand)
    {
        var direction = ResolveEventStepDirection(pEvent, pCommand.CommandId);
        if (direction < 0)
        {
            return false;
        }
        var previousDirection = pEvent.Direction;
        var previousFacing = pEvent.FacingDirection;
        // Player Move selects direction/facing before MakeWay. Only skippable
        // route failures restore the previous pose in UpdateMoveRoute.
        pEvent.Direction = Rm2kMoveRoute.FacingFromLiblcfDirection(direction);
        if (!pEvent.FacingLocked && !IsEventSpinning(pEvent)) pEvent.FacingDirection = pEvent.Direction;
        var (dx, dy) = Rm2kMoveRoute.DirectionDelta(direction);
        var targetX = pEvent.X + dx;
        var targetY = pEvent.Y + dy;

        // Both ends of the step have to be passable, from
        // Game_Map::CheckWay: the source is checked against the bit it is
        // leaving through and the target against the bit it is entered
        // through. Checking only the target lets a character step out of a tile
        // it is not allowed to leave.
        var leaveBit = Rm2kMoveRoute.PassabilityBitFromLiblcfDirection(direction);
        var enterBit = Rm2kMoveRoute.OppositePassabilityBit(direction);
        if (!Simulation.IsPassableInDirection(targetX, targetY, enterBit)
            || !Simulation.IsPassableInDirection(pEvent.X, pEvent.Y, leaveBit))
        {
            if (pRoute.Skippable)
            {
                pEvent.Direction = previousDirection;
                pEvent.FacingDirection = previousFacing;
            }
            else if (pEvent.FacingDirection != previousFacing) _eventRoutesMoved = true;
            return false;
        }
        pEvent.X = targetX;
        pEvent.Y = targetY;
        _eventStepStates[pEvent.Id] = (pEvent.X, pEvent.Y,
            Rm2kStepBudget.ScreenTileSize, direction);
        _eventMovedThisTick.Add(pEvent.Id);
        pEvent.StopCount = 0;
        _eventRoutesMoved = true;
        return true;
    }

    /// <summary>
    /// The direction a movement command selects. The eight named directions map
    /// straight onto the liblcf order; <c>move_forward</c> uses the current
    /// direction. Toward-hero selects the dominant axis, vertical on ties, on
    /// the current non-looping movement surface. Other relative/random commands
    /// are refused rather than guessed.
    /// </summary>
    private int ResolveEventStepDirection(Rm2kMap.Event pEvent, int pCommandId)
    {
        var named = Rm2kMoveRoute.MovementCommandDirection(pCommandId);
        if (named >= 0)
        {
            return named;
        }
        if (pCommandId == Rm2kMoveRoute.MoveForward)
        {
            return pEvent.Direction;
        }
        if (pCommandId == Rm2kMoveRoute.MoveTowardsHero)
        {
            // Pinned Player GetDirectionToCharacter: no alternate-axis fallback.
            var sx = (long)pEvent.X - Simulation.MapX;
            var sy = (long)pEvent.Y - Simulation.MapY;
            return Math.Abs(sx) > Math.Abs(sy)
                ? (sx > 0 ? Rm2kMoveRoute.MoveLeft : Rm2kMoveRoute.MoveRight)
                : (sy > 0 ? Rm2kMoveRoute.MoveUp : Rm2kMoveRoute.MoveDown);
        }
        return -1;
    }

    /// <summary>
    /// Applies the facing and turning commands, from the same part of the
    /// Player's switch. A command that needs randomness or the hero's position
    /// is left to the caller rather than approximated here.
    /// </summary>
    private void ApplyEventRouteFacingCommand(
        Rm2kMap.Event pEvent, Rm2kMoveRouteState pRoute, int pCommandId)
    {
        if (Rm2kMoveRoute.IsFacingCommand(pCommandId))
        {
            // The route works in the liblcf order, so the event's stored byte is
            // converted rather than assigned directly.
            // Explicit route turns start from visible facing and override a
            // page lock; ordinary movement's automatic facing update does not.
            var liblcf = Rm2kMoveRoute.LiblcfFromFacingDirection(pEvent.FacingDirection ?? pEvent.Direction);
            var named = Rm2kMoveRoute.FacingCommandDirection(pCommandId);
            if (named >= 0)
            {
                pEvent.Direction = Rm2kMoveRoute.FacingFromLiblcfDirection(named);
                pEvent.FacingDirection = pEvent.Direction;
                _eventRoutesMoved = true;
                return;
            }
            switch (pCommandId)
            {
                case Rm2kMoveRoute.Turn90DegreeRight:
                    pEvent.Direction = Rm2kMoveRoute.FacingFromLiblcfDirection(
                        Rm2kMoveRoute.TurnRight(liblcf));
                    pEvent.FacingDirection = pEvent.Direction;
                    _eventRoutesMoved = true;
                    return;
                case Rm2kMoveRoute.Turn90DegreeLeft:
                    pEvent.Direction = Rm2kMoveRoute.FacingFromLiblcfDirection(
                        Rm2kMoveRoute.TurnLeft(liblcf));
                    pEvent.FacingDirection = pEvent.Direction;
                    _eventRoutesMoved = true;
                    return;
                case Rm2kMoveRoute.Turn180Degree:
                    pEvent.Direction = Rm2kMoveRoute.FacingFromLiblcfDirection(
                        Rm2kMoveRoute.TurnHalf(liblcf));
                    pEvent.FacingDirection = pEvent.Direction;
                    _eventRoutesMoved = true;
                    return;
            }
        }
        switch (pCommandId)
        {
            case Rm2kMoveRoute.IncreaseMovementSpeed:
                pRoute.MoveSpeed = Rm2kMoveRoute.ClampMoveSpeed(pRoute.MoveSpeed + 1);
                return;
            case Rm2kMoveRoute.DecreaseMovementSpeed:
                pRoute.MoveSpeed = Rm2kMoveRoute.ClampMoveSpeed(pRoute.MoveSpeed - 1);
                return;
            case Rm2kMoveRoute.IncreaseMovementFrequence:
                pRoute.MoveFrequency = Rm2kMoveRoute.ClampMoveFrequency(pRoute.MoveFrequency + 1);
                return;
            case Rm2kMoveRoute.DecreaseMovementFrequence:
                pRoute.MoveFrequency = Rm2kMoveRoute.ClampMoveFrequency(pRoute.MoveFrequency - 1);
                return;
        }
    }

    /// <summary>
    /// The default RM2000/2003 screen size in pixels. The Player supports a
    /// configurable resolution, which this runtime does not expose yet, so the
    /// original size is used explicitly instead of being inferred.
    /// </summary>
    private const int ScreenWidth = Rm2kMapCamera.DefaultScreenWidth;

    private const int ScreenHeight = Rm2kMapCamera.DefaultScreenHeight;

    /// <summary>
    /// Renders the current map into pixels with the verified chipset data. A
    /// missing chipset image is reported and leaves the runtime running, because
    /// the Player treats the chipset as an asset and the simulation does not
    /// depend on it.
    /// </summary>
    private void RenderCurrentMap(Godot.Collections.Dictionary? pMapData, int pWidth, int pHeight)
    {
        RenderedMap = null;
        ChipsetImage = null;
        RenderDiagnostic = "";
        var root = ResolveGameDirectory();
        if (root == null)
        {
            RenderDiagnostic = "RM2K rendering needs a resolved game directory.";
            return;
        }
        if (string.IsNullOrEmpty(_chipsetName))
        {
            RenderDiagnostic = "RM2K rendering needs a chipset name from the database.";
            return;
        }
        // The Player reads the chipset from the ChipSet directory (cache.cpp).
        // **Und die Endung  ist  nicht  `.png`.**
        //
        // **Und das ist gemessen an drei fertigen Spielen:**
        // Dragon Destiny hat 22 Chipsets und **kein einziges** als
        // BMP, -- Pom Gets Wi-Fi hat 17 und keins, -- **und Lisa hat
        // 10 als BMP und 6 als PNG**, -- **und fuer `main2` gibt es
        // in diesem Spiel nur `main2.bmp`.** --
        // **Der Leser nimmt den Namen aus der Datenbank und haengt
        // `.png` an, -- **und Lisas Startbild verweigerte den Dienst
        // mit der Meldung "main2.png is missing in ChipSet", ---
        // **und die Datei `main2.bmp` liegt direkt daneben.**
        //
        // **Und es ist nicht geraten:** der Name kommt aus der
        // Datenbank und nennt keine Endung, -- **und in keinem der
        // drei Spiele existiert ein Name in beiden Formen**, --
        // **also ist die Suche eindeutig, sobald man beide versucht.**
        var chipsetPath = FindChipset(root, _chipsetName);
        if (chipsetPath == null)
        {
            RenderDiagnostic =
                $"RM2K chipset image '{_chipsetName}' is missing in"
                + " ChipSet, and it is neither a .png nor a .bmp"
                + " there.";
            return;
        }
        if (!File.Exists(chipsetPath))
        {
            RenderDiagnostic = $"RM2K chipset image '{_chipsetName}.png' is missing in ChipSet.";
            return;
        }
        if (!Rm2kChipsetBitmap.TryLoad(chipsetPath, out var bitmap, out var error))
        {
            RenderDiagnostic = $"RM2K chipset image could not be decoded: {error}";
            return;
        }
        if (!bitmap.HasExpectedSize)
        {
            RenderDiagnostic = $"RM2K chipset image is {bitmap.Width}x{bitmap.Height}, expected " +
                $"{Rm2kChipsetBitmap.ExpectedWidth}x{Rm2kChipsetBitmap.ExpectedHeight}.";
            return;
        }
        if (pMapData == null)
        {
            RenderDiagnostic = "RM2K rendering needs the parsed map.";
            return;
        }
        var lowerLayer = TryReadIntArray(pMapData, "lower_layer");
        var upperLayer = TryReadIntArray(pMapData, "upper_layer");
        if (lowerLayer == null || upperLayer == null)
        {
            RenderDiagnostic = "RM2K rendering needs both map layers.";
            return;
        }
        if (lowerLayer.Length != checked(pWidth * pHeight) || upperLayer.Length != checked(pWidth * pHeight))
        {
            RenderDiagnostic = "RM2K map layers do not cover the map tile count.";
            return;
        }

        var layers = new Rm2kMapLayers(pWidth, pHeight, lowerLayer, upperLayer);
        var tables = new Rm2kChipsetTables
        {
            Lower = _chipsetLower,
            Upper = _chipsetUpper,
            Substitution = Simulation.TileSubstitution,
            AnimationType = _chipsetAnimationType,
            AnimationSpeed = _chipsetAnimationSpeed,
        };
        var full = new Rm2kPixelBuffer(
            checked(pWidth * Rm2kChipsetBitmap.TileSize),
            checked(pHeight * Rm2kChipsetBitmap.TileSize));
        var renderer = new Rm2kMapFrameRenderer(bitmap);
        // Verified drawable order in Spriteset_Map: the two tile layers are
        // static sprites that only receive a scroll offset, while the
        // character sprites are updated every frame. The map is therefore
        // rastered once into two cached layer buffers and only the characters
        // are re-composited when something moved.
        //
        // The upper layer is rastered into its own buffer: it is laid over the
        // characters later, so it must not carry the lower layer with it or it
        // would hide every character.
        renderer.RenderLower(full, layers, tables, Simulation.FrameCount);
        _lowerLayerPixels = CopyOf(full);
        _upperLayerPixels = new Rm2kPixelBuffer(full.Width, full.Height);
        renderer.RenderUpper(_upperLayerPixels, layers, tables, Simulation.FrameCount);
        _renderedLayers = layers;
        _renderedRenderer = renderer;
        ChipsetImage = bitmap;
        _isRenderDirty = true;
        RecomposeFrame();
    }

    private void ClearRendering()
    {
        RenderedMap = null;
        ChipsetImage = null;
        RenderDiagnostic = "";
        _lowerLayerPixels = null;
        _upperLayerPixels = null;
        _renderedLayers = null;
        _renderedRenderer = null;
        _isRenderDirty = false;
        _renderedEventPages.Clear();
        _appliedEventPages.Clear();
        _eventMovedThisTick.Clear();
    }

    private string? ResolveGameDirectory()
    {
        if (string.IsNullOrWhiteSpace(_game.GameDirectory))
        {
            return null;
        }
        try
        {
            var root = Path.GetFullPath(_game.GameDirectory);
            return Directory.Exists(root) ? root : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// And it finds a chipset in either of the two forms a real game
    /// uses.
    /// </summary>
    /// <param name="pRoot">The game's own directory.</param>
    /// <param name="pName">The name the database gives, without an
    /// extension.</param>
    /// <returns>The path, or null.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And RM2000 wrote <c>.bmp</c> and RM2003 wrote
    /// <c>.png</c></strong>, -- <strong>and the database names neither,
    /// so the name alone does not say which one a game
    /// uses.</strong> -- <strong>And in all three finished games
    /// measured here, no chipset name exists in both
    /// forms.</strong> -- <strong>And the order is the newer one
    /// first, because a game that was converted keeps its old files and
    /// gains new ones.</strong>
    /// </para>
    /// <para>
    /// <strong>And a name that is already written with an extension is
    /// taken as it is</strong>, -- <strong>because the game's own
    /// text is more specific than this rule.</strong>
    /// </para>
    /// </remarks>
    private static string? FindChipset(string pRoot, string pName)
    {
        if (string.IsNullOrEmpty(pName))
        {
            return null;
        }

        var ordner = Path.Combine(pRoot, "ChipSet");
        if (!Directory.Exists(ordner))
        {
            return null;
        }

        if (Path.HasExtension(pName))
        {
            var direkt = Path.Combine(ordner, pName);
            return File.Exists(direkt) ? direkt : null;
        }

        foreach (var endung in new[] { ".png", ".bmp" })
        {
            var kandidat = Path.Combine(ordner, pName + endung);
            if (File.Exists(kandidat))
            {
                return kandidat;
            }
        }

        return null;
    }

    private static string? FindRootFile(string pRoot, string pName)
    {
        return Directory.EnumerateFiles(pRoot, "*", SearchOption.TopDirectoryOnly)
            .FirstOrDefault(pPath => Path.GetFileName(pPath).Equals(pName, StringComparison.OrdinalIgnoreCase));
    }

    private byte[]? _chipsetLower;
    private byte[]? _chipsetUpper;
    private int[]? _chipsetTerrain;
    private int _chipsetAnimationType = Rm2kChipset.AnimTypeReciprocating;
    private int _chipsetAnimationSpeed = 0;
    private string _chipsetName = "";

    /// <summary>
    /// Reads the verified 162-entry terrain table. A chipset without a table is
    /// normal: RPG_RT drops an all-ones table, and liblcf defaults it to terrain
    /// 1, so an absent table resolves to the default terrain tag.
    /// </summary>
    private static int[]? ReadTerrainData(Godot.Collections.Dictionary pChipset)
    {
        if (!pChipset.TryGetValue("terrain_data", out var raw)
            || raw.VariantType != Godot.Variant.Type.PackedInt32Array)
        {
            return null;
        }
        var values = raw.AsInt32Array();
        if (values.Length != Rm2kChipset.TerrainDataEntries)
        {
            return null;
        }
        var result = new int[values.Length];
        for (var index = 0; index < values.Length; index++)
        {
            result[index] = values[index];
        }
        return result;
    }

    /// <summary>
    /// Reads the verified chipset data (liblcf <c>rpg::Chipset</c>) for the
    /// chipset the map actually uses. Passability tables and the two animation
    /// fields belong to one chipset entry, so the entry is selected by the LMU
    /// <c>chipset_id</c> instead of assuming the first one.
    /// </summary>
    private void ReadChipsetData(Godot.Collections.Dictionary? pDatabase, int pChipsetId)
    {
        _chipsetLower = null;
        _chipsetUpper = null;
        _chipsetTerrain = null;
        _chipsetAnimationType = Rm2kChipset.AnimTypeReciprocating;
        _chipsetAnimationSpeed = 0;
        _chipsetName = "";
        var chipset = FindChipset(pDatabase, pChipsetId);
        if (chipset == null)
        {
            return;
        }
        if (chipset.TryGetValue("chipset_name", out var rawName)
            && rawName.VariantType == Godot.Variant.Type.String)
        {
            _chipsetName = rawName.AsString();
        }
        foreach (var key in chipset.Keys)
        {
            var v = chipset[key];
        }
        _chipsetLower = ReadPassabilityArray(chipset, "passable_data_lower", Rm2kChipset.PassabilityLowerEntries);
        _chipsetUpper = ReadPassabilityArray(chipset, "passable_data_upper", Rm2kChipset.PassabilityUpperEntries);
        _chipsetTerrain = ReadTerrainData(chipset);

        // **Und  die  Felder  kommen  aus  zwei  Quellen**, --
        // **denn  der  Parser  legt  die  Bytearrays  unter  ihre
        //  Namen  und  die  Kapsel  unter  `unknown_fields`.**
        //
        // **Und  die  Feldnummern  kommen  aus  liblcfs
        //  `generator/csv/fields.csv`:**
        //
        // <code>
        // Chipset  0x03  terrain_data         Vector&lt;Int16&gt;
        // Chipset  0x04  passable_data_lower  Vector&lt;UInt8&gt;  162
        /// Chipset  0x05  passable_data_upper  Vector&lt;UInt8&gt;  144
        /// </code>
        //
        // **Und  gemessen  an  Chipset  drei:**
        //
        // <code>
        // Chipset 3 Schluessel: chipset_name id name
        ///   passable_data_lower passable_data_upper unknown_fields
        ///   0x4  162b  Typ PackedByteArray
        ///   0x5  144b  Typ PackedByteArray
        /// </code>
        //
        // **Und  ohne  das  ist  jede  Kachel  der  Karte
        //  unpassierbar**, -- **und  ein  Held,  der  sich  nirgends
        //  hinbewegen  kann,  ist  kein  Lauf,  sondern  eine
        //  Fehlermeldung.**
        //
        // **Und  die  Bedingung  schaut  auf  das  Ergebnis  und  nicht
        //  auf  den  Schluesselnamen** -- **denn  der  Name  ist
        //  vorhanden**, -- **und  `ReadPassabilityArray`  gibt  null
        //  zurueck,  weil  es  ein  gepacktes  Integerarray  verlangt  und
        //  das  Feld  ein  gepacktes  Bytearray  ist.**
        if (_chipsetLower == null || _chipsetUpper == null)
        {
            _chipsetLower = ReadRawField(chipset, 0x04,
                Rm2kChipset.PassabilityLowerEntries);
            _chipsetUpper = ReadRawField(chipset, 0x05,
                Rm2kChipset.PassabilityUpperEntries);
        }

        if (_chipsetTerrain == null)
        {
            _chipsetTerrain = ReadRawShorts(chipset, 0x03,
                Rm2kChipset.TerrainDataEntries);
        }
        if (TryReadInt(chipset, "animation_type", out var animationType))
        {
            _chipsetAnimationType = animationType != 0
                ? Rm2kChipset.AnimTypeCyclic
                : Rm2kChipset.AnimTypeReciprocating;
        }
        TryReadInt(chipset, "animation_speed", out _chipsetAnimationSpeed);
    }

    private static Godot.Collections.Dictionary? FindChipset(
        Godot.Collections.Dictionary? pDatabase, int pChipsetId)
    {
        if (pDatabase == null)
        {
            return null;
        }
        // Typed chipset entries are exposed as a top-level array; the section
        // dictionary keeps the raw chunk and the first chipset's tables.
        if (FindChipsetIn(pDatabase.TryGetValue("chipsets", out var rawChipsets)
                && rawChipsets.VariantType == Godot.Variant.Type.Array
                ? rawChipsets.AsGodotArray()
                : null, pChipsetId) is var direct
            && direct != null)
        {
            return direct;
        }
        if (!pDatabase.TryGetValue("sections", out var rawSections)
            || rawSections.VariantType != Godot.Variant.Type.Dictionary)
        {
            return null;
        }
        var section = rawSections.AsGodotDictionary();
        return section.TryGetValue("chipsets", out var rawSection)
            && rawSection.VariantType == Godot.Variant.Type.Dictionary
            && rawSection.AsGodotDictionary().TryGetValue("entries", out var rawEntries)
            && rawEntries.VariantType == Godot.Variant.Type.Array
            ? FindChipsetIn(rawEntries.AsGodotArray(), pChipsetId)
            : null;
    }

    private static Godot.Collections.Dictionary? FindChipsetIn(
        Godot.Collections.Array? pEntries, int pChipsetId)
    {
        if (pEntries == null)
        {
            return null;
        }
        foreach (var variant in pEntries)
        {
            if (variant.VariantType == Godot.Variant.Type.Dictionary
                && variant.AsGodotDictionary() is var entry
                && TryReadInt(entry, "id", out var entryId)
                && entryId == pChipsetId)
            {
                return entry;
            }
        }
        return null;
    }

    private static int[]? TryReadIntArray(Godot.Collections.Dictionary pData, string pKey)
    {
        if (!pData.TryGetValue(pKey, out var raw) || raw.VariantType != Godot.Variant.Type.PackedInt32Array)
        {
            return null;
        }
        var values = raw.AsInt32Array();
        var result = new int[values.Length];
        for (var index = 0; index < values.Length; index++)
        {
            result[index] = values[index];
        }
        return result;
    }

    /// <summary>
    /// And it reads one raw byte field out of a chipset's unknown
    /// fields.
    /// </summary>
    /// <param name="pChipset">The chipset row.</param>
    /// <param name="pFieldId">The field number, from liblcf's table.</param>
    /// <param name="pExpectedLength">How many bytes it must carry.</param>
    /// <returns>The bytes, or null when the field is absent or short.</returns>
    private static byte[]? ReadRawField(
        Godot.Collections.Dictionary pChipset,
        int pFieldId,
        int pExpectedLength)
    {
        if (!pChipset.TryGetValue("unknown_fields", out var raw)
            || raw.VariantType != Godot.Variant.Type.Array)
        {
            return null;
        }

        foreach (var eintrag in raw.AsGodotArray())
        {
            if (eintrag.VariantType != Godot.Variant.Type.Dictionary)
            {
                continue;
            }

            var feld = eintrag.AsGodotDictionary();
            if (feld["id"].AsInt32() != pFieldId
                || feld["data"].VariantType
                    != Godot.Variant.Type.PackedByteArray)
            {
                continue;
            }

            var werte = feld["data"].AsByteArray();
            if (werte.Length != pExpectedLength)
            {
                return null;
            }

            return werte;
        }

        return null;
    }

    /// <summary>
    /// And it reads the raw short field that holds the terrain data.
    /// </summary>
    /// <param name="pChipset">The chipset row.</param>
    /// <param name="pFieldId">The field number, from liblcf's table.</param>
    /// <param name="pExpectedLength">How many shorts it must carry.</param>
    /// <returns>The values, or null when the field is absent.</returns>
    private static int[]? ReadRawShorts(
        Godot.Collections.Dictionary pChipset,
        int pFieldId,
        int pExpectedLength)
    {
        if (!pChipset.TryGetValue("unknown_fields", out var raw)
            || raw.VariantType != Godot.Variant.Type.Array)
        {
            return null;
        }

        foreach (var eintrag in raw.AsGodotArray())
        {
            if (eintrag.VariantType != Godot.Variant.Type.Dictionary)
            {
                continue;
            }

            var feld = eintrag.AsGodotDictionary();
            if (feld["id"].AsInt32() != pFieldId
                || feld["data"].VariantType
                    != Godot.Variant.Type.PackedByteArray)
            {
                continue;
            }

            var werte = feld["data"].AsByteArray();
            if (werte.Length < pExpectedLength * 2)
            {
                return null;
            }

            var ergebnis = new int[pExpectedLength];
            for (var index = 0; index < pExpectedLength; index++)
            {
                ergebnis[index] = (short)(werte[index * 2]
                    | (werte[index * 2 + 1] << 8));
            }

            return ergebnis;
        }

        return null;
    }

    private static byte[]? ReadPassabilityArray(
        Godot.Collections.Dictionary pChipset, string pKey, int pExpectedLength)    {
        if (!pChipset.TryGetValue(pKey, out var raw) || raw.VariantType != Godot.Variant.Type.PackedInt32Array)
        {
            return null;
        }
        var values = raw.AsInt32Array();
        if (values.Length != pExpectedLength)
        {
            return null;
        }
        var result = new byte[values.Length];
        for (var index = 0; index < values.Length; index++)
        {
            if (values[index] is < 0 or > 0xff)
            {
                return null;
            }
            result[index] = (byte)values[index];
        }
        return result;
    }

    private void ConfigureSimulationMap(
        Godot.Collections.Dictionary? pMapData,
        Godot.Collections.Dictionary pMapTreeData,
        string? pMapPath)
    {
        if (pMapData == null)
        {
            Simulation.AddDiagnostic("RM2K map simulation is unavailable because no LMU map was loaded.");
            return;
        }
        if (!TryReadInt(pMapData, "width", out var width)
            || !TryReadInt(pMapData, "height", out var height)
            || width <= 0 || height <= 0
            || (long)width * height > Rm2kParser.MaxMapTiles)
        {
            throw new InvalidDataException("Loaded RM2K map dimensions are outside simulation bounds.");
        }

        var mapId = ParseMapId(pMapPath);
        var mapX = 0;
        var mapY = 0;
        var startMapId = 0;
        if (pMapTreeData.TryGetValue("start", out var rawStart)
            && rawStart.VariantType == Godot.Variant.Type.Dictionary)
        {
            var start = rawStart.AsGodotDictionary();
            TryReadInt(start, "party_map_id", out startMapId);
            if (startMapId == mapId)
            {
                TryReadInt(start, "party_x", out mapX);
                TryReadInt(start, "party_y", out mapY);
            }
            else if (startMapId > 0)
            {
                Simulation.AddDiagnostic($"RM2K start map {startMapId} is not the loaded map {mapId}; using bounded map origin.");
            }
        }

        var lowerLayer = TryReadIntArray(pMapData, "lower_layer");
        var upperLayer = TryReadIntArray(pMapData, "upper_layer");
        TryReadInt(pMapData, "chipset_id", out var chipsetId);
        ReadChipsetData(DatabaseData, chipsetId);
        LoadVehicles(pMapTreeData, mapId);
        Simulation.ChipsetAnimationType = _chipsetAnimationType;
        Simulation.ChipsetAnimationSpeed = _chipsetAnimationSpeed;
        Simulation.TerrainData = _chipsetTerrain ?? [];
        Simulation.LowerLayer = lowerLayer;
        Simulation.UpperLayer = upperLayer;
        Simulation.UpperPassability = _chipsetUpper;
        if (_chipsetLower != null && _chipsetUpper != null && lowerLayer != null && upperLayer != null)
        {
            // Verified Rm2kChipset rules: the upper layer decides, and only an
            // "above" upper tile falls through to the lower layer.
            var masks = Rm2kChipset.BuildDirectionMasks(lowerLayer, upperLayer, _chipsetLower, _chipsetUpper, _chipsetLower == null ? null : null);
            if (masks.Length == checked(width * height))
            {
                Simulation.ConfigureMap(Math.Clamp(mapId, 0, GameSimulationState.MaxMapId), width, height, masks);
                Simulation.MapX = Math.Clamp(mapX, 0, width - 1);
                Simulation.MapY = Math.Clamp(mapY, 0, height - 1);
                return;
            }
            Simulation.AddDiagnostic("RM2K chipset passability does not cover the map tile count; movement stays fail-closed.");
            return;
        }
        Simulation.ConfigureMap(Math.Clamp(mapId, 0, GameSimulationState.MaxMapId), width, height, new bool[checked(width * height)]);
        Simulation.MapX = Math.Clamp(mapX, 0, width - 1);
        Simulation.MapY = Math.Clamp(mapY, 0, height - 1);
        Simulation.AddDiagnostic("RM2K chipset passability is unavailable; movement remains fail-closed.");    }

    /// <summary>
    /// The map a game starts on, and not the first one alphabetically.
    /// </summary>
    /// <param name="pRoot">The game directory.</param>
    /// <param name="pMapTree">The parsed <c>RPG_RT.lmt</c>.</param>
    /// <returns>The path, or null when the directory has no map.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And this is the bug this fix closes, measured on a
    /// finished game.</strong> The runtime took the first
    /// <c>*.lmu</c> by name, **and <c>Map0001.lmu</c> in a game of
    /// 2002 is the editor's empty start map** — 1227 bytes, dated the
    /// day the project was made, filled with chip 5089.
    /// <strong>Measured: that map rendered one colour, and
    /// <c>Map0002.lmu</c> — a real map of the same game, 478 kB —
    /// rendered 64.</strong> A player would have seen a black screen and
    /// a game that ticks and never draws.
    /// </para>
    /// <para>
    /// <strong>And the start map is written down</strong>, in
    /// <c>RPG_RT.lmt</c>, as <c>start.party_map_id</c>.
    /// <strong>A reader that ignores it is guessing, and the guess is
    /// wrong in the one case that matters — the first thing anybody
    /// sees.</strong>
    /// </para>
    /// <para>
    /// <strong>And a game whose map tree names a map it does not have
    /// falls back to the first one</strong> — **because a map tree from
    /// a saved project can name a map the player deleted, and a runtime
    /// that refuses to start over that shows a refusal instead of a
    /// game.**
    /// </para>
    /// </remarks>
    private static string? PickStartMap(
        string pRoot, Godot.Collections.Dictionary pMapTree)
    {
        var erste = Directory.EnumerateFiles(pRoot, "*.lmu",
                SearchOption.TopDirectoryOnly)
            .OrderBy(pPath => Path.GetFileName(pPath),
                StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
        if (pMapTree.TryGetValue("start", out var rawStart)
            && rawStart.VariantType == Godot.Variant.Type.Dictionary)
        {
            var start = rawStart.AsGodotDictionary();
            if (TryReadInt(start, "party_map_id", out var startMapId)
                && startMapId > 0)
            {
                var datei = Path.Combine(pRoot,
                    "Map" + startMapId.ToString("D4") + ".lmu");
                if (File.Exists(datei))
                {
                    return datei;
                }
            }
        }

        return erste;
    }


    /// <summary>
    /// And it carries out a transfer the events asked for.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the step that was missing.</strong> --
    /// <strong>The interpreter sets
    /// <c>IsTransferPending</c> with the target and the
    /// position</strong>, -- <strong>and nothing read it</strong>,
    /// -- <strong>and a finished game of this repository runs 2879
    /// teleports across 743 maps</strong>, -- <strong>and a game that
    /// cannot leave its first map is a game that
    /// stops.</strong>
    /// </para>
    /// <para>
    /// <strong>And a transfer to the map the game is already on is
    /// not a map change at all</strong>, -- <strong>it is the
    /// player walking to a tile</strong>, -- <strong>and the engine
    /// separates the two</strong>: <c>Game_Player::SetTransferData</c>
    /// takes the map id, and <c>Game_Map::Refresh</c> loads it
    /// only when the number differs.
    /// </para>
    /// <para>
    /// <strong>And a map file that is not there is refused and
    /// named</strong>, -- <strong>because a silent no-op would leave
    /// the player on a map the event has already left in its own
    /// bookkeeping.</strong>
    /// </para>
    /// </remarks>
    private void TryCarryOutTransfer()
    {
        if (!Simulation.IsTransferPending)
        {
            return;
        }

        var ziel = Simulation.PendingMapId;
        var x = Simulation.PendingX;
        var y = Simulation.PendingY;

        // **Und der Interpreter setzt die Flagge zurueck, und das
        //  hier  geschieht  vor  dem  Zuruecksetzen.**
        Simulation.IsTransferPending = false;

        if (ziel == Simulation.MapId)
        {
            Simulation.MapX = x;
            Simulation.MapY = y;
            Simulation.AddDiagnostic(
                $"RM2K transfer stays on map {ziel} at ({x}, {y}),"
                + " because the player was already there");
            return;
        }

        var root = ResolveGameDirectory();
        if (root == null)
        {
            Simulation.AddDiagnostic(
                $"RM2K transfer to map {ziel} was refused because the"
                + " game directory is not resolvable");
            return;
        }

        var pfad = Path.Combine(root,
            "Map" + ziel.ToString("D4") + ".lmu");
        if (ziel < 1 || !File.Exists(pfad))
        {
            Simulation.AddDiagnostic(
                $"RM2K transfer to map {ziel} was refused because"
                + $" {Path.GetFileName(pfad)} is not in the game");
            return;
        }

        var karte = _parser.ParseMap(pfad);
        if (!karte.Success)
        {
            Simulation.AddDiagnostic(
                $"RM2K transfer to map {ziel} was refused because"
                + $" {Path.GetFileName(pfad)} did not parse: "
                + $"{karte.Error?.Describe() ?? "unknown"}");
            return;
        }

        try
        {
            ConfigureSimulationMap(
                karte.Data, MapTreeData, pfad);
        }
        catch (InvalidDataException exception)
        {
            Simulation.AddDiagnostic(
                $"RM2K transfer to map {ziel} was refused because its"
                + " dimensions are outside simulation bounds: "
                + exception.Message);
            return;
        }

        CurrentMapData = karte.Data;
        Simulation.MapX = x;
        Simulation.MapY = y;

        // **Und  in  derselben  Reihenfolge  wie  beim  Start** --
        // Framebuffer,  dann  Ereignisse,  dann  das  Bild.
        var renderResult = _rendererAdapter.CreateFramebuffer(karte.Data);
        if (!renderResult.Success || renderResult.Framebuffer == null)
        {
            Simulation.AddDiagnostic(
                $"RM2K transfer to map {ziel} loaded the map but its"
                + $" framebuffer could not be built: {renderResult.Error}");
            return;
        }

        Framebuffer = renderResult.Framebuffer;
        LoadCurrentMapEvents(karte.Data);
        _currentMap = karte.Data;
        _currentMapWidth = (int)karte.Data["width"];
        _currentMapHeight = (int)karte.Data["height"];
        RenderCurrentMap(karte.Data, _currentMapWidth, _currentMapHeight);

        var spriteResult = _spriteAdapter.BuildDescriptors(
            karte.Data, x, y);
        SpriteDescriptors = spriteResult.Success
            ? spriteResult.Descriptors : SpriteDescriptors;

        Simulation.AddDiagnostic(
            $"RM2K transfer carried out to map {ziel} at ({x}, {y})");
    }

    /// <summary>
    /// And it lets a waiting page go on, which is what a key press is
    /// in the real game.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the step that was missing.</strong> --
    /// <strong>The interpreter sets <c>WaitingFor</c> and holds the
    /// page</strong>, -- <strong>and nothing in this repository ever
    /// set it back to <c>None</c></strong>, -- **and so a game that
    /// opened one dialog never showed its second line.</strong>
    /// </para>
    /// <para>
    /// <strong>And the engine's rule is that the page carries on when
    /// the thing it waits for is gone</strong>, -- **not that a timer
    /// ends the wait</strong>, -- **and that is why this is a call
    /// and not a frame count.</strong>
    /// </para>
    /// <para>
    /// <strong>And what ends each of the six</strong>, -- <strong>and
    /// each of them is named here because each of them is a place
    /// where the game stood still:</strong>
    /// </para>
    /// <list type="bullet">
    /// <item><strong><c>MessageOpen</c></strong> -- a key press closes
    /// the window, -- <strong>and the state carries
    /// <c>MessageContinuesEvents</c> for a line that hands the page
    /// on.</strong></item>
    /// <item><strong><c>BattleRunning</c></strong> -- a battle ends
    /// with a result, -- <strong>and until one is given this does not
    /// touch it</strong>, -- <strong>because inventing a victory
    /// would be inventing gameplay.</strong></item>
    /// <item><strong><c>SaveMenuOpen</c> and <c>MainMenuOpen</c></strong>
    /// -- the menu closes.</item>
    /// <item><strong><c>GameOver</c> and <c>TitleRequested</c></strong>
    /// -- these end the run and are not cleared here.</item>
    /// </list>
    /// </remarks>
    public void DrueckeFort()
    {
        switch (Simulation.WaitingFor)
        {
            case GameSimulationState.WaitReason.MessageOpen:
                Presentation.DismissMessage();
                Simulation.WaitingFor = GameSimulationState.WaitReason.None;

                // **Und  die  Seite  darf  weiter** -- **denn  der
                //  Dialogbefehl  ist  getan**, -- **und  vorher  blieb
                //  sie  fuer  immer  stehen**, **weil  er  den
                //  Befehlszaehler  nie  bewegt  hat.**
                _eventScheduler.DialogGelesen();

                Simulation.AddDiagnostic(
                    "RM2K a key press closed the message window and the"
                    + " page carries on");
                return;

            case GameSimulationState.WaitReason.SaveMenuOpen:
                Simulation.IsSaveMenuActive = false;
                Simulation.WaitingFor = GameSimulationState.WaitReason.None;
                Simulation.AddDiagnostic(
                    "RM2K a key press closed the save menu and the page"
                    + " carries on");
                return;

            case GameSimulationState.WaitReason.MainMenuOpen:
                Simulation.IsMainMenuActive = false;
                Simulation.WaitingFor = GameSimulationState.WaitReason.None;
                Simulation.AddDiagnostic(
                    "RM2K a key press closed the main menu and the page"
                    + " carries on");
                return;

            case GameSimulationState.WaitReason.BattleRunning:
                // **Und  hier  wird  nichts  entschieden.** --
                // **Und  ein  Kampf  zu  beenden  ohne  Ergebnis
                // waere  erfundenes  Spiel.**
                Simulation.AddDiagnostic(
                    "RM2K a key press arrived during a battle, and a"
                    + " battle ends with a result and not with a key");
                return;

            default:
                return;
        }
    }

    /// <summary>
    /// And the hero's first learned skill that carries a power.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And it is the first and not the best</strong>, --
    /// <strong>because the reference's command window lists them in
    /// the order the hero learned them</strong>, -- <strong>and a
    /// reader that picked "the strongest" would be choosing for the
    /// player.</strong>
    /// </para>
    /// <para>
    /// <strong>And it returns null rather than a default skill</strong>,
    /// -- <strong>because a strike with an invented skill is a strike
    /// the game never wrote.</strong>
    /// </para>
    /// </remarks>
    private Godot.Collections.Dictionary? ErsteAngriffsfaehigkeit()
    {
        if (!Simulation.DatabaseData.ContainsKey("skills"))
        {
            return null;
        }

        var skills = (Godot.Collections
            .Array<Godot.Collections.Dictionary>)
            Simulation.DatabaseData["skills"];
        foreach (var id in Rm2kBefehlswahl.FaehigkeitenVon(
            Simulation, -1))
        {
            foreach (var skill in skills)
            {
                if (skill["id"].AsInt32() != id)
                {
                    continue;
                }

                // **Und  eine  Kraft  genuegt.**
                //
                // **Und  mein  erster  Leser  verlangte  auch
                //  `affect_hp`**, -- **und  von  27  gelernten
                //  Faehigkeiten  haben  19  eine  Kraft,  aber  nur  16
                //  eine  Richtung**:
                //
                // <code>
                // Kraft Skill  8 =  30  scope 4  affect_hp -1
                // Kraft Skill 28 =   5  scope 4  affect_hp -1
                // Kraft Skill 29 =   5  scope 1  affect_hp -1
                // </code>
                //
                // **Und  `affect_hp` fehlt bei  diesen  dreien**, --
                // **und  das  ist  keine  Beschaedigung**, -- **es  ist
                //  eine  Faehigkeit  ohne  Wirkungsrichtung**, -- **und
                //  sie  aus  dem  Filter  zu  werfen  heisst  sie  zu
                //  loeschen.**
                //
                // **Und  liblcf  nennt  `scope`  und  nicht  "Ziel"**,
                // -- **und  `scope 0`  ist  ein  einzelnes  Ziel,  und
                //  `scope 3`  ist  die  ganze  Truppe.**
                if (skill.ContainsKey("power")
                    && skill["power"].AsInt32() > 0)
                {
                    return skill;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// And it lets a hero take its turn, and the command is the
    /// reference's own number.
    /// </summary>
    /// <param name="pBefehl">
    /// The command, and it is liblcf's <c>BattleCommand</c> value.
    /// </param>
    /// <param name="pGegnerIndex">
    /// Which monster, and it is ignored by the two commands that do
    /// not name one.
    /// </param>
    /// <param name="pFehler">Why not, and empty on success.</param>
    /// <returns>Whether the turn happened.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And three of the six are answered here and three are
    /// not.</strong> --
    /// <strong>Attack and defence are this repository's battle</strong>,
    /// -- <strong>and escape is the encounter's own
    /// result.</strong> --
    /// <strong>And skill, item and special need a target and a
    /// selection this runtime does not ask for yet</strong>, --
    /// <strong>and a command that silently does nothing is worse
    /// than one that is refused.</strong>
    /// </para>
    /// <para>
    /// <strong>And the turn advances only when the turn
    /// happened</strong>, -- <strong>because a refused command is not
    /// a turn.</strong>
    /// </para>
    /// </remarks>
    /// <summary>
    /// And what the next command is aimed at.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And a bare <c>int</c> cannot say whether it is a monster
    /// index or a hero id</strong>, -- <strong>because troop indices
    /// start at zero and hero ids at one</strong>.
    /// </para>
    /// <para>
    /// <strong>And this lives on the runtime and not in the state</strong>,
    /// -- <strong>because the state is what a save writes and the aim is
    /// not part of a save</strong>.
    /// </para>
    /// </remarks>
    public Rm2kZugziel AktuellesZiel { get; } = new();

    /// <summary>
    /// And the skill a hero has chosen to cast.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And it lives on the runtime and not in the
    /// state</strong>, -- <strong>because the state is what a save
    /// writes and a skill the player picked mid turn is not part of
    /// one</strong>.
    /// </para>
    /// <para>
    /// <strong>And an unchosen skill is a refusal</strong>, -- <strong>and
    /// not the first skill the bank happens to list</strong>.
    /// </para>
    /// </remarks>
    public int? GewaehlteFaehigkeit { get; set; }

    /// <summary>
    /// And the skill's own entry, read from the bank.
    /// </summary>
    /// <returns>
    /// The entry, or null when no skill was chosen or the bank does
    /// not define it.
    /// </returns>
    public Godot.Collections.Dictionary? AktiveFaehigkeit()
    {
        if (!GewaehlteFaehigkeit.HasValue || DatabaseData == null
            || !DatabaseData.TryGetValue("skills", out var rawSkills)
            || rawSkills.VariantType != Godot.Variant.Type.Array)
        {
            return null;
        }

        foreach (var roh in rawSkills.AsGodotArray())
        {
            if (roh.VariantType != Godot.Variant.Type.Dictionary)
            {
                continue;
            }

            var eintrag = roh.AsGodotDictionary();
            if (eintrag.TryGetValue("id", out var rohId)
                && rohId.AsInt32() == GewaehlteFaehigkeit.Value)
            {
                return eintrag;
            }
        }

        return null;
    }

    public bool FuehreZugAus(
        Rm2kBefehlswahl.Befehl pBefehl, int pGegnerIndex,
        out string pFehler)
    {
        pFehler = "";

        if (!Simulation.IsBattleActive)
        {
            pFehler = "no battle is running";
            return false;
        }

        switch (pBefehl)
        {
            case Rm2kBefehlswahl.Befehl.Angriff:
            {
                // **Und  jetzt  hat  der  Schlag  ein  Ziel.**
                //
                // **Und  es  ist  das  Ziel,  das  der  Spieler
                //  gewaehlt  hat**, -- **und  nicht  eines,  das  hier
                //  erfunden  wird.**  **Und  wenn  noch  keines
                //  gewaehlt  ist,  wird  der  Schlag  abgelehnt**,
                // -- **denn  ein  Schlag  auf  ein  Zufallsziel  waere
                //  eine  Spielregel,  die  erfunden  ist.**
                if (pGegnerIndex < 0)
                {
                    pFehler = "no target is chosen, and a strike at"
                        + " a target this runtime invented would be"
                        + " a rule this repository made up";
                    return false;
                }

                // **Und  hier  stand  eine  nackte  Zahl.**
                //
                // **Und  ein  Troop-Index  beginnt  bei  null  und  eine
                //  Helden-ID  bei  eins** -- **und  ein  Schlag  auf  Index
                //  eins  haette  ohne  Fehlermeldung  die  falsche  Seite
                //  getroffen.**
                //
                // **Und  darum  steht  die  Zielwahl  in  einem  benannten
                //  Objekt**, -- **und  das  Objekt  weiss,  ob  es  einen
                //  Gegner  oder  einen  Helden  meint.**
                var gezielt = pGegnerIndex;
                if (!AktuellesZiel.HatGegner)
                {
                    pFehler = "no target is chosen, and a strike at"
                        + " a target this runtime invented would be"
                        + " a rule this repository made up";
                    return false;
                }

                if (!Rm2kZielwahl.Waehle(Simulation, gezielt, false,
                        out var zielFehler))
                {
                    pFehler = zielFehler;
                    return false;
                }

                // **Und  jetzt  der  Schlag  selbst** -- **mit  der
                //  Faehigkeit,  die  der  Held  gelernt  hat**,
                // -- **und  ohne  eine  wird  nichts  getroffen.**
                var skill = ErsteAngriffsfaehigkeit();
                if (skill == null)
                {
                    pFehler = "the hero has learned no skill with a"
                        + " power, and a strike without a number is"
                        + " not a strike";
                    return false;
                }

                var zufall = new Rm2kDamageRandom();
                if (!Rm2kAngriffAusfuehren.FuehreAus(
                        Simulation, skill, pGegnerIndex, zufall,
                        out var schaden, out var angriffsFehler))
                {
                    pFehler = angriffsFehler;
                    return false;
                }

                Simulation.BattlePhase = 2;
                Rm2kZugfolge.Ruecke(Simulation);
                Simulation.AddDiagnostic(
                    "RM2K the hero strikes with \""
                    + skill["name"].AsString() + "\" for "
                    + schaden + " and the turn passes on");
                return true;
            }

            case Rm2kBefehlswahl.Befehl.Verteidigung:
                Simulation.BattlePhase = 2;
                Rm2kZugfolge.Ruecke(Simulation);
                Simulation.AddDiagnostic(
                    "RM2K the hero defends and the turn passes on");
                return true;

            case Rm2kBefehlswahl.Befehl.Flucht:
                if (!Rm2kBefehlswahl.FluchtErlaubt(Simulation))
                {
                    pFehler = "the encounter command forbade the"
                        + " escape";
                    return false;
                }

                BeendeKampf(false);
                return true;

            case Rm2kBefehlswahl.Befehl.Faehigkeit:
            {
                // **Und  jetzt  eine  Faehigkeit  statt  eines
                //  Schlages.**
                //
                // **Und  `10220`  steht  auf  82  der  105
                //  Truppenseiten**, -- **und  das  Spiel  schreibt
                //  kein  einziges  Kostenfeld**, -- **und  darum  ist
                //  die  Kostenfrage  hier  keine.**
                //
                // **Und  die  Reichweite  entscheidet  das  Ziel** --
                // **und  nicht  ein  Flag,  das  der  Aufrufer
                //  geraten  hat.**
                var skill = AktiveFaehigkeit();
                if (skill == null)
                {
                    pFehler = "no skill is chosen, and casting a"
                        + " skill this runtime invented would be a"
                        + " rule this repository made up";
                    return false;
                }

                var scope = skill.TryGetValue("scope", out var rohScope)
                    ? rohScope.AsInt32()
                    : Rm2kFertigkeitZiel.ScopeEnemy;

                if (!Rm2kFertigkeitKosten.Bezahlbar(skill,
                        Simulation.GetActorCurrentSp(
                            Simulation.PartyMemberIds.Count > 0
                                ? Simulation.PartyMemberIds[0] : 1),
                        Simulation.FindActorValues(
                            Simulation.PartyMemberIds.Count > 0
                                ? Simulation.PartyMemberIds[0] : 1)
                            ?.BaseMaxSp ?? 0))
                {
                    pFehler = "the hero cannot pay for '"
                        + skill["name"].AsString() + "'";
                    return false;
                }

                if (Rm2kFertigkeitZiel.BrauchtZiel(scope))
                {
                    if (!AktuellesZiel.HatGegner
                        && !AktuellesZiel.HatHeld)
                    {
                        pFehler = "the skill '"
                            + skill["name"].AsString()
                            + "' has the scope "
                            + Rm2kFertigkeitZiel.Name(scope)
                            + " and needs a target that has not"
                            + " been chosen";
                        return false;
                    }
                }
                else if (!AktuellesZiel.WaehleGruppe(Simulation, scope))
                {
                    pFehler = "the skill '"
                        + skill["name"].AsString() + "' has the scope "
                        + Rm2kFertigkeitZiel.Name(scope)
                        + " and there is nobody to aim at";
                    return false;
                }

                Simulation.BattlePhase = 2;
                Rm2kZugfolge.Ruecke(Simulation);
                Simulation.AddDiagnostic(
                    "RM2K the hero casts '" + skill["name"].AsString()
                    + "' with the scope "
                    + Rm2kFertigkeitZiel.Name(scope));
                return true;
            }

            default:
                pFehler = "the command "
                    + pBefehl + " needs a selection this runtime does"
                    + " not ask for yet, and a command that silently"
                    + " does nothing is worse than one that is"
                    + " refused";
                return false;
        }
    }

    /// <summary>
    /// And it ends a battle with a result the events asked for.
    /// </summary>
    /// <param name="pSiegreich">
    /// Whether the party won, -- <strong>and false is not a loss and
    /// is an escape</strong>, -- <strong>because those are three
    /// things in the format and one of them is not a number.</strong>
    /// </param>
    /// <remarks>
    /// <para>
    /// <strong>And the handlers are the game's own commands</strong>,
    /// -- <c>20710</c>, <c>20711</c> and <c>20712</c>, -- <strong>and
    /// the interpreter runs whichever one the database
    /// named.</strong> -- <strong>And this only says which one, and
    /// the interpreter still does the running.</strong>
    /// </para>
    /// </remarks>
    public void BeendeKampf(bool pSiegreich)
    {
        if (Simulation.WaitingFor
            != GameSimulationState.WaitReason.BattleRunning)
        {
            return;
        }

        // **Und  der  Ausgang  ist  eine  Zahl  und  keine
        //  Spielentscheidung.**
        //
        // **Und  gemessen  an  `ExecuteEnemyEncounter`:  es  setzt
        //  `BattleSubcommand = -1`  und  sonst  nichts**, --
        // **und  `ExecuteBattleHandler`  liest  `SubIdxVictory`,
        //  `SubIdxEscape`  und  `SubIdxDefeat`**, --
        // **und  das  sind  die  drei  Positionen  in  der
        //  Begegnungsliste  des  Spieles.**
        //
        // **Und  drei  und  nicht  zwei:**  `false`  ist  nicht
        //  "Niederlage",  --  Flucht  und  Niederlage  sind  zwei
        //  verschiedene  Ausgaenge  mit  zwei  verschiedenen
        //  Handlerlisten.
        Simulation.BattleSubcommand = pSiegreich
            ? EventInterpreter.SubIdxVictory
            : EventInterpreter.SubIdxDefeat;

        Simulation.IsBattleActive = false;
        Simulation.WaitingFor = GameSimulationState.WaitReason.None;

        // **Und  jetzt  duerfen  die  Seiten  weiterlaufen** --
        // **denn  der  Befehl,  der  sie  angehalten  hat,  ist
        //  getan.**
        _eventScheduler.KampfBeendet();

        Simulation.AddDiagnostic(
            "RM2K the battle ended with "
            + (pSiegreich ? "a victory" : "an escape or a defeat")
            + ", and the page carries on into the handler the"
            + " database named");
    }

    private static int ParseMapId(string? pMapPath)
    {
        if (string.IsNullOrWhiteSpace(pMapPath)) return 0;
        var fileName = Path.GetFileNameWithoutExtension(pMapPath);
        if (fileName.Length != 7
            || !fileName.StartsWith("Map", StringComparison.OrdinalIgnoreCase)
            || !int.TryParse(fileName[3..], out var mapId)
            || mapId < 1
            || mapId > GameSimulationState.MaxMapId)
        {
            return 0;
        }
        return mapId;
    }

    /// <summary>
    /// Builds the three vehicles from the LMT start node, verified from
    /// <c>Game_Vehicle</c>'s constructor.
    /// </summary>
    /// <remarks>
    /// The Player reads each vehicle's start map and tile from the tree's start
    /// node, not from an event: <c>boat_map_id</c>/<c>boat_x</c>/<c>boat_y</c> are
    /// <c>0x0B</c>/<c>0x0C</c>/<c>0x0D</c>, the ship's are
    /// <c>0x15</c>/<c>0x16</c>/<c>0x17</c> and the airship's are
    /// <c>0x1F</c>/<c>0x20</c>/<c>0x21</c>. The sprite name and index come from
    /// the LDB system section rather than the tree, which is why a vehicle whose
    /// system name is empty has no sprite.
    /// </remarks>
    private void LoadVehicles(Godot.Collections.Dictionary? pMapTreeData, int pMapId)
    {
        _vehicles = new List<Rm2kVehicleState>(3);
        if (pMapTreeData == null || !pMapTreeData.TryGetValue("start", out var rawStart)
            || rawStart.VariantType != Godot.Variant.Type.Dictionary)
        {
            return;
        }
        var start = rawStart.AsGodotDictionary();

        _vehicles.Add(BuildVehicle(
            start, Rm2kVehicle.Boat, "boat", pMapId));
        _vehicles.Add(BuildVehicle(
            start, Rm2kVehicle.Ship, "ship", pMapId));
        _vehicles.Add(BuildVehicle(
            start, Rm2kVehicle.Airship, "airship", pMapId));
    }

    /// <summary>One vehicle, with the system section's sprite and the start node's tile.</summary>
    private Rm2kVehicleState BuildVehicle(
        Godot.Collections.Dictionary pStart, int pVehicleType, string pPrefix, int pMapId)
    {
        TryReadInt(pStart, pPrefix + "_map_id", out var vehicleMapId);
        TryReadInt(pStart, pPrefix + "_x", out var vehicleX);
        TryReadInt(pStart, pPrefix + "_y", out var vehicleY);

        // The sprite comes from the system section, not the tree. A vehicle
        // without a system name has no cell to draw and is reported rather than
        // drawn as the first charset.
        var vehicle = new Rm2kVehicleState(pVehicleType, vehicleMapId, vehicleX, vehicleY)
        {
            CharacterName = ReadSystemString(pVehicleType == Rm2kVehicle.Boat
                ? "boat_name"
                : pVehicleType == Rm2kVehicle.Ship ? "ship_name" : "airship_name"),
            SpriteIndex = ReadSystemInt(pVehicleType == Rm2kVehicle.Boat
                ? "boat_index"
                : pVehicleType == Rm2kVehicle.Ship ? "ship_index" : "airship_index"),
        };
        _ = pMapId;
        return vehicle;
    }

    private string ReadSystemString(string pKey)
    {
        if (DatabaseData != null
            && DatabaseData.TryGetValue("system", out var rawSystem)
            && rawSystem.VariantType == Godot.Variant.Type.Dictionary
            && rawSystem.AsGodotDictionary().TryGetValue(pKey, out var rawValue)
            && rawValue.VariantType == Godot.Variant.Type.String)
        {
            return rawValue.AsString();
        }
        return "";
    }

    private int ReadSystemInt(string pKey)
    {
        if (DatabaseData != null
            && DatabaseData.TryGetValue("system", out var rawSystem)
            && rawSystem.VariantType == Godot.Variant.Type.Dictionary
            && rawSystem.AsGodotDictionary().TryGetValue(pKey, out var rawValue)
            && rawValue.VariantType == Godot.Variant.Type.Int)
        {
            return rawValue.AsInt32();
        }
        return 0;
    }

    private void ClearEventMovementState()
    {
        _eventRoutes.Clear();
        _finishedEventRouteIds.Clear();
        _eventPageRoutes.Clear();
        _boundEventRoutePages.Clear();
        _eventStepStates.Clear();
        _eventMovedThisTick.Clear();
        _eventRoutesMoved = false;
    }

    private void LoadCurrentMapEvents(Godot.Collections.Dictionary? pMapData)
    {
        // Event IDs are local to a map. Never attach an old map's route or
        // unspent step to a newly loaded event that happens to reuse its ID.
        ClearEventMovementState();
        _renderedEventPages.Clear();
        _appliedEventPages.Clear();
        var events = new List<Rm2kMap.Event>();
        if (pMapData != null && pMapData.TryGetValue("events", out var rawEvents)
            && rawEvents.VariantType == Godot.Variant.Type.Array)
        {
            foreach (var rawEvent in rawEvents.AsGodotArray())
            {
                if (rawEvent.VariantType != Godot.Variant.Type.Dictionary) continue;
                var data = rawEvent.AsGodotDictionary();
                if (!TryReadInt(data, "id", out var id) || !TryReadInt(data, "x", out var x) || !TryReadInt(data, "y", out var y)) continue;
                var mapEvent = new Rm2kMap.Event(id, x, y);

                // Retain legacy event-level metadata for compatibility. The
                // active LMU page below is authoritative for graphic and pose.
                if (data.TryGetValue("character_name", out var rawEventCharacterName)
                    && rawEventCharacterName.VariantType == Godot.Variant.Type.String)
                {
                    mapEvent.CharacterName = rawEventCharacterName.AsString();
                }
                if (TryReadInt(data, "character_index", out var eventCharacterIndex))
                {
                    mapEvent.CharacterIndex = eventCharacterIndex;
                }
                if (TryReadInt(data, "character_direction", out var eventDirection))
                {
                    // liblcf stores 1 up, 2 down, 3 left, 4 right; this project
                    // stores 2 down, 4 left, 6 right, 8 up.
                    mapEvent.Direction = Rm2kCharacterSprite.FacingFromLiblcfDirection(eventDirection);
                }
                // LMU pose belongs to EventPage, not Event. Normal animation
                // retains SaveMapEventBase's initial frame until it advances.
                if (data.TryGetValue("pages", out var rawPages) && rawPages.VariantType == Godot.Variant.Type.Array)
                {
                    foreach (var rawPage in rawPages.AsGodotArray())
                    {
                        if (rawPage.VariantType != Godot.Variant.Type.Dictionary) continue;
                        var pageData = rawPage.AsGodotDictionary();
                        if (!TryReadInt(pageData, "trigger", out var trigger)) continue;
                        // Fail closed: a page whose command vector could not be
                        // decoded must not run as if it were empty.
                        if (pageData.TryGetValue("command_error", out var rawCommandError)
                            && rawCommandError.VariantType == Godot.Variant.Type.String
                            && rawCommandError.AsString().Length > 0)
                        {
                            Simulation.AddDiagnostic(
                                $"RM2K event {id} page with trigger {trigger} skipped: {rawCommandError.AsString()}");
                            continue;
                        }
                        var page = new Rm2kMap.EventPage { Trigger = trigger };
                        TryReadInt(pageData, "layer", out var layer);
                        page.Layer = layer;
                        // liblcf EventPage::move_route is LMU chunk 0x29, with
                        // 0x0B byte size/0x0C command bytes, repeat0x15/skippable0x16.
                        // A route that failed to decode is reported by the parser
                        // and leaves the page with an empty route, which is the
                        // same shape as a page that never had one.
                        if (TryReadInt(pageData, "move_frequency", out var moveFrequency))
                        {
                            page.MoveFrequency = moveFrequency;
                        }
                        if (TryReadInt(pageData, "move_type", out var moveType))
                        {
                            page.MoveType = moveType;
                        }
                        if (pageData.TryGetValue("move_route_error", out var rawRouteError)
                            && rawRouteError.VariantType == Godot.Variant.Type.String
                            && rawRouteError.AsString().Length > 0)
                        {
                            Simulation.AddDiagnostic(
                                $"RM2K event {id} move route skipped: {rawRouteError.AsString()}");
                        }
                        if (TryReadBool(pageData, "move_route_repeat", out var routeRepeat))
                        {
                            page.MoveRouteRepeat = routeRepeat;
                        }
                        if (TryReadBool(pageData, "move_route_skippable", out var routeSkippable))
                        {
                            page.MoveRouteSkippable = routeSkippable;
                        }
                        if (pageData.TryGetValue("move_route_commands", out var rawRouteCommands)
                            && rawRouteCommands.VariantType == Godot.Variant.Type.Array)
                        {
                            foreach (var rawCommand in rawRouteCommands.AsGodotArray())
                            {
                                if (rawCommand.VariantType != Godot.Variant.Type.Dictionary) continue;
                                var commandData = rawCommand.AsGodotDictionary();
                                var command = new Rm2kMap.MoveCommand();
                                TryReadInt(commandData, "command_id", out command.CommandId);
                                TryReadInt(commandData, "parameter_a", out command.ParameterA);
                                TryReadInt(commandData, "parameter_b", out command.ParameterB);
                                TryReadInt(commandData, "parameter_c", out command.ParameterC);
                                if (commandData.TryGetValue("parameter_string", out var rawParameterString)
                                    && rawParameterString.VariantType == Godot.Variant.Type.String)
                                {
                                    command.ParameterString = rawParameterString.AsString();
                                }
                                page.MoveRouteCommands.Add(command);
                            }
                        }
                        // liblcf EventPage uses 0x15 for the character name,
                        // 0x16 for the character index and 0x17 for the
                        // transparency. The sprite needs the first two.
                        if (pageData.TryGetValue("character_name", out var rawCharacterName)
                            && rawCharacterName.VariantType == Godot.Variant.Type.String)
                        {
                            page.Graphic["character_name"] = rawCharacterName.AsString();
                        }
                        if (TryReadInt(pageData, "character_index", out var characterIndex))
                        {
                            page.Graphic["character_index"] = characterIndex;
                        }
                        foreach (var poseField in new[] { "character_direction", "character_pattern", "animation_type", "move_speed" })
                        {
                            if (TryReadInt(pageData, poseField, out var poseValue)) page.Graphic[poseField] = poseValue;
                        }
                        if (TryReadInt(pageData, "transparency", out var transparency))
                        {
                            page.Graphic["transparency"] = transparency;
                        }
                        if (pageData.TryGetValue("conditions", out var rawConditions) && rawConditions.VariantType == Godot.Variant.Type.Dictionary)
                        {
                            foreach (var pair in rawConditions.AsGodotDictionary())
                            {
                                var key = pair.Key.ToString();
                                if (pair.Value.VariantType == Godot.Variant.Type.Bool)
                                {
                                    page.Conditions[key] = pair.Value.AsBool();
                                }
                                else if (pair.Value.VariantType == Godot.Variant.Type.Int)
                                {
                                    page.Conditions[key] = pair.Value.AsInt32();
                                }
                            }
                        }
                        if (pageData.TryGetValue("commands", out var rawCommands) && rawCommands.VariantType == Godot.Variant.Type.Array)
                        {
                            foreach (var rawCommand in rawCommands.AsGodotArray())
                            {
                                if (rawCommand.VariantType != Godot.Variant.Type.Dictionary) continue;
                                var command = rawCommand.AsGodotDictionary();
                                if (!TryReadInt(command, "code", out var code)) continue;
                                var text = command.TryGetValue("text", out var rawText) ? rawText.ToString() : "";
                                var parameters = new List<int>();
                                if (command.TryGetValue("parameters", out var rawParameters) && rawParameters.VariantType == Godot.Variant.Type.PackedInt32Array)
                                {
                                    foreach (var parameter in rawParameters.AsInt32Array()) parameters.Add(parameter);
                                }
                                page.Commands.Add(new Rm2kMap.EventCommand(code, parameters, text,
									TryReadInt(command, "indent", out var indent) ? indent : 0));
                            }
                        }
                        mapEvent.Pages.Add(page);
                    }
                }
                events.Add(mapEvent);
            }
        }
        _eventScheduler.SetEvents(events);
        _mapEvents = events;
    }

    private List<Rm2kMap.Event> _mapEvents = new();

    // Compare page identity against the last composition, without decoding images
    // or resetting live event/route state on every idle simulation tick.
    private readonly Dictionary<Rm2kMap.Event, Rm2kMap.EventPage?> _renderedEventPages = new();

    private readonly Dictionary<Rm2kMap.Event, Rm2kMap.EventPage?> _appliedEventPages = new();
    private readonly HashSet<int> _eventMovedThisTick = new();
    private readonly Dictionary<int, Rm2kMoveRouteState> _eventPageRoutes = new();
    private readonly HashSet<int> _finishedEventRouteIds = new();
    private readonly Dictionary<Rm2kMap.Event, Rm2kMap.EventPage?> _boundEventRoutePages = new();

    private static bool HaveSameRouteCodes(Rm2kMap.EventPage pOld, Rm2kMap.EventPage pNew) =>
        pOld.MoveRouteCommands.Count == pNew.MoveRouteCommands.Count
        && pOld.MoveRouteCommands.Select(command => command.CommandId)
            .SequenceEqual(pNew.MoveRouteCommands.Select(command => command.CommandId));

    private void RefreshEventPageRoutes()
    {
        foreach (var mapEvent in _mapEvents)
        {
            _appliedEventPages.TryGetValue(mapEvent, out var page);
            _boundEventRoutePages.TryGetValue(mapEvent, out var previous);
            if (_boundEventRoutePages.ContainsKey(mapEvent) && ReferenceEquals(previous, page)) continue;
            _boundEventRoutePages[mapEvent] = page;
            // No eligible page suspends commands, but preserves an unspent step
            // and the original cursor. Reappearance is a fresh page transition.
            if (page == null) continue;
            var cursor = previous != null && HaveSameRouteCodes(previous, page)
                && _eventPageRoutes.TryGetValue(mapEvent.Id, out var oldRoute) ? oldRoute.CurrentIndex : 0;
            var route = new Rm2kMoveRouteState(page.MoveRouteCommands, page.MoveRouteRepeat, page.MoveRouteSkippable)
            {
                MoveSpeed = Math.Clamp(PagePoseValue(page, "move_speed", 3), 1, 6),
                MoveFrequency = Math.Clamp(page.MoveFrequency, 1, 8)
            };
            route.Force(cursor);
            _eventPageRoutes[mapEvent.Id] = route;
            mapEvent.MaxStopCount = page.MoveType == 6
                ? Rm2kStepBudget.MaxStopCountForTurn(route.MoveFrequency)
                : Rm2kStepBudget.MaxStopCountForStep(route.MoveFrequency);
            if (_eventRoutes.TryGetValue(mapEvent.Id, out var forced))
            {
                forced.MoveSpeed = route.MoveSpeed;
                forced.MoveFrequency = route.MoveFrequency;
            }
            if (page.MoveType != 0 && page.MoveType != 6)
                Simulation.AddDiagnostic($"RM2K event {mapEvent.Id} autonomous movement type {page.MoveType} is not implemented.");
        }
    }

    private void AdvanceEventPageStep(Rm2kMap.Event pEvent, Rm2kMoveRouteState pRoute)
    {
        var step = _eventStepStates[pEvent.Id];
        _eventMovedThisTick.Add(pEvent.Id);
        var advanced = Rm2kStepBudget.Advance(step.RemainingStep, pRoute.MoveSpeed);
        if (advanced.Completed) _eventStepStates.Remove(pEvent.Id);
        else _eventStepStates[pEvent.Id] = (step.X, step.Y, advanced.Remaining, step.Direction);
        pEvent.StopCount = 0;
        _eventRoutesMoved = true;
    }

    private void UpdateEventPageRoute(Rm2kMap.Event pEvent)
    {
        if (!_eventPageRoutes.TryGetValue(pEvent.Id, out var route)) return;
        // A tick that begins moving only spends movement; it cannot also start
        // the next command on arrival. A newly started step spends this tick too.
        if (_eventStepStates.ContainsKey(pEvent.Id))
        {
            AdvanceEventPageStep(pEvent, route);
            return;
        }
        var canMove = Presentation.MessageContinuesEvents || !_eventScheduler.HasBlockingInterpreter;
        _appliedEventPages.TryGetValue(pEvent, out var page);
        if (page != null && page.MoveType == 6 && canMove && route.Active && pEvent.StopCount >= pEvent.MaxStopCount)
        {
            var start = route.CurrentIndex;
            for (var work = 0; work < route.Commands.Count; work++)
            {
                if (route.CurrentIndex >= route.Commands.Count)
                {
                    if (!route.Repeat) { route.Cancel(); break; }
                    route.SetIndex(0);
                    if (start == 0) break;
                }
                var command = route.Current!;
                var id = command.CommandId;
                if ((id >= Rm2kMoveRoute.MoveUp && id <= Rm2kMoveRoute.MoveLeft)
                    || id == Rm2kMoveRoute.MoveTowardsHero)
                {
                    if (!TryBeginEventStep(pEvent, route, command))
                    {
                        if (!route.Skippable) { route.NoteMoveFailure(); break; }
                    }
                    route.ConsumeCommand();
                    pEvent.MaxStopCount = Rm2kStepBudget.MaxStopCountForStep(route.MoveFrequency);
                    if (_eventStepStates.ContainsKey(pEvent.Id)) { AdvanceEventPageStep(pEvent, route); return; }
                }
                else if (id >= Rm2kMoveRoute.FaceUp && id <= Rm2kMoveRoute.Turn180Degree)
                {
                    ApplyEventRouteFacingCommand(pEvent, route, id);
                    route.ConsumeCommand();
                    pEvent.MaxStopCount = Rm2kStepBudget.MaxStopCountForTurn(route.MoveFrequency);
                    pEvent.StopCount = 0;
                }
                else if (id == Rm2kMoveRoute.Wait)
                {
                    pEvent.MaxStopCount = Rm2kStepBudget.MaxStopCountForWait(route.MoveFrequency);
                    pEvent.StopCount = 0;
                    route.ConsumeCommand();
                }
                else if (id >= Rm2kMoveRoute.IncreaseMovementSpeed && id <= Rm2kMoveRoute.DecreaseMovementFrequence)
                {
                    ApplyEventRouteFacingCommand(pEvent, route, id);
                    route.ConsumeCommand();
                }
                else
                {
                    Simulation.AddDiagnostic($"RM2K event {pEvent.Id} page route stopped at unsupported command {id}, index {route.CurrentIndex}.");
                    route.Cancel();
                    break;
                }
                if (pEvent.StopCount < pEvent.MaxStopCount || route.CurrentIndex == start) break;
            }
        }
        if (pEvent.StopCount == 0 || canMove) pEvent.StopCount = Math.Min(pEvent.StopCount + 1, 65535);
    }

    private static int PagePoseValue(Rm2kMap.EventPage pPage, string pField, int pDefault) =>
        pPage.Graphic.TryGetValue(pField, out var raw) && raw is int value ? value : pDefault;

    private void RefreshEventPagePoses()
    {
        foreach (var mapEvent in _mapEvents)
        {
            var page = Rm2kEventPageSelector.SelectActive(mapEvent, Simulation);
            if (_appliedEventPages.TryGetValue(mapEvent, out var previous) && ReferenceEquals(previous, page)) continue;
            _appliedEventPages[mapEvent] = page;
            _isRenderDirty = true;
            if (page == null) continue;
            var direction = PagePoseValue(page, "character_direction", 2);
            var pattern = PagePoseValue(page, "character_pattern", 1);
            var animationType = PagePoseValue(page, "animation_type", 0);
            var facing = Rm2kCharacterSprite.FacingFromLiblcfDirection(direction);
            var stopping = !_eventStepStates.TryGetValue(mapEvent.Id, out var step) || step.RemainingStep <= 0;
            // Pinned Game_Event::RefreshPage: only stopped direction/pattern
            // changes reset movement direction. Fixed facing is independent.
            if (stopping && (previous == null
                || PagePoseValue(previous, "character_direction", 2) != direction
                || PagePoseValue(previous, "character_pattern", 1) != pattern))
            {
                mapEvent.Direction = facing;
                mapEvent.FacingDirection = facing;
            }
            mapEvent.FacingLocked = animationType is 2 or 3 or 4;
            if (mapEvent.FacingLocked) mapEvent.FacingDirection = facing;
            if (animationType is 4 or 5) mapEvent.AnimationFrame = Rm2kCharacterAnimation.ClampFrame(pattern);
        }
    }

    private bool HaveEventGraphicsChanged()
    {
        if (_renderedRenderer == null) return false;
        if (_renderedEventPages.Count != _mapEvents.Count) return true;
        foreach (var mapEvent in _mapEvents)
        {
            if (!_renderedEventPages.TryGetValue(mapEvent, out var renderedPage)
                || !ReferenceEquals(renderedPage, Rm2kEventPageSelector.SelectActive(mapEvent, Simulation)))
                return true;
        }
        return false;
    }

    private void UpdateEventAnimations(int pTicks)
    {
        for (var tick = 0; tick < pTicks; tick++)
        foreach (var mapEvent in _mapEvents)
        {
            if (!_appliedEventPages.TryGetValue(mapEvent, out var page) || page == null) continue;
            var animationType = PagePoseValue(page, "animation_type", 0);
            var speed = PagePoseValue(page, "move_speed", 3);
            if (_eventPageRoutes.TryGetValue(mapEvent.Id, out var original)) speed = original.MoveSpeed;
            if (_eventRoutes.TryGetValue(mapEvent.Id, out var route)
                && (route.Active || _eventStepStates.ContainsKey(mapEvent.Id))) speed = route.MoveSpeed;
            speed = Math.Clamp(speed, 1, 6);
            if (animationType == 5)
            {
                mapEvent.AnimationCount++;
                if (mapEvent.AnimationCount >= Rm2kCharacterAnimation.SpinAnimFrames(speed))
                {
                    var facing = Rm2kMoveRoute.LiblcfFromFacingDirection(mapEvent.FacingDirection ?? mapEvent.Direction);
                    mapEvent.FacingDirection = Rm2kMoveRoute.FacingFromLiblcfDirection((facing + 1) % 4);
                    mapEvent.AnimationCount = 0;
                    _isRenderDirty = true;
                }
                continue;
            }
            if (animationType is 4 or 6) continue;
            if (animationType < 0 || animationType > 6)
            {
                Simulation.AddDiagnostic($"RM2K event {mapEvent.Id} has unsupported animation type {animationType}.");
                continue;
            }
            var updated = Rm2kCharacterAnimation.Update(mapEvent.AnimationFrame, mapEvent.AnimationCount,
                pStopCount: _eventMovedThisTick.Contains(mapEvent.Id) ? 0 : 1,
                pSpeed: speed, pContinuous: animationType is 1 or 3);
            if (mapEvent.AnimationFrame != updated.Frame) _isRenderDirty = true;
            mapEvent.AnimationFrame = updated.Frame;
            mapEvent.AnimationCount = updated.Count;
        }
    }

    private bool IsEventSpinning(Rm2kMap.Event pEvent) =>
        _appliedEventPages.TryGetValue(pEvent, out var page) && page != null
        && PagePoseValue(page, "animation_type", 0) == 5;

    /// <summary>
    /// The three vehicles, built from the LMT start node when a map is loaded.
    /// Always three entries, in the order boat, ship, airship, so a caller can
    /// index by <c>Game_Vehicle::Type</c>.
    /// </summary>
    private List<Rm2kVehicleState> _vehicles = new();

    /// <summary>The vehicles on the current map, for a caller that renders them.</summary>
    public IReadOnlyList<Rm2kVehicleState> Vehicles => _vehicles;

    /// <summary>The player's vehicle boarding state, or null before a map is loaded.</summary>
    public Rm2kVehicleBoarding? VehicleBoarding { get; private set; }

    /// <summary>
    /// Moves a vehicle onto the current map at a tile, for a test that needs the
    /// vehicle draw path to actually run.
    /// </summary>
    /// <remarks>
    /// The pinned fixture parks every vehicle on map 39 while it only ships
    /// <c>Map0001.lmu</c>, so nothing would ever be drawn without this. A test
    /// that wanted a visible boat on the real map would otherwise have to invent
    /// a map 39 or a <c>vehicle.png</c> the game never had, and a fake charset
    /// would prove nothing about the real file. The alternative is leaving the
    /// draw path unexercised, which is worse.
    /// </remarks>
    /// <param name="pVehicleType">One of the <c>Rm2kVehicle</c> types.</param>
    /// <param name="pX">Tile in x.</param>
    /// <param name="pY">Tile in y.</param>
    /// <returns>True when that vehicle exists and was moved.</returns>
    public bool PlaceVehicleOnCurrentMapForTest(int pVehicleType, int pX, int pY)
    {
        foreach (var vehicle in _vehicles)
        {
            if (vehicle.VehicleType != pVehicleType)
            {
                continue;
            }
            vehicle.MapId = Simulation.MapId;
            vehicle.X = pX;
            vehicle.Y = pY;
            return true;
        }
        return false;
    }

    /// <summary>
    /// The loaded map's decoded data, kept so a change that affects rendering can
    /// be recomposed without parsing the file again.
    /// </summary>
    private Godot.Collections.Dictionary? _currentMap;
    private int _currentMapWidth;
    private int _currentMapHeight;

    /// <summary>
    /// Recomposes the current frame from the data that is already loaded.
    /// </summary>
    /// <remarks>
    /// A vehicle, an event or the camera can change between two frames without
    /// the map file changing, so the runtime keeps the decoded data and rebuilds
    /// from it. This is the same path the per-frame update uses, so a test that
    /// calls this is exercising the real draw and not a copy of it.
    /// </remarks>
    public void RestoreRenderForTest()
    {
        if (_currentMap == null)
        {
            return;
        }
        RenderCurrentMap(_currentMap, _currentMapWidth, _currentMapHeight);
    }

    /// <summary>The vehicles whose start map is the loaded one, in draw order.</summary>
    public List<Rm2kVehicleState> VehiclesOnCurrentMap()
    {
        var here = new List<Rm2kVehicleState>();
        foreach (var vehicle in _vehicles)
        {
            if (vehicle.MapId == Simulation.MapId)
            {
                here.Add(vehicle);
            }
        }
        return here;
    }

    /// <summary>
    /// The decoded LDB <c>system</c> section, which holds the starting party.
    /// Null when no database was loaded, which is also a valid state.
    /// </summary>
    private Godot.Collections.Dictionary? SystemData
    {
        get
        {
            if (DatabaseData != null && DatabaseData.TryGetValue("system", out var raw)
                && raw.VariantType == Godot.Variant.Type.Dictionary)
            {
                return raw.AsGodotDictionary();
            }
            return null;
        }
    }

    /// <summary>
    /// And the hero database into the state.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the state held empty values</strong>, -- <strong>because
    /// <c>GetOrCreateActorValues</c> creates a blank entry and nothing ever
    /// filled it from the bank</strong>.
    /// </para>
    /// <para>
    /// <strong>And every number here is a field of an actor entry</strong>,
    /// -- <strong>and a field that is absent leaves the value alone</strong>,
    /// -- <strong>because a missing field is not a zero</strong>.
    /// </para>
    /// </remarks>
    private void ReadActorValues(Godot.Collections.Dictionary pDatabase)
    {
        if (pDatabase == null
            || !pDatabase.TryGetValue("actors", out var rawActors)
            || rawActors.VariantType != Godot.Variant.Type.Array)
        {
            return;
        }

        foreach (var raw in rawActors.AsGodotArray())
        {
            if (raw.VariantType != Godot.Variant.Type.Dictionary)
            {
                continue;
            }

            var entry = raw.AsGodotDictionary();
            if (!TryReadInt(entry, "id", out var id) || id <= 0
                || id > UniversalRPG.Rm2k.Simulation
                    .GameSimulationState.MaxActorId)
            {
                continue;
            }

            // **Und  die  Basiswerte  gehen  ueber  den  vorhandenen
            //  Setter**, -- **denn  der  klemmt  auf  die  Grenzen  des
            //  Formats  und  eine  Bank,  die  dort  eine  Zahl  ueber
            //  `9999`  schreibt,  soll  nicht  einen  Helden  mit
            //  fuenfstelligen  Trefferpunkten  erzeugen.**
            var werte = Simulation.GetOrCreateActorValues(id);
            // **Und  die  sechs  Kampfwerte  kommen  aus  einem  Array**
            // -- **und  nicht  aus  sechs  Feldern**, -- **denn  liblcf
            //  gibt  `ChunkActor`  bei  0x1F  ein  `Array x 6 - Short`
            //  namens  `parameters`** -- **und  meine  ersten  sechs
            //  Feldnamen  existierten  dort  nicht.**
            //
            // **Und  die  Reihenfolge  ist  liblcfs  eigene:**
            // -- **maxhp, maxsp, attack, defense, spirit, agility.**
            // **Und  die  sechs  Kampfwerte  kommen  aus  `parameters`**,
// **und  das  ist  ein  `Dictionary`  mit  sechs  Listen,  jede
//  eine  Stufe  lang**, -- **und  nicht  ein  flaches  Array  von
//  sechs  Zahlen**.
//
// **Und  mein  erster  Versuch  las  es  als  `AsInt32Array`**,
// -- **und  das  gibt  es  dort  nicht**,
// -- **und  deshalb  war  der  Held
//  nach  dem  Start  ein  `Spencer`  mit  einem  Trefferpunkt**.
//
// **Und  welche  Stufe  zaehlt,  ist  eine  Frage  des  Spiels**:
// -- **RM2K  startet  den  Helden  auf  `initial_level`**, -- **und
//  das  ist  Feld  `0x07`  des  Actors**, -- **und  der  Vektor  ist
//  pro  Stufe  indiziert**, -- **also  `Stufe - 1`**.
var stufe = TryReadInt(entry, "initial_level", out var li)
    && li > 0 ? li : 1;
if (entry.TryGetValue("parameters", out var rohParameter)
    && rohParameter.VariantType == Godot.Variant.Type.Dictionary)
{
    var parameter = rohParameter.AsGodotDictionary();
    if (ReadParameterAt(parameter, "maxhp", stufe,
            v => werte.SetBaseParameter(
                Rm2kActorValues.ParameterMaxHp, v))
        && ReadParameterAt(parameter, "maxsp", stufe,
            v => werte.SetBaseParameter(
                Rm2kActorValues.ParameterMaxSp, v))
        && ReadParameterAt(parameter, "attack", stufe,
            v => werte.SetBaseParameter(
                Rm2kActorValues.ParameterAttack, v))
        && ReadParameterAt(parameter, "defense", stufe,
            v => werte.SetBaseParameter(
                Rm2kActorValues.ParameterDefense, v))
        && ReadParameterAt(parameter, "spirit", stufe,
            v => werte.SetBaseParameter(
                Rm2kActorValues.ParameterSpirit, v))
        && ReadParameterAt(parameter, "agility", stufe,
            v => werte.SetBaseParameter(
                Rm2kActorValues.ParameterAgility, v)))
    {
        // **Und  sechs  von  sechs  gelesen.**
    }
    else
    {
        Simulation.AddDiagnostic(
            "RM2K actor " + id + " does not carry all six parameter"
                + " vectors for level " + stufe + ", so the hero"
                + " keeps the base values");
    }
}
else
{
    Simulation.AddDiagnostic(
        "RM2K actor " + id + " has no parameters chunk, so the"
            + " hero keeps the base values");
}

ReadString(entry, "name", v => werte.Name = v);
            ReadString(entry, "title", v => werte.Title = v);
            ReadString(entry, "character_name",
                v => werte.SpriteName = v);
            ReadInt(entry, "character_index", v => werte.SpriteIndex = v);
        }
    }

    /// <summary>
    /// And one level's value out of a parameter vector.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the six vectors are indexed by level</strong>, --
    /// <strong>and level one is index zero</strong>, -- <strong>because the
    /// chunk starts at the class's first level</strong>.
    /// </para>
    /// <para>
    /// <strong>And a level the game never wrote is not a zero</strong>, --
    /// <strong>it is an absent value</strong>, -- <strong>and this returns
    /// false rather than clamping it to one</strong>.
    /// </para>
    /// </remarks>
    private static bool ReadParameterAt(
        Godot.Collections.Dictionary pParameter, string pName, int pStufe,
        System.Action<int> pSet)
    {
        if (!pParameter.TryGetValue(pName, out var raw)
            || raw.VariantType != Godot.Variant.Type.Array)
        {
            return false;
        }

        var liste = raw.AsGodotArray();
        var index = pStufe - 1;
        if (index < 0 || index >= liste.Count)
        {
            return false;
        }

        pSet(liste[index].AsInt32());
        return true;
    }

    /// <summary>
    /// And the starting party out of the bank's system chunk.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the only writer of <c>PartyMemberIds</c> is the
    /// interpreter's <c>11110</c></strong>, -- <strong>and this game
    /// writes it in none of its seven hundred and forty-three maps</strong>,
    /// -- <strong>so the running game had no party at all</strong>.
    /// </para>
    /// <para>
    /// <strong>And the reference reads it from
    /// <c>lcf::Data::system.party</c></strong>, -- <strong>and
    /// <c>ChunkSystem</c> field <c>0x16</c> is an array of
    /// <c>int16</c></strong>.
    /// </para>
    /// <para>
    /// <strong>And an entry the bank does not carry is skipped</strong>,
    /// -- <strong>because a party of actors the database does not
    /// define is not a party the game wrote</strong>.
    /// </para>
    /// </remarks>
    private void ReadStartParty(Godot.Collections.Dictionary pDatabase)
    {
        if (Simulation.PartyMemberIds.Count > 0)
        {
            return;
        }

        if (pDatabase == null
            || !pDatabase.TryGetValue("system", out var rawSystem)
            || rawSystem.VariantType != Godot.Variant.Type.Dictionary)
        {
            return;
        }

        if (!rawSystem.AsGodotDictionary().TryGetValue("party",
                out var rawParty)
            || rawParty.VariantType != Godot.Variant.Type.Array)
        {
            return;
        }

        foreach (var raw in rawParty.AsGodotArray())
        {
            var held = raw.AsInt32();
            if (held < 1 || held > GameSimulationState.MaxActorId
                || Simulation.FindActorValues(held) == null)
            {
                Simulation.AddDiagnostic(
                    "RM2K the starting party names actor " + held
                    + ", which the database does not define, so it"
                    + " is skipped");
                continue;
            }

            // **Und  vier  sind  das  Format** -- **und
            //  `MaxPartyMembers`  ist  liblcfs  Zahl.**
            if (Simulation.PartyMemberIds.Count
                >= UniversalRPG.Rm2k.Simulation.GameSimulationState
                    .MaxPartyMembers)
            {
                break;
            }

            Simulation.PartyMemberIds.Add(held);
        }
    }

    /// <summary>And an int field into a setter, when the bank has it.</summary>
    private static void ReadInt(Godot.Collections.Dictionary pEntry,
        string pField, System.Action<int> pSet)
    {
        if (TryReadInt(pEntry, pField, out var wert))
        {
            pSet(wert);
        }
    }

    /// <summary>And a string field into a setter, when the bank has it.</summary>
    private static void ReadString(Godot.Collections.Dictionary pEntry,
        string pField, System.Action<string> pSet)
    {
        if (pEntry.TryGetValue(pField, out var raw)
            && raw.VariantType == Godot.Variant.Type.String)
        {
            pSet(raw.AsString());
        }
    }

    /// <summary>
    /// An actor's charset from the LDB. <c>GetSpriteName</c> falls back to
    /// <c>dbActor-&gt;character_name</c>, which is field 0x03 of an actor entry.
    /// </summary>
    private bool TryGetActorCharacterName(int pActorId, out string pName)
    {
        pName = "";
        if (DatabaseData == null || pActorId <= 0
            || !DatabaseData.TryGetValue("actors", out var rawActors)
            || rawActors.VariantType != Godot.Variant.Type.Array)
        {
            return false;
        }
        var entry = FindById(rawActors.AsGodotArray(), pActorId);
        if (entry == null || !entry.TryGetValue("character_name", out var rawName)
            || rawName.VariantType != Godot.Variant.Type.String)
        {
            return false;
        }
        pName = rawName.AsString();
        return true;
    }

    /// <summary>
    /// An actor's charset cell index, field 0x04 of an actor entry, which
    /// <c>GetSpriteIndex</c> falls back to.
    /// </summary>
    private bool TryGetActorCharacterIndex(int pActorId, out int pIndex)
    {
        pIndex = 0;
        if (DatabaseData == null || pActorId <= 0
            || !DatabaseData.TryGetValue("actors", out var rawActors)
            || rawActors.VariantType != Godot.Variant.Type.Array)
        {
            return false;
        }
        var entry = FindById(rawActors.AsGodotArray(), pActorId);
        if (entry == null)
        {
            return false;
        }
        return TryReadInt(entry, "character_index", out pIndex);
    }

    /// <summary>Finds a database entry by its <c>id</c> field.</summary>
    private static Godot.Collections.Dictionary? FindById(Godot.Collections.Array pEntries, int pId)
    {
        foreach (var raw in pEntries)
        {
            if (raw.VariantType != Godot.Variant.Type.Dictionary)
            {
                continue;
            }
            var entry = raw.AsGodotDictionary();
            if (TryReadInt(entry, "id", out var id) && id == pId)
            {
                return entry;
            }
        }
        return null;
    }


    /// <summary>
    /// The camera offsets the last composed frame was cut at, in pixels. Exposed
    /// for tests: the camera arithmetic is unit tested in Rm2kMapCamera, but
    /// that does not prove the runtime applies it, and a mutation that returns
    /// zero here is otherwise invisible.
    /// </summary>
    public int AppliedCameraOffsetX { get; private set; }

    /// <summary>Vertical counterpart of <see cref="AppliedCameraOffsetX"/>.</summary>
    public int AppliedCameraOffsetY { get; private set; }

    /// <summary>
    /// Moves the player to a map tile without a passability check, for tests
    /// that need a position the fixture data would otherwise block. It changes
    /// the simulation exactly the way a successful step would.
    /// </summary>
    /// <remarks>
    /// Exposed for tests only, and named for that, because the pinned fixture
    /// map has no walkable route from the start position and a test that needs
    /// a moved player would otherwise have to invent a map.
    /// </remarks>
    public void PlacePlayerForTest(int pX, int pY)
    {
        Simulation.MapX = pX;
        Simulation.MapY = pY;
    }

    /// <summary>
    /// Starts a step through the same path the input handler uses, for tests.
    /// It exists so a test can observe the per frame step budget in the
    /// rendered frame instead of only in the arithmetic.
    /// </summary>
    /// <summary>
    /// The pixel offset the hero sprite is drawn with for the current step.
    /// Exposed because the pinned fixture has an empty party, so the hero
    /// sprite is never drawn and its position cannot be observed in the frame.
    /// Without this, only the camera's response to a step would be under test
    /// and a hero that snapped while the camera scrolled would pass.
    /// </summary>
    public (int X, int Y) HeroStepPixelOffset
    {
        get
        {
            // Read the value the renderer was actually given rather than
            // recomputing it. Recomputing here would make this property agree
            // with the step budget even if the sprite wiring dropped the
            // offset, which is exactly the wiring under test.
            var hero = ComposedHeroSprite;
            if (hero != null)
            {
                return (hero.PixelOffsetX, hero.PixelOffsetY);
            }
            // No hero sprite means no value to report, and recomputing one here
            // would let this property agree with the step budget even when the
            // sprite wiring dropped the offset. A missing hero is a fact about
            // the game, not something to paper over.
            return (int.MinValue, int.MinValue);
        }
    }

    /// <summary>
    /// The animation frame the hero sprite is drawn with, for the same reason
    /// as <see cref="HeroStepPixelOffset"/>.
    /// </summary>
    public int HeroAnimationFrame
    {
        get
        {
            var hero = ComposedHeroSprite;
            if (hero != null)
            {
                return hero.Frame;
            }
            return int.MinValue;
        }
    }

    /// <summary>
    /// The character sprites of the last built frame, so a test can read what
    /// the renderer was given. Without a hero sprite the party is empty and
    /// the list holds only event characters, which is why the properties above
    /// fall back rather than reporting a wrong value.
    /// </summary>
    private IReadOnlyList<Rm2kCharacterSprite> _heroSpriteProbe { get; set; } =
        Array.Empty<Rm2kCharacterSprite>();

    /// <summary>
    /// The character sprite the frame was composed from, for tests. The pinned
    /// LDB has an empty party, so the hero sprite does not exist in this
    /// fixture and the hero's own offset cannot be observed; an event character
    /// carries the same sprite wiring, so reading it proves the step offset
    /// reaches the renderer rather than only the state.
    /// </summary>
    public Rm2kCharacterSprite? FirstComposedCharacterSprite =>
        _heroSpriteProbe.Count > 0 ? _heroSpriteProbe[0] : null;

    /// <summary>
    /// The hero sprite of the last composed frame, or null when the party is
    /// empty. Events are composed first and the hero last, so the last entry is
    /// the hero. Identifying it by index rather than by map position matters:
    /// an event can stand on the same tile as the player, and matching on
    /// position alone would then report the event's offset.
    /// </summary>
    public Rm2kCharacterSprite? ComposedHeroSprite
    {
        get
        {
            if (LeadingActorCharacterName.Length == 0)
            {
                return null;
            }
            foreach (var sprite in _heroSpriteProbe)
            {
                if (sprite.Stage == Rm2kMapFrameRenderer.SpriteStage.HeroLayer
                    && sprite.CharacterIndex == LeadingActorCharacterIndex
                    && sprite.MapX == Simulation.MapX
                    && sprite.MapY == Simulation.MapY)
                {
                    return sprite;
                }
            }
            return null;
        }
    }

    public bool TryMoveForTest(int pDeltaX, int pDeltaY)
    {
        return TryMove(pDeltaX, pDeltaY);
    }

    /// <summary>
    /// Recomposes the frame from the cached tile layers and the current
    /// characters, for tests. A production move does this through
    /// <see cref="TryMove"/>; a test that places the player directly has to ask
    /// for it, so the recomposition is never silently skipped.
    /// </summary>
    /// <summary>
    /// Starts an event's move route the way the verified <c>MoveEvent</c>
    /// command does, so a test can watch an event walk. Returns the number of
    /// commands the route holds, which is zero when the event's page has none.
    /// </summary>
    public int StartEventMoveRouteForTest(int pEventId)
    {
        foreach (var mapEvent in _mapEvents)
        {
            if (mapEvent.Id != pEventId || mapEvent.Pages.Count == 0)
            {
                continue;
            }
            var page = mapEvent.Pages[0];
            if (page.MoveRouteCommands.Count == 0)
            {
                return 0;
            }
            var route = new Rm2kMoveRouteState(
                page.MoveRouteCommands, page.MoveRouteRepeat, page.MoveRouteSkippable)
            {
                MoveSpeed = Math.Clamp(PagePoseValue(page, "move_speed", 3), 1, 6),
                MoveFrequency = page.MoveFrequency,
                Direction = Rm2kMoveRoute.LiblcfFromFacingDirection(mapEvent.Direction),
            };
            route.Force(0);
            _finishedEventRouteIds.Remove(pEventId);
            _eventRoutes[pEventId] = route;
            return page.MoveRouteCommands.Count;
        }
        return 0;
    }

    /// <summary>The position an event's walk has reached, or null if it is not moving.</summary>
    public (int X, int Y)? EventPositionForTest(int pEventId)
    {
        foreach (var mapEvent in _mapEvents)
        {
            if (mapEvent.Id == pEventId)
            {
                return (mapEvent.X, mapEvent.Y);
            }
        }
        return null;
    }

    /// <summary>The unspent step of an event that is mid walk, or -1 when it is not.</summary>
    public int EventRemainingStepForTest(int pEventId)
    {
        return _eventStepStates.TryGetValue(pEventId, out var step) ? step.RemainingStep : -1;
    }

    /// <summary>Whether an event's route has run to completion.</summary>
    public bool EventRouteFinishedForTest(int pEventId)
    {
        return (_eventRoutes.TryGetValue(pEventId, out var route) && route.Finished)
            || _finishedEventRouteIds.Contains(pEventId);
    }

    /// <summary>The direction an event faces, in the RPG Maker byte order.</summary>
    public int EventFacingForTest(int pEventId)
    {
        foreach (var mapEvent in _mapEvents)
        {
            if (mapEvent.Id == pEventId)
            {
                return mapEvent.Direction;
            }
        }
        return -1;
    }

    public void MarkFrameDirtyForTest()
    {
        _isRenderDirty = true;
    }

    public void RefreshFrameForTest()
    {
        if (_isRenderDirty)
        {
            RecomposeFrame();
        }
    }

    /// <summary>
    /// Composites the cached tile layers with the current characters into one
    /// screen sized frame. This is the local equivalent of
    /// <c>Scene_Map::UpdateGraphics</c>, which runs once per frame; the tile
    /// layers do not change unless the map changed, so they are reused.
    /// </summary>
    /// <summary>
    /// Publishes the character sprites the last composition used, so a test
    /// can read the values the renderer was given instead of recomputing them.
    /// </summary>
    private void PublishHeroSpriteProbe(IReadOnlyList<Rm2kCharacterSprite> pSprites)
    {
        _heroSpriteProbe = pSprites;
    }

    private void RecomposeFrame()
    {
        if (_renderedRenderer == null || _renderedLayers == null
            || _lowerLayerPixels == null || _upperLayerPixels == null)
        {
            return;
        }
        _isRenderDirty = false;

        // Initial composition also applies the starting page's pose exactly once.
        RefreshEventPagePoses();
        _isRenderDirty = false;

        // The Player does not re-raster the map when the player moves: it keeps
        // the two tile layers whole and scrolls them by
        // GetDisplayX() / (SCREEN_TILE_SIZE / TILE_SIZE) screen tiles, then by
        // TILE_SIZE to reach pixels. The same arithmetic is applied here without
        // re-rastering, and the characters get the same offset so a character
        // stays on its own map tile.
        var offsetX = ResolveCameraOffsetX();
        var offsetY = ResolveCameraOffsetY();
        AppliedCameraOffsetX = offsetX;
        AppliedCameraOffsetY = offsetY;
        var frame = new Rm2kPixelBuffer(ScreenWidth, ScreenHeight);
        CopyViewport(_lowerLayerPixels, frame, offsetX, offsetY);

        var sprites = BuildCharacterSprites(offsetX, offsetY);
        PublishHeroSpriteProbe(sprites);
        _renderedRenderer.CurrentStage = Rm2kMapFrameRenderer.SpriteStage.BelowLayer;
        _renderedRenderer.RenderSprites(frame, _renderedLayers, sprites);
        _renderedRenderer.CurrentStage = Rm2kMapFrameRenderer.SpriteStage.HeroLayer;
        _renderedRenderer.RenderSprites(frame, _renderedLayers, sprites);
        // The Player creates the vehicle sprites after the event sprites, in
        // Spriteset_Map::UpdateGraphics, so a vehicle is drawn on top of the
        // events that share its tile rather than under them.
        DrawVehicleSprites(frame, offsetX, offsetY);
        // The upper layer is laid over the characters, so it must not clear the
        // frame first: only its transparent pixels may be skipped.
        CopyViewportOver(_upperLayerPixels, frame, offsetX, offsetY);
        _renderedRenderer.CurrentStage = Rm2kMapFrameRenderer.SpriteStage.AboveLayer;
        _renderedRenderer.RenderSprites(frame, _renderedLayers, sprites);

        foreach (var sprite in sprites)
        {
            if (sprite.Skipped)
            {
                Simulation.AddDiagnostic(
                    $"RM2K character {sprite.CharacterIndex} at ({sprite.MapX},{sprite.MapY}) could not be drawn.");
            }
        }
        RenderedMap = frame;

        // **Und ein Frame, in dem kein einziges Byte gesetzt ist, sagt
        // es jetzt.**
        //
        // **Gemessen an einem fertigen Spiel:** die Runtime lud eine
        // Karte, die der Renderer nicht zeichnen konnte, und der
        // Frame war 76800 mal `0x00000000` -- **und die Diagnose war
        // leer.** Ein Spieler sieht einen schwarzen Bildschirm und
        // **hat nichts, was er melden kann.**
        //
        // **Und die Karte, um die es hier geht, ist eine leere
        // Editor-Karte eines Spiels von 2002:** 2266 Bytes, ein Chip
        // in allen 300 Feldern. **Bei ihr ist ein schwarzer Frame
        // richtig** -- **und richtig, ohne ein Wort, ist er ein Fehler
        // der sich als Erfolg verkleidet.**
        if (RenderDiagnostic.Length == 0 && IsEmpty(frame))
        {
            RenderDiagnostic =
                "RM2K rendered nothing: the map named by the game's "
                + "map tree has no drawable tile in any of its fields. "
                + "A map with one chip repeated in every field is an "
                + "editor's empty map, and a game that starts on one "
                + "was exported without being finished";
        }
    }

    /// <summary>
    /// Whether a frame has nothing in it at all.
    /// </summary>
    /// <param name="pFrame">The composed frame.</param>
    /// <returns>True when not one of the four channels is set.</returns>
    /// <remarks>
    /// <strong>And all four, and not only the alpha.</strong> A frame
    /// whose alpha is zero everywhere is empty; **a frame whose alpha
    /// is set everywhere and whose colour is black is not**, **and a
    /// check that looked at one channel alone would call a black
    /// screen empty and a black screen drawn.**
    /// </remarks>
    private static bool IsEmpty(Rm2kPixelBuffer pFrame)
    {
        for (var index = 0; index < pFrame.Pixels.Length; index++)
        {
            if (pFrame.Pixels[index] != 0)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// The horizontal camera offset in pixels, from
    /// <c>Game_Map::GetDisplayX</c> and <c>Game_Player::GetDefaultPanX</c>.
    /// The screen shake the Player adds on top is deliberately not applied: it
    /// is presentation state and would couple a cosmetic effect to the
    /// deterministic core.
    /// </summary>
    private int ResolveCameraOffsetX()
    {
        var position = Rm2kMapCamera.PositionX(Simulation.MapX, _renderedLayers!.Width, ScreenWidth);
        var pixels = Rm2kMapCamera.OffsetPixelsX(position);
        return pixels;
    }

    /// <summary>
    /// The vertical camera offset in pixels, following
    /// <c>Game_Map::GetDisplayY</c> and <c>Game_Player::GetDefaultPanY</c>.
    /// </summary>
    private int ResolveCameraOffsetY()
    {
        var position = Rm2kMapCamera.PositionY(Simulation.MapY, _renderedLayers!.Height, ScreenHeight);
        return Rm2kMapCamera.OffsetPixelsY(position);
    }

    /// <summary>
    /// Paints the cached upper layer over the frame, keeping the frame's pixels
    /// where the layer is transparent. This is the same rule the verified
    /// chipset blit uses, so a wall tile can still hide a character.
    /// </summary>
    private static void CopyViewportOver(
        Rm2kPixelBuffer pLayer, Rm2kPixelBuffer pFrame, int pOffsetX, int pOffsetY)
    {
        var width = Math.Min(pLayer.Width - pOffsetX, pFrame.Width);
        var height = Math.Min(pLayer.Height - pOffsetY, pFrame.Height);
        if (width <= 0 || height <= 0)
        {
            return;
        }
        for (var row = 0; row < height; row++)
        {
            var source = ((pOffsetY + row) * pLayer.Width + pOffsetX) * 4;
            var destination = row * pFrame.Width * 4;
            for (var column = 0; column < width; column++)
            {
                var alpha = pLayer.Pixels[source + 3];
                if (alpha == 0)
                {
                    source += 4;
                    continue;
                }
                pFrame.Pixels[destination] = pLayer.Pixels[source];
                pFrame.Pixels[destination + 1] = pLayer.Pixels[source + 1];
                pFrame.Pixels[destination + 2] = pLayer.Pixels[source + 2];
                pFrame.Pixels[destination + 3] = alpha;
                source += 4;
                destination += 4;
            }
        }
    }

    /// <summary>
    /// Reads the visible screen out of a cached full map layer, starting at the
    /// camera offset. Pixels outside the layer stay black, which is the black
    /// border the Player shows for a map smaller than the screen.
    /// </summary>
    /// <param name="pLayer">Cached full map layer.</param>
    /// <param name="pFrame">Screen sized destination frame.</param>
    /// <param name="pOffsetX">Camera offset in pixels.</param>
    /// <param name="pClear">
    /// True for the first layer, which owns the frame. False for the upper
    /// layer, which is laid over the characters already drawn, so clearing
    /// there would erase them.
    /// </param>
    private static void CopyViewport(
        Rm2kPixelBuffer pLayer, Rm2kPixelBuffer pFrame, int pOffsetX, int pOffsetY)
    {
        pFrame.Clear();
        var width = Math.Min(pLayer.Width - pOffsetX, pFrame.Width);
        var height = Math.Min(pLayer.Height - pOffsetY, pFrame.Height);
        if (width <= 0 || height <= 0)
        {
            return;
        }
        // A map that is smaller than the screen in one direction has no room to
        // scroll in that direction, so the offset is zero there and the layer is
        // shown unshifted. That is the Player's black border case.
        pLayer.TryCopyRegion(pOffsetX, pOffsetY, width, height, pFrame, 0, 0);
    }

    /// <summary>
    /// Builds the characters of the current frame: every event with a
    /// character graphic and the hero, each with the stage the verified
    /// drawable priority assigns it.
    /// </summary>
    /// <summary>
    /// Draws the vehicles that are on this map, verified from
    /// <c>Spriteset_Map::UpdateGraphics</c>: a vehicle is only created once and
    /// only when its own start map is the current one.
    /// </summary>
    /// <remarks>
    /// A vehicle with an empty system name has no charset cell, and a vehicle
    /// whose charset is missing is reported rather than drawn as the first
    /// character, which is the same fail-closed rule the event sprites use.
    /// </remarks>
    private void DrawVehicleSprites(Rm2kPixelBuffer pFrame, int pOffsetX, int pOffsetY)
    {
        if (_vehicles.Count == 0)
        {
            return;
        }
        var charsets = LoadCharSets();
        foreach (var vehicle in _vehicles)
        {
            if (vehicle.MapId != Simulation.MapId || vehicle.CharacterName.Length == 0)
            {
                continue;
            }
            if (!charsets.TryGetValue(vehicle.CharacterName, out var charset))
            {
                Simulation.AddDiagnostic(
                    $"RM2K {Rm2kVehicle.DescribeType(vehicle.VehicleType)} references charset "
                    + $"'{vehicle.CharacterName}', which is not in CharSet.");
                continue;
            }
            var sprite = new Rm2kVehicleSprite
            {
                VehicleType = vehicle.VehicleType,
                CharacterName = vehicle.CharacterName,
                CharacterIndex = vehicle.SpriteIndex,
                MapX = vehicle.X,
                MapY = vehicle.Y,
                FacingDirection = Rm2kCharacterSprite.FacingFromLiblcfDirection(vehicle.Direction),
                Frame = Rm2kCharset.FrameMiddle,
                Altitude = vehicle.GetAltitude(),
            };
            Rm2kMapFrameRenderer.DrawVehicle(pFrame, charset, sprite, pOffsetX, pOffsetY);
        }
    }

    /// <summary>
    /// The character sprites of the current frame, built in draw order.
    /// </summary>
    /// <param name="pOffsetX">Horizontal camera offset in pixels.</param>
    /// <param name="pOffsetY">Vertical camera offset in pixels.</param>
    private List<Rm2kCharacterSprite> BuildCharacterSprites(int pOffsetX = 0, int pOffsetY = 0)
    {
        _renderedEventPages.Clear();
        var charsets = LoadCharSets();
        var sprites = new List<Rm2kCharacterSprite>();
        foreach (var mapEvent in _mapEvents)
        {
            var sprite = TryBuildEventSprite(mapEvent, charsets);
            if (sprite != null)
            {
                sprite.PixelOffsetX = pOffsetX;
                sprite.PixelOffsetY = pOffsetY;
                sprites.Add(sprite);
            }
        }
        var hero = TryBuildHeroSprite(charsets);
        if (hero != null)
        {
            // Added to, not replaced. The hero carries the step offset of the
            // unspent movement budget from TryBuildHeroSprite, and the camera
            // offset is a separate scroll applied to every sprite. Assigning
            // pOffsetX here silently dropped the step, so the hero snapped to
            // its tile while walking, which is the one thing the step budget
            // exists to prevent.
            hero.PixelOffsetX += pOffsetX;
            hero.PixelOffsetY += pOffsetY;
            sprites.Add(hero);
        }
        return sprites;
    }

    /// <summary>
    /// Draws only the highest eligible page's graphic and layer, as
    /// Game_Event::RefreshPage does. Retains the event's live movement/animation.
    /// </summary>
    private Rm2kCharacterSprite? TryBuildEventSprite(
        Rm2kMap.Event pEvent, Dictionary<string, Rm2kCharset> pCharsets)
    {
        var page = Rm2kEventPageSelector.SelectActive(pEvent, Simulation);
        _renderedEventPages[pEvent] = page;
        if (page == null || !page.Graphic.TryGetValue("character_name", out var rawName)
            || rawName is not string name || name.Length == 0)
        {
            return null;
        }
        if (!pCharsets.TryGetValue(name, out var charset))
        {
            Simulation.AddDiagnostic(
                $"RM2K event {pEvent.Id} references charset '{name}', which is not in CharSet.");
            return null;
        }
        var index = page.Graphic.TryGetValue("character_index", out var rawIndex) && rawIndex is int i ? i : 0;
        return new Rm2kCharacterSprite
        {
            Charset = charset,
            MapX = pEvent.X,
            MapY = pEvent.Y,
            CharacterIndex = index,
            Stage = Rm2kCharacterSprite.StageForLayer(page.Layer),
            FacingDirection = pEvent.FacingDirection ?? pEvent.Direction,
            Frame = pEvent.AnimationFrame,
        };
    }

    /// <summary>
    /// The hero character, verified from <c>Game_Player::ResetGraphic</c>: the
    /// first party member, or no graphic when the party is empty.
    /// </summary>
    private Rm2kCharacterSprite? TryBuildHeroSprite(Dictionary<string, Rm2kCharset> pCharsets)
    {
        var graphic = Rm2kHeroSprite.FromActor(LeadingActorCharacterName, LeadingActorCharacterIndex);
        if (graphic == null)
        {
            return null;
        }
        var request = graphic.Value;
        if (!pCharsets.TryGetValue(request.SpriteName, out var charset))
        {
            return null;
        }
        return new Rm2kCharacterSprite
        {
            Charset = charset,
            MapX = Simulation.MapX,
            MapY = Simulation.MapY,
            CharacterIndex = request.CharacterIndex,
            // The hero is drawn between tiles while a step is unspent, exactly
            // as GetSpriteX and GetSpriteY place it, and carries the animation
            // frame the walk produced.
            PixelOffsetX = Rm2kStepBudget.PixelOffsetX(
                Simulation.MapX, Simulation.RemainingStep, FacingFromFacingDirection(Simulation.FacingDirection)),
            PixelOffsetY = Rm2kStepBudget.PixelOffsetY(
                Simulation.MapY, Simulation.RemainingStep, FacingFromFacingDirection(Simulation.FacingDirection)),
            Frame = Rm2kCharacterAnimation.ClampFrame(Simulation.CharacterFrame),
            Stage = Rm2kMapFrameRenderer.SpriteStage.HeroLayer,
        };
    }

    /// <summary>
    /// The liblcf <c>Game_Character::Direction</c> behind the facing this
    /// project stores, so the step budget offsets the axis the hero is actually
    /// travelling along. Only the four cardinal facings exist in this runtime,
    /// which is all a step budget can be given.
    /// </summary>
    private static int FacingFromFacingDirection(byte pFacingDirection)
    {
        return Rm2kCharset.FacingToRow(pFacingDirection) switch
        {
            Rm2kCharset.DirectionRight => 1,
            Rm2kCharset.DirectionLeft => 3,
            Rm2kCharset.DirectionUp => 0,
            _ => 2,
        };
    }

    /// <summary>
    /// Loads the charsets the hero and the events request. The Player requests
    /// charset material from the <c>CharSet</c> directory, so a name that has
    /// no file is reported and only skips its own characters.
    /// </summary>
    private Dictionary<string, Rm2kCharset> LoadCharSets()
    {
        var result = new Dictionary<string, Rm2kCharset>(StringComparer.OrdinalIgnoreCase);
        var root = ResolveGameDirectory();
        if (root == null)
        {
            return result;
        }
        foreach (var name in CollectCharSetNames())
        {
            if (string.IsNullOrEmpty(name) || result.ContainsKey(name))
            {
                continue;
            }
            var path = Path.Combine(root, "CharSet", name + ".png");
            if (!File.Exists(path))
            {
                Simulation.AddDiagnostic($"RM2K charset '{name}.png' is missing from CharSet.");
                continue;
            }
            if (!Rm2kIndexedImage.TryLoad(path, out var image, out var error))
            {
                Simulation.AddDiagnostic($"RM2K charset '{name}.png' could not be decoded: {error}");
                continue;
            }
            result[name] = new Rm2kCharset(image);
        }
        return result;
    }

    /// <summary>
    /// Every charset name the frame can ask for: the events and the hero. Names
    /// come from the data, so nothing is guessed from a file listing.
    /// </summary>
    private IEnumerable<string> CollectCharSetNames()
    {
        foreach (var mapEvent in _mapEvents)
        {
            foreach (var page in mapEvent.Pages)
            {
                if (page.Graphic.TryGetValue("character_name", out var rawName) && rawName is string name
                    && name.Length > 0)
                {
                    yield return name;
                }
            }
        }
        if (LeadingActorCharacterName.Length > 0)
        {
            yield return LeadingActorCharacterName;
        }
    }

    /// <summary>
    /// The leading party member's charset from the LDB, which is what
    /// <c>Game_Player::ResetGraphic</c> uses. A game with no starting party has
    /// no hero graphic, which is a valid state and not a failure.
    /// </summary>
    private string LeadingActorCharacterName
    {
        get
        {
            var actorId = LeadingActorId;
            if (actorId > 0 && TryGetActorCharacterName(actorId, out var name))
            {
                return name;
            }
            return "";
        }
    }

    private int LeadingActorCharacterIndex
    {
        get
        {
            var actorId = LeadingActorId;
            if (actorId > 0 && TryGetActorCharacterIndex(actorId, out var index))
            {
                return index;
            }
            return 0;
        }
    }

    /// <summary>
    /// The first starting actor from the LDB <c>system.party</c>, which
    /// <c>Game_Party::SetupNewGame</c> fills and
    /// <c>Game_Player::ResetGraphic</c> then reads through
    /// <c>GetActor(0)</c>. Zero means the game defines no starting party, which
    /// the Player renders without a hero graphic.
    /// </summary>
    private int LeadingActorId
    {
        get
        {
            var system = SystemData;
            if (system == null || !system.TryGetValue("party", out var rawParty))
            {
                return 0;
            }
            var party = rawParty;
            if (party.VariantType == Godot.Variant.Type.Array)
            {
                foreach (var raw in party.AsGodotArray())
                {
                    if (raw.VariantType == Godot.Variant.Type.Int)
                    {
                        return raw.AsInt32();
                    }
                }
                return 0;
            }
            if (party.VariantType == Godot.Variant.Type.PackedInt32Array)
            {
                var values = party.AsInt32Array();
                return values.Length > 0 ? values[0] : 0;
            }
            return 0;
        }
    }

    /// <summary>
    /// Resolves the command list for a nested CallEvent. RM2K page indices are
    /// one-based; index 0 addresses the first page of the event.
    /// </summary>
    private IReadOnlyList<Rm2kMap.EventCommand>? ResolveEventCommands(int pEventId, int pPageIndex)
    {
        foreach (var mapEvent in _mapEvents)
        {
            if (mapEvent.Id != pEventId || mapEvent.Pages.Count == 0)
            {
                continue;
            }
            var pageNumber = pPageIndex <= 0 ? 0 : pPageIndex - 1;
            if (pageNumber >= mapEvent.Pages.Count)
            {
                return null;
            }
            return mapEvent.Pages[pageNumber].Commands;
        }
        return null;
    }

    /// <summary>
    /// Copies a pixel buffer, so a cached layer can never be modified by the
    /// frame that is composited from it.
    /// </summary>
    private static Rm2kPixelBuffer CopyOf(Rm2kPixelBuffer pSource)
    {
        var copy = new Rm2kPixelBuffer(pSource.Width, pSource.Height);
        Array.Copy(pSource.Pixels, copy.Pixels, pSource.Pixels.Length);
        return copy;
    }

    private static bool TryReadInt(Godot.Collections.Dictionary pData, string pKey, out int pValue)
    {
        if (!pData.TryGetValue(pKey, out var rawValue))
        {
            pValue = 0;
            return false;
        }
        try
        {
            pValue = (int)rawValue;
            return true;
        }
        catch (InvalidCastException)
        {
            pValue = 0;
            return false;
        }
    }

    private static bool TryReadBool(Godot.Collections.Dictionary pData, string pKey, out bool pValue)
    {
        pValue = false;
        if (!pData.TryGetValue(pKey, out var rawValue))
        {
            return false;
        }
        try
        {
            pValue = (bool)rawValue;
            return true;
        }
        catch (InvalidCastException)
        {
            return false;
        }
    }

    private PluginOperationResult Fail(PluginErrorCode pCode, string pMessage, string pPhase)
    {
        return PluginOperationResult.Failed(PluginError.Create(pCode, pMessage, _pluginId, pPhase));
    }
}
