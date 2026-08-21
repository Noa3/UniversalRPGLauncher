using System;
using System.Collections.Generic;
using System.Linq;

namespace UniversalRPG.Plugins;

/// <summary>
/// The result of deterministic plugin probing. Reports are retained so callers
/// can explain both the selected plugin and rejected candidates.
/// </summary>
public sealed class PluginProbeReport
{
	public string PluginId { get; init; } = "";
	public bool Supported { get; init; }
	public bool Matched { get; init; }
	public int Score { get; init; }
	public string Reason { get; init; } = "";
	public PluginError? Error { get; init; }
	public IReadOnlyList<PluginDiagnostic> Diagnostics { get; init; } = Array.Empty<PluginDiagnostic>();
}

public sealed class EnginePluginSelection
{
	public EnginePluginSelection(
		IEnginePlugin pPlugin,
		PluginGameInfo pGame,
		PluginProbeResult pProbe,
		IReadOnlyList<PluginProbeReport> pReports
	)
	{
		Plugin = pPlugin ?? throw new ArgumentNullException(nameof(pPlugin));
		Game = pGame ?? throw new ArgumentNullException(nameof(pGame));
		Probe = pProbe ?? throw new ArgumentNullException(nameof(pProbe));
		Reports = pReports ?? throw new ArgumentNullException(nameof(pReports));
	}

	public IEnginePlugin Plugin { get; }
	public PluginGameInfo Game { get; }
	public PluginProbeResult Probe { get; }
	public IReadOnlyList<PluginProbeReport> Reports { get; }
}

/// <summary>
/// Explicit registry for trusted, in-process engine plugins. It never loads an
/// assembly, executable, DLL, script, or other user-provided binary from a game.
/// Callers register plugin instances compiled into the application.
/// </summary>
public sealed class EnginePluginRegistry
{
	private readonly Dictionary<string, IEnginePlugin> _plugins = new(StringComparer.Ordinal);

	public IReadOnlyList<IEnginePlugin> Plugins => _plugins.Values
		.OrderBy(pPlugin => pPlugin.Metadata.Id, StringComparer.Ordinal)
		.ToArray();

	public PluginOperationResult Register(IEnginePlugin pPlugin)
	{
		if (pPlugin == null)
		{
			return PluginOperationResult.Failed(PluginError.Create(
				PluginErrorCode.InvalidMetadata,
				"A null plugin cannot be registered."
			));
		}
		var metadata = pPlugin.Metadata;
		if (metadata == null)
		{
			return PluginOperationResult.Failed(PluginError.Create(
				PluginErrorCode.InvalidMetadata,
				"A plugin must expose metadata before registration."
			));
		}
		var validation = metadata.Validate();
		if (!validation.Success)
		{
			return validation;
		}
		if (_plugins.ContainsKey(metadata.Id))
		{
			return PluginOperationResult.Failed(PluginError.Create(
				PluginErrorCode.DuplicatePluginId,
				$"Plugin ID '{metadata.Id}' is already registered.",
				metadata.Id,
				"register"
			));
		}
		_plugins.Add(metadata.Id, pPlugin);
		return PluginOperationResult.Succeeded(new[]
		{
			PluginDiagnostic.Info("plugin.registered", $"Registered in-process plugin '{metadata.Id}'.", metadata.Id),
		});
	}

	public bool Unregister(string pPluginId)
	{
		return !string.IsNullOrEmpty(pPluginId) && _plugins.Remove(pPluginId);
	}

	public bool TryGet(string pPluginId, out IEnginePlugin? pPlugin)
	{
		if (string.IsNullOrEmpty(pPluginId))
		{
			pPlugin = null;
			return false;
		}
		return _plugins.TryGetValue(pPluginId, out pPlugin);
	}

	public bool HasSupport(string pEngineId)
	{
		if (string.IsNullOrEmpty(pEngineId))
		{
			return false;
		}
		foreach (var plugin in _plugins.Values)
		{
			foreach (var range in plugin.Metadata.SupportedEngines)
			{
				if (range != null && range.EngineId.Equals(pEngineId, StringComparison.Ordinal))
				{
					return true;
				}
			}
		}
		return false;
	}

