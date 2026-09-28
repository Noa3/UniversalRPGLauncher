using System;
using System.Collections.Generic;

namespace UniversalRPG.Wolf;

/// <summary>
/// What one approach step did.
/// </summary>
/// <remarks>
/// <strong>Four answers, and a bool could not carry them.</strong> "Moved" and
/// "already there" are both successes with different timing, and "no such
/// target" and "blocked" are both failures with the same handling. A reader
/// that answered with a bool would have had to pick one of each pair to
/// discard, and the one it discarded is where a guard's route would end.
/// </remarks>
public enum WolfApproachResult
{
	/// <summary>The figure moved one tile toward the target.</summary>
	Stepped,

	/// <summary>The figure is already beside the target.</summary>
	Arrived,

	/// <summary>The target names nobody on this board.</summary>
	NoTarget,

	/// <summary>The step was refused: a wall, or another figure in the way.</summary>
	Blocked,
}

/// <summary>
/// The characters on the map, and the routes they are running.
/// </summary>
/// <remarks>
/// <para>
/// <strong>The VM needs this and did not have it.</strong> The route reader
/// produced a <see cref="WolfMoveRoute"/> with steps, the route type table
/// verified twenty four types, and nothing anywhere held a figure to run them
/// on — so a game with a patrol route loaded and stood still.
/// </para>
/// <para>
/// <strong>Routes are not steps.</strong> A route is a list of steps plus four
/// flags the format carries, and the flags decide what happens when the list
/// runs out and when a step is impossible. <see cref="WolfMoveRouteRunner"/>
/// runs one step; this holds the queue and the timing, so the caller does not
/// have to reinvent either.
/// </para>
/// </remarks>
public sealed class WolfCharacterBoard
{
	private readonly Dictionary<int, WolfCharacter> _characters = new();

	/// <summary>
	/// The characters, as a list the collision test can walk.
	/// </summary>
	/// <remarks>
	/// <strong>A field and not a fresh list per question.</strong> The collision
	/// test asks once per step, and a step can be refused, and a fresh list each
	/// time would allocate on every step of every figure in the game. More
	/// importantly it has to be the *same* list for every figure, or two
	/// figures asked at the same moment would see different worlds.
	/// </remarks>
	private List<WolfCharacter> _occupants = [];

	/// <summary>The hero, whose event id is zero.</summary>
	public WolfCharacter Hero { get; private set; } = new() { Id = 0 };

	/// <summary>The map the board is on.</summary>
	public int MapId { get; set; }

	/// <summary>
	/// The map's passability, in two layers.
	/// </summary>
	/// <remarks>
	/// <strong>The grid is the map, and the width and height come from it.</strong>
	/// A board with a width of 20 and a grid of 10 would let a figure walk to
	/// tile 15 and read passability from a row that does not exist — and a
	/// reader that answered "passable" out there would put a guard in the void.
	/// The width and height are therefore the grid's, and setting them alone
	/// does nothing.
	/// </remarks>
	public WolfPassabilityGrid? Passability { get; private set; }

	/// <summary>How wide the map is, in tiles, from the grid.</summary>
	public int Width => Passability?.Width ?? 0;

	/// <summary>How tall the map is, in tiles, from the grid.</summary>
	public int Height => Passability?.Height ?? 0;

	/// <summary>How far the hero is in, in pixels.</summary>
	public int ScrollX { get; set; }

	/// <summary>How far the hero is down, in pixels.</summary>
	public int ScrollY { get; set; }

	/// <summary>
	/// The route running for one character, or null when none is.
	/// </summary>
	private readonly Dictionary<int, RunningRoute> _routes = new();

	/// <summary>
	/// One character with the route it is running and the step it is on.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>The index, the route and the frame budget are one state and not
	/// three.</strong> The frames left on a step are the only thing that makes
	/// a move take time, and a caller that kept them separately would have to
	/// know which character they belong to. This is the third attempt at that
	/// shape, and it is the one where the timing and the position cannot
	/// disagree.
	/// </para>
	/// </remarks>
	private sealed class RunningRoute
	{
		/// <summary>The route's steps, in order.</summary>
		public required IReadOnlyList<WolfMoveRouteStep> Steps { get; init; }

		/// <summary>How the route is driven, from the mode table.</summary>
		public int Mode { get; init; }

		/// <summary>Whether the event waits for the route to finish.</summary>
		public bool WaitUntilDone { get; init; }

		/// <summary>Whether an impossible step is skipped instead of stopping.</summary>
		public bool SkipImpossibleMoves { get; init; }

		/// <summary>Whether the route repeats when it runs out.</summary>
		public bool RepeatActions { get; init; }

		/// <summary>
		/// The event this route belongs to, for the target number -1.
		/// </summary>
		/// <remarks>
		/// <strong>Remembered and not looked up.</strong> The help's target list
		/// says <c>-1＝このイベント</c> — this event — and "this" means the
		/// program the route is running in. The runner has no such context, and
		/// the board is what knows which event started a route, so the number
		/// travels with the route.
		/// </remarks>
		public int OwnerId { get; init; }

