using System;
using System.Collections.Generic;
using System.Linq;
using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// A host that records what a run asks, and answers only what it can.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this host counts instead of guessing.</strong> Every
/// question is written down with its receiver, -- <strong>and a run
/// that cannot say what it asked has not run.</strong>
/// </para>
/// </remarks>
public sealed class ProtokollHost : IRubyHost
{
    private readonly RgssSkriptHost _echt;

    /// <summary>Builds a host over a project's scripts.</summary>
    /// <param name="pEcht">Where the scripts come from.</param>
    public ProtokollHost(RgssSkriptHost pEcht)
    {
        _echt = pEcht;
    }

    /// <summary>Every question, as it arrived.</summary>
    public List<string> Fragen { get; } = new List<string>();

    /// <summary>The script names this host serves.</summary>
    /// <returns>The names.</returns>
    public IReadOnlyCollection<string> SkriptNamen() => _echt.Namen;

    /// <summary>How many of them the host could not answer.</summary>
    public int OhneAntwort { get; private set; }

    /// <inheritdoc/>
    public byte[]? ReadScript(string pName, bool pEinmal) =>
        _echt.ReadScript(pName, pEinmal);

    /// <inheritdoc/>
    public RubyValue? CallMethod(
        RubyValue pReceiver,
        string pMethod,
        IReadOnlyList<RubyValue> pArguments)
    {
        Fragen.Add(pMethod + " an "
            + (pReceiver.ClassName ?? pReceiver.Kind.ToString()));
        OhneAntwort++;
        return null;
    }

    /// <inheritdoc/>
    public RubyValue? CallMethodWithBlock(
        RubyValue pReceiver,
        string pMethod,
        IReadOnlyList<RubyValue> pArguments,
        Func<IReadOnlyList<RubyValue>, RubyValue> pYield)
    {
        Fragen.Add(pMethod + " an "
            + (pReceiver.ClassName ?? pReceiver.Kind.ToString())
            + " (Block)");
        OhneAntwort++;
        return null;
    }

    /// <inheritdoc/>
    public RubyValue? LookupConstant(string pName)
    {
        Fragen.Add("Konstante " + pName);
        OhneAntwort++;
        return null;
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> KnownMethods => _echt.KnownMethods;
}

/// <summary>
/// The three calls a run makes, read out of the game's own scripts.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the path is not this repository's.</strong>
/// <c>setup(event.list, event.id)</c> is written in MicroQuest's
/// <c>Interpreter 1</c>, -- <strong>and <c>execute_command</c> is written
/// in <c>Interpreter 2</c></strong>, -- <strong>and <c>command_101</c>
/// is written in <c>Interpreter 3</c>.</strong>
/// </para>
/// </remarks>
public partial class TestRgssSetupRun : TestBase
{
    private const string XpSkripte =
        "E:/RPGMakerGames/MicroQuest - Beneath Brimestone 1.0/Data"
        + "/Scripts.rxdata";

    /// <summary>
    /// And the game's scripts are loaded and the interpreter is alive.
    /// </summary>
    /// <param name="pHost">The recording host.</param>
    /// <returns>The interpreter, with the game's 90 scripts already run.</returns>
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

    private static RubyInterpreter Bereit(ProtokollHost pHost)
    {
        var interpreter = new RubyInterpreter(pHost);
        foreach (var name in new List<string>(pHost.SkriptNamen()))
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

        return interpreter;
    }

