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
/// Weather, battlebacks, rotation and two ways of naming an actor.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the forms are measured at
/// <c>D:/NextCloud/Games/Android Games/vhmv/VHMV</c></strong>:
/// </para>
/// <code>
/// 236 Set Weather Effect   2x, and one of them is ["none", 5, 1, true]
/// 320 Change Name         14x, all of them {CE-...} in braces
/// 233 Rotate Picture       0x
/// 283 Change Battleback    0x
/// 303 Change Actor Name    0x
/// </code>
/// <para>
/// <strong>And that first weather form is the whole of the command's
/// only rule.</strong> <code>changeWeather</code> is
/// <c>if (type !== 'none' || duration === 0) { this._weatherType =
/// type; }</c> -- <strong>so <c>none</c> with a duration above zero
/// leaves the type standing</strong> -- <strong>and this game writes
/// <c>none</c> with a duration of one, which is one frame, and the
/// condition then says "yes, set it", and the rain stops at
/// once.</strong>
/// </para>
/// <para>
/// <strong>And <c>320</c>s names are <c>{CE-VirginBlood-1}</c> and its
/// neighbours</strong> -- <strong>in braces</strong> -- <strong>and
/// those are <c>Comment</c> escape codes the engine's own text
/// renderer understands</strong> -- <strong>and <c>setName</c> stores
/// the string as it is.</strong> <strong>And this repository stores it
/// and does not expand it.</strong>
/// </para>
/// </remarks>
public partial class TestMvWeatherAndNames : TestBase
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
    /// The forms this game writes, and what they mean.
    /// </summary>
    public void Test_DieFormenAusDiesemSpiel()
    {
        if (!Vorhanden())
        {
            return;
        }

        var w236 = new Dictionary<string, int>();
        var n320 = 0;
        foreach (var datei in Karten())
        {
            foreach (var c in BefehleDerKarte(datei))
            {
                if (c.Code == MzCommandTable.SetWeatherEffect)
                {
                    var form = string.Join("|", c.Parameters);
                    w236[form] = w236.GetValueOrDefault(form) + 1;
                }
                else if (c.Code == MzCommandTable.ChangeName)
                {
                    n320++;
                }
            }
        }

        AssertEq(w236.Count, 2,
            "**and 236 has two forms in this game** -- "
            + string.Join(", ", w236.Select(k => k.Key + " " + k.Value))
            + ", and they are `rain` at power two and `none` at power "
            + "five, and both wait");

        // **Und `none` mit einer Dauer ueber null ist die Form, an der
        // man die Regel sieht** -- **und dieses Spiel schreibt sie mit
        // einer Dauer von eins**, **und eins ist ein Bild, und `duration
        // === 0` ist damit falsch**, **und der Typ wird gesetzt und der
        // Regen hoert sofort auf.**
        AssertTrue(
            w236.Keys.Any(k => k.StartsWith("none|5|", StringComparison.Ordinal)),
            "**and the `none` form is this game's, with a duration of one"
            + "** -- and that is `type !== 'none' || duration === 0` "
            + "saying yes, because one frame is not zero");

        AssertTrue(n320 >= 14,
            "**and 320 fourteen times** -- " + n320 + ", in fourteen "
            + "forms, one for each name");
    }

    /// <summary>
    /// And the rule that a `none` with a duration above zero keeps the
    /// type.
    /// </summary>
    public void Test_DasKeinBeiDauerNullBleibtKein()
    {
        // **Und die Regel des Codes:** `none` mit einer Dauer ueber
        // null laesst den Typ stehen.
        var mitDauer = new MzWeather();
        mitDauer.Setze("rain", 5, 60);
        AssertEq(mitDauer.Typ, "rain",
            "**and rain first**");
        mitDauer.Setze("none", 0, 120);
        AssertEq(mitDauer.Typ, "rain",
            "**and `none` over two seconds leaves the rain standing** -- "
            + "and it is " + mitDauer.Typ + ", and `changeWeather` says "
            + "`if (type !== 'none' || duration === 0)`, and 120 is not "
            + "zero, and a reader that always set the type would stop "
            + "the rain at once and make the duration do nothing");
        AssertEq(mitDauer.KraftZiel, 0,
            "**and the power goes to zero either way** -- and it is "
            + mitDauer.KraftZiel + ", and `type === 'none' ? 0 : power` "
            + "is not guarded by the condition above it");

        // **Und `none` mit Dauer null raeumt sofort auf** -- **und das
        // ist der Unterschied zur Zeile darüber.**
        var sofort = new MzWeather();
        sofort.Setze("storm", 9, 0);
        AssertEq(sofort.Kraft, 9,
            "**and a storm at nine with no duration is at once**");
        sofort.Setze("none", 5, 0);
        AssertEq(sofort.Typ, "none",
            "**and `none` with a duration of zero clears the type** -- and "
            + "it is " + sofort.Typ + ", and that is the second half of "
            + "the same condition");
        AssertEq(sofort.Kraft, 0,
            "**and the power with it**");
    }

    /// <summary>
    /// And 320 stores the name as the game wrote it.
    /// </summary>
    public void Test_DerNameWirdGespeichertWieDasSpielIhnSchreibt()
    {
        // **Und `{CE-VirginBlood-1}` ist ein Kommentar-Escape des
        // Text-Renderers** -- **und `setName` ist eine
        // Zuweisung.**
        var interp = new MzInterpreter(new List<MzCommandEntry>
        {
            // **Und `{` und `}` muessen im JSON-Parameter als Text
            // stehen**, **denn der Helfer baut die Liste aus den
            // Zeichenketten**, **und ein nacktes `{CE-...}` ist kein
            // gueltiges JSON.** **Meine erste Fassung gab den Namen
            // unescaped und der Leser sagte "a member's name is
            // expected at character 41".**
            Befehl(MzCommandTable.ChangeName, "60",
                "\"{CE-VirginBlood-1}\""),
            Befehl(0, "\"\""),
        });
        interp.Setup(1, 1);
        var fakten = new MzBranchFacts
        {
            PartyMembers = new HashSet<int> { 60, 61 },
        };
        interp.Run(new List<MzAction>(), fakten);

        AssertTrue(fakten.Namen.ContainsKey(60),
            "**and actor sixty has a name**");
        AssertEq(fakten.Namen[60], "{CE-VirginBlood-1}",
            "**and it is stored as the game wrote it** -- it is '"
            + fakten.Namen[60] + "', and all fourteen of this game's "
            + "names are in braces, and those are escape codes the "
            + "text renderer understands and this repository does not "
            + "expand");
        AssertTrue(!fakten.Namen.ContainsKey(61),
            "**and nobody else was named** -- and that is `const actor "
            + "= $gameActors.actor(this._params[0]); if (actor) { "
            + "actor.setName(this._params[1]); }`, and sixty-one is "
            + "in the party and this command said sixty");

        // **Und `303` schiebt die Namensszene und merkt sich, fuer wen.**
        var interp2 = new MzInterpreter(new List<MzCommandEntry>
        {
            Befehl(MzCommandTable.ChangeActorName, "60", "\"Rin\""),
            Befehl(0, "\"\""),
        });
        interp2.Setup(1, 1);
        var fakten2 = new MzBranchFacts();
        fakten2.Szene.GeheZu("Scene_Map");
        interp2.Run(new List<MzAction>(), fakten2);
        AssertEq(fakten2.Szene.Current, "Scene_Name",
            "**and 303 pushes the name editor** -- it is '"
            + fakten2.Szene.Current + "'");
        AssertEq(fakten2.NamensZiel, 60,
            "**and it is for actor sixty** -- and that is "
            + "`SceneManager.prepareNextScene(this._params[0], "
            + "this._params[1])`, and the first argument is the actor's "
            + "number and not a field slot");
        AssertEq(fakten2.NamensText, "Rin",
            "**and the second argument is the name**");
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
