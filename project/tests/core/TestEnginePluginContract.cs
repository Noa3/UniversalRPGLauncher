using System;
using UniversalRPG.Plugins;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

public partial class TestEnginePluginContract : TestBase
{
	public void Test_AmbiguousDetectionUsesScorePriorityAndStableId()
	{
		var registry = new EnginePluginRegistry();
		var lowerPriority = new FakePlugin("zeta-engine", 1, 500);
		var samePriorityLowerId = new FakePlugin("alpha-engine", 1, 500);
		var higherPriority = new FakePlugin("middle-engine", 2, 500);
		AssertTrue(registry.Register(lowerPriority).Success);
		AssertTrue(registry.Register(samePriorityLowerId).Success);
		AssertTrue(registry.Register(higherPriority).Success);

		var result = registry.Select(CreateGame());

		AssertTrue(result.Success);
		AssertEq(result.Value?.Plugin.Metadata.Id, "middle-engine", "Declared priority breaks score ties");

		registry.Unregister("middle-engine");
		result = registry.Select(CreateGame());
		AssertTrue(result.Success);
		AssertEq(result.Value?.Plugin.Metadata.Id, "alpha-engine", "Ordinal plugin ID breaks remaining ties");
	}

	public void Test_InvalidProbeResultIsReported()
	{
		var registry = new EnginePluginRegistry();
		var plugin = new FakePlugin("invalid-probe", 0, 1001);
		AssertTrue(registry.Register(plugin).Success);

		var result = registry.Select(CreateGame());

		AssertFalse(result.Success);
		AssertEq(result.Error?.Code, PluginErrorCode.InvalidProbeResult);
		AssertEq(result.Error?.Phase, "probe");
	}

	public void Test_LifecycleFailureIsTypedAndCleansUpRuntime()
	{
		var registry = new EnginePluginRegistry();
		var plugin = new FakePlugin("failing-runtime", 0, 700, failStart: true);
		AssertTrue(registry.Register(plugin).Success);
		using var host = new EnginePluginHost(registry);

		var result = host.Start(CreateGame());

		AssertFalse(result.Success);
		AssertEq(result.Error?.Code, PluginErrorCode.LifecycleFailure);
		AssertEq(result.Error?.PluginId, "failing-runtime");
		AssertEq(result.Error?.Phase, "start");
		AssertEq(host.State, PluginRuntimeState.Faulted);
		AssertTrue(plugin.LastRuntime?.Disposed ?? false, "Failed lifecycle disposes the runtime");
		host.Dispose();
		host.Dispose();
		AssertEq(plugin.LastRuntime?.DisposeCount ?? -1, 1, "Runtime cleanup is idempotent");
	}

	public void Test_MalformedMetadataIsRejectedBeforeRegistration()
	{
		var registry = new EnginePluginRegistry();
		var malformedId = new FakePlugin(new EnginePluginMetadata
		{
			Id = "Not A Stable ID",
			DisplayName = "Malformed",
			Description = "Invalid identifier",
			SupportedEngines = new[] { SupportedRange() },
		});
		var reversedRange = new FakePlugin(new EnginePluginMetadata
		{
			Id = "reversed-range",
			DisplayName = "Malformed",
			Description = "Invalid version range",
			SupportedEngines = new[]
			{
				new PluginEngineRange
				{
					EngineId = "test-engine",
					MinimumVersion = new Version(2, 0),
					MaximumVersion = new Version(1, 0),
				},
			},
		});

		var badIdResult = registry.Register(malformedId);
		var badRangeResult = registry.Register(reversedRange);

		AssertFalse(badIdResult.Success);
		AssertEq(badIdResult.Error?.Code, PluginErrorCode.InvalidMetadata);
		AssertFalse(badRangeResult.Success);
		AssertEq(badRangeResult.Error?.Code, PluginErrorCode.InvalidMetadata);
		AssertEq(registry.Plugins.Count, 0);
	}

