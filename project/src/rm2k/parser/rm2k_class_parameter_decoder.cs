using System;
using System.Collections.Generic;

namespace UniversalRPG.Rm2k.Parser;

/// <summary>
/// The LDB class parameter chunk, <c>0x1F</c>, decoded into six level vectors.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Six <c>int16</c> vectors and not six scalars.</strong> liblcf
/// <c>rpg::Parameters</c> holds <c>maxhp</c>, <c>maxsp</c>, <c>attack</c>,
/// <c>defense</c>, <c>spirit</c> and <c>agility</c> as
/// <c>std::vector&lt;int16_t&gt;</c>, one entry per level, and its
/// <c>WriteLcf</c> stores them in exactly that order with no lengths in front.
/// A reader that assumed six scalars would read the first value of each vector
/// and call it the class maximum — <em>a level 99 class would give its heroes
/// level 1 stats</em>.
/// </para>
/// <para>
/// <strong>The vectors are the same length or the chunk is malformed.</strong>
/// <c>Parameters::Setup</c> sizes them all to the final level, so a chunk whose
/// vectors disagree is not a game that made a choice, it is a file that was cut
/// short. That is refused, and the reason is said.
/// </para>
/// <para>
/// <strong>Nothing is thrown away.</strong> The raw chunk stays in the entry's
/// <c>unknown_fields</c>, so the bytes a game shipped are still reachable — this
/// decode adds a reading of them and does not replace one.
/// </para>
/// </remarks>
public static class Rm2kClassParameterDecoder
{
	/// <summary>
	/// Reads the six level vectors out of a <c>0x1F</c> chunk.
	/// </summary>
	/// <param name="pData">The chunk bytes, header already stripped.</param>
	/// <param name="pResult">
	/// On success, a dictionary with the six names from
	/// <see cref="Rm2kParser.LdbClassParameterNames"/>, each a list of level
	/// values.
	/// </param>
	/// <param name="pError">The reason on failure, or an empty string.</param>
	/// <returns>False when the chunk is not six equal-length vectors.</returns>
	public static bool TryDecode(
		byte[] pData, out Dictionary<string, object> pResult, out string pError)
	{
		pResult = new Dictionary<string, object>();
		pError = "";
		// **Six vectors of int16 is twelve bytes each, so the chunk is a
		// multiple of twelve.** Anything else is a different structure and not
		// something to guess at.
		if (pData.Length % 12 != 0)
		{
			pError = $"class parameter chunk is {pData.Length} bytes, which is"
				+ " not a multiple of 12 (six int16 vectors)";
			return false;
		}
		var count = pData.Length / 12;
		if (count > Rm2kParser.MaxClassParameterLevels)
		{
			pError = $"class parameter chunk holds {count} levels, and the"
				+ $" format stops at {Rm2kParser.MaxClassParameterLevels}";
			return false;
		}
		for (var vector = 0; vector < Rm2kParser.LdbClassParameterVectorCount; vector++)
		{
			var values = new List<int>(count);
			for (var level = 0; level < count; level++)
			{
				// **Little endian, as the whole LCF format is.** The high byte
				// first would make every stat of every class wrong by a factor
				// of 256 on the low end.
				var at = ((vector * count) + level) * 2;
				values.Add((short)((pData[at] & 0xff) | (pData[at + 1] << 8)));
			}
			pResult[Rm2kParser.LdbClassParameterNames[vector]] = values;
		}
		return true;
	}
}