		/// <summary>The step the route is on.</summary>
		public int Index { get; set; }

		/// <summary>The frames still to spend on the step at <see cref="Index"/>.</summary>
		public int FramesLeft { get; set; }

		/// <summary>Whether the route has run out of steps.</summary>
		public bool IsFinished { get; set; }

		/// <summary>
		/// Whether a step is still spending its frames.
		/// </summary>
		/// <remarks>
		/// <para>
		/// <strong>A flag and not a frames-left test, and the difference is
		/// one frame per step.</strong> The index points at the <em>next</em>
		/// step, because a step advances it when it runs — so a frames-left of
		/// zero is ambiguous: it means either "this step is still on its last
		/// frame" or "no step has started". An earlier version decided that with
		/// <c>FramesLeft &gt; 0</c> and the route ended a frame late, and a
		/// version that checked the index first threw the running step's frames
		/// away entirely and ran every step in every second frame.
		/// </para>
		/// <para>
		/// With the flag the arithmetic is exact: a step of n frames is
		/// started by one of them, so it is finished on the nth tick, and the
		/// tick that finishes it also looks at the next step.
		/// </para>
		/// </remarks>
		public bool IsStepRunning { get; set; }
	}

	/// <summary>The runner that applies the steps.</summary>
	private readonly WolfMoveRouteRunner _runner;

	/// <summary>
	/// The bands, so an approach step can read a coordinate out of a variable.
	/// </summary>
	/// <remarks>
	/// **The same bands the runner writes to, and not a second set.</strong> The
	/// help allows a variable wherever a number is entered, and a coordinate of
	/// 2,000,000 is normal variable 0 — a reader with its own bands would
	/// resolve it against a store the event does not read, and the guard would
	/// walk to a coordinate nobody set.
	/// </remarks>
	private readonly WolfVariableBands _variables;

	/// <summary>Creates a board over the given variable bands.</summary>
	public WolfCharacterBoard(WolfVariableBands pVariables)
	{
		_runner = new WolfMoveRouteRunner(pVariables);
		_variables = pVariables;
		// **The hero is in the cast before anything else happens.** The
		// occupant list starts empty, and the first call a game makes is
		// usually LoadMap — which hands the grid to whoever is on the list. An
		// empty list meant the hero never got the map, so the hero could not
		// step at all and a game with an event on it opened with a player who
		// was stuck. **The hero is placed here rather than by Add**, because a
		// caller who never places the hero still needs one.
		RefreshOccupants();
	}

	/// <summary>
	/// Puts a character on the map, or returns the one already there.
	/// </summary>
	/// <remarks>
	/// <strong>The same id twice is the same character, not two.</strong> A map
	/// event that appears in two programs refers to one figure, and a reader
	/// that appended would give the game two figures at one tile — one of them
	/// invisible, because the renderer draws by id.
	/// </remarks>
	public WolfCharacter Add(WolfCharacter pCharacter)
	{
		// **The map is handed to the figure as it is placed, and not looked up
		// at step time.** A figure placed before the map was loaded would have
		// to be revisited, and a reader that forgot one figure would leave a
		// guard walking through walls while the hero did not.
		pCharacter.PassabilityGrid = Passability;
		if (pCharacter.Id == 0)
		{
			Hero = pCharacter;
			RefreshOccupants();
			return Hero;
		}
		_characters[pCharacter.Id] = pCharacter;
		RefreshOccupants();
		return pCharacter;
	}

	/// <summary>
	/// Loads a map's passability, and hands it to every figure on the board.
	/// </summary>
	/// <remarks>
	/// <strong>Every figure, and not only the ones that move.</strong> A figure
	/// placed before the map was loaded has the same question as one placed
	/// after, and handing the grid to the board's figures at once is the only
	/// way the two can be answered the same way.
	/// </remarks>
	public void LoadMap(int pMapId, WolfPassabilityGrid pGrid)
	{
		MapId = pMapId;
		Passability = pGrid;
		foreach (var character in All)
		{
			character.PassabilityGrid = pGrid;
		}
		RefreshOccupants();
	}

	/// <summary>Reads a character by event id.</summary>
	public WolfCharacter? Find(int pId)
	{
		return pId == 0
			? Hero
			: _characters.TryGetValue(pId, out var found) ? found : null;
	}

	/// <summary>Every character on the map, the hero first.</summary>
	public IReadOnlyList<WolfCharacter> All => _occupants;

	/// <summary>
	/// Rebuilds the occupant list and hands it to every figure.
	/// </summary>
	/// <remarks>
	/// <strong>After every change to the cast, and not lazily.</strong> A figure
	/// added in the middle of a frame would otherwise be invisible to the
	/// collision test until something asked, and a guard who walked into a
	/// newly placed event would stand inside it.
	/// </remarks>
	private void RefreshOccupants()
	{
		_occupants = [Hero];
		_occupants.AddRange(_characters.Values);
		foreach (var character in _occupants)
		{
			character.Occupants = () => _occupants;
			// **The map is (re)given to every figure here, and not only where
			// it is placed.** A figure placed before the map was loaded has the
			// same question as one placed after, and handing the grid at
			// placement time alone would leave the earlier figures walking
			// through walls — which is why the hero needed this: the board
			// exists before its first Add.
			if (Passability is { } grid)
			{
				character.PassabilityGrid = grid;
			}
		}
	}

