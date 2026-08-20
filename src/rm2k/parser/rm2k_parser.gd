class_name RM2KParser
extends RefCounted

const LCFBinaryReaderScript = preload("res://src/rm2k/parser/lcf_binary_reader.gd")
const LegacyTextDecoderScript = preload("res://src/core/legacy_text_decoder.gd")

const MAX_FILE_BYTES := 64 * 1024 * 1024
const MAX_MAP_DIMENSION := 500
const MAX_MAP_TILES := 250_000

const LDB_HEADER := "LcfDataBase"
const LMU_HEADER := "LcfMapUnit"
const LSD_HEADER := "LcfSaveData"

const LDB_SECTION_NAMES := {
	0x0b: "actors",
	0x0c: "skills",
	0x0d: "items",
	0x0e: "enemies",
	0x0f: "troops",
	0x10: "terrains",
	0x11: "attributes",
	0x12: "states",
	0x13: "animations",
	0x14: "chipsets",
	0x15: "terms",
	0x16: "system",
	0x17: "switches",
	0x18: "variables",
	0x19: "common_events",
	0x1a: "version",
	0x1b: "common_event_duplicate_1",
	0x1c: "common_event_duplicate_2",
	0x1d: "battle_commands",
	0x1e: "classes",
	0x1f: "class_duplicate",
	0x20: "battler_animations",
	0x21: "string_variables",
}

const LDB_ARRAY_SECTIONS := [
	0x0b, 0x0c, 0x0d, 0x0e, 0x0f, 0x10, 0x11, 0x12, 0x13,
	0x14, 0x17, 0x18, 0x19, 0x1e, 0x1f, 0x20, 0x21,
]

var _text_decoder = LegacyTextDecoderScript.new()


class ParseError:
	var offset: int = -1
	var message: String = ""

	func _init(p_offset: int = -1, p_message: String = "") -> void:
		offset = p_offset
		message = p_message

	func describe() -> String:
		if offset >= 0:
			return "Offset 0x%X: %s" % [offset, message]
		return message


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


func parse_game_ini(p_path: String) -> ParseResult:
	var loaded := _read_file(p_path, 1024 * 1024)
	if not loaded.success:
		return loaded
	var text := _text_decoder.decode(loaded.data["bytes"])
	if text.is_empty() and not loaded.data["bytes"].is_empty():
		return _failure("Unable to decode INI text", 0)

	var values := {}
	var current_section := ""
	var found_section := false
	for raw_line in text.split("\n"):
		var line := raw_line.trim_suffix("\r").strip_edges()
		if line.is_empty() or line.begins_with(";") or line.begins_with("#"):
			continue
		if line.begins_with("[") and line.ends_with("]"):
			current_section = line.substr(1, line.length() - 2)
			if current_section.nocasecmp_to("RPG_RT") == 0 or current_section.nocasecmp_to("Game") == 0:
				found_section = true
			continue
		var separator := line.find("=")
		if separator < 0 or not found_section:
			continue
		var key := line.left(separator).strip_edges()
		var value := line.substr(separator + 1).strip_edges()
		values[key] = value

	if not found_section:
		return _failure("Expected [RPG_RT] or [Game] section", 0)
	values["section"] = current_section
	return ParseResult.new(true, null, values)


func parse_database(p_path: String) -> ParseResult:
	var opened := _open_lcf(p_path, LDB_HEADER)
	if not opened.success:
		return opened
	var top := _read_top_chunks(opened.data["reader"])
	if not top.success:
		return top

	var sections := {}
	var section_counts := {}
	var unknown_chunks: Array[Dictionary] = []
	var engine_family := "RPG Maker 2000"
	var version := 0

	for chunk in top.data["chunks"]:
		var id: int = chunk["id"]
		if not LDB_SECTION_NAMES.has(id):
			unknown_chunks.append(chunk)
			continue
		var section_name: String = LDB_SECTION_NAMES[id]
		var section := {
			"id": id,
			"offset": chunk["offset"],
			"length": chunk["length"],
		}
		if id in LDB_ARRAY_SECTIONS:
			var array_result := _parse_struct_array(chunk["data"], false)
			if not array_result.success:
				return _failure("Invalid %s section: %s" % [section_name, array_result.error.message], chunk["payload_offset"] + max(array_result.error.offset, 0))
			section["count"] = array_result.data["count"]
			section_counts[section_name] = array_result.data["count"]
		if id == 0x1a:
			var integer := _decode_lcf_integer(chunk["data"])
			if not integer.success:
				return _failure("Invalid database version: %s" % integer.error.message, chunk["payload_offset"])
			version = integer.data["value"]
			section["value"] = version
		if id in [0x1d, 0x1e, 0x1f, 0x20]:
			engine_family = "RPG Maker 2003"
		sections[section_name] = section

	return ParseResult.new(true, null, {
		"format": "LDB",
		"header": LDB_HEADER,
		"file_size": opened.data["file_size"],
		"chunk_count": top.data["chunks"].size(),
		"sections": sections,
		"section_counts": section_counts,
		"unknown_chunks": unknown_chunks,
		"version": version,
		"engine_family": engine_family,
	})


