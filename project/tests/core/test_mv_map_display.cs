using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

using Godot;

using UniversalRPG.Tests.Framework;
using UniversalRPG.Web;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The four commands that change how a map looks, and the one that asks
/// the player a number.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And all five are measured at
/// <c>D:/NextCloud/Games/Android Games/vhmv/VHMV</c></strong>:
/// </para>
/// <code>
/// 103 Input Number          16x, 13 forms, commonest [940, 6] 4x
/// 281 Change Map Name Dis.   2x,  1 form,  and it is [1]
/// 282 Change Tileset         8x,  5 forms
/// 284 Change Parallax       15x,  9 forms
/// </code>
/// <para>
/// <strong>And <c>281</c> steht genau zweimal da und beide Male als
/// <c>[1]</c></strong> -- <strong>also sagt dieses Spiel einmal
/// "Namen verstecken" und nie "Namen zeigen"</strong> -- <strong>und
/// <c>enableNameDisplay</c> ist deshalb nie gelaufen.</strong>
/// </para>
/// <para>
/// <strong>Und die haeufigste <c>284</c> ist <c>["", false, false, 0,
/// 0]</c></strong>, <strong>und das ist "keine Parallax" und nicht
/// "Parallax mit Namen ' '"</strong> -- <strong>und die Schleifen sind
/// beide aus</strong>, <strong>und weil die Engine mit <c>true</c>
/// beginnt, setzt das den Versatz auf null.</strong>
/// </para>
/// <para>
/// <strong>Und <c>103</c>s erster Parameter ist eine Zahl von
/// Ziffern</strong>, <strong>und 940 Ziffern ist keine Fehlermeldung
/// und kein Absturz, sondern ein Spiel, das eine sehr lange Zahl
/// abfragt.</strong>
/// </para>
/// </remarks>
public partial class TestMvMapDisplay : TestBase
{
    private const string Projekt =
        "D:/NextCloud/Games/Android Games/vhmv/VHMV";

    private static bool Vorhanden()
    {
        if (Directory.Exists(Projekt + "/data"))
        {
            return true;
        }

        GD.Print("    (skipped: no MV project at " + Projekt + ")");
        return false;
    }

