using System.IO;
using UniversalRPG.Tests.Framework;
using UniversalRPG.Web;

namespace UniversalRPG.Tests.Core;

public partial class TestMzBalloonDuration : TestBase
{
    private const string Spiel = @"D:\Itch\sister\www\js\rpg_sprites.js";

    /// <summary>
    /// The balloon's own clock, and the number this reader guessed before.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And sixty was in the code as a constant whose name did
    /// not admit that it was a choice</strong>, and the help says plainly
    /// that <c>213</c> carries <c>no duration</c> -- so the length of the
    /// balloon lives entirely in <c>Sprite_Balloon</c>, and a reader that
    /// picks one out of the air shows the game a quarter of a second it
    /// did not ask for.
    /// </para>
    /// <para>
    /// <strong>And this test reads the game's own source</strong> and
    /// not this repository, and fails when the two disagree.
    /// </para>
    /// </remarks>
    public void Test_DieBallonDauerIstDieAusDerQuelleUndNichtGeraten()
    {
        if (!File.Exists(Spiel))
        {
            return;
        }

        var quelltext = File.ReadAllText(Spiel);

        // **`setup` ist der ganze Befehl:**
        //     this._duration = 8 * this.speed() + this.waitTime();
        AssertTrue(
            quelltext.Contains(
                "_duration = 8 * this.speed() + this.waitTime()"),
            "**and the sprite sets its own length that way** -- and the"
            + " source says: this._duration = 8 * this.speed() +"
            + " this.waitTime();");
        AssertTrue(quelltext.Contains("speed() {\n        return 8;\n    }"),
            "**and the speed is eight**");
        AssertTrue(
            quelltext.Contains("waitTime() {\n        return 12;\n    }"),
            "**and the wait after it is twelve**");
        AssertTrue(quelltext.Contains(
                "isPlaying() {\n        return this._duration > 0;\n    }"),
            "**and it is playing exactly while its clock is above zero**");

        // **Und daraus folgt die Zahl, und nicht umgekehrt.**
        AssertEq(
            MzScreen.MaxBalloonFrames, 8 * 8 + 12,
            "**and the repository's number follows from the source** -- and"
            + " it is " + MzScreen.MaxBalloonFrames + ", and 8 * 8 + 12 is"
            + " 76, and sixty was a guess");

        // **Und der Timer laeuft, und nicht nur die Zahl.**
        var spieler = new MzPlayer();
        spieler.ShowBalloon(2, MzScreen.MaxBalloonFrames);
        AssertEq(
            spieler.BalloonFramesLeft, MzScreen.MaxBalloonFrames,
            "**and the icon shows for exactly that many frames**");
        spieler.TickBalloon(MzScreen.MaxBalloonFrames - 1);
        AssertTrue(
            spieler.BalloonFramesLeft == 1,
            "**and it is still showing after all but one** -- and there"
            + $" are {spieler.BalloonFramesLeft}");
        spieler.TickBalloon(1);
        AssertEq(
            spieler.BalloonFramesLeft, 0,
            "**and it is gone on the frame the source counts down to"
            + " zero**");
    }
}
