using System;
using System.Collections.Generic;

using UniversalRPG.Rm2k;
using UniversalRPG.Rm2k.Interpreter;
using UniversalRPG.Rm2k.Presentation;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Commands 11010, 11020 and 11030 — the two screen transitions and the tint.
/// </summary>
/// <remarks>
/// <para>
/// The transition tables are the sharpest thing in this slice.
/// <strong>Show and erase are the same 21 kinds read from opposite ends, and the
/// pairing is not regular</strong> — the stripe and scroll arms mirror their
/// suffix while the division and combine arms invert their meaning, so a reader
/// that mirrored the name would pair <c>CrossDivision</c> with itself.
/// </para>
/// <para>
/// The expectations come from the reference's two <c>switch</c>es, read arm by
/// arm.
/// </para>
/// </remarks>
public partial class TestRm2kScreen : TestBase
{
	private static Rm2kMap.EventCommand Transition(int pCode, int pKind)
	{
		return new Rm2kMap.EventCommand
		{
			Code = pCode,
			Text = "",
			Parameters = [pKind],
		};
	}

	/// <summary>
	/// <c>[red, green, blue, saturation, tenths, wait]</c>.
	/// </summary>
	private static Rm2kMap.EventCommand Tint(
		int pRed = 0, int pGreen = 0, int pBlue = 0, int pSaturation = 100,
		int pTenths = 0, int pWait = 0)
	{
		return new Rm2kMap.EventCommand
		{
			Code = EventInterpreter.TintScreen,
			Text = "",
			Parameters = [pRed, pGreen, pBlue, pSaturation, pTenths, pWait],
		};
	}

	private static (EventInterpreter Interpreter, PresentationState Presentation,
		GameSimulationState State) Run(params Rm2kMap.EventCommand[] pCommands)
	{
		var state = new GameSimulationState { MapId = 1 };
		var presentation = new PresentationState();
		var interpreter = new EventInterpreter(state, 1, pCommands, presentation);
		return (interpreter, presentation, state);
	}

	/// <summary>
	/// The twenty-one kinds, for the erase table, from the reference's switch.
	/// </summary>
	private static readonly Rm2kTransition[] EraseArms =
	[
		Rm2kTransition.FadeOut, Rm2kTransition.RandomBlocks,
		Rm2kTransition.RandomBlocksDown, Rm2kTransition.RandomBlocksUp,
		Rm2kTransition.BlindClose, Rm2kTransition.VerticalStripesOut,
		Rm2kTransition.HorizontalStripesOut, Rm2kTransition.BorderToCenterOut,
		Rm2kTransition.CenterToBorderOut, Rm2kTransition.ScrollUpOut,
		Rm2kTransition.ScrollDownOut, Rm2kTransition.ScrollLeftOut,
		Rm2kTransition.ScrollRightOut, Rm2kTransition.VerticalDivision,
		Rm2kTransition.HorizontalDivision, Rm2kTransition.CrossDivision,
		Rm2kTransition.ZoomIn, Rm2kTransition.MosaicOut,
		Rm2kTransition.WaveOut, Rm2kTransition.CutOut,
	];

	/// <summary>
	/// The same twenty-one parameters, for the show table.
	/// </summary>
	private static readonly Rm2kTransition[] ShowArms =
	[
		Rm2kTransition.FadeIn, Rm2kTransition.RandomBlocks,
		Rm2kTransition.RandomBlocksDown, Rm2kTransition.RandomBlocksUp,
		Rm2kTransition.BlindOpen, Rm2kTransition.VerticalStripesIn,
		Rm2kTransition.HorizontalStripesIn, Rm2kTransition.BorderToCenterIn,
		Rm2kTransition.CenterToBorderIn, Rm2kTransition.ScrollUpIn,
		Rm2kTransition.ScrollDownIn, Rm2kTransition.ScrollLeftIn,
		Rm2kTransition.ScrollRightIn, Rm2kTransition.VerticalCombine,
		Rm2kTransition.HorizontalCombine, Rm2kTransition.CrossCombine,
		Rm2kTransition.ZoomOut, Rm2kTransition.MosaicIn,
		Rm2kTransition.WaveIn, Rm2kTransition.CutIn,
	];

	/// <summary>
	/// Every parameter of the erase table names what the reference's switch
	/// says it names.
	/// </summary>
	/// <remarks>
	/// **Twenty parameters, twenty kinds, checked one at a time.** A table is
	/// the kind of thing that looks right and is wrong in one entry, and a
	/// mutation moves exactly one entry.
	/// </remarks>
	public void Test_EveryEraseParameterNamesWhatTheReferenceNames()
	{
		for (var p = 0; p < EraseArms.Length; p++)
		{
			var ergebnis = Rm2kTransitionKind.FromParameter(
				p, Rm2kTransitionDirection.Erase);
			AssertEq(
				(int)ergebnis.Transition, (int)EraseArms[p],
				$"and parameter {p} of the erase table is the kind the"
					+ $" reference's switch gives it");
		}
	}

