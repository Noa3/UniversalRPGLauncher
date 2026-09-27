using System;
using System.Collections.Generic;
using System.Text;
using UniversalRPG.Plugins;

namespace UniversalRPG.Wolf;

/// <summary>The decoded contents of a WOLF binary database file.</summary>
public sealed class WolfBinaryDatabaseData
{
	/// <summary>The file version byte, after the <c>FM\0</c> marker.</summary>
	public int Version { get; init; }

	/// <summary>
	/// The records, in the order the file stores them. The file groups records by
	/// type, so consecutive records share a schema and the group boundaries are
	/// what a caller needs.
	/// </summary>
	public IReadOnlyList<WolfBinaryDatabaseRecord> Records { get; init; } = [];

	/// <summary>The trailing version footer byte.</summary>
	public int Footer { get; init; }
}

/// <summary>
/// Reads the WOLF <c>DataBase.dat</c>, <c>CDataBase.dat</c> and
/// <c>SysDataBase.dat</c> binary format as data only.
/// </summary>
/// <remarks>
/// <para>
/// The file is a header, a record type count, and then one block per type. A
/// record does not store its values in order: it stores a table of positions
/// where each entry packs the block and the index into a single number, the block
/// in the thousands digit and the index in the remainder. The values then live
/// in two separate blocks, numbers first and strings second. Reading the values
/// in file order instead of through that table produces plausible but wrong
/// data, which is why the table is decoded rather than assumed.
/// </para>
/// <para>
/// The two block sizes are not stored anywhere. They are derived from the
/// position table, so a type with a string before a number still works and a type
/// with no properties needs no special case.
/// </para>
/// <para>
/// Every field here is taken from the published format description. A file that
/// does not match the framing is refused rather than partially decoded, and a
/// property whose position points outside its block is reported as missing
/// rather than defaulted, because a zero would be indistinguishable from a value
/// the game actually has.
/// </para>
/// <para>
/// This reader is structural only. It is validated against byte sequences built
/// from the specification, not against a real WOLF game, so the framing and the
/// field order are proven but no single property is shown to mean a particular
/// thing.
/// </para>
/// </remarks>
public sealed class WolfBinaryDatabaseReader
{
	/// <summary>The leading magic, <c>0 'W' 0 0 'O' 'L'</c>.</summary>
	private static readonly byte[] FileMagic = [0x00, 0x57, 0x00, 0x00, 0x4F, 0x4C];

	/// <summary>The marker after the version header, <c>'F' 'M' 0</c>.</summary>
	private static readonly byte[] MagicTwo = [0x46, 0x4D, 0x00];

	/// <summary>The per-record-type sub header, <c>0xFE 0xFF 0xFF 0xFF</c>.</summary>
	private static readonly byte[] TypeSubHeader = [0xFE, 0xFF, 0xFF, 0xFF];

	/// <summary>How many bytes the file header occupies before the type count.</summary>
	private const int HeaderBytes = 6 + 1 + 3 + 1;

	private readonly WolfParseLimits _limits;

	public WolfBinaryDatabaseReader(WolfParseLimits pLimits)
	{
		_limits = pLimits ?? throw new ArgumentNullException(nameof(pLimits));
	}

	/// <summary>True when the payload carries the verified database framing.</summary>
	public static bool HasDatabaseHeader(byte[] pBytes)
	{
		if (pBytes == null || pBytes.Length < HeaderBytes + 4)
		{
			return false;
		}
		for (var index = 0; index < FileMagic.Length; index++)
		{
			if (pBytes[index] != FileMagic[index])
			{
				return false;
			}
		}
		// The version header sits between the two magics: 0 for v2 and 0x55 for
		// v3. Anything else is not a database file this reader understands.
		var versionHeader = pBytes[FileMagic.Length];
		if (versionHeader != 0x00 && versionHeader != 0x55)
		{
			return false;
		}
		for (var index = 0; index < MagicTwo.Length; index++)
		{
			if (pBytes[FileMagic.Length + 1 + index] != MagicTwo[index])
			{
				return false;
			}
		}
		return true;
	}

