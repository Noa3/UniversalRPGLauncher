using System;
using System.Collections.Generic;
using UniversalRPG.GameDetectorNs;

namespace UniversalRPG.Plugins;

/// <summary>
/// Stable identifiers for the engines currently understood by GameDetector.
/// Additional in-process plugins may use their own identifiers without changing
/// the registry or runtime host.
/// </summary>
public static class EnginePluginIds
{
	public const string RpgMaker2000 = "rpg-maker-2000";
	public const string RpgMaker2003 = "rpg-maker-2003";
	public const string RpgMaker2000_2003 = "rpg-maker-2000-2003";
	public const string RpgMakerXp = "rpg-maker-xp";
	public const string RpgMakerVx = "rpg-maker-vx";
	public const string RpgMakerVxAce = "rpg-maker-vx-ace";
	public const string RpgMakerMv = "rpg-maker-mv";
	public const string RpgMakerMz = "rpg-maker-mz";
	public const string RpgMaker95 = "rpg-maker-95";
	public const string Dante98 = "rpg-tsukuru-dante-98";
	public const string WolfRpg = "wolf-rpg";
	public const string RpgMakerUnite = "rpg-maker-unite";

	public static string FromDetectorEngine(GameDetector.EngineType pEngine)
	{
		return pEngine switch
		{
			GameDetector.EngineType.RpgMaker2000 => RpgMaker2000,
			GameDetector.EngineType.RpgMaker2003 => RpgMaker2003,
			GameDetector.EngineType.RpgMaker2000_2003 => RpgMaker2000_2003,
			GameDetector.EngineType.RpgMakerXp => RpgMakerXp,
			GameDetector.EngineType.RpgMakerVx => RpgMakerVx,
			GameDetector.EngineType.RpgMakerVxAce => RpgMakerVxAce,
			GameDetector.EngineType.RpgMakerMv => RpgMakerMv,
			GameDetector.EngineType.RpgMakerMz => RpgMakerMz,
			GameDetector.EngineType.RpgMaker95 => RpgMaker95,
			GameDetector.EngineType.Dante98 => Dante98,
			GameDetector.EngineType.WolfRpg => WolfRpg,
			GameDetector.EngineType.RpgMakerUnite => RpgMakerUnite,
			_ => "",
		};
	}
}

public enum PluginDiagnosticSeverity
{
	Info,
	Warning,
	Error,
}

[Flags]
public enum PluginCapability
{
	None = 0,
	Detection = 1 << 0,
	Parsing = 1 << 1,
	Runtime = 1 << 2,
	Rendering = 1 << 3,
	Audio = 1 << 4,
	Input = 1 << 5,
	SaveLoad = 1 << 6,
	Debugging = 1 << 7,
}

public enum PluginErrorCode
{
	InvalidMetadata,
	DuplicatePluginId,
	InvalidGame,
	UnsupportedEngine,
	NoMatchingPlugin,
	ProbeFailed,
	InvalidProbeResult,
	RuntimeCreationFailed,
	LifecycleFailure,
	InvalidLifecycleTransition,
}

public enum PluginRuntimeState
{
	NotStarted,
	Created,
	Initialized,
	Running,
	Stopped,
	Faulted,
	Disposed,
}

public sealed class PluginDiagnostic
{
	public PluginDiagnosticSeverity Severity { get; init; }
	public string Code { get; init; } = "";
	public string Message { get; init; } = "";
	public string PluginId { get; init; } = "";

	public static PluginDiagnostic Info(string pCode, string pMessage, string pPluginId = "")
	{
		return new PluginDiagnostic
		{
			Severity = PluginDiagnosticSeverity.Info,
			Code = pCode,
			Message = pMessage,
			PluginId = pPluginId,
		};
	}

	public static PluginDiagnostic Warning(string pCode, string pMessage, string pPluginId = "")
	{
		return new PluginDiagnostic
		{
			Severity = PluginDiagnosticSeverity.Warning,
			Code = pCode,
			Message = pMessage,
			PluginId = pPluginId,
		};
	}
}

