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
    /// <summary>The self switches, and where each one belongs.</summary>
    /// <remarks>
    /// <strong>And a self switch belongs to an event on a map, and not
    /// to the game.</strong> Measured at
    /// <c>Game_Interpreter.prototype.command123</c>:
    /// <c>if (this._eventId > 0) { const key = [this._mapId, this._eventId,
    /// params[0]]; $gameSelfSwitches.setValue(key, params[1] === 0); }</c>
    /// — <strong>three numbers, and the first two say whose switch it
    /// is.</strong>
    /// <para>
    /// <strong>And that is why a dictionary keyed by the letter alone
    /// cannot answer a page's condition.</strong> Measured at Map004:
    /// event 9's page 1 asks for switch 3 and event 14 asks for switch 3
    /// too, and both would have been answered by whichever of them ran
    /// last.
    /// </para>
    /// <para>
    /// <strong>And a switch with <c>_eventId &lt;= 0</c> is dropped by
    /// the engine itself</strong> — <strong>which is how a parallel page
    /// of the map, whose event id is zero, cannot set one.</strong>
    /// </para>
    /// </remarks>
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

    /// <summary>How fast the scroll text runs, and what it says.</summary>
    /// <remarks>
    /// <strong>And these come from <c>105 Scroll Text</c>, measured at
    /// <c>command105</c>:</strong> <c>$gameMessage.setScroll(params[0],
    /// params[1]); while (this.nextEventCode() === 405) { this._index++;
    /// $gameMessage.add(this.currentCommand().parameters[0]); }</c> —
    /// <strong>the speed is the first parameter and the second one is the
    /// line the text scrolls over.</strong>
    /// </para>
    /// <para>
    /// <strong>And this project's four 105 commands carry
    /// <c>[1, False]</c> and <c>[3, False]</c> and <c>[2, False]</c>, so
    /// the second value is a boolean here and a string in the engine
    /// where a scroll is a dialogue.</strong>
    /// </para>
    /// </remarks>
    public int ScrollSpeed { get; set; }

    /// <summary>The choice lines a scroll text carries in its 405s.</summary>
    public List<string> ScrollLines { get; set; } = new();

    /// <summary>Which actors have been healed to full.</summary>
    /// <remarks>
    /// <strong>And this comes from <c>314 Recover All</c>, measured at
    /// <c>command314</c>:</strong> <c>this.iterateActorEx(params[0],
    /// params[1], actor =&gt; { actor.recoverAll(); });</c> — <strong>and
    /// <c>iterateActorEx</c> walks the whole party when the second
    /// parameter is set and one actor when it is not.</strong>
    /// </para>
    /// <para>
    /// <strong>And <c>recoverAll</c> is not "healed"</strong> — <strong>it
    /// restores hp, mp and every state</strong>, <strong>and a reader that
    /// wrote only the hit points back is a different command.</strong>
    /// </para>
    /// </remarks>
    public HashSet<int> Recovered { get; } = new();

    /// <summary>
    /// Whether the party is in a battle, which is the one condition
    /// <c>command351</c> asks before it opens the menu.
    /// </summary>
    public bool InBattle { get; private set; }

    /// <summary>
    /// The troop the party is fighting, and the two rules of that fight.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And a battle starts, and so this cannot be an
    /// <c>init</c> field any more.</strong> <c>command351 Open Menu</c>
    /// asks whether the party is in a battle before it opens anything,
    /// <strong>and a field that was set when the facts were built could
    /// never become true</strong>, **so a game that fights and then opens
    /// a menu had its menu open during a fight.**
    /// </para>
    /// <para>
    /// <strong>And the troop is 0 when there is no fight.</strong> The
    /// help for <c>301 Battle Processing</c>: *Causes troops to appear and
    /// starts a battle. Troops — Specify the troop against which the
    /// player will fight.*
    /// </para>
    /// </remarks>
    /// <summary>
    /// The choice a player is looking at, and the number it answers with.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And a choice is three things, and a number alone is not
    /// one.</strong> The engine's own <c>command102</c> reads the option
    /// texts off the following <c>402</c>s, shows them, waits for a key,
    /// and then jumps into the branch of the one that was chosen. <strong>A
    /// reader that kept only the chosen number had no text to show and
    /// nothing to jump into</strong>, **and the event that made the
    /// choice would have gone on down the list as if nothing had been
    /// asked.**
    /// </para>
    /// <para>
    /// <strong>And the engine numbers the branches from one.</strong> The
    /// <c>402</c> that opens a branch carries its own index in its first
    /// parameter — measured: <c>[0, "Yes"]</c>, <c>[1, "No"]</c> — **and
    /// <c>0</c> is not a branch nobody may take, it is the first one.**
    /// </para>
    /// </para>
    /// </remarks>
    public MzOpenChoice? OpenChoice { get; private set; }

    /// <summary>
    /// The options a choice is made of, and nothing while none is open.
    /// </summary>
    public IReadOnlyList<string> ChoiceOptions
    {
        get
        {
            var offen = OpenChoice;
            return offen == null
                ? System.Array.Empty<string>()
                : offen.Options;
        }
    }

    /// <summary>Whether a choice is waiting for the player.</summary>
    public bool ChoicePending => OpenChoice != null;

    /// <summary>The branch a settled choice landed in, or -1.</summary>
    public int ChoiceResult { get; private set; } = -1;

    /// <summary>
    /// Opens a choice, from <c>102 Show Choice List</c>.
    /// </summary>
    /// <param name="pOptions">The option texts, in the game's order.</param>
    /// <param name="pCancel">Whether the last option cancels the choice.</param>
    public void StartChoice(MzChoice.Set pChoice, bool pCancel)
    {
        OpenChoice = new MzOpenChoice(pChoice.Options, pCancel);
        ChoiceResult = -1;
    }

    /// <summary>
    /// Answers the open choice, from a key press, and says which branch
    /// it landed in.
    /// </summary>
    /// <param name="pBranch">Which branch, counting from one.</param>
    /// <returns>Whether the answer was one of the options.</returns>
    public bool AnswerChoice(int pBranch)
    {
        var offen = OpenChoice;
        if (offen == null)
        {
            return false;
        }

        if (pBranch < 1 || pBranch > offen.Options.Count)
        {
            return false;
        }

        OpenChoice = null;
        ChoiceResult = pBranch;
        return true;
    }
    public int BattleTroop { get; private set; }

    /// <summary>Whether the player may flee, from <c>301</c>'s second field.</summary>
    public bool BattleCanEscape { get; private set; }

    /// <summary>
    /// Whether a defeat ends the game, from <c>301</c>'s third field.
    /// </summary>
    /// <remarks>
    /// <strong>And this one is the opposite of what the name
    /// suggests.</strong> The help: *When enabled, there will not be a
    /// game over even if the entire party is defeated.* <strong>So the
    /// field says "losing is allowed", and not "losing is
    /// forbidden"</strong> — **and a reader that named it
    /// <c>CanLose</c> and stored the value as it stands had it
    /// backwards** — **and a game that set the box to survive a defeat
    /// got a game over instead.**
    /// </remarks>
    public bool BattleCanLose { get; private set; }

    /// <summary>
    /// Starts a fight, from <c>301 Battle Processing</c>.
    /// </summary>
    /// <param name="pTroop">Which troop.</param>
    /// <param name="pCanEscape">Whether the escape command works.</param>
    /// <param name="pCanLose">Whether losing ends the game.</param>
    /// <returns>What happened, in a sentence.</returns>
    /// <summary>
    /// Puts the party in a fight without naming a troop, which is what a
    /// test needs when it is not testing the fight.
    /// </summary>
    /// <remarks>
    /// <strong>And this is a door, and the field's setter is not.</strong>
    /// A caller that may set the state outright can also leave it half
    /// set; <strong>this only opens the door the tests need</strong>,
    /// **and every command that starts a fight goes through
    /// <see cref="StartBattle"/>**, **which is the one place the three
    /// fields are set together.**
    /// </remarks>
    public void EnterBattle() => InBattle = true;

    public string StartBattle(int pTroop, bool pCanEscape, bool pCanLose)
    {
        InBattle = true;
        BattleTroop = pTroop;
        BattleCanEscape = pCanEscape;
        BattleCanLose = pCanLose;
        return $"troop {pTroop}, escape "
            + (pCanEscape ? "allowed" : "not allowed")
            + ", and a defeat "
            + (pCanLose ? "ends the game" : "does not end it");
    }

    /// <summary>
    /// Ends a fight, and says how it went, from <c>301</c>'s fourth field.
    /// </summary>
    /// <param name="pLost">Whether the party was defeated.</param>
    public void EndBattle(bool pLost)
    {
        InBattle = false;
        BattleTroop = 0;
        BattleLost = pLost;
    }

    /// <summary>
    /// Whether the last fight was lost, which is what a branch asks.
    /// </summary>
    /// <remarks>
    /// <strong>And the help names two branches, not one.</strong> *You
    /// can also make conditional branches based on [If Player Won] and
    /// [If Player Escaped]*, *and [If Player Lost]*.
    /// <strong>So there are three answers, and this field holds
    /// one</strong>, **and an escaped fight is not a lost one** — **and a
    /// reader that set this true for both gave a game its defeat branch
    /// after the player ran away.**
    /// </remarks>
    public bool BattleLost { get; private set; }

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
    /// The same facts, with figures named under their event numbers.
    /// </summary>
    /// <param name="pCharacters">The figures, by event.</param>
    /// <param name="pPlayer">The player, which is kept.</param>
    /// <returns>The facts with the figures in them.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And the figures are named when a map is read, and not when a
    /// route arrives.</strong> <strong>A <c>205</c> arrives after its page
    /// has already been running for some frames</strong>, <strong>and a
    /// reader that named figures then would miss the first route of every
    /// page.</strong>
    /// </para>
    /// <para>
    /// <strong>And everything else is carried over, because a route must
    /// not lose the switches and variables the page has already
    /// written.</strong>
    /// </para>
    /// </remarks>
    public MzBranchFacts WithCharacters(
        IReadOnlyDictionary<int, MzCharacter> pCharacters, MzPlayer pPlayer)
    {
        return new MzBranchFacts
        {
            Characters = new Dictionary<int, MzCharacter>(pCharacters),
            Player = pPlayer,
            Switches = Switches,
            Variables = Variables,
            SelfSwitches = SelfSwitches,
            Gold = Gold,
            PartyMembers = PartyMembers,
            Items = Items,
            KnownItems = KnownItems,
            Screen = Screen,
        };
    }

    /// <summary>
    /// Which event a <c>205</c>'s first parameter names, as the engine's own
    /// <c>character(param)</c> decides it.
    /// </summary>
    /// <param name="pId">The parameter as the game wrote it.</param>
    /// <param name="pEigenes">The event this page belongs to.</param>
    /// <param name="pFigur">The character the engine would pick, or
    /// nothing.</param>
    /// <returns>Whether a character was named.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And the rule has three parts, and all three were
    /// measured.</strong> <c>Game_Interpreter.prototype.character</c> is
    /// verbatim:
    /// </para>
    /// <list type="bullet">
    /// <item><description>
    /// In a battle it is <strong>nothing</strong>, whatever the number.
    /// </description></item>
    /// <item><description>
    /// Below zero it is <strong>the player</strong> — <strong>and the
    /// measured project says minus one forty-six times</strong>, <strong>so
    /// nearly half of all its routes move the player.</strong>
    /// </description></item>
    /// <item><description>
    /// Zero is <strong>the event the page belongs to</strong>, and a
    /// positive number is the event with that id.
    /// </description></item>
    /// </list>
    /// <para>
    /// <strong>And zero is the part a reader gets wrong most cheaply.</strong>
    /// It looks like "no character", <strong>and it means "me"</strong> —
    /// <strong>and a reader that treated it as none left a page that walks
    /// its own event standing still.</strong>
    /// </para>
    /// </remarks>
    public bool TryNameCharacter(
        int pId, int pEigenes, out MzCharacter? pFigur)
    {
        pFigur = null;
        if (InBattle)
        {
            return false;
        }

        if (pId < 0)
        {
            pFigur = Player.Figur;
            return pFigur != null;
        }

        var gesucht = pId > 0 ? pId : pEigenes;
        if (Characters.TryGetValue(gesucht, out var treffer))
        {
            pFigur = treffer;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Where the player is, and whether a transfer is still on its way.
    /// </summary>
    public MzPlayer Player { get; init; } = new();

    /// <summary>
    /// Counts one frame off everything that has a clock: the balloon
    /// icons over the player and over every figure.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the manual says the icon goes away on its own.</strong>
    /// For <c>213 Show Balloon Icon</c>: *When enabled, the event will be
    /// paused until the balloon icon being displayed has disappeared.*
    /// <strong>And nothing disappears without a clock, and
    /// <c>213</c> carries no duration** — **so a reader that wrote the
    /// icon and never counted it left it over a head for ever**, and the
    /// event that asked to wait for it never came back.
    /// </para>
    /// <para>
    /// <strong>And the figures are a dictionary, and every one of them
    /// is stepped.</strong> The player's and the figures' clocks are
    /// written out twice in the classes,
    /// <strong>and a step that covered only the player left every other
    /// icon on the map for the rest of the game.</strong>
    /// </para>
    /// </remarks>
    public void TickAnimations(int pFrames)
    {
        Player.TickAnimation(pFrames);
        foreach (var character in Characters.Values)
        {
            character?.TickAnimation(pFrames);
        }
    }

    public void TickBalloons(int pFrames)
    {
        Player.TickBalloon(pFrames);
        foreach (var character in Characters.Values)
        {
            character?.TickBalloon(pFrames);
        }
    }

    /// <summary>
    /// The lines read so far, in the order they were read.
    /// </summary>
    /// <remarks>
    /// A reader with no window keeps the lines as data — the words, the number
    /// of times each asks the player to wait, and anything it could not draw.
    /// <b>It does not keep a picture of a window</b>, because a window this
    /// repository cannot draw is a window a caller must not think it has.
    /// </remarks>
    public List<MzMessage.Line> Message { get; } = new();

    /// <summary>
    /// The names a line can ask for, or null when this reader has no database
    /// to answer with.
    /// </summary>
    /// <remarks>
    /// <b>null and empty are different and both are honest.</b> With no
    /// source, a <c>\N[1]</c> becomes an empty name and the line says it
    /// substituted an actor name — which is true. With a source that has no
    /// actors, the same thing happens. What a reader must not do is put a
    /// plausible name in.
    /// </remarks>
    public MzMessage.INameSource? Names { get; set; }

    /// <summary>
    /// Whether a message is on the screen, which is the second thing that stops
    /// a 201 doing anything. <c>command201</c> is
    /// <c>if ($gameParty.inBattle() || $gameMessage.isBusy()) return false;
    /// </c> — the engine transfers nobody in a battle and nobody while a text
    /// box is up, and a reader that moved the player through one would put them
    /// on a new map with the old message still running.
    /// </summary>
        public bool MessageBusy { get; set; }

    /// <summary>
    /// The last dialogue, as <c>command101</c> left it.
    /// </summary>
    /// <remarks>
    /// A reader with no window keeps the block as data: the speaker, the
    /// lines, and what followed them. <b>It does not pretend to be showing
    /// it</b>, and it does not offer a "close" the engine has no method for.
    /// </remarks>
    public MzDialogue.Block? LastDialogue { get; set; }

    /// <summary>
    /// The choices under the last dialogue, as <c>setupChoices</c> left them.
    /// </summary>
    public MzChoice.Set? LastChoice { get; set; }

    /// <summary>
    /// The number to enter or the item to choose, when the last dialogue was
    /// followed by a 103 or a 104.
    /// </summary>
    /// <remarks>
    /// **A separate field from <see cref="LastChoice"/>, and that is the
    /// point.** A 103 asks for a digit count and a 104 for an item id, and
    /// neither is a list of options — a reader that put both through the
    /// choice reader would have counted a 103's `4` as four options. **This
    /// game has neither command anywhere in nineteen maps**, so a reader that
    /// got it wrong would never hear about it from the data.
    /// </remarks>
    public MzPrompt? LastPrompt { get; set; }
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
