using System.Collections.Generic;

namespace UniversalRPG.Web;

/// <summary>
/// The contents of an MZ save file, built in the engine's own shape.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the shape is <c>DataManager.makeSaveContents</c>, and it is
/// measured and not invented:</strong>
/// </para>
/// <code>
/// contents.system       = $gameSystem;
/// contents.screen       = $gameScreen;
/// contents.timer        = $gameTimer;
/// contents.switches     = $gameSwitches;
/// contents.variables    = $gameVariables;
/// contents.selfSwitches = $gameSelfSwitches;
/// contents.actors       = $gameActors;
/// contents.party        = $gameParty;
/// contents.map          = $gameMap;
/// contents.player       = $gamePlayer;
/// </code>
/// <para>
/// <strong>And each entry is a whole object, and <c>JsonEx</c> writes the
/// name of its constructor into an <c>"@"</c> key:</strong> a save file
/// really contains <c>{"@":"Game_Switches","_data":[null,true]}</c> and not
/// a bare array. <strong>And <c>JsonEx</c> has no circular-reference
/// handling at all in MZ</strong> -- the note in its own <c>_encode</c> says
/// the code MV had "has been removed because it was too complicated and
/// expensive" -- <strong>so the game objects are a tree by design, and a
/// reader that had to break a cycle would be reading something the engine
/// never writes.</strong>
/// </para>
/// <para>
/// <strong>And this runtime writes the entries it has and names the ones it
/// does not.</strong> A save that silently dropped the party would be a save
/// that loses a player's gold, and a report is the honest answer.
/// </para>
/// </remarks>
public static class MzSaveContents
{
    /// <summary>
    /// The ten entries the engine's own <c>makeSaveContents</c> builds, in
    /// its own order.
    /// </summary>
    public static readonly IReadOnlyList<string> EngineEntries =
        new[]
        {
            "system", "screen", "timer", "switches", "variables",
            "selfSwitches", "actors", "party", "map", "player",
        };

    /// <summary>The entries this runtime can build and read back.</summary>
    public static readonly IReadOnlyList<string> WrittenEntries =
        new[] { "system", "switches", "variables", "selfSwitches", "map", "player" };

    /// <summary>
    /// Builds a save from the facts of a running game.
    /// </summary>
    /// <param name="pFacts">The state to write.</param>
    /// <param name="pSavefileId">The slot being written to.</param>
    /// <param name="pFramesOnSave">
    /// The frame the game stood at, which is <c>Game_System._framesOnSave</c>.
    /// </param>
    /// <returns>
    /// The contents, and the entries of the engine's ten that are missing
    /// from it.
    /// </returns>
    public static (MzValue Contents, IReadOnlyList<string> Missing) Capture(
        MzBranchFacts pFacts, int pSavefileId, int pFramesOnSave)
    {
        var contents = Obj(
            ("system", SystemEntry(pSavefileId, pFramesOnSave)),
            ("switches", Switches(pFacts)),
            ("variables", Variables(pFacts)),
            ("selfSwitches", SelfSwitches(pFacts)),
            ("map", Map(pFacts)),
            ("player", Player(pFacts)));

        var missing = new List<string>();
        foreach (var name in EngineEntries)
        {
            if (!contents.Members.ContainsKey(name))
            {
                missing.Add(name);
            }
        }

        return (contents, missing);
    }