func parse_map(p_path: String) -> ParseResult:
	var opened := _open_lcf(p_path, LMU_HEADER)
	if not opened.success:
		return opened
	var top := _read_top_chunks(opened.data["reader"])
	if not top.success:
		return top

	var fields := _chunks_by_id(top.data["chunks"])
	var chipset_result := _integer_from_fields(fields, 0x01, 1)
	var width_result := _integer_from_fields(fields, 0x02, 20)
	var height_result := _integer_from_fields(fields, 0x03, 15)
	for result in [chipset_result, width_result, height_result]:
		if not result.success:
			return result
	var width: int = width_result.data["value"]
	var height: int = height_result.data["value"]
	if width <= 0 or width > MAX_MAP_DIMENSION or height <= 0 or height > MAX_MAP_DIMENSION:
		return _failure("Map dimensions %dx%d exceed limits" % [width, height], 0)
	var tile_count := width * height
	if tile_count > MAX_MAP_TILES:
		return _failure("Map contains too many tiles", 0)

	var lower_result := _decode_tile_layer(fields.get(0x47, {}), tile_count, "lower")
	if not lower_result.success:
		return lower_result
	var upper_result := _decode_tile_layer(fields.get(0x48, {}), tile_count, "upper")
	if not upper_result.success:
		return upper_result

	var events: Array[Dictionary] = []
	if fields.has(0x51):
		var event_array := _parse_struct_array(fields[0x51]["data"], true)
		if not event_array.success:
			return _failure("Invalid map events: %s" % event_array.error.message, fields[0x51]["payload_offset"] + max(event_array.error.offset, 0))
		for event_object in event_array.data["objects"]:
			var event_fields := _chunks_by_id(event_object["fields"])
			var x_result := _integer_from_fields(event_fields, 0x02, 0)
			var y_result := _integer_from_fields(event_fields, 0x03, 0)
			if not x_result.success or not y_result.success:
				return _failure("Invalid event coordinates", fields[0x51]["payload_offset"])
			var page_count := 0
			if event_fields.has(0x05):
				var pages := _parse_struct_array(event_fields[0x05]["data"], false)
				if not pages.success:
					return _failure("Invalid event pages: %s" % pages.error.message, fields[0x51]["payload_offset"])
				page_count = pages.data["count"]
			events.append({
				"id": event_object["id"],
				"name": _decode_text_field(event_fields, 0x01),
				"x": x_result.data["value"],
				"y": y_result.data["value"],
				"page_count": page_count,
			})

	var known_ids := [0x01, 0x02, 0x03, 0x0b, 0x1f, 0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x28, 0x29, 0x2a, 0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x3c, 0x3d, 0x3e, 0x47, 0x48, 0x51, 0x5a, 0x5b]
	var unknown_chunks: Array[Dictionary] = []
	for chunk in top.data["chunks"]:
		if chunk["id"] not in known_ids:
			unknown_chunks.append(chunk)

	return ParseResult.new(true, null, {
		"format": "LMU",
		"header": LMU_HEADER,
		"file_size": opened.data["file_size"],
		"chunk_count": top.data["chunks"].size(),
		"chipset_id": chipset_result.data["value"],
		"width": width,
		"height": height,
		"lower_layer": lower_result.data["tiles"],
		"upper_layer": upper_result.data["tiles"],
		"event_count": events.size(),
		"events": events,
		"unknown_chunks": unknown_chunks,
	})


func parse_save(p_path: String) -> ParseResult:
	var opened := _open_lcf(p_path, LSD_HEADER)
	if not opened.success:
		return opened
	var top := _read_top_chunks(opened.data["reader"])
	if not top.success:
		return top
	return ParseResult.new(true, null, {
		"format": "LSD",
		"header": LSD_HEADER,
		"file_size": opened.data["file_size"],
		"chunk_count": top.data["chunks"].size(),
		"chunks": top.data["chunks"],
	})


func _open_lcf(p_path: String, p_header: String) -> ParseResult:
	var loaded := _read_file(p_path, MAX_FILE_BYTES)
	if not loaded.success:
		return loaded
	var reader = LCFBinaryReaderScript.new(loaded.data["bytes"])
	reader.read_header(p_header)
	if reader.has_error():
		return _reader_failure(reader)
	return ParseResult.new(true, null, {
		"reader": reader,
		"file_size": loaded.data["bytes"].size(),
	})


func _read_file(p_path: String, p_limit: int) -> ParseResult:
	if not FileAccess.file_exists(p_path):
		return _failure("File not found: " + p_path)
	var file := FileAccess.open(p_path, FileAccess.READ)
	if file == null:
		return _failure("Cannot open file: " + p_path)
	var length := file.get_length()
	if length > p_limit:
		return _failure("File exceeds %d-byte limit" % p_limit)
	return ParseResult.new(true, null, {"bytes": file.get_buffer(length)})


