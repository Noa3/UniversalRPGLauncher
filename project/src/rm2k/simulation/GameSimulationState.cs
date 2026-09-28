using System;
using System.Collections.Generic;
using Godot;
using UniversalRPG.Rm2k.Rendering;

namespace UniversalRPG.Rm2k.Simulation;

/// <summary>
/// Deterministic simulation state model for RPG Maker 2000/2003 games.
/// All fields mirror the documented RM2K/LCF internal state layout.
/// No JavaScript, DLL, or native execution.
/// </summary>
public sealed class GameSimulationState
{
    public const int MaxPartyMembers = 4;
    public const int MaxTroopMembers = 8;
    public const int MaxActorId = 50000;
    public const int MaxMapId = 1000;
    public const int MaxSwitches = 50000;
    public const int MaxVariables = 50000;
    public const int MinActorLevel = 1;
    public const int MaxActorLevel = 99;
    public const int MaxActorExp = 999999;
    public const int MaxActorNameLength = 64;

    public string GameTitle { get; init; } = "";
    public int MapId { get; set; } = 0;
    public int MapX { get; set; } = 0;
    public int MapY { get; set; } = 0;
    public byte FacingDirection { get; set; } = 2;
    public int Gold { get; set; } = 0;
    public int FrameCount { get; set; } = 0;
    public int Steps { get; set; } = 0;

    /// <summary>
    /// The unspent part of the current tile step, in
    /// <see cref="Rm2kStepBudget.ScreenTileSize"/> units. Zero while the hero
    /// stands still. A started move fills it, and every update subtracts the
    /// per frame amount, which is what makes a move take sixteen updates at
    /// the default move speed instead of one call.
    /// </summary>
    public int RemainingStep { get; set; } = 0;

    /// <summary>
    /// The character walk animation state, verified from
    /// <c>Game_Character::anim_frame</c> and <c>anim_count</c>. These are not
    /// the tile animation counters: they drive the character sprites only, and
    /// they rotate over four values, the fourth of which is drawn as the
    /// middle frame.
    /// </summary>
    public int CharacterFrame { get; set; } = Rm2kCharacterAnimation.FrameMiddle;

    /// <summary>
    /// <c>Game_Character::anim_count</c>: ticks accumulated towards the next
    /// visible frame change.
    /// </summary>
    public int CharacterAnimCount { get; set; } = 0;

    /// <summary>
    /// The hero move speed, one based because the upstream animation tables
    /// are one based. The default in an RPG Maker 2000 database is 3.
    /// </summary>
    public int HeroMoveSpeed { get; set; } = 3;

    /// <summary>
    /// Advances the character walk animation by one update tick, using
    /// <see cref="Rm2kCharacterAnimation.Update"/>. The hero counts
    /// down its stop count while standing, so a stopped hero reaches the
    /// stationary limit; a moving hero is continuous and reaches the lower
    /// continuous limit instead.
    /// </summary>
    public void UpdateCharacterAnimation(bool pMoving)
    {
        if (RemainingStep > 0)
        {
            (RemainingStep, _) = Rm2kStepBudget.Advance(RemainingStep, HeroMoveSpeed);
        }
        var updated = Rm2kCharacterAnimation.Update(
            CharacterFrame,
            CharacterAnimCount,
            pMoving ? 0 : 1,
            HeroMoveSpeed,
            pAnimated: true,
            pContinuous: pMoving);
        CharacterFrame = updated.Frame;
        CharacterAnimCount = updated.Count;
    }
    public int FrameRate { get; set; } = 60;

    /// <summary>Chipset <c>animation_type</c>: 0 reciprocating, 1 cyclic.</summary>
    public int ChipsetAnimationType { get; set; } = Rm2kChipset.AnimTypeReciprocating;

    /// <summary>Chipset <c>animation_speed</c>: 0 means "not animated".</summary>
    public int ChipsetAnimationSpeed { get; set; } = 0;

    /// <summary>
    /// Animation step for a map tile id at the current <see cref="FrameCount"/>,
    /// following the verified Player dispatch order.
    /// </summary>
    public int GetChipAnimationStep(int pChipId)
    {
        return Rm2kChipset.ChipAnimationStep(
            pChipId, FrameCount, ChipsetAnimationType, ChipsetAnimationSpeed);
    }

    /// <summary>Chipset terrain table (162 entries) for the loaded map.</summary>
    public int[] TerrainData { get; set; } = [];

    /// <summary>Tile substitution for the loaded map; identity when unknown.</summary>
    public Rm2kTileSubstitution? TileSubstitution { get; set; }

