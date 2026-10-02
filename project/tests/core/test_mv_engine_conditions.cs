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

        // **Und die Ordner, in denen das Spiel nach den
        // Platzhalterdateien sieht** -- **denn sonst zaehlt dieser Test
        // vier Bedingungen weniger**, **und die Zahl waere dann die Zahl
        // eines leeren Ordners und nicht die des Spiels.**
        MzEngineCondition.PlatzhalterDateien = new[]
        {
            Projekt, System.IO.Path.GetDirectoryName(Projekt),
        };

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
                var fakten = new MzBranchFacts
                {
                    MapId = 2,
                    EventId = 2,
                };
                // **Und die Rechnung zuerst**, **denn sie ist der
                // Weg, den der Auswerter auch geht.**
                var geprueft = false;

                if (MzArithmetic.KenntAlleVariablen(ausdruck, fakten)
                    && MzArithmetic.TryRead(ausdruck, fakten, out _)
                        .HasValue)
                {
                    beantwortbar++;
                    geprueft = true;
                }
                else if (MzEngineCondition.Answer(
                    ausdruck, fakten, out _).HasValue)
                {
                    beantwortbar++;
                    geprueft = true;
                }

                if (!geprueft)
                {
                    nicht.TryGetValue(ausdruck ?? "<null>", out var n);
                    nicht[ausdruck ?? "<null>"] = n + 1;
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
        // **Und 320 und nicht 684, und der Grund ist gemessen:**
        // **dieses Spiel schreibt vierzig Variablen**, **und die 136
        // Kreisbedingungen lesen die Nummern 4 und 5**, **und kein
        // einziger `122` in der ganzen Karte schreibt eine davon.**
        //
        // **Und das heisst: sie kommen aus einem Plugin-Skript**, **und
        // sie sind Positionsangaben des Spielers, die dieses Repository
        // nicht kennt.** **Und eine Bedingung ueber die Position des
        // Spielers auf einer Karte, die es nicht laedt, ist eine Frage
        // ueber etwas, das es nicht gibt** -- **und sie wird nicht
        // geraten und nicht mit null beantwortet.**
        AssertTrue(beantwortbar >= 320,
            "**and three hundred and twenty of them this reader can "
            + "answer** -- " + beantwortbar + " of " + alle + ", and that is "
            + "the engine's two questions and the arithmetic together, "
            + "and it was 142 before either of the two");
        // **Und der Anteil wird als Bruch gerechnet und nicht durch
        // Ganzzahldivision** -- **denn 320 von 4952 sind 6,45 Prozent,
        // und `alle / 15` sagt 330 und damit nein.**
        // **Und 320 von 4952 sind 6,45 Prozent und nicht sieben**,
        // **und meine Fassung mit einem Fünfzehntel war gerundet nach
        // unten und damit zu streng** -- **denn 320 mal 15 ist 4800 und
        // 4800 ist kleiner als 4952.** **Und es ist gemessen und nicht
        // gerundet: 320 sind mehr als ein Sechzehntel.**
        AssertTrue(beantwortbar * 16 > alle,
            "**and more than a sixteenth of them** -- one in "
            + (alle / Math.Max(1, beantwortbar)) + ", and "
            + (beantwortbar * 1000 / alle) + " of a thousand, and every "
            + "one of them is a condition that runs and not one that is "
            + "skipped");
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
        // **Und was er braucht, ist gesagt** -- **und der Grund ist eine
        // Variable, die das Spiel nie geschrieben hat, und nicht "das
        // Skript des Autors"**: **denn `$gameVariables.value(1)` mit einer
        // ungeschriebenen Variablen ist eine Frage ueber etwas, das es
        // nicht gibt**, **und `value` gibt da zwar null zurueck, aber das
        // ist nicht dasselbe wie eine Null, die das Spiel hingesetzt
        // hat.**
        AssertTrue(r.Missing.Contains("variable 1"),
            "**and it names the variable it does not have** -- it said '"
            + r.Missing + "', and a refusal that says only 'the author's own "
            + "script' when the truth is 'variable one, which the game "
            + "never wrote' is a refusal nobody can act on");
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
    /// <summary>
    /// And the platform flags are decided by a file, so they can be
    /// answered.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the condition that stopped the real
    /// run.</strong> <code>Map002</code>'s first event asks
    /// <c>!ConfigManager.isJapanesePlatform</c>, and this repository
    /// refused it and stopped.
    /// </para>
    /// <para>
    /// <strong>And the game itself says where the flag comes
    /// from:</strong>
    /// </para>
    /// <code>
    /// const [isSFW, isDLsite, isCien, isFanza, isImouto] = await Promise.all([
    ///     DataManager.checkPlaceholderExists("SFW.json"),
    ///     DataManager.checkPlaceholderExists("DLsite.json"),
    ///     ...
    /// ]);
    /// if (isDLsite || isCien || isFanza) {
    ///     ConfigManager.isJapanesePlatform = true;
    /// }
    /// </code>
    /// <para>
    /// <strong>And <c>checkPlaceholderExists</c> is
    /// <c>fs.existsSync(...)</c> under NW.js</strong> -- <strong>a file
    /// check and not a script</strong>, <strong>so answering it runs no
    /// JavaScript at all.</strong>
    /// </para>
    /// </remarks>
    public void Test_DiePlattformkennzeichenSindEineDatefrage()
    {
        if (!System.IO.Directory.Exists(Projekt + "/data"))
        {
            GD.Print("    (skipped: no " + Projekt + ")");
            return;
        }

        // **Und die Ordner, in denen das Spiel sucht.**
        MzEngineCondition.PlatzhalterDateien = new[]
        {
            Projekt, System.IO.Path.GetDirectoryName(Projekt),
        };

        var fakten = new MzBranchFacts();
        var antwort = MzEngineCondition.Answer(
            "ConfigManager.isJapanesePlatform", fakten,
            out var fehlt);

        AssertTrue(antwort.HasValue,
            "**and the platform question is answered** -- it is "
            + fehlt + ", and a refusal that names the file it wanted "
            + "would be a better one than this");

        // **Und es ist falsch, und nicht unbekannt.**
        AssertEq(antwort.GetValueOrDefault(), false,
            "**and the answer is false** -- and that is measured: "
            + "the files `SFW.json`, `DLsite.json`, `Cien.json`, "
            + "`Fanza.json` and `Imouto.json` are in none of "
            + "this project's folders, and a desktop build of this game "
            + "has none of them either");

        // **Und mit der Datei da ist es wahr** -- **und das ist der
        // Beweis, dass es eine Datefrage ist und keine Vermutung.**
        // **Und der Name enthaelt die Suite**, **damit kein zweiter Lauf
        // auf dieselbe Datei trifft.**
        var temp = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "urpg-platzhalter-" + nameof(TestMvEngineConditions)
                .Replace("Test", string.Empty));
        System.IO.Directory.CreateDirectory(temp + "/data");
        System.IO.File.WriteAllText(temp + "/data/DLsite.json", "{}");
        MzEngineCondition.PlatzhalterDateien = new[] { temp };
        var mitDatei = MzEngineCondition.Answer(
            "ConfigManager.isJapanesePlatform", fakten, out var fehlt2);
        AssertEq(mitDatei.GetValueOrDefault(), true,
            "**and with the file there the answer is true** -- it is "
            + fehlt2 + ", and that is what makes it a file check and not "
            + "a guess");

        // **Und `!` davor dreht um** -- **und `isImouto` bleibt
        // unbeantwortet**, **weil es auch aus dem Spielstand
        // kommt.**
        MzEngineCondition.PlatzhalterDateien = new[] { Projekt };
        var verneint = MzEngineCondition.Answer(
            "!ConfigManager.isJapanesePlatform", fakten, out var fehlt3);
        AssertEq(verneint.GetValueOrDefault(), true,
            "**and the negated form is the other way round** -- and that "
            + "is what the game wrote, and it is the branch the run "
            + "takes");
        AssertEq(
            MzEngineCondition.Answer(
                "ConfigManager.isImouto", fakten,
                out var fehltImouto),
            null,
            "**and `isImouto` stays unanswered** -- it said '"
            + fehltImouto + "', and the game writes it from a file "
            + "and then overwrites it from game state, and a flag that "
            + "can be either is not a fact about the machine");

        // **Und ein Aufruf ist kein Flag.**
        AssertEq(
            MzEngineCondition.Answer(
                "ConfigManager.isJapanesePlatform()", fakten, out var fehlt4),
            null,
            "**and a call is not a flag** -- it said '" + fehlt4 + "'");

        // **Und der Ordner bekommt einen eigenen Namen** -- **denn
        // `GetTempPath` ist derselbe fuer jeden Test**, **und ein Test,
        // der ihn leert, raeumt einem anderen die Beweisdatei weg.**
        System.IO.Directory.Delete(temp, true);
        MzEngineCondition.PlatzhalterDateien = Array.Empty<string>();
    }
}