	/// <summary>
	/// Starts a route for a character, replacing any route it had.
	/// </summary>
	/// <remarks>
	/// <strong>Replace and not queue.</strong> The format's route command sets
	/// the character's movement, and a second route command overrides the first
	/// — a character that queued them would walk the old patrol after the new
	/// one, which is a behaviour no game asks for.
	/// </remarks>
	public void StartRoute(int pId, WolfMoveRoute pRoute)
	{
		if (Find(pId) is not { } character)
		{
			return;
		}
		_routes[pId] = new RunningRoute
		{
			Steps = pRoute.Steps,
			OwnerId = pId,
			Mode = pRoute.Mode,
			WaitUntilDone = pRoute.WaitUntilDone,
			SkipImpossibleMoves = pRoute.SkipImpossibleMoves,
			RepeatActions = pRoute.RepeatActions,
			Index = 0,
			FramesLeft = 0,
			IsStepRunning = false,
		};
	}

	/// <summary>
	/// The party, so a companion target has something to name.
	/// </summary>
	/// <remarks>
	/// **The board's own, and not a party's the caller keeps.</strong> The
	/// help's target list reaches a route step — a guard approaching the third
	/// companion is a route step, and a route runs on the board. A party
	/// elsewhere would have to be handed in at every step, and a caller that
	/// forgot would make every companion target resolve to nothing.
	/// </remarks>
	public WolfParty Party { get; } = new();

	/// <summary>
	/// The sounds the commands ask for, and what has been asked for since.
	/// </summary>
	/// <remarks>
	/// **The board's own, and for the same reason as the party.** A sound step
	/// runs on a route, and a route runs on the board; a sound state held
	/// elsewhere would have to be handed in at every step, and a caller that
	/// forgot would lose the game's background music without an error anywhere.
	/// </remarks>
	public WolfAudioState Audio { get; } = new();

	/// <summary>
	/// The file name a step's single byte arguments spell, in the editor's
	/// own encoding.
	/// </summary>
	/// <param name="pStep">The step to read.</param>
	/// <returns>The name, or an empty string when the step carries none.</returns>
	/// <remarks>
	/// <para>
	/// <strong>The name is in the single byte arguments, and not in the four
	/// byte numbers.</strong> A route step's shape is a type, a count of four
	/// byte values, those values, a count of single byte values, and those — and
	/// a file name is text, so it is in the second list. A reader that read the
	/// four byte values as a name would produce a number where a path belongs,
	/// and a game that asked for <c>SE/door.ogg</c> would be handed something
	/// like <c>-18867289</c>.
	/// </para>
	/// <para>
	/// <strong>The name is not packed four bytes to a value.</strong> That was
	/// what this comment claimed first, and the reader does not do it: it puts
	/// each single byte into its own list entry in file order, so the name is the
	/// second list joined back together. **The four byte half is the numbers** —
	/// the volume, the frequency and the delay — and a reader that looked for a
	/// file name there would find three integers and wonder why no track played.
	/// </para>
	/// </remarks>
	public static string NameOf(WolfMoveRouteStep pStep)
	{
		if (pStep.ByteArguments.Count == 0)
		{
			return string.Empty;
		}
		// **The bytes are the name, in order, and they are already split.** The
		// reader put each single byte into the list in file order, so joining
		// them back is the name — and the four byte half is the numbers, which
		// is where the volume and the frequency live.
		var chars = new char[pStep.ByteArguments.Count];
		for (var index = 0; index < chars.Length; index++)
		{
			chars[index] = (char)pStep.ByteArguments[index];
		}
		return new string(chars);
	}

	/// <summary>
	/// How many walking patterns the game's settings use: three or five.
	/// </summary>
	/// <remarks>
	/// **A setting and not a constant, because it decides the sheet's shape.**
	/// The material specification says the character's layout changes with the
	/// game's animation pattern setting, and a reader that assumed three would
	/// cut a five pattern sheet in half and show every character mid-stride.
	/// </remarks>
	public int CharacterPatternCount { get; set; } = WolfCharacterSheet.Patterns3;

	/// <summary>
	/// Whether the sheet has eight directions or four.
	/// </summary>
	/// <remarks>
	/// **The setting, because four and eight cannot be mixed in one game** —
	/// the setting guide says so twice. A reader that guessed would show half
	/// the characters from the wrong column of an eight direction sheet.
	/// </remarks>
	public bool EightDirectionCharacters { get; set; }

	/// <summary>
	/// How many idle frames a row carries: none, one, or three.
	/// </summary>
	/// <remarks>
	/// **None for a plain sheet, one for T.png, three for TX.png.** The
	/// material specification gives both special forms, and the sheet's
	/// column count depends on it — a reader that used the pattern count would
	/// cut the idle frames off every row.
	/// </remarks>
	public int CharacterIdleFrames { get; set; }

