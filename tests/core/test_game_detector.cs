using System.Collections.Generic;
using Godot;
using UniversalRPG.GameDetectorNs;
using UniversalRPG.Tests.Framework;
using static UniversalRPG.GameDetectorNs.GameDetector;

namespace UniversalRPG.Tests.Core;

public partial class TestGameDetector : TestBase
{
	private const string TempBase = "user://detector_test";

	private GameDetector _detector = null!;

	public override void Setup()
	{
		_detector = new GameDetector();
		DirAccess.MakeDirRecursiveAbsolute(TempBase);
		CreateRm2000Game();
		CreateRm2003Game();
		CreateXpGame();
		CreateVxAceGame();
		CreateMvGame();
		CreateUnknownGame();
	}

	public override void Teardown()
	{
		CleanupDir(TempBase);
	}

	private static void CreateRm2000Game()
	{
		var dir = TempBase.PathJoin("RM2000_Test");
		DirAccess.MakeDirRecursiveAbsolute(dir.PathJoin("Data"));
		DirAccess.MakeDirRecursiveAbsolute(dir.PathJoin("Graphics"));
		DirAccess.MakeDirRecursiveAbsolute(dir.PathJoin("Maps"));
		DirAccess.MakeDirRecursiveAbsolute(dir.PathJoin("Images"));

		WriteText(dir.PathJoin("Game.ini"), "[Game]\nTitle=TestRM2000\nEngineID=RM2000\n");
		WriteText(dir.PathJoin("RPG_RT.ldb"), "database");
		WriteText(dir.PathJoin("RPG_RT.lmt"), "map_tree");
		WriteText(dir.PathJoin("Map0001.lmu"), "map_data");
		WriteText(dir.PathJoin("Graphics/Characters/hero.png"), "char_data");
	}

	private static void CreateRm2003Game()
	{
		var dir = TempBase.PathJoin("RM2003_Test");
		DirAccess.MakeDirRecursiveAbsolute(dir.PathJoin("Data"));
		DirAccess.MakeDirRecursiveAbsolute(dir.PathJoin("Graphics"));
		DirAccess.MakeDirRecursiveAbsolute(dir.PathJoin("Maps"));
		DirAccess.MakeDirRecursiveAbsolute(dir.PathJoin("Images"));

		WriteText(dir.PathJoin("Game.ini"), "[Game]\nTitle=TestRM2003\nEngineID=RM2003\n");
		WriteText(dir.PathJoin("RPG_RT.ldb"), "database");
		WriteText(dir.PathJoin("RPG_RT.lmt"), "map_tree");
		WriteText(dir.PathJoin("Map0001.lmu"), "map_data");
		WriteText(dir.PathJoin("Data/Map001.rxdata"), "map_data");
		WriteText(dir.PathJoin("RPG_RT.exe"), "rpg_rt_binary");
	}

	private static void CreateXpGame()
	{
		var dir = TempBase.PathJoin("RMXP_Test");
		DirAccess.MakeDirRecursiveAbsolute(dir.PathJoin("Data"));
		DirAccess.MakeDirRecursiveAbsolute(dir.PathJoin("Graphics"));
		DirAccess.MakeDirRecursiveAbsolute(dir.PathJoin("System"));

		WriteText(dir.PathJoin("Game.ini"), "[Game]\nTitle=TestXP\nLibrary=RGSS102A.dll\nRTP1=Standard\n");
		WriteText(dir.PathJoin("RGSS102A.dll"), "rgss1_dll");
		WriteText(dir.PathJoin("Data/Map001.rxdata"), "map_data");
	}

