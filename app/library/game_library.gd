class_name GameLibrary
extends RefCounted

const GameDetectorScript = preload("res://src/game_detector/game_detector.gd")
const SETTINGS_PATH := "user://library.cfg"
const MAX_SCAN_DEPTH := 2


class GameEntry:
	var id: String
	var title: String
	var path: String
	var detection

	func _init(p_path: String, p_detection) -> void:
		path = p_path
		detection = p_detection
		id = p_path.sha256_text()
		title = p_detection.title if not p_detection.title.is_empty() else p_path.get_file()


var root_path: String = ""
var games: Array = []
var _detector = GameDetectorScript.new()


func load_settings() -> void:
	var default_path := ProjectSettings.globalize_path("user://games")
	var config := ConfigFile.new()
	if config.load(SETTINGS_PATH) == OK:
		root_path = str(config.get_value("library", "games_directory", default_path))
	else:
		root_path = default_path
	set_root_path(root_path, false)


func set_root_path(p_path: String, persist: bool = true) -> Error:
	var normalized := p_path.strip_edges().replace("\\", "/").simplify_path()
	if normalized.is_empty():
		return ERR_INVALID_PARAMETER
	if normalized.begins_with("user://") or normalized.begins_with("res://"):
		normalized = ProjectSettings.globalize_path(normalized)
	var error := DirAccess.make_dir_recursive_absolute(normalized)
	if error != OK and error != ERR_ALREADY_EXISTS:
		return error
	root_path = normalized
	if persist:
		_save_settings()
	return OK


func scan() -> Array:
	games.clear()
	if not DirAccess.dir_exists_absolute(root_path):
		return games
	_scan_directory(root_path, 0)
	games.sort_custom(_sort_entries)
	return games


func _scan_directory(p_path: String, p_depth: int) -> void:
	var detection = _detector.analyze(p_path)
	if detection.engine != GameDetectorScript.EngineType.UNKNOWN:
		games.append(GameEntry.new(p_path, detection))
		return
	if p_depth >= MAX_SCAN_DEPTH:
		return
	var directory := DirAccess.open(p_path)
	if directory == null:
		return
	for directory_name in directory.get_directories():
		if directory_name.begins_with("."):
			continue
		if directory.is_link(directory_name):
			continue
		_scan_directory(p_path.path_join(directory_name), p_depth + 1)


func _save_settings() -> void:
	var config := ConfigFile.new()
	config.load(SETTINGS_PATH)
	config.set_value("library", "games_directory", root_path)
	config.save(SETTINGS_PATH)


static func _sort_entries(p_left, p_right) -> bool:
	return p_left.title.naturalnocasecmp_to(p_right.title) < 0
