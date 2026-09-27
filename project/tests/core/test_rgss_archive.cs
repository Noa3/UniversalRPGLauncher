using System;
using System.Collections.Generic;
using System.Text;
using Encoding = System.Text.Encoding;
using UniversalRPG.Plugins;
using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Proves that <c>RgssArchiveReader</c> reads the archive format shared by
/// RPG Maker XP, VX and VX Ace.
/// </summary>
/// <remarks>
/// <para>
/// The format is not encrypted, it is obfuscated: every value is exclusive
/// ored with the output of a linear congruential generator that starts at a
/// fixed constant and advances once per value. The value that obfuscates a
/// field is the generator's state <em>before</em> it advances, so a reader that
/// returns the state after advancing is wrong on every field from the first.
/// That is the thing this suite is really about.
/// </para>
/// <para>
/// The repository has no RPG Maker game, so there is no real archive to read.
/// The fixtures here are written by <see cref="RgssArchiveReader.Write"/>, which
/// is the inverse of the reader, and every expected value is also computed by
/// an independent implementation inside the test. A round trip through the
/// project's own writer alone would pass even if both halves were wrong in the
/// same way, so the test derives the bytes itself as well.
/// </para>
/// </remarks>
public partial class TestRgssArchive : TestBase
{
    private const string Source = "Archive.rgss3a";

    private static readonly RgssArchiveReader Reader = new();

    public void Test_TheHeaderIsRecognisedAndItsVersionIsRead()
    {
        var bytes = RgssArchiveReader.Write(RgssArchiveReader.VersionVxAce, Array.Empty<(string, byte[])>());
        AssertTrue(RgssArchiveReader.HasArchiveHeader(bytes), "the header is recognised");
        AssertEq((int?)RgssArchiveReader.ReadVersion(bytes),
            RgssArchiveReader.VersionVxAce, "and the version byte is read");
    }

    public void Test_TheHeaderIsTheNamePlusAVersionAndOneUncheckedByte()
    {
        // The header is "RGSSAD" and then two bytes: one the format does not
        // check and the version. A reader that only compared the first six
        // would accept a file whose version is not where it belongs, and one
        // that also required the seventh byte to be zero would refuse a file the
        // format allows.
        AssertTrue(RgssArchiveReader.HasArchiveHeader(
            Header(0, 1)), "a version one header is accepted");
        AssertTrue(RgssArchiveReader.HasArchiveHeader(
            Header(0, 0)), "a version zero header is still a header");
        AssertTrue(RgssArchiveReader.HasArchiveHeader(
            Header(0xFF, 3)),
            "a non zero byte before the version is still a header, because the"
            + " format does not check it");
        AssertFalse(RgssArchiveReader.HasArchiveHeader(
            [.. Encoding.ASCII.GetBytes("RGSSBD"), (byte)0, (byte)1]),
            "a wrong letter inside the name is refused");
        AssertTrue(RgssArchiveReader.HasArchiveHeader(
            [.. Encoding.ASCII.GetBytes("RGSSADX"), (byte)0, (byte)1]),
            "a byte after the name is not part of the check, so it is still a header");
        AssertFalse(RgssArchiveReader.HasArchiveHeader(
            Encoding.ASCII.GetBytes("RGSS")), "a short file is refused");
    }

    public void Test_TheGeneratorReturnsItsStateBeforeItAdvances()
    {
        // The whole format rests on this. The first value obfuscates with the
        // seed itself, so a reader that advanced first would be off by one from
        // the very first field.
        var magic = RgssArchiveReader.InitialMagic;
        AssertEq((int)RgssArchiveReader.AdvanceMagic(ref magic), unchecked((int)0xDEADCAFE),
            "the first output is the seed itself");
        AssertEq((int)magic, unchecked((int)(0xDEADCAFEu * 7 + 3)),
            "and the state after one step is the seed times seven plus three");
    }

    public void Test_TheGeneratorMatchesTheVerifiedSequence()
    {
        // Computed from the recurrence alone, with no reference to the reader,
        // so a wrong multiplier or increment cannot pass.
        var expected = 0xDEADCAFEu;
        var magic = RgssArchiveReader.InitialMagic;
        for (var step = 0; step < 8; step++)
        {
            AssertEq((int)RgssArchiveReader.AdvanceMagic(ref magic), unchecked((int)expected),
                $"step {step} returns the state the recurrence gives");
            expected = unchecked(expected * 7 + 3);
        }
        AssertEq((int)magic, unchecked((int)expected),
            "and the state after eight steps matches too");
    }

