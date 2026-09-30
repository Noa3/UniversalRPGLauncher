using System;
using System.Collections.Generic;
using UniversalRPG.Tests.Framework;
using UniversalRPG.Web;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// <c>102 Show Choice List</c> and the block that follows it.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is not one command but a block</strong>, which is why
/// <c>402</c>, <c>404</c>, <c>405</c> and <c>412</c> were documented as
/// no-ops rather than as missing: <strong>they are parts of a block, and
/// the block did not run.</strong>
/// </para>
/// <para>
/// <strong>And the measured shape</strong>, from
/// <c>CamelliaCoronation</c>, <c>Map004</c> event 14:
///
/// <code>
/// 401 ["(Done looking around for today?)"]
/// 102 [["Yes", "No"], 1, 0, 2, 0]     indent 0
/// 402 [0, "Yes"]                     indent 0
/// 221 []                             indent 1
/// 101 ["", 0, 0, 2, ""]              indent 1
/// 402 [1, "No"]                      indent 0
/// 404 []                             indent 0
/// 412 []                             indent 1
/// </code>
///
/// <strong>And the texts stand twice, and both places hold all of
/// them</strong> — once as a list in <c>params[0]</c> and once on the
/// <c>402</c> lines.
/// </para>
/// <para>
/// <strong>And <c>402</c> is two different commands under one number.</strong>
/// Inside a <c>401</c> block it carries on with the next line; inside a
/// <c>102</c> block it is one option, carrying its branch index and its
/// text.
/// </para>
/// </remarks>
public partial class TestMzChoice : TestBase
{
    /// <summary>
    /// The measured block, so that a test of the choice is a test of the
    /// real shape.
    /// </summary>
    /// <remarks>
    /// <strong>And a test that builds only the <c>102</c> tests a
    /// program no game writes.</strong> The real block is a <c>401</c>,
    /// the <c>102</c>, then one <c>402</c> per option with its branches
    /// indented, then a <c>404</c> for the else and a <c>412</c>.
    /// </remarks>
    private static List<MzCommandEntry> Block()
    {
        return new List<MzCommandEntry>
        {
            new(401, ["(Done looking around for today?)"], 0),
            new(102, ["[\"Yes\", \"No\"]", "1", "0", "2", "0"], 0),
            new(402, ["0", "Yes"], 0),
            new(221, [], 1),
            new(101, ["", "0", "0", "2", ""], 1),
            new(402, ["1", "No"], 0),
            new(404, [], 0),
            new(412, [], 1),
        };
    }

    /// <summary>
    /// A choice of two offers two texts and holds the list.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the second parameter is the cancel branch, and not
    /// the number of options.</strong> Measured
    /// <c>[["Yes", "No"], 1, 0, 2, 0]</c>: the <c>1</c> is a branch
    /// index, <strong>and a reader that read it as a count opened a
    /// choice of one</strong> and showed the player a single answer to a
    /// question that had two.
    /// </para>
    /// <para>
    /// <strong>And the option list is a nested object, and not a
    /// string.</strong> <c>["Yes", "No"]</c> arrives as written JSON,
    /// <strong>and a reader that split a string on a separator found one
    /// option that reads <c>["Yes", "No"]</c> with its brackets and its
    /// comma in it.</strong>
    /// </para>
    /// </remarks>
    public void Test_EineWahlBietetIhreTexteAn()
    {
        var fakten = new MzBranchFacts();
        var lauf = new MzInterpreter(Block());
        lauf.Setup(1, 14);
        lauf.Index = 1;
        var aktionen = new List<MzAction>();

        AssertTrue(MzCommands.HasEffect(102),
            "**and the command is one this reader runs**");

        // **Und die gemessene Form: `102 [["Yes", "No"], 1, 0, 2, 0]`.**
        AssertEq(lauf.ChoiceOptions.Count, 0,
            "**and no choice is open before the block runs**");

        MzControlFlow.TryExecute(lauf, lauf.Commands[1], aktionen, fakten);
        AssertTrue(fakten.ChoicePending,
            "**and a choice is open**");
        AssertEq(fakten.ChoiceOptions.Count, 2,
            "**and the player is asked two things** -- and the nested "
                + "object is read back as two options, and it is an object "
                + "and not a string, and a reader that split a string found "
                + "one option reading [\"Yes\", \"No\"] with its brackets "
                + "in it");
        AssertTrue(fakten.ChoiceOptions[0] == "Yes"
                && fakten.ChoiceOptions[1] == "No",
            "**and in the game's own order**");
    }

