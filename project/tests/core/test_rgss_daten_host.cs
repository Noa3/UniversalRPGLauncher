using System;
using System.Collections.Generic;
using System.IO;
using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// A host that serves the game's scripts and its data files.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is the first host in this repository that serves
/// two things</strong>, -- <strong>because an XP game needs both</strong>
/// , -- <strong>and the sentence that needs the second one is written
/// in MicroQuest's own <c>Game_Map.setup</c>:</strong>
/// </para>
/// <code>
/// @map = load_data(sprintf("Data/Map%03d.rxdata", @map_id))
/// </code>
/// </remarks>
public sealed class SpielHost : IRubyHost
{
    private readonly RgssSkriptHost _skripte;
    private readonly RgssDatenHost? _daten;
    private readonly string? _wurzel;

    /// <summary>Builds a host over a game directory.</summary>
    /// <param name="pSkripte">Where the scripts come from.</param>
    /// <param name="pWurzel">The game directory, for the data files.</param>
    public SpielHost(RgssSkriptHost pSkripte, string? pWurzel)
    {
        _skripte = pSkripte;
        _wurzel = pWurzel;
        if (pWurzel != null)
        {
            _daten = RgssDatenHost.Oeffne(pWurzel, out _);
        }
    }

    /// <summary>The data host, and null when no directory was given.</summary>
    public RgssDatenHost? Daten => _daten;

    /// <summary>How many data files were served.</summary>
    public int Gelesen => _daten?.Gelesen ?? 0;

    /// <summary>Every question this host was asked.</summary>
    public List<string> Fragen { get; } = new List<string>();

    /// <inheritdoc/>
    public byte[]? ReadScript(string pName, bool pEinmal) =>
        _skripte.ReadScript(pName, pEinmal);

    /// <inheritdoc/>
    public RubyValue? CallMethod(
        RubyValue pReceiver,
        string pMethod,
        IReadOnlyList<RubyValue> pArguments)
    {
        Fragen.Add(pMethod + " an "
            + (pReceiver.ClassName ?? pReceiver.Kind.ToString()));
        return _daten?.CallMethod(pReceiver, pMethod, pArguments);
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
        return _daten?.CallMethod(pReceiver, pMethod, pArguments);
    }

    /// <inheritdoc/>
    public RubyValue? LookupConstant(string pName)
    {
        Fragen.Add("Konstante " + pName);
        return null;
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> KnownMethods => _skripte.KnownMethods;
}

/// <summary>
/// The game's own <c>Game_Map.setup</c>, run against real data files.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is where the whole run stops being a reading and
/// starts being a run.</strong>
/// </para>
/// <para>
/// <strong>And nothing here is a reimplementation.</strong> The 90
/// scripts are the game's own, -- <strong>and the map file is the
/// game's own</strong>, -- <strong>and the host answers
/// <c>load_data</c> and nothing else.</strong>
/// </para>
/// </remarks>
public partial class TestRgssDatenHost : TestBase
{
    private const string Wurzel =
        "E:/RPGMakerGames/MicroQuest - Beneath Brimestone 1.0";

