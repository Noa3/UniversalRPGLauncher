using System;
using System.Collections.Generic;
using System.Globalization;

namespace UniversalRPG.Web;

/// <summary>
/// Decides a conditional branch, the way the engine does.
/// </summary>
/// <remarks>
/// <para>
/// Every number read here was taken out of the engine's own
/// <c>command111</c>: which parameter holds the kind of thing being tested, which
/// holds the way of testing it, and which hold the operands. A branch is
/// decided from what the caller has and from nothing else.
/// </para>
/// <para>
/// <b>Two things are not decided here, and both say so.</b> A branch that tests
/// a line of the author's own JavaScript is reported and not evaluated, because
/// this repository does not run a game's code. A branch that asks about
/// something the caller has not supplied is reported as missing and not guessed:
/// a branch that came to false because nothing was known is a branch that would
/// skip a game's content, and that is the one failure here that would be
/// invisible.
/// </para>
/// </remarks>
public static class MzBranchEvaluator
{
    public static MzBranchResult Evaluate(MzBranch pBranch, MzBranchFacts pFacts)
    {
        var parameters = pBranch.Parameters;
        var number = At(parameters, 0);

        switch ((MzBranchKind)number)
        {
            case MzBranchKind.Switch:
            {
                var id = At(parameters, 1);

                // **Und ein Schalter, den diese Faktoren nicht kennt,
                // ist aus, und nicht unbekannt.**
                //
                // **Gemessen an `Game_Switches.prototype.value`:**
                // `return !!this._data[switchId];`** -- **und
                // `setValue` schreibt nur, wenn
                // `switchId > 0 && switchId < $dataSystem.switches.length`**
                // -- **und also ist jeder Schalter ausserhalb dieser
                // Grenzen immer aus.**
                //
                // **Und der Motor vergleicht `value(params[1]) === (params[2]
                // === 0)`** -- **und `false === false` ist wahr** --
                // **und eine Verzweigung, die nach einem unbekannten
                // Schalter fragt, nimmt in diesem Spiel also den
                // "ist aus"-Zweig.**
                //
                // **Und gemessen ist `[0, 10, 0]` aus Map011 Event 6,
                // und ohne diese Regel waere diese eine Verzweigung des
                // Spiels unbeantwortbar** -- **und ein Leser, der hier
                // verweigert, laesst die Seite anhalten, wo das Spiel
                // weiterlaeuft.**
                var on = pFacts.Switches.TryGetValue(id, out var gefunden)
                    && gefunden;
                // The second parameter says what is being asked: 0 asks whether
                // the switch is on, anything else asks whether it is off.
                // **Und `params[2] === 0` heisst "ist AUS", und nicht
                // "ist AN".**
                //
                // **Und gemessen ist `command111`:
                // `result = $gameSwitches.value(params[1]) === (params[2]
                // === 0);`** -- **und die Klammern stehen um die
                // zweite Bedingung, und nicht um die erste.**
                //
                // **Und in C# verkettet `==` von links, und
                // `At(parameters, 2) == 0 == on` liest sich als
                // `(0 == 0) == on`, und das ist ein Vergleich zweier
                // `bool` in ihrer Zahlenform** -- **und `false == false`
                // ist nicht wahr, sondern eine Compile-Zahl von `0`,
                // und `false` ist auch `0`, und `0 == 0` ist wahr.**
                // **Und damit war die Frage "ist Schalter 99 aus"
                // immer falsch, egal wie der Schalter stand.**
                var fragtAus = At(parameters, 2) == 0;
                return MzBranchResult.Of(
                    on == fragtAus
                        ? MzBranchOutcome.True
                        : MzBranchOutcome.False);
            }

            case MzBranchKind.Variable:
            {
                var id = At(parameters, 1);
                if (!pFacts.Variables.TryGetValue(id, out var left))
                {
                    return MzBranchResult.Needs($"variable {id}", "", false);
                }
                // The third parameter says only whether the right side is a
                // number or a variable. The number or the variable's own number
                // is the fourth. Reading the third as the variable asks about
                // variable one where the game asked about variable seventy
                // eight, and a branch about a variable nobody has would be
                // refused for the wrong one.
                var isVariable = At(parameters, 2) != 0;
                var operand = At(parameters, 3);
                var right = 0;
                if (isVariable)
                {
                    if (!pFacts.Variables.TryGetValue(operand, out right))
                    {
                        return MzBranchResult.Needs(
                            $"variable {operand}", Comparison(At(parameters, 4)), true);
                    }
                }
                else
                {
                    right = operand;
                }
                return Compare(left, right, (MzComparison)At(parameters, 4));
            }

            case MzBranchKind.SelfSwitch:
            {
                // A self switch is named by the map, the event and the letter,
                // and this reader is given the whole key as text.
                // **Und derselbe Fehler wie beim Schalter, und an derselben Stelle.**
                //
                // **Gemessen an `command111`, Fall 2:** `if (this._eventId > 0) {
                // const key = [this._mapId, this._eventId, params[1]];
                // result = $gameSelfSwitches.value(key) === (params[2] === 0); }`
                //
                // **Und `_eventId <= 0` laesst `result` bei `false`,**
                // **und `params[2] === 0` fragt nach "ist AUS",**
                // **und ein nicht gesetzter Selbstschalter ist aus.**
                var key = Text(parameters, 1);
                var on = pFacts.SelfSwitches.TryGetValue(key, out var gesetzt)
                    && gesetzt;

                var fragt = At(parameters, 2) == 0;
                return MzBranchResult.Of(
                    on == fragt
                        ? MzBranchOutcome.True
                        : MzBranchOutcome.False);
            }

            case MzBranchKind.Timer:
            {
                // There is no number for which timer in the parameters. The
                // engine reads the second as the threshold in seconds and the
                // third as the way, and it asks the one timer the event owns.
                // This reader treated the second as the timer's number, so a
                // branch on a five second timer asked for a timer called five,
                // found none, and refused a branch it could have answered.
                var threshold = At(parameters, 1);
                var seconds = pFacts.TimerSeconds;
                if (seconds < 0)
                {
                    return MzBranchResult.Needs(
                        "the event's timer, which is not running",
                        Comparison(At(parameters, 2)), true);
                }
                // The engine counts a running timer in frames and divides by
                // sixty, and a timer that is not running is not compared at all.
                return At(parameters, 2) == 0
                    ? MzBranchResult.Compared(
                        seconds >= threshold
                            ? MzBranchOutcome.True
                            : MzBranchOutcome.False,
                        "at least")
                    : MzBranchResult.Compared(
                        seconds <= threshold
                            ? MzBranchOutcome.True
                            : MzBranchOutcome.False,
                        "at most");
            }

            case MzBranchKind.Gold:
            {
                // Gold has a numbering of its own and it is not the variable's.
                // The engine reads `switch (params[2])` with case 0 at least,
                // case 1 at most and case 2 less, where a variable's case 0 is
                // equal to and case 1 is at least. Reading the gold number
                // through the variable's numbering tested a hundred gold for
                // equality and said it was not enough, and a game that spends
                // gold would never spend any.
                var wanted = At(parameters, 1);
                return At(parameters, 2) switch
                {
                    0 => Compare(pFacts.Gold, wanted, MzComparison.AtLeast),
                    1 => Compare(pFacts.Gold, wanted, MzComparison.AtMost),
                    2 => Compare(pFacts.Gold, wanted, MzComparison.Less),
                    // Three ways and no fourth. A fourth would be from a newer
                    // editor, and a reader that guessed which was meant would be
                    // inventing a rule about a game's money.
                    var other => MzBranchResult.Needs(
                        $"a way of testing gold this reader has no name for"
                        + $" ({other})", "", false),
                };
            }

            case MzBranchKind.Item:
                return Held(pFacts.Items, At(parameters, 1), 0, "item");

            case MzBranchKind.Weapon:
            case MzBranchKind.Armor:
            {
                var bag = pBranch.Kind == MzBranchKind.Weapon
                    ? pFacts.Weapons
                    : pFacts.Armors;
                return Held(
                    bag, At(parameters, 1), At(parameters, 2),
                    pBranch.Kind == MzBranchKind.Weapon ? "weapon" : "armor");
            }

            case MzBranchKind.Actor:
            {
                var id = At(parameters, 1);
                if (!pFacts.PartyMembers.Contains(id))
                {
                    return MzBranchResult.Needs($"actor {id}", "", false);
                }
                return (MzComparison)At(parameters, 2) switch
                {
                    // The engine's own way of asking: is this actor in the party.
                    MzComparison.Equal => MzBranchResult.Of(MzBranchOutcome.True),
                    // The other six need the actor's own fields, which are a
                    // database this reader has not opened yet.
                    _ => MzBranchResult.Needs(
                        $"what actor {id} has, for the way of testing"
                        + $" {At(parameters, 2)}", "", false),
                };
            }

            case MzBranchKind.Script:
            {
                // The engine writes `result = !!eval(params[1])` here. This
                // repository does not evaluate a game's JavaScript, and the one
                // thing it may not do is pretend to have.
                //
                // **Und trotzdem gibt es jetzt eine Ausnahme**, und sie ist
                // eine Liste und keine Aufhebung:
                //
                // ```text
                // Utils.isOptionValid("test")        118x
                // Utils.isMobileDevice()              50x
                // -- und in den Bedingungen selbst:
                // Utils.isOptionValid("test")         95x
                // !Utils.isOptionValid("test")          6x
                // Utils.isMobileDevice()              32x
                // !Utils.isMobileDevice()              9x
                // -- von 4952 Bedingungen des Typs 12 in einem
                //    fertigen Spiel
                // ```
                //
                // **Und beide stehen in `rpg_core.js` und nicht in einem
                // Plugin und nicht im Skript des Autors** -- **und diese
                // Datei liest dieses Repository fuer jede andere Regel
                // bereits.** **Und beide haben auf einem Desktop genau eine
                // Antwort, und es ist jedes Mal dieselbe, und keine von
                // beiden liest ein Spiel, einen Spielstand oder eine
                // Variable.**
                //
                // **Und alles andere bleibt eine Verweigerung** -- **und
                // `Utils.isOptionValid("test") || $gameVariables.value(25) >
                // 50` bleibt eine, denn die Haelfte davon ist das Skript
                // des Autors, und ein Leser, der die erste Haelfte
                // beantwortete, behauptete ein Ergebnis, das er nicht
                // berechnet hat.** **Und dieses Spiel schreibt genau das,
                // einmal.**
                // **Und bevor die Liste der Engine-Fragen kommt, kommt
                // die Rechnung** -- **denn eine Bedingung, die nur Zahlen
                // und `$gameVariables.value(n)` liest, ist Arithmetik
                // und keine Skriptfrage.**
                //
                // **Und gemessen an `D:/Itch/sister/www`: 364 von 4952
                // Skriptbedingungen sind genau das**, **und die haeufigste
                // Form ist ein Radius um einen Punkt auf der Karte:**
                //
                // ```js
                // ($gameVariables.value(4) - 1107) ** 2
                //   + ($gameVariables.value(5) - 612) ** 2 <= 113 ** 2
                // ```
                //
                // **Und die Rechnung wird nur gemacht, wenn sie ganz
                // aufgeht** -- **und ein Ausdruck mit einem Namen darin
                // geht nicht auf und bleibt die Verweigerung, die sie
                // war.**
                // **Und eine Rechnung, in der eine Variable vorkommt, die
                // das Spiel nie geschrieben hat, ist nicht dieselbe wie
                // eine, in der eine Null steht.**
                //
                // `Game_Variables.prototype.value` gibt null zurueck,
                // wenn die Variable leer ist, **und eine leere Variable
                // ist nicht dieselbe wie eine, die auf null
                // geschrieben wurde.** **Und gemessen an diesem Spiel:
                // 3280 seiner Variablen werden nie geschrieben, und
                // `$gameVariables.value(3280) > 5` ist eine Frage ueber
                // etwas, das es nicht gibt.**
                //
                // **Und `$gameScreen.picture(40)` ist derselbe Fall und
                // noch deutlicher**: **eine Frage ueber ein Bild, das nie
                // gezeigt wurde.** **Und die Rechnung geht trotzdem auf,
                // weil der Leser fuer beides eine Zahl hat.**
                if (!MzArithmetic.KenntAlleVariablen(
                    pBranch.ScriptText, pFacts))
                {
                    var fehlend = MzArithmetic.WelcheVariableFehlt(
                        pBranch.ScriptText, pFacts);
                    return MzBranchResult.ScriptNotRun(
                        "variable " + fehlend + ", which this game has "
                        + "never written, and a variable that was never "
                        + "written is not the same as one that was written "
                        + "as zero");
                }

                var gerechnet = MzArithmetic.TryRead(
                    pBranch.ScriptText, pFacts, out var rechenFehler);
                if (gerechnet.HasValue)
                {
                    return MzArithmetic.IstWahr(gerechnet.Value)
                        ? MzBranchResult.Of(MzBranchOutcome.True)
                        : MzBranchResult.Of(MzBranchOutcome.False);
                }

                var antwort = MzEngineCondition.Answer(
                    pBranch.ScriptText, pFacts, out var warum);
                if (antwort.HasValue)
                {
                    return antwort.Value
                        ? MzBranchResult.Of(MzBranchOutcome.True)
                        : MzBranchResult.Of(MzBranchOutcome.False);
                }

                // **Und es ist `ScriptNotRun` und nicht `Unknown`**, **denn
                // es fehlt nichts: es wird nur nichts gerechnet.** **Und
                // `Unknown` sagt "da ist etwas, das dieser Leser noch
                // nicht hat", und das waere hier falsch.**
                return MzBranchResult.ScriptNotRun(warum);
            }

            case MzBranchKind.Enemy:
            case MzBranchKind.Character:
            case MzBranchKind.Button:
            case MzBranchKind.Vehicle:
                return MzBranchResult.Needs(
                    $"the {pBranch.Kind.ToString().ToLowerInvariant()} this branch"
                    + " asks about", "", false);

            default:
                return MzBranchResult.Needs(
                    $"a kind of branch this reader has no name for ({number})",
                    "", false);
        }
    }