	/// <summary>
	/// The sheet cell a figure should be showing right now.
	/// </summary>
	/// <param name="pCharacter">The figure to place on a sheet.</param>
	/// <param name="pRow">The row, counted from the top.</param>
	/// <param name="pColumn">The column, counted from the left.</param>
	/// <returns>
	/// False when the figure's direction has no cell on this sheet — a
	/// diagonal on a four direction sheet.
	/// </returns>
	/// <remarks>
	/// <para>
	/// <strong>Walking and standing are two different cells,</strong> and the
	/// standing one is the idle column when the sheet has one. A figure that is
	/// not walking shows its idle cell and does not advance, which is what makes
	/// the TX form's standing animation work at all.
	/// </para>
	/// <para>
	/// <strong>The row is found from the facing and the column from the
	/// animation state,</strong> and they come from two different places on
	/// purpose: a figure's facing is a direction bit, and its animation is a
	/// counter, and neither one can produce the other.
	/// </para>
	/// </remarks>
	public bool CellOf(WolfCharacter pCharacter, out int pRow, out int pColumn)
	{
		if (!WolfCharacterSheet.Locate(
			pCharacter.Facing, EightDirectionCharacters, out pRow, out pColumn))
		{
			// **No cell, and the caller is told.** A diagonal on a four
			// direction sheet has no picture, and a reader that clamped it to
			// the nearest cardinal would draw a figure facing down while it
			// walks down-left.
			pRow = 0;
			pColumn = 0;
			return false;
		}
		var patterns = CharacterPatternCount >= WolfCharacterSheet.Patterns5
			? WolfCharacterSheet.Patterns5
			: WolfCharacterSheet.Patterns3;
		// **The idle frame is column 0 when the sheet has one, and the walk
		// starts after it.** The material specification says the T form adds one
		// idle cell to the left of each direction's walk, and a reader that
		// added the offset the other way would put the standing pose in the
		// middle of the walk — which is a figure that never stops walking and
		// never appears to stand.
		// **The offset is applied once, to the walk, and the idle is not
		// offset at all.** The idle cells sit to the left of the walk, so their
		// own index is already right; the walk cells have to be pushed right by
		// the number of idle cells in front of them. An earlier version added
		// the offset to both, which put a walking figure one column too far
		// right and made it show a pose the artist never drew.
		if (!pCharacter.IsWalking)
		{
			// **A plain sheet has no idle cell, so a standing figure shows the
			// first walk cell.** That is the three pattern case the guide
			// describes, and it is why a sheet without an idle frame has no
			// standing pose at all.
			pColumn = CharacterIdleFrames <= 0
				? 0
				: WolfCharacterSheet.IdleCell(
					pCharacter.AnimationStep, patterns, CharacterIdleFrames);
			return true;
		}
		pColumn = WolfCharacterSheet.WalkPattern(pCharacter.AnimationStep, patterns)
			+ Math.Max(0, CharacterIdleFrames);
		return true;
	}

	/// <summary>
	/// Finds the figure a target number names.
	/// </summary>
	/// <param name="pTarget">A target number, in the help's numbering.</param>
	/// <param name="pThisEvent">The event the running route belongs to.</param>
	/// <returns>The figure, or null where the number names nobody here.</returns>
	/// <remarks>
	/// <para>
	/// <strong>Roles first, then event ids, and the negative numbers are never
	/// ids.</strong> The help is explicit: 0 and above is the event with that id,
	/// -1 is this event, -2 is the hero, and -3 to -7 are the companions. A reader
	/// that looked up minus two as an id would find nothing and answer "nowhere",
	/// and a guard walking to the hero would walk to the wall instead.
	/// </para>
	/// <para>
	/// <strong>A number the help does not offer is nothing, and says so.</strong>
	/// -8 is not the sixth companion, because the list stops at -7; answering it
	/// with a sixth member would put a figure on the map the editor cannot name.
	/// </para>
	/// </remarks>
	public WolfCharacter? FindTarget(int pTarget, int pThisEvent)
	{
		switch (WolfCharacterTarget.Classify(pTarget))
		{
			case WolfCharacterTarget.Kind.ThisEvent:
				return Find(pThisEvent);
			case WolfCharacterTarget.Kind.Hero:
				// **The party, and not the board's hero.** A companion who is
				// also on the map is the board's hero when it is the player, and
				// a reader that answered from the board would ignore a party
				// that names a different figure as the player — which is what a
				/// game does when the player character changes.
				return Party.Hero;
			case WolfCharacterTarget.Kind.Companion:
				return Party.Companion(WolfCharacterTarget.CompanionNumber(pTarget));
			case WolfCharacterTarget.Kind.EventId:
				return Find(pTarget);
			default:
				return null;
		}
	}

	/// <summary>Whether a character is running a route.</summary>
	public bool IsMoving(int pId)
	{
		return _routes.TryGetValue(pId, out var route) && !route.IsFinished;
	}

