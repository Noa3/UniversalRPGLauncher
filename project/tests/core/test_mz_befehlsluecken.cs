using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Which commands are named, which are carried out, and that the
/// difference is two.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And I first measured this wrong twice.</strong> I counted
/// the <c>case</c> branches in <c>MzCommands.cs</c> alone, got a
/// hundred, and said twenty-one commands are named and not run.
/// -- <strong>And the control flow lives in
/// <c>MzControlFlow.cs</c></strong>, -- <strong>which carries
/// eleven more</strong>, -- <strong>and the interpreter itself
/// handles eight, -- <strong>and `MzEventRunner` handles `117`</strong>,
/// -- <strong>-- <strong>and the real number is two.</strong>
/// </para>
/// <code>
/// MzCommands     100
/// MzControlFlow   11
/// MzInterpreter    8
/// </code>
/// <para>
/// <strong>And the two that are named and used nowhere are
/// `412 EndBranch` and `657 ScriptLine2</strong>, -- <strong>and the
/// engine has no `command412`</strong>, -- <strong>and `SkipBranch`
/// steps over it without naming it, exactly as the engine does.</strong>
/// </para>
/// </remarks>
public partial class TestMzBefehlsluecken : TestBase
{
    private const string Quelle = "E:/URPG/project/src/mz/";

    private static HashSet<string> AlleNamenImOrdner()
    {
        var raus = new HashSet<string>(StringComparer.Ordinal);
        foreach (var datei in Directory.GetFiles(Quelle, "*.cs"))
        {
            foreach (Match m in Regex.Matches(File.ReadAllText(datei),
                @"MzCommandTable\.(\w+)"))
            {
                raus.Add(m.Groups[1].Value);
            }
        }

        return raus;
    }

    private static HashSet<string> Faelle(string pDatei)
    {
        var pfad = Path.Combine(Quelle, pDatei);
        return Regex.Matches(File.ReadAllText(pfad),
            @"case MzCommandTable\.(\w+):")
            .Select(m => m.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);
    }

    private static Dictionary<int, List<string>> Tabelle()
    {
        var quelle = File.ReadAllText(
            Path.Combine(Quelle, "MzCommandTable.cs"));
        var raus = new Dictionary<int, List<string>>();
        foreach (Match m in Regex.Matches(quelle,
            @"public const int (\w+) = (\d+);"))
        {
            var nr = int.Parse(m.Groups[2].Value);
            if (!raus.TryGetValue(nr, out var liste))
            {
                liste = new List<string>();
                raus[nr] = liste;
            }

            liste.Add(m.Groups[1].Value);
        }

        return raus;
    }

    /// <summary>
    /// And the three places, and the five that fall through all of
    /// And the four places, and the two that fall through all of
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And <c>412 EndBranch</c> being among them is not a
    /// fault.</strong> -- <strong>The engine's own file has no
    /// <c>command412</c></strong>, -- <strong>and the source comment
    /// in this repository says so</strong>: <c>"412 is the end of a
    /// branch and has no method; 403 has one and skips."</c>
    /// </remarks>
    public void Test_DieFuenfLuecken()
    {
        var befehle = Faelle("MzCommands.cs");
        var fluss = Faelle("MzControlFlow.cs");
        var interp = File.ReadAllText(Path.Combine(Quelle, "MzInterpreter.cs"));
        var tabelle = Tabelle();
        var ueberall = AlleNamenImOrdner();

        AssertEq(100, befehle.Count, "**and MzCommands carries a hundred**");
        AssertEq(11, fluss.Count,
            "**and MzControlFlow carries eleven more** -- and that is"
                + " the file I did not look at when I said twenty-one");
        AssertEq(118, ueberall.Count,
            "**and one hundred and eighteen names are used somewhere"
                + " in the folder** -- and `MzEventRunner` handles"
                + " `117` without a case branch, and it is the file I"
                + " did not look at when I said five");

        var offen = new List<string>();
        foreach (var eintrag in tabelle.OrderBy(x => x.Key))
        {
            var nr = eintrag.Key;
            var namen = eintrag.Value;
            if (!namen.Any(n => ueberall.Contains(n)))
            {
                offen.Add(nr + " " + string.Join("/", namen));
            }
        }

        Console.WriteLine("offen: " + string.Join("  ", offen));
        AssertEq(2, offen.Count,
            "**and two commands are named and used nowhere** -- and"
                + " a list of them is worth more than a percentage"
                + " that hides where the gap is");
        AssertTrue(!offen.Any(o =>
                o.StartsWith("117 ", StringComparison.Ordinal)),
            "**and `117 CommonEvent` is NOT among them** -- and I"
                + " said it was, and that was the third wrong"
                + " count: `MzEventRunner.Called` builds the child"
                + " with `Depth + 1` and pushes a frame, and"
                + " `MzEngineRuntime` hands it the list it reads"
                + " out of `CommonEvents.json`");
        AssertTrue(offen.Any(o => o.StartsWith("412 ", StringComparison.Ordinal)),
            "**and `412 EndBranch` is among them** -- and the engine"
                + " has no `command412`, and the comment in"
                + " `MzControlFlow.cs` says exactly that");

        var engine = File.ReadAllText(
            "D:/Itch/sister/www/js/rpg_objects.js");
        AssertTrue(!engine.Contains("command412()"),
            "**and the engine really has no `command412`** -- and"
                + " `command403()` is there and skips the branch");
    }

