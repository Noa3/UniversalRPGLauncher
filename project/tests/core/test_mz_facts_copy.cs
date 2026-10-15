using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Godot;
using UniversalRPG.Tests.Framework;
using UniversalRPG.Web;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The facts copy, which the runtime builds on every map load.
/// </summary>
/// <remarks>
/// <strong>And a copy that drops state is a game that forgets it.</strong> The runtime replaces
/// its facts on every map load with <c>WithCharacters</c>, which carried twenty-four of the
/// class's properties and dropped thirty-seven -- <c>Actors</c> among them, so
/// <c>$gameActors</c> was filled at start-up and thrown away the moment a map was entered. The
/// first test here is the guard: every property is either carried or named as not carried, and a
/// new one that is in neither fails.
/// </remarks>
public partial class TestMzFactsCopy : TestBase
{
    public void Test_JedeEigenschaftIstEntwederMitgenommenOderBenannt()
    {
        // **And the carried set is read from the copy itself**, not from a second list that
        // could go stale the way the copy did: the source is parsed for the names the copy
        // assigns and for the collections it fills by loop, and that set plus the declared
        // exceptions must be exactly the class's properties.
        var quelle = File.ReadAllText(
            ProjectSettings.GlobalizePath("res://src/mz/MzBranch.cs"));
        var kopfrumpf = quelle.Substring(quelle.IndexOf("public MzBranchFacts WithCharacters("));
        kopfrumpf = kopfrumpf.Substring(0, kopfrumpf.IndexOf("\n    }"));

        var mitgenommen = new HashSet<string>();
        foreach (Match treffer in Regex.Matches(kopfrumpf, @"^\s+(\w+)\s*=\s*\1\s*,",
                     RegexOptions.Multiline))
        {
            mitgenommen.Add(treffer.Groups[1].Value);
        }
        foreach (Match treffer in Regex.Matches(kopfrumpf, @"foreach \(var \w+ in (\w+)\)"))
        {
            mitgenommen.Add(treffer.Groups[1].Value);
        }

        var eigenschaften = typeof(MzBranchFacts)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(property => property.Name)
            .Where(name => name != "WithCharacters")
            .ToHashSet();

        var benannt = new HashSet<string>(MzBranchFacts.NotCarriedByWithCharacters);

        AssertTrue(mitgenommen.Count > 0,
            "**the copy's source was read** -- a parse that found nothing would make the checks "
                + "below pass for the wrong reason");

        AssertEq(mitgenommen.Intersect(benannt).Count(), 0,
            "**no property is carried and named as not carried at the same time**");

        var undeklariert = eigenschaften.Except(mitgenommen).Except(benannt)
            .OrderBy(name => name).ToArray();
        AssertEq(undeklariert.Length, 0,
            "**every property is either carried by the copy or named as an exception** -- and "
                + "these are in neither: " + string.Join(", ", undeklariert));

        var unbekannt = mitgenommen.Concat(benannt).Except(eigenschaften)
            .OrderBy(name => name).ToArray();
        AssertEq(unbekannt.Length, 0,
            "**and no list names something the class has not got** -- and these are: "
                + string.Join(", ", unbekannt));
    }

    /// <summary>
    /// And the copy really carries the state a map change must not forget.
    /// </summary>
    /// <remarks>
    /// <strong>And the identity matters as much as the value.</strong> The engine's state lives
    /// in globals, so a page that writes a name, or a delegate that keeps filling a list, must be
    /// writing the one the copy reads. A copy that rebuilt the collections with the same contents
    /// would pass a value check and still lose what a later write added -- which is why two of
    /// the assertions here compare references rather than contents.
    /// </remarks>
    public void Test_DieKopieNimmtDenZustandMit()
    {
        var fakten = new MzBranchFacts();
        fakten.TimerFrames = 123;
        fakten.TimerWorking = true;
        fakten.Begegnungszaehler = 7;
        fakten.NamensZiel = 3;
        fakten.NamensText = "Alex";
        fakten.MoviePlaying = "Intro";
        fakten.Recovered.Add(1);
        fakten.Namen[1] = "Camellia";
        fakten.AnimationAsked.Add(9);

        var kopie = fakten.WithCharacters(fakten.Characters, fakten.Player);

        AssertEq(kopie.TimerFrames, 123, "**the timer survives the copy**");
        AssertEq(kopie.TimerWorking, true, "and whether it is running");
        AssertEq(kopie.Begegnungszaehler, 7, "**and the encounter counter**");
        AssertEq(kopie.NamensZiel, 3, "and the name prompt's target");
        AssertEq(kopie.NamensText, "Alex", "and what it has collected so far");
        AssertEq(kopie.MoviePlaying, "Intro", "and the movie that is playing");
        AssertEq(kopie.Namen[1], "Camellia", "**and the names the game set**");
        AssertTrue(kopie.Recovered.Contains(1), "**and the recovered actors**");
        AssertTrue(ReferenceEquals(kopie.Namen, fakten.Namen),
            "**and the dictionary is the same one**, not a copy of it: the engine has one set "
                + "of globals, and a page that writes a name must write the one the copy reads");
        AssertTrue(kopie.AnimationAsked.Contains(9), "and the animations a page asked for");
        AssertEq(kopie.AnimationAsked.Count, fakten.AnimationAsked.Count,
            "**and that list is a copy of the contents**, not the same list: the copy fills its "
                + "own from the original, and assigning the same one made that loop write into "
                + "the collection it was reading");
        AssertTrue(ReferenceEquals(kopie.Kampf, fakten.Kampf),
            "**and the battle is the same object**, so a battle that is running is the battle "
                + "the copied facts see");
        AssertTrue(ReferenceEquals(kopie.Spiel, fakten.Spiel),
            "**and `$gameSystem` too**, which is where the save's own slot and playtime live");
    }
}
