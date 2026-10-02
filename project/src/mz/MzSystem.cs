using System;
using System.Collections.Generic;

namespace UniversalRPG.Web;

/// <summary>
/// The three switches and the one tone <c>$gameSystem</c> carries, and the
/// one timer <c>$gameTimer</c> carries.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this exists because four commands write here and there was
/// nowhere to write.</strong> Measured at <c>rpg_objects.js</c>:
/// </para>
/// <code>
/// command134: if (this._params[0] === 0) { $gameSystem.disableSave(); }
///            else { $gameSystem.enableSave(); }
/// command135: if (this._params[0] === 0) { $gameSystem.disableMenu(); }
///            else { $gameSystem.enableMenu(); }
/// command138: $gameSystem.setWindowTone(this._params[0]);
/// command124: if (this._params[0] === 0) { $gameTimer.start(this._params[1] * 60); }
///            else { $gameTimer.stop(); }
/// </code>
/// <para>
/// <strong>And the store is <c>_saveEnabled</c>, <c>_menuEnabled</c> and
/// <c>_windowTone</c></strong>, <strong>and each setter is one
/// assignment</strong>:
/// </para>
/// <code>
/// Game_System.prototype.disableSave = function() { this._saveEnabled = false; };
/// Game_System.prototype.enableSave = function() { this._saveEnabled = true; };
/// Game_System.prototype.disableMenu = function() { this._menuEnabled = false; };
/// Game_System.prototype.enableMenu = function() { this._menuEnabled = true; };
/// Game_System.prototype.setWindowTone = function(value) { this._windowTone = value; };
/// </code>
/// <para>
/// <strong>And all three start true or empty</strong> -- <strong>and the
/// engine's own initialiser is</strong> <c>this._saveEnabled = true; this.
/// _menuEnabled = true; this._windowTone = (0, 0, 0, 0);</c>, <strong>so
/// a game that never says anything about saving can still save.</strong>
/// </para>
/// <para>
/// <strong>And <c>124</c>'s second parameter is seconds and the engine
/// multiplies it.</strong> <c>$gameTimer.start(this._params[1] *
/// 60)</c> -- <strong>and <c>Game_Timer</c> counts frames</strong>,
/// <strong>and <c>seconds()</c> is <c>Math.floor(this._frames / 60)</c>.
/// </strong> <strong>So <c>[0, 45]</c> is two thousand seven hundred
/// frames and forty-five seconds.</strong>
/// </para>
/// </remarks>
public sealed class MzSystem
{
    /// <summary>
    /// Whether the player may save. The engine starts this true, so a game
    /// that never says anything about saving can save.
    /// </summary>
    public bool SaveEnabled { get; private set; } = true;

    /// <summary>The same for the menu, and the engine's own start value.</summary>
    public bool MenuEnabled { get; private set; } = true;

    /// <summary>
    /// The tint over the windows, four numbers, and the engine's start
    /// value.
    /// </summary>
    public IReadOnlyList<int> WindowTone { get; private set; } =
        new[] { 0, 0, 0, 0 };

    /// <summary>
    /// The music a battle will use, and whether it has been set.
    /// </summary>
    /// <remarks>
    /// <strong>And <c>132</c> only stores it.</strong> <strong><c>241</c>
    /// is the one that plays</strong>, <strong>and the engine's two lines
    /// are <c>$gameSystem.setBattleBgm(this._params[0])</c> and
    /// <c>AudioManager.playBgm(this._params[0])</c>.</strong>
    /// <strong>And the difference is one word: <c>set</c> against
    /// <c>play</c>.</strong>
    /// </para>
    /// <para>
    /// <strong>And measured at <c>Fatal Fantasy</c>: 148 of them, and 62
    /// carry <c>(Regular Battle)</c>, 39 <c>(Boss Battle)</c> and 12
    /// <c>(Bad Situation)</c></strong> -- <strong>and all three are
    /// placeholders in parentheses, and the battle substitutes them
    /// itself.</strong>
    /// </remarks>
    public string Kampflied { get; set; } = "";

