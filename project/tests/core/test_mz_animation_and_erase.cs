using System;
using System.Collections.Generic;
using UniversalRPG.Tests.Framework;
using UniversalRPG.Web;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// <c>221 Show Animation</c> and <c>222 Erase Event</c>, the two
/// character commands a finished MZ project uses most after the
/// balloon.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the source is the official manual.</strong> For
/// <c>222</c>: *Temporarily removes the event currently being run. There
/// are no parameters to set. The event will remain erased until the party
/// moves to another map.* For <c>221</c>: *Character — The display location
/// will be based on the position of the player or event. Animations —
/// Specify the animation to display. Wait for Completion — When enabled, the
/// event will be paused until the animation being displayed has finished.*
/// </para>
/// <para>
/// <strong>And the help gives 221 and 213 the same three sentences</strong>,
/// which is why both carry the same shape, <strong>and they are separate
/// codes in every finished project</strong> — <strong>and one field for
/// both would have let a balloon overwrite an animation.</strong>
/// </para>
/// <para>
/// <strong>And both measured forms are empty: <c>221 []</c> sixteen
/// times, <c>222 []</c> fourteen times.</strong> The animation's
/// parameters are inside its own name in the editor and this reader gets
/// the list the file carries, which for this game is empty.
/// </para>
/// </remarks>
public partial class TestMzAnimationAndErase : TestBase
{
    private static (MzBranchFacts Facts, MzInterpreter Lauf) Start()
    {
        var fakten = new MzBranchFacts();
        fakten.Characters[7] = new MzCharacter();
        return (fakten, new MzInterpreter(new List<MzCommandEntry>()));
    }

    /// <summary>
    /// An animation goes over the player or a figure, and it finishes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the duration is a number this repository chose
    /// again</strong>, <strong>and the constant admits it the same way
    /// the balloon's does</strong> — the manual names the character, the
    /// animation and whether to wait, <strong>and no duration
    /// anywhere</strong>, <strong>and without one *wait for the animation
    /// being displayed has finished* is a wait that never ends.</strong>
    /// </para>
    /// <para>
    /// <strong>And an animation is not a balloon</strong> — a separate
    /// field on each, <strong>and a shared one would have been the
    /// bug the two codes exist to avoid.</strong>
    /// </para>
    /// </remarks>
    public void Test_EineAnimationGehtUeberDenSpielerOderUeberEineFigur()
    {
        var (fakten, lauf) = Start();
        var aktionen = new List<MzAction>();

        MzCommands.TryExecute(lauf,
            new MzCommandEntry(221, ["-1", "3", "false"], 0),
            aktionen, fakten, new MzRandom());

        AssertEq(fakten.Player.Animation, 3,
            "**and the player plays the animation the file named** -- and "
                + "-1 is the player, the same as for 213, because it is the "
                + "same first parameter and the same help text");
        AssertTrue(fakten.Player.HasAnimation,
            "**and it is playing**");
        AssertEq(aktionen.Count, 1,
            "**and the command wrote one action**");
        AssertEq(fakten.Notices.Count, 0,
            "**and nothing is complained about**");

        // **Und die Figur traegt ihre eigene, und nicht die des
        // Spielers.**
        MzCommands.TryExecute(lauf,
            new MzCommandEntry(221, ["7", "5", "false"], 0),
            new List<MzAction>(), fakten, new MzRandom());
        AssertEq(fakten.Characters[7].Animation, 5,
            "**and a figure plays its own animation** -- and one field for "
                + "both would have put icon three over a head that asked "
                + "for five");
        AssertEq(fakten.Player.Animation, 3,
            "**and the player still has its own** -- and a shared field "
                + "made the second command land on the first one");

        // **Und sie ist vorbei, und beide zaehlen gemeinsam.**
        fakten.TickAnimations(59);
        AssertTrue(fakten.Player.HasAnimation
                && fakten.Characters[7].HasAnimation,
            "**and both are still there after fifty-nine frames**");
        fakten.TickAnimations(1);
        AssertTrue(!fakten.Player.HasAnimation
                && !fakten.Characters[7].HasAnimation,
            "**and both are gone on the sixtieth** -- and a tick that "
                + "covered only one of them left the other on the map");

        // **Und eine Figur, die es nicht gibt, wird gemeldet.**
        MzCommands.TryExecute(lauf,
            new MzCommandEntry(221, ["99", "3", "false"], 0),
            new List<MzAction>(), fakten, new MzRandom());
        AssertEq(fakten.Notices.Count, 1,
            "**and a character the map does not have is named**");
    }