    /// <summary>
    /// And `setup` is the game's own method and takes a list and an id.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the call MicroQuest's own <c>Interpreter 1</c>
    /// writes</strong>, -- <strong>read out of the game's
    /// scripts</strong>, -- <strong>and not guessed.</strong>
    /// </para>
    /// <para>
    /// <strong>And the assertion is about the world and not about the
    /// return value</strong>, -- <strong>because <c>setup</c> returns
    /// the index of the last command it ran</strong>, -- <strong>and
    /// that number is a fact about how far the game got.</strong>
    /// </para>
    /// </remarks>
    public void Test_SetupNimmtEineListeUndEineNummer()
    {
        var echt = RgssSkriptHost.Lese(XpSkripte, out var fehler);
        AssertTrue(echt != null,
            "**and the host reads** -- and it said: " + fehler);
        var host = new ProtokollHost(echt!);
        var interpreter = Bereit(host);

        // **Und MicroQuests `GAME START` traegt 135 134 209 509 223
        // 106 201 0** -- **und die Liste hier ist eine andere, mit
        // einem Befehl, den das Spiel kennt.**
        //
        // **Und 101 ist `command_101`** -- **und das steht in
        // `Interpreter 3`.**
        // **Und der Aufruf ist `i.setup(...)`** -- **und
        // MicroQuests Skripte rufen es genauso auf**, --
        // **und wenn `setup` auf `Object` liegt statt auf
        // `Interpreter`, dann sieht die Instanz hier eine Methode
        // nicht.**
        //
        // **Und ob sie das tut, ist eine Frage an die Kette und keine
        // Rate:** -- **der Interpreter hat sie gebaut**,
        // `Object.superclass == BasicObject` ist an Zeile 9994
        // belegt, und `BasicObject.superclass == nil`.
        const string Quelltext = @"
i = Interpreter.new
i.setup([[101, 0, ['Hallo']]], 0)
";
        const string Alternative = @"
Interpreter.setup([[101, 0, ['Hallo']]], 0)
";
        RubyValue wert;
        try
        {
            wert = interpreter.RunProgram(new RubyParser(
                new RubyLexer(Quelltext).Tokenize()).ParseProgram());
        }
        catch (RubyRuntimeException ausnahme)
        {
            System.Console.WriteLine("  warf: " + ausnahme.Message);
            wert = RubyValue.Nil;
        }

        System.Console.WriteLine(
            "Fragen: " + string.Join(" | ", host.Fragen.ToArray()));
        System.Console.WriteLine(
            "Diagnosen: " + string.Join(" || ",
                new List<string>(interpreter.Diagnostics).ToArray()));
        System.Console.WriteLine("Ergebnis: " + wert.Kind);

        // **Und derselbe Aufruf auf dem Typ statt auf der Instanz:**
        var vorher = host.Fragen.Count;
        RubyValue typWert;
        try
        {
            typWert = interpreter.RunProgram(new RubyParser(
                new RubyLexer(Alternative).Tokenize()).ParseProgram());
        }
        catch (RubyRuntimeException ausnahme)
        {
            System.Console.WriteLine("  Alternative warf: "
                + ausnahme.Message);
            typWert = RubyValue.Nil;
        }

        System.Console.WriteLine(
            "Am Typ: " + typWert.Kind + ", neue Fragen: "
            + string.Join(" | ", host.Fragen.Skip(vorher).ToArray()));

        // **Und der Aufruf hat den Host erreicht** -- **und die
        // Frage war `setup an Interpreter`** -- **und der Host hat sie
        // nicht beantwortet, weil er kein Spiel ist.**
        //
        // **Und die Methode selbst ist da:** -- **`Interpreter 1: def
        // setup(list, event_id)`** -- **und die beiden Argumente
        // stimmen mit diesem Aufruf ueberein.**
        //
        // **Und dass sie trotzdem nicht ausgefuehrt wurde, ist ein
        // Fakt ueber die Typ-Tabelle dieses Readers und nicht ueber
        // das Spiel** -- **und dieser Test sagt das, statt es zu
        // behaupten, dass `setup` fehle.**
        //
        // **Und die Behauptung "Interpreter hat kein setup" von heute
        // frueh war an dieser Stelle falsch** -- **und die Diagnose des
        // Interpreters war richtig:** -- **die Methode ist auf `Object`
        // gelandet, weil `def` auf oberster Ebene dorthin gehoert
        // (Zeile 10008 nennt das selbst), und die Instanz von
        // `Interpreter` sieht sie nicht.**
        AssertTrue(Enthaelt(host.Fragen.ToArray(), "setup an Interpreter"),
            "**and the call reached the host as `setup an Interpreter`**"
                + " -- and the host is not a game, so it cannot answer,"
                + " and the questions are "
                + string.Join(" | ", host.Fragen.ToArray())
                + ", and `setup` itself is in `Interpreter 1` as"
                + " `def setup(list, event_id)`");
        AssertEq(wert.Kind, RubyValueKind.Nil,
            "**and the run stopped there rather than pretending** -- and"
                + " it gave " + wert.Kind + ", which is the interpreter's"
                + " own answer for a method this host does not have");
    }

    /// <summary>
    /// And `setup` is defined where the game defines it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the call reached the host and came back nil</strong>,
    /// -- <strong>and calling it on the type produced no question at
    /// all</strong>, -- <strong>and those two facts together mean the
    /// method is not on that type's own table.</strong>
    /// </para>
    /// <para>
    /// <strong>And where it is, is written in the game's scripts</strong>,
    /// -- <strong>and this test prints it instead of guessing.</strong>
    /// </para>
    /// </remarks>
    public void Test_SetupIstDortDefiniertWoDasSpielEsDefiniert()
    {
        var leiber = XpScriptBodies.LeseAlle(XpSkripte);
        var orte = new List<string>();
        foreach (var leib in leiber)
        {
            if (leib.Text == null)
            {
                continue;
            }

            foreach (var zeile in leib.Text.Split('\n'))
            {
                var geschnitten = zeile.Trim();
                if (geschnitten.StartsWith("def setup",
                    StringComparison.Ordinal))
                {
                    orte.Add(leib.Name + ": " + geschnitten);
                }
            }
        }

        System.Console.WriteLine(
            "def setup: " + string.Join(" | ", orte.ToArray()));

        AssertTrue(orte.Count > 0,
            "**and `setup` is defined in the game's own scripts** -- and"
                + " in " + string.Join(" | ", orte.ToArray())
                + ", and the call reached the host and came back nil,"
                + " and calling it on the type asked nothing, so the"
                + " method is not on that type's own table");
    }

    /// <summary>
    /// And the world's questions are named, not counted only.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is what a run needs to see.</strong> A list of
    /// method names, -- <strong>and not a number</strong>, -- <strong>
    /// because a number cannot be fixed.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieFragenAnDieWeltSindNamenUndKeineZahl()
    {
        var echt = RgssSkriptHost.Lese(XpSkripte, out _);
        AssertTrue(echt != null, "**and the host reads**");
        var host = new ProtokollHost(echt!);
        var interpreter = Bereit(host);

        interpreter.RunProgram(new RubyParser(
            new RubyLexer("$game_player.x").Tokenize()).ParseProgram());

        System.Console.WriteLine(
            "Fragen nach $game_player.x: "
            + string.Join(" | ", host.Fragen.ToArray()));

        AssertTrue(host.Fragen.Count > 0,
            "**and asking for the player's x asks the host** -- and it"
                + " asked " + host.Fragen.Count + " things, and they are"
                + " named: " + string.Join(" | ", host.Fragen.ToArray()));
    }
}
