using System;
using System.Collections.Generic;
using System.Text;
using Godot;
using UniversalRPG.Tests.Framework;
using UniversalRPG.Wolf;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Proves the WOLF event command reader against byte sequences built from the
/// published command signature table. As with the map reader, what is proven is
/// the framing and the field order, not the meaning of any single command,
/// because no real WOLF game is available in this environment.
/// </summary>
public partial class TestWolfEventCommand : TestBase
{
    public void Test_ACommandIsAParamCountThenAFourByteLittleEndianType()
    {
        // The verified layout is param_count(u1), command_type(u4 LE), then
        // param_count-1 four byte parameters. A comment is type 103, which is
        // 0x67, so a one parameter comment is 01 67 00 00 00.
        var bytes = new List<byte>();
        AddComment(bytes, "a note");

        var commands = Read(bytes, 1);

        AssertEq(commands.Count, 1, "one command");
        AssertEq(commands[0].ParamCount, 1, "the parameter count is read");
        AssertEq(commands[0].Signature, WolfCommandSignature.Comment,
            "the type is the plain value 103, with no count packed into it");
        AssertEq(commands[0].Name, "Comment", "and the type names the command");
    }

    public void Test_TheTypeIsLittleEndianAndReadingItBigEndianWouldNotMatch()
    {
        // This is the asymmetry worth a test: the file's ordinary values are
        // little endian and so is the command type. Reading the type big endian
        // gives a different number, which is not a known type, so a wrong
        // endianness fails loudly instead of producing a plausible command.
        var bytes = new List<byte> { 0x01, 0x67, 0x00, 0x00, 0x00 };
        var readLittleEndian = (uint)(bytes[1] | (bytes[2] << 8) | (bytes[3] << 16) | (bytes[4] << 24));
        var readBigEndian = (uint)((bytes[1] << 24) | (bytes[2] << 16) | (bytes[3] << 8) | bytes[4]);
        AssertEq(readLittleEndian, 103u, "the little endian read gives the type");
        AssertTrue(readBigEndian != 103u, "the big endian read of the same bytes does not");
    }

    public void Test_AZeroParamCountEndsTheListAndCarriesNoType()
    {
        // A count of zero is the list's end marker, not a command with no
        // arguments. Nothing at all follows it, so a reader that read a type
        // here would consume the next command's first byte.
        var bytes = new List<byte>();
        AddComment(bytes, "first");
        AddEndMarker(bytes);

        var commands = Read(bytes, 2);

        AssertEq(commands.Count, 2, "the marker is read as a command");
        AssertEq(commands[0].Name, "Comment", "the real command comes first");
        AssertEq(commands[0].Strings[0], "first", "and its text is intact");
        AssertEq(commands[1].ParamCount, 0, "the marker carries no parameters");
        AssertEq(commands[1].Signature, 0u, "and no type at all");
        AssertEq(commands[1].Length, 1, "so it is exactly one byte long");
    }

    public void Test_TheTypeValueIsNotTheCountPackedIntoIt()
    {
        // The old constants packed the parameter count into the top byte, so a
        // comment read 0x01670000. The verified value is 103 on its own, and a
        // reader that expects the packed form would not find this type.
        AssertEq(WolfCommandSignature.Comment, 103u, "a comment is type 103");
        AssertEq(WolfCommandSignature.ShowMessage, 101u, "a message is type 101");
        AssertEq(WolfCommandSignature.NumberCondition, 111u, "a number condition is type 111");
        AssertEq(WolfCommandSignature.StringCondition, 112u, "a string condition is type 112");
        AssertEq(WolfCommandSignature.CallCommonByName, 300u, "calling a common event is type 300");
    }

    public void Test_TheArgumentStatusWordSplitsIntoANumberCountAndAStringCount()
    {
        // The number count is the low nibble and the string count is the next
        // one. The status word belongs to the commands that take arguments; a
        // message command has no status word at all, which is what makes
        // reading one here a mistake.
        var bytes = new List<byte>();
        AddHeader(bytes, WolfCommandSignature.SetVariableBase, 3);
        AddStatus(bytes, 2, 1);
        AddUInt32(bytes, 7);
        AddUInt32(bytes, 9);
        AddCommandTail(bytes, ["target"]);

        var command = Read(bytes, 1)[0];

        AssertEq(command.Numbers.Count, 2, "two numbers are read");
        AssertEq(command.Numbers[0], 7, "the first number");
        AssertEq(command.Numbers[1], 9, "the second number");
        AssertEq(command.Strings.Count, 1, "one string is read");
        AssertEq(command.Strings[0], "target", "and it is the string the fixture wrote");
    }

