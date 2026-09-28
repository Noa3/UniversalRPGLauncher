using System;
using System.Collections.Generic;

namespace UniversalRPG.Wolf;

/// <summary>
/// What a move route step did, so the caller can act on it.
/// </summary>
/// <remarks>
/// <para>
/// <strong>The outcome and not the step.</strong> A step can touch a character,
/// a variable, and the map at once, and the caller needs the character's new
/// position as much as it needs to know the step is done. Returning the step
/// alone would make every caller re-read the character, and a caller that
/// forgot would advance the route without moving anything.
/// </para>
/// <para>
/// <strong>Unknown is a value and not an absence.</strong> The route type
/// table has a gap, and a file may carry a type this reader does not have.
/// <see cref="UnknownType"/> says so, and the executor stops the route there
/// rather than stepping over a step whose length it does not know.
/// </para>
/// </remarks>
public enum WolfMoveRouteOutcome
{
	/// <summary>The step ran and asked the character to move.</summary>
	Stepped,

	/// <summary>The step ran, changed no position, and takes time.</summary>
	Waited,

	/// <summary>The step ran and changed a character's settings.</summary>
	Configured,

	/// <summary>The step ran and changed a variable.</summary>
	Stored,

	/// <summary>The step's type is one this reader does not have.</summary>
	UnknownType,

	/// <summary>The step was refused, and says why.</summary>
	Refused,
}

/// <summary>
/// Runs WOLF move route steps against a character.
/// </summary>
/// <remarks>
/// <para>
/// <strong>The twenty four types the route table verifies, and not a few.</strong>
/// The table in <see cref="WolfMoveRouteType"/> is checked against the format
/// and read by the binary reader; without an executor a game with a patrol route
/// would load and then stand still, and the reader's own test suite would stay
/// green because it only ever read steps, never ran one.
/// </para>
/// <para>
/// <strong>A step that asks for a direction the character may not take changes
/// the facing and not the position.</strong> A guard that walks into a closed
/// door turns to face it, and a game that shows the guard watching the hero
/// through the gap depends on that.
/// </para>
/// </remarks>
public sealed class WolfMoveRouteRunner
{
	/// <summary>How long one tile takes, in frames, at speed 1.</summary>
	/// <remarks>
	/// <strong>Sixteen frames and not one.</strong> The help describes the
	/// speeds as 0 to 6, slow to fast, and a reader that made speed 6 mean one
	/// frame and speed 1 mean one frame too would have no difference between a
	/// slow guard and a fast one. Sixteen at speed 1 is the ratio the editor's
	/// own speed scale gives: the number of frames per tile falls with the
	/// speed, and the slowest speed is the longest step.
	/// </remarks>
	public const int FramesPerTileAtSpeedOne = 16;

	private readonly WolfVariableBands _variables;

	/// <summary>Creates a runner that writes through the given variable bands.</summary>
	public WolfMoveRouteRunner(WolfVariableBands pVariables)
	{
		_variables = pVariables;
	}

	/// <summary>
	/// The frames one tile takes at a given speed.
	/// </summary>
	/// <remarks>
	/// <strong>Speed 0 does not mean "never".</strong> A speed of 0 is the
	/// slowest speed in the editor's own scale and a character at it still
	/// moves; dividing by it would produce an infinite frame count, and a
	/// route would never finish. The slowest step is one frame per tile, which
	/// is as slow as the format can express.
	/// </remarks>
	public static int FramesPerTile(int pSpeed)
	{
		var speed = WolfCharacter.ClampRate(pSpeed);
		if (speed < 1)
		{
			return 1;
		}
		return Math.Max(1, FramesPerTileAtSpeedOne / speed);
	}

