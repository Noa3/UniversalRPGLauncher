## tests/core/test_rm2k_real_fixtures.gd
## Real-fixture validation for the pinned EasyRPG TestGame LCF files.
## Fixtures are parser input only; no imported game code is executed.

extends Test

const RM2KParserScript = preload("res://src/rm2k/parser/rm2k_parser.gd")
const LCFBinaryReaderScript = preload("res://src/rm2k/parser/lcf_binary_reader.gd")
const FIXTURE_ROOT := "res://tests/fixtures/easyrpg-testgame"

var parser: RM2KParserScript


func setup() -> void:
	parser = RM2KParserScript.new()


func test_rm2000_database_has_valid_lcf_boundaries() -> void:
	var result := parser.parse_database(FIXTURE_ROOT + "/rm2000/RPG_RT.ldb")
	assert_true(result.is_success(), _describe_error(result))
	if not result.is_success():
		return
	var data := result.get_data()
	assert_eq(data["header"], "LcfDataBase")
	assert_true(data["file_size"] > 0)
	assert_true(data["chunk_count"] > 0)
	assert_true(data["chunk_count"] < 100000)


func test_rm2003_database_has_valid_lcf_boundaries() -> void:
	var result := parser.parse_database(FIXTURE_ROOT + "/rm2003/RPG_RT.ldb")
	assert_true(result.is_success(), _describe_error(result))
	if not result.is_success():
		return
	var data := result.get_data()
	assert_eq(data["header"], "LcfDataBase")
	assert_true(data["file_size"] > 0)
	assert_true(data["chunk_count"] > 0)
	assert_true(data["chunk_count"] < 100000)


func test_rm2000_map_has_valid_lcf_boundaries() -> void:
	var result := parser.parse_map(FIXTURE_ROOT + "/rm2000/Map0001.lmu")
	assert_true(result.is_success(), _describe_error(result))
	if not result.is_success():
		return
	var data := result.get_data()
	assert_eq(data["header"], "LcfMapUnit")
	assert_true(data["width"] > 0)
	assert_true(data["height"] > 0)
	assert_true(data["chunk_count"] > 0)


func test_rm2003_map_has_valid_lcf_boundaries() -> void:
	var result := parser.parse_map(FIXTURE_ROOT + "/rm2003/Map0001.lmu")
	assert_true(result.is_success(), _describe_error(result))
	if not result.is_success():
		return
	var data := result.get_data()
	assert_eq(data["header"], "LcfMapUnit")
	assert_true(data["width"] > 0)
	assert_true(data["height"] > 0)
	assert_true(data["chunk_count"] > 0)


func test_real_fixture_framing_consumes_exact_file_boundaries() -> void:
	_assert_fixture_framing("rm2000/RPG_RT.ldb", "LcfDataBase", 16, 210227, false)
	_assert_fixture_framing("rm2003/RPG_RT.ldb", "LcfDataBase", 22, 416513, false)
	_assert_fixture_framing("rm2000/Map0001.lmu", "LcfMapUnit", 6, 8544, true)
	_assert_fixture_framing("rm2003/Map0001.lmu", "LcfMapUnit", 11, 8488, true)


func _describe_error(p_result) -> String:
	if p_result.is_success():
		return ""
	return p_result.get_error().describe()


func _assert_fixture_framing(
	p_relative_path: String,
	p_header: String,
	p_expected_chunks: int,
	p_expected_size: int,
	p_expected_terminator: bool
) -> void:
	var path := FIXTURE_ROOT + "/" + p_relative_path
	var file := FileAccess.open(path, FileAccess.READ)
	assert_ne(file, null, "Open fixture " + path)
	if file == null:
		return
	var bytes := file.get_buffer(file.get_length())
	file.close()
	assert_eq(bytes.size(), p_expected_size, "Fixture size " + p_relative_path)

	var reader := LCFBinaryReaderScript.new(bytes)
	assert_eq(reader.read_header(p_header), p_header, "Fixture header " + p_relative_path)
	assert_false(reader.has_error(), "Fixture header error " + p_relative_path)
	var chunk_count := 0
	var saw_terminator := false
	while not reader.is_eof():
		var chunk: Dictionary = reader.read_chunk()
		assert_false(reader.has_error(), "Fixture chunk error " + p_relative_path)
		if reader.has_error():
			return
		chunk_count += 1
		if chunk["terminator"]:
			saw_terminator = true
			break
	assert_eq(chunk_count, p_expected_chunks, "Fixture chunk count " + p_relative_path)
	assert_eq(saw_terminator, p_expected_terminator, "Fixture terminator " + p_relative_path)
	assert_eq(reader.get_position(), bytes.size(), "Fixture boundary " + p_relative_path)