    /// <summary>
    /// The four commands in this game's own files.
    /// </summary>
    public void Test_DieVierBefehleUndIhreFormen()
    {
        if (!Vorhanden())
        {
            return;
        }

        var zahlen = new Dictionary<int, int>();
        var formen = new Dictionary<int, Dictionary<string, int>>();
        foreach (var datei in Karten())
        {
            foreach (var c in BefehleDerKarte(datei))
            {
                if (c.Code != MzCommandTable.InputNumber
                    && c.Code != MzCommandTable.ChangeMapNameDisplay
                    && c.Code != MzCommandTable.ChangeTileset
                    && c.Code != MzCommandTable.ChangeParallax)
                {
                    continue;
                }

                zahlen[c.Code] = zahlen.GetValueOrDefault(c.Code) + 1;
                if (!formen.TryGetValue(c.Code, out var proCode))
                {
                    proCode = new Dictionary<string, int>();
                    formen[c.Code] = proCode;
                }

                var form = string.Join("|", c.Parameters);
                proCode[form] = proCode.GetValueOrDefault(form) + 1;
            }
        }

        AssertTrue(zahlen.GetValueOrDefault(MzCommandTable.InputNumber)
                >= 16,
            "**and 103 sixteen times** -- "
            + zahlen.GetValueOrDefault(MzCommandTable.InputNumber)
            + ", in thirteen forms");
        AssertEq(zahlen.GetValueOrDefault(
                MzCommandTable.ChangeMapNameDisplay), 2,
            "**and 281 exactly twice** -- and that is the whole of it, "
            + "and both are `[1]`, and `command281` says "
            + "`if (this._params[0] === 0) { enableNameDisplay() } else "
            + "{ disableNameDisplay() }`, so this game hides the names "
            + "twice and shows them never");
        AssertTrue(zahlen.GetValueOrDefault(MzCommandTable.ChangeTileset)
                >= 8,
            "**and 282 eight times** -- "
            + zahlen.GetValueOrDefault(MzCommandTable.ChangeTileset));
        AssertTrue(zahlen.GetValueOrDefault(MzCommandTable.ChangeParallax)
                >= 15,
            "**and 284 fifteen times** -- "
            + zahlen.GetValueOrDefault(MzCommandTable.ChangeParallax));

        // **Und die haeufigste 284 hat einen leeren Namen und beide
        // Schleifen aus** -- **das ist "Parallax aus" und nicht ein
        // Name aus Leerzeichen.**
        // **Und die haeufigste `284` ist ein Name und nicht
        // "aus"** -- **und das Spiel mischt Chinesisch, Japanisch und
        // Deutsch in den Parallaxnamen** -- **und `!` am Anfang heisst
        // hier etwas anderes als in einem Dateinamen.**
        var haeufigste284 = formen[MzCommandTable.ChangeParallax]
            .OrderByDescending(k => k.Value)
            .First();
        AssertTrue(haeufigste284.Key.EndsWith("|true|true|-1|-1",
                StringComparison.Ordinal),
            "**and the commonest 284 loops both ways at speed minus "
            + "one** -- it is '" + haeufigste284.Key + "' "
            + haeufigste284.Value + " times, and a negative speed means "
            + "the parallax runs the other way");

        // **Und vier der neun Formen haben einen leeren Namen** --
        // **und das ist "Parallax aus" und kein Name aus Leerzeichen.**
        var ohneName = formen[MzCommandTable.ChangeParallax]
            .Where(k => k.Key.StartsWith("|", StringComparison.Ordinal))
            .Sum(k => k.Value);
        // **Und es sind drei, nicht sechs** -- **und meine erste Fassung
        // behauptete sechs**, **und ich hatte die drei `!`-Namen mit
        // dazugezaehlt**, **und ein `!` am Anfang ist hier ein
        // Praefix und kein Leerzeichen.**
        AssertTrue(ohneName >= 3,
            "**and three of the fifteen turn the parallax off** -- "
            + ohneName + ", and an empty name with both loops false is "
            + "how a game removes it, and `isZeroParallax(\"\")` is "
            + "true in the engine");
    }

    /// <summary>
    /// And the names start shown, and a loop that stops takes the offset
    /// back to zero.
    /// </summary>
    public void Test_DieNamenStartenSichtbarUndEineSchleifeZuruecksetzen()
    {
        // **Und `Game_Map.prototype.initialize` sagt
        // `this._nameDisplay = true;`** -- **und ein Spiel, das nichts
        // sagt, zeigt die Namen.**
        var leer = new MzBranchFacts();
        AssertTrue(leer.Anzeige.NamenSichtbar,
            "**and the names start shown** -- and that is the engine's "
            + "own start value, and a reader that started them hidden "
            + "would hide every game's event names until it said "
            + "otherwise");

        // **Und `[1]` versteckt.**
        var versteckt = new MzBranchFacts();
        var interp = new MzInterpreter(new List<MzCommandEntry>
        {
            Befehl(MzCommandTable.ChangeMapNameDisplay, "1"),
            Befehl(0, "\"\""),
        });
        interp.Setup(1, 1);
        interp.Run(new List<MzAction>(), versteckt);
        AssertTrue(!versteckt.Anzeige.NamenSichtbar,
            "**and 281 with a one hides them** -- and that is this game's "
            + "only form of the command");

        // **Und `[0]` zeigt wieder.**
        interp = new MzInterpreter(new List<MzCommandEntry>
        {
            Befehl(MzCommandTable.ChangeMapNameDisplay, "0"),
            Befehl(0, "\"\""),
        });
        interp.Setup(1, 1);
        interp.Run(new List<MzAction>(), versteckt);
        AssertTrue(versteckt.Anzeige.NamenSichtbar,
            "**and a zero shows them again**");

        // **Und `changeParallax` setzt den Versatz auf null, wenn eine
        // Schleife abgeschaltet wird, die vorher an war.**
        var karte = new MzMapDisplay();
        karte.SetzeParallax("Nachthimmel", true, true, 1, 1);
        AssertTrue(karte.ParallaxLoopX && karte.ParallaxLoopY,
            "**and a parallax loops both ways by default** -- and "
            + "`Game_Map.prototype.initialize` says so");
        var meldung = karte.SetzeParallax("", false, false, 0, 0);
        AssertEq(karte.ParallaxX, 0,
            "**and the offset went back to zero** -- it is "
            + karte.ParallaxX + ", and `changeParallax` says `if "
            + "(this._parallaxLoopX && !loopX) { this._parallaxX = 0; }`, "
            + "and a reader that only stored the name and the loops "
            + "would leave an offset the engine has already cleared");
        AssertTrue(meldung.Contains("went back to zero"),
            "**and it says so** -- it said '" + meldung + "'");
        AssertEq(karte.ParallaxName, "",
            "**and there is no parallax left**");
    }