    /// <summary>
    /// Terrain tag of a map tile, following verified
    /// <c>Game_Map::GetTerrainTag</c>: the lower layer decides. Coordinates
    /// outside the map use the terrain of the first lower tile, as RPG_RT does.
    /// </summary>
    public int GetTerrainTagAt(int pX, int pY)
    {
        var lower = LowerLayer;
        if (lower == null || lower.Length == 0)
        {
            return Rm2kChipset.DefaultTerrainTag;
        }
        var index = 0;
        if (pX >= 0 && pY >= 0 && pX < MapWidth && pY < MapHeight)
        {
            index = pX + pY * MapWidth;
        }
        if (index >= lower.Length)
        {
            return Rm2kChipset.DefaultTerrainTag;
        }
        return Rm2kTileSubstitution.GetTerrainTag(lower[index], TerrainData, TileSubstitution);
    }

    /// <summary>Lower map layer tile ids, used for terrain and passability lookups.</summary>
    public int[]? LowerLayer { get; set; }

    /// <summary>Upper map layer tile ids, used for counter tile lookups.</summary>
    public int[]? UpperLayer { get; set; }

    /// <summary>Upper passability table of the map's chipset, when verified.</summary>
    public byte[]? UpperPassability { get; set; }

    /// <summary>
    /// Verified Game_Map::IsCounter for a map tile: an upper tile that resolves
    /// to a chipset entry carrying the counter flag.
    /// </summary>
    public bool IsCounterAt(int pX, int pY)
    {
        var upper = UpperLayer;
        if (upper == null || MapWidth <= 0)
        {
            return false;
        }
        var (x, y) = Wrap(pX, pY);
        var index = x + y * MapWidth;
        if (index < 0 || index >= upper.Length)
        {
            return false;
        }
        return Rm2kChipset.IsCounterTile(upper[index], UpperPassability, TileSubstitution);
    }

    /// <summary>
    /// Verified Game_Map::XwithDirection/YwithDirection: the tile in front of
    /// the given position, with the looping map wrap applied.
    /// </summary>
    public (int X, int Y) FrontTile(int pX, int pY, byte pDirection)
    {
        var x = pX;
        var y = pY;
        switch (pDirection)
        {
            case 2: y++; break;
            case 4: x--; break;
            case 6: x++; break;
            case 8: y--; break;
        }
        return Wrap(x, y);
    }

    /// <summary>Wraps a coordinate onto a looping map, like Game_Map::RoundX/RoundY.</summary>
    private (int X, int Y) Wrap(int pX, int pY)
    {
        var x = pX;
        var y = pY;
        if (MapWidth > 0)
        {
            x = ((x % MapWidth) + MapWidth) % MapWidth;
        }
        if (MapHeight > 0)
        {
            y = ((y % MapHeight) + MapHeight) % MapHeight;
        }
        return (x, y);
    }

    public bool Timer1Active { get; private set; }
    public bool Timer2Active { get; private set; }
    public int Timer1Seconds { get; private set; }
    public int Timer2Seconds { get; private set; }

    /// <summary>
    /// Whether timer 1 is drawn, from <c>StartTimer</c>'s first flag.
    /// </summary>
    public bool Timer1Visible { get; private set; } = true;

    /// <summary>Whether timer 1 keeps running during a battle.</summary>
    public bool Timer1InBattle { get; private set; } = true;

    /// <summary>Whether timer 2 is drawn, from the same flag.</summary>
    public bool Timer2Visible { get; private set; } = true;

    /// <summary>Whether timer 2 keeps running during a battle.</summary>
    public bool Timer2InBattle { get; private set; } = true;
    public bool IsPaused { get; set; }
    public bool IsMenuOpen { get; set; }
    public bool IsSaveEnabled { get; set; } = true;
    public bool IsTransferPending { get; set; }
    public int PendingMapId { get; set; } = 0;
    public int PendingX { get; set; } = 0;
    public int PendingY { get; set; } = 0;

    public int MapWidth { get; private set; }
    public int MapHeight { get; private set; }
    public Godot.Collections.Array<bool> PassableTiles { get; init; } = new();

    /// <summary>
    /// The battle commands an actor has, keyed by actor id, from
    /// <c>Game_Actor::ChangeBattleCommands</c> and command 1009.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A list, not a set of flags, because the command removes as well as adds:
    /// <c>parameters[3] != 0</c> is the add flag, and the fixture carries
    /// <c>[1,4,10,0]</c> — a removal.
    /// </para>
    /// <para>
    /// <strong>An actor with no entry has every command the database gives
    /// them</strong>, which is the RM2K default and the reason an empty list
    /// here does not mean "the actor can do nothing". A reader that stored an
    /// empty list as the actor's commands would take every ability away the
    /// moment command 1009 ran. <em>Absent is not empty.</em>
    /// </para>
    /// </remarks>
    public Godot.Collections.Dictionary<int, Godot.Collections.Array<int>>
        BattleCommands { get; init; } = new();

