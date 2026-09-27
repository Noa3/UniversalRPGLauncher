using System.Collections.Generic;
using UniversalRPG.Plugins;
using UniversalRPG.Tests.Framework;
using UniversalRPG.Wolf;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Proves the WOLF binary database framing against the published format.
/// </summary>
/// <remarks>
/// The part worth proving is the position table. A WOLF record does not store
/// its values in order, and reading them in file order instead of through the
/// table produces data that looks right and is not. The fixtures here are built
/// so that a reader which ignores the table gets different values rather than
/// failing outright, which is the failure mode a real game would hide.
/// </remarks>
public partial class TestWolfBinaryDatabase : TestBase
{
	private static readonly WolfParseLimits Limits = new();

	/// <summary>The file header: magic, version header, FM marker, version byte.</summary>
	private static List<byte> Header(byte pVersionHeader = 0x00, byte pVersion = 0x03)
	{
		return
		[
			0x00, 0x57, 0x00, 0x00, 0x4F, 0x4C,
			pVersionHeader,
			0x46, 0x4D, 0x00,
			pVersion,
		];
	}

	private static void AddInt32(List<byte> pBytes, int pValue)
	{
		pBytes.Add((byte)(pValue & 0xFF));
		pBytes.Add((byte)((pValue >> 8) & 0xFF));
		pBytes.Add((byte)((pValue >> 16) & 0xFF));
		pBytes.Add((byte)((pValue >> 24) & 0xFF));
	}

	private static void AddString(List<byte> pBytes, string pText)
	{
		var raw = System.Text.Encoding.UTF8.GetBytes(pText);
		AddInt32(pBytes, raw.Length);
		pBytes.AddRange(raw);
		pBytes.Add(0x00);
	}

	/// <summary>
	/// One type: the sub header, the data id method, the position table, the
	/// record count, and then the numbers and strings of every record.
	/// </summary>
	private static void AddType(
		List<byte> pBytes, int[] pPositions, int pDataIdMethod, int pRecordCount,
		int[][] pNumbers, string[][] pStrings)
	{
		pBytes.AddRange([0xFE, 0xFF, 0xFF, 0xFF]);
		AddInt32(pBytes, pDataIdMethod);
		AddInt32(pBytes, pPositions.Length);
		foreach (var position in pPositions)
		{
			AddInt32(pBytes, position);
		}
		AddInt32(pBytes, pRecordCount);
		for (var record = 0; record < pRecordCount; record++)
		{
			foreach (var value in pNumbers[record])
			{
				AddInt32(pBytes, value);
			}
			foreach (var value in pStrings[record])
			{
				AddString(pBytes, value);
			}
		}
	}

	/// <summary>A header plus the four byte type count the reader needs to start.</summary>
	private static byte[] HeaderAndTypeCount(byte pVersionHeader = 0x00)
	{
		var bytes = Header(pVersionHeader);
		AddInt32(bytes, 1);
		return bytes.ToArray();
	}

	public void Test_TheVerifiedHeaderIsRecognised()
	{
		// 0 'W' 0 0 'O' 'L', then the version header as a single byte where 0 is
		// v2 and 0x55 is v3, then 'F' 'M' 0, then the version byte. The check
		// also needs the type count behind it, because a file that stops after
		// the header cannot be read and saying otherwise would be a lie.
		AssertTrue(WolfBinaryDatabaseReader.HasDatabaseHeader(HeaderAndTypeCount()),
			"a v2 header followed by a type count is a database header");
		AssertTrue(WolfBinaryDatabaseReader.HasDatabaseHeader(HeaderAndTypeCount(pVersionHeader: 0x55)),
			"and so is a v3 header");

		var bytes = Header();
		bytes[1] = 0x58; // 'X' instead of 'W'
		AssertTrue(!WolfBinaryDatabaseReader.HasDatabaseHeader(bytes.ToArray()),
			"but a different first magic letter is not");

		AssertTrue(!WolfBinaryDatabaseReader.HasDatabaseHeader(Header().ToArray()),
			"and a header with no type count behind it is not readable either");
	}

	public void Test_AnUnknownVersionHeaderIsRefusedRatherThanGuessed()
	{
		// The version header is a single byte with two defined values. Accepting
		// any other byte would decode a file whose layout is unknown.
		AssertTrue(!WolfBinaryDatabaseReader.HasDatabaseHeader(HeaderAndTypeCount(pVersionHeader: 0x42)),
			"an undefined version header is not a database header");
	}