    /// <summary>
    /// Reads a save back into the values a runtime applies.
    /// </summary>
    /// <param name="pContents">The parsed contents.</param>
    /// <param name="pLoaded">The values, when the contents are readable.</param>
    /// <param name="pProblem">
    /// What is wrong with them, when they are not.
    /// </param>
    /// <returns>Whether the contents were readable.</returns>
    /// <remarks>
    /// <strong>And a named entry that is missing is reported and not
    /// guessed</strong> -- the engine's own <c>extractSaveContents</c>
    /// assigns all ten and would take a <c>undefined</c> for a missing one,
    /// and a reader that did the same would wipe a running game's screen to
    /// nothing.
    /// </remarks>
    public static bool TryRestore(
        MzValue pContents,
        out MzLoadedSave? pLoaded,
        out string pProblem)
    {
        pLoaded = null;
        pProblem = "";

        if (pContents.Kind != MzKind.Object)
        {
            pProblem = "the contents are not an object, so there are no "
                + "entries to read";
            return false;
        }

        var switches = new Dictionary<int, bool>();
        var variables = new Dictionary<int, int>();
        var selfSwitches = new Dictionary<string, bool>();
        var figures = new Dictionary<int, (int X, int Y, int Direction)>();

        ReadSwitches(pContents.Member("switches"), switches);
        ReadVariables(pContents.Member("variables"), variables);
        ReadSelfSwitches(pContents.Member("selfSwitches"), selfSwitches);
        ReadFigures(pContents.Member("map"), figures);

        var system = pContents.Member("system");
        var player = pContents.Member("player");

        pLoaded = new MzLoadedSave
        {
            Switches = switches,
            Variables = variables,
            SelfSwitches = selfSwitches,
            Figures = figures,
            SavefileId = system?.Member("_savefileId")?.IntOr(0) ?? 0,
            FramesOnSave = system?.Member("_framesOnSave")?.IntOr(0) ?? 0,
            MapId = player?.Member("_mapId")?.IntOr(0) ?? 0,
            PlayerX = player?.Member("_x")?.IntOr(0) ?? 0,
            PlayerY = player?.Member("_y")?.IntOr(0) ?? 0,
            PlayerDirection = player?.Member("_direction")?.IntOr(2) ?? 2,
            Missing = MissingFrom(pContents),
        };
        return true;
    }

    /// <summary>
    /// The entries of the engine's ten that a contents object does not
    /// carry.
    /// </summary>
    public static IReadOnlyList<string> MissingFrom(MzValue pContents)
    {
        var missing = new List<string>();
        foreach (var name in EngineEntries)
        {
            if (pContents.Member(name) == null)
            {
                missing.Add(name);
            }
        }

        return missing;
    }

    /// <summary>
    /// A save's file info, which is <c>DataManager.makeSavefileInfo</c>.
    /// </summary>
    /// <remarks>
    /// <strong>And this is what the load screen draws</strong> -- the title,
    /// the party's faces, and the playtime -- and it is a separate object
    /// from the save itself, written into <c>global.rmmzsave</c> so that a
    /// load screen can list twenty slots without reading twenty saves.
    /// </remarks>
    public static MzValue Info(
        string pTitle,
        IReadOnlyList<int> pCharacterIds,
        IReadOnlyList<string> pFaces,
        string pPlaytime,
        long pTimestamp)
    {
        var characters = new MzValue(MzKind.Array);
        foreach (var id in pCharacterIds)
        {
            characters.Items.Add(Num(id));
        }

        var faces = new MzValue(MzKind.Array);
        foreach (var face in pFaces)
        {
            faces.Items.Add(Str(face));
        }

        return Obj(
            ("title", Str(pTitle)),
            ("characters", characters),
            ("faces", faces),
            ("playtime", Str(pPlaytime)),
            ("timestamp", Num(pTimestamp)));
    }

    private static MzValue SystemEntry(int pSavefileId, int pFramesOnSave) =>
        Tagged(
            "Game_System",
            ("_savefileId", Num(pSavefileId)),
            ("_framesOnSave", Num(pFramesOnSave)));

    private static MzValue Switches(MzBranchFacts pFacts) =>
        Tagged("Game_Switches", ("_data", Sparse(pFacts.Switches)));

    private static MzValue Variables(MzBranchFacts pFacts) =>
        Tagged("Game_Variables", ("_data", Sparse(pFacts.Variables)));

    private static MzValue SelfSwitches(MzBranchFacts pFacts)
    {
        var data = new MzValue(MzKind.Object);
        var keys = new List<string>(pFacts.SelfSwitches.Keys);
        keys.Sort(System.StringComparer.Ordinal);
        foreach (var key in keys)
        {
            if (pFacts.SelfSwitches[key])
            {
                data.Keys.Add(key);
                data.Members[key] = new MzValue(MzKind.Bool) { Boolean = true };
            }
        }

        return Tagged("Game_SelfSwitches", ("_data", data));
    }

