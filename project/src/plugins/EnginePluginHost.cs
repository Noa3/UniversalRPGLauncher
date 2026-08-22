using System;
using System.Collections.Generic;

namespace UniversalRPG.Plugins;

/// <summary>
/// Owns one selected plugin runtime and enforces a predictable lifecycle. A
/// failed lifecycle operation moves the host to Faulted and disposes the
/// runtime so a partially initialized engine cannot be reused accidentally.
/// </summary>
public sealed class EnginePluginHost : IDisposable
{
	private readonly EnginePluginRegistry _registry;
	private bool _disposed;
	private bool _runtimeDisposed;

	public EnginePluginHost(EnginePluginRegistry pRegistry)
	{
		_registry = pRegistry ?? throw new ArgumentNullException(nameof(pRegistry));
	}

	public IEnginePlugin? Plugin { get; private set; }
	public IEngineRuntime? Runtime { get; private set; }
	public PluginRuntimeState State { get; private set; } = PluginRuntimeState.NotStarted;
	public PluginError? LastError { get; private set; }

	public PluginOperationResult Start(PluginGameInfo pGame)
	{
		if (_disposed || State == PluginRuntimeState.Disposed)
		{
			return TransitionFailure("start", "The plugin host has already been disposed.");
		}
		if (State != PluginRuntimeState.NotStarted)
		{
			return TransitionFailure("start", $"Cannot start a host in state {State}.");
		}

		var selection = _registry.Select(pGame);
		if (!selection.Success || selection.Value == null)
		{
			return FailBeforeRuntime(selection.Error ?? PluginError.Create(
				PluginErrorCode.NoMatchingPlugin,
				"No plugin could be selected.",
				pPhase: "select"
			), selection.Diagnostics);
		}

		var selected = selection.Value;
		var runtimeResult = _registry.CreateRuntime(selected);
		if (!runtimeResult.Success || runtimeResult.Value == null)
		{
			return FailBeforeRuntime(runtimeResult.Error ?? PluginError.Create(
				PluginErrorCode.RuntimeCreationFailed,
				"The selected plugin did not create a runtime.",
				selected.Plugin.Metadata.Id,
				"create"
			), runtimeResult.Diagnostics);
		}

		Plugin = selected.Plugin;
		Runtime = runtimeResult.Value;
		State = PluginRuntimeState.Created;

		var initialize = InvokeLifecycle(
		    "initialize",
		    () => Runtime.Initialize(new EnginePluginRuntimeContext(selected.Game, selected))
		);
		if (!initialize.Success)
		{
		    return FailRuntime(AddDiagnostics(initialize, selection.Diagnostics));
		}
		State = PluginRuntimeState.Initialized;

		var start = InvokeLifecycle("start", () => Runtime.Start());
		if (!start.Success)
		{
		    return FailRuntime(AddDiagnostics(start, CombineDiagnostics(selection.Diagnostics, initialize.Diagnostics)));
		}
		State = PluginRuntimeState.Running;
		return PluginOperationResult.Succeeded(
		    CombineDiagnostics(selection.Diagnostics, CombineDiagnostics(initialize.Diagnostics, start.Diagnostics)));
	}

	public PluginOperationResult Update(double pDeltaSeconds)
	{
		if (double.IsNaN(pDeltaSeconds) || double.IsInfinity(pDeltaSeconds) || pDeltaSeconds < 0)
		{
			return TransitionFailure("update", "Delta time must be finite and non-negative.");
		}
		if (State != PluginRuntimeState.Running || Runtime == null)
		{
			return TransitionFailure("update", $"Cannot update a host in state {State}.");
		}
		var result = InvokeLifecycle("update", () => Runtime.Update(pDeltaSeconds));
		return result.Success ? result : FailRuntime(result);
	}

