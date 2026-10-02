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
/// The arithmetic in a finished game's script conditions, read out of the
/// game and not guessed.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is not a JavaScript interpreter.</strong> <b>It is a
/// grammar of eleven operators and one function call</b>, <strong>and it
/// refuses everything it does not know</strong> -- <b>because a reader
/// that answered an unknown expression would be claiming a result it did
/// not compute.</b>
/// </para>
/// <para>
/// <strong>And the grammar is measured, not chosen.</strong> Of the 449
/// conditions in this game that read only <c>$gameVariables.value</c> and
/// numbers, <b>364 are pure arithmetic</b> and use no other operator.
/// </para>
/// </remarks>
public partial class TestMvArithmeticConditions : TestBase
{
    private const string Projekt = "D:/Itch/sister/www";

    /// <summary>
    /// The circles, and the commonest shape the game writes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And <c>($gameVariables.value(4) - 1107) ** 2 +
    /// ($gameVariables.value(5) - 612) ** 2 &lt;= 113 ** 2</c> is a radius
    /// of 113 tiles around the point 1107, 612 on the map</strong>, <b>and
    /// it appears sixteen times.</b>
    /// </para>
    /// </remarks>
    public void Test_DerKreisUmEinenPunkt()
    {
        var fakten = new MzBranchFacts();
        fakten.Variables[4] = 1107;
        fakten.Variables[5] = 612;

        var auf = MzArithmetic.TryRead(
            "($gameVariables.value(4) - 1107) ** 2 "
            + "+ ($gameVariables.value(5) - 612) ** 2 <= 113 ** 2",
            fakten, out var grund);
        AssertEq(auf, 1.0,
            "**and standing on the point is inside the circle** -- it said "
            + auf + " and refused with '" + grund + "'");

        fakten.Variables[4] = 1107 + 113;
        fakten.Variables[5] = 612;
        AssertEq(MzArithmetic.TryRead(
            "($gameVariables.value(4) - 1107) ** 2 "
            + "+ ($gameVariables.value(5) - 612) ** 2 <= 113 ** 2",
            fakten, out var b), 1.0,
            "**and exactly on the rim is inside too** -- and `<=` is `<=`, "
            + "and it said " + b);

        fakten.Variables[4] = 1107 + 114;
        AssertEq(MzArithmetic.TryRead(
            "($gameVariables.value(4) - 1107) ** 2 "
            + "+ ($gameVariables.value(5) - 612) ** 2 <= 113 ** 2",
            fakten, out var c), 0.0,
            "**and one tile past the rim is out** -- and it said " + c);
    }

