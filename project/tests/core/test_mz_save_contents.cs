using System;
using System.Collections.Generic;
using System.Linq;
using UniversalRPG.Tests.Framework;
using UniversalRPG.Web;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And a save is written in the engine's own shape, and read back.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And every name and every order here is measured in the project's
/// own <c>rmmz_managers.js</c> and <c>rmmz_core.js</c>.</strong>
/// </para>
/// <code>
/// DataManager.makeSaveContents = function() {
///     const contents = {};
///     contents.system = $gameSystem;
///     contents.screen = $gameScreen;
///     contents.timer = $gameTimer;
///     contents.switches = $gameSwitches;
///     contents.variables = $gameVariables;
///     contents.selfSwitches = $gameSelfSwitches;
///     contents.actors = $gameActors;
///     contents.party = $gameParty;
///     contents.map = $gameMap;
///     contents.player = $gamePlayer;
///     return contents;
/// };
/// JsonEx._encode = function(value, depth) {
///     if (depth >= this.maxDepth) { throw new Error("Object too deep"); }
///     const type = Object.prototype.toString.call(value);
///     if (type === "[object Object]" || type === "[object Array]") {
///         const constructorName = value.constructor.name;
///         if (constructorName !== "Object" && constructorName !== "Array") {
///             value["@"] = constructorName;
///         }
///         for (const key of Object.keys(value)) {
///             value[key] = this._encode(value[key], depth + 1);
///         }
///     }
///     return value;
/// };
/// </code>
/// <para>
/// <strong>And the note in <c>_encode</c> is the reason there is no cycle
/// handling here either:</strong> <em>"The handling code for circular
/// references in certain versions of MV has been removed because it was too
/// complicated and expensive."</em>
/// </para>
/// </remarks>
public partial class TestMzSaveContents : TestBase
{
    /// <summary>
    /// And the contents carry the engine's ten entries and the objects carry
    /// their constructor names.
    /// </summary>
    public void Test_DerInhaltHatDieFormDerEngine()
    {
        var fakten = Fakten();
        var (inhalt, fehlend) = MzSaveContents.Capture(fakten, 3, 1234);

        // **And the root is a plain object, and it has no `@`.**
        AssertFalse(inhalt.Member("@") != null,
            "**and the contents themselves carry no constructor name** -- "
            + "and that is `const contents = {}` in the engine's own source");

        // **And the entries are the engine's own names, and in its order.**
        var namen = string.Join(",", inhalt.Keys);
        Console.WriteLine($"MZ save: entries are {namen}, missing {fehlend.Count}");
        AssertEq(namen, "system,switches,variables,selfSwitches,map,player",
            "**and the entries are the engine's own names** -- and the four "
            + "this runtime cannot write are left out rather than filled "
            + "with something made up");

        // **And each entry names its own class, and the name comes first.**
        var schalter = inhalt.Member("switches")!;
        AssertEq(schalter.Keys[0], "@",
            "**and `@` is the first key of an entry** -- measured: `_encode` "
            + "sets it before it walks `Object.keys`");
        AssertEq(schalter.Member("@")?.StringOr(""), "Game_Switches",
            "**and it names the class the engine built it from**");
        AssertEq(inhalt.Member("player")!.Member("@")?.StringOr(""), "Game_Player",
            "**and the player too** -- and the engine's player IS a "
            + "Game_Character, and its own name is Game_Player");

        // **And the four that are missing are named and not invented.**
        AssertEq(string.Join(",", fehlend), "screen,timer,actors,party",
            "**and the entries this runtime does not have are reported** -- "
            + "and a save that silently dropped the party would be a save "
            + "that loses a player's gold");
    }

    /// <summary>
    /// And what was written is read back, value for value.
    /// </summary>
    public void Test_EineHinUndZurueckLesungGibtDieselbenWerte()
    {
        var fakten = Fakten();
        var (inhalt, _) = MzSaveContents.Capture(fakten, 3, 1234);

        // **And the way through the file is the engine's own way**: JsonEx
        // writes it, and JsonEx reads it.
        var text = MzJson.Write(inhalt);
        Console.WriteLine($"MZ save: {text.Length} characters, "
            + $"starts {text.Substring(0, Math.Min(90, text.Length))}");
        AssertTrue(MzJson.TryParse(text, out var gelesen, out var fehler),
            "**and the written text parses again** -- and the reader said: "
                + fehler);

        AssertTrue(MzSaveContents.TryRestore(gelesen!, out var geladen, out var problem),
            "**and the contents read back** -- and the reader said: " + problem);

        AssertEq(geladen!.Switches.Count, fakten.Switches.Count(s => s.Value),
            "**and every switch came back**");
        AssertTrue(geladen.Switches.TryGetValue(5, out var an) && an,
            "**and switch 5 is on**");
        AssertEq(geladen.Variables[9], 42,
            "**and variable 9 is 42** -- and a reader that wrote a zero "
            + "would lose it");
        AssertTrue(geladen.SelfSwitches.ContainsKey("3,9,A"),
            "**and the self switch came back by its own key** -- and the key "
            + "is `map,event,channel`, which is what `makeSelfSwitchKey` "
            + "builds");
        AssertEq(geladen.MapId, 3, "**and the map is the one that was saved**");
        AssertEq(geladen.PlayerX, 4, "**and the player is where it stood**");
        AssertEq(geladen.PlayerY, 11, "**and not where the map's start is**");
        AssertEq(geladen.PlayerDirection, 2, "**and it faces what it faced**");
        AssertEq(geladen.SavefileId, 3,
            "**and the slot is written into the save itself** -- which is "
            + "`$gameSystem._savefileId`, and the game reads it back on load");
        AssertEq(geladen.FramesOnSave, 1234,
            "**and the frame it was saved at**");
    }

