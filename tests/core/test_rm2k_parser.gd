## class_name RM2KParserTest
## tests/core/test_rm2k_parser.gd
##
## Unit tests for RM2KParser.
## Tests parsing of Game.ini, LCF database (LDB), map (LMU), and save (LSD) files.
## Fixtures are real LCF encodings: BER length + header, then BER-coded
## ID/length/payload chunks, structures terminated by ID 0.

extends Test

const RM2KParserScript = preload("res://src/rm2k/parser/rm2k_parser.gd")

var parser: RM2KParserScript


func setup() -> void:
	parser = RM2KParserScript.new()
	_create_test_files()


func teardown() -> void:
	_cleanup_test_files()


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


func _lcf(p_header: String, p_chunks: Array) -> PackedByteArray:
	var bytes := _ber(p_header.length())
	bytes.append_array(p_header.to_ascii_buffer())
	for chunk in p_chunks:
		bytes.append_array(chunk)
	return bytes


func _tile_layer(p_count: int, p_start: int = 0) -> PackedByteArray:
	var bytes := PackedByteArray()
	bytes.resize(p_count * 2)
	for i in range(p_count):
		var tile := (p_start + i) & 0xFFFF
		bytes[i * 2] = tile & 0xFF
		bytes[i * 2 + 1] = (tile >> 8) & 0xFF
	return bytes


func _write_file(p_path: String, p_bytes: PackedByteArray) -> void:
	var file := FileAccess.open(p_path, FileAccess.WRITE)
	if file:
		file.store_buffer(p_bytes)
		file.close()


func _create_test_files() -> void:
	var dir := "user://rm2k_test"
	if not DirAccess.dir_exists_absolute(dir):
		DirAccess.make_dir_recursive_absolute(dir)

	_write_file(dir + "/Game.ini", "[RPG_RT]\nGameTitle=TestGame\nEngineID=RM2000\nEnginePath=Game.exe\n".to_ascii_buffer())

	var database := _lcf("LcfDataBase", [
		_chunk(0x1a, _ber(259)),
		_chunk(0x0b, _ber(0)),
		PackedByteArray([0x00]),
	])
	_write_file(dir + "/Data.rdata", database)

	var map := _lcf("LcfMapUnit", [
		_chunk(0x01, _ber(1)),
		_chunk(0x02, _ber(20)),
		_chunk(0x03, _ber(15)),
		_chunk(0x47, _tile_layer(300)),
		_chunk(0x48, _tile_layer(300, 0x8000)),
		_chunk(0x51, _ber(0)),
		PackedByteArray([0x00]),
	])
	_write_file(dir + "/Map001.rmm", map)

	var save := _lcf("LcfSaveData", [
		_chunk(0x01, "TestGame".to_ascii_buffer()),
		PackedByteArray([0x00]),
	])
	_write_file(dir + "/Save001.rmm", save)


func _cleanup_test_files() -> void:
	var dir := "user://rm2k_test"
	if not DirAccess.dir_exists_absolute(dir):
		return
	var d := DirAccess.open(dir)
	if d == null:
		return
	d.list_dir_begin()
	var file_name := d.get_next()
	while file_name != "":
		DirAccess.remove_absolute(dir + "/" + file_name)
		file_name = d.get_next()
	DirAccess.remove_absolute(dir)


# === TESTS: Game.ini Parsing ===

func test_parse_game_ini_success() -> void:
	var result := parser.parse_game_ini("user://rm2k_test/Game.ini")
	assert_true(result.is_success())
	assert_eq(result.get_data()["GameTitle"], "TestGame")
	assert_eq(result.get_data()["EngineID"], "RM2000")
	assert_eq(result.get_data()["section"], "RPG_RT")


func test_parse_game_ini_not_found() -> void:
	var result := parser.parse_game_ini("user://rm2k_test/NonExistent.ini")
	assert_false(result.is_success())
	assert_ne(result.get_error(), null)
	assert_true("not found" in result.get_error().message.to_lower())


func test_parse_game_ini_wrong_header() -> void:
	var dir := "user://rm2k_test"
	_write_file(dir + "/BadGame.ini", "[BadHeader]\nTitle=Test\n".to_ascii_buffer())
	var result := parser.parse_game_ini(dir + "/BadGame.ini")
	assert_false(result.is_success())


func test_parse_game_ini_empty_file() -> void:
	var dir := "user://rm2k_test"
	_write_file(dir + "/EmptyGame.ini", PackedByteArray())
	var result := parser.parse_game_ini(dir + "/EmptyGame.ini")
	assert_false(result.is_success())


# === TESTS: Database Parsing ===

func test_parse_database_success() -> void:
	var result := parser.parse_database("user://rm2k_test/Data.rdata")
	assert_true(result.is_success())
	var data := result.get_data()
	assert_eq(data["format"], "LDB")
	assert_eq(data["header"], "LcfDataBase")
	assert_eq(data["version"], 259)
	assert_eq(data["engine_family"], "RPG Maker 2000")
	assert_eq(data["section_counts"]["actors"], 0)


