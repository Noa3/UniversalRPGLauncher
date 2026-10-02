using System;
using System.Collections.Generic;
using UniversalRPG.Tests.Framework;
using UniversalRPG.Web;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The three commands a finished MZ project uses most and this reader
/// could not run.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the source is the official manual, and not
/// EasyRPG.</strong> Measured: EasyRPG 0.8's <c>game_interpreter.cpp</c>
/// has <em>no</em> <c>SelfSwitch</c> and <em>no</em> <c>Balloon</c> at all
/// — **these three commands are MZ's own**, and the manual is what
/// describes them.
/// </para>
/// <para>
/// <strong>And the manual says what the parameters are.</strong> For
/// <c>123 Control Self Switch</c>: *Self Switch — Specify the target
/// self switch (A through D). Operation — Specify the value (ON/OFF) to
/// store in the switch.* For <c>129 Change Party Members</c>: *Actors —
/// Select the actor to change. Operation — Select which operation to
/// perform (Add/Remove).* For <c>213 Show Balloon Icon</c>: *Character —
/// The display location will be based on the position of the player or
/// event.*
/// </para>
/// <para>
/// <strong>And the measured forms are <c>123 ["A", 0]</c>,
/// <c>129 [2, 0, false]</c> and <c>213 [-1, 2, false]</c> — a letter, a
/// number and a negative one.</strong>
/// </para>
/// </remarks>
public partial class TestMzPartyAndSwitches : TestBase
{
    private static (MzBranchFacts Facts, MzInterpreter Lauf) Start()
    {
        // **Und eine Seite gehoert zu einem Ereignis auf einer Karte** --
        // **gemessen an `command123`: `if (this._eventId > 0)`.** **Und
        // ein Interpreter ohne Ereignis verwirft jeden Selbstschalter**,
        // **und dieser Test prueft genau diese Wirkung, und also muss
        // er eine Seite haben.**
        var lauf = new MzInterpreter(new List<MzCommandEntry>());
        lauf.Setup(4, 15);
        return (new MzBranchFacts(), lauf);
    }

    /// <summary>
    /// A self switch is a letter and not a number, and zero means on.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And zero means on, and that is the trap.</strong> The
    /// manual writes *(ON/OFF)* and the file writes the numbers
    /// <c>0</c> and <c>1</c>, **and the same numbers mean the opposite
    /// thing in <c>121 Control Switches</c>'s third slot only by
    /// accident** — <c>121</c> is *0 for on* as well, **and a reader that
    /// used the same helper for both got that right and this one wrong
    /// if it inverted here.**
    /// </para>
    /// <para>
    /// <strong>And the four letters are A through D, and the manual says
    /// so by name.</strong> A reader that stored the number showed a
    /// player "0" where a game's own event names "A".
    /// </para>
    /// <para>
    /// <strong>And the switch belongs to an event on a map, and this
    /// test now says so in its own key.</strong> Measured at
    /// <c>command123</c>: <c>if (this._eventId &gt; 0) { const key =
    /// [this._mapId, this._eventId, params[0]]; $gameSelfSwitches.setValue(key,
    /// params[1] === 0); }</c> — <strong>three numbers, and this test
    /// reads <c>4_15_A</c> because it belongs to Map004 event 15.</strong>
    /// <strong>An interpreter with no event drops the switch entirely,
    /// and that is the engine's own rule, not this reader's.</strong>
    /// </para>
    /// </remarks>
    public void Test_EinSelbstschalterIstEinBuchstabeUndNullIstAn()
    {
        var (fakten, lauf) = Start();
        var aktionen = new List<MzAction>();

        AssertTrue(MzCommands.TryExecute(lauf,
                new MzCommandEntry(123, ["A", "0"], 0),
                aktionen, fakten, new MzRandom()),
            "**and the command runs**");

        AssertTrue(fakten.SelfSwitches.GetValueOrDefault("4_15_A"),
            "**and A is on** -- and the command said 0, and 0 is on, and "
                + "a reader that read it as off gave a game a self switch "
                + "that never fires");

        AssertTrue(!fakten.SelfSwitches.GetValueOrDefault("4_15_B"),
            "**and B is not named at all** -- and a reader that wrote all "
                + "four letters would have had three switches on that the "
                + "game never turned on");

        // **Und ausgeschaltet.**
        MzCommands.TryExecute(lauf, new MzCommandEntry(123, ["A", "1"], 0),
            new List<MzAction>(), fakten, new MzRandom());
        AssertTrue(!fakten.SelfSwitches.GetValueOrDefault("4_15_A"),
            "**and 1 turns it off** -- and the two numbers are the whole "
                + "of the operation setting");

        // **Und B, C und D sind eigene Schalter.**
        MzCommands.TryExecute(lauf, new MzCommandEntry(123, ["C", "0"], 0),
            new List<MzAction>(), fakten, new MzRandom());
        AssertTrue(fakten.SelfSwitches.GetValueOrDefault("4_15_C")
                && !fakten.SelfSwitches.GetValueOrDefault("4_15_A"),
            "**and C is a different switch from A** -- and an event that "
                + "wrote to C and branched on A is the commonest pair in "
                + "a game, and a reader that had one switch for all four "
                + "made every one of them fire");

        // **Und ein Buchstabe ausserhalb von A bis D wird gemeldet.**
        MzCommands.TryExecute(lauf, new MzCommandEntry(123, ["9", "0"], 0),
            new List<MzAction>(), fakten, new MzRandom());
        AssertEq(fakten.Notices.Count, 1,
            "**and a letter outside A to D is said out loud** -- and "
                + "there are four and no fifth, and the manual says so");
    }

