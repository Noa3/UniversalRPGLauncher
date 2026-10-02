using System;
using System.Collections.Generic;

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
    public RgssLaufzeit(RubyInterpreter pInterpreter, RgssDatenHost? pDaten)
    {
        _interpreter = pInterpreter
            ?? throw new ArgumentNullException(nameof(pInterpreter));
        _daten = pDaten;
    }

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
        Setze("data_tilesets", "Data/Tilesets.rxdata");
        Erzeuge("$game_map", "Game_Map");
        Erzeuge("$game_player", "Game_Player");
    }

    private void Setze(string pName, string pDatei)
    {
        if (_daten == null)
        {
            Verweigert++;
            return;
        }

        var wert = _daten.Lade(pDatei);
        if (wert == null)
        {
            Verweigert++;
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
        _interpreter.RunProgram(new RubyParser(new RubyLexer(
            pName + " = " + pTyp + ".new").Tokenize()).ParseProgram());
        Gesetzt.Add(pName + " als " + pTyp + ".new");
    }
}
