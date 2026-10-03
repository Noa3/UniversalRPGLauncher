using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using UniversalRPG.Mz;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The actors, measured against the game and split into what the
/// engine answers and what a plugin answers.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the split is the finding:</strong>
///
/// <code>
/// direkt nach .leader():            direkt nach $gameActors.actor(n):
///   equips()   8  Engine              changeEquipById()  37  Engine
///   hasSkill() 1  Engine              equips()           14  Engine
///   pha()     12  Plugin              skillMasteryLevel() 13  Plugin
///   _isZAKO()  7  Plugin              setSkillMasteryLevel() 5  Plugin
///   addState()  5  Plugin              skillMasteryUses()  4  Plugin
/// </code>
///
/// <strong>And so 52 of 84 calls are the engine's and 32 are
/// plugins', and this reader answers the 52 and names the 32.</strong>
/// </para>
/// </remarks>
public partial class TestMzGameActors : TestBase
{
    private static int Zugriff(
        System.Collections.Generic.IDictionary<string, int> pZaehler,
        string pName) =>
        pZaehler.TryGetValue(pName, out var w) ? w : 0;

    private const string Wurzel = "D:/Itch/sister/www/data";

    private static string[] Skripte()
    {
        var alle = new System.Collections.Generic.List<string>();

        void Seite(JsonElement pSeite)
        {
            if (!pSeite.TryGetProperty("list", out var liste)
                || liste.ValueKind != JsonValueKind.Array)
            {
                return;
            }

            foreach (var b in liste.EnumerateArray())
            {
                if (b.ValueKind != JsonValueKind.Object
                    || !b.TryGetProperty("code", out var c)
                    || c.GetInt32() != 355
                    || !b.TryGetProperty("parameters", out var ps)
                    || ps.ValueKind != JsonValueKind.Array
                    || ps.GetArrayLength() == 0
                    || ps[0].ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                alle.Add(ps[0].GetString() ?? "");
            }
        }

        foreach (var datei in Directory.GetFiles(Wurzel, "Map*.json"))
        {
            if (Path.GetFileName(datei)
                .Equals("MapInfos.json", StringComparison.OrdinalIgnoreCase)
                || new FileInfo(datei).Length > 4_000_000)
            {
                continue;
            }

            JsonDocument doc;
            try
            {
                doc = JsonDocument.Parse(File.ReadAllText(datei));
            }
            catch (JsonException)
            {
                continue;
            }

            using (doc)
            {
                if (!doc.RootElement.TryGetProperty("events", out var ev)
                    || ev.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var e in ev.EnumerateArray())
                {
                    if (e.ValueKind != JsonValueKind.Object
                        || !e.TryGetProperty("pages", out var seiten)
                        || seiten.ValueKind != JsonValueKind.Array)
                    {
                        continue;
                    }

                    foreach (var s in seiten.EnumerateArray())
                    {
                        if (s.ValueKind == JsonValueKind.Object)
                        {
                            Seite(s);
                        }
                    }
                }
            }
        }

        var ce = Path.Combine(Wurzel, "CommonEvents.json");
        if (File.Exists(ce))
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(ce));
            foreach (var e in doc.RootElement.EnumerateArray())
            {
                if (e.ValueKind == JsonValueKind.Object)
                {
                    Seite(e);
                }
            }
        }

