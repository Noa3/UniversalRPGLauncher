using System;
using System.Collections.Generic;
using System.Globalization;

namespace UniversalRPG.Rgss;

/// <summary>
/// Gives a running Ruby Maker game the globals its own scripts expect.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And every name in here is measured out of the game's own
/// Ruby</strong>, -- <strong>and the list is written down in the
/// comments where it was counted</strong>, -- <strong>because a
/// runtime that invents globals runs a game on values the game never
/// wrote.</strong>
/// </para>
/// <para>
/// <strong>And this is the first runtime in this repository and it is
/// a hundred lines long.</strong> -- <strong>And that is not a claim
/// about how easy a runtime is;</strong> -- <strong>it is a claim
/// about how much of one MicroQuest needed</strong>, --
/// <strong>and the measurement behind it is in
/// <c>TestRgssGlobalSurvey</c>: 25 different globals in the game's own
/// scripts, of which this runtime sets three.</strong>
/// </para>
/// <para>
/// <strong>And the order matters and is measured.</strong>
/// <c>Game_Map#setup</c> asks for <c>$game_map</c> first and then for
/// <c>$data_tilesets</c>, -- <strong>and a runtime that set them in the
/// other order would work anyway, and a runtime that set only one would
/// stop at a different line each time</strong>, -- <strong>and which
/// line is a fact about the game and worth knowing.</strong>
/// </para>
/// </remarks>
public sealed class RgssLaufzeit
{
    private readonly RubyInterpreter _interpreter;
    private readonly RgssDatenHost? _daten;

    /// <summary>
    /// Builds a runtime over a game's scripts and data.
    /// </summary>
    /// <param name="pInterpreter">The interpreter with the scripts already run.</param>
    /// <param name="pDaten">The data host, or null for a game with no data.</param>
    /// <param name="pErweiterung">
    /// The data file extension, which is the generation's own: `.rxdata` for XP, `.rvdata` for VX
    /// and `.rvdata2` for VX Ace. **And it has to be passed in**, because the data host takes a
    /// name as written -- the engine's own `load_data` writes the extension itself, and a caller
    /// that leaves it out asks for a file that is not there. Measured: thirteen refusals for
    /// thirteen globals, every one of them because the extension was missing.
    /// </param>
    public RgssLaufzeit(
        RubyInterpreter pInterpreter, RgssDatenHost? pDaten, string pErweiterung = ".rxdata")
    {
        _interpreter = pInterpreter
            ?? throw new ArgumentNullException(nameof(pInterpreter));
        _daten = pDaten;
        Erweiterung = pErweiterung;
    }

    /// <summary>
    /// The data file extension this runtime resolves names against.
    /// </summary>
    public string Erweiterung { get; }

    /// <summary>
    /// The globals this runtime set, in the order it set them.
    /// </summary>
    /// <remarks>
    /// <strong>And a runtime that cannot say what it set is a runtime
    /// whose state nobody can check</strong>, -- <strong>and that list
    /// is what a test reads instead of trusting a return
    /// value.</strong>
    /// </remarks>
    public List<string> Gesetzt { get; } = new List<string>();

    /// <summary>
    /// How many data files this runtime loaded.
    /// </summary>
    public int Geladen => _daten?.Gelesen ?? 0;

    /// <summary>
    /// How many of its globals a load refused.
    /// </summary>
    public int Verweigert { get; private set; }

