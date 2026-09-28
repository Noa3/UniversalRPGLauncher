using System;
using System.Collections.Generic;
using Godot;

using UniversalRPG.Engines;
using UniversalRPG.Web;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Tests for <see cref="MzScriptCommandReader"/> — the reader that names a
/// game's plugin commands without executing any of its JavaScript.
/// </summary>
public partial class TestMzScriptCommands : TestBase
{
    private const string FixtureRoot = "res://tests/fixtures/mz";

    /// <summary>
    /// Reads the fixture through Godot's own file access, and the same way
    /// every other fixture in this suite is read.
    /// </summary>
    /// <remarks>
    /// <strong><c>File.ReadAllText</c> does not understand <c>res://</c></strong>
    /// — it treated it as a relative Windows path and threw
    /// <c>"Die Syntax für den Dateinamen ist falsch"</c>. A reader that
    /// reached for the .NET file API on a Godot path fails on the first
    /// fixture it is given.
    /// </remarks>
    private string Fixture()
    {
        var pfad = FixtureRoot.PathJoin("js").PathJoin("rmmz_managers.js");
        var bytes = Godot.FileAccess.GetFileAsBytes(pfad);
        AssertTrue(bytes != null && bytes.Length > 0,
            "the fixture is present and not empty at " + pfad);
        return System.Text.Encoding.UTF8.GetString(bytes!);
    }

    /// <summary>
    /// A plugin command is named by its two arguments, not by the function
    /// that carries it.
    /// </summary>
    /// <remarks>
    /// <strong>The reference's own call site builds the key from the two
    /// strings:</strong> <c>PluginManager.callCommand(pluginName, args)</c>
    /// looks up <c>pluginName + ":" + args[0]</c>. <strong>A reader that
    /// reported the function's name would name something the game never
    /// calls</strong> — a plugin's implementation detail rather than its
    /// command.
    /// </remarks>
    public void Test_ACommandIsNamedByItsTwoArguments()
    {
        var reader = new MzScriptCommandReader().Read(Fixture(), "rmmz_managers.js");

        AssertTrue(reader.Commands.Count >= 0, "the reader returned");
        // **Die Fixture registriert nichts ueber registerCommand -- das ist
        // der Punkt der naechsten Tests.** Hier geht es um die Form selbst.
        var probe = new MzScriptCommandReader().Read(
            "PluginManager.registerCommand('MyPlugin', 'GiveItem', fn);",
            "probe.js");
        AssertEq(probe.Commands.Count, 1, "**one registration is one command**");
        AssertEq(probe.Commands[0].Plugin, "MyPlugin",
            "**the plugin is the first argument** — the reference's own key");
        AssertEq(probe.Commands[0].Command, "GiveItem",
            "**and the command is the second** — a reader that reported the "
                + "function's name would name something the game never calls");
        AssertEq(probe.Commands[0].Key, "MyPlugin:GiveItem",
            "**and the key is the two joined** — `callCommand` builds exactly "
                + "this, so a reader that left the colon out would not match "
                + "a call site");
    }

