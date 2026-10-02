using System;
using System.Collections.Generic;

namespace UniversalRPG.Rgss;

/// <summary>
/// The state an XP event page writes to, as the game's own scripts
/// name it.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And every field name here was read out of MicroQuest's
/// <c>Interpreter 3</c> to <c>Interpreter 7</c>.</strong> <c>command_101</c>
/// writes <c>$game_temp.message_text</c>, <c>$game_temp.choice_start</c>
/// and sets <c>@message_waiting</c>; <c>command_121</c> writes
/// <c>$game_switches[i]</c>; -- <strong>and a field this repository
/// invented would be a field no game writes and no page
/// reads.</strong>
/// </para>
/// <para>
/// <strong>And the host answers methods, not values.</strong>
/// <c>IRubyHost.CallMethod</c> is the interpreter's last resort before it
/// gives up, -- <strong>and it receives the receiver, the method name and
/// the arguments</strong>, -- <strong>so a field assignment
/// <c>message_text=</c> arrives here as a method named
/// <c>message_text=</c> and not as a property.</strong>
/// </para>
/// </remarks>
public sealed class RgssWelt
{
    private readonly Dictionary<string, object?> _felder =
        new Dictionary<string, object?>(StringComparer.Ordinal);

    /// <summary>
    /// How many field writes this world took.
    /// </summary>
    /// <remarks>
    /// <strong>And this is what separates "the world answered" from
    /// "the run happened"</strong>, -- <strong>and a runtime that
    /// cannot tell them apart reports progress it has not
    /// made.</strong>
    /// </remarks>
    public int Schreibvorgaenge { get; private set; }

    /// <summary>
    /// How many field reads this world answered.
    /// </summary>
    public int Lesevorgaenge { get; private set; }

    /// <summary>
    /// Whether a page asked the interpreter to wait for the player.
    /// </summary>
    /// <remarks>
    /// <strong>And this is XP's <c>@message_waiting</c> and not a
    /// return value.</strong> <c>command_101</c> returns <c>false</c>
    /// while the message is open, -- <strong>and a runner that read
    /// the return value as "finished" would skip every line of
    /// dialogue in the game.</strong>
    /// </remarks>
    public bool Wartet { get; private set; }

    /// <summary>
    /// The text the page asked to show, and null when it asked for none.
    /// </summary>
    public string? Text =>
        _felder.TryGetValue("message_text", out var wert)
            ? wert as string
            : null;

    /// <summary>Where the text starts on the screen, and zero when unset.</summary>
    public int WahlStart =>
        _felder.TryGetValue("choice_start", out var wert) && wert is int i
            ? i
            : 0;

    /// <summary>Every field name this world was asked about.</summary>
    public IReadOnlyCollection<string> FeldNamen => _felder.Keys;

    /// <summary>
    /// Answers one call from the interpreter.
    /// </summary>
    /// <param name="pEmpfaenger">The receiver, as a Ruby value.</param>
    /// <param name="pMethode">The method name, with or without its <c>=</c>.</param>
    /// <param name="pArgumente">The arguments, already evaluated.</param>
    /// <returns>
    /// The value, and null when this world does not have this field or
    /// this method -- which is a refusal and not a default.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <strong>And a <c>=</c> at the end of the name is what makes this
    /// a write.</strong> <c>$game_temp.message_text = x</c> reaches here
    /// as <c>message_text=</c>, -- <strong>and treating that as a read
    /// would answer nil and drop every line of dialogue in the
    /// game.</strong>
    /// </para>
    /// <para>
    /// <strong>And the value that comes back for a write is the value
    /// that was written</strong>, -- <strong>because Ruby assignment
    /// evaluates to its right side</strong>, -- <strong>and answering
    /// nil would make <c>a = b = 1</c> give a nil in
    /// <c>a</c>.</strong>
    /// </para>
    /// </remarks>
    public RubyValue? Rufe(
        RubyValue pEmpfaenger,
        string pMethode,
        IReadOnlyList<RubyValue> pArgumente)
    {
        if (pMethode.Length == 0)
        {
            return null;
        }

        var schreibt = pMethode.EndsWith("=", StringComparison.Ordinal);
        var feld = schreibt
            ? pMethode.Substring(0, pMethode.Length - 1)
            : pMethode;

        if (schreibt)
        {
            if (pArgumente.Count < 1)
            {
                return null;
            }

            _felder[feld] = Wert(pArgumente[0]);
            Schreibvorgaenge++;
            if (feld == "message_waiting" || feld == "waiting")
            {
                Wartet = Wahres(pArgumente[0]);
            }

            return pArgumente[0];
        }

        // **Und `message_text` wird nicht gelesen, sondern
        // geschrieben** -- **und ein Spiel liest es ueber
        // `Kernel#p`** -- **und diese Welt gibt es zurueck, wenn es
        // da ist.**
        if (!_felder.ContainsKey(feld))
        {
            return null;
        }

        Lesevorgaenge++;
        return pArgumente.Count > 0 ? pArgumente[0] : Ruby(_felder[feld]);
    }

    /// <summary>
    /// Whether a value is true, the way Ruby decides and not the way C# does.
    /// </summary>
    /// <param name="pWert">The value.</param>
    /// <returns>Yes or no, and only <c>nil</c> and <c>false</c> are no.</returns>
    /// <remarks>
    /// <strong>And this differs from C#.</strong> <c>0</c> is false in C#
    /// and true in Ruby, -- <strong>and <c>""</c> is true in Ruby
    /// too</strong>, -- <strong>and a wait flag read with the host's
    /// rule would stop waiting on a count of zero.</strong>
    /// </remarks>
    private static bool Wahres(RubyValue pWert) => pWert.Kind switch
    {
        RubyValueKind.Nil => false,
        RubyValueKind.Boolean => pWert.Boolean,
        _ => true,
    };

    private static object? Wert(RubyValue pWert) => pWert.Kind switch
    {
        RubyValueKind.Integer => pWert.Integer,
        RubyValueKind.String => System.Text.Encoding.UTF8
            .GetString(pWert.Bytes),
        RubyValueKind.Boolean => pWert.Boolean,
        RubyValueKind.Nil => null,
        _ => pWert,
    };

    private static RubyValue Ruby(object? pWert) => pWert switch
    {
        long i => RubyValue.OfInteger(i),
        string s => RubyValue.OfBytes(System.Text.Encoding.UTF8
            .GetBytes(s)),
        bool b => RubyValue.OfBoolean(b),
        null => RubyValue.Nil,
        _ => RubyValue.OfEmptyObject(pWert.GetType().Name),
    };
}
