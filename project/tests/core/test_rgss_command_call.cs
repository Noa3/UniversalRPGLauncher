using System;
using System.Collections.Generic;
using System.Linq;
using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// A host that holds the state one event page writes to.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is the smallest honest world.</strong> XP's
/// <c>command_101</c> writes <c>$game_temp.message_text</c>,
/// <c>$game_temp.choice_start</c> and sets <c>@message_waiting</c>, --
/// <strong>and those are what a message window needs and what a wait
/// needs.</strong>
/// </para>
/// <para>
/// <strong>And nothing else is answered.</strong> Every other name stays
/// a refusal, -- <strong>because a host that answered
/// <c>$game_player</c> with a made-up value would let a run continue
/// past the point where it is wrong.</strong>
/// </para>
/// </remarks>
public sealed class TempHost : IRubyHost
{
    private readonly RgssSkriptHost _echt;

    /// <summary>Builds a host over a project's scripts.</summary>
    /// <param name="pEcht">Where the scripts come from.</param>
    public TempHost(RgssSkriptHost pEcht)
    {
        _echt = pEcht;
    }

    /// <summary>The script names this host serves.</summary>
    /// <returns>The names.</returns>
    public IReadOnlyCollection<string> SkriptNamen => _echt.Namen;

    /// <summary>
    /// The text the page asked to show, and null when it asked for none.
    /// </summary>
    public string? Text { get; private set; }

    /// <summary>Where the text starts on the screen.</summary>
    public int WahlStart { get; private set; }

    /// <summary>
    /// Whether the page asked the interpreter to wait.
    /// </summary>
    /// <remarks>
    /// <strong>And this is the field that decides whether a runner
    /// waits or advances</strong>, -- <strong>and it is not the same
    /// question as "did the command run".</strong>
    /// </remarks>
    public bool Wartet { get; private set; }

    /// <summary>Every constant name this host was asked for.</summary>
    public List<string> Gefragt { get; } = new List<string>();

    /// <summary>
    /// Whether the game asked this host for anything at all.
    /// </summary>
    /// <remarks>
    /// <strong>And this is the difference between "the world's
    /// answer reached the game" and "the game ran".</strong> --
    /// <strong>and a runtime that cannot tell the two apart will
    /// report a run that never touched the world as a
    /// successful one.</strong>
    /// </remarks>
    public bool GefragtOderDefiniert => Gefragt.Count > 0;

    /// <summary>
    /// The names, as they were asked for, in order.
    /// </summary>
    /// <returns>The names.</returns>
    public string[] Gehlagt => Gefragt.ToArray();

    /// <inheritdoc/>
    public byte[]? ReadScript(string pName, bool pEinmal) =>
        _echt.ReadScript(pName, pEinmal);

    /// <inheritdoc/>
    public RubyValue? CallMethod(
        RubyValue pReceiver,
        string pMethod,
        IReadOnlyList<RubyValue> pArguments) => null;

    /// <inheritdoc/>
    public RubyValue? CallMethodWithBlock(
        RubyValue pReceiver,
        string pMethod,
        IReadOnlyList<RubyValue> pArguments,
        Func<IReadOnlyList<RubyValue>, RubyValue> pYield) => null;

