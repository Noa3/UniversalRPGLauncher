using System;
using System.Collections.Generic;

namespace UniversalRPG.Wolf;

/// <summary>
/// Reads WOLF event commands as data.
/// </summary>
/// <remarks>
/// <para>
/// A command is a one byte parameter count, a four byte little endian type and
/// a parameter block whose shape belongs to the type. That last part is the
/// whole difficulty: the block is not uniform. A message command has none, a
/// condition has a flag and a count and then that many triples, a call has an
/// event id and then an argument status word. A reader that treats the block as
/// one fixed shape decodes the first command of a file and then desynchronises,
/// which is the failure this reader is written to avoid.
/// </para>
/// <para>
/// After the parameter block come a branch depth byte and a string count byte,
/// then that many strings and a flag saying whether a move route follows. Those
/// fields do belong to every command, which is why they are read here rather
/// than per type.
/// </para>
/// <para>
/// A route is not decoded. A command that has one is reported as carrying an
/// undecoded route rather than stepped over, because stepping over bytes the
/// reader does not understand would shift every command after it.
/// </para>
/// <para>
/// This reader is structural only and is validated against byte sequences built
/// from the format specification, not against a real WOLF game.
/// </para>
/// </remarks>
public static class WolfEventCommandReader
{
    /// <summary>
    /// Reads <paramref name="pCount"/> commands, or throws when the buffer
    /// ends.
    /// </summary>
    /// <remarks>
    /// A partial read is never returned: a command list with a wrong length
    /// desynchronises every following event, so the only safe options are a
    /// complete list or a refusal.
    /// </remarks>
    public static IReadOnlyList<WolfBinaryEventCommand> ReadCommands(
        WolfByteCursor pCursor, int pCount, WolfParseLimits pLimits)
    {
        ArgumentNullException.ThrowIfNull(pCursor);
        var commands = new List<WolfBinaryEventCommand>(Math.Max(0, pCount));
        for (var index = 0; index < pCount; index++)
        {
            commands.Add(ReadCommand(pCursor, pLimits));
        }
        return commands;
    }

    /// <summary>Reads exactly one command from the cursor.</summary>
    public static WolfBinaryEventCommand ReadCommand(
        WolfByteCursor pCursor, WolfParseLimits pLimits)
    {
        var start = pCursor.Position;
        var paramCount = pCursor.ReadByte();

        // A zero count is the list's end marker, not a command with no
        // arguments. Nothing follows it, so reading a type here would consume
        // the next command's first byte and desynchronise everything after it.
        if (paramCount == 0)
        {
            return new WolfBinaryEventCommand
            {
                Signature = 0,
                Name = "the end of the command list",
                ParamCount = 0,
                StartOffset = start,
                Length = 1,
            };
        }

        var signature = pCursor.ReadUInt32();
        if (!WolfCommandSignature.IsKnown(signature))
        {
            // An unknown type's block cannot be walked, because its length is
            // what is unknown. Stopping is the only honest option: guessing
            // would shift every command after this one.
            throw new WolfFormatException(
                $"WOLF command type {signature} with {paramCount} parameters is not "
                + "implemented by this reader. Decoding stops here rather than "
                + "guessing a length, because a wrong length would shift every "
                + "following command.");
        }

        var strings = new List<string>();
        var numbers = new List<int>();
        ReadParameterBlock(pCursor, signature, paramCount, numbers);

        // These fields do belong to every command, whatever its block shape.
        _ = pCursor.ReadByte(); // branch depth
        var stringCount = pCursor.ReadByte();
        if (stringCount > pLimits.MaxStringsPerCommand)
        {
            throw new WolfFormatException(
                $"A WOLF command declares {stringCount} strings, over the "
                + $"{pLimits.MaxStringsPerCommand} limit.");
        }
        for (var index = 0; index < stringCount; index++)
        {
            strings.Add(pCursor.ReadLengthPrefixedString(pLimits.MaxStringBytes));
        }
        var hasRoute = pCursor.ReadByte() != 0;
        WolfMoveRoute? route = null;
        if (hasRoute)
        {
            route = WolfMoveRouteReader.Read(pCursor, pLimits);
        }

        return new WolfBinaryEventCommand
        {
            Signature = signature,
            Name = WolfCommandSignature.Describe(signature, paramCount),
            ParamCount = paramCount,
            StartOffset = start,
            Length = pCursor.Position - start,
            Strings = strings,
            Numbers = numbers,
            Route = route,
        };
    }

