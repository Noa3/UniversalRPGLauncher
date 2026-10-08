using System;
using UniversalRPG.Plugins;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// And the opening a map plays before the player can do anything.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this exists because the trigger numbering was wrong and a real
/// opening never played.</strong> Measured: Camellia's start map <em>is</em>
/// Map002 and it carries an <strong>autorun</strong> page -- event 5, 22
/// commands, four messages, then <c>222 Fadeout</c> and <c>123 Self Switch
/// A</c>. With the numbers corrected that page runs, and it holds the player
/// while it speaks -- which is what the engine does and what a player sees.
/// </para>
/// <para>
/// <strong>So a test that walks onto a map and expects it quiet has to let the
/// opening finish first.</strong> This walks the frames, closes each message the
/// way a player would, and waits for the map to settle -- <strong>and it
/// returns how many messages it closed, so a test can say what it saw instead
/// of assuming.</strong>
/// </para>
/// <para>
/// <strong>And it is not a workaround.</strong> The opening is four messages
/// long and ends by itself after the last one; a runtime that skipped it would
/// be a runtime that does not show a game's first scene.
/// </para>
/// </remarks>
internal static class MzTestOpening
{
    /// <summary>
    /// And the map's opening is let through, message by message.
    /// </summary>
    /// <param name="pRuntime">The runtime, already started.</param>
    /// <param name="pMaxMessages">
    /// How many messages to close before giving up, so a bug cannot hang a
    /// test.
    /// </param>
    /// <returns>How many messages were closed.</returns>
    public static int Durchlassen(MzEngineRuntime pRuntime, int pMaxMessages = 40)
    {
        if (pRuntime == null)
        {
            return 0;
        }

        // **Und die Seite muss erst anlaufen**, denn Autorun startet im Takt
        // und nicht beim Aufbau der Karte.
        for (var frame = 0; frame < 60; frame++)
        {
            pRuntime.Update(1.0 / 60.0);
        }

        var gesehen = 0;
        while (pRuntime.MessageVisible && gesehen < pMaxMessages)
        {
            pRuntime.SubmitInput(
                UniversalRPG.Rm2k.Input.Rm2kInputAction.Confirm);
            pRuntime.Update(1.0 / 60.0);
            gesehen++;
        }

        // **Und danach laeuft die Seite zu Ende** -- ein Fadeout, ein
        // Self-Switch, ein `0`.
        for (var frame = 0; frame < 60; frame++)
        {
            pRuntime.Update(1.0 / 60.0);
        }

        Console.WriteLine($"MZ opening: let {gesehen} message(s) through, and the "
            + $"map is quiet: message={pRuntime.MessageVisible} "
            + $"busy={pRuntime.MessageHoldsPlayer}");
        return gesehen;
    }
}
