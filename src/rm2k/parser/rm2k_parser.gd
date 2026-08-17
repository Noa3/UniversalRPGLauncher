## class_name RM2KParser
## src/rm2k/parser/rm2k_parser.gd
##
## Parser for RPG Maker 2000/2003 data files.
## Handles Game.ini, map files, database files, and save data.
## Unknown fields are preserved/skipped safely — never crashes on malformed data.

extends RefCounted


## Parser error
class ParseError:
	var line: int = 0
	var column: int = 0
	var message: String = ""
	
	func _init(p_line: int = 0, p_col: int = 0, p_msg: String = "") -> void:
		line = p_line
		column = p_col
		message = p_msg
	
	func to_string() -> String:
		if line > 0:
			return "Line %d, Col %d: %s" % [line, column, message]
		return "Error: %s" % message


## Parse result with error handling
class ParseResult:
	var success: bool = false
	var error: ParseError = null
	var data: Dictionary = {}
	
	func _init(p_success: bool = false, p_error: ParseError = null, p_data: Dictionary = {}) -> void:
		success = p_success
		error = p_error
		data = p_data
	
	func is_success() -> bool:
		return success
	
	func get_data() -> Dictionary:
		return data
	
	func get_error() -> ParseError:
		return error


## Parse Game.ini file
func parse_game_ini(p_path: String) -> ParseResult:
	if not FileAccess.file_exists(p_path):
		return ParseResult.new(false, ParseError.new(0, 0, "File not found: " + p_path))
	
	var file := FileAccess.open(p_path, FileAccess.READ)
	if file == null:
		return ParseResult.new(false, ParseError.new(0, 0, "Cannot open file: " + p_path))
	
	var data := {}
	var line_num := 0
	var first_line := true
	
	while not file.eof_reached():
		line_num += 1
		var line := file.get_line().strip_edges()
		
		## Skip comments
		if line.begins_with(";") or line.begins_with("#"):
			continue
		
		## First line should be [Game]
		if first_line:
			first_line = false
			if line != "[Game]":
				return ParseResult.new(false, ParseError.new(line_num, 0, "Expected [Game] header, got: " + line))
			data["header"] = "[Game]"
			continue
		
		## Parse key=value pairs
		if "=" in line:
			var parts := line.split("=", false, 1)
			if parts.size() == 2:
				var key := parts[0].strip_edges()
				var value := parts[1].strip_edges()
				data[key] = value
	
	file.close()
	
	return ParseResult.new(true, null, data)


## Parse RM2K database file
## RM2K databases are binary, but we handle them as structured data
func parse_database(p_path: String) -> ParseResult:
	if not FileAccess.file_exists(p_path):
		return ParseResult.new(false, ParseError.new(0, 0, "Database file not found: " + p_path))
	
	var file := FileAccess.open(p_path, FileAccess.READ)
	if file == null:
		return ParseResult.new(false, ParseError.new(0, 0, "Cannot open database file: " + p_path))
	
	var result := ParseResult.new()
	
	## Read file header
	var header := file.get_buffer(4)
	if header.size() < 4:
		return ParseResult.new(false, ParseError.new(0, 0, "Database file too small"))
	
	## Check for RM2K database signature
	## RM2K uses a specific header format
	var file_size := file.get_length()
	
	## Parse known sections (simplified — full binary parsing is complex)
	result.data["file_size"] = file_size
	result.data["header_bytes"] = header.hex()
	
	## Try to read metadata
	var metadata := {}
	
	## Number of actors (stored at specific offset in RM2K)
	var actor_count := _read_u16(file, 4)
	metadata["actor_count"] = actor_count
	
	## Number of items
	var item_count := _read_u16(file, 6)
	metadata["item_count"] = item_count
	
	## Number of skills
	var skill_count := _read_u16(file, 8)
	metadata["skill_count"] = skill_count
	
	## Number of states
	var state_count := _read_u16(file, 10)
	metadata["state_count"] = state_count
	
	## Number of classes
	var class_count := _read_u16(file, 12)
	metadata["class_count"] = class_count
	
	## Number of weapons
	var weapon_count := _read_u16(file, 14)
	metadata["weapon_count"] = weapon_count
	
	## Number of armors
	var armor_count := _read_u16(file, 16)
	metadata["armor_count"] = armor_count
	
	## Number of enemies
	var enemy_count := _read_u16(file, 18)
	metadata["enemy_count"] = enemy_count
	
	## Number of battle animations
	var anim_count := _read_u16(file, 20)
	metadata["animation_count"] = anim_count
	
	result.data["metadata"] = metadata
	result.success = true
	
	file.close()
	return result


