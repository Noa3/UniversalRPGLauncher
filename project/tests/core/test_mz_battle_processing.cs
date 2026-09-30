using System;
using System.Collections.Generic;
using UniversalRPG.Tests.Framework;
using UniversalRPG.Web;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// <c>301 Battle Processing</c>, the seventh of seven character and
/// battle commands a finished MZ project still used that this reader
/// could not run.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the help is short.</strong> *Causes troops to appear and
/// starts a battle. Troops — Specify the troop against which the player
/// will fight. Can Escape — When enabled, the [Escape] command will be
/// enabled during battle. Can Lose — When enabled, there will not be a
/// game over even if the entire party is defeated.*
/// </para>
/// <para>
/// <strong>And the measured form is <c>[0, 7, false, false]</c></strong>
/// — four values, <strong>and the last two are real JSON booleans</strong>,
/// which is the same word-not-number finding <c>213</c> brought.
/// </para>
/// </remarks>
public partial class TestMzBattleProcessing : TestBase
{
    /// <summary>
    /// A fight starts, and the two rules of it are stored.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And "Can Lose" is the one whose name lies.</strong> *When
    /// enabled, there will not be a game over even if the entire party
    /// is defeated* — <strong>so the box says losing is
    /// survivable.</strong>
    /// </para>
    /// <para>
    /// <strong>And a reader that stored it as "losing is forbidden"
    /// turned a game's escape from a defeat into a game over</strong> —
    /// which is the one way a field can be right in its type and wrong
    /// in its sense.
    /// </para>
    /// </remarks>
    public void Test_EinKampfBeginntUndSeineZweiRegelnWerdenGemerkt()
    {
        var fakten = new MzBranchFacts();
        var lauf = new MzInterpreter(new List<MzCommandEntry> { });
        var aktionen = new List<MzAction>();

        // **Und die gemessene Form: [0, 7, false, false].**
        AssertTrue(MzCommands.TryExecute(lauf,
                new MzCommandEntry(301, ["0", "7", "false", "false"], 0),
                aktionen, fakten, new MzRandom()),
            "**and the command runs**");

        AssertTrue(fakten.InBattle,
            "**and the party is in a battle** -- and the field could not be "
                + "set before, so a game that fought and then opened a "
                + "menu had its menu open during a fight");
        AssertEq(fakten.BattleTroop, 7,
            "**and it is troop seven** -- and the first value is the way of naming a troop, and the second is the troop");
        AssertTrue(!fakten.BattleCanEscape,
            "**and the escape command is off**");
        AssertTrue(!fakten.BattleCanLose,
            "**and a defeat ends the game** -- and the help says *when "
                + "enabled, there will not be a game over*, so the box "
                + "means losing is survivable and not that losing is "
                + "allowed");
    }

    /// <summary>
    /// Both boxes, the other way round.
    /// </summary>
    /// <remarks>
    /// <strong>And the booleans are real JSON booleans, and the third and
    /// fourth value are the ones that lie.</strong> Checked both ways,
    /// because a field that only ever sees <c>false</c> in a game's file
    /// has not been tested at all.
    /// </remarks>
    public void Test_DieBeidenKaestenInDerGegenrichtung()
    {
        var fakten = new MzBranchFacts();
        var lauf = new MzInterpreter(new List<MzCommandEntry> { });
        MzCommands.TryExecute(lauf,
            new MzCommandEntry(301, ["0", "3", "true", "true"], 0),
            new List<MzAction>(), fakten, new MzRandom());

        AssertTrue(fakten.BattleCanEscape,
            "**and the escape command is on** -- and it is the second value, and the first is the way of naming a troop");
        AssertTrue(fakten.BattleCanLose,
            "**and a defeat does not end the game** -- and this is the wording of the help: *when enabled, there will not be a game over*");
    }

    /// <summary>
    /// A second fight does not start.
    /// </summary>
    /// <remarks>
    /// <strong>And the engine starts no second one.</strong> A troop
    /// that appears while another is already fighting has nothing to
    /// fight, <strong>and a reader that started it left the party in two
    /// battles at once, with one troop id and no way back.</strong>
    /// </remarks>
    public void Test_EinZweiterKampfBeginntNicht()
    {
        var fakten = new MzBranchFacts();
        var lauf = new MzInterpreter(new List<MzCommandEntry> { });
        MzCommands.TryExecute(lauf,
            new MzCommandEntry(301, ["0", "3", "true", "true"], 0),
            new List<MzAction>(), fakten, new MzRandom());
        AssertEq(fakten.BattleTroop, 3, "**and the first troop is there**");

        var aktionen = new List<MzAction>();
        MzCommands.TryExecute(lauf,
            new MzCommandEntry(301, ["0", "9", "true", "true"], 0),
            aktionen, fakten, new MzRandom());

        AssertEq(fakten.BattleTroop, 3,
            "**and the second troop does not replace it** -- and a reader "
                + "that started it left the party in two battles with one "
                + "troop id and no way back");
        AssertEq(fakten.Notices.Count, 1,
            "**and it is said**");
    }

    /// <summary>
    /// Ending a fight remembers whether it was lost.
    /// </summary>
    /// <remarks>
    /// <strong>And the help names three answers, not two.</strong> *You
    /// can also make conditional branches based on [If Player Won] and
    /// [If Player Escaped]* — *and [If Player Lost]*. <strong>An escaped
    /// fight is not a lost one</strong>, **and a reader that set the lost
    /// flag for both handed a game its defeat branch after the player ran
    /// away.**
    /// </remarks>
    public void Test_EinBeendeterKampfMerktSichObErVerlorenWurde()
    {
        var fakten = new MzBranchFacts();
        var lauf = new MzInterpreter(new List<MzCommandEntry> { });
        MzCommands.TryExecute(lauf,
            new MzCommandEntry(301, ["0", "3", "true", "true"], 0),
            new List<MzAction>(), fakten, new MzRandom());

        fakten.EndBattle(true);
        AssertTrue(!fakten.InBattle, "**and the party is out of it**");
        AssertEq(fakten.BattleTroop, 0,
            "**and the troop is gone** -- and a reader that left the id "
                + "standing named a battle that is not running");
        AssertTrue(fakten.BattleLost,
            "**and the loss is remembered** -- and that is the branch the "
                + "help calls *If Player Lost*");

        // **Und ein gewonnener Kampf ist kein verlorener.**
        fakten.EndBattle(false);
        AssertTrue(!fakten.BattleLost,
            "**and a won fight is not a lost one** -- and the help names "
                + "*If Player Won* as a branch of its own, and a reader "
                + "with one flag for both gave a game its defeat branch "
                + "after a win");
    }
}
