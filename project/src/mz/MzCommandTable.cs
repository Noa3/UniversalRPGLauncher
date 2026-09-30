using System;
using System.Collections.Generic;

namespace UniversalRPG.Web;

/// <summary>
/// What a number in an MZ event command list is, and which command owns it.
/// </summary>
/// <remarks>
/// <para>
/// A command list holds two kinds of number and telling them apart is the whole
/// of reading one. A <b>command</b> is dispatched to a method of its own, and
/// <see cref="MzCommandName"/> names every one of them as the engine does. A
/// <b>data</b> number carries what a command above it works on: a line of text
/// under a message, a purchase under a shop, a line of script under a script
/// block. The engine never dispatches those to a method, it reads them inside the
/// command above, and a reader that treated them as commands would run a line of
/// dialogue as if it were an instruction.
/// </para>
/// <para>
/// <b>The owner of each data number was read out of the engine, not derived.</b>
/// Four of the five are three hundred above their command — 401 under 101, 405
/// under 105, 408 under 108, 655 under 355 — and one is not: <b>605 belongs to
/// 302, and three hundred above it is 305, which is a different command
/// entirely.</b> A reader that used the rule rather than the measurement would
/// attach a shop's purchase list to the wrong command, and the rule is not a
/// rule, it is a coincidence in four cases out of five.
/// </para>
/// </remarks>
public static class MzCommandTable
{
    // The numbers the interpreter's own movement depends on. These are the ones
    // that appear in more than one place in the engine's control flow, so a
    // reader that spelled them out twice would be able to disagree with itself.
    public const int ShowText = 111;
    public const int Loop = 112;
    public const int BreakLoop = 113;
    public const int ExitEventProcessing = 115;
    public const int Label = 118;
    public const int JumpToLabel = 119;
    public const int ControlSwitches = 121;
    public const int ControlVariables = 122;
    public const int EndBranch = 412;
    /// <summary>The end of a branch, which the editor writes and which
    /// the engine steps over because it has no method for it.</summary>
    public const int Else = 411;
    public const int RepeatAbove = 413;

    /// <summary>Calls a common event by the index it has in
    /// <c>CommonEvents.json</c>. The engine makes a child interpreter for it,
    /// and the list that called it waits until the child has finished.</summary>
    public const int CommonEvent = 117;

    /// <summary>Puts a picture on the screen. The engine makes a new picture
    /// and puts it in the slot, so anything the old one had is gone with it.</summary>
    public const int ShowPicture = 231;

    /// <summary>Moves a picture to a new place over a number of frames. It
    /// sets a target, so a move of zero frames changes nothing at all.</summary>
    public const int MovePicture = 232;

    /// <summary>Removes a picture from the screen.</summary>
    public const int ErasePicture = 235;

    /// <summary>
    /// A dialogue, and <b>the lines under it are eaten by this command rather
    /// than dispatched</b>: <c>while (this.nextEventCode() === 401) {
    /// this._index++; $gameMessage.add(…); }</c>. There is no
    /// <c>command401</c> for them to be dispatched to, because this one does
    /// it.
    /// </summary>
    /// <remarks>
    /// **And it refuses a second dialogue while one is up**, and takes one
    /// of 102, 103 and 104 — but only the one directly after its last line.
    /// </remarks>
    public const int ShowDialogue = 101;

    /// <summary>
    /// One line of text. **It has no <c>command401</c> method** — the engine
    /// reads it by position, as the text of a 101's line, and
    /// <c>command401</c> does not exist.
    /// </summary>
    public const int ShowTextLine = 401;

    /// <summary>
    /// The choices under a line, again with no method of its own.
    /// </summary>
    /// <summary>
    /// The 102 under a dialogue: <b>the event command that asks the
    /// player</b>. <c>command102</c> calls <c>setupChoices</c> and steps the
    /// index over itself, so a 101 takes it — <b>there is nothing left of it
    /// to dispatch</b>.
    /// </summary>
    /// <remarks>
    /// **And this is the fifth name in five cards that had to be measured
    /// rather than remembered.** `ShowChoices` has been in this table since
    /// K-132, and it was 405 — the 405 that carries the choice list <b>as
    /// data</b>, not the 102 that shows it. A first draft of K-133 compared
    /// the follower against <c>ShowChoices</c>, so not one of this game's
    /// eight choices was ever found: 1352 commands instead of 1360, and a
    /// dialogue that ended on a choice the engine would have taken.
    /// <b>One constant, two meanings, and the tests did not notice for four
    /// runs** — because the ones that failed were the ones checking the sum.
    /// </remarks>
    public const int ShowChoiceList = 102;

