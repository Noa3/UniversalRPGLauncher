## class_name GameDetector
## src/game_detector/game_detector.gd
##
## Detects RPG Maker game type by analyzing directory structure,
## file signatures, and metadata — without executing any files.

extends RefCounted


## Detected engine type
enum EngineType {
	UNKNOWN,
	RPGMAKER_2000,
	RPGMAKER_2003,
	RPGMAKER_XP,
	RPGMAKER_VX,
	RPGMAKER_VX_ACE,
	RPGMAKER_MV,
	RPGMAKER_MZ,
}


## Confidence level for detection
enum Confidence {
	LOW,    ## Few signals, ambiguous
	MEDIUM, ## Moderate signals, likely correct
	HIGH,   ## Multiple strong signals
}


## Detection result
class DetectionResult:
	var engine: EngineType
	var confidence: Confidence
	var evidence: Array[String]
	var rtp_dependency: String
	var has_custom_scripts: bool
	var has_native_libraries: bool
	var has_encrypted_archives: bool
	var unknown_runtimes: Array[String]
	var game_directory: String
	
	func _init() -> void:
		engine = EngineType.UNKNOWN
		confidence = Confidence.LOW
		evidence = []
		rtp_dependency = ""
		has_custom_scripts = false
		has_native_libraries = false
		has_encrypted_archives = false
		unknown_runtimes = []
		game_directory = ""
	
	func get_engine_name() -> String:
		match engine:
			EngineType.RPGMAKER_2000: return "RPG Maker 2000"
			EngineType.RPGMAKER_2003: return "RPG Maker 2003"
			EngineType.RPGMAKER_XP: return "RPG Maker XP"
			EngineType.RPGMAKER_VX: return "RPG Maker VX"
			EngineType.RPGMAKER_VX_ACE: return "RPG Maker VX Ace"
			EngineType.RPGMAKER_MV: return "RPG Maker MV"
			EngineType.RPGMAKER_MZ: return "RPG Maker MZ"
			_: return "Unknown"
	
	func get_confidence_string() -> String:
		match confidence:
			Confidence.HIGH: return "High"
			Confidence.MEDIUM: return "Medium"
			_: return "Low"
	
	func to_string() -> String:
		var result := "Detected engine: %s\nConfidence: %s\nEvidence:\n" % [
			get_engine_name(), get_confidence_string()
		]
		for e in evidence:
			result += "- %s\n" % e
		return result


## File signature constants
const RVDATA2_MAGIC: PackedByteArray = PackedByteArray([0x52, 0x56, 0x44, 0x41, 0x54, 0x41, 0x32])  ## "RVRDATA2"
const RPG_RT_INI_SIGNATURE: String = "[Game]"
const MOG_TRG1_SIGNATURE: String = "Moguri's TRG1"
const RPG_RT_SIGNATURE: String = "RPG_RT"
const MV_PACKAGE_MAGIC: PackedByteArray = PackedByteArray([0x55, 0x73, 0x72, 0x47, 0x50, 0x6B])  ## "UsrGPk"
const MZ_PACKAGE_MAGIC: PackedByteArray = PackedByteArray([0x4D, 0x5A, 0x50, 0x6B])  ## "MZPk"


