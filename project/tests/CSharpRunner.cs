using System;
using System.Collections.Generic;
using System.IO;
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
	private int _index;
	private int _orphanBefore;

	/// <summary>
	/// Which suite names an argument asked for, and null for all of them.
	/// </summary>
	/// <remarks>
	/// <strong>And a whole run passes no argument.</strong> The complete
	/// suite runs through <c>scripts/validate.sh</c> without one,
	/// <strong>and this is a debugging tool for one suite at a time.</strong>
	/// </remarks>
	private string _nurSuite = ReadSuiteFilter();

	private static string ReadSuiteFilter()
	{
		// **Und `GetCmdlineUserArgs`, und nicht `GetCmdlineArgs`** --
			// **denn Godot legt alles nach `--` in die Benutzerliste, und
			// die Motorliste ist alles davor.** **Ein Leser, der die
			// falsche fragt, sieht sein eigenes Argument nie und
			// laesst alles laufen.**
			foreach (var argument in OS.GetCmdlineUserArgs())
		{
			if (argument.StartsWith("--suite=", StringComparison.Ordinal))
			{
				return argument["--suite=".Length..];
			}
		}

		return null;
	}

	public override void _Ready()
	{
		TestBase.Tree = GetTree().Root;
		TestBase.Host = this;
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
			// Ein Argument wie `--suite TestMzPartyAndSwitches` laesst nur
			// diese Suite laufen. **Und das ist ein Werkzeug fuer die
			// Fehlersuche, und keine Ausnahme**, **denn die vollstaendige
			// Suite laeuft ueber `scripts/validate.sh` und dort ist kein
			// Argument gesetzt.**
			if (_nurSuite != null
				&& !suiteType.Name.Contains(_nurSuite, StringComparison.Ordinal))
			{
				continue;
			}

			var suite = Activator.CreateInstance(suiteType) as TestBase;
			if (suite == null)
			{
				_failures.Add($"{suiteType.Name}: could not create test suite instance");
				continue;
			}
			// **Und die Suite wird *vor* dem Lauf benannt** -- **denn wenn der
			// native Prozess mitten in einer Suite stirbt, ist die letzte
			// gedruckte Zeile der einzige Hinweis darauf, welche es war.**
			// **Ohne diese Zeile sieht ein Abbruch ohne Meldung wie ein Lauf aus,
			// der sich einfach weigert, fertig zu werden.**
			GD.Print($"RUNNING {suiteType.Name}");
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
			// **Und  die  Objektrechnung  des  Prozesses  wird  nach  jeder
			// Suite  mitprotokolliert** -- **denn  eine  Suite,  die  ihre
			// Knoten  nicht  freigibt,  sammelt  sie  ueber  300  Laeufe  an,
			//  und  das  sieht  dann  aus wie  ein  Absturz  ohne  Ursache.
			// **Und  nur  die  Suite,  die  die  Zahl  erhoeht,  ist  die
			//  schuldige** -- **ein  Summenwert  allein  sagt  nur,  dass
			//  etwas  leckt,  und  nicht  was.**
			// **Und  der  verwaltete  Heap  wird  zwischen  den Suiten
			// zurueckgegeben.**
			//
			// **Und  das  ist  Haushalten  und  nicht  die  erklaerte
			// Ursache:**  die  Messung  zeigte  einen  GC-Heap  von  8 bis 23
			// MiB  bei  einem  Working  Set  bis  947 MiB,  und  der
			// Speicher  liegt  damit  auf  Godots  Seite  und  nicht  in
			// verwaltetem  Code.  Wer  das  umdreht  und  die  GC  als  Fix  fuer
			//  den  Abbruch  verkauft,  hat  zwei  Messungen  verwechselt.
			//
			// **Und  richtig  ist  es  trotzdem:**  zwischen  zwei  Suiten  gibt
			// es  keinen  laufenden  Zustand,  den  eine  Erschuetterung
			// beschaedigen koennte,  und  die  Parsebaffer  der  echten
			// Spiele  sind  genau  die  Sorte  Objekt,  die  man  nicht  bis
			//  zum  Laufende  liegen  lassen  will.
			if (_index % 20 == 0)
			{
				GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true);
				GC.WaitForPendingFinalizers();
				GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true);
			}
			var orphanAfter = (int)Performance.GetMonitor(
				Performance.Monitor.ObjectOrphanNodeCount);
			if (orphanAfter != _orphanBefore)
			{
				// **Und  nur  eine  Veraenderung  der  Verwaisten  wird
				// gemeldet,  nicht  die  Knotenzahl** -- **denn
				// `ObjectCount` ist  Godots  Objektpool  und  schwankt  ueber
				// 3000  Eintraege  hin  und  her,  ohne  dass  irgendetwas
				//  undicht  wird;  ein  Lauf  mit  konstanten 53  Verwaisten
				//  hat  keinen  Knotenleck,  und  eine  Meldung  darueber
				//  waere  ein  Messfehler,  der  wie  ein  Befund  aussieht.**
				GD.Print($"ORPHANS after suite {suiteType.Name}: {orphanAfter} "
					+ $"(+{orphanAfter - _orphanBefore})");
			}
			_orphanBefore = orphanAfter;
			_index += 1;
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
		SmokeConfiguredExternalGame();
	}

	private void SmokeConfiguredExternalGame()
	{
		var path = System.Environment.GetEnvironmentVariable("URPG_EXTERNAL_GAME_PATH");
		if (string.IsNullOrWhiteSpace(path))
		{
			return;
		}
		var result = new GameDetector().Analyze(path);
		GD.Print($"External detection: engine={result.GetEngineName()}, title={result.Title}, version={result.EngineVersion?.ToString() ?? "unknown"}, confidence={result.Confidence}, candidates={result.Candidates.Count}, partial={result.Report.Inspection?.IsPartial}");
		foreach (var candidate in result.Candidates)
		{
			GD.Print($"External candidate: {candidate.DisplayName} score={candidate.Score} status={candidate.Status}");
		}
		foreach (var diagnostic in result.Diagnostics)
		{
			GD.Print($"External diagnostic: {diagnostic.Code}: {diagnostic.Message}");
		}
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
		var settingsPath = ProjectSettings.GlobalizePath(GameLibrary.SettingsPath);
		var settingsBackup = File.Exists(settingsPath) ? File.ReadAllBytes(settingsPath) : null;
		if (File.Exists(settingsPath))
		{
			File.Delete(settingsPath);
		}
		var packedScene = GD.Load<PackedScene>("res://scenes/main.tscn");
		Check(packedScene != null, "Load main scene");
		if (packedScene == null)
		{
			RestoreSettings(settingsPath, settingsBackup);
			return;
		}
		var instance = packedScene.Instantiate();
		Check(instance != null, "Instantiate main scene");
		if (instance == null)
		{
			RestoreSettings(settingsPath, settingsBackup);
			return;
		}
		AddChild(instance);
		instance.QueueFree();
		RestoreSettings(settingsPath, settingsBackup);
	}

	private static void RestoreSettings(string pPath, byte[]? pBackup)
	{
		if (pBackup == null)
		{
			if (File.Exists(pPath))
			{
				File.Delete(pPath);
			}
			return;
		}
		File.WriteAllBytes(pPath, pBackup);
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
		using var file = Godot.FileAccess.Open(pPath, Godot.FileAccess.ModeFlags.Write);
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
