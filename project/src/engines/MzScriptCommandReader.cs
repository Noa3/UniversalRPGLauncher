using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace UniversalRPG.Engines;

/// <summary>
/// Reads a game's JavaScript as data and names the commands it registers,
/// without executing any of it.
/// </summary>
/// <remarks>
/// <para>
/// <strong>This reader never evaluates a line of the game's code.</strong>
/// There is no engine, no interpreter and no <c>eval</c> anywhere in this
/// path: the file is read as text, and a bounded set of patterns picks the
/// registration names out of it. <strong>That is not a limitation dressed up
/// as a policy — a game's <c>rmmz_managers.js</c> is 83 KB of the author's
/// own code, and executing it would mean executing the game</strong>, which
/// this project does not do for an imported game.
/// </para>
/// <para>
/// So what this reader is good for is the question a player actually asks:
/// <em>which plugin commands can this game run, and which are built in?</em>
/// That is a data question, and the answer comes from the registrations.
/// </para>
/// </remarks>
public sealed class MzScriptCommandReader
{
    /// <summary>
    /// The plugin command registrations, as <c>(plugin, command)</c> pairs.
    /// </summary>
    public IReadOnlyList<MzPluginCommand> Commands { get; private set; } = Array.Empty<MzPluginCommand>();

    /// <summary>
    /// The method names assigned onto engine prototypes, which is how the
    /// built-in commands and the hooks are written.
    /// </summary>
    public IReadOnlyList<string> PrototypeMethods { get; private set; } = Array.Empty<string>();

    /// <summary>What the reader could not read, and why.</summary>
    public IReadOnlyList<string> Diagnostics { get; private set; } = Array.Empty<string>();

    /// <summary>
    /// Reads one script file as data.
    /// </summary>
    /// <param name="pSource">The file's text.</param>
    /// <param name="pFileName">The file's name, for diagnostics only.</param>
    /// <returns>Never null. A file that is not readable yields an empty
    /// result and a diagnostic.</returns>
    /// <remarks>
    /// <strong>The patterns are bounded and anchored on the engine's own
    /// naming.</strong> A generic "find every function name" pass would name
    /// thousands of private helpers a game happens to have, and a list a
    /// player cannot act on is the same as no list.
    /// </remarks>
    public MzScriptCommandReader Read(string pSource, string pFileName)
    {
        var commands = new List<MzPluginCommand>();
        var methods = new List<string>();
        var diagnostics = new List<string>();

        if (pSource == null)
        {
            diagnostics.Add($"{pFileName}: no source, and nothing was read");
            Commands = commands;
            PrototypeMethods = methods;
            Diagnostics = diagnostics;
            return this;
        }

        // **PluginManager.registerCommand("PluginName", "command", fn)** --
        // the shape every MZ plugin command is written in.
        foreach (Match m in Regex.Matches(
            pSource,
            @"registerCommand\s*\(\s*[""']([^""']+)[""']\s*,\s*[""']([^""']+)[""']",
            RegexOptions.CultureInvariant))
        {
            var plugin = m.Groups[1].Value;
            var command = m.Groups[2].Value;

            // **Der Doppelpunkt ist der Schluessel, den der Aufruf sucht** --
            // `PluginManager.callCommand` baut ihn aus Plugin und Befehl, und
            // ein Leser, der ihn wegließe, würde zwei Befehle verschiedener
            // Plugins als denselben ausgeben.
            commands.Add(new MzPluginCommand(plugin, command, $"{plugin}:{command}"));
        }

        // **Prototype-Methoden: "X.prototype.y =".** Das ist die Form, in der
        // die eingebauten Befehle und die Ereignis-Haken geschrieben sind.
        foreach (Match m in Regex.Matches(
            pSource,
            @"([A-Za-z_$][\w$]*)\.prototype\.([A-Za-z_$][\w$]*)\s*=",
            RegexOptions.CultureInvariant))
        {
            var typ = m.Groups[1].Value;
            var methode = m.Groups[2].Value;
            methods.Add($"{typ}.prototype.{methode}");
        }

        // **Eine Datei, die weder das eine noch das andere traegt, ist eine
        // mit einem anderen Aufbau** -- und das ist eine Aussage, keine
        // Luecke. Ein stilles leeres Ergebnis wuerde eine Datei, die der
        // Leser nicht versteht, von einer ohne Befehle unterscheiden.
        if (commands.Count == 0 && methods.Count == 0)
        {
            diagnostics.Add(
                $"{pFileName}: no PluginManager.registerCommand and no "
                + "prototype assignment; the file is readable but this reader "
                + "does not know its shape");
        }

        // **Doppelte Namen fallen weg** -- zwei Plugins koennen denselben
        // Befehlsnamen nennen, und zweimal derselbe Eintrag ist eine
        // Ausgabe, die nichts zaehlt.
        var gesehen = new HashSet<string>(StringComparer.Ordinal);
        var eindeutig = new List<MzPluginCommand>();
        foreach (var c in commands)
        {
            if (gesehen.Add(c.Key))
            {
                eindeutig.Add(c);
            }
        }

        Commands = eindeutig;
        PrototypeMethods = methods;
        Diagnostics = diagnostics;
        return this;
    }
}

/// <summary>One plugin command a game's own code registers.</summary>
/// <param name="pPlugin">The plugin that registers it.</param>
/// <param name="pCommand">The command's name, as the event command names it.</param>
/// <param name="pKey">The two joined, which is the key the call site builds.</param>
public readonly record struct MzPluginCommand(
    string pPlugin,
    string pCommand,
    string pKey)
{
    /// <summary>The plugin that registers this command.</summary>
    public string Plugin => pPlugin;

    /// <summary>The command's name, as an event command names it.</summary>
    public string Command => pCommand;

    /// <summary>The two joined, which is the key a call site builds.</summary>
    public string Key => pKey;
}
