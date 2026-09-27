using System.Collections.Generic;

namespace UniversalRPG.Web;

/// <summary>
/// Something a command asked the game to do, as a record rather than as an
/// effect.
/// </summary>
/// <remarks>
/// The interpreter does not change the game. It walks a list of commands,
/// decides the branches it meets, and writes down what each command asked for, so
/// a caller can act on it and can see what was skipped. That is a deliberate
/// boundary: nothing here opens a file, moves a character or shows a window.
/// </remarks>
public readonly record struct MzAction(MzCommandEntry Command, string What)
{
    /// <summary>The command's number, so a caller can group them.</summary>
    public int Code => Command.Code;

    /// <summary>How deep the command sat, which is what says whether it ran.</summary>
    public int Indent => Command.Indent;

    public static MzAction Branch(MzCommandEntry pCommand, MzBranchResult pResult) =>
        new(
            pCommand,
            $"branch {pResult.Outcome}"
            + (pResult.Comparison == "" ? "" : $" by {pResult.Comparison}")
            + (pResult.Missing == "" ? "" : $": needs {pResult.Missing}"));

    public static MzAction Switches(
        MzCommandEntry pCommand, int pFrom, int pTo, bool pOn) =>
        new(pCommand, $"switch {pFrom} to {pTo} set {(pOn ? "on" : "off")}");

    public static MzAction Variable(
        MzCommandEntry pCommand, int pId, int pWas, int pIs, string pHow) =>
        new(pCommand, $"variable {pId} {pHow} {pWas} to {pIs}");

    /// <summary>
    /// A wait, with the frames it asked for. The engine counts these down one
    /// per frame, and a run with no frames is waiting rather than finished.
    /// </summary>
    public static MzAction Wait(MzCommandEntry pCommand, int pFrames) =>
        new(pCommand, pFrames > 0
            ? $"wait {pFrames} frames"
            : "wait for something outside this reader");

    /// <summary>A common event being called, by the index the game named.</summary>
    public static MzAction CommonEvent(MzCommandEntry pCommand, int pIndex) =>
        new(pCommand, $"call common event {pIndex}");
}

/// <summary>
/// The commands that change the game's numbers, as recorded changes.
/// </summary>
/// <remarks>
/// <para>
/// Written from <c>command121</c> and <c>command122</c>. Two details in the
/// engine are not what a first reading gives, and both are kept here.
/// </para>
/// <para>
/// <b>A range is a range.</b> Both commands run from the first id to the last
/// one inclusive, so a command that names a range switches every switch in it.
/// </para>
/// <para>
/// <b>Dividing by zero writes zero.</b> The engine wraps each operation in a
/// try and, on any failure, sets the variable to zero. A reader that let the
/// failure out would stop a game the engine plays on.
/// </para>
/// <para>
/// The fourth operand of a variable command is the author's own script, and the
/// engine evaluates it. This repository does not, so such a command is refused
/// and says so, and the other five operands are read.
/// </para>
/// </remarks>
public static class MzCommands
{
    /// <summary>
    /// Is this a command this reader acts on? A command it acts on neither is
    /// one the engine has no method for — which the engine steps over, and so
    /// does this — nor one whose effect belongs to a part of the runtime that
    /// does not exist yet. Both are stepped over rather than refused, because
    /// refusing would strand a game on a command the engine itself ran past.
    /// </summary>
    /// <remarks>
    /// The name says "effect" and not "method" on purpose. The engine's own
    /// question is <c>typeof this["command" + code] === "function"</c>, and
    /// <b>every command this game stores that has no method is one it stores on
    /// purpose</b>: 0 the end of a block, 401 a line of text under a 101, 412
    /// the end of a branch, and 655 and 657 the two halves of a script. Answering
    /// the engine's question exactly would mean calling all five of those
    /// "stepped over because there is no method", which is true of none of them
    /// and would hide the reason a command did nothing.
    /// </remarks>
    public static bool HasEffect(int pCode) =>
        pCode is MzCommandTable.ShowText
            or MzCommandTable.Else
            or MzCommandTable.Loop
            or MzCommandTable.BreakLoop
            or MzCommandTable.RepeatAbove
            or MzCommandTable.ExitEventProcessing
            or MzCommandTable.Label
            or MzCommandTable.JumpToLabel
            or MzCommandTable.ControlSwitches
            or MzCommandTable.ControlVariables
            or MzCommandTable.ChangeItems
            or MzCommandTable.ShowPicture
            or MzCommandTable.MovePicture
            or MzCommandTable.ErasePicture
            or MzCommandTable.Wait;

