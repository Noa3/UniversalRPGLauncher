using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace UniversalRPG.Rm2k.Simulation;

/// <summary>Bounded, JSON-only persistence for deterministic RM2K simulation state.</summary>
public static class Rm2kSimulationSaveCodec
{
    public const int MaxPayloadBytes = 1024 * 1024;
    private const int CurrentVersion = 1;

    public sealed class SaveData
    {
        public int Version { get; set; } = CurrentVersion;
        public string GameTitle { get; set; } = "";
        public int MapId { get; set; }
        public int MapX { get; set; }
        public int MapY { get; set; }
        public byte FacingDirection { get; set; }
        public int Gold { get; set; }
        public int FrameCount { get; set; }
        public int Steps { get; set; }
        public bool Timer1Active { get; set; }
        public bool Timer2Active { get; set; }
        public int Timer1Seconds { get; set; }
        public int Timer2Seconds { get; set; }
        /// <summary>
        /// One actor saved: the six base values, then the current HP and SP.
        /// </summary>
        /// <remarks>
        /// **The bases and the current values travel together**, because a
        /// save that kept the current hit points and dropped the base would
        /// reload a hero clamped to a maximum it no longer has, and one that
        /// kept the base and dropped the current count would reload a hero at
        /// full health after he was nearly dead. A first draft had neither,
        /// and every <c>10430</c> a game did was lost at the next save.
        /// </remarks>
        public sealed class SavedActor
        {
            public int Id { get; set; }
            public int BaseMaxHp { get; set; }
            public int BaseMaxSp { get; set; }
            public int BaseAttack { get; set; }
            public int BaseDefense { get; set; }
            public int BaseSpirit { get; set; }
            public int BaseAgility { get; set; }

            /// <summary>-1 when the actor was never hurt or healed.</summary>
            public int CurrentHp { get; set; } = -1;

            /// <summary>-1 when the actor never spent or gained skill points.</summary>
            public int CurrentSp { get; set; } = -1;
        }

        /// <summary>Every actor that has a base value or a current count.</summary>
        public List<SavedActor> Actors { get; set; } = new();

        /// <summary>
        /// The skills each actor has learned, keyed by actor id.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <strong>And this field was missing</strong>, --
        /// <strong>and a save without it reloads a hero who learned
        /// nothing</strong>, -- <strong>and the battle command list
        /// comes back empty</strong>, -- <strong>and a hero in a game
        /// with three hundred skills suddenly has none of
        /// them.</strong>
        /// </para>
        /// <para>
        /// <strong>And the base values were saved and these were
        /// not</strong>, -- <strong>which is why the round trip looked
        /// complete and was not.</strong>
        /// </para>
        /// </remarks>
        /// <summary>
        /// One actor's learned skills.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <strong>And this is an object in a list and not a
        /// dictionary entry</strong>, -- <strong>because a JSON key
        /// must be a string</strong>, -- <strong>and
        /// <c>JsonSerializer</c> writes a
        /// <c>Dictionary&lt;int, ...&gt;</c> as an empty object without
        /// an error</strong>, -- <strong>and a silent loss of every
        /// learned skill is worse than a refusal.</strong>
        /// </para>
        /// </remarks>
        public sealed class SavedActorSkills
        {
            /// <summary>Which hero.</summary>
            public int ActorId { get; set; }

            /// <summary>The skill ids he has learned.</summary>
            public List<int> SkillIds { get; set; } = new();
        }

        public List<SavedActorSkills> ActorSkills { get; set; } = new();

        /// <summary>
        /// One actor's condition ids.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <strong>And a list and not a dictionary</strong>, --
        /// <strong>because a JSON key must be a string</strong>, --
        /// <strong>and <c>JsonSerializer</c> writes
        /// <c>Dictionary&lt;int, ...&gt;</c> as an empty object without
        /// an error.</strong>
        /// </para>
        /// <para>
        /// <strong>And conditions carry the story</strong>, -- <strong>
/// a hero who is poisoned, asleep or dead comes back from a save
        /// that forgot them as healthy.</strong>
        /// </para>
        /// </remarks>
        public sealed class SavedActorConditions
        {
            /// <summary>Which hero.</summary>
            public int ActorId { get; set; }

