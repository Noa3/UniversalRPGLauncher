## class_name GameDetectorTest
## tests/core/test_game_detector.gd
##
## Unit tests for GameDetector.
## Tests detection logic with synthetic game directories.

extends Test

var detector: GameDetector
var temp_base: String


func setup() -> void:
	detector = GameDetector.new()
	temp_base = "user://detector_test"
	
	if not DirAccess.dir_exists_absolute(temp_base):
		DirAccess.make_dir_recursive_absolute(temp_base)
	
	# Create synthetic game directories
	_create_rmgm2000_game()
	_create_rmgm2003_game()
	_create_rmgm_xp_game()
	_create_rmgm_vx_ace_game()
	_create_rmgm_mv_game()
	_create_unknown_game()


func teardown() -> void:
	_cleanup_dir(temp_base)


func _create_rmgm2000_game() -> void:
	var dir := temp_base + "/RM2000_Test"
	DirAccess.make_dir_recursive_absolute(dir + "/Data")
	DirAccess.make_dir_recursive_absolute(dir + "/Graphics")
	DirAccess.make_dir_recursive_absolute(dir + "/Maps")
	DirAccess.make_dir_recursive_absolute(dir + "/Images")
	
	_create_file(dir + "/Game.ini", "[Game]\nTitle=TestRM2000\nEngineID=RM2000\n")
	_create_file(dir + "/RPG_RT.ldb", "database")
	_create_file(dir + "/RPG_RT.lmt", "map_tree")
	_create_file(dir + "/Map0001.lmu", "map_data")
	_create_file(dir + "/Graphics/Characters/hero.png", "char_data")


func _create_rmgm2003_game() -> void:
	var dir := temp_base + "/RM2003_Test"
	DirAccess.make_dir_recursive_absolute(dir + "/Data")
	DirAccess.make_dir_recursive_absolute(dir + "/Graphics")
	DirAccess.make_dir_recursive_absolute(dir + "/Maps")
	DirAccess.make_dir_recursive_absolute(dir + "/Images")
	
	_create_file(dir + "/Game.ini", "[Game]\nTitle=TestRM2003\nEngineID=RM2003\n")
	_create_file(dir + "/RPG_RT.ldb", "database")
	_create_file(dir + "/RPG_RT.lmt", "map_tree")
	_create_file(dir + "/Map0001.lmu", "map_data")
	_create_file(dir + "/Data/Map001.rxdata", "map_data")
	_create_file(dir + "/RPG_RT.exe", "rpg_rt_binary")


func _create_rmgm_xp_game() -> void:
	var dir := temp_base + "/RMXP_Test"
	DirAccess.make_dir_recursive_absolute(dir + "/Data")
	DirAccess.make_dir_recursive_absolute(dir + "/Graphics")
	DirAccess.make_dir_recursive_absolute(dir + "/System")
	
	_create_file(dir + "/Game.ini", "[Game]\nTitle=TestXP\nLibrary=RGSS102A.dll\nRTP1=Standard\n")
	_create_file(dir + "/RGSS102A.dll", "rgss1_dll")
	_create_file(dir + "/Data/Map001.rvdata", "map_data")


func _create_rmgm_vx_ace_game() -> void:
	var dir := temp_base + "/RMVXAce_Test"
	DirAccess.make_dir_recursive_absolute(dir + "/Data")
	DirAccess.make_dir_recursive_absolute(dir + "/Graphics")
	DirAccess.make_dir_recursive_absolute(dir + "/Pictures")
	DirAccess.make_dir_recursive_absolute(dir + "/Animations")
	
	_create_file(dir + "/Game.ini", "[Game]\nTitle=TestVXAce\nLibrary=RGSS302A.dll\nRTP=RPGVXAce\n")
	_create_file(dir + "/RGSS302A.dll", "rgss3_dll")
	_create_file(dir + "/Data/Map001.rvdata2", "rvdata2_data")
	_create_file(dir + "/Data/Save001.rxdata", "save_data")


func _create_rmgm_mv_game() -> void:
	var dir := temp_base + "/RMVV_Test"
	DirAccess.make_dir_recursive_absolute(dir + "/data")
	DirAccess.make_dir_recursive_absolute(dir + "/img")
	DirAccess.make_dir_recursive_absolute(dir + "/js")
	DirAccess.make_dir_recursive_absolute(dir + "/js/plugins")
	
	_create_file(dir + "/index.html", "<!DOCTYPE html><html><body>RPG Maker MV</body></html>")
	_create_file(dir + "/package.json", '{"name":"rmmv","version":"1.6.0"}')
	_create_file(dir + "/data/Map001.json", '{"id":1,"name":"Test"}')
	_create_file(dir + "/js/rpg_core.js", "// MV runtime\n")
	_create_file(dir + "/js/plugins/TestPlugin.js", '// Test plugin\n')


func _create_unknown_game() -> void:
	var dir := temp_base + "/Unknown_Test"
	DirAccess.make_dir_recursive_absolute(dir)
	_create_file(dir + "/readme.txt", "This is not an RPG Maker game")
	_create_file(dir + "/data.bin", "random_data")


func _create_file(p_path: String, p_content: String) -> void:
	var dir := p_path.get_base_dir()
	if not DirAccess.dir_exists_absolute(dir):
		DirAccess.make_dir_recursive_absolute(dir)
	var file := FileAccess.open(p_path, FileAccess.WRITE)
	if file:
		file.store_string(p_content)
		file.close()


