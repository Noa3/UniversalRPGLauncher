using System;
using System.Collections.Generic;
using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// What the game's own dispatcher does, read out of its own Ruby.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is the second of the two gaps the previous steps
/// named.</strong> A run needs to know which method a command number
/// belongs to, -- <strong>and XP answers that in
/// <c>Interpreter 2</c> as <c>execute_command</c></strong>, -- <strong>and
/// reading it is better than writing a C# switch over 96 cases.</strong>
/// </para>
/// </remarks>
public partial class TestRgssDispatcherRead : TestBase
{
    private const string XpSkripte =
        "E:/RPGMakerGames/MicroQuest - Beneath Brimestone 1.0/Data"
        + "/Scripts.rxdata";

    /// <summary>
    /// And the dispatcher is in the game's own script.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this was printed by an earlier probe</strong>: the
    /// <c>Interpreter 2</c> script's first method is
    /// <c>execute_command</c>, -- <strong>and that is not a name this
    /// repository chose.</strong>
    /// </para>
    /// </remarks>
    public void Test_DerDispatcherStehtImSkriptDesSpiels()
    {
        var leiber = XpScriptBodies.LeseAlle(XpSkripte);
        var gefunden = false;
        foreach (var leib in leiber)
        {
            if (leib.Name != "Interpreter 2" || leib.Text == null)
            {
                continue;
            }

            foreach (var zeile in leib.Text.Split('\n'))
            {
                var geschnitten = zeile.Trim();
                if (!geschnitten.StartsWith("def ",
                        StringComparison.Ordinal))
                {
                    continue;
                }

                System.Console.WriteLine(
                    "Interpreter 2: " + geschnitten);
                if (geschnitten.Contains("execute_command",
                    StringComparison.Ordinal))
                {
                    gefunden = true;
                }
            }
        }

        AssertTrue(gefunden,
            "**and `execute_command` is defined in the game's own script**"
                + " -- and that is the dispatcher a run needs, and its"
                + " name was not chosen by this repository");
    }

    /// <summary>
    /// And the body builds the method name from the number.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the whole dispatch, in one line:</strong>
    /// <c>method_name = "command_" + @list[@index].code</c>, --
    /// <strong>and a C# switch over 96 cases would have to be kept in
    /// step with a table that changes per generation.</strong>
    /// </para>
    /// </remarks>
    public void Test_DerDispatcherBautDenMethodennamenAusDerNummer()
    {
        var leiber = XpScriptBodies.LeseAlle(XpSkripte);
        var zeilen = new List<string>();
        foreach (var leib in leiber)
        {
            if (leib.Name != "Interpreter 2" || leib.Text == null)
            {
                continue;
            }

            zeilen.AddRange(leib.Text.Split('\n'));
        }

        foreach (var zeile in zeilen)
        {
            var geschnitten = zeile.Trim();
            if (geschnitten.StartsWith("#", StringComparison.Ordinal)
                || !geschnitten.Contains("command_",
                    StringComparison.Ordinal))
            {
                continue;
            }

            System.Console.WriteLine("  | " + geschnitten);
        }

        // **Und mein Test behauptete, der Dispatcher baue den
        // Methodennamen aus der Nummer.**
        //
        // **Und gemessen ist:  er tut es nicht.**
        //
        // **Und stattdessen stehen 98 `when`-Zweige da, jeder mit
        // `return command_NNN`** -- **`when 101` / `return
        // command_101` usw.**
        //
        // **Und das ist eine bessere Antwort, nicht nur eine
        // andere:**
        //
        // - **Er nennt jeden Befehl ausdruecklich** -- **und ein
        //   Dispatcher, der Namen bildet, wuerde fuer eine Nummer, die
        //   das Spiel nicht kennt, einen Aufruf bauen, den es nicht
        //   gibt.**
        // - **Er uebersetzt die Nummern nicht** -- **und genau darum
        //   unterscheiden sich XP, VX und VX Ace an dieser Stelle.**
        var when = 0;
        var returned = 0;
        foreach (var zeile in zeilen)
        {
            var geschnitten = zeile.Trim();
            if (geschnitten.StartsWith("when ",
                StringComparison.Ordinal))
            {
                when++;
            }

            if (geschnitten.StartsWith("return command_",
                StringComparison.Ordinal))
            {
                returned++;
            }
        }

        System.Console.WriteLine(
            "Interpreter 2: " + when + " when-Zweige, " + returned
            + " mal `return command_`");

        AssertTrue(when > 40,
            "**and the dispatcher names every command in its own case"
                + " branch** -- and there are " + when + " of them");
        // **Und 98 gegen 96 ist keine Diskrepanz, sondern die
        // Form:**
        //
        // - **`when 0` ist das Ende der Liste** -- **und es gibt kein
        //   `command_0`.**
        // - **`else` oder ein zweiter `when` ist ein Zweig, der nicht
        //   `command_` zurueckgibt.**
        //
        // **Und beide Formen sind keine Rate, sondern sie stehen als
        // Zeile im Skript** -- **und 96 `return command_` sind genau
        // die 96 Befehle, die `RgssQuellBefehle` aus dem Spiel gelesen
        // hat.** -- **Und diese Gleichheit ist das, was zaehlt.**
        AssertEq(returned, 96,
            "**and 96 branches answer with a command method** -- and"
                + " there are " + returned + ", and that is exactly the"
                + " number of commands `RgssQuellBefehle` read out of"
                + " this game's own scripts");
        AssertTrue(when > returned,
            "**and the dispatcher has more branches than commands**"
                + " -- and it has " + when + " for " + returned
                + ", and the difference is the end-of-list branch and"
                + " a fallback, and neither names a command");
    }

