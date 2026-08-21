## class_name VirtualFileSystem
class_name VirtualFileSystem
## plugins/vfs/virtual_filesystem.gd
##
## A non-destructive virtual filesystem layer for UniversalRPG.
## Merges multiple mount points into a unified read-only or read-write view.
## Handles case-insensitive path resolution for Windows compatibility.

extends RefCounted


## Mount types
enum MountType {
	GAME,      ## Original game files (read-only)
	OVERRIDE,  ## User overrides (read-only)
	RTP,       ## Runtime package files (read-only)
	SAVE,      ## Save directory (read-write)
	CACHE,     ## Temporary cache (read-write)
}


## Mount entry
class Mount:
	var path: String
	var mount_type: MountType
	var writable: bool
	
	func _init(p_path: String, p_type: MountType, p_writable: bool = false) -> void:
		path = p_path
		mount_type = p_type
		writable = p_writable


## File entry in the virtual filesystem
class VFile:
	var _vfs: VirtualFileSystem
	var _path: String
	var _mode: FileAccess.ModeFlags
	var _file: FileAccess
	
	func _init(p_vfs: VirtualFileSystem, p_path: String, p_mode: FileAccess.ModeFlags) -> void:
		_vfs = p_vfs
		_path = p_path
		_mode = p_mode
		_file = FileAccess.open(p_path, p_mode)
	
	func is_open() -> bool:
		return _file != null and _file.is_open()
	
	func get_as_text() -> String:
		if not is_open():
			return ""
		_file.seek(0)
		return _file.get_as_text()
	
	func get_position() -> int:
		if not is_open():
			return 0
		return _file.get_position()
	
	func seek(pos: int) -> void:
		if is_open():
			_file.seek(pos)
	
	func eof_reached() -> bool:
		if not is_open():
			return true
		return _file.eof_reached()
	
	func get_byte() -> int:
		if is_open():
			return _file.get_byte()
		return 0
	
	func get_buffer(p_size: int) -> PackedByteArray:
		if is_open():
			return _file.get_buffer(p_size)
		return PackedByteArray()
	
	func get_line() -> String:
		if is_open():
			return _file.get_line()
		return ""
	
	func get_length() -> int:
		if is_open():
			return _file.get_length()
		return 0
	
	func close() -> void:
		if is_open():
			_file.close()


# Mount points
var _mounts: Array[Mount] = []

# Case-insensitive lookup cache
var _case_map: Dictionary = {}

# Path separator (platform-independent)
const PATH_SEPARATOR: String = "/"


func _init() -> void:
	pass


## Add a mount point to the filesystem
func add_mount(p_name: String, p_path: String, p_mount_type: MountType, p_writable: bool = false) -> Mount:
	var mount := Mount.new(p_path, p_mount_type, p_writable)
	_mounts.append(mount)
	_rebuild_case_map()
	return mount


## Remove a mount point
func remove_mount(p_mount_type: MountType) -> bool:
	for i in range(_mounts.size() - 1, -1, -1):
		if _mounts[i].mount_type == p_mount_type:
			_mounts.remove_at(i)
			_rebuild_case_map()
			return true
	return false


## Get the mount of a specific type
func get_mount(p_mount_type: MountType) -> Mount:
	for mount in _mounts:
		if mount.mount_type == p_mount_type:
			return mount
	return null


## Normalize a path to use forward slashes and remove redundant separators
func normalize_path(p_path: String) -> String:
	# Replace backslashes with forward slashes
	var normalized := p_path.replace("\\", "/")
	
	# Remove double slashes
	while "//" in normalized:
		normalized = normalized.replace("//", "/")
	
	# Remove trailing slash (unless it's the root)
	if normalized.length() > 1 and normalized.ends_with("/"):
		normalized = normalized.trim_suffix("/")
	
	return normalized


## Check if a path is safe (no traversal outside mount points)
func is_safe_path(p_path: String) -> bool:
	# Normalize the path
	var normalized := normalize_path(p_path)
	
	# Block path traversal
	if "../" in normalized or normalized.begins_with("../"):
		return false
	
	# Block absolute paths (must be relative to mount)
	if normalized.begins_with("/"):
		return false
	
	# Block embedded NUL bytes without putting a NUL character in the script
	# source; Godot's GDScript parser warns when decoding that escape literal.
	if _contains_null_byte(normalized.to_utf8_buffer()):
		return false
	
	return true


static func _contains_null_byte(p_bytes: PackedByteArray) -> bool:
	return p_bytes.has(0)


