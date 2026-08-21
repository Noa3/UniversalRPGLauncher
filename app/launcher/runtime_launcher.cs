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

	public RuntimeLauncher()
		: this(BuiltInEnginePluginCatalog.CreateRuntimeRegistry())
	{
	}

	public RuntimeLauncher(EnginePluginRegistry pRegistry)
	{
		_registry = pRegistry ?? throw new ArgumentNullException(nameof(pRegistry));
		_selector = new EngineRuntimeSelector(_registry);
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
		if (pGame == null)
		{
			return Failure(PluginError.Create(
				PluginErrorCode.InvalidGame,
				"No game was selected.",
				pPhase: "select"));
		}
		var selection = _selector.Select(pGame.Detection.Report, GetCurrentPlatform());
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
		var started = _activeHost.Start(selection.Value.Game);
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
