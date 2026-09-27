using System.Collections.Generic;
using System.IO;
using UniversalRPG.Plugins;
using UniversalRPG.Tests.Framework;
using UniversalRPG.Wolf;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Proves that <c>WolfBinaryCommonEventReader</c> reads the WOLF
/// <c>CommonEvent.dat</c> format as specified.
/// </summary>
/// <remarks>
/// <para>
/// The fixtures here are byte sequences built from the format specification.
/// They are not taken from a real WOLF game, because the repository has no WOLF
/// game to take them from. That bounds what these tests can claim: they prove
/// the reader agrees with the specification, not that the specification matches
/// a shipped game.
/// </para>
/// <para>
/// The record layout is the thing under test. Its argument name table, its
/// option tables and its fixed self variable name table sit between separator
/// bytes, so a reader that walks the fields top to bottom lands on plausible
/// wrong values instead of failing. Each of those positions therefore has a test
/// that puts a recognisable value there.
/// </para>
/// </remarks>
public partial class TestWolfBinaryCommonEvent : TestBase
{
    private const string Source = "CommonEvent.dat";


    // Fixtures are built from the specification, byte by byte, so a wrong
    // expectation shows up as a failure rather than as a reader that agrees
    // with a wrong idea of the format.

    private static void AddInt32(List<byte> pBytes, int pValue)
    {
        pBytes.Add((byte)(pValue & 0xFF));
        pBytes.Add((byte)((pValue >> 8) & 0xFF));
        pBytes.Add((byte)((pValue >> 16) & 0xFF));
        pBytes.Add((byte)((pValue >> 24) & 0xFF));
    }

    /// <summary>
    /// Writes a length prefixed string: the byte count, then the bytes.
    /// </summary>
    /// <remarks>
    /// The terminator is part of the count, because the format's string is a
    /// zero terminated string with a size and the size covers the terminator.
    /// Writing the count without the terminator and then adding one leaves a
    /// byte in the buffer that the reader does not consume, which shifts every
    /// field after the first string.
    /// </remarks>
    private static void AddString(List<byte> pBytes, string pText)
    {
        var raw = System.Text.Encoding.GetEncoding(932).GetBytes(pText);
        AddInt32(pBytes, raw.Length + 1);
        pBytes.AddRange(raw);
        pBytes.Add(0x00);
    }

    private static byte[] FileHeader(byte pVersionHeader = 0x00, byte pFooter = 0x91)
    {
        // magic, version header, 'F' 'C' 0, then the version byte. The count
        // starts after that, so a header that omits the version byte makes the
        // reader read the count's first byte as the version.
        var bytes = new List<byte>
        {
            0x00, 0x57, 0x00, 0x00, 0x4F, 0x4C, pVersionHeader, 0x46, 0x43, 0x00, 0x01,
        };
        _ = pFooter;
        return bytes.ToArray();
    }

