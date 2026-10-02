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
/// The command that gives an actor hit points, and what this repository's
/// reading of it really does.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And <c>311 Change Actor HP</c> is fourteen times in this game's
/// 133484 map commands</strong>, <strong>and five different commands are
/// among the fourteen</strong>:
/// <c>[0, 1, 0, 0, 12, false]</c> nine times, <c>[0, 1, 1, 0, 450,
/// false]</c>, <c>[0, 1, 0, 0, 999, false]</c>, <c>[0, 1, 0, 0, 3,
/// false]</c>, and <c>[0, 1, 1, 0, 999, true]</c> twice.
/// </para>
/// <para>
/// <strong>And <c>command311</c> is</strong> <c>const value =
/// this.operateValue(this._params[2], this._params[3], this._params[4]);
/// this.iterateActorEx(this._params[0], this._params[1], actor =&gt; {
/// this.changeHp(actor, value, this._params[5]); }); return true;</c>
/// </para>
/// <para>
/// <strong>And the first two parameters are not what the editor calls
/// them.</strong> <c>iterateActorEx</c> is <c>if (param1 === 0) {
/// this.iterateActorId(param2) } else { this.iterateActorId(
/// $gameVariables.value(param2)) }</c> -- <strong>so the first parameter
/// says whether the target is a number or a variable, and not whether it
/// is the whole party</strong>, <strong>and <c>313</c> and <c>314</c>
/// read the same two slots the same way.</strong>
/// </para>
/// <para>
/// <strong>And all fourteen of this game's start <c>0|1|</c></strong> --
/// <strong>a constant, and actor number one</strong>, <strong>which is
/// the game's hero.</strong> <strong>A reader that read the second
/// parameter as "the whole party" would hand every hit point to the whole
/// cast.</strong>
/// </para>
/// <para>
/// <strong>And the sixth parameter is the rest of the command.</strong>
/// <c>changeHp</c> is <c>if (target.isAlive()) { if (!allowDeath &amp;&amp;
/// target.hp &lt;= -value) { value = 1 - target.hp; } target.gainHp(value);
/// if (target.isDead()) { target.performCollapse(); } }</c> -- <strong>so
/// a nine-hundred-and-ninety-nine-point loss that may not kill becomes one
/// point short of death</strong>, <strong>and this game writes
/// <c>true</c> twice so that it does kill.</strong>
/// </para>
/// <para>
/// <strong>And what this repository records is the order and not the
/// result</strong>, <strong>because the result needs <c>_hp</c> and
/// <c>_mhp</c></strong>, <strong>and those come from <c>Actors.json</c>
/// and <c>Classes.json</c></strong>, <strong>and this repository keeps no
/// actor</strong> -- <strong>and an invented result would be a number
/// about a thing nobody measured.</strong>
/// </para>
/// </remarks>
public partial class TestMvActorOrders : TestBase
{
    private const string Projekt = "D:/Itch/sister/www";

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
    /// Every 311 this game writes, and one of them running.
    /// </summary>
    public void Test_JedesEinUndDreizehnAusDiesemSpielLaeuft()
    {
        if (!Vorhanden())
        {
            return;
        }

        var eigene = 0;
        var formen = new Dictionary<string, int>();
        foreach (var datei in Karten())
        {
            foreach (var c in BefehleDerKarte(datei))
            {
                if (c.Code != MzCommandTable.ChangeHp)
                {
                    continue;
                }

                eigene++;
                AssertEq(c.Parameters.Count, 6,
                    "**and every 311 carries six parameters** -- '"
                    + string.Join("|", c.Parameters) + "' carries "
                    + c.Parameters.Count);
                var form = string.Join("|", c.Parameters);
                formen[form] = formen.GetValueOrDefault(form) + 1;
            }
        }

        AssertTrue(eigene >= 14,
            "**and this game writes fourteen of them** -- " + eigene
            + ", and it is the most used command of the ones this "
            + "repository still did not run");

        // **Und es sind fuenf Formen und nicht eine** -- **und meine
        // erste Fassung dieses Tests behauptete, alle vierzehn waeren
        // gleich**, **und hatte nur die Karten gelesen** -- **und
        // `CommonEvents.json` hat drei der vierzehn.**
        AssertEq(formen.Count, 5,
            "**and this game writes five different ones** -- "
            + string.Join(", ", formen.Select(pF => pF.Value + "x '"
                + pF.Key + "'")));
        AssertTrue(formen.Keys.All(k => k.StartsWith("0|1|",
                StringComparison.Ordinal)),
            "**and every one of the five starts `0|1|`** -- and they are "
            + string.Join(", ", formen.Keys) + ", and `iterateActorEx` "
            + "is `if (param1 === 0) { this.iterateActorId(param2) }`, so "
            + "the first parameter says whether the target is a number or "
            + "a variable, and not whether it is the whole party");
        AssertTrue(
            formen.Keys.Count(k => k.EndsWith("|true",
                StringComparison.Ordinal)) >= 1,
            "**and one of the five may kill** -- and it is "
            + string.Join(", ", formen.Keys.Where(k => k.EndsWith("|true",
                StringComparison.Ordinal)))
            + ", and `changeHp` shortens a killing blow to one point "
            + "short of death only when `allowDeath` is false");

        // **Und jetzt laeuft eines**, **denn eine Abdeckung ohne einen
        // Lauf ist eine Liste und kein Beweis.**
        var interp = new MzInterpreter(new List<MzCommandEntry>
        {
            Befehl(MzCommandTable.ChangeHp,
                "0", "1", "0", "0", "12", "false"),
        });
        interp.Setup(1, 1);
        var fakten = new MzBranchFacts
        {
            PartyMembers = new HashSet<int> { 1, 2, 3 },
        };
        var aktionen = new List<MzAction>();
        interp.Run(aktionen, fakten);

        AssertEq(aktionen.Count, 1,
            "**and it ran once** -- " + string.Join(" / ",
                aktionen.Select(pA => pA.ToString())));
        AssertEq(fakten.HpOrders.Count, 1,
            "**and it ordered one actor's hit points** -- "
            + fakten.HpOrders.Count + " orders, and a command that reports "
            + "finished and changes nothing is what this repository had "
            + "for 311, and `0|1` names actor one and not the party");
        AssertEq(fakten.HpOrders[0].Actor, 1,
            "**and the actor is number one** -- and it is "
            + fakten.HpOrders[0].Actor + ", and the first parameter is 0, "
            + "so the target list is `iterateActorId(param2)` and param2 "
            + "is 1");
        AssertEq(fakten.HpOrders[0].Value, 12,
            "**and the value is plus twelve** -- and it is "
            + fakten.HpOrders[0].Value + ", and `operateValue` returns "
            + "`operation === 0 ? value : -value`, and this game's third "
            + "parameter is 0 in every one of the fourteen, so it heals");
        AssertTrue(!fakten.HpOrders[0].AllowDeath,
            "**and the party may not die of it** -- and the sixth "
            + "parameter is `false` in thirteen of the fourteen, and "
            + "`changeHp` shortens a killing blow to one point short of "
            + "death");
    }