    /// <summary>
    /// The <c>name</c> inside <c>_battleBgm</c>, and what
    /// <c>AudioManager</c> would play.
    /// </summary>
    /// <remarks>
    /// <strong>And it is read out of the object and not kept
    /// separately</strong>, <strong>because <c>setBattleBgm</c> stores
    /// the whole object and <c>playBgm</c> reads its
    /// <c>name</c>.</strong>
    /// </remarks>
    public string KampfliedName
    {
        get
        {
            // **Und der Name wird aus dem Objekt gelesen und nicht
            // herausgeschnitten** -- **denn `Kampflied` ist der
            // geschriebene JSON-Text eines Parameters, und ein Schnitt
            // an zwei Anfuehrungszeichen bricht an jeder Form, in der
            // das Feld nicht zuerst steht.**
            MzJson.TryParse(Kampflied, out var wert, out var fehler);
            if (fehler.Length == 0 && wert.Kind == MzKind.Object)
            {
                return wert.Member("name")?.StringOr("") ?? "";
            }

            // **Und was kein Objekt ist, wird als Text genommen** --
            // **denn `MzCommandEntry` haelt eine Liste als ihr
            // geschriebenes JSON**, **und es gibt Spiele, die einen
            // blossen Dateinamen schreiben.**
            return Kampflied;
        }
    }

    /// <summary>
    /// Whether <c>132</c> has set it, and <c>saveBgm</c> is what copies
    /// it for the next battle.
    /// </summary>
    public bool HatKampflied { get; set; }

    /// <summary>Whether the timer runs, and the engine's start value.</summary>
    public bool TimerWorking { get; private set; }

    /// <summary>
    /// The timer's frames, and the engine counts frames and not seconds.
    /// </summary>
    public int TimerFrames { get; private set; }

    /// <summary>The four frames a second, as the engine multiplies by it.</summary>
    public const int FramesPerSecond = 60;

    /// <summary>
    /// <c>disableSave</c> and <c>enableSave</c>, and which of the two the
    /// command's parameter picks.
    /// </summary>
    /// <param name="pErlaubt">Zero disables and anything else enables.</param>
    /// <returns>One line, for an action and for a log.</returns>
    public string SetzeSpeichern(int pErlaubt)
    {
        SaveEnabled = pErlaubt != 0;
        return "saving is " + (SaveEnabled ? "allowed" : "not allowed");
    }

    /// <summary>
    /// <c>disableMenu</c> and <c>enableMenu</c>, and the same reading of
    /// the parameter.
    /// </summary>
    /// <param name="pErlaubt">Zero disables and anything else enables.</param>
    /// <returns>One line, for an action and for a log.</returns>
    public string SetzeMenue(int pErlaubt)
    {
        MenuEnabled = pErlaubt != 0;
        return "the menu is " + (MenuEnabled ? "open" : "shut");
    }

    /// <summary>
    /// <c>setWindowTone</c>, which takes the four numbers as they are.
    /// </summary>
    /// <param name="pTon">The four numbers the command carries.</param>
    /// <returns>One line, for an action and for a log.</returns>
    public string SetzeFensterTon(IReadOnlyList<int> pTon)
    {
        WindowTone = new List<int>
        {
            pTon.Count > 0 ? pTon[0] : 0,
            pTon.Count > 1 ? pTon[1] : 0,
            pTon.Count > 2 ? pTon[2] : 0,
            pTon.Count > 3 ? pTon[3] : 0,
        };
        return "the windows are tinted " + string.Join(",", WindowTone);
    }

    /// <summary>
    /// <c>$gameTimer.start(this._params[1] * 60)</c> and
    /// <c>$gameTimer.stop()</c>, and the parameter picks which.
    /// </summary>
    /// <param name="pArt">Zero starts and anything else stops.</param>
    /// <param name="pSekunden">The seconds the command carries.</param>
    /// <returns>One line, for an action and for a log.</returns>
    public string SetzeUhr(int pArt, int pSekunden)
    {
        if (pArt == 0)
        {
            TimerFrames = pSekunden * FramesPerSecond;
            TimerWorking = true;
            return "the timer runs for " + pSekunden + " seconds, which is "
                + TimerFrames + " frames";
        }

        TimerWorking = false;
        return "the timer is stopped";
    }

    /// <summary>
    /// <c>Game_Timer.update</c>, one frame, and whether it expired.
    /// </summary>
    /// <param name="pSzeneAktiv">
    /// The engine's own argument: <c>update(sceneActive)</c>, and a timer
    /// only counts while the scene is the active one.
    /// </param>
    /// <returns>
    /// Whether it ran out, because <c>onExpire</c> is
    /// <c>BattleManager.abort()</c>.
    /// </returns>
    public bool UhrEinBild(bool pSzeneAktiv)
    {
        if (!pSzeneAktiv || !TimerWorking || TimerFrames <= 0)
        {
            return false;
        }

        TimerFrames--;
        if (TimerFrames != 0)
        {
            return false;
        }

        TimerWorking = false;
        return true;
    }

    /// <summary>
    /// <c>Game_Timer.seconds()</c>, and the engine's own floor.
    /// </summary>
    public int UhrSekunden() => TimerFrames / FramesPerSecond;
}
