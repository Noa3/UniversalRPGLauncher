using System;
using System.Collections.Generic;
using System.Linq;
using UniversalRPG.Tests.Framework;
using UniversalRPG.Web;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The audio commands a finished MZ project carries, executed.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the measurement that asked for this found the commands
/// missing.</strong> On <c>CamelliaCoronation-Win</c>:
/// <c>241 Play BGM</c> four times, <c>250 Play SE</c> eighteen,
/// <c>221 Fadeout Screen</c> sixteen. <strong>None of them ran.</strong>
/// A game whose music never starts is a game that runs and is not the
/// game.
/// </para>
/// <para>
/// <strong>And the parameter is an object, and not four numbers.</strong>
/// Measured on that project: <c>241 [{"name":"Scene8","volume":40,
/// "pitch":80,"pan":0}]</c>. XP writes the same command as <c>11510</c>
/// with four bit fields, <strong>and a reader that read one of the two
/// forms into the other produced a channel named <c>{"name"</c> at
/// volume zero.</strong>
/// </para>
/// <para>
/// <strong>And the object comes back as text</strong>, because
/// <c>MzCommandEntry.From</c> writes a nested object with
/// <c>MzJson.Write</c> so nothing is thrown away, <strong>and it is read
/// again with <c>MzJson.TryParse</c> — the same parser that wrote
/// it.</strong>
/// </para>
/// <para>
/// <strong>And a channel is a state and not a sound.</strong> Nothing
/// here opens an <c>.ogg</c>: a command names a file and three numbers,
/// <strong>and a channel that knows what it was told and never carried
/// a player is a fact about the command and not a claim.</strong>
/// </para>
/// </remarks>
public partial class TestMzAudio : TestBase
{
    /// <summary>
    /// A play command names a file and three numbers, and the numbers are
    /// bounded.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the four numbers are read out of the object, and not
    /// out of four positions.</strong> The command's own first parameter
    /// is the object, and <c>At(pCommand, 0)</c> on a parameter that is
    /// an object returns zero, <strong>so a reader that read positions
    /// here produced volume zero and pitch zero for every track in the
    /// game.</strong>
    /// </para>
    /// </remarks>
    public void Test_PlayBgmNenntDieDateiUndDreiZahlen()
    {
        var bildschirm = new MzScreen();
        var fakten = new MzBranchFacts { Screen = bildschirm };
        var aktionen = new List<MzAction>();
        var befehl = new MzCommandEntry(241, ["{\"name\":\"Scene8\","
            + "\"volume\":40,\"pitch\":80,\"pan\":0}"], 0);

        AssertTrue(MzCommands.TryExecute(
                new MzInterpreter(new List<MzCommandEntry>()),
                befehl, aktionen, fakten, new MzRandom()),
            "**and the play command runs**");

        AssertEq(bildschirm.Bgm.Name, "Scene8",
            "**and the channel carries the file the object named** -- and "
                + "a reader that did not read the object back would have "
                + "the whole JSON text here");
        AssertEq(bildschirm.Bgm.Volume, 40,
            "**and the volume the object carried**");
        AssertEq(bildschirm.Bgm.Pitch, 80,
            "**and the pitch** -- and 80 is not 100, and a reader that "
                + "defaulted would pass a test that used 100");
        AssertEq(bildschirm.Bgm.Pan, 0,
            "**and the pan**");
        AssertTrue(!bildschirm.Bgm.IsSilent,
            "**and the channel is not silent**");
    }

    /// <summary>
    /// A number outside the editor's own range is bounded, and not
    /// refused.
    /// </summary>
    /// <remarks>
    /// <strong>And the bounds are the editor's, and not mine.</strong>
    /// Volume 0 to 100, pitch 0 to 200, pan -100 to 100 — **and a file
    /// that says 400 for the volume means "as loud as this engine can
    /// get", and not a corrupt file.**
    /// </remarks>
    public void Test_EineZahlAusserhalbDerSchrankeWirdBegrenzt()
    {
        var bildschirm = new MzScreen();
        var fakten = new MzBranchFacts { Screen = bildschirm };
        MzCommands.TryExecute(
            new MzInterpreter(new List<MzCommandEntry>()),
            new MzCommandEntry(241,
                ["{\"name\":\"Laut\",\"volume\":400,\"pitch\":900,"
                    + "\"pan\":-400}"], 0),
            new List<MzAction>(), fakten, new MzRandom());

        AssertEq(bildschirm.Bgm.Volume, 100,
            "**and the volume is 100** -- 400 asked, and the editor's own "
                + "maximum is 100");
        AssertEq(bildschirm.Bgm.Pitch, 200,
            "**and the pitch is 200** -- 900 asked");
        AssertEq(bildschirm.Bgm.Pan, -100,
            "**and the pan is -100** -- -400 asked, and the pan's reach is "
                + "a hundred on each side");
    }