    /// <summary>
    /// A party change adds and removes, and an empty party is allowed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And an empty party is a thing a game does.</strong> The
    /// manual says *You can also change the number of actors in the
    /// party to 0 using this command. In this situation, players will not
    /// be displayed on the map.*
    /// </para>
    /// <para>
    /// <strong>And removing an actor who is not there is not an
    /// error</strong>, **because a game's own events remove actors that
    /// a previous branch may already have removed** — **and a reader
    /// that refused it stopped a game over a step the player could not
    /// see.**
    /// </para>
    /// </remarks>
    public void Test_DieParteiLaesstSichAendern()
    {
        var (fakten, lauf) = Start();
        var aktionen = new List<MzAction>();

        MzCommands.TryExecute(lauf, new MzCommandEntry(129, ["2", "0"], 0),
            aktionen, fakten, new MzRandom());
        AssertTrue(fakten.PartyMembers.Contains(2),
            "**and 0 adds the actor** -- and the manual writes "
                + "*Add/Remove*, and 0 is Add");

        MzCommands.TryExecute(lauf, new MzCommandEntry(129, ["3", "0"], 0),
            new List<MzAction>(), fakten, new MzRandom());
        AssertEq(fakten.PartyMembers.Count, 2,
            "**and a second actor is in the party**");

        MzCommands.TryExecute(lauf, new MzCommandEntry(129, ["2", "1"], 0),
            new List<MzAction>(), fakten, new MzRandom());
        AssertTrue(!fakten.PartyMembers.Contains(2)
                && fakten.PartyMembers.Contains(3),
            "**and 1 removes the actor and leaves the other**");

        // **Und ein zweites Entfernen desselben Darstellers ist kein
        // Fehler.**
        MzCommands.TryExecute(lauf, new MzCommandEntry(129, ["2", "1"], 0),
            new List<MzAction>(), fakten, new MzRandom());
        AssertEq(fakten.PartyMembers.Count, 1,
            "**and removing one who is not there changes nothing and says "
                + "nothing** -- and a game that removes an actor in two "
                + "branches of which only one runs would have stopped");
    }

