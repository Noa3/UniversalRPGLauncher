using System;
using System.Collections.Generic;

namespace UniversalRPG.Wolf;

/// <summary>
/// The verified WOLF event command types. Every value comes from the published
/// command type table; none is invented.
/// </summary>
/// <remarks>
/// A command is a one byte parameter count, a four byte little endian type and
/// then a parameter block whose shape belongs to the type. The count is one
/// more than the number of parameter values, because the type is read as if it
/// were the first parameter, and a count of zero ends the command list without
/// a type following it.
/// </remarks>
public static class WolfCommandSignature
{
    /// <summary>Type 101, a message box.</summary>
    public const uint ShowMessage = 101;

    /// <summary>Type 103, a message box that only the editor shows.</summary>
    public const uint Comment = 103;

    /// <summary>Type 106, a debug message.</summary>
    public const uint DebugText = 106;

    /// <summary>Type 111, a numeric branch condition.</summary>
    public const uint NumberCondition = 111;

    /// <summary>Type 112, a string branch condition.</summary>
    public const uint StringCondition = 112;

    /// <summary>Type 121, a variable assignment.</summary>
    public const uint SetVariableBase = 121;

    /// <summary>Type 124, a variable operation.</summary>
    public const uint SetVariablePlusBase = 124;

    /// <summary>Type 210, a call to another event.</summary>
    public const uint CallEvent = 210;

    /// <summary>Type 300, a call to a common event by name.</summary>
    public const uint CallCommonByName = 300;

    /// <summary>Type 401, the opening half of a branch.</summary>
    public const uint Branch = 401;

    /// <summary>Type 499, the closing half of a branch.</summary>
    public const uint BranchEnd = 499;

    /// <summary>True when the type is one this reader decodes.</summary>
    public static bool IsKnown(uint pSignature)
    {
        return pSignature is ShowMessage or Comment or DebugText
            or NumberCondition or StringCondition
            or SetVariableBase or SetVariablePlusBase
            or CallEvent or CallCommonByName or Branch or BranchEnd;
    }

    /// <summary>
    /// A command's name, from its verified type and its parameter count.
    /// </summary>
    /// <remarks>
    /// Several WOLF commands share one type value and are told apart by how many
    /// parameters they carry: the variable commands are all type 121 and differ
    /// only in their parameter count, and so are the string conditions. Naming
    /// them by type alone would collapse them into one another, so the parameter
    /// count is part of the name.
    /// </remarks>
    public static string Describe(uint pSignature, int pParamCount)
    {
        return (pSignature, pParamCount) switch
        {
            (ShowMessage, _) => "ShowMessage",
            (Comment, _) => "Comment",
            (DebugText, _) => "DebugText",
            (NumberCondition, _) => "NumberCondition",
            (StringCondition, _) => "StringCondition",
            // The variable commands are all type 121 and are told apart only by
            // their parameter count. The specification gives the count and not
            // the operation each count stands for, so the name reports the
            // count rather than an operation that would be a guess.
            (SetVariableBase, 3) => "SetVariable with 3 parameters",
            (SetVariablePlusBase, 4) => "SetVariablePlus with 4 parameters",
            (CallEvent, _) => "CallEvent",
            (CallCommonByName, _) => "CallCommonByName",
            (Branch, _) => "Branch",
            (BranchEnd, _) => "BranchEnd",
            (SetVariableBase, _) => "SetVariable",
            (SetVariablePlusBase, _) => "SetVariablePlus",
            _ => $"type {pSignature} with {pParamCount} parameters",
        };
    }

    /// <summary>A command's name from its type alone, for diagnostics.</summary>
    public static string Describe(uint pSignature)
    {
        return IsKnown(pSignature) ? Describe(pSignature, 1) : $"type {pSignature}";
    }
}

/// <summary>One decoded WOLF event command.</summary>
public sealed class WolfBinaryEventCommand
{
    public required uint Signature { get; init; }
    public required string Name { get; init; }

    /// <summary>
    /// The parameter count, which is the command's first byte.
    /// </summary>
    /// <remarks>
    /// The count is one more than the number of parameter values, because the
    /// type is read as if it were the first parameter. A count of zero is the
    /// list's end marker and carries no type at all.
    /// </remarks>
    public int ParamCount { get; init; }

    public int StartOffset { get; init; }
    public int Length { get; init; }
    public IReadOnlyList<string> Strings { get; init; } = Array.Empty<string>();
    public IReadOnlyList<int> Numbers { get; init; } = Array.Empty<int>();

    /// <summary>
    /// The move route the command carries, or null when it has none.
    /// </summary>
    public WolfMoveRoute? Route { get; init; }

    /// <summary>Whether the command carries a move route.</summary>
    public bool HasRoute => Route != null;
}
