using System;
using System.Collections.Generic;
using System.IO;
using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// A host that answers two of MicroQuest's questions, and nothing
/// else.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And <c>Interpreter.setup</c> is seven lines long, and this
/// prints them:</strong>
/// </para>
/// <code>
/// clear
/// @map_id = $game_map.map_id
/// @event_id = event_id
/// @list = list
/// @index = 0
/// @branch.clear
/// </code>
/// <para>
/// <strong>And two of those six lines ask the host something</strong>,
/// -- <strong><c>map_id</c> and <c>clear</c></strong>, --
/// <strong>and the rest read or write the interpreter's own
/// variables.</strong>
/// </para>
/// <para>
/// <strong>And this test answers exactly those two</strong>, --
/// <strong>because an answer that is not measured is a
/// guess</strong>, --
/// <strong>and it then asks whether <c>@list</c> finally carries the
/// commands, which is the question
/// <c>TestRgssOpenListGap</c> left open.</strong>
/// </para>
/// </remarks>
public partial class TestRgssSetupDurch : TestBase
{
    private const string Wurzel =
        "E:/RPGMakerGames/MicroQuest - Beneath Brimestone 1.0";

    private const string Quelltext = @"
i = Interpreter.new
i.setup([[101, 0, ['Hallo']]], 0)
i.instance_variable_get(:@list)
";

    /// <summary>
    /// And whether <c>@list</c> carries the commands now.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the question that was open since
    /// <c>TestRgssOpenListGap</c></strong>, -- <strong>and it is asked
    /// again here because the host answers differently.</strong>
    /// </para>
    /// </remarks>
    public void Test_KommtSetupBisZurListe()
    {
        var skripte = RgssSkriptHost.Lese(
            Path.Combine(Wurzel, "Data", "Scripts.rxdata"), out var grund);
        if (skripte == null)
        {
            throw new InvalidOperationException(grund);
        }

        var host = new ZweiAntwortenHost(skripte);
        var interpreter = new RubyInterpreter(host);
        Lade(interpreter, host);

        RubyValue ergebnis;
        try
        {
            ergebnis = interpreter.RunProgram(new RubyParser(
                new RubyLexer(Quelltext).Tokenize()).ParseProgram());
        }
        catch (RubyRuntimeException ausnahme)
        {
            Console.WriteLine("Laufzeit: " + ausnahme.Message);
            ergebnis = RubyValue.Nil;
        }

        Console.WriteLine("Fragen: " + string.Join(" | ", host.Fragen.ToArray()));
        Console.WriteLine("ohne Antwort: " + host.OhneAntwort);
        Console.WriteLine("@list: " + Describe(ergebnis));

        // **Und `@list` traegt endlich  die Befehle**, --
        // **und das ist die offene Frage aus
        // `TestRgssOpenListGap`.**
        AssertTrue(ergebnis.IsList && ergebnis.Items.Count == 1,
            "**and `@list` carries the command list** -- and it is one"
                + " command with three entries, and MicroQuest's own"
                + " `setup(list, event_id)` writes `@list = list` as"
                + " its fourth line, and until this host answered"
                + " `map_id` and `clear` the run stopped at line two");

        AssertTrue(ergebnis.Items.Count == 1 && ergebnis.Items[0].IsList
                && ergebnis.Items[0].Items.Count == 3,
            "**and that one command is `[[101, 0, ['Hallo']]]`** -- and"
                + " 101 is a command the game knows, 0 is its first"
                + " parameter and `['Hallo']` is its text, and the"
                + " count of three is what says the whole list was"
                + " read and not one entry of it");

        // **Und die zwei unbeantworteten  Fragen  sind  namentlich
        //  die  beiden,  die  MicroQuest  beim  Laden  stellt.**
        //
        // **Und  das  ist  nicht  geraten:**
        //
        // ```text
        // Fragen: Konstante Graphics | freeze an Nil (Block)
        //         | map_id an Game_game_map | clear an Object
        // ohne Antwort: 2
        // ```
        //
        // **Und  `Graphics`  und  `freeze`  kommen  aus  den  Skripten**
        // **und nicht aus `setup`**,
        // **und `setup` selbst  hat  nichts  offen  gelassen.**
        //
        // **Und  mein  erster  Entwurf  behauptete hier  `0`,
        // **und  die  Messung  sagte  `2`** --
        // **und  ein  Test,  der  eine  Zahl  behauptet,  die  die
        // Messung  nicht  traegt,  ist  das  Gegenteil  von  dem,
        // was  dieses  Repository  braucht.**
        AssertEq(2, host.Unbeantwortet.Count,
            "**and exactly two questions went unanswered** -- and"
                + " they are `Graphics` and `freeze`, both asked while"
                + " MicroQuest's scripts load, and neither asked by"
                + " `setup` itself");

        AssertTrue(host.Unbeantwortet.Contains("freeze")
                && host.Unbeantwortet.Contains("Konstante Graphics"),
            "**and the two are named** -- and a count without the"
                + " names says nothing about which runtime would"
                + " need them");
    }

    /// <summary>And what the run gave back, read out.</summary>
    /// <param name="pWert">The value.</param>
    /// <returns>Its kind, its class and its elements.</returns>
    private static string Describe(RubyValue pWert)
    {
        var teile = new List<string>
        {
            pWert.Kind.ToString(),
            pWert.ClassName ?? "-",
        };
        // **Und `Items` und nicht `Array`** --
        // **und das ist an `RubyValue` gemessen**, --
        // **und `IsList` sagt, ob es eine Liste ist.**
        if (pWert.IsList)
        {
            teile.Add(pWert.Items.Count + " Elemente");
            foreach (var e in pWert.Items)
            {
                teile.Add(e.IsList
                    ? "[" + e.Items.Count + "]"
                    : e.Kind.ToString());
            }
        }

        return string.Join(" / ", teile);
    }