	/// <summary>Reads a binary database file.</summary>
	/// <param name="pBytes">The whole file.</param>
	/// <param name="pSourceName">The name used in diagnostics.</param>
	public PluginResult<WolfBinaryDatabaseData> Read(byte[] pBytes, string pSourceName)
	{
		if (pBytes == null)
		{
			throw new ArgumentNullException(nameof(pBytes));
		}
		if (pBytes.LongLength > _limits.MaxFileBytes)
		{
			return Fail<WolfBinaryDatabaseData>($"WOLF database {pSourceName} is {pBytes.LongLength} bytes, over the {_limits.MaxFileBytes} byte limit");
		}
		if (!HasDatabaseHeader(pBytes))
		{
			return Fail<WolfBinaryDatabaseData>($"WOLF database {pSourceName} does not carry the verified WOLF/FM header");
		}

		var offset = HeaderBytes;
		// The version byte is the last of the header, so it sits at index 10:
		// six magic bytes, the one byte version header, then the three byte FM
		// marker. Reading index 9 would return the marker's 'M'.
		var version = pBytes[HeaderBytes - 1];

		var typeCount = ReadInt32(pBytes, ref offset, pSourceName, "type count");
		if (!typeCount.Success)
		{
			return Refuse<WolfBinaryDatabaseData>(typeCount);
		}
		if (typeCount.Value <= 0 || typeCount.Value > _limits.MaxDatabaseRecords)
		{
			return Fail<WolfBinaryDatabaseData>($"WOLF database {pSourceName} declares {typeCount.Value} record types, outside 1 to {_limits.MaxDatabaseRecords}");
		}

		var records = new List<WolfBinaryDatabaseRecord>(typeCount.Value);
		for (var typeIndex = 0; typeIndex < typeCount.Value; typeIndex++)
		{
			var read = ReadTypeBlock(pBytes, ref offset, pSourceName, typeIndex);
			if (!read.Success)
			{
				return Refuse<WolfBinaryDatabaseData>(read);
			}
			records.AddRange(read.Value!);
		}

		// The file ends with a single version footer byte. Reading it closes the
		// file rather than leaving trailing bytes unexamined.
		if (offset + 1 > pBytes.Length)
		{
			return Fail<WolfBinaryDatabaseData>($"WOLF database {pSourceName} ends before the version footer");
		}
		var footer = pBytes[offset];
		offset += 1;
		if (offset != pBytes.Length)
		{
			return Fail<WolfBinaryDatabaseData>($"WOLF database {pSourceName} has {pBytes.Length - offset} bytes after the version footer");
		}

		return PluginResult<WolfBinaryDatabaseData>.Succeeded(new WolfBinaryDatabaseData
		{
			Version = version,
			Records = records,
			Footer = footer,
		});
	}

	/// <summary>Reads one record type: the sub header, the position table, then the value blocks.</summary>
	private PluginResult<List<WolfBinaryDatabaseRecord>> ReadTypeBlock(
		byte[] pBytes, ref int pOffset, string pSourceName, int pTypeIndex)
	{
		for (var index = 0; index < TypeSubHeader.Length; index++)
		{
			if (pOffset + 1 > pBytes.Length)
			{
				return Fail<List<WolfBinaryDatabaseRecord>>($"WOLF database {pSourceName} ends inside the type {pTypeIndex} sub header");
			}
			if (pBytes[pOffset] != TypeSubHeader[index])
			{
				return Fail<List<WolfBinaryDatabaseRecord>>(
					$"WOLF database {pSourceName} type {pTypeIndex} has 0x{pBytes[pOffset]:X2} where the sub header needs 0x{TypeSubHeader[index]:X2}");
			}
			pOffset += 1;
		}

		var dataIdMethod = ReadInt32(pBytes, ref pOffset, pSourceName, $"type {pTypeIndex} data id method");
		if (!dataIdMethod.Success)
		{
			return Refuse<List<WolfBinaryDatabaseRecord>>(dataIdMethod);
		}

		var propertyCount = ReadInt32(pBytes, ref pOffset, pSourceName, $"type {pTypeIndex} property count");
		if (!propertyCount.Success)
		{
			return Refuse<List<WolfBinaryDatabaseRecord>>(propertyCount);
		}
		if (propertyCount.Value < 0 || propertyCount.Value > _limits.MaxDatabaseFieldsPerRecord)
		{
			return Fail<List<WolfBinaryDatabaseRecord>>(
				$"WOLF database {pSourceName} type {pTypeIndex} declares {propertyCount.Value} properties, outside 0 to {_limits.MaxDatabaseFieldsPerRecord}");
		}

		// The position table belongs to the type, not to a record, so it is read
		// once and shared by every record of this type.
		var table = new WolfBinaryProperty[propertyCount.Value];
		for (var index = 0; index < propertyCount.Value; index++)
		{
			var raw = ReadInt32(pBytes, ref pOffset, pSourceName, $"type {pTypeIndex} property {index} position");
			if (!raw.Success)
			{
				return Refuse<List<WolfBinaryDatabaseRecord>>(raw);
			}
			table[index] = new WolfBinaryProperty(raw.Value);
		}

		var numberCount = CountNumberBlockEntries(table);
		var stringCount = propertyCount.Value - numberCount;

		var recordCount = ReadInt32(pBytes, ref pOffset, pSourceName, $"type {pTypeIndex} record count");
		if (!recordCount.Success)
		{
			return Refuse<List<WolfBinaryDatabaseRecord>>(recordCount);
		}
		if (recordCount.Value < 0 || recordCount.Value > _limits.MaxDatabaseRecords)
		{
			return Fail<List<WolfBinaryDatabaseRecord>>(
				$"WOLF database {pSourceName} type {pTypeIndex} declares {recordCount.Value} records, outside 0 to {_limits.MaxDatabaseRecords}");
		}

		var records = new List<WolfBinaryDatabaseRecord>(recordCount.Value);
		for (var recordIndex = 0; recordIndex < recordCount.Value; recordIndex++)
		{
			var numbers = new List<int>(numberCount);
			for (var index = 0; index < numberCount; index++)
			{
				var value = ReadInt32(pBytes, ref pOffset, pSourceName, $"type {pTypeIndex} record {recordIndex} number {index}");
				if (!value.Success)
				{
					return Refuse<List<WolfBinaryDatabaseRecord>>(value);
				}
				numbers.Add(value.Value);
			}

			var strings = new List<string>(stringCount);
			for (var index = 0; index < stringCount; index++)
			{
				var value = ReadString(pBytes, ref pOffset, pSourceName, $"type {pTypeIndex} record {recordIndex} string {index}");
				if (!value.Success)
				{
					return Refuse<List<WolfBinaryDatabaseRecord>>(value);
				}
				strings.Add(value.Value);
			}

			records.Add(new WolfBinaryDatabaseRecord(dataIdMethod.Value, table, numbers, strings));
		}

		return PluginResult<List<WolfBinaryDatabaseRecord>>.Succeeded(records);
	}

