extends SceneTree

const LegacyTextDecoderScript = preload("res://src/core/legacy_text_decoder.gd")
const GameDetectorScript = preload("res://src/game_detector/game_detector.gd")
const GameLibraryScript = preload("res://app/library/game_library.gd")

var _failures := 0
var _root := "user://smoke_test"


func _initialize() -> void:
	_cleanup(_root)
	DirAccess.make_dir_recursive_absolute(_root)
	_test_cp932()
	_test_lcf_detection()
	_test_lcf_parser()
	_test_mz_detection()
	_test_library_scan()
	_test_translation()
	_cleanup(_root)
	if _failures == 0:
		print("UniversalRPG smoke tests passed")
	quit(_failures)


func _test_cp932() -> void:
	var bytes := PackedByteArray([0x83, 0x65, 0x83, 0x58, 0x83, 0x67])
	_check(LegacyTextDecoderScript.new().decode(bytes) == "テスト", "CP932 title decoding")


func _test_lcf_detection() -> void:
	var game_dir := _root.path_join("JapaneseLCF")
	DirAccess.make_dir_recursive_absolute(game_dir)
	_write_bytes(game_dir.path_join("RPG_RT.ldb"), PackedByteArray([0x0b]))
	_write_bytes(game_dir.path_join("RPG_RT.lmt"), PackedByteArray([0x0a]))
	_write_bytes(game_dir.path_join("Map0001.lmu"), PackedByteArray([0x09]))
	var ini := "[RPG_RT]\nGameTitle=".to_utf8_buffer()
	ini.append_array(PackedByteArray([0x83, 0x65, 0x83, 0x58, 0x83, 0x67]))
	ini.append_array("\n".to_utf8_buffer())
	_write_bytes(game_dir.path_join("RPG_RT.ini"), ini)
	var result = GameDetectorScript.new().analyze(ProjectSettings.globalize_path(game_dir))
	_check(result.engine == GameDetectorScript.EngineType.RPGMAKER_2000_2003, "LCF family detection")
	_check(result.title == "テスト", "LCF CP932 title")
	_check(result.confidence == GameDetectorScript.Confidence.HIGH, "LCF detection confidence")


func _test_lcf_parser() -> void:
	var parser_dir := "user://smoke_lcf_parser"
	DirAccess.make_dir_recursive_absolute(parser_dir)
	var db := _ber(11)
	db.append_array("LcfDataBase".to_ascii_buffer())
	db.append_array(_chunk(0x1a, _ber(259)))
	db.append_array(_chunk(0x0b, _ber(0)))
	db.append_array(PackedByteArray([0x00]))
	var db_path := parser_dir.path_join("RPG_RT.ldb")
	_write_bytes(db_path, db)
	var parser = preload("res://src/rm2k/parser/rm2k_parser.gd").new()
	var result = parser.parse_database(db_path)
	_check(result.is_success(), "LCF LDB parse success")
	if result.is_success():
		var data := result.get_data()
		_check(data["version"] == 259, "LCF LDB version")
		_check(data["section_counts"]["actors"] == 0, "LCF LDB actors count")
	_cleanup(parser_dir)


func _ber(p_value: int) -> PackedByteArray:
	var groups := PackedByteArray()
	var v := p_value
	while v >= 0x80:
		groups.append(v & 0x7f)
		v >>= 7
	groups.append(v)
	var bytes := PackedByteArray()
	for i in range(groups.size() - 1, -1, -1):
		var b: int = groups[i]
		if i > 0:
			b |= 0x80
		bytes.append(b)
	return bytes


func _chunk(p_id: int, p_payload: PackedByteArray) -> PackedByteArray:
	var bytes := _ber(p_id)
	bytes.append_array(_ber(p_payload.size()))
	bytes.append_array(p_payload)
	return bytes


func _test_mz_detection() -> void:
	var game_dir := _root.path_join("MZGame")
	DirAccess.make_dir_recursive_absolute(game_dir.path_join("js"))
	DirAccess.make_dir_recursive_absolute(game_dir.path_join("data"))
	_write_text(game_dir.path_join("index.html"), "<!doctype html>")
	_write_text(game_dir.path_join("js/rmmz_core.js"), "// runtime")
	_write_text(game_dir.path_join("data/System.json"), '{"gameTitle":"MZ Test"}')
	var result = GameDetectorScript.new().analyze(ProjectSettings.globalize_path(game_dir))
	_check(result.engine == GameDetectorScript.EngineType.RPGMAKER_MZ, "MZ detection")
	_check(result.title == "MZ Test", "MZ title")


func _test_library_scan() -> void:
	var library = GameLibraryScript.new()
	library.set_root_path(ProjectSettings.globalize_path(_root), false)
	var games := library.scan()
	_check(games.size() == 2, "Library scans recognized child directories")


func _test_translation() -> void:
	TranslationServer.set_locale("ja")
	_check(TranslationServer.translate("ACTION_RESCAN") == "再スキャン", "Japanese UI catalog")
	for locale in ["en", "de", "es", "fr", "ja", "ko", "zh_CN"]:
		TranslationServer.set_locale(locale)
		_check(TranslationServer.translate("ACTION_RESCAN") != "ACTION_RESCAN", "%s UI catalog" % locale)
	TranslationServer.set_locale("en")


func _check(p_condition: bool, p_name: String) -> void:
	if p_condition:
		return
	_failures += 1
	push_error("Smoke test failed: " + p_name)


func _write_text(p_path: String, p_text: String) -> void:
	_write_bytes(p_path, p_text.to_utf8_buffer())


func _write_bytes(p_path: String, p_bytes: PackedByteArray) -> void:
	var file := FileAccess.open(p_path, FileAccess.WRITE)
	if file == null:
		_check(false, "Create fixture " + p_path)
		return
	file.store_buffer(p_bytes)


func _cleanup(p_path: String) -> void:
	if not DirAccess.dir_exists_absolute(p_path):
		return
	var directory := DirAccess.open(p_path)
	if directory == null:
		return
	for child in directory.get_directories():
		_cleanup(p_path.path_join(child))
	for file_name in directory.get_files():
		DirAccess.remove_absolute(p_path.path_join(file_name))
	DirAccess.remove_absolute(p_path)