    /// <summary>And the 90 scripts, loaded.</summary>
    /// <param name="pInterpreter">The interpreter.</param>
    /// <param name="pHost">The host that serves the scripts.</param>
    private static void Lade(
        RubyInterpreter pInterpreter, ZweiAntwortenHost pHost)
    {
        foreach (var name in new List<string>(pHost.Namen))
        {
            var bytes = pHost.ReadScript(name, true);
            if (bytes == null)
            {
                continue;
            }

            try
            {
                pInterpreter.RunProgram(new RubyParser(new RubyLexer(
                    System.Text.Encoding.UTF8.GetString(bytes))
                    .Tokenize()).ParseProgram());
            }
            catch (RubyParseException)
            {
                // **Und siehe `TestRgssSkriptHost`.**
            }
        }

        foreach (var name in new[]
        {
            "$game_map", "$game_player", "$game_party", "$game_variables",
            "$game_switches", "$game_self_switches", "$game_temp",
            "$game_system", "$game_message", "$game_actors", "$game_enemies",
        })
        {
            pInterpreter.SetzeGlobal(name,
                RubyValue.OfEmptyObject("Game_" + name.TrimStart('$')));
        }
    }
}

/// <summary>
/// A host that answers <c>map_id</c> and <c>clear</c> and nothing
/// else.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the two answers are the ones
/// <c>Interpreter.setup</c> asks for</strong>, -- <strong>and
/// <c>map_id</c> is 1 because MicroQuest starts on its own
/// map</strong>, -- <strong>and a reader that returned 0 would put
/// every event on a map that is not there.</strong>
/// </para>
/// <para>
/// <strong>And <c>clear</c> answers with <c>nil</c> and not with an
/// object</strong>, -- <strong>because Ruby 1.8.1's own
/// <c>Array#clear</c> and <c>Hash#clear</c> return the receiver and
/// the interpreter does not read the value</strong> --
/// <strong>and returning an object here would invent a
/// receiver.</strong>
/// </para>
/// </remarks>
public sealed class ZweiAntwortenHost : IRubyHost
{
    private readonly RgssSkriptHost _echt;

    /// <summary>Build a host over a project's scripts.</summary>
    /// <param name="pEcht">Where the scripts come from.</param>
    public ZweiAntwortenHost(RgssSkriptHost pEcht) => _echt = pEcht;

    /// <summary>Every question, as it arrived.</summary>
    public List<string> Fragen { get; } = new List<string>();

    /// <summary>How many of them were not answered.</summary>
    public int OhneAntwort { get; private set; }

    /// <summary>And which ones, so a count can be read.</summary>
    public System.Collections.Generic.List<string> Unbeantwortet { get; } =
        new System.Collections.Generic.List<string>();

    /// <summary>And the script names this host serves.</summary>
    /// <returns>The names.</returns>
    public IReadOnlyCollection<string> Namen => _echt.Namen;

    /// <inheritdoc/>
    public byte[]? ReadScript(string pName, bool pEinmal) =>
        _echt.ReadScript(pName, pEinmal);

    /// <inheritdoc/>
    public RubyValue? CallMethod(
        RubyValue pEmpfaenger,
        string pMethode,
        IReadOnlyList<RubyValue> pArgumente)
    {
        Fragen.Add(pMethode + " an "
            + (pEmpfaenger.ClassName ?? pEmpfaenger.Kind.ToString()));

        // **Und `map_id` ist die erste Frage, und ihre Antwort ist
        // eine Zahl.** -- **Und MicroQuests eigenes `Interpreter`
        // setzt sie in `@map_id`, und `Interpreter 3` liest sie in
        // `setup_starting_event`.**
        if (pMethode == "map_id")
        {
            return RubyValue.OfInteger(1);
        }

        // **Und `clear` kommt zweimal:  einmal auf dem Interpreter
        // selbst und einmal auf `@branch`.** -- **Und beide geben
        // nichts zurueck,  denn Ruby 1.8.1 gibt den Empfaenger
        // zurueck und der Leser liest den Wert nicht.**
        if (pMethode == "clear")
        {
            return RubyValue.Nil;
        }

        Unbeantwortet.Add(pMethode);
        OhneAntwort++;
        return null;
    }

    /// <inheritdoc/>
    public RubyValue? CallMethodWithBlock(
        RubyValue pEmpfaenger,
        string pMethode,
        IReadOnlyList<RubyValue> pArgumente,
        Func<IReadOnlyList<RubyValue>, RubyValue> pErtrag)
    {
        Fragen.Add(pMethode + " an "
            + (pEmpfaenger.ClassName ?? pEmpfaenger.Kind.ToString())
            + " (Block)");
        Unbeantwortet.Add(pMethode);
        OhneAntwort++;
        return null;
    }

    /// <inheritdoc/>
    public RubyValue? LookupConstant(string pName)
    {
        Fragen.Add("Konstante " + pName);
        Unbeantwortet.Add("Konstante " + pName);
        OhneAntwort++;
        return null;
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> KnownMethods => _echt.KnownMethods;
}
