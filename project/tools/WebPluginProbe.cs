using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Godot;
using UniversalRPG.JavaScript.Jint;
using UniversalRPG.Plugins;
using UniversalRPG.Sdk;

namespace UniversalRPG.Tools;

/// <summary>
/// Explicit developer entry point for a user-owned MV/MZ folder. Inspection is
/// the default. --execute-plugins is the isolated subset; --execute-core loads
/// original libraries/cores before plugins. Neither executes main.js or proves
/// game playability. No mode sets engine Runtime capability.
/// </summary>
public partial class WebPluginProbe : Node
{
    public override void _Ready()
    {
        var exitCode = 2;
        var report = new Dictionary<string, object?>
        {
            ["schema"] = "urpg.web-plugin-probe.v2",
            ["originalEntryPointExecuted"] = false,
            ["originalCoreScriptsCompleted"] = false,
            ["utc"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            ["fullGameRuntimeExecuted"] = false,
            ["playability"] = "not-tested",
            ["scope"] = "plugin inventory, local game JSON and limited script execution; no complete RPG Maker core boot",
            ["notCovered"] = new[] { "RPG Maker core boot", "maps/rendering", "audio", "input", "persistent saves", "Node/native dependencies" },
            ["security"] = "In-process Jint constraints are not an operating-system sandbox. Execute only projects you trust.",
        };
        try
        {
            var options = ReadOptions(OS.GetCmdlineUserArgs());
            exitCode = Run(options, report);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException and not StackOverflowException)
        {
            report["status"] = "failed";
            report["error"] = exception.GetType().Name + ": " + exception.Message;
        }
        try
        {
            // App-owned, uniquely named report. Never write inside the game.
            var directory = ProjectSettings.GlobalizePath("user://web-plugin-probes");
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "probe-" + Guid.NewGuid().ToString("N") + ".json");
            using (var stream = new FileStream(path, FileMode.CreateNew, System.IO.FileAccess.Write))
            {
                JsonSerializer.Serialize(stream, report, new JsonSerializerOptions { WriteIndented = true });
            }
            GD.Print("UniversalRPG MV/MZ probe report: " + path);
            GD.Print("Probe scope: script initialization only; main.js and full game playability were NOT tested.");
            if (exitCode == 0 && report.TryGetValue("status", out var status) && Equals(status, "subset-passed"))
                GD.Print("UniversalRPG web plugin subset passed.");
            if (exitCode == 0 && report.TryGetValue("status", out var coreStatus) && Equals(coreStatus, "core-scripts-passed"))
                GD.Print("UniversalRPG original core scripts initialized; entry point and playability NOT tested.");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            GD.PushError("Could not write probe report: " + exception.Message);
            exitCode = 2;
        }
        GetTree().Quit(exitCode);
    }