    /// <summary>
    /// A figure nobody drew a balloon or an animation on has neither.
    /// </summary>
    /// <remarks>
    /// <strong>And zero is the editor's first icon and its first
    /// animation</strong>, <strong>and a field that starts at zero says
    /// every figure on the map carries one</strong> from the frame the
    /// map loads. Both fields were caught this way, once by a living
    /// mutation for the balloon and by the same reasoning for the
    /// animation.
    /// </remarks>
    public void Test_EineFrischGeladeneFigurHatWederBallonNochAnimation()
    {
        var (fakten, _) = Start();

        AssertTrue(!fakten.Characters[7].HasBalloon,
            "**and no balloon** -- and the editor's list starts at zero, "
                + "and a field that starts at zero puts the first icon "
                + "over every head the moment the map loads");
        AssertTrue(!fakten.Characters[7].HasAnimation,
            "**and no animation** -- and the same list starts at zero, and "
                + "the same bug would have started a sixty-frame clock on "
                + "every figure in the game");
    }

    /// <summary>
    /// Erase hides the running event and leaves it on the map.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And "erased" is not "gone".</strong> *Temporarily removes
    /// the event currently being run.* <strong>The event still has its
    /// commands</strong>, <strong>and a reader that took it out of the map
    /// made every later command that named it fail</strong> — and a
    /// game's own event erases itself and then runs four more commands.
    /// </para>
    /// <para>
    /// <strong>And nothing here clears the flag.</strong> *The event will
    /// remain erased until the party moves to another map*,
    /// <strong>and a reader that ticked it off after N frames brought the
    /// event back while the player was looking straight at the place
    /// where it had been.</strong>
    /// </para>
    /// </remarks>
    public void Test_EreignisWirdVerstecktUndBleibtAufDerKarte()
    {
        var (fakten, lauf) = Start();
        lauf.Setup(1, 7);
        var aktionen = new List<MzAction>();

        AssertTrue(MzCommands.TryExecute(lauf,
                new MzCommandEntry(222, [], 0),
                aktionen, fakten, new MzRandom()),
            "**and the command runs** -- and it carries no parameters, and "
                + "the manual says so in as many words");

        AssertTrue(fakten.Characters[7].Erased,
            "**and the running event is hidden** -- and it was event 7, "
                + "and the interpreter said so with Setup(1, 7)");
        AssertTrue(fakten.Characters.ContainsKey(7),
            "**and it is still on the map** -- and a reader that removed it "
                + "from the map made every later command that named it say "
                + "\"no such character\", and a game's own event erases "
                + "itself and then runs four more commands");

        // **Und ein Schritt aendert daran nichts.**
        fakten.TickBalloons(600);
        fakten.TickAnimations(600);
        AssertTrue(fakten.Characters[7].Erased,
            "**and six hundred frames do not bring it back** -- and the "
                + "manual says *until the party moves to another map*, and "
                + "a reader that cleared the flag on a timer undid it "
                + "while the player was looking at the place");

        // **Und ein anderes Ereignis ist nicht betroffen.**
        AssertTrue(!fakten.Player.Erased,
            "**and the player is not erased by it**");
    }