    /// <summary>
    /// A fade runs over the frames it was given, and a fade of zero is a
    /// stop.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And a fade of zero stops, and does not hang.</strong> The
    /// editor leaves the field empty when a person did not touch it,
    /// **and a reader that treated zero as "not yet" left the music
    /// playing for ever** — which is the one state a player notices
    /// and cannot report.
    /// </para>
    /// <para>
    /// <strong>And the channel is silent only after the frames are
    /// gone.</strong> A fade of three is not a stop on the frame it
    /// starts.
    /// </para>
    /// </remarks>
    public void Test_EinAusblendenLaeuftUndNullIstEinStopp()
    {
        var bildschirm = new MzScreen();
        var fakten = new MzBranchFacts { Screen = bildschirm };
        var lauf = new MzInterpreter(new List<MzCommandEntry>());

        MzCommands.TryExecute(lauf,
            new MzCommandEntry(241,
                ["{\"name\":\"Theme1\",\"volume\":25,\"pitch\":100,"
                    + "\"pan\":0}"], 0),
            new List<MzAction>(), fakten, new MzRandom());
        MzCommands.TryExecute(lauf,
            new MzCommandEntry(242, ["30"], 0),
            new List<MzAction>(), fakten, new MzRandom());

        AssertEq(bildschirm.Bgm.FadeFramesLeft, 30,
            "**and the fade has thirty frames** -- and it is not a stop, "
                + "because thirty is not zero");

        bildschirm.PassFrame();
        AssertEq(bildschirm.Bgm.FadeFramesLeft, 29,
            "**and one frame took one off**");
        AssertTrue(!bildschirm.Bgm.IsSilent,
            "**and it is still playing after one frame**");

        for (var frame = 0; frame < 29; frame++)
        {
            bildschirm.PassFrame();
        }

        AssertTrue(bildschirm.Bgm.IsSilent,
            "**and it is silent on the thirtieth frame** -- and a fade "
                + "that ended one frame late left music playing that "
                + "nothing would ever stop");

        // **Und null ist ein Stopp, und nicht ein Fade, das wartet.**
        MzCommands.TryExecute(lauf,
            new MzCommandEntry(241,
                ["{\"name\":\"Theme1\",\"volume\":25,\"pitch\":100,"
                    + "\"pan\":0}"], 0),
            new List<MzAction>(), fakten, new MzRandom());
        MzCommands.TryExecute(lauf,
            new MzCommandEntry(242, ["0"], 0),
            new List<MzAction>(), fakten, new MzRandom());

        AssertTrue(bildschirm.Bgm.IsSilent,
            "**and a fade of zero stops at once** -- and a reader that "
                + "refused zero would have left the old track playing "
                + "under the new one");
    }

    /// <summary>
    /// A play during a fade replaces it, and a stop names no file.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And two plays in a row are two tracks, and not the first
    /// one twice.</strong> The reference's <c>playBgm</c> stops whatever
    /// was playing and starts the file it was given,
    /// <strong>and a second <c>241</c> in a row is the second track.</strong>
    /// </para>
    /// <para>
    /// <strong>And <c>251 Stop SE</c> carries no parameter at all</strong>
    /// — the reference's <c>stopSe()</c> takes none — <strong>and this
    /// reader gives it zero frames, which is a stop.</strong>
    /// </para>
    /// </remarks>
    public void Test_EinSpielWaehrendEinesFadesErsetztUndStoppNenntKeineDatei()
    {
        var bildschirm = new MzScreen();
        var fakten = new MzBranchFacts { Screen = bildschirm };
        var lauf = new MzInterpreter(new List<MzCommandEntry>());

        MzCommands.TryExecute(lauf,
            new MzCommandEntry(241,
                ["{\"name\":\"Erste\",\"volume\":40,\"pitch\":100,\"pan\":0}"],
                0),
            new List<MzAction>(), fakten, new MzRandom());
        MzCommands.TryExecute(lauf, new MzCommandEntry(242, ["30"], 0),
            new List<MzAction>(), fakten, new MzRandom());
        MzCommands.TryExecute(lauf,
            new MzCommandEntry(241,
                ["{\"name\":\"Zweite\",\"volume\":40,\"pitch\":100,\"pan\":0}"],
                0),
            new List<MzAction>(), fakten, new MzRandom());

        AssertEq(bildschirm.Bgm.Name, "Zweite",
            "**and the second track is the one that plays**");
        AssertEq(bildschirm.Bgm.FadeFramesLeft, 0,
            "**and the fade the first one asked for is gone** -- and a "
                + "fade that survived a new track would fade the new "
                + "track out instead");

        // **Und 251 traegt keinen Parameter.**
        MzCommands.TryExecute(lauf,
            new MzCommandEntry(250,
                ["{\"name\":\"Thunder4\",\"volume\":60,\"pitch\":120,"
                    + "\"pan\":0}"], 0),
            new List<MzAction>(), fakten, new MzRandom());
        AssertEq(bildschirm.Se.Name, "Thunder4",
            "**and the sound effect is on its own channel** -- and it does "
                + "not touch the music, because a game that plays a "
                + "thunderclap and loses its music has two bugs");

        MzCommands.TryExecute(lauf, new MzCommandEntry(251, [], 0),
            new List<MzAction>(), fakten, new MzRandom());
        AssertTrue(bildschirm.Se.IsSilent,
            "**and 251 stops it** -- and 251 carries no parameter, and a "
                + "reader that read parameters[0] from an empty list read "
                + "zero, and zero frames is a stop, and that is the same "
                + "answer for the wrong reason");
    }
}