	public PluginOperationResult Stop()
	{
		if (State != PluginRuntimeState.Initialized && State != PluginRuntimeState.Running)
		{
			return TransitionFailure("stop", $"Cannot stop a host in state {State}.");
		}
		if (Runtime == null)
		{
			return FailBeforeRuntime(PluginError.Create(
				PluginErrorCode.InvalidLifecycleTransition,
				"The host has no runtime to stop.",
				Plugin?.Metadata.Id ?? "",
				"stop"
			));
		}
		var result = InvokeLifecycle("stop", () => Runtime.Stop());
		if (!result.Success)
		{
			return FailRuntime(result);
		}
		State = PluginRuntimeState.Stopped;
		return result;
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}
		_disposed = true;
		if (Runtime != null)
		{
			if (State == PluginRuntimeState.Initialized || State == PluginRuntimeState.Running)
			{
				Stop();
			}
			if (!_runtimeDisposed)
			{
				try
				{
					Runtime.Dispose();
				}
				catch (Exception exception)
				{
					if (LastError == null)
					{
						LastError = PluginError.Create(
							PluginErrorCode.LifecycleFailure,
							$"Plugin '{Plugin?.Metadata.Id}' threw while disposing its runtime.",
							Plugin?.Metadata.Id ?? "",
							"dispose",
							exception
						);
					}
				}
				finally
				{
					_runtimeDisposed = true;
				}
			}
		}
		State = PluginRuntimeState.Disposed;
	}

	private PluginOperationResult InvokeLifecycle(string pPhase, Func<PluginOperationResult> pOperation)
	{
		var pluginId = Plugin?.Metadata.Id ?? "";
		try
		{
			var result = pOperation();
			if (result == null)
			{
				return PluginOperationResult.Failed(PluginError.Create(
					PluginErrorCode.LifecycleFailure,
					$"Plugin '{pluginId}' returned no result during {pPhase}.",
					pluginId,
					pPhase
				));
			}
			if (!result.Success)
			{
				var detail = result.Error?.Message ?? "The plugin reported an unspecified lifecycle failure.";
				return PluginOperationResult.Failed(PluginError.Create(
					PluginErrorCode.LifecycleFailure,
					$"Plugin '{pluginId}' failed during {pPhase}: {detail}",
					pluginId,
					pPhase,
					result.Error == null ? null : new InvalidOperationException(result.Error.Message)
				), result.Diagnostics);
			}
			return result;
		}
		catch (Exception exception)
		{
			return PluginOperationResult.Failed(PluginError.Create(
				PluginErrorCode.LifecycleFailure,
				$"Plugin '{pluginId}' threw during {pPhase}.",
				pluginId,
				pPhase,
				exception
			));
		}
	}

	private PluginOperationResult FailRuntime(PluginOperationResult pResult)
	{
	    State = PluginRuntimeState.Faulted;
	    LastError = pResult.Error;
	    DisposeRuntimeAfterFailure();
	    return pResult;
	}

	private static PluginOperationResult AddDiagnostics(
	    PluginOperationResult pResult,
	    IEnumerable<PluginDiagnostic> pAdditionalDiagnostics)
	{
	    var diagnostics = CombineDiagnostics(pAdditionalDiagnostics, pResult.Diagnostics);
	    return pResult.Success
	        ? PluginOperationResult.Succeeded(diagnostics)
	        : PluginOperationResult.Failed(pResult.Error!, diagnostics);
	}

	private static IReadOnlyList<PluginDiagnostic> CombineDiagnostics(
	    IEnumerable<PluginDiagnostic> pFirst,
	    IEnumerable<PluginDiagnostic> pSecond)
	{
	    var diagnostics = new List<PluginDiagnostic>();
	    diagnostics.AddRange(pFirst);
	    diagnostics.AddRange(pSecond);
	    return diagnostics;
	}

	private PluginOperationResult FailBeforeRuntime(PluginError pError, System.Collections.Generic.IEnumerable<PluginDiagnostic>? pDiagnostics = null)
	{
		State = PluginRuntimeState.Faulted;
		LastError = pError;
		return PluginOperationResult.Failed(pError, pDiagnostics);
	}

	private PluginOperationResult TransitionFailure(string pPhase, string pMessage)
	{
		var error = PluginError.Create(
			PluginErrorCode.InvalidLifecycleTransition,
			pMessage,
			Plugin?.Metadata.Id ?? "",
			pPhase
		);
		LastError = error;
		return PluginOperationResult.Failed(error);
	}

	private void DisposeRuntimeAfterFailure()
	{
		if (Runtime == null || _runtimeDisposed)
		{
			return;
		}
		try
		{
			Runtime.Dispose();
		}
		catch (Exception exception)
		{
			if (LastError == null)
			{
				LastError = PluginError.Create(
					PluginErrorCode.LifecycleFailure,
					$"Plugin '{Plugin?.Metadata.Id}' also threw while cleaning up a failed runtime.",
					Plugin?.Metadata.Id ?? "",
					"dispose",
					exception
				);
			}
		}
		finally
		{
			_runtimeDisposed = true;
		}
	}
}
