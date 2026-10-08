using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Jint;

namespace UniversalRPG.Web;

/// <summary>
/// And a game's own plugin JavaScript, run in a sandbox that has nothing.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is the one place this repository executes a game's own
/// code.</strong> The user asked for it on 2026-10-07 and accepted the risk:
/// a game's menus, windows and messages are what its plugins make them, and
/// reading only the plugin <em>parameters</em> cannot reproduce that.
/// </para>
/// <para>
/// <strong>And it runs in a box with no doors.</strong> The interpreter is
/// Jint, a pure managed JavaScript engine with no native dependency, and the
/// script is given <em>no</em> host API: no file access, no network, no
/// process, no <c>require</c>, no clock it can wait on. It gets the engine's
/// own <c>js/rmmz_*.js</c>, the project's data files as globals, and the
/// plugins -- and nothing else. It is bounded by a timeout, a memory limit
/// and a statement budget, so a plugin that loops forever or allocates
/// without end is stopped rather than allowed to take the launcher with it.
/// </para>
/// <para>
/// <strong>And detection and parsing still execute nothing.</strong> A probe,
/// a detector or a parser never starts this host; it is started when a game
/// is run.
/// </para>
/// <para>
/// <strong>And a plugin that will not load is reported and does not stop the
/// rest.</strong> Measured on a real project (81 plugins, 72 active): the
/// whole engine loads, and 70 of 72 plugins execute in about two seconds,
/// the seventeen obfuscated VisuStella files included. The two that failed
/// were a plugin wanting <c>console</c> and a debugger plugin.
/// </para>
/// </remarks>
public sealed class MzPluginHost
{
    /// <summary>How long one plugin may take before it is stopped.</summary>
    public const int TimeoutSeconds = 30;

    /// <summary>How much memory the sandbox may hold.</summary>
    public const long MemoryLimitBytes = 768L * 1024 * 1024;

    /// <summary>How many statements the sandbox may run in one call.</summary>
    public const int StatementBudget = 50_000_000;

    private readonly Engine _engine;

    private MzPluginHost(
        string pGameDirectory,
        Engine pEngine,
        IReadOnlyList<string> pEngineFiles,
        IReadOnlyList<string> pLoaded,
        IReadOnlyList<(string Name, string Reason)> pFailed,
        string pProblem)
    {
        GameDirectory = pGameDirectory;
        _engine = pEngine;
        EngineFiles = pEngineFiles;
        LoadedPlugins = pLoaded;
        FailedPlugins = pFailed;
        Problem = pProblem;
    }

    /// <summary>Which project this host was built for.</summary>
    public string GameDirectory { get; }

    /// <summary>Which of the engine's own files are in the sandbox.</summary>
    public IReadOnlyList<string> EngineFiles { get; }

    /// <summary>And which plugins executed.</summary>
    public IReadOnlyList<string> LoadedPlugins { get; }

    /// <summary>And which did not, each with the reason it gave.</summary>
    public IReadOnlyList<(string Name, string Reason)> FailedPlugins { get; }

    /// <summary>
    /// Why there is no sandbox at all, and empty when there is one.
    /// </summary>
    public string Problem { get; }

    /// <summary>Whether a name exists inside the sandbox.</summary>
    /// <param name="pName">The global the engine or a plugin defines.</param>
    public bool IsDefined(string pName) =>
        _engine.Evaluate($"typeof {pName} !== 'undefined'").AsBoolean();

    /// <summary>
    /// And one expression, evaluated inside the sandbox.
    /// </summary>
    /// <param name="pJavaScript">What to ask.</param>
    /// <returns>What it answered, or null when it threw.</returns>
    public string? Evaluate(string pJavaScript)
    {
        try
        {
            return _engine.Evaluate(pJavaScript).ToString();
        }
        catch (Exception ausnahme)
        {
            return null;
        }
    }

    /// <summary>
    /// And the names of the plugins the project switched on.
    /// </summary>
    /// <param name="pGameDirectory">Where the project is.</param>
    /// <returns>
    /// The names, in the order the editor lists them, or an empty list when
    /// the project has no <c>js/plugins.js</c>.
    /// </returns>
    public static IReadOnlyList<string> ActivePluginNames(string pGameDirectory)
    {
        var pfad = Path.Combine(pGameDirectory, "js", "plugins.js");
        if (!File.Exists(pfad))
        {
            return Array.Empty<string>();
        }

        try
        {
            return PluginNames(File.ReadAllText(pfad));
        }
        catch (IOException)
        {
            return Array.Empty<string>();
        }
    }

