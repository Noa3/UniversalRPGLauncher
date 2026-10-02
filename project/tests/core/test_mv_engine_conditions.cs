using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

using Godot;

using UniversalRPG.Tests.Framework;
using UniversalRPG.Web;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The two engine questions a game's script asks, and the answers a desktop
/// run has.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is not a JavaScript interpreter</strong>, <strong>and it
/// is not going to become one.</strong> <c>Utils.isMobileDevice</c> and
/// <c>Utils.isOptionValid</c> are two named functions in
/// <c>rpg_core.js</c> -- <strong>the engine's own file, which this
/// repository already reads for every other rule it follows.</strong>
/// </para>
/// <para>
/// <strong>And measured at <c>D:/Itch/sister/www</c>:</strong>
/// <c>Utils.isOptionValid("test")</c> 118 times and
/// <c>Utils.isMobileDevice()</c> 50 times in the game's files, <strong>and
/// 143 of its 4952 script conditions are exactly one of these two</strong>.
/// </para>
/// <para>
/// <strong>And a condition this class does not know is still refused</strong>,
/// <strong>because a reader that answered an unknown expression would be
/// claiming a result it did not compute.</strong>
/// </para>
/// </remarks>
public partial class TestMvEngineConditions : TestBase
{
    private const string Projekt = "D:/Itch/sister/www";

    /// <summary>
    /// Both come to false on a desktop, and that is the engine's own code.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And <c>isMobileDevice</c> is false because the first line is
    /// <c>typeof require === 'function' &amp;&amp; typeof process === 'object'</c>
    /// </strong> -- <strong>a desktop application.</strong> <strong>And a
    /// headless run on a Windows desktop is one.</strong>
    /// </para>
    /// </remarks>
    public void Test_BeideSindAufEinemDesktopFalsch()
    {
        AssertEq(MzEngineCondition.IsMobileDevice, false,
            "**and the platform is not a phone**");
        AssertEq(MzEngineCondition.Answer(
            "Utils.isMobileDevice()", out var a), false,
            "**and `Utils.isMobileDevice()` comes to false** -- it said '"
            + a + "'");
        AssertEq(MzEngineCondition.Answer(
            "!Utils.isMobileDevice()", out var b), true,
            "**and its negation comes to true** -- it said '" + b + "'");
        AssertEq(MzEngineCondition.Answer(
            "Utils.isOptionValid(\"test\")", out var c), false,
            "**and a launch option this repository was not given is not "
            + "set** -- it said '" + c + "'");
        AssertEq(MzEngineCondition.Answer(
            "!Utils.isOptionValid(\"test\")", out var d), true,
            "**and its negation comes to true** -- it said '" + d + "'");
    }

    /// <summary>
    /// A launch flag the caller knows is a fact and is answered.
    /// </summary>
    public void Test_EinGesetzterSchalterIstEineTatsache()
    {
        try
        {
            MzEngineCondition.LaunchOptions = new[] { "test" };
            AssertEq(MzEngineCondition.Answer(
                "Utils.isOptionValid(\"test\")", out var e), true,
                "**and a flag that was given is set** -- it said '" + e
                + "'");
            AssertEq(MzEngineCondition.Answer(
                "!Utils.isOptionValid(\"test\")", out var f), false,
                "**and its negation comes to false** -- it said '" + f + "'");
            AssertEq(MzEngineCondition.Answer(
                "Utils.isOptionValid(\"other\")", out var g), false,
                "**and a flag that was not given is not set** -- it said '"
                + g + "', and a reader that answered true to any name would "
                + "run every test branch of every game");
        }
        finally
        {
            MzEngineCondition.LaunchOptions = Array.Empty<string>();
        }

        AssertEq(MzEngineCondition.Answer(
            "Utils.isOptionValid(\"test\")", out var h), false,
            "**and the default is the empty set**");
    }

