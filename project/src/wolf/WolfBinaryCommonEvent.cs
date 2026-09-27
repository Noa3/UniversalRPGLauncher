using System.Collections.Generic;

namespace UniversalRPG.Wolf;

/// <summary>When a WOLF common event runs.</summary>
public static class WolfCommonEventRunCondition
{
    /// <summary>The event runs only when it is called.</summary>
    public const int CallOnly = 0;

    /// <summary>The event runs on every map load.</summary>
    public const int AutoStart = 1;

    /// <summary>The event runs in the background while another event runs.</summary>
    public const int ParallelProcess = 2;

    /// <summary>The event runs in the background even when no event is active.</summary>
    public const int ParallelProcessAlways = 3;
}

/// <summary>How an argument's options are specified.</summary>
public static class WolfArgumentSpecification
{
    /// <summary>The options are not specified.</summary>
    public const int NoSpecialSpecification = 0;

    /// <summary>The options are read from a database.</summary>
    public const int DatabaseReference = 1;

    /// <summary>The options are written out by hand.</summary>
    public const int ManualOptions = 2;
}

/// <summary>The message colour of a common event.</summary>
public static class WolfCommonEventColor
{
    public const int Black = 0;
    public const int Red = 1;
    public const int Blue = 2;
    public const int Green = 3;
    public const int Magenta = 4;
    public const int Yellow = 5;
    public const int Gray = 6;

    /// <summary>Whether a value is one of the seven colours the format defines.</summary>
    public static bool IsKnown(int pColor)
    {
        return pColor >= Black && pColor <= Gray;
    }
}

/// <summary>One WOLF common event.</summary>
public sealed class WolfBinaryCommonEvent
{
    /// <summary>The database id the event is called by.</summary>
    public int Id { get; init; }

    /// <summary>The comparison operator for the run condition.</summary>
    public int ConditionOperator { get; init; }

    /// <summary>When the event runs, from <c>run_condition</c>.</summary>
    public int RunCondition { get; init; }

    /// <summary>The variable the run condition reads.</summary>
    public uint ConditionVariable { get; init; }

    /// <summary>The value the variable is compared against.</summary>
    public int ConditionValue { get; init; }

    /// <summary>How many number arguments the event takes.</summary>
    public int ArgumentNumberCount { get; init; }

    /// <summary>How many string arguments the event takes.</summary>
    public int ArgumentStringCount { get; init; }

    /// <summary>The event's title, shown in the call list.</summary>
    public string Title { get; init; } = "";

    /// <summary>The event's body, as decoded commands.</summary>
    public IReadOnlyList<WolfBinaryEventCommand> Commands { get; init; } = [];

    /// <summary>The editor memo, which has no meaning at runtime.</summary>
    public string Memo { get; init; } = "";

    /// <summary>The display names of the arguments, in order.</summary>
    public IReadOnlyList<string> ArgumentNames { get; init; } = [];

    /// <summary>The default values of the number arguments.</summary>
    public IReadOnlyList<int> ArgumentNumberDefaults { get; init; } = [];

    /// <summary>The message colour.</summary>
    public int Color { get; init; }

    /// <summary>The internal variable names the event reads and writes, 100 of them.</summary>
    public IReadOnlyList<string> SelfVariableNames { get; init; } = [];

    /// <summary>The name this common event returns a value under.</summary>
    public string ReturnName { get; init; } = "";

    /// <summary>The variable the return value is stored in.</summary>
    public int ReturnValueId { get; init; }

    /// <summary>Whether the event is one a call can use.</summary>
    public bool IsCallable => Id > 0;

    /// <summary>Whether the run condition is one the format defines.</summary>
    public bool IsKnownRunCondition => RunCondition >= WolfCommonEventRunCondition.CallOnly
        && RunCondition <= WolfCommonEventRunCondition.ParallelProcessAlways;
}