    /// <summary>
    /// Sets the globals MicroQuest's own scripts read before the first
    /// scene.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And there are exactly three, and all three are
    /// measured:</strong>
    /// </para>
    /// <list type="number">
    /// <item><description>
    /// <c>$data_tilesets</c> -- <c>Scene_Title</c> writes
    /// <c>$data_tilesets = load_data("Data/Tilesets.rxdata")</c>, --
    /// <strong>and it is 191 uses of the name across the game's
    /// scripts</strong>, -- <strong>and <c>Game_Map#setup</c> reads
    /// <c>$data_tilesets[@map.tileset_id]</c> out of it</strong>.
    /// </description></item>
    /// <item><description>
    /// <c>$game_map</c> -- 44 lines mention it and none assigns it, --
    /// <strong>and the runtime creates it</strong>, -- <strong>and
    /// <c>Game_Map#setup</c> reads <c>$game_map.map_id</c> and
    /// <c>$game_map.width</c> out of it</strong>.
    /// </description></item>
    /// <item><description>
    /// <c>$game_player</c> -- <c>Interpreter 1</c> calls
    /// <c>$game_player.setup_starting_event(nil)</c>, --
    /// <strong>and 52 uses of the name stand behind it</strong>.
    /// </description></item>
    /// </list>
    /// <para>
    /// <strong>And the two objects that are not files are created from
    /// the game's own classes</strong>, -- <c>Game_Map.new</c> and
    /// <c>Game_Player.new</c>, -- <strong>because those classes run
    /// <c>initialize</c> and a C# object would not.</strong>
    /// </para>
    /// </remarks>
    public void Startet()
    {
        // **And this is the engine's own `main`, minus the scene loop.** Measured against XP's
        // `main.rb`: it loads thirteen `$data_*` globals, builds eleven `$game_*` objects, then
        // calls `$game_map.setup($data_system.start_map_id)`, moves the player to the system's
        // start position and refreshes them. **And what was here before was three globals** --
        // `$data_tilesets`, `$game_map` and `$game_player` -- which was enough for the scripts to
        // load and not enough for a frame: measured on a real game, `$game_map.update` then
        // compared `@scroll_rest` with a number, and `@scroll_rest` is set by `setup`, which
        // nobody ever called.
        //
        // **And every step is guarded and named.** A game whose `System.rxdata` is missing must
        // still get the globals that are there, and a failure has to say which one it was: one
        // try around the whole sequence would turn a missing file into a game with no state and
        // no reason.
        foreach (var (name, file) in DataFiles)
        {
            Setze(name, file);
        }
        foreach (var (name, type) in GameObjects)
        {
            Erzeuge(name, type);
        }
        // **And every step asks whether the data it needs is there.** A real
        // XP `System.rxdata` has `start_map_id`, `start_x` and `start_y` -- and
        // a minimal one does not, **and a step that runs anyway throws inside
        // the game's own code**: measured, `$game_player.moveto` then reported
        // *"undefined operator '%' for a Nil and a Integer"* for a fixture
        // whose System has no start position. **And the guard is `respond_to?`
        // and not a nil check**, because it is the same question the game asks
        // itself.
        Rufe("$game_map.setup($data_system.start_map_id)"
            + " if $data_system.respond_to?(:start_map_id)");
        Rufe("$game_player.moveto($data_system.start_x, $data_system.start_y)"
            + " if $data_system.respond_to?(:start_x)"
            + " && $data_system.respond_to?(:start_y)");
        Rufe("$game_player.refresh if $game_player.respond_to?(:refresh)");

        // **And then the runtime reads back what the game's own state says.**
        // Measured: the last setup step reported *"undefined operator '%' for a
        // Integer and a Nil"* -- so `start_x` arrives and something else is
        // nil, and a report that names only the step leaves the reader to guess
        // which value. **And a reading is not a fix:** this list says what the
        // game holds, and the next slice fixes what it says is missing.
        Messen("map_id", "$game_map.map_id if $game_map");
        Messen("map_width", "$game_map.width if $game_map");
        Messen("map_height", "$game_map.height if $game_map");
        Messen("map_class", "$game_map.class.name if $game_map");

        // **Und die Startposition, weil sie der naechste Schritt ist.**
        // Gemessen meldet `$game_player.moveto($data_system.start_x,
        // $data_system.start_y)` danach *"undefined operator '+' for a Nil and a
        // Nil"*, -- **und der Schritt davor meldete den Fehler noch bei `%`**,
        // -- **also liest `moveto` jetzt etwas anderes.** Was das Spiel in
        // `$data_system` haelt, steht hier.
        Messen("start_x", "$data_system.start_x if $data_system");
        Messen("start_y", "$data_system.start_y if $data_system");
        Messen("data_tilesets", "$data_tilesets.size if $data_tilesets");

        // **Und die Rohdaten der Karte, weil dort der naechste Fehler sitzt.**
        // `Game_Map#setup` baut die Events mit
        // `Game_Event.new(@map_id, @map.events[i])`, -- **und
        // `Game_Event#initialize` ruft `moveto(@event.x, @event.y)`** -- und
        // `moveto` meldet *"'%' for a Nil and a Integer"*, also ist `x` nil.
        // **Und die Frage ist, ob das Feld fehlt oder die Markierung**, und
        // das entscheidet sich an den Rohdaten und nicht am Spielobjekt.
        Messen("raw_events", "load_data(\"Data/Map001.rxdata\").events.size");
        Messen("raw_event1_x", "load_data(\"Data/Map001.rxdata\").events[1].x");
        Messen("raw_event1_y", "load_data(\"Data/Map001.rxdata\").events[1].y");

        // **Und die Bildflaeche, weil der letzte Fehler dort sitzt.**
        // `Game_Player#center` liest `Graphics.width` und `Graphics.height`, --
        // **und wenn sie hier nil sind, ist der Host nicht in der Kette.**
        Messen("graphics_width", "Graphics.width");
        Messen("graphics_height", "Graphics.height");
        Messen("graphics_nil", "Graphics.nil?");
        Messen("graphics_name", "Graphics.class.name");

        // **Und der Host zaehlt mit.** Die Karte liest sich ausserhalb des
        // Spiels einwandfrei -- `RPG::Map` mit elf Feldern und `@width` darin --
        // **und im Spiel ist `$game_map.width` nil**, also ist die Frage, ob
        // `load_data` dieselbe Datei durch denselben Host gelesen hat.
        Zustand.Add("host gelesen = " + (_daten?.Gelesen ?? -1)
            + ", verweigert = " + (_daten?.Verweigert ?? -1));
    }

