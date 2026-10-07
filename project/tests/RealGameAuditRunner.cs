using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Godot;
using UniversalRPG.App.Launcher;
using UniversalRPG.App.Library;
using UniversalRPG.Plugins;

namespace UniversalRPG.Tests;

/// <summary>Opt-in, data-only audit of a caller-selected game library.</summary>
public partial class RealGameAuditRunner : Node
{
    public override void _Ready()
    {
        var root = Argument("--games-root=");
        var output = Argument("--audit-report=");
        var explicitLcfEngine = Argument("--lcf-engine=");
        if (explicitLcfEngine != null && explicitLcfEngine != EnginePluginIds.RpgMaker2000
            && explicitLcfEngine != EnginePluginIds.RpgMaker2003)
        {
            GD.PushError("--lcf-engine must name rpg-maker-2000 or rpg-maker-2003.");
            GetTree().Quit(2);
            return;
        }
        if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root)
            || string.IsNullOrWhiteSpace(output))
        {
            GD.PushError("Supply an existing --games-root= and an --audit-report= path.");
            GetTree().Quit(2);
            return;
        }
        var scratch = ProjectSettings.GlobalizePath("user://real-game-audit/" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(scratch);
        using var library = new GameLibrary(pSettingsPath: Path.Combine(scratch, "library.cfg"));
        using var launcher = new RuntimeLauncher();
        var entries = new List<object>();
        try
        {
            library.SetRootPath(root, false);
            foreach (var game in library.Scan())
            {
                if (game.Detection.Report.IsAmbiguous && explicitLcfEngine != null
                    && !library.TrySelectEngine(game, explicitLcfEngine, out var selectionError, false))
                    throw new InvalidOperationException(selectionError);
                var support = launcher.GetSupport(game);
                RuntimeLauncher.LaunchResult? launch = null;
                string? updateError = null;
                string? wait = null;
                var successfulFrames = 0;
                var acknowledgedMessages = 0;
                string? rasterProbe = null;
                if (support.State == RuntimeLauncher.SupportState.Available)
                {
                    launch = launcher.Launch(game);
                    if (launch.Success)
                    {
                        for (var frame = 0; frame < 180; frame++)
                        {
                            var update = launcher.Update(1.0 / 60.0);
                            if (!update.Success)
                            {
                                updateError = update.Error?.Message ?? "Unknown update failure.";
                                break;
                            }
                            successfulFrames++;
                            if (launcher.ActiveRuntime is Rm2kEngineRuntime runtime)
                            {
                                wait = runtime.Simulation.WaitingFor.ToString();
                                if (runtime.Presentation.MessageVisible)
                                {
                                    runtime.DrueckeFort();
                                    acknowledgedMessages++;
                                }
                                if (runtime.Presentation.ActiveChoice != null
                                    || runtime.Presentation.PendingInputVariableId != null) break;
                            }
                        }
                        if (launcher.ActiveRuntime is Rm2kEngineRuntime rasterRuntime)
                        {
                            rasterProbe = ProbeRaster(rasterRuntime);
                        }
                    }
                    launcher.Shutdown();
                }
                entries.Add(new
                {
                    game.Title, game.Path,
                    engine = game.Detection.Engine.ToString(),
                    plugin = game.SelectedPluginId,
                    explicitEngineChoice = game.ExplicitPluginId,
                    compatibility = game.CompatibilityStatus.ToString(),
                    runtimeSupport = support.State.ToString(),
                    supportReason = support.Reason,
                    launchSuccess = launch?.Success,
                    launchMessage = launch?.Message,
                    successfulFrames, acknowledgedMessages, wait, updateError,
                    rasterProbe,
                    rtpDependency = game.Detection.RtpDependency,
                });
            }
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
            File.WriteAllText(output, JsonSerializer.Serialize(new
            {
                gamesRoot = Path.GetFullPath(root),
                boundary = "No imported EXE, DLL, Ruby, JavaScript, installer or native plugin executed. Native UniversalRPG runtime only; this is not a complete playthrough.",
                framesPerGameLimit = 180,
                requestedLcfEngine = explicitLcfEngine,
                gamesFound = entries.Count,
                games = entries,
            }, new JsonSerializerOptions { WriteIndented = true }));
            GD.Print($"Real-game audit: {entries.Count} entries; report: {output}");
            GetTree().Quit(entries.Count == 0 ? 3 : 0);
        }
        catch (Exception exception)
        {
            GD.PushError("Real-game audit failed: " + exception.Message);
            GetTree().Quit(1);
        }
        finally
        {
            launcher.Shutdown();
            Directory.Delete(scratch, true);
        }
    }

    /// <summary>
    /// The rendered map as the runtime produced it: null, size and the most
    /// frequent colours. A playability claim needs this probe, because a
    /// runtime that runs 180 frames and renders nothing is not playable.
    /// </summary>
    private static string ProbeRaster(Rm2kEngineRuntime pRuntime)
    {
        var map = pRuntime.RenderedMap;
        if (map == null)
        {
            return "map=null diagnostic=" + pRuntime.RenderDiagnostic;
        }
        var histogram = new Dictionary<string, int>();
        var pixels = map.Pixels;
        for (var index = 0; index + 3 < pixels.Length; index += 4)
        {
            var key = $"{pixels[index]:X2}{pixels[index + 1]:X2}{pixels[index + 2]:X2}{pixels[index + 3]:X2}";
            histogram.TryGetValue(key, out var count);
            histogram[key] = count + 1;
        }
        var top = new List<string>();
        foreach (var pair in System.Linq.Enumerable.OrderByDescending(histogram, pEntry => pEntry.Value).Take(8))
        {
            top.Add(pair.Key + ":" + pair.Value);
        }
        var opaque = 0;
        for (var index = 3; index < pixels.Length; index += 4)
        {
            if (pixels[index] != 0)
            {
                opaque++;
            }
        }
        return $"map={map.Width}x{map.Height} opaquePixels={opaque}/{map.Width * map.Height}"
            + $" top={string.Join(", ", top)} diagnostic={pRuntime.RenderDiagnostic}";
    }

    private static string? Argument(string pPrefix)
    {
        foreach (var argument in OS.GetCmdlineUserArgs())
            if (argument.StartsWith(pPrefix, StringComparison.Ordinal)) return argument[pPrefix.Length..];
        return null;
    }
}
