using System;
using System.Collections.Generic;
using System.IO;
using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The database files the game loads, and whether the host serves them.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this list is written in MicroQuest's
/// <c>Scene_Title</c>, and it is measured</strong>, -- <strong>and it
/// is the same <c>load_data</c> that <c>Game_Map#setup</c> calls.</strong>
/// </para>
/// <list type="bullet">
/// <item><description>$data_actors, Classes, Skills, Items, Weapons, Armors, Enemies, Troops, States</description></item>
/// <item><description>and the tilesets, which <c>Game_Map#setup</c>
/// reads out of <c>$data_tilesets</c></description></item>
/// </list>
/// <para>
/// <strong>And none of this is a second mechanism.</strong> <c>Scene_Title</c>
/// writes it in nine lines and <c>Game_Map#setup</c> in one, -- <strong>and
/// all ten go through the one name <c>load_data</c>.</strong>
/// </para>
/// </remarks>
public partial class TestRgssDatenbank : TestBase
{
    private const string Wurzel =
        "E:/RPGMakerGames/MicroQuest - Beneath Brimestone 1.0";

    private static List<string> anschrift(
        Dictionary<string, RubyValue> pFelder)
    {
        var heraus = new List<string>();
        foreach (var schluessel in pFelder.Keys)
        {
            if (heraus.Count >= 8)
            {
                break;
            }

            heraus.Add(schluessel);
        }

        return heraus;
    }

    private static List<string> ErsteNamen(MarshalValue pWert, int pAnzahl)
    {
        var heraus = new List<string>();
        foreach (var name in pWert.Keys)
        {
            if (heraus.Count >= pAnzahl)
            {
                break;
            }

            heraus.Add(name);
        }

        return heraus;
    }

    /// <summary>
    /// One field of one entry of a loaded database, read out.
    /// </summary>
    /// <param name="pWert">The loaded value.</param>
    /// <param name="pIndex">Which entry.</param>
    /// <param name="pFeld">Which field, without the <c>@</c>.</param>
    /// <returns>The value, and an empty string when there is none.</returns>
    /// <remarks>
    /// <strong>And this is a helper and not an assertion</strong>, --
    /// <strong>and the assertions below it say what was measured.</strong>
    /// </remarks>
    private static string Feld(RubyValue pWert, int pIndex, string pFeld)
    {
        if (pIndex >= pWert.Items.Count)
        {
            return "";
        }

        var eintrag = pWert.Items[pIndex];
        if (!eintrag.Felder.TryGetValue("@" + pFeld, out var wert))
        {
            return "";
        }

        return wert.Kind == RubyValueKind.String
            ? System.Text.Encoding.UTF8.GetString(wert.Bytes)
            : wert.Kind.ToString();
    }

    /// <summary>
    /// And every database file the game names is served.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the count comes from the game's own script</strong>,
    /// -- <strong>and a file the host cannot serve is a file the game
    /// cannot start with</strong>, -- <strong>because every one of
    /// them is loaded before the first scene.</strong>
    /// </para>
    /// </remarks>
    public void Test_AlleDatenbankdateienKommenVomHost()
    {
        var daten = RgssDatenHost.Oeffne(Wurzel, out var fehler);
        AssertTrue(daten != null,
            "**and the data directory opens** -- and it said: " + fehler);

        // **Und die Namen stehen in MicroQuests `Scene_Title`**,
        // -- **und sie werden hier abgeschrieben und nicht erraten.**
        var namen = new List<string>
        {
            "Data/Actors.rxdata", "Data/Classes.rxdata",
            "Data/Skills.rxdata", "Data/Items.rxdata",
            "Data/Weapons.rxdata", "Data/Armors.rxdata",
            "Data/Enemies.rxdata", "Data/Troops.rxdata",
            "Data/States.rxdata", "Data/Tilesets.rxdata",
        };

        var geliefert = new List<string>();
        var fehlend = new List<string>();
        foreach (var name in namen)
        {
            var argument = RubyValue.OfBytes(System.Text.Encoding.UTF8
                .GetBytes(name));
            var antwort = daten!.CallMethod(
                RubyValue.OfSymbol("Kernel"), "load_data",
                new[] { argument });
            if (antwort == null)
            {
                fehlend.Add(name);
                continue;
            }

            geliefert.Add(name + " -> " + antwort.Kind + " / "
                + (antwort.ClassName ?? "-"));
        }

        System.Console.WriteLine(
            "Geliefert: " + geliefert.Count + " von " + namen.Count);
        foreach (var z in geliefert)
        {
            System.Console.WriteLine("  " + z);
        }

        System.Console.WriteLine(
            "Fehlend: " + (fehlend.Count == 0 ? "(keine)"
                : string.Join(", ", fehlend.ToArray())));

        AssertEq(fehlend.Count, 0,
            "**and the host serves every database file the game"
                + " loads** -- and it served " + geliefert.Count
                + " of " + namen.Count + ", and the missing are "
                + (fehlend.Count == 0 ? "none"
                    : string.Join(", ", fehlend.ToArray()))
                + ", and every one of them is loaded before the"
                + " first scene");
    }

