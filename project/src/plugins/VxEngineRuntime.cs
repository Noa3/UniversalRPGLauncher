using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniversalRPG.Core;
using UniversalRPG.Rgss;

namespace UniversalRPG.Plugins;

/// <summary>
/// Runs an RPG Maker VX project's own Ruby scripts, in process.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is the reading and running half of the engine, and not
/// a picture.</strong> The project's <c>Data/Scripts.rvdata</c> is read and
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
public sealed class VxEngineRuntime : IEngineRuntime
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

    public VxEngineRuntime(string pPluginId, PluginGameInfo pGame)
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
            return Fail(PluginErrorCode.InvalidLifecycleTransition, "VX runtime was already initialized.", "initialize");
        }
        if (pContext == null || pContext.Game == null)
        {
            return Fail(PluginErrorCode.InvalidGame, "VX runtime context is missing the detected game.", "initialize");
        }
        if (!pContext.Game.EngineId.Equals(_pluginId, StringComparison.Ordinal)
            || !pContext.Selection.Plugin.Metadata.Id.Equals(_pluginId, StringComparison.Ordinal))
        {
            return Fail(PluginErrorCode.UnsupportedEngine, "The VX runtime context does not match this plugin.", "initialize");
        }

        // Use the base RGSS initialization for metadata
        var baseRuntime = new RgssEngineRuntime(
            _pluginId, "vx", "RGSS202", ".rvdata", ".rvdata", _game);
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
                $"Failed to initialize VX Ruby runtime: {ex.Message}", "initialize-ruby");
        }

        State = PluginRuntimeState.Initialized;
        return PluginOperationResult.Succeeded(new[]
        {
            PluginDiagnostic.Info(
                "vx.runtime-initialized",
                $"Initialized complete VX RGSS runtime with Ruby execution; {_runtimeInfo?.InspectedFileCount ?? 0} files inspected, scripts loaded: {_scriptHost?.Anzahl ?? 0}.",
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
        var scriptsPath = Path.Combine(gameDir, "Data", "Scripts.rvdata");
        if (!File.Exists(scriptsPath))
        {
            throw new FileNotFoundException($"Scripts.rvdata not found at {scriptsPath}");
        }

        _scriptHost = RgssSkriptHost.Lese(scriptsPath, out var scriptError);
        if (_scriptHost == null)
        {
            throw new InvalidOperationException($"Failed to load scripts: {scriptError}");
        }

        // Load data host using factory method
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
        _datenHost.Fallback = _scriptHost;
        _interpreter = new RubyInterpreter(_datenHost);

        // Define built-in RGSS classes before running scripts
        DefineBuiltinClasses();

        // Run all game scripts
        RunGameScripts();

        // Initialize runtime globals
        _laufzeit = new RgssLaufzeit(_interpreter, _datenHost, ".rvdata");
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
                + "  def self.update; $graphics_frames = $graphics_frames + 1; end\n"
                + "  def self.wait(n); $graphics_frames = $graphics_frames + n; end\n"
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

        System.Console.WriteLine($"VX: {scriptNames.Count} scripts, {scriptNames.Count - _scriptProblems.Count} parsed and executed, {_scriptProblems.Count} failed");
    }

    public PluginOperationResult Start()
    {
        if (State != PluginRuntimeState.Initialized)
        {
            return Fail(PluginErrorCode.InvalidLifecycleTransition, $"VX runtime cannot start from state {State}.", "start");
        }
        State = PluginRuntimeState.Running;
        return PluginOperationResult.Succeeded();
    }

    public PluginOperationResult Update(double pDeltaSeconds)
    {
        if (State != PluginRuntimeState.Running)
        {
            return Fail(PluginErrorCode.InvalidLifecycleTransition, $"VX runtime cannot update from state {State}.", "update");
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

        try
        {
            // Update $game_temp, $game_system, etc.
            _interpreter.RunProgram(new RubyParser(new RubyLexer(
                "$game_temp.update if $game_temp.respond_to?(:update)").Tokenize()).ParseProgram());
            _interpreter.RunProgram(new RubyParser(new RubyLexer(
                "$game_system.update if $game_system.respond_to?(:update)").Tokenize()).ParseProgram());
            _interpreter.RunProgram(new RubyParser(new RubyLexer(
                "$game_screen.update if $game_screen.respond_to?(:update)").Tokenize()).ParseProgram());
            _interpreter.RunProgram(new RubyParser(new RubyLexer(
                "$game_map.update if $game_map.respond_to?(:update)").Tokenize()).ParseProgram());
            _interpreter.RunProgram(new RubyParser(new RubyLexer(
                "$game_player.update if $game_player.respond_to?(:update)").Tokenize()).ParseProgram());
        }
        catch (Exception ausnahme)
        {
            // **And an update that throws every frame is reported once**,
            // because a silent one turns a broken game into a still picture.
            Melde("update", "the per-frame globals", ausnahme);
        }
    }

    private void RunSceneUpdate()
    {
        if (_interpreter == null) return;

        try
        {
            // Run current scene's main/update
            _interpreter.RunProgram(new RubyParser(new RubyLexer(
                "$scene.main if $scene && $scene.respond_to?(:main)").Tokenize()).ParseProgram());
            _interpreter.RunProgram(new RubyParser(new RubyLexer(
                "$scene.update if $scene && $scene.respond_to?(:update)").Tokenize()).ParseProgram());
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
            return Fail(PluginErrorCode.InvalidLifecycleTransition, $"VX runtime cannot stop from state {State}.", "stop");
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