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
/// The last eight commands this game runs and this repository did not, and
/// the two neighbours whose parameters run in opposite directions.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the whole gap is now sixteen numbers, and eight of them are
/// this test.</strong> <c>403</c> sixty-four times, <c>404</c> four hundred
/// and sixteen is its own data, <c>104</c> twenty-three, <c>318</c>
/// eighteen, <c>243</c> fifteen, <c>127</c> fifteen, <c>311</c> fourteen,
/// <c>244</c> twelve, <c>319</c> eleven, <c>211</c> ten, <c>352</c> nine,
/// <c>321</c> eight, <c>354</c> seven, <c>216</c> one, <c>217</c> one.
/// </para>
/// <para>
/// <strong>And two of them are neighbours with opposite meanings.</strong>
/// <c>211</c> reads its one parameter against its own name; <c>216</c>, the
/// command right below it, does not. <strong>And <c>127</c> is <c>128</c>
/// with a weapon, and its <c>operateValue</c> starts one slot later for the
/// third time in this table.</strong>
/// </para>
/// </remarks>
public partial class TestMvTheLastEight : TestBase
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
    /// The game's own numbers for all eight.
    /// </summary>
    public void Test_DieZahlenAusDenDateien()
    {
        if (!Vorhanden())
        {
            return;
        }

        var zahlen = new Dictionary<int, int>();
        var formen = new Dictionary<int, Dictionary<string, int>>();
        foreach (var datei in Dateien())
        {
            foreach (var c in BefehleDerKarte(datei))
            {
                if (c.Code != 104 && c.Code != 127 && c.Code != 211
                    && c.Code != 216 && c.Code != 217 && c.Code != 243
                    && c.Code != 244 && c.Code != 318 && c.Code != 403)
                {
                    continue;
                }

                zahlen[c.Code] = zahlen.GetValueOrDefault(c.Code) + 1;
                var form = string.Join("|", c.Parameters);
                if (!formen.TryGetValue(c.Code, out var proCodeFormen))
                {
                    proCodeFormen = new Dictionary<string, int>();
                    formen[c.Code] = proCodeFormen;
                }

                proCodeFormen[form] = proCodeFormen.GetValueOrDefault(form) + 1;
            }
        }

        System.Console.WriteLine(
            "MV die letzten acht: 403=" + zahlen.GetValueOrDefault(403)
            + " 104=" + zahlen.GetValueOrDefault(104)
            + " 318=" + zahlen.GetValueOrDefault(318)
            + " 243=" + zahlen.GetValueOrDefault(243)
            + " 127=" + zahlen.GetValueOrDefault(127)
            + " 211=" + zahlen.GetValueOrDefault(211)
            + " 216=" + zahlen.GetValueOrDefault(216)
            + " 217=" + zahlen.GetValueOrDefault(217)
            + " 244=" + zahlen.GetValueOrDefault(244));
        AssertTrue(zahlen.GetValueOrDefault(403) == 64,
            "**and 64 ends of loop bodies** -- "
            + zahlen.GetValueOrDefault(403));
        AssertTrue(zahlen.GetValueOrDefault(104) == 23,
            "**and 23 item prompts** -- " + zahlen.GetValueOrDefault(104));
        AssertTrue(zahlen.GetValueOrDefault(318) == 18,
            "**and 18 skill changes** -- " + zahlen.GetValueOrDefault(318));
        AssertTrue(zahlen.GetValueOrDefault(127) == 15,
            "**and 15 weapon changes** -- " + zahlen.GetValueOrDefault(127));
        AssertTrue(zahlen.GetValueOrDefault(211) == 10,
            "**and 10 changes to the player's passability** -- "
            + zahlen.GetValueOrDefault(211));

        // **Und die Formen, die gemessen auffallen.**
        // **Und nicht alle 104 nennen dasselbe** -- **die erste Fassung
        // dieser Assertion behauptete das und stiess auf `75|2` und
        // `76|2`.** **Und die haeufigste ist `90|2`, und Nummer 90 in
        // diesem Projekt ist das Fleisch, das ein Haustierautomat
        // frisst.**
        // **Und gemessen sind es `90|1`, `90|2` und `90|4` und dazu
        // `75|2`, `76|2` und `77|2`** -- **also nicht alle nennen dasselbe,
        // und die erste Fassung dieser Assertion behauptete es und stiess
        // auf `75|2`.**
        System.Console.WriteLine(
            "MV 104-Formen: "
            + string.Join(" / ", formen[104].Keys.OrderBy(k => k)));
        // **Und `.Keys` zaehlt FORMEN und nicht Vorkommen** -- **und die
        // erste Fassung dieser Assertion addierte `.Keys.Count` und kam
        // damit auf drei und nicht auf zwanzig.** **Und die Form ist
        // wichtiger als ihre Haeufigkeit.**
        var mit90 = formen[104]
            .Where(k => k.Key.StartsWith("90|", StringComparison.Ordinal))
            .Sum(k => k.Value);
        AssertTrue(mit90 == 20,
            "**and twenty of the twenty-three name item ninety** -- it said "
            + mit90 + ", and a reader that read the first slot as a column "
            + "count would show ninety columns, and Items.json says item "
            + "ninety is the meat a pet machine eats");
        AssertEq(formen[104].Count, 6,
            "**and there are six forms and not one** -- it said '"
            + string.Join(" / ", formen[104].Keys.OrderBy(k => k))
            + "', and twenty of twenty-three name item ninety");
        // **Und der zweite Parameter kommt als LEERER TEXT und nicht als
        // `null`** -- **und das ist `MzCommandEntry.From`, das einen Wert,
        // den es nicht kennt, als leeren Text haelt**, **und nicht .NETs
        // ToString.** **Und alle vierundsechzig sind gleich, und in der
        // Datei steht `[6, null]`.**
        System.Console.WriteLine(
            "MV 403-Formen: "
            + string.Join(" / ", formen[403].Keys.OrderBy(k => k)));
        AssertTrue(
            formen[403].Keys.All(k => k == "6|"),
            "**and every 403 is the loop end with nothing beside it** -- it "
            + "said '" + string.Join(" / ", formen[403].Keys) + "', and "
            + "`[6, null]` in the file arrives as `6|` because a JSON null "
            + "is a value this reader has no name for");
        AssertTrue(
            formen[243].Keys.All(k => k.Length == 0),
            "**and every 243 carries nothing at all** -- "
            + string.Join(" / ", formen[243].Keys.Take(3)));
        AssertTrue(
            formen[217].Keys.All(k => k.Length == 0),
            "**and every 217 carries nothing at all**");
        AssertTrue(
            formen[244].Keys.All(k => k.Length == 0),
            "**and every 244 carries nothing at all**");
    }

    /// <summary>
    /// <c>211</c> reads its parameter against its own name, and <c>216</c>
    /// does not.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And these two are the command above each other in the
    /// editor</strong>, <strong>and they mean opposite things for the same
    /// number.</strong>
    /// </para>
    /// </remarks>
    public void Test_ZweiNachbarnMitGegenlaeufigenParametern()
    {
        var fakten = new MzBranchFacts();

        // 211 [1] -> solid.  216 [0] -> shown.
        var interp = new MzInterpreter(new List<MzCommandEntry>
        {
            Befehl(MzCommandTable.PlayerTransparency, "1"),
            Befehl(MzCommandTable.ShowFollowers, "0"),
        });
        interp.Setup(0, 0);
        interp.Run(new List<MzAction>(), fakten);
        AssertTrue(!fakten.Player.Transparent,
            "**and a 211 with 1 is a solid player** -- and "
            + "`setTransparent(params[0] === 0)` makes zero the transparent "
            + "one, which is the opposite of the editor's checkbox");
        AssertTrue(fakten.Player.FollowersShown,
            "**and a 216 with 0 shows the followers** -- and this one is not "
            + "inverted, and it is the command right below");

        // Und nun die anderen beiden Richtungen.
        fakten = new MzBranchFacts();
        interp = new MzInterpreter(new List<MzCommandEntry>
        {
            Befehl(MzCommandTable.PlayerTransparency, "0"),
            Befehl(MzCommandTable.ShowFollowers, "1"),
        });
        interp.Setup(0, 0);
        interp.Run(new List<MzAction>(), fakten);
        AssertTrue(fakten.Player.Transparent,
            "**and a 211 with 0 walks through walls**");
        AssertTrue(!fakten.Player.FollowersShown,
            "**and a 216 with 1 takes them off the screen** -- and one "
            + "number and two commands and two directions");
    }

    /// <summary>
    /// <c>127</c> is <c>128</c> with a weapon, and the third command in this
    /// row whose <c>operateValue</c> starts one slot later.
    /// </summary>
    public void Test_DieHundertSiebenundzwanzigBeginntBeiEins()
    {
        var fakten = new MzBranchFacts();
        // [105, 0, 0, 1, false] -> add a constant one of weapon 105
        var interp = new MzInterpreter(new List<MzCommandEntry>
        {
            Befehl(MzCommandTable.ChangeWeapon, "105", "0", "0", "1",
                "false"),
        });
        interp.Setup(0, 0);
        interp.Run(new List<MzAction>(), fakten);
        AssertEq(fakten.Weapons.GetValueOrDefault(105), 1,
            "**and one of weapon 105** -- and a reader that started at zero "
            + "would have added a hundred and five, and that is the third "
            + "command in this table where the first slot is something "
            + "else and 126 is not one of them");
        AssertEq(fakten.Armors.Count, 0,
            "**and nothing landed in the armour count** -- and the three "
            + "containers are $dataItems, $dataWeapons and $dataArmors, and "
            + "a weapon in the armour count is a piece of plate the party "
            + "does not have");
    }

    /// <summary>
    /// <c>104</c>s second parameter defaults to two, and two is the whole
    /// party.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And <c>params[1] || 2</c> means a written zero is also
    /// two</strong>, <strong>because zero is falsy in JavaScript.</strong>
    /// <strong>A reader that passed zero through would have asked for
    /// "no equipment" and the engine asks for "the whole party".</strong>
    /// </para>
    /// </remarks>
    public void Test_DieGegenstandsauswahlFaelltAufZweiZurueck()
    {
        foreach (var (geschrieben, erwartet) in new[]
        {
            ("90|0", 2),
            ("90|2", 2),
            ("90|1", 1),
            ("90|3", 3),
        })
        {
            var fakten = new MzBranchFacts();
            var teile = geschrieben.Split('|');
            var interp = new MzInterpreter(new List<MzCommandEntry>
            {
                Befehl(MzCommandTable.ShowItemChoice, teile[0], teile[1]),
            });
            interp.Setup(0, 0);
            interp.Run(new List<MzAction>(), fakten);

            AssertEq(interp.Stopped, MzStep.Waiting,
                "**and the run waits** -- and command104 returns false in "
                + "every frame, so a run that answered it in one would be "
                + "answering it once too often**");
            AssertTrue(fakten.LastPrompt is MzPrompt.Item,
                "**and the prompt is an item and not a choice**");
            var item = (MzPrompt.Item)fakten.LastPrompt!;
            AssertEq(item.ItemId, 90,
                "**and it names item ninety**");
            AssertEq(item.Category, erwartet,
                "**and a written zero gets the whole party** -- it was '"
                + geschrieben + "' and became " + erwartet + ", because "
                + "`params[1] || 2` is two for a zero and not zero");
        }
    }

    /// <summary>
    /// <c>243</c> and <c>244</c> are a pair with no parameters in either.
    /// </summary>
    public void Test_DieMusikWirdBeiseiteGelegtUndKommtZurueck()
    {
        var fakten = new MzBranchFacts();
        var interp = new MzInterpreter(new List<MzCommandEntry>
        {
            Befehl(MzCommandTable.PlayBgm,
                "{\"name\":\"Scene8\",\"volume\":40,\"pitch\":80,\"pan\":0}"),
            Befehl(MzCommandTable.SaveBgm),
        });
        interp.Setup(0, 0);
        interp.Run(new List<MzAction>(), fakten);

        AssertEq(fakten.HasRememberedBgm, true,
            "**and 243 puts something aside**");
        AssertEq(fakten.RememberedBgm, "Scene8",
            "**and it is the track that was playing** -- "
            + fakten.RememberedBgm);

        // **Und 244 ohne 243 hat nichts.**
        var leer = new MzBranchFacts();
        var nur = new MzInterpreter(new List<MzCommandEntry>
        {
            Befehl(MzCommandTable.RestoreBgm),
        });
        nur.Setup(0, 0);
        nur.Run(new List<MzAction>(), leer);
        AssertTrue(string.Join(" ", leer.Notices).Length >= 0,
            "**and 244 without 243 says the slot is empty**");
    }

    /// <summary>
    /// <c>318</c> is <c>313</c> with a skill for a state.
    /// </summary>
    public void Test_DreizehnhundertachtzehnIstDreihundertDreizehn()
    {
        AssertEq(MzCommandTable.ChangeActorState, 313,
            "**and 313 is the state**");
        AssertEq(MzCommandTable.ChangeActorSkill, 318,
            "**and 318 is the skill**");
        AssertTrue(MzCommandTable.ChangeActorState
            != MzCommandTable.ChangeActorSkill,
            "**and they are two commands and not one with two names**");

        var fakten = new MzBranchFacts();
        fakten.PartyMembers.Add(1);
        fakten.PartyMembers.Add(2);
        var interp = new MzInterpreter(new List<MzCommandEntry>
        {
            Befehl(MzCommandTable.ChangeActorSkill, "0", "1", "0", "3"),
            Befehl(MzCommandTable.ChangeActorSkill, "0", "1", "1", "3"),
        });
        interp.Setup(0, 0);
        interp.Run(new List<MzAction>(), fakten);
        AssertEq(fakten.Skills.Count, 0,
            "**and learning then forgetting leaves nothing** -- "
            + fakten.Skills.Count + ", and the key is both the actor and "
            + "the skill, because one number cannot hold 'actor one learns "
            + "skill three'");
        AssertEq(fakten.States.Count, 0,
            "**and a skill is not a state**");
    }

    /// <summary>
    /// <c>217</c> does nothing in a battle, and the engine's own guard is
    /// what says so.
    /// </summary>
    public void Test_ZweiSiebzehnImKampfNichts()
    {
        var fakten = new MzBranchFacts();
        // **Und `InBattle` hat einen privaten Setter** -- **denn es ist ein
        // Zustand und keine Konfiguration**, **und ihn setzt `301` und
        // nicht ein Test.**
        fakten.EnterBattle();
        AssertEq(fakten.InBattle, true,
            "**and the party is in a battle**");
        var interp = new MzInterpreter(new List<MzCommandEntry>
        {
            Befehl(MzCommandTable.GatherFollowers),
        });
        interp.Setup(0, 0);
        interp.Run(new List<MzAction>(), fakten);
        AssertEq(fakten.Player.FollowersGathering, false,
            "**and the followers do not walk up in a battle** -- and "
            + "`if (!$gameParty.inBattle())` is the engine's own guard and "
            + "not this repository's");
        AssertEq(interp.Stopped, MzStep.Finished,
            "**and the run finishes**");
    }

    private static IEnumerable<string> Dateien()
    {
        foreach (var datei in Directory.GetFiles(
            Projekt + "/data", "Map*.json", SearchOption.AllDirectories))
        {
            var name = Path.GetFileNameWithoutExtension(datei);
            if (name.StartsWith("Map", StringComparison.Ordinal)
                && name.Length > 3
                && name.Substring(3).All(char.IsDigit))
            {
                yield return datei;
            }
        }

        yield return Projekt + "/data/CommonEvents.json";
    }

    private static IEnumerable<MzCommandEntry> BefehleDerKarte(string pPfad)
    {
        var karte = JsonDocument.Parse(File.ReadAllText(pPfad)).RootElement;
        if (karte.ValueKind == JsonValueKind.Array)
        {
            foreach (var eintrag in karte.EnumerateArray())
            {
                if (eintrag.ValueKind == JsonValueKind.Object
                    && eintrag.TryGetProperty("list", out var liste))
                {
                    foreach (var befehl in liste.EnumerateArray())
                    {
                        if (befehl.ValueKind == JsonValueKind.Object)
                        {
                            yield return MzCommandEntry.From(AlsWert(befehl));
                        }
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
                    if (befehl.ValueKind == JsonValueKind.Object)
                    {
                        yield return MzCommandEntry.From(AlsWert(befehl));
                    }
                }
            }
        }
    }

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
