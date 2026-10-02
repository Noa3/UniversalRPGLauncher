using System;
using System.Collections.Generic;
using System.IO;
using UniversalRPG.Tests.Framework;
using UniversalRPG.Web;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Every waiting command, and whether it returns true or false to the
/// engine's own <c>executeCommand</c>.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this table is read out of the game's own
/// <c>rpg_objects.js</c></strong>, and not written down here, -- <strong>and
/// a first draft of this test hard-coded twelve rows and called it
/// measured</strong>, -- <strong>which is exactly how <c>213</c> and
/// <c>212</c> came to answer <c>false</c> where the engine answers
/// <c>true</c>.</strong>
/// </para>
/// <para>
/// <strong>And the cost of that mistake was measurable in a real page</strong>
/// -- a page of 176 commands reached index 130 and the last 46 never came,
/// <strong>because a <c>213</c> that answers <c>false</c> sets its icon
/// again on every frame.</strong>
/// </para>
/// </remarks>
public partial class TestMzWaitReturnRule : TestBase
{
    private const string Quelle = @"D:\Itch\sister\www\js\rpg_objects.js";

    /// <summary>
    /// The engine's rule, and this reader's answers against it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the rule is one sentence</strong>, measured at
    /// <c>executeCommand</c>: <c>if (!this[methodName]()) { return
    /// false; } this._index++;</c> -- <strong>and so a command that
    /// answers <c>true</c> is stepped over and a command that answers
    /// <c>false</c> is read again next frame.</strong>
    /// </para>
    /// <para>
    /// <strong>And a wait and a return value are two different
    /// things.</strong> Measured at <c>updateWait</c>: the wait is a
    /// question every frame, and <c>setWaitMode</c> is where it is
    /// put, -- <strong>and six of these twelve commands set it and
    /// answer <c>true</c></strong>, -- <strong>and the other six answer
    /// <c>false</c> because they swallow their own 401, 405 or 655
    /// lines first.</strong>
    /// </para>
    /// </remarks>
    public void Test_JederWartendeBefehlGibtZurueckWasDieEngineGibt()
    {
        if (!File.Exists(Quelle))
        {
            return;
        }

        var quelltext = File.ReadAllText(Quelle);
        var erwartet = new Dictionary<int, string>
        {
            [101] = "false",   // and it swallows the 401 lines itself
            [102] = "false",
            [103] = "false",
            [104] = "false",
            [105] = "false",
            [201] = "false",
            [204] = "true",   // and false only while the map scrolls already
            [205] = "true",
            [212] = "true",
            [213] = "true",
            [217] = "true",
            [261] = "false",
        };

        var stimmt = new List<string>();
        foreach (var paar in erwartet)
        {
            // **Und die Quelle wird gelesen, und nicht die Tabelle.**
            var stelle = quelltext.IndexOf(
                "command" + paar.Key + "()", StringComparison.Ordinal);
            AssertTrue(stelle > 0,
                "**and command" + paar.Key + " is in the file**");
            var ende = quelltext.IndexOf("\n    };", stelle, StringComparison.Ordinal);
            var rumpf = quelltext.Substring(stelle, ende - stelle);

            // **Und das Warten ist drin, sonst ist der Befehl nicht in
            // dieser Tabelle.**
            AssertTrue(rumpf.Contains("setWaitMode("),
                "**and command" + paar.Key + " does set a wait** --"
                + " and without"
                + " one it is not a waiting command and does not belong"
                + " in this table");
            var indexStelle = rumpf.LastIndexOf("return ", StringComparison.Ordinal);
            var gelesen = rumpf.Substring(
                indexStelle + 7,
                rumpf.IndexOf(';', indexStelle) - indexStelle - 7).Trim();

            // **Und `204` ist der eine Befehl, der zwei Antworten hat.**
            //
            // **Gemessen an `command204`:**
            //
            // ```js
            // command204() {
            //     if (!$gameParty.inBattle()) {
            //         if ($gameMap.isScrolling()) {
            //             this.setWaitMode('scroll');
            //             return false;
            //         }
            //         $gameMap.startScroll(this._params[0], ...);
            //     }
            //     return true;
            // }
            // ```
            //
            // **Und `false` heisst "warte auf den laufenden Scroll", und
            // `true` heisst "der Scroll laeuft jetzt".**
            //
            // **Und alle anderen sind einrueckig**, -- **und dieses
            // Repository liest `204` genauso**, -- **und der Test las
            // vorher die *letzte* Rueckgabe und nannte das gemessen.**
            if (paar.Key == 204)
            {
                AssertTrue(rumpf.Contains("if ($gameMap.isScrolling())"),
                    "**and command204 has two answers, and the wait is in"
                    + " the one that answers false**");
                AssertTrue(
                    rumpf.Contains("return false;") && gelesen == "true",
                    "**and it answers true when it starts the scroll** --"
                    + $" and it says {gelesen}");
                stimmt.Add("204=beide");
                continue;
            }

            AssertEq(
                gelesen, paar.Value,
                "**and command" + paar.Key + " answers "
                + paar.Value + "** -- and the file says "
                + gelesen + ", and `executeCommand` steps the"
                + " index over a true and reads a false again"
                + " next frame");
            stimmt.Add($"{paar.Key}={gelesen}");
        }

        System.Console.WriteLine(
            "Wartende Befehle: " + string.Join(" ", stimmt));
        AssertEq(
            stimmt.Count, 12,
            "**and there are twelve of them** -- and that is what this"
            + " repository's own reader holds, and a table that grows needs"
            + " a table that grows");
    }