    public void Test_SeveralCommandsAreReadInSequenceWithoutDrift()
    {
        // A command list with a wrong length would shift every following
        // command, so consecutive different commands are the real test. The
        // three here have different numbers, string counts and string lengths,
        // so a reader that is off by a byte anywhere cannot read all of them.
        var bytes = new List<byte>();
        AddComment(bytes, "first");
        AddNumberCondition(bytes, [[0, 42, 0]]);
        AddVariable(bytes, WolfCommandSignature.SetVariableBase, 3, [11, 22]);
        AddMessage(bytes, "a longer message");

        var commands = Read(bytes, 4);

        AssertEq(commands.Count, 4, "all four commands are read");
        AssertEq(commands[0].Name, "Comment", "the first is a comment");
        AssertEq(commands[0].Strings[0], "first", "with its own text");
        AssertEq(commands[1].Name, "NumberCondition", "the second is a condition");
        AssertEq(commands[1].Numbers.Count, 3, "with its three numbers");
        AssertEq(commands[1].Numbers[1], 42, "and the middle one intact");
        AssertEq(commands[1].ParamCount, 5, "and the condition's parameter count");
        AssertEq(commands[2].Name, "SetVariable with 3 parameters",
            "the third is a variable command, named by its parameter count");
        AssertEq(commands[2].Numbers.Count, 2, "with its two numbers");
        AssertEq(commands[2].Numbers[1], 22, "and the second one intact");
        AssertEq(commands[3].Name, "ShowMessage", "the fourth is a message");
        AssertEq(commands[3].Strings[0], "a longer message", "with its longer text intact");
    }

    public void Test_AMessageCommandHasNoParametersAndItsTextIsAString()
    {
        // A message command's parameter list is empty, so its text cannot come
        // from the parameters. It comes from the string count, which is the
        // thing a reader written per command type gets wrong.
        var bytes = new List<byte>();
        AddMessage(bytes, "Hello");

        var command = Read(bytes, 1)[0];

        AssertEq(command.Name, "ShowMessage", "the type names the command");
        AssertEq(command.ParamCount, 1, "a message command carries no parameter values");
        AssertEq(command.Numbers.Count, 0, "so there are no numbers");
        AssertEq(command.Strings.Count, 1, "and exactly one string");
        AssertEq(command.Strings[0], "Hello", "which is the message text");
    }

    public void Test_AStringCountOverTheLimitIsRefused()
    {
        // The string count is a single byte, so a file can claim up to 255
        // strings. Reading that many out of a short file would walk off the
        // end, so a count over the limit is refused instead of followed.
        var bytes = new List<byte>();
        AddHeader(bytes, WolfCommandSignature.ShowMessage, 1);
        bytes.Add(0x00); // branch depth
        bytes.Add(0xFE); // two hundred and fifty-four strings
        AddString(bytes, "only one");

        var error = "";
        try
        {
            Read(bytes, 1);
        }
        catch (WolfFormatException exception)
        {
            error = exception.Message;
        }
        AssertTrue(error.Contains("over the", StringComparison.OrdinalIgnoreCase),
            $"the diagnostic names the limit, but it said: {error}");
    }

    public void Test_ACommandWithAMoveRouteIsReportedRatherThanGuessed()
    {
        // A command may carry a move route. This reader does not decode routes,
        // so it says so on the command instead of stepping over bytes it does
        // not understand, which would shift every command after it.
        var bytes = new List<byte>();
        AddHeader(bytes, WolfCommandSignature.Comment, 1);
        AddCommandTail(bytes, ["move"], pHaveRoute: 0x01, pRoute: TwoStepRoute());

        var command = Read(bytes, 1)[0];

        AssertTrue(command.HasRoute, "the command carries a route");
        AssertEq(command.Strings[0], "move", "and its own text is still read");
        AssertEq(command.Route!.Steps.Count, 2, "the route's two steps are read");
        AssertEq(command.Route.Steps[0].Type, WolfMoveRouteType.MoveDown,
            "the first step is a plain move");
    }


    public void Test_ATruncatedCommandListIsRefusedRatherThanPartiallyDecoded()
    {
        var bytes = new List<byte>();
        AddComment(bytes, "only one");

        var error = "";
        try
        {
            // Two commands are declared but only one is present.
            Read(bytes, 2);
        }
        catch (WolfFormatException exception)
        {
            error = exception.Message;
        }

        AssertTrue(error.Length > 0, "a truncated command list must be refused");
        AssertTrue(error.Contains("ends at byte", StringComparison.OrdinalIgnoreCase),
            $"the diagnostic names the offset, but it said: {error}");
    }

    // ---- helpers ----

    private static IReadOnlyList<WolfBinaryEventCommand> Read(List<byte> pBytes, int pCount)
    {
        var cursor = new WolfByteCursor(pBytes.ToArray(), "commands");
        return WolfEventCommandReader.ReadCommands(cursor, pCount, new WolfParseLimits());
    }

    /// <summary>
    /// Writes the tail every command shares: a branch depth byte, a string
    /// count, the strings and the move route flag.
    /// </summary>
    private static void AddCommandTail(
        List<byte> pBody, string[] pStrings = null, byte pHaveRoute = 0x00,
        byte[] pRoute = null)
    {
        pStrings ??= [];
        pBody.Add(0x00); // branch depth
        pBody.Add((byte)pStrings.Length);
        foreach (var text in pStrings)
        {
            AddString(pBody, text);
        }
        pBody.Add(pHaveRoute);
        if (pRoute != null)
        {
            pBody.AddRange(pRoute);
        }
    }

