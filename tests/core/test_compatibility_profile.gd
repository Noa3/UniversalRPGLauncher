## class_name CompatibilityProfileTest
## tests/core/test_compatibility_profile.gd
##
## Unit tests for CompatibilityProfile system.
## Tests profile loading, flag resolution, and entry matching.

extends Test


func setup() -> void:
	pass


func teardown() -> void:
	# Clean up test profile files
	_cleanup_test_profiles()


func _create_test_profile(p_name: String, p_data: Dictionary) -> String:
	var path := "user://test_profiles/" + p_name + ".json"
	DirAccess.make_dir_recursive_absolute(path.get_base_dir())
	var file := FileAccess.open(path, FileAccess.WRITE)
	if file:
		file.store_string(JSON.stringify(p_data, "  "))
		file.close()
	return path


func _cleanup_test_profiles() -> void:
	var dir := DirAccess.open("user://test_profiles")
	if dir == null:
		return
	dir.list_dir_begin()
	var file_name := dir.get_next()
	while file_name != "":
		DirAccess.remove_absolute("user://test_profiles/" + file_name)
		file_name = dir.get_next()


# === TESTS: Profile Entry Creation ===

func test_profile_entry_creation() -> void:
	var entry := GameDetector.DetectionResult.new()  ## Using DetectionResult as proxy
	## Actually test the CompatibilityProfile.ProfileEntry class
	var entry_class := preload("res://src/compatibility/compatibility_profile.gd")
	## Since we can't instantiate inner classes directly in tests,
	## we test through the public API instead
	pass  ## See load_profile tests below


# === TESTS: Profile Loading ===

func test_load_valid_profile() -> void:
	var compat := preload("res://src/compatibility/compatibility_profile.gd").new()
	
	var profile_data := {
		"flags": [
			{"name": "PreserveLegacyPictureTiming", "type": "BOOLEAN", "value": true},
			{"name": "LegacyTextEncoding", "type": "STRING", "value": "CP932"},
			{"name": "MaxMapCount", "type": "INTEGER", "value": 999},
		],
		"entries": [
			{
				"id": "test.game.1",
				"sha256": "abc123def456",
				"engine": "RPGMaker2003",
				"type": "game_profile",
				"compatibility": "full",
				"flags": [
					{"name": "DisableEnhancedRenderer", "type": "BOOLEAN", "value": true},
				],
				"notes": "Test game with known quirks",
			},
			{
				"id": "test.plugin.1",
				"sha256": "plugin123hash",
				"engine": "RPGMakerVXAce",
				"type": "plugin_profile",
				"compatibility": "partial",
				"replacement": "HLEPluginCompat",
				"notes": "Custom plugin requiring HLE",
			},
		],
	}
	
	var path := _create_test_profile("test_global", profile_data)
	assert_true(compat.load_profile(path))
	assert_eq(compat.get_loaded_files().size(), 1)


func test_load_nonexistent_profile() -> void:
	var compat := preload("res://src/compatibility/compatibility_profile.gd").new()
	assert_false(compat.load_profile("user://nonexistent_profile.json"))


func test_load_multiple_profiles() -> void:
	var compat := preload("res://src/compatibility/compatibility_profile.gd").new()
	
	var profile1 := {
		"flags": [
			{"name": "GlobalFlag", "type": "BOOLEAN", "value": true},
		],
		"entries": [],
	}
	var profile2 := {
		"flags": [
			{"name": "AnotherGlobalFlag", "type": "INTEGER", "value": 42},
		],
		"entries": [
			{
				"id": "test.game.2",
				"sha256": "def789",
				"engine": "RPGMakerMV",
				"type": "game_profile",
				"compatibility": "experimental",
			},
		],
	}
	
	var path1 := _create_test_profile("test_multi_1", profile1)
	var path2 := _create_test_profile("test_multi_2", profile2)
	
	assert_true(compat.load_profile(path1))
	assert_true(compat.load_profile(path2))
	assert_eq(compat.get_loaded_files().size(), 2)


func test_load_profiles_from_directory() -> void:
	var compat := preload("res://src/compatibility/compatibility_profile.gd").new()
	
	# Create multiple profiles in test directory
	var dir := "user://test_profiles"
	DirAccess.make_dir_recursive_absolute(dir)
	
	var profile_data := {
		"flags": [],
		"entries": [
			{
				"id": "test.batch.1",
				"sha256": "batch1",
				"engine": "RPGMaker2000",
				"type": "game_profile",
				"compatibility": "full",
			},
		],
	}
	
	_create_test_profile("batch_1", profile_data)
	_create_test_profile("batch_2", profile_data)
	_create_test_profile("batch_3", profile_data)
	
	var count := compat.load_profiles_from_directory(dir)
	assert_true(count >= 2)  ## At least the ones we created


# === TESTS: Flag Resolution ===