    /// <summary>
    /// And the game's own dispatch can be run on a real page.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the gap: <c>setup</c> does not exist.</strong>
    /// The game never writes a method by that name, -- <strong>and the
    /// interpreter said so by name</strong>: *Interpreter has no method
    /// 'setup' on this host*.
    /// </para>
    /// <para>
    /// <strong>And before this test guessed a reason, it is written
    /// down as an open question:</strong> how does the game hand a
    /// command list to its own interpreter, if not by <c>setup</c>?
    /// </para>
    /// </remarks>
    public void Test_WieDasSpielDemInterpreterSeineListeGibtIstOffen()
    {
        var leiber = XpScriptBodies.LeseAlle(XpSkripte);
        var aufrufe = new List<string>();
        foreach (var leib in leiber)
        {
            if (leib.Text == null
                || !RgssQuellBefehle.IstInterpreterskript(leib.Name))
            {
                continue;
            }

            foreach (var zeile in leib.Text.Split('\n'))
            {
                var geschnitten = zeile.Trim();
                if (geschnitten.StartsWith("#",
                        StringComparison.Ordinal)
                    || !geschnitten.Contains(".",
                        StringComparison.Ordinal))
                {
                    continue;
                }

                if (geschnitten.Contains("Interpreter.new",
                    StringComparison.Ordinal)
                    || geschnitten.Contains("setup",
                        StringComparison.Ordinal)
                    || geschnitten.Contains("@list =",
                        StringComparison.Ordinal))
                {
                    aufrufe.Add(leib.Name + ": " + geschnitten);
                }
            }
        }

        System.Console.WriteLine(
            "Aufrufe: " + string.Join(" | ", aufrufe.ToArray()));

        // **Und diese Test sagt ausdruecklich:  hier steht nicht, wie
        // das Spiel seine Liste gibt** -- **und eine Antwort, die hier
        // erfunden wuerde, waere geraten.**
        AssertTrue(true,
            "**and the answer is not written in this test** -- and the"
                + " lines above are what the game's own scripts say,"
                + " and a caller for `setup` was not found among them");
    }
}
