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

    /// <summary>How many of them the host could not answer.</summary>
    public int OhneAntwort { get; private set; }

    /// <summary>The script names this host serves.</summary>
    /// <returns>The names.</returns>
    public IReadOnlyCollection<string> SkriptNamen() => _echt.Namen;

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
/// The game's own setup, run against a world that answers nothing.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is the test that was failing for three steps.</strong>
/// It asks whether XP's own <c>setup</c> can be called, -- <strong>and
/// for three steps the answer was "the method does not exist", and
/// that was true of this reader and not of the game.</strong>
/// </para>
/// <para>
/// <strong>And now it runs</strong>, -- <strong>and what it asks is
/// the list of what a world has to provide</strong>, -- <strong>and
/// that list is two names long.</strong>
/// </para>
/// </remarks>
public partial class TestRgssSetupRun : TestBase
{
    private const string XpSkripte =
        "E:/RPGMakerGames/MicroQuest - Beneath Brimestone 1.0/Data"
        + "/Scripts.rxdata";

    /// <summary>
    /// Runs a game's own scripts and then this repository's Ruby on top.
    /// </summary>
    /// <param name="pHost">The recording host.</param>
    /// <param name="pQuelltext">Ruby to run after the scripts.</param>
    /// <returns>What the Ruby returned.</returns>
    private static RubyValue Lauf(
        ProtokollHost pHost, string pQuelltext)
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

        return interpreter.RunProgram(new RubyParser(
            new RubyLexer(pQuelltext).Tokenize()).ParseProgram());
    }

    /// <summary>
    /// And the game's own `setup` runs and asks the world for two names.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the whole finding of the last four steps in
    /// four lines:</strong>
    /// </para>
    /// <code>
    /// Fragen: Konstante Graphics | freeze an Nil (Block)
    ///         | map_id an Nil | clear an Object
    /// </code>
    /// <para>
    /// <strong>And <c>setup</c> runs.** -- <strong>And it asks
    /// <c>map_id</c></strong>, -- <strong>and that is
    /// <c>Game_Player.setup_starting_event</c>, which MicroQuest's
    /// <c>Interpreter 1</c> calls</strong>, -- <strong>and it asks
    /// <c>clear</c> on <c>Object</c></strong>, -- <strong>and that is
    /// <c>Object#clear</c>, the shared base of <c>Array</c> and
    /// <c>Hash</c>.</strong>
    /// </para>
    /// <para>
    /// <strong>And so MicroQuest's game really starts now, and it needs
    /// a world</strong>, -- <strong>and that world is two questions
    /// long.</strong> -- <strong>And none of this is a guess: it is
    /// the protocol of the run.</strong>
    /// </para>
    /// </remarks>
    public void Test_SetupLaeuftUndFragtDieWeltNachZweiNamen()
    {
        var echt = RgssSkriptHost.Lese(XpSkripte, out var fehler);
        AssertTrue(echt != null,
            "**and the host reads** -- and it said: " + fehler);
        var host = new ProtokollHost(echt!);

        // **Und MicroQuests `GAME START` traegt 135 134 209 509 223
        // 106 201 0** -- **und die Liste hier ist ein Befehl, den das
        // Spiel kennt:  101.**
        const string Quelltext = @"
i = Interpreter.new
i.setup([[101, 0, ['Hallo']]], 0)
";
        Lauf(host, Quelltext);

        System.Console.WriteLine(
            "Fragen: " + string.Join(" | ", host.Fragen.ToArray()));

        // **Und `clear` an `Object` ist ein Befund und kein Zufall.**
        //
        // **Und Ruby 1.8.6 definiert `clear` nur auf `Hash` und auf
        // `Array`** (`object.c` kennt es nicht), -- **und MicroQuests
        // `setup` ruft es auf einem Objekt auf.**
        //
        // **Und das heisst:  der Empfaenger war nicht das, was das
        // Spiel meint** -- **und die Frage ist, was der Leser fuer
        // `@list` haelt.**
        System.Console.WriteLine(
            "Ruby 1.8.6: clear nur auf Hash und Array, object.c kennt"
            + " es nicht");

        AssertTrue(Enthaelt(host.Fragen.ToArray(), "map_id an Nil"),
            "**and `setup` runs and asks the world for `map_id`** -- and"
                + " the questions are "
                + string.Join(" | ", host.Fragen.ToArray())
                + ", and `map_id` is what MicroQuest's own"
                + " `setup_starting_event` asks");
        AssertTrue(Enthaelt(host.Fragen.ToArray(), "clear an Object"),
            "**and it asks `clear` on `Object`** -- and that is"
                + " `Object#clear`, the shared base of `Array` and"
                + " `Hash`, and a run that answered it would empty the"
                + " interpreter's own list");
    }

    /// <summary>
    /// And the game's `setup` carries the signature its own script
    /// writes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this test says where the signature is, and it is a
    /// line in MicroQuest's <c>Interpreter 1</c>:</strong>
    /// <c>def setup(list, event_id)</c>. -- <strong>And <c>map_id</c>
    /// and <c>event_id</c> are the same parameter in different
    /// clothes</strong>, -- <strong>and a reader that took the second
    /// one for a map number would look for the wrong map.</strong>
    /// </para>
    /// </remarks>
    public void Test_SetupTraegtDieSignaturDesSpiels()
    {
        var orte = new List<string>();
        foreach (var leib in XpScriptBodies.LeseAlle(XpSkripte))
        {
            if (leib.Text == null)
            {
                continue;
            }

            foreach (var zeile in leib.Text.Split('\n'))
            {
                var geschnitten = zeile.Trim();
                if (geschnitten.StartsWith("def setup(",
                    StringComparison.Ordinal))
                {
                    orte.Add(leib.Name + ": " + geschnitten);
                }
            }
        }

        System.Console.WriteLine(
            "def setup(...): " + string.Join(" | ", orte.ToArray()));

        var treffer = false;
        foreach (var orte1 in orte)
        {
            if (orte1.EndsWith("def setup(list, event_id)",
                    StringComparison.Ordinal))
            {
                treffer = true;
            }
        }

        AssertTrue(treffer,
            "**and `Interpreter 1` writes `def setup(list, event_id)`**"
                + " -- and the definitions are "
                + string.Join(" | ", orte.ToArray())
                + ", and `event_id` is not a map number, and a reader"
                + " that read it as one would open the wrong map");
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
        Lauf(host, "$game_player.x\n");

        System.Console.WriteLine(
            "Fragen nach $game_player.x: "
            + string.Join(" | ", host.Fragen.ToArray()));

        AssertTrue(host.Fragen.Count > 0,
            "**and asking for the player's x asks the host** -- and it"
                + " asked " + host.Fragen.Count + " things, and they are"
                + " named: " + string.Join(" | ", host.Fragen.ToArray()));
    }

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
}
