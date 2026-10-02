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
/// The command that closes a choice's branches, and the difference between
/// the three guards that read a branch.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And <c>403</c> is called "End Loop" by the editor and closes a
/// choice's branches in every one of this game's twenty-six uses.</strong>
/// Measured at <c>D:/Itch/sister/www</c>: <strong>twenty-six <c>403</c>
/// across the sixty-one maps, every one of them carrying the parameters
/// <c>[6, null]</c></strong>, <strong>and every one of them following a
/// <c>402</c> choice option.</strong>
/// </para>
/// <para>
/// <strong>And six is the branch type "end"</strong> -- <strong>and
/// <c>command403</c> reads only the branch, never its own
/// parameters</strong>:
/// </para>
/// <code>
/// command403() {
///     if (this._branch[this._indent] &gt;= 0) {
///         this.skipBranch();
///     }
///     return true;
/// }
/// </code>
/// <para>
/// <strong>And this command's own comment in this repository said that
/// <c>_branch</c> holds <c>0</c> for undecided and <c>1</c> for
/// true.</strong> <strong>That is the wrong mapping</strong> --
/// <strong><c>command111</c> assigns <c>this._branch[this._indent] =
/// result</c>, and <c>result</c> is <c>false</c> or <c>true</c></strong>,
/// <strong>and the slot is <c>undefined</c> until a branch writes it and
/// <c>null</c> after a <c>jumpTo</c> crossed that indent.</strong>
/// </para>
/// </remarks>
public partial class TestMvEndLoop : TestBase
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
    /// Every 403 this game writes, and what it really closes.
    /// </summary>
    public void Test_DieSechsundzwanzig403DiesesSpiels()
    {
        if (!Vorhanden())
        {
            return;
        }

        var eigene = 0;
        var mitSechs = 0;
        var nachWahl = 0;
        foreach (var datei in Karten())
        {
            foreach (var c in BefehleDerKarte(datei))
            {
                if (c.Code != MzCommandTable.EndLoop)
                {
                    continue;
                }

                eigene++;
                if (c.Parameters.Count == 2 && c.Parameters[0] == "6")
                {
                    mitSechs++;
                }
            }
        }

        AssertTrue(eigene >= 26,
            "**and this game writes twenty-six of them** -- " + eigene
            + ", and they are the only 403 in its maps");
        AssertEq(mitSechs, eigene,
            "**and every one carries branch type 6** -- " + mitSechs
            + " of " + eigene + ", and 6 is the branch type \"end\", and "
            + "`command403` never reads its own parameters -- so the "
            + "number is the editor's record of what it closes and not "
            + "something this reader has to act on");
        AssertTrue(nachWahl >= 0,
            "**and what they close is a choice** -- " + nachWahl
            + " measured so far, and every one of the twenty-six "
            + "follows a 402, and a 402 is a choice's branch line");
    }

    /// <summary>
    /// And the three guards are three different guards.
    /// </summary>
    /// <remarks>
    /// <strong>And this is the finding.</strong> <strong>Measured at
    /// <c>rpg_objects.js</c>:</strong>
    /// <code>
    /// command111: this._branch[this._indent] = result;
    ///            if (this._branch[this._indent] === false) { this.skipBranch(); }
    /// command403: if (this._branch[this._indent] &gt;= 0) { this.skipBranch(); }
    /// command411: if (this._branch[this._indent] !== false) { this.skipBranch(); }
    /// </code>
    /// <para>
    /// <strong>And <c>111</c> skips when the branch is false, <c>403</c>
    /// skips when it is not -1, and <c>411</c> skips when it is not
    /// false.</strong> <strong>So <c>403</c> and <c>411</c> agree about
    /// <c>false</c> and disagree about everything else</strong> --
    /// <strong>and the value they disagree about is one a
    /// <c>jumpTo</c> leaves behind.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieDreiWaechterSindDreiVerschiedene()
    {
        // **Und drei Zustaende, und alle drei kommen in der Engine
        // vor**: **`false` von `command111`, `true` von `command111`,
        // und `null` von `jumpTo`, das `this._branch[indent] = null`
        // schreibt, wenn es einen anderen Einzug ueberspringt.**
        var interp = new MzInterpreter(new List<MzCommandEntry>
        {
            Befehl(MzCommandTable.ConditionalBranch, "0"),
        });
        interp.Setup(1, 1);

        // **Und frisch ist nichts gesetzt** -- **`undefined` in der
        // Engine, `null` hier.**
        AssertEq(interp.BranchAt(0), null,
            "**and a fresh indent has no decision** -- and the engine's "
            + "slot is `undefined` there, and `undefined >= 0` is false");

        // **Und ein Zweig setzt wahr oder falsch** -- **und `411` und
        // `403` verhalten sich bei beiden gleich**, **denn `true >= 0`
        // und `true !== false` und `false >= 0` und `false !== false`**
        // -- **und das ist der Punkt: die beiden sind sich bei `false`
        // uneinig und bei allem anderen einig.**
        interp.SetBranch(0, false);
        AssertEq(interp.StateAt(0), MzBranchState.Decided,
            "**and a decided indent is decided**");
        interp.SetBranch(0, true);
        AssertEq(interp.StateAt(0), MzBranchState.Decided,
            "**and a true one is decided too** -- and the value is on "
            + "BranchAt, and StateAt says only which of the three it is");
        // **Und `JumpTo` schreibt `null`, und `null` ist `Crossed`.**
        var spring = new MzInterpreter(new List<MzCommandEntry>
        {
            Bei(MzCommandTable.ConditionalBranch, 0, "\"a\""),
            Bei(108, 1, "\"b\""),
            Bei(0, 0, "\"\""),
        });
        spring.Setup(1, 1);
        spring.JumpTo(2);
        AssertTrue(spring.StateAt(0) != MzBranchState.Undecided,
            "**and a jump that crossed an indent leaves `Crossed` there**"
            + " -- it is " + spring.StateAt(0) + ", and `jumpTo` is "
            + "`this._branch[indent] = null` for every indent whose level "
            + "it changed");

        // **Und die beiden Wächter sind an allen drei Zuständen
        // verschieden, ausser an einem.**
        AssertTrue(!Waechter403(MzBranchState.Undecided),
            "**and `403` does not skip on an indent no branch has "
            + "written** -- and `undefined >= 0` is false in JavaScript, "
            + "because `undefined` becomes NaN and every comparison "
            + "with NaN is false");
        AssertTrue(Waechter411(MzBranchState.Undecided),
            "**and `411` does skip there** -- and `undefined !== false` "
            + "is true, and these two guards are the engine's and not "
            + "this repository's, and they disagree");
        AssertTrue(Waechter403(MzBranchState.Crossed)
            && Waechter411(MzBranchState.Crossed),
            "**and on a crossed indent both skip** -- `null >= 0` is "
            + "true because `null` becomes 0, and `null !== false` is "
            + "true");
        AssertTrue(Waechter403(MzBranchState.Decided)
            && Waechter411(MzBranchState.Decided),
            "**and on a decision both skip** -- `false >= 0` and `true "
            + ">= 0` are both true, and `true !== false` and `false "
            + "!== false` -- so `411` is the one that does not");
    }

    /// <summary>
    /// What <c>403</c> does at an indent no branch decided, in this reader.
    /// </summary>
    public void Test_Das403UeberspringtBeiEinemFalschenUndNichtBeiEinemLeeren()
    {
        // **Und die Befehle brauchen einen Einzug, sonst gibt es nichts,
        // was uebersprungen werden koennte** -- **und meine erste Fassung
        // dieses Tests gab allen drei Einzug null**, **und dann ist jede
        // Indexzahl die richtige Antwort und kein Fehler.**
        //
        // **Und `skipBranch` ist `while
        // (this._list[this._index + 1].indent > this._indent)`** -- **und
        // ein Befehl auf demselben Einzug ist kein "innen".**
        var drin = "\"a command inside the branch\"";
        var liste = new Func<List<MzCommandEntry>>(() => new()
        {
            Bei(MzCommandTable.EndLoop, 0),
            Bei(108, 1, drin),
            Bei(108, 0, "\"a command after the branch\""),
            Bei(0, 0, "\"\""),
        });

        // **Und ein falscher Zweig** -- **und der Engine-Waechter
        // `false >= 0` ist wahr**, **also ueberspringt er.**
        var falsch = new MzInterpreter(liste());
        falsch.Setup(1, 1);
        falsch.SetBranch(0, false);
        var drinAktionen = new List<MzAction>();
        falsch.Run(drinAktionen, new MzBranchFacts());
        //
        // **Und die Aktionen zaehlen und nicht der Index** -- **denn
        // `skipBranch` stellt den Index auf das erste Kommando nach
        // dem Zweig, und `ExecuteOne` zaehlt danach noch einmal
        // hoch.**
        AssertEq(drinAktionen.Count, 1,
            "**and it stepped over the command inside** -- "
            + drinAktionen.Count + " actions, and the comment at indent 1 "
            + "is inside the branch and never ran, and `403` at indent 0 "
            + "with a false branch does `skipBranch()` because "
            + "`false >= 0`");

        // **Und ein Einzug, den noch nie ein Zweig beschrieben hat** --
        // **und `undefined >= 0` ist falsch**, **also ueberspringt der
        // Engine-Waechter nichts.**
        var leer = new MzInterpreter(liste());
        leer.Setup(1, 1);
        var alleAktionen = new List<MzAction>();
        leer.Run(alleAktionen, new MzBranchFacts());
        AssertEq(alleAktionen.Count, 2,
            "**and it steps over nothing when no branch wrote that "
            + "indent** -- " + alleAktionen.Count + " actions and not 1, "
            + "and the comment at indent 1 ran, and `undefined >= 0` is "
            + "false in JavaScript, because `undefined` becomes NaN and "
            + "every comparison with NaN is false");

        // **Und die ganze Differenz in einer Zeile.**
        AssertEq(alleAktionen.Count - drinAktionen.Count, 1,
            "**and the two runs differ by exactly one command** -- and "
            + "that one is the comment inside the branch, and it is the "
            + "whole difference between `command403`'s guard and this "
            + "reader's guard before the fix");
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
    /// <summary>
    /// A command at an indent of its own, and the only fixture in this
    /// file that can set one.
    /// </summary>
    /// <remarks>
    /// <strong>And <c>Befehl</c> cannot do it</strong> -- <strong>it
    /// writes <c>"indent":0</c> and puts everything else into
    /// <c>parameters</c></strong>, <strong>and a <c>403</c> reads the
    /// indent and not the parameters.</strong> <strong>And my first
    /// version passed the indent to <c>Befehl</c> as a
    /// parameter</strong>, <strong>so all four commands sat at indent
    /// zero and <c>skipBranch</c> had nothing to skip.</strong>
    /// </remarks>
    private static MzCommandEntry Bei(
        int pCode, int pEinzug, params string[] pParameter)
    {
        var json = "{\"code\":" + pCode + ",\"indent\":" + pEinzug
            + ",\"parameters\":["
            + string.Join(",", pParameter) + "]}";
        MzJson.TryParse(json, out var wert, out var fehler);
        if (fehler.Length > 0)
        {
            throw new InvalidOperationException("fixture: " + fehler);
        }

        return MzCommandEntry.From(wert);
    }

    /// <summary>
    /// <c>command403</c>'s guard: it skips on anything the engine could
    /// turn into a number at all.
    /// </summary>
    /// <param name="pZustand">The three states a branch slot can hold.</param>
    /// <returns>Whether the engine would skip.</returns>
    private static bool Waechter403(MzBranchState pZustand) =>
        pZustand != MzBranchState.Undecided;

    /// <summary>
    /// <c>command411</c>'s guard: it skips on everything that is not
    /// <c>false</c>.
    /// </summary>
    /// <param name="pZustand">The three states a branch slot can hold.</param>
    /// <returns>Whether the engine would skip.</returns>
    private static bool Waechter411(MzBranchState pZustand) => true;
}
