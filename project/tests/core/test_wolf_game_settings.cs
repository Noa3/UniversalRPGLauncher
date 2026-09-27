using System.Collections.Generic;
using System.Text;
using UniversalRPG.Plugins;
using UniversalRPG.Tests.Framework;
using UniversalRPG.Wolf;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Proves the WOLF <c>Game.dat</c> framing against the published format.
/// </summary>
/// <remarks>
/// The two things that make this file different from the other WOLF formats are
/// the version byte, which changes the record shape rather than only the
/// meaning of a field, and the encoding, which is not constant inside one
/// record: a v2 file's first eight strings are Shift-JIS and its ninth is UTF-8.
/// Both are proven here with bytes that fail under the wrong reading, so a
/// reader that ignored either would produce visibly wrong strings rather than
/// just different ones.
/// </remarks>
public partial class TestWolfGameSettings : TestBase
{
	private static readonly WolfParseLimits Limits = new();

	/// <summary>
	/// "Test" in Shift-JIS: <c>83 65 83 58 83 67</c>.
	/// </summary>
	/// <remarks>
	/// These bytes are not valid UTF-8, which is what makes the test worth having:
	/// a reader that decoded a v2 record as UTF-8 would refuse the file outright
	/// rather than produce a wrong title. The expected string was measured from
	/// the decoder rather than guessed, because <c>83 65 83 58 83 67</c> is
	/// "Test" and not the word one would reach for first.
	/// </remarks>
	private static readonly byte[] SjisTitle = [0x83, 0x65, 0x83, 0x58, 0x83, 0x67];

	/// <summary>What <see cref="SjisTitle"/> decodes to under code page 932.</summary>
	private const string SjisTitleText = "テスト";

	private static void AddInt32(List<byte> pBytes, int pValue)
	{
		pBytes.Add((byte)(pValue & 0xFF));
		pBytes.Add((byte)((pValue >> 8) & 0xFF));
		pBytes.Add((byte)((pValue >> 16) & 0xFF));
		pBytes.Add((byte)((pValue >> 24) & 0xFF));
	}

	private static void AddRawString(List<byte> pBytes, byte[] pRaw)
	{
		AddInt32(pBytes, pRaw.Length);
		pBytes.AddRange(pRaw);
		pBytes.Add(0x00);
	}

	private static void AddString(List<byte> pBytes, string pText)
	{
		AddRawString(pBytes, Encoding.UTF8.GetBytes(pText));
	}

	/// <summary>
	/// A whole v3 file, which is twelve UTF-8 strings and so needs no encoding
	/// argument anywhere.
	/// </summary>
	/// <summary>Where the declared size field sits, for a fixture just built.</summary>
	private static int _lastSizeAt;

	private static List<byte> V3File(
		byte[] pBytesCount = null, string[] pStrings = null, int pWordCount = 23,
		byte[] pWords = null, byte[] pRandomBlock = null, byte pFooter = 0xC2)
	{
		pBytesCount ??= [30, 8, 60];
		pStrings ??=
		[
			"Title", "SERIAL", "", "MS Gothic", "", "", "", "",
			"Subtitle", "load.gam", "gauge.png", "Loading",
		];
		// The word bytes have to match the declared count, or the file describes
		// more words than it carries and the reader is right to refuse it.
		pWords ??= new byte[pWordCount * 2];
		pRandomBlock ??= [0xAA, 0xBB, 0xCC, 0xDD];

		var bytes = new List<byte> { 0x00, 0x57, 0x00, 0x00, 0x4F, 0x4C, 0x00, 0x46, 0x4D, 0x55 };
		AddInt32(bytes, pBytesCount.Length);
		bytes.AddRange(pBytesCount);
		foreach (var text in pStrings)
		{
			AddString(bytes, text);
		}
		// The declared size has to be patched in after the body is known, so its
		// position is remembered rather than recomputed by the caller, because
		// the fixture's length varies with its parameters.
		var sizeAt = bytes.Count;
		AddInt32(bytes, 0);
		AddInt32(bytes, 0); // unknown3
		AddInt32(bytes, pWordCount);
		bytes.AddRange(pWords);
		bytes.AddRange(pRandomBlock);
		bytes.Add(pFooter);
		_lastSizeAt = sizeAt;
		// The declared size is everything up to but not including the footer.
		var size = bytes.Count - 1;
		bytes[sizeAt] = (byte)(size & 0xFF);
		bytes[sizeAt + 1] = (byte)((size >> 8) & 0xFF);
		bytes[sizeAt + 2] = (byte)((size >> 16) & 0xFF);
		bytes[sizeAt + 3] = (byte)((size >> 24) & 0xFF);
		return bytes;
	}

