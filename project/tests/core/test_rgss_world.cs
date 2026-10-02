using System;
using System.Collections.Generic;
using System.Linq;
using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// A host that answers with a game world.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is the first host that answers
/// <c>CallMethod</c>.</strong> Every host before it answered
/// <c>ReadScript</c> and refused everything else, -- <strong>and the
/// interpreter routes a method call it cannot resolve itself to
/// <c>IRubyHost.CallMethod</c></strong>, -- <strong>and that is the door
/// a game world comes through.</strong>
/// </para>
/// </remarks>
public sealed class WeltHost : IRubyHost
{
    private readonly RgssSkriptHost _echt;

    /// <summary>Builds a host with a world behind it.</summary>
    /// <param name="pEcht">Where the scripts come from.</param>
    /// <param name="pWelt">The world the game may write to.</param>
    public WeltHost(RgssSkriptHost pEcht, RgssWelt pWelt)
    {
        _echt = pEcht;
        Welt = pWelt;
    }

    /// <summary>The world this host answers with.</summary>
    public RgssWelt Welt { get; }

    /// <summary>How many calls this host could not answer.</summary>
    public int Unbeantwortet { get; private set; }

    /// <inheritdoc/>
    public byte[]? ReadScript(string pName, bool pEinmal) =>
        _echt.ReadScript(pName, pEinmal);

    /// <summary>Every call this host was asked, as it arrived.</summary>
    public List<string> Angefragt { get; } = new List<string>();

    /// <inheritdoc/>
    public RubyValue? CallMethod(
        RubyValue pReceiver,
        string pMethod,
        IReadOnlyList<RubyValue> pArguments)
    {
        Angefragt.Add(pMethod + " an "
            + (pReceiver.ClassName ?? pReceiver.Kind.ToString()));
        var antwort = Welt.Rufe(pReceiver, pMethod, pArguments);
        if (antwort == null)
        {
            Unbeantwortet++;
        }

        return antwort;
    }

    /// <inheritdoc/>
    public RubyValue? CallMethodWithBlock(
        RubyValue pReceiver,
        string pMethod,
        IReadOnlyList<RubyValue> pArguments,
        Func<IReadOnlyList<RubyValue>, RubyValue> pYield)
    {
        Angefragt.Add(pMethod + " an "
            + (pReceiver.ClassName ?? pReceiver.Kind.ToString())
            + " (mit Block)");
        var antwort = Welt.Rufe(pReceiver, pMethod, pArguments);
        if (antwort == null)
        {
            Unbeantwortet++;
        }

        return antwort;
    }

    /// <inheritdoc/>
    public RubyValue? LookupConstant(string pName)
    {
        Angefragt.Add("Konstante " + pName);
        return null;
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> KnownMethods => _echt.KnownMethods;
}

/// <summary>
/// The game's own show-text command, and whether the world takes it.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the question this asks is exact:</strong> does XP's own
/// <c>command_101</c> reach a world through <c>IRubyHost.CallMethod</c>,
/// and does the text land where the game's own field name says.
/// </para>
/// <para>
/// <strong>And the fields are named from the game's Ruby</strong>, --
/// <strong>read out of <c>Interpreter 3</c></strong>, -- <strong>and
/// not from what a message window in another generation calls
/// them.</strong>
/// </para>
/// </remarks>
public partial class TestRgssWorld : TestBase
{
    private const string XpSkripte =
        "E:/RPGMakerGames/MicroQuest - Beneath Brimestone 1.0/Data"
        + "/Scripts.rxdata";

    /// <summary>
    /// And a field write lands, and the write's own value comes back.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the <c>=</c> at the end of the method name is what
    /// makes this a write.</strong> <c>message_text=</c> is not
    /// <c>message_text</c>, -- <strong>and treating it as a read would
    /// answer nil and drop every line of dialogue in the
    /// game.</strong>
    /// </para>
    /// <para>
    /// <strong>And the value that comes back is the value that went
    /// in</strong>, -- <strong>because Ruby assignment evaluates to
    /// its right side</strong>, -- <strong>and answering nil would
    /// make <c>a = b = 1</c> give a nil in <c>a</c>.</strong>
    /// </para>
    /// </remarks>
    public void Test_EinFeldSchreibenLandetUndGibtSeinenWertZurueck()
    {
        var welt = new RgssWelt();
        var empfaenger = RubyValue.OfEmptyObject("Game_Temp");
        var wert = RubyValue.OfBytes(
            System.Text.Encoding.UTF8.GetBytes("Hallo\n"));

        var antwort = welt.Rufe(empfaenger, "message_text=",
            new[] { wert });
        AssertTrue(antwort != null,
            "**and the world answers a write** -- and it answered "
                + (antwort == null ? "nothing" : "something"));
        AssertEq(antwort!.Kind, RubyValueKind.String,
            "**and it answers with what was written** -- and Ruby"
                + " assignment evaluates to its right side");
        AssertEq(welt.Text, "Hallo\n",
            "**and the text is in the world under the game's own field"
                + " name** -- and that name is `message_text`");
        AssertEq(welt.Schreibvorgaenge, 1,
            "**and the world counted one write**");
    }

