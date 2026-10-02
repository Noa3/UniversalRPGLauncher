using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace UniversalRPG.Rgss;

/// <summary>
/// One command as the game's own interpreter script writes it.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is read out of the game, and not written
/// here.</strong> A table written from what RPG Maker is remembered to
/// contain is a different thing from the one a given project ships, --
/// <strong>and a game with a cut-down interpreter would run commands
/// this table claims exist.</strong>
/// </para>
/// </remarks>
public sealed class RgssQuellBefehl
{
    /// <summary>The command's own number, from <c>def command101</c>.</summary>
    public int Code { get; init; }

    /// <summary>Which script the method was found in.</summary>
    public string Skript { get; init; } = "";

    /// <summary>
    /// The method's own body, without the <c>def</c> and the
    /// <c>end</c>.
    /// </summary>
    /// <remarks>
    /// <strong>And the body is kept and not just the name</strong>, --
    /// <strong>because the name says what a command is called and the
    /// body says what it does</strong>, -- <strong>and only the second
    /// one tells a reader whether a port is faithful.</strong>
    /// </remarks>
    public string Koerper { get; init; } = "";

    /// <summary>The lines the body has, and empty means a one-liner.</summary>
    public int Zeilen { get; init; }
}

/// <summary>
/// Reads a project's own command set out of its interpreter scripts.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this executes nothing.</strong> It reads Ruby text out
/// of a Marshal file, matches a method name, and keeps the text. No
/// Ruby is evaluated, no method is called, and a game's script is
/// treated as data.
/// </para>
/// <para>
/// <strong>And RPG Maker XP splits its interpreter over seven
/// scripts</strong>, named <c>Interpreter 1</c> to <c>Interpreter 7</c>,
/// -- <strong>and VX has one <c>Game_Interpreter</c></strong>, --
/// <strong>and both spellings are read here</strong>, -- <strong>because
/// a reader that only knows one of them finds nothing in half of all
/// projects.</strong>
/// </para>
/// </remarks>
public static class RgssQuellBefehle
{
    private static readonly Regex Definition = new(
        @"^\s*def\s+command_?(\d{3})\s*$",
        RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex Skriptname = new(
        @"^(?:class|module)\s+(\w+)",
        RegexOptions.Compiled | RegexOptions.Multiline);

    /// <summary>
    /// Reads every command a project's interpreter scripts define.
    /// </summary>
    /// <param name="pSkriptePfad">A <c>Scripts.rxdata</c> or <c>Scripts.rvdata2</c>.</param>
    /// <param name="pFehler">What stopped it, and empty when nothing did.</param>
    /// <returns>The commands, ordered by their number.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And a script that could not be inflated is skipped and
    /// said so</strong>, -- <strong>not counted as a script without
    /// commands</strong>, -- <strong>because those two are different
    /// answers and only the first one is a fact about the game.</strong>
    /// </para>
    /// </remarks>
    public static List<RgssQuellBefehl> Lese(string pSkriptePfad, out string pFehler)
    {
        pFehler = "";
        var gefunden = new Dictionary<int, RgssQuellBefehl>();
        List<XpScript> skripte;
        try
        {
            skripte = XpScriptBodies.LeseAlle(pSkriptePfad);
        }
        catch (IOException ausnahme)
        {
            pFehler = ausnahme.Message;
            return new List<RgssQuellBefehl>();
        }

        var uebersprungen = 0;
        foreach (var skript in skripte)
        {
            if (!skript.Entpackt || skript.Text == null)
            {
                if (IstInterpreterskript(skript.Name))
                {
                    uebersprungen++;
                }

                continue;
            }

            if (!IstInterpreterskript(skript.Name))
            {
                continue;
            }

            foreach (Match treffer in Definition.Matches(
                skript.Text))
            {
                var code = int.Parse(
                    treffer.Groups[1].Value, CultureInfo.InvariantCulture);
                if (gefunden.ContainsKey(code))
                {
                    continue;
                }

                var koerper = Koerper(skript.Text, treffer.Index);
                gefunden[code] = new RgssQuellBefehl
                {
                    Code = code,
                    Skript = skript.Name,
                    Koerper = koerper,
                    Zeilen = koerper.Length == 0
                        ? 1
                        : koerper.Split('\n').Length,
                };
            }
        }

        if (uebersprungen > 0)
        {
            pFehler = $"{uebersprungen} interpreter script(s) could not be"
                + " inflated, and their commands are unknown";
        }

        var alle = new List<RgssQuellBefehl>(gefunden.Values);
        alle.Sort(static (a, b) => a.Code.CompareTo(b.Code));
        return alle;
    }

    /// <summary>
    /// Whether a script carries an interpreter, under either spelling.
    /// </summary>
    /// <param name="pName">The script's own name.</param>
    /// <returns>Yes or no.</returns>
    /// <remarks>
    /// <para>
    /// <para>
    /// <strong>And both spellings are measured on this machine.</strong>
    /// MicroQuest's <c>Scripts.rxdata</c> holds <c>Interpreter 1</c>
    /// through <c>Interpreter 7</c> and no <c>Game_Interpreter</c> at
    /// all, -- <strong>and the commands live in parts three to
    /// seven</strong>, -- <strong>and part one holds the setup
    /// instead</strong>, -- <strong>and a reader that only looks at
    /// part one reads an XP project and finds nothing.</strong>
    /// </para>
    /// <para>
    /// <strong>And the method name differs too.</strong> XP writes
    /// <c>def command_101</c> with an underscore and VX writes
    /// <c>def command101</c> without one, -- <strong>and a pattern for
    /// one generation finds zero methods in the other.</strong>
    /// </para>
    /// <para>
    /// <strong>And XP indents its methods by two spaces</strong>, --
    /// <strong>and the line is <c>  def command_101</c> and not
    /// <c>def command_101</c></strong>, -- <strong>and a pattern
    /// anchored at the start of the line matches nothing in half of all
    /// projects.</strong>
    /// </para>
    /// </para>
    /// </remarks>
    public static bool IstInterpreterskript(string pName) =>
        pName == "Game_Interpreter"
        || pName.StartsWith("Interpreter ", StringComparison.Ordinal);

    /// <summary>
    /// One method's body, without its <c>def</c> and its <c>end</c>.
    /// </summary>
    /// <param name="pQuelltext">The script's own text.</param>
    /// <param name="pAnfang">Where the <c>def</c> was found.</param>
    /// <returns>The body, and empty for a one-liner.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And the body ends at the matching <c>end</c></strong>,
    /// -- <strong>and Ruby nests</strong>, -- <strong>so counting
    /// <c>end</c> lines alone cuts a command with a branch in half.**
    /// </para>
    /// <para>
    /// <strong>And <c>command101</c> is a one-liner with a trailing
    /// <c>end</c></strong>, -- <strong>and
    /// <c>command102</c> is three branches with three
    /// <c>end</c></strong>, -- <strong>and both were measured.</strong>
    /// </para>
    /// </remarks>
    private static string Koerper(string pQuelltext, int pAnfang)
    {
        // **Und hier beginnt der Koerper bei der gefundenen `def`**
        // -- **und nicht bei der ersten `def` der Datei.**
        //
        // **Und das ist ein Fehler, der in einem Skript mit sieben
        // `Interpreter`-Teilen sichtbar wird**, -- **denn der Koerper
        // von `command101` waere sonst der von `initialize`.**
        var zeilen = pQuelltext.Split('\n');
        var beginn = 0;
        for (var i = 0; i < zeilen.Length; i++)
        {
            if (pAnfang < zeilen[i].Length)
            {
                break;
            }

            pAnfang -= zeilen[i].Length + 1;
            beginn = i + 1;
        }

        var tiefe = 0;
        var sammel = new List<string>();
        for (var i = beginn; i < zeilen.Length; i++)
        {
            var zeile = zeilen[i].Trim();
            if (tiefe == 0)
            {
                // **Und hier ist noch nichts offen** -- **und die
                // gesuchte Zeile ist die erste `def` danach.**
                // **Und `zeile` ist getrimmt**, -- **und damit ist die
                // Einrueckung weg**, -- **und deshalb greift `def `
                // bei XP genauso wie bei VX.**
                if (!zeile.StartsWith("def ", StringComparison.Ordinal))
                {
                    continue;
                }

                tiefe = 1;
                continue;
            }

            if (zeile.Length == 0)
            {
                sammel.Add(zeile);
                continue;
            }

            if (zeile.StartsWith("if ", StringComparison.Ordinal)
                || zeile.StartsWith("unless ", StringComparison.Ordinal)
                || zeile.StartsWith("while ", StringComparison.Ordinal)
                || zeile.StartsWith("until ", StringComparison.Ordinal)
                || zeile.StartsWith("case ", StringComparison.Ordinal)
                || zeile.StartsWith("begin", StringComparison.Ordinal)
                || zeile.StartsWith("for ", StringComparison.Ordinal))
            {
                tiefe++;
                sammel.Add(zeile);
                continue;
            }

            if (zeile == "end"
                || zeile.StartsWith("end ", StringComparison.Ordinal))
            {
                tiefe--;
                if (tiefe == 0)
                {
                    break;
                }

                sammel.Add(zeile);
                continue;
            }

            sammel.Add(zeile);
        }

        while (sammel.Count > 0 && sammel[sammel.Count - 1].Length == 0)
        {
            sammel.RemoveAt(sammel.Count - 1);
        }

        return string.Join("\n", sammel);
    }
}