	public PluginResult<EnginePluginSelection> Select(PluginGameInfo pGame)
	{
		if (pGame == null)
		{
			return PluginResult<EnginePluginSelection>.Failed(PluginError.Create(
				PluginErrorCode.InvalidGame,
				"A game description is required for plugin selection."
			));
		}
		var gameValidation = pGame.Validate();
		if (!gameValidation.Success)
		{
			return PluginResult<EnginePluginSelection>.Failed(gameValidation.Error!);
		}

		var supportedPlugins = new List<IEnginePlugin>();
		foreach (var plugin in Plugins)
		{
			if (plugin.Metadata.Supports(pGame))
			{
				supportedPlugins.Add(plugin);
			}
		}
		if (supportedPlugins.Count == 0)
		{
			return PluginResult<EnginePluginSelection>.Failed(PluginError.Create(
				PluginErrorCode.UnsupportedEngine,
				$"No registered plugin supports engine '{pGame.EngineId}'.",
				pPhase: "select"
			));
		}

		var candidates = new List<(IEnginePlugin Plugin, PluginProbeResult Probe)>();
		var reports = new List<PluginProbeReport>();
		PluginError? firstFailure = null;
		foreach (var plugin in supportedPlugins)
		{
			PluginProbeResult? probe;
			try
			{
				probe = plugin.Probe(new EnginePluginProbeContext(pGame));
			}
			catch (Exception exception)
			{
				var error = PluginError.Create(
					PluginErrorCode.ProbeFailed,
					$"Plugin '{plugin.Metadata.Id}' threw while probing the game.",
					plugin.Metadata.Id,
					"probe",
					exception
				);
				firstFailure ??= error;
				reports.Add(new PluginProbeReport
				{
					PluginId = plugin.Metadata.Id,
					Supported = true,
					Error = error,
				});
				continue;
			}
			if (probe == null)
			{
				var error = PluginError.Create(
					PluginErrorCode.InvalidProbeResult,
					$"Plugin '{plugin.Metadata.Id}' returned no probe result.",
					plugin.Metadata.Id,
					"probe"
				);
				firstFailure ??= error;
				reports.Add(new PluginProbeReport
				{
					PluginId = plugin.Metadata.Id,
					Supported = true,
					Error = error,
				});
				continue;
			}

			var probeValidation = probe.Validate(plugin.Metadata.Id);
			if (!probeValidation.Success)
			{
				firstFailure ??= probeValidation.Error;
				reports.Add(new PluginProbeReport
				{
					PluginId = plugin.Metadata.Id,
					Supported = true,
					Error = probeValidation.Error,
				});
				continue;
			}

			reports.Add(new PluginProbeReport
			{
				PluginId = plugin.Metadata.Id,
				Supported = true,
				Matched = probe.IsMatch,
				Score = probe.Score,
				Reason = probe.Reason,
				Diagnostics = probe.Diagnostics,
			});
			if (probe.IsMatch)
			{
				candidates.Add((plugin, probe));
			}
		}

		if (candidates.Count == 0)
		{
			if (firstFailure != null)
			{
				return PluginResult<EnginePluginSelection>.Failed(firstFailure, BuildDiagnostics(reports));
			}
			return PluginResult<EnginePluginSelection>.Failed(PluginError.Create(
				PluginErrorCode.NoMatchingPlugin,
				$"Registered plugins support engine '{pGame.EngineId}', but none matched this game.",
				pPhase: "probe"
			), BuildDiagnostics(reports));
		}

		// Ambiguous detection is resolved in this order and never by registration
		// order: probe score, declared priority, then ordinal plugin ID.
		candidates.Sort((pLeft, pRight) =>
		{
			var score = pRight.Probe.Score.CompareTo(pLeft.Probe.Score);
			if (score != 0)
			{
				return score;
			}
			var priority = pRight.Plugin.Metadata.Priority.CompareTo(pLeft.Plugin.Metadata.Priority);
			return priority != 0
				? priority
				: string.CompareOrdinal(pLeft.Plugin.Metadata.Id, pRight.Plugin.Metadata.Id);
		});

		var winner = candidates[0];
		var diagnostics = BuildDiagnostics(reports);
		diagnostics.Add(PluginDiagnostic.Info(
			"plugin.selected",
			$"Selected plugin '{winner.Plugin.Metadata.Id}' using score, priority, and ordinal ID precedence.",
			winner.Plugin.Metadata.Id
		));
		return PluginResult<EnginePluginSelection>.Succeeded(
			new EnginePluginSelection(winner.Plugin, pGame, winner.Probe, reports),
			diagnostics
		);
	}

	public PluginResult<IEngineRuntime> CreateRuntime(EnginePluginSelection pSelection)
	{
		if (pSelection == null)
		{
			return PluginResult<IEngineRuntime>.Failed(PluginError.Create(
				PluginErrorCode.RuntimeCreationFailed,
				"A plugin selection is required before runtime creation.",
				pPhase: "create"
			));
		}
		var pluginId = pSelection.Plugin.Metadata.Id;
		try
		{
			var result = pSelection.Plugin.CreateRuntime(new EnginePluginRuntimeContext(pSelection.Game, pSelection));
			if (result == null || !result.Success || result.Value == null)
			{
				var message = result?.Error?.Message ?? "Plugin returned no runtime instance.";
				var error = result?.Error ?? PluginError.Create(
					PluginErrorCode.RuntimeCreationFailed,
					message,
					pluginId,
					"create"
				);
				return PluginResult<IEngineRuntime>.Failed(
					new PluginError
					{
						Code = PluginErrorCode.RuntimeCreationFailed,
						Message = $"Plugin '{pluginId}' could not create a runtime: {error.Message}",
						PluginId = pluginId,
						Phase = "create",
						ExceptionType = error.ExceptionType,
					},
					result?.Diagnostics
				);
			}
			return result;
		}
		catch (Exception exception)
		{
			return PluginResult<IEngineRuntime>.Failed(PluginError.Create(
				PluginErrorCode.RuntimeCreationFailed,
				$"Plugin '{pluginId}' threw while creating its runtime.",
				pluginId,
				"create",
				exception
			));
		}
	}

	private static List<PluginDiagnostic> BuildDiagnostics(IEnumerable<PluginProbeReport> pReports)
	{
		var diagnostics = new List<PluginDiagnostic>();
		foreach (var report in pReports)
		{
			foreach (var diagnostic in report.Diagnostics)
			{
				diagnostics.Add(diagnostic);
			}
			if (report.Error != null)
			{
				diagnostics.Add(PluginDiagnostic.Warning(
					"plugin.probe.failed",
					report.Error.Message,
					report.PluginId
				));
			}
			else if (!report.Matched)
			{
				diagnostics.Add(PluginDiagnostic.Info(
					"plugin.probe.no-match",
					$"Plugin '{report.PluginId}' did not match: {report.Reason}",
					report.PluginId
				));
			}
		}
		return diagnostics;
	}
}
