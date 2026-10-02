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
/// The three commands that were most often unrun in a finished MV game, and
/// the three places each of them is easy to get wrong.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And all three were absent, and all three are frequent:</strong>
/// <c>313</c> three hundred and nineteen times, <c>224</c> one hundred and
/// fifty-five, <c>212</c> ninety, <c>128</c> forty-seven -- <strong>which is
/// six hundred and eleven commands that ran as nothing and were reported as
/// finished.</strong>
/// </para>
/// <para>
/// <strong>And each one has a trap, and each trap is a different kind.</strong>
/// <c>313</c>'s third parameter is a direction and not a number. <c>224</c>'s
/// colour has four channels and steps toward its target rather than toward
/// full opacity. <c>212</c>'s first parameter is a character and not an
/// actor id. <c>128</c>'s <c>operateValue</c> starts one slot later than
/// <c>125</c>'s.
/// </para>
/// </remarks>
public partial class TestMvStatesAnimationArmor : TestBase
{
    private const string Projekt = "D:/Itch/sister/www";

    private static bool Vorhanden()
    {
        if (!Directory.Exists(Projekt + "/data"))
        {
            GD.Print("    (skipped: no " + Projekt + ")");
            return false;
        }

        return true;
    }

    /// <summary>
    /// The game's own forms, counted out of its own files.
    /// </summary>
    public void Test_DieEigenenFormenAusDenDateien()
    {
        if (!Vorhanden())
        {
            return;
        }

        var zahlen = new Dictionary<int, int>();
        var proben = new Dictionary<int, string>();
        var formen = new Dictionary<int, Dictionary<string, int>>();
        // **Und `CommonEvents.json` mitzaehlen** -- **denn die erste Fassung
        // dieses Tests las nur die Karten und fand 275 `313` statt 319**,
        // **und die Zahl der Karten allein ist eine kleinere Zahl und nicht
        // die ganze.**
        foreach (var datei in Directory.GetFiles(
            Projekt + "/data", "Map*.json", SearchOption.AllDirectories)
            .Concat(new[] { Projekt + "/data/CommonEvents.json" }))
        {
            // **Und die gemeinsame Ereignisliste faengt nicht mit "Map"
            // an** -- **und ein Filter, der nur Karten gelten laesst,
            // uebersieht sie und zaehlt zweihundertfuenfundsiebzig `313`
            // statt dreihundertneunzehn.** **Und die Differenz sind genau
            // die gemeinsamen Ereignisse, und die sind ein Teil des
            // Spiels.**
            var name = Path.GetFileNameWithoutExtension(datei);
            var istKarte = name.StartsWith("Map", StringComparison.Ordinal)
                && name.Length > 3
                && name.Substring(3).All(char.IsDigit);
            if (!istKarte
                && !string.Equals(name, "CommonEvents",
                    StringComparison.Ordinal))
            {
                continue;
            }

            foreach (var c in BefehleDerKarte(datei))
            {
                if (c.Code != 313 && c.Code != 224 && c.Code != 212
                    && c.Code != 128)
                {
                    continue;
                }

                // **Und das hier ist der Rohwert und nicht JSON** -- **und
                // `MzCommandEntry` haelt eine Liste von Texten, in der eine
                // verschachtelte Liste als ihr geschriebenes JSON
                // steht.** **Und darum ist `[255,0,0,170]` in
                // Anfuehrungszeichen gesetzt, und das ist richtig.**
                zahlen[c.Code] = zahlen.GetValueOrDefault(c.Code) + 1;
                var rohForm = string.Join("|", c.Parameters);
                if (!formen.TryGetValue(c.Code, out var proCodeFormen))
                {
                    proCodeFormen = new Dictionary<string, int>();
                    formen[c.Code] = proCodeFormen;
                }

                proCodeFormen[rohForm] =
                    proCodeFormen.GetValueOrDefault(rohForm) + 1;
                proben.TryAdd(c.Code, rohForm);
            }
        }

        System.Console.WriteLine("MV haeufigste neue Befehle: 313="
            + zahlen.GetValueOrDefault(313) + " 224="
            + zahlen.GetValueOrDefault(224) + " 212="
            + zahlen.GetValueOrDefault(212) + " 128="
            + zahlen.GetValueOrDefault(128));
        AssertTrue(zahlen.GetValueOrDefault(313) > 300,
            "**and 313 is used more than three hundred times** -- "
            + zahlen.GetValueOrDefault(313));
        AssertTrue(zahlen.GetValueOrDefault(224) > 150,
            "**and 224 more than a hundred and fifty** -- "
            + zahlen.GetValueOrDefault(224));
        AssertTrue(zahlen.GetValueOrDefault(212) >= 90,
            "**and 212 more than ninety** -- "
            + zahlen.GetValueOrDefault(212));
        AssertTrue(zahlen.GetValueOrDefault(128) > 40,
            "**and 128 more than forty** -- "
            + zahlen.GetValueOrDefault(128));

        // **Und die vier Formen, wie sie dort stehen.**
        // **Und die haeufigste Form ist `0|2|0|N`** -- **und "2" heisst
        // nicht "zweiter Darsteller" und nicht Boolean, sondern
        /// `iterateActorEx`, dessen zweiter Parameter die ganze Party
        // bedeutet.** **Und 248 der 319 haben diese Form.**
        // **Und die haeufigste Form, nicht die erste gefundene** -- **denn
        // `TryAdd` nimmt, was zuerst kommt, und das ist eine Reihenfolge
        // und keine Aussage ueber das Spiel.**
        var haeufigste313 = formen[313]
            .OrderByDescending(pP => pP.Value)
            .First();
        AssertTrue(haeufigste313.Value >= 40,
            "**and the commonest 313 is used more than forty times** -- "
            + haeufigste313.Value + " times as '" + haeufigste313.Key
            + "', and 248 of the 319 are `0|2|0|N`, which is 'add this "
            + "state to the whole party'");
        AssertTrue(haeufigste313.Key.StartsWith("0|2|0|",
            StringComparison.Ordinal),
            "**and the commonest form is `0|2|0|N`** -- it said '"
            + haeufigste313.Key + "', and the second slot is not a second "
            + "actor but `iterateActorEx`'s own whole-party flag");

        AssertTrue(
            formen[128].Keys.All(k => k.EndsWith("|false",
                StringComparison.Ordinal)),
            "**and every 128 ends in the equip flag** -- and it is written "
            + "as `False`, which is how a .NET boolean reaches a string and "
            + "not the lower-case word the JSON file holds");
        AssertTrue(
            formen[224].Keys.All(k => k.StartsWith("[",
                StringComparison.Ordinal)),
            "**and every 224's first parameter is a list** -- and a reader "
            + "that parsed it as text tinted nothing");
        // **Und dreizehn der einunddreianzig Formen warten auf die
        // Animation und achtzehn nicht** -- **und die erste Fassung
        // dieser Assertion behauptete, alle wuerden auf nichts warten,
        // und das war aus einer Stichprobe von sechs geschlossen und
        // nicht gemessen.** **Und `MzWaitMode.Animation` ist deshalb kein
        // Randfall, sondern die Haelfte aller 212 in diesem Spiel.**
        var wartende212 = formen[212].Keys.Count(
            k => k.EndsWith("|true", StringComparison.Ordinal));
        var nichtWartende212 = formen[212].Keys.Count - wartende212;
        AssertTrue(wartende212 >= 13,
            "**and thirteen of this game's 212 wait for the animation to "
            + "finish** -- " + wartende212 + " forms do and "
            + nichtWartende212 + " do not, and a reader that waited a fixed "
            + "number of frames would cut a long animation short and hold "
            + "a short one");
        AssertTrue(nichtWartende212 >= 13,
            "**and eighteen do not wait** -- " + nichtWartende212
            + ", and that is a fact about the game and not a rule");
    }

