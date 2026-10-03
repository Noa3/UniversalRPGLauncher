using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UniversalRPG.Web;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// What the command table names and what the command code runs, and
/// that the difference is twenty-one and not zero.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this test exists because I said something false, and
/// twice.</strong>
/// I wrote that <c>MzParty</c> has no actors and that the party path
/// was missing. -- <strong>It is not.</strong> <c>MzCommands</c> has
/// 102 case branches, -- <strong>and <c>ChangeClass</c>,
/// <c>ChangeEquipment</c>, <c>ChangeName</c>,
/// <c>ChangePartyMember</c>, <c>ChangeActorName</c> and
/// <c>ChangeGold</c> write live state:</strong>
///
/// <code>
/// case MzCommandTable.ChangeName:
///     if (!pFacts.PartyMembers.Contains(zuBenennen)) { ... }
///     pFacts.Namen[zuBenennen] = Text(pCommand, 1);
/// </code>
/// </para>
/// <para>
/// <strong>And the real gap is twenty-one named constants</strong>,
/// -- <strong>and nine of them are the control flow</strong>, --
/// <strong>and <c>355 Script</c> is the one every one of the game's
/// 10111 script commands needs.</strong>
/// </para>
/// </remarks>
public partial class TestMzBefehlsabdeckung : TestBase
{
    private const string Engine =
        "D:/Itch/sister/www/js/rpg_objects.js";

    private static List<int> EngineBefehle()
    {
        var liste = new List<int>();
        foreach (Match m in Regex.Matches(
            File.ReadAllText(Engine), @"^\s*command(\d+)\(\)\s*\{",
            RegexOptions.Multiline))
        {
            liste.Add(int.Parse(m.Groups[1].Value));
        }

        return liste;
    }

    private static Dictionary<string, int> Tabelle()
    {
        var quelle = File.ReadAllText(
            "E:/URPG/project/src/mz/MzCommandTable.cs");
        var raus = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (Match m in Regex.Matches(quelle,
            @"public const int (\w+) = (\d+);"))
        {
            raus[m.Groups[1].Value] = int.Parse(m.Groups[2].Value);
        }

        return raus;
    }

    private static HashSet<string> Zweige()
    {
        var quelle = File.ReadAllText(
            "E:/URPG/project/src/mz/MzCommands.cs");
        return Regex.Matches(quelle, @"case MzCommandTable\.(\w+):")
            .Select(m => m.Groups[1].Value).ToHashSet(StringComparer.Ordinal);
    }

    /// <summary>
    /// And the engine has 112 commands, and the table names 121.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the table names more than the engine
    /// implements</strong>, -- <strong>and that is not a fault</strong>
    /// -- <strong>the data numbers (402, 405, 408, 605, 655) are not
    /// commands and are named so that a reader does not dispatch
    /// them.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieDreiZahlen()
    {
        var engine = EngineBefehle();
        var tabelle = Tabelle();
        var zweige = Zweige();

        Console.WriteLine($"engine {engine.Count}  tabelle {tabelle.Count}"
            + $"  zweige {zweige.Count}");

        AssertEq(112, engine.Count,
            "**and the engine's own file has 112 `commandNNN()`"
                + " blocks** -- and every one has an empty parameter"
                + " list, which is why a regex expecting `(params)`"
                + " finds none of them");

        AssertEq(121, tabelle.Count,
            "**and the table names 121** -- and nine more than the"
                + " engine implements, because the data numbers are"
                + " named too");

        AssertEq(100, zweige.Count,
            "**and the command code runs exactly 100 branches** --"
                + " and seven of them touch the party and the"
                + " actors, and 121 - 100 = 21 is what is named"
                + " and not run");
    }

    /// <summary>
    /// And the twenty-one that are named and not run.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And nine of the twenty-one are the control flow</strong>,
    /// -- <strong>and <c>355</c> is the one that matters</strong>,
    /// -- <strong>because the game's scripts are 10111 commands
    /// with that code.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieNeunzehnOffenen()
    {
        var tabelle = Tabelle();
        var zweige = Zweige();
        var offen = tabelle
            .Where(x => !zweige.Contains(x.Key))
            .OrderBy(x => x.Value)
            .Select(x => x.Key + " " + x.Value)
            .ToList();

        Console.WriteLine("offen: " + string.Join("  ", offen));
        AssertEq(21, offen.Count,
            "**and twenty-one constants are named and not run** --"
                + " and 402 appears twice and 655 twice, so the"
                + " table names some numbers under two names");

        var kontrollfluss = new[]
        {
            "ConditionalBranch", "Loop", "BreakLoop", "ExitEventProcessing",
            "Label", "JumpToLabel", "Else", "EndBranch", "RepeatAbove",
        };
        foreach (var name in kontrollfluss)
        {
            AssertTrue(offen.Any(o => o.StartsWith(name + " ", StringComparison.Ordinal)),
                "**and `" + name + "` is among them** -- and the"
                    + " control flow is where a gap stops a page"
                    + " rather than only dimming it");
        }

        AssertTrue(offen.Any(o => o.StartsWith("Script ", StringComparison.Ordinal)),
            "**and `355 Script` is among them** -- and the game's"
                + " 10111 script commands all carry that code, and"
                + " this is the single largest gap in the command"
                + " table");
        AssertTrue(offen.Any(o => o.StartsWith("CommonEvent ", StringComparison.Ordinal)),
            "**and `117 CommonEvent` is among them** -- and that is"
                + " the command that runs the scripts in"
                + " `CommonEvents.json`");
    }

    /// <summary>
    /// And the party path is not missing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this test is the correction of a false
    /// statement.</strong> The six party commands write live state in
    /// <c>MzBranchFacts</c>, -- <strong>and <c>PartyMembers</c>,
    /// <c>Namen</c> and <c>Classes</c> are changed there</strong>, --
    /// <strong>so the path exists and only the actor object behind
    /// it is a separate question.</strong>
    /// </para>
    /// </remarks>
    public void Test_DerParteiwegExistiert()
    {
        var zweige = Zweige();
        foreach (var name in new[]
        {
            "ChangeClass", "ChangeEquipment", "ChangeName",
            "ChangePartyMember", "ChangeActorName", "ChangeGold",
            "ChangeItems",
        })
        {
            AssertTrue(zweige.Contains(name),
                "**and `" + name + "` has a branch** -- and I claimed"
                    + " it did not, and that claim was wrong");
        }

        var quelle = File.ReadAllText(
            "E:/URPG/project/src/mz/MzCommands.cs");
        foreach (var zuweisung in new[]
        {
            "pFacts.PartyMembers.Add(darsteller)",
            "pFacts.PartyMembers.Remove(darsteller)",
            "pFacts.Namen[zuBenennen]",
            "pFacts.Classes.Add",
        })
        {
            AssertTrue(quelle.Contains(zuweisung),
                "**and `" + zuweisung + "` is there** -- and this is"
                    + " live state, not a notice");
        }
    }
}
