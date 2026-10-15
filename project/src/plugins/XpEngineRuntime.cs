using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniversalRPG.Core;
using UniversalRPG.Rgss;

namespace UniversalRPG.Plugins;

/// <summary>
/// Runs an RPG Maker XP project's own Ruby scripts, in process.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is the reading and running half of the engine, and not
/// a picture.</strong> The project's <c>Data/Scripts.rxdata</c> is read and
/// executed, so a game's own commands and menus exist as values -- and
/// nothing here renders, takes input, plays audio or saves, which is why the
/// plugin declares <c>Runtime</c> and not <c>Rendering</c>.
/// </para>
/// <para>
/// <strong>And every script that fails is named with its reason.</strong> A
/// runtime whose empty catches hide a failing script looks exactly like a
/// game that does nothing.
/// </para>
/// </remarks>
public sealed class XpEngineRuntime : IEngineRuntime
{
    private readonly string _pluginId;
    private readonly PluginGameInfo _game;
    private readonly VirtualClock _clock = new();
    private RgssRuntimeInfo? _runtimeInfo;
    private RubyInterpreter? _interpreter;
    private RgssSkriptHost? _scriptHost;
    private RgssLaufzeit? _laufzeit;
    private RgssDatenHost? _datenHost;
    private bool _scriptsRun;

    /// <summary>
    /// The scripts and updates that failed, by name and reason.
    /// </summary>
    public IReadOnlyList<string> ScriptProblems => _scriptProblems;

    /// <summary>
    /// The setup steps the RGSS runtime could not run, with their reason.
    /// </summary>
    /// <remarks>
    /// <strong>And this is the same shape as <see cref="ScriptProblems"/> on purpose:</strong> a
    /// global the engine's own <c>main</c> would have set and this runtime could not is a gap the
    /// game will hit later, and it has to be readable by name before that happens rather than
    /// after.
    /// </remarks>
    public IReadOnlyList<string> SetupProblems => _laufzeit == null
        ? (IReadOnlyList<string>)Array.Empty<string>()
        : _laufzeit.Fehlgeschlagen;

    /// <summary>
    /// What the game's own state reads after the runtime's setup sequence.
    /// </summary>
    /// <remarks>
    /// <strong>And this is the other half of <see cref="SetupProblems"/>.</strong> A step that
    /// threw names the step; these say what the game holds afterwards, and a reader who has both
    /// does not have to guess which value is the nil. Measured: the last step reported
    /// *"undefined operator '%' for a Integer and a Nil"*, which says the first operand arrives and
    /// not which value is missing.
    /// </remarks>
    public IReadOnlyList<string> SetupState => _laufzeit == null
        ? (IReadOnlyList<string>)Array.Empty<string>()
        : _laufzeit.Zustand;

    /// <summary>
    /// The names the game asked for and the runtime could not answer.
    /// </summary>
    /// <remarks>
    /// <strong>And this is the demand list, and it is the reason it is
    /// exposed.</strong> Measured: the setup and the first frames report no
    /// problems at all, and nothing is drawn -- because the reader answers an
    /// unknown name with nil and a diagnostic rather than an exception, so the
    /// gaps are silent. **A list of names is what a game actually needs**, and
    /// it is the honest way to find the next class instead of guessing which
    /// one to build.
    /// </remarks>
    public IReadOnlyList<string> Diagnostics => _interpreter == null
        ? (IReadOnlyList<string>)Array.Empty<string>()
        : _interpreter.Diagnostics;