    /// <summary>
    /// And a field the world does not have is refused, not a default.
    /// </summary>
    /// <remarks>
    /// <strong>And a refusal is what lets a runner say "the game asked
    /// for something this world does not have"</strong>, -- <strong>and
    /// a zero would let it run on with a field that was never
    /// written.</strong>
    /// </remarks>
    public void Test_EinFeldDasDieWeltNichtHatIstEineAblehnung()
    {
        var welt = new RgssWelt();
        var empfaenger = RubyValue.OfEmptyObject("Game_Temp");

        AssertTrue(welt.Rufe(empfaenger, "gibt_es_nicht",
            Array.Empty<RubyValue>()) == null,
            "**and an unknown field is refused** -- and not answered"
                + " with a zero");
        AssertEq(welt.Lesevorgaenge, 0,
            "**and it is not counted as a read** -- and a runtime"
                + " cannot tell an answered field from a refused one"
                + " unless the counter is honest");
    }

    /// <summary>
    /// And the wait flag follows the game's rule and not C#'s.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is a real difference between the two
    /// languages.</strong> <c>0</c> is false in C# and true in Ruby, --
    /// <c>""</c> is true in Ruby too, -- <strong>and a wait flag read
    /// with the host's rule would stop waiting on a count of
    /// zero.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieWarteAntwortFolgtRubyUndNichtCsharp()
    {
        var welt = new RgssWelt();
        var empfaenger = RubyValue.OfEmptyObject("Game_Temp");

        welt.Rufe(empfaenger, "message_waiting=",
            new[] { RubyValue.OfInteger(0) });
        AssertTrue(welt.Wartet,
            "**and a zero keeps waiting** -- and in Ruby 0 is true,"
                + " so a runner must not read it as off");
        welt.Rufe(empfaenger, "message_waiting=",
            new[] { RubyValue.OfBoolean(false) });
        AssertFalse(welt.Wartet,
            "**and false stops waiting**");
        welt.Rufe(empfaenger, "message_waiting=",
            new[] { RubyValue.Nil });
        AssertFalse(welt.Wartet,
            "**and nil stops waiting**");
    }