## Analyze a game directory and return detection results
func analyze(p_game_directory: String) -> DetectionResult:
	var result := DetectionResult.new()
	result.game_directory = p_game_directory
	
	if not DirAccess.dir_exists_absolute(p_game_directory):
		return result
	
	# Check each engine type and collect evidence
	var scores := {
		EngineType.RPGMAKER_2000: 0,
		EngineType.RPGMAKER_2003: 0,
		EngineType.RPGMAKER_XP: 0,
		EngineType.RPGMAKER_VX: 0,
		EngineType.RPGMAKER_VX_ACE: 0,
		EngineType.RPGMAKER_MV: 0,
		EngineType.RPGMAKER_MZ: 0,
	}
	
	# Check Game.ini (strongest signal for 2000/2003/XP/VX/VXAce)
	var game_ini_evidence := _check_game_ini(p_game_directory)
	if game_ini_evidence.engine != EngineType.UNKNOWN:
		scores[game_ini_evidence.engine] += 3
		for e in game_ini_evidence.evidence:
			result.evidence.append(e)
		result.rtp_dependency = game_ini_evidence.rtp_dependency
		result.confidence = max(result.confidence, Confidence.MEDIUM)
	
	# Check for RGSS versions (XP/VX/VXAce)
	var rgss_evidence := _check_rgss(p_game_directory)
	if rgss_evidence.engine != EngineType.UNKNOWN:
		scores[rgss_evidence.engine] += 3
		for e in rgss_evidence.evidence:
			result.evidence.append(e)
		result.has_native_libraries = true
	
	# Check for encrypted archives (VX/VXAce use .rvdata2)
	var archive_evidence := _check_archives(p_game_directory)
	if archive_evidence.engine != EngineType.UNKNOWN:
		scores[archive_evidence.engine] += 2
		for e in archive_evidence.evidence:
			result.evidence.append(e)
		result.has_encrypted_archives = true
	
	# Check for MV/MZ specific files
	var mv_mz_evidence := _check_mv_mz(p_game_directory)
	if mv_mz_evidence.engine != EngineType.UNKNOWN:
		scores[mv_mz_evidence.engine] += 3
		for e in mv_mz_evidence.evidence:
			result.evidence.append(e)
		result.has_custom_scripts = true
	
	# Check directory structure
	var dir_evidence := _check_directory_structure(p_game_directory)
	if dir_evidence.engine != EngineType.UNKNOWN:
		scores[dir_evidence.engine] += 1
		for e in dir_evidence.evidence:
			result.evidence.append(e)
	
	# Check for custom scripts/plugins
	var custom_evidence := _check_custom_scripts(p_game_directory)
	if custom_evidence:
		result.has_custom_scripts = true
	
	# Check for native libraries
	var native_evidence := _check_native_libraries(p_game_directory)
	if native_evidence:
		result.has_native_libraries = true
	
	# Determine best match
	var best_engine := EngineType.UNKNOWN
	var best_score := 0
	for engine in scores:
		if scores[engine] > best_score:
			best_score = scores[engine]
			best_engine = engine
	
	result.engine = best_engine
	
	# Set confidence based on score
	if best_score >= 6:
		result.confidence = Confidence.HIGH
	elif best_score >= 3:
		result.confidence = Confidence.MEDIUM
	else:
		result.confidence = Confidence.LOW
	
	# Add unknown runtime warnings
	_check_unknown_runtimes(p_game_directory, result)
	
	return result


## Check Game.ini for engine identification
func _check_game_ini(p_dir: String) -> DetectionResult:
	var result := DetectionResult.new()
	var game_ini := p_dir + "/Game.ini"
	
	if not FileAccess.file_exists(game_ini):
		return result
	
	var file := FileAccess.open(game_ini, FileAccess.READ)
	if file == null:
		return result
	
	var first_line := file.get_line()
	result.evidence.append("Found Game.ini")
	
	if first_line == RPG_RT_INI_SIGNATURE:
		result.evidence.append("Game.ini has RPG_RT header")
		
		# Read the Title field
		var title_line := file.get_line()
		if title_line.begins_with("Title="):
			var title := title_line.substr(6)
			result.evidence.append("Game title: %s" % title)
		
		# Check for RPG_RT version info
		var content := file.get_as_text()
		file.close()
		
		if "RPG_RT.exe" in content:
			result.evidence.append("References RPG_RT.exe")
			result.rtp_dependency = "RPG Maker 2003 RTP"
		
		# Check for version markers
		if "EnginePath=" in content or "EngineID=" in content:
			result.engine = EngineType.RPGMAKER_XP
			result.rtp_dependency = "RPG Maker XP RTP"
		else:
			result.engine = EngineType.RPGMAKER_2003
			result.rtp_dependency = "RPG Maker 2003 RTP"
	
	return result