	/// <summary>Whether every running route has run out of steps.</summary>
	/// <remarks>
	/// <strong>This is what "wait until done" waits for.</strong> The format's
	/// wait option holds the event until the character's movement finishes, and
	/// a reader that counted the steps instead would let a waiting event
	/// continue while the figure is still walking into the next step.
	/// </remarks>
	public bool IsEveryRouteFinished()
	{
		foreach (var route in _routes.Values)
		{
			if (!route.IsFinished)
			{
				return false;
			}
		}
		return true;
	}

	/// <summary>
	/// Advances every running route by one frame.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>One frame, and not "one step".</strong> A step that moves takes
	/// its frames from the character's speed, and a reader that ran a whole
	/// step in one tick would make a slow guard and a fast one identical — the
	/// speed setting would be a number nothing read.
	/// </para>
	/// <para>
	/// <strong>Steps that do not move take no time</strong>, other than the wait
	/// step, which takes exactly the frames it carries. Facing, settings and
	/// variable steps are instant in the format, and a reader that spent a
	/// frame on each would slow every route in the game by a factor of the step
	/// count.
	/// </para>
	/// </remarks>
	public void Tick()
	{
		foreach (var pair in _routes)
		{
			var character = Find(pair.Key);
			if (character == null || pair.Value.IsFinished)
			{
				// **A finished route stands still,** and the animation has to
				// stop with it. A reader that left IsWalking set would show a
				// guard that has arrived walking in place for ever.
				if (character != null)
				{
					character.IsWalking = false;
				}
				continue;
			}
			TickOne(character, pair.Value);
		}
	}

	/// <summary>
	/// Advances one frame's animation for every figure.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>The frequency is 0 to 6 and it is frames per step, and the order
	/// is the opposite of the speed.</strong> The help writes
	/// <c>アニメ頻度[早0-6遅]</c> — often to rarely — while the move speed is
	/// slow to fast. **A reader that divided by the frequency, or that used the
	/// speed, would make a figure whose feet blur also cross the map in a blur.**
	/// </para>
	/// <para>
	/// <strong>Zero is every frame and not never.</strong> The help's scale puts
	/// 0 at the fast end, and a figure set to it animates on every frame; a
	/// reader that divided by zero would freeze the animation entirely.
	/// </para>
	/// </remarks>
	public void TickAnimation()
	{
		foreach (var character in _occupants)
		{
			// **A standing figure does not advance,** and a sheet with idle
			// frames animates those instead. A reader that advanced both would
			// have the walk continue under a figure that is not walking.
			if (!character.IsWalking || character.IsErased)
			{
				continue;
			}
			// **The counter carries its own budget, and that is where the
			// frequency lives.** Advancing every frame would make the frequency a
			// number nothing read — a figure's feet would blur and its settings
			// would do nothing, which is the same failure the move speed had
			// before the board got a clock.
			character.AnimationCountdown =
				(character.AnimationCountdown <= 0
					? AnimationFrames(character.AnimationFrequency) - 1
					: character.AnimationCountdown - 1);
			if (character.AnimationCountdown <= 0)
			{
				character.AnimationStep += 1;
			}
		}
	}

	/// <summary>
	/// How many frames one animation step takes, from a frequency of 0 to 6.
	/// </summary>
	/// <remarks>
	/// <strong>One frame at the fastest and eight at the slowest.</strong> The
	/// help gives the scale and not the numbers, so the ratio is this reader's
	/// choice — **the only thing that depends on it is how fast a figure's feet
	/// move, and a game that disagrees with this is a game whose feet look
	/// wrong rather than whose logic is wrong.**
	/// </remarks>
	public static int AnimationFrames(int pFrequency)
	{
		var frequency = WolfCharacter.ClampRate(pFrequency);
		// **One at the fast end and never zero,** because a zero would mean the
		// animation never advances and a figure with the fastest setting would
		// stand frozen.
		return 1 + frequency;
	}

