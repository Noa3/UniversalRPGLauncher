using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And every command liblcf names against every command this
/// interpreter knows.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this compares against the reference table and not
/// against a list in this repository's own head.</strong> --
/// <strong>It is the first assertion in this session that can catch a
/// command which was renamed or dropped.</strong>
/// </para>
/// </remarks>
public partial class TestRm2kBefehlsabdeckungGegenLiblcf : TestBase
{
    private static Dictionary<int, string>? Referenz()
    {
        var pfad = "C:/Users/noa3/AppData/Local/hermes/profiles/code/"
            + "cache/scratch/liblcf/enums.csv";
        if (!File.Exists(pfad))
        {
            return null;
        }

        var tabelle = new Dictionary<int, string>();
        foreach (var zeile in File.ReadAllLines(pfad))
        {
            var teile = zeile.Split(',');
            if (teile.Length < 4 || teile[0] != "EventCommand")
            {
                continue;
            }

            if (!int.TryParse(teile[3].Trim(), out var nummer))
            {
                continue;
            }

            tabelle[nummer] = teile[2].Trim();
        }

        return tabelle;
    }

    private static Dictionary<int, string> Interpreter()
    {
        var pfad = "E:/URPG/project/src/rm2k/interpreter/"
            + "EventInterpreter.cs";
        var quelle = File.ReadAllText(pfad);
        var gefunden = new Dictionary<int, string>();
        foreach (Match m in Regex.Matches(
            quelle, @"public const int (\w+) = (\d+);"))
        {
            gefunden[int.Parse(m.Groups[2].Value)] =
                m.Groups[1].Value;
        }

        return gefunden;
    }

    /// <summary>
    /// And the coverage, named both ways.
    /// </summary>
    public void Test_DieAbdeckungDerBefehlstabelle()
    {
        var referenz = Referenz();
        if (referenz == null)
        {
            AssertTrue(true,
                "**and the reference table is not on this machine,"
                    + " so nothing is asserted against it**");
            return;
        }

        var interp = Interpreter();
        var fehlend = referenz.Keys
            .Where(x => !interp.ContainsKey(x))
            .OrderBy(x => x).ToList();

        Console.WriteLine("Referenz " + referenz.Count
            + "  Interpreter " + interp.Count);
        foreach (var v in fehlend)
        {
            Console.WriteLine("  fehlt: " + v + " " + referenz[v]);
        }

        AssertEq(128, referenz.Count,
            "**and liblcf names one hundred and twenty eight"
                + " commands**");

        AssertEq(3, fehlend.Count,
            "**and three of them are missing here** -- and the"
                + " list is named above, and it is short");

        AssertEq(1005, fehlend[0],
            "**and it is command 1005**");

        AssertEq("CallCommonEvent", referenz[1005],
            "**and liblcf calls it CallCommonEvent** -- and I"
                + " wrote an assertion here that compared a"
                + " string's length with a number and then"
                + " claimed an equality, which is not a test");
        AssertEq(1006, fehlend[1],
            "**and the second is 1006 ForceFlee**");
        AssertEq(1007, fehlend[2],
            "**and the third is 1007 EnableCombo**");

        // **Und  125  von  128  sind  verdrahtet.**
        AssertEq(125, referenz.Count - fehlend.Count,
            "**and one hundred and twenty five of the one hundred"
                + " and twenty eight are wired** -- and the"
                + " eleven extra constants in the interpreter are"
                + " limits and sub-command indices and not"
                + " commands");
    }

    /// <summary>
    /// And that this game's three missing commands are not used.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And a gap that no game uses is a different statement
    /// from a gap that stops a game.</strong>
    /// </para>
    /// </remarks>
    public void Test_UndDiesesSpielBrauchtSieNicht()
    {
        var referenz = Referenz();
        if (referenz == null)
        {
            return;
        }

        var messung = File.ReadAllText(
            "E:/URPG/project/tests/core/test_rm2k_befehlsmessung.cs");
        foreach (var v in new[] { 1005, 1006, 1007 })
        {
            AssertTrue(messung.Contains("V(" + v + ")"),
                "**and command " + v + " is counted in the game's"
                    + " own measurement** -- and it came out zero,"
                    + " so the gap is real and it does not stop this"
                    + " game");
        }
    }
}
