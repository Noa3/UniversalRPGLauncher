using System;
using System.Collections.Generic;
using System.Linq;

using Godot;

using UniversalRPG.Tests.Framework;
using UniversalRPG.Web;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The three battle outcomes and the nine commands that change the troop.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And <c>command111</c> hat in MZ keine Kampf-Art</strong> --
/// **die aeusseren Faelle sind Switch, Variable, Self Switch, Timer,
/// Actor, Enemy, Character, Gold, Item, Weapon, Armor, Button, Script,
/// Vehicle**, **und keiner davon ist "Battle".</strong>
/// </para>
/// <para>
/// <strong>Und trotzdem vergleichen <c>601</c>, <c>602</c> und
/// <c>603</c> gegen <c>0</c>, <c>1</c> und <c>2</c>.</strong>
/// <strong>Die Zahlen kommen aus <c>BattleManager.endBattle(result)</c>
/// und landen ueber <c>command301</c>s Rueckruf an
/// <c>this._branch[this._indent]</c>.</strong>
/// </para>
/// <code>
/// BattleManager.setEventCallback(function(n) {
///     this._branch[this._indent] = n;
/// }.bind(this));
/// </code>
/// <code>
/// BattleManager.endBattle = function(result) {
///     this._phase = 'battleEnd';
///     if (this._eventCallback) { this._eventCallback(result); }
///     if (result === 0) { $gameSystem.onBattleWin(); }
///     ...
/// }
/// </code>
/// <para>
/// <strong>Und dieselbe <c>_branch</c>-Stelle beantwortet <c>402</c>,
/// <c>403</c> und <c>404</c></strong> -- <strong>dort stehen aber
/// <c>true</c> und <c>false</c>, weil <c>111</c> und <c>401</c>
/// Boolesches schreiben.</strong>
/// </para>
/// <para>
/// <strong>Und <c>iterateEnemyIndex</c> hat eine Regel mit zwei
/// Haelften:</strong> <strong>ein negativer Index ist jeder Gegner,
/// ein positiver genau einer</strong> -- <strong>und ein Index hinter
/// dem Ende ist ueberhaupt nichts, wegen <c>if (enemy)</c>, und kein
/// Fehler.</strong>
/// </para>
/// </remarks>
public partial class TestMvBattleOutcomes : TestBase
{
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

    /// <summary>A troop of three, two of a kind and one of its own.</summary>
    private static MzBranchFacts DreiGegner(bool pImKampf)
    {
        var fakten = new MzBranchFacts();
        if (pImKampf)
        {
            // **Und `InBattle` hat ein `private set`** -- **und der eine
            // Weg hinein ist `EnterBattle`**, **weil `301` dieselbe
            // Tuer benutzt und nicht das Feld direkt setzt.**
            fakten.EnterBattle();
        }

        fakten.Kampf.SetzeGegner(new[]
        {
            new MzEnemy(0, "Slime", 4),
            new MzEnemy(1, "Slime", 4),
            new MzEnemy(2, "Bat", 7),
        });
        return fakten;
    }

    /// <summary>
    /// And the three numbers are the enum's, and each branch takes only
    /// its own.
    /// </summary>
    public void Test_DieDreiAusgaengeUndIhreDreiZweige()
    {
        foreach (var (ausgang, erwartet) in new[]
        {
            (MzBattleResult.Win, MzCommandTable.BattleWin),
            (MzBattleResult.Escape, MzCommandTable.BattleEscape),
            (MzBattleResult.Lose, MzCommandTable.BattleLose),
        })
        {
            var fakten = DreiGegner(true);
            fakten.Kampf.Endet((int)ausgang);

            var interp = new MzInterpreter(new List<MzCommandEntry>
            {
                Befehl(erwartet, "\"\""),
                Befehl(MzCommandTable.ChangeName, "0", "\"nie gelaufen\""),
            });
            interp.Setup(1, 1);
            var aktionen = new List<MzAction>();
            interp.Run(aktionen, fakten);

            AssertTrue(aktionen.Any(a =>
                    a.What.Contains("it is taken", StringComparison.Ordinal)),
                "**and " + ausgang + " takes its own branch and not the "
                + "other two** -- the actions were "
                + string.Join(" / ", aktionen.Select(a => a.What)));
        }
    }

    /// <summary>
    /// And a branch for another outcome is skipped, which is
    /// <c>skipBranch()</c> and not a <c>return false</c>.
    /// </summary>
    public void Test_DerFalscheZweigWirdUebersprungen()
    {
        var fakten = DreiGegner(true);
        fakten.Kampf.Endet((int)MzBattleResult.Win);

        // **Und `command602` bei einem Sieg:** `this._branch[this._indent]
        // !== 1` ist wahr, **und also `skipBranch()`.**
        var interp = new MzInterpreter(new List<MzCommandEntry>
        {
            Befehl(MzCommandTable.BattleEscape, "\"\""),
            Befehl(MzCommandTable.ChangeName, "0", "\"nie gelaufen\""),
        });
        interp.Setup(1, 1);
        var aktionen = new List<MzAction>();
        interp.Run(aktionen, fakten);

        AssertTrue(aktionen.Any(a =>
                a.What.Contains("it is skipped", StringComparison.Ordinal)),
            "**and the escape branch is skipped after a win** -- the "
            + "actions were "
            + string.Join(" / ", aktionen.Select(a => a.What)));
        AssertTrue(!aktionen.Any(a =>
                a.What.Contains("never ran", StringComparison.Ordinal)),
            "**and what came after the branch did not run either** -- and "
            + "that is `skipBranch()` and not a `return false`, and a "
            + "`return false` would have left the page standing on the "
            + "same line forever");
    }

