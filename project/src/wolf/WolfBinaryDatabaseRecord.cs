using System;
using System.Collections.Generic;
using System.Text;

namespace UniversalRPG.Wolf;

/// <summary>Which block of a WOLF property table a value lives in.</summary>
public enum WolfBinaryPropertyBlock
{
	/// <summary>The value is a 32-bit number in the record's number block.</summary>
	Number = 1,

	/// <summary>The value is a string in the record's string block.</summary>
	String = 2,
}

/// <summary>One property of a WOLF binary database record.</summary>
/// <remarks>
/// A WOLF record does not store its values in order. It stores a table of
/// positions, and the positions say whether the value is in the number block or
/// the string block. Two records of the same type can therefore interleave
/// their values differently and still mean the same thing.
/// </remarks>
public sealed class WolfBinaryProperty
{
	/// <summary>Which block this value lives in, from <c>raw / 1000</c>.</summary>
	public WolfBinaryPropertyBlock Block { get; }

	/// <summary>The index inside that block, from <c>raw % 1000</c>.</summary>
	public int Position { get; }

	public WolfBinaryProperty(int pRaw)
	{
		// The encoding is positional: the block is the thousands digit and the
		// index is the remainder. Dividing rather than shifting is what the
		// format does, so a raw of 1001 is block 1 at position 1.
		Block = (WolfBinaryPropertyBlock)(pRaw / 1000);
		Position = pRaw % 1000;
	}

	/// <summary>Whether the block is one the format defines.</summary>
	public bool IsKnownBlock => Block == WolfBinaryPropertyBlock.Number || Block == WolfBinaryPropertyBlock.String;

	/// <inheritdoc/>
	public override string ToString()
	{
		return $"{(Block == WolfBinaryPropertyBlock.Number ? "N" : "S")}{Position}";
	}
}

/// <summary>One WOLF database record: a type's data with its property table.</summary>
public sealed class WolfBinaryDatabaseRecord
{
	/// <summary>How the data id of this record is derived, from <c>data_id_method</c>.</summary>
	public int DataIdMethod { get; }

	/// <summary>The number of properties in the table.</summary>
	public int PropertyCount { get; }

	/// <summary>The property table, in the order the file stores it.</summary>
	public IReadOnlyList<WolfBinaryProperty> Properties { get; }

	/// <summary>The number block: the 32-bit values, in index order.</summary>
	public IReadOnlyList<int> NumberBlock { get; }

	/// <summary>The string block, in index order.</summary>
	public IReadOnlyList<string> StringBlock { get; }

	public WolfBinaryDatabaseRecord(
		int pDataIdMethod,
		IReadOnlyList<WolfBinaryProperty> pProperties,
		IReadOnlyList<int> pNumberBlock,
		IReadOnlyList<string> pStringBlock)
	{
		DataIdMethod = pDataIdMethod;
		Properties = pProperties;
		PropertyCount = pProperties.Count;
		NumberBlock = pNumberBlock;
		StringBlock = pStringBlock;
	}

	/// <summary>
	/// The value of a property, or null when the table points outside the block.
	/// </summary>
	/// <remarks>
	/// A position outside the block is reported as missing rather than clamped
	/// or defaulted. The table is written by the editor and a position past the
	/// end of the block means the record is not what the file claims, so
	/// substituting a zero here would invent a value the game never had.
	/// </remarks>
	public object? ValueAt(int pIndex)
	{
		if (pIndex < 0 || pIndex >= Properties.Count)
		{
			return null;
		}
		var property = Properties[pIndex];
		if (!property.IsKnownBlock)
		{
			return null;
		}
		if (property.Block == WolfBinaryPropertyBlock.Number)
		{
			return property.Position >= 0 && property.Position < NumberBlock.Count
				? NumberBlock[property.Position]
				: null;
		}
		return property.Position >= 0 && property.Position < StringBlock.Count
			? StringBlock[property.Position]
			: null;
	}

	/// <summary>
	/// A number property, or null when the property is missing, is a string, or
	/// is outside the number block.
	/// </summary>
	public int? NumberAt(int pIndex)
	{
		return ValueAt(pIndex) is int value ? value : null;
	}

	/// <summary>
	/// A string property, or null when the property is missing, is a number, or
	/// is outside the string block.
	/// </summary>
	public string? StringAt(int pIndex)
	{
		return ValueAt(pIndex) as string;
	}
}