    /// <summary>
    /// Runs a command that changes the game's numbers. It returns false when the
    /// interpreter has stopped, which only the operand that is a script does.
    /// </summary>
    public static bool TryExecute(
        MzInterpreter pInterpreter, MzCommandEntry pCommand,
        List<MzAction> pActions, MzBranchFacts pFacts, MzRandom pRandom)
    {
        switch (pCommand.Code)
        {
            case MzCommandTable.ShowPicture:
            {
                // `command231` is
                //   const point = this.picturePoint(params);
                //   $gameScreen.showPicture(params[0], params[1], params[2],
                //       point.x, point.y, params[6], params[7], params[8], params[9]);
                // and `picturePoint` is
                //   if (params[3] === 0) { point.x = params[4]; point.y = params[5]; }
                //   else { point.x = $gameVariables.value(params[4]);
                //          point.y = $gameVariables.value(params[5]); }
                // **so the fourth parameter decides where the other two are
                // read from** — a number written in the event, or a variable
                // to look up. A first draft read them as numbers and a game
                // that places a picture from a variable would have put it at
                // the variable's own number.
                var show = Point(pCommand, pFacts, out var shownX, out var shownY);
                var said = pFacts.Screen.Show(
                    At(pCommand, 0),
                    Text(pCommand, 1),
                    At(pCommand, 2),
                    shownX, shownY,
                    At(pCommand, 6),
                    At(pCommand, 7),
                    At(pCommand, 8),
                    At(pCommand, 9));
                pActions.Add(new MzAction(pCommand, said));
                _ = show;
                return true;
            }

            case MzCommandTable.MovePicture:
            {
                // `command232` is
                //   $gameScreen.movePicture(params[0], params[2], point.x, point.y,
                //       params[6], params[7], params[8], params[9], params[10],
                //       params[12] || 0);
                //   if (params[11]) { this.wait(params[10]); }
                // **and there is no second one** — the reader that assumed
                // there was one would wait on every move, and this game asks
                // for the wait on two of its four and not on the other two.
                Point(pCommand, pFacts, out var movedX, out var movedY);
                var moving = pFacts.Screen.Move(
                    At(pCommand, 0),
                    At(pCommand, 2),
                    movedX, movedY,
                    At(pCommand, 6),
                    At(pCommand, 7),
                    At(pCommand, 8),
                    At(pCommand, 9),
                    At(pCommand, 10),
                    // `params[12] || 0` — an easing the game did not write is
                    // zero, and not "whatever is in the next slot".
                    At(pCommand, 12),
                    Truth(pCommand, 11),
                    out var frames);

                // **The command runs and the index moves, and the wait is the
                // interpreter's, not this command's.** A first draft returned
                // false here to hold the list up, and that is what
                // `return false` means to this reader: the index stays where
                // it was. So the next frame read the same 232 again, set the
                // same twenty frames again, and the picture never arrived —
                // a move that had to wait became a move that waited for ever.
                //
                // The engine has none of this trouble because `command232`
                // ends in `return true` whatever it asked for, and the wait it
                // set lives in `this._waitCount` where the next command cannot
                // reach it. **That is the shape to keep**: the command is done,
                // and the frame belongs to the interpreter.
                pActions.Add(new MzAction(pCommand, moving));
                if (frames > 0)
                {
                    pActions.Add(MzAction.Wait(pCommand, frames));
                    pInterpreter.Wait(frames);
                }
                return true;
            }

            case MzCommandTable.ErasePicture:
            {
                // `command235` is `$gameScreen.erasePicture(params[0])`, which
                // sets the slot to null. One parameter, and a picture that is
                // not there is not an error.
                pActions.Add(new MzAction(
                    pCommand, pFacts.Screen.Erase(At(pCommand, 0))));
                return true;
            }

            case MzCommandTable.ChangeItems:
            {
                // `command126` is
                //   const value = this.operateValue(params[1], params[2], params[3]);
                //   $gameParty.gainItem($dataItems[params[0]], value);
                // and **all four parameters are read**: the item, the
                // operation, the operand's kind and the operand itself. A first
                // draft read the item and the number and left the other two
                // out, which made every 126 in this game a gain of a literal —
                // and the game writes nine of them as a gain of 999, which the
                // engine refuses to hold.
                // The party is built over **the facts this interpreter was
                // given**, so it knows the ids only when the caller has read
                // this game's `Items.json`. **A reader with no data behind it
                // must not answer for an item it cannot name**, and building
                // the party without the ids made every 126 a silent no-op:
                // the first draft of the wiring test caught exactly that, with
                // every count coming back zero and nothing saying why.
                var party = new MzParty(pFacts, pFacts.KnownItems);
                var said = party.GainItem(
                    At(pCommand, 0),
                    At(pCommand, 1),
                    At(pCommand, 2),
                    At(pCommand, 3),
                    pFacts.Variables.TryGetValue(At(pCommand, 3), out var held)
                        ? held
                        : 0);
                pActions.Add(new MzAction(pCommand, said));
                return true;
            }

            case MzCommandTable.Wait:
            {
                // The engine's wait is `this._waitCount = params[0]`, and
                // `updateWaitCount` takes one off it per frame and breaks the
                // frame while it is above zero. **A command that is waiting does
                // not advance the index**, so the same 230 is read again next
                // frame, and a reader that stepped over it would run the rest of
                // the list a whole list of frames too early.
                //
                // There are no frames here, so the run is handed back waiting and
                // the caller decides when the next frame is. The count is kept so
                // that it can be counted down.
                var frames = At(pCommand, 0);
                pActions.Add(MzAction.Wait(pCommand, frames));
                pInterpreter.Wait(frames);
                // `false` here does not mean "carry on" and `true` would not
                // mean it either. `ExecuteOne` finishes with
                // `return Stopped == MzStep.Stepped`, and `Wait` has just set
                // `Stopped` to `Waiting`, so the run stops either way. **This
                // return value is a dead branch** — a first mutation run proved
                // it, by changing this to `true` and watching every test still
                // pass. It is left as `false` because it says what the command
                // meant, and the next branch in this switch is a real one.
                return false;
            }

            case MzCommandTable.ControlSwitches:
            {
                // for (let i = params[0]; i <= params[1]; i++) — inclusive, and
                // the third parameter says which way, 0 being on.
                var from = At(pCommand, 0);
                var to = At(pCommand, 1);
                var on = At(pCommand, 2) == 0;
                foreach (var id in Range(from, to))
                {
                    pFacts.SetSwitch(id, on);
                    pActions.Add(MzAction.Switches(pCommand, id, id, on));
                }
                return true;
            }

            case MzCommandTable.ControlVariables:
            {
                if (!TryOperand(pCommand, pFacts, out var value, out var missing))
                {
                    pInterpreter.Stop(MzStep.Refused, missing);
                    return false;
                }
                // A random operand picks a number in its own range, and the
                // engine draws **inside** its loop:
                //
                //     for (let i = startId; i <= endId; i++) {
                //         const realValue = value + Math.randomInt(randomMax);
                //         this.operateVariable(i, operationType, realValue);
                //     }
                //
                // so every variable of a range gets its own roll. That is kept
                // because a reader that drew once for the range would give a
                // game the same number five times over in a roll of "how many
                // of these did I get" — and this comment once said the opposite
                // of what the line below it does.
                var randomMax = 1;
                if ((MzOperand)At(pCommand, 3) == MzOperand.Random)
                {
                    var low = At(pCommand, 4);
                    randomMax = System.Math.Max(At(pCommand, 5) - low + 1, 1);
                    value = low;
                }
                var operation = (MzOperation)At(pCommand, 2);
                var from = At(pCommand, 0);
                var to = At(pCommand, 1);
                foreach (var id in Range(from, to))
                {
                    var was = pFacts.Variable(id);
                    // The engine draws inside its own loop:
                    //
                    //     for (let i = startId; i <= endId; i++) {
                    //         const realValue = value + Math.randomInt(randomMax);
                    //         this.operateVariable(i, operationType, realValue);
                    //     }
                    //
                    // so every variable of a range gets its OWN roll. A reader
                    // that drew once for the range would give a game the same
                    // number five times over in a roll of "how many of these did
                    // I get", and this file's own comment claimed that was the
                    // engine's shape until the source was read again.
                    var drawn = randomMax > 1
                        ? value + (pRandom?.Next(randomMax) ?? 0)
                        : value;
                    var is_ = Operate(was, drawn, operation);
                    pFacts.SetVariable(id, is_);
                    pActions.Add(MzAction.Variable(
                        pCommand, id, was, is_, Operation(operation)));
                }
                return true;
            }

            default:
                return true;
        }
    }