	public void Test_OneRecordWithANumberAndAStringRoundTrips()
	{
		// The position table packs block and index: 1000 is block 1 at index 0,
		// 2000 is block 2 at index 0. A record of one number and one string
		// therefore has a one-entry number block and a one-entry string block.
		var bytes = Header();
		AddInt32(bytes, 1); // one record type
		AddType(bytes, pPositions: [1000, 2000], pDataIdMethod: 1, pRecordCount: 1,
			pNumbers: [[42]], pStrings: [["Alex"]]);
		bytes.Add(0xC2); // the v2 version footer

		var result = new WolfBinaryDatabaseReader(Limits).Read(bytes.ToArray(), "DataBase.dat");
		AssertTrue(result.Success, "the fixture is a valid database");
		AssertEq(result.Value!.Records.Count, 1, "and it has one record");
		AssertEq(result.Value.Version, 0x03, "the version byte is 3");
		AssertEq(result.Value.Footer, 0xC2, "the version footer is 0xC2");

		var record = result.Value.Records[0];
		AssertEq(record.NumberAt(0)!.Value, 42, "property 0 is the number 42");
		AssertEq(record.StringAt(1), "Alex", "and property 1 is the name");
	}

	public void Test_ThePositionTableDecidesTheOrderNotTheFileOrder()
	{
		// This is the fixture that matters. The table puts the string first and
		// the number second, so a reader that reads the blocks in the order the
		// properties appear would swap them. The stored values are deliberately
		// asymmetric so a swap is visible.
		var bytes = Header();
		AddInt32(bytes, 1);
		AddType(bytes, pPositions: [2000, 1000], pDataIdMethod: 1, pRecordCount: 1,
			pNumbers: [[7]], pStrings: [["name"]]);
		bytes.Add(0xC2);

		var result = new WolfBinaryDatabaseReader(Limits).Read(bytes.ToArray(), "DataBase.dat");
		AssertTrue(result.Success, "the fixture is valid");
		var record = result.Value!.Records[0];
		AssertEq(record.StringAt(0), "name", "property 0 is the string, because the table says so");
		AssertEq(record.NumberAt(1)!.Value, 7, "and property 1 is the number");
	}

	public void Test_TwoRecordsOfOneTypeKeepTheirOwnValues()
	{
		// The position table belongs to the type, not to a record, so every record
		// of the type has the same shape and its own values.
		var bytes = Header();
		AddInt32(bytes, 1);
		AddType(bytes, pPositions: [1000, 2000], pDataIdMethod: 1, pRecordCount: 2,
			pNumbers: [[1], [2]], pStrings: [["first"], ["second"]]);
		bytes.Add(0xC2);

		var result = new WolfBinaryDatabaseReader(Limits).Read(bytes.ToArray(), "DataBase.dat");
		AssertTrue(result.Success, "the fixture is valid");
		AssertEq(result.Value!.Records.Count, 2, "there are two records");
		AssertEq(result.Value.Records[0].StringAt(1), "first", "the first has its own name");
		AssertEq(result.Value.Records[1].StringAt(1), "second", "and the second has its own");
		AssertEq(result.Value.Records[0].NumberAt(0)!.Value, 1, "and their numbers differ too");
		AssertEq(result.Value.Records[1].NumberAt(0)!.Value, 2, "in the same order as the file");
	}

	public void Test_SeveralTypesAreReadInFileOrder()
	{
		// The type count is in the header and each type is self contained, so a
		// file with more than one type is just the blocks in sequence.
		var bytes = Header();
		AddInt32(bytes, 2);
		AddType(bytes, pPositions: [1000], pDataIdMethod: 0, pRecordCount: 1,
			pNumbers: [[10]], pStrings: [[]]);
		AddType(bytes, pPositions: [2000], pDataIdMethod: 2, pRecordCount: 1,
			pNumbers: [[]], pStrings: [["type two"]]);
		bytes.Add(0xC2);

		var result = new WolfBinaryDatabaseReader(Limits).Read(bytes.ToArray(), "DataBase.dat");
		AssertTrue(result.Success, "the fixture is valid");
		AssertEq(result.Value!.Records.Count, 2, "both types contributed a record");
		AssertEq(result.Value.Records[0].NumberAt(0)!.Value, 10, "the first is from the first type");
		AssertEq(result.Value.Records[1].StringAt(0), "type two", "and the second from the second");
		AssertEq(result.Value.Records[0].DataIdMethod, 0, "the data id method belongs to the type");
		AssertEq(result.Value.Records[1].DataIdMethod, 2, "and differs between types");
	}

