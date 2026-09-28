using System;
using System.Collections.Generic;

namespace UniversalRPG.Rm2k.Interpreter;

/// <summary>
/// Command 11610, "Key Input Proc", from EasyRPG
/// <c>Game_Interpreter::CommandKeyInputProc</c>.
/// </summary>
/// <remarks>
/// <para>
/// This is the one command in the two pinned RM2K fixtures that this
/// repository had named "unidentified" for several cards, because guessing a
/// command code is how a reader ends up confident and wrong. It is now read
/// from the reference implementation: 11610 is
/// <c>CommandKeyInputProc</c>, and the one occurrence in these fixtures is
/// <c>[1, 1, 0, 0, 0, 1, 1, 2, 1, 0, 0, 0, 0, 0]</c>.
/// </para>
/// <para>
/// <strong>The command waits for a key and writes a number into a
/// variable.</strong> The number is not the key itself and not a scancode: it
/// is a fixed table, checked <em>highest value first</em>, so a game that
/// allows both digits and operators gets the operator when both are pressed in
/// the same frame. That ordering is the command's whole reason to exist and
/// is invisible unless it is written down.
/// </para>
/// <para>
/// <strong>What the fixture's parameters mean</strong>, against
/// <c>CommandKeyInputProc</c> and its <c>param_size</c> branches:
/// </para>
/// <list type="bullet">
/// <item><description>[0] the variable that receives the value.</description></item>
/// <item><description>[1] whether to wait. Zero sets the variable once and
/// carries on; nonzero holds the page until a key arrives.</description></item>
/// <item><description>[2] the legacy all-directions switch, used only by
/// RM2K before 1.50 and RM2K3 before 1.05. It is <c>0</c> here.</description></item>
/// <item><description>[3] decision, [4] cancel.</description></item>
/// <item><description>[5]–[9] <em>mean different keys on 2K and 2K3</em>:
/// shift/down/left/right/up on 2K from 1.50, and numbers/operators/time
/// variable/timed on 2K3. <strong>One number, two meanings, chosen by the
/// engine version</strong>, and a reader that picks the wrong column produces a
/// plausible wrong answer rather than an error.</description></item>
/// <item><description>[10]–[13] the Maniac patch's per-key bitmask, which is
/// why this command carries fourteen parameters and not five.</description></item>
/// </list>
/// <para>
/// <strong>And <c>[7] is an int, not a bool</c> — it is the time variable's
/// number, and the comment in the reference says so in as many words. A reader
/// that read it as a flag would write the elapsed time into variable 1
/// instead of variable 2.
/// </para>
/// </remarks>
public static class Rm2kKeyInput
{
	// KeyInputState::CheckInput's return values, in the order it tests them.
	// The order is the semantics: RPG processes keys from the highest variable
	// value to the lowest, and a game that allows two keys in one frame gets
	// the larger number.
	public const int ValueNone = 0;
	public const int ValueDown = 1;
	public const int ValueLeft = 2;
	public const int ValueRight = 3;
	public const int ValueUp = 4;
	public const int ValueDecision = 5;
	public const int ValueCancel = 6;
	public const int ValueShift = 7;

	/// <summary>
	/// The lowest digit value, from <c>10 + i</c> over <c>N0 + i</c>. A digit
	/// returns 11 for 1, 20 for 0 — <strong>not</strong> 0 through 9.
	/// </summary>
	public const int DigitBase = 10;

	/// <summary>The lowest operator value, from <c>20 + i</c> over <c>PLUS + i</c>.</summary>
	public const int OperatorBase = 20;

	public const int ValueMouseScrollDown = 1001;
	public const int ValueMouseScrollUp = 1004;
	public const int ValueMouseLeft = 1005;
	public const int ValueMouseRight = 1006;
	public const int ValueMouseMiddle = 1007;

	/// <summary>
	/// What one 11610 asks for, decoded from its parameters.
	/// </summary>
	/// <remarks>
	/// This is a decode, not an execution: the engine never runs this as code
	/// and neither does this reader. It is the set of keys a game allowed and
	/// the variable the answer goes into, so a caller can show the prompt and
	/// compare the answer without a window existing yet.
	/// </remarks>
	public sealed record Request
	{
		/// <summary>The variable that receives the value, from <c>parameters[0]</c>.</summary>
		public int VariableId { get; init; }