    /// <summary>
    /// The value a variable command works with. Five of the engine's six
    /// operands are read here; the sixth is the author's own script and is
    /// refused, because this repository does not evaluate a game's JavaScript.
    /// </summary>
    private static bool TryOperand(
        MzCommandEntry pCommand, MzBranchFacts pFacts,
        out int pValue, out string pMissing)
    {
        pValue = 0;
        pMissing = "";
        switch ((MzOperand)At(pCommand, 3))
        {
            case MzOperand.Constant:
                pValue = At(pCommand, 4);
                return true;

            case MzOperand.Variable:
            {
                var id = At(pCommand, 4);
                if (!pFacts.HasVariable(id))
                {
                    pMissing = $"variable {id}, which the command works with";
                    return false;
                }
                pValue = pFacts.Variable(id);
                return true;
            }

            case MzOperand.Random:
                // The drawn value is added by the caller, which also holds the
                // range; here the low end is enough to say it is answerable.
                pValue = At(pCommand, 4);
                return true;

            case MzOperand.GameData:
            {
                // Nine kinds of thing the game can count, named by the engine.
                // Each needs a database this reader has not opened, so each is
                // refused by name rather than answered with a zero that would
                // look like the engine had counted nothing.
                var what = At(pCommand, 4) switch
                {
                    0 => "the number of items held",
                    1 => "the number of weapons held",
                    2 => "the number of armours held",
                    3 => "an actor's weapon count",
                    4 => "an actor's armour count",
                    5 => "another actor's weapon count",
                    6 => "another actor's armour count",
                    7 => "an enemy's number",
                    8 => "how often the player has escaped",
                    9 => "how many times the player has saved",
                    var other =>
                        $"a count this reader has no name for ({other})",
                };
                pMissing = what;
                return false;
            }

            case MzOperand.Script:
                // The engine writes `value = eval(params[4])`. This repository
                // does not evaluate a game's JavaScript and the one thing it may
                // not do is answer as though it had.
                pMissing = "the author's own script, which is not run here";
                return false;

            default:
                pMissing = $"an operand this reader has no name for"
                    + $" ({At(pCommand, 3)})";
                return false;
        }
    }