	public void Test_ATypeWithNoPropertiesStillReadsItsRecords()
	{
		// property_block1_count is derived from the table, so an empty table means
		// empty blocks and the records still have to be counted.
		var bytes = Header();
		AddInt32(bytes, 1);
		AddType(bytes, pPositions: [], pDataIdMethod: 0, pRecordCount: 3,
			pNumbers: [[], [], []], pStrings: [[], [], []]);
		bytes.Add(0xC2);

		var result = new WolfBinaryDatabaseReader(Limits).Read(bytes.ToArray(), "DataBase.dat");
		AssertTrue(result.Success, "the fixture is valid");
		AssertEq(result.Value!.Records.Count, 3, "the three empty records are all there");
		AssertEq(result.Value.Records[0].PropertyCount, 0, "with no properties at all");
	}

	public void Test_APropertyOutsideItsBlockIsReportedAsMissing()
	{
		// A position past the end of the block means the record is not what the
		// file claims. Defaulting to zero would be indistinguishable from a value
		// the game actually has, so it is reported as missing instead.
		var bytes = Header();
		AddInt32(bytes, 1);
		AddType(bytes, pPositions: [1005, 2000], pDataIdMethod: 1, pRecordCount: 1,
			pNumbers: [[7]], pStrings: [["x"]]);
		bytes.Add(0xC2);

		var result = new WolfBinaryDatabaseReader(Limits).Read(bytes.ToArray(), "DataBase.dat");
		AssertTrue(result.Success, "the file itself is still valid");
		var record = result.Value!.Records[0];
		AssertEq(record.NumberAt(0), null, "a number property past the block is missing");
		AssertEq(record.NumberAt(9), null, "and so is a property index that does not exist");
		AssertEq(record.StringAt(1), "x", "while the string is still readable");
	}

	public void Test_TheBlockAndIndexSplitIsTheThousandsDigit()
	{
		// A raw of 1005 is block 1 at index 5, and 2013 is block 2 at index 13.
		var number = new WolfBinaryProperty(1005);
		AssertEq((int)number.Block, 1, "1005 is in the number block");
		AssertEq(number.Position, 5, "at index 5");

		var text = new WolfBinaryProperty(2013);
		AssertEq((int)text.Block, 2, "2013 is in the string block");
		AssertEq(text.Position, 13, "at index 13");

		AssertTrue(!new WolfBinaryProperty(3000).IsKnownBlock,
			"and a block the format does not define is reported as unknown");
	}

	public void Test_AStringIsLengthPrefixedAndNulTerminated()
	{
		// The length counts the bytes and the terminator sits one past it, so
		// reading the length and stopping there would leave the terminator to be
		// read as the next value.
		var bytes = Header();
		AddInt32(bytes, 1);
		AddType(bytes, pPositions: [2000], pDataIdMethod: 1, pRecordCount: 1,
			pNumbers: [[]], pStrings: [["abc"]]);
		bytes.Add(0xC2);

		var result = new WolfBinaryDatabaseReader(Limits).Read(bytes.ToArray(), "DataBase.dat");
		AssertTrue(result.Success, "the reader consumed the terminator and reached the footer");
		AssertEq(result.Value!.Records[0].StringAt(0), "abc", "the string round trips");
	}

	public void Test_AnEmptyStringIsAZeroLengthAndAStaysTerminator()
	{
		var bytes = Header();
		AddInt32(bytes, 1);
		AddType(bytes, pPositions: [2000], pDataIdMethod: 1, pRecordCount: 1,
			pNumbers: [[]], pStrings: [[""]]);
		bytes.Add(0xC2);

		var result = new WolfBinaryDatabaseReader(Limits).Read(bytes.ToArray(), "DataBase.dat");
		AssertTrue(result.Success, "an empty string is valid");
		AssertEq(result.Value!.Records[0].StringAt(0), "", "and it is the empty string, not null");
	}

	public void Test_AMissingFooterIsRefused()
	{
		// The footer is what closes the file. Accepting a file without it would
		// mean the last value was never checked.
		var bytes = Header();
		AddInt32(bytes, 1);
		AddType(bytes, pPositions: [1000], pDataIdMethod: 1, pRecordCount: 1,
			pNumbers: [[1]], pStrings: [[]]);

		var result = new WolfBinaryDatabaseReader(Limits).Read(bytes.ToArray(), "DataBase.dat");
		AssertTrue(!result.Success, "a database with no version footer is refused");
	}

