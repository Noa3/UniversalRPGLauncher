using System;
using System.Collections.Generic;
using Godot;
using UniversalRPG.App.Library;
using UniversalRPG.GameDetectorNs;
using UniversalRPG.Plugins;

namespace UniversalRPG.App.Launcher;

public partial class RuntimeLauncher : RefCounted
{
	public enum SupportState
	{
		Unavailable,
		Experimental,
		Available,
	}

	public class SupportInfo
	{
		public SupportState State { get; init; }
		public string Label { get; init; } = "";
		public string Reason { get; init; } = "";
		public string PluginId { get; init; } = "";
		public PluginErrorCode? ErrorCode { get; init; }
		public IReadOnlyList<PluginDiagnostic> Diagnostics { get; init; } = Array.Empty<PluginDiagnostic>();
	}

	public class LaunchResult
	{
		public bool Success { get; init; }
		public string Message { get; init; } = "";
		public string PluginId { get; init; } = "";
		public PluginErrorCode? ErrorCode { get; init; }
		public string Phase { get; init; } = "";
		public IReadOnlyList<PluginDiagnostic> Diagnostics { get; init; } = Array.Empty<PluginDiagnostic>();
	}

	private readonly EnginePluginRegistry _registry;
	private readonly EngineRuntimeSelector _selector;
	private EnginePluginHost? _activeHost;

	public IEngineRuntime? ActiveRuntime => _activeHost?.Runtime;
	public PluginRuntimeState ActiveRuntimeState => _activeHost?.State ?? PluginRuntimeState.NotStarted;

	/// <summary>
	/// Whether the next launch should present the project's title screen.
	/// </summary>
	/// <remarks>
	/// <strong>And the window sets this, because the window is the caller
	/// that shows a player a screen.</strong> Measured: every game at hand
	/// names a title -- Camellia <c>menu_page</c>, LegalTruck
	/// <c>Castle</c>, Skies <c>SkieTitle</c> -- so a launcher that never
	/// asked would never show any of them, and a player who starts a game
	/// would land on a map with no front page.
	/// </remarks>
	public bool PresentTitleScreen { get; set; }

	public RuntimeLauncher()
		: this(BuiltInEnginePluginCatalog.CreateRuntimeRegistry())
	{
	}

	public RuntimeLauncher(EnginePluginRegistry pRegistry)
	{
		_registry = pRegistry ?? throw new ArgumentNullException(nameof(pRegistry));
		_selector = new EngineRuntimeSelector(_registry);
	}

	public PluginOperationResult Update(double pDeltaSeconds)
	{
		return _activeHost?.Update(pDeltaSeconds) ?? PluginOperationResult.Failed(PluginError.Create(
			PluginErrorCode.InvalidLifecycleTransition,
			"No runtime is active.",
			pPhase: "update"));
	}

	public PluginOperationResult Stop()
	{
		return _activeHost?.Stop() ?? PluginOperationResult.Succeeded();
	}

	public void Shutdown()
	{
		_activeHost?.Dispose();
		_activeHost = null;
	}

	public SupportInfo GetSupport(GameLibrary.GameEntry pGame)
	{
		return GetSupport(pGame, GetCurrentPlatform());
	}

	public SupportInfo GetSupport(GameLibrary.GameEntry pGame, string pPlatform)
	{
		if (pGame == null)
		{
			return Unavailable(
				"",
				PluginErrorCode.InvalidGame,
				"No game was selected.",
				"select");
		}
		return GetSupport(pGame.Detection.Report, pPlatform);
	}

	/// <summary>
	/// Compatibility overload for callers that only have the legacy enum. New
	/// import/launch paths must pass the complete detection report instead.
	/// </summary>
	public SupportInfo GetSupport(GameDetector.EngineType pEngine)
	{
		var pluginId = EnginePluginIds.FromDetectorEngine(pEngine);
		if (string.IsNullOrEmpty(pluginId))
		{
			return Unavailable("", PluginErrorCode.NoMatchingPlugin, Tr("RUNTIME_UNSUPPORTED_REASON"), "select");
		}
		var report = new EngineDetectionReport
		{
			SourcePath = "legacy://engine-enum",
			SelectedCandidate = new EngineDetectionCandidate
			{
				PluginId = pluginId,
				EngineId = pluginId,
				DisplayName = pluginId,
				Status = EngineDetectionStatus.DetectionOnly,
				Score = 1000,
				Reason = "Legacy engine enum compatibility request.",
			},
			Candidates = Array.Empty<EngineDetectionCandidate>(),
		};
		return GetSupport(report, GetCurrentPlatform());
	}