    /// <summary>
    /// Everything else is still refused.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the line that matters is the game's own:</strong>
    /// <c>Utils.isOptionValid("test") || $gameVariables.value(25) &gt; 50</c>
    /// <strong>is half an engine question and half the author's
    /// script</strong>, <strong>and a reader that answered the first half
    /// would be claiming a result it did not compute.</strong>
    /// </para>
    /// </remarks>
    public void Test_AllesAndereBleibtVerweigert()
    {
        foreach (var (text, warum) in new[]
        {
            ("Utils.isOptionValid(\"test\") || $gameVariables.value(25) > 50",
                "half engine, half author"),
            ("Utils.isMobileDevice() || $gameMessage.isBusy()", "an `||`"),
            ("($gameSelfSwitches.value([1, 14, 'A']) && Math.random() > 0.7)"
                + " || Utils.isOptionValid(\"test\")", "a parenthesised left side"),
            ("$gameMap.chahuiMapTemp?.alreadyBribed && "
                + "!Utils.isOptionValid(\"test\")", "an `&&` on the left"),
            ("$gameVariables.value(1) > 5", "no engine question at all"),
            ("", "empty"),
            ("Utils.isMobileDevice(1)", "an argument it does not take"),
            ("Utils.isOptionValid(test)", "an argument that is not quoted"),
            ("Utils.isOptionValid(\"a\" && \"b\")", "an argument with an `&&`"),
            ("SceneManager.goto(Scene_Title)", "somebody else's call"),
        })
        {
            var antwort = MzEngineCondition.Answer(text, out var fehlt);
            AssertTrue(antwort == null,
                "**and '" + text + "' is not answered** -- it said '"
                + fehlt + "', and that one is " + warum + " and not a "
                + "question this reader knows");
            AssertTrue(fehlt.Length > 0,
                "**and why is said** -- and a refusal that does not say what "
                + "it would need is a refusal nobody can act on");
        }
    }

    /// <summary>
    /// The two questions out of the game's own files, counted.
    /// </summary>
    public void Test_DieHaeufigstenSkriptBedingungenDesSpiels()
    {
        if (!System.IO.Directory.Exists(Projekt + "/data"))
        {
            GD.Print("    (skipped: no " + Projekt + ")");
            return;
        }

        var beantwortbar = 0;
        var alle = 0;
        var nicht = new Dictionary<string, int>();
        foreach (var datei in System.IO.Directory
            .GetFiles(Projekt + "/data", "*.json",
                System.IO.SearchOption.AllDirectories))
        {
            var name = System.IO.Path.GetFileNameWithoutExtension(datei);
            var istKarte = name.StartsWith("Map", StringComparison.Ordinal)
                && name.Length > 3
                && name.Substring(3).All(char.IsDigit);
            if (!istKarte && name != "CommonEvents")
            {
                continue;
            }

            foreach (var ausdruck in SkriptBedingungen(datei))
            {
                alle++;
                if (MzEngineCondition.Answer(ausdruck, out _).HasValue)
                {
                    beantwortbar++;
                }
                else
                {
                    nicht.TryGetValue(ausdruck, out var n);
                    nicht[ausdruck] = n + 1;
                }
            }
        }

        System.Console.WriteLine(
            "MV Skript-Bedingungen: " + alle + " insgesamt, "
            + beantwortbar + " davon beantwortbar ("
            + (alle > 0 ? beantwortbar * 100 / alle : 0) + "%)");
        AssertTrue(alle > 4000,
            "**and this game has more than four thousand script "
            + "conditions** -- " + alle);
        AssertTrue(beantwortbar >= 140,
            "**and a hundred and forty of them are one of the two engine "
            + "questions** -- " + beantwortbar + ", and that is the "
            + "number this reader can now answer where it could answer "
            + "none");
    }