		/// <summary>
		/// Whether the command holds the page, from <c>parameters[1] != 0</c>.
		/// </summary>
		public bool Wait { get; init; }

		/// <summary>
		/// The variable the elapsed tenths go into while waiting, or 0.
		/// </summary>
		/// <remarks>
		/// From <c>parameters[7]</c> on RM2K3, and only when the command is
		/// timed. <strong>It is an int, not a bool</strong>, so a value of 2
		/// means variable 2 and a reader that read it as a flag would write
		/// the time into variable 1.
		/// </remarks>
		public int TimeVariableId { get; init; }

		/// <summary>Whether the elapsed time is reported, from <c>parameters[8]</c>.</summary>
		public bool Timed { get; init; }

		/// <summary>Whether digits are accepted, from <c>parameters[5]</c> on RM2K3.</summary>
		public bool Numbers { get; init; }

		/// <summary>Whether operators are accepted, from <c>parameters[6]</c> on RM2K3.</summary>
		public bool Operators { get; init; }

		/// <summary>Whether the confirm key is accepted, from <c>parameters[3]</c>.</summary>
		public bool Decision { get; init; }

		/// <summary>Whether the cancel key is accepted, from <c>parameters[4]</c>.</summary>
		public bool Cancel { get; init; }

		/// <summary>Whether up is accepted.</summary>
		public bool Up { get; init; }

		/// <summary>Whether right is accepted.</summary>
		public bool Right { get; init; }

		/// <summary>Whether left is accepted.</summary>
		public bool Left { get; init; }

		/// <summary>Whether down is accepted.</summary>
		public bool Down { get; init; }

		/// <summary>Whether shift is accepted.</summary>
		public bool Shift { get; init; }

		/// <summary>
		/// Whether the all-directions legacy switch is on, from
		/// <c>parameters[2]</c>. Read only by RM2K before 1.50 and RM2K3
		/// before 1.05, and kept because a modern editor can still write it.
		/// </summary>
		public bool LegacyAllDirections { get; init; }

		/// <summary>
		/// How many parameters the command carried, which is what selects the
		/// branch. A five parameter command is the pre-1.50 form; fourteen is
		/// the Maniac form this repository's fixture uses.
		/// </summary>
		public int ParameterCount { get; init; }

		/// <summary>Whether this is the fourteen parameter Maniac form.</summary>
		public bool IsManiacForm => ParameterCount > 10;

		/// <summary>
		/// The keys this request accepts, in the order
		/// <c>CheckInput</c> tests them. <strong>The order is the answer</strong>:
		/// the first accepted key pressed wins, and RPG walks from the highest
		/// value down.
		/// </summary>
		public IReadOnlyList<(int Value, string Key)> AllowedKeys { get; init; } = [];
	}