public sealed class PluginError
{
	public PluginErrorCode Code { get; init; }
	public string Message { get; init; } = "";
	public string PluginId { get; init; } = "";
	public string Phase { get; init; } = "";
	public string ExceptionType { get; init; } = "";

	public static PluginError Create(
		PluginErrorCode pCode,
		string pMessage,
		string pPluginId = "",
		string pPhase = "",
		Exception? pException = null
	)
	{
		return new PluginError
		{
			Code = pCode,
			Message = pMessage,
			PluginId = pPluginId,
			Phase = pPhase,
			ExceptionType = pException?.GetType().FullName ?? "",
		};
	}
}

public sealed class PluginOperationResult
{
	private PluginOperationResult(bool pSuccess, PluginError? pError, IReadOnlyList<PluginDiagnostic> pDiagnostics)
	{
		Success = pSuccess;
		Error = pError;
		Diagnostics = pDiagnostics;
	}

	public bool Success { get; }
	public PluginError? Error { get; }
	public IReadOnlyList<PluginDiagnostic> Diagnostics { get; }

	public static PluginOperationResult Succeeded(IEnumerable<PluginDiagnostic>? pDiagnostics = null)
	{
		return new PluginOperationResult(true, null, CopyDiagnostics(pDiagnostics));
	}

	public static PluginOperationResult Failed(PluginError pError, IEnumerable<PluginDiagnostic>? pDiagnostics = null)
	{
		if (pError == null)
		{
			throw new ArgumentNullException(nameof(pError));
		}
		return new PluginOperationResult(false, pError, CopyDiagnostics(pDiagnostics));
	}

	private static IReadOnlyList<PluginDiagnostic> CopyDiagnostics(IEnumerable<PluginDiagnostic>? pDiagnostics)
	{
		return pDiagnostics == null ? Array.Empty<PluginDiagnostic>() : new List<PluginDiagnostic>(pDiagnostics);
	}
}

public sealed class PluginResult<T>
{
	private PluginResult(bool pSuccess, T? pValue, PluginError? pError, IReadOnlyList<PluginDiagnostic> pDiagnostics)
	{
		Success = pSuccess;
		Value = pValue;
		Error = pError;
		Diagnostics = pDiagnostics;
	}

	public bool Success { get; }
	public T? Value { get; }
	public PluginError? Error { get; }
	public IReadOnlyList<PluginDiagnostic> Diagnostics { get; }

	public static PluginResult<T> Succeeded(T pValue, IEnumerable<PluginDiagnostic>? pDiagnostics = null)
	{
		return new PluginResult<T>(true, pValue, null, CopyDiagnostics(pDiagnostics));
	}

	public static PluginResult<T> Failed(PluginError pError, IEnumerable<PluginDiagnostic>? pDiagnostics = null)
	{
		if (pError == null)
		{
			throw new ArgumentNullException(nameof(pError));
		}
		return new PluginResult<T>(false, default, pError, CopyDiagnostics(pDiagnostics));
	}

	private static IReadOnlyList<PluginDiagnostic> CopyDiagnostics(IEnumerable<PluginDiagnostic>? pDiagnostics)
	{
		return pDiagnostics == null ? Array.Empty<PluginDiagnostic>() : new List<PluginDiagnostic>(pDiagnostics);
	}
}

/// <summary>
/// Declares one engine, generation, and optional version interval supported by a plugin.
/// An empty interval means all versions of the declared engine/generation.
/// </summary>
public sealed class PluginEngineRange
{
	public string EngineId { get; init; } = "";
	public string Generation { get; init; } = "";
	public Version? MinimumVersion { get; init; }
	public Version? MaximumVersion { get; init; }

