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
    /// <remarks>
    /// This is a count and not a presence, because the engine's is:
    /// <c>container[item.id] === 0</c> deletes the entry rather than storing a
    /// zero, and a reader that kept a zero would answer <c>hasItem</c>
    /// differently the moment a game asked. <b>A 126 writes here through
    /// <see cref="MzParty"/></b>, which owns the clamp to ninety-nine — this
    /// dictionary is the count, and <see cref="MzParty"/> is the rule.
    /// </remarks>
    public Dictionary<int, int> Items { get; init; } = new();

    /// <summary>
    /// The ninety-nine an item container holds, which is
    /// <c>Game_Party.prototype.maxItems</c> — <c>return 99</c>, with no
    /// argument and no per-item case. **Settable, because the clamp is the
    /// whole of the rule and it is invisible in the middle of the range** — a
    /// test has to be able to ask what happens at the boundary without waiting
    /// for a party to carry ninety-nine of something.
    /// </summary>
    public int MaxItems { get; set; } = 99;

    /// <summary>
    /// The item ids the game actually stores, by their index in
    /// <c>Items.json</c>. **A 126 names one of them, and a reader with no
    /// game behind it must not answer for one it cannot name.**
    /// </summary>
    /// <remarks>
    /// Empty means "nothing is known", **not** "everything exists". A reader
    /// that read an empty set as "every id is fine" would hand a player a
    /// thousand of an item the game never had, and a game that checks
    /// <c>hasItem</c> afterwards would disagree with whatever filled the bag.
    /// So an interpreter with no data refuses a 126 and says which id it could
    /// not hand over — which is what the engine does with an item that is not
    /// there, and this reader says it rather than stepping over it.
    /// </remarks>
    public HashSet<int> KnownItems { get; init; }

    /// <summary>
    /// Whether the ids this game stores are known here. A caller that has read
    /// <c>Items.json</c> says so, and a caller that has not gets a 126 refused
    /// by name rather than answered from nothing.
    /// </summary>
    public bool ItemsAreKnown => KnownItems != null;

    /// <summary>
    /// What is on the screen, and who is looking at it. **A party is a part of
    /// the facts and so is a screen**, for the same reason: the engine has one
    /// <c>$gameScreen</c> and a 231 and a branch asking what is on the screen
    /// must be looking at the same thing.
    /// </summary>
    public MzScreen Screen { get; init; } = new();

    /// <summary>
    /// Whether the party is in a battle, which is the one condition
    /// <c>command351</c> asks before it opens the menu.
    /// </summary>
    public bool InBattle { get; init; }

    /// <summary>
    /// The characters this reader knows, by the id the game uses.
    /// </summary>
    /// <remarks>
    /// <b>The ids are the interpreter's own numbering</b>, from
    /// <c>Game_Interpreter.character</c>: <c>-1</c> is the player, <c>0</c> is
    /// this event, and <c>1..n</c> are the other events by their order on the
    /// map — <b>not</b> the event's id in the file. This game's ninety-six
    /// routes use ids from <c>-1</c> to <c>15</c>, and the same event is a
    /// different number on a different map.
    ///
    /// A 205 naming a character this reader has not got is <b>not</b> an
    /// error: the engine's <c>if (character)</c> guards the work and the
    /// command still returns true.
    /// </remarks>
    public Dictionary<int, MzCharacter> Characters { get; init; } = new();

    /// <summary>
    /// Where the player is, and whether a transfer is still on its way.
    /// </summary>
    public MzPlayer Player { get; init; } = new();

    /// <summary>
    /// Whether a message is on the screen, which is the second thing that stops
    /// a 201 doing anything. <c>command201</c> is
    /// <c>if ($gameParty.inBattle() || $gameMessage.isBusy()) return false;
    /// </c> — the engine transfers nobody in a battle and nobody while a text
    /// box is up, and a reader that moved the player through one would put them
    /// on a new map with the old message still running.
    /// </summary>
    public bool MessageOpen { get; set; }

    /// <summary>
    /// Whether a menu is open, as the last 351 left it.
    /// </summary>
    /// <remarks>
    /// **A menu is a thing a player sees, so a reader that has no screen still
    /// has to be able to say one was asked for.** Without this a 351 would be
    /// indistinguishable from a command with no effect, and a caller asking
    /// "did the game open a menu here" would have nothing to read.
    /// </remarks>
    public MzMenuState Menu { get; set; } = MzMenuState.Closed;

    /// <summary>
    /// Something a command asked for and this reader would not or could not do,
    /// in the order it happened. A caller reads this rather than the log.
    /// </summary>
    public List<string> Notices { get; } = new();

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

    /// <summary>
    /// What a switch holds, read and written by the commands around it, so a
    /// branch two lines after the command that turned one on sees the change.
    /// </summary>
    public void SetSwitch(int pId, bool pOn) => Switches[pId] = pOn;

    /// <summary>What a variable holds, or zero, which is what the engine's own
    /// <c>$gameVariables.value</c> returns for one that was never set.</summary>
    public int Variable(int pId) =>
        Variables.TryGetValue(pId, out var value) ? value : 0;

    /// <summary>Whether a variable was ever set, which the engine cannot tell
    /// apart from one set to zero and this reader can.</summary>
    public bool HasVariable(int pId) => Variables.ContainsKey(pId);

    /// <summary>Writes a variable, so a command that changes it is visible to a
    /// branch that comes after.</summary>
    public void SetVariable(int pId, int pValue) => Variables[pId] = pValue;
}
