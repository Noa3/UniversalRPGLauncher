using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace UniversalRPG.Web;

/// <summary>
/// A 101, and everything it swallows while it runs.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is the first command in this reader that eats other commands.</b>
/// <c>command101</c> is
///
/// <c>
/// if ($gameMessage.isBusy()) { return false; }
/// $gameMessage.setFaceImage(params[0], params[1]);
/// …
/// while (this.nextEventCode() === 401) {
///     this._index++;
///     $gameMessage.add(this.currentCommand().parameters[0]);
/// }
/// switch (this.nextEventCode()) {
///     case 102: this._index++; this.setupChoices(…); break;
///     case 103: this._index++;
///     case 104: this._index++;
/// }
/// this.setWaitMode("message");
/// return true;
/// </c>
///
/// <para>
/// <b>So the lines of a dialogue are never dispatched.</b> There is no
/// <c>command401</c> to dispatch them to — <c>nextEventCode()</c> looks
/// <b>one ahead</b> and the 101 steps the index over each line itself. A
/// reader that ran a 401 as a command of its own would be running something
/// the engine never runs, and a reader that read the lines as they came past
/// would show a game's dialogue in the wrong order.
/// </para>
///
/// <para>
/// <b>Three rules that a first reading gets wrong.</b>
///
/// <list type="number">
/// <item><b>A dialogue that is already up is refused.</b>
/// <c>if ($gameMessage.isBusy()) return false;</c> — and
/// <c>isBusy</c> is <c>hasText() || isChoice() || isNumberInput() ||
/// isItemChoice()</c>, so a second 101 while a choice is on the screen is
/// refused as firmly as a second one while a line is up. The index stays and
/// the second dialogue happens when the first is done.</item>
///
/// <item><b>Exactly one of 102, 103 and 104 is taken, and it is the one
/// immediately after the last line.</b> The <c>switch</c> runs once — it is
/// not a loop, and there is no <c>break</c> outside the three cases, so a
/// 102 that is <b>not</b> right after the last line is not taken at all and
/// the interpreter will reach it later as a command of its own. This game has
/// eight 102s and all eight sit directly after their lines.</item>
///
/// <item><b>And it always ends in a wait, whatever it found.</b>
/// <c>this.setWaitMode("message")</c> is outside the switch, so a dialogue
/// with no choice still holds its page — <c>updateWaitMode</c> answers
/// <c>waiting = $gameMessage.isBusy()</c>, and a line is up.</item>
/// </list>
///
/// <para>
/// <b>This game's own numbers, measured over the files:</b> 414 dialogues,
/// 938 lines between them — <b>one to four each</b>, 118 with one, 130 with
/// two, 104 with three and 62 with four — and the total is exactly 938, so
/// <b>every line of text in this game belongs to a 101 and to nothing
/// else.</b> Eight of the 414 are followed by a 102, and there is no 103, no
/// 104, no 403 anywhere in nineteen maps.
/// </para>
/// </remarks>
public sealed class MzDialogue
{
    /// <summary>One dialogue, as <c>command101</c> leaves it.</summary>
    public sealed class Block
    {
        /// <summary>The face, and the person it belongs to.</summary>
        public int FaceName { get; init; }

        public int FaceIndex { get; init; }

        /// <summary><c>setBackground</c> — 0 window, 1 faded, 2 none.</summary>
        public int Background { get; init; }

        /// <summary><c>setPositionType</c> — 0 top, 1 middle, 2 bottom.</summary>
        public int Position { get; init; }

        /// <summary><c>setSpeakerName</c>, and <b>an empty one means no
        /// name</b>: <c>speakerName ? speakerName : ""</c>.</summary>
        public string SpeakerName { get; init; } = "";

        /// <summary>The lines, in the order the game wrote them, each one
        /// read through <see cref="MzMessage"/>.</summary>
        public IReadOnlyList<MzMessage.Line> Lines { get; init; } =
            System.Array.Empty<MzMessage.Line>();

        /// <summary>What followed the last line, as the <c>switch</c> found
        /// it: 102, 103, 104 or nothing.</summary>
        public int Follower { get; init; }

        /// <summary>Whether the page is held, and it always is.</summary>
        public bool IsBusy => Lines.Count > 0 || Follower != 0;

