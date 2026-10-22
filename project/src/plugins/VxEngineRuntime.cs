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
    public IReadOnlyList<string> ScriptProblems
    {
        get { lock (_lifecycleGate) return _scriptProblems.ToArray(); }
    }

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
        if (_frameSession != null)
            throw new InvalidOperationException("A persistent scene session already owns this interpreter.");
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
        finally
        {
            // The diagnostic entry unwinds the Ruby stack; it cannot resume it.
            // Do not drive the legacy per-frame loop after this run has ended.
            if (State == PluginRuntimeState.Running)
            {
                State = PluginRuntimeState.Stopped;
            }
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
    public int Frames
    {
        get { lock (_lifecycleGate) return _frameSession?.Frames ?? _grafik?.Frames ?? 0; }
    }

    public int FrameBudget
    {
        get { lock (_lifecycleGate) return _grafik?.Budget ?? 0; }
        set
        {
            lock (_lifecycleGate)
            {
                if (_frameSession != null)
                    throw new InvalidOperationException("A persistent scene session owns the frame budget.");
                if (_grafik != null) _grafik.Budget = value;
            }
        }
    }

    private RgssGrafikHost? _grafik;
    private RgssFrameSession? _frameSession;
    private readonly object _lifecycleGate = new();
    private bool _updateInProgress;

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
        // **And the chain is data, then the frame, then the game's own host.**
        // The frame host is reachable now because the built-in module no longer
        // answers `update` -- and a frame has to be the host's work, because the
        // surface will be.
        _grafik = new RgssGrafikHost { Fallback = _scriptHost, Wurzel = gameDir };
        _datenHost.Fallback = _grafik;
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
            // **And `Sprite` is the second, and the game said so by name:**
            // *"Sprite has no method 'bitmap='"*. A sprite is what every visible
            // thing in an RPG Maker game is -- the player, the events, the
            // pictures -- so this class is on the path to a first frame.
            //
            // **And it holds its state and does not draw**, for the same reason
            // `Window` does: there is no surface. **And its fields are real**,
            // because a game sets `bitmap`, `x`, `y`, `z`, `opacity`, `src_rect`,
            // `ox`, `oy` and `visible` and reads them back.
            "class Sprite\n"
                + "  def initialize(viewport = nil)\n"
                + "    @viewport = viewport\n"
                + "    @bitmap = nil\n"
                + "    @src_rect = Rect.new\n"
                + "    @x = 0; @y = 0; @z = 0\n"
                + "    @ox = 0; @oy = 0\n"
                + "    @opacity = 255\n"
                + "    @visible = true\n"
                + "    @angle = 0\n"
                + "    @zoom_x = 1.0; @zoom_y = 1.0\n"
                + "    @mirror = false\n"
                + "    @bush_depth = 0\n"
                + "    @blend_type = 0\n"
                + "    @color = Color.new\n"
                + "    @tone = Tone.new\n"
                + "  end\n"
                + "  attr_accessor :bitmap\n"
                + "  attr_accessor :src_rect\n"
                + "  attr_accessor :x\n"
                + "  attr_accessor :y\n"
                + "  attr_accessor :z\n"
                + "  attr_accessor :ox\n"
                + "  attr_accessor :oy\n"
                + "  attr_accessor :opacity\n"
                + "  attr_accessor :visible\n"
                + "  attr_accessor :angle\n"
                + "  attr_accessor :zoom_x\n"
                + "  attr_accessor :zoom_y\n"
                + "  attr_accessor :mirror\n"
                + "  attr_accessor :bush_depth\n"
                + "  attr_accessor :blend_type\n"
                + "  attr_accessor :color\n"
                + "  attr_accessor :tone\n"
                + "  attr_reader :viewport\n"
                + "  def update; end\n"
                + "  def dispose; end\n"
                + "  def disposed?; false; end\n"
                + "end",
            // **And `Bitmap` is what a window builds its contents out of, and
            // the game asked for it without a name of its own:** *"nil has no
            // method 'clear'"* is `Window_Base#initialize` calling `clear` on the
            // bitmap it made -- so the bitmap came back nil, because there was no
            // `Bitmap` at all.
            //
            // **And it holds a size and does not draw.** `width`, `height`,
            // `clear` and `dispose` are real, and the drawing calls are not here
            // yet -- **and they will be named the same way this class was**, by
            // the game asking for them.
            "class Bitmap\n"
                + "  def initialize(w, h = nil)\n"
                + "    @width = w; @height = h || w\n"
                + "    @font = Font.new\n"
                + "    @disposed = false\n"
                + "  end\n"
                + "  attr_reader :width\n"
                + "  attr_reader :height\n"
                + "  attr_accessor :font\n"
                + "  def clear; end\n"
                + "  def dispose; @disposed = true; end\n"
                + "  def disposed?; @disposed; end\n"
                + "  def get_pixel(x, y); nil; end\n"
                + "  def set_pixel(x, y, c); end\n"
                + "  def blt(x, y, src, rect = nil, opacity = 255); end\n"
                + "  def blt(x, y, src, rect, opacity); end\n"
                + "  def stretch_blt(dr, src, sr = nil, opacity = 255); end\n"
                + "  def fill_rect(x, y, w, h, c); end\n"
                + "  def fill_rect(r, c); end\n"
                + "  def gradient_fill_rect(x, y, w, h, c1, c2, v = false); end\n"
                + "  def draw_text(x, y, w, h, s, a = 0); end\n"
                + "  def draw_text(r, s, a = 0); end\n"
                + "  def text_size(s); [0, 0]; end\n"
                + "  def hline(x, y, w, c = nil); end\n"
                + "  def vline(x, y, h, c = nil); end\n"
                + "end",
            "module Plane\nend",
            "module Tilemap\nend",
            // **And `Window` is the first class the game's own setup builds.**
            // Measured, and it named itself: *"Window does not have initialize,
            // so super has no method to call"*, *"self has no method 'active'"*,
            // *"'contents'"*, *"'contents='"*, and *"nil has no method 'clear'"*
            // on the bitmap it was going to build. **And this is a class and not
            // a module**, because a scene writes `Window.new(x, y, w, h)`.
            //
            // **And it holds its state and does not draw.** There is no surface,
            // so `update`, `open`, `close` and `dispose` do nothing and say so --
            // **and the fields are real**, because a game reads and writes them
            // and a window whose `contents` were nil would be a window whose
            // every drawing call is a nil.
            "class Window\n"
                + "  def initialize(x = 0, y = 0, w = 0, h = 0)\n"
                + "    @x = x; @y = y; @width = w; @height = h\n"
                + "    @contents = nil\n"
                + "    @active = false\n"
                + "    @visible = true\n"
                + "    @opacity = 255\n"
                + "    @back_opacity = 192\n"
                + "    @contents_opacity = 255\n"
                + "    @windowskin = nil\n"
                + "    @title = nil\n"
                + "    @pause = false\n"
                + "    @cursor_rect = Rect.new\n"
                + "    @ox = 0; @oy = 0; @z = 100\n"
                + "    @stretch = false\n"
                + "  end\n"
                + "  attr_accessor :x\n"
                + "  attr_accessor :y\n"
                + "  attr_accessor :width\n"
                + "  attr_accessor :height\n"
                + "  attr_accessor :contents\n"
                + "  attr_accessor :active\n"
                + "  attr_accessor :visible\n"
                + "  attr_accessor :opacity\n"
                + "  attr_accessor :back_opacity\n"
                + "  attr_accessor :contents_opacity\n"
                + "  attr_accessor :windowskin\n"
                + "  attr_accessor :title\n"
                + "  attr_accessor :pause\n"
                + "  attr_accessor :cursor_rect\n"
                + "  attr_accessor :ox\n"
                + "  attr_accessor :oy\n"
                + "  attr_accessor :z\n"
                + "  attr_accessor :stretch\n"
                + "  def clear; @contents = nil; end\n"
                + "  def update; end\n"
                + "  def open; end\n"
                + "  def close; end\n"
                + "  def dispose; end\n"
                + "  def disposed?; false; end\n"
                + "  def open?; true; end\n"
                + "end",
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
            // **And `RPG::Cache` is where an XP game's images come from, and it
            // was the last thing the scene's setup waited for.** Measured: *"the
            // constant RPG::Cache is not defined by this host"*, with the nil
            // title and windowskin reads that follow from it.
            //
            // **And the nested form is written `RPG::Cache` and not nested
            // `module` statements, because that is what this list already does
            // and what the reader resolves.** The list writes `module RPG::Actor`
            // and the constant lookup asks for the full path; measured, `Cache`
            // alone answers nil and so did `RPG::Cache`, while `RPG` was there.
            //
            // **And the folder names are XP's own layout and not a reading of one
            // game.** Measured, this game ships Graphics with Battlers, Pictures
            // and Titles and nothing else, and takes its windowskin from the RTP,
            // which its Game.ini names as `RTP1=Standard`.
            //
            // **And a missing image is nil and a name.** The host reads the
            // file's own size out of the PNG header and reports the path it
            // looked for when there is none.
            RgssBitmapRubySource.CacheSource + "\nmodule RPG::Cache\n"
                + "  def self.windowskin(n); Bild(:windowskins, n); end\n"
                + "  def self.picture(n); Bild(:pictures, n); end\n"
                + "  def self.battleback_name(n); Bild(:battlebacks, n); end\n"
                + "  def self.battleback(n); Bild(:battlebacks, n); end\n"
                + "  def self.animation(n); Bild(:animations, n); end\n"
                + "  def self.character(n, hue = 0); Bild(:characters, n); end\n"
                + "  def self.tileset(n); Bild(:tilesets, n); end\n"
                + "  def self.autotile(n); Bild(:autotiles, n); end\n"
                + "  def self.battler(n, hue = 0); Bild(:battlers, n); end\n"
                + "  def self.fog(n); Bild(:fogs, n); end\n"
                + "  def self.panorama(n); Bild(:panoramas, n); end\n"
                + "  def self.title(n); Bild(:titles, n); end\n"
                + "end",

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

        // **And the extra sources run first, so a module they open is
        // opened before the game's own scripts extend it.** Measured: the
        // engine's own `main` asks for `Graphics.update` and `Audio.bgm_play`,
        // and neither module was in the list.
        var alleQuellen = new List<string>(RgssExtraRubySources.Sources);
        alleQuellen.AddRange(builtinClasses);
        alleQuellen.Add(RgssBitmapRubySource.Source);
        alleQuellen.Add(RgssBitmapRubySource.ClearRectSource);
        foreach (var code in alleQuellen)
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

    /// <summary>Reach one host boundary or finish the scene; success need not add a frame.</summary>
    public PluginOperationResult Update(double pDeltaSeconds)
    {
        RgssFrameSession session;
        lock (_lifecycleGate)
        {
            if (State != PluginRuntimeState.Running || _updateInProgress)
                return Fail(PluginErrorCode.InvalidLifecycleTransition,
                    "The runtime is not running or another update is in progress.", "update");
            if (double.IsNaN(pDeltaSeconds) || double.IsInfinity(pDeltaSeconds) || pDeltaSeconds < 0)
                return Fail(PluginErrorCode.InvalidLifecycleTransition,
                    "Delta time must be finite and non-negative.", "update");
            _clock.ProcessFrame(pDeltaSeconds);
            if (!_scriptsRun) return PluginOperationResult.Succeeded();
            if (_interpreter == null || _grafik == null)
            {
                State = PluginRuntimeState.Faulted;
                return Fail(PluginErrorCode.LifecycleFailure, "The script runtime is missing its interpreter or graphics host.", "update");
            }
            session = _frameSession ??= new RgssFrameSession(_interpreter, _grafik,
                new RubyParser(new RubyLexer("while $scene; $scene.main; end").Tokenize()).ParseProgram());
            _updateInProgress = true;
        }

        try
        {
            // Do not hold the lifecycle lock while Ruby runs: Stop must be able to cancel it.
            var advanced = session.Advance(TimeSpan.FromSeconds(1));
            lock (_lifecycleGate)
            {
                if (State != PluginRuntimeState.Running)
                    return Fail(PluginErrorCode.InvalidLifecycleTransition,
                        "The runtime was stopped or disposed during this update.", "update");
                if (advanced) return PluginOperationResult.Succeeded();
                if (session.State == RgssExecutionState.Completed)
                {
                    State = PluginRuntimeState.Stopped;
                    return PluginOperationResult.Succeeded();
                }
                if (session.State == RgssExecutionState.Cancelled)
                {
                    State = PluginRuntimeState.Stopped;
                    return Fail(PluginErrorCode.InvalidLifecycleTransition, "The scene was stopped.", "update");
                }
                State = PluginRuntimeState.Faulted;
                var reason = session.Error ?? "The scene execution failed.";
                Melde("scene", "frame session", new InvalidOperationException(reason));
                session.Stop(TimeSpan.FromSeconds(2));
                return Fail(PluginErrorCode.LifecycleFailure, reason, "scene-frame");
            }
        }
        catch (ObjectDisposedException)
        {
            lock (_lifecycleGate)
            {
                if (State != PluginRuntimeState.Disposed) throw;
                return Fail(PluginErrorCode.InvalidLifecycleTransition,
                    "The runtime was disposed before this update could advance.", "update");
            }
        }
        finally
        {
            lock (_lifecycleGate) _updateInProgress = false;
        }
    }

    public PluginOperationResult Stop()
    {
        lock (_lifecycleGate)
        {
            if (State != PluginRuntimeState.Initialized && State != PluginRuntimeState.Running
                && State != PluginRuntimeState.Faulted)
                return Fail(PluginErrorCode.InvalidLifecycleTransition,
                    $"The runtime cannot stop from state {State}.", "stop");
            if (_frameSession != null && !_frameSession.Stop(TimeSpan.FromSeconds(2)))
            {
                State = PluginRuntimeState.Faulted;
                return Fail(PluginErrorCode.LifecycleFailure, "The RGSS scene worker did not stop.", "stop");
            }
            State = PluginRuntimeState.Stopped;
            return PluginOperationResult.Succeeded();
        }
    }

    public void Dispose()
    {
        lock (_lifecycleGate)
        {
            _frameSession?.Dispose();
            // Release worker-owned pixels only after its stack has stopped.
            _grafik?.Bitmaps.ReleaseAll();
            _frameSession = null;
            State = PluginRuntimeState.Disposed;
            _runtimeInfo = null;
            _interpreter = null;
            _scriptHost = null;
            _laufzeit = null;
            _datenHost = null;
        }
    }

    private PluginOperationResult Fail(PluginErrorCode pCode, string pMessage, string pPhase)
    {
        return PluginOperationResult.Failed(PluginError.Create(pCode, pMessage, _pluginId, pPhase));
    }
}