    /// <summary>
    /// The battle commands an actor has, or null when they still have the
    /// database's set.
    /// </summary>
    /// <remarks>
    /// The null is the answer, and it is not a bug: <c>GetActor</c> in the
    /// reference hands back a list that is null until something changes it, and
    /// the battle code checks for exactly that. Returning an empty list instead
    /// would be a claim about the actor that nothing measured.
    /// </remarks>
    public Godot.Collections.Array<int>? GetActorBattleCommands(int pActorId)
    {
        if (pActorId < 1 || pActorId > MaxActorId)
        {
            return null;
        }
        return BattleCommands.TryGetValue(pActorId, out var list) ? list : null;
    }

    /// <summary>
    /// Adds or removes one battle command for one actor, from
    /// <c>Game_Actor::ChangeBattleCommands(bool add, int command_id)</c>.
    /// </summary>
    /// <remarks>
    /// <strong>Adding a command that is already there changes nothing, and
    /// removing one that is not there changes nothing.</strong> A reader that
    /// appended unconditionally would grow the list every time a game asked for
    /// a command the actor already had, and a reader that removed a missing one
    /// without complaint would have nothing to say about a typo in the editor.
    /// Both outcomes are reported here rather than being silent.
    /// </remarks>
    /// <returns>True when the actor's list changed.</returns>
    public bool ChangeActorBattleCommand(int pActorId, int pCommandId, bool pAdd)
    {
        if (pActorId < 1 || pActorId > MaxActorId)
        {
            return false;
        }
        if (!BattleCommands.TryGetValue(pActorId, out var list) || list == null)
        {
            list = new Godot.Collections.Array<int>();
            BattleCommands[pActorId] = list;
        }
        var vorhanden = list.Contains(pCommandId);
        if (pAdd && vorhanden)
        {
            return false;
        }
        if (!pAdd && !vorhanden)
        {
            return false;
        }
        if (pAdd)
        {
            list.Add(pCommandId);
        }
        else
        {
            list.Remove(pCommandId);
        }
        return true;
    }

    /// <summary>
    /// The three vehicles on the current map, from <c>Game_Vehicle</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>These were not in the simulation state until K-094, and that is
    /// the fault the card was written for.</strong> <c>Rm2kDecisionTurn</c>,
    /// <c>Rm2kVehicleBoarding</c> and <c>Rm2kVehicleState</c> were implemented
    /// and mutation checked, and the runtime kept its own <c>_vehicles</c> list
    /// in the plugin instead — so the boarding rules had nowhere to read from
    /// and the decision key never called them.
    /// </para>
    /// <para>
    /// The list holds three entries in the order boat, ship, airship once a map
    /// is loaded, so a caller can index by <c>Rm2kVehicle</c> type. It is empty
    /// before that, which is not an error: a game that has not loaded a map has
    /// no vehicles, and <c>GetOnOffVehicle</c> returns false there.
    /// </para>
    /// </remarks>
    public List<Rm2kVehicleState> Vehicles { get; } = new();

    /// <summary>
    /// The player's vehicle boarding state, or null before a map is loaded.
    /// </summary>
    /// <remarks>
    /// Null means "not aboard anything", which is the same as
    /// <c>aboard = 0</c> in the save data. It is not lazily created by a
    /// reader: the player turn creates one when a map is loaded and a decision
    /// key is pressed, so a game that never touches a vehicle never allocates
    /// one.
    /// </remarks>
    public Rm2kVehicleBoarding? Boarding { get; set; }

    /// <summary>Per-tile direction masks (Rm2kChipset.Pass* bits); authoritative for movement.</summary>
    public Godot.Collections.Array<byte> PassabilityMasks { get; init; } = new();

    // Switches (bool or byte for RM2000 compatibility)
    public Godot.Collections.Array<bool> Switches { get; init; } = new();

    // Variables (int)
    public Godot.Collections.Array<int> Variables { get; init; } = new();

    // Inventory counts keyed by RM2K item ID.
    public Godot.Collections.Dictionary<int, int> ItemCounts { get; init; } = new();

    // Party members (max 4)
    public Godot.Collections.Array<int> PartyMemberIds { get; init; } = new();
    public int ActiveActorIndex { get; set; } = 0;