## Check for RGSS DLLs (XP/VX/VXAce)
func _check_rgss(p_dir: String) -> DetectionResult:
	var result := DetectionResult.new()
	
	var rgss_dlls := {
		"RGSS102A.dll": EngineType.RPGMAKER_XP,
		"RGSS104A.dll": EngineType.RPGMAKER_XP,
		"RGSS204A.dll": EngineType.RPGMAKER_VX,
		"RGSS302A.dll": EngineType.RPGMAKER_VX_ACE,
		"RGSS304A.dll": EngineType.RPGMAKER_VX_ACE,
	}
	
	for dll_name in rgss_dlls:
		var dll_path := p_dir + "/" + dll_name
		if FileAccess.file_exists(dll_path):
			result.engine = rgss_dlls[dll_name]
			result.evidence.append("Found %s" % dll_name)
			
			# Try to get file version info
			var version := _get_file_version(dll_path)
			if version != "":
				result.evidence.append("Version: %s" % version)
			
			# Check imported functions
			var imports := _get_dll_imports(dll_path)
			if "Ruby" in imports or "rb_" in imports:
				result.evidence.append("Contains Ruby import table")
			
			return result
	
	return result


## Check for encrypted archive files
func _check_archives(p_dir: String) -> DetectionResult:
	var result := DetectionResult.new()
	
	# Check for .rvdata2 files (VX/VXAce encrypted save data)
	var rvdata2_count := 0
	var dir := DirAccess.open(p_dir)
	if dir:
		dir.list_dir_begin()
		var file_name := dir.get_next()
		while file_name != "":
			if file_name.ends_with(".rvdata2"):
				rvdata2_count += 1
			file_name = dir.get_next()
	
	if rvdata2_count > 0:
		result.engine = EngineType.RPGMAKER_VX_ACE
		result.evidence.append("Found %d .rvdata2 files" % rvdata2_count)
		return result
	
	# Check for .rxdata files (VX/VXAce save data)
	var rxdata_count := 0
	if dir:
		dir.list_dir_begin()
		var file_name := dir.get_next()
		while file_name != "":
			if file_name.ends_with(".rxdata"):
				rxdata_count += 1
			file_name = dir.get_next()
	
	if rxdata_count > 0:
		result.engine = EngineType.RPGMAKER_VX_ACE
		result.evidence.append("Found %d .rxdata files" % rxdata_count)
		return result
	
	return result


## Check for MV/MZ specific files
func _check_mv_mz(p_dir: String) -> DetectionResult:
	var result := DetectionResult.new()
	
	# Check for MV/MZ package files
	var dir := DirAccess.open(p_dir)
	if dir == null:
		return result
	
	dir.list_dir_begin()
	var file_name := dir.get_next()
	
	while file_name != "":
		var file_path := p_dir + "/" + file_name
		if not FileAccess.file_exists(file_path):
			file_name = dir.get_next()
			continue
		
		if file_name == "index.html":
			result.engine = EngineType.RPGMAKER_MV
			result.evidence.append("Found index.html (MV/MZ web structure)")
		
		if file_name == "www" and DirAccess.dir_exists_absolute(file_path):
			result.engine = EngineType.RPGMAKER_MZ
			result.evidence.append("Found www/ directory (MZ structure)")
		
		if file_name == "package.json":
			var file := FileAccess.open(file_path, FileAccess.READ)
			if file:
				var content := file.get_as_text()
				if "rmmv" in content.to_lower() or "rmz" in content.to_lower():
					if "rmmv" in content.to_lower():
						result.engine = EngineType.RPGMAKER_MV
						result.evidence.append("package.json indicates MV")
					else:
						result.engine = EngineType.RPGMAKER_MZ
						result.evidence.append("package.json indicates MZ")
				file.close()
		
		file_name = dir.get_next()
	
	# Check for data/ directory (MV/MZ structure)
	var data_dir := p_dir + "/data"
	if DirAccess.dir_exists_absolute(data_dir):
		result.evidence.append("Found data/ directory")
		if result.engine == EngineType.UNKNOWN:
			result.engine = EngineType.RPGMAKER_MV  ## Default to MV if data/ exists
		elif result.engine == EngineType.RPGMAKER_MV:
			## Already MV, keep it
	
	return result


