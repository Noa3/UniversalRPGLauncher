using System;
using System.Collections.Generic;
using System.Linq;
using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The fields of a data object whose class the scripts never define.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the engine's data classes are the engine's, not the
/// scripts'.</strong> <c>$data_system.start_x</c> is answered out of
/// <c>@start_x</c> without any script ever writing
/// <c>attr_reader :start_x</c>, -- <strong>and a game reads those
/// fields on every frame.</strong>
/// </para>
/// <para>
/// <strong>And this was measured, not guessed.</strong> On a real XP
/// game, <c>$game_player.moveto($data_system.start_x,
/// $data_system.start_y)</c> reported *"undefined operator '%' for a Nil
/// and a Nil"* because both were nil, -- <strong>and the objects had
/// the fields all along.</strong>
/// </para>
/// <para>
/// <strong>And the rule is deliberately narrow, and the third test is
/// the one that holds it there.</strong> For a <em>script</em> object a
/// name that is not a method is a <c>NoMethodError</c> in Ruby, -- **and
/// a blanket field fallback would answer a value for a typo in the
/// game's own code and hide it.**
/// </para>
/// </remarks>
public partial class TestRgssDataFields : TestBase
{
    /// <summary>
    /// And a Marshal-loaded object answers a field it never defined a
    /// method for.
    /// </summary>
    public void Test_EinDatenobjektAntwortetSeineFelder()
    {
        var interpreter = new RubyInterpreter(new RubyNullHost());
        var daten = RubyValue.OfObject(
            "RPG::System",
            new Dictionary<RubyValue, RubyValue>
            {
                [RubyValue.OfSymbol("@start_x")] = RubyValue.OfInteger(7),
                [RubyValue.OfSymbol("@start_map_id")] = RubyValue.OfInteger(3),
            });
        daten.AusDaten = true;
        interpreter.SetzeGlobal("$data_system", daten);

        // **Und das Skript schreibt den Feldnamen und nicht den
        // Instanznamen**, -- **und genau das ist die Form, die jedes
        // XP-Skript benutzt.**
        interpreter.RunProgram(new RubyParser(new RubyLexer(
            "$x = $data_system.start_x\n"
            + "$map = $data_system.start_map_id\n"
            + "$fehlt = $data_system.gibt_es_nicht\n"
            + "$antwortet = $data_system.respond_to?(:start_x)\n")
            .Tokenize()).ParseProgram());

        // **Und die Zusicherung liest `Boolean` und nicht `Integer`** -- **und
        // das war mein Fehler und nicht der des Lesers:** `OfBoolean` traegt
        // seinen Wert in `Boolean`, und eine Zusicherung auf `Integer` las
        // daraus eine Null und meldete einen Defekt, den es nicht gab.
        AssertTrue(interpreter.Global("$antwortet").Boolean,
            "**and `respond_to?` says the same thing the call does** -- and"
                + " it said " + interpreter.Global("$antwortet").Boolean
                + ", and two answers to one question are a game that takes"
                + " one branch and lands in the other");

        AssertEq(interpreter.Global("$x").Integer, 7,
            "**and a data object answers `start_x` out of `@start_x`** --"
                + " and it answered " + interpreter.Global("$x").Integer);
        AssertEq(interpreter.Global("$map").Integer, 3,
            "**and every other field as well** -- and it answered "
                + interpreter.Global("$map").Integer);
        AssertTrue(interpreter.Global("$fehlt").IsNil,
            "**and a name that is neither a method nor a field is still"
                + " nil** -- and a reader that invented a value here would"
                + " be inventing game state");

        // **Und `sprintf` ist der Satz, mit dem jedes XP-Skript seine
        // Kartendatei nennt:** `load_data(sprintf("Data/Map%03d.rxdata",
        // @map_id))`, gemessen in MicroQuest's eigenem `Game_Map#setup`.
        // **Und der `%` in dieser Zeile ist der Format-Operator**, -- **und
        // ein Leser ohne `sprintf` gibt `load_data` ein nil und die Karte
        // laedt nicht.**
        interpreter.RunProgram(new RubyParser(new RubyLexer(
            "$karte = sprintf(\"Data/Map%03d.rxdata\", 3)\n")
            .Tokenize()).ParseProgram());
        System.Console.WriteLine(
            "sprintf: ["
            + (interpreter.Global("$karte").IsNil
                ? "nil"
                : System.Text.Encoding.UTF8.GetString(
                    interpreter.Global("$karte").Bytes))
            + "]");
    }