    // Actors (mutable battle stats)
    public Godot.Collections.Dictionary<int, Godot.Collections.Dictionary> ActorState { get; init; } = new();

    /// <summary>
    /// The base battle values each actor has, from <c>10430</c>.
    /// </summary>
    /// <remarks>
    /// <strong>Base and current are separate, and this is the base.</strong>
    /// The reference has <c>SetBaseMaxHp</c> and <c>SetMaxHp</c> as
    /// different calls, because <c>10430</c> changes the base — which
    /// survives a level change and a save — while a buff changes the current
    /// value and must not. <em>A reader that stored the current value would
    /// let a saved game keep a buff that ended three maps ago.</em>
    /// </remarks>
    /// <remarks>
    /// **This is a plain dictionary and not a Godot one**, because
    /// <c>Godot.Collections.Dictionary</c> is a Variant container and a
    /// class is not a Variant. A first draft used the Godot type and the
    /// compiler refused it with GD0301.
    /// </remarks>
    public System.Collections.Generic.Dictionary<int, Rm2kActorValues> ActorValues { get; init; } = new();

    /// <summary>The current hit points of each actor, from <c>10460</c>.</summary>
    /// <remarks>
    /// <strong>Keyed separately from the base</strong> for the reason above,
    /// and absent means the actor is at its maximum — which is what the
    /// reference does, because a database row has no current HP and the
    /// engine fills it from the base on first read.
    /// </remarks>
    public Godot.Collections.Dictionary<int, int> CurrentHp { get; init; } = new();

    /// <summary>The current skill points of each actor, from <c>10470</c>.</summary>
    public Godot.Collections.Dictionary<int, int> CurrentSp { get; init; } = new();

