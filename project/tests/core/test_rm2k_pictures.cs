using System.Collections.Generic;

using UniversalRPG.Rm2k;
using UniversalRPG.Rm2k.Interpreter;
using UniversalRPG.Rm2k.Presentation;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Commands 11110 Show Picture and 11130 Erase Picture, executed.
/// </summary>
/// <remarks>
/// <para>
/// Both were implemented in <c>PresentationState</c>, bounded, and tested —
/// and no event command could reach either. <c>ShowPicture</c> took six scalars
/// and threw the other eight parameters away. <strong>A validated island is not
/// a feature</strong>, and this is the same shape of fault as K-094's vehicles:
/// the state was there and the wiring was not.
/// </para>
/// <para>
/// The parameter list is EasyRPG's <c>CommandShowPicture</c>, whose
/// <c>CmdSetup</c> gives it a minimum width of 14, and the last parameter is
/// optional. The expectations come from the source.
/// </para>
/// </remarks>
public partial class TestRm2kPictures : TestBase
{
	/// <summary>
	/// The fourteen parameters in the reference's order, with the name in the
	/// command's string field.
	/// </summary>
	private static Rm2kMap.EventCommand Show(
		string pName,
		int pId = 1, int pPosMode = 0, int pX = 320, int pY = 240,
		int pFixed = 0, int pMagnify = 100, int pTop = 0, int pUseTop = 0,
		int pRed = 255, int pGreen = 255, int pBlue = 255,
		int pSaturation = 100, int pEffectMode = 0, int pEffectPower = 100,
		int? pBottom = null)
	{
		var parameters = new List<int>
		{
			pId, pPosMode, pX, pY, pFixed, pMagnify, pTop, pUseTop,
			pRed, pGreen, pBlue, pSaturation, pEffectMode, pEffectPower,
		};
		if (pBottom is int bottom)
		{
			parameters.Add(bottom);
		}
		return new Rm2kMap.EventCommand
		{
			Code = EventInterpreter.ShowPicture,
			Text = pName,
			Parameters = parameters,
		};
	}

