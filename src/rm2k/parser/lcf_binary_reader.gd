class_name LCFBinaryReader
extends RefCounted

const MAX_BER_BYTES := 5
const MAX_INTEGER := 0x7fffffff
const MAX_UNSIGNED_INTEGER := 0xffffffff
const MAX_CHUNK_BYTES := 32 * 1024 * 1024
const MAX_CHUNKS := 100_000
const MAX_ARRAY_ITEMS := 100_000
const MAX_STRUCT_FIELDS := 10_000

var error_message: String = ""
var error_offset: int = -1

var _data := PackedByteArray()
var _position := 0


func _init(p_data: PackedByteArray = PackedByteArray()) -> void:
	reset(p_data)


func reset(p_data: PackedByteArray) -> void:
	_data = p_data
	_position = 0
	error_message = ""
	error_offset = -1


func has_error() -> bool:
	return not error_message.is_empty()


func get_position() -> int:
	return _position


func get_size() -> int:
	return _data.size()


func get_remaining() -> int:
	return _data.size() - _position


func is_eof() -> bool:
	return _position >= _data.size()


func read_ber() -> int:
	var value := 0
	var start := _position
	for index in range(MAX_BER_BYTES):
		if is_eof():
			_fail("Unexpected end of data while reading BER integer", start)
			return -1
		var byte := _data[_position]
		_position += 1
		if value > (MAX_INTEGER >> 7):
			_fail("BER integer overflow", start)
			return -1
		value = (value << 7) | (byte & 0x7f)
		if (byte & 0x80) == 0:
			return value
		if index == MAX_BER_BYTES - 1:
			_fail("BER integer exceeds %d bytes" % MAX_BER_BYTES, start)
	return -1


func read_signed_ber() -> int:
	var value: int = 0
	var start := _position
	for index in range(MAX_BER_BYTES):
		if is_eof():
			_fail("Unexpected end of data while reading signed BER integer", start)
			return 0
		var byte := _data[_position]
		_position += 1
		if value > (MAX_UNSIGNED_INTEGER >> 7):
			_fail("Signed BER integer overflow", start)
			return 0
		value = (value << 7) | (byte & 0x7f)
		if (byte & 0x80) == 0:
			return value - 0x100000000 if value > MAX_INTEGER else value
		if index == MAX_BER_BYTES - 1:
			_fail("Signed BER integer exceeds %d bytes" % MAX_BER_BYTES, start)
			return 0
	return 0


func read_bytes(p_length: int) -> PackedByteArray:
	if p_length < 0:
		_fail("Negative read length", _position)
		return PackedByteArray()
	if p_length > get_remaining():
		_fail("Read of %d bytes exceeds remaining %d bytes" % [p_length, get_remaining()], _position)
		return PackedByteArray()
	var result := _data.slice(_position, _position + p_length)
	_position += p_length
	return result


func read_header(p_expected: String) -> String:
	var length := read_ber()
	if has_error():
		return ""
	if length <= 0 or length > 64:
		_fail("Invalid LCF header length %d" % length, _position)
		return ""
	var header_bytes := read_bytes(length)
	if has_error():
		return ""
	var header := header_bytes.get_string_from_ascii()
	if header != p_expected:
		_fail("Expected LCF header %s, got %s" % [p_expected, header], 0)
		return ""
	return header


func read_chunk() -> Dictionary:
	var chunk_offset := _position
	var id := read_ber()
	if has_error():
		return {}
	if id == 0:
		return {
			"id": 0,
			"length": 0,
			"offset": chunk_offset,
			"payload_offset": _position,
			"data": PackedByteArray(),
			"terminator": true,
		}
	var length := read_ber()
	if has_error():
		return {}
	if length > MAX_CHUNK_BYTES:
		_fail("Chunk %d exceeds %d-byte limit" % [id, MAX_CHUNK_BYTES], chunk_offset)
		return {}
	var payload_offset := _position
	var payload := read_bytes(length)
	if has_error():
		return {}
	return {
		"id": id,
		"length": length,
		"offset": chunk_offset,
		"payload_offset": payload_offset,
		"data": payload,
		"terminator": false,
	}


func _fail(p_message: String, p_offset: int) -> void:
	if has_error():
		return
	error_message = p_message
	error_offset = p_offset