    /// <summary>
    /// Runs the game's own scene loop, bounded by <see cref="FrameBudget"/>.
    /// </summary>
    /// <remarks>
    /// <strong>And this is a method and not part of <c>Start</c>, because the
    /// loop belongs to the game and blocking is what it does.</strong> XP's
    /// scenes are `loop do ... end` and <c>Graphics.update</c> presents one frame
    /// and returns; a host that plays a game runs this on its own thread, and a
    /// host that measures sets a budget and calls it here. **And measured, a
    /// runtime that never ran it never ran a scene's setup** -- the windows and
    /// the sprites are built inside `main`, and nothing was drawn while every
    /// report said zero problems.
    /// </remarks>
    public void SpielSchleifeStarten()
    {
        if (_interpreter == null || _grafik == null)
        {
            return;
        }

        try
        {
            _interpreter.RunProgram(new RubyParser(new RubyLexer(
                "$scene.main if $scene && $scene.respond_to?(:main)")
                .Tokenize()).ParseProgram());
        }
        catch (RgssFrameBudgetErreicht)
        {
            // **And the budget is the end of the run and not a problem.** A
            // game's loop has no exit until the player leaves the scene, so a
            // measured run ends here.
        }
        catch (Exception ausnahme)
        {
            Melde("scene", "the game's own loop", ausnahme);
        }
    }

    /// <summary>
    /// How many frames the game's own loop may run before the run ends.
    /// </summary>
    /// <remarks>
    /// <strong>And zero is what a player wants and a measurement does not.</strong>
    /// A game's loop has no exit until the player leaves the scene, so a run that
    /// measures sets a budget -- and a run that plays does not.
    /// </remarks>
    /// <summary>The frames the game's own loop presented.</summary>
    public int Frames => _grafik?.Frames ?? 0;

    public int FrameBudget
    {
        get => _grafik?.Budget ?? 0;
        set
        {
            if (_grafik != null)
            {
                _grafik.Budget = value;
            }
        }
    }

    private RgssGrafikHost? _grafik;

    private readonly List<string> _scriptProblems = new();

    /// <summary>How many distinct failures the run remembers.</summary>
    private const int MaxProblems = 64;

    /// <summary>
    /// Records one failure by name and reason, once, up to a bound.
    /// </summary>
    /// <remarks>
    /// <strong>And this exists because the empty catches that were here made
    /// a failing game look like a game that does nothing.</strong> The bound
    /// keeps a failure that repeats every frame from growing without end, and
    /// the dedupe keeps one broken script from filling the list.
    /// </remarks>
    private void Melde(string pWo, string pName, Exception pAusnahme)
    {
        var meldung = pWo + " " + pName + ": " + pAusnahme.Message;
        if (_scriptProblems.Contains(meldung) || _scriptProblems.Count >= MaxProblems)
        {
            return;
        }
        _scriptProblems.Add(meldung);
    }

    public XpEngineRuntime(string pPluginId, PluginGameInfo pGame)
    {
        _pluginId = pPluginId ?? throw new ArgumentNullException(nameof(pPluginId));
        _game = pGame ?? throw new ArgumentNullException(nameof(pGame));
    }

    public PluginRuntimeState State { get; private set; } = PluginRuntimeState.Created;
    public RgssRuntimeInfo? RuntimeInfo => _runtimeInfo;
    public int SimulationTicks => _clock.GetSimulationTicks();

    public PluginOperationResult Initialize(EnginePluginRuntimeContext pContext)
    {
        if (State != PluginRuntimeState.Created)
        {
            return Fail(PluginErrorCode.InvalidLifecycleTransition, "XP runtime was already initialized.", "initialize");
        }
        if (pContext == null || pContext.Game == null)
        {
            return Fail(PluginErrorCode.InvalidGame, "XP runtime context is missing the detected game.", "initialize");
        }
        if (!pContext.Game.EngineId.Equals(_pluginId, StringComparison.Ordinal)
            || !pContext.Selection.Plugin.Metadata.Id.Equals(_pluginId, StringComparison.Ordinal))
        {
            return Fail(PluginErrorCode.UnsupportedEngine, "The XP runtime context does not match this plugin.", "initialize");
        }

        // Use the base RGSS initialization for metadata
        var baseRuntime = new RgssEngineRuntime(
            _pluginId, "xp", "rgss1", ".rxdata", ".rgssad", _game);
        var baseResult = baseRuntime.Initialize(pContext);
        if (!baseResult.Success)
        {
            return baseResult;
        }

        _runtimeInfo = baseRuntime.RuntimeInfo;

        // Now set up Ruby execution
        try
        {
            SetupRubyRuntime();
        }
        catch (Exception ex)
        {
            return Fail(PluginErrorCode.InvalidGame,
                $"Failed to initialize XP Ruby runtime: {ex.Message}", "initialize-ruby");
        }

        State = PluginRuntimeState.Initialized;
        return PluginOperationResult.Succeeded(new[]
        {
            PluginDiagnostic.Info(
                "xp.runtime-initialized",
                $"Initialized complete XP RGSS runtime with Ruby execution; {_runtimeInfo?.InspectedFileCount ?? 0} files inspected, scripts loaded: {_scriptHost?.Anzahl ?? 0}.",
                _pluginId),
        });
    }