    /// <summary>
    /// The evaluator's own script case reaches the engine's two questions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the link that the run depends on and that no
    /// other test proves</strong>: <c>MzBranchEvaluator.Evaluate</c> must
    /// hand <c>!Utils.isMobileDevice()</c> to
    /// <c>MzEngineCondition.Answer</c> <strong>and come back with true</strong>,
    /// <strong>and not with a refusal that reads the same from the
    /// outside.</strong>
    /// </para>
    /// </remarks>
    public void Test_DerAuswerterReichtDieFrageWeiter()
    {
        var fakten = new MzBranchFacts();
        foreach (var (text, erwartet) in new[]
        {
            ("!Utils.isMobileDevice()", true),
            ("Utils.isMobileDevice()", false),
            ("!Utils.isOptionValid(\"test\")", true),
            ("Utils.isOptionValid(\"test\")", false),
        })
        {
            var zweig = MzBranch.FromParameters(
                new List<string> { "12", text });
            var ergebnis = MzBranchEvaluator.Evaluate(zweig, fakten);
            AssertEq(ergebnis.Outcome,
                erwartet ? MzBranchOutcome.True : MzBranchOutcome.False,
                "**and '" + text + "' comes to " + (erwartet ? "true"
                    : "false") + " through the evaluator** -- it came to "
                    + ergebnis.Outcome + " and said '" + ergebnis.Missing
                    + "', and a refusal here would look the same from "
                    + "outside as the refusal it replaced");
        }

        // **Und die Kette vom Parameter bis zur Antwort**, **denn
        // `ScriptText` ist eine eigene Eigenschaft und nicht
        // `Parameters[1]`, und ein Leser, der die falsche von beiden
        // nimmt, sieht fuer jede Bedingung einen leeren Text.**
        var roh = MzBranch.FromParameters(
            new List<string> { "12", "!Utils.isMobileDevice()" });
        AssertEq(roh.Kind, MzBranchKind.Script,
            "**and a condition of type twelve is a script**");
        AssertEq(roh.ScriptText, "!Utils.isMobileDevice()",
            "**and the text arrives** -- it arrived as '"
            + roh.ScriptText + "'");
        AssertEq(MzEngineCondition.Answer(
            roh.ScriptText, out var _weg).Value, true,
            "**and that text is answered** -- it said '" + _weg + "'");

        // Und was nicht beantwortbar ist, bleibt eine Verweigerung.
        var fremd = MzBranch.FromParameters(
            new List<string> { "12", "$gameVariables.value(1) > 5" });
        var r = MzBranchEvaluator.Evaluate(fremd, fakten);
        // **Und es ist `ScriptNotRun` und nicht `Unknown`** -- **denn es
        // fehlt nichts, es wird nur nichts gerechnet**, **und `Unknown`
        // wuerde sagen, da sei etwas, das dieser Leser noch nicht
        // hat.**
        AssertEq(r.Outcome, MzBranchOutcome.ScriptNotRun,
            "**and the author's own script is still a refusal** -- it came "
            + "to " + r.Outcome + ", and a reader that answered it would "
            + "be claiming a result it did not compute");
        AssertEq(r.Missing,
            "the author's own script, and this repository runs no JavaScript",
            "**and what it would need is said**");
    }

    private static IEnumerable<string> SkriptBedingungen(string pPfad)
    {
        var wurzel = JsonDocument.Parse(
            System.IO.File.ReadAllText(pPfad)).RootElement;
        if (wurzel.ValueKind == JsonValueKind.Array)
        {
            foreach (var eintrag in wurzel.EnumerateArray())
            {
                if (eintrag.ValueKind == JsonValueKind.Object
                    && eintrag.TryGetProperty("list", out var liste))
                {
                    foreach (var ausdruck in Aus(liste))
                    {
                        yield return ausdruck;
                    }
                }
            }

            yield break;
        }

        if (!wurzel.TryGetProperty("events", out var events))
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
                    foreach (var ausdruck in Aus(liste))
                    {
                        yield return ausdruck;
                    }
                }
            }
        }
    }

    private static IEnumerable<string> Aus(JsonElement pListe)
    {
        foreach (var befehl in pListe.EnumerateArray())
        {
            if (befehl.ValueKind != JsonValueKind.Object
                || !befehl.TryGetProperty("code", out var roh)
                || !roh.TryGetInt32(out var code) || code != 111
                || !befehl.TryGetProperty("parameters", out var ps)
                || ps.GetArrayLength() < 2)
            {
                continue;
            }

            if (ps[0].ValueKind == JsonValueKind.Number
                && ps[0].GetInt32() == 12
                && ps[1].ValueKind == JsonValueKind.String)
            {
                yield return ps[1].GetString() ?? "";
            }
        }
    }
}