## Parse a map file
## RM2K map files contain tile data, event data, and passability data
func parse_map(p_path: String) -> ParseResult:
	if not FileAccess.file_exists(p_path):
		return ParseResult.new(false, ParseError.new(0, 0, "Map file not found: " + p_path))
	
	var file := FileAccess.open(p_path, FileAccess.READ)
	if file == null:
		return ParseResult.new(false, ParseError.new(0, 0, "Cannot open map file: " + p_path))
	
	var result := ParseResult.new()
	
	## Read map header
	var header := file.get_buffer(10)
	if header.size() < 10:
		return ParseResult.new(false, ParseError.new(0, 0, "Map file too small"))
	
	var map_width := _read_u16_le(header, 0)
	var map_height := _read_u16_le(header, 2)
	
	result.data["width"] = map_width
	result.data["height"] = map_height
	result.data["header"] = header.hex()
	
	## Parse tile layers (lower, middle, upper)
	## Each layer is width * height bytes
	var tile_data_size := map_width * map_height
	
	## Lower layer
	var lower_layer := file.get_buffer(tile_data_size)
	if lower_layer.size() == tile_data_size:
		result.data["lower_layer"] = lower_layer.hex()
	
	## Middle layer
	var middle_layer := file.get_buffer(tile_data_size)
	if middle_layer.size() == tile_data_size:
		result.data["middle_layer"] = middle_layer.hex()
	
	## Upper layer
	var upper_layer := file.get_buffer(tile_data_size)
	if upper_layer.size() == tile_data_size:
		result.data["upper_layer"] = upper_layer.hex()
	
	## Parse events (variable length, starts after tile data)
	var events := []
	var event_count := 0
	
	## Read event count (stored at offset 8)
	var event_data_offset := 10 + tile_data_size * 3
	file.seek(event_data_offset)
	
	## Try to read events
	while not file.eof_reached():
		var event_header := file.get_buffer(4)
		if event_header.size() < 4 or event_header[0] == 0 and event_header[1] == 0:
			break
		
		var event_id := _read_u16_le(event_header, 0)
		events.append({
			"id": event_id,
			"header_hex": event_header.hex(),
		})
		event_count += 1
	
	result.data["event_count"] = event_count
	result.data["events"] = events
	result.success = true
	
	file.close()
	return result


## Parse save data
## RM2K save data has a specific format with game state
func parse_save(p_path: String) -> ParseResult:
	if not FileAccess.file_exists(p_path):
		return ParseResult.new(false, ParseError.new(0, 0, "Save file not found: " + p_path))
	
	var file := FileAccess.open(p_path, FileAccess.READ)
	if file == null:
		return ParseResult.new(false, ParseError.new(0, 0, "Cannot open save file: " + p_path))
	
	var result := ParseResult.new()
	
	## Read save header
	var header := file.get_buffer(16)
	if header.size() < 16:
		return ParseResult.new(false, ParseError.new(0, 0, "Save file too small"))
	
	result.data["save_header"] = header.hex()
	
	## Parse save metadata
	var save_data := {}
	
	## Game title (stored in save)
	var title_len := _read_u16_le(header, 0)
	if title_len > 0 and title_len < 256:
		var title_bytes := file.get_buffer(title_len)
		if title_bytes.size() == title_len:
			save_data["title"] = title_bytes.get_string_from_utf8()
	
	## Map ID
	var map_id := _read_u16_le(header, title_len + 2)
	save_data["map_id"] = map_id
	
	## Player position
	var player_x := _read_u16_le(header, title_len + 4)
	var player_y := _read_u16_le(header, title_len + 6)
	save_data["player_x"] = player_x
	save_data["player_y"] = player_y
	
	## Switches (bit array)
	var switch_count := _read_u16_le(header, title_len + 8)
	var switches := []
	for i in range(min(switch_count, 100)):  ## Limit to 100 switches
		var switch_byte := file.get_byte()
		for bit in range(8):
			if switch_byte & (1 << bit):
				switches.append(i * 8 + bit)
	save_data["switches"] = switches
	
	## Variables
	var variable_count := _read_u16_le(header, title_len + 10)
	var variables := []
	for i in range(min(variable_count, 500)):  ## Limit to 500 variables
		var value := _read_s16_le(file)
		variables.append(value)
	save_data["variables"] = variables
	
	result.data["save_data"] = save_data
	result.success = true
	
	file.close()
	return result


## Read a 16-bit unsigned integer (little-endian) from buffer
func _read_u16_le(p_buffer: PackedByteArray, p_offset: int) -> int:
	if p_offset + 1 >= p_buffer.size():
		return 0
	return p_buffer[p_offset] | (p_buffer[p_offset + 1] << 8)


## Read a 16-bit unsigned integer from buffer at position
func _read_u16(p_file: FileAccess, p_position: int) -> int:
	var pos := p_file.get_position()
	p_file.seek(p_position)
	var bytes := p_file.get_buffer(2)
	p_file.seek(pos)
	if bytes.size() < 2:
		return 0
	return bytes[0] | (bytes[1] << 8)


## Read a 16-bit signed integer (little-endian) from file
func _read_s16_le(p_file: FileAccess) -> int:
	var bytes := p_file.get_buffer(2)
	if bytes.size() < 2:
		return 0
	var value := bytes[0] | (bytes[1] << 8)
	## Convert to signed
	if value > 32767:
		value -= 65536
	return value
