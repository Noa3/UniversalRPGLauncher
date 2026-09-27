using System;
using System.Collections.Generic;

namespace UniversalRPG.Web;

/// <summary>
/// A conditional branch of an MZ event, and what it tests.
/// </summary>
/// <remarks>
/// <para>
/// The engine decides a branch in one method, and every number in it was read
/// out of that method: which parameter says what kind of thing is being tested,
/// which parameter says how, and which parameters hold the operands. A branch
/// that is read but not understood is a number with a name, and a game whose
/// branches are not understood is a game whose flow is not known.
/// </para>
/// <para>
/// <b>One kind of branch is deliberately not implemented.</b> Kind 12 asks
/// whether a line of the author's own JavaScript is true, and the engine answers
/// it by evaluating that line. This repository does not execute a game's code, so
/// a branch of kind 12 is <see cref="MzBranchKind.Script"/> and reading it
/// returns <see cref="MzBranchOutcome.ScriptNotRun"/>: the branch is reported,
/// its text is kept, and nothing is evaluated. A caller that needed the answer
/// has to supply it, and a caller that did not know the branch existed has not
/// been lied to.
/// </para>
/// </remarks>
public sealed class MzBranch
{
    public MzBranch(
        MzBranchKind pKind,
        IReadOnlyList<string> pParameters,
        string pScriptText)
    {
        Kind = pKind;
        Parameters = pParameters;
        ScriptText = pScriptText;
    }

    public MzBranchKind Kind { get; }

    /// <summary>
    /// The branch's own parameters, as the engine reads them: the kind first,
    /// then the operands, then the way of testing.
    /// </summary>
    public IReadOnlyList<string> Parameters { get; }

    /// <summary>
    /// The author's own text, for the one kind of branch whose answer is a piece
    /// of code. It is kept as text and is never evaluated.
    /// </summary>
    public string ScriptText { get; }

    /// <summary>Reads a branch out of a command's parameters.</summary>
    public static MzBranch FromParameters(IReadOnlyList<string> pParameters)
    {
        var kind = pParameters.Count > 0 ? ToInt(pParameters[0]) : 0;
        return new MzBranch(
            (MzBranchKind)kind, pParameters,
            kind == (int)MzBranchKind.Script && pParameters.Count > 1
                ? pParameters[1]
                : "");
    }

    internal static int ToInt(string pText) =>
        int.TryParse(
            pText, System.Globalization.NumberStyles.Integer,
            System.Globalization.CultureInfo.InvariantCulture, out var value)
            ? value
            : 0;
}

/// <summary>What a branch tests, named by the engine's own switch.</summary>
public enum MzBranchKind
{
    Switch = 0,
    Variable = 1,
    SelfSwitch = 2,
    Timer = 3,
    Actor = 4,
    Enemy = 5,
    Character = 6,
    Gold = 7,
    Item = 8,
    Weapon = 9,
    Armor = 10,
    Button = 11,

    /// <summary>
    /// A line of the author's own JavaScript, which the engine evaluates. This
    /// repository does not, and a branch of this kind is reported and not
    /// answered.
    /// </summary>
    Script = 12,

    Vehicle = 13,
}

/// <summary>How a branch that compares two things compares them.</summary>
public enum MzComparison
{
    Equal = 0,
    AtLeast = 1,
    AtMost = 2,
    Greater = 3,
    Less = 4,
    NotEqual = 5,
}

/// <summary>What a branch came to.</summary>
public enum MzBranchOutcome
{
    /// <summary>The test came to true.</summary>
    True,

    /// <summary>The test came to false.</summary>
    False,

    /// <summary>
    /// The test is a line of the author's own JavaScript and was not run. The
    /// branch is reported; nothing was evaluated.
    /// </summary>
    ScriptNotRun,

    /// <summary>
    /// The test needs something this reader does not have yet, which is named in
    /// <see cref="MzBranchResult.Missing"/>.
    /// </summary>
    Unknown,
}

/// <summary>A branch's outcome, with whatever is missing said rather than assumed.</summary>
public sealed class MzBranchResult
{
    private MzBranchResult(
        MzBranchOutcome pOutcome, string pMissing, string pComparison, bool pHasComparison)
    {
        Outcome = pOutcome;
        Missing = pMissing;
        Comparison = pComparison;
        HasComparison = pHasComparison;
    }

    public MzBranchOutcome Outcome { get; }
    public string Missing { get; }
    public string Comparison { get; }
    public bool HasComparison { get; }

    internal static MzBranchResult Of(MzBranchOutcome pOutcome) =>
        new(pOutcome, "", "", false);

    internal static MzBranchResult Needs(
        string pMissing, string pComparison, bool pHasComparison) =>
        new(MzBranchOutcome.Unknown, pMissing, pComparison, pHasComparison);

    internal static MzBranchResult Compared(
        MzBranchOutcome pOutcome, string pComparison) =>
        new(pOutcome, "", pComparison, true);
}

/// <summary>
/// The things a branch asks about, as the caller has them.
/// </summary>
/// <remarks>
/// Everything a branch can test is here and nothing else, so a caller cannot
/// satisfy a branch by accident with a value that was not meant for it. A thing
/// that is absent is absent, and a branch that needs it says so.
/// </remarks>
public sealed class MzBranchFacts
{
    public Dictionary<int, bool> Switches { get; init; } = new();
    public Dictionary<int, int> Variables { get; init; } = new();
    public Dictionary<string, bool> SelfSwitches { get; init; } = new();
    public int Gold { get; init; }

    /// <summary>Which of the party's members are in it, by actor number.</summary>
    public HashSet<int> PartyMembers { get; init; } = new();

    /// <summary>What is in the party's bag, by item number and how many.</summary>
    public Dictionary<int, int> Items { get; init; } = new();

    public Dictionary<int, int> Weapons { get; init; } = new();
    public Dictionary<int, int> Armors { get; init; } = new();

    /// <summary>
    /// How long the event's own timer has been running, in seconds, or a
    /// negative number when it is not running.
    /// </summary>
    /// <remarks>
    /// There is no number here, and that is the engine's own arrangement rather
    /// than a simplification. Its timer branch reads the second parameter as a
    /// threshold in seconds and the third as the way, and asks
    /// <c>$gameTimer</c>, which is the one timer the event owns. A reader that
    /// took the second parameter for a timer's number would ask for a timer
    /// called five on a branch about five seconds and refuse a branch it could
    /// have answered.
    /// </remarks>
    public int TimerSeconds { get; init; } = -1;
}