    /// <summary>
    /// An answer lands in the branch of the option, counted from one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the branches count from one, and their indices in the
    /// file count from zero.</strong> Measured <c>402 [0, "Yes"]</c> and
    /// <c>402 [1, "No"]</c> — <strong>so the first option's branch is
    /// the answer 1, and a reader that used the file's zero as the
    /// answer number made the second option unreachable.</strong>
    /// </para>
    /// <para>
    /// <strong>And an answer outside the list is no answer.</strong> A
    /// choice of two does not take a 3, <strong>and a reader that let it
    /// jump into whatever followed the block ran commands the player's
    /// key never chose.</strong>
    /// </para>
    /// </remarks>
    public void Test_EineAntwortLandetImZweigDerOption()
    {
        var fakten = new MzBranchFacts();
        fakten.StartChoice(
            MzChoice.Read(new MzCommandEntry(102, ["[\"Yes\", \"No\"]", "1"], 0)
                .Parameters),
            true);

        AssertTrue(fakten.AnswerChoice(1),
            "**and the first answer is taken**");
        AssertEq(fakten.ChoiceResult, 1,
            "**and it is the first branch** -- and the file counts its "
                + "options from zero, and a reader that used the file's "
                + "zero as the answer number made the second option "
                + "unreachable");
        AssertTrue(!fakten.ChoicePending,
            "**and no choice is open any more**");

        // **Und eine Zahl, die es nicht gibt, ist keine Antwort.**
        var zweite = new MzBranchFacts();
        zweite.StartChoice(
            MzChoice.Read(new MzCommandEntry(102, ["[\"Yes\", \"No\"]", "1"], 0)
                .Parameters),
            true);
        AssertTrue(!zweite.AnswerChoice(3),
            "**and a third answer to a choice of two is refused** -- and a "
                + "reader that let it through jumped into whatever followed "
                + "the block, and the player never chose that");
        AssertTrue(!zweite.AnswerChoice(0),
            "**and so is a zero** -- and the file counts from zero, and a "
                + "reader that let zero through had a branch for nothing");
        AssertTrue(zweite.ChoicePending,
            "**and the choice is still open**");
    }

    /// <summary>
    /// A choice with no option is refused, and not shown empty.
    /// </summary>
    /// <remarks>
    /// <strong>And the engine would show the player an empty list.</strong>
    /// That is a game that stops with a question on the screen and
    /// nothing to answer it with — <strong>and a reader that opened the
    /// choice anyway left the event waiting for a key that can never
    /// arrive.</strong>
    /// </remarks>
    public void Test_EineWahlOhneOptionWirdAbgewiesen()
    {
        var fakten = new MzBranchFacts();
        var lauf = new MzInterpreter(Block());
        lauf.Setup(1, 14);
        lauf.Index = 1;
        var aktionen = new List<MzAction>();

        MzControlFlow.TryExecute(lauf,
            new MzCommandEntry(102, ["[]", "1"], 0), aktionen, fakten);

        AssertTrue(!fakten.ChoicePending,
            "**and no choice is open** -- and a reader that opened it "
                + "anyway showed the player a question with nothing to "
                + "answer it with");
    }

    /// <summary>
    /// A choice with no branch is refused too.
    /// </summary>
    /// <remarks>
    /// <strong>And the texts are not enough.</strong> A choice of two
    /// with no <c>402</c> has nothing to jump into when the player
    /// answers, <strong>and a reader that waited anyway left the event
    /// waiting for a key whose answer goes nowhere.</strong>
    /// </remarks>
    public void Test_EineWahlOhneZweigWirdAbgewiesen()
    {
        var fakten = new MzBranchFacts();
        // **Und `102` allein, ohne eine einzige `402` danach.**
        var lauf = new MzInterpreter(new List<MzCommandEntry>
        {
            new(102, ["[\"Yes\", \"No\"]", "1"], 0),
        });
        lauf.Setup(1, 14);
        var aktionen = new List<MzAction>();

        MzControlFlow.TryExecute(lauf, lauf.Commands[0], aktionen, fakten);

        AssertTrue(!fakten.ChoicePending,
            "**and no choice is open** -- and the texts alone are not a "
                + "choice, and a reader that waited anyway left the event "
                + "waiting for a key whose answer goes nowhere");
    }

