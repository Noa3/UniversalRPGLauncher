using System.Collections.Generic;
using Godot;
using UniversalRPG.Compatibility;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

partial class TestCompatibilityProfile : TestBase
{
	private const string ProfileDir = "user://test_profiles";

	public override void Teardown()
	{
		CleanupTestProfiles();
	}

	private static string CreateTestProfile(string pName, string pJson)
	{
		var path = ProfileDir.PathJoin(pName + ".json");
		DirAccess.MakeDirRecursiveAbsolute(path.GetBaseDir());
		using var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
		file?.StoreString(pJson);
		return path;
	}

	private static void CleanupTestProfiles()
	{
		if (!DirAccess.DirExistsAbsolute(ProfileDir))
		{
			return;
		}
		using var directory = DirAccess.Open(ProfileDir);
		if (directory == null)
		{
			return;
		}
		foreach (var fileName in directory.GetFiles())
		{
			DirAccess.RemoveAbsolute(ProfileDir.PathJoin(fileName));
		}
	}

	private const string ValidProfile = """
		{
		  "schema_version": 1,
		  "flags": [
		    {"name": "PreserveLegacyPictureTiming", "type": "BOOLEAN", "value": true},
		    {"name": "LegacyTextEncoding", "type": "STRING", "value": "CP932"},
		    {"name": "MaxMapCount", "type": "INTEGER", "value": 999}
		  ],
		  "entries": [
		    {
		      "id": "test.game.1",
		      "sha256": "abc123def456",
		      "engine": "RPGMaker2003",
		      "type": "game_profile",
		      "compatibility": "full",
		      "flags": [
		        {"name": "DisableEnhancedRenderer", "type": "BOOLEAN", "value": true}
		      ],
		      "notes": "Test game with known quirks"
		    },
		    {
		      "id": "test.plugin.1",
		      "sha256": "plugin123hash",
		      "engine": "RPGMakerVXAce",
		      "type": "plugin_profile",
		      "compatibility": "partial",
		      "replacement": "HLEPluginCompat",
		      "notes": "Custom plugin requiring HLE"
		    }
		  ]
		}
		""";

	// === TESTS: Profile Loading ===

	public void Test_LoadValidProfile()
	{
		var compat = new CompatibilityProfile();
		var path = CreateTestProfile("test_global", ValidProfile);
		AssertTrue(compat.LoadProfile(path));
		AssertEq(compat.GetLoadedFiles().Count, 1);
		AssertEq(compat.LastLoadedSchemaVersion, CompatibilityProfile.CurrentSchemaVersion);
	}

	public void Test_LegacyProfileWithoutSchemaVersionRemainsCompatible()
	{
		var compat = new CompatibilityProfile();
		var path = CreateTestProfile("legacy_schema", "{\"flags\":[],\"entries\":[]}");

		AssertTrue(compat.LoadProfile(path));
		AssertEq(compat.LastLoadedSchemaVersion, CompatibilityProfile.LegacySchemaVersion);
		AssertEq(compat.LastError, "");
	}

	public void Test_FutureOrInvalidSchemaIsRejectedWithDiagnostic()
	{
		var compat = new CompatibilityProfile();
		var future = CreateTestProfile("future_schema", "{\"schema_version\":99,\"flags\":[],\"entries\":[]}");
		AssertFalse(compat.LoadProfile(future));
		AssertTrue(compat.LastError.StartsWith("schema-unsupported:", System.StringComparison.Ordinal));

		var invalid = CreateTestProfile("invalid_schema", "{\"schema_version\":\"one\",\"flags\":[],\"entries\":[]}");
		AssertFalse(compat.LoadProfile(invalid));
		AssertTrue(compat.LastError.StartsWith("schema-invalid:", System.StringComparison.Ordinal));
	}

	public void Test_LoadNonexistentProfile()
	{
		var compat = new CompatibilityProfile();
		AssertFalse(compat.LoadProfile("user://nonexistent_profile.json"));
	}