func test_parse_database_not_found() -> void:
	var result := parser.parse_database("user://rm2k_test/NonExistent.rdata")
	assert_false(result.is_success())
	assert_ne(result.get_error(), null)


func test_parse_database_too_small() -> void:
	var dir := "user://rm2k_test"
	_write_file(dir + "/Tiny.rdata", "abc".to_ascii_buffer())
	var result := parser.parse_database(dir + "/Tiny.rdata")
	assert_false(result.is_success())


func test_parse_database_wrong_header() -> void:
	var dir := "user://rm2k_test"
	_write_file(dir + "/Wrong.rdata", _lcf("LcfSomething", [PackedByteArray([0x00])]))
	var result := parser.parse_database(dir + "/Wrong.rdata")
	assert_false(result.is_success())
	assert_true("header" in result.get_error().message.to_lower())


func test_parse_database_truncated_chunk() -> void:
	var dir := "user://rm2k_test"
	var bytes := _ber(11)
	bytes.append_array("LcfDataBase".to_ascii_buffer())
	bytes.append(0x1a)
	_write_file(dir + "/Trunc.rdata", bytes)
	var result := parser.parse_database(dir + "/Trunc.rdata")
	assert_false(result.is_success())


# === TESTS: Map Parsing ===

func test_parse_map_success() -> void:
	var result := parser.parse_map("user://rm2k_test/Map001.rmm")
	assert_true(result.is_success())
	var data := result.get_data()
	assert_eq(data["format"], "LMU")
	assert_eq(data["width"], 20)
	assert_eq(data["height"], 15)
	assert_eq(data["chipset_id"], 1)
	assert_eq(data["event_count"], 0)
	var lower: Array = data["lower_layer"]
	assert_eq(lower.size(), 300)
	assert_eq(lower[0], 0)
	assert_eq(lower[299], 299)
	var upper: Array = data["upper_layer"]
	assert_eq(upper.size(), 300)
	assert_eq(upper[299], 0x8000 + 299)


func test_parse_map_not_found() -> void:
	var result := parser.parse_map("user://rm2k_test/NonExistent.rmm")
	assert_false(result.is_success())
	assert_ne(result.get_error(), null)


func test_parse_map_too_small() -> void:
	var dir := "user://rm2k_test"
	_write_file(dir + "/TinyMap.rmm", "abc".to_ascii_buffer())
	var result := parser.parse_map(dir + "/TinyMap.rmm")
	assert_false(result.is_success())


func test_parse_map_bad_layer_size() -> void:
	var dir := "user://rm2k_test"
	var map := _lcf("LcfMapUnit", [
		_chunk(0x01, _ber(1)),
		_chunk(0x02, _ber(20)),
		_chunk(0x03, _ber(15)),
		_chunk(0x47, _tile_layer(2)),
		PackedByteArray([0x00]),
	])
	_write_file(dir + "/BadLayer.rmm", map)
	var result := parser.parse_map(dir + "/BadLayer.rmm")
	assert_false(result.is_success())
	assert_true("expected" in result.get_error().message.to_lower())


func test_parse_map_dimension_limit() -> void:
	var dir := "user://rm2k_test"
	var map := _lcf("LcfMapUnit", [
		_chunk(0x01, _ber(1)),
		_chunk(0x02, _ber(600)),
		_chunk(0x03, _ber(15)),
		PackedByteArray([0x00]),
	])
	_write_file(dir + "/Huge.rmm", map)
	var result := parser.parse_map(dir + "/Huge.rmm")
	assert_false(result.is_success())


# === TESTS: Save Parsing ===

func test_parse_save_success() -> void:
	var result := parser.parse_save("user://rm2k_test/Save001.rmm")
	assert_true(result.is_success())
	var data := result.get_data()
	assert_eq(data["format"], "LSD")
	assert_eq(data["header"], "LcfSaveData")
	assert_eq(data["chunk_count"], 1)
	var chunks: Array = data["chunks"]
	assert_eq(chunks[0]["id"], 1)


func test_parse_save_not_found() -> void:
	var result := parser.parse_save("user://rm2k_test/NonExistent.rmm")
	assert_false(result.is_success())
	assert_ne(result.get_error(), null)


func test_parse_save_too_small() -> void:
	var dir := "user://rm2k_test"
	_write_file(dir + "/TinySave.rmm", "abc".to_ascii_buffer())
	var result := parser.parse_save(dir + "/TinySave.rmm")
	assert_false(result.is_success())


# === TESTS: Error Handling ===

func test_parse_error_details() -> void:
	var result := parser.parse_game_ini("user://rm2k_test/NonExistent.ini")
	var error := result.get_error()
	assert_ne(error, null)
	assert_true("not found" in error.message.to_lower())
	assert_true("NonExistent" in error.message)


func test_parse_returns_empty_data_on_failure() -> void:
	var result := parser.parse_game_ini("user://rm2k_test/NonExistent.ini")
	assert_false(result.is_success())
	assert_eq(result.get_data().size(), 0)