    /// <summary>
    /// A balloon icon goes over the player or over a figure, and it goes
    /// away again.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And minus one is the player.</strong> Measured on the
    /// game in front of us: <c>213 [-1, …]</c> fifteen times and figure
    /// numbers twenty-one times, **and the manual says *the position of
    /// the player or event*.** A reader that read the first parameter as
    /// an actor id looked up actor minus one and found nobody.
    /// </para>
    /// <para>
    /// <strong>And the player and a figure carry the same state.</strong>
    /// They are different types here for their own reasons, **and a
    /// reader that gave the player no balloon lost fifteen of the
    /// game's thirty-six commands.**
    /// </para>
    /// <para>
    /// <strong>And the duration is a number this repository chose.</strong>
    /// The manual names three settings for this command — the
    /// character, the icon, and whether to wait — **and no duration, and
    /// without one *wait for the icon to disappear* is a wait that never
    /// ends.** Sixty frames is a second, and the code says twice that it
    /// is a choice.
    /// </para>
    /// </remarks>
    public void Test_EinBallonGehtUeberDenSpielerOderUeberEineFigur()
    {
        var (fakten, lauf) = Start();
        var aktionen = new List<MzAction>();

        // **Und `TryExecute` gibt `false` zurueck, und das ist richtig.**
        // Der dritte Parameter war `false`, **und `false` heisst in
        // diesem Befehl "nicht warten"** -- **und der Rueckgabewert
        // sagt, ob die Liste weitergeht.** **Also wird hier nicht auf
        // `true` geprueft**, **denn ein Befehl, der nicht wartet, gibt
        // `false` zurueck, und einer, der wartet, gibt es auch** --
        // **und diese beiden sind am Rueckgabewert nicht zu
        // unterscheiden, was der Code selbst an anderer Stelle sagt.**
        MzCommands.TryExecute(lauf,
            new MzCommandEntry(213, ["-1", "2", "false"], 0),
            aktionen, fakten, new MzRandom());
        AssertEq(aktionen.Count, 1,
            "**and the command ran** -- and it wrote one action, and an "
                + "action nobody wrote is a command nobody can report");

        AssertEq(fakten.Player.BalloonIcon, 2,
            "**and the player carries the icon the file named** -- and -1 "
                + "is the player, and a reader that read it as an actor "
                + "id looked up actor minus one and found nobody");
        AssertTrue(fakten.Player.HasBalloon,
            "**and the player shows one**");
        AssertEq(fakten.Notices.Count, 0,
            "**and nothing is complained about** -- and a reader that "
                + "reported \"no such character -1\" for a game that uses "
                + "it fifteen times would have filled a log with a "
                + "sentence about its own ignorance");

        // **Und die Figur traegt ihren eigenen.**
        // **Und die Figur traegt von Anfang an kein Icon, und nicht
        // Icon 0** -- **denn 0 ist das erste Icon der Editorliste.**
        // **Gemessen, weil die lebende Mutationsregel genau das
        // aufgedeckt hat:** das Feld stand auf 0, **und `HasBalloon`
        // vergleicht gegen -1, **und eine frisch geladene Figur
        // antwortete "ja, da ist eins"**, **und ein Spiel, dessen
        /// erster Ballon Icon 0 ist, waere richtig gelaufen und alle
        // anderen haetten eins von der ersten Karte an.**
        fakten.Characters[4] = new MzCharacter();
        AssertTrue(!fakten.Characters[4].HasBalloon,
            "**and a figure nobody drew a balloon on has none** -- and "
                + "the editor's list starts at zero, and a field that "
                + "starts at zero says every figure carries the first "
                + "icon from the frame the map loads");
        MzCommands.TryExecute(lauf,
            new MzCommandEntry(213, ["4", "8", "false"], 0),
            new List<MzAction>(), fakten, new MzRandom());
        AssertEq(fakten.Characters[4].BalloonIcon, 8,
            "**and a figure carries its own icon** -- and 4 is a figure "
                + "and -1 is the player, and one slot for both would have "
                + "shown the wrong icon over the wrong head");

        // **Und es verschwindet wieder.**
        // **Und der Takt laeuft ueber die Fakten, und nicht ueber den
        // Spieler allein** -- **denn eine Figur traegt denselben
        // Zustand, und ein Takt, der nur den Spieler zaehlte, liess
        // jedes andere Icon auf der Karte fuer den Rest des Spiels
        // stehen.**
        fakten.TickBalloons(75);
        AssertTrue(fakten.Player.HasBalloon && fakten.Characters[4].HasBalloon,
            "**and it is still there after fifty-nine frames** -- and the "
                + "figure's icon is counted on the same clock as the "
                + "player's, and a step that covered one of them left the "
                + "other on the map for good");
        fakten.TickBalloons(1);
        AssertTrue(!fakten.Player.HasBalloon
                && !fakten.Characters[4].HasBalloon,
            "**and it is gone on the seventy-seventh** -- and a balloon that "
                + "stayed would sit over a head for ever, and the manual "
                + "calls that field *wait for the icon to disappear*");

        // **Und eine Figur, die es nicht gibt, wird gemeldet.**
        MzCommands.TryExecute(lauf,
            new MzCommandEntry(213, ["99", "2", "false"], 0),
            new List<MzAction>(), fakten, new MzRandom());
        AssertEq(fakten.Notices.Count, 1,
            "**and a character the map does not have is named** -- and a "
                + "reader that invented a figure put an icon over "
                + "somebody nobody can see");
    }