func _read_top_chunks(p_reader) -> ParseResult:
	var chunks: Array[Dictionary] = []
	var terminated := false
	while not p_reader.is_eof():
		if chunks.size() >= LCFBinaryReaderScript.MAX_CHUNKS:
			return _failure("LCF chunk count exceeds limit", p_reader.get_position())
		var chunk: Dictionary = p_reader.read_chunk()
		if p_reader.has_error():
			return _reader_failure(p_reader)
		if chunk["terminator"]:
			terminated = true
			if not p_reader.is_eof():
				return _failure("Trailing data after LCF terminator", p_reader.get_position())
			break
		chunks.append(chunk)
	return ParseResult.new(true, null, {"chunks": chunks, "terminated": terminated})


func _parse_struct_array(p_data: PackedByteArray, p_collect_fields: bool) -> ParseResult:
	var reader = LCFBinaryReaderScript.new(p_data)
	var count := reader.read_ber()
	if reader.has_error():
		return _reader_failure(reader)
	if count > LCFBinaryReaderScript.MAX_ARRAY_ITEMS:
		return _failure("Array count %d exceeds limit" % count, 0)
	var objects: Array[Dictionary] = []
	for index in range(count):
		var object_id := reader.read_ber()
		if reader.has_error():
			return _reader_failure(reader)
		var fields_result := _read_struct_fields(reader, p_collect_fields)
		if not fields_result.success:
			return fields_result
		if p_collect_fields:
			objects.append({"id": object_id, "fields": fields_result.data["fields"]})
	if not reader.is_eof():
		return _failure("Trailing bytes after structure array", reader.get_position())
	return ParseResult.new(true, null, {"count": count, "objects": objects})


func _read_struct_fields(p_reader, p_collect: bool) -> ParseResult:
	var fields: Array[Dictionary] = []
	var field_count := 0
	while not p_reader.is_eof():
		if field_count >= LCFBinaryReaderScript.MAX_STRUCT_FIELDS:
			return _failure("Structure field count exceeds limit", p_reader.get_position())
		var field: Dictionary = p_reader.read_chunk()
		if p_reader.has_error():
			return _reader_failure(p_reader)
		if field["terminator"]:
			return ParseResult.new(true, null, {"fields": fields})
		if p_collect:
			fields.append(field)
		field_count += 1
	return _failure("Structure is missing terminator", p_reader.get_position())


func _chunks_by_id(p_chunks: Array) -> Dictionary:
	var result := {}
	for chunk in p_chunks:
		result[chunk["id"]] = chunk
	return result


func _integer_from_fields(p_fields: Dictionary, p_id: int, p_default: int) -> ParseResult:
	if not p_fields.has(p_id):
		return ParseResult.new(true, null, {"value": p_default})
	var result := _decode_lcf_integer(p_fields[p_id]["data"])
	if not result.success:
		return _failure("Invalid integer field 0x%X: %s" % [p_id, result.error.message], p_fields[p_id]["payload_offset"])
	return result


func _decode_lcf_integer(p_data: PackedByteArray) -> ParseResult:
	if p_data.is_empty():
		return ParseResult.new(true, null, {"value": 0})
	var reader = LCFBinaryReaderScript.new(p_data)
	var value := reader.read_ber()
	if reader.has_error():
		return _reader_failure(reader)
	if not reader.is_eof():
		return _failure("Integer payload has trailing bytes", reader.get_position())
	return ParseResult.new(true, null, {"value": value})


func _decode_tile_layer(p_chunk: Dictionary, p_tile_count: int, p_name: String) -> ParseResult:
	if p_chunk.is_empty():
		return ParseResult.new(true, null, {"tiles": []})
	var bytes: PackedByteArray = p_chunk["data"]
	var expected_size := p_tile_count * 2
	if bytes.size() != expected_size:
		return _failure("%s tile layer has %d bytes, expected %d" % [p_name, bytes.size(), expected_size], p_chunk["payload_offset"])
	var tiles: Array[int] = []
	tiles.resize(p_tile_count)
	for index in range(p_tile_count):
		tiles[index] = bytes[index * 2] | (bytes[index * 2 + 1] << 8)
	return ParseResult.new(true, null, {"tiles": tiles})


func _decode_text_field(p_fields: Dictionary, p_id: int) -> String:
	if not p_fields.has(p_id):
		return ""
	return _text_decoder.decode(p_fields[p_id]["data"])


func _reader_failure(p_reader) -> ParseResult:
	return _failure(p_reader.error_message, p_reader.error_offset)


func _failure(p_message: String, p_offset: int = -1) -> ParseResult:
	return ParseResult.new(false, ParseError.new(p_offset, p_message))