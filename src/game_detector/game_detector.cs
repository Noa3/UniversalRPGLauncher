using System;
using System.Collections.Generic;
using Godot;
using UniversalRPG.Core;

namespace UniversalRPG.GameDetectorNs;

public partial class GameDetector : RefCounted
{
	public enum EngineType
	{
		Unknown,
		RpgMaker2000,
		RpgMaker2003,
		RpgMakerXp,
		RpgMakerVx,
		RpgMakerVxAce,
		RpgMakerMv,
		RpgMakerMz,
		RpgMaker2000_2003,
	}

	public enum Confidence
	{
		Low,
		Medium,
		High,
	}

	public static readonly EngineType[] DetectableEngines =
	{
		EngineType.RpgMaker2000,
		EngineType.RpgMaker2003,
		EngineType.RpgMakerXp,
		EngineType.RpgMakerVx,
		EngineType.RpgMakerVxAce,
		EngineType.RpgMakerMv,
		EngineType.RpgMakerMz,
	};

	public const int MaxMetadataBytes = 1024 * 1024;

	private readonly LegacyTextDecoder _textDecoder = new();

	public class DetectionResult
	{
		public EngineType Engine { get; set; } = EngineType.Unknown;
		public Confidence Confidence { get; set; } = Confidence.Low;
		public List<string> Evidence { get; } = new();
		public string Title { get; set; } = "";
		public string RtpDependency { get; set; } = "";
		public bool HasCustomScripts { get; set; }
		public bool HasNativeLibraries { get; set; }
		public bool HasEncryptedArchives { get; set; }
		public List<string> UnknownRuntimes { get; } = new();
		public string GameDirectory { get; set; } = "";

		public string GetEngineName()
		{
			return Engine switch
			{
				EngineType.RpgMaker2000 => "RPG Maker 2000",
				EngineType.RpgMaker2003 => "RPG Maker 2003",
				EngineType.RpgMaker2000_2003 => "RPG Maker 2000/2003",
				EngineType.RpgMakerXp => "RPG Maker XP",
				EngineType.RpgMakerVx => "RPG Maker VX",
				EngineType.RpgMakerVxAce => "RPG Maker VX Ace",
				EngineType.RpgMakerMv => "RPG Maker MV",
				EngineType.RpgMakerMz => "RPG Maker MZ",
				_ => "Unknown",
			};
		}

		public string GetConfidenceString()
		{
			return Confidence switch
			{
				Confidence.High => TranslationServer.Translate("CONFIDENCE_HIGH"),
				Confidence.Medium => TranslationServer.Translate("CONFIDENCE_MEDIUM"),
				_ => TranslationServer.Translate("CONFIDENCE_LOW"),
			};
		}

		public string Describe()
		{
			var text = $"Detected engine: {GetEngineName()}\nConfidence: {GetConfidenceString()}\nEvidence:\n";
			foreach (var item in Evidence)
			{
				text += $"- {item}\n";
			}
			return text;
		}
	}

	public DetectionResult Analyze(string pGameDirectory)
	{
		var result = new DetectionResult { GameDirectory = pGameDirectory };
		if (!DirAccess.DirExistsAbsolute(pGameDirectory))
		{
			return result;
		}

		var scores = new Dictionary<EngineType, int>();
		var evidence = new Dictionary<EngineType, List<string>>();
		foreach (var engine in DetectableEngines)
		{
			scores[engine] = 0;
			evidence[engine] = new List<string>();
		}

		InspectLcf(pGameDirectory, scores, evidence);
		InspectRgss(pGameDirectory, scores, evidence, result);
		InspectMvMz(pGameDirectory, scores, evidence);

		var bestScore = 0;
		var bestEngines = new List<EngineType>();
		foreach (var engine in DetectableEngines)
		{
			var score = scores[engine];
			if (score > bestScore)
			{
				bestScore = score;
				bestEngines = new List<EngineType> { engine };
			}
			else if (score == bestScore && score > 0)
			{
				bestEngines.Add(engine);
			}
		}

		if (bestEngines.Contains(EngineType.RpgMaker2000) && bestEngines.Contains(EngineType.RpgMaker2003))
		{
			result.Engine = EngineType.RpgMaker2000_2003;
			result.Evidence.AddRange(evidence[EngineType.RpgMaker2000]);
			result.Evidence.Add(Tr("DETECT_LCF_VERSION_AMBIGUOUS"));
		}
		else if (bestEngines.Count > 0)
		{
			result.Engine = bestEngines[0];
			result.Evidence.AddRange(evidence[result.Engine]);
		}

		if (bestScore >= 7)
		{
			result.Confidence = Confidence.High;
		}
		else if (bestScore >= 4)
		{
			result.Confidence = Confidence.Medium;
		}

		result.Title = ReadGameTitle(pGameDirectory);
		result.HasCustomScripts = HasCustomScripts(pGameDirectory);
		result.HasNativeLibraries = HasNativeLibraries(pGameDirectory);
		CollectUnknownLibraries(pGameDirectory, result);
		return result;
	}

