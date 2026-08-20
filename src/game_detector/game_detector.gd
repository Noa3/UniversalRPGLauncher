class_name GameDetector
extends RefCounted

const LegacyTextDecoderScript = preload("res://src/core/legacy_text_decoder.gd")

enum EngineType {
	UNKNOWN,
	RPGMAKER_2000,
	RPGMAKER_2003,
	RPGMAKER_XP,
	RPGMAKER_VX,
	RPGMAKER_VX_ACE,
	RPGMAKER_MV,
	RPGMAKER_MZ,
	RPGMAKER_2000_2003,
}

enum Confidence {
	LOW,
	MEDIUM,
	HIGH,
}

const DETECTABLE_ENGINES := [
	EngineType.RPGMAKER_2000,
	EngineType.RPGMAKER_2003,
	EngineType.RPGMAKER_XP,
	EngineType.RPGMAKER_VX,
	EngineType.RPGMAKER_VX_ACE,
	EngineType.RPGMAKER_MV,
	EngineType.RPGMAKER_MZ,
]
const MAX_METADATA_BYTES := 1024 * 1024

var _text_decoder = LegacyTextDecoderScript.new()


class DetectionResult:
	var engine: int = EngineType.UNKNOWN
	var confidence: int = Confidence.LOW
	var evidence: Array[String] = []
	var title: String = ""
	var rtp_dependency: String = ""
	var has_custom_scripts: bool = false
	var has_native_libraries: bool = false
	var has_encrypted_archives: bool = false
	var unknown_runtimes: Array[String] = []
	var game_directory: String = ""

	func get_engine_name() -> String:
		match engine:
			EngineType.RPGMAKER_2000:
				return "RPG Maker 2000"
			EngineType.RPGMAKER_2003:
				return "RPG Maker 2003"
			EngineType.RPGMAKER_2000_2003:
				return "RPG Maker 2000/2003"
			EngineType.RPGMAKER_XP:
				return "RPG Maker XP"
			EngineType.RPGMAKER_VX:
				return "RPG Maker VX"
			EngineType.RPGMAKER_VX_ACE:
				return "RPG Maker VX Ace"
			EngineType.RPGMAKER_MV:
				return "RPG Maker MV"
			EngineType.RPGMAKER_MZ:
				return "RPG Maker MZ"
			_:
				return "Unknown"

	func get_confidence_string() -> String:
		match confidence:
			Confidence.HIGH:
				return TranslationServer.translate("CONFIDENCE_HIGH")
			Confidence.MEDIUM:
				return TranslationServer.translate("CONFIDENCE_MEDIUM")
			_:
				return TranslationServer.translate("CONFIDENCE_LOW")

	func describe() -> String:
		var text := "Detected engine: %s\nConfidence: %s\nEvidence:\n" % [
			get_engine_name(), get_confidence_string()
		]
		for item in evidence:
			text += "- %s\n" % item
		return text


func analyze(p_game_directory: String) -> DetectionResult:
	var result := DetectionResult.new()
	result.game_directory = p_game_directory
	if not DirAccess.dir_exists_absolute(p_game_directory):
		return result

	var scores := {}
	var evidence := {}
	for engine in DETECTABLE_ENGINES:
		scores[engine] = 0
		evidence[engine] = []

	_inspect_lcf(p_game_directory, scores, evidence)
	_inspect_rgss(p_game_directory, scores, evidence, result)
	_inspect_mv_mz(p_game_directory, scores, evidence)

	var best_score := 0
	var best_engines: Array[int] = []
	for engine in DETECTABLE_ENGINES:
		var score: int = scores[engine]
		if score > best_score:
			best_score = score
			best_engines = [engine]
		elif score == best_score and score > 0:
			best_engines.append(engine)

	if EngineType.RPGMAKER_2000 in best_engines and EngineType.RPGMAKER_2003 in best_engines:
		result.engine = EngineType.RPGMAKER_2000_2003
		result.evidence.assign(evidence[EngineType.RPGMAKER_2000])
		result.evidence.append(tr("DETECT_LCF_VERSION_AMBIGUOUS"))
	elif not best_engines.is_empty():
		result.engine = best_engines[0]
		result.evidence.assign(evidence[result.engine])

	if best_score >= 7:
		result.confidence = Confidence.HIGH
	elif best_score >= 4:
		result.confidence = Confidence.MEDIUM

	result.title = _read_game_title(p_game_directory)
	result.has_custom_scripts = _has_custom_scripts(p_game_directory)
	result.has_native_libraries = _has_native_libraries(p_game_directory)
	_collect_unknown_libraries(p_game_directory, result)
	return result