    /// <summary>
    /// Each option leads somewhere, and the two lead somewhere else.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the texts are not enough, and the branches are not
    /// enough either.</strong> What a choice does is
    /// <em>put the player in one of its branches</em>, and a reader that
    /// kept only the texts showed the player a question whose answer
    /// went nowhere.
    /// </para>
    /// <para>
    /// <strong>And this is the test the living mutation rule asked
    /// for.</strong> The three tests above ask the facts object what the
    /// player is being asked, and <strong>none of them asks the
    /// interpreter where an answer leads</strong> — and the code had the
    /// map in it, and nothing reached it.
    /// </para>
    /// <para>
    /// <strong>And the answer counts from one while the file counts from
    /// zero.</strong> So <c>402 [0, "Yes"]</c> is where answer 1 leads,
    /// <strong>and a reader that kept the file's number jumped into the
    /// first branch for every answer</strong> — which is the mutation
    /// that survived.
    /// </para>
    /// </remarks>
    public void Test_JedeOptionFuehrtWoandershin()
    {
        var lauf = new MzInterpreter(Block());
        lauf.Setup(1, 14);
        lauf.Index = 1;
        MzControlFlow.TryExecute(lauf, lauf.Commands[1],
            new List<MzAction>(), new MzBranchFacts());

        AssertTrue(lauf.HasChoice,
            "**and the interpreter holds the choice**");
        AssertEq(lauf.ChoiceOptions.Count, 2,
            "**and both options**");

        // **Und der Block: `401` an 0, `102` an 1, `402 [0, "Yes"]` an
        // 2, ihre Zweige an 3 und 4, `402 [1, "No"]` an 5, `404` an 6,
        // `412` an 7.**
        var erste = lauf.ChoiceBranch(1);
        var zweite = lauf.ChoiceBranch(2);
        AssertTrue(erste > 0,
            "**and the first answer leads somewhere** -- and a reader that "
                + "kept only the texts showed the player a question whose "
                + "answer went nowhere");
        AssertEq(erste, 2,
            "**and to the first option's line** -- and the file counts its "
                + "options from zero while the answer counts from one, so "
                + "402 [0, \"Yes\"] is where answer 1 leads");
        // **Und gemessen: 5, und nicht 4** -- **und der Unterschied ist
        // der erste Zweig, der zwei Zeilen hat** -- **ein Leser, der
        // gerechnet hat statt gemessen, suchte die zweite Option an
        // einem Index, an dem nichts steht.**
        AssertEq(zweite, 5,
            "**and the second answer leads somewhere else** -- and a "
                + "reader that kept the file's number for both jumped "
                + "into the first branch for every answer");
        AssertEq(lauf.ChoiceBranch(3), -1,
            "**and a third answer leads nowhere at all** -- and a choice "
                + "of two does not take a 3");
    }

    /// <summary>
    /// An option line is never a command of its own.
    /// </summary>
    /// <remarks>
    /// <strong>And stepping over it steps over one command.</strong> An
    /// option is a line the <c>102</c> reads; <strong>a reader that
    /// stepped over two jumped into the branch that belongs to the
    /// answer</strong> — <strong>and a game whose first answer is "Yes"
    /// would have run its "No" branch.</strong>
    /// </remarks>
    public void Test_EineOptionWirdUeberschrittenUndNichtAusgefuehrt()
    {
        var lauf = new MzInterpreter(Block());
        lauf.Setup(1, 14);
        lauf.Index = 2;
        var vorher = lauf.Index;

        MzControlFlow.TryExecute(lauf, lauf.Commands[2],
            new List<MzAction>(), new MzBranchFacts());

        AssertEq(lauf.Index, vorher + 1,
            "**and exactly one command is stepped over** -- and a reader "
                + "that stepped over two jumped into the branch that "
                + "belongs to the answer, and a game whose first answer "
                + "is Yes would have run its No branch");
        AssertEq(lauf.Commands[lauf.Index].Code, 221,
            "**and the next line is the branch** -- and it is at indent "
                + "one, and a reader that ran it as its own command "
                + "started an animation nobody asked for");
    }