    /// <summary>
    /// <c>313</c>'s third parameter is a direction and not a number.
    /// </summary>
    public void Test_DerDritteWertVon313IstDieRichtung()
    {
        var fakten = new MzBranchFacts();
        fakten.PartyMembers.Add(1);
        fakten.PartyMembers.Add(2);
        fakten.PartyMembers.Add(3);

        // Zwei Befehle: einer fuegt hinzu, einer nimmt weg.
        var interp = new MzInterpreter(new List<MzCommandEntry>
        {
            Befehl(MzCommandTable.ChangeActorState, "0", "2", "0", "25"),
            Befehl(MzCommandTable.ChangeActorState, "0", "2", "1", "25"),
        });
        interp.Setup(0, 0);
        interp.Run(new List<MzAction>(), fakten);

        AssertEq(fakten.States.Count, 6,
            "**and every actor of the party gets it and loses it** -- "
            + fakten.States.Count + ", and `iterateActorEx(params[0], "
            + "params[1])` with a true second parameter walks the whole "
            + "party and not one actor");
        AssertTrue(fakten.States.Take(3).All(s => s.Added && s.State == 25),
            "**and the first command adds it**");
        AssertTrue(fakten.States.Skip(3).All(s => !s.Added && s.State == 25),
            "**and the second takes it away** -- and `params[2] === 0` is "
            + "the add and anything else is the take, and a reader that "
            + "read it as a number would add on every value above zero "
            + "never");
        AssertEq(fakten.States.Count,
            fakten.States.Select(s => s.Actor).Distinct().Count() * 2,
            "**and the two are not merged into one** -- and the same state "
            + "on the same actor twice is two commands and not one");
    }