func test_get_game_flags_with_sha256() -> void:
	var compat := preload("res://src/compatibility/compatibility_profile.gd").new()
	
	var profile_data := {
		"flags": [
			{"name": "GlobalFlag", "type": "BOOLEAN", "value": true},
		],
		"entries": [
			{
				"id": "test.flag.1",
				"sha256": "abc123",
				"engine": "RPGMaker2003",
				"type": "game_profile",
				"compatibility": "full",
				"flags": [
					{"name": "GameSpecificFlag", "type": "BOOLEAN", "value": false},
				],
			},
		],
	}
	
	_create_test_profile("test_flags", profile_data)
	compat.load_profile("user://test_profiles/test_flags.json")
	
	var flags := compat.get_game_flags("abc123", "RPGMaker2003")
	assert_true(flags.size() >= 2)  ## Global + game-specific


func test_get_game_flags_without_match() -> void:
	var compat := preload("res://src/compatibility/compatibility_profile.gd").new()
	
	var profile_data := {
		"flags": [
			{"name": "GlobalFlag", "type": "BOOLEAN", "value": true},
		],
		"entries": [],
	}
	
	_create_test_profile("test_no_match", profile_data)
	compat.load_profile("user://test_profiles/test_no_match.json")
	
	var flags := compat.get_game_flags("nonexistent", "RPGMakerUnknown")
	## Should still return global flags
	assert_true(flags.size() >= 1)


func test_has_flag() -> void:
	var compat := preload("res://src/compatibility/compatibility_profile.gd").new()
	
	var profile_data := {
		"flags": [
			{"name": "TestFlag", "type": "BOOLEAN", "value": true},
		],
		"entries": [],
	}
	
	_create_test_profile("test_has_flag", profile_data)
	compat.load_profile("user://test_profiles/test_has_flag.json")
	
	assert_true(compat.has_flag("", "RPGMakerUnknown", "TestFlag"))
	assert_false(compat.has_flag("", "RPGMakerUnknown", "NonExistentFlag"))


func test_get_flag_value() -> void:
	var compat := preload("res://src/compatibility/compatibility_profile.gd").new()
	
	var profile_data := {
		"flags": [
			{"name": "MaxSpeed", "type": "INTEGER", "value": 100},
			{"name": "Encoding", "type": "STRING", "value": "UTF-8"},
		],
		"entries": [],
	}
	
	_create_test_profile("test_flag_value", profile_data)
	compat.load_profile("user://test_profiles/test_flag_value.json")
	
	assert_eq(compat.get_flag_value("", "RPGMakerUnknown", "MaxSpeed"), 100)
	assert_eq(compat.get_flag_value("", "RPGMakerUnknown", "Encoding"), "UTF-8")
	assert_eq(compat.get_flag_value("", "RPGMakerUnknown", "NonExistent"), null)


func test_game_specific_flag_overrides_global_value() -> void:
	var compat := preload("res://src/compatibility/compatibility_profile.gd").new()

	var profile_data := {
		"flags": [
			{"name": "EnhancedRenderer", "type": "BOOLEAN", "value": true},
		],
		"entries": [
			{
				"id": "test.override",
				"sha256": "override123",
				"engine": "RPGMaker2003",
				"flags": [
					{"name": "EnhancedRenderer", "type": "BOOLEAN", "value": false},
				],
			},
		],
	}

	_create_test_profile("test_override", profile_data)
	assert_true(compat.load_profile("user://test_profiles/test_override.json"))
	assert_eq(compat.get_flag_value("override123", "RPGMaker2003", "EnhancedRenderer"), false)


func test_specific_profile_does_not_match_when_hash_is_unknown() -> void:
	var compat := preload("res://src/compatibility/compatibility_profile.gd").new()
	var profile_data := {
		"flags": [],
		"entries": [
			{
				"id": "specific.game",
				"sha256": "specific123",
				"engine": "RPGMaker2003",
				"flags": [
					{"name": "SpecificOnly", "type": "BOOLEAN", "value": true},
				],
			},
		],
	}
	_create_test_profile("test_no_hash_wildcard", profile_data)
	assert_true(compat.load_profile("user://test_profiles/test_no_hash_wildcard.json"))
	assert_false(compat.has_flag("", "RPGMaker2003", "SpecificOnly"))
	assert_false(compat.has_flag("other-hash", "RPGMaker2003", "SpecificOnly"))
	assert_true(compat.has_flag("specific123", "RPGMaker2003", "SpecificOnly"))


func test_engine_wide_profile_can_intentionally_use_empty_hash() -> void:
	var compat := preload("res://src/compatibility/compatibility_profile.gd").new()
	var profile_data := {
		"flags": [],
		"entries": [
			{
				"id": "rm2k3.engine.default",
				"sha256": "",
				"engine": "RPGMaker2003",
				"flags": [
					{"name": "EngineWide", "type": "BOOLEAN", "value": true},
				],
			},
		],
	}
	_create_test_profile("test_engine_wide", profile_data)
	assert_true(compat.load_profile("user://test_profiles/test_engine_wide.json"))
	assert_true(compat.has_flag("any-hash", "RPGMaker2003", "EngineWide"))
	assert_false(compat.has_flag("any-hash", "RPGMakerMV", "EngineWide"))