	/// <summary>
	/// And every parameter of the show table, which is the same list read from
	/// the other end.
	/// </summary>
	public void Test_EveryShowParameterNamesWhatTheReferenceNames()
	{
		for (var p = 0; p < ShowArms.Length; p++)
		{
			var ergebnis = Rm2kTransitionKind.FromParameter(
				p, Rm2kTransitionDirection.Show);
			AssertEq(
				(int)ergebnis.Transition, (int)ShowArms[p],
				$"and parameter {p} of the show table is the kind the"
					+ $" reference's switch gives it");
		}
	}

	/// <summary>
	/// The pairing is not a mirror of the names, and that is the trap.
	/// </summary>
	/// <remarks>
	/// The stripes and the scrolls mirror their suffix — <c>BlindClose</c> pairs
	/// with <c>BlindOpen</c> — but <strong>the divisions pair with the combines</strong>,
	/// so a reader that mirrored the name would pair <c>CrossDivision</c> with
	/// itself and animate nothing at all.
	/// </remarks>
	public void Test_TheDivisionsPairWithTheCombinesAndNotWithThemselves()
	{
		for (var p = 0; p < EraseArms.Length; p++)
		{
			var erase = EraseArms[p];
			var show = ShowArms[p];
			// **The three division arms are the ones that break a mirror.**
			var istDivision = erase is Rm2kTransition.VerticalDivision
				or Rm2kTransition.HorizontalDivision
				or Rm2kTransition.CrossDivision;
			if (!istDivision)
			{
				continue;
			}
			AssertTrue(
				show != erase,
				$"**and parameter {p} does not pair a division with itself**,"
					+ $" because the reference's two switches give"
					+ $" {erase} and {show} and a name mirror would have given"
					+ " the same kind twice");
			AssertTrue(
				show.ToString().EndsWith("Combine", StringComparison.Ordinal),
				$"and the show side of parameter {p} is a combine, because the"
					+ $" two lists are opposites rather than mirrors; it is {show}");
		}
	}

	/// <summary>
	/// A transition command reaches the presentation state.
	/// </summary>
	public void Test_AShowCommandReachesThePresentationState()
	{
		var (interpreter, presentation, _) = Run(
			Transition(EventInterpreter.ShowScreen, 0));

		interpreter.ExecuteFrame();

		AssertEq(
			presentation.PendingTransition, Rm2kTransition.FadeIn,
			"**and the pending transition is FadeIn**, because parameter 0 of the"
			+ $" show table is a fade in; it is {presentation.PendingTransition}");
		AssertEq(
			presentation.PendingTransitionDirection,
			Rm2kTransitionDirection.Show,
			"and the direction is show, from the command's own number, because a"
			+ " reader that picked the direction from the kind would get this"
			+ " one right by luck and every other wrong; it is"
			+ $" {presentation.PendingTransitionDirection}");
	}

	/// <summary>
	/// The same parameter number means different things for the two commands.
	/// </summary>
	public void Test_TheSameNumberMeansDifferentKindsForTheTwoCommands()
	{
		var (interpreter, presentation, _) = Run(
			Transition(EventInterpreter.EraseScreen, 4));

		interpreter.ExecuteFrame();

		AssertEq(
			presentation.PendingTransition, Rm2kTransition.BlindClose,
			"**and parameter 4 of the erase table is a blind close**, where the"
			+ $" show table has a blind open at the same number; it is"
			+ $" {presentation.PendingTransition}");
		AssertEq(
			presentation.PendingTransitionDirection,
			Rm2kTransitionDirection.Erase,
			"and the direction is erase; it is"
			+ $" {presentation.PendingTransitionDirection}");
	}

	/// <summary>
	/// Parameter −1 is the game's own teleport transition, not a kind.
	/// </summary>
	/// <remarks>
	/// The reference reaches for
	/// <c>GetTransition(Transition_TeleportErase)</c> or
	/// <c>GetTransition(Transition_TeleportShow)</c>, which live in the editor's
	/// settings and not in the command. <strong>No reader here has read those
	/// settings, so it says so</strong> — the reference falls through to none for
	/// a number it does not know, and that is what would make every teleport in
	/// a game lose its transition without a word.
	/// </remarks>
	public void Test_TheTeleportSentinelIsNotAKind()
	{
		var (interpreter, presentation, state) = Run(
			Transition(EventInterpreter.ShowScreen, -1));

		interpreter.ExecuteFrame();

		AssertEq(
			presentation.PendingTransition, Rm2kTransition.None,
			"**and no transition was chosen**, because the game's own teleport"
			+ $" transition is not in the file; the pending one is"
			+ $" {presentation.PendingTransition}");
		AssertTrue(
			ContainsDiagnostic(state, "teleport transition"),
			"and the diagnostic says that, because a reader that reported"
			+ " \"none\" without saying why would look like a game that asked"
			+ $" for nothing; the diagnostics are {state.Diagnostics.Count}");
	}

