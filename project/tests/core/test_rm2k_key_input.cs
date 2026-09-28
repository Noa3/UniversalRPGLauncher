using System.Collections.Generic;

using UniversalRPG.Rm2k.Interpreter;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Command 11610, "Key Input Proc", read from EasyRPG's
/// <c>Game_Interpreter::CommandKeyInputProc</c>.
/// </summary>
/// <remarks>
/// <para>
/// This is the command K-135 recorded as "unidentified" for several cards,
/// because two occurrences in the pinned fixtures were not enough to name it
/// and a guess would have been a claim without evidence. It is named here from
/// the reference implementation, and the one fixture occurrence is
/// <c>[1, 1, 0, 0, 0, 1, 1, 2, 1, 0, 0, 0, 0, 0]</c> — measured out of
/// <c>rm2k-dragon-destiny/Map0001.lmu</c>.
/// </para>
/// <para>
/// <strong>The expectations here are the reference's own numbers</strong>, not
/// values that make the test pass: the key table, the parameter columns, the
/// digit offset, the parameter count that selects the branch.
/// </para>
/// </remarks>
public partial class TestRm2kKeyInput : TestBase
{
	/// <summary>The one occurrence in the pinned fixtures, in file order.</summary>
	private static readonly List<int> Fixture =
		[1, 1, 0, 0, 0, 1, 1, 2, 1, 0, 0, 0, 0, 0];

	/// <summary>
	/// The command waits for a key and writes a number into a variable.
	/// </summary>
	public void Test_TheFixtureWaitsForAKeyAndWritesIntoVariableOne()
	{
		var request = Rm2kKeyInput.Read(Fixture, pIsRpg2k3: true, pIsMajorUpdated: true);

		AssertEq(
			request.VariableId, 1,
			"and the value goes into variable 1, because parameters[0] is the"
			+ $" variable id; it is {request.VariableId}");
		AssertEq(
			request.Wait, true,
			"and the page is held, because parameters[1] is nonzero and the"
			+ $" command waits when it is; Wait is {request.Wait}");
	}

	/// <summary>
	/// Parameters 5 to 9 mean different keys on 2K and 2K3, and the version
	/// picks the column. This fixture is 2K3, so it asks for digits and
	/// operators — not for a shift key.
	/// </summary>
	public void Test_ThisFixtureAsksForDigitsAndOperatorsAndNotForShift()
	{
		var request = Rm2kKeyInput.Read(Fixture, pIsRpg2k3: true, pIsMajorUpdated: true);

		AssertEq(
			request.Numbers, true,
			"and digits are accepted, because on 2K3 parameters[5] is the"
			+ $" numbers flag and it is 1; Numbers is {request.Numbers}");
		AssertEq(
			request.Operators, true,
			"and operators are accepted, because parameters[6] is the"
			+ $" operators flag and it is 1; Operators is {request.Operators}");
		AssertEq(
			request.Shift, false,
			"and shift is not accepted, because on 2K3 the Maniac slot for it"
			+ $" is parameters[9] and that is 0; Shift is {request.Shift}");
		AssertEq(
			request.Decision, false,
			"and the confirm key is not accepted, because parameters[3] is 0;"
			+ $" Decision is {request.Decision}");
		AssertEq(
			request.Cancel, false,
			"and the cancel key is not accepted, because parameters[4] is 0;"
			+ $" Cancel is {request.Cancel}");
	}

	/// <summary>
	/// Parameters[7] is the time variable and <strong>an int, not a bool</strong>.
	/// The reference says so in a comment in as many words.
	/// </summary>
	public void Test_ParameterSevenIsTheTimeVariableAndNotAFlag()
	{
		var request = Rm2kKeyInput.Read(Fixture, pIsRpg2k3: true, pIsMajorUpdated: true);

		AssertEq(
			request.Timed, true,
			"and the command is timed, because parameters[8] is the timed flag"
			+ $" and it is 1; Timed is {request.Timed}");
		AssertEq(
			request.TimeVariableId, 2,
			"and the elapsed tenths go into variable 2, because parameters[7] is"
			+ " an int naming the variable — a reader that read it as a flag would"
			+ $" write the time into variable 1; it is {request.TimeVariableId}");
	}