    /// <summary>
    /// The 405: <b>the choices as the editor wrote them</b>, under a 102. It
    /// has no <c>command405</c> method, because the 102 that shows them reads
    /// it by position.
    /// </summary>
    public const int ShowChoices = 405;

    /// <summary>
    /// Carries on with the next line, the counterpart of a 401.
    /// </summary>
    public const int ContinueText = 402;

    /// <summary>
    /// Forces a move route onto a character. <c>command205</c> is
    /// <c>$gameMap.refreshIfNeeded(); this._characterId = params[0]; const
    /// character = this.character(params[0]); if (character) { character
    /// .forceMoveRoute(params[1]); if (params[1].wait) setWaitMode("route"); }
    /// return true;</c> — **it returns true even when there is no such
    /// character**, so a route for a character this reader has not got is a
    /// route that goes nowhere and is not an error.
    /// </summary>
    public const int MoveRoute = 205;

    /// <summary>Sends the player to another place. The engine's
    /// <c>command201</c> returns <b>false</b> in a battle or with a message on
    /// the screen, and otherwise only <i>reserves</i> the transfer and sets
    /// <c>setWaitMode("transfer")</c>.</summary>
    public const int TransferPlayer = 201;

    /// <summary>Opens the menu. The engine's own
    /// <c>command351</c> is <c>if (!$gameParty.inBattle()) {
    /// SceneManager.push(Scene_Menu); }</c> — no number changes, and a menu is
    /// a thing the player sees rather than a thing the game computes.</summary>
    public const int OpenMenu = 351;

    /// <summary>Runs a plugin's own code. The engine's <c>command357</c> is
    /// <c>PluginManager.callCommand(this, pluginName, params[1], params[3])
    /// </c>, which is somebody else's JavaScript and is never run here.</summary>
    public const int PluginCommand = 357;

    /// <summary>Runs a line of the author's own JavaScript. The engine's
    /// <c>command355</c> ends in <c>eval(script)</c> and is not run here.</summary>
    public const int Script = 355;

    /// <summary>Changes how many of an item the party has. The engine's
    /// <c>gainItem</c> clamps the count to <c>maxItems</c>, which is
    /// ninety-nine, and deletes the entry when it lands on zero.</summary>
    public const int ChangeItems = 126;

    /// <summary>Waits a number of frames. The engine's
    /// <c>updateWaitCount</c> counts them down one per frame, and a run that
    /// has no frames to count is waiting, not finished.</summary>
    public const int Wait = 230;

    /// <summary>Plays a background music file, from
    /// <c>command241</c>.</summary>
    /// <remarks>
    /// <strong>And the parameter is an object, and not four numbers.</strong>
    /// Measured on a finished project:
    /// <c>241 [{"name":"Scene8","volume":40,"pitch":80,"pan":0}]</c> —
    /// <strong>and XP writes the same command as 11510 with four bit
    /// fields, and a reader that read one of the two forms into the
    /// other produced a track at volume 0 named
    /// <c>{"name"</c>.</strong>
    /// </remarks>
    public const int PlayBgm = 241;

    /// <summary>Fades the background music out, from
    /// <c>command242</c>.</summary>
    public const int FadeOutBgm = 242;

    /// <summary>Plays a background sound, from <c>command245</c>.</summary>
    public const int PlayBgs = 245;

    /// <summary>Fades the background sound out, from
    /// <c>command246</c>.</summary>
    public const int FadeOutBgs = 246;

    /// <summary>Plays a music that plays alone, from
    /// <c>command249</c>.</summary>
    public const int PlayMe = 249;

    /// <summary>Plays a sound effect, from <c>command250</c>.</summary>
    public const int PlaySe = 250;

    /// <summary>Stops the sound effect, from <c>command251</c>.</summary>
    public const int StopSe = 251;

    /// <summary>Controls a self switch, from <c>command123</c>.</summary>
    /// <remarks>
    /// <strong>And the first parameter is a letter and not a number.</strong>
    /// Measured on a finished project: <c>123 ["A", 0]</c> — **and
    /// four letters for four switches, A through D**, **which the
    /// official help names as such**: *Self Switch — Specify the target
    /// self switch (A through D).*
    /// </remarks>
    public const int ControlSelfSwitch = 123;

    /// <summary>Changes the party, from <c>command129</c>.</summary>
    public const int ChangePartyMember = 129;

    /// <summary>Shows a balloon icon over a character, from
    /// <c>command213</c>.</summary>
    /// <remarks>
    /// <strong>And the first parameter is a character, and not an
    /// actor id.</strong> Measured: <c>213 [-1, 2, false]</c> — **and
    /// minus one is the player**, **and the official help says the
    /// position is *based on the position of the player or event*.**
    /// A reader that read the first parameter as an actor id looked up
    /// actor minus one and found nobody.
    /// </remarks>
    public const int ShowBalloonIcon = 213;