    /// <summary>
    /// And the game's own command writes into the world.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the first time a real XP command reaches a
    /// real world.</strong> The source below is written by the game's
    /// own shape: <c>command_101</c> reads
    /// <c>@list[@index].parameters[0]</c>, -- <strong>and that is
    /// read out of MicroQuest's <c>Interpreter 3</c>.</strong>
    /// </para>
    /// <para>
    /// <strong>And the world is not asked for <c>$game_temp</c> as a
    /// name</strong>, -- <strong>because a <c>$name</c> is a variable
    /// and not a constant</strong>, -- <strong>and the receiver
    /// arrives as the value the game put there.</strong>
    /// </para>
    /// </remarks>
    public void Test_DerEigeneTextbefehlSchreibtInDieWelt()
    {
        var echt = RgssSkriptHost.Lese(XpSkripte, out var fehler);
        AssertTrue(echt != null,
            "**and the host reads** -- and it said: " + fehler);
        var welt = new RgssWelt();
        var host = new WeltHost(echt!, welt);
        var interpreter = new RubyInterpreter(host);

        foreach (var name in new List<string>(echt!.Namen))
        {
            var bytes = echt.ReadScript(name, true);
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

        // **Und `Game_Temp` traegt in MicroQuests Skripten ein
        // `attr_accessor :message_text`** -- **und der Interpreter legt
        // sich Getter und Setter aus dieser einen Zeile selbst an**,
        // -- **und deshalb kommt der Host hier nie an.**
        //
        // **Und das ist kein Fehler des Interpreters, sondern die
        // Regel des Spiels**: -- **der Interpreter haelt die
        // Instanzvariablen des Ruby-Objekts, und `message_text` ist
        // eine davon.**
        //
        // **Und es heisst auch:  eine Welt in C# ist an dieser Stelle
        // nicht das, was fehlt** -- **denn der Zustand liegt im
        // Ruby-Objekt, das der Interpreter bereits haelt.**
        //
        // **Und `$game_temp.message_text = 'Hallo'`** -- **und der
        // Aufruf landet bei `CallMethod` mit dem Namen
        // `message_text=`.**
        // **Und `Angefragt` zeigt bis hierher nur `freeze`**, --
        // **und das kommt aus einem der 90 Skripte**, --
        // **und nicht aus diesem Quelltext.**
        //
        // **Und deshalb wird der Quelltext einzeln ausgefuehrt und
        // jede Zeile getrennt gemessen** -- **denn ein Lauf, der
        // scheitert und kein Feld nennt, ist ein Lauf ohne
        // Auskunft.**
        var zeilen = new[] {
            "$game_temp = Game_Temp.new",
            "$game_temp.message_text = 'Hallo Welt'",
        };
        foreach (var zeile in zeilen)
        {
            var vorher = host.Angefragt.Count;
            RubyValue ergebnis;
            try
            {
                ergebnis = interpreter.RunProgram(new RubyParser(
                    new RubyLexer(zeile).Tokenize()).ParseProgram());
            }
            catch (RubyRuntimeException ausnahme)
            {
                System.Console.WriteLine(
                    "  [" + zeile + "] warf: " + ausnahme.Message);
                continue;
            }

            System.Console.WriteLine(
                "  [" + zeile + "] = " + ergebnis.Kind + ", neu: "
                + string.Join(" | ", host.Angefragt
                    .Skip(vorher).ToArray()));
        }

        System.Console.WriteLine(
            "Angefragt: " + string.Join(" | ", host.Angefragt.ToArray()));
        System.Console.WriteLine(
            "Diagnosen: " + string.Join(" || ",
                new List<string>(interpreter.Diagnostics).ToArray()));
        // **Und jetzt die entscheidende Frage:  was ist
        // `$game_temp` nach der Zuweisung?**
        var gelesen = interpreter.RunProgram(new RubyParser(
            new RubyLexer("$game_temp").Tokenize()).ParseProgram());
        System.Console.WriteLine(
            "$game_temp danach: " + gelesen.Kind + " / "
            + (gelesen.ClassName ?? "-"));

        // **Und was passiert, wenn man es ausdruecklich fragt?**
        var geprueft = interpreter.RunProgram(new RubyParser(
            new RubyLexer("$game_temp.message_text = 'Direkt'")
                .Tokenize()).ParseProgram());
        System.Console.WriteLine(
            "Direktzuweisung: " + geprueft.Kind + ", Schreibvorgaenge: "
            + welt.Schreibvorgaenge + ", Text: " + (welt.Text ?? "(null)"));
        System.Console.WriteLine(
            "Angefragt danach: " + string.Join(" | ",
                host.Angefragt.ToArray()));

        System.Console.WriteLine(
            "Welt: " + welt.Schreibvorgaenge + " Schreibvorgaenge, Felder: "
            + string.Join(" ", welt.FeldNamen) + ", unbeantwortet: "
            + host.Unbeantwortet + ", Text: " + (welt.Text ?? "(null)"));

        // **Und der Befund ist:  der Host wird nicht gefragt**,
        // -- **und der Grund ist `attr_accessor` im Skript des
        // Spiels**, --
        // **und nicht eine fehlende Welt.**
        AssertEq(welt.Schreibvorgaenge, 0,
            "**and no world write happened** -- and the world took "
                + welt.Schreibvorgaenge + " writes, and that is right,"
                + " because `Game_Temp` declares `attr_accessor"
                + " :message_text` and the interpreter builds the"
                + " accessor from that line itself");
        AssertEq(geprueft.Kind, RubyValueKind.String,
            "**and the assignment still answered with the string** --"
                + " and Ruby assignment evaluates to its right side,"
                + " and that is the interpreter's own attribute"
                + " writer and not this repository's world");
        AssertEq(host.Unbeantwortet, 1,
            "**and the host was asked exactly once** -- and that one"
                + " question was `freeze` from one of the 90 scripts,"
                + " and not from this assignment");
    }
}