	private static void CreateVxAceGame()
	{
		var dir = TempBase.PathJoin("RMVXAce_Test");
		DirAccess.MakeDirRecursiveAbsolute(dir.PathJoin("Data"));
		DirAccess.MakeDirRecursiveAbsolute(dir.PathJoin("Graphics"));
		DirAccess.MakeDirRecursiveAbsolute(dir.PathJoin("Pictures"));
		DirAccess.MakeDirRecursiveAbsolute(dir.PathJoin("Animations"));

		WriteText(dir.PathJoin("Game.ini"), "[Game]\nTitle=TestVXAce\nLibrary=RGSS302A.dll\nRTP=RPGVXAce\n");
		WriteText(dir.PathJoin("RGSS302A.dll"), "rgss3_dll");
		WriteText(dir.PathJoin("Data/Map001.rvdata2"), "rvdata2_data");
		WriteText(dir.PathJoin("Data/Save001.rxdata"), "save_data");
	}

	private static void CreateMvGame()
	{
		var dir = TempBase.PathJoin("RMVV_Test");
		DirAccess.MakeDirRecursiveAbsolute(dir.PathJoin("data"));
		DirAccess.MakeDirRecursiveAbsolute(dir.PathJoin("img"));
		DirAccess.MakeDirRecursiveAbsolute(dir.PathJoin("js/plugins"));

		WriteText(dir.PathJoin("index.html"), "<!DOCTYPE html><html><body>RPG Maker MV</body></html>");
		WriteText(dir.PathJoin("package.json"), "{\"name\":\"rmmv\",\"version\":\"1.6.0\"}");
		WriteText(dir.PathJoin("data/Map001.json"), "{\"id\":1,\"name\":\"Test\"}");
		WriteText(dir.PathJoin("js/rpg_core.js"), "// MV runtime\n");
		WriteText(dir.PathJoin("js/plugins/TestPlugin.js"), "// Test plugin\n");
	}

	private static void CreateUnknownGame()
	{
		var dir = TempBase.PathJoin("Unknown_Test");
		DirAccess.MakeDirRecursiveAbsolute(dir);
		WriteText(dir.PathJoin("readme.txt"), "This is not an RPG Maker game");
		WriteText(dir.PathJoin("data.bin"), "random_data");
	}

	private GameDetector.DetectionResult Analyze(string pRelativePath)
	{
		return _detector.Analyze(ProjectSettings.GlobalizePath(TempBase.PathJoin(pRelativePath)));
	}

	// === TESTS: RM2000 Detection ===

	public void Test_DetectRmgm2000()
	{
		var result = Analyze("RM2000_Test");
		AssertEq(result.Engine, EngineType.RpgMaker2000);
		AssertTrue(result.Confidence >= Confidence.Medium);
		AssertTrue(result.Evidence.Count > 0);
	}

	public void Test_DetectRmgm2000Evidence()
	{
		var result = Analyze("RM2000_Test");
		var hasLcfDatabase = false;
		foreach (var evidence in result.Evidence)
		{
			if (evidence.Contains("RPG_RT.ldb"))
			{
				hasLcfDatabase = true;
			}
		}
		AssertTrue(hasLcfDatabase);
	}

	// === TESTS: RM2003 Detection ===

	public void Test_DetectRmgm2003()
	{
		var result = Analyze("RM2003_Test");
		AssertEq(result.Engine, EngineType.RpgMaker2003);
		AssertTrue(result.Confidence >= Confidence.Medium);
	}

	public void Test_DetectRmgm2003Evidence()
	{
		var result = Analyze("RM2003_Test");
		AssertTrue(result.HasNativeLibraries);
	}

	// === TESTS: RPG Maker XP Detection ===

	public void Test_DetectRmgmXp()
	{
		var result = Analyze("RMXP_Test");
		AssertEq(result.Engine, EngineType.RpgMakerXp);
		AssertTrue(result.Confidence >= Confidence.High);
	}

	public void Test_DetectRmgmXpRgss()
	{
		var result = Analyze("RMXP_Test");
		AssertTrue(result.HasNativeLibraries);
		AssertNe(result.RtpDependency, "");
	}

	// === TESTS: RPG Maker VX Ace Detection ===