            /// <summary>The condition ids he carries.</summary>
            public List<int> ConditionIds { get; set; } = new();
        }

        public int MapWidth { get; set; }
        public int MapHeight { get; set; }
        public List<bool> PassableTiles { get; set; } = new();
        public List<bool> Switches { get; set; } = new();
        public List<int> Variables { get; set; } = new();
        public Dictionary<int, int> ItemCounts { get; set; } = new();
        public List<int> PartyMemberIds { get; set; } = new();

        /// <summary>
        /// The four access rights, and they travel together.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <strong>And <c>SetAccess</c> moves all four at once</strong>,
        /// -- <strong>because a game that forbids the menu but allows
        /// the save is one the author wrote on purpose.</strong>
        /// </para>
        /// <para>
        /// <strong>And a save that dropped them reloads a game where
        /// the author locked the player out of everything.</strong>
        /// </para>
        /// </remarks>
        public bool AllowEscape { get; set; } = true;
        public bool AllowSave { get; set; } = true;
        public bool AllowMenu { get; set; } = true;
        public bool AllowTeleport { get; set; } = true;

        /// <summary>One actor's conditions, as ids.</summary>
        public List<SavedActorConditions> ActorConditions { get; set; } = new();

        /// <summary>The common events currently running, and their counter.</summary>
        public List<int> CommonEventIds { get; set; } = new();

        /// <summary>
        /// How many common events have been started.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <strong>And this is a parallel-execution number, not a
        /// name</strong>, -- <strong>and a save that dropped it would
        /// restart the count at one and let two parallel copies of the
        /// same event share a name.</strong>
        /// </para>
        /// </remarks>
        public int CommonEventCounter { get; set; }
        public int ActiveActorIndex { get; set; }
        public string CurrentScene { get; set; } = "Menu";
        public List<string> SceneStack { get; set; } = new();
        public long SaveTimestamp { get; set; }
        public string SaveComment { get; set; } = "";
    }


    /// <summary>
    /// Every actor that has a base value or a current count, in id order.
    /// </summary>
    /// <remarks>
    /// **Only the actors that have something are written.** An entry for an
    /// actor nobody touched would be four base values the format minimums
    /// already imply, and a save full of them is a save nobody can read.
    /// </remarks>
    private static List<SaveData.SavedActor> WriteActors(GameSimulationState pState)
    {
        var list = new List<SaveData.SavedActor>();
        // A sorted walk, so two saves of the same game are byte for byte the
        // same and a diff of two saves says something.
        var ids = new List<int>(pState.ActorValues.Keys);
        foreach (var id in pState.CurrentHp.Keys)
        {
            if (!ids.Contains(id)) ids.Add(id);
        }
        foreach (var id in pState.CurrentSp.Keys)
        {
            if (!ids.Contains(id)) ids.Add(id);
        }
        ids.Sort();
        foreach (var id in ids)
        {
            var values = pState.GetOrCreateActorValues(id);
            list.Add(new SaveData.SavedActor
            {
                Id = id,
                BaseMaxHp = values.BaseMaxHp,
                BaseMaxSp = values.BaseMaxSp,
                BaseAttack = values.BaseAttack,
                BaseDefense = values.BaseDefense,
                BaseSpirit = values.BaseSpirit,
                BaseAgility = values.BaseAgility,
                CurrentHp = pState.CurrentHp.TryGetValue(id, out var hp) ? hp : -1,
                CurrentSp = pState.CurrentSp.TryGetValue(id, out var sp) ? sp : -1,
            });
        }
        return list;
    }

