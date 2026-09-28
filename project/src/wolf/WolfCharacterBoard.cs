using System;
using System.Collections.Generic;

namespace UniversalRPG.Wolf;

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

	/// <summary>Creates a board over the given variable bands.</summary>
	public WolfCharacterBoard(WolfVariableBands pVariables)
	{
		_runner = new WolfMoveRouteRunner(pVariables);
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
			return Hero;
		}
		_characters[pCharacter.Id] = pCharacter;
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
	}

	/// <summary>Reads a character by event id.</summary>
	public WolfCharacter? Find(int pId)
	{
		return pId == 0
			? Hero
			: _characters.TryGetValue(pId, out var found) ? found : null;
	}

	/// <summary>Every character on the map, the hero first.</summary>
	public IReadOnlyList<WolfCharacter> All
	{
		get
		{
			var list = new List<WolfCharacter> { Hero };
			list.AddRange(_characters.Values);
			return list;
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
			Mode = pRoute.Mode,
			WaitUntilDone = pRoute.WaitUntilDone,
			SkipImpossibleMoves = pRoute.SkipImpossibleMoves,
			RepeatActions = pRoute.RepeatActions,
			Index = 0,
			FramesLeft = 0,
			IsStepRunning = false,
		};
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
				continue;
			}
			TickOne(character, pair.Value);
		}
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

			// **The two refusals are decided before the timing, and that order
			// is the whole of the fix.** An earlier version asked the timing
			// first, so a step that was refused still got a frame budget — and
			// a route whose first step was impossible ran its whole frame
			// budget before it noticed, which is sixteen frames of a guard
			// standing at a wall and then a stop nobody asked for. Asking the
			// timing of a step that did not run is also a question with no
			// answer: there is nothing to time.
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
	}
}