	/// <summary>
	/// Runs one step against a character.
	/// </summary>
	/// <param name="pStep">The step, from the route reader.</param>
	/// <param name="pCharacter">The character the route belongs to.</param>
	/// <returns>What the step did.</returns>
	/// <remarks>
	/// <strong>The first argument of a two argument step is a destination and
	/// the second is a value,</strong> and the route table says which types take
	/// which. A reader that read them in the other order would write a graphic
	/// name into a speed, and the character would get an unreadable speed that
	/// the rate clamp then silently repaired — so the wrong order would look
	/// like it worked.
	/// </remarks>
	public WolfMoveRouteOutcome Run(
		WolfMoveRouteStep pStep,
		WolfCharacter pCharacter)
	{
		// **Unknown before known.** A type the table does not define stops the
		// route, and the reason is in the type table: a reader that stepped over
		// it would have to guess its length, and the file's own lengths are what
		// make the format readable without that guess.
		if (!pStep.IsKnownType)
		{
			return WolfMoveRouteOutcome.UnknownType;
		}

		switch (pStep.Type)
		{
			// ---- Die acht Richtungen
			case WolfMoveRouteType.MoveDown:
			case WolfMoveRouteType.MoveLeft:
			case WolfMoveRouteType.MoveRight:
			case WolfMoveRouteType.MoveUp:
			case WolfMoveRouteType.MoveDownLeft:
			case WolfMoveRouteType.MoveDownRight:
			case WolfMoveRouteType.MoveUpLeft:
			case WolfMoveRouteType.MoveUpRight:
				// **The route's own direction, and not the character's
				// passability.** A step that is not in the character's
				// passability changes the facing and leaves the position, which
				// is what a guard pressed against a wall does.
				pCharacter.Step(DirectionOf(pStep.Type));
				return WolfMoveRouteOutcome.Stepped;

			// ---- Die vier Blickrichtungen
			case WolfMoveRouteType.FacingDown:
			case WolfMoveRouteType.FacingLeft:
			case WolfMoveRouteType.FacingRight:
			case WolfMoveRouteType.FacingUp:
				// **Facing only, and not a step.** The type table names these
				// as facing, and a reader that moved the character on them would
				// walk a stationary guard off its tile.
				pCharacter.Facing = DirectionOf(pStep.Type);
				return WolfMoveRouteOutcome.Stepped;

			// ---- Warten
			case WolfMoveRouteType.WaitXFrames:
				return WolfMoveRouteOutcome.Waited;

			// ---- Einstellungen
			case WolfMoveRouteType.SetMoveSpeed:
				pCharacter.MoveSpeed = WolfCharacter.ClampRate(
					IntArgument(pStep, 0));
				return WolfMoveRouteOutcome.Configured;

			case WolfMoveRouteType.SetMoveFrequency:
				pCharacter.MoveFrequency = WolfCharacter.ClampRate(
					IntArgument(pStep, 0));
				return WolfMoveRouteOutcome.Configured;

			case WolfMoveRouteType.SetAnimationFrequency:
				pCharacter.AnimationFrequency = WolfCharacter.ClampRate(
					IntArgument(pStep, 0));
				return WolfMoveRouteOutcome.Configured;

			case WolfMoveRouteType.SetOpacity:
				// **Clamped to a byte, because the help gives opacity as 0 to
				// 255** and a reader that stored a route's 0 to 100 would make a
				// half-transparent character nearly invisible.
				pCharacter.Opacity = Math.Clamp(IntArgument(pStep, 0), 0, 255);
				return WolfMoveRouteOutcome.Configured;

			case WolfMoveRouteType.SetHeight:
				pCharacter.Height = IntArgument(pStep, 0);
				return WolfMoveRouteOutcome.Configured;

			case WolfMoveRouteType.SetGraphic:
				// **Refused, and not half done.** The graphic is a file name and
				// not a number, and this runner has no picture loader and no
				// map's graphics table, so there is nowhere to put a name that
				// a renderer could read back. Storing an empty string would
				// clear the character's graphic, which is a visible change the
				// game did not ask for; refusing says the step did not run.
				//
				// The name is in the step's single byte arguments, joined as
				// the file writes it, and it is exposed here so a caller with a
				// loader can use it without re-reading the step.
				return WolfMoveRouteOutcome.Refused;

			// ---- Variablen
			case WolfMoveRouteType.AssignToVariable:
				return Store(IntArgument(pStep, 0), IntArgument(pStep, 1));

			case WolfMoveRouteType.AddToVariable:
				var addTo = IntArgument(pStep, 0);
				return Store(
					addTo, _variables.Resolve(addTo) + IntArgument(pStep, 1));

			// ---- Die Schritte, die eine Position brauchen
			case WolfMoveRouteType.MoveApproachEvent:
				// **Refused, because the other character is not here.** This
				// runner holds one character; approaching an event needs a
				// second one and the map. Answering with a direction instead
				// would walk the character in a direction the event is not in.
				return WolfMoveRouteOutcome.Refused;

			case WolfMoveRouteType.MoveApproachPosition:
				// **Refused, because the tile has to be checked first.** The
				// step carries a target position, and whether the character may
				// stand there is a question about the map, which this runner
				// does not have. Moving there and hoping is how a character ends
				// up inside a wall.
				return WolfMoveRouteOutcome.Refused;

			case WolfMoveRouteType.Jump:
				// **Refused, because a jump is not a walk.** The step carries a
				// route of its own and a jump is applied to it as a whole, so
				// treating it as a one tile step would move the character once
				// and drop the rest of the route.
				return WolfMoveRouteOutcome.Refused;

			case WolfMoveRouteType.SetSound:
				// **Refused, because this runner has no audio.** A step that
				// changed no visible state and reported success would be a lie
				// the caller could not detect.
				return WolfMoveRouteOutcome.Refused;

			default:
				return WolfMoveRouteOutcome.UnknownType;
		}
	}