    private static MzValue Map(MzBranchFacts pFacts)
    {
        var events = new MzValue(MzKind.Array);
        var ids = new List<int>(pFacts.Characters.Keys);
        ids.Sort();
        foreach (var id in ids)
        {
            var figure = pFacts.Characters[id];
            if (figure == null)
            {
                // **And a hole in `_events` stays a hole.** The engine's own
                // array is sparse -- `_events[3]` is a `Game_Event` and the
                // slots around it are `null` -- and `JSON.stringify` writes
                // those as `null`, so a reader that packed them together
                // would renumber every event on the map.
                while (events.Items.Count <= id)
                {
                    events.Items.Add(new MzValue(MzKind.Null));
                }

                continue;
            }

            while (events.Items.Count < id)
            {
                events.Items.Add(new MzValue(MzKind.Null));
            }

            events.Items.Add(Event(figure));
        }

        return Tagged(
            "Game_Map",
            ("_mapId", Num(pFacts.Player.MapId)),
            ("_events", events));
    }

    private static MzValue Event(MzCharacter pFigure) =>
        Tagged(
            "Game_Event",
            ("_x", Num(pFigure.X)),
            ("_y", Num(pFigure.Y)),
            ("_realX", Num(pFigure.X)),
            ("_realY", Num(pFigure.Y)),
            ("_direction", Num(pFigure.Direction)),
            ("_pattern", Num(pFigure.CharacterIndex)),
            ("_characterName", Str(pFigure.CharacterName)));

    private static MzValue Player(MzBranchFacts pFacts) =>
        Tagged(
            "Game_Player",
            ("_mapId", Num(pFacts.Player.MapId)),
            ("_x", Num(pFacts.Player.X)),
            ("_y", Num(pFacts.Player.Y)),
            ("_realX", Num(pFacts.Player.X)),
            ("_realY", Num(pFacts.Player.Y)),
            ("_direction", Num(pFacts.Player.Direction)));

    /// <summary>
    /// A JavaScript array with holes, which is what <c>_data</c> is.
    /// </summary>
    /// <remarks>
    /// <strong>And a hole is not the same as a zero.</strong> Measured:
    /// <c>Game_Switches.clear</c> sets <c>this._data = []</c>, and
    /// <c>setValue(5, true)</c> sets index 5 and leaves 0 to 4 as holes,
    /// and <c>JSON.stringify</c> writes those as <c>null</c>. <strong>And
    /// <c>value</c> reads <c>!!this._data[switchId]</c>, so a hole and a
    /// false are the same to the engine but not to a save file</strong> --
    /// and the length is part of the data.
    /// </remarks>
    private static MzValue Sparse(Dictionary<int, bool> pValues)
    {
        var array = new MzValue(MzKind.Array);
        var highest = -1;
        foreach (var id in pValues.Keys)
        {
            if (pValues[id] && id > highest)
            {
                highest = id;
            }
        }

        for (var i = 0; i <= highest; i++)
        {
            array.Items.Add(
                pValues.TryGetValue(i, out var wert) && wert
                    ? new MzValue(MzKind.Bool) { Boolean = true }
                    : new MzValue(MzKind.Null));
        }

        return array;
    }

    private static MzValue Sparse(Dictionary<int, int> pValues)
    {
        var array = new MzValue(MzKind.Array);
        var highest = -1;
        foreach (var id in pValues.Keys)
        {
            if (pValues[id] != 0 && id > highest)
            {
                highest = id;
            }
        }

        for (var i = 0; i <= highest; i++)
        {
            array.Items.Add(
                pValues.TryGetValue(i, out var wert) && wert != 0
                    ? Num(wert)
                    : new MzValue(MzKind.Null));
        }

        return array;
    }

    private static void ReadSwitches(MzValue? pEntry, Dictionary<int, bool> pInto)
    {
        var data = pEntry?.Member("_data");
        if (data == null || data.Kind != MzKind.Array)
        {
            return;
        }

        for (var i = 0; i < data.Items.Count; i++)
        {
            if (data.Items[i].Kind == MzKind.Bool && data.Items[i].Boolean)
            {
                pInto[i] = true;
            }
        }
    }