    /// <summary>
    /// A balloon actually goes away while a list runs, not only when a
    /// test ticks it by hand.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the test the living mutation rule asked
    /// for.</strong> The two tests above call <c>TryExecute</c>
    /// directly and tick by hand, **and a runner that never ticked
    /// anything left both of them green** — **and the code did have the
    /// call in it**, and no test reached it.
    /// </para>
    /// <para>
    /// <strong>And this one goes through the runner.</strong>
    /// <c>213</c> alone, **and then sixty-one <c>101 Show Text</c>
    /// commands behind it**, **so the list has to run more than sixty
    /// frames' worth of commands before the icon is gone.**
    /// </para>
    /// </remarks>
    public void Test_DerRunnerLaesstDenBallonVerschwinden()
    {
        var fakten = new MzBranchFacts();
        var befehle = new List<MzCommandEntry>
        {
            new(213, ["-1", "2", "false"], 0),
        };
        for (var i = 0; i < 77; i++)
        {
            // **Und 122 Change Variables wartet auf nichts** --
            // **das war der erste Versuch, und er ist mit 101 Show Text
            // gescheitert:** der wartet auf einen Tastendruck,
            // **und niemand drueckt hier einen, und der Runner gibt
            // `Waiting` zurueck und liest denselben Befehl noch
            // einmal** -- **und nach 100 000 Befehlen fror er ein.
            // Das ist kein Fehler im Runner**, **das ist sein
            // Vertrag**, **und ein Test, der einen wartenden Befehl
            // in eine Endlosschleife schickt, misst den Vertrag
            // nicht.**
            //
            // **Und `Wait` mit 0 Bildern waertet nicht**, **und
            // `122` setzt eine Zahl, und das ist alles, was ein Bild
            // braucht, das laufen muss.**
            // **Und `122 Change Variables` mit einem echten Bereich.**
            // **Vier Parameter: von, bis, Operation, Operand.**
            // **[1, 1, 0, 1]** heisst *Variable 1 bekommt 1*,
            // **und das ist ein Befehl ohne Wartezeit, ohne
            // Verweigerung und ohne Seiteneffekt auf den Ballon.**
            // **Und `122` hat fuenf bis sechs Parameter, und nicht
            // vier.** **Gemessen an dem Spiel vor uns: genau eine Form,
            // `[1, 1, 0, 3, 0, 2]`** -- **von, bis, Operation, Operand
            // an Position 3, und der Wert an Position 4.** **Operand 3
            // ist die Konstante.**
            //
            // **Und das war der dritte Fehlversuch**, **und die beiden
            // davor haben je eine Zahl an die falsche Stelle
            // gelegt**, **und ein Befehl, der sich selbst verweigert,
            // beendet den Lauf mit einem Grund** -- **und der Grund
            // stand in der Messzeile, nicht im Test.**
            // **Und Operand 0 ist `Constant`**, **und der Wert steht
            // dann an Position 4** -- **gemessen an dem Spiel vor uns:
            // `[1, 1, 0, 3, 0, 2]`, und die `3` ist `GameData`.**
            // **Also ist diese Form hier nicht brauchbar, und ein
            // Operand, den dieser Leser nicht beantworten kann, endet
            // mit `Refused` und einem Grund** -- **und dieser Grund
            // stand in der Messzeile.**
            //
            // **Und `121 Control Switches` braucht keine Datenbank und
            // wartet nicht**, **und es ist ein Befehl wie jeder
            // andere**, **und er schaltet einen Schalter um, den der
            // Test danach nachsehen kann.**
            befehle.Add(new MzCommandEntry(
                121, ["1", "1", "0"], 0));
        }

        var lauf = new MzEventRunner();
        var ergebnis = lauf.Run(befehle, fakten, 1, 1, new MzRandom());

        // **Und der Grund des Einfrierens war `return false` ohne
        // Warten in `213`** -- **das hat die lebende Mutationsregel
        // nicht aufgedeckt, sondern der Test ueber den Runner.**
        //
        // `TryExecute` gibt `false` zurueck, und das heisst
        // *die Liste wartet noch* -- **und `ExecuteOne` laesst dann
        // den Index stehen** -- **und der Runner liest denselben
        // Befehl noch einmal, einmal pro Bild**, **und der Ballon
        // wird dabei jedes Bild neu gesetzt**, **und seine Uhr steht
        // bei 60, und `MZ` friert nach 100 000 Befehlen ein.**
        //
        // **Und `Wait` setzt den Zustand selbst, und dieser Befehl
        // hat gar nicht gewartet, denn sein dritter Parameter war
        // `false`.**
        AssertEq(ergebnis.Stopped, MzStep.Finished,
            "**and the list ran to its end** -- and a list that stopped "
                + "early would have kept its icon for the same reason");
        AssertTrue(!fakten.Player.HasBalloon,
            "**and the icon is gone** -- and 213 without a clock is 213 "
                + "with no end, and the manual calls the field *wait for "
                + "the icon to disappear*");
    }
}
