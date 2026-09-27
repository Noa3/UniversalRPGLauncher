using System;
using System.Collections.Generic;
using System.IO;
using UniversalRPG.Plugins;

namespace UniversalRPG.Wolf;

/// <summary>
/// Reads the WOLF <c>CommonEvent.dat</c> binary format as data only.
/// </summary>
/// <remarks>
/// <para>
/// The file is a header, a count, and then that many records. Each record is
/// introduced by a <c>0x8E</c> header byte and is divided by five separator
/// bytes into six parts, and the order matters: the argument name table, the
/// option strings and the option values all sit between separators rather than
/// being stored as a block at the end. A reader that reads the record top to
/// bottom without honouring the separators produces plausible wrong values,
/// because every field is still a valid length prefixed string.
/// </para>
/// <para>
/// The self variable name table is a fixed hundred entries, not a counted one,
/// which is the other place a count that looks optional is actually mandatory.
/// Reading it as a count would leave the cursor inside the table and shift
/// everything after it.
/// </para>
/// <para>
/// The command list is decoded with the shared command reader, so a command in
/// a common event is read by the same code as a command on a map.
/// </para>
/// <para>
/// This reader is structural only and is validated against byte sequences built
/// from the specification, not against a real WOLF game.
/// </para>
/// </remarks>
public sealed class WolfBinaryCommonEventReader
{
    /// <summary>The leading magic, <c>0 'W' 0 0 'O' 'L'</c>.</summary>
    private static readonly byte[] FileMagic = [0x00, 0x57, 0x00, 0x00, 0x4F, 0x4C];

    /// <summary>The marker after the version header, <c>'F' 'C' 0</c>.</summary>
    private static readonly byte[] MagicTwo = [0x46, 0x43, 0x00];

    /// <summary>The version header byte for v2.</summary>
    private const byte VersionHeaderV2 = 0x00;

    /// <summary>The version header byte for v3.</summary>
    private const byte VersionHeaderV3 = 0x55;

    /// <summary>The byte that starts every common event record.</summary>
    public const byte RecordHeader = 0x8E;

    /// <summary>The fixed count of internal variable names a record carries.</summary>
    public const int SelfVariableNameCount = 100;

    private readonly WolfParseLimits _limits;

    public WolfBinaryCommonEventReader(WolfParseLimits pLimits)
    {
        _limits = pLimits ?? throw new ArgumentNullException(nameof(pLimits));
    }