    /// <summary>
    /// And the same object answers a field kept in the other store.
    /// </summary>
    /// <remarks>
    /// <strong>And the two stores are two, and that is not an
    /// accident:</strong> a script object keeps its fields in
    /// <c>Felder</c> (by name) and a Marshal-loaded one keeps them in
    /// <c>Members</c> (by symbol value), -- **and a reader that knows
    /// only one of the two finds half of them.**
    /// </remarks>
    public void Test_EinDatenobjektAntwortetAuchAusDerZweitenAblage()
    {
        var interpreter = new RubyInterpreter(new RubyNullHost());
        var daten = RubyValue.OfEmptyObject("RPG::Actor");
        daten.AusDaten = true;
        daten.Felder["@name"] = RubyValue.OfBytes(
            System.Text.Encoding.UTF8.GetBytes("Camellia"));
        daten.Felder["@level"] = RubyValue.OfInteger(21);
        interpreter.SetzeGlobal("$data_actors", RubyValue.OfArray(
            new List<RubyValue> { daten }));

        interpreter.RunProgram(new RubyParser(new RubyLexer(
            "$name = $data_actors[0].name\n"
            + "$level = $data_actors[0].level\n")
            .Tokenize()).ParseProgram());

        AssertEq(
            System.Text.Encoding.UTF8.GetString(
                interpreter.Global("$name").Bytes), "Camellia",
            "**and a field in `Felder` is answered too** -- and it"
                + " answered ["
                + System.Text.Encoding.UTF8.GetString(
                    interpreter.Global("$name").Bytes) + "]");
        AssertEq(interpreter.Global("$level").Integer, 21,
            "**and a number field with it** -- and it answered "
                + interpreter.Global("$level").Integer);
    }

