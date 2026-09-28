namespace UniversalRPG.Wolf;

/// <summary>
/// The seven comparisons WOLF's variable branch offers, from
/// <c>silversecond.com/WolfRPGEditor/Help/04ev_ifvalue.html</c>.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Seven and not one.</strong> The WOLF help lists exactly seven:
/// greater, greater or equal, equal, less or equal, less, not equal, and bit
/// and. A reader that implemented only <c>==</c> would take one branch in seven
/// — <em>and every one of the six it did not implement would fall through to
/// the else branch</em>, so a game whose treasure chest is guarded by "V0 is
/// at least 1" would never open it.
/// </para>
/// <para>
/// <strong>The numbers here are the ones the editor writes</strong>, and they
/// are the order the help lists them in. The binary format is not public
/// documentation and this reader has no native WOLF fixture, so the numbering
/// is a model of the editor's choice list, not a claim about a file on disk.
/// Anything read from a file keeps its number in a diagnostic.
/// </para>
/// </remarks>
public enum WolfComparison
{
	/// <summary>Not a comparison the editor offers; refused.</summary>
	Unknown = -1,

	/// <summary>より大きい — the variable is greater than the value.</summary>
	Greater = 0,

	/// <summary>以上 — the variable is greater than or equal to the value.</summary>
	GreaterOrEqual = 1,

	/// <summary>と同じ — the variable is equal to the value.</summary>
	Equal = 2,

	/// <summary>以下 — the variable is less than or equal to the value.</summary>
	LessOrEqual = 3,

	/// <summary>未満 — the variable is less than the value.</summary>
	Less = 4,

	/// <summary>以外 — the variable is anything but the value.</summary>
	NotEqual = 5,

	/// <summary>とのビット積 — the bit and of variable and value equals the value.</summary>
	BitAnd = 6,
}

/// <summary>
/// Evaluates WOLF's seven variable comparisons.
/// </summary>
/// <remarks>
/// <para>
/// <strong>The bit-and test is the one that surprises people</strong>, and the
/// help spends a paragraph on it: the result of <c>variable &amp; value</c> has
/// to <em>equal</em> the value, which is not the same as "the variable contains
/// those bits". With V0 = 5 (<c>101</c>) and value 1 (<c>001</c>), the test
/// fails — the variable cannot cover the second digit. A reader that wrote
/// <c>(variable &amp; value) != 0</c> would pass every test with any bit set,
/// and a game that guards a door with a bit test would open it for everyone.
/// </para>
/// <para>
/// <strong>A value of zero satisfies the bit test in every case</strong>, because
/// anything bit anded with zero is zero. The help says so outright, and a game
/// that uses a zero comparison value has written a condition that is always
/// true.
/// </para>
/// </remarks>
public static class WolfComparisonEvaluator
{
	/// <summary>The highest comparison the editor offers.</summary>
	public const int MaxComparison = 6;

	/// <summary>
	/// Evaluates one comparison, from the editor's list.
	/// </summary>
	/// <param name="pComparison">One of the seven numbers.</param>
	/// <param name="pLeft">The variable's value.</param>
	/// <param name="pRight">The value it is compared against.</param>
	/// <param name="pResult">The result, valid only when the method returns true.</param>
	/// <returns>False when the number is not one of the seven.</returns>
	public static bool TryEvaluate(
		int pComparison, int pLeft, int pRight, out bool pResult)
	{
		pResult = false;
		switch (pComparison)
		{
			case (int)WolfComparison.Greater:
				pResult = pLeft > pRight;
				return true;
			case (int)WolfComparison.GreaterOrEqual:
				pResult = pLeft >= pRight;
				return true;
			case (int)WolfComparison.Equal:
				pResult = pLeft == pRight;
				return true;
			case (int)WolfComparison.LessOrEqual:
				pResult = pLeft <= pRight;
				return true;
			case (int)WolfComparison.Less:
				pResult = pLeft < pRight;
				return true;
			case (int)WolfComparison.NotEqual:
				pResult = pLeft != pRight;
				return true;
			case (int)WolfComparison.BitAnd:
				// **Equal to the value and not "any bit set".** With V0 = 5 and
				// value 1 this is false, and that is the case the help explains.
				pResult = (pLeft & pRight) == pRight;
				return true;
			default:
				return false;
		}
	}
}