    private static int Run(Options options, Dictionary<string, object?> report)
    {
        report["requestedEngine"] = options.Mz ? "MZ" : "MV";
        report["executionRequested"] = options.Execute;
        report["coreMode"] = options.Core;
        report["scope"] = options.Core
            ? "Original project libraries/core/config plus plugin initialization; main.js/scene boot NOT executed"
            : "Isolated plugin/data subset; no original core execution";
        report["completedPluginBootstrap"] = false;
        report["completedFrames"] = 0;
        report["requestedFrames"] = options.Frames;
        report["frameMilliseconds"] = options.StepMilliseconds;
        var phases = new List<object>();
        report["phases"] = phases;
        using var content = new DirectoryGameContentSource(options.Game, pMaxFileBytes: 16L * 1024 * 1024);
        var coreName = options.Mz ? "rmmz_core.js" : "rpg_core.js";
        if (!content.Exists("js/" + coreName) && !content.Exists("www/js/" + coreName))
        {
            report["status"] = "blocked";
            report["error"] = "The selected generation's expected core script was not found; no code was executed.";
            return 2;
        }
        // Select one root for both native data and the existing script inventory.
        // Mixing a root export with a second www export would run the wrong code.
        var rootCore = content.Exists("js/" + coreName);
        var wwwCore = content.Exists("www/js/" + coreName);
        if ((rootCore && wwwCore)
            || (rootCore && content.Exists("www/js/plugins.js"))
            || (wwwCore && content.Exists("js/plugins.js")))
        {
            report["status"] = "blocked";
            report["error"] = "Ambiguous root/www projects; select a single exported game folder.";
            return 2;
        }
        var contentPrefix = rootCore ? "" : "www";
        report["dataRoot"] = contentPrefix;
        report["nativeDataScope"] = "Read-only local data/*.json; no network, script writes or whole-browser execution.";
        var inventory = WebScriptInventory.Inspect(options.Game, options.Mz);
        report["inventoryDiagnostics"] = inventory.Diagnostics.Select(d => new
        {
            severity = d.Severity.ToString(), code = d.Code, message = d.Message,
        }).ToArray();
        // Deliberately omit source text, plugin parameters, encryption keys and
        // the absolute game directory from reports.
        report["plugins"] = inventory.Entries.Select(entry => new
        {
            id = entry.Script.Id, name = entry.Script.DisplayName, enabled = entry.Enabled,
            order = entry.Script.LoadOrder, relativePath = entry.Script.RelativePath,
            sha256 = entry.Script.Sha256, classification = entry.Compatibility.ToString(),
            reasons = entry.Reasons,
        }).ToArray();
        var incomplete = inventory.Diagnostics.Any(d => d.Severity != SdkDiagnosticSeverity.Info
            || d.Code == "scripts.plugins-config-missing");
        report["inspectionComplete"] = !incomplete;
        var language = options.Mz ? ScriptLanguageIds.RpgMakerMzJavaScript : ScriptLanguageIds.RpgMakerMvJavaScript;
        NativeCoreScriptSet? core = null;
        if (options.Core)
        {
            core = NativeCoreScriptSet.Read(content, language, contentPrefix);
            if (!core.Success)
            {
                report["status"] = "core-manifest-blocked";
                report["error"] = core.Error;
                return 2;
            }
            report["coreScripts"] = core.Modules.Select(module => new
            {
                id = module.Descriptor.Id, path = module.Descriptor.RelativePath,
                sha256 = module.Descriptor.Sha256, bytes = module.Source.Length,
            }).ToArray();
            report["indexSha256"] = core.IndexSha256;
            report["deferredEntryPoint"] = new { path = "js/main.js", sha256 = core.EntryPointSha256 };
        }
        if (!options.Execute)
        {
            report["status"] = incomplete ? "inspection-incomplete" : "inspection-only";
            return incomplete ? 2 : 0;
        }
        if (incomplete)
        {
            report["status"] = "blocked";
            report["error"] = "Inspection produced warnings/errors or no plugin configuration. No code was executed.";
            return 2;
        }
        var entries = WebPluginLoadPlan.SelectEnabled(inventory.Entries);
        report["selectedPluginCount"] = entries.Count;
        if (entries.Count == 0 && !options.Core)
        {
            report["status"] = "nothing-to-execute";
            return 3;
        }
        IEnumerable<ScriptModule> preludes = options.Core
            ? core!.Modules.Concat(new[] { NativeCorePluginSetup.Build(language, entries) })
            : new[] { WebPluginManagerShimBuilder.Build(language, entries) };
        using var vm = new JintEmbeddedScriptVm(language, content, contentPrefix);
        using var runtime = new WebScriptRuntime(language, vm, new GameContentWebScriptSourceProvider(content), entries,
            preludes, new WebBrowserHostOptions());
        var policy = new ScriptExecutionPolicy
        {
            MaxMemoryMegabytes = 128, MaxExecutionMillisecondsPerTick = 250, MaxCallDepth = 128,
            // The installed adapter permits local JSON reads only. No write/network grant.
            AllowReadGameFiles = true, AllowWriteSaveFiles = false, AllowWriteCacheFiles = false,
        };
        bool Record(string name, SdkOperationResult result)
        {
            phases.Add(new { phase = name, success = result.Success, code = result.ErrorCode, message = result.ErrorMessage });
            if (!result.Success) report["status"] = options.Core ? "core-scripts-failed" : "subset-failed";
            return result.Success;
        }
        if (!Record("load", runtime.LoadScripts(policy))) return 1;
        if (!Record("bootstrap", runtime.ExecuteBootstrap())) return 1;
        report["completedPluginBootstrap"] = true;
        report["originalCoreScriptsCompleted"] = options.Core;
        for (var frame = 0; frame < options.Frames; frame++)
        {
            if (!Record("frame:" + frame, runtime.AdvanceFrame(options.StepMilliseconds / 1000.0))) return 1;
            report["completedFrames"] = frame + 1;
        }
        report["status"] = options.Core ? "core-scripts-passed" : "subset-passed";
        // This only proves the enabled plugin startup plus the requested frames
        // in this deliberately limited host, NOT a functioning MV/MZ game.
        return 0;
    }

    private sealed record Options(string Game, bool Mz, bool Execute, bool Core, int Frames, double StepMilliseconds);

    private static Options ReadOptions(string[] args)
    {
        string? game = null;
        string? engine = null;
        var execute = false;
        var core = false;
        var frames = 3;
        var step = 1000.0 / 60.0;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < args.Length; i++)
        {
            var name = args[i];
            if (!seen.Add(name)) throw new ArgumentException("Repeated option: " + name);
            if (name is "--execute-plugins" or "--execute-core" or "--inspect-core")
            {
                if (seen.Count(option => option is "--execute-plugins" or "--execute-core" or "--inspect-core") > 1)
                    throw new ArgumentException("Choose exactly one inspection/execution mode.");
                execute = name != "--inspect-core";
                core = name != "--execute-plugins";
                continue;
            }
            if (name is not ("--game" or "--engine" or "--frames" or "--step-ms"))
                throw new ArgumentException("Unknown option: " + name);
            if (++i >= args.Length) throw new ArgumentException("Missing value for " + name);
            var value = args[i];
            switch (name)
            {
                case "--game": game = value; break;
                case "--engine": engine = value.ToLowerInvariant(); break;
                case "--frames":
                    if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out frames) || frames is < 1 or > 600)
                        throw new ArgumentException("--frames must be 1..600.");
                    break;
                case "--step-ms":
                    if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out step)
                        || !double.IsFinite(step) || step <= 0 || step > 1000)
                        throw new ArgumentException("--step-ms must be finite, greater than 0 and at most 1000.");
                    break;
            }
        }
        if (string.IsNullOrWhiteSpace(game) || engine is not ("mv" or "mz"))
            throw new ArgumentException("Usage: --game <folder> --engine mv|mz [--execute-plugins | --inspect-core | --execute-core] [--frames 3] [--step-ms 16.6666667]");
        if (!Directory.Exists(game)) throw new DirectoryNotFoundException("--game must select an existing game folder, not an archive.");
        return new Options(Path.GetFullPath(game), engine == "mz", execute, core, frames, step);
    }
}