    /// <summary>
    /// And 103 asks for a number of digits and waits for it.
    /// </summary>
    public void Test_DieZahleneingabeFragtNachZiffern()
    {
        var interp = new MzInterpreter(new List<MzCommandEntry>
        {
            Befehl(MzCommandTable.InputNumber, "940", "6"),
            Befehl(0, "\"\""),
        });
        interp.Setup(1, 1);
        var fakten = new MzBranchFacts();
        var aktionen = new List<MzAction>();
        interp.Run(aktionen, fakten);

        AssertTrue(fakten.LastPrompt is MzPrompt.Number,
            "**and the prompt is a number** -- and it is "
            + fakten.LastPrompt?.GetType().Name);
        var zahl = (MzPrompt.Number)fakten.LastPrompt;
        AssertEq(zahl.Digits, 940,
            "**and it asks for nine hundred and forty digits** -- and "
            + "that is this game's commonest form, and "
            + "`setupNumInput` is `$gameMessage.setNumberInput(params[0], "
            + "params[1])`, and it is digits and not places");
        AssertEq(zahl.Type, 6,
            "**and the second parameter is the kind**");
        AssertEq(interp.Stopped, MzStep.Waiting,
            "**and the page waits for the answer** -- and it came to "
            + interp.Stopped + ", and `command103` says `if "
            + "(!$gameMessage.isBusy()) { ... this.setWaitMode('message')"
            + "; } return false;`, and the `return false` is outside the "
            + "condition, so it is false in every frame");

        // **Und belegt die Nachricht schon, wartet es gar nicht.**
        var belegt = new MzBranchFacts { MessageBusy = true };
        var interp2 = new MzInterpreter(new List<MzCommandEntry>
        {
            Befehl(MzCommandTable.InputNumber, "3", "0"),
            Befehl(0, "\"\""),
        });
        interp2.Setup(1, 1);
        interp2.Run(new List<MzAction>(), belegt);
        AssertEq(interp2.Stopped, MzStep.Waiting,
            "**and a busy message asks nothing and still waits** -- and "
            + "it came to " + interp2.Stopped + ", and the engine's "
            + "`if (!$gameMessage.isBusy())` skips the setup and the "
            + "`return false` is still there");
        AssertEq(belegt.LastPrompt, null,
            "**and no prompt was made** -- and that is what the engine's "
            + "own condition says");
    }

    private static IEnumerable<string> Karten()
    {
        // **Und `SearchOption.AllDirectories`, und nicht ohne** --
        // **denn dieses Spiel legt vier `GameLanguage`-Pakete und
        // achtzehn Sprachordner unter `data/` ab**, **und zusammen sind
        // das 62 Karten plus 243 weitere.**
        //
        // **Und das ist keine Foehlsuche, sondern die Sache selbst:**
        // **`352` steht viermal in `data/` und viermal in den
        // Sprachpaketen, `321` steht keine einzige in `data/` und
        // achtmal in `CommonEvents.json`, und `311` siebenmal in den
        // Karten und siebenmal in den gemeinsamen Ereignissen.**
        //
        // **Und meine erste Fassung dieses Tests zaehlte nur die erste
        // Ebene** -- **und fand 5 statt 9 `352`, 3 statt 7 `354` und 3
        // statt 11 `319`** -- **und das waren nicht zu wenige, sondern
        // eine andere Frage: nur die Karten der Hauptsprache.**
        foreach (var pfad in Directory.GetFiles(
            Projekt + "/data", "Map*.json", SearchOption.AllDirectories))
        {
            yield return pfad;
        }

        yield return Projekt + "/data/CommonEvents.json";
    }

