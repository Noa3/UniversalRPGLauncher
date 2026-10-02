using System.IO;
using UniversalRPG.Tests.Framework;
using UniversalRPG.Web;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The screen's brightness, and the two commands that move it.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this repository had no field for the screen's brightness at
/// all</strong> -- <strong>and <c>221</c> and <c>222</c> are exactly the
/// two commands that move it</strong>, -- <strong>and one of the two
/// carried the code for <c>212 Show Animation</c> in a case named for
/// it.</strong>
/// </para>
/// </remarks>
public partial class TestMzScreenFade : TestBase
{
    private const string Quelle =
        "D:/RPGMakerGames/Fatal Fantasy Update/Fatal Fantasy/www/js/"
            + "rpg_objects.js";

    /// <summary>
    /// The engine's own numbers, read out of its own file.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And neither command carries a duration</strong>, and both
    /// ask <c>this.fadeSpeed()</c>, and <c>fadeSpeed</c> is
    /// <c>return 24;</c> -- <strong>and the wait and the picture have to
    /// use the same number</strong>, or the screen is black while the page
    /// already carries on.
    /// </para>
    /// </remarks>
    public void Test_DieBildschirmHelligkeitKommtAusDerQuelle()
    {
        if (!File.Exists(Quelle))
        {
            return;
        }

        var quelltext = File.ReadAllText(Quelle);
        AssertTrue(
            quelltext.Contains("$gameScreen.startFadeOut(this.fadeSpeed())"),
            "**and 221 asks the engine's own speed**");
        AssertTrue(
            quelltext.Contains("$gameScreen.startFadeIn(this.fadeSpeed())"),
            "**and 222 asks it too**");
        AssertTrue(
            quelltext.Contains("fadeSpeed() {\n        return 24;\n    }")
            || quelltext.Contains("fadeSpeed() {\n        return 24;\n    };")
            || quelltext.Contains("prototype.fadeSpeed = function() {\n"
                + "        return 24;"),
            "**and that speed is twenty-four** -- and it is what this"
            + $" repository calls {MzScreen.FadeSpeed}");

        // **Und die Arithmetik, und beide Formen.**
        AssertTrue(quelltext.Contains(
                "this._brightness = (this._brightness * (d - 1)) / d"),
            "**and a fade out divides** -- `updateFadeOut`");
        AssertTrue(quelltext.Contains(
                "this._brightness * (d - 1) + 255) / d"),
            "**and a fade in adds 255** -- `updateFadeIn`");
    }

    /// <summary>
    /// And what the reader does with them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the engine's own division, not a step of one per
    /// frame</strong>, -- <strong>and a fade that moved one per frame is
    /// not the engine's fade</strong>, -- <strong>and one that counted
    /// frames without touching the brightness showed a fade with no
    /// picture at all.</strong>
    /// </para>
    /// </remarks>
    public void Test_DerBildschirmDunkeltUndHelltAufUndEndetGenau()
    {
        var bildschirm = new MzScreen();
        AssertEq(bildschirm.Brightness, 255,
            "**and it starts bright** -- and the engine's `_brightness`"
            + $" starts at 255, and this reader says {bildschirm.Brightness}");

        bildschirm.StarteAbdunkeln(MzScreen.FadeSpeed);
        AssertEq(bildschirm.FadeOutDuration, MzScreen.FadeSpeed,
            "**and a fade out asks for the engine's number of frames**");

        // **Und jetzt alle 24 Bilder, und das Ergebnis ist 0.**
        for (var i = 0; i < MzScreen.FadeSpeed; i++)
        {
            bildschirm.TickBildschirm();
        }

        AssertEq(bildschirm.Brightness, 0,
            "**and it lands on black exactly** -- and it is at "
            + bildschirm.Brightness + ", and a reader that stepped one"
            + " per frame would be at 231");
        AssertEq(bildschirm.FadeOutDuration, 0,
            "**and the fade is over**");

        // **Und die andere Richtung.**
        bildschirm.StarteAufhellen(MzScreen.FadeSpeed);
        for (var i = 0; i < MzScreen.FadeSpeed; i++)
        {
            bildschirm.TickBildschirm();
        }

        AssertEq(bildschirm.Brightness, 255,
            "**and it comes back to bright exactly** -- and it is at "
            + bildschirm.Brightness);

        // **Und die beiden heben sich auf, und das ist gemessen.**
        bildschirm.StarteAbdunkeln(10);
        bildschirm.StarteAufhellen(10);
        AssertEq(bildschirm.FadeOutDuration, 0,
            "**and a fade in cancels a fade out** -- and `startFadeIn`"
            + " sets `_fadeOutDuration = 0`");
    }
}
