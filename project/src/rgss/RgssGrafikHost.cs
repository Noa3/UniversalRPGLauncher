using System;
using System.Collections.Generic;

namespace UniversalRPG.Rgss;

/// <summary>
/// Answers the RGSS <c>Graphics</c> module for a running game.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is what a game reads before it draws anything.</strong>
/// Measured on a real XP game: <c>Game_Player#center</c> clamps the player's
/// position against <c>Graphics.width</c> and <c>Graphics.height</c>, and this
/// runtime had no <c>Graphics</c> at all -- the module was an empty stub, so
/// both were nil, and <c>moveto</c> reported *"undefined operator '+' for a
/// Nil and a Nil"*. **And that was the last step between that game and its
/// first frame.**
/// </para>
/// <para>
/// <strong>And the screen size is the engine's own.</strong> RGSS 1.0 opens a
/// 640x480 window, and <c>Game.ini</c> does not carry a size -- measured, the
/// game's <c>Game.ini</c> has <c>Library</c>, <c>Scripts</c>, <c>Title</c> and
/// the RTP names and nothing about pixels.
/// </para>
/// <para>
/// <strong>And this does not draw.</strong> <c>update</c> counts frames and
/// returns, because there is no surface yet; <c>freeze</c>, <c>transition</c>
/// and the fades are the same, and a runtime that reported a transition it did
/// not perform would be describing a picture nobody made. **And the counter is
/// real**, because <c>Graphics.frame_count</c> is what a game times itself
/// with, and a frame that was never counted is a game that never moved.
/// </para>
/// <para>
/// <strong>And every name here is measured out of the game's own
/// scripts</strong>, -- <strong>and a name that is not measured is not in
/// it.</strong>
/// </para>
/// </remarks>
public sealed class RgssGrafikHost : IRubyHost
{
    /// <summary>
    /// The host this one asks when the receiver is not <c>Graphics</c>.
    /// </summary>
    /// <remarks>
    /// <strong>And the chain is one chain.</strong> The data host answers
    /// <c>load_data</c>, this host answers <c>Graphics</c>, and the script host
    /// answers the game's own host methods -- and a game that called one of
    /// them and reached a host that answered nil would be a game reading a
    /// picture that is not there.
    /// </remarks>
    public IRubyHost? Fallback { get; set; }

    /// <summary>The width in pixels, 640 for RGSS 1.0.</summary>
    public int Breite { get; private set; } = 640;

    /// <summary>The height in pixels, 480 for RGSS 1.0.</summary>
    public int Hoehe { get; private set; } = 480;

    /// <summary>The frames this runtime has counted.</summary>
    public int Frames { get; private set; }

    /// <summary>The frame rate a game set, 40 for RGSS 1.0.</summary>
    public int Rate { get; private set; } = 40;

    /// <summary>The brightness a game set, 255 while nothing dims it.</summary>
    public int Helligkeit { get; private set; } = 255;

    /// <summary>The names this host answers.</summary>
    /// <remarks>
    /// <strong>And the names are the module's own</strong>, so that
    /// <c>Graphics.respond_to?(:width)</c> and the call agree.
    /// </remarks>
    public IReadOnlyList<string> KnownMethods
    {
        get
        {
            var namen = new List<string>
            {
                "width", "height", "frame_count", "frame_rate", "frame_rate=",
                "update", "resize_screen", "freeze", "transition",
                "transition=", "brightness", "brightness=", "fadein",
                "fadeout", "wait",
            };
            if (Fallback != null)
            {
                namen.AddRange(Fallback.KnownMethods);
            }

            return namen;
        }
    }

    /// <inheritdoc/>
    public RubyValue? CallMethod(
        RubyValue pReceiver, string pMethod, IReadOnlyList<RubyValue> pArguments)
    {
        // **Und nur `Graphics` gehoert diesem Host.** Ein anderer Empfaenger
        // geht weiter die Kette hinunter, -- **und das ist der Unterschied
        // zwischen einem Namen und einem Namen auf einem Empfaenger:**
        // `Graphics.width` ist eine Aussage ueber die Bildflaeche, und
        // `held.width` ist eine ueber den Helden.
        if (pReceiver.Kind != RubyValueKind.Symbol || pReceiver.Name != "Graphics")
        {
            return Fallback?.CallMethod(pReceiver, pMethod, pArguments);
        }

        switch (pMethod)
        {
            case "width":
                return RubyValue.OfInteger(Breite);
            case "height":
                return RubyValue.OfInteger(Hoehe);
            case "frame_count":
                return RubyValue.OfInteger(Frames);
            case "frame_rate":
                return RubyValue.OfInteger(Rate);
            case "frame_rate=":
                Rate = Zahl(pArguments, 0, Rate);
                return RubyValue.Nil;
            case "brightness":
                return RubyValue.OfInteger(Helligkeit);
            case "brightness=":
                Helligkeit = Zahl(pArguments, 0, Helligkeit);
                return RubyValue.Nil;
            case "update":
                Frames++;
                return RubyValue.Nil;
            case "resize_screen":
                Breite = Zahl(pArguments, 0, Breite);
                Hoehe = Zahl(pArguments, 1, Hoehe);
                return RubyValue.Nil;
            case "wait":
                // **Und `wait` ist eine Anzahl von Frames, und keine Zeit.**
                // Ein Spiel schreibt `Graphics.wait(20)` und meint zwanzig
                // Frames; **sie zu zaehlen ist das Einzige, was hier ehrlich
                // ist**, weil es noch keine Flaeche gibt, auf die man warten
                // koennte.
                Frames += Zahl(pArguments, 0, 0);
                return RubyValue.Nil;
            case "freeze":
            case "transition":
            case "transition=":
            case "fadein":
            case "fadeout":
                // **Und sie sind leer, und das ist eine Grenze und kein
                // Fehler.** Es gibt keine Flaeche, also gibt es nichts
                // einzufrieren und nichts zu ueberblenden.
                return RubyValue.Nil;
            default:
                return Fallback?.CallMethod(pReceiver, pMethod, pArguments);
        }
    }

    /// <inheritdoc/>
    public RubyValue? CallMethodWithBlock(
        RubyValue pReceiver,
        string pMethod,
        IReadOnlyList<RubyValue> pArguments,
        Func<IReadOnlyList<RubyValue>, RubyValue> pYield) =>
        Fallback?.CallMethodWithBlock(pReceiver, pMethod, pArguments, pYield);

    /// <inheritdoc/>
    /// <remarks>
    /// <strong>And a constant this host does not define is the fallback's.</strong>
    /// `Graphics` has no constants of its own in RGSS 1.0, and a game that asks
    /// for one is asking about something else.
    /// </remarks>
    public RubyValue? LookupConstant(string pName) => Fallback?.LookupConstant(pName);

    private static int Zahl(
        IReadOnlyList<RubyValue> pArgumente, int pStelle, int pVorgabe)
    {
        if (pStelle >= pArgumente.Count)
        {
            return pVorgabe;
        }

        var wert = pArgumente[pStelle];
        return wert.Kind == RubyValueKind.Integer ? (int)wert.Integer : pVorgabe;
    }
}
