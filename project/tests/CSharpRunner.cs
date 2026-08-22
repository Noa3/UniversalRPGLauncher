using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;
using UniversalRPG.App.Library;
using UniversalRPG.Core;
using UniversalRPG.GameDetectorNs;
using UniversalRPG.Rm2k.Parser;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests;

/// <summary>
/// Headless C# test runner. Runs every TestBase-derived suite in the assembly
/// plus the integration smoke checks, then quits with the failure count.
/// </summary>
public partial class CSharpRunner : Node
{
	private const string Root = "user://smoke_test";

	private readonly List<string> _failures = new();
	private int _total;
	private int _passed;

	public override void _Ready()
	{
		TranslationServer.SetLocale("en");
		RunSuites();
		RunSmokeTests();

		if (_failures.Count == 0)
		{
			GD.Print($"All {_total} tests passed");
			Cleanup(Root);
			GetTree().Quit(0);
			return;
		}

		GD.Print($"{_failures.Count}/{_total} tests failed");
		foreach (var failure in _failures)
		{
			GD.PushError(failure);
		}
		Cleanup(Root);
		GetTree().Quit(_failures.Count);
	}

	private void RunSuites()
	{
		var suiteTypes = Assembly.GetExecutingAssembly()
			.GetTypes()
			.Where(pType => pType.IsClass && !pType.IsAbstract && typeof(TestBase).IsAssignableFrom(pType))
			.OrderBy(pType => pType.Name, StringComparer.Ordinal);

		foreach (var suiteType in suiteTypes)
		{
			var suite = Activator.CreateInstance(suiteType) as TestBase;
			if (suite == null)
			{
				_failures.Add($"{suiteType.Name}: could not create test suite instance");
				continue;
			}
			TestBase.SuiteResult result;
			try
			{
				result = suite.RunAll();
			}
			catch (Exception exception)
			{
				_failures.Add($"{suiteType.Name}: crashed: {exception.Message}");
				continue;
			}
			finally
			{
				suite.Dispose();
			}
			_total += result.Tests;
			_passed += result.Passed;
			var label = suiteType.Name;
			if (result.Failed > 0)
			{
				_failures.Add($"{label}: {result.Failed}/{result.Tests} tests failed");
				foreach (var failure in result.Failures)
				{
					_failures.Add("  " + failure);
				}
			}
			GD.Print($"{label}: {result.Passed}/{result.Tests} passed");
		}
	}

	private void RunSmokeTests()
	{
		Cleanup(Root);
		DirAccess.MakeDirRecursiveAbsolute(Root);
		SmokeCp932();
		SmokeLcfDetection();
		SmokeLcfParser();
		SmokeMzDetection();
		SmokeLibraryScan();
		SmokeTranslation();
		SmokeUiScene();
	}

	private void SmokeCp932()
	{
		byte[] bytes = { 0x83, 0x65, 0x83, 0x58, 0x83, 0x67 };
		Check(new LegacyTextDecoder().Decode(bytes) == "テスト", "CP932 title decoding");
	}

	private void SmokeLcfDetection()
	{
		var gameDir = Root.PathJoin("JapaneseLCF");
		DirAccess.MakeDirRecursiveAbsolute(gameDir);
		WriteBytes(gameDir.PathJoin("RPG_RT.ldb"), new byte[] { 0x0b });
		WriteBytes(gameDir.PathJoin("RPG_RT.lmt"), new byte[] { 0x0a });
		WriteBytes(gameDir.PathJoin("Map0001.lmu"), new byte[] { 0x09 });
		var ini = new List<byte>();
		ini.AddRange("[RPG_RT]\nGameTitle=".ToUtf8Buffer());
		ini.AddRange(new byte[] { 0x83, 0x65, 0x83, 0x58, 0x83, 0x67 });
		ini.AddRange("\n".ToUtf8Buffer());
		WriteBytes(gameDir.PathJoin("RPG_RT.ini"), ini.ToArray());
		var result = new GameDetector().Analyze(ProjectSettings.GlobalizePath(gameDir));
		Check(result.Engine == GameDetector.EngineType.RpgMaker2000_2003, "LCF family detection");
		Check(result.Title == "テスト", "LCF CP932 title");
		Check(result.Confidence == GameDetector.Confidence.High, "LCF detection confidence");
	}

	private void SmokeLcfParser()
	{
		var parserDir = "user://smoke_lcf_parser";
		DirAccess.MakeDirRecursiveAbsolute(parserDir);
		var db = new List<byte>();
		db.AddRange(Ber(11));
		db.AddRange("LcfDataBase".ToAsciiBuffer());
		db.AddRange(Chunk(0x1a, Ber(259)));
		db.AddRange(Chunk(0x0b, Ber(0)));
		db.AddRange(new byte[] { 0x00 });
		var dbPath = parserDir.PathJoin("RPG_RT.ldb");
		WriteBytes(dbPath, db.ToArray());
		var parser = new Rm2kParser();
		var result = parser.ParseDatabase(dbPath);
		Check(result.IsSuccess(), "LCF LDB parse success");
		if (result.IsSuccess())
		{
			var data = result.GetData();
			Check((int)data["version"] == 259, "LCF LDB version");
			Check(((Godot.Collections.Dictionary)data["section_counts"])["actors"].AsInt32() == 0,
				"LCF LDB actors count");
		}
		Cleanup(parserDir);
	}

