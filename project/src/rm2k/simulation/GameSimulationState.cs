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
    /// Which chipset the map is drawn with, from <c>11710</c>.
    /// </summary>
    /// <remarks>
    /// <strong>Zero is a real chipset and not "none".</strong> The reference
    /// compares against <c>Game_Map::GetChipset()</c> and skips the redraw when
    /// they match, so a game that sets the chipset it already has pays nothing.
    /// A reader that treated zero as unset would redraw a game every time it
    /// ran the command, and would refuse the first chipset in the database.
    /// </remarks>
    public int ChipsetId { get; private set; }

    /// <summary>How many steps of walking trigger an encounter.</summary>
    /// <remarks>
    /// <strong>Zero is a real value</strong> and it is the one that turns random
    /// encounters off. A reader that treated zero as unset could never turn
    /// them off, and a game that does so would keep fighting every few steps
    /// for the rest of the map.
    /// </remarks>
    public int EncounterSteps { get; private set; } = 50;

    /// <summary>The parallax background, from <c>11720</c>.</summary>
    /// <remarks>
    /// <para>
    /// <strong>An empty name means the database panorama and not a file that
    /// does not exist.</strong> That is what the reference does with
    /// <c>if (!params.name.empty())</c> before it asks for the file.
    /// </para>
    /// <para>
    /// <strong>Six flags and two speeds, and they come from different
    /// parameters.</strong> The flags are 0, 1, 2 and 4; the horizontal speed is
    /// 3 and the vertical is 5. A reader that read them in order would take
    /// the speed out of a flag.
    /// </para>
    /// </remarks>
    public sealed class Parallax
    {
		/// <summary>The file name, empty for the database panorama.</summary>
		public string Name { get; set; } = "";

		/// <summary>Whether the panorama scrolls with the player sideways.</summary>
		public bool ScrollHorizontally { get; set; }

		/// <summary>Whether it scrolls up and down.</summary>
		public bool ScrollVertically { get; set; }

		/// <summary>Whether it scrolls on its own sideways.</summary>
		public bool ScrollHorizontallyAutomatic { get; set; }

		/// <summary>How fast it scrolls on its own, in pixels per frame.</summary>
		public int HorizontalSpeed { get; set; }

		/// <summary>Whether it scrolls upwards on its own.</summary>
		public bool ScrollVerticallyAutomatic { get; set; }

		/// <summary>How fast it scrolls upwards, in pixels per frame.</summary>
		public int VerticalSpeed { get; set; }
    }

    /// <summary>The panorama, or the defaults when the game set none.</summary>
    public Parallax MapParallax { get; private set; } = new Parallax();

    /// <summary>Replaces the panorama, from <c>11720</c>.</summary>
    public void SetParallax(Parallax pParallax)
    {
		MapParallax = pParallax;
    }

    /// <summary>
    /// Changes the chipset, from <c>11710</c>.
    /// </summary>
    /// <remarks>
    /// The sprite set is told to redraw by the reference through a call this
    /// reader has no equivalent for, so the redraw is a diagnostic — **a
    /// chipset that changed and nothing redrew looks exactly like a chipset that
    /// did not change.**
    /// </remarks>
    /// <returns>False when the id is outside the database bound.</returns>
    public bool SetChipset(int pChipsetId)
    {
		if (pChipsetId < 0 || pChipsetId > MaxChipsetId)
		{
			return false;
		}
		ChipsetId = pChipsetId;
		return true;
	}

    /// <summary>The highest chipset the database can name.</summary>
    public const int MaxChipsetId = 99;

    /// <summary>Sets how many steps trigger an encounter.</summary>
    /// <returns>False when the value is outside the bound.</returns>
    public bool SetEncounterSteps(int pSteps)
    {
		// **The bound is liblcf's own field width for the saved value.**
		if (pSteps < 0 || pSteps > MaxEncounterSteps)
		{
			return false;
		}
		EncounterSteps = pSteps;
		return true;
	}

    /// <summary>The highest encounter step count the save format holds.</summary>
    public const int MaxEncounterSteps = 9999;

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

    /// <summary>
    /// Whether the player may leave a battle with the escape command, from
    /// <c>11840 Change Escape Access</c>.
    /// </summary>
    /// <remarks>
    /// <strong>All three access flags default to allowed, and that is a real
    /// default and not a guess</strong> — a database that never ran one of
    /// these commands has all three set, so a new game is a game the player
    /// may open the menu in, save from and escape from. A reader that
    /// defaulted to forbidden would make every untouched game unplayable the
    /// moment the player pressed Escape.
    /// </remarks>
    /// <summary>
    /// One teleport target, from <c>11810 Teleport Targets</c>.
    /// </summary>
    /// <remarks>
    /// <strong>Targets belong to the game and not to the map.</strong> They
    /// survive a map change, which is the whole reason a game can have a warp
    /// point on a map the player is not on. A target list per map would be a
    /// different command.
    /// </remarks>
    public sealed class TeleportTarget
    {
		/// <summary>The map this point is on.</summary>
		public int MapId { get; set; }

		/// <summary>The column, in tiles.</summary>
		public int X { get; set; }

		/// <summary>The row, in tiles.</summary>
		public int Y { get; set; }

		/// <summary>
		/// Whether <see cref="SwitchId"/> has to be <em>on</em> for this point to
		/// count, from the command flag.
		/// </summary>
		/// <remarks>
		/// <strong>The flag is not "use a switch" but "the switch must be on".</strong>
		/// A reader that read it as the first would make every conditional warp
		/// unconditional, and a secret entrance would open at the start of the
		/// game.
		/// </remarks>
		public bool RequiresSwitchOn { get; set; }

		/// <summary>The switch this point depends on, when it has one.</summary>
		public int SwitchId { get; set; }

		/// <summary>
		/// Whether this point is one the player can stand on at all.
		/// </summary>
		/// <remarks>
		/// <strong>A point outside the map is not a point.</strong> The reference
		/// stores it unchecked, and a warp to a tile outside the map is a warp
		/// into nothing — so this reader refuses it and says which coordinate is
		/// wrong.
		/// </remarks>
		public bool IsUsable { get; set; } = true;
    }

    /// <summary>Every warp point the game has declared, keyed by map id.</summary>
    /// <remarks>
    /// A map id of zero is the map the player is on, which the reference uses
    /// for a point on the current map and this reader keeps as written.
    /// </remarks>
    /// <remarks>
    /// **A plain dictionary and not a Godot one**, because
    /// <c>Godot.Collections.Dictionary</c> is a Variant container and a class
    /// is not a Variant — the same GD0301 the actor values ran into.
    /// </remarks>
    /// <summary>
    /// Where the player leaves to when they press Escape, from
    /// <c>11830 Escape Target</c>.
    /// </summary>
    /// <remarks>
    /// <strong>One and not a list.</strong> A map has many warp points and
    /// exactly one place Escape goes to, and a reader that stored a list would
    /// have to invent a rule for which one wins — a rule the game never wrote.
    /// <c>SetEscapeTarget</c> in the reference replaces, and so does this.
    /// </remarks>
    public TeleportTarget? EscapeTarget { get; set; }

    /// <summary>
    /// One system sound effect, from <c>10670 Change System SFX</c>.
    /// </summary>
    /// <remarks>
    /// <strong>There are twelve of these and not four.</strong> The cursor, the
    /// decision, the cancel, the buzzer, the battle, the escape, and then one
    /// per battle event: enemy attack, enemy damage, ally damage, evasion,
    /// enemy death, item use. A reader that offered only the menu four would
    /// leave a game with a silent battle, and a reader that offered twelve for
    /// the music would offer seven sounds that do not exist.
    /// </remarks>
    public sealed class SystemSfx
    {
		/// <summary>The file name, empty when the slot is the database default.</summary>
		public string Name { get; set; } = "";

		/// <summary>Volume in percent, 0 to 100.</summary>
		public int Volume { get; set; } = 100;

		/// <summary>Tempo in percent, 50 to 200.</summary>
		public int Tempo { get; set; } = 100;

		/// <summary>Stereo balance, 50 is centred.</summary>
		public int Balance { get; set; } = 50;

		/// <summary>Milliseconds to fade in, 0 for none.</summary>
		public int FadeIn { get; set; }
    }

    /// <summary>
    /// One system music track, from <c>10660 Change System BGM</c>.
    /// </summary>
    /// <remarks>
    /// <strong>Seven of these</strong>: battle, victory, inn, boat, ship,
    /// airship, game over. Music has a fade-in and sounds do not, because a
    /// sound effect with a fade is a sound effect the player waited for.
    /// </remarks>
    public sealed class SystemBgm
    {
		/// <summary>The file name, empty when the slot is the database default.</summary>
		public string Name { get; set; } = "";

		/// <summary>Milliseconds to fade in, 0 for none.</summary>
		public int FadeIn { get; set; }

		public int Volume { get; set; } = 100;
		public int Tempo { get; set; } = 100;

		/// <summary>Stereo balance, 50 is centred.</summary>
		public int Balance { get; set; } = 50;
    }

    /// <summary>The twelve system sound slots, by context number.</summary>
    public System.Collections.Generic.Dictionary<int, SystemSfx> SystemSfxSlots { get; init; } = new();

    /// <summary>The seven system music slots, by context number.</summary>
    public System.Collections.Generic.Dictionary<int, SystemBgm> SystemBgmSlots { get; init; } = new();

    /// <summary>The highest system sound context the reference knows.</summary>
    public const int MaxSystemSfxContext = 12;

    /// <summary>The highest system music context the reference knows.</summary>
    public const int MaxSystemBgmContext = 7;

    /// <summary>
    /// The system graphic, from <c>10680 Change System Graphics</c>.
    /// </summary>
    /// <remarks>
    /// The name is empty when the game has never changed it, which is the
    /// state a fresh database is in. The two numbers are a stretch mode and a
    /// font, both of which the reference casts straight from the command
    /// without checking, so this reader clamps them instead of asserting.
    /// </remarks>
    public string SystemGraphicName { get; set; } = "";

    /// <summary>How the message window stretches, 0 for none.</summary>
    public int SystemGraphicStretch { get; set; }

    /// <summary>Which font the message window uses.</summary>
    public int SystemGraphicFont { get; set; }

    /// <summary>
    /// The six screen transitions, from <c>10690 Change Screen Transitions</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>-1 means "back to whatever the database says", and that is the
    /// whole reason this is a dictionary and not six numbers.</strong> The
    /// reference writes `return t != db ? t : -1;` — a value equal to the
    /// database one is stored as -1, because a game that sets a transition to
    /// the value it already has is asking to stop overriding it. A reader that
    /// stored the value would pin the transition forever, and a game that later
    /// changed its database row would be overridden by a command that meant
    /// "let go".
    /// </para>
    /// <para>
    /// <strong>Six, and they come in three pairs:</strong> teleport in and out,
    /// battle start in and out, battle end in and out. A reader that offered
    /// one "transition" would make a game that fades out on teleport also fade
    /// out on entering a battle, and those are separate choices.
    /// </para>
    /// </remarks>
    public System.Collections.Generic.Dictionary<int, int> SystemTransitions { get; init; } = new();

    /// <summary>
    /// The value that means "use the database transition".
    /// </summary>
    public const int TransitionFromDatabase = -1;

    /// <summary>Teleport, leaving the old map.</summary>
    public const int TransitionTeleportErase = 0;

    /// <summary>Teleport, arriving on the new map.</summary>
    public const int TransitionTeleportShow = 1;

    /// <summary>Battle start, leaving the map.</summary>
    public const int TransitionBeginBattleErase = 2;

    /// <summary>Battle start, entering the battle.</summary>
    public const int TransitionBeginBattleShow = 3;

    /// <summary>Battle end, leaving the battle.</summary>
    public const int TransitionEndBattleErase = 4;

    /// <summary>Battle end, returning to the map.</summary>
    public const int TransitionEndBattleShow = 5;

    /// <summary>How many transitions there are, from <c>Transition_Count</c>.</summary>
    /// <remarks>
    /// **A count and not a last index.</strong> The reference enum ends with
    /// <c>Transition_Count</c>, so the guard is <c>which &lt; Count</c> and a
    /// reader that read a last index of five would refuse the sixth transition
    /// — the one that brings the player back from a battle.
    /// </remarks>
    public const int MaxTransition = 6;

    /// <summary>
    /// Whether the player may use the teleport command, from
    /// <c>11820 Change Teleport Access</c>.
    /// </summary>
    /// <remarks>
    /// <strong>Defaulted to allowed, with the other three access flags and for
    /// the same reason</strong> — a database that never ran the command has
    /// teleport on, and a reader that defaulted to forbidden would leave a game
    /// with no warps and no way to walk anywhere.
    /// </remarks>
    public bool AllowTeleport { get; private set; } = true;

    public System.Collections.Generic.Dictionary<int, List<TeleportTarget>> TeleportTargets { get; init; } = new();

    /// <summary>Why the interpreter is waiting, when it is.</summary>
    public enum WaitReason
    {
		/// <summary>Nothing is blocking the event.</summary>
		None,

		/// <summary>A message window is open, so the outcome is not shown yet.</summary>
		MessageOpen,

		/// <summary>The game over screen is up.</summary>
		GameOver,

		/// <summary>The title screen was requested.</summary>
		TitleRequested,

		/// <summary>The save menu is up, from <c>11910</c>.</summary>
		SaveMenuOpen,

		/// <summary>The main menu is up, from <c>11950</c>.</summary>
		MainMenuOpen,
    }

    /// <summary>What the interpreter is waiting for, from <c>12420</c> and <c>12510</c>.</summary>
    public WaitReason WaitingFor { get; set; } = WaitReason.None;

	/// <summary>Whether the game over screen is up, from <c>12420</c>.</summary>
	public bool IsGameOverActive { get; set; }

	/// <summary>Whether the title screen was requested, from <c>12510</c>.</summary>
	public bool IsTitleRequested { get; set; }

	/// <summary>Whether the save menu is up, from <c>11910</c>.</summary>
	/// <remarks>
	/// <strong>A request and not an open menu.</strong> This reader builds no
	/// menu scene, so the flag says what a command asked for — the same shape
	/// as <see cref="IsGameOverActive"/>, and a caller that draws the menu from
	/// it is the runtime's business, not the simulation's.
	/// </remarks>
	public bool IsSaveMenuActive { get; set; }

	/// <summary>Whether the main menu is up, from <c>11950</c>.</summary>
	/// <remarks>
	/// <strong>Its own flag and not a second value of the save one.</strong> A
	/// reader that stored "a menu" in one field would have the save command
	/// clear the main menu's request, and a game that opened the main menu and
	/// then saved would find neither.
	/// </remarks>
	public bool IsMainMenuActive { get; set; }

    public bool AllowEscape { get; private set; } = true;



    /// <summary>Whether the player may save, from <c>11930</c>.</summary>
    public bool AllowSave { get; private set; } = true;

    /// <summary>Whether the player may open the menu, from <c>11960</c>.</summary>
    public bool AllowMenu { get; private set; } = true;

    /// <summary>
    /// Sets or clears one of the three access rights.
    /// </summary>
    /// <remarks>
    /// The setter is private on purpose. A timer can only be set through
    /// <see cref="SetTimer"/> and only started through
    /// <see cref="StartTimer"/>, because the two are different operations that
    /// were once collapsed and the collapse cost a save file. The access flags
    /// are a single assignment each, so one method is enough — but it is one
    /// method and not a public setter, so the next reader does not have to
    /// rediscover that the four of them move together.
    /// </remarks>
    public void SetAccess(bool pEscape, bool pSave, bool pMenu, bool pTeleport)
    {
        AllowEscape = pEscape;
        AllowSave = pSave;
        AllowMenu = pMenu;
        AllowTeleport = pTeleport;
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
        // **The access flags go back to allowed with everything else.** A new
        // game is a game the player may save and escape from, and a reset that
        // left a cutscene's restrictions in place would lock the next game.
        // **The map settings go back to the database with everything else.**
        // A new game that inherited another game s tiles, panorama or
        // encounter rate would look like a bug — and an encounter rate of zero
        // would make it unwinnable, with no fights and no experience.
        ChipsetId = 0;
        MapParallax = new Parallax();
        SetEncounterSteps(50);
        SetAccess(pEscape: true, pSave: true, pMenu: true, pTeleport: true);
        EscapeTarget = null;
        // **The system slots go back to the database with everything else.**
        // A new game that inherited the last game battle music would start in
        // silence with somebody else playing.
        SystemSfxSlots.Clear(); SystemBgmSlots.Clear(); SystemTransitions.Clear();
        SystemGraphicName = ""; SystemGraphicStretch = 0; SystemGraphicFont = 0;
        // **The outcome belongs to the game, not to the next one.** A new game
        // that started with the last game over screen still up would look like
        // a crash, and one that started with a title request pending would
        // go straight back to the title.
        TeleportTargets.Clear();
        WaitingFor = WaitReason.None; IsGameOverActive = false; IsTitleRequested = false;
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
