using System;
using System.Collections.Generic;

namespace UniversalRPG.Wolf;

/// <summary>How a WOLF move route is driven.</summary>
public static class WolfMoveRouteMode
{
    public const int DontMove = 0;
    public const int Custom = 1;
    public const int Random = 2;
    public const int TowardHero = 3;
}

/// <summary>
/// One step of a WOLF move route.
/// </summary>
/// <remarks>
/// A step is a one byte type and, for the types that take arguments, a
/// self describing pair of lengths: a byte saying how many four byte values
/// follow, the values, a byte saying how many single byte values follow, and
/// then those. The lengths are in the file rather than in a table, so a reader
/// does not have to know each type's shape to step over it, and a file that
/// disagrees with this reader's table still reads.
/// </remarks>
public sealed class WolfMoveRouteStep
{
    /// <summary>The step's type value.</summary>
    public required byte Type { get; init; }

    /// <summary>The four byte arguments, in file order.</summary>
    public IReadOnlyList<int> Arguments { get; init; } = Array.Empty<int>();

    /// <summary>The single byte arguments, in file order.</summary>
    public IReadOnlyList<byte> ByteArguments { get; init; } = Array.Empty<byte>();

    /// <summary>
    /// The step's name, from the verified route type table.
    /// </summary>
    public string Name => WolfMoveRouteType.Describe(Type);

    /// <summary>Whether the type is one the route type table defines.</summary>
    public bool IsKnownType => WolfMoveRouteType.IsKnown(Type);
}

/// <summary>One WOLF move route, as stored in an event command.</summary>
public sealed class WolfMoveRoute
{
    /// <summary>How often the character steps forward, in frames.</summary>
    public int AnimationFrequency { get; init; }

    /// <summary>The move speed, in the format's own scale.</summary>
    public int MoveSpeed { get; init; }

    /// <summary>How often the character moves, in frames.</summary>
    public int MoveFrequency { get; init; }

    /// <summary>How the route is driven, from the move route mode table.</summary>
    public int Mode { get; init; }

    /// <summary>Whether the route waits for a step to finish before the next.</summary>
    public bool WaitUntilDone { get; init; }

    /// <summary>Whether an impossible step is skipped instead of stopping the route.</summary>
    public bool SkipImpossibleMoves { get; init; }

    /// <summary>Whether the route repeats when it runs out.</summary>
    public bool RepeatActions { get; init; }

    /// <summary>Whether the character is drawn above the hero.</summary>
    public bool AboveHero { get; init; }

    /// <summary>Whether the character ignores other characters' collision.</summary>
    public bool SlipThrough { get; init; }

    /// <summary>Whether the character keeps the direction it was set to.</summary>
    public bool FixedDirection { get; init; }

    /// <summary>Whether the character is animated while moving.</summary>
    public bool MoveAnimation { get; init; }

    /// <summary>Whether the character is animated while standing still.</summary>
    public bool IdleAnimation { get; init; }

    /// <summary>Whether the character is drawn behind the hero.</summary>
    public bool SquareHitbox { get; init; }

    /// <summary>Whether the character moves half a tile per step.</summary>
    public bool HalfStepLeft { get; init; }

    /// <summary>Whether the character moves up a half tile per step.</summary>
    public bool HalfStepUp { get; init; }

    /// <summary>The steps, in file order.</summary>
    public IReadOnlyList<WolfMoveRouteStep> Steps { get; init; } = Array.Empty<WolfMoveRouteStep>();
}

/// <summary>The verified WOLF move route type values.</summary>
public static class WolfMoveRouteType
{
    public const byte MoveDown = 0x00;
    public const byte MoveLeft = 0x01;
    public const byte MoveRight = 0x02;
    public const byte MoveUp = 0x03;
    public const byte MoveDownLeft = 0x04;
    public const byte MoveDownRight = 0x05;
    public const byte MoveUpLeft = 0x06;
    public const byte MoveUpRight = 0x07;
    public const byte FacingDown = 0x08;
    public const byte FacingLeft = 0x09;
    public const byte FacingRight = 0x0A;
    public const byte FacingUp = 0x0B;
    public const byte Jump = 0x15;
    public const byte AssignToVariable = 0x1C;
    public const byte SetMoveSpeed = 0x1D;
    public const byte SetMoveFrequency = 0x1E;
    public const byte SetAnimationFrequency = 0x1F;
    public const byte SetGraphic = 0x2C;
    public const byte SetOpacity = 0x2D;
    public const byte SetSound = 0x2E;
    public const byte WaitXFrames = 0x2F;
    public const byte MoveApproachEvent = 0x35;
    public const byte MoveApproachPosition = 0x36;
    public const byte AddToVariable = 0x37;
    public const byte SetHeight = 0x3A;

    /// <summary>True when the type is one the route type table defines.</summary>
    public static bool IsKnown(byte pType)
    {
        // The table is contiguous apart from one gap, at 0x2A and 0x2B, which
        // the specification leaves unused. Treating the whole range as known
        // would accept a value the format does not define.
        return pType <= 0x29 || pType is >= 0x2C and <= 0x3A;
    }

    /// <summary>
    /// A step's name.
    /// </summary>
    /// <remarks>
    /// Names come from the verified table. A value the table does not define is
    /// reported as such rather than skipped, because a reader that skipped it
    /// would not know how long it is.
    /// </remarks>
    public static string Describe(byte pType)
    {
        return pType switch
        {
            MoveDown => "MoveDown",
            MoveLeft => "MoveLeft",
            MoveRight => "MoveRight",
            MoveUp => "MoveUp",
            MoveDownLeft => "MoveDownLeft",
            MoveDownRight => "MoveDownRight",
            MoveUpLeft => "MoveUpLeft",
            MoveUpRight => "MoveUpRight",
            FacingDown => "FacingDown",
            FacingLeft => "FacingLeft",
            FacingRight => "FacingRight",
            FacingUp => "FacingUp",
            Jump => "Jump",
            AssignToVariable => "AssignToVariable",
            SetMoveSpeed => "SetMoveSpeed",
            SetMoveFrequency => "SetMoveFrequency",
            SetAnimationFrequency => "SetAnimationFrequency",
            SetGraphic => "SetGraphic",
            SetOpacity => "SetOpacity",
            SetSound => "SetSound",
            WaitXFrames => "WaitXFrames",
            MoveApproachEvent => "MoveApproachEvent",
            MoveApproachPosition => "MoveApproachPosition",
            AddToVariable => "AddToVariable",
            SetHeight => "SetHeight",
            _ when !IsKnown(pType) => $"route type 0x{pType:X2}, which the table leaves unused",
            _ => $"route type 0x{pType:X2}",
        };
    }
}
