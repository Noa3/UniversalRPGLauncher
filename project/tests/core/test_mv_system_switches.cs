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
/// The five commands that write <c>$gameSystem</c> and <c>$gameTimer</c>,
/// and what this repository's reader does with them.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the game they are measured on is
/// <c>E:/RPGMakerGames/Fatal Fantasy Update/Fatal Fantasy/www</c></strong>,
/// <strong>which is this machine's largest MV project: 122278 commands
/// across its maps.</strong> <strong>And the MV project next to it in
/// <c>D:/Itch/sister/www</c> uses none of the five.</strong>
/// </para>
/// <para>
/// <strong>And the counts, all measured:</strong>
/// </para>
/// <code>
/// 134 Change Save Access    255x   133 [1], 122 [0]
/// 135 Change Menu Access    111x    66 [0],  45 [1]
/// 138 Change Window Color   135x    69 [[-255, -255, -35, 0]],
///                                  65 [[-255, -255, -255, 0]]
/// 132 Change Battle BGM     148x    62 [{"name": "(Regular Battle)", ...}],
///                                  39 [(Boss Battle)], 12 [(Bad Situation)]
/// 124 Control Timer          35x    17 [1], 18 [0, N]
/// </code>
/// </remarks>
public partial class TestMvSystemSwitches : TestBase
{
    private const string Projekt =
        "E:/RPGMakerGames/Fatal Fantasy Update/Fatal Fantasy/www";

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
    /// Every one of the five this game writes, and how often.
    /// </summary>
    public void Test_DieFuenfBefehleDiesesSpielsUndIhreFormen()
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
                if (c.Code != MzCommandTable.ChangeSaveAccess
                    && c.Code != MzCommandTable.ChangeMenuAccess
                    && c.Code != MzCommandTable.ChangeWindowColor
                    && c.Code != MzCommandTable.ChangeBattleBgm
                    && c.Code != MzCommandTable.ControlTimer)
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

        AssertTrue(zahlen.GetValueOrDefault(MzCommandTable.ChangeSaveAccess)
                >= 255,
            "**and it writes 134 two hundred and fifty-five times** -- "
            + zahlen.GetValueOrDefault(MzCommandTable.ChangeSaveAccess));
        AssertTrue(zahlen.GetValueOrDefault(MzCommandTable.ChangeMenuAccess)
                >= 111,
            "**and 135 a hundred and eleven times** -- "
            + zahlen.GetValueOrDefault(MzCommandTable.ChangeMenuAccess));
        AssertTrue(zahlen.GetValueOrDefault(MzCommandTable.ChangeWindowColor)
                >= 135,
            "**and 138 a hundred and thirty-five times** -- "
            + zahlen.GetValueOrDefault(
                MzCommandTable.ChangeWindowColor));
        AssertTrue(zahlen.GetValueOrDefault(MzCommandTable.ChangeBattleBgm)
                >= 148,
            "**and 132 a hundred and forty-eight times** -- "
            + zahlen.GetValueOrDefault(MzCommandTable.ChangeBattleBgm));
        AssertTrue(zahlen.GetValueOrDefault(MzCommandTable.ControlTimer) >= 35,
            "**and 124 thirty-five times** -- "
            + zahlen.GetValueOrDefault(MzCommandTable.ControlTimer));

        // **Und beide Schalter kommen in beiden Richtungen vor** -- **und
        // ein Befehl, den ein Spiel nur in einer Richtung schreibt, ist
        // ein Befehl, den man nur halb gebaut hat.**
        AssertTrue(
            formen[MzCommandTable.ChangeSaveAccess]
                .GetValueOrDefault("1") >= 100,
            "**and 134 allows saving a hundred and thirty-three times** -- "
            + formen[MzCommandTable.ChangeSaveAccess].GetValueOrDefault("1")
            + ", and it forbids it " + formen[
                MzCommandTable.ChangeSaveAccess].GetValueOrDefault("0")
            + " times, and both are in the game");
        AssertTrue(
            formen[MzCommandTable.ChangeMenuAccess]
                .GetValueOrDefault("0") >= 60,
            "**and 135 shuts the menu sixty-six times** -- "
            + formen[MzCommandTable.ChangeMenuAccess].GetValueOrDefault("0")
            + ", and opens it " + formen[
                MzCommandTable.ChangeMenuAccess].GetValueOrDefault("1")
            + " times");