    /// <summary>
    /// Nothing is executed, and a file that would run code is still just read.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>This is the whole point of the reader and it is asserted, not
    /// claimed.</strong> The source below is a valid registration wrapped in
    /// code that would throw if it ran.
    /// </para>
    /// <para>
    /// <strong>Why this matters and not in theory:</strong> an imported
    /// game's <c>rmmz_managers.js</c> is 83 KB of the author's own code, and
    /// this project does not execute a game. A reader that evaluated any of
    /// it would be a remote-code-execution path with a game file as the
    /// payload — and the extraction below gets the same answer without it.
    /// </para>
    /// </remarks>
    public void Test_NothingIsExecuted()
    {
        const string quelle = """
            PluginManager.registerCommand('Evil', 'Run', function () {
                throw new Error("this must never run");
            });
            while (true) { }
            """;

        MzScriptCommandReader reader = null;
        var fertig = false;
        var ausnahme = "";
        // **Ein eigener Thread mit einem Limit**, weil die Aussage
        // "laeuft nicht" sonst nur eine Behauptung waere.
        var thread = new System.Threading.Thread(() =>
        {
            try
            {
                reader = new MzScriptCommandReader().Read(quelle, "evil.js");
                fertig = true;
            }
            catch (Exception x)
            {
                ausnahme = x.GetType().Name;
            }
        });
        thread.IsBackground = true;
        thread.Start();
        var beendet = thread.Join(5000);

        AssertTrue(beendet,
            "**the reader returned within five seconds** — a source with an "
                + "`while (true)` in it cannot have been executed");
        AssertEq(ausnahme, "",
            "**and it threw nothing** — the `throw` in the source is text, "
                + "not code; a reader that evaluated it would have reported "
                + "an exception here");
        AssertTrue(fertig && reader != null, "the reader produced a result");
        AssertEq(reader!.Commands.Count, 1,
            "**and it still found the command** — reading the name out of the "
                + "text does not need the code around it to be safe");
    }

    /// <summary>
    /// A file whose shape the reader does not know says so.
    /// </summary>
    /// <remarks>
    /// <strong>An empty result and a diagnostic are not the same
    /// thing.</strong> Without the diagnostic, a file the reader failed to
    /// understand would look like a file with no commands — <strong>and a
    /// player would conclude a game has no plugin commands when this reader
    /// simply did not recognise the file.</strong>
    /// </remarks>
    public void Test_AFileTheReaderDoesNotUnderstandSaysSo()
    {
        var reader = new MzScriptCommandReader().Read(
            "this is not javascript at all, and it names nothing",
            "unknown.js");

        AssertEq(reader.Commands.Count, 0, "no commands were found");
        AssertEq(reader.PrototypeMethods.Count, 0, "and no prototype methods");
        AssertTrue(reader.Diagnostics.Count == 1,
            "**and there is exactly one diagnostic** — " + string.Join(" | ",
                reader.Diagnostics));
        AssertTrue(reader.Diagnostics[0].Contains("does not know its shape"),
            "**and it says the reader does not know the shape** — an empty "
                + "result and a failure to read are different answers: "
                + reader.Diagnostics[0]);
    }

    /// <summary>
    /// A prototype assignment is a method, and the type is part of the name.
    /// </summary>
    /// <remarks>
    /// <strong>The built-in commands and the event hooks are written as
    /// <c>X.prototype.y =</c> assignments</strong>, and the type is what says
    /// whose command it is. A reader that reported only <c>y</c> would
    /// collapse <c>Window_Base.prototype.drawLine</c> and
    /// <c>Window_ItemList.prototype.drawLine</c> into one name.
    /// </remarks>
    public void Test_APrototypeAssignmentNamesTypeAndMethod()
    {
        var reader = new MzScriptCommandReader().Read(Fixture(), "rmmz_managers.js");

        var gefunden = false;
        foreach (var m in reader.PrototypeMethods)
        {
            if (m == "Window_Base.prototype.drawLine")
            {
                gefunden = true;
            }
        }

        AssertTrue(gefunden,
            "**`Window_Base.prototype.drawLine` is named as it is written** — "
                + "the type is part of the name, and a reader that reported "
                + "only the method would collapse two classes' commands into "
                + "one. The reader found: "
                + string.Join(" | ", reader.PrototypeMethods));
    }