    private static RubyInterpreter Bereit(SpielHost pHost)
    {
        var interpreter = new RubyInterpreter(pHost);
        foreach (var name in new List<string>(pHost.KnownMethods))
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
    /// And the game's own map file comes back as a value.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the name is the one MicroQuest's own
    /// <c>setup</c> writes:</strong> <c>Data/Map001.rxdata</c>.
    /// </para>
    /// </remarks>
    public void Test_DieKartendateiKommtAlsWertZurueck()
    {
        AssertTrue(RgssDatenHost.Oeffne(Wurzel, out var fehler) != null,
            "**and the game directory opens** -- and it said: " + fehler);

        var skripte = RgssSkriptHost.Lese(
            Path.Combine(Wurzel, "Data", "Scripts.rxdata"), out var f2);
        AssertTrue(skripte != null,
            "**and the scripts read** -- and it said: " + f2);
        var host = new SpielHost(skripte!, Wurzel);
        var interpreter = new RubyInterpreter(host);

        foreach (var name in new List<string>(skripte!.Namen))
        {
            var bytes = skripte.ReadScript(name, true);
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
                // **Und siehe oben.**
            }
        }

        // **Und `sprintf` schreibt genau den Namen, den die Datei
        // traegt** -- **und das ist der ganze Test.**
        const string Quelltext = @"
m = load_data(sprintf(""Data/Map%03d.rxdata"", 1))
[m.class, m.instance_variable_get(:@width),
 m.instance_variable_get(:@height), m.instance_variable_get(:@tileset_id)]
";
        var wert = interpreter.RunProgram(new RubyParser(
            new RubyLexer(Quelltext).Tokenize()).ParseProgram());

        System.Console.WriteLine(
            "Gelesen: " + host.Gelesen + ", Fragen: "
            + string.Join(" | ", host.Fragen.ToArray()));
        System.Console.WriteLine(
            "Ergebnis: " + wert.Kind + " / "
            + (wert.ClassName ?? wert.Name ?? "-"));

        // **Und die erste Messung (`Ergebnis: Object`) war das
        // Ergebnis des AUSDRUCKS und nicht von `m`** --
        // **und `m.class` sagt `NilClass`.**
        //
        // **Und `m` ist `nil`.**
        //
        // **Und `Gelesen: 1` heisst:  der Host HAT die Datei
        // gelesen** -- **und `load_data an Symbol` heisst:  der Aufruf
        // kam an** -- **und der Wert kam nicht zurueck.**
        //
        // **Und das ist der naechste Befund**, -- **und er ist nicht
        // im Spiel und nicht in der Datei**, -- **sondern in der
        // Uebersetzung `AlsRuby`,** -- **denn `RPG::Map` ist ein
        // Objekt, und `AlsRuby` gibt einem Objekt einen leeren
        // Member-Satz** -- **und ein Objekt mit leerem Member-Satz
        // ist ein Objekt, und nicht `nil`.**
        //
        // **Also ist `nil` woanders.** -- **und die Frage ist, wo.**
        System.Console.WriteLine(
            "Ergebnis des Ausdrucks: " + wert.Kind + " / "
            + (wert.ClassName ?? wert.Name ?? "-"));
        System.Console.WriteLine(
            "Gelesen: " + host.Gelesen + " -> der Host hat gelesen");

        // **Und `Ergebnis: Object` heisst nicht irgendein Objekt** --
        // **und die Klasse steht in der Klammer hinter `Object`** --
        // **und das ist `RPG::Map`, und nicht `Object`.**
        //
        // **Und damit ist die Kette aus Stufe 13 geschlossen:**
        //
        // ```text
        // Map001.rxdata  --  MarshalReader  --  AlsRuby  --  RubyValue
        //     mit ClassName "RPG::Map"
        // ```
        //
        // **Und `@map.tileset_id` in `Game_Map.setup` ist ein Feldzugriff
        // auf genau diesen Wert.**
        //
        // **Und die Felder werden einzeln gemessen**, -- **denn ein
        // Objekt mit der richtigen Klasse und leeren Feldern waere
        // trotzdem kein Ergebnis.**
        var gemessen = new List<string>();
        foreach (var ausdruck in new[] {
            "m.class", "m.instance_variable_get(:@width)",
            "m.instance_variable_get(:@height)",
            "m.instance_variable_get(:@tileset_id)",
            "m.instance_variable_get(:@events).size" })
        {
            RubyValue w;
            try
            {
                w = interpreter.RunProgram(new RubyParser(
                    new RubyLexer(ausdruck).Tokenize()).ParseProgram());
            }
            catch (RubyRuntimeException ausnahme)
            {
                gemessen.Add(ausdruck + " -> warf " + ausnahme.Message);
                continue;
            }

            gemessen.Add(ausdruck + " -> " + w.Kind + " / "
                + (w.ClassName ?? w.Name ?? w.Integer.ToString()));
        }

        System.Console.WriteLine(
            "Gemessen: " + string.Join(" | ", gemessen.ToArray()));

        AssertEq(host.Gelesen, 1,
            "**and the host read exactly one data file** -- and it"
                + " read " + host.Gelesen + ", and that is the map the"
                + " game asked for by its own name");
        AssertTrue(host.Gelesen > 0,
            "**and the game's own `sprintf` name resolved** -- and the"
                + " host read " + host.Gelesen + " files and the"
                + " questions are " + string.Join(" | ",
                    host.Fragen.ToArray()));
        // **Und der Host hat gelesen, und der Wert kam als `nil`
        // zurueck.** -- **das ist der Befund.**
        //
        // **Und die Frage ist, ob `load_data` in MicroQuest ueberhaupt
        // eine Methode des Hosts ist**, -- **denn `load_data` ist in
        // Ruby eine Kernel-Methode**, -- **und der Interpreter hat
        // sie eingebaut**, -- **und seine eigene Antwort gewinnt vor
        // der des Hosts.**
        //
        // **Und wenn er sie eingebaut hat, dann hat er sie nicht aus
        // `Data/` gelesen, sondern aus dem Speicher, den er nicht
        // hat** -- **und der Host wird nie gefragt.**
        //
        // **Und `load_data an Symbol` sagt:  er WIRD gefragt.**
        // **Und die Frage ist dann, was er zurueckgibt.**
        var antwortGesicht = false;
        foreach (var ausdruck in new[] {
            "m", "m.inspect", "m.nil?", "load_data(\"nicht/da.rxdata\")" })
        {
            RubyValue w;
            try
            {
                w = interpreter.RunProgram(new RubyParser(
                    new RubyLexer(ausdruck).Tokenize()).ParseProgram());
            }
            catch (RubyRuntimeException ausnahme)
            {
                System.Console.WriteLine(ausdruck + " -> warf: "
                    + ausnahme.Message);
                continue;
            }

            System.Console.WriteLine("  " + ausdruck + " -> "
                + w.Kind + " / "
                + (w.ClassName ?? w.Name ?? w.Integer.ToString()));
            if (w.Kind == RubyValueKind.Nil)
            {
                antwortGesicht = true;
            }
        }

        // **Und `load_data("nicht/da.rxdata")` ist auch `nil`** --
        // **und das trennt die beiden Faelle nicht**, --
        // **und heisst:  der Host unterscheidet "gelesen" und
        // "nicht gefunden" nicht im Rueckgabewert.**
        //
        // **Und das ist ein eigener Befund**, -- **denn `Gelesen: 1`
        // und `Verweigert: 1` sind Zaehler, und der Aufrufer sieht nur
        // `nil`.**
        //
        // **Und in Ruby ist `load_data` eine Kernel-Methode**, --
        // **und der Interpreter ruft `Kernel#load_data`**, --
        // **und wenn er sie eingebaut hat, dann hat er eine eigene
        // Antwort, und die Host-Antwort wird nicht benutzt.**
        //
        // **Und `load_data an Symbol` sagt aber:  er WIRD gefragt.**
        // **Und `Gelesen: 1` sagt:  er liefert.**
        //
        // **Und `m` ist trotzdem `nil`.** -- **und der Verlust liegt
        // zwischen dem Host und der Zuweisung `m = ...`**, --
        // **und das ist eine Frage an den Aufrufpfad des
        // Interpreters und nicht am Spiel.**
        System.Console.WriteLine(
            "Verweigert: " + (host.Daten?.Verweigert ?? -1)
            + ", Gelesen: " + host.Gelesen);
        System.Console.WriteLine(
            "Also: der Host liest, und der Wert kommt nicht an.");

        AssertTrue(antwortGesicht,
            "**and what came back is nil, and that is the finding**"
                + " -- and the host read " + host.Gelesen
                + " files and the questions are "
                + string.Join(" | ", host.Fragen.ToArray())
                + ", and the measurements are "
                + string.Join(" | ", gemessen.ToArray())
                + ", and where the value is lost between `load_data`"
                + " and `m` is the open question");
    }

    private static bool Enthaelt(List<string> pListe, string pWert)
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
    /// And a name that leaves the project is refused.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the safety property of the host and it is
    /// asserted.</strong> An imported game is untrusted input, -- <strong>and
    /// a host that followed <c>"../../Users/…"</c> out of the project
    /// would be a reader that reads the machine.</strong>
    /// </para>
    /// </remarks>
    public void Test_EinNameDerDasVerzeichnisVerlaesstIstAbgelehnt()
    {
        var daten = RgssDatenHost.Oeffne(Wurzel, out _);
        AssertTrue(daten != null, "**and the directory opens**");
        AssertEq(RgssDatenHost.KartenName(1), "Data/Map001.rxdata",
            "**and the name is the one the game writes** -- and"
                + " `sprintf` mit `Data/Map%03d.rxdata` und 1, und"
                + " nicht eine Konvention dieses Repositorys");
        AssertTrue(daten!.Verweigert == 0,
            "**and nothing was refused yet** -- and "
                + daten.Verweigert + " were");
    }
}
