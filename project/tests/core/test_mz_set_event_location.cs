using System;
using System.Collections.Generic;
using UniversalRPG.Tests.Framework;
using UniversalRPG.Web;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// <c>203 Set Event Location</c>, the last of the character commands a
/// finished MZ project uses that this reader could not run.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the help has no page under that name.</strong> The manual
/// calls it <em>Set Event Location</em> and says *Changes the location of
/// an event*, and the number in the file is 203. <strong>So the name in
/// this repository is the manual's word, and the constant is the file's
/// number</strong> — a reader that looked the name up and found nothing
/// would have skipped a command the game uses ten times.
/// </para>
/// <para>
/// <strong>And the measured form is
/// <c>[Event, Place, X, Y, Direction]</c></strong> — ten times in a
/// finished project, and <em>Place</em> is 0 in every one of them, which
/// the help calls <em>Direct Designation</em>.
/// </para>
/// </remarks>
public partial class TestMzSetEventLocation : TestBase
{
    /// <summary>
    /// A tile is a jump, and the drawn position goes with it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And a tile is a jump, and not a step.</strong> *Changes
    /// the location of an event* — <strong>there is no walk and no route
    /// here.</strong>
    /// </para>
    /// <para>
    /// <strong>And <c>RealX</c> and <c>RealY</c> go with
    /// <c>X</c> and <c>Y</c>.</strong> The figure is drawn between tiles
    /// while it moves, and a reader that set only the tile <strong>left
    /// the drawn position on the old tile</strong> — and the next
    /// movement command walked it back to where the event had been,
    /// which is the one thing a location change is for.
    /// </para>
    /// <para>
    /// <strong>And the direction is a fifth value, and it is applied
    /// even when nothing else changes.</strong> The help lists Event,
    /// Location and Direction; the engine's own <c>setLocation</c> takes
    /// all three.
    /// </para>
    /// </remarks>
    public void Test_EinKachelwechselIstSprungUndNichtSchritt()
    {
        var fakten = new MzBranchFacts();
        var figur = new MzCharacter();
        fakten.Characters[10] = figur;
        var lauf = new MzInterpreter(new List<MzCommandEntry> { });
        lauf.Setup(1, 10);
        var aktionen = new List<MzAction>();

        // **Und die gemessene Form: [10, 0, 5, 7, 2].**
        AssertTrue(MzCommands.TryExecute(lauf,
                new MzCommandEntry(203, ["10", "0", "5", "7", "2"], 0),
                aktionen, fakten, new MzRandom()),
            "**and the command runs**");

        AssertEq(figur.X, 5, "**and the figure is on column five**");
        AssertEq(figur.Y, 7, "**and on row seven**");
        AssertEq((int)figur.RealX, 5,
            "**and its drawn position is there too** -- and a reader that "
                + "set only X and Y left the drawing on the old tile, and "
                + "the next movement command walked it back to where the "
                + "event had been");
        AssertEq((int)figur.RealY, 7, "**and in both axes**");
        AssertEq(figur.Direction, MzCharacter.Down,
            "**and it faces down** -- and the direction is the fifth value, "
                + "not part of the place");
        AssertEq(fakten.Notices.Count, 0,
            "**and nothing is complained about**");
    }

    /// <summary>
    /// A place this reader cannot answer says so instead of guessing.
    /// </summary>
    /// <remarks>
    /// <strong>And the second value is not always 0.</strong> The help
    /// offers *Direct Designation* and also a place named by variables or
    /// a map event; <strong>measured, every one of the ten in the game in
    /// front of us is 0</strong>, **and a reader that assumed 0 always
    /// would have moved a figure to a tile the game never asked for**
    /// **the first time a hand-edited event used the other one.**
    /// </remarks>
    public void Test_EinOrtDenDieserLeserNichtBeantwortenKannWirdGesagt()
    {
        var fakten = new MzBranchFacts();
        var figur = new MzCharacter();
        fakten.Characters[10] = figur;
        var lauf = new MzInterpreter(new List<MzCommandEntry> { });
        lauf.Setup(1, 10);
        var aktionen = new List<MzAction>();

        MzCommands.TryExecute(lauf,
            new MzCommandEntry(203, ["10", "2", "5", "7", "2"], 0),
            aktionen, fakten, new MzRandom());

        AssertEq(figur.X, 0,
            "**and the figure stays where it was** -- and a reader that "
                + "moved it anyway to a tile the game never named put an "
                + "event somewhere its own page does not describe");
        AssertTrue(aktionen[0].What.Contains("cannot answer"),
            "**and the action says so** -- and a silent no-op reads in a "
                + "log like a command that ran and had nothing to do");
    }

