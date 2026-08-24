using System;
using System.Collections.Generic;
using UniversalRPG.Core;
using UniversalRPG.Rm2k;

namespace UniversalRPG.Rm2k.Parser;

/// <summary>
/// Decodes the special LMU event-command vector. Unlike normal LCF arrays it
/// has no item count and ends with four zero bytes.
/// </summary>
public static class Rm2kEventCommandDecoder
{
    public const int MaxCommands = 10000;
    public const int MaxParameters = 1000;
    public const int MaxStringBytes = 1024 * 1024;

    public static Rm2kParser.ParseResult Decode(byte[] pData)
    {
        if (pData == null) throw new ArgumentNullException(nameof(pData));
        var reader = new LcfBinaryReader(pData);
        var commands = new Godot.Collections.Array<Godot.Collections.Dictionary>();

        for (var index = 0; index < MaxCommands; index++)
        {
            if (reader.GetRemaining() >= 4 && IsTerminator(pData, reader.GetPosition()))
            {
                reader.ReadBytes(4);
                if (!reader.IsEof())
                {
                    return Failure("Trailing bytes after event command terminator", reader.GetPosition());
                }
                return new Rm2kParser.ParseResult(true, null, new Godot.Collections.Dictionary
                {
                    { "commands", commands },
                    { "count", commands.Count },
                });
            }

            var code = reader.ReadBer();
            var indent = reader.ReadBer();
            if (reader.HasError()) return ReaderFailure(reader);
            var stringLength = reader.ReadBer();
            if (reader.HasError() || stringLength < 0 || stringLength > MaxStringBytes)
            {
                return Failure("Event command string length is outside bounds", reader.GetPosition());
            }
            var textBytes = reader.ReadBytes(stringLength);
            var text = new LegacyTextDecoder().Decode(textBytes);
            var parameterCount = reader.ReadBer();
            if (reader.HasError() || parameterCount < 0 || parameterCount > MaxParameters)
            {
                return Failure("Event command parameter count is outside bounds", reader.GetPosition());
            }
            var parameters = new List<int>(parameterCount);
            for (var parameter = 0; parameter < parameterCount; parameter++)
            {
                parameters.Add(reader.ReadBer());
                if (reader.HasError()) return ReaderFailure(reader);
            }
            commands.Add(new Godot.Collections.Dictionary
            {
                { "code", code },
                { "indent", indent },
                { "text", text },
                { "parameters", parameters.ToArray() },
            });
        }
        return Failure($"Event command count exceeds {MaxCommands}", reader.GetPosition());
    }

    private static bool IsTerminator(byte[] pData, int pOffset)
    {
        return pOffset >= 0 && pOffset + 4 <= pData.Length
            && pData[pOffset] == 0 && pData[pOffset + 1] == 0
            && pData[pOffset + 2] == 0 && pData[pOffset + 3] == 0;
    }

    private static Rm2kParser.ParseResult ReaderFailure(LcfBinaryReader pReader)
        => Failure(pReader.ErrorMessage, pReader.ErrorOffset);

    private static Rm2kParser.ParseResult Failure(string pMessage, int pOffset)
        => new(false, new Rm2kParser.ParseError(pOffset, pMessage));
}