	public void Test_RegistrationSelectionAndRuntimeCreationAreInProcess()
	{
		var registry = new EnginePluginRegistry();
		var plugin = new FakePlugin("test-engine-plugin", 0, 800);

		var registration = registry.Register(plugin);
		var selection = registry.Select(CreateGame());
		var runtime = selection.Success && selection.Value != null
			? registry.CreateRuntime(selection.Value)
			: PluginResult<IEngineRuntime>.Failed(PluginError.Create(
				PluginErrorCode.RuntimeCreationFailed,
				"Selection failed in test"
			));

		AssertTrue(registration.Success);
		AssertTrue(selection.Success);
		AssertEq(selection.Value?.Plugin.Metadata.Id, "test-engine-plugin");
		AssertTrue((plugin.Metadata.Capabilities & PluginCapability.Runtime) != 0);
		AssertTrue(runtime.Success);
		AssertTrue(runtime.Value != null);
		AssertTrue(plugin.LastRuntime != null);
		runtime.Value?.Dispose();
	}

	public void Test_UnsupportedEngineReturnsTypedError()
	{
		var registry = new EnginePluginRegistry();
		var plugin = new FakePlugin("xp-only", 0, 500, EnginePluginIds.RpgMakerXp);
		AssertTrue(registry.Register(plugin).Success);

		var result = registry.Select(CreateGame(EnginePluginIds.RpgMaker2003));

		AssertFalse(result.Success);
		AssertEq(result.Error?.Code, PluginErrorCode.UnsupportedEngine);
		AssertEq(result.Error?.Phase, "select");
	}

	public void Test_VersionAndGenerationRangesAreHonored()
	{
		var registry = new EnginePluginRegistry();
		var plugin = new FakePlugin(new EnginePluginMetadata
		{
			Id = "versioned-engine",
			DisplayName = "Versioned engine",
			Description = "Range test plugin",
			SupportedEngines = new[]
			{
				new PluginEngineRange
				{
					EngineId = "test-engine",
					Generation = "test",
					MinimumVersion = new Version(1, 0),
					MaximumVersion = new Version(2, 0),
				},
			},
		});
		AssertTrue(registry.Register(plugin).Success);

		var matching = registry.Select(CreateGame());
		var wrongVersion = registry.Select(new PluginGameInfo
		{
			GameDirectory = "fixture://plugin-game",
			EngineId = "test-engine",
			Generation = "test",
			EngineVersion = new Version(3, 0),
		});
		var wrongGeneration = registry.Select(new PluginGameInfo
		{
			GameDirectory = "fixture://plugin-game",
			EngineId = "test-engine",
			Generation = "other",
			EngineVersion = new Version(1, 0),
		});

		AssertTrue(matching.Success);
		AssertFalse(wrongVersion.Success);
		AssertEq(wrongVersion.Error?.Code, PluginErrorCode.UnsupportedEngine);
		AssertFalse(wrongGeneration.Success);
		AssertEq(wrongGeneration.Error?.Code, PluginErrorCode.UnsupportedEngine);
	}

	public void Test_HostRunsAndStopsSuccessfulLifecycle()
	{
		var registry = new EnginePluginRegistry();
		var plugin = new FakePlugin("healthy-runtime", 0, 600);
		AssertTrue(registry.Register(plugin).Success);
		using var host = new EnginePluginHost(registry);

		var started = host.Start(CreateGame());
		var updated = host.Update(1.0 / 60.0);
		var stopped = host.Stop();

		AssertTrue(started.Success);
		AssertEq(host.State, PluginRuntimeState.Stopped);
		AssertTrue(updated.Success);
		AssertTrue(stopped.Success);
		AssertEq(plugin.LastRuntime?.UpdateCount ?? -1, 1);
		AssertTrue(plugin.LastRuntime?.StopCalled ?? false);
	}

	private static PluginGameInfo CreateGame(string pEngineId = "test-engine")
	{
		return new PluginGameInfo
		{
			GameDirectory = "fixture://plugin-game",
			EngineId = pEngineId,
			EngineVersion = new Version(1, 0),
			Generation = "test",
			DetectorScore = 3,
		};
	}