## Resolve a path case-insensitively across all mounts
## Returns the resolved path or empty string if not found
func resolve(p_path: String) -> String:
	if not is_safe_path(p_path):
		printerr("[VirtualFileSystem] Unsafe path rejected: ", p_path)
		return ""
	
	var normalized := normalize_path(p_path).to_lower()
	
	if normalized.is_empty():
		var game := get_mount(MountType.GAME)
		return game.path if game else ""
	
	# Check case map first (cached lookup)
	if normalized in _case_map:
		return _case_map[normalized]
	
	# Search through mounts in priority order
	for mount in _mounts_in_priority_order():
		var full_path := mount.path + "/" + normalized
		if DirAccess.dir_exists_absolute(mount.path):
			var candidates := _find_case_insensitive(mount.path, normalized)
			if candidates.size() > 0:
				# Cache the mapping
				_case_map[normalized] = candidates[0]
				return candidates[0]
	
	return ""


## Find files case-insensitively in a directory
func _find_case_insensitive(p_dir: String, p_normalized: String) -> Array[String]:
	var results: Array[String] = []
	
	if not DirAccess.dir_exists_absolute(p_dir):
		return results
	
	var dir := DirAccess.open(p_dir)
	if dir == null:
		return results
	
	dir.list_dir_begin()
	var file_name := dir.get_next()
	
	while file_name != "":
		var full_path := p_dir + "/" + file_name
		var lower_name := file_name.to_lower()
		
		# Check if the normalized path is a prefix of this file
		if lower_name == p_normalized or lower_name.begins_with(p_normalized + "/"):
			results.append(full_path)
		
		file_name = dir.get_next()
	
	return results


## Rebuild the case-insensitive lookup cache
func _rebuild_case_map() -> void:
	_case_map.clear()
	
	for mount in _mounts_in_priority_order():
		if not DirAccess.dir_exists_absolute(mount.path):
			continue
		
		_scan_directory(mount.path, mount.path, "")


## Return mounts ordered by resolution priority (highest first)
func _mounts_in_priority_order() -> Array[Mount]:
	var ordered := _mounts.duplicate()
	ordered.sort_custom(_mount_priority_less)
	return ordered


## Priority comparison for mount resolution order
static func _mount_priority_less(p_left: Mount, p_right: Mount) -> bool:
	return _mount_priority(p_left.mount_type) < _mount_priority(p_right.mount_type)


## Resolution priority of a mount type (lower wins first)
static func _mount_priority(p_type: MountType) -> int:
	match p_type:
		MountType.OVERRIDE: return 0
		MountType.RTP: return 1
		MountType.GAME: return 2
		MountType.SAVE: return 3
		_: return 4


## Recursively scan a directory and build case map
func _scan_directory(p_base: String, p_current: String, p_relative: String) -> void:
	var dir := DirAccess.open(p_current)
	if dir == null:
		return
	
	dir.list_dir_begin()
	var file_name := dir.get_next()
	
	while file_name != "":
		var full_path := p_current + "/" + file_name
		var relative := p_relative + file_name
		
		if DirAccess.dir_exists_absolute(full_path):
			# Recurse into subdirectory
			_scan_directory(p_base, full_path, relative + "/")
		elif FileAccess.file_exists(full_path):
			# Add to case map
			var lower := relative.to_lower()
			if lower not in _case_map:
				_case_map[lower] = full_path
		
		file_name = dir.get_next()


## Open a file through the virtual filesystem
func open(p_path: String, p_mode: FileAccess.ModeFlags) -> VFile:
	var resolved := resolve(p_path)
	if resolved == "":
		printerr("[VirtualFileSystem] Path not found: ", p_path)
		return null
	
	return VFile.new(self, resolved, p_mode)


## Check if a file exists
func file_exists(p_path: String) -> bool:
	var resolved := resolve(p_path)
	return resolved != "" and FileAccess.file_exists(resolved)


## Check if a directory exists
func dir_exists(p_path: String) -> bool:
	var resolved := resolve(p_path)
	return resolved != "" and DirAccess.dir_exists_absolute(resolved)


## List files in a directory
func list_dir(p_path: String) -> Array[String]:
	var resolved := resolve(p_path)
	if resolved == "":
		return []
	
	var dir := DirAccess.open(resolved)
	if dir == null:
		return []
	
	var results: Array[String] = []
	dir.list_dir_begin()
	var file_name := dir.get_next()
	
	while file_name != "":
		results.append(file_name)
		file_name = dir.get_next()
	
	return results


## Get all mount points
func get_mounts() -> Array[Mount]:
	return _mounts.duplicate()


## Get the base game directory
func get_game_directory() -> String:
	var game_mount := get_mount(MountType.GAME)
	return game_mount.path if game_mount else ""


## Get the save directory
func get_save_directory() -> String:
	var save_mount := get_mount(MountType.SAVE)
	return save_mount.path if save_mount else ""