    /// <summary>
    /// A complete single event file. Each argument keeps its own recognisable
    /// value so a reader that reads the wrong field produces a wrong value that
    /// a test can see.
    /// </summary>
    private byte[] File(
        int pEventCount = 1,
        int pId = 1,
        int pConditionOperator = 2,
        int pRunCondition = 1,
        uint pConditionVariable = 0x1234_5678,
        int pConditionValue = 99,
        int pArgumentNumberCount = 2,
        int pArgumentStringCount = 1,
        string pTitle = "Common One",
        int pCommandCount = 0,
        string pMemo = "an editor note",
        string[] pArgumentNames = null,
        int pArgumentDefaultCount = 2,
        int[] pArgumentDefaults = null,
        int pColor = 3,
        string pReturnName = "Result",
        int pReturnValueId = 42,
        byte pUnknown5 = 0x01,
        string[] pSelfNames = null,
        byte pFirstSeparator = 0x8F,
        byte pSecondSeparator = 0x90,
        byte pThirdSeparator = 0x91,
        byte pFourthSeparator = 0x92,
        byte pFifthSeparator = 0x92,
        byte pFooter = 0x91,
        int pTrailingBytes = 0)
    {
        pArgumentNames ??= ["first", "second", "third"];
        pArgumentDefaults ??= [-7, 7];
        pSelfNames ??= [];

        var bytes = new List<byte>(FileHeader());
        AddInt32(bytes, pEventCount);

        for (var index = 0; index < pEventCount; index++)
        {
            var eventId = pEventCount == 1 ? pId : index + 1;
            var eventTitle = pEventCount == 1 ? pTitle : $"{pTitle} {eventId}";

            bytes.Add(0x8E);
            AddInt32(bytes, eventId);
            AddInt32(bytes, pConditionOperator);
            AddInt32(bytes, pRunCondition);
            AddInt32(bytes, unchecked((int)pConditionVariable));
            AddInt32(bytes, pConditionValue);
            bytes.Add((byte)pArgumentNumberCount);
            bytes.Add((byte)pArgumentStringCount);
            AddString(bytes, eventTitle);
            AddInt32(bytes, pCommandCount);
            for (var command = 0; command < pCommandCount; command++)
            {
                // A message command: the parameter count, the four byte type,
                // then the shared tail of a branch depth, a string count, the
                // string and the route flag. A zero byte here would be read as
                // the list's end marker instead.
                bytes.Add(0x01);
                bytes.Add(101);
                bytes.Add(0x00);
                bytes.Add(0x00);
                bytes.Add(0x00);
                bytes.Add(0x00); // branch depth
                bytes.Add(0x00); // string count
                bytes.Add(0x00); // no route
            }
            AddInt32(bytes, 1);
            bytes.Add(0x00);
            AddString(bytes, pMemo);

            bytes.Add(pFirstSeparator);
            AddInt32(bytes, pArgumentNames.Length);
            foreach (var name in pArgumentNames)
            {
                AddString(bytes, name);
            }

            // No option pages: a zero count for the mode pages, one for the
            // option string tables and one for the option value tables.
            AddInt32(bytes, 0);
            AddInt32(bytes, 0);
            AddInt32(bytes, 0);

            AddInt32(bytes, pArgumentDefaultCount);
            foreach (var value in pArgumentDefaults)
            {
                AddInt32(bytes, value);
            }

            bytes.Add(pSecondSeparator);
            AddInt32(bytes, pColor);

            // One hundred names. The fixture fills the first with a recognisable
            // value and leaves the rest empty, so a reader that treats the table
            // as a counted one stops early and the following fields are wrong.
            for (var name = 0; name < WolfBinaryCommonEventReader.SelfVariableNameCount; name++)
            {
                AddString(bytes, name == 0 ? "self-zero" : "");
            }

            bytes.Add(pThirdSeparator);
            AddInt32(bytes, pUnknown5);
            bytes.Add(0x00);

            bytes.Add(pFourthSeparator);
            AddString(bytes, pReturnName);
            AddInt32(bytes, pReturnValueId);
            bytes.Add(pFifthSeparator);
        }

        bytes.Add(pFooter);
        for (var extra = 0; extra < pTrailingBytes; extra++)
        {
            bytes.Add(0x00);
        }
        return bytes.ToArray();
    }

    private static readonly WolfParseLimits Limits = new();

    private PluginResult<IReadOnlyList<WolfBinaryCommonEvent>> Read(byte[] pBytes)
    {
        return new WolfBinaryCommonEventReader(Limits).Read(pBytes, Source);
    }

    public void Test_a_file_with_a_valid_header_is_accepted()
    {
        var result = Read(File());
        AssertTrue(result.Success, $"the fixture parses: {result.Error?.Message}");
        AssertEq(result.Value?.Count ?? -1, 1, "and it holds the one event it declares");
        AssertTrue(WolfBinaryCommonEventReader.HasCommonEventHeader(File()),
            "the header predicate agrees");
    }

    public void Test_a_file_without_the_magic_is_refused()
    {
        var bytes = File();
        bytes[1] = 0x58;
        var result = Read(bytes);
        AssertFalse(result.Success, "a wrong magic is refused");
        AssertFalse(WolfBinaryCommonEventReader.HasCommonEventHeader(bytes),
            "and the header predicate rejects it too");
    }

