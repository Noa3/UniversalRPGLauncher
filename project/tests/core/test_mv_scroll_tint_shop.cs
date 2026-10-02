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
/// Scrolling the map, tinting one picture, and the shop -- three commands
/// that wait, wait and open a screen.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And all three are measured at
/// <c>D:/NextCloud/Games/Android Games/vhmv/VHMV</c></strong>:
/// </para>
/// <code>
/// 204 Scroll Map   227x,  88 forms, commonest [6, 4, 4] 16x
/// 234 Tint Picture  102x,  27 forms, commonest [30, [0,0,0,0], 6, false] 17x
/// 302 Shop          39x, and not one 605 line anywhere
/// </code>
/// <para>
/// <strong>And the 204 forms are direction, distance and speed</strong> --
/// <strong>and the directions that appear are 2, 4, 6 and 8, which is
/// <c>down, left, right, up</c></strong> -- <strong>and 6 is the most
/// common, which is right.</strong>
/// </para>
/// <para>
/// <strong>And <c>scrollDistance</c> is <c>Math.pow(2, speed) / 256</c></strong>
/// -- <strong>a dyadic fraction, so speed four is a sixteenth of a tile a
/// frame</strong> -- <strong>and a reader that rounded it to a tile
/// would move a hundred and sixty times too fast.</strong>
/// </para>
/// <para>
/// <strong>And <c>234</c>s vierter Parameter ist ein Wahrheitswert, keine
/// Zahl</strong>, <strong>und dieses Spiel wartet bei 16 seiner 102.</strong>
/// </para>
/// </remarks>
public partial class TestMvScrollTintShop : TestBase
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
    /// The three commands in this game's own files, and their forms.
    /// </summary>
    public void Test_DieDreiBefehleUndIhreFormen()
    {
        if (!Vorhanden())
        {
            return;
        }

        var zahlen = new Dictionary<int, int>();
        var formen = new Dictionary<int, Dictionary<string, int>>();
        var goods = 0;
        foreach (var datei in Karten())
        {
            foreach (var c in BefehleDerKarte(datei))
            {
                if (c.Code == MzCommandTable.GoodsLine)
                {
                    goods++;
                    continue;
                }

                if (c.Code != MzCommandTable.ScrollMap
                    && c.Code != MzCommandTable.TintPicture
                    && c.Code != MzCommandTable.ShopProcessing)
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

        AssertTrue(zahlen.GetValueOrDefault(MzCommandTable.ScrollMap) >= 227,
            "**and 204 two hundred and twenty-seven times** -- "
            + zahlen.GetValueOrDefault(MzCommandTable.ScrollMap)
            + ", in eighty-eight forms");
        AssertTrue(
            zahlen.GetValueOrDefault(MzCommandTable.TintPicture) >= 102,
            "**and 234 a hundred and two times** -- "
            + zahlen.GetValueOrDefault(MzCommandTable.TintPicture));
        AssertTrue(
            zahlen.GetValueOrDefault(MzCommandTable.ShopProcessing) >= 39,
            "**and 302 thirty-nine times** -- "
            + zahlen.GetValueOrDefault(MzCommandTable.ShopProcessing));

        // **Und keine einzige `605` im ganzen Spiel** -- **und das ist
        // kein Zufall, sondern die Regel: `goods = [this._params]` und
        // dann die Zeilen.** **Ein Spiel mit einem Waren pro Befehl hat
        // keine Folgezeilen, und dieses hat 39 von beiden.**
        AssertEq(goods, 0,
            "**and not one 605 line in the whole game** -- " + goods
            + ", and `command302` begins `const goods = [this._params]` "
            + "and only then reads 605s, so a game that writes one good "
            + "per command has none of them");

        // **Und `234` wartet bei sechzehn und nicht bei allen.**
        var wartende = formen[MzCommandTable.TintPicture]
            .Where(k => k.Key.EndsWith("|true", StringComparison.Ordinal))
            .Sum(k => k.Value);
        AssertTrue(wartende >= 16,
            "**and sixteen of the 234 wait for the tint to finish** -- "
            + wartende + " of "
            + zahlen.GetValueOrDefault(MzCommandTable.TintPicture)
            + ", and the fourth parameter is a flag and not a number, "
            + "and `command234` says `if (this._params[3]) { this.wait("
            + "this._params[2]); }`");

        // **Und die Richtungen sind 2, 4, 6 und 8**, **und 6 kommt am
        // haeufigsten vor**, **denn 6 ist rechts und die Spieler gehen
        // nach rechts.**
        var richtungen = new HashSet<int>();
        foreach (var k in formen[MzCommandTable.ScrollMap].Keys)
        {
            richtungen.Add(int.Parse(k.Split('|')[0],
                System.Globalization.CultureInfo.InvariantCulture));
        }

        AssertTrue(richtungen.IsSubsetOf(new HashSet<int> { 2, 4, 6, 8 }),
            "**and every direction is one of the engine's four** -- and "
            + "they are " + string.Join(",", richtungen.OrderBy(x => x))
            + ", and `doScroll` switches on 2 down, 4 left, 6 right and "
            + "8 up, and it has no case for anything else");
    }

    /// <summary>
    /// And the scroll is a fraction, and it waits on the map and not on a
    /// number of frames.
    /// </summary>
    public void Test_DasScrollenIstEinBruchUndEinZustand()
    {
        var interp = new MzInterpreter(new List<MzCommandEntry>
        {
            Befehl(MzCommandTable.ScrollMap, "6", "4", "4"),
            Befehl(0, "\"\""),
        });
        interp.Setup(1, 1);
        var fakten = new MzBranchFacts();
        var aktionen = new List<MzAction>();
        interp.Run(aktionen, fakten);

        AssertTrue(fakten.Rollen.Laeuft,
            "**and the map is scrolling** -- and `isScrolling` is "
            + "`this._scrollRest > 0` and `startScroll` set it to four");
        AssertEq(fakten.Rollen.Rest, 4,
            "**and four tiles are left** -- and it is "
            + fakten.Rollen.Rest + ", and the engine's own form in this "
            + "game is `[6, 4, 4]`, six being right");

        // **Und `scrollDistance` ist `Math.pow(2, 4) / 256` = 1/16.**
        AssertEq(fakten.Rollen.Schritt(), 0.0625,
            "**and one sixteenth of a tile a frame** -- and it is "
            + fakten.Rollen.Schritt() + ", and `scrollDistance` is "
            + "`Math.pow(2, this._scrollSpeed) / 256`, and a reader "
            + "that rounded that to a tile would move the map sixteen "
            + "times too fast at this speed and two hundred and fifty "
            + "six times too fast at speed one");

        // **Und die zweite Karte wartet, weil es schon rollt.**
        var zweiter = new MzInterpreter(new List<MzCommandEntry>
        {
            Befehl(MzCommandTable.ScrollMap, "6", "4", "4"),
            Befehl(0, "\"\""),
        });
        zweiter.Setup(1, 1);
        var fakten2 = new MzBranchFacts();
        fakten2.Rollen.Starte(6, 4, 4);
        zweiter.Run(new List<MzAction>(), fakten2);
        AssertEq(zweiter.Stopped, MzStep.Waiting,
            "**and a second 204 waits instead of starting a new one** -- "
            + "and it came to " + zweiter.Stopped + ", and "
            + "`command204` says `if ($gameMap.isScrolling()) { "
            + "this.setWaitMode('scroll'); return false; }`, and that "
            + "is a state and not a number of frames");

        // **Und die Karte kommt an den Rand, und `updateScroll` setzt
        // den Rest auf null, ohne zu subtrahieren.**
        fakten.Rollen.EinBild(true);
        AssertTrue(fakten.Rollen.Laeuft,
            "**and it keeps going while the display moves**");
        var stand = fakten.Rollen.Rest;
        fakten.Rollen.EinBild(false);
        AssertEq(fakten.Rollen.Rest, 0,
            "**and it stops at the edge with the rest at zero** -- it "
            + "went from " + stand + " to " + fakten.Rollen.Rest
            + ", and `updateScroll` says `if (this._displayX === lastX "
            + "&amp;&amp; this._displayY === lastY) { this._scrollRest "
            + "= 0; }`, and that is the map's edge and not a distance");
    }

    /// <summary>
    /// And the shop reads its goods from the command and pushes a scene.
    /// </summary>
    public void Test_DerLadenLiestSeineWarenAusDemBefehl()
    {
        var interp = new MzInterpreter(new List<MzCommandEntry>
        {
            Befehl(MzCommandTable.ShopProcessing, "1", "2", "3", "4", "0"),
            Befehl(0, "\"\""),
        });
        interp.Setup(1, 1);
        var fakten = new MzBranchFacts();
        // **Und die Karte ist die Szene, von der aus der Laden kommt.**
        fakten.Szene.GeheZu("Scene_Map");
        interp.Run(new List<MzAction>(), fakten);

        AssertEq(fakten.LetzterLaden.Count, 1,
            "**and the shop has one good** -- and that is the command's "
            + "own parameters, and `command302` begins `const goods = "
            + "[this._params]`, and a reader that started with an empty "
            + "list would sell nothing");
        AssertEq(fakten.Szene.Current, "Scene_Shop",
            "**and the shop is what stands on the screen** -- it is '"
            + fakten.Szene.Current + "', and `command352` pushes the save "
            + "screen the same way, and `command354` does not push at all");
        AssertTrue(fakten.Szene.CanPop,
            "**and it can be walked back out of** -- and that is what "
            + "push does and goto does not, and the stack holds "
            + fakten.Szene.Stack.Count + " scene, and it holds "
            + fakten.Szene.Stack[0] + ", and the shop is the one a "
            + "player leaves with the cancel button");
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
