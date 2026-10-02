using System;
using System.Collections.Generic;

namespace UniversalRPG.Rgss;

/// <summary>
/// What a command costs to run, decided by reading its own Ruby.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the question this answers is not "what does command 121
/// do"</strong> -- <strong>that the body says</strong> -- <strong>but
/// "can this repository run it without a Ruby interpreter"</strong>, --
/// <strong>and the answer is decided by the body, not by the
/// name.</strong>
/// </para>
/// <para>
/// <strong>And this is a real split.</strong> A command that writes
/// <c>$game_switches[i] = (@parameters[2] == 0)</c> is a state write
/// this repository can perform, -- <strong>and a command that sets
/// <c>@message_waiting = true</c> needs a message window and a wait
/// mode</strong>, -- <strong>and pretending the second kind is as cheap
/// as the first is how a runtime ends up claiming to run a game it only
/// reads.</strong>
/// </para>
/// </remarks>
public enum RgssBefehlArt
{
    /// <summary>Nothing was found, and that is not a guess.</summary>
    Unbekannt = 0,

    /// <summary>Writes a variable or a switch, and nothing else.</summary>
    ZustandsSchreibend,

    /// <summary>Puts something on screen, and waiting follows from it.</summary>
    Bildschirm,

    /// <summary>Moves something, or moves the player.</summary>
    ZweiOrt,

    /// <summary>Plays a sound, a music or an animation.</summary>
    Ton,

    /// <summary>Branches, loops and jumps, and the shape of the walk.</summary>
    Ablauf,

    /// <summary>Gives or takes an item, gold or a skill.</summary>
    Inventar,

    /// <summary>Says something that needs a Ruby expression to evaluate.</summary>
    RubyAusdruck,

    /// <summary>Asks the player something, and waits for the answer.</summary>
    Eingabe,
}