    /// <summary>
    /// Waiting is a setting, and it is not the same as running on.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the third time this shape bit us, and the
    /// test that catches it is always the one that goes through the
    /// runner.</strong> <c>TryExecute</c> returning <c>false</c> means
    /// *the list still waits*; <strong>the index then stays, and the
    /// same command is read again next frame</strong> — and an
    /// animation resets its clock on every pass, so it never finishes
    /// and the run freezes.
    /// </para>
    /// <para>
    /// <strong>And the difference is a single boolean, so both branches
    /// are checked here.</strong> <c>false</c> in the third parameter
    /// must let the list go on; <c>true</c> must hold it, and the
    /// animation has to be there for the wait to be about anything.
    /// </para>
    /// </remarks>
    public void Test_WartenIstEineEinstellungUndNichtDasEnde()
    {
        var (fakten, lauf) = Start();
        var aktionen = new List<MzAction>();

        // **Und ohne Warten geht die Liste weiter.**
        MzCommands.TryExecute(lauf,
            new MzCommandEntry(221, ["-1", "3", "false"], 0),
            aktionen, fakten, new MzRandom());
        AssertTrue(aktionen[0].What.Contains("the player"),
            "**and the action names the player**");

        // **Und mit Warten haelt der Befehl die Liste.**
        var (fakten2, lauf2) = Start();
        lauf2.Setup(1, 7);
        var aktionen2 = new List<MzAction>();
        var rueck = MzCommands.TryExecute(lauf2,
            new MzCommandEntry(221, ["-1", "3", "true"], 0),
            aktionen2, fakten2, new MzRandom());

        AssertTrue(!rueck,
            "**and with wait on, TryExecute says the list is still "
                + "waiting** -- and a reader that returned true anyway let "
                + "the next command run while the animation was still on "
                + "the screen, and the whole wait setting does nothing");
        AssertTrue(fakten2.Player.HasAnimation,
            "**and the animation is there** -- and a wait for an "
                + "animation that was never shown is a wait for nothing");
        AssertTrue(aktionen2[0].What.Contains("waiting for it to finish"),
            "**and the action says it waits** -- and an action that does "
                + "not say it leaves a log entry that reads as if the "
                + "command had simply run");
    }

    /// <summary>
    /// The animation ends while a list runs, not only when a test ticks
    /// it by hand.
    /// </summary>
    /// <remarks>
    /// <strong>And this is the test the living mutation rule asked
    /// for.</strong> The test above counts frames itself,
    /// <strong>and a runner that never ticked anything left it
    /// green</strong> — and the code did have the call in it, and
    /// nothing reached it.
    /// </remarks>
    public void Test_DerRunnerLaesstDieAnimationVerschwinden()
    {
        var fakten = new MzBranchFacts();
        var befehle = new List<MzCommandEntry>
        {
            new(221, ["-1", "3", "false"], 0),
        };
        for (var i = 0; i < 61; i++)
        {
            // **Und 121 Control Switches wartet nicht und verweigert
            // sich nicht**, **und es ist ein Befehl wie jeder
            // andere.**
            befehle.Add(new MzCommandEntry(121, ["1", "1", "0"], 0));
        }

        var lauf = new MzEventRunner();
        var ergebnis = lauf.Run(befehle, fakten, 1, 1, new MzRandom());

        AssertEq(ergebnis.Stopped, MzStep.Finished,
            "**and the list ran to its end**");
        AssertTrue(!fakten.Player.HasAnimation,
            "**and the animation is gone** -- and 221 without a clock is "
                + "221 with no end, and the manual calls the field *wait "
                + "for the animation being displayed has finished*");
    }

    /// <summary>
    /// The action of 222 says how long the erasure lasts.
    /// </summary>
    /// <remarks>
    /// <strong>And the words are the whole of the value.</strong> The
    /// manual says *until the party moves to another map*; a log entry
    /// that stops at *erased* says the same thing as a log entry that
    /// says the picture is gone, **and both would have left a reader of
    /// that log thinking the event is not coming back at all.**
    /// </remarks>
    public void Test_DieMeldungVon222SagtBisWann()
    {
        var (fakten, lauf) = Start();
        lauf.Setup(1, 7);
        var aktionen = new List<MzAction>();

        MzCommands.TryExecute(lauf, new MzCommandEntry(222, [], 0),
            aktionen, fakten, new MzRandom());

        AssertTrue(aktionen[0].What.Contains("another map"),
            "**and the action says it lasts until the map changes** -- and "
                + "the manual says exactly that, and an action without it "
                + "reads as though the event never comes back");
    }

