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
/// The four commands that give an actor a number, on the largest MV
/// project on this machine.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the project is
/// <c>D:/NextCloud/Games/Android Games/vhmv/VHMV</c></strong> --
/// <strong>838 maps and 521262 commands</strong>, <strong>which is more
/// than three times the next one.</strong> <strong>And it is the reason
/// the earlier ranking was right and my search for it was
/// wrong</strong>: <strong>I looked at <c>D:/Itch</c> and
/// <c>E:/RPGMakerGames</c> and forgot <c>D:/NextCloud</c>.</strong>
/// </para>
/// <para>
/// <strong>And the counts, all measured:</strong>
/// </para>
/// <code>
/// 317 Change Parameter   461x, 128 forms
/// 312 Change MP           72x,  32 forms
/// 315 Change EXP          46x,  15 forms
/// 316 Change Level         2x,   2 forms
/// </code>
/// <para>
/// <strong>And the commonest <c>317</c> is
/// <c>[0, 1, 0, 1, 0, 1]</c>, <c>[0, 1, 2, 1, 0, 1]</c> and
/// <c>[0, 1, 3, 1, 0, 1]</c>, thirteen times each</strong> --
/// <strong>and every one of them gives actor one, parameter zero, one,
/// two or three, a constant.</strong>
/// </para>
/// <para>
/// <strong>And the commonest <c>312</c> and <c>315</c> carry
/// <c>1, 397</c></strong> -- <strong>and the first parameter of one is
/// "the actor is in a variable"</strong>, <strong>and that variable is
/// 397</strong>, <strong>and it is set by a plugin or by an earlier
/// command this repository can only read.</strong> <strong>And
/// <c>[1, 397, 1, 0, 2]</c> is therefore actor <c>value(397)</c>, whose
/// magic points change by a constant two.</strong>
/// </para>
/// </remarks>
public partial class TestMvActorNumbers : TestBase
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
    /// Every one of the four this game writes, and their forms.
    /// </summary>
    public void Test_DieVierBefehleDiesesSpiels()
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
                if (c.Code != MzCommandTable.ChangeParameter
                    && c.Code != MzCommandTable.ChangeMp
                    && c.Code != MzCommandTable.ChangeExp
                    && c.Code != MzCommandTable.ChangeLevel)
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

        AssertTrue(
            zahlen.GetValueOrDefault(MzCommandTable.ChangeParameter) >= 461,
            "**and it writes 317 four hundred and sixty-one times** -- "
            + zahlen.GetValueOrDefault(MzCommandTable.ChangeParameter));
        AssertEq(formen[MzCommandTable.ChangeParameter].Count, 128,
            "**and in a hundred and twenty-eight forms** -- "
            + formen[MzCommandTable.ChangeParameter].Count + ", and a "
            + "command with 128 shapes is not one command but a family");
        AssertTrue(zahlen.GetValueOrDefault(MzCommandTable.ChangeMp) >= 72,
            "**and 312 seventy-two times** -- "
            + zahlen.GetValueOrDefault(MzCommandTable.ChangeMp));
        AssertTrue(zahlen.GetValueOrDefault(MzCommandTable.ChangeExp) >= 46,
            "**and 315 forty-six times** -- "
            + zahlen.GetValueOrDefault(MzCommandTable.ChangeExp));
        AssertTrue(zahlen.GetValueOrDefault(MzCommandTable.ChangeLevel)
                >= 2,
            "**and 316 twice** -- "
            + zahlen.GetValueOrDefault(MzCommandTable.ChangeLevel));

        // **Und die haeufigste 317 gibt dem Darsteller eins einen
        // Parameter**, **und die drei haeufigsten unterscheiden sich nur
        // in der Parameternummer.**
        var haeufigste = formen[MzCommandTable.ChangeParameter]
            .OrderByDescending(pP => pP.Value)
            .Select(pP => pP.Key)
            .ToList();
        AssertTrue(haeufigste[0].StartsWith("0|1|", StringComparison.Ordinal),
            "**and the commonest gives actor one a parameter** -- it is '"
            + haeufigste[0] + "', and `0` says the actor is a constant "
            + "and `1` is actor number one, and the third is the "
            + "parameter and the fourth is the operation and the fifth "
            + "is the kind and the sixth is the value, and that is six "
            + "slots because `operateValue` starts at `params[3]` here "
            + "and at `params[2]` in every other command of this family");

        // **Und `315` und `312` nennen beide die Variable 397** -- **und
        // das ist der ganze Unterschied zu `311`, das im
        // MV-Spiel `[0, 1, ...]` schreibt und also einen
        // konstanten Darsteller.**
        // **Und 44 von 46 `315` nennen Variable 397** -- **und meine
        // erste Fassung behauptete zwanzig und bekam sechsundzwanzig aus
        // `312`**, **und die Formenliste zaehlt verschiedene Schlüssel und
        // nicht ihre Vorkommen.**
        AssertTrue(
            formen[MzCommandTable.ChangeExp]
                .Where(k => k.Key.StartsWith("1|397|",
                    StringComparison.Ordinal))
                .Sum(pP => pP.Value) >= 44,
            "**and forty-four of the forty-six 315 name variable 397** -- "
            + "and " + formen[MzCommandTable.ChangeMp]
                .Where(k => k.Key.StartsWith("1|397|",
                    StringComparison.Ordinal))
                .Sum(pP => pP.Value) + " of the 312 do, and "
            + "`iterateActorEx` is `else { this.iterateActorId("
            + "$gameVariables.value(param2)) }`, so a first parameter of "
            + "one is not the hero and not the party and it is whoever "
            + "that variable holds");
    }

    /// <summary>
    /// And all four run, and the one that reads a variable reads it.
    /// </summary>
    public void Test_DieVierBefehleLaufen()
    {
        var fakten = new MzBranchFacts
        {
            PartyMembers = new HashSet<int> { 4, 1, 7 },
        };
        fakten.SetVariable(397, 4);
        var interp = new MzInterpreter(new List<MzCommandEntry>
        {
            // **Und `317` hat sechs Parameter, weil der dritte die
            // Parameternummer ist und `operateValue` bei `[3]`
            // beginnt.**
            Befehl(MzCommandTable.ChangeParameter, "0", "1", "2", "0", "0",
                "5"),
            Befehl(MzCommandTable.ChangeMp, "1", "397", "1", "0", "2"),
            Befehl(MzCommandTable.ChangeExp, "0", "7", "0", "0", "150",
                "false"),
            Befehl(MzCommandTable.ChangeLevel, "0", "1", "1", "0", "2",
                "true"),
            Befehl(0, "\"\""),
        });
        interp.Setup(1, 1);
        interp.Run(new List<MzAction>(), fakten);

        AssertEq(fakten.ParamOrders.Count, 1,
            "**and 317 ordered one parameter** -- "
            + fakten.ParamOrders.Count + ", and `[0, 1, 2, 0, 5, 1]` is "
            + "actor one, parameter two, plus a constant five");
        AssertEq(fakten.ParamOrders[0].Actor, 1,
            "**and it is actor one**");
        AssertEq(fakten.ParamOrders[0].Was, 2,
            "**and parameter two** -- and `paramMax` gives 999999 for 0, "
            + "9999 for 1 and 999 for everything else, and 2 is an "
            + "attribute");
        AssertEq(fakten.ParamOrders[0].Value, 5,
            "**and plus five** -- and `operateValue` returns "
            + "`operation === 0 ? value : -value`, and for 317 it is "
            + "called as `operateValue(params[3], params[4], "
            + "params[5])` while 311 is called as `operateValue(params[2], "
            + "params[3], params[4])`, and the fourth parameter here is "
            + "the operation and 0 means take");

        AssertEq(fakten.MpOrders.Count, 1,
            "**and 312 ordered one actor's magic points** -- "
            + fakten.MpOrders.Count);
        AssertEq(fakten.MpOrders[0].Actor, 4,
            "**and it is actor four, because variable 397 holds four** -- "
            + "it is " + fakten.MpOrders[0].Actor + ", and this is the "
            + "form `[1, 397, 1, 0, 2]` that this game writes eleven "
            + "times, and a reader that read the first parameter as "
            + "\"the whole party\" would have given it to three actors");

        AssertEq(fakten.ExpOrders.Count, 1,
            "**and 315 ordered one actor's experience** -- "
            + fakten.ExpOrders.Count);
        AssertEq(fakten.ExpOrders[0].Actor, 7,
            "**and it is actor seven**");
        AssertEq(fakten.ExpOrders[0].Value, 150,
            "**and plus a hundred and fifty**");
        AssertTrue(!fakten.ExpOrders[0].Show,
            "**and the sixth parameter says it does not show it** -- and "
            + "`changeExp(exp, show)` uses it for `displayLevelUp`, and "
            + "this game writes `false` in all forty-six");

        AssertEq(fakten.LevelOrders.Count, 1,
            "**and 316 ordered one actor's levels** -- "
            + fakten.LevelOrders.Count);

        // **Und `show` ist in diesem Spiel immer `false`** -- **und
        // `Fatal Fantasy` schreibt es auch immer `false`** -- **und
        // meine erste Fassung dieses Tests gab ihm `true` und nannte
        // das einen Befund**, **und der Befund war die Assertion.**
        AssertTrue(!fakten.LevelOrders[0].Show,
            "**and `show` is false, as it is in all forty-six of this "
            + "game's 315 and both of its 316** -- the engine's own form "
            + "is `[0, 4, 1, 0, 9, false]` and `[0, 0, 0, 0, 5, false]`, "
            + "and `changeLevel(level, show)` uses it for "
            + "`displayLevelUp`, and a game that never asks for the "
            + "banner is a game that never levels up on screen");
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