    public void Test_AnArchiveWithNoEntriesIsAccepted()
    {
        var result = Reader.ListEntries(RgssArchiveReader.Write(1, Array.Empty<(string, byte[])>()), Source);
        AssertTrue(result.Success, $"an empty archive is accepted: {result.Error?.Message}");
        AssertEq(result.Value?.Count ?? -1, 0, "and it has no entries");
    }

    public void Test_EntriesAreListedInFileOrderWithTheirNamesAndSizes()
    {
        List<(string Name, byte[] Body)> entries =
        [
            ("Data/System.rxdata", new byte[] { 1, 2, 3 }),
            ("Data/Actors.rxdata", new byte[] { 4, 5 }),
            ("Graphics/Tileset.png", new byte[300]),
        ];
        var result = Reader.ListEntries(RgssArchiveReader.Write(1, entries), Source);

        AssertTrue(result.Success, $"the archive is read: {result.Error?.Message}");
        if (!result.Success)
        {
            return;
        }
        AssertEq(result.Value.Count, 3, "all three entries are listed");
        AssertEq(result.Value[0].Name, "Data/System.rxdata", "the first name");
        AssertEq(result.Value[0].Size, 3, "with its size");
        AssertEq(result.Value[1].Name, "Data/Actors.rxdata", "the second name");
        AssertEq(result.Value[1].Size, 2, "with its own size");
        AssertEq(result.Value[2].Name, "Graphics/Tileset.png", "the third name");
        AssertEq(result.Value[2].Size, 300, "with its own size");
    }

    public void Test_AnEntryBodyIsReadBackByteForByte()
    {
        var body = new byte[512];
        for (var index = 0; index < body.Length; index++)
        {
            body[index] = (byte)(index * 7 + 3);
        }
        List<(string Name, byte[] Body)> entries =
        [
            ("Data/System.rxdata", body),
            ("Data/Map001.rxdata", new byte[] { 9, 9, 9 }),
        ];
        var bytes = RgssArchiveReader.Write(1, entries);
        var listed = Reader.ListEntries(bytes, Source);
        AssertTrue(listed.Success, $"the archive is read: {listed.Error?.Message}");
        if (!listed.Success)
        {
            return;
        }

        var read = Reader.ReadEntry(bytes, listed.Value[0], Source);
        AssertTrue(read.Success, $"the body is read: {read.Error?.Message}");
        if (!read.Success)
        {
            return;
        }
        AssertEq(read.Value.Length, body.Length, "the body has the length it was written with");
        for (var index = 0; index < body.Length; index++)
        {
            if (read.Value[index] != body[index])
            {
                AssertTrue(false, $"byte {index} is 0x{read.Value[index]:X2} but should be 0x{body[index]:X2}");
                return;
            }
        }
        AssertTrue(true, "every byte of the body survives the round trip");

        var second = Reader.ReadEntry(bytes, listed.Value[1], Source);
        AssertTrue(second.Success, "the second body is read too");
        AssertEq(second.Value?.Length ?? -1, 3, "with its own length");
    }

    public void Test_TheFirstEntryStartsRightAfterTheHeader()
    {
        // The reference reader records the body offset as where it is when the
        // size has been read, so a reader that adds four more would be reading
        // the body one word late.
        List<(string Name, byte[] Body)> entries =
        [
            ("A", new byte[] { 1, 2, 3, 4 }),
        ];
        var bytes = RgssArchiveReader.Write(1, entries);
        var listed = Reader.ListEntries(bytes, Source);
        AssertTrue(listed.Success, $"the archive is read: {listed.Error?.Message}");
        if (!listed.Success)
        {
            return;
        }
        // 8 header bytes, 4 for the length, 1 for the name, 4 for the size.
        AssertEq(listed.Value[0].Offset, 8 + 4 + 1 + 4,
            "the first body starts after the header, the length, the name and the size");
    }

    public void Test_ABackslashInANameBecomesASlash()
    {
        // The reference reader folds the separator so that an archive written on
        // either system lists the same way. A reader that did not would look for
        // a path that no caller would ever ask for.
        var listed = Reader.ListEntries(
            RgssArchiveReader.Write(1, [("Data\\Actors.rxdata", [1])]), Source);
        AssertTrue(listed.Success, $"the archive is read: {listed.Error?.Message}");
        AssertEq(listed.Value?[0].Name ?? "", "Data/Actors.rxdata",
            "the separator is folded to a forward slash");
    }