    /// <summary>
    /// The engine's six operations. Dividing by zero and taking a remainder of
    /// zero both come to zero here, because the engine catches the failure and
    /// writes zero rather than letting it out.
    /// </summary>
    internal static int Operate(int pWas, int pValue, MzOperation pOperation)
    {
        try
        {
            return pOperation switch
            {
                MzOperation.Set => pValue,
                MzOperation.Add => pWas + pValue,
                MzOperation.Subtract => pWas - pValue,
                MzOperation.Multiply => pWas * pValue,
                MzOperation.Divide => pWas / pValue,
                MzOperation.Modulo => pWas % pValue,
                _ => throw new MzDataException(
                    $"an operation this reader has no name for"
                    + $" ({(int)pOperation})"),
            };
        }
        catch (System.ArithmeticException)
        {
            // The engine catches anything and writes zero. So does this.
            return 0;
        }
    }

    private static string Operation(MzOperation pOperation) => pOperation switch
    {
        MzOperation.Set => "set from",
        MzOperation.Add => "added to",
        MzOperation.Subtract => "less",
        MzOperation.Multiply => "times",
        MzOperation.Divide => "divided by",
        MzOperation.Modulo => "mod",
        _ => "by an operation this reader has no name for",
    };

    /// <summary>
    /// Where a picture goes, as <c>picturePoint</c> answers it. **The fourth
    /// parameter decides where the fifth and sixth are read from**: a number
    /// written in the event, or the number a variable holds.
    /// </summary>
    private static bool Point(
        MzCommandEntry pCommand, MzBranchFacts pFacts,
        out int pX, out int pY)
    {
        pX = 0;
        pY = 0;
        var fromVariables = At(pCommand, 3) != 0;
        pX = From(pCommand, pFacts, 4, fromVariables);
        pY = From(pCommand, pFacts, 5, fromVariables);
        return fromVariables;
    }