    /// <summary>
    /// The base values of an actor, creating the entry on first use.
    /// </summary>
    public Rm2kActorValues GetOrCreateActorValues(int pActorId)
    {
        if (pActorId < 1 || pActorId > MaxActorId)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pActorId), "Actor id is outside RM2K bounds.");
        }
        if (!ActorValues.TryGetValue(pActorId, out var values))
        {
            values = new Rm2kActorValues();
            ActorValues[pActorId] = values;
        }
        return values;
    }

    /// <summary>
    /// An actor's current hit points, or the base when nothing was set.
    /// </summary>
    /// <remarks>
    /// The fallback is the base maximum and not a number of its own, because
    /// that is what the engine does: an actor nobody has hurt is at its
    /// maximum, and a reader that stored a separate full value would need to
    /// keep the two in step forever.
    /// </remarks>
    public int GetActorCurrentHp(int pActorId)
    {
        if (pActorId < 1 || pActorId > MaxActorId) return 0;
        return CurrentHp.TryGetValue(pActorId, out var hp)
            ? hp
            : GetOrCreateActorValues(pActorId).BaseMaxHp;
    }

    /// <summary>An actor's current skill points, or the base when nothing was set.</summary>
    public int GetActorCurrentSp(int pActorId)
    {
        if (pActorId < 1 || pActorId > MaxActorId) return 0;
        return CurrentSp.TryGetValue(pActorId, out var sp)
            ? sp
            : GetOrCreateActorValues(pActorId).BaseMaxSp;
    }


    /// <summary>Whether the player sprite is hidden, from 11310.</summary>
    /// <remarks>
    /// <strong>The command inverts its parameter</strong>: a first draft
    /// mapped a non-zero to visible, which gets a hide command right and
    /// a show command wrong — and a game whose only use is to hide a
    /// sprite works until the first time it shows one.
    /// </remarks>
    public bool PlayerIsHidden { get; set; }

    /// <summary>
    /// Whether the player is standing through a wall, which 11310 clears.
    /// </summary>
    /// <remarks>
    /// The reference calls <c>ResetThrough</c> right after hiding the
    /// player, with its own comment "RPG_RT does this here" — so a
    /// player who walked through a wall and is then hidden does not stay
    /// standing in the wall.
    /// </remarks>
    public bool PlayerIsThrough { get; set; }

    // Troop (active battle)
    public int ActiveTroopId { get; set; } = -1;
    public Godot.Collections.Array<Godot.Collections.Dictionary> TroopMembers { get; init; } = new();
    public bool IsBattleActive { get; set; }
    public int BattleTurn { get; set; }
    public int BattlePhase { get; set; } // 0=initial, 1=player, 2=enemy, 3=reward, 4=escape, -1=none

    // Common events (parallel execution)
    public Godot.Collections.Array<int> CommonEventIds { get; init; } = new();
    public int CommonEventCounter { get; set; }

    // Scene stack
    public Godot.Collections.Array<string> SceneStack { get; init; } = new();
    public string CurrentScene { get; set; } = "";

    /// <summary>
    /// Whether the game carries the Maniac patch, from
    /// <c>Player::IsPatchManiac</c>.
    /// </summary>
    /// <remarks>
    /// <strong>This is the switch that decides whether a value is a plain
    /// number or a packed mode field.</strong> EasyRPG's
    /// <c>ValueOrVariableBitfield</c> opens with
    /// <c>if (!IsPatchManiac()) return com.parameters[val_idx];</c> — without
    /// the patch every value is simply its own parameter, and the extra
    /// parameter a patched command carries holds four two-bit mode fields
    /// instead. <em>Reading a patched command without this flag does not give
    /// the wrong answer; it gives the parameters as written and calls them the
    /// volume.</em>
    /// </remarks>
    public bool SupportsManiacPatch { get; set; }

    /// <summary>
    /// Whether the game declares the RPG2K3 E commands, which is the gate on
    /// all five menu commands.
    /// </summary>
    /// <remarks>
    /// <strong>This is read from the game, never assumed.</strong> EasyRPG's
    /// <c>Player::IsRPG2k3ECommands()</c> decides it from the save data's
    /// runtime flags, and every one of the five commands does nothing at all
    /// when the answer is no. A reader that defaulted it to true would open
    /// menus in a 2000 game that never asked for one, and a reader that
    /// defaulted it to false would refuse them in a 2003 game. <em>The default
    /// is false because a game that has not said yes has not said yes.</em>
    /// </remarks>
    public bool SupportsRpg2k3ECommands { get; set; }

    /// <summary>Whether 5002 asked the game to exit.</summary>
    public bool ExitRequested { get; set; }

    /// <summary>
    /// Whether the ATB gauge waits for the player's turn, toggled by 5003.
    /// </summary>
    public bool AtbWaitMode { get; set; } = true;

    /// <summary>Whether 5004 asked the display to change, not a state of it.</summary>
    /// <remarks>
    /// <strong>A request, not a result.</strong> The engine asks the display
    /// layer and the display layer may refuse — EasyRPG checks
    /// <c>IsOptionVisible</c> and <c>IsLocked</c> first and logs
    /// "not supported on this platform". A boolean that claimed to be the
    /// screen state would be a claim this reader cannot keep.
    /// </remarks>
    public bool FullscreenRequested { get; set; }

    // Audio positions
    /// <summary>
    /// The audio the commands ask for.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>This replaces four position doubles that nothing read and
    /// nothing wrote.</strong> They were the residue of a plan for playback this
    /// repository has not built, and a double no command moves is a claim about
    /// time that nothing keeps.
    /// </para>
    /// <para>
    /// What is here instead is what the format holds: the current track per
    /// channel, the fade state, and the one memorised BGM that <c>11530</c> and
    /// <c>11540</c> are about. <strong>It is data, not sound</strong> — there
    /// is no player behind it, and nothing here claims a track can be heard.
    /// </para>
    /// </remarks>
    public Rm2kAudioState Audio { get; } = new();

    // Save state
    public long SaveTimestamp { get; set; }
    public string SaveComment { get; set; } = "";

    // Debug diagnostics
    public Godot.Collections.Array<string> Diagnostics { get; init; } = new();

    public void AddDiagnostic(string pMessage)
    {
        if (Diagnostics.Count < 100)
        {
            Diagnostics.Add(pMessage);
        }
    }

    public void ClearDiagnostics()
    {
        Diagnostics.Clear();
    }

    /// <summary>
    /// Returns the mutable per-actor record, creating bounded RM2K defaults.
    /// Keys are always present so callers never observe a partial record.
    /// </summary>
    public Godot.Collections.Dictionary GetOrCreateActorState(int pActorId)
    {
        if (pActorId < 1 || pActorId > MaxActorId)
        {
            throw new ArgumentOutOfRangeException(nameof(pActorId), "Actor id is outside RM2K bounds.");
        }
        if (!ActorState.TryGetValue(pActorId, out var state) || state == null)
        {
            state = new Godot.Collections.Dictionary
            {
                { "level", MinActorLevel },
                { "exp", 0 },
                { "name", "" },
            };
            ActorState[pActorId] = state;
        }
        else
        {
            if (!state.ContainsKey("level")) state["level"] = MinActorLevel;
            if (!state.ContainsKey("exp")) state["exp"] = 0;
            if (!state.ContainsKey("name")) state["name"] = "";
        }
        return state;
    }

    public int GetActorLevel(int pActorId)
    {
        if (pActorId < 1 || pActorId > MaxActorId || !ActorState.TryGetValue(pActorId, out var state) || state == null)
        {
            return MinActorLevel;
        }
        return state.ContainsKey("level") ? state["level"].AsInt32() : MinActorLevel;
    }

    public void SetActorLevel(int pActorId, int pLevel)
    {
        var state = GetOrCreateActorState(pActorId);
        state["level"] = Math.Clamp(pLevel, MinActorLevel, MaxActorLevel);
    }

    public int GetActorExp(int pActorId)
    {
        if (pActorId < 1 || pActorId > MaxActorId || !ActorState.TryGetValue(pActorId, out var state) || state == null)
        {
            return 0;
        }
        return state.ContainsKey("exp") ? state["exp"].AsInt32() : 0;
    }

    public void SetActorExp(int pActorId, int pExp)
    {
        var state = GetOrCreateActorState(pActorId);
        state["exp"] = Math.Clamp(pExp, 0, MaxActorExp);
    }

    public string GetActorName(int pActorId)
    {
        if (pActorId < 1 || pActorId > MaxActorId || !ActorState.TryGetValue(pActorId, out var state) || state == null)
        {
            return "";
        }
        return state.ContainsKey("name") ? state["name"].AsString() : "";
    }

    public void SetActorName(int pActorId, string pName)
    {
        var state = GetOrCreateActorState(pActorId);
        var name = pName ?? "";
        state["name"] = name.Length > MaxActorNameLength ? name[..MaxActorNameLength] : name;
    }

    private int _timer1TickRemainder;
    private int _timer2TickRemainder;

    /// <summary>
    /// The longest a timer may hold, from <c>SetTimer</c>'s own bound in the
    /// format and 86400 seconds being a day.
    /// </summary>
    public const int MaxTimerSeconds = 86400;

    /// <summary>
    /// Sets a timer's seconds <em>without starting it</em>, from
    /// <c>Game_Party::SetTimer</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>This used to start the timer, and that was a real fault.</strong>
    /// The reference has three commands in one: <c>SetTimer</c> writes the
    /// seconds, <c>StartTimer</c> starts it and takes the visible and battle
    /// flags, and <c>StopTimer</c> stops it. A reader that started on set
    /// collapsed the first two, and a game that wrote <c>SetTimer</c> to arm a
    /// countdown it would start later <em>started it immediately</em> — which is
    /// the exact difference between a timer that counts and one that does not.
    /// </para>
    /// </remarks>
    /// <returns>False when the id or the seconds were out of range.</returns>
    public bool SetTimer(int pTimerId, int pSeconds)
    {
        if (pTimerId is not (1 or 2) || pSeconds < 0 || pSeconds > MaxTimerSeconds)
        {
            return false;
        }
        if (pTimerId == 1) { Timer1Seconds = pSeconds; _timer1TickRemainder = 0; }
        else { Timer2Seconds = pSeconds; _timer2TickRemainder = 0; }
        return true;
    }

    /// <summary>
    /// Starts a timer and says whether it is shown and whether it runs in
    /// battle, from <c>Game_Party::StartTimer</c>.
    /// </summary>
    /// <remarks>
    /// The two flags are separate because a game can want a timer it hides from
    /// the player and one that stops for a battle — and **collapsing them into
    /// one would make "hidden" and "not in battle" the same choice.**
    /// </remarks>
    /// <returns>False when the id was out of range.</returns>
    public bool StartTimer(int pTimerId, bool pVisible, bool pInBattle)
    {
        if (pTimerId is not (1 or 2))
        {
            return false;
        }
        if (pTimerId == 1)
        {
            Timer1Active = true; Timer1Visible = pVisible; Timer1InBattle = pInBattle;
            _timer1TickRemainder = 0;
        }
        else
        {
            Timer2Active = true; Timer2Visible = pVisible; Timer2InBattle = pInBattle;
            _timer2TickRemainder = 0;
        }
        return true;
    }

    /// <summary>
    /// Stops a timer, from <c>Game_Party::StopTimer</c>.
    /// </summary>
    /// <remarks>
    /// <strong>It does not reset the seconds, and that is the point.</strong> A
    /// game that stops a timer to show the count and then starts it again
    /// expects the count to still be there. This also stops throwing for an id
    /// it does not know: the reference's own switch has no arm for one, and a
    /// reader that threw would turn a stale timer id into a dead event.
    /// </remarks>
    /// <returns>False when the id was out of range.</returns>
    public bool StopTimer(int pTimerId)
    {
        if (pTimerId == 1) { Timer1Active = false; return true; }
        if (pTimerId == 2) { Timer2Active = false; return true; }
        return false;
    }

    public void AdvanceTimers(int pSimulationTicks)
    {
        if (pSimulationTicks < 0) throw new ArgumentOutOfRangeException(nameof(pSimulationTicks));
        var timer1Seconds = Timer1Seconds;
        var timer1Active = Timer1Active;
        AdvanceTimer(ref timer1Seconds, ref timer1Active, ref _timer1TickRemainder, pSimulationTicks);
        Timer1Seconds = timer1Seconds;
        Timer1Active = timer1Active;
        var timer2Seconds = Timer2Seconds;
        var timer2Active = Timer2Active;
        AdvanceTimer(ref timer2Seconds, ref timer2Active, ref _timer2TickRemainder, pSimulationTicks);
        Timer2Seconds = timer2Seconds;
        Timer2Active = timer2Active;
    }

    private static void AdvanceTimer(ref int pSeconds, ref bool pActive, ref int pRemainder, int pTicks)
    {
        if (!pActive) return;
        pRemainder += pTicks;
        var elapsedSeconds = pRemainder / 60;
        pRemainder %= 60;
        pSeconds = Math.Max(0, pSeconds - elapsedSeconds);
        if (pSeconds == 0) pActive = false;
    }

    public void ConfigureMap(int pMapId, int pWidth, int pHeight, IEnumerable<bool> pPassableTiles)
    {
        var tiles = new List<byte>(checked(pWidth * pHeight));
        foreach (var passable in pPassableTiles)
        {
            // Legacy all-directions view: a passable tile is walkable from any
            // cardinal side, matching the previous boolean contract.
            tiles.Add(passable ? Rm2kChipset.AllDirections : (byte)0);
        }
        ConfigureMap(pMapId, pWidth, pHeight, tiles);
    }

    /// <summary>
    /// Configures the map with verified per-direction passability masks
    /// (Rm2kChipset.Pass* bits) so one-way tiles behave like RM2K.
    /// </summary>
    public void ConfigureMap(int pMapId, int pWidth, int pHeight, IEnumerable<byte> pDirectionMasks)
    {
        if (pMapId < 0 || pMapId > MaxMapId || pWidth <= 0 || pHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pWidth), "Map identity and dimensions are outside simulation bounds.");
        }
        var expected = checked(pWidth * pHeight);
        var tiles = new List<byte>(expected);
        foreach (var mask in pDirectionMasks)
        {
            if (tiles.Count == expected)
            {
                throw new ArgumentException("Passability data contains more tiles than the map.", nameof(pDirectionMasks));
            }
            tiles.Add(mask);
        }
        if (tiles.Count != expected)
        {
            throw new ArgumentException("Passability data does not cover the complete map.", nameof(pDirectionMasks));
        }
        MapId = pMapId;
        MapWidth = pWidth;
        MapHeight = pHeight;
        PassabilityMasks.Clear();
        foreach (var mask in tiles)
        {
            PassabilityMasks.Add(mask);
        }
        PassableTiles.Clear();
        for (var index = 0; index < tiles.Count; index++)
        {
            PassableTiles.Add(tiles[index] == Rm2kChipset.AllDirections);
        }
        MapX = Math.Clamp(MapX, 0, pWidth - 1);
        MapY = Math.Clamp(MapY, 0, pHeight - 1);
    }

    /// <summary>True when the tile is walkable in the requested direction.</summary>
    /// <summary>
    /// Overrides the passability masks for a test. A route that is refused at a
    /// wall needs a map with a wall, and the real passability comes from the
    /// database's chipset, so a test cannot describe one through a file without
    /// inventing a chipset. This replaces the masks outright and is a no-op on
    /// the shipped path, which is why it lives behind a ForTest name.
    /// </summary>
    public void OverridePassabilityMasksForTest(IEnumerable<byte> pDirectionMasks)
    {
        PassabilityMasks.Clear();
        foreach (var mask in pDirectionMasks)
        {
            PassabilityMasks.Add(mask);
        }
    }

    public bool IsPassableInDirection(int pX, int pY, byte pDirectionBit)
    {
        if (pX < 0 || pY < 0 || pX >= MapWidth || pY >= MapHeight)
        {
            return false;
        }
        var index = pX + pY * MapWidth;
        return index < PassabilityMasks.Count && (PassabilityMasks[index] & pDirectionBit) == pDirectionBit;
    }

    public bool TryMove(int pDeltaX, int pDeltaY)
    {
        if (Math.Abs(pDeltaX) + Math.Abs(pDeltaY) != 1)
        {
            AddDiagnostic("Movement requires exactly one cardinal tile step.");
            return false;
        }
        var targetX = MapX + pDeltaX;
        var targetY = MapY + pDeltaY;
        FacingDirection = (byte)(pDeltaX > 0 ? 6 : pDeltaX < 0 ? 4 : pDeltaY > 0 ? 2 : 8);
        if (MapWidth <= 0 || MapHeight <= 0 || targetX < 0 || targetX >= MapWidth || targetY < 0 || targetY >= MapHeight)
        {
            AddDiagnostic("Movement blocked by map bounds.");
            return false;
        }
        var directionBit = Rm2kChipset.DirectionBit(pDeltaX, pDeltaY);
        // Verified Game_Map::IsPassable: bit_from is the direction leaving the
        // current tile and bit_to is the opposite direction on the target tile.
        // Both are checked, so a one way tile or a blocked tile the player
        // stands on both refuse the step. Checking only the target let a player
        // walk out of an impassable tile, which the Player never allows.
        var reverseBit = Rm2kChipset.DirectionBit(-pDeltaX, -pDeltaY);
        if (!IsPassableInDirection(MapX, MapY, directionBit))
        {
            AddDiagnostic("Movement blocked by the passability of the tile being left.");
            return false;
        }
        if (!IsPassableInDirection(targetX, targetY, reverseBit))
        {
            AddDiagnostic("Movement blocked by tile passability.");
            return false;
        }
        // Game_Character::Move sets the logical tile to the target immediately
        // and fills remaining_step to SCREEN_TILE_SIZE, so the sprite walks
        // across the tile it just entered rather than snapping. The drawn
        // position comes from that budget, so filling it here is what makes the
        // move take HeroMoveSpeed worth of updates instead of a single call.
        MapX = targetX;
        MapY = targetY;
        RemainingStep = Rm2kStepBudget.ScreenTileSize;
        Steps += 1;
        return true;
    }

    public void Reset()
    {
        MapId = 0; MapX = 0; MapY = 0; FacingDirection = 2;
        Gold = 0; FrameCount = 0; Steps = 0;
        // A half finished step must not survive into a new game, or the hero
        // would start walking on a tile it is not standing on.
        RemainingStep = 0;
        // A half finished boarding must not survive into a new game, or the
        // player would start halfway into a boat they never boarded. The
        // vehicles go with it: they belong to a map's start node, so the last
        // game's boat is not this game's boat.
        Vehicles.Clear();
        Boarding = null;
        CharacterFrame = Rm2kCharacterAnimation.FrameMiddle;
        CharacterAnimCount = 0;
        HeroMoveSpeed = 3;
        Timer1Active = false; Timer2Active = false; Timer1Seconds = 0; Timer2Seconds = 0;
        Timer1Visible = true; Timer2Visible = true;
        Timer1InBattle = true; Timer2InBattle = true; _timer1TickRemainder = 0; _timer2TickRemainder = 0;
        IsPaused = false; IsMenuOpen = false; IsSaveEnabled = true;
        IsTransferPending = false; PendingMapId = 0; PendingX = 0; PendingY = 0; ActiveActorIndex = 0;
        MapWidth = 0; MapHeight = 0; PassableTiles.Clear(); PassabilityMasks.Clear();
        TerrainData = []; TileSubstitution = null; LowerLayer = null;
        UpperLayer = null; UpperPassability = null;
        Switches.Clear(); Variables.Clear(); ItemCounts.Clear(); PartyMemberIds.Clear(); ActorState.Clear(); BattleCommands.Clear();
        ActorValues.Clear(); CurrentHp.Clear(); CurrentSp.Clear(); TroopMembers.Clear(); CommonEventIds.Clear();
        PlayerIsHidden = false; PlayerIsThrough = false;
        ActiveTroopId = -1; IsBattleActive = false; BattleTurn = 0; BattlePhase = -1;
        CommonEventCounter = 0;
        // **The stack starts empty, not with an invented "Menu" scene.** A
        // first draft pushed "Menu" and made it the current scene, which is
        // not an RPG_RT scene name and made every scene test pass against a
        // fiction. An empty stack says what is true: nothing is open.
        SceneStack.Clear(); CurrentScene = "";
        SupportsRpg2k3ECommands = false; ExitRequested = false;
        SupportsManiacPatch = false;
        AtbWaitMode = true; FullscreenRequested = false;
        // The audio goes with the rest: a new game must not inherit the last
        // one's music, and least of all the track it memorised.
        Audio.Reset();
        SaveTimestamp = 0; SaveComment = "";
        ClearDiagnostics();
    }
}