    /// <summary>True when the payload carries the verified CommonEvent.dat header.</summary>
    public static bool HasCommonEventHeader(byte[] pBytes)
    {
        if (pBytes == null || pBytes.Length < 6 + 1 + 3 + 1 + 4 + 1)
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
        var versionHeader = pBytes[FileMagic.Length];
        if (versionHeader is not (VersionHeaderV2 or VersionHeaderV3))
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

    /// <summary>Reads a CommonEvent.dat file.</summary>
    public PluginResult<IReadOnlyList<WolfBinaryCommonEvent>> Read(
        byte[] pBytes, string pSourceName)
    {
        if (pBytes == null)
        {
            throw new ArgumentNullException(nameof(pBytes));
        }
        if (pBytes.LongLength > _limits.MaxFileBytes)
        {
            return Fail<IReadOnlyList<WolfBinaryCommonEvent>>(
                $"WOLF CommonEvent.dat {pSourceName} is {pBytes.LongLength} bytes, over the {_limits.MaxFileBytes} byte limit");
        }
        if (!HasCommonEventHeader(pBytes))
        {
            return Fail<IReadOnlyList<WolfBinaryCommonEvent>>(
                $"WOLF CommonEvent.dat {pSourceName} does not carry the verified WOLF/FC header");
        }

        var cursor = new WolfByteCursor(pBytes, pSourceName);
        cursor.Skip(FileMagic.Length + 1 + MagicTwo.Length + 1);

        var count = cursor.ReadUInt32();
        if (count > (uint)_limits.MaxCommonEvents)
        {
            return Fail<IReadOnlyList<WolfBinaryCommonEvent>>(
                $"WOLF CommonEvent.dat {pSourceName} declares {count} events, over the {_limits.MaxCommonEvents} limit");
        }

        var events = new List<WolfBinaryCommonEvent>((int)count);
        for (var index = 0; index < count; index++)
        {
            try
            {
                var commonEvent = ReadRecord(cursor, pSourceName, (int)index);
                if (commonEvent == null)
                {
                    return Fail<IReadOnlyList<WolfBinaryCommonEvent>>(
                        $"WOLF CommonEvent.dat {pSourceName} record {index} could not be read");
                }
                events.Add(commonEvent);
            }
            catch (InvalidDataException error)
            {
                return Fail<IReadOnlyList<WolfBinaryCommonEvent>>(
                    $"WOLF CommonEvent.dat {pSourceName} record {index}: {error.Message}");
            }
        }

        // The footer is one of three bytes, which is what a finished file ends
        // with. Anything else means the count did not describe the file.
        if (cursor.Position >= pBytes.Length)
        {
            return Fail<IReadOnlyList<WolfBinaryCommonEvent>>(
                $"WOLF CommonEvent.dat {pSourceName} ends before the version footer");
        }
        var footer = pBytes[cursor.Position];
        if (footer is not (0x8F or 0x90 or 0x91))
        {
            return Fail<IReadOnlyList<WolfBinaryCommonEvent>>(
                $"WOLF CommonEvent.dat {pSourceName} has 0x{footer:X2} after {count} events, which is not a version footer");
        }
        cursor.Skip(1);
        if (cursor.Position != pBytes.Length)
        {
            return Fail<IReadOnlyList<WolfBinaryCommonEvent>>(
                $"WOLF CommonEvent.dat {pSourceName} has {pBytes.Length - cursor.Position} bytes after the version footer");
        }

        return PluginResult<IReadOnlyList<WolfBinaryCommonEvent>>.Succeeded(events);
    }

    private static PluginResult<T> Fail<T>(string pMessage)
    {
        return PluginResult<T>.Failed(PluginError.Create(
            PluginErrorCode.InvalidGame,
            pMessage,
            EnginePluginIds.WolfRpg,
            "wolf-common-event"));
    }

    /// <summary>
    /// Reads one common event record, from the <c>0x8E</c> header to the last
    /// separator.
    /// </summary>
    /// <remarks>
    /// The field order is the whole of this record. The argument name table, the
    /// option string tables and the option value tables sit between separator
    /// bytes rather than at the end, so they have to be read where they are
    /// rather than collected and applied later.
    /// </remarks>
    private WolfBinaryCommonEvent ReadRecord(
        WolfByteCursor pCursor, string pSourceName, int pIndex)
    {
        var header = pCursor.ReadByte();
        if (header != RecordHeader)
        {
            throw new InvalidDataException(
                $"has 0x{header:X2} where the record header 0x{RecordHeader:X2} belongs");
        }

        var id = (int)pCursor.ReadUInt32();
        var conditionOperator = (int)pCursor.ReadUInt32();
        var runCondition = (int)pCursor.ReadUInt32();
        var conditionVariable = pCursor.ReadUInt32();
        var conditionValue = (int)pCursor.ReadUInt32();
        var argumentNumberCount = pCursor.ReadByte();
        var argumentStringCount = pCursor.ReadByte();

        var title = pCursor.ReadLengthPrefixedString(_limits.MaxStringBytes);
        var commandCount = (int)pCursor.ReadUInt32();
        if (commandCount < 0 || commandCount > _limits.MaxCommandsPerEvent)
        {
            throw new InvalidDataException(
                $"declares {commandCount} commands, outside 0 to {_limits.MaxCommandsPerEvent}");
        }
        var commands = WolfEventCommandReader.ReadCommands(pCursor, commandCount, _limits);

        // unknown4 is the fixed five byte block 01 00 00 00 00. It is not a u4
        // followed by a byte: the first byte is the value and the other four
        // are zero padding, so reading a u4 here would consume the padding as
        // part of the value.
        ExpectFixedOneBlock(pCursor, "unknown4");

        var memo = pCursor.ReadLengthPrefixedString(_limits.MaxStringBytes);
        ExpectSeparator(pCursor, 0x8F, "the first separator");
        var argumentNames = ReadStringArray(pCursor, _limits);

        return ReadRecordTail(
            pCursor, pSourceName, pIndex, id, conditionOperator, runCondition,
            conditionVariable, conditionValue, argumentNumberCount,
            argumentStringCount, title, commands, memo, argumentNames);
    }

    /// <summary>
    /// Reads the fixed five byte block <c>01 00 00 00 00</c>.
    /// </summary>
    /// <remarks>
    /// The first byte is the value and the other four are padding. Treating it
    /// as a <c>u4</c> plus a byte would read <c>01 00 00 00</c> as a little
    /// endian one, which happens to work, and then swallow the fifth byte as if
    /// it were a separate field, which shifts everything after it by one.
    /// </remarks>
    private static void ExpectFixedOneBlock(WolfByteCursor pCursor, string pWhat)
    {
        var first = pCursor.ReadByte();
        if (first != 0x01)
        {
            throw new InvalidDataException(
                $"has {pWhat} starting 0x{first:X2} where 0x01 belongs");
        }
        for (var index = 0; index < 4; index++)
        {
            var padding = pCursor.ReadByte();
            if (padding != 0x00)
            {
                throw new InvalidDataException(
                    $"has {pWhat} padding 0x{padding:X2} where four zero bytes belong");
            }
        }
    }

    private void ExpectSeparator(WolfByteCursor pCursor, byte pExpected, string pWhat)
    {
        var value = pCursor.ReadByte();
        if (value != pExpected)
        {
            throw new InvalidDataException(
                $"has 0x{value:X2} where {pWhat} 0x{pExpected:X2} belongs");
        }
    }

    /// <summary>A length prefixed string array: a count, then that many strings.</summary>
    private static List<string> ReadStringArray(WolfByteCursor pCursor, WolfParseLimits pLimits)
    {
        var count = (int)pCursor.ReadUInt32();
        if (count < 0 || count > pLimits.MaxDatabaseFieldsPerRecord)
        {
            throw new InvalidDataException(
                $"declares {count} entries, over the {pLimits.MaxDatabaseFieldsPerRecord} limit");
        }
        var values = new List<string>(count);
        for (var index = 0; index < count; index++)
        {
            values.Add(pCursor.ReadLengthPrefixedString(pLimits.MaxStringBytes));
        }
        return values;
    }

    /// <summary>An array of length prefixed string tables, one table per page.</summary>
    private static void ReadStringTableArray(WolfByteCursor pCursor, WolfParseLimits pLimits)
    {
        var pages = (int)pCursor.ReadUInt32();
        if (pages < 0 || pages > pLimits.MaxDatabaseFieldsPerRecord)
        {
            throw new InvalidDataException(
                $"declares {pages} option string pages, over the {pLimits.MaxDatabaseFieldsPerRecord} limit");
        }
        for (var page = 0; page < pages; page++)
        {
            ReadStringArray(pCursor, pLimits);
        }
    }

    /// <summary>
    /// Reads the rest of a record: the option tables, the colour, the fixed self
    /// variable name table and the return value.
    /// </summary>
    /// <remarks>
    /// The self variable name table is a fixed hundred entries, not a counted
    /// one. Reading it as a count would leave the cursor inside the table and
    /// shift every field after it, which is the kind of mistake that produces a
    /// file that looks decoded and is not.
    /// </remarks>
    private WolfBinaryCommonEvent ReadRecordTail(
        WolfByteCursor pCursor, string pSourceName, int pIndex,
        int pId, int pConditionOperator, int pRunCondition,
        uint pConditionVariable, int pConditionValue,
        int pArgumentNumberCount, int pArgumentStringCount,
        string pTitle, IReadOnlyList<WolfBinaryEventCommand> pCommands,
        string pMemo, List<string> pArgumentNames)
    {
        _ = pSourceName;
        _ = pIndex;

        var modePages = (int)pCursor.ReadUInt32();
        if (modePages < 0 || modePages > _limits.MaxDatabaseFieldsPerRecord)
        {
            throw new InvalidDataException(
                $"declares {modePages} option mode pages, over the {_limits.MaxDatabaseFieldsPerRecord} limit");
        }
        for (var page = 0; page < modePages; page++)
        {
            _ = pCursor.ReadByte();
        }
        ReadStringTableArray(pCursor, _limits);
        ReadNumberTableArray(pCursor, _limits);

        var defaults = ReadSignedArray(pCursor, _limits);
        ExpectSeparator(pCursor, 0x90, "the second separator");
        var color = (int)pCursor.ReadUInt32();

        var selfVariableNames = new List<string>(SelfVariableNameCount);
        for (var index = 0; index < SelfVariableNameCount; index++)
        {
            selfVariableNames.Add(pCursor.ReadLengthPrefixedString(_limits.MaxStringBytes));
        }

        ExpectSeparator(pCursor, 0x91, "the third separator");
        ExpectFixedOneBlock(pCursor, "unknown5");

        ExpectSeparator(pCursor, 0x92, "the fourth separator");
        var returnName = pCursor.ReadLengthPrefixedString(_limits.MaxStringBytes);
        var returnValueId = (int)pCursor.ReadUInt32();
        ExpectSeparator(pCursor, 0x92, "the fifth separator");

        return new WolfBinaryCommonEvent
        {
            Id = pId,
            ConditionOperator = pConditionOperator,
            RunCondition = pRunCondition,
            ConditionVariable = pConditionVariable,
            ConditionValue = pConditionValue,
            ArgumentNumberCount = pArgumentNumberCount,
            ArgumentStringCount = pArgumentStringCount,
            Title = pTitle,
            Commands = pCommands,
            Memo = pMemo,
            ArgumentNames = pArgumentNames,
            ArgumentNumberDefaults = defaults,
            Color = color,
            SelfVariableNames = selfVariableNames,
            ReturnName = returnName,
            ReturnValueId = returnValueId,
        };
    }

    /// <summary>An array of number tables, one table per page.</summary>
    private static void ReadNumberTableArray(WolfByteCursor pCursor, WolfParseLimits pLimits)
    {
        var pages = (int)pCursor.ReadUInt32();
        if (pages < 0 || pages > pLimits.MaxDatabaseFieldsPerRecord)
        {
            throw new InvalidDataException(
                $"declares {pages} option value pages, over the {pLimits.MaxDatabaseFieldsPerRecord} limit");
        }
        for (var page = 0; page < pages; page++)
        {
            var count = (int)pCursor.ReadUInt32();
            if (count < 0 || count > pLimits.MaxDatabaseFieldsPerRecord)
            {
                throw new InvalidDataException(
                    $"declares {count} option values, over the {pLimits.MaxDatabaseFieldsPerRecord} limit");
            }
            for (var index = 0; index < count; index++)
            {
                _ = pCursor.ReadUInt32();
            }
        }
    }

    /// <summary>A signed number array: a count, then that many values.</summary>
    private static List<int> ReadSignedArray(WolfByteCursor pCursor, WolfParseLimits pLimits)
    {
        var count = (int)pCursor.ReadUInt32();
        if (count < 0 || count > pLimits.MaxDatabaseFieldsPerRecord)
        {
            throw new InvalidDataException(
                $"declares {count} number defaults, over the {pLimits.MaxDatabaseFieldsPerRecord} limit");
        }
        var values = new List<int>(count);
        for (var index = 0; index < count; index++)
        {
            values.Add((int)pCursor.ReadUInt32());
        }
        return values;
    }
}
