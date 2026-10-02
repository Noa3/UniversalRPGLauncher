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
/// <summary>
/// The three things a branch slot can hold, and they are not two.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this exists because <c>command403</c> and
/// <c>command411</c> do not agree.</strong>
/// </para>
/// <para>
/// <strong>Measured at <c>rpg_objects.js</c>:</strong>
/// </para>
/// <code>
/// this._branch[this._indent] = result;              // command111
/// if (this._branch[this._indent] === false) { ... } // command111
/// this._branch[indent] = null;                      // jumpTo
/// if (this._branch[this._indent] &gt;= 0) { ... }    // command403
/// if (this._branch[this._indent] !== false) { ... } // command411
/// </code>
/// <para>
/// <strong>And the slot is <c>undefined</c> until one of those writes
/// it</strong>, <strong>and <c>null</c> after a <c>jumpTo</c> crossed
/// the indent</strong>, <strong>and <c>false</c> or <c>true</c> after a
/// <c>111</c> decided it.</strong> <strong>JavaScript tells those last
/// two apart by number</strong> -- <strong><c>ToNumber(null)</c> is
/// <c>0</c> and <c>ToNumber(undefined)</c> is <c>NaN</c> -- <strong>and
/// that is why <c>403</c> skips after a jump and <c>411</c> skips on an
/// untouched indent, and why both skip on a decision.</strong>
/// </para>
/// </remarks>
public enum MzBranchState
{
    /// <summary>Nothing ever wrote this indent. <c>undefined</c>.</summary>
    Undecided = 0,

    /// <summary>
    /// A <c>jumpTo</c> crossed this indent and set it to nothing on
    /// purpose. <c>null</c>.
    /// </summary>
    Crossed = 1,