    private void SetupRubyRuntime()
    {
        var gameDir = _game.GameDirectory;
        if (!Directory.Exists(gameDir))
        {
            throw new DirectoryNotFoundException($"Game directory not found: {gameDir}");
        }

        // Load script host
        var scriptsPath = Path.Combine(gameDir, "Data", "Scripts.rxdata");
        if (!File.Exists(scriptsPath))
        {
            throw new FileNotFoundException($"Scripts.rxdata not found at {scriptsPath}");
        }

        _scriptHost = RgssSkriptHost.Lese(scriptsPath, out var scriptError);
        if (_scriptHost == null)
        {
            throw new InvalidOperationException($"Failed to load scripts: {scriptError}");
        }

        // Load data host
        _datenHost = RgssDatenHost.Oeffne(gameDir, out var dataError);
        if (_datenHost == null)
        {
            throw new InvalidOperationException($"Failed to open data host: {dataError}");
        }

        // **And the interpreter gets ONE host that answers both.** It used to
        // get the script host, and the runtime loaded its data through the data
        // host -- and `load_data` inside the game then reached the wrong one:
        // measured, `Game_Map#setup` read `Data/Map001.rxdata` and got nil, so
        // `$game_map.width` was nil and the map was never read.
        // **And the chain is the data host and then the game's own host.**
        // `Graphics` used to be answered by a host here, and that was a second
        // source of truth for one module: measured, the reader resolves
        // `Graphics.width` on the module itself, so the host never fired and
        // the empty stub was what answered nil. **And one module has one
        // place.**
        // **And the chain is data, then the frame, then the game's own host.**
        // The frame host is reachable now because the built-in module no longer
        // answers `update` -- and a frame has to be the host's work, because the
        // surface will be.
        _grafik = new RgssGrafikHost { Fallback = _scriptHost };
        _datenHost.Fallback = _grafik;
        _interpreter = new RubyInterpreter(_datenHost);

        // Define built-in RGSS classes before running scripts
        DefineBuiltinClasses();

        // Run all game scripts
        RunGameScripts();

        // Initialize runtime globals
        // **And the extension is XP's own**: the data host takes the name as written, and a
        // runtime that leaves it out gets thirteen refusals and a game with no data.
        _laufzeit = new RgssLaufzeit(_interpreter, _datenHost, ".rxdata");
        _laufzeit.Startet();



        _scriptsRun = true;
    }