    private static MzBranchResult Held(
        Dictionary<int, int> pBag, int pId, int pAtLeast, string pWhat)
    {
        if (!pBag.TryGetValue(pId, out var held))
        {
            return MzBranchResult.Needs($"{pWhat} {pId}", "", false);
        }
        return MzBranchResult.Of(
            held >= pAtLeast ? MzBranchOutcome.True : MzBranchOutcome.False);
    }

    private static MzBranchResult Compare(
        int pLeft, int pRight, MzComparison pComparison)
    {
        var outcome = pComparison switch
        {
            MzComparison.Equal => pLeft == pRight,
            MzComparison.AtLeast => pLeft >= pRight,
            MzComparison.AtMost => pLeft <= pRight,
            MzComparison.Greater => pLeft > pRight,
            MzComparison.Less => pLeft < pRight,
            MzComparison.NotEqual => pLeft != pRight,
            _ => (bool?)null,
        };
        if (outcome == null)
        {
            return MzBranchResult.Needs(
                $"a way of comparing this reader has no name for"
                + $" ({(int)pComparison})", Comparison((int)pComparison), false);
        }
        return MzBranchResult.Compared(
            outcome.Value ? MzBranchOutcome.True : MzBranchOutcome.False,
            Comparison((int)pComparison));
    }