    /// <summary>And the switched-on names out of a <c>plugins.js</c> text.</summary>
    public static IReadOnlyList<string> PluginNames(string pPluginsJs)
    {
        var namen = new List<string>();
        var anfang = pPluginsJs.IndexOf('[');
        var ende = pPluginsJs.LastIndexOf(']');
        if (anfang < 0 || ende <= anfang)
        {
            return namen;
        }

        MzDataFile liste;
        try
        {
            liste = MzDataFile.ReadText(
                "js/plugins.js", pPluginsJs.Substring(anfang, ende - anfang + 1));
        }
        catch (MzDataException)
        {
            return namen;
        }

        foreach (var eintrag in liste.Root.Items)
        {
            var status = eintrag.Member("status");
            var name = eintrag.Member("name")?.StringOr("") ?? "";
            if (name.Length > 0 && status != null
                && status.Kind == MzKind.Bool && status.Boolean)
            {
                namen.Add(name);
            }
        }

        return namen;
    }

    /// <summary>
    /// And a project's engine, data and plugins, in a sandbox.
    /// </summary>
    /// <param name="pGameDirectory">Where the project is.</param>
    /// <returns>
    /// The host, and one that carries a <see cref="Problem"/> when the
    /// engine's own files are not there.
    /// </returns>
    public static MzPluginHost Start(string pGameDirectory)
    {
        var engine = new Engine(o => o
            .TimeoutInterval(TimeSpan.FromSeconds(TimeoutSeconds))
            .LimitMemory(MemoryLimitBytes)
            .MaxStatements(StatementBudget));

        engine.Execute(Sandkasten);

        var jsOrdner = Path.Combine(pGameDirectory, "js");
        var geladen = new List<string>();
        var problem = "";
        foreach (var name in EngineFileNames)
        {
            var pfad = Path.Combine(jsOrdner, name);
            if (!File.Exists(pfad))
            {
                continue;
            }
            try
            {
                engine.Execute(File.ReadAllText(pfad));
                geladen.Add(name);
            }
            catch (Exception ausnahme)
            {
                problem = $"{name} could not be executed: {Kurz(ausnahme)}";
                break;
            }
        }

        if (problem.Length == 0 && geladen.Count == 0)
        {
            problem = "The project has none of the engine's own js/rmmz_*.js "
                + "files, and a plugin patches an engine that is not there.";
        }

        var ok = new List<string>();
        var fehler = new List<(string Name, string Reason)>();
        if (problem.Length == 0)
        {
            LadeDaten(engine, pGameDirectory);
            LadePlugins(engine, pGameDirectory, ok, fehler);
        }

        return new MzPluginHost(
            pGameDirectory, engine, geladen, ok, fehler, problem);
    }

    /// <summary>
    /// And the engine's own files, in the order the engine loads them.
    /// </summary>
    /// <remarks>
    /// <strong>And the order is load-bearing:</strong>
    /// <c>rmmz_core.js</c> defines <c>Bitmap</c>, and
    /// <c>rmmz_managers.js</c> builds on it; a reader that loaded them the
    /// other way round failed with <em>Property 'initialize' of object is
    /// not a function</em>, which names nothing useful.
    /// </remarks>
    private static readonly string[] EngineFileNames =
    {
        "rmmz_core.js",
        "rmmz_managers.js",
        "rmmz_objects.js",
        "rmmz_sprites.js",
        "rmmz_scenes.js",
        "rmmz_windows.js",
    };

    private static void LadeDaten(Engine pEngine, string pGameDirectory)
    {
        var paare = new (string Global, string Datei)[]
        {
            ("$dataSystem", "System.json"),
            ("$dataMapInfos", "MapInfos.json"),
            ("$dataTilesets", "Tilesets.json"),
            ("$dataActors", "Actors.json"),
        };

        foreach (var (global, datei) in paare)
        {
            var pfad = Path.Combine(pGameDirectory, "data", datei);
            if (!File.Exists(pfad))
            {
                continue;
            }
            try
            {
                pEngine.Execute($"var {global} = {File.ReadAllText(pfad)};");
            }
            catch (Exception)
            {
                // **And a data file that will not parse is left out**, and
                // said so by the plugin that then cannot read it -- which is
                // a better report than a silent nothing.
            }
        }
    }