    private void DefineBuiltinClasses()
    {
        if (_interpreter == null) return;

        // Define core RGSS built-in classes that the game's scripts expect
        // These are minimal stubs - the game's scripts will extend them

        var builtinClasses = new[]
        {
            // Core modules
            "module RGSS\nend",
            // **And `Graphics` is not an empty stub, because a game reads it
            // before it draws anything.** Measured: `Game_Player#center` clamps
            // the player against `Graphics.width` and `Graphics.height`, and an
            // empty module answers nil -- so `moveto` reported *"undefined
            // operator '+' for a Nil and a Nil"*, and that was the last step
            // between that game and its first frame.
            //
            // **And the state lives in globals and not in module instance
            // variables, and that is measured:** a module with `@breite` and
            // `def self.breite; @breite; end` answers nil in this reader, so a
            // module that kept its numbers that way would report a picture of
            // no size. **And the numbers are the engine's own:** RGSS 1.0 opens
            // 640x480, and the game's `Game.ini` carries no size at all --
            // measured, it has `Library`, `Scripts`, `Title` and the RTP names.
            //
            // **And this does not draw, and it says so.** `update` counts frames
            // and returns, because there is no surface yet; the transitions and
            // the fades are empty for the same reason. **And the counter is
            // real**, because `Graphics.frame_count` is what a game times itself
            // with.
            "module Graphics\n"
                + "  $graphics_width = 640\n"
                + "  $graphics_height = 480\n"
                + "  $graphics_frames = 0\n"
                + "  $graphics_rate = 40\n"
                + "  $graphics_brightness = 255\n"
                + "  def self.width; $graphics_width; end\n"
                + "  def self.height; $graphics_height; end\n"
                + "  def self.frame_count; $graphics_frames; end\n"
                + "  def self.frame_rate; $graphics_rate; end\n"
                + "  def self.frame_rate=(v); $graphics_rate = v; end\n"
                + "  def self.brightness; $graphics_brightness; end\n"
                + "  def self.brightness=(v); $graphics_brightness = v; end\n"
                + "  def self.resize_screen(w, h);\n"
                + "    $graphics_width = w; $graphics_height = h; end\n"
                + "  def self.freeze; end\n"
                + "  def self.transition; end\n"
                + "  def self.transition=(v); end\n"
                + "  def self.fadein(t); end\n"
                + "  def self.fadeout(t); end\n"
                + "end",
            // **And `Input` is what a scene reads every frame, and an empty
            // module answers nil for every one of those reads.** Measured:
            // `Scene_Title#update` asks `Input.trigger?(Input::C)`, and a nil
            // there is a title screen that cannot be started -- not because the
            // game is wrong, but because the runtime never said whether a key
            // is down.
            //
            // **And this runtime is headless, and it says so.** There is no
            // keyboard, so nothing is pressed: `trigger?`, `press?` and
            // `repeat?` answer false and `dir4`/`dir8` answer 0. **And that is
            // not a lie** -- no key arrived -- and it is the difference between
            // a game that waits for a player and a game that cannot tell
            // whether one is there. **And the lists are globals so that a host
            // can fill them**: `$input_pressed` and `$input_triggered` are the
            // two names a host writes, and the module reads.
            //
            // **And the key numbers are RGSS 1.0's own.** A game writes
            // `Input.trigger?(Input::C)` and never the number, so the values
            // matter to the direction helpers and to a host that feeds keys.
            "module Input\n"
                + "  DOWN = 2; LEFT = 4; RIGHT = 6; UP = 8\n"
                + "  A = 11; B = 12; C = 13; X = 14; Y = 15; Z = 16\n"
                + "  L = 17; R = 18; SHIFT = 19; CTRL = 20; ALT = 21\n"
                + "  F5 = 25; F6 = 26; F7 = 27; F8 = 28; F9 = 29\n"
                + "  $input_pressed = []\n"
                + "  $input_triggered = []\n"
                + "  $input_repeated = []\n"
                + "  def self.update\n"
                + "    $input_repeated = []\n"
                + "    $input_triggered = []\n"
                + "  end\n"
                + "  def self.trigger?(k); $input_triggered.include?(k); end\n"
                + "  def self.press?(k); $input_pressed.include?(k); end\n"
                + "  def self.repeat?(k)\n"
                + "    $input_repeated.include?(k) || $input_triggered.include?(k); end\n"
                + "  def self.dir4\n"
                + "    return 2 if $input_pressed.include?(DOWN)\n"
                + "    return 4 if $input_pressed.include?(LEFT)\n"
                + "    return 6 if $input_pressed.include?(RIGHT)\n"
                + "    return 8 if $input_pressed.include?(UP)\n"
                + "    0\n"
                + "  end\n"
                + "  def self.dir8\n"
                + "    d = dir4\n"
                + "    return d if d != 0\n"
                + "    0\n"
                + "  end\n"
                + "end",
            "module Font\nend",
            "module Color\nend",
            "module Tone\nend",
            "module Rect\nend",
            "module Viewport\nend",
            "module Sprite\nend",
            "module Bitmap\nend",
            "module Plane\nend",
            "module Tilemap\nend",
            "module Window\nend",
            "module Window_Base\nend",
            "module Window_Selectable\nend",
            "module Window_Command\nend",
            "module Window_Help\nend",
            "module Window_Message\nend",
            "module Window_Gold\nend",
            "module Window_Status\nend",
            "module Window_SaveFile\nend",
            "module Scene_Base\nend",
            "module Scene_Title\nend",
            "module Scene_Map\nend",
            "module Scene_Menu\nend",
            "module Scene_Item\nend",
            "module Scene_Skill\nend",
            "module Scene_Equip\nend",
            "module Scene_Status\nend",
            "module Scene_File\nend",
            "module Scene_Save\nend",
            "module Scene_Load\nend",
            "module Scene_Battle\nend",
            "module Scene_Gameover\nend",
            "module Scene_Debug\nend",
            "module Scene_None\nend",
            "module Game_Temp\nend",
            "module Game_System\nend",
            "module Game_Switches\nend",
            "module Game_Variables\nend",
            "module Game_SelfSwitches\nend",
            "module Game_Screen\nend",
            "module Game_Picture\nend",
            "module Game_Map\nend",
            "module Game_Player\nend",
            "module Game_Character\nend",
            "module Game_Event\nend",
            "module Game_Battler\nend",
            "module Game_Actor\nend",
            "module Game_Enemy\nend",
            "module Game_Action\nend",
            "module Game_Actors\nend",
            "module Game_Party\nend",
            "module Game_Troop\nend",
            "module Game_Interpreter\nend",
            "module RPG\nend",
            "module RPG::Actor\nend",
            "module RPG::Class\nend",
            "module RPG::Skill\nend",
            "module RPG::Item\nend",
            "module RPG::Weapon\nend",
            "module RPG::Armor\nend",
            "module RPG::Enemy\nend",
            "module RPG::Troop\nend",
            "module RPG::State\nend",
            "module RPG::Animation\nend",
            "module RPG::Tileset\nend",
            "module RPG::CommonEvent\nend",
            "module RPG::System\nend",
            "module RPG::AudioFile\nend",
            "module RPG::Tileset\nend",
            "module RPG::Map\nend",
            "module RPG::MapInfo\nend",
        };

        foreach (var code in builtinClasses)
        {
            try
            {
                var tokens = new RubyLexer(code).Tokenize();
                var nodes = new RubyParser(tokens).ParseProgram();
                _interpreter!.RunProgram(nodes);
            }
            catch (Exception ausnahme)
            {
                // **And a stub that does not parse is a bug in this file**,
                // and not something to pass over in silence.
                Melde("built-in class", code.Split('\n')[0], ausnahme);
            }
        }
    }