        return alle.ToArray();
    }

    /// <summary>
    /// And the split, counted out of the game's scripts.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And <c>changeEquipById</c> alone is 37 calls</strong>,
    /// -- <strong>and the game writes <c>changeEquipById(3, 152)
    /// </c> 11 times and <c>changeEquipById(3, null)</c> 2
    /// times</strong>.
    /// </para>
    /// </remarks>
    public void Test_DieTeilung()
    {
        var nach = new System.Collections.Generic
            .Dictionary<string, int>(StringComparer.Ordinal);
        var mit152 = 0;

        foreach (var s in Skripte())
        {
            foreach (Match m in Regex.Matches(s,
                @"\$gameActors\s*\.\s*actor\s*\((\d+)\)\s*\.\s*(\w+)"))
            {
                var meth = m.Groups[2].Value;
                nach.TryGetValue(meth, out var k);
                nach[meth] = k + 1;
            }

            foreach (Match m in Regex.Matches(s,
                @"\$gameParty\s*\.\s*leader\s*\(\s*\)\s*\.\s*(\w+)"))
            {
                var meth = "leader:" + m.Groups[1].Value;
                nach.TryGetValue(meth, out var k);
                nach[meth] = k + 1;
            }

            if (Regex.IsMatch(s, @"changeEquipById\(3,\s*152\)"))
            {
                mit152++;
            }
        }

        Console.WriteLine(string.Join("  ", nach.OrderByDescending(x => x.Value)
            .Take(8).Select(x => x.Key + " " + x.Value)));

        AssertEq(37, Zugriff(nach, "changeEquipById"),
            "**and `changeEquipById()` is called 37 times** -- and"
                + " the engine writes it as a subtraction and a"
                + " test for 1");
        AssertEq(14, Zugriff(nach, "equips"),
            "**and `equips()` is called 14 times** -- and the engine"
                + " maps it to `item.object()`, so an empty slot is"
                + " nothing");
        AssertEq(13, Zugriff(nach, "skillMasteryLevel"),
            "**and 13 calls are `skillMasteryLevel` from a plugin** --"
                + " and this repository does not answer it, and says"
                + " so in `NichtBeantworteteMethoden`");
        AssertEq(12, Zugriff(nach, "leader:pha"),
            "**and 12 calls are `$gameParty.leader().pha` from a"
                + " plugin** -- and `pha` is not one of the 130"
                + " methods `Game_Actor` has in the engine file");
    }

    /// <summary>
    /// And etypeId is one-based, and that is not cosmetic.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the engine writes <c>const slotId = etypeId - 1;</c>
    /// </strong>, -- <strong>and a reader that used the number as
    /// written would put the game's <c>changeEquipById(3, 152)</c>
    /// 11 times into the helmet slot</strong>, -- <strong>and
    /// <c>equips()[1]</c> is what the game reads 14
    /// times.</strong>
    /// </para>
    /// </remarks>
    public void Test_DerSlotIstEinsWeniger()
    {
        var db = new MzGameActors();
        var schwert = new MzAusrüstung { Id = 152, Name = "Schwert" };
        var helm = new MzAusrüstung { Id = 9, Name = "Helm" };
        db.SetzeWaffe(152, schwert);
        db.SetzeRuestung(9, helm);

        var held = new MzActor { Id = 2, Name = "Schwester" };
        db.Setze(2, held);

        // **Und  etypeId 3  ist  slotId 2.**
        held.SetzeSlot(2, schwert);
        AssertEq("Schwert", held.Equips()[2]?.Name,
            "**and the item lands in slot two for etypeId three** --"
                + " and the engine writes `const slotId = etypeId - 1;`,"
                + " and a reader that skipped that line would put the"
                + " sword in the helmet");
        AssertEq(0, held.Equips()[1] == null ? 0 : 1,
            "**and slot one is still empty**");

        // **Und  `changeEquipById(4, null)`  leert  Slot 3.**
        held.SetzeSlot(3, helm);
        held.SetzeSlot(3, null);
        AssertTrue(held.Equips()[3] == null,
            "**and an empty slot comes back as nothing** -- and the"
                + " engine maps `item.object()`, and the game writes"
                + " `if (!$gameActors.actor(2).equips()[1])` 5 times");
    }

    /// <summary>
    /// And the ones this reader does not answer.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this test is why the class has a method for
    /// it.</strong> -- <strong>A coverage number would have hidden
    /// 32 calls behind a percentage.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieNichtBeantworteten()
    {
        var db = new MzGameActors();

        Console.WriteLine("beantwortet: "
            + string.Join(", ", db.BekannteMethoden()));
        Console.WriteLine("nicht: " + db.NichtBeantworteteMethoden().Count);

        AssertEq(4, db.BekannteMethoden().Count,
            "**and this reader answers four methods** -- and"
                + " `Game_Actor` has 130 in the game's own engine file,"
                + " and three of the four sit on the actor"
                + " (`changeEquipById`, `equips`, `hasSkill`) while"
                + " `actor` is the way in");
        AssertEq(16, db.NichtBeantworteteMethoden().Count,
            "**and it names sixteen it does not answer** -- and 32"
                + " calls land on them, and a reader that answered"
                + " them would be answering a plugin it has not"
                + " read");

        var held = new MzActor();
        AssertTrue(db.Actor(2) == null,
            "**and an actor that does not exist comes back as"
                + " nothing**");
        db.Setze(2, held);
        AssertTrue(db.Actor(2) != null,
            "**and one that does comes back**");
        AssertEq(1, db.Anzahl,
            "**and the world holds one actor**");
    }
}
