using System.Collections.Generic;

namespace UniversalRPG.Web;

/// <summary>
/// The commands that move the interpreter's index, and nothing else.
/// </summary>
/// <remarks>
/// These are the commands whose whole effect is where the interpreter goes. Each
/// one was read out of <c>Game_Interpreter</c> in the engine, and each returns
/// whether the interpreter may carry on; one of them returns false, and that
/// return value is what holds the interpreter in place.
/// </remarks>
public static class MzControlFlow
{
    /// <summary>What running a control flow command did.</summary>
    public enum MzControlOutcome
    {
        /// <summary>It is not one of these, and belongs to
        /// <see cref="MzCommands"/>.</summary>
        NotControlFlow,

        /// <summary>It ran, and the index is where the command left it.</summary>
        Ran,

        /// <summary>
        /// The interpreter has stopped and has said why. The index stays where
        /// the command left it, which is what the engine's
        /// <c>if (!this[methodName](...)) return false;</c> does.
        /// </summary>
        Stopped,
    }

    /// <summary>
    /// Runs a command whose only effect is on the index. The three answers are
    /// three things: a first draft returned one bool for two of them, and a
    /// caller reading the first as the second stopped the index moving after the
    /// first command it met.
    /// </summary>
    public static MzControlOutcome TryExecute(
        MzInterpreter pInterpreter, MzCommandEntry pCommand,
        List<MzAction> pActions, MzBranchFacts pFacts)
    {
        switch (pCommand.Code)
        {
            case MzCommandTable.ShowText:
            {
                var branch = MzBranch.FromParameters(pCommand.Parameters);
                var result = MzBranchEvaluator.Evaluate(branch, pFacts);
                pActions.Add(MzAction.Branch(pCommand, result));
                pInterpreter.SetBranch(
                    pCommand.Indent,
                    result.Outcome == MzBranchOutcome.True ? true
                    : result.Outcome == MzBranchOutcome.False ? false
                    : null);

                if (result.Outcome == MzBranchOutcome.Unknown
                    || result.Outcome == MzBranchOutcome.ScriptNotRun)
                {
                    // The engine would have decided this one by running the
                    // game's own code or by asking for something it has. This
                    // reader cannot, so it stops here and says which, rather
                    // than stepping into an arm of a branch it did not decide.
                    pInterpreter.Stop(
                        MzStep.Refused,
                        result.Outcome == MzBranchOutcome.ScriptNotRun
                            ? $"a branch on the author's own script: {result.Missing}"
                            : result.Missing);
                    return MzControlOutcome.Stopped;
                }
                if (result.Outcome == MzBranchOutcome.False
                    && !pInterpreter.SkipBranch())
                {
                    return MzControlOutcome.Stopped;
                }
                return MzControlOutcome.Ran;
            }

            // The engine's else: it skips the branch when the indent does not
            // already hold false, so a branch decided true lets the else be
            // stepped over and a branch not decided at all is treated as false.
            case MzCommandTable.ShowChoiceList:
            {
                // Gemessen: `102 [["Yes", "No"], 1, 0, 2, 0]`, und danach
                // `402 [0, "Yes"]` bei Einzug 0, deren Zweige bei Einzug 1.
                //
                // **Und die Texte stehen zweimal, und beide Stellen sind
                // vollstaendig**: einmal als Liste in `params[0]`, und
                // einmal auf den `402`-Zeilen. **Die Liste ist die
                // Quelle**, **denn sie traegt alle Optionen an einer
                // Stelle**, **und eine `402`, deren Zweig leer ist,
                // traegt trotzdem ihren Text.**
                //
                // **Und der zweite Parameter ist der Abbruchzweig, und
                // nicht die Anzahl** -- **und er ist eine Nummer, die
                // kleiner als die Anzahl der Optionen bedeutet "eine
                // davon bricht ab".**
                //
                // **Und eine Wahl endet da, wo ihr Einzug endet** --
                // **und es gibt kein `412`, das sie schliesst**,
                // **das `412` gehoert zu einem gewoehnlichen `411`-Zweig.**
                var gewaehlt = MzChoice.Read(pCommand.Parameters);
                if (gewaehlt.Options.Count == 0)
                {
                    pInterpreter.Stop(
                        MzStep.Refused,
                        $"a choice at index {pInterpreter.Index} carries no "
                        + "option, and the engine would show the player an "
                        + "empty list");
                    return MzControlOutcome.Stopped;
                }

                // **Und die Zweige kommen von den `402`s, jede mit
                // ihrem Index**, -- **denn der Index steht im ersten
                // Parameter, und 0 ist der erste Zweig.**
                var zweige = new Dictionary<int, int>();
                var ende = pInterpreter.Index + 1;
                for (; ende < pInterpreter.Commands.Count; ende++)
                {
                    var zeile = pInterpreter.Commands[ende];

                    // **Und ein Zweig liegt bei Einzug 1, und der Befehl
                    // bei Einzug 0** -- **und ein `break` an dieser
                    // Stelle hat nach dem ersten Zweig aufgehoert**,
                    // **und so fand der Leser nur die erste Option und
                    // meldete der zweiten einen Zweig, den es nicht
                    // gibt.**
                    if (zeile.Indent > pCommand.Indent)
                    {
                        continue;
                    }

                    if (zeile.Code != MzCommandTable.ChoicesOption
                        || zeile.Parameters.Count < 1)
                    {
                        break;
                    }

                    zweige[MzCommands.At(zeile, 0)] = ende;
                }

                if (zweige.Count == 0)
                {
                    pInterpreter.Stop(
                        MzStep.Refused,
                        $"a choice at index {pInterpreter.Index} is followed "
                        + "by no branch, and the engine would have nothing "
                        + "to jump into");
                    return MzControlOutcome.Stopped;
                }

                pInterpreter.SetChoice(gewaehlt, zweige);
                // **Und der Abbruchzweig steht in `CancelType`, und
                // `MzChoice.Read` hat ihn bereits ausgerechnet** -- **und
                // `NoCancel` ist -2, und nicht 0.** **Eine eigene
                // Rechnung hier waere eine zweite Wahrheit ueber
                // dieselbe Zahl**, **und die beiden laeuften auseinander,
                // sobald das Spiel eine andere Form schreibt.**
                pFacts.StartChoice(
                    gewaehlt, gewaehlt.CancelType != MzChoice.NoCancel);
                pActions.Add(new MzAction(pCommand,
                    $"choice of {gewaehlt.Options.Count}: "
                    + string.Join(" / ", gewaehlt.Options)
                    + (gewaehlt.CancelType != MzChoice.NoCancel
                        ? $", option {gewaehlt.CancelType} cancels"
                        : "")));
                return MzControlOutcome.Ran;
            }

            case MzCommandTable.ChoicesOption:
                // **Eine Option ist kein Befehl, sondern eine Zeile der
                // Wahl.** **Der Index steht auf der `102`, und die Zeilen
                // werden dort alle auf einmal gelesen** -- **und wird
                // eine doch einmal erreicht, wird sie uebersprungen,
                // statt sie als eigenen Befehl auszufuehren.**
                pInterpreter.SkipChoiceOption();
                return MzControlOutcome.Ran;
                return MzControlOutcome.Ran;

            case MzCommandTable.Else:
                if (pInterpreter.BranchAt(pCommand.Indent) != false
                    && !pInterpreter.SkipBranch())
                {
                    return MzControlOutcome.Stopped;
                }
                return MzControlOutcome.Ran;

            // **Und `403` ist das Ende eines Schleifenrumpfes und
            // nicht `412`.** **`412` ist das Ende eines Zweiges und hat
            // keine Methode; `403` hat eine und ueberspringt.**
            //
            // ```js
            // command403() {
            //     if (this._branch[this._indent] >= 0) {
            //         this.skipBranch();
            //     }
            //     return true;
            // }
            // ```
            //
            // **Und `_branch[indent]` ist -1, wenn dort kein Zweig steht,
            // und 0 oder 1, wenn einer steht.** **Und `BranchAt` gibt
            // `null` zurueck, wenn dort nichts gesetzt wurde** -- **und
            // `null != false` ist wahr, wie `null >= 0` es auch waere.**
            //
            // **Und die Wache ist damit in beiden Faellen dasselbe** --
            // **und das ist keine Zufallsuebereinstimmung, sondern die
            // Folge davon, dass diese drei Zustaende in der Engine -1,
            // 0 und 1 heissen und in diesem Leser `null`, `false` und
            // `true`.** **Und mein Kommentar behauptete eine andere
            // Zuordnung** -- **und ein Kommentar, der die falsche
            // nennt, ist schlimmer als keiner.**
                        case MzCommandTable.EndLoop:
                // **Und `>= 0` in JavaScript ist `null >= 0` und nicht
                // `undefined >= 0`.** **`null` wird `+0` und
                // `undefined` wird `NaN`, und jeder Vergleich mit `NaN`
                // ist falsch.** **Also ueberspringt die Engine, wenn ein
                // `jumpTo` diesen Einzug gekreuzt hat (`null`), und
                // nicht, wenn noch nie ein Zweig hier war
                // (`undefined`).**
                //
                // **Und `BranchAt` gab in beiden Faellen `null`
                // zurueck**, **und `!= false` ist in beiden wahr** --
                // **also hat dieser Leser bei jedem `403` uebersprungen,
                // das die Engine stehen laesst.** **Und das ist gemessen:
                // ein `403` bei Einzug null, ohne dass ein `111` dort
                // etwas geschrieben hatte, sprang ueber den Befehl
                // dahinter.**
                if (pInterpreter.StateAt(pCommand.Indent)
                        != MzBranchState.Undecided
                    && !pInterpreter.SkipBranch())
                {
                    return MzControlOutcome.Stopped;
                }

                return MzControlOutcome.Ran;
            case MzCommandTable.Loop:
                return MzControlOutcome.Ran;

            // Repeat above walks back to the first command at its own indent.
            case MzCommandTable.RepeatAbove:
                return pInterpreter.JumpToRepeat()
                    ? MzControlOutcome.Ran
                    : MzControlOutcome.Stopped;

            // Break loop steps over to the repeat that closes this loop.
            case MzCommandTable.BreakLoop:
                return pInterpreter.JumpToEndOfLoop()
                    ? MzControlOutcome.Ran
                    : MzControlOutcome.Stopped;

            // Exit event processing ends the list.
            case MzCommandTable.ExitEventProcessing:
                pInterpreter.Index = pInterpreter.Commands.Count;
                return MzControlOutcome.Ran;

            // A label is a name in the list and nothing else.
            case MzCommandTable.Label:
                return MzControlOutcome.Ran;

            // Jump to label: a label that is not there leaves the index where it
            // was, which is what the engine does and is not a reason to stop.
            case MzCommandTable.JumpToLabel:
                pInterpreter.JumpToLabel(
                    pCommand.Parameters.Count > 0 ? pCommand.Parameters[0] : "");
                return MzControlOutcome.Ran;

            default:
                return MzControlOutcome.NotControlFlow;
        }
    }
}