	/// <summary>
	/// A parameter outside both tables says so instead of falling through.
	/// </summary>
	public void Test_AParameterOutsideTheTablesSaysSo()
	{
		var (interpreter, _, state) = Run(
			Transition(EventInterpreter.EraseScreen, 99));

		interpreter.ExecuteFrame();

		AssertTrue(
			ContainsDiagnostic(state, "names no transition"),
			"**and the diagnostic says the number named nothing**, because the"
			+ " reference's switch has no default arm and falls through in"
			+ $" silence; the diagnostics are {state.Diagnostics.Count}");
		AssertTrue(
			ContainsDiagnostic(state, "99"),
			"and it names the number, because \"none\" without it cannot be"
			+ $" looked up; the diagnostics are {state.Diagnostics.Count}");
	}

	/// <summary>
	/// A message holds the transition instead of running behind it.
	/// </summary>
	public void Test_AMessageHoldsTheTransition()
	{
		var (interpreter, presentation, state) = Run(
			Transition(EventInterpreter.ShowScreen, 0));
		presentation.ShowMessage("A line of dialogue");

		interpreter.ExecuteFrame();

		AssertEq(
			presentation.PendingTransition, Rm2kTransition.None,
			"**and no transition was chosen**, because the reference returns"
			+ $" false while a message is open; the pending one is {presentation.PendingTransition}");
		AssertTrue(
			ContainsDiagnostic(state, "a message is open"),
			"and the diagnostic says the message held it, because a transition"
			+ " that ran behind a dialogue window is invisible and unexplained;"
			+ $" the diagnostics are {state.Diagnostics.Count}");
	}

	/// <summary>
	/// The tint stores four numbers and a duration in frames.
	/// </summary>
	/// <remarks>
	/// <strong>The duration is converted, not stored as tenths.</strong> The
	/// reference does `tenths * DEFAULT_FPS / 10` and hands the result to the
	/// screen, so a reader that kept tenths would report a number the engine
	/// never had.
	/// </remarks>
	public void Test_ATintStoresFourNumbersAndFramesNotTenths()
	{
		var (interpreter, presentation, _) = Run(
			Tint(64, 32, 16, 80, pTenths: 25));

		interpreter.ExecuteFrame();

		AssertEq(
			presentation.TintRed, 64,
			"and red is 64, from parameters[0]; it is {presentation.TintRed}");
		AssertEq(
			presentation.TintGreen, 32,
			"and green is 32, from parameters[1]; it is {presentation.TintGreen}");
		AssertEq(
			presentation.TintBlue, 16,
			"and blue is 16, from parameters[2]; it is {presentation.TintBlue}");
		AssertEq(
			presentation.TintSaturation, 80,
			"**and the saturation is 80, a percentage**, from parameters[3]; it"
			+ $" is {presentation.TintSaturation}");
		AssertEq(
			presentation.TintFramesRemaining, 150,
			"**and the duration is 150 frames, not 25 tenths**, because the"
			+ " reference converts with tenths * DEFAULT_FPS / 10; the remaining"
			+ $" frames are {presentation.TintFramesRemaining}");
		AssertEq(
			presentation.IsTintActive, true,
			"and the tint is active; it is {presentation.IsTintActive}");
	}

	/// <summary>
	/// Saturation 100 is the untinted screen, and 0 is a tint.
	/// </summary>
	/// <remarks>
	/// **This is backwards from what a reader would guess.** A reader that
	/// treated 0 as "no tint" would tint the screen to grey at the one value a
	/// game writes when it wants no tint, and clear it at the value that means
	/// "leave the colours alone".
	/// </remarks>
	public void Test_SaturationHundredIsUntintedAndZeroIsATint()
	{
		var (interpreter, presentation, _) = Run(
			Tint(255, 0, 0, pSaturation: 100));
		interpreter.ExecuteFrame();
		AssertEq(
			presentation.TintSaturation, 100,
			"**and 100 is stored as 100**, because it means the screen keeps its"
			+ $" colours; the saturation is {presentation.TintSaturation}");

		var (zweit, pres2, _) = Run(Tint(255, 0, 0, pSaturation: 0));
		zweit.ExecuteFrame();
		AssertEq(
			pres2.TintSaturation, 0,
			"**and 0 is stored as 0 and the tint is still on**, because 0 means"
			+ $" fully tinted rather than not tinted; the saturation is"
			+ $" {pres2.TintSaturation} and the tint is {pres2.IsTintActive}");
	}

