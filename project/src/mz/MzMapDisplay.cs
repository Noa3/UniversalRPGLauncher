using System;
using System.Collections.Generic;

namespace UniversalRPG.Web;

/// <summary>
/// What <c>$gameMap</c> carries about how the map looks, and not about
/// what is on it.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this exists because four commands write here and there was
/// nowhere to write.</strong> Measured at <c>rpg_objects.js</c>:
/// </para>
/// <code>
/// command281: if (this._params[0] === 0) { $gameMap.enableNameDisplay(); }
///            else { $gameMap.disableNameDisplay(); }
/// command284: $gameMap.changeParallax(this._params[0], this._params[1],
///             this._params[2], this._params[3], this._params[4]);
/// command282: $gameMap.changeTileset(this._params[0]);
/// </code>
/// <para>
/// <strong>And <c>enableNameDisplay</c> und <c>disableNameDisplay</c> sind
/// je eine Zuweisung</strong> -- <strong>und beide starten wahr</strong>,
/// <strong>denn <c>Game_Map.prototype.initialize</c> sagt
/// <c>this._nameDisplay = true;</c></strong> -- <strong>und ein Spiel, das
/// nichts sagt, zeigt die Namen.</strong>
/// </para>
/// <para>
/// <strong>Und <c>changeParallax</c> hat eine Regel, die man nicht sieht:
/// wenn eine Schleife abgeschaltet wird, springt der Versatz auf
/// null.</strong>
/// </para>
/// <code>
/// changeParallax(name, loopX, loopY, sx, sy) {
///     this._parallaxName = name;
///     this._parallaxZero = ImageManager.isZeroParallax(this._parallaxName);
///     if (this._parallaxLoopX &amp;&amp; !loopX) {
///         this._parallaxX = 0;
///     }
///     if (this._parallaxLoopY &amp;&amp; !loopY) {
///         this._parallaxY = 0;
///     }
///     this._parallaxLoopX = loopX;
///     this._parallaxLoopY = loopY;
///     this._parallaxSx = sx;
///     this._parallaxSy = sy;
/// }
/// </code>
/// <para>
/// <strong>Und das heisst: ein Leser, der nur den Namen und die Schleifen
/// setzt, laesst einen Versatz stehen, den die Engine auf null
/// setzt.</strong>
/// </para>
/// </remarks>
public sealed class MzMapDisplay
{
    /// <summary>
    /// <c>enableNameDisplay</c> and <c>disableNameDisplay</c>, and the
    /// parameter picks which.
    /// </summary>
    /// <param name="pAnzeigen">Zero shows the names and anything else hides
    /// them.</param>
    /// <returns>One line, for an action and for a log.</returns>
    public string SetzeNamenAnzeige(int pAnzeigen)
    {
        NamenSichtbar = pAnzeigen == 0;
        return "the map's event names are "
            + (NamenSichtbar ? "shown" : "hidden");
    }

    /// <summary>
    /// Whether event names are on the screen, and the engine's own start
    /// value.
    /// </summary>
    /// <remarks>
    /// <strong>And this is <c>true</c> and not <c>false</c></strong> --
    /// <strong><c>Game_Map.prototype.initialize</c> sagt
    /// <c>this._nameDisplay = true;</c></strong>.
    /// </remarks>
    public bool NamenSichtbar { get; private set; } = true;

    /// <summary>The parallax picture's name, and empty for none.</summary>
    public string ParallaxName { get; private set; } = "";

    /// <summary>Whether the parallax scrolls sideways, and it does.</summary>
    public bool ParallaxLoopX { get; private set; } = true;

    /// <summary>And whether it scrolls up and down, and it does.</summary>
    public bool ParallaxLoopY { get; private set; } = true;

    /// <summary>
    /// Whether the parallax picture is the engine's own zero picture,
    /// and it checks with <c>ImageManager.isZeroParallax</c>.
    /// </summary>
    /// <remarks>
    /// <strong>And this repository cannot answer it</strong> --
    /// <strong>the check is <c>name === ""</c> against a list of
    /// built-in pictures</strong>, <strong>and those pictures live in
    /// <c>img/system/</c> under names the project's own
    /// <c>System.json</c> does not list.</strong>
    /// </remarks>
    public bool? ParallaxZero { get; private set; }