	public void Test_LoadMultipleProfiles()
	{
		var compat = new CompatibilityProfile();

		var profile1 = """
			{
			  "flags": [{"name": "GlobalFlag", "type": "BOOLEAN", "value": true}],
			  "entries": []
			}
			""";
		var profile2 = """
			{
			  "flags": [{"name": "AnotherGlobalFlag", "type": "INTEGER", "value": 42}],
			  "entries": [
			    {
			      "id": "test.game.2",
			      "sha256": "def789",
			      "engine": "RPGMakerMV",
			      "type": "game_profile",
			      "compatibility": "experimental"
			    }
			  ]
			}
			""";

		var path1 = CreateTestProfile("test_multi_1", profile1);
		var path2 = CreateTestProfile("test_multi_2", profile2);

		AssertTrue(compat.LoadProfile(path1));
		AssertTrue(compat.LoadProfile(path2));
		AssertEq(compat.GetLoadedFiles().Count, 2);
	}

	public void Test_LoadProfilesFromDirectory()
	{
		var compat = new CompatibilityProfile();
		DirAccess.MakeDirRecursiveAbsolute(ProfileDir);

		var profileData = """
			{
			  "flags": [],
			  "entries": [
			    {
			      "id": "test.batch.1",
			      "sha256": "batch1",
			      "engine": "RPGMaker2000",
			      "type": "game_profile",
			      "compatibility": "full"
			    }
			  ]
			}
			""";

		CreateTestProfile("batch_1", profileData);
		CreateTestProfile("batch_2", profileData);
		CreateTestProfile("batch_3", profileData);

		var count = compat.LoadProfilesFromDirectory(ProjectSettings.GlobalizePath(ProfileDir));
		AssertTrue(count >= 2, "Expected at least 2 loaded profiles, got " + count);
	}

	// === TESTS: Flag Resolution ===

	public void Test_GetGameFlagsWithSha256()
	{
		var compat = new CompatibilityProfile();

		var profileData = """
			{
			  "flags": [{"name": "GlobalFlag", "type": "BOOLEAN", "value": true}],
			  "entries": [
			    {
			      "id": "test.flag.1",
			      "sha256": "abc123",
			      "engine": "RPGMaker2003",
			      "type": "game_profile",
			      "compatibility": "full",
			      "flags": [{"name": "GameSpecificFlag", "type": "BOOLEAN", "value": false}]
			    }
			  ]
			}
			""";

		CreateTestProfile("test_flags", profileData);
		compat.LoadProfile(ProfileDir.PathJoin("test_flags.json"));

		var flags = compat.GetGameFlags("abc123", "RPGMaker2003");
		AssertTrue(flags.Count >= 2, "Expected global + game-specific flags");
	}

	public void Test_GetGameFlagsWithoutMatch()
	{
		var compat = new CompatibilityProfile();

		var profileData = """
			{
			  "flags": [{"name": "GlobalFlag", "type": "BOOLEAN", "value": true}],
			  "entries": []
			}
			""";

		CreateTestProfile("test_no_match", profileData);
		compat.LoadProfile(ProfileDir.PathJoin("test_no_match.json"));

		var flags = compat.GetGameFlags("nonexistent", "RPGMakerUnknown");
		AssertTrue(flags.Count >= 1, "Should still return global flags");
	}

	public void Test_HasFlag()
	{
		var compat = new CompatibilityProfile();

		var profileData = """
			{
			  "flags": [{"name": "TestFlag", "type": "BOOLEAN", "value": true}],
			  "entries": []
			}
			""";

		CreateTestProfile("test_has_flag", profileData);
		compat.LoadProfile(ProfileDir.PathJoin("test_has_flag.json"));

		AssertTrue(compat.HasFlag("", "RPGMakerUnknown", "TestFlag"));
		AssertFalse(compat.HasFlag("", "RPGMakerUnknown", "NonExistentFlag"));
	}