    /// <summary>
    /// Applies the saved actors, after the whole save was validated.
    /// </summary>
    private static void ReadActors(SaveData pData, GameSimulationState pState)
    {
        foreach (var actor in pData.Actors)
        {
            var values = new Rm2kActorValues();
            // **The setters clamp, and the clamp is what a save between these
            // bounds deserves.** A save written by a build with a higher bound
            // lands on the highest value this one holds, which is a slightly
            // weaker hero and not a broken one.
            values.AddToParameter(Rm2kActorValues.ParameterMaxHp,
                actor.BaseMaxHp - values.BaseMaxHp);
            values.AddToParameter(Rm2kActorValues.ParameterMaxSp,
                actor.BaseMaxSp - values.BaseMaxSp);
            values.AddToParameter(Rm2kActorValues.ParameterAttack,
                actor.BaseAttack - values.BaseAttack);
            values.AddToParameter(Rm2kActorValues.ParameterDefense,
                actor.BaseDefense - values.BaseDefense);
            values.AddToParameter(Rm2kActorValues.ParameterSpirit,
                actor.BaseSpirit - values.BaseSpirit);
            values.AddToParameter(Rm2kActorValues.ParameterAgility,
                actor.BaseAgility - values.BaseAgility);
            pState.ActorValues[actor.Id] = values;
            if (actor.CurrentHp >= 0) pState.CurrentHp[actor.Id] = actor.CurrentHp;
            if (actor.CurrentSp >= 0) pState.CurrentSp[actor.Id] = actor.CurrentSp;
        }
    }

    public static string Serialize(GameSimulationState pState)
    {
        ArgumentNullException.ThrowIfNull(pState);
        var json = JsonSerializer.Serialize(Capture(pState));
        if (System.Text.Encoding.UTF8.GetByteCount(json) > MaxPayloadBytes)
        {
            throw new InvalidOperationException("Simulation save exceeds the bounded payload limit.");
        }
        return json;
    }

    public static bool TryRestore(string pJson, GameSimulationState pState, out string pError)
    {
        pError = "";
        if (string.IsNullOrWhiteSpace(pJson) || System.Text.Encoding.UTF8.GetByteCount(pJson) > MaxPayloadBytes)
        {
            pError = "Save payload is empty or exceeds the bounded size limit.";
            return false;
        }
        try
        {
            var data = JsonSerializer.Deserialize<SaveData>(pJson);
            if (data == null) { pError = "Save payload is null."; return false; }
            if (data.Version != CurrentVersion) { pError = $"Unsupported save version {data.Version}."; return false; }
            Validate(data);
            Apply(data, pState);
            return true;
        }
        catch (JsonException) { pError = "Save payload is not valid JSON."; return false; }
        catch (ArgumentException exception) { pError = exception.Message; return false; }
        catch (InvalidOperationException exception) { pError = exception.Message; return false; }
    }

    /// <summary>
    /// Writes one runtime-owned JSON slot below the explicitly supplied directory.
    /// This is not an implementation of the original RM2K/RM2K3 LSD format.
    /// </summary>
    public static bool TryWriteFile(string pSaveDirectory, string pSlot, GameSimulationState pState, out string pError)
    {
        pError = "";
        if (!TryResolveSlot(pSaveDirectory, pSlot, out var path, out pError))
        {
            return false;
        }

        var temporary = path + ".tmp";
        try
        {
            ArgumentNullException.ThrowIfNull(pState);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(temporary, Serialize(pState), System.Text.Encoding.UTF8);
            File.Move(temporary, path, true);
            return true;
        }
        catch (IOException exception)
        {
            pError = $"Could not write save slot: {exception.Message}";
            return false;
        }
        catch (UnauthorizedAccessException exception)
        {
            pError = $"Could not write save slot: {exception.Message}";
            return false;
        }
        catch (ArgumentException exception)
        {
            pError = exception.Message;
            return false;
        }
        catch (InvalidOperationException exception)
        {
            pError = exception.Message;
            return false;
        }
        finally
        {
            TryDeleteTemporary(temporary);
        }
    }

