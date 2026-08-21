using System;
using System.Collections.Generic;
using Godot;
using UniversalRPG.GameDetectorNs;

namespace UniversalRPG.App.Library;

public partial class GameLibrary : RefCounted
{
	public const string SettingsPath = "user://library.cfg";
	public const int MaxScanDepth = 2;

	public class GameEntry
	{
		public string Id;
		public string Title;
		public string Path;
		public GameDetector.DetectionResult Detection;

		public GameEntry(string pPath, GameDetector.DetectionResult pDetection)
		{
			Path = pPath;
			Detection = pDetection;
			Id = pPath.Sha256Text();
			Title = string.IsNullOrEmpty(pDetection.Title) ? System.IO.Path.GetFileName(pPath) : pDetection.Title;
		}
	}

	public string RootPath { get; private set; } = "";
	public List<GameEntry> Games { get; } = new();

	private readonly GameDetector _detector = new();

	public void LoadSettings()
	{
		var defaultPath = ProjectSettings.GlobalizePath("user://games");
		var config = new ConfigFile();
		if (config.Load(SettingsPath) == Error.Ok)
		{
			RootPath = config.GetValue("library", "games_directory", defaultPath).AsString();
		}
		else
		{
			RootPath = defaultPath;
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
		Games.Clear();
		if (!DirAccess.DirExistsAbsolute(RootPath))
		{
			return Games;
		}
		ScanDirectory(RootPath, 0);
		Games.Sort((pLeft, pRight) => string.Compare(pLeft.Title, pRight.Title, StringComparison.OrdinalIgnoreCase));
		return Games;
	}

	private void ScanDirectory(string pPath, int pDepth)
	{
		var detection = _detector.Analyze(pPath);
		if (detection.Engine != GameDetector.EngineType.Unknown)
		{
			Games.Add(new GameEntry(pPath, detection));
			return;
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
			if (directoryName.StartsWith("."))
			{
				continue;
			}
			if (directory.IsLink(directoryName))
			{
				continue;
			}
			ScanDirectory(pPath.PathJoin(directoryName), pDepth + 1);
		}
	}

	private void SaveSettings()
	{
		var config = new ConfigFile();
		config.Load(SettingsPath);
		config.SetValue("library", "games_directory", RootPath);
		config.Save(SettingsPath);
	}
}