	public void Test_DetectRmgmVxAce()
	{
		var result = Analyze("RMVXAce_Test");
		AssertEq(result.Engine, EngineType.RpgMakerVxAce);
		AssertTrue(result.Confidence >= Confidence.High);
	}

	public void Test_DetectRmgmVxAceArchives()
	{
		var result = Analyze("RMVXAce_Test");
		AssertFalse(result.HasEncryptedArchives);
		AssertTrue(result.HasNativeLibraries);
	}

	// === TESTS: RPG Maker MV Detection ===

	public void Test_DetectRmgmMv()
	{
		var result = Analyze("RMVV_Test");
		AssertEq(result.Engine, EngineType.RpgMakerMv);
		AssertTrue(result.Confidence >= Confidence.Medium);
	}

	public void Test_DetectRmgmMvStructure()
	{
		var result = Analyze("RMVV_Test");
		AssertTrue(result.HasCustomScripts);
		var hasRuntime = false;
		foreach (var evidence in result.Evidence)
		{
			if (evidence.ToLowerInvariant().Contains("javascript"))
			{
				hasRuntime = true;
			}
		}
		AssertTrue(hasRuntime);
	}

	// === TESTS: Unknown Game ===

	public void Test_DetectUnknown()
	{
		var result = Analyze("Unknown_Test");
		AssertEq(result.Engine, EngineType.Unknown);
		AssertTrue(result.Confidence <= Confidence.Low);
	}

	// === TESTS: Non-Existent Directory ===

	public void Test_DetectNonexistent()
	{
		var result = Analyze("NonExistent");
		AssertEq(result.Engine, EngineType.Unknown);
		AssertEq(result.Confidence, Confidence.Low);
		AssertEq(result.Evidence.Count, 0);
	}

	// === TESTS: DetectionResult Helpers ===

	public void Test_GetEngineName()
	{
		var result = new GameDetector.DetectionResult { Engine = EngineType.RpgMakerVxAce };
		AssertEq(result.GetEngineName(), "RPG Maker VX Ace");
	}

	public void Test_GetConfidenceString()
	{
		var result = new GameDetector.DetectionResult();

		result.Confidence = Confidence.High;
		AssertEq(result.GetConfidenceString(), "High");

		result.Confidence = Confidence.Medium;
		AssertEq(result.GetConfidenceString(), "Medium");

		result.Confidence = Confidence.Low;
		AssertEq(result.GetConfidenceString(), "Low");
	}

	public void Test_Describe()
	{
		var result = new GameDetector.DetectionResult
		{
			Engine = EngineType.RpgMakerXp,
			Confidence = Confidence.High,
		};
		result.Evidence.Add("Found Game.ini");
		result.Evidence.Add("Found RGSS102A.dll");

		var text = result.Describe();
		AssertTrue(text.Contains("RPG Maker XP"));
		AssertTrue(text.Contains("High"));
		AssertTrue(text.Contains("Game.ini"));
		AssertTrue(text.Contains("RGSS102A.dll"));
	}

	private static void WriteText(string pPath, string pText)
	{
		DirAccess.MakeDirRecursiveAbsolute(pPath.GetBaseDir());
		using var file = FileAccess.Open(pPath, FileAccess.ModeFlags.Write);
		file?.StoreString(pText);
	}

	private static void CleanupDir(string pDir)
	{
		if (!DirAccess.DirExistsAbsolute(pDir))
		{
			return;
		}
		using var directory = DirAccess.Open(pDir);
		if (directory == null)
		{
			return;
		}
		foreach (var child in directory.GetDirectories())
		{
			CleanupDir(pDir.PathJoin(child));
		}
		foreach (var fileName in directory.GetFiles())
		{
			DirAccess.RemoveAbsolute(pDir.PathJoin(fileName));
		}
		DirAccess.RemoveAbsolute(pDir);
	}
}