	/// <summary>
	/// Reads one 11610 into the set of keys it accepts, for the engine version
	/// the game declares.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>The version decides the column, and this is the sharp edge.</strong>
	/// Parameters 5 to 9 are shift/down/left/right/up on RM2K and
	/// numbers/operators/time variable/timed on RM2K3. A reader that reads one
	/// column for both gets a command that looks right and waits for the wrong
	/// keys — and on the fixture, which is RM2K3, it would wait for a shift
	/// key where the game asked for digits.
	/// </para>
	/// <para>
	/// The parameter <em>count</em> also decides the branch, independently of
	/// the version: a short list is the legacy form and means "any direction
	/// key" in <c>parameters[2]</c>. Both branches are kept because a game
	/// written in 2002 and opened in 2003 can carry either.
	/// </para>
	/// </remarks>
	/// <param name="pParameters">The command's parameters, in file order.</param>
	/// <param name="pIsRpg2k3">
	/// Whether the game is RM2K/2003. This is read from the file and not
	/// assumed, because the parameter meanings differ.
	/// </param>
	/// <param name="pIsMajorUpdated">
	/// Whether the game is RM2K3 1.05 or later, which is the version that
	/// gives the directions their own parameters.
	/// </param>
	public static Request Read(
		IReadOnlyList<int> pParameters, bool pIsRpg2k3, bool pIsMajorUpdated = false)
	{
		if (pParameters == null || pParameters.Count < 2)
		{
			return null;
		}

		var request = new Request
		{
			VariableId = pParameters[0],
			Wait = pParameters[1] != 0,
			ParameterCount = pParameters.Count,
			LegacyAllDirections = pParameters.Count > 2 && pParameters[2] != 0,
			Decision = pParameters.Count > 3 && pParameters[3] != 0,
			Cancel = pParameters.Count > 4 && pParameters[4] != 0,
		};

		var decision = request.Decision ? ValueDecision : -1;
		var cancel = request.Cancel ? ValueCancel : -1;

		if (!pIsRpg2k3)
		{
			// RM2K: [5] shift, [6] down, [7] left, [8] right, [9] up, and
			// before 1.50 one switch turned all four directions on at once.
			var shiftOn2k = pParameters.Count > 5 && pParameters[5] != 0;
			var directionsOn = pIsMajorUpdated
				? new[]
				{
					pParameters.Count > 9 && pParameters[9] != 0 ? ValueUp : -1,
					pParameters.Count > 8 && pParameters[8] != 0 ? ValueRight : -1,
					pParameters.Count > 7 && pParameters[7] != 0 ? ValueLeft : -1,
					pParameters.Count > 6 && pParameters[6] != 0 ? ValueDown : -1,
				}
				: (request.LegacyAllDirections
					? new[] { ValueUp, ValueRight, ValueLeft, ValueDown }
					: new[] { -1, -1, -1, -1 });

			var upOn = Array.IndexOf(directionsOn, ValueUp) >= 0;
			var rightOn = Array.IndexOf(directionsOn, ValueRight) >= 0;
			var leftOn = Array.IndexOf(directionsOn, ValueLeft) >= 0;
			var downOn = Array.IndexOf(directionsOn, ValueDown) >= 0;

			return request with
			{
				Shift = shiftOn2k,
				Up = upOn,
				Right = rightOn,
				Left = leftOn,
				Down = downOn,
				// **A 2K game has no digit group and no operator group** — those
				// two parameters mean shift and down on this engine — so the
				// two flags are off rather than read out of the wrong column.
				Numbers = false,
				Operators = false,
				AllowedKeys = BuildKeys(
					MouseChecks(),
					pNumbers: false,
					pOperators: false,
					pShift: shiftOn2k ? ValueShift : -1,
					pUp: upOn ? ValueUp : -1,
					pRight: rightOn ? ValueRight : -1,
					pLeft: leftOn ? ValueLeft : -1,
					pDown: downOn ? ValueDown : -1,
					pDecision: decision,
					pCancel: cancel),
			};
		}

		// RM2K3: [5] numbers, [6] operators, [7] time variable, [8] timed,
		// and from 1.05 [9]..[13] are the Maniac per-key bitmask.
		var numbers = pParameters.Count > 5 && pParameters[5] != 0;
		var operators = pParameters.Count > 6 && pParameters[6] != 0;
		var timeVariable = pParameters.Count > 7 ? pParameters[7] : 0;
		var timed = pParameters.Count > 8 && pParameters[8] != 0;

		// On 2K3 before 1.05 the four directions share [9]; from 1.05 each has
		// its own slot and the Maniac patch packs them as bitmasks.
		var shiftSlot = -1;
		var up = -1;
		var right = -1;
		var left = -1;
		var down = -1;
		var shift = -1;
		if (!pIsMajorUpdated)
		{
			if (pParameters.Count <= 9 || pParameters[9] != 0)
			{
				up = ValueUp;
				right = ValueRight;
				left = ValueLeft;
				down = ValueDown;
			}
		}
		else
		{
			shiftSlot = BitSet(pParameters, 9);
			down = BitSet(pParameters, 10);
			left = BitSet(pParameters, 11);
			right = BitSet(pParameters, 12);
			up = BitSet(pParameters, 13);
		}

		return request with
		{
			Numbers = numbers,
			Operators = operators,
			TimeVariableId = timed ? timeVariable : 0,
			Timed = timed,
			Shift = shiftSlot >= 0,
			Up = up >= 0,
			Right = right >= 0,
			Left = left >= 0,
			Down = down >= 0,
			AllowedKeys = BuildKeys(
				MouseChecks(),
				numbers,
				operators,
				shiftSlot,
				up,
				right,
				left,
				down,
				decision,
				cancel),
		};
	}

