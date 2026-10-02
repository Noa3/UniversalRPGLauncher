using System;

namespace UniversalRPG.Web;

/// <summary>
/// The weather <c>$gameScreen</c> shows, and the engine's four fields.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the four fields are not the same as the four parameters
/// the command carries.</strong> Measured at <c>rpg_objects.js</c>:
/// </para>
/// <code>
/// changeWeather(type, power, duration) {
///     if (type !== 'none' || duration === 0) {
///         this._weatherType = type;
///     }
///     this._weatherPowerTarget = type === 'none' ? 0 : power;
///     this._weatherDuration = duration;
///     if (duration === 0) {
///         this._weatherPower = this._weatherPowerTarget;
///     }
/// }
/// </code>
/// <para>
/// <strong>And a fourth parameter decides whether the page waits</strong>
/// -- <strong><c>command236</c> says <c>if (this._params[3]) {
/// this.wait(this._params[2]); }</c></strong> -- <strong>and it is not
/// part of <c>changeWeather</c>.</strong>
/// </para>
/// <para>
/// <strong>And the engine's own names are <c>none</c>, <c>rain</c>,
/// <c>snow</c> and <c>storm</c></strong> -- <strong>and <c>none</c> is a
/// word and not the empty string</strong>, <strong>and <c>power</c> runs
/// from one to nine.</strong>
/// </para>
/// </remarks>
public sealed class MzWeather
{
    /// <summary>The engine's four weather names.</summary>
    public const string None = "none";
    public const string Rain = "rain";
    public const string Snow = "snow";
    public const string Storm = "storm";

    /// <summary>
    /// The weather that is showing, and <c>none</c> is not no weather.
    /// </summary>
    /// <remarks>
    /// <strong>And this starts as <c>none</c></strong> -- <strong>and
    /// that is what <c>Game_Screen.prototype.initialize</c> says.</strong>
    /// </remarks>
    public string Typ { get; private set; } = None;

    /// <summary>How strong it is right now, and one to nine.</summary>
    public int Kraft { get; private set; }

    /// <summary>How strong it is going to be, and the two differ.</summary>
    public int KraftZiel { get; private set; }

    /// <summary>How many frames that change takes.</summary>
    public int Dauer { get; private set; }

    /// <summary>
    /// <c>changeWeather</c>, and the one condition in it is the whole
    /// command.
    /// </summary>
    /// <param name="pTyp">One of the engine's four names.</param>
    /// <param name="pKraft">How strong, and one to nine.</param>
    /// <param name="pDauer">How many frames, and zero means at once.</param>
    /// <returns>One line, for an action and for a log.</returns>
    public string Setze(string pTyp, int pKraft, int pDauer)
    {
        // **Und `type !== 'none' || duration === 0`** -- **und ohne diese
        // Bedingung loescht ein `none` bei laufender Dauer das Wetter
        // sofort, und die Dauer waere wirkungslos.**
        var typBleibt = pTyp == None && pDauer != 0;
        if (!typBleibt)
        {
            Typ = pTyp;
        }

        KraftZiel = pTyp == None ? 0 : pKraft;
        Dauer = pDauer;
        if (pDauer == 0)
        {
            Kraft = KraftZiel;
        }

        return "the weather is " + pTyp + " at " + pKraft + " over "
            + pDauer + " frames"
            + (typBleibt
                ? ", and the type stands as " + Typ + " until the duration "
                    + "has run, because `changeWeather` only sets it when "
                    + "`type !== 'none' || duration === 0`"
                : "");
    }

    /// <summary>
    /// One frame of <c>updateWeather</c>, and whether it has arrived.
    /// </summary>
    /// <remarks>
    /// <strong>And this is the engine's own rule: the power moves one step
    /// per frame towards the target</strong> -- <strong>and a reader that
    /// does not move it would report a weather that never arrives.</strong>
    /// </remarks>
    public bool EinBild()
    {
        if (Dauer <= 0)
        {
            return false;
        }

        Dauer--;
        if (KraftZiel > Kraft)
        {
            Kraft++;
        }
        else if (KraftZiel < Kraft)
        {
            Kraft--;
        }

        return Dauer > 0;
    }

    /// <summary>One line, for a log.</summary>
    public override string ToString() =>
        Typ + " at " + Kraft + " toward " + KraftZiel + " over " + Dauer;
}