func _inspect_lcf(p_dir: String, p_scores: Dictionary, p_evidence: Dictionary) -> void:
	var has_database := _has_file(p_dir, "RPG_RT.ldb")
	var has_map_tree := _has_file(p_dir, "RPG_RT.lmt")
	if has_database and has_map_tree:
		_score(p_scores, p_evidence, EngineType.RPGMAKER_2000, 6, tr("DETECT_LCF_DATABASE"))
		_score(p_scores, p_evidence, EngineType.RPGMAKER_2003, 6, tr("DETECT_LCF_DATABASE"))

	var map_count := _count_extension(p_dir, ".lmu")
	if map_count > 0:
		var map_message := tr("DETECT_LCF_MAPS").format({"count": map_count})
		_score(p_scores, p_evidence, EngineType.RPGMAKER_2000, 2, map_message)
		_score(p_scores, p_evidence, EngineType.RPGMAKER_2003, 2, map_message)

	var ini_path := _find_file(p_dir, "RPG_RT.ini")
	if ini_path.is_empty():
		ini_path = _find_file(p_dir, "Game.ini")
	if ini_path.is_empty():
		return
	var content := _read_text(ini_path).to_lower()
	if "engineid=rm2000" in content:
		_score(p_scores, p_evidence, EngineType.RPGMAKER_2000, 7, tr("DETECT_INI_RM2000"))
	elif "engineid=rm2003" in content:
		_score(p_scores, p_evidence, EngineType.RPGMAKER_2003, 7, tr("DETECT_INI_RM2003"))
	elif "[rpg_rt]" in content:
		_score(p_scores, p_evidence, EngineType.RPGMAKER_2000, 1, tr("DETECT_RPG_RT_INI"))
		_score(p_scores, p_evidence, EngineType.RPGMAKER_2003, 1, tr("DETECT_RPG_RT_INI"))


func _inspect_rgss(
	p_dir: String,
	p_scores: Dictionary,
	p_evidence: Dictionary,
	p_result: DetectionResult
) -> void:
	var ini_path := _find_file(p_dir, "Game.ini")
	if not ini_path.is_empty():
		var content := _read_text(ini_path).to_lower()
		if "rgss1" in content:
			_score(p_scores, p_evidence, EngineType.RPGMAKER_XP, 6, tr("DETECT_RGSS1_INI"))
			p_result.rtp_dependency = _read_ini_value(ini_path, "RTP1")
		elif "rgss2" in content:
			_score(p_scores, p_evidence, EngineType.RPGMAKER_VX, 6, tr("DETECT_RGSS2_INI"))
			p_result.rtp_dependency = _read_ini_value(ini_path, "RTP")
		elif "rgss3" in content:
			_score(p_scores, p_evidence, EngineType.RPGMAKER_VX_ACE, 6, tr("DETECT_RGSS3_INI"))
			p_result.rtp_dependency = _read_ini_value(ini_path, "RTP")

	_score_for_file_prefix(p_dir, "rgss1", EngineType.RPGMAKER_XP, p_scores, p_evidence)
	_score_for_file_prefix(p_dir, "rgss2", EngineType.RPGMAKER_VX, p_scores, p_evidence)
	_score_for_file_prefix(p_dir, "rgss3", EngineType.RPGMAKER_VX_ACE, p_scores, p_evidence)

	for file_name in DirAccess.get_files_at(p_dir):
		var lower := file_name.to_lower()
		if lower.ends_with(".rgssad"):
			_score(p_scores, p_evidence, EngineType.RPGMAKER_XP, 5, tr("DETECT_RGSSAD"))
			p_result.has_encrypted_archives = true
		elif lower.ends_with(".rgss2a"):
			_score(p_scores, p_evidence, EngineType.RPGMAKER_VX, 5, tr("DETECT_RGSS2A"))
			p_result.has_encrypted_archives = true
		elif lower.ends_with(".rgss3a"):
			_score(p_scores, p_evidence, EngineType.RPGMAKER_VX_ACE, 5, tr("DETECT_RGSS3A"))
			p_result.has_encrypted_archives = true

	var data_dir := _find_directory(p_dir, "Data")
	if data_dir.is_empty():
		return
	var data_files := DirAccess.get_files_at(data_dir)
	if _array_has_extension(data_files, ".rxdata"):
		_score(p_scores, p_evidence, EngineType.RPGMAKER_XP, 3, tr("DETECT_XP_DATA"))
	if _array_has_extension(data_files, ".rvdata"):
		_score(p_scores, p_evidence, EngineType.RPGMAKER_VX, 3, tr("DETECT_VX_DATA"))
	if _array_has_extension(data_files, ".rvdata2"):
		_score(p_scores, p_evidence, EngineType.RPGMAKER_VX_ACE, 3, tr("DETECT_VXA_DATA"))


func _inspect_mv_mz(p_dir: String, p_scores: Dictionary, p_evidence: Dictionary) -> void:
	var web_root := p_dir
	var www_dir := _find_directory(p_dir, "www")
	if not www_dir.is_empty():
		web_root = www_dir

	var js_dir := _find_directory(web_root, "js")
	var data_dir := _find_directory(web_root, "data")
	var has_index := _has_file(web_root, "index.html")
	if js_dir.is_empty() or data_dir.is_empty() or not has_index:
		return

	if _has_file(js_dir, "rmmz_core.js") or _has_file(js_dir, "rmmz_managers.js"):
		_score(p_scores, p_evidence, EngineType.RPGMAKER_MZ, 9, tr("DETECT_MZ_RUNTIME"))
	elif _has_file(js_dir, "rpg_core.js") or _has_file(js_dir, "rpg_managers.js"):
		_score(p_scores, p_evidence, EngineType.RPGMAKER_MV, 9, tr("DETECT_MV_RUNTIME"))
	else:
		_score(p_scores, p_evidence, EngineType.RPGMAKER_MV, 3, tr("DETECT_MV_MZ_GENERIC"))