    /// <summary>
    /// And the first parameter says whether the target is in a variable.
    /// </summary>
    /// <remarks>
    /// <strong>And this is the one thing about this command that is not in
    /// its name</strong>, <strong>and this repository's first version did
    /// not resolve the variable</strong> -- <strong>it ordered actor number
    /// two</strong>, <strong>which is a number that means nothing.</strong>
    /// </remarks>
    public void Test_DerErsteParameterSagtObDasZielInEinerVariablenSteht()
    {
        var fakten = new MzBranchFacts
        {
            PartyMembers = new HashSet<int> { 7, 1 },
        };
        fakten.SetVariable(2, 7);
        var interp = new MzInterpreter(new List<MzCommandEntry>
        {
            Befehl(MzCommandTable.ChangeHp, "1", "2", "0", "0", "12",
                "false"),
        });
        interp.Setup(1, 1);
        interp.Run(new List<MzAction>(), fakten);

        AssertEq(fakten.HpOrders.Count, 1,
            "**and a first parameter of one picks one actor** -- "
            + fakten.HpOrders.Count + " order, and `iterateActorEx` is "
            + "`else { this.iterateActorId($gameVariables.value(param2)) "
            + "}`");
        AssertEq(fakten.HpOrders[0].Actor, 7,
            "**and that variable holds the actor** -- and it is actor "
            + fakten.HpOrders[0].Actor + ", and variable two holds 7, and "
            + "the game's own 313 writes `0|2|0|25`, where the 2 says the "
            + "same thing about states");
    }

    private static IEnumerable<string> Karten()
    {
        foreach (var pfad in Directory.GetFiles(
            Projekt + "/data", "Map*.json"))
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
