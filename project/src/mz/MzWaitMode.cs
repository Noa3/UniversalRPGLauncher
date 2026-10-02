namespace UniversalRPG.Web;

/// <summary>
/// Why a run is being held up, when the answer is not a number of frames.
/// </summary>
/// <remarks>
/// <para>
/// The engine's <c>Game_Interpreter</c> has a <c>_waitCount</c> and a
/// <c>_waitMode</c>, and they are two different waits. A 230 sets
/// <c>_waitCount</c>, and <c>updateWaitCount</c> counts it down. A 201 sets
/// <c>_waitMode</c> to <c>"transfer"</c>, and <c>updateWaitMode</c> asks
/// <c>$gamePlayer.isTransferring()</c> every frame until the map has changed.
/// </para>
/// <para>
/// <b>K-125 modelled the count and K-127 the movement, and neither of those
/// is this.</b> A condition wait has no length: it is over when the thing it
/// is waiting for is done, and a caller passing frames cannot end it. A reader
/// that treated a transfer as a wait of some length would either hold the page
/// for ever or cut it short, and neither is what the engine does.
///
/// </para>
/// <para>
/// The engine's own modes are <c>message</c>, <c>transfer</c>, <c>scroll</c>,
/// <c>route</c> and <c>until</c>. **Two are modelled here**, because only a transfer is something this reader can be told about;
/// the others need a scrolling map, a moving character and a plugin callback
/// that this repository does not run.
/// </para>
/// </remarks>
public enum MzWaitMode
{
    /// <summary>Not held up by a condition. A 230's frames are held here.</summary>
    None = 0,

    /// <summary>
    /// Held until the reserved transfer has been carried out — until the player
    /// is no longer on their way somewhere.
    /// </summary>
    /// <remarks>
    /// **This is the one condition a reader can actually be told about**, and
    /// it is the reason this enum exists rather than a flag on the interpreter.
    /// </remarks>
    Transfer = 1,

    /// <summary>
    /// Held until a forced move route has been walked to its end — the page
    /// waits for the character, not for a number of frames.
    /// </summary>
    /// <remarks>
    /// **`command205` sets this only when the route's own `wait` flag is
    /// set**: <c>if (params[1].wait) this.setWaitMode("route");</c>. **The
    /// wait is the route's, not the command's** — sixty of this game's
    /// ninety-six routes say so, and the other thirty-six do not, and a reader
    /// that held every page would stall a game on the thirty-six.
    /// </remarks>
    Route = 2,

    /// <summary>
    /// Held while an icon over a figure is still showing — the engine's
    /// <c>"balloon"</c>, set by a 213 whose third parameter is true.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this mode was missing, and that is why a page of 211
    /// commands stopped at eight.</strong> Measured at
    /// <c>updateWaitMode</c>: <c>case "balloon": character =
    /// this.character(this._characterId); waiting = character &amp;&amp;
    /// character.isBalloonPlaying();</c>
    /// </para>
    /// <para>
    /// <strong>And a reader that counted sixty frames instead waited for
    /// a length of time the game never said</strong> — <strong>and the
    /// engine waits for the icon to be gone, which is a condition and not
    /// a clock.</strong> The official help says it plainly: <em>the event
    /// will be paused until the balloon icon being displayed has
    /// disappeared.</em>
    /// </para>
    /// </remarks>
    Balloon = 4,

    /// <summary>
    /// Held while a line, a choice, a number to enter or an item to choose is
    /// on the screen — the engine's <c>"message"</c>.
    /// </summary>
    /// <remarks>
    /// **`updateWaitMode` asks <c>$gameMessage.isBusy()</c>**, and
    /// <c>isBusy</c> is <c>hasText() || isChoice() || isNumberInput() ||
    /// isItemChoice()</c> — **four things, and the one that matters most is
    /// the choice**, because a choice is up while the player is still deciding
    /// and a reader that waited only for text would let the page run on
    /// before the player had answered.
    ///
    /// <b>And it is the same condition a 101 refuses on</b>: a second
    /// dialogue while one is up returns false, so the two are one fact asked
    /// twice.
    /// </remarks>
    Message = 3,

    /// <summary>
    /// Held while a film plays — the engine's <c>"video"</c>, set by a
    /// <c>261</c> whose first parameter names a film.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is a condition and not a clock.</strong>
    /// <c>command261</c> is
    /// <c>Graphics.playVideo('movies/' + name + ext);
    /// this.setWaitMode('video');</c>
    /// <strong>and the engine's <c>updateWaitMode</c> asks whether the
    /// video is still playing, every frame, until it is not.</strong>
    /// </para>
    /// <para>
    /// <strong>And a reader that waited a fixed number of frames would
    /// either cut a long film short or hold a short one</strong>, **and no
    /// number of frames appears anywhere in the command** -- **the only
    /// thing it carries is the name.**
    /// </para>
    /// </remarks>
    Video = 5,
    /// <summary>
    /// Held while an animation plays over a character -- the engine's
    /// <c>"animation"</c>, set by a <c>212</c> or a <c>221</c> whose third
    /// parameter says the page waits.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is a condition and not a clock, for the same reason
    /// <see cref="Video"/> is.</strong> The engine sets it with
    /// <c>this.setWaitMode('animation')</c> and its <c>updateWaitMode</c>
    /// asks whether the animation is still going, every frame, until it is
    /// not.
    /// </para>
    /// <para>
    /// <strong>And no number of frames appears in the command</strong> --
    /// <strong>the length is in the project's own <c>Animations.json</c></strong>
    /// -- <strong>so a reader that waited a fixed count would cut a long
    /// animation short and hold a short one.</strong>
    /// </para>
    /// </remarks>
    Animation = 6,

    /// <summary>
    /// Held while the followers walk up to the player -- the engine's
    /// <c>"gather"</c>, set by a <c>217</c> outside a battle.
    /// </summary>
    /// <remarks>
    /// <strong>And this is a walk and not a teleport.</strong> <strong>The
    /// engine moves each follower over its own frames and then
    /// stops</strong>, <strong>and a reader that placed them at once would
    /// be a different game.</strong>
    /// </remarks>
    Gather = 7,

    /// <summary>
    /// Held until the map has finished scrolling.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is the engine's own state and not a number of
    /// frames.</strong>
    /// </para>
    /// <para>
    /// Measured at <c>command204</c>:
    /// </para>
    /// <code>
    /// command204() {
    ///     if (!$gameParty.inBattle()) {
    ///         if ($gameMap.isScrolling()) {
    ///             this.setWaitMode('scroll');
    ///             return false;
    ///         }
    ///         $gameMap.startScroll(this._params[0], this._params[1],
    ///             this._params[2]);
    ///     }
    ///     return true;
    /// }
    /// </code>
    /// <para>
    /// <strong>And <c>updateWaitMode</c> sagt <c>case "scroll":
    /// waiting = $gameMap.isScrolling();</c></strong>, <strong>und
    /// <c>isScrolling</c> ist <c>return this._scrollRest &gt; 0;</c>.</strong>
    /// </para>
    /// <para>
    /// <strong>Und <c>return false</c> heisst, dass der Befehl im
    /// naechsten Bild noch einmal gelesen wird</strong> -- <strong>und
    /// das ist der Unterschied zu einem <c>230</c>, das eine Zahl von
    /// Bildern wartet.</strong>
    /// </para>
    /// </remarks>
    Scroll = 8,
}