    /// <summary>
    /// <c>224</c> is <c>223</c> with a colour, and it steps the same way.
    /// </summary>
    public void Test_DerBlitzGehtGenausoWieDieEinfaerbung()
    {
        var fakten = new MzBranchFacts();
        var interp = new MzInterpreter(new List<MzCommandEntry>
        {
            Befehl(MzCommandTable.ScreenFlash,
                "[255,255,255,119]", "60", "false"),
        });
        interp.Setup(0, 0);
        interp.Run(new List<MzAction>(), fakten);

        AssertEq(fakten.Screen.TargetFlash[0], 255,
            "**and the colour comes out of the list** -- "
            + fakten.Screen.TargetFlash[0]);
        AssertEq(fakten.Screen.TargetFlash[3], 119,
            "**and the fourth number is the strength and not an alpha** -- "
            + fakten.Screen.TargetFlash[3]);
        AssertEq(fakten.Screen.Flash[0], 0,
            "**and the screen is not lit yet**");
        AssertEq(fakten.Screen.FlashDuration, 60,
            "**and it has the sixty frames the game asked for**");
        AssertTrue(fakten.Screen.FlashIsMoving,
            "**and a wait would ask about it**");

        // Und vier Bilder, wie die Engine sie macht.
        var bild = new MzScreen();
        bild.StarteBlitz(new int[] { 255, 255, 255, 119 }, 4);
        var erwartet = new List<int>();
        var wert = 0;
        var dauer = 4;
        while (dauer > 0)
        {
            wert = (wert * (dauer - 1) + 255) / dauer;
            erwartet.Add(wert);
            dauer--;
        }

        var gelesen = new List<int>();
        while (bild.FlashDuration > 0 && gelesen.Count < 50)
        {
            bild.TickBlitz();
            gelesen.Add(bild.Flash[0]);
        }

        for (var i = 0; i < erwartet.Count; i++)
        {
            AssertEq(gelesen[i], erwartet[i],
                "**and frame " + i + " is the engine's own value**");
        }
    }

