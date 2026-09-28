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

    // ---- Pan Screen (11060)

    /// <summary>What a pan command does, from <c>11060</c>'s first parameter.</summary>
    /// <remarks>
    /// <para>
    /// <strong>Four values, and only the middle two move anything.</strong> The
    /// reference's switch has 0 lock, 1 unlock, 2 pan and 3 reset — <strong>and
    /// a value it does not know falls through all four and does nothing at
    /// all.</strong> A reader that defaulted to the pan would have a game's
    /// mistyped mode scrolling the screen instead of doing nothing, and a game
    /// that uses lock to hold the camera during a cutscene would have the
    /// camera move instead of holding.
    /// </para>
    /// </remarks>
    public enum PanMode
    {
        /// <summary>Hold the camera where it is.</summary>
        Lock,

        /// <summary>Let the camera follow the player again.</summary>
        Unlock,

        /// <summary>Scroll the camera in a direction.</summary>
        Pan,

        /// <summary>Return the camera to the player.</summary>
        Reset,
    }

    /// <summary>
    /// The eight pan directions, in the editor's own order.
    /// </summary>
    /// <remarks>
    /// <strong>Up, right, down, left, and then the four diagonals</strong> —
    /// which is a compass order here and not the passability bit order the
    /// eight directions use elsewhere. A reader that reused the bit order
    /// would scroll the wrong way for every diagonal a game pans.
    /// </remarks>
    public enum PanDirection
    {
        /// <summary>Up.</summary>
        Up,

        /// <summary>Right.</summary>
        Right,

        /// <summary>Down.</summary>
        Down,

        /// <summary>Left.</summary>
        Left,

        /// <summary>Up and left.</summary>
        UpLeft,

        /// <summary>Up and right.</summary>
        UpRight,

        /// <summary>Down and left.</summary>
        DownLeft,

        /// <summary>Down and right.</summary>
        DownRight,
    }

    /// <summary>Whether the camera is held, from <c>11060</c>'s lock and unlock.</summary>
    public bool IsPanLocked { get; set; }

    /// <summary>Whether a pan is running.</summary>
    public bool IsPanActive { get; set; }

    /// <summary>Where the pan is going, in tiles.</summary>
    public int PanTargetX { get; set; }

    /// <summary>Where the pan is going, vertically.</summary>
    public int PanTargetY { get; set; }

    /// <summary>How fast the pan runs, in the editor's own 1 to 6 scale.</summary>
    /// <remarks>
    /// <strong>Clamped to 1 to 6 and not refused.</strong> The reference writes
    /// <c>Utils::Clamp&lt;int&gt;(com.parameters[3], 1, 6)</c>, so a game that
    /// wrote a zero or a nine gets the nearest speed and the pan still runs —
    /// <strong>and a reader that refused would have stopped the event on a
    /// number the engine repairs.</strong>
    /// </remarks>
    public int PanSpeed { get; set; } = 3;

    /// <summary>The frames the pan has left.</summary>
    public int PanFramesLeft { get; set; }

    /// <summary>What the last pan command asked for.</summary>
    public PanMode PanLastMode { get; set; } = PanMode.Unlock;

    /// <summary>Which direction the last pan asked for.</summary>
    public PanDirection PanLastDirection { get; set; } = PanDirection.Up;

    /// <summary>How far the last pan asked to scroll, in tiles.</summary>
    public int PanLastDistance { get; set; }

    /// <summary>
    /// Whether the event waits for movement to finish, from <c>11340</c>.
    /// </summary>
    /// <remarks>
    /// <strong>A flag and not a counter.</strong> The reference writes
    /// <c>_state.wait_movement = true;</c> and nothing else — and the
    /// interpreter clears it once the movement is over, so a second
    /// <c>11340</c> before that is one flag written twice and not two waits.
    /// </remarks>
    public bool ProceedWithMovement { get; set; }

    /// <summary>
    /// Stops every pending move on the map, from <c>11350</c>.
    /// </summary>
    /// <remarks>
    /// <strong>The map, and not the player.</strong> The reference calls
    /// <c>Game_Map::RemoveAllPendingMoves()</c> — every figure with a move
    /// route still running, and not the hero. <strong>A reader that stopped
    /// only the player would have a game whose guards keep walking after a
    /// cutscene stops them.</strong>
    /// </remarks>
    public void HaltAllMovement()
    {
        ProceedWithMovement = false;
        // **And the pans stop with them**, because a command that stops
        // everything and leaves the camera sliding stops less than everything.
        IsPanActive = false;
        PanFramesLeft = 0;
    }

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

		/// <summary>A battle is running, from <c>10710</c>.</summary>
		BattleRunning,
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

    // ---- Shop (10720) and inn (10730), with their handlers

    /// <summary>
    /// What a shop or an inn is doing, and the option a command offers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Five states, and the reference's own names for them.</strong>
    /// <c>eOptionShopTransaction</c>, <c>eOptionShopNoTransaction</c>,
    /// <c>eOptionInnStay</c> and <c>eOptionInnNoStay</c> are the four, and
    /// <c>None</c> is this reader's own for "no option on the table". <strong>A
    /// reader with a single "is a shop open" flag could not tell</strong>
    /// which of the two shop options a handler answered, and the handler list
    /// it has to walk is built from exactly that.
    /// </para>
    /// </remarks>
    public enum ShopOption
    {
        /// <summary>No shop or inn option is on the table.</summary>
        None,

        /// <summary>10720, Open Shop: the player may buy and sell.</summary>
        ShopTransaction,

        /// <summary>20720, Transaction: the shop runs and the player buys.</summary>
        Transaction,

        /// <summary>20721, No Transaction: the shop runs and nothing is traded.</summary>
        NoTransaction,

        /// <summary>10730, Show Inn: the player may stay and rest.</summary>
        InnStay,

        /// <summary>20730, Stay: the inn runs and the party rests.</summary>
        Stay,

        /// <summary>20731, No Stay: the inn runs and nothing is rested.</summary>
        NoStay,
    }

    /// <summary>
    /// The option the last shop or inn command offered.
    /// </summary>
    public ShopOption ActiveShopOption { get; set; } = ShopOption.None;

    /// <summary>
    /// The subcommand index a command block belongs to, from the shop's and
    /// the inn's own option.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>This is the one number the reference's handler depends on.</strong>
    /// <c>CommandOptionGeneric</c> reads the sub-index and compares it with
    /// the option the handler is for — <strong>so a handler runs its block
    /// only when it is the option that was chosen, and skips it when it is
    /// not.</strong> A reader without this would run every handler in a shop,
    /// and a game's "you bought nothing" branch would have run beside its
    /// "you bought something" branch.
    /// </para>
    /// <para>
    /// <strong>And the chosen sub-index is cleared when its handler runs,</strong>
    /// so the next handler of the same list finds nothing chosen and skips.
    /// That is the reference's own sentinel, and it is why a shop with two
    /// handlers runs one of them and not both.
    /// </para>
    /// </remarks>
    public int SubcommandIndex { get; set; }

    /// <summary>
    /// The reference's own sentinel for "this option has already run".
    /// </summary>
    public const int SubcommandSentinel = -1;

    /// <summary>
    /// Whether a handler for a sub-index should run its block, and clears the
    /// choice when it does.
    /// </summary>
    /// <remarks>
    /// <strong>This is the reference's own if/else and it is one
    /// comparison.</strong> The other arm is the caller skipping to the next
    /// handler, and the two together are the whole of a handler.
    /// </remarks>
    public bool IsSubcommandChosen(int pOptionSubIdx)
    {
        if (SubcommandIndex != pOptionSubIdx)
        {
            return false;
        }

        SubcommandIndex = SubcommandSentinel;
        return true;
    }

    /// <summary>
    /// Whether a shop or an inn is open, and which of the two it is.
    /// </summary>
    /// <remarks>
    /// <strong>It is the shape of the option and not a separate flag.</strong>
    /// The reference calls <c>Game_Shop::SetMode()</c> from the opener and
    /// clears it from the closer, so a second flag would be free to disagree
    /// with the option it is supposed to follow.
    /// </remarks>
    public bool IsShopOpen { get; private set; }

    /// <summary>
    /// Whether an inn is open.
    /// </summary>
    public bool IsInnOpen { get; private set; }

    /// <summary>The gold a stay in an inn costs, from 10730's second
    /// parameter.</summary>
    public int InnPrice { get; set; }

    /// <summary>The inn's type, from 10730's first parameter.</summary>
    public int InnType { get; private set; }

    /// <summary>
    /// The shop's own items, from 10720 Open Shop.
    /// </summary>
    /// <remarks>
    /// <strong>Three values per item and not two.</strong> The reference's
    /// <c>CmdSetup&lt;..., 4&gt;</c> says four, and the first is the item id
    /// while the second is a <em>price</em> and not a stock count. <strong>A
    /// reader that read the second parameter as "how many" would have shown
    /// one item where the shopkeeper has fifty</strong>, and a game's shop
    /// would have sold its wares one at a time and then stopped.
    /// </remarks>
    // ---- 10440 Change Skills, 10450 Change Equipment, 10480 Change Condition

    /// <summary>
    /// The five equipment slots, in the reference's own order.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Five slots and a sixth "all of them", and the slot comes from
    /// the item's own type.</strong> The reference's <c>CommandChangeEquipment</c>
    /// reads the item, switches on <c>item-&gt;type</c> across weapon, shield,
    /// armor, helmet and accessory, and assigns <c>slot = item-&gt;type</c> —
    /// <strong>so a reader that took the slot from the command's parameter
    /// would have put a helmet where a sword goes</strong>, because the
    /// parameter is only a *direct* slot when the mode says so.
    /// </para>
    /// <para>
    /// And the sixth value is not a slot: <c>slot == 6</c> is the reference's
    /// own "remove everything", and it is checked before any of the five.
    /// </para>
    /// </remarks>
    public enum EquipmentSlot
    {
        /// <summary>A weapon, and with two weapons a second one.</summary>
        Weapon,

        /// <summary>A shield.</summary>
        Shield,

        /// <summary>A body armour.</summary>
        Armor,

        /// <summary>A helmet.</summary>
        Helmet,

        /// <summary>An accessory.</summary>
        Accessory,

        /// <summary>Every slot at once — the reference's sixth value.</summary>
        All,
    }

    /// <summary>
    /// What an item is, in the five types the equipment command distinguishes.
    /// </summary>
    /// <remarks>
    /// <strong>Five, and this repository's item database had three.</strong> It
    /// distinguished a weapon, an armour and a consumable, so a shield, a
    /// helmet and an accessory were all "armour" — and a reader that
    /// derived the slot from that would have equipped a party's helmets into
    /// their body slot, three at a time.
    /// </remarks>
    public enum EquipmentKind
    {
        /// <summary>Not equipment, and never assigned to a slot.</summary>
        None,

        /// <summary>A weapon.</summary>
        Weapon,

        /// <summary>A shield.</summary>
        Shield,

        /// <summary>A body armour.</summary>
        Armor,

        /// <summary>A helmet.</summary>
        Helmet,

        /// <summary>An accessory.</summary>
        Accessory,
    }

    /// <summary>The items each actor wears, keyed by actor id and then by slot.</summary>
    /// <remarks>
    /// <strong>A dictionary and not an array</strong>, because the reference's
    /// own <c>ChangeEquipment(slot, id)</c> is addressed by slot and an array
    /// of five would have made the sixth value — "everything" — impossible to
    /// write.
    /// </remarks>
    public System.Collections.Generic.Dictionary<int,
        System.Collections.Generic.Dictionary<EquipmentSlot, int>> ActorEquipment
    { get; init; } = new();

    /// <summary>The skill ids each actor knows, from <c>10440</c>.</summary>
    public System.Collections.Generic.HashSet<int>[] ActorSkills { get; init; } =
        new System.Collections.Generic.HashSet<int>[MaxActorId + 1];

    /// <summary>The condition ids each actor has, from <c>10480</c>.</summary>
    /// <remarks>
    /// <strong>An array of sets, and not a set of sets,</strong> because the
    /// reference asks an actor whether it has <em>a</em> state and never asks
    /// which actors share one.
    /// </remarks>
    public System.Collections.Generic.HashSet<int>[] ActorConditions { get; init; } =
        new System.Collections.Generic.HashSet<int>[MaxActorId + 1];

    /// <summary>
    /// Whether an actor fights with two weapons, which is the reference's
    /// <c>HasTwoWeapons()</c>.
    /// </summary>
    /// <remarks>
    /// <strong>It changes what a shield and a weapon do, and not only
    /// whether.</strong> The reference skips a shield outright for a
    /// two-weapon actor while a shield is in hand, and puts a second weapon
    /// into the second slot when the first is empty and neither weapon is
    /// two-handed.
    /// </remarks>
    /// <summary>
    /// What kind of equipment each item is, from the item database.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>This is the bridge 10450's first mode needs,</strong> because
    /// the reference reads the item and takes its slot from
    /// <c>item-&gt;type</c> — and a reader that had no item table could only
    /// guess. An id that is not here is not equipment, and the reference's own
    /// switch returns without touching anything.
    /// </para>
    /// <para>
    /// <strong>Five kinds, and "everything" is not one of them</strong> — it is
    /// the sixth slot value and no item has it.
    /// </para>
    /// </remarks>
    public System.Collections.Generic.Dictionary<int, EquipmentKind> ItemEquipmentKinds
    { get; init; } = new();

    /// <summary>What kind of equipment an item is, or none.</summary>
    public EquipmentKind EquipmentKindOf(int pItemId)
    {
        return ItemEquipmentKinds.TryGetValue(pItemId, out var art)
            ? art
            : EquipmentKind.None;
    }

    /// <summary>Whether a weapon is two-handed, which the reference checks.</summary>
    /// <remarks>
    /// <strong>It decides where a second weapon goes.</strong> The reference
    /// puts a one-handed weapon into the second slot when the first is empty
    /// and <em>neither</em> weapon is two-handed, so a reader that ignored
    /// this would have given a two-handed swordsman a second sword in his
    /// shield hand.
    /// </remarks>
    public System.Collections.Generic.HashSet<int> TwoHandedWeapons { get; init; } = new();


    public System.Collections.Generic.Dictionary<int, bool> ActorHasTwoWeapons
    { get; init; } = new();

    /// <summary>
    /// Whether a hero is dead, and the reference's own rule for it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Every one of the three commands ends in
    /// <c>CheckGameOver()</c></strong>, so a game whose last hero is killed by
    /// a condition command reaches the game-over screen from the condition
    /// command and not from a battle's defeat arm. A reader that left the
    /// check out would have had a party walk on with every hero at zero.
    /// </para>
    /// <para>
    /// And the reference's check is over the *party*, not over every actor in
    /// the database — a hero left behind in a town who is dead changes
    /// nothing.
    /// </para>
    /// </remarks>
    public void CheckGameOver()
    {
        // **The index loop and not the enumeration.** PartyMemberIds is a
        // Godot.Collections.Array, so its enumerator hands out Variants and
        // GetActorCurrentHp's int parameter refuses them — and the exception
        // is "The given key was not present in the dictionary", which names
        // neither the array nor the party.
        for (var i = 0; i < PartyMemberIds.Count; i++)
        {
            if (GetActorCurrentHp(PartyMemberIds[i]) > 0)
            {
                return;
            }
        }

        IsGameOverActive = true;
    }

    /// <summary>The conditions an actor has, and the set is created on demand.</summary>
    public System.Collections.Generic.HashSet<int> ConditionsOf(int pActorId)
    {
        if (pActorId < 0 || pActorId > MaxActorId)
        {
            return new System.Collections.Generic.HashSet<int>();
        }

        ActorConditions[pActorId] ??= new System.Collections.Generic.HashSet<int>();
        return ActorConditions[pActorId];
    }

    /// <summary>The skills an actor knows, and the set is created on demand.</summary>
    public System.Collections.Generic.HashSet<int> SkillsOf(int pActorId)
    {
        if (pActorId < 0 || pActorId > MaxActorId)
        {
            return new System.Collections.Generic.HashSet<int>();
        }

        ActorSkills[pActorId] ??= new System.Collections.Generic.HashSet<int>();
        return ActorSkills[pActorId];
    }

    /// <summary>
    /// Puts an item in a slot, and the reference's own <c>ChangeEquipment</c>.
    /// </summary>
    /// <remarks>
    /// <strong>One of the five slots only.</strong> The sixth value is the
    /// interpreter's own <see cref="RemoveWholeEquipment"/>, and the reference
    /// checks it before it reaches this method.
    /// </remarks>
    /// <remarks>
    /// <strong>A slot of "everything" is not a slot and empties instead.</strong>
    /// The reference checks <c>slot == 6</c> before any of the five and calls
    /// <c>RemoveWholeEquipment()</c>, so a reader that treated the sixth
    /// value as a sixth slot would have had a game's "unequip everything"
    /// write a hidden sixth entry and leave every real slot on.
    /// </remarks>
    public void ChangeEquipment(int pActorId, EquipmentSlot pSlot, int pItemId)
    {
        // **TryGetValue and not `??=` on an indexer.** A dictionary's indexer
        // throws on a missing key, and `??=` compiles to a get followed by a
        // set -- so the first question asked about a hero who has never worn
        // anything threw "The given key was not present in the dictionary",
        // which names neither the party nor the slot.
        if (!ActorEquipment.TryGetValue(
                pActorId, out var slots) || slots is null)
        {
            slots = new System.Collections.Generic.Dictionary<EquipmentSlot, int>();
            ActorEquipment[pActorId] = slots;
        }
        // **"Everything" is not this method's business.** The reference
        // branches on the sixth slot in the interpreter and calls its own
        // RemoveWholeEquipment; a reader that also handled it here would have
        // had two places to change the same fact, and a mutation of either
        // one would be invisible to the other.

        if (pItemId == 0)
        {
            slots.Remove(pSlot);
            return;
        }

        slots[pSlot] = pItemId;
    }

    /// <summary>
    /// Empties every slot of an actor, from 10450's sixth slot.
    /// </summary>
    public void RemoveWholeEquipment(int pActorId)
    {
        ActorEquipment.Remove(pActorId);
    }

    /// <summary>What an actor wears in a slot, or zero.</summary>
    public int GetEquipment(int pActorId, EquipmentSlot pSlot)
    {
        // **TryGetValue on the outer and on the inner dictionary.** An actor
        // with no equipment at all has no entry, and a reader that indexed
        // the outer dictionary would have thrown on the first question
        // asked about a hero who has never worn anything.
        if (!ActorEquipment.TryGetValue(pActorId, out var slots)
            || slots is null)
        {
            return 0;
        }

        return slots.TryGetValue(pSlot, out var id) ? id : 0;
    }

    /// <summary>Whether an actor fights with two weapons.</summary>
    public bool HasTwoWeapons(int pActorId)
    {
        return ActorHasTwoWeapons.TryGetValue(pActorId, out var flag) && flag;
    }


    public Godot.Collections.Array<int> ShopItemIds { get; } = new();

    /// <summary>
    /// The shop's own prices, in the same order as
    /// <see cref="ShopItemIds"/>.
    /// </summary>
    public Godot.Collections.Array<int> ShopPrices { get; } = new();

    /// <summary>
    /// Whether a shop is open, from 10720 and 20722.
    /// </summary>
    /// <summary>
    /// Whether the shop lets the player buy, from 10720's first parameter.
    /// </summary>
    public bool CanBuy { get; private set; }

    /// <summary>
    /// Whether the shop lets the player sell, from the same parameter.
    /// </summary>
    public bool CanSell { get; private set; }

    /// <summary>
    /// The shop's type, from 10720's second parameter.
    /// </summary>
    public int ShopType { get; private set; }

    /// <summary>
    /// Whether the shop's own handlers are written, from its third parameter.
    /// </summary>
    public bool ShopHasHandlers { get; private set; }

    /// <summary>
    /// Opens a shop of a type, from 10720.
    /// </summary>
    public void OpenShop(
        int pType,
        bool pCanBuy,
        bool pCanSell,
        bool pHasHandlers)
    {
        IsShopOpen = true;
        IsInnOpen = false;
        ShopType = pType;
        CanBuy = pCanBuy;
        CanSell = pCanSell;
        ShopHasHandlers = pHasHandlers;
    }

    /// <summary>
    /// Adds one of the shop's goods, from 10720's fourth parameter onward.
    /// </summary>
    public void AddShopGood(int pItemId)
    {
        ShopItemIds.Add(pItemId);
        // **The price is not a parameter of its own** — the reference copies
        // everything from the fourth parameter on into one list, so this
        // reader keeps the ids and reports no price of its own.
        ShopPrices.Add(0);
    }

    /// <summary>
    /// Closes a shop, from 20722 End Shop.
    /// </summary>
    public void CloseShop()
    {
        IsShopOpen = false;
        ActiveShopOption = ShopOption.None;
        ShopItemIds.Clear();
        ShopPrices.Clear();
    }

    /// <summary>
    /// Opens an inn with a price, from 10730 Show Inn.
    /// </summary>
    public void OpenInn(int pType, int pPrice)
    {
        IsInnOpen = true;
        IsShopOpen = false;
        InnType = pType;
        InnPrice = pPrice;
    }

    /// <summary>
    /// Closes an inn, from 20732 End Inn.
    /// </summary>
    public void CloseInn()
    {
        IsInnOpen = false;
        ActiveShopOption = ShopOption.None;
    }

    /// <summary>
    /// The gold a stay costs, and it is a third parameter and not a division.
    /// </summary>
    /// <remarks>
    /// <strong>The reference's <c>CmdSetup</c> gives 10730 a width of 3</strong>
    /// — item, who shows the price, and the price. <strong>A reader that read
    /// the price as the second parameter would have taken the "show the price
    /// or not" flag for the amount</strong> and charged a party one gold for a
    /// night's rest, or nothing at all.
    /// </remarks>
    public bool ShouldShowInnPrice { get; set; }

    // ---- The battle outcome handlers (20710, 20711, 20712, 20713)

    /// <summary>
    /// Which battle outcome a handler answers, from the encounter's own
    /// options.
    /// </summary>
    /// <remarks>
    /// <strong>The reference's option is per outcome and not per battle.</strong>
    /// <c>eOptionEnemyEncounterVictory</c>, <c>…Escape</c> and <c>…Defeat</c>
    /// are three separate values, and the encounter command's third and
    /// fourth parameters pick which handler list belongs to the battle in
    /// hand.
    /// </remarks>
    public BattleOutcome ActiveBattleOption { get; set; } = BattleOutcome.None;

    /// <summary>
    /// The battle outcome a handler answers.
    /// </summary>
    public enum BattleOutcome
    {
        /// <summary>No outcome is being handled.</summary>
        None,

        /// <summary>10710's victory option: what runs when the party wins.</summary>
        Victory,

        /// <summary>10710's escape option: what runs when the party flees.</summary>
        Escape,

        /// <summary>10710's defeat option: what runs when the party loses.</summary>
        Defeat,
    }

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

    /// <summary>
    /// The facing a pending transfer will use, and -1 means "keep the hero's".
    /// </summary>
    /// <remarks>
    /// <strong>-1 is the reference's own "unchanged", and it is not a
    /// direction.</strong> <c>10810</c> writes a direction and defaults to the
    /// one the player has; <c>10830</c> writes -1 because a recall has no
    /// reason to turn the hero. <strong>A reader that copied 10810's default
    /// would have turned a game's hero on every recall</strong>, and a reader
    /// that refused the -1 would have refused half of all 2K maps' recalls.
    /// </remarks>
    public int PendingFacing { get; set; } = -1;

    /// <summary>
    /// The terrain id of a tile, from 10910 Store Terrain ID.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Two arrays and not one.</strong> The reference's
    /// <c>Game_Map::GetTerrainTag(x, y)</c> reads the map's lower layer, takes
    /// the chip index, and looks it up in the chipset's terrain table — so a
    /// reader that answered from the table alone would have returned the same
    /// number for every tile on the map, and a game's "am I on grass" branch
    /// would have taken the same arm everywhere. The chip id lives in
    /// <see cref="LowerLayer"/>, **not** in the derived
    /// <see cref="PassableTiles"/> — that one holds a bool per tile and would
    /// have given every tile on the map the same chip.
    /// </para>
    /// <para>
    /// <strong>And a tile outside the map is -1, not zero.</strong> The
    /// reference's own bounds check returns -1 for a chip index it cannot
    /// look up, and -1 is also the value a game's "no terrain here" test
    /// writes.
    /// </para>
    /// </remarks>
    public int TerrainTagAt(int pX, int pY)
    {
        // **Ohne Karte gibt es keine Kachel** -- und das ist -1 und nicht 0.
        if (MapWidth <= 0 || MapHeight <= 0
            || pX < 0 || pY < 0 || pX >= MapWidth || pY >= MapHeight)
        {
            return -1;
        }

        // **Die Chip-Id steht in der unteren Ebene, und nicht in
        // `PassableTiles`.** Die Passierbarkeit ist eine Nebenrechnung aus
        // derselben Ebene -- **ein Leser, der die Chip-Id aus ihr holen
        // wuerde, wuerde bei jedem Kachelpaar dieselbe Nummer sehen**, und
        // der Chipset-Eintrag waere nur noch eine Ziffer, die man raten
        // muss.
        var index = pX + pY * MapWidth;
        if (LowerLayer == null || index < 0 || index >= LowerLayer.Length)
        {
            return -1;
        }

        var chip = LowerLayer[index];
        if (chip < 0 || chip >= TerrainData.Length)
        {
            return -1;
        }

        return TerrainData[chip];
    }


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
    /// <summary>
    /// The base values of an actor, or null when that actor does not exist.
    /// </summary>
    /// <remarks>
    /// <strong>Look and not create.</strong> The reference's
    /// <c>Main_Data::game_actors-&gt;GetActor(id)</c> returns null for a hero
    /// nobody made, and <c>CommandEnterHeroName</c> warns on that null. <strong>A
    /// reader that created the entry would have made a game that names hero 99
    /// create hero 99</strong> — and the hero would then exist for every
    /// later command, in the party window and in the save file.
    /// </remarks>
    public Rm2kActorValues? FindActorValues(int pActorId)
    {
        if (pActorId < 1 || pActorId > MaxActorId)
        {
            return null;
        }

        return ActorValues.TryGetValue(pActorId, out var values) ? values : null;
    }

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

    /// <summary>
    /// The battle background, from 13210 Change Battle BG.
    /// </summary>
    /// <remarks>
    /// <strong>The command has a width of 1 and the value is not a parameter —
    /// it is the command's own text.</strong> The reference writes
    /// <c>Game_Battle::ChangeBackground(ToString(com.string))</c>, and
    /// <c>com.string</c> is where the editor puts the file name. A reader that
    /// looked in <c>parameters</c> would have found an empty list of one zero
    /// and changed nothing, and a game's battle background would have stayed
    /// whatever it was before the fight.
    /// </remarks>
    // ---- 11210 and 13260, Show Battle Animation

    /// <summary>
    /// A battle animation that is playing, from 11210 and 13260.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>The frames come out of the animation's own timing rows,</strong>
    /// because the reference's <c>Game_Battle::ShowBattleAnimation</c> returns
    /// <c>BattleAnimationBattle::GetFrames()</c> and the command writes that
    /// number into the interpreter's wait. <strong>A reader that invented a
    /// duration would have held the page for a number the game never
    /// wrote</strong> — and a battle where the hero's sword animation is 30
    /// frames would have frozen for 12.
    /// </para>
    /// <para>
    /// And the last timing row's frame count is the length, because the rows
    /// are absolute frame positions and the animation ends when the last one
    /// has passed.
    /// </para>
    /// </remarks>
    public int? BattleAnimationId { get; set; }

    /// <summary>Which side the animation is aimed at, from the target.</summary>
    /// <remarks>
    /// <strong>Allies count from one and enemies from zero.</strong> The
    /// reference subtracts one for a party member and not for a monster — so
    /// a target of 0 is the first enemy and the <em>zeroth</em> ally, which
    /// does not exist. A reader that used one numbering for both would have
    /// played a game's first hero's animation on its second hero.
    /// </remarks>
    public bool BattleAnimationOnAllies { get; set; }

    /// <summary>The index the animation was aimed at, in its side's numbering.</summary>
    public int BattleAnimationTarget { get; set; }

    /// <summary>Whether the animation plays on every member of its side.</summary>
    /// <remarks>
    /// <strong>A negative target means the whole side, and the flag says
    /// which.</strong> The reference reads <c>target &lt; 0</c> and then
    /// collects the party or the enemy party — so a target of -1 on the
    /// enemies is every enemy, and without the flag it would be every party
    /// member instead.
    /// </remarks>
    public bool BattleAnimationOnAllTargets { get; set; }

    /// <summary>How many frames the animation runs, from its own timing rows.</summary>
    public int BattleAnimationFrames { get; set; }

    /// <summary>Whether the command asked to wait for the animation.</summary>
    /// <remarks>
    /// <strong>And the wait is the animation's own frame count</strong> — the
    /// reference writes <c>_state.wait_time = frames</c> and not a constant,
    /// so a game that wrote "wait" gets a wait as long as its animation.
    /// </remarks>
    public bool BattleAnimationWaitRequested { get; set; }

    /// <summary>
    /// The frame count of a battle animation, from its timing rows.
    /// </summary>
    /// <remarks>
    /// <strong>Zero for an animation that is not in the table</strong> — the
    /// reference's <c>GetElement</c> returns nothing, it warns, and it returns
    /// zero frames, so a game with a mistyped animation id waits for nothing
    /// rather than for ever.
    /// </remarks>
    public int BattleAnimationFrameCount(int pAnimationId)
    {
        if (pAnimationId <= 0)
        {
            return 0;
        }

        return BattleAnimationDurations.TryGetValue(pAnimationId, out var frames)
            ? frames
            : 0;
    }

    /// <summary>
    /// How long each battle animation runs, keyed by its id.
    /// </summary>
    /// <remarks>
    /// <strong>This is the table the parser fills from the animation
    /// database,</strong> and it holds one number per animation: the frame
    /// count of its last timing row. A game's whole battle animation timing
    /// lives here, and nothing else in the simulation needs it.
    /// </remarks>
    public System.Collections.Generic.Dictionary<int, int> BattleAnimationDurations
    { get; init; } = new();


    public string BattleBackground { get; set; } = "";

    /// <summary>
    /// A monster's current hit points, from 13110 Change Monster HP.
    /// </summary>
    /// <remarks>
    /// <strong>Three change modes, and the third is a percentage.</strong> The
    /// reference's switch is 0 a constant, 1 a variable and 2 a share of the
    /// monster's own maximum. A reader that read mode 2 as another constant
    /// would have healed a wounded boss for one hit point where the game asked
    /// for a tenth of his life.
    /// </remarks>
    public int MonsterHp(int pIndex)
    {
        var m = MonsterAt(pIndex);
        return m is null ? 0 : (int)m["hp"];
    }

    /// <summary>
    /// Sets a monster's hit points, clamped to zero at the bottom.
    /// </summary>
    /// <remarks>
    /// <strong>Zero is a floor and not a ceiling.</strong> The reference's
    /// <c>ChangeHp</c> clamps at 0, so a game that subtracts more than a
    /// monster has leaves him on zero and not on a negative number — and a
    /// reader without the clamp would have a dead monster whose hit points
    /// count backwards.
    /// </remarks>
    public void SetMonsterHp(int pIndex, int pValue)
    {
        var m = MonsterAt(pIndex);
        if (m is null)
        {
            return;
        }

        m["hp"] = Math.Max(0, pValue);
    }

    /// <summary>
    /// A monster's maximum hit points, which mode 2 of 13110 is a share of.
    /// </summary>
    public int MonsterMaxHp(int pIndex)
    {
        var m = MonsterAt(pIndex);
        return m is null ? 0 : (int)m["max_hp"];
    }

    /// <summary>
    /// A monster's current spirit points, from 13120 Change Monster MP.
    /// </summary>
    public int MonsterSp(int pIndex)
    {
        var m = MonsterAt(pIndex);
        return m is null ? 0 : (int)m["sp"];
    }

    /// <summary>
    /// Sets a monster's spirit points, clamped to zero at the bottom.
    /// </summary>
    public void SetMonsterSp(int pIndex, int pValue)
    {
        var m = MonsterAt(pIndex);
        if (m is null)
        {
            return;
        }

        m["sp"] = Math.Max(0, pValue);
    }

    /// <summary>
    /// Whether a monster is dead, and what its death timer holds.
    /// </summary>
    /// <remarks>
    /// <strong>A dead monster is not removed at once.</strong> The reference
    /// plays the system's enemy-kill sound and then calls
    /// <c>enemy->SetDeathTimer()</c>, and its own comment says the monster
    /// disappears and animates. <strong>A reader that removed the monster from
    /// the troop the moment its hit points reached zero would have had it
    /// vanish mid-frame</strong>, and the sound and the animation would have
    /// had nothing to play on.
    /// </remarks>
    public bool IsMonsterDead(int pIndex)
    {
        var m = MonsterAt(pIndex);
        return m is not null && (int)m["hp"] <= 0;
    }

    /// <summary>
    /// How a monster leaves a battle, from 13110 and 13130 together.
    /// </summary>
    /// <remarks>
    /// <strong>Timed for a hit-point death and immediate for a state
    /// removal.</strong> The reference's two paths are different on purpose:
    /// a monster at zero hit points gets a death timer, and one whose death
    /// state was removed disappears immediately and "doesn't animate death" —
    /// which is written down as an RPG_RT bug the reference reproduces.
    /// </remarks>
    public enum MonsterExit
    {
        /// <summary>Still in the fight.</summary>
        None,

        /// <summary>Left the troop at once, without a death animation.</summary>
        Immediate,

        /// <summary>Left after a death animation ran.</summary>
        Timed,
    }

    /// <summary>
    /// How a monster left the troop, and when.
    /// </summary>
    public MonsterExit MonsterExitOf(int pIndex)
    {
        var m = MonsterAt(pIndex);
        if (m is null || !m.ContainsKey("exit"))
        {
            return MonsterExit.None;
        }

        return (MonsterExit)(int)m["exit"];
    }

    /// <summary>
    /// Records how a monster left the troop.
    /// </summary>
    public void SetMonsterExit(int pIndex, MonsterExit pExit)
    {
        var m = MonsterAt(pIndex);
        if (m is null)
        {
            return;
        }

        m["exit"] = (int)pExit;
    }

    /// <summary>
    /// A monster's conditions, from 13130 Change Monster Condition.
    /// </summary>
    /// <remarks>
    /// <strong>A list, and it grows and shrinks.</strong> The reference calls
    /// <c>AddState(id, true)</c> and <c>RemoveState(id, false)</c> — and the
    /// second argument of the removal is the reference's own "don't animate
    /// death", which is the bug it reproduces rather than a flag of its own.
    /// </remarks>
    public Godot.Collections.Array<int> MonsterConditions(int pIndex)
    {
        var m = MonsterAt(pIndex);
        if (m is null)
        {
            return new Godot.Collections.Array<int>();
        }

        if (!m.ContainsKey("conditions"))
        {
            m["conditions"] = new Godot.Collections.Array<int>();
        }

        return (Godot.Collections.Array<int>)m["conditions"];
    }

    /// <summary>
    /// Adds a condition to a monster.
    /// </summary>
    public void AddMonsterCondition(int pIndex, int pStateId)
    {
        var zustand = MonsterConditions(pIndex);
        if (!zustand.Contains(pStateId))
        {
            zustand.Add(pStateId);
        }
    }

    /// <summary>
    /// Removes a condition from a monster, and it is the death path.
    /// </summary>
    /// <remarks>
    /// <strong>Removing the death state is a death, and it is immediate.</strong>
    /// The reference calls <c>RemoveState(state_id, false)</c> here and
    /// <c>SetDeathTimer()</c> on the hit-point path, so the two commands
    /// remove a monster differently and a reader that treated them alike would
    /// have animated a death the reference does not animate.
    /// </remarks>
    public void RemoveMonsterCondition(int pIndex, int pStateId)
    {
        var zustand = MonsterConditions(pIndex);
        zustand.Remove(pStateId);
        if (pStateId == DeathConditionId)
        {
            SetMonsterExit(pIndex, MonsterExit.Immediate);
        }
    }

    /// <summary>
    /// The condition id RPG Maker uses for death, which is 1 in every 2K
    /// database.
    /// </summary>
    public const int DeathConditionId = 1;

    /// <summary>
    /// Whether a monster is hidden, from 13150 Show Hidden Monster.
    /// </summary>
    /// <remarks>
    /// <strong>Only the false direction exists.</strong> The reference's whole
    /// command is <c>enemy->SetHidden(false)</c> — there is no parameter and
    /// no other arm, so a monster starts hidden in the database and this
    /// command is the only thing that shows it again.
    /// </remarks>
    public bool IsMonsterHidden(int pIndex)
    {
        var m = MonsterAt(pIndex);
        return m is not null && m.ContainsKey("hidden") && (int)m["hidden"] != 0;
    }

    /// <summary>
    /// Shows a monster that was hidden.
    /// </summary>
    public void ShowMonster(int pIndex)
    {
        var m = MonsterAt(pIndex);
        if (m is null)
        {
            return;
        }

        m["hidden"] = 0;
    }

    /// <summary>
    /// The troop member at an index, or null when the index is out of range.
    /// </summary>
    /// <remarks>
    /// <strong>Null and not a new empty monster.</strong> Every one of these
    /// commands starts with the reference's own
    /// <c>GetEnemy(id)</c> plus a warning when it returns nothing, and a
    /// reader that created a monster instead would have grown the troop with
    /// every bad id a game contains.
    /// </remarks>
    private Godot.Collections.Dictionary? MonsterAt(int pIndex)
    {
        if (pIndex < 0 || pIndex >= TroopMembers.Count)
        {
            return null;
        }

        return TroopMembers[pIndex];
    }
    public bool IsBattleActive { get; set; }
    public int BattleTurn { get; set; }
    public int BattlePhase { get; set; } // 0=initial, 1=player, 2=enemy, 3=reward, 4=escape, -1=none

    /// <summary>What a battle may be escaped from, from <c>10710</c>'s fourth parameter.</summary>
    /// <remarks>
    /// <strong>Three modes, and the middle one ends the event.</strong> The
    /// reference's <c>escape_mode</c> is 0 for "not at all", 1 for "end the
    /// event processing" and 2 for "the game's own handler" — **and 1 is not
    /// merely "yes", it changes what happens after the battle ends.** A reader
    /// that treated the parameter as a boolean would have a game whose escape
    /// returned to the event's next line where the reference ends the event
    /// dead.
    /// </remarks>
    public enum BattleEscapeMode
    {
        /// <summary>The battle cannot be escaped.</summary>
        Disallow,

        /// <summary>Escaping ends the event processing.</summary>
        EndEvent,

        /// <summary>The game's own event handlers decide.</summary>
        CustomHandler,
    }

    /// <summary>What a defeat does, from <c>10710</c>'s fifth parameter.</summary>
    public enum BattleDefeatMode
    {
        /// <summary>A defeat is a game over.</summary>
        GameOver,

        /// <summary>The game's own event handlers decide.</summary>
        CustomHandler,
    }

    /// <summary>How the battle's terrain is decided, from <c>10710</c>'s third.</summary>
    /// <remarks>
    /// <strong>Three values, and a fourth is refused.</strong> The reference's
    /// switch has cases 0, 1 and 2 and a <c>default</c> that returns false — so
    /// a command with a mode of 3 does not start a battle at all, and a reader
    /// that defaulted to the first would have fought a battle the file did not
    /// ask for.
    /// </remarks>
    public enum BattleTerrainMode
    {
        /// <summary>The system's own terrain setting.</summary>
        System,

        /// <summary>A background the command's string names.</summary>
        Background,

        /// <summary>A terrain id from the command's eighth parameter.</summary>
        TerrainId,
    }

    /// <summary>Whether the party may escape, from <c>10710</c>.</summary>
    public BattleEscapeMode BattleEscape { get; set; } = BattleEscapeMode.Disallow;

    /// <summary>How the battle's terrain is decided, from <c>10710</c>'s third.</summary>
    /// <remarks>
    /// <strong>The mode, and not the terrain id itself.</strong> A reader that
    /// stored the id would lose the difference between "the system's own
    /// setting" and "this id" — and the first is what a game that never
    /// touches the terrain uses.
    /// </remarks>
    public BattleTerrainMode BattleTerrain { get; set; } = BattleTerrainMode.System;

    /// <summary>What a defeat does, from <c>10710</c>.</summary>
    public BattleDefeatMode BattleDefeat { get; set; } = BattleDefeatMode.GameOver;

    /// <summary>The party strikes first, from <c>10710</c>'s sixth parameter.</summary>
    public bool BattleFirstStrike { get; set; }

    /// <summary>Which subcommand the battle's outcome selected, or -1.</summary>
    /// <remarks>
    /// <strong>-1 is "no battle yet" and not "no outcome".</strong> The
    /// reference's continuation writes 0 for a victory, 1 for an escape and 2
    /// for a defeat into the command's own subcommand index — and the arms
    /// after the battle command read that index. **A reader that stored the
    /// outcome as an enum would need a fourth value for "the battle is still
    /// running", and that value is the one a game reads most.**
    /// </remarks>
    /// <summary>
    /// How a battle ended, from 13410 and the three outcome handlers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Four values, and "abort" is one of them and not a
    /// defeat.</strong> The reference's <c>CommandTerminateBattle</c> builds
    /// <c>AsyncOp::MakeTerminateBattle(static_cast&lt;int&gt;(BattleResult::Abort))</c>
    /// — <strong>so 13410 does not end the battle, it ends it as an
    /// abort</strong>, which is a fourth outcome beside victory, escape and
    /// defeat. A reader that wrote "defeat" there would have had a game that
    /// deliberately abandons a fight reach the game over screen.
    /// </para>
    /// <para>
    /// And the command returns false, not true — it asks the frame to stop,
    /// and the result arrives later.
    /// </para>
    /// </remarks>
    public enum BattleResult
    {
        /// <summary>The battle is still running.</summary>
        None,

        /// <summary>The party won, and 20710's block runs.</summary>
        Victory,

        /// <summary>The party fled, and 20711's block runs.</summary>
        Escape,

        /// <summary>The party lost, and 20712's block runs.</summary>
        Defeat,

        /// <summary>
        /// 13410: the battle was abandoned, and <em>no</em> handler runs —
        /// the reference names no option for it.
        /// </summary>
        Abort,
    }

    /// <summary>How the battle in hand ended, or that it is still running.</summary>
    public BattleResult Result { get; set; } = BattleResult.None;

    /// <summary>The enemy the party is aiming at, from 13310's fourth mode.</summary>
    /// <remarks>
    /// <strong>Only a 2003 game has it, and only in the fourth mode.</strong> The
    /// reference guards the whole case with <c>Player::IsRPG2k3Commands()</c>,
    /// so a 2K game's monster-is-the-target branch is always false — and a
    /// reader that evaluated it anyway would have had a 2K game's branch taken
    /// by an enemy's number in a file that never carried one.
    /// </remarks>
    /// <summary>What the last battle branch evaluated to, from 13310.</summary>
    /// <remarks>
    /// <strong>Kept for the branch's own sake and not for anything else's.</strong>
    /// A game's cutscene reads it through a variable, not through this field —
    /// but a reader that kept nothing would have had the six modes evaluated
    /// and discarded, and a test could not tell a branch that ran from one
    /// that did not.
    /// </remarks>
    public bool LastBattleBranch { get; set; }

    /// <summary>
    /// The hero whose turn it is, which 13310's fifth mode names.
    /// </summary>
    /// <remarks>
    /// <strong>And the reference compares the hero's <em>last battle
    /// action</em> with the command, not the hero's name with it.</strong> So
    /// this is two fields — the hero and the number of the command they last
    /// chose — and a game that never wrote the second has it at zero.
    /// </remarks>
    public int CurrentActorId { get; set; }

    /// <summary>The battle command the current hero last chose.</summary>
    public int LastBattleAction { get; set; }

    /// <summary>
    /// Chooses a subcommand option, and the reference's own
    /// <c>SetSubcommandIndex</c>.
    /// </summary>
    /// <remarks>
    /// <strong>Writing it before the skip is what makes the else branch
    /// run.</strong> The reference sets the sub-index and then calls its skip
    /// — <strong>in that order</strong> — so the else handler that comes next
    /// finds its own option chosen. A reader that skipped first and wrote
    /// afterwards would have had the else branch skip itself.
    /// </remarks>
    public void SetSubcommandIndex(int pIndent, int pOptionSubIdx)
    {
        SubcommandIndex = pOptionSubIdx;
    }


    public int CurrentTargetIndex { get; set; }

    /// <summary>Whether the party is aiming at a single enemy at all.</summary>
    /// <remarks>
    /// <strong>The reference compares two values and the first is a
    /// flag.</strong> <c>targets_single_enemy &amp;&amp; target_enemy_index ==
    /// com.parameters[1]</c> — so a battle with all monsters targeted at once
    /// never matches, whatever the index says.
    /// </remarks>
    public bool TargetsSingleEnemy { get; set; }

    /// <summary>
    /// Whether a hero can act, which 13310's second mode asks.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Three ways not to be able to act, and the reference's own
    /// <c>CanAct()</c> is the sum of them.</strong> A hero at zero hit points
    /// cannot, a hero with a condition that prevents it cannot, and a hero
    /// asleep cannot.
    /// </para>
    /// <para>
    /// And an id that is not in the database is a warning and a false, not an
    /// error — the reference writes nothing to the state and leaves
    /// <c>result</c> at the false it was initialised to.
    /// </para>
    /// </remarks>
    public bool CanHeroAct(int pActorId)
    {
        if (pActorId < 1 || pActorId > MaxActorId)
        {
            return false;
        }

        return GetActorCurrentHp(pActorId) > 0;
    }

    /// <summary>
    /// Whether a monster can act, which 13310's third mode asks.
    /// </summary>
    /// <remarks>
    /// <strong>And a monster at zero is not simply out of the troop.</strong> The
    /// reference's own command that killed it started a death timer, so the
    /// monster is still in the list while it animates — <strong>and a reader
    /// that asked "is he in the troop" instead of "can he act" would have had
    /// a dying boss able to strike back on the frame he fell.</strong>
    /// </remarks>
    public bool CanMonsterAct(int pIndex)
    {
        if (pIndex < 0 || pIndex >= TroopMembers.Count)
        {
            return false;
        }

        var monster = TroopMembers[pIndex];
        // **The cast is `(int)` and not `Convert.ToInt32`** -- TroopMembers
        // is a Godot dictionary, so every read is a Variant and the
        // IConvertible route throws at run time.
        return (int)monster["hp"] > 0
            && !IsMonsterHidden(pIndex);
    }

    /// <summary>Whether a figure is asleep, which is 13310's fifth mode.</summary>
    /// <remarks>
    /// <strong>Condition 3 is the sleep, in every 2K database.</strong> A
    /// reader that read the mode as "is dead" would have asked the wrong
    /// question of a sleeping hero and got a different answer.
    /// </remarks>
    public const int SleepConditionId = 3;


    public int BattleSubcommand { get; set; } = -1;

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