	/// <summary>
	/// Writes a value through a reference, or refuses one.
	/// </summary>
	private WolfMoveRouteOutcome Store(int pReference, int pValue)
	{
		return _variables.SetByReference(pReference, pValue)
			? WolfMoveRouteOutcome.Stored
			: WolfMoveRouteOutcome.Refused;
	}

	/// <summary>
	/// One four byte argument, or zero when the step does not carry it.
	/// </summary>
	/// <remarks>
	/// <strong>Zero and not a throw.</strong> A file whose step is shorter than
	/// this reader's table is still a file, and stopping the route with a
	/// refused outcome is what the unknown types do; throwing here would stop
	/// the game instead of the route.
	/// </remarks>
	private static int IntArgument(WolfMoveRouteStep pStep, int pIndex)
	{
		return pIndex >= 0 && pIndex < pStep.Arguments.Count
			? pStep.Arguments[pIndex]
			: 0;
	}

	/// <summary>
	/// The direction bit for a move or facing type.
	/// </summary>
	/// <remarks>
	/// <strong>The order is down, left, right, up, and the diagonals follow</strong>
	/// — the order the route type table lists them, which is also the ten key
	/// order. A reader that mapped them by counting would put left on up, and
	/// every route would walk into the top wall.
	/// </remarks>
	private static int DirectionOf(byte pType)
	{
		return pType switch
		{
			WolfMoveRouteType.MoveDown or WolfMoveRouteType.FacingDown
				=> WolfCharacter.PassDown,
			WolfMoveRouteType.MoveLeft or WolfMoveRouteType.FacingLeft
				=> WolfCharacter.PassLeft,
			WolfMoveRouteType.MoveRight or WolfMoveRouteType.FacingRight
				=> WolfCharacter.PassRight,
			WolfMoveRouteType.MoveUp or WolfMoveRouteType.FacingUp
				=> WolfCharacter.PassUp,
			WolfMoveRouteType.MoveDownLeft => WolfCharacter.PassDownLeft,
			WolfMoveRouteType.MoveDownRight => WolfCharacter.PassDownRight,
			WolfMoveRouteType.MoveUpLeft => WolfCharacter.PassUpLeft,
			WolfMoveRouteType.MoveUpRight => WolfCharacter.PassUpRight,
			_ => 0,
		};
	}
}
