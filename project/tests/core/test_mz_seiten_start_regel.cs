using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using UniversalRPG.Mz;
using UniversalRPG.Tests.Framework;
using UniversalRPG.Web;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And which page of an event starts, measured against the real game.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is the rule that decides whether a game shows any
/// dialogue at all.</strong> Measured in <c>rmmz_objects.js</c>:
/// <c>Game_Event.prototype.start</c> is
/// <c>if (list &amp;&amp; list.length &gt; 1)</c> and
/// <c>Game_Event.prototype.update</c> starts a page while
/// <c>isTriggerIn([2])</c>. Trigger 2 is autorun.
/// </para>
/// <para>
/// <strong>And the previous reader had this as 3</strong>, which is the
/// <em>parallel process</em> trigger, <strong>and started nothing at map
/// setup at all</strong> -- so a game whose opening is an autorun event
/// reached its first line of dialogue only after the player had already
/// walked into something.
/// </para>
/// </remarks>
public partial class TestMzSeitenStartRegel : TestBase
{
    private const string MzSpiel = "E:/RPGMakerGames/CamelliaCoronation-Win";

    private static bool Vorhanden() => File.Exists(MzSpiel + "/data/Map002.json");

    public void Test_DieNummernSindAutomatischZweiUndNichtDrei()
    {
        // The engine's own words, from Game_Event.prototype.update:
        // isTriggerIn([2]) is the autorun test.
        AssertEq(MzSeitenStart.AusloeserAutomatisch, 2,
            "autorun is trigger 2");
        AssertEq(MzSeitenStart.AusloeserTaste, 0, "the action button is trigger 0");
        AssertEq(MzSeitenStart.AusloeserBeruehrt, 1, "touch is trigger 1");
        AssertEq(MzSeitenStart.AusloeserParallel, 3,
            "parallel process is trigger 3, and it is not started here");
    }

    public void Test_EineSeiteMitEinemBefehlStartetNicht()
    {
        // `if (list && list.length > 1)` -- one command is not a page.
        AssertFalse(MzSeitenStart.StartetAutomatisch(
                MzSeitenStart.AusloeserAutomatisch,
                MzSeitenStart.HoechstBefehleOhneStart, false, false),
            "a one-command autorun page does not start, because the engine"
            + " asks for more than one");
        AssertFalse(MzSeitenStart.StartetAutomatisch(
                MzSeitenStart.AusloeserAutomatisch, 0, false, false),
            "an empty page does not start");
        // `if (list && list.length > 1)` -- strictly greater than one, so
        // two is the smallest page that starts.
        AssertTrue(MzSeitenStart.StartetAutomatisch(
                MzSeitenStart.AusloeserAutomatisch, 2, false, false),
            $"two commands are the minimum that starts, and MindestBefehle"
            + $" boundary is {MzSeitenStart.HoechstBefehleOhneStart}");
    }

    public void Test_NurTriggerZweiStartetVonSelbst()
    {
        foreach (var ausloeser in new[]
        {
            MzSeitenStart.AusloeserTaste,
            MzSeitenStart.AusloeserBeruehrt,
            MzSeitenStart.AusloeserParallel,
        })
        {
            AssertFalse(MzSeitenStart.StartetAutomatisch(ausloeser, 20, false, false),
                $"trigger {ausloeser} does not start by itself");
        }
        AssertFalse(MzSeitenStart.StartetAutomatisch(
                MzSeitenStart.AusloeserAutomatisch, 20, true, false),
            "an erased event does not start");
        AssertFalse(MzSeitenStart.StartetAutomatisch(
                MzSeitenStart.AusloeserAutomatisch, 20, false, true),
            "a page that is already starting does not start again");
    }

    /// <summary>
    /// And a real map of this game carries an autorun event with dialogue.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the start map is the wrong place to look.</strong>
    /// Measured at Camellia Map002, the map the game starts on
    /// (<c>startMapId: 2</c>, player at 4,11): its events are two touch
    /// transfers, two touch bridges, a <c>trigger = 3</c> "Intro" and two
    /// action-button gates — <strong>and no <c>trigger = 2</c> page at
    /// all</strong>, so a test on that map would say "this game has no
    /// autorun" and be right about the map and wrong about the game.
    /// </para>
    /// <para>
    /// <strong>Map003 "Day 1" carries two of them</strong>, events 9 and 10
    /// with 211 commands each and text lines, <strong>and those are the
    /// ones a runtime that never starts an autorun page silently skips.</strong>
    /// </para>
    /// </remarks>
    public void Test_DieEchteKarteTraegtEinenAutomatischenText()
    {
        if (!Vorhanden() || !File.Exists(MzSpiel + "/data/Map003.json"))
        {
            return;
        }
        var karte = MzDataFile.Read("data/Map003.json",
            File.ReadAllBytes(MzSpiel + "/data/Map003.json"));
        var ereignisse = karte.Root.Member("events")?.Items;
        AssertTrue(ereignisse != null && ereignisse.Count > 0,
            "the start map carries events");

        var automatisch = 0;
        var automatischMitText = 0;
        foreach (var ereignis in ereignisse!)
        {
            if (ereignis.Member("pages")?.Items == null)
            {
                continue;
            }
            foreach (var seite in ereignis.Member("pages")!.Items!)
            {
                var ausloeser = seite.Member("trigger")?.IntOr(-1) ?? -1;
                var befehle = seite.Member("list")?.Items?.Count ?? 0;
                if (!MzSeitenStart.StartetAutomatisch(ausloeser, befehle, false, false))
                {
                    continue;
                }
                automatisch += 1;
                var code = seite.Member("list")!.Items!.Any(pBefehl =>
                    pBefehl.Member("code")?.IntOr(0) == 401
                    || pBefehl.Member("code")?.IntOr(0) == 405);
                if (code)
                {
                    automatischMitText += 1;
                }
            }
        }
        Console.WriteLine($"MZ Map003 'Day 1': autorun pages={automatisch} with text={automatischMitText}");
        AssertTrue(automatisch > 0,
            "this game's Day 1 map has autorun pages, which is where its"
            + " text lives; the start map has none and would prove nothing");
        AssertTrue(automatischMitText > 0,
            "and at least one of them carries a text line");
    }

    public void Test_DieBeruehrungVergleichtDieZielZelle()
    {
        var ereignisse = new List<(int, int, int)> { (7, 4, 11), (8, 5, 11) };
        AssertEq(
            string.Join(",", MzSeitenStart.BeruehrteEreignisse(ereignisse, 4, 11)),
            "7",
            "the event on the cell the player walked onto is found");
        AssertEq(
            string.Join(",", MzSeitenStart.BeruehrteEreignisse(ereignisse, 5, 11)),
            "8",
            "and the other event, when the player is on its tile");
        AssertEq(
            string.Join(",", MzSeitenStart.BeruehrteEreignisse(ereignisse, 9, 9)),
            "",
            "and nothing on an empty cell");
    }
}