	public void Test_TrailingBytesAfterTheFooterAreRefused()
	{
		// Data after the footer is not part of the format, and silently ignoring
		// it would hide a file this reader does not actually understand.
		var bytes = Header();
		AddInt32(bytes, 1);
		AddType(bytes, pPositions: [1000], pDataIdMethod: 1, pRecordCount: 1,
			pNumbers: [[1]], pStrings: [[]]);
		bytes.Add(0xC2);
		bytes.AddRange([0x00, 0x00]);

		var result = new WolfBinaryDatabaseReader(Limits).Read(bytes.ToArray(), "DataBase.dat");
		AssertTrue(!result.Success, "trailing bytes after the footer are refused");
	}

	public void Test_AWrongSubHeaderIsRefused()
	{
		// The per type sub header is what separates the types. A file that does
		// not carry it is not a database this reader understands.
		var bytes = Header();
		AddInt32(bytes, 1);
		bytes.AddRange([0xFE, 0xFF, 0xFF, 0x00]); // last byte wrong
		AddInt32(bytes, 1);
		AddInt32(bytes, 1);
		AddInt32(bytes, 1000);
		AddInt32(bytes, 1);
		AddInt32(bytes, 5);
		bytes.Add(0xC2);

		var result = new WolfBinaryDatabaseReader(Limits).Read(bytes.ToArray(), "DataBase.dat");
		AssertTrue(!result.Success, "a wrong sub header is refused");
	}

	public void Test_AnUnterminatedStringIsRefused()
	{
		// A length that runs past the end of the file, or a missing terminator,
		// both mean the string cannot be read. Clamping the length would invent
		// the tail of the value.
		var bytes = Header();
		AddInt32(bytes, 1);
		bytes.AddRange([0xFE, 0xFF, 0xFF, 0xFF]);
		AddInt32(bytes, 1); // data id method
		AddInt32(bytes, 1); // one property
		AddInt32(bytes, 2000); // string at index 0
		AddInt32(bytes, 1); // one record
		AddInt32(bytes, 5); // a length of five with only one byte left
		bytes.Add((byte)'x');
		bytes.Add(0xC2);

		var result = new WolfBinaryDatabaseReader(Limits).Read(bytes.ToArray(), "DataBase.dat");
		AssertTrue(!result.Success, "a string running past the end of the file is refused");

		// The other way to be unterminated: the bytes are all there, but the byte
		// after the string is not the NUL. This has to be refused on its own,
		// because a reader that skipped the check would take that byte as the next
		// value and carry on.
		var missing = Header();
		AddInt32(missing, 1);
		missing.AddRange([0xFE, 0xFF, 0xFF, 0xFF]);
		AddInt32(missing, 1);
		AddInt32(missing, 1);
		AddInt32(missing, 2000);
		AddInt32(missing, 1);
		AddInt32(missing, 3);
		missing.AddRange(System.Text.Encoding.UTF8.GetBytes("abc"));
		missing.Add(0x41); // 'A' where the NUL belongs
		missing.Add(0xC2);

		var second = new WolfBinaryDatabaseReader(Limits).Read(missing.ToArray(), "DataBase.dat");
		AssertTrue(!second.Success, "a string whose terminator is not NUL is refused");
		AssertTrue(
			second.Error!.Message.Contains("NUL"),
			"and it says so, rather than reporting a later value as broken");
	}

	public void Test_ANonZeroTypeCountThatIsNotThereIsRefused()
	{
		var bytes = Header();
		AddInt32(bytes, 0); // no record types at all
		bytes.Add(0xC2);

		var result = new WolfBinaryDatabaseReader(Limits).Read(bytes.ToArray(), "DataBase.dat");
		AssertTrue(!result.Success, "a database with no record types is refused");
	}

	public void Test_AFileWithTheWrongMagicIsRefusedBeforeAnythingIsRead()
	{
		var bytes = Header();
		bytes[0] = 0x01;
		var result = new WolfBinaryDatabaseReader(Limits).Read(bytes.ToArray(), "DataBase.dat");
		AssertTrue(!result.Success, "a file without the magic is refused");
		AssertEq(result.Error!.Code, PluginErrorCode.InvalidGame, "and it is reported as invalid game data");
	}
}