    public void Test_a_file_with_an_unknown_version_header_is_refused()
    {
        // The version header byte selects the record shape, so an unknown value
        // has to be refused rather than read as one of the known shapes.
        var result = Read(FileHeader(0x7A));
        AssertFalse(result.Success, "an unknown version header is refused");
    }

    public void Test_a_magic_with_the_wrong_second_marker_is_refused()
    {
        var bytes = File();
        bytes[7] = 0x44; // 'D' instead of 'F'
        var result = Read(bytes);
        AssertFalse(result.Success, "a right magic with a wrong FC marker is refused");
    }

    public void Test_the_event_count_is_read_and_the_events_come_back_in_file_order()
    {
        var result = Read(File(pEventCount: 3));
        AssertTrue(result.Success, $"three events parse: {result.Error?.Message}");
        if (!result.Success)
        {
            return;
        }
        AssertEq(result.Value.Count, 3, "all three are read");
        for (var index = 0; index < result.Value.Count; index++)
        {
            AssertEq(result.Value[index].Id, index + 1, $"event {index} has the id the file gave it");
            AssertEq(result.Value[index].Title, $"Common One {index + 1}",
                $"event {index} has its own title, so the reader is not reusing one record");
        }
    }

    public void Test_a_record_that_does_not_start_with_the_header_byte_is_refused()
    {
        var bytes = File();
        // The record header is the byte right after the event count, which is
        // eleven header bytes plus the four byte count.
        bytes[15] = 0x8F;
        var result = Read(bytes);
        AssertFalse(result.Success, "a record that does not begin with 0x8E is refused");
    }

    public void Test_a_record_whose_first_separator_is_wrong_is_refused()
    {
        var result = Read(File(pFirstSeparator: 0x8E));
        AssertFalse(result.Success, "a wrong first separator is refused");
    }

    public void Test_the_run_condition_and_its_variable_are_read()
    {
        var result = Read(File(
            pConditionOperator: 1,
            pRunCondition: 2,
            pConditionVariable: 0xABCD_EF01,
            pConditionValue: -5,
            pArgumentNumberCount: 3,
            pArgumentStringCount: 4));
        AssertTrue(result.Success, $"the fixture parses: {result.Error?.Message}");
        if (!result.Success)
        {
            return;
        }
        var commonEvent = result.Value[0];
        AssertEq(commonEvent.ConditionOperator, 1, "the operator is read");
        AssertEq(commonEvent.RunCondition, 2, "the run condition is read");
        AssertTrue(commonEvent.IsKnownRunCondition, "and it is one the format defines");
        AssertTrue(commonEvent.ConditionVariable == 0xABCD_EF01u,
            $"the condition variable keeps its full value, got 0x{commonEvent.ConditionVariable:X8}");
        AssertEq(commonEvent.ConditionValue, -5, "a negative condition value is read as signed");
        AssertEq(commonEvent.ArgumentNumberCount, 3, "the number argument count is read");
        AssertEq(commonEvent.ArgumentStringCount, 4, "the string argument count is read");
    }

    public void Test_the_argument_name_table_is_read_from_between_the_separators()
    {
        // The name table sits after the 0x8F separator, not at the end of the
        // record. A reader that collects the fields top to bottom reads the
        // memo and the defaults here instead, and both are plausible.
        var result = Read(File(pArgumentNames: ["alpha", "beta", "gamma", "delta"]));
        AssertTrue(result.Success, $"the fixture parses: {result.Error?.Message}");
        if (!result.Success)
        {
            return;
        }
        var names = result.Value[0].ArgumentNames;
        AssertEq(names.Count, 4, "all four names are read");
        AssertEq(names[0], "alpha", "the first name is the first name");
        AssertEq(names[3], "delta", "and the last one is the last one");
    }

