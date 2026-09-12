using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using Godot;
using UniversalRPG.JavaScript.Jint;
using UniversalRPG.Plugins;
using UniversalRPG.Sdk;

namespace UniversalRPG.Tools;

/// <summary>
/// Developer-only first-failure probe. It executes original core/config/plugins,
/// original main.js and window load. It is not a renderer/game runner; missing
/// graphics/audio/platform services remain explicit failures. Save/config data
/// is intentionally ephemeral in this probe and never touches a user's saves.
/// </summary>
public partial class NativeMvMzBootProbe : Node
{
    public override void _Ready()
    {
        var report = new Dictionary<string, object?>
        {
            ["schema"] = "urpg.native-mv-mz-boot-probe.v1",
            ["utc"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            ["fullGameRuntimeExecuted"] = false, ["playability"] = "not-tested",
            ["entryPointExecuted"] = false, ["windowLoadDispatched"] = false, ["framesCompleted"] = 0,
            ["storage"] = "ephemeral-memory; user save/config files are not read or written by this probe",
        };
        var exit = 2;
        try { exit = Run(Parse(OS.GetCmdlineUserArgs()), report); }
        catch (Exception exception) when (exception is not OutOfMemoryException and not StackOverflowException)
        { report["status"] = "probe-failed"; report["error"] = exception.GetType().Name + ": " + exception.Message; }
        try
        {
            var directory = ProjectSettings.GlobalizePath("user://native-mv-mz-boot-probes"); Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "probe-" + Guid.NewGuid().ToString("N") + ".json");
            using var stream = new FileStream(path, FileMode.CreateNew, System.IO.FileAccess.Write);
            JsonSerializer.Serialize(stream, report, new JsonSerializerOptions { WriteIndented = true });
            GD.Print("UniversalRPG native MV/MZ boot probe: " + path);
            if (report.TryGetValue("status", out var status)) GD.Print("Boot probe status: " + status);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        { GD.PushError("Could not write boot-probe report: " + exception.Message); exit = 2; }
        GetTree().Quit(exit);
    }

    private static int Run(Options options, Dictionary<string, object?> report)
    {
        report["engine"] = options.Mz ? "MZ" : "MV"; report["requestedFrames"] = options.Frames; report["stepMilliseconds"] = options.StepMilliseconds;
        var phases = new List<object>(); report["phases"] = phases;
        bool Phase(string name, SdkOperationResult result)
        {
            phases.Add(new { phase=name, success=result.Success, code=result.ErrorCode, message=result.ErrorMessage });
            if (!result.Success)
            {
                report["status"] = name switch { "entry"=>"entry-point-failed", "pre-load-frame"=>"entry-callbacks-failed", "window-load"=>"platform-startup-failed", _=>"core-startup-failed" };
                report["firstFailurePhase"] = name; report["firstFailureCode"] = result.ErrorCode; report["firstFailureMessage"] = result.ErrorMessage;
            }
            return result.Success;
        }

        using var content = new DirectoryGameContentSource(options.Game, pMaxFileBytes:16L*1024*1024);
        var coreName=options.Mz?"rmmz_core.js":"rpg_core.js"; var rootCore=content.Exists("js/"+coreName); var wwwCore=content.Exists("www/js/"+coreName);
        if (rootCore==wwwCore) { report["status"]="project-root-ambiguous"; return 2; }
        var prefix=rootCore?"":"www"; report["contentRoot"]=prefix;
        var language=options.Mz?ScriptLanguageIds.RpgMakerMzJavaScript:ScriptLanguageIds.RpgMakerMvJavaScript;
        var core=NativeCoreScriptSet.Read(content,language,prefix);
        if(!core.Success||core.EntryPoint==null){report["status"]="manifest-blocked";report["error"]=core.Error;return 2;}
        var inventory=WebScriptInventory.Inspect(options.Game,options.Mz);
        if(inventory.Diagnostics.Any(d=>d.Severity!=SdkDiagnosticSeverity.Info)){report["status"]="plugin-inspection-blocked";report["diagnostics"]=inventory.Diagnostics.Select(d=>new{severity=d.Severity.ToString(),d.Code,d.Message}).ToArray();return 2;}
        var entries=WebPluginLoadPlan.SelectEnabled(inventory.Entries);
        report["coreFiles"]=core.Modules.Select(m=>new{path=m.Descriptor.RelativePath,sha256=m.Descriptor.Sha256}).ToArray();
        report["entryPoint"]=new{path=core.EntryPoint.Descriptor.RelativePath,sha256=core.EntryPoint.Descriptor.Sha256};
        report["plugins"]=entries.Select(e=>new{name=e.Script.DisplayName,path=e.Script.RelativePath,sha256=e.Script.Sha256}).ToArray();

        string StripRoot(string path){if(prefix.Length==0)return path;var marker=prefix+"/";return path.StartsWith(marker,StringComparison.OrdinalIgnoreCase)?path[marker.Length..]:path;}
        var lifecycle=NativeEntryPointHostPrelude.Build(language,core.Modules.Select(m=>StripRoot(m.Descriptor.RelativePath)));
        var preludes=new[]{lifecycle,NativeGamepadHostPrelude.Build(language)}.Concat(core.Modules).Concat(new[]{NativeCorePluginSetup.Build(language,entries)});
        using var storage=new MemoryScriptKeyValueStorage("native-boot-probe");
        using var vm=new JintEmbeddedScriptVm(language,content,prefix,storage);
        using var runtime=new WebScriptRuntime(language,vm,new GameContentWebScriptSourceProvider(content),entries,preludes,new WebBrowserHostOptions(),core.EntryPoint);
        var policy=new ScriptExecutionPolicy{MaxMemoryMegabytes=192,MaxExecutionMillisecondsPerTick=500,MaxCallDepth=192,AllowReadGameFiles=true,AllowWriteSaveFiles=true,AllowWriteCacheFiles=false};
        if(!Phase("load",runtime.LoadScripts(policy)))return 1;
        if(!Phase("core-bootstrap",runtime.ExecuteBootstrap()))return 1;
        if(!Phase("entry",runtime.ExecuteEntryPoint()))return 1; report["entryPointExecuted"]=true;
        if(!Phase("pre-load-frame",runtime.AdvanceFrame(0)))return 1;
        if(!Phase("window-load",runtime.DispatchWindowLoad()))return 1; report["windowLoadDispatched"]=true;
        for(var frame=0;frame<options.Frames;frame++){if(!Phase("frame:"+frame,runtime.AdvanceFrame(options.StepMilliseconds/1000.0)))return 1;report["framesCompleted"]=frame+1;}
        report["status"]="entry-lifecycle-passed";report["note"]="main.js/load completed in the current host subset; rendering/game playability still require explicit verification.";return 0;
    }

    private sealed record Options(string Game,bool Mz,int Frames,double StepMilliseconds);
    private static Options Parse(string[] args)
    {
        string? game=null;string? engine=null;var execute=false;var frames=3;var step=1000.0/60.0;var seen=new HashSet<string>(StringComparer.Ordinal);
        for(var i=0;i<args.Length;i++){var name=args[i];if(!seen.Add(name))throw new ArgumentException("Repeated option: "+name);if(name=="--execute-entry"){execute=true;continue;}if(name is not ("--game" or "--engine" or "--frames" or "--step-ms"))throw new ArgumentException("Unknown option: "+name);if(++i>=args.Length)throw new ArgumentException("Missing value for "+name);var value=args[i];switch(name){case "--game":game=value;break;case "--engine":engine=value.ToLowerInvariant();break;case "--frames":if(!int.TryParse(value,NumberStyles.None,CultureInfo.InvariantCulture,out frames)||frames is < 0 or > 600)throw new ArgumentException("--frames must be 0..600.");break;case "--step-ms":if(!double.TryParse(value,NumberStyles.Float,CultureInfo.InvariantCulture,out step)||!double.IsFinite(step)||step<=0||step>1000)throw new ArgumentException("--step-ms must be finite and in (0,1000].");break;}}
        if(!execute)throw new ArgumentException("This tool executes trusted project scripts; pass --execute-entry explicitly.");
        if(string.IsNullOrWhiteSpace(game)||engine is not ("mv" or "mz"))throw new ArgumentException("Usage: --game <folder> --engine mv|mz --execute-entry [--frames N]");
        if(!Directory.Exists(game))throw new DirectoryNotFoundException("--game must be an existing folder.");
        return new Options(Path.GetFullPath(game),engine=="mz",frames,step);
    }
}