    private static int From(
        MzCommandEntry pCommand, MzBranchFacts pFacts, int pIndex, bool pVariable) =>
        pVariable
        ? pFacts.Variables.TryGetValue(At(pCommand, pIndex), out var held)
            ? held
            : 0
        : At(pCommand, pIndex);

    /// <summary>
    /// A parameter read the way <c>if (params[11])</c> reads it.
    /// </summary>
    /// <remarks>
    /// **A truth value, not a number.** RPG Maker writes booleans into the
    /// event list as <c>"1"</c> and <c>""</c>, and a game may leave the slot
    /// out entirely. A reader that called <c>int.Parse</c> on the string
    /// would throw on an empty one — and a reader that read an empty string as
    /// zero would be right by accident, while a reader that read a missing
    /// parameter as a number at all would not.
    /// </remarks>
    private static bool Truth(MzCommandEntry pCommand, int pIndex)
    {
        if (pIndex >= pCommand.Parameters.Count)
        {
            return false;
        }
        var written = pCommand.Parameters[pIndex];
        return written == "1"
            || string.Equals(written, "true", System.StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// A parameter the game wrote as text — a picture's file name, which is
    /// the only string a 231 carries.
    /// </summary>
    private static string Text(MzCommandEntry pCommand, int pIndex) =>
        pIndex < pCommand.Parameters.Count ? pCommand.Parameters[pIndex] : "";

    private static int At(MzCommandEntry pCommand, int pIndex) =>
        pIndex < pCommand.Parameters.Count
        && int.TryParse(
            pCommand.Parameters[pIndex],
            System.Globalization.NumberStyles.Integer,
            System.Globalization.CultureInfo.InvariantCulture, out var value)
            ? value
            : 0;

    /// <summary>
    /// Every number from the first to the last, both ends in, which is what the
    /// engine's own loop over a range does.
    /// </summary>
    internal static IEnumerable<int> Range(int pFrom, int pTo)
    {
        for (var i = pFrom; i <= pTo; i++)
        {
            yield return i;
        }
    }
}