	private void InspectLcf(string pDir, Dictionary<EngineType, int> pScores, Dictionary<EngineType, List<string>> pEvidence)
	{
		var hasDatabase = HasFile(pDir, "RPG_RT.ldb");
		var hasMapTree = HasFile(pDir, "RPG_RT.lmt");
		if (hasDatabase && hasMapTree)
		{
			Score(pScores, pEvidence, EngineType.RpgMaker2000, 6, Tr("DETECT_LCF_DATABASE"));
			Score(pScores, pEvidence, EngineType.RpgMaker2003, 6, Tr("DETECT_LCF_DATABASE"));
		}

		var mapCount = CountExtension(pDir, ".lmu");
		if (mapCount > 0)
		{
			var mapMessage = Tr("DETECT_LCF_MAPS").Replace("{count}", mapCount.ToString());
			Score(pScores, pEvidence, EngineType.RpgMaker2000, 2, mapMessage);
			Score(pScores, pEvidence, EngineType.RpgMaker2003, 2, mapMessage);
		}

		var iniPath = FindFile(pDir, "RPG_RT.ini");
		if (string.IsNullOrEmpty(iniPath))
		{
			iniPath = FindFile(pDir, "Game.ini");
		}
		if (string.IsNullOrEmpty(iniPath))
		{
			return;
		}
		var content = ReadText(iniPath).ToLowerInvariant();
		if (content.Contains("engineid=rm2000"))
		{
			Score(pScores, pEvidence, EngineType.RpgMaker2000, 7, Tr("DETECT_INI_RM2000"));
		}
		else if (content.Contains("engineid=rm2003"))
		{
			Score(pScores, pEvidence, EngineType.RpgMaker2003, 7, Tr("DETECT_INI_RM2003"));
		}
		else if (content.Contains("[rpg_rt]"))
		{
			Score(pScores, pEvidence, EngineType.RpgMaker2000, 1, Tr("DETECT_RPG_RT_INI"));
			Score(pScores, pEvidence, EngineType.RpgMaker2003, 1, Tr("DETECT_RPG_RT_INI"));
		}
	}

	private void InspectRgss(
		string pDir,
		Dictionary<EngineType, int> pScores,
		Dictionary<EngineType, List<string>> pEvidence,
		DetectionResult pResult
	)
	{
		var iniPath = FindFile(pDir, "Game.ini");
		if (!string.IsNullOrEmpty(iniPath))
		{
			var content = ReadText(iniPath).ToLowerInvariant();
			if (content.Contains("rgss1"))
			{
				Score(pScores, pEvidence, EngineType.RpgMakerXp, 6, Tr("DETECT_RGSS1_INI"));
				pResult.RtpDependency = ReadIniValue(iniPath, "RTP1");
			}
			else if (content.Contains("rgss2"))
			{
				Score(pScores, pEvidence, EngineType.RpgMakerVx, 6, Tr("DETECT_RGSS2_INI"));
				pResult.RtpDependency = ReadIniValue(iniPath, "RTP");
			}
			else if (content.Contains("rgss3"))
			{
				Score(pScores, pEvidence, EngineType.RpgMakerVxAce, 6, Tr("DETECT_RGSS3_INI"));
				pResult.RtpDependency = ReadIniValue(iniPath, "RTP");
			}
		}

		ScoreForFilePrefix(pDir, "rgss1", EngineType.RpgMakerXp, pScores, pEvidence);
		ScoreForFilePrefix(pDir, "rgss2", EngineType.RpgMakerVx, pScores, pEvidence);
		ScoreForFilePrefix(pDir, "rgss3", EngineType.RpgMakerVxAce, pScores, pEvidence);

		foreach (var fileName in DirAccess.GetFilesAt(pDir))
		{
			var lower = fileName.ToLowerInvariant();
			if (lower.EndsWith(".rgssad"))
			{
				Score(pScores, pEvidence, EngineType.RpgMakerXp, 5, Tr("DETECT_RGSSAD"));
				pResult.HasEncryptedArchives = true;
			}
			else if (lower.EndsWith(".rgss2a"))
			{
				Score(pScores, pEvidence, EngineType.RpgMakerVx, 5, Tr("DETECT_RGSS2A"));
				pResult.HasEncryptedArchives = true;
			}
			else if (lower.EndsWith(".rgss3a"))
			{
				Score(pScores, pEvidence, EngineType.RpgMakerVxAce, 5, Tr("DETECT_RGSS3A"));
				pResult.HasEncryptedArchives = true;
			}
		}

		var dataDir = FindDirectory(pDir, "Data");
		if (string.IsNullOrEmpty(dataDir))
		{
			return;
		}
		var dataFiles = DirAccess.GetFilesAt(dataDir);
		if (ArrayHasExtension(dataFiles, ".rxdata"))
		{
			Score(pScores, pEvidence, EngineType.RpgMakerXp, 3, Tr("DETECT_XP_DATA"));
		}
		if (ArrayHasExtension(dataFiles, ".rvdata"))
		{
			Score(pScores, pEvidence, EngineType.RpgMakerVx, 3, Tr("DETECT_VX_DATA"));
		}
		if (ArrayHasExtension(dataFiles, ".rvdata2"))
		{
			Score(pScores, pEvidence, EngineType.RpgMakerVxAce, 3, Tr("DETECT_VXA_DATA"));
		}
	}