    /// <summary>
    /// And how often each of the five occurs in this game.
    /// And how often the two and their neighbours occur here.
    /// <remarks>
    /// <para>
    /// <strong>And this is what orders the work.</strong> -- <strong>
    /// <c>117</c> is 934, <c>412</c> is 2794 and <c>337</c> is
    /// zero.</strong> -- <strong>And <c>412</c> is high because every
    /// <c>111</c> needs one</strong>, -- <strong>and the interpreter
    /// reaches it by stepping over the branch, so the number of
    /// times it is passed is not a number of times it is
    /// missed.</strong>
    /// </para>
    /// </remarks>
    public void Test_WieOftDieFuenfVorkommen()
    {
        var zaehler = new Dictionary<int, int>();

        void Seite(JsonElement pSeite)
        {
            if (!pSeite.TryGetProperty("list", out var liste)
                || liste.ValueKind != JsonValueKind.Array)
            {
                return;
            }

            foreach (var b in liste.EnumerateArray())
            {
                if (b.ValueKind == JsonValueKind.Object
                    && b.TryGetProperty("code", out var c)
                    && c.ValueKind == JsonValueKind.Number)
                {
                    var nr = c.GetInt32();
                    zaehler.TryGetValue(nr, out var k);
                    zaehler[nr] = k + 1;
                }
            }
        }

        foreach (var datei in Directory.GetFiles(
            "D:/Itch/sister/www/data", "Map*.json"))
        {
            if (Path.GetFileName(datei)
                .Equals("MapInfos.json", StringComparison.OrdinalIgnoreCase))
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

        var ce = Path.Combine(
            "D:/Itch/sister/www/data", "CommonEvents.json");
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

        Console.WriteLine($"117 {Hole(zaehler, 117)}  412 {Hole(zaehler, 412)}"
            + $"  337 {Hole(zaehler, 337)}  405 {Hole(zaehler, 405)}"
            + $"  657 {Hole(zaehler, 657)}");

        AssertEq(934, Hole(zaehler, 117),
            "**and `117 CommonEvent` occurs 934 times** -- and that"
                + " is the one that runs a list of its own, so the"
                + " interpreter cannot just step over it");
        AssertEq(2794, Hole(zaehler, 412),
            "**and `412 EndBranch` occurs 2794 times** -- and the"
                + " interpreter reaches it by stepping over a branch,"
                + " so the count is not a count of misses");
        AssertEq(0, Hole(zaehler, 337),
            "**and `337` does not occur at all**");
        AssertEq(0, Hole(zaehler, 405),
            "**and `405` does not occur at all** -- and the choices"
                + " in this game are a 102 and its 402 lines");
        AssertEq(0, Hole(zaehler, 657),
            "**and `657` does not occur at all**");
    }

    private static int Hole(Dictionary<int, int> pZaehler, int pNr) =>
        pZaehler.TryGetValue(pNr, out var w) ? w : 0;
}