	/// <summary>
	/// Erase, with the id first and the mode second — the order
	/// <c>CommandErasePicture</c> reads, and <em>not</em> the order the show
	/// command puts its position mode in.
	/// </summary>
	private static Rm2kMap.EventCommand Erase(int pId, int pMode = 0, int? pLast = null)
	{
		var parameters = new List<int> { pId, pMode };
		if (pLast is int last)
		{
			parameters.Add(last);
		}
		return new Rm2kMap.EventCommand
		{
			Code = EventInterpreter.ErasePicture,
			Text = "",
			Parameters = parameters,
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
	/// A picture command reaches the presentation state.
	/// </summary>
	/// <remarks>
	/// **This is the assertion the file exists for.** The state had the method,
	/// the bounds and a test of its own, and the interpreter had no case for the
	/// code — so a game's picture command did nothing and every test was green.
	/// </remarks>
	public void Test_APictureCommandReachesThePresentationState()
	{
		var (interpreter, presentation, _) = Run(Show("Logo", pId: 4, pX: 100, pY: 50));

		interpreter.ExecuteFrame();

		AssertEq(
			presentation.Pictures.Count, 1,
			"and a picture exists, because 11110 is Show Picture and the"
			+ $" command was dispatched; there are {presentation.Pictures.Count}");
		AssertEq(
			presentation.Pictures[4].Name, "Logo",
			"**and it is the picture the command named**, from the command's"
			+ $" string field; the name is \"{presentation.Pictures[4].Name}\"");
		AssertEq(
			presentation.Pictures[4].X, 100,
			"and it is where parameters[2] put it;"
			+ $" x is {presentation.Pictures[4].X}");
		AssertEq(
			presentation.Pictures[4].Y, 50,
			"and parameters[3] put the y; y is {presentation.Pictures[4].Y}");
	}

	/// <summary>
	/// The nine parameters the old signature threw away are all kept.
	/// </summary>
	/// <remarks>
	/// Every one of them is a number the reference reads, and dropping them is
	/// how a picture arrives with the wrong transparency colour and nobody can
	/// say why.
	/// </remarks>
	public void Test_EveryParameterTheReferenceReadsIsKept()
	{
		var (interpreter, presentation, _) = Run(Show(
			"Tinted", pId: 2, pFixed: 1, pMagnify: 150,
			pTop: 50, pUseTop: 1,
			pRed: 128, pGreen: 64, pBlue: 32,
			pSaturation: 150, pEffectMode: 2, pEffectPower: 80,
			pBottom: 100));

		interpreter.ExecuteFrame();
		var bild = presentation.Pictures[2];

		AssertEq(
			bild.FixedToMap, true,
			"**and it moves with the map, because parameters[4] says so**; it is"
			+ $" {bild.FixedToMap}");
		AssertEq(
			bild.Magnify, 150,
			"and the magnification is 150, from parameters[5]; it is {bild.Magnify}");
		AssertEq(
			bild.TopTransparency, 50,
			"**and the top transparency is 50 percent, from parameters[6]** —"
			+ " a percentage and NOT a colour, because the reference clamps it"
			+ " with std::min(top_trans, 100), and NOT a height either, because"
			+ $" the reference sets magnify_height = magnify_width; it is {bild.TopTransparency}");
		AssertEq(
			bild.UseTransparentColor, true,
			"and the transparency is on, from parameters[7]; it is"
			+ $" {bild.UseTransparentColor}");
		AssertEq(
			bild.Red, 128,
			"and red is 128, from parameters[8]; it is {bild.Red}");
		AssertEq(
			bild.Green, 64,
			"and green is 64, from parameters[9]; it is {bild.Green}");
		AssertEq(
			bild.Blue, 32,
			"and blue is 32, from parameters[10]; it is {bild.Blue}");
		AssertEq(
			bild.Saturation, 150,
			"and saturation is 150, from parameters[11]; it is {bild.Saturation}");
		AssertEq(
			bild.EffectMode, 2,
			"and the effect is a horizontal flip, from parameters[12]; it is"
			+ $" {bild.EffectMode}");
		AssertEq(
			bild.EffectPower, 80,
			"and the effect power is 80, from parameters[13]; it is"
			+ $" {bild.EffectPower}");
		AssertEq(
			bild.BottomTransparency, 100,
			"**and the bottom transparency is 100, from parameters[14]**, which"
			+ $" the command carried; it is {bild.BottomTransparency}");
	}

	/// <summary>
	/// The optional last parameter is absent from a fourteen parameter command,
	/// and that is not the same as zero.
	/// </summary>
	/// <remarks>
	/// The reference has a corner case: 2k maps inside 2k3 before 1.10 carry no
	/// second chunk, and it copies the top colour into the bottom in exactly
	/// that case. <strong>A reader that always copied would apply that rule to
	/// every modern command</strong>, where the two chunks are independent.
	/// </remarks>
	public void Test_AShortCommandHasNoSecondColour()
	{
		var (interpreter, presentation, _) = Run(
			Show("OnlyTop", pId: 5, pTop: 75, pUseTop: 1));

		interpreter.ExecuteFrame();
		var bild = presentation.Pictures[5];

		AssertEq(
			bild.TopTransparency, 75,
			"and the top transparency is there, from parameters[6]; it is"
			+ $" {bild.TopTransparency}");
		AssertEq(
			bild.BottomTransparency, 0,
			"**and the bottom transparency is 0, not a copy of the top**, because"
			+ " the reference only copies for one specific old-map case and a"
			+ " reader that always copied would apply that to every modern"
			+ $" command; it is {bild.BottomTransparency}");
	}

	/// <summary>
	/// The position mode's upper bits belong to the Maniac patch, not to the
	/// mode.
	/// </summary>
	/// <remarks>
	/// The reference masks with <c>ManiacBitmask(com.parameters[1], 0xFF)</c>
	/// because the patch packs the X and Y origin into the upper bits.
	/// <strong>A reader that took the whole number as the mode would read mode
	/// 257 where a game meant mode 1</strong> — and would then read a constant
	/// where a variable sits.
	/// </remarks>
	public void Test_TheManiacOriginBitsDoNotBecomeTheMode()
	{
		var state = new GameSimulationState
		{
			MapId = 1,
			Variables = [0, 111, 222],
		};
		var presentation = new PresentationState();
		// 0x0101 = 257: the low byte says "variable", the rest is the origin.
		var interpreter = new EventInterpreter(
			state, 1, [Show("Masked", pId: 6, pPosMode: 0x0101, pX: 2, pY: 3)], presentation);

		interpreter.ExecuteFrame();
		var bild = presentation.Pictures[6];

		AssertEq(
			bild.X, 111,
			"**and x came from variable 2**, because the low byte of the mode is"
			+ $" 1 and the origin bits above it are not the mode; x is {bild.X}");
		AssertEq(
			bild.Y, 222,
			"and y came from variable 3, for the same reason; y is {bild.Y}");
	}

	/// <summary>
	/// A command shorter than fourteen parameters is refused, not defaulted.
	/// </summary>
	/// <remarks>
	/// <c>CmdSetup</c> gives the command a minimum width of 14. A reader that
	/// defaulted the missing ones would show a picture the game never asked
	/// for, and a truncated file would turn into a grey rectangle instead of a
	/// diagnostic.
	/// </remarks>
	public void Test_ACommandShorterThanFourteenIsRefused()
	{
		var (interpreter, presentation, state) = Run(
			new Rm2kMap.EventCommand
			{
				Code = EventInterpreter.ShowPicture,
				Text = "Short",
				Parameters = [1, 0, 0, 0, 0, 100],
			});

		interpreter.ExecuteFrame();

		AssertEq(
			presentation.Pictures.Count, 0,
			"and no picture opened, because the command is truncated and not a"
			+ $" picture with defaults; there are {presentation.Pictures.Count}");
		AssertTrue(
			ContainsDiagnostic(state, "malformed"),
			"and the diagnostic says so, which is what a truncated file should"
			+ $" produce; the diagnostics are {state.Diagnostics.Count}");
	}

	/// <summary>
	/// Erase removes the picture and says there was one.
	/// </summary>
	public void Test_EraseRemovesThePictureAndSaysThereWasOne()
	{
		var (interpreter, presentation, state) = Run(
			Show("Gone", pId: 7), Erase(7));

		interpreter.ExecuteFrame();
		AssertEq(
			presentation.Pictures.Count, 1,
			"sanity: the picture is there before the erase runs");
		AssertEq(
			interpreter.CurrentCommandIndex, 1,
			"**and the index moved to the erase**, because a frame is one step"
			+ " and a first draft ran the erase in the same frame and then read"
			+ " a picture that was still there; the index is"
			+ $" {interpreter.CurrentCommandIndex}");

		interpreter.ExecuteFrame();

		AssertEq(
			presentation.Pictures.Count, 0,
			"and it is gone after the erase command ran;"
			+ $" there are {presentation.Pictures.Count}");
		AssertTrue(
			ContainsDiagnostic(state, "Erase picture 7"),
			"and the diagnostic names the picture, because a refusal with no"
			+ $" id in it cannot be acted on; the diagnostics are {state.Diagnostics.Count}");
	}

	/// <summary>
	/// Erasing a picture that is not there is success, and says so.
	/// </summary>
	/// <remarks>
	/// The reference returns true regardless. <strong>A reader that reported a
	/// refusal would make a game that legitimately erases twice look broken</strong>
	/// — and games do erase twice, because a page runs and then runs again.
	/// </remarks>
	public void Test_ErasingNothingIsSuccessAndSaysThereWasNone()
	{
		var (interpreter, presentation, state) = Run(Erase(9));

		interpreter.ExecuteFrame();

		AssertEq(
			presentation.Pictures.Count, 0,
			"and nothing was there and nothing is now;"
			+ $" there are {presentation.Pictures.Count}");
		AssertTrue(
			ContainsDiagnostic(state, "there was none"),
			"**and the diagnostic says there was none**, so the caller can tell"
			+ " that from a refusal; the reference's own comment on this command"
			+ $" is that it is not an error; the diagnostics are {state.Diagnostics.Count}");
	}

	/// <summary>
	/// A bound that is out of range is refused and says which picture.
	/// </summary>
	public void Test_AnOutOfRangePictureIdIsRefusedByNumber()
	{
		var (interpreter, presentation, state) = Run(Show("TooBig", pId: 500));

		interpreter.ExecuteFrame();

		AssertEq(
			presentation.Pictures.Count, 0,
			"and no picture opened, because 500 is past MaxPictures;"
			+ $" there are {presentation.Pictures.Count}");
		AssertTrue(
			ContainsDiagnostic(state, "500"),
			"and the diagnostic names the id that was refused, because"
			+ " \"refused\" without the number is a dead end for whoever reads"
			+ $" the log; the diagnostics are {state.Diagnostics.Count}");
	}

	/// <summary>
	/// A second show for the same id replaces the first.
	/// </summary>
	public void Test_ASecondShowForTheSameIdReplacesTheFirst()
	{
		var (interpreter, presentation, _) = Run(
			Show("First", pId: 3, pX: 1), Show("Second", pId: 3, pX: 2));

		interpreter.ExecuteFrame();
		interpreter.ExecuteFrame();

		AssertEq(
			presentation.Pictures.Count, 1,
			"and there is still one picture, because the id is the key;"
			+ $" there are {presentation.Pictures.Count}");
		AssertEq(
			presentation.Pictures[3].Name, "Second",
			"**and it is the second one**, because a reader that kept the first"
			+ $" would show a game two versions of its own picture; it is"
			+ $" \"{presentation.Pictures[3].Name}\"");
		AssertEq(
			presentation.Pictures[3].X, 2,
			"and it moved, because the second command said where; x is"
			+ $" {presentation.Pictures[3].X}");
	}

	/// <summary>
	/// A new game does not inherit a picture.
	/// </summary>
	public void Test_APictureDoesNotSurviveANewGame()
	{
		var (interpreter, presentation, state) = Run(Show("Stale", pId: 1));
		interpreter.ExecuteFrame();
		AssertEq(
			presentation.Pictures.Count, 1, "sanity: the picture is there");

		presentation.Reset();
		state.Reset();

		AssertEq(
			presentation.Pictures.Count, 0,
			"**and a new game has no pictures**, because a game that began with"
			+ $" the last one's logo on screen is a game with a bug; there are"
			+ $" {presentation.Pictures.Count}");
	}

	private static bool ContainsDiagnostic(
		GameSimulationState pState, string pNeedle)
	{
		foreach (var line in pState.Diagnostics)
		{
			if (line.Contains(pNeedle, System.StringComparison.Ordinal))
			{
				return true;
			}
		}
		return false;
	}
}