        // **Und `138`s erster Parameter ist eine Liste aus vier Zahlen und
        // keine Zahl** -- **denn `setWindowTone(value)` ist
        // `this._windowTone = value`, und `value` ist das ganze Array.**
        AssertTrue(
            formen[MzCommandTable.ChangeWindowColor].Keys
                .All(k => k.StartsWith("[-", StringComparison.Ordinal)),
            "**and every 138 starts with a list** -- and they are "
            + string.Join(", ", formen[
                MzCommandTable.ChangeWindowColor].Keys.Take(3))
            + ", and a reader that read it as a number tinted nothing");
    }

    /// <summary>
    /// And the two switches are two switches, and the timer counts
    /// frames.
    /// </summary>
    public void Test_DieSchalterUndDieUhr()
    {
        // **Und beide starten erlaubt** -- **denn
        // `Game_System.prototype.initialize` ist
        // `this._saveEnabled = true; this._menuEnabled = true;`**, **und
        // ein Spiel, das nichts sagt, darf speichern.**
        var spiel = new MzSystem();
        AssertTrue(spiel.SaveEnabled && spiel.MenuEnabled,
            "**and both start allowed** -- and the engine's own "
            + "initialiser says so, and a reader that started them shut "
            + "would make every game unsaveable until it said otherwise");

        // **Und `134 [0]` sperrt, und `[1]` erlaubt.**
        AssertEq(spiel.SetzeSpeichern(0), "saving is not allowed",
            "**and 134 with a zero forbids saving**");
        AssertTrue(!spiel.SaveEnabled,
            "**and the switch followed**");
        spiel.SetzeSpeichern(1);
        AssertTrue(spiel.SaveEnabled,
            "**and a one allows it again** -- and `disableSave` and "
            + "`enableSave` are one assignment each and nothing else");

        // **Und `135` ist derselbe Befehl an anderer Stelle, und
        // unabhaengig von `134`.**
        spiel.SetzeSpeichern(0);
        spiel.SetzeMenue(0);
        AssertTrue(!spiel.SaveEnabled && !spiel.MenuEnabled,
            "**and 135 shuts the menu on its own**");
        spiel.SetzeSpeichern(1);
        AssertTrue(spiel.SaveEnabled && !spiel.MenuEnabled,
            "**and reopening saving does not open the menu** -- and they "
            + "are two fields and not one");

        // **Und `124` multipliziert mit sechzig**, **denn
        // `$gameTimer.start(this._params[1] * 60)`**, **und `Game_Timer`
        // zaehlt Bilder und `seconds()` ist
        // `Math.floor(this._frames / 60)`.**
        spiel.SetzeUhr(0, 45);
        AssertEq(spiel.TimerFrames, 2700,
            "**and 124 with 45 seconds is 2700 frames** -- and it is "
            + spiel.TimerFrames + ", and a reader that stored 45 would "
            + "run out in three quarters of a second");
        AssertEq(spiel.UhrSekunden(), 45,
            "**and the engine's own `seconds()` says 45 again**");
        AssertTrue(spiel.TimerWorking,
            "**and it is running** -- and `start` is `this._frames = "
            + "count; this._working = true;`");

        // **Und es zaehlt nur, wenn die Szene die aktive ist** -- **denn
        // `update(sceneActive)` beginnt mit `if (sceneActive &&
        // this._working && this._frames > 0)`.**
        spiel.UhrEinBild(false);
        AssertEq(spiel.TimerFrames, 2700,
            "**and it does not count while the scene is not the active "
            + "one** -- and it is still " + spiel.TimerFrames);
        spiel.UhrEinBild(true);
        AssertEq(spiel.TimerFrames, 2699,
            "**and it counts one frame when it is**");

        // **Und `124 [1]` stoppt, und der Zaehler bleibt stehen.**
        spiel.SetzeUhr(1, 0);
        AssertTrue(!spiel.TimerWorking,
            "**and a one stops it**");
        AssertEq(spiel.TimerFrames, 2699,
            "**and the frames stay where they were** -- and `stop` is "
            + "`this._working = false`, and it does not clear the count");
    }

    /// <summary>
    /// And 132 stores the battle music and does not play it.
    /// </summary>
    public void Test_DerBefehl132SpeichertUndSpieltNicht()
    {
        // **Und der Befehl kommt aus der Datei dieses Spiels** --
        // **und nicht aus einer Hand, die ich gebaut habe** --
        // **denn meine erste Fassung dieses Tests schrieb ihn mit
        // `Befehl(code, params...)`**, **und dieser Helfer baut
        // `{"code":132,"indent":0,"parameters":["{...}"]}`**, **und
        // das erste Parameter landet dann als JSON-Text mit
        // Anfuehrungszeichen darin in der Liste** -- **und `Text()`
        // gibt genau das zurueck, und das ist nicht das Objekt,
        // sondern sein geschriebenes JSON in Anfuehrungszeichen.**
        var echter = Erstes132AusDiesemSpiel();
        AssertTrue(echter.Length > 0,
            "**and this game writes 132 a hundred and forty-eight times**"
            + " -- and the first one is '" + echter + "', and a fixture "
            + "built by hand would be a different command");
        if (echter.Length == 0)
        {
            return;
        }

        var interp = new MzInterpreter(new List<MzCommandEntry>
        {
            MzCommandEntry.From(AlsWert(
                JsonDocument.Parse(echter).RootElement)),
            Befehl(0, "\"\""),
        });
        interp.Setup(1, 1);
        var fakten = new MzBranchFacts();
        interp.Run(new List<MzAction>(), fakten);

        // **Und die Kette in einer Zeile**, **denn drei Stellen
        // koennten hier leer sein** -- **der Dispatcher, das Tor, oder
        // `Text()` an einem Objekt** -- **und die Meldung sagt, welche.**
        AssertTrue(fakten.Spiel.Kampflied.Length > 0,
            "**and the object is kept** -- the command was '"
            + echter + "', HasEffect says "
            + MzCommands.HasEffect(MzCommandTable.ChangeBattleBgm)
            + ", and the field is '" + fakten.Spiel.Kampflied
            + "', and `setBattleBgm` is "
            + "`this._battleBgm = value`, and `value` is the object, and "
            + "a reader that kept only the name would lose the volume "
            + "and the pitch");
        // **Und der Name ist der, den die Datei schreibt** -- **und es
        // ist `(Boss Battle)` in `Map005` und nicht `(Regular Battle)`**,
        // **und mein Test hatte einen hingereschrieben**, **und die
        // Sonde hat es gemeldet: `{"code":132,"indent":0,
        // "parameters":[{"name":"(Boss Battle)","volume":100,
        // "pitch":100,"pan":0}]}`**.
        AssertTrue(
            fakten.Spiel.KampfliedName.StartsWith("(",
                StringComparison.Ordinal)
            && fakten.Spiel.KampfliedName.EndsWith(")",
                StringComparison.Ordinal),
            "**and the name inside it is the placeholder the "
            + "battle substitutes** -- and it is '"
            + fakten.Spiel.KampfliedName + "', and "
            + "`AudioManager.playBgm` reads `.name` out of the "
            + "object, and this game writes three such "
            + "placeholders 113 times between them");
        AssertTrue(fakten.Spiel.HatKampflied,
            "**and it is set** -- and `setBattleBgm` is "
            + "`this._battleBgm = value`, and `241` is the one that "
            + "says `AudioManager.playBgm`, and a reader that played on "
            + "132 would start the music sixty-two times too early");
        AssertEq(fakten.Screen.Bgm.Name, "",
            "**and nothing is playing** -- and the screen's own channel "
            + "is still empty, because 132 does not touch it");
    }

    /// <summary>
    /// One real 132 out of this game's own files, as the file writes it.
    /// </summary>
    /// <returns>
    /// The command's own JSON, and empty when the game has none.
    /// </returns>
    private static string Erstes132AusDiesemSpiel()
    {
        foreach (var datei in Karten())
        {
            var karte = JsonDocument.Parse(
                File.ReadAllText(datei)).RootElement;
            foreach (var zeile in RohBefehle(karte))
            {
                if (zeile.GetProperty("code").GetInt32()
                    != MzCommandTable.ChangeBattleBgm)
                {
                    continue;
                }

                return zeile.GetRawText();
            }
        }

        return "";
    }

    /// <summary>
    /// Every command in a map or in a common-events file, and the file
    /// decides which of the two shapes it is.
    /// </summary>
    private static IEnumerable<JsonElement> RohBefehle(
        JsonElement pWurzel)
    {
        if (pWurzel.ValueKind == JsonValueKind.Array)
        {
            foreach (var eintrag in pWurzel.EnumerateArray())
            {
                if (eintrag.ValueKind == JsonValueKind.Object
                    && eintrag.TryGetProperty("list", out var liste))
                {
                    foreach (var b in liste.EnumerateArray())
                    {
                        yield return b;
                    }
                }
            }

            yield break;
        }

        if (!pWurzel.TryGetProperty("events", out var events))
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
                if (seite.ValueKind == JsonValueKind.Object
                    && seite.TryGetProperty("list", out var liste))
                {
                    foreach (var b in liste.EnumerateArray())
                    {
                        yield return b;
                    }
                }
            }
        }
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