	/// <summary>
	/// Counts the table entries that point into the number block, from the
	/// <c>sum_block1</c> rule: the running count only grows on a number entry.
	/// </summary>
	private static int CountNumberBlockEntries(WolfBinaryProperty[] pTable)
	{
		var count = 0;
		foreach (var property in pTable)
		{
			if (property.Block == WolfBinaryPropertyBlock.Number)
			{
				count++;
			}
		}
		return count;
	}

	private PluginResult<int> ReadInt32(byte[] pBytes, ref int pOffset, string pSourceName, string pWhat)
	{
		if (pOffset + 4 > pBytes.Length)
		{
			return Fail<int>($"WOLF database {pSourceName} ends before the {pWhat} value");
		}
		var value = pBytes[pOffset]
			| (pBytes[pOffset + 1] << 8)
			| (pBytes[pOffset + 2] << 16)
			| (pBytes[pOffset + 3] << 24);
		pOffset += 4;
		return PluginResult<int>.Succeeded(value);
	}

	/// <summary>
	/// Reads a length-prefixed NUL terminated string. The length counts the bytes
	/// and the terminator is not part of it, so the reader has to consume one byte
	/// past the length or every following value is off by one.
	/// </summary>
	private PluginResult<string> ReadString(byte[] pBytes, ref int pOffset, string pSourceName, string pWhat)
	{
		var length = ReadInt32(pBytes, ref pOffset, pSourceName, $"{pWhat} length");
		if (!length.Success)
		{
			return Refuse<string>(length);
		}
		if (length.Value < 0 || length.Value > _limits.MaxStringBytes)
		{
			return Fail<string>($"WOLF database {pSourceName} {pWhat} declares {length.Value} bytes, outside 0 to {_limits.MaxStringBytes}");
		}
		if (pOffset + length.Value + 1 > pBytes.Length)
		{
			return Fail<string>($"WOLF database {pSourceName} ends inside the {pWhat}, which needs {length.Value} bytes and a terminator");
		}
		if (pBytes[pOffset + length.Value] != 0)
		{
			return Fail<string>($"WOLF database {pSourceName} {pWhat} is not NUL terminated at {length.Value}");
		}

		// The format declares these strings as UTF-8. Decoding strictly means a
		// Shift-JIS game reports invalid bytes instead of silently showing
		// replacement characters, which is the honest outcome for a reader that
		// was not told the encoding.
		string text;
		try
		{
			text = new UTF8Encoding(false, throwOnInvalidBytes: true).GetString(pBytes, pOffset, length.Value);
		}
		catch (DecoderFallbackException)
		{
			return Fail<string>($"WOLF database {pSourceName} {pWhat} is not valid UTF-8");
		}
		pOffset += length.Value + 1;
		return PluginResult<string>.Succeeded(text);
	}

	private static PluginResult<T> Fail<T>(string pMessage)
	{
		return PluginResult<T>.Failed(PluginError.Create(
			PluginErrorCode.InvalidGame,
			pMessage,
			EnginePluginIds.WolfRpg,
			"wolf-database"));
	}

	/// <summary>Carries a failed read's own error into a differently typed result.</summary>
	private static PluginResult<T> Refuse<T>(PluginResult<int> pFailed)
	{
		return PluginResult<T>.Failed(pFailed.Error!);
	}

	/// <summary>Carries a failed read's own error into a differently typed result.</summary>
	private static PluginResult<T> Refuse<T>(PluginResult<string> pFailed)
	{
		return PluginResult<T>.Failed(pFailed.Error!);
	}

	/// <summary>Carries a failed read's own error into a differently typed result.</summary>
	private static PluginResult<T> Refuse<T>(PluginResult<List<WolfBinaryDatabaseRecord>> pFailed)
	{
		return PluginResult<T>.Failed(pFailed.Error!);
	}
}