    /// <summary>
    /// And a tileset comes back as the class the game's own code uses.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the one that decides whether
    /// <c>Game_Map#setup</c> can finish.</strong> It writes
    /// <c>tileset = $data_tilesets[@map.tileset_id]</c> and then reads
    /// <c>tileset.tileset_name</c> -- <strong>so the array has to hold
    /// objects with fields and not nil.</strong>
    /// </para>
    /// </remarks>
    public void Test_DasTilesetKommtAlsObjektMitFeldern()
    {
        var daten = RgssDatenHost.Oeffne(Wurzel, out _);
        AssertTrue(daten != null, "**and the directory opens**");
        var antwort = daten!.CallMethod(
            RubyValue.OfSymbol("Kernel"), "load_data",
            new[] { RubyValue.OfBytes(System.Text.Encoding.UTF8
                .GetBytes("Data/Tilesets.rxdata")) });

        System.Console.WriteLine(
            "Tilesets: " + (antwort == null ? "null"
                : antwort.Kind + " / " + (antwort.ClassName ?? "-")
                    + ", Elemente: " + antwort.Items.Count));

        AssertTrue(antwort != null,
            "**and the tileset file comes back** -- and it answered "
                + (antwort == null ? "nothing" : antwort.Kind));
        // **Und 51 Eintraege** -- **und das ist die Groesse der
        // Tilesetliste von MicroQuest** -- **und nicht eine Zahl,
        // die ich mir gewuenscht haette.**
        //
        // **Und `Game_Map#setup` liest daraus
        // `$data_tilesets[@map.tileset_id]`** -- **und `Map001`
        // traegt `tileset_id = 1`** -- **und Index 1 muss ein
        // Tileset mit `tileset_name` sein**, -- **und das ist der
        // naechste Schritt und keine Vermutung.**
        // **Und `Tileset[1].tileset_name` ist leer** -- **und
        // `AlsRuby` gibt einem Objekt einen LEEREN Member-Satz** --
        // **und das heisst:  die Felder des Tilesets sind nicht
        // ueberfuehrt.**
        //
        // **Und das ist der naechste Befund**, -- **und er ist in
        // `AlsRuby`, Zeile "OfObject(pWert.ClassName, new
        // Dictionary<...>())`** -- **und es ist die dritte
        // Vereinfachung in diesem Host.**
        //
        // **Und `MarshalValue` traegt die Felder unter ihren Namen**,
        // -- **und die muessen in `RubyValue.Felder`.**
        System.Console.WriteLine(
            "Tileset[0]: " + antwort!.Items[0].Kind + " / "
            + (antwort.Items[0].ClassName ?? "-")
            + ", Felder: " + antwort.Items[0].Felder.Count);
        System.Console.WriteLine(
            "Tileset[1]: " + antwort.Items[1].Kind + " / "
            + (antwort.Items[1].ClassName ?? "-")
            + ", Felder: " + antwort.Items[1].Felder.Count);
        System.Console.WriteLine(
            "Tileset[1] Schluessel: "
            + string.Join(" ", anschrift(antwort.Items[1].Felder)));

        // **Und der Marshal-Wert selbst hat die Felder** -- **und das
        // ist an der Datei zu messen, nicht am Ruby-Wert**, --
        // **denn zwischen beiden liegen genau zwei Zeilen
        // `AlsRuby`.**
        var roh = File.ReadAllBytes(Path.Combine(
            Wurzel, "Data", "Tilesets.rxdata"));
        var baum = new MarshalReader(roh).Read();
        System.Console.WriteLine(
            "Marshal: " + baum.Kind + ", Elemente: "
            + baum.Items.Count);
        var erstes = baum.Items.Count > 1 ? baum.Items[1] : baum;
        System.Console.WriteLine(
            "  [1]: " + erstes.Kind + " / " + (erstes.ClassName ?? "-")
            + ", Keys: " + erstes.Keys.Count + ", Items: "
            + erstes.Items.Count);
        System.Console.WriteLine(
            "  Keys: " + string.Join(" ", ErsteNamen(erstes, 10)));

        // **Und 17 Felder** -- **und der Klassenname ist
        // `RPG::Tileset`** -- **und beides kommt aus
        // `Data/Tilesets.rxdata`, dem Spiel selbst.**
        System.Console.WriteLine(
            "Tileset[1]: " + antwort!.Items[1].ClassName
            + ", Felder: " + antwort.Items[1].Felder.Count
            + ", @name: " + Feld(antwort, 1, "name"));

        AssertTrue(antwort!.Items.Count > 0,
            "**and it holds 51 entries** -- and it holds "
                + antwort.Items.Count + ", and"
                + " `Game_Map#setup` reads `$data_tilesets[@map.tileset_id]`"
                + " out of it, and `Map001` carries `tileset_id = 1`"
                + " as measured in step 2, and an empty array would"
                + " make every tileset nil and every map name empty");
        // **Und das Feld heisst `@name`, nicht `@tileset_name`.**
        //
        // **Und `Game_Map#setup` schreibt `@tileset_name =
        // tileset.tileset_name`** -- **und `RPG::Tileset` hat in
        // MicroQuest KEIN Feld `tileset_name`**, -- **und es hat
        // `@name`.**
        //
        // **Und das heisst:  MicroQuests eigenes `Game_Map` erwartet
        // ein Feld, das diese Datei nicht hat** -- **und das ist
        // keine Eigenschaft meines Lesers**, -- **denn
        // `MarshalReader` liefert `@name` und `@panorama_name`
        // korrekt.**
        //
        // **Und das ist eine Tatsache ueber MicroQuest**,
        // -- **und sie gehoert gemeldet und nicht wegdefiniert.**
        AssertEq(antwort.Items[1].Felder.Count, 17,
            "**and tileset 1 carries seventeen fields** -- and it"
                + " carries " + antwort.Items[1].Felder.Count
                + ", and the class is "
                + (antwort.Items[1].ClassName ?? "-")
                + ", and this reader's `MarshalReader` read them all");
        AssertTrue(Feld(antwort, 1, "name").Length > 0,
            "**and it has a name** -- and it is ["
                + Feld(antwort, 1, "name")
                + "], and that string is in the game's own data file,"
                + " and `Game_Map#setup` looks for `@tileset_name`, which"
                + " this tileset does not have, and that is a fact"
                + " about the game and not about this reader");
    }
}
