## class_name CompatibilityProfile
## src/compatibility/compatibility_profile.gd
##
## Extensible compatibility profile system for game-specific quirks and fixes.
## Profiles are loaded from JSON and applied dynamically.

extends RefCounted


## Compatibility flag types
enum FlagType {
	STRING,
	BOOLEAN,
	INTEGER,
	FLOAT,
	ARRAY,
	DICTIONARY,
}


## Individual compatibility flag
class CompatFlag:
	var name: String
	var type: FlagType
	var value: Variant
	
	func _init(p_name: String, p_type: FlagType, p_value: Variant) -> void:
		name = p_name
		type = p_type
		value = p_value


## Compatibility profile entry
class ProfileEntry:
	var id: String
	var sha256: String
	var engine: String
	var game_title: String
	var type: String  ## "game_profile", "plugin_profile", "dll_profile", etc.
	var compatibility: String  ## "full", "partial", "experimental", "unknown"
	var flags: Array[CompatFlag] = []
	var notes: String = ""
	var replacement: String = ""
	
	func has_flag(p_name: String) -> bool:
		for flag in flags:
			if flag.name == p_name:
				return true
		return false
	
	func get_flag_value(p_name: String) -> Variant:
		for flag in flags:
			if flag.name == p_name:
				return flag.value
		return null


## Compatibility database
class CompatibilityDatabase:
	var entries: Array[ProfileEntry] = []
	var flags: Dictionary = {}  ## Global flags
	
	func add_entry(p_entry: ProfileEntry) -> void:
		entries.append(p_entry)
	
	func find_by_sha256(p_sha256: String) -> ProfileEntry:
		for entry in entries:
			if entry.sha256 == p_sha256:
				return entry
		return null
	
	func find_by_engine(p_engine: String) -> Array[ProfileEntry]:
		var result: Array[ProfileEntry] = []
		for entry in entries:
			if entry.engine == p_engine:
				result.append(entry)
		return result
	
	func find_all_matching(p_sha256: String, p_engine: String) -> Array[ProfileEntry]:
		var result: Array[ProfileEntry] = []
		for entry in entries:
			if (p_sha256 == "" or entry.sha256 == p_sha256) and \
			   (p_engine == "" or entry.engine == p_engine):
				result.append(entry)
		return result


## Main compatibility profile manager
var _database := CompatibilityDatabase.new()
var _loaded_files: Array[String] = []


## Load a compatibility profile from JSON file
func load_profile(p_path: String) -> bool:
	if not FileAccess.file_exists(p_path):
		printerr("[CompatibilityProfile] File not found: ", p_path)
		return false
	
	var file := FileAccess.open(p_path, FileAccess.READ)
	if file == null:
		printerr("[CompatibilityProfile] Cannot open file: ", p_path)
		return false
	
	var json_string := file.get_as_text()
	file.close()
	
	var json := JSON.new()
	var parse_result := json.parse(json_string)
	if parse_result != OK:
		printerr("[CompatibilityProfile] JSON parse error: ", json.get_error_message(), " in ", p_path)
		return false
	
	var data = json.data
	if not data is Dictionary:
		printerr("[CompatibilityProfile] Invalid profile format in ", p_path)
		return false
	
	_parse_profile_data(data)
	_loaded_files.append(p_path)
	return true


## Load profiles from a directory
func load_profiles_from_directory(p_directory: String) -> int:
	var count := 0
	var dir := DirAccess.open(p_directory)
	if dir == null:
		return 0
	
	dir.list_dir_begin()
	var file_name := dir.get_next()
	
	while file_name != "":
		if file_name.ends_with(".json"):
			if load_profile(p_directory + "/" + file_name):
				count += 1
		file_name = dir.get_next()
	
	return count


## Add a compatibility flag
func add_flag(p_name: String, p_type: FlagType, p_value: Variant) -> void:
	var flag := CompatFlag.new(p_name, p_type, p_value)
	_database.flags[p_name] = flag


## Add a profile entry
func add_entry(p_entry: ProfileEntry) -> void:
	_database.add_entry(p_entry)


## Get all flags for a game
func get_game_flags(p_sha256: String, p_engine: String) -> Array[CompatFlag]:
	var matching_entries := _database.find_all_matching(p_sha256, p_engine)
	var all_flags: Array[CompatFlag] = []
	
	## First add global flags
	for flag_name in _database.flags:
		all_flags.append(_database.flags[flag_name])
	
	## Then add game-specific flags (they override global)
	for entry in matching_entries:
		for flag in entry.flags:
			var already_exists := false
			for existing in all_flags:
				if existing.name == flag.name:
					already_exists = true
					break
			if not already_exists:
				all_flags.append(flag)
	
	return all_flags


## Check if a specific flag is set for a game
func has_flag(p_sha256: String, p_engine: String, p_flag_name: String) -> bool:
	var flags := get_game_flags(p_sha256, p_engine)
	for flag in flags:
		if flag.name == p_flag_name:
			return true
	return false


## Get a flag value for a game
func get_flag_value(p_sha256: String, p_engine: String, p_flag_name: String) -> Variant:
	var flags := get_game_flags(p_sha256, p_engine)
	for flag in flags:
		if flag.name == p_flag_name:
			return flag.value
	return null


## Parse a profile JSON structure
func _parse_profile_data(p_data: Dictionary) -> void:
	## Parse global flags
	if "flags" in p_data and p_data["flags"] is Array:
		for flag_data in p_data["flags"]:
			if flag_data is Dictionary and "name" in flag_data:
				var flag_type := _string_to_flag_type(flag_data.get("type", "STRING"))
				var value: Variant = flag_data.get("value", null)
				add_flag(flag_data["name"], flag_type, value)
	
	## Parse entries
	if "entries" in p_data and p_data["entries"] is Array:
		for entry_data in p_data["entries"]:
			if entry_data is Dictionary:
				var entry := ProfileEntry.new()
				entry.id = entry_data.get("id", "")
				entry.sha256 = entry_data.get("sha256", "")
				entry.engine = entry_data.get("engine", "")
				entry.game_title = entry_data.get("game_title", "")
				entry.type = entry_data.get("type", "game_profile")
				entry.compatibility = entry_data.get("compatibility", "unknown")
				entry.notes = entry_data.get("notes", "")
				entry.replacement = entry_data.get("replacement", "")
				
				## Parse flags
				if "flags" in entry_data and entry_data["flags"] is Array:
					for flag_data in entry_data["flags"]:
						if flag_data is Dictionary and "name" in flag_data:
							var flag_type := _string_to_flag_type(flag_data.get("type", "BOOLEAN"))
							var value: Variant = flag_data.get("value", false)
							var flag := CompatFlag.new(flag_data["name"], flag_type, value)
							entry.flags.append(flag)
				
				_database.add_entry(entry)


## Convert string to FlagType enum
func _string_to_flag_type(p_string: String) -> FlagType:
	match p_string.to_upper():
		"BOOLEAN": return FlagType.BOOLEAN
		"INTEGER": return FlagType.INTEGER
		"FLOAT": return FlagType.FLOAT
		"ARRAY": return FlagType.ARRAY
		"DICTIONARY": return FlagType.DICTIONARY
		_: return FlagType.STRING


## Get the compatibility database
func get_database() -> CompatibilityDatabase:
	return _database


## Get loaded profile files
func get_loaded_files() -> Array[String]:
	return _loaded_files.duplicate()