/// <summary>
/// Decides what a command costs, from its own Ruby body.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the order of these tests is the whole design, and it
/// was not the first one written.</strong>
/// </para>
/// <para>
/// <strong>And three wrong orders were written first and each of them
/// produced a wrong answer on a real command:</strong>
/// </para>
/// <list type="number">
/// <item><description>
/// A text marker before the choice marker turned <c>command_102</c>,
/// which is <em>show choices</em>, into a text command.
/// </description></item>
/// <item><description>
/// The word <c>choice</c> as a pattern counts <strong>comments</strong>,
/// and <c>command_101</c> has three lines mentioning choices, so show
/// text was classified as a question.
/// </description></item>
/// <item><description>
/// <c>return false</c> as a jump marker is wrong, because it is the
/// interpreter's <em>waiting</em> answer and every message command
/// carries it, so show text was classified as a branch.
/// </description></item>
/// </list>
/// <para>
/// <strong>And the order below is the one a walk meets.</strong>
/// </para>
/// </remarks>
public static class RgssBefehlArten
{
    /// <summary>
    /// Decides one command's kind.
    /// </summary>
    /// <param name="pBefehl">The command, as the game's own script wrote it.</param>
    /// <returns>The kind, and <c>Unbekannt</c> for a command with no body.</returns>
    /// <remarks>
    /// <strong>And a body this repository cannot classify is
    /// <c>Unbekannt</c> and not a guess</strong>, -- <strong>because an
    /// invented kind would let a runtime report progress it has not
    /// made.</strong>
    /// </remarks>
    public static RgssBefehlArt Von(RgssQuellBefehl pBefehl)
    {
        if (pBefehl == null || pBefehl.Koerper.Length == 0)
        {
            return RgssBefehlArt.Unbekannt;
        }

        var k = pBefehl.Koerper;

        // **Und der erste Test ist nicht "fragt der Befehl den
        // Spieler", sondern "ist der Befehl selbst die Frage".**
        //
        // **Und das ist gemessen und es ist ein Unterschied.**
        //
        // **Und `command_102` ist die Frage**, -- **und sein Koerper
        // enthaelt `setup_choices(` als eigenen Aufruf** -- **und
        // `command_101` enthaelt denselben Aufruf ebenfalls**, --
        // **aber nur in einem Zweig, der `if
        // @list[@index+1].code == 102` vorausgeht.**
        //
        // **Und 101 stellt Text auf den Bildschirm und raeumt nur auf,
        // wenn der naechste Befehl eine Frage ist**, -- **und so ist es
        // im Skript des Spiels geschrieben**, -- **und kein Muster auf
        // das blosse Wort unterscheidet die beiden.**
        //
        // **Und der Unterschied ist `@parameters` gegen
        // `@list[@index].parameters`.**
        //
        // **Und beides steht so in den Koerpern des Spiels:**
        //
        // ```ruby
        // # command_101,  Zeile 27   setup_choices(@list[@index].parameters)
        // # command_102,  Zeile 3    setup_choices(@parameters)
        // ```
        //
        // **Und das ist die Regel des Interpreters, nicht eine
        // Besonderheit dieser Befehle**: -- **vor <c>commandNNN</c>
        // setzt er `@parameters` auf die Liste des Befehls selbst**,
        // -- **und `@list[@index].parameters` ist immer die eines
        // anderen Befehls.**
        //
        // **Und 101 ruft es fuer den Nachbarbefehl auf**, -- **und 102
        // fuer sich selbst**, -- **und ein Muster auf den blossen
        /// Wortlaut kann das nicht unterscheiden.**
        var eigeneFrage = false;
        foreach (var zeile in k.Split('\n'))
        {
            var geschnitten = zeile.Trim();
            if (geschnitten.StartsWith("#", StringComparison.Ordinal)
                || !geschnitten.Contains("setup_choices(",
                    StringComparison.Ordinal))
            {
                continue;
            }

            if (geschnitten.Contains("@list[", StringComparison.Ordinal))
            {
                continue;
            }

            eigeneFrage = true;
            break;
        }

        if (eigeneFrage
            || Enthaelt(k, "choice_cancel")
            || Enthaelt(k, "input_number")
            || Enthaelt(k, "input_string"))
        {
            return RgssBefehlArt.Eingabe;
        }

        // **Und dann:  legt der Befehl etwas auf den Bildschirm?**
        //
        // **Und XP legt Text ueber `$game_temp.message_text` auf den
        // Bildschirm und nicht ueber `$game_message.add`**, --
        // **und `@message_waiting = true` ist die WarteAntwort.**
        if (Enthaelt(k, "$game_message.add")
            || Enthaelt(k, "$game_temp.message_text")
            || Enthaelt(k, "@message_waiting")
            || Enthaelt(k, "message_proc"))
        {
            return RgssBefehlArt.Bildschirm;
        }

        // **Und dann:  bewegt der Befehl etwas?**
        if (Enthaelt(k, "move_to")
            || Enthaelt(k, "move_up")
            || Enthaelt(k, "move_down")
            || Enthaelt(k, "move_left")
            || Enthaelt(k, "move_right")
            || Enthaelt(k, "move_random")
            || Enthaelt(k, "turn_toward")
            || Enthaelt(k, "walk_to")
            || Enthaelt(k, "$game_player")
            || Enthaelt(k, "segue")
            || Enthaelt(k, "transfer"))
        {
            return RgssBefehlArt.ZweiOrt;
        }

        // **Und dann:  spielt der Befehl etwas?**
        if (Enthaelt(k, "play_bgm")
            || Enthaelt(k, "play_se")
            || Enthaelt(k, "play_bgs")
            || Enthaelt(k, "perform")
            || Enthaelt(k, "start_enemy")
            || Enthaelt(k, "start_actor")
            || Enthaelt(k, "start_combo")
            || Enthaelt(k, "battle"))
        {
            return RgssBefehlArt.Ton;
        }

        // **Und dann:  gibt der Befehl etwas?**
        if (Enthaelt(k, "$game_party")
            || Enthaelt(k, "$game_actors")
            || Enthaelt(k, "gain_item")
            || Enthaelt(k, "lose_item")
            || Enthaelt(k, "gain_gold")
            || Enthaelt(k, "lose_gold")
            || Enthaelt(k, "gain_skill")
            || Enthaelt(k, "learn_skill"))
        {
            return RgssBefehlArt.Inventar;
        }

        // **Und dann:  springt der Befehl?**
        //
        // **Und `return false` steht hier ausdruecklich NICHT**,
        // -- **denn es ist die Warte-Antwort des Interpreters**, --
        // **es heisst "noch nicht fertig"** -- **und nicht "springe
        // weg"**, -- **und jeder Textbefehl enthaelt es.**
        if (Enthaelt(k, "@index -= ")
            || Enthaelt(k, "jump(")
            || Enthaelt(k, "command_skip")
            || Enthaelt(k, "while ")
            || Enthaelt(k, "loop"))
        {
            return RgssBefehlArt.Ablauf;
        }

        // **Und dann:  braucht der Befehl einen Ruby-Ausdruck?**
        //
        // **Und das ist die Art, die ein sicherer Bewerter nicht
        // beantworten kann** -- **denn ein Skriptausdruck darf jede
        // Methode jedes Objekts aufrufen.**
        if (Enthaelt(k, "eval")
            || Enthaelt(k, "@parameters[0].is_a?")
            || Enthaelt(k, ".call"))
        {
            return RgssBefehlArt.RubyAusdruck;
        }

        // **Und zuletzt:  schreibt der Befehl nur Zustand?**
        if (Enthaelt(k, "$game_switches")
            || Enthaelt(k, "$game_variables")
            || Enthaelt(k, "$game_self_switches"))
        {
            return RgssBefehlArt.ZustandsSchreibend;
        }

        return RgssBefehlArt.Unbekannt;
    }

    /// <summary>
    /// Counts a project's commands by kind.
    /// </summary>
    /// <param name="pBefehle">The commands, as the game's own scripts wrote them.</param>
    /// <returns>How many of each kind.</returns>
    public static Dictionary<RgssBefehlArt, int> Verteilung(
        IReadOnlyList<RgssQuellBefehl> pBefehle)
    {
        var alle = new Dictionary<RgssBefehlArt, int>();
        foreach (var befehl in pBefehle)
        {
            var art = Von(befehl);
            alle.TryGetValue(art, out var anzahl);
            alle[art] = anzahl + 1;
        }

        return alle;
    }

    private static bool Enthaelt(string pText, string pNadel) =>
        pText.Contains(pNadel, StringComparison.Ordinal);
}