    private void RunGameScripts()
    {
        if (_interpreter == null || _scriptHost == null) return;

        var scriptNames = _scriptHost.Namen.ToList();
        foreach (var name in scriptNames)
        {
            var bytes = _scriptHost.ReadScript(name, true);
            if (bytes == null) continue;

            try
            {
                var text = System.Text.Encoding.UTF8.GetString(bytes);
                var tokens = new RubyLexer(text).Tokenize();
                var nodes = new RubyParser(tokens).ParseProgram();
                _interpreter.RunProgram(nodes);
            }
            catch (RubyParseException ausnahme)
            {
                Melde("script", name, ausnahme);
            }
            catch (Exception ausnahme)
            {
                Melde("script", name, ausnahme);
            }
        }

        System.Console.WriteLine($"XP: {scriptNames.Count} scripts, {scriptNames.Count - _scriptProblems.Count} parsed and executed, {_scriptProblems.Count} failed");
    }

    public PluginOperationResult Start()
    {
        if (State != PluginRuntimeState.Initialized)
        {
            return Fail(PluginErrorCode.InvalidLifecycleTransition, $"XP runtime cannot start from state {State}.", "start");
        }
        State = PluginRuntimeState.Running;
        return PluginOperationResult.Succeeded();
    }

    public PluginOperationResult Update(double pDeltaSeconds)
    {
        if (State != PluginRuntimeState.Running)
        {
            return Fail(PluginErrorCode.InvalidLifecycleTransition, $"XP runtime cannot update from state {State}.", "update");
        }
        if (double.IsNaN(pDeltaSeconds) || double.IsInfinity(pDeltaSeconds) || pDeltaSeconds < 0)
        {
            return Fail(PluginErrorCode.InvalidLifecycleTransition, "Delta time must be finite and non-negative.", "update");
        }
        _clock.ProcessFrame(pDeltaSeconds);

        // Run game loop if scripts are loaded
        if (_scriptsRun && _interpreter != null)
        {
            RunGameLoop();
        }

        return PluginOperationResult.Succeeded();
    }