    /// <summary>
    /// <c>212</c> names a character and not an actor.
    /// </summary>
    public void Test_DerErsteWertVon212IstEinDarsteller()
    {
        var fakten = new MzBranchFacts();
        fakten.Characters[0] = new MzCharacter { EventId = 0 };
        fakten.Characters[-1] = new MzCharacter { EventId = 0 };

        var interp = new MzInterpreter(new List<MzCommandEntry>
        {
            Befehl(MzCommandTable.ShowAnimation2, "0", "157", "false"),
            Befehl(MzCommandTable.ShowAnimation2, "-1", "182", "false"),
            // **Und ein Darsteller, den es nicht gibt.**
            Befehl(MzCommandTable.ShowAnimation2, "-9", "3", "false"),
        });
        interp.Setup(0, 0);
        interp.Run(new List<MzAction>(), fakten);

        AssertEq(fakten.AnimationAsked.Count, 2,
            "**and two of the three ran** -- " + fakten.AnimationAsked.Count
            + ", and the third names a character that is not on the map, "
            + "which is what the engine's `if (this._character)` steps over "
            + "and is not an error");
        AssertTrue(fakten.AnimationAsked.Contains(0),
            "**and the first character got its animation**");
        AssertEq(fakten.Characters[0].AnimationFramesLeft, 0,
            "**and it has no frames** -- and that is honest: "
            + "`requestAnimation(157)` takes a number and the length is in "
            + "the project's own Animations.json, so no frame count is "
            + "invented here");
    }

    /// <summary>
    /// <c>128</c>'s <c>operateValue</c> starts one slot later than
    /// <c>125</c>'s.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the trap that makes a party's stock of plate go
    /// the wrong way</strong>, <strong>because <c>[150, 0, 0, 1, false]</c>
    /// read from zero is <em>add 150</em> and read from one is <em>add
    /// 1</em>.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieHundertachtundzwanzigBeginntBeiEins()
    {
        var fakten = new MzBranchFacts();

        // [150, 0, 0, 1, false]  -> add a constant 1 of armour 150
        var interp = new MzInterpreter(new List<MzCommandEntry>
        {
            Befehl(MzCommandTable.ChangeArmor, "150", "0", "0", "1",
                "false"),
        });
        interp.Setup(0, 0);
        interp.Run(new List<MzAction>(), fakten);
        AssertEq(fakten.Armors.GetValueOrDefault(150), 1,
            "**and one of armour 150** -- and a reader that started at zero "
            + "would have added a hundred and fifty");

        // Und dieselbe Form mit einem anderen Startwert:
        // [27, 1, 0, 2, false] -> take a constant 2 of armour 27
        fakten = new MzBranchFacts();
        fakten.Armors[27] = 5;
        interp = new MzInterpreter(new List<MzCommandEntry>
        {
            Befehl(MzCommandTable.ChangeArmor, "27", "1", "0", "2",
                "false"),
        });
        interp.Setup(0, 0);
        interp.Run(new List<MzAction>(), fakten);
        AssertEq(fakten.Armors.GetValueOrDefault(27), 3,
            "**and taking away works from the same reader** -- "
            + fakten.Armors.GetValueOrDefault(27) + ", and `operation === 0` "
            + "is the add and anything else is the take");

        // Und auf null loescht den Eintrag, das ist gainItems Regel.
        fakten = new MzBranchFacts();
        fakten.Armors[9] = 1;
        interp = new MzInterpreter(new List<MzCommandEntry>
        {
            Befehl(MzCommandTable.ChangeArmor, "9", "1", "0", "1",
                "false"),
        });
        interp.Setup(0, 0);
        interp.Run(new List<MzAction>(), fakten);
        AssertTrue(!fakten.Armors.ContainsKey(9),
            "**and a count that lands on zero is gone and not stored** -- "
            + "and `container[item.id] === 0` deletes the entry, and a "
            + "reader that kept a zero would answer `hasArmor` wrongly");
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