func _score_for_file_prefix(
	p_dir: String,
	p_prefix: String,
	p_engine: int,
	p_scores: Dictionary,
	p_evidence: Dictionary
) -> void:
	for file_name in DirAccess.get_files_at(p_dir):
		if file_name.to_lower().begins_with(p_prefix) and file_name.to_lower().ends_with(".dll"):
			_score(p_scores, p_evidence, p_engine, 5, tr("DETECT_FILE_FOUND").format({"file": file_name}))
			return


func _score(
	p_scores: Dictionary,
	p_evidence: Dictionary,
	p_engine: int,
	p_points: int,
	p_message: String
) -> void:
	p_scores[p_engine] += p_points
	if p_message not in p_evidence[p_engine]:
		p_evidence[p_engine].append(p_message)


func _read_game_title(p_dir: String) -> String:
	for ini_name in ["RPG_RT.ini", "Game.ini"]:
		var ini_path := _find_file(p_dir, ini_name)
		if ini_path.is_empty():
			continue
		for key in ["GameTitle", "Title"]:
			var title := _read_ini_value(ini_path, key)
			if not title.is_empty():
				return title

	var web_root := p_dir
	var www_dir := _find_directory(p_dir, "www")
	if not www_dir.is_empty():
		web_root = www_dir
	var data_dir := _find_directory(web_root, "data")
	if data_dir.is_empty():
		return ""
	var system_path := _find_file(data_dir, "System.json")
	if system_path.is_empty():
		return ""
	var parsed = JSON.parse_string(_read_text(system_path))
	if parsed is Dictionary and parsed.has("gameTitle"):
		return str(parsed.gameTitle)
	return ""


func _read_ini_value(p_path: String, p_key: String) -> String:
	for line in _read_text(p_path).split("\n"):
		var stripped := line.strip_edges()
		var separator := stripped.find("=")
		if separator < 0:
			continue
		if stripped.left(separator).strip_edges().nocasecmp_to(p_key) == 0:
			return stripped.substr(separator + 1).strip_edges()
	return ""


func _read_text(p_path: String) -> String:
	var file := FileAccess.open(p_path, FileAccess.READ)
	if file == null:
		return ""
	if file.get_length() > MAX_METADATA_BYTES:
		return ""
	return _text_decoder.decode(file.get_buffer(file.get_length()))


func _has_file(p_dir: String, p_name: String) -> bool:
	return not _find_file(p_dir, p_name).is_empty()


func _find_file(p_dir: String, p_name: String) -> String:
	var directory := DirAccess.open(p_dir)
	if directory == null:
		return ""
	for file_name in directory.get_files():
		if file_name.nocasecmp_to(p_name) == 0:
			if directory.is_link(file_name):
				return ""
			return p_dir.path_join(file_name)
	return ""


func _find_directory(p_dir: String, p_name: String) -> String:
	for directory_name in DirAccess.get_directories_at(p_dir):
		if directory_name.nocasecmp_to(p_name) == 0:
			return p_dir.path_join(directory_name)
	return ""


func _count_extension(p_dir: String, p_extension: String) -> int:
	var count := 0
	for file_name in DirAccess.get_files_at(p_dir):
		if file_name.to_lower().ends_with(p_extension):
			count += 1
	return count


func _array_has_extension(p_files: PackedStringArray, p_extension: String) -> bool:
	for file_name in p_files:
		if file_name.to_lower().ends_with(p_extension):
			return true
	return false


func _has_custom_scripts(p_dir: String) -> bool:
	for file_name in DirAccess.get_files_at(p_dir):
		var lower := file_name.to_lower()
		if lower.ends_with(".rb") or lower.ends_with(".js"):
			return true
	for directory_name in ["js", "Scripts"]:
		var directory := _find_directory(p_dir, directory_name)
		if directory.is_empty():
			continue
		for file_name in DirAccess.get_files_at(directory):
			var lower := file_name.to_lower()
			if lower.ends_with(".rb") or lower.ends_with(".js"):
				return true
	return false


func _has_native_libraries(p_dir: String) -> bool:
	for file_name in DirAccess.get_files_at(p_dir):
		var lower := file_name.to_lower()
		if lower.ends_with(".dll") or lower.ends_with(".so") or lower.ends_with(".dylib") or lower.ends_with(".exe"):
			return true
	return false


func _collect_unknown_libraries(p_dir: String, p_result: DetectionResult) -> void:
	for file_name in DirAccess.get_files_at(p_dir):
		var lower := file_name.to_lower()
		if not (lower.ends_with(".dll") or lower.ends_with(".so") or lower.ends_with(".dylib")):
			continue
		if not lower.begins_with("rgss") and not lower.begins_with("rpg_rt"):
			p_result.unknown_runtimes.append(file_name)
