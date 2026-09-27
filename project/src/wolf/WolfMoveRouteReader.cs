using System;
using System.Collections.Generic;

namespace UniversalRPG.Wolf;

/// <summary>
/// Reads a WOLF move route as data.
/// </summary>
/// <remarks>
/// <para>
/// A route is a header of single byte fields, then a length, then that many
/// steps. The header's order is not alphabetical and not the order a reader
/// would guess: the animation frequency, the move speed and the move frequency
/// come first, then the mode, then two option blocks, and only then the length.
/// </para>
/// <para>
/// A step is self describing. The types that take arguments write a byte saying
/// how many four byte values follow, the values, a byte saying how many single
/// byte values follow, and then those. Reading that from the file rather than
/// from a per type table is what lets this reader handle a type it has never
/// seen: it can still step over the step correctly, and report the type as one
/// it does not name.
/// </para>
/// <para>
/// The two option blocks are bit fields, not bytes. Reading them as bytes would
/// shift every following field by six, which is the kind of mistake that leaves
/// a file that looks decoded and is not.
/// </para>
/// <para>
/// This reader is structural only and is validated against byte sequences built
/// from the format specification, not against a real WOLF game.
/// </para>
/// </remarks>
public static class WolfMoveRouteReader
{
    /// <summary>
    /// Reads a move route from the cursor.
    /// </summary>
    public static WolfMoveRoute Read(WolfByteCursor pCursor, WolfParseLimits pLimits)
    {
        ArgumentNullException.ThrowIfNull(pCursor);

        var animationFrequency = pCursor.ReadByte();
        var moveSpeed = pCursor.ReadByte();
        var moveFrequency = pCursor.ReadByte();
        var mode = pCursor.ReadByte();

        // The behavior block is eight flags in one byte.
        var behavior = pCursor.ReadByte();
        var routeOptions = pCursor.ReadByte();

        var length = (int)pCursor.ReadUInt32();
        if (length < 0 || length > pLimits.MaxCommandsPerEvent)
        {
            throw new WolfFormatException(
                $"A WOLF move route declares {length} steps, outside 0 to "
                + $"{pLimits.MaxCommandsPerEvent}.");
        }

        var steps = new List<WolfMoveRouteStep>(length);
        for (var index = 0; index < length; index++)
        {
            steps.Add(ReadStep(pCursor, pLimits));
        }

        return new WolfMoveRoute
        {
            AnimationFrequency = animationFrequency,
            MoveSpeed = moveSpeed,
            MoveFrequency = moveFrequency,
            Mode = mode,
            HalfStepLeft = (behavior & 0x01) != 0,
            HalfStepUp = (behavior & 0x02) != 0,
            SquareHitbox = (behavior & 0x04) != 0,
            AboveHero = (behavior & 0x08) != 0,
            SlipThrough = (behavior & 0x10) != 0,
            FixedDirection = (behavior & 0x20) != 0,
            MoveAnimation = (behavior & 0x40) != 0,
            IdleAnimation = (behavior & 0x80) != 0,
            WaitUntilDone = (routeOptions & 0x80) != 0,
            SkipImpossibleMoves = (routeOptions & 0x40) != 0,
            RepeatActions = (routeOptions & 0x20) != 0,
            Steps = steps,
        };
    }

    /// <summary>
    /// Reads one step: its type, then its self describing argument pairs.
    /// </summary>
    /// <remarks>
    /// A step that takes no arguments still writes both length bytes, both zero.
    /// Reading only the first would leave the second to be taken from the next
    /// step's type, which is exactly the drift a self describing format is
    /// supposed to prevent.
    /// </remarks>
    private static WolfMoveRouteStep ReadStep(
        WolfByteCursor pCursor, WolfParseLimits pLimits)
    {
        var type = pCursor.ReadByte();

        var wordCount = pCursor.ReadByte();
        if (wordCount > pLimits.MaxDatabaseFieldsPerRecord)
        {
            throw new WolfFormatException(
                $"A WOLF route step of type 0x{type:X2} declares {wordCount} four "
                + $"byte arguments, over the {pLimits.MaxDatabaseFieldsPerRecord} limit.");
        }
        var arguments = new List<int>(wordCount);
        for (var index = 0; index < wordCount; index++)
        {
            arguments.Add((int)pCursor.ReadUInt32());
        }

        var byteCount = pCursor.ReadByte();
        if (byteCount > pLimits.MaxDatabaseFieldsPerRecord)
        {
            throw new WolfFormatException(
                $"A WOLF route step of type 0x{type:X2} declares {byteCount} single "
                + $"byte arguments, over the {pLimits.MaxDatabaseFieldsPerRecord} limit.");
        }
        var byteArguments = new List<byte>(byteCount);
        for (var index = 0; index < byteCount; index++)
        {
            byteArguments.Add(pCursor.ReadByte());
        }

        return new WolfMoveRouteStep
        {
            Type = type,
            Arguments = arguments,
            ByteArguments = byteArguments,
        };
    }
}