	/// <summary>
	/// Advances one character's route by one frame.
	/// </summary>
	/// <remarks>
	/// <strong>The loop over the index and the loop over the frames are the
	/// same loop.</strong> A step that takes no time is run and the next one
	/// starts in the same frame; that is what a route of ten facing commands
	/// does in the format, and splitting the two would make it take ten frames.
	/// </remarks>
	private void TickOne(WolfCharacter pCharacter, RunningRoute pRoute)
	{
		// **Bounded, because a route whose steps all take no time would
		// otherwise spin here forever.** The bound is the step count, so every
		// step runs at most once in a frame, and a route of ten facing commands
		// still finishes in one frame.
		var guard = pRoute.Steps.Count + 1;
		while (guard-- > 0)
		{
			// **A running step is charged its frame, and when the last one is
			// spent the tick carries on to the next step.**
			//
			// Three versions of this loop were wrong in three different ways.
			// The first checked the index before the frames, so a step that had
			// advanced past the last one was already "at the end" — the route
			// ended, the repeat flag reset the index, and the fifteen frames
			// the step had asked for were thrown away with it. The guard then
			// ran every step in every second frame and crossed the screen
			// eight times too fast. The second fixed the order and ended every
			// step a frame late, because the tick that spent the last frame
			// returned instead of looking at what came next. This one spends
			// the frame and continues, and a step of n frames is finished on
			// the nth tick exactly.
			if (pRoute.IsStepRunning)
			{
				pRoute.FramesLeft -= 1;
				if (pRoute.FramesLeft > 0)
				{
					return;
				}
				pRoute.IsStepRunning = false;
			}

			if (pRoute.Index >= pRoute.Steps.Count)
			{
				OnRouteEnded(pRoute);
				return;
			}

			var step = pRoute.Steps[pRoute.Index];
			var outcome = _runner.Run(step, pCharacter);
			pRoute.Index += 1;
			// **Only a step that moved somebody sets the flag.** A facing step
			// or a settings step runs and changes something, and a reader that
			// set the flag on every step would have a figure that turns in place
			// walking — and the walk cycle is the wrong pose for a figure that
			// is not moving, which is the sort of thing a player notices and
			// does not report.
			// **Only a movement, and not a turn.** A figure that turned in place
			// is standing, and drawing it with the walk cycle is a pose the
			// artist never drew for it. A wait is not a movement either.
			pCharacter.IsWalking = outcome == WolfMoveRouteOutcome.Stepped;

			// **The two refusals are decided before the timing, and that order
			// is the whole of the fix.** An earlier version asked the timing
			// first, so a step that was refused still got a frame budget — and
			// a route whose first step was impossible ran its whole frame
			// budget before it noticed, which is sixteen frames of a guard
			// standing at a wall and then a stop nobody asked for. Asking the
			// timing of a step that did not run is also a question with no
			// answer: there is nothing to time.
			// **The sound step is answered here and not by the runner.** It needs
			// the audio state — three channels, the config switches and the zero
			// volume setting — and the runner holds one figure and nothing else.
			// The board has all of it, so the refusal moved here rather than
			// staying a refusal.
			if (step.Type == WolfMoveRouteType.SetSound)
			{
				// **A sound step is instant and it never stops a route.** A
				// player that opened a door and whose route then ended would
				// have the door's script stop at the door, and a reader that
				// gave the step a frame budget would make every footstep wait.
				Audio.Play(SoundOf(step));
				continue;
			}
			// **The two approach steps are answered here and not by the runner.**
			// They need the board — a second figure, or a tile — and the runner
			// holds one figure, so it refused them outright. That refusal was
			// correct while the board had neither; now it has both, and a reader
			// that kept the refusal would have a game whose guards never approach
			// anything.
			if (step.Type == WolfMoveRouteType.MoveApproachEvent
				|| step.Type == WolfMoveRouteType.MoveApproachPosition)
			{
				// **The three answers, and each one ends the step differently.**
				// Arrived and Stepped both let the route carry on, but arrived
				// takes no time and stepped does; NoTarget and Blocked both stop
				// it unless the route says to skip. An earlier version folded
				// arrived into NoTarget, and a guard that had reached its target
				// was recorded as having chased somebody who is not there.
				switch (ApproachOne(pCharacter, pRoute, step))
				{
					case WolfApproachResult.Arrived:
						// **No time, and the next step runs now.** A guard that
						// is already beside its target has arrived; spending a
						// tile's worth of frames standing still would freeze it
						// for sixteen frames before it did anything else.
						pRoute.FramesLeft = 0;
						pRoute.IsStepRunning = false;
						continue;
					case WolfApproachResult.Stepped:
						var approachFrames = WolfMoveRouteRunner.FramesPerTile(
							pCharacter.MoveSpeed);
						if (approachFrames > 0)
						{
							pRoute.FramesLeft = approachFrames - 1;
							pRoute.IsStepRunning = true;
						}
						return;
					default:
						if (!pRoute.SkipImpossibleMoves)
						{
							pRoute.IsFinished = true;
							return;
						}
						continue;
				}
			}
			if (outcome == WolfMoveRouteOutcome.UnknownType)
			{
				// **An unknown type ends the route** rather than being skipped.
				// The type table's gap is deliberate, and stepping over a value
				// the format does not define would mean guessing what the next
				// command is.
				pRoute.IsFinished = true;
				return;
			}
			if (outcome == WolfMoveRouteOutcome.Refused)
			{
				// **A refused step stops the route unless the route says
				// otherwise.** The format carries a flag for exactly this, and a
				// reader that always skipped would let a patrol walk through a
				// wall and a reader that never skipped would freeze a guard
				// whose sound step this reader cannot run.
				if (!pRoute.SkipImpossibleMoves)
				{
					pRoute.IsFinished = true;
					return;
				}
				// **Skipping means moving straight on, in this frame.** A
				// reader that fell through to the timing would give the refused
				// step a frame budget anyway, and the route would pause at the
				// wall for sixteen frames before carrying on — which is the
				// opposite of what the flag asks for.
				continue;
			}

			var frames = FramesFor(step, outcome, pCharacter);
			if (frames > 0)
			{
				// **One less, because the tick that starts the step is its first
				// frame.** A step of sixteen frames is started by one of them,
				// so fifteen are left — and a reader that stored sixteen would
				// make every figure in the game one frame slower.
				pRoute.FramesLeft = frames - 1;
				pRoute.IsStepRunning = true;
				return;
			}
		}
	}