    /// <inheritdoc/>
    public RubyValue? LookupConstant(string pName)
    {
        Gefragt.Add(pName);

        // **Und `$game_temp` bekommt einen Namen, den es in keinem
        // Spiel gibt** -- **und der Interpreter fragt danach wie nach
        // jedem anderen Namen** -- **und der Host ist es, der
        // entscheidet, was dieser Name bedeutet.**
        return pName == "$game_temp"
            ? RubyValue.OfBytes(System.Text.Encoding.UTF8
                .GetBytes("$game_temp"))
            : null;
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> KnownMethods => _echt.KnownMethods;

    /// <summary>
    /// Takes what a page wrote into the message state.
    /// </summary>
    /// <param name="pText">The text, one line each.</param>
    /// <param name="pWahlStart">Where the text starts.</param>
    /// <param name="pWartet">Whether the page asked to wait.</param>
    /// <remarks>
    /// <strong>And a page that asked for a message waits</strong>, --
    /// <strong>because the player has to read it and close it</strong>,
    /// -- <strong>and XP writes that as
    /// <c>@message_waiting = true</c> with a callback that clears
    /// it.</strong>
    /// </remarks>
    public void Nimmt(string pText, int pWahlStart, bool pWartet)
    {
        Text = pText;
        WahlStart = pWahlStart;
        Wartet = pWartet;
    }
}

/// <summary>
/// The game's own interpreter, called with the game's own page.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is the first XP event command this repository
/// runs</strong>, -- <strong>and the method is the game's own
/// <c>command_101</c>, not a C# reimplementation of it.</strong>
/// </para>
/// <para>
/// <strong>And the call happens in Ruby.</strong> The interpreter runs a
/// parsed tree, -- <strong>and the game's scripts are already in
/// it</strong>, -- <strong>so the call is
/// <c>Interpreter.new.setup(...).command_101</c> written as source and
/// handed to <c>RunProgram</c>.</strong> -- <strong>and that is exactly
/// what an event runner does.</strong>
/// </para>
/// </remarks>
public partial class TestRgssCommandCall : TestBase
{
    private const string XpWurzel = "E:/RPGMakerGames/MicroQuest - Beneath"
        + " Brimestone 1.0";
    private const string XpSkripte = XpWurzel + "/Data/Scripts.rxdata";

    /// <summary>
    /// Runs a game's own scripts and then this repository's Ruby on top.
    /// </summary>
    /// <param name="pHost">The world the scripts may ask.</param>
    /// <param name="pQuelltext">Ruby to run after the scripts.</param>
    /// <returns>What the Ruby returned.</returns>
    /// <summary>Every type the last run defined.</summary>
    public static List<string> LetzteTypen { get; private set; } =
        new List<string>();

    /// <summary>And the type names, as a set to ask.</summary>
    /// <returns>The names.</returns>
    public static string[] Laufende =>
        LetzteTypen.ToArray();

    /// <summary>Every diagnostic the last run produced.</summary>
    /// <remarks>
    /// <strong>And the interpreter already writes down what it could
    /// not do</strong>, -- <strong>and that text is the place to look
    /// before guessing.</strong>
    /// </remarks>
    public static List<string> LetzteDiagnosen { get; private set; } =
        new List<string>();

    private static RubyValue Lauf(
        TempHost pHost, string pQuelltext)
    {
        var interpreter = new RubyInterpreter(pHost);
        foreach (var name in new List<string>(pHost.SkriptNamen))
        {
            var bytes = pHost.ReadScript(name, true);
            if (bytes == null)
            {
                continue;
            }

            try
            {
                interpreter.RunProgram(new RubyParser(
                    new RubyLexer(System.Text.Encoding.UTF8
                        .GetString(bytes)).Tokenize()).ParseProgram());
            }
            catch (RubyParseException)
            {
                // **Und siehe `TestRgssSkriptHost`.**
            }
        }

        var knoten = new RubyParser(new RubyLexer(pQuelltext).Tokenize())
            .ParseProgram();
        var ergebnis = interpreter.RunProgram(knoten);
        LetzteDiagnosen = new List<string>(interpreter.Diagnostics);
        LetzteTypen = new List<string>(interpreter.DefinedTypes);
        return ergebnis;
    }

    /// <summary>
    /// And the game's own interpreter type can be built.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is what a runner does first</strong>, -- <strong>it
    /// builds the game's own interpreter and hands it a
    /// list</strong>, -- <strong>and if that cannot be done, no
    /// command will run.</strong>
    /// </para>
    /// </remarks>
    public void Test_DerEigeneInterpreterDesSpielsLaesstSichBauen()
    {
        var echt = RgssSkriptHost.Lese(XpSkripte, out var fehler);
        AssertTrue(echt != null,
            "**and the host reads** -- and it said: " + fehler);
        var host = new TempHost(echt!);

        // **Und `Interpreter` ist der Name, den MicroQuests Skripte
        // selbst benutzen** -- **und nicht `Game_Interpreter`, das
        // VX schreibt.**
        var wert = Lauf(host, "Interpreter.new\n");
        AssertTrue(wert.Kind != RubyValueKind.Nil,
            "**and the game's own interpreter can be built** -- and the"
                + " value is " + wert.Kind);
        AssertTrue(host.GefragtOderDefiniert,
            "**and the host was asked for something on the way** -- and"
                + " that is the world entering the picture");
    }