    /// <summary>
    /// Reads one expression out of the game and records what it holds.
    /// </summary>
    private void Messen(string pName, string pQuelltext)
    {
        try
        {
            _interpreter.RunProgram(new RubyParser(new RubyLexer(
                "$gemessen_" + pName + " = (" + pQuelltext + ")").Tokenize())
                .ParseProgram());
            Zustand.Add(pName + " = " + Beschreibe(
                _interpreter.Global("$gemessen_" + pName)));
        }
        catch (Exception ausnahme)
        {
            Zustand.Add(pName + " -- " + ausnahme.Message);
        }
    }

    /// <summary>
    /// A reading as text, because a nil and a zero are different answers.
    /// </summary>
    private static string Beschreibe(RubyValue pWert)
    {
        if (pWert.IsNil)
        {
            return "nil";
        }

        if (pWert.Kind == RubyValueKind.Integer)
        {
            return pWert.Integer.ToString(CultureInfo.InvariantCulture);
        }

        if (pWert.Kind == RubyValueKind.Float)
        {
            return pWert.Real.ToString(CultureInfo.InvariantCulture);
        }

        if (pWert.Kind == RubyValueKind.Boolean)
        {
            return pWert.Boolean ? "true" : "false";
        }

        if (pWert.Kind == RubyValueKind.String)
        {
            return "[" + System.Text.Encoding.UTF8.GetString(pWert.Bytes) + "]";
        }

        return pWert.ClassName ?? pWert.Kind.ToString();
    }

    /// <summary>
    /// What the game's own state reads after the setup sequence.
    /// </summary>
    /// <remarks>
    /// <strong>And this is a reading and not a claim.</strong> The setup steps
    /// say which step threw; these say what the game holds afterwards, and a
    /// reader who has both does not have to guess which value is the nil.
    /// </remarks>
    public List<string> Zustand { get; } = new List<string>();