	/// <summary>
	/// Runs one approach step: take one tile toward a target, or say there is
	/// nowhere to go.
	/// </summary>
	/// <param name="pCharacter">The figure the route belongs to.</param>
	/// <param name="pRoute">The route, which knows its own event.</param>
	/// <param name="pStep">The approach step.</param>
	/// <param name="pResult">
	/// What happened: the figure moved, it is already there, or the step cannot
	/// be answered.
	/// </param>
	/// <remarks>
	/// <strong>Three answers and not two, and that is the whole fix.</strong> An
	/// earlier version returned a bool where false meant both "already arrived"
	/// and "no such target", and the caller could not tell them apart — so a
	/// guard that had arrived was treated as having chased a target that does
	/// not exist, and its route ended in a refusal rather than an arrival. The
	/// two are different events and they need different handling.
	/// </remarks>
	/// <remarks>
	/// <para>
	/// <strong>One tile per step, and the target is re-read every step.</strong> A
	/// guard that approaches a moving hero has to keep closing the distance, and
	/// a reader that computed the whole path once would walk to where the hero
	/// was — which is a guard that misses by however far the hero moved.
	/// </para>
	/// <para>
	/// <strong>The axis with the larger gap goes first.</strong> That is the
	/// standard order for a chase and it is what makes a diagonal move
	/// diagonal: a guard five to the right and one down closes the five and
	/// then closes the one, instead of alternating and arriving in half the time.
	/// **The help does not name the order, so it is stated here as a choice**
	/// — a reader that closed both axes at once would produce a diagonal step
	/// the format has no type for.
	/// </para>
	/// </remarks>
	private WolfApproachResult ApproachOne(
		WolfCharacter pCharacter,
		RunningRoute pRoute,
		WolfMoveRouteStep pStep)
	{
		int targetX;
		int targetY;
		if (pStep.Type == WolfMoveRouteType.MoveApproachEvent)
		{
			var target = FindTarget(StepArgument(pStep, 0), pRoute.OwnerId);
			// **A target that names nobody is a step that cannot be answered.**
			// The help's -1 means this event, -2 the hero, -3 to -7 the
			// companions, and 0 and up an event id — so a party with no third
			// companion makes -5 name nobody, and a reader that treated it as
			// coordinates would walk the guard to minus five.
			if (target == null)
			{
				return WolfApproachResult.NoTarget;
			}
			targetX = target.X;
			targetY = target.Y;
		}
		else
		{
			// **The coordinates come from the step and may be a variable,** which
			// the help allows wherever a number is entered. They are resolved
			// through the bands for the same reason every other number is: a
			// value at or above a million is a reference.
			targetX = _variables.Resolve(StepArgument(pStep, 0));
			targetY = _variables.Resolve(StepArgument(pStep, 1));
			if (Passability is not { } grid
				|| !grid.IsInRange(targetX, targetY))
			{
				return WolfApproachResult.NoTarget;
			}
		}

		var gapX = targetX - pCharacter.X;
		var gapY = targetY - pCharacter.Y;
		// **Arrived when the X gap is gone, whatever the Y gap is.** A half
		// height hitbox means standing beside the target counts as being on it,
		// so a guard that has closed the horizontal gap is close enough. A
		// reader that demanded both gaps be zero would have guards shuffling
		// up and down one tile forever.
		if (gapX == 0)
		{
			return WolfApproachResult.Arrived;
		}
		// **The larger gap goes first, and a tie closes X.** A tie is the
		// diagonal case, and a reader that closed Y on a tie would step
		// vertically out of a diagonal and arrive one frame later — which is
		// invisible and wrong.
		var closerX = Math.Abs(gapX) >= Math.Abs(gapY);
		var gap = closerX ? gapX : gapY;
		var direction = gap > 0
			? (closerX ? WolfCharacter.PassRight : WolfCharacter.PassDown)
			: (closerX ? WolfCharacter.PassLeft : WolfCharacter.PassUp);
		if (pCharacter.Step(direction))
		{
			return WolfApproachResult.Stepped;
		}
		// **Blocked by a figure is arrived, and blocked by a wall is not.**
		// This is the one place the collision rule and the approach rule meet,
		// and getting it wrong in either direction breaks a chase: read it as
		// blocked and a guard gives up the moment the hero stands next to it —
		// which is what a guard does when it catches the player, so the game
		// would lose the confrontation. Read it as arrived always and a guard
		// pressed against a wall would stop one tile short of its target and
		// call it done.
		//
		// **The difference is what refused the step, and the board can ask.**
		// The target's own tile is occupied by the target — that is the whole
		// point — so standing beside it is what arriving looks like.
		return WolfCharacterCollision.IsBlockedByAFigure(
			pCharacter, direction)
			? WolfApproachResult.Arrived
			: WolfApproachResult.Blocked;
	}