    /// <summary>A minimal route: a header, no steps and a two step body.</summary>
    private static byte[] TwoStepRoute()
    {
        var bytes = new List<byte>
        {
            0x04, // animation frequency
            0x04, // move speed
            0x04, // move frequency
            0x01, // mode
            0x00, // behavior flags
            0x00, // route options
        };
        AddUInt32(bytes, 2);
        // A step is a type, a four byte argument count, the arguments, a single
        // byte argument count and those arguments.
        bytes.AddRange([WolfMoveRouteType.MoveDown, 0x00, 0x00]);
        bytes.AddRange([WolfMoveRouteType.FacingUp, 0x00, 0x00]);
        return bytes.ToArray();
    }

    /// <summary>Writes the parameter count and the four byte type.</summary>
    private static void AddHeader(List<byte> pBody, uint pSignature, int pParamCount)
    {
        pBody.Add((byte)pParamCount);
        pBody.Add((byte)(pSignature & 0xFF));
        pBody.Add((byte)((pSignature >> 8) & 0xFF));
        pBody.Add((byte)((pSignature >> 16) & 0xFF));
        pBody.Add((byte)((pSignature >> 24) & 0xFF));
    }

    /// <summary>Writes the argument status word, whose low nibble is the count.</summary>
    private static void AddStatus(List<byte> pBody, int pNumberCount, int pStringCount = 0)
    {
        var raw = (pStringCount << 4) | (pNumberCount & 0xF);
        for (var index = 0; index < 4; index++)
        {
            pBody.Add((byte)((raw >> (index * 8)) & 0xFF));
        }
    }

    private static void AddUInt32(List<byte> pBody, int pValue)
    {
        pBody.Add((byte)(pValue & 0xFF));
        pBody.Add((byte)((pValue >> 8) & 0xFF));
        pBody.Add((byte)((pValue >> 16) & 0xFF));
        pBody.Add((byte)((pValue >> 24) & 0xFF));
    }

    /// <summary>
    /// Writes a message command: no parameter block, one string, no route.
    /// </summary>
    /// <remarks>
    /// This is the case a reader written with one fixed block shape gets wrong,
    /// because the block it expects is not there at all.
    /// </remarks>
    private static void AddMessage(List<byte> pBody, string pText)
    {
        AddHeader(pBody, WolfCommandSignature.ShowMessage, 1);
        AddCommandTail(pBody, [pText]);
    }

    /// <summary>
    /// Writes a variable command: a status word and the numbers it counts.
    /// </summary>
    private static void AddVariable(List<byte> pBody, uint pSignature, int pParamCount,
        int[] pNumbers)
    {
        AddHeader(pBody, pSignature, pParamCount);
        AddStatus(pBody, pNumbers.Length);
        foreach (var number in pNumbers)
        {
            AddUInt32(pBody, number);
        }
        AddCommandTail(pBody);
    }

    /// <summary>
    /// Writes a number condition: an else flag, a condition count, three
    /// padding bytes and then that many variable, value, operator triples.
    /// </summary>
    private static void AddNumberCondition(
        List<byte> pBody, int[][] pConditions, int pUseElse = 0)
    {
        AddHeader(pBody, WolfCommandSignature.NumberCondition, 5);
        AddUInt32(pBody, pUseElse);
        AddUInt32(pBody, pConditions.Length);
        pBody.AddRange([0x00, 0x00, 0x00]); // the verified padding
        foreach (var condition in pConditions)
        {
            AddUInt32(pBody, condition[0]);
            AddUInt32(pBody, condition[1]);
            AddUInt32(pBody, condition[2]);
        }
        AddCommandTail(pBody);
    }

    /// <summary>Writes a comment, which is a message with an editor only type.</summary>
    private static void AddComment(List<byte> pBody, string pText)
    {
        AddHeader(pBody, WolfCommandSignature.Comment, 1);
        AddCommandTail(pBody, [pText]);
    }

    /// <summary>Writes the zero that ends a command list.</summary>
    private static void AddEndMarker(List<byte> pBody)
    {
        pBody.Add(0x00);
    }

    private static void AddString(List<byte> pBody, string pText)
    {
        var bytes = Encoding.GetEncoding("Shift-JIS").GetBytes(pText);
        AddUInt32(pBody, (uint)bytes.Length);
        pBody.AddRange(bytes);
    }

    private static void AddUInt32(List<byte> pBody, uint pValue)
    {
        pBody.Add((byte)(pValue & 0xFF));
        pBody.Add((byte)((pValue >> 8) & 0xFF));
        pBody.Add((byte)((pValue >> 16) & 0xFF));
        pBody.Add((byte)((pValue >> 24) & 0xFF));
    }
}