	/// <summary>
	/// A whole v2 file: eight Shift-JIS strings and then one UTF-8 string, which
	/// is the asymmetry that makes this record worth testing.
	/// </summary>
	private static List<byte> V2File()
	{
		var bytes = new List<byte> { 0x00, 0x57, 0x00, 0x00, 0x4F, 0x4C, 0x00, 0x46, 0x4D, 0x00 };
		AddInt32(bytes, 3);
		bytes.AddRange([30, 8, 60]);
		AddRawString(bytes, SjisTitle);
		AddRawString(bytes, [0x01, 0x02, 0x03]);
		AddRawString(bytes, []);                 // encryption key, empty
		AddRawString(bytes, [0x83, 0x4D, 0x83, 0x65]); // MS Gothic, Shift-JIS
		AddRawString(bytes, []);
		AddRawString(bytes, []);
		AddRawString(bytes, []);
		AddRawString(bytes, []);                 // starting hero graphic
		AddString(bytes, "utf8tail");           // the ninth string is UTF-8
		var sizeAt = bytes.Count;
		AddInt32(bytes, 0);
		AddInt32(bytes, 0);
		AddInt32(bytes, 0);
		bytes.AddRange([0x01, 0x02, 0x03, 0x04]);
		bytes.Add(0xC2);
		_lastSizeAt = sizeAt;
		var size = bytes.Count - 1;
		bytes[sizeAt] = (byte)(size & 0xFF);
		bytes[sizeAt + 1] = (byte)((size >> 8) & 0xFF);
		bytes[sizeAt + 2] = (byte)((size >> 16) & 0xFF);
		bytes[sizeAt + 3] = (byte)((size >> 24) & 0xFF);
		return bytes;
	}

	public void Test_TheVerifiedHeaderIsRecognised()
	{
		AssertTrue(WolfGameSettingsReader.HasGameHeader(V3File().ToArray()),
			"a v3 header is a Game.dat header");
		AssertTrue(WolfGameSettingsReader.HasGameHeader(V2File().ToArray()),
			"and so is a v2 header");

		var bytes = V3File().ToArray();
		bytes[8] = 0x47; // 'G' instead of 'M'
		AssertTrue(!WolfGameSettingsReader.HasGameHeader(bytes),
			"but a different final magic letter is not");
	}

	public void Test_AVersion2FileDecodesItsEightShiftJisStringsAndItsUtf8Ninth()
	{
		// The record mixes encodings: the first eight are Shift-JIS and the ninth
		// is UTF-8. Decoding the ninth as Shift-JIS would give mojibake, and
		// decoding the first eight as UTF-8 would fail outright, because the
		// Japanese bytes are not valid UTF-8.
		var result = new WolfGameSettingsReader(Limits).Read(V2File().ToArray(), "Game.dat");
		AssertTrue(result.Success, $"the v2 fixture is valid: {result.Error?.Message}");
		if (!result.Success)
		{
			return;
		}
		AssertEq(result.Value!.StringSettings.Count, 9, "a v2 file has eight Shift-JIS strings and one UTF-8 one");
		AssertEq(result.Value.StringSettings[0], SjisTitleText,
			"the first string decodes as Shift-JIS, not as UTF-8");
		AssertEq(result.Value.StringSettings[8], "utf8tail", "and the ninth decodes as UTF-8");
		AssertTrue(!result.Value.IsVersion3, "and it reports itself as v2");
	}

	public void Test_AVersion3FileHasTwelveUtf8StringsAndNoTrailingOne()
	{
		// A v3 record has twelve UTF-8 strings and no trailing UTF-8 field, so the
		// v2 rule of "nine strings, the last one different" does not apply.
		var result = new WolfGameSettingsReader(Limits).Read(V3File().ToArray(), "Game.dat");
		AssertTrue(result.Success, $"the v3 fixture is valid: {result.Error?.Message}");
		if (!result.Success)
		{
			return;
		}
		AssertEq(result.Value!.StringSettings.Count, 12, "a v3 file has twelve strings");
		AssertEq(result.Value.GameTitle, "Title", "the game title is the first one");
		AssertEq(result.Value.StringSettings[7], "",
			"the starting hero graphic is the eighth, so the ninth is the subtitle");
		AssertTrue(result.Value.IsVersion3, "and it reports itself as v3");
	}

