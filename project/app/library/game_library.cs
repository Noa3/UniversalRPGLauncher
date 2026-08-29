using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Godot;
using UniversalRPG.GameDetectorNs;
using UniversalRPG.Plugins;

namespace UniversalRPG.App.Library;

public partial class GameLibrary : RefCounted
{
	public const string SettingsPath = "user://library.cfg";
	public const int MaxScanDepth = 4;
	public const int MaxScanDirectories = 4096;

	public enum GameCompatibilityStatus
	{
		Unknown,
		Ambiguous,
		Malformed,
		DetectionOnly,
		Supported,
		RegistryFailure,
	}

	public class GameEntry
	{
		public string Id;
		public string Title;
		public string Path;
		public GameDetector.DetectionResult Detection;
		public string SelectedPluginId { get; internal set; } = "";
		public GameCompatibilityStatus CompatibilityStatus { get; internal set; }
		public bool LoadedFromPersistence { get; internal set; }
		public IReadOnlyList<EngineDetectionCandidate> Candidates => Detection.Candidates;
		public IReadOnlyList<PluginDiagnostic> Diagnostics => Detection.Diagnostics;
		public int DetectionScore => Candidates.Count == 0 ? 0 : Candidates[0].Score;
		public double DetectionConfidence => Candidates.Count == 0 ? 0 : Candidates[0].Confidence;

		public GameEntry(
			string pPath,
			GameDetector.DetectionResult pDetection,
			string pSelectedPluginId = "",
			GameCompatibilityStatus pCompatibilityStatus = GameCompatibilityStatus.Unknown
		)
		{
			Path = pPath;
			Detection = pDetection;
			Id = pPath.Sha256Text();
			Title = string.IsNullOrEmpty(pDetection.Title) ? System.IO.Path.GetFileName(pPath) : pDetection.Title;
			SelectedPluginId = pSelectedPluginId;
			CompatibilityStatus = pCompatibilityStatus;
		}
	}

	public string RootPath { get; private set; } = "";
	public List<GameEntry> Games { get; } = new();