    /// <summary>A <c>111</c> wrote <c>true</c> or <c>false</c> here.</summary>
    Decided = 2,
}

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

    /// <summary>
    /// The author's own script was not run, and why it was not.
    /// </summary>
    /// <remarks>
    /// <strong>And this is not <see cref="Unknown"/>.</strong>
    /// <strong><c>Unknown</c> says there is something this reader does not
    /// have yet; here there is nothing missing and nothing computed.</strong>
    /// </remarks>
    internal static MzBranchResult ScriptNotRun(string pMissing) =>
        new(MzBranchOutcome.ScriptNotRun, pMissing, "", false);

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

    /// <summary>
    /// The same facts, with the place the run stands written on them.
    /// </summary>
    /// <remarks>
    /// <strong>And this is a copy and not a mutation</strong>, <strong>because
    /// the facts are shared between a page and its child interpreter</strong>
    /// -- <strong>and a child on a common event has no event of its
    /// own</strong>, <strong>so it must be able to say zero where its parent
    /// said seven.</strong>
    /// </remarks>
    public MzBranchFacts At(int pMapId, int pEventId)
    {
        if (MapId == pMapId && EventId == pEventId)
        {
            return this;
        }

        return new MzBranchFacts
        {
            MapId = pMapId,
            EventId = pEventId,
            Switches = Switches,
            Variables = Variables,
            SelfSwitches = SelfSwitches,
            Gold = Gold,
            PartyMembers = PartyMembers,
            Items = Items,
            KnownItems = KnownItems,
            Screen = Screen,
            Characters = Characters,
            Player = Player,
            InBattle = InBattle,
        };
    }
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

    /// <summary>
    /// Where the run stands, and a self switch belongs to that place.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the map and the event the interpreter is
    /// in</strong>, <strong>and a script condition reads both of them</strong>
    /// -- <strong>measured at <c>D:/Itch/sister/www</c>:
    /// <c>$gameSelfSwitches.value([$gameMap.mapId(), this._eventId, 'F'])</c>
    /// fifteen times</strong>, <strong>which is the form that names both
    /// instead of writing them down.</strong>
    /// </para>
    /// <para>
    /// <strong>And zero for both is a common event</strong>, <strong>and a
    /// common event has no self switch and cannot set one</strong> --
    /// <strong>which is the engine's own guard, <c>if (this._eventId &gt;
    /// 0)</c>.</strong>
    /// </para>
    /// </remarks>
    public int MapId { get; init; }

    /// <summary>The event the run is in, or zero for a common event.</summary>
    public int EventId { get; init; }
    /// <summary>
    /// The party's money, and how it got there.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this was <c>init</c> and is not any more</strong>, **because
    /// <c>125 Change Gold</c> writes it** and <strong>a finished game uses it
    /// --- measured at <c>D:/Itch/sister/www</c>: the party of that game is
    /// funded by its own events.</strong>
    /// </para>
    /// </remarks>
    public int Gold { get; set; }

    /// <summary>
    /// The film currently playing, and nothing when none is.
    /// </summary>
    /// <remarks>
    /// <strong>And it is a name and not a number of frames</strong>, because
    /// <c>command261</c> sets <c>setWaitMode('video')</c> and the engine's
    /// <c>updateWaitMode</c> asks <c>Graphics.isVideoPlaying()</c> every frame
    /// until it says no. **A frame count would be a guess about a film
    /// nobody has measured.**
    /// </remarks>
    public string MoviePlaying { get; set; } = "";

    /// <summary>Whether a film has ended and the run may carry on.</summary>
    public void FilmEnded() => MoviePlaying = "";

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
    /// The events the current map carries, and which of them are gone.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is what <c>214</c> removes an event from</strong>,
    /// <strong>and it is not the same thing as <c>222</c>, which removes
    /// the running event until the party leaves the map</strong> -- <strong>
    /// and both carry no parameters, so a reader that gave them one meaning
    /// could not tell them apart.</strong>
    /// </para>
    /// <para>
    /// <para>
    /// <strong>And the engine's line is
    /// <c>$gameMap.eraseEvent(this._eventId)</c></strong>, <strong>which
    /// takes the event out of the map's own list and does not touch
    /// anything else</strong> -- <strong>a treasure chest that was opened
    /// and a door that was walked through are gone from the map and come
    /// back when the party returns to it.</strong>
    /// </para>
    /// <para>
    /// <strong>And it needs both the map and the event, and a common event
    /// has neither</strong> -- <strong>which is why the engine guards it
    /// with <c>isOnCurrentMap() &amp;&amp; this._eventId &gt; 0</c> and not
    /// with a check on the list.</strong>
    /// </para>
    /// </remarks>
    public MzMapState Map { get; init; } = new();

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
    /// The states that went onto actors and off them, in the order the
    /// commands asked.
    /// </summary>
    /// <remarks>
    /// <strong>And a list and not a set</strong>, <strong>because the same
    /// state can go onto the same actor twice in a row</strong> -- <strong>
    /// and a page that adds poison, takes it away and adds it again is three
    /// commands and not one.</strong>
    /// </remarks>
    public List<MzStateChange> States { get; } = new();

    /// <summary>
    /// The HP orders the run gave, in order, and no party's health.
    /// </summary>
    /// <remarks>
    /// <strong>And this is what <c>311</c> records</strong> -- <strong>and
    /// not an HP total</strong>, <strong>because the engine's own
    /// <c>gainHp</c> clamps to <c>_hp</c> and <c>_mhp</c></strong>,
    /// <strong>and those two numbers come from
    /// <c>Actors.json</c> and <c>Classes.json</c></strong>, <strong>and
    /// this repository does not keep an actor.</strong>
    /// </remarks>
    public List<MzHpOrder> HpOrders { get; } = new();

    /// <summary>The MP orders the run gave, in order.</summary>
    public List<MzActorOrder<string>> MpOrders { get; } = new();

    /// <summary>The experience orders the run gave, in order.</summary>
    public List<MzActorOrder<string>> ExpOrders { get; } = new();

    /// <summary>The level orders the run gave, in order.</summary>
    public List<MzActorOrder<string>> LevelOrders { get; } = new();

    /// <summary>
    /// The parameter orders the run gave, with the parameter's own id.
    /// </summary>
    /// <remarks>
    /// <strong>And <c>317</c>s zweiter Platz ist die Nummer des
    /// Parameters</strong> -- <strong>0 ist MHP, 1 MMP, alles andere
    /// ein Attribut</strong>, <strong>und <c>paramMax</c> gibt 999999,
    /// 9999 und 999.</strong>
    /// </remarks>
    public List<MzActorOrder<int>> ParamOrders { get; } = new();

    /// <summary>
    /// Where the game is showing, and what it can go back to.
    /// </summary>
    /// <remarks>
    /// <strong>And <c>352</c>, <c>354</c> and <c>351</c> all push or
    /// goto a scene</strong>, <strong>and <c>push</c> and <c>goto</c>
    /// are different operations</strong> -- <strong>so this is a stack
    /// and not a flag.</strong>
    /// </remarks>
    public MzSceneStack Szene { get; } = new();

    /// <summary>
    /// What <c>$gameMap._scrollRest</c> holds, which is the whole of
    /// <c>204</c>.
    /// </summary>
    public MzMapScroll Rollen { get; } = new();

    /// <summary>
    /// How the map looks, and not what is on it.
    /// </summary>
    /// <remarks>
    /// <strong>And <c>281</c>, <c>282</c> und <c>284</c> schreiben
    /// hierhin</strong> -- <strong>und das sind die ersten drei Befehle
    /// dieser Reihe, die kein Darsteller und kein Kartenstand
    /// anfassen.</strong>
    /// </remarks>
    public MzMapDisplay Anzeige { get; } = new();

    /// <summary>
    /// Whether the run is holding a reservation on tileset images, and the
    /// engine's own <c>_imageReservationId</c>.
    /// </summary>
    /// <remarks>
    /// <strong>And this is what makes <c>282</c> different from every
    /// other command.</strong> <strong><c>command282</c> reserves the
    /// images, returns, and is read again next frame until they are
    /// ready</strong> -- <strong>and <c>return true</c> is outside the
    /// <c>if</c></strong>, <strong>so the engine never waits and never
    /// gives up</strong>.
    /// </remarks>
    public bool BilderReserviert { get; set; }

    /// <summary>
    /// The names <c>320</c> gave the actors, and the actor as the key.
    /// </summary>
    /// <remarks>
    /// <strong>And <c>setName(name)</c> ist <c>this._name =
    /// name;</c></strong> -- <strong>und <c>actor.setName</c> macht
    /// noch etwas</strong>, <strong>das davon abhaengt, ob der
    /// Darsteller im Helden steht oder nicht**, <strong>und das
    /// braucht die <c>Actors.json</c> dieses Spiels.</strong>
    /// </remarks>
    public Dictionary<int, string> Namen { get; } = new();

    /// <summary>
    /// The troop in the battle, and it is <c>$gameTroop</c> for the
    /// commands that change enemies.
    /// </summary>
    /// <remarks>
    /// <strong>And this is where the three battle outcomes
    /// land</strong> -- <strong><c>301</c> hands
    /// <c>BattleManager.setEventCallback(function(n) { this._branch
    /// [this._indent] = n; })</c></strong>, <strong>and that is where
    /// <c>601</c>, <c>602</c> and <c>603</c> read it.</strong>
    /// </remarks>
    public MzBattle Kampf { get; } = new();

    /// <summary>
    /// Who the name editor was opened for, and what it was given.
    /// </summary>
    /// <remarks>
    /// <strong>And these are <c>SceneManager.prepareNextScene(
    /// params[0], params[1])</c>'s two arguments</strong> -- <strong>the
    /// actor's number and the new name</strong> -- <strong>and not a
    /// field slot and a text.</strong>
    /// </remarks>
    public int NamensZiel { get; set; }

    /// <summary>And the name it was given, in whatever the game wrote.</summary>
    public string NamensText { get; set; } = "";

    /// <summary>
    /// The goods the last <c>302</c> was given, and its own parameters
    /// first.
    /// </summary>
    /// <remarks>
    /// <strong>And this is the engine's own <c>goods</c> array</strong>:
    /// <strong><c>const goods = [this._params]</c> und dann eine Zeile je
    /// <c>605</c>.</strong> <strong>Und jede Zeile ist kein Befehl,
    /// sondern eine Warenzeile</strong> -- <strong>und
    /// <c>prepareNextScene(goods, this._params[4])</c> gibt beides an
    /// die Ladenszene.</strong>
    /// </remarks>
    public List<List<string>> LetzterLaden { get; set; } = new();

    /// <summary>
    /// What <c>$gameSystem</c> and <c>$gameTimer</c> carry.
    /// </summary>
    /// <remarks>
    /// <strong>And <c>134</c>, <c>135</c>, <c>138</c> and <c>124</c> all
    /// write here</strong> -- <strong>and this game writes them 255, 111,
    /// 135 and 35 times.</strong>
    ///
    /// <strong>And it is called <c>Spiel</c> and not <c>System</c></strong>
    /// -- <strong>because a field called <c>System</c> inside this class
    /// shadows the <c>System</c> namespace for every member that reads
    /// it</strong>, <strong>and the first thing that broke was
    /// <c>System.Array.Empty&lt;string&gt;()</c> nine lines further
    /// up</strong> -- <strong>which is this repository's own
    /// <c>128 Change Armor</c> neighbour reading an empty option
    /// list.</strong>
    /// </remarks>
    public MzSystem Spiel { get; } = new();

    /// <summary>
    /// <c>$gamePlayer.makeEncounterCount()</c>, and <c>136</c> throws it
    /// back.
    /// </summary>
    /// <remarks>
    /// <strong>And that throw is the whole difference between
    /// <c>136</c> and <c>137</c></strong> -- <strong>a reader that only
    /// flips the flag makes the next fight start one tile later.</strong>
    /// </remarks>
    public int Begegnungszaehler { get; set; }

    /// <summary>
    /// The nickname, and it is not the name.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the engine's three are three separate
    /// assignments:</strong>
    /// </para>
    /// <code>
    /// setName(name)       { this._name = name; }       // command320
    /// setNickname(nick)   { this._nickname = nick; }   // command324
    /// setProfile(profile) { this._profile = profile; } // command325
    /// </code>
    /// <para>
    /// <strong>And a reader that put a nickname in the name field would
    /// lose both.</strong>
    /// </para>
    /// </remarks>
    public Dictionary<int, string> Spitznamen { get; } = new();

    /// <summary>And the third one, which is a face rather than a name.</summary>
    public Dictionary<int, string> Profile { get; } = new();

    /// <summary>
    /// <c>$gameActors.actor(n)._tp</c>, and it is a number per actor.
    /// </summary>
    /// <remarks>
    /// <strong>And it starts at zero</strong> -- <strong>and
    /// <c>Game_Actor.prototype.initialize</c> says
    /// <c>this._tp = 0;</c></strong> -- <strong>and a game gains
    /// tactical points from skills, not from the start.</strong>
    /// </remarks>
    public Dictionary<int, int> Taktischpunkte { get; } = new();

    /// <summary>
    /// The class each actor stands in, as a 321 ordered it.
    /// </summary>
    /// <remarks>
    /// <strong>And the order, not the class.</strong> <strong>MV's
    /// <c>changeClass</c> takes the class id and a flag, and the class's
    /// own levels come from <c>Classes.json</c></strong>, <strong>and
    /// this repository keeps no actor</strong> -- <strong>so it keeps
    /// that a change was asked for and by whom.</strong>
    /// </remarks>
    public List<(int Actor, int Class)> Classes { get; } = new();

    /// <summary>
    /// What each actor wears, as a 319 ordered it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And <c>changeEquipById</c> takes a slot type and an item
    /// id, and no direction at all.</strong> Measured at
    /// <c>rpg_objects.js</c>:
    /// </para>
    /// <code>
    /// changeEquipById(etypeId, itemId) {
    ///     const slotId = etypeId - 1;
    ///     if (this.equipSlots()[slotId] === 1) {
    ///         this.changeEquip(slotId, $dataWeapons[itemId]);
    ///     } else {
    ///         this.changeEquip(slotId, $dataArmors[itemId]);
    ///     }
    /// }
    /// </code>
    /// <para>
    /// <strong>And that is the whole of it</strong> -- <strong>a slot, a
    /// type decided by that slot's own <c>equipSlots()</c> entry, and an
    /// item id.</strong> <strong>My first version read the third
    /// parameter as "equip or unequip"</strong> -- <strong>which is the
    /// reading of <c>129 Change Party Members</c>, and of
    /// <c>313</c>'s third parameter</strong>, <strong>and of neither of
    /// the two neighbours of this command.</strong>
    /// </para>
    /// </remarks>
    public HashSet<(int Actor, int Item)> Equipment { get; } = new();

    /// <summary>
    /// Which characters were asked for an animation.
    /// </summary>
    /// <remarks>
    /// <strong>And the number of the animation is not here</strong> --
    /// <strong>it lives in the character, which is where the engine keeps
    /// it</strong>, <strong>and a second copy here would be a second place to
    /// get it wrong.</strong>
    /// </remarks>
    public List<int> AnimationAsked { get; } = new();

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
        var kopie = new MzBranchFacts
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

            // **Und der Zustand, der eine Seite geschrieben hat und der
            // eine andere noch liest, wird mitgenommen.**
            //
            // **Und gemessen ist, dass `WithCharacters` neun Felder
            // mitnahm und alles andere fallen liess** -- **und
            // `Repaint` ruft es bei jedem Kartenwechsel** -- **und
            // also war `ScrollLines` nach dem Umzug leer, obwohl die
            // Seite es fuenf Befehle vorher gefuellt hatte.**
            //
            // **Und das ist kein Zufall: `ScrollLines` ist eine
            // veraenderliche Liste mit `set`, und eine
            // Initialisierer-Zuweisung traegt den alten Inhalt nicht.**
            ScrollSpeed = ScrollSpeed,
            ScrollLines = ScrollLines,

            // **Und die Wartebedingungen des Bildschirms** -- **denn
            // `MessageBusy` und `InBattle` entscheiden, ob `201`
            // ueberhaupt umzieht.**
            MessageBusy = MessageBusy,
            InBattle = InBattle,
            BattleCanEscape = BattleCanEscape,
            BattleCanLose = BattleCanLose,
            BattleLost = BattleLost,
            BattleTroop = BattleTroop,
            Menu = Menu,
            ChoiceResult = ChoiceResult,
            OpenChoice = OpenChoice,
            LastPrompt = LastPrompt,
            TimerSeconds = TimerSeconds,
        };

        // **Und die geheilten Darsteller kommen mit** -- **denn das ist
        // eine nur-lesende Menge, und eine Kopie, die sie nicht
        // befuellt, verliert genau das, worauf ein spaeterer `314`
        // prueft.**
        foreach (var darsteller in Recovered)
        {
            kopie.Recovered.Add(darsteller);
        }

        // **Und die Zustandswechsel kommen mit** -- **denn `313` darf in
        // einem gemeinsamen Ereignis stehen, und ein Kindlauf, der seine
        // Zustandswechsel verliert, wuerde ein Gift, das er gegeben hat,
        // nicht mehr sehen.**
        foreach (var wechsel in States)
        {
            kopie.States.Add(wechsel);
        }

        // **Und die angefragten Animationen auch**, **denn `212` mit
        // Warten wartet in derselben Schleife, in der der Kindlauf
        // laeuft.**
        foreach (var wen in AnimationAsked)
        {
            kopie.AnimationAsked.Add(wen);
        }

        return kopie;
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

    /// <summary>
    /// Which skills actors have learned, and which they have forgotten.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is a third container and not the state's
    /// list.</strong> <c>command318</c> is <c>actor.learnSkill(id)</c> --
    /// <strong>and a skill is a thing an actor has and not a thing the
    /// party has.</strong>
    /// </para>
    /// <para>
    /// <strong>And the key is both sides of it</strong>, <strong>because
    /// "actor one learns skill three" and "actor three learns skill one"
    /// are different facts and one number cannot hold both.</strong>
    /// </para>
    /// </remarks>
    public HashSet<(int Darsteller, int Skill)> Skills { get; } = new();

    /// <summary>
    /// The music that <c>243</c> put aside, and <c>244</c> puts back.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the whole of what <c>243</c> does</strong>:
    /// <c>$gameSystem.saveBgm()</c> <strong>copies the current track into a
    /// slot and changes nothing the player can see.</strong>
    /// </para>
    /// <para>
    /// <strong>And a reader with no such slot has nowhere to put
    /// anything</strong>, <strong>and every 243 in a game would be reported
    /// as finished and have changed nothing.</strong>
    /// </para>
    /// </remarks>
    public string RememberedBgm { get; set; } = "";

    /// <summary>
    /// Whether <c>243</c> has put a track aside, and a flag and not the
    /// empty string because an empty name is a real answer.
    /// </summary>
    public bool HasRememberedBgm { get; set; }
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
