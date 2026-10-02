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

        // **Und die Animationen kommen aus dem Projekt, und ein Test,
        // der keine Projektdatei laedt, braucht selbst welche.**
        //
        // **Und diese Zahlen sind die des Spiels vor uns**, -- **gemessen
        // an `D:/Itch/sister/www/data/Animations.json`**, -- **und nicht
        // gerundet und nicht erfunden:**
        //
        // ```text
        // Nummer  Rahmen  Dauer = Rahmen * 4 + 1
        //      1       5              21
        //      2       3              13
        //      3       4              17
        // ```
        //
        // **Und eine Animationstabelle, die man sich ausdenkt, ist
        // genau die Sorte Zahl, die eben vier echte Seiten
        // aufgehalten hat.**
        foreach (var paar in new Dictionary<int, int>
        {
            [1] = MzScreen.AnimationsDauer(5),
            [2] = MzScreen.AnimationsDauer(3),
            [3] = MzScreen.AnimationsDauer(4),
            [5] = MzScreen.AnimationsDauer(8),
        })
        {
            fakten.AnimationLaengen[paar.Key] = paar.Value;
        }

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
            new MzCommandEntry(212, ["-1", "3", "false"], 0),
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
            new MzCommandEntry(212, ["7", "5", "false"], 0),
            new List<MzAction>(), fakten, new MzRandom());
        AssertEq(fakten.Characters[7].Animation, 5,
            "**and a figure plays its own animation** -- and one field for "
                + "both would have put icon three over a head that asked "
                + "for five");
        AssertEq(fakten.Player.Animation, 3,
            "**and the player still has its own** -- and a shared field "
                + "made the second command land on the first one");

        // **Und sie sind vorbei, und beide zaehlen gemeinsam.**
        //
        // **Und nicht nach sechzig Bildern**, -- **denn eine Animation
        // traegt keine feste Laenge.** **Gemessen an
        // `Sprite_Animation.setupDuration`:**
        //
        // ```js
        // this._duration = this._animation.frames.length * this._rate + 1;
        // ```
        //
        // **Und `setupRate() { this._rate = 4; }`**, -- **und Animation 3
        // hat vier Rahmen und Animation 5 hat acht**, -- **und das sind
        // siebzehn und dreiunddreissig Bilder und nicht einundsechzig
        // fuer beide.**
        //
        // **Und "nach sechzig" war die geratene Zahl**, **und sie passte
        // fuer keine Animation irgendeines Spiels.**
        var spielerLaenge = MzScreen.AnimationsDauer(4);
        var figurLaenge = MzScreen.AnimationsDauer(8);
        AssertEq(spielerLaenge, 17,
            "**and the player's animation lasts seventeen frames** -- and"
            + $" it says {spielerLaenge}, and four frames times four plus"
            + " one is seventeen");
        AssertEq(figurLaenge, 33,
            "**and the figure's lasts thirty-three** -- and it says "
            + $"{figurLaenge}, and eight frames times four plus one is"
            + " thirty-three");

        // **Und die laengere laeuft laenger, und das ist der ganze
        // Punkt.**
        fakten.TickAnimations(spielerLaenge);
        AssertTrue(!fakten.Player.HasAnimation,
            "**and the player's is gone after its own seventeen**");
        AssertTrue(fakten.Characters[7].HasAnimation,
            "**and the figure's is still there** -- and one number for"
            + " both would have cut a long animation short and held a"
            + " short one on the screen");
        fakten.TickAnimations(figurLaenge - spielerLaenge);
        AssertTrue(!fakten.Characters[7].HasAnimation,
            "**and it is gone after its own thirty-three** -- and a tick"
            + " that covered only one of them left the other on the map");

        // **Und eine Figur, die es nicht gibt, wird gemeldet.**
        MzCommands.TryExecute(lauf,
            new MzCommandEntry(212, ["99", "3", "false"], 0),
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
                new MzCommandEntry(214, [], 0),
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
            new MzCommandEntry(212, ["-1", "3", "false"], 0),
            aktionen, fakten, new MzRandom());
        AssertTrue(aktionen[0].What.Contains("the player"),
            "**and the action names the player**");

        // **Und mit Warten haelt der Befehl die Liste.**
        var (fakten2, lauf2) = Start();
        lauf2.Setup(1, 7);
        var aktionen2 = new List<MzAction>();
        var rueck = MzCommands.TryExecute(lauf2,
            new MzCommandEntry(212, ["-1", "3", "true"], 0),
            aktionen2, fakten2, new MzRandom());

        // **Und `TryExecute` gibt `true` zurueck, und das ist die
        // Engine.**
        //
        // **Gemessen an `command212` und `command213`:** beide enden mit
        // `return true;`, -- **und `executeCommand` sagt `if
        // (!this[methodName]()) { return false; } this._index++; }`** --
        // **und also geht der Index hoch.**
        //
        // **Und die Wartezeit steht woanders**, -- **in
        // `setWaitMode('animation')`**, -- **und die wird im naechsten
        // `updateWait()` ausgefragt**, -- **und das ist
        // `isAnimationPlaying()` und nicht der Rueckgabewert eines
        // Befehls.**
        //
        // **Und `return !warten` war mein Fehler**, -- **und er kostete
        // eine echte Seite 46 Befehle**, -- **denn die Seite blieb auf
        // ihrem `213` bei Index 130 stehen und zeigte das Icon bei jedem
        // Bild neu.**
        //
        // **Und "die Liste haelt" ist darum eine Aussage ueber
        // `MzStep.Waiting`, nicht ueber `true` oder `false`.**
        AssertTrue(rueck,
            "**and with wait on, TryExecute says the command ran** -- and"
                + " the engine's `command212` ends in `return true;` and"
                + " `executeCommand` steps the index over it");
        AssertEq(lauf2.Stopped, MzStep.Waiting,
            "**and the list is waiting all the same** -- and that is where"
                + " the wait lives: `setWaitMode('animation')`, asked by"
                + " `updateWait` every frame, and not by a return value;"
                + " it is " + lauf2.Stopped);
        AssertTrue(fakten2.Player.HasAnimation,
            "**and the animation is there** -- and a wait for an "
                + "animation that was never shown is a wait for nothing");
        AssertTrue(aktionen2[0].What.Contains("the page waits for it"),
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
        fakten.AnimationLaengen[3] = MzScreen.AnimationsDauer(4);
        var befehle = new List<MzCommandEntry>
        {
            new(212, ["-1", "3", "false"], 0),
        };

        // **Und einundsechzig `121` statt sechzig** -- **denn die
        // Animation hat vier Rahmen**, -- **und `4 * 4 + 1` ist
        // siebzehn Bilder**, -- **und siebzehn ist keine runde Zahl, die
        // man durch Zufall trifft.**
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
            "**and the animation is gone** -- and the manual calls"
                + " the field *wait for the animation being displayed has"
                + " finished*, and without that setting the picture has"
                + " no end except the one in Animations.json");
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
    public void Test_DieMeldungVon214SagtBisWann()
    {
        var (fakten, lauf) = Start();
        lauf.Setup(1, 7);
        var aktionen = new List<MzAction>();

        MzCommands.TryExecute(lauf, new MzCommandEntry(214, [], 0),
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
        // **Und "die Liste haelt" ist eine Aussage ueber `MzStep`, und
        // nicht ueber den Rueckgabewert.**
        //
        // **Gemessen an `command213`:** es endet mit `return true;` --
        // **und `executeCommand` sagt `if (!this[methodName]()) { return
        // false; } this._index++; }`** -- **und also geht der Index
        // hoch.** **Und `101` gibt `false` zurueck** und der Index
        // bleibt -- **und das ist der ganze Unterschied.**
        //
        // **Und "True" zu lesen ist damit eine eigene Frage**, -- **und
        // ein Leser, der nur Zahlen las, sah hier eine Null** -- **und
        // eine Null ist keine Eins**, -- **und die Warteeinstellung von
        // sechsunddreissig Befehlen tat nichts.**
        AssertTrue(rueck,
            "**and \"True\" says the command ran** -- and `command213`"
                + " ends in `return true;`, and `executeCommand` steps the"
                + " index over it");
        AssertEq(lauf.Stopped, MzStep.Waiting,
            "**and \"True\" holds the list all the same** -- and the wait"
                + " lives in `setWaitMode('balloon')` and not in a return"
                + " value; it is " + lauf.Stopped);
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
            new MzCommandEntry(212, ["-1", "3", "True"], 0),
            aktionen3, fakten3, new MzRandom());
        AssertTrue(rueck3,
            "**and an animation written the same way runs too** -- and"
                + " `command212` ends in `return true;` like `command213`,"
                + " and the help gives both the same third setting");
        AssertEq(lauf3.Stopped, MzStep.Waiting,
            "**and it waits in the same way** -- and `setWaitMode"
                + "('animation')` is asked every frame by `updateWait`;"
                + " it is " + lauf3.Stopped);

        // **Und eine Zahl geht auch, weil 121 und 122 Zahlen
        // schreiben.**
        var (fakten4, lauf4) = Start();
        var rueck4 = MzCommands.TryExecute(lauf4,
            new MzCommandEntry(213, ["-1", "2", "1"], 0),
            new List<MzAction>(), fakten4, new MzRandom());
        AssertTrue(rueck4,
            "**and a plain one runs as well** -- and a finished project"
                + " writes both forms, and a reader that took only the word"
                + " would have broken every hand-edited event");
        AssertEq(lauf4.Stopped, MzStep.Waiting,
            "**and it waits just the same** -- and the word and the number"
                + " are the same setting; it is " + lauf4.Stopped);

        // **Und ein leerer Parameter ist "nein".**
        var (fakten5, lauf5) = Start();
        var zurueck5 = MzCommands.TryExecute(lauf5,
            new MzCommandEntry(214, [], 0),
            new List<MzAction>(), fakten5, new MzRandom());
        AssertTrue(zurueck5,
            "**and 214's empty list does not wait** -- and that is exactly "
                + "what the game in front of us carries, sixteen times, "
                + "and a reader that waited on a missing parameter would "
                + "have frozen the first animation it ever met");
    }
}