    /// <summary>
    /// Two plugins may name the same command, and the key keeps them apart.
    /// </summary>
    /// <remarks>
    /// <strong>The key is what the call site builds, so it is what keeps
    /// them apart</strong> — and a reader that reported the command name alone
    /// would show one command where the game has two, and a player choosing
    /// between them would have no way to say which.
    /// </remarks>
    public void Test_TwoPluginsMayNameTheSameCommand()
    {
        var reader = new MzScriptCommandReader().Read(
            "PluginManager.registerCommand('A', 'Give', fn1);\n"
            + "PluginManager.registerCommand('B', 'Give', fn2);",
            "zwei.js");

        AssertEq(reader.Commands.Count, 2,
            "**two registrations are two commands** — even with the same name, "
                + "because the plugin is half of the key");
        AssertEq(reader.Commands[0].Key, "A:Give", "the first belongs to A");
        AssertEq(reader.Commands[1].Key, "B:Give", "**and the second to B**");
    }

    /// <summary>
    /// The same registration twice is one command.
    /// </summary>
    /// <remarks>
    /// <strong>The second registration overwrites the first at runtime</strong>
    /// — <c>$plugins[key] = fn</c> — <strong>so there is one command with
    /// that key and not two</strong>. A list that counted the registrations
    /// would tell a player their game has a command it only has once.
    /// </remarks>
    public void Test_TheSameRegistrationTwiceIsOneCommand()
    {
        var reader = new MzScriptCommandReader().Read(
            "PluginManager.registerCommand('A', 'Give', fn1);\n"
            + "PluginManager.registerCommand('A', 'Give', fn2);",
            "doppelt.js");

        AssertEq(reader.Commands.Count, 1,
            "**the same key twice is one command** — the second registration "
                + "overwrites the first, and a list that counted both would "
                + "tell a player their game has a command it has once");
    }

    /// <summary>
    /// A missing source is a diagnostic and not a crash.
    /// </summary>
    /// <remarks>
    /// <strong>An imported game's data directory can have a script the
    /// manifest names and the disk does not hold</strong> — a trimmed
    /// release, a partial copy. The reader reports that and returns empty
    /// rather than throwing in the middle of a scan.
    /// </remarks>
    public void Test_AMissingSourceIsADiagnostic()
    {
        var reader = new MzScriptCommandReader().Read(null, "weg.js");

        AssertEq(reader.Commands.Count, 0, "no commands from no source");
        AssertEq(reader.Diagnostics.Count, 1,
            "**and one diagnostic** — a missing file is an answer, and a "
                + "reader that threw here would stop a scan of a whole "
                + "directory");
        AssertTrue(reader.Diagnostics[0].Contains("no source"),
            "and it says what was missing: " + reader.Diagnostics[0]);
    }

    /// <summary>
    /// The real fixture carries no registration, and the reader says so.
    /// </summary>
    /// <remarks>
    /// <strong>The placeholder is still the placeholder.</strong> The real
    /// <c>rmmz_managers.js</c> is a game asset and is not in this repository,
    /// and the test says which file it read — <strong>so a reader that found
    /// nothing here cannot be mistaken for a reader that would find something
    /// in a real game.</strong>
    /// </remarks>
    public void Test_TheRealFixtureIsNamedAndItsShapeStated()
    {
        var reader = new MzScriptCommandReader().Read(Fixture(), "rmmz_managers.js");

        var gefunden = false;
        foreach (var d in reader.Diagnostics)
        {
            if (d.Contains("rmmz_managers.js"))
            {
                gefunden = true;
            }
        }

        // **Oder der Leser fand etwas, oder er sagt, dass er die Form nicht
        // kennt** -- und in beiden Faellen nennt er die Datei, damit die
        // Antwort einem Spiel zugeordnet bleibt und nicht der Suite.
        if (reader.Commands.Count == 0 && reader.PrototypeMethods.Count == 0)
        {
            AssertTrue(gefunden,
                "**a file with neither shape says which file it was** — the "
                    + "diagnostics were: " + string.Join(" | ", reader.Diagnostics));
        }
        else
        {
            AssertTrue(reader.PrototypeMethods.Count > 0,
                "**and a file that has the shape reports the methods it read** "
                    + "— " + reader.PrototypeMethods.Count + " from "
                    + "rmmz_managers.js");
        }
    }
}