	private void SmokeMzDetection()
	{
		var gameDir = Root.PathJoin("MZGame");
		DirAccess.MakeDirRecursiveAbsolute(gameDir.PathJoin("js"));
		DirAccess.MakeDirRecursiveAbsolute(gameDir.PathJoin("data"));
		WriteText(gameDir.PathJoin("index.html"), "<!doctype html>");
		WriteText(gameDir.PathJoin("js/rmmz_core.js"), "// runtime");
		WriteText(gameDir.PathJoin("js/rmmz_managers.js"), "// runtime");
		WriteText(gameDir.PathJoin("data/System.json"), "{\"gameTitle\":\"MZ Test\"}");
		var result = new GameDetector().Analyze(ProjectSettings.GlobalizePath(gameDir));
		Check(result.Engine == GameDetector.EngineType.RpgMakerMz, "MZ detection");
		Check(result.Title == "MZ Test", "MZ title");
	}

	private void SmokeLibraryScan()
	{
		var settingsPath = "user://smoke_library.cfg";
		DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(settingsPath));
		var library = new GameLibrary(pSettingsPath: settingsPath);
		library.SetRootPath(ProjectSettings.GlobalizePath(Root), false);
		var games = library.Scan();
		Check(games.Count == 2, "Library scans recognized child directories");
		DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(settingsPath));
	}

	private void SmokeTranslation()
	{
		TranslationServer.SetLocale("ja");
		Check(TranslationServer.Translate("ACTION_RESCAN") == "再スキャン", "Japanese UI catalog");
		foreach (var locale in new[] { "en", "de", "es", "fr", "ja", "ko", "zh_CN" })
		{
			TranslationServer.SetLocale(locale);
			Check(TranslationServer.Translate("ACTION_RESCAN") != "ACTION_RESCAN", $"{locale} UI catalog");
		}
		TranslationServer.SetLocale("en");
	}

	private void SmokeUiScene()
	{
		var packedScene = GD.Load<PackedScene>("res://scenes/main.tscn");
		Check(packedScene != null, "Load main scene");
		if (packedScene == null)
		{
			return;
		}
		var instance = packedScene.Instantiate();
		Check(instance != null, "Instantiate main scene");
		if (instance == null)
		{
			return;
		}
		AddChild(instance);
		instance.QueueFree();
	}

	private static byte[] Ber(int pValue)
	{
		var value = pValue;
		var groups = new List<byte>();
		while (value >= 0x80)
		{
			groups.Add((byte)(value & 0x7f));
			value >>= 7;
		}
		groups.Add((byte)value);
		var bytes = new List<byte>();
		for (var index = groups.Count - 1; index >= 0; index--)
		{
			var current = groups[index];
			if (index > 0)
			{
				current |= 0x80;
			}
			bytes.Add(current);
		}
		return bytes.ToArray();
	}

	private static byte[] Chunk(int pId, byte[] pPayload)
	{
		var bytes = new List<byte>();
		bytes.AddRange(Ber(pId));
		bytes.AddRange(Ber(pPayload.Length));
		bytes.AddRange(pPayload);
		return bytes.ToArray();
	}

	private void Check(bool pCondition, string pName)
	{
		_total += 1;
		if (pCondition)
		{
			_passed += 1;
			return;
		}
		_failures.Add($"Smoke test failed: {pName}");
	}

	private static void WriteText(string pPath, string pText)
	{
		WriteBytes(pPath, pText.ToUtf8Buffer());
	}

	private static void WriteBytes(string pPath, byte[] pBytes)
	{
		using var file = FileAccess.Open(pPath, FileAccess.ModeFlags.Write);
		if (file == null)
		{
			GD.PushError("Create fixture failed: " + pPath);
			return;
		}
		file.StoreBuffer(pBytes);
	}

	private static void Cleanup(string pPath)
	{
		if (!DirAccess.DirExistsAbsolute(pPath))
		{
			return;
		}
		using var directory = DirAccess.Open(pPath);
		if (directory == null)
		{
			return;
		}
		foreach (var child in directory.GetDirectories())
		{
			Cleanup(pPath.PathJoin(child));
		}
		foreach (var fileName in directory.GetFiles())
		{
			DirAccess.RemoveAbsolute(pPath.PathJoin(fileName));
		}
		DirAccess.RemoveAbsolute(pPath);
	}
}