	public void Test_GetFlagValue()
	{
		var compat = new CompatibilityProfile();

		var profileData = """
			{
			  "flags": [
			    {"name": "MaxSpeed", "type": "INTEGER", "value": 100},
			    {"name": "Encoding", "type": "STRING", "value": "UTF-8"}
			  ],
			  "entries": []
			}
			""";

		CreateTestProfile("test_flag_value", profileData);
		compat.LoadProfile(ProfileDir.PathJoin("test_flag_value.json"));

		AssertEq(compat.GetFlagValue("", "RPGMakerUnknown", "MaxSpeed").AsInt32(), 100);
		AssertEq(compat.GetFlagValue("", "RPGMakerUnknown", "Encoding").AsString(), "UTF-8");
		AssertEq(compat.GetFlagValue("", "RPGMakerUnknown", "NonExistent").VariantType, Variant.Type.Nil);
	}

	public void Test_GameSpecificFlagOverridesGlobalValue()
	{
		var compat = new CompatibilityProfile();

		var profileData = """
			{
			  "flags": [{"name": "EnhancedRenderer", "type": "BOOLEAN", "value": true}],
			  "entries": [
			    {
			      "id": "test.override",
			      "sha256": "override123",
			      "engine": "RPGMaker2003",
			      "flags": [{"name": "EnhancedRenderer", "type": "BOOLEAN", "value": false}]
			    }
			  ]
			}
			""";

		CreateTestProfile("test_override", profileData);
		AssertTrue(compat.LoadProfile(ProfileDir.PathJoin("test_override.json")));
		AssertFalse(compat.GetFlagValue("override123", "RPGMaker2003", "EnhancedRenderer").AsBool());
	}

	public void Test_SpecificProfileDoesNotMatchWhenHashIsUnknown()
	{
		var compat = new CompatibilityProfile();
		var profileData = """
			{
			  "flags": [],
			  "entries": [
			    {
			      "id": "specific.game",
			      "sha256": "specific123",
			      "engine": "RPGMaker2003",
			      "flags": [{"name": "SpecificOnly", "type": "BOOLEAN", "value": true}]
			    }
			  ]
			}
			""";
		CreateTestProfile("test_no_hash_wildcard", profileData);
		AssertTrue(compat.LoadProfile(ProfileDir.PathJoin("test_no_hash_wildcard.json")));
		AssertFalse(compat.HasFlag("", "RPGMaker2003", "SpecificOnly"));
		AssertFalse(compat.HasFlag("other-hash", "RPGMaker2003", "SpecificOnly"));
		AssertTrue(compat.HasFlag("specific123", "RPGMaker2003", "SpecificOnly"));
	}

	public void Test_EngineWideProfileCanIntentionallyUseEmptyHash()
	{
		var compat = new CompatibilityProfile();
		var profileData = """
			{
			  "flags": [],
			  "entries": [
			    {
			      "id": "rm2k3.engine.default",
			      "sha256": "",
			      "engine": "RPGMaker2003",
			      "flags": [{"name": "EngineWide", "type": "BOOLEAN", "value": true}]
			    }
			  ]
			}
			""";
		CreateTestProfile("test_engine_wide", profileData);
		AssertTrue(compat.LoadProfile(ProfileDir.PathJoin("test_engine_wide.json")));
		AssertTrue(compat.HasFlag("any-hash", "RPGMaker2003", "EngineWide"));
		AssertFalse(compat.HasFlag("any-hash", "RPGMakerMV", "EngineWide"));
	}

	// === TESTS: Entry Matching ===

	public void Test_FindBySha256()
	{
		var compat = new CompatibilityProfile();

		var profileData = """
			{
			  "flags": [],
			  "entries": [
			    {
			      "id": "test.match.1",
			      "sha256": "match123",
			      "engine": "RPGMakerVXAce",
			      "type": "game_profile",
			      "compatibility": "full"
			    }
			  ]
			}
			""";

		CreateTestProfile("test_match", profileData);
		compat.LoadProfile(ProfileDir.PathJoin("test_match.json"));

		var entry = compat.GetDatabase().FindBySha256("match123");
		AssertNe(entry, null);
		AssertEq(entry!.Id, "test.match.1");
	}