        /// <summary>One line, for an action and for a log.</summary>
        public override string ToString() =>
            $"{(SpeakerName.Length > 0 ? SpeakerName : "no name")} says"
            + $" {Lines.Count} line{(Lines.Count == 1 ? "" : "s")}"
            + $": \"{string.Join(" / ", Lines.Select(l => l.Text))}\""
            + $" (background {Background}, position {Position})"
            + (Follower == MzCommandTable.ShowChoiceList
                ? ", with a choice under it"
                : Follower == 103 ? ", with a number to enter"
                : Follower == 104 ? ", with an item to choose"
                : "");
    }

    /// <summary>
    /// Reads a 101 and everything it swallows, and says how many commands it
    /// ate.
    /// </summary>
    /// <param name="pCommands">
    /// The list, so the reader can look at what follows.
    /// </param>
    /// <param name="pIndex">
    /// Where the 101 is. <b>This is where the engine's index is when
    /// <c>command101</c> runs</b> — the engine has already stepped onto it.
    /// </param>
    /// <param name="pFacts">What the game knows, for the substitutions a line
    /// asks for.</param>
    /// <returns>The block, and how many commands came with it.</returns>
    public static (Block Block, int Consumed) Read(
        IReadOnlyList<MzCommandEntry> pCommands,
        int pIndex,
        MzBranchFacts pFacts)
    {
        var zeile = pCommands[pIndex];
        var lines = new List<MzMessage.Line>();
        var eaten = 1;
        var i = pIndex + 1;

        // **`while (this.nextEventCode() === 401) { this._index++;
        // add(…) }`** — a loop over the lines, and each one is stepped over
        // here, by this command, and never dispatched.
        //
        // **`nextEventCode()` is `this._list[this._index + 1]`, and `_index`
        // is on the 101.** So the first line is looked at while `i` is still
        // on the 101's own successor — `pIndex + 1` — and not one further
        // along.
        //
        // **A first draft wrote `NextCode(pCommands, i)`, and `NextCode`
        // looks one past the index it is given, so the read started at the
        // second line.** Every dialogue in this game came out one line short:
        // 524 lines instead of 938, and a distribution that matched the raw
        // data exactly one bucket out of step — which is what made it look
        // like an off-by-one in the test rather than in the reader.
        // **The symptom was right, the test was right, and the reader was
        // wrong; only one of those three was being checked.**
        while (i < pCommands.Count
            && pCommands[i].Code == MzCommandTable.ShowTextLine)
        {
            lines.Add(MzMessage.Read(
                pCommands[i].Parameters.Count > 0 ? pCommands[i].Parameters[0] : "",
                id => pFacts.HasVariable(id)
                    ? pFacts.Variable(id).ToString(CultureInfo.InvariantCulture)
                    : null,
                pFacts.Names));
            i++;
            eaten++;
        }

        // **One of three, and the switch runs once.** A 102 that is not
        // directly after the last line is not taken here and is reached later
        // as a command of its own. `i` is on the command after the last line
        // by now, so this reads it directly rather than through
        // `nextEventCode` — which would look one past it and answer a
        // question about the command after the 102.
        var follower = i < pCommands.Count ? pCommands[i].Code : 0;
        if (follower is MzCommandTable.ShowChoiceList or 103 or 104)
        {
            eaten++;
        }
        else
        {
            follower = 0;
        }

        return (
            new Block
            {
                FaceName = At(zeile, 0),
                FaceIndex = At(zeile, 1),
                Background = At(zeile, 2),
                Position = At(zeile, 3),
                SpeakerName = pCommands[pIndex].Parameters.Count > 4
                    ? pCommands[pIndex].Parameters[4]
                    : "",
                Lines = lines,
                Follower = follower,
            },
            eaten);
    }

    /// <summary>
    /// <c>nextEventCode</c>: <b>the command after</b> the index, and
    /// <b>0 when there is none</b> — which is why a dialogue at the end of a
    /// list ends rather than reaching past it.
    /// </summary>
    public static int NextCode(IReadOnlyList<MzCommandEntry> pCommands, int pIndex)
    {
        var next = pIndex + 1;
        return next < pCommands.Count ? pCommands[next].Code : 0;
    }

    private static int At(MzCommandEntry pCommand, int pIndex) =>
        pIndex < pCommand.Parameters.Count
        && int.TryParse(
            pCommand.Parameters[pIndex],
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var wert)
            ? wert
            : 0;
}
