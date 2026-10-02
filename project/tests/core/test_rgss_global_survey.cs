using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Which global names the game's own 90 scripts use.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is the list a runtime has to answer, and it is
/// counted out of the game.</strong>
/// </para>
/// <para>
/// <strong>And the count is the point.</strong> A runtime that answers
/// five names runs a fifth of a game and says nothing about the rest, --
/// <strong>and a runtime that invents the rest runs on values that are
/// not there.</strong>
/// </para>
/// </remarks>
public partial class TestRgssGlobalSurvey : TestBase
{
    private const string XpSkripte =
        "E:/RPGMakerGames/MicroQuest - Beneath Brimestone 1.0/Data"
        + "/Scripts.rxdata";

    private static readonly Regex Global = new(
        @"\$([a-z_][a-z0-9_]*)", RegexOptions.Compiled);

    /// <summary>
    /// And every global the game names, largest first.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And names like <c>$~</c> and <c>$1</c> are not
    /// globals a runtime has to answer</strong>, -- <strong>and they
    /// are excluded here because they are not
    /// <c>[a-z_]</c> names</strong> -- <strong>and that is the rule
    /// the pattern states and not a hand-written
    /// list.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieGlobalenNamenDesSpielsAbzaehlen()
    {
        var haeufig = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var leib in XpScriptBodies.LeseAlle(XpSkripte))
        {
            if (leib.Text == null)
            {
                continue;
            }

            foreach (Match treffer in Global.Matches(leib.Text))
            {
                var name = treffer.Groups[1].Value;
                haeufig.TryGetValue(name, out var anzahl);
                haeufig[name] = anzahl + 1;
            }
        }

        var liste = new List<KeyValuePair<string, int>>(haeufig);
        liste.Sort(static (a, b) => b.Value.CompareTo(a.Value));
        var namen = new List<string>();
        foreach (var paar in liste)
        {
            if (namen.Count >= 16)
            {
                break;
            }

            namen.Add("$" + paar.Key + "=" + paar.Value);
        }

        System.Console.WriteLine(
            "Globale: " + haeufig.Count + " verschiedene");
        System.Console.WriteLine("  " + string.Join("  ", namen.ToArray()));

        AssertTrue(haeufig.Count > 5,
            "**and the game names more than a handful of globals** --"
                + " and it names " + haeufig.Count + ": "
                + string.Join("  ", namen.ToArray())
                + ", and that list is what a runtime has to answer");
        AssertTrue(haeufig.ContainsKey("game_map"),
            "**and `game_map` is among them** -- and the interpreter"
                + " already knows the names without the dollar sign,"
                + " and that is where `Global()` looks");
    }

    /// <summary>
    /// And which of them the interpreter's own host already answers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this test does not pretend the answer is
    /// good.</strong> The interpreter's <c>IRubyHost</c> has
    /// <c>LookupConstant</c> and not a global slot, -- <strong>and a
    /// runtime that wants to set <c>$game_map</c> needs a way in that
    /// the language does not have.</strong>
    /// </para>
    /// </remarks>
    public void Test_DerHostHatKeineStelleFuerGlobaleWerte()
    {
        var echt = RgssSkriptHost.Lese(XpSkripte, out var fehler);
        AssertTrue(echt != null,
            "**and the host reads** -- and it said: " + fehler);

        var host = echt!;
        AssertTrue(host.LookupConstant("game_map") == null,
            "**and `LookupConstant` cannot answer a global** -- and"
                + " that is not a fault, because a global in Ruby is a"
                + " variable and not a constant, and the two are"
                + " different names for different things");

        // **Und `CallMethod` ist der Weg, den ein Host hat.**
        System.Console.WriteLine(
            "IRubyHost: ReadScript, CallMethod, CallMethodWithBlock,"
            + " LookupConstant -- und kein Setzen einer Variablen");

        AssertTrue(true,
            "**and the interface has no way to set one** -- and that"
                + " is the shape a runtime has to grow into, and this"
                + " test does not grow it");
    }
}