    /// <summary>
    /// <c>&amp;&amp;</c> and <c>||</c> bind the way JavaScript binds them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And in JavaScript <c>a || b</c> yields <c>a</c> when
    /// <c>a</c> is true and <c>b</c> when it is false</b>, <strong>and a
    /// reader that returned a plain <c>true</c>/<c>false</c> would be
    /// right about the condition and wrong about every expression inside
    /// it.</strong>
    /// </para>
    /// </remarks>
    public void Test_UndOderOderBindenWieJavaScript()
    {
        var fakten = new MzBranchFacts();
        fakten.Variables[15] = 6;
        fakten.Variables[18] = 0;

        // **Und `a || b` ergibt `a`, wenn `a` wahr ist, und `b`, wenn es
        // falsch ist** -- **und `a && b` ergibt `b`, wenn `a` wahr ist, und
        // `a` sonst.** **Und das ist der ganze Grund, warum diese Grammatik
        // Zahlen liefert und keine Wahrheitswerte.**
        AssertEq(MzArithmetic.TryRead(
            "$gameVariables.value(15) >= 3 "
            + "|| $gameVariables.value(18) >= 1",
            fakten, out var a), 1.0,
            "**and a true left side short-circuits** -- and it said " + a);
        AssertEq(MzArithmetic.TryRead(
            "$gameVariables.value(15) >= 9 "
            + "|| $gameVariables.value(18) >= 1",
            fakten, out var b), 0.0,
            "**and a false left side asks the right one** -- and it said "
            + b);
        AssertEq(MzArithmetic.TryRead(
            "$gameVariables.value(18) >= 3 "
            + "&& $gameVariables.value(15) >= 1",
            fakten, out var c), 0.0,
            "**and a false left side stops `&&`** -- and variable 18 is "
            + "zero, and `0 >= 3` is false, and it said " + c);

        // **Und meine erste Fassung dieses Tests schrieb hier eine Drei
        // hin und erwartete wahr** -- **und der Leser sagte zu Recht
        // falsch**, **und `&&` war nicht die Sache, die er falsch
        // machte.**

        fakten.Variables[18] = 4;
        AssertEq(MzArithmetic.TryRead(
            "$gameVariables.value(18) >= 3 "
            + "&& $gameVariables.value(15) >= 1",
            fakten, out var d), 1.0,
            "**and with a true left side it asks the right one** -- and it "
            + "said " + d);

        // **Und `(6 + 3 * x)` ist 6 + 3x und nicht (6+3)x** -- **denn `*`
        // bindet fester als `+` in JavaScript.**
        fakten.Variables[205] = 24;
        AssertEq(MzArithmetic.TryRead(
            "$gameVariables.value(205) >= (6 + 3 * $gameVariables.value(15))",
            fakten, out var e), 1.0,
            "**and `*` binds tighter than `+`** -- 6 + 3 * 6 is 24 and it "
            + "said " + e + ", and a reader that added first would want 24 "
            + ">= 36 and would take the other arm of the branch");
    }

    /// <summary>
    /// A variable the game never wrote is zero, and that is the engine's
    /// own line.
    /// </summary>
    public void Test_EineUngeschriebeneVariableIstNull()
    {
        var fakten = new MzBranchFacts();
        AssertEq(MzArithmetic.TryRead(
            "$gameVariables.value(9999) === 0", fakten, out var a), 1.0,
            "**and an unwritten variable is zero** -- "
            + "`Game_Variables.prototype.value` is "
            + "`return this._data[index] || 0;`, and it said " + a);
        // **Und `!0` ist WAHR und nicht falsch** -- **und meine erste
        // Fassung dieses Tests behauptete das Gegenteil**, **und der
        // Leser hatte recht und der Test nicht.**
        AssertEq(MzArithmetic.TryRead(
            "!$gameVariables.value(9999)", fakten, out var b), 1.0,
            "**and `!0` is true** -- and `!0` is `true` in JavaScript, and "
            + "a reader that made it false would step into the wrong arm of "
            + "every condition that negates a zero**");
    }

    /// <summary>
    /// A division by zero is not an error in JavaScript and is not one here.
    /// </summary>
    public void Test_EineDivisionDurchNullIstKeinFehler()
    {
        var fakten = new MzBranchFacts();
        fakten.Variables[1] = 5;
        var a = MzArithmetic.TryRead(
            "$gameVariables.value(1) / 0 > 1", fakten, out var m);
        AssertEq(a, 1.0,
            "**and five over zero is greater than one** -- and in "
            + "JavaScript that is `Infinity > 1`, and a reader that threw "
            + "would refuse a condition the engine answers");
        AssertEq(m, "",
            "**and nothing was refused**");
    }