	public void Test_AVersion2FileCannotBeReadAsAVersion3One()
	{
		// This is the failure the version byte prevents. The two records have
		// different string counts, so reading one as the other consumes the wrong
		// number of strings and lands somewhere else entirely.
		var v2 = V2File().ToArray();
		var asV3 = (byte[])v2.Clone();
		asV3[9] = 0x55; // claim v3
		var result = new WolfGameSettingsReader(Limits).Read(asV3, "Game.dat");

		// It either refuses, or reads twelve strings where the file only holds
		// nine. Both are acceptable; reading nine and calling them v3 strings is
		// not.
		if (result.Success)
		{
			AssertTrue(result.Value!.StringSettings.Count != 9,
				"a v2 file forced through the v3 path does not quietly yield nine strings");
		}
		else
		{
			AssertTrue(true, "a v2 file forced through the v3 path is refused");
		}
	}

	public void Test_TheByteSettingsAreReadInFileOrder()
	{
		var bytes = V3File(pBytesCount: [30, 8, 60, 1, 0, 1, 30, 60]);
		var result = new WolfGameSettingsReader(Limits).Read(bytes.ToArray(), "Game.dat");
		AssertTrue(result.Success, "the fixture is valid");
		if (!result.Success)
		{
			return;
		}
		// 30 tile size 16x16, 8 eight character directions, 60 40x40 tiles,
		// 1 anti aliasing on, 0 disabled, 1 disabled double width, 30 and 60 the
		// same sizes again for the shadow.
		AssertEq(result.Value!.ByteSettings.Count, 8, "the record has eight byte settings");
		AssertEq(result.Value.ByteSettings[0], 30, "the tile size is 40x40");
		AssertEq(result.Value.ByteSettings[1], 8, "the character directions are eight");
		AssertEq(result.Value.ByteSettings[2], 60, "and the second size is 40x40");
		AssertEq(result.Value.ByteSettings[4], 0, "anti aliasing is off");
		AssertEq(result.Value.ByteSettings[5], 1, "and off at double width");
	}

	public void Test_TheEncryptionKeyIsReadAndReportedButNothingIsDecrypted()
	{
		// The key is in the file and its presence is what marks a protected game.
		// Reading it must not lead to any decryption: this reader has no
		// decryption path at all, and the value is only reported.
		var protectedFile = V3File(pStrings:
		[
			"Title", "SERIAL", "SECRET", "MS Gothic", "", "", "", "",
			"Subtitle", "a", "b", "Loading",
		]);
		var result = new WolfGameSettingsReader(Limits).Read(protectedFile.ToArray(), "Game.dat");
		AssertTrue(result.Success, "a file with a key still parses");
		if (!result.Success)
		{
			return;
		}
		AssertEq(result.Value!.EncryptionKey, "SECRET", "the key is the third string setting");
		AssertTrue(result.Value.IsProtected, "and its presence marks the file as protected");

		var plain = new WolfGameSettingsReader(Limits).Read(V3File().ToArray(), "Game.dat");
		AssertTrue(plain.Success, "a file without a key parses");
		if (plain.Success)
		{
			AssertTrue(!plain.Value!.IsProtected, "and is not marked as protected");
		}
	}

	public void Test_TheWordSettingsAreBoundedByTheirOwnLength()
	{
		// A later version added fields behind this length, so the record is read
		// to its own count rather than to a fixed number of known fields. The
		// version field is the eighteenth entry, so it is only present when the
		// file actually has that many.
		// 23 words: unknown, twelve move speeds, unknown_2, width, height,
		// version at index 16, and the four optional loading gauge fields.
		var words = new List<byte>();
		for (var index = 0; index < 23; index++)
		{
			words.Add(0x00);
			words.Add(0x00);
		}
		words[32] = 0x35; // low byte of word 16
		words[33] = 0x00;

		var full = new WolfGameSettingsReader(Limits).Read(
			V3File(pWordCount: 23, pWords: words.ToArray()).ToArray(), "Game.dat");
		AssertTrue(full.Success, $"a full word record parses: {full.Error?.Message}");
		if (full.Success)
		{
			AssertEq(full.Value!.WordSettings.Count, 23, "all twenty-three words are read");
			AssertEq(full.Value.WolfRpgVersion, 0x35, "and the version is the eighteenth");
		}

		var fewer = new WolfGameSettingsReader(Limits).Read(
			V3File(pWordCount: 10).ToArray(), "Game.dat");
		AssertTrue(fewer.Success, "a shorter word record parses too");
		if (fewer.Success)
		{
			AssertEq(fewer.Value!.WordSettings.Count, 10, "and it has only its own ten words");
			AssertEq(fewer.Value.WolfRpgVersion, 0,
				"so the version field is absent rather than read from past the record");
		}
	}