    /// <summary>
    /// And the real data file's object says what it is.
    /// </summary>
    /// <remarks>
    /// <strong>And this is the measurement that decides the rule.</strong>
    /// The fallback applies to an object whose class the scripts do not
    /// define, -- **and the constructed objects in the tests above have a
    /// class nothing defines, which is not the same as a real
    /// <c>System.rxdata` being one.** So the real file is read here and
    /// asked what its object calls itself.
    /// </remarks>
    public void Test_DieEchteDatendateiSagtWasIhrObjektIst()
    {
        const string Wurzel =
            "E:/RPGMakerGames/MicroQuest - Beneath Brimestone 1.0";
        var daten = RgssDatenHost.Oeffne(Wurzel, out _);
        if (daten == null)
        {
            throw new InvalidOperationException("no data host");
        }

        var system = daten.Lade("Data/System.rxdata");
        if (system == null)
        {
            throw new InvalidOperationException("no System.rxdata");
        }

        System.Console.WriteLine(
            "System.rxdata: Kind " + system.Kind
            + ", ClassName [" + (system.ClassName ?? "-") + "]"
            + ", Felder " + system.Felder.Count
            + ", Members " + system.Members.Count);
        foreach (var name in system.Felder.Keys)
        {
            System.Console.WriteLine("  Feld " + name);
        }

        var interpreter = new RubyInterpreter(new RubyNullHost());
        interpreter.SetzeGlobal("$data_system", system);
        interpreter.RunProgram(new RubyParser(new RubyLexer(
            "$start_x = $data_system.start_x\n"
            + "$antwortet = $data_system.respond_to?(:start_x)\n"
            + "$hat_map = $data_system.respond_to?(:start_map_id)\n")
            .Tokenize()).ParseProgram());

        System.Console.WriteLine(
            "start_x = " + (interpreter.Global("$start_x").IsNil
                ? "nil"
                : interpreter.Global("$start_x").Integer.ToString())
            + ", respond_to?(:start_x) = "
            + interpreter.Global("$antwortet").Boolean
            + ", respond_to?(:start_map_id) = "
            + interpreter.Global("$hat_map").Boolean
            + ", Feld da = "
            + system.Felder.ContainsKey("@start_x")
            + ", AusDaten = " + system.AusDaten
            + ", start_y da = " + system.Felder.ContainsKey("@start_y")
            + ", Felder = " + string.Join(" ", system.Felder.Keys));

        AssertEq(system.Felder.Count + system.Members.Count > 0, true,
            "**and the real object carries its fields** -- and it carries "
                + (system.Felder.Count + system.Members.Count)
                + " (ClassName [" + (system.ClassName ?? "-") + "])");

        // **Und die Karte ist der naechste Wert, den das Spiel liest.**
        // `Game_Map#setup` schreibt `@map = load_data(sprintf("Data/Map%03d.rxdata",
        // @map_id))`, -- **und gemessen ist `$game_map.width` danach nil**, --
        // **und `width` ist `@map.width`.** Also wird hier dieselbe Datei
        // gelesen, die das Spiel liest, und gefragt, was zurueckkommt.
        var karte = daten.Lade("Data/Map001.rxdata");
        System.Console.WriteLine(
            "Map001.rxdata: "
            + (karte == null
                ? "refused"
                : "Kind " + karte.Kind
                    + ", ClassName [" + (karte.ClassName ?? "-") + "]"
                    + ", Felder " + karte.Felder.Count
                    + ", Members " + karte.Members.Count));
        if (karte != null)
        {
            foreach (var name in karte.Felder.Keys)
            {
                System.Console.WriteLine("  Kartenfeld " + name);
            }
        }

        AssertTrue(karte != null,
            "**and the map file reads** -- and it said "
                + (karte == null
                    ? "refused, and that is why `$game_map.width` is nil"
                    : "read, so the nil is somewhere after the read"));

        // **Und die Events der Karte, weil das Spiel dort stehenbleibt.**
        // `Game_Map#setup` baut sie mit
        // `Game_Event.new(@map_id, @map.events[i])`, -- **und
        // `Game_Event#initialize` ruft `moveto(@event.x, @event.y)`** -- und im
        // Spiel ist `x` nil. **Die Frage ist, ob das Feld fehlt oder ob das
        // Objekt nicht als Datenobjekt markiert ist**, und das entscheidet sich
        // hier und nicht im Spiel.
        var events = karte!.Felder.TryGetValue("@events", out var e) ? e : null;
        System.Console.WriteLine(
            "Map001 @events: "
            + (events == null
                ? "no field"
                : "Kind " + events.Kind
                    + ", Members " + events.Members.Count
                    + ", Items " + events.Items.Count));
        if (events != null && events.Items.Count > 0)
        {
            // **Und ein Hash traegt seine Paare in `Items` und nicht in
            // `Members`** -- flach, Schluessel und Wert abwechselnd. **Und das
            // ist gemessen und nicht geraten:** der Ausdruck oben sagte
            // `Members 0, Items 2`.
            for (var i = 0; i + 1 < events.Items.Count; i += 2)
            {
                var wert = events.Items[i + 1];
                System.Console.WriteLine(
                    "  Schluessel " + events.Items[i].Integer
                    + " -> Kind " + wert.Kind
                    + ", Klasse " + (wert.ClassName ?? "-")
                    + ", AusDaten " + wert.AusDaten
                    + ", Felder " + string.Join(" ", wert.Felder.Keys)
                    + ", Members " + string.Join(" ",
                        wert.Members.Keys.Select(k => k.Name ?? "?")));
            }
        }

        AssertTrue(events != null,
            "**and the map carries its events** -- and the field is "
                + (events == null ? "not there" : "there"));

        // **Und eine Ebene tiefer, weil die Frage dort entschieden wird.**
        // Der Wert im Hash ist ein Symbol und kein Event-Objekt, -- **und die
        // Frage ist, ob der Marshal-Leser ihn so liest oder ob die
        // Ueberfuehrung ihn verliert.** Also wird dieselbe Datei mit dem
        // Marshal-Leser gelesen und der Wert angesehen.
        var roh = new MarshalReader(
            System.IO.File.ReadAllBytes(
                System.IO.Path.Combine(Wurzel, "Data", "Map001.rxdata")))
            .Read();
        System.Console.WriteLine(
            "Marshal: Kind " + roh.Kind
            + ", ClassName " + (roh.ClassName ?? "-")
            + ", Keys " + roh.Keys.Count
            + ", Items " + roh.Items.Count);
        var rohEvents = roh.Keys.ToList().IndexOf("@events") is var stelle
            && stelle >= 0
            ? roh.Items[stelle]
            : null;
        if (rohEvents != null)
        {
            System.Console.WriteLine(
                "Marshal @events: Kind " + rohEvents.Kind
                + ", ClassName " + (rohEvents.ClassName ?? "-")
                + ", Keys " + string.Join(" ", rohEvents.Keys)
                + ", Items " + rohEvents.Items.Count);
            foreach (var item in rohEvents.Items)
            {
                System.Console.WriteLine(
                    "    Item: Kind " + item.Kind
                    + ", ClassName " + (item.ClassName ?? "-")
                    + ", Keys " + item.Keys.Count);
            }
        }
    }

    /// <summary>
    /// And a script object does <em>not</em> answer a name that is only
    /// a field.
    /// </summary>
    /// <remarks>
    /// <strong>And this is the test that keeps the rule narrow.</strong>
    /// Ruby raises <c>NoMethodError</c> for a name that is not a method,
    /// -- **and a runtime that answered <c>@x</c> here would turn a typo
    /// in the game's own code into a value that looks like game
    /// state.**
    /// </remarks>
    public void Test_EinSkriptobjektAntwortetSeineFelderNicht()
    {
        var interpreter = new RubyInterpreter(new RubyNullHost());
        interpreter.RunProgram(new RubyParser(new RubyLexer(
            "class Held\n"
            + "  def initialize\n"
            + "    @x = 5\n"
            + "  end\n"
            + "end\n"
            + "$held = Held.new\n"
            + "$feld = $held.x\n")
            .Tokenize()).ParseProgram());

        AssertTrue(interpreter.Global("$feld").IsNil,
            "**and a script object's field is not a method** -- and it"
                + " answered "
                + (interpreter.Global("$feld").IsNil
                    ? "nil"
                    : interpreter.Global("$feld").Integer.ToString()));
    }
}