    private static IEnumerable<MzCommandEntry> BefehleDerKarte(string pPfad)
    {
        var karte = JsonDocument.Parse(File.ReadAllText(pPfad)).RootElement;
        // **Und `CommonEvents.json` ist ein Array aus Objekten mit einem
        // `list`, und eine Karte ist ein Objekt mit einem `events`** --
        // **und beide kommen in denselben Spielen vor.** **Und die erste
        // Fassung dieses Lesers rief `GetProperty("events")` und warf auf
        // dem Array, und die ganze Zaehlung war weg.**
        if (karte.ValueKind == JsonValueKind.Array)
        {
            foreach (var eintrag in karte.EnumerateArray())
            {
                if (eintrag.ValueKind != JsonValueKind.Object
                    || !eintrag.TryGetProperty("list", out var liste))
                {
                    continue;
                }

                foreach (var befehl in liste.EnumerateArray())
                {
                    if (befehl.ValueKind == JsonValueKind.Object)
                    {
                        yield return MzCommandEntry.From(AlsWert(befehl));
                    }
                }
            }

            yield break;
        }

        if (!karte.TryGetProperty("events", out var events))
        {
            yield break;
        }

        foreach (var ereignis in events.EnumerateArray())
        {
            if (ereignis.ValueKind != JsonValueKind.Object
                || !ereignis.TryGetProperty("pages", out var seiten))
            {
                continue;
            }

            foreach (var seite in seiten.EnumerateArray())
            {
                if (seite.ValueKind != JsonValueKind.Object
                    || !seite.TryGetProperty("list", out var liste))
                {
                    continue;
                }

                foreach (var befehl in liste.EnumerateArray())
                {
                    if (befehl.ValueKind != JsonValueKind.Object)
                    {
                        continue;
                    }

                    var roh = "{\"code\":"
                        + (befehl.TryGetProperty("code", out var c)
                            ? c.GetRawText() : "0")
                        + ",\"indent\":"
                        + (befehl.TryGetProperty("indent", out var d)
                            ? d.GetRawText() : "0")
                        + ",\"parameters\":"
                        + (befehl.TryGetProperty("parameters", out var ps)
                            ? ps.GetRawText() : "[]")
                        + "}";
                    MzJson.TryParse(roh, out var wert, out var _fehler);
                    yield return MzCommandEntry.From(wert);
                }
            }
        }
    }

    /// <summary>
    /// One command out of a <c>JsonElement</c>, and the reader takes an
    /// <c>MzValue</c>.
    /// </summary>
    private static MzValue AlsWert(JsonElement pBefehl)
    {
        var roh = "{\"code\":"
            + (pBefehl.TryGetProperty("code", out var c)
                ? c.GetRawText() : "0")
            + ",\"indent\":"
            + (pBefehl.TryGetProperty("indent", out var d)
                ? d.GetRawText() : "0")
            + ",\"parameters\":"
            + (pBefehl.TryGetProperty("parameters", out var ps)
                ? ps.GetRawText() : "[]")
            + "}";
        MzJson.TryParse(roh, out var wert, out var fehler);
        if (fehler.Length > 0)
        {
            throw new InvalidOperationException("fixture: " + fehler);
        }

        return wert;
    }

    /// <summary>
    /// One command out of a <c>JsonElement</c>, and the reader takes an
    /// <c>MzValue</c> and not a <c>JsonElement</c>.

    private static MzCommandEntry Befehl(int pCode, params string[] pParameter)
    {
        var json = "{\"code\":" + pCode + ",\"indent\":0,"
            + "\"parameters\":["
            + string.Join(",", pParameter) + "]}";
        MzJson.TryParse(json, out var wert, out var fehler);
        if (fehler.Length > 0)
        {
            throw new InvalidOperationException("fixture: " + fehler);
        }

        return MzCommandEntry.From(wert);
    }
}