	public SupportInfo GetSupport(EngineDetectionReport pReport, string pPlatform)
	{
		if (pReport == null)
		{
			return Unavailable("", PluginErrorCode.InvalidGame, "No detection report was provided.", "select");
		}
		var selection = _selector.Select(pReport, pPlatform);
		if (!selection.Success || selection.Value == null)
		{
			var error = selection.Error;
			return Unavailable(
				pReport.SelectedCandidate?.PluginId ?? "",
				error?.Code ?? PluginErrorCode.NoMatchingPlugin,
				error?.Message ?? Tr("RUNTIME_NOT_REGISTERED"),
				error?.Phase ?? "select",
				selection.Diagnostics);
		}
		return new SupportInfo
		{
			State = SupportState.Available,
			Label = "Runtime available",
			Reason = "The selected plugin passed detection, capability, platform, and compatibility checks.",
			PluginId = selection.Value.Plugin.Metadata.Id,
			Diagnostics = selection.Diagnostics,
		};
	}

	public LaunchResult Launch(GameLibrary.GameEntry pGame)
	{
		return Launch(pGame, GetCurrentPlatform());
	}

	/// <summary>
	/// Launches a game on a platform the caller names.
	/// </summary>
	/// <remarks>
	/// <strong>And the caller names the platform, because the caller is the
	/// one that may ask Godot.</strong> The window loads a project off the
	/// main thread: a project with 1.6 GiB of images takes seconds to read,
	/// and a window that stops answering is what a player calls a hang.
	/// <c>OS.GetName()</c> is a Godot call and does not belong on a worker,
	/// <strong>so the window reads it first and passes it in.</strong>
	/// <c>EnginePluginHost</c> and both runtimes are plain .NET classes with
	/// no Godot types in them, which is what makes the load itself safe
	/// there.
	/// </remarks>
	public LaunchResult Launch(GameLibrary.GameEntry pGame, string pPlatform)
	{
		if (pGame == null)
		{
			return Failure(PluginError.Create(
				PluginErrorCode.InvalidGame,
				"No game was selected.",
				pPhase: "select"));
		}
		var selection = _selector.Select(pGame.Detection.Report, pPlatform);
		if (!selection.Success || selection.Value == null)
		{
			var error = selection.Error ?? PluginError.Create(
				PluginErrorCode.NoMatchingPlugin,
				Tr("RUNTIME_NOT_REGISTERED"),
				pPhase: "select");
			return Failure(error, selection.Diagnostics);
		}

		_activeHost?.Dispose();
		_activeHost = new EnginePluginHost(_registry);

		// **And the title is asked for here, where a window is about to be
		// shown.** The selection's own game info is what every other caller
		// uses, and it is not mutated: a copy carries the flag, so a caller
		// that measures a map keeps the map.
		var spiel = selection.Value.Game;
		if (PresentTitleScreen && !spiel.PresentTitleScreen)
		{
			spiel = new PluginGameInfo
			{
				GameDirectory = spiel.GameDirectory,
				EngineId = spiel.EngineId,
				Generation = spiel.Generation,
				EngineVersion = spiel.EngineVersion,
				DetectorScore = spiel.DetectorScore,
				Evidence = spiel.Evidence,
				PresentTitleScreen = true,
			};
		}
		var started = _activeHost.Start(spiel);
		if (!started.Success)
		{
			return Failure(started.Error ?? PluginError.Create(
				PluginErrorCode.LifecycleFailure,
				"The selected runtime failed to start.",
				selection.Value.Plugin.Metadata.Id,
				"start"), started.Diagnostics);
		}
		return new LaunchResult
		{
			Success = true,
			Message = "Runtime started.",
			PluginId = selection.Value.Plugin.Metadata.Id,
			Diagnostics = started.Diagnostics,
		};
	}

	private SupportInfo Unavailable(
		string pPluginId,
		PluginErrorCode pCode,
		string pReason,
		string pPhase,
		IReadOnlyList<PluginDiagnostic>? pDiagnostics = null)
	{
		return new SupportInfo
		{
			State = SupportState.Unavailable,
			Label = pCode == PluginErrorCode.UnsupportedEngine
				? Tr("RUNTIME_PLANNED_LABEL")
				: Tr("RUNTIME_UNSUPPORTED_LABEL"),
			Reason = $"[{pCode}/{pPhase}] {pReason}",
			PluginId = pPluginId,
			ErrorCode = pCode,
			Diagnostics = pDiagnostics ?? Array.Empty<PluginDiagnostic>(),
		};
	}

	private static LaunchResult Failure(PluginError pError, IReadOnlyList<PluginDiagnostic>? pDiagnostics = null)
	{
		return new LaunchResult
		{
			Success = false,
			Message = $"[{pError.Code}/{pError.Phase}] {pError.Message}",
			PluginId = pError.PluginId,
			ErrorCode = pError.Code,
			Phase = pError.Phase,
			Diagnostics = pDiagnostics ?? Array.Empty<PluginDiagnostic>(),
		};
	}

	private static string GetCurrentPlatform()
	{
		return OS.GetName().ToLowerInvariant();
	}
}