    /// <summary>
    /// A setting the editor writes as a word is read as a word.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the finding, and it was invisible for the
    /// whole file until a real project was measured.</strong> Measured on
    /// <c>CamelliaCoronation</c>: <c>213</c> carries
    /// <c>["-1", "2", "False"]</c> and <c>["-1", "8", "True"]</c> --
    /// <strong>words</strong> -- while <c>121</c>, <c>123</c> and
    /// <c>129</c> carry <c>"0"</c> and <c>"1"</c>, <strong>also
    /// words, also not numbers, but words that parse.</strong>
    /// </para>
    /// <para>
    /// <strong>And <c>At</c> returns 0 for a word, and
    /// <c>0 == 1</c> is false, and so the waiting setting of a balloon
    /// and of an animation was dead in every real game.</strong> The
    /// tests were green, <strong>because the tests wrote the numbers
    /// themselves</strong> -- and a test that writes its own input is
    /// not a measurement of the file.
    /// </para>
    /// <para>
    /// <strong>And <c>221</c> carries an empty list in that game, so its
    /// third parameter is not there and that reads as "do not wait".</strong>
    /// </para>
    /// </remarks>
    public void Test_EinAlsWortGeschriebenerSchalterWirdAlsWortGelesen()
    {
        var (fakten, lauf) = Start();
        var aktionen = new List<MzAction>();

        // **Und "True" wartet.**
        var rueck = MzCommands.TryExecute(lauf,
            new MzCommandEntry(213, ["-1", "2", "True"], 0),
            aktionen, fakten, new MzRandom());
        AssertTrue(!rueck,
            "**and \"True\" holds the list** -- and a reader that only "
                + "parsed numbers read this as zero, and zero is not one, "
                + "and the wait setting of thirty-six commands in the game "
                + "in front of us did nothing");
        AssertTrue(aktionen[0].What.Contains("waiting for it to go"),
            "**and the action says it waits**");

        // **Und "False" laeuft weiter.**
        var (fakten2, lauf2) = Start();
        var aktionen2 = new List<MzAction>();
        var zurueck = MzCommands.TryExecute(lauf2,
            new MzCommandEntry(213, ["-1", "2", "False"], 0),
            aktionen2, fakten2, new MzRandom());
        AssertTrue(zurueck,
            "**and \"False\" lets the list go on** -- and a reader that "
                + "treated any unparseable value as true would have held "
                + "every balloon in the game for a second");
        AssertTrue(!fakten2.Player.HasBalloon == false,
            "**and the balloon is there either way** -- and the setting is "
                + "about waiting, not about whether the icon shows");

        // **Und dieselbe Form bei 221.**
        var (fakten3, lauf3) = Start();
        var aktionen3 = new List<MzAction>();
        var rueck3 = MzCommands.TryExecute(lauf3,
            new MzCommandEntry(221, ["-1", "3", "True"], 0),
            aktionen3, fakten3, new MzRandom());
        AssertTrue(!rueck3,
            "**and an animation written the same way waits too** -- and "
                + "the help gives both commands the same third setting, "
                + "so a reader that read one and not the other was half "
                + "right and no test could see it");

        // **Und eine Zahl geht auch, weil 121 und 122 Zahlen
        // schreiben.**
        var (fakten4, lauf4) = Start();
        var rueck4 = MzCommands.TryExecute(lauf4,
            new MzCommandEntry(213, ["-1", "2", "1"], 0),
            new List<MzAction>(), fakten4, new MzRandom());
        AssertTrue(!rueck4,
            "**and a plain one waits as well** -- and a finished project "
                + "writes both forms, and a reader that took only the word "
                + "would have broken every hand-edited event");

        // **Und ein leerer Parameter ist "nein".**
        var (fakten5, lauf5) = Start();
        var zurueck5 = MzCommands.TryExecute(lauf5,
            new MzCommandEntry(221, [], 0),
            new List<MzAction>(), fakten5, new MzRandom());
        AssertTrue(zurueck5,
            "**and 221's empty list does not wait** -- and that is exactly "
                + "what the game in front of us carries, sixteen times, "
                + "and a reader that waited on a missing parameter would "
                + "have frozen the first animation it ever met");
    }
}