    private void RunGameLoop()
    {
        // Run the game's main loop
        // This would typically call $scene.main or similar
        try
        {
            // Update global game objects
            UpdateGlobals();

            // Run scene update if there's a current scene
            RunSceneUpdate();
        }
        catch (Exception ex)
        {
            Melde("game loop", "one frame", ex);
        }
    }

    private void UpdateGlobals()
    {
        if (_interpreter == null) return;

        // **And each global is named when it fails.** One try around all five reported "the
        // per-frame globals", and the first thing a reader needs is which of them threw: a game
        // whose `$game_system` is nil and one whose `$game_map` is nil are two different problems
        // with two different fixes. Measured on a real game, the whole failure was one of these
        // five and the message did not say which.
        UpdateGlobal("$game_temp");
        UpdateGlobal("$game_system");
        UpdateGlobal("$game_screen");
        UpdateGlobal("$game_map");
        UpdateGlobal("$game_player");
    }

    /// <summary>
    /// Runs one global's per-frame update, and names it if it throws.
    /// </summary>
    private void UpdateGlobal(string pName)
    {
        if (_interpreter == null) return;

        try
        {
            _interpreter.RunProgram(new RubyParser(new RubyLexer(
                $"{pName}.update if {pName}.respond_to?(:update)").Tokenize()).ParseProgram());
        }
        catch (Exception ausnahme)
        {
            // **And an update that throws every frame is reported once**, because a silent one
            // turns a broken game into a still picture.
            Melde("update", pName, ausnahme);
        }
    }

    /// <summary>
    /// Runs one statement of the frame, and names it if it throws.
    /// </summary>
    private void RufeSzene(string pQuelltext)
    {
        if (_interpreter == null) return;

        try
        {
            _interpreter.RunProgram(new RubyParser(
                new RubyLexer(pQuelltext).Tokenize()).ParseProgram());
        }
        catch (Exception ausnahme)
        {
            Melde("scene", pQuelltext, ausnahme);
        }
    }

    private void RunSceneUpdate()
    {
        if (_interpreter == null) return;

        try
        {
            // **And this is the body of the engine's own loop, and not the
            // loop.** XP's `Scene_Title#main` is
            // `loop do Graphics.update; Input.update; update; break if $scene != self end`
            // -- **and a runtime that called `main` would sit in that loop
            // forever**, because there is no scene change to break on. **So the
            // runtime is the loop**, and what it runs each frame is the three
            // statements inside it.
            //
            // **And each one is named if it throws**, because "the scene failed"
            // says nothing about whether it was the picture, the input or the
            // game's own update.
            RufeSzene("Graphics.update");
            RufeSzene("Input.update");
            RufeSzene("$scene.update if $scene");
        }
        catch (Exception ausnahme)
        {
            Melde("scene", "the current scene", ausnahme);
        }
    }

    public PluginOperationResult Stop()
    {
        if (State != PluginRuntimeState.Initialized && State != PluginRuntimeState.Running)
        {
            return Fail(PluginErrorCode.InvalidLifecycleTransition, $"XP runtime cannot stop from state {State}.", "stop");
        }
        State = PluginRuntimeState.Stopped;
        return PluginOperationResult.Succeeded();
    }

    public void Dispose()
    {
        State = PluginRuntimeState.Disposed;
        _runtimeInfo = null;
        _interpreter = null;
        _scriptHost = null;
        _laufzeit = null;
        _datenHost = null;
    }

    private PluginOperationResult Fail(PluginErrorCode pCode, string pMessage, string pPhase)
    {
        return PluginOperationResult.Failed(PluginError.Create(pCode, pMessage, _pluginId, pPhase));
    }
}