	/// <summary>
	/// Reading the 2K column of parameters 5 to 9 instead of the 2K3 one
	/// produces a command that waits for the wrong keys.
	/// </summary>
	public void Test_ReadingTheWrongColumnWaitsForDifferentKeys()
	{
		var on2k3 = Rm2kKeyInput.Read(Fixture, pIsRpg2k3: true, pIsMajorUpdated: true);
		var as2k = Rm2kKeyInput.Read(Fixture, pIsRpg2k3: false, pIsMajorUpdated: true);

		AssertEq(
			as2k.Shift, true,
			"and read as a 2K command, the same bytes mean the shift key is"
			+ " wanted, because parameters[5] is shift on that engine and it is 1;"
			+ $" Shift is {as2k.Shift}");
		AssertEq(
			as2k.Numbers, false,
			"and no digit is wanted at all, because a 2K game has no digit"
			+ $" group in this command; Numbers is {as2k.Numbers}");
		AssertTrue(
			as2k.Shift != on2k3.Shift,
			"and the two readings are not the same command, which is the whole"
			+ " reason the version is a parameter of this reader and not an"
			+ " assumption: the 2K3 one wants digits, the 2K one wants shift");
	}

	/// <summary>
	/// A digit is <c>10 + i</c>, so 1 is 11 and 0 has no value at all.
	/// </summary>
	public void Test_ADigitIsTenPlusItsNumberAndZeroIsNotOne()
	{
		var request = Rm2kKeyInput.Read(Fixture, pIsRpg2k3: true, pIsMajorUpdated: true);

		AssertEq(
			Rm2kKeyInput.ValueFor(request, ["1"]), 11,
			"and pressing the digit 1 answers 11, because the reference walks"
			+ " i from 10 down to 1 and returns 10 + i");
		AssertEq(
			Rm2kKeyInput.ValueFor(request, ["9"]), 19,
			"and pressing 9 answers 19; it is"
			+ $" {Rm2kKeyInput.ValueFor(request, ["9"])}");
		AssertEq(
			Rm2kKeyInput.ValueFor(request, ["0"]), 0,
			"**and pressing 0 answers nothing**, because the loop starts at 10"
			+ " and the digit zero is not in it — a reader that returned 10 for"
			+ " it would answer a question the game never asked; it is"
			+ $" {Rm2kKeyInput.ValueFor(request, ["0"])}");
	}

	/// <summary>
	/// Keys are processed from the highest value down, so an operator beats a
	/// digit pressed in the same frame.
	/// </summary>
	public void Test_AnOperatorBeatsADigitPressedInTheSameFrame()
	{
		var request = Rm2kKeyInput.Read(Fixture, pIsRpg2k3: true, pIsMajorUpdated: true);

		// The reference checks the operator group first and returns 20 + i, so
		// "plus" is 25 and it wins over any digit.
		AssertEq(
			Rm2kKeyInput.ValueFor(request, ["plus"]), 25,
			"and pressing plus answers 25, because the operator group is walked"
			+ " before the digit group; it is"
			+ $" {Rm2kKeyInput.ValueFor(request, ["plus"])}");
		AssertEq(
			Rm2kKeyInput.ValueFor(request, ["3", "plus"]), 25,
			"and pressing both answers 25, because RPG processes keys from the"
			+ " highest variable value to the lowest and the order is the"
			+ " command's whole point; it is"
			+ $" {Rm2kKeyInput.ValueFor(request, ["3", "plus"])}");
		AssertEq(
			Rm2kKeyInput.ValueFor(request, ["plus", "3"]), 25,
			"and the order the two were pressed in does not change it, which is"
			+ " what makes the table an order rather than a set; it is"
			+ $" {Rm2kKeyInput.ValueFor(request, ["plus", "3"])}");
	}