    /// <summary>
    /// The cancel branch is the number the game wrote.
    /// </summary>
    /// <remarks>
    /// <strong>And the measured <c>102</c> writes <c>1</c> for a choice
    /// of two.</strong> The engine's rule is
    /// <c>params[1] &lt; choices.length ? params[1] : -2</c> —
    /// <strong>a number below the count is an option that cancels, and
    /// anything else is "no cancel".</strong> <strong>A reader that read
    /// it as a boolean turned every choice into a cancellable one</strong>,
    /// and a player could leave a question open with a cancel key that the
    /// game never offered.
    /// </remarks>
    public void Test_DerAbbruchzweigIstDieGeschriebeneZahl()
    {
        // **Und `1` bei zwei Optionen heisst "die zweite bricht ab".**
        var mitAbbruch = new MzBranchFacts();
        mitAbbruch.StartChoice(
            MzChoice.Read(new MzCommandEntry(102, ["[\"Yes\", \"No\"]", "1"], 0)
                .Parameters),
            true);
        AssertTrue(mitAbbruch.OpenChoice!.CanCancel,
            "**and a number below the count is a cancel** -- and it is the "
                + "number the game wrote, and not a boolean the reader "
                + "made of it");

        // **Und `-2` ist "kein Abbruch".**
        var ohneAbbruch = new MzBranchFacts();
        ohneAbbruch.StartChoice(
            MzChoice.Read(new MzCommandEntry(102, ["[\"Yes\", \"No\"]", "-2"], 0)
                .Parameters),
            false);
        AssertTrue(!ohneAbbruch.OpenChoice!.CanCancel,
            "**and a number at or above the count is none** -- and -2 is "
                + "the engine's own way of writing that, and a reader that "
                + "read any non-zero value as a cancel offered a player a "
                + "key the game never put on the screen");
    }

    /// <summary>
    /// The cancel branch the <c>102</c> itself wrote, through the
    /// dispatch and not by hand.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the test the living mutation rule asked
    /// for.</strong> The test above calls <c>StartChoice</c> itself and
    /// passes the answer in, <strong>so a dispatch that wrote
    /// <c>true</c> instead of reading the number passed it</strong> — and
    /// the code did read the number, and no test reached that line.
    /// </para>
    /// <para>
    /// <strong>And the difference is a number, and not a boolean.</strong>
    /// The engine's rule is
    /// <c>params[1] &lt; choices.length ? params[1] : -2</c>;
    /// <strong>a reader that turned it into a plain boolean offered the
    /// player a cancel key on every choice</strong>, and one that turned
    /// it into "is it non-zero" offered none on any.
    /// </para>
    /// </remarks>
    public void Test_DerAbbruchZweigDurchDenDispatch()
    {
        // **Und die gemessene Form mit `1` bei zwei Optionen.**
        var gemessen = new MzBranchFacts();
        var lauf = new MzInterpreter(Block());
        lauf.Setup(1, 14);
        lauf.Index = 1;
        MzControlFlow.TryExecute(lauf, lauf.Commands[1],
            new List<MzAction>(), gemessen);
        AssertTrue(gemessen.OpenChoice!.CanCancel,
            "**and the number 1 in a choice of two is a cancel** -- and a "
                + "reader that wrote true instead of reading the number "
                + "offered the player a cancel key on every choice");

        // **Und dieselbe Liste mit `-2`, und das ist kein Abbruch.**
        var ohne = new MzBranchFacts();
        var zweiter = new MzInterpreter(new List<MzCommandEntry>
        {
            new(401, ["?"], 0),
            new(102, ["[\"Yes\", \"No\"]", "-2"], 0),
            new(402, ["0", "Yes"], 0),
            new(402, ["1", "No"], 0),
        });
        zweiter.Setup(1, 14);
        zweiter.Index = 1;
        MzControlFlow.TryExecute(zweiter, zweiter.Commands[1],
            new List<MzAction>(), ohne);
        AssertTrue(!ohne.OpenChoice!.CanCancel,
            "**and -2 is none** -- and -2 is the engine's own way of "
                + "writing that, and a reader that read any number as a "
                + "cancel put a key on the screen the game never wrote");
    }
}