	private static PluginEngineRange SupportedRange(string pEngineId = "test-engine")
	{
		return new PluginEngineRange
		{
			EngineId = pEngineId,
			Generation = "test",
		};
	}

	private sealed class FakePlugin : IEnginePlugin
	{
		private readonly int _probeScore;
		private readonly bool _failInitialize;
		private readonly bool _failStart;

		public FakePlugin(
			string pId,
			int pPriority,
			int pProbeScore,
			bool failInitialize = false,
			bool failStart = false
		)
			: this(new EnginePluginMetadata
			{
				Id = pId,
				DisplayName = pId,
				Description = "Test plugin",
				Priority = pPriority,
				SupportedEngines = new[] { SupportedRange() },
			}, pProbeScore, failInitialize, failStart)
		{
		}

		public FakePlugin(string pId, int pPriority, int pProbeScore, string pEngineId)
			: this(new EnginePluginMetadata
			{
				Id = pId,
				DisplayName = pId,
				Description = "Test plugin",
				Priority = pPriority,
				SupportedEngines = new[] { SupportedRange(pEngineId) },
			}, pProbeScore, false, false)
		{
		}

		public FakePlugin(EnginePluginMetadata pMetadata)
			: this(pMetadata, 500, false, false)
		{
		}

		private FakePlugin(
			EnginePluginMetadata pMetadata,
			int pProbeScore,
			bool pFailInitialize,
			bool pFailStart
		)
		{
			Metadata = pMetadata;
			_probeScore = pProbeScore;
			_failInitialize = pFailInitialize;
			_failStart = pFailStart;
		}

		public EnginePluginMetadata Metadata { get; }
		public FakeRuntime? LastRuntime { get; private set; }

		public PluginProbeResult Probe(EnginePluginProbeContext pContext)
		{
			return PluginProbeResult.Match(_probeScore, "Synthetic fixture matched");
		}

		public PluginResult<IEngineRuntime> CreateRuntime(EnginePluginRuntimeContext pContext)
		{
			LastRuntime = new FakeRuntime(_failInitialize, _failStart);
			return PluginResult<IEngineRuntime>.Succeeded(LastRuntime);
		}
	}

	private sealed class FakeRuntime : IEngineRuntime
	{
		private readonly bool _failInitialize;
		private readonly bool _failStart;

		public FakeRuntime(bool pFailInitialize, bool pFailStart)
		{
			_failInitialize = pFailInitialize;
			_failStart = pFailStart;
		}

		public PluginRuntimeState State { get; private set; } = PluginRuntimeState.Created;
		public bool Disposed { get; private set; }
		public int DisposeCount { get; private set; }
		public bool StopCalled { get; private set; }
		public int UpdateCount { get; private set; }

		public PluginOperationResult Initialize(EnginePluginRuntimeContext pContext)
		{
			if (_failInitialize)
			{
				State = PluginRuntimeState.Faulted;
				return PluginOperationResult.Failed(PluginError.Create(
					PluginErrorCode.LifecycleFailure,
					"Synthetic initialization failure"
				));
			}
			State = PluginRuntimeState.Initialized;
			return PluginOperationResult.Succeeded();
		}

		public PluginOperationResult Start()
		{
			if (_failStart)
			{
				State = PluginRuntimeState.Faulted;
				return PluginOperationResult.Failed(PluginError.Create(
					PluginErrorCode.LifecycleFailure,
					"Synthetic start failure"
				));
			}
			State = PluginRuntimeState.Running;
			return PluginOperationResult.Succeeded();
		}

		public PluginOperationResult Update(double pDeltaSeconds)
		{
			UpdateCount += 1;
			return PluginOperationResult.Succeeded();
		}

		public PluginOperationResult Stop()
		{
			StopCalled = true;
			State = PluginRuntimeState.Stopped;
			return PluginOperationResult.Succeeded();
		}

		public void Dispose()
		{
			DisposeCount += 1;
			Disposed = true;
			State = PluginRuntimeState.Disposed;
		}
	}
}