	/// <summary>
	/// Reads a Maniac bitmask slot: nonzero is on, and the low bit is what the
	/// patch tests when the engine maps one key to two.
	/// </summary>
	private static int BitSet(IReadOnlyList<int> pParameters, int pIndex)
	{
		return pParameters.Count > pIndex && (pParameters[pIndex] & 1) != 0
			? ValueNone - 1
			: -1;
	}

	private static (int Value, string Key)[] MouseChecks()
	{
		return
		[
			(ValueMouseScrollDown, "scroll down"),
			(ValueMouseScrollUp, "scroll up"),
			(ValueMouseMiddle, "middle mouse"),
			(ValueMouseRight, "right mouse"),
			(ValueMouseLeft, "left mouse"),
		];
	}

	/// <summary>
	/// The accepted keys in the order <c>CheckInput</c> tests them.
	/// </summary>
	/// <remarks>
	/// <strong>Highest value first, and the mouse before the keys</strong> —
	/// the reference says so and explains why: the mouse is checked first to
	/// stop a conflict when the confirm key is mapped to the left mouse button.
	/// A reader that ordered this list differently would report a different
	/// answer for a frame in which two inputs arrived together.
	/// </remarks>
	/// <param name="pNumbers">Whether digits are accepted; -1 for a game that has no digit group.</param>
	/// <param name="pOperators">Whether operators are accepted.</param>
	private static (int Value, string Key)[] BuildKeys(
		(int Value, string Key)[] pMouse,
		bool pNumbers,
		bool pOperators,
		int pShift,
		int pUp,
		int pRight,
		int pLeft,
		int pDown,
		int pDecision,
		int pCancel)
	{
		var keys = new List<(int, string)>(pMouse);
		if (pOperators)
		{
			// The reference walks i from 5 down to 1 and returns 20 + i, so the
			// order inside the group is 25, 24, 23, 22, 21.
			for (var i = 5; i >= 1; i--)
			{
				keys.Add((OperatorBase + i, OperatorName(i)));
			}
		}
		if (pNumbers)
		{
			// i from 10 down to 1, returning 10 + i. **0 is not included**: the
			// reference starts at 10, so the digit zero has no value here and a
			// reader that returned 10 for it would answer a question the game
			// never asked.
			for (var i = 10; i >= 1; i--)
			{
				keys.Add((DigitBase + i, i.ToString()));
			}
		}
		if (pShift >= 0)
		{
			keys.Add((ValueShift, "shift"));
		}
		if (pCancel >= 0)
		{
			keys.Add((ValueCancel, "cancel"));
		}
		if (pDecision >= 0)
		{
			keys.Add((ValueDecision, "decision"));
		}
		if (pUp >= 0)
		{
			keys.Add((ValueUp, "up"));
		}
		if (pRight >= 0)
		{
			keys.Add((ValueRight, "right"));
		}
		if (pLeft >= 0)
		{
			keys.Add((ValueLeft, "left"));
		}
		if (pDown >= 0)
		{
			keys.Add((ValueDown, "down"));
		}
		return keys.ToArray();
	}

	/// <summary>
	/// The operator names in the reference's order, from <c>Input::PLUS + i</c>.
	/// </summary>
	/// <remarks>
	/// <c>Input::PLUS</c> is followed by MINUS, TIMES, DIVIDE and MOD in
	/// liblcf's <c>InputButton</c> enumeration, so i = 5..1 is
	/// PLUS, MINUS, TIMES, DIVIDE, MOD.
	/// </remarks>
	private static string OperatorName(int pIndex) => pIndex switch
	{
		5 => "plus",
		4 => "minus",
		3 => "times",
		2 => "divide",
		1 => "mod",
		_ => $"operator {pIndex}",
	};

	/// <summary>
	/// The value one key press produces, in the reference's order, or 0 when
	/// nothing this request allows was pressed.
	/// </summary>
	/// <param name="pRequest">What the command allows.</param>
	/// <param name="pPressed">
	/// The keys pressed this frame, by the same names
	/// <see cref="Request.AllowedKeys"/> uses.
	/// </param>
	public static int ValueFor(
		Request pRequest, IReadOnlyList<string> pPressed)
	{
		if (pRequest == null || pPressed == null)
		{
			return ValueNone;
		}
		foreach (var (value, key) in pRequest.AllowedKeys)
		{
			foreach (var pressed in pPressed)
			{
				if (string.Equals(key, pressed, StringComparison.Ordinal))
				{
					return value;
				}
			}
		}
		return ValueNone;
	}
}
