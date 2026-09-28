using System;
using System.Collections.Generic;

using UniversalRPG.Rm2k.Database;
using UniversalRPG.Rm2k.Parser;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The LDB class parameter chunk, <c>0x1F</c>, and the class model's use of it.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Six <c>int16</c> vectors and not six scalars.</strong> liblcf
/// <c>rpg::Parameters</c> holds one vector per stat with one entry per level, and
/// its <c>WriteLcf</c> stores them in that order with no lengths in front. A
/// reader that assumed six scalars would read the first value of each vector —
/// <em>a level 99 class would give its heroes level 1 stats</em>.
/// </para>
/// <para>
/// Before this slice the chunk was read into <c>unknown_fields</c> and stayed
/// there: nothing was lost, but <c>1008 ChangeClass</c> had no way in.
/// </para>
/// </remarks>
public partial class TestRm2kClassParameters : TestBase
{
	/// <summary>
	/// A chunk of six vectors over <paramref name="pLevels"/> levels, written
	/// the way liblcf writes it.
	/// </summary>
	/// <param name="pLevels">How many levels the class has.</param>
	/// <param name="pBase">The value at level 1 of every stat.</param>
	/// <param name="pStep">How much each level adds.</param>
	private static byte[] Chunk(int pLevels, int pBase, int pStep)
	{
		var data = new byte[pLevels * 12];
		for (var vector = 0; vector < 6; vector++)
		{
			for (var level = 0; level < pLevels; level++)
			{
				var value = pBase + (pStep * level);
				var at = ((vector * pLevels) + level) * 2;
				data[at] = (byte)(value & 0xff);
				data[at + 1] = (byte)((value >> 8) & 0xff);
			}
		}
		return data;
	}

	// ---- Der Decoder

	/// <summary>
	/// Six vectors, one entry per level.
	/// </summary>
	/// <remarks>
	/// The values grow with the level, which is the whole point: <strong>a
	/// reader that kept the first entry of each vector would give every level of
	/// this class the same stats</strong>, and a hero who reached level 99 would
	/// be exactly as strong as one who reached level 2.
	/// </remarks>
	public void Test_TheChunkIsSixLevelVectors()
	{
		var ok = Rm2kClassParameterDecoder.TryDecode(
			Chunk(pLevels: 5, pBase: 10, pStep: 3), out var result, out var error);

		AssertEq(ok, true, $"and the chunk decodes; the error is \"{error}\"");
		AssertEq(
			result.Count, 6,
			$"**and there are six vectors**, because the chunk holds six stats;"
			+ $" there are {result.Count}");
		AssertEq(
			((List<int>)result["maxhp"]).Count, 5,
			"**and the max hit point vector has one entry per level**, which is"
			+ $" five here, not one; it has {((List<int>)result["maxhp"]).Count}");

		var maxhp = (List<int>)result["maxhp"];
		AssertEq(maxhp[0], 10, "and level 1 is 10; it is " + maxhp[0]);
		AssertEq(maxhp[1], 13, "and level 2 is 13, three more; it is " + maxhp[1]);
		AssertEq(maxhp[4], 22, "and level 5 is 22; it is " + maxhp[4]);
	}

	/// <summary>
	/// The six vectors are in liblcf's order, not alphabetical.
	/// </summary>
	/// <remarks>
	/// <c>WriteLcf</c> writes maxhp, maxsp, attack, defense, spirit, agility.
	/// <strong>A reader that assumed alphabetical order would give a class
	/// agility as its hit points</strong>, and the numbers would all be in range,
	/// so nothing would look wrong.
	/// </remarks>
	public void Test_TheOrderIsLiblcfsOrder()
	{
		// Six distinct values, one per vector, over a single level.
		var data = new byte[12];
		for (var vector = 0; vector < 6; vector++)
		{
			var value = (vector + 1) * 11;
			data[vector * 2] = (byte)(value & 0xff);
			data[(vector * 2) + 1] = (byte)((value >> 8) & 0xff);
		}
		Rm2kClassParameterDecoder.TryDecode(
			data, out var result, out _);

		AssertEq(
			((List<int>)result["maxhp"])[0], 11,
			"**and the first vector is maxhp at 11**, because liblcf writes it"
			+ $" first; it is {((List<int>)result["maxhp"])[0]}");
		AssertEq(
			((List<int>)result["agility"])[0], 66,
			"**and the sixth is agility at 66**, which is the last one written"
			+ $" and not the first; it is {((List<int>)result["agility"])[0]}");
	}

	/// <summary>
	/// The values are little endian, signed, and both halves matter.
	/// </summary>
	/// <remarks>
	/// A stat above 255 has a non-zero high byte. <strong>A reader that read one
	/// byte would cap every stat at 255</strong>, and a game with a max hit
	/// point above 255 would have its heroes quietly weakened.
	/// </remarks>
	public void Test_TheHighByteCounts()
	{
		var data = new byte[12];
		// 300 in the first vector, level 1.
		data[0] = 300 & 0xff;
		data[1] = 300 >> 8;
		Rm2kClassParameterDecoder.TryDecode(data, out var result, out _);

		AssertEq(
			((List<int>)result["maxhp"])[0], 300,
			"**and 300 comes back as 300**, because both bytes are read; a"
			+ " one-byte reader would have said 44;"
			+ $" it is {((List<int>)result["maxhp"])[0]}");
	}