    /// <summary>
    /// Reads the parameter block of a known command type.
    /// </summary>
    /// <remarks>
    /// The block belongs to the type, so this is a switch and not a loop. The
    /// argument status word that packs a number count and a string count exists
    /// only for the commands that take arguments; a message command has no block
    /// at all, and giving it one is how a reader ends up reading its text out
    /// of a field that is not there.
    /// </remarks>
    private static void ReadParameterBlock(
        WolfByteCursor pCursor, uint pSignature, int pParamCount, List<int> pNumbers)
    {
        switch (pSignature)
        {
            case WolfCommandSignature.ShowMessage:
            case WolfCommandSignature.Comment:
            case WolfCommandSignature.DebugText:
            case WolfCommandSignature.BranchEnd:
                return;

            case WolfCommandSignature.NumberCondition:
            {
                var useElse = pCursor.ReadUInt32();
                if (useElse > 1)
                {
                    throw new WolfFormatException(
                        $"A WOLF number condition has an else flag of {useElse}, "
                        + "which is neither zero nor one.");
                }
                var conditionCount = pCursor.ReadUInt32();
                pCursor.Skip(3); // the verified three padding bytes
                for (var index = 0; index < conditionCount; index++)
                {
                    pNumbers.Add((int)pCursor.ReadUInt32()); // variable
                    pNumbers.Add((int)pCursor.ReadUInt32()); // value
                    pNumbers.Add((int)pCursor.ReadUInt32()); // operator
                }
                return;
            }

            case WolfCommandSignature.CallEvent:
            {
                var eventId = (int)pCursor.ReadUInt32();
                pNumbers.Add(eventId);
                if (eventId is >= 500000 and <= 599999)
                {
                    // An id in that range is a common event, and only then does
                    // the call carry arguments.
                    ReadArgumentBlock(pCursor, pParamCount, pNumbers);
                }
                return;
            }

            case WolfCommandSignature.CallCommonByName:
            {
                var leading = pCursor.ReadUInt32();
                if (leading != 0)
                {
                    throw new WolfFormatException(
                        $"A WOLF common event call begins with {leading}, where the "
                        + "verified zero belongs.");
                }
                var status = ReadArgumentBlock(pCursor, pParamCount, pNumbers);
                if (status.ReturnValueEnabled)
                {
                    pNumbers.Add((int)pCursor.ReadUInt32());
                }
                return;
            }

            case WolfCommandSignature.Branch:
                pNumbers.Add((int)pCursor.ReadUInt32());
                return;

            case WolfCommandSignature.SetVariableBase:
            case WolfCommandSignature.SetVariablePlusBase:
            case WolfCommandSignature.StringCondition:
            default:
                ReadArgumentBlock(pCursor, pParamCount, pNumbers);
                return;
        }
    }

    /// <summary>
    /// Reads the argument status word and the numbers it counts.
    /// </summary>
    /// <remarks>
    /// The number count is the low nibble and the string count is the next one.
    /// The strings themselves are not read here: they come after the branch
    /// depth byte and the command's own string count, so reading them from this
    /// word would put them in the wrong place.
    /// </remarks>
    private static ArgumentStatus ReadArgumentBlock(
        WolfByteCursor pCursor, int pParamCount, List<int> pNumbers)
    {
        var raw = pCursor.ReadUInt32();
        var status = new ArgumentStatus
        {
            NumberCount = (int)(raw & 0xF),
            StringCount = (int)((raw >> 4) & 0xF),
            ReturnValueEnabled = ((raw >> 17) & 1) == 1,
        };
        for (var index = 0; index < status.NumberCount; index++)
        {
            pNumbers.Add((int)pCursor.ReadUInt32());
        }
        // Any parameter values the status word did not account for are read and
        // reported rather than skipped silently, so a count that disagrees with
        // the parameter count shows up as a value instead of a drift.
        for (var index = status.NumberCount; index < pParamCount - 1; index++)
        {
            pNumbers.Add((int)pCursor.ReadUInt32());
        }
        return status;
    }

    private readonly struct ArgumentStatus
    {
        public int NumberCount { get; init; }
        public int StringCount { get; init; }
        public bool ReturnValueEnabled { get; init; }
    }
}
