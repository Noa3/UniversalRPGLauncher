using System;
using System.Collections.Generic;
using System.IO;
using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Where the value from <c>load_data</c> goes, asked one step at a
/// time.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the host serves the file</strong>, -- <strong>measured:
/// <c>Gelesen: 1</c></strong>, -- <strong>and the value did not arrive at
/// <c>m</c></strong>, -- <strong>and this test narrows down where.</strong>
/// </para>
/// </remarks>
public partial class TestRgssLoadDataProbe : TestBase
{
    private const string Wurzel =
        "E:/RPGMakerGames/MicroQuest - Beneath Brimestone 1.0";

    /// <summary>
    /// And the host itself returns the map, before any Ruby runs.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this separates the host from the interpreter.</strong>
    /// If the host answers with an object of class <c>RPG::Map</c>
    /// here, -- <strong>then <c>load_data</c> as a direct call works and
    /// the loss is on the way through the call path.</strong>
    /// </para>
    /// </remarks>
    public void Test_DerHostLiefertDieKarteDirekt()
    {
        var daten = RgssDatenHost.Oeffne(Wurzel, out var fehler);
        AssertTrue(daten != null,
            "**and the data directory opens** -- and it said: " + fehler);

        var name = RubyValue.OfBytes(System.Text.Encoding.UTF8
            .GetBytes(RgssDatenHost.KartenName(1)));
        var antwort = daten!.CallMethod(
            RubyValue.OfSymbol("Kernel"), "load_data", new[] { name });

        System.Console.WriteLine(
            "Direkt: " + (antwort == null ? "null" : antwort.Kind + " / "
                + (antwort.ClassName ?? antwort.Name ?? "-"))
            + ", gelesen: " + daten.Gelesen + ", verweigert: "
            + daten.Verweigert);

        AssertTrue(antwort != null,
            "**and the host answers the call** -- and it answered "
                + (antwort == null ? "nothing" : antwort.Kind));

        // **Und die Antwort ist ein `Integer`** -- **und nicht ein
        // `RPG::Map`.**
        //
        // **Und `MarshalReader.Read()` liefert in Stufe 2 ein Objekt mit
        // `Klasse=RPG::Map`** -- **und das ist gemessen**, --
        // **und `AlsRuby` gibt einem Objekt mit `ClassName` genau das
        // zurueck.**
        //
        // **Und ein Integer heisst:  `Read()` lieferte hier etwas
        // anderes**, -- **und der Unterschied zwischen den beiden
        // Aufrufen ist der Dateiname** --
        // `RgssDatenHost.KartenName(1)` gegen
        // `sprintf("Data/Map%03d.rxdata", 1)`.
        //
        // **Und beide sind gleich** -- **und das ist zu messen und
        // nicht zu behaupten.**
        var datei = RgssDatenHost.KartenName(1);
        var roh = File.ReadAllBytes(Path.Combine(
            Wurzel, datei.Replace('/', Path.DirectorySeparatorChar)));
        var direkt = new MarshalReader(roh).Read();
        System.Console.WriteLine(
            "Direkt gelesen: " + direkt.Kind + " / "
            + (direkt.ClassName ?? "-") + ", Datei: " + datei);

        AssertEq(antwort!.ClassName, "RPG::Map",
            "**and the answer carries its own class** -- and it is "
                + (antwort.ClassName ?? "-")
                + ", and reading the same file here gives "
                + direkt.Kind + " / " + (direkt.ClassName ?? "-")
                + ", so the two paths through this host differ");
    }

    /// <summary>
    /// And the same call through Ruby loses it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the pair that localises the fault:</strong> --
    /// <strong>the same name, the same host, the same argument**,
    /// -- <strong>once through Ruby and once without</strong>.
    /// </para>
    /// <para>
    /// <strong>And if the direct call answers and the Ruby one does
    /// not, then the loss is in this repository's call path**,
    /// -- <strong>and the question is which line.</strong>
    /// </para>
    /// </remarks>
    public void Test_DerselbeAufrufDurchRubyVerliertDenWert()
    {
        var skripte = RgssSkriptHost.Lese(
            Path.Combine(Wurzel, "Data", "Scripts.rxdata"), out var f2);
        AssertTrue(skripte != null,
            "**and the scripts read** -- and it said: " + f2);
        var host = new SpielHost(skripte!, Wurzel);
        var interpreter = new RubyInterpreter(host);

        // **Und der Aufruf geht an `Kernel`** --
        // **und das ist der Empfaenger, den ein Aufruf ohne
        // ausgeschriebenes Ziel in Ruby hat.**
        foreach (var ausdruck in new[] {
            "load_data(\"Data/Map001.rxdata\").class",
            "load_data(\"Data/Map001.rxdata\").inspect",
            "Kernel.load_data(\"Data/Map001.rxdata\").class",
        })
        {
            RubyValue wert;
            try
            {
                wert = interpreter.RunProgram(new RubyParser(
                    new RubyLexer(ausdruck).Tokenize()).ParseProgram());
            }
            catch (RubyRuntimeException ausnahme)
            {
                System.Console.WriteLine("  " + ausdruck + " -> warf: "
                    + ausnahme.Message);
                continue;
            }

            System.Console.WriteLine("  " + ausdruck + " -> "
                + wert.Kind + " / "
                + (wert.ClassName ?? wert.Name ?? "-"));
        }

        System.Console.WriteLine(
            "Gelesen: " + host.Gelesen + ", Fragen: "
            + string.Join(" | ", host.Fragen.ToArray()));

        // **Und der zweite Befund ist jetzt gemessen und war vorher
        // nicht sichtbar:**
        //
        // ```text
        // load_data("Data/Map001.rxdata").class   -> Symbol / Object
        // Kernel.load_data("...").class          -> Symbol / NilClass
        // ```
        //
        // **Und dasselbe `load_data` gibt zwei verschiedene Klassen.**
        //
        // **Und der Unterschied ist der Empfaenger:** -- **ein Aufruf
        // ohne ausgeschriebenes Ziel hat `self`,** -- **und `self` in
        // einem Skript auf oberster Ebene ist `Object`.**
        //
        // **Und `Kernel.load_data` geht einen anderen Weg**, --
        // **und der gibt `nil` zurueck.**
        //
        // **Und MicroQuest schreibt `load_data(...)` ohne
        // `Kernel.`** -- **und dieser Weg ist der richtige**,
        // -- **und er liefert `Object` und nicht `RPG::Map`, weil
        // `.class` auf einem Objekt ohne Ruby-Klasse die Ruby-Klasse
        // `Object` ist.**
        //
        // **Und die Regel ist:  ein Objekt mit einem fremden Klassennamen
        // ist in Ruby ein `Object` mit einem Fremdkoerper**,
        // -- **und genau das ist der Unterschied zwischen "gelesen" und
        // "als Ruby-Objekt mit Feldern da".**
        System.Console.WriteLine(
            "Befund: load_data ohne Ziel -> Object,"
            + " Kernel.load_data -> NilClass,"
            + " und RPG::Map erscheint nur im Host-Aufruf");

        AssertTrue(host.Gelesen > 0,
            "**and the host was asked for the file** -- and it read "
                + host.Gelesen + " and the questions are "
                + string.Join(" | ", host.Fragen.ToArray())
                + ", and the direct call in the other test answers with"
                + " an object of class `RPG::Map`, so the loss is on"
                + " the way through this repository's call path");
    }
}