    /// <summary>Where the parallax has scrolled to, sideways.</summary>
    public int ParallaxX { get; private set; }

    /// <summary>And up and down.</summary>
    public int ParallaxY { get; private set; }

    /// <summary>How fast it scrolls, and the engine's two numbers.</summary>
    public int ParallaxSx { get; private set; } = 0;

    /// <summary>And the other one.</summary>
    public int ParallaxSy { get; private set; } = 0;

    /// <summary>
    /// The two battleback pictures, and <c>changeBattleback</c> is two
    /// assignments.
    /// </summary>
    /// <remarks>
    /// <strong>And the engine's own:</strong> <c>changeBattleback(
    /// battleback1Name, battleback2Name) { this._battleback1Name =
    /// battleback1Name; this._battleback2Name = battleback2Name; }</c> --
    /// <strong>and a battle background is not a tileset and not a
    /// parallax</strong>, <strong>and all three are named separately on
    /// the map.</strong>
    /// </remarks>
    public string Kampfgrund1 { get; private set; } = "";

    /// <summary>And the second one.</summary>
    public string Kampfgrund2 { get; private set; } = "";

    /// <summary>The tileset the map draws with, and zero for none.</summary>
    public int TilesetId { get; private set; }

    /// <summary>
    /// <c>changeParallax</c>, and the two resets are the whole of what is
    /// not assignment.
    /// </summary>
    /// <param name="pName">The picture's name.</param>
    /// <param name="pLoopX">Whether it scrolls sideways.</param>
    /// <param name="pLoopY">And up and down.</param>
    /// <param name="pSx">How fast, sideways.</param>
    /// <param name="pSy">And up and down.</param>
    /// <returns>One line, for an action and for a log.</returns>
    public string SetzeParallax(
        string pName, bool pLoopX, bool pLoopY, int pSx, int pSy)
    {
        var zurueckX = ParallaxLoopX && !pLoopX;
        var zurueckY = ParallaxLoopY && !pLoopY;
        ParallaxName = pName;
        ParallaxZero = null;
        if (zurueckX)
        {
            ParallaxX = 0;
        }

        if (zurueckY)
        {
            ParallaxY = 0;
        }

        ParallaxLoopX = pLoopX;
        ParallaxLoopY = pLoopY;
        ParallaxSx = pSx;
        ParallaxSy = pSy;
        return (pName.Length > 0 ? "the parallax is " + pName : "no parallax")
            + (pLoopX ? ", looping sideways" : "")
            + (pLoopY ? ", looping up and down" : "")
            + ", scrolling at " + pSx + "," + pSy
            + ((zurueckX || zurueckY)
                ? ", and the offset went back to zero because a loop was "
                    + "turned off"
                : "");
    }

    /// <summary>
    /// <c>changeBattleback</c>, and the two assignments are all of it.
    /// </summary>
    /// <param name="pErste">The first picture's name.</param>
    /// <param name="pZweite">And the second's.</param>
    /// <returns>One line, for an action and for a log.</returns>
    public string SetzeKampfgrund(string pErste, string pZweite)
    {
        Kampfgrund1 = pErste;
        Kampfgrund2 = pZweite;
        return "the battle background is " + (pErste.Length > 0 ? pErste
            : "nothing")
            + " behind " + (pZweite.Length > 0 ? pZweite : "nothing");
    }

    /// <summary>
    /// <c>changeTileset</c>, which is one assignment plus a refresh.
    /// </summary>
    /// <param name="pTileset">The tileset's id in <c>Tilesets.json</c>.</param>
    /// <returns>One line, for an action and for a log.</returns>
    public string SetzeTileset(int pTileset)
    {
        var gewechselt = TilesetId != pTileset;
        TilesetId = pTileset;
        return gewechselt
            ? "the map draws with tileset " + pTileset
            : "the map is told to draw with tileset " + pTileset
                + " again, and it already was";
    }
}
