using System;
using System.Collections.Generic;

namespace UniversalRPG.Rm2k.Simulation;

/// <summary>
/// One event's live move route state, reproduced from
/// <c>Game_Character::UpdateMoveRoute</c>.
/// </summary>
/// <remarks>
/// <para>
/// The Player advances a route one command per update, and a command that
/// starts a step returns immediately. The character then spends the rest of the
/// step budget walking before the next command is read. That return is the
/// whole reason a route looks like walking rather than teleporting along a
/// list, and dropping it would be the single most visible way to get this
/// wrong.
/// </para>
/// <para>
/// The index is the player's <c>GetMoveRouteIndex()</c>. On reaching the end
/// of a non repeating route the character stops and
/// <c>IsMoveRouteFinished()</c> becomes true; a repeating route wraps to the
/// start. <c>start_index</c> is captured when the route is forced, and a route
/// that wraps all the way back to it is how the Player detects completion
/// rather than comparing against the length, which is what makes a route that
/// is stopped and restarted behave the same.
/// </para>
/// </remarks>
public sealed class Rm2kMoveRouteState
{
	/// <summary>liblcf <c>rpg::MoveRoute::move_commands</c>.</summary>
	public IReadOnlyList<Rm2kMap.MoveCommand> Commands { get; }

	/// <summary>liblcf <c>rpg::MoveRoute::repeat</c>.</summary>
	public bool Repeat { get; }

	/// <summary>liblcf <c>rpg::MoveRoute::skippable</c>.</summary>
	public bool Skippable { get; }

	/// <summary>liblcf <c>EventPage::move_speed</c>, default 3.</summary>
	public int MoveSpeed { get; set; } = 3;

	/// <summary>liblcf <c>EventPage::move_frequency</c>, default 3.</summary>
	public int MoveFrequency { get; set; } = 3;

	/// <summary>
	/// The direction the character is facing, in the liblcf order this state
	/// machine works in: 0 up, 1 right, 2 down, 3 left. The event stores the RPG
	/// Maker byte order, so <see cref="Rm2kMoveRoute.FacingFromLiblcfDirection"/>
	/// converts between the two.
	/// </summary>
	public int Direction { get; set; } = 2;

	/// <summary>The player's <c>GetMoveRouteIndex()</c>.</summary>
	public int CurrentIndex { get; private set; }

	/// <summary>The player's <c>IsMoveRouteFinished()</c>.</summary>
	public bool Finished { get; private set; }

	/// <summary>Whether a route is running at all.</summary>
	public bool Active { get; private set; }

	/// <summary>
	/// The index the route was forced at, which is what the Player compares
	/// against to notice that a repeating route came all the way round.
	/// </summary>
	public int StartIndex { get; private set; }

	/// <summary>How many commands have been refused because the way was blocked.</summary>
	public int MoveFailureCount { get; private set; }

	public Rm2kMoveRouteState(
		IReadOnlyList<Rm2kMap.MoveCommand>? pCommands = null,
		bool pRepeat = true,
		bool pSkippable = false)
	{
		Commands = pCommands ?? Array.Empty<Rm2kMap.MoveCommand>();
		Repeat = pRepeat;
		Skippable = pSkippable;
	}

	/// <summary>
	/// <c>ForceMoveRoute</c>: starts the route at the given index and marks it
	/// unfinished. The index is the player's own so that a save file can
	/// restore a route part way through.
	/// </summary>
	public void Force(int pIndex)
	{
		CurrentIndex = pIndex;
		StartIndex = pIndex;
		Finished = false;
		Active = true;
		MoveFailureCount = 0;
	}

	/// <summary><c>CancelMoveRoute</c>: stops the route where it stands.</summary>
	public void Cancel()
	{
		Active = false;
	}

	/// <summary>
	/// <c>GetMoveRouteIndex</c> and <c>SetMoveRouteIndex</c>, so a save can
	/// restore a route part way through.
	/// </summary>
	public void SetIndex(int pIndex)
	{
		CurrentIndex = pIndex;
	}

	/// <summary>
	/// Whether the route has been overwritten by a forced route, which is what
	/// the Player reports through <c>IsMoveRouteOverwritten</c>.
	/// </summary>
	public bool Overwritten { get; private set; }

	/// <summary><c>SetMoveRouteOverwritten</c>.</summary>
	public void SetOverwritten(bool pForce)
	{
		Overwritten = pForce;
	}

	/// <summary>
	/// The command at the current index, or null when the route has run out or
	/// has not been started. A route that is finished is not wrapped here: the
	/// wrapping is the caller's decision, because it depends on the repeat
	/// flag and on the start index.
	/// </summary>
	public Rm2kMap.MoveCommand? Current
	{
		get
		{
			if (!Active || Finished || Commands.Count == 0)
			{
				return null;
			}
			if (CurrentIndex < 0 || CurrentIndex >= Commands.Count)
			{
				return null;
			}
			return Commands[CurrentIndex];
		}
	}

	/// <summary>
	/// Records that a step was refused. The Player keeps this count so it can
	/// stop a route that is stuck against a wall, and resets it after every
	/// successful command.
	/// </summary>
	public void NoteMoveFailure()
	{
		MoveFailureCount++;
	}

	/// <summary>
	/// Advances the index past the command that was just handled.
	/// <c>SetMoveFailureCount(0); ++current_index;</c> and then the wrap check:
	/// a route whose index lands back on the start index has come all the way
	/// round, which is how the Player knows a repeating route is done rather
	/// than looping forever.
	/// </summary>
	public void Advance()
	{
		MoveFailureCount = 0;
		CurrentIndex++;

		if (Commands.Count == 0)
		{
			// A page with no route must not wrap forever on an empty list. The
			// Player's own loop cannot reach this case because it checks the
			// route pointer first, so the guard belongs here: an empty list is
			// finished, not a route that resets its index and spins.
			CurrentIndex = StartIndex;
			Finished = true;
			Active = false;
			return;
		}

		if (CurrentIndex >= Commands.Count)
		{
			if (!Repeat)
			{
				// A non repeating route stops on the step past its last command
				// and reports itself finished.
				CurrentIndex = Commands.Count;
				Finished = true;
				Active = false;
				return;
			}
			CurrentIndex = 0;
		}

		if (CurrentIndex == StartIndex)
		{
			Finished = true;
			Active = false;
		}
	}
}