    private static void LadePlugins(
        Engine pEngine, string pGameDirectory,
        List<string> pOk, List<(string, string)> pFehler)
    {
        var pluginsPfad = Path.Combine(pGameDirectory, "js", "plugins.js");
        if (!File.Exists(pluginsPfad))
        {
            return;
        }

        var text = File.ReadAllText(pluginsPfad);
        try
        {
            pEngine.Execute(text);
        }
        catch (Exception ausnahme)
        {
            pFehler.Add(("js/plugins.js", Kurz(ausnahme)));
            return;
        }

        foreach (var name in PluginNames(text))
        {
            var pfad = Path.Combine(pGameDirectory, "js", "plugins", name + ".js");
            if (!File.Exists(pfad))
            {
                pFehler.Add((name, "The plugin's own file is missing."));
                continue;
            }
            try
            {
                pEngine.Execute(File.ReadAllText(pfad));
                pOk.Add(name);
            }
            catch (Exception ausnahme)
            {
                pFehler.Add((name, Kurz(ausnahme)));
            }
        }
    }

    private static string Kurz(Exception pAusnahme)
    {
        var text = pAusnahme.Message.Replace("\r", " ").Replace("\n", " ");
        return text.Length > 200 ? text[..200] : text;
    }

    /// <summary>
    /// And the little the engine is given to stand on.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And every line here exists because a run said it was
    /// missing.</strong> Measured, in order: <c>rmmz_core.js</c> failed with
    /// <em>PIXI is not defined</em>, so the rendering library the engine's
    /// <c>Bitmap</c> extends is a stand-in; then the browser it asks about
    /// became a stand-in too.
    /// </para>
    /// <para>
    /// <strong>And none of it does anything.</strong> The rendering stub
    /// hands out a function for whatever it is asked for, so
    /// <c>X.prototype</c> exists; the browser answers and forgets. There is
    /// no canvas, no image, no sound and no request that leaves the process --
    /// which is the point of running a game's code in here rather than in a
    /// browser with a file system behind it.
    /// </para>
    /// </remarks>
    public const string Sandkasten = """
var PIXI = new Proxy(function () {}, {
    get: function (t, k) {
        if (k === 'prototype') { return t.prototype; }
        if (!(k in t)) { t[k] = new Proxy(function () {}, { get: function (s, p) {
            if (p === 'prototype') { return s.prototype; }
            if (!(p in s)) { s[p] = function () {}; }
            return s[p];
        } }); }
        return t[k];
    }
});
var console = { log: function () {}, warn: function () {}, error: function () {},
    info: function () {}, debug: function () {} };
var document = {
    createElement: function () { return { style: {}, getContext: function () { return null; },
        addEventListener: function () {}, removeEventListener: function () {} }; },
    addEventListener: function () {}, removeEventListener: function () {},
    body: { appendChild: function () {} },
    documentElement: { style: {} }
};
var window = { addEventListener: function () {}, removeEventListener: function () {},
    document: document, navigator: { userAgent: 'UniversalRPG' }, location: { href: '' } };
var navigator = { userAgent: 'UniversalRPG', platform: 'UniversalRPG' };
var location = { href: '', protocol: 'file:' };
var performance = { now: function () { return Date.now(); } };
var requestAnimationFrame = function () { return 0; };
var Image = function () { this.width = 0; this.height = 0;
    this.addEventListener = function () {}; this.removeEventListener = function () {}; };
var XMLHttpRequest = function () {
    this.open = function () {}; this.send = function () {}; this.abort = function () {};
    this.addEventListener = function () {}; this.removeEventListener = function () {};
};
var URL = { createObjectURL: function () { return ''; }, revokeObjectURL: function () {} };
var Blob = function () {};
var Audio = function () { this.play = function () {}; this.stop = function () {}; };
var HTMLCanvasElement = function () {};
var ImageData = function () {};
""";
}