    /// <summary>
    /// The data globals the engine's own <c>main</c> loads, in its own order.
    /// </summary>
    /// <remarks>
    /// <strong>And the names are the engine's, and the files are named without their
    /// extension</strong> -- the data host resolves `.rxdata`, `.rvdata` and `.rvdata2`, so one
    /// list serves XP, VX and VX Ace. **And a file that is not there is refused and counted**,
    /// which is the difference between a game that runs without its troops and one that does not
    /// start.
    /// </remarks>
    private static readonly (string Name, string File)[] DataFiles =
    {
        ("data_actors", "Data/Actors"),
        ("data_classes", "Data/Classes"),
        ("data_skills", "Data/Skills"),
        ("data_items", "Data/Items"),
        ("data_weapons", "Data/Weapons"),
        ("data_armors", "Data/Armors"),
        ("data_enemies", "Data/Enemies"),
        ("data_troops", "Data/Troops"),
        ("data_states", "Data/States"),
        ("data_animations", "Data/Animations"),
        ("data_tilesets", "Data/Tilesets"),
        ("data_common_events", "Data/CommonEvents"),
        ("data_system", "Data/System"),
    };

    /// <summary>
    /// The game objects the engine's own <c>main</c> builds, in its own order.
    /// </summary>
    private static readonly (string Name, string Type)[] GameObjects =
    {
        ("$game_temp", "Game_Temp"),
        ("$game_system", "Game_System"),
        ("$game_switches", "Game_Switches"),
        ("$game_variables", "Game_Variables"),
        ("$game_self_switches", "Game_SelfSwitches"),
        ("$game_screen", "Game_Screen"),
        ("$game_actors", "Game_Actors"),
        ("$game_party", "Game_Party"),
        ("$game_troop", "Game_Troop"),
        ("$game_map", "Game_Map"),
        ("$game_player", "Game_Player"),
    };

    /// <summary>
    /// Runs one setup step, and names it if it throws.
    /// </summary>
    private void Rufe(string pQuelltext)
    {
        try
        {
            _interpreter.RunProgram(new RubyParser(
                new RubyLexer(pQuelltext).Tokenize()).ParseProgram());
            Gesetzt.Add(pQuelltext);
        }
        catch (Exception ausnahme)
        {
            Fehlgeschlagen.Add(pQuelltext + " -- " + ausnahme.Message);
        }
    }

    /// <summary>
    /// The setup steps that threw, with their reason.
    /// </summary>
    /// <remarks>
    /// <strong>And a step that fails is reported and the rest still runs.</strong> A game with a
    /// missing data file is a game that is missing that file, not a game that refuses to start --
    /// and a runtime that threw here would take the other twelve globals down with it.
    /// </remarks>
    public List<string> Fehlgeschlagen { get; } = new List<string>();

    private void Setze(string pName, string pDatei)
    {
        if (_daten == null)
        {
            Verweigert++;
            Fehlgeschlagen.Add("$" + pName + " -- no data host");
            return;
        }

        UniversalRPG.Rgss.RubyValue? wert;
        try
        {
            wert = _daten.Lade(pDatei + Erweiterung);
        }
        catch (Exception ausnahme)
        {
            Verweigert++;
            Fehlgeschlagen.Add("$" + pName + " aus " + pDatei + " -- " + ausnahme.Message);
            return;
        }

        if (wert == null)
        {
            Verweigert++;
            Fehlgeschlagen.Add("$" + pName + " -- " + pDatei + " is not there");
            return;
        }

        _interpreter.SetzeGlobal("$" + pName, wert);
        Gesetzt.Add("$" + pName + " aus " + pDatei);
    }

    private void Erzeuge(string pName, string pTyp)
    {
        // **Und der Aufruf laeuft durch das Spiel selbst** --
        // **und nicht durch einen C#-Konstruktor** --
        // **und das heisst:  `Game_Map#initialize` laeuft.**
        Rufe(pName + " = " + pTyp + ".new");
    }
}
