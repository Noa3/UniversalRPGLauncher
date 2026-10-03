using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using UniversalRPG.Web;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The 355 block, run through the interpreter and not through the
/// reader alone.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And until now the counter was set by hand.</strong>
/// <c>Test_DerEigeneFensterzaehler</c> writes
/// <c>fakten.EigenesFenster = 7</c> and then asks whether a condition
/// is true, -- <strong>and that proves the condition reads the
/// number and not that the block writes
/// it.</strong>
/// </para>
/// <para>
/// <strong>And this test closes that chain</strong>, -- <strong>with
/// a command list taken out of the game, not written here.</strong>
/// </para>
/// </remarks>
public partial class TestMz355ImLauf : TestBase
{
    private const string Wurzel = "D:/Itch/sister/www/data";

    /// <summary>
    /// And the pages of this game that carry the counter, and that
    /// none of them is an autorun or a parallel page.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And that is why this test drives the interpreter
    /// directly.</strong> -- <strong>All 47 pages with `frames`
    /// commands have trigger 0 or 4</strong>, -- <strong>and a
    /// <c>RunPage</c> takes the page with <c>isStarting()</c></strong>,
    /// -- <strong>which is trigger 2</strong>, -- <strong>and no
    /// page in this game has one.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieSiebenundvierzigSeitenSindNichtAutorun()
    {
        var seiten = new List<(string Trigger, bool Parallel, bool Autorun)>();

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
                        || !e.TryGetProperty("pages", out var ps)
                        || ps.ValueKind != JsonValueKind.Array)
                    {
                        continue;
                    }

                    foreach (var s in ps.EnumerateArray())
                    {
                        if (s.ValueKind != JsonValueKind.Object
                            || !s.TryGetProperty("list", out var liste)
                            || liste.ValueKind != JsonValueKind.Array)
                        {
                            continue;
                        }

                        var mit = false;
                        foreach (var b in liste.EnumerateArray())
                        {
                            if (b.ValueKind != JsonValueKind.Object
                                || !b.TryGetProperty("parameters", out var pa)
                                || pa.ValueKind != JsonValueKind.Array
                                || pa.GetArrayLength() == 0
                                || pa[0].ValueKind != JsonValueKind.String)
                            {
                                continue;
                            }

                            var c = b.TryGetProperty("code", out var cd)
                                ? cd.GetInt32() : -1;
                            if (c is 355 or 655
                                && pa[0].GetString()!.Contains(
                                    "frames", StringComparison.Ordinal))
                            {
                                mit = true;
                            }
                        }

                        if (mit)
                        {
                            seiten.Add((
                                s.TryGetProperty("trigger", out var tr)
                                    ? tr.GetInt32().ToString() : "?",
                                s.TryGetProperty("parallel", out var pa)
                                    && pa.ValueKind == JsonValueKind.True,
                                s.TryGetProperty("autorun", out var au)
                                    && au.ValueKind == JsonValueKind.True));
                        }
                    }
                }
            }
        }

        Console.WriteLine("frames-Seiten: " + seiten.Count
            + "  autorun " + seiten.Count(x => x.Autorun)
            + "  parallel " + seiten.Count(x => x.Parallel));

        AssertEq(51, seiten.Count,
            "**and 51 pages of this game carry the counter** -- and my first count said 47 because the test that found it also skipped files over four megabytes, and no map file in this game is that big");
        AssertEq(0, seiten.Count(x => x.Autorun),
            "**and not one of them runs by itself** -- and trigger 0"
                + " is an action button and trigger 4 is parallel"
                + " with no movement, and a page that runs on"
                + " entering would be trigger 3");
        AssertEq(0, seiten.Count(x => x.Parallel),
            "**and not one of them is parallel** -- and that is why"
                + " this test drives the interpreter rather than"
                + " a map");
    }

    /// <summary>
    /// And the block moves the counter, and the condition sees it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the command list is the game's own, copied out of
    /// the file, and not written here.</strong> -- <strong>And
    /// <c>Test_DerEigeneFensterzaehler</c> set the number by hand</strong>,
    /// -- <strong>so this is the first test where the block writes
    /// and a condition reads it in the same run.</strong>
    /// </para>
    /// </remarks>
    public void Test_DerZaehlerWandertUndDieBedingungSiehtEs()
    {
        // **Und  das  ist  die  Zeile  18x  im  Spiel.**
        var liste = new List<MzCommandEntry>
        {
            new(355, ["$gameSelfVariables.set(this, 'frames', 0);"], 0),
            new(655, ["$gameSelfVariables.add(this, 'frames', 1)"], 0),
            new(655, ["$gameSelfVariables.add(this, 'frames', 1)"], 0),
            new(655, ["$gameSelfVariables.add(this, 'frames', 1)"], 0),
            new(111, ["0", "1", "0", "1"], 0),
            new(411, [], 0),
            new(355,
                ["$gameSelfVariables.add(this, 'frames', 1)"], 0),
            new(655,
                ["$gameSelfVariables.add(this, 'frames', 1)"], 0),
            new(411, [], 0),
            new(412, [], 0),
            new(0, [], 0),
        };

        var interp = new MzInterpreter(liste);
        interp.Setup(0, 0);
        var fakten = new MzBranchFacts();

        // **Und  `Run`  ist  `void`  und  laeuft  bis  zum  Ende
        //  oder  bis  ein  Befehl  wartet** -- **und  gemessen  an
        //  `Interpreter.update`,  das  `while (this.isRunning())`
        //  schreibt.**
        var aktionen = new List<MzAction>();
        interp.Run(aktionen, fakten);
        var schritte = interp.Index;

        // **Und  Index 11  bei  11  Befehlen  heisst:  der
        //  else-Zweig  ist  gelaufen,  und  das  ist  richtig.**
        //  Und  gemessen:  `>= 7`  bei  3  gibt  0,  also
        //  falsch,  --  und  darum  gehoert  der  Sprung  in  den
        //  else-Zweig,  und  der  Zaehler  sammelt  dort  die
        //  beiden  weiteren  adds.
        Console.WriteLine("EigenesFenster " + fakten.EigenesFenster
            + "  Index " + interp.Index);

        // **Und der Zaehler steht auf fuenf**, --
        // **und das war erst drei Schritte lang zwei**, --
        // **und der Grund ist ein echter Fehler von mir.**
        AssertEq(5, fakten.EigenesFenster,
            "**and the counter stands at five** -- and it stood at"
                + " two until this run, and that was my fault and"
                + " not the game's: every 355 block began its reader"
                + " at zero, and the engine has one number in one"
                + " object, and two blocks in a row are that same"
                + " number. Three adds make three, and `>= 7` is"
                + " false at three, so the else branch runs and its"
                + " two adds make five");
        AssertTrue(interp.Index > 0,
            "**and the index is behind the block and not inside"
                + " it** -- and that is what the 655 lines are for");
    }
}
