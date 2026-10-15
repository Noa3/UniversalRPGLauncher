using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using UniversalRPG.Rm2k.Parser;
using UniversalRPG.Rm2k.Presentation;
using UniversalRPG.Rm2k.Simulation;

namespace UniversalRPG.Rm2k.Simulation;

/// <summary>Bounded, JSON-only persistence for deterministic RM2K simulation state.</summary>
public static class Rm2kSimulationSaveCodec
{
    public const int MaxPayloadBytes = 1024 * 1024;

    /// <summary>
    /// And the extension a runtime-owned slot really has.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is not the original LSD format</strong>, so it is not
    /// the original LSD extension either. The writer, the reader and every
    /// caller that lists or searches slots read this one constant, because a
    /// listing that guesses the extension hides every slot the game wrote.
    /// </para>
    /// </remarks>
    public const string SlotExtension = ".json";

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
        /// <summary>The warp points the game wrote, per map.</summary>
        /// <remarks>
        /// <para>
        /// <strong>And a list and not a dictionary</strong>, --
        /// <strong>because <c>JsonSerializer</c> writes
        /// <c>Dictionary&lt;int, ...&gt;</c> as an empty object without
        /// an error</strong>, -- <strong>and this was measured:
        /// <c>JSON nennt TeleportTargets: False</c>.</strong>
        /// </para>
        /// <para>
        /// <strong>And Dragon Destiny writes 2879 of them</strong>, --
        /// <strong>and a save that drops them reloads a game whose
        /// warps the author wrote are gone</strong>, -- <strong>and a
        /// warp that needs a switch to be open is the sharpest case,
        /// because dropping it makes a secret entrance
        /// vanish.</strong>
        /// </para>
        /// </remarks>
        public List<SavedWarpPoint> TeleportTargets { get; set; } = new();

        /// <summary>
        /// One map's warp point.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <strong>And <c>RequiresSwitchOn</c> is not "use a
        /// switch"</strong>, -- <strong>it is "the switch must be
        /// on"</strong>, -- <strong>and a reader that read it as the
        /// first would make every conditional warp
        /// unconditional</strong>, -- <strong>and a secret entrance
        /// would open at the start of the game.</strong>
        /// </para>
        /// </remarks>
        public sealed class SavedWarpPoint
        {
            /// <summary>Which map this point is on.</summary>
            public int MapId { get; set; }

            /// <summary>Where it leads.</summary>
            public int TargetMapId { get; set; }

            /// <summary>The column.</summary>
            public int X { get; set; }

            /// <summary>The row.</summary>
            public int Y { get; set; }

            /// <summary>Whether the switch has to be on for this to count.</summary>
            public bool RequiresSwitchOn { get; set; }

            /// <summary>The switch this point depends on.</summary>
            public int SwitchId { get; set; }
        }

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

        /// <summary>
        /// Screen presentation state: pictures, tint, flash, shake, weather,
        /// and transitions. Captures the full <c>Game_Screen</c> equivalent.
        /// </summary>
        public SavedPresentation Presentation { get; set; } = new();

        /// <summary>
        /// Vehicle state: positions, types, and boarding info for the three
        /// vehicles. From <c>Game_Vehicle</c> and <c>Game_Player::GetOnVehicle</c>.
        /// </summary>
        public List<SavedVehicle> Vehicles { get; set; } = new();

        /// <summary>
        /// Active event move routes, keyed by event id. From
        /// <c>Game_Character::UpdateMoveRoute</c>.
        /// </summary>
        public List<SavedEventRoute> EventRoutes { get; set; } = new();