    /// <summary>
    /// And outside a battle there is no battle outcome at all.
    /// </summary>
    public void Test_AusserhalbDesKampfesGibtEsKeineKampfroute()
    {
        var fakten = DreiGegner(false);
        var interp = new MzInterpreter(new List<MzCommandEntry>
        {
            Befehl(MzCommandTable.BattleWin, "\"\""),
        });
        interp.Setup(1, 1);
        var aktionen = new List<MzAction>();
        interp.Run(aktionen, fakten);

        AssertTrue(aktionen.Count == 0,
            "**and 601 outside a battle does nothing** -- and there were "
            + aktionen.Count + " actions, and `command601` would have "
            + "compared against whatever the last `111` left in "
            + "`_branch`, which is a boolean");
        AssertTrue(fakten.Notices.Count >= 1,
            "**and it says why** -- it said '"
            + fakten.Notices.FirstOrDefault() + "'");
    }

    /// <summary>
    /// And a negative index is every enemy, a past index is none.
    /// </summary>
    public void Test_DerIndexIstEntwederEinerOderAlle()
    {
        // **Und `-1` heisst jeder Gegner** -- **und hier sind es alle
        // drei.**
        var alle = new MzInterpreter(new List<MzCommandEntry>
        {
            // **Und `-1` an erster Stelle ist der Index und nicht die
            // Operation** -- **und meine erste Fassung schrieb `"-1",
            // "1", "0", "500"`** -- **und das heisst Minus fuenfhundert**
            // -- **und der Test sah korrekt `-500` und ich hielt es fuer
            // einen Fehler der Implementierung.**
            Befehl(MzCommandTable.ChangeEnemyHp, "-1", "0", "0", "500"),
        });
        alle.Setup(1, 1);
        var faktenAlle = DreiGegner(true);
        alle.Run(new List<MzAction>(), faktenAlle);
        AssertTrue(faktenAlle.Kampf.Gegner.All(g => g.Hp == 500),
            "**and minus one named all three** -- it is "
            + string.Join(", ", faktenAlle.Kampf.Gegner.Select(g => g.Hp))
            + ", and `if (param < 0) { $gameTroop.members().forEach(...)"
            + " }` is that rule");

        // **Und ein Index hinter dem Ende ist ueberhaupt nichts.**
        var keiner = new MzInterpreter(new List<MzCommandEntry>
        {
            Befehl(MzCommandTable.ChangeEnemyHp, "9", "0", "0", "500"),
        });
        keiner.Setup(1, 1);
        var faktenKeiner = DreiGegner(true);
        keiner.Run(new List<MzAction>(), faktenKeiner);
        AssertTrue(faktenKeiner.Kampf.Gegner.All(g => g.Hp == 0),
            "**and index nine named none of them** -- it is "
            + string.Join(", ", faktenKeiner.Kampf.Gegner.Select(g => g.Hp))
            + ", and `var enemy = $gameTroop.members()[9]; if (enemy) { "
            + "callback(enemy); }` is why that is not an error");
    }

    /// <summary>
    /// And <c>340</c> is not <c>331</c> with an operation, and <c>336</c>
    /// is not one call but two.
    /// </summary>
    public void Test_DerSchadenIstMinusImAufrufUndNichtInDerOperation()
    {
        // **Und `gainHp(-value)`** -- **und `[0,0,500]` heisst plus 500,
        // **und das Minus macht daraus minus 500.** **Und ein Leser, der
        // `gainHp(value)` ruft, heilt.**
        var interp = new MzInterpreter(new List<MzCommandEntry>
        {
            Befehl(MzCommandTable.AbortBattle, "-1", "0", "0", "500"),
        });
        interp.Setup(1, 1);
        var fakten = DreiGegner(true);
        fakten.Kampf.GegnerNummer(0).Hp = 100;
        fakten.Kampf.GegnerNummer(1).Hp = 100;
        fakten.Kampf.GegnerNummer(2).Hp = 100;
        interp.Run(new List<MzAction>(), fakten);
        AssertTrue(fakten.Kampf.Gegner.All(g => g.Hp == -400),
            "**and the troop lost five hundred from a hundred** -- it is "
            + string.Join(", ", fakten.Kampf.Gegner.Select(g => g.Hp))
            + ", and `command340` is `enemy.gainHp(-value)` and `gainHp` "
            + "is `this._hp += value`");

        // **Und `336` macht zwei Dinge**, **und `appear()` allein
        // wuerde zwei gleiche Namen hinterlassen.**
        var versteckt = new MzInterpreter(new List<MzCommandEntry>
        {
            Befehl(MzCommandTable.EnemyAppear, "-1"),
        });
        versteckt.Setup(1, 1);
        var faktenVersteckt = DreiGegner(true);
        foreach (var gegner in faktenVersteckt.Kampf.Gegner)
        {
            gegner.Sichtbar = false;
        }

        faktenVersteckt.Kampf.MacheNamenEindeutig(
            klasse => klasse == 4 ? "Slime" : "Bat");
        versteckt.Run(new List<MzAction>(), faktenVersteckt);
        AssertTrue(faktenVersteckt.Kampf.Gegner.All(g => g.Sichtbar),
            "**and they are all back on the field**");
        AssertEq(faktenVersteckt.Kampf.Gegner[1].Name, "Slime 2",
            "**and the second slime has its own name** -- it is '"
            + faktenVersteckt.Kampf.Gegner[1].Name + "', and that is "
            + "`$gameTroop.makeUniqueNames()`, and `command336` calls it "
            + "in the same line as `enemy.appear()` and a reader that "
            + "only calls `appear()` leaves two identical names");
    }
}