    public void Test_the_self_variable_table_is_a_fixed_hundred_entries_not_a_count()
    {
        var result = Read(File());
        AssertTrue(result.Success, $"the fixture parses: {result.Error?.Message}");
        if (!result.Success)
        {
            return;
        }
        var selfNames = result.Value[0].SelfVariableNames;
        AssertEq(selfNames.Count, WolfBinaryCommonEventReader.SelfVariableNameCount,
            "the table has a hundred entries, whatever the file holds");
        AssertEq(selfNames[0], "self-zero", "the first entry is read");
        AssertEq(selfNames[99], "", "and the hundredth is read too");

        // If the table were read as a counted one the cursor would stop inside
        // it, and the return name and value id that follow would be wrong.
        AssertEq(result.Value[0].ReturnName, "Result",
            "so the cursor is still on the return name after the table");
        AssertEq(result.Value[0].ReturnValueId, 42, "and the return value id is intact");
    }

    public void Test_the_return_name_and_value_id_are_read_from_the_last_separators()
    {
        var result = Read(File(pReturnName: "CallMe", pReturnValueId: -7, pColor: 5));
        AssertTrue(result.Success, $"the fixture parses: {result.Error?.Message}");
        if (!result.Success)
        {
            return;
        }
        var commonEvent = result.Value[0];
        AssertEq(commonEvent.ReturnName, "CallMe", "the return name is read");
        AssertEq(commonEvent.ReturnValueId, -7, "a negative return value id is read as signed");
        AssertEq(commonEvent.Color, 5, "the colour is read");
        AssertTrue(WolfCommonEventColor.IsKnown(commonEvent.Color), "and it is a colour the format defines");
    }

    public void Test_the_number_argument_defaults_are_read_as_a_signed_array()
    {
        var result = Read(File(pArgumentDefaultCount: 3, pArgumentDefaults: [-2147483648, 0, 2147483647]));
        AssertTrue(result.Success, $"the fixture parses: {result.Error?.Message}");
        if (!result.Success)
        {
            return;
        }
        var defaults = result.Value[0].ArgumentNumberDefaults;
        AssertEq(defaults.Count, 3, "all three defaults are read");
        AssertEq(defaults[0], -2147483648, "the smallest int survives a round trip");
        AssertEq(defaults[1], 0, "zero is zero");
        AssertEq(defaults[2], 2147483647, "and the largest int survives too");
    }

    public void Test_the_commands_are_read_with_the_shared_command_reader()
    {
        // The command list has to be read by the same code a map uses, so a
        // common event's commands and a map event's commands cannot drift apart.
        var result = Read(File(pCommandCount: 3));
        AssertTrue(result.Success, $"a file with commands parses: {result.Error?.Message}");
        if (!result.Success)
        {
            return;
        }
        AssertEq(result.Value[0].Commands.Count, 3,
            "the declared command count is the number of commands read");
    }

    public void Test_an_unknown4_block_other_than_one_is_refused()
    {
        // The fixed five byte block after the commands is a one, not a count of
        // something. Reading it as a free value would let a corrupt file through
        // and the following memo would be garbage.
        var result = Read(File(pUnknown5: 0x02));
        AssertFalse(result.Success, "an unknown5 block that is not one is refused");
    }

    public void Test_a_count_larger_than_the_limit_is_refused()
    {
        var header = new List<byte>(FileHeader());
        AddInt32(header, int.MaxValue);
        var result = Read(header.ToArray());
        AssertFalse(result.Success, "a count of two billion events is refused");
    }

    public void Test_a_file_with_trailing_bytes_after_the_footer_is_refused()
    {
        // The count has to describe the whole file. Bytes after the footer mean
        // the reader stopped early, and accepting them would hide that.
        var result = Read(File(pTrailingBytes: 4));
        AssertFalse(result.Success, "trailing bytes after the footer are refused");
    }

    public void Test_a_footer_that_is_not_a_version_byte_is_refused()
    {
        AssertTrue(Read(File(pFooter: 0x8F)).Success, "footer 0x8F is accepted");
        AssertTrue(Read(File(pFooter: 0x90)).Success, "footer 0x90 is accepted");
        AssertTrue(Read(File(pFooter: 0x91)).Success, "footer 0x91 is accepted");
        AssertFalse(Read(File(pFooter: 0x00)).Success, "a zero footer is refused");
        AssertFalse(Read(File(pFooter: 0x42)).Success, "an arbitrary byte is refused");
    }
}