	private void InspectMvMz(string pDir, Dictionary<EngineType, int> pScores, Dictionary<EngineType, List<string>> pEvidence)
	{
		var webRoot = pDir;
		var wwwDir = FindDirectory(pDir, "www");
		if (!string.IsNullOrEmpty(wwwDir))
		{
			webRoot = wwwDir;
		}

		var jsDir = FindDirectory(webRoot, "js");
		var dataDir = FindDirectory(webRoot, "data");
		var hasIndex = HasFile(webRoot, "index.html");
		if (string.IsNullOrEmpty(jsDir) || string.IsNullOrEmpty(dataDir) || !hasIndex)
		{
			return;
		}

		if (HasFile(jsDir, "rmmz_core.js") || HasFile(jsDir, "rmmz_managers.js"))
		{
			Score(pScores, pEvidence, EngineType.RpgMakerMz, 9, Tr("DETECT_MZ_RUNTIME"));
		}
		else if (HasFile(jsDir, "rpg_core.js") || HasFile(jsDir, "rpg_managers.js"))
		{
			Score(pScores, pEvidence, EngineType.RpgMakerMv, 9, Tr("DETECT_MV_RUNTIME"));
		}
		else
		{
			Score(pScores, pEvidence, EngineType.RpgMakerMv, 3, Tr("DETECT_MV_MZ_GENERIC"));
		}
	}

	private void ScoreForFilePrefix(
		string pDir,
		string pPrefix,
		EngineType pEngine,
		Dictionary<EngineType, int> pScores,
		Dictionary<EngineType, List<string>> pEvidence
	)
	{
		foreach (var fileName in DirAccess.GetFilesAt(pDir))
		{
			var lower = fileName.ToLowerInvariant();
			if (lower.StartsWith(pPrefix) && lower.EndsWith(".dll"))
			{
				Score(pScores, pEvidence, pEngine, 5, Tr("DETECT_FILE_FOUND").Replace("{file}", fileName));
				return;
			}
		}
	}

	private static void Score(
		Dictionary<EngineType, int> pScores,
		Dictionary<EngineType, List<string>> pEvidence,
		EngineType pEngine,
		int pPoints,
		string pMessage
	)
	{
		pScores[pEngine] += pPoints;
		if (!pEvidence[pEngine].Contains(pMessage))
		{
			pEvidence[pEngine].Add(pMessage);
		}
	}

	private string ReadGameTitle(string pDir)
	{
		foreach (var iniName in new[] { "RPG_RT.ini", "Game.ini" })
		{
			var iniPath = FindFile(pDir, iniName);
			if (string.IsNullOrEmpty(iniPath))
			{
				continue;
			}
			foreach (var key in new[] { "GameTitle", "Title" })
			{
				var title = ReadIniValue(iniPath, key);
				if (!string.IsNullOrEmpty(title))
				{
					return title;
				}
			}
		}

		var webRoot = pDir;
		var wwwDir = FindDirectory(pDir, "www");
		if (!string.IsNullOrEmpty(wwwDir))
		{
			webRoot = wwwDir;
		}
		var dataDir = FindDirectory(webRoot, "data");
		if (string.IsNullOrEmpty(dataDir))
		{
			return "";
		}
		var systemPath = FindFile(dataDir, "System.json");
		if (string.IsNullOrEmpty(systemPath))
		{
			return "";
		}
		var parsed = Json.ParseString(ReadText(systemPath));
		if (parsed.VariantType == Variant.Type.Dictionary)
		{
			var dict = parsed.AsGodotDictionary();
			if (dict.ContainsKey("gameTitle"))
			{
				return parsed.AsGodotDictionary()["gameTitle"].AsString();
			}
		}
		return "";
	}