	public void Test_FindBySha256NoMatch()
	{
		var compat = new CompatibilityProfile();

		var profileData = """
			{
			  "flags": [],
			  "entries": [
			    {
			      "id": "test.nomatch",
			      "sha256": "other123",
			      "engine": "RPGMaker2003",
			      "type": "game_profile",
			      "compatibility": "full"
			    }
			  ]
			}
			""";

		CreateTestProfile("test_nomatch", profileData);
		compat.LoadProfile(ProfileDir.PathJoin("test_nomatch.json"));

		var entry = compat.GetDatabase().FindBySha256("nonexistent");
		AssertEq(entry, null);
	}

	public void Test_FindByEngine()
	{
		var compat = new CompatibilityProfile();

		var profileData = """
			{
			  "flags": [],
			  "entries": [
			    {"id": "test.engine.1", "sha256": "eng1", "engine": "RPGMakerVXAce", "type": "game_profile", "compatibility": "full"},
			    {"id": "test.engine.2", "sha256": "eng2", "engine": "RPGMakerVXAce", "type": "game_profile", "compatibility": "partial"},
			    {"id": "test.engine.3", "sha256": "eng3", "engine": "RPGMaker2003", "type": "game_profile", "compatibility": "full"}
			  ]
			}
			""";

		CreateTestProfile("test_engine", profileData);
		compat.LoadProfile(ProfileDir.PathJoin("test_engine.json"));

		var entries = compat.GetDatabase().FindByEngine("RPGMakerVXAce");
		AssertEq(entries.Count, 2);
	}

	// === TESTS: Profile Entry Properties ===

	public void Test_ProfileEntryHasFlag()
	{
		var compat = new CompatibilityProfile();

		var profileData = """
			{
			  "flags": [],
			  "entries": [
			    {
			      "id": "test.entry.flag",
			      "sha256": "flagtest",
			      "engine": "RPGMaker2003",
			      "type": "game_profile",
			      "compatibility": "full",
			      "flags": [{"name": "TestFlag", "type": "BOOLEAN", "value": true}]
			    }
			  ]
			}
			""";

		CreateTestProfile("test_entry_flag", profileData);
		compat.LoadProfile(ProfileDir.PathJoin("test_entry_flag.json"));

		var entry = compat.GetDatabase().FindBySha256("flagtest");
		AssertNe(entry, null);
		AssertTrue(entry!.HasFlag("TestFlag"));
		AssertFalse(entry.HasFlag("NonExistentFlag"));
	}

	public void Test_ProfileEntryGetFlagValue()
	{
		var compat = new CompatibilityProfile();

		var profileData = """
			{
			  "flags": [],
			  "entries": [
			    {
			      "id": "test.entry.value",
			      "sha256": "valuetest",
			      "engine": "RPGMakerMV",
			      "type": "plugin_profile",
			      "compatibility": "partial",
			      "flags": [
			        {"name": "Speed", "type": "INTEGER", "value": 42},
			        {"name": "Mode", "type": "STRING", "value": "enhanced"}
			      ]
			    }
			  ]
			}
			""";

		CreateTestProfile("test_entry_value", profileData);
		compat.LoadProfile(ProfileDir.PathJoin("test_entry_value.json"));

		var entry = compat.GetDatabase().FindBySha256("valuetest");
		AssertNe(entry, null);
		AssertEq(entry!.GetFlagValue("Speed").AsInt32(), 42);
		AssertEq(entry.GetFlagValue("Mode").AsString(), "enhanced");
		AssertEq(entry.GetFlagValue("NonExistent").VariantType, Variant.Type.Nil);
	}

	// === TESTS: Error Handling ===

	public void Test_LoadInvalidJson()
	{
		var compat = new CompatibilityProfile();

		var path = CreateTestProfile("invalid", "{ invalid json }");

		AssertFalse(compat.LoadProfile(path));
	}

	public void Test_LoadEmptyProfile()
	{
		var compat = new CompatibilityProfile();

		var path = CreateTestProfile("test_empty", "{\n  \"flags\": [],\n  \"entries\": []\n}");
		AssertTrue(compat.LoadProfile(path));

		var database = compat.GetDatabase();
		AssertEq(database.Entries.Count, 0);
	}
}