	private readonly GameDetector _detector;
	private readonly EnginePluginRegistry _runtimeRegistry;
	private readonly string _settingsPath;
	private readonly Dictionary<string, StoredGameRecord> _persistedRecords = new(StringComparer.Ordinal);
	private bool _persistedRecordsLoaded;
	private int _scannedDirectories;

	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
	};

	public GameLibrary(
		GameDetector? pDetector = null,
		EnginePluginRegistry? pRuntimeRegistry = null,
		string? pSettingsPath = null
	)
	{
		_detector = pDetector ?? new GameDetector();
		_runtimeRegistry = pRuntimeRegistry ?? BuiltInEnginePluginCatalog.CreateRuntimeRegistry();
		_settingsPath = string.IsNullOrWhiteSpace(pSettingsPath) ? SettingsPath : pSettingsPath;
	}

	public void LoadSettings()
	{
		var defaultPath = ProjectSettings.GlobalizePath("user://games");
		var config = new ConfigFile();
		if (config.Load(_settingsPath) == Error.Ok)
		{
			RootPath = config.GetValue("library", "games_directory", defaultPath).AsString();
			LoadPersistedRecords(config);
		}
		else
		{
			RootPath = defaultPath;
			_persistedRecords.Clear();
			_persistedRecordsLoaded = true;
		}
		SetRootPath(RootPath, false);
	}

	public Error SetRootPath(string pPath, bool pPersist = true)
	{
		var normalized = pPath.StripEdges().Replace("\\", "/").SimplifyPath();
		if (string.IsNullOrEmpty(normalized))
		{
			return Error.InvalidParameter;
		}
		if (normalized.StartsWith("user://") || normalized.StartsWith("res://"))
		{
			normalized = ProjectSettings.GlobalizePath(normalized);
		}
		var error = DirAccess.MakeDirRecursiveAbsolute(normalized);
		if (error != Error.Ok && error != Error.AlreadyExists)
		{
			return error;
		}
		RootPath = normalized;
		if (pPersist)
		{
			SaveSettings();
		}
		return Error.Ok;
	}

	public List<GameEntry> Scan()
	{
		EnsurePersistedRecordsLoaded();
		Games.Clear();
		_scannedDirectories = 0;
		if (!DirAccess.DirExistsAbsolute(RootPath))
		{
			return Games;
		}
		ScanDirectory(RootPath, 0);
		Games.Sort((pLeft, pRight) => string.Compare(pLeft.Title, pRight.Title, StringComparison.OrdinalIgnoreCase));
		PersistEntries();
		return Games;
	}

	/// <summary>
	/// Imports one explicit folder or supported archive and persists its complete
	/// detection snapshot. Unknown and ambiguous sources are retained so the UI
	/// can explain why launch is unavailable instead of silently dropping them.
	/// </summary>
	public GameEntry? Import(string pPath, bool pPersist = true)
	{
		EnsurePersistedRecordsLoaded();
		var normalized = NormalizeSourcePath(pPath);
		if (string.IsNullOrEmpty(normalized)
			|| (!Directory.Exists(normalized) && !File.Exists(normalized)))
		{
			return null;
		}

		var entry = ImportInternal(normalized);
		UpsertEntry(entry);
		if (pPersist)
		{
			PersistEntry(entry);
		}
		return entry;
	}

	private void ScanDirectory(string pPath, int pDepth)
	{
		if (_scannedDirectories >= MaxScanDirectories)
		{
			return;
		}
		_scannedDirectories += 1;

		if (LooksLikeGameRoot(pPath))
		{
			var entry = ImportInternal(pPath);
			if (entry != null && IsRecognized(entry.Detection))
			{
				Games.Add(entry);
				return;
			}
		}
		if (pDepth >= MaxScanDepth)
		{
			return;
		}
		using var directory = DirAccess.Open(pPath);
		if (directory == null)
		{
			return;
		}
		foreach (var directoryName in directory.GetDirectories())
		{
			if (ShouldSkipDirectory(directoryName) || directory.IsLink(directoryName))
			{
				continue;
			}
			ScanDirectory(pPath.PathJoin(directoryName), pDepth + 1);
		}
	}

	private static bool LooksLikeGameRoot(string pPath)
	{
		try
		{
			var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (var entry in new DirectoryInfo(pPath).EnumerateFileSystemInfos())
			{
				if (!entry.Attributes.HasFlag(FileAttributes.ReparsePoint))
				{
					names.Add(entry.Name);
				}
			}
			return names.Contains("RPG_RT.ldb") || names.Contains("RPG_RT.lmt")
				|| names.Contains("RPG_RT.ini") || names.Contains("Game.ini")
				|| names.Contains("Game.exe") || names.Contains("index.html")
				|| names.Contains("package.json") || names.Contains("Game.dat")
				|| names.Contains("BasicData") || names.Contains("Data")
				|| names.Contains("data") || names.Contains("www") || names.Contains("Scripts");
		}
		catch
		{
			return false;
		}
	}

	private static bool ShouldSkipDirectory(string pName)
	{
		return pName.StartsWith(".", StringComparison.Ordinal)
			|| pName.Equals("node_modules", StringComparison.OrdinalIgnoreCase)
			|| pName.Equals("locales", StringComparison.OrdinalIgnoreCase)
			|| pName.Equals("pnacl", StringComparison.OrdinalIgnoreCase)
			|| pName.Equals("swiftshader", StringComparison.OrdinalIgnoreCase)
			|| pName.Equals("img", StringComparison.OrdinalIgnoreCase)
			|| pName.Equals("audio", StringComparison.OrdinalIgnoreCase)
			|| pName.Equals("fonts", StringComparison.OrdinalIgnoreCase)
			|| pName.Equals("effects", StringComparison.OrdinalIgnoreCase);
	}

	private void SaveSettings()
	{
		var config = new ConfigFile();
		config.Load(_settingsPath);
		config.SetValue("library", "games_directory", RootPath);
		foreach (var record in _persistedRecords.Values)
		{
			config.SetValue("games", record.Id, JsonSerializer.Serialize(record, JsonOptions));
		}
		config.Save(_settingsPath);
	}

	private GameEntry ImportInternal(string pPath)
	{
		var detection = _detector.Analyze(pPath);
		var selected = detection.Report.SelectedCandidate;
		var entry = new GameEntry(
			pPath,
			detection,
			selected?.PluginId ?? "",
			DetermineCompatibility(detection)
		);
		var persisted = FindPersistedRecord(entry);
		if (persisted != null)
		{
			entry.LoadedFromPersistence = true;
			// A persisted explicit selection is reused only when the current bounded
			// detection still reports that candidate. Stale selections never drive launch.
			if (!string.IsNullOrEmpty(persisted.SelectedPluginId)
				&& detection.Candidates.Any(pCandidate =>
					pCandidate.PluginId.Equals(persisted.SelectedPluginId, StringComparison.Ordinal)))
			{
				entry.SelectedPluginId = persisted.SelectedPluginId;
			}
		}
		return entry;
	}

	private void UpsertEntry(GameEntry pEntry)
	{
		for (var index = Games.Count - 1; index >= 0; index -= 1)
		{
			if (Games[index].Id.Equals(pEntry.Id, StringComparison.Ordinal))
			{
				Games.RemoveAt(index);
			}
		}
		Games.Add(pEntry);
		Games.Sort((pLeft, pRight) => string.Compare(pLeft.Title, pRight.Title, StringComparison.OrdinalIgnoreCase));
	}

	private static bool IsRecognized(GameDetector.DetectionResult pDetection)
	{
		return pDetection.Engine != GameDetector.EngineType.Unknown || pDetection.Candidates.Count > 0;
	}

	private GameCompatibilityStatus DetermineCompatibility(GameDetector.DetectionResult pDetection)
	{
		var report = pDetection.Report;
		if (report.IsMalformed || report.SelectedCandidate?.Status == EngineDetectionStatus.Malformed)
		{
			return GameCompatibilityStatus.Malformed;
		}
		if (report.IsAmbiguous)
		{
			return GameCompatibilityStatus.Ambiguous;
		}
		var candidate = report.SelectedCandidate;
		if (report.IsUnknown || candidate == null)
		{
			return GameCompatibilityStatus.Unknown;
		}
		if (!_runtimeRegistry.TryGet(candidate.PluginId, out var plugin) || plugin == null)
		{
			return GameCompatibilityStatus.RegistryFailure;
		}
		if (candidate.Status != EngineDetectionStatus.Supported
			|| (plugin.Metadata.Capabilities & PluginCapability.Runtime) == 0)
		{
			return GameCompatibilityStatus.DetectionOnly;
		}
		return GameCompatibilityStatus.Supported;
	}

	private void PersistEntries()
	{
		foreach (var entry in Games)
		{
			_persistedRecords[entry.Id] = CreateRecord(entry);
		}
		SaveSettings();
	}

	private void PersistEntry(GameEntry pEntry)
	{
		_persistedRecords[pEntry.Id] = CreateRecord(pEntry);
		SaveSettings();
	}

	private StoredGameRecord CreateRecord(GameEntry pEntry)
	{
		var report = pEntry.Detection.Report;
		return new StoredGameRecord
		{
			SchemaVersion = 1,
			Id = pEntry.Id,
			Path = pEntry.Path,
			Title = pEntry.Title,
			SelectedPluginId = pEntry.SelectedPluginId,
			CompatibilityStatus = pEntry.CompatibilityStatus.ToString(),
			Confidence = pEntry.Detection.Confidence.ToString(),
			DetectionScore = pEntry.DetectionScore,
			EngineId = report.SelectedCandidate?.EngineId ?? "",
			Evidence = pEntry.Detection.Evidence.ToList(),
			Candidates = report.Candidates.Select(CreateCandidateRecord).ToList(),
			Diagnostics = pEntry.Diagnostics.Select(pDiagnostic => new StoredDiagnostic
			{
				Severity = pDiagnostic.Severity.ToString(),
				Code = pDiagnostic.Code,
				Message = pDiagnostic.Message,
			}).ToList(),
		};
	}

	private static StoredCandidate CreateCandidateRecord(EngineDetectionCandidate pCandidate)
	{
		return new StoredCandidate
		{
			PluginId = pCandidate.PluginId,
			EngineId = pCandidate.EngineId,
			DisplayName = pCandidate.DisplayName,
			Generation = pCandidate.Generation,
			Version = pCandidate.EngineVersion?.ToString() ?? "",
			Score = pCandidate.Score,
			Status = pCandidate.Status.ToString(),
			Reason = pCandidate.Reason,
			Evidence = pCandidate.Evidence.ToList(),
		};
	}

	private StoredGameRecord? FindPersistedRecord(GameEntry pEntry)
	{
		if (_persistedRecords.TryGetValue(pEntry.Id, out var byId))
		{
			return byId;
		}
		return _persistedRecords.Values.FirstOrDefault(pRecord =>
			pRecord.Path.Equals(pEntry.Path, StringComparison.OrdinalIgnoreCase));
	}

	private void EnsurePersistedRecordsLoaded()
	{
		if (_persistedRecordsLoaded)
		{
			return;
		}
		var config = new ConfigFile();
		if (config.Load(_settingsPath) == Error.Ok)
		{
			LoadPersistedRecords(config);
		}
		else
		{
			_persistedRecordsLoaded = true;
		}
	}

	private void LoadPersistedRecords(ConfigFile pConfig)
	{
		_persistedRecords.Clear();
		if (!pConfig.HasSection("games"))
		{
			_persistedRecordsLoaded = true;
			return;
		}
		foreach (var key in pConfig.GetSectionKeys("games"))
		{
			var json = pConfig.GetValue("games", key, "").AsString();
			if (string.IsNullOrEmpty(json))
			{
				continue;
			}
			try
			{
				var record = JsonSerializer.Deserialize<StoredGameRecord>(json, JsonOptions);
				if (record != null && !string.IsNullOrEmpty(record.Id) && !string.IsNullOrEmpty(record.Path))
				{
					_persistedRecords[record.Id] = record;
				}
			}
			catch (JsonException)
			{
				// A damaged optional record must not prevent the legacy library setting
				// or the remaining game records from loading.
			}
		}
		_persistedRecordsLoaded = true;
	}

	private static string NormalizeSourcePath(string pPath)
	{
		if (string.IsNullOrWhiteSpace(pPath))
		{
			return "";
		}
		var normalized = pPath.StripEdges().Replace("\\", "/").SimplifyPath();
		if (normalized.StartsWith("user://") || normalized.StartsWith("res://"))
		{
			normalized = ProjectSettings.GlobalizePath(normalized);
		}
		try
		{
			return Path.GetFullPath(normalized).Replace("\\", "/").TrimEnd('/');
		}
		catch
		{
			return "";
		}
	}

	private sealed class StoredGameRecord
	{
		public int SchemaVersion { get; set; }
		public string Id { get; set; } = "";
		public string Path { get; set; } = "";
		public string Title { get; set; } = "";
		public string SelectedPluginId { get; set; } = "";
		public string CompatibilityStatus { get; set; } = "";
		public string Confidence { get; set; } = "";
		public int DetectionScore { get; set; }
		public string EngineId { get; set; } = "";
		public List<string> Evidence { get; set; } = new();
		public List<StoredCandidate> Candidates { get; set; } = new();
		public List<StoredDiagnostic> Diagnostics { get; set; } = new();
	}

	private sealed class StoredCandidate
	{
		public string PluginId { get; set; } = "";
		public string EngineId { get; set; } = "";
		public string DisplayName { get; set; } = "";
		public string Generation { get; set; } = "";
		public string Version { get; set; } = "";
		public int Score { get; set; }
		public string Status { get; set; } = "";
		public string Reason { get; set; } = "";
		public List<string> Evidence { get; set; } = new();
	}

	private sealed class StoredDiagnostic
	{
		public string Severity { get; set; } = "";
		public string Code { get; set; } = "";
		public string Message { get; set; } = "";
	}
}