    /// <summary>Reads one bounded runtime-owned JSON slot without executing its contents.</summary>
    public static bool TryReadFile(string pSaveDirectory, string pSlot, GameSimulationState pState, out string pError)
    {
        pError = "";
        if (!TryResolveSlot(pSaveDirectory, pSlot, out var path, out pError))
        {
            return false;
        }

        try
        {
            ArgumentNullException.ThrowIfNull(pState);
            if (!File.Exists(path))
            {
                pError = "Save slot does not exist.";
                return false;
            }
            return TryRestore(File.ReadAllText(path), pState, out pError);
        }
        catch (IOException exception)
        {
            pError = $"Could not read save slot: {exception.Message}";
            return false;
        }
        catch (UnauthorizedAccessException exception)
        {
            pError = $"Could not read save slot: {exception.Message}";
            return false;
        }
        catch (ArgumentException exception)
        {
            pError = exception.Message;
            return false;
        }
    }

    private static bool TryResolveSlot(string pSaveDirectory, string pSlot, out string pPath, out string pError)
    {
        pPath = "";
        pError = "";
        if (string.IsNullOrWhiteSpace(pSaveDirectory) || string.IsNullOrWhiteSpace(pSlot)
            || pSlot.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || pSlot is "." or "..")
        {
            pError = "Save directory or slot name is invalid.";
            return false;
        }

        try
        {
            var root = Path.GetFullPath(pSaveDirectory);
            var rootPrefix = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            var candidate = Path.GetFullPath(Path.Combine(root, pSlot + ".json"));
            if (!candidate.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
            {
                pError = "Save slot escapes the save directory.";
                return false;
            }
            pPath = candidate;
            return true;
        }
        catch (ArgumentException exception)
        {
            pError = exception.Message;
            return false;
        }
        catch (NotSupportedException exception)
        {
            pError = exception.Message;
            return false;
        }
        catch (PathTooLongException exception)
        {
            pError = exception.Message;
            return false;
        }
    }

    private static void TryDeleteTemporary(string pPath)
    {
        try
        {
            if (File.Exists(pPath))
            {
                File.Delete(pPath);
            }
        }
        catch (IOException)
        {
            // The primary write error is more useful than cleanup failure.
        }
        catch (UnauthorizedAccessException)
        {
            // The primary write error is more useful than cleanup failure.
        }
    }

    private static SaveData Capture(GameSimulationState pState)
    {
        var data = new SaveData
        {
            GameTitle = pState.GameTitle,
            MapId = pState.MapId, MapX = pState.MapX, MapY = pState.MapY,
            FacingDirection = pState.FacingDirection, Gold = pState.Gold,
            FrameCount = pState.FrameCount, Steps = pState.Steps,
            Timer1Active = pState.Timer1Active, Timer2Active = pState.Timer2Active,
            Timer1Seconds = pState.Timer1Seconds, Timer2Seconds = pState.Timer2Seconds,
            MapWidth = pState.MapWidth, MapHeight = pState.MapHeight,
            Actors = WriteActors(pState),
            ActiveActorIndex = pState.ActiveActorIndex, CurrentScene = pState.CurrentScene,
            SaveTimestamp = pState.SaveTimestamp, SaveComment = pState.SaveComment,
            AllowEscape = pState.AllowEscape, AllowSave = pState.AllowSave,
            AllowMenu = pState.AllowMenu,
            AllowTeleport = pState.AllowTeleport,
            CommonEventCounter = pState.CommonEventCounter,
        };
        foreach (var id in pState.CommonEventIds) data.CommonEventIds.Add(id);
        foreach (var value in pState.PassableTiles) data.PassableTiles.Add(value);
        foreach (var value in pState.Switches) data.Switches.Add(value);
        foreach (var value in pState.Variables) data.Variables.Add(value);
        foreach (var pair in pState.ItemCounts) data.ItemCounts[pair.Key] = pair.Value;
        foreach (var value in pState.PartyMemberIds) data.PartyMemberIds.Add(value);
        foreach (var value in pState.SceneStack) data.SceneStack.Add(value);

        // **Und  die  gelernten  Faehigkeiten  gehoeren  dazu.**
        for (var held = 0; held < pState.ActorSkills.Length; held++)
        {
            var menge = pState.ActorSkills[held];
            if (menge == null || menge.Count == 0)
            {
                continue;
            }

            // **Und  eine  Liste  und  kein  Woerterbuch.**
            //
            // **Und  `JsonSerializer`  schreibt  ein
            //  `Dictionary<int, ...>`  ohne  Fehler  und  ohne
            //  Inhalt** -- **es  hat  `"ActorSkills":{}`  geliefert**
            // -- **und  damit  waere  jede  gelernte  Faehigkeit  beim
            //  Speichern  verloren  gegangen,  ohne  dass  irgendetwas
            //  gemeldet  wurde.**
            //
            // **Und  JSON-Schluessel  muessen  Zeichen  sein**,
            // -- **und  ganzzahlige  Schluessel  gehoeren  in  eine
            //  Liste  aus  Objekten.**
            // **Und  `SavedActorSkills`  ist  eine  verschachtelte  Klasse
            //  von  `SaveData`** -- **und  diese  Stelle  liegt  in  der
            //  Fabrik  und  nicht  in  der  Klasse.**
            data.ActorSkills.Add(new SaveData.SavedActorSkills
            {
                ActorId = held,
                SkillIds = new List<int>(menge),
            });
        }

        // **Und  die  Bedingungen  eines  Helden  sind  genauso  ein
        //  Fortschritt  wie  seine  Faehigkeiten.**
        for (var held = 0; held < pState.ActorConditions.Length; held++)
        {
            var menge = pState.ActorConditions[held];
            if (menge == null || menge.Count == 0)
            {
                continue;
            }

            data.ActorConditions.Add(new SaveData.SavedActorConditions
            {
                ActorId = held,
                ConditionIds = new List<int>(menge),
            });
        }

        return data;
    }

    private static void Validate(SaveData pData)
    {
        if (pData.MapId < 0 || pData.MapId > GameSimulationState.MaxMapId || pData.MapWidth <= 0 || pData.MapHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(pData.MapId), "Save map metadata is outside bounds.");
        if (pData.MapWidth > 512 || pData.MapHeight > 512 || pData.MapWidth * pData.MapHeight != pData.PassableTiles.Count)
            throw new ArgumentException("Save passability dimensions are invalid.");
        if (pData.Switches.Count > GameSimulationState.MaxSwitches || pData.Variables.Count > GameSimulationState.MaxVariables)
            throw new ArgumentException("Save switch or variable data exceeds bounds.");
        if (pData.Timer1Seconds < 0 || pData.Timer1Seconds > 86400 || pData.Timer2Seconds < 0 || pData.Timer2Seconds > 86400)
            throw new ArgumentException("Save timer data is outside bounds.");
        // **Every actor entry is validated before any of them is applied**, so
        // a save with one bad row is rejected whole instead of half-restored.
        if (pData.Actors.Count > GameSimulationState.MaxActorId)
            throw new ArgumentException("Save actor data exceeds bounds.");
        foreach (var actor in pData.Actors)
        {
            if (actor.Id < 1 || actor.Id > GameSimulationState.MaxActorId)
                throw new ArgumentException("Save actor id is outside bounds.");
            if (actor.BaseMaxHp < 1 || actor.BaseMaxHp > Rm2kActorValues.MaxHitPoints
                || actor.BaseMaxSp < 0 || actor.BaseMaxSp > Rm2kActorValues.MaxHitPoints
                || actor.BaseAttack < 1 || actor.BaseAttack > Rm2kActorValues.MaxStat
                || actor.BaseDefense < 1 || actor.BaseDefense > Rm2kActorValues.MaxStat
                || actor.BaseSpirit < 1 || actor.BaseSpirit > Rm2kActorValues.MaxStat
                || actor.BaseAgility < 1 || actor.BaseAgility > Rm2kActorValues.MaxStat)
                throw new ArgumentException("Save actor base values are outside bounds.");
            // -1 is the marker for "never set", and anything below it is not.
            if (actor.CurrentHp < -1 || actor.CurrentHp > Rm2kActorValues.MaxHitPoints
                || actor.CurrentSp < -1 || actor.CurrentSp > Rm2kActorValues.MaxHitPoints)
                throw new ArgumentException("Save actor current values are outside bounds.");
        }
        if (pData.PartyMemberIds.Count > GameSimulationState.MaxPartyMembers || pData.SceneStack.Count > 64)
            throw new ArgumentException("Save party or scene data exceeds bounds.");
        if (pData.MapX < 0 || pData.MapX >= pData.MapWidth || pData.MapY < 0 || pData.MapY >= pData.MapHeight)
            throw new ArgumentException("Save player position is outside map bounds.");
    }

    private static void Apply(SaveData pData, GameSimulationState pState)
    {
        pState.ConfigureMap(pData.MapId, pData.MapWidth, pData.MapHeight, pData.PassableTiles);
        pState.MapX = pData.MapX; pState.MapY = pData.MapY; pState.FacingDirection = pData.FacingDirection;
        pState.Gold = pData.Gold; pState.FrameCount = pData.FrameCount; pState.Steps = pData.Steps;
        pState.Switches.Clear(); foreach (var value in pData.Switches) pState.Switches.Add(value);
        pState.Variables.Clear(); foreach (var value in pData.Variables) pState.Variables.Add(value);
        pState.ItemCounts.Clear(); foreach (var pair in pData.ItemCounts) pState.ItemCounts[pair.Key] = pair.Value;
        pState.PartyMemberIds.Clear(); foreach (var value in pData.PartyMemberIds) pState.PartyMemberIds.Add(value);
        pState.SceneStack.Clear(); foreach (var value in pData.SceneStack) pState.SceneStack.Add(value);

        // **Und  die  gelernten  Faehigkeiten  kommen  zurueck.**
        for (var held = 0; held < pState.ActorSkills.Length; held++)
        {
            pState.ActorSkills[held]?.Clear();
        }

        foreach (var eintrag in pData.ActorSkills)
        {
            var menge = pState.SkillsOf(eintrag.ActorId);
            foreach (var skill in eintrag.SkillIds)
            {
                menge.Add(skill);
            }
        }

        // **Und  die  Bedingungen  kommen  mit  dem  einen  Weg  zurueck,
        //  den  es  fuer  Faehigkeiten  schon  gibt.**
        for (var held = 0; held < pState.ActorConditions.Length; held++)
        {
            pState.ActorConditions[held]?.Clear();
        }

        foreach (var eintrag in pData.ActorConditions)
        {
            var menge = pState.ConditionsOf(eintrag.ActorId);
            foreach (var zustand in eintrag.ConditionIds)
            {
                menge.Add(zustand);
            }
        }

        // **Und  die  vier  Zugangsrechte  kommen  ueber  den  einen
        //  Weg,  den  die  Klasse  dafuer  anbietet.**
        pState.SetAccess(pData.AllowEscape, pData.AllowSave,
            pData.AllowMenu, pData.AllowTeleport);

        pState.CommonEventIds.Clear();
        foreach (var id in pData.CommonEventIds)
        {
            pState.CommonEventIds.Add(id);
        }

        pState.CommonEventCounter = pData.CommonEventCounter;
        pState.ActiveActorIndex = pData.ActiveActorIndex; pState.CurrentScene = pData.CurrentScene;
        pState.SaveTimestamp = pData.SaveTimestamp; pState.SaveComment = pData.SaveComment;
        // **The seconds first, and the running flag second.** A first draft
        // restored a timer with SetTimer alone, and because SetTimer used
        // to start a timer that turned every saved countdown into a
        // running one — a game that saved a paused timer and reloaded it
        // got a live one. The two operations are separate now, and this
        // is where the difference shows.
        pState.StopTimer(1); pState.StopTimer(2);
        pState.SetTimer(1, pData.Timer1Seconds);
        pState.SetTimer(2, pData.Timer2Seconds);
        if (pData.Timer1Active)
        {
            pState.StartTimer(1, pState.Timer1Visible, pState.Timer1InBattle);
        }
        if (pData.Timer2Active)
        {
            pState.StartTimer(2, pState.Timer2Visible, pState.Timer2InBattle);
        }

        ReadActors(pData, pState);
    }
}