	/// <summary>
	/// The mouse is checked before the keys, so a confirm key mapped to the
	/// left mouse button does not conflict.
	/// </summary>
	public void Test_TheMouseIsCheckedBeforeTheKeys()
	{
		var request = new Rm2kKeyInput.Request
		{
			VariableId = 1,
			Decision = true,
			AllowedKeys =
			[
				(Rm2kKeyInput.ValueMouseLeft, "left mouse"),
				(Rm2kKeyInput.ValueDecision, "decision"),
			],
		};

		AssertEq(
			Rm2kKeyInput.ValueFor(request, ["left mouse"]), Rm2kKeyInput.ValueMouseLeft,
			"and the left mouse answers 1005, because CheckInput tests the mouse"
			+ " first specifically to avoid a conflict when decision is mapped to"
			+ $" it; it is {Rm2kKeyInput.ValueFor(request, ["left mouse"])}");
		AssertEq(
			Rm2kKeyInput.ValueFor(request, ["decision"]), Rm2kKeyInput.ValueDecision,
			"and the confirm key answers 5, which is the reference's own value"
			+ $" for it; it is {Rm2kKeyInput.ValueFor(request, ["decision"])}");
		AssertEq(
			Rm2kKeyInput.ValueFor(request, ["shift"]), 0,
			"**and a key this request does not allow answers nothing**, because"
			+ " a command that was not asked for a shift key must not accept one;"
			+ $" it is {Rm2kKeyInput.ValueFor(request, ["shift"])}");
	}

	/// <summary>
	/// A short parameter list is the legacy form, and parameters[2] then turns
	/// on all four directions at once.
	/// </summary>
	public void Test_AShortParameterListIsTheLegacyForm()
	{
		// The pre-1.05 shape: variable, wait, all-directions, decision, cancel.
		var legacy = new List<int> { 3, 1, 1, 0, 0 };
		var request = Rm2kKeyInput.Read(legacy, pIsRpg2k3: true, pIsMajorUpdated: false);

		AssertEq(
			request.ParameterCount, 5,
			"and the command carried five parameters, which is what makes it the"
			+ $" legacy form; it is {request.ParameterCount}");
		AssertEq(
			request.IsManiacForm, false,
			"and it is not the fourteen parameter form, because the Maniac patch"
			+ $" is a later version's addition; it is {request.IsManiacForm}");
		AssertEq(
			request.LegacyAllDirections, true,
			"and parameters[2] turned all four directions on, which is the whole"
			+ $" meaning of that slot before 1.05; it is {request.LegacyAllDirections}");
		AssertEq(
			request.Up, true,
			"and so up is accepted; Up is"
			+ $" {request.Up}");
		AssertEq(
			request.Down, true,
			"and so is down, because the flag is one switch and not four;"
			+ $" Down is {request.Down}");
		AssertEq(
			request.Up && request.Down && request.Left && request.Right, true,
			"and all four are on, because a reader that read the flag as only up"
			+ " would leave a player who presses down standing still forever");
	}

	/// <summary>
	/// A command that is not waiting sets its variable once and carries on.
	/// </summary>
	public void Test_ACommandThatDoesNotWaitAsksForNoKeyAtAll()
	{
		var request = Rm2kKeyInput.Read(
			new List<int> { 5, 0, 0, 0, 0, 0, 0, 0, 0 }, pIsRpg2k3: true);

		AssertEq(
			request.Wait, false,
			"and the page is not held, because parameters[1] is 0; Wait is"
			+ $" {request.Wait}");
		AssertEq(
			request.VariableId, 5,
			"and it still names variable 5, because that is what it writes when"
			+ $" it does not wait; it is {request.VariableId}");
	}

	/// <summary>
	/// A command with too few parameters to read is refused rather than
	/// guessed at.
	/// </summary>
	public void Test_ACommandWithTooFewParametersIsRefused()
	{
		AssertEq(
			Rm2kKeyInput.Read(new List<int> { 1 }, pIsRpg2k3: true), null,
			"and a one parameter command is refused, because a reader that"
			+ " invented a variable id would write the player's keypress into a"
			+ " variable the game never named");
		AssertEq(
			Rm2kKeyInput.Read(new List<int>(), pIsRpg2k3: true), null,
			"and so is an empty one");
		AssertEq(
			Rm2kKeyInput.Read(null, pIsRpg2k3: true), null,
			"and so is a null parameter list, which is a truncated file and not"
			+ " a command");
	}
}