    /// <summary>
    /// And a hole in a JavaScript array is a hole and not a zero.
    /// </summary>
    /// <remarks>
    /// <strong>Measured:</strong> <c>Game_Switches.clear</c> sets
    /// <c>this._data = []</c>, and <c>setValue(5, true)</c> sets index 5 and
    /// leaves 0 to 4 as holes. <strong>And <c>JSON.stringify</c> writes a
    /// hole as <c>null</c></strong> -- <strong>so the array has a length, and
    /// the length is part of the data.</strong>
    /// </remarks>
    public void Test_EineLueckeImFeldIstKeineNull()
    {
        var fakten = new MzBranchFacts { Switches = { [5] = true } };
        var (inhalt, _) = MzSaveContents.Capture(fakten, 1, 0);
        var data = inhalt.Member("switches")!.Member("_data")!;

        AssertEq(data.Items.Count, 6,
            "**and the array reaches to the highest switch that is on**");
        AssertEq(data.Items.Count(x => x.Kind == MzKind.Null), 5,
            "**and the five below it are holes** -- and a reader that wrote "
            + "`false` there would write a switch the game never set");

        var text = MzJson.Write(inhalt);
        AssertTrue(text.Contains("\"_data\":[null,null,null,null,null,true]"),
            "**and that is exactly what `JSON.stringify` writes for it** -- "
            + $"and it wrote {text.Substring(text.IndexOf("_data", StringComparison.Ordinal))}");
    }

    /// <summary>
    /// And a slot the map has no event in stays a hole.
    /// </summary>
    /// <remarks>
    /// <strong>And this is not tidiness.</strong> The engine's
    /// <c>_events</c> is a sparse array indexed by event id, and a reader
    /// that packed the entries together would renumber every event on the
    /// map -- <strong>so a save written by such a reader would move figures
    /// the player never touched.</strong>
    /// </remarks>
    public void Test_EinLochInDenEreignissenBleibtEinLoch()
    {
        var figur = new MzCharacter(6, 7, 4);
        var fakten = new MzBranchFacts { Characters = { [7] = figur } };
        var (inhalt, _) = MzSaveContents.Capture(fakten, 1, 0);
        var events = inhalt.Member("map")!.Member("_events")!;

        AssertEq(events.Items.Count, 8,
            "**and the array reaches to event 7** -- so there are seven "
            + "holes before it and not none");
        AssertEq(events.Items.Count(x => x.Kind == MzKind.Null), 7,
            "**and the seven before it are holes**");

        var text = MzJson.Write(inhalt);
        Console.WriteLine($"MZ save: the map entry is "
            + $"{text.Substring(text.IndexOf("\"_events\"", StringComparison.Ordinal))}");

        // **And reading it back does not invent an event at (0,0).**
        AssertTrue(MzJson.TryParse(text, out var gelesen, out _),
            "**and it parses**");
        AssertTrue(MzSaveContents.TryRestore(gelesen!, out var geladen, out _),
            "**and it restores**");
        AssertEq(geladen!.Figures.Count, 1,
            "**and exactly one figure came back** -- and a reader that read "
            + "the seven holes as events would have eight");
        AssertTrue(geladen.Figures.TryGetValue(7, out var ort), "**and it is 7**");
        AssertEq(ort.X, 6, "**and it stood at 6**");
        AssertEq(ort.Y, 7, "**and at 7**");
        AssertEq(ort.Direction, 4, "**and it faced left**");
    }

    /// <summary>
    /// And the save file's own info is a separate small object.
    /// </summary>
    /// <remarks>
    /// <strong>Measured:</strong> <c>DataManager.makeSavefileInfo</c> builds
    /// <c>{ title, characters, faces, playtime, timestamp }</c> and
    /// <c>saveGame</c> writes it into the global info file -- <strong>so a
    /// load screen can list twenty slots without reading twenty
    /// saves.</strong>
    /// </remarks>
    public void Test_DieDateiInfoIstKleinUndEigen()
    {
        var info = MzSaveContents.Info(
            "Camellia Coronation",
            new[] { 1, 2 },
            new[] { "Actor1", "Actor2" },
            "00:12:34",
            1759872000000L);

        var text = MzJson.Write(info);
        Console.WriteLine($"MZ save: the file info is {text}");
        AssertTrue(text.Contains("\"title\":\"Camellia Coronation\""),
            "**and it carries the game's title**");
        AssertTrue(text.Contains("\"playtime\":\"00:12:34\""),
            "**and the playtime as text** -- and it is text in the engine "
            + "too, because `playtimeText` formats it");
        AssertTrue(text.Contains("\"timestamp\":1759872000000"),
            "**and the moment it was written** -- and a whole number has no "
            + "decimal point");
        AssertEq(info.Keys.Count, 5,
            "**and it is five fields and not the whole save**");
    }

    /// <summary>And a game with something in every corner of a save.</summary>
    private static MzBranchFacts Fakten()
    {
        var spieler = new MzPlayer();
        spieler.StandAt(3, 4, 11, 2);

        var figur = new MzCharacter(6, 7, 4);

        return new MzBranchFacts
        {
            Player = spieler,
            Switches = { [5] = true },
            Variables = { [9] = 42 },
            SelfSwitches = { ["3,9,A"] = true },
            Characters = { [1] = figur },
        };
    }
}