	private static string ReadIniValue(string pPath, string pKey)
	{
		foreach (var line in ReadTextStatic(pPath).Split('\n'))
		{
			var stripped = line.Trim();
			var separator = stripped.IndexOf('=');
			if (separator < 0)
			{
				continue;
			}
			if (stripped[..separator].Trim().Equals(pKey, StringComparison.OrdinalIgnoreCase))
			{
				return stripped[(separator + 1)..].Trim();
			}
		}
		return "";
	}

	private string ReadText(string pPath)
	{
		return ReadTextStatic(pPath);
	}

	private static string ReadTextStatic(string pPath)
	{
		using var file = FileAccess.Open(pPath, FileAccess.ModeFlags.Read);
		if (file == null)
		{
			return "";
		}
		if (file.GetLength() > MaxMetadataBytes)
		{
			return "";
		}
		var decoder = new LegacyTextDecoder();
		return decoder.Decode(file.GetBuffer((long)file.GetLength()));
	}

	private bool HasFile(string pDir, string pName)
	{
		return !string.IsNullOrEmpty(FindFile(pDir, pName));
	}

	private static string FindFile(string pDir, string pName)
	{
		using var directory = DirAccess.Open(pDir);
		if (directory == null)
		{
			return "";
		}
		foreach (var fileName in directory.GetFiles())
		{
			if (fileName.Equals(pName, StringComparison.OrdinalIgnoreCase))
			{
				if (directory.IsLink(fileName))
				{
					return "";
				}
				return PathJoin(pDir, fileName);
			}
		}
		return "";
	}

	private static string FindDirectory(string pDir, string pName)
	{
		using var directory = DirAccess.Open(pDir);
		if (directory == null)
		{
			return "";
		}
		foreach (var directoryName in directory.GetDirectories())
		{
			if (!directoryName.Equals(pName, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			// Imported games are untrusted. Do not follow a Data/www/js/Scripts
			// symlink or junction out of the selected game directory.
			if (directory.IsLink(directoryName))
			{
				return "";
			}
			return PathJoin(pDir, directoryName);
		}
		return "";
	}

	private static int CountExtension(string pDir, string pExtension)
	{
		var count = 0;
		foreach (var fileName in DirAccess.GetFilesAt(pDir))
		{
			if (fileName.ToLowerInvariant().EndsWith(pExtension))
			{
				count += 1;
			}
		}
		return count;
	}

	private static bool ArrayHasExtension(string[] pFiles, string pExtension)
	{
		foreach (var fileName in pFiles)
		{
			if (fileName.ToLowerInvariant().EndsWith(pExtension))
			{
				return true;
			}
		}
		return false;
	}

	private static bool HasCustomScripts(string pDir)
	{
		foreach (var fileName in DirAccess.GetFilesAt(pDir))
		{
			var lower = fileName.ToLowerInvariant();
			if (lower.EndsWith(".rb") || lower.EndsWith(".js"))
			{
				return true;
			}
		}
		foreach (var directoryName in new[] { "js", "Scripts" })
		{
			var directory = FindDirectory(pDir, directoryName);
			if (string.IsNullOrEmpty(directory))
			{
				continue;
			}
			foreach (var fileName in DirAccess.GetFilesAt(directory))
			{
				var lower = fileName.ToLowerInvariant();
				if (lower.EndsWith(".rb") || lower.EndsWith(".js"))
				{
					return true;
				}
			}
		}
		return false;
	}

	private static bool HasNativeLibraries(string pDir)
	{
		foreach (var fileName in DirAccess.GetFilesAt(pDir))
		{
			var lower = fileName.ToLowerInvariant();
			if (lower.EndsWith(".dll") || lower.EndsWith(".so") || lower.EndsWith(".dylib") || lower.EndsWith(".exe"))
			{
				return true;
			}
		}
		return false;
	}

	private static void CollectUnknownLibraries(string pDir, DetectionResult pResult)
	{
		foreach (var fileName in DirAccess.GetFilesAt(pDir))
		{
			var lower = fileName.ToLowerInvariant();
			if (!(lower.EndsWith(".dll") || lower.EndsWith(".so") || lower.EndsWith(".dylib")))
			{
				continue;
			}
			if (!lower.StartsWith("rgss") && !lower.StartsWith("rpg_rt"))
			{
				pResult.UnknownRuntimes.Add(fileName);
			}
		}
	}

	private static string PathJoin(string pLeft, string pRight)
	{
		return pLeft.PathJoin(pRight);
	}
}