    /// <summary>Shows an animation over a character, from
    /// <c>command221</c>.</summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is <c>213</c> with a different name.</strong> The
    /// official help for both says the same three sentences: *Character —
    /// The display location will be based on the position of the player or
    /// event. … Wait for Completion — When enabled, the event will be
    /// paused until the … being displayed has …*.
    /// </para>
    /// <para>
    /// <strong>And so it carries the same state and the same
    /// limits</strong>, **and the help names no duration here either**,
    /// **which is the second place a reader has to make a choice and say
    /// which.**
    /// </para>
    /// </remarks>
    public const int ShowAnimation = 221;

    /// <summary>Erases the running event, from <c>command222</c>.</summary>
    /// <remarks>
    /// <para>
    /// <strong>And this command has no parameters at all.</strong> The
    /// official help: *Temporarily removes the event currently being run.
    /// There are no parameters to set. The event will remain erased until
    /// the party moves to another map.* Measured on a finished project:
    /// sixteen of them, every one with an empty list.
    /// </para>
    /// <para>
    /// <strong>And "until the party moves to another map" is the part a
    /// reader gets wrong.</strong> It is not for ever, and it is not until
    /// something else erases it — **it ends at a map change**, and a
    /// reader that set the flag to false again on the next frame brought
    /// the event back while the player was still looking at it.
    /// </para>
    /// </remarks>
    public const int EraseEvent = 222;

    /// <summary>Moves an event to a tile, from <c>command203</c>.</summary>
    /// <remarks>
    /// <para>
    /// <strong>And this has no page in the official help under that
    /// name, and the help has the page under
    /// <em>Set Event Location</em> instead</strong>, **and the number
    /// in the file is 203.**
    /// </para>
    /// <para>
    /// <strong>And the measured form is
    /// <c>[Event, Place, X, Y, Direction]</c></strong> — ten times in a
    /// finished project, <strong>and <em>Place</em> is 0 in every one of
    /// them</strong>, **which is *Direct Designation*.** The help:
    /// *To move an event to a specific location, select [Direct
    /// Designation], then click [...].*
    /// </para>
    /// </remarks>
    public const int SetEventLocation = 203;

    /// <summary>Starts a fight, from <c>command301</c>.</summary>
    /// <remarks>
    /// <para>
    /// <strong>And the help is short and exact.</strong> *Causes troops
    /// to appear and starts a battle. Troops — Specify the troop against
    /// which the player will fight. Can Escape — When enabled, the
    /// [Escape] command will be enabled during battle. Can Lose — When
    /// enabled, there will not be a game over even if the entire party is
    /// defeated.*
    /// </para>
    /// <para>
    /// <strong>And the measured form is
    /// <c>[0, 7, false, false]</c></strong> — four values, **and the
    /// last two are real JSON booleans**, **which is the same
    /// word-not-number finding that <c>213</c> brought.**
    /// </para>
    /// <para>
    /// <strong>And "Can Lose" is the one whose name lies.</strong> *When
    /// enabled, there will not be a game over* — **so the box says
    /// losing is survivable, and a reader that stored it as "losing is
    /// forbidden" turned a game's escape-from-defeat into a game
    /// over.**
    /// </para>
    /// </remarks>
    public const int BattleProcessing = 301;

    /// <summary>Sets a vehicle's image, from <c>command322</c>.</summary>
    /// <remarks>
    /// <para>
    /// <strong>And the help names two settings, and the second has a
    /// value that is not a file.</strong> *Change the image used for
    /// vehicles. These settings will remain in effect until updated again
    /// by using this event command. Vehicle — Specify the target vehicle.
    /// Images — Double-click the box to specify the image to be displayed.
    /// Setting this to <c>[(None)]</c> will result in no image being
    /// displayed.*
    /// </para>
    /// <para>
    /// <strong>And the measured form is
    /// <c>[1, "MC_Sprite_sheet", 1, "SlimeActors", 5, "Actor1_1"]</c></strong>
    /// — six values, <strong>and the first is the vehicle, which is 1 in
    /// all six, and that is the ship.</strong>
    /// </para>
    /// </remarks>
    public const int ChangeVehicleImage = 322;

    private static readonly Dictionary<int, string> Names = BuildNames();