    private static void ReadVariables(MzValue? pEntry, Dictionary<int, int> pInto)
    {
        var data = pEntry?.Member("_data");
        if (data == null || data.Kind != MzKind.Array)
        {
            return;
        }

        for (var i = 0; i < data.Items.Count; i++)
        {
            if (data.Items[i].Kind == MzKind.Number)
            {
                pInto[i] = data.Items[i].IntOr(0);
            }
        }
    }

    private static void ReadSelfSwitches(
        MzValue? pEntry, Dictionary<string, bool> pInto)
    {
        var data = pEntry?.Member("_data");
        if (data == null || data.Kind != MzKind.Object)
        {
            return;
        }

        foreach (var key in data.Keys)
        {
            if (data.Members[key].Kind == MzKind.Bool && data.Members[key].Boolean)
            {
                pInto[key] = true;
            }
        }
    }

    private static void ReadFigures(
        MzValue? pEntry, Dictionary<int, (int X, int Y, int Direction)> pInto)
    {
        var events = pEntry?.Member("_events");
        if (events == null || events.Kind != MzKind.Array)
        {
            return;
        }

        for (var i = 0; i < events.Items.Count; i++)
        {
            var eintrag = events.Items[i];
            if (eintrag.Kind != MzKind.Object)
            {
                // **And null is not event zero.** A hole in the array is a
                // slot the engine has no event in, and a reader that read it
                // as an event at (0,0) would move a figure that does not
                // exist.
                continue;
            }

            pInto[i] = (
                eintrag.Member("_x")?.IntOr(0) ?? 0,
                eintrag.Member("_y")?.IntOr(0) ?? 0,
                eintrag.Member("_direction")?.IntOr(2) ?? 2);
        }
    }

    private static MzValue Tagged(
        string pConstructor, params (string Key, MzValue Value)[] pMembers)
    {
        var value = new MzValue(MzKind.Object);

        // **And `@` comes first, because `_encode` writes it first.**
        // Measured: `_encode` sets `value["@"] = constructorName` and only
        // then walks `Object.keys`, so the key is the first one in the
        // output -- and a save file's own bytes are the measured thing.
        value.Keys.Add("@");
        value.Members["@"] = Str(pConstructor);
        foreach (var (key, member) in pMembers)
        {
            value.Keys.Add(key);
            value.Members[key] = member;
        }

        return value;
    }

    private static MzValue Obj(params (string Key, MzValue Value)[] pMembers)
    {
        // **And the contents themselves carry no `@`.**
        //
        // Measured: `makeSaveContents` starts with `const contents = {}` and
        // only fills its entries, so `_encode` finds a plain object and
        // writes no constructor name for it. **A reader that tagged the
        // root would write `{"@":"Object",...}` and a game would still read
        // it -- and a save file's own bytes are the measured thing.**
        var value = new MzValue(MzKind.Object);
        foreach (var (key, member) in pMembers)
        {
            value.Keys.Add(key);
            value.Members[key] = member;
        }

        return value;
    }

    private static MzValue Num(double pValue) =>
        new(MzKind.Number) { Number = pValue };

    private static MzValue Str(string pValue) =>
        new(MzKind.String) { Text = pValue };
}

/// <summary>
/// What a save file holds, read back into values a runtime applies.
/// </summary>
public sealed class MzLoadedSave
{
    public Dictionary<int, bool> Switches { get; init; } = new();

    public Dictionary<int, int> Variables { get; init; } = new();

    public Dictionary<string, bool> SelfSwitches { get; init; } = new();

    /// <summary>
    /// The position of every event on the saved map, by event id.
    /// </summary>
    public Dictionary<int, (int X, int Y, int Direction)> Figures { get; init; } = new();

    public int SavefileId { get; init; }

    public int FramesOnSave { get; init; }

    public int MapId { get; init; }

    public int PlayerX { get; init; }

    public int PlayerY { get; init; }

    public int PlayerDirection { get; init; } = 2;

    /// <summary>
    /// The engine's entries this save does not carry.
    /// </summary>
    public IReadOnlyList<string> Missing { get; init; } = new List<string>();
}