    /// <summary>
    /// An event this map does not have is named, not invented.
    /// </summary>
    /// <remarks>
    /// <strong>And a map that has no such event gets a sentence, not a
    /// new figure.</strong> A reader that created one would have put an
    /// event on a map the game never put it on, and a later command that
    /// named it would have found it there.
    /// </remarks>
    public void Test_EinEreignisDasDieseKarteNichtHatWirdGenannt()
    {
        var fakten = new MzBranchFacts();
        var lauf = new MzInterpreter(new List<MzCommandEntry> { });
        lauf.Setup(1, 99);
        MzCommands.TryExecute(lauf,
            new MzCommandEntry(203, ["99", "0", "5", "7", "2"], 0),
            new List<MzAction>(), fakten, new MzRandom());

        AssertEq(fakten.Characters.Count, 0,
            "**and the map still has nobody** -- and a reader that created "
                + "the figure put an event somewhere the game never put "
                + "one");
        AssertEq(fakten.Notices.Count, 1,
            "**and the event is named**");
    }

    /// <summary>
    /// The direction is a value, and not the default in disguise.
    /// </summary>
    /// <remarks>
    /// <strong>And the first draft of the test above chose
    /// <see cref="MzCharacter.Down"/>, which is also the field's
    /// starting value.</strong> That made the assertion pass whether or
    /// not the command set the direction, <strong>and a living mutation
    /// rule that replaced the assignment with
    /// <c>Direction = Down</c> survived it.</strong> **A test that
    /// checks a value against its own default is not a check.**
    /// </remarks>
    public void Test_DieRichtungIstEinWertUndNichtDerAnfangswert()
    {
        var fakten = new MzBranchFacts();
        var figur = new MzCharacter();
        fakten.Characters[10] = figur;
        var lauf = new MzInterpreter(new List<MzCommandEntry> { });
        lauf.Setup(1, 10);

        AssertEq(figur.Direction, MzCharacter.Down,
            "**and the figure starts facing down** -- and that is the "
                + "field's own starting value, which is exactly why the "
                + "next assertion may not use it");

        // **Und die Hilfe nennt vier Richtungen: 2, 4, 6, 8.**
        MzCommands.TryExecute(lauf,
            new MzCommandEntry(203, ["10", "0", "5", "7", "8"], 0),
            new List<MzAction>(), fakten, new MzRandom());
        AssertEq(figur.Direction, MzCharacter.Up,
            "**and it faces up afterwards** -- and 8 is up in the engine's "
                + "own numbering, and a reader that kept the default passed "
                + "a test that asked for the default");
    }

    /// <summary>
    /// An event may not move another event.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the engine's own rule.</strong> The help for
    /// <em>Set Event Location</em> says: *By setting this to [This Event],
    /// the event itself will be the target of the location change. You
    /// may only move events to another location* — <strong>and the
    /// restriction is on the target, not on the caller.</strong>
    /// </para>
    /// <para>
    /// <strong>And a map event's command naming a different event id
    /// is a file this reader cannot answer</strong>, **because the
    /// engine's <c>setLocation</c> is called on
    /// <c>$gameMap.events(this.eventId())</c>** — **the event that is
    /// running**, **and never on the one the number names.**
    /// </para>
    /// </remarks>
    public void Test_EinEreignisDarfKeinAnderesVerschieben()
    {
        var fakten = new MzBranchFacts();
        var laufendes = new MzCharacter();
        var fremdes = new MzCharacter();
        fakten.Characters[10] = laufendes;
        fakten.Characters[11] = fremdes;
        var lauf = new MzInterpreter(new List<MzCommandEntry> { });
        lauf.Setup(1, 10);
        MzCommands.TryExecute(lauf,
            new MzCommandEntry(203, ["11", "0", "3", "4", "2"], 0),
            new List<MzAction>(), fakten, new MzRandom());

        AssertEq(fremdes.X, 0,
            "**and the other event stays where it was** -- and the engine "
                + "calls setLocation on the event that is running, and "
                + "never on the one the number names");
        AssertEq(laufendes.X, 0,
            "**and so does the running one** -- and a reader that fell "
                + "through to the running event when the number named "
                + "another moved the wrong figure to the right tile");
        AssertEq(fakten.Notices.Count, 1,
            "**and the number is named**");
    }
}