	/// <summary>
	/// A chunk that is not six equal-length vectors is refused and says why.
	/// </summary>
	/// <remarks>
	/// The whole chunk is a multiple of twelve or it is a different structure.
	/// <strong>A reader that decoded it anyway would read six values out of a
	/// chunk that holds something else</strong>, and the stat numbers would be
	/// plausible — which is worse than a refusal.
	/// </remarks>
	public void Test_AMalformedChunkIsRefused()
	{
		var ok = Rm2kClassParameterDecoder.TryDecode(
			new byte[7], out _, out var error);

		AssertEq(ok, false, "and a seven-byte chunk is refused");
		AssertTrue(
			error.Contains("multiple of 12"),
			$"**and the reason names the structure**, because 'refused' alone"
			+ $" cannot be acted on; the error is \"{error}\"");

		// More levels than the format allows.
		var zuViel = Rm2kClassParameterDecoder.TryDecode(
			Chunk(pLevels: 200, pBase: 1, pStep: 1), out _, out var error2);
		AssertEq(zuViel, false, "and 200 levels is refused");
		AssertTrue(
			error2.Contains("stops at"),
			$"and that refusal names the bound too; the error is \"{error2}\"");
	}

	/// <summary>
	/// Levels are one based in a game and zero based in the array.
	/// </summary>
	/// <remarks>
	/// The reference reads <c>parameters[level]</c> after decrementing. <strong>A
	/// reader that skipped the decrement would hand a level 1 hero the level 0
	/// row</strong> — and on a class whose first level is deliberately weak that
	/// is the difference between a tutorial and a hero who starts the game
	/// under-strengthed.
	/// </remarks>
	public void Test_LevelsAreOneBasedInAGame()
	{
		var klasse = new Rm2kDatabaseModel.Class { Id = 1, Name = "Fighter" };
		klasse.Parameters["maxhp"] = [20, 30, 45];

		AssertEq(
			klasse.TryValueAt("maxhp", 1, out var level1), true,
			"**and level 1 is readable**, because a game writes levels that way");
		AssertEq(
			level1, 20,
			$"**and it is 20, the first row**, not the second; it is {level1}");
		AssertEq(
			klasse.TryValueAt("maxhp", 3, out var level3), true,
			"and level 3 is readable too");
		AssertEq(
			level3, 45, $"and it is 45, the last row; it is {level3}");
	}

	/// <summary>
	/// A level outside the vector, and a class with no parameters, both say so.
	/// </summary>
	/// <remarks>
	/// <strong>Returning zero would be the wrong answer twice.</strong> A class
	/// whose chunk was absent has no parameters, and a zero hit point maximum
	/// reads like a design choice — a hero the game made unplayable rather than
	/// a file that did not parse.
	/// </remarks>
	public void Test_AMissingLevelOrStatSaysSo()
	{
		var klasse = new Rm2kDatabaseModel.Class { Id = 1 };
		klasse.Parameters["maxhp"] = [20, 30];

		AssertEq(
			klasse.TryValueAt("maxhp", 0, out var level0), false,
			"**and level 0 is refused**, because levels start at one in a game");
		AssertEq(
			klasse.TryValueAt("maxhp", 9, out var level9), false,
			"**and level 9 is refused**, because the vector stops at two");
		AssertEq(
			klasse.TryValueAt("agility", 1, out _), false,
			"**and a stat the class does not carry is refused**, because the"
			+ " six are named and not assumed");
		AssertEq(
			new Rm2kDatabaseModel.Class { Id = 2 }
				.TryValueAt("maxhp", 1, out _),
			false,
			"**and a class with no parameters at all is refused**, which is the"
			+ " state a game is in when its chunk did not decode");
	}

	/// <summary>
	/// The chunk's size is what a real class occupies, not a round number.
	/// </summary>
	/// <remarks>
	/// A 99-level class is 1188 bytes: six vectors of 99 <c>int16</c>. <strong>The
	/// test builds the chunk rather than trusting the number</strong>, because a
	 /// hand-written byte count would pass even if the decoder disagreed about
	/// the structure.
	/// </remarks>
	public void Test_AFullClassChunkIsTheSizeTheFormatImplies()
	{
		var voll = Chunk(pLevels: 99, pBase: 1, pStep: 1);
		AssertEq(voll.Length, 1188, "and a 99-level class chunk is 1188 bytes");

		var ok = Rm2kClassParameterDecoder.TryDecode(voll, out var result, out var error);
		AssertEq(ok, true, $"and it decodes; the error is \"{error}\"");
		AssertEq(
			((List<int>)result["maxhp"]).Count, 99,
			$"**and every vector has all 99 levels**, because a class is defined"
			+ $" for every level and not for the ones the heroes reach;"
			+ $" it has {((List<int>)result["maxhp"]).Count}");
		AssertEq(
			((List<int>)result["maxhp"])[98], 99,
			$"and the last level is 99, one more than the first; it is"
			+ $" {((List<int>)result["maxhp"])[98]}");
	}
}