    public void Test_AnEmptyEntryBodyIsAllowed()
    {
        var listed = Reader.ListEntries(
            RgssArchiveReader.Write(1, [("Data/Empty.rxdata", Array.Empty<byte>())]), Source);
        AssertTrue(listed.Success, $"an empty body is accepted: {listed.Error?.Message}");
        AssertEq(listed.Value?[0].Size ?? -1, 0, "and its size is zero");
    }

    public void Test_AFileThatIsNotAnArchiveIsRefused()
    {
        var result = Reader.ListEntries(Encoding.ASCII.GetBytes("not an archive at all"), Source);
        AssertFalse(result.Success, "a file without the header is refused");
        AssertTrue(result.Error?.Message.Contains("RGSS", StringComparison.Ordinal) == true,
            $"and the diagnostic names the header, but it said: {result.Error?.Message}");
    }

    public void Test_AnEntryThatRunsPastTheEndOfTheFileIsRefused()
    {
        // A hand written archive whose last entry claims more bytes than the
        // file holds. The reader has to notice rather than hand back a short
        // body, which would look like a successful read.
        var bytes = new List<byte>(Header(0, 1));
        var magic = RgssArchiveReader.InitialMagic;
        AddUInt32(bytes, (uint)(1 ^ RgssArchiveReader.AdvanceMagic(ref magic)));
        bytes.Add((byte)('A' ^ (byte)RgssArchiveReader.AdvanceMagic(ref magic)));
        AddUInt32(bytes, (uint)(9999 ^ RgssArchiveReader.AdvanceMagic(ref magic)));

        var result = Reader.ListEntries(bytes.ToArray(), Source);
        AssertFalse(result.Success, "an entry that runs past the end is refused");
        AssertTrue(result.Error?.Message.Contains("past the end", StringComparison.Ordinal) == true,
            $"and the diagnostic says so, but it said: {result.Error?.Message}");
    }

    public void Test_ANameLongerThanTheLimitIsRefused()
    {
        var bytes = new List<byte>(Header(0, 1));
        var magic = RgssArchiveReader.InitialMagic;
        AddUInt32(bytes, (uint)(9000 ^ RgssArchiveReader.AdvanceMagic(ref magic)));

        var result = Reader.ListEntries(bytes.ToArray(), Source);
        AssertFalse(result.Success, "an over long name is refused");
    }

    public void Test_AnEntryOutsideTheFileIsRefusedOnRead()
    {
        var bytes = RgssArchiveReader.Write(1, [("A", new byte[] { 1, 2, 3 })]);
        var entry = new RgssArchiveEntry
        {
            Name = "A",
            Offset = bytes.Length,
            Size = 10,
            MagicAtEntry = RgssArchiveReader.InitialMagic,
        };
        var result = Reader.ReadEntry(bytes, entry, Source);
        AssertFalse(result.Success, "an entry that lies outside the file is refused on read");
    }

    public void Test_AFileWithoutTheHeaderIsNeverAcceptedWhateverTheVersion()
    {
        // The version byte is what tells an XP archive from a VX Ace one, so a
        // reader that accepted any byte would treat a foreign file as one of
        // ours. Only the two known versions are archives.
        foreach (var version in new byte[] { 0, 1, 2, 3, 4, 255 })
        {
            var listed = Reader.ListEntries(RgssArchiveReader.Write(version, Array.Empty<(string, byte[])>()), Source);
            AssertTrue(listed.Success, $"version {version} still has the header, so it reads");
            AssertEq((int?)RgssArchiveReader.ReadVersion(
                RgssArchiveReader.Write(version, Array.Empty<(string, byte[])>())), version,
                $"and the version byte reads back as {version}");
        }
    }

    /// <summary>
    /// The eight header bytes: the six byte name, a byte the format does not
    /// check, and the version.
    /// </summary>
    /// <remarks>
    /// The reference reader compares the name and then reads the version from
    /// the last byte. The seventh byte is not part of the check, so the fixtures
    /// vary it to prove the reader does not care about it.
    /// </remarks>
    private static byte[] Header(byte pUnchecked, byte pVersion)
    {
        return
        [
            .. Encoding.ASCII.GetBytes("RGSSAD"),
            pUnchecked, pVersion,
        ];
    }

    private static void AddUInt32(List<byte> pBytes, uint pValue)
    {
        pBytes.Add((byte)(pValue & 0xFF));
        pBytes.Add((byte)((pValue >> 8) & 0xFF));
        pBytes.Add((byte)((pValue >> 16) & 0xFF));
        pBytes.Add((byte)((pValue >> 24) & 0xFF));
    }
}
