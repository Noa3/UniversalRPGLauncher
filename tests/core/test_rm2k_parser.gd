## class_name RM2KParserTest
## tests/core/test_rm2k_parser.gd
##
## Unit tests for RM2KParser.
## Tests parsing of Game.ini, database, map, and save data.

extends Test

var parser: RefCounted


func setup() -> void:
	parser = preload("res://src/rm2k/parser/rm2k_parser.gd").new()
	_create_test_files()


func teardown() -> void:
	_cleanup_test_files()


func _create_test_files() -> void:
	## Create test directory
	var dir := "user://rm2k_test"
	if not DirAccess.dir_exists_absolute(dir):
		DirAccess.make_dir_recursive_absolute(dir)
	
	## Create Game.ini
	var ini_path := dir + "/Game.ini"
	var file := FileAccess.open(ini_path, FileAccess.WRITE)
	if file:
		file.store_string("[Game]\nTitle=TestGame\nEngineID=RM2000\nEnginePath=Game.exe\n")
		file.close()
	
	## Create a minimal database file (100 bytes of zeros)
	var db_path := dir + "/Data.rdata"
	file = FileAccess.open(db_path, FileAccess.WRITE)
	if file:
		var zeros := PackedByteArray()
		for i in range(100):
			zeros.append(0)
		file.store_buffer(zeros)
		file.close()
	
	## Create a minimal map file (width=20, height=15, header=10 bytes)
	var map_path := dir + "/Map001.rmm"
	file = FileAccess.open(map_path, FileAccess.WRITE)
	if file:
		## Header: width(2), height(2), unknown(6)
		var header := PackedByteArray()
		header.append(20)  ## width low
		header.append(0)   ## width high
		header.append(15)  ## height low
		header.append(0)   ## height high
		for i in range(6):
			header.append(0)
		file.store_buffer(header)
		
		## Tile data: 20 * 15 = 300 bytes per layer (3 layers)
		var tile_data := PackedByteArray()
		for i in range(300 * 3):
			tile_data.append(0)
		file.store_buffer(tile_data)
		
		## Empty event list (0 events)
		var empty_event := PackedByteArray([0, 0])
		file.store_buffer(empty_event)
		
		file.close()
	
	## Create a minimal save file
	var save_path := dir + "/Save001.rmm"
	file = FileAccess.open(save_path, FileAccess.WRITE)
	if file:
		## Save header (16 bytes)
		var header := PackedByteArray()
		header.append(8)  ## title length
		header.append(0)  ## title length high
		header.append(0)  ## padding
		header.append(0)  ## padding
		header.append(1)  ## map ID low
		header.append(0)  ## map ID high
		header.append(10) ## player X low
		header.append(0)  ## player X high
		header.append(10) ## player Y low
		header.append(0)  ## player Y high
		header.append(20) ## switch count low
		header.append(0)  ## switch count high
		header.append(50) ## variable count low
		header.append(0)  ## variable count high
		header.append(0)  ## padding
		header.append(0)  ## padding
		file.store_buffer(header)
		
		## Title
		var title := "TestGame"
		for c in title.to_ascii_buffer():
			header.append(c)
		file.store_buffer(title.to_ascii_buffer())
		
		## Switches (20 switches = 3 bytes)
		var switch_data := PackedByteArray([0xFF, 0x00, 0x01])
		file.store_buffer(switch_data)
		
		## Variables (50 variables = 100 bytes)
		var var_data := PackedByteArray()
		for i in range(50):
			var_data.append(i & 0xFF)
			var_data.append((i >> 8) & 0xFF)
		file.store_buffer(var_data)
		
		file.close()


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
	assert_eq(result.get_data()["Title"], "TestGame")
	assert_eq(result.get_data()["EngineID"], "RM2000")


func test_parse_game_ini_not_found() -> void:
	var result := parser.parse_game_ini("user://rm2k_test/NonExistent.ini")
	assert_false(result.is_success())
	assert_ne(result.get_error(), null)
	assert_true("not found" in result.get_error().message.to_lower())


func test_parse_game_ini_wrong_header() -> void:
	var dir := "user://rm2k_test"
	var file := FileAccess.open(dir + "/BadGame.ini", FileAccess.WRITE)
	if file:
		file.store_string("[BadHeader]\nTitle=Test\n")
		file.close()
	
	var result := parser.parse_game_ini(dir + "/BadGame.ini")
	assert_false(result.is_success())


func test_parse_game_ini_empty_file() -> void:
	var dir := "user://rm2k_test"
	var file := FileAccess.open(dir + "/EmptyGame.ini", FileAccess.WRITE)
	if file:
		file.close()
	
	var result := parser.parse_game_ini(dir + "/EmptyGame.ini")
	assert_false(result.is_success())


# === TESTS: Database Parsing ===

func test_parse_database_success() -> void:
	var result := parser.parse_database("user://rm2k_test/Data.rdata")
	assert_true(result.is_success())
	assert_true("metadata" in result.get_data())


func test_parse_database_not_found() -> void:
	var result := parser.parse_database("user://rm2k_test/NonExistent.rdata")
	assert_false(result.is_success())
	assert_ne(result.get_error(), null)


func test_parse_database_too_small() -> void:
	var dir := "user://rm2k_test"
	var file := FileAccess.open(dir + "/Tiny.rdata", FileAccess.WRITE)
	if file:
		file.store_string("abc")  ## Only 3 bytes, less than header
		file.close()
	
	var result := parser.parse_database(dir + "/Tiny.rdata")
	assert_false(result.is_success())


# === TESTS: Map Parsing ===

func test_parse_map_success() -> void:
	var result := parser.parse_map("user://rm2k_test/Map001.rmm")
	assert_true(result.is_success())
	assert_eq(result.get_data()["width"], 20)
	assert_eq(result.get_data()["height"], 15)
	assert_eq(result.get_data()["event_count"], 0)


func test_parse_map_not_found() -> void:
	var result := parser.parse_map("user://rm2k_test/NonExistent.rmm")
	assert_false(result.is_success())
	assert_ne(result.get_error(), null)


func test_parse_map_too_small() -> void:
	var dir := "user://rm2k_test"
	var file := FileAccess.open(dir + "/TinyMap.rmm", FileAccess.WRITE)
	if file:
		file.store_string("abc")  ## Less than header size
		file.close()
	
	var result := parser.parse_map(dir + "/TinyMap.rmm")
	assert_false(result.is_success())


# === TESTS: Save Parsing ===

func test_parse_save_success() -> void:
	var result := parser.parse_save("user://rm2k_test/Save001.rmm")
	assert_true(result.is_success())
	var save_data := result.get_data()["save_data"]
	assert_eq(save_data["map_id"], 1)
	assert_eq(save_data["player_x"], 10)
	assert_eq(save_data["player_y"], 10)
	assert_eq(save_data["title"], "TestGame")


func test_parse_save_not_found() -> void:
	var result := parser.parse_save("user://rm2k_test/NonExistent.rmm")
	assert_false(result.is_success())
	assert_ne(result.get_error(), null)


func test_parse_save_too_small() -> void:
	var dir := "user://rm2k_test"
	var file := FileAccess.open(dir + "/TinySave.rmm", FileAccess.WRITE)
	if file:
		file.store_string("abc")  ## Less than save header
		file.close()
	
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