## Check directory structure patterns
func _check_directory_structure(p_dir: String) -> DetectionResult:
	var result := DetectionResult.new()
	
	var expected_dirs := {
		"Data": [EngineType.RPGMAKER_2000, EngineType.RPGMAKER_2003, EngineType.RPGMAKER_XP],
		"Graphics": [EngineType.RPGMAKER_2000, EngineType.RPGMAKER_2003, EngineType.RPGMAKER_XP,
		             EngineType.RPGMAKER_VX, EngineType.RPGMAKER_VX_ACE,
		             EngineType.RPGMAKER_MV, EngineType.RPGMAKER_MZ],
		"Maps": [EngineType.RPGMAKER_2000, EngineType.RPGMAKER_2003, EngineType.RPGMAKER_XP],
		"Images": [EngineType.RPGMAKER_2000, EngineType.RPGMAKER_2003],
		"System": [EngineType.RPGMAKER_XP],
		"Textures": [EngineType.RPGMAKER_MV, EngineType.RPGMAKER_MZ],
		"audio": [EngineType.RPGMAKER_MV, EngineType.RPGMAKER_MZ],
		"js": [EngineType.RPGMAKER_MV, EngineType.RPGMAKER_MZ],
		"plugins": [EngineType.RPGMAKER_MV, EngineType.RPGMAKER_MZ],
	}
	
	var dir := DirAccess.open(p_dir)
	if dir == null:
		return result
	
	dir.list_dir_begin()
	var found_dirs: Array[String] = []
	var file_name := dir.get_next()
	
	while file_name != "":
		if DirAccess.dir_exists_absolute(p_dir + "/" + file_name):
			found_dirs.append(file_name)
		file_name = dir.get_next()
	
	# Check each expected directory
	for dir_name in expected_dirs:
		if dir_name in found_dirs:
			for engine in expected_dirs[dir_name]:
				result.engine = engine
				result.evidence.append("Found expected directory: %s/" % dir_name)
				break  ## Only add one evidence per directory
	
	return result


## Check for custom scripts and plugins
func _check_custom_scripts(p_dir: String) -> bool:
	var dir := DirAccess.open(p_dir)
	if dir == null:
		return false
	
	dir.list_dir_begin()
	var file_name := dir.get_next()
	
	while file_name != "":
		# Check for custom script files
		if file_name.ends_with(".rb") or file_name.ends_with(".js"):
			return true
		
		# Check for plugin directories
		if DirAccess.dir_exists_absolute(p_dir + "/" + file_name):
			var sub_dir := DirAccess.open(p_dir + "/" + file_name)
			if sub_dir:
				sub_dir.list_dir_begin()
				var sub_file := sub_dir.get_next()
				while sub_file != "":
					if sub_file.ends_with(".js") or sub_file.ends_with(".rb"):
						return true
					sub_file = sub_dir.get_next()
		
		file_name = dir.get_next()
	
	return false


## Check for native libraries
func _check_native_libraries(p_dir: String) -> bool:
	var dir := DirAccess.open(p_dir)
	if dir == null:
		return false
	
	dir.list_dir_begin()
	var file_name := dir.get_next()
	
	while file_name != "":
		if file_name.ends_with(".dll") or file_name.ends_with(".so") or file_name.ends_with(".dylib"):
			if file_name != "Game.exe" and file_name != "GameMain.exe":
				return true
		file_name = dir.get_next()
	
	return false


## Check for unknown runtimes
func _check_unknown_runtimes(p_dir: String, p_result: DetectionResult) -> void:
	var dir := DirAccess.open(p_dir)
	if dir == null:
		return
	
	dir.list_dir_begin()
	var file_name := dir.get_next()
	
	while file_name != "":
		if file_name.ends_with(".dll"):
			# Check if it's a known RPG Maker DLL
			var known_dlls := ["RGSS", "RPG_RT", "EasyRPG", "mkxp", "nw"]
			var is_known := false
			for known in known_dlls:
				if known.to_lower() in file_name.to_lower():
					is_known = true
					break
			
			if not is_known:
				p_result.unknown_runtimes.append(file_name)
		
		file_name = dir.get_next()


## Get file version information
func _get_file_version(p_path: String) -> String:
	## TODO: Implement PE version info parsing
	## For now, return empty string
	return ""


## Get DLL import table (simplified)
func _get_dll_imports(p_path: String) -> String:
	## TODO: Implement PE import table parsing
	## For now, return empty string
	return ""