        /// <summary>
        /// Self-switches for RM2K3, keyed by "mapId_eventId_switch".
        /// </summary>
        public Dictionary<string, bool> SelfSwitches { get; set; } = new();
    }

    public sealed class SavedPresentation
    {
        public List<SavedPicture> Pictures { get; set; } = new();
        public bool IsTintActive { get; set; }
        public int TintRed { get; set; }
        public int TintFinishRed { get; set; }
        public int TintFinishGreen { get; set; }
        public int TintFinishBlue { get; set; }
        public int TintFinishSaturation { get; set; }
        public int TintGreen { get; set; }
        public int TintBlue { get; set; }
        public int TintSaturation { get; set; }
        public int TintFramesRemaining { get; set; }
        public int PendingTransition { get; set; }
        public int PendingTransitionDirection { get; set; }
        public bool IsFlashActive { get; set; }
        public int FlashRed { get; set; }
        public int FlashGreen { get; set; }
        public int FlashBlue { get; set; }
        public int FlashAlpha { get; set; }
        public int FlashFramesRemaining { get; set; }
        /// <summary>
        /// The flash's decaying level, which is what is drawn.
        /// </summary>
        /// <remarks>
        /// **And the level has to be saved with the frames**, for the same reason
        /// the tint's target is: a load in the middle of a flash that kept only the
        /// frames would come back with a level of zero, and the flash the player was
        /// watching would be gone. The engine does not save its flash at all (its own
        /// comment calls that an RPG_RT bug), so saving the pair is already more than
        /// it does -- but a half saved flash is worse than none.
        /// </remarks>
        public double FlashCurrentLevel { get; set; }
        public bool IsShakeActive { get; set; }
        public int ShakeStrength { get; set; }
        public int ShakeSpeed { get; set; }
        public int ShakeFramesRemaining { get; set; }
        public int WeatherType { get; set; }
        public int WeatherStrength { get; set; }
    }

    public sealed class SavedPicture
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public bool FixedToMap { get; set; }
        public int Magnify { get; set; }
        public int TopTransparency { get; set; }
        public int BottomTransparency { get; set; }
        public bool UseTransparentColor { get; set; }
        public int Red { get; set; }
        public int Green { get; set; }
        public int Blue { get; set; }
        public int Saturation { get; set; }
        public int EffectMode { get; set; }
        public int EffectPower { get; set; }
        public bool IsMoving { get; set; }
        // Nullable for version-1 snapshots written before movement origins were stored.
        public int? MoveStartX { get; set; }
        public int? MoveStartY { get; set; }
        public int MoveTargetX { get; set; }
        public int MoveTargetY { get; set; }
        public int MoveFramesLeft { get; set; }
        public int MoveTotalFrames { get; set; }
    }

    public sealed class SavedVehicle
    {
        public int VehicleType { get; set; }
        public int MapId { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Direction { get; set; }
        public int MoveSpeed { get; set; }
        public int RemainingAscent { get; set; }
        public int RemainingDescent { get; set; }
    }

    public sealed class SavedEventRoute
    {
        public int EventId { get; set; }
        public List<SavedMoveCommand> Commands { get; set; } = new();
        public bool Repeat { get; set; }
        public bool Skippable { get; set; }
        public int MoveSpeed { get; set; }
        public int MoveFrequency { get; set; }
        public int CurrentIndex { get; set; }
        public int StartIndex { get; set; }
        public bool Finished { get; set; }
        public bool Active { get; set; }
        public bool Overwritten { get; set; }
        public int MoveFailureCount { get; set; }
        public int Direction { get; set; }
    }

    public sealed class SavedMoveCommand
    {
        public int CommandId { get; set; }
        public string ParameterString { get; set; } = "";
        public int ParameterA { get; set; }
        public int ParameterB { get; set; }
        public int ParameterC { get; set; }
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
            var candidate = Path.GetFullPath(Path.Combine(root, pSlot + SlotExtension));
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

        // **Und  die  Sprungpunkte  gehoeren  dazu.**
        foreach (var eintrag in pState.TeleportTargets)
        {
            foreach (var punkt in eintrag.Value)
            {
                data.TeleportTargets.Add(new SaveData.SavedWarpPoint
                {
                    MapId = eintrag.Key,
                    TargetMapId = punkt.MapId,
                    X = punkt.X,
                    Y = punkt.Y,
                    RequiresSwitchOn = punkt.RequiresSwitchOn,
                    SwitchId = punkt.SwitchId,
                });
            }
        }
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

        // **Presentation state: pictures, tint, flash, shake, weather, transitions.**
        if (pState.Presentation != null)
        {
            var pres = pState.Presentation;
            data.Presentation = new SavedPresentation
            {
                IsTintActive = pres.IsTintActive,
                TintRed = pres.TintRed,
                TintGreen = pres.TintGreen,
                TintBlue = pres.TintBlue,
                TintSaturation = pres.TintSaturation,
                // **And the target of a running fade is saved with it**, because a
                // load in the middle of a tint has to keep fading towards the same
                // colour: without it the screen would arrive at whatever the next
                // tick happened to interpolate towards.
                TintFinishRed = pres.TintFinishRed,
                TintFinishGreen = pres.TintFinishGreen,
                TintFinishBlue = pres.TintFinishBlue,
                TintFinishSaturation = pres.TintFinishSaturation,
                TintFramesRemaining = pres.TintFramesRemaining,
                PendingTransition = (int)pres.PendingTransition,
                PendingTransitionDirection = (int)pres.PendingTransitionDirection,
                IsFlashActive = pres.IsFlashActive,
                FlashRed = pres.FlashRed,
                FlashGreen = pres.FlashGreen,
                FlashBlue = pres.FlashBlue,
                FlashAlpha = pres.FlashAlpha,
                FlashFramesRemaining = pres.FlashFramesRemaining,
                FlashCurrentLevel = pres.FlashCurrentLevel,
                IsShakeActive = pres.IsShakeActive,
                ShakeStrength = pres.ShakeStrength,
                ShakeSpeed = pres.ShakeSpeed,
                ShakeFramesRemaining = pres.ShakeFramesRemaining,
                WeatherType = pres.WeatherType,
                WeatherStrength = pres.WeatherStrength,
            };
            foreach (var pic in pres.Pictures.Values)
            {
                var isMoving = pres.MovingPictures.ContainsKey(pic.Id);
                var move = isMoving ? pres.MovingPictures[pic.Id] : null;
                data.Presentation.Pictures.Add(new SavedPicture
                {
                    Id = pic.Id,
                    Name = pic.Name,
                    X = pic.X,
                    Y = pic.Y,
                    Width = pic.Width,
                    Height = pic.Height,
                    FixedToMap = pic.FixedToMap,
                    Magnify = pic.Magnify,
                    TopTransparency = pic.TopTransparency,
                    BottomTransparency = pic.BottomTransparency,
                    UseTransparentColor = pic.UseTransparentColor,
                    Red = pic.Red,
                    Green = pic.Green,
                    Blue = pic.Blue,
                    Saturation = pic.Saturation,
                    EffectMode = pic.EffectMode,
                    EffectPower = pic.EffectPower,
                    IsMoving = isMoving,
                    MoveStartX = move?.StartX,
                    MoveStartY = move?.StartY,
                    MoveTargetX = move?.TargetX ?? 0,
                    MoveTargetY = move?.TargetY ?? 0,
                    MoveFramesLeft = move?.FramesLeft ?? 0,
                    MoveTotalFrames = move?.TotalFrames ?? 0,
                });
            }
        }

        // **Vehicle state.**
        foreach (var veh in pState.Vehicles)
        {
            data.Vehicles.Add(new SavedVehicle
            {
                VehicleType = veh.VehicleType,
                MapId = veh.MapId,
                X = veh.X,
                Y = veh.Y,
                Direction = veh.Direction,
                MoveSpeed = veh.MoveSpeed,
                RemainingAscent = veh.RemainingAscent,
                RemainingDescent = veh.RemainingDescent,
            });
        }

        // **Event move routes.**
        if (pState.EventRoutes != null)
        {
            foreach (var kvp in pState.EventRoutes)
            {
                var route = kvp.Value;
                var savedRoute = new SavedEventRoute
                {
                    EventId = kvp.Key,
                    Repeat = route.Repeat,
                    Skippable = route.Skippable,
                    MoveSpeed = route.MoveSpeed,
                    MoveFrequency = route.MoveFrequency,
                    CurrentIndex = route.CurrentIndex,
                    StartIndex = route.StartIndex,
                    Finished = route.Finished,
                    Active = route.Active,
                    Overwritten = route.Overwritten,
                    MoveFailureCount = route.MoveFailureCount,
                    Direction = route.Direction,
                };
                foreach (var cmd in route.Commands)
                {
                    savedRoute.Commands.Add(new SavedMoveCommand
                    {
                        CommandId = cmd.CommandId,
                        ParameterString = cmd.ParameterString,
                        ParameterA = cmd.ParameterA,
                        ParameterB = cmd.ParameterB,
                        ParameterC = cmd.ParameterC,
                    });
                }
                data.EventRoutes.Add(savedRoute);
            }
        }

        // **Self-switches.**
        if (pState.SelfSwitches != null)
        {
            foreach (var kvp in pState.SelfSwitches)
            {
                data.SelfSwitches[kvp.Key] = kvp.Value;
            }
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

        // **Presentation validation.**
        if (pData.Presentation != null)
        {
            var pres = pData.Presentation;
            if (pres.TintFramesRemaining < 0 || pres.TintFramesRemaining > PresentationState.MaxEffectFrames)
                throw new ArgumentException("Save tint frames outside bounds.");
            if (pres.FlashFramesRemaining < 0 || pres.FlashFramesRemaining > PresentationState.MaxEffectFrames)
                throw new ArgumentException("Save flash frames outside bounds.");
            if (pres.ShakeFramesRemaining < 0 || pres.ShakeFramesRemaining > PresentationState.MaxEffectFrames)
                throw new ArgumentException("Save shake frames outside bounds.");
            if (pres.ShakeStrength < 0 || pres.ShakeStrength > PresentationState.MaxShakeStrength)
                throw new ArgumentException("Save shake strength outside bounds.");
            if (pres.ShakeSpeed < 0 || pres.ShakeSpeed > PresentationState.MaxShakeSpeed)
                throw new ArgumentException("Save shake speed outside bounds.");
            if (pres.WeatherType < 0 || pres.WeatherType > PresentationState.MaxWeatherType)
                throw new ArgumentException("Save weather type outside bounds.");
            if (pres.WeatherStrength < 0 || pres.WeatherStrength > PresentationState.MaxWeatherStrength)
                throw new ArgumentException("Save weather strength outside bounds.");
            foreach (var pic in pres.Pictures)
            {
                if (pic.Id < 1 || pic.Id > PresentationState.MaxPictures)
                    throw new ArgumentException("Save picture id outside bounds.");
                if (pic.Width < 0 || pic.Width > PresentationState.MaxPictureDimension
                    || pic.Height < 0 || pic.Height > PresentationState.MaxPictureDimension)
                    throw new ArgumentException("Save picture dimensions outside bounds.");
                if (pic.Magnify < 0 || pic.Magnify > PresentationState.MaxMagnifyPercent)
                    throw new ArgumentException("Save picture magnification outside bounds.");
                if (pic.TopTransparency < 0 || pic.TopTransparency > PresentationState.MaxTransparencyPercent)
                    throw new ArgumentException("Save picture top transparency outside bounds.");
                if (pic.BottomTransparency < 0 || pic.BottomTransparency > PresentationState.MaxTransparencyPercent)
                    throw new ArgumentException("Save picture bottom transparency outside bounds.");
                if (pic.EffectMode < 0 || pic.EffectMode > PresentationState.MaxEffectMode)
                    throw new ArgumentException("Save picture effect mode outside bounds.");
                if (pic.Saturation < 0 || pic.Saturation > PresentationState.MaxSaturationPercent)
                    throw new ArgumentException("Save picture saturation outside bounds.");
                if (pic.IsMoving)
                {
                    // **A movement that claims to be running has to carry a
                    // coherent timeline**, because the codec restores it as
                    // live interpolation state: a total of zero has no frame
                    // to interpolate over, a remaining count outside the total
                    // cannot be reached, and a finished movement (no frames
                    // left) is removed by the presentation itself and must not
                    // come back as a moving picture.
                    if (pic.MoveTotalFrames < 1 || pic.MoveTotalFrames > PresentationState.MaxEffectFrames)
                        throw new ArgumentException("Save picture movement total frames outside bounds.");
                    if (pic.MoveFramesLeft < 1 || pic.MoveFramesLeft > pic.MoveTotalFrames)
                        throw new ArgumentException("Save picture movement remaining frames outside bounds.");
                    // **The origin is what makes the trajectory resumable.** A
                    // version-1 snapshot written before origins were stored has
                    // none, and inventing one from the current position would
                    // silently change the path the picture is already on.
                    if (pic.MoveStartX == null || pic.MoveStartY == null)
                        throw new ArgumentException("Save picture movement is missing its origin.");
                }
            }
        }

        // **Vehicle validation.**
        if (pData.Vehicles != null)
        {
            foreach (var veh in pData.Vehicles)
            {
                if (!Rm2kVehicle.IsKnownType(veh.VehicleType))
                    throw new ArgumentException("Save vehicle type unknown.");
            }
        }

        // **Event route validation.**
        if (pData.EventRoutes != null)
        {
            foreach (var route in pData.EventRoutes)
            {
                if (route.EventId < 1 || route.EventId > GameSimulationState.MaxEvents)
                    throw new ArgumentException("Save event route id outside bounds.");
                if (route.Commands.Count > Rm2kMoveRouteDecoder.MaxMoveCommands)
                    throw new ArgumentException("Save event route command count exceeds limit.");
            }
        }
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

        // **Und  die  Sprungpunkte  kommen  zurueck**, -- **und  die
        //  Liste  nach  Karte  wird  wieder  aufgebaut**, -- **denn
        //  das  Woerterbuch  ist  die  Form,  die  die  Klasse
        //  benutzt.**
        pState.TeleportTargets.Clear();
        foreach (var eintrag in pData.TeleportTargets)
        {
            if (!pState.TeleportTargets.TryGetValue(
                    eintrag.MapId, out var liste))
            {
                liste = new List<GameSimulationState.TeleportTarget>();
                pState.TeleportTargets[eintrag.MapId] = liste;
            }

            liste.Add(new GameSimulationState.TeleportTarget
            {
                MapId = eintrag.TargetMapId,
                X = eintrag.X,
                Y = eintrag.Y,
                RequiresSwitchOn = eintrag.RequiresSwitchOn,
                SwitchId = eintrag.SwitchId,
            });
        }
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

        // **Presentation state: pictures, tint, flash, shake, weather, transitions.**
        if (pData.Presentation != null && pState.Presentation != null)
        {
            var pres = pData.Presentation;
            var target = pState.Presentation;
            target.IsTintActive = pres.IsTintActive;
            target.TintRed = pres.TintRed;
            target.TintGreen = pres.TintGreen;
            target.TintBlue = pres.TintBlue;
            target.TintSaturation = pres.TintSaturation;
            target.TintFinishRed = pres.TintFinishRed;
            target.TintFinishGreen = pres.TintFinishGreen;
            target.TintFinishBlue = pres.TintFinishBlue;
            target.TintFinishSaturation = pres.TintFinishSaturation;
            target.TintFramesRemaining = pres.TintFramesRemaining;
            target.PendingTransition = (Rm2kTransition)pres.PendingTransition;
            target.PendingTransitionDirection = (Rm2kTransitionDirection)pres.PendingTransitionDirection;
            target.IsFlashActive = pres.IsFlashActive;
            target.FlashRed = pres.FlashRed;
            target.FlashGreen = pres.FlashGreen;
            target.FlashBlue = pres.FlashBlue;
            target.FlashAlpha = pres.FlashAlpha;
            target.FlashFramesRemaining = pres.FlashFramesRemaining;
            target.FlashCurrentLevel = pres.FlashCurrentLevel;
            target.IsShakeActive = pres.IsShakeActive;
            target.ShakeStrength = pres.ShakeStrength;
            target.ShakeSpeed = pres.ShakeSpeed;
            target.ShakeFramesRemaining = pres.ShakeFramesRemaining;
            target.WeatherType = pres.WeatherType;
            target.WeatherStrength = pres.WeatherStrength;

            target.Pictures.Clear();
            target.MovingPictures.Clear();
            foreach (var pic in pres.Pictures)
            {
                var picture = new PictureState
                {
                    Id = pic.Id,
                    Name = pic.Name,
                    X = pic.X,
                    Y = pic.Y,
                    Width = pic.Width,
                    Height = pic.Height,
                    FixedToMap = pic.FixedToMap,
                    Magnify = pic.Magnify,
                    TopTransparency = pic.TopTransparency,
                    BottomTransparency = pic.BottomTransparency,
                    UseTransparentColor = pic.UseTransparentColor,
                    Red = pic.Red,
                    Green = pic.Green,
                    Blue = pic.Blue,
                    Saturation = pic.Saturation,
                    EffectMode = pic.EffectMode,
                    EffectPower = pic.EffectPower,
                };
                target.Pictures[pic.Id] = picture;
                if (pic.IsMoving)
                {
                    target.MovingPictures[pic.Id] = new PresentationState.PictureMove
                    {
                        StartX = pic.MoveStartX ?? picture.X,
                        StartY = pic.MoveStartY ?? picture.Y,
                        TargetX = pic.MoveTargetX,
                        TargetY = pic.MoveTargetY,
                        TotalFrames = pic.MoveTotalFrames,
                        FramesLeft = pic.MoveFramesLeft,
                    };
                }
            }
        }

        // **Vehicle state.**
        if (pData.Vehicles != null)
        {
            pState.Vehicles.Clear();
            foreach (var veh in pData.Vehicles)
            {
                var vehicle = new Rm2kVehicleState(veh.VehicleType, veh.MapId, veh.X, veh.Y)
                {
                    Direction = veh.Direction,
                    MoveSpeed = veh.MoveSpeed,
                    RemainingAscent = veh.RemainingAscent,
                    RemainingDescent = veh.RemainingDescent,
                };
                pState.Vehicles.Add(vehicle);
            }
        }

        // **Event move routes.**
        if (pData.EventRoutes != null && pState.EventRoutes != null)
        {
            pState.EventRoutes.Clear();
            foreach (var savedRoute in pData.EventRoutes)
            {
                var route = new Rm2kMoveRouteState(
                    savedRoute.Commands.ConvertAll(cmd => new Rm2kMap.MoveCommand
                    {
                        CommandId = cmd.CommandId,
                        ParameterString = cmd.ParameterString,
                        ParameterA = cmd.ParameterA,
                        ParameterB = cmd.ParameterB,
                        ParameterC = cmd.ParameterC,
                    }),
                    savedRoute.Repeat,
                    savedRoute.Skippable)
                {
                    MoveSpeed = savedRoute.MoveSpeed,
                    MoveFrequency = savedRoute.MoveFrequency,
                    CurrentIndex = savedRoute.CurrentIndex,
                    StartIndex = savedRoute.StartIndex,
                    Finished = savedRoute.Finished,
                    Active = savedRoute.Active,
                    Overwritten = savedRoute.Overwritten,
                    MoveFailureCount = savedRoute.MoveFailureCount,
                    Direction = savedRoute.Direction,
                };
                pState.EventRoutes[savedRoute.EventId] = route;
            }
        }

        // **Self-switches.**
        if (pData.SelfSwitches != null && pState.SelfSwitches != null)
        {
            pState.SelfSwitches.Clear();
            foreach (var kvp in pData.SelfSwitches)
            {
                pState.SelfSwitches[kvp.Key] = kvp.Value;
            }
        }

        ReadActors(pData, pState);
    }
}