func _cleanup_dir(p_dir: String) -> void:
	if not DirAccess.dir_exists_absolute(p_dir):
		return
	var dir := DirAccess.open(p_dir)
	if dir == null:
		return
	dir.list_dir_begin()
	var file_name := dir.get_next()
	while file_name != "":
		var full_path := p_dir + "/" + file_name
		if DirAccess.dir_exists_absolute(full_path):
			_cleanup_dir(full_path)
			DirAccess.remove_absolute(full_path)
		elif FileAccess.file_exists(full_path):
			DirAccess.remove_absolute(full_path)
		file_name = dir.get_next()


# === TESTS: RM2000 Detection ===

func test_detect_rmgm2000() -> void:
	var result := detector.analyze(temp_base + "/RM2000_Test")
	assert_eq(result.engine, GameDetector.EngineType.RPGMAKER_2000)
	assert_true(result.confidence >= GameDetector.Confidence.MEDIUM)
	assert_true(result.evidence.size() > 0)


func test_detect_rmgm2000_evidence() -> void:
	var result := detector.analyze(temp_base + "/RM2000_Test")
	var has_lcf_database := false
	for e in result.evidence:
		if "RPG_RT.ldb" in e:
			has_lcf_database = true
	assert_true(has_lcf_database)


# === TESTS: RM2003 Detection ===

func test_detect_rmgm2003() -> void:
	var result := detector.analyze(temp_base + "/RM2003_Test")
	assert_eq(result.engine, GameDetector.EngineType.RPGMAKER_2003)
	assert_true(result.confidence >= GameDetector.Confidence.MEDIUM)


func test_detect_rmgm2003_evidence() -> void:
	var result := detector.analyze(temp_base + "/RM2003_Test")
	assert_true(result.has_native_libraries)


# === TESTS: RPG Maker XP Detection ===

func test_detect_rmgm_xp() -> void:
	var result := detector.analyze(temp_base + "/RMXP_Test")
	assert_eq(result.engine, GameDetector.EngineType.RPGMAKER_XP)
	assert_true(result.confidence >= GameDetector.Confidence.HIGH)


func test_detect_rmgm_xp_rgss() -> void:
	var result := detector.analyze(temp_base + "/RMXP_Test")
	assert_true(result.has_native_libraries)
	assert_true(result.rtp_dependency != "")


# === TESTS: RPG Maker VX Ace Detection ===

func test_detect_rmgm_vx_ace() -> void:
	var result := detector.analyze(temp_base + "/RMVXAce_Test")
	assert_eq(result.engine, GameDetector.EngineType.RPGMAKER_VX_ACE)
	assert_true(result.confidence >= GameDetector.Confidence.HIGH)


func test_detect_rmgm_vx_ace_archives() -> void:
	var result := detector.analyze(temp_base + "/RMVXAce_Test")
	assert_false(result.has_encrypted_archives)
	assert_true(result.has_native_libraries)


# === TESTS: RPG Maker MV Detection ===

func test_detect_rmgm_mv() -> void:
	var result := detector.analyze(temp_base + "/RMVV_Test")
	assert_eq(result.engine, GameDetector.EngineType.RPGMAKER_MV)
	assert_true(result.confidence >= GameDetector.Confidence.MEDIUM)


func test_detect_rmgm_mv_structure() -> void:
	var result := detector.analyze(temp_base + "/RMVV_Test")
	assert_true(result.has_custom_scripts)
	var has_runtime := false
	for e in result.evidence:
		if "javascript" in e.to_lower():
			has_runtime = true
	assert_true(has_runtime)


# === TESTS: Unknown Game ===

func test_detect_unknown() -> void:
	var result := detector.analyze(temp_base + "/Unknown_Test")
	assert_eq(result.engine, GameDetector.EngineType.UNKNOWN)
	assert_true(result.confidence <= GameDetector.Confidence.LOW)


# === TESTS: Non-Existent Directory ===

func test_detect_nonexistent() -> void:
	var result := detector.analyze(temp_base + "/NonExistent")
	assert_eq(result.engine, GameDetector.EngineType.UNKNOWN)
	assert_eq(result.confidence, GameDetector.Confidence.LOW)
	assert_eq(result.evidence.size(), 0)


# === TESTS: DetectionResult Helpers ===

func test_get_engine_name() -> void:
	var result := GameDetector.DetectionResult.new()
	result.engine = GameDetector.EngineType.RPGMAKER_VX_ACE
	assert_eq(result.get_engine_name(), "RPG Maker VX Ace")


func test_get_confidence_string() -> void:
	var result := GameDetector.DetectionResult.new()
	
	result.confidence = GameDetector.Confidence.HIGH
	assert_eq(result.get_confidence_string(), "High")
	
	result.confidence = GameDetector.Confidence.MEDIUM
	assert_eq(result.get_confidence_string(), "Medium")
	
	result.confidence = GameDetector.Confidence.LOW
	assert_eq(result.get_confidence_string(), "Low")


func test_describe() -> void:
	var result := GameDetector.DetectionResult.new()
	result.engine = GameDetector.EngineType.RPGMAKER_XP
	result.confidence = GameDetector.Confidence.HIGH
	result.evidence = ["Found Game.ini", "Found RGSS102A.dll"]
	
	var str := result.describe()
	assert_true("RPG Maker XP" in str)
	assert_true("High" in str)
	assert_true("Game.ini" in str)
	assert_true("RGSS102A.dll" in str)