	/// <summary>
	/// What a sound step asks for, from its arguments and its name.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>The channel comes first, and the numbers after it.</strong> The
	/// material guide describes the sound command with a channel selector, a
	/// file or a database entry, and then the volume, the frequency and the
	/// time — and the format's step carries the numbers as four byte values and
	/// the name as single bytes.
	/// </para>
	/// <para>
	/// <strong>A sound effect's time is a delay and a music track's is a
	/// fade</strong>, and putting both in one field would have a sound effect
	/// start quietly and grow. The material guide is explicit that the fade time
	/// becomes "delay the playback" for an effect, and gives the unit: sixty
	/// frames is one second.
	/// </para>
	/// </remarks>
	private WolfSoundRequest SoundOf(WolfMoveRouteStep pStep)
	{
		var channel = StepArgument(pStep, 0);
		return new WolfSoundRequest
		{
			// **A channel outside the three is the music one, and the reason is
			// stated.** A step from a newer editor could name a channel this
			// reader does not have, and the answer is the one that is silent
			// rather than the one that would be loud and wrong.
			Channel = channel >= 0 && channel <= WolfSoundChannel.MaxChannel
				? channel
				: WolfSoundChannel.Bgm,
			Name = NameOf(pStep),
			Volume = StepArgument(pStep, 1),
			Frequency = StepArgument(pStep, 2),
			DelayFrames = channel == WolfSoundChannel.Se
				? StepArgument(pStep, 3)
				: 0,
			FadeFrames = channel == WolfSoundChannel.Se
				? 0
				: StepArgument(pStep, 3),
		};
	}

	/// <summary>
	/// One four byte argument of a step, or zero when it has none.
	/// </summary>
	private static int StepArgument(WolfMoveRouteStep pStep, int pIndex)
	{
		return pIndex >= 0 && pIndex < pStep.Arguments.Count
			? pStep.Arguments[pIndex]
			: 0;
	}

	/// <summary>
	/// How many frames a step takes.
	/// </summary>
	/// <remarks>
	/// <strong>A moving step takes its time and nothing else does</strong> —
	/// except the wait step, which takes exactly what it says. A reader that
	/// spent a frame on a facing command would slow every route in the game.
	/// </remarks>
	private static int FramesFor(
		WolfMoveRouteStep pStep,
		WolfMoveRouteOutcome pOutcome,
		WolfCharacter pCharacter)
	{
		if (pStep.Type == WolfMoveRouteType.WaitXFrames)
		{
			return Math.Max(1, IntArgument(pStep, 0));
		}
		// **The eight move types are 0x00 to 0x07 and the four facing types are
		// 0x08 to 0x0x0B**, so a numeric range from "MoveDown" to
		// "MoveUpRight" covers both. An earlier version of this method used
		// exactly that range and every facing step took a frame to move — which
		// meant a route of ten facing commands took ten frames, and the frame
		// count the test measured was nine tiles of drift instead of one.
		//
		// **The range ends at the last move type, 0x07, and the facing types
		// are excluded by it.** The boundary is the point: the move types are
		// contiguous from zero and the facing types start where they end.
		return pStep.Type <= WolfMoveRouteType.MoveUpRight
			? WolfMoveRouteRunner.FramesPerTile(pCharacter.MoveSpeed)
			: 0;
	}

	/// <summary>
	/// One four byte argument, or zero when the step does not carry it.
	/// </summary>
	private static int IntArgument(WolfMoveRouteStep pStep, int pIndex)
	{
		return pIndex >= 0 && pIndex < pStep.Arguments.Count
			? pStep.Arguments[pIndex]
			: 0;
	}

	/// <summary>
	/// What happens when a route has no steps left.
	/// </summary>
	/// <remarks>
	/// <strong>Repeat restarts at zero, and no repeat finishes.</strong> The
	/// format carries the flag, and a patrol that stopped after one lap would
	/// be a route that ran once — which is what the flag is for.
	/// </remarks>
	private void OnRouteEnded(RunningRoute pRoute)
	{
		if (pRoute.RepeatActions && pRoute.Steps.Count > 0)
		{
			pRoute.Index = 0;
			pRoute.FramesLeft = 0;
			pRoute.IsStepRunning = false;
			return;
		}
		pRoute.IsFinished = true;
	}

	/// <summary>Empties the board, for a new game.</summary>
	public void Clear()
	{
		_characters.Clear();
		_routes.Clear();
		Hero = new WolfCharacter { Id = 0 };
		MapId = 0;
		// **The grid goes with them.** A board that cleared its figures and
		// kept its passability would let the next game's figures walk on the
		// last game's walls, and the new game would start with a hero who
		// cannot leave the starting tile.
		Passability = null;
		ScrollX = 0;
		ScrollY = 0;
		// **The party goes with them.** A new game that kept the last game's
		// companions would send a guard walking to a figure that is not there.
		Party.Clear();
		// **The sounds go with them, and this is the one the test found.** A
		// game that kept the last one's background music would open its title
		// screen playing the theme of whatever was loaded before it — and
		// nothing else on the board would have caught it, because the figures
		// were gone and there is no figure to look wrong.
		Audio.Clear();
		// **The list goes last and not first**, because clearing it while a
		// figure still points at it would leave the hero asking a list that no
		// longer contains it — and asking is how it is told the world is empty.
		RefreshOccupants();
	}
}