	public PluginOperationResult Validate(string pPluginId)
	{
		if (!IsStableIdentifier(EngineId, 96))
		{
			return PluginOperationResult.Failed(PluginError.Create(
				PluginErrorCode.InvalidMetadata,
				"Supported engine IDs must be stable lowercase identifiers.",
				pPluginId
			));
		}
		if ((Generation?.Length ?? 0) > 96)
		{
			return PluginOperationResult.Failed(PluginError.Create(
				PluginErrorCode.InvalidMetadata,
				"The supported engine generation is too long.",
				pPluginId
			));
		}
		if (MinimumVersion != null && MaximumVersion != null && MinimumVersion > MaximumVersion)
		{
			return PluginOperationResult.Failed(PluginError.Create(
				PluginErrorCode.InvalidMetadata,
				"The minimum supported version must not exceed the maximum version.",
				pPluginId
			));
		}
		return PluginOperationResult.Succeeded();
	}

	public bool Matches(PluginGameInfo pGame)
	{
		if (!EngineId.Equals(pGame.EngineId, StringComparison.Ordinal))
		{
			return false;
		}
		if (!string.IsNullOrEmpty(Generation)
			&& !Generation.Equals(pGame.Generation, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		if (MinimumVersion != null
			&& (pGame.EngineVersion == null || pGame.EngineVersion < MinimumVersion))
		{
			return false;
		}
		if (MaximumVersion != null
			&& (pGame.EngineVersion == null || pGame.EngineVersion > MaximumVersion))
		{
			return false;
		}
		return true;
	}

	private static bool IsStableIdentifier(string pValue, int pMaxLength)
	{
		if (string.IsNullOrEmpty(pValue) || pValue.Length > pMaxLength)
		{
			return false;
		}
		if (!IsAsciiAlphaNumeric(pValue[0]) || !IsAsciiAlphaNumeric(pValue[^1]))
		{
			return false;
		}
		foreach (var character in pValue)
		{
			if (!IsAsciiAlphaNumeric(character) && character != '-' && character != '_' && character != '.')
			{
				return false;
			}
		}
		return true;
	}

	private static bool IsAsciiAlphaNumeric(char pCharacter)
	{
		return (pCharacter >= 'a' && pCharacter <= 'z')
			|| (pCharacter >= '0' && pCharacter <= '9');
	}
}

public sealed class EnginePluginMetadata
{
	public string Id { get; init; } = "";
	public string DisplayName { get; init; } = "";
	public string Description { get; init; } = "";
	public IReadOnlyList<PluginEngineRange> SupportedEngines { get; init; } = Array.Empty<PluginEngineRange>();
	public PluginCapability Capabilities { get; init; } = PluginCapability.Runtime;
	public int Priority { get; init; }
	/// <summary>
	/// Optional normalized platform identifiers. An empty list means the plugin
	/// does not impose a platform restriction; an explicit list is enforced by
	/// runtime selection before a runtime is created.
	/// </summary>
	public IReadOnlyList<string> SupportedPlatforms { get; init; } = Array.Empty<string>();

	public bool SupportsPlatform(string pPlatform)
	{
		if (string.IsNullOrWhiteSpace(pPlatform) || SupportedPlatforms.Count == 0)
		{
			return SupportedPlatforms.Count == 0;
		}
		foreach (var platform in SupportedPlatforms)
		{
			if (platform.Equals(pPlatform, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}

	public PluginOperationResult Validate()
	{
		if (!IsStableIdentifier(Id, 64))
		{
			return PluginOperationResult.Failed(PluginError.Create(
				PluginErrorCode.InvalidMetadata,
				"Plugin ID must be a stable lowercase identifier.",
				Id
			));
		}
		if (string.IsNullOrWhiteSpace(DisplayName) || DisplayName.Length > 128)
		{
			return PluginOperationResult.Failed(PluginError.Create(
				PluginErrorCode.InvalidMetadata,
				"Plugin display name must be non-empty and at most 128 characters.",
				Id
			));
		}
		if (string.IsNullOrWhiteSpace(Description) || Description.Length > 2048)
		{
			return PluginOperationResult.Failed(PluginError.Create(
				PluginErrorCode.InvalidMetadata,
				"Plugin description must be non-empty and at most 2048 characters.",
				Id
			));
		}
		const PluginCapability allCapabilities =
			PluginCapability.Detection
			| PluginCapability.Parsing
			| PluginCapability.Runtime
			| PluginCapability.Rendering
			| PluginCapability.Audio
			| PluginCapability.Input
			| PluginCapability.SaveLoad
			| PluginCapability.Debugging;
		if (Capabilities == PluginCapability.None || (Capabilities & ~allCapabilities) != 0)
		{
			return PluginOperationResult.Failed(PluginError.Create(
				PluginErrorCode.InvalidMetadata,
				"Plugin capabilities must report at least one known capability.",
				Id
			));
		}
		if (SupportedEngines == null || SupportedEngines.Count == 0)
		{
			return PluginOperationResult.Failed(PluginError.Create(
				PluginErrorCode.InvalidMetadata,
				"At least one supported engine range is required.",
				Id
			));
		}
		if (SupportedPlatforms == null)
		{
			return PluginOperationResult.Failed(PluginError.Create(
				PluginErrorCode.InvalidMetadata,
				"Supported platforms must be an initialized collection.",
				Id
			));
		}

		for (var index = 0; index < SupportedEngines.Count; index += 1)
		{
			var range = SupportedEngines[index];
			if (range == null)
			{
				return PluginOperationResult.Failed(PluginError.Create(
					PluginErrorCode.InvalidMetadata,
					$"Supported engine range {index} is null.",
					Id
				));
			}
			var rangeResult = range.Validate(Id);
			if (!rangeResult.Success)
			{
				return rangeResult;
			}
		}
		return PluginOperationResult.Succeeded();
	}

	public bool Supports(PluginGameInfo pGame)
	{
		foreach (var range in SupportedEngines)
		{
			if (range != null && range.Matches(pGame))
			{
				return true;
			}
		}
		return false;
	}

	private static bool IsStableIdentifier(string pValue, int pMaxLength)
	{
		if (string.IsNullOrEmpty(pValue) || pValue.Length > pMaxLength)
		{
			return false;
		}
		foreach (var character in pValue)
		{
			if ((character < 'a' || character > 'z')
				&& (character < '0' || character > '9')
				&& character != '-' && character != '_' && character != '.')
			{
				return false;
			}
		}
		return IsAsciiAlphaNumeric(pValue[0]) && IsAsciiAlphaNumeric(pValue[^1]);
	}

	private static bool IsAsciiAlphaNumeric(char pCharacter)
	{
		return (pCharacter >= 'a' && pCharacter <= 'z')
			|| (pCharacter >= '0' && pCharacter <= '9');
	}
}

public sealed class PluginGameInfo
{
	public string GameDirectory { get; init; } = "";
	public string EngineId { get; init; } = "";
	public string Generation { get; init; } = "";
	public Version? EngineVersion { get; init; }
	public int DetectorScore { get; init; }
	public IReadOnlyList<string> Evidence { get; init; } = Array.Empty<string>();

	public PluginOperationResult Validate()
	{
		if (string.IsNullOrWhiteSpace(GameDirectory))
		{
			return PluginOperationResult.Failed(PluginError.Create(
				PluginErrorCode.InvalidGame,
				"A game directory is required before probing a plugin."
			));
		}
		if (string.IsNullOrWhiteSpace(EngineId))
		{
			return PluginOperationResult.Failed(PluginError.Create(
				PluginErrorCode.InvalidGame,
				"A detected engine ID is required before selecting a plugin."
			));
		}
		if (DetectorScore < 0)
		{
			return PluginOperationResult.Failed(PluginError.Create(
				PluginErrorCode.InvalidGame,
				"The detector score must not be negative."
			));
		}
		return PluginOperationResult.Succeeded();
	}

	public static PluginGameInfo FromDetection(GameDetector.DetectionResult pDetection)
	{
		if (pDetection == null)
		{
			throw new ArgumentNullException(nameof(pDetection));
		}
		var score = pDetection.Confidence switch
		{
			GameDetector.Confidence.High => 3,
			GameDetector.Confidence.Medium => 2,
			_ => 1,
		};
		return new PluginGameInfo
		{
			GameDirectory = pDetection.GameDirectory,
			EngineId = EnginePluginIds.FromDetectorEngine(pDetection.Engine),
			DetectorScore = score,
			Evidence = pDetection.Evidence,
		};
	}
}

public sealed class EnginePluginProbeContext
{
	public EnginePluginProbeContext(PluginGameInfo pGame)
	{
		Game = pGame ?? throw new ArgumentNullException(nameof(pGame));
	}

	public PluginGameInfo Game { get; }
}

public sealed class EnginePluginRuntimeContext
{
	public EnginePluginRuntimeContext(PluginGameInfo pGame, EnginePluginSelection pSelection)
	{
		Game = pGame ?? throw new ArgumentNullException(nameof(pGame));
		Selection = pSelection ?? throw new ArgumentNullException(nameof(pSelection));
	}

	public PluginGameInfo Game { get; }
	public EnginePluginSelection Selection { get; }
}

public sealed class PluginProbeResult
{
	public bool IsMatch { get; init; }
	public int Score { get; init; }
	public string Reason { get; init; } = "";
	public IReadOnlyList<PluginDiagnostic> Diagnostics { get; init; } = Array.Empty<PluginDiagnostic>();

	public static PluginProbeResult Match(int pScore, string pReason, IEnumerable<PluginDiagnostic>? pDiagnostics = null)
	{
		return new PluginProbeResult
		{
			IsMatch = true,
			Score = pScore,
			Reason = pReason,
			Diagnostics = pDiagnostics == null ? Array.Empty<PluginDiagnostic>() : new List<PluginDiagnostic>(pDiagnostics),
		};
	}

	public static PluginProbeResult NoMatch(string pReason, IEnumerable<PluginDiagnostic>? pDiagnostics = null)
	{
		return new PluginProbeResult
		{
			IsMatch = false,
			Reason = pReason,
			Diagnostics = pDiagnostics == null ? Array.Empty<PluginDiagnostic>() : new List<PluginDiagnostic>(pDiagnostics),
		};
	}

	public PluginOperationResult Validate(string pPluginId)
	{
		if (Score < 0 || Score > 1000)
		{
			return PluginOperationResult.Failed(PluginError.Create(
				PluginErrorCode.InvalidProbeResult,
				"Probe score must be between 0 and 1000.",
				pPluginId,
				"probe"
			));
		}
		if (IsMatch && string.IsNullOrWhiteSpace(Reason))
		{
			return PluginOperationResult.Failed(PluginError.Create(
				PluginErrorCode.InvalidProbeResult,
				"A matching probe must provide a reason.",
				pPluginId,
				"probe"
			));
		}
		return PluginOperationResult.Succeeded(Diagnostics);
	}
}

public interface IEnginePlugin
{
	EnginePluginMetadata Metadata { get; }
	PluginProbeResult Probe(EnginePluginProbeContext pContext);
	PluginResult<IEngineRuntime> CreateRuntime(EnginePluginRuntimeContext pContext);
}

public interface IRuntimeSaveTools
{
	/// <summary>Exports a bounded runtime-owned snapshot; this never writes a game save file.</summary>
	PluginResult<string> ExportSaveSnapshot();
	/// <summary>Imports a bounded runtime-owned snapshot already supplied by the user or editor.</summary>
	PluginOperationResult ImportSaveSnapshot(string pSnapshot);
}

public interface IRuntimeDebugTools
{
	bool DebugToolsEnabled { get; }
	/// <summary>Explicit opt-in gate for local single-player debug mutations.</summary>
	PluginOperationResult SetDebugToolsEnabled(bool pEnabled);
	PluginOperationResult TrySetGold(int pGold);
	PluginOperationResult TrySetSwitch(int pSwitchId, bool pValue);
}

public interface IEngineRuntime : IDisposable
{
	PluginRuntimeState State { get; }
	PluginOperationResult Initialize(EnginePluginRuntimeContext pContext);
	PluginOperationResult Start();
	PluginOperationResult Update(double pDeltaSeconds);
	PluginOperationResult Stop();
}