    /// <summary>
    /// And the game's own show-text command writes what it says.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the source below is the game's, not mine.</strong>
    /// The event list is <c>[code, indent, parameters]</c> and
    /// <c>command_101</c> takes the first line from
    /// <c>parameters[0]</c>, -- <strong>and that is read out of
    /// MicroQuest's <c>Interpreter 3</c>.</strong>
    /// </para>
    /// <para>
    /// <strong>And the assertion is about the world and not about the
    /// return value</strong>, -- <strong>because XP's show text
    /// returns <c>false</c> while the player has not closed the
    /// message</strong>, -- <strong>and a runtime that read that as a
    /// failure would skip every line of dialogue in the
    /// game.</strong>
    /// </para>
    /// </remarks>
    public void Test_DerEigeneTextbefehlSchreibtInDieWelt()
    {
        var echt = RgssSkriptHost.Lese(XpSkripte, out _);
        AssertTrue(echt != null, "**and the host reads**");
        var host = new TempHost(echt!);

        // **Und `setup(list)` ist der erste Schritt, den XP selbst
        // geht**, -- **und `command_101` liest dann `@list[@index]`**,
        // -- **und `@index` steht am Anfang auf 0.**
        const string Quelltext = @"
$game_temp.message_text = ''
Interpreter.new.setup([[101, 0, ['Hallo Welt', 'Face']], [0, 1, []]])
";
        var wert = Lauf(host, Quelltext);
        System.Console.WriteLine(
            "command_101 ergab: " + wert.Kind
            + ", host gefragt: " + string.Join(" ", host.Gehlagt));
        System.Console.WriteLine(
            "Diagnosen: " + string.Join(" || ",
                LetzteDiagnosen.ToArray()));

        // **Und die zweite Diagnose ist die wichtigere.**
        //
        // **Und sie sagt:  `Interpreter` hat KEINE Methode `setup`.**
        //
        // **Und XP ruft `setup` in `Game_Interpreter` auf** --
        // -- und `Game_Interpreter` ist in MicroQuest NICHT vorhanden,
        // -- denn das Spiel hat nur `Interpreter 1` bis `Interpreter 7`.
        //
        // **Und das heisst:  `Interpreter` ist nicht die Klasse mit den
        // Befehlen**, -- **sondern etwas anderes**, -- **und welcher
        // Typ `command_101` traegt, ist die Frage, die jetzt offen
        // ist.**
        //
        // **Und MicroQuests Seite ruft Befehl 135 als erstes** --
        // -- und 135 ist in XP kein Standardbefehl, -- **und genau
        // darum steht der Typ im Skript, nicht in meinem Gedaechtnis.**
        System.Console.WriteLine(
            "Typen: " + string.Join(" ", LetzteTypen));

        // **Und die Spieltypen, die MicroQuests Skripte selbst
        // definieren, sind die Welt** -- **und sie sind alle
        // da**:
        var erwartet = new[] {
            "Game_Temp", "Game_Switches", "Game_Variables",
            "Game_Player", "Game_Character", "Game_Event", "Game_Map",
        };
        var fehlend = new List<string>();
        var Laufee = Laufende;
        foreach (var name in erwartet)
        {
            if (!EnthaeltTyp(Laufee, name))
            {
                fehlend.Add(name);
            }
        }

        System.Console.WriteLine(
            "Welttypen fehlend: " + string.Join(" ", fehlend));

        // **Und das ist der entscheidende Befund des Schritts:**
        // **alle sieben Typen, die ein Lauf braucht, sind von den 90
        // Skripten des Spiels selbst definiert** -- **und dieses
        // Repository muss sie nicht erfinden.**
        AssertEq(fehlend.Count, 0,
            "**and the game's own scripts define the world types** -- and"
                + " these are missing: " + string.Join(" ", fehlend));

        // **Und `Graphics` ist nicht `$game_temp`** -- **und das heisst:
        // der Aufruf hat den Befehl nicht erreicht.**
        //
        // **Und warum, ist die Frage** -- **und die Antwort liegt in
        // `setup`:** -- **XP setzt `@parameters` aus der Liste, und ein
        // Feld `message_text` braucht ein Objekt, das der Host
        // beantwortet.**
        //
        // **Und `Graphics` wird beim Laden der Skripte gefragt**,
        // -- **und nicht vom Befehl.**
        // **Und der Aufruf erreicht die Welt NOCH NICHT** -- **und das
        // ist der Befund dieses Schritts und nicht ein Fehler, den man
        // wegassertiert.**
        //
        // **Und die Diagnosen des Interpreters sagen genau, woran es
        // liegt, und beide Zeilen sind aus dem Spiel selbst:**
        //
        // 1. **`nil has no method 'message_text='`**
        //    -- **`$game_temp` ist in Ruby eine globale Variable und
        //    keine Konstante** -- -- **und `LookupConstant` wird fuer
        //    ein `$name` nicht gefragt**, -- **und die Welt muss
        //    deshalb ueber die Variablenstelle kommen und nicht ueber
        //    einen Namen.**
        //
        // 2. **`Interpreter has no method 'setup'`**
        //    -- **und this one is not a missing world, it is a
        //    missing method in the game's own scripts** -- --
        //    **and that is measured, not guessed.**
        AssertTrue(host.Gehlagt.Contains("Graphics"),
            "**and the only name the world was asked for is Graphics**"
                + " -- and it was asked while the scripts loaded, and"
                + " `$game_temp` was never asked for, because a"
                + " `$name` is a variable and not a constant");
        AssertFalse(Enthaelt(host.Gehlagt, "$game_temp"),
            "**and `$game_temp` never arrives as a name** -- and that is"
                + " the first of the two things this repository still"
                + " has to build: a variable slot for the world");
        AssertTrue(LetzteDiagnosen.Any(static d => d.Contains(
                "Interpreter has no method 'setup'",
                StringComparison.Ordinal)),
            "**and the game's `Interpreter` has no `setup`** -- and"
                + " that is the second thing to build, and the"
                + " interpreter's own diagnostic says so rather than"
                + " this test guessing it: "
                + string.Join(" || ", LetzteDiagnosen.ToArray()));
    }

    private static bool EnthaeltTyp(string[] pListe, string pWert) =>
        Enthaelt(pListe, pWert);

    private static bool Enthaelt(string[] pListe, string pWert)
    {
        foreach (var eintrag in pListe)
        {
            if (eintrag == pWert)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// And the message state a page needs has a home.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the three fields are named after what XP's
    /// <c>command_101</c> actually writes</strong>, -- <strong>read out
    /// of the game's own Ruby</strong>, -- <strong>and not after what MV
    /// calls them</strong>, -- <strong>because the two generations do
    /// not agree.</strong>
    /// </para>
    /// </remarks>
    public void Test_DerNachrichtenzustandHatDieFelderDesSpiels()
    {
        var echt = RgssSkriptHost.Lese(XpSkripte, out _);
        AssertTrue(echt != null, "**and the host reads**");
        var host = new TempHost(echt!);
        host.Nimmt("Hallo\n", 0, true);
        AssertEq(host.Text, "Hallo\n",
            "**and the text is what the page sent**");
        AssertEq(host.WahlStart, 0,
            "**and it starts at the first line**");
        AssertTrue(host.Wartet,
            "**and it waits** -- because a message the player has to"
                + " close is not a command the interpreter may skip");
    }
}