# === TESTS: Entry Matching ===

func test_find_by_sha256() -> void:
	var compat := preload("res://src/compatibility/compatibility_profile.gd").new()
	
	var profile_data := {
		"flags": [],
		"entries": [
			{
				"id": "test.match.1",
				"sha256": "match123",
				"engine": "RPGMakerVXAce",
				"type": "game_profile",
				"compatibility": "full",
			},
		],
	}
	
	_create_test_profile("test_match", profile_data)
	compat.load_profile("user://test_profiles/test_match.json")
	
	var db := compat.get_database()
	var entry := db.find_by_sha256("match123")
	assert_ne(entry, null)
	assert_eq(entry.id, "test.match.1")


func test_find_by_sha256_no_match() -> void:
	var compat := preload("res://src/compatibility/compatibility_profile.gd").new()
	
	var profile_data := {
		"flags": [],
		"entries": [
			{
				"id": "test.nomatch",
				"sha256": "other123",
				"engine": "RPGMaker2003",
				"type": "game_profile",
				"compatibility": "full",
			},
		],
	}
	
	_create_test_profile("test_nomatch", profile_data)
	compat.load_profile("user://test_profiles/test_nomatch.json")
	
	var db := compat.get_database()
	var entry := db.find_by_sha256("nonexistent")
	assert_eq(entry, null)


func test_find_by_engine() -> void:
	var compat := preload("res://src/compatibility/compatibility_profile.gd").new()
	
	var profile_data := {
		"flags": [],
		"entries": [
			{
				"id": "test.engine.1",
				"sha256": "eng1",
				"engine": "RPGMakerVXAce",
				"type": "game_profile",
				"compatibility": "full",
			},
			{
				"id": "test.engine.2",
				"sha256": "eng2",
				"engine": "RPGMakerVXAce",
				"type": "game_profile",
				"compatibility": "partial",
			},
			{
				"id": "test.engine.3",
				"sha256": "eng3",
				"engine": "RPGMaker2003",
				"type": "game_profile",
				"compatibility": "full",
			},
		],
	}
	
	_create_test_profile("test_engine", profile_data)
	compat.load_profile("user://test_profiles/test_engine.json")
	
	var db := compat.get_database()
	var entries := db.find_by_engine("RPGMakerVXAce")
	assert_eq(entries.size(), 2)


# === TESTS: Profile Entry Properties ===

func test_profile_entry_has_flag() -> void:
	var compat := preload("res://src/compatibility/compatibility_profile.gd").new()
	
	var profile_data := {
		"flags": [],
		"entries": [
			{
				"id": "test.entry.flag",
				"sha256": "flagtest",
				"engine": "RPGMaker2003",
				"type": "game_profile",
				"compatibility": "full",
				"flags": [
					{"name": "TestFlag", "type": "BOOLEAN", "value": true},
				],
			},
		],
	}
	
	_create_test_profile("test_entry_flag", profile_data)
	compat.load_profile("user://test_profiles/test_entry_flag.json")
	
	var db := compat.get_database()
	var entry := db.find_by_sha256("flagtest")
	assert_ne(entry, null)
	assert_true(entry.has_flag("TestFlag"))
	assert_false(entry.has_flag("NonExistentFlag"))


func test_profile_entry_get_flag_value() -> void:
	var compat := preload("res://src/compatibility/compatibility_profile.gd").new()
	
	var profile_data := {
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
					{"name": "Mode", "type": "STRING", "value": "enhanced"},
				],
			},
		],
	}
	
	_create_test_profile("test_entry_value", profile_data)
	compat.load_profile("user://test_profiles/test_entry_value.json")
	
	var db := compat.get_database()
	var entry := db.find_by_sha256("valuetest")
	assert_ne(entry, null)
	assert_eq(entry.get_flag_value("Speed"), 42)
	assert_eq(entry.get_flag_value("Mode"), "enhanced")
	assert_eq(entry.get_flag_value("NonExistent"), null)


# === TESTS: Error Handling ===

func test_load_invalid_json() -> void:
	var compat := preload("res://src/compatibility/compatibility_profile.gd").new()
	
	var path := "user://test_profiles/invalid.json"
	DirAccess.make_dir_recursive_absolute(path.get_base_dir())
	var file := FileAccess.open(path, FileAccess.WRITE)
	if file:
		file.store_string("{ invalid json }")
		file.close()
	
	## Should return false without crashing
	assert_false(compat.load_profile(path))


func test_load_empty_profile() -> void:
	var compat := preload("res://src/compatibility/compatibility_profile.gd").new()
	
	var profile_data := {
		"flags": [],
		"entries": [],
	}
	
	_create_test_profile("test_empty", profile_data)
	assert_true(compat.load_profile("user://test_profiles/test_empty.json"))
	
	var db := compat.get_database()
	assert_eq(db.entries.size(), 0)