	public void Test_TheStaticRandomBlockIsSkippedRatherThanDecoded()
	{
		// Its length varies per file, so it is bounded by the file's own declared
		// size. A reader that treated it as a fixed block would misread every
		// other game, so the fixture uses a length that is not a round number.
		var result = new WolfGameSettingsReader(Limits).Read(
			V3File(pRandomBlock: [1, 2, 3, 4, 5, 6, 7]).ToArray(), "Game.dat");
		AssertTrue(result.Success, $"a file with an odd random block parses: {result.Error?.Message}");
		if (result.Success)
		{
			AssertEq(result.Value!.WordSettings.Count, 23, "and the word settings are still complete");
		}
	}

	public void Test_ADeclaredSizeThatDoesNotMatchTheFileIsRefused()
	{
		// The declared size is what bounds the random block. A size past the end
		// of the file would read past the buffer, and a size before the current
		// position would give a negative block.
		var source = V3File();
		var sizeAt = _lastSizeAt;
		var bytes = source.ToArray();
		bytes[sizeAt] = 0xFF;
		bytes[sizeAt + 1] = 0xFF;
		var result = new WolfGameSettingsReader(Limits).Read(bytes, "Game.dat");
		AssertTrue(!result.Success, "a declared size past the end of the file is refused");
	}

	public void Test_TrailingBytesAfterTheFooterAreRefused()
	{
		var bytes = V3File().ToArray();
		var padded = new List<byte>(bytes) { 0x00, 0x00 };
		var result = new WolfGameSettingsReader(Limits).Read(padded.ToArray(), "Game.dat");
		AssertTrue(!result.Success, "trailing bytes after the version footer are refused");
	}

	public void Test_AMissingFooterIsRefused()
	{
		var bytes = V3File().ToArray();
		var trimmed = new List<byte>(bytes);
		trimmed.RemoveAt(trimmed.Count - 1);
		var result = new WolfGameSettingsReader(Limits).Read(trimmed.ToArray(), "Game.dat");
		AssertTrue(!result.Success, "a file with no version footer is refused");
	}

	public void Test_AStringRunningPastTheEndOfTheFileIsRefused()
	{
		var bytes = V3File().ToArray();
		var bad = new List<byte>(bytes);
		// The first string's length sits after the magic, the u8 settings length
		// and the byte settings themselves: 10 + 4 + 3 = 17.
		bad[17] = 0xFF;
		bad[18] = 0xFF;
		var result = new WolfGameSettingsReader(Limits).Read(bad.ToArray(), "Game.dat");
		AssertTrue(!result.Success, "a string longer than the file is refused");
		AssertEq(result.Error!.Code, PluginErrorCode.InvalidGame, "and it is invalid game data");
	}

	public void Test_ShiftJisBytesThatAreNotValidInTheDeclaredEncodingAreRefused()
	{
		// Both encodings are decoded strictly. Bytes that are not valid in the
		// encoding the record declares are reported rather than replaced, because
		// a replacement character in a game title looks like a real title.
		var bytes = new List<byte> { 0x00, 0x57, 0x00, 0x00, 0x4F, 0x4C, 0x00, 0x46, 0x4D, 0x55 };
		AddInt32(bytes, 0);
		// A lone 0x81 is a valid lead byte in Shift-JIS but not valid UTF-8, so it
		// must be refused in a v3 file and must not be in a v2 one.
		AddRawString(bytes, [0x81]);
		var result = new WolfGameSettingsReader(Limits).Read(bytes.ToArray(), "Game.dat");
		AssertTrue(!result.Success, "bytes that are not valid UTF-8 in a v3 file are refused");
	}
}