    /// <summary>
    /// Everything outside the grammar is refused, and says what it is.
    /// </summary>
    public void Test_AllesAndereWirdVerweigert()
    {
        var fakten = new MzBranchFacts();
        foreach (var (text, weil) in new[]
        {
            ("$gameScreen.picture(4)", "another object"),
            ("Math.random() > 0.8", "a random number"),
            ("$gameActors.actor(2).equips()[1]", "another object"),
            ("$gameSelfVariables.get(this, 'Type')", "a plugin's store"),
            ("\"a\" === \"a\"", "a string"),
            ("x = 1", "an assignment"),
            ("true", "a bare name"),
            ("$gameVariables.value(1) ? 2 : 3", "a ternary"),
            ("$gameVariables.value(1) <= 2 $gameVariables.value(2)",
                "two sides and no operator"),
            ("$gameVariables.value(1) <=", "an operator with no right side"),
            ("($gameVariables.value(1) + 1", "an unclosed bracket"),
            ("", "empty"),
            ("$gameVariables.value(1) == 1 $gameVariables.value(2)",
                "no operator between two sides"),
        })
        {
            var antwort = MzArithmetic.TryRead(text, fakten, out var warum);
            AssertTrue(antwort == null,
                "**and '" + text + "' is not read** -- and it is " + weil);
            AssertTrue(warum.Length > 0,
                "**and why is said** -- and it said '" + warum + "'");
        }
    }

    /// <summary>
    /// Every pure-arithmetic condition in the game, read and none refused.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the number to compare against next time</strong>,
    /// <strong>and it is measured out of the game's own files.</strong>
    /// </para>
    /// </remarks>
    public void Test_JedeReineRechnungDesSpielsLaesstSichLesen()
    {
        if (!Directory.Exists(Projekt + "/data"))
        {
            GD.Print("    (skipped: no " + Projekt + ")");
            return;
        }

        var fakten = new MzBranchFacts();
        var gelesen = 0;
        var verweigert = new List<string>();
        foreach (var ausdruck in Rechnungen())
        {
            if (MzArithmetic.TryRead(ausdruck, fakten, out var warum)
                .HasValue)
            {
                gelesen++;
            }
            else
            {
                verweigert.Add(ausdruck + "  ->  " + warum);
            }
        }

        System.Console.WriteLine(
            "MV Rechnungen: " + gelesen + " gelesen, " + verweigert.Count
            + " verweigert");
        AssertTrue(gelesen >= 360,
            "**and three hundred and sixty of this game's arithmetic "
            + "conditions are readable** -- " + gelesen + ", and the "
            + "grammar was measured from them and not chosen");
        AssertTrue(verweigert.Count == 0,
            "**and not one of them is refused** -- it said "
            + string.Join(" | ", verweigert.Take(3))
            + ", and a reader that refused one of its own measured forms "
            + "would be refusing the measurement");
    }

    /// <summary>
    /// The conditions that are pure arithmetic, straight out of the files.
    /// </summary>
    private static IEnumerable<string> Rechnungen()
    {
        foreach (var ausdruck in SkriptBedingungen())
        {
            if (!ausdruck.Contains("$gameVariables.value",
                StringComparison.Ordinal))
            {
                continue;
            }

            var rest = ausdruck.Replace("$gameVariables.value", "");
            rest = System.Text.RegularExpressions.Regex.Replace(
                rest, @"\(\s*\d+\s*\)", "");
            rest = rest.Replace(" ", "");
            if (rest.Length == 0)
            {
                continue;
            }

            // **Und die einzige Regel: was uebrig bleibt, darf nur
            // Ziffern und die Operatoren der Grammatik sein** -- **und
            // alles andere wird nicht gelistet und nicht beantwortet.**
            if (System.Text.RegularExpressions.Regex.IsMatch(
                rest, @"^[0-9+\-*/%()<>=!&|.]+$"))
            {
                yield return ausdruck;
            }
        }
    }