    /// <summary>The owner of each data number, measured from the engine.</summary>
    /// <summary>
    /// The owner of each data number. Every entry here was measured, from the
    /// engine's own source where it names one and from a real game's own event
    /// lists where it does not, and **not one of them follows a rule**.
    /// </summary>
    /// <summary>
    /// The command that reads each number as its own data.
    /// </summary>
    /// <remarks>
    /// Every entry was measured, from the engine's own source where it
    /// names one and from a real game's own event lists where it does not,
    /// and <b>not one of them follows a rule</b>. Four of the first five are
    /// three hundred above their command, which is how the rule was found,
    /// and the fifth is not: 605 belongs to 302 while three hundred above it
    /// is 305, a different command. The rest are not three hundred above
    /// anything, and 412 belongs to 111 rather than to the 112 that three
    /// hundred above it would name. A reader that used the rule would be
    /// right often enough to look checked and wrong in a place nothing
    /// else would show.
    /// </remarks>
    /// <summary>The command that reads each number as its own data.</summary>
    /// <remarks>
    /// Every entry was measured, from the engine source where it names one
    /// and from a real game own event lists where it does not, and <b>none
    /// of them follows a rule</b>. Some are three hundred above their
    /// command, which is how the rule is found; 605 is not (three hundred
    /// above it is 305, a different command) and 412 is not (three hundred
    /// above it is 112, a loop and not a branch).
    /// </remarks>
    /// <summary>The command that reads each number as its own data.</summary>
    /// <remarks>
    /// A number in this map is one the engine gives <b>no method of its
    /// own</b>, which is how a command and a piece of data are told apart
    /// here. That is a measurement, not a rule, and the rule that is
    /// tempting is wrong in both directions: 605 is three hundred above 605
    /// minus 300 is 305, a different command, and <b>411 and 413 are commands
    /// the engine does dispatch</b> (Else and Repeat Above) while 412 beside
    /// them is not a command at all. A reader that assumed a whole family
    /// was data would refuse a branch and run a structure as an
    /// instruction.
    /// </remarks>
    private static readonly Dictionary<int, int> Owners = new()
    {
        [401] = 101,        // a line of text under Show Text
        [405] = 105,        // a line under Show Scrolling Text
        [408] = 108,        // a line under Comment
        [412] = 111,        // the else of a branch that has one; the engine gives it no method of its own
        [501] = 102,        // one choice under a Show Choices
        [605] = 302,        // a purchase under Shop Processing
        [655] = 355,        // a line of script under Script
        [657] = 355,        // a further line of script under the same block
    };

    /// <summary>How many commands this generation dispatches.</summary>
    public static int Count => Names.Count;

    /// <summary>
    /// The name the engine gives a command, and for a data number the name of the
    /// command that reads it. Null when the number is neither.
    /// </summary>
    public static string? NameOf(int pCode)
    {
        if (Names.TryGetValue(pCode, out var name))
        {
            return name;
        }
        if (Owners.TryGetValue(pCode, out var owner))
        {
            return $"{Names[owner]} ({pCode})";
        }
        return null;
    }

    public static bool IsCommand(int pCode) => Names.ContainsKey(pCode);

    /// <summary>
    /// The command that reads this number as its own data, or 0 when the number
    /// is a command of its own or is nothing this reader knows.
    /// </summary>
    public static int OwnerOf(int pCode) =>
        Owners.TryGetValue(pCode, out var owner) ? owner : 0;

    public static MzCommandKind KindOf(int pCode)
    {
        if (IsCommand(pCode))
        {
            return MzCommandKind.Command;
        }
        if (Owners.ContainsKey(pCode))
        {
            return MzCommandKind.Data;
        }
        return pCode == 0 ? MzCommandKind.Separator : MzCommandKind.Unknown;
    }

    /// <summary>
    /// The name the engine gives a command.
    /// </summary>
    /// <remarks>
    /// A command has exactly one name and it is written once, in the enum
    /// member above. A name in a second table beside it could be changed
    /// there without the enum noticing, and a reader would then hand out a
    /// name no source in this repository carries. There is one place.
    /// </remarks>
    private static Dictionary<int, string> BuildNames()
    {
        // One place a name is written: MzCommandSet. A name in a second
        // table beside the enum drifted from it once already, and the reader
        // handed out the copy that nothing else carried.
        var names = new Dictionary<int, string>();
        foreach (var command in MzCommandSet.Commands)
        {
            names[command.Code] = command.Name;
        }
        return names;
    }
}

/// <summary>What a number in a command list is.</summary>
public enum MzCommandKind
{
    /// <summary>A command the engine dispatches to a method of its own.</summary>
    Command,

    /// <summary>A number another command reads as its own data.</summary>
    Data,

    /// <summary>A command this reader has no name for.</summary>
    Unknown,

    /// <summary>An indent of zero in the editor's own list.</summary>
    Separator,
}