	/// <summary>
	/// The tint runs down and ends, on the same tick as the flash.
	/// </summary>
	public void Test_TheTintEndsWhenItsFramesAreUp()
	{
		var (_, presentation, _) = Run(Tint(0, 0, 255, 60, pTenths: 5));
		presentation.TintScreen(0, 0, 255, 60, 30);

		presentation.Tick(10);
		AssertEq(
			presentation.TintFramesRemaining, 20,
			"**and ten frames of thirty are gone**, because the tint ticks"
			+ $" with the flash and the shake; the remaining frames are {presentation.TintFramesRemaining}");

		presentation.Tick(30);
		AssertEq(
			presentation.IsTintActive, false,
			"**and it ended**, because a tint that outlived its duration would"
			+ $" leave the screen coloured after the game said it was over; it is {presentation.IsTintActive}");
		AssertEq(
			presentation.TintFramesRemaining, 0,
			"and the counter is zero, not negative, because a negative count is"
			+ $" a state nobody can read; it is {presentation.TintFramesRemaining}");
	}

	/// <summary>
	/// A tint that does not wait advances the page; one that does holds it.
	/// </summary>
	public void Test_OnlyATintThatAsksToWaitHoldsThePage()
	{
		var (ohne, _, state1) = Run(Tint(0, 0, 0, 100, 10, pWait: 0));
		ohne.ExecuteFrame();
		AssertEq(
			ohne.CurrentCommandIndex, 1,
			"**and the page moved on**, because the reference calls"
			+ $" SetupWait only when the last parameter is set; the index is {ohne.CurrentCommandIndex}");

		var (mit, _, _) = Run(Tint(0, 0, 0, 100, 10, pWait: 1));
		mit.ExecuteFrame();
		AssertEq(
			mit.CurrentCommandIndex, 0,
			"**and a tint that asks to wait holds the page**, for as long as it"
			+ $" runs; the index is {mit.CurrentCommandIndex}");
		AssertEq(
			mit.WaitFramesRemaining, 60,
			"**and it waits for the same duration the tint runs** — ten tenths is"
			+ $" sixty frames; the remaining wait is {mit.WaitFramesRemaining}");
		_ = state1;
	}

	/// <summary>
	/// A tint with too few parameters is refused.
	/// </summary>
	public void Test_ATintWithTooFewParametersIsRefused()
	{
		var (interpreter, presentation, state) = Run(
			new Rm2kMap.EventCommand
			{
				Code = EventInterpreter.TintScreen,
				Text = "",
				Parameters = [0, 0, 0, 100, 10],
			});

		interpreter.ExecuteFrame();

		AssertEq(
			presentation.IsTintActive, false,
			"**and nothing was tinted**, because CmdSetup gives the command a"
			+ $" minimum width of six; the tint is {presentation.IsTintActive}");
		AssertTrue(
			ContainsDiagnostic(state, "malformed"),
			"and the diagnostic says so; the diagnostics are"
			+ $" {state.Diagnostics.Count}");
	}

	/// <summary>
	/// A new game starts with no tint and no pending transition.
	/// </summary>
	public void Test_ATintDoesNotSurviveANewGame()
	{
		var (interpreter, presentation, _) = Run(
			Tint(255, 0, 0, 100, 20), Transition(EventInterpreter.ShowScreen, 0));
		interpreter.ExecuteFrame();
		interpreter.ExecuteFrame();
		AssertEq(
			presentation.IsTintActive, true, "sanity: the tint is on");

		presentation.Reset();

		AssertEq(
			presentation.IsTintActive, false,
			"**and a new game has no tint**, because a game that begins with"
			+ $" the last one's screen coloured is a game with a bug; the tint is {presentation.IsTintActive}");
		AssertEq(
			presentation.PendingTransition, Rm2kTransition.None,
			"**and no transition is pending**, because a game that begins mid"
			+ $" transition is a game with a cutscene; it is {presentation.PendingTransition}");
	}

	private static bool ContainsDiagnostic(
		GameSimulationState pState, string pNeedle)
	{
		foreach (var line in pState.Diagnostics)
		{
			if (line.Contains(pNeedle, StringComparison.Ordinal))
			{
				return true;
			}
		}
		return false;
	}
}