    private static IEnumerable<string> SkriptBedingungen()
    {
        var d = Projekt + "/data";
        foreach (var datei in Directory.GetFiles(
            d, "*.json", SearchOption.AllDirectories))
        {
            var name = Path.GetFileNameWithoutExtension(datei);
            var istKarte = name.StartsWith("Map", StringComparison.Ordinal)
                && name.Length > 3
                && name.Substring(3).All(char.IsDigit);
            if (!istKarte && name != "CommonEvents")
            {
                continue;
            }

            var wurzel = JsonDocument.Parse(File.ReadAllText(datei))
                .RootElement;
            if (wurzel.ValueKind == JsonValueKind.Array)
            {
                foreach (var eintrag in wurzel.EnumerateArray())
                {
                    if (eintrag.ValueKind == JsonValueKind.Object
                        && eintrag.TryGetProperty("list", out var liste))
                    {
                        foreach (var x in Aus(liste))
                        {
                            yield return x;
                        }
                    }
                }

                continue;
            }

            if (!wurzel.TryGetProperty("events", out var events))
            {
                continue;
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
                        foreach (var x in Aus(liste))
                        {
                            yield return x;
                        }
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
    /// And the frame counter is a number this reader knows.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And it is the most common expression in this game's
    /// conditions.</strong> Measured at <c>sister/www</c>:
    /// </para>
    /// <code>
    /// 193x  $gameSelfVariables.get(this, 'frames') &lt;= -1
    ///  75x  $gameSelfVariables.get(this, 'frames') &gt;= 7
    ///  58x  $gameSelfVariables.get(this, 'frames') &gt;= 5
    ///  38x  $gameSelfVariables.get(this, 'frames') &gt;= 6
    /// ```
    /// </code>
    /// <para>
    /// <strong>And 661 conditions in the whole game ask about
    /// it.</strong>
    /// </para>
    /// <para>
    /// <strong>And the plugin says where the number comes
    /// from:</strong>
    /// </para>
    /// <code>
    /// $.prototype.value = function(key) { return this._data[key] || 0; };
    /// $.prototype.get = function(interpreter, key) {
    ///     return this.value(_createKey(interpreter, key));
    /// };
    /// ```
    /// <para>
    /// <strong>And so it starts at zero</strong>, <strong>and the block
    /// that writes it is a <c>355</c> with one parameter:</strong>
    /// </para>
    /// <code>
    /// {"code":355,"indent":0,"parameters":[
    ///     "$gameSelfVariables.set(this, 'frames', 0);"]}
    /// </code>
    /// </remarks>
    public void Test_DerEigeneFensterzaehler()
    {
        // **Und bei null ist `&gt;= 7` falsch.**
        var fakten = new MzBranchFacts();
        var wert0 = MzArithmetic.TryRead(
            "$gameSelfVariables.get(this, 'frames') >= 7",
            fakten, out var fehlt0);

        AssertEq(
            wert0,
            0,
            "**and at zero the first form is false** -- it is "
            + fehlt0 + ", and the counter starts at zero because "
            + "`value(key)` is `this._data[key] || 0`");

        // **Und bei sieben ist es wahr** -- **und das ist der ganze
        // Beweis, dass die Zahl gelesen wird und nicht geraten.**
        fakten.EigenesFenster = 7;
        AssertEq(
            MzArithmetic.TryRead(
                "$gameSelfVariables.get(this, 'frames') >= 7",
                fakten, out var fehlt7),
            1,
            "**and at seven it is true** -- it is " + fehlt7);

        // **Und `&lt;= -1` ist die haeufigste Form und ist bei null
        // wahr.**
        AssertEq(
            MzArithmetic.TryRead(
                "$gameSelfVariables.get(this, 'frames') <= -1",
                fakten, out var fehltM),
            0,
            "**and at seven the commonest form is false** -- it is "
            + fehltM + ", and 193 conditions in this game ask exactly "
            + "that");

        // **Und `<= -1` ist bei null FALSCH** -- **und das ist keine
        // Fehlbehandlung, sondern `0 <= -1` in C# und in JavaScript.**
        //
        // **Und meine erste Fassung dieses Tests erwartete hier `true`,
        // und der Leser sagte `0`, und ich hielt den Leser fuer falsch
        // und ging drei Schritte in die Irre** -- **und der Leser hatte
        // die ganze Zeit recht.**
        //
        // ```text
        // 0 <= -1   -> 0    (0 ist nicht kleiner als -1)
        // -1 <= 0   -> 1    (-1 ist kleiner als 0)
        // ```
        //
        // **Und die Bedingung fragt also nach einem Zaehler, der
        // negativ geworden ist** -- **und das entsteht aus
        // `$gameSelfVariables.set(this, 'frames', -1)` oder aus einem
        // `add` mit einer negativen Zahl.**
        fakten.EigenesFenster = 0;
        AssertEq(
            MzArithmetic.TryRead(
                "$gameSelfVariables.get(this, 'frames') <= -1",
                fakten, out var fehltN),
            0,
            "**and at zero the commonest form of this game's conditions "
            + "is false** -- and that is `0 <= -1` and not a reader that "
            + "misreads, and 193 of the 661 conditions ask exactly that");

        fakten.EigenesFenster = -1;
        AssertEq(
            MzArithmetic.TryRead(
                "$gameSelfVariables.get(this, 'frames') <= -1",
                fakten, out var fehltO),
            1,
            "**and at minus one it is true** -- it is " + fehltO
            + ", and that is what the branch is for");

        // **Und der Wert daneben gehoert zu derselben Bedingung und ist
        // keine Fehlermeldung.**
        AssertEq(
            MzArithmetic.TryRead(
                "$gameSelfVariables.get(this, 'frames') <= 0",
                fakten, out var fehltP),
            1,
            "**and against a plain zero it is true** -- and that is "
            + "`-1 <= 0`, which is the other direction and the other "
            + "branch");

        // **Und die drei Formen des Schreibblocks** -- **und alle drei
        // sind Zuweisungen und keine Aufrufe.**
        var interp = new MzInterpreter(new List<MzCommandEntry>
        {
            Befehl(355, "\"$gameSelfVariables.set(this, 'frames', 0);\""),
            Befehl(0, "\"\""),
        });
        interp.Setup(2, 2);
        var fakten2 = new MzBranchFacts();
        interp.Run(new List<MzAction>(), fakten2);
        AssertEq(fakten2.EigenesFenster, 0,
            "**and the first form writes zero**");

        interp = new MzInterpreter(new List<MzCommandEntry>
        {
            Befehl(355, "\"$gameSelfVariables.add(this, 'frames', 1)\""),
            Befehl(355, "\"$gameSelfVariables.add(this, 'frames', 6)\""),
            Befehl(0, "\"\""),
        });
        interp.Setup(2, 2);
        var fakten3 = new MzBranchFacts();
        interp.Run(new List<MzAction>(), fakten3);
        AssertEq(fakten3.EigenesFenster, 7,
            "**and the third form adds, and it carries no semicolon** -- "
            + "it is " + fakten3.EigenesFenster + ", and `add` is "
            + "`this.setValue(key, currentValue + value)`");

        // **Und die zweite Form liest eine Spielvariable.**
        interp = new MzInterpreter(new List<MzCommandEntry>
        {
            Befehl(355,
                "\"$gameSelfVariables.set(this, 'frames', "
                + "$gameVariables.value(3));\""),
            Befehl(0, "\"\""),
        });
        interp.Setup(2, 2);
        var fakten4 = new MzBranchFacts
        {
            Variables = { [3] = 12 },
        };
        interp.Run(new List<MzAction>(), fakten4);
        AssertEq(fakten4.EigenesFenster, 12,
            "**and the second form takes the number out of the game's "
            + "own variable** -- it is " + fakten4.EigenesFenster);

        // **Und ein fremdes Skript fasst den Zaehler nicht an.**
        interp = new MzInterpreter(new List<MzCommandEntry>
        {
            Befehl(355, "\"var x = 1;\""),
            Befehl(0, "\"\""),
        });
        interp.Setup(2, 2);
        var fakten5 = new MzBranchFacts { EigenesFenster = 5 };
        interp.Run(new List<MzAction>(), fakten5);
        AssertEq(fakten5.EigenesFenster, 5,
            "**and a block that is the author's own JavaScript leaves the "
            + "counter alone** -- it is " + fakten5.EigenesFenster
            + ", because `var x = 1;` writes nothing this reader knows");
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