    private static string Comparison(int pComparison) => pComparison switch
    {
        0 => "equal to",
        1 => "at least",
        2 => "at most",
        3 => "greater than",
        4 => "less than",
        5 => "not equal to",
        _ => $"a comparison this reader has no name for ({pComparison})",
    };

    /// <summary>
    /// A parameter as a whole number. A parameter the editor left empty is zero,
    /// which is what the engine reads it as, and a parameter holding something
    /// that is not a number is refused rather than taken as zero.
    /// </summary>
    private static int At(IReadOnlyList<string> pParameters, int pIndex)
    {
        if (pIndex >= pParameters.Count || pParameters[pIndex] == "")
        {
            return 0;
        }
        if (int.TryParse(
            pParameters[pIndex], NumberStyles.Integer,
            CultureInfo.InvariantCulture, out var value))
        {
            return value;
        }
        if (double.TryParse(
            pParameters[pIndex], NumberStyles.Float,
            CultureInfo.InvariantCulture, out var real))
        {
            return (int)real;
        }
        throw new MzDataException(
            $"a branch holds '{pParameters[pIndex]}' at position {pIndex}, which"
            + " is not a number the engine would compare");
    }

    private static string Text(IReadOnlyList<string> pParameters, int pIndex) =>
        pIndex < pParameters.Count ? pParameters[pIndex] : "";
}
