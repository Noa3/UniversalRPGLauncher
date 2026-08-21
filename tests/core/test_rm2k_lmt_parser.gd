## tests/core/test_rm2k_lmt_parser.gd
## Bounded LMT map-tree parsing tests.
## Field IDs follow EasyRPG/liblcf's LMT reader definitions.

extends Test

const RM2KParserScript = preload("res://src/rm2k/parser/rm2k_parser.gd")
const FIXTURE_ROOT := "res://tests/fixtures/easyrpg-testgame"

var parser: RM2KParserScript


func setup() -> void:
	parser = RM2KParserScript.new()


func test_parse_real_rm2000_map_tree() -> void:
	var result: Variant = _parse_map_tree(FIXTURE_ROOT + "/rm2000/RPG_RT.lmt")
	assert_true(result.is_success(), _describe_error(result))
	if not result.is_success():
		return
	var data: Dictionary = result.get_data()
	assert_eq(data["header"], "LcfMapTree")
	assert_eq(data["map_count"], 81)
	assert_eq(data["maps"][0]["id"], -1)
	assert_eq(data["maps"][0]["name"], "MAP-0001")
	assert_eq(data["maps"][1]["id"], 0)
	assert_eq(data["maps"][1]["name"], "RPG Maker 2000 Test suite")
	assert_eq(data["tree_order"].size(), 81)
	assert_eq(data["active_node"], 50)
	assert_eq(data["start"]["party_map_id"], 30)
	assert_eq(data["start"]["party_x"], 37)
	assert_eq(data["start"]["party_y"], 72)


func test_parse_real_rm2003_map_tree() -> void:
	var result: Variant = _parse_map_tree(FIXTURE_ROOT + "/rm2003/RPG_RT.lmt")
	assert_true(result.is_success(), _describe_error(result))
	if not result.is_success():
		return
	var data: Dictionary = result.get_data()
	assert_eq(data["header"], "LcfMapTree")
	assert_eq(data["map_count"], 22)
	assert_eq(data["maps"][0]["id"], 0)
	assert_eq(data["maps"][0]["name"], "RPG Maker 2003 Test suite")
	assert_eq(data["maps"][2]["parent_id"], 1)
	assert_eq(data["tree_order"].size(), 22)
	assert_eq(data["active_node"], 20)
	assert_eq(data["start"]["party_map_id"], 1)
	assert_eq(data["start"]["party_x"], 4)
	assert_eq(data["start"]["party_y"], 8)


func test_parse_empty_map_tree() -> void:
	var result: Variant = _parse_map_tree(_write_fixture("Empty.lmt", _build_lmt([], [], 0, {})))
	assert_true(result.is_success(), _describe_error(result))
	if result.is_success():
		assert_eq(result.get_data()["map_count"], 0)
		assert_eq(result.get_data()["tree_order"], [])


func test_parse_map_tree_rejects_truncation() -> void:
	var bytes := _build_lmt([_map_entry(1, "Root", 0, 0)], [1], 1, {1: 1, 2: 2, 3: 3})
	bytes = bytes.slice(0, bytes.size() - 1)
	var result: Variant = _parse_map_tree(_write_fixture("Truncated.lmt", bytes))
	assert_false(result.is_success())


func test_parse_map_tree_rejects_malicious_map_count() -> void:
	var bytes := _header()
	bytes.append_array(_ber(100001))
	var result: Variant = _parse_map_tree(_write_fixture("HugeCount.lmt", bytes))
	assert_false(result.is_success())


func test_parse_map_tree_rejects_invalid_parent_reference() -> void:
	var bytes := _build_lmt([_map_entry(1, "Child", 99, 1)], [1], 1, {1: 1, 2: 2, 3: 3})
	var result: Variant = _parse_map_tree(_write_fixture("InvalidParent.lmt", bytes))
	assert_false(result.is_success())


func test_parse_map_tree_rejects_parent_cycle() -> void:
	var entries := [
		_map_entry(1, "One", 2, 1),
		_map_entry(2, "Two", 1, 1),
	]
	var result: Variant = _parse_map_tree(_write_fixture("Cycle.lmt", _build_lmt(entries, [1, 2], 1, {1: 1, 2: 2, 3: 3})))
	assert_false(result.is_success())


func _parse_map_tree(p_path: String) -> Variant:
	if not parser.has_method("parse_map_tree"):
		return RM2KParserScript.ParseResult.new(
			false,
			RM2KParserScript.ParseError.new(-1, "parse_map_tree is not implemented")
		)
	return parser.parse_map_tree(p_path)


func _map_entry(p_id: int, p_name: String, p_parent: int, p_indent: int) -> Dictionary:
	return {
		"id": p_id,
		"fields": [
			{"id": 1, "payload": p_name.to_utf8_buffer()},
			{"id": 2, "payload": _ber(p_parent)},
			{"id": 3, "payload": _ber(p_indent)},
			{"id": 4, "payload": _ber(1)},
		],
	}


func _build_lmt(
	p_entries: Array,
	p_tree_order: Array[int],
	p_active_node: int,
	p_start: Dictionary
) -> PackedByteArray:
	var bytes := _header()
	bytes.append_array(_ber(p_entries.size()))
	for entry in p_entries:
		bytes.append_array(_ber(entry["id"]))
		for field in entry["fields"]:
			bytes.append_array(_chunk(field["id"], field["payload"]))
		bytes.append(0)
	bytes.append_array(_ber(p_tree_order.size()))
	for map_id in p_tree_order:
		bytes.append_array(_ber(map_id))
	bytes.append_array(_ber(p_active_node))
	for field_id in p_start.keys():
		bytes.append_array(_chunk(field_id, _ber(p_start[field_id])))
	bytes.append(0)
	return bytes


func _header() -> PackedByteArray:
	var bytes := _ber(10)
	bytes.append_array("LcfMapTree".to_ascii_buffer())
	return bytes


func _ber(p_value: int) -> PackedByteArray:
	var value := p_value
	if value < 0:
		value += 0x100000000
	var groups := PackedByteArray()
	while value >= 0x80:
		groups.append(value & 0x7f)
		value >>= 7
	groups.append(value)
	var bytes := PackedByteArray()
	for i in range(groups.size() - 1, -1, -1):
		var current: int = groups[i]
		if i > 0:
			current |= 0x80
		bytes.append(current)
	return bytes


func _chunk(p_id: int, p_payload: PackedByteArray) -> PackedByteArray:
	var bytes := _ber(p_id)
	bytes.append_array(_ber(p_payload.size()))
	bytes.append_array(p_payload)
	return bytes


func _write_fixture(p_name: String, p_bytes: PackedByteArray) -> String:
	var path := "user://rm2k_lmt_test/" + p_name
	DirAccess.make_dir_recursive_absolute(path.get_base_dir())
	var file := FileAccess.open(path, FileAccess.WRITE)
	file.store_buffer(p_bytes)
	file.close()
	return path


func _describe_error(p_result) -> String:
	if p_result.is_success():
		return ""
	return p_result.get_error().describe()
