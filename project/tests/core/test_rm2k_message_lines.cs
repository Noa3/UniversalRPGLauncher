using System.Collections.Generic;
using Godot;
using UniversalRPG.Rm2k.Interpreter;
using UniversalRPG.Rm2k.Parser;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

partial class TestRm2kMessageLines : TestBase
{
    private Rm2kParser _parser = null!;

    public override void Setup()
    {
        _parser = new Rm2kParser();
    }

    private static readonly string[] Maps =
    {
        "res://tests/fixtures/rm2k-dragon-destiny/Map0001.lmu",
        "res://tests/fixtures/rm2k-dragon-destiny/Map0100.lmu",
        "res://tests/fixtures/easyrpg-testgame/rm2000/Map0001.lmu",
        "res://tests/fixtures/easyrpg-testgame/rm2003/Map0001.lmu",
    };

    /// <summary>
    /// Both continuation codes are followed, so a message in either game keeps
    /// every line it has.
    /// </summary>
    public void Test_EveryMessageLineInTheseFourMapsIsReached()
    {
        var offen = 0;
        foreach (var path in Maps)
        {
            if (!FileAccess.FileExists(path)) { continue; }
            var map = _parser.ParseMap(path);
            if (!map.IsSuccess()) { continue; }
            var data = map.GetData();
            if (!data.TryGetValue("events", out var rawEvents)) { continue; }
            foreach (var rawEvent in rawEvents.AsGodotArray())
            {
                if (rawEvent.VariantType != Variant.Type.Dictionary) { continue; }
                var ev = rawEvent.AsGodotDictionary();
                if (!ev.TryGetValue("pages", out var rawPages)) { continue; }
                foreach (var rawPage in rawPages.AsGodotArray())
                {
                    if (rawPage.VariantType != Variant.Type.Dictionary) { continue; }
                    var page = rawPage.AsGodotDictionary();
                    if (!page.TryGetValue("commands", out var rawCommands)) { continue; }
                    var codes = new List<Godot.Collections.Dictionary>();
                    foreach (var c in rawCommands.AsGodotArray())
                    {
                        if (c.VariantType == Variant.Type.Dictionary)
                        {
                            codes.Add(c.AsGodotDictionary());
                        }
                    }
                    for (var i = 0; i < codes.Count; i++)
                    {
                        if ((int)codes[i]["code"].AsInt64() != EventInterpreter.ShowMessage)
                        {
                            continue;
                        }
                        var zeilen = 1;
                        for (var k = i + 1; k < codes.Count; k++)
                        {
                            var folge = (int)codes[k]["code"].AsInt64();
                            if (folge == EventInterpreter.ShowMessage2
                                || folge == EventInterpreter.ShowMessageLine)
                            {
                                zeilen++;
                            }
                            else
                            {
                                break;
                            }
                        }
                        offen += zeilen;
                    }
                }
            }
        }

        // **And the count the old reader produced.** The loop below is the
        // fix; this one is what shipped, and the difference is the size of the
        // bug. A test that only checks the new count is satisfied by any
        // number, and **the two numbers side by side are the evidence**.
        var alt = 0;
        foreach (var path in Maps)
        {
            if (!FileAccess.FileExists(path)) { continue; }
            var map = _parser.ParseMap(path);
            if (!map.IsSuccess()) { continue; }
            var data = map.GetData();
            if (!data.TryGetValue("events", out var rawEvents)) { continue; }
            foreach (var rawEvent in rawEvents.AsGodotArray())
            {
                if (rawEvent.VariantType != Variant.Type.Dictionary) { continue; }
                var ev = rawEvent.AsGodotDictionary();
                if (!ev.TryGetValue("pages", out var rawPages)) { continue; }
                foreach (var rawPage in rawPages.AsGodotArray())
                {
                    if (rawPage.VariantType != Variant.Type.Dictionary) { continue; }
                    var page = rawPage.AsGodotDictionary();
                    if (!page.TryGetValue("commands", out var rawCommands)) { continue; }
                    var codes = new List<Godot.Collections.Dictionary>();
                    foreach (var c in rawCommands.AsGodotArray())
                    {
                        if (c.VariantType == Variant.Type.Dictionary)
                        {
                            codes.Add(c.AsGodotDictionary());
                        }
                    }
                    for (var i = 0; i < codes.Count; i++)
                    {
                        if ((int)codes[i]["code"].AsInt64() != EventInterpreter.ShowMessage)
                        {
                            continue;
                        }
                        var zeilen = 1;
                        for (var k = i + 1; k < codes.Count; k++)
                        {
                            // **Only 20110, which is what shipped.**
                            if ((int)codes[k]["code"].AsInt64() == EventInterpreter.ShowMessage2)
                            {
                                zeilen++;
                            }
                            else
                            {
                                break;
                            }
                        }
                        alt += zeilen;
                    }
                }
            }
        }

        // **291 against 281: a difference of ten, not twenty.** There are 20
        // `1009` commands in the four maps and ten messages that carry them, so
        // the missing lines are ten and the twenty is the number of *commands*.
        //
        // **A first draft asserted a difference of 20** and failed, and the
        // failure was worth having: it is the difference between "twenty lines
        // of dialogue" and "ten messages lost half their text", and only one of
        // those is true. **The two numbers mean different things and a test
        // that conflates them reports the wrong size of the bug.**
        AssertEq(
            offen - alt, 10,
            "and the two readers differ by the ten lines that ten of these"
            + $" messages lose, which is half of each; {offen} against {alt}");
    }
}