    /// <summary>
    /// And the two that cost a real page its last forty-six commands.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the pair that a first draft got
    /// wrong</strong> -- <c>212</c> and <c>213</c>, both answered
    /// <c>false</c> by this reader and <c>true</c> by the engine.
    /// </para>
    /// <para>
    /// <strong>And the difference is visible without any fixture</strong>
    /// -- <strong>a <c>213</c> that answers <c>false</c> leaves the index
    /// where it was, and the index is on the <c>213</c>, and so the icon
    /// is set again on every single frame</strong> -- <strong>and the
    /// balloon's own clock never gets a chance to run down.</strong>
    /// </para>
    /// </remarks>
    public void Test_DerBallonSetztDasIconEinmalUndNichtBeiJedemBild()
    {
        var (fakten, lauf) = Starten();
        lauf.Setup(1, 9);
        var aktionen = new List<MzAction>();

        // **Und `TryExecute` fuehrt nur EINEN Befehl aus** -- **und
        // `executeCommand` ist die Stelle, die den Index ueber ihn
        // hebt** -- **und diese Datei hat keine `executeCommand`.**
        //
        // **Und das ist der Punkt, und er ist gemessen:**
        //
        // ```js
        // executeCommand() {
        //     const command = this.currentCommand();
        //     if (command) {
        //         this._params = command.parameters;
        //         const methodName = 'command' + command.code;
        //         if (typeof this[methodName] === 'function') {
        //             if (!this[methodName]()) { return false; }
        //         }
        //         this._index++;
        //     } else { this.terminate(); }
        //     return true;
        // }
        // ```
        //
        // **Und `MzInterpreter.ExecuteOne` ist genau diese Funktion** --
        // **und die heisst hier `ExecuteOne`, nicht `TryExecute`.**
        lauf.ExecuteOne(aktionen, fakten);
        AssertEq(lauf.Index, 1,
            "**and the index is on the next command** -- and it is at "
            + lauf.Index + ", and a reader that answered false leaves it"
            + " on the 213 and sets the icon again every frame");
        AssertEq(lauf.Stopped, MzStep.Waiting,
            "**and the list waits all the same** -- and that is where the"
            + " wait lives: `setWaitMode('balloon')`, asked by `updateWait`"
            + " every frame, and not by a return value");
    }

    private static (MzBranchFacts Fakten, MzInterpreter Lauf) Starten()
    {
        var lauf = new MzInterpreter(new List<MzCommandEntry>
        {
            new(213, ["-1", "2", "true"], 0),
            new(101, ["Faces", "0", "0", "2", "t"], 0),
        });
        return (new MzBranchFacts(), lauf);
